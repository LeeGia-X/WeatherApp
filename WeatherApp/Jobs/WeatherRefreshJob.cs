using Quartz;
using WeatherApp.Services;

public class WeatherRefreshJob : IJob
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherRefreshJob> _logger;

    public WeatherRefreshJob(IWeatherService weatherService, ILogger<WeatherRefreshJob> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Quartz job: refreshing weather data");
        await _weatherService.RefreshWeatherDataAsync();
    }
}