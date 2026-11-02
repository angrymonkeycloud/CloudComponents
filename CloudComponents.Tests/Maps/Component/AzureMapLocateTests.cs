using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using AngryMonkey.CloudComponents.Maps.Services;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>The "locate me" button, the automatic locate at start-up and the consent popup in front of the browser's own prompt.</summary>
public sealed class AzureMapLocateTests : AzureMapTestContext
{
    private readonly List<MapCoordinate> _found = [];

    private IRenderedComponent<AzureMap> RenderLocate(Action<ComponentParameterCollectionBuilder<AzureMap>>? configure = null, bool withInitialView = true) =>
        RenderMap(p =>
        {
            if (withInitialView)
                p.Add(m => m.InitialView, new InitialMapView(10, 20));

            p.Add(m => m.OnMyLocationFound, (MapCoordinate c) => _found.Add(c));
            configure?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement LocateButton(IRenderedComponent<AzureMap> cut) => cut.Find("button[aria-label='Show my location']");

    // ── The locate button ───────────────────────────────────────────────────

    [Fact]
    public void Clicking_the_button_centres_the_map_on_the_location_and_reports_it()
    {
        Locator.Location = (33.9, 35.5);
        IRenderedComponent<AzureMap> cut = RenderLocate();

        LocateButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal([new MapCoordinate(33.9, 35.5)], _found));
        Assert.Equal("[33.9,35.5]", Controller.Single("showCurrentLocation").ArgsJson);
    }

    [Fact]
    public void The_location_is_latitude_first_in_both_the_call_and_the_event()
    {
        Locator.Location = (-12.5, 77.25);
        IRenderedComponent<AzureMap> cut = RenderLocate();

        LocateButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Single(_found));
        Assert.Equal(-12.5, _found[0].Latitude);
        Assert.Equal(77.25, _found[0].Longitude);
        Assert.Equal("[-12.5,77.25]", Controller.Single("showCurrentLocation").ArgsJson);
    }

