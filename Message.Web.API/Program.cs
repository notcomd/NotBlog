using System.Reflection;
using Message.Infrastructure;
using Message.Infrastructure.EntityFramework;
using Message.Web.API.Middleware;
using NotBlog.ServiceDefaults;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("Redis");
builder.Services.AddNpgsql<MessageDbContext>("PostgresSQL");
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.AddRedisDistributedCache("Redis");
builder.Services.AddMessageInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MessageDbContext>());
builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

builder.Services.AddSignalR();


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