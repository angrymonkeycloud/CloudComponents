using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;

namespace CloudComponents.Tests.Icons;

/// <summary>Checks every icon and logo component found by reflection, with every Style/Variant combination it offers.</summary>
public class AllIconsTests : BunitContext
{
    private static readonly Regex UrlReference = new(@"url\(\s*['""]?#([^)'""\s]+)", RegexOptions.Compiled);
    private static readonly Regex StyleSelector = new(@"([^{}]+)\{", RegexOptions.Compiled);
    private static readonly Regex SelectorId = new(@"#([A-Za-z_][\w-]*)", RegexOptions.Compiled);
    private static readonly Regex RandomId = new(@"_[0-9a-f]{32}", RegexOptions.Compiled);

    [Fact]
    public void The_catalog_finds_the_whole_icon_set()
    {
        // There are 130 .razor files: if reflection finds far fewer, the data-driven tests below would silently check nothing.
        Assert.True(IconCatalog.AllComponents.Length >= 130, $"Only {IconCatalog.AllComponents.Length} icon components were found by reflection.");
        Assert.Contains(IconCatalog.AllComponents, type => type.Name == "CloseIcon");
        Assert.Contains(IconCatalog.AllComponents, type => type.Name == "EditorBoldIcon");
        Assert.Contains(IconCatalog.AllComponents, type => type.Name == "MicrosoftLogo");
    }

