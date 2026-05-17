using Microsoft.AspNetCore.Mvc;
using FakeInfo.Core.Services;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/person")]
public class PersonController : ControllerBase
{
    private readonly PersonGenerator _generator;

    public PersonController(PersonGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("full")]
    public IActionResult GetFull()
    {
        return Ok(_generator.GenerateFullPerson());
    }

    [HttpGet("bulk")]
    public IActionResult GetBulk(int count = 10)
    {
        if (count < 2 || count > 100)
            return BadRequest("Count must be between 2 and 100");

        return Ok(_generator.GenerateBulk(count));
    }
}