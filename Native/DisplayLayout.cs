using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls;

namespace TypeMaster.Native;
static class DisplayLayout
{
    internal static void FitWindow(Window window)
    {
        if (window is not MainWindow && window is not ReferenceReportWindow && window.Content is FrameworkElement content && content is not Viewbox && content.ActualWidth > 0 && content.ActualHeight > 0)
        {
            double originalWidth = content.ActualWidth, originalHeight = content.ActualHeight;
            window.Content = null;
            var surface = new Grid { Width = originalWidth, Height = originalHeight }; surface.Children.Add(content);
            window.Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Child = surface };
        }
        var monitor = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle).WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(window);
        double width = monitor.Width / dpi.DpiScaleX, height = monitor.Height / dpi.DpiScaleY;
        window.MinWidth = Math.Min(window.MinWidth, width); window.MinHeight = Math.Min(window.MinHeight, height);
        if (window.WindowState != WindowState.Normal) return;
        window.Width = Math.Min(double.IsFinite(window.Width) ? window.Width : window.ActualWidth, width); window.Height = Math.Min(double.IsFinite(window.Height) ? window.Height : window.ActualHeight, height);
        window.Left = Math.Clamp(window.Left, monitor.Left / dpi.DpiScaleX, monitor.Right / dpi.DpiScaleX - window.Width);
        window.Top = Math.Clamp(window.Top, monitor.Top / dpi.DpiScaleY, monitor.Bottom / dpi.DpiScaleY - window.Height);
    }
}

