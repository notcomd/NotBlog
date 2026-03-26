using System.Reflection;
using Markdown.Infrastructure.EntityFramework;
using NotBlog.ServiceDefaults;
using Notcomd.Evenbus;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 配置 PostgreSQL DbContext
builder.Services.AddNpgsql<MarkDownDbContext>("MarkDownPostgres");

// 配置 NotMediator（领域事件中介）
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());


// 配置 EventBus（RabbitMQ 消息总线）
builder.Services.Configure<IntegrationEventRabbitMqOptions>(options =>
{
    options.HostName = builder.Configuration["EventBus:HostName"] ?? "localhost";
    options.ExchangeName = builder.Configuration["EventBus:ExchangeName"] ?? "markdown_events";
    options.UserName = builder.Configuration["EventBus:UserName"];
    options.Password = builder.Configuration["EventBus:Password"];
});

builder.Services.AddEventBus("markdown_queue", Assembly.GetExecutingAssembly());

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

// 使用 EventBus
app.UseEventBus();

app.MapControllers();

app.Run();