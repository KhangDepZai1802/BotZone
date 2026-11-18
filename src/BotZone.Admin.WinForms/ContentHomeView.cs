using System.Text.Json;

namespace BotZone.Admin.WinForms;

internal sealed class ContentHomeView : PageView
{
    private readonly DataGridView grid=Ui.Grid();
    private readonly BindingSource binding=new();
    private readonly ComboBox view=Ui.Combo(new("all","Tất cả khối"),new("missing","Cần hoàn thiện"),new("changed","Chưa xuất bản"));
    private readonly Label totalCount=Ui.Label("—"),missingCount=Ui.Label("—"),changedCount=Ui.Label("—");
    private readonly Button details;
    private readonly Action<string> editSection;
    private string publication="",scope="";
    private string? preferredSectionId;
    private List<ContentRow> rows=[];
    private bool cleaned,bindingRows;
    internal Form? ActiveSectionDetails{get;private set;}
    internal string? SelectedContentId=>Selected()?.Id;
    public ContentHomeView(ApiClient api,Action<string> navigate,Action<string>? editSection=null):base(api)
    {
        this.editSection=editSection??(_=>navigate("landing"));Dock=DockStyle.Fill;
        grid.DataSource=binding;grid.AccessibleName="Công việc nội dung landing";view.AccessibleName="Phạm vi công việc nội dung";view.Width=240;
        Ui.Column(grid,"Name","Khối nội dung",300);Ui.Column(grid,"Work","Việc cần làm",200);Ui.Column(grid,"State","Xuất bản",210);
        grid.CellFormatting+=(_,e)=>{if(e.RowIndex<0||e.ColumnIndex<0)return;var column=grid.Columns[e.ColumnIndex].DataPropertyName;if(column=="State")e.CellStyle.ForeColor=Convert.ToString(e.Value)=="Chưa xuất bản"?Ui.Primary:Ui.Muted;};
        details=Ui.Button("Xem khối",async(_,_)=>await ShowSelectedAsync());details.Name="contentSectionDetails";
        grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await ShowSelectedAsync();};grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.Handled=true;e.SuppressKeyPress=true;await ShowSelectedAsync();}};
        grid.CurrentCellChanged+=(_,_)=>{if(!IsDisposed&&!Disposing&&!cleaned){var selected=Selected();details.Enabled=selected is not null;if(!bindingRows&&selected is not null)preferredSectionId=selected.Id;}};view.SelectedIndexChanged+=(_,_)=>Filter();
        var cards=new TableLayoutPanel{Dock=DockStyle.Top,Height=76,ColumnCount=3,RowCount=1,Margin=new Padding(0,0,0,12)};for(var i=0;i<3;i++)cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));cards.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        cards.Controls.Add(Metric("Khối trang chủ",totalCount),0,0);cards.Controls.Add(Metric("Cần hoàn thiện",missingCount),1,0);cards.Controls.Add(Metric("Chưa xuất bản",changedCount),2,0);
        var more=Ui.MenuButton("Khác",("Nội dung sản phẩm",()=>navigate("catalogue")),("Thư viện media",()=>navigate("media")),("Thông tin xuất bản",()=>Ui.ShowInformation(FindForm(),"Xuất bản landing",publication)));
        Controls.Add(Ui.Vertical(Ui.PageHeader("Việc nội dung","Landing trang chủ · Việt / Anh",Ui.Button("Biên tập",(_,_)=>navigate("landing"),true)),cards,
            Ui.Bar(Ui.FrameInput(view),details,more,Ui.Button("Tải lại",async(_,_)=>await RefreshAsync())),Status,grid));
        SizeChanged+=(_,_)=>cards.Height=(int)(76*DeviceDpi/96f);
    }
    public override Task RefreshAsync()=>LoadLatestAsync(async token=>
    {
        var draft=Api.GetAsync<JsonElement>("api/admin/landing",token);var current=Api.GetAsync<JsonElement>("api/landing",token);await Task.WhenAll(draft,current);return(admin:await draft,current:await current);
    },data=>
    {
        var draft=data.admin.GetProperty("draft");var sections=Ui.Rows(draft,"sections");var current=Ui.Rows(data.current,"sections");
        rows=sections.Select((section,index)=>
        {
            var problems=new List<string>();foreach(var language in new[]{"vi","en"}){if(string.IsNullOrWhiteSpace(Nested(section,"title",language)))problems.Add(language.ToUpperInvariant()+": Thiếu tiêu đề");if(string.IsNullOrWhiteSpace(Nested(section,"subtitle",language)))problems.Add(language.ToUpperInvariant()+": Thiếu mô tả");}
            if(string.IsNullOrWhiteSpace(Ui.Text(section,"poster")))problems.Add("Chưa chọn ảnh đại diện");if(Ui.Text(section,"type")is"hero"or"duo"&&string.IsNullOrWhiteSpace(Ui.Text(section,"productSlug")))problems.Add("Chưa chọn sản phẩm liên kết");
            var id=Ui.Text(section,"id");var old=current.FirstOrDefault(x=>Ui.Text(x,"id")==id);var changed=old.ValueKind==JsonValueKind.Undefined||!JsonElement.DeepEquals(section,old)||current.FindIndex(x=>Ui.Text(x,"id")==id)!=index;
            var name=Nested(section,"title","vi");if(string.IsNullOrWhiteSpace(name))name=Nested(section,"title","en");if(string.IsNullOrWhiteSpace(name))name="Khối chưa có tiêu đề";
            return new ContentRow(id,$"{index+1:00} · {name}",problems.Count>0?$"{problems.Count} mục cần bổ sung":changed?"Cần xem trước":"—",changed?"Chưa xuất bản":"Khớp bản công khai",problems.Count>0,changed,section.Clone(),problems);
        }).ToList();
        totalCount.Text=rows.Count.ToString();missingCount.Text=rows.Count(x=>x.Missing).ToString();changedCount.Text=rows.Count(x=>x.Changed).ToString();
        var history=Ui.Rows(data.admin,"history");var latest=history.FirstOrDefault();scope=$"Nháp r{Ui.Text(draft,"revision")} · Bản công khai v{Ui.Text(data.current,"revision")}";
        publication=scope+(history.Count==0?"\nChưa có lịch sử xuất bản.":$"\nNgười xuất bản: {Ui.Text(latest,"createdBy")}\nThời gian: "+(DateTimeOffset.TryParse(Ui.Text(latest,"publishedAt"),out var at)?Ui.VietDate(at):Ui.Text(latest,"publishedAt")));
        Filter();
    });
    private ContentRow? Selected(){var index=grid.CurrentRow?.Index??-1;return index>=0&&index<binding.Count?binding[index]as ContentRow:null;}
    private void Filter()
    {
        var selected=Ui.Selected(view);var filtered=rows.Where(x=>selected=="missing"?x.Missing:selected!="changed"||x.Changed).ToList();
        bindingRows=true;try{binding.DataSource=filtered;var index=filtered.FindIndex(x=>x.Id==preferredSectionId);if(index>=0&&index<grid.Rows.Count){grid.CurrentCell=grid.Rows[index].Cells["Name"];grid.Rows[index].Selected=true;}else if(preferredSectionId is null)preferredSectionId=Selected()?.Id;}finally{bindingRows=false;}details.Enabled=Selected()is not null;
        Status.Text=filtered.Count==0?"Không có khối phù hợp · "+scope:$"{filtered.Count} / {rows.Count} khối · {scope}";
    }
    private async Task ShowSelectedAsync()
    {
        var row=Selected();var owner=FindForm();if(row is null||owner is null||ActiveSectionDetails is not null)return;
        using var dialog=new Form{Text="Khối nội dung",Size=new Size(760,540),MinimumSize=new Size(540,400),StartPosition=FormStartPosition.CenterParent,KeyPreview=true};ActiveSectionDetails=dialog;Ui.Prepare(dialog);dialog.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape){e.Handled=true;dialog.Close();}};
        var language=Ui.Combo(new("vi","Tiếng Việt"),new("en","English"));language.AccessibleName="Ngôn ngữ xem nội dung";
        var title=ReadOnlyText("Tiêu đề khối nội dung");title.Font=new Font("Segoe UI",13,FontStyle.Bold);
        var description=ReadOnlyText("Mô tả khối nội dung");
        void ShowLanguage(){var code=Ui.Selected(language);title.Text=Nested(row.Section,"title",code);description.Text=Nested(row.Section,"subtitle",code);}language.SelectedIndexChanged+=(_,_)=>ShowLanguage();ShowLanguage();
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Ui.Surface,Padding=new Padding(16)};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Percent,40));body.RowStyles.Add(new RowStyle(SizeType.Percent,60));body.Controls.Add(TextSection("Tiêu đề",title),0,0);body.Controls.Add(TextSection("Mô tả",description),0,1);
        var edit=Ui.Button("Biên tập khối",(_,_)=>{dialog.DialogResult=DialogResult.OK;dialog.Close();},true);edit.Name="editContentSection";
        var more=Ui.MenuButton("Thông tin",("Tài nguyên & liên kết",()=>Ui.ShowInformation(dialog,"Hiển thị của khối",$"Loại khối: {SectionKind(Ui.Text(row.Section,"type"))}\nẢnh đại diện: {Ui.Text(row.Section,"poster")}\nSản phẩm liên kết: {Ui.Text(row.Section,"productSlug")}\nMô hình 3D: {Ui.Text(row.Section,"model")}")),("Xuất bản",()=>Ui.ShowInformation(dialog,"Xuất bản landing",publication)));
        var issue=Ui.Label(row.Problems.Count>0?string.Join(" · ",row.Problems):"");issue.ForeColor=Ui.Warning;issue.Visible=row.Problems.Count>0;
        dialog.Controls.Add(Ui.Vertical(Ui.PageHeader("Khối nội dung",row.State,edit),Ui.Bar(Ui.FrameInput(language),more),issue,body));dialog.SizeChanged+=(_,_)=>issue.MaximumSize=new Size(Math.Max(150,dialog.ClientSize.Width-64),0);
        try{if(await dialog.ShowDialogAsync(owner)==DialogResult.OK&&!IsDisposed)editSection(row.Id);}finally{ActiveSectionDetails=null;}
    }
    private static Control Metric(string title,Label number)
    {
        var card=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Ui.Surface,Padding=new Padding(16,10,16,10),Margin=new Padding(0,0,12,0)};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.AutoSize));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));var caption=Ui.Label(title);caption.ForeColor=Ui.Muted;caption.Margin=Padding.Empty;number.Font=new Font("Segoe UI",23,FontStyle.Bold);number.Dock=DockStyle.Fill;number.Margin=Padding.Empty;card.Controls.Add(caption,0,0);card.Controls.Add(number,0,1);card.Paint+=(_,e)=>{using var border=new Pen(Ui.Line);e.Graphics.DrawRectangle(border,0,0,card.Width-1,card.Height-1);};return card;
    }
    private static string Nested(JsonElement item,string property,string language)=>item.TryGetProperty(property,out var value)?Ui.Text(value,language):"";
    private static TextBox ReadOnlyText(string label)=>new(){ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Ui.Surface,AccessibleName=label};
    private static Control TextSection(string caption,TextBox text){var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0,0,0,12)};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));var label=Ui.Label(caption);label.ForeColor=Ui.Muted;panel.Controls.Add(label,0,0);panel.Controls.Add(text,0,1);return panel;}
    private static string SectionKind(string value)=>value switch{"hero"=>"Sản phẩm nổi bật","duo"=>"Duo mở/gập","collection"=>"Bộ sưu tập","benefits"=>"Lợi ích",_=>value};
    private sealed record ContentRow(string Id,string Name,string Work,string State,bool Missing,bool Changed,JsonElement Section,List<string> Problems);
    protected override void Dispose(bool disposing){if(disposing&&!cleaned){cleaned=true;grid.DataSource=null;binding.Dispose();}base.Dispose(disposing);}
}
