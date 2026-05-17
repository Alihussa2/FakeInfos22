using FakeInfo.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<PersonGenerator>();

var app = builder.Build();

// Swagger (kun i development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 👇 VIGTIGT: gør frontend muligt
app.UseDefaultFiles();   // loader index.html automatisk
app.UseStaticFiles();    // gør wwwroot tilgængelig

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();