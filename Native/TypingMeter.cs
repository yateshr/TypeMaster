using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace TypeMaster.Native;
public sealed class MeterAccumulator
{
    readonly Profile profile;
    int previous;
    DateTime last;
    public MeterAccumulator(Profile owner) => profile = owner;
    public MeterDay Today
    {
        get { string date = DateTime.Today.ToString("yyyy-MM-dd"); var day = profile.Meter.FirstOrDefault(d => d.Date == date); if (day == null) { day = new MeterDay { Date = date }; profile.Meter.Add(day); } return day; }
    }
    public void ResetSequence() { previous = 0; last = default; }
    public void Record(int key, DateTime time)
    {
        var day = Today;
        double gap = last == default ? 0 : (time - last).TotalSeconds;
        day.Keys++; if (gap > 0 && gap < 5) day.ActiveSeconds += gap;
        day.KeyCounts[key] = day.KeyCounts.GetValueOrDefault(key) + 1;
        if (key == 8)
        {
            day.Corrections++;
            if (previous >= 32 && gap < 2) day.CorrectedKeys[previous] = day.CorrectedKeys.GetValueOrDefault(previous) + 1;
        }
        if (previous >= 32 && key >= 32 && gap > .02 && gap < 2)
        {
            string pair = previous + ":" + key;
            if (!day.Pairs.TryGetValue(pair, out var timing)) { timing = new PairTiming(); day.Pairs[pair] = timing; }
            timing.Count++; timing.TotalMilliseconds += gap * 1000;
        }
        previous = key; last = time;
    }
}
// WH_KEYBOARD_LL runs on the WPF message loop. The callback does no disk I/O.
// https://learn.microsoft.com/windows/win32/winmsg/lowlevelkeyboardproc
public sealed class WindowsTypingMeter : IDisposable
{
    delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    readonly HookProc callback;
    readonly MeterAccumulator accumulator;
    IntPtr hook;
    IntPtr lastWindow;
    public bool Paused { get; private set; }
    public bool IsRunning => hook != IntPtr.Zero;
    public WindowsTypingMeter(Profile profile) { accumulator = new MeterAccumulator(profile); callback = OnKey; }
    public MeterDay Today => accumulator.Today;
    public void Start() { if (IsRunning) return; hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0); if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    public void Pause(bool value) { Paused = value; accumulator.ResetSequence(); }
    IntPtr OnKey(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && !Paused && message.ToInt64() == 0x100)
        {
            int key = Marshal.ReadInt32(data), flags = Marshal.ReadInt32(data, 8);
            IntPtr foreground = GetForegroundWindow();
            if (foreground != lastWindow) { accumulator.ResetSequence(); lastWindow = foreground; }
            GetWindowThreadProcessId(foreground, out uint process);
            bool shortcut = (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
            // Ignore synthetic keys, app practice, modifiers, and shortcut combinations. Never translate keys into text.
            if ((flags & 0x10) == 0 && process != Environment.ProcessId && !shortcut && (key is >= 0x30 and <= 0x5A || key is >= 0x60 and <= 0x6F || key is >= 0xBA and <= 0xE2 || key is 8 or 32))
                accumulator.Record(key, DateTime.Now);
            else accumulator.ResetSequence();
        }
        return CallNextHookEx(hook, code, message, data);
    }
    public void Dispose() { if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } accumulator.ResetSequence(); }
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
}
public sealed class MeterWidget : Window
{
    readonly TextBlock speed, words, time, status;
    public MeterWidget(Action toggle, Action show, Action stop)
    {
        Title = "TypeMaster Typing Meter"; Width = 310; Height = 150; ResizeMode = ResizeMode.NoResize; WindowStyle = WindowStyle.None; Topmost = true; ShowInTaskbar = false; Background = Brushes.Black;
        Left = Math.Max(0, SystemParameters.WorkArea.Right - Width - 20); Top = Math.Max(0, SystemParameters.WorkArea.Bottom - Height - 20);
        var grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(102) }); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        var body = new Grid { Width = 255, HorizontalAlignment = HorizontalAlignment.Left };
        body.Children.Add(new Image { Source = new BitmapImage(new Uri(Path.Combine(Assets.Root, "spmeter1.bmp"))), Width = 255, Height = 102 });
        TextBlock Overlay(string value, double x, double y, double size)
        { var label = new TextBlock { Text = value, Foreground = Brushes.White, FontSize = size, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(x, y, 0, 0), Background = Brushes.Black }; body.Children.Add(label); return label; }
        speed = Overlay("0", 27, 62, 22); words = Overlay("0", 169, 26, 12); time = Overlay("00:00", 169, 43, 12); status = Overlay("0 WPM", 169, 62, 12);
        grid.Children.Add(body);
        var buttons = new WrapPanel { Margin = new Thickness(3, 0, 0, 0) }; Grid.SetRow(buttons, 1);
        foreach (var (name, action) in new (string, Action)[] { ("Pause", toggle), ("Open", show), ("Stop", stop) }) { var b = new Button { Content = name, Padding = new Thickness(10, 3, 10, 3), MinHeight = 28, FontSize = 12 }; b.Click += (_, _) => action(); buttons.Children.Add(b); }
        grid.Children.Add(buttons); Content = grid;
        body.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
    }
    public void Update(MeterDay day, bool paused)
    {
        int wpm = day.ActiveSeconds > 0 ? (int)Math.Round(day.Keys / 5d / (day.ActiveSeconds / 60)) : 0;
        speed.Text = paused ? "Ⅱ" : Math.Min(999, wpm).ToString(); words.Text = (day.Keys / 5).ToString(); time.Text = TimeSpan.FromSeconds(day.ActiveSeconds).ToString(@"hh\:mm"); status.Text = paused ? "PAUSED" : wpm + " WPM";
    }
}

