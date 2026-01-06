using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.EventArgs;
using WeatherBot.Services.Interfaces;

namespace WeatherBot.Commands
{
    public class ContextMenuCommands : ApplicationCommandModule
    {
        private readonly IWeatherService _weatherService;

        public ContextMenuCommands(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Xem thời tiết")]
        public async Task GetWeatherFromMessage(ContextMenuContext ctx)
        {
            await ctx.DeferAsync(true);

            try
            {
                // Extract location from message content
                var messageContent = ctx.TargetMessage.Content;
                // Simple extraction logic - can be improved with NLP
                var possibleCity = ExtractLocationFromText(messageContent);

                if (string.IsNullOrEmpty(possibleCity))
                {
                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent("❌ Không tìm thấy tên thành phố trong tin nhắn.")
                    );
                    return;
                }

                var (lat, lon, locationName) =
                    await _weatherService.GetLocationAsync(possibleCity);

                var weather = await _weatherService.GetCurrentWeatherAsync(lat, lon);

                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"🌤️ Thời tiết tại {locationName}")
                    .WithDescription($"**{weather.Temperature:F1}°C** - {_weatherService.GetWeatherDescription(weather.WeatherCode)}")
                    .AddField("💨 Gió", $"{weather.WindSpeed:F1} km/h", true)
                    .AddField("💧 Độ ẩm", $"{weather.Humidity}%", true)
                    .WithColor(DiscordColor.Blurple)
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

        private string ExtractLocationFromText(string text)
        {
            // Simple implementation - can be enhanced with proper NLP
            var commonCities = new[] { "Hanoi", "Ho Chi Minh", "Da Nang", "Hue", "Nha Trang", "Dalat", "Sa Pa" };

            foreach (var city in commonCities)
            {
                if (text.Contains(city, StringComparison.OrdinalIgnoreCase))
                    return city;
            }

            return string.Empty;
        }
    }
}