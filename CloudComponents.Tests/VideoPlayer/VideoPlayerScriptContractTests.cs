using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using global::CloudComponents.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>
/// The player talks to the browser through global functions defined in its wwwroot scripts. These tests read the
/// scripts from the repository and check that C# and JavaScript still agree on names and on the .NET assembly name.
/// </summary>
public partial class VideoPlayerScriptContractTests
{
    private static readonly string LibraryDirectory = FindLibrary();

    private static string FindLibrary([CallerFilePath] string thisFile = "")
    {
        // <repo>\CloudComponents.Tests\VideoPlayer\<this file>
        string repository = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));
        return Path.Combine(repository, "CloudComponents.VideoPlayer");
    }

    private static string Script(string name) => File.ReadAllText(Path.Combine(LibraryDirectory, "wwwroot", name));

    [GeneratedRegex(@"Invoke(?:Void)?Async(?:<[^>]+>)?\(\s*(?:JS\s*,\s*)?""(?<name>[A-Za-z0-9_]+)""")]
    private static partial Regex InteropCall();

    private static string[] FunctionsCalledFromCSharp() =>
    [
        .. Directory.EnumerateFiles(LibraryDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(LibraryDirectory, "*.razor", SearchOption.TopDirectoryOnly))
            .SelectMany(file => InteropCall().Matches(File.ReadAllText(file)).Select(match => match.Groups["name"].Value))
            .Where(name => name != "import")
            .Distinct()
            .Order()
    ];

    [Fact]
    public void The_library_sources_are_found()
    {
        Assert.True(Directory.Exists(LibraryDirectory), LibraryDirectory);
        Assert.True(File.Exists(Path.Combine(LibraryDirectory, "wwwroot", "videoPlayer.js")));
    }

    [Fact]
    public void The_scan_finds_the_interop_calls_it_is_meant_to_check()
    {
        string[] called = FunctionsCalledFromCSharp();

        Assert.True(called.Length >= 20, "Only found: " + string.Join(", ", called));
        Assert.Contains("amcVideoPlayerPlay", called);
        Assert.Contains("amcVideoPlayerStartCasting", called);
        Assert.Contains("amcVideoPlayerRemovePoster", called);
    }

    [Fact]
    public void Every_function_the_player_calls_is_defined_by_one_of_its_scripts()
    {
        string scripts = string.Join("\n", new[] { "videoPlayer.js", "videoPlayerCast.js", "progressbar.js" }.Select(Script));

        string[] missing =
        [
            .. FunctionsCalledFromCSharp().Where(name => !Regex.IsMatch(scripts, $@"window\.{name}\s*=|function\s+{name}\s*\("))
        ];
        Assert.True(missing.Length == 0, "Called from C# but defined nowhere in the scripts: " + string.Join(", ", missing));
    }

    [Fact]
    public void The_module_loader_loads_every_script_the_player_needs()
    {
        string loader = Script("CloudComponents.VideoPlayer.lib.module.js");

        foreach (string script in new[] { "hls.js", "videoPlayer.js", "videoPlayerCast.js", "progressbar.js" })
            Assert.Contains($"_content/AngryMonkey.CloudComponents.VideoPlayer/{script}", loader);
    }

    [Fact(Skip = "BUG: videoPlayerCast.js calls DotNet.invokeMethodAsync('CloudComponents.VideoPlayer', ...) but the assembly is named AngryMonkey.CloudComponents.VideoPlayer, so every cast event (connect, playing, timeupdate ...) fails to reach HandleCastJsEventStatic")]
    public void The_cast_script_calls_back_into_the_real_assembly_name()
    {
        string assembly = typeof(VideoPlayerCast).Assembly.GetName().Name!;
        Match call = Regex.Match(Script("videoPlayerCast.js"), @"invokeMethodAsync\(\s*'(?<assembly>[^']+)'\s*,\s*'HandleCastJsEventStatic'");

        Assert.True(call.Success, "The script no longer calls HandleCastJsEventStatic");
        Assert.Equal(assembly, call.Groups["assembly"].Value);
    }

    [Fact]
    public void The_static_cast_callback_the_script_targets_exists_and_is_invokable()
    {
        var method = typeof(VideoPlayerCast).GetMethod(nameof(VideoPlayerCast.HandleCastJsEventStatic));

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.NotEmpty(method.GetCustomAttributes(typeof(Microsoft.JSInterop.JSInvokableAttribute), false));
    }

    [Fact(Skip = "BUG: videoPlayer.js amcVideoPlayerStop calls an undefined pause(component) before resetting currentTime, so it throws ReferenceError and the video is neither paused nor rewound (the C# catch only logs it)")]
    public void The_stop_script_pauses_and_rewinds_the_video()
    {
        string script = Script("videoPlayer.js");
        Match body = Regex.Match(script, @"window\.amcVideoPlayerStop\s*=\s*\(component\)\s*=>\s*\{(?<body>.*?)\n\}", RegexOptions.Singleline);

        Assert.True(body.Success);
        Assert.DoesNotMatch(@"(?<![.\w])pause\(", body.Groups["body"].Value);
        Assert.Contains("currentTime = 0", body.Groups["body"].Value);
    }
}
