using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TypeMaster.Native;

internal static class AppUpdateTests
{
    sealed class FixtureHandler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();var response=respond(request);response.RequestMessage=request;return Task.FromResult(response);
        }
    }
    internal static void Verify(string output,Action<bool,string> check)
    {
        string folder=Path.Combine(output,"updates-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var bytes=Encoding.UTF8.GetBytes("MZ TypeMaster update fixture; never execute");string digest=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string Json(string tag="v99.0.0",string? url=null,string? hash=null,bool preview=false,bool draft=false,long? size=null,bool assets=true)=>JsonSerializer.Serialize(new{tag_name=tag,prerelease=preview,draft,assets=assets?new[]{new{name="TypeMaster-Native-99.0.0-Setup.exe",size=size??bytes.Length,digest=hash??"sha256:"+digest,browser_download_url=url??AppUpdates.Releases+"/download/v99.0.0/TypeMaster-Native-99.0.0-Setup.exe"}}:[]});
        var release=AppUpdates.Parse(Json(),new Version(1,0,0))!;
        check(release.Version==new Version(99,0,0)&&release.CanInstall,"Updater selects the version-matched Windows installer and GitHub SHA-256 digest");
        check(AppUpdates.Parse(Json(),new Version(99,0,0)) is null&&AppUpdates.Parse(Json(),new Version(100,0,0)) is null,"Updater never offers the same version or a downgrade");
        check(AppUpdates.Parse(Json(preview:true),new Version(1,0,0)) is null&&AppUpdates.Parse(Json(draft:true),new Version(1,0,0)) is null,"Updater skips preview and draft releases");
        bool Reject(string json){try{AppUpdates.Parse(json,new Version(1,0,0));return false;}catch(Exception ex) when(ex is InvalidDataException or JsonException){return true;}}
        check(Reject(Json(tag:"v99.0.0-beta")),"Updater rejects unsupported or executable-looking version tags");
        check(Reject(Json(url:"https://example.com/TypeMaster-Native-99.0.0-Setup.exe")),"Updater rejects installer assets outside the TypeMaster GitHub repository");
        check(Reject(Json(url:AppUpdates.Releases+"/download/v1.0.0/TypeMaster-Native-99.0.0-Setup.exe")),"Updater rejects installer URLs from a mismatched release");
        check(Reject(Json(size:AppUpdates.MaximumInstallerBytes+1)),"Updater rejects oversized installer metadata");
        check(!AppUpdates.Parse(Json(hash:"sha256:invalid"),new Version(1,0,0))!.CanInstall&&!AppUpdates.Parse(Json(assets:false),new Version(1,0,0))!.CanInstall,"Missing checksums or Windows installers leave release browsing available without enabling execution");
        Task.Run(async()=>
        {
            using var client=new HttpClient(new FixtureHandler(request=>new(HttpStatusCode.OK){Content=request.RequestUri!.AbsoluteUri==AppUpdates.LatestApi?new StringContent(Json()):new ByteArrayContent(bytes)}));
            var service=new AppUpdates(client);check((await service.CheckAsync(new Version(1,0,0)))?.Version==release.Version,"Update metadata is read asynchronously through the GitHub release endpoint");
            string startupFolder = Path.Combine(folder, "startup");
            new UpdatePreferences { AutomaticChecks = true, LastCheckedUtc = DateTime.UtcNow, DismissedVersion = "99.0.0" }.Save(startupFolder);
            int startupRequests = 0;
            using var startupClient = new HttpClient(new FixtureHandler(_ => { startupRequests++; return new(HttpStatusCode.OK) { Content = new StringContent(Json()) }; }));
            var startupService = new AppUpdates(startupClient);
            var firstLaunch = await startupService.CheckStartupAsync(new Version(1,0,0), startupFolder);
            var secondLaunch = await startupService.CheckStartupAsync(new Version(1,0,0), startupFolder);
            check(startupRequests == 2 && firstLaunch?.Version == release.Version && secondLaunch?.Version == release.Version, "Each launch checks GitHub even after a recent check or a previous dismissal");
            check(await startupService.CheckStartupAsync(new Version(99,0,0), startupFolder) is null && startupRequests == 3, "An up-to-date startup produces no update to notify or open");
            (UpdatePreferences.Load(startupFolder) with { AutomaticChecks = false }).Save(startupFolder);
            check(await startupService.CheckStartupAsync(new Version(1,0,0), startupFolder) is null && startupRequests == 3, "Disabling automatic checks prevents startup network requests");
            string file=await service.DownloadAsync(release,folder);check(File.Exists(file)&&await AppUpdates.VerifyAsync(file,digest),"A matching installer download is verified before becoming installable");
            await File.WriteAllTextAsync(file,"altered");check(!await AppUpdates.VerifyAsync(file,digest),"Installer verification detects changes after download");
            using var corruptClient=new HttpClient(new FixtureHandler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(Enumerable.Repeat((byte)42,bytes.Length).ToArray())}));
            bool rejected=false;try{await new AppUpdates(corruptClient).DownloadAsync(release,folder);}catch(InvalidDataException){rejected=true;}
            check(rejected&&!Directory.EnumerateFiles(folder,"*.partial",SearchOption.AllDirectories).Any(),"Checksum failure removes the partial download and never returns an installer path");
            using var shortClient=new HttpClient(new FixtureHandler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes[..4])}));rejected=false;try{await new AppUpdates(shortClient).DownloadAsync(release,folder);}catch(InvalidDataException){rejected=true;}
            check(rejected,"Truncated installer downloads are rejected");
            using var offlineClient=new HttpClient(new FixtureHandler(_=>throw new HttpRequestException("Offline fixture")));rejected=false;try{await new AppUpdates(offlineClient).CheckAsync(new Version(1,0,0));}catch(HttpRequestException){rejected=true;}
            check(rejected,"Offline update checks report a connection failure without changing candidate records");
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();rejected=false;try{await service.DownloadAsync(release,folder,cancel:cancelled.Token);}catch(OperationCanceledException){rejected=true;}
            check(rejected&&!Directory.EnumerateFiles(folder,"*.partial",SearchOption.AllDirectories).Any(),"Cancelled updates do not leave executable partial downloads");
        }).GetAwaiter().GetResult();
        check(UpdatePreferences.Load(folder).AutomaticChecks,"Automatic update checks are enabled for a new installation");
        var settings=new UpdatePreferences{AutomaticChecks=false,LastCheckedUtc=new DateTime(2026,10,25,0,0,0,DateTimeKind.Utc),DismissedVersion="99.0.0"};settings.Save(folder);
        check(UpdatePreferences.Load(folder)==settings,"Automatic-update preference and legacy check metadata persist");
        File.WriteAllText(Path.Combine(folder,"updates.json"),"{damaged");check(!UpdatePreferences.Load(folder).AutomaticChecks,"Damaged update preferences fall back to manual checks");
        using var uiClient=new HttpClient(new FixtureHandler(_=>new(HttpStatusCode.OK){Content=new StringContent(Json())}));
        var window=new UpdatesWindow(folder,_=>throw new InvalidOperationException("The UI capture must not launch an installer."),new AppUpdates(uiClient)){ShowInTaskbar=false};window.Show();window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);window.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(Path.Combine(output,"native-updates-window.png")))encoder.Save(stream);window.Close();
        check(true,"Updates and GitHub window renders without downloading or executing an installer");
    }
}
