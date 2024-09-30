using EmailSendServer;
using Identity.Infrastructure;
using Notcomd.Token.JWT;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityService(builder.Configuration.GetSection(nameof(Notcomd_JwtOptions)));
builder.Services.Configure<EmailAddress>(builder.Configuration.GetSection(nameof(EmailAddress)));
builder.Services.Configure<Notcomd_JwtOptions>(builder.Configuration.GetSection(nameof(Notcomd_JwtOptions)));
builder.Services.AddEmailServer(builder.Configuration.GetSection(nameof(EmailAddress)));
builder.Services.AddOptions();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();