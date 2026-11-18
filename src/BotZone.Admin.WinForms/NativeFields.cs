using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace BotZone.Admin.WinForms;

internal sealed class InputSurface : Panel
{
    private readonly Control input;private bool hovered,ready;
    internal InputSurface(Control input)
    {
        this.input=input;DoubleBuffered=true;BackColor=Color.Transparent;Padding=new Padding(12,8,12,8);Margin=input.Margin;
        Width=input.Width;MinimumSize=new Size(input.Width,0);Height=input is TextBox {Multiline:true}?Math.Max(80,input.Height+16):52;
        if(input is TextBoxBase text){text.BorderStyle=BorderStyle.None;text.BackColor=Ui.Surface;}
        if(input is ComboBox combo)combo.FlatStyle=FlatStyle.Flat;
        if(input is NumericUpDown number)number.BorderStyle=BorderStyle.None;
        input.Margin=Padding.Empty;input.Dock=DockStyle.None;Controls.Add(input);ready=true;PerformLayout();
        input.GotFocus+=(_,_)=>Invalidate();input.LostFocus+=(_,_)=>Invalidate();input.EnabledChanged+=(_,_)=>Invalidate();
        input.MouseEnter+=(_,_)=>{hovered=true;Invalidate();};input.MouseLeave+=(_,_)=>{hovered=false;Invalidate();};
        MouseDown+=(_,_)=>input.Focus();AccessibleRole=AccessibleRole.Grouping;AccessibleName=input.AccessibleName;
    }
    public override Size GetPreferredSize(Size proposedSize)=>new(Math.Max(MinimumSize.Width,proposedSize.Width),Height);
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);if(input is null||!ready)return;var scale=DeviceDpi/96f;var horizontal=(int)(12*scale);
        var h=input is TextBox {Multiline:true}?Math.Max(24,ClientSize.Height-(int)(16*scale)):input.PreferredSize.Height;
        input.SetBounds(horizontal,Math.Max(2,(ClientSize.Height-h)/2),Math.Max(20,ClientSize.Width-horizontal*2),h);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=FieldPaint.Shape(new Rectangle(1,1,Width-3,Height-3),10*DeviceDpi/96);using var fill=new SolidBrush(Ui.Surface);e.Graphics.FillPath(fill,path);using var pen=new Pen(input.ContainsFocus?Ui.Focus:hovered?Ui.Primary:Ui.Line,input.ContainsFocus?2:1);e.Graphics.DrawPath(pen,path);}
}
internal static class FieldPaint
{
    internal static GraphicsPath Shape(Rectangle r,int radius)
    {
        var path=new GraphicsPath();var d=Math.Max(1,Math.Min(radius*2,Math.Min(r.Width,r.Height)));path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
    internal static void Draw(Graphics g,Control control,string text,bool calendar=false)
    {
        var r=control.ClientRectangle;using var background=new SolidBrush(control.Enabled?Ui.Surface:Ui.Paper);g.FillRectangle(background,r);var scale=control.DeviceDpi/96f;var right=(int)(26*scale);
        var framed=control.Parent is InputSurface;var left=framed?2:10;
        if(!framed){using var outline=Shape(new Rectangle(1,1,Math.Max(1,r.Width-3),Math.Max(1,r.Height-3)),(int)(10*scale));using var edge=new Pen(control.ContainsFocus?Ui.Focus:Ui.Line,control.ContainsFocus?2:1);g.DrawPath(edge,outline);}
        TextRenderer.DrawText(g,text,control.Font,new Rectangle(left,0,Math.Max(1,r.Width-right-left),r.Height),control.Enabled?Ui.Ink:Ui.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix|TextFormatFlags.PreserveGraphicsClipping);
        DrawGlyph(g,control,calendar);
    }
    internal static void DrawDateEditor(Graphics g,Control control)
    {
        // Keep the native editable date segments and their keyboard selection.
        // Only replace the native frame and drop-down button inside our surface.
        var r=control.ClientRectangle;var right=(int)(34*control.DeviceDpi/96f);
        using var background=new SolidBrush(control.Enabled?Ui.Surface:Ui.Paper);
        g.FillRectangle(background,0,0,r.Width,1);g.FillRectangle(background,0,r.Height-1,r.Width,1);
        g.FillRectangle(background,0,0,1,r.Height);g.FillRectangle(background,Math.Max(0,r.Width-right),0,right,r.Height);
        if(control.Parent is not InputSurface){using var outline=Shape(new Rectangle(1,1,Math.Max(1,r.Width-3),Math.Max(1,r.Height-3)),10*control.DeviceDpi/96);using var edge=new Pen(Ui.Focus,2);g.DrawPath(edge,outline);}
        DrawGlyph(g,control,true);
    }
    private static void DrawGlyph(Graphics g,Control control,bool calendar)
    {
        var r=control.ClientRectangle;var scale=control.DeviceDpi/96f;g.SmoothingMode=SmoothingMode.AntiAlias;
        using var pen=new Pen(Ui.Muted,1.3f*scale);var x=r.Right-(int)(14*scale);var y=r.Height/2;
        if(calendar){g.DrawRectangle(pen,x-6*scale,y-5*scale,12*scale,11*scale);g.DrawLine(pen,x-6*scale,y-1*scale,x+6*scale,y-1*scale);g.DrawLine(pen,x-3*scale,y-7*scale,x-3*scale,y-3*scale);g.DrawLine(pen,x+3*scale,y-7*scale,x+3*scale,y-3*scale);}
        else{g.DrawLines(pen,[new PointF(x-4*scale,y-2*scale),new PointF(x,y+2*scale),new PointF(x+4*scale,y-2*scale)]);}
    }
}
internal sealed class BzComboBox : ComboBox
{
    private readonly ComboPopupChrome popup=new();
    internal BzComboBox(){FlatStyle=FlatStyle.Flat;DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=34;IntegralHeight=false;}
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if(e.Index<0||e.Index>=Items.Count)return;ComboPopupChrome.DrawItem(e,GetItemText(Items[e.Index])??"",Font,DeviceDpi,Enabled);
    }
    protected override void OnDropDown(EventArgs e){using var graphics=CreateGraphics();DropDownWidth=Math.Min(Screen.FromControl(this).WorkingArea.Width,Math.Max(Width,Items.Cast<object>().Select(item=>TextRenderer.MeasureText(graphics,GetItemText(item)??"",Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix).Width+36*DeviceDpi/96).DefaultIfEmpty(Width).Max()));base.OnDropDown(e);BeginInvoke((Action)(()=>{if(!IsDisposed&&IsHandleCreated&&DroppedDown)popup.Apply(this);}));}
    protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg is 0xF or 0x317 or 0x318 && DropDownStyle==ComboBoxStyle.DropDownList&&IsHandleCreated){using var g=m.Msg!=0xF&&m.WParam!=IntPtr.Zero?Graphics.FromHdc(m.WParam):Graphics.FromHwnd(Handle);FieldPaint.Draw(g,this,Text);}}
    protected override void Dispose(bool disposing){if(disposing)popup.Dispose();base.Dispose(disposing);}
}
internal sealed class BzDateTimePicker : DateTimePicker
{
    private CalendarPopup? calendar;
    internal BzDateTimePicker(){Width=220;}
    protected override CreateParams CreateParams{get{var value=base.CreateParams;value.Style&=~0x00800000;value.ExStyle&=~0x00000200;return value;}}
    internal void OpenCalendar(){if(!Enabled||IsDisposed)return;if(calendar is {IsDisposed:false}){calendar.Close();calendar.Dispose();}calendar=new CalendarPopup(this);calendar.Show(this,new Point(0,Height+8));calendar.FocusSelected();}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.F4||e.Alt&&e.KeyCode==Keys.Down){e.Handled=e.SuppressKeyPress=true;OpenCalendar();return;}base.OnKeyDown(e);}
    protected override void WndProc(ref Message m){if(m.Msg==0x201&&Enabled&&(m.LParam.ToInt64()&0xffff)>Width-34*DeviceDpi/96){Focus();OpenCalendar();return;}base.WndProc(ref m);if(m.Msg is 0xF or 0x317 or 0x318&&IsHandleCreated){using var g=m.Msg!=0xF&&m.WParam!=IntPtr.Zero?Graphics.FromHdc(m.WParam):Graphics.FromHwnd(Handle);if(Focused)FieldPaint.DrawDateEditor(g,this);else FieldPaint.Draw(g,this,Text,true);}}
    protected override void Dispose(bool disposing){if(disposing){calendar?.Dispose();calendar=null;}base.Dispose(disposing);}
}

internal static partial class Ui
{
    internal static Control FrameInput(Control input)
    {
        if(input is not (TextBoxBase or ComboBox or NumericUpDown or DateTimePicker))return input;
        return new InputSurface(input){Dock=DockStyle.Top};
    }
}
