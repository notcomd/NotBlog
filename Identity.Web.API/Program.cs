using Identity.Domain.Option;
using Identity.Infrastructure;
using Identity.Infrastructure.EntityFramework;
using Identity.Web.API.APIs;
using Identity.Web.API.Extensions;
using Identity.Web.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "Identity.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddIdentityDbContext(builder.Configuration.GetSection(nameof(DbContextOption)));

builder.Services.AddMigration<IdentityDbContext,RoleContextSeed>();
builder.Services.AddMigration<IdentityDbContext,UserDefullContextSeed>();

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());


builder.Services.AddControllers(opt =>
{
    opt.Filters.Add(new UnitOfWorkFilter());
    //opt.Filters.Add(new UserLimitsOfAuthorityFilter());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.NotBlogUseServer();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();

var identitySignUpWithLoginApis = app.MapGroup(("api/identity"));
var identityManagerApis=app.MapGroup(("api/identity/manager"));
identitySignUpWithLoginApis.NotMapIdentityApi();
identityManagerApis.NotMapIdentityManagerApi();

app.MapControllers();

app.Run();