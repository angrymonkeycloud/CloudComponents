using Microsoft.JSInterop;

namespace CloudComponents.Tests.Core.Interactive;

/// <summary>One call a component made into JavaScript. <see cref="Module"/> is the import path when the call went to an imported module.</summary>
public sealed record JsCall(string? Module, string Identifier, object?[] Args);

/// <summary>
/// A JavaScript runtime that records every call, lets a test decide the answer or make a call fail, and keeps track of which imported
/// modules were disposed and whether any were used afterwards.
/// </summary>
public sealed class RecordingJs : IJSRuntime
{
    private readonly object _gate = new();
    private readonly List<JsCall> _calls = [];
    private readonly List<RecordingJsModule> _modules = [];
    private readonly Dictionary<string, Exception> _failures = new(StringComparer.Ordinal);

    /// <summary>Gives the answer for a call; return <c>null</c> for the default (a void call, or an unconfigured one).</summary>
    public Func<JsCall, object?>? Responder { get; set; }

    /// <summary>The exception a module throws from <c>DisposeAsync</c>, when set.</summary>
    public Exception? ModuleDisposeFailure { get; set; }

    public JsCall[] Calls { get { lock (_gate) return [.. _calls]; } }

    public RecordingJsModule[] Modules { get { lock (_gate) return [.. _modules]; } }

    /// <summary>Calls to the runtime itself (not to an imported module) with this identifier.</summary>
    public JsCall[] RuntimeCalls(string identifier) => [.. Calls.Where(call => call.Module is null && call.Identifier == identifier)];

    /// <summary>Calls to any imported module with this identifier.</summary>
    public JsCall[] ModuleCalls(string identifier) => [.. Calls.Where(call => call.Module is not null && call.Identifier == identifier)];

    /// <summary>Makes every call with this identifier throw <paramref name="exception"/> (after being recorded).</summary>
    public void FailWith(string identifier, Exception exception)
    {
        lock (_gate)
            _failures[identifier] = exception;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => Invoke<TValue>(null, identifier, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        Invoke<TValue>(null, identifier, args);

    internal ValueTask<TValue> Invoke<TValue>(string? module, string identifier, object?[]? args)
    {
        JsCall call = new(module, identifier, args ?? []);
        Exception? failure;

        lock (_gate)
        {
            _calls.Add(call);
            _failures.TryGetValue(identifier, out failure);
        }

        if (failure is not null)
            return ValueTask.FromException<TValue>(failure);

        if (module is null && identifier == "import")
        {
            RecordingJsModule created = new(this, (string)call.Args[0]!);

            lock (_gate)
                _modules.Add(created);

            return ValueTask.FromResult((TValue)(object)created);
        }

        object? result = Responder?.Invoke(call);

        return result is null ? ValueTask.FromResult(default(TValue)!) : ValueTask.FromResult((TValue)result);
    }
}

public sealed class RecordingJsModule(RecordingJs runtime, string path) : IJSObjectReference
{
    public string Path { get; } = path;

    public bool IsDisposed { get; private set; }

    /// <summary>Identifiers of calls made after the module was disposed.</summary>
    public List<string> CallsAfterDispose { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (IsDisposed)
            CallsAfterDispose.Add(identifier);

        return runtime.Invoke<TValue>(Path, identifier, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;

        return runtime.ModuleDisposeFailure is { } failure ? ValueTask.FromException(failure) : ValueTask.CompletedTask;
    }
}
