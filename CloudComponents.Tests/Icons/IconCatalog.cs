using Microsoft.AspNetCore.Components;

namespace CloudComponents.Tests.Icons;

/// <summary>Finds every icon and logo component in the Icons assembly, so the data-driven tests cover new icons without being edited.</summary>
internal static class IconCatalog
{
    public static readonly Type[] AllComponents =
    [
        .. typeof(CloudIcons.Icons.CloseIcon).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsPublic && typeof(ComponentBase).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
    ];

    /// <summary>The logos that expose a boolean <c>UseDefaultTheme</c> parameter.</summary>
    public static IEnumerable<object[]> DefaultThemeLogoTheoryData => AllComponents
        .Where(type => type.GetProperty("UseDefaultTheme")?.PropertyType == typeof(bool))
        .Select(type => new object[] { type });

    /// <summary>The logos that expose Style and Variant enum parameters.</summary>
    public static IEnumerable<object[]> StyledLogoTheoryData => AllComponents
        .Where(type => EnumParameters(type).Any(parameter => parameter.Name == "Variant"))
        .Select(type => new object[] { type });

    /// <summary>The enum-typed parameters (Style, Variant) a component exposes, so every combination can be rendered.</summary>
    public static IReadOnlyList<(string Name, Type EnumType)> EnumParameters(Type component) =>
    [
        .. component.GetProperties()
            .Where(property => property.GetCustomAttributes(typeof(ParameterAttribute), true).Length > 0 && property.PropertyType.IsEnum)
            .Select(property => (property.Name, property.PropertyType))
    ];

    /// <summary>One parameter set per combination of the component's enum parameters (a single empty set when it has none).</summary>
    public static IReadOnlyList<Dictionary<string, object>> Combinations(Type component)
    {
        IEnumerable<Dictionary<string, object>> result = [new()];

        foreach ((string name, Type enumType) in EnumParameters(component))
            result = result.SelectMany(existing => Enum.GetValues(enumType).Cast<object>().Select(value =>
                new Dictionary<string, object>(existing) { [name] = value }));

        return result.ToList();
    }

    /// <summary>
    /// Combinations that are known to be broken. They are excluded from the data-driven tests and covered by
    /// skipped tests in <c>LogoDefectTests</c>, so one defect does not hide every other check on the same icon.
    /// </summary>
    public static bool IsKnownDefect(Type component, IReadOnlyDictionary<string, object> parameters)
    {
        string variant = parameters.TryGetValue("Variant", out object? value) ? value.ToString()! : string.Empty;

        // The AMA/AMC/AME/AMS logos have no Logotype case and render nothing for it.
        if (variant == "Logotype" && component.Name is "AMALogo" or "AMCLogo" or "AMELogo" or "AMSLogo")
            return true;

        // AMLogo's Logotype/Symbol/Combination svg has no id, so the scoped colour style never applies.
        if (component.Name == "AMLogo" && variant.Length > 0 && variant != "Wordmark" &&
            parameters.TryGetValue("Style", out object? style) && style.ToString() == "Colored")
            return true;

        return false;
    }

    public static IEnumerable<Dictionary<string, object>> HealthyCombinations(Type component) =>
        Combinations(component).Where(parameters => !IsKnownDefect(component, parameters));

    public static string Describe(Type component, IReadOnlyDictionary<string, object> parameters) =>
        parameters.Count == 0
            ? component.Name
            : $"{component.Name}({string.Join(", ", parameters.Select(pair => $"{pair.Key}={pair.Value}"))})";

    public static RenderFragment Fragment(Type component, IReadOnlyDictionary<string, object>? parameters = null) =>
        builder =>
        {
            builder.OpenComponent(0, component);

            int sequence = 1;

            foreach ((string name, object value) in parameters ?? new Dictionary<string, object>())
                builder.AddAttribute(sequence++, name, value);

            builder.CloseComponent();
        };
}
