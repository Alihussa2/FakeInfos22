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
public class AuthControllerTests
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
                        options.UseInMemoryDatabase("AuthTestDb"));
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

    // Hjælpemetode: opret unik bruger og returner username
    private async Task<string> RegisterUniqueUser()
    {
        var username = $"user_{Guid.NewGuid().ToString()[..8]}";
        await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = username, Password = "test1234" });
        return username;
    }

    // POST /api/auth/register
    // Tester at Controller gemmer bruger korrekt i databasen

    [Test]
    public async Task Register_ValidUser_Returns200OK()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = "testbruger1", Password = "pass1234" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Register_ValidUser_ResponseHasCorrectFields()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = "testbruger2", Password = "pass1234" });
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Success, Is.True);
            Assert.That(result.Username, Is.EqualTo("testbruger2"));
            Assert.That(result.Role, Is.EqualTo("user"));
        });
    }

    [Test]
    public async Task Register_ValidUser_SavedInDatabase()
    {
        // Tester at brugeren faktisk gemmes i databasen
        var username = $"dbtest_{Guid.NewGuid().ToString()[..8]}";
        await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = username, Password = "pass1234" });

        var db = _factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        Assert.That(await db.Users.FirstOrDefaultAsync(u => u.Username == username),
            Is.Not.Null);
    }

    // BV for brugernavn: minimum 3 tegn

    [TestCase("ab")]  // BV: 2 tegn – under minimum
    [TestCase("")]    // EP: tomt brugernavn
    public async Task Register_ShortUsername_Returns400(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = username, Password = "pass1234" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("abc")]  // BV: 3 tegn – minimum gyldigt
    [TestCase("abcd")] // BV: 4 tegn – over minimum
    public async Task Register_ValidUsernameLength_Returns200OK(string username)
    {
        var uniqueUser = username + Guid.NewGuid().ToString()[..4];
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = uniqueUser, Password = "pass1234" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("abc")] // BV: password 3 tegn – under minimum
    [TestCase("")]    // EP: tomt password
    public async Task Register_ShortPassword_Returns400(string password)
    {
        var uniqueUser = "user_" + Guid.NewGuid().ToString()[..6];
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = uniqueUser, Password = password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_DuplicateUsername_Returns409()
    {
        // Negative test: brugernavn der allerede er taget
        var username = $"dup_{Guid.NewGuid().ToString()[..8]}";
        await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = username, Password = "pass1234" });
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new User { Username = username, Password = "anderkode" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    // POST /api/auth/login
    // Tester at Controller validerer credentials mod databasen korrekt

    [Test]
    public async Task Login_ValidCredentials_Returns200OK()
    {
        var username = await RegisterUniqueUser();
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new User { Username = username, Password = "test1234" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_WrongPassword_Returns401()
    {
        // Negative test: forkert password
        var username = await RegisterUniqueUser();
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new User { Username = username, Password = "forkert" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_NonExistingUser_Returns401()
    {
        // Negative test: bruger der ikke eksisterer
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new User { Username = "ikkeeksisterende", Password = "pass1234" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("", "pass1234")] // tomt brugernavn
    [TestCase("user", "")]     // tomt password
    public async Task Login_EmptyFields_Returns400(string username, string password)
    {
        // Negative test: tomme felter
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new User { Username = username, Password = password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // GET /api/auth/users
    // Tester at Controller henter brugerliste fra databasen

    [Test]
    public async Task GetAllUsers_Returns200OK()
    {
        Assert.That((await _client.GetAsync("/api/auth/users")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetAllUsers_ContainsRegisteredUser()
    {
        var username = await RegisterUniqueUser();
        var body = await (await _client.GetAsync("/api/auth/users")).Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain(username));
    }

    // GET /api/auth/users/{id}

    [Test]
    public async Task GetUser_ExistingId_Returns200OK()
    {
        var username = await RegisterUniqueUser();
        var db = _factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        var user = await db.Users.FirstAsync(u => u.Username == username);

        Assert.That((await _client.GetAsync($"/api/auth/users/{user.Id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetUser_NonExistingId_Returns404()
    {
        Assert.That((await _client.GetAsync("/api/auth/users/999999")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    // DELETE /api/auth/users/{id}

    [Test]
    public async Task DeleteUser_ExistingId_Returns200OK()
    {
        var username = await RegisterUniqueUser();
        var db = _factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        var user = await db.Users.FirstAsync(u => u.Username == username);

        Assert.That((await _client.DeleteAsync($"/api/auth/users/{user.Id}")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task DeleteUser_ExistingId_RemovedFromDatabase()
    {
        // Tester at brugeren faktisk er slettet fra databasen
        var username = await RegisterUniqueUser();
        var db = _factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<FakeInfoDbContext>();
        var user = await db.Users.FirstAsync(u => u.Username == username);

        await _client.DeleteAsync($"/api/auth/users/{user.Id}");
        Assert.That(await db.Users.FindAsync(user.Id), Is.Null);
    }

    [Test]
    public async Task DeleteUser_NonExistingId_Returns404()
    {
        Assert.That((await _client.DeleteAsync("/api/auth/users/999999")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }
}