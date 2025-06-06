namespace WeatherApp.Services
{
    public class WeatherRefreshService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<WeatherRefreshService> _logger;
        private readonly TimeSpan _interval;

        public WeatherRefreshService(IServiceProvider services, ILogger<WeatherRefreshService> logger)
        {
            _services = services;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(30); // Set your refresh interval here
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeatherRefreshService started, refreshing every {Interval} minutes", _interval.TotalMinutes);

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
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}