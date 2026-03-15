using System.Reflection;
using CommonsInitializer;
using DomainCommonst;
using FileDev.Infrastructure.EntityFramework;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "FileDev.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddNpgsql<NotFileDbContext>("PostgresSQL");

builder.AddRedisDistributedCache("Redis");

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();


var app = builder.Build();
app.NotBlogUseServer();
app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.MapControllers();
app.Run();