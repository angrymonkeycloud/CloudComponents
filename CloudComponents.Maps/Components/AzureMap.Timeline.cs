using AngryMonkey.CloudComponents.Maps.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.Maps.Components;

/// <summary>
/// Tracking/history timeline surface of <see cref="AzureMap"/>.
/// Each timeline renders as a route line with direction arrows, clustered raw GPS
/// fixes that stack when zoomed out and break apart on zoom-in (or on clicking a
/// cluster), always-visible labeled places, and distinct start/end badges. Clicking
/// any point opens a details popup and raises <see cref="OnTimelinePointClick"/>.
/// </summary>
public partial class AzureMap
{
    private readonly Dictionary<string, MapTimeline> _timelinesById = [];
    private IReadOnlyList<MapTimeline>? _appliedTimelines;

    /// <summary>
    /// Declarative timelines to render. Assign a new list instance to update —
    /// the component diffs by reference and re-renders the timeline layers when
    /// it changes. For imperative control use <see cref="SetTimelinesAsync"/> /
    /// <see cref="ClearTimelinesAsync"/>.
    /// </summary>
    [Parameter] public IReadOnlyList<MapTimeline>? Timelines { get; set; }

    /// <summary>
    /// When <c>true</c> (default), the camera smoothly frames the full timeline
    /// whenever the timelines change.
    /// </summary>
    [Parameter] public bool FitToTimelines { get; set; } = true;

    /// <summary>Raised when the user clicks a timeline point on the map.</summary>
    [Parameter] public EventCallback<MapTimelinePointClickEventArgs> OnTimelinePointClick { get; set; }

    // ── Public imperative API ────────────────────────────────────────────────

    /// <summary>Replaces all rendered timelines.</summary>
    /// <param name="timelines">Timelines to render.</param>
    /// <param name="fitToBounds">Overrides <see cref="FitToTimelines"/> for this call.</param>
    public async Task SetTimelinesAsync(IEnumerable<MapTimeline> timelines, bool? fitToBounds = null)
    {
        var c = EnsureController();
        var list = timelines.ToList();

        _timelinesById.Clear();
        foreach (var t in list)
            _timelinesById[t.Id] = t;

        await c.InvokeVoidAsync("setTimelines", list, fitToBounds ?? FitToTimelines);
    }

    /// <summary>Removes all rendered timelines from the map.</summary>
    public async Task ClearTimelinesAsync()
    {
        if (_controller is null) return;
        _timelinesById.Clear();
        _appliedTimelines = null;
        await _controller.InvokeVoidAsync("clearTimelines");
    }

    // ── Parameter sync ───────────────────────────────────────────────────────

    /// <summary>
    /// Pushes <see cref="Timelines"/> to JS when the parameter changed (by reference).
    /// Called from <c>OnParametersSetAsync</c> and once when the map becomes ready.
    /// </summary>
    private async Task SyncTimelinesAsync()
    {
        if (_controller is null || !IsReady) return;
        if (ReferenceEquals(_appliedTimelines, Timelines)) return;

        _appliedTimelines = Timelines;

        _timelinesById.Clear();
        var list = Timelines ?? [];
        foreach (var t in list)
            _timelinesById[t.Id] = t;

        await _controller.InvokeVoidAsync("setTimelines", list, FitToTimelines);
    }

    // ── JS callbacks ─────────────────────────────────────────────────────────

    [JSInvokable]
    public async Task NotifyTimelinePointClickAsync(string timelineId, int pointIndex)
    {
        if (!_timelinesById.TryGetValue(timelineId, out var timeline)) return;
        if (pointIndex < 0 || pointIndex >= timeline.Points.Count) return;

        await OnTimelinePointClick.InvokeAsync(
            new MapTimelinePointClickEventArgs(timeline, timeline.Points[pointIndex], pointIndex));
    }
}
