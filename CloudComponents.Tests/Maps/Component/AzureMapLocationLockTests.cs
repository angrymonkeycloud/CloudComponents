using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>Restricting the map to countries / subdivisions: what is resolved, what is sent to JavaScript, and the rejection feedback.</summary>
public sealed class AzureMapLocationLockTests : AzureMapTestContext
{
    private static double[][][] Square(double origin) =>
        [[[origin, origin], [origin + 1, origin], [origin + 1, origin + 1], [origin, origin]]];

    private void ResolveEverythingToASquare() => Controller.On("resolveBoundary", call => Square(call.Json(0).GetString()!.Length));

    private static LocationLockOptions Lock(params string[] countries) => new(countries);

    // ── Resolving the lock ──────────────────────────────────────────────────

    [Fact]
    public void No_lock_means_no_lock_call_and_nothing_resolved()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        Assert.Empty(Controller.Calls);
        Assert.False(cut.Instance.IsLocationLockActive);
        Assert.False(cut.Instance.IsResolvingLocationLock);
    }

    [Fact]
    public void A_whole_country_is_resolved_by_its_name_at_the_Country_level()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.Equal("""["Lebanon","Country",null]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public void The_resolved_polygons_and_the_boundary_options_are_sent_to_setLocationLock()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions(["LB"])
        {
            ShowBoundary = false,
            BoundaryFillColor = "#112233",
            BoundaryStrokeColor = "#445566",
            ZoomToBoundary = false
        }));

        JsCall call = Controller.Single("setLocationLock");
        Assert.Equal(2, call.Args.Length);
        Assert.Equal(1, call.Json(0).GetArrayLength());
        Assert.Equal(
            """{"showBoundary":false,"fillColor":"#112233","strokeColor":"#445566","zoomToBoundary":false}""",
            call.Json(1).GetRawText());
    }

    [Fact]
    public void The_default_boundary_options_are_sent_when_none_are_set()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.Equal(
            """{"showBoundary":true,"fillColor":"rgba(16, 124, 16, 0.10)","strokeColor":"#107c10","zoomToBoundary":true}""",
            Controller.Single("setLocationLock").Json(1).GetRawText());
    }

    [Fact]
    public void Two_countries_are_resolved_in_order_and_sent_together()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, Lock("LB", "FR")));

        Assert.Equal(["Lebanon", "France"], Controller.CallsTo("resolveBoundary").Select(c => c.Json(0).GetString()));
        Assert.Equal(2, Controller.Single("setLocationLock").Json(0).GetArrayLength());
    }

    [Fact]
    public void A_subdivision_is_resolved_by_name_and_country_at_the_CountrySubdivision_level()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions([new LocationLockArea("US", ["CA"])])));

        Assert.Equal("""["California, United States of America","CountrySubdivision","US"]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public void A_subdivision_the_geography_data_does_not_know_is_searched_by_its_code()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions([new LocationLockArea("LB", ["XX-NOWHERE"])])));

        Assert.Equal("""["XX-NOWHERE, Lebanon","CountrySubdivision","LB"]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact(Skip = "BUG: LocationLockArea documents subdivision codes as \"US-CA\" (XML doc, README and the demo use it) but only the bare code (\"CA\") resolves to a name, so \"US-CA\" is searched literally as \"US-CA, United States of America\"")]
    public void The_documented_US_CA_subdivision_code_is_searched_by_the_subdivision_name()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions([new LocationLockArea("US", ["US-CA"])])));

        Assert.Equal("California, United States of America", Controller.Single("resolveBoundary").Json(0).GetString());
    }

    [Fact]
    public void Several_subdivisions_are_resolved_one_after_another()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions([new LocationLockArea("US", ["CA", "NY"])])));

        Assert.Equal(["California, United States of America", "New York, United States of America"], Controller.CallsTo("resolveBoundary").Select(c => c.Json(0).GetString()));
    }

    [Fact]
    public void An_empty_subdivision_list_locks_the_whole_country()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions([new LocationLockArea("LB", [])])));

        Assert.Equal("""["Lebanon","Country",null]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public void A_country_code_the_geography_data_does_not_know_is_skipped()
    {
        ResolveEverythingToASquare();

        RenderMap(p => p.Add(m => m.LocationLock, Lock("ZZ", "LB")));

        Assert.Equal(["Lebanon"], Controller.CallsTo("resolveBoundary").Select(c => c.Json(0).GetString()));
    }

    [Fact]
    public void An_area_whose_boundary_cannot_be_found_is_left_out_of_the_lock()
    {
        Controller.On("resolveBoundary", call => call.Json(0).GetString() == "Lebanon" ? null : Square(1));

        RenderMap(p => p.Add(m => m.LocationLock, Lock("LB", "FR")));

        Assert.Equal(1, Controller.Single("setLocationLock").Json(0).GetArrayLength());
    }

    [Fact]
    public void The_lock_is_active_once_a_boundary_has_resolved()
    {
        ResolveEverythingToASquare();

        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.True(cut.Instance.IsLocationLockActive);
        Assert.False(cut.Instance.IsResolvingLocationLock);
    }

    [Fact]
    public void The_lock_is_not_active_when_no_boundary_resolved()
    {
        Controller.Returns("resolveBoundary", null);

        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.False(cut.Instance.IsLocationLockActive);
        Assert.Equal("[]", Controller.Single("setLocationLock").Json(0).GetRawText());
    }

    [Fact]
    public void The_map_reports_that_it_is_resolving_until_the_boundary_arrives()
    {
        JsGate gate = Controller.Hold("resolveBoundary");

        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.True(cut.Instance.IsResolvingLocationLock);
        Assert.False(cut.Instance.IsLocationLockActive);
        Assert.False(Controller.WasCalled("setLocationLock"));

        gate.Release(Square(1));

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsResolvingLocationLock));
        Assert.True(cut.Instance.IsLocationLockActive);
        Assert.True(Controller.WasCalled("setLocationLock"));
    }

    [Fact]
    public void A_lock_with_no_areas_clears_the_lock_instead_of_resolving_anything()
    {
        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions(Array.Empty<LocationLockArea>())));

        Assert.Equal(["clearLocationLock"], Controller.Methods);
    }

    [Fact]
    public void A_resolution_failure_raises_OnMapError_and_leaves_the_map_unlocked()
    {
        Controller.Throws("resolveBoundary", new JSException("quota exceeded"));
        List<string> errors = [];

        IRenderedComponent<AzureMap> cut = RenderMap(p => p
            .Add(m => m.LocationLock, Lock("LB"))
            .Add(m => m.OnMapError, (string e) => errors.Add(e)));

        Assert.Equal(["Failed to resolve location lock: quota exceeded"], errors);
        Assert.False(cut.Instance.IsResolvingLocationLock);
        Assert.False(cut.Instance.IsLocationLockActive);
        Assert.False(Controller.WasCalled("setLocationLock"));
    }

    // ── Changing the lock ───────────────────────────────────────────────────

    [Fact]
    public void Assigning_a_lock_after_creation_resolves_it()
    {
        ResolveEverythingToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.True(Controller.WasCalled("setLocationLock"));
        Assert.True(cut.Instance.IsLocationLockActive);
    }

    [Fact]
    public void Re_rendering_with_the_same_lock_instance_does_not_resolve_it_again()
    {
        ResolveEverythingToASquare();
        LocationLockOptions options = Lock("LB");
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, options));

        cut.Render(p => p.Add(m => m.LocationLock, options));
        cut.Render(p => p.Add(m => m.Height, "10px"));

        Assert.Single(Controller.CallsTo("resolveBoundary"));
        Assert.Single(Controller.CallsTo("setLocationLock"));
    }

    [Fact]
    public void A_new_lock_instance_is_resolved_again_even_if_it_has_the_same_content()
    {
        ResolveEverythingToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        cut.Render(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.Equal(2, Controller.CallsTo("setLocationLock").Count);
    }

    [Fact]
    public void Removing_the_lock_clears_it_and_deactivates_it()
    {
        ResolveEverythingToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        cut.Render(p => p.Add(m => m.LocationLock, (LocationLockOptions?)null));

        Assert.True(Controller.WasCalled("clearLocationLock"));
        Assert.False(cut.Instance.IsLocationLockActive);
    }

    [Fact]
    public void A_lock_replaced_while_still_resolving_never_applies_the_old_boundaries()
    {
        JsGate gate = Controller.Hold("resolveBoundary");
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Controller.Returns("resolveBoundary", null);
        cut.Render(p => p.Add(m => m.LocationLock, new LocationLockOptions(Array.Empty<LocationLockArea>())));
        gate.Release(Square(1));

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsResolvingLocationLock));
        Assert.False(Controller.WasCalled("setLocationLock"));
        Assert.False(cut.Instance.IsLocationLockActive);
    }

    [Fact(Skip = "BUG: AzureMap.LocationLock.cs SyncLocationLockAsync (finally block, line ~93): a superseded resolution that finishes late sets IsResolvingLocationLock = false while the newer resolution is still running")]
    public void A_superseded_resolution_finishing_late_does_not_end_the_newer_resolving_state()
    {
        List<TaskCompletionSource<object?>> pending = [];
        Controller.OnAsync("resolveBoundary", _ =>
        {
            TaskCompletionSource<object?> source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Add(source);
            return source.Task;
        });
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));
        cut.Render(p => p.Add(m => m.LocationLock, Lock("FR")));
        Assert.Equal(2, pending.Count);

        pending[0].SetResult(Square(1));
        Thread.Sleep(150);

        Assert.True(cut.Instance.IsResolvingLocationLock);
        pending[1].SetResult(Square(2));
        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsResolvingLocationLock));
        Assert.Single(Controller.CallsTo("setLocationLock"));
    }

    // ── Point checks ────────────────────────────────────────────────────────

    [Fact]
    public async Task Without_a_lock_every_point_is_allowed_without_asking_JavaScript()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        bool allowed = await cut.InvokeAsync(() => cut.Instance.IsPointAllowedByLocationLockAsync(0, 0));

        Assert.True(allowed);
        Assert.False(Controller.WasCalled("isPointAllowed"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task With_an_active_lock_JavaScript_decides(bool answer)
    {
        ResolveEverythingToASquare();
        Controller.Returns("isPointAllowed", answer);
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        bool allowed = await cut.InvokeAsync(() => cut.Instance.IsPointAllowedByLocationLockAsync(33.5, 35.5));

        Assert.Equal(answer, allowed);
        Assert.Equal("[33.5,35.5]", Controller.Single("isPointAllowed").ArgsJson);
    }

    [Fact]
    public async Task A_lock_that_resolved_nothing_allows_every_point()
    {
        Controller.Returns("resolveBoundary", null);
        Controller.Returns("isPointAllowed", false);
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, Lock("LB")));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.IsPointAllowedByLocationLockAsync(0, 0)));
    }

    // ── Rejection feedback ──────────────────────────────────────────────────
    // The JavaScript call returns only after the toast has gone (the component awaits its 2.6 s timer), so these tests start
    // the call without awaiting it and wait for what they check.

    private Task RejectFromJs(double latitude, double longitude) => Js.InvokeFromJsAsync("NotifyLocationLockRejectedAsync", latitude, longitude);

    [Fact]
    public void A_rejection_from_JavaScript_raises_OnLocationLockRejected_with_the_coordinate()
    {
        List<MapCoordinate> rejected = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnLocationLockRejected, (MapCoordinate c) => rejected.Add(c)));

        _ = RejectFromJs(40.5, -3.5);

        cut.WaitForAssertion(() => Assert.Equal([new MapCoordinate(40.5, -3.5)], rejected));
    }

    [Fact]
    public void A_rejection_shows_the_default_message_in_an_alert_toast()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        _ = RejectFromJs(1, 2);

        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement toast = cut.Find(".map-lock-toast");
            Assert.Equal("This location is outside the allowed area.", toast.TextContent);
            Assert.Equal("alert", toast.GetAttribute("role"));
        });
    }

    [Fact]
    public void A_rejection_shows_the_RestrictionMessage_of_the_lock()
    {
        ResolveEverythingToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions(["LB"]) { RestrictionMessage = "Lebanon only, please." }));

        _ = RejectFromJs(1, 2);

        cut.WaitForAssertion(() => Assert.Equal("Lebanon only, please.", cut.Find(".map-lock-toast").TextContent));
    }

    [Fact]
    public void The_toast_hides_itself_after_a_few_seconds()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        _ = RejectFromJs(1, 2);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".map-lock-toast")), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public async Task The_toast_is_still_shown_well_before_its_timeout()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        _ = RejectFromJs(1, 2);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));

        await Task.Delay(400);

        Assert.Single(cut.FindAll(".map-lock-toast"));
    }

    [Fact]
    public void Every_rejection_raises_the_handler_and_the_toast_stays_a_single_element()
    {
        int handled = 0;
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnLocationLockRejected, (MapCoordinate _) => handled++));

        _ = RejectFromJs(1, 2);
        _ = RejectFromJs(3, 4);

        cut.WaitForAssertion(() => Assert.Equal(2, handled));
        Assert.Single(cut.FindAll(".map-lock-toast"));
    }

    [Fact]
    public void A_second_rejection_restarts_the_toast_timer_instead_of_the_first_one_hiding_it()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        _ = RejectFromJs(1, 2);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));
        Thread.Sleep(1500);

        _ = RejectFromJs(3, 4);
        Thread.Sleep(1500);

        // 3.0 s after the first rejection, 1.5 s after the second: the first timer (2.6 s) would have hidden it.
        Assert.Single(cut.FindAll(".map-lock-toast"));
    }

    [Fact]
    public async Task Disposing_the_map_while_the_toast_is_showing_stops_its_timer_quietly()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        Task call = RejectFromJs(1, 2);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));

        await DisposeComponentsAsync();

        await call.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(call.IsCompletedSuccessfully);
    }
}
