using AngleSharp.Dom;
using Bunit;

namespace CloudComponents.Tests.Icons;

public class LogoTests : BunitContext
{
    private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderLogo(Type type, params (string Name, object Value)[] parameters) =>
        Render(IconCatalog.Fragment(type, parameters.ToDictionary(pair => pair.Name, pair => pair.Value)));

    public static IEnumerable<object[]> DefaultThemeLogos => IconCatalog.DefaultThemeLogoTheoryData;

    public static IEnumerable<object[]> StyledLogos => IconCatalog.StyledLogoTheoryData;

    [Fact]
    public void The_catalog_finds_both_kinds_of_logo()
    {
        Assert.NotEmpty(IconCatalog.DefaultThemeLogoTheoryData);
        Assert.NotEmpty(IconCatalog.StyledLogoTheoryData);
    }

    [Theory]
    [MemberData(nameof(DefaultThemeLogos))]
    public void Brand_logos_use_their_own_colours_unless_the_default_theme_is_requested(Type logo)
    {
        IReadOnlyList<IElement> plain = RenderLogo(logo).FindAll("[class*='amc-svg-fill']");
        IReadOnlyList<IElement> themed = RenderLogo(logo, ("UseDefaultTheme", true)).FindAll("[class*='amc-svg-fill']");

        Assert.NotEmpty(plain);
        Assert.DoesNotContain(plain, element => element.ClassList.Contains("_default"));
        Assert.Equal(plain.Count, themed.Count);
        Assert.All(themed, element => Assert.Contains("_default", element.ClassList));
    }

    [Theory]
    [MemberData(nameof(DefaultThemeLogos))]
    public void The_default_theme_flag_only_adds_a_class_and_never_changes_the_shapes(Type logo)
    {
        string[] plain = [.. RenderLogo(logo).FindAll("path, polygon, rect, circle").Select(element => element.GetAttribute("d") ?? element.GetAttribute("points") ?? element.OuterHtml)];
        string[] themed = [.. RenderLogo(logo, ("UseDefaultTheme", true)).FindAll("path, polygon, rect, circle").Select(element => element.GetAttribute("d") ?? element.GetAttribute("points") ?? element.OuterHtml)];

        Assert.NotEmpty(plain);
        Assert.Equal(plain, themed);
    }

    [Theory]
    [MemberData(nameof(StyledLogos))]
    public void Solid_logos_carry_no_inline_colour_style(Type logo)
    {
        foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(logo).Where(p => p["Style"].ToString() == "Solid"))
            Assert.True(
                Render(IconCatalog.Fragment(logo, parameters)).FindAll("style").Count == 0,
                IconCatalog.Describe(logo, parameters) + " emits a <style> although it asked for the solid (theme controlled) look");
    }

    [Theory]
    [MemberData(nameof(StyledLogos))]
    public void Coloured_logos_scope_their_colour_to_their_own_svg_id(Type logo)
    {
        foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(logo).Where(p => p["Style"].ToString() == "Colored"))
        {
            string name = IconCatalog.Describe(logo, parameters);
            var cut = Render(IconCatalog.Fragment(logo, parameters));
            string css = Assert.Single(cut.FindAll("style")).TextContent;
            string svgId = cut.Find("svg").Id ?? string.Empty;

            Assert.True(svgId.Length > 0, name + " has no svg id to scope its colour to");
            Assert.Contains($"#{svgId} ", css);
            Assert.Contains("fill:", css);
        }
    }

    [Theory]
    [MemberData(nameof(StyledLogos))]
    public void Two_coloured_logos_on_one_page_do_not_leak_colour_into_each_other(Type logo)
    {
        Dictionary<string, object> parameters = IconCatalog.HealthyCombinations(logo).First(p => p["Style"].ToString() == "Colored");

        var first = Render(IconCatalog.Fragment(logo, parameters));
        var second = Render(IconCatalog.Fragment(logo, parameters));

        Assert.NotEqual(first.Find("svg").Id, second.Find("svg").Id);
        Assert.NotEqual(first.Find("style").TextContent, second.Find("style").TextContent);
    }

    [Theory]
    [MemberData(nameof(StyledLogos))]
    public void Each_variant_draws_its_own_artwork(Type logo)
    {
        List<string> viewBoxes = [];
        List<string> variants = [];

        foreach (Dictionary<string, object> parameters in IconCatalog.HealthyCombinations(logo).Where(p => p["Style"].ToString() == "Solid"))
        {
            viewBoxes.Add(Render(IconCatalog.Fragment(logo, parameters)).Find("svg").GetAttribute("viewBox")!);
            variants.Add(parameters["Variant"].ToString()!);
        }

        // Symbol, wordmark and combination have different proportions, so each variant must differ from the others.
        // AMLogo only draws a wordmark and one stacked form, which every other variant falls back to.
        int expectedDistinct = logo.Name == "AMLogo" ? 2 : viewBoxes.Count;

        Assert.True(viewBoxes.Distinct().Count() == expectedDistinct,
            $"{logo.Name}: variants {string.Join(", ", variants)} produce viewBoxes {string.Join(" | ", viewBoxes)}");
    }

    [Fact]
    public void The_combination_variant_is_taller_than_the_wordmark_of_the_same_logo()
    {
        static double Height(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> cut) =>
            double.Parse(cut.Find("svg").GetAttribute("viewBox")!.Split(' ')[3], System.Globalization.CultureInfo.InvariantCulture);

        foreach (Type logo in new[] { typeof(CloudIcons.Logos.AMALogo), typeof(CloudIcons.Logos.AMCLogo), typeof(CloudIcons.Logos.AMSLogo) })
        {
            double wordmark = Height(RenderLogo(logo, ("Variant", CloudIcons.Logos.LogoVariants.Wordmark)));
            double combination = Height(RenderLogo(logo, ("Variant", CloudIcons.Logos.LogoVariants.Combination)));

            Assert.True(combination > wordmark, $"{logo.Name}: combination ({combination}) should be taller than the wordmark ({wordmark})");
        }
    }

    [Fact]
    public void Defaults_are_the_coloured_style_and_the_documented_variant()
    {
        Assert.Contains(RenderLogo(typeof(CloudIcons.Logos.AMCLogo)).FindAll("style"), style => style.TextContent.Contains("fill:"));
        Assert.Equal(
            RenderLogo(typeof(CloudIcons.Logos.AMCLogo), ("Variant", CloudIcons.Logos.LogoVariants.Symbol)).Find("svg").GetAttribute("viewBox"),
            RenderLogo(typeof(CloudIcons.Logos.AMCLogo)).Find("svg").GetAttribute("viewBox"));
    }

    [Fact]
    public void AngryMonkeyCloudLogo_is_a_plain_svg_with_no_parameters()
    {
        var cut = Render<CloudIcons.Logos.AngryMonkeyCloudLogo>();

        Assert.Equal("0 0 1046.7 208.9", cut.Find("svg").GetAttribute("viewBox"));
        Assert.Empty(cut.FindAll("style"));
    }
}
