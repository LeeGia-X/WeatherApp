using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WeatherApp.Controllers;
using WeatherApp.Models;
using WeatherApp.Services;
using Xunit;

public class WeatherControllerTests
{
    [Fact]
    public async Task GetByCity_ReturnsOk_WhenFound()
    {
        var mockService = new Mock<IWeatherService>();
        mockService.Setup(s => s.GetWeatherByCityAsync("City")).ReturnsAsync(
            new WeatherData { City = "City", TemperatureCelsius = 10, Description = "desc", RetrievedAt = System.DateTime.UtcNow }
        );
        var ctrl = new WeatherController(mockService.Object);

        var result = await ctrl.GetByCity("City");
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<WeatherData>(ok.Value);
        Assert.Equal("City", data.City);
    }

    [Fact]
    public async Task GetByCity_ReturnsNotFound_WhenNull()
    {
        var mockService = new Mock<IWeatherService>();
        mockService.Setup(s => s.GetWeatherByCityAsync("NoCity")).ReturnsAsync((WeatherData?)null);
        var ctrl = new WeatherController(mockService.Object);

        var result = await ctrl.GetByCity("NoCity");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByZip_ReturnsOk_WhenFound()
    {
        var mockService = new Mock<IWeatherService>();
        mockService.Setup(s => s.GetWeatherByZipAsync("12345")).ReturnsAsync(
            new WeatherData { City = "C", TemperatureCelsius = 5, Description = "desc", ZipCode = "12345", RetrievedAt = System.DateTime.UtcNow }
        );
        var ctrl = new WeatherController(mockService.Object);

        var result = await ctrl.GetByZip("12345");
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<WeatherData>(ok.Value);
        Assert.Equal("12345", data.ZipCode);
    }

    [Fact]
    public async Task GetByZip_ReturnsNotFound_WhenNull()
    {
        var mockService = new Mock<IWeatherService>();
        mockService.Setup(s => s.GetWeatherByZipAsync("99999")).ReturnsAsync((WeatherData?)null);
        var ctrl = new WeatherController(mockService.Object);

        var result = await ctrl.GetByZip("99999");
        Assert.IsType<NotFoundResult>(result);
    }
}