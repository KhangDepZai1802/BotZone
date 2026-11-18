using System.Globalization;
using System.Text.Json;

namespace BotZone.Admin.WinForms;

internal enum FieldKind { Text, Note, Money, Integer, Boolean, Date, Choice, Lookup, MultiLookup, PurchaseLines, ReceiptLines, QuoteLines }
internal sealed record FieldSpec(string Key, string Label, FieldKind Kind = FieldKind.Text, bool Required = true, string? Lookup = null, Choice[]? Choices = null, object? Default = null);
internal sealed record ActionSpec(string Label, string Command, string[] Roles, Func<JsonElement, Dictionary<string, object?>> Context, FieldSpec[] Fields, Func<Dictionary<string, object?>, object>? Map = null);

internal sealed class NativeCommandForm : Form
{
    private readonly ApiClient api;
    private readonly ActionSpec action;
    private readonly Dictionary<string, object?> context;
    private readonly Dictionary<string, Control> inputs = [];
    private readonly ErrorProvider errors = new();
    private readonly CommandKeys keys;
    private readonly Dictionary<string, Label> fieldErrors = [];
    private readonly Dictionary<string,Control> fieldGroups=[];
    private readonly Dictionary<Control,TabPage> inputPages=[];
    private TabControl? formSections;
    private bool dirty, submitting;
    private readonly Label status = Ui.Label("Kiểm tra thông tin trước khi xác nhận.");
    private readonly Button submit;
    public bool Completed { get; private set; }
    internal JsonElement CommandResult{get;private set;}
    internal string SubmissionStatus=>status.Text;

