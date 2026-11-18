using System.Drawing.Drawing2D;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

internal sealed class SettingsView : PageView
{
    private readonly FlowLayoutPanel cards=new(){Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,BackColor=Ui.Paper};
    private readonly PageHeading heading;
    private bool editing;
    internal SettingsView(ApiClient api):base(api)
    {
        Dock=DockStyle.Fill;heading=(PageHeading)Ui.PageHeader("Cài đặt cửa hàng","Bốn chính sách khách hàng cần biết.",Ui.Button("Làm mới",async(_,_)=>await RefreshAsync()));Controls.Add(Ui.Vertical(heading,Status,cards));cards.SizeChanged+=(_,_)=>ResizeCards();
    }
    public override Task RefreshAsync()=>LoadLatestAsync(t=>Api.GetAsync<JsonElement>("api/admin/data/settings?page=1&pageSize=20",t),data=>
    {
        var rows=Ui.Rows(data);foreach(Control c in cards.Controls.Cast<Control>().ToArray())c.Dispose();
        var commerce=rows.FirstOrDefault(x=>Ui.Text(x,"id","key")=="commerce");var currency=rows.FirstOrDefault(x=>Ui.Text(x,"id","key")=="exchangeRateVnd");
        if(commerce.ValueKind==JsonValueKind.Object)
        {
            var value=commerce.GetProperty("value");Add("shipping","Giao hàng",value,"Sửa phí giao hàng");Add("returns","Trả hàng",value,"Sửa thời hạn trả hàng");Add("points","Điểm thưởng",value,"Sửa cách tích và đổi điểm");
        }
        if(currency.ValueKind==JsonValueKind.Object)Add("currency","Quy đổi giá hiển thị",currency.GetProperty("value"),"Sửa tỷ giá");
        Status.Text=cards.Controls.Count==0?"Chưa có thiết lập cửa hàng.":"";Status.Visible=Status.Text.Length>0;ResizeCards();
    });
    private void Add(string kind,string title,JsonElement value,string action)
    {
        var card=new TableLayoutPanel{ColumnCount=1,RowCount=3,BackColor=Ui.Surface,Padding=new Padding(20),Margin=new Padding(0,0,16,16)};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.AutoSize));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label=Ui.Label(title,true);label.Font=new Font("Segoe UI Semibold",14);label.Margin=new Padding(0,0,0,10);label.Dock=DockStyle.Fill;
        var visual=new PolicyDiagram(kind,value){Dock=DockStyle.Fill,Name="policy_"+kind};var edit=Ui.Button(action,async(sender,_)=>await EditAsync(kind,(Control)sender!));edit.Name="edit_"+kind;edit.Margin=Padding.Empty;
        card.Controls.Add(label,0,0);card.Controls.Add(visual,0,1);card.Controls.Add(edit,0,2);cards.Controls.Add(card);
    }
    private void ResizeCards()
    {
        var scale=DeviceDpi/96f;var width=cards.ClientSize.Width;var columns=width/scale>=850?2:1;var target=(width-(int)(24*scale))/columns-(columns==2?(int)(8*scale):0);
        foreach(Control card in cards.Controls)card.Size=new Size(Math.Max(260,target),(int)(270*scale));heading.CompactDescription(ClientSize.Height/scale<600);
    }
    private async Task EditAsync(string kind,Control source)
    {
        if(editing)return;editing=true;using var busy=Ui.Busy(source,"Đang mở…");
        try
        {
            // Fetch the latest policy before editing one section. The unchanged
            // commerce fields are retained in Context, preserving the API contract.
            var data=await Api.GetAsync<JsonElement>("api/admin/data/settings?page=1&pageSize=20");if(IsDisposed||!Visible)return;
            var key=kind=="currency"?"exchangeRateVnd":"commerce";var row=Ui.Rows(data).FirstOrDefault(x=>Ui.Text(x,"id","key")==key);if(row.ValueKind!=JsonValueKind.Object)throw new InvalidOperationException("Thiết lập đã thay đổi. Tải lại trang để kiểm tra.");
            var sourceAction=ModuleActions.For("settings").Single(x=>x.Fields.Any(f=>f.Key==(key=="commerce"?"shippingVnd":"exchangeRateVnd")));
            var fields=kind switch{"shipping"=>new[]{"shippingVnd","freeShippingVnd","reason"},"returns"=>["returnDays","reason"],"points"=>["pointsPerVnd","pointValueVnd","maxPointsPercent","reason"],_=>["exchangeRateVnd","reason"]};
            var action=sourceAction with{Label=kind switch{"shipping"=>"Phí giao hàng","returns"=>"Thời hạn trả hàng","points"=>"Tích và đổi điểm thưởng",_=>"Tỷ giá hiển thị"},Fields=sourceAction.Fields.Where(f=>fields.Contains(f.Key)).ToArray()};
            using var form=new NativeCommandForm(Api,action,row,JsonSerializer.SerializeToElement(new{}));if(await form.ShowDialogAsync(FindForm()??(IWin32Window)this)==DialogResult.OK&&!IsDisposed)await RefreshAsync();
        }
        catch(Exception ex){if(!IsDisposed){Status.Visible=true;Status.Text=Ui.Error(ex);Status.ForeColor=Ui.Danger;}}
        finally{editing=false;}
    }
    internal override bool CanLeave()=>!editing;
}

