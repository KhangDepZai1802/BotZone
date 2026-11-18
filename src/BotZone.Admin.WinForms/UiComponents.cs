using System.Globalization;

namespace BotZone.Admin.WinForms;

internal static partial class Ui
{
    internal static string DisplayName(string key) => Headers.GetValueOrDefault(key,key);
    internal static Control? FocusedButton(Control parent) { foreach(Control c in parent.Controls){if(c is Button && c.Focused)return c;var nested=FocusedButton(c);if(nested is not null)return nested;}return null; }
    public static Control PageHeader(string title,string subtitle,params Control[] actions)
    {
        return new PageHeading(title,subtitle,actions);
    }
    public static Control Field(string label,Control input)
    {
        var p=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,RowCount=2,Width=Math.Max(120,input.Width),MinimumSize=new Size(Math.Max(120,input.Width),0),Margin=new Padding(0,0,12,8)};p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));p.RowStyles.Add(new RowStyle(SizeType.AutoSize));p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var caption=Label(label);caption.UseMnemonic=true;caption.Font=new Font("Segoe UI",9.5f,FontStyle.Regular);caption.ForeColor=Muted;caption.TabIndex=0;input.TabIndex=1;input.AccessibleName=label.Replace("&","");input.Dock=DockStyle.Fill;p.Controls.Add(caption,0,0);var framed=FrameInput(input);framed.Dock=DockStyle.Fill;p.Controls.Add(framed,0,1);return p;
    }
    public static TabControl Tabs()=>new BzTabControl{Dock=DockStyle.Fill};
    public static IDisposable Busy(Control control,string message)=>new BusyScope(control,message);
    public static void Pulse(Control control)=>Motion.Pulse(control);
    public static void Prepare(Form form)
    {
        form.BackColor=Paper;form.ForeColor=Ink;form.Font=new Font("Segoe UI",10.5f);form.AutoScaleMode=AutoScaleMode.Dpi;form.Icon=BrandMark.WindowIcon;
        form.Shown+=(_,_)=>{Enhance(form);FitWindow(form);Pulse(form);};
    }
    internal static void FitWindow(Form form)
    {
        if(!form.TopLevel)return;var area=Screen.FromControl(form).WorkingArea;
        form.MinimumSize=new Size(Math.Min(form.MinimumSize.Width,area.Width),Math.Min(form.MinimumSize.Height,area.Height));
        if(form.Width>area.Width||form.Height>area.Height)form.Size=new Size(Math.Min(form.Width,area.Width),Math.Min(form.Height,area.Height));
        if(form.Right>area.Right||form.Bottom>area.Bottom||form.Left<area.Left||form.Top<area.Top)form.Location=new Point(area.Left+Math.Max(0,(area.Width-form.Width)/2),area.Top+Math.Max(0,(area.Height-form.Height)/2));
    }
    internal static void Enhance(Control parent)
    {
        foreach(Control c in parent.Controls){if(c is TextBoxBase or ComboBox or NumericUpDown or DateTimePicker or CheckBox){if(string.IsNullOrWhiteSpace(c.AccessibleName)){var previous=parent.Controls.Cast<Control>().Where(x=>x.TabIndex<c.TabIndex).OrderByDescending(x=>x.TabIndex).FirstOrDefault();c.AccessibleName=previous is Label?previous.Text.Replace("&",""):!string.IsNullOrWhiteSpace(c.Text)?c.Text:"Giá trị";}}Enhance(c);}
    }
    private static DataGridView CreateGrid()
    {
        var g=new BzDataGrid{Dock=DockStyle.Fill,AutoGenerateColumns=false,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,RowHeadersVisible=false,BackgroundColor=Surface,BorderStyle=BorderStyle.None,GridColor=Line,CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal,ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.EnableResizing,ColumnHeadersHeight=40,RowTemplate={Height=48},EnableHeadersVisualStyles=false,Font=new Font("Segoe UI",10.5f),AccessibleName="Bảng dữ liệu",ColumnHeadersDefaultCellStyle=new(){BackColor=Paper,ForeColor=Muted,Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(10,5,10,5),WrapMode=DataGridViewTriState.False},DefaultCellStyle=new(){BackColor=Surface,ForeColor=Ink,SelectionBackColor=Soft,SelectionForeColor=Ink,Padding=new Padding(10,6,10,6)}};
        g.DataBindingComplete+=(_,_)=>{foreach(DataGridViewColumn c in g.Columns){if(Headers.TryGetValue(c.DataPropertyName,out var title)&&c.HeaderText==c.DataPropertyName)c.HeaderText=title;if(c.Frozen)c.AutoSizeMode=DataGridViewAutoSizeColumnMode.DisplayedCells;else if(c.AutoSizeMode!=DataGridViewAutoSizeColumnMode.None)c.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;if(c.FillWeight==100)c.FillWeight=Math.Max(70,c.MinimumWidth);if(c.DataPropertyName.EndsWith("vnd",StringComparison.OrdinalIgnoreCase)){c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;c.HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleRight;c.MinimumWidth=Math.Max(140,c.MinimumWidth);}else if(c.DataPropertyName.EndsWith("Count",StringComparison.OrdinalIgnoreCase)||c.DataPropertyName.Equals("quantity",StringComparison.OrdinalIgnoreCase)||c.DataPropertyName.StartsWith("qty_")){c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;c.HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleCenter;}}};
        g.CellFormatting+=(_,e)=>{if(e.ColumnIndex<0||e.Value is null)return;var name=g.Columns[e.ColumnIndex].DataPropertyName;var text=Convert.ToString(e.Value,CultureInfo.InvariantCulture)??"";if(name.EndsWith("vnd",StringComparison.OrdinalIgnoreCase)&&decimal.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var money)){e.Value=Money(text);e.FormattingApplied=true;}else if(name.EndsWith("_at")&&DateTimeOffset.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date)){e.Value=VietDate(date);e.FormattingApplied=true;}};
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor=Paper;g.ColumnHeadersDefaultCellStyle.SelectionForeColor=Muted;
        g.CellFormatting+=(_,e)=>{if(e.ColumnIndex<0||e.Value is not string state)return;var property=g.Columns[e.ColumnIndex].DataPropertyName.ToLowerInvariant();if(property.Contains("status")||property=="state"){e.Value=StatusText(state);e.FormattingApplied=true;}};
        g.CellPainting+=PaintStatusCell;
        return g;
    }
    private static readonly Dictionary<string,string> Headers=new(StringComparer.OrdinalIgnoreCase){["id"]="Mã định danh",["order_no"]="Mã đơn",["order_status"]="Xử lý đơn",["payment_method"]="Thanh toán",["created_at"]="Ngày tạo",["updated_at"]="Cập nhật",["name"]="Tên",["code"]="Mã",["sku"]="SKU",["model_code"]="Mã sản phẩm",["business_status"]="Trạng thái bán",["price_vnd"]="Giá bán",["total_vnd"]="Tổng VND",["amount_vnd"]="Số tiền",["unit_cost_vnd"]="Giá vốn",["status"]="Trạng thái",["state"]="Trạng thái",["active"]="Hoạt động",["role"]="Vai trò",["email"]="Email",["phone"]="Điện thoại",["name_vi"]="Tên tiếng Việt",["name_en"]="Tên tiếng Anh",["slug"]="Đường dẫn",["staff_code"]="Mã nhân viên",["display_name"]="Họ tên",["qty_on_hand"]="Tồn sổ sách",["qty_reserved"]="Đang giữ",["available"]="Khả dụng",["quarantine"]="Chờ kiểm tra",["quantity"]="Số lượng",["purchase_no"]="Mã phiếu nhập",["supplier_name"]="Nhà cung cấp",["received_at"]="Ngày nhận",["custody_status"]="Lưu kho",["condition_status"]="Tình trạng hàng",["internal_code"]="Mã tiếp nhận",["reason"]="Lý do",["mime"]="Loại tệp",["size_bytes"]="Dung lượng (byte)",["visibility"]="Phạm vi",["validation_status"]="Kiểm tra",["source_url"]="Nguồn",["license_note"]="Quyền sử dụng",["key"]="Thiết lập",["value"]="Giá trị",["version"]="Phiên bản",["action"]="Thao tác",["entity_type"]="Đối tượng",["actor_id"]="Người thao tác",["count"]="Số lượng",["contact"]="Liên hệ"};
}
