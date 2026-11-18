using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Admin.WinForms;

public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class ApiClient : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient client;
    private readonly SemaphoreSlim csrfLock = new(1, 1);
    private string? csrf;
    private long sessionVersion;
    internal long SessionVersion=>Interlocked.Read(ref sessionVersion);
    public Uri BaseUri { get; }
    public Actor? Actor { get; private set; }
    internal event EventHandler? SessionExpired;
    internal CommandKeys PendingCommands{get;}=new();

    public ApiClient(string baseUrl)
    {
        BaseUri = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        if (BaseUri.Scheme is not ("http" or "https")) throw new ArgumentException("API URL phải dùng HTTP/HTTPS.");
        var handler = new HttpClientHandler { CookieContainer = new CookieContainer(), UseCookies = true, AllowAutoRedirect = false };
        client = new HttpClient(handler) { BaseAddress = BaseUri, Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Add("X-BotZone-Client", "winforms");
    }

    public async Task RefreshCsrfAsync(CancellationToken token = default)
    {
        await csrfLock.WaitAsync(token);
        try
        {
            using var response = await client.GetAsync("api/auth/csrf", token);
            await CheckAsync(response, token);
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(Json, token);
            csrf = data.GetProperty("token").GetString() ?? throw new ApiException(500, "CSRF", "API chưa trả request token.");
        }
        finally { csrfLock.Release(); }
    }

    public async Task<Actor> LoginAsync(string email, string password, CancellationToken token = default)
    {
        await RefreshCsrfAsync(token);
        var session = await PostAsync<SessionResponse>("api/auth/login", new LoginRequest(email, password, "winforms"), token);
        Actor = session.Actor ?? throw new ApiException(401, "AUTH_REQUIRED", "Không nhận được phiên đăng nhập.");
        Interlocked.Increment(ref sessionVersion);
        await RefreshCsrfAsync(token);
        return Actor;
    }

    public async Task<Actor?> SessionAsync(CancellationToken token = default)
    {
        var version=SessionVersion;var session=await GetAsync<SessionResponse>("api/session", token);
        if(version!=SessionVersion)return Actor;
        Actor = session.Actor;
        if(Actor is null)SessionExpired?.Invoke(this,EventArgs.Empty);
        return Actor;
    }

    public async Task LogoutAsync(CancellationToken token = default)
    {
        await PostAsync<JsonElement>("api/auth/logout", new { }, token);
        Actor = null;
        Interlocked.Increment(ref sessionVersion);
        csrf = null;
        await RefreshCsrfAsync(token);
    }

    public async Task<T> GetAsync<T>(string route, CancellationToken token = default)
    {
        var version=SessionVersion;using var response = await client.GetAsync(SameOrigin(route), token);
        await CheckAsync(response, token,version);
        return (await response.Content.ReadFromJsonAsync<T>(Json, token))!;
    }

    public async Task<T> PostAsync<T>(string route, object body, CancellationToken token = default)
    {
        if (csrf is null) await RefreshCsrfAsync(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, SameOrigin(route)) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        var version=SessionVersion;using var response = await client.SendAsync(request, token);
        await CheckAsync(response, token,version);
        if (response.StatusCode == HttpStatusCode.NoContent) return default!;
        return (await response.Content.ReadFromJsonAsync<T>(Json, token))!;
    }

    public Task<JsonElement> CommandAsync(string command, object input, string key, CancellationToken token = default) =>
        PostAsync<JsonElement>("api/commands", new CommandRequest(command, key, JsonSerializer.SerializeToElement(input, Json)), token);

    public async Task DownloadAsync(string route, string filename, CancellationToken token = default)
    {
        var version=SessionVersion;using var response = await client.GetAsync(SameOrigin(route), HttpCompletionOption.ResponseHeadersRead, token);
        await CheckAsync(response, token,version);
        await using var source = await response.Content.ReadAsStreamAsync(token);
        var temporary = filename + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            await using (var target = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
                await source.CopyToAsync(target, token);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, filename, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public async Task<JsonElement> UploadAsync(string filename, string visibility, string sourceUrl, string licenseNote, CancellationToken token = default)
    {
        if (csrf is null) await RefreshCsrfAsync(token);
        using var body = new MultipartFormDataContent();
        await using var stream = File.OpenRead(filename);
        body.Add(new StreamContent(stream), "file", Path.GetFileName(filename));
        body.Add(new StringContent(visibility), "visibility");
        body.Add(new StringContent(sourceUrl), "sourceUrl");
        body.Add(new StringContent(licenseNote), "licenseNote");
        using var request = new HttpRequestMessage(HttpMethod.Post, SameOrigin("api/media")) { Content = body };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        var version=SessionVersion;using var response = await client.SendAsync(request, token);
        await CheckAsync(response, token,version);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json, token);
    }

    private Uri SameOrigin(string route)
    {
        var uri = new Uri(BaseUri, route);
        if (uri.Scheme != BaseUri.Scheme || uri.Host != BaseUri.Host || uri.Port != BaseUri.Port)
            throw new ApiException(400, "INVALID_DOWNLOAD", "Đường dẫn phải thuộc API BotZone đã cấu hình.");
        return uri;
    }

    private async Task CheckAsync(HttpResponseMessage response, CancellationToken token,long? version=null)
    {
        if (response.IsSuccessStatusCode) return;
        var status = (int)response.StatusCode;
        if(status==401&&(version is null||version==SessionVersion)&&response.RequestMessage?.RequestUri?.AbsolutePath.StartsWith("/api/auth/",StringComparison.OrdinalIgnoreCase)!=true)
            SessionExpired?.Invoke(this,EventArgs.Empty);
        var message = status switch { 401 => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", 403 => "Bạn không có quyền thực hiện thao tác này.", 409 => "Dữ liệu đã thay đổi hoặc thao tác không còn hợp lệ. Hãy tải lại.", _ => "API chưa xử lý được yêu cầu. Vui lòng thử lại." };
        var code = "HTTP_" + status;
        try
        {
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(Json, token);
            if (data.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var m)) message = m.GetString() ?? message;
                if (error.TryGetProperty("code", out var c)) code = c.GetString() ?? code;
            }
        }
        catch (JsonException) { }
        throw new ApiException(status, code, message);
    }

    public void Dispose() { client.Dispose(); csrfLock.Dispose(); }
    private sealed record SessionResponse(Actor? Actor);
}
