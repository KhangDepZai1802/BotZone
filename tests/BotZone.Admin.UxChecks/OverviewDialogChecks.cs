using System.Collections;
using System.Reflection;

internal sealed partial class Harness
{
    private async Task<Form> OpenOverviewDialog(Form main,Control page,string buttonText,string name)
    {
        await Foreground(main);Desc(page).OfType<Button>().Single(b=>b.Text==buttonText).PerformClick();
        await Until(()=>Application.OpenForms.Cast<Form>().Any(f=>f.Name==name),"Overview dialog did not open: "+name);
        var dialog=Application.OpenForms.Cast<Form>().Single(f=>f.Name==name);await Foreground(dialog);return dialog;
    }
    private void FooterVisible(Form dialog,string name)
    {
        foreach(var button in Desc(dialog).OfType<Button>().Where(b=>b.Visible))
        {
            var bounds=button.RectangleToScreen(button.ClientRectangle);var visible=dialog.RectangleToScreen(dialog.ClientRectangle);
            for(Control? parent=button.Parent;parent is not null;parent=parent.Parent)visible=Rectangle.Intersect(visible,parent.RectangleToScreen(parent.ClientRectangle));
            Check(name+"-full-button-"+button.Name,visible.Contains(bounds),new{bounds,visible});
        }
    }
    private static bool FullyVisible(Control control)
    {
        var bounds=control.RectangleToScreen(control.ClientRectangle);var visible=bounds;
        for(Control? parent=control.Parent;parent is not null;parent=parent.Parent)visible=Rectangle.Intersect(visible,parent.RectangleToScreen(parent.ClientRectangle));
        return control.Visible&&visible.Contains(bounds);
    }
    private async Task OverviewDialogs(Form main)
    {
        await Call(main,"NavigateAsync","overview",null);var page=(Control)Property(main,"CurrentPage")!;
        var from=Field<DateTimePicker>(page,"from");var to=Field<DateTimePicker>(page,"to");var originalFrom=from.Value;var originalTo=to.Value;
        var period=Field<Label>(page,"period");var originalPeriod=period.Text;
        using(var dialog=await OpenOverviewDialog(main,page,"Đổi khoảng ngày","overviewPeriodDialog"))
        {
            var start=dialog.Controls.Find("periodStart",true).OfType<DateTimePicker>().Single();var end=dialog.Controls.Find("periodEnd",true).OfType<DateTimePicker>().Single();
            Check("period-prefills-inclusive-end",start.Value.Date==originalFrom.Date&&end.Value.Date==originalTo.Date.AddDays(-1));
            FooterVisible(dialog,"period-default");await Capture(dialog,"period-default");
            dialog.Size=dialog.MinimumSize;await Settle();FooterVisible(dialog,"period-minimum");Check("date-fields-fully-visible-at-minimum",FullyVisible(start.Parent!)&&FullyVisible(end.Parent!));await Capture(dialog,"period-minimum");
            end.Value=start.Value.AddDays(-1);dialog.Controls.Find("periodApply",true).OfType<Button>().Single().PerformClick();
            Check("invalid-period-stays-open-with-inline-error",dialog.Visible&&dialog.DialogResult!=DialogResult.OK&&FullyVisible(dialog.Controls.Find("periodValidation",true).Single()));
            FooterVisible(dialog,"period-invalid");await Capture(dialog,"period-invalid");
            Invoke(dialog,"ProcessDialogKey",Keys.Escape);await Until(()=>!dialog.Visible,"Escape did not close date dialog.");
        }
        Check("cancel-date-edit-preserves-period",from.Value==originalFrom&&to.Value==originalTo&&period.Text==originalPeriod);
        using(var dialog=await OpenOverviewDialog(main,page,"Đổi khoảng ngày","overviewPeriodDialog"))
        {
            var start=dialog.Controls.Find("periodStart",true).OfType<DateTimePicker>().Single();var end=dialog.Controls.Find("periodEnd",true).OfType<DateTimePicker>().Single();end.Value=start.Value;
            dialog.Controls.Find("periodApply",true).OfType<Button>().Single().PerformClick();
            await Until(()=>!dialog.Visible&&to.Value.Date==originalFrom.Date.AddDays(1)&&period.Text!=originalPeriod,"Single-day period did not apply.");
            Check("single-day-period-remains-inclusive",from.Value.Date==originalFrom.Date&&to.Value.Date==originalFrom.Date.AddDays(1));
        }
        using(var dialog=await OpenOverviewDialog(main,page,"Đổi khoảng ngày","overviewPeriodDialog"))
        {
            dialog.Controls.Find("periodStart",true).OfType<DateTimePicker>().Single().Value=originalFrom;dialog.Controls.Find("periodEnd",true).OfType<DateTimePicker>().Single().Value=originalTo.AddDays(-1);
            dialog.Controls.Find("periodApply",true).OfType<Button>().Single().PerformClick();await Until(()=>!dialog.Visible&&period.Text==originalPeriod,"Original period was not restored.");
        }
        using(var dialog=await OpenOverviewDialog(main,page,"Giải thích số liệu","overviewMetricsDialog"))
        {
            var rows=Field<IEnumerable>(page,"metricDetails").Cast<object>().ToArray();var labels=Desc(dialog).OfType<Label>().Select(l=>l.Text).ToArray();
            Check("metric-explanation-keeps-each-value-and-description",rows.Length==3&&rows.All(r=>labels.Contains((string)Property(r,"Title")!)&&labels.Contains((string)Property(r,"Value")!)&&labels.Contains((string)Property(r,"Description")!)));
            Check("metric-explanation-shows-applied-period",labels.Contains(originalPeriod));Check("metric-explanation-has-separated-sections",rows.All(r=>Desc(dialog).OfType<TableLayoutPanel>().Any(c=>c.AccessibleName==(string)Property(r,"Title")!)));
            FooterVisible(dialog,"metrics-default");Check("metric-scope-note-visible-at-default-size",FullyVisible(dialog.Controls.Find("metricScopeNote",true).Single()));await Capture(dialog,"metrics-default");dialog.Size=dialog.MinimumSize;await Settle();FooterVisible(dialog,"metrics-minimum");await Capture(dialog,"metrics-minimum");
            var scroll=Desc(dialog).OfType<Panel>().Single(p=>p.GetType()==typeof(Panel)&&p.AutoScroll);scroll.AutoScrollPosition=new Point(0,scroll.DisplayRectangle.Height);await Settle();
            Check("metric-scope-note-can-be-reached",scroll.RectangleToScreen(scroll.ClientRectangle).Contains(dialog.Controls.Find("metricScopeNote",true).Single().RectangleToScreen(dialog.Controls.Find("metricScopeNote",true).Single().ClientRectangle)));
            FooterVisible(dialog,"metrics-scrolled");await Capture(dialog,"metrics-scrolled");Invoke(dialog,"ProcessDialogKey",Keys.Escape);
        }
        await Foreground(main);
        main.BeginInvoke((Action)(()=>admin.GetType("BotZone.Admin.WinForms.Ui")!.GetMethod("ShowInformation",Static)!.Invoke(null,[main,"Kiểm tra xuống dòng","Dòng một\n\nDòng hai\r\nDòng ba"])));
        await Until(()=>Application.OpenForms.Cast<Form>().Any(f=>f.Text=="Kiểm tra xuống dòng"),"Information dialog did not open.");
        using(var dialog=Application.OpenForms.Cast<Form>().Single(f=>f.Text=="Kiểm tra xuống dòng"))
        {
            await Foreground(dialog);var text=Desc(dialog).OfType<TextBox>().Single();Check("information-native-line-breaks",text.Text=="Dòng một\r\n\r\nDòng hai\r\nDòng ba"&&SendMessage(text.Handle,0xBA,IntPtr.Zero,IntPtr.Zero).ToInt32()==4);await Capture(dialog,"information-line-breaks");dialog.Close();
        }
        await Foreground(main);
    }
}
