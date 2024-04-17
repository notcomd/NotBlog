

using Microsoft.Extensions.DependencyInjection;

using Notcomd.Identity.Server;
using Notcomd.Identity.Server.HostServer;
using Notcomd.Token.JWT;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


builder.Services.Configure<Notcomd_JwtToken_Configural>(builder.Configuration.GetSection("Notcomd_JwtToken_Configural"));
builder.Services.AddIdentityServerConfig(builder.Configuration.GetSection("Notcomd_JwtToken_Configural"));

builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
