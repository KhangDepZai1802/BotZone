using System.Drawing.Drawing2D;

namespace BotZone.Admin.WinForms;

// A native, keyboard-accessible calendar surface. DateTimePicker remains the
// bound input and retains its editable date/time segments and min/max contract.
internal sealed class CalendarPopup : ToolStripDropDown
{
    private readonly BzDateTimePicker owner;
    private readonly TableLayoutPanel days;
    private readonly Button heading;
    private readonly Button previous,next;
    private readonly TableLayoutPanel monthPicker;
    private readonly TableLayoutPanel weekdays;
    private readonly NumericUpDown year;
    private bool choosingMonth;
    private DateTime month;
    private Button? selected;
    internal CalendarPopup(BzDateTimePicker owner)
    {
        this.owner=owner;month=new DateTime(owner.Value.Year,owner.Value.Month,1);BackColor=Ui.Surface;Padding=new Padding(10);AutoSize=false;DropShadowEnabled=true;
        var scale=owner.DeviceDpi/96f;var width=(int)(304*scale);var height=(int)(324*scale);
        var body=new TableLayoutPanel{Size=new Size(width,height),ColumnCount=1,RowCount=4,BackColor=Ui.Surface,Font=owner.Font,Margin=Padding.Empty};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,44*scale));body.RowStyles.Add(new RowStyle(SizeType.Absolute,28*scale));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,42*scale));
        var navigation=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1};navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,38*scale));navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,38*scale));
        previous=SmallButton("‹",()=>ChangeMonth(-1));previous.AccessibleName="Tháng trước";next=SmallButton("›",()=>ChangeMonth(1));next.AccessibleName="Tháng sau";
        heading=SmallButton("",()=>ShowMonths(!choosingMonth));heading.AccessibleName="Chọn tháng và năm";heading.Font=new Font("Segoe UI",11,FontStyle.Bold);((BzButton)heading).Quiet=true;navigation.Controls.Add(previous,0,0);navigation.Controls.Add(heading,1,0);navigation.Controls.Add(next,2,0);body.Controls.Add(navigation,0,0);
        weekdays=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=7,RowCount=1};foreach(var title in new[]{"T2","T3","T4","T5","T6","T7","CN"}){weekdays.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/7));weekdays.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Ui.Muted,Font=new Font("Segoe UI",9)});}body.Controls.Add(weekdays,0,1);
        days=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=7,RowCount=6,Margin=Padding.Empty};for(var i=0;i<7;i++)days.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/7));for(var i=0;i<6;i++)days.RowStyles.Add(new RowStyle(SizeType.Percent,100f/6));var dayHost=new Panel{Dock=DockStyle.Fill};dayHost.Controls.Add(days);body.Controls.Add(dayHost,0,2);
        monthPicker=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=5,Visible=false};for(var i=0;i<3;i++)monthPicker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));monthPicker.RowStyles.Add(new RowStyle(SizeType.Absolute,52*scale));for(var i=0;i<4;i++)monthPicker.RowStyles.Add(new RowStyle(SizeType.Percent,25));
        year=new NumericUpDown{Minimum=owner.MinDate.Year,Maximum=owner.MaxDate.Year,Value=month.Year,Width=240,AccessibleName="Năm",Font=owner.Font,TextAlign=HorizontalAlignment.Center};var yearFrame=Ui.FrameInput(year);yearFrame.Dock=DockStyle.Fill;monthPicker.Controls.Add(yearFrame,0,0);monthPicker.SetColumnSpan(yearFrame,3);year.ValueChanged+=(_,_)=>UpdateMonths();
        for(var i=1;i<=12;i++){var number=i;var button=(BzButton)SmallButton("Tháng "+i,()=>PickMonth(number));button.Quiet=true;button.Tag=i;monthPicker.Controls.Add(button,(i-1)%3,1+(i-1)/3);}dayHost.Controls.Add(monthPicker);
        var today=Ui.Button("Hôm nay",(_,_)=>Choose(DateTime.Today));today.Dock=DockStyle.Fill;today.Enabled=DateTime.Today>=owner.MinDate.Date&&DateTime.Today<=owner.MaxDate.Date;body.Controls.Add(today,0,3);
        var host=new ToolStripControlHost(body){AutoSize=false,Size=body.Size,Margin=Padding.Empty,Padding=Padding.Empty};Items.Add(host);Size=new Size(width+Padding.Horizontal,height+Padding.Vertical);Rebuild();
        Closed+=(_,_)=>{if(!owner.IsDisposed&&!owner.Disposing){owner.Focus();owner.Invalidate();}};
    }
    private Button SmallButton(string text,Action action){var b=Ui.Button(text,(_,_)=>action());b.AutoSize=false;b.MinimumSize=Size.Empty;b.Dock=DockStyle.Fill;b.Margin=new Padding(2);b.Padding=Padding.Empty;return b;}
    private void ChangeMonth(int direction){if(choosingMonth){year.Value=Math.Clamp(year.Value+direction,year.Minimum,year.Maximum);return;}var target=FocusedDate().AddMonths(direction);MoveFocus(target);}
    private DateTime FocusedDate()=>days.Controls.OfType<Button>().FirstOrDefault(x=>x.Focused)?.Tag is DateTime date?date:new DateTime(month.Year,month.Month,Math.Min(owner.Value.Day,DateTime.DaysInMonth(month.Year,month.Month)));
    private void MoveFocus(DateTime target){target=target<owner.MinDate.Date?owner.MinDate.Date:target>owner.MaxDate.Date?owner.MaxDate.Date:target;month=new DateTime(target.Year,target.Month,1);Rebuild();days.Controls.OfType<Button>().FirstOrDefault(x=>Equals(x.Tag,target))?.Focus();}
    private void ShowMonths(bool visible){choosingMonth=visible;previous.AccessibleName=visible?"Năm trước":"Tháng trước";next.AccessibleName=visible?"Năm sau":"Tháng sau";monthPicker.Visible=visible;days.Visible=!visible;weekdays.Visible=!visible;((TableLayoutPanel)weekdays.Parent!).RowStyles[1].Height=visible?0:28*owner.DeviceDpi/96f;if(visible){year.Value=month.Year;UpdateMonths();monthPicker.BringToFront();year.Focus();}else{Rebuild();FocusSelected();}}
    private void UpdateMonths(){foreach(var b in monthPicker.Controls.OfType<BzButton>()){var number=(int)b.Tag!;var date=new DateTime((int)year.Value,number,1);b.Enabled=date<=owner.MaxDate.Date&&date.AddMonths(1).AddDays(-1)>=owner.MinDate.Date;b.Primary=number==month.Month&&(int)year.Value==month.Year;}heading.Text="Chọn tháng · "+year.Value;previous.Enabled=year.Value>year.Minimum;next.Enabled=year.Value<year.Maximum;}
    private void PickMonth(int value){month=new DateTime((int)year.Value,value,1);ShowMonths(false);MoveFocus(new DateTime(month.Year,month.Month,Math.Min(owner.Value.Day,DateTime.DaysInMonth(month.Year,month.Month))));}
    private void Rebuild()
    {
        heading.Text=$"Tháng {month.Month} · {month.Year} ▾";previous.Enabled=month>new DateTime(owner.MinDate.Year,owner.MinDate.Month,1);next.Enabled=month<new DateTime(owner.MaxDate.Year,owner.MaxDate.Month,1);days.SuspendLayout();foreach(Control control in days.Controls.Cast<Control>().ToArray())control.Dispose();days.Controls.Clear();selected=null;
        var start=month.AddDays(-((int)month.DayOfWeek+6)%7);
        for(var i=0;i<42;i++){var date=start.AddDays(i);var button=(BzButton)SmallButton(date.Day.ToString(),()=>Choose(date));button.Quiet=true;button.AccessibleName=date.ToString("dd/MM/yyyy");button.Tag=date;button.Enabled=date>=owner.MinDate.Date&&date<=owner.MaxDate.Date;button.Primary=date==owner.Value.Date;button.ForeColor=date.Month==month.Month?Ui.Ink:Ui.Muted;if(date==owner.Value.Date)selected=button;days.Controls.Add(button,i%7,i/7);}
        days.ResumeLayout();
    }
    private void Choose(DateTime date){var value=date.Date+owner.Value.TimeOfDay;owner.Value=value<owner.MinDate?owner.MinDate:value>owner.MaxDate?owner.MaxDate:value;Close(ToolStripDropDownCloseReason.ItemClicked);}
    internal void FocusSelected(){(selected??days.Controls.OfType<Button>().FirstOrDefault(x=>x.Enabled&&x.Tag is DateTime d&&d.Month==month.Month))?.Focus();}
    protected override bool ProcessCmdKey(ref Message m,Keys keyData)
    {
        if(keyData==Keys.Escape){if(choosingMonth)ShowMonths(false);else Close();return true;}if(keyData is Keys.PageUp or Keys.PageDown){ChangeMonth(keyData==Keys.PageUp?-1:1);return true;}if(choosingMonth)return base.ProcessCmdKey(ref m,keyData);
        var delta=keyData switch{Keys.Left=>-1,Keys.Right=>1,Keys.Up=>-7,Keys.Down=>7,_=>0};
        if(delta!=0&&days.Controls.OfType<Button>().FirstOrDefault(x=>x.Focused)?.Tag is DateTime focused){var target=focused.AddDays(delta);if(target>=owner.MinDate.Date&&target<=owner.MaxDate.Date){if(target.Month!=month.Month||target.Year!=month.Year){month=new DateTime(target.Year,target.Month,1);Rebuild();}days.Controls.OfType<Button>().FirstOrDefault(x=>Equals(x.Tag,target))?.Focus();}return true;}
        return base.ProcessCmdKey(ref m,keyData);
    }
    protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;using var path=FieldPaint.Shape(new Rectangle(0,0,Width,Height),12);var old=Region;Region=new Region(path);old?.Dispose();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=FieldPaint.Shape(new Rectangle(0,0,Width-1,Height-1),12);using var border=new Pen(Ui.Line);e.Graphics.DrawPath(border,path);}
}
