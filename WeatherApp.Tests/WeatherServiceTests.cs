using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using WeatherApp.Data;
using WeatherApp.Models;
using WeatherApp.Services;
using Xunit;

public class WeatherServiceTests
{
    private WeatherDbContext GetDbContext(string dbName = "WeatherTestDb")
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(dbName + Guid.NewGuid())
            .Options;
        return new WeatherDbContext(options);
    }

   private IConfiguration GetConfig(string? apiKey = "fake", string? baseUrl = "http://fakeapi")
    {
        var dict = new Dictionary<string, string?>
        {
            {"WeatherApi:ApiKey", apiKey},
            {"WeatherApi:BaseUrl", baseUrl}
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private IHttpClientFactory GetHttpClientFactory(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ReturnsWeatherData_AndCaches()
    {
        var json = "{\"name\":\"TestCity\",\"main\":{\"temp\":10},\"weather\":[{\"description\":\"clear sky\"}]}";
        var db = GetDbContext();
        var service = new WeatherService(db, GetHttpClientFactory(json), GetConfig());

        var result = await service.GetWeatherByCityAsync("TestCity");
        Assert.NotNull(result);
        Assert.Equal("TestCity", result.City);
        Assert.Equal(10, result.TemperatureCelsius);
        Assert.Equal("clear sky", result.Description);

        // Should now be cached in DB, so API won't be called again
        var result2 = await service.GetWeatherByCityAsync("TestCity");
        Assert.NotNull(result2);
        Assert.Equal(result.Id, result2.Id);
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ReturnsWeatherData_IfDbOld()
    {
        var db = GetDbContext();
        db.WeatherData.Add(new WeatherData
        {
            City = "OldCity",
            TemperatureCelsius = 1,
            Description = "old",
            ZipCode = null,
            RetrievedAt = DateTime.UtcNow.AddHours(-2)
        });
        db.SaveChanges();

        var json = "{\"name\":\"OldCity\",\"main\":{\"temp\":20},\"weather\":[{\"description\":\"new desc\"}]}";
        var service = new WeatherService(db, GetHttpClientFactory(json), GetConfig());

        var result = await service.GetWeatherByCityAsync("OldCity");
        Assert.NotNull(result);
        Assert.Equal("OldCity", result.City);
        Assert.Equal(20, result.TemperatureCelsius);
        Assert.Equal("new desc", result.Description);
    }

    [Fact]
    public async Task GetWeatherByZipAsync_ReturnsWeatherData()
    {
        var json = "{\"name\":\"ZipCity\",\"main\":{\"temp\":15},\"weather\":[{\"description\":\"cloudy\"}]}";
        var db = GetDbContext();
        var service = new WeatherService(db, GetHttpClientFactory(json), GetConfig());

        var result = await service.GetWeatherByZipAsync("12345");
        Assert.NotNull(result);
        Assert.Equal("ZipCity", result.City);
        Assert.Equal(15, result.TemperatureCelsius);
        Assert.Equal("cloudy", result.Description);
        Assert.Equal("12345", result.ZipCode);
    }

    [Fact]
    public async Task GetWeatherByZipAsync_ReturnsWeatherData_IfDbOld()
    {
        var db = GetDbContext();
        db.WeatherData.Add(new WeatherData
        {
            City = "ZipCity",
            TemperatureCelsius = 1,
            Description = "old",
            ZipCode = "12345",
            RetrievedAt = DateTime.UtcNow.AddHours(-2)
        });
        db.SaveChanges();

        var json = "{\"name\":\"ZipCity\",\"main\":{\"temp\":22},\"weather\":[{\"description\":\"fresh\"}]}";
        var service = new WeatherService(db, GetHttpClientFactory(json), GetConfig());

        var result = await service.GetWeatherByZipAsync("12345");
        Assert.NotNull(result);
        Assert.Equal("ZipCity", result.City);
        Assert.Equal(22, result.TemperatureCelsius);
        Assert.Equal("fresh", result.Description);
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ReturnsNull_OnApiFailure()
    {
        var db = GetDbContext();
        var service = new WeatherService(db, GetHttpClientFactory("{}", HttpStatusCode.BadRequest), GetConfig());

        var result = await service.GetWeatherByCityAsync("NoCity");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ReturnsNull_OnMalformedJson()
    {
        var db = GetDbContext();
        var service = new WeatherService(db, GetHttpClientFactory("not a json"), GetConfig());

        var result = await service.GetWeatherByCityAsync("NoCity");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ReturnsNull_OnMissingMainOrWeather()
    {
        var db = GetDbContext();
        var service = new WeatherService(db, GetHttpClientFactory("{\"name\":\"NoCity\"}"), GetConfig());

        var result = await service.GetWeatherByCityAsync("NoCity");
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshWeatherDataAsync_UpdatesExistingCities()
    {
        var db = GetDbContext();
        db.WeatherData.Add(new WeatherData
        {
            City = "RefreshCity",
            TemperatureCelsius = 1,
            Description = "old",
            ZipCode = "99999",
            RetrievedAt = DateTime.UtcNow.AddHours(-2)
        });
        db.SaveChanges();

        var json = "{\"name\":\"RefreshCity\",\"main\":{\"temp\":20},\"weather\":[{\"description\":\"new desc\"}]}";
        var service = new WeatherService(db, GetHttpClientFactory(json), GetConfig());

        await service.RefreshWeatherDataAsync();
        var updated = await db.WeatherData.FirstOrDefaultAsync(w => w.City == "RefreshCity");
        Assert.NotNull(updated);
        Assert.Equal(20, updated.TemperatureCelsius);
        Assert.Equal("new desc", updated.Description);
    }

    [Fact]
    public async Task RefreshWeatherDataAsync_DoesNothing_WhenNoCities()
    {
        var db = GetDbContext();
        var json = "{\"name\":\"AnyCity\",\"main\":{\"temp\":20},\"weather\":[{\"description\":\"desc\"}]}";
        var service = new WeatherService(db, GetHttpClientFactory(json), GetConfig());

        // Should not throw or update anything
        await service.RefreshWeatherDataAsync();
        Assert.Empty(db.WeatherData);
    }

    [Fact]
    public void Constructor_Throws_OnMissingApiKey()
    {
        var db = GetDbContext();
        var config = GetConfig(apiKey: null);
        var factory = GetHttpClientFactory("{}", HttpStatusCode.OK);

        Assert.Throws<InvalidOperationException>(() => new WeatherService(db, factory, config));
    }

    [Fact]
    public void Constructor_Throws_OnMissingApiBaseUrl()
    {
        var db = GetDbContext();
        var config = GetConfig(baseUrl: null);
        var factory = GetHttpClientFactory("{}", HttpStatusCode.OK);

        Assert.Throws<InvalidOperationException>(() => new WeatherService(db, factory, config));
    }
}