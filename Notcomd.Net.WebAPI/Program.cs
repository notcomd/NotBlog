using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

using Notcomd.Identity.Server;
using Notcomd.Identity.Server.Config;
using Notcomd.Token.JWT;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    var scheme = new OpenApiSecurityScheme()
    {
        Description = "Authorization header Example",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Authorization" },
        Scheme = "oauth2",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
    };
    opt.AddSecurityDefinition("Authorization", scheme);
    var requirment = new OpenApiSecurityRequirement();
    requirment[scheme] = new List<string>();
    opt.AddSecurityRequirement(requirment);
});
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("MyServer", poli =>
    {
        poli.WithOrigins("https://localhost:7097").AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
    });
});
builder.Services.Configure<Notcomd_JwtOptions>(builder.Configuration.GetSection(nameof(Notcomd_JwtOptions)));
builder.Services.Configure<IdentitySQLSetting>(builder.Configuration.GetSection(nameof(IdentitySQLSetting)));
builder.Services.Configure<StatUserSetting>(builder.Configuration.GetSection(nameof(StatUserSetting)));
builder.Services.AddDbContextOptions(builder.Configuration.GetSection(nameof(IdentitySQLSetting)));
builder.Services.AddIdentityServerConfig(builder.Configuration.GetSection(nameof(Notcomd_JwtOptions)));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Static")),
    RequestPath = "/Static"
});
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
