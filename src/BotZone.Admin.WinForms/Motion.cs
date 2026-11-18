using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BotZone.Admin.WinForms;

internal static class Motion
{
    internal const int FeedbackMs = 100, TransitionMs = 160;
    internal static bool ReduceForTesting { get; set; }
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, out int value, uint flags);
    internal static bool Enabled => !ReduceForTesting && !SystemInformation.HighContrast && SystemParametersInfo(0x1042, 0, out var enabled, 0) && enabled != 0;
    internal static Color Blend(Color a, Color b, double t) => Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));
    private static readonly ConditionalWeakTable<Control, PulseEffect> pulses = new();
    internal static void Pulse(Control control) { if (!control.IsDisposed) pulses.GetValue(control, c => new PulseEffect(c)).Start(); }
    private sealed class PulseEffect
    {
        private readonly Control owner; private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 }; private readonly Stopwatch clock = new();
        internal PulseEffect(Control owner) { this.owner=owner; owner.Paint+=Paint; owner.Disposed+=(_,_)=>timer.Dispose(); owner.VisibleChanged+=(_,_)=>{if(!owner.Visible)timer.Stop();}; timer.Tick+=(_,_)=>{owner.Invalidate();if(clock.ElapsedMilliseconds>=TransitionMs||!Enabled)timer.Stop();}; }
        internal void Start(){timer.Stop();clock.Restart();if(Enabled)timer.Start();owner.Invalidate();}
        private void Paint(object? sender, PaintEventArgs e){if(!timer.Enabled)return;var t=Math.Min(1,clock.Elapsed.TotalMilliseconds/TransitionMs);using var pen=new Pen(Blend(Ui.Primary,Ui.Line,1-Math.Pow(1-t,3)),2);e.Graphics.DrawRectangle(pen,1,1,Math.Max(0,owner.Width-3),Math.Max(0,owner.Height-3));}
    }
}

