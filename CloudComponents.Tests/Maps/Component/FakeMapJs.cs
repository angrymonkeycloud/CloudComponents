using System.Reflection;
using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>
/// One call the component made to the map's JavaScript, with every argument as the browser would have received it
/// (serialised with the same options Blazor's JS runtime uses, so property names are the camelCase names JavaScript reads).
/// </summary>
public sealed class JsCall
{
    internal JsCall(string target, string method, object?[] args, JsonElement[] wire, int sequence)
    {
        Target = target;
        Method = method;
        Args = args;
        Wire = wire;
        Sequence = sequence;
    }

    /// <summary><c>module</c>, <c>controller</c> or <c>runtime</c>: the object the call went to.</summary>
    public string Target { get; }

    /// <summary>The JavaScript function name (<c>createMap</c>, <c>addMarker</c>, ...).</summary>
    public string Method { get; }

    /// <summary>The arguments as .NET objects (the real <c>DotNetObjectReference</c> for <c>createMap</c>).</summary>
    public object?[] Args { get; }

    /// <summary>The arguments as JSON, in call order. A <c>DotNetObjectReference</c> is JSON <c>null</c> here.</summary>
    public JsonElement[] Wire { get; }

    /// <summary>Position among every call the fake saw (module, controller and runtime), starting at 0.</summary>
    public int Sequence { get; }

    /// <summary>The JSON of one argument.</summary>
    public JsonElement Json(int index) => Wire[index];

    /// <summary>The JSON of every argument as one array, which is the easiest thing to compare against.</summary>
    public string ArgsJson => "[" + string.Join(",", Wire.Select(w => w.GetRawText())) + "]";

    public override string ToString() => $"{Target}.{Method}{ArgsJson}";
}

/// <summary>A call the test has told the fake to hold back until the test releases it.</summary>
public sealed class JsGate
{
    private readonly TaskCompletionSource<object?> _source = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>How many calls are currently waiting at this gate.</summary>
    public int Waiting { get; internal set; }

    /// <summary>Lets every waiting call (and every later call) return <paramref name="value"/>.</summary>
    public void Release(object? value = null) => _source.TrySetResult(value);

    /// <summary>Makes every waiting call fail with <paramref name="exception"/>.</summary>
    public void Fail(Exception exception) => _source.TrySetException(exception);

    internal Task<object?> Task => _source.Task;
}

/// <summary>
/// A scripted JavaScript object reference: the module (<c>import(...)</c>) or the controller <c>createMap</c> returns.
/// Records every call, answers from what the test scripted, and is not tied to bUnit's own JS interop.
/// </summary>
public class FakeJsObject : IJSObjectReference
{
    private readonly string _target;
    private readonly FakeMapJs _owner;
    private readonly List<JsCall> _calls = [];
    private readonly Dictionary<string, Func<JsCall, object?>> _handlers = [];
    private readonly Dictionary<string, JsGate> _gates = [];
    private readonly Dictionary<string, Func<JsCall, Task<object?>>> _asyncHandlers = [];

    internal FakeJsObject(string target, FakeMapJs owner)
    {
        _target = target;
        _owner = owner;
    }

    /// <summary>Every call made on this object, in order.</summary>
    public IReadOnlyList<JsCall> Calls => _calls;

    /// <summary>Calls made after <see cref="DisposeAsync"/> ran: after disposal the component must not talk to JavaScript.</summary>
    public List<JsCall> CallsAfterDisposal { get; } = [];

    /// <summary>How many times <see cref="DisposeAsync"/> ran.</summary>
    public int DisposeCount { get; private set; }

    public bool IsDisposed => DisposeCount > 0;

    /// <summary>When set, <see cref="DisposeAsync"/> throws this after recording the disposal.</summary>
    public Exception? DisposeFailure { get; set; }

    /// <summary>Every call to one JavaScript function.</summary>
    public IReadOnlyList<JsCall> CallsTo(string method) => [.. _calls.Where(c => c.Method == method)];

    /// <summary>The single call to a function; fails the test if it was called zero or several times.</summary>
    public JsCall Single(string method)
    {
        IReadOnlyList<JsCall> calls = CallsTo(method);
        Assert.True(calls.Count == 1, $"Expected exactly one call to {_target}.{method} but there were {calls.Count}. Calls: {Describe()}");
        return calls[0];
    }

    public bool WasCalled(string method) => _calls.Any(c => c.Method == method);

    /// <summary>The names of the calls made, in order: the quickest way to assert a sequence.</summary>
    public string[] Methods => [.. _calls.Select(c => c.Method)];

