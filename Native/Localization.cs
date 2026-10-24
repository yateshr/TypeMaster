using System.IO;
using System.Text.RegularExpressions;

namespace TypeMaster.Native;
static class Localization
{
    static readonly Dictionary<string, Dictionary<string, string>> cache = [];
    internal static string Language = "eng";
    internal static string ResourceCode(string language) => language switch { "fre" => "fr", "dut" => "nl", "ita" => "it", "por" => "pt", "tur" => "tr", _ => language };
    internal static Dictionary<string, string> Entries(string language)
    {
        string file = Path.Combine(Assets.Root, "language." + ResourceCode(language));
        if (!File.Exists(file)) return [];
        return Regex.Matches(Assets.Read(file), @"^(\d+)=(.*)$", RegexOptions.Multiline).GroupBy(m => m.Groups[1].Value).ToDictionary(g => g.Key, g => Clean(g.Last().Groups[2].Value));
    }
    static string Clean(string value) => value.Trim().Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&").Replace("\\+", " ").Replace("\\n", "\n").Trim();
    internal static string Translate(string text)
    {
        if (Language == "eng") return text;
        if (!cache.TryGetValue(Language, out var translations))
        {
            var english = Entries("eng"); var target = Entries(Language); translations = new(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in english) if (pair.Value.Length > 0 && target.TryGetValue(pair.Key, out var value) && value.Length > 0) translations.TryAdd(pair.Value, value);
            if (Language == "ger") foreach (var pair in new Dictionary<string,string> { ["Choose Course"]="Kurs wählen", ["Training Settings"]="Einstellungen", ["Studying"]="Kurs", ["Typing Meter"]="Satellit", ["Custom Review"]="Wiederholen", ["Typing Test"]="Schreibtest", ["Games"]="Spiele", ["Statistics"]="Statistiken", ["Settings"]="Einstellungen", ["Information"]="Informationen", ["About"]="Informationen", ["Close"]="Ende", ["Language"]="Sprache", ["Sounds"]="Ton", ["Speed Unit"]="Tempoeinheit", ["Screen Size"]="Bildschirmgröße", ["Keyboard Layout"]="Tastaturlayout", ["Change settings"]="Einstellungen ändern", ["Continue course"]="Kurs fortsetzen", ["Change course"]="Kurs wechseln" }) translations[pair.Key] = pair.Value;
            if (Language == "ger") foreach (var pair in new Dictionary<string,string> { ["Key Hint Speed"]="Key Hint Speed", ["Default (English US)"]="Standard (English US)", ["Advanced Typing Courses"]="Fortgeschrittene Schreibkurse", ["This setting will take\neffect the next time you\nstart this software."]="Diese Einstellung wird\nbeim nächsten Start\nberücksichtigt.", ["All exercises"]="Alle Übungen" }) translations[pair.Key]=pair.Value;
            cache[Language] = translations;
        }
        if (translations.TryGetValue(text, out string? translated)) return translated;
        foreach (string prefix in new[] { "▸ ", "▶ ", "⌄ " }) if (text.StartsWith(prefix)) return prefix + Translate(text[prefix.Length..]);
        return text;
    }
}
public partial class MainWindow
{
    internal static bool MatchesLanguage(string textLanguage, string profileLanguage)
    {
        string Canonical(string value) => value.ToLowerInvariant() switch { "fr" or "fra" => "fre", "nl" or "nld" => "dut", "pt" => "por", "it" => "ita", "tr" => "tur", "de" or "deu" => "ger", _ => value.ToLowerInvariant() };
        return Canonical(textLanguage).StartsWith(Canonical(profileLanguage), StringComparison.OrdinalIgnoreCase);
    }
}

