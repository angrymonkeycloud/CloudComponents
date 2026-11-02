using System.Text.RegularExpressions;
using CloudComponents.Tests.Support;
using Xunit.Abstractions;

namespace CloudComponents.Tests.JavaScript;

/// <summary>
/// Runs the JavaScript tests under <c>js/</c> (the Maps controller driven against a fake browser and a fake SDK) so a plain
/// <c>dotnet test</c> covers them. Each file is its own test; a failure shows Node's own report of what failed.
/// </summary>
[Trait("Category", "JavaScript")]
public sealed partial class JavaScriptSuiteTests(ITestOutputHelper output)
{
    public static TheoryData<string> Files()
    {
        TheoryData<string> files = [];

        foreach (string file in NodeRunner.TestFiles())
            files.Add(file);

        return files;
    }

    [NodeTheory]
    [MemberData(nameof(Files))]
    public async Task The_javascript_tests_in_the_file_pass(string file)
    {
        NodeRunner.Result result = await NodeRunner.RunAsync(file, TimeSpan.FromMinutes(2));

        // Known problems are recorded in the tests as TODO: they do not fail the run, but they are never hidden.
        foreach (Match known in KnownIssue().Matches(result.Output))
            output.WriteLine($"Known issue in {file}: {known.Groups["text"].Value.Trim()}");

        Assert.True(result.ExitCode == 0, $"{file} failed (exit code {result.ExitCode}).\n{Tail(result.Output)}");
        Assert.Matches(@"# pass [1-9]", result.Output);
    }

    [Fact]
    public void There_are_javascript_tests_to_run() => Assert.NotEmpty(NodeRunner.TestFiles());

    private static string Tail(string text) => text.Length <= 6000 ? text : "..." + text[^6000..];

    [GeneratedRegex(@"^not ok .*# TODO (?<text>.+)$", RegexOptions.Multiline)]
    private static partial Regex KnownIssue();
}
