using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TypeMaster.Native;
public partial class MainWindow
{
    void Login()
    {
        timer.Stop(); gameView?.Dispose(); exercising = false; StopMeter(false); Current = null; Theme(); Navigation.Visibility = Visibility.Collapsed; NavColumn.Width = new GridLength(0);
        var canvas = ReferencePage(null, true);
        Position(canvas, Picture("Branding/typemaster-wordmark.png", 370, 75), 201, 12, 370, 75);
        ReferenceClose(canvas, 746, 13, Close);
        var panel = new Border(); panel.SetResourceReference(Border.BackgroundProperty, "SoftBrush"); Position(canvas, panel, 17, 90, 744, 429);
        Position(canvas, RefText("Welcome to TypeMaster", 22), 230, 102, 340, 36);
        Position(canvas, RefText("Enter your name", 16), 260, 142, 245, 24);
        var name = Position(canvas, new TextBox { MaxLength = 50, FontFamily = new FontFamily("Tahoma"), FontSize = 17, Padding = new Thickness(1), BorderBrush = Brushes.DodgerBlue, BorderThickness = new Thickness(1) }, 260, 165, 246, 26);
        var list = Position(canvas, new ListBox { ItemsSource = Store.Profiles, Padding = new Thickness(0), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), FontSize = 14 }, 260, 191, 246, 112);
        list.SelectionChanged += (_, _) => { if (list.SelectedItem is Profile profile) name.Text = profile.Name; };
        void Enter()
        {
            string value = name.Text.Trim(); if (value.Length == 0) { Notify("Please enter your name."); return; }
            if (Store.Profiles.Any(p => p.Name.Equals(value, StringComparison.OrdinalIgnoreCase))) EnterProfile(value);
            else if (MessageBox.Show(this, "User " + value + " not found, are you a new user?", "TypeMaster", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) NewUser(value);
        }
        name.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) Enter(); }; list.MouseDoubleClick += (_, _) => Enter();
        var newUser = RefButton("I am a new user..", () => NewUser(name.Text.Trim()), true); var newUserContent = new StackPanel { Orientation = Orientation.Horizontal };
        var newUserIcon = new Grid { Width = 14, Height = 14 }; newUserIcon.Children.Add(new System.Windows.Shapes.Ellipse { Fill = Brushes.DodgerBlue }); newUserIcon.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 6 3 L 10 7 L 6 11 Z"), Fill = Brushes.White }); newUserContent.Children.Add(newUserIcon);
        newUserContent.Children.Add(new TextBlock { Text = " I am a new user..", TextDecorations = TextDecorations.Underline }); newUser.Content = newUserContent; newUser.Padding = new Thickness(6, 0, 4, 0); Position(canvas, newUser, 260, 306, 246, 34);
        Position(canvas, RefButton("Delete User", () => { if (list.SelectedItem is Profile profile && MessageBox.Show(this, "Delete " + profile.Name + " and their progress?", "Delete User", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { Store.Profiles.Remove(profile); Save(); Login(); } }, true), 238, 461, 137, 34);
        Position(canvas, RefButton("Enter", Enter), 414, 461, 137, 34);
        WelcomeInformation(canvas); name.Focus();
    }
    void WelcomeInformation(Canvas canvas)
    {

        Position(canvas, RefText("Getting Started", 14, true), 568, 107, 177, 24);
        Position(canvas, RefText("Choose your name to continue, or create a new profile. Practice typing, complete tests and review your progress. Your results are saved on this computer.", 13), 568, 130, 177, 145);
    }
    internal void NewUser(string initial)
    {
        var canvas = ReferencePage(null, true); Position(canvas, Picture("Branding/typemaster-wordmark.png", 370, 75), 201, 12, 370, 75);
        ReferenceClose(canvas, 746, 13, Login);
        var panel = new Border(); panel.SetResourceReference(Border.BackgroundProperty, "SoftBrush"); Position(canvas, panel, 17, 90, 744, 429);
        Position(canvas, RefTitle("Welcome New User!"), 230, 102, 350, 35).FontSize = 22;
        Position(canvas, RefText("Welcome to TypeMaster touch typing tutor!\nPlease give a user name below and click Enter.", 13), 230, 133, 350, 38);
        Position(canvas, Picture("TypeMasterArtwork/profile.png",22,21), 229, 222, 22, 21);
        Position(canvas, RefText("Enter your name", 14, true, true), 260, 223, 260, 22);
        var name = Position(canvas, new TextBox { Text = initial, MaxLength = 50, Padding = new Thickness(1), BorderBrush = Brushes.LightGray, FontFamily = new FontFamily("Tahoma"), FontSize = 13 }, 260, 242, 253, 24);
        var dark = Position(canvas, new CheckBox { Content = "Use Dark Mode", FontFamily = new FontFamily("Georgia"), FontWeight = FontWeights.Bold, FontSize = 13 }, 260, 279, 253, 24);
        dark.Style = (Style)Application.Current.FindResource("ReferenceCheck");
        Position(canvas, Picture("TypeMasterArtwork/training-profile.png",22,21), 229, 391, 22, 21);
        Position(canvas, RefText("Typing Configuration Profile", 14, true, true), 260, 392, 300, 22);
        string training = "Default Training Profile"; Position(canvas, RefChoice(["Default Training Profile", "Accuracy First", "Speed Builder"], training, v => training = v), 260, 414, 253, 24);
        var cancel = RefButton("Cancel", Login, true); cancel.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, cancel, 230, 480, 129, 34);
        var enter = RefButton("Enter", () => { string value = name.Text.Trim(); if (value.Length == 0) { Notify("Please enter your name."); return; } if (Store.Profiles.Any(p => p.Name.Equals(value, StringComparison.OrdinalIgnoreCase))) { Notify("This user already exists. Select the name on the welcome screen."); return; } Store.Profiles.Add(new Profile { Name = value, Dark = dark.IsChecked == true, TrainingProfile = training }); Save(); EnterProfile(value); }, true); enter.HorizontalContentAlignment = HorizontalAlignment.Center; Position(canvas, enter, 429, 480, 122, 34);
        WelcomeInformation(canvas);
    }
    string expandedCourse = "basic";
    void Courses()
    {
        var canvas = ReferencePage("Choose Course"); Position(canvas, LanguageChoice(Courses), 389, 13, 185, 29);
        var available = allCourses.Where(c => c.Language == Current!.Language).ToList(); var entries = Localization.Entries(Current!.Language);
        CourseDefinition? CourseAt(string key) => entries.TryGetValue(key, out string? file) ? available.FirstOrDefault(c => c.File.Equals(file, StringComparison.OrdinalIgnoreCase)) : null;
        var basic = CourseAt("5") ?? available.FirstOrDefault(c => c.File.Equals("uk7qwert.cnt", StringComparison.OrdinalIgnoreCase)) ?? available.FirstOrDefault();
        var secondary = CourseAt("7"); if (secondary == basic) secondary = null;
        var speed = available.FirstOrDefault(c => c.File.Contains("Speed", StringComparison.OrdinalIgnoreCase));
        var extras = available.Where(c => c != basic && c != secondary && (secondary != null || c != speed)).ToList(); double top = 65;
        void Header(string id, string title, List<CourseDefinition> choices)
        {
            bool expanded = expandedCourse == id; double y = top;
            var toggle = RefButton("", () => { expandedCourse = expanded ? "" : id; Courses(); }); toggle.Tag = new CornerRadius(8,8,0,0); toggle.Background = new SolidColorBrush(Color.FromRgb(20,111,226)); toggle.Padding = new Thickness(0); toggle.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            var header = new Canvas { Width = 550, Height = 35 }; var label = RefText(title, 18, true); label.FontFamily = new FontFamily("Arial"); label.Foreground = Brushes.White; if (!expanded) label.TextDecorations = TextDecorations.Underline; Position(header, label, 8, 8, 519, 27); Position(header, RefText(expanded ? "⌃" : "⌄", 19), 524, 4, 22, 25).Foreground = Brushes.White; toggle.Content = header; Position(canvas, toggle, 22,y,550,35);
            if (!expanded) { top += 76; return; }
            Brush background = Current!.Dark ? new LinearGradientBrush(Color.FromRgb(49,77,138),Color.FromRgb(27,41,61),90) : new LinearGradientBrush(Color.FromRgb(231,229,255),Color.FromRgb(202,209,251),90);
            var panel = new Border { Background = background }; RenderOptions.SetBitmapScalingMode(panel,BitmapScalingMode.NearestNeighbor); Position(canvas,panel,22,y+35,550,164);
            CourseDefinition selected = choices[0]; bool choice = choices.Count > 1;
            var description=Position(canvas,RefText("",16),34,y+(choice?75:49),520,choice?43:65);description.FontFamily=new FontFamily("Arial");description.LineHeight=21;
            var values=new List<TextBlock>(); int n=0; foreach(string name in new[]{"Duration","Lessons","Progress"}) { Position(canvas,RefText(name,14,true),34,y+118+n*24,88,24);values.Add(Position(canvas,RefText("",14),124,y+118+n++*24,245,24)); }
            void RefreshDetails() { description.Text=selected.Description; int done=selected.Chapters.SelectMany(c=>c.Groups).Count(g=>g.IsComplete(Current));values[0].Text=selected.File=="uk7qwert.cnt"?"3:10 - 5:20 hours":selected.DurationRange; values[1].Text=selected.Chapters.Count+" "+Localization.Translate("lessons");values[2].Text=done==0?Localization.Translate("Not started"):done+" / "+selected.Chapters.Sum(c=>c.Groups.Count)+" "+Localization.Translate("activities completed"); }
            if(choice) { var chooser=new ComboBox { ItemsSource=choices,SelectedIndex=0,FontSize=13 };chooser.SelectionChanged+=(_,_)=>{if(chooser.SelectedItem is CourseDefinition selectedCourse){selected=selectedCourse;RefreshDetails();}}; Position(canvas,chooser,34,y+42,507,25); }
            RefreshDetails();var start=RefButton("Start Now",()=>OpenCourse(selected));start.FontFamily=new FontFamily("Arial");start.FontWeight=FontWeights.Bold;start.FontSize=16.5;Position(canvas,start,398,y+163,154,34);top+=235;
        }
        if(basic!=null)Header("basic",basic.Name,[basic]);
        if(secondary!=null)Header("secondary",secondary.Name,[secondary]);
        if(secondary==null&&speed!=null&&speed!=basic)Header("speed",speed.Name,[speed]);
        if(extras.Count>0)Header("extra",secondary!=null&&secondary!=speed?"Advanced Typing Courses":"Extra Courses: Keyboarding, Numbers, Specials, 10-Key",extras);
    }
    void Settings()
    {
        var canvas = ReferencePage("Training Settings"); Rule(canvas, 18, 63, 549); Rule(canvas, 18, 223, 549);
        Position(canvas, RefText("Sounds", 15, true, true), 24, 73, 150, 22);
        Position(canvas, RefChoice(["Sounds on", "Sounds off"], Current!.Sound ? "Sounds on" : "Sounds off", v => { Current.Sound = v == "Sounds on"; Save(); }), 24, 93, 137, 24);
        Position(canvas, RefText("Speed Unit", 15, true, true), 200, 73, 160, 22);
        Position(canvas, RefChoice(["WPM (words/min)", "CPM (chars/min)"], Current.SpeedUnit == "WPM" ? "WPM (words/min)" : "CPM (chars/min)", v => { Current.SpeedUnit = v.StartsWith("WPM") ? "WPM" : "CPM"; Save(); }), 200, 93, 169, 24);
        Position(canvas, RefText("Screen Size", 15, true, true), 408, 73, 161, 22);
        Position(canvas, RefChoice(["Maximized window", "Normal window"], Current.ScreenMode == "Maximized" ? "Maximized window" : "Normal window", v => { Current.ScreenMode = v.StartsWith("Maximized") ? "Maximized" : "Windowed"; Save(); }), 408, 93, 161, 24);
        var help = RefText("This setting will take\neffect the next time you\nstart this software.", 12); help.Foreground = Brushes.Gray; Position(canvas, help, 408, 130, 155, 58);
        Position(canvas, RefText("Key Hint Speed", 15, true, true), 24, 154, 160, 22);
        Position(canvas, RefChoice(["Fast", "Normal", "Slow"], Current.HintSpeed, v => { Current.HintSpeed = v; Save(); }), 24, 179, 137, 24);
        Position(canvas, RefText("Language", 15, true, true), 200, 154, 169, 22); Position(canvas, LanguageChoice(() => ShowPage(page)), 200, 179, 169, 24);
        var dark = new CheckBox { Content = "Dark Mode", IsChecked = Current.Dark, FontFamily = new FontFamily("Georgia"), FontSize = 15, FontWeight = FontWeights.Bold }; dark.SetResourceReference(ForegroundProperty, "TextBrush"); dark.Click += (_, _) => { Current.Dark = dark.IsChecked == true; Save(); Theme(); }; Position(canvas, dark, 408, 182, 160, 24);
        dark.Style = (Style)Application.Current.FindResource("ReferenceCheck");
        Position(canvas, RefText("Keyboard Layout", 14, true, true), 20, 239, 170, 25);
        var standard = Position(canvas, new RadioButton { Content = Localization.Translate("Default (English US)"), FontFamily = new FontFamily("Verdana"), FontSize = 13, Style = (Style)Application.Current.FindResource("ReferenceRadio"), IsChecked = Current.Keyboard == "us.kbd", GroupName = "Keyboard" }, 190, 239, 240, 22); standard.Checked += (_, _) => { Current.Keyboard = "us.kbd"; Save(); Settings(); };
        var other = Position(canvas, new RadioButton { GroupName = "Keyboard", Style = (Style)Application.Current.FindResource("ReferenceRadio"), IsChecked = Current.Keyboard != "us.kbd" }, 190, 269, 20, 20);
        string customDir = Path.Combine(Store.DirectoryPath, "Keyboards"); Directory.CreateDirectory(customDir);
        var files = Directory.GetFiles(Path.Combine(Assets.Root, "keyboards"), "*.kbd").Concat(Directory.GetFiles(customDir, "*.kbd")).Select(Path.GetFileName).OfType<string>().OrderBy(v => v).ToList();
        var values = files.ToDictionary(f => f, f => KeyboardLayout.DisplayName(f));
        other.Checked += (_, _) => { if (Current.Keyboard == "us.kbd") { Current.Keyboard = files.FirstOrDefault(f => f != "us.kbd") ?? "us.kbd"; Save(); Settings(); } };
        var layouts = new ComboBox { ItemsSource = values, DisplayMemberPath = "Value", SelectedValuePath = "Key", SelectedValue = Current.Keyboard, Style = (Style)Application.Current.FindResource("ReferenceCombo") }; layouts.SelectionChanged += (_, _) => { if (layouts.SelectedValue is string file) { Current.Keyboard = file; Save(); Settings(); } }; layouts.ToolTip = "Choose the same keyboard layout in Windows using Windows + Space."; Position(canvas, layouts, 208, 263, 224, 24);
        var custom = RefButton("Add Custom Layout", ImportKeyboard, true); custom.FontSize = 11; custom.Foreground = Brushes.Black; custom.Tag = new CornerRadius(3); custom.BorderBrush = Brushes.LightGray; Position(canvas, custom, 442, 254, 136, 32);
        Position(canvas, new KeyboardView(LoadKeyboard()) { MaxHeight = 175, Stretch = Stretch.Fill }, 45, 316, 500, 175);
        Position(canvas,RefButton("Company Branding",CompanyBrandingSettings,true),24,498,205,27);
        var menu = new ContextMenu(); var tools = new MenuItem { Header = "More settings, profile backup and restore" }; tools.Click += (_, _) => MoreSettings(); menu.Items.Add(tools); canvas.ContextMenu = menu;
    }
    void ImportKeyboard()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Keyboard layout|*.kbd" }; if (dialog.ShowDialog(this) != true) return;
        try { KeyboardLayout.Load(dialog.FileName); string directory = Path.Combine(Store.DirectoryPath, "Keyboards"); Directory.CreateDirectory(directory); string name = "custom-" + Guid.NewGuid().ToString("N")[..8] + ".kbd"; File.Copy(dialog.FileName, Path.Combine(directory, name)); Current!.Keyboard = name; Save(); Settings(); } catch (Exception ex) { Notify(ex.Message, "Layout could not be imported"); }
    }
}






