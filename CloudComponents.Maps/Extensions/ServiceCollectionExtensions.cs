using AngryMonkey.CloudComponents.Maps.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AngryMonkey.CloudComponents.Maps.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure Maps configuration so the <c>AzureMap</c> component can
    /// resolve its subscription key from DI instead of requiring it as a
    /// component parameter.
    /// </summary>
    public static IServiceCollection AddAzureMaps(this IServiceCollection services, Action<AzureMapsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    /// <summary>Registers Azure Maps using a literal subscription key.</summary>
    public static IServiceCollection AddAzureMaps(this IServiceCollection services, string subscriptionKey)
        => services.AddAzureMaps(o => o.SubscriptionKey = subscriptionKey);

    /// <summary>
    /// Registers Google Maps browser configuration so <c>GoogleMap</c> can resolve
    /// its API key and vector Map ID from dependency injection.
    /// </summary>
    public static IServiceCollection AddGoogleMaps(this IServiceCollection services, Action<GoogleMapsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    /// <summary>Registers Google Maps using a browser API key and optional cloud Map ID.</summary>
    public static IServiceCollection AddGoogleMaps(
        this IServiceCollection services,
        string apiKey,
        string? mapId = null)
        => services.AddGoogleMaps(options =>
        {
            options.ApiKey = apiKey;
            options.MapId = mapId ?? string.Empty;
        });
}
