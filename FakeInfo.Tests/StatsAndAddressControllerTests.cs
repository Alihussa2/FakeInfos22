using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FakeInfo.Core.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using FakeInfoModels;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace FakeInfo.Tests;

[TestFixture]
public class StatsControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("StatsTestDb"));
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

    // GET /api/stats
    // Tester at Controller beregner statistik korrekt fra databasen

    [Test]
    public async Task GetStats_Returns200OK()
    {
        Assert.That((await _client.GetAsync("/api/stats")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetStats_EmptyDatabase_TotalGeneratedIsZero()
    {
        // EP: tom database skal returnere TotalGenerated = 0
        await using var emptyFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var d = services.SingleOrDefault(
                        x => x.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (d != null) services.Remove(d);
                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("EmptyStatsDb"));
                });
            });

        var stats = await (await emptyFactory.CreateClient().GetAsync("/api/stats"))
            .Content.ReadFromJsonAsync<GenerationStats>(JsonOptions);

        Assert.That(stats!.TotalGenerated, Is.EqualTo(0));
    }

    [Test]
    public async Task GetStats_WithData_TotalMatchesGeneratedCount()
    {
        // EP: generer 5 personer og tjek at TotalGenerated matcher
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var d = services.SingleOrDefault(
                        x => x.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (d != null) services.Remove(d);
                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("CountStatsDb"));
                });
            });

        var client = factory.CreateClient();
        for (int i = 0; i < 5; i++) await client.GetAsync("/api/person/full");

        var stats = await (await client.GetAsync("/api/stats"))
            .Content.ReadFromJsonAsync<GenerationStats>(JsonOptions);

        Assert.That(stats!.TotalGenerated, Is.EqualTo(5));
    }

    [Test]
    public async Task GetStats_GenderPercentages_SumTo100()
    {
        // Tester at mand + kvinde procent altid summer til 100
        await _client.GetAsync("/api/person/bulk?count=10");
        var stats = await (await _client.GetAsync("/api/stats"))
            .Content.ReadFromJsonAsync<GenerationStats>(JsonOptions);

        Assert.That(stats!.MalePercentage + stats.FemalePercentage,
            Is.EqualTo(100.0).Within(0.1));
    }

    [Test]
    public async Task GetStats_GenderCounts_SumToTotal()
    {
        await _client.GetAsync("/api/person/bulk?count=10");
        var stats = await (await _client.GetAsync("/api/stats"))
            .Content.ReadFromJsonAsync<GenerationStats>(JsonOptions);

        Assert.That(stats!.MaleCount + stats.FemaleCount, Is.EqualTo(stats.TotalGenerated));
    }

    // GET /api/stats/top-names
    // Tester at Controller henter og sorterer top-navne fra databasen

    [Test]
    public async Task GetTopNames_Returns200OK()
    {
        Assert.That((await _client.GetAsync("/api/stats/top-names")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetTopNames_MaxFiveResults()
    {
        // Tester at top-names aldrig returnerer mere end 5
        await _client.GetAsync("/api/person/bulk?count=20");
        var names = await (await _client.GetAsync("/api/stats/top-names"))
            .Content.ReadFromJsonAsync<List<NameStat>>(JsonOptions);

        Assert.That(names!.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public async Task GetTopNames_SortedByCountDescending()
    {
        // Tester at navnene er sorteret med mest populær først
        await _client.GetAsync("/api/person/bulk?count=20");
        var names = await (await _client.GetAsync("/api/stats/top-names"))
            .Content.ReadFromJsonAsync<List<NameStat>>(JsonOptions);

        for (int i = 0; i < names!.Count - 1; i++)
            Assert.That(names[i].Count, Is.GreaterThanOrEqualTo(names[i + 1].Count));
    }
}

[TestFixture]
public class AddressControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("AddressTestDb"));
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

    // GET /api/address/search
    // Tester søgning i adressedatasættet

    [Test]
    public async Task Search_NoParameters_Returns400()
    {
        // EP: ingen parametre skal give 400
        Assert.That((await _client.GetAsync("/api/address/search")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Search_EmptyParameters_Returns400()
    {
        // EP: tomme parametre skal give 400
        Assert.That((await _client.GetAsync("/api/address/search?postalCode=&town=")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Search_WithPostalCode_Returns200OK()
    {
        Assert.That((await _client.GetAsync("/api/address/search?postalCode=1000")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Search_WithTown_Returns200OK()
    {
        Assert.That((await _client.GetAsync("/api/address/search?town=København")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Search_ResultsMatchPostalCodePrefix()
    {
        // Tester at alle resultater matcher det søgte postnummer
        var results = await (await _client.GetAsync("/api/address/search?postalCode=2"))
            .Content.ReadFromJsonAsync<List<AddressSearchResult>>(JsonOptions);

        foreach (var r in results ?? [])
            Assert.That(r.PostalCode, Does.StartWith("2"));
    }

    [Test]
    public async Task Search_MaxFiftyResults()
    {
        // Tester at søgning maksimalt returnerer 50 resultater
        var results = await (await _client.GetAsync("/api/address/search?postalCode=1"))
            .Content.ReadFromJsonAsync<List<AddressSearchResult>>(JsonOptions);

        Assert.That(results!.Count, Is.LessThanOrEqualTo(50));
    }

    [Test]
    public async Task Search_BothParameters_Returns200OK()
    {
        Assert.That((await _client.GetAsync("/api/address/search?postalCode=2000&town=Frederiksberg")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }
}