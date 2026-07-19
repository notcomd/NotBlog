using System.Reflection;
using Markdown.Infrastructure.EntityFramework;
using NotBlog.ServiceDefaults;
using Notcomd.Evenbus.Extension;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

// 添加控制器服务
builder.Services.AddControllers();

// OpenAPI/Swagger 配置
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

app.Run();