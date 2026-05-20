using FakeInfo.Core.Data;
using FakeInfoModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly FakeInfoDbContext _db;

    public AuthController(FakeInfoDbContext db)
    {
        _db = db;
    }

    // CREATE - Register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return BadRequest(new { error = "Brugernavn skal være mindst 3 tegn" });

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
            return BadRequest(new { error = "Password skal være mindst 4 tegn" });

        var exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            return Conflict(new { error = "Brugernavn er allerede taget" });

        var user = new UserEntity
        {
            Username = request.Username,
            Password = request.Password,
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            Success = true,
            Username = user.Username,
            Role = user.Role
        });
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Brugernavn og password er påkrævet" });

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Username == request.Username && u.Password == request.Password);

        if (user == null)
            return Unauthorized(new { error = "Forkert brugernavn eller password" });

        return Ok(new LoginResponse
        {
            Success = true,
            Username = user.Username,
            Role = user.Role
        });
    }

    // READ - Hent alle brugere
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Username, u.Role, u.CreatedAt })
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return Ok(users);
    }

    // READ - Hent én bruger
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "Bruger ikke fundet" });

        return Ok(new { user.Id, user.Username, user.Role, user.CreatedAt });
    }

    // UPDATE - Opdater bruger
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "Bruger ikke fundet" });

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            if (request.Username.Length < 3)
                return BadRequest(new { error = "Brugernavn skal være mindst 3 tegn" });

            var duplicate = await _db.Users.AnyAsync(u => u.Username == request.Username && u.Id != id);
            if (duplicate)
                return Conflict(new { error = "Brugernavn er allerede taget" });

            user.Username = request.Username;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 4)
                return BadRequest(new { error = "Password skal være mindst 4 tegn" });

            user.Password = request.Password;
        }

        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Role, user.CreatedAt });
    }

    // DELETE - Slet bruger
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "Bruger ikke fundet" });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Bruger '{user.Username}' slettet" });
    }
}
