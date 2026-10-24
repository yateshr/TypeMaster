using System.IO;
using Path = System.IO.Path;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    internal void ShowCourseIntroduction(Lesson lesson)
    {
        Navigation.Visibility = Visibility.Collapsed; NavColumn.Width = new GridLength(0);
        void Back() { if (openedCourse != null) ShowChapter(openedCourse, openedChapter); else ShowPage("Studying"); }
        void Complete() { Current!.Completed.Add(lesson.Id); Save(); NextLesson(); }
        var canvas = ReferencePage(null, true);
        if (lesson.Kind == "Key introduction")
        {
            Position(canvas, new Border { Background = (Brush)Application.Current.FindResource("SoftBrush") }, 0, 0, 568, 529);
            Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(213, 229, 250)) }, 568, 0, 210, 529);
            var layout = LoadKeyboard(); string keys = lesson.Keys == "@home" ? layout.HomeKeys : lesson.Keys;
            if (string.IsNullOrEmpty(keys)) keys = "asdfjkl;";
            var prompt = Position(canvas, RefText("", 21), 37, 49, 510, 100); prompt.FontFamily = new FontFamily("Arial");
            var keyboard = new KeyboardView(layout) { MaxHeight = 175, Stretch = Stretch.Fill }; Position(canvas, keyboard, 34, 185, 500, 175);
            var patch = ReferenceGraphic("key-drill-new-reference.png", new Int32Rect(724, 692, 22, 22)); patch.Clip = new EllipseGeometry(new Rect(0, 0, 22, 22)); Position(canvas, patch, 152, 406, 22, 22);
            var hands = ReferenceGraphic("key-drill-new-reference.png", new Int32Rect(637, 648, 367, 143)); hands.Clip = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0,0,367,143)), new EllipseGeometry(new Point(97,32),11,11)); Position(canvas, hands, 66, 385, 367, 143);
            var pointer = Position(canvas, new Ellipse { Fill = Brushes.Plum, Stroke = Brushes.MediumPurple }, 153, 407, 18, 18);
            int index = 0; var next = RefButton("Next", Complete); next.IsEnabled = false; Position(canvas, next, 584, 449, 180, 34);
            var input = new TextBox { Opacity = 0, MaxLength = 1, IsUndoEnabled = false, AllowDrop = false }; Position(canvas, input, 0, 0, 1, 1); DataObject.AddPastingHandler(input, (_, e) => e.CancelCommand());
            void ShowKey()
            {
                if (index >= keys.Length) { prompt.Text = Localization.Translate("All highlighted keys completed. Press Next to continue."); keyboard.Highlight(""); pointer.Visibility = Visibility.Hidden; next.IsEnabled = true; return; }
                char key = keys[index]; var shape = layout.Keys.FirstOrDefault(k => k.Caption.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase)); int finger = Math.Clamp(shape?.Finger ?? 1, 0, 9);
                string[] names = ["left thumb", "left little finger", "left ring finger", "left middle finger", "left index finger", "right index finger", "right middle finger", "right ring finger", "right little finger", "right thumb"];
                prompt.Text = Localization.Translate("Now try typing") + " \"" + key + "\" " + Localization.Translate("with") + " " + Localization.Translate(names[finger]); keyboard.Highlight(key.ToString());
                Point[] points = [new(150, 470),new(93,435),new(124,411),new(152,401),new(180,405),new(309,405),new(337,401),new(365,413),new(393,435),new(195,480)]; Canvas.SetLeft(pointer, points[finger].X - 9); Canvas.SetTop(pointer, points[finger].Y - 9);
            }
            input.TextChanged += (_, _) => { if (input.Text.Length == 0) return; if (index < keys.Length && char.ToLowerInvariant(input.Text[0]) == char.ToLowerInvariant(keys[index])) index++; input.Clear(); ShowKey(); };
            input.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter && next.IsEnabled) { e.Handled = true; Complete(); } };
            ShowKey(); input.Loaded += (_, _) => input.Focus();
            Position(canvas, RefButton("Cancel", Back, true), 584, 493, 180, 34).HorizontalContentAlignment = HorizontalAlignment.Center; ReferenceClose(canvas, 746, 13, Back); input.Focus();
            return;
        }
        Position(canvas, new Border { Background = new SolidColorBrush(Color.FromRgb(20,111,226)) }, 0, 0, 778, 45);
        var title = Position(canvas, RefText(lesson.Title, 27), 15, 5, 740, 38); title.Foreground = Brushes.White; title.TextAlignment = TextAlignment.Center; title.FontFamily = new FontFamily("Tahoma");
        ReferenceClose(canvas, 746, 13, Back);
        var text = RefText(lesson.Text, 16); text.FontFamily = new FontFamily("Arial"); text.LineHeight = 18;
        var textArea = new ScrollViewer { Content = text, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        bool illustrated = lesson.Image.Length > 0 && File.Exists(Path.Combine(Assets.Root, lesson.Image)); Position(canvas, textArea, 29, 84, illustrated ? 190 : 720, 350);
        if (illustrated) Position(canvas, new Image { Source = AssetBitmap(lesson.Image), Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Top }, 244, 84, 293, 202);
        Position(canvas, RefButton("Cancel", Back, true), 16, 479, 180, 34).HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, RefButton("Next", Complete), 584, 479, 180, 34);
    }
}

