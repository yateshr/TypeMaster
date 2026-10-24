using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace TypeMaster.Native;
public sealed class CourseDefinition
{
    public string File { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Language { get; set; } = "eng";
    public List<Chapter> Chapters { get; set; } = [];
    public int MissingEvents { get; set; }
    public List<string> SourceGaps { get; set; } = [];
    public string Keyboard { get; set; } = "us.kbd";
    public int AccuracyGoal { get; set; } = 94;
    public int DurationMargin { get; set; } = 2;
    public bool Universal { get; set; }
    public string DurationRange => $"{TimeSpan.FromSeconds(Chapters.Sum(c => c.Groups.Sum(g => g.Seconds))):h\\:mm} - {TimeSpan.FromSeconds(Chapters.Sum(c => c.Groups.Sum(g => g.Seconds + DurationMargin * 60))):h\\:mm} hours";
    public override string ToString() => Name;
}
public static class CourseLibrary
{
    public static List<CourseDefinition> LoadAll() => Directory.GetFiles(Path.Combine(Assets.Root, "lessons"), "*.cnt").Select(Load).Where(c => c.Chapters.Count > 0).ToList();
    public static CourseDefinition Load(string file)
    {
        var data = Assets.Read(file);
        string Meta(string name) => Regex.Match(data, @"^\$" + name + "=(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
        var course = new CourseDefinition { File = Path.GetFileName(file), Name = Meta("description"), Description = Meta("longdesc"), Language = Meta("language") };
        if (course.Name.Length == 0) course.Name = Path.GetFileNameWithoutExtension(file);
        course.Language = NormalizeLanguage(course.Language, Path.GetFileName(file));
        string Setting(string name) => Regex.Match(data, @"^" + name + "=(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
        course.Keyboard = Setting("keyboard").Length > 0 ? Setting("keyboard") : "us.kbd";
        var goals = Setting("accuracygoals").Split(','); if (goals.Length > 1 && int.TryParse(goals[1], out int goal)) course.AccuracyGoal = goal;
        course.Universal = data.Contains("PHYSGEN:", StringComparison.OrdinalIgnoreCase);
        if (int.TryParse(Setting("dynamicdurationmargin"), out int margin)) course.DurationMargin = margin;
        Chapter? chapter = null; ExerciseGroup? group = null; string title = "Practice", chapterKeys = "asdfjkl;"; int sequence = 0;
        void AddEvent(Lesson lesson)
        {
            chapter!.Lessons.Add(lesson);
            if (group == null) { group = new ExerciseGroup { Name = title }; chapter.Groups.Add(group); }
            group.Events.Add(lesson);
        }
        foreach (var raw in data.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("chapter name="))
            {
                var name = line[13..]; chapter = new Chapter { Name = (course.Chapters.Count + 1) + ". " + name[(name.IndexOf(';') + 1)..] }; course.Chapters.Add(chapter); group = null;
            }
            if (line.Equals("begin exercise", StringComparison.OrdinalIgnoreCase) && chapter != null) { group = new ExerciseGroup(); chapter.Groups.Add(group); }
            if (line.StartsWith("type=")) { title = line[5..].Trim(); if (group != null) group.Name = title; }
            if (line.StartsWith("new keys_qwerty=")) { string value = line[(line.IndexOf('=') + 1)..]; chapterKeys = value == "SEMICOLON" ? ";" : value; }
            if (chapter == null) continue;
            var match = Regex.Match(line, @"^EVENT(?:_QWERTY|_09)?=(.*)", RegexOptions.IgnoreCase);
            if (!match.Success) continue;
            var parts = match.Groups[1].Value.Replace("%;", "\u0001").Split(';').Select(p => p.Replace('\u0001', ';')).ToArray(); if (parts.Length < 3) continue;
            string type = parts[0].ToUpperInvariant(), argument = parts[2];
            int seconds = int.TryParse(parts[1], out int min) ? Math.Clamp(min * 60, 60, 600) : 180;
            string source = argument.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? argument[5..] : argument;
            string full = Path.Combine(Assets.Root, "lessons", Path.GetFileName(source));
            string text = "", image = "", keys = "", kind = "Practice";
            if (type == "TEST") { kind = "Choose test"; text = "Choose a test from the supplied library."; }
            else if (type == "HTMLMESSAGE")
            {
                if (!File.Exists(full))
                {
                    course.MissingEvents++; course.SourceGaps.Add(argument);
                    AddEvent(new Lesson(Path.GetFileNameWithoutExtension(file) + ":source:" + sequence++, parts.Length > 3 ? parts[3] : title, "This optional page references content that was not included in the supplied asset folder.\n\nSource: " + argument + "\n\nContinue to the next local activity.", 0, "Source note"));
                    continue;
                }
                string html = Assets.Read(full); image = Regex.Match(html, "<img[^>]+src=[\"']([^\"']+)", RegexOptions.IgnoreCase).Groups[1].Value;
                image = SafeImage(full, image); text = HtmlText(html); kind = "Introduction";
            }
            else if (type == "MESSAGE")
            {
                text = File.Exists(full) ? Assets.Read(full) : Uri.UnescapeDataString(argument); kind = "Introduction";
                if (parts.Length > 4) image = SafeImage(full, parts[4]);
            }
            else if (type == "KEYINTRO")
            {
                keys = argument.StartsWith("HOMEROW:", StringComparison.OrdinalIgnoreCase) ? "@home" : parts.Length > 3 ? Uri.UnescapeDataString(parts[3]) : "asdfjkl;";
                if (keys is "HOMEROW" or "PRESS" or "SHOW" || keys.Contains(':')) keys = "asdfjkl;";
                text = File.Exists(full) ? Assets.Read(full) : "Place your fingers on the home row. Use the highlighted fingers and keys.";
                kind = "Key introduction";
                image = keys.Contains(' ') ? "keyboard_spacebar.gif" : keys.Any(char.IsUpper) ? "keyboard_shift.gif" : "keyboard_home_row.gif";
            }
            else if (new[] { "KEY2", "WORD2", "SENTENCE2", "PARAGRAPH2", "TEXT2", "KEYBSENTENCE", "KEYBOARD", "WORDS", "WORD", "TEXT", "SENTENCE", "PARAGRAPH", "NEWKEYS", "KEYBROW", "BUILDER", "EXAM", "EXAM2", "EASYEXAM" }.Contains(type))
            {
                if (File.Exists(full)) text = Assets.Normalize(Assets.Read(full));
                else if (argument.StartsWith("PHYSGEN:", StringComparison.OrdinalIgnoreCase) || argument.StartsWith("HOMEROW:", StringComparison.OrdinalIgnoreCase) || argument.StartsWith("keybuilder:", StringComparison.OrdinalIgnoreCase) || type == "BUILDER")
                {
                    text = Generate(argument, chapterKeys);
                    if (argument.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) { course.MissingEvents++; course.SourceGaps.Add(source); }
                }
                else if (!argument.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && !Regex.IsMatch(argument, @"\.(?:txt|exc|htm|html|int)$", RegexOptions.IgnoreCase)) text = Assets.Normalize(Uri.UnescapeDataString(argument));
                else { course.MissingEvents++; course.SourceGaps.Add(source); continue; }
                if (type is "EXAM" or "EXAM2" or "EASYEXAM") kind = "Test";
            }
            else if (new[] { "WORDTRIS", "CLOUDS", "BUBBLES", "ABC", "PIPE", "CLOUDRACE", "METEOR" }.Contains(type))
            { kind = "Game"; text = type; }
            else { course.MissingEvents++; course.SourceGaps.Add("Unsupported event " + type); continue; }
            if (string.IsNullOrWhiteSpace(text)) continue;
            string label = (kind == "Introduction" && parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])) ? parts[3] : title;
            // Stable IDs use the original source/event, so adding introductions does not renumber practice progress.
            string id = Path.GetFileNameWithoutExtension(file) + ":" + course.Chapters.Count + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(line)))[..16];
            AddEvent(new Lesson(id, label, text.Trim(), seconds, kind, image, keys, argument.StartsWith("PHYSGEN:", StringComparison.OrdinalIgnoreCase) ? argument : ""));
        }
        foreach (var c in course.Chapters) c.Groups.RemoveAll(g => g.Events.Count == 0);
        course.Chapters.RemoveAll(c => c.Lessons.Count == 0); return course;
    }
    static string SafeImage(string page, string image)
    {
        if (string.IsNullOrWhiteSpace(image) || image == "0") return "";
        string full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, image.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(full)) full = Path.Combine(Assets.Root, Path.GetFileName(image));
        return File.Exists(full) && full.StartsWith(Assets.Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? Path.GetRelativePath(Assets.Root, full) : "";
    }
    internal static string HtmlText(string html)
    {
        html = Regex.Replace(html, @"<(head|script|style)\b[^>]*>.*?</\1>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<!--.*?(?:-->|--!>)", "", RegexOptions.Singleline);
        html = Regex.Replace(html, @"<\s*(?:br\s*/?|/p|/li|/h[1-6]|/tr)\s*>", "\n\n", RegexOptions.IgnoreCase);
        string plain = Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")), @"[ \t]+", " ");
        return Regex.Replace(plain, @"\s*\n\s*", "\n").Trim();
    }
    static string NormalizeLanguage(string language, string file)
    {
        language = language.Trim().ToLowerInvariant();
        if (language.Length == 0) { string f = file.ToLowerInvariant(); language = f.StartsWith("fi") ? "fin" : f.StartsWith("fr") ? "fre" : f.StartsWith("ge") ? "ger" : f.StartsWith("sp") ? "spa" : f.StartsWith("sw") ? "swe" : f.StartsWith("nl") ? "dut" : f.Contains("hindi") ? "hin" : "eng"; }
        return language switch { "tr" => "tur", "it" => "ita", "pt" => "por", "de" => "ger", "fr" => "fre", "en" => "eng", "nl" => "dut", "es" => "spa", "fi" => "fin", "se" or "sv" => "swe", _ => language };
    }
    internal static string Generate(string argument, string chapterKeys, KeyboardLayout? selectedLayout = null)
    {
        string keys = chapterKeys;
        if (argument.StartsWith("keybuilder:", StringComparison.OrdinalIgnoreCase)) keys = argument[(argument.IndexOf(':') + 1)..];
        else if (argument.StartsWith("PHYSGEN:", StringComparison.OrdinalIgnoreCase))
        {
            var fingers = Regex.Match(argument, @"fingers:([\d,]+)").Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
            var rows = Regex.Match(argument, @"row:([\d,]+)").Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
            var layout = selectedLayout ?? KeyboardLayout.Load(Path.Combine(Assets.Root, "keyboards", "us.kbd"));
            var positions = layout.Keys.Select(k => k.Y).Distinct().OrderBy(x => x).ToList();
            keys = string.Concat(layout.Keys.Where(k => k.Caption.Length == 1 && (fingers.Count == 0 || fingers.Contains(k.Finger)) && (rows.Count == 0 || rows.Contains(positions.IndexOf(k.Y) + 1))).Select(k => k.Caption));
        }
        else if (!argument.Contains(':') && !argument.Contains('.')) keys = Uri.UnescapeDataString(argument);
        if (keys.Length == 0 || keys.Contains(':')) keys = selectedLayout?.HomeKeys ?? "asdfjkl;";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(argument + keys)); var random = new Random(BitConverter.ToInt32(hash, 0));
        return string.Join(" ", Enumerable.Range(0, 55).Select(_ => string.Concat(Enumerable.Range(0, random.Next(2, 6)).Select(_ => keys[random.Next(keys.Length)]))));
    }
}

