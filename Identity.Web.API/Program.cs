using CacheMemory.Extensions;
using DomainInfrastructure;
using Identity.Infrastructure;
using Identity.Infrastructure.Services;
using Identity.Web.API.APIs;
using Microsoft.Extensions.Http.Resilience;
using NotBlog.ServiceDefaults;
using Notcomd.NotEmail.Extensions;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCacheMemory("Redis");

builder.Services.AddNotEmail(opt => { builder.Configuration.GetSection("NotEmail").Bind(opt); });

builder.Services.AddNotBlogServices("IdentityPostgres",
    ReflectionHelper.GetAllReferencedAssemblies().ToArray());

builder.Services.AddIdentityService(builder.Configuration.GetSection("JwtOptions"));

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

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