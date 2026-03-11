using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Video.Infrastructure.EntityFramework;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddDbContext<VideoDbContext>(op =>
{
    op.UseNpgsql("Host=localhost;Database=video;Username=notcomd;Password=makefile");
});
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();