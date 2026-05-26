using System.Runtime.Serialization;

namespace CloudComponents.Maps.Options;

/// <summary>
/// Azure Maps base style. Names match the Azure Maps Web SDK style identifiers
/// so they can be passed straight through to JS.
/// </summary>
public enum MapStyle
{
    [EnumMember(Value = "road")] Road,
    [EnumMember(Value = "grayscale_light")] GrayscaleLight,
    [EnumMember(Value = "grayscale_dark")] GrayscaleDark,
    [EnumMember(Value = "night")] Night,
    [EnumMember(Value = "road_shaded_relief")] RoadShadedRelief,
    [EnumMember(Value = "satellite")] Satellite,
    [EnumMember(Value = "satellite_road_labels")] SatelliteRoadLabels,
    [EnumMember(Value = "high_contrast_dark")] HighContrastDark,
    [EnumMember(Value = "high_contrast_light")] HighContrastLight,
}

/// <summary>Position anchor used for on-map UI controls.</summary>
public enum MapControlPosition
{
    [EnumMember(Value = "top-left")] TopLeft,
    [EnumMember(Value = "top-right")] TopRight,
    [EnumMember(Value = "bottom-left")] BottomLeft,
    [EnumMember(Value = "bottom-right")] BottomRight
}

/// <summary>Toggleable, optionally positioned built-in map control.</summary>
public sealed record MapControlOption(bool Enabled = true, MapControlPosition Position = MapControlPosition.TopRight);
