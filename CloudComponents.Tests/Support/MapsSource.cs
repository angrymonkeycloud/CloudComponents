namespace CloudComponents.Tests.Support;

/// <summary>Finds the library sources the tests read from disk, such as the Maps JavaScript, wherever the tests are run from.</summary>
public static class MapsSource
{
    private static readonly Lazy<string> Root = new(FindMapsRoot);

    /// <summary>The root of the <c>CloudComponents.Maps</c> project.</summary>
    public static string Directory => Root.Value;

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root.Value, .. parts]);

    private static string FindMapsRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = System.IO.Path.Combine(directory.FullName, "CloudComponents.Maps", "wwwroot", "mapInterop.js");

            if (File.Exists(candidate))
                return System.IO.Path.Combine(directory.FullName, "CloudComponents.Maps");
        }

        throw new DirectoryNotFoundException("CloudComponents.Maps was not found above " + AppContext.BaseDirectory);
    }
}
