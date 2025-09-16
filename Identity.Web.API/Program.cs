using Identity.Infrastructure.EntityFramework;
using Identity.Web.API.APIs;
using Identity.Web.API.Infrastructure;

using Notcomd.DomainCommand;

var builder = WebApplication.CreateBuilder(args);

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "Identity.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddMigration<IdentityDbContext, SeederDataDbContext>();



builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());


builder.Services.AddControllers(opt =>
{
    opt.Filters.Add(new Notcomd.DomainCommand.UnitOfWorkFilter());
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
var identityManagerApis = app.MapGroup(("api/identity/manager"));
identitySignUpWithLoginApis.NotMapIdentityApi();
identityManagerApis.NotMapIdentityManagerApi();

app.MapControllers();

app.Run();