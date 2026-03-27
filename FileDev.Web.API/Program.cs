var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNotBlogServices(builder.Configuration.GetSection("DbContextConnect"));

builder.Services.AddNpgsql<NotFileDbContext>("PostgresSQL");
builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
// 添加这行来注册 IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FormOptions>(options => { options.MultipartBoundaryLengthLimit = 1024 * 1024 * 1024; }
);
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024; });


var app = builder.Build();
app.UseNotBlogPipeline();
app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGroup("/api/filestorage").NotFileApis();

app.MapControllers();
app.Run();