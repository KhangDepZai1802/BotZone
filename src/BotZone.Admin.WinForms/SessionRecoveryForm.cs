using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

// Uses the existing client/cookie jar. A successful sign-in never replays the
// failed request, particularly a financial or publishing command.
internal sealed class SessionRecoveryForm : Form
{
    private readonly ApiClient api;
    private readonly Actor previous;
    private readonly TextBox password=new(){UseSystemPasswordChar=true,Width=390};
    private readonly Label status=Ui.Label("Bản nháp và vị trí đang làm việc vẫn được giữ.");
    private readonly Button signIn;
    private readonly Button cancel;
    private readonly ErrorProvider errors=new();
    private CancellationTokenSource? pending;
    private bool signingIn;
    internal Actor? RestoredActor{get;private set;}
    internal SessionRecoveryForm(ApiClient api,Actor previous)
    {
        this.api=api;this.previous=previous;
        Text="Tiếp tục phiên làm việc";Size=new Size(580,460);MinimumSize=new Size(480,400);StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;Ui.Prepare(this);errors.ContainerControl=this;
        var identity=Ui.Label(previous.Name+"\n"+previous.Email);identity.ForeColor=Ui.Muted;
        var show=new CheckBox{Text="Hiện mật khẩu",AutoSize=true,Margin=new Padding(0,4,0,12)};show.CheckedChanged+=(_,_)=>password.UseSystemPasswordChar=!show.Checked;
        signIn=Ui.Button("Đăng &nhập lại",async(_,_)=>await SignInAsync(),true);
        cancel=Ui.Button("Để sau",(_,_)=>Close());cancel.DialogResult=DialogResult.Cancel;CancelButton=cancel;AcceptButton=signIn;
        status.MaximumSize=new Size(440,0);status.ForeColor=Ui.Muted;
        var fields=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(var control in new Control[]{Ui.Label("Tiếp tục công việc",true),identity,Ui.Field("&Mật khẩu",password),show,status,Ui.Bar(signIn,cancel)}){fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));control.Dock=DockStyle.Fill;control.TabIndex=fields.RowCount;fields.Controls.Add(control,0,fields.RowCount++);}
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(28)};scroll.Controls.Add(fields);Controls.Add(scroll);Shown+=(_,_)=>password.Focus();
        FormClosing+=(_,e)=>{if(signingIn&&RestoredActor is null){e.Cancel=true;status.Text="Đang xác thực tài khoản. Vui lòng chờ kết quả.";}};
    }
    internal void FillPassword(string value)=>password.Text=value;
    internal async Task SignInAsync()
    {
        if(signingIn)return;errors.Clear();
        if(string.IsNullOrWhiteSpace(password.Text)){errors.SetError(password,"Nhập mật khẩu để tiếp tục.");password.Focus();return;}
        signingIn=true;cancel.Enabled=false;using var busy=Ui.Busy(signIn,"Đang xác thực…");status.Text="Đang xác thực tài khoản…";status.ForeColor=Ui.Muted;pending?.Dispose();pending=new();
        try
        {
            var fresh=await api.LoginAsync(previous.Email,password.Text,pending.Token);
            if(IsDisposed||Disposing)return;
            if(fresh.Id!=previous.Id||fresh.Role=="customer")
            {
                await api.LogoutAsync(pending.Token);
                throw new ApiException(403,"IDENTITY_CHANGED","Tài khoản này không thể khôi phục phiên quản trị hiện tại.");
            }
            RestoredActor=fresh;password.Clear();DialogResult=DialogResult.OK;Close();
        }
        catch(OperationCanceledException){if(!IsDisposed)status.Text="Đã dừng đăng nhập. Bản nháp vẫn được giữ.";}
        catch(Exception ex){if(!IsDisposed){status.Text=Ui.Error(ex);status.ForeColor=Ui.Danger;password.Focus();password.SelectAll();}}
        finally{signingIn=false;if(!IsDisposed)cancel.Enabled=true;}
    }
    protected override void Dispose(bool disposing){if(disposing){pending?.Cancel();pending?.Dispose();pending=null;errors.Dispose();}base.Dispose(disposing);}
}
