namespace WeatherBot.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<(double lat, double lon, string name)>
            GetLocationAsync(string city, string country);

        Task<(double temp, int code, double wind)>
            GetCurrentWeatherAsync(double lat, double lon);

        string GetWeatherDescription(int code);
    }
}
