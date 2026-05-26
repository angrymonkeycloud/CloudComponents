using CloudComponents.Maps.Services;

namespace CloudComponents.Maps.Mobile.Services;

/// <summary>
/// .NET MAUI implementation of <see cref="ILocationService"/> using the native
/// <see cref="Geolocation"/> API. Requests location permission on demand.
/// </summary>
public sealed class MauiLocationService : ILocationService
{
    public async Task<(double Lat, double Lng)?> GetCurrentLocationAsync()
    {
        try
        {
            if (!await EnsureForegroundPermissionAsync())
                return null;

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

            return location is null ? null : (location.Latitude, location.Longitude);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> EnsureForegroundPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        return status == PermissionStatus.Granted;
    }

    public Task<LocationPermissionState> GetPermissionStateAsync()
    {
        throw new NotImplementedException();
    }
}
