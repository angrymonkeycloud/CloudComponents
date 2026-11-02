using System.Runtime.CompilerServices;

namespace CloudComponents.Tests.Core.Interactive;

/// <summary>Finds the core library's own files on disk, such as the JavaScript it ships, from wherever the tests were compiled.</summary>
internal static class LibrarySource
{
    private static readonly Lazy<string> Root = new(Find);

    private static string Find()
    {
        // The compile-time path of this file is inside the repository even when the test assembly is built somewhere else.
        string? start = Here();

        for (DirectoryInfo? directory = start is null ? null : new FileInfo(start).Directory; directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "CloudComponents", "CloudComponents.csproj");

            if (File.Exists(candidate))
                return Path.Combine(directory.FullName, "CloudComponents");
        }

        throw new DirectoryNotFoundException("The CloudComponents library project was not found above " + start);
    }

    private static string Here([CallerFilePath] string path = "") => path;

    public static string FilePath(params string[] parts) => Path.Combine([Root.Value, .. parts]);

    public static string ReadAllText(params string[] parts) => File.ReadAllText(FilePath(parts));

    public static bool Exists(params string[] parts) => File.Exists(FilePath(parts));
}
