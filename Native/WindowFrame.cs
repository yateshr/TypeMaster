using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
namespace TypeMaster.Native;

internal static class WindowFrame
{
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr handle,out Rect rectangle);
    internal static Thickness VisibleInsets(int left,int top,int right,int bottom,int workLeft,int workTop,int workRight,int workBottom,double dpiX,double dpiY) =>
        new(Math.Max(0,workLeft-left)/dpiX,Math.Max(0,workTop-top)/dpiY,Math.Max(0,right-workRight)/dpiX,Math.Max(0,bottom-workBottom)/dpiY);
    internal static void Configure(Window window,FrameworkElement surface,Panel buttons)
    {
        void Fit()
        {
            if(window.WindowState!=WindowState.Maximized){surface.Margin=new Thickness(0);return;}
            var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero||!GetWindowRect(handle,out var bounds))return;
            var work=System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;var dpi=VisualTreeHelper.GetDpi(window);
            // Windows retains its invisible resize frame outside the work area when
            // maximized. Keep custom client-drawn captions inside the visible area.
            surface.Margin=VisibleInsets(bounds.Left,bounds.Top,bounds.Right,bounds.Bottom,work.Left,work.Top,work.Right,work.Bottom,dpi.DpiScaleX,dpi.DpiScaleY);
        }
        window.Loaded+=(_,_)=>Fit();window.StateChanged+=(_,_)=>Fit();window.SizeChanged+=(_,_)=>Fit();window.LocationChanged+=(_,_)=>Fit();window.DpiChanged+=(_,_)=>Fit();
        var minimize=MakeButton("Minimize",()=>SystemCommands.MinimizeWindow(window),false);
        var maximize=MakeButton("Maximize",()=>{if(window.WindowState==WindowState.Maximized)SystemCommands.RestoreWindow(window);else SystemCommands.MaximizeWindow(window);},false);
        var close=MakeButton("Close",window.Close,true);
        minimize.Content=Icon("M 0 5 L 10 5");close.Content=Icon("M 0 0 L 10 10 M 10 0 L 0 10");
        void Update(){bool restored=window.WindowState==WindowState.Maximized;maximize.Content=Icon(restored?"M 2 0 L 10 0 10 8 M 0 2 L 8 2 8 10 0 10 Z":"M 0 0 L 10 0 10 10 0 10 Z");AutomationProperties.SetName(maximize,restored?"Restore":"Maximize");maximize.ToolTip=restored?"Restore":"Maximize";}
        window.StateChanged+=(_,_)=>Update();Update();buttons.Children.Add(minimize);buttons.Children.Add(maximize);buttons.Children.Add(close);
    }
    static Path Icon(string geometry)=>new(){Data=Geometry.Parse(geometry),Stroke=Brushes.Black,StrokeThickness=1,Width=11,Height=11,Stretch=Stretch.None,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
    static Button MakeButton(string name,Action action,bool close)
    {
        var button=new Button {Width=46,Height=23,MinHeight=0,Margin=new Thickness(0),Padding=new Thickness(0),Background=Brushes.Transparent,Foreground=Brushes.Black,BorderThickness=new Thickness(0),Focusable=false,ToolTip=name,Cursor=System.Windows.Input.Cursors.Arrow};
        var border=new FrameworkElementFactory(typeof(Border));border.Name="CaptionBackground";border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
        var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(presenter);
        var template=new ControlTemplate(typeof(Button)){VisualTree=border};var hover=new Trigger {Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Border.BackgroundProperty,new SolidColorBrush(close?Color.FromRgb(232,87,87):Color.FromRgb(220,220,220)),"CaptionBackground"));template.Triggers.Add(hover);button.Template=template;
        AutomationProperties.SetName(button,name);button.Click+=(_,_)=>action();return button;
    }
}

