using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TypeMaster.Native;
public partial class MainWindow
{
    string statisticsCourse = "";
    void Statistics()
    {
        var canvas=ReferencePage("Statistics");var charts=new Canvas();Position(canvas,charts,0,77,593,373);
        var choices=new List<CourseDefinition>{new(){File="*",Name="All exercises"}};choices.AddRange(allCourses.Where(c=>c.Language==Current!.Language||c.File==Current!.CourseFile));
        if(statisticsCourse.Length==0)statisticsCourse=choices.Any(c=>c.File==Current!.CourseFile)?Current!.CourseFile:"*";
        var chooser=new ComboBox{ItemsSource=choices,SelectedItem=choices.FirstOrDefault(c=>c.File==statisticsCourse)??choices[0],FontSize=12,Style=(Style)Application.Current.FindResource("ReferenceCombo")};Position(canvas,chooser,280,15,298,24);
        Button speed=null!,accuracy=null!;bool percentages=false;
        speed=RefButton("Typing Speed",()=>{percentages=false;Draw();});Position(canvas,speed,18,50,181,27);accuracy=RefButton("Accuracy",()=>{percentages=true;Draw();});Position(canvas,accuracy,206,50,181,27);Rule(canvas,0,76,593);
        void Draw()
        {
            charts.Children.Clear();speed.FontWeight=percentages?FontWeights.Normal:FontWeights.Bold;accuracy.FontWeight=percentages?FontWeights.Bold:FontWeights.Normal;
            var course=chooser.SelectedItem as CourseDefinition;var results=Current!.Results.Where(r=>course?.File=="*"||r.CourseFile==course?.File).TakeLast(24).ToList();
            for(int metric=0;metric<2;metric++)
            {
                bool difficult=percentages&&metric==1;
                int Value(TypingResult result)=>difficult?result.Mistakes.Count:percentages?result.Accuracy:metric==0?result.GrossWpm:result.NetWpm;
                double top=47+metric*200;int minimum=percentages&&!difficult?80:0;int maximum=difficult?Math.Max(3,results.Select(Value).DefaultIfEmpty(0).Max()):percentages?100:Math.Max(25,(int)Math.Ceiling(results.Select(Value).DefaultIfEmpty(0).Max()/5d)*5);
                Position(charts,RefText(difficult?"Difficult Keys":percentages?"Accuracy":metric==0?"Gross Speed (wpm)":"Net Speed (wpm)",14,true,true),24,top-28,500,24);
                var plot=new Canvas{Background=Brushes.White,ClipToBounds=true};Position(charts,new Border{BorderBrush=Brushes.Black,BorderThickness=new Thickness(1),Child=plot},47,top,497,128);
                double Y(double value)=>126-(value-minimum)/(maximum-minimum)*126;
                if(!difficult)for(int tick=0;tick<=5;tick++){if(percentages&&tick!=0&&tick!=5)continue;double value=minimum+(maximum-minimum)*tick/5d;var label=RefText(value.ToString("0"),12);label.TextAlignment=TextAlignment.Right;Position(charts,label,12,top+126-tick*126d/5-8,29,18);}
                void Threshold(double value,string title){double y=Y(value);Rule(plot,0,y,495,Brushes.YellowGreen);var label=RefText(title,12,true);label.Foreground=Brushes.OliveDrab;label.TextAlignment=TextAlignment.Right;Position(plot,label,300,Math.Max(0,y-25),190,22);}
                if(difficult){Threshold(maximum*.8,"Problematic");Threshold(maximum*.5,"Difficult");Threshold(maximum*.2,"OK");}
                else if(percentages){int goal=course?.AccuracyGoal??94;Threshold(goal,Localization.Translate("Accuracy Goal")+" ("+goal+"%)");}
                else{Threshold(maximum*.6,"Good");Threshold(maximum*.32,"OK");}
                var line=new Polyline{Stroke=Brushes.RoyalBlue,StrokeThickness=1.5};for(int i=0;i<results.Count;i++){var result=results[i];double x=results.Count==1?10:5+i*485d/(results.Count-1),y=Y(Value(result));line.Points.Add(new Point(x,y));Position(plot,new Ellipse{Fill=Brushes.RoyalBlue,ToolTip=result.Title+"\n"+result.Date.ToString("g")+"\n"+Value(result)},x-2,y-2,4,4);}plot.Children.Insert(0,line);
            }
        }
        chooser.SelectionChanged+=(_,_)=>{statisticsCourse=(chooser.SelectedItem as CourseDefinition)?.File??"*";Draw();};Draw();Position(canvas,RefButton("▸ Progress Report",PrintReport,true),24,481,175,34);
        var menu=new ContextMenu();var detail=new MenuItem{Header="Exercise history and result export…"};detail.Click+=(_,_)=>StatisticsDetails();menu.Items.Add(detail);canvas.ContextMenu=menu;
    }
}
