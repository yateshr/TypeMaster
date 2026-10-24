using System.Globalization;
using System.Text.RegularExpressions;
namespace TypeMaster.Native;
internal static partial class TechnicalSkills
{
    internal static readonly string[] Applications=["Excel","Word","PowerPoint","Outlook","Computer & Email","Other Skills"];
    internal static string DisplayName(string text)
    {
        string value=Regex.Replace(text.Trim(),@"\s+"," ");
        bool formula=catalog.RootElement.GetProperty("groups").EnumerateArray().Any(g=>g.GetProperty("functions").EnumerateArray().Any(n=>string.Equals(n.GetString(),value,StringComparison.OrdinalIgnoreCase)));
        if(formula||Groups.First(g=>g.Group==LegacyGroup).Items.Contains(value,StringComparer.OrdinalIgnoreCase))return value.ToUpperInvariant();
        value=CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
        foreach(string term in new[]{"VBA","PDF","GST","VAT","ERP","MIS","SQL","HTML","CSV","ID","IT","PC","CPM","WPM"})value=Regex.Replace(value,@"\b"+term+@"\b",term,RegexOptions.IgnoreCase);
        return Regex.Replace(Regex.Replace(value,@"\bPivottable\b","PivotTable"),@"\bPowerpoint\b","PowerPoint");
    }
    internal static string ApplicationFor(string skill,Profile profile)
    {
        foreach(var group in Groups)if(group.Items.Contains(skill,StringComparer.OrdinalIgnoreCase))
        {
            if(group.Group.StartsWith("Excel ")||group.Group==LegacyGroup)return "Excel";
            if(group.Group=="Microsoft Word")return "Word";
            if(group.Group=="Microsoft PowerPoint")return "PowerPoint";
            if(skill.Equals("Microsoft Outlook",StringComparison.OrdinalIgnoreCase))return "Outlook";
            return "Computer & Email";
        }
        var assigned=profile.TechnicalSkillApplications.FirstOrDefault(p=>p.Key.Equals(skill,StringComparison.OrdinalIgnoreCase)).Value;
        return assigned!=null&&Applications.Contains(assigned)?assigned:"Other Skills";
    }
    internal static IEnumerable<(string Group,string[] Items)> ReportGroups(Profile profile)
    {
        foreach(string app in Applications)
        {
            var items=profile.TechnicalSkills.Where(s=>ApplicationFor(s,profile)==app).OrderBy(DisplayName,StringComparer.OrdinalIgnoreCase).ToArray();
            if(items.Length>0)yield return(app,items);
        }
    }
}
