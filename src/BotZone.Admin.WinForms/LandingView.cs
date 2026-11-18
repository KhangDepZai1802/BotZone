using System.Diagnostics;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class LandingView : PageView
{
    private readonly Actor actor;
    private readonly TreeView sectionsTree = new() { Dock = DockStyle.Fill, HideSelection = false, BorderStyle = BorderStyle.None, ShowLines = false, ShowRootLines = false, ShowPlusMinus = false, FullRowSelect = true, ItemHeight = 44, AccessibleName = "Các khối nội dung landing" };
    private readonly TabControl tabs = Ui.Tabs();
    private readonly ComboBox locale=Ui.Combo(new("vi","Tiếng Việt"),new("en","English")),displayMode=Ui.Combo(new("basic","Ảnh & liên kết"),new("motion","Chuyển động"),new("advanced","Nâng cao"));
    private readonly Panel languageHost=new(){Dock=DockStyle.Fill},displayHost=new(){Dock=DockStyle.Fill};
    private Control localeFrame=null!,displayModeFrame=null!,sectionFrame=null!;
    private FlowLayoutPanel languageBar=null!,displayBar=null!;
    private Control viFields=null!,enFields=null!,basicFields=null!,motionFields=null!,advancedFields=null!;
    private TabPage contentTab=null!,displayTab=null!;
    private readonly List<TableLayoutPanel> fieldLayouts=[];
    private readonly TextBox titleVi = new(), titleEn = new(), subtitleVi = new() { Multiline = true, Height = 80 }, subtitleEn = new() { Multiline = true, Height = 80 };
    private readonly ComboBox product = Ui.Combo(), type = Ui.Combo(new("hero", "Hero sản phẩm"), new("duo", "Duo mở/gập"), new("collection", "Bộ sưu tập"), new("benefits", "Lợi ích"));
    private readonly ComboBox poster = Ui.Combo(new("/models/v003/iphone-18-pro-max.webp", "Pro Max v003 — màn khóa Burgundy"), new("/models/v003/iphone-duo.webp", "Duo v003 — màn khóa phong cảnh"), new("/media/phone-burgundy.svg", "iPhone Burgundy — minh họa cũ"), new("/media/duo.svg", "Duo — minh họa cũ"), new("/media/ipad.svg", "iPad — local"), new("/media/mac.svg", "Mac — local"), new("/media/watch.svg", "Watch — local"));
    private readonly ComboBox model = Ui.Combo(new("v003:iphone-18-pro-max", "Pro Max v003 — mobile/laptop"), new("v003:iphone-duo", "Duo v003 — gập và tắt màn ngoài"), new("procedural:phone", "Điện thoại — tương thích bản cũ"), new("procedural:duo", "Duo — tương thích bản cũ"));
    private readonly ComboBox preset = Ui.Combo(new("float", "Nghiêng nhẹ"), new("rotate", "Xoay"), new("fold", "Mở/gập"));
    private readonly NumericUpDown camera = Number(2, 12, 5), tilt = Number(0, 30, 12), light = Number(.2m, 5, 2), speed = Number(0, 3, 1), fold = Number(0, 180, 70);
    private readonly TextBox colors = new() { Text = "#5b2333,#b7d8e5" };
    private readonly ListBox history = new() { Dock = DockStyle.Fill, DisplayMember = nameof(HistoryItem.Label), BorderStyle = BorderStyle.None, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 58, AccessibleName = "Phiên bản đã xuất bản" };
    private readonly Label historyEmpty = Ui.Label("Chưa có phiên bản đã xuất bản."), historyScope = Ui.Label("Tối đa 100 phiên bản mới nhất · Khôi phục sẽ xuất bản lại toàn trang.");
    private readonly ErrorProvider errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly Dictionary<Control, Label> inlineErrors = [];
    private FieldProblem? validationProblem;
    private readonly CancellationTokenSource lifetime = new();
    private readonly CommandKeys keys;
    private List<LandingSection> sections = [];
    private int revision;
    private bool syncing, busy, previewBusy, needsReload, cleaned;
    private int selectedIndex = -1;
    private readonly FlowLayoutPanel toolbar;
    private string savedSectionsJson="";
    private readonly Label versionLabel = Ui.Label("Đang đọc bản nháp…"), sectionTitle = Ui.Label("Chọn khối nội dung");
    private readonly ComboBox sectionPicker = Ui.Combo();
    private readonly TableLayoutPanel split = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private Control sectionList = null!, compactSelector = null!;
    private TableLayoutPanel contentLayout=null!,editorLayout=null!;
    private Button saveButton = null!, publishButton = null!, restoreButton = null!, refreshButton = null!, previewButton = null!;
    private readonly ContextMenuStrip previewMenu=new BzContextMenu();
    private Button? activeWriteButton;
    private readonly List<Button> structureButtons = [];
    internal int CurrentRevision=>revision;
    internal string SelectedSectionId=>selectedIndex>=0&&selectedIndex<sections.Count?sections[selectedIndex].Id:"";

    public LandingView(ApiClient api, Actor actor) : base(api)
    {
        this.actor = actor;keys=api.PendingCommands; Dock = DockStyle.Fill; errors.ContainerControl = this;
        var canEdit = actor.Role == "content_staff";
        var save = saveButton = Ui.Button("&Lưu nháp", async (_, _) => { await SendAsync("save_landing"); });
        var publish = publishButton = Ui.Button("&Xuất bản", async (_, _) => { await SendAsync("publish_landing"); }, true);
        var restore = restoreButton = Ui.Button("Khôi phục phiên bản", async (_, _) => { await SendAsync("restore_landing"); });
        save.Name="saveLanding";publish.Name="publishLanding";restore.Name="restoreLanding";
        var refresh = refreshButton = Ui.Button("Tải lại", async (_, _) => await RefreshAsync());
        previewButton=Ui.Button("Xem trước ▾",(_,_)=>previewMenu.Show(previewButton,new Point(0,previewButton.Height)));previewMenu.Items.Add("Màn hình laptop",null,async(_,_)=>await PreviewAsync("laptop"));previewMenu.Items.Add("Màn hình di động",null,async(_,_)=>await PreviewAsync("mobile"));
        versionLabel.ForeColor=Ui.Muted;versionLabel.Font=new Font("Segoe UI",9.5f);versionLabel.Margin=new Padding(12,10,0,0);
        save.Enabled = publish.Enabled = restore.Enabled = canEdit; toolbar = Ui.Bar(previewButton,refresh,versionLabel);
        var up = Ui.Button("Lên", (_, _) => MoveSection(-1)); var down = Ui.Button("Xuống", (_, _) => MoveSection(1)); var add = Ui.Button("Thêm", (_, _) => Add()); var remove = Ui.Button("Xóa", (_, _) => Remove());
        structureButtons.AddRange([up, down, add, remove]);
        up.Enabled = down.Enabled = add.Enabled = remove.Enabled = canEdit;
        var structureActions=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=2,RowCount=2,AutoSize=true,Margin=Padding.Empty};structureActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));structureActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));foreach(var button in new[]{up,down,add,remove}){button.AutoSize=false;button.MinimumSize=new Size(40,36);button.Dock=DockStyle.Fill;structureActions.Controls.Add(button);}
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12), BackColor = Ui.Surface, Margin = new Padding(0,0,16,0) }; left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); var listTitle = Ui.Label("Cấu trúc trang"); listTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold); left.Controls.Add(listTitle, 0, 0); left.Controls.Add(sectionsTree, 0, 1); left.Controls.Add(structureActions, 0, 2); sectionList = left;
        sectionsTree.DrawMode=TreeViewDrawMode.OwnerDrawAll;sectionsTree.ShowNodeToolTips=true;sectionsTree.DrawNode+=DrawSection;
        locale.AccessibleName="Ngôn ngữ nội dung";displayMode.AccessibleName="Tùy chọn hiển thị";
        viFields=Fields(("Tiêu đề tiếng Việt",titleVi),("Mô tả tiếng Việt",subtitleVi));enFields=Fields(("English title",titleEn),("English subtitle",subtitleEn));languageHost.Controls.AddRange([viFields,enFields]);
        contentTab=AddTab("Nội dung",ChoiceWorkspace(locale,languageHost,out localeFrame,out languageBar));
        var defaults = Ui.Button("Khôi phục mặc định", (_, _) => { camera.Value = 5; tilt.Value = 12; light.Value = 2; speed.Value = 1; fold.Value = 70; }); defaults.Enabled = canEdit; structureButtons.Add(defaults);
        basicFields=Fields(("Ảnh đại diện",poster),("Sản phẩm liên kết",product),("Loại khối",type));motionFields=Fields(("Mô hình 3D",model),("Mẫu chuyển động",preset));advancedFields=Fields(("Màu đại diện (#RRGGBB)",colors),("Khoảng cách camera · 2–12",camera),("Mức nghiêng · 0–30°",tilt),("Ánh sáng · 0,2–5",light),("Tốc độ · 0–3",speed),("Góc mở Duo · 0–180°",fold),("",defaults));displayHost.Controls.AddRange([basicFields,motionFields,advancedFields]);displayTab=AddTab("Hiển thị",ChoiceWorkspace(displayMode,displayHost,out displayModeFrame,out displayBar));
        locale.SelectedIndexChanged+=(_,_)=>ShowLanguage();displayMode.SelectedIndexChanged+=(_,_)=>ShowDisplayOptions();ShowLanguage();ShowDisplayOptions();
        var historyPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) }; historyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); historyPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); historyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); historyPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        historyScope.ForeColor=Ui.Muted;historyEmpty.ForeColor=Ui.Muted;historyEmpty.Dock=DockStyle.Fill;var historyBody=new Panel{Dock=DockStyle.Fill};historyBody.Controls.AddRange([history,historyEmpty]);historyPanel.Controls.Add(historyScope,0,0);historyPanel.Controls.Add(historyBody,0,1);historyPanel.Controls.Add(Ui.Bar(restore),0,2);AddTab("Lịch sử",historyPanel);history.DrawItem+=DrawHistory;history.SelectedIndexChanged+=(_,_)=>UpdateEnabled();historyPanel.SizeChanged+=(_,_)=>historyScope.MaximumSize=new Size(Math.Max(120,historyPanel.ClientSize.Width-historyPanel.Padding.Horizontal),0);
        sectionTitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);sectionTitle.AutoSize=false;sectionTitle.Height=32;sectionTitle.AutoEllipsis=true;sectionTitle.Dock=DockStyle.Top; sectionPicker.Width = 300;
        var compactActions=Ui.MenuButton("Khối",("Đưa lên",()=>MoveSection(-1)),("Đưa xuống",()=>MoveSection(1)),("Thêm khối",Add),("Xóa khối",Remove));compactActions.Enabled=canEdit;structureButtons.Add(compactActions);
        sectionPicker.AccessibleName="Khối đang biên tập";sectionPicker.DropDownWidth=420;sectionFrame=Ui.FrameInput(sectionPicker);compactSelector = Ui.Bar(sectionFrame,compactActions);compactSelector.Dock = DockStyle.Top;
        var editor = editorLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Ui.Surface, Padding = new Padding(16) }; editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); editor.RowStyles.Add(new RowStyle(SizeType.AutoSize)); editor.RowStyles.Add(new RowStyle(SizeType.AutoSize)); editor.RowStyles.Add(new RowStyle(SizeType.Percent,100)); editor.Controls.Add(sectionTitle,0,0); editor.Controls.Add(compactSelector,0,1); editor.Controls.Add(tabs,0,2);
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,240));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));split.RowStyles.Add(new RowStyle(SizeType.Percent,100));split.Controls.Add(left,0,0);split.Controls.Add(editor,1,0);
        var content = contentLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(24), BackColor = Ui.Paper }; content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); for(var i=0;i<4;i++)content.RowStyles.Add(new RowStyle(i==3?SizeType.Percent:SizeType.AutoSize,100)); content.Controls.Add(Ui.PageHeader("Landing trang chủ",canEdit?"Biên tập theo khối · Nội dung Việt / Anh":"Chế độ xem và giám sát · Chỉ nhân viên nội dung được xuất bản",save,publish),0,0); content.Controls.Add(toolbar,0,1); content.Controls.Add(Status,0,2); content.Controls.Add(split,0,3); Controls.Add(content);
        sectionsTree.BeforeSelect += (_, _) => SaveSelected(); sectionsTree.AfterSelect += (_, e) => ShowSelected(e.Node?.Index ?? -1);
        sectionPicker.SelectedIndexChanged += (_,_) => { if(!syncing && sectionPicker.SelectedIndex>=0) { SaveSelected(); ShowSelected(sectionPicker.SelectedIndex); } };
        foreach(var control in EditorControls()) { if(control is TextBox text) { text.ReadOnly=!canEdit;text.MaxLength=control==colors?80:500;text.TextChanged+=EditorChanged; } else if(control is ComboBox combo) {combo.Enabled=canEdit;combo.SelectedIndexChanged+=EditorChanged;}else if(control is NumericUpDown number){number.Enabled=canEdit;number.ValueChanged+=EditorChanged;} }
        SizeChanged += (_,_)=>ResponsiveLayout(); HandleCreated += (_,_)=>{ResponsiveLayout();Ui.Enhance(this);};
        tabs.SelectedIndexChanged+=(_,_)=>ResponsiveLayout();
        UpdateEnabled();
    }

    public override async Task RefreshAsync()
    {
        if(busy||previewBusy)return;if(IsDirty()&&!ConfirmLeave())return;var selectedId=SelectedSectionId;var selectedHistory=(history.SelectedItem as HistoryItem)?.Id;
        await LoadLatestAsync(async token =>
    {
        var data = await Api.GetAsync<JsonElement>("api/admin/landing", token); var lookups = await Api.GetAsync<JsonElement>("api/admin/lookups", token); return (data, lookups);
    }, result =>
    {
        syncing=true;
        var draft = result.data.GetProperty("draft"); revision = draft.GetProperty("revision").GetInt32(); sections = JsonSerializer.Deserialize<List<LandingSection>>(draft.GetProperty("sections"), ApiClient.Json) ?? [];
        product.Items.Clear(); product.Items.Add(new Choice("", "Không có CTA sản phẩm")); foreach (var p in Ui.Rows(result.lookups, "products")) product.Items.Add(new Choice(Ui.Text(p, "slug"), Ui.Text(p, "name")));
        LoadHistory(result.data,selectedHistory);
        savedSectionsJson=JsonSerializer.Serialize(sections,ApiClient.Json);validationProblem=null;needsReload=false;selectedIndex = -1; RebuildTree(Math.Max(0,sections.FindIndex(x=>x.Id==selectedId))); versionLabel.Text=$"Bản nháp r{revision} · {sections.Count} khối"; Status.Text = actor.Role == "owner" ? "Chỉ xem · Nhân viên nội dung phụ trách biên tập và xuất bản." : "Bản nháp đã tải · Lưu nháp chưa thay đổi nội dung công khai."; UpdateEnabled();
    });
    }
    private IEnumerable<Control> EditorControls()=>[type,product,titleVi,titleEn,subtitleVi,subtitleEn,poster,model,preset,colors,camera,tilt,light,speed,fold];
    private bool IsDirty(){SaveSelected();return actor.Role=="content_staff"&&savedSectionsJson.Length>0&&JsonSerializer.Serialize(sections,ApiClient.Json)!=savedSectionsJson;}
    private void EditorChanged(object? sender,EventArgs e){if(syncing||actor.Role!="content_staff")return;SaveSelected();if(selectedIndex>=0&&selectedIndex<sections.Count){sectionTitle.Text=$"{selectedIndex+1:00} · {sections[selectedIndex].Title.Vi}";sectionsTree.Nodes[selectedIndex].Text=NodeLabel(selectedIndex,sections[selectedIndex]);sectionsTree.Nodes[selectedIndex].ToolTipText=sections[selectedIndex].Title.Vi;sectionsTree.Invalidate();syncing=true;sectionPicker.Items[selectedIndex]=new Choice(sections[selectedIndex].Id,sectionTitle.Text);syncing=false;}if(validationProblem is{} problem&&problem.SectionId==SelectedSectionId&&ReferenceEquals(sender,problem.Field))validationProblem=null;ApplyFieldErrors();if(!busy&&!previewBusy)ShowEditStatus();}
    private void ShowEditStatus(){Status.ForeColor=validationProblem is null?Ui.Muted:Ui.Danger;Status.Text=validationProblem is{} problem?$"Khối {sections.FindIndex(x=>x.Id==problem.SectionId)+1} · {problem.Message}":IsDirty()?"Có thay đổi chưa lưu · Ctrl+S để lưu nháp.":"Bản nháp đã lưu. Nội dung công khai chưa thay đổi.";}
    private void ApplyFieldErrors(){errors.Clear();foreach(var pair in inlineErrors){var message=validationProblem is{} problem&&problem.SectionId==SelectedSectionId&&ReferenceEquals(problem.Field,pair.Key)?problem.Message:"";pair.Value.Text=message;pair.Value.Visible=message.Length>0;pair.Key.AccessibleDescription=message;errors.SetError(pair.Key,message);}}
    private void ResponsiveLayout(){if(split.ColumnStyles.Count<2||contentLayout is null)return;var compact=ClientSize.Width*96d/Math.Max(DeviceDpi,96)<800;sectionList.Visible=!compact;compactSelector.Visible=compact;sectionTitle.Visible=versionLabel.Visible=!compact;contentLayout.Padding=new Padding(compact?12:24);editorLayout.Padding=new Padding(compact?8:16);Status.Padding=compact?new Padding(20,2,0,4):new Padding(20,5,0,8);toolbar.Padding=compact?Padding.Empty:new Padding(0,4,0,2);split.ColumnStyles[0].Width=compact?0:240*DeviceDpi/96f;sectionsTree.ItemHeight=(int)(56*DeviceDpi/96f);history.ItemHeight=(int)(58*DeviceDpi/96f);foreach(var field in fieldLayouts)field.Padding=new Padding(compact?8:16);languageBar.Visible=displayBar.Visible=!compact;
        if(compact){if(localeFrame.Parent!=compactSelector)compactSelector.Controls.Add(localeFrame);if(displayModeFrame.Parent!=compactSelector)compactSelector.Controls.Add(displayModeFrame);compactSelector.Controls.SetChildIndex(localeFrame,1);compactSelector.Controls.SetChildIndex(displayModeFrame,2);sectionFrame.MinimumSize=new Size(240,0);sectionFrame.Width=240;}else{if(localeFrame.Parent!=languageBar)languageBar.Controls.Add(localeFrame);if(displayModeFrame.Parent!=displayBar)displayBar.Controls.Add(displayModeFrame);sectionFrame.MinimumSize=new Size(300,0);sectionFrame.Width=300;}localeFrame.Visible=!compact||tabs.SelectedTab==contentTab;displayModeFrame.Visible=!compact||tabs.SelectedTab==displayTab;
    }
    private void UpdateEnabled(){if(IsDisposed||Disposing)return;var ready=savedSectionsJson.Length>0;var canEdit=actor.Role=="content_staff"&&ready&&!busy&&!needsReload;foreach(var button in new[]{saveButton,publishButton,restoreButton})button.Enabled=canEdit&&!previewBusy&&(button!=restoreButton||history.SelectedItem is HistoryItem)||busy&&ReferenceEquals(button,activeWriteButton);foreach(var button in structureButtons)button.Enabled=canEdit;foreach(var c in EditorControls()){if(c is TextBox t)t.ReadOnly=!canEdit;else c.Enabled=canEdit;}sectionsTree.Enabled=sectionPicker.Enabled=!busy;history.Enabled=!busy;refreshButton.Enabled=!busy&&!previewBusy;previewButton.Enabled=ready&&!busy&&!needsReload;}
    private void RebuildTree(int select)
    {
        syncing = true; sectionsTree.BeginUpdate(); sectionsTree.Nodes.Clear(); sectionPicker.Items.Clear(); for(var i=0;i<sections.Count;i++){var s=sections[i];sectionsTree.Nodes.Add(new TreeNode(NodeLabel(i,s)){ToolTipText=s.Title.Vi});sectionPicker.Items.Add(new Choice(s.Id,$"{i+1:00} · {s.Title.Vi}"));} sectionsTree.EndUpdate();syncing = false;
        if (sections.Count > 0) { sectionsTree.SelectedNode = sectionsTree.Nodes[Math.Clamp(select, 0, sections.Count - 1)]; sectionsTree.SelectedNode.EnsureVisible(); }
    }
    private void ShowSelected(int index)
    {
        if (syncing || index < 0 || index >= sections.Count) return; syncing = true; selectedIndex = index; var s = sections[index];
        sectionsTree.SelectedNode=sectionsTree.Nodes[index];sectionPicker.SelectedIndex=index;sectionTitle.Text=$"{index+1:00} · {s.Title.Vi}";
        Set(type, s.Type); Set(product, s.ProductSlug); titleVi.Text = s.Title.Vi; titleEn.Text = s.Title.En; subtitleVi.Text = s.Subtitle.Vi; subtitleEn.Text = s.Subtitle.En; Set(poster, s.Poster); Set(model, s.Model); Set(preset, s.Preset); colors.Text = string.Join(',', s.Colors);
        camera.Value = Math.Clamp(s.Camera, camera.Minimum, camera.Maximum); tilt.Value = Math.Clamp(s.Tilt, tilt.Minimum, tilt.Maximum); light.Value = Math.Clamp(s.Light, light.Minimum, light.Maximum); speed.Value = Math.Clamp(s.Speed, speed.Minimum, speed.Maximum); fold.Value = Math.Clamp(s.Fold, fold.Minimum, fold.Maximum); syncing = false;ApplyFieldErrors();Ui.Pulse(tabs);
    }
    private void SaveSelected()
    {
        if (syncing || actor.Role != "content_staff" || selectedIndex < 0 || selectedIndex >= sections.Count) return;
        var s = sections[selectedIndex]; s.Type = Ui.Selected(type); s.ProductSlug = Ui.Selected(product); s.Title = new(titleVi.Text, titleEn.Text); s.Subtitle = new(subtitleVi.Text, subtitleEn.Text); s.Poster = Ui.Selected(poster); s.Model = Ui.Selected(model); s.Preset = Ui.Selected(preset); s.Colors = colors.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(); s.Camera = camera.Value; s.Tilt = tilt.Value; s.Light = light.Value; s.Speed = speed.Value; s.Fold = fold.Value;
    }
    internal bool SelectSection(string id){var index=sections.FindIndex(x=>x.Id==id);if(index<0){Status.Text="Khối đã thay đổi hoặc không còn trong bản nháp. Tải lại để đối chiếu.";Status.ForeColor=Ui.Danger;return false;}SaveSelected();ShowSelected(index);tabs.SelectedTab=contentTab;locale.SelectedIndex=0;return true;}
    private static Control ChoiceWorkspace(ComboBox choice,Control body,out Control frame,out FlowLayoutPanel bar){var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Ui.Surface};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));frame=Ui.FrameInput(choice);bar=Ui.Bar(frame);bar.Padding=new Padding(12,8,12,0);layout.Controls.Add(bar,0,0);layout.Controls.Add(body,0,1);return layout;}
    private void ShowLanguage(){var english=Ui.Selected(locale)=="en";viFields.Visible=!english;enFields.Visible=english;(english?enFields:viFields).BringToFront();Ui.Pulse(languageHost);}
    private void ShowDisplayOptions(){var selected=Ui.Selected(displayMode);basicFields.Visible=selected=="basic";motionFields.Visible=selected=="motion";advancedFields.Visible=selected=="advanced";(selected=="motion"?motionFields:selected=="advanced"?advancedFields:basicFields).BringToFront();Ui.Pulse(displayHost);}
    private void RevealField(Control field){if(field==titleVi||field==subtitleVi||field==titleEn||field==subtitleEn){tabs.SelectedTab=contentTab;locale.SelectedIndex=field==titleEn||field==subtitleEn?1:0;ShowLanguage();}else{tabs.SelectedTab=displayTab;displayMode.SelectedIndex=field==model||field==preset?1:field==colors||field is NumericUpDown?2:0;ShowDisplayOptions();}foreach(var layout in fieldLayouts)if(layout.Contains(field))layout.ScrollControlIntoView(field);field.Focus();}
    private void Add() { if(actor.Role!="content_staff"||busy||needsReload)return;if(sections.Count>=20){Status.Text="Landing hỗ trợ tối đa 20 khối.";return;}SaveSelected(); sections.Add(new LandingSection { Id = Guid.NewGuid().ToString(), Title = new("Khám phá sản phẩm", "Explore the product"), Subtitle = new("Thiết kế cho trải nghiệm của bạn.", "Designed for your everyday."), Poster = "/media/phone-burgundy.svg", Model = "procedural:phone" });selectedIndex=-1; RebuildTree(sections.Count - 1);tabs.SelectedTab=contentTab;locale.SelectedIndex=0;titleVi.Focus();ShowEditStatus(); }
    private void Remove() { if(actor.Role!="content_staff"||busy||needsReload)return;if (selectedIndex < 0 || sections.Count <= 1) return; if (MessageBox.Show(this, "Xóa khối này khỏi bản nháp?", "BotZone", MessageBoxButtons.YesNo) != DialogResult.Yes) return;var next=selectedIndex;if(validationProblem?.SectionId==SelectedSectionId)validationProblem=null;sections.RemoveAt(selectedIndex); selectedIndex = -1; RebuildTree(Math.Min(next,sections.Count-1));ShowEditStatus(); }
    private void MoveSection(int direction) { if(actor.Role!="content_staff"||busy||needsReload)return;SaveSelected(); var next = selectedIndex + direction; if (next < 0 || next >= sections.Count || selectedIndex < 0) return; (sections[selectedIndex], sections[next]) = (sections[next], sections[selectedIndex]); selectedIndex = -1; RebuildTree(next);ShowEditStatus(); }
    private bool ValidateDraft()
    {
        validationProblem=null;ApplyFieldErrors();
        for(var i=0;i<sections.Count;i++)
        {
            var s=sections[i];Control? invalid=null;string message="";
            foreach(var item in new[]{(titleVi,s.Title.Vi,"tiêu đề tiếng Việt"),(subtitleVi,s.Subtitle.Vi,"mô tả tiếng Việt"),(titleEn,s.Title.En,"tiêu đề tiếng Anh"),(subtitleEn,s.Subtitle.En,"mô tả tiếng Anh")})
                if(string.IsNullOrWhiteSpace(item.Item2)||item.Item2.Trim().Length>500){invalid=item.Item1;message=$"Nhập {item.Item3} từ 1 đến 500 ký tự.";break;}
            if(invalid is null&&s.Type is "hero" or "duo"&&string.IsNullOrWhiteSpace(s.ProductSlug)){invalid=product;message="Chọn sản phẩm cho khối Hero / Duo.";}
            if(invalid is null&&string.IsNullOrWhiteSpace(s.Poster)){invalid=poster;message="Chọn ảnh đại diện cho khối nội dung.";}
            if(invalid is null&&string.IsNullOrWhiteSpace(s.Model)){invalid=model;message="Chọn mô hình 3D cho khối nội dung.";}
            if(invalid is null&&(s.Colors.Count is <1 or >8||s.Colors.Any(c=>!System.Text.RegularExpressions.Regex.IsMatch(c,"^#[0-9a-fA-F]{6}$")))){invalid=colors;message="Nhập từ 1 đến 8 mã màu dạng #RRGGBB.";}
            if(invalid is null&&s.Type=="duo"&&s.Model is "procedural:phone" or "v003:iphone-18-pro-max"){invalid=model;message="Khối Duo cần mô hình 3D hỗ trợ mở/gập.";}
            if(invalid is null&&s.Preset=="fold"&&s.Model is "procedural:phone" or "v003:iphone-18-pro-max"){invalid=preset;message="Mẫu mở/gập cần mô hình Duo. Chọn Nghiêng nhẹ hoặc Xoay cho điện thoại thường.";}
            if(invalid is null&&s.Type is "hero" or "duo"&&s.Model is "v003:iphone-18-pro-max" or "v003:iphone-duo"&&s.ProductSlug!=s.Model[5..]){invalid=product;message="Sản phẩm liên kết phải khớp mô hình 3D v003 đã chọn.";}
            if(invalid is null)continue;validationProblem=new FieldProblem(s.Id,invalid,message);ShowSelected(i);ApplyFieldErrors();
            RevealField(invalid);Status.Text=$"Khối {i+1} · {message}";Status.ForeColor=Ui.Danger;return false;
        }
        return true;
    }
    private async Task<bool> SendAsync(string command)
    {
        if (busy || previewBusy || needsReload || savedSectionsJson.Length==0 || actor.Role != "content_staff") return false; SaveSelected(); if(command!="restore_landing"&&!ValidateDraft())return false;
        if(command is "publish_landing" or "restore_landing"&&IsDirty()){Status.Text="Có thay đổi chưa lưu. Lưu nháp trước khi xuất bản hoặc khôi phục.";saveButton.Focus();return false;}
        if (command == "restore_landing" && history.SelectedItem is not HistoryItem) { Status.Text = "Chọn phiên bản trong lịch sử cần khôi phục.";history.Focus(); return false; }
        if (command is "publish_landing" or "restore_landing" && MessageBox.Show(this, command == "publish_landing" ? $"Xuất bản trang chủ từ bản nháp r{revision}, gồm {sections.Count} khối Việt / Anh? Nội dung công khai sẽ đổi." : $"Tạo phiên bản công khai mới từ {((HistoryItem)history.SelectedItem!).Label}? Lịch sử được giữ nguyên.", "Xác nhận nội dung", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
        object input = command switch { "save_landing" => new { pageSlug = "home", revision, sections }, "restore_landing" => new { pageSlug = "home", revision, versionId = ((HistoryItem)history.SelectedItem!).Id }, _ => new { pageSlug = "home", revision } };
        var button=command=="save_landing"?saveButton:command=="publish_landing"?publishButton:restoreButton;var message=command=="save_landing"?"Đang lưu nháp…":command=="publish_landing"?"Đang xuất bản…":"Đang khôi phục…";
        busy = true;activeWriteButton=button;using var feedback=Ui.Busy(button,message);UpdateEnabled();Status.Text=message;Status.ForeColor=Ui.Muted;var confirmed=false;
        try
        {
            // Leaving a view does not cancel a committed write. Keep the same pending key until confirmation.
            var result=await Api.CommandAsync(command,input,keys.For(command,input));keys.Succeeded(command,input);confirmed=true;if(IsDisposed||Disposing)return true;
            if(result.TryGetProperty("revision",out var r))revision=r.GetInt32();
            if(command=="save_landing") { savedSectionsJson=JsonSerializer.Serialize(sections,ApiClient.Json);versionLabel.Text=$"Bản nháp r{revision} · {sections.Count} khối · Đã lưu";Status.Text=$"Đã lưu bản nháp r{revision}. Nội dung công khai chưa thay đổi."; }
            else
            {
                needsReload=true;var data=await Api.GetAsync<JsonElement>("api/admin/landing",lifetime.Token);if(IsDisposed||Disposing)return true;
                var sectionId=SelectedSectionId;syncing=true;var draft=data.GetProperty("draft");sections=JsonSerializer.Deserialize<List<LandingSection>>(draft.GetProperty("sections"),ApiClient.Json)??[];revision=draft.GetProperty("revision").GetInt32();savedSectionsJson=JsonSerializer.Serialize(sections,ApiClient.Json);validationProblem=null;LoadHistory(data);selectedIndex=-1;RebuildTree(Math.Max(0,sections.FindIndex(x=>x.Id==sectionId)));needsReload=false;versionLabel.Text=$"Bản nháp r{revision} · {sections.Count} khối";Status.Text=command=="publish_landing"?"Đã xuất bản trang chủ Việt / Anh.":"Đã khôi phục thành phiên bản công khai mới.";
            }
            return true;
        }
        catch (Exception ex) { if(!IsDisposed&&!Disposing){Status.Text=confirmed?"Lệnh đã được dịch vụ xác nhận, nhưng chưa tải được dữ liệu mới. Chọn Tải lại để đối chiếu; không gửi lại lệnh.":Ui.Error(ex)+" Dữ liệu đang nhập được giữ lại.";Status.ForeColor=Ui.Danger;}return false; }
        finally { busy = false;activeWriteButton=null;UpdateEnabled(); }
    }
    private async Task PreviewAsync(string viewport)
    {
        if(busy||previewBusy||needsReload||savedSectionsJson.Length==0)return;if(IsDirty()){Status.Text="Lưu nháp trước khi mở xem trước của bản này.";saveButton.Focus();return;}
        var requestedRevision=revision;previewBusy=true;using var feedback=Ui.Busy(previewButton,"Đang mở xem trước…");UpdateEnabled();Status.Text="Đang mở bản nháp đã lưu để xem trước…";Status.ForeColor=Ui.Muted;
        try { var input = new { revision=requestedRevision, viewport }; var result = await Api.CommandAsync("preview_landing", input, keys.For("preview_landing", input));keys.Succeeded("preview_landing",input);if(IsDisposed||Disposing)return; var url = Ui.Text(result, "url"); if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Dịch vụ chưa trả địa chỉ xem trước hợp lệ."); Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });Status.Text=$"Đã mở bản nháp r{requestedRevision} · {(viewport=="mobile"?"Di động":"Laptop")}."+(IsDirty()?" Có thay đổi cục bộ chưa lưu.":""); } catch (Exception ex) {if(!IsDisposed&&!Disposing){ Status.Text = Ui.Error(ex);Status.ForeColor=Ui.Danger;} }finally{previewBusy=false;UpdateEnabled();}
    }
    private TabPage AddTab(string title, Control content) { var p = new TabPage(title){BackColor=Ui.Surface}; p.Controls.Add(content); tabs.TabPages.Add(p);return p; }
    private TableLayoutPanel Fields(params (string Label, Control Control)[] items)
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoScroll = true, Padding = new Padding(16), BackColor=Ui.Surface,Tag="landing-fields" }; p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var item in items)
        {
            var field=string.IsNullOrEmpty(item.Label)?item.Control:Ui.Field(item.Label,item.Control);field.Dock=DockStyle.Top;field.Margin=new Padding(0,0,8,4);p.RowStyles.Add(new RowStyle(SizeType.AutoSize));p.Controls.Add(field,0,p.RowCount++);
            if(item.Control is TextBox or ComboBox or NumericUpDown){var error=Ui.Label("");error.Visible=false;error.Dock=DockStyle.Top;error.Margin=new Padding(0,0,8,4);error.ForeColor=Ui.Danger;error.Font=new Font("Segoe UI",9);error.AccessibleName="Lỗi: "+item.Label;inlineErrors[item.Control]=error;p.RowStyles.Add(new RowStyle(SizeType.AutoSize));p.Controls.Add(error,0,p.RowCount++);p.SizeChanged+=(_,_)=>error.MaximumSize=new Size(Math.Max(80,p.ClientSize.Width-p.Padding.Horizontal-24),0);}
        }
        fieldLayouts.Add(p);return p;
    }
    private static NumericUpDown Number(decimal min, decimal max, decimal value) => new() { Minimum = min, Maximum = max, Value = value, DecimalPlaces = 1, Increment = .1m };
    private static void Set(ComboBox c, string value) { for (var i = 0; i < c.Items.Count; i++) if ((c.Items[i] as Choice)?.Value == value) { c.SelectedIndex = i; return; } c.Items.Add(new Choice(value, value.Length==0?"Chưa chọn":value)); c.SelectedIndex = c.Items.Count - 1; }
    private sealed record FieldProblem(string SectionId,Control Field,string Message);
    private sealed record HistoryItem(string Id, string Label,string Creator);
    private void LoadHistory(JsonElement data,string? selectedId=null){history.BeginUpdate();history.Items.Clear();foreach(var h in Ui.Rows(data,"history"))history.Items.Add(new HistoryItem(Ui.Text(h,"id","versionId"),HistoryLabel(h),Ui.Text(h,"createdBy")));history.SelectedIndex=selectedId is null?-1:history.Items.Cast<HistoryItem>().ToList().FindIndex(x=>x.Id==selectedId);history.EndUpdate();history.Visible=history.Items.Count>0;historyEmpty.Visible=history.Items.Count==0;}
    private void DrawHistory(object? sender,DrawItemEventArgs e){if(IsDisposed||Disposing||!history.Visible||e.Bounds.Width<=0||e.Bounds.Height<=0||e.Index<0||e.Index>=history.Items.Count)return;var item=(HistoryItem)history.Items[e.Index];var selected=(e.State&DrawItemState.Selected)!=0;using var brush=new SolidBrush(selected?Ui.Soft:Ui.Surface);e.Graphics.FillRectangle(brush,e.Bounds);var pad=(int)(10*DeviceDpi/96f);var bounds=new Rectangle(e.Bounds.X+pad,e.Bounds.Y+pad,Math.Max(1,e.Bounds.Width-pad*2),22*DeviceDpi/96);TextRenderer.DrawText(e.Graphics,item.Label,Font,bounds,selected?Ui.Primary:Ui.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);bounds.Y+=22*DeviceDpi/96;TextRenderer.DrawText(e.Graphics,"Xuất bản bởi "+(item.Creator.Length>0?item.Creator:"—"),Font,bounds,Ui.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();}
    private static string HistoryLabel(JsonElement item){var value=Ui.Text(item,"publishedAt","created_at");return $"Phiên bản {Ui.Text(item,"versionNo","version_no")} · "+(DateTimeOffset.TryParse(value,out var date)?Ui.VietDate(date):value);}
    private static string NodeLabel(int index,LandingSection section)=>$"{index+1:00} · "+(section.Title.Vi.Length>18?section.Title.Vi[..18]+"…":section.Title.Vi);
    private void DrawSection(object? sender,DrawTreeNodeEventArgs e)
    {
        if(IsDisposed||Disposing||!sectionsTree.Visible||sectionsTree.ClientSize.Width<=0||e.Bounds.Height<=0||e.Node is null||e.Node.Index<0||e.Node.Index>=sections.Count)return;var section=sections[e.Node.Index];var selected=e.Node==sectionsTree.SelectedNode;var bounds=new Rectangle(0,e.Bounds.Y,sectionsTree.ClientSize.Width,e.Bounds.Height);using var background=new SolidBrush(selected?Ui.Soft:Ui.Surface);e.Graphics.FillRectangle(background,bounds);if(selected&&bounds.Height>16){using var marker=new SolidBrush(Ui.Primary);e.Graphics.FillRectangle(marker,0,bounds.Top+8,3,bounds.Height-16);}
        var inset=(int)(10*DeviceDpi/96f);var numberWidth=(int)(28*DeviceDpi/96f);using var titleFont=new Font("Segoe UI",10,FontStyle.Bold);using var detailFont=new Font("Segoe UI",9);
        TextRenderer.DrawText(e.Graphics,$"{e.Node.Index+1:00}",detailFont,new Rectangle(inset,bounds.Top+inset,numberWidth,bounds.Height-inset),selected?Ui.Primary:Ui.Muted,TextFormatFlags.Top|TextFormatFlags.Left);
        var textBounds=new Rectangle(inset+numberWidth,bounds.Top+inset,Math.Max(1,bounds.Width-numberWidth-inset*2),24*DeviceDpi/96);TextRenderer.DrawText(e.Graphics,section.Title.Vi,titleFont,textBounds,Ui.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        var kind=section.Type switch{"hero"=>"Hero","duo"=>"Duo","collection"=>"Bộ sưu tập",_=>"Lợi ích"};var complete=!string.IsNullOrWhiteSpace(section.Title.Vi)&&!string.IsNullOrWhiteSpace(section.Title.En)&&!string.IsNullOrWhiteSpace(section.Subtitle.Vi)&&!string.IsNullOrWhiteSpace(section.Subtitle.En);
        textBounds.Y+=24*DeviceDpi/96;TextRenderer.DrawText(e.Graphics,kind+" · "+(complete?"VI / EN":"Cần hoàn thiện"),detailFont,textBounds,complete?Ui.Muted:Ui.Warning,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        if(selected&&sectionsTree.Focused)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(bounds,-2,-2),Ui.Focus,Ui.Soft);
    }
    private sealed class LandingSection
    {
        [System.Text.Json.Serialization.JsonExtensionData] public Dictionary<string,JsonElement>? AdditionalConfiguration{get;set;}
        public string Id { get; set; } = ""; public string Type { get; set; } = "hero"; public Bilingual Title { get; set; } = new("", ""); public Bilingual Subtitle { get; set; } = new("", ""); public string ProductSlug { get; set; } = ""; public string Poster { get; set; } = ""; public string Model { get; set; } = ""; public List<string> Colors { get; set; } = ["#5b2333", "#b7d8e5"]; public string Preset { get; set; } = "float"; public decimal Camera { get; set; } = 5; public decimal Tilt { get; set; } = 12; public decimal Light { get; set; } = 2; public decimal Speed { get; set; } = 1; public decimal Fold { get; set; } = 70;
    }
    private sealed record Bilingual(string Vi, string En);
    internal override bool HasPendingWrite=>busy;
    internal override bool CanLeave()
    {
        if(busy)return true;return !IsDirty()||ConfirmLeave();
    }
    private bool ConfirmLeave()
    {
        using var dialog=new Form{Text="Thay đổi chưa lưu",StartPosition=FormStartPosition.CenterParent,Size=new Size(680,310),MinimumSize=new Size(480,240),MaximizeBox=false,MinimizeBox=false};
        var note=Ui.Label("Bản nháp đang sửa chưa được lưu. Chọn cách tiếp tục.");
        var discard=Ui.Button("Bỏ thay đổi",(_,_)=>{var id=SelectedSectionId;sections=JsonSerializer.Deserialize<List<LandingSection>>(savedSectionsJson,ApiClient.Json)??[];validationProblem=null;selectedIndex=-1;RebuildTree(Math.Max(0,sections.FindIndex(x=>x.Id==id)));Status.ForeColor=Ui.Muted;Status.Text="Đã bỏ thay đổi cục bộ. Bản nháp đã lưu được giữ nguyên.";dialog.DialogResult=DialogResult.OK;dialog.Close();});
        var stay=Ui.Button("Ở lại",(_,_)=>{dialog.DialogResult=DialogResult.Cancel;dialog.Close();});
        var save=Ui.Button("Lưu rồi tiếp tục",async(sender,_)=>{if(busy)return;using var feedback=Ui.Busy((Control)sender!,"Đang lưu…");discard.Enabled=stay.Enabled=false;if(await SendAsync("save_landing")){dialog.DialogResult=DialogResult.OK;dialog.Close();}else{note.Text=Status.Text;note.ForeColor=Ui.Danger;discard.Enabled=stay.Enabled=true;}},true);
        dialog.Controls.Add(Ui.Vertical(Ui.PageHeader("Giữ lại thay đổi của bạn",$"Landing trang chủ · bản nháp r{revision}"),note,Ui.Bar(save,discard,stay)));dialog.CancelButton=stay;dialog.FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};Ui.Prepare(dialog);return dialog.ShowDialog(FindForm())==DialogResult.OK;
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Control|Keys.S)&&actor.Role=="content_staff"){_=SendAsync("save_landing");return true;}return base.ProcessCmdKey(ref msg,keyData);}
    protected override void Dispose(bool disposing){if(disposing&&!cleaned){cleaned=true;lifetime.Cancel();lifetime.Dispose();errors.Dispose();previewMenu.Dispose();}base.Dispose(disposing);}
}

internal sealed class CommandKeys
{
    private readonly Dictionary<string, string> pending = [];
    private static string Identity(string command, object input) => command + ":" + JsonSerializer.Serialize(input, ApiClient.Json);
    public string For(string command, object input) { var id = Identity(command, input); if (!pending.TryGetValue(id, out var key)) { key = Guid.NewGuid().ToString(); pending[id] = key; } return key; }
    public void Succeeded(string command, object input) => pending.Remove(Identity(command, input));
}
