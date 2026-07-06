namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>
/// A single allowed country, optionally narrowed to specific subdivisions
/// (state/province/governorate/etc.) within that country.
/// </summary>
/// <param name="CountryCode">ISO 3166-1 alpha-2 country code (e.g. <c>"US"</c>).</param>
/// <param name="SubdivisionCodes">
/// Optional subdivision codes that further restrict selection within the country
/// (e.g. <c>"US-CA"</c>, <c>"US-NY"</c>). When <c>null</c>/empty, the entire country is allowed.
/// </param>
public sealed record LocationLockArea(string CountryCode, IReadOnlyList<string>? SubdivisionCodes = null);

/// <summary>
/// Restricts interactive location selection (marker add, center-pin drag, and search-result
/// selection) to one or more countries and/or subdivisions. Assign to
/// <see cref="Components.AzureMap.LocationLock"/> to enable. The component resolves each
/// <see cref="LocationLockArea"/> to a boundary polygon (via geocode + polygon lookup) and
/// rejects interactions whose coordinate falls outside every allowed area.
/// </summary>
public sealed record LocationLockOptions(IReadOnlyList<LocationLockArea> Areas)
{
    /// <summary>Convenience constructor locking to one or more whole countries.</summary>
    public LocationLockOptions(params string[] countryCodes)
        : this(countryCodes.Select(c => new LocationLockArea(c)).ToArray())
    {
    }

    /// <summary>When true (default), the resolved boundary is rendered as a region overlay so users can see the allowed area.</summary>
    public bool ShowBoundary { get; init; } = true;

    /// <summary>Fill color for the boundary overlay (when <see cref="ShowBoundary"/> is true).</summary>
    public string BoundaryFillColor { get; init; } = "rgba(16, 124, 16, 0.10)";

    /// <summary>Stroke color for the boundary overlay.</summary>
    public string BoundaryStrokeColor { get; init; } = "#107c10";

    /// <summary>When true (default), the camera automatically fits to the locked boundary once resolved.</summary>
    public bool ZoomToBoundary { get; init; } = true;

    /// <summary>Message briefly shown when the user attempts to interact outside the locked area.</summary>
    public string RestrictionMessage { get; init; } = "This location is outside the allowed area.";
}
