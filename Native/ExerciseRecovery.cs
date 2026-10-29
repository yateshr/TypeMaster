using System.IO;
using System.Text.Json;
using System.Windows;

namespace TypeMaster.Native;
public sealed class ExerciseDraft
{
    public Guid AttemptId { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = "";
    public string Typed { get; set; } = "";
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "Test";
    public string? LessonId { get; set; }
    public string CourseFile { get; set; } = "";
    public int Duration { get; set; }
    public double Elapsed { get; set; }
    public DateTime Started { get; set; }
    public int Attempts { get; set; }
    public int Errors { get; set; }
    public int Caret { get; set; }
    public Dictionary<char, int> Mistakes { get; set; } = [];
}
public partial class MainWindow
{
    Guid attemptId;
    double restoredElapsed;
    bool restoringDraft;
    double ExerciseSeconds => restoredElapsed + watch.Elapsed.TotalSeconds;
    string DraftFile => Path.Combine(Store.DirectoryPath, "exercise-" + Current!.Id.ToString("N") + ".json");
    readonly ExerciseDraftWriter draftWriter = new();
    internal void SaveExerciseDraft(bool background = false)
    {
        if (Current == null || !exercising || restoringDraft) return;
        if (typed.Length == 0) { ClearExerciseDraft(); return; }
        int caret = TypingBox == null ? typed.Length : TypingBox.Text[..Math.Min(TypingBox.CaretIndex, TypingBox.Text.Length)].Replace("\r\n", "\n").Replace('\r', '\n').Length;
        var draft = new ExerciseDraft { AttemptId = attemptId, Source = exerciseSourceText, Typed = typed, Title = exerciseTitle, Kind = kind, LessonId = lessonId, CourseFile = Current.CourseFile, Duration = duration, Elapsed = Math.Min(ExerciseSeconds, duration), Started = exerciseStarted, Attempts = attempts, Errors = errors, Caret = caret, Mistakes = new(sessionMistakes) };
        if (background) draftWriter.Queue(DraftFile, draft); else draftWriter.Save(DraftFile, draft);
    }
    void ClearExerciseDraft()
    {
        if (Current == null) return;
        draftWriter.Clear(DraftFile);
    }
    ExerciseDraft? ReadExerciseDraft()
    {
        foreach (string file in new[] { DraftFile, DraftFile + ".bak" })
        {
            if (!File.Exists(file)) continue;
            try
            {
                var draft = JsonSerializer.Deserialize<ExerciseDraft>(File.ReadAllText(file));
                if (draft == null || string.IsNullOrEmpty(draft.Source) || draft.Typed == null || draft.Mistakes == null || draft.Duration < 1 || !double.IsFinite(draft.Elapsed) || draft.Elapsed < 0 || draft.Elapsed > draft.Duration || draft.Attempts < 0 || draft.Errors < 0) continue;
                if (Current!.Results.Any(r => r.AttemptId == draft.AttemptId)) { ClearExerciseDraft(); return null; }
                return draft;
            }
            catch (JsonException) { }
        }
        return null;
    }
    void OfferExerciseRecovery()
    {
        if (ReadExerciseDraft() is not { } draft) return;
        var answer = MessageBox.Show(this, "An unfinished exercise was recovered: " + draft.Title + ".\n\nYes: resume with its saved time and text.\nNo: discard it.\nCancel: keep it for later.", "Resume unfinished exercise", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
        if (answer == MessageBoxResult.Yes) ResumeExerciseDraft();
        else if (answer == MessageBoxResult.No) ClearExerciseDraft();
    }
    internal bool ResumeExerciseDraft()
    {
        if (Current == null || ReadExerciseDraft() is not { } draft) return false;
        restoringDraft = true;
        try
        {
            Current.CourseFile = draft.CourseFile;
            StartExercise(draft.Source, draft.Title, draft.Duration, draft.Kind, draft.LessonId);
            if (draft.LessonId != null && allCourses.FirstOrDefault(c => c.File == draft.CourseFile) is { } course)
            {
                openedCourse = course; openedChapter = Math.Max(0, course.Chapters.FindIndex(c => c.Lessons.Any(e => e.Id == draft.LessonId)));
                openedGroup = course.Chapters[openedChapter].Groups.FirstOrDefault(g => g.Events.Any(e => e.Id == draft.LessonId));
                lessonSequence = course.Chapters.SelectMany(c => c.Lessons).ToList(); activeLesson = lessonSequence.FirstOrDefault(e => e.Id == draft.LessonId); lessonIndex = lessonSequence.FindIndex(e => e.Id == draft.LessonId);
            }
            attemptId = draft.AttemptId; exerciseStarted = draft.Started; restoredElapsed = draft.Elapsed;
            attempts = draft.Attempts; errors = draft.Errors; sessionMistakes = new(draft.Mistakes); typed = draft.Kind == "Test" ? draft.Typed : draft.Typed[..Math.Min(draft.Typed.Length, target.Length)];
            testInputTracker?.Update(typed);
            committedInputLength = draft.Kind == "Test" ? CommittedLength(typed) : 0;
            TypingBox!.Text = typed; TypingBox.Select(Math.Clamp(draft.Caret, 0, TypingBox.Text.Length), 0); TypingBox.ScrollToEnd();
            Paint(); UpdateMetrics(); typingFeedback?.Refresh(target);
        }
        finally { restoringDraft = false; }
        if ((testInputTracker?.Position ?? typed.Length) >= target.Length || ExerciseSeconds >= duration) FinishExercise();
        return true;
    }
}

