using System.Reflection;

namespace TypeMaster.Native;

internal static class AppInfo
{
    static readonly Assembly Assembly=typeof(AppInfo).Assembly;
    internal static System.Windows.Media.ImageSource WindowIcon { get; }=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/TypeMaster;component/TypeMaster.ico",UriKind.Absolute));
    internal static string Version=>Assembly.GetName().Version?.ToString(3)??"Unknown";
    internal static string Build=>Metadata("Build");
    internal static string Developer=>Metadata("Developer");
    static string Metadata(string key)=>Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a=>a.Key==key)?.Value??"Unknown";
}
