using System.IO;
using System.Windows;

namespace TypeMaster.Native;
public partial class App : Application
{
    System.Threading.Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--typing-benchmark")) { Shutdown(TypingPerformance.Run()); return; }
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, _) => { var window=(Window)sender;window.Icon??=AppInfo.WindowIcon;DisplayLayout.FitWindow(window); }));
        if (e.Args.Contains("--self-test"))
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            Shutdown(SelfTests.Run());
            return;
        }
        instance = new System.Threading.Mutex(true, "Local\\TypeMasterNative", out bool first);
        if (!first) { MessageBox.Show("TypeMaster is already running. Open its existing window from the taskbar.", "TypeMaster"); Shutdown(); return; }
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("TypeMaster could not complete this action.\n\n" + args.Exception.Message, "TypeMaster", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        try { var window = new MainWindow(); MainWindow = window; MainWindow.Show(); if (window.Store.RecoveryNotice is { } notice) MessageBox.Show(window, notice, "Profiles recovered", MessageBoxButton.OK, MessageBoxImage.Information); int meter = Array.IndexOf(e.Args, "--meter-profile"); if (meter >= 0 && meter + 1 < e.Args.Length && Guid.TryParse(e.Args[meter + 1], out var id)) window.StartMeterForProfile(id); _=window.CheckAutomaticUpdatesAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "TypeMaster could not start", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}


