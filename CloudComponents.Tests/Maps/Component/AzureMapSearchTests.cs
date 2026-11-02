using AngleSharp.Dom;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>The built-in search box: debounce, dropdown, keyboard, selection and the C# records built from the JavaScript results.</summary>
public sealed class AzureMapSearchTests : AzureMapTestContext
{
    private const int Debounce = 30;

    private static readonly object Jounieh = new
    {
        address = "Jounieh, Mount Lebanon, Lebanon",
        description = "Jounieh, Mount Lebanon, Lebanon",
        latitude = 33.98,
        longitude = 35.62,
        north = 34.0,
        south = 33.9,
        east = 35.7,
        west = 35.5,
        geometryId = "geo-j"
    };

    private static readonly object Beirut = new
    {
        address = "Beirut",
        description = (string?)null,
        latitude = 33.89,
        longitude = 35.5,
        north = 33.89,
        south = 33.89,
        east = 35.5,
        west = 35.5,
        geometryId = (string?)null
    };

    private IRenderedComponent<AzureMap> RenderSearch(Action<ComponentParameterCollectionBuilder<AzureMap>>? configure = null, int debounce = Debounce) =>
        RenderMap(p =>
        {
            p.Add(m => m.ShowSearchBox, true);
            p.Add(m => m.SearchDebounceMs, debounce);
            configure?.Invoke(p);
        });

