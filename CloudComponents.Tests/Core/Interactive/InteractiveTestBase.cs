using System.Globalization;
using AngryMonkey.CloudComponents;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Core.Interactive;

/// <summary>Common set-up: a recording JavaScript runtime and a fixed culture, so the tests do not depend on the machine they run on.</summary>
public abstract class InteractiveTestBase : BunitContext
{
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;

    protected InteractiveTestBase()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Js = new RecordingJs();
        Services.AddSingleton<IJSRuntime>(Js);
    }

    protected RecordingJs Js { get; }

    protected NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    /// <summary>An <see cref="EventCallback{T}"/> that appends every payload it receives to <paramref name="log"/>.</summary>
    protected EventCallback<T> Record<T>(List<T> log) => EventCallback.Factory.Create<T>(this, (Action<T>)log.Add);

    protected EventCallback Record(List<string> log, string entry) => EventCallback.Factory.Create(this, () => log.Add(entry));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }

        base.Dispose(disposing);
    }
}
