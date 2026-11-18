using System.Text.Json;
using BotZone.Api;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Data.Entities;
using BotZone.Infrastructure.Invoices;
using BotZone.Infrastructure.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
var builder=WebApplication.CreateBuilder(args);
var root=Environment.GetEnvironmentVariable("BOTZONE_ROOT")??Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,"../.."));
Environment.SetEnvironmentVariable("BOTZONE_ROOT",root);
builder.Configuration.AddJsonFile(Path.Combine(root,"config/backend.local.json"),optional:true,reloadOnChange:false).AddEnvironmentVariables();
builder.Services.AddDbContext<BotZoneDbContext>(o=>o.UseNpgsql(builder.Configuration.GetConnectionString("BotZone")??throw new InvalidOperationException("Configure backend ConnectionStrings:BotZone.")));
builder.Services.AddScoped<SqlStore>();builder.Services.AddScoped<ReadService>();builder.Services.AddScoped<OperationalReportsService>();builder.Services.AddScoped<InvoiceQueryService>();
builder.Services.AddSingleton<ProductPhotoCatalog>();
builder.Services.AddScoped<CustomerReadService>();
builder.Services.AddScoped<AfterSalesService>();builder.Services.AddScoped<CommerceService>();builder.Services.AddScoped<CommandService>();builder.Services.AddScoped<InventoryService>();builder.Services.AddScoped<LandingService>();builder.Services.AddScoped<MediaService>();
builder.Services.AddHostedService<LocalWorker>();
builder.Services.AddIdentityCore<User>(o=>{
    o.User.RequireUniqueEmail=true;
    o.Password.RequiredLength=6;
    o.Password.RequireUppercase=false;
    o.Password.RequireLowercase=false;
    o.Password.RequireDigit=false;
    o.Password.RequireNonAlphanumeric=false;
    o.Password.RequiredUniqueChars=1;
    o.Lockout.MaxFailedAccessAttempts=5;
    o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<BotZoneDbContext>().AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(o=>o.TokenLifespan=TimeSpan.FromMinutes(30));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetFullPath(builder.Configuration["UPLOAD_DIR"]??"uploads",root),"data-protection"))).SetApplicationName("BotZone.Local");
