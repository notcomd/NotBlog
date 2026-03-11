using System.Reflection;
using FileDev.Web.API.APIs;
using NotMediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

/*builder.Services.AddDbContext<FileDevDbContext>(opt
    => opt.UseNpgsql(builder.Configuration.GetConnectionString(nameof(DbContextOptions)),
        o => o.MigrationsAssembly("FileDev.Infrastructure"))
);*/
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.UseHttpsRedirection();

var notFileMapApi = app.MapGroup("api/notfile");
notFileMapApi.NotFileApis();

app.MapControllers();

app.Run();