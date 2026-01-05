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
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                // .AddJsonFile("appsettings.Development.json", optional: false)
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
