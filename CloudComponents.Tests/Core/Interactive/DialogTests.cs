using System.Text.RegularExpressions;
using AngryMonkey.CloudComponents;
using Bunit;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Core.Interactive;

public class DialogTests : InteractiveTestBase
{
    private static List<DialogButton> OkCancel(List<string>? log = null) =>
    [
        new() { Text = "OK", ClassCss = "primary", OnReply = () => log?.Add("ok") },
        new() { Text = "Cancel", OnReply = () => log?.Add("cancel") }
    ];

    private IRenderedComponent<Dialog> RenderDialog(
        List<DialogButton>? buttons = null,
        string title = "Confirm",
        string? message = "Are you sure?",
        List<string>? events = null)
    {
        return Render<Dialog>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.Buttons, buttons ?? OkCancel());

            if (message is not null)
                parameters.AddChildContent(message);

            if (events is not null)
            {
                parameters.Add(p => p.OnOpened, Record(events, "opened"));
                parameters.Add(p => p.OnClosed, Record(events, "closed"));
            }
        });
    }

    private static Task OpenAsync(IRenderedComponent<Dialog> cut) => cut.InvokeAsync(() => cut.Instance.Open());

    [Fact]
    public void A_dialog_is_closed_until_it_is_opened()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.DoesNotContain("Are you sure?", cut.Markup);
        Assert.Empty(Js.Calls);
    }

    [Fact]
    public async Task An_open_dialog_shows_the_title_the_message_and_one_button_per_entry()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        await OpenAsync(cut);

        Assert.Equal("Confirm", cut.Find(".header strong").TextContent);
        Assert.Equal("Are you sure?", cut.Find(".amc-dialog-message").TextContent.Trim());

        AngleSharp.Dom.IElement[] buttons = [.. cut.FindAll(".amc-dialog-buttons button")];
        Assert.Equal(["OK", "Cancel"], buttons.Select(button => button.TextContent));
    }

    [Fact]
    public async Task A_button_class_is_applied_and_a_button_without_one_has_no_class_attribute_value()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        await OpenAsync(cut);

        AngleSharp.Dom.IElement[] buttons = [.. cut.FindAll(".amc-dialog-buttons button")];
        Assert.Equal("primary", buttons[0].GetAttribute("class"));
        Assert.True(string.IsNullOrEmpty(buttons[1].GetAttribute("class")));
    }

    [Fact]
    public async Task A_dialog_without_a_message_renders_no_message_area()
    {
        IRenderedComponent<Dialog> cut = RenderDialog(message: null);

        await OpenAsync(cut);

        Assert.Empty(cut.FindAll(".amc-dialog-message"));
        Assert.NotEmpty(cut.FindAll(".amc-dialog-buttons button"));
    }

    [Fact]
    public async Task A_dialog_without_buttons_renders_no_button_area()
    {
        IRenderedComponent<Dialog> cut = RenderDialog(buttons: []);

        await OpenAsync(cut);

        Assert.Empty(cut.FindAll(".amc-dialog-buttons"));
    }

    [Fact]
    public async Task A_dialog_has_no_header_close_button_and_no_default_close_button_because_its_own_buttons_close_it()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        await OpenAsync(cut);

        Assert.Empty(cut.FindAll(".closemodal"));
        Assert.Empty(cut.FindAll("button.close"));
    }

    [Fact]
    public async Task Opening_a_dialog_imports_the_dialog_script_and_installs_the_keyboard_handler_then_focuses_a_button()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        await OpenAsync(cut);

        JsCall import = Assert.Single(Js.RuntimeCalls("import"));
        Assert.Matches(new Regex(@"^\./_content/AngryMonkey\.CloudComponents/js/dialog(\.min)?\.js\?v=[0-9a-f-]{36}$"), (string)import.Args[0]!);

        Assert.Equal(["InitKeyboard", "FocusDefault"], Js.Calls.Where(call => call.Module is not null).Select(call => call.Identifier));
    }

    [Fact]
    public async Task Opening_a_dialog_again_reuses_the_module_and_only_refocuses()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        await OpenAsync(cut);
        await cut.InvokeAsync(() => cut.Instance.Close());
        await OpenAsync(cut);

        Assert.Single(Js.RuntimeCalls("import"));
        Assert.Single(Js.ModuleCalls("InitKeyboard"));
        Assert.Equal(2, Js.ModuleCalls("FocusDefault").Length);
    }

    [Fact]
    public async Task OnOpened_fires_after_the_dialog_is_shown_and_focused()
    {
        List<string> events = [];
        IRenderedComponent<Dialog> cut = RenderDialog(events: events);

        await OpenAsync(cut);

        Assert.Equal(["opened"], events);
        Assert.NotEmpty(cut.FindAll(".amc-popup"));
        Assert.Single(Js.ModuleCalls("FocusDefault"));
    }

    [Fact]
    public async Task Clicking_a_button_runs_its_reply_closes_the_dialog_and_then_reports_the_close()
    {
        List<string> events = [];
        List<DialogButton> buttons =
        [
            new() { Text = "OK", OnReply = () => events.Add("reply") }
        ];
        IRenderedComponent<Dialog> cut = RenderDialog(buttons, events: events);
        await OpenAsync(cut);
        events.Clear();

        cut.Find(".amc-dialog-buttons button").Click();

        Assert.Equal(["reply", "closed"], events);
        Assert.Empty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public async Task Only_the_clicked_buttons_reply_runs()
    {
        List<string> replies = [];
        IRenderedComponent<Dialog> cut = RenderDialog(OkCancel(replies));
        await OpenAsync(cut);

        cut.FindAll(".amc-dialog-buttons button")[1].Click();

        Assert.Equal(["cancel"], replies);
    }

    [Fact]
    public async Task A_button_with_auto_close_off_runs_its_reply_but_leaves_the_dialog_open()
    {
        List<string> events = [];
        List<DialogButton> buttons = [new() { Text = "Apply", AutoClose = false, OnReply = () => events.Add("applied") }];
        IRenderedComponent<Dialog> cut = RenderDialog(buttons, events: events);
        await OpenAsync(cut);
        events.Clear();

        cut.Find(".amc-dialog-buttons button").Click();

        Assert.Equal(["applied"], events);
        Assert.NotEmpty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public async Task A_button_without_a_reply_still_closes_the_dialog()
    {
        List<DialogButton> buttons = [new() { Text = "Close" }];
        IRenderedComponent<Dialog> cut = RenderDialog(buttons);
        await OpenAsync(cut);

        cut.Find(".amc-dialog-buttons button").Click();

        Assert.Empty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public async Task Clicking_outside_the_dialog_does_not_dismiss_it()
    {
        List<string> events = [];
        IRenderedComponent<Dialog> cut = RenderDialog(events: events);
        await OpenAsync(cut);
        events.Clear();

        cut.Find(".amc-popup").Click();

        Assert.NotEmpty(cut.FindAll(".amc-popup"));
        Assert.Empty(events);
    }

    [Fact]
    public async Task Close_closes_an_open_dialog_and_reports_it_once()
    {
        List<string> events = [];
        IRenderedComponent<Dialog> cut = RenderDialog(events: events);
        await OpenAsync(cut);

        await cut.InvokeAsync(() => cut.Instance.Close());

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.Equal(["opened", "closed"], events);
    }

    [Fact]
    public async Task A_dialog_can_be_closed_through_the_IClosable_interface()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();
        await OpenAsync(cut);
        IClosable closable = Assert.IsAssignableFrom<IClosable>(cut.Instance);

        await cut.InvokeAsync(() => closable.Close());

        Assert.Empty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public async Task Disposing_an_opened_dialog_disposes_its_script_module()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();
        await OpenAsync(cut);
        RecordingJsModule module = Assert.Single(Js.Modules);
        Assert.False(module.IsDisposed);

        await cut.Instance.DisposeAsync();

        Assert.True(module.IsDisposed);
        Assert.Empty(module.CallsAfterDispose);
    }

    [Fact]
    public async Task Disposing_a_dialog_that_was_never_opened_imports_nothing()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();

        await cut.Instance.DisposeAsync();

        Assert.Empty(Js.Calls);
    }

    [Fact]
    public async Task Disposing_after_the_browser_connection_was_lost_does_not_throw()
    {
        IRenderedComponent<Dialog> cut = RenderDialog();
        await OpenAsync(cut);
        Js.ModuleDisposeFailure = new JSDisconnectedException("circuit gone");

        await cut.Instance.DisposeAsync();

        Assert.True(Js.Modules[0].IsDisposed);
    }
}
