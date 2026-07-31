using DomainInfrastructure;
using FileDev.Web.API.Grpc;
using Notcomd.Token.JWT.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNotBlogServices(
    builder.Configuration.GetValue<string>("DbContextConnect")!,
    [.. ReflectionHelper.GetAllReferencedAssemblies()]);

builder.Services.AddCacheMemory(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();

builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddScoped<FileStorageServiceGRPC>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 1024 * 1024 * 1024; // 1GB
    options.MaxSendMessageSize = 1024 * 1024 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
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
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024; });


var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotFileDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseNotBlogPipeline();
app.UseAuthentication();
app.UseAuthorization();
app.UseFileAccess();
app.UseMiddleware<FileCheckTypeMiddleware>();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGroup("/api/filestorage").MapFileChunkApis();
app.MapGroup("/api/filestorage").MapStreamUploadApis();
app.MapGroup("/api/filestorage").MapDedupApis();

app.MapGrpcService<FileStorageServiceGRPC>();

app.Run();
