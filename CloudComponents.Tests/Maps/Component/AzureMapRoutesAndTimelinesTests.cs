using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>Routes and timelines: what is sent to JavaScript, when the parameters are synchronised, and the point-click event.</summary>
public sealed class AzureMapRoutesAndTimelinesTests : AzureMapTestContext
{
    private static MapRoute Route(string id, params (double Lat, double Lng)[] points) => new()
    {
        Id = id,
        Color = "#ff0000",
        Points = [.. points.Select(p => new MapCoordinate(p.Lat, p.Lng))]
    };

    private static MapTimeline Timeline(string id, int points = 3) => new()
    {
        Id = id,
        Name = "Courier " + id,
        Color = "#00aa00",
        ShowLine = false,
        Points =
        [
            .. Enumerable.Range(0, points).Select(i => new MapTimelinePoint(30 + i, 40 + i, new DateTime(2026, 5, 1, 8, i, 0, DateTimeKind.Utc))
            {
                Label = i == 0 ? "Depot" : null,
                Description = i == 0 ? "Start" : null
            })
        ]
    };

    // ── Routes ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetRoutesAsync_sends_the_routes_with_id_colour_and_points_as_latitude_longitude_objects()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.SetRoutesAsync([Route("r1", (1, 2), (3, 4))]));

