using Microsoft.Extensions.DependencyInjection;
using WeatherBot.Services;
using WeatherBot.Services.Interfaces;

namespace WeatherBot.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddHttpClient<IWeatherService, WeatherService>();

            return services;
        }
    }
}
