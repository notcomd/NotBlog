
using CommonsInitializer;

using FileDev.Web.API.APIs;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "FileDev.Web.Api",
    LogFilePath = "F:/"
});


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

app.NotBlogUseServer();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();

var routeFileManager = app.MapGroup("FileManager");
routeFileManager.FileDevManagerAPI();


await app.RunAsync();