internal sealed class PolicyDiagram(string kind,JsonElement value) : Control
{
    protected override void OnCreateControl(){base.OnCreateControl();DoubleBuffered=true;AccessibleRole=AccessibleRole.Graphic;AccessibleName=kind switch{"shipping"=>"Phí giao và ngưỡng miễn phí","returns"=>"Thời hạn trả hàng","points"=>"Cách tích và đổi điểm",_=>"Tỷ giá hiển thị"};AccessibleDescription=kind switch{"shipping"=>$"Phí {Money("shippingVnd")}. Miễn phí từ {Money("freeShippingVnd")}","returns"=>$"Thời hạn {Ui.Text(value,"returnDays")} ngày kể từ khi nhận hàng, theo điều kiện trả hàng.","points"=>$"Mỗi {Money("pointsPerVnd")} tích 1 điểm. 1 điểm đổi {Money("pointValueVnd")}. Giảm tối đa {Ui.Text(value,"maxPointsPercent")}% giá trị hàng.",_=>$"1 đô la Mỹ tương đương {Ui.Money(value.ToString())}. Giá và hóa đơn tính bằng đồng Việt Nam."};}
    private int S(float n)=>(int)(n*DeviceDpi/96f);
    private string Money(string key)=>Ui.Money(Ui.Text(value,key));
    private void TextAt(Graphics g,string text,float size,Color color,Rectangle area,bool bold=false,TextFormatFlags alignment=TextFormatFlags.Left)
    {using var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular);TextRenderer.DrawText(g,text,font,area,color,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|alignment);}
    private void Box(Graphics g,Rectangle r,Color color)
    {
        var radius=S(10);using var path=new GraphicsPath();path.AddArc(r.X,r.Y,radius*2,radius*2,180,90);path.AddArc(r.Right-radius*2,r.Y,radius*2,radius*2,270,90);path.AddArc(r.Right-radius*2,r.Bottom-radius*2,radius*2,radius*2,0,90);path.AddArc(r.X,r.Bottom-radius*2,radius*2,radius*2,90,90);path.CloseFigure();using var brush=new SolidBrush(color);g.FillPath(brush,path);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var width=ClientSize.Width;var half=(width-S(30))/2;using var line=new Pen(Ui.Primary,S(2));using var dot=new SolidBrush(Ui.Primary);
        if(kind=="shipping")
        {
            TextAt(g,Money("shippingVnd"),26,Ui.Primary,new Rectangle(0,0,width,S(46)),true);
            TextAt(g,"phí giao cho đơn dưới ngưỡng miễn phí",10,Ui.Muted,new Rectangle(0,S(45),width,S(24)));
            Box(g,new Rectangle(0,S(82),half,S(68)),Ui.Paper);Box(g,new Rectangle(half+S(30),S(82),half,S(68)),Ui.Soft);
            TextAt(g,"Dưới "+Money("freeShippingVnd"),10,Ui.Muted,new Rectangle(S(12),S(86),half-S(24),S(26)));
            TextAt(g,Money("shippingVnd"),14,Ui.Ink,new Rectangle(S(12),S(112),half-S(24),S(30)),true);
            TextAt(g,"Từ "+Money("freeShippingVnd"),10,Ui.Primary,new Rectangle(half+S(42),S(86),half-S(24),S(26)));
            TextAt(g,"Miễn phí",14,Ui.Primary,new Rectangle(half+S(42),S(112),half-S(24),S(30)),true);
        }
        else if(kind=="returns")
        {
            TextAt(g,Ui.Text(value,"returnDays")+" ngày",26,Ui.Primary,new Rectangle(0,0,width,S(46)),true);
            TextAt(g,"kể từ ngày khách nhận hàng",10,Ui.Muted,new Rectangle(0,S(45),width,S(24)));
            var y=S(102);var left=S(16);var right=width-S(24);g.DrawLine(line,left,y,right,y);g.FillEllipse(dot,left-S(5),y-S(5),S(10),S(10));g.FillEllipse(dot,right-S(5),y-S(5),S(10),S(10));
            TextAt(g,"Nhận hàng",10,Ui.Ink,new Rectangle(0,y+S(12),width/2,S(26)));TextAt(g,"Hết thời hạn",10,Ui.Ink,new Rectangle(width/2,y+S(12),width/2,S(26)),false,TextFormatFlags.Right);
        }
        else if(kind=="points")
        {
            TextAt(g,"Giảm tối đa "+Ui.Text(value,"maxPointsPercent")+"%",24,Ui.Primary,new Rectangle(0,0,width,S(44)),true);
            TextAt(g,"giá trị hàng khi dùng điểm",10,Ui.Muted,new Rectangle(0,S(44),width,S(24)));
            Box(g,new Rectangle(0,S(80),half,S(70)),Ui.Paper);Box(g,new Rectangle(half+S(30),S(80),half,S(70)),Ui.Soft);
            TextAt(g,"Chi tiêu "+Money("pointsPerVnd"),10,Ui.Muted,new Rectangle(S(12),S(84),half-S(24),S(26)));TextAt(g,"Tích 1 điểm",14,Ui.Ink,new Rectangle(S(12),S(110),half-S(24),S(30)),true);
            TextAt(g,"Đổi 1 điểm",10,Ui.Primary,new Rectangle(half+S(42),S(84),half-S(24),S(26)));TextAt(g,Money("pointValueVnd"),14,Ui.Primary,new Rectangle(half+S(42),S(110),half-S(24),S(30)),true);
            using var arrow=new Pen(Ui.Muted,S(1.5f));var x=half+S(8);var y=S(115);g.DrawLine(arrow,x,y,x+S(14),y);g.DrawLines(arrow,[new Point(x+S(9),y-S(4)),new Point(x+S(14),y),new Point(x+S(9),y+S(4))]);
        }
        else
        {
            Box(g,new Rectangle(0,S(10),half,S(92)),Ui.Paper);Box(g,new Rectangle(half+S(30),S(10),half,S(92)),Ui.Soft);
            TextAt(g,"Đô la Mỹ",10,Ui.Muted,new Rectangle(S(12),S(18),half-S(24),S(26)));TextAt(g,"1 USD",23,Ui.Ink,new Rectangle(S(12),S(48),half-S(24),S(44)),true);
            TextAt(g,"Đồng Việt Nam",10,Ui.Primary,new Rectangle(half+S(42),S(18),half-S(24),S(26)));TextAt(g,Ui.Money(value.ToString()),23,Ui.Primary,new Rectangle(half+S(42),S(48),half-S(24),S(44)),true);
            using var equals=new Pen(Ui.Muted,S(1.5f));g.DrawLine(equals,half+S(9),S(52),half+S(21),S(52));g.DrawLine(equals,half+S(9),S(58),half+S(21),S(58));
            TextAt(g,"Giá bán và hóa đơn vẫn tính bằng đồng Việt Nam.",10,Ui.Muted,new Rectangle(0,S(116),width,S(30)));
        }
    }
}
