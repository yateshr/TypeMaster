using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;

namespace TypeMaster.Native;

public sealed record ReportHeading(string Title, DateTime Date, string Font = "Arial", string? TestTakenBy = null, string? CandidateName = null, DateTime? AssessmentDate = null, IReadOnlyDictionary<string,int>? SkillCounts = null);

public static class ReportFactory
{
    public static string Time(double seconds) => $"{(int)Math.Max(0, seconds) / 60}:{(int)Math.Max(0, seconds) % 60:00} min.";
    public static string Status(TypingResult r) => !r.Completed ? "PARTIAL" : r.Passed ? "PASSED" : "NOT PASSED";
    static string Planned(TypingResult r) => r.PlannedSeconds > 0 ? Time(r.PlannedSeconds) : "Not recorded";
    static string Date(TypingResult r) => (r.StartedAt == default ? r.Date : r.StartedAt).ToString("dd-MM-yyyy hh:mm tt");
    static string Gross(TypingResult r) => r.HasTextSnapshot ? r.GrossStrokes.ToString() : "Not recorded";
    static string Net(TypingResult r) => r.HasTextSnapshot ? r.NetStrokes.ToString() : "Not recorded";
    static string Hits(TypingResult r) => r.HasTextSnapshot ? $"{r.ErrorHits} ({r.Errors} errors{(r.Kind == "Test" ? " * 5" : "")})" : $"{r.Errors} errors";
    static FlowDocument Document(string title, double left = 40, double top = 120, string font = "Arial") => new()
    {
        Tag = new ReportHeading(title, DateTime.Now, font), FontFamily = new FontFamily(font), FontSize = 40d / 3,
        PageWidth = 794, PageHeight = 1123, PagePadding = new Thickness(left, top, 40, 50), ColumnWidth = 794 - left - 40,
        Foreground = Brushes.Black, Background = Brushes.White, IsHyphenationEnabled = false
    };
    static Paragraph P(string text, double size = 13, bool bold = false, double top = 0)
        => new(new Run(text)) { FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0, top, 0, 0), LineHeight = size * 1.28, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
    static void Section(FlowDocument d, string title, double top = 25)
    {
        var p = P(title, 44d / 3, true, top); p.BorderBrush = Brushes.Black; p.BorderThickness = new Thickness(0, 0, 0, .7); p.KeepWithNext = true; d.Blocks.Add(p);
    }
    static Table Table(double[] widths, IEnumerable<string[]> rows, double top = 0, bool headers = false, bool labels = true)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, top, 0, 0) };
        foreach (double width in widths) table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        var group = new TableRowGroup(); table.RowGroups.Add(group); int index = 0;
        foreach (var values in rows)
        {
            var row = new TableRow();
            for (int i = 0; i < values.Length; i++) row.Cells.Add(new TableCell(P(values[i], bold: (headers && index == 0) || (labels && i % 2 == 0))) { Padding = new Thickness(0, 0, 4, 0) });
            group.Rows.Add(row); index++;
        }
        return table;
    }
    static void Metadata(FlowDocument d, (string Label, string Value)[] rows, double top = 0, double labelWidth = 132)
        => d.Blocks.Add(Table([labelWidth, d.PageWidth - d.PagePadding.Left - d.PagePadding.Right - labelWidth], rows.Select(r => new[] { r.Label, r.Value }), top));
    public static FlowDocument Result(Profile profile, TypingResult result)
    {
        var d = Document(result.Completed ? "Typing Test completed" : "Typing Test partial", 68, 140, "Segoe UI");
        d.FontSize = 13;
        d.Blocks.Add(P("TYPING TEST - " + Status(result), bold: true));
        Metadata(d, [("User:", profile.Name), ("Test name:", result.Title), ("Date:", Date(result))]);
        d.Blocks.Add(P("TEST RESULTS", bold: true, top: 28));
        Metadata(d, [("Duration:", Time(result.Seconds) + " of total " + Planned(result))]);
        d.Blocks.Add(Table([132, 128, 132, 170], new[] {
            new[] { "Gross speed:", result.GrossWpm + " wpm", "Gross strokes:", Gross(result) },
            new[] { "Accuracy:", result.Accuracy + "%", "Error hits:", Hits(result) },
            new[] { "Net speed:", result.NetWpm + " wpm", "Net strokes:", Net(result) }
        }));
        d.Blocks.Add(P("TEST TEXT", bold: true, top: 60));
        if(result.HasTextSnapshot) AddCorrectedText(d,result);
        else d.Blocks.Add(P("The typed text was not captured for this older attempt.",top:24));
        return d;
    }
    internal sealed record ReportWord(string Typed,string? Expected)
    {
        public bool Wrong => Expected != null;
        public string Display => Expected == null ? Typed : Typed.Length == 0 ? "["+Expected+"]" : Typed+" ["+Expected+"]";
    }
    internal static List<ReportWord> CorrectedWords(string original,string typed)
    {
        var a=Regex.Matches(original,@"\S+").Select(m=>m.Value).ToArray();var b=Regex.Matches(typed,@"\S+").Select(m=>m.Value).ToArray();
        // Ignore the unattempted suffix of a timed test. Align substitutions and
        // insertions/deletions so one error does not mark every following word.
        if((long)a.Length*b.Length>2_000_000) a=a.Take(b.Length+32).ToArray();
        if((long)a.Length*b.Length>2_000_000)
        {
            var fallback=new List<ReportWord>();int x=0;
            for(int y=0;y<b.Length;y++)
            {
                if(x<a.Length&&a[x]==b[y]){fallback.Add(new(b[y],null));x++;continue;}
                int skip=Array.FindIndex(a,x,Math.Min(8,a.Length-x),w=>w==b[y]);
                if(skip>=x){while(x<skip)fallback.Add(new("",a[x++]));fallback.Add(new(b[y],null));x++;}
                else if(y+1<b.Length&&x<a.Length&&b[y+1]==a[x])fallback.Add(new(b[y],"extra word"));
                else fallback.Add(new(b[y],x<a.Length?a[x++]:"extra word"));
            }
            return fallback;
        }
        var cost=new int[a.Length+1,b.Length+1];for(int i=0;i<=a.Length;i++)cost[i,0]=i;for(int j=0;j<=b.Length;j++)cost[0,j]=j;
        for(int i=1;i<=a.Length;i++)for(int j=1;j<=b.Length;j++)cost[i,j]=Math.Min(cost[i-1,j-1]+(a[i-1]==b[j-1]?0:1),Math.Min(cost[i-1,j]+1,cost[i,j-1]+1));
        int end=0;for(int i=1;i<=a.Length;i++)if(cost[i,b.Length]<cost[end,b.Length]||(cost[i,b.Length]==cost[end,b.Length]&&Math.Abs(i-b.Length)<Math.Abs(end-b.Length)))end=i;
        var words=new List<ReportWord>();int row=end,column=b.Length;
        while(row>0||column>0)
        {
            if(row>0&&column>0&&cost[row,column]==cost[row-1,column-1]+(a[row-1]==b[column-1]?0:1))
            {words.Add(new(b[column-1],a[row-1]==b[column-1]?null:a[row-1]));row--;column--;}
            else if(column>0&&(row==0||cost[row,column]==cost[row,column-1]+1)){words.Add(new(b[column-1],"extra word"));column--;}
            else {words.Add(new("",a[row-1]));row--;}
        }
        words.Reverse();return words;
    }
    static void AddCorrectedText(FlowDocument document,TypingResult result)
    {
        bool numeric=Regex.IsMatch(result.TypedText,@"^[\d\s.,+\-*/=]+$");
        if(numeric)
        {
            var numbers=P(result.TypedText,13,top:24);numbers.Margin=new Thickness(32,24,0,0);numbers.LineHeight=18.5;document.Blocks.Add(numbers);return;
        }
        string expected=result.ExpectedText.Length>0?result.ExpectedText:result.OriginalText;
        if(expected.Length==0){document.Blocks.Add(P(result.TypedText,13,top:24));return;}
        var words=CorrectedWords(expected,result.TypedText);var lines=new List<List<ReportWord>>();var line=new List<ReportWord>();double width=0;const double maxWidth=554;
        foreach(var word in words)
        {
            double Measure(string text,bool bold)=>new FormattedText(text,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),13,Brushes.Black,1).WidthIncludingTrailingWhitespace;
            double next=Measure(word.Display+(line.Count>0?" ":""),word.Wrong);
            if(line.Count>0&&width+next>maxWidth){lines.Add(line);line=[];width=0;}
            line.Add(word);width+=next;
        }
        if(line.Count>0)lines.Add(line);
        var table=new Table {CellSpacing=0,Margin=new Thickness(0,24,0,0)};table.Columns.Add(new TableColumn{Width=new GridLength(32)});table.Columns.Add(new TableColumn{Width=new GridLength(maxWidth)});var rows=new TableRowGroup();table.RowGroups.Add(rows);
        foreach(var items in lines)
        {
            int errors=items.Count(w=>w.Wrong);var text=P("",13);text.LineHeight=18.5;
            for(int i=0;i<items.Count;i++)
            {
                if(i>0)text.Inlines.Add(new Run(" "));var word=items[i];var typedRun=new Run(word.Typed){FontWeight=word.Wrong?FontWeights.Bold:FontWeights.Normal};
                if(word.Wrong&&word.Typed.Length>0)typedRun.TextDecorations=TextDecorations.Underline;text.Inlines.Add(typedRun);
                if(word.Wrong)text.Inlines.Add(new Run((word.Typed.Length>0?" ":"")+"["+word.Expected+"]"){FontWeight=FontWeights.Bold});
            }
            var row=new TableRow();row.Cells.Add(new TableCell(P(errors>0?errors+">":"",13)){Padding=new Thickness(0)});row.Cells.Add(new TableCell(text){Padding=new Thickness(0)});rows.Rows.Add(row);
        }
        document.Blocks.Add(table);
    }
    public static FlowDocument Certificate(Profile profile, TypingResult result)
    {
        if (!result.Completed || result.Kind != "Test") throw new InvalidOperationException("A certificate requires a completed typing test.");
        var d = Document("TypeMaster Test Certificate, ", 148, 150);
        d.Blocks.Add(P(profile.Name.ToUpperInvariant(), 64d / 3, true));
        d.Blocks.Add(P("has today successfully completed a computer based\nTypeMaster typing test with the following results:", top: 40));
        Metadata(d, [("Test name:", result.Title)], 30);
        Metadata(d, [("Date:", Date(result)), ("Duration:", Planned(result)), ("Done:", Time(result.Seconds))], 23);
        Metadata(d, [("Gross speed:", result.GrossWpm + " wpm"), ("Accuracy:", result.Accuracy + "%"), ("Net speed:", result.NetWpm + " wpm")], 23);
        Metadata(d, [("Gross strokes:", Gross(result)), ("Error hits:", Hits(result)), ("Net strokes:", Net(result))], 23);
        d.Blocks.Add(P("Signature:", bold: true, top: 64)); d.Blocks.Add(P("________", top: 65));
        d.Blocks.Add(P("Generated by TypeMaster for this local typing assessment.\nThis certificate records completion and the measured results above.", 32d / 3, top: 75));
        return d;
    }
    public static FlowDocument Examination(Profile profile, TypingResult result)
    {
        var d = Document("TYPING TEST REPORT", 66, 150);
        d.Blocks.Add(P("Typing Examination Sheet", 64d / 3, true));
        Metadata(d, [("Candidate Name:", profile.Name), ("Name of Exam:", result.Title), ("Exam Start Date:", result.StartedAt == default ? "Not recorded" : Date(result)),
            ("Duration:", Time(result.Seconds) + " / " + Planned(result)), ("Completed:", (result.Completed ? "Yes " : "No ") + Time(result.Seconds))], 30);
        d.Blocks.Add(P("Original Test Paragraph", bold: true, top: 28));
        string expected = result.ExpectedText.Length > 0 ? result.ExpectedText : result.OriginalText;
        string original = result.HasTextSnapshot ? expected[..Math.Min(expected.Length, result.TypedText.Length)] : "Original text was not recorded for this older attempt.";
        d.Blocks.Add(P(original, 12, top: 20));
        d.Blocks.Add(P("Content Typed by Candidate", bold: true, top: 25));
        var candidate = P("", 12, top: 20);
        if (result.HasTextSnapshot)
        {
            var incorrect = IncorrectWords(original, result.TypedText); int word = 0;
            foreach (Match token in Regex.Matches(result.TypedText, @"\S+|\s+")) { bool whitespace = char.IsWhiteSpace(token.Value[0]); candidate.Inlines.Add(new Run(token.Value) { FontWeight = !whitespace && incorrect[word] ? FontWeights.Bold : FontWeights.Normal }); if (!whitespace) word++; }
        }
        else candidate.Inlines.Add(new Run("Typed content was not recorded for this older attempt."));
        d.Blocks.Add(candidate);
        d.Blocks.Add(Table([546, 142], new[] { new[] { "Signature of Candidate", "Examiner Signature" }, new[] { "________________", "________________" } }, 135, headers: true, labels: false));
        return d;
    }
    internal static bool[] IncorrectWords(string original, string typed)
    {
        var a = Regex.Matches(original, @"\S+").Select(m => m.Value).ToArray(); var b = Regex.Matches(typed, @"\S+").Select(m => m.Value).ToArray(); var bad = Enumerable.Repeat(true, b.Length).ToArray();
        if ((long)a.Length * b.Length > 2_000_000)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (a[i] == b[j]) { bad[j] = false; i++; j++; continue; }
                int nextSource = -1, nextTyped = -1;
                for (int k = 1; k <= 8; k++) { if (nextSource < 0 && i + k < a.Length && a[i + k] == b[j]) nextSource = k; if (nextTyped < 0 && j + k < b.Length && b[j + k] == a[i]) nextTyped = k; }
                if (nextSource >= 0 && (nextTyped < 0 || nextSource <= nextTyped)) i += nextSource;
                else if (nextTyped >= 0) j += nextTyped;
                else { i++; j++; }
            }
            return bad;
        }
        var lengths = new int[a.Length + 1, b.Length + 1];
        for (int i = a.Length - 1; i >= 0; i--) for (int j = b.Length - 1; j >= 0; j--) lengths[i, j] = a[i] == b[j] ? lengths[i + 1, j + 1] + 1 : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
        int x = 0, y = 0; while (x < a.Length && y < b.Length) { if (a[x] == b[y]) { bad[y] = false; x++; y++; } else if (lengths[x + 1, y] >= lengths[x, y + 1]) x++; else y++; } return bad;
    }
    public static FlowDocument Assessment(Profile profile) => Progress(profile);
    public static FlowDocument Progress(Profile profile)
    {
        var d = Document("Complete Report", font: "Segoe UI");
        var latestTest=profile.Results.Where(r=>r.Kind=="Test").OrderByDescending(r=>r.Date).FirstOrDefault();
        DateTime? assessmentDate=latestTest==null?null:latestTest.StartedAt==default?latestTest.Date:latestTest.StartedAt;
        d.Tag=((ReportHeading)d.Tag) with {TestTakenBy=profile.TestTakenBy,CandidateName=profile.Name,AssessmentDate=assessmentDate,SkillCounts=TechnicalSkills.ReportGroups(profile).ToDictionary(g=>g.Group,g=>g.Items.Count())};
        d.PagePadding=new Thickness(d.PagePadding.Left,d.PagePadding.Top,d.PagePadding.Right,152);
        var identity=new Grid();
        var card=new Border{Background=new SolidColorBrush(Color.FromRgb(237,243,251)),CornerRadius=new CornerRadius(6.666667),Padding=new Thickness(16,7,16,9),MinHeight=58.666667};
        var details=new Grid();details.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(505.333333)});details.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});details.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});details.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        void IdentityText(string value,int row,int column,double size,bool bold,Thickness margin,Brush brush)
        {
            var text=new TextBlock{Text=value,FontFamily=new FontFamily("Segoe UI"),FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,Foreground=brush,TextWrapping=TextWrapping.Wrap,Margin=margin};Grid.SetRow(text,row);Grid.SetColumn(text,column);details.Children.Add(text);
        }
        var muted=new SolidColorBrush(Color.FromRgb(82,106,135));var nameInk=new SolidColorBrush(Color.FromRgb(24,39,66));
        IdentityText("CANDIDATE NAME",0,0,9.333333,true,new Thickness(0,0,16,3),muted);
        IdentityText("ASSESSMENT DATE",0,1,9.333333,true,new Thickness(0,0,0,3),muted);
        IdentityText(profile.Name,1,0,22,true,new Thickness(0,0,24,0),nameInk);
        IdentityText(assessmentDate?.ToString("dd-MM-yyyy hh:mm tt",System.Globalization.CultureInfo.InvariantCulture)??"Not recorded",1,1,12,false,new Thickness(0,7,0,0),nameInk);
        card.Child=details;identity.Children.Add(card);identity.Children.Add(new Border{Width=4,HorizontalAlignment=HorizontalAlignment.Left,Background=new SolidColorBrush(Color.FromRgb(23,109,234)),CornerRadius=new CornerRadius(2)});
        d.Blocks.Add(new BlockUIContainer(identity){Margin=new Thickness(0,0,0,4)});
        string courseName = profile.Completed.Count > 0 ? "Course progress recorded" : "No course started"; CourseDefinition? course = null;
        string file = Path.Combine(Assets.Root, "lessons", Path.GetFileName(profile.CourseFile));
        if (profile.CourseFile.Length > 0 && File.Exists(file)) { course = CourseLibrary.Load(file); courseName = course.Name; }
        var panel=new Section{Margin=new Thickness(0,2,0,0),Padding=new Thickness(12,6,12,6),Background=new SolidColorBrush(Color.FromRgb(247,249,252))};
        var info=new Table{CellSpacing=0,Margin=new Thickness(0)};foreach(double width in new double[]{115,230,95,248})info.Columns.Add(new TableColumn{Width=new GridLength(width)});var infoRows=new TableRowGroup();info.RowGroups.Add(infoRows);
        var fields=new (string Label,string Value)[]{("TRAINING GROUP",profile.TrainingProfile),("STUDYING TIME",(profile.Results.Sum(r=>r.Seconds)/60).ToString("0")+" min"),("CURRENT COURSE",course==null&&profile.Completed.Count==0?"":courseName),("USAGE",profile.Sessions+" times"),("PROFILE CREATED",profile.Created==default?"":profile.Created.ToString("dd-MM-yyyy")),("LAST USED",profile.LastUsed==default?"":profile.LastUsed.ToString("dd-MM-yyyy hh:mm tt"))};
        var columns=new[]{fields.Where((field,index)=>index%2==0&&!string.IsNullOrWhiteSpace(field.Value)).ToArray(),fields.Where((field,index)=>index%2==1&&!string.IsNullOrWhiteSpace(field.Value)).ToArray()};
        for(int i=0;i<columns.Max(column=>column.Length);i++)
        {
            var row=new TableRow();
            for(int c=0;c<2;c++)
            {
                var labelCell=new TableCell{Padding=new Thickness(0,3,6,3)};
                var valueCell=new TableCell{Padding=new Thickness(0,3,10,3)};
                if(i<columns[c].Length)
                {
                    var field=columns[c][i];var caption=P(field.Label,9.5,true);caption.Foreground=new SolidColorBrush(Color.FromRgb(102,118,139));caption.KeepWithNext=true;
                    labelCell.Blocks.Add(caption);valueCell.Blocks.Add(P(field.Value,11.5));
                }
                row.Cells.Add(labelCell);row.Cells.Add(valueCell);
            }
            infoRows.Rows.Add(row);
        }
        panel.Blocks.Add(info);d.Blocks.Add(panel);
        var groups = profile.Results.Where(r => r.Kind != "Test").GroupBy(r => r.CourseFile).ToList();
        List<CourseDefinition> definitions = groups.Count > 0 ? CourseLibrary.LoadAll() : [];
        if (course != null && !groups.Any(g => g.Key == course.File)) { Section(d, courseName, 30); d.Blocks.Add(P("No completed exercises recorded.")); }
        foreach (var group in groups)
        {
            var definition = definitions.FirstOrDefault(c => c.File == group.Key); Section(d, definition?.Name ?? "Practice Exercises", 30);
            var data = new List<string[]> { new[] { "#", "Type", "Duration", "Time", "Gross Spd", "Accuracy", "Net Spd", "Date" } }; int n = 1;
            foreach (var r in group) data.Add(["(" + n++ + ")", r.Title, r.PlannedSeconds > 0 ? Time(r.PlannedSeconds) : "-", Time(r.Seconds), r.GrossWpm + " wpm", r.Accuracy + "%", r.NetWpm + " wpm", r.Date.ToString("dd-MM-yyyy hh:mm tt")]);
            var table = Table([36, 60, 78, 78, 100, 90, 100, 172], data, 8, true, false); table.FontSize = 12; d.Blocks.Add(table);
            int total = definition?.Chapters.Sum(c => c.Lessons.Count) ?? 0; int completed = definition?.Chapters.SelectMany(c => c.Lessons).Count(l => profile.Completed.Contains(l.Id)) ?? 0;
            d.Blocks.Add(P("Completed " + (total == 0 ? 0 : completed * 100 / total) + "%", bold: true, top: 6));
        }
        // Preserve the approved original typography and metric layout in the typing section.
        var typingSection=new Section{FontFamily=new FontFamily("Arial"),Tag="Typing Tests"};
        var typingHeading=P("Typing Tests",44d/3,true,18);typingHeading.BorderBrush=Brushes.Black;typingHeading.BorderThickness=new Thickness(0,0,0,.7);typingHeading.KeepWithNext=true;typingSection.Blocks.Add(typingHeading);
        foreach (var r in profile.Results.Where(r => r.Kind == "Test").Reverse())
        {
            var heading=P(r.Title+" - "+Status(r),13,true,12);heading.KeepWithNext=true;typingSection.Blocks.Add(heading);
            var summary=Table([95,159,100,95,110,132],new[] {
                new[] { "Date:",Date(r),"Gross speed:",r.GrossWpm+" wpm","Gross strokes:",Gross(r) },
                new[] { "Duration:",Planned(r),"Accuracy:",r.Accuracy+"%","Error hits:",Hits(r) },
                new[] { "Done:",Time(r.Seconds),"Net speed:",r.NetWpm+" wpm","Net strokes:",Net(r) }
            },4);summary.Margin=new Thickness(23,4,0,0);typingSection.Blocks.Add(summary);
        }
        if(!profile.Results.Any(r=>r.Kind=="Test"))typingSection.Blocks.Add(P("No typing tests recorded."));
        d.Blocks.Add(typingSection);
        if(profile.TechnicalSkills.Count>0)
        {
            var ink=new SolidColorBrush(Color.FromRgb(24,39,66));var line=new SolidColorBrush(Color.FromRgb(170,184,200));var pale=new SolidColorBrush(Color.FromRgb(240,245,251));
            var heading=P("Technical Skills",15,true,16);heading.FontFamily=new FontFamily("Segoe UI");heading.Foreground=Brushes.White;heading.Background=ink;heading.Padding=new Thickness(12,7,12,7);heading.KeepWithNext=true;d.Blocks.Add(heading);
            void SkillGroup(string title,IEnumerable<string> skills)
            {
                var items=skills.ToArray();if(items.Length==0)return;
                var label=P(title+"  ("+items.Length+")",12,true,4);label.Tag="Technical Skills / "+title;label.FontFamily=new FontFamily("Segoe UI");label.Foreground=ink;label.Background=pale;label.Padding=new Thickness(10,3,10,3);label.TextAlignment=TextAlignment.Left;label.KeepWithNext=true;d.Blocks.Add(label);
                var table=new Table{CellSpacing=0,Margin=new Thickness(0)};double column=(d.PageWidth-d.PagePadding.Left-d.PagePadding.Right)/3;for(int c=0;c<3;c++)table.Columns.Add(new TableColumn{Width=new GridLength(column)});var rows=new TableRowGroup();table.RowGroups.Add(rows);
                for(int i=0;i<items.Length;i+=3)
                {
                    var row=new TableRow();
                    for(int c=0;c<3;c++)
                    {
                        if(items.Length==1&&c>0)break;
                        if(i+c>=items.Length){row.Cells.Add(new TableCell(P("",12)){Padding=new Thickness(10,2,10,2),BorderBrush=line,BorderThickness=new Thickness(0,0,1,1)});continue;}
                        var text=P("",12);text.TextIndent=-14;text.Margin=new Thickness(14,0,0,0);text.KeepTogether=true;text.Tag="Technical Skills / "+title;text.FontFamily=new FontFamily("Segoe UI");text.TextAlignment=TextAlignment.Left;
                        if(i+c<items.Length){text.Inlines.Clear();text.Inlines.Add(new Run("✓  "){Foreground=new SolidColorBrush(Color.FromRgb(28,78,128))});text.Inlines.Add(new Run(TechnicalSkills.DisplayName(items[i+c])));}
                        row.Cells.Add(new TableCell(text){ColumnSpan=items.Length==1?3:1,Padding=new Thickness(10,2,10,2),BorderBrush=line,BorderThickness=new Thickness(c==0?1:0,0,1,1)});
                    }
                    rows.Rows.Add(row);
                }
                d.Blocks.Add(table);
            }
            foreach(var group in TechnicalSkills.ReportGroups(profile))SkillGroup(group.Group,group.Items);
        }
        if(!string.IsNullOrWhiteSpace(profile.AssessmentRemarks))
        {
            var remarks=new Section{Tag="Remarks",Margin=new Thickness(0,24,0,0),FontFamily=new FontFamily("Segoe UI")};
            var title=P("Remarks",14,true);title.Foreground=new SolidColorBrush(Color.FromRgb(24,39,66));title.KeepWithNext=true;title.Padding=new Thickness(0,0,0,5);title.BorderBrush=new SolidColorBrush(Color.FromRgb(170,184,200));title.BorderThickness=new Thickness(0,0,0,1);remarks.Blocks.Add(title);
            foreach(var comment in profile.AssessmentRemarks.Replace("\r\n","\n").Split('\n'))
            {
                var body=P(comment,12,top:5);body.Tag="Remarks";body.TextAlignment=TextAlignment.Left;body.LineHeight=18;body.KeepTogether=profile.AssessmentRemarks.Length<650;body.KeepWithNext=profile.AssessmentRemarks.Length<650;body.MinOrphanLines=2;body.MinWidowLines=2;remarks.Blocks.Add(body);
            }
            if(remarks.Blocks.LastBlock is Paragraph lastRemark)lastRemark.KeepWithNext=false;
            d.Blocks.Add(remarks);
        }
        void StyleBlocks(BlockCollection blocks)
        {
            foreach(var block in blocks)
            {
                if(block is Paragraph paragraph && paragraph.FontSize==13){paragraph.FontSize=12;paragraph.LineHeight=16;}
                if(block is Section section && !ReferenceEquals(section,typingSection))StyleBlocks(section.Blocks);
                if(block is Table table)foreach(var group in table.RowGroups)foreach(var row in group.Rows)foreach(var cell in row.Cells)StyleBlocks(cell.Blocks);
            }
        }
        StyleBlocks(d.Blocks);
        return d;
    }
    public static DocumentPaginator Paginator(FlowDocument document) => new ReportPaginator(document);
}

