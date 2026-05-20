using FakeInfo.Core.Services;
using FakeInfoModels;
using Microsoft.AspNetCore.Mvc;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/cpr")]
public class CprController : ControllerBase
{
    private readonly CprValidator _validator;

    public CprController(CprValidator validator)
    {
        _validator = validator;
    }

    [HttpPost("validate")]
    public IActionResult Validate([FromBody] CprValidationRequest request)
    {
        var result = _validator.Validate(request.Cpr);
        return Ok(result);
    }
}
