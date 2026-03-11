var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
string[] urls =
{
    "http://localhost:4000", "http://localhost:8080"
};
builder.Services.AddCors(Options => Options.AddDefaultPolicy(builder => builder.WithOrigins(urls).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  //
}
app.UseCors();
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();