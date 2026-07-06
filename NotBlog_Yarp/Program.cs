using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddHostedService<Worker>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var host = builder.Build();
host.MapReverseProxy();
host.Run();