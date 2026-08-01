using System.Reflection;
using Message.Infrastructure;
using Message.Infrastructure.EntityFramework;
using Message.Web.API.Extensions;
using Message.Web.API.Middleware;
using NotBlog.ServiceDefaults;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("Redis");
builder.Services.AddNpgsql<MessageDbContext>("PostgresSQL");
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddMessageInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MessageDbContext>());
builder.Services.AddHttpContextAccessor();

// ═══ Web 应用层服务统一注册（SignalR / JWT 认证 / gRPC 文件客户端 / 推送服务 / CORS） ═══
builder.Services.AddMessageWebApiServices(builder.Configuration);

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

app.UseCors();
app.UseHttpsRedirection();

app.UseExceptionHandling();
app.UseUserContext();
app.UseAuthentication();

app.UseAuthorization();

app.MapAuditApi();
app.MapCommentsApi();
app.MapFilesApi();
app.MapFriendsApi();
app.MapGroupsApi();
app.MapMessagesApi();
app.MapReportsApi();
app.MapSessionsApi();
app.MapTweetsApi();

app.MapHub<Message.Web.API.Hubs.MessageHub>("/MessageHub");

app.Run();