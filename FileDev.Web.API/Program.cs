using DomainInfrastructure;
using Notcomd.Token.JWT.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNotBlogServices(
    builder.Configuration.GetValue<string>("DbContextConnect")!,
    [.. ReflectionHelper.GetAllReferencedAssemblies()]);

builder.Services.AddCacheMemory(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();

builder.Services.AddScoped<INotFileService, NotFileService>();
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
// 添加这行来注册 IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FormOptions>(options => { options.MultipartBoundaryLengthLimit = 1024 * 1024 * 1024; }
);
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024; });


var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotFileDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseNotBlogPipeline();
app.UseAuthentication();
app.UseAuthorization();
app.UseFileAccess();
app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGroup("/api/filestorage").MapFileChunkApis();
app.MapGroup("/api/filestorage").MapStreamUploadApis();
app.MapGroup("/api/filestorage").MapDedupApis();

app.Run();