    public string Describe() => string.Join(" | ", _calls.Select(c => c.ToString()));

    /// <summary>Forgets the recorded calls (not the scripting), to assert only what a later step does.</summary>
    public void ClearCalls() => _calls.Clear();

    /// <summary>Scripts what a function returns. The value goes through the same JSON round trip a real result does.</summary>
    public FakeJsObject Returns(string method, object? value) => On(method, _ => value);

    /// <summary>Scripts a function with a handler that sees the call.</summary>
    public FakeJsObject On(string method, Func<JsCall, object?> handler)
    {
        _handlers[method] = handler;
        return this;
    }

    /// <summary>Scripts a function with a handler that completes later, so a test can finish calls in a chosen order.</summary>
    public FakeJsObject OnAsync(string method, Func<JsCall, Task<object?>> handler)
    {
        _asyncHandlers[method] = handler;
        return this;
    }

    /// <summary>Scripts a function to fail, as a JavaScript exception would.</summary>
    public FakeJsObject Throws(string method, Exception? exception = null)
    {
        _handlers[method] = _ => throw exception ?? new JSException("scripted failure of " + method);
        return this;
    }

    /// <summary>Makes calls to a function wait until the test releases the returned gate.</summary>
    public JsGate Hold(string method) => _gates[method] = new JsGate();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        JsCall call = _owner.Record(_target, identifier, args ?? []);

        if (IsDisposed)
            CallsAfterDisposal.Add(call);

        _calls.Add(call);

        cancellationToken.ThrowIfCancellationRequested();

        if (_gates.TryGetValue(identifier, out JsGate? gate))
        {
            gate.Waiting++;

            try
            {
                object? released = await gate.Task.WaitAsync(cancellationToken);
                return FakeMapJs.Convert<TValue>(_handlers.TryGetValue(identifier, out var afterGate) ? afterGate(call) : released);
            }
            finally
            {
                gate.Waiting--;
            }
        }

        if (_asyncHandlers.TryGetValue(identifier, out Func<JsCall, Task<object?>>? asyncHandler))
            return FakeMapJs.Convert<TValue>(await asyncHandler(call));

        object? value = _handlers.TryGetValue(identifier, out Func<JsCall, object?>? handler) ? handler(call) : DefaultAnswer(identifier);
        return FakeMapJs.Convert<TValue>(value);
    }

    protected virtual object? DefaultAnswer(string identifier) => null;

    public ValueTask DisposeAsync()
    {
        DisposeCount++;

        if (DisposeFailure is not null)
            throw DisposeFailure;

        return ValueTask.CompletedTask;
    }
}

/// <summary>The module <c>import("./_content/.../mapInterop.js")</c> resolves to.</summary>
public sealed class FakeJsModule : FakeJsObject
{
    private readonly FakeMapJs _map;

    internal FakeJsModule(FakeMapJs owner) : base("module", owner) => _map = owner;

    protected override object? DefaultAnswer(string identifier) => identifier == "createMap" ? _map.Controller : null;
}

/// <summary>
/// The whole JavaScript side of the map in one object. Register it as the <c>IJSRuntime</c>
/// (<see cref="AzureMapTestContext"/> does) and the component imports <see cref="Module"/>, calls <c>createMap</c>
/// and gets <see cref="Controller"/>. Tests script answers with <c>Controller.Returns("geocode", ...)</c> and read what was sent with
/// <c>Controller.CallsTo("addMarker")</c>; they raise the callbacks JavaScript would with <see cref="InvokeFromJsAsync"/>.
/// This is the C#/JS contract a different map provider's JavaScript has to honour.
/// </summary>
public sealed class FakeMapJs : IJSRuntime
{
    /// <summary>The options Blazor's JS runtime serialises with, so a test sees property names exactly as JavaScript does.</summary>
    public static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly List<JsCall> _all = [];
    private int _sequence;

    public FakeMapJs()
    {
        Module = new FakeJsModule(this);
        Controller = new FakeJsObject("controller", this);
    }

    public FakeJsModule Module { get; }

    public FakeJsObject Controller { get; }

    /// <summary>The paths passed to <c>import</c>.</summary>
    public List<string> Imports { get; } = [];

    /// <summary>Calls to the runtime itself other than <c>import</c> (for example <c>history.back</c>).</summary>
    public List<JsCall> RuntimeCalls { get; } = [];

    /// <summary>When set, <c>import</c> fails with this.</summary>
    public Exception? ImportFailure { get; set; }