        Assert.Equal(
            """[[{"id":"r1","color":"#ff0000","points":[{"latitude":1,"longitude":2},{"latitude":3,"longitude":4}]}]]""",
            Controller.Single("setRoutes").ArgsJson);
    }

    [Fact]
    public async Task SetRoutesAsync_sends_the_default_blue_for_a_route_without_a_colour()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.SetRoutesAsync([new MapRoute { Id = "r" }]));

        Assert.Equal("#2563eb", Controller.Single("setRoutes").Json(0)[0].GetProperty("color").GetString());
    }

    [Fact]
    public async Task SetRoutesAsync_with_nothing_sends_an_empty_list()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.SetRoutesAsync([]));

        Assert.Equal("[[]]", Controller.Single("setRoutes").ArgsJson);
    }

    [Fact]
    public async Task ClearRoutesAsync_clears_the_routes()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.ClearRoutesAsync());

        Assert.Equal("[]", Controller.Single("clearRoutes").ArgsJson);
    }

    [Fact]
    public void Routes_given_at_creation_wait_for_the_ready_event()
    {
        RenderMap(p => p.Add(m => m.Routes, new[] { Route("r1", (1, 2)) }));

        Assert.False(Controller.WasCalled("setRoutes"));
    }

    [Fact]
    public async Task Routes_given_at_creation_are_sent_when_the_map_becomes_ready()
    {
        IReadOnlyList<MapRoute> routes = [Route("r1", (1, 2))];
        RenderMap(p => p.Add(m => m.Routes, routes));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Equal("r1", Controller.Single("setRoutes").Json(0)[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task No_routes_at_ready_means_no_setRoutes_call()
    {
        RenderMap();

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.False(Controller.WasCalled("setRoutes"));
    }

    [Fact]
    public async Task A_new_routes_list_after_ready_is_sent_but_the_same_list_is_not_sent_again()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync();
        IReadOnlyList<MapRoute> routes = [Route("r1", (1, 2))];

        cut.Render(p => p.Add(m => m.Routes, routes));
        cut.Render(p => p.Add(m => m.Routes, routes));

        Assert.Single(Controller.CallsTo("setRoutes"));
    }

    [Fact]
    public async Task A_replaced_routes_list_is_sent_again()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync();

        cut.Render(p => p.Add(m => m.Routes, new[] { Route("a", (1, 2)) }));
        cut.Render(p => p.Add(m => m.Routes, new[] { Route("b", (3, 4)) }));

        Assert.Equal(["a", "b"], Controller.CallsTo("setRoutes").Select(c => c.Json(0)[0].GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Setting_the_routes_to_null_after_ready_clears_them_by_sending_an_empty_list()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync(p => p.Add(m => m.Routes, new[] { Route("a", (1, 2)) }));
        Controller.ClearCalls();

        cut.Render(p => p.Add(m => m.Routes, (IReadOnlyList<MapRoute>?)null));

        Assert.Equal("[[]]", Controller.Single("setRoutes").ArgsJson);
    }

    [Fact]
    public void Routes_changed_before_the_map_is_ready_are_held_back()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.Routes, new[] { Route("a", (1, 2)) }));

        Assert.False(Controller.WasCalled("setRoutes"));
    }

    [Fact]
    public async Task Routes_changed_before_ready_are_sent_once_the_map_is_ready()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        cut.Render(p => p.Add(m => m.Routes, new[] { Route("a", (1, 2)) }));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Single(Controller.CallsTo("setRoutes"));
    }

    // ── Timelines: imperative ───────────────────────────────────────────────

    [Fact]
    public async Task SetTimelinesAsync_sends_every_timeline_field_and_the_fit_flag()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("t1", 2)]));

        JsCall call = Controller.Single("setTimelines");
        Assert.Equal(
            """[{"id":"t1","name":"Courier t1","color":"#00aa00","showLine":false,"points":[{"latitude":30,"longitude":40,"timestamp":"2026-05-01T08:00:00Z","label":"Depot","description":"Start"},{"latitude":31,"longitude":41,"timestamp":"2026-05-01T08:01:00Z","label":null,"description":null}]}]""",
            call.Json(0).GetRawText());
        Assert.True(call.Json(1).GetBoolean());
    }

    [Fact]
    public async Task SetTimelinesAsync_fitToBounds_overrides_the_FitToTimelines_parameter()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.FitToTimelines, true));

        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("t1")], fitToBounds: false));

        Assert.False(Controller.Single("setTimelines").Json(1).GetBoolean());
    }

    [Fact]
    public async Task SetTimelinesAsync_follows_FitToTimelines_when_not_overridden()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.FitToTimelines, false));

        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("t1")]));

        Assert.False(Controller.Single("setTimelines").Json(1).GetBoolean());
    }

    [Fact]
    public async Task ClearTimelinesAsync_clears_the_map_and_forgets_the_timelines()
    {
        List<MapTimelinePointClickEventArgs> clicks = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnTimelinePointClick, (MapTimelinePointClickEventArgs e) => clicks.Add(e)));
        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("t1")]));

        await cut.InvokeAsync(() => cut.Instance.ClearTimelinesAsync());
        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", "t1", 0);

        Assert.Equal("[]", Controller.Single("clearTimelines").ArgsJson);
        Assert.Empty(clicks);
    }

    // ── Timelines: the parameter ────────────────────────────────────────────

    [Fact]
    public async Task Timelines_given_at_creation_are_sent_when_the_map_becomes_ready_with_the_fit_flag()
    {
        RenderMap(p => p.Add(m => m.Timelines, new[] { Timeline("t1") }));
        Assert.False(Controller.WasCalled("setTimelines"));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        JsCall call = Controller.Single("setTimelines");
        Assert.Equal("t1", call.Json(0)[0].GetProperty("id").GetString());
        Assert.True(call.Json(1).GetBoolean());
    }

    [Fact]
    public async Task No_timelines_at_ready_means_no_setTimelines_call()
    {
        RenderMap();

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.False(Controller.WasCalled("setTimelines"));
    }

    [Fact]
    public async Task A_new_timelines_list_after_ready_is_sent_once_and_the_same_list_is_not_sent_again()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync();
        IReadOnlyList<MapTimeline> timelines = [Timeline("t1")];

        cut.Render(p => p.Add(m => m.Timelines, timelines));
        cut.Render(p => p.Add(m => m.Timelines, timelines));

        Assert.Single(Controller.CallsTo("setTimelines"));
    }

    [Fact]
    public async Task Setting_the_timelines_to_null_after_they_were_shown_sends_an_empty_list()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync(p => p.Add(m => m.Timelines, new[] { Timeline("t1") }));
        Controller.ClearCalls();

        cut.Render(p => p.Add(m => m.Timelines, (IReadOnlyList<MapTimeline>?)null));

        Assert.Equal(JsonValueKind.Array, Controller.Single("setTimelines").Json(0).ValueKind);
        Assert.Equal(0, Controller.Single("setTimelines").Json(0).GetArrayLength());
    }

    [Fact]
    public async Task The_fit_flag_sent_with_the_parameter_is_FitToTimelines()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync(p => p.Add(m => m.FitToTimelines, false));

        cut.Render(p => p.Add(m => m.FitToTimelines, false).Add(m => m.Timelines, new[] { Timeline("t1") }));

        Assert.False(Controller.Single("setTimelines").Json(1).GetBoolean());
    }

    [Fact]
    public void Timelines_changed_before_the_map_is_ready_are_held_back()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.Timelines, new[] { Timeline("t1") }));

        Assert.False(Controller.WasCalled("setTimelines"));
    }

    // ── Timeline point click ────────────────────────────────────────────────

    [Fact]
    public async Task A_point_click_raises_OnTimelinePointClick_with_the_timeline_point_and_index()
    {
        List<MapTimelinePointClickEventArgs> clicks = [];
        MapTimeline timeline = Timeline("t1");
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnTimelinePointClick, (MapTimelinePointClickEventArgs e) => clicks.Add(e)));
        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([timeline]));

        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", "t1", 2);

        MapTimelinePointClickEventArgs click = Assert.Single(clicks);
        Assert.Same(timeline, click.Timeline);
        Assert.Same(timeline.Points[2], click.Point);
        Assert.Equal(2, click.PointIndex);
    }

    [Fact]
    public async Task A_point_click_works_for_a_timeline_that_came_from_the_parameter()
    {
        List<MapTimelinePointClickEventArgs> clicks = [];
        MapTimeline timeline = Timeline("t1");
        RenderMap(p => p
            .Add(m => m.Timelines, new[] { timeline })
            .Add(m => m.OnTimelinePointClick, (MapTimelinePointClickEventArgs e) => clicks.Add(e)));
        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", "t1", 0);

        Assert.Equal("Depot", Assert.Single(clicks).Point.Label);
    }

    [Fact]
    public async Task A_click_picks_the_right_timeline_among_several()
    {
        List<MapTimelinePointClickEventArgs> clicks = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnTimelinePointClick, (MapTimelinePointClickEventArgs e) => clicks.Add(e)));
        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("a"), Timeline("b")]));

        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", "b", 1);

        Assert.Equal("b", Assert.Single(clicks).Timeline.Id);
    }

    [Theory]
    [InlineData("ghost", 0)]
    [InlineData("t1", -1)]
    [InlineData("t1", 3)]
    [InlineData("t1", 99)]
    public async Task A_click_on_an_unknown_timeline_or_an_index_out_of_range_is_ignored(string id, int index)
    {
        List<MapTimelinePointClickEventArgs> clicks = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnTimelinePointClick, (MapTimelinePointClickEventArgs e) => clicks.Add(e)));
        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("t1", 3)]));

        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", id, index);

        Assert.Empty(clicks);
    }

    [Fact]
    public async Task Replacing_the_timelines_forgets_the_old_ones_for_clicks()
    {
        List<MapTimelinePointClickEventArgs> clicks = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnTimelinePointClick, (MapTimelinePointClickEventArgs e) => clicks.Add(e)));
        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("old")]));
        await cut.InvokeAsync(() => cut.Instance.SetTimelinesAsync([Timeline("new")]));

        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", "old", 0);
        await Js.InvokeFromJsAsync("NotifyTimelinePointClickAsync", "new", 0);

        Assert.Equal("new", Assert.Single(clicks).Timeline.Id);
    }
}
