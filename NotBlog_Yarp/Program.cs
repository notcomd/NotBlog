using Microsoft.AspNetCore.Builder;
using NotBlog_Yarp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var host = builder.Build();
host.MapReverseProxy();
host.Run();