using Identity.Infrastructure.Services;
using Identity.Web.API.APIs;
using NotBlog.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNotBlogServices("IdentityPostgres");

//builder.Services.AddNpgsql<IdentityDbContext>("IdentityPostgres");

builder.AddRedisDistributedCache("Redis");

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

//builder.Services.AddControllers(opt => { opt.Filters.Add(new UnitOfWorkFilter()); });

///c7d9264a-5c9b-45dd-a3b6-84ec3f138395
/*builder.Services.AddNotEmailWithOAuth2Provider(() => EmailProviderConfig.OutlookOAuth2(
    "311cc2de-cf6a-4b92-82de-a3382f68c2e4",
    "c7d9264a-5c9b-45dd-a3b6-84ec3f138395",
    async ac =>
    {
        using var token = await new HttpClient()
            .GetAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token");
        return await token.Content.ReadFromJsonAsync<string>();
    }, "common", "", "notcomd@outlook.com")
);*/

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

app.UseNotBlogPipeline();


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