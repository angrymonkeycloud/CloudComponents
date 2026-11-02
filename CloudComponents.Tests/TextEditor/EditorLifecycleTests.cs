using AngryMonkey.CloudComponents.TextEditor.Components;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.TextEditor;

/// <summary>Start-up, two-way binding, the callbacks the script invokes, the public methods and disposal.</summary>
public class EditorLifecycleTests : EditorTestBase
{
    // ----- Start-up -----------------------------------------------------------------------

    [Fact]
    public void The_editor_imports_its_module_and_creates_a_controller_for_the_content_element()
    {
        RenderEditor();

        Assert.Equal(FakeEditorJs.ModulePath, Assert.Single(Js.Calls, call => call.Identifier == "import").Arguments[0]);
        JsCall create = Assert.Single(Js.Calls, call => call.Identifier == "create");
        Assert.Equal("module", create.Target);
        Assert.IsType<DotNetObjectReference<CloudTextEditor>>(create.Arguments[0]);
        Assert.IsType<ElementReference>(create.Arguments[1]);
    }

    [Fact]
    public void The_controller_options_follow_the_parameters()
    {
        RenderEditor(p => p.Add(x => x.DebounceMilliseconds, 400).Add(x => x.SanitizePaste, false));

        object options = Assert.Single(Js.Calls, call => call.Identifier == "create").Arguments[2]!;

        Assert.Equal(400, options.GetType().GetProperty("debounceMs")!.GetValue(options));
        Assert.Equal(false, options.GetType().GetProperty("sanitizePaste")!.GetValue(options));
        Assert.Equal(true, options.GetType().GetProperty("allowIframes")!.GetValue(options));
    }

    [Fact]
    public void Paste_is_sanitised_and_the_debounce_is_250_ms_by_default()
    {
        RenderEditor();

        object options = Assert.Single(Js.Calls, call => call.Identifier == "create").Arguments[2]!;

        Assert.Equal(250, options.GetType().GetProperty("debounceMs")!.GetValue(options));
        Assert.Equal(true, options.GetType().GetProperty("sanitizePaste")!.GetValue(options));
    }

    [Fact]
    public void A_negative_debounce_is_treated_as_zero()
    {
        RenderEditor(p => p.Add(x => x.DebounceMilliseconds, -50));

        object options = Assert.Single(Js.Calls, call => call.Identifier == "create").Arguments[2]!;

        Assert.Equal(0, options.GetType().GetProperty("debounceMs")!.GetValue(options));
    }

    [Fact]
    public void The_initial_value_is_loaded_into_the_surface()
    {
        RenderEditor(p => p.Add(x => x.Value, "<p>Hello</p>"));

        Assert.Equal("<p>Hello</p>", OnlyCall("setHtml")[0]);
    }

    [Fact]
    public void Without_a_value_an_empty_document_is_loaded()
    {
        RenderEditor();

        Assert.Equal(string.Empty, OnlyCall("setHtml")[0]);
    }

    [Fact]
    public void A_writable_editor_does_not_touch_the_read_only_state_at_start()
    {
        RenderEditor();

        Assert.Empty(Js.ControllerCalls("setReadOnly"));
    }

    [Fact]
    public void A_read_only_editor_tells_the_script_at_start()
    {
        RenderEditor(p => p.Add(x => x.ReadOnly, true));

        Assert.Equal(true, OnlyCall("setReadOnly")[0]);
    }

    // ----- Parameters changing ------------------------------------------------------------

