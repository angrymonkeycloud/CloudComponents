using System.Diagnostics;

namespace CloudComponents.Tests.Support;

/// <summary>Runs the JavaScript test files in <c>js/</c> with Node's built-in test runner.</summary>
public static class NodeRunner
{
    private static readonly Lazy<string?> NodeExecutable = new(FindNode);

    public static bool Available => NodeExecutable.Value is not null;

    /// <summary>The folder holding <c>CloudComponents.Tests.csproj</c>, found from wherever the tests were built to.</summary>
    public static string TestProjectDirectory { get; } = FindTestProjectDirectory();

    public static string JsDirectory => Path.Combine(TestProjectDirectory, "js");

    /// <summary>Every <c>*.test.mjs</c> file, relative to <c>js/</c>, so each one is its own test and a failure names the file.</summary>
    public static IEnumerable<string> TestFiles() =>
        Directory.EnumerateFiles(JsDirectory, "*.test.mjs", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(JsDirectory, file).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);

    public sealed record Result(int ExitCode, string Output);

    public static async Task<Result> RunAsync(string relativeFile, TimeSpan timeout)
    {
        ProcessStartInfo start = new(NodeExecutable.Value ?? throw new InvalidOperationException("Node.js was not found."))
        {
            WorkingDirectory = JsDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("--test");
        start.ArgumentList.Add(relativeFile);

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Node.js could not be started.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource cancellation = new(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return new Result(-1, $"Timed out after {timeout.TotalSeconds:0} seconds.\n{await output}\n{await errors}");
        }

        return new Result(process.ExitCode, await output + await errors);
    }

    private static string? FindNode()
    {
        string name = OperatingSystem.IsWindows() ? "node.exe" : "node";

        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                string candidate = Path.Combine(directory.Trim(), name);

                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException)
            {
                // A malformed PATH entry is not a reason to fail the search.
            }
        }

        return null;
    }

    private static string FindTestProjectDirectory()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "CloudComponents.Tests.csproj")))
                return directory.FullName;

        throw new DirectoryNotFoundException("CloudComponents.Tests.csproj was not found above " + AppContext.BaseDirectory);
    }
}

/// <summary>A theory that needs Node.js; it is skipped, saying so, on a machine without it.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NodeTheoryAttribute : TheoryAttribute
{
    public NodeTheoryAttribute()
    {
        if (!NodeRunner.Available)
            Skip = "Node.js 22 or later is needed on the PATH to run the JavaScript tests.";
    }
}