    [Fact]
    public void An_unavailable_location_changes_nothing()
    {
        Locator.Location = null;
        IRenderedComponent<AzureMap> cut = RenderLocate();

        LocateButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, Locator.LocationRequests));
        Assert.Empty(_found);
        Assert.False(Controller.WasCalled("showCurrentLocation"));
        Assert.False(LocateButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task The_button_is_disabled_and_marked_loading_while_the_location_is_being_fetched()
    {
        Locator.LocationGate = new TaskCompletionSource<(double Lat, double Lng)?>();
        IRenderedComponent<AzureMap> cut = RenderLocate();

        Task<bool> click = cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        cut.WaitForAssertion(() =>
        {
            Assert.True(LocateButton(cut).HasAttribute("disabled"));
            Assert.Contains("_loading", LocateButton(cut).ClassName);
        });

        Locator.LocationGate.SetResult((1, 2));
        Assert.True(await click);
        Assert.False(LocateButton(cut).HasAttribute("disabled"));
        Assert.DoesNotContain("_loading", LocateButton(cut).ClassName);
    }

    [Fact]
    public async Task A_second_request_while_locating_is_ignored()
    {
        Locator.LocationGate = new TaskCompletionSource<(double Lat, double Lng)?>();
        IRenderedComponent<AzureMap> cut = RenderLocate();
        Task<bool> first = cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        bool second = await cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());
        Locator.LocationGate.SetResult((1, 2));

        Assert.False(second);
        Assert.True(await first);
        Assert.Equal(1, Locator.LocationRequests);
    }

    [Fact]
    public async Task PinMyLocationAsync_returns_true_when_the_map_was_recentred_and_false_when_not()
    {
        IRenderedComponent<AzureMap> cut = RenderLocate();

        Locator.Location = null;
        bool missing = await cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());
        Locator.Location = (5, 6);
        bool found = await cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        Assert.False(missing);
        Assert.True(found);
    }

    [Fact]
    public async Task Pinning_the_location_does_not_check_the_permission_state_itself()
    {
        Locator.Permission = LocationPermissionState.Denied;
        Locator.Location = (5, 6);
        IRenderedComponent<AzureMap> cut = RenderLocate();

        await cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        Assert.Equal(0, Locator.PermissionChecks);
        Assert.True(Controller.WasCalled("showCurrentLocation"));
    }

    // ── Location lock ───────────────────────────────────────────────────────

    [Fact]
    public async Task A_location_outside_the_lock_is_rejected_and_the_map_is_not_recentred()
    {
        Controller.Returns("resolveBoundary", new[] { new[] { new[] { 0.0, 0.0 } } });
        Controller.Returns("isPointAllowed", false);
        Locator.Location = (48.85, 2.35);
        List<MapCoordinate> rejected = [];
        IRenderedComponent<AzureMap> cut = RenderLocate(p => p
            .Add(m => m.LocationLock, new LocationLockOptions("LB"))
            .Add(m => m.OnLocationLockRejected, (MapCoordinate c) => rejected.Add(c)));

        Task<bool> pinned = cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        cut.WaitForAssertion(() => Assert.Equal([new MapCoordinate(48.85, 2.35)], rejected));
        Assert.False(Controller.WasCalled("showCurrentLocation"));
        Assert.Empty(_found);
        Assert.Equal("[48.85,2.35]", Controller.Single("isPointAllowed").ArgsJson);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));
        Assert.False(await pinned);
    }

    [Fact(Skip = "BUG: AzureMap.razor.cs RecenterOnCurrentLocationAsync awaits RaiseLocationLockRejectedAsync, which awaits the 2.6 s toast timer, so after a rejected locate the button stays disabled and spinning until the toast disappears")]
    public void After_a_rejected_locate_the_button_is_usable_again_while_the_toast_is_showing()
    {
        Controller.Returns("resolveBoundary", new[] { new[] { new[] { 0.0, 0.0 } } });
        Controller.Returns("isPointAllowed", false);
        Locator.Location = (48.85, 2.35);
        IRenderedComponent<AzureMap> cut = RenderLocate(p => p.Add(m => m.LocationLock, new LocationLockOptions("LB")));

        _ = cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".map-lock-toast")));
        Assert.False(LocateButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_location_inside_the_lock_recentres_the_map()
    {
        Controller.Returns("resolveBoundary", new[] { new[] { new[] { 0.0, 0.0 } } });
        Controller.Returns("isPointAllowed", true);
        Locator.Location = (33.9, 35.5);
        IRenderedComponent<AzureMap> cut = RenderLocate(p => p.Add(m => m.LocationLock, new LocationLockOptions("LB")));

        bool pinned = await cut.InvokeAsync(() => cut.Instance.PinMyLocationAsync());

        Assert.True(pinned);
        Assert.Equal([new MapCoordinate(33.9, 35.5)], _found);
    }

    // ── Automatic locate ────────────────────────────────────────────────────

    [Fact]
    public void With_an_InitialView_the_permission_is_never_checked_and_no_location_is_requested()
    {
        Locator.Permission = LocationPermissionState.Granted;
        Locator.Location = (1, 2);

        RenderLocate(withInitialView: true);

        Assert.Equal(0, Locator.PermissionChecks);
        Assert.Equal(0, Locator.LocationRequests);
        Assert.False(Controller.WasCalled("showCurrentLocation"));
    }

    [Fact]
    public void Without_an_InitialView_and_with_permission_granted_the_map_centres_on_the_user_straight_away()
    {
        Locator.Permission = LocationPermissionState.Granted;
        Locator.Location = (33.9, 35.5);

        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);

        cut.WaitForAssertion(() => Assert.Equal([new MapCoordinate(33.9, 35.5)], _found));
        Assert.Equal("[33.9,35.5]", Controller.Single("showCurrentLocation").ArgsJson);
        Assert.Empty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public void The_map_is_created_at_the_world_view_first_and_only_then_moved_to_the_user()
    {
        Locator.Permission = LocationPermissionState.Granted;
        Locator.Location = (33.9, 35.5);

        RenderLocate(withInitialView: false);

        Assert.Equal(0, Js.CreateMapOptions.GetProperty("latitude").GetDouble());
        Assert.Equal(2, Js.CreateMapOptions.GetProperty("zoom").GetDouble());
        Assert.True(Js.AllCalls.Single(c => c.Method == "createMap").Sequence < Controller.Single("showCurrentLocation").Sequence);
    }

    [Theory]
    [InlineData(LocationPermissionState.Denied)]
    [InlineData(LocationPermissionState.Unsupported)]
    public void Without_an_InitialView_a_denied_or_unsupported_permission_neither_prompts_nor_locates(LocationPermissionState state)
    {
        Locator.Permission = state;
        Locator.Location = (1, 2);

        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);

        cut.WaitForAssertion(() => Assert.Equal(1, Locator.PermissionChecks));
        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.Equal(0, Locator.LocationRequests);
    }

    [Fact]
    public void A_prompt_state_shows_the_consent_popup_with_the_default_texts_before_anything_is_requested()
    {
        Locator.Permission = LocationPermissionState.Prompt;
        Locator.Location = (1, 2);

        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".amc-popup")));
        Assert.Equal("Use your location?", cut.Find(".amc-popup .header strong").TextContent);
        Assert.Equal("Allow this app to access your current location so the map can center on you.", cut.Find(".map-permission-message").TextContent);
        Assert.Equal("Allow", cut.Find(".map-permission-confirm").TextContent.Trim());
        Assert.Equal("Cancel", cut.Find(".map-permission-cancel").TextContent.Trim());
        Assert.Equal(0, Locator.LocationRequests);
    }

    [Fact]
    public void The_consent_popup_uses_the_text_parameters()
    {
        Locator.Permission = LocationPermissionState.Prompt;

        IRenderedComponent<AzureMap> cut = RenderLocate(p => p
            .Add(m => m.LocationPromptTitle, "Où êtes-vous ?")
            .Add(m => m.LocationPromptMessage, "Autoriser la localisation.")
            .Add(m => m.LocationPromptConfirmText, "Oui")
            .Add(m => m.LocationPromptCancelText, "Non"), withInitialView: false);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".amc-popup")));
        Assert.Equal("Où êtes-vous ?", cut.Find(".amc-popup .header strong").TextContent);
        Assert.Equal("Autoriser la localisation.", cut.Find(".map-permission-message").TextContent);
        Assert.Equal("Oui", cut.Find(".map-permission-confirm").TextContent.Trim());
        Assert.Equal("Non", cut.Find(".map-permission-cancel").TextContent.Trim());
    }

    [Fact]
    public void Allowing_in_the_popup_closes_it_and_centres_the_map_on_the_user()
    {
        Locator.Permission = LocationPermissionState.Prompt;
        Locator.Location = (33.9, 35.5);
        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".amc-popup")));

        cut.Find(".map-permission-confirm").Click();

        cut.WaitForAssertion(() => Assert.Equal([new MapCoordinate(33.9, 35.5)], _found));
        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.Equal(1, Locator.LocationRequests);
    }

    [Fact]
    public void Cancelling_the_popup_closes_it_and_never_asks_for_the_location()
    {
        Locator.Permission = LocationPermissionState.Prompt;
        Locator.Location = (33.9, 35.5);
        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".amc-popup")));

        cut.Find(".map-permission-cancel").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".amc-popup")));
        Assert.Equal(0, Locator.LocationRequests);
        Assert.Empty(_found);
        Assert.False(Controller.WasCalled("showCurrentLocation"));
    }

    [Fact]
    public void Dismissing_the_popup_with_its_close_button_counts_as_cancel()
    {
        Locator.Permission = LocationPermissionState.Prompt;
        Locator.Location = (33.9, 35.5);
        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".amc-popup")));

        cut.Find(".amc-popup .closemodal").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".amc-popup")));
        Assert.Equal(0, Locator.LocationRequests);
        Assert.Empty(_found);
    }

    [Fact]
    public void While_the_popup_is_open_the_locate_button_is_not_yet_busy()
    {
        Locator.Permission = LocationPermissionState.Prompt;
        IRenderedComponent<AzureMap> cut = RenderLocate(withInitialView: false);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".amc-popup")));

        Assert.False(LocateButton(cut).HasAttribute("disabled"));
    }
}
