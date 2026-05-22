namespace CloudComponents.Map.Options;

/// <summary>How a marker is added to / displayed on the map.</summary>
public enum MarkerAddTrigger
{
    /// <summary>User cannot add markers by clicking.</summary>
    Disabled,
    /// <summary>A single click on an empty area drops a marker.</summary>
    SingleClick,
    /// <summary>A double click on an empty area drops a marker (default).</summary>
    DoubleClick,
    /// <summary>
    /// A single pin is rendered fixed to the map's screen center. The map can
    /// still be panned/zoomed; the pin stays anchored visually and its
    /// coordinate updates as the map moves. Useful for "pick a location" UX.
    /// </summary>
    CenterPin
}

