using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
namespace TypeMaster.Native;
internal sealed record CompanyBranding
{
    public string Name {get;set;}=ReportBranding.DefaultCompany;
    public string? FirstLogo {get;set;}
    public string? SecondLogo {get;set;}
}
internal static class BrandingStorage
{
    internal static CompanyBranding Load(string directory)
    {
        string file=Path.Combine(directory,"company-branding.json");
        if(!File.Exists(file)&&!File.Exists(file+".bak"))return new();
        Exception? failure=null;
        foreach(string path in new[]{file,file+".bak"})
        {
            if(!File.Exists(path))continue;
            try{var value=JsonSerializer.Deserialize<CompanyBranding>(File.ReadAllText(path))??throw new InvalidDataException("Empty branding settings.");Validate(value);return value;}catch(Exception ex)when(ex is JsonException or InvalidDataException or FormatException or NotSupportedException or ArgumentException){failure=ex;}
        }
        throw new InvalidDataException("Saved company branding could not be read. Your settings files have been preserved.",failure);
    }
    internal static void Validate(CompanyBranding value)
    {
        if(string.IsNullOrWhiteSpace(value.Name)||value.Name.Length>200||value.Name.Any(char.IsControl))throw new InvalidDataException("Enter a company name of 1 to 200 characters.");
        foreach(string? logo in new[]{value.FirstLogo,value.SecondLogo})if(!string.IsNullOrEmpty(logo))_ = Image(logo);
    }
    static readonly Dictionary<string,BitmapSource> imageCache=[];
    internal static BitmapSource Image(string encoded)
    {
        if(imageCache.TryGetValue(encoded,out var cached))return cached;
        if(encoded.Length>8_000_000)throw new InvalidDataException("The saved logo is too large.");
        using var stream=new MemoryStream(Convert.FromBase64String(encoded));var bitmap=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];bitmap.Freeze();if(imageCache.Count>=8)imageCache.Clear();imageCache[encoded]=bitmap;return bitmap;
    }
    internal static string Import(string file)
    {
        using var input=File.OpenRead(file);var source=BitmapDecoder.Create(input,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
        if((long)source.PixelWidth*source.PixelHeight>40_000_000)throw new InvalidDataException("Choose a logo smaller than 40 megapixels.");
        System.Windows.Media.Imaging.BitmapSource image=source;double scale=Math.Min(1,1024d/Math.Max(source.PixelWidth,source.PixelHeight));
        if(scale<1)image=new TransformedBitmap(source,new System.Windows.Media.ScaleTransform(scale,scale));
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var result=new MemoryStream();encoder.Save(result);return Convert.ToBase64String(result.ToArray());
    }
    internal static void Save(string directory,CompanyBranding value)
    {
        Validate(value);Directory.CreateDirectory(directory);string file=Path.Combine(directory,"company-branding.json"),temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));if(File.Exists(file))File.Replace(temporary,file,file+".bak",true);else File.Move(temporary,file);}finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}



