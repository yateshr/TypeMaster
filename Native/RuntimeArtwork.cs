using System.IO;
using System.Text.Json;
using System.Windows;

namespace TypeMaster.Native;

internal static class RuntimeArtwork
{
    static Dictionary<string,string>? replacements;
    static Point[] fingertips=[];
    static void Load()
    {
        if(replacements!=null)return;
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(Assets.Root,"TypeMasterArtwork","manifest.json")));
        var mappings=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var entry in manifest.RootElement.GetProperty("legacy").EnumerateObject())
            mappings.Add(entry.Name,"TypeMasterArtwork/"+entry.Value.GetProperty("file").GetString());
        fingertips=manifest.RootElement.GetProperty("fingertips").EnumerateArray().Select(p=>new Point(p[0].GetDouble(),p[1].GetDouble())).ToArray();
        if(fingertips.Length!=10)throw new InvalidDataException("The hands illustration needs ten finger positions.");
        replacements=mappings;
    }
    internal static Point FingerCue(int finger)
    {
        Load();var point=fingertips[Math.Clamp(finger,0,9)];return new Point(66+point.X,385+point.Y);
    }
    internal static string Resolve(string file)
    {
        // Only legacy root illustrations are remapped; company logos and imported
        // candidate/company images keep their authentic artwork.
        if (file.Contains('/') || file.Contains('\\')) return file;
        Load();
        return replacements!.GetValueOrDefault(file,file);
    }
}
