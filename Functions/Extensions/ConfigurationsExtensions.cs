using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImageIngest.Functions.Extensions;

public static class ConfigurationsExtensions
{
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TargetSourceOptions>(configuration.GetSection(nameof(TargetSourceOptions)));
        return services;
    }
}