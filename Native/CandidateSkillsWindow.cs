using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace TypeMaster.Native;

internal static partial class TechnicalSkills
{
    static readonly JsonDocument catalog=JsonDocument.Parse(File.ReadAllText(Path.Combine(Assets.Root,"excel-functions.json")));
    internal static int FunctionCount=>catalog.RootElement.GetProperty("groups").EnumerateArray().Sum(g=>g.GetProperty("functions").GetArrayLength());
    internal const string LegacyGroup="Previously selected combined formulas";
    internal static readonly (string Group,string[] Items)[] Groups=BuildGroups();
    static (string Group,string[] Items)[] BuildGroups()
    {
        var result=new List<(string Group,string[] Items)>
        {
            ("Excel basics",["Data entry","Cell formatting","Number and date formats","Copy / paste special","Rows and columns","Freeze panes","Sort and filter","Find and replace","Print area and page setup"]),
            ("Excel calculations",["Addition (+)","Subtraction (-)","Multiplication (*)","Division (/)","Percentage calculations","Absolute and relative references"])
        };
        string[] advanced=["FILTER","SORT","SORTBY","UNIQUE","SEQUENCE","RANDARRAY","CHOOSECOLS","CHOOSEROWS","TAKE","DROP","EXPAND","HSTACK","VSTACK","TOCOL","TOROW","WRAPCOLS","WRAPROWS","BYCOL","BYROW","LAMBDA","LET","MAP","REDUCE","SCAN","MAKEARRAY","GROUPBY","PIVOTBY","TRIMRANGE"];
        var advancedSet=advanced.ToHashSet();var all=new HashSet<string>();
        foreach(var group in catalog.RootElement.GetProperty("groups").EnumerateArray())
        {
            string category=group.GetProperty("name").GetString()!;var names=group.GetProperty("functions").EnumerateArray().Select(n=>n.GetString()!).ToArray();all.UnionWith(names);
            result.Add(("Excel "+category.ToLowerInvariant(),names.Where(n=>!advancedSet.Contains(n)).ToArray()));
        }
        result.Add(("Excel advanced / dynamic arrays",advanced.Where(all.Contains).Order().ToArray()));
        result.Add(("Excel data and analysis",["PivotTable creation","PivotTable grouping and filters","Pivot charts","Charts and graphs","Conditional formatting","Data validation / dropdown lists","Remove duplicates","Text to columns","Excel tables","Protect sheets / cells","Power Query","Macros / VBA"]));
        result.Add(("Microsoft Word",["Document formatting","Tables in Word","Page layout and margins","Headers, footers and page numbers","Mail merge","Export to PDF"]));
        result.Add(("Microsoft PowerPoint",["Create and format slides","Insert images and charts","Transitions and animations","Present a slide show"]));
        result.Add(("Computer and email",["File and folder management","Keyboard shortcuts","Email and attachments","Internet browsing","Printing and scanning","Microsoft Outlook"]));
        result.Add((LegacyGroup,["MIN / MAX","COUNT / COUNTA","AND / OR","SUMIF / SUMIFS","COUNTIF / COUNTIFS","AVERAGEIF / AVERAGEIFS","INDEX / MATCH","ROUND / ROUNDUP / ROUNDDOWN","LEFT / RIGHT / MID","CONCAT / TEXTJOIN","UPPER / LOWER / PROPER","TODAY / NOW","DATE / DAY / MONTH / YEAR"]));
        return result.Where(g=>g.Item2.Length>0).ToArray();
    }
    internal static string Availability(string name)=>catalog.RootElement.GetProperty("introduced").TryGetProperty(name,out var marker)?"Introduced: "+marker.GetString()+". Availability depends on the installed Excel version.":"Availability depends on the installed Excel version.";
}

