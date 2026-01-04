using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherBot.Bot;
using WeatherBot.Infrastructure;

namespace WeatherBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddApplicationServices();

            var provider = services.BuildServiceProvider();

            var bot = ActivatorUtilities.CreateInstance<DiscordBot>(provider);
            await bot.RunAsync();
        }
    }
}
