using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Services;

namespace WeatherApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("city/{city}")]
        public async Task<IActionResult> GetByCity(string city)
        {
            var weather = await _weatherService.GetWeatherByCityAsync(city);
            if (weather == null)
                return NotFound();

            return Ok(weather);
        }

        [HttpGet("zip/{zipCode}")]
        public async Task<IActionResult> GetByZip(string zipCode)
        {
            var weather = await _weatherService.GetWeatherByZipAsync(zipCode);
            if (weather == null)
                return NotFound();

            return Ok(weather);
        }
    }
}