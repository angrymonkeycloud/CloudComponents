using AngryMonkey.CloudComponents.Maps.Services;
using CloudComponents.Tests.Maps.Component;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Models;

/// <summary>The browser location service: which script functions it calls and how their answers become C# values.</summary>
public sealed class WebLocationServiceTests
{
    private const string ModulePath = "./_content/AngryMonkey.CloudComponents.Maps/mapInterop.js";

    private readonly FakeMapJs _js = new();

    private WebLocationService Service() => new(_js);

    // ── Construction and the module ─────────────────────────────────────────

    [Fact]
    public void Creating_the_service_does_not_touch_JavaScript()
    {
        _ = Service();

        Assert.Empty(_js.AllCalls);
    }

    [Fact]
    public void The_service_is_a_location_service_and_async_disposable()
    {
        WebLocationService service = Service();

        Assert.IsAssignableFrom<ILocationService>(service);
        Assert.IsAssignableFrom<IAsyncDisposable>(service);
    }

    [Fact]
    public async Task The_module_is_imported_from_the_package_static_assets_path()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 1.0, 2.0 });

        await Service().GetCurrentLocationAsync();

        Assert.Equal([ModulePath], _js.Imports);
    }

    [Fact]
    public async Task The_module_is_imported_once_and_shared_by_both_methods()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 1.0, 2.0 });
        _js.Module.Returns("queryLocationPermission", "granted");
        WebLocationService service = Service();

        await service.GetCurrentLocationAsync();
        await service.GetPermissionStateAsync();
        await service.GetCurrentLocationAsync();

        Assert.Single(_js.Imports);
    }

    [Fact]
    public async Task A_failed_import_is_retried_on_the_next_call()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 1.0, 2.0 });
        _js.ImportFailure = new JSException("offline");
        WebLocationService service = Service();
        Assert.Null(await service.GetCurrentLocationAsync());

        _js.ImportFailure = null;
        (double Lat, double Lng)? location = await service.GetCurrentLocationAsync();

        Assert.Equal((1.0, 2.0), location);
        Assert.Equal(2, _js.Imports.Count);
    }

    // ── Current location ────────────────────────────────────────────────────

    [Fact]
    public async Task The_location_script_is_called_without_arguments()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 1.0, 2.0 });

        await Service().GetCurrentLocationAsync();

        Assert.Equal("[]", _js.Module.Single("getCurrentLocation").ArgsJson);
    }

    [Fact]
    public async Task A_two_number_array_becomes_latitude_then_longitude()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 33.9, 35.5 });

        (double Lat, double Lng)? location = await Service().GetCurrentLocationAsync();

        Assert.Equal((33.9, 35.5), location);
    }

    [Fact]
    public async Task Extra_array_elements_are_ignored()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 33.9, 35.5, 12.0 });

        (double Lat, double Lng)? location = await Service().GetCurrentLocationAsync();

        Assert.Equal((33.9, 35.5), location);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[1.5]")]
    public async Task No_array_or_a_short_one_means_no_location(string answer)
    {
        _js.Module.Returns("getCurrentLocation", FakeMapJs.Json(answer));

        Assert.Null(await Service().GetCurrentLocationAsync());
    }

    [Fact]
    public async Task A_script_failure_means_no_location_instead_of_an_exception()
    {
        _js.Module.Throws("getCurrentLocation", new JSException("denied"));

        Assert.Null(await Service().GetCurrentLocationAsync());
    }

    [Fact]
    public async Task A_failed_import_means_no_location_instead_of_an_exception()
    {
        _js.ImportFailure = new JSException("offline");

        Assert.Null(await Service().GetCurrentLocationAsync());
    }

    [Fact]
    public async Task A_disconnected_circuit_means_no_location()
    {
        _js.Module.Throws("getCurrentLocation", new JSDisconnectedException("gone"));

        Assert.Null(await Service().GetCurrentLocationAsync());
    }

    // ── Permission state ────────────────────────────────────────────────────

    [Theory]
    [InlineData("granted", LocationPermissionState.Granted)]
    [InlineData("denied", LocationPermissionState.Denied)]
    [InlineData("unsupported", LocationPermissionState.Unsupported)]
    [InlineData("prompt", LocationPermissionState.Prompt)]
    public async Task Each_state_word_the_script_returns_maps_to_its_state(string word, LocationPermissionState expected)
    {
        _js.Module.Returns("queryLocationPermission", word);

        Assert.Equal(expected, await Service().GetPermissionStateAsync());
    }

    [Theory]
    [InlineData("something-new")]
    [InlineData("")]
    public async Task Any_other_word_is_treated_as_prompt(string word)
    {
        _js.Module.Returns("queryLocationPermission", word);

        Assert.Equal(LocationPermissionState.Prompt, await Service().GetPermissionStateAsync());
    }

    [Fact]
    public async Task A_null_answer_is_treated_as_prompt()
    {
        _js.Module.Returns("queryLocationPermission", null);

        Assert.Equal(LocationPermissionState.Prompt, await Service().GetPermissionStateAsync());
    }

    [Fact]
    public async Task A_script_failure_is_treated_as_prompt_not_as_unsupported_or_denied()
    {
        _js.Module.Throws("queryLocationPermission", new JSException("boom"));

        Assert.Equal(LocationPermissionState.Prompt, await Service().GetPermissionStateAsync());
    }

    [Fact]
    public async Task A_failed_import_is_treated_as_prompt()
    {
        _js.ImportFailure = new JSException("offline");

        Assert.Equal(LocationPermissionState.Prompt, await Service().GetPermissionStateAsync());
    }

    [Fact]
    public async Task The_permission_script_is_called_without_arguments()
    {
        _js.Module.Returns("queryLocationPermission", "granted");

        await Service().GetPermissionStateAsync();

        Assert.Equal("[]", _js.Module.Single("queryLocationPermission").ArgsJson);
    }

    // ── Disposal ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Disposing_before_the_module_was_imported_does_nothing()
    {
        WebLocationService service = Service();

        await service.DisposeAsync();

        Assert.Empty(_js.AllCalls);
        Assert.Equal(0, _js.Module.DisposeCount);
    }

    [Fact]
    public async Task Disposing_releases_the_imported_module()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 1.0, 2.0 });
        WebLocationService service = Service();
        await service.GetCurrentLocationAsync();

        await service.DisposeAsync();

        Assert.Equal(1, _js.Module.DisposeCount);
    }

    [Fact]
    public async Task Disposing_after_the_circuit_disconnected_does_not_throw()
    {
        _js.Module.Returns("getCurrentLocation", new[] { 1.0, 2.0 });
        WebLocationService service = Service();
        await service.GetCurrentLocationAsync();
        _js.Module.DisposeFailure = new JSDisconnectedException("circuit gone");

        await service.DisposeAsync();

        Assert.Equal(1, _js.Module.DisposeCount);
    }
}
