using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace TypeMaster.Native;
internal static class TypingPerformance
{
    internal static void Verify(string output, Action<bool, string> check)
    {
        string file = Path.Combine(output, "background-draft.json");
        using var started = new System.Threading.ManualResetEventSlim();
        using var release = new System.Threading.ManualResetEventSlim();
        int writes = 0;
        var writer = new ExerciseDraftWriter((path, draft) =>
        {
            if (System.Threading.Interlocked.Increment(ref writes) == 1) { started.Set(); if (!release.Wait(5000)) throw new IOException("Storage simulation timed out"); }
            File.WriteAllText(path, JsonSerializer.Serialize(draft));
        });
        writer.Queue(file, new ExerciseDraft { Typed = "first" });
        check(started.Wait(3000), "Recovery storage runs on a background worker");
        var elapsed = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++) writer.Queue(file, new ExerciseDraft { Typed = "latest " + i });
        check(elapsed.ElapsedMilliseconds < 100, "Slow recovery storage cannot block input or build an unbounded write queue");
        release.Set(); check(writer.Completion.Wait(3000), "Background recovery worker finishes queued snapshots");
        check(writes == 2 && JsonSerializer.Deserialize<ExerciseDraft>(File.ReadAllText(file))!.Typed == "latest 99", "Recovery coalesces pending snapshots and retains the latest candidate text");
        for (int i = 0; i < 100; i++) writer.Queue(file, new ExerciseDraft { Typed = "obsolete" });
        writer.Clear(file); writer.Completion.Wait(3000);
        check(!File.Exists(file), "Cancelled or completed recovery snapshots cannot be recreated by an older worker");
        var window = new MainWindow(new ProfileStore(Path.Combine(output, "rapid-input"))) { ShowInTaskbar = false };
        window.Show(); window.EnterProfile("Input regression sample", false); window.Current!.Sound = false;
        string passage = string.Concat(Enumerable.Repeat("The candidate types accurately. ", 100));
        window.StartExercise(passage, "Rapid typing", 600, "Test"); window.UpdateLayout();
        for (int i = 0; i < 600; i++) { window.TypingBox!.SelectedText = passage[i].ToString(); window.TypingBox.Select(window.TypingBox.Text.Length, 0); }
        check(window.TypingBox!.Text == passage[..600], "Rapid typing preserves every character in order");
        check(!Directory.GetFiles(window.Store.DirectoryPath, "exercise-*.json").Any(), "Individual keystrokes do not synchronously write recovery files");
        var result = window.FinishExercise(true)!;
        check(result.TypedText == passage[..600] && result.Errors == 0 && result.Correct == 600 && result.Accuracy == 100, "Rapid input preserves exact response, error counts and scoring");
        window.Close();
    }
    public static int Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TypeMaster-typing-performance", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new ProfileStore(directory));
        window.Show(); window.EnterProfile("Performance sample", false); window.Current!.Sound = false;
        var results = new List<object>();
        foreach (int prefix in new[] { 0, 2000, 8000 })
        {
            string passage = string.Concat(Enumerable.Repeat("The candidate types words and numbers accurately. ", 400));
            window.StartExercise(passage, "Input performance", 3600, "Test");
            window.UpdateLayout();
            window.TypingBox!.Text = passage[..prefix]; window.TypingBox.Select(prefix, 0);
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            var samples = new List<double>();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = prefix; i < prefix + 120; i++)
            {
                var elapsed = Stopwatch.StartNew();
                window.TypingBox.SelectedText = passage[i].ToString();
                window.TypingBox.Select(window.TypingBox.Text.Length, 0);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                samples.Add(elapsed.Elapsed.TotalMilliseconds);
            }
            var ordered = samples.Order().ToArray();
            results.Add(new { prefix, keystrokes = samples.Count, averageMs = samples.Average(), p95Ms = ordered[(int)(ordered.Length * .95)], maxMs = samples.Max(), allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated, inputPreserved = window.TypingBox.Text == passage[..(prefix + 120)] });
            window.FinishExercise(true);
        }
        window.Close();
        string output = Path.Combine(Path.GetTempPath(), "TypeMaster-typing-performance", "latest.json");
        File.WriteAllText(output, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