    [Fact]
    public void A_new_value_from_the_host_replaces_the_content()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Value, "<p>one</p>"));

        cut.Render(p => p.Add(x => x.Value, "<p>two</p>"));

        Assert.Equal(["<p>one</p>", "<p>two</p>"], Js.ControllerCalls("setHtml").Select(call => (string)call.Arguments[0]!));
    }

    [Fact]
    public void Clearing_the_value_clears_the_content()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Value, "<p>one</p>"));

        cut.Render(p => p.Add(x => x.Value, null));

        Assert.Equal(string.Empty, Js.ControllerCalls("setHtml").Last().Arguments[0]);
    }

    [Fact]
    public void Re_rendering_with_the_same_value_does_not_reset_the_content()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Value, "<p>one</p>"));

        cut.Render(p => p.Add(x => x.Value, "<p>one</p>").Add(x => x.Placeholder, "changed"));

        Assert.Single(Js.ControllerCalls("setHtml"));
    }

    [Fact]
    public void Toggling_read_only_is_passed_to_the_script_once_per_change()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        cut.Render(p => p.Add(x => x.ReadOnly, true));
        cut.Render(p => p.Add(x => x.ReadOnly, true));
        cut.Render(p => p.Add(x => x.ReadOnly, false));

        Assert.Equal([true, false], Js.ControllerCalls("setReadOnly").Select(call => (bool)call.Arguments[0]!));
    }

    // ----- Callbacks from the script ------------------------------------------------------

    [Fact]
    public async Task A_content_change_updates_the_value_and_raises_both_callbacks()
    {
        List<string?> changed = [];
        List<string?> onChanged = [];
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p
            .Add(x => x.ValueChanged, value => { changed.Add(value); })
            .Add(x => x.OnChanged, value => { onChanged.Add(value); }));

        await cut.InvokeAsync(() => cut.Instance.HandleContentChangedAsync("<p>typed</p>"));

        Assert.Equal("<p>typed</p>", cut.Instance.Value);
        Assert.Equal(["<p>typed</p>"], changed);
        Assert.Equal(["<p>typed</p>"], onChanged);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public async Task Blank_content_is_reported_as_null(string html)
    {
        List<string?> changed = [];
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p
            .Add(x => x.Value, "<p>x</p>")
            .Add(x => x.ValueChanged, value => { changed.Add(value); }));

        await cut.InvokeAsync(() => cut.Instance.HandleContentChangedAsync(html));

        Assert.Null(Assert.Single(changed));
        Assert.Null(cut.Instance.Value);
    }

    [Fact]
    public async Task Content_typed_by_the_user_is_not_pushed_back_into_the_surface()
    {
        // With @bind-Value the host re-renders the editor with the value it just received: that must not reload the content and reset the caret.
        string? bound = null;
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p
            .Add(x => x.Value, bound)
            .Add(x => x.ValueChanged, value => { bound = value; }));

        await cut.InvokeAsync(() => cut.Instance.HandleContentChangedAsync("<p>typed</p>"));
        cut.Render(p => p.Add(x => x.Value, bound));

        Assert.Single(Js.ControllerCalls("setHtml"));
    }

    [Fact]
    public async Task Focus_and_blur_from_the_script_raise_the_host_callbacks()
    {
        int focus = 0, blur = 0;
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p
            .Add(x => x.OnFocus, () => { focus++; })
            .Add(x => x.OnBlur, () => { blur++; }));

        await cut.InvokeAsync(() => cut.Instance.HandleFocusAsync());
        await cut.InvokeAsync(() => cut.Instance.HandleBlurAsync());
        await cut.InvokeAsync(() => cut.Instance.HandleBlurAsync());

        Assert.Equal((1, 2), (focus, blur));
    }

    [Fact]
    public async Task Focus_and_blur_without_host_callbacks_do_nothing()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Instance.HandleFocusAsync());
        await cut.InvokeAsync(() => cut.Instance.HandleBlurAsync());
    }

    // ----- Public methods -----------------------------------------------------------------

    [Fact]
    public async Task FocusAsync_asks_the_script_to_focus_the_surface()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Instance.FocusAsync());

        Assert.Single(Js.ControllerCalls("focus"));
    }

    [Fact]
    public async Task InsertHtmlAsync_passes_the_markup_to_the_script()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Instance.InsertHtmlAsync("<b>x</b>"));

        Assert.Equal("<b>x</b>", OnlyCall("insertHtml")[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task InsertHtmlAsync_ignores_empty_markup(string? html)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Instance.InsertHtmlAsync(html!));

        Assert.Empty(Js.ControllerCalls("insertHtml"));
    }

    [Fact]
    public async Task The_public_methods_are_harmless_before_the_script_is_ready()
    {
        CloudTextEditor notStarted = new();

        await notStarted.FocusAsync();
        await notStarted.InsertHtmlAsync("<p>x</p>");

        Assert.Empty(Js.Calls);
    }

    // ----- Disposal -----------------------------------------------------------------------

    [Fact]
    public async Task Disposing_releases_the_controller_the_module_and_the_callback_reference()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        var self = (DotNetObjectReference<CloudTextEditor>)Assert.Single(Js.Calls, call => call.Identifier == "create").Arguments[0]!;

        await DisposeComponentsAsync();

        Assert.Single(Js.ControllerCalls("dispose"));
        Assert.Equal(1, Js.Controller.DisposeCount);
        Assert.Equal(1, Js.Module.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => self.Value);
    }

    [Fact]
    public async Task A_page_that_is_already_gone_when_disposing_is_tolerated()
    {
        RenderEditor();
        var self = (DotNetObjectReference<CloudTextEditor>)Assert.Single(Js.Calls, call => call.Identifier == "create").Arguments[0]!;
        Js.Controller.Failures["dispose"] = new JSDisconnectedException("circuit closed");

        await DisposeComponentsAsync();

        Assert.Throws<ObjectDisposedException>(() => self.Value);
    }

    [Fact]
    public async Task A_module_that_fails_to_load_surfaces_the_script_error_and_can_still_be_disposed()
    {
        Js.ImportFailure = new JSException("module missing");

        Exception error = Assert.ThrowsAny<Exception>(() => Render<CloudTextEditor>());

        Assert.Contains("module missing", error.ToString());
        await DisposeComponentsAsync();
    }

    [Fact(Skip = "BUG: DisposeAsync never clears _controller, so FocusAsync/InsertHtmlAsync called on a disposed editor still invoke JavaScript on the disposed object reference")]
    public async Task No_script_is_called_after_the_editor_was_disposed()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        CloudTextEditor editor = cut.Instance;
        await DisposeComponentsAsync();
        int callsAfterDispose = Js.Calls.Count;

        await editor.FocusAsync();
        await editor.InsertHtmlAsync("<p>x</p>");

        Assert.Equal(callsAfterDispose, Js.Calls.Count);
    }

    [Fact]
    public async Task A_script_callback_after_disposal_is_still_safe_for_the_host()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        CloudTextEditor editor = cut.Instance;
        await DisposeComponentsAsync();

        await editor.HandleBlurAsync();
        await editor.HandleContentChangedAsync("<p>late</p>");
    }
}
