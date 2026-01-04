using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using WeatherBot.Services;
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

        [SlashCommand("weather", "Xem thời tiết hiện tại của một thành phố")]
        public async Task WeatherAsync(
            InteractionContext ctx,
            [Option("city", "Tên thành phố")] string city,
            [Option("country", "Mã quốc gia (VN, JP...)")] string country = "")
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⏳ Đang lấy dữ liệu thời tiết...")
            );

            try
            {
                var (lat, lon, locationName) =
                    await _weatherService.GetLocationAsync(city, country);

                var (temp, code, wind) =
                    await _weatherService.GetCurrentWeatherAsync(lat, lon);

                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"🌤 Thời tiết tại {locationName}")
                    .WithDescription($"**{temp}°C** - {_weatherService.GetWeatherDescription(code)}")
                    .AddField("💨 Gió", $"{wind} km/h", true)
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
    }
}
