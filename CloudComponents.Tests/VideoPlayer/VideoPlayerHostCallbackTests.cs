using Bunit;
using global::CloudComponents.VideoPlayer;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>
/// Host callbacks that the README advertises ("exposes TimeUpdate, VideoEvents ...") and that are declared as component parameters.
/// </summary>
public class VideoPlayerHostCallbackTests : PlayerTestBase
{
    [Fact]
    public async Task The_on_event_callback_is_raised_for_time_updates()
    {
        List<VideoEvents> names = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.OnEvent, data => { names.Add(data.EventName); }));
        await LoadMetadataAsync(cut);
        names.Clear();

        await RaiseAsync(cut, "timeupdate", new { currentTime = 3 });

        Assert.Equal([VideoEvents.TimeUpdate], names);
    }

    [Fact(Skip = "BUG: OnVideoChange returns inside the TimeUpdate case when the duration is unknown (always the case for live streams, where duration is Infinity), before OnEvent is invoked, so a host never sees timeupdate events of a live stream")]
    public async Task Time_updates_reach_the_host_even_when_the_duration_is_unknown()
    {
        BrowserVideoInfo.Duration = null;
        List<VideoEvents> names = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.OnEvent, data => { names.Add(data.EventName); }));
        await LoadMetadataAsync(cut);
        names.Clear();

        await RaiseAsync(cut, "timeupdate", new { currentTime = 3 });

        Assert.Equal([VideoEvents.TimeUpdate], names);
    }

    [Fact(Skip = "BUG: the TimeUpdate (Action<VideoState>) parameter is declared and documented but nothing ever invokes it")]
    public async Task The_time_update_action_is_called_with_the_state()
    {
        List<VideoState> states = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.TimeUpdate, state => states.Add(state)));
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "timeupdate", new { currentTime = 3 });

        Assert.Equal(3, Assert.Single(states).CurrentTime);
    }

    [Fact(Skip = "BUG: the TimeUpdateEvent (EventCallback<VideoState>) parameter is declared but nothing ever invokes it")]
    public async Task The_time_update_event_callback_is_called_with_the_state()
    {
        List<VideoState> states = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.TimeUpdateEvent, state => { states.Add(state); }));
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "timeupdate", new { currentTime = 3 });

        Assert.Equal(3, Assert.Single(states).CurrentTime);
    }

    [Fact(Skip = "BUG: the EventFired (Action<VideoEventData>) parameter is declared but nothing ever invokes it")]
    public async Task The_event_fired_action_is_called_for_every_event()
    {
        List<VideoEvents> names = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.EventFired, data => names.Add(data.EventName)));
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "play");

        Assert.Equal([VideoEvents.Play], names);
    }
}
