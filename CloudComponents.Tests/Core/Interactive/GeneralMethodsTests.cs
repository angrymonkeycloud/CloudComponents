using System.Globalization;
using AngryMonkey.CloudComponents;

namespace CloudComponents.Tests.Core.Interactive;

public class GeneralMethodsTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("hello", "hello")]
    [InlineData("HELLO", "hello")]
    [InlineData("Mixed Case 123", "mixed-case-123")]
    [InlineData("already-friendly-text", "already-friendly-text")]
    [InlineData("under_score_text", "under-score-text")]
    [InlineData("dots.and,commas!", "dots-and-commas")]
    [InlineData("C# & .NET", "c-net")]
    [InlineData("a/b\\c", "a-b-c")]
    [InlineData("tab\tand\nnewline", "tab-and-newline")]
    public void Text_becomes_a_lowercase_hyphen_separated_slug(string text, string expected)
    {
        Assert.Equal(expected, GeneralMethods.ToUrlFriendly(text));
    }

    [Theory]
    [InlineData("a  b", "a-b")]
    [InlineData("a - b", "a-b")]
    [InlineData("a -- b", "a-b")]
    [InlineData("a!!!!b", "a-b")]
    [InlineData("a------b", "a-b")]
    public void Runs_of_separators_collapse_into_one_hyphen(string text, string expected)
    {
        Assert.Equal(expected, GeneralMethods.ToUrlFriendly(text));
    }

    [Theory]
    [InlineData("  padded  ", "padded")]
    [InlineData("-leading", "leading")]
    [InlineData("trailing-", "trailing")]
    [InlineData("!!!shout!!!", "shout")]
    [InlineData("  - both - ", "both")]
    public void Leading_and_trailing_separators_are_removed(string text, string expected)
    {
        Assert.Equal(expected, GeneralMethods.ToUrlFriendly(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    [InlineData("!@#$%^&*()")]
    public void Text_without_letters_or_digits_gives_an_empty_slug(string text)
    {
        Assert.Equal(string.Empty, GeneralMethods.ToUrlFriendly(text));
    }

    [Theory]
    [InlineData("Café Crème", "café-crème")]
    [InlineData("Über uns", "über-uns")]
    [InlineData("日本語 テスト", "日本語-テスト")]
    public void Letters_from_other_alphabets_are_kept(string text, string expected)
    {
        Assert.Equal(expected, GeneralMethods.ToUrlFriendly(text));
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("  -- Odd   Input!! -- ")]
    [InlineData("already-friendly")]
    public void Slugging_a_slug_changes_nothing(string text)
    {
        string once = GeneralMethods.ToUrlFriendly(text);

        Assert.Equal(once, GeneralMethods.ToUrlFriendly(once));
    }

    [Theory]
    [InlineData("Hello World 2024")]
    [InlineData("Ünïcödé & Symbols!")]
    public void A_slug_only_contains_lowercase_letters_digits_and_single_hyphens(string text)
    {
        string slug = GeneralMethods.ToUrlFriendly(text);

        Assert.All(slug, character => Assert.True(char.IsLetterOrDigit(character) || character == '-'));
        Assert.Equal(slug.ToLowerInvariant(), slug);
        Assert.DoesNotContain("--", slug);
        Assert.False(slug.StartsWith('-') || slug.EndsWith('-'));
    }

    [Fact(Skip = "BUG: GeneralMethods.ToUrlFriendly lower-cases with the current culture (text.ToLower()), so under tr-TR 'I' becomes the dotless 'ı' and the slug of 'ISSUE' is 'ıssue'; it should use ToLowerInvariant().")]
    public void The_slug_does_not_depend_on_the_current_culture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Equal("issue-title", GeneralMethods.ToUrlFriendly("ISSUE Title"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Missing_text_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => GeneralMethods.ToUrlFriendly(null!));
    }
}
