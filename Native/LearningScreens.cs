using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TypeMaster.Native;
public partial class MainWindow
{
    static readonly Dictionary<string, BitmapSource> assetBitmaps = new(StringComparer.OrdinalIgnoreCase);
    internal static BitmapSource AssetBitmap(string file)
    {
        file = RuntimeArtwork.Resolve(file);
        string full = Path.GetFullPath(Path.Combine(Assets.Root, file));
        if (!full.StartsWith(Assets.Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid image path.");
        if (assetBitmaps.TryGetValue(full, out var cached)) return cached;
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(full); bitmap.EndInit(); bitmap.Freeze();
        assetBitmaps[full] = bitmap; return bitmap;
    }
    internal static Image Picture(string file, double width, double height = 180) => new() { Source = AssetBitmap(file), Width = width, MaxHeight = height, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center };
    static ComboBox Choice(IEnumerable<string> values, string selected, Action<string> changed)
    {
        var box = new ComboBox { ItemsSource = values.ToList(), SelectedItem = selected, Padding = new Thickness(4), Margin = new Thickness(0, 3, 8, 10), MinWidth = 110 };
        box.SelectionChanged += (_, _) => { if (box.SelectedItem is string item) changed(item); }; return box;
    }
    static Button Outline(string title, Action action) { var b = Button(title, action); b.Background = Brushes.Transparent; b.Foreground = Brushes.RoyalBlue; b.BorderThickness = new Thickness(1); return b; }
    void LegacyOpenCourse(CourseDefinition course)
    {
        openedCourse = course; openedGroup = null; Heading(course.Name); lessonSequence = course.Chapters.SelectMany(c => c.Lessons).ToList();
        Add(Outline("‹ Choose Course", Courses));
        foreach (var chapter in course.Chapters)
        {
            var panel = new StackPanel();
            foreach (var lesson in chapter.Lessons)
            {
                var b = Outline((Current!.Completed.Contains(lesson.Id) ? "✓  " : "○  ") + lesson.Title + (lesson.Kind != "Practice" ? " · " + lesson.Kind : ""), () => { lessonIndex = lessonSequence.IndexOf(lesson); ShowLesson(lesson); });
                b.HorizontalAlignment = HorizontalAlignment.Stretch; b.HorizontalContentAlignment = HorizontalAlignment.Left; panel.Children.Add(b);
            }
            Add(new Expander { Header = chapter.Name, Content = panel, Margin = new Thickness(0, 7, 0, 7), Foreground = (Brush)Application.Current.Resources["TextBrush"] });
        }
    }
    internal void ShowLesson(Lesson lesson)
    {
        activeLesson = lesson;
        if (lesson.Kind == "Choose test") { ShowPage("Typing Test"); return; }
        if (lesson.Kind is "Practice" or "Test") { int seconds = Current!.TrainingProfile == "Speed Builder" ? Math.Max(60, lesson.Seconds / 2) : lesson.Seconds; if (Current.OptimalDuration && lesson.Kind == "Practice") { double accuracy = Current.Results.Where(r => r.CourseFile == Current.CourseFile).TakeLast(5).Select(r => (double)r.Accuracy).DefaultIfEmpty(95).Average(); seconds = (int)Math.Clamp(seconds * (accuracy < 90 ? 1.25 : accuracy >= 98 ? .75 : 1), 60, 600); } if (Current.CourseDurationMinutes.TryGetValue(Current.CourseFile, out int chosenMinutes)) { int groupSeconds = Math.Max(1, openedGroup?.Seconds ?? lesson.Seconds); seconds = Math.Max(15, chosenMinutes * 60 * lesson.Seconds / groupSeconds); } string exerciseText = lesson.Generator.Length > 0 ? CourseLibrary.Generate(lesson.Generator, "asdfjkl;", LoadKeyboard()) : lesson.Text; StartExercise(exerciseText, lesson.Title, seconds, lesson.Kind == "Test" ? "Test" : "Course", lesson.Id); return; }
        if (lesson.Kind == "Game") { string mode = lesson.Text switch { "WORDTRIS" => "WordTris", "CLOUDS" or "METEOR" => "Clouds", "ABC" => "ABC Speed Race", "PIPE" => "Pipe Game", "CLOUDRACE" => "Cloud Race", _ => "Bubbles" }; StartGame(mode); return; }
        ShowCourseIntroduction(lesson);
    }
    void NextLesson()
    {
        if (openedGroup != null && openedCourse != null)
        {
            int current = openedGroup.Events.FindIndex(e => e.Id == activeLesson?.Id);
            if (current >= 0 && current + 1 < openedGroup.Events.Count) { var next = openedGroup.Events[current + 1]; lessonIndex = lessonSequence.IndexOf(next); ShowLesson(next); }
            else ShowChapter(openedCourse, openedChapter);
            return;
        }
        if (lessonIndex + 1 < lessonSequence.Count) ShowLesson(lessonSequence[++lessonIndex]); else { activeLesson = null; ShowPage("Studying"); }
    }
    void MoreTests()
    {
        Heading("Typing Test");
        var header = new DockPanel(); var language = Choice(["English", "Hindi", "All languages"], Current!.Language == "hin" ? "Hindi" : "English", _ => { }); language.Width = 160; DockPanel.SetDock(language, Dock.Right); header.Children.Add(language); header.Children.Add(Text("1   Test Text", 15, true)); Add(header);
        var list = new ListBox { Height = 140 }; Add(list);
        var customDir = Path.Combine(Store.DirectoryPath, "Texts"); Directory.CreateDirectory(customDir);
        var texts = Library.Concat(Directory.GetFiles(customDir, "*.txt").Select(f => new TestText(f, Path.GetFileNameWithoutExtension(f), "custom"))).ToList();
        void Filter() { list.ItemsSource = texts.Where(t => language.SelectedIndex == 2 || t.Language == "custom" || t.Language.StartsWith(language.SelectedIndex == 0 ? "eng" : "hin")).ToList(); list.SelectedIndex = 0; }
        language.SelectionChanged += (_, _) => Filter(); Filter();
        var options = new WrapPanel(); int minutes = 5; options.Children.Add(Text("2  Duration ", 15, true)); options.Children.Add(Choice(["1", "2", "5", "10", "15"], "5", v => minutes = int.Parse(v)));
        options.Children.Add(Button("3  Start test", () => { if (activeLesson?.Kind != "Choose test") activeLesson = null; if (list.SelectedItem is TestText selected) StartExercise(Assets.Read(selected.File), selected.Name, minutes * 60, "Test", activeLesson?.Id); })); Add(options);
        var imports = new WrapPanel(); imports.Children.Add(Outline("Import Text", () =>
        {
            var dialog = new OpenFileDialog { Filter = "Text files|*.txt;*.exm" }; if (dialog.ShowDialog(this) != true) return;
            string text = Assets.Read(dialog.FileName); if (string.IsNullOrWhiteSpace(text) || text.Length > 500000) { Notify("Choose a non-empty text under 500,000 characters."); return; }
            File.WriteAllText(Path.Combine(customDir, Path.GetFileNameWithoutExtension(dialog.FileName) + "-" + Guid.NewGuid().ToString("N")[..6] + ".txt"), text, Encoding.UTF8); Tests();
        })); imports.Children.Add(Outline("Export Text", () => { if (list.SelectedItem is not TestText t) return; var dialog = new SaveFileDialog { Filter = "Text file|*.txt", FileName = "practice-text.txt" }; if (dialog.ShowDialog(this) == true) File.WriteAllText(dialog.FileName, Assets.Read(t.File), Encoding.UTF8); }));
        foreach (Button b in imports.Children) { b.FontSize = 11; b.Padding = new Thickness(8, 4, 8, 4); b.MinHeight = 28; }
        options.Children.Add(imports);
        Add(Text("Completed Tests", 15, true));
        var lower = new Grid(); lower.ColumnDefinitions.Add(new ColumnDefinition()); lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(215) });
        var results = new ListBox { ItemsSource = Current.Results.Where(r => r.Kind == "Test").Reverse().ToList(), Height = 110, Margin = new Thickness(0, 5, 10, 0) }; lower.Children.Add(results);
        var actions = new StackPanel(); Grid.SetColumn(actions, 1); lower.Children.Add(actions);
        TypingResult? Selected() => results.SelectedItem as TypingResult;
        var print = Outline("View & Print Results", () => { if (Selected() is { } r) PreviewReport(ReportFactory.Result(Current, r)); });
        var certificate = Outline("View & Print Certificate", () => { if (Selected() is { Completed: true } r) PreviewReport(ReportFactory.Certificate(Current, r)); });
        var assessment = Outline("Assessment Report", () => { if (Selected() is { } r) PreviewReport(ReportFactory.Examination(Current, r)); });
        print.IsEnabled = certificate.IsEnabled = false; results.SelectionChanged += (_, _) => { print.IsEnabled = Selected() != null; certificate.IsEnabled = Selected()?.Completed == true; };
        foreach (var b in new[] { print, certificate, assessment }) { b.MinHeight = 29; b.Padding = new Thickness(7, 5, 7, 5); b.Margin = new Thickness(0, 3, 0, 3); b.HorizontalAlignment = HorizontalAlignment.Stretch; actions.Children.Add(b); } Add(lower);
    }
    KeyboardLayout LoadKeyboard()
    {
        string builtin = Path.Combine(Assets.Root, "keyboards", Path.GetFileName(Current!.Keyboard));
        string custom = Path.Combine(Store.DirectoryPath, "Keyboards", Path.GetFileName(Current.Keyboard));
        return KeyboardLayout.Load(File.Exists(custom) ? custom : File.Exists(builtin) ? builtin : Path.Combine(Assets.Root, "keyboards", "us.kbd"));
    }
    void ApplyScreenMode() { WindowState = Current?.ScreenMode == "Maximized" ? WindowState.Maximized : WindowState.Normal; }
    void MoreSettings()
    {
        Heading("Training Settings");
        var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3 };
        void Setting(string label, UIElement editor) { var panel = new StackPanel { Margin = new Thickness(0, 0, 8, 4) }; panel.Children.Add(Text(label, 14, true)); panel.Children.Add(editor); grid.Children.Add(panel); }
        Setting("Sounds", Choice(["Sounds on", "Sounds off"], Current!.Sound ? "Sounds on" : "Sounds off", v => { Current.Sound = v == "Sounds on"; Save(); }));
        Setting("Speed Unit", Choice(["WPM", "CPM"], Current.SpeedUnit, v => { Current.SpeedUnit = v; Save(); }));
        Setting("Screen Size", Choice(["Windowed", "Maximized"], Current.ScreenMode, v => { Current.ScreenMode = v; Save(); ApplyScreenMode(); }));
        Setting("Key Hint Speed", Choice(["Fast", "Normal", "Slow"], Current.HintSpeed, v => { Current.HintSpeed = v; Save(); }));
        Setting("Course Language", LanguageChoice(() => { }));
        var dark = new CheckBox { Content = "Dark Mode", IsChecked = Current.Dark, Margin = new Thickness(0, 10, 0, 10) }; dark.SetResourceReference(ForegroundProperty, "TextBrush"); dark.Click += (_, _) => { Current.Dark = dark.IsChecked == true; Save(); Theme(); }; Setting("Appearance", dark); Add(grid);
        Add(Text("Keyboard Layout", 16, true));
        var customDir = Path.Combine(Store.DirectoryPath, "Keyboards"); Directory.CreateDirectory(customDir);
        var files = Directory.GetFiles(Path.Combine(Assets.Root, "keyboards"), "*.kbd").Concat(Directory.GetFiles(customDir, "*.kbd")).Select(Path.GetFileName).OfType<string>().Distinct().OrderBy(x => x).ToList();
        Add(Choice(files, Current.Keyboard, v => { try { var file = Path.Combine(customDir, v); KeyboardLayout.Load(File.Exists(file) ? file : Path.Combine(Assets.Root, "keyboards", v)); Current.Keyboard = v; Save(); Settings(); } catch (Exception ex) { Notify(ex.Message, "Layout could not be loaded"); } }));
        var layout = LoadKeyboard(); var keyboard = new KeyboardView(layout) { Margin = new Thickness(0, 10, 0, 8), MaxHeight = 145 };
        var actions = new WrapPanel(); actions.Children.Add(Outline("Add Custom Layout", () =>
        {
            var dialog = new OpenFileDialog { Filter = "Typing keyboard layout|*.kbd" }; if (dialog.ShowDialog(this) != true) return;
            try { KeyboardLayout.Load(dialog.FileName); string name = "custom-" + Guid.NewGuid().ToString("N")[..8] + ".kbd"; File.Copy(dialog.FileName, Path.Combine(customDir, name)); Current.Keyboard = name; Save(); Settings(); } catch (Exception ex) { Notify(ex.Message, "Invalid keyboard layout"); }
        }));
        actions.Children.Add(Outline("Activate Windows Layout", () => { if (layout.Code.Length != 8 || LoadKeyboardLayout(layout.Code, 1) == IntPtr.Zero) Notify("Select the matching installed keyboard with Windows + Space. This layout file controls the on-screen guide."); })); Add(actions); Add(keyboard);
        Note("Choose the matching Windows input layout when practicing. The colored keys show finger positions.");
        Add(Text("Profile backup", 16, true)); Add(Outline("Export profile backup", () => { var dialog = new SaveFileDialog { Filter = "Profile backup|*.json", FileName = "TypeMaster-profile.json" }; if (dialog.ShowDialog(this) == true) File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true })); }));
        Add(Outline("Restore profile backup", () =>
        {
            var dialog = new OpenFileDialog { Filter = "Profile backup|*.json" }; if (dialog.ShowDialog(this) != true) return;
            try
            {
                if (new FileInfo(dialog.FileName).Length > 10000000) throw new InvalidDataException("Backup must be under 10 MB.");
                var profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(dialog.FileName)) ?? throw new InvalidDataException("Invalid profile backup.");
                if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 50 || profile.Results == null || profile.Completed == null || profile.Mistakes == null || profile.Meter == null || profile.Games == null) throw new InvalidDataException("Backup is missing profile data.");
                profile.Id = Guid.NewGuid(); while (Store.Profiles.Any(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase))) profile.Name += " (restored)";
                Store.Profiles.Add(profile); Save(); EnterProfile(profile.Name);
            }
            catch (Exception ex) { Notify(ex.Message, "Backup could not be restored"); }
        }));
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadKeyboardLayout(string layout, uint flags);
    void MoreAbout()
    {
        Heading("Information"); Add(Picture("Branding/typemaster-wordmark.png", 310, 65)); Add(Text("TypeMaster", 21, true));
        Add(Text("Developed by "+AppInfo.Developer,18,true));
        Add(Text("TypeMaster is a typing-practice application customised for our computer-assessment workflow. Use it to practise typing, test candidates, record examiner-observed technical skills and create individual PDF reports."));
        Add(Outline("GitHub project",()=>UpdatesWindow.Open(AppUpdates.Project)));
        Add(Outline("Releases & downloads",()=>UpdatesWindow.Open(AppUpdates.Releases)));
        Add(Outline("Check for updates",Updates));
        Add(Text($"Version   {AppInfo.Version}\nBuild       {AppInfo.Build}\nPlatform  Native Windows · C# / WPF\nUser       {Current!.Name}\nLibrary    {Library.Count} tests · {allCourses.Count} courses\nGames    Bubbles, WordTris, Clouds, ABC Speed Race, Pipe Game, Cloud Race"));
        Add(Outline("Learning guide", () => { Heading("Learning Guide"); AddImage("ergo_deskchair.gif", 250); Add(Text("Sit comfortably, keep your wrists relaxed, and place your fingers on the home row. Work through Studying, then use Typing Test to measure progress. Custom Review targets keys that need attention. Pause regularly and aim for accuracy before speed.")); }));
        Note("Supplied artwork and course content retain their original ownership.");
    }
    ComboBox LanguageChoice(Action refresh)
    {
        var names = new Dictionary<string, string> { ["eng"] = "English", ["hin"] = "Hindi", ["fin"] = "Suomi", ["fre"] = "Français", ["ger"] = "Deutsch", ["ita"] = "Italiano", ["por"] = "Português", ["spa"] = "Español", ["swe"] = "Svenska", ["dut"] = "Nederlands", ["tur"] = "Turkish" };
        var values = allCourses.Select(c => c.Language).Distinct().OrderBy(c => c).ToDictionary(c => c, c => names.GetValueOrDefault(c, c));
        var choice = new ComboBox { ItemsSource = values, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = Current!.Language, Padding = new Thickness(4), Margin = new Thickness(0, 3, 8, 10) };
        choice.Style = (Style)Application.Current.FindResource("ReferenceCombo");
        choice.SelectionChanged += (_, _) => { if (choice.SelectedValue is string code) { Current.Language = code; Localization.Language = code; Save(); refresh(); } }; return choice;
    }
}