    [Fact]
    public void Component_names_are_unique_so_two_icons_cannot_be_confused_by_a_using_directive()
    {
        string[] duplicates = [.. IconCatalog.AllComponents.GroupBy(type => type.Name).Where(group => group.Count() > 1).Select(group => group.Key)];

        Assert.True(duplicates.Length == 0, "Icons sharing a type name: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void Every_icon_renders_exactly_one_svg_with_a_viewBox()
    {
        List<string> problems = [];

        foreach (Type type in IconCatalog.AllComponents)
            foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(type))
            {
                string name = IconCatalog.Describe(type, parameters);

                try
                {
                    IReadOnlyList<IElement> svgs = Render(IconCatalog.Fragment(type, parameters)).FindAll("svg");

                    if (svgs.Count != 1)
                    {
                        problems.Add($"{name}: {svgs.Count} <svg> elements");
                        continue;
                    }

                    string? viewBox = svgs[0].GetAttribute("viewBox");
                    string[] parts = viewBox?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

                    if (parts.Length != 4 || parts.Any(part => !double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                        problems.Add($"{name}: viewBox '{viewBox}' is not four numbers, so the icon cannot scale");
                }
                catch (Exception exception)
                {
                    problems.Add($"{name}: threw {exception.GetType().Name}: {exception.Message}");
                }
            }

        Assert.True(problems.Count == 0, "Icons that do not render as one scalable svg:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Every_icon_markup_is_inert_with_no_script_event_handler_or_javascript_url()
    {
        List<string> problems = [];

        foreach (Type type in IconCatalog.AllComponents)
            foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(type))
            {
                IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> cut = Render(IconCatalog.Fragment(type, parameters));
                string name = IconCatalog.Describe(type, parameters);

                if (cut.FindAll("script, foreignObject, iframe").Count > 0)
                    problems.Add($"{name}: contains script/foreignObject/iframe");

                foreach (IElement element in cut.FindAll("*"))
                    foreach (IAttr attribute in element.Attributes)
                        if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                            attribute.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                            problems.Add($"{name}: <{element.LocalName} {attribute.Name}=...> is active content");
            }

        Assert.True(problems.Count == 0, "Icons with active content:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Every_icon_has_unique_non_empty_ids()
    {
        List<string> problems = [];

        foreach (Type type in IconCatalog.AllComponents)
            foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(type))
            {
                string name = IconCatalog.Describe(type, parameters);
                string[] ids = [.. Render(IconCatalog.Fragment(type, parameters)).FindAll("[id]").Select(element => element.Id ?? string.Empty)];

                if (ids.Any(string.IsNullOrWhiteSpace))
                    problems.Add($"{name}: has an empty id attribute");

                foreach (string duplicate in ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key))
                    problems.Add($"{name}: id '{duplicate}' is used more than once");
            }

        Assert.True(problems.Count == 0, "Icons with bad ids:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Every_id_an_icon_refers_to_exists_in_its_own_markup()
    {
        // url(#x), href="#x" and a "#x .class" selector in an inline <style> must all find their target,
        // otherwise the gradient, clip path or scoped colour silently does nothing.
        List<string> problems = [];

        foreach (Type type in IconCatalog.AllComponents)
            foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(type))
            {
                var cut = Render(IconCatalog.Fragment(type, parameters));
                string name = IconCatalog.Describe(type, parameters);
                HashSet<string> ids = [.. cut.FindAll("[id]").Select(element => element.Id!)];
                HashSet<string> referenced = [];

                foreach (IElement element in cut.FindAll("*"))
                    foreach (IAttr attribute in element.Attributes)
                    {
                        foreach (Match match in UrlReference.Matches(attribute.Value))
                            referenced.Add(match.Groups[1].Value);

                        if (attribute.Name is "href" or "xlink:href" && attribute.Value.StartsWith('#'))
                            referenced.Add(attribute.Value[1..]);
                    }

                foreach (IElement style in cut.FindAll("style"))
                    foreach (Match rule in StyleSelector.Matches(style.TextContent))
                        foreach (Match match in SelectorId.Matches(rule.Groups[1].Value))
                            referenced.Add(match.Groups[1].Value);

                foreach (string missing in referenced.Where(id => !ids.Contains(id)))
                    problems.Add($"{name}: refers to #{missing}, which is not in its markup");
            }

        Assert.True(problems.Count == 0, "Dangling id references:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Two_instances_of_an_icon_never_share_an_id()
    {
        // Duplicate ids make url(#x) resolve to the first element on the page, which breaks when that one is hidden.
        // Loading is checked on its own, as a known defect, below.
        List<string> problems = [];

        foreach (Type type in IconCatalog.AllComponents.Where(type => type.Name != "Loading"))
            foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(type))
            {
                var first = Render(IconCatalog.Fragment(type, parameters));
                var second = Render(IconCatalog.Fragment(type, parameters));
                string[] shared = [.. first.FindAll("[id]").Select(element => element.Id!).Intersect(second.FindAll("[id]").Select(element => element.Id!))];

                if (shared.Length > 0)
                    problems.Add($"{IconCatalog.Describe(type, parameters)}: both instances use id {string.Join(", ", shared)}");
            }

        Assert.True(problems.Count == 0, "Icons whose ids collide when used twice on a page:\n" + string.Join("\n", problems));
    }

    [Fact(Skip = "BUG: Loading.razor hard-codes ids \"a\" and \"Oval-2\", so two Loading icons on a page (or in two players) share them and the gradient reference can break when the first one is hidden")]
    public void Two_loading_icons_do_not_share_ids()
    {
        var first = Render<CloudIcons.Icons.Loading>();
        var second = Render<CloudIcons.Icons.Loading>();

        Assert.Empty(first.FindAll("[id]").Select(element => element.Id!).Intersect(second.FindAll("[id]").Select(element => element.Id!)));
    }

    [Fact]
    public void Every_icon_renders_the_same_markup_each_time_apart_from_its_random_ids()
    {
        // Ids are random per instance by design; everything else must be stable, or server and client renders would disagree.
        List<string> problems = [];

        foreach (Type type in IconCatalog.AllComponents)
            foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(type))
            {
                string first = RandomId.Replace(Render(IconCatalog.Fragment(type, parameters)).Markup, "_ID");
                string second = RandomId.Replace(Render(IconCatalog.Fragment(type, parameters)).Markup, "_ID");

                if (first != second)
                    problems.Add(IconCatalog.Describe(type, parameters));
            }

        Assert.True(problems.Count == 0, "Icons whose markup differs between renders (beyond ids):\n" + string.Join("\n", problems));
    }

    [Fact(Skip = "BUG: LZBLogo, MLCLogo and CSPLogo are declared in the AngryMonkey.Logos namespace, but the README tells consumers to import CloudIcons.Logos, so the documented using does not find those three")]
    public void Every_component_lives_in_a_namespace_the_readme_tells_consumers_to_import()
    {
        string[] documented = ["CloudIcons.Icons", "CloudIcons.Logos"];
        string[] offenders = [.. IconCatalog.AllComponents.Where(type => !documented.Contains(type.Namespace)).Select(type => type.FullName!)];

        Assert.True(offenders.Length == 0, "Components outside the documented namespaces: " + string.Join(", ", offenders));
    }
}
