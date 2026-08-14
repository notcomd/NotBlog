using Commons.Extensions;
using Commons.EntityFramework;
using FileDev.Web.API.Background;
using FileDev.Web.API.Grpc;
using Notcomd.Token.JWT.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();


builder.Services.AddNotBlogServices(
    
        builder.Configuration.GetValue<string>("DbContextConnect")!
     );

builder.Services.AddCacheMemory(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();

builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddScoped<FileStorageServiceGRPC>();
builder.Services.AddScoped<GrpcJwtAuthInterceptor>();
builder.Services.AddScoped<GrpcExceptionMapperInterceptor>();
builder.Services.AddHostedService<ChunkCleanupBackgroundService>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddGrpc(options =>
{
   
    options.MaxReceiveMessageSize = 64 * 1024 * 1024;
    options.MaxSendMessageSize = 64 * 1024 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    
    // 异常映射拦截器注册在最外层，确保能捕获服务方法及内层拦截器抛出的业务异常
    options.Interceptors.Add<GrpcExceptionMapperInterceptor>();
    options.Interceptors.Add<GrpcJwtAuthInterceptor>();
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

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

// 静态工具类注入日志工厂（ImageValidator 为静态类，无法走构造注入）
ImageValidator.Configure(app.Services.GetRequiredService<ILoggerFactory>());

// 启动时自动应用 EF Core 迁移（与 Identity/Markdown 项目的 AddMigration 一致，
// 用 Database.Migrate 替代 EnsureCreated，避免与 Migration 管理的库结构冲突）
// 修复：2026-08-13 42P01 FileChunkRecord 不存在 —— 此前该块被注释导致迁移从未应用，
// 后台清理服务（ChunkCleanupBackgroundService）查询 FileChunkRecord 表时报 relation does not exist。
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

app.MapFileDownloadApi();
app.MapGrpcService<FileStorageServiceGRPC>();

app.Run();
