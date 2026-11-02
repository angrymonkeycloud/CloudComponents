using Microsoft.Extensions.Configuration;

namespace CloudComponents.Tests.Support;

/// <summary>
/// The keys the live map tests use, read from this project's user secrets or from environment variables
/// (<c>Maps__Azure__SubscriptionKey</c> and so on). Nothing here is ever written to the repository or to a test's output.
/// </summary>
public static class TestSecrets
{
    /// <summary>The Azure Maps subscription (shared) key.</summary>
    public const string AzureKey = "Maps:Azure:SubscriptionKey";

    /// <summary>A Google Maps Platform key for the web services (Geocoding, Places): no HTTP-referrer restriction, since servers send none.</summary>
    public const string GoogleServerKey = "Maps:Google:ServerKey";

    /// <summary>A Google Maps Platform key for the Maps JavaScript API, restricted by HTTP referrer. It cannot be used from a test; see <c>Maps/Google/GoogleMapsJsCheck.html</c>.</summary>
    public const string GoogleBrowserKey = "Maps:Google:BrowserKey";

    /// <summary>Set to <c>1</c> to make a live test fail, instead of being skipped, when its key is missing.</summary>
    private const string RequireLiveSetting = "CLOUDCOMPONENTS_REQUIRE_LIVE";

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(TestSecrets).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();

    public static bool RequireLive => Configuration[RequireLiveSetting] is "1" or "true";

    public static string? Find(string name) =>
        Configuration[name] is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    public static bool Has(string name) => Find(name) is not null;

    /// <summary>The key, or a failure that names the setting without ever naming a value.</summary>
    public static string Require(string name) =>
        Find(name) ?? throw new InvalidOperationException($"The secret '{name}' is not set. See CloudComponents.Tests/README.md.");

    /// <summary>Replaces every configured key in <paramref name="text"/>, so a failure message can show a URL without leaking its key.</summary>
    public static string Redact(string text)
    {
        foreach (string name in new[] { AzureKey, GoogleServerKey, GoogleBrowserKey })
            if (Find(name) is { Length: >= 6 } key)
                text = text.Replace(key, "***", StringComparison.Ordinal).Replace(Uri.EscapeDataString(key), "***", StringComparison.Ordinal);

        return text;
    }
}
