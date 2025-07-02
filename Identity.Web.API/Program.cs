var builder = WebApplication.CreateBuilder(args);

builder.NotBlogConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "Identity.Web.API",
    LogFilePath = "E:/web.log"
});

builder.Services.AddControllers(opt =>
{
    opt.Filters.Add(new UnitOfWorkFilter());
    opt.Filters.Add(new UserLimitsOfAuthorityFilter());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
var app = builder.Build();

app.NotBlogUseServer();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();
app.MapControllers();
app.Run();