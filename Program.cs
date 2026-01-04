using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace WeatherBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
    static async Task MainAsync()
    {
        // Ưu tiên 1: Đọc từ Environment Variable (Railway/cloud)
        // Ưu tiên 2: Đọc từ appsettings.json (chỉ để local dev, optional)
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)  // optional: true → không bắt buộc
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()  // Env var có ưu tiên cao nhất
            .Build();

        string token = config["Discord:Token"]
                    ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");  // Backup cách đọc trực tiếp

        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Token not found! Hãy set env var DISCORD_TOKEN trên Railway hoặc thêm vào appsettings.json để chạy local.");
        }

        var discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        });

        var slash = discord.UseSlashCommands();
        slash.RegisterCommands<WeatherCommands>();

        await discord.ConnectAsync();
        Console.WriteLine("Bot đang online!");
        await Task.Delay(-1);
    }
    public class WeatherCommands : ApplicationCommandModule
    {
        private readonly HttpClient httpClient = new HttpClient();

        [SlashCommand("weather", "Xem thời tiết hiện tại và dự báo của một thành phố")]
        public async Task WeatherCommand(InteractionContext ctx,
            [Option("city", "Tên thành phố (ví dụ: Hanoi)")] string city,
            [Option("country", "Mã quốc gia (ví dụ: VN), optional")] string country = "")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Đang lấy dữ liệu thời tiết..."));

            // Tìm tọa độ từ tên thành phố (dùng geocoding API của Open-Meteo)
            string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=vi";
            if (!string.IsNullOrEmpty(country)) geoUrl += $"&country={country}";

            var geoResponse = await httpClient.GetStringAsync(geoUrl);
            var geoJson = JObject.Parse(geoResponse);

            if (geoJson["results"] == null || !geoJson["results"].HasValues)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Không tìm thấy thành phố nào!"));
                return;
            }

            var result = geoJson["results"][0];
            
            double lat = (double)result["latitude"];
            double lon = (double)result["longitude"];
            string locationName = result["name"] + ", " + result["country"].ToString();

            // Lấy dữ liệu thời tiết
            string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code,wind_speed_10m&hourly=temperature_2m&daily=temperature_2m_max,temperature_2m_min,weather_code&timezone=Asia%2FBangkok";

            var weatherResponse = await httpClient.GetStringAsync(weatherUrl);
            var weatherJson = JObject.Parse(weatherResponse);

            var current = weatherJson["current"];
            double temp = (double)current["temperature_2m"];
            string weatherDesc = GetWeatherDescription((int)current["weather_code"]);

            // Embed đẹp
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Thời tiết tại {locationName}")
                .WithDescription($"**Hiện tại:** {temp}°C - {weatherDesc}")
                .AddField("Gió", $"{current["wind_speed_10m"]} km/h", true)
                .WithColor(DiscordColor.Blurple)
                .WithTimestamp(DateTimeOffset.Now);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private string GetWeatherDescription(int code)
        {
            // Một số code phổ biến từ Open-Meteo (WMO codes)
            return code switch
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
}}