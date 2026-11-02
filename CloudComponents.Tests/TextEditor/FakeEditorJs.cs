using Microsoft.JSInterop;

namespace CloudComponents.Tests.TextEditor;

/// <summary>One call made from .NET to a JavaScript object (or to the runtime itself).</summary>
public sealed record JsCall(string Target, string Identifier, object?[] Arguments);

/// <summary>
/// A scripted browser for the editor: the runtime imports a module, the module's <c>create</c> returns the controller, and
/// every call on any of them is recorded. Results and failures can be set per identifier.
/// </summary>
public sealed class FakeEditorJs : IJSRuntime
{
    public const string ModulePath = "./_content/AngryMonkey.CloudComponents.TextEditor/cloudeditor.js";

    private readonly List<JsCall> _calls = [];

    public FakeEditorJs()
    {
        Module = new FakeJsObject("module", _calls);
        Controller = new FakeJsObject("controller", _calls);
        Module.Results["create"] = Controller;
        Controller.Results["saveSelection"] = new AngryMonkey.CloudComponents.TextEditor.Models.CloudTextEditorSelectionState();
        Controller.Results["getHtml"] = string.Empty;
    }

    public FakeJsObject Module { get; }

    public FakeJsObject Controller { get; }

    /// <summary>Set to make the module import itself fail.</summary>
    public Exception? ImportFailure { get; set; }

    public IReadOnlyList<JsCall> Calls
    {
        get
        {
            lock (_calls)
                return [.. _calls];
        }
    }

    public IEnumerable<JsCall> ControllerCalls(string identifier) => Calls.Where(call => call.Target == "controller" && call.Identifier == identifier);

    public IEnumerable<string> ControllerCallNames => Calls.Where(call => call.Target == "controller").Select(call => call.Identifier);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        lock (_calls)
            _calls.Add(new("runtime", identifier, args ?? []));

        if (identifier == "import")
        {
            if (ImportFailure is not null)
                return ValueTask.FromException<TValue>(ImportFailure);

            if ((string?)args?[0] != ModulePath)
                return ValueTask.FromException<TValue>(new JSException($"Unexpected module '{args?[0]}'."));

            return new((TValue)(object)Module);
        }

        // Anything else the framework asks the runtime for (focus on an element reference, for example) is accepted and ignored.
        return new(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}

public sealed class FakeJsObject(string name, List<JsCall> calls) : IJSObjectReference
{
    public Dictionary<string, object?> Results { get; } = [];

    public Dictionary<string, Exception> Failures { get; } = [];

    public int DisposeCount { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        lock (calls)
            calls.Add(new(name, identifier, args ?? []));

        if (Failures.TryGetValue(identifier, out Exception? failure))
            return ValueTask.FromException<TValue>(failure);

        if (Results.TryGetValue(identifier, out object? result) && result is TValue typed)
            return new(typed);

        return new(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync()
    {
        DisposeCount++;

        lock (calls)
            calls.Add(new(name, "$dispose", []));

        if (Failures.TryGetValue("$dispose", out Exception? failure))
            return ValueTask.FromException(failure);

        return ValueTask.CompletedTask;
    }
}
