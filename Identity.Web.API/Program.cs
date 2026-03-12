using Identity.Infrastructure.EntityFramework;
using Identity.Web.API.APIs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "Identity.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddNpgsql<IdentityDbContext>("IdentityPostgres");
builder.AddRedisDistributedCache("Redis");

// builder.Services.AddDbContext<IdentityDbContext>(opt
//     => opt.UseNpgsql(builder.Configuration.GetConnectionString(nameof(DbContextOptions)),
//         o => o.MigrationsAssembly("Identity.Infrastructure"))
// );

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

app.MapDefaultEndpoints();

app.NotBlogUseServer();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
var identityService = app.MapGroup("api/identity");
identityService.NotMapIdentityApi();
app.MapControllers();
app.Run();