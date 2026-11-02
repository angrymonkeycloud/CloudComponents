using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>
/// Dispatches DOM events without waiting for every handler to finish. Almost every pointer event on the player ends in
/// ProgressiveDelay(), a fixed two second wait before the controls may hide again, and bUnit would otherwise wait for it
/// after each click. The handler of the clicked element has completed (and the player has rendered) by the time the
/// dispatch first has to wait, so the state can be asserted at once; a handler that throws is still reported.
/// </summary>
internal static class FireEvents
{
    public static Task FireClickAsync(this IElement element, MouseEventArgs args) => Observe(element.ClickAsync(args));

    public static Task FireMouseDownAsync(this IElement element, MouseEventArgs args) => Observe(element.MouseDownAsync(args));

    public static Task FireMouseMoveAsync(this IElement element, MouseEventArgs args) => Observe(element.MouseMoveAsync(args));

    public static Task FireMouseOutAsync(this IElement element, MouseEventArgs args) => Observe(element.MouseOutAsync(args));

    public static Task FireTouchStartAsync(this IElement element, TouchEventArgs args) => Observe(element.TouchStartAsync(args));

    public static Task FireChangeAsync(this IElement element, ChangeEventArgs args) => Observe(element.ChangeAsync(args));

    public static Task FireTriggerEventAsync(this IElement element, string eventName, EventArgs args) => Observe(element.TriggerEventAsync(eventName, args));

    private static async Task Observe(Task dispatch)
    {
        if (dispatch.IsCompleted)
        {
            await dispatch;
            return;
        }

        // Give the handler of the target element the chance to run to its first real wait, then surface a failure if it already faulted.
        await Task.Yield();

        if (dispatch.IsFaulted)
            await dispatch;
    }
}
