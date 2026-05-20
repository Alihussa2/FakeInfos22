using FakeInfo.Core.Data;
using FakeInfo.Core.Services;
using FakeInfoModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/person")]
public class PersonController : ControllerBase
{
    private readonly PersonGenerator _generator;
    private readonly FakeInfoDbContext _db;

    public PersonController(PersonGenerator generator, FakeInfoDbContext db)
    {
        _generator = generator;
        _db = db;
    }

    // CREATE - Generer én person
    [HttpGet("full")]
    public async Task<IActionResult> GetFull()
    {
        var person = _generator.GenerateFullPerson();

        var entity = MapToEntity(person);
        _db.GeneratedPersons.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(person);
    }

    // CREATE - Generer bulk
    [HttpGet("bulk")]
    public async Task<IActionResult> GetBulk(int count = 10)
    {
        if (count < 2 || count > 100)
            return BadRequest(new { error = "Antal skal være mellem 2 og 100" });

        var persons = _generator.GenerateBulk(count);

        var entities = persons.Select(MapToEntity).ToList();
        _db.GeneratedPersons.AddRange(entities);
        await _db.SaveChangesAsync();

        return Ok(persons);
    }

    // READ - Hent alle med paginering
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var total = await _db.GeneratedPersons.CountAsync();

        var persons = await _db.GeneratedPersons
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = persons
        });
    }

    // READ - Hent én person by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var person = await _db.GeneratedPersons.FindAsync(id);
        if (person == null)
            return NotFound(new { error = "Person ikke fundet" });

        return Ok(person);
    }

    // READ - Historik (seneste 20)
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _db.GeneratedPersons
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Ok(history);
    }

    // UPDATE - Opdater person
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePerson(int id, [FromBody] UpdatePersonRequest request)
    {
        var person = await _db.GeneratedPersons.FindAsync(id);
        if (person == null)
            return NotFound(new { error = "Person ikke fundet" });

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            person.FirstName = request.FirstName;
        if (!string.IsNullOrWhiteSpace(request.LastName))
            person.LastName = request.LastName;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            if (request.Phone.Length != 8 || !request.Phone.All(char.IsDigit))
                return BadRequest(new { error = "Telefonnummer skal være 8 cifre" });
            person.Phone = request.Phone;
        }
        if (!string.IsNullOrWhiteSpace(request.Street))
            person.Street = request.Street;
        if (!string.IsNullOrWhiteSpace(request.Number))
            person.Number = request.Number;
        if (request.Floor != null)
            person.Floor = request.Floor;
        if (request.Door != null)
            person.Door = request.Door;
        if (!string.IsNullOrWhiteSpace(request.PostalCode))
            person.PostalCode = request.PostalCode;
        if (!string.IsNullOrWhiteSpace(request.Town))
            person.Town = request.Town;

        await _db.SaveChangesAsync();

        return Ok(person);
    }

    // DELETE - Slet person
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePerson(int id)
    {
        var person = await _db.GeneratedPersons.FindAsync(id);
        if (person == null)
            return NotFound(new { error = "Person ikke fundet" });

        _db.GeneratedPersons.Remove(person);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Person '{person.FirstName} {person.LastName}' slettet" });
    }

    private static GeneratedPersonEntity MapToEntity(PersonFull person)
    {
        return new GeneratedPersonEntity
        {
            Cpr = person.Cpr,
            DateOfBirth = person.DateOfBirth,
            Phone = person.Phone,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Gender = person.Gender,
            Street = person.Address.Street,
            Number = person.Address.Number,
            Floor = person.Address.Floor,
            Door = person.Address.Door,
            PostalCode = person.Address.PostalCode,
            Town = person.Address.Town,
            CreatedAt = DateTime.UtcNow
        };
    }
}
