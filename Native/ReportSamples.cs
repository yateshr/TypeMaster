using System.IO;

namespace TypeMaster.Native;
internal static class ReportSamples
{
    public static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        var texts = Assets.Tests(); var test = texts.First(t => t.File.EndsWith("india.exm", StringComparison.OrdinalIgnoreCase)); string original = Assets.Read(test.File).Replace("\r\n", "\n").Trim(); original = original[..Math.Min(340, original.Length)];
        string typed = "India is a vast and diverse country that encompasses numerous cultures, languages, and traditions. This South Asian country i hom to ";
        var english = Scoring.MeasureTest(original, typed, 60, typed.Length, 2); english.Title = test.Name; english.Kind = "Test"; english.Completed = true; english.StartedAt = DateTime.Today.AddHours(9); english.Date = english.StartedAt.AddMinutes(1); english.PlannedSeconds = 60; english.OriginalText = original; english.TypedText = typed; english.HasTextSnapshot = true; english.RequiredAccuracy = test.MinAccuracy; english.RequiredWpm = test.MinSpeed;
        var numericText = texts.First(t => t.Name.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) && t.Name.Contains("numbers only", StringComparison.OrdinalIgnoreCase)); string numbers = System.Text.RegularExpressions.Regex.Replace(Assets.Read(numericText.File).Replace("\r\n", "\n"), @"\n\s*\n", "\n").Trim();
        string numberTyped = numbers[..152]; var numeric = Scoring.MeasureTest(numbers, numberTyped, 60, numberTyped.Length, 0); numeric.Title = numericText.Name; numeric.Kind = "Test"; numeric.Completed = true; numeric.StartedAt = DateTime.Today.AddHours(10); numeric.Date = numeric.StartedAt.AddMinutes(1); numeric.PlannedSeconds = 60; numeric.OriginalText = numbers; numeric.TypedText = numberTyped; numeric.HasTextSnapshot = true;
        var courseAttempt = new TypingResult { Title = "Keys", Kind = "Course", PlannedSeconds = 300, Date = DateTime.Today.AddDays(-2), CourseFile = "uk7qwert.cnt", HasTextSnapshot = true };
        var profile = new Profile { Name = "Sample Candidate", Created = DateTime.Today.AddDays(-2), LastUsed = DateTime.Today.AddHours(10), Sessions = 5, CourseFile = "uk7qwert.cnt", Results = [courseAttempt, english, numeric] };
        profile.TestTakenBy="Sample Interviewer"; profile.TechnicalSkills=["Addition (+)","Subtraction (-)","SUM","IF","VLOOKUP","PivotTable creation","Data entry","Document formatting","Create and format slides"]; profile.AssessmentRemarks="Candidate completed addition, subtraction and SUM independently. Demonstrated lookup formulas and created a PivotTable.\nInterviewer remarks can be written freely here.";
        NativePdf.Export(ReportFactory.Result(profile, english), Path.Combine(directory, "typing-test-results.pdf"));
        NativePdf.Export(ReportFactory.Result(profile, numeric), Path.Combine(directory, "numeric-test-results.pdf"));
        NativePdf.Export(ReportFactory.Certificate(profile, english), Path.Combine(directory, "test-certificate.pdf"));
        NativePdf.Export(ReportFactory.Examination(profile, english), Path.Combine(directory, "assessment-examination-sheet.pdf"));
        NativePdf.Export(ReportFactory.Progress(profile), Path.Combine(directory, "complete-progress-report.pdf"));
        var candidateSamples=Path.Combine(Path.GetDirectoryName(directory)!,"candidate-samples");Directory.CreateDirectory(candidateSamples);
        foreach(bool longSkills in new[]{false,true})
        {
            var candidate=new Profile{Name=longSkills?"Sample Candidate With A Longer Name":"Sample Candidate",Created=DateTime.Today.AddDays(-2),LastUsed=DateTime.Today.AddHours(10),Sessions=4,TestTakenBy="Sample Interviewer",TrainingProfile="Interview assessment",AssessmentRemarks=longSkills?"Candidate demonstrated the selected technical skills during the practical assessment. Formula knowledge and spreadsheet tasks were reviewed by the examiner. Additional training requirements can be recorded here.":"Candidate completed four typing attempts and demonstrated the selected Excel skills. Practical observations and interview feedback can be recorded here."};
            for(int attempt=0;attempt<4;attempt++)
            {
                var result=System.Text.Json.JsonSerializer.Deserialize<TypingResult>(System.Text.Json.JsonSerializer.Serialize(attempt%2==0?english:numeric))!;
                result.Title="Attempt "+(attempt+1)+" - "+(attempt%2==0?"English typing test":"Numpad numbers-only test");result.StartedAt=DateTime.Today.AddHours(9).AddMinutes(attempt*15);result.Date=result.StartedAt.AddMinutes(1);candidate.Results.Add(result);
            }
            candidate.TechnicalSkills=longSkills?TechnicalSkills.Groups.Where(g=>g.Group.StartsWith("Excel ")).SelectMany(g=>g.Items).Distinct().Take(110).Concat(new[]{"Document formatting","Create and format slides"}).ToHashSet():new HashSet<string>{"Addition (+)","Subtraction (-)","SUM","IF","VLOOKUP","Data entry"};
            NativePdf.Export(ReportFactory.Progress(candidate),Path.Combine(candidateSamples,longSkills?"sample-candidate-long-skills.pdf":"sample-candidate-short-skills.pdf"));
        }

    }
}






