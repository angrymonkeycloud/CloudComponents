namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>
/// Defines a named map zone composed of one or more addresses (or free-text place queries).
/// CloudMaps resolves each address to its administrative boundary polygon automatically —
/// no geocoding or polygon-fetching boilerplate required in the consuming application.
/// </summary>
/// <remarks>
/// <para>
/// <b>Single address:</b>
/// <code>new MapZone(["Paris, France"], Label: "Paris")</code>
/// </para>
/// <para>
/// <b>Multiple addresses in one zone</b> (each is resolved independently and all share the
/// same styling, so overlapping boundaries are intentionally allowed for comparison views):
/// <code>new MapZone(["Lyon", "Marseille"], Label: "Key Cities", FillColor: "#e8420026")</code>
/// </para>
/// <para>
/// Colors accept any CSS color string. Add two hex digits of alpha to a 6-digit hex color for
/// transparency, e.g. <c>"#0078d426"</c> ≈ 15 % opacity blue.
/// </para>
/// </remarks>
public sealed record MapZone
{
    /// <summary>
    /// One or more free-text addresses or place names. Each entry is geocoded independently;
    /// if Azure Maps returns a geometry ID for an entry its actual administrative boundary
    /// polygon is drawn. Entries without a geometry ID (e.g. street addresses, POIs) are
    /// silently skipped — no crash, no fallback rectangle.
    /// </summary>
    public required IReadOnlyList<string> Addresses { get; init; }

    /// <summary>Unique identifier. Auto-generated when not supplied.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Optional label rendered at the visual centroid of the zone on the map.
    /// Also appears in the map legend when provided.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Fill color for the boundary polygon. Accepts any CSS color string.
    /// Defaults to a semi-transparent blue (<c>"rgba(0,120,212,0.15)"</c>).
    /// </summary>
    public string FillColor { get; init; } = "rgba(0,120,212,0.15)";

    /// <summary>Stroke (outline) color. Defaults to <c>"#0078d4"</c>.</summary>
    public string StrokeColor { get; init; } = "#0078d4";

    /// <summary>Stroke width in pixels. Defaults to <c>2</c>.</summary>
    public double StrokeWidth { get; init; } = 2;

    /// <summary>
    /// Optional ISO 3166-1 alpha-2 country code (e.g. <c>"US"</c>, <c>"LB"</c>, <c>"FR"</c>) used
    /// to narrow geocoding results to a specific country. When set, addresses that would
    /// otherwise resolve to a same-named place in another country are locked to this country,
    /// preventing ambiguous matches (e.g. <c>"Lebanon"</c> always resolves to the country rather
    /// than Lebanon, Pennsylvania). Multiple codes can be passed as a comma-separated string
    /// (e.g. <c>"US,CA"</c>).
    /// </summary>
    public string? CountrySet { get; init; }
}
