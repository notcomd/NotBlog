using Identity.Infrastructure.EntityFramework;
using Identity.Web.API.APIs;
using NotBlog.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "Identity.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddNpgsql<IdentityDbContext>("IdentityPostgres");

builder.AddRedisDistributedCache("Redis");

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddControllers(opt =>
{
    opt.Filters.Add(new UnitOfWorkFilter());
});

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints();

app.NotBlogUseServer();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGroup("api/Identity").NotMapIdentityApi();

app.MapControllers();

app.Run();