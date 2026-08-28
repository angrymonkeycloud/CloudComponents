namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>A municipality returned by the Azure Maps geography search index.</summary>
public sealed record MunicipalitySearchResult(
    string? CountryCode,
    string? CountrySubdivisionCode,
    string? CountrySecondarySubdivision,
    string? LocalName,
    string? Municipality,
    string? MunicipalitySubdivision,
    string? FreeformAddress);

/// <summary>One paged response from an Azure Maps municipality search.</summary>
public sealed record MunicipalitySearchPage(
    int TotalResults,
    IReadOnlyList<MunicipalitySearchResult> Results);
