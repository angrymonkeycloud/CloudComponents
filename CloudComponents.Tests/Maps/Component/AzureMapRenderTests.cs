using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using AngryMonkey.CloudComponents.Maps.Options;
using AngryMonkey.CloudComponents.Maps.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CloudComponents.Tests.Maps.Component;

public sealed class AzureMapRenderTests : AzureMapTestContext
{
    [Fact]
    public void The_host_div_takes_the_default_height_and_width()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        Assert.Equal("height:500px; width:100%;", cut.Find("div.map-host").GetAttribute("style"));
    }

    [Fact]
    public void The_host_div_takes_the_height_and_width_parameters()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Height, "20rem").Add(m => m.Width, "640px"));

        Assert.Equal("height:20rem; width:640px;", cut.Find("div.map-host").GetAttribute("style"));
    }

    [Fact]
    public void The_map_canvas_id_is_the_component_MapId_and_is_what_createMap_is_told()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        string id = cut.Find("div.map-canvas").Id!;

        Assert.Equal(cut.Instance.MapId, id);
        Assert.Equal(id, Js.CreateMapOptions.GetProperty("elementId").GetString());
    }

    [Fact]
    public void The_map_id_has_the_azmap_prefix_and_differs_between_instances()
    {
        IRenderedComponent<AzureMap> first = RenderMap();
        IRenderedComponent<AzureMap> second = Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k"));

        Assert.StartsWith("azmap-", first.Instance.MapId);
        Assert.NotEqual(first.Instance.MapId, second.Instance.MapId);
    }

    [Fact]
    public void The_map_id_stays_the_same_across_renders()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        string before = cut.Instance.MapId;

        cut.Render(p => p.Add(m => m.Height, "300px"));

        Assert.Equal(before, cut.Instance.MapId);
        Assert.Equal(before, cut.Find("div.map-canvas").Id);
    }

    [Fact]
    public void Nothing_optional_is_drawn_by_default_except_the_style_switch()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        Assert.Empty(cut.FindAll(".map-search"));
        Assert.Empty(cut.FindAll(".map-pin"));
        Assert.Empty(cut.FindAll(".map-legend"));
        Assert.Empty(cut.FindAll(".map-lock-toast"));
        Assert.Single(cut.FindAll(".map-style-switch"));
    }

    [Fact]
    public void The_search_box_renders_only_when_ShowSearchBox_is_set()
    {
        Assert.Empty(RenderMap().FindAll("input.map-search-input"));

        IRenderedComponent<AzureMap> cut = Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.ShowSearchBox, true));

        Assert.Single(cut.FindAll("input.map-search-input"));
    }

    [Fact]
    public void The_search_input_has_the_placeholder_parameter_and_an_accessible_name()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.ShowSearchBox, true).Add(m => m.SearchPlaceholder, "Find a street"));

        AngleSharp.Dom.IElement input = cut.Find("input.map-search-input");

        Assert.Equal("Find a street", input.GetAttribute("placeholder"));
        Assert.Equal("Search for a place", input.GetAttribute("aria-label"));
        Assert.Equal($"{cut.Instance.MapId}-search", input.GetAttribute("name"));
    }

    [Fact]
    public void The_search_input_starts_read_only_so_a_mobile_keyboard_does_not_open_on_load()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.ShowSearchBox, true));

        Assert.True(cut.Find("input.map-search-input").HasAttribute("readonly"));
    }

    [Fact]
    public void The_style_switch_is_removed_when_ShowStyleToggle_is_false()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.ShowStyleToggle, false));

        Assert.Empty(cut.FindAll(".map-style-switch"));
    }

    [Fact]
    public void The_style_switch_moves_below_the_search_box_when_the_search_box_is_shown()
    {
        Assert.DoesNotContain("_below-search", RenderMap().Find(".map-style-switch").ClassName);

        IRenderedComponent<AzureMap> withSearch = Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.ShowSearchBox, true));

        Assert.Contains("_below-search", withSearch.Find(".map-style-switch").ClassName);
    }

    [Fact]
    public void The_centre_pin_renders_only_for_the_CenterPin_trigger()
    {
        foreach (MarkerAddTrigger trigger in new[] { MarkerAddTrigger.Disabled, MarkerAddTrigger.SingleClick, MarkerAddTrigger.DoubleClick })
            Assert.Empty(Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.AddMarkerTrigger, trigger)).FindAll(".map-pin"));

        IRenderedComponent<AzureMap> cut = Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.AddMarkerTrigger, MarkerAddTrigger.CenterPin));

        Assert.Single(cut.FindAll(".map-pin"));
    }

    [Fact(Skip = "BUG: AzureMap.Locator is [Inject] ILocationService? documented as optional, but rendering without a registered ILocationService throws InvalidOperationException (Cannot provide a value for property Locator)")]
    public void The_map_renders_without_a_location_service_and_then_has_no_locate_button()
    {
        Services.RemoveAll<ILocationService>();

        IRenderedComponent<AzureMap> cut = RenderMap();

        Assert.Empty(cut.FindAll("button[aria-label='Show my location']"));
    }

    [Fact]
    public void The_locate_button_renders_when_a_location_service_is_registered()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.InitialView, new InitialMapView(1, 2)));

        AngleSharp.Dom.IElement button = cut.Find("button[aria-label='Show my location']");
        Assert.Equal("Show my location", button.GetAttribute("title"));
        Assert.False(button.HasAttribute("disabled"));
    }

    [Fact]
    public void The_locate_button_is_hidden_when_ShowCurrentLocationButton_is_false()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p
            .Add(m => m.InitialView, new InitialMapView(1, 2))
            .Add(m => m.ShowCurrentLocationButton, false));

        Assert.Empty(cut.FindAll("button[aria-label='Show my location']"));
    }

    [Fact]
    public async Task The_legend_is_drawn_for_labelled_regions_and_hidden_by_ShowLegend_false()
    {
        IRenderedComponent<AzureMap> shown = RenderMap();
        await shown.InvokeAsync(() => shown.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]], "red", "blue") { Label = "Zone A" }]));

        AngleSharp.Dom.IElement item = shown.Find(".map-legend-item");
        Assert.Equal("Zone A", item.QuerySelector(".map-legend-label")!.TextContent);
        Assert.Contains("background: red", item.QuerySelector(".map-legend-swatch")!.GetAttribute("style"));
        Assert.Contains("border-color: blue", item.QuerySelector(".map-legend-swatch")!.GetAttribute("style"));

        IRenderedComponent<AzureMap> hidden = Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.ShowLegend, false));
        await hidden.InvokeAsync(() => hidden.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]]) { Label = "Zone A" }]));

        Assert.Empty(hidden.FindAll(".map-legend"));
    }

    [Fact]
    public async Task A_region_without_a_label_adds_nothing_to_the_legend()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]]), new MapRegion([[[0, 0]]]) { Label = "  " }]));

        Assert.Empty(cut.FindAll(".map-legend"));
    }

    [Fact]
    public async Task Two_regions_with_the_same_label_share_one_legend_row()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync(
        [
            new MapRegion([[[0, 0]]]) { Label = "Same" },
            new MapRegion([[[1, 1]]]) { Label = "Same" }
        ]));

        Assert.Single(cut.FindAll(".map-legend-item"));
    }

    [Fact]
    public void The_permission_popup_is_closed_until_consent_is_needed()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.Empty(cut.FindAll(".map-permission-message"));
    }
}