builder.Services.AddAuthentication(AuthEndpoints.Scheme).AddCookie(AuthEndpoints.Scheme,o=>{o.Cookie.Name="BotZone.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=false;o.Events.OnValidatePrincipal=AuthEndpoints.Validate;o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};});
builder.Services.AddAntiforgery(o=>{o.HeaderName="X-CSRF-TOKEN";o.Cookie.Name="BotZone.Antiforgery";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;});
builder.Services.ConfigureHttpJsonOptions(o=>{o.SerializerOptions.PropertyNamingPolicy=JsonNamingPolicy.CamelCase;o.SerializerOptions.AllowOutOfOrderMetadataProperties=true;});
var app=builder.Build();
if(Array.IndexOf(args,"--import-product-specifications") is var specificationIndex&&specificationIndex>=0)
{
    var mode=specificationIndex+1<args.Length?args[specificationIndex+1]:"plan";
    if(mode is not ("plan" or "apply"))throw new InvalidOperationException("Use --import-product-specifications plan|apply.");
    using var scope=app.Services.CreateScope();
    var result=await new BotZone.Infrastructure.Seeding.ProductSpecificationImporter(scope.ServiceProvider.GetRequiredService<SqlStore>()).RunAsync(root,mode=="apply");
    Console.WriteLine(JsonSerializer.Serialize(result,new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}));
    return;
}
if(Array.IndexOf(args,"--import-product-photos") is var photoImportIndex&&photoImportIndex>=0)
{
    var mode=photoImportIndex+1<args.Length?args[photoImportIndex+1]:"plan";
    if(mode is not ("plan" or "apply"))throw new InvalidOperationException("Use --import-product-photos plan|apply.");
    using var scope=app.Services.CreateScope();
    var result=await new BotZone.Infrastructure.Seeding.ProductPhotoImporter(scope.ServiceProvider.GetRequiredService<SqlStore>()).RunAsync(root,mode=="apply");
    Console.WriteLine(JsonSerializer.Serialize(result,new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}));
    return;
}
if(Array.IndexOf(args,"--seed") is var seedIndex&&seedIndex>=0){using var scope=app.Services.CreateScope();var profile=seedIndex+1<args.Length?args[seedIndex+1]:"demo";await new BotZone.Infrastructure.Seeding.DemoSeeder(scope.ServiceProvider.GetRequiredService<BotZoneDbContext>()).SeedAsync(profile=="catalogue"?"catalog":profile);if(profile is "demo" or "catalogue")await new BotZone.Infrastructure.Seeding.CatalogueSeeder(scope.ServiceProvider.GetRequiredService<SqlStore>(),scope.ServiceProvider.GetRequiredService<LandingService>()).SeedAsync();return;}
app.Use(async(c,next)=>{c.Response.Headers.CacheControl="no-store";try{await next();}catch(BusinessException e){c.Response.StatusCode=e.Status;await c.Response.WriteAsJsonAsync(new{error=new{code=e.Code,message=ApiLocalization.Message(c,e.Code,e.Message)}});}catch(AntiforgeryValidationException){c.Response.StatusCode=400;await c.Response.WriteAsJsonAsync(new{error=new{code="CSRF_INVALID",message=ApiLocalization.Message(c,"CSRF_INVALID","Phiên yêu cầu đã thay đổi. Hãy tải lại và thử lại.")}});}catch(Exception e)when(e is JsonException or BadHttpRequestException or FormatException or OverflowException){c.Response.StatusCode=400;await c.Response.WriteAsJsonAsync(new{error=new{code="VALIDATION_ERROR",message=ApiLocalization.Message(c,"VALIDATION_ERROR","Dữ liệu gửi lên không đúng định dạng hoặc vượt giới hạn.")}});}catch(PostgresException e)when(e.SqlState is "23505" or "23503" or "23514" or "P0001" or "40001" or "40P01"){app.Logger.LogWarning(e,"Database rejected a business operation");c.Response.StatusCode=409;await c.Response.WriteAsJsonAsync(new{error=new{code="STATE_CONFLICT",message=ApiLocalization.Message(c,"STATE_CONFLICT","Dữ liệu hoặc trạng thái không còn phù hợp. Hãy tải lại trước khi thử lại.")}});}catch(Exception e)when(e is not OperationCanceledException){app.Logger.LogError(e,"Request failed {Path}",c.Request.Path);c.Response.StatusCode=500;await c.Response.WriteAsJsonAsync(new{error=new{code="SERVER_ERROR",message=ApiLocalization.Message(c,"SERVER_ERROR","Không thể hoàn tất yêu cầu. Xem log API local để kiểm tra.")}});}});
app.UseAuthentication();
app.Use(async(c,next)=>{var sql=c.RequestServices.GetRequiredService<SqlStore>();sql.Token=c.RequestAborted;sql.SessionId=Guid.TryParse(c.User.FindFirst("sid")?.Value,out var sid)?sid:null;if(c.Request.Path.StartsWithSegments("/api")&&c.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")await c.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(c);await next();});
AuthEndpoints.Map(app);
app.MapPost("/api/quote",(JsonElement input,HttpContext c,CommerceService s)=>s.Quote(AuthEndpoints.Actor(c),input));
app.MapPost("/api/commands",(CommandRequest input,HttpContext c,CommandService s)=>s.Execute(AuthEndpoints.Actor(c),input));
app.MapGet("/api/admin/landing",(HttpContext c,LandingService s)=>s.AdminLanding(AuthEndpoints.Actor(c)));
app.MapPost("/api/landing/preview/redeem",async(JsonElement input,HttpContext c,LandingService s)=>{var session=await s.RedeemPreview(input.Text("code"));c.Response.Cookies.Append("BotZone.Preview",session.Token,new CookieOptions{HttpOnly=true,Secure=c.Request.IsHttps,SameSite=SameSiteMode.Strict,Path="/api/landing/preview",Expires=session.ExpiresAt});return Results.Ok(new{redeemed=true,expiresAt=session.ExpiresAt});});
app.MapGet("/api/landing/preview",(HttpContext c,LandingService s)=>s.ReadPreview(c.Request.Cookies["BotZone.Preview"]??""));
app.MapPost("/api/media",async(HttpContext c,MediaService s)=>{var actor=AuthEndpoints.Actor(c);var form=await c.Request.ReadFormAsync(c.RequestAborted);var file=form.Files.GetFile("file")??throw new BusinessException("VALIDATION_ERROR","Thiếu tệp upload.",400);Guid? reference=Guid.TryParse(form["referenceId"],out var id)?id:null;return await s.Upload(actor,file,form["visibility"].ToString(),form["referenceType"].ToString(),reference,form["sourceUrl"].ToString(),form["licenseNote"].ToString());});
app.MapGet("/api/media/{id:guid}",async(Guid id,HttpContext c,MediaService s)=>{var file=await s.Download(id,c.Items["actor"] as Actor);return Results.File(file.Buffer,file.ContentType);});
app.MapGet("/health",async(BotZoneDbContext db)=>new{status=await db.Database.CanConnectAsync()?"ready":"database-unavailable",backend="ASP.NET Core",migrationOwner="EF Core"});
app.MapGet("/api/catalogue",(HttpContext c,ReadService r)=>r.Catalogue(c.Request.Query["locale"].ToString(),c.Request.Query["q"].ToString(),c.Request.Query["category"].ToString()));
app.MapGet("/api/catalogue/{slug}",(string slug,HttpContext c,ReadService r)=>r.Catalogue(c.Request.Query["locale"].ToString(),slug:slug));
app.MapGet("/api/landing",(ReadService r)=>r.Landing());
app.MapGet("/api/cart",(HttpContext c,ReadService r)=>r.Cart(AuthEndpoints.Actor(c)));
app.MapGet("/api/account",(HttpContext c,ReadService r)=>r.Account(AuthEndpoints.Actor(c),ApiLocalization.Locale(c)));
app.MapGet("/api/orders",(HttpContext c,ReadService r)=>r.Orders(AuthEndpoints.Actor(c)));
app.MapGet("/api/orders/{id:guid}",(Guid id,HttpContext c,ReadService r)=>r.OrderDetail(AuthEndpoints.Actor(c),id));
app.MapGet("/api/admin/reports/operations",(HttpContext c,OperationalReportsService s)=>s.Read(AuthEndpoints.Actor(c),c.Request.Query["kind"].ToString(),c.Request.Query["from"].FirstOrDefault()??"2024-01-01",c.Request.Query["to"].FirstOrDefault()??"2027-01-01",c.Request.Query["q"].ToString()));
app.MapGet("/api/admin/overview",(HttpContext c,ReadService r)=>r.Overview(AuthEndpoints.Actor(c)));
app.MapGet("/api/admin/customers/{userId}",(string userId,HttpContext c,CustomerReadService r)=>r.Detail(AuthEndpoints.Actor(c),userId));
app.MapGet("/api/admin/lookups",(HttpContext c,ReadService r)=>r.Lookups(AuthEndpoints.Actor(c)));
app.MapGet("/api/admin/data/{module}",(string module,HttpContext c,ReadService r)=>r.AdminData(AuthEndpoints.Actor(c),module,c.Request.Query["q"].ToString(),int.TryParse(c.Request.Query["page"],out var p)?p:1,int.TryParse(c.Request.Query["pageSize"],out var n)?n:20));
app.MapPost("/api/admin/invoices/search",(InvoiceQuery query,HttpContext c,InvoiceQueryService s)=>s.ListInvoicesAsync(query,AuthEndpoints.Actor(c),c.RequestAborted));
app.MapGet("/api/invoices/{id:guid}",(Guid id,HttpContext c,InvoiceQueryService s)=>s.GetInvoiceDetailAsync(id,AuthEndpoints.Actor(c),c.RequestAborted));
app.MapPost("/api/admin/reports/invoices",(ReportRequest request,HttpContext c,InvoiceQueryService s)=>s.GetInvoiceStatisticsAsync(request,AuthEndpoints.Actor(c),c.RequestAborted));
app.MapPost("/api/invoices/exports",(ExportRequest request,HttpContext c,InvoiceQueryService s)=>s.CreateInvoiceExportAsync(request,AuthEndpoints.Actor(c),c.RequestAborted));
app.MapGet("/api/invoices/exports/{id:guid}/{format}",async(Guid id,string format,HttpContext c,InvoiceQueryService s)=>{var file=await s.DownloadExportAsync(id,format,AuthEndpoints.Actor(c),c.RequestAborted);return Results.File(file.Buffer,file.ContentType,file.Filename);});
app.Run();
