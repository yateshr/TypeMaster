using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    Canvas? keySequence;
    Ellipse? handPointer;
    KeyboardLayout? drillLayout;
    void BuildKeyDrill()
    {
        var canvas = ReferencePage(null, true); var paper = new Border(); paper.SetResourceReference(Border.BackgroundProperty, "SoftBrush"); Position(canvas, paper, 0, 0, 568, 529);
        Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(213, 229, 250)) }, 568, 0, 210, 529);
        void Prompt(string text, double y) { Position(canvas, new Ellipse { Fill = Brushes.YellowGreen }, 22, y, 22, 22); Position(canvas, new TextBlock { Text = "▶", Foreground = Brushes.White, FontSize = 12 }, 28, y + 2, 16, 18); Position(canvas, RefText(text, 13), 52, y + 4, 500, 22).Foreground = Brushes.OliveDrab; }
        Prompt("Type the key sequences, follow the highlighted key", 25); Prompt("Look at the on-screen keyboard and hands for hints, when needed", 150);
        keySequence = new Canvas(); Position(canvas, keySequence, 34, 59, 510, 81);
        drillLayout = LoadKeyboard(); liveKeyboard = new KeyboardView(drillLayout) { MaxHeight = 175, Stretch = Stretch.Fill }; Position(canvas, liveKeyboard, 34, 185, 500, 175);
        // Reuse the hands photograph from the new guide. Exclude its fixed marker
        // from the rendering, then draw the live finger cue for the current key.
        var fingerTexture = ReferenceGraphic("key-drill-new-reference.png", new Int32Rect(724, 692, 22, 22)); fingerTexture.Clip = new EllipseGeometry(new Rect(0, 0, 22, 22)); Position(canvas, fingerTexture, 152, 406, 22, 22);
        var hands = ReferenceGraphic("key-drill-new-reference.png", new Int32Rect(637, 648, 367, 143));
        hands.Clip = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0, 0, 367, 143)), new EllipseGeometry(new Point(97, 32), 11, 11)); Position(canvas, hands, 66, 385, 367, 143);
        handPointer = Position(canvas, new Ellipse { Fill = Brushes.Plum, Stroke = Brushes.MediumPurple, StrokeThickness = 1 }, 153, 407, 19, 19);
        Position(canvas, RefText("Completed", 14, true, true), 585, 48, 180, 22); Rule(canvas, 584, 67, 180, Brushes.White);
        clockBlock = Position(canvas, RefText("0 %", 24, true), 585, 70, 180, 36); Position(canvas, new Ellipse { Stroke = Brushes.YellowGreen, StrokeThickness = 1, Fill = new SolidColorBrush(Color.FromRgb(243, 246, 255)) }, 609, 104, 122, 67);
        speedBlock = new TextBlock(); accuracyBlock = new TextBlock(); targetBlock = null;
        TypingBox = new TextBox { Opacity = 0, IsUndoEnabled = false, AcceptsReturn = false, AllowDrop = false }; Position(canvas, TypingBox, 0, 0, 1, 1); DataObject.AddPastingHandler(TypingBox, (_, e) => e.CancelCommand()); TypingBox.TextChanged += (_, _) => InputChanged();
        void Cancel() { ClearExerciseDraft(); exercising = false; timer.Stop(); ShowPage(page); }
        Position(canvas, RefButton("Next", () => FinishExercise()), 584, 449, 180, 34); var cancel = RefButton("Cancel", Cancel, true); cancel.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, cancel, 584, 493, 180, 34); ReferenceClose(canvas, 746, 13, Cancel);
        PaintKeyDrill(); UpdateMetrics(); TypingBox.Focus();
    }
    void PaintKeyDrill()
    {
        if (keySequence == null) return; keySequence.Children.Clear(); int start = typed.Length / 20 * 20;
        double x = 0;
        for (int n = 0; n < 20 && start + n < target.Length; n++)
        {
            if (n == 10) x = 0; char c = target[start + n]; double width = c == ' ' ? 50 : 32; bool active = start + n == typed.Length;
            var label = new TextBlock { Text = c == ' ' ? "Space" : c.ToString(), FontSize = c == ' ' ? 12 : 18, FontWeight = FontWeights.Bold, Foreground = active ? Brushes.White : Brushes.DarkBlue, Padding = new Thickness(4, 1, 0, 0) };
            Position(keySequence, new Border { Background = active ? Brushes.MediumPurple : new SolidColorBrush(Color.FromRgb(223, 230, 255)), BorderBrush = Brushes.SlateBlue, BorderThickness = new Thickness(1), Child = label }, x, n < 10 ? 0 : 40, width, 34); x += width + 8;
        }
        if (typed.Length < target.Length)
        {
            char expected = target[typed.Length]; liveKeyboard?.Highlight(expected.ToString());
            var key = drillLayout?.Keys.FirstOrDefault(k => (k.Caption == "Space" && expected == ' ') || k.Caption.Equals(expected.ToString(), StringComparison.OrdinalIgnoreCase) || k.Shift == expected.ToString());
            Point[] fingers = [new(163, 417), new(99, 459), new(128, 428), new(149, 402), new(163, 417), new(309, 405), new(337, 401), new(365, 413), new(393, 435), new(195, 480)];
            if (handPointer != null) { handPointer.Visibility = key == null ? Visibility.Hidden : Visibility.Visible; var point = fingers[Math.Clamp(key?.Finger ?? 0, 0, 9)]; Canvas.SetLeft(handPointer, point.X - 10); Canvas.SetTop(handPointer, point.Y - 10); }
        }
    }
}


