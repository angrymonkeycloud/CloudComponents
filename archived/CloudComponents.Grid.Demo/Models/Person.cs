using System.Globalization;

namespace CloudComponents.Grid.Demo.Models;

/// <summary>
/// Sample record used across the CloudGrid demo pages. Intentionally mixes value
/// types (string, int, decimal, DateOnly, bool) so column sorting can be exercised.
/// </summary>
public sealed class Person
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Department { get; init; }
    public required string Country { get; init; }
    public int Age { get; init; }
    public decimal Salary { get; init; }
    public DateOnly HireDate { get; init; }
    public bool IsActive { get; init; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Self-contained avatar rendered as an inline SVG data URI so the demo's
    /// image column works without any network access.
    /// </summary>
    public string AvatarUrl => BuildAvatar(Initials, ColorFor(FullName));

    private string Initials =>
        $"{char.ToUpperInvariant(FirstName[0])}{char.ToUpperInvariant(LastName[0])}";

    private static string ColorFor(string value)
    {
        // Deterministic pastel color derived from the name.
        int hash = value.Aggregate(17, (acc, c) => (acc * 31) + c);
        int hue = Math.Abs(hash) % 360;
        return $"hsl({hue.ToString(CultureInfo.InvariantCulture)}, 55%, 60%)";
    }

    private static string BuildAvatar(string initials, string color)
    {
        string svg =
            $"<svg xmlns='http://www.w3.org/2000/svg' width='40' height='40' viewBox='0 0 40 40'>" +
            $"<rect width='40' height='40' rx='20' fill='{color}'/>" +
            $"<text x='50%' y='50%' dy='.1em' fill='#fff' font-family='Segoe UI, sans-serif' " +
            $"font-size='16' font-weight='600' text-anchor='middle' dominant-baseline='middle'>{initials}</text>" +
            $"</svg>";

        return $"data:image/svg+xml,{Uri.EscapeDataString(svg)}";
    }
}
