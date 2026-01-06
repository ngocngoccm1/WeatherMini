namespace WeatherBot.Models
{
    public class CurrentWeather
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public double Precipitation { get; set; }
        public int WeatherCode { get; set; }
        public int CloudCover { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public double WindGusts { get; set; }
        public double Pressure { get; set; }
        public double UVIndex { get; set; }
        public double Visibility { get; set; }
        public bool IsDay { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public int WeatherCode { get; set; }
        public double MaxTemperature { get; set; }
        public double MinTemperature { get; set; }
        public double? Precipitation { get; set; }
        public double? WindSpeed { get; set; }
    }

    public class WeatherForecast
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<DailyForecast> DailyForecasts { get; set; } = new();
    }

    public class AirQualityData
    {
        public int AQI { get; set; }
        public double PM25 { get; set; }
        public double PM10 { get; set; }
        public double CO { get; set; }
        public double NO2 { get; set; }
        public double SO2 { get; set; }
        public double O3 { get; set; }
    }

    public class HistoricalWeather
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    public class WeatherAlert
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Severity { get; set; } = string.Empty;
    }
}