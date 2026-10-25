using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;

namespace TypeMaster.Native;

internal sealed class UpdatesWindow:Window
{
    readonly string directory;
    readonly Func<string,bool> install;
    readonly AppUpdates service;
    readonly TextBlock status;
    readonly Button check,download,run;
    readonly ProgressBar progress;
    readonly CancellationTokenSource cancel=new();
    ReleaseUpdate? release;
    string? installer;
    bool closed;
    internal UpdatesWindow(string directory,Func<string,bool> install,AppUpdates? service=null)
    {
        this.directory=directory;this.install=install;this.service=service??new();
        Title="TypeMaster Updates & GitHub";Width=590;Height=530;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Brushes.White;
        var panel=new StackPanel{Margin=new Thickness(24)};
        void Text(string value,double size=13,bool bold=false)=>panel.Children.Add(new TextBlock{Text=value,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10),Foreground=new SolidColorBrush(Color.FromRgb(24,39,66))});
        Text("Keep TypeMaster up to date",22,true);Text("Installed version: "+AppInfo.Version+"  ·  Developed by "+AppInfo.Developer);
        Text("TypeMaster is a typing-practice application customised for computer assessments. Practise typing, test candidates, record examiner-observed technical skills and create individual PDF reports.");
        var automatic=new CheckBox{Content="Check GitHub for updates when TypeMaster starts",IsChecked=UpdatePreferences.Load(directory).AutomaticChecks,Margin=new Thickness(0,4,0,12)};
        automatic.Click+=(_,_)=>{try{var settings=UpdatePreferences.Load(directory);(settings with{AutomaticChecks=automatic.IsChecked==true}).Save(directory);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Update preference could not be saved");}};panel.Children.Add(automatic);
        status=new TextBlock{Text="Check for a newer release. Downloads and installation only start when you choose them.",TextWrapping=TextWrapping.Wrap,MinHeight=50,Margin=new Thickness(0,0,0,10)};panel.Children.Add(status);
        progress=new ProgressBar{Minimum=0,Maximum=100,Height=8,Visibility=Visibility.Collapsed,Margin=new Thickness(0,0,0,10)};panel.Children.Add(progress);
        var actions=new WrapPanel();
        check=Action("Check for updates",async()=>await CheckAsync());download=Action("Download update",async()=>await DownloadAsync());run=Action("Install and restart",async()=>await InstallAsync());download.IsEnabled=run.IsEnabled=false;
        actions.Children.Add(check);actions.Children.Add(download);actions.Children.Add(run);panel.Children.Add(actions);
        Text(File.Exists(Path.Combine(AppContext.BaseDirectory,"Uninstall.exe"))?"Installation will close TypeMaster, preserve candidate records and open the update installer.":"Portable copy: use the release ZIP to update portable files. The installer option installs TypeMaster for this Windows account.",11);
        var links=new WrapPanel();links.Children.Add(Action("GitHub project",()=>{Open(AppUpdates.Project);return Task.CompletedTask;}));links.Children.Add(Action("Releases / ZIP downloads",()=>{Open(AppUpdates.Releases);return Task.CompletedTask;}));panel.Children.Add(links);
        Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Closed+=(_,_)=>{closed=true;cancel.Cancel();};
    }
    static Button Action(string title,Func<Task> action)
    {
        var button=new Button{Content=title,Margin=new Thickness(0,0,8,10),Padding=new Thickness(10,6,10,6)};button.Click+=async(_,_)=>await action();return button;
    }
    internal static void Open(string url)=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true});
    async Task CheckAsync()
    {
        check.IsEnabled=download.IsEnabled=run.IsEnabled=false;installer=null;status.Text="Checking the latest published GitHub release…";
        try
        {
            release=await service.CheckAsync(Version.Parse(AppInfo.Version),cancel.Token);if(closed)return;
            status.Text=release is null?"No newer stable release is published on GitHub. Your copy is up to date or newer than the public release.":release.CanInstall?"TypeMaster "+release.Version+" is available. Download the verified installer, then choose when to install.":"TypeMaster "+release.Version+" is available. A verifiable Windows installer is not available; use the releases page.";
            download.IsEnabled=release?.CanInstall==true;
            var preferences=UpdatePreferences.Load(directory);(preferences with{LastCheckedUtc=DateTime.UtcNow}).Save(directory);
        }
        catch(OperationCanceledException){if(!closed)status.Text="The update check timed out. Try again when connected.";}
        catch(Exception ex){if(!closed)status.Text="Could not check for updates. "+ex.Message;}
        finally{if(!closed)check.IsEnabled=true;}
    }
    async Task DownloadAsync()
    {
        if(release is null)return;download.IsEnabled=check.IsEnabled=false;progress.Visibility=Visibility.Visible;progress.Value=0;status.Text="Downloading the update from GitHub…";
        try{installer=await service.DownloadAsync(release,directory,new Progress<double>(value=>{if(!closed)progress.Value=value;}),cancel.Token);if(closed)return;status.Text="Download verified. Choose Install and restart when you are ready.";run.IsEnabled=true;}
        catch(OperationCanceledException){if(!closed)status.Text="Download cancelled or timed out.";}
        catch(Exception ex){if(!closed)status.Text="Update was not downloaded. "+ex.Message;}
        finally{if(!closed){check.IsEnabled=true;download.IsEnabled=true;}}
    }
    async Task InstallAsync()
    {
        if(installer is null||release?.Sha256 is null)return;run.IsEnabled=false;
        try{if(!await AppUpdates.VerifyAsync(installer,release.Sha256,cancel.Token))throw new InvalidDataException("The installer changed after download. Check for updates and download again.");if(!install(installer))run.IsEnabled=true;}
        catch(Exception ex){if(!closed){status.Text=ex.Message;run.IsEnabled=true;}}
    }
}

