using System.Drawing.Drawing2D;

namespace BotZone.Admin.WinForms;

internal static partial class Ui
{
    internal static string StatusText(string value)=>value switch{
        "cancelled" or "canceled"=>"Đã hủy","completed"=>"Hoàn tất","expired"=>"Hết hạn","handed_off"=>"Đã bàn giao","confirmed"=>"Đã xác nhận","draft"=>"Bản nháp","pending"=>"Đang chờ","active"=>"Đang hoạt động","inactive"=>"Ngừng hoạt động","paid"=>"Đã thu đủ","unpaid"=>"Chưa thu","partially_paid"=>"Thu một phần","partially_refunded"=>"Hoàn một phần","refunded"=>"Đã hoàn","published"=>"Đã xuất bản","received"=>"Đã nhận","delivered"=>"Đã giao","failed"=>"Thất bại","processing"=>"Đang xử lý","pending_payment" or "awaiting_payment"=>"Chờ thanh toán","available"=>"Khả dụng","in_stock"=>"Trong kho","quarantine"=>"Chờ kiểm tra","rejected"=>"Đã từ chối",_=>value};
    private static void PaintStatusCell(object? sender,DataGridViewCellPaintingEventArgs e)
    {
        if(sender is not DataGridView grid||e.RowIndex<0||e.ColumnIndex<0)return;
        var property=grid.Columns[e.ColumnIndex].DataPropertyName.ToLowerInvariant();
        if(!property.Contains("status")&&property is not ("state" or "active"))return;
        var text=Convert.ToString(e.FormattedValue);if(string.IsNullOrWhiteSpace(text))return;
        var raw=Convert.ToString(e.Value)?.ToLowerInvariant()??"";
        var color=raw is "paid" or "active" or "completed" or "delivered" or "received" or "published" or "available" or "true" or "in_stock"?Success:
            raw is "cancelled" or "canceled" or "failed" or "rejected" or "inactive"?Danger:
            raw.Contains("pending")||raw is "draft" or "unpaid" or "processing" or "quarantine"?Warning:Muted;
        var fill=Color.FromArgb(245+(color.R-245)/12,245+(color.G-245)/12,245+(color.B-245)/12);
        var graphicsState=e.Graphics!.Save();
        try
        {
        e.Graphics.SetClip(Rectangle.Intersect(e.CellBounds,e.ClipBounds),CombineMode.Intersect);
        e.PaintBackground(e.ClipBounds,true);e.Paint(e.ClipBounds,DataGridViewPaintParts.Border);
        var scale=grid.DeviceDpi/96f;var textSize=TextRenderer.MeasureText(text,e.CellStyle!.Font);var rect=new Rectangle(e.CellBounds.Left+(int)(6*scale),e.CellBounds.Top+(e.CellBounds.Height-(int)(27*scale))/2,Math.Min(e.CellBounds.Width-(int)(12*scale),textSize.Width+(int)(10*scale)),(int)(27*scale));
        if(rect.Width>0){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=FieldPaint.Shape(rect,(int)(5*scale));using var background=new SolidBrush(fill);e.Graphics.FillPath(background,path);TextRenderer.DrawText(e.Graphics,text,e.CellStyle.Font,rect,color,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.PreserveGraphicsClipping);}
        }
        finally{e.Graphics.Restore(graphicsState);}
        e.Handled=true;
    }
}
