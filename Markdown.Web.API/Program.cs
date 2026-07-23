using System.Reflection;
using Markdown.Infrastructure.EntityFramework;
using Markdown.Web.API.Apis;
using NotBlog.ServiceDefaults;
using Notcomd.Evenbus.Extension;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 配置 JWT Bearer 认证
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"] ?? "https://localhost:5001";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NotBlog",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false // 信任 Authority（Identity 服务）的签名密钥
        };
    });

// 配置 PostgreSQL DbContext
builder.Services.AddNpgsql<MarkDownDbContext>("MarkDownPostgres");

// 配置 NotMediator（领域事件中介）
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());


// 配置 EventBus（通过 IConfiguration 配置驱动）
// IConnectionFactory 来源：Aspire AddRabbitMQClient("EventBus") 或手动注册
var eventBusCfg = builder.Configuration.GetSection("EventBus");
#if DEBUG
builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(_ =>
{
    var host = eventBusCfg["HostName"] ?? "localhost";
    var userName = eventBusCfg["UserName"] ?? "guest";
    var password = eventBusCfg["Password"] ?? "guest";
    return new RabbitMQ.Client.ConnectionFactory
    {
        HostName = host,
        UserName = userName,
        Password = password
    };
});
#else
builder.AddRabbitMQClient("EventBus");
#endif
builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

// OpenAPI/Swagger 配置
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

// 当前用户服务
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Markdown.Domain.IServices.ICurrentUserService, Markdown.Web.API.Services.CurrentUserService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


app.MapMarkdownApis();

app.Run();