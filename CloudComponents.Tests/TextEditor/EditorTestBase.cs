using AngleSharp.Dom;
using AngryMonkey.CloudComponents.TextEditor.Components;
using AngryMonkey.CloudComponents.TextEditor.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.TextEditor;

/// <summary>Shared set-up: the scripted browser replaces the JS runtime, and helpers find toolbar buttons by their title.</summary>
public abstract class EditorTestBase : BunitContext
{
    protected EditorTestBase()
    {
        DefaultWaitTimeout = TimeSpan.FromSeconds(8);

        Js = new FakeEditorJs();
        Services.AddSingleton<IJSRuntime>(Js);
    }

    protected FakeEditorJs Js { get; }

    /// <summary>Renders the editor and waits until its script controller has been created and given the initial content.</summary>
    protected IRenderedComponent<CloudTextEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<CloudTextEditor>>? configure = null)
    {
        IRenderedComponent<CloudTextEditor> cut = Render<CloudTextEditor>(parameters => configure?.Invoke(parameters));

        cut.WaitForAssertion(() => Assert.NotEmpty(Js.ControllerCalls("setHtml")));
        return cut;
    }

    protected static IElement Button(IRenderedComponent<CloudTextEditor> cut, string title) =>
        cut.Find($"button[title='{title}']");

    protected static bool HasButton(IRenderedComponent<CloudTextEditor> cut, string title) =>
        cut.FindAll($"button[title='{title}']").Count > 0;

    protected static bool IsDisabled(IElement element) => element.HasAttribute("disabled");

    protected static string[] ToolbarTitles(IRenderedComponent<CloudTextEditor> cut) =>
        [.. cut.FindAll(".cloudeditor-toolbar button").Select(button => button.GetAttribute("title") ?? string.Empty)];

    protected static Task Select(IRenderedComponent<CloudTextEditor> cut, string title) => Button(cut, title).ClickAsync(new());

    /// <summary>Pushes a selection state, as the script does whenever the caret moves.</summary>
    protected static Task PushState(IRenderedComponent<CloudTextEditor> cut, Action<CloudTextEditorSelectionState> configure)
    {
        CloudTextEditorSelectionState state = new();
        configure(state);
        return cut.InvokeAsync(() => cut.Instance.UpdateSelectionStateAsync(state));
    }

    protected object?[] OnlyCall(string identifier) => Assert.Single(Js.ControllerCalls(identifier)).Arguments;
}
