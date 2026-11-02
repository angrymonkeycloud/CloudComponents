using System.Text.Json;
using System.Text.RegularExpressions;
using CloudComponents.Tests.Support;

namespace CloudComponents.Tests.Maps.Azure;

/// <summary>
/// Calls Azure Maps with the real subscription key, with the same requests the map's JavaScript makes
/// (<c>CloudComponents.Maps/wwwroot/mapInterop.js</c>) and the CDM server makes, and checks the answers carry what the code reads.
/// Skipped until <c>Maps:Azure:SubscriptionKey</c> is set.
/// </summary>
[Trait("Category", "Live")]
[Trait("Provider", "Azure")]
public sealed class AzureLiveTests
{
    private const string Atlas = "https://atlas.microsoft.com";

    private static string Key => Uri.EscapeDataString(TestSecrets.Require(TestSecrets.AzureKey));

    private static (double Latitude, double Longitude) Position(JsonElement result) =>
        (result.GetProperty("position").GetProperty("lat").GetDouble(), result.GetProperty("position").GetProperty("lon").GetDouble());

    private static void AssertInLebanon(double latitude, double longitude)
    {
        Assert.InRange(latitude, 33.0, 34.7);
        Assert.InRange(longitude, 35.0, 36.7);
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task The_key_is_accepted_and_a_wrong_one_is_refused()
    {
        LiveResponse good = await LiveHttp.GetAsync($"{Atlas}/search/fuzzy/json?api-version=1.0&subscription-key={Key}&query=Beirut&limit=1");
        LiveResponse bad = await LiveHttp.GetAsync($"{Atlas}/search/fuzzy/json?api-version=1.0&subscription-key=not-a-real-key&query=Beirut&limit=1");

        good.EnsureSuccess();
        Assert.True((int)bad.Status is 401 or 403, $"A wrong key should be refused, got {bad.Describe()}");
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task The_web_sdk_the_map_loads_is_served_at_the_version_the_code_asks_for()
    {
        string source = File.ReadAllText(MapsSource.Path("wwwroot", "mapInterop.js"));
        string version = Regex.Match(source, @"const ATLAS_VERSION = '(?<v>[^']+)'").Groups["v"].Value;
        Assert.NotEmpty(version);

        LiveResponse script = await LiveHttp.GetAsync($"{Atlas}/sdk/javascript/mapcontrol/{version}/atlas.min.js");
        LiveResponse styles = await LiveHttp.GetAsync($"{Atlas}/sdk/javascript/mapcontrol/{version}/atlas.min.css");

        Assert.Contains("atlas", script.EnsureSuccess().Body);
        Assert.Contains("{", styles.EnsureSuccess().Body);
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task Fuzzy_search_resolves_a_town_to_a_point_a_viewport_and_the_address_parts_the_code_reads()
    {
        // geocode() and search() in mapInterop.js read these fields.
        JsonElement response = (await LiveHttp.GetAsync($"{Atlas}/search/fuzzy/json?api-version=1.0&subscription-key={Key}&query=Jounieh%20Lebanon&limit=5")).EnsureSuccess().Json;

        JsonElement first = response.GetProperty("results")[0];
        (double latitude, double longitude) = Position(first);
        AssertInLebanon(latitude, longitude);
        Assert.True(first.TryGetProperty("viewport", out _) || first.TryGetProperty("boundingBox", out _), "A town comes with a viewport the camera can fit to.");
        Assert.True(first.TryGetProperty("entityType", out _) || first.TryGetProperty("type", out _));
        Assert.False(string.IsNullOrWhiteSpace(first.GetProperty("address").GetProperty("freeformAddress").GetString()));
        Assert.Equal("LB", first.GetProperty("address").GetProperty("countryCode").GetString());
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task A_country_name_resolves_to_the_country_itself_when_the_geography_index_is_asked()
    {
        // geocode(query, "Country", "LB"): "Lebanon" must not become a same-named town elsewhere.
        JsonElement response = (await LiveHttp.GetAsync($"{Atlas}/search/fuzzy/json?api-version=1.0&subscription-key={Key}&query=Lebanon&limit=1&idxSet=Geo&entityType=Country&countrySet=LB")).EnsureSuccess().Json;

        JsonElement first = response.GetProperty("results")[0];
        Assert.Equal("Country", first.GetProperty("entityType").GetString());
        Assert.Equal("LB", first.GetProperty("address").GetProperty("countryCode").GetString());
        Assert.True(first.TryGetProperty("dataSources", out JsonElement sources) && sources.TryGetProperty("geometry", out JsonElement geometry) && geometry.TryGetProperty("id", out _),
            "A geometry id is what lets the boundary be fetched.");
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task Reverse_geocoding_a_point_in_beirut_names_the_country_and_the_municipality()
    {
        JsonElement response = (await LiveHttp.GetAsync($"{Atlas}/search/address/reverse/json?api-version=1.0&subscription-key={Key}&query=33.8938,35.5018&language=en-US")).EnsureSuccess().Json;

        JsonElement address = response.GetProperty("addresses")[0].GetProperty("address");
        Assert.Equal("LB", address.GetProperty("countryCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(address.GetProperty("freeformAddress").GetString()));
        Assert.True(address.TryGetProperty("municipality", out _) || address.TryGetProperty("localName", out _) || address.TryGetProperty("countrySubdivision", out _));
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task The_municipality_search_pages_through_a_countrys_towns_with_the_fields_the_import_reads()
    {
        // searchMunicipalities(): geography index only, municipalities only, one country, paged.
        JsonElement response = (await LiveHttp.GetAsync($"{Atlas}/search/fuzzy/json?api-version=1.0&subscription-key={Key}&query=Jou&idxSet=Geo&entityType=Municipality&countrySet=LB&limit=10&ofs=0&language=en-US&typeahead=true")).EnsureSuccess().Json;

        Assert.True(response.GetProperty("summary").GetProperty("totalResults").GetInt32() >= 1);
        JsonElement results = response.GetProperty("results");
        Assert.NotEqual(0, results.GetArrayLength());
        foreach (JsonElement result in results.EnumerateArray())
        {
            JsonElement address = result.GetProperty("address");
            Assert.Equal("LB", address.GetProperty("countryCode").GetString());
            Assert.True(address.TryGetProperty("municipality", out _) || address.TryGetProperty("localName", out _) || address.TryGetProperty("municipalitySubdivision", out _));
        }

        JsonElement nextPage = (await LiveHttp.GetAsync($"{Atlas}/search/fuzzy/json?api-version=1.0&subscription-key={Key}&query=Jou&idxSet=Geo&entityType=Municipality&countrySet=LB&limit=1&ofs=1&language=en-US")).EnsureSuccess().Json;
        Assert.True(nextPage.GetProperty("summary").GetProperty("offset").GetInt32() == 1);
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task The_current_boundary_api_returns_an_outline_for_a_point_in_a_city()
    {
        // getBoundaryPolygon(): newest api-version first, then the GA one. The older geometry-id lookup no longer returns shapes
        // for many places, so this is the call that keeps outlines drawing.
        List<string> attempts = [];

        foreach (string version in new[] { "2026-01-01", "2025-01-01" })
        {
            LiveResponse response = await LiveHttp.GetAsync($"{Atlas}/search/polygon?api-version={version}&subscription-key={Key}&coordinates=35.5018,33.8938&resultType=locality&resolution=medium");
            attempts.Add($"{version}: {(int)response.Status}");

            if (!response.IsSuccess)
                continue;

            Assert.Contains("coordinates", response.Body);
            Assert.Matches("Polygon", response.Body);
            return;
        }

        Assert.Fail($"Neither boundary api-version answered ({string.Join(", ", attempts)}); outlines would not draw.");
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task The_address_search_the_server_uses_for_jurisdictions_answers_with_the_administrative_levels()
    {
        // CDM MethodsController: search/address/json for administrative areas.
        JsonElement response = (await LiveHttp.GetAsync($"{Atlas}/search/address/json?api-version=1.0&subscription-key={Key}&query=Jounieh&countrySet=LB&limit=5")).EnsureSuccess().Json;

        JsonElement first = response.GetProperty("results")[0];
        JsonElement address = first.GetProperty("address");
        Assert.Equal("LB", address.GetProperty("countryCode").GetString());
        Assert.True(address.TryGetProperty("countrySubdivision", out _) || address.TryGetProperty("municipality", out _));
    }

    [LiveFact(TestSecrets.AzureKey)]
    public async Task The_road_tiles_the_map_draws_are_served_and_imagery_is_reported_either_way()
    {
        LiveResponse road = await LiveHttp.GetAsync($"{Atlas}/map/tile?api-version=2.1&tilesetId=microsoft.base.road&zoom=2&x=2&y=1&subscription-key={Key}");
        road.EnsureSuccess();
        Assert.NotEmpty(road.Body);

        // Satellite imagery is not on every Azure Maps pricing tier; the map falls back to road on a 403 (see _onMapSdkError).
        // This only has to answer, so the fallback's reason is visible in the results.
        LiveResponse imagery = await LiveHttp.GetAsync($"{Atlas}/map/tile?api-version=2.1&tilesetId=microsoft.imagery&zoom=2&x=2&y=1&subscription-key={Key}");
        Assert.True(imagery.IsSuccess || (int)imagery.Status == 403, $"Imagery tiles should either be served or refused for the tier, got {imagery.Describe()}");
    }
}
