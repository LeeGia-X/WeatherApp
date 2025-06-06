using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Services
{
    public class WeatherRefreshService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<WeatherRefreshService> _logger;

        public WeatherRefreshService(IServiceProvider services, ILogger<WeatherRefreshService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
                    try
                    {
                        await weatherService.RefreshWeatherDataAsync();
                        _logger.LogInformation("Weather data refreshed at: {time}", DateTimeOffset.Now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing weather data");
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}