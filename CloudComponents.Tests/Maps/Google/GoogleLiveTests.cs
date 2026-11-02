using System.Text.Json;
using CloudComponents.Tests.Support;

namespace CloudComponents.Tests.Maps.Google;

/// <summary>
/// Calls the Google Maps Platform web services the port will rely on, with the real server key, and checks they can fill the same
/// results the Azure code returns today: a point, a viewport, address parts, a place identity, suggestions.
/// Skipped until <c>Maps:Google:ServerKey</c> is set. The Maps JavaScript API cannot be checked from here; see
/// <c>GoogleMapsJsCheck.html</c>.
/// </summary>
[Trait("Category", "Live")]
[Trait("Provider", "Google")]
public sealed class GoogleLiveTests
{
    private const string Geocoding = "https://maps.googleapis.com/maps/api/geocode/json";
    private const string Places = "https://places.googleapis.com/v1";

    private static string Key => TestSecrets.Require(TestSecrets.GoogleServerKey);

    private static string Escaped => Uri.EscapeDataString(Key);

    private static Dictionary<string, string> PlacesHeaders(string fieldMask) => new()
    {
        ["X-Goog-Api-Key"] = Key,
        ["X-Goog-FieldMask"] = fieldMask
    };

    private static void AssertInLebanon(double latitude, double longitude)
    {
        Assert.InRange(latitude, 33.0, 34.7);
        Assert.InRange(longitude, 35.0, 36.7);
    }

    /// <summary>The Geocoding API reports a failure as <c>status</c> in a 200 body; surface it with its message.</summary>
    private static JsonElement GeocodingOk(LiveResponse response)
    {
        JsonElement json = response.EnsureSuccess().Json;
        string status = json.GetProperty("status").GetString() ?? string.Empty;
        string message = json.TryGetProperty("error_message", out JsonElement text) ? text.GetString() ?? string.Empty : string.Empty;
        Assert.True(status == "OK", $"Geocoding API status {status}. {TestSecrets.Redact(message)}");
        return json;
    }

    private static bool HasComponent(JsonElement result, string type, string shortName) =>
        result.GetProperty("address_components").EnumerateArray().Any(component =>
            component.GetProperty("types").EnumerateArray().Any(t => t.GetString() == type)
            && component.GetProperty("short_name").GetString() == shortName);

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task The_geocoding_api_is_enabled_and_a_wrong_key_is_refused()
    {
        GeocodingOk(await LiveHttp.GetAsync($"{Geocoding}?address=Beirut&key={Escaped}"));

        LiveResponse bad = await LiveHttp.GetAsync($"{Geocoding}?address=Beirut&key=not-a-real-key");
        Assert.Equal("REQUEST_DENIED", bad.EnsureSuccess().Json.GetProperty("status").GetString());
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task Geocoding_a_town_gives_a_point_a_viewport_a_place_id_and_the_address_parts()
    {
        // The equivalent of Azure fuzzy search in geocode(): latitude/longitude, north/south/east/west, geometryId, names.
        JsonElement json = GeocodingOk(await LiveHttp.GetAsync($"{Geocoding}?address={Uri.EscapeDataString("Jounieh, Lebanon")}&language=en&key={Escaped}"));

        JsonElement first = json.GetProperty("results")[0];
        JsonElement location = first.GetProperty("geometry").GetProperty("location");
        AssertInLebanon(location.GetProperty("lat").GetDouble(), location.GetProperty("lng").GetDouble());

        JsonElement viewport = first.GetProperty("geometry").GetProperty("viewport");
        Assert.True(viewport.GetProperty("northeast").GetProperty("lat").GetDouble() > viewport.GetProperty("southwest").GetProperty("lat").GetDouble());
        Assert.True(viewport.GetProperty("northeast").GetProperty("lng").GetDouble() > viewport.GetProperty("southwest").GetProperty("lng").GetDouble());

        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("place_id").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("formatted_address").GetString()));
        Assert.True(HasComponent(first, "country", "LB"));
        Assert.Contains(first.GetProperty("types").EnumerateArray().Select(t => t.GetString()), t => t is "locality" or "political" or "administrative_area_level_2");
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task A_country_name_restricted_to_the_country_component_resolves_to_the_country_itself()
    {
        // geocode("Lebanon", "Country", "LB"): components=country:LB keeps a same-named town elsewhere out.
        JsonElement json = GeocodingOk(await LiveHttp.GetAsync($"{Geocoding}?address=Lebanon&components=country:LB&language=en&key={Escaped}"));

        JsonElement first = json.GetProperty("results")[0];
        Assert.Contains(first.GetProperty("types").EnumerateArray().Select(t => t.GetString()), t => t == "country");
        Assert.True(HasComponent(first, "country", "LB"));
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task Reverse_geocoding_a_point_in_beirut_names_the_country_and_a_locality()
    {
        // The equivalent of Azure reverse search in reverseGeocode().
        JsonElement json = GeocodingOk(await LiveHttp.GetAsync($"{Geocoding}?latlng=33.8938,35.5018&language=en&key={Escaped}"));

        JsonElement[] results = [.. json.GetProperty("results").EnumerateArray()];
        Assert.NotEmpty(results);
        Assert.Contains(results, result => HasComponent(result, "country", "LB"));
        Assert.All(results, result => Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("formatted_address").GetString())));
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task Geocoding_does_not_return_a_boundary_shape_only_a_viewport()
    {
        // An executable record of the gap: Azure's polygon APIs give an outline; Google's Geocoding API gives a box.
        // If this ever starts failing, Google began returning outlines and the boundary plan can change.
        JsonElement json = GeocodingOk(await LiveHttp.GetAsync($"{Geocoding}?address={Uri.EscapeDataString("Jounieh, Lebanon")}&key={Escaped}"));

        JsonElement geometry = json.GetProperty("results")[0].GetProperty("geometry");
        Assert.True(geometry.TryGetProperty("viewport", out _));
        Assert.False(geometry.TryGetProperty("coordinates", out _) || geometry.TryGetProperty("polygon", out _) || geometry.TryGetProperty("rings", out _));
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task Places_text_search_finds_a_town_with_a_point_and_a_viewport()
    {
        LiveResponse response = await LiveHttp.PostJsonAsync(
            $"{Places}/places:searchText",
            new { textQuery = "Jounieh Lebanon", languageCode = "en", maxResultCount = 5 },
            PlacesHeaders("places.id,places.displayName,places.formattedAddress,places.location,places.viewport,places.types"));

        JsonElement first = response.EnsureSuccess().Json.GetProperty("places")[0];
        AssertInLebanon(first.GetProperty("location").GetProperty("latitude").GetDouble(), first.GetProperty("location").GetProperty("longitude").GetDouble());
        Assert.True(first.GetProperty("viewport").GetProperty("high").GetProperty("latitude").GetDouble() > first.GetProperty("viewport").GetProperty("low").GetProperty("latitude").GetDouble());
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("id").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("displayName").GetProperty("text").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("formattedAddress").GetString()));
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task Autocomplete_suggests_places_as_the_person_types_and_a_suggestion_resolves_to_a_point()
    {
        // The search box: suggestions while typing, then the chosen one's point and viewport.
        LiveResponse suggestions = await LiveHttp.PostJsonAsync(
            $"{Places}/places:autocomplete",
            new { input = "Jouni", languageCode = "en", includedRegionCodes = new[] { "lb" } },
            new Dictionary<string, string> { ["X-Goog-Api-Key"] = Key });

        JsonElement[] predictions = [.. suggestions.EnsureSuccess().Json.GetProperty("suggestions").EnumerateArray().Where(s => s.TryGetProperty("placePrediction", out _)).Select(s => s.GetProperty("placePrediction"))];
        Assert.NotEmpty(predictions);
        JsonElement first = predictions[0];
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("text").GetProperty("text").GetString()));
        string placeId = first.GetProperty("placeId").GetString()!;

        LiveResponse details = await LiveHttp.GetAsync($"{Places}/places/{placeId}?languageCode=en", PlacesHeaders("id,displayName,formattedAddress,location,viewport,addressComponents"));
        JsonElement place = details.EnsureSuccess().Json;
        AssertInLebanon(place.GetProperty("location").GetProperty("latitude").GetDouble(), place.GetProperty("location").GetProperty("longitude").GetDouble());
        Assert.True(place.TryGetProperty("viewport", out _));
        Assert.Contains(place.GetProperty("addressComponents").EnumerateArray(), component => component.GetProperty("types").EnumerateArray().Any(t => t.GetString() == "country") && component.GetProperty("shortText").GetString() == "LB");
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task Autocomplete_can_be_limited_to_towns_of_one_country_for_the_municipality_import()
    {
        // searchMunicipalities(): localities only, one country.
        LiveResponse response = await LiveHttp.PostJsonAsync(
            $"{Places}/places:autocomplete",
            new { input = "Jou", languageCode = "en", includedRegionCodes = new[] { "lb" }, includedPrimaryTypes = new[] { "locality" } },
            new Dictionary<string, string> { ["X-Goog-Api-Key"] = Key });

        JsonElement[] predictions = [.. response.EnsureSuccess().Json.GetProperty("suggestions").EnumerateArray().Select(s => s.GetProperty("placePrediction"))];
        Assert.NotEmpty(predictions);
        Assert.All(predictions, prediction => Assert.Contains(prediction.GetProperty("types").EnumerateArray().Select(t => t.GetString()), t => t == "locality"));
    }

    [LiveFact(TestSecrets.GoogleServerKey)]
    public async Task The_places_api_refuses_a_wrong_key()
    {
        LiveResponse bad = await LiveHttp.PostJsonAsync(
            $"{Places}/places:searchText",
            new { textQuery = "Beirut" },
            new Dictionary<string, string> { ["X-Goog-Api-Key"] = "not-a-real-key", ["X-Goog-FieldMask"] = "places.id" });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, bad.Status);
        Assert.Contains("API_KEY_INVALID", bad.Body);
    }
}