internal sealed class CandidateSkillsWindow : Window
{
    readonly Profile profile;
    readonly Action save;
    readonly Dictionary<string,CheckBox> choices=new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,string> categories=new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> customSkills=new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,StackPanel> groups=[];
    readonly TextBox remarks,search,custom,examiner;
    readonly ComboBox category,customApplication;
    readonly Dictionary<string,string> applications;
    readonly StackPanel list;
    readonly TextBlock status;
    internal CandidateSkillsWindow(Profile profile,Action save,IEnumerable<string>? sharedCustomSkills=null,IReadOnlyDictionary<string,string>? sharedApplications=null)
    {
        this.profile=profile;this.save=save;
        applications=new Dictionary<string,string>(sharedApplications??new Dictionary<string,string>(),StringComparer.OrdinalIgnoreCase);foreach(var entry in profile.TechnicalSkillApplications)applications[entry.Key]=entry.Value;
        Title="Technical Skills & Remarks — "+profile.Name;Width=860;Height=760;MinWidth=320;MinHeight=300;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Brushes.White;FontFamily=new FontFamily("Segoe UI");FontSize=14;
        var root=new Grid{Margin=new Thickness(18)};
        foreach(var height in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto,GridLength.Auto,GridLength.Auto})root.RowDefinitions.Add(new RowDefinition{Height=height});
        root.Children.Add(new TextBlock{Text="Candidate: "+profile.Name+"\nTick demonstrated skills. "+TechnicalSkills.FunctionCount+" Excel functions are included; availability varies by Excel version.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var filters=new Grid{Margin=new Thickness(0,0,0,10)};filters.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(280)});filters.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});Grid.SetRow(filters,1);root.Children.Add(filters);
        category=new ComboBox{ItemsSource=new[]{"All categories"}.Concat(TechnicalSkills.Groups.Where(g=>g.Group!=TechnicalSkills.LegacyGroup||g.Items.Any(profile.TechnicalSkills.Contains)).Select(g=>g.Group)).Append("Custom skills").ToArray(),SelectedIndex=0,Height=30,Margin=new Thickness(0,0,10,0),VerticalContentAlignment=VerticalAlignment.Center};filters.Children.Add(category);
        search=new TextBox{Height=30,Padding=new Thickness(6),ToolTip="Search formulas or skills",VerticalContentAlignment=VerticalAlignment.Center};Grid.SetColumn(search,1);filters.Children.Add(search);
        System.Windows.Automation.AutomationProperties.SetName(search,"Search formulas or skills");
        var searchHint=new TextBlock{Text="Search formulas or skills…",Foreground=Brushes.Gray,Margin=new Thickness(8,0,0,0),VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};Grid.SetColumn(searchHint,1);filters.Children.Add(searchHint);search.TextChanged+=(_,_)=>searchHint.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
        list=new StackPanel{Margin=new Thickness(10)};var scroll=new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,BorderBrush=Brushes.LightGray,BorderThickness=new Thickness(1)};Grid.SetRow(scroll,2);root.Children.Add(scroll);
        foreach(var group in TechnicalSkills.Groups)foreach(string item in group.Items)if(group.Group!=TechnicalSkills.LegacyGroup||profile.TechnicalSkills.Contains(item))AddChoice(item,group.Group,profile.TechnicalSkills.Contains(item));
        foreach(string item in profile.CustomTechnicalSkills.Concat(sharedCustomSkills??[]).Concat(profile.TechnicalSkills.Where(s=>!choices.ContainsKey(s))))
        {if(!choices.ContainsKey(item)){customSkills.Add(item);AddChoice(item,"Custom skills",profile.TechnicalSkills.Contains(item));}}
        var customPanel=new Grid{Margin=new Thickness(0,10,0,0)};customPanel.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});customPanel.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(145)});customPanel.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});Grid.SetRow(customPanel,3);root.Children.Add(customPanel);
        custom=new TextBox{Padding=new Thickness(7),MaxLength=100,ToolTip="Enter a custom skill or formula name",VerticalContentAlignment=VerticalAlignment.Center};customPanel.Children.Add(custom);
        var customHint=new TextBlock{Text="Custom skill or formula name…",Foreground=Brushes.Gray,Margin=new Thickness(8,0,0,0),VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};customPanel.Children.Add(customHint);custom.TextChanged+=(_,_)=>customHint.Visibility=custom.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
        customApplication=new ComboBox{ItemsSource=TechnicalSkills.Applications,SelectedItem="Other Skills",Margin=new Thickness(8,0,0,0),VerticalContentAlignment=VerticalAlignment.Center,ToolTip="Application for this custom skill"};Grid.SetColumn(customApplication,1);customPanel.Children.Add(customApplication);
        var add=new Button{Content="Add custom skill / formula",Margin=new Thickness(10,0,0,0)};add.Click+=(_,_)=>{try{AddCustomSkill(custom.Text,customApplication.SelectedItem as string);}catch(ArgumentException ex){MessageBox.Show(this,ex.Message,"Custom skill");}};Grid.SetColumn(add,2);customPanel.Children.Add(add);
        var comments=new StackPanel{Margin=new Thickness(0,12,0,0)};Grid.SetRow(comments,4);root.Children.Add(comments);comments.Children.Add(new TextBlock{Text="Remarks",FontWeight=FontWeights.SemiBold});
        remarks=new TextBox{Text=profile.AssessmentRemarks,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Height=100,Margin=new Thickness(0,6,0,0),Padding=new Thickness(8)};comments.Children.Add(remarks);
        var examinerRow=new DockPanel{Margin=new Thickness(0,8,0,0)};examinerRow.Children.Add(new TextBlock{Text="Test taken by",Width=115,VerticalAlignment=VerticalAlignment.Center});examiner=new TextBox{Text=profile.TestTakenBy,MaxLength=100,Padding=new Thickness(6),Height=30,VerticalContentAlignment=VerticalAlignment.Center};examinerRow.Children.Add(examiner);comments.Children.Add(examinerRow);
        var footer=new DockPanel{Margin=new Thickness(0,10,0,0)};Grid.SetRow(footer,5);root.Children.Add(footer);var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);
        actions.Children.Add(new Button{Content="Cancel",IsCancel=true});var apply=new Button{Content="Save & Update Report"};apply.Click+=(_,_)=>{try{Apply();DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Assessment could not be saved");}};actions.Children.Add(apply);
        status=new TextBlock{VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.DimGray};footer.Children.Add(status);Content=root;
        search.TextChanged+=(_,_)=>Filter();category.SelectionChanged+=(_,_)=>Filter();Filter();
    }
    void AddChoice(string item,string group,bool selected)
    {
        if(choices.ContainsKey(item))return;
        if(!groups.TryGetValue(group,out var panel)){panel=new StackPanel{Margin=new Thickness(0,0,0,14)};panel.Children.Add(new TextBlock{Text=group,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,6,0,8)});groups[group]=panel;list.Children.Add(panel);}
        var check=new CheckBox{Content=TechnicalSkills.DisplayName(item),IsChecked=selected,Margin=new Thickness(0,5,0,5),VerticalContentAlignment=VerticalAlignment.Center,ToolTip=TechnicalSkills.Availability(item)};choices[item]=check;categories[item]=group;check.Checked+=(_,_)=>UpdateStatus();check.Unchecked+=(_,_)=>UpdateStatus();panel.Children.Add(check);
    }
    internal string AddCustomSkill(string text,string? application=null)
    {
        string name=Regex.Replace(text.Trim(),@"\s+"," ");if(name.Length==0||name.Length>100||name.Any(char.IsControl))throw new ArgumentException("Enter a skill or formula name of 1 to 100 characters.");
        if(choices.TryGetValue(name,out var existing)){existing.IsChecked=true;name=choices.Keys.First(k=>k.Equals(name,StringComparison.OrdinalIgnoreCase));}
        else{customSkills.Add(name);AddChoice(name,"Custom skills",true);}
        if(customSkills.Contains(name)&&application!=null&&TechnicalSkills.Applications.Contains(application))applications[name]=application;
        custom.Clear();category.SelectedItem="All categories";search.Text=name;Filter();return name;
    }
    void Filter()
    {
        string text=search.Text.Trim(),selected=category.SelectedItem as string??"All categories";
        foreach(var choice in choices)choice.Value.Visibility=(selected=="All categories"||categories[choice.Key]==selected)&&(text.Length==0||choice.Key.Contains(text,StringComparison.OrdinalIgnoreCase)||categories[choice.Key].Contains(text,StringComparison.OrdinalIgnoreCase))?Visibility.Visible:Visibility.Collapsed;
        foreach(var group in groups)group.Value.Visibility=group.Value.Children.OfType<CheckBox>().Any(c=>c.Visibility==Visibility.Visible)?Visibility.Visible:Visibility.Collapsed;
        UpdateStatus();
    }
    void UpdateStatus(){if(status!=null)status.Text=choices.Count(c=>c.Value.IsChecked==true)+" selected · "+choices.Count(c=>c.Value.Visibility==Visibility.Visible)+" matching";}
    internal void SetSkill(string skill,bool selected)=>choices[skill].IsChecked=selected;
    internal void SetFilter(string text,string group="All categories"){category.SelectedItem=group;search.Text=text;Filter();}
    internal string[] MatchingSkills=>choices.Where(c=>c.Value.Visibility==Visibility.Visible).Select(c=>c.Key).ToArray();
    internal void SetRemarks(string text)=>remarks.Text=text;
    internal void SetExaminer(string text)=>examiner.Text=text;
    internal void Apply()
    {
        if(!string.IsNullOrWhiteSpace(custom.Text))AddCustomSkill(custom.Text,customApplication.SelectedItem as string);
        var selected=new HashSet<string>(choices.Where(c=>c.Value.IsChecked==true).Select(c=>c.Key));
        var previous=profile.TechnicalSkills;var previousCustom=profile.CustomTechnicalSkills;var previousApplications=profile.TechnicalSkillApplications;string previousRemarks=profile.AssessmentRemarks,previousExaminer=profile.TestTakenBy;
        profile.TechnicalSkills=selected;profile.CustomTechnicalSkills=new HashSet<string>(customSkills);profile.TechnicalSkillApplications=new Dictionary<string,string>(applications,StringComparer.OrdinalIgnoreCase);profile.AssessmentRemarks=remarks.Text;profile.TestTakenBy=Regex.Replace(examiner.Text.Trim(),@"\s+"," ");
        try{save();}catch{profile.TechnicalSkills=previous;profile.CustomTechnicalSkills=previousCustom;profile.TechnicalSkillApplications=previousApplications;profile.AssessmentRemarks=previousRemarks;profile.TestTakenBy=previousExaminer;throw;}
    }
}



