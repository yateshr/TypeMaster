using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TypeMaster.Native;
public sealed record KeyShape(string Caption, string Shift, int Finger, double X, double Y, double Width, double Height);
public sealed class KeyboardLayout
{
    static Dictionary<string,string>? names;
    internal static string DisplayName(string file)
    {
        if (names == null)
        {
            names = new(StringComparer.OrdinalIgnoreCase);
            string ini = Path.Combine(Assets.Root, "TmKeyb.ini");
            if (System.IO.File.Exists(ini)) foreach (string line in Assets.Read(ini).Split('\n')) { int split = line.IndexOf('='); if (split > 0) { string value = line[(split + 1)..].Split(';')[0].Trim(); if (value.EndsWith(".kbd", StringComparison.OrdinalIgnoreCase)) names.TryAdd(value, line[..split].Trim()); } }
        }
        return names.GetValueOrDefault(Path.GetFileName(file), Path.GetFileNameWithoutExtension(file));
    }
    public string File { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string HomeKeys { get; set; } = "asdfjkl;";
    public List<KeyShape> Keys { get; set; } = [];
    public override string ToString() => Name;
    public static KeyboardLayout Load(string file)
    {
        var layout = new KeyboardLayout { File = Path.GetFileName(file), Name = DisplayName(file) };
        double x = 0, y = 0, w = 32, h = 34, rowBottom = 0, dw = 32, dh = 34;
        foreach (string raw in Assets.Read(file).Split('\n'))
        {
            string line = raw.Trim(); var p = line.Replace("%;", "\u0001").Split(';').Select(v => v.Replace('\u0001', ';')).ToArray();
            if (p.Length < 2) continue;
            if (p[0].Equals("layoutcode", StringComparison.OrdinalIgnoreCase)) layout.Code = p[1];
            if (p[0].Equals("HomeRow", StringComparison.OrdinalIgnoreCase)) layout.HomeKeys = p[1];
            if (p[0] == "DefaultSize" && p.Length > 2) { double.TryParse(p[1], out dw); double.TryParse(p[2], out dh); continue; }
            if (!p[0].EndsWith("Key", StringComparison.OrdinalIgnoreCase) || p.Length < 7) continue;
            int.TryParse(p[1], out int finger);
            static double Value(string s, double fallback) => double.TryParse(s, out double n) ? n : fallback;
            x = p[2].StartsWith('+') ? x + w + Value(p[2][1..], 1) : p[2] == "0" ? x : Value(p[2], x);
            y = p[3].StartsWith('+') ? rowBottom + Value(p[3][1..], 1) : p[3] == "0" ? y : Value(p[3], y);
            w = p[4] == "-" ? dw : p[4] == "0" ? w : Value(p[4], dw);
            h = p[5] == "-" ? dh : p[5] == "0" ? h : Value(p[5], dh);
            rowBottom = Math.Max(rowBottom, y + h);
            string caption = p[0] switch { "SpaceKey" => "Space", "BackSpaceKey" => "Back", "EnterKey" => "Enter", "TabKey" => "Tab", "CapsLockKey" => "Caps", "ControlKey" => p.Length > 8 ? p[8] : "Ctrl", _ => p.Length > 7 ? p[7] : "" };
            string shift = p[0] == "Key" && p.Length > 8 ? p[8] : "";
            if (p[6].Contains('i')) continue;
            layout.Keys.Add(new KeyShape(caption, shift, finger, x, y, Math.Clamp(w, 5, 500), Math.Clamp(h, 5, 100)));
            if (layout.Keys.Count > 300) throw new InvalidDataException("Keyboard layout contains too many keys.");
        }
        if (layout.Keys.Count == 0) throw new InvalidDataException("No keys were found in the layout file.");
        return layout;
    }
}
public sealed class KeyboardView : Viewbox
{
    readonly List<(KeyShape Shape, Border Border, Brush Background)> keys = [];
    public KeyboardView(KeyboardLayout layout)
    {
        Stretch = Stretch.Uniform; HorizontalAlignment = HorizontalAlignment.Left; MaxHeight = 180;
        var canvas = new Canvas { Width = layout.Keys.Max(k => k.X + k.Width) + 2, Height = layout.Keys.Max(k => k.Y + k.Height) + 2 };
        string[] colors = ["#C3C8FF", "#B6C3FF", "#F0B6B6", "#C5E2A4", "#D7B8F2", "#D7B8F2", "#C5E2A4", "#F0B6B6", "#B6C3FF", "#E5DCC0"];
        foreach (var k in layout.Keys)
        {
            bool letter = k.Caption.Length == 1 && char.IsLetter(k.Caption[0]);
            string caption = letter ? k.Caption.ToUpperInvariant() : k.Caption;
            if (Localization.Language == "ger") caption = caption switch { "Back" => "Rücktaste", "Enter" => "Eingabe", "Space" => "Leertaste", "Ctrl" => "Strg", _ => caption };
            var label = new TextBlock { Text = (!letter && k.Shift.Length > 0 ? k.Shift + "\n" : "") + caption, FontFamily = new FontFamily("Arial"), FontSize = !letter && k.Shift.Length > 0 ? 10 : 13, Foreground = Brushes.DarkBlue, Padding = new Thickness(2, 0, 0, 0) };
            var face = new Border { Child = label, BorderBrush = new SolidColorBrush(Color.FromRgb(235, 234, 255)), BorderThickness = new Thickness(4, 3, 0, 0), Margin = new Thickness(0, 0, 3, 3) };
            var border = new Border { Width = k.Width, Height = k.Height, Child = face, BorderBrush = new SolidColorBrush(Color.FromRgb(150, 147, 243)), BorderThickness = new Thickness(1), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[Math.Clamp(k.Finger, 0, 9)])), ToolTip = "Finger " + k.Finger };
            Canvas.SetLeft(border, k.X); Canvas.SetTop(border, k.Y); canvas.Children.Add(border); keys.Add((k, border, border.Background));
        }
        Child = canvas;
    }
    public void Highlight(string characters)
    {
        foreach (var (shape, border, background) in keys)
        {
            bool active = (shape.Caption == "Space" && characters.Contains(' ')) || shape.Caption.Length == 1 && characters.Contains(shape.Caption, StringComparison.OrdinalIgnoreCase) || shape.Shift.Length == 1 && characters.Contains(shape.Shift);
            border.BorderBrush = Brushes.MediumSlateBlue; border.BorderThickness = new Thickness(1); border.Background = active ? Brushes.MediumPurple : background;
            if (border.Child is Border face && face.Child is TextBlock label) { label.Foreground = active ? Brushes.White : Brushes.DarkBlue; label.FontWeight = active ? FontWeights.Bold : FontWeights.Normal; }
        }
    }
}

