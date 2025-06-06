using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _apiKey;
        private readonly string? _apiBaseUrl;

        public WeatherService(
            WeatherDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _apiKey = Environment.GetEnvironmentVariable("OPENWEATHERMAP_API_KEY") ?? config["WeatherApi:ApiKey"];
            _apiBaseUrl = config["WeatherApi:BaseUrl"];
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiBaseUrl))
                throw new InvalidOperationException("API key or base URL is not configured.");
        }

        public async Task<WeatherData?> GetWeatherByCityAsync(string city)
        {
            var weather = await _context.WeatherData.FirstOrDefaultAsync(w => w.City == city);
            if (weather != null && weather.RetrievedAt > DateTime.UtcNow.AddHours(-1))
                return weather;

            var result = await FetchWeatherFromApiAsync($"q={city}");
            if (result != null)
            {
                await UpsertWeatherDataAsync(result);
            }
            return result;
        }

        public async Task<WeatherData?> GetWeatherByZipAsync(string zipCode)
        {
            var weather = await _context.WeatherData.FirstOrDefaultAsync(w => w.ZipCode == zipCode);
            if (weather != null && weather.RetrievedAt > DateTime.UtcNow.AddHours(-1))
                return weather;

            var result = await FetchWeatherFromApiAsync($"zip={zipCode}");
            if (result != null)
            {
                await UpsertWeatherDataAsync(result);
            }
            return result;
        }

        public async Task RefreshWeatherDataAsync()
        {
            var allCities = await _context.WeatherData.Select(w => w.City).Distinct().ToListAsync();
            foreach (var city in allCities)
            {
                var weather = await FetchWeatherFromApiAsync($"q={city}");
                if (weather != null)
                {
                    await UpsertWeatherDataAsync(weather);
                }
            }
        }

        private async Task<WeatherData?> FetchWeatherFromApiAsync(string query)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_apiBaseUrl}?{query}&appid={_apiKey}&units=metric";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(json);

                var root = doc.RootElement;
                if (!root.TryGetProperty("main", out var main) || !root.TryGetProperty("weather", out var weatherArr))
                    return null;

                var weatherData = new WeatherData
                {
                    City = root.GetProperty("name").GetString() ?? string.Empty,
                    ZipCode = query.Contains("zip=") ? query.Split('=')[1].Split('&')[0] : null,
                    TemperatureCelsius = main.GetProperty("temp").GetDouble(),
                    Description = weatherArr[0].GetProperty("description").GetString() ?? string.Empty,
                    RetrievedAt = DateTime.UtcNow
                };
                return weatherData;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        private async Task UpsertWeatherDataAsync(WeatherData data)
        {
            var existing = await _context.WeatherData.FirstOrDefaultAsync(w => w.City == data.City);
            if (existing != null)
            {
                existing.TemperatureCelsius = data.TemperatureCelsius;
                existing.Description = data.Description;
                existing.RetrievedAt = data.RetrievedAt;
                existing.ZipCode = data.ZipCode;
            }
            else
            {
                await _context.WeatherData.AddAsync(data);
            }
            await _context.SaveChangesAsync();
        }
        
    }
}