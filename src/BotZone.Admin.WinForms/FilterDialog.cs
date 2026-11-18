using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class FilterDialog : Form
{
    private readonly TreeView tree = new() { Dock = DockStyle.Fill, HideSelection = false, ShowNodeToolTips = true, BorderStyle = BorderStyle.None, FullRowSelect = true, ItemHeight = 38, Indent = 24, AccessibleName = "Cây điều kiện hóa đơn" };
    private readonly ComboBox field = Ui.Combo(new("grandTotalVnd", "Tổng hóa đơn"), new("discountTotalVnd", "Tiền giảm"), new("invoiceNo", "Mã hóa đơn"), new("issuedAt", "Ngày phát hành"), new("responsibleStaffId", "Nhân viên phụ trách"), new("customerId", "Khách hàng"), new("lifecycle", "Vòng đời"), new("paymentStatus", "Tình trạng thanh toán"), new("product", "Sản phẩm có / không có"));
    private readonly ComboBox op = Ui.Combo(new("=", "Bằng (=)"), new("<>", "Khác (<>)"), new(">", "Lớn hơn (>)"), new("<", "Nhỏ hơn (<)"), new(">=", "Lớn hơn hoặc bằng (>=)"), new("<=", "Nhỏ hơn hoặc bằng (<=)"), new("isNull", "Trống (NULL)"), new("notNull", "Có dữ liệu"));
    private readonly TextBox value = new() { Dock = DockStyle.Fill };
    private readonly DateTimePicker date = new BzDateTimePicker() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Dock = DockStyle.Fill };
    private readonly ComboBox options = Ui.Combo();
    private readonly ComboBox logic = Ui.Combo(new("AND", "Tất cả điều kiện (AND)"), new("OR", "Ít nhất một điều kiện (OR)"));
    private readonly Label help = Ui.Label("Điều kiện được nhóm bằng ngoặc. Quyền truy cập luôn do API giới hạn.");
    private readonly Label summary = Ui.Label("Chưa có điều kiện. Áp dụng để xem tất cả trong phạm vi được phép.");
    private readonly ErrorProvider errors = new();
    private readonly Dictionary<string, List<Choice>> choices = [];
    private readonly Control logicEditor, fieldEditor, operatorEditor, valueEditor, dateEditor, optionsEditor;
    private bool syncing;
    public FilterNode? Filter { get; private set; }

    public FilterDialog(FilterNode? filter, JsonElement lookups)
    {
        Text = "Bộ lọc nâng cao — Hóa đơn"; AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent; Size = new Size(1060, 700); MinimumSize = new Size(680, 480); errors.ContainerControl = this; errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;
        foreach (var pair in new[] { ("staff", "responsibleStaffId"), ("customers", "customerId"), ("products", "product") }) choices[pair.Item2] = Ui.Rows(lookups, pair.Item1).Select(x => new Choice(Ui.Text(x, "id"), Ui.Text(x, "name", "displayName", "email"))).ToList();
        choices["lifecycle"] = [new("issued", "Đã phát hành"), new("draft", "Bản nháp")];
        choices["paymentStatus"] = [new("unpaid", "Chưa thu"), new("partially_paid", "Thu một phần"), new("paid", "Đã thu đủ"), new("refunded", "Đã hoàn"), new("partially_refunded", "Hoàn một phần")];
        var root = filter is null ? new TreeNode("Tất cả điều kiện (AND)") { Tag = new EditNode("group") } : FromFilter(filter); tree.Nodes.Add(root); tree.ExpandAll();
        SetNodeLabels(root);
        var addRule = Ui.Button("+ Điều &kiện", (_, _) => Add("rule")); var addGroup = Ui.Button("+ &Nhóm", (_, _) => Add("group"));
        var remove = Ui.Button("&Xóa", (_, _) => Remove());
        var extra=Ui.MenuButton("Thêm",("Phủ định NOT",Negate),("Xóa bộ lọc",()=>{tree.Nodes.Clear();tree.Nodes.Add(new TreeNode("Tất cả điều kiện (AND)"){Tag=new EditNode("group")});tree.SelectedNode=tree.Nodes[0];RefreshDescription();}),("Hướng dẫn",()=>Ui.ShowInformation(this,"Hướng dẫn bộ lọc","Tất cả (AND): mọi điều kiện đều đúng.\nÍt nhất một (OR): có ít nhất một điều kiện đúng.\nPhủ định (NOT): đảo kết quả điều kiện hoặc nhóm.\n\nTối đa 4 cấp và 30 điều kiện. Trường trống dùng NULL.\nSố tiền VND nhập số nguyên, không dùng dấu phân cách.")));
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 }; left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); left.Controls.Add(Ui.Bar(addRule, addGroup, remove, extra), 0, 0); left.Controls.Add(tree, 0, 1);
        var edit = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount=0, AutoScroll = true, Padding = new Padding(14) };edit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        logicEditor=Ui.Field("Quan hệ nhóm",logic);fieldEditor=Ui.Field("Trường dữ liệu",field);operatorEditor=Ui.Field("Toán tử so sánh",op);valueEditor=Ui.Field("Giá trị",value);dateEditor=Ui.Field("Ngày lịch Việt Nam",date);optionsEditor=Ui.Field("Giá trị",options);
        var editorTitle=Ui.Label("Điều kiện đang chọn");editorTitle.Font=new Font("Segoe UI",13,FontStyle.Bold);
        foreach (var control in new Control[] { editorTitle,logicEditor,fieldEditor,operatorEditor,valueEditor,dateEditor,optionsEditor,help }) { control.Dock = DockStyle.Top; edit.RowStyles.Add(new RowStyle(SizeType.AutoSize)); edit.Controls.Add(control, 0, edit.RowCount++); }
        edit.BackColor = Color.White; logic.AccessibleName = "Quan hệ nhóm"; field.AccessibleName = "Trường dữ liệu"; op.AccessibleName = "Toán tử so sánh"; value.AccessibleName = date.AccessibleName = options.AccessibleName = "Giá trị điều kiện";
        help.MaximumSize = new Size(340, 0);
        var split = new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,RowCount=1 };split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,55));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,45));split.Controls.Add(left,0,0);split.Controls.Add(edit,1,0);
        var apply = Ui.Button("&Áp dụng", (_, _) => Apply(), true); var cancel = Ui.Button("&Hủy", (_, _) => { DialogResult = DialogResult.Cancel; Close(); });
        var review=Ui.Button("Đọc lại bộ lọc",(_,_)=>Ui.ShowInformation(this,"Đọc lại bộ lọc",summary.Text));summary.Visible=false;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(20) }; panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.Controls.Add(Ui.PageHeader("Bộ lọc hóa đơn", ""), 0, 0); panel.Controls.Add(split, 0, 1); panel.Controls.Add(summary,0,2); panel.Controls.Add(Ui.Bar(apply, cancel,review), 0, 3); Controls.Add(panel); AcceptButton = apply; CancelButton = cancel;
        summary.ForeColor = Ui.Muted;summary.BackColor=Ui.Soft; summary.Padding = new Padding(12);summary.Margin=new Padding(0,12,0,8);
        panel.SizeChanged += (_, _) => summary.MaximumSize = new Size(Math.Max(200,panel.ClientSize.Width-panel.Padding.Horizontal),0);
        tree.AfterSelect += (_, _) => SelectNode();
        field.SelectedIndexChanged += (_, _) => { if (!syncing) { UpdateSelected(); SelectNode(); } };
        op.SelectedIndexChanged += (_, _) => { if (!syncing) { UpdateSelected(); SelectNode(); } };
        logic.SelectedIndexChanged += (_, _) => UpdateSelected(); value.TextChanged += (_, _) => UpdateSelected(); date.ValueChanged += (_, _) => UpdateSelected(); options.SelectedIndexChanged += (_, _) => UpdateSelected(); tree.SelectedNode = root;
        RefreshDescription(); Ui.Prepare(this);
    }

    private void Add(string kind)
    {
        var parent = tree.SelectedNode;
        if (parent?.Tag is not EditNode { Kind: "group" }) parent = parent?.Parent;
        if (parent?.Tag is not EditNode { Kind: "group" })
        {
            var old = tree.Nodes[0]; tree.Nodes.Clear(); parent = new TreeNode("Tất cả điều kiện (AND)") { Tag = new EditNode("group") }; parent.Nodes.Add(old); tree.Nodes.Add(parent);
        }
        var child = new TreeNode(kind == "group" ? "Tất cả điều kiện (AND)" : "Tổng hóa đơn = 0") { Tag = new EditNode(kind) }; parent.Nodes.Add(child); parent.Expand(); tree.SelectedNode = child;
        RefreshDescription(); Ui.Pulse(tree);
    }
    private void Negate()
    {
        var selected = tree.SelectedNode; if (selected is null) return;
        if (selected.Tag is EditNode { Kind: "not" }) { if (selected.Nodes.Count == 1) { var child = selected.Nodes[0]; child.Remove(); var index = selected.Index; var collection = selected.Parent?.Nodes ?? tree.Nodes; selected.Remove(); collection.Insert(index, child); tree.SelectedNode = child; } RefreshDescription(); return; }
        var nodes = selected.Parent?.Nodes ?? tree.Nodes; var at = selected.Index; selected.Remove(); var wrapper = new TreeNode("KHÔNG (NOT)") { Tag = new EditNode("not") }; wrapper.Nodes.Add(selected); nodes.Insert(at, wrapper); wrapper.Expand(); tree.SelectedNode = wrapper;
        RefreshDescription(); Ui.Pulse(tree);
    }
    private void Remove() { var n = tree.SelectedNode; if (n is null || n.Parent is null) return; var parent = n.Parent; n.Remove(); tree.SelectedNode = parent; RefreshDescription(); }
    private void SelectNode()
    {
        if (tree.SelectedNode?.Tag is not EditNode n) return; syncing = true;
        try
        {
            logic.Enabled = n.Kind == "group"; Set(logic, n.Logic); field.Enabled = op.Enabled = n.Kind == "rule"; Set(field, n.Field); Set(op, n.Op);
            value.Text = n.Value; if (DateTime.TryParse(n.Value, out var parsed)) date.Value = parsed.Date;
            options.Items.Clear(); if (choices.TryGetValue(n.Field, out var entries)) options.Items.AddRange(entries.Cast<object>().ToArray());
            var missingChoice=choices.ContainsKey(n.Field)&&!string.IsNullOrEmpty(n.Value)&&!options.Items.Cast<Choice>().Any(item=>item.Value==n.Value);
            if(missingChoice)options.Items.Add(new Choice(n.Value,"Giá trị đã lưu · "+n.Value));Set(options, n.Value);
            if (options.SelectedIndex < 0 && options.Items.Count > 0) options.SelectedIndex = 0;
            logicEditor.Visible=n.Kind=="group";fieldEditor.Visible=operatorEditor.Visible=n.Kind=="rule";
            var needsValue = n.Kind == "rule" && n.Op is not ("isNull" or "notNull"); valueEditor.Visible = value.Visible = needsValue && n.Field != "issuedAt" && !choices.ContainsKey(n.Field); dateEditor.Visible = date.Visible = needsValue && n.Field == "issuedAt"; optionsEditor.Visible = options.Visible = needsValue && choices.ContainsKey(n.Field);
            help.Text=n.Kind=="group"?"Áp dụng quan hệ này cho các dòng con.":n.Kind=="not"?"Đảo kết quả điều kiện hoặc nhóm bên trong.":n.Field=="product"?"= chứa sản phẩm; <> không chứa sản phẩm.":n.Field=="issuedAt"?"Ngày lịch theo giờ Việt Nam.":n.Field is "grandTotalVnd" or "discountTotalVnd"?"Nhập VND không có dấu phân cách.":"";
            if(missingChoice)help.Text="Giá trị đã lưu không có trong danh sách hiện tại. Chỉ thay đổi khi bạn chọn giá trị khác.";
        }
        finally { syncing = false; }
    }
    private void UpdateSelected()
    {
        if (syncing || tree.SelectedNode?.Tag is not EditNode n) return;
        n.Logic = Ui.Selected(logic);
        if (n.Kind == "rule") { var previous = n.Field; n.Field = Ui.Selected(field); n.Op = Ui.Selected(op); n.Value = previous != n.Field ? n.Field == "issuedAt" ? DateTime.Today.ToString("yyyy-MM-dd") : "" : date.Visible ? date.Value.ToString("yyyy-MM-dd") : options.Visible ? Ui.Selected(options) : value.Text; }
        SetNodeLabels(tree.SelectedNode);
        errors.Clear(); help.ForeColor = Ui.Muted; summary.Visible=false;RefreshDescription();
    }
    private void Apply()
    {
        try { UpdateSelected(); var count = 0; Filter = tree.Nodes[0].Nodes.Count == 0 && tree.Nodes[0].Tag is EditNode { Kind: "group" } ? null : Build(tree.Nodes[0], 1, ref count); DialogResult = DialogResult.OK; Close(); }
        catch (Exception ex)
        {
            if(ex is FilterValidationException invalid){tree.SelectedNode=invalid.Node;invalid.Node.EnsureVisible();}
            var target=tree.SelectedNode?.Tag is EditNode{Kind:"rule"}?(options.Visible?(Control)options:date.Visible?date:value):tree;
            errors.SetError(target,ex.Message); help.Text = ex.Message; help.ForeColor = Ui.Danger; summary.Text = "Chưa áp dụng: " + ex.Message; summary.ForeColor = Ui.Danger;summary.Visible=true; target.Focus();
        }
    }
    private static FilterNode Build(TreeNode treeNode, int depth, ref int count)
    {
        if (depth > 4) throw new FilterValidationException(treeNode,"Bộ lọc tối đa 4 cấp.");
        var n = (EditNode)treeNode.Tag!;
        if (n.Kind == "group") { if (treeNode.Nodes.Count == 0) throw new FilterValidationException(treeNode,"Nhóm cần ít nhất một điều kiện."); var children = new List<FilterNode>(); foreach (TreeNode child in treeNode.Nodes) children.Add(Build(child, depth + 1, ref count)); return new InvoiceGroupFilter(n.Logic, children); }
        if (n.Kind == "not") { if (treeNode.Nodes.Count != 1) throw new FilterValidationException(treeNode,"NOT cần đúng một điều kiện/nhóm."); return new InvoiceNotFilter(Build(treeNode.Nodes[0], depth + 1, ref count)); }
        if (++count > 30) throw new FilterValidationException(treeNode,"Bộ lọc tối đa 30 điều kiện.");
        if (n.Op is "isNull" or "notNull") { if (n.Field is not ("issuedAt" or "invoiceNo" or "responsibleStaffId")) throw new FilterValidationException(treeNode,"Trường này không hỗ trợ NULL."); return new InvoiceNullFilter(n.Field, n.Op == "isNull"); }
        if (n.Field == "product") { if (!Guid.TryParse(n.Value, out var product)) throw new FilterValidationException(treeNode,"Chọn một sản phẩm."); if (n.Op is not ("=" or "<>")) throw new FilterValidationException(treeNode,"Sản phẩm dùng = (chứa) hoặc <> (không chứa)."); return new InvoiceProductFilter(product, n.Op == "=" ? "contains" : "not_contains"); }
        if (string.IsNullOrWhiteSpace(n.Value)) throw new FilterValidationException(treeNode,"Nhập/chọn giá trị cho điều kiện.");
        if(n.Field is "responsibleStaffId" or "customerId" or "lifecycle" or "paymentStatus"&&n.Op is not("=" or "<>"))throw new FilterValidationException(treeNode,"Trường lựa chọn chỉ dùng toán tử = hoặc <>.");
        if (n.Field is "grandTotalVnd" or "discountTotalVnd" && (!decimal.TryParse(n.Value,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var v) || v < 0 || decimal.Truncate(v) != v)) throw new FilterValidationException(treeNode,"Nhập số nguyên VND không âm, không có dấu phân cách. Ví dụ: 5000000.");
        return new InvoiceRuleFilter(n.Field, n.Op, JsonSerializer.SerializeToElement(n.Value));
    }
    private static TreeNode FromFilter(FilterNode filter)
    {
        var n = new TreeNode();
        switch (filter)
        {
            case InvoiceGroupFilter g: n.Tag = new EditNode("group") { Logic = g.Logic }; n.Text = g.Logic; foreach (var child in g.Children) n.Nodes.Add(FromFilter(child)); break;
            case InvoiceNotFilter not: n.Tag = new EditNode("not"); n.Text = "KHÔNG (NOT)"; n.Nodes.Add(FromFilter(not.Child)); break;
            case InvoiceRuleFilter rule: n.Tag = new EditNode("rule") { Field = rule.Field, Op = rule.Op, Value = rule.Value.ToString() }; n.Text = $"{rule.Field} {rule.Op} {rule.Value}"; break;
            case InvoiceNullFilter nil: n.Tag = new EditNode("rule") { Field = nil.Field, Op = nil.IsNull ? "isNull" : "notNull" }; n.Text = $"{nil.Field} {(nil.IsNull ? "trống" : "có dữ liệu")}"; break;
            case InvoiceProductFilter p: n.Tag = new EditNode("rule") { Field = "product", Op = p.Mode == "contains" ? "=" : "<>", Value = p.ProductId.ToString() }; n.Text = $"Sản phẩm {p.Mode}: {p.ProductId}"; break;
        }
        return n;
    }
    private static void Set(ComboBox box, string value) { for (var i = 0; i < box.Items.Count; i++) if ((box.Items[i] as Choice)?.Value == value) { box.SelectedIndex = i; return; } box.SelectedIndex = -1; }
    private void SetNodeLabels(TreeNode node)
    {
        var n=(EditNode)node.Tag!;var label=field.Items.Cast<Choice>().FirstOrDefault(c=>c.Value==n.Field)?.Label??n.Field;
        var display=choices.TryGetValue(n.Field,out var values)?values.FirstOrDefault(c=>c.Value==n.Value)?.Label??(string.IsNullOrEmpty(n.Value)?"chưa chọn":"giá trị đã lưu · "+n.Value):n.Value;
        node.Text=n.Kind=="group"?n.Logic=="AND"?"Tất cả điều kiện (AND)":"Ít nhất một điều kiện (OR)":n.Kind=="not"?"Phủ định (NOT)":label+" "+(n.Op=="isNull"?"trống":n.Op=="notNull"?"có dữ liệu":n.Op+" "+display);
        node.ToolTipText=node.Text;foreach(TreeNode child in node.Nodes)SetNodeLabels(child);
    }
    private void RefreshDescription()
    {
        if (tree.Nodes.Count == 0) return;
        string Read(TreeNode node)
        {
            var n = (EditNode)node.Tag!;
            if (n.Kind == "group") return "(" + string.Join(n.Logic == "AND" ? " và " : " hoặc ", node.Nodes.Cast<TreeNode>().Select(Read)) + ")";
            if (n.Kind == "not") return "Không thỏa " + (node.Nodes.Count > 0 ? Read(node.Nodes[0]) : "(chưa có điều kiện)");
            var label = field.Items.Cast<Choice>().FirstOrDefault(c => c.Value == n.Field)?.Label ?? n.Field;
            var display = choices.TryGetValue(n.Field, out var values) ? values.FirstOrDefault(c => c.Value == n.Value)?.Label ?? (string.IsNullOrEmpty(n.Value)?"chưa chọn":"giá trị đã lưu · "+n.Value) : n.Value;
            return label + " " + (n.Op == "isNull" ? "trống" : n.Op == "notNull" ? "có dữ liệu" : n.Op + " " + display);
        }
        summary.Text = "Đọc lại: " + Read(tree.Nodes[0]); summary.ForeColor = Ui.Muted;
    }
    internal static string Describe(FilterNode? node) => node switch
    {
        null => "Tất cả chứng từ trong phạm vi được phép",
        InvoiceGroupFilter group => "(" + string.Join(group.Logic == "AND" ? " và " : " hoặc ", group.Children.Select(Describe)) + ")",
        InvoiceNotFilter not => "Không thỏa " + Describe(not.Child),
        InvoiceProductFilter product => (product.Mode == "contains" ? "Có" : "Không có") + " sản phẩm " + product.ProductId,
        InvoiceNullFilter nil => FieldName(nil.Field) + (nil.IsNull ? " trống" : " có dữ liệu"),
        InvoiceRuleFilter rule => FieldName(rule.Field) + " " + rule.Op + " " + rule.Value,
        _ => "Bộ lọc nâng cao"
    };
    private static string FieldName(string field) => field switch { "grandTotalVnd" => "Tổng HĐ", "discountTotalVnd" => "Giảm giá", "invoiceNo" => "Mã HĐ", "issuedAt" => "Ngày phát hành", "responsibleStaffId" => "Nhân viên", "customerId" => "Khách hàng", "lifecycle" => "Vòng đời", "paymentStatus" => "Thanh toán", _ => field };
    protected override void Dispose(bool disposing) { if (disposing) errors.Dispose(); base.Dispose(disposing); }
    private sealed class EditNode(string kind) { public string Kind = kind, Field = "grandTotalVnd", Op = "=", Value = "0", Logic = "AND"; }
    private sealed class FilterValidationException(TreeNode node,string message):InvalidOperationException(message){internal TreeNode Node{get;}=node;}
}
