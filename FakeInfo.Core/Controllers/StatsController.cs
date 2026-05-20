using FakeInfo.Core.Data;
using FakeInfoModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly FakeInfoDbContext _db;

    public StatsController(FakeInfoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var persons = await _db.GeneratedPersons.ToListAsync();

        if (persons.Count == 0)
        {
            return Ok(new GenerationStats
            {
                TotalGenerated = 0,
                TopFirstNames = new List<NameStat>(),
                TopPostalCodes = new List<PostalCodeStat>()
            });
        }

        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        var maleCount = persons.Count(p => p.Gender.ToLower() == "male");
        var femaleCount = persons.Count(p => p.Gender.ToLower() == "female");
        var total = persons.Count;

        var ages = persons
            .Select(p =>
            {
                var age = DateTime.Today.Year - p.DateOfBirth.Year;
                if (p.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
                return age;
            })
            .ToList();

        var topNames = persons
            .GroupBy(p => p.FirstName)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new NameStat { Name = g.Key, Count = g.Count() })
            .ToList();

        var topPostalCodes = persons
            .GroupBy(p => new { p.PostalCode, p.Town })
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new PostalCodeStat
            {
                PostalCode = g.Key.PostalCode,
                Town = g.Key.Town,
                Count = g.Count()
            })
            .ToList();

        return Ok(new GenerationStats
        {
            TotalGenerated = total,
            MaleCount = maleCount,
            FemaleCount = femaleCount,
            MalePercentage = Math.Round(maleCount * 100.0 / total, 1),
            FemalePercentage = Math.Round(femaleCount * 100.0 / total, 1),
            AverageAge = Math.Round(ages.Average(), 1),
            YoungestAge = ages.Min(),
            OldestAge = ages.Max(),
            TopFirstNames = topNames,
            TopPostalCodes = topPostalCodes,
            GeneratedToday = persons.Count(p => p.CreatedAt.Date == today),
            GeneratedThisWeek = persons.Count(p => p.CreatedAt.Date >= weekAgo)
        });
    }

    // Top 5 navne separat endpoint til forsiden
    [HttpGet("top-names")]
    public async Task<IActionResult> GetTopNames()
    {
        var topNames = await _db.GeneratedPersons
            .GroupBy(p => p.FirstName)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new NameStat { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(topNames);
    }
}
