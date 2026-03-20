using Identity.Domain.IService;
using Identity.Domain.Options;
using Identity.Infrastructure.EntityFramework;
using Identity.Infrastructure.Services;
using Identity.Web.API.APIs;
using NotBlog.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "Identity.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddNpgsql<IdentityDbContext>("IdentityPostgres");

builder.AddRedisDistributedCache("Redis");

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddControllers(opt => { opt.Filters.Add(new UnitOfWorkFilter()); });

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

// 配置 OAuth 选项
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuthOptions"));

// 注册 OAuth 服务
builder.Services.AddHttpClient<IOAuthService, OAuthService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.NotBlogUseServer();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGroup("api/Identity").NotMapIdentityApi();

// 注册 OAuth 端点
app.MapGroup("api/auth").MapOAuthEndpoints();

app.MapControllers();

app.Run();