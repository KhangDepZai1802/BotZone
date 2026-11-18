namespace BotZone.Admin.WinForms;

internal sealed record OverviewMetric(string Title,string Value,string Description);

internal sealed class OverviewPeriodDialog : Form
{
    private readonly DateTimePicker start,end;
    private readonly Label validation=Ui.Label("");
    internal DateTime StartDate=>start.Value.Date;
    internal DateTime EndDate=>end.Value.Date;

    internal OverviewPeriodDialog(DateTime first,DateTime last)
    {
        SuspendLayout();AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        Text="Khoảng ngày";Name="overviewPeriodDialog";ClientSize=new Size(540,370);
        StartPosition=FormStartPosition.CenterParent;MinimizeBox=MaximizeBox=false;Ui.Prepare(this);
        start=new BzDateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=first,Width=180,Name="periodStart"};
        end=new BzDateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",MaxDate=DateTimePicker.MaximumDateTime.AddDays(-1),Value=last,Width=180,Name="periodEnd"};
        var fields=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=2,Padding=new Padding(16),BackColor=Ui.Surface};
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var firstField=Ui.Field("Từ ngày",start);var lastField=Ui.Field("Đến ngày",end);firstField.Dock=lastField.Dock=DockStyle.Fill;lastField.Margin=new Padding(0,0,0,8);
        fields.Controls.Add(firstField,0,0);fields.Controls.Add(lastField,1,0);
        validation.Name="periodValidation";validation.ForeColor=Ui.Danger;validation.Visible=false;validation.Dock=DockStyle.Fill;
        fields.Controls.Add(validation,0,1);fields.SetColumnSpan(validation,2);
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};scroll.Controls.Add(fields);
        var apply=Ui.Button("Xem số liệu",(_,_)=>
        {
            if(StartDate>EndDate){validation.Text="Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.";validation.Visible=true;end.Focus();return;}
            DialogResult=DialogResult.OK;
        },true);apply.Name="periodApply";
        var cancel=Ui.Button("Hủy",(_,_)=>DialogResult=DialogResult.Cancel);cancel.Name="periodCancel";AcceptButton=apply;CancelButton=cancel;
        start.ValueChanged+=(_,_)=>validation.Visible=false;end.ValueChanged+=(_,_)=>validation.Visible=false;
        Controls.Add(Ui.DialogLayout(Ui.PageHeader("Khoảng ngày","Chọn kỳ xem số liệu, tính cả ngày kết thúc."),scroll,apply,cancel));
        // Use the rendered size at the current DPI; the short form must not shrink
        // far enough to hide a date field or its inline validation.
        Shown+=(_,_)=>MinimumSize=Size;
        ResumeLayout(true);
    }
}

internal sealed class OverviewMetricsDialog : Form
{
    internal OverviewMetricsDialog(IReadOnlyList<OverviewMetric> metrics,string period)
    {
        SuspendLayout();AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        Text="Cách đọc số liệu";Name="overviewMetricsDialog";ClientSize=new Size(620,650);MinimumSize=new Size(500,420);
        StartPosition=FormStartPosition.CenterParent;MinimizeBox=MaximizeBox=false;Ui.Prepare(this);
        var sections=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,Padding=Padding.Empty};sections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(var metric in metrics)
        {
            var card=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=2,Padding=new Padding(16),BackColor=Ui.Surface,Margin=new Padding(0,0,0,12),AccessibleName=metric.Title};
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));card.RowStyles.Add(new RowStyle(SizeType.AutoSize));card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var title=Ui.Label(metric.Title);title.Font=new Font("Segoe UI Semibold",11);title.Anchor=AnchorStyles.Left;title.Margin=new Padding(0,0,12,10);
            var value=Ui.Label(metric.Value);value.Font=new Font("Segoe UI Semibold",16);value.ForeColor=Ui.Primary;value.Anchor=AnchorStyles.Right;value.Margin=new Padding(0,0,0,10);
            var detail=Ui.Label(metric.Description);detail.ForeColor=Ui.Muted;detail.Dock=DockStyle.Fill;detail.Margin=Padding.Empty;
            card.Controls.Add(title,0,0);card.Controls.Add(value,1,0);card.Controls.Add(detail,0,1);card.SetColumnSpan(detail,2);
            card.SizeChanged+=(_,_)=>detail.MaximumSize=new Size(Math.Max(100,card.ClientSize.Width-card.Padding.Horizontal),0);
            sections.RowStyles.Add(new RowStyle(SizeType.AutoSize));sections.Controls.Add(card,0,sections.RowCount++);
        }
        var note=Ui.Label(metrics.Count==0?"Số liệu đang tải. Đóng cửa sổ và thử lại sau khi trang tổng quan tải xong.":"Các số tiền này là giá trị hóa đơn, không phải lợi nhuận.\r\n\r\nBiểu đồ đơn hàng và tồn kho thể hiện tình hình hiện tại, không lọc theo khoảng ngày đã chọn.");
        note.Name="metricScopeNote";note.ForeColor=Ui.Muted;note.Dock=DockStyle.Fill;note.Margin=new Padding(0,4,0,0);
        sections.RowStyles.Add(new RowStyle(SizeType.AutoSize));sections.Controls.Add(note,0,sections.RowCount++);
        sections.SizeChanged+=(_,_)=>note.MaximumSize=new Size(Math.Max(100,sections.ClientSize.Width),0);
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};scroll.Controls.Add(sections);
        var close=Ui.Button("Đóng",(_,_)=>Close());close.Name="metricsClose";CancelButton=close;AcceptButton=close;
        Controls.Add(Ui.DialogLayout(Ui.PageHeader("Cách đọc số liệu",period),scroll,close));ResumeLayout(true);
    }
}
