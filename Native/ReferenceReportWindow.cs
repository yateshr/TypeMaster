using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace TypeMaster.Native;
public sealed class ReferenceReportWindow : Window
{
    internal string ReportTitle { get; }
    DocumentPaginator paginator;
    readonly DocumentPageView page;
    readonly TextBlock count;
    readonly Button previous, next;
    readonly ComboBox zoom;
    int index;
    public ReferenceReportWindow(FlowDocument document, Profile? candidate = null, Action? save = null, Func<IEnumerable<string>>? customSkills = null, Func<IReadOnlyDictionary<string,string>>? customApplications=null)
    {
        ReportTitle = (document.Tag as ReportHeading)?.Title ?? "TypeMaster Report";
        Title = "View Report"; Width = 1440; Height = 852; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize; Background = Brushes.LightGray; Foreground = Brushes.Black; FontFamily = new FontFamily("Tahoma"); FontSize = 11; UseLayoutRounding = true;
        WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 23, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), UseAeroCaptionButtons = false });
        paginator = ReportFactory.Paginator(document); paginator.ComputePageCount();
        var root = new Grid(); foreach (var height in new[] { new GridLength(23), GridLength.Auto, new GridLength(1, GridUnitType.Star), new GridLength(22) }) root.RowDefinitions.Add(new RowDefinition { Height = height });
        var caption = new Grid { Background = new SolidColorBrush(Color.FromRgb(247, 242, 237)) }; root.Children.Add(caption);
        var title = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; title.Children.Add(new Image { Source = MainWindow.AssetBitmap("Branding/typemaster-icon.png"), Width = 16, Height = 16 }); title.Children.Add(new TextBlock { Text = "View Report", FontFamily = new FontFamily("Segoe UI"), FontSize = 12, Margin = new Thickness(6, 0, 0, 0) }); caption.Children.Add(title);
        var captionButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; WindowChrome.SetIsHitTestVisibleInChrome(captionButtons, true); caption.Children.Add(captionButtons);
        WindowFrame.Configure(this,root,captionButtons);
        var tools = new WrapPanel { Orientation = Orientation.Horizontal, Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)) }; Grid.SetRow(tools, 1); root.Children.Add(tools);
        previous = Tool("▣  Prev. Page", () => Change(-1), 112); next = Tool("▣  Next Page", () => Change(1), 109); tools.Children.Add(previous); tools.Children.Add(next); tools.Children.Add(new Border { Width = 1, Background = Brushes.Gray, Margin = new Thickness(0, 3, 8, 3) });
        tools.Children.Add(Tool("▣  Save PDF", SavePdf, 100)); tools.Children.Add(Tool("▧  Print", Print, 107)); tools.Children.Add(new Border { Width = 1, Background = Brushes.Gray, Margin = new Thickness(0, 3, 8, 3) }); tools.Children.Add(Tool("⊠  Close", Close, 111));
        zoom = new ComboBox { ItemsSource = new[] { "Page width", "100%", "75%", "50%" }, SelectedIndex = 0, Width = 136, Height = 22, Style = (Style)Application.Current.FindResource("ReferenceCombo"), FontSize = 11, Margin = new Thickness(0, 4, 0, 0), VerticalAlignment = VerticalAlignment.Top }; tools.Children.Add(zoom);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Background = new SolidColorBrush(Color.FromRgb(239, 239, 239)), BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray }; Grid.SetRow(scroll, 2); root.Children.Add(scroll);
        page = new DocumentPageView { DocumentPaginator = paginator, PageNumber = 0, Width = 794, Height = 1123, Stretch = Stretch.Fill };
        if(candidate != null && save != null) tools.Children.Add(Tool("Skills & Remarks", () => { var editor=new CandidateSkillsWindow(candidate,save,customSkills?.Invoke(),customApplications?.Invoke()){Owner=this}; if(editor.ShowDialog()==true){paginator=ReportFactory.Paginator(ReportFactory.Progress(candidate));paginator.ComputePageCount();page.DocumentPaginator=paginator;index=0;page.PageNumber=0;UpdatePager();Scale();}},140));
        var paper = new Border { Background = Brushes.White, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Child = page, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(40, 40, 20, 40) }; scroll.Content = paper;
        var footer = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)) }; Grid.SetRow(footer, 3); root.Children.Add(footer); count = new TextBlock { Margin = new Thickness(6, 3, 0, 0) }; footer.Children.Add(count);
        Content = root; zoom.SelectionChanged += (_, _) => Scale(); SizeChanged += (_, _) => Scale(); Loaded += (_, _) => { Scale(); UpdatePager(); };
    }
    static Button Tool(string text, Action action, double width)
    {
        var b = new Button { Content = text, Width = width, Height = 31, MinHeight = 0, Padding = new Thickness(3, 0, 3, 0), Margin = new Thickness(0), Background = Brushes.Transparent, Foreground = Brushes.Black, Tag = new CornerRadius(0), FontFamily = new FontFamily("Tahoma"), FontSize = 11, HorizontalContentAlignment = HorizontalAlignment.Left }; b.Click += (_, _) => action(); return b;
    }
    void Scale() { double factor = zoom.SelectedIndex == 0 ? Math.Max(.5, (ActualWidth - 80) / 794) : zoom.SelectedIndex == 1 ? 1 : zoom.SelectedIndex == 2 ? .75 : .5; page.LayoutTransform = new ScaleTransform(factor, factor); }
    void Change(int amount) { index = Math.Clamp(index + amount, 0, Math.Max(0, paginator.PageCount - 1)); page.PageNumber = index; UpdatePager(); }
    void UpdatePager() { previous.IsEnabled = index > 0; next.IsEnabled = index + 1 < paginator.PageCount; count.Text = $"Page {index + 1} of {paginator.PageCount}"; }
    void Print() { var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) dialog.PrintDocument(paginator, "TypeMaster Report"); }
    void SavePdf()
    {
        string kind = ReportTitle switch { "TYPING TEST REPORT" => "Assessment", "Complete Report" => "Progress", "TypeMaster Test Certificate, " => "Certificate", _ => "Test-Results" };
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PDF report|*.pdf", FileName = "TypeMaster-" + kind + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".pdf", AddExtension = true, DefaultExt = ".pdf" };
        if (dialog.ShowDialog(this) != true) return;
        try { NativePdf.Export(paginator, dialog.FileName); MessageBox.Show(this, "Report saved to:\n" + dialog.FileName, "Save PDF"); }
        catch (Exception ex) { MessageBox.Show(this, "The PDF could not be saved.\n\n" + ex.Message, "Save PDF"); }
    }
}





