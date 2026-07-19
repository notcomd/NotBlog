var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCacheMemory("Redis");

builder.AddRabbitMQClient("EventBus");

builder.Services.AddNotEmail(opt => { builder.Configuration.GetSection("NotEmail").Bind(opt); });


#if DEBUG

builder.Services.AddNotBlogServices(builder.Configuration.GetSection("DbContextOption"),
    [..ReflectionHelper.GetAllReferencedAssemblies()]);

#else
builder.Services.AddNotBlogServices(builder.Configuration.GetConnectionString("IdentityPostgres") ?? throw new InvalidOperationException(),
    [..ReflectionHelper.GetAllReferencedAssemblies()]);
#endif
builder.Services.AddIdentityService(builder.Configuration.GetSection("JwtOptions"));

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddScoped<IdentityService>();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

// ═══ EventBus 注册（通过 IConfiguration 配置驱动） ═══
// IConnectionFactory 来源：Aspire AddRabbitMQClient("EventBus") 或手动注册
 
// var hostName = eventBusCfg["HostName"] ?? "localhost";
// var userName = eventBusCfg["UserName"] ?? "guest";
// var password = eventBusCfg["Password"] ?? "guest";
// var port = int.TryParse(eventBusCfg["Port"], out var p) ? p : 5672;

// builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
// {
//     HostName = hostName,
//     UserName = userName,
//     Password = password,
//     Port = port
// });
var eventBusCfg = builder.Configuration.GetSection("EventBus");
builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        bu => bu.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// 配置 OAuth 选项
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuthOptions"));

// 注册 OAuth 服务
// 注册 Github 认证服务并配置弹性策略
builder.Services.AddHttpClient<GithubAuthService>()
    .ConfigureHttpClient(client => { client.Timeout = TimeSpan.FromMinutes(2); })
    .AddResilienceHandler("github-resilience", bu =>
    {
        bu.AddTimeout(TimeSpan.FromMinutes(2));
        bu.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential
        });
    });


// 注册 Github 认证 DI 聚合
builder.Services.AddScoped<GithubAuthDI>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseNotBlogPipeline();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//登入注册端点
app.MapGroup("api/ready").NotMapIdentityApi();
// 注册 Github 认证 API
app.MapGroup("api/git").GithubAuthApis();
// 注册 OAuth 端点
app.MapGroup("api/auth").MapOAuthEndpoints();
//管理端点
app.MapGroup("api/manger").MapUserManagerApi();

app.MapControllers();

app.Run();