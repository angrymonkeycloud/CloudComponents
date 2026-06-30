namespace CloudComponents.Maps.Models;

/// <summary>
/// Result of a reverse geocode operation (lat/lon → address components).
/// </summary>
public sealed record ReverseGeocodeResult(
    string? CountryCode,
    string? CountrySubdivisionCode,
    string? CountrySecondarySubdivision,
    string? Municipality,
    string? MunicipalitySubdivision,
    string? PostalCode);
