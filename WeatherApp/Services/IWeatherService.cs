using WeatherApp.Models;

namespace WeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherData?> GetWeatherByCityAsync(string city);
        Task<WeatherData?> GetWeatherByZipAsync(string zipCode);
        Task RefreshWeatherDataAsync();
    }
}