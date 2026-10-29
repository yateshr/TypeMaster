using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    void MinimizeWindow(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    void ToggleMaximize(object sender, RoutedEventArgs e) { if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this); else SystemCommands.MaximizeWindow(this); }
    void CloseWindow(object sender, RoutedEventArgs e) => Close();
    static T Position<T>(Canvas canvas, T control, double x, double y, double width = double.NaN, double height = double.NaN) where T : FrameworkElement
    {
        control.Margin = new Thickness(0); if (!double.IsNaN(width)) control.Width = width; if (!double.IsNaN(height)) control.Height = height;
        Canvas.SetLeft(control, x); Canvas.SetTop(control, y); canvas.Children.Add(control); return control;
    }
    static TextBlock RefText(string text, double size = 14, bool bold = false, bool serif = false)
    {
        var label = new TextBlock { Text = Localization.Translate(text), FontFamily = new FontFamily(serif ? "Georgia" : "Verdana"), FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0) }; label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush"); return label;
    }
    static TextBlock RefTitle(string text) { var label = RefText(text, 27); label.FontFamily = new FontFamily("Segoe UI"); return label; }
    static Button RefButton(string title, Action action, bool outline = false)
    {
        var button = Button(Localization.Translate(title), action); button.Margin = new Thickness(0); button.Padding = new Thickness(outline ? 9 : 4, 0, 4, 0); button.MinHeight = 0; button.FontFamily = new FontFamily("Verdana"); button.FontSize = 13; button.HorizontalContentAlignment = outline ? HorizontalAlignment.Left : HorizontalAlignment.Center;
        if (new[] { "Enter", "Cancel", "Next", "Delete User", "Start", "Start Now" }.Contains(title)) button.Content = new TextBlock { Text = Localization.Translate(title), TextDecorations = TextDecorations.Underline };
        if (outline) { button.Style = (Style)Application.Current.FindResource("ReferenceOutlineButton"); button.SetResourceReference(BackgroundProperty, "PanelBrush"); button.Foreground = new SolidColorBrush(Color.FromRgb(0, 119, 255)); button.BorderBrush = button.Foreground; button.BorderThickness = new Thickness(1); button.Tag = new CornerRadius(10); }
        else { button.Background = new LinearGradientBrush(Color.FromRgb(13, 91, 204), Color.FromRgb(14, 96, 206), 90); button.Tag = new CornerRadius(4); }
        return button;
    }
    Canvas ReferencePage(string? title, bool wide = false)
    {
        PageScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled; PageContent.Margin = new Thickness(0); PageContent.Children.Clear();
        var canvas = new Canvas { Width = wide ? 778 : 593, Height = 529, ClipToBounds = true }; canvas.SetResourceReference(Panel.BackgroundProperty, "PanelBrush"); Add(canvas);
        if (title != null) Position(canvas, RefTitle(title), 18, 9, 550, 42); return canvas;
    }
    static void Rule(Canvas canvas, double x, double y, double width, Brush? brush = null) => Position(canvas, new Border { Background = brush ?? new SolidColorBrush(Color.FromRgb(104, 139, 255)) }, x, y, width, 1);
    static ComboBox RefChoice(IEnumerable<string> values, string selected, Action<string> change)
    {
        var box = new ComboBox { ItemsSource = values.Select(v => new KeyValuePair<string,string>(v,Localization.Translate(v))).ToList(), DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = selected, FontFamily = new FontFamily("Verdana"), FontSize = 13, Padding = new Thickness(2, 1, 2, 0), VerticalContentAlignment = VerticalAlignment.Center };
        box.Style = (Style)Application.Current.FindResource("ReferenceCombo");
        box.SelectionChanged += (_, _) => { if (box.SelectedValue is string value) change(value); }; return box;
    }
    void Nav()
    {
        Navigation.Visibility = Visibility.Visible; NavColumn.Width = new GridLength(185); Navigation.Children.Clear();
        var close = RefButton("Close", Login, true); close.Background = Brushes.Transparent; close.BorderThickness = new Thickness(0); close.Foreground = Current?.Dark == true ? Brushes.WhiteSmoke : Brushes.Black; close.FontFamily = new FontFamily("Georgia"); close.FontSize = 17; close.FontWeight = FontWeights.Bold; Position(Navigation, close, 91, 3, 66, 31);
        ReferenceClose(Navigation, 153, 13, Login);
        string[] names = ["Studying", "Typing Meter", "Custom Review", "Typing Test", "Games", "Statistics", "Settings", "About"];
        for (int i = 0; i < names.Length; i++)
        {
            string title = names[i]; bool active = title == page; double top = 48 + i * 51;
            var button = RefButton("", () => { activeLesson = null; ShowPage(title); }); button.Tag = new CornerRadius(0); button.Padding = new Thickness(0); button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.Background = active ? new LinearGradientBrush(Color.FromRgb(193, 209, 247), Color.FromArgb(0, 244, 245, 248), 0) : Brushes.Transparent;
            if (active) { button.BorderBrush = Brushes.RoyalBlue; button.BorderThickness = new Thickness(5, 0, 0, 0); }
            var body = new Canvas { Width = 174, Height = 32 }; var icon = NavigationIcon(title, active, Current?.Dark == true); Position(body, icon, active ? 24 : 29, 8, 18, 18);
            var text = RefText(Localization.Translate(title).ToUpperInvariant(), active ? 12 : 11, active); text.Foreground = Current?.Dark == true ? Brushes.WhiteSmoke : active ? new SolidColorBrush(Color.FromRgb(18,86,172)) : new SolidColorBrush(Color.FromRgb(13,22,41)); Position(body, text, active ? 52 : 57, 9, 115, 23); button.Content = body;
            System.Windows.Automation.AutomationProperties.SetName(button, title);
            Position(Navigation, button, 0, top, 174, 32);
        }
        var logo = new Image { Source = AssetBitmap("Branding/typemaster-wordmark.png"), Stretch = Stretch.Fill }; Position(Navigation, logo, 30, 486, 132, 27);
    }
    static void ReferenceClose(Canvas canvas, double x, double y, Action action)
    {
        canvas.Tag = action;
        var button = RefButton("Close", action); button.Content = Picture("TypeMasterArtwork/close.png",17,17); button.Padding = new Thickness(0); button.Tag = new CornerRadius(0); button.Background = Brushes.Transparent; System.Windows.Automation.AutomationProperties.SetName(button, "Close"); Position(canvas, button, x, y, 17, 17);
    }
    internal void GoBack()
    {
        if (PageContent.Children.OfType<Canvas>().FirstOrDefault()?.Tag is Action back) { back(); return; }
        if (Current != null) { if (page == "Studying") Login(); else ShowPage("Studying"); }
    }
    static FrameworkElement NavigationIcon(string title, bool active, bool dark)
    {
        var canvas = new Canvas { Width = 17, Height = 17 }; Brush ink = dark && !active ? Brushes.LightGray : new SolidColorBrush(Color.FromRgb(61, 67, 69));
        void Rectangle(double x, double y, double w, double h) => Position(canvas, new Rectangle { Fill = ink }, x, y, w, h);
        if (active) { Position(canvas, new Ellipse { Fill = ink }, 1, 1, 15, 15); Position(canvas, new TextBlock { Text = "‹", FontFamily = new FontFamily("Arial"), FontSize = 19, FontWeight = FontWeights.Bold, Foreground = Brushes.LightGray }, 4, -3, 12, 20); }
        else if (title == "Studying") { Rectangle(2, 5, 14, 10); Rectangle(5, 1, 8, 3); Position(canvas, new Rectangle { Fill = Brushes.LightGray }, 2, 8, 14, 2); }
        else if (title == "Typing Meter") { Rectangle(3, 1, 13, 15); Position(canvas, new Rectangle { Fill = Brushes.LightGray }, 5, 3, 9, 3); Rectangle(8, 7, 2, 9); }
        else if (title == "Typing Test") { Rectangle(1, 9, 3, 7); Rectangle(6, 3, 3, 13); Rectangle(11, 6, 3, 10); }
        else if (title == "Custom Review") { Position(canvas, new Ellipse { Fill = ink }, 6, 0, 7, 8); Position(canvas, new Border { Background = ink, CornerRadius = new CornerRadius(4, 4, 1, 1) }, 4, 7, 12, 10); }
        else if (title == "About") { Position(canvas, new Ellipse { Fill = ink }, 1, 1, 16, 16); Position(canvas, new TextBlock { Text = "i", FontFamily = new FontFamily("Georgia"), FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White }, 7, 0, 7, 16); }
        else if (title == "Statistics") { Position(canvas, new Ellipse { Fill = ink }, 1, 1, 15, 15); Position(canvas, new Rectangle { Fill = Brushes.LightGray }, 8, 0, 8, 8); }
        else if (title == "Settings") { Position(canvas, new TextBlock { Text = "⚙", FontFamily = new FontFamily("Segoe UI Symbol"), FontSize = 20, Foreground = ink }, -1, -4, 22, 24); }
        else { Position(canvas, new Ellipse { Stroke = ink, StrokeThickness = 1.5 }, 1, 1, 15, 15); Position(canvas, new Ellipse { Stroke = ink, StrokeThickness = 1.3 }, 5, 1, 7, 15); Position(canvas, new Rectangle { Fill = ink }, 1, 8, 15, 1); }
        return canvas;
    }
}

public sealed class ComboDisplayConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is KeyValuePair<string,string> pair ? pair.Value : value?.ToString() ?? "";
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => System.Windows.Data.Binding.DoNothing;
}



