using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Options;
using AngleSharp.Dom;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>The Road / Satellite switch drawn on the map.</summary>
public sealed class AzureMapStyleToggleTests : AzureMapTestContext
{
    private static IElement Road(IRenderedComponent<AzureMap> cut) => cut.FindAll(".map-style-switch button")[0];

    private static IElement Satellite(IRenderedComponent<AzureMap> cut) => cut.FindAll(".map-style-switch button")[1];

    [Fact]
    public void The_switch_is_a_labelled_group_with_Road_then_Satellite()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        IElement group = cut.Find(".map-style-switch");
        Assert.Equal("group", group.GetAttribute("role"));
        Assert.Equal("Map style", group.GetAttribute("aria-label"));
        Assert.Equal(["Road", "Satellite"], cut.FindAll(".map-style-switch button").Select(b => b.TextContent.Trim()));
    }

    [Theory]
    [InlineData(MapStyle.Satellite, false)]
    [InlineData(MapStyle.SatelliteRoadLabels, false)]
    [InlineData(MapStyle.Road, true)]
    [InlineData(MapStyle.Night, true)]
    [InlineData(MapStyle.GrayscaleDark, true)]
    [InlineData(MapStyle.HighContrastLight, true)]
    public void Only_the_two_satellite_styles_light_up_the_Satellite_button(MapStyle style, bool roadActive)
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, style));

        Assert.Equal(roadActive ? "true" : "false", Road(cut).GetAttribute("aria-pressed"));
        Assert.Equal(roadActive ? "false" : "true", Satellite(cut).GetAttribute("aria-pressed"));
        Assert.Equal(roadActive, Road(cut).ClassList.Contains("_active"));
        Assert.Equal(!roadActive, Satellite(cut).ClassList.Contains("_active"));
    }

    [Fact]
    public void Clicking_Road_while_on_satellite_switches_the_map_to_road_and_moves_the_active_button()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        Road(cut).Click();

        Assert.Equal("""["road"]""", Controller.Single("setStyle").ArgsJson);
        Assert.Equal("true", Road(cut).GetAttribute("aria-pressed"));
        Assert.Equal("false", Satellite(cut).GetAttribute("aria-pressed"));
        Assert.True(Road(cut).ClassList.Contains("_active"));
        Assert.False(Satellite(cut).ClassList.Contains("_active"));
    }

    [Fact]
    public void Clicking_Satellite_while_on_road_switches_to_satellite_with_road_labels()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Road));

        Satellite(cut).Click();

        Assert.Equal("""["satellite_road_labels"]""", Controller.Single("setStyle").ArgsJson);
        Assert.Equal("true", Satellite(cut).GetAttribute("aria-pressed"));
        Assert.Equal("false", Road(cut).GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Clicking_Satellite_when_the_map_is_already_a_satellite_style_does_nothing()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        Satellite(cut).Click();

        Assert.False(Controller.WasCalled("setStyle"));
    }

    [Fact]
    public void Clicking_Satellite_on_satellite_with_road_labels_does_nothing()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.SatelliteRoadLabels));

        Satellite(cut).Click();

        Assert.False(Controller.WasCalled("setStyle"));
    }

    [Fact]
    public void Clicking_Road_when_the_map_is_already_road_does_nothing()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Road));

        Road(cut).Click();

        Assert.False(Controller.WasCalled("setStyle"));
    }

    [Fact]
    public void Clicking_Road_while_on_a_non_satellite_non_road_style_switches_to_road()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Night));

        Road(cut).Click();

        Assert.Equal("""["road"]""", Controller.Single("setStyle").ArgsJson);
    }

    [Fact]
    public void Switching_back_and_forth_sends_each_change_in_order()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        Road(cut).Click();
        Satellite(cut).Click();
        Road(cut).Click();

        Assert.Equal(["""["road"]""", """["satellite_road_labels"]""", """["road"]"""], Controller.CallsTo("setStyle").Select(c => c.ArgsJson));
    }

    [Fact]
    public async Task A_style_change_reported_by_JavaScript_updates_the_switch_without_calling_back()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        await Js.InvokeFromJsAsync("NotifyStyleChangedAsync", "road");

        Assert.Equal("true", Road(cut).GetAttribute("aria-pressed"));
        Assert.False(Controller.WasCalled("setStyle"));
    }

    [Theory]
    [InlineData("satellite", false)]
    [InlineData("satellite_road_labels", false)]
    [InlineData("road", true)]
    [InlineData("night", true)]
    [InlineData("grayscale_light", true)]
    [InlineData("grayscale_dark", true)]
    [InlineData("road_shaded_relief", true)]
    [InlineData("high_contrast_dark", true)]
    [InlineData("high_contrast_light", true)]
    public async Task Every_style_identifier_JavaScript_reports_maps_to_the_right_side_of_the_switch(string reported, bool roadActive)
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, roadActive ? MapStyle.Satellite : MapStyle.Road));

        await Js.InvokeFromJsAsync("NotifyStyleChangedAsync", reported);

        Assert.Equal(roadActive, Road(cut).ClassList.Contains("_active"));
    }

    [Fact]
    public async Task An_unknown_style_identifier_from_JavaScript_is_treated_as_road()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        await Js.InvokeFromJsAsync("NotifyStyleChangedAsync", "no-such-style");

        Assert.True(Road(cut).ClassList.Contains("_active"));
    }

    [Fact]
    public async Task A_style_the_user_picked_in_the_map_then_clicked_on_the_switch_sends_the_new_one()
    {
        // The in-map style control moved the map to night; clicking Satellite must restyle, not be mistaken for "already there".
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));
        await Js.InvokeFromJsAsync("NotifyStyleChangedAsync", "night");

        Satellite(cut).Click();

        Assert.Equal("""["satellite_road_labels"]""", Controller.Single("setStyle").ArgsJson);
    }

    [Fact]
    public async Task SetStyleAsync_restyles_the_map_and_the_switch_follows()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        await cut.InvokeAsync(() => cut.Instance.SetStyleAsync(MapStyle.Road));

        Assert.Equal("""["road"]""", Controller.Single("setStyle").ArgsJson);
        Assert.True(Road(cut).ClassList.Contains("_active"));
    }

    [Fact]
    public async Task SetStyleAsync_does_not_fight_with_a_later_re_render_of_the_same_host_value()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));
        await cut.InvokeAsync(() => cut.Instance.SetStyleAsync(MapStyle.Road));
        Controller.ClearCalls();

        cut.Render(p => p.Add(m => m.Style, MapStyle.Satellite));

        Assert.False(Controller.WasCalled("setStyle"));
        Assert.True(Road(cut).ClassList.Contains("_active"));
    }
}
