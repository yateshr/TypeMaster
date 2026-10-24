using System.IO;
using Path = System.IO.Path;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    CourseDefinition? openedCourse;
    int openedChapter;
    ExerciseGroup? openedGroup;
    internal static string MissingCourseKeys(CourseDefinition course, KeyboardLayout layout)
    {
        if (course.Universal) return "";
        string file = Path.Combine(Assets.Root, "keyboards", Path.GetFileName(course.Keyboard));
        if (!File.Exists(file)) return "";
        var required = KeyboardLayout.Load(file).Keys.SelectMany(k => k.Caption + k.Shift).Where(c => char.IsLetter(c) && c > 127).Distinct();
        var available = layout.Keys.SelectMany(k => k.Caption + k.Shift).ToHashSet();
        return string.Concat(required.Where(c => !available.Contains(c)));
    }
    void ShowKeyboardWarning(CourseDefinition course, string missing)
    {
        Navigation.Visibility = Visibility.Collapsed; NavColumn.Width = new GridLength(0);
        var canvas = ReferencePage("Choose Course", true); ReferenceClose(canvas, 746, 13, () => ShowPage("Studying"));
        Position(canvas, RefTitle("Keyboard Layout"), 190, 72, 540, 38).FontSize = 23;
        Position(canvas, RefText("This course does not match your current keyboard. Some required keys are unavailable. Choose a matching layout in Settings, or continue with the current layout.", 13), 190, 109, 520, 70);
        Position(canvas, RefText("Keyboard Layout: " + LoadKeyboard().Name, 14, true), 190, 191, 540, 28);
        Position(canvas, RefText("Missing keys: " + missing, 14, true), 190, 224, 540, 28);
        void Link(string name, double y, Action action) { var button = RefButton("▸ " + name, action, true); button.Background = Brushes.Honeydew; button.Foreground = Brushes.Blue; button.BorderBrush = Brushes.YellowGreen; button.Tag = new CornerRadius(2); Position(canvas, button, 190, y, 370, 26); }
        Link("Change settings", 273, () => ShowPage("Settings")); Link("Continue course", 354, () => OpenCourse(course, true)); Link("Change course", 391, () => ShowPage("Studying"));
    }
    internal void ShowChapter(CourseDefinition course, int index)
    {
        page = "Studying"; Nav(); Navigation.Visibility = Visibility.Visible; NavColumn.Width = new GridLength(185);
        openedCourse = course; openedChapter = Math.Clamp(index, 0, course.Chapters.Count - 1); openedGroup = null;
        Current!.CourseFile = course.File; Save(); activeLesson = null;
        lessonSequence = course.Chapters.SelectMany(c => c.Lessons).ToList();
        var chapter = course.Chapters[openedChapter];
        var canvas = ReferencePage(course.Name); canvas.SetResourceReference(Panel.BackgroundProperty, "SoftBrush"); Rule(canvas, 18, 49, 561);
        var tabs = new WrapPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < course.Chapters.Count; i++)
        {
            int tab = i; var button = RefButton((i + 1).ToString(), () => ShowChapter(course, tab)); button.FontFamily = new FontFamily("Georgia"); button.FontWeight = FontWeights.Bold; button.Foreground = i == openedChapter ? Brushes.White : Brushes.LightSteelBlue; button.Background = i == openedChapter ? Brushes.DodgerBlue : Brushes.Transparent; button.Width = 28; button.Height = 27; tabs.Children.Add(button);
        }
        Position(canvas, new ScrollViewer { Content = tabs, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }, 18, 50, 560, 47);
        string chapterName = chapter.Name.Contains(". ") ? chapter.Name[(chapter.Name.IndexOf(". ") + 2)..] : chapter.Name;
        Position(canvas, RefTitle("Lesson " + (openedChapter + 1) + ": " + chapterName), 100, 100, 478, 35).FontSize = 23;
        var rows = new Canvas { Width = 365, Height = chapter.Groups.Count * 36 };
        for (int i = 0; i < chapter.Groups.Count; i++)
        {
            int groupIndex = i; var group = chapter.Groups[i]; bool done = group.IsComplete(Current);
            var row = new Canvas(); Position(rows, new Border { BorderBrush = i == 0 ? Brushes.LightSkyBlue : Brushes.Transparent, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Child = row }, 0, i * 36, 350, 32);
            Position(row, new Ellipse { Stroke = Brushes.SkyBlue, StrokeThickness = 2, Fill = Brushes.Transparent }, 3, 5, 19, 19);
            if (done) Position(row, new TextBlock { Text = "✓", Foreground = Brushes.SkyBlue, FontSize = 17, FontWeight = FontWeights.Bold }, 4, 2, 22, 25);
            Position(row, RefText((openedChapter + 1) + "." + (i + 1), 14, true, true), 34, 6, 40, 24);
            var link = RefButton(group.Name, () => BeginGroup(course, openedChapter, groupIndex), true); link.Content = new TextBlock { Text = Localization.Translate(group.Name), TextDecorations = TextDecorations.Underline, TextTrimming = TextTrimming.CharacterEllipsis }; link.Background = Brushes.Transparent; link.BorderThickness = new Thickness(0); link.Foreground = Brushes.Blue; link.Padding = new Thickness(0); Position(row, link, 73, 2, 181, 29);
            int minutes = Math.Max(1, (int)Math.Ceiling(group.Seconds / 60d)); if (Current.CourseDurationMinutes.TryGetValue(course.File, out int selected)) minutes = selected;
            Position(row, RefText(minutes + " - " + (minutes + course.DurationMargin) + " min.", 13), 268, 7, 80, 22);
        }
        Position(canvas, new ScrollViewer { Content = rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }, 120, 139, 377, 220);
        if (openedChapter + 1 < course.Chapters.Count) { var nextChapter = RefButton("▶", () => ShowChapter(course, openedChapter + 1)); nextChapter.Tag = new CornerRadius(19); Position(canvas, nextChapter, 526, 367, 38, 38); Position(canvas, RefText("Lesson " + (openedChapter + 2), 13), 510, 411, 80, 24).Foreground = Brushes.Blue; }
        Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(213, 229, 250)) }, 0, 432, 593, 97);
        Position(canvas, RefText("Course progress", 13), 92, 450, 190, 25);
        double elapsed = Current.Results.Where(r => r.CourseFile == course.File).Sum(r => r.Seconds); int remainingSeconds = course.Chapters.SelectMany(c => c.Groups).Where(g => !g.IsComplete(Current)).Sum(g => g.Seconds);
        double completeGroups = course.Chapters.SelectMany(c => c.Groups).Count(g => g.IsComplete(Current)), groupCount = Math.Max(1,course.Chapters.Sum(c=>c.Groups.Count));
        var track = new Canvas(); track.Children.Add(new Border { Width = 249 * completeGroups / groupCount, Height = 20, Background = Brushes.LightSkyBlue });
        var progress = new Border { BorderBrush = Brushes.RoyalBlue, BorderThickness = new Thickness(1), Background = Brushes.WhiteSmoke, Child = track };
        System.Windows.Automation.AutomationProperties.SetName(progress,"Course progress: "+completeGroups+" of "+groupCount);
        Position(canvas, progress, 23, 475, 251, 22);
        Position(canvas, RefText(Hours(elapsed) + " h", 12), 28, 476, 104, 22); var remaining = Position(canvas, RefText(Hours(remainingSeconds) + " h", 12), 163, 476, 107, 22); remaining.TextAlignment = TextAlignment.Right;
        Position(canvas, RefButton("⌄ Change course", () => ShowPage("Studying")), 352, 444, 201, 34);
        Position(canvas, RefButton("▶ Adjust course duration", () => AdjustCourseDuration(course), true), 352, 487, 201, 32);
    }
    static string Hours(double seconds) => ((int)(seconds / 3600)) + ":" + ((int)(seconds / 60) % 60).ToString("00");
    internal void BeginGroup(CourseDefinition course, int chapter, int group)
    {
        openedCourse = course; openedChapter = chapter; openedGroup = course.Chapters[chapter].Groups[group]; Current!.CourseFile = course.File;
        lessonSequence = course.Chapters.SelectMany(c => c.Lessons).ToList();
        var next = openedGroup.Events.FirstOrDefault(e => !Current.Completed.Contains(e.Id)) ?? openedGroup.Events[0]; lessonIndex = lessonSequence.IndexOf(next); ShowLesson(next);
    }
    void AdjustCourseDuration(CourseDefinition course)
    {
        var dialog = new Window { Owner = this, Title = "Adjust course duration", Width = 430, Height = 265, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var body = new StackPanel { Margin = new Thickness(20) }; body.Children.Add(RefText("Choose minutes per exercise group", 17, true));
        string[] durations = ["Use course durations", "1 min.", "2 min.", "3 min.", "5 min.", "10 min."]; var choice = new ComboBox { ItemsSource = durations, SelectedIndex = Current!.CourseDurationMinutes.TryGetValue(course.File, out int savedMinutes) ? Math.Max(0, Array.IndexOf(durations, savedMinutes + " min.")) : 0, Margin = new Thickness(0, 15, 0, 15) }; body.Children.Add(choice);
        var optimal = new CheckBox { Content = "Optimal duration", IsChecked = Current!.OptimalDuration }; body.Children.Add(optimal);
        body.Children.Add(RefButton("Save", () => { if (choice.SelectedIndex == 0) Current.CourseDurationMinutes.Remove(course.File); else Current.CourseDurationMinutes[course.File] = int.Parse(choice.SelectedItem.ToString()!.Split(' ')[0]); Current.OptimalDuration = optimal.IsChecked == true; Save(); dialog.Close(); ShowChapter(course, openedChapter); })); dialog.Content = body; dialog.ShowDialog();
    }
}

