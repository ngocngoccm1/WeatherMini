using System.Text.Json.Serialization;

namespace WeatherBot.Models.Weather
{
    public class WeatherResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; set; }
    }
}
