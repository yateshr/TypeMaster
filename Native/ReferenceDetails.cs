using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TypeMaster.Native;
public partial class MainWindow
{
    void Tests()
    {
        var canvas = ReferencePage("Typing Test");
        var first = Position(canvas, RefText("1", 52), 14, 61, 39, 67); first.FontFamily = new FontFamily("Segoe UI"); first.Foreground = new SolidColorBrush(Color.FromRgb(103, 139, 255));
        Position(canvas, RefText("Test Text", 14, true, true), 60, 64, 200, 24);
        string directory = Path.Combine(Store.DirectoryPath, "Texts"); Directory.CreateDirectory(directory);
        var tests = Library.Where(t => testAllLanguages || MatchesLanguage(t.Language, Current!.Language)).Concat(Directory.GetFiles(directory, "*.txt").Select(f => new TestText(f, Path.GetFileNameWithoutExtension(f), "custom"))).ToList();
        string[] priority = ["10 Min.", "A Bright Future", "A Love Letter", "A Typing Exam - Aiming", "A Typing Exam - Awakening", "Aesop", "Astronauts"];
        tests = tests.OrderBy(t => { int index = Array.FindIndex(priority, p => t.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)); return index < 0 ? priority.Length : index; }).ThenBy(t => t.Name).ToList();
        var list = new ListBox { ItemsSource = tests, SelectedIndex = 0, FontFamily = new FontFamily("Tahoma"), FontSize = 13, Padding = new Thickness(0), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) }; Position(canvas, list, 60, 86, 468, 150);
        list.ItemContainerStyle = (Style)Application.Current.FindResource("ReferenceListItem");
        var delete = RefButton("Delete", () => { if (list.SelectedItem is not TestText { Language: "custom" } selected) return; if (MessageBox.Show(this, "Delete this custom test? Saved results will be kept.", "Delete test", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { string full = Path.GetFullPath(selected.File); if (full.StartsWith(Path.GetFullPath(CustomTextsDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) File.Delete(full); Tests(); } }, true); delete.HorizontalContentAlignment = HorizontalAlignment.Center; delete.Foreground = Brushes.Gray; delete.BorderBrush = Brushes.LightGray; delete.Tag = new CornerRadius(3); delete.IsEnabled = list.SelectedItem is TestText { Language: "custom" }; Position(canvas, delete, 291, 50, 111, 28);
        var add = RefButton("Add   ▾", () => AddCustomTest(), true); add.Foreground = Brushes.Black; add.HorizontalContentAlignment = HorizontalAlignment.Center; add.Tag = new CornerRadius(3); Position(canvas, add, 417, 50, 111, 28);
        var addMenu = new ContextMenu(); foreach (var entry in new (string, Action)[] { ("Add Custom Test…", AddCustomTest), ("Import Document (Word, HTML, TXT)…", () => ImportTestDocument()), ("Browse Test Library…", () => { testAllLanguages = !testAllLanguages; Tests(); }), ("Import Test Pack (ZIP)…", () => ImportTestDocument(true)) }) { var item = new MenuItem { Header = entry.Item1 }; item.Click += (_, _) => entry.Item2(); addMenu.Items.Add(item); } add.ContextMenu = addMenu; add.Click += (_, _) => { }; add.PreviewMouseLeftButtonDown += (_, e) => { e.Handled = true; addMenu.PlacementTarget = add; addMenu.IsOpen = true; };
        var description = RefText("Select a text to practice typing.", 10); description.Foreground = Brushes.Olive; Position(canvas, description, 71, 237, 450, 26);
        void SelectedText() { if (list.SelectedItem is TestText text) { description.Text = (text.Description.Length > 0 ? text.Description : text.Name) + (text.MinAccuracy > 0 ? " (Accuracy " + text.MinAccuracy + "%)" : "") + (text.MinSpeed > 0 ? " (Net Speed " + text.MinSpeed + " wpm)" : ""); delete.IsEnabled = text.Language == "custom"; } } list.SelectionChanged += (_, _) => SelectedText(); SelectedText();
        var second = Position(canvas, RefText("2", 52), 14, 283, 39, 67); second.FontFamily = new FontFamily("Segoe UI"); second.Foreground = new SolidColorBrush(Color.FromRgb(103, 139, 255));
        Position(canvas, RefText("Duration", 14, true, true), 60, 289, 170, 24); int duration = Current!.TestMinutes;
        Position(canvas, RefChoice(Enumerable.Range(1, 60).Select(n => n + " min."), duration + " min.", v => { duration = int.Parse(v.Split(' ')[0]); Current.TestMinutes = duration; Save(); }), 60, 313, 121, 24);
        var third = Position(canvas, RefText("3", 52), 292, 283, 39, 67); third.FontFamily = new FontFamily("Segoe UI"); third.Foreground = new SolidColorBrush(Color.FromRgb(103, 139, 255));
        var start = RefButton("Start test", () => { if (activeLesson?.Kind != "Choose test") activeLesson = null; if (list.SelectedItem is TestText text) StartExercise(Assets.Read(text.File), text.Name, duration * 60, "Test", activeLesson?.Id); }); start.FontSize = 18; Position(canvas, start, 336, 296, 204, 34);
        var tools = new ContextMenu(); var import = new MenuItem { Header = "Import / export text and choose another language…" }; import.Click += (_, _) => MoreTests(); tools.Items.Add(import); list.ContextMenu = tools;
        Rule(canvas, 0, 376, 593); Position(canvas, RefText("Completed Tests", 14, true, true), 25, 387, 290, 25);
        var completed = new ListBox { ItemsSource = Current!.Results.Where(r => r.Kind == "Test").Reverse().ToList(), FontSize = 12, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Padding = new Thickness(1) }; Position(canvas, completed, 23, 409, 266, 102);
        TypingResult? Selected() => completed.SelectedItem as TypingResult;
        var result = RefButton("View & Print Results", () => { if (Selected() is { } r) PreviewReport(ReportFactory.Result(Current, r)); }, true);
        var certificate = RefButton("View & Print Certificate", () => { if (Selected() is { Completed: true } r) PreviewReport(ReportFactory.Certificate(Current, r)); }, true);
        var assessment = RefButton("View & Print Assessment Report", () => { if (Selected() is { } r) PreviewReport(ReportFactory.Examination(Current, r)); }, true);
        foreach (var button in new[] { result, certificate, assessment }) button.Foreground = Brushes.DarkGray;
        result.IsEnabled = certificate.IsEnabled = assessment.IsEnabled = false;
        completed.SelectionChanged += (_, _) => { result.IsEnabled = assessment.IsEnabled = Selected() != null; certificate.IsEnabled = Selected()?.Completed == true; result.Foreground = assessment.Foreground = result.IsEnabled ? Brushes.DodgerBlue : Brushes.DarkGray; certificate.Foreground = certificate.IsEnabled ? Brushes.DodgerBlue : Brushes.DarkGray; }; completed.SelectedIndex = Current.Results.Any(r => r.Kind == "Test") ? 0 : -1;
        Position(canvas, result, 336, 402, 241, 34); Position(canvas, certificate, 336, 441, 241, 34); Position(canvas, assessment, 336, 482, 241, 34);
    }
    void About()
    {
        var canvas = ReferencePage("Information");
        Position(canvas, RefTitle("TypeMaster"), 101, 61, 400, 32).FontSize = 23; Rule(canvas, 100, 87, 399);
        foreach (var row in new[] { ("Version", "1.10.2 [2026]", 101d), ("Build", "Native Windows · C# / WPF", 124d), ("User", Current!.Name, 147d) }) { Position(canvas, RefText(row.Item1, 14, true, true), 103, row.Item3, 94, 22); Position(canvas, RefText(row.Item2, 13), 199, row.Item3, 300, 22); }
        Rule(canvas, 100, 186, 399);
        Position(canvas, RefText("Profile Services", 14, true, true), 103, 204, 390, 24);
        void Link(string title, double y, Action action) { var link = RefButton("▸ " + title, action, true); link.BorderThickness = new Thickness(0); link.Tag = new CornerRadius(0); link.Foreground = Brushes.Blue; link.FontSize = 13; Position(canvas, link, 103, y, 390, 25); }
        Link("Profile Backup & Restore", 228, MoreSettings); Link("Export My Results", 255, Export); Link("View My Progress Report", 282, PrintReport);
        Position(canvas, RefText("Product Information", 14, true, true), 103, 334, 390, 24);
        Link("Source Code & Build Information", 358, MoreAbout); Link("User Support and Manual", 385, () => { Heading("User Guide"); AddImage("ergo_deskchair.gif", 240); Add(Text("Start with Studying and follow the lessons in order. Use Typing Test to measure your speed and accuracy. Custom Review targets difficult keys. All progress is saved locally. Pause or stop the Typing Meter through its widget or tray icon.")); });
        Rule(canvas, 100, 487, 399); Position(canvas, RefText("Native Windows build · Supplied assets retain their ownership.", 10), 103, 490, 400, 20);
    }
    void Games()
    {
        var canvas = ReferencePage(null); canvas.Background = new SolidColorBrush(Color.FromRgb(200, 205, 254));
        // Pure illustration areas from the supplied reference; all labels, selectors and buttons below remain native controls.
        var art = AssetBitmap("ReferenceUI/games-reference.png");
        var images = new[] { new Int32Rect(331, 172, 200, 182), new Int32Rect(553, 173, 168, 171), new Int32Rect(731, 173, 190, 179), new Int32Rect(353, 449, 170, 173), new Int32Rect(553, 449, 168, 173), new Int32Rect(731, 449, 190, 173) };
        string[] names = TypingGameEngine.Names;
        for (int i = 0; i < names.Length; i++)
        {
            string mode = names[i]; int col = i % 3; bool lower = i >= 3; double x = col == 0 ? 0 : col == 1 ? 200 : 399;
            var image = new Image { Source = new CroppedBitmap(art, images[i]), Stretch = Stretch.Fill }; Position(canvas, image, images[i].X - 331, images[i].Y - 172, images[i].Width, images[i].Height);
            if (i == 2) Position(canvas, ReferenceGraphic("games-reference.png", new Int32Rect(779, 399, 145, 6)), 448, 227, 145, 6);
            if (i == 5) Position(canvas, ReferenceGraphic("games-reference.png", new Int32Rect(731, 647, 190, 8)), 400, 475, 190, 8);
            // These fixed game captions are part of the supplied selector artwork.
            // Keep their exact lettering while exposing a text name to accessibility tools.
            var captionBounds = new[] { new Int32Rect(377, 344, 110, 31), new Int32Rect(577, 344, 110, 31), new Int32Rect(773, 344, 110, 31), new Int32Rect(335, 623, 191, 32), new Int32Rect(552, 623, 170, 32), new Int32Rect(731, 623, 193, 32) };
            var captionRect = captionBounds[i]; var label = ReferenceGraphic("games-reference.png", captionRect); System.Windows.Automation.AutomationProperties.SetName(label, mode); Position(canvas, label, captionRect.X - 331, captionRect.Y - 172, captionRect.Width, captionRect.Height);
            string category = mode is "Bubbles" or "ABC Speed Race" or "Pipe Game" ? "a-z" : "Animals";
            if (i < 3 || i == 3) Position(canvas, RefChoice(i is 0 or 3 ? ["a-z", "A-Z", "0-9"] : ["Animals", "Common words", "Difficult words"], category, v => category = v), x + (col == 2 ? 19 : i == 3 ? 37 : 22), lower ? 422 : 203, i == 3 ? 139 : 169, 24);
            Position(canvas, RefButton("Start", () => { activeLesson = null; StartGame(mode, category); }), x + (col == 2 ? 43 : 46), lower ? 483 : 233, 110, 34);
        }
        Rule(canvas, 12, 273, 181); Rule(canvas, 208, 273, 183); Rule(canvas, 400, 273, 193);
        Position(canvas, new Border { Background = Brushes.RoyalBlue }, 200, 1, 1, 528); Position(canvas, new Border { Background = Brushes.RoyalBlue }, 399, 1, 1, 528);
    }
}


















