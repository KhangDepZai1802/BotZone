namespace BotZone.Admin.WinForms;

// Explicitly measures its two text rows and responsive action strip. Native TLP
// row-span autosizing can retain an empty action row after DPI/window changes.
internal sealed class PageHeading : Panel
{
    private readonly Label title, subtitle;
    private readonly FlowLayoutPanel actions;
    private bool arranging,layoutQueued,hideDescription;
    internal void CompactDescription(bool hidden){if(hideDescription==hidden)return;hideDescription=hidden;subtitle.Visible=!hidden&&!string.IsNullOrWhiteSpace(subtitle.Text);PerformLayout();Parent?.PerformLayout();}
    internal PageHeading(string heading,string description,Control[] commands)
    {
        Dock=DockStyle.Top;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,0,0,16);Height=80;DoubleBuffered=true;
        title=Ui.Label(heading,true);subtitle=Ui.Label(description);subtitle.ForeColor=Ui.Muted;
        title.AutoSize=false;subtitle.AutoSize=false;subtitle.Visible=!string.IsNullOrWhiteSpace(description);
        actions=Ui.Bar(commands);actions.AutoSize=false;actions.WrapContents=true;actions.Margin=Padding.Empty;
        Controls.AddRange([title,subtitle,actions]);
    }
    private (int Height,int TextWidth,int TitleHeight,int DescriptionHeight,int ActionsWidth,int ActionsHeight,bool Compact) Measure(int width)
    {
        var gap=(int)(16*DeviceDpi/96f);var aw=actions.Controls.Cast<Control>().Where(c=>c.Visible).Sum(c=>c.GetPreferredSize(Size.Empty).Width+c.Margin.Horizontal)+actions.Padding.Horizontal;
        var naturalTitle=TextRenderer.MeasureText(title.Text,title.Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix).Width;
        var compact=aw>0&&aw+Math.Min(naturalTitle,400*DeviceDpi/96)+gap>width;
        var tw=Math.Max(80,compact?width:width-aw-gap);
        var th=TextRenderer.MeasureText(title.Text,title.Font,new Size(tw,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height;
        var dh=hideDescription||string.IsNullOrWhiteSpace(subtitle.Text)?0:TextRenderer.MeasureText(subtitle.Text,subtitle.Font,new Size(tw,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height;
        var ah=actions.Controls.Count==0?0:actions.GetPreferredSize(new Size(compact?width:aw,0)).Height;
        var textHeight=th+(dh>0?dh+(int)(6*DeviceDpi/96f):0);
        return (compact?textHeight+(ah>0?gap/2+ah:0):Math.Max(textHeight,ah),tw,th,dh,aw,ah,compact);
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        if(title is null)return base.GetPreferredSize(proposedSize);
        // TLP probes with width=1 while allocating an AutoSize row. That is not
        // the actual available width and must not inflate wrapped title height.
        var width=Math.Max(80,(Parent?.DisplayRectangle.Width??Width)-Margin.Horizontal);
        return new Size(width,Measure(width).Height);
    }
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(Visible){PerformLayout();Parent?.PerformLayout();}}
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);if(arranging||title is null||Width<1)return;arranging=true;
        try{var m=Measure(ClientSize.Width);title.SetBounds(0,0,m.TextWidth,m.TitleHeight);subtitle.SetBounds(0,m.TitleHeight+(int)(6*DeviceDpi/96f),m.TextWidth,m.DescriptionHeight);
            actions.SetBounds(m.Compact?0:ClientSize.Width-m.ActionsWidth,m.Compact?m.Height-m.ActionsHeight:0,m.Compact?ClientSize.Width:m.ActionsWidth,m.ActionsHeight);
            if(Height!=m.Height){Height=m.Height;QueueParentLayout();}
        }finally{arranging=false;}
    }
    private void QueueParentLayout()
    {
        if(layoutQueued||!IsHandleCreated||IsDisposed)return;layoutQueued=true;
        BeginInvoke((Action)(()=>{layoutQueued=false;if(!IsDisposed&&Parent is {IsDisposed:false} owner)owner.PerformLayout();}));
    }
}
