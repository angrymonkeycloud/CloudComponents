using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AngryMonkey.CloudComponents.TextEditor.Components;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.TextEditor;

/// <summary>
/// The editor engine is JavaScript (wwwroot/cloudeditor.js). These tests read it from the repository and check that C# and
/// the script agree on the module path, the controller's method names and the .NET callbacks the script invokes.
/// </summary>
public partial class EditorScriptContractTests
{
    private static readonly string LibraryDirectory = FindLibrary();

    private static string FindLibrary([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..")), "CloudComponents.TextEditor");

    private static string Script => File.ReadAllText(Path.Combine(LibraryDirectory, "wwwroot", "cloudeditor.js"));

    [GeneratedRegex(@"_controller[!]?\.Invoke(?:Void)?Async(?:<[^>]+>)?\(\s*""(?<name>[A-Za-z]+)""")]
    private static partial Regex ControllerCall();

    [GeneratedRegex(@"#invoke\('(?<name>[A-Za-z]+)'")]
    private static partial Regex ScriptCallback();

    private static string[] ControllerMethodsCalledFromCSharp() =>
    [
        .. Directory.EnumerateFiles(Path.Combine(LibraryDirectory, "Components"), "*.cs")
            .SelectMany(file => ControllerCall().Matches(File.ReadAllText(file)).Select(match => match.Groups["name"].Value))
            .Distinct()
            .Order()
    ];

    [Fact]
    public void The_script_is_found_in_the_repository()
    {
        Assert.True(File.Exists(Path.Combine(LibraryDirectory, "wwwroot", "cloudeditor.js")), LibraryDirectory);
    }

    [Fact]
    public void The_module_path_points_at_this_package_and_matches_the_one_the_tests_expect()
    {
        string path = (string)typeof(CloudTextEditor).GetField("ModulePath", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;

        Assert.Equal(FakeEditorJs.ModulePath, path);
        Assert.Equal($"./_content/{typeof(CloudTextEditor).Assembly.GetName().Name}/cloudeditor.js", path);
    }

    [Fact]
    public void The_script_exports_the_create_function_the_component_calls()
    {
        Assert.Matches(@"export function create\(\s*dotNetRef\s*,\s*editorElement\s*,\s*options\s*\)", Script);
    }

    [Fact]
    public void The_scan_finds_the_controller_calls_it_is_meant_to_check()
    {
        string[] called = ControllerMethodsCalledFromCSharp();

        Assert.True(called.Length >= 12, "Only found: " + string.Join(", ", called));
        Assert.Contains("setHtml", called);
        Assert.Contains("createLink", called);
        Assert.Contains("dispose", called);
    }

    [Fact]
    public void Every_controller_method_the_component_calls_exists_on_the_script_controller()
    {
        string script = Script;

        string[] missing = [.. ControllerMethodsCalledFromCSharp().Where(name => !Regex.IsMatch(script, $@"^\s{{4}}{name}\(", RegexOptions.Multiline))];

        Assert.True(missing.Length == 0, "Called from C# but not on the controller: " + string.Join(", ", missing));
    }

    [Fact]
    public void Every_dotnet_callback_the_script_invokes_is_a_jsinvokable_method_of_the_component()
    {
        string[] invokedByScript = [.. ScriptCallback().Matches(Script).Select(match => match.Groups["name"].Value).Distinct().Order()];
        string[] invokable = [.. typeof(CloudTextEditor).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<JSInvokableAttribute>() is not null)
            .Select(method => method.Name)
            .Order()];

        Assert.NotEmpty(invokedByScript);
        Assert.Equal(invokable, invokedByScript);
    }

    [Fact]
    public void The_script_sends_the_selection_state_with_the_property_names_the_model_reads()
    {
        string script = Script;
        string[] jsonNames = [.. typeof(AngryMonkey.CloudComponents.TextEditor.Models.CloudTextEditorSelectionState).GetProperties()
            .Select(property => property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()!.Name)];

        Assert.All(jsonNames, name => Assert.Matches($@"\b{name}\s*:", script));
    }

    [Fact]
    public void The_script_honours_the_options_the_component_passes()
    {
        string script = Script;

        Assert.Contains("debounceMs", script);
        Assert.Contains("sanitizePaste", script);
        Assert.Contains("allowIframes", script);
    }

    [Fact]
    public void The_paste_allow_list_keeps_the_tags_the_editor_itself_can_produce()
    {
        // Everything the toolbar can insert must survive sanitising a copy/paste of the editor's own output.
        Match allowList = Regex.Match(Script, @"const ALLOWED_TAGS = \{(?<body>.*?)\n\};", RegexOptions.Singleline);
        Assert.True(allowList.Success);

        string[] produced = ["p", "h1", "h2", "h3", "h4", "h5", "h6", "strong", "b", "em", "i", "u", "s", "strike", "sub", "sup", "code", "pre", "blockquote",
            "ul", "ol", "li", "a", "img", "video", "figure", "iframe", "hr", "br", "span", "font"];

        string[] missing = [.. produced.Where(tag => !Regex.IsMatch(allowList.Groups["body"].Value, $@"\b{tag}\s*:"))];

        Assert.True(missing.Length == 0, "Tags the editor produces but pasting would strip: " + string.Join(", ", missing));
    }

    [Fact]
    public void Scripts_and_inline_event_handlers_can_never_be_pasted_in()
    {
        string script = Script;
        Match removed = Regex.Match(script, @"const REMOVE_ENTIRELY = \[(?<body>[^\]]*)\]");

        Assert.True(removed.Success);
        Assert.All(new[] { "script", "style", "object", "embed", "form" }, tag => Assert.Contains($"'{tag}'", removed.Groups["body"].Value));    }
}