/// <summary>A native Button with stable bounds and short color-only feedback.</summary>
internal sealed class BzButton : Button
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval=16 };
    private readonly Stopwatch clock = new();
    private bool hover, pressed, primary, selected, busy,navigation,quiet;
    private Color from = Ui.Surface, to = Ui.Surface, current = Ui.Surface;
    private string busyText = "";
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Primary { get=>primary; set{primary=value;ForeColor=value?Color.White:Ui.Ink;Animate();} }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Selected {get=>selected;set{selected=value;if(!busy)AccessibleDescription=value?"Đang chọn":null;Animate();}}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Navigation {get=>navigation;set{navigation=value;Animate();}}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Quiet {get=>quiet;set{quiet=value;Animate();}}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string IconKey {get;set;}="";
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool IconOnly {get;set;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal int NavigationLevel {get;set;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Expanded {get;set;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool IsBusy => busy;
    // ButtonBase is opaque by default. Rounded painting must first restore the
    // parent background, including the pixels outside the rounded face.
    internal BzButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);SetStyle(ControlStyles.Opaque,false);FlatStyle=FlatStyle.Flat;UseVisualStyleBackColor=false;BackColor=Color.Transparent;timer.Tick+=(_,_)=>{var t=Math.Min(1,clock.Elapsed.TotalMilliseconds/Motion.FeedbackMs);current=Motion.Blend(from,to,1-Math.Pow(1-t,3));Invalidate();if(t>=1&&!busy)timer.Stop();if(!Visible||!Motion.Enabled){current=to;timer.Stop();}};}
    internal void SetBusy(bool value,string message=""){busy=value;busyText=message;AccessibleDescription=value?message:null;Animate();}
    private void Animate(){if(IsDisposed)return;var normal=Navigation?Ui.NavSurface:primary?Ui.Primary:Ui.Surface;to=!Enabled?(quiet?Ui.Surface:Ui.Line):selected?Ui.Soft:pressed?Motion.Blend(normal,Navigation?Ui.Primary:Ui.Ink,Navigation ? .12 : .22):hover?Motion.Blend(normal,primary?Color.White:Ui.Primary,.07):normal;from=current;clock.Restart();if(Motion.Enabled&&Visible&&IsHandleCreated)timer.Start();else{current=to;timer.Stop();}Invalidate();}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Animate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;pressed=false;Animate();}
    protected override void OnMouseDown(MouseEventArgs e){pressed=true;Animate();current=to;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Animate();base.OnMouseUp(e);}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode is Keys.Space or Keys.Enter){pressed=true;Animate();current=to;Invalidate();}base.OnKeyDown(e);}
    protected override void OnKeyUp(KeyEventArgs e){pressed=false;Animate();base.OnKeyUp(e);}
    protected override void OnClick(EventArgs e){if(!busy)base.OnClick(e);}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);pressed=false;Animate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Animate();}
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(!Visible)timer.Stop();else Animate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var scale=DeviceDpi/96f;var child=Navigation&&!IconOnly&&NavigationLevel==2;var group=Navigation&&!IconOnly&&NavigationLevel==1;
        var inset=child?(int)(24*scale):1;var rect=new Rectangle(inset,1,Width-inset-2,Height-3);if(rect.Width<=0||rect.Height<=0)return;
        using var shape=new GraphicsPath();var radius=Math.Min(20*DeviceDpi/96,Math.Min(rect.Width,rect.Height));shape.AddArc(rect.Left,rect.Top,radius,radius,180,90);shape.AddArc(rect.Right-radius,rect.Top,radius,radius,270,90);shape.AddArc(rect.Right-radius,rect.Bottom-radius,radius,radius,0,90);shape.AddArc(rect.Left,rect.Bottom-radius,radius,radius,90,90);shape.CloseFigure();
        using var brush=new SolidBrush(SystemInformation.HighContrast?SystemColors.Control:current);e.Graphics.FillPath(brush,shape);
        var showFocus=Focused&&ShowFocusCues;
        using var border=new Pen(showFocus?Ui.Focus:Navigation||quiet&&!primary?current:primary?Ui.Primary:Ui.Line,showFocus?2:1);e.Graphics.DrawPath(border,shape);
        if(selected&&!group){using var marker=new SolidBrush(Ui.Primary);e.Graphics.FillRectangle(marker,inset+1,Height/4,Math.Max(2,3*scale),Height/2);}
        var textRect=new Rectangle(Padding.Left,0,Math.Max(1,Width-Padding.Horizontal),Height);
        var color=!Enabled?Ui.Muted:group?(hover||Expanded?Ui.Ink:Ui.NavMuted):Navigation?(selected?Ui.Primary:Ui.Ink):primary?Color.White:ForeColor;
        if(group){textRect.X=(int)(12*scale);textRect.Width=Width-textRect.X-(int)(32*scale);var x=Width-18*scale;var y=Height/2f;using var chevron=new Pen(color,1.4f*scale);e.Graphics.DrawLines(chevron,Expanded?[new PointF(x-4*scale,y-2*scale),new PointF(x,y+2*scale),new PointF(x+4*scale,y-2*scale)]:[new PointF(x-2*scale,y-4*scale),new PointF(x+2*scale,y),new PointF(x-2*scale,y+4*scale)]);}
        else if(child){textRect.X=(int)(38*scale);textRect.Width=Math.Max(1,Width-textRect.X-8);}
        else if(Navigation&&IconKey.Length>0){var d=18*DeviceDpi/96;var x=IconOnly?(Width-d)/2:12*DeviceDpi/96;NavIcon.Draw(e.Graphics,IconKey,new Rectangle(x,(Height-d)/2,d,d),selected?Ui.Primary:Ui.NavMuted);if(IconOnly)return;textRect.X=x+d+12*DeviceDpi/96;textRect.Width=Width-textRect.X-8;}
        if(busy){var d=14*DeviceDpi/96;using var pen=new Pen(color,2);e.Graphics.DrawArc(pen,8,(Height-d)/2,d,d,Motion.Enabled?(float)(clock.Elapsed.TotalMilliseconds*.35%360):0,260);textRect.X+=d+8;textRect.Width-=d+8;}
        var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.PreserveGraphicsClipping;
        if(Navigation)flags|=TextFormatFlags.NoPrefix;else if(!ShowKeyboardCues)flags|=TextFormatFlags.HidePrefix;
        flags|=TextAlign is ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter;
        TextRenderer.DrawText(e.Graphics,busy?busyText:Text,Font,textRect,color,flags);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}

