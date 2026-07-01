namespace CloudComponents.Maps.Models;

/// <summary>
/// A single suggestion returned while searching (Azure Maps fuzzy search), shown in the
/// <c>AzureMap</c> built-in search box dropdown.
/// </summary>
public sealed record MapSearchResult(string Address, double Latitude, double Longitude, double North, double South, double East, double West, string? GeometryId)
{
    /// <summary>Secondary line shown under <see cref="Address"/> (e.g. locality/country), when available.</summary>
    public string? Description { get; init; }
}
