using System.IO;
using Path = System.IO.Path;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    UIElement GameArtwork(string mode)
    {
        if (mode is "Bubbles" or "WordTris") return new Image { Source = AssetBitmap(mode == "Bubbles" ? "bubbles.gif" : "wordtris.gif"), Height = 105, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center };
        var canvas = new Canvas { Width = 160, Height = 105, ClipToBounds = true };
        if (mode == "Pipe Game")
        {
            var tile = new CroppedBitmap(AssetBitmap("pipe_tileset.bmp"), new Int32Rect(0, 128, 192, 160));
            canvas.Children.Add(new Image { Source = tile, Width = 160, Height = 125, Stretch = Stretch.Uniform });
            for (int i = 0; i < 5; i++) { var letter = new TextBlock { Text = "ASDFG"[i].ToString(), FontSize = 19, FontWeight = FontWeights.Bold, Foreground = Brushes.DodgerBlue }; Canvas.SetLeft(letter, 10 + i * 28); Canvas.SetTop(letter, 45 + i % 2 * 25); canvas.Children.Add(letter); }
            return canvas;
        }
        if (mode == "ABC Speed Race")
        {
            for (int i = 0; i < 26; i++)
            {
                double angle = (i * 13 - 130) * Math.PI / 180;
                var circle = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(8), Background = Brushes.YellowGreen, Child = new TextBlock { Text = ((char)('a' + i)).ToString(), FontFamily = new FontFamily("Georgia"), Foreground = Brushes.White, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center } };
                Canvas.SetLeft(circle, 72 + 57 * Math.Cos(angle)); Canvas.SetTop(circle, 55 + 50 * Math.Sin(angle)); canvas.Children.Add(circle);
            } return canvas;
        }
        for (int i = 0; i < 3; i++)
        {
            var cloud = new Grid { Width = 84, Height = 46 };
            cloud.Children.Add(new Ellipse { Fill = Brushes.WhiteSmoke, Width = 40, Height = 42, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(12, 0, 0, 0) });
            cloud.Children.Add(new Border { Background = Brushes.WhiteSmoke, CornerRadius = new CornerRadius(16), Height = 25, VerticalAlignment = VerticalAlignment.Bottom, BorderBrush = Brushes.Silver, BorderThickness = new Thickness(1) });
            Canvas.SetLeft(cloud, i % 2 == 0 ? 5 : 70); Canvas.SetTop(cloud, i * 37); canvas.Children.Add(cloud);
        }
        var sun = new TextBlock { Text = mode == "Cloud Race" ? "+12" : "☀", Foreground = Brushes.Gold, FontSize = 23, FontWeight = FontWeights.Bold }; Canvas.SetLeft(sun, 118); Canvas.SetTop(sun, 8); canvas.Children.Add(sun); return canvas;
    }
    internal void StartGame(string mode, string category = "Animals", string? reviewKeys = null)
    {
        gameView?.Dispose(); Heading(mode);
        string file = category == "Common words" ? Path.Combine(Assets.Root, "top1000wordlist.txt") : category == "Difficult words" ? Path.Combine(Assets.Root, "top100difficultwords.txt") : Path.Combine(Assets.Root, "lessons", "animals.vcb.eng");
        string[] words = Regex.Split(Assets.Read(file), @"\s+").Where(s => s.Length is >= 2 and <= 14 && s.All(char.IsLetter)).Distinct().ToArray();
        string chars = category == "0-9" ? "0123456789" : category == "A-Z" ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "abcdefghijklmnopqrstuvwxyz";
        if (!string.IsNullOrEmpty(reviewKeys)) { chars = reviewKeys; var focused = words.Where(w => w.Any(c => reviewKeys.Contains(char.ToLowerInvariant(c)))).ToArray(); words = focused.Length > 0 ? focused : Enumerable.Range(0, 50).Select(_ => string.Concat(Enumerable.Range(0, 4).Select(_ => chars[Random.Shared.Next(chars.Length)]))).ToArray(); }
        var engine = new TypingGameEngine(mode, words, chars);
        gameView = new TypingGameView(engine, result =>
        {
            Current!.Games.Add(result); if (activeLesson?.Kind == "Game" && result.Score > 0) Current.Completed.Add(activeLesson.Id); Save();
            Heading("Game Results"); Add(Text(mode, 22, true)); Add(Text($"Score: {result.Score}\nCorrect answers: {engine.Completed}\nMisses: {result.Errors}\nTime: {result.Seconds:0.0} seconds", 19));
            var buttons = new WrapPanel(); buttons.Children.Add(Button("Play again", () => StartGame(mode, category, reviewKeys))); buttons.Children.Add(Outline("Back to games", Games)); if (activeLesson?.Kind == "Game") buttons.Children.Add(Button("Next Lesson", NextLesson)); Add(buttons);
        }); Add(gameView); Add(Outline("Back to Games", () => { gameView.Dispose(); Games(); }));
    }
}

