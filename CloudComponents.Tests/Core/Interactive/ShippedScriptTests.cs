using System.Text.RegularExpressions;

namespace CloudComponents.Tests.Core.Interactive;

/// <summary>
/// The components call into JavaScript files the library ships. These tests read those files and check the contract the
/// components rely on: the functions exist under the names the C# code calls, in the files the C# code loads.
/// </summary>
public class ShippedScriptTests
{
    private static string[] CalledProgressBarFunctions() =>
        [.. Regex.Matches(LibrarySource.ReadAllText("ProgressBar", "ProgressBar.razor.cs"), "\"(amcProgressBar\\w+)\"").Select(match => match.Groups[1].Value).Distinct()];

    [Fact]
    public void The_progress_bar_calls_four_script_functions()
    {
        Assert.Equal(
            ["amcProgressBarGetInfo", "amcProgressBarMouseDown", "amcProgressBarRepaint", "amcProgressBarTouchDown"],
            CalledProgressBarFunctions().Order());
    }

    [Theory]
    [InlineData("wwwroot/js/amc-components.js")]
    [InlineData("wwwroot/js/amc-components.min.js")]
    [InlineData("ProgressBar/progressbar.js")]
    public void Every_function_the_progress_bar_calls_is_defined_on_window_in_the_script(string relativePath)
    {
        string script = LibrarySource.ReadAllText(relativePath.Split('/'));

        foreach (string function in CalledProgressBarFunctions())
            Assert.Matches(new Regex($@"window\.{function}\s*="), script);
    }

    [Theory]
    [InlineData("InitKeyboard")]
    [InlineData("FocusDefault")]
    public void The_dialog_module_exports_the_functions_the_dialog_invokes(string function)
    {
        string script = LibrarySource.ReadAllText("wwwroot", "js", "dialog.js");

        Assert.Matches(new Regex($@"export\s+function\s+{function}\s*\("), script);
    }

    [Fact]
    public void The_dialog_keyboard_handler_is_only_installed_once_per_page()
    {
        string script = LibrarySource.ReadAllText("wwwroot", "js", "dialog.js");

        Assert.Single(Regex.Matches(script, @"document\.addEventListener\('keydown'"));
        Assert.Matches(new Regex(@"if\s*\(\s*keyboardHandler\s*\)\s*return"), script);
    }

    [Fact]
    public void The_markdown_module_exports_the_function_the_component_invokes()
    {
        string script = LibrarySource.ReadAllText("wwwroot", "cloud-markdown.js");

        Assert.Matches(new Regex(@"export\s+function\s+bindAndRestore\s*\("), script);
    }

    [Fact(Skip = "BUG: GeneralMethods.GetIJSObjectReference imports 'js/dialog.min.js' in a Release build, but wwwroot/js has no dialog.min.js (mateconfig.json does not produce one), so Dialog.Open fails with a 404 import in Release.")]
    public void The_dialog_script_exists_in_the_minified_form_a_release_build_imports()
    {
        Assert.True(LibrarySource.Exists("wwwroot", "js", "dialog.js"));
        Assert.True(LibrarySource.Exists("wwwroot", "js", "dialog.min.js"));
    }

    [Fact(Skip = "BUG: progressbar.js (and the shipped amc-components bundle) remove the touch end listener with removeEventListener('touchup', ...) while it was added as 'touchend', so every touch interaction leaks a document listener that later fires with stale state.")]
    public void The_touch_end_listener_is_removed_with_the_same_event_name_it_was_added_with()
    {
        foreach (string file in new[] { "ProgressBar/progressbar.js", "wwwroot/js/amc-components.js" })
        {
            string script = LibrarySource.ReadAllText(file.Split('/'));

            Assert.DoesNotContain("'touchup'", script);
            Assert.Contains("removeEventListener('touchend', touchEndListener)", script);
        }
    }

    [Fact(Skip = "BUG: amcProgressBarGetInfo returns Top: element.getBoundingClientRect().right instead of .top, so ProgressBarSeekButtonInfo.Top is the right edge.")]
    public void The_seek_button_info_reports_the_top_edge_as_top()
    {
        foreach (string file in new[] { "ProgressBar/progressbar.js", "wwwroot/js/amc-components.js" })
        {
            string script = LibrarySource.ReadAllText(file.Split('/'));

            Assert.Contains("Top: element.getBoundingClientRect().top", script);
        }
    }
}
