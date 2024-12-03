using System.Reflection;
using EmailSendServer;
using Identity.Domain.Option;
using Identity.Infrastructure;
using Identity.Web.API;
using Identity.Web.API.ActionFilter;
using MediatR;
using Notcomd.Token.JWT;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddIdentityService(builder.Configuration.GetSection(nameof(Notcomd_JwtOptions)));
builder.Services.Configure<EmailAddress>(builder.Configuration.GetSection(nameof(EmailAddress)));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(nameof(JwtOptions)));
builder.Services.Configure<DbContextOption>(builder.Configuration.GetSection(nameof(DbContextOption)));
builder.Services.AddOptions();

builder.Services.AddEmailServer(builder.Configuration.GetSection(nameof(EmailAddress)));
builder.Services.AddIdentityService(builder.Configuration.GetSection(nameof(JwtOptions)));
builder.Services.AddIdentityDbContext(builder.Configuration.GetSection(nameof(DbContextOption)));
builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.AddControllers(opt =>
{
    opt.Filters.Add(new UnitOfWorkFilter());
    opt.Filters.Add(new UserLimitsOfAuthorityFilter());
});

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