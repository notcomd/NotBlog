using Markdown.Infrastructure.EntityFramework;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNpgsql<MarkDownDbContext>("MarkDownPostgres");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();