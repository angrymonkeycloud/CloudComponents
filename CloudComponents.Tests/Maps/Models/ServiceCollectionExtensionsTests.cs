using AngryMonkey.CloudComponents.Maps.Extensions;
using AngryMonkey.CloudComponents.Maps.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CloudComponents.Tests.Maps.Models;

public sealed class ServiceCollectionExtensionsTests
{
    private static AzureMapsOptions Resolve(IServiceCollection services) =>
        services.BuildServiceProvider().GetRequiredService<IOptions<AzureMapsOptions>>().Value;

    [Fact]
    public void The_key_overload_registers_the_key_in_IOptions()
    {
        ServiceCollection services = new();

        services.AddAzureMaps("my-key");

        Assert.Equal("my-key", Resolve(services).SubscriptionKey);
    }

    [Fact]
    public void The_configure_overload_runs_the_action_on_the_options()
    {
        ServiceCollection services = new();

        services.AddAzureMaps(options => options.SubscriptionKey = "from-action");

        Assert.Equal("from-action", Resolve(services).SubscriptionKey);
    }

    [Fact]
    public void Both_overloads_return_the_same_collection_so_calls_can_be_chained()
    {
        ServiceCollection services = new();

        IServiceCollection byKey = services.AddAzureMaps("k");
        IServiceCollection byAction = services.AddAzureMaps(o => o.SubscriptionKey = "k");

        Assert.Same(services, byKey);
        Assert.Same(services, byAction);
    }

    [Fact]
    public void A_null_configure_action_is_refused_with_its_parameter_name()
    {
        ServiceCollection services = new();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddAzureMaps((Action<AzureMapsOptions>)null!));

        Assert.Equal("configure", exception.ParamName);
    }

    [Fact]
    public void A_refused_null_configure_action_registers_nothing()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(() => services.AddAzureMaps((Action<AzureMapsOptions>)null!));

        Assert.Empty(services);
    }

    [Fact]
    public void The_configure_action_does_not_run_until_the_options_are_first_read_and_then_only_once()
    {
        ServiceCollection services = new();
        int runs = 0;
        services.AddAzureMaps(options => { runs++; options.SubscriptionKey = "k"; });
        Assert.Equal(0, runs);

        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<AzureMapsOptions>>().Value;
        _ = provider.GetRequiredService<IOptions<AzureMapsOptions>>().Value;

        Assert.Equal(1, runs);
    }

    [Fact]
    public void The_options_instance_is_shared_between_resolutions()
    {
        ServiceCollection services = new();
        services.AddAzureMaps("k");
        ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<IOptions<AzureMapsOptions>>().Value, provider.GetRequiredService<IOptions<AzureMapsOptions>>().Value);
    }

    [Fact]
    public void Calling_it_twice_lets_the_later_registration_win()
    {
        ServiceCollection services = new();

        services.AddAzureMaps("first");
        services.AddAzureMaps("second");

        Assert.Equal("second", Resolve(services).SubscriptionKey);
    }

    [Fact]
    public void The_options_are_also_available_through_IOptionsMonitor_and_IOptionsSnapshot()
    {
        ServiceCollection services = new();
        services.AddAzureMaps("k");
        ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("k", provider.GetRequiredService<IOptionsMonitor<AzureMapsOptions>>().CurrentValue.SubscriptionKey);
        using IServiceScope scope = provider.CreateScope();
        Assert.Equal("k", scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AzureMapsOptions>>().Value.SubscriptionKey);
    }

    [Fact]
    public void An_empty_key_is_registered_as_given()
    {
        ServiceCollection services = new();

        services.AddAzureMaps("");

        Assert.Equal("", Resolve(services).SubscriptionKey);
    }

    [Fact]
    public void Without_any_registration_the_options_still_resolve_with_an_empty_key()
    {
        ServiceCollection services = new();
        services.AddOptions();

        Assert.Equal(string.Empty, Resolve(services).SubscriptionKey);
    }

    [Fact]
    public void The_extension_works_alongside_other_options_registrations()
    {
        ServiceCollection services = new();
        services.Configure<AzureMapsOptions>(o => o.SubscriptionKey = "earlier");

        services.AddAzureMaps(o => o.SubscriptionKey += "+later");

        Assert.Equal("earlier+later", Resolve(services).SubscriptionKey);
    }
}
