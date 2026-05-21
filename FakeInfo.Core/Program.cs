using FakeInfo.Core.Data;
using FakeInfo.Core.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Services
builder.Services.AddScoped<PersonGenerator>();
builder.Services.AddSingleton<CprValidator>();
builder.Services.AddSingleton<AddressSearchService>();

// Database
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<FakeInfoDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
}
else
{
    builder.Services.AddDbContext<FakeInfoDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

// Opret database + seed admin bruger
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FakeInfoDbContext>();
    db.Database.EnsureCreated();

    // Seed admin bruger hvis den ikke findes
    if (!db.Users.Any(u => u.Username == "admin"))
    {
        db.Users.Add(new UserEntity
        {
            Username = "admin",
            Password = "admin1234",
            Role = "admin",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }