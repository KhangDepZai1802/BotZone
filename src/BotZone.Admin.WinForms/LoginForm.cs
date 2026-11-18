using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

internal sealed class LoginForm : Form
{
    private readonly TextBox url = new() { Text = Environment.GetEnvironmentVariable("BOTZONE_API_URL") ?? "http://localhost:5080", Dock = DockStyle.Fill };
    private readonly TextBox email = new() { PlaceholderText = "ten@botzone.local", Dock = DockStyle.Fill };
    private readonly TextBox password = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    private readonly Label status = Ui.Label("Đăng nhập bằng tài khoản nhân viên của cửa hàng.");
    private readonly ErrorProvider errors = new();
    private readonly Button login;
    private Control connectionSettings=null!;
    private CancellationTokenSource? pending;
    private ApiClient? api;
    private bool signingIn;
    private readonly TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly Panel brand = new() { Dock = DockStyle.Fill, BackColor = Ui.Rail, Padding = new Padding(40) };
    internal MainForm? ActiveMain { get; private set; }
    internal ApiClient? AuthenticatedApi => api;
    internal string LoginStatus => status.Text;
    internal void FillCredentials(string apiUrl, string emailAddress, string secret) { url.Text=apiUrl; email.Text=emailAddress; password.Text=secret; }

    public LoginForm()
    {
        Text = "BotZone — Đăng nhập quản trị"; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(560, 480); Size = new Size(1120, 720); Ui.Prepare(this);
        errors.ContainerControl = this;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var brandLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4};brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent,25));brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent,75));brandLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var logo=new BrandMark{Size=new Size(224,64)};brandLayout.Controls.Add(logo,0,0);
        var statement=Ui.Label("Mọi việc.\nĐúng chỗ.",true);statement.Font=new Font("Segoe UI",34,FontStyle.Bold);statement.ForeColor=Color.White;brandLayout.Controls.Add(statement,0,1);
        var intro=Ui.Label("Một không gian vận hành.\nBốn vai trò rõ ràng.");intro.ForeColor=Ui.RailMuted;intro.Margin=new Padding(0,24,0,0);brandLayout.Controls.Add(intro,0,2);
        var footer=Ui.Label("Được thiết kế cho công việc mỗi ngày.");footer.ForeColor=Ui.RailMuted;brandLayout.Controls.Add(footer,0,3);brand.Controls.Add(brandLayout);layout.Controls.Add(brand,0,0);
        var host=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(40,36,40,24)};
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0,12,0,0) }; fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        login = Ui.Button("Đăng &nhập", async (_, _) => await LoginAsync(), true); AcceptButton = login;
        login.Dock=DockStyle.Fill;login.MinimumSize=new Size(0,44);email.AccessibleName="Email nhân viên";password.AccessibleName="Mật khẩu";url.AccessibleName="Địa chỉ dịch vụ BotZone";
        var showPassword=new CheckBox{Text="Hiện mật khẩu",AutoSize=true,Margin=new Padding(0,8,0,12)};showPassword.CheckedChanged+=(_,_)=>password.UseSystemPasswordChar=!showPassword.Checked;
        var caps=Ui.Label("");caps.ForeColor=Ui.Warning;caps.MinimumSize=new Size(0,24);password.KeyUp+=(_,_)=>caps.Text=IsKeyLocked(Keys.CapsLock)?"Caps Lock đang bật":"";
        var config=Ui.Field("Địa chỉ dịch vụ",url);connectionSettings=config;config.Visible=false;var configure=new LinkLabel{Text="Thiết lập kết nối",AutoSize=true,TextAlign=ContentAlignment.MiddleCenter,LinkColor=Ui.Muted,ActiveLinkColor=Ui.Primary,VisitedLinkColor=Ui.Muted,LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(0,12,0,0)};configure.LinkClicked+=(_,_)=>{config.Visible=!config.Visible;Ui.Pulse(fields);if(config.Visible){url.Focus();RevealConnectionSettings();}};
        var eyebrow=Ui.Label("ỨNG DỤNG QUẢN TRỊ");eyebrow.ForeColor=Ui.Primary;eyebrow.Margin=new Padding(0,0,0,18);
        status.Text="Sử dụng tài khoản nhân viên được cấp.";status.ForeColor=Ui.Muted;status.MinimumSize=new Size(0,52);status.MaximumSize=new Size(560,0);
        foreach (var control in new Control[] { eyebrow,Ui.Label("Chào mừng trở lại.",true),status,Ui.Field("&Email nhân viên",email),Ui.Field("&Mật khẩu",password),showPassword,caps,login,Ui.Label("Quyền truy cập được xác định theo tài khoản."),configure,config }) { fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));control.Dock=DockStyle.Fill;control.TabIndex=fields.RowCount;fields.Controls.Add(control, 0, fields.RowCount++); }
        host.Controls.Add(fields);layout.Controls.Add(host,1,0);Controls.Add(layout);
        Resize+=(_,_)=>{var compact=ClientSize.Width*96/DeviceDpi<850;brand.Visible=!compact;layout.ColumnStyles[0].SizeType=SizeType.Percent;layout.ColumnStyles[0].Width=compact?0:42;layout.ColumnStyles[1].Width=compact?100:58;};
        Shown+=(_,_)=>email.Focus();
        FormClosed += (_, _) => { pending?.Cancel(); pending?.Dispose(); api?.Dispose(); errors.Dispose(); };
    }

    private async Task LoginAsync()
    {
        if(signingIn)return;
        errors.Clear();
        if (!Uri.TryCreate(url.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) { connectionSettings.Visible=true;status.Text="Kiểm tra địa chỉ dịch vụ bên dưới.";status.ForeColor=Ui.Danger;errors.SetError(url, "Nhập URL API HTTP/HTTPS."); url.Focus();RevealConnectionSettings();return; }
        if (!email.Text.Contains('@')) { errors.SetError(email, "Nhập email nhân viên."); email.Focus(); return; }
        if (string.IsNullOrEmpty(password.Text)) { errors.SetError(password, "Nhập mật khẩu."); password.Focus(); return; }
        signingIn=true;using var busy=Ui.Busy(login,"Đang đăng nhập…"); status.Text = "Đang xác thực…"; status.ForeColor = Ui.Muted;
        pending?.Dispose(); pending = new CancellationTokenSource(); api?.Dispose(); api = new ApiClient(url.Text.Trim());
        try
        {
            var actor = await api.LoginAsync(email.Text.Trim(), password.Text, pending.Token);
            if(IsDisposed||Disposing)return;
            if (actor.Role == "customer") { await api.LogoutAsync(pending.Token); throw new ApiException(403, "FORBIDDEN", "Tài khoản khách không có quyền mở ứng dụng quản trị."); }
            password.Clear(); Hide();
            using var main = new MainForm(api, actor);
            ActiveMain=main;
            await main.ShowDialogAsync();
            ActiveMain=null;
            Show(); status.Text = "Đã kết thúc phiên. Đăng nhập để tiếp tục."; email.Focus();
        }
        catch (Exception ex) { if(!IsDisposed){status.Text = Ui.Error(ex); status.ForeColor = Ui.Danger;} }
        finally { signingIn=false; }
    }
    private void RevealConnectionSettings()
    {
        for(Control? parent=connectionSettings.Parent;parent is not null;parent=parent.Parent)
            if(parent is ScrollableControl {AutoScroll:true} scroll){scroll.PerformLayout();scroll.ScrollControlIntoView(connectionSettings);break;}
    }
}
