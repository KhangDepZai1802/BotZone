using System.Drawing.Drawing2D;

namespace BotZone.Admin.WinForms;

internal sealed class OrderProgress : Control
{
    private string state="";
    internal OrderProgress(){DoubleBuffered=true;BackColor=Ui.Surface;Height=90;AccessibleRole=AccessibleRole.Graphic;AccessibleName="Tiến trình đơn hàng";Margin=new Padding(0,0,0,10);}
    internal void SetState(string value){state=value;AccessibleDescription="Trạng thái hiện tại: "+OrdersView.StateLabel(value)+". Các bước thông thường: xác nhận, chuẩn bị hàng, giao hàng, hoàn tất.";Invalidate();}
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);Height=(int)(90*DeviceDpi/96f);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var s=DeviceDpi/96f;var active=state switch{"pending"=>0,"confirmed"=>1,"handed_off" or "in_transit" or "delivered"=>2,"completed"=>3,_=>-1};
        string[] labels=["Xác nhận đơn","Chuẩn bị hàng","Giao hàng","Hoàn tất"];
        var step=ClientSize.Width/4f;var y=26*s;using var line=new Pen(Ui.Line,2*s);g.DrawLine(line,step/2,y,ClientSize.Width-step/2,y);
        using var font=new Font("Segoe UI",10f);using var number=new Font("Segoe UI Semibold",10f);
        for(var i=0;i<4;i++){var x=step*(i+.5f);var diameter=26*s;var current=i==active;using var fill=new SolidBrush(current?Ui.Primary:Ui.Paper);g.FillEllipse(fill,x-diameter/2,y-diameter/2,diameter,diameter);TextRenderer.DrawText(g,(i+1).ToString(),number,Rectangle.Round(new RectangleF(x-diameter/2,y-diameter/2,diameter,diameter)),current?Color.White:Ui.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,labels[i],font,Rectangle.Round(new RectangleF(step*i,y+20*s,step,26*s)),current?Ui.Primary:Ui.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);}
    }
}