    public NativeCommandForm(ApiClient api, ActionSpec action, JsonElement row, JsonElement lookups)
    {
        this.api = api;keys=api.PendingCommands; this.action = action; context = action.Context(row); Text = action.Label; StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(900, 700); MinimumSize = new Size(580, 400); errors.ContainerControl = this;errors.BlinkStyle=ErrorBlinkStyle.NeverBlink;
        var sections=new Dictionary<string,List<(Control Control,bool Wide)>>();
        var controlSections=new Dictionary<Control,string>();
        foreach (var spec in action.Fields)
        {
            var initial = context.TryGetValue(spec.Key, out var existing) ? existing : spec.Default;
            var control = Create(spec, initial, lookups,context);control.Name=spec.Key; control.AccessibleName = spec.Label; control.Dock = DockStyle.Top; inputs[spec.Key] = control;
            if(action.Command=="update_translation"&&spec.Key=="contentStatus"){control.Visible=false;Controls.Add(control);continue;}
            var group=(TableLayoutPanel)Ui.Field(spec.Kind==FieldKind.Boolean?"Trạng thái":spec.Label+(spec.Required?" *":""),control);group.Dock=DockStyle.Top;group.Margin=new Padding(0,0,20,12);group.BackColor=Color.White;
            var message=Ui.Label(" ");message.Font=new Font("Segoe UI",9);message.ForeColor=Ui.Danger;message.AccessibleName="Lỗi "+spec.Label;message.Margin=new Padding(0,4,0,0);message.Dock=DockStyle.Fill;fieldErrors[spec.Key]=message;group.RowCount=3;group.Controls.Add(message,0,2);
            var section=SectionFor(action,spec);if(!sections.ContainsKey(section))sections[section]=[];sections[section].Add((group,spec.Kind is FieldKind.Note or FieldKind.MultiLookup or FieldKind.PurchaseLines or FieldKind.ReceiptLines or FieldKind.QuoteLines||action.Fields.Length==1));fieldGroups[spec.Key]=group;controlSections[control]=section;
            WatchChanges(control);
        }
        Control fields;
        if(action.Fields.Length==0)
        {
            var groups=new List<(Control Control,bool Wide)>{(Ui.Label(ConfirmationText(action.Command,context)),true)};var amount=Ui.Text(row,"requested_amount_vnd","amount_vnd");if(amount.Length>0){var money=Ui.Label(Ui.Money(amount),true);money.Margin=new Padding(0,14,0,10);groups.Add((money,true));}fields=FieldArea(groups);Size=new Size(680,400);status.Text="Chỉ thực hiện sau khi chọn Xác nhận.";
        }
        else if(sections.Count>1)
        {
            formSections=Ui.Tabs();foreach(var section in sections){var page=new TabPage(section.Key);page.Controls.Add(FieldArea(section.Value));formSections.TabPages.Add(page);foreach(var item in controlSections.Where(x=>x.Value==section.Key))inputPages[item.Key]=page;}fields=formSections;Size=new Size(900,660);status.Text="Điền từng mục. Các trường có * là bắt buộc.";
        }
        else
        {
            fields=FieldArea(sections.Values.Single());if(action.Fields.Length<=4&&!action.Fields.Any(x=>x.Kind is FieldKind.PurchaseLines or FieldKind.ReceiptLines or FieldKind.QuoteLines or FieldKind.MultiLookup))Size=new Size(820,540);
        }
        if(action.Command=="create_promotion"&&inputs["targetKind"] is ComboBox target){void ShowTargets(){fieldGroups["productId"].Visible=Ui.Selected(target)=="product";fieldGroups["variantId"].Visible=Ui.Selected(target)=="variant";}target.SelectedIndexChanged+=(_,_)=>ShowTargets();ShowTargets();}
        if(action.Command=="process_warranty"&&inputs.TryGetValue("action",out var choice)&&choice is ComboBox step&&fieldGroups.ContainsKey("replacementUnitId")){void ShowReplacement(){fieldGroups["replacementUnitId"].Visible=fieldGroups["replacementLotId"].Visible=Ui.Selected(step)=="replace";}step.SelectedIndexChanged+=(_,_)=>ShowReplacement();ShowReplacement();}
        submit = Ui.Button(action.Command=="update_translation"?"Lưu nháp":action.Command.StartsWith("save_")||action.Command.StartsWith("update_")?"Lưu thay đổi":"&Xác nhận", async (_, _) => {if(action.Command=="update_translation")SetPublication("draft");await SubmitAsync();}, true); var cancel = Ui.Button("Hủy / Đóng", (_, _) => Close()); AcceptButton = submit; CancelButton = cancel;
        var buttons=Ui.Bar(submit,cancel);
        buttons.FlowDirection=FlowDirection.RightToLeft;status.ForeColor=Ui.Muted;status.Font=new Font("Segoe UI",9.5f);status.Margin=new Padding(0,0,0,10);
        if(action.Command=="update_translation")buttons.Controls.Add(Ui.Button("Xuất bản",async(sender,_)=>{if(submitting)return;if(MessageBox.Show(this,"Xuất bản nội dung sản phẩm ở ngôn ngữ đang chọn?","Xuất bản sản phẩm",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;SetPublication("published");using var busy=Ui.Busy((Control)sender!,"Đang xuất bản…");await SubmitAsync();}));
        var identity=Ui.Text(row,"order_no","code","sku","name","display_name","staff_code");
        var contextActions=new List<Control>();if(row.ValueKind==JsonValueKind.Object&&row.EnumerateObject().Any())contextActions.Add(Ui.Button("Xem hồ sơ",async(_,_)=>{using var detail=new RecordDetailForm(row,"Hồ sơ đang thao tác");await detail.ShowDialogAsync(this);}));
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,Padding=new Padding(20),BackColor=Ui.Paper };panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.Controls.Add(Ui.PageHeader(action.Label,identity,contextActions.ToArray()),0,0); panel.Controls.Add(fields, 0, 1); panel.Controls.Add(status, 0, 2); panel.Controls.Add(buttons, 0, 3);foreach(Control c in panel.Controls)c.Dock=DockStyle.Fill;Controls.Add(panel);
        FormClosing+=(_,e)=>{if(Completed)return;if(submitting){if(MessageBox.Show(this,"Thao tác đang gửi. Đóng cửa sổ không hủy giao dịch tại máy chủ. Sau đó hãy tải lại hồ sơ để kiểm tra kết quả.\n\nĐóng cửa sổ này?","Thao tác đang xử lý",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)e.Cancel=true;}else if(dirty&&MessageBox.Show(this,"Bỏ các thay đổi chưa gửi và đóng biểu mẫu?","Chưa lưu thay đổi",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)e.Cancel=true;};
        Ui.Prepare(this);
    }
    private static string SectionFor(ActionSpec action,FieldSpec field)=>action.Command switch
    {
        "create_variant"=>field.Key is "color" or "storage" or "ram" or "size" or "connectivity"?"Cấu hình":field.Key is "standardWarrantyMonths" or "careFeeVnd"?"Bảo hành":"SKU & giá",
        "create_product"=>field.Key is "generation" or "sourceUrl" or "poster"?"Thông số & ảnh":"Model",
        "create_promotion"=>field.Key is "targetKind" or "productId" or "variantId"?"Phạm vi":field.Key is "startsAt" or "endsAt" or "maxUses" or "maxUsesPerCustomer"?"Thời hạn & lượt":"Mức giảm",
        "update_settings" when action.Fields.Length>4=>field.Key is "shippingVnd" or "freeShippingVnd" or "returnDays"?"Giao hàng & trả hàng":"Điểm thưởng & lý do",
        _=>"Thông tin"
    };
    private static string ConfirmationText(string command,Dictionary<string,object?> context)=>command switch
    {
        "confirm_order"=>"Xác nhận đơn để chuyển sang chuẩn bị hàng.","cancel_order"=>"Hủy đơn trước bàn giao. Khoản đã thu được xử lý qua bước hoàn tiền riêng.","handoff"=>"Ghi nhận bàn giao hàng. Tồn kho và chứng từ được cập nhật khi máy chủ xác nhận.","approve_purchase"=>"Duyệt phiếu nhập để kho có thể nhận hàng.","shipment_event"=>"Cập nhật giao nhận: "+OrdersView.StateLabel(context.GetValueOrDefault("status")?.ToString()??"")+".","execute_refund"=>"Thực hiện hoàn theo hồ sơ đã được duyệt. Kết quả và số tiền thực tế được máy chủ đối chiếu.",_=>"Xác nhận thực hiện thao tác này cho hồ sơ đã chọn."
    };
    private static TableLayoutPanel FieldArea(List<(Control Control,bool Wide)> groups)
    {
        var fields=new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=1,Padding=new Padding(20),BackColor=Color.White,Margin=new Padding(0,0,0,12)};foreach(var group in groups)fields.Controls.Add(group.Control);var columns=0;
        void Arrange(){var next=fields.ClientSize.Width*96/Math.Max(96,fields.DeviceDpi)>=620?2:1;if(columns==next)return;columns=next;fields.SuspendLayout();fields.ColumnCount=next;fields.ColumnStyles.Clear();for(var i=0;i<next;i++)fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/next));fields.RowStyles.Clear();var rowIndex=0;var col=0;foreach(var group in groups){var span=group.Wide?next:1;if(span>1&&col>0){rowIndex++;col=0;}fields.SetColumnSpan(group.Control,span);fields.SetCellPosition(group.Control,new TableLayoutPanelCellPosition(col,rowIndex));group.Control.Margin=new Padding(0,0,col==0&&span==1&&next==2?20:0,12);col+=span;if(col>=next){rowIndex++;col=0;}}fields.RowCount=rowIndex+(col>0?1:0)+1;for(var i=0;i<fields.RowCount-1;i++)fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));fields.RowStyles.Add(new RowStyle(SizeType.Percent,100));fields.ResumeLayout();}
        fields.SizeChanged+=(_,_)=>Arrange();fields.VisibleChanged+=(_,_)=>Arrange();Arrange();return fields;
    }
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
    private void SetPublication(string value){if(inputs.TryGetValue("contentStatus",out var control)&&control is ComboBox combo)for(var i=0;i<combo.Items.Count;i++)if((combo.Items[i] as Choice)?.Value==value)combo.SelectedIndex=i;}
    private void WatchChanges(Control control)
    {
        if(control is TextBox text)text.TextChanged+=(_,_)=>dirty=true;
        else if(control is ComboBox combo)combo.SelectedIndexChanged+=(_,_)=>dirty=true;
        else if(control is CheckBox check)check.CheckedChanged+=(_,_)=>dirty=true;
        else if(control is NumericUpDown number)number.ValueChanged+=(_,_)=>dirty=true;
        else if(control is DateTimePicker date)date.ValueChanged+=(_,_)=>dirty=true;
        else if(control is CheckedListBox list)list.ItemCheck+=(_,_)=>dirty=true;
        else if(control is MultiLookupPicker picker)picker.Changed+=(_,_)=>dirty=true;
        else if(control is LinesEditor lines)lines.Changed+=(_,_)=>dirty=true;
    }
    private static Control Create(FieldSpec spec, object? initial, JsonElement lookups,Dictionary<string,object?> context)
    {
        if (spec.Kind is FieldKind.PurchaseLines or FieldKind.ReceiptLines or FieldKind.QuoteLines) return new LinesEditor(spec.Kind, lookups,context);
        if(spec.Kind==FieldKind.MultiLookup)
        {
            var lot=context.GetValueOrDefault("lotId")?.ToString();
            var choices=Ui.Rows(lookups,spec.Lookup??"inventoryUnits").Where(row=>string.IsNullOrEmpty(lot)||Ui.Text(row,"lotId","lot_id")==lot).Select(row=>new Choice(Ui.Text(row,"id"),Ui.Text(row,"name","label","id")));
            return new MultiLookupPicker(choices,initial as IEnumerable<string>);
        }
        if (spec.Kind == FieldKind.Boolean) return new CheckBox { AutoSize = true, Text = spec.Label, Checked = initial is true || initial?.ToString() == "true" };
        if (spec.Kind == FieldKind.Integer) return new NumericUpDown { Minimum = spec.Key == "quantityDelta" ? -100000 : 0, Maximum = 100000, Value = decimal.TryParse(initial?.ToString(), out var i) ? i : 1, Width = 200 };
        if (spec.Kind == FieldKind.Date) return new BzDateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = DateTime.TryParse(initial?.ToString(), out var d) ? d : DateTime.Now.AddDays(7) };
        if (spec.Kind is FieldKind.Choice or FieldKind.Lookup)
        {
            var c = Ui.Combo(); if (!spec.Required) c.Items.Add(new Choice("", "Không chọn"));
            if (spec.Choices is not null) c.Items.AddRange(spec.Choices.Cast<object>().ToArray());
            if (spec.Lookup is not null)
            {
                var rows=Ui.Rows(lookups,spec.Lookup).AsEnumerable();if(spec.Lookup is "tradeInItems" or "quoteItems" or "tradeInQuotes"&&context.TryGetValue("requestId",out var request))rows=rows.Where(x=>Ui.Text(x,"requestId","request_id")==request?.ToString());
                if(spec.Key=="replacementUnitId")rows=rows.Where(x=>Ui.Text(x,"custody_status","custodyStatus")=="available"&&Ui.Text(x,"condition_status","conditionStatus")=="new");
                if(spec.Key is "replacementUnitId" or "replacementLotId"&&context.TryGetValue("variantId",out var variant)&&!string.IsNullOrWhiteSpace(variant?.ToString()))rows=rows.Where(x=>Ui.Text(x,"variantId","variant_id")==variant.ToString());
                foreach (var item in rows) c.Items.Add(new Choice(Ui.Text(item, "id", "userId"), Ui.Text(item, "name", "label", "sku", "email", "code")));
            }
            c.SelectedIndex = c.Items.Count > 0 ? 0 : -1; for (var j = 0; j < c.Items.Count; j++) if ((c.Items[j] as Choice)?.Value == initial?.ToString()) c.SelectedIndex = j; return c;
        }
        return new TextBox { Text = initial?.ToString() ?? "",TextAlign=spec.Kind==FieldKind.Money?HorizontalAlignment.Right:HorizontalAlignment.Left,PlaceholderText=spec.Kind==FieldKind.Money?"0":"", Multiline = spec.Kind == FieldKind.Note,AcceptsReturn=spec.Kind==FieldKind.Note, Height = spec.Kind == FieldKind.Note ? 100 : 30, ScrollBars = spec.Kind == FieldKind.Note ? ScrollBars.Vertical : ScrollBars.None };
    }
    private async Task SubmitAsync()
    {
        if(submitting)return;
        errors.Clear();foreach(var label in fieldErrors.Values)label.Text=" "; var data = new Dictionary<string, object?>(context); var valid = true;Control? firstError=null;
        foreach (var field in action.Fields)
        {
            var control = inputs[field.Key];
            try
            {
                object? value = control switch { LinesEditor lines => lines.Values(),MultiLookupPicker picker=>picker.SelectedValues,CheckedListBox list=>list.CheckedItems.Cast<Choice>().Select(x=>x.Value).ToArray(), CheckBox check => check.Checked, NumericUpDown number => decimal.ToInt32(number.Value), DateTimePicker date => new DateTimeOffset(DateTime.SpecifyKind(date.Value,DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToString("O"), ComboBox combo => Ui.Selected(combo), TextBox text => text.Text.Trim(), _ => null };
                if (field.Required && (value is null || value is string s && string.IsNullOrWhiteSpace(s))) throw new InvalidOperationException("Vui lòng nhập/chọn " + field.Label.ToLowerInvariant() + ".");
                if (field.Kind == FieldKind.Money && value is string money && money.Length > 0 && (!long.TryParse(money, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount < 0)) throw new InvalidOperationException("Nhập số nguyên VND không âm, không dấu phân cách.");
                if (!field.Required && value is string empty && empty.Length == 0) value = null;
                data[field.Key] = value;
            }
            catch (Exception ex) { errors.SetError(control, ex.Message);if(fieldErrors.TryGetValue(field.Key,out var label))label.Text=ex.Message;firstError??=control;valid = false; }
        }
        if (!valid) { status.Text = "Kiểm tra các trường được đánh dấu. Nội dung đã nhập được giữ nguyên.";status.ForeColor=Ui.Danger;if(firstError is not null){if(inputPages.TryGetValue(firstError,out var page)&&formSections is not null)formSections.SelectedTab=page;firstError.Focus();for(Control? parent=firstError.Parent;parent is not null;parent=parent.Parent)if(parent is ScrollableControl scroll&&scroll.AutoScroll)scroll.ScrollControlIntoView(firstError);if(firstError is LinesEditor lines)lines.FocusError();}return; }
        object input;try{input=action.Map?.Invoke(data)??data;}catch(Exception ex){status.Text=Ui.Error(ex);status.ForeColor=Ui.Danger;return;}
        submitting=true;using var busy=Ui.Busy(submit,action.Command=="update_translation"&&data.GetValueOrDefault("contentStatus")?.ToString()=="published"?"Đang xuất bản…":"Đang lưu…");status.ForeColor=Ui.Muted;status.Text = "Đang gửi, vui lòng chờ xác nhận…";
        foreach(var control in inputs.Values)control.Enabled=false;
        // Closing the dialog does not cancel a financial/inventory write or imply that the server rolled it back.
        try { CommandResult=await api.CommandAsync(action.Command, input, keys.For(action.Command, input)); keys.Succeeded(action.Command, input); Completed = true;dirty=false;if(!IsDisposed){DialogResult = DialogResult.OK; Close();} }
        catch (Exception ex) {if(!IsDisposed){status.Text = Ui.Error(ex)+(ex is HttpRequestException or OperationCanceledException?" Chưa xác định kết quả ghi. Kiểm tra lại hồ sơ trước khi gửi tiếp.":""); status.ForeColor = Ui.Danger;} }
        finally {submitting=false; if (!IsDisposed) foreach(var control in inputs.Values)control.Enabled=true; }
    }
}

internal sealed class LinesEditor : UserControl
{
    private readonly DataGridView grid = Ui.Grid();
    private readonly FieldKind kind;
    private readonly TextBox scan=new(){Width=320,PlaceholderText="Quét hoặc nhập serial / IMEI…",AccessibleName="Quét serial vào dòng hàng đang chọn"};
    private readonly Label feedback=Ui.Label("Chọn dòng hàng trước khi nhập. Enter ghi nhận mã vào dòng, không gửi phiếu.");
    private readonly Dictionary<string,int> ordered=[];
    private readonly List<Choice> choices;
    public event EventHandler? Changed;
    public LinesEditor(FieldKind kind, JsonElement lookups,Dictionary<string,object?> context)
    {
        this.kind = kind; Height = kind==FieldKind.ReceiptLines?380:280; MinimumSize = new Size(400, 220); grid.ReadOnly = false; grid.AllowUserToAddRows = true; grid.AllowUserToDeleteRows = true; grid.SelectionMode = DataGridViewSelectionMode.CellSelect;grid.AccessibleName="Các dòng hàng";grid.RowHeadersVisible=true;grid.RowHeadersWidth=30;
        var key = kind == FieldKind.PurchaseLines ? "variants" : kind == FieldKind.ReceiptLines ? "purchaseItems" : "tradeInItems";
        var lookupRows=Ui.Rows(lookups,key).AsEnumerable();if(kind==FieldKind.ReceiptLines&&context.TryGetValue("purchaseId",out var purchaseId))lookupRows=lookupRows.Where(x=>Ui.Text(x,"purchaseOrderId","purchase_order_id")==purchaseId?.ToString());if(kind==FieldKind.QuoteLines&&context.TryGetValue("requestId",out var requestId))lookupRows=lookupRows.Where(x=>Ui.Text(x,"requestId","request_id")==requestId?.ToString());
        var items=lookupRows.ToList();choices = items.Select(x => new Choice(Ui.Text(x, "id"), Ui.Text(x, "name", "label", "sku", "internalCode"))).ToList();
        foreach(var item in items)if(int.TryParse(Ui.Text(item,"quantity"),out var quantity))ordered[Ui.Text(item,"id")]=quantity;
        grid.Columns.Add(new BzGridComboBoxColumn { Name = "item", HeaderText = kind == FieldKind.PurchaseLines ? "SKU" : kind == FieldKind.ReceiptLines ? "SKU / dòng nhận" : "Món thu cũ", DataSource = choices, DisplayMember = nameof(Choice.Label), ValueMember = nameof(Choice.Value), MinimumWidth = 220,Width=240,Frozen=true,AutoSizeMode=DataGridViewAutoSizeColumnMode.None,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton,FlatStyle=FlatStyle.Flat });
        if(kind==FieldKind.ReceiptLines)grid.Columns.Add(new DataGridViewTextBoxColumn{Name="ordered",HeaderText="Đặt",Width=80,ReadOnly=true});
        if (kind != FieldKind.QuoteLines) grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "quantity", HeaderText = kind==FieldKind.ReceiptLines?"Nhận lần này":"Số lượng", Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "value", HeaderText = kind == FieldKind.ReceiptLines ? "Serial (phân tách dấu phẩy)" : kind == FieldKind.QuoteLines ? "Giá thu VND" : "Giá vốn VND", MinimumWidth = 200 });
        var valueColumn=grid.Columns["value"]!;valueColumn.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        if(grid.Columns["quantity"] is {} quantityColumn){quantityColumn.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;quantityColumn.HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleCenter;}
        if(kind!=FieldKind.ReceiptLines){valueColumn.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;valueColumn.HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleRight;}
        grid.DataError += (_, e) => {e.ThrowException=false;if(e.RowIndex>=0)grid.Rows[e.RowIndex].ErrorText="Chọn giá trị hợp lệ từ danh sách.";feedback.Text="Kiểm tra dòng được đánh dấu; dữ liệu đang nhập được giữ nguyên.";feedback.ForeColor=Ui.Danger;};
        grid.CellValueChanged+=(_,e)=>{if(e.RowIndex>=0&&e.ColumnIndex>=0&&kind==FieldKind.ReceiptLines&&grid.Columns[e.ColumnIndex].Name=="item"){var row=grid.Rows[e.RowIndex];row.Cells["ordered"].Value=ordered.GetValueOrDefault(Convert.ToString(row.Cells["item"].Value)??"");}Changed?.Invoke(this,EventArgs.Empty);};
        grid.RowsRemoved+=(_,_)=>Changed?.Invoke(this,EventArgs.Empty);
        var add=Ui.Button("Thêm dòng",async(_,_)=>await AddLineAsync());add.Name="addLine";var remove=Ui.Button("Xóa dòng",(_,_)=>RemoveLine());remove.Name="removeLine";
        var lineActions=Ui.Bar(add,remove);lineActions.BackColor=Ui.Surface;
        grid.SelectionChanged+=(_,_)=>remove.Enabled=grid.CurrentRow is {IsNewRow:false};remove.Enabled=false;
        if(kind==FieldKind.ReceiptLines)
        {
            var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var record=Ui.Button("Ghi nhận mã",(_,_)=>Scan());record.Name="recordSerial";
            content.Controls.Add(lineActions,0,0);content.Controls.Add(Ui.Bar(Ui.FrameInput(scan),record),0,1);content.Controls.Add(feedback,0,2);content.Controls.Add(grid,0,3);Controls.Add(content);
            scan.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Scan();}};
        }
        else {var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.Controls.Add(lineActions,0,0);content.Controls.Add(grid,0,1);feedback.Text="";content.Controls.Add(feedback,0,2);Controls.Add(content);}
    }
    private async Task AddLineAsync()
    {
        grid.EndEdit();using var picker=new OperationChoiceForm(kind==FieldKind.QuoteLines?"Chọn món thu cũ":"Chọn dòng hàng",choices);
        if(await picker.ShowDialogAsync(FindForm()??(IWin32Window)this)!=DialogResult.OK||picker.Selected is not {} choice||IsDisposed)return;
        var index=kind==FieldKind.ReceiptLines?grid.Rows.Add(choice.Value,ordered.GetValueOrDefault(choice.Value),1,""):kind==FieldKind.QuoteLines?grid.Rows.Add(choice.Value,""):grid.Rows.Add(choice.Value,1,"");
        grid.CurrentCell=grid.Rows[index].Cells[kind==FieldKind.QuoteLines?"value":"quantity"];grid.Focus();grid.BeginEdit(true);Changed?.Invoke(this,EventArgs.Empty);
    }
    private void RemoveLine(){grid.EndEdit();if(grid.CurrentRow is {IsNewRow:false} row)grid.Rows.Remove(row);}
    internal void FocusError()=>grid.Focus();
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Control|Keys.Insert)){_=AddLineAsync();return true;}return base.ProcessCmdKey(ref msg,keyData);}
    private void Scan()
    {
        grid.EndEdit();var row=grid.CurrentRow;var value=scan.Text.Trim();
        if(row is null||row.IsNewRow||string.IsNullOrWhiteSpace(Convert.ToString(row.Cells["item"].Value))){feedback.Text="Chọn dòng hàng cần nhận trước khi quét.";feedback.ForeColor=Ui.Danger;scan.Focus();return;}
        if(value.Length==0)return;
        var duplicate=grid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).SelectMany(r=>(Convert.ToString(r.Cells["value"].Value)??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries)).Contains(value,StringComparer.OrdinalIgnoreCase);
        if(duplicate){feedback.Text="Mã đã có trong phiếu: "+value;feedback.ForeColor=Ui.Danger;scan.SelectAll();scan.Focus();return;}
        var existing=Convert.ToString(row.Cells["value"].Value)??"";row.Cells["value"].Value=existing.Length==0?value:existing+", "+value;
        feedback.Text="Đã ghi nhận "+value+". Kiểm tra số lượng Nhận lần này trước khi xác nhận.";feedback.ForeColor=Ui.Success;scan.Clear();scan.Focus();Ui.Pulse(scan);
    }
    public List<Dictionary<string, object?>> Values()
    {
        grid.EndEdit(); var values = new List<Dictionary<string, object?>>();
        var serials=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            row.ErrorText="";
            var errorCell="item";
            try{
            if (row.IsNewRow) continue; var id = Convert.ToString(row.Cells["item"].Value) ?? ""; if (!Guid.TryParse(id, out _)) throw new InvalidOperationException("Mỗi dòng cần chọn một món/SKU hợp lệ.");
            var text = Convert.ToString(row.Cells["value"].Value)?.Trim() ?? "";
            if (kind == FieldKind.QuoteLines) { errorCell="value";if (!long.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture, out var price) || price <= 0) throw new InvalidOperationException("Giá thu phải là VND nguyên lớn hơn 0."); values.Add(new() { ["itemId"] = id, ["offeredVnd"] = price.ToString(CultureInfo.InvariantCulture) }); continue; }
            errorCell="quantity";
            if (!int.TryParse(Convert.ToString(row.Cells["quantity"].Value), out var quantity) || quantity <= 0) throw new InvalidOperationException("Số lượng từng dòng phải lớn hơn 0.");
            if (kind == FieldKind.ReceiptLines) {errorCell="value";var codes=text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);foreach(var code in codes)if(!serials.Add(code))throw new InvalidOperationException("Serial bị trùng trong phiếu: "+code);errorCell="quantity";if(ordered.TryGetValue(id,out var planned)&&quantity>planned)throw new InvalidOperationException("Nhận lần này không được vượt số lượng đặt. Phần đã nhận trước sẽ được kiểm tra khi xác nhận.");values.Add(new() { ["purchaseOrderItemId"] = id, ["quantity"] = quantity, ["serials"] = codes });}
            else { errorCell="value";if (!long.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture, out var cost) || cost < 0) throw new InvalidOperationException("Giá vốn phải là VND nguyên không âm."); values.Add(new() { ["variantId"] = id, ["quantity"] = quantity, ["unitCostVnd"] = cost.ToString(CultureInfo.InvariantCulture) }); }
            }catch(Exception ex){row.ErrorText=ex.Message;grid.CurrentCell=row.Cells[errorCell];grid.Focus();feedback.Text=ex.Message;feedback.ForeColor=Ui.Danger;throw;}
        }
        if (values.Count == 0) throw new InvalidOperationException("Thêm ít nhất một dòng hàng."); feedback.Text=$"{values.Count} dòng đã kiểm tra.";feedback.ForeColor=Ui.Muted;return values;
    }
}
