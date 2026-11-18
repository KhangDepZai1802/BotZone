using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace BotZone.Admin.WinForms;

internal sealed class BrandMark : Control
{
    private static readonly Image Logo=Read("Logo"),Mark=Read("Icon");
    internal static Icon WindowIcon{get;}=ReadIcon();
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IconOnly{get;set;}
    internal BrandMark(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;Size=new Size(176,48);AccessibleRole=AccessibleRole.Graphic;AccessibleName="BotZone";TabStop=false;}
    private static Image Read(string name){using var stream=typeof(BrandMark).Assembly.GetManifestResourceStream($"BotZone.Brand.{name}.png")!;using var original=Image.FromStream(stream);return new Bitmap(original);}
    private static Icon ReadIcon(){using var stream=typeof(BrandMark).Assembly.GetManifestResourceStream("BotZone.Brand.App.ico")!;using var original=new Icon(stream);return (Icon)original.Clone();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        var target=IconOnly?new Rectangle(0,0,Math.Min(Width,Height),Math.Min(Width,Height)):new Rectangle(0,0,Width,Math.Min(Height,(int)(Width/3.6f)));
        if(target.Width<2||target.Height<2)return;using var path=FieldPaint.Shape(target,IconOnly?8:6);var state=g.Save();g.SetClip(path);g.Clear(Color.White);
        if(IconOnly)g.DrawImage(Mark,target);else{var source=new Rectangle(100,490,1060,260);var available=Rectangle.Inflate(target,-8,-5);var ratio=Math.Min(available.Width/(float)source.Width,available.Height/(float)source.Height);var w=(int)(source.Width*ratio);var h=(int)(source.Height*ratio);var box=new Rectangle(target.X+(target.Width-w)/2,target.Y+(target.Height-h)/2,w,h);g.DrawImage(Logo,box,source,GraphicsUnit.Pixel);}g.Restore(state);
    }
}