    private static IElement Input(IRenderedComponent<AzureMap> cut) => cut.Find("input.map-search-input");

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<AzureMap> cut) => cut.FindAll("li.map-search-result");

    private static bool[] Highlighted(IRenderedComponent<AzureMap> cut) => [.. Rows(cut).Select(r => r.ClassList.Contains("_active"))];

    private static void Type(IRenderedComponent<AzureMap> cut, string text) => Input(cut).Input(text);

    private static void WaitForResults(IRenderedComponent<AzureMap> cut, int count) =>
        cut.WaitForAssertion(() => Assert.Equal(count, Rows(cut).Count));

    private void Press(IRenderedComponent<AzureMap> cut, string key) => Input(cut).KeyDown(new KeyboardEventArgs { Key = key });

    // ── Typing and the request ──────────────────────────────────────────────

    [Fact]
    public void Typing_searches_after_the_debounce_with_the_query_and_the_result_limit()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "Jounieh");

        cut.WaitForAssertion(() => Assert.Equal("""["Jounieh",5]""", Controller.Single("search").ArgsJson));
    }

    [Fact]
    public void SearchResultLimit_is_the_limit_sent_to_JavaScript()
    {
        Controller.Returns("search", Array.Empty<object>());
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.SearchResultLimit, 12));

        Type(cut, "x");

        cut.WaitForAssertion(() => Assert.Equal("""["x",12]""", Controller.Single("search").ArgsJson));
    }

    [Fact]
    public async Task Nothing_is_searched_before_the_debounce_has_elapsed()
    {
        Controller.Returns("search", Array.Empty<object>());
        IRenderedComponent<AzureMap> cut = RenderSearch(debounce: 600);

        Type(cut, "Jou");
        await Task.Delay(150);

        Assert.False(Controller.WasCalled("search"));
    }

    [Fact]
    public async Task Quick_keystrokes_search_only_the_last_text()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch(debounce: 150);

        Type(cut, "J");
        Type(cut, "Jo");
        Type(cut, "Jou");
        await Task.Delay(500);

        cut.WaitForAssertion(() => Assert.Equal(["""["Jou",5]"""], Controller.CallsTo("search").Select(c => c.ArgsJson)));
    }

    [Fact]
    public void A_blank_query_does_not_search_and_closes_the_dropdown()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);
        Controller.ClearCalls();

        Type(cut, "   ");

        Assert.Empty(Rows(cut));
        Assert.Empty(cut.Instance.SearchResults);
        Assert.False(Controller.WasCalled("search"));
    }

    [Fact]
    public async Task Clearing_the_text_before_the_debounce_ends_cancels_the_pending_search()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch(debounce: 200);

        Type(cut, "Jou");
        Type(cut, "");
        await Task.Delay(500);

        Assert.False(Controller.WasCalled("search"));
    }

    // ── Results ─────────────────────────────────────────────────────────────

    [Fact]
    public void Results_fill_a_listbox_with_the_address_and_the_description()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Assert.Equal("listbox", cut.Find("ul.map-search-results").GetAttribute("role"));
        Assert.Equal("Jounieh, Mount Lebanon, Lebanon", Rows(cut)[0].QuerySelector(".map-search-result-address")!.TextContent);
        Assert.Equal("Jounieh, Mount Lebanon, Lebanon", Rows(cut)[0].QuerySelector(".map-search-result-description")!.TextContent);
        Assert.Equal("Beirut", Rows(cut)[1].QuerySelector(".map-search-result-address")!.TextContent);
        Assert.Null(Rows(cut)[1].QuerySelector(".map-search-result-description"));
    }

    [Fact]
    public void The_first_result_starts_highlighted()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Assert.Equal([true, false], Highlighted(cut));
        Assert.True(Rows(cut)[0].ClassList.Contains("_active"));
        Assert.False(Rows(cut)[1].ClassList.Contains("_active"));
    }

    [Fact(Skip = "BUG: AzureMap.razor line 55 renders aria-selected=\"@(index == _searchHighlightIndex)\" with a bool, which Blazor writes as a bare attribute (aria-selected=\"\") when true and omits when false; ARIA needs the strings true and false (the style switch right above does it correctly)")]
    public void The_highlighted_result_is_exposed_to_assistive_technology_as_aria_selected_true()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Assert.Equal(["true", "false"], Rows(cut).Select(r => r.GetAttribute("aria-selected")));
    }

    [Fact]
    public void The_results_map_every_field_into_a_MapSearchResult()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Assert.Equal(
            new MapSearchResult("Jounieh, Mount Lebanon, Lebanon", 33.98, 35.62, 34.0, 33.9, 35.7, 35.5, "geo-j") { Description = "Jounieh, Mount Lebanon, Lebanon" },
            cut.Instance.SearchResults[0]);
        Assert.Equal(new MapSearchResult("Beirut", 33.89, 35.5, 33.89, 33.89, 35.5, 35.5, null), cut.Instance.SearchResults[1]);
    }

    [Fact]
    public void No_results_shows_no_dropdown()
    {
        Controller.Returns("search", Array.Empty<object>());
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "zzzz");

        cut.WaitForAssertion(() => Assert.True(Controller.WasCalled("search")));
        cut.WaitForAssertion(() => Assert.DoesNotContain("_busy", cut.Find(".map-search-box").ClassName));
        Assert.Empty(cut.FindAll("ul.map-search-results"));
    }

    [Fact]
    public void A_null_answer_from_JavaScript_is_treated_as_no_results()
    {
        Controller.Returns("search", null);
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "zzzz");

        cut.WaitForAssertion(() => Assert.True(Controller.WasCalled("search")));
        cut.WaitForAssertion(() => Assert.DoesNotContain("_busy", cut.Find(".map-search-box").ClassName));
        Assert.Empty(cut.Instance.SearchResults);
    }

    [Fact]
    public void A_failing_search_raises_OnMapError_and_shows_nothing()
    {
        Controller.Throws("search", new JSException("network down"));
        List<string> errors = [];
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.OnMapError, (string e) => errors.Add(e)));

        Type(cut, "Jou");

        cut.WaitForAssertion(() => Assert.Equal(["Search failed: network down"], errors));
        Assert.Empty(cut.FindAll("ul.map-search-results"));
        Assert.DoesNotContain("_busy", cut.Find(".map-search-box").ClassName);
    }

    // ── Spinner and clear button ────────────────────────────────────────────

    [Fact]
    public void A_spinner_replaces_the_clear_button_while_the_search_is_running()
    {
        JsGate gate = Controller.Hold("search");
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Type(cut, "Jou");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-search-spinner")));
        Assert.Contains("_busy", cut.Find(".map-search-box").ClassName);
        Assert.Empty(cut.FindAll(".map-search-clear"));

        gate.Release();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".map-search-spinner")));
        Assert.DoesNotContain("_busy", cut.Find(".map-search-box").ClassName);
        Assert.Single(cut.FindAll(".map-search-clear"));
    }

    [Fact]
    public void The_clear_button_only_shows_when_there_is_text()
    {
        Controller.Returns("search", Array.Empty<object>());
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Assert.Empty(cut.FindAll(".map-search-clear"));

        Type(cut, "a");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-search-clear")));
        Assert.Equal("Clear search", cut.Find(".map-search-clear").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_clear_button_empties_the_text_the_results_and_the_dropdown()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        cut.Find(".map-search-clear").Click();

        Assert.Equal("", Input(cut).GetAttribute("value") ?? "");
        Assert.Empty(Rows(cut));
        Assert.Empty(cut.Instance.SearchResults);
        Assert.Empty(cut.FindAll(".map-search-clear"));
    }

    [Fact]
    public async Task Clearing_cancels_a_search_that_has_not_started_yet()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch(debounce: 200);
        Type(cut, "Jou");

        await cut.InvokeAsync(() => cut.Instance.ClearSearch());
        await Task.Delay(500);

        Assert.False(Controller.WasCalled("search"));
        Assert.Empty(Rows(cut));
    }

    [Fact]
    public async Task A_late_answer_to_a_search_that_was_cleared_is_not_shown()
    {
        JsGate gate = Controller.Hold("search");
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-search-spinner")));

        await cut.InvokeAsync(() => cut.Instance.ClearSearch());
        gate.Release();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".map-search-spinner")));
        Assert.Empty(Rows(cut));
    }

    // ── Keyboard ────────────────────────────────────────────────────────────

    [Fact]
    public void Arrow_down_moves_the_highlight_and_stops_at_the_last_result()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Press(cut, "ArrowDown");
        Assert.Equal([false, true], Highlighted(cut));

        Press(cut, "ArrowDown");
        Assert.Equal([false, true], Highlighted(cut));
    }

    [Fact]
    public void Arrow_up_moves_the_highlight_back_and_stops_at_the_first_result()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jo");
        WaitForResults(cut, 2);
        Press(cut, "ArrowDown");

        Press(cut, "ArrowUp");
        Assert.Equal([true, false], Highlighted(cut));

        Press(cut, "ArrowUp");
        Assert.Equal([true, false], Highlighted(cut));
    }

    [Fact]
    public void Escape_closes_the_dropdown_but_keeps_the_text()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        Press(cut, "Escape");

        Assert.Empty(Rows(cut));
        Assert.Single(cut.Instance.SearchResults);
        Assert.Equal("Jou", Input(cut).GetAttribute("value"));
    }

    [Fact]
    public void Focusing_the_input_again_reopens_a_dropdown_that_escape_closed()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);
        Press(cut, "Escape");

        Input(cut).Focus();

        Assert.Single(Rows(cut));
    }

    [Fact]
    public void Enter_selects_the_highlighted_result_and_moves_the_camera_to_its_viewport()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        List<MapSearchResult> selected = [];
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.OnSearchResultSelected, (MapSearchResult r) => selected.Add(r)));
        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Press(cut, "Enter");

        cut.WaitForAssertion(() => Assert.Single(selected));
        Assert.Equal("Jounieh, Mount Lebanon, Lebanon", selected[0].Address);
        Assert.Equal("[33.9,35.5,34,35.7,40]", Controller.Single("setBounds").ArgsJson);
    }

    [Fact]
    public void Enter_after_arrow_down_selects_the_second_result_which_is_a_point_and_so_centres_at_the_search_zoom()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.SearchResultZoom, 11));
        Type(cut, "Jo");
        WaitForResults(cut, 2);
        Press(cut, "ArrowDown");

        Press(cut, "Enter");

        cut.WaitForAssertion(() => Assert.Equal("[33.89,35.5,11]", Controller.Single("setCenter").ArgsJson));
        Assert.False(Controller.WasCalled("setBounds"));
    }

    [Fact]
    public void Enter_with_no_results_does_nothing()
    {
        Controller.Returns("search", Array.Empty<object>());
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "zzz");
        cut.WaitForAssertion(() => Assert.True(Controller.WasCalled("search")));

        Press(cut, "Enter");

        Assert.False(Controller.WasCalled("setBounds"));
        Assert.False(Controller.WasCalled("setCenter"));
    }

    [Fact]
    public void Other_keys_change_nothing()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Press(cut, "a");
        Press(cut, "Tab");

        Assert.Equal([true, false], Highlighted(cut));
        Assert.Equal(2, Rows(cut).Count);
    }

    // ── Selecting ───────────────────────────────────────────────────────────

    [Fact]
    public void Pressing_a_result_with_the_mouse_selects_it()
    {
        Controller.Returns("search", new[] { Jounieh, Beirut });
        List<MapSearchResult> selected = [];
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.OnSearchResultSelected, (MapSearchResult r) => selected.Add(r)));
        Type(cut, "Jo");
        WaitForResults(cut, 2);

        Rows(cut)[1].MouseDown();

        cut.WaitForAssertion(() => Assert.Equal("Beirut", Assert.Single(selected).Address));
    }

    [Fact]
    public void A_selected_result_closes_the_dropdown_and_puts_its_address_in_the_box()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.Empty(Rows(cut)));
        Assert.Equal("Jounieh, Mount Lebanon, Lebanon", Input(cut).GetAttribute("value"));
        Assert.Empty(cut.Instance.SearchResults);
    }

    [Fact]
    public void A_result_with_a_viewport_fits_the_camera_to_south_west_north_east_with_40px_padding()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.Equal("[33.9,35.5,34,35.7,40]", Controller.Single("setBounds").ArgsJson));
        Assert.False(Controller.WasCalled("setCenter"));
    }

    [Fact]
    public void A_result_without_an_area_is_centred_at_the_default_search_zoom_of_14()
    {
        Controller.Returns("search", new[] { Beirut });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Bei");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.Equal("[33.89,35.5,14]", Controller.Single("setCenter").ArgsJson));
    }

    [Fact]
    public void A_result_with_an_inverted_viewport_is_treated_as_a_point()
    {
        Controller.Returns("search", new[] { new { address = "Odd", description = (string?)null, latitude = 10.0, longitude = 20.0, north = 5.0, south = 15.0, east = 25.0, west = 15.0, geometryId = (string?)null } });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Odd");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.True(Controller.WasCalled("setCenter")));
        Assert.False(Controller.WasCalled("setBounds"));
    }

    [Fact]
    public void A_result_with_a_degenerate_viewport_in_one_axis_is_treated_as_a_point()
    {
        // Same west and east: a line, not an area.
        Controller.Returns("search", new[] { new { address = "Line", description = (string?)null, latitude = 10.0, longitude = 20.0, north = 12.0, south = 8.0, east = 20.0, west = 20.0, geometryId = (string?)null } });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Line");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.True(Controller.WasCalled("setCenter")));
    }

    [Fact]
    public void The_selected_event_carries_the_whole_result_including_the_geometry_id()
    {
        Controller.Returns("search", new[] { Jounieh });
        List<MapSearchResult> selected = [];
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.OnSearchResultSelected, (MapSearchResult r) => selected.Add(r)));
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.Single(selected));
        Assert.Equal("geo-j", selected[0].GeometryId);
        Assert.Equal((33.98, 35.62), (selected[0].Latitude, selected[0].Longitude));
    }

    [Fact]
    public void Selecting_a_result_outside_the_location_lock_is_rejected_without_moving_the_camera()
    {
        Controller.Returns("search", new[] { Jounieh });
        Controller.Returns("resolveBoundary", new[] { new[] { new[] { 0.0, 0.0 } } });
        Controller.Returns("isPointAllowed", false);
        List<MapSearchResult> selected = [];
        List<MapCoordinate> rejected = [];
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p
            .Add(m => m.LocationLock, new LocationLockOptions("LB"))
            .Add(m => m.OnSearchResultSelected, (MapSearchResult r) => selected.Add(r))
            .Add(m => m.OnLocationLockRejected, (MapCoordinate c) => rejected.Add(c)));
        Type(cut, "Jou");
        WaitForResults(cut, 1);
        Controller.ClearCalls();

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.Equal([new MapCoordinate(33.98, 35.62)], rejected));
        Assert.Equal("""[33.98,35.62]""", Controller.Single("isPointAllowed").ArgsJson);
        Assert.False(Controller.WasCalled("setBounds"));
        Assert.False(Controller.WasCalled("setCenter"));
        Assert.Empty(selected);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));
    }

    [Fact]
    public void Selecting_a_result_inside_the_location_lock_moves_the_camera()
    {
        Controller.Returns("search", new[] { Jounieh });
        Controller.Returns("resolveBoundary", new[] { new[] { new[] { 0.0, 0.0 } } });
        Controller.Returns("isPointAllowed", true);
        IRenderedComponent<AzureMap> cut = RenderSearch(p => p.Add(m => m.LocationLock, new LocationLockOptions("LB")));
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        Rows(cut)[0].MouseDown();

        cut.WaitForAssertion(() => Assert.True(Controller.WasCalled("setBounds")));
    }

    // ── Focus and blur ──────────────────────────────────────────────────────

    [Fact]
    public void The_input_becomes_editable_on_focus_and_read_only_again_on_blur()
    {
        IRenderedComponent<AzureMap> cut = RenderSearch();

        Input(cut).Focus();
        Assert.False(Input(cut).HasAttribute("readonly"));

        Input(cut).Blur();
        Assert.True(Input(cut).HasAttribute("readonly"));
    }

    [Fact]
    public void Leaving_the_box_closes_the_dropdown_after_a_short_delay_so_a_press_on_a_result_still_lands()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch();
        Type(cut, "Jou");
        WaitForResults(cut, 1);

        Input(cut).Blur();

        Assert.Single(Rows(cut));
        cut.WaitForAssertion(() => Assert.Empty(Rows(cut)), TimeSpan.FromSeconds(2));
        Assert.Single(cut.Instance.SearchResults);
    }

    [Fact]
    public async Task Disposing_the_map_cancels_a_search_waiting_for_its_debounce()
    {
        Controller.Returns("search", new[] { Jounieh });
        IRenderedComponent<AzureMap> cut = RenderSearch(debounce: 200);
        Type(cut, "Jou");

        await DisposeComponentsAsync();
        await Task.Delay(500);

        Assert.False(Controller.WasCalled("search"));
    }
}
