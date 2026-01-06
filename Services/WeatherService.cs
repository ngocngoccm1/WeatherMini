using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeatherBot.Models;
using WeatherBot.Services.Interfaces;

namespace WeatherBot.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
        }

        public async Task<(double lat, double lon, string locationName)> GetLocationAsync(string city, string country = "")
        {
            var query = string.IsNullOrEmpty(country) ? city : $"{city}, {country}";
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=1&language=vi&format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var result = doc.RootElement.GetProperty("results")[0];
            var lat = result.GetProperty("latitude").GetDouble();
            var lon = result.GetProperty("longitude").GetDouble();
            var name = result.GetProperty("name").GetString();
            var admin1 = result.TryGetProperty("admin1", out var admin1Element) ? admin1Element.GetString() : "";
            var countryName = result.TryGetProperty("country", out var countryElement) ? countryElement.GetString() : "";

            var locationName = $"{name}{(string.IsNullOrEmpty(admin1) ? "" : $", {admin1}")}, {countryName}";

            return (lat, lon, locationName);
        }

        public async Task<CurrentWeather> GetCurrentWeatherAsync(double lat, double lon)
        {
            var url = $"forecast?latitude={lat}&longitude={lon}" +
                     "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,showers,snowfall," +
                     "weather_code,cloud_cover,pressure_msl,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m," +
                     "is_day,uv_index,visibility,cape,evapotranspiration,soil_temperature_0cm,soil_moisture_0_1cm" +
                     "&timezone=auto&forecast_days=1";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var current = doc.RootElement.GetProperty("current");

            return new CurrentWeather
            {
                Temperature = current.GetProperty("temperature_2m").GetDouble(),
                FeelsLike = current.GetProperty("apparent_temperature").GetDouble(),
                Humidity = current.GetProperty("relative_humidity_2m").GetInt32(),
                Precipitation = current.GetProperty("precipitation").GetDouble(),
                WeatherCode = current.GetProperty("weather_code").GetInt32(),
                CloudCover = current.GetProperty("cloud_cover").GetInt32(),
                WindSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
                WindDirection = current.GetProperty("wind_direction_10m").GetInt32(),
                WindGusts = current.GetProperty("wind_gusts_10m").GetDouble(),
                Pressure = current.GetProperty("pressure_msl").GetDouble(),
                UVIndex = current.GetProperty("uv_index").GetDouble(),
                Visibility = current.GetProperty("visibility").GetDouble(),
                IsDay = current.GetProperty("is_day").GetInt32() == 1,
                Timestamp = current.GetProperty("time").GetDateTime()
            };
        }

        public async Task<WeatherForecast> GetDailyForecastAsync(double lat, double lon, int days = 7)
        {
            var url = $"forecast?latitude={lat}&longitude={lon}" +
                     "&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min," +
                     "sunrise,sunset,uv_index_max,precipitation_sum,rain_sum,showers_sum,snowfall_sum,precipitation_hours," +
                     "precipitation_probability_max,wind_speed_10m_max,wind_gusts_10m_max,wind_direction_10m_dominant," +
                     "shortwave_radiation_sum,et0_fao_evapotranspiration" +
                     $"&forecast_days={days}&timezone=auto";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var daily = doc.RootElement.GetProperty("daily");
            var times = daily.GetProperty("time").EnumerateArray();
            var weatherCodes = daily.GetProperty("weather_code").EnumerateArray();
            var tempMax = daily.GetProperty("temperature_2m_max").EnumerateArray();
            var tempMin = daily.GetProperty("temperature_2m_min").EnumerateArray();

            var forecasts = new List<DailyForecast>();

            using var timesEnumerator = times.GetEnumerator();
            using var codesEnumerator = weatherCodes.GetEnumerator();
            using var maxEnumerator = tempMax.GetEnumerator();
            using var minEnumerator = tempMin.GetEnumerator();

            while (timesEnumerator.MoveNext() && codesEnumerator.MoveNext() &&
                   maxEnumerator.MoveNext() && minEnumerator.MoveNext())
            {
                forecasts.Add(new DailyForecast
                {
                    Date = timesEnumerator.Current.GetDateTime(),
                    WeatherCode = codesEnumerator.Current.GetInt32(),
                    MaxTemperature = maxEnumerator.Current.GetDouble(),
                    MinTemperature = minEnumerator.Current.GetDouble()
                });
            }

            return new WeatherForecast
            {
                Latitude = lat,
                Longitude = lon,
                DailyForecasts = forecasts
            };
        }

        public async Task<AirQualityData> GetAirQualityAsync(double lat, double lon)
        {
            var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?" +
                     $"latitude={lat}&longitude={lon}" +
                     "&current=us_aqi,pm10,pm2_5,carbon_monoxide,nitrogen_dioxide,sulphur_dioxide,ozone,dust," +
                     "aerosol_optical_depth,uv_index,alder_pollen,birch_pollen,grass_pollen,mugwort_pollen,olive_pollen,ragweed_pollen" +
                     "&timezone=auto";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var current = doc.RootElement.GetProperty("current");

            return new AirQualityData
            {
                AQI = current.GetProperty("us_aqi").GetInt32(),
                PM25 = current.GetProperty("pm2_5").GetDouble(),
                PM10 = current.GetProperty("pm10").GetDouble(),
                CO = current.GetProperty("carbon_monoxide").GetDouble(),
                NO2 = current.GetProperty("nitrogen_dioxide").GetDouble(),
                SO2 = current.GetProperty("sulphur_dioxide").GetDouble(),
                O3 = current.GetProperty("ozone").GetDouble()
            };
        }

        public async Task<HistoricalWeather> GetHistoricalWeatherAsync(double lat, double lon, DateOnly date)
        {
            var startDate = date.AddDays(-30).ToString("yyyy-MM-dd");
            var endDate = date.ToString("yyyy-MM-dd");

            var url = $"https://archive-api.open-meteo.com/v1/archive?" +
                     $"latitude={lat}&longitude={lon}" +
                     "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
                     $"&start_date={startDate}&end_date={endDate}&timezone=auto";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var daily = doc.RootElement.GetProperty("daily");

            return new HistoricalWeather
            {
                Latitude = lat,
                Longitude = lon,
                Data = json // Có thể parse chi tiết hơn nếu cần
            };
        }

        public string GetWeatherDescription(int wmoCode)
        {
            return wmoCode switch
            {
                0 => "Trời quang",
                1 => "Chủ yếu quang",
                2 => "Có mây rải rác",
                3 => "Nhiều mây",
                45 => "Sương mù",
                48 => "Sương mù giá",
                51 => "Mưa phùn nhẹ",
                53 => "Mưa phùn vừa",
                55 => "Mưa phùn dày",
                56 => "Mưa phùn đóng băng nhẹ",
                57 => "Mưa phùn đóng băng dày",
                61 => "Mưa nhẹ",
                63 => "Mưa vừa",
                65 => "Mưa to",
                66 => "Mưa đá nhẹ",
                67 => "Mưa đá nặng",
                71 => "Tuyết nhẹ",
                73 => "Tuyết vừa",
                75 => "Tuyết dày",
                77 => "Hạt tuyết",
                80 => "Mưa rào nhẹ",
                81 => "Mưa rào vừa",
                82 => "Mưa rào lớn",
                85 => "Tuyết rào nhẹ",
                86 => "Tuyết rào nặng",
                95 => "Giông bão nhẹ/vừa",
                96 => "Giông bão có mưa đá nhẹ",
                99 => "Giông bão có mưa đá nặng",
                _ => "Không xác định"
            };
        }

        public string GetWeatherEmoji(int wmoCode)
        {
            return wmoCode switch
            {
                0 => "☀️",  // Clear sky
                1 => "🌤️",  // Mainly clear
                2 => "⛅",  // Partly cloudy
                3 => "☁️",  // Overcast
                45 => "🌫️", // Fog
                48 => "🌫️", // Fog
                >= 51 and <= 57 => "🌧️", // Drizzle
                >= 61 and <= 67 => "🌧️", // Rain
                >= 71 and <= 77 => "❄️", // Snow
                >= 80 and <= 82 => "🌦️", // Rain showers
                >= 85 and <= 86 => "🌨️", // Snow showers
                >= 95 and <= 99 => "⛈️", // Thunderstorm
                _ => "🌀"
            };
        }

        public string GetWindDirection(double degree)
        {
            return degree switch
            {
                >= 337.5 or < 22.5 => "Bắc",
                >= 22.5 and < 67.5 => "Đông Bắc",
                >= 67.5 and < 112.5 => "Đông",
                >= 112.5 and < 157.5 => "Đông Nam",
                >= 157.5 and < 202.5 => "Nam",
                >= 202.5 and < 247.5 => "Tây Nam",
                >= 247.5 and < 292.5 => "Tây",
                >= 292.5 and < 337.5 => "Tây Bắc",
                _ => "Không xác định"
            };
        }

        public string GetUVIndexDescription(double uvIndex)
        {
            return uvIndex switch
            {
                < 3 => "Thấp",
                >= 3 and < 6 => "Trung bình",
                >= 6 and < 8 => "Cao",
                >= 8 and < 11 => "Rất cao",
                _ => "Cực kỳ cao"
            };
        }

        public string GetAirQualityDescription(double aqi)
        {
            return aqi switch
            {
                <= 50 => "Tốt",
                <= 100 => "Trung bình",
                <= 150 => "Không tốt cho nhóm nhạy cảm",
                <= 200 => "Xấu",
                <= 300 => "Rất xấu",
                _ => "Nguy hiểm"
            };
        }

        public Task<WeatherForecast> GetHourlyForecastAsync(double lat, double lon, int hours = 24)
        {
            // Tương tự như GetDailyForecastAsync nhưng với hourly data
            throw new NotImplementedException();
        }

        public Task<List<WeatherAlert>> GetWeatherAlertsAsync(double lat, double lon)
        {
            // Có thể tích hợp với API cảnh báo thời tiết
            throw new NotImplementedException();
        }
    }
}