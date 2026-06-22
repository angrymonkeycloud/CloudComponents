namespace CloudComponents.Maps.Services;

/// <summary>Browser/device geolocation permission state.</summary>
public enum LocationPermissionState
{
    /// <summary>Geolocation isn't available (no API, no GPS, etc.).</summary>
    Unsupported,
    /// <summary>Permission has not been requested yet — calling get-location will prompt.</summary>
    Prompt,
    /// <summary>The user has explicitly granted geolocation access.</summary>
    Granted,
    /// <summary>The user has explicitly denied geolocation access.</summary>
    Denied
}

public interface ILocationService
{
    /// <summary>Returns the current device location, or <c>null</c> if unavailable / denied.</summary>
    Task<(double Lat, double Lng)?> GetCurrentLocationAsync();

    /// <summary>
    /// Returns the current permission state without triggering the OS prompt.
    /// Implementations that can't introspect (e.g. some MAUI targets) should
    /// return <see cref="LocationPermissionState.Prompt"/>.
    /// </summary>
    Task<LocationPermissionState> GetPermissionStateAsync();
}

