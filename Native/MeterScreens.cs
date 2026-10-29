using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace TypeMaster.Native;
public partial class MainWindow
{
    WindowsTypingMeter? meter;
    MeterWidget? meterWidget;
    Forms.NotifyIcon? tray;
    readonly DispatcherTimer meterTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    bool quitting;
    int meterTicks;
    DateTime lastBreak, lastTrainingNotice;
    string meterTab = "Training";
    string StartupKey => "TypeMasterNativeMeter";
    void MeterScreenDetails()
    {
        Heading("Typing Meter");
        var tabs = new WrapPanel(); foreach (string title in new[] { "Training", "Analysis", "History", "Key-to-Key", "Settings" }) tabs.Children.Add(Button(title, () => { meterTab = title; MeterScreen(); })); Add(tabs);
        if (meterTab == "Settings" || meter == null && meterTab == "Training") { MeterSettings(); return; }
        var days = Current!.Meter; var today = meter?.Today ?? days.FirstOrDefault(d => d.Date == DateTime.Today.ToString("yyyy-MM-dd")) ?? new MeterDay();
        if (meterTab == "Training")
        {
            Add(Text("Training", 23)); Add(Text("Hone the keys that need more practice."));
            var difficult = days.SelectMany(d => d.CorrectedKeys).GroupBy(p => p.Key).Select(g => (Key: g.Key, Count: g.Sum(x => x.Value))).OrderByDescending(p => p.Count).Take(8).ToList();
            Add(Text(difficult.Count > 0 ? "Suggested keys: " + string.Join(", ", difficult.Select(p => KeyName(p.Key))) : "✓  Training Not Necessary — no problems found yet.", 16, true));
            var analysisButtons = new WrapPanel(); analysisButtons.Children.Add(Outline("Detailed analysis", () => { meterTab = "Analysis"; MeterScreen(); })); analysisButtons.Children.Add(Outline("Bigram Heat Map", () => { meterTab = "Key-to-Key"; MeterScreen(); })); Add(analysisButtons);
            int minutes = 10; Add(Text("Choose training duration", 15, true)); var trainingRow = new WrapPanel(); trainingRow.Children.Add(Choice(["1", "2", "5", "10"], "10", v => minutes = int.Parse(v)));
            trainingRow.Children.Add(Button("Start Training", () => { string letters = string.Concat(difficult.Select(p => p.Key is >= 65 and <= 90 ? char.ToLowerInvariant((char)p.Key) : ' ')).Trim(); if (letters.Length == 0) letters = "asdfjkl;"; string text = string.Join(" ", Enumerable.Range(0, 100).Select(_ => string.Concat(Enumerable.Range(0, 5).Select(_ => letters[Random.Shared.Next(letters.Length)])))); StartExercise(text, "Typing Meter training", minutes * 60); })); Add(trainingRow);
            Note("Backspace use provides correction hints, not an accuracy score.");
        }
        else if (meterTab == "Analysis")
        {
            Add(Text($"Today\nKeystrokes: {today.Keys}\nEstimated words: {today.Keys / 5}\nActive typing time: {TimeSpan.FromSeconds(today.ActiveSeconds):hh\\:mm\\:ss}\nBackspaces: {today.Corrections}", 18));
            var counts = days.SelectMany(d => d.KeyCounts).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Sum(x => x.Value));
            var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 9 };
            int max = Math.Max(1, counts.Values.DefaultIfEmpty(0).Max());
            foreach (int key in Enumerable.Range(65, 26)) grid.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb((byte)(50 + 205 * counts.GetValueOrDefault(key) / max), 25, 105, 225)), Margin = new Thickness(2), Padding = new Thickness(4), Child = new TextBlock { Text = KeyName(key) + "\n" + counts.GetValueOrDefault(key), Foreground = Brushes.White, TextAlignment = TextAlignment.Center } }); Add(grid);
        }
        else if (meterTab == "History")
        {
            var grid = new DataGrid { AutoGenerateColumns = true, IsReadOnly = true, MaxHeight = 300, ItemsSource = days.OrderByDescending(d => d.Date).Select(d => new { d.Date, Keystrokes = d.Keys, Words = d.Keys / 5, ActiveMinutes = Math.Round(d.ActiveSeconds / 60, 1), Backspaces = d.Corrections }).ToList() }; Add(grid);
            Add(Outline("Clear meter history", () => { if (MessageBox.Show(this, "Clear this profile's meter statistics?", "Clear history", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { bool restart = meter != null; StopMeter(false); Current.Meter.Clear(); Save(); if (restart) StartMeter(); MeterScreen(); } }));
        }
        else
        {
            Add(Text("Key-to-Key Timing", 20, true));
            var pairs = days.SelectMany(d => d.Pairs).GroupBy(p => p.Key).Select(g => new { Pair = string.Join(" → ", g.Key.Split(':').Select(x => KeyName(int.Parse(x)))), Samples = g.Sum(x => x.Value.Count), AverageMs = Math.Round(g.Sum(x => x.Value.TotalMilliseconds) / Math.Max(1, g.Sum(x => x.Value.Count))) }).Where(p => p.Samples >= 3).OrderByDescending(p => p.AverageMs).Take(30).ToList();
            Add(new DataGrid { ItemsSource = pairs, IsReadOnly = true, AutoGenerateColumns = true, MaxHeight = 260 });
            Note("Pairs appear after at least three samples. Only aggregate key-pair counts and timing are stored; words and ordered text are not stored.");
        }
        Add(Text("Typing Meter Status: " + (meter == null ? "Stopped" : meter.Paused ? "Paused" : "Running"), 14, true));
        var buttons = new WrapPanel(); buttons.Children.Add(Button(meter == null ? "Launch" : meter.Paused ? "Resume" : "Pause", () => { if (meter == null) StartMeter(); else ToggleMeter(); MeterScreen(); })); buttons.Children.Add(Outline("Stop Meter", () => { StopMeter(); MeterScreen(); })); Add(buttons);
    }
    void MeterSettings()
    {
        var layout = new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition()); layout.ColumnDefinitions.Add(new ColumnDefinition());
        var left = new StackPanel { Margin = new Thickness(0, 0, 12, 0) }; var right = new StackPanel { Margin = new Thickness(10, 0, 0, 0) }; Grid.SetColumn(right, 1);
        left.Children.Add(Picture("spmeter1.bmp", 245, 102)); left.Children.Add(Text("What is Typing Meter?", 15, true)); left.Children.Add(Text("Measure your typing activity while you work in other Windows applications. Review frequently corrected keys and slow key pairs, then practice them here."));
        left.Children.Add(Text("The visible widget and tray icon let you pause or stop at any time. Only aggregate key counts and timing are saved locally. Typed words and documents are not recorded.", 13));
        right.Children.Add(Text("Launch Typing Meter", 20));
        using var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        bool startup = registry?.GetValue(StartupKey) is string value && value.Contains(Current!.Id.ToString());
        right.Children.Add(Text("Automatic Launch", 14, true));
        right.Children.Add(Choice(["Do not launch at sign-in", "Launch at Windows sign-in"], startup ? "Launch at Windows sign-in" : "Do not launch at sign-in", option =>
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (option.StartsWith("Launch")) key.SetValue(StartupKey, "\"" + Environment.ProcessPath + "\" --meter-profile " + Current!.Id); else key.DeleteValue(StartupKey, false);
        }));
        var reminders = new CheckBox { Content = "Remind me to take a break", IsChecked = Current!.BreakReminder, Margin = new Thickness(0, 8, 0, 8) }; reminders.SetResourceReference(ForegroundProperty, "TextBrush"); reminders.Click += (_, _) => { Current.BreakReminder = reminders.IsChecked == true; Save(); }; right.Children.Add(reminders);
        right.Children.Add(Choice(["30", "60", "120"], Current.BreakMinutes.ToString(), v => { Current.BreakMinutes = int.Parse(v); Save(); })); right.Children.Add(Text("Minutes between break reminders", 12));
        var training = new CheckBox { Content = "Notify me when training is useful", IsChecked = Current.TrainingReminder, Margin = new Thickness(0, 8, 0, 8) }; training.SetResourceReference(ForegroundProperty, "TextBrush"); training.Click += (_, _) => { Current.TrainingReminder = training.IsChecked == true; Save(); }; right.Children.Add(training);
        right.Children.Add(Text("Privacy: Do not collect words", 13, true));
        right.Children.Add(Button(meter == null ? "Launch" : "Show Widget", () => { if (meter == null) StartMeter(); else meterWidget?.Show(); meterTab = "Training"; MeterScreen(); }));
        if (meter != null) right.Children.Add(Outline("Stop Meter", () => { StopMeter(); MeterScreen(); }));
        layout.Children.Add(left); layout.Children.Add(right); Add(layout);
    }
    internal void StartMeterForProfile(Guid id)
    {
        var profile = Store.Profiles.FirstOrDefault(p => p.Id == id); if (profile == null) return;
        EnterProfile(profile.Name); StartMeter(); Hide();
    }
    internal void StartMeter()
    {
        if (Current == null || meter != null) return;
        var service = new WindowsTypingMeter(Current); service.Start(); meter = service;
        meterWidget = new MeterWidget(ToggleMeter, ShowMain, () => StopMeter()); meterWidget.Show();
        tray = new Forms.NotifyIcon { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Text = "TypeMaster — Typing Meter running", Visible = true };
        var menu = new Forms.ContextMenuStrip(); menu.Items.Add("Open TypeMaster", null, (_, _) => Dispatcher.Invoke(ShowMain)); menu.Items.Add("Pause / Resume", null, (_, _) => Dispatcher.Invoke(ToggleMeter)); menu.Items.Add("Stop Meter", null, (_, _) => Dispatcher.Invoke(() => StopMeter())); menu.Items.Add("Exit TypeMaster", null, (_, _) => Dispatcher.Invoke(() => { ShowMain(); Close(); })); tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMain);
        lastBreak = lastTrainingNotice = DateTime.Now; meterTicks = 0;
        meterTimer.Tick -= MeterTick; meterTimer.Tick += MeterTick; meterTimer.Start(); MeterTick(null, EventArgs.Empty);
    }
    void MeterTick(object? sender, EventArgs e)
    {
        if (meter == null || Current == null) return;
        var today = meter.Today; meterWidget?.Update(today, meter.Paused); meterTicks++;
        if (meterTicks % 30 == 0) Save();
        if (!meter.Paused && Current.BreakReminder && DateTime.Now - lastBreak > TimeSpan.FromMinutes(Current.BreakMinutes)) { tray?.ShowBalloonTip(5000, "Time for a short break", "Relax your hands and look away from the screen for a moment.", Forms.ToolTipIcon.Info); lastBreak = DateTime.Now; }
        if (!meter.Paused && Current.TrainingReminder && today.Corrections >= 20 && DateTime.Now - lastTrainingNotice > TimeSpan.FromMinutes(30)) { tray?.ShowBalloonTip(5000, "A short practice may help", "Open Typing Meter > Training to review frequently corrected keys.", Forms.ToolTipIcon.Info); lastTrainingNotice = DateTime.Now; }
    }
    void ShowMain() { Show(); WindowState = WindowState.Normal; Activate(); }
    void ToggleMeter() { if (meter == null) return; meter.Pause(!meter.Paused); if (tray != null) tray.Text = "TypeMaster — " + (meter.Paused ? "Paused" : "Typing Meter running"); meterWidget?.Update(meter.Today, meter.Paused); }
    internal void StopMeter(bool showMain = true)
    {
        meterTimer.Stop(); bool hadMeter = meter != null; meter?.Dispose(); meter = null; meterWidget?.Close(); meterWidget = null; var trayIcon=tray?.Icon;tray?.Dispose();if(trayIcon!=null&&!ReferenceEquals(trayIcon,System.Drawing.SystemIcons.Application))trayIcon.Dispose();tray = null;
        if (hadMeter) { Save(); if (showMain && !quitting) ShowMain(); }
    }
    static string KeyName(int key) => key == 32 ? "Space" : key == 8 ? "Backspace" : KeyInterop.KeyFromVirtualKey(key).ToString();
}


