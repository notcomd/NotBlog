using System.Reflection;
using Message.Domain.SeedWork;
using Message.Infrastructure;
using Message.Infrastructure.EntityFramework;
using NotBlog.ServiceDefaults;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("Redis");
builder.Services.AddNpgsql<MessageDbContext>("PostgresSQL");
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddMessageInfrastructure();
builder.AddRedisDistributedCache("Redis");
builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MessageDbContext>());
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

builder.Services.AddSignalR();

string[] urls =
{
    "http://localhost:4000", "http://localhost:8080"
};
builder.Services.AddCors(Options =>
    Options.AddDefaultPolicy(builder =>
        builder.WithOrigins(urls).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

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

app.UseAuthorization();
app.MapControllers();

app.Run();