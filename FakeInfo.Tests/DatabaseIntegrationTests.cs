using FakeInfo.Core.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace FakeInfo.Tests;

/// <summary>
/// Database integration tests - tester at FLERE lag virker sammen:
/// HTTP request → Controller → PersonGenerator → Database → Response
/// Controlleren gemmer genererede personer i databasen ved hvert kald.
/// Disse tests verificerer at dette flow virker korrekt end-to-end.
/// </summary>
[TestFixture]
public class DatabaseIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Fjern den rigtige MySQL database så tests ikke kræver en kørende database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Brug in-memory database i stedet - hver test kører isoleret
                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("DatabaseTestDb"));
                });
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // Hjælpemetode til at hente DbContext direkte så vi kan tjekke databasen
    private FakeInfoDbContext GetDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FakeInfoDbContext>();
    }

    // Tester at GET /api/person/full gemmer personen i databasen
    // Controller → PersonGenerator → Database: alle tre lag virker sammen

    [Test]
    public async Task GetFull_GemmerPersonIDatabase()
    {
        var db = GetDbContext();
        int antalFør = await db.GeneratedPersons.CountAsync();

        await _client.GetAsync("/api/person/full");

        int antalEfter = await db.GeneratedPersons.CountAsync();
        // én person skal være gemt i databasen efter kaldet
        Assert.That(antalEfter, Is.EqualTo(antalFør + 1));
    }

    [Test]
    public async Task GetFull_GemmerKorrektCpr_IDatabase()
    {
        await _client.GetAsync("/api/person/full");

        var db = GetDbContext();
        var gemt = await db.GeneratedPersons.OrderByDescending(p => p.CreatedAt).FirstAsync();

        // CPR i databasen skal være 10 cifre
        Assert.That(gemt.Cpr, Has.Length.EqualTo(10));
        Assert.That(gemt.Cpr.All(char.IsDigit), Is.True);
    }

    [Test]
    public async Task GetFull_GemmerKorrektKøn_IDatabase()
    {
        await _client.GetAsync("/api/person/full");

        var db = GetDbContext();
        var gemt = await db.GeneratedPersons.OrderByDescending(p => p.CreatedAt).FirstAsync();

        // køn i databasen skal være male eller female
        Assert.That(gemt.Gender, Is.EqualTo("male").Or.EqualTo("female"));
    }

    [Test]
    public async Task GetFull_GemmerAlleAdressefelter_IDatabase()
    {
        await _client.GetAsync("/api/person/full");

        var db = GetDbContext();
        var gemt = await db.GeneratedPersons.OrderByDescending(p => p.CreatedAt).FirstAsync();

        // alle adressefelter skal være gemt korrekt i databasen
        Assert.Multiple(() =>
        {
            Assert.That(gemt.Street, Is.Not.Empty);
            Assert.That(gemt.Number, Is.Not.Empty);
            Assert.That(gemt.PostalCode, Is.Not.Empty);
            Assert.That(gemt.Town, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetFull_GemmerCreatedAt_IDatabase()
    {
        var førKald = DateTime.UtcNow;
        await _client.GetAsync("/api/person/full");

        var db = GetDbContext();
        var gemt = await db.GeneratedPersons.OrderByDescending(p => p.CreatedAt).FirstAsync();

        // CreatedAt skal være sat til tidspunktet for kaldet
        Assert.That(gemt.CreatedAt, Is.GreaterThanOrEqualTo(førKald));
    }

    // Tester at GET /api/person/bulk gemmer alle personer i databasen

    [TestCase(2)]   // BV: mindste gyldige værdi
    [TestCase(10)]  // EP: typisk værdi
    [TestCase(100)] // BV: største gyldige værdi
    public async Task GetBulk_GemmerKorrektAntalPersoner_IDatabase(int count)
    {
        var db = GetDbContext();
        int antalFør = await db.GeneratedPersons.CountAsync();

        await _client.GetAsync($"/api/person/bulk?count={count}");

        int antalEfter = await db.GeneratedPersons.CountAsync();
        // præcis count antal personer skal være gemt i databasen
        Assert.That(antalEfter, Is.EqualTo(antalFør + count));
    }

    [Test]
    public async Task GetBulk_GemmerAllePersonerMedGyldigCpr_IDatabase()
    {
        await _client.GetAsync("/api/person/bulk?count=5");

        var db = GetDbContext();
        var gemte = await db.GeneratedPersons.OrderByDescending(p => p.CreatedAt).Take(5).ToListAsync();

        // alle 5 personer i databasen skal have gyldigt CPR
        foreach (var person in gemte)
        {
            Assert.That(person.Cpr, Has.Length.EqualTo(10));
            Assert.That(person.Cpr.All(char.IsDigit), Is.True);
        }
    }

    // Tester GET /api/person/history endpointet
    // Tester at Controller henter data korrekt fra databasen

    [Test]
    public async Task GetHistory_ReturnerPersonerFraDatabase()
    {
        // gem nogle personer først
        await _client.GetAsync("/api/person/full");
        await _client.GetAsync("/api/person/full");

        var response = await _client.GetAsync("/api/person/history");

        // history endpointet skal returnere 200 OK
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetHistory_ReturnerMaksimalt20Personer()
    {
        // gem 25 personer i databasen
        for (int i = 0; i < 25; i++)
            await _client.GetAsync("/api/person/full");

        var response = await _client.GetAsync("/api/person/history");
        var history = await response.Content.ReadFromJsonAsync<List<GeneratedPersonEntity>>();

        // history skal maks returnere 20 personer som defineret i controlleren
        Assert.That(history!.Count, Is.LessThanOrEqualTo(20));
    }

    [Test]
    public async Task GetHistory_TomDatabase_ReturnerTomListe()
    {
        // brug en separat factory med tom database
        await using var emptyFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("EmptyDb"));
                });
            });

        var emptyClient = emptyFactory.CreateClient();
        var response = await emptyClient.GetAsync("/api/person/history");
        var history = await response.Content.ReadFromJsonAsync<List<GeneratedPersonEntity>>();

        // tom database skal returnere tom liste
        Assert.That(history, Is.Empty);
    }
}