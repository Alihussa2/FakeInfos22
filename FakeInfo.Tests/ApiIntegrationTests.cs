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
    // Integration test - tester FLERE lag sammen: HTTP request → Controller → PersonGenerator → Database → Response
    // WebApplicationFactory starter den rigtige ASP.NET Core app i memory uden at mocke noget
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
                    // Fjern den rigtige database så tests ikke kræver en kørende database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FakeInfoDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Brug in-memory database i stedet under tests
                    services.AddDbContext<FakeInfoDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
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

    // Hjælpemetode: generer én person og returner dens database-id
    private async Task<int> GeneratePersonAndGetId()
    {
        await _client.GetAsync("/api/person/full");
        var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        return (await db.GeneratedPersons.OrderByDescending(p => p.CreatedAt).FirstAsync()).Id;
    }

    // GET /api/person/full
    // Tester at hele kaldet fra HTTP request → controller → service → response virker korrekt

    [Test]
    public async Task GetFull_Returns200OK()
    {
        var response = await _client.GetAsync("/api/person/full");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)); // smoke test - er endpointet tilgængeligt?
    }

    [Test]
    public async Task GetFull_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/api/person/full");
        Assert.That(response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("application/json")); // tjekker at Content-Type headeren er sat korrekt
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

        Assert.That(person!.Gender, Is.EqualTo("male").Or.EqualTo("female")); // EP: kun to gyldige værdier
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

        Assert.That(person!.Address, Is.Not.Null);
        Assert.Multiple(() =>
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
        Assert.That(person.Cpr[..6], Is.EqualTo(expectedDate),
            $"CPR '{person.Cpr}' stemmer ikke overens med DateOfBirth {person.DateOfBirth:dd-MM-yyyy}");
    }

    [Test]
    public async Task GetFull_CprLastDigit_MatchesGender()
    {
        // Køres 10 gange - sandsynlighed for falsk pass er 0.5^10
        // Tester at NameGenerator (køn) og CprGenerator (sidst ciffer) virker korrekt sammen
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/api/person/full");
            var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);
            int lastDigit = int.Parse(person!.Cpr[^1].ToString());

            // Black-box regel: sidst ciffer er lige for kvinder, ulige for mænd
            if (person.Gender == "female")
                Assert.That(lastDigit % 2, Is.EqualTo(0), $"Kvinde har ulige sidst ciffer: {person.Cpr}");
            else
                Assert.That(lastDigit % 2, Is.EqualTo(1), $"Mand har lige sidst ciffer: {person.Cpr}");
        }
    }

    [Test]
    public async Task GetFull_FirstName_HasNoSpaces()
    {
        var response = await _client.GetAsync("/api/person/full");
        var person = await response.Content.ReadFromJsonAsync<PersonFull>(JsonOptions);
        // White-box: CleanFirstName() splitter på mellemrum og tager første token
        Assert.That(person!.FirstName, Does.Not.Contain(" "));
    }

    // GET /api/person/bulk?count=n
    // Tester boundary values og equivalence partitions for count parameteren

    [TestCase(2)]   // BV: mindste gyldige værdi
    [TestCase(3)]   // BV: én over minimum
    [TestCase(10)]  // EP: typisk værdi midt i gyldig partition
    [TestCase(50)]  // EP: midtpunkt af gyldig partition
    [TestCase(99)]  // BV: én under maximum
    [TestCase(100)] // BV: største gyldige værdi
    public async Task GetBulk_ValidCount_Returns200OK(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase(2)]   // BV: mindste gyldige værdi
    [TestCase(10)]  // EP: typisk værdi
    [TestCase(100)] // BV: største gyldige værdi
    public async Task GetBulk_ValidCount_ReturnsExactNumberOfPersons(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        Assert.That(persons, Is.Not.Null);
        Assert.That(persons!.Count, Is.EqualTo(count));
    }

    [TestCase(1)]   // BV: én under minimum → ugyldig
    [TestCase(0)]   // EP: nul er ikke en gyldig mængde
    [TestCase(-1)]  // EP: negative tal er ugyldige
    [TestCase(101)] // BV: én over maximum → ugyldig
    [TestCase(200)] // EP: langt uden for gyldig partition
    public async Task GetBulk_InvalidCount_Returns400BadRequest(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        // Negative test: ugyldigt input skal give 400 Bad Request
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase(1)]   // BV: én under minimum
    [TestCase(101)] // BV: én over maximum
    public async Task GetBulk_InvalidCount_ResponseBody_ContainsErrorMessage(int count)
    {
        var response = await _client.GetAsync($"/api/person/bulk?count={count}");
        var body = await response.Content.ReadAsStringAsync();
        // fejlbeskeden skal nævne den gyldige range så klienten ved hvad der forventes
        Assert.That(body, Does.Contain("2").And.Contain("100"));
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

    [Test]
    public async Task GetBulk_AllPersons_HaveValid10DigitCpr()
    {
        var response = await _client.GetAsync("/api/person/bulk?count=10");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        foreach (var person in persons!)
        {
            Assert.That(person.Cpr, Has.Length.EqualTo(10));
            Assert.That(person.Cpr.All(char.IsDigit), Is.True);
        }
    }

    [Test]
    public async Task GetBulk_AllPersons_HaveValidGender()
    {
        var response = await _client.GetAsync("/api/person/bulk?count=10");
        var persons = await response.Content.ReadFromJsonAsync<List<PersonFull>>(JsonOptions);

        foreach (var person in persons!)
            Assert.That(person.Gender, Is.EqualTo("male").Or.EqualTo("female")); // EP: kun to gyldige værdier
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
            Assert.That(person.Cpr[..6], Is.EqualTo(expected));
        }
    }

    // GET /api/person/{id}
    // Tester at Controller henter korrekt person fra databasen via id

    [Test]
    public async Task GetById_ExistingId_Returns200OK()
    {
        int id = await GeneratePersonAndGetId();
        Assert.That((await _client.GetAsync($"/api/person/{id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetById_NonExistingId_Returns404()
    {
        // Negative test: id der ikke eksisterer
        Assert.That((await _client.GetAsync("/api/person/999999")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase(0)]   // BV: lige under minimum
    [TestCase(-1)]  // EP: negativt id
    public async Task GetById_InvalidId_ReturnsError(int id)
    {
        // Negative test: ugyldige id-værdier
        var status = (await _client.GetAsync($"/api/person/{id}")).StatusCode;
        Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound).Or.EqualTo(HttpStatusCode.BadRequest));
    }

    // GET /api/person/all
    // Tester Controller og database paginering virker korrekt sammen

    [Test]
    public async Task GetAll_Returns200OK()
    {
        await _client.GetAsync("/api/person/full");
        Assert.That((await _client.GetAsync("/api/person/all")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetAll_ResponseHasPaginationFields()
    {
        await _client.GetAsync("/api/person/full");
        var body = await (await _client.GetAsync("/api/person/all")).Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("total").And.Contain("page").And.Contain("data"));
    }

    [TestCase(1, 5)]   // gyldig page og pageSize
    [TestCase(1, 20)]  // standard pageSize
    [TestCase(1, 100)] // BV: maksimal pageSize
    public async Task GetAll_ValidPagination_Returns200OK(int page, int pageSize)
    {
        var response = await _client.GetAsync($"/api/person/all?page={page}&pageSize={pageSize}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // PUT /api/person/{id}
    // Tester at Controller opdaterer korrekt i databasen

    [Test]
    public async Task UpdatePerson_ValidData_Returns200OK()
    {
        int id = await GeneratePersonAndGetId();
        var response = await _client.PutAsJsonAsync($"/api/person/{id}",
            new UpdatePersonRequest { FirstName = "TestNavn" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task UpdatePerson_ValidData_SavedInDatabase()
    {
        // Tester at opdateringen faktisk gemmes i databasen
        int id = await GeneratePersonAndGetId();
        await _client.PutAsJsonAsync($"/api/person/{id}",
            new UpdatePersonRequest { FirstName = "NytNavn" });

        var db = _factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        Assert.That((await db.GeneratedPersons.FindAsync(id))!.FirstName, Is.EqualTo("NytNavn"));
    }

    [Test]
    public async Task UpdatePerson_InvalidPhone_Returns400()
    {
        // Negative test: ugyldigt telefonnummer skal give 400
        int id = await GeneratePersonAndGetId();
        var response = await _client.PutAsJsonAsync($"/api/person/{id}",
            new UpdatePersonRequest { Phone = "123" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdatePerson_NonExistingId_Returns404()
    {
        // Negative test: person der ikke eksisterer
        var response = await _client.PutAsJsonAsync("/api/person/999999",
            new UpdatePersonRequest { FirstName = "Test" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // DELETE /api/person/{id}
    // Tester at Controller sletter korrekt fra databasen

    [Test]
    public async Task DeletePerson_ExistingId_Returns200OK()
    {
        int id = await GeneratePersonAndGetId();
        Assert.That((await _client.DeleteAsync($"/api/person/{id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task DeletePerson_ExistingId_RemovedFromDatabase()
    {
        // Tester at personen faktisk er væk fra databasen
        int id = await GeneratePersonAndGetId();
        await _client.DeleteAsync($"/api/person/{id}");

        var db = _factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        Assert.That(await db.GeneratedPersons.FindAsync(id), Is.Null);
    }

    [Test]
    public async Task DeletePerson_NonExistingId_Returns404()
    {
        // Negative test: sletning af person der ikke eksisterer
        Assert.That((await _client.DeleteAsync("/api/person/999999")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeletePerson_GetAfterDelete_Returns404()
    {
        // Tester at man ikke kan hente en person efter den er slettet
        int id = await GeneratePersonAndGetId();
        await _client.DeleteAsync($"/api/person/{id}");
        Assert.That((await _client.GetAsync($"/api/person/{id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }
}