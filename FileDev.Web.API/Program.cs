using Commons.Extensions;
using Commons.EntityFramework;
using FileDev.Web.API.Background;
using FileDev.Web.API.Grpc;
using Notcomd.Token.JWT.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 凭据外置（S-01）：数据库口令从环境变量 FILEDEV_DB_PASSWORD 读取
builder.Services.AddNotBlogServices(
    DbConnectionStringResolver.Resolve(
        builder.Configuration.GetValue<string>("DbContextConnect"),
        "FILEDEV_DB_PASSWORD"),
    [.. ReflectionHelper.GetAllReferencedAssemblies()]);

builder.Services.AddCacheMemory(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();

builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddScoped<FileStorageServiceGRPC>();
builder.Services.AddScoped<GrpcJwtAuthInterceptor>();
builder.Services.AddHostedService<ChunkCleanupBackgroundService>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddGrpc(options =>
{
    // S-09：单条 gRPC 消息上限由 1GB 下调至 64MB，超大文件必须走分片上传
    options.MaxReceiveMessageSize = 64 * 1024 * 1024;
    options.MaxSendMessageSize = 64 * 1024 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    // S-08：所有 gRPC 方法强制 JWT 认证（拦截器解析调用者 id 写入 context.UserState）
    options.Interceptors.Add<GrpcJwtAuthInterceptor>();
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
// 添加这行来注册 IHttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<FileCheckTypeMiddleware>();

// ═══ EventBus 注册 ═══
var eventBusCfg = builder.Configuration.GetSection("EventBus");
builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
{
    HostName = eventBusCfg["HostName"] ?? "localhost",
    UserName = eventBusCfg["UserName"] ?? "guest",
    Password = eventBusCfg["Password"] ?? "guest",
    Port = int.TryParse(eventBusCfg["Port"], out var p) ? p : 5672
});
builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

// 绑定文件存储配置（包括 AllowedExtensions 白名单）
builder.Services.Configure<NotFileStorageOptions>(
    builder.Configuration.GetSection("NotFileStorage"));

builder.Services.Configure<FormOptions>(options => { options.MultipartBoundaryLengthLimit = 1024 * 1024 * 1024; }
);
// S-09：Kestrel 请求体上限从 1GB 下调到 100MB，超大文件必须走分片上传（分片 ≤ 5MB）；
// 配合端点级 [RequestSizeLimit] 实现分层限制
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; });


var app = builder.Build();

// 启动时自动应用 EF Core 迁移（与 Identity/Markdown 项目的 AddMigration 一致，
// 用 Database.Migrate 替代 EnsureCreated，避免与 Migration 管理的库结构冲突）
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotFileDbContext>();
    dbContext.Database.Migrate();
}

app.UseNotBlogPipeline();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<FileCheckTypeMiddleware>();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// S-08：文件存储 HTTP API 全部要求 JWT 认证（匿名访问 → 401）
var fileStorageGroup = app.MapGroup("/api/filestorage").RequireAuthorization();
fileStorageGroup.MapFileChunkApis();
fileStorageGroup.MapStreamUploadApis();
fileStorageGroup.MapDedupApis();

// F-09.2：注册文件组 API（FileStrongApi 内部自带 RequireAuthorization，
// 端点：/api/filestorage/upload_file、/api/filestorage/create_file_group）
app.MapGroup("/api").FileStrongApis();

app.MapGrpcService<FileStorageServiceGRPC>();

app.Run();
