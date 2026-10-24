using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TypeMaster.Native;
public partial class MainWindow
{
    void MeterScreen()
    {
        if (meterTab == "Settings" || meter == null && meterTab == "Training") { ReferenceMeterLaunch(); return; }
        if (meterTab != "Training") { PageScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; MeterScreenDetails(); return; }
        var canvas = ReferencePage("Typing Meter");
        foreach (var tab in new[] { ("Training", 18d, 151d), ("Analysis", 175d, 151d), ("History", 331d, 118d), ("Key-to-Key", 454d, 125d) }) { var button = RefButton(tab.Item1, () => { meterTab = tab.Item1; MeterScreen(); }); button.FontWeight = tab.Item1 == "Training" ? FontWeights.Bold : FontWeights.Normal; Position(canvas, button, tab.Item2, 48, tab.Item3, 28); }
        Rule(canvas, 0, 76, 593); var body = new Border(); body.SetResourceReference(Border.BackgroundProperty, "SoftBrush"); Position(canvas, body, 7, 81, 578, 368);
        Position(canvas, RefTitle("Training"), 18, 84, 400, 34).FontSize = 23;
        Position(canvas, RefText("Hone the keys and words that Typing Meter has found difficult.", 13), 18, 114, 558, 24); Rule(canvas, 18, 143, 558);
        var difficult = Current!.Meter.SelectMany(d => d.CorrectedKeys).GroupBy(p => p.Key).Select(g => (Key: g.Key, Count: g.Sum(x => x.Value))).OrderByDescending(p => p.Count).Take(8).ToList();
        Position(canvas, Picture("checkmark.gif", 16, 16), 18, 153, 16, 16);
        Position(canvas, RefText(difficult.Count == 0 ? "Training Not Necessary" : "Focused Training Recommended", 14, true, true), 40, 153, 500, 24);
        Position(canvas, RefText(difficult.Count == 0 ? "No problems found yet" : "Review frequently corrected keys", 13), 40, 174, 500, 24);
        int row = 0; foreach (string label in new[] { "Keys", "Capital letters", "Words" })
        {
            double y = 201 + row * 24; Position(canvas, RefText("●", 12), 42, y - 1, 15, 20).Foreground = Brushes.SlateBlue; Position(canvas, RefText(label, 13), 56, y, 112, 21);
            Position(canvas, new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Background = Brushes.White }, 168, y + 4, 100, 9);
            string status = row == 0 && difficult.Count > 0 ? string.Join(" ", difficult.Select(k => KeyName(k.Key))) : row == 2 ? "No words collected" : "No problems found";
            Position(canvas, RefText(status, 13), 280, y, 280, 22); row++;
        }
        var analysis = RefButton("Detailed analysis", () => { meterTab = "Analysis"; MeterScreen(); }, true); analysis.Tag = new CornerRadius(3); analysis.BorderBrush = Brushes.LightGray; analysis.Foreground = Brushes.Black; analysis.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, analysis, 89, 286, 178, 32);
        var heat = RefButton("Bigram Heat Map", () => { meterTab = "Key-to-Key"; MeterScreen(); }, true); heat.Tag = new CornerRadius(3); heat.BorderBrush = Brushes.LightGray; heat.Foreground = Brushes.Black; heat.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, heat, 321, 286, 178, 32); Rule(canvas, 18, 327, 558);
        Position(canvas, RefText("1", 60, true), 18, 350, 35, 70).Foreground = Brushes.RoyalBlue; Position(canvas, new Border { Background = Brushes.CornflowerBlue }, 48, 359, 1, 45);
        Position(canvas, RefText("Choose training duration", 14, true, true), 56, 366, 250, 22); int minutes = 10;
        Position(canvas, RefChoice(["1 minute", "2 minutes", "5 minutes", "10 minutes"], "10 minutes", v => minutes = int.Parse(v.Split(' ')[0])), 56, 388, 164, 24);
        Position(canvas, RefText("2", 60, true), 281, 350, 35, 70).Foreground = Brushes.RoyalBlue; Position(canvas, new Border { Background = Brushes.CornflowerBlue }, 312, 359, 1, 57);
        Position(canvas, RefButton("Start Training", () => { string keys = string.Concat(difficult.Select(k => k.Key is >= 65 and <= 90 ? char.ToLowerInvariant((char)k.Key) : ' ')).Trim(); if (keys.Length == 0) keys = "asdfjkl;"; string text = string.Join(" ", Enumerable.Range(0, 100).Select(_ => string.Concat(Enumerable.Range(0, 5).Select(_ => keys[Random.Shared.Next(keys.Length)])))); StartExercise(text, "Typing Meter training", minutes * 60); }), 320, 369, 180, 34);
        Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(213, 229, 250)) }, 7, 449, 578, 73); Rule(canvas, 7, 449, 578);
        Position(canvas, RefText("Typing Meter Status", 14, true, true), 40, 463, 210, 22); Position(canvas, RefText("● " + (meter?.Paused == true ? "Paused" : "Launched") + "\n● User: " + Current.Name, 13), 40, 486, 210, 39);
        Position(canvas, new Border { Background = Brushes.CornflowerBlue }, 237, 458, 1, 64); Position(canvas, new Border { Background = Brushes.CornflowerBlue }, 414, 458, 1, 64);
        void Link(string text, double x, double y, double width, Action action) { var button = RefButton("▸ " + text, action, true); button.Tag = new CornerRadius(0); button.BorderThickness = new Thickness(0); button.Foreground = Brushes.Blue; Position(canvas, button, x, y, width, 22); }
        Link("Settings", 240, 470, 170, () => { meterTab = "Settings"; MeterScreen(); }); Link("Remove", 240, 492, 170, () => { StopMeter(); meterTab = "Training"; MeterScreen(); }); Link("What is Typing Meter?", 416, 470, 168, () => { meterTab = "Settings"; MeterScreen(); });
    }
    void ReferenceMeterLaunch()
    {
        var canvas = ReferencePage("Typing Meter"); Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(213, 229, 250)) }, 282, 1, 303, 528);
        Position(canvas, new Image { Source = new CroppedBitmap(AssetBitmap("ReferenceUI/meter-reference.png"), new Int32Rect(360, 246, 236, 133)), Stretch = Stretch.Fill }, 29, 74, 236, 133);
        var meterHeading = RefText("What is Typing Meter Widget?", 14, true); meterHeading.FontFamily = new FontFamily("Arial"); Position(canvas, meterHeading, 29, 211, 248, 26).Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 204));
        var body = RefText("Typing Meter is a handy Windows widget that measures and analyzes your typing habits in the background while you work.\n\nIt gathers basic typing stats, and also detects those keys and key pairs that are problematic for you.\n\nWhen recurring problems are detected, Typing Meter suggests a quick training session to improve those problem areas.", 13); body.FontFamily = new FontFamily("Arial"); body.FontSize = 14; body.LineHeight = 17; Position(canvas, body, 29, 239, 241, 224);
        Position(canvas, RefButton("▸ More information", () => Notify("The meter saves aggregate key counts and timing only. Use its visible widget or tray icon to pause or stop it. Backspace use provides correction hints. Typed words are not collected.", "Typing Meter"), true), 56, 467, 170, 34);
        Position(canvas, RefTitle("Launch Typing Meter"), 294, 10, 285, 33).FontSize = 23; Rule(canvas, 290, 43, 289);
        Position(canvas, RefText("Automatic Launch", 14, true, true), 294, 53, 285, 22); Position(canvas, RefText("When user \"" + Current!.Name + "\" starts", 13), 294, 76, 280, 38);
        using var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); bool enabled = registry?.GetValue(StartupKey) is string command && command.Contains(Current!.Id.ToString());
        Position(canvas, RefChoice(["Do not launch Typing Meter", "Launch at Windows startup"], enabled ? "Launch at Windows startup" : "Do not launch Typing Meter", value => { using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); if (value.StartsWith("Launch")) key.SetValue(StartupKey, "\"" + Environment.ProcessPath + "\" --meter-profile " + Current!.Id); else key.DeleteValue(StartupKey, false); }), 294, 95, 282, 24);
        Position(canvas, RefText("●", 11), 294, 134, 15, 18).Foreground = Brushes.SlateBlue; Position(canvas, RefText(enabled ? "Typing Meter will launch when you sign in to Windows." : "Typing Meter is not automatically launched on Windows start up. To use Typing Meter, you need to launch it from TypeMaster.", 13), 308, 134, 264, 93);
        Rule(canvas, 290, 245, 289, Brushes.White); Position(canvas, RefText("Set Notifications", 14, true, true), 294, 252, 282, 22);
        var train = new CheckBox { Content = "Notify me when training is needed", IsChecked = Current!.TrainingReminder, FontSize = 13, FontFamily = new FontFamily("Verdana") }; train.Style = (Style)Application.Current.FindResource("ReferenceCheck"); train.Click += (_, _) => { Current.TrainingReminder = train.IsChecked == true; Save(); }; Position(canvas, train, 294, 274, 287, 24);
        var rest = new CheckBox { Content = "Notify me to take a break every...", IsChecked = Current.BreakReminder, FontSize = 13, FontFamily = new FontFamily("Verdana") }; rest.Style = (Style)Application.Current.FindResource("ReferenceCheck"); rest.Click += (_, _) => { Current.BreakReminder = rest.IsChecked == true; Save(); }; Position(canvas, rest, 294, 300, 287, 24);
        Position(canvas, RefChoice(["30 minutes", "1 hour", "2 hours"], Current.BreakMinutes == 30 ? "30 minutes" : Current.BreakMinutes == 60 ? "1 hour" : "2 hours", v => { Current.BreakMinutes = v == "30 minutes" ? 30 : v == "1 hour" ? 60 : 120; Save(); }), 318, 321, 178, 24);
        Rule(canvas, 290, 352, 289, Brushes.White); var privacy = RefButton("Security ▸ Privacy Information", () => Notify("Only aggregate key counts and pair timings are stored on your Windows account. No words, documents, or ordered keystroke stream are saved."), true); privacy.Background = Brushes.Transparent; privacy.BorderThickness = new Thickness(0); privacy.Padding = new Thickness(0); privacy.FontFamily = new FontFamily("Georgia"); privacy.FontSize = 14; Position(canvas, privacy, 294, 356, 282, 24);
        Position(canvas, RefChoice(["Do not collect any words"], "Do not collect any words", _ => { }), 294, 380, 278, 24); Position(canvas, RefText("No words are stored. You'll still see\nproblem keys and key pairs.", 13), 294, 412, 282, 40); Rule(canvas, 290, 454, 289);
        Position(canvas, RefButton(meter == null ? "Launch" : "Show Widget", () => { if (meter == null) StartMeter(); else meterWidget?.Show(); meterTab = "Training"; MeterScreen(); }), 341, 467, 180, 34);
        var skip = RefButton("Not now", () => ShowPage("Studying"), true); skip.Background = Brushes.Transparent; skip.Foreground = Brushes.Blue; skip.BorderThickness = new Thickness(0); skip.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, skip, 330, 506, 200, 20);
    }
}



