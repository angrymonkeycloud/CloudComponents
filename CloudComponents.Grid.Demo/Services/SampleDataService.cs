using CloudComponents.Grid.Demo.Models;
using CloudComponents.Grid.Models;

namespace CloudComponents.Grid.Demo.Services;

/// <summary>
/// In-memory, server-like data source for the demo. Generates a deterministic set of
/// <see cref="Person"/> records and applies search, sorting and paging exactly the way a
/// real backend would, so every <c>CloudGrid</c> feature can be exercised against it.
/// </summary>
public sealed class SampleDataService
{
    private static readonly string[] FirstNames =
    [
        "Olivia", "Liam", "Emma", "Noah", "Ava", "Ethan", "Sophia", "Mason",
        "Isabella", "Lucas", "Mia", "Logan", "Amelia", "James", "Harper",
        "Benjamin", "Evelyn", "Henry", "Abigail", "Alexander", "Ella", "Daniel",
        "Scarlett", "Matthew", "Grace", "Samuel", "Chloe", "David", "Zoe", "Joseph"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller",
        "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Wilson",
        "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee"
    ];

    private static readonly string[] Departments =
        ["Engineering", "Sales", "Marketing", "Finance", "Support", "Operations", "Design"];

    private static readonly string[] Countries =
        ["USA", "Canada", "UK", "Germany", "France", "Spain", "Italy", "Brazil", "Japan", "Australia"];

    private readonly List<Person> _people;

    /// <summary>Maps a column key to the value used when sorting on that column.</summary>
    private static readonly Dictionary<string, Func<Person, object?>> SortSelectors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = p => p.FullName,
            ["email"] = p => p.Email,
            ["department"] = p => p.Department,
            ["country"] = p => p.Country,
            ["age"] = p => p.Age,
            ["salary"] = p => p.Salary,
            ["hiredate"] = p => p.HireDate,
            ["active"] = p => p.IsActive
        };

    public SampleDataService()
    {
        _people = Generate(120);
    }

    /// <summary>Total number of records currently held.</summary>
    public int Count => _people.Count;

    /// <summary>A deterministic, fixed set of records (seeded RNG) so the demo is repeatable.</summary>
    private static List<Person> Generate(int count)
    {
        Random rng = new(20240517);
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        List<Person> people = new(count);

        for (int i = 0; i < count; i++)
        {
            string first = FirstNames[rng.Next(FirstNames.Length)];
            string last = LastNames[rng.Next(LastNames.Length)];
            string department = Departments[rng.Next(Departments.Length)];
            string country = Countries[rng.Next(Countries.Length)];
            int age = rng.Next(22, 64);
            decimal salary = rng.Next(45, 180) * 1000m;
            DateOnly hireDate = today.AddDays(-rng.Next(30, 3650));

            people.Add(new Person
            {
                FirstName = first,
                LastName = last,
                Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}{i}@example.com",
                Department = department,
                Country = country,
                Age = age,
                Salary = salary,
                HireDate = hireDate,
                IsActive = (i % 4) != 0
            });
        }

        return people;
    }

    /// <summary>
    /// Applies the request's search, sort and paging, then projects the page into a
    /// <see cref="CloudGridDataResult"/> using the supplied row factory. A small artificial
    /// delay simulates network latency so the grid's loading / searching states are visible.
    /// </summary>
    public async Task<CloudGridDataResult> QueryAsync(
        CloudGridDataRequest request,
        Func<Person, CloudGridRow> rowFactory,
        int simulatedDelayMs = 250)
    {
        if (simulatedDelayMs > 0)
            await Task.Delay(simulatedDelayMs);

        IEnumerable<Person> query = _people;

        // ── Search across the most relevant text fields ──────────────────────
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim();
            query = query.Where(p =>
                p.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Department.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Country.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // ── Sort the full result set (not just the loaded page) ──────────────
        if (request.Sort != null && SortSelectors.TryGetValue(request.Sort.Key, out Func<Person, object?>? selector))
        {
            query = request.Sort.Direction == CloudGridSortDirection.Ascending
                ? query.OrderBy(selector)
                : query.OrderByDescending(selector);
        }

        List<Person> filtered = query.ToList();
        int total = filtered.Count;

        // ── Page ─────────────────────────────────────────────────────────────
        int pageSize = request.PageSize <= 0 ? 30 : request.PageSize;
        List<Person> page = filtered
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new CloudGridDataResult
        {
            Page = request.Page,
            PageSize = pageSize,
            Total = total,
            Rows = page.Select(rowFactory).ToList()
        };
    }

    /// <summary>Returns the records for the given ids, preserving the supplied id order.</summary>
    public IReadOnlyList<Person> GetByIds(IEnumerable<Guid> ids)
    {
        Dictionary<Guid, Person> lookup = _people.ToDictionary(p => p.Id);
        return ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToList();
    }

    /// <summary>Persists a drag-reorder against the master list so it survives reloads.</summary>
    public void ApplyReorder(CloudGridRowReorder reorder)
    {
        int oldIndex = _people.FindIndex(p => p.Id == reorder.RecordId);
        if (oldIndex < 0)
            return;

        Person moved = _people[oldIndex];
        _people.RemoveAt(oldIndex);

        int newIndex = Math.Clamp(reorder.NewIndex, 0, _people.Count);
        _people.Insert(newIndex, moved);
    }
}
