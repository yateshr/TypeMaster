using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TypeMaster.Native;
public sealed class GameToken
{
    public string Word { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public int Color { get; set; }
}
public sealed class TypingGameEngine
{
    public static readonly string[] Names = ["Bubbles", "WordTris", "Clouds", "ABC Speed Race", "Pipe Game", "Cloud Race"];
    public string Mode { get; }
    public List<GameToken> Tokens { get; } = [];
    public int Score { get; private set; }
    public int Errors { get; private set; }
    public int Lives { get; private set; } = 5;
    public int Completed { get; private set; }
    public int Stacked { get; private set; }
    public double Elapsed { get; private set; }
    public bool Over { get; private set; }
    public double Player { get; private set; }
    public double Opponent => Math.Min(1, Elapsed / 60);
    public string Expected => Mode == "ABC Speed Race" ? (Completed < 26 ? ((char)('a' + Completed)).ToString() : "") : Tokens.FirstOrDefault()?.Word ?? "";
    readonly Random random;
    readonly string[] words;
    readonly string letters;
    double spawn;
    public TypingGameEngine(string mode, string[] vocabulary, string characters = "abcdefghijklmnopqrstuvwxyz", int? seed = null)
    {
        if (!Names.Contains(mode)) throw new ArgumentException("Unknown game");
        Mode = mode; words = vocabulary.Where(w => w.Length > 0).DefaultIfEmpty("type").ToArray(); letters = characters.Length > 0 ? characters : "abc"; random = seed.HasValue ? new Random(seed.Value) : new Random();
        if (Mode == "Pipe Game") for (int i = 0; i < 32; i++) Tokens.Add(new GameToken { Word = letters[random.Next(letters.Length)].ToString(), X = i % 8, Y = i / 8 });
        else if (Mode != "ABC Speed Race") Spawn();
    }
    void Spawn()
    {
        string word = Mode == "Bubbles" ? letters[random.Next(letters.Length)].ToString() : words[random.Next(words.Length)];
        Tokens.Add(new GameToken { Word = word, X = Mode == "Clouds" ? -0.15 : .08 + random.NextDouble() * .70, Y = Mode == "Bubbles" ? .95 : Mode == "Clouds" ? .15 + random.NextDouble() * .60 : 0, Color = random.Next(5) });
    }
    public void Advance(double seconds)
    {
        if (Over) return; Elapsed += seconds;
        if (Mode == "ABC Speed Race" || Mode == "Pipe Game" || Mode == "Cloud Race") { if (Elapsed >= (Mode == "ABC Speed Race" ? 120 : 60)) Over = true; return; }
        spawn += seconds;
        if (spawn > (Mode == "Bubbles" ? 1.8 : 3.2)) { spawn = 0; Spawn(); }
        foreach (var token in Tokens.ToArray())
        {
            if (Mode == "Bubbles") token.Y -= seconds * (.07 + Completed * .002);
            else if (Mode == "Clouds") token.X += seconds * (.05 + Completed * .001);
            else token.Y += seconds * (.045 + Completed * .001);
            if (token.Y < -.1 || token.Y > (1 - Stacked * .065) || token.X > 1.1)
            { Tokens.Remove(token); Lives--; if (Mode == "WordTris") Stacked++; }
        }
        if (Lives <= 0 || Elapsed >= 60) Over = true;
    }
    public bool Submit(string value)
    {
        if (Over || value.Length == 0) return false;
        GameToken? matched = Mode == "Bubbles" ? Tokens.FirstOrDefault(t => t.Word == value) : Tokens.FirstOrDefault(t => t.Word.Equals(value, StringComparison.OrdinalIgnoreCase));
        bool correct = Mode == "ABC Speed Race" ? value.Equals(Expected, StringComparison.OrdinalIgnoreCase) : Mode == "Pipe Game" || Mode == "Cloud Race" ? value.Equals(Expected, StringComparison.OrdinalIgnoreCase) : matched != null;
        if (!correct) { Errors++; if (Mode == "Pipe Game") Lives--; if (Lives <= 0) Over = true; return false; }
        Score += value.Length * 10; Completed++;
        if (Mode != "ABC Speed Race") Tokens.Remove(Mode is "Pipe Game" or "Cloud Race" ? Tokens[0] : matched!);
        if (Mode == "Cloud Race") { Player = Math.Min(1, Player + .075); if (Player >= 1) Over = true; else Spawn(); }
        if (Mode == "ABC Speed Race" && Completed == 26 || Mode == "Pipe Game" && Tokens.Count == 0) Over = true;
        if (!Over && Tokens.Count == 0 && Mode is "Bubbles" or "WordTris" or "Clouds") Spawn();
        return true;
    }
}
public sealed class TypingGameView : Grid, IDisposable
{
    internal TypingGameEngine Engine { get; }
    readonly Canvas stage = new() { Height = 300, ClipToBounds = true };
    readonly TextBox input = new() { FontSize = 20, Margin = new Thickness(0, 8, 0, 0) };
    readonly TextBlock status = new() { Margin = new Thickness(0, 6, 0, 8), Foreground = Brushes.DarkBlue };
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    DateTime last;
    bool paused, ended;
    readonly Action<GameResult> finish;
    public TypingGameView(TypingGameEngine engine, Action<GameResult> onFinish)
    {
        Engine = engine; finish = onFinish;
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Children.Add(status); SetRow(stage, 1); Children.Add(stage); SetRow(input, 2); Children.Add(input);
        var pause = new Button { Content = "Pause", HorizontalAlignment = HorizontalAlignment.Left }; SetRow(pause, 3); Children.Add(pause);
        pause.Click += (_, _) => { paused = !paused; pause.Content = paused ? "Resume" : "Pause"; input.IsEnabled = !paused; if (!paused) { last = DateTime.UtcNow; input.Focus(); } };
        stage.Background = new LinearGradientBrush(Color.FromRgb(198, 204, 251), Color.FromRgb(232, 240, 255), 90);
        input.ToolTip = engine.Mode is "WordTris" or "Clouds" or "Cloud Race" ? "Type a word and press Space or Enter" : "Type the displayed letters";
        DataObject.AddPastingHandler(input, (_, e) => e.CancelCommand());
        input.TextChanged += (_, _) => { if (engine.Mode is "Bubbles" or "ABC Speed Race" or "Pipe Game" && input.Text.Length > 0) { string s = input.Text; input.Clear(); engine.Submit(s); Draw(); CheckEnd(); } };
        input.PreviewKeyDown += (_, e) => { if (e.Key is Key.Space or Key.Enter) { e.Handled = true; if (input.Text.Length > 0) { engine.Submit(input.Text.Trim()); input.Clear(); Draw(); CheckEnd(); } } };
        Loaded += (_, _) => { last = DateTime.UtcNow; timer.Start(); Draw(); input.Focus(); };
        Unloaded += (_, _) => Dispose();
        stage.SizeChanged += (_, _) => Draw();
        timer.Tick += (_, _) => { var now = DateTime.UtcNow; double dt = Math.Min(.2, (now - last).TotalSeconds); last = now; if (!paused) { engine.Advance(dt); Draw(); CheckEnd(); } };
    }
    void CheckEnd() { if (Engine.Over && !ended) { ended = true; timer.Stop(); input.IsEnabled = false; finish(new GameResult(Engine.Mode, Engine.Score, Engine.Errors, Engine.Elapsed, DateTime.Now)); } }
    void Draw()
    {
        stage.Children.Clear(); double width = Math.Max(450, stage.ActualWidth), height = stage.Height;
        status.Text = $"Score {Engine.Score}    Lives {Math.Max(0, Engine.Lives)}    Time {Engine.Elapsed:0}s    Misses {Engine.Errors}";
        if (Engine.Mode == "ABC Speed Race")
        {
            for (int i = 0; i < 26; i++) { double angle = (i * 12 - 90) * Math.PI / 180; Bubble(((char)('a' + i)).ToString(), width / 2 + Math.Cos(angle) * 130 - 18, 145 + Math.Sin(angle) * 120 - 18, i < Engine.Completed ? Brushes.Gray : i == Engine.Completed ? Brushes.RoyalBlue : Brushes.YellowGreen, 36); }
            Label(Engine.Expected.ToUpperInvariant(), width / 2 - 25, 110, 52, Brushes.DarkBlue); return;
        }
        if (Engine.Mode == "Pipe Game")
        {
            double tile = Math.Min(62, (width - 30) / 8);
            for (int i = 0; i < 32; i++)
            {
                int row = i / 8, col = row % 2 == 0 ? i % 8 : 7 - i % 8;
                var box = new Border { Width = tile - 4, Height = 46, CornerRadius = new CornerRadius(9), Background = i < Engine.Completed ? Brushes.DodgerBlue : Brushes.ForestGreen, BorderBrush = i == Engine.Completed ? Brushes.Gold : Brushes.DarkGreen, BorderThickness = new Thickness(3) };
                Canvas.SetLeft(box, 12 + col * tile); Canvas.SetTop(box, 30 + row * 62); stage.Children.Add(box);
                string letter = i < Engine.Completed ? "≈" : Engine.Tokens[i - Engine.Completed].Word.ToUpperInvariant(); Label(letter, 28 + col * tile, 34 + row * 62, 24, Brushes.White);
            }
            return;
        }
        if (Engine.Mode == "Cloud Race")
        {
            Label("YOU", 8, 40, 12, Brushes.DarkBlue); Label("PACE", 8, 160, 12, Brushes.DarkBlue);
            Cloud("You", 55 + Engine.Player * (width - 185), 40, Brushes.White); Cloud("Pace", 55 + Engine.Opponent * (width - 185), 155, Brushes.LightGray);
            Label(Engine.Expected, 20, 255, 24, Brushes.DarkBlue); return;
        }
        Brush[] colors = [Brushes.DodgerBlue, Brushes.Crimson, Brushes.YellowGreen, Brushes.MediumPurple, Brushes.Orange];
        foreach (var token in Engine.Tokens)
        {
            double x = token.X * (width - 70), y = token.Y * (height - 40);
            if (Engine.Mode == "Bubbles") Bubble(token.Word, x, y, colors[token.Color], 42);
            else if (Engine.Mode == "Clouds") Cloud(token.Word, x, y, Brushes.White);
            else
            {
                var tile = new Border { Width = 150, Height = 30, Background = new LinearGradientBrush(Colors.LightSteelBlue, Colors.White, 90), BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(2), Child = new TextBlock { Text = token.Word, FontSize = 17, Foreground = Brushes.DarkSlateBlue, HorizontalAlignment = HorizontalAlignment.Center } }; Canvas.SetLeft(tile, x); Canvas.SetTop(tile, y); stage.Children.Add(tile);
            }
        }
        if (Engine.Mode == "WordTris") for (int i = 0; i < Engine.Stacked; i++) { var block = new Rectangle { Width = width - 25, Height = 18, Fill = Brushes.SlateGray }; Canvas.SetLeft(block, 12); Canvas.SetTop(block, height - 20 * (i + 1)); stage.Children.Add(block); }
    }
    void Label(string text, double x, double y, double size, Brush color) { var label = new TextBlock { Text = text, FontSize = size, Foreground = color, FontWeight = FontWeights.SemiBold }; Canvas.SetLeft(label, x); Canvas.SetTop(label, y); stage.Children.Add(label); }
    void Bubble(string word, double x, double y, Brush color, double size) { var circle = new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = color, Child = new TextBlock { Text = word, Foreground = Brushes.White, FontSize = size / 2, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontFamily = new FontFamily("Georgia") } }; Canvas.SetLeft(circle, x); Canvas.SetTop(circle, y); stage.Children.Add(circle); }
    void Cloud(string word, double x, double y, Brush color)
    {
        var group = new Grid { Width = 120, Height = 60 }; group.Children.Add(new Ellipse { Fill = color, Width = 55, Height = 52, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(15, 0, 0, 8) }); group.Children.Add(new Border { Background = color, CornerRadius = new CornerRadius(22), Height = 38, VerticalAlignment = VerticalAlignment.Bottom, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) }); group.Children.Add(new TextBlock { Text = word, Foreground = Brushes.DarkSlateBlue, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 8) }); Canvas.SetLeft(group, x); Canvas.SetTop(group, y); stage.Children.Add(group);
    }
    public void Dispose() { timer.Stop(); }
}