public sealed class ReportPaginator : DocumentPaginator
{
    readonly DocumentPaginator inner;
    readonly ReportHeading? heading;
    readonly FlowDocument document;
    public ReportPaginator(FlowDocument document) { this.document=document;heading=document.Tag as ReportHeading;inner = ((IDocumentPaginatorSource)document).DocumentPaginator; inner.PageSize = new Size(794, 1123); inner.ComputePageCount(); }
    (string Context,bool RepeatSkill) PageContext(DocumentPage original)
    {
        string context="Technical Skills";bool startsWithHeading=false;
        if(inner is DynamicDocumentPaginator dynamicPaginator && dynamicPaginator.GetPagePosition(original) is TextPointer position)
        {
            DependencyObject? node=position.Parent;
            while(node is not null){if(node is Paragraph paragraph&&paragraph.Background is not null)startsWithHeading=true;if(node is FrameworkContentElement element&&element.Tag is string tag){context=tag;break;}node=LogicalTreeHelper.GetParent(node);}
            if(context=="Technical Skills")
            {
                string latest=context;
                void Find(BlockCollection blocks){foreach(var block in blocks){if(block.ContentStart.CompareTo(position)>0)return;if(block.Tag is string tag)latest=tag;if(block is Section section)Find(section.Blocks);if(block is Table table)foreach(var group in table.RowGroups)foreach(var row in group.Rows)foreach(var cell in row.Cells)Find(cell.Blocks);}}
                Find(document.Blocks);context=latest;
            }
        }
        return(context,!startsWithHeading&&context.StartsWith("Technical Skills / ",StringComparison.Ordinal));
    }
    public override DocumentPage GetPage(int pageNumber)
    {
        var original=inner.GetPage(pageNumber);if(original==DocumentPage.Missing)return original;
        bool progress=heading?.CandidateName is not null;
        var context=progress&&pageNumber>0?PageContext(original):("",false);
        double shift=progress&&pageNumber>0?40+(context.Item2?21.333333:0):0;
        var visual=new DrawingVisual();
        using(var draw=visual.RenderOpen())
        {
            draw.DrawRectangle(Brushes.White,null,new Rect(new Point(),PageSize));
            draw.PushClip(new RectangleGeometry(new Rect(0,112,PageSize.Width,PageSize.Height-112)));
            if(shift>0)draw.PushTransform(new TranslateTransform(0,shift));
            draw.DrawRectangle(new VisualBrush(original.Visual){ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(new Point(),PageSize),Stretch=Stretch.Fill},null,new Rect(new Point(),PageSize));
            if(shift>0)draw.Pop();draw.Pop();
            if(progress)ReportBranding.DrawProgressHeader(draw,PageSize.Width,pageNumber>0);else ReportBranding.Draw(draw,PageSize.Width);
            var face=new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
            var ink=new SolidColorBrush(Color.FromRgb(24,39,66));var muted=new SolidColorBrush(Color.FromRgb(82,106,135));
            FormattedText Text(string value,double size,Brush brush,bool bold=false){var text=new FormattedText(value,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,face,size,brush,1);if(bold)text.SetFontWeight(FontWeights.SemiBold);return text;}
            if(progress&&pageNumber>0)
            {
                var candidate=Text(heading!.CandidateName!,12,ink,true);candidate.MaxTextWidth=400;candidate.MaxLineCount=2;
                double bandHeight=Math.Max(33.333333,candidate.Height+12);
                draw.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(237,243,251)),null,new Rect(40,108,714,bandHeight),5.333333,5.333333);
                draw.DrawText(candidate,new Point(52,116));
                var continuation=Text(context.Item1.Replace(" / "," | ")+" - continued",10.666667,muted);continuation.MaxTextWidth=300;continuation.MaxLineCount=2;continuation.TextAlignment=TextAlignment.Right;draw.DrawText(continuation,new Point(442,119));
                if(context.Item2)
                {
                    string application=context.Item1["Technical Skills / ".Length..];int count=heading.SkillCounts?.GetValueOrDefault(application)??0;
                    draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(240,245,251)),null,new Rect(40,160,714,21.333333));
                    draw.DrawText(Text(application+(count>0?" ("+count+")":"")+" - continued",12,ink,true),new Point(49.333333,163));
                }
            }
            if(progress)draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(198,211,233)),.7),new Point(40,1045),new Point(754,1045));
            if(heading?.TestTakenBy is string examiner)
            {
                var label=Text("Test taken by: "+(string.IsNullOrWhiteSpace(examiner)?"________________________":examiner),12,ink);label.MaxTextWidth=560;label.MaxLineCount=2;label.Trimming=TextTrimming.CharacterEllipsis;label.SetFontWeight(FontWeights.SemiBold,0,14);draw.DrawText(label,new Point(40,1054));
            }
            var pageLabel=Text("Page "+(pageNumber+1)+(progress?" of "+inner.PageCount:""),10,Brushes.Gray);draw.DrawText(pageLabel,new Point(754-pageLabel.Width,progress?1056:1084));
        }
        return new DocumentPage(visual,PageSize,new Rect(new Point(),PageSize),new Rect(new Point(),PageSize));
    }
    public override bool IsPageCountValid => inner.IsPageCountValid;
    public override int PageCount => inner.PageCount;
    public override Size PageSize { get => inner.PageSize; set => inner.PageSize = value; }
    public override IDocumentPaginatorSource Source => inner.Source;
}







