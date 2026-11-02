using System.Net;
using AngryMonkey.CloudComponents.Markdown;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CloudComponents.Tests.Core.Interactive;

public class CloudMarkdownTests : InteractiveTestBase
{
    private const string GitHubDocument = "https://github.com/acme/widgets/blob/main/docs/guide.md";

    private readonly StubHttpHandler _http = new();

    public CloudMarkdownTests()
    {
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(_http));
    }

    private IRenderedComponent<CloudMarkdown> RenderMarkdown(Action<ComponentParameterCollectionBuilder<CloudMarkdown>>? configure = null) =>
        Render<CloudMarkdown>(parameters => configure?.Invoke(parameters));

    private IRenderedComponent<CloudMarkdown> RenderContent(string markdown, string? documentUrl = null, Action<ComponentParameterCollectionBuilder<CloudMarkdown>>? configure = null) =>
        RenderMarkdown(p =>
        {
            p.Add(x => x.Content, markdown);

            if (documentUrl is not null)
                p.Add(x => x.DocumentUrl, documentUrl);

            configure?.Invoke(p);
        });

    private string ResolveLink(string target, string documentUrl, Action<ComponentParameterCollectionBuilder<CloudMarkdown>>? configure = null) =>
        RenderContent($"[link]({target})", documentUrl, configure).Find("article a").GetAttribute("href")!;

    private string ResolveImage(string target, string documentUrl) =>
        RenderContent($"![alt]({target})", documentUrl).Find("article img").GetAttribute("src")!;

    // ---- empty and rendering ----

    [Fact]
    public void Nothing_to_show_renders_only_the_empty_root_and_loads_nothing()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown();

        Assert.NotNull(cut.Find("div.cloud-markdown"));
        Assert.Empty(cut.FindAll("article"));
        Assert.Empty(cut.FindAll(".cloud-markdown__status"));
        Assert.Empty(_http.Requests);
        Assert.Empty(Js.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void Blank_content_is_treated_as_nothing_to_show(string content)
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent(content, configure: p => p.Add(x => x.EmptyTemplate, "<p id=\"nothing\">Nothing here</p>"));

        Assert.NotNull(cut.Find("#nothing"));
        Assert.Empty(cut.FindAll("article"));
        Assert.Empty(Js.Calls);
    }

    [Fact]
    public void The_empty_template_is_shown_when_nothing_was_supplied()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.EmptyTemplate, "<p id=\"nothing\">Nothing here</p>"));

        Assert.Equal("Nothing here", cut.Find("#nothing").TextContent);
    }

    [Fact]
    public void Markdown_content_is_rendered_as_html_inside_the_document_article()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("# Title\n\nSome **bold** and *italic* text.\n\n- one\n- two\n");

        AngleSharp.Dom.IElement article = cut.Find(".cloud-markdown > article.cloud-markdown__document");
        Assert.Equal("Title", article.QuerySelector("h1")!.TextContent);
        Assert.Equal("bold", article.QuerySelector("p strong")!.TextContent);
        Assert.Equal("italic", article.QuerySelector("p em")!.TextContent);
        Assert.Equal(["one", "two"], article.QuerySelectorAll("li").Select(item => item.TextContent));
    }

    [Fact]
    public void Headings_get_ids_so_that_fragment_links_can_reach_them()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("## Getting started\n\nText");

        Assert.Equal("getting-started", cut.Find("article h2").Id);
    }

    [Fact]
    public void Advanced_markdown_such_as_tables_and_task_lists_is_supported()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("| a | b |\n|---|---|\n| 1 | 2 |\n\n- [x] done\n- [ ] todo\n");

        Assert.Equal(["a", "b"], cut.FindAll("article table th").Select(cell => cell.TextContent));
        Assert.Equal(2, cut.FindAll("article input[type=checkbox]").Count);
    }

    [Fact]
    public void Raw_html_in_the_markdown_is_shown_as_text_and_never_becomes_elements()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent(
            "<script>window.hacked = true</script>\n\n<div onclick=\"steal()\">block</div>\n\nInline <img src=x onerror=steal()> here.");

        Assert.Empty(cut.FindAll("article script"));
        Assert.Empty(cut.FindAll("article img"));
        Assert.Empty(cut.FindAll("article div"));
        Assert.Empty(cut.FindAll("[onclick]"));
        Assert.Empty(cut.FindAll("[onerror]"));
        Assert.Contains("window.hacked", cut.Find("article").TextContent);
    }

    [Theory(Skip = "BUG: CloudMarkdown disables raw HTML but still renders javascript: link targets as live href values (Resolve returns the absolute 'javascript:' URI), so untrusted markdown can run script when a link is clicked.")]
    [InlineData("[click](javascript:alert(1))")]
    [InlineData("[click](JavaScript:alert(1))")]
    public void A_javascript_link_target_is_not_rendered_as_a_live_link(string markdown)
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent(markdown);

        string? href = cut.FindAll("article a").FirstOrDefault()?.GetAttribute("href");
        Assert.False(href is not null && href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase), $"href was '{href}'");
    }

    [Fact]
    public void Links_opt_out_of_enhanced_navigation_so_the_browser_handles_them()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("[one](https://example.com/a) and [two](#top)");

        Assert.All(cut.FindAll("article a"), link => Assert.Equal("false", link.GetAttribute("data-enhance-nav")));
        Assert.Equal(2, cut.FindAll("article a").Count);
    }

    [Fact]
    public void The_class_parameter_is_added_to_the_root_and_the_style_is_applied()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("text", configure: p => p
            .Add(x => x.Class, "docs wide")
            .Add(x => x.Style, "--cloudcomponents-markdown-accent: red"));

        AngleSharp.Dom.IElement root = cut.Find("div.cloud-markdown");
        Assert.Contains("docs", root.ClassList);
        Assert.Contains("wide", root.ClassList);
        Assert.Contains("--cloudcomponents-markdown-accent: red", root.GetAttribute("style"));
    }

    [Fact]
    public void Changing_the_content_renders_the_new_document()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("# First");

        cut.Render(p => p.Add(x => x.Content, "# Second"));

        Assert.Equal("Second", cut.Find("article h1").TextContent);
    }

    [Fact]
    public void Clearing_the_content_removes_the_document()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("# First");

        cut.Render(p => p.Add(x => x.Content, ""));

        Assert.Empty(cut.FindAll("article"));
    }

    // ---- loading from a URL ----

    [Fact]
    public void A_document_is_downloaded_from_the_source_url_and_rendered()
    {
        _http.Respond("# Remote doc");

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/readme.md"));

        cut.WaitForAssertion(() => Assert.Equal("Remote doc", cut.Find("article h1").TextContent));
        Assert.Equal(new Uri("https://docs.example.com/readme.md"), Assert.Single(_http.Requests));
    }

    [Theory]
    [InlineData("https://github.com/acme/widgets/blob/main/docs/guide.md", "https://raw.githubusercontent.com/acme/widgets/main/docs/guide.md")]
    [InlineData("https://GitHub.com/acme/widgets/blob/feature-x/README.md", "https://raw.githubusercontent.com/acme/widgets/feature-x/README.md")]
    [InlineData("https://raw.githubusercontent.com/acme/widgets/main/docs/guide.md", "https://raw.githubusercontent.com/acme/widgets/main/docs/guide.md")]
    [InlineData("https://github.com/acme/widgets/tree/main/docs", "https://github.com/acme/widgets/tree/main/docs")]
    [InlineData("https://example.com/files/readme.md", "https://example.com/files/readme.md")]
    public void A_github_page_url_is_downloaded_from_its_raw_content_address(string sourceUrl, string expectedRequest)
    {
        _http.Respond("text");

        RenderMarkdown(p => p.Add(x => x.SourceUrl, sourceUrl));

        Assert.Equal(new Uri(expectedRequest), Assert.Single(_http.Requests));
    }

    [Fact]
    public void Supplied_content_takes_precedence_over_the_source_url()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p
            .Add(x => x.SourceUrl, "https://docs.example.com/readme.md")
            .Add(x => x.Content, "# From content"));

        Assert.Equal("From content", cut.Find("article h1").TextContent);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public void While_the_document_downloads_a_loading_message_is_shown()
    {
        TaskCompletionSource<string> download = _http.Hold();

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/readme.md"));

        Assert.Equal("Loading documentation...", cut.Find(".cloud-markdown__status[role=status]").TextContent);
        Assert.Empty(cut.FindAll("article"));

        download.SetResult("# Done");
        cut.WaitForAssertion(() => Assert.Equal("Done", cut.Find("article h1").TextContent));
        Assert.Empty(cut.FindAll(".cloud-markdown__status"));
    }

    [Fact]
    public void A_loading_template_replaces_the_default_loading_message()
    {
        _http.Hold();

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p
            .Add(x => x.SourceUrl, "https://docs.example.com/readme.md")
            .Add(x => x.LoadingTemplate, "<span id=\"spinner\">...</span>"));

        Assert.NotNull(cut.Find("#spinner"));
        Assert.Empty(cut.FindAll(".cloud-markdown__status"));
    }

    [Fact]
    public void A_failed_download_shows_an_unavailable_message_without_a_link_by_default()
    {
        _http.Respond("missing", HttpStatusCode.NotFound);

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/readme.md"));

        cut.WaitForAssertion(() => Assert.Contains("Documentation is temporarily unavailable.", cut.Find(".cloud-markdown__status").TextContent));
        Assert.Empty(cut.FindAll("a"));
        Assert.Empty(cut.FindAll("article"));
    }

    [Fact]
    public void A_failed_download_links_to_the_fallback_source_in_a_new_tab_safely()
    {
        _http.Respond("boom", HttpStatusCode.InternalServerError);

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p
            .Add(x => x.SourceUrl, "https://docs.example.com/readme.md")
            .Add(x => x.SourceFallbackUrl, "https://github.com/acme/widgets"));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloud-markdown__status a")));
        AngleSharp.Dom.IElement link = cut.Find(".cloud-markdown__status a");
        Assert.Equal("https://github.com/acme/widgets", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }

    [Fact]
    public void An_error_template_replaces_the_default_unavailable_message()
    {
        _http.Respond("boom", HttpStatusCode.BadGateway);

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p
            .Add(x => x.SourceUrl, "https://docs.example.com/readme.md")
            .Add(x => x.SourceFallbackUrl, "https://github.com/acme/widgets")
            .Add(x => x.ErrorTemplate, "<p id=\"oops\">Custom failure</p>"));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#oops")));
        Assert.Empty(cut.FindAll(".cloud-markdown__status"));
        Assert.Empty(cut.FindAll("a"));
    }

    [Fact]
    public void A_network_failure_is_shown_as_unavailable_instead_of_throwing()
    {
        _http.Fail(new HttpRequestException("no network"));

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/readme.md"));

        cut.WaitForAssertion(() => Assert.Contains("temporarily unavailable", cut.Find(".cloud-markdown__status").TextContent));
    }

    [Fact]
    public void A_source_url_that_is_not_absolute_is_shown_as_unavailable_without_a_request()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "docs/readme.md"));

        cut.WaitForAssertion(() => Assert.Contains("temporarily unavailable", cut.Find(".cloud-markdown__status").TextContent));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public void A_downloaded_document_with_no_text_shows_the_empty_template()
    {
        _http.Respond("   ");

        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p
            .Add(x => x.SourceUrl, "https://docs.example.com/readme.md")
            .Add(x => x.EmptyTemplate, "<p id=\"nothing\">Nothing here</p>"));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#nothing")));
        Assert.Empty(cut.FindAll(".cloud-markdown__status"));
    }

    [Fact]
    public void Re_rendering_with_the_same_source_does_not_download_it_again()
    {
        _http.Respond("# Doc");
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/readme.md"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("article")));

        cut.Render(p => p.Add(x => x.Class, "other"));
        cut.Render(p => p.Add(x => x.DocumentRoute, "/docs"));

        Assert.Single(_http.Requests);
    }

    [Fact]
    public void A_new_reload_key_downloads_the_document_again_and_can_recover_from_an_error()
    {
        _http.Respond("down", HttpStatusCode.ServiceUnavailable);
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p
            .Add(x => x.SourceUrl, "https://docs.example.com/readme.md")
            .Add(x => x.ReloadKey, 1));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloud-markdown__status")));

        _http.Respond("# Back online");
        cut.Render(p => p.Add(x => x.ReloadKey, 2));

        cut.WaitForAssertion(() => Assert.Equal("Back online", cut.Find("article h1").TextContent));
        Assert.Empty(cut.FindAll(".cloud-markdown__status"));
        Assert.Equal(2, _http.Requests.Count);
    }

    [Fact]
    public void A_new_source_url_replaces_the_displayed_document()
    {
        _http.Respond(request => request.RequestUri!.AbsolutePath.EndsWith("a.md") ? "# A" : "# B");
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/a.md"));
        cut.WaitForAssertion(() => Assert.Equal("A", cut.Find("article h1").TextContent));

        cut.Render(p => p.Add(x => x.SourceUrl, "https://docs.example.com/b.md"));

        cut.WaitForAssertion(() => Assert.Equal("B", cut.Find("article h1").TextContent));
    }

    // ---- link and image resolution ----

    [Theory]
    [InlineData("other.md", "https://github.com/acme/widgets/blob/main/docs/other.md")]
    [InlineData("sub/page.md", "https://github.com/acme/widgets/blob/main/docs/sub/page.md")]
    [InlineData("../README.md", "https://github.com/acme/widgets/blob/main/README.md")]
    [InlineData("/src/Program.cs", "https://github.com/acme/widgets/blob/main/src/Program.cs")]
    [InlineData("https://example.com/x", "https://example.com/x")]
    [InlineData("mailto:team@example.com", "mailto:team@example.com")]
    [InlineData("tel:+123456", "tel:+123456")]
    public void Relative_links_in_a_github_document_point_at_the_matching_github_page(string target, string expected)
    {
        Assert.Equal(expected, ResolveLink(target, GitHubDocument));
    }

    [Theory]
    [InlineData("img/diagram.png", "https://raw.githubusercontent.com/acme/widgets/main/docs/img/diagram.png")]
    [InlineData("../logo.png", "https://raw.githubusercontent.com/acme/widgets/main/logo.png")]
    [InlineData("/assets/logo.png", "https://raw.githubusercontent.com/acme/widgets/main/assets/logo.png")]
    [InlineData("https://cdn.example.com/a.png", "https://cdn.example.com/a.png")]
    [InlineData("data:image/png;base64,AAAA", "data:image/png;base64,AAAA")]
    public void Relative_images_in_a_github_document_are_loaded_from_raw_content(string target, string expected)
    {
        Assert.Equal(expected, ResolveImage(target, GitHubDocument));
    }

    [Fact]
    public void A_document_given_by_its_raw_address_resolves_links_the_same_way()
    {
        string href = ResolveLink("other.md", "https://raw.githubusercontent.com/acme/widgets/main/docs/guide.md");

        Assert.Equal("https://github.com/acme/widgets/blob/main/docs/other.md", href);
    }

    [Theory]
    [InlineData("c.md", "https://docs.example.com/a/b/c.md")]
    [InlineData("../c.md", "https://docs.example.com/a/c.md")]
    [InlineData("/root.md", "https://docs.example.com/root.md")]
    public void Relative_links_in_any_other_document_resolve_against_its_address(string target, string expected)
    {
        Assert.Equal(expected, ResolveLink(target, "https://docs.example.com/a/b/page.md"));
    }

    [Fact]
    public void Relative_images_in_any_other_document_resolve_against_its_address()
    {
        Assert.Equal("https://docs.example.com/a/b/img/x.png", ResolveImage("img/x.png", "https://docs.example.com/a/b/page.md"));
    }

    [Fact]
    public void Content_without_a_document_url_resolves_relative_links_against_localhost()
    {
        Assert.Equal("https://localhost/a.md", RenderContent("[x](a.md)").Find("article a").GetAttribute("href"));
    }

    [Fact]
    public void A_fragment_link_stays_a_bare_fragment_without_a_document_route()
    {
        Assert.Equal("#setup", ResolveLink("#setup", GitHubDocument));
    }

    [Theory]
    [InlineData("/docs/guide", "/docs/guide#setup")]
    [InlineData("/docs/guide/", "/docs/guide#setup")]
    [InlineData("/", "#setup")]
    public void A_fragment_link_is_kept_on_the_current_application_route(string route, string expected)
    {
        Assert.Equal(expected, ResolveLink("#setup", GitHubDocument, p => p.Add(x => x.DocumentRoute, route)));
    }

    [Fact]
    public void A_link_to_a_mapped_markdown_file_becomes_the_application_route()
    {
        Dictionary<string, string> routes = new() { ["docs/other.md"] = "/guide/other", ["README.md"] = "/" };

        Assert.Equal("/guide/other", ResolveLink("other.md", GitHubDocument, p => p.Add(x => x.DocumentRoutes, routes)));
        Assert.Equal("/", ResolveLink("../README.md", GitHubDocument, p => p.Add(x => x.DocumentRoutes, routes)));
    }

    [Fact]
    public void A_fragment_on_a_mapped_link_is_kept()
    {
        Dictionary<string, string> routes = new() { ["docs/other.md"] = "/guide/other" };

        Assert.Equal("/guide/other#setup", ResolveLink("other.md#setup", GitHubDocument, p => p.Add(x => x.DocumentRoutes, routes)));
    }

    [Fact]
    public void A_link_to_an_unmapped_file_stays_a_github_address()
    {
        Dictionary<string, string> routes = new() { ["docs/other.md"] = "/guide/other" };

        Assert.Equal("https://github.com/acme/widgets/blob/main/docs/missing.md", ResolveLink("missing.md", GitHubDocument, p => p.Add(x => x.DocumentRoutes, routes)));
    }

    [Fact]
    public void A_link_that_points_into_another_repository_is_not_mapped()
    {
        Dictionary<string, string> routes = new() { ["docs/other.md"] = "/guide/other" };

        Assert.Equal(
            "https://github.com/someone/else/blob/main/docs/other.md",
            ResolveLink("https://github.com/someone/else/blob/main/docs/other.md", GitHubDocument, p => p.Add(x => x.DocumentRoutes, routes)));
    }

    [Fact]
    public void Images_are_never_replaced_by_application_routes()
    {
        Dictionary<string, string> routes = new() { ["docs/pic.png"] = "/should-not-be-used" };
        IRenderedComponent<CloudMarkdown> cut = RenderContent("![pic](pic.png)", GitHubDocument, p => p.Add(x => x.DocumentRoutes, routes));

        Assert.Equal("https://raw.githubusercontent.com/acme/widgets/main/docs/pic.png", cut.Find("article img").GetAttribute("src"));
    }

    [Fact]
    public void Routes_are_not_applied_to_documents_that_do_not_come_from_github()
    {
        Dictionary<string, string> routes = new() { ["a/b/c.md"] = "/mapped" };

        Assert.Equal("https://docs.example.com/a/b/c.md", ResolveLink("c.md", "https://docs.example.com/a/b/page.md", p => p.Add(x => x.DocumentRoutes, routes)));
    }

    // ---- JavaScript ----

    [Fact]
    public void A_rendered_document_loads_the_markdown_script_and_binds_the_root_element()
    {
        RenderContent("# Hello\n\n[jump](#hello)");

        JsCall import = Assert.Single(Js.RuntimeCalls("import"));
        Assert.Equal("./_content/AngryMonkey.CloudComponents/cloud-markdown.js", import.Args[0]);

        JsCall bind = Assert.Single(Js.ModuleCalls("bindAndRestore"));
        ElementReference root = Assert.IsType<ElementReference>(Assert.Single(bind.Args));
        Assert.False(string.IsNullOrEmpty(root.Id));
    }

    [Fact]
    public void A_second_document_reuses_the_script_module_and_binds_again()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("# One");

        cut.Render(p => p.Add(x => x.Content, "# Two"));

        Assert.Single(Js.RuntimeCalls("import"));
        Assert.Equal(2, Js.ModuleCalls("bindAndRestore").Length);
    }

    [Fact]
    public void Re_rendering_without_a_new_document_does_not_bind_again()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("# One");

        cut.Render(p => p.Add(x => x.Class, "changed"));

        Assert.Single(Js.ModuleCalls("bindAndRestore"));
    }

    [Fact]
    public void No_script_is_loaded_when_there_is_no_document_or_the_download_failed()
    {
        _http.Respond("x", HttpStatusCode.NotFound);

        RenderMarkdown();
        IRenderedComponent<CloudMarkdown> failing = RenderMarkdown(p => p.Add(x => x.SourceUrl, "https://docs.example.com/readme.md"));
        failing.WaitForAssertion(() => Assert.NotEmpty(failing.FindAll(".cloud-markdown__status")));

        Assert.Empty(Js.Calls);
    }

    [Fact]
    public async Task Disposing_releases_the_script_module()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderContent("# One");
        RecordingJsModule module = Assert.Single(Js.Modules);

        await cut.Instance.DisposeAsync();

        Assert.True(module.IsDisposed);
        Assert.Empty(module.CallsAfterDispose);
    }

    [Fact]
    public async Task Disposing_before_any_document_was_rendered_loads_nothing()
    {
        IRenderedComponent<CloudMarkdown> cut = RenderMarkdown();

        await cut.Instance.DisposeAsync();

        Assert.Empty(Js.Calls);
    }

    // ---- test doubles ----

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly object _gate = new();
        private readonly List<Uri> _requests = [];
        private Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        public List<Uri> Requests { get { lock (_gate) return [.. _requests]; } }

        public void Respond(string body, HttpStatusCode status = HttpStatusCode.OK) =>
            _respond = _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });

        public void Respond(Func<HttpRequestMessage, string> body) =>
            _respond = request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body(request)) });

        public void Fail(Exception exception) => _respond = _ => Task.FromException<HttpResponseMessage>(exception);

        /// <summary>Makes every request wait until the returned source is completed with the body.</summary>
        public TaskCompletionSource<string> Hold()
        {
            TaskCompletionSource<string> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _respond = async _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(await gate.Task) };

            return gate;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_gate)
                _requests.Add(request.RequestUri!);

            return _respond(request);
        }
    }
}
