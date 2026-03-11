
using FileDev.Web.API.APIs;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNotMediator();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.UseHttpsRedirection();
app.MapGet("/", () => "Hello World!");
var NotFileMapApi = app.MapGroup("/api");
NotFileMapApi.NotFileRouterGroup();
app.MapControllers();
app.Run();

