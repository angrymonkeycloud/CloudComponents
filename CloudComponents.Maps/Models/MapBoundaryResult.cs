namespace AngryMonkey.CloudComponents.Maps.Models;

public sealed record MapBoundaryResult(bool Found, bool CanDisplay, double South, double West, double North, double East, string? Message = null);
