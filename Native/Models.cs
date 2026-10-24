using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TypeMaster.Native;

public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.Now;
    public bool Dark { get; set; }
    public bool Sound { get; set; } = true;
    public string SpeedUnit { get; set; } = "WPM";
    public string Language { get; set; } = "eng";
    public string Keyboard { get; set; } = "us.kbd";
    public string HintSpeed { get; set; } = "Normal";
    public string ScreenMode { get; set; } = "Windowed";
    public HashSet<string> TechnicalSkills { get; set; } = [];
    public HashSet<string> CustomTechnicalSkills { get; set; } = [];
    public Dictionary<string,string> TechnicalSkillApplications {get;set;}=[];
    public string AssessmentRemarks { get; set; } = "";
    public string TestTakenBy { get; set; } = "";
    public string TrainingProfile { get; set; } = "Default Training Profile";
    public int TestMinutes { get; set; } = 10;
    public bool OptimalDuration { get; set; } = true;
    public Dictionary<string, int> CourseDurationMinutes { get; set; } = [];
    public string CourseFile { get; set; } = "";
    public DateTime LastUsed { get; set; } = DateTime.Now;
    public int Sessions { get; set; }
    public bool BreakReminder { get; set; } = true;
    public int BreakMinutes { get; set; } = 120;
    public bool TrainingReminder { get; set; } = true;
    public List<GameResult> Games { get; set; } = [];
    public List<MeterDay> Meter { get; set; } = [];
    public List<TypingResult> Results { get; set; } = [];
    public HashSet<string> Completed { get; set; } = [];
    public Dictionary<char, int> Mistakes { get; set; } = [];
    public override string ToString() => Name;
}
public sealed class TypingResult
{
    public Guid AttemptId { get; set; }
    public string ScoringPolicy { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;
    public double Seconds { get; set; }
    public int GrossWpm { get; set; }
    public int NetWpm { get; set; }
    public int Accuracy { get; set; }
    public int Errors { get; set; }
    public int Correct { get; set; }
    public string Kind { get; set; } = "Practice";
    public bool Completed { get; set; }
    public DateTime StartedAt { get; set; }
    public int PlannedSeconds { get; set; }
    public string OriginalText { get; set; } = "";
    public string ExpectedText { get; set; } = "";
    public string TypedText { get; set; } = "";
    public bool HasTextSnapshot { get; set; }
    public int GrossStrokes { get; set; }
    public int NetStrokes { get; set; }
    public int ErrorHits { get; set; }
    public int RequiredAccuracy { get; set; }
    public int RequiredWpm { get; set; }
    public bool CriteriaRecorded { get; set; }
    public string CourseFile { get; set; } = "";
    public string LessonId { get; set; } = "";
    public Dictionary<char, int> Mistakes { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore] public bool Passed => Completed && Accuracy >= RequiredAccuracy && NetWpm >= RequiredWpm;
    public override string ToString() => Date.ToString("g") + " · " + NetWpm + " WPM · " + Title;
}
public sealed record GameResult(string Game, int Score, int Errors, double Seconds, DateTime Date);
public sealed class MeterDay
{
    public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public int Keys { get; set; }
    public int Corrections { get; set; }
    public double ActiveSeconds { get; set; }
    public Dictionary<int, int> KeyCounts { get; set; } = [];
    public Dictionary<int, int> CorrectedKeys { get; set; } = [];
    public Dictionary<string, PairTiming> Pairs { get; set; } = [];
}
public sealed class PairTiming { public int Count { get; set; } public double TotalMilliseconds { get; set; } }
public static class Scoring
{
    public static TypingResult MeasureTest(string target, string typed, double seconds, int attempts, int errors)
    {
        var result = Measure(target, typed, seconds, attempts, errors);
        result.GrossStrokes = typed.Length; result.ErrorHits = errors * 5;
        result.NetStrokes = Math.Max(0, result.GrossStrokes - result.ErrorHits);
        double minutes = Math.Max(seconds, 1) / 60;
        result.GrossWpm = (int)Math.Floor(result.GrossStrokes / 5d / minutes);
        result.NetWpm = (int)Math.Floor(result.NetStrokes / 5d / minutes);
        result.Accuracy = result.GrossStrokes == 0 ? 100 : (int)Math.Floor(result.NetStrokes * 100d / result.GrossStrokes);
        result.ScoringPolicy = "test-five-stroke-truncate-v1";
        return result;
    }
    public static TypingResult Measure(string target, string typed, double seconds, int attempts, int errors)
    {
        int correct = typed.Where((c, i) => i < target.Length && c == target[i]).Count();
        double minutes = Math.Max(seconds, 1) / 60;
        return new TypingResult { Seconds = Math.Round(seconds, 1), Correct = correct, Errors = errors,
            GrossWpm = (int)Math.Round(typed.Length / 5d / minutes), NetWpm = (int)Math.Round(correct / 5d / minutes),
            Accuracy = attempts == 0 ? 100 : (int)Math.Round(Math.Clamp((attempts - errors) * 100d / attempts, 0, 100)) };
    }
}
public sealed class ProfileStore
{
    public string? RecoveryNotice { get; private set; }
    public string DirectoryPath { get; }
    public List<Profile> Profiles { get; private set; } = [];
    readonly JsonSerializerOptions options = new() { WriteIndented = true };
    public ProfileStore(string? directory = null)
    {
        DirectoryPath = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TypeMasterNative");
        Directory.CreateDirectory(DirectoryPath);
        var file = Path.Combine(DirectoryPath, "profiles.json");
        if (File.Exists(file))
        {
            try { Profiles = ReadProfiles(file); }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                string backup = file + ".bak";
                if (!File.Exists(backup)) throw new InvalidDataException("The profile file is damaged and no backup is available. Your file is preserved at " + file, ex);
                try { Profiles = ReadProfiles(backup); }
                catch (Exception backupError) when (backupError is JsonException or InvalidDataException) { throw new InvalidDataException("Both profile files are damaged. They have been preserved in " + DirectoryPath, backupError); }
                string preserved = file + ".damaged-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                File.Move(file, preserved); File.Copy(backup, file);
                RecoveryNotice = "Your profiles were automatically restored from the last backup. The damaged file was preserved at " + preserved;
            }
        }
        else if (File.Exists(file + ".bak")) { Profiles = ReadProfiles(file + ".bak"); File.Copy(file + ".bak", file); RecoveryNotice = "Your missing profile file was restored from its backup."; }
    }
    static List<Profile> ReadProfiles(string file)
    {
        var profiles = JsonSerializer.Deserialize<List<Profile>>(File.ReadAllText(file)) ?? throw new InvalidDataException("Empty profile data.");
        if (profiles.Any(p => p == null || string.IsNullOrWhiteSpace(p.Name) || p.TechnicalSkillApplications == null || p.CustomTechnicalSkills == null || p.TechnicalSkills == null || p.TestTakenBy == null || p.AssessmentRemarks == null || p.CourseDurationMinutes == null || p.Results == null || p.Completed == null || p.Mistakes == null || p.Games == null || p.Meter == null)) throw new InvalidDataException("Incomplete profile data.");
        return profiles;
    }
    public void Save()
    {
        string file = Path.Combine(DirectoryPath, "profiles.json"), temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Profiles, options), Encoding.UTF8);
        if (File.Exists(file)) File.Replace(temp, file, file + ".bak", true);
        else File.Move(temp, file);
    }
}
public sealed record TestText(string File, string Name, string Language)
{
    public string Description { get; init; } = "";
    public int MinAccuracy { get; init; }
    public int MinSpeed { get; init; }
    public int Minutes { get; init; } = 10;
    public override string ToString() => Name;
}
public sealed record Lesson(string Id, string Title, string Text, int Seconds, string Kind = "Practice", string Image = "", string Keys = "", string Generator = "");
public sealed class ExerciseGroup
{
    public string Name { get; set; } = "Practice";
    public List<Lesson> Events { get; } = [];
    public int Seconds => Events.Where(e => e.Kind != "Key introduction").Sum(e => e.Seconds);
    public bool IsComplete(Profile profile) => Events.Count > 0 && Events.All(e => profile.Completed.Contains(e.Id));
}
public sealed class Chapter
{
    public string Name { get; set; } = "";
    public List<Lesson> Lessons { get; } = [];
    public List<ExerciseGroup> Groups { get; } = [];
}
public static class Assets
{
    public static string Root => Path.Combine(AppContext.BaseDirectory, "Assets");
    public static string Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(1252).GetString(bytes); }
    }
    public static string Normalize(string text) => Regex.Replace(text.Replace("$loop$", ""), @"\s+", " ").Trim();
    public static List<TestText> Tests()
    {
        return Directory.EnumerateFiles(Path.Combine(Root, "texts"), "*.exm").Select(file =>
        {
            string info = Path.ChangeExtension(file, ".exi");
            string meta = File.Exists(info) ? Read(info) : "";
            string Get(string key) => Regex.Match(meta, "^" + key + "=(.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value.Trim();
            int Number(string key, int fallback = 0) => int.TryParse(Get(key), out int value) ? value : fallback;
            string name = Get("nameunicode"); if (name.Length == 0) name = Get("name");
            return new TestText(file, string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file) : name, Get("language")) { Description = Get("descriptionunicode").Length > 0 ? Get("descriptionunicode") : Get("copyright"), MinAccuracy = Number("MinAccuracy"), MinSpeed = Number("MinSpeed"), Minutes = Number("time", 10) };
        }).OrderBy(t => t.Name).ToList();
    }
    // Import the actual QWERTY text exercises. Other event engines are not treated as text drills.
    public static List<Chapter> Course()
    {
        var chapters = new List<Chapter>();
        Chapter? chapter = null;
        string exercise = "Practice";
        int sequence = 0;
        foreach (string raw in File.ReadLines(Path.Combine(Root, "lessons", "uk7qwert.cnt")))
        {
            string line = raw.Trim();
            if (line.StartsWith("chapter name="))
            {
                string title = line[13..];
                chapter = new Chapter { Name = (chapters.Count + 1) + ". " + (title.Contains(';') ? title[(title.IndexOf(';') + 1)..] : title) };
                chapters.Add(chapter);
            }
            if (line.StartsWith("type=")) exercise = line[5..].Trim();
            var match = Regex.Match(line, @"^EVENT(?:_QWERTY)?=(KEY2|WORD2|SENTENCE2|PARAGRAPH2);(\d+);file:([^;]+)");
            if (!match.Success || chapter == null) continue;
            string file = Path.Combine(Root, "lessons", Path.GetFileName(match.Groups[3].Value));
            if (!File.Exists(file)) continue;
            string text = Normalize(Read(file));
            if (text.Length == 0) continue;
            int seconds = Math.Clamp(int.Parse(match.Groups[2].Value) * 60, 60, 600);
            chapter.Lessons.Add(new Lesson("uk7qwert:" + sequence++, exercise, text, seconds));
        }
        return chapters.Where(c => c.Lessons.Count > 0).ToList();
    }
}