internal sealed class BusyScope : IDisposable
{
    private sealed class State { internal bool Enabled;internal string? Description;internal readonly List<(Guid Id,string Text)> Active=[]; }
    private static readonly ConditionalWeakTable<Control,State> states=new();
    private readonly Control control;private readonly State state;private readonly Guid id=Guid.NewGuid();private bool disposed;
    internal BusyScope(Control control,string text){this.control=control;state=states.GetValue(control,_=>new State());if(state.Active.Count==0){state.Enabled=control.Enabled;state.Description=control.AccessibleDescription;}state.Active.Add((id,text));if(control is BzButton button)button.SetBusy(true,text);else{control.Enabled=false;control.AccessibleDescription=text;}}
    public void Dispose(){if(disposed)return;disposed=true;state.Active.RemoveAll(x=>x.Id==id);if(control.IsDisposed)return;if(state.Active.Count>0){if(control is BzButton pending)pending.SetBusy(true,state.Active[^1].Text);return;}if(control is BzButton button)button.SetBusy(false);else control.Enabled=state.Enabled;control.AccessibleDescription=state.Description;}
}
internal sealed class BusyLabel : Label
{
    private readonly System.Windows.Forms.Timer timer=new(){Interval=40};private int angle;private bool busy;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool IsBusy{get=>busy;set{busy=value;if(value&&Visible&&Motion.Enabled)timer.Start();else timer.Stop();Invalidate();}}
    internal BusyLabel(){DoubleBuffered=true;timer.Tick+=(_,_)=>{angle=(angle+18)%360;if(!Motion.Enabled||!Visible)timer.Stop();Invalidate();};}
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);IsBusy=busy;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(!busy)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var pen=new Pen(Ui.Primary,2);e.Graphics.DrawArc(pen,2,Math.Max(2,(Height-12)/2),12,12,angle,260);}
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
internal sealed class BzTabControl : TabControl
{
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string className,string? title);
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr handle,out NativeRect rect);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(IntPtr handle);
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{internal int Left,Top,Right,Bottom;}
    private readonly System.Windows.Forms.Timer timer=new(){Interval=16};private readonly Stopwatch clock=new();
    internal BzTabControl(){DrawMode=TabDrawMode.OwnerDrawFixed;Padding=new Point(16,10);Font=new Font("Segoe UI",10.5f);timer.Tick+=(_,_)=>{Invalidate();if(clock.ElapsedMilliseconds>=Motion.TransitionMs||!Visible||!Motion.Enabled)timer.Stop();};}
    protected override void OnControlAdded(ControlEventArgs e){base.OnControlAdded(e);if(e.Control is TabPage page){page.UseVisualStyleBackColor=false;page.BackColor=Ui.Paper;}}
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var state=e.Graphics.Save();
        try{if(TabCount>0&&GetTabRect(TabCount-1).Right>Width)e.Graphics.SetClip(new Rectangle(0,0,Math.Max(0,Width-44*DeviceDpi/96),Height),CombineMode.Intersect);
            var rect=GetTabRect(e.Index);var selected=e.Index==SelectedIndex;using var brush=new SolidBrush(selected?Ui.Soft:Ui.Paper);e.Graphics.FillRectangle(brush,rect);TextRenderer.DrawText(e.Graphics,TabPages[e.Index].Text,Font,rect,selected?Ui.Primary:Ui.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.PreserveGraphicsClipping);if(selected){var t=timer.Enabled?Math.Min(1,clock.Elapsed.TotalMilliseconds/Motion.TransitionMs):1;using var pen=new Pen(Focused?Ui.Focus:Motion.Blend(Ui.ControlBorder,Ui.Primary,1-Math.Pow(1-t,3)),3);e.Graphics.DrawLine(pen,rect.Left+8,rect.Bottom-2,rect.Right-8,rect.Bottom-2);}
        }finally{e.Graphics.Restore(state);}
    }
    protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);timer.Stop();clock.Restart();if(Motion.Enabled)timer.Start();Invalidate();}
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(!Visible)timer.Stop();}
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);if(m.Msg is not (0xF or 0x317 or 0x318)||!IsHandleCreated||TabCount==0)return;
        using var g=m.Msg!=0xF&&m.WParam!=IntPtr.Zero?Graphics.FromHdc(m.WParam):Graphics.FromHwnd(Handle);
        var scroller=FindWindowEx(Handle,IntPtr.Zero,"msctls_updown32",null);
        if(scroller!=IntPtr.Zero&&IsWindowVisible(scroller)&&GetWindowRect(scroller,out var bounds)){var point=PointToClient(new Point(bounds.Left,bounds.Top));g.ExcludeClip(new Rectangle(point.X,point.Y,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top));}
        using var paper=new SolidBrush(Ui.Paper);var page=DisplayRectangle;
        g.FillRectangle(paper,new Rectangle(0,0,Width,Math.Max(0,page.Top)));
        g.FillRectangle(paper,new Rectangle(0,page.Top,Math.Max(0,page.Left),Height-page.Top));
        g.FillRectangle(paper,new Rectangle(page.Right,page.Top,Math.Max(0,Width-page.Right),Height-page.Top));
        g.FillRectangle(paper,new Rectangle(0,page.Bottom,Width,Math.Max(0,Height-page.Bottom)));
        using var line=new Pen(Ui.Line);g.DrawLine(line,0,page.Top-1,Width,page.Top-1);
        if(GetTabRect(TabCount-1).Right>Width)g.SetClip(new Rectangle(0,0,Math.Max(0,Width-44*DeviceDpi/96),Height),CombineMode.Intersect);
        for(var i=0;i<TabCount;i++){var r=GetTabRect(i);if(r.Left>=Width)break;var selected=i==SelectedIndex;TextRenderer.DrawText(g,TabPages[i].Text,Font,r,selected?Ui.Primary:Ui.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.PreserveGraphicsClipping);if(selected){var progress=timer.Enabled?Math.Min(1,clock.Elapsed.TotalMilliseconds/Motion.TransitionMs):1;using var accent=new Pen(Focused?Ui.Focus:Motion.Blend(Ui.ControlBorder,Ui.Primary,1-Math.Pow(1-progress,3)),3);g.DrawLine(accent,r.Left+10,page.Top-2,Math.Min(Width,r.Right-10),page.Top-2);}}
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
internal sealed class BzDataGrid : DataGridView { internal BzDataGrid(){DoubleBuffered=true;} }
