using System.Text.RegularExpressions;
using AngryMonkey.CloudComponents.Maps.Components;
using CloudComponents.Tests.Support;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>
/// The two directions of the C# / JavaScript boundary checked against the shipped <c>mapInterop.js</c> text: every function the
/// component calls exists, and every .NET method the script calls exists with the same number of arguments. A rename on either side
/// fails here; a different provider's script must keep these names. (The behaviour of the script is tested elsewhere.)
/// </summary>
public sealed partial class AzureMapJsContractTests
{
    private static readonly string Script = File.ReadAllText(MapsSource.Path("wwwroot", "mapInterop.js"));

    /// <summary>Methods of the controller class: indented definitions such as <c>    async geocode(query) {</c>.</summary>
    private static HashSet<string> ControllerMethods { get; } = [.. ControllerMethodPattern().Matches(Script).Select(m => m.Groups["name"].Value)];

    private static HashSet<string> ModuleExports { get; } = [.. ExportPattern().Matches(Script).Select(m => m.Groups["name"].Value)];

    [GeneratedRegex(@"^    (?:async\s+)?(?<name>[A-Za-z]\w*)\([^)]*\)\s*\{", RegexOptions.Multiline)]
    private static partial Regex ControllerMethodPattern();

    [GeneratedRegex(@"^export\s+(?:async\s+)?function\s+(?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex ExportPattern();

    [GeneratedRegex(@"Invoke(?:Void)?Async(?:<[^(]+>)?\(\s*""(?<name>\w+)""")]
    private static partial Regex ControllerCallPattern();

    /// <summary>Function names the component calls on the controller (the module-level import and createMap are checked separately).</summary>
    private static string[] ControllerCalls() =>
    [
        .. ComponentSources()
            .SelectMany(source => ControllerCallPattern().Matches(source).Select(m => m.Groups["name"].Value))
            .Where(name => name is not ("import" or "createMap"))
            .Distinct()
            .Order()
    ];

    private static string[] ComponentSources() =>
        [.. Directory.GetFiles(MapsSource.Path("Components"), "AzureMap*.cs").Select(File.ReadAllText)];

    [Fact]
    public void The_script_exports_the_entry_points_the_component_and_the_location_service_import()
    {
        Assert.Contains("createMap", ModuleExports);
        Assert.Contains("getCurrentLocation", ModuleExports);
        Assert.Contains("queryLocationPermission", ModuleExports);
    }

    [Fact]
    public void Every_controller_function_the_component_calls_exists_in_the_script()
    {
        string[] called = ControllerCalls();

        Assert.NotEmpty(called);
        string[] missing = [.. called.Where(name => !ControllerMethods.Contains(name))];
        Assert.True(missing.Length == 0, "mapInterop.js has no controller method for: " + string.Join(", ", missing));
    }

    [Fact]
    public void The_component_calls_a_known_set_of_controller_functions()
    {
        // The list a replacement script has to implement. Adding a call to the component must show up here on purpose.
        string[] called = ControllerCalls();

        string[] expected =
        [
            "addMarker", "addMergedRegion", "addRegion", "clearLocationLock", "clearMarkers", "clearRegions", "clearRoutes", "clearTimelines",
            "dispose", "geocode", "getBrowserLocation", "getPolygon", "isPointAllowed", "removeMarker", "removeRegion", "resolveBoundary",
            "reverseGeocode", "search", "searchMunicipalities", "setAddMarkerTrigger", "setBounds", "setCameraOrientation", "setCenter",
            "setControls", "setInteractions", "setLocationLock", "setRoutes", "setStyle", "setTimelines", "setTraffic", "showCurrentLocation"
        ];

        Assert.Equal(expected, called);
    }

    [Fact]
    public void Every_dot_net_method_the_script_invokes_is_a_JSInvokable_on_the_component_with_the_same_argument_count()
    {
        MatchCollection calls = Regex.Matches(Script, @"invokeMethodAsync\((?<args>[^;]*?)\);");
        Assert.NotEmpty(calls);

        foreach (Match call in calls)
        {
            string[] arguments = SplitTopLevel(call.Groups["args"].Value);
            string name = arguments[0].Trim().Trim('\'', '"');

            Assert.True(FakeMapJs.Invokables.TryGetValue(name, out System.Reflection.MethodInfo? method), $"mapInterop.js invokes '{name}' but AzureMap has no [JSInvokable] with that name");
            Assert.True(
                method!.GetParameters().Length == arguments.Length - 1,
                $"mapInterop.js invokes '{name}' with {arguments.Length - 1} argument(s) but AzureMap.{method.Name} takes {method.GetParameters().Length}");
        }
    }

    [Fact]
    public void Every_JSInvokable_of_the_component_is_invoked_by_the_script_except_the_ones_only_the_C_sharp_side_needs()
    {
        HashSet<string> invoked = [.. Regex.Matches(Script, @"invokeMethodAsync\(\s*'(?<name>\w+)'").Select(m => m.Groups["name"].Value)];

        string[] unused = [.. FakeMapJs.Invokables.Keys.Where(name => !invoked.Contains(name))];

        Assert.True(unused.Length == 0, "No script code invokes: " + string.Join(", ", unused));
    }

    private static string[] SplitTopLevel(string arguments)
    {
        List<string> parts = [];
        int depth = 0;
        int start = 0;

        for (int i = 0; i < arguments.Length; i++)
        {
            char c = arguments[i];

            if (c is '(' or '[' or '{')
                depth++;
            else if (c is ')' or ']' or '}')
                depth--;
            else if (c == ',' && depth == 0)
            {
                parts.Add(arguments[start..i]);
                start = i + 1;
            }
        }

        parts.Add(arguments[start..]);
        return [.. parts];
    }
}
