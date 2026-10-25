using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace TypeMaster.Native;

internal sealed record ReleaseUpdate(Version Version,string Tag,string FileName,Uri? Installer,long Size,string? Sha256)
{
    internal bool CanInstall=>Installer is not null&&Sha256 is not null;
}

internal sealed record UpdatePreferences
{
    public bool AutomaticChecks {get;init;}=true;
    public DateTime LastCheckedUtc {get;init;}
    public string DismissedVersion {get;init;}="";
    internal static UpdatePreferences Load(string directory)
    {
        string file=Path.Combine(directory,"updates.json");
        try{return File.Exists(file)?JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllText(file))??new():new();}
        catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException){return new(){AutomaticChecks=false};}
    }
    internal void Save(string directory)
    {
        Directory.CreateDirectory(directory);string file=Path.Combine(directory,"updates.json"),temp=file+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(this));File.Move(temp,file,true);
    }
}

internal sealed class AppUpdates
{
    internal const string Project="https://github.com/yateshr/TypeMaster";
    internal const string Releases=Project+"/releases";
    internal const string LatestApi="https://api.github.com/repos/yateshr/TypeMaster/releases/latest";
    internal const long MaximumInstallerBytes=300L*1024*1024;
    readonly HttpClient client;
    internal AppUpdates(HttpClient? client=null)=>this.client=client??SharedClient;
    static readonly HttpClient SharedClient=CreateClient();
    static HttpClient CreateClient()
    {
        var result=new HttpClient{Timeout=TimeSpan.FromMinutes(10)};
        result.DefaultRequestHeaders.UserAgent.ParseAdd("TypeMaster/"+AppInfo.Version);
        result.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return result;
    }
    internal static ReleaseUpdate? Parse(string json,Version current)
    {
        using var document=JsonDocument.Parse(json);var root=document.RootElement;
        if(root.TryGetProperty("draft",out var draft)&&draft.GetBoolean()||root.TryGetProperty("prerelease",out var preview)&&preview.GetBoolean())return null;
        string tag=root.GetProperty("tag_name").GetString()??"";
        if(!Regex.IsMatch(tag,@"^v?\d+\.\d+\.\d+$",RegexOptions.CultureInvariant)||!Version.TryParse(tag.TrimStart('v'),out var version))throw new InvalidDataException("The release has an unsupported version tag.");
        if(version<=current)return null;
        string file="TypeMaster-Native-"+version.ToString(3)+"-Setup.exe";
        string expected=Releases+"/download/"+Uri.EscapeDataString(tag)+"/"+file;
        foreach(var asset in root.GetProperty("assets").EnumerateArray())
        {
            if(asset.GetProperty("name").GetString()!=file)continue;
            string url=asset.GetProperty("browser_download_url").GetString()??"";
            if(!string.Equals(url,expected,StringComparison.Ordinal))throw new InvalidDataException("The update installer is not hosted in the TypeMaster release.");
            long size=asset.GetProperty("size").GetInt64();if(size<=0||size>MaximumInstallerBytes)throw new InvalidDataException("The update installer has an invalid size.");
            string? digest=asset.TryGetProperty("digest",out var value)?value.GetString():null;
            string? hash=digest is not null&&Regex.IsMatch(digest,@"^sha256:[a-fA-F0-9]{64}$",RegexOptions.CultureInvariant)?digest[7..]:null;
            return new(version,tag,file,new Uri(expected),size,hash);
        }
        return new(version,tag,file,null,0,null);
    }
    internal async Task<ReleaseUpdate?> CheckAsync(Version current,CancellationToken cancel=default)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel);timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var response=await client.GetAsync(LatestApi,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
        if(response.StatusCode==HttpStatusCode.NotFound)return null;
        response.EnsureSuccessStatusCode();
        await using var input=await response.Content.ReadAsStreamAsync(timeout.Token);using var data=new MemoryStream();var buffer=new byte[16384];
        int read;while((read=await input.ReadAsync(buffer,timeout.Token))>0){if(data.Length+read>2*1024*1024)throw new InvalidDataException("The release response is too large.");data.Write(buffer,0,read);}
        return Parse(System.Text.Encoding.UTF8.GetString(data.ToArray()),current);
    }
    internal static async Task<bool> VerifyAsync(string file,string hash,CancellationToken cancel=default)
    {
        if(!Regex.IsMatch(hash,@"^[a-fA-F0-9]{64}$",RegexOptions.CultureInvariant))return false;
        await using var input=File.OpenRead(file);var actual=await SHA256.HashDataAsync(input,cancel);
        return CryptographicOperations.FixedTimeEquals(actual,Convert.FromHexString(hash));
    }
    internal async Task<string> DownloadAsync(ReleaseUpdate update,string directory,IProgress<double>? progress=null,CancellationToken cancel=default)
    {
        // Revalidate even when the caller supplies a record instead of parsed GitHub metadata.
        string expected="TypeMaster-Native-"+update.Version.ToString(3)+"-Setup.exe";
        if(!update.CanInstall||update.FileName!=expected||!Regex.IsMatch(update.Tag,@"^v?\d+\.\d+\.\d+$")||update.Installer!.AbsoluteUri!=Releases+"/download/"+update.Tag+"/"+expected||update.Size<=0||update.Size>MaximumInstallerBytes)throw new InvalidDataException("A verified TypeMaster installer is not available for this release. Use the releases page.");
        string folder=Path.Combine(directory,"Updates",update.Version+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string partial=Path.Combine(folder,expected+".partial"),file=Path.Combine(folder,expected);
        try
        {
            using var response=await client.GetAsync(update.Installer,HttpCompletionOption.ResponseHeadersRead,cancel);response.EnsureSuccessStatusCode();
            var final=response.RequestMessage?.RequestUri;
            if(final is not null&&(final.Scheme!="https"||final.Host is not("github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com")))throw new InvalidDataException("The update download redirected to an unexpected host.");
            if(response.Content.Headers.ContentLength is long size&&size!=update.Size)throw new InvalidDataException("The installer download size does not match the release.");
            await using(var input=await response.Content.ReadAsStreamAsync(cancel))
            await using(var output=new FileStream(partial,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true))
            {
                var buffer=new byte[65536];long total=0;int read;
                while((read=await input.ReadAsync(buffer,cancel))>0){total+=read;if(total>update.Size)throw new InvalidDataException("The installer download exceeded its expected size.");await output.WriteAsync(buffer.AsMemory(0,read),cancel);progress?.Report(total*100d/update.Size);}
                if(total!=update.Size)throw new InvalidDataException("The installer download is incomplete.");
            }
            if(!await VerifyAsync(partial,update.Sha256!,cancel))throw new InvalidDataException("Installer verification failed. The downloaded file will not be run.");
            File.Move(partial,file);return file;
        }
        catch{if(File.Exists(partial))File.Delete(partial);throw;}
    }
}
