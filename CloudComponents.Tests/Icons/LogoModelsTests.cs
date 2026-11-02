using CloudIcons.Logos;

namespace CloudComponents.Tests.Icons;

public class LogoModelsTests
{
    [Fact]
    public void LogoStyles_offers_solid_and_colored_in_that_order()
    {
        Assert.Equal(["Solid", "Colored"], Enum.GetNames<LogoStyles>());
        Assert.Equal(0, (int)LogoStyles.Solid);
        Assert.Equal(1, (int)LogoStyles.Colored);
    }

    [Fact]
    public void LogoVariants_offers_the_four_logo_forms_in_a_stable_order()
    {
        // The numeric values may be persisted by host apps, so reordering would silently change a stored logo.
        Assert.Equal(["Wordmark", "Logotype", "Symbol", "Combination"], Enum.GetNames<LogoVariants>());
        Assert.Equal([0, 1, 2, 3], Enum.GetValues<LogoVariants>().Select(value => (int)value));
    }

    [Theory]
    [InlineData("Wordmark", LogoVariants.Wordmark)]
    [InlineData("symbol", LogoVariants.Symbol)]
    [InlineData("COMBINATION", LogoVariants.Combination)]
    public void LogoVariants_parses_from_text_ignoring_case(string text, LogoVariants expected)
    {
        Assert.Equal(expected, Enum.Parse<LogoVariants>(text, ignoreCase: true));
    }

    [Theory]
    [InlineData("Mono")]
    [InlineData("")]
    public void LogoVariants_rejects_unknown_names(string text)
    {
        Assert.False(Enum.TryParse(text, out LogoVariants _));
        Assert.False(Enum.IsDefined(typeof(LogoVariants), text));
    }

    [Fact]
    public void Logos_with_their_own_nested_style_and_variant_enums_offer_the_same_choices_as_the_shared_models()
    {
        // LZBLogo, MLCLogo and CSPLogo (and Expand) declare nested copies of the enums; a host switching between
        // logos with the same names should not meet a style that exists on one and not the other.
        foreach (Type logo in new[] { typeof(AngryMonkey.Logos.CSPLogo), typeof(AngryMonkey.Logos.LZBLogo), typeof(AngryMonkey.Logos.MLCLogo) })
        {
            Type styles = logo.GetNestedType("Styles")!;
            Type variants = logo.GetNestedType("Variants")!;

            Assert.Equal(Enum.GetNames<LogoStyles>(), Enum.GetNames(styles));
            Assert.Equal(Enum.GetNames<LogoVariants>().Where(name => name != "Logotype"), Enum.GetNames(variants).OrderBy(name => Array.IndexOf(Enum.GetNames<LogoVariants>(), name)));
        }
    }
}
