using System.Text.Json;
using WeatherBot.Models.Geo;
using WeatherBot.Models.Weather;
using WeatherBot.Services.Interfaces;

namespace WeatherBot.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(double lat, double lon, string name)>
            GetLocationAsync(string city, string country)
        {
            string url =
                $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=vi";

            if (!string.IsNullOrEmpty(country))
                url += $"&country={country}";

            var response = await _httpClient.GetStringAsync(url);
            var geo = JsonSerializer.Deserialize<GeoResponse>(response, JsonOptions);

            if (geo?.Results == null || geo.Results.Count == 0)
                throw new Exception("City not found");

            var r = geo.Results[0];
            return (r.Latitude, r.Longitude, $"{r.Name}, {r.Country}");
        }

        public async Task<(double temp, int code, double wind)>
            GetCurrentWeatherAsync(double lat, double lon)
        {
            string url =
                $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                $"&current=temperature_2m,weather_code,wind_speed_10m&timezone=Asia%2FBangkok";

            var response = await _httpClient.GetStringAsync(url);
            var weather = JsonSerializer.Deserialize<WeatherResponse>(response, JsonOptions);

            if (weather?.Current == null)
                throw new Exception("Weather data not available");

            return (
                weather.Current.Temperature,
                weather.Current.WeatherCode,
                weather.Current.WindSpeed
            );
        }

        public string GetWeatherDescription(int code) => code switch
        {
            0 => "Trời quang",
            1 or 2 or 3 => "Có mây",
            45 or 48 => "Sương mù",
            51 or 53 or 55 => "Mưa phùn",
            61 or 63 or 65 => "Mưa",
            80 or 81 or 82 => "Mưa rào",
            95 or 96 or 99 => "Dông",
            _ => "Không rõ"
        };
    }
}
