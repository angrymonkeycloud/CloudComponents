using System.Reflection;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Bunit.JSInterop;
using global::CloudComponents.VideoPlayer;
using Microsoft.AspNetCore.Components;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>
/// Shared setup for tests of the <see cref="Player"/> component: a loose JS runtime that answers the video-info call,
/// helpers that raise the browser events the player listens to, and access to the metadata members that are internal.
/// </summary>
public abstract class PlayerTestBase : BunitContext
{
    /// <summary>What the browser reports for the video element. Change it before rendering to test other sizes or lengths.</summary>
    protected VideoInfo BrowserVideoInfo { get; } = new() { Duration = 125, Width = 1920, Height = 1080 };

    protected PlayerTestBase()
    {
        // Several callbacks are raised from pool threads (Task.Run), which can be slow to start while other test classes run in parallel.
        DefaultWaitTimeout = TimeSpan.FromSeconds(8);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<VideoInfo>("amcVideoPlayerGetVideoInfo", _ => true).SetResult(BrowserVideoInfo);
    }

    protected static VideoPlayerMetadata NewMetadata(Action<VideoPlayerMetadata>? configure = null)
    {
        VideoPlayerMetadata metadata = new() { VideoUrl = "https://media.example/clip.mp4", Title = "Clip" };
        configure?.Invoke(metadata);
        return metadata;
    }

    /// <summary>Renders a player and waits until its initial JavaScript set-up finished (Ready or Error).</summary>
    protected IRenderedComponent<Player> RenderPlayer(VideoPlayerMetadata? metadata = null, Action<ComponentParameterCollectionBuilder<Player>>? configure = null)
    {
        metadata ??= NewMetadata();

        IRenderedComponent<Player> cut = Render<Player>(parameters =>
        {
            parameters.Add(p => p.Metadata, metadata);
            configure?.Invoke(parameters);
        });

        cut.WaitForState(() => metadata.VideoState is VideoStates.Ready or VideoStates.Error, TimeSpan.FromSeconds(5));
        return cut;
    }

    /// <summary>Raises a browser media event the way the registered JavaScript handler does: a JSON payload in the video's change event.</summary>
    protected static async Task RaiseAsync(IRenderedComponent<Player> cut, string eventName, object? state = null)
    {
        string json = JsonSerializer.Serialize(state is null ? new { name = eventName } : (object)new { name = eventName, state });
        await cut.Find("video").FireChangeAsync(new ChangeEventArgs { Value = json });
    }

    /// <summary>Raises <c>loadedmetadata</c> so the player has the video's duration and size.</summary>
    protected static Task LoadMetadataAsync(IRenderedComponent<Player> cut) => RaiseAsync(cut, "loadedmetadata");

    protected IEnumerable<JSRuntimeInvocation> Calls(string identifier) =>
        JSInterop.Invocations.Where(invocation => invocation.Identifier == identifier);

    protected static async Task ClickAsync(IElement element) => await element.FireClickAsync(new());

    protected static T GetInternal<T>(object target, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        object? value = type.GetProperty(name, flags) is { } property
            ? property.GetValue(target)
            : type.GetField(name, flags)?.GetValue(target) ?? throw new MissingMemberException(type.Name, name);

        return (T)value!;
    }

    protected static void SetInternal(object target, string name, object? value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        if (type.GetProperty(name, flags) is { } property)
            property.SetValue(target, value);
        else
            (type.GetField(name, flags) ?? throw new MissingMemberException(type.Name, name)).SetValue(target, value);
    }

    /// <summary>Raises <c>playing</c> then a <c>timeupdate</c>, as a real browser does: the class list only reflects a state change at the next event.</summary>
    protected static async Task PlayingAsync(IRenderedComponent<Player> cut)
    {
        await RaiseAsync(cut, "playing");
        await RaiseAsync(cut, "timeupdate", new { currentTime = 1 });
    }

    protected static string Classes(IRenderedComponent<Player> cut) => cut.Find(".amc-videoplayer").ClassName ?? string.Empty;
}
