


using EmailSendServer;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<EmailAddress>(builder.Configuration.GetSection(nameof(EmailAddress)));
builder.Services.AddEmailServer(builder.Configuration.GetSection(nameof(EmailAddress)));
builder.Services.AddOptions();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.UseHttpsRedirection();
app.MapControllers();
await app.RunAsync();