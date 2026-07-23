using CacheMemory.Extensions;
using NotBlog.ServiceDefaults;
using NotMediator;
using Scalar.AspNetCore;
using Video.Infrastructure;
using Video.Infrastructure.EntityFramework;
using Video.Web.API.Apis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNpgsql<VideoDbContext>("VideoPostgres");

// CacheMemory (Redis) — Aspire-style registration
builder.AddCacheMemory("CacheMemory");

// Add Video domain and infrastructure services
var fileDevBaseUrl = builder.Configuration.GetValue<string>("FileDev:BaseUrl") ?? "http://localhost:5000";
builder.Services.AddVideoInfrastructure(fileDevBaseUrl);

// Add HTTP client for streaming proxy to FileDev
builder.Services.AddHttpClient("FileDevProxy", client =>
{
    client.BaseAddress = new Uri(fileDevBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddAuthorization();
builder.Services.AddNotMediator();

builder.Services.AddOpenApi();
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

// --- MiniAPI Endpoint Registration ---
app.MapAddVideoEndpoints();
app.MapVideoEndpoints();
app.MapVideoCollectionEndpoints();
app.MapVideoReviewEndpoints();
app.MapVideoBarrageEndpoints();
app.MapVideoStreamEndpoints();

app.Run();