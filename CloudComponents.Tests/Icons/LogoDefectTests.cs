using Bunit;
using CloudIcons.Logos;

namespace CloudComponents.Tests.Icons;

/// <summary>Logo defects found while writing the tests. Each is skipped with the defect named; remove the Skip when the logo is fixed.</summary>
public class LogoDefectTests : BunitContext
{
    [Theory(Skip = "BUG: AMALogo/AMCLogo/AMELogo/AMSLogo declare Variant=Logotype (LogoVariants.Logotype) but their @switch has no Logotype case, so they render no svg (and a stray <style> when coloured)")]
    [InlineData(typeof(AMALogo))]
    [InlineData(typeof(AMCLogo))]
    [InlineData(typeof(AMELogo))]
    [InlineData(typeof(AMSLogo))]
    public void The_logotype_variant_renders_an_svg(Type logo)
    {
        var cut = Render(IconCatalog.Fragment(logo, new Dictionary<string, object> { ["Variant"] = LogoVariants.Logotype }));

        Assert.Single(cut.FindAll("svg"));
    }

    [Theory(Skip = "BUG: AMLogo's Logotype (its default), Symbol and Combination svg elements have no id, so the '#id .--svg-fill' colour style matches nothing and the default AMLogo is not coloured")]
    [InlineData(LogoVariants.Logotype)]
    [InlineData(LogoVariants.Symbol)]
    [InlineData(LogoVariants.Combination)]
    public void AMLogo_colour_style_matches_its_svg(LogoVariants variant)
    {
        var cut = Render<AMLogo>(p => p.Add(x => x.Variant, variant).Add(x => x.Style, LogoStyles.Colored));

        string svgId = cut.Find("svg").Id ?? string.Empty;

        Assert.NotEqual(string.Empty, svgId);
        Assert.Contains("#" + svgId, cut.Find("style").TextContent);
    }
}
