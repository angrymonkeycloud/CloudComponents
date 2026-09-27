namespace AngryMonkey.CloudComponents.Maps.Models;

public sealed record MapRoute
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Color { get; init; } = "#2563eb";
    public IReadOnlyList<MapCoordinate> Points { get; init; } = [];
}
