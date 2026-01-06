using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using WeatherBot.Services.Interfaces;

namespace WeatherBot.Commands
{
    public class WeatherCommands : ApplicationCommandModule
    {
        private readonly IWeatherService _weatherService;

        public WeatherCommands(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [SlashCommand("weather", "Xem thời tiết hiện tại")]
        public async Task WeatherAsync(
            InteractionContext ctx,
            [Option("city", "Tên thành phố")] string city,
            [Option("country", "Mã quốc gia (VN, JP...)")] string country = "",
            [Option("detailed", "Hiển thị chi tiết?")] bool detailed = false)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⏳ Đang lấy dữ liệu thời tiết...")
            );

            try
            {
                var (lat, lon, locationName) =
                    await _weatherService.GetLocationAsync(city, country);

                var currentWeather =
                    await _weatherService.GetCurrentWeatherAsync(lat, lon);

                var embed = detailed
                    ? CreateDetailedWeatherEmbed(currentWeather, locationName)
                    : CreateSimpleWeatherEmbed(currentWeather, locationName);

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(embed)
                );
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"❌ Lỗi: {ex.Message}")
                );
            }
        }

        [SlashCommand("forecast", "Dự báo thời tiết 7 ngày")]
        public async Task ForecastAsync(
            InteractionContext ctx,
            [Option("city", "Tên thành phố")] string city,
            [Option("country", "Mã quốc gia")] string country = "",
            [Option("days", "Số ngày dự báo (1-16)")] long days = 7)
        {
            await ctx.DeferAsync();

            try
            {
                var (lat, lon, locationName) =
                    await _weatherService.GetLocationAsync(city, country);

                var forecast =
                    await _weatherService.GetDailyForecastAsync(lat, lon, (int)Math.Clamp(days, 1, 16));

                var embed = CreateForecastEmbed(forecast, locationName);

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(embed)
                );
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"❌ Lỗi: {ex.Message}")
                );
            }
        }

        [SlashCommand("airquality", "Chất lượng không khí")]
        public async Task AirQualityAsync(
            InteractionContext ctx,
            [Option("city", "Tên thành phố")] string city,
            [Option("country", "Mã quốc gia")] string country = "")
        {
            await ctx.DeferAsync();

            try
            {
                var (lat, lon, locationName) =
                    await _weatherService.GetLocationAsync(city, country);

                var airQuality =
                    await _weatherService.GetAirQualityAsync(lat, lon);

                var embed = CreateAirQualityEmbed(airQuality, locationName);

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(embed)
                );
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"❌ Lỗi: {ex.Message}")
                );
            }
        }

        [SlashCommand("weathermap", "Bản đồ thời tiết")]
        public async Task WeatherMapAsync(
            InteractionContext ctx,
            [Option("city", "Tên thành phố")] string city,
            [Option("country", "Mã quốc gia")] string country = "",
            [Option("type", "Loại bản đồ")]
            [Choice("Mây", "clouds")]
            [Choice("Nhiệt độ", "temperature")]
            [Choice("Mưa", "precipitation")]
            [Choice("Áp suất", "pressure")]
            [Choice("Gió", "wind")] string mapType = "clouds")
        {
            await ctx.DeferAsync();

            try
            {
                var (lat, lon, locationName) =
                    await _weatherService.GetLocationAsync(city, country);

                var mapUrl = GetWeatherMapUrl(lat, lon, mapType);

                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"🗺️ Bản đồ thời tiết - {locationName}")
                    .WithDescription($"Loại: **{GetMapTypeName(mapType)}**")
                    .WithImageUrl(mapUrl)
                    .WithColor(DiscordColor.Blue)
                    .WithFooter($"Tọa độ: {lat:F2}°N, {lon:F2}°E")
                    .WithTimestamp(DateTimeOffset.Now);

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(embed)
                );
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"❌ Lỗi: {ex.Message}")
                );
            }
        }

        [SlashCommand("weatheralert", "Cảnh báo thời tiết")]
        [SlashCooldown(1, 30, SlashCooldownBucketType.User)]
        public async Task WeatherAlertAsync(
            InteractionContext ctx,
            [Option("city", "Tên thành phố")] string city,
            [Option("country", "Mã quốc gia")] string country = "")
        {
            await ctx.DeferAsync();

            var (lat, lon, locationName) =
                await _weatherService.GetLocationAsync(city, country);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"⚠️ Cảnh báo thời tiết - {locationName}")
                .WithDescription("Hiện không có cảnh báo thời tiết nào.")
                .WithColor(DiscordColor.Green)
                .WithFooter("Dữ liệu từ Open-Meteo")
                .WithTimestamp(DateTimeOffset.Now);

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddEmbed(embed)
            );
        }

        private DiscordEmbedBuilder CreateSimpleWeatherEmbed(Models.CurrentWeather weather, string location)
        {
            var emoji = _weatherService.GetWeatherEmoji(weather.WeatherCode);
            var description = _weatherService.GetWeatherDescription(weather.WeatherCode);
            var windDirection = _weatherService.GetWindDirection(weather.WindDirection);
            var dayNight = weather.IsDay ? "☀️ Ban ngày" : "🌙 Ban đêm";

            return new DiscordEmbedBuilder()
                .WithTitle($"{emoji} Thời tiết tại {location}")
                .WithDescription($"**{weather.Temperature:F1}°C** (Cảm giác {weather.FeelsLike:F1}°C)\n{description}")
                .AddField("💨 Gió", $"{weather.WindSpeed:F1} km/h ({windDirection})", true)
                .AddField("💧 Độ ẩm", $"{weather.Humidity}%", true)
                .AddField("🌧️ Mưa", $"{weather.Precipitation:F1} mm", true)
                .AddField("📊 UV", $"{weather.UVIndex:F1} ({_weatherService.GetUVIndexDescription(weather.UVIndex)})", true)
                .AddField("🕒 Thời gian", dayNight, true)
                .AddField("👁️ Tầm nhìn", $"{weather.Visibility / 1000:F1} km", true)
                .WithColor(GetWeatherColor(weather.WeatherCode, weather.IsDay))
                .WithFooter($"Cập nhật: {weather.Timestamp:HH:mm}")
                .WithTimestamp(weather.Timestamp);
        }

        private DiscordEmbedBuilder CreateDetailedWeatherEmbed(Models.CurrentWeather weather, string location)
        {
            var embed = CreateSimpleWeatherEmbed(weather, location);

            embed.AddField("☁️ Mây", $"{weather.CloudCover}%", true)
                 .AddField("💨 Gió giật", $"{weather.WindGusts:F1} km/h", true)
                 .AddField("📏 Áp suất", $"{weather.Pressure:F1} hPa", true);

            return embed;
        }

        private DiscordEmbedBuilder CreateForecastEmbed(Models.WeatherForecast forecast, string location)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"📅 Dự báo thời tiết - {location}")
                .WithDescription($"Dự báo {forecast.DailyForecasts.Count} ngày tới")
                .WithColor(DiscordColor.SpringGreen);

            foreach (var day in forecast.DailyForecasts.Take(7))
            {
                var emoji = _weatherService.GetWeatherEmoji(day.WeatherCode);
                var description = _weatherService.GetWeatherDescription(day.WeatherCode);

                embed.AddField(
                    $"{emoji} {day.Date:dd/MM} ({day.Date:ddd})",
                    $"**{day.MaxTemperature:F1}°C** / **{day.MinTemperature:F1}°C**\n{description}",
                    true
                );
            }

            embed.WithFooter($"Tọa độ: {forecast.Latitude:F2}°N, {forecast.Longitude:F2}°E")
                 .WithTimestamp(DateTimeOffset.Now);

            return embed;
        }

        private DiscordEmbedBuilder CreateAirQualityEmbed(Models.AirQualityData aqi, string location)
        {
            var aqiDescription = _weatherService.GetAirQualityDescription(aqi.AQI);
            var aqiColor = GetAQIColor(aqi.AQI);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"🌬️ Chất lượng không khí - {location}")
                .WithDescription($"**Chỉ số AQI: {aqi.AQI}** ({aqiDescription})")
                .AddField("🧪 PM2.5", $"{aqi.PM25:F1} µg/m³", true)
                .AddField("🌫️ PM10", $"{aqi.PM10:F1} µg/m³", true)
                .AddField("🚗 CO", $"{aqi.CO:F1} ppm", true)
                .AddField("🏭 NO₂", $"{aqi.NO2:F1} ppm", true)
                .AddField("⚡ O₃", $"{aqi.O3:F1} ppm", true)
                .AddField("🏭 SO₂", $"{aqi.SO2:F1} ppm", true)
                .WithColor(aqiColor)
                .WithFooter("AQI: 0-50 Tốt | 51-100 TB | 101-150 Kém | 151-200 Xấu | 201-300 Rất xấu | 301+ Nguy hiểm")
                .WithTimestamp(DateTimeOffset.Now);

            return embed;
        }

        private string GetWeatherMapUrl(double lat, double lon, string type)
        {
            return type switch
            {
                "temperature" => $"https://open-meteo.com/images/temperature?latitude={lat}&longitude={lon}",
                "clouds" => $"https://open-meteo.com/images/clouds?latitude={lat}&longitude={lon}",
                "precipitation" => $"https://open-meteo.com/images/precipitation?latitude={lat}&longitude={lon}",
                "pressure" => $"https://open-meteo.com/images/pressure?latitude={lat}&longitude={lon}",
                "wind" => $"https://open-meteo.com/images/wind?latitude={lat}&longitude={lon}",
                _ => $"https://open-meteo.com/images/temperature?latitude={lat}&longitude={lon}"
            };
        }

        private string GetMapTypeName(string type)
        {
            return type switch
            {
                "temperature" => "Nhiệt độ",
                "clouds" => "Mây",
                "precipitation" => "Lượng mưa",
                "pressure" => "Áp suất",
                "wind" => "Gió",
                _ => "Nhiệt độ"
            };
        }

        private DiscordColor GetWeatherColor(int weatherCode, bool isDay)
        {
            if (weatherCode >= 95 && weatherCode <= 99) // Thunderstorm
                return DiscordColor.DarkRed;
            if (weatherCode >= 80 && weatherCode <= 86) // Rain showers
                return DiscordColor.Blue;
            if (weatherCode >= 51 && weatherCode <= 67) // Rain
                return new DiscordColor(0, 0, 128);
            if (weatherCode >= 71 && weatherCode <= 77) // Snow
                return DiscordColor.White;

            return isDay ? DiscordColor.Gold : DiscordColor.Purple;
        }

        private DiscordColor GetAQIColor(int aqi)
        {
            return aqi switch
            {
                <= 50 => DiscordColor.Green,
                <= 100 => DiscordColor.Yellow,
                <= 150 => DiscordColor.Orange,
                <= 200 => DiscordColor.Red,
                <= 300 => DiscordColor.DarkRed,
                _ => DiscordColor.VeryDarkGray
            };
        }
    }
}