using WeatherBot.Models;

namespace WeatherBot.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<(double lat, double lon, string locationName)> GetLocationAsync(string city, string country = "");
        Task<CurrentWeather> GetCurrentWeatherAsync(double lat, double lon);
        Task<WeatherForecast> GetDailyForecastAsync(double lat, double lon, int days = 7);
        Task<WeatherForecast> GetHourlyForecastAsync(double lat, double lon, int hours = 24);
        Task<AirQualityData> GetAirQualityAsync(double lat, double lon);
        Task<HistoricalWeather> GetHistoricalWeatherAsync(double lat, double lon, DateOnly date);
        Task<List<WeatherAlert>> GetWeatherAlertsAsync(double lat, double lon);
        string GetWeatherDescription(int wmoCode);
        string GetWeatherEmoji(int wmoCode);
        string GetWindDirection(double degree);
        string GetUVIndexDescription(double uvIndex);
        string GetAirQualityDescription(double aqi);
    }
}