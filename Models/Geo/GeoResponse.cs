using System.Text.Json.Serialization;

namespace WeatherBot.Models.Geo
{
    public class GeoResponse
    {
        [JsonPropertyName("results")]
        public List<GeoResult>? Results { get; set; }
    }
}
