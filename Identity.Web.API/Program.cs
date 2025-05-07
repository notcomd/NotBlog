using CommonsInitializer;
using DomainCommonst;
using Identity.Web.API.ActionFilter;

var builder = WebApplication.CreateBuilder(args);

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    LogFilePath = builder.Configuration!.GetValue<string>("LogFilePath"),

    EventBusQueueName = builder.Configuration!.GetValue<string>("User.Web.Api")
});

//builder.Services.AddIdentityService(builder.Configuration.GetSection(nameof(Notcomd_JwtOptions)));
// builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection(nameof(EmailSetting)));
// builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(nameof(JwtOptions)));
// builder.Services.Configure<DbContextOption>(builder.Configuration.GetSection(nameof(DbContextOption)));
// builder.Services.AddOptions();


// builder.Services.AddEmailServer(builder.Configuration.GetSection(nameof(EmailSetting)));
// builder.Services.AddIdentityService(builder.Configuration.GetSection(nameof(JwtOptions)));
// builder.Services.AddIdentityDbContext(builder.Configuration.GetSection(nameof(DbContextOption)));
// builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
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
app.NotBlogUseServer();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();