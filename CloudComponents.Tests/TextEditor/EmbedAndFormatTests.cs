using System.Reflection;
using AngryMonkey.CloudComponents.TextEditor.Components;

namespace CloudComponents.Tests.TextEditor;

/// <summary>
/// The markup builders behind the video, image and code-view features. They are internal static helpers of the
/// component, so they are reached by reflection; the dialog tests check the same output through the UI.
/// </summary>
public class EmbedAndFormatTests
{
    private static T Call<T>(string name, params object?[] arguments)
    {
        MethodInfo method = typeof(CloudTextEditor).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingMethodException(nameof(CloudTextEditor), name);

        return (T)method.Invoke(null, arguments)!;
    }

    private static string? Video(string url) => Call<string?>("BuildVideoEmbedHtml", url);

    private static string Image(string url, string alt, string width) => Call<string>("BuildImageHtml", url, alt, width);

    private static string Format(string html) => Call<string>("FormatHtml", html);

    // ----- Video --------------------------------------------------------------------------

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&t=30s", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=share&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/aB3_dE-fGh1", "aB3_dE-fGh1")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("HTTPS://WWW.YOUTUBE.COM/WATCH?V=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void YouTube_links_become_a_privacy_enhanced_iframe(string url, string id)
    {
        string? html = Video(url);

        Assert.NotNull(html);
        Assert.Contains($"src=\"https://www.youtube-nocookie.com/embed/{id}\"", html);
        Assert.StartsWith("<figure class=\"cloudeditor-embed\"><iframe ", html);
        Assert.EndsWith("</iframe></figure>", html);
        Assert.Contains("allowfullscreen", html);
        Assert.Contains("title=\"Embedded video\"", html);
    }

    [Theory]
    [InlineData("https://vimeo.com/76979871", "76979871")]
    [InlineData("https://player.vimeo.com/video/76979871", "76979871")]
    [InlineData("https://vimeo.com/video/76979871?h=abc", "76979871")]
    public void Vimeo_links_become_a_player_iframe(string url, string id)
    {
        string? html = Video(url);

        Assert.NotNull(html);
        Assert.Contains($"src=\"https://player.vimeo.com/video/{id}\"", html);
    }

    [Theory]
    [InlineData("https://cdn.example.com/clip.mp4")]
    [InlineData("https://cdn.example.com/clip.webm")]
    [InlineData("https://cdn.example.com/clip.ogg")]
    [InlineData("https://cdn.example.com/clip.ogv")]
    [InlineData("https://cdn.example.com/clip.mov")]
    [InlineData("https://cdn.example.com/CLIP.MP4")]
    [InlineData("https://cdn.example.com/clip.mp4?token=a.b")]
    [InlineData("https://cdn.example.com/clip.mp4#t=10")]
    [InlineData("/media/clip.mp4")]
    public void Direct_video_files_become_a_native_video_element(string url)
    {
        string? html = Video(url);

        Assert.Equal($"<figure class=\"cloudeditor-embed\"><video controls src=\"{System.Net.WebUtility.HtmlEncode(url)}\"></video></figure>", html);
    }

    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("https://example.com/clip.avi")]
    [InlineData("https://example.com/clip.mp4.txt")]
    [InlineData("https://example.com/page?file=clip.mp4")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://vimeo.com/notanumber")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public void Anything_else_is_not_embedded(string url)
    {
        Assert.Null(Video(url));
    }

    [Fact]
    public void A_file_url_is_attribute_encoded_so_it_cannot_break_out_of_the_src_attribute()
    {
        string? html = Video("https://x.example/a\"onload=\"alert(1).mp4");

        Assert.NotNull(html);
        Assert.DoesNotContain("src=\"https://x.example/a\"onload", html);
        Assert.Contains("&quot;", html);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "<video"));
    }

    [Fact]
    public void A_url_that_only_pretends_to_be_youtube_with_markup_in_the_id_is_not_embedded_with_that_markup()
    {
        string? html = Video("https://youtu.be/abcdef\"><script>alert(1)</script>");

        Assert.NotNull(html);
        Assert.DoesNotContain("<script", html);
        Assert.Contains("youtube-nocookie.com/embed/abcdef\"", html);
    }

    // ----- Image --------------------------------------------------------------------------

    [Fact]
    public void An_image_without_width_has_only_source_and_alt()
    {
        Assert.Equal("<img src=\"https://x.example/a.png\" alt=\"A cat\">", Image("https://x.example/a.png", "A cat", ""));
    }

    [Fact]
    public void A_pixel_width_becomes_the_width_attribute()
    {
        Assert.Equal("<img src=\"a.png\" alt=\"\" width=\"400\">", Image("a.png", "", "400"));
    }

    [Theory]
    [InlineData("50%")]
    [InlineData("20em")]
    [InlineData("12.5")]
    public void Any_other_width_becomes_an_inline_style(string width)
    {
        Assert.Equal($"<img src=\"a.png\" alt=\"\" style=\"width:{width}\">", Image("a.png", "", width));
    }

    [Fact]
    public void Url_alt_and_width_are_html_encoded()
    {
        string html = Image("a.png?x=1&y=\"2\"", "5 > 3 & \"quoted\" <b>", "\"50%");

        Assert.DoesNotContain("<b>", html);
        Assert.Contains("src=\"a.png?x=1&amp;y=&quot;2&quot;\"", html);
        Assert.Contains("alt=\"5 &gt; 3 &amp; &quot;quoted&quot; &lt;b&gt;\"", html);
        Assert.Contains("style=\"width:&quot;50%\"", html);
        Assert.Equal(1, html.Count(c => c == '<'));
    }

    // ----- Code view formatting -----------------------------------------------------------

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Nothing_to_format_gives_an_empty_string(string? html, string expected)
    {
        Assert.Equal(expected, Format(html!));
    }

    [Fact]
    public void Block_elements_are_put_on_their_own_lines()
    {
        Assert.Equal("<h1>T</h1>\n<p>a</p>\n<ul>\n<li>x</li>\n<li>y</li>\n</ul>\n<blockquote>q</blockquote>",
            Format("<h1>T</h1><p>a</p><ul><li>x</li><li>y</li></ul><blockquote>q</blockquote>"));
    }

    [Fact]
    public void Inline_markup_is_left_on_its_line()
    {
        string html = "<p>one <strong>two</strong><em>three</em> <a href=\"#\">four</a></p>";

        Assert.Equal(html, Format(html));
    }

    [Fact]
    public void Formatting_is_idempotent_for_already_formatted_source()
    {
        string once = Format("<p>a</p><p>b</p><hr><p>c</p>");

        Assert.Equal(once, Format(once));
    }

    [Fact]
    public void Formatting_only_adds_whitespace_between_tags()
    {
        string html = "<div><p>a</p><p>b</p></div><figure><img src=\"x\"></figure><table><tr><td>1</td></tr></table>";

        Assert.Equal(html, Format(html).Replace("\n", string.Empty));
    }
}
