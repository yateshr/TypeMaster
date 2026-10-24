using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace TypeMaster.Native;
internal static class ReportBranding
{
    internal const string DefaultCompany = "Nirmal Poly Plast Private Limited";
    internal static CompanyBranding Current {get;private set;}=new();
    internal static string Company=>Current.Name;
    internal static string? LoadNotice {get;private set;}
    internal static void Configure(string directory)
    {
        LoadNotice=null;
        try{Current=BrandingStorage.Load(directory);}catch(System.IO.InvalidDataException ex){Current=new();LoadNotice=ex.Message;}
    }
    internal static void Save(string directory,CompanyBranding value){BrandingStorage.Save(directory,value);Current=value with {};LoadNotice=null;}
    static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(24,39,66));
    static readonly Brush Outline = new SolidColorBrush(Color.FromRgb(198,211,233));
    internal static void Draw(DrawingContext draw, double width,CompanyBranding? branding=null)
    {
        branding??=Current;
        var first=branding.FirstLogo==null?MainWindow.AssetBitmap("Branding/ratan.png"):branding.FirstLogo.Length==0?null:BrandingStorage.Image(branding.FirstLogo);
        var second=branding.SecondLogo==null?MainWindow.AssetBitmap("Branding/kolorr.png"):branding.SecondLogo.Length==0?null:BrandingStorage.Image(branding.SecondLogo);
        void Logo(ImageSource image,Rect box){double factor=Math.Min(box.Width/image.Width,box.Height/image.Height);double w=image.Width*factor,h=image.Height*factor;draw.DrawImage(image,new Rect(box.X+(box.Width-w)/2,box.Y+(box.Height-h)/2,w,h));}
        if(first!=null&&second!=null){Logo(first,new Rect(50,31,90,90d*96/334));Logo(second,new Rect(50,61,90,90d*96/334));}
        else if(first!=null||second!=null)Logo((ImageSource?)first??second!,new Rect(50,31,90,56));
        draw.DrawLine(new Pen(Outline,1),new Point(157,29),new Point(157,91));
        var title = Text(branding.Name,19,Ink,true); title.MaxTextWidth = width-224; title.MaxLineCount=2; title.Trimming = TextTrimming.CharacterEllipsis; draw.DrawText(title,new Point(174,47));
        draw.DrawLine(new Pen(Ink,1),new Point(40,104),new Point(width-40,104));
    }
    static FormattedText Text(string value,double size,Brush brush,bool bold=false) => new(value,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,brush,1);
}



