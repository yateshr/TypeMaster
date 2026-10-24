using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace TypeMaster.Native;
internal sealed class CompanyBrandingWindow : Window
{
    readonly string directory;
    CompanyBranding draft;
    readonly TextBox name;
    readonly Image preview;
    internal CompanyBrandingWindow(string directory)
    {
        this.directory=directory;draft=ReportBranding.Current with {};
        Title="Company Branding";Width=820;Height=540;MinWidth=320;MinHeight=300;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Brushes.White;FontFamily=new FontFamily("Segoe UI");FontSize=14;
        var root=new StackPanel{Margin=new Thickness(20)};root.Children.Add(new TextBlock{Text="Company name and report logos",FontSize=21,FontWeight=FontWeights.SemiBold});
        root.Children.Add(new TextBlock{Text="Applies to reports for every candidate on this Windows account. Current company branding remains the default.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,14)});
        if(ReportBranding.LoadNotice!=null)root.Children.Add(new TextBlock{Text=ReportBranding.LoadNotice,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DarkRed});
        root.Children.Add(new TextBlock{Text="Company name"});name=new TextBox{Text=draft.Name,MaxLength=200,Padding=new Thickness(8),Margin=new Thickness(0,4,0,12)};name.TextChanged+=(_,_)=>Refresh();root.Children.Add(name);
        var logos=new WrapPanel();
        Button Action(string title,System.Action action){var b=new Button{Content=title};b.Click+=(_,_)=>action();return b;}
        void Pick(bool first){var dialog=new OpenFileDialog{Filter="Logo images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff"};if(dialog.ShowDialog(this)!=true)return;try{string image=BrandingStorage.Import(dialog.FileName);if(first)draft.FirstLogo=image;else draft.SecondLogo=image;Refresh();}catch(System.Exception ex){MessageBox.Show(this,"This image could not be loaded.\n"+ex.Message,"Company logo");}}
        logos.Children.Add(Action("Choose first logo",()=>Pick(true)));logos.Children.Add(Action("Remove first",()=>{draft.FirstLogo="";Refresh();}));logos.Children.Add(Action("Choose second logo",()=>Pick(false)));logos.Children.Add(Action("Remove second",()=>{draft.SecondLogo="";Refresh();}));root.Children.Add(logos);
        preview=new Image{Height=115,Stretch=Stretch.Uniform,Margin=new Thickness(0,12,0,14)};root.Children.Add(new Border{Background=Brushes.White,BorderBrush=Brushes.LightGray,BorderThickness=new Thickness(1),Child=preview});
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};
        actions.Children.Add(Action("Restore current company defaults",()=>{draft=new();name.Text=draft.Name;Refresh();}));var cancel=Action("Cancel",()=>DialogResult=false);cancel.IsCancel=true;actions.Children.Add(cancel);
        actions.Children.Add(Action("Save",()=>{try{draft.Name=name.Text.Trim();ReportBranding.Save(directory,draft);DialogResult=true;}catch(System.Exception ex){MessageBox.Show(this,ex.Message,"Branding could not be saved");}}));root.Children.Add(actions);Content=new ScrollViewer{Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Refresh();
    }
    void Refresh()
    {
        if(preview==null)return;draft.Name=name.Text;var drawing=new DrawingGroup();using(var context=drawing.Open())ReportBranding.Draw(context,794,draft);preview.Source=new DrawingImage(drawing);
    }
}
public partial class MainWindow
{
    void CompanyBrandingSettings()=>new CompanyBrandingWindow(Store.DirectoryPath){Owner=this}.ShowDialog();
}

