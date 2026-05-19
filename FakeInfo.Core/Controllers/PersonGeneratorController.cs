using FakeInfo.Core.Data;
using FakeInfo.Core.Services;
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

    [HttpGet("full")]
    public async Task<IActionResult> GetFull()
    {
        var person = _generator.GenerateFullPerson();

        var entity = new GeneratedPersonEntity
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

        _db.GeneratedPersons.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(person);
    }

    [HttpGet("bulk")]
    public async Task<IActionResult> GetBulk(int count = 10)
    {
        if (count < 2 || count > 100)
            return BadRequest("Count must be between 2 and 100");

        var persons = _generator.GenerateBulk(count);

        var entities = persons.Select(person => new GeneratedPersonEntity
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
        }).ToList();

        _db.GeneratedPersons.AddRange(entities);
        await _db.SaveChangesAsync();

        return Ok(persons);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _db.GeneratedPersons
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Ok(history);
    }
}