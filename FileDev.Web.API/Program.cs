

using FileDev.Web.API.Grpc;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCacheMemory("Redis");



if(builder.Configuration.GetConnectionString("EventBus") is null)
{

    // 单机 EventBus：从配置节点构建 IConnectionFactory，否则 RabbitMqConnection 激活失败
    var eventBusSection = builder.Configuration.GetSection("EventBus");
    var hostName = eventBusSection["HostName"] ?? "127.0.0.1";
    var userName = eventBusSection["UserName"] ?? "guest";
    var password = eventBusSection["Password"] ?? "guest";
    var port = eventBusSection["Port"] is { } p && int.TryParse(p, out var parsedPort) ? parsedPort : 5672;
    builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
    {
        HostName = hostName,
        UserName = userName,
        Password = password,
        Port = port
    });
    builder.Services.AddEventBus(eventBusSection);
}
else
{
    builder.AddRabbitMQClient("EventBus");
    builder.Services.AddEventBus(
        builder.Configuration.GetConnectionString("EventBus")!,
        Assembly.GetEntryAssembly() ?? throw new AppDomainUnloadedException("load assembly error"));
}



if (builder.Configuration.GetConnectionString("NotFilePostgres") is null)
{
    builder.Services.AddNpgsql<NotFileDbContext>(
        builder.Configuration.GetSection("DbContextOption")["DbContextConnection"]);
}
else
{
    Console.WriteLine("Aspire服务执行！");
    // DbContext 注册（Aspire 版：连接名语义 + 自动健康检查/OpenTelemetry）
    builder.AddNpgsqlDbContext<NotFileDbContext>("NotFilePostgres");
}

// ── MongoDB（分片上传跟踪，方案 B）──
// 单机分支：从配置节点 MongoDb:ConnectionString 读取，未配置时回落本地默认地址；
// Aspire 分支：通过 AddMongoDBClient 注入连接串。二者最终统一为单例 IMongoDatabase。
if (builder.Configuration.GetConnectionString("NotFileMongo") is null)
{
    var mongoConn = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://127.0.0.1:27017";
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
}
else
{
    builder.AddMongoDBClient("NotFileMongo");
}
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDb:Database"] ?? "notfile";
    return client.GetDatabase(databaseName);
});

// 模块自动初始化（仓储/领域服务注册；原 AddNotBlogServices 拆分，DbContext 改用 Aspire 注册）
builder.Services.AddAutoAddInstance(ReflectionHelper.GetAllReferencedAssemblies());





builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();

builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddScoped<FileStorageServiceGRPC>();
builder.Services.AddScoped<GrpcJwtAuthInterceptor>();
builder.Services.AddScoped<GrpcExceptionMapperInterceptor>();
builder.Services.AddHostedService<ChunkCleanupBackgroundService>();
builder.Services.AddHostedService<VolumeSyncBackgroundService>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());


builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggerBehavior<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
builder.Services.AddGrpc(options =>
{

    options.MaxReceiveMessageSize = 64 * 1024 * 1024;
    options.MaxSendMessageSize = 64 * 1024 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();

    options.Interceptors.Add<GrpcExceptionMapperInterceptor>();
    options.Interceptors.Add<GrpcJwtAuthInterceptor>();
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<FileCheckTypeMiddleware>();


builder.Services.AddEventBus(builder.Configuration.GetSection("EventBus"), Assembly.GetExecutingAssembly());



// 绑定文件存储配置（包括 AllowedExtensions 白名单）
builder.Services.Configure<NotFileStorageOptions>(
    builder.Configuration.GetSection("NotFileStorage"));

builder.Services.Configure<FormOptions>(options => { options.MultipartBoundaryLengthLimit = 1024 * 1024 * 1024; }
);

builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; });



var app = builder.Build();

ResourcesBanner.PrintStartupBanner();

// 静态工具类注入日志工厂（ImageValidator 为静态类，无法走构造注入）
ImageValidator.Configure(app.Services.GetRequiredService<ILoggerFactory>());


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
fileStorageGroup.MapFileVolumeApis();
fileStorageGroup.MapMyFilesApis();
// 管理端文件端点（/api/filestorage/admin/*，内部校验管理员角色）
fileStorageGroup.MapAdminFileApi();

// F-09.2：注册标签 API（FileTagApi 内部自带 RequireAuthorization，
// 端点：/api/filestorage/tags/...，即原文件组 API 的标签化替代）
app.MapGroup("/api").FileTagApis();

app.MapFileDownloadApi();
app.MapGrpcService<FileStorageServiceGRPC>();

app.Run();
