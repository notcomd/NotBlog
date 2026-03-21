using System.Reflection;
using CommonsInitializer;
using DomainCommonst;
using FileDev.Domain.IServices;
using FileDev.Infrastructure.EntityFramework;
using FileDev.Infrastructure.Service;
using FileDev.Web.API.APIs;
using Microsoft.AspNetCore.Http.Features;
using NotBlog.ServiceDefaults;
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
builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.Configure<FormOptions>(ope => { ope.MultipartBoundaryLengthLimit = 1024 * 1024 * 1024; });
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024; });


var app = builder.Build();
app.NotBlogUseServer();
app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGroup("/api/filestorage").NotFileApis();

app.MapControllers();
app.Run();