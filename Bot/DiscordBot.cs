using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using WeatherBot.Commands;

namespace WeatherBot.Bot
{
    public class DiscordBot
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _provider;
        private DiscordClient? _discord;

        public DiscordBot(IConfiguration config, IServiceProvider provider)
        {
            _config = config;
            _provider = provider;
        }


        public async Task RunAsync()
        {
            string? token =
                _config["Discord:Token"]
                ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");

            if (string.IsNullOrEmpty(token))
                throw new Exception("Discord token not found");

            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            });

            var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = _provider
            });
            slash.RegisterCommands<WeatherCommands>();
            slash.RegisterCommands<ContextMenuCommands>();

            await _discord.ConnectAsync();
            Console.WriteLine("Bot đang online!");
            await Task.Delay(-1);
        }
    }
}
