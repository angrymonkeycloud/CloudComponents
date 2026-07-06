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
}