public partial class MainWindow
{
    void Updates()=>new UpdatesWindow(Store.DirectoryPath,InstallUpdate){Owner=this}.ShowDialog();
    internal async Task CheckAutomaticUpdatesAsync()
    {
        try
        {
            var preferences=UpdatePreferences.Load(Store.DirectoryPath);if(!preferences.AutomaticChecks||DateTime.UtcNow-preferences.LastCheckedUtc<TimeSpan.FromDays(1))return;
            var update=await new AppUpdates().CheckAsync(Version.Parse(AppInfo.Version));
            var latest=UpdatePreferences.Load(Store.DirectoryPath);(latest with{LastCheckedUtc=DateTime.UtcNow}).Save(Store.DirectoryPath);
            if(!latest.AutomaticChecks||update is null||preferences.DismissedVersion==update.Version.ToString()||exercising||!IsVisible||!IsActive||!IsLoaded||Application.Current.MainWindow!=this||Application.Current.Windows.OfType<Window>().Any(window=>window!=this&&window.IsVisible))return;
            if(MessageBox.Show(this,"TypeMaster "+update.Version+" is available. Open Updates & GitHub? No files will download until you choose Download update.","TypeMaster update",MessageBoxButton.YesNo,MessageBoxImage.Information)==MessageBoxResult.Yes)Updates();
            else (preferences with{LastCheckedUtc=DateTime.UtcNow,DismissedVersion=update.Version.ToString()}).Save(Store.DirectoryPath);
        }
        catch{ /* An offline update check must not prevent typing practice or tests. */ }
    }
    bool InstallUpdate(string installer)
    {
        if(exercising){Notify("Finish or stop the current exercise before installing an update.");return false;}
        if(MessageBox.Show(this,"Close TypeMaster and open the verified update installer? Candidate records will be kept.","Install TypeMaster update",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return false;
        Store.Save();string profiles=Path.Combine(Store.DirectoryPath,"profiles.json");if(File.Exists(profiles))File.Copy(profiles,Path.Combine(Store.DirectoryPath,"profiles.pre-update-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));
        bool installed=File.Exists(Path.Combine(AppContext.BaseDirectory,"Uninstall.exe"));
        string arguments="/UPDATEPID="+Environment.ProcessId+(installed?" /D="+AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar):"");
        _ = Process.Start(new ProcessStartInfo(installer){UseShellExecute=true,Arguments=arguments})??throw new IOException("Windows could not start the update installer.");
        quitting=true;Application.Current.Shutdown();return true;
    }
}
