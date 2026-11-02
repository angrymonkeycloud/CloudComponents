namespace CloudComponents.Tests.Support;

/// <summary>
/// A test that calls a real service with a real key. It is skipped, with the missing setting named, when a key is not configured;
/// set <c>CLOUDCOMPONENTS_REQUIRE_LIVE=1</c> to fail instead, as a build that must prove the keys work would.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute(params string[] secrets)
    {
        Traits = secrets;

        string[] missing = [.. secrets.Where(secret => !TestSecrets.Has(secret))];

        if (missing.Length > 0 && !TestSecrets.RequireLive)
            Skip = $"Live test: set {string.Join(" and ", missing)} (user secrets or environment) to run it.";
    }

    public string[] Traits { get; }
}
