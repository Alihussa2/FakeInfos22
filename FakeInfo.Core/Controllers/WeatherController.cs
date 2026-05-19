using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FakeInfo.Api.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public WeatherController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet("copenhagen")]
    public async Task<IActionResult> GetCopenhagenWeather()
    {
        var url =
            "https://api.open-meteo.com/v1/forecast" +
            "?latitude=55.6761" +
            "&longitude=12.5683" +
            "&current=temperature_2m,wind_speed_10m";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(502, "Could not fetch weather from external API");
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var current = data.GetProperty("current");

        var result = new
        {
            city = "Copenhagen",
            source = "Open-Meteo",
            temperature = current.GetProperty("temperature_2m").GetDouble(),
            windSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
            time = current.GetProperty("time").GetString()
        };

        return Ok(result);
    }
}