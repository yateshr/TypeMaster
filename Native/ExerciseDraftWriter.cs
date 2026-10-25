using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TypeMaster.Native;
// One worker, one pending snapshot. Slow storage never holds the input thread.
internal sealed class ExerciseDraftWriter
{
    readonly object state = new(), disk = new();
    (string File, ExerciseDraft Draft, long Generation)? pending;
    long generation;
    bool running;
    Task worker = Task.CompletedTask;
    readonly Action<string, ExerciseDraft> write;
    internal ExerciseDraftWriter(Action<string, ExerciseDraft>? write = null) { this.write = write ?? Write; }
    internal Task Completion { get { lock (state) return worker; } }
    internal Exception? LastError { get; private set; }
    internal void Queue(string file, ExerciseDraft draft)
    {
        lock (state)
        {
            pending = (file, draft, generation);
            if (running) return;
            running = true;
            worker = Task.Run(Work);
        }
    }
    void Work()
    {
        while (true)
        {
            (string File, ExerciseDraft Draft, long Generation) next;
            lock (state)
            {
                if (pending is not { } item) { running = false; return; }
                next = item; pending = null;
            }
            lock (disk)
            {
                lock (state) if (next.Generation != generation) continue;
                try { write(next.File, next.Draft); LastError = null; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { LastError = error; }
            }
        }
    }
    internal void Save(string file, ExerciseDraft draft)
    {
        lock (disk) { Invalidate(); write(file, draft); LastError = null; }
    }
    internal void Clear(string file)
    {
        lock (disk)
        {
            Invalidate();
            foreach (string path in new[] { file, file + ".bak", file + ".tmp" }) if (File.Exists(path)) File.Delete(path);
        }
    }
    void Invalidate() { lock (state) { generation++; pending = null; } }
    static void Write(string file, ExerciseDraft draft)
    {
        string temp = file + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { JsonSerializer.Serialize(stream, draft); stream.Flush(true); }
        if (File.Exists(file)) File.Replace(temp, file, file + ".bak", true); else File.Move(temp, file);
    }
}
