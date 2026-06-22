namespace CloudComponents.Maps.Models;

/// <summary>
/// Represents a marker on the map. <see cref="Id"/> is auto-generated unless
/// explicitly provided, so existing markers loaded from a data source can keep
/// their stable identity.
/// </summary>
public sealed record MapMarker(
    double Latitude,
    double Longitude,
    string? Label = null,
    string Color = "#e81123")
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    // ?? Optional info-popup fields ??????????????????????????????????????
    // When provided, the marker's popup will surface these alongside the
    // coordinates. All are optional and rendered only when non-empty.

    /// <summary>Headline shown in the popup (falls back to <see cref="Label"/>).</summary>
    public string? Title { get; init; }

    /// <summary>City / locality name.</summary>
    public string? City { get; init; }

    /// <summary>District / sub-locality name.</summary>
    public string? District { get; init; }

    /// <summary>Administrative subdivision (state / region / governorate).</summary>
    public string? Subdivision { get; init; }

    /// <summary>Country display name.</summary>
    public string? Country { get; init; }

    /// <summary>Cover image URL shown in the popup card.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Formatted area string (e.g. "120 sqm").</summary>
    public string? Area { get; init; }

    /// <summary>Formatted price string (e.g. "$150,000").</summary>
    public string? Price { get; init; }

    /// <summary>When provided, the popup renders a "View" link/button pointing to this URL (relative or absolute).</summary>
    public string? DetailsUrl { get; init; }

    /// <summary>Label for the <see cref="DetailsUrl"/> button. Defaults to "View Property" in the popup template.</summary>
    public string? DetailsLabel { get; init; }
}

