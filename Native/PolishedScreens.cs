using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    internal void OpenCourse(CourseDefinition course, bool allowMismatch = false)
    {
        string missing = MissingCourseKeys(course, LoadKeyboard()); if (!allowMismatch && missing.Length > 0) { ShowKeyboardWarning(course, missing); return; }
        page = "Studying"; Nav(); Navigation.Visibility = Visibility.Visible; NavColumn.Width = new GridLength(185);
        Current!.CourseFile = course.File; Save(); lessonSequence = course.Chapters.SelectMany(c => c.Lessons).ToList();
        var canvas = ReferencePage(course.Name); var change = RefButton("▸ Change course", Courses, true); change.FontSize = 13; Position(canvas, change, 451, 13, 128, 34);
        Position(canvas, RefTitle("Lessons"), 34, 58, 280, 32).FontSize = 23;
        var lessons = new Canvas { Width = 300, Height = Math.Max(350, course.Chapters.Count * 24) }; Position(canvas, new ScrollViewer { Content = lessons, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }, 34, 85, 300, 350);
        int nextChapter = Math.Max(0, course.Chapters.FindIndex(c => c.Lessons.Any(l => !Current.Completed.Contains(l.Id))));
        for (int i = 0; i < course.Chapters.Count; i++)
        {
            int chapterIndex = i; var chapter = course.Chapters[i]; string title = chapter.Name.Contains(". ") ? chapter.Name[(chapter.Name.IndexOf(". ") + 2)..] : chapter.Name;
            Position(lessons, RefText((i + 1) + ".", 13, true), 0, 7 + i * 24, 28, 24).Foreground = Brushes.DarkOliveGreen;
            var link = RefButton(title, () => BeginChapter(chapterIndex), true); link.Content = new TextBlock { Text = title, TextDecorations = TextDecorations.Underline }; link.Background = Brushes.Transparent; link.BorderThickness = new Thickness(0); link.Foreground = Brushes.Blue; link.Padding = new Thickness(0); link.FontSize = 13;
            Position(lessons, link, 28, i * 24, 270, 30);
            var menu = new ContextMenu(); var activities = new MenuItem { Header = "View all course activities" }; activities.Click += (_, _) => LegacyOpenCourse(course); menu.Items.Add(activities); link.ContextMenu = menu;
        }
        void BeginChapter(int index) { ShowChapter(course, index); }
        var information = new Canvas { Background = Brushes.White }; Position(canvas, new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Child = information }, 344, 66, 233, 307);
        Position(information, RefText(Current.OptimalDuration ? "▥ Optimal Duration" : "Exercise Duration", 14, true, true), 7, 4, 217, 24);
        Position(information, RefText(Current.OptimalDuration ? "During this course the duration of each exercise will be optimal based on your progress. This will save you time while guaranteeing good learning results." : "Exercises use the duration supplied with the course. You can enable optimal duration to adjust the length using your recent accuracy.", 13), 7, 28, 215, 122);
        Position(information, RefText(course.File == "uk7qwert.cnt" ? "● Each lesson will take 15 - 25\n   minutes depending on your\n   progress\n\n● The whole course takes 3:10 -\n   5:20 hours" : "● Work through the lessons at\n   your own pace.\n\n● Progress is saved after each\n   completed activity.", 12), 8, 157, 215, 108);
        var optimal = RefButton(Current.OptimalDuration ? "Turn Off Optimal Duration" : "Turn On Optimal Duration", () => { Current.OptimalDuration = !Current.OptimalDuration; Save(); OpenCourse(course); }, true); optimal.Background = Brushes.Transparent; optimal.Padding = new Thickness(0); optimal.BorderThickness = new Thickness(0); optimal.Foreground = Brushes.Blue; Position(information, optimal, 7, 271, 219, 29);
        Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(245, 250, 254)) }, 0, 448, 593, 81);
        var cancel = RefButton("Cancel", Courses, true); cancel.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, cancel, 26, 472, 97, 34);
        Position(canvas, RefButton("Continue Lesson " + (nextChapter + 1), () => BeginChapter(nextChapter)), 384, 472, 190, 34);
    }
    internal void ShowResult(TypingResult result)
    {
        pendingCompletion = null;
        Navigation.Visibility = Visibility.Collapsed; NavColumn.Width = new GridLength(0); var canvas = ReferencePage(null, true);
        var left = new Border(); left.SetResourceReference(Border.BackgroundProperty, "SoftBrush"); Position(canvas, left, 0, 0, 497, 529);
        Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(213, 229, 250)) }, 497, 0, 281, 529);
        Position(canvas, RefTitle(result.Kind == "Test" ? "Typing Test Results" : "Exercise Results"), 18, 9, 470, 42);
        string status = !result.Completed ? "Partial Attempt Saved" : result.Kind != "Test" ? "Exercise Completed" : result.Passed ? "Test Passed" : "Test Not Passed";
        if (result.Passed) Position(canvas, Picture("checkmark.gif", 16, 16), 24, 60, 16, 16);
        else Position(canvas, new Border { Background = Brushes.Gold, CornerRadius = new CornerRadius(2), Child = new TextBlock { Text = "!", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center } }, 24, 60, 16, 16);
        Position(canvas, RefText(status, 14, true, true), 46, 60, 430, 23);
        Position(canvas, RefText(result.Passed ? "You completed this typing test." : result.Completed ? "For more information please look at the details." : "This attempt ended early and does not qualify for a certificate.", 13), 46, 82, 430, 36);
        Rule(canvas, 48, 126, 399); Rule(canvas, 48, 252, 399);
        int row = 0; foreach (var item in new[] { ("Time Used", ReportFactory.Time(result.Seconds)), ("Gross Speed", result.GrossWpm + " wpm"), ("Accuracy", result.Accuracy + "%"), ("Net Speed", result.NetWpm + " wpm") }) { Position(canvas, RefText(item.Item1, 14, true, true), 50, 143 + row * 25, 127, 24); Position(canvas, RefText(item.Item2, 13), 180, 143 + row++ * 25, 270, 24); }
        Position(canvas, RefText("Your Difficult Keys", 14, true, true), 50, 275, 380, 24); DrawDifficultKeys(canvas, 53, 299, 365, 113); Rule(canvas, 48, 450, 399);
        Position(canvas, RefButton("▸ View & Print Results", () => PreviewReport(ReportFactory.Result(Current!, result)), true), 48, 455, 241, 34);
        var certificate = RefButton("▸ View & Print Certificate", () => PreviewReport(ReportFactory.Certificate(Current!, result)), true); certificate.IsEnabled = result.Completed && result.Kind == "Test"; Position(canvas, certificate, 48, 494, 241, 34);
        if (!result.Passed && result.Completed)
        {
            Position(canvas, RefText("▣ Comment", 14, true, true), 523, 33, 235, 23);
            var reasons = new List<string>(); if (result.Accuracy < result.RequiredAccuracy) reasons.Add("To pass this exam your accuracy should have been at least " + result.RequiredAccuracy + "%."); if (result.NetWpm < result.RequiredWpm) reasons.Add("To pass this exam your typing speed should have been at least " + result.RequiredWpm + " wpm (Net Speed).");
            Position(canvas, RefText(string.Join("\n\n", reasons), 13), 523, 64, 235, 270);
        }
        Action close = () => { if (lessonId != null && activeLesson != null) NextLesson(); else ShowPage(page); };
        Position(canvas, RefButton("OK", close), 584, 440, 180, 34); var cancel = RefButton("Cancel", close, true); cancel.HorizontalContentAlignment = HorizontalAlignment.Center; cancel.IsEnabled = false; Position(canvas, cancel, 584, 484, 180, 34); ReferenceClose(canvas, 746, 13, close);
    }
    void DrawDifficultKeys(Canvas parent, double x, double y, double width, double height)
    {
        var chart = new Canvas { Background = Brushes.White, ClipToBounds = false }; Position(parent, new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Child = chart }, x, y, width, height);
        for (int i = 1; i <= 2; i++) { Rule(chart, 0, height * i / 3, width - 2, Brushes.YellowGreen); var label = RefText(i == 1 ? "Problematic" : "Difficult", 12, true); label.Foreground = Brushes.YellowGreen; label.TextAlignment = TextAlignment.Right; Position(chart, label, width - 110, height * i / 3 - 26, 105, 22); }
        var okay = RefText("OK", 12, true); okay.Foreground = Brushes.YellowGreen; Position(chart, okay, width - 26, height - 28, 25, 20);
        var items = Current!.Mistakes.OrderByDescending(k => k.Value).Take(10).ToList(); double max = Math.Max(1, items.Select(i => i.Value).DefaultIfEmpty(1).Max());
        for (int i = 0; i < items.Count; i++) { double h = Math.Max(3, items[i].Value / max * (height - 18)); var bar = new Border { Background = h > height * .67 ? Brushes.DeepPink : h > height * .33 ? Brushes.Gold : new SolidColorBrush(Color.FromRgb(170, 180, 240)), BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), ToolTip = items[i].Value + " recorded errors" }; Position(chart, bar, 2 + i * 33, height - h - 2, 26, h); Position(chart, RefText(items[i].Key == ' ' ? "␣" : items[i].Key.ToString(), 12), 10 + i * 33, height + 7, 28, 20); }
    }
    void Review()
    {
        var canvas = ReferencePage("Review"); var difficult = Current!.Mistakes.OrderByDescending(p => p.Value).Take(10).Select(p => p.Key).ToArray();
        Position(canvas, RefText("1", 46), 18, 69, 35, 65).Foreground = Brushes.CornflowerBlue; Position(canvas, RefText("Keys to Review", 14, true, true), 58, 73, 210, 24);
        Position(canvas, RefText("2", 46), 286, 69, 35, 65).Foreground = Brushes.CornflowerBlue; Position(canvas, RefText("Exercise Type", 14, true, true), 331, 73, 220, 24);
        string selection = "Difficult keys", mode = "Keyboard drill", game = "Clouds";
        RadioButton Radio(string title, string group, double x, double y, Action choose, bool selected = false) { var radio = new RadioButton { Content = title, GroupName = group, FontFamily = new FontFamily("Tahoma"), FontSize = 14, Style = (Style)Application.Current.FindResource("ReferenceRadio"), IsChecked = selected }; radio.Checked += (_, _) => choose(); Position(canvas, radio, x, y, 245, 23); return radio; }
        Radio("Difficult keys", "ReviewKeys", 58, 95, () => selection = "Difficult keys", true); var studied = Radio("Studied keys", "ReviewKeys", 58, 119, () => selection = "Studied keys"); studied.IsEnabled = Current.Completed.Count > 0; Radio("Choose keys", "ReviewKeys", 58, 143, () => selection = "Choose keys");
        Position(canvas, RefText(difficult.Length > 0 ? string.Concat(difficult) : "-", 13), 188, 97, 90, 22);
        var keys = Position(canvas, new TextBox { MaxLength = 40, Padding = new Thickness(2) }, 188, 142, 80, 27);
        Radio("Keyboard drill", "ReviewMode", 331, 97, () => mode = "Keyboard drill", true); Radio("Word drill", "ReviewMode", 331, 121, () => mode = "Word drill"); Radio("Game", "ReviewMode", 331, 145, () => mode = "Game"); Position(canvas, RefChoice(TypingGameEngine.Names, "Clouds", value => game = value), 430, 144, 145, 27);
        Position(canvas, RefText("3", 46), 154, 216, 35, 65).Foreground = Brushes.CornflowerBlue;
        Position(canvas, RefButton("Start Review", () => {
            string chars = selection == "Choose keys" ? keys.Text : selection == "Studied keys" ? string.Concat(allCourses.SelectMany(c => c.Chapters).SelectMany(c => c.Lessons).Where(l => Current.Completed.Contains(l.Id)).SelectMany(l => l.Keys)).ToLowerInvariant() : string.Concat(difficult);
            chars = new string(chars.Where(c => !char.IsControl(c) && c != ' ').Distinct().ToArray()); if (chars.Length == 0) { if (selection == "Choose keys") { Notify("Enter the keys to review."); return; } chars = "asdfjkl;"; }
            activeLesson = null; if (mode == "Game") { StartGame(game, "Review keys", chars); return; }
            string text;
            if (mode == "Word drill") { var vocabulary = System.Text.RegularExpressions.Regex.Split(Assets.Read(System.IO.Path.Combine(Assets.Root, "top1000wordlist.txt")), @"\s+").Where(w => w.Length is >= 2 and <= 16 && w.Any(c => chars.Contains(char.ToLowerInvariant(c)))).ToArray(); text = vocabulary.Length > 0 ? string.Join(" ", Enumerable.Range(0, 50).Select(_ => vocabulary[Random.Shared.Next(vocabulary.Length)])) : string.Join(" ", Enumerable.Range(0, 40).Select(_ => string.Concat(Enumerable.Range(0, 5).Select(_ => chars[Random.Shared.Next(chars.Length)])))); }
            else text = string.Join(" ", Enumerable.Range(0, 40).Select(_ => string.Concat(Enumerable.Range(0, 4).Select(_ => chars[Random.Shared.Next(chars.Length)]))));
            StartExercise(text, "Custom key review", 120, mode == "Keyboard drill" ? "ReviewKeys" : "Review");
        }), 197, 224, 180, 34);
        Rule(canvas, 0, 307, 593, Brushes.LightGray); Position(canvas, RefText("All Difficult Keys", 14, true, true), 42, 321, 470, 24);
        var keyboard = new KeyboardView(LoadKeyboard()) { MaxHeight = 175, Stretch = Stretch.Fill }; keyboard.Highlight(string.Concat(difficult)); Position(canvas, keyboard, 42, 345, 500, 175);
    }
}

