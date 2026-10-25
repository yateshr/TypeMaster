using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TypeMaster.Native;
internal static class SelfTests
{
    public static int Run()
    {
        string output = Path.Combine(Path.GetTempPath(), "TypeMasterNative-tests"); Directory.CreateDirectory(output);
        var checks = new List<string>();
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks.Add(message); }
        try
        {
            Check(WindowFrame.VisibleInsets(-8,-8,1928,1048,0,0,1920,1040,1,1)==new Thickness(8),"Maximized caption compensates for invisible Windows resize borders");
            Check(WindowFrame.VisibleInsets(-1932,-12,12,1452,-1920,0,0,1440,1.5,1.5)==new Thickness(8),"Caption insets support a secondary monitor at 150 percent DPI");
            Check(WindowFrame.VisibleInsets(0,0,1920,1040,0,0,1920,1040,1,1)==new Thickness(0),"Already fitted maximized frames receive no extra padding");
            Check(Scoring.Measure("hello", "hello", 30, 5, 0).NetWpm == 2, "WPM uses elapsed time");
            Check(Scoring.Measure("hello", "hello", 60, 6, 1).Accuracy == 83, "Corrected errors count toward accuracy");
            Check(Scoring.Measure("abcdefghij", "abcdeXXXXX", 60, 10, 5).NetWpm == 1, "Wrong characters excluded from net speed");
            Check(Scoring.Measure("a", "", 0, 0, 0).NetWpm == 0, "Empty metrics remain finite");
            var sampleScore = Scoring.MeasureTest(new string('a', 139), new string('a', 139), 60, 139, 7);
            Check(sampleScore.GrossWpm == 27 && sampleScore.NetWpm == 20 && sampleScore.Accuracy == 74 && sampleScore.ErrorHits == 35, "Test scoring truncates fractional speeds and accuracy after the five-stroke penalty");
            foreach (var example in new[] { (Strokes:158, Errors:5, Gross:31, Net:26, Accuracy:84, NetStrokes:133), (Strokes:133, Errors:2, Gross:26, Net:24, Accuracy:92, NetStrokes:123), (Strokes:139, Errors:2, Gross:27, Net:25, Accuracy:92, NetStrokes:129) })
            {
                var actual = Scoring.MeasureTest(new string('a', example.Strokes), new string('a', example.Strokes), 60, example.Strokes, example.Errors);
                Check(actual.GrossWpm == example.Gross && actual.NetWpm == example.Net && actual.Accuracy == example.Accuracy && actual.NetStrokes == example.NetStrokes && actual.ErrorHits == example.Errors * 5, "Reproduces supplied report: " + example.Strokes + " strokes and " + example.Errors + " reported errors");
                Check(actual.ScoringPolicy == "test-five-stroke-truncate-v1", "New test result identifies its arithmetic policy for " + example.Strokes + " strokes");
            }
            var halfMinute = Scoring.MeasureTest(new string('a',158),new string('a',158),30,158,5); Check(halfMinute.GrossWpm == 63 && halfMinute.NetWpm == 53 && halfMinute.Accuracy == 84, "Test speeds use elapsed minutes before truncation, without changing accuracy");
            var exhausted = Scoring.MeasureTest("abc", "a", 60, 1, 1); Check(exhausted.NetStrokes == 0 && exhausted.NetWpm == 0 && exhausted.Accuracy == 0, "Penalty cannot produce negative strokes, speed or accuracy");
            var emptyTest = Scoring.MeasureTest("abc", "", 0, 0, 0); Check(emptyTest.GrossWpm == 0 && emptyTest.NetWpm == 0 && emptyTest.Accuracy == 100, "Empty test metrics remain defined");
            Check(ReportFactory.IncorrectWords("one two three", "one too three").SequenceEqual(new[] { false, true, false }), "Examination report highlights changed candidate words");
            var corrections=ReportFactory.CorrectedWords("country is home to over","country i hom to");
            Check(corrections.Select(w=>w.Display).SequenceEqual(new[]{"country","i [is]","hom [home]","to"}),"Result report shows reference-style wrong words and bracketed corrections without the unattempted suffix");
            Check(ReportFactory.CorrectedWords("one two three","one extra two three").Count(w=>w.Wrong)==1,"Inserted words do not mark subsequent correct words as errors");
            Check(ReportFactory.CorrectedWords("one two three four","one three four").Any(w=>w.Typed==""&&w.Expected=="two"),"Skipped words are shown as missing corrections");
            string longOriginal = string.Join(" ", Enumerable.Repeat("alpha beta gamma delta", 600)); Check(ReportFactory.IncorrectWords(longOriginal, "extra " + longOriginal).Count(b => b) == 1, "Long report comparisons recover after an inserted word without a large quadratic matrix");
            var tests = Assets.Tests(); Check(tests.Count > 0, "Supplied tests load");
            Check(tests.Any(t => t.Name == "Astronauts"), "Test metadata imported");
            Check(Assets.Course().Count == 12, "All 12 course chapters import");
            var courses = CourseLibrary.LoadAll();
            File.WriteAllText(Path.Combine(output, "course-audit.json"), JsonSerializer.Serialize(Directory.GetFiles(Path.Combine(Assets.Root, "lessons"), "*.cnt").Select(CourseLibrary.Load).Select(c => new { c.File, chapters = c.Chapters.Count, c.MissingEvents, c.SourceGaps }), new JsonSerializerOptions { WriteIndented = true }));
            Check(courses.Any(c => c.File.Equals("UK7Speed.cnt", StringComparison.OrdinalIgnoreCase)), "Speed Building course imported");
            Check(courses.Any(c => c.File.Equals("uk7numxpad.cnt", StringComparison.OrdinalIgnoreCase)), "10-key course imported");
            Check(courses.SelectMany(c => c.Chapters).SelectMany(c => c.Lessons).Any(l => l.Kind == "Introduction" && l.Image.Length > 0), "Illustrated introductions imported");
            Check(courses.SelectMany(c => c.Chapters).SelectMany(c => c.Lessons).Any(l => l.Kind == "Key introduction" && l.Keys.Length > 0), "Interactive finger-placement steps imported");
            Check(courses.Count == Directory.GetFiles(Path.Combine(Assets.Root, "lessons"), "*.cnt").Length, "Every supplied course has playable content");
            Check(courses.Any(c => c.File == "hindi_review2.cnt" && c.Chapters.SelectMany(ch => ch.Lessons).Any(l => l.Text.Contains("रति"))), "Inline Hindi lesson text imports");
            Check(!courses.SelectMany(c => c.SourceGaps).Any(s => s.StartsWith("Unsupported event")), "All supplied course event types handled");
            Check(!Assets.Read(Path.Combine(Assets.Root, "lessons", "it0101.exc")).Contains('\uFFFD'), "Legacy lesson encoding preserves accents");
            var keyboard = KeyboardLayout.Load(Path.Combine(Assets.Root, "keyboards", "us.kbd"));
            Check(keyboard.Keys.Any(k => k.Caption == ";") && keyboard.Keys.Any(k => k.Caption == "Space"), "Keyboard parser preserves semicolon and space");
            foreach (string file in Directory.GetFiles(Path.Combine(Assets.Root, "keyboards"), "*.kbd")) Check(KeyboardLayout.Load(file).Keys.Count > 0, "Keyboard loads: " + Path.GetFileName(file));
            foreach (string mode in TypingGameEngine.Names)
            {
                var engine = new TypingGameEngine(mode, ["cat", "dog"], seed: 42);
                int before = engine.Score; engine.Submit(engine.Expected); Check(engine.Score > before && engine.Completed == 1, mode + " scores correct input");
                engine.Submit("!wrong!"); Check(engine.Errors == 1, mode + " counts wrong input");
                if (mode is "ABC Speed Race" or "Pipe Game" or "Cloud Race") { for (int i = 0; i < 40 && !engine.Over; i++) engine.Submit(engine.Expected); Check(engine.Over, mode + " reaches completion"); }
                else { engine.Advance(61); Check(engine.Over, mode + " respects time limit"); }
            }
            var meterProfile = new Profile { Name = "Meter check" }; var accumulator = new MeterAccumulator(meterProfile); var time = DateTime.Now;
            accumulator.Record(65, time); accumulator.Record(66, time.AddMilliseconds(200)); accumulator.Record(8, time.AddMilliseconds(400));
            Check(accumulator.Today.Keys == 3 && accumulator.Today.CorrectedKeys[66] == 1, "Meter aggregates counts and correction hints");
            Check(accumulator.Today.Pairs["65:66"].Count == 1 && Math.Abs(accumulator.Today.ActiveSeconds - .4) < .01, "Meter aggregates pair timing and active time");
            accumulator.ResetSequence(); accumulator.Record(67, time.AddSeconds(20)); Check(accumulator.Today.Pairs.Count == 1, "Meter resets timing across pauses");
            using (var meter = new WindowsTypingMeter(meterProfile)) { meter.Start(); Check(meter.IsRunning, "Windows keyboard hook installs"); meter.Pause(true); Check(meter.Paused, "Windows meter pauses"); meter.Dispose(); Check(!meter.IsRunning, "Windows keyboard hook is removed"); }
            var store = new ProfileStore(Path.Combine(output, Guid.NewGuid().ToString("N")));
            var window = new MainWindow(store) { ShowInTaskbar = false };
            window.Show(); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-login.png"));
            Check(window.Icon!=null,"Application windows load the embedded TypeMaster icon");
            Check(window.WorkspaceFrame.Effect==null,"Workspace content is rendered independently of the decorative shadow");
            window.NewUser("YATESH"); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-new-user.png"));
            window.EnterProfile("Native test");
            Check(window.WorkspaceFrame.ActualWidth == 790 && window.WorkspaceFrame.ActualHeight == 541, "Reference workspace is exactly 790 by 541");
            var origin = window.WorkspaceFrame.TranslatePoint(new Point(0, 0), window); Check(Math.Abs(origin.X - 325) < 1 && Math.Abs(origin.Y - 166) < 1, "Reference workspace placement matches 1440 by 852 screenshots");
            Check(window.Current != null, "Native profile creation");
            // Compare static artwork without the user's mouse position activating hover opacity.
            window.IsHitTestVisible = false;
            foreach (var page in new[] { "Studying", "Typing Meter", "Typing Test", "Custom Review", "Games", "Statistics", "Settings", "About" }) { window.ShowPage(page); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "screen-" + page.Replace(" ", "-") + ".png")); checks.Add("WPF page renders: " + page); }
            window.IsHitTestVisible = true;
            var visualAudit = new Dictionary<string, double> {
                ["Default keyboard illustration"] = MatchReferenceRegion(Path.Combine(output, "screen-Settings.png"), "settings-reference.png", new Int32Rect(376, 488, 501, 175)),
                ["Game caption artwork"] = MatchReferenceRegion(Path.Combine(output, "screen-Games.png"), "games-reference.png", new Int32Rect(731, 623, 193, 32)),
                ["Close button artwork"] = MatchReferenceRegion(Path.Combine(output, "screen-Settings.png"), "course-reference.png", new Int32Rect(1077, 185, 17, 17)),
                ["Selected navigation button"] = MatchReferenceRegion(Path.Combine(output, "screen-Settings.png"), "settings-reference.png", new Int32Rect(924, 526, 174, 32)),
                ["Course panel gradient"] = MatchReferenceRegion(Path.Combine(output, "screen-Studying.png"), "course-reference.png", new Int32Rect(355, 272, 5, 164)),
                ["Game selector background"] = MatchReferenceRegion(Path.Combine(output, "screen-Games.png"), "games-reference.png", new Int32Rect(332, 400, 10, 200))
            };
            File.WriteAllText(Path.Combine(output, "visual-asset-audit.json"), JsonSerializer.Serialize(visualAudit, new JsonSerializerOptions { WriteIndented = true }));
            // Historical screenshot comparison remains diagnostic: original artwork intentionally differs.
            VerifyOriginalArtwork(Check);
            var screenAudit = new Dictionary<string, double>();
            foreach (var sample in new[] { ("Studying", "course-reference.png"), ("Typing-Meter", "meter-reference.png"), ("Typing-Test", "tests-reference.png"), ("Games", "games-reference.png"), ("Statistics", "statistics-reference.png"), ("Settings", "settings-reference.png"), ("About", "about-reference.png"), ("new-user", "new-user-reference.png") })
                screenAudit[sample.Item1] = MatchReferenceRegion(Path.Combine(output, sample.Item1 == "new-user" ? "native-new-user.png" : "screen-" + sample.Item1 + ".png"), sample.Item2, new Int32Rect(325, 166, 790, 541));
            File.WriteAllText(Path.Combine(output, "screen-pixel-audit.json"), JsonSerializer.Serialize(new { measurement = "Percentage of workspace pixels with exactly equal RGB values; includes differing live text and profile data. This is not a perceptual similarity score.", exactRgbPercent = screenAudit }, new JsonSerializerOptions { WriteIndented = true }));
            Descendants<System.Windows.Controls.Button>(window.PageContent).First(b=>b.Content?.ToString()=="▸ User Support and Manual").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(Descendants<System.Windows.Controls.Image>(window.PageContent).Any(i=>ReferenceEquals(i.Source,MainWindow.AssetBitmap("ergo_deskchair.gif"))),"Learning guide uses the authored ergonomic illustration rather than a raw legacy bitmap");
            window.Capture(Path.Combine(output,"native-learning-guide.png"));window.ShowPage("About");
            window.StartMeter(); window.ShowPage("Typing Meter"); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-meter-training.png")); window.StopMeter(false); checks.Add("Meter widget, tray, training screen and stop lifecycle");
            foreach (var mode in TypingGameEngine.Names) { window.StartGame(mode); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "game-" + mode.Replace(" ", "-") + ".png")); checks.Add("Game canvas renders: " + mode); }
            window.ShowPage("Studying"); var intro = courses.First(c => c.File == "uk7qwert.cnt").Chapters.SelectMany(c => c.Lessons).First(l => l.Kind == "Introduction"); window.ShowLesson(intro); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-introduction.png"));
            window.StartExercise("hello world", "Native scoring test", 60);
            window.Current!.Sound = false;
            window.TypingBox!.Text = "hxllo";
            window.TypingBox.Text = "hello";
            var result = window.FinishExercise(true);
            Check(result?.Correct == 5, "Native TextBox input scored");
            Check(result?.Accuracy == 83 && result.Errors == 1, "Editing a middle character preserves keystroke accuracy");
            Check(result?.Completed == false, "Early finish is marked partial");
            Check(result!.HasTextSnapshot && result.OriginalText == "hello world" && result.TypedText == "hello" && result.PlannedSeconds == 60 && result.StartedAt != default && result.Date >= result.StartedAt, "Saved attempt retains original text, typed text, planned duration and start time");
            bool rejected = false; try { ReportFactory.Certificate(window.Current!, result!); } catch (InvalidOperationException) { rejected = true; } Check(rejected, "Certificates reject partial tests");
            var completed = new TypingResult { Title = "Completed test", Kind = "Test", Completed = true, Accuracy = 98, NetWpm = 45, Seconds = 300 };
            var certificate = ReportFactory.Certificate(window.Current!, completed); Check(certificate.Blocks.Count > 5, "Completed tests produce certificates");
            RenderReport(certificate, Path.Combine(output, "certificate.png")); RenderReport(ReportFactory.Assessment(window.Current!), Path.Combine(output, "assessment.png")); checks.Add("Certificate and assessment pages paginate and render");
            var preview = new ReferenceReportWindow(ReportFactory.Assessment(window.Current!)) { ShowInTaskbar = false };
            preview.Show(); preview.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            var previewImage = new RenderTargetBitmap((int)preview.ActualWidth, (int)preview.ActualHeight, 96, 96, PixelFormats.Pbgra32); previewImage.Render(preview);
            var previewEncoder = new PngBitmapEncoder(); previewEncoder.Frames.Add(BitmapFrame.Create(previewImage)); using (var previewFile = File.Create(Path.Combine(output, "native-report-preview.png"))) previewEncoder.Save(previewFile);
            Check(preview.ActualWidth == 1440 && preview.ActualHeight == 852, "Reference report preview renders with native toolbar"); preview.Close();
            Check(window.Current!.Results.Count == 1, "Native exercise saves result");
            window.Current.Dark = true; store.Save(); window.ShowPage("Settings");
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            window.Capture(Path.Combine(output, "native-dark.png"));
            Check(new ProfileStore(store.DirectoryPath).Profiles.Single().Results.Count == 1, "Result survives disk reload");
            Check(new ProfileStore(store.DirectoryPath).Profiles.Single().Dark, "Settings survive disk reload");
            Check(File.Exists(Path.Combine(store.DirectoryPath, "profiles.json.bak")), "Backup retained");
            window.Current.Dark = false; window.ShowPage("Studying");
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            window.Capture(Path.Combine(output, "native-courses.png"));
            window.StartExercise(Assets.Read(tests.First(t => t.Name.StartsWith("10 Min.") && t.Name.Contains("English")).File), "Typing Test", 300, "Test");
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            window.Capture(Path.Combine(output, "native-practice.png"));
            string numbers = Assets.Read(tests.First(t => t.File.EndsWith("znumbers1.exm")).File);
            string normalizedNumbers = numbers.Replace("\r\n", "\n").Trim();
            window.StartExercise(numbers, "Numpad viewport check", 300, "Test");
            window.Capture(Path.Combine(output, "native-numbers.png"));
            var numberText = Descendants<System.Windows.Controls.TextBlock>(window.PageContent).First(t => new TextRange(t.ContentStart, t.ContentEnd).Text.StartsWith("1622"));
            var numberRuns = numberText.Inlines.OfType<Run>().ToList();
            double firstRow = numberRuns[0].ContentStart.GetCharacterRect(LogicalDirection.Forward).Top;
            double secondRow = numberText.Inlines.OfType<LineBreak>().First().ElementEnd.GetCharacterRect(LogicalDirection.Forward).Top;
            Check(Math.Abs(secondRow - firstRow - 25) < 1, "Numeric rows match the reference's compact 25-pixel spacing despite trailing blank lines");
            var countdown = Descendants<System.Windows.Shapes.Path>(window.PageContent).Single(p => p.Fill is SolidColorBrush brush && brush.Color == Color.FromRgb(155, 193, 85));
            Check(countdown.Data.IsEmpty(), "Countdown pie begins empty before the first keystroke");
            window.UpdateCountdownPie(.25); Check(Math.Abs(countdown.Data.GetArea() / (Math.PI * 61 * 33.5) - .25) < .01, "Countdown pie fills one quarter after one quarter of the duration");
            window.UpdateCountdownPie(.78); Check(Math.Abs(countdown.Data.GetArea() / (Math.PI * 61 * 33.5) - .78) < .01, "Countdown pie fills clockwise past halfway as in the running reference");
            Check(!Descendants<System.Windows.Controls.TextBlock>(window.PageContent).Any(t => t.IsVisible && t.Text.Contains("WPM")), "Test timer retains the reference pie without overlapping speed labels");
            for (int position = 0; position < normalizedNumbers.Length; position++)
            {
                if (position > 0 && normalizedNumbers[position - 1] != '\n' && position != normalizedNumbers.Length - 1) continue;
                window.TypingBox!.Text = normalizedNumbers[..position]; window.TypingBox.Select(window.TypingBox.Text.Length, 0);
                Check(window.ActiveTargetBounds.Top >= 0 && window.ActiveTargetBounds.Bottom <= 225, "Next numeric character remains visible at position " + position);
            }
            int eighteenthNumber = normalizedNumbers.IndexOf("8671155", StringComparison.Ordinal);
            window.TypingBox!.Text = normalizedNumbers[..eighteenthNumber];
            window.TypingBox.Select(window.TypingBox.Text.Length, 0); window.TypingBox.ScrollToEnd();
            Check(new TextRange(numberText.ContentStart, numberText.ContentEnd).Text.StartsWith("593522") && new TextRange(numberText.ContentStart, numberText.ContentEnd).Text.Contains("8671155") && !new TextRange(numberText.ContentStart, numberText.ContentEnd).Text.Contains("…"), "Numeric page two retains rows ten through eighteen without partial numbers or ellipses");
            window.TypingBox.Select(0, 0); Check(new TextRange(numberText.ContentStart, numberText.ContentEnd).Text.StartsWith("1622"), "Moving the caret back reveals the earlier numeric page for corrections");
            window.TypingBox.Select(window.TypingBox.Text.Length, 0); Check(new TextRange(numberText.ContentStart, numberText.ContentEnd).Text.StartsWith("593522"), "Returning the caret to the end restores the active numeric page");
            window.UpdateCountdownPie(.78);
            window.Capture(Path.Combine(output, "native-numbers-running.png"));
            window.TypingBox.Text = normalizedNumbers[..^1];
            window.Capture(Path.Combine(output, "native-numbers-scrolled.png"));
            window.TypingBox!.Text = normalizedNumbers;
            Check(window.CompletionWaiting && window.Current!.Results.Last().Correct == normalizedNumbers.Length, "All numeric rows can be completed without losing the remaining passage");
            Check(Math.Abs(countdown.Data.GetArea() / (Math.PI * 61 * 33.5) - 1) < .01, "Countdown pie is completely filled on completion");
            window.StartExercise("one\n\ntwo", "Paragraph input check", 60, "Test"); window.TypingBox!.Text = "one\r\ntwo"; Check(window.Current!.Results.Last().Correct == 7 && window.Current.Results.Last().Completed, "Native paragraph input matches a single Enter between paragraphs");
            Check(window.Current.Results.Last().OriginalText == "one\n\ntwo" && window.Current.Results.Last().ExpectedText == "one\ntwo", "Original paragraph spacing is archived separately from comparison text");
            Check(window.CompletionWaiting, "Completed test waits for Enter or Next before showing results"); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-test-completed.png")); int resultCount = window.Current.Results.Count; window.ContinueCompleted(); Check(!window.CompletionWaiting && window.Current.Results.Count == resultCount, "Completion continues without saving the same result twice");
            window.ShowResult(new TypingResult { Title = "Numpad 10-key test, numbers only", Kind = "Test", Seconds = 60, PlannedSeconds = 60, GrossWpm = 30, NetWpm = 30, Accuracy = 100, Completed = true }); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-test-results.png"));
            var failedTest = new TypingResult { Title = "Sample failed assessment", Kind = "Test", Seconds = 60, PlannedSeconds = 60, GrossWpm = 28, NetWpm = 21, Accuracy = 75, RequiredAccuracy = 95, RequiredWpm = 35, Completed = true };
            Check(!failedTest.Passed && failedTest.Completed, "Completed and passed are independent test states"); window.ShowResult(failedTest); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-failed-results.png"));
            Check(ReportFactory.Certificate(window.Current, failedTest).Blocks.Count > 0, "Completed failed tests retain their actual scores on a completion certificate");
            window.ShowPage("Typing Test"); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            foreach (var action in new[] { ("View & Print Results", "Typing Test completed"), ("View & Print Certificate", "TypeMaster Test Certificate, "), ("View & Print Assessment Report", "TYPING TEST REPORT") })
            {
                var button = Descendants<System.Windows.Controls.Button>(window.PageContent).First(b => b.Content?.ToString() == action.Item1); string? opened = null;
                window.Dispatcher.BeginInvoke(() => { var report = Application.Current.Windows.OfType<ReferenceReportWindow>().FirstOrDefault(); if (report != null) { opened = report.ReportTitle; report.Close(); } }, DispatcherPriority.ContextIdle);
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); Check(opened == action.Item2, action.Item1 + " opens its own selected-test report");
            }
            window.ShowPage("Statistics"); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); string? progressTitle = null;
            window.Dispatcher.BeginInvoke(() => { var report = Application.Current.Windows.OfType<ReferenceReportWindow>().FirstOrDefault(); if (report != null) { progressTitle = report.ReportTitle; report.Close(); } }, DispatcherPriority.ContextIdle);
            Descendants<System.Windows.Controls.Button>(window.PageContent).First(b => b.Content?.ToString() == "▸ Progress Report").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); Check(progressTitle == "Complete Report", "Progress Report opens the current profile's complete report");
            window.ShowPage("Studying"); window.OpenCourse(courses.First(c => c.File == "uk7qwert.cnt")); window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-course-overview.png"));
            window.StartExercise("f d f d", "Keyboard drill test", 120, "ReviewKeys"); window.TypingBox!.Text = "f"; window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render); window.Capture(Path.Combine(output, "native-key-drill.png")); Check(window.FinishExercise(true)?.Correct == 1, "Native keyboard drill accepts and scores keyboard input");
            string importDirectory = Path.Combine(store.DirectoryPath, "import-checks"); Directory.CreateDirectory(importDirectory);
            string html = Path.Combine(importDirectory, "sample.html"); File.WriteAllText(html, "<p>One &amp; two</p><script>bad()</script><p>Three</p>"); Check(TestImport.Read(html).Contains("One & two") && !TestImport.Read(html).Contains("bad()"), "HTML imports decode text and omit scripts");
            string word = Path.Combine(importDirectory, "sample.docx"); using (var zip = System.IO.Compression.ZipFile.Open(word, System.IO.Compression.ZipArchiveMode.Create)) { using var writer = new StreamWriter(zip.CreateEntry("word/document.xml").Open()); writer.Write("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>First paragraph</w:t></w:r></w:p><w:p><w:r><w:t>नमस्ते</w:t></w:r></w:p></w:body></w:document>"); } Check(TestImport.Read(word) == "First paragraph\n\nनमस्ते", "Word imports preserve paragraphs and Unicode text");
            string pack = Path.Combine(importDirectory, "pack.zip"); using (var zip = System.IO.Compression.ZipFile.Open(pack, System.IO.Compression.ZipArchiveMode.Create)) { using var writer = new StreamWriter(zip.CreateEntry("../../outside.txt").Open()); writer.Write("Safe imported passage"); }
            string imported = Path.Combine(importDirectory, "texts"); Check(TestImport.Pack(pack, imported) == 1 && Directory.GetFiles(imported).Length == 1 && !File.Exists(Path.Combine(store.DirectoryPath, "outside.txt")), "ZIP import stores safe basenames without extracting archive paths");
            var legacy = new TypingResult { Title = tests.First(t => t.File.EndsWith("india.exm")).Name, Kind = "Test", Completed = true, Accuracy = 75, NetWpm = 21 }; window.Current.Results.Add(legacy); window.EnterProfile(window.Current.Name);
            Check(legacy.CriteriaRecorded && legacy.RequiredAccuracy == 95 && legacy.RequiredWpm == 35 && !legacy.Passed && legacy.Accuracy == 75 && legacy.NetWpm == 21, "Legacy pass criteria load from supplied metadata without recalculating historical scores"); window.Current!.Results.Remove(legacy); store.Save();
            var oldSheet = ReportFactory.Examination(window.Current, legacy); string oldText = new TextRange(oldSheet.ContentStart, oldSheet.ContentEnd).Text; Check(oldText.Contains("Not recorded") && !oldText.Contains(legacy.Date.ToString("dd-MM-yyyy hh:mm tt")), "Legacy examination sheets do not invent an exam start time from the completion timestamp");
            VerifyCourseScreens(window, output, Check);
            VerifyDisplayAndRecovery(window, output, Check);
            TypingPerformance.Verify(output, Check);
            VerifyReportBranding(Check);
            ReportSamples.Write(Path.Combine(output, "reports")); Check(Directory.GetFiles(Path.Combine(output, "reports"), "*.pdf").Length == 5, "All five sample PDF reports export directly without a PDF printer");
            VerifyVectorPdf(output, Check);
            VerifyCandidateSkills(output,Check);
            VerifyCompanyBranding(output,Check);
            AppUpdateTests.Verify(output,Check);
            window.Close();
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new { passed = true, checks, texts = tests.Count, courses = courses.Count, activities = courses.Sum(c => c.Chapters.Sum(ch => ch.Lessons.Count)), missingSourceReferences = courses.Sum(c => c.MissingEvents) }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new { passed = false, checks, error = ex.ToString() }, new JsonSerializerOptions { WriteIndented = true })); return 1; }
    }
    static void VerifyCourseScreens(MainWindow window, string output, Action<bool,string> check)
    {
        var courses=CourseLibrary.LoadAll();var universal=courses.Single(c=>c.File=="1universal.cnt");var first=universal.Chapters[0];
        check(first.Groups.Count==6&&first.Groups[0].Events.Count==3,"Universal chapter preserves six exercise groups with three introduction events");
        check(courses.All(c=>c.Chapters.All(ch=>ch.Groups.SelectMany(g=>g.Events).SequenceEqual(ch.Lessons))),"Grouping preserves the order and identity of every existing course event");
        var profile=new Profile();profile.Completed.Add(first.Groups[0].Events[0].Id);check(!first.Groups[0].IsComplete(profile),"A partial introduction cannot mark its whole exercise group complete");
        var layout=new KeyboardLayout{HomeKeys="üö",Keys=[new KeyShape("ü","Ü",4,0,0,32,34),new KeyShape("ö","Ö",5,32,0,32,34)]};
        string generated=CourseLibrary.Generate("PHYSGEN:fingers:4,5","asdfjkl;",layout);check(generated.Any(c=>c=='ü')&&generated.All(c=>"üö ".Contains(c)),"Physical finger drills use the selected layout rather than US characters");
        var german=courses.First(c=>c.File=="ge7qwert.cnt");check(MainWindow.MissingCourseKeys(german,KeyboardLayout.Load(Path.Combine(Assets.Root,"keyboards","us.kbd"))).Contains('ü'),"German course detects keys unavailable on an English US layout");
        check(MainWindow.MissingCourseKeys(universal,layout)=="","Universal layout-independent course avoids an inappropriate US-keyboard warning");
        check(MainWindow.MatchesLanguage("fre","fr")&&!MainWindow.MatchesLanguage("eng","ger"),"Test filtering respects the actual language and known aliases");
        string Label(System.Windows.Controls.Button b)=>b.Content is System.Windows.Controls.TextBlock text?text.Text:b.Content?.ToString()??"";
        System.Windows.Controls.Button Find(string name)=>Descendants<System.Windows.Controls.Button>(window.PageContent).First(b=>Label(b)==name);
        bool Has(string value)=>Descendants<System.Windows.Controls.TextBlock>(window.PageContent).Any(t=>t.Text.Contains(value,StringComparison.Ordinal));
        window.Current!.Language="eng";Localization.Language="eng";window.Current.Keyboard="us.kbd";
        foreach(var e in first.Groups[0].Events)window.Current.Completed.Remove(e.Id);
        window.Current.Completed.Add(first.Groups[0].Events[0].Id);
        window.BeginGroup(universal,0,0);window.UpdateLayout();check(Has("Bumps"),"Continuing a partial group resumes the first unfinished step using preserved event IDs");window.Capture(Path.Combine(output,"native-course-bumps.png"));
        Find("Next").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));window.UpdateLayout();check(Has("Now try typing")&&!Find("Next").IsEnabled,"Course home-row introduction requires the highlighted key sequence");
        window.Capture(Path.Combine(output,"native-course-key-introduction-start.png"));
        var input=Descendants<System.Windows.Controls.TextBox>(window.PageContent).Single();input.Text="z";check(!Find("Next").IsEnabled,"An unrelated key does not complete the finger-placement prompt");
        foreach(char key in KeyboardLayout.Load(Path.Combine(Assets.Root,"keyboards","us.kbd")).HomeKeys)input.Text=key.ToString();check(Find("Next").IsEnabled,"All home-row keys enable continuation");window.Capture(Path.Combine(output,"native-course-key-introduction.png"));
        Find("Next").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));window.UpdateLayout();check(first.Groups[0].IsComplete(window.Current)&&Has("1.6"),"Completed introduction returns to its six-row chapter page with grouped progress");window.Capture(Path.Combine(output,"native-course-chapter.png"));
        Find("2").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));check(Has("Lesson 2:"),"Numbered chapter tabs navigate to the selected chapter");
        window.OpenCourse(german);check(Has("Missing keys:"),"Incompatible course opens the keyboard warning before course navigation");window.Capture(Path.Combine(output,"native-keyboard-warning.png"));
        window.Current.Language="ger";Localization.Language="ger";window.ShowPage("Settings");check(Has("Einstellungen")&&Has("Sprache"),"German settings use supplied and matching reference interface translations");window.Capture(Path.Combine(output,"native-german-settings.png"));
        check(KeyboardLayout.DisplayName("german.kbd").Contains("German")&&KeyboardLayout.DisplayName("us.kbd")=="English US","Keyboard selector uses readable names from the supplied mapping");
        window.Current.Language="eng";Localization.Language="eng";window.Current.CourseFile=universal.File;
        var universalResult=new TypingResult{Title="Universal chart check",CourseFile=universal.File,Accuracy=91,Mistakes=new(){{'f',2}}};var germanResult=new TypingResult{Title="German chart check",CourseFile=german.File,Accuracy=99};window.Current.Results.Add(universalResult);window.Current.Results.Add(germanResult);window.ShowPage("Statistics");
        var courseChoice=Descendants<System.Windows.Controls.ComboBox>(window.PageContent).Single();courseChoice.SelectedItem=courseChoice.Items.OfType<CourseDefinition>().Single(c=>c.File==universal.File);
        Find("Accuracy").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));check(Has("Difficult Keys")&&Has("Accuracy Goal (94%)"),"Course-specific accuracy page includes difficult-key history and the course's 94% goal");window.Capture(Path.Combine(output,"native-course-accuracy.png"));
        var plotted=Descendants<System.Windows.Shapes.Ellipse>(window.PageContent).Where(e=>e.ToolTip is string).Select(e=>e.ToolTip.ToString()!).ToList();check(plotted.Any(s=>s.Contains("Universal chart check"))&&!plotted.Any(s=>s.Contains("German chart check")),"Selecting a statistics course excludes attempts from other courses");window.Current.Results.Remove(universalResult);window.Current.Results.Remove(germanResult);
        window.ShowPage("Studying");
        var extraHeader=Descendants<System.Windows.Controls.Button>(window.PageContent).First(b=>b.Content is System.Windows.Controls.Canvas body&&Descendants<System.Windows.Controls.TextBlock>(body).Any(t=>t.Text.StartsWith("Extra Courses")));
        extraHeader.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));var extraChoice=Descendants<System.Windows.Controls.ComboBox>(window.PageContent).Single(c=>c.Items.OfType<CourseDefinition>().Any());
        var selected=extraChoice.Items.OfType<CourseDefinition>().Last(c=>c.Description.Length>0);extraChoice.SelectedItem=selected;check(Has(selected.Description)&&Has(selected.Chapters.Count+" lessons"),"Extra-course selection updates description and lesson counts together");window.Capture(Path.Combine(output,"native-extra-course-details.png"));
    }
    static void VerifyDisplayAndRecovery(MainWindow window, string output, Action<bool, string> check)
    {
        foreach (var size in new[] { (640d,480d), (800d,600d), (853d,480d), (1024d,768d), (1280d,720d) })
        {
            window.Width = size.Item1; window.Height = size.Item2;
            foreach (string screen in new[] { "Studying", "Typing Test", "Custom Review", "Games", "Statistics", "Settings", "About" })
            {
                window.ShowPage(screen); window.UpdateLayout();
                var bounds = window.ReferenceShell.TransformToAncestor(window).TransformBounds(new Rect(window.ReferenceShell.RenderSize));
                check(bounds.Left >= 0 && bounds.Top >= 23 && bounds.Right <= window.ActualWidth + .1 && bounds.Bottom <= window.ActualHeight + .1, screen + " fits fully at " + size.Item1 + "x" + size.Item2);
            }
            if (size.Item1 == 640 || size.Item1 == 1280) window.Capture(Path.Combine(output, "display-" + size.Item1 + "x" + size.Item2 + ".png"));
            var universal = CourseLibrary.LoadAll().First(c => c.File == "1universal.cnt"); window.ShowChapter(universal,0);window.UpdateLayout();var chapterBounds=window.ReferenceShell.TransformToAncestor(window).TransformBounds(new Rect(window.ReferenceShell.RenderSize));check(chapterBounds.Left>=0&&chapterBounds.Bottom<=window.ActualHeight+.1&&chapterBounds.Right<=window.ActualWidth+.1,"New chapter screen fits at "+size.Item1+"x"+size.Item2);
        }
        window.Width = 1440; window.Height = 852;
        window.WindowState=WindowState.Maximized;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
        var work=System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle).WorkingArea;
        var iconTop=window.CaptionIcon.PointToScreen(new Point());var iconBottom=window.CaptionIcon.PointToScreen(new Point(window.CaptionIcon.ActualWidth,window.CaptionIcon.ActualHeight));
        check(iconTop.X>=work.Left&&iconTop.Y>=work.Top&&iconBottom.X<=work.Right&&iconBottom.Y<=work.Bottom,"Maximized title-bar icon stays fully inside the monitor work area");
        check(window.CaptionButtons.Children.OfType<System.Windows.Controls.Button>().All(b=>b.Content is System.Windows.Shapes.Path),"All caption buttons use vector icons instead of font symbols");
        check(System.Windows.Automation.AutomationProperties.GetName((System.Windows.Controls.Button)window.CaptionButtons.Children[1])=="Restore","Maximized caption exposes the Restore action");
        window.Capture(Path.Combine(output,"display-maximized-frame.png"));
        window.WindowState=WindowState.Normal;window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
        check(window.WindowSurface.Margin==new Thickness(0),"Restoring the window removes maximized frame compensation");
        string directory = Path.Combine(output, "recovery-" + Guid.NewGuid().ToString("N"));
        var store = new ProfileStore(directory); store.Profiles.Add(new Profile { Name = "Recovery candidate" }); store.Save(); store.Save();
        string profileFile = Path.Combine(directory, "profiles.json"); File.WriteAllText(profileFile, "{broken");
        var repaired = new ProfileStore(directory); check(repaired.Profiles.Single().Name == "Recovery candidate" && repaired.RecoveryNotice != null && Directory.GetFiles(directory, "*.damaged-*").Length == 1, "Damaged primary profiles recover automatically while preserving the damaged file");
        check(new ProfileStore(directory).Profiles.Count == 1, "Automatically recovered profiles survive another restart");
        string brokenDirectory = Path.Combine(directory, "both-broken"); Directory.CreateDirectory(brokenDirectory); File.WriteAllText(Path.Combine(brokenDirectory, "profiles.json"), "null"); File.WriteAllText(Path.Combine(brokenDirectory, "profiles.json.bak"), "null"); bool refused = false;
        try { _ = new ProfileStore(brokenDirectory); } catch (InvalidDataException) { refused = true; }
        check(refused && File.ReadAllText(Path.Combine(brokenDirectory, "profiles.json")) == "null", "Two damaged profile files are preserved instead of silently resetting user history");
        var first = new MainWindow(repaired) { ShowInTaskbar = false }; first.Show(); first.EnterProfile("Recovery candidate", false);
        first.StartExercise("12\n34\n56\n78", "Recovery check", 120, "Test"); first.TypingBox!.Text = "12\r\n54"; first.TypingBox.Select(2, 0); first.SaveExerciseDraft();
        string draftFile = Directory.GetFiles(directory, "exercise-*.json").Single(); var draft = JsonSerializer.Deserialize<ExerciseDraft>(File.ReadAllText(draftFile))!;
        draft.Elapsed = 37; draft.Caret = 2; File.WriteAllText(draftFile, JsonSerializer.Serialize(draft));
        first.ShowPage("Studying"); first.Close();
        var second = new MainWindow(new ProfileStore(directory)) { ShowInTaskbar = false }; second.Show(); second.EnterProfile("Recovery candidate", false);
        check(second.ResumeExerciseDraft() && second.TypingBox!.Text == "12\n54" && second.TypingBox.CaretIndex == 2, "Restart recovery restores actual candidate text and the saved cursor position");
        var recoveredResult = second.FinishExercise(true)!;
        check(recoveredResult.Seconds == 37 && recoveredResult.StartedAt == draft.Started && recoveredResult.Errors == draft.Errors && recoveredResult.AttemptId == draft.AttemptId, "Recovery preserves elapsed time, original start, errors and attempt identity without counting downtime");
        check(!File.Exists(draftFile) && !second.ResumeExerciseDraft(), "Finishing a recovered attempt clears recovery data and cannot save a duplicate");
        File.WriteAllText(draftFile, JsonSerializer.Serialize(draft)); check(!second.ResumeExerciseDraft() && !File.Exists(draftFile), "A crash after result saving cannot recover the same completed attempt twice");
        second.StartExercise("abcdefghi", "Overflow check", 120, "Test"); second.TypingBox!.Text = "abc"; second.TypingBox.Text = "abcdefghiEXTRA";
        check(second.Current!.Results.Last().TypedText == "abcdefghi" && second.TypingBox.Text == "abcdefghi", "Extra end characters cannot leave visible input different from the saved response");
        second.StartExercise("12\n34\n56\n78", "Correction check", 120, "Test"); second.TypingBox.Text = "12\r\n56\r\n"; second.TypingBox.Select(4, 2); second.TypingBox.SelectedText = "34";
        var corrected = second.FinishExercise(true)!; check(corrected.TypedText == "12\n34\n" && corrected.Errors > 0 && corrected.Correct == 6, "Skipped-number correction retains Enter boundaries and the original error history");
        second.StartExercise("longer passage", "Clear check", 120, "Test"); second.TypingBox.Text = "long"; second.TypingBox.Clear();
        check(!Directory.GetFiles(directory, "exercise-*.json").Any(), "Erasing the entire response removes stale recovery text"); second.ShowPage("Studying"); second.Close();
    }
    static void VerifyReportBranding(Action<bool,string> check)
    {
        check(ReportBranding.Company == "Nirmal Poly Plast Private Limited", "Company header uses the company name without version badges");
        var result = new TypingResult { Kind="Test",Title="Branding check",Completed=true,HasTextSnapshot=true,OriginalText="Sample passage",ExpectedText="Sample passage",TypedText="Sample passage",Seconds=60,PlannedSeconds=60 };
        var profile = new Profile { Name="Sample Candidate",Results=[result] }; byte[]? expected = null;
        foreach (var document in new[] { ReportFactory.Result(profile,result),ReportFactory.Certificate(profile,result),ReportFactory.Examination(profile,result),ReportFactory.Progress(profile) })
        {
            var paginator=ReportFactory.Paginator(document);paginator.ComputePageCount();var bitmap=new RenderTargetBitmap(794,1123,96,96,PixelFormats.Pbgra32);bitmap.Render(paginator.GetPage(0).Visual);
            var pixels=new byte[714*76*4];bitmap.CopyPixels(new Int32Rect(40,24,714,76),pixels,714*4,0);expected??=pixels;bool progress=(document.Tag as ReportHeading)?.CandidateName is not null;check(progress?!pixels.SequenceEqual(expected):pixels.SequenceEqual(expected),(progress?"Approved progress masthead differs from legacy report headers on ":"Shared company header is identical on ")+(document.Tag as ReportHeading)?.Title);
            int RedOrBlue(Int32Rect rect,bool red){var data=new byte[rect.Width*rect.Height*4];bitmap.CopyPixels(rect,data,rect.Width*4,0);int count=0;for(int i=0;i<data.Length;i+=4)if(red?data[i+2]>200&&data[i+1]<100&&data[i]<100:data[i]>130&&data[i+1]>50&&data[i+2]<70)count++;return count;}
            check(RedOrBlue(progress?new Int32Rect(40,31,102,29):new Int32Rect(50,34,90,26),true)>200&&RedOrBlue(progress?new Int32Rect(40,64,102,29):new Int32Rect(50,64,90,26),false)>200,"Both original brand logos render visibly on "+(document.Tag as ReportHeading)?.Title);
        }
        result.TypedText=string.Join("\n",Enumerable.Range(1,70).Select(n=>n.ToString("D8")));result.OriginalText=result.ExpectedText=result.TypedText;var multi=ReportFactory.Paginator(ReportFactory.Result(profile,result));multi.ComputePageCount();check(multi.PageCount>1,"Branded numeric report still paginates");
        for(int page=1;page<multi.PageCount;page++){var bitmap=new RenderTargetBitmap(794,1123,96,96,PixelFormats.Pbgra32);bitmap.Render(multi.GetPage(page).Visual);var pixels=new byte[714*76*4];bitmap.CopyPixels(new Int32Rect(40,24,714,76),pixels,714*4,0);check(pixels.SequenceEqual(expected!),"Company header repeats unchanged on continuation page "+(page+1));}
    }
    static void VerifyVectorPdf(string output, Action<bool,string> check)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(output,"reports"),"*.pdf"))
        {
            string pdf=System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(file));
            check(pdf.Contains("/ToUnicode") && pdf.Contains("/FontFile2") && !pdf.Contains("/PageImage"), "Report embeds selectable Unicode fonts rather than page images: "+Path.GetFileName(file));
        }
        var document=new FlowDocument { PageWidth=794,PageHeight=1123,PagePadding=new Thickness(66,120,48,60),ColumnWidth=680,FontFamily=new FontFamily("Times New Roman"),FontSize=16 };
        foreach(string text in new[]{"Copy check: café – résumé € 1234567890", "हिन्दी टाइपिंग परीक्षा", "العربية", "中文 日本語"}) document.Blocks.Add(new Paragraph(new Run(text)));
        NativePdf.Export(document,Path.Combine(output,"unicode-copy-check.pdf"));
        RenderReport(document,Path.Combine(output,"unicode-copy-check.png"));
        check(File.Exists(Path.Combine(output,"unicode-copy-check.pdf")),"Multilingual PDF exports embedded font text and complex glyphs");
        var result=new TypingResult { Kind="Test",Title="Export comparison",Completed=true,HasTextSnapshot=true,OriginalText="Sample passage",ExpectedText="Sample passage",TypedText="Sample passage",Seconds=60,PlannedSeconds=60 };
        var profile=new Profile {Name="Sample Candidate",Results=[result]};
        foreach(var pair in new[]{("result",ReportFactory.Result(profile,result)),("certificate",ReportFactory.Certificate(profile,result)),("examination",ReportFactory.Examination(profile,result)),("progress",ReportFactory.Progress(profile))})
        {
            NativePdf.Export(pair.Item2,Path.Combine(output,"vector-"+pair.Item1+".pdf"));
            RenderReport(pair.Item2,Path.Combine(output,"vector-"+pair.Item1+".png"));
        }
    }
    static void VerifyCandidateSkills(string output,Action<bool,string> check)
    {
        var store=new ProfileStore(Path.Combine(output,"skills-"+Guid.NewGuid().ToString("N")));var candidate=new Profile{Name="Skills candidate"};var other=new Profile{Name="Other candidate"};store.Profiles.Add(candidate);store.Profiles.Add(other);store.Save();
        var editor=new CandidateSkillsWindow(candidate,store.Save);editor.SetSkill("SUM",true);editor.SetSkill("PivotTable creation",true);editor.SetRemarks("Good Excel knowledge.\nNeeds practice in Word.");editor.SetExaminer("  Asha Rao  ");editor.Apply();editor.Close();
        var loaded=new ProfileStore(store.DirectoryPath);var saved=loaded.Profiles[0];
        check(saved.TechnicalSkills.SetEquals(new[]{"SUM","PivotTable creation"})&&saved.AssessmentRemarks=="Good Excel knowledge.\nNeeds practice in Word.","Candidate skills and multiline remarks persist after restart");
        check(loaded.Profiles[1].TechnicalSkills.Count==0&&loaded.Profiles[1].AssessmentRemarks=="","Skills and remarks remain specific to one candidate");
        check(saved.TestTakenBy=="Asha Rao"&&loaded.Profiles[1].TestTakenBy=="","Test taken by is saved per candidate and survives restart");
        var cancelled=new CandidateSkillsWindow(candidate,store.Save);cancelled.SetSkill("IF",true);cancelled.SetRemarks("Unsaved remarks");cancelled.Close();check(!candidate.TechnicalSkills.Contains("IF")&&candidate.AssessmentRemarks.StartsWith("Good Excel"),"Cancelling skills editing leaves saved assessment unchanged");
        var document=ReportFactory.Progress(candidate);check(document.Blocks.OfType<Table>().Any(table=>table.Columns.Count==3)&&!new TextRange(document.ContentStart,document.ContentEnd).Text.Contains("CANDIDATE ASSESSMENT"),"Approved skills report uses three columns and leaves the unused assessment label blank");string text=new TextRange(document.ContentStart,document.ContentEnd).Text;
        var identityValues=((Grid)((Border)((Grid)((BlockUIContainer)document.Blocks.FirstBlock).Child).Children[0]).Child).Children.OfType<TextBlock>().Select(t=>t.Text).ToList();check(identityValues.Contains("CANDIDATE NAME")&&identityValues.Contains(candidate.Name)&&!identityValues.Contains(candidate.Name.ToUpperInvariant()),"Progress report clearly labels the candidate and preserves the entered name casing");
        check(text.IndexOf("Technical Skills")>text.IndexOf("Typing Tests")&&text.IndexOf("Remarks")>text.IndexOf("Technical Skills"),"Selected skills and remarks appear below the typing test results");
        check(text.Contains("SUM")&&text.Contains("PivotTable Creation")&&!text.Contains("XLOOKUP"),"Progress report includes only ticked technical skills");
        check(text.Contains("Excel  (2)")&&!text.Contains("Excel data and analysis")&&!text.Contains("Excel math and trigonometry"),"Excel formulas and practical skills share one compact Excel section");
        check(((ReportHeading)document.Tag).TestTakenBy=="Asha Rao"&&document.PagePadding.Bottom>=100,"Progress report reserves footer space for the examiner name");
        check(TechnicalSkills.DisplayName("Data entry")=="Data Entry"&&TechnicalSkills.DisplayName("PivotTable creation")=="PivotTable Creation"&&TechnicalSkills.DisplayName("SUM")=="SUM"&&TechnicalSkills.DisplayName("Export to PDF")=="Export To PDF","Skill labels use consistent title case while preserving official function names and acronyms");
        var mixed=new Profile{Name="Mixed application skills",TechnicalSkills=["SUM","Document formatting","Create and format slides","Microsoft Outlook"]};var appGroups=TechnicalSkills.ReportGroups(mixed).ToDictionary(g=>g.Group,g=>g.Items);check(appGroups["Excel"].SequenceEqual(new[]{"SUM"})&&appGroups["Word"].Contains("Document formatting")&&appGroups["PowerPoint"].Contains("Create and format slides")&&appGroups["Outlook"].Contains("Microsoft Outlook"),"Excel, Word, PowerPoint and Outlook skills stay under their own application headings");
        var older=new TypingResult{Kind="Test",Title="Older attempt",Date=new DateTime(2026,1,1),Completed=true};var newest=new TypingResult{Kind="Test",Title="Latest attempt",Date=new DateTime(2026,2,1),NetWpm=42,Accuracy=97,Completed=true};mixed.Results=[newest,older,new TypingResult{Kind="Course",Date=new DateTime(2026,3,1)}];var compactDocument=ReportFactory.Progress(mixed);check(compactDocument.Blocks.OfType<Table>().Where(table=>table.RowGroups[0].Rows.Count==1).All(table=>table.RowGroups[0].Rows[0].Cells.Count==1&&table.RowGroups[0].Rows[0].Cells[0].ColumnSpan==3),"Single-skill application rows span all three report columns");string compactText=new TextRange(compactDocument.ContentStart,compactDocument.ContentEnd).Text;check(!compactText.Contains("Latest Typing Result")&&compactText.Contains("42 wpm")&&compactText.Contains("97%")&&compactText.Split("Latest attempt").Length==2&&compactText.Split("Older attempt").Length==2,"Compact typing history includes every attempt once and retains latest metrics without a highlighted card");
        check(((ReportHeading)compactDocument.Tag).AssessmentDate==new DateTime(2026,2,1),"Assessment date comes from the latest typing test rather than later course activity or export time");
        NativePdf.Export(document,Path.Combine(output,"candidate-skills-report.pdf"));RenderReport(document,Path.Combine(output,"candidate-skills-report.png"));
        var editAgain=new CandidateSkillsWindow(candidate,store.Save);editAgain.SetSkill("SUM",false);editAgain.SetRemarks("");editAgain.Apply();editAgain.Close();
        check(!new ProfileStore(store.DirectoryPath).Profiles[0].TechnicalSkills.Contains("SUM")&&candidate.AssessmentRemarks=="","Skills can be unticked and remarks cleared");
        check(TechnicalSkills.FunctionCount==521&&TechnicalSkills.Groups.SelectMany(g=>g.Items).Contains("PIVOTBY"),"The bundled Microsoft catalogue includes 521 Excel function names, including advanced functions");
        var expanded=new CandidateSkillsWindow(candidate,store.Save);expanded.SetFilter("XLOOK");check(expanded.MatchingSkills.SequenceEqual(new[]{"XLOOKUP"}),"Function search finds matching names without losing selections");
        expanded.SetFilter("","Excel financial");check(expanded.MatchingSkills.Contains("PMT")&&!expanded.MatchingSkills.Contains("SUM"),"Category filtering separates financial functions");
        expanded.AddCustomSkill("  GST   reconciliation  ");expanded.AddCustomSkill("gst reconciliation");expanded.AddCustomSkill("sum");expanded.Apply();expanded.Close();
        var customSaved=new ProfileStore(store.DirectoryPath).Profiles[0];check(customSaved.CustomTechnicalSkills.SetEquals(new[]{"GST reconciliation"})&&customSaved.TechnicalSkills.Contains("GST reconciliation")&&customSaved.TechnicalSkills.Contains("SUM"),"Custom skills persist, normalise whitespace and avoid case-insensitive duplicates with standard functions");
        var reused=new CandidateSkillsWindow(other,store.Save,candidate.CustomTechnicalSkills);reused.SetFilter("GST reconciliation");check(reused.MatchingSkills.Length==1&&other.TechnicalSkills.Count==0,"Custom names can be reused for another candidate without copying assessed selections");reused.Close();
        var legacy=new Profile{Name="Existing assessment",TechnicalSkills=["INDEX / MATCH"]};var legacyEditor=new CandidateSkillsWindow(legacy,()=>{});legacyEditor.Apply();legacyEditor.Close();check(legacy.TechnicalSkills.Contains("INDEX / MATCH"),"Previously saved combined formula selections remain intact");
        var customDocument=ReportFactory.Progress(candidate);string customText=new TextRange(customDocument.ContentStart,customDocument.ContentEnd).Text;check(customText.Contains("Other Skills")&&customText.Contains("GST Reconciliation"),"Custom skill names appear in the formatted progress report");NativePdf.Export(customDocument,Path.Combine(output,"custom-skills-report.pdf"));
        var classify=new CandidateSkillsWindow(candidate,store.Save);classify.AddCustomSkill("GST reconciliation","Excel");classify.AddCustomSkill("Letter formatting","Word");classify.Apply();classify.Close();var classified=new ProfileStore(store.DirectoryPath).Profiles[0];check(TechnicalSkills.ApplicationFor("GST reconciliation",classified)=="Excel"&&TechnicalSkills.ApplicationFor("Letter formatting",classified)=="Word","Custom skill application assignments persist and print under the correct application");NativePdf.Export(ReportFactory.Progress(classified),Path.Combine(output,"classified-skills-report.pdf"));
        var captured=new CandidateSkillsWindow(candidate,()=>{});captured.SetFilter("","Excel advanced / dynamic arrays");captured.Show();captured.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);captured.UpdateLayout();var bitmap=new RenderTargetBitmap((int)captured.ActualWidth,(int)captured.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(captured);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(Path.Combine(output,"skills-catalogue.png")))encoder.Save(stream);captured.Close();
        var longCandidate=new Profile{Name="Long assessment",TestTakenBy="Sample Interviewer",TechnicalSkills=TechnicalSkills.Groups.Where(g=>g.Group.StartsWith("Excel ")).SelectMany(g=>g.Items).Distinct().Take(100).ToHashSet()};var multi=ReportFactory.Progress(longCandidate);var paginator=ReportFactory.Paginator(multi);paginator.ComputePageCount();check(paginator.PageCount>1,"Large Excel checklists paginate with reserved examiner footer space");NativePdf.Export(paginator,Path.Combine(output,"progress-footer-multipage.pdf"));
        var longRemarks=new Profile{Name="Long remarks candidate",TestTakenBy="Sample Interviewer",AssessmentRemarks=string.Join("\n",Enumerable.Range(1,90).Select(i=>"Observation "+i+": Candidate independently completed the assigned spreadsheet task and explained the calculation clearly."))};
        var remarksDocument=ReportFactory.Progress(longRemarks);var remarksPaginator=ReportFactory.Paginator(remarksDocument);remarksPaginator.ComputePageCount();check(remarksPaginator.PageCount>1&&new TextRange(remarksDocument.ContentStart,remarksDocument.ContentEnd).Text.Contains("Observation 90:"),"Long assessment remarks flow across pages and retain the final observation");NativePdf.Export(remarksPaginator,Path.Combine(output,"progress-long-remarks.pdf"));
    }
    static void VerifyCompanyBranding(string output,Action<bool,string> check)
    {
        string directory=Path.Combine(output,"branding-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        check(BrandingStorage.Load(directory).Name==ReportBranding.DefaultCompany,"New installations retain the current company branding by default");
        string source=Path.Combine(directory,"logo.png");File.Copy(Path.Combine(Assets.Root,"Branding","ratan.png"),source);string imported=BrandingStorage.Import(source);File.Delete(source);
        var custom=new CompanyBranding{Name="Example Company Limited",FirstLogo=imported,SecondLogo=""};BrandingStorage.Save(directory,custom);var saved=BrandingStorage.Load(directory);
        check(saved.Name==custom.Name&&saved.FirstLogo==imported&&saved.SecondLogo==""&&BrandingStorage.Image(saved.FirstLogo).PixelWidth>0,"Company name and imported logo survive restart without the original image file");
        bool rejected=false;try{BrandingStorage.Save(directory,new(){Name=" "});}catch(InvalidDataException){rejected=true;}check(rejected&&BrandingStorage.Load(directory).Name==custom.Name,"Invalid company names do not overwrite saved branding");
        ReportBranding.Configure(directory);
        try
        {
            var candidate=new Profile{Name="Branding candidate"};var result=new TypingResult{Kind="Test",Completed=true,HasTextSnapshot=true,TypedText="sample",ExpectedText="sample",OriginalText="sample"};candidate.Results.Add(result);
            foreach(var entry in new[]{("result",ReportFactory.Result(candidate,result)),("certificate",ReportFactory.Certificate(candidate,result)),("examination",ReportFactory.Examination(candidate,result)),("progress",ReportFactory.Progress(candidate))})
            {
                NativePdf.Export(entry.Item2,Path.Combine(output,"custom-branding-"+entry.Item1+".pdf"));
            }
            check(ReportBranding.Company==custom.Name,"All report types use the saved company configuration");
            ReportBranding.Save(directory,new());check(ReportBranding.Company==ReportBranding.DefaultCompany&&ReportBranding.Current.FirstLogo==null,"Restore defaults brings back the current company and original logos");
            File.WriteAllText(Path.Combine(directory,"company-branding.json"),"{broken");check(BrandingStorage.Load(directory).Name==custom.Name,"Damaged company settings recover from the previous backup");
        }
        finally{ReportBranding.Configure(Path.Combine(output,"branding-defaults"));}
    }
    static void VerifyOriginalArtwork(Action<bool,string> check)
    {
        string directory=Path.Combine(Assets.Root,"TypeMasterArtwork");
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"manifest.json")));
        var legacy=manifest.RootElement.GetProperty("legacy").EnumerateObject().ToArray();
        check(legacy.Length==92,"All 92 legacy image canvases have original TypeMaster replacements");
        foreach(var entry in legacy)
        {
            using var input=File.OpenRead(Path.Combine(Assets.Root,entry.Name));
            var old=BitmapDecoder.Create(input,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
            var current=MainWindow.AssetBitmap(entry.Name);
            check(current.PixelWidth==old.PixelWidth&&current.PixelHeight==old.PixelHeight,"Original artwork preserves exact pixel canvas: "+entry.Name);
            check(RuntimeArtwork.Resolve(entry.Name).StartsWith("TypeMasterArtwork/",StringComparison.Ordinal),"Runtime selects authored replacement: "+entry.Name);
        }
        foreach(var entry in manifest.RootElement.GetProperty("extras").EnumerateObject())
        {
            var spec=entry.Value;var bitmap=MainWindow.AssetBitmap("TypeMasterArtwork/"+spec.GetProperty("file").GetString());
            check(bitmap.PixelWidth==spec.GetProperty("width").GetInt32()&&bitmap.PixelHeight==spec.GetProperty("height").GetInt32(),"Standalone illustration retains its layout footprint: "+entry.Name);
        }
        check(RuntimeArtwork.FingerCue(0).X<RuntimeArtwork.FingerCue(9).X,"Left and right thumb markers use their corresponding hands");
        check(RuntimeArtwork.FingerCue(1).X<RuntimeArtwork.FingerCue(4).X&&RuntimeArtwork.FingerCue(5).X<RuntimeArtwork.FingerCue(8).X,"Finger cues follow the teaching hands from little finger to index and index to little finger");
        check(RuntimeArtwork.Resolve("Branding/ratan.png")=="Branding/ratan.png"&&RuntimeArtwork.Resolve("Branding/kolorr.png")=="Branding/kolorr.png","Authentic company branding is unaffected by illustration replacements");
    }
    static void RenderReport(FlowDocument document, string file)
    {
        var paginator = ReportFactory.Paginator(document); paginator.ComputePageCount(); var page = paginator.GetPage(0);
        var image = new RenderTargetBitmap(794, 1123, 96, 96, PixelFormats.Pbgra32); image.Render(page.Visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using var stream = File.Create(file); encoder.Save(stream);
    }
    static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T typed) yield return typed; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    static double MatchReferenceRegion(string capture, string reference, Int32Rect region)
    {
        byte[] Read(string file)
        {
            using var stream = File.OpenRead(file); var source = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
            var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0); var bytes = new byte[region.Width * region.Height * 4]; bitmap.CopyPixels(region, bytes, region.Width * 4, 0); return bytes;
        }
        var actual = Read(capture); var expected = Read(Path.Combine(Assets.Root, "ReferenceUI", reference)); int identical = 0;
        for (int i = 0; i < actual.Length; i += 4) if (actual[i] == expected[i] && actual[i + 1] == expected[i + 1] && actual[i + 2] == expected[i + 2]) identical++;
        return Math.Round(100d * identical / (region.Width * region.Height), 6);
    }
}




