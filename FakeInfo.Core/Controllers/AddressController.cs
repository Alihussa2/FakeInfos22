using FakeInfo.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/address")]
public class AddressController : ControllerBase
{
    private readonly AddressSearchService _searchService;

    public AddressController(AddressSearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? postalCode, [FromQuery] string? town)
    {
        if (string.IsNullOrWhiteSpace(postalCode) && string.IsNullOrWhiteSpace(town))
            return BadRequest(new { error = "Angiv mindst et postnummer eller bynavn" });

        var results = _searchService.Search(postalCode, town);
        return Ok(results);
    }
}