    /// <summary>Every call, to every object, in the order they happened.</summary>
    public IReadOnlyList<JsCall> AllCalls => _all;

    /// <summary>
    /// Runs a callback on the component's renderer thread, as the browser's call into .NET would. Set by
    /// <see cref="AzureMapTestContext"/>; without it the callback runs inline.
    /// </summary>
    public Func<Func<Task>, Task>? Dispatch { get; set; }

    /// <summary>The component the first <c>createMap</c> call was given through its <c>DotNetObjectReference</c>.</summary>
    public AzureMap? Component =>
        Module.CallsTo("createMap").FirstOrDefault()?.Args.FirstOrDefault() is DotNetObjectReference<AzureMap> reference ? reference.Value : null;

    /// <summary>The options object (second argument) of the first <c>createMap</c> call.</summary>
    public JsonElement CreateMapOptions => Module.Single("createMap").Json(1);

    internal JsCall Record(string target, string method, object?[] args)
    {
        JsonElement[] wire = [.. args.Select(ToWire)];
        JsCall call = new(target, method, args, wire, _sequence++);
        _all.Add(call);
        return call;
    }

    private static JsonElement ToWire(object? value)
    {
        if (value is null || value.GetType().Name.StartsWith("DotNetObjectReference", StringComparison.Ordinal))
            return JsonSerializer.SerializeToElement<object?>(null);

        return JsonSerializer.SerializeToElement(value, value.GetType(), Wire);
    }

    /// <summary>Turns a scripted value into what the component asked for, through JSON as a real result travels.</summary>
    internal static TValue Convert<TValue>(object? value)
    {
        if (value is null || typeof(TValue).Name == "IJSVoidResult")
            return default!;

        if (value is IJSObjectReference && value is TValue same)
            return same;

        string json = value is JsonElement element ? element.GetRawText() : JsonSerializer.Serialize(value, value.GetType(), Wire);
        return JsonSerializer.Deserialize<TValue>(json, Wire)!;
    }

    /// <summary>Parses a JSON literal into a value to script, which keeps multi-field replies readable.</summary>
    public static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier == "import")
        {
            JsCall call = Record("runtime", identifier, args ?? []);
            Imports.Add((string)args![0]!);

            if (ImportFailure is not null)
                throw ImportFailure;

            return ValueTask.FromResult(Convert<TValue>(Module));
        }

        JsCall other = Record("runtime", identifier, args ?? []);
        RuntimeCalls.Add(other);
        return ValueTask.FromResult(default(TValue)!);
    }

    // ── JavaScript calling into .NET ─────────────────────────────────────────

    /// <summary>The [JSInvokable] methods of the component by the identifier JavaScript uses.</summary>
    public static IReadOnlyDictionary<string, MethodInfo> Invokables { get; } = typeof(AzureMap)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Select(m => (Method: m, Attribute: m.GetCustomAttribute<JSInvokableAttribute>()))
        .Where(x => x.Attribute is not null)
        .ToDictionary(x => x.Attribute!.Identifier ?? x.Method.Name, x => x.Method);

    /// <summary>
    /// Calls <c>invokeMethodAsync(name, ...args)</c> the way the browser does: arguments cross as JSON and are converted to the
    /// method's parameter types, and the call runs on the renderer's thread. Fails if no [JSInvokable] method has that name.
    /// </summary>
    public async Task<object?> InvokeFromJsAsync(string name, params object?[] args)
    {
        AzureMap component = Component ?? throw new InvalidOperationException("createMap has not been called yet.");

        if (!Invokables.TryGetValue(name, out MethodInfo? method))
            throw new Xunit.Sdk.XunitException($"AzureMap has no [JSInvokable] method named '{name}'. JavaScript calls would fail. Known: {string.Join(", ", Invokables.Keys)}");

        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal(parameters.Length, args.Length);

        object?[] converted = new object?[args.Length];
        for (int i = 0; i < args.Length; i++)
            converted[i] = JsonSerializer.Deserialize(JsonSerializer.Serialize(args[i], Wire), parameters[i].ParameterType, Wire);

        object? result = null;

        async Task Run()
        {
            object? returned = method.Invoke(component, converted);

            if (returned is Task task)
            {
                await task;
                result = task.GetType().IsGenericType ? task.GetType().GetProperty("Result")!.GetValue(task) : null;
            }
        }

        if (Dispatch is { } dispatch)
            await dispatch(Run);
        else
            await Run();

        return result;
    }
}
