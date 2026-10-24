using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TypeMaster.Native;
public partial class MainWindow : Window
{
    internal readonly ProfileStore Store;
    internal Profile? Current;
    internal readonly List<TestText> Library;
    internal readonly List<Chapter> Chapters;
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    readonly Stopwatch watch = new();
    string page = "Studying";
    string target = "", typed = "", exerciseTitle = "", kind = "Practice";
    string? lessonId;
    int duration, attempts, errors;
    Dictionary<char, int> sessionMistakes = [];
    bool exercising;
    internal TextBox? TypingBox;
    TextBlock? targetBlock, clockBlock, speedBlock, accuracyBlock;
    readonly MediaPlayer errorSound = new();
    TypingGameView? gameView;
    readonly List<CourseDefinition> allCourses = CourseLibrary.LoadAll();
    KeyboardView? liveKeyboard;
    bool paused;
    bool paragraphSpacing;
    DateTime lastInput;
    DateTime lastDraftSave;
    Lesson? activeLesson;
    List<Lesson> lessonSequence = [];
    int lessonIndex;

    public MainWindow() : this(new ProfileStore()) { }
    internal MainWindow(ProfileStore store)
    {
        InitializeComponent();
        WindowFrame.Configure(this,WindowSurface,CaptionButtons);
        ReportBranding.Configure(store.DirectoryPath);
        Store = store; Library = Assets.Tests(); Chapters = Assets.Course();
        timer.Tick += (_, _) => Tick();
        Closing += (_, e) =>
        {
            if (exercising && typed.Length > 0 && MessageBox.Show(this, "An exercise is in progress. Save it for recovery and close?", "Close TypeMaster", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) e.Cancel = true;
            if (!e.Cancel && meter?.IsRunning == true && !quitting) { e.Cancel = true; Hide(); return; }
            if (!e.Cancel) { SaveExerciseDraft(); timer.Stop(); gameView?.Dispose(); StopMeter(false); errorSound.Close(); }
        };
        BrandImage.Source = AssetBitmap("Branding/typemaster-wordmark.png");
        CaptionIcon.Source = AssetBitmap("Branding/typemaster-icon.png");
        Login();
    }
    static TextBlock Text(string value, double size = 14, bool bold = false)
    {
        var block = new TextBlock { Text = value, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 10) };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush"); return block;
    }
    static Button Button(string label, Action action)
    {
        var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => action(); return button;
    }
    void Add(UIElement element) => PageContent.Children.Add(element);
    void Heading(string title) { PageScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; PageContent.Margin = new Thickness(18,12,18,12); PageContent.Children.Clear(); Add(Text(title, 25)); PageScroll.ScrollToTop(); }
    void Note(string text) { var block = Text(text, 13); block.Foreground = Brushes.SlateGray; Add(block); }
    void Notify(string text, string title = "TypeMaster") => MessageBox.Show(this, text, title, MessageBoxButton.OK, MessageBoxImage.Information);
    void Save() => Store.Save();
    void Theme()
    {
        bool dark = Current?.Dark == true;
        Application.Current.Resources["PanelBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#1B293D" : "#FFFFFF"));
        Application.Current.Resources["SoftBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#24344F" : "#F3F6FF"));
        Application.Current.Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E4ECF9" : "#000000"));
        Application.Current.Resources["SidebarBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#172438" : "#F4F6F9"));
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#101927" : "#F5FAFE"));
    }
    internal void EnterProfile(string name, bool offerRecovery = true)
    {
        Current = Store.Profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (Current == null) { Current = new Profile { Name = name }; Store.Profiles.Add(Current); Save(); }
        Localization.Language = Current.Language; Current.LastUsed = DateTime.Now; Current.Sessions++; Save();
        bool migrated = false;
        foreach (var result in Current.Results.Where(r => r.Kind == "Test" && !r.CriteriaRecorded))
        {
            var specification = Library.FirstOrDefault(t => t.Name == result.Title || Path.GetFileNameWithoutExtension(t.File) == result.Title);
            if (specification != null) { result.RequiredAccuracy = specification.MinAccuracy; result.RequiredWpm = specification.MinSpeed; }
            result.CriteriaRecorded = true; migrated = true;
        }
        var originalLessons = Chapters.SelectMany(c => c.Lessons).ToList();
        foreach (string completed in Current.Completed.ToArray())
        {
            if (!completed.StartsWith("uk7qwert:") || !int.TryParse(completed[9..], out int index) || index < 0 || index >= originalLessons.Count) continue;
            var old = originalLessons[index];
            foreach (var lesson in allCourses.Where(c => c.File == "uk7qwert.cnt").SelectMany(c => c.Chapters).SelectMany(c => c.Lessons).Where(l => l.Text == old.Text)) migrated |= Current.Completed.Add(lesson.Id);
        }
        if (migrated) Save();
        page = "Studying"; ApplyScreenMode(); ShowPage(page); if (offerRecovery) OfferExerciseRecovery();
    }
    internal void ShowPage(string title)
    {
        pendingCompletion = null;
        timer.Stop(); gameView?.Dispose(); gameView = null; exercising = false; page = title; Theme(); Nav();
        PageScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        switch (title)
        {
            case "Studying": Courses(); break;
            case "Typing Test": Tests(); break;
            case "Typing Meter": MeterScreen(); break;
            case "Custom Review": Review(); break;
            case "Games": Games(); break;
            case "Statistics": Statistics(); break;
            case "Settings": Settings(); break;
            default: About(); break;
        }
    }
    void InputChanged()
    {
        if (!exercising || paused || restoringDraft || TypingBox == null) return;
        lastInput = DateTime.UtcNow;
        if (!watch.IsRunning && TypingBox.Text.Length > 0) { if (ExerciseSeconds == 0) exerciseStarted = DateTime.Now; watch.Start(); timer.Start(); }
        string value = TypingBox.Text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (value.Length > target.Length) { int caret = TypingBox.CaretIndex; TypingBox.Text = value[..target.Length]; TypingBox.CaretIndex = Math.Min(caret, TypingBox.Text.Length); return; }
        int prefix = 0; while (prefix < typed.Length && prefix < value.Length && typed[prefix] == value[prefix]) prefix++;
        int oldEnd = typed.Length, newEnd = value.Length;
        while (oldEnd > prefix && newEnd > prefix && typed[oldEnd - 1] == value[newEnd - 1]) { oldEnd--; newEnd--; }
        bool madeError = false;
        for (int i = prefix; i < newEnd; i++)
        {
            attempts++;
            if (i < target.Length && value[i] != target[i]) { errors++; madeError = true; sessionMistakes[target[i]] = sessionMistakes.GetValueOrDefault(target[i]) + 1; }
        }
        typed = value;
        if (madeError && Current!.Sound)
        {
            var file = Path.Combine(Assets.Root, "Error.wav");
            if (File.Exists(file)) { errorSound.Open(new Uri(file)); errorSound.Volume = .12; errorSound.Play(); }
        }
        Paint(); UpdateMetrics(); SaveExerciseDraft(); typingFeedback?.Refresh(target); liveKeyboard?.Highlight(kind == "ReviewKeys" && typed.Length < target.Length ? target[typed.Length].ToString() : "");
        if (typed.Length >= target.Length) FinishExercise();
    }
    void Paint()
    {
        if (kind == "ReviewKeys") { PaintKeyDrill(); return; }
        if (targetBlock == null || paintingTarget) return;
        paintingTarget = true;
        try {
        targetBlock.Inlines.Clear();
        int cursor = Math.Min(typed.Length, (TypingBox?.Text[..Math.Min(TypingBox.CaretIndex, TypingBox.Text.Length)] ?? typed).Replace("\r\n", "\n").Replace('\r', '\n').Length);
        int start = Math.Max(0, cursor - 100), end = Math.Min(target.Length, start + 550);
        if (numericPassage)
        {
            int row = target.Take(cursor).Count(c => c == '\n');
            int firstRow = row / 9 * 9;
            start = 0;
            for (int line = 0; line < firstRow; line++) start = target.IndexOf('\n', start) + 1;
            end = start;
            for (int line = 0; line < 9 && end < target.Length; line++) { int next = target.IndexOf('\n', end); end = next < 0 ? target.Length : next + 1; }
        }
        int activeEnd = typed.Length;
        if (kind == "Test") while (activeEnd < target.Length && !char.IsWhiteSpace(target[activeEnd])) activeEnd++;
        if (start > 0 && !numericPassage) targetBlock.Inlines.Add(new Run("… "));
        Run? activeRun = null;
        for (int i = start; i < end; i++)
        {
            var run = new Run(target[i] == '\n' ? "↵" : target[i].ToString());
            if (i < typed.Length) { run.Foreground = typed[i] == target[i] ? (kind == "Test" ? Brushes.DarkOliveGreen : Brushes.SeaGreen) : Brushes.Crimson; if (typed[i] != target[i]) run.Background = Brushes.MistyRose; }
            else if (i == typed.Length || i < activeEnd) { run.Foreground = Brushes.Blue; run.TextDecorations = TextDecorations.Underline; }
            targetBlock.Inlines.Add(run);
            if (i == cursor) activeRun = run;
            if (target[i] == '\n') { targetBlock.Inlines.Add(new LineBreak()); if (paragraphSpacing) targetBlock.Inlines.Add(new LineBreak()); }
        }
        if (activeRun != null && targetViewport != null)
        {
            targetViewport.UpdateLayout();
            var bounds = activeRun.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            targetViewport.ScrollToVerticalOffset(numericPassage ? 0 : Math.Max(0, bounds.Top - 50));
            targetViewport.UpdateLayout();
            ActiveTargetBounds = new Rect(bounds.X, bounds.Y - targetViewport.VerticalOffset, bounds.Width, bounds.Height);
        }
        } finally { paintingTarget = false; }
    }
    bool paintingTarget;
    ScrollViewer? targetViewport;
    bool numericPassage;
    System.Windows.Shapes.Path? countdownPie;
    internal void UpdateCountdownPie(double fraction)
    {
        if (countdownPie == null) return;
        fraction = Math.Clamp(fraction, 0, 1);
        if (fraction == 0) { countdownPie.Data = Geometry.Empty; return; }
        if (fraction == 1) { countdownPie.Data = new EllipseGeometry(new Point(61, 33.5), 61, 33.5); return; }
        double angle = fraction * Math.PI * 2;
        var figure = new PathFigure { StartPoint = new Point(61, 33.5), IsClosed = true };
        figure.Segments.Add(new LineSegment(new Point(61, 0), true));
        figure.Segments.Add(new ArcSegment(new Point(61 + 61 * Math.Sin(angle), 33.5 - 33.5 * Math.Cos(angle)), new Size(61, 33.5), 0, fraction > .5, SweepDirection.Clockwise, true));
        countdownPie.Data = new PathGeometry(new[] { figure });
    }
    internal Rect ActiveTargetBounds { get; private set; }
    void Tick() { if (exercising && !paused) { if (typed.Length > 0 && (DateTime.UtcNow - lastDraftSave).TotalSeconds >= 1) { SaveExerciseDraft(); lastDraftSave = DateTime.UtcNow; } UpdateMetrics(); if (liveKeyboard != null && typed.Length < target.Length && (DateTime.UtcNow - lastInput).TotalMilliseconds > (Current!.HintSpeed == "Fast" ? 250 : Current.HintSpeed == "Slow" ? 2000 : 1000)) liveKeyboard.Highlight(target[typed.Length].ToString()); if (ExerciseSeconds >= duration) FinishExercise(); } }
    void UpdateMetrics()
    {
        if (kind == "ReviewKeys") { clockBlock!.Text = Math.Min(100, typed.Length * 100 / target.Length) + " %"; return; }
        var score = kind == "Test" ? Scoring.MeasureTest(target, typed, Math.Min(ExerciseSeconds, duration), attempts, errors) : Scoring.Measure(target, typed, Math.Min(ExerciseSeconds, duration), attempts, errors);
        clockBlock!.Text = TimeSpan.FromSeconds(Math.Max(0, duration - (int)ExerciseSeconds)).ToString(@"mm\:ss");
        speedBlock!.Text = typed.Length == 0 ? "" : (Current!.SpeedUnit == "CPM" ? score.NetWpm * 5 : score.NetWpm) + " " + Current.SpeedUnit; accuracyBlock!.Text = typed.Length == 0 ? "" : score.Accuracy + "% accuracy";
        UpdateCountdownPie(ExerciseSeconds / Math.Max(1, duration));
        if (countdownPie != null) countdownPie.ToolTip = speedBlock.Text + " · " + accuracyBlock.Text;
    }
    internal TypingResult? FinishExercise(bool silent = false)
    {
        if (!exercising || typed.Length == 0) { if (!silent) Notify("Type some of the exercise before saving a result."); return null; }
        watch.Stop(); timer.Stop();
        var result = kind == "Test" ? Scoring.MeasureTest(target, typed, Math.Min(ExerciseSeconds, duration), attempts, errors) : Scoring.Measure(target, typed, Math.Min(ExerciseSeconds, duration), attempts, errors);
        result.AttemptId = attemptId; result.Title = exerciseTitle; result.Kind = kind;
        result.Completed = ExerciseSeconds >= duration || typed.Length == target.Length;
        result.StartedAt = exerciseStarted; result.PlannedSeconds = duration; result.OriginalText = exerciseSourceText; result.ExpectedText = target; result.TypedText = typed; result.HasTextSnapshot = true;
        if (kind != "Test") { result.GrossStrokes = typed.Length; result.NetStrokes = result.Correct; result.ErrorHits = errors; }
        var specification = Library.FirstOrDefault(t => t.Name == exerciseTitle); result.RequiredAccuracy = specification?.MinAccuracy ?? 0; result.RequiredWpm = specification?.MinSpeed ?? 0; result.CriteriaRecorded = true;
        result.CourseFile = lessonId == null ? "" : Current!.CourseFile; result.LessonId = lessonId ?? ""; result.Mistakes = new Dictionary<char, int>(sessionMistakes);
        Current!.Results.Add(result);
        foreach (var pair in sessionMistakes) Current.Mistakes[pair.Key] = Current.Mistakes.GetValueOrDefault(pair.Key) + pair.Value;
        if (lessonId != null && result.Completed && result.Accuracy >= (Current.TrainingProfile == "Accuracy First" ? 98 : (openedCourse?.AccuracyGoal ?? 90))) Current.Completed.Add(lessonId);
        Save(); ClearExerciseDraft(); exercising = false;
        if (!silent && kind == "Test") ShowTestCompleted(result);
        else { ShowPage(page); if (!silent) ShowResult(result); }
        return result;
    }
    static DataGrid ResultsGrid(IEnumerable<TypingResult> results)
    {
        var grid = new DataGrid { ItemsSource = results.ToList(), AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, MaxHeight = 280, Margin = new Thickness(0, 8, 0, 12), HeadersVisibility = DataGridHeadersVisibility.Column, Background = Brushes.White, Foreground = Brushes.Black };
        foreach (var (title, property, format) in new[] { ("Exercise", "Title", ""), ("Net WPM", "NetWpm", ""), ("Accuracy", "Accuracy", "{0}%"), ("Date", "Date", "{0:g}") })
            grid.Columns.Add(new DataGridTextColumn { Header = title, Binding = new System.Windows.Data.Binding(property) { StringFormat = format }, Width = property == "Title" ? new DataGridLength(1, DataGridLengthUnitType.Star) : DataGridLength.Auto });
        return grid;
    }
    void Export()
    {
        var dialog = new SaveFileDialog { Filter = "JSON result file|*.json", FileName = "TypeMaster-results.json" };
        if (dialog.ShowDialog(this) != true) return;
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(new { Current!.Name, Current.Results }, new JsonSerializerOptions { WriteIndented = true }));
        Notify("Results exported successfully.");
    }
    void AddImage(string filename, double width)
    {
        string file = Path.Combine(Assets.Root, filename);
        if (!File.Exists(file)) return;
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(file); bitmap.EndInit(); bitmap.Freeze();
        Add(new Image { Source = bitmap, MaxWidth = width, MaxHeight = 190, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 10) });
    }
    internal void Capture(string file)
    {
        // Let text and effect caches finish before reading the visual tree.
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);
        UpdateLayout(); Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        var bitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(this);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = File.Create(file); encoder.Save(output);
    }
}







