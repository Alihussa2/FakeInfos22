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
public class ApiIntegrationTests
{
    // Integration test - tester FLERE lag sammen: HTTP request → Controller → PersonGenerator → Response
    // WebApplicationFactory starter den rigtige ASP.NET Core app i memory uden at mocke noget
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    // PropertyNameCaseInsensitive så JSON felter som "cpr" matcher vores C# property "Cpr"
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
                    // Fjern den rigtige MySQL database så tests ikke kræver en kørende database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Brug in-memory database i stedet under tests
                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
            });

        _client = _factory.CreateClient(); // opretter en HTTP klient der taler direkte med den in-memory app
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();  // rydder op efter alle tests er kørt
        _factory.Dispose();
    }

    // Integration tests: GET /api/person/full
    // Tester at hele kaldet fra HTTP request → controller → service → response virker korrekt

    [Test]
    public async Task GetFull_Returns200OK()
    {
        var response = await _client.GetAsync("/api/person/full");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)); // smoke test - er endpointet overhovedet tilgængeligt?
    }

    [Test]
    public async Task GetFull_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/api/person/full");
        Assert.That(response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("application/json")); // tjekker at Content-Type headeren er sat korrekt af controlleren
    }

    [Test]
    public async Task GetFull_ResponseBody_DeserializesToPersonFull()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);
        Assert.That(person, Is.Not.Null); // bekræfter at JSON strukturen matcher PersonFull modellen
    }

    [Test]
    public async Task GetFull_Cpr_IsExactly10Digits()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

        Assert.That(person!.Cpr, Has.Length.EqualTo(10));       // dansk CPR er altid 10 tegn
        Assert.That(person.Cpr.All(char.IsDigit), Is.True);      // må kun indeholde cifre, ingen bindestreg
    }

    [Test]
    public async Task GetFull_Gender_IsValidValue()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

        Assert.That(person!.Gender, Is.EqualTo("male").Or.EqualTo("female")); // EP: kun to gyldige værdier er acceptable
    }

    [Test]
    public async Task GetFull_Phone_IsExactly8Digits()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

        Assert.That(person!.Phone, Has.Length.EqualTo(8));       // dansk mobilnummer er altid 8 cifre
        Assert.That(person.Phone.All(char.IsDigit), Is.True);
    }

    [Test]
    public async Task GetFull_Address_IsFullyPopulated()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

        Assert.That(person!.Address, Is.Not.Null); // adresse-objektet må ikke mangle i responsen
        Assert.Multiple(() =>                       // Assert.Multiple samler alle fejl frem for at stoppe ved første
        {
            Assert.That(person.Address.Street, Is.Not.Empty);
            Assert.That(person.Address.Number, Is.Not.Empty);
            Assert.That(person.Address.PostalCode, Is.Not.Empty);
            Assert.That(person.Address.Town, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetFull_CprDatePart_MatchesDateOfBirth()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

        string expectedDate = person!.DateOfBirth.ToString("ddMMyy");
        // Black-box regel: de første 6 cifre i CPR skal være fødselsdatoen på formen ddMMyy
        // Tester at CprGenerator og PersonGenerator producerer konsistente data sammen
        Assert.That(person.Cpr[..6], Is.EqualTo(expectedDate),
            $"CPR '{person.Cpr}' stemmer ikke overens med DateOfBirth {person.DateOfBirth:dd-MM-yyyy}");
    }

    [Test]
    public async Task GetFull_CprLastDigit_MatchesGender()
    {
        // Køres 10 gange for at undgå at testen tilfældigt består - sandsynlighed for falsk pass er 0.5^10
        // Tester at NameGenerator (køn) og CprGenerator (sidst ciffer) virker korrekt sammen
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/api/person/full");
            var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

            int lastDigit = int.Parse(person!.Cpr[^1].ToString());

            // Black-box regel fra dansk CPR-standard: sidst ciffer er lige for kvinder, ulige for mænd
            if (person.Gender == "female")
                Assert.That(lastDigit % 2, Is.EqualTo(0),
                    $"Kvinde har ulige sidst ciffer: {person.Cpr}");
            else
                Assert.That(lastDigit % 2, Is.EqualTo(1),
                    $"Mand har lige sidst ciffer: {person.Cpr}");
        }
    }

    [Test]
    public async Task GetFull_FirstName_HasNoSpaces()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);

        // White-box: vi ved at CleanFirstName() splitter på mellemrum og tager første token
        Assert.That(person!.FirstName, Does.Not.Contain(" "));
    }

    // Integration tests: GET /api/person/bulk?count=n
    // Tester boundary values og equivalence partitions for count parameteren
    // Tester at Controller validering og PersonGenerator.GenerateBulk() virker korrekt sammen

    [TestCase(2)]   // BV: mindste gyldige værdi
    [TestCase(3)]   // BV: én over minimum
    [TestCase(10)]  // EP: typisk værdi midt i gyldig partition
    [TestCase(50)]  // EP: midtpunkt af gyldig partition
    [TestCase(99)]  // BV: én under maximum
    [TestCase(100)] // BV: største gyldige værdi
    public async Task GetBulk_ValidCount_Returns200OK(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            $"count={count} burde returnere 200 OK");
    }

    [TestCase(2)]   // BV: mindste gyldige værdi
    [TestCase(10)]  // EP: typisk værdi
    [TestCase(100)] // BV: største gyldige værdi
    public async Task GetBulk_ValidCount_ReturnsExactNumberOfPersons(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        Assert.That(persons, Is.Not.Null);
        // bekræfter at count parameteren faktisk styrer hvor mange personer der genereres
        Assert.That(persons!.Count, Is.EqualTo(count),
            $"Forventede {count} personer men fik {persons.Count}");
    }

    [TestCase(1)]   // BV: én under minimum → ugyldig
    [TestCase(0)]   // EP: nul er ikke en gyldig mængde
    [TestCase(-1)]  // EP: negative tal er ugyldige
    [TestCase(101)] // BV: én over maximum → ugyldig
    [TestCase(200)] // EP: langt uden for gyldig partition
    public async Task GetBulk_InvalidCount_Returns400BadRequest(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        // Negative test: ugyldigt input skal give 400 Bad Request, ikke 200 eller 500
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            $"count={count} burde returnere 400 Bad Request");
    }

    [TestCase(1)]   // BV: én under minimum
    [TestCase(101)] // BV: én over maximum
    public async Task GetBulk_InvalidCount_ResponseBody_ContainsErrorMessage(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        var body = await response.Content.ReadAsStringAsync();

        // fejlbeskeden skal indeholde den gyldige range så klienten ved hvad der forventes
        Assert.That(body, Does.Contain("2").And.Contain("100"),
            $"Fejlbesked for count={count} skal nævne den gyldige range 2-100. Fik: {body}");
    }

    [Test]
    public async Task GetBulk_NoCountParameter_Returns10Persons()
    {
        var response = await _client.GetAsync("/api/person/bulk");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);
        // default værdien for count er 10 som defineret i controller signaturen
        Assert.That(persons!.Count, Is.EqualTo(10));
    }

    // Datakvalitetstjek: verificerer at alle personer i bulk-svaret er gyldigt genererede
    // Tester at PersonGenerator og ALLE sub-generatorer virker korrekt sammen for hele listen

    [Test]
    public async Task GetBulk_AllPersons_HaveValid10DigitCpr()
    {
        var response = await _client.GetAsync("/api/person/bulk?count=10");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        foreach (var person in persons!)
        {
            // samme CPR regler som for enkeltperson, men verificeret for hele listen
            Assert.That(person.Cpr, Has.Length.EqualTo(10),
                $"{person.FirstName} har ugyldigt CPR '{person.Cpr}'");
            Assert.That(person.Cpr.All(char.IsDigit), Is.True);
        }
    }

    [Test]
    public async Task GetBulk_AllPersons_HaveValidGender()
    {
        var response = await _client.GetAsync("/api/person/bulk?count=10");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        foreach (var person in persons!)
        {
            Assert.That(person.Gender, Is.EqualTo("male").Or.EqualTo("female")); // EP: kun disse to værdier er gyldige
        }
    }

    [Test]
    public async Task GetBulk_AllPersons_Have8DigitPhone()
    {
        var response = await _client.GetAsync("/api/person/bulk?count=10");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        foreach (var person in persons!)
        {
            Assert.That(person.Phone, Has.Length.EqualTo(8));
            Assert.That(person.Phone.All(char.IsDigit), Is.True);
        }
    }

    [Test]
    public async Task GetBulk_AllPersons_CprDatePartMatchesDob()
    {
        var response = await _client.GetAsync("/api/person/bulk?count=10");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        foreach (var person in persons!)
        {
            string expected = person.DateOfBirth.ToString("ddMMyy");
            // Black-box regel: CPR dato-del skal matche DateOfBirth for alle genererede personer
            // Tester at CprGenerator og PersonGenerator producerer konsistente data for alle personer i listen
            Assert.That(person.Cpr[..6], Is.EqualTo(expected),
                $"{person.FirstName}: CPR dato '{person.Cpr[..6]}' matcher ikke DateOfBirth '{expected}'");
        }
    }
}