using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TypeMaster.Native;
public partial class MainWindow
{
    void PreviewReport(FlowDocument document) => new ReferenceReportWindow(document) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    void PrintReport() => new ReferenceReportWindow(ReportFactory.Progress(Current!), Current!, Store.Save, () => Store.Profiles.SelectMany(p=>p.CustomTechnicalSkills), () => Store.Profiles.SelectMany(p=>p.TechnicalSkillApplications).GroupBy(p=>p.Key,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.Last().Value,StringComparer.OrdinalIgnoreCase)) {Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner}.ShowDialog();
    void StatisticsDetails()
    {
        Heading("Statistics");
        var tabs = new WrapPanel(); var charts = new StackPanel();
        void Plot(string mode)
        {
            charts.Children.Clear();
            foreach (var metric in mode == "Accuracy" ? new[] { "Accuracy (%)" } : new[] { "Gross Speed (wpm)", "Net Speed (wpm)" })
            {
                charts.Children.Add(Text(metric, 15, true)); var barPanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 110, Margin = new Thickness(0, 4, 0, 16) };
                var recent = Current!.Results.TakeLast(12).ToList();
                int Value(TypingResult r) => mode == "Accuracy" ? r.Accuracy : metric.StartsWith("Gross") ? r.GrossWpm : r.NetWpm;
                int maximum = mode == "Accuracy" ? 100 : Math.Max(1, recent.Select(Value).DefaultIfEmpty(1).Max());
                foreach (var r in recent) barPanel.Children.Add(new Border { Background = Brushes.RoyalBlue, Width = 30, Height = Math.Max(14, Value(r) * 105d / maximum), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(3, 0, 3, 0), ToolTip = r.Title + " · " + r.Date.ToString("g"), Child = new TextBlock { Text = Value(r).ToString(), Foreground = Brushes.White, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center } });
                charts.Children.Add(barPanel);
            }
        }
        tabs.Children.Add(Button("Typing Speed", () => Plot("Speed"))); tabs.Children.Add(Button("Accuracy", () => Plot("Accuracy"))); Add(tabs); Add(charts); Plot("Speed");
        if (Current!.Results.Count == 0) Note("Complete a test or course exercise to see your progress here.");
        Add(Outline("▸ Progress Report", PrintReport));
        Add(ResultsGrid(Current.Results.AsEnumerable().Reverse())); Add(Outline("Export Results", Export));
    }
}





