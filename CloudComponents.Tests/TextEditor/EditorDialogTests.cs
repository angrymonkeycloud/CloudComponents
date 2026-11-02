using AngleSharp.Dom;
using AngryMonkey.CloudComponents.TextEditor.Components;
using AngryMonkey.CloudComponents.TextEditor.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CloudComponents.Tests.TextEditor;

/// <summary>The link, image and video dialogs.</summary>
public class EditorDialogTests : EditorTestBase
{
    private static IElement Dialog(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-dialog");

    private static IElement InsertButton(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-dialogfoot .cloudeditor-action:not(._secondary)");

    private static string DialogTitle(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-dialoghead span").TextContent.Trim();

    private static Task Type(IElement input, string text) => input.TriggerEventAsync("oninput", new ChangeEventArgs { Value = text });

    private static Task Change(IElement input, object value) => input.ChangeAsync(new ChangeEventArgs { Value = value });

    // ----- Shared behaviour ---------------------------------------------------------------

    [Theory]
    [InlineData("Insert link (Ctrl+K)", "Insert link")]
    [InlineData("Insert image", "Insert image")]
    [InlineData("Insert video", "Insert video")]
    public async Task Each_dialog_opens_with_its_title_and_remembers_the_selection(string button, string title)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));

        await Select(cut, button);

        Assert.Equal(title, DialogTitle(cut));
        Assert.Single(Js.ControllerCalls("saveSelection"));
        Assert.Single(cut.FindAll(".cloudeditor-overlay"));
    }

    [Theory]
    [InlineData("Insert link (Ctrl+K)")]
    [InlineData("Insert image")]
    [InlineData("Insert video")]
    public async Task A_dialog_closes_from_the_overlay_the_close_button_and_cancel(string button)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, button);
        await cut.Find(".cloudeditor-overlay").ClickAsync(new());
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));

        await Select(cut, button);
        await cut.Find(".cloudeditor-dialoghead button").ClickAsync(new());
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));

        await Select(cut, button);
        await cut.Find(".cloudeditor-dialogfoot .cloudeditor-action._secondary").ClickAsync(new());
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
        Assert.Empty(cut.FindAll(".cloudeditor-overlay"));
        Assert.Empty(Js.ControllerCalls("insertHtml"));
        Assert.Empty(Js.ControllerCalls("createLink"));
    }

    [Theory]
    [InlineData("Insert link (Ctrl+K)")]
    [InlineData("Insert image")]
    [InlineData("Insert video")]
    public async Task A_dialog_cannot_be_inserted_until_something_is_entered(string button)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, button);

        Assert.True(IsDisabled(InsertButton(cut)));
    }

    // ----- Link ---------------------------------------------------------------------------

    [Fact]
    public async Task The_link_dialog_is_prefilled_from_the_selection()
    {
        Js.Controller.Results["saveSelection"] = new CloudTextEditorSelectionState { LinkUrl = "https://old.example", SelectedText = "old text", Link = true };
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, "Insert link (Ctrl+K)");

        Assert.Equal("https://old.example", cut.Find("input[placeholder='https://example.com']").GetAttribute("value"));
        Assert.Equal("old text", cut.Find("input[placeholder='Link text (optional)']").GetAttribute("value"));
        Assert.False(IsDisabled(InsertButton(cut)));
        Assert.False(cut.Find("input[type='checkbox']").HasAttribute("checked"));
    }

    [Fact]
    public async Task Inserting_a_link_sends_the_trimmed_url_text_and_target_choice()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert link (Ctrl+K)");

        await Type(cut.Find("input[placeholder='https://example.com']"), "  https://example.com/page  ");
        await Change(cut.Find("input[placeholder='Link text (optional)']"), "  Example  ");
        await Change(cut.Find("input[type='checkbox']"), true);
        await InsertButton(cut).ClickAsync(new());

        object?[] arguments = OnlyCall("createLink");
        Assert.Equal(["https://example.com/page", "Example", true], arguments);
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
    }

    [Fact]
    public async Task A_link_opens_in_the_same_tab_by_default()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert link (Ctrl+K)");

        await Type(cut.Find("input[placeholder='https://example.com']"), "https://example.com");
        await InsertButton(cut).ClickAsync(new());

        Assert.Equal(false, OnlyCall("createLink")[2]);
        Assert.Equal(string.Empty, OnlyCall("createLink")[1]);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public async Task A_blank_link_url_cannot_be_inserted(string url)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert link (Ctrl+K)");

        await Type(cut.Find("input[placeholder='https://example.com']"), url);

        Assert.True(IsDisabled(InsertButton(cut)));
        Assert.Empty(Js.ControllerCalls("createLink"));
    }

    [Fact]
    public async Task The_link_shortcut_opens_the_link_dialog_with_the_current_selection()
    {
        Js.Controller.Results["saveSelection"] = new CloudTextEditorSelectionState { SelectedText = "chosen words" };
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.InvokeAsync(() => cut.Instance.HandleLinkShortcutAsync());

        Assert.Equal("Insert link", DialogTitle(cut));
        Assert.Equal("chosen words", cut.Find("input[placeholder='Link text (optional)']").GetAttribute("value"));
    }

    [Fact]
    public async Task The_link_shortcut_does_nothing_when_links_are_switched_off()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Toolbar, new CloudTextEditorToolbarOptions { Link = false }));

        await cut.InvokeAsync(() => cut.Instance.HandleLinkShortcutAsync());

        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
    }

    [Fact]
    public async Task Reopening_the_link_dialog_starts_from_the_new_selection_not_the_old_input()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert link (Ctrl+K)");
        await Type(cut.Find("input[placeholder='https://example.com']"), "https://typed.example");
        await cut.Find(".cloudeditor-dialogfoot .cloudeditor-action._secondary").ClickAsync(new());

        await Select(cut, "Insert link (Ctrl+K)");

        Assert.True(string.IsNullOrEmpty(cut.Find("input[placeholder='https://example.com']").GetAttribute("value")));
    }

    // ----- Image from a URL ---------------------------------------------------------------

    [Fact]
    public async Task The_image_dialog_starts_on_the_url_tab()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, "Insert image");

        Assert.Equal(["From URL"], cut.FindAll(".cloudeditor-tabs button._active").Select(button => button.TextContent));
        Assert.Single(cut.FindAll("input[placeholder='https://example.com/image.png']"));
        Assert.Empty(cut.FindAll("input[type='file']"));
    }

    [Fact]
    public async Task Inserting_an_image_by_url_sends_encoded_markup_and_closes()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert image");

        await Type(cut.Find("input[placeholder='https://example.com/image.png']"), " https://x.example/a.png ");
        await Change(cut.Find("input[placeholder='Description (recommended)']"), "A \"cat\"");
        await Change(cut.Find("input[placeholder='e.g. 400 or 50% (optional)']"), "300");
        await InsertButton(cut).ClickAsync(new());

        Assert.Equal("<img src=\"https://x.example/a.png\" alt=\"A &quot;cat&quot;\" width=\"300\">", OnlyCall("insertHtml")[0]);
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
    }

    [Fact]
    public async Task A_percentage_width_is_inserted_as_a_style()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert image");

        await Type(cut.Find("input[placeholder='https://example.com/image.png']"), "a.png");
        await Change(cut.Find("input[placeholder='e.g. 400 or 50% (optional)']"), "50%");
        await InsertButton(cut).ClickAsync(new());

        Assert.Equal("<img src=\"a.png\" alt=\"\" style=\"width:50%\">", OnlyCall("insertHtml")[0]);
    }

    [Fact]
    public async Task Reopening_the_image_dialog_forgets_the_previous_entries()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert image");
        await Type(cut.Find("input[placeholder='https://example.com/image.png']"), "a.png");
        await cut.Find(".cloudeditor-dialogfoot .cloudeditor-action._secondary").ClickAsync(new());

        await Select(cut, "Insert image");

        Assert.True(string.IsNullOrEmpty(cut.Find("input[placeholder='https://example.com/image.png']").GetAttribute("value")));
        Assert.True(IsDisabled(InsertButton(cut)));
    }

    // ----- Image upload -------------------------------------------------------------------

    private static InputFileChangeEventArgs Upload(string name, string contentType, byte[] content, long? reportedSize = null) =>
        new([new FakeBrowserFile(name, contentType, content, reportedSize)]);

    private async Task<IRenderedComponent<CloudTextEditor>> OpenUploadAsync(Action<ComponentParameterCollectionBuilder<CloudTextEditor>>? configure = null)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(configure);
        await Select(cut, "Insert image");
        await cut.FindAll(".cloudeditor-tabs button").Single(button => button.TextContent == "Upload").ClickAsync(new());
        return cut;
    }

    private static Task Choose(IRenderedComponent<CloudTextEditor> cut, InputFileChangeEventArgs files) =>
        cut.InvokeAsync(() => cut.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(files));

    [Fact]
    public async Task The_upload_tab_shows_a_file_picker_for_images()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync();

        Assert.Equal("image/*", cut.Find("input[type='file']").GetAttribute("accept"));
        Assert.Empty(cut.FindAll("input[placeholder='https://example.com/image.png']"));
        Assert.Equal(["Upload"], cut.FindAll(".cloudeditor-tabs button._active").Select(button => button.TextContent));
    }

    [Fact]
    public async Task Without_an_upload_handler_the_file_is_embedded_as_a_data_url()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync();

        await Choose(cut, Upload("photo.jpg", "image/jpeg", [1, 2, 3]));

        Assert.Contains("File ready to insert.", cut.Markup);
        Assert.False(IsDisabled(InsertButton(cut)));

        await InsertButton(cut).ClickAsync(new());

        Assert.Equal("<img src=\"data:image/jpeg;base64,AQID\" alt=\"\">", OnlyCall("insertHtml")[0]);
    }

    [Fact]
    public async Task A_file_without_a_content_type_is_embedded_as_png()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync();

        await Choose(cut, Upload("photo", "", [9]));
        await InsertButton(cut).ClickAsync(new());

        Assert.StartsWith("<img src=\"data:image/png;base64,", (string)OnlyCall("insertHtml")[0]!);
    }

    [Fact]
    public async Task The_upload_handler_receives_the_file_details_and_its_url_is_used()
    {
        CloudTextEditorFileUpload? received = null;
        byte[]? content = null;
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p.Add(x => x.ImageUploadHandler, upload =>
        {
            received = upload;
            using Stream stream = upload.OpenReadStream();
            using MemoryStream copy = new();
            stream.CopyTo(copy);
            content = copy.ToArray();
            return Task.FromResult<string?>("https://cdn.example/stored.png");
        }));

        await Choose(cut, Upload("cat.png", "image/png", [5, 6, 7, 8]));
        await InsertButton(cut).ClickAsync(new());

        Assert.NotNull(received);
        Assert.Equal(("cat.png", "image/png", 4L), (received.FileName, received.ContentType, received.Size));
        Assert.Equal([5, 6, 7, 8], content);
        Assert.Equal("<img src=\"https://cdn.example/stored.png\" alt=\"\">", OnlyCall("insertHtml")[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_handler_that_returns_no_url_cancels_the_insertion_with_a_message(string? url)
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p.Add(x => x.ImageUploadHandler, _ => Task.FromResult(url)));

        await Choose(cut, Upload("cat.png", "image/png", [1]));

        Assert.Equal("Upload failed.", cut.Find(".cloudeditor-message._error").TextContent);
        Assert.True(IsDisabled(InsertButton(cut)));
    }

    [Fact]
    public async Task A_handler_that_throws_shows_its_message_and_leaves_the_dialog_usable()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p.Add(x => x.ImageUploadHandler, _ => throw new InvalidOperationException("storage offline")));

        await Choose(cut, Upload("cat.png", "image/png", [1]));

        Assert.Equal("Upload failed: storage offline", cut.Find(".cloudeditor-message._error").TextContent);
        Assert.True(IsDisabled(InsertButton(cut)));
        Assert.DoesNotContain("Uploading", cut.Markup);
    }

    [Fact]
    public async Task A_failed_upload_is_cleared_when_a_good_file_is_chosen_next()
    {
        int calls = 0;
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p.Add(x => x.ImageUploadHandler, _ =>
            Task.FromResult<string?>(++calls == 1 ? null : "https://cdn.example/ok.png")));
        await Choose(cut, Upload("a.png", "image/png", [1]));

        await Choose(cut, Upload("b.png", "image/png", [1]));

        Assert.Empty(cut.FindAll(".cloudeditor-message._error"));
        Assert.False(IsDisabled(InsertButton(cut)));
    }

    [Fact]
    public async Task A_file_over_the_limit_is_rejected_without_calling_the_handler()
    {
        bool called = false;
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p
            .Add(x => x.MaxUploadSize, 2 * 1024 * 1024)
            .Add(x => x.ImageUploadHandler, _ => { called = true; return Task.FromResult<string?>("x"); }));

        await Choose(cut, Upload("big.png", "image/png", [1], reportedSize: 3 * 1024 * 1024));

        Assert.False(called);
        Assert.Equal("File is too large. Maximum size is 2 MB.", cut.Find(".cloudeditor-message._error").TextContent);
        Assert.True(IsDisabled(InsertButton(cut)));
    }

    [Fact]
    public async Task A_file_of_exactly_the_limit_is_accepted_and_one_byte_more_is_not()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p.Add(x => x.MaxUploadSize, 1024));

        await Choose(cut, Upload("exact.png", "image/png", [1], reportedSize: 1024));
        Assert.Empty(cut.FindAll(".cloudeditor-message._error"));
        Assert.False(IsDisabled(InsertButton(cut)));

        await Choose(cut, Upload("over.png", "image/png", [1], reportedSize: 1025));
        Assert.NotEmpty(cut.FindAll(".cloudeditor-message._error"));
    }

    [Fact(Skip = "BUG: the too-large message divides MaxUploadSize by 1 MiB with integer division, so a limit under 1 MB (or 1.5 MB) is reported as \"Maximum size is 0 MB\" (or \"1 MB\")")]
    public async Task The_size_limit_message_names_the_real_limit_for_sub_megabyte_limits()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync(p => p.Add(x => x.MaxUploadSize, 512 * 1024));

        await Choose(cut, Upload("big.png", "image/png", [1], reportedSize: 600 * 1024));

        Assert.DoesNotContain("0 MB", cut.Find(".cloudeditor-message._error").TextContent);
    }

    [Fact]
    public async Task Switching_back_to_the_url_tab_keeps_an_uploaded_file_ready()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenUploadAsync();
        await Choose(cut, Upload("a.png", "image/png", [1]));

        await cut.FindAll(".cloudeditor-tabs button").Single(button => button.TextContent == "From URL").ClickAsync(new());

        Assert.StartsWith("data:image/png;base64,", cut.Find("input[placeholder='https://example.com/image.png']").GetAttribute("value"));
    }

    // ----- Video --------------------------------------------------------------------------

    [Fact]
    public async Task Inserting_a_youtube_link_embeds_it_and_adds_a_paragraph_after_it()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert video");

        await Type(cut.Find("input[placeholder='YouTube, Vimeo or direct video file URL']"), "  https://youtu.be/dQw4w9WgXcQ ");
        await InsertButton(cut).ClickAsync(new());

        string html = (string)OnlyCall("insertHtml")[0]!;
        Assert.Contains("youtube-nocookie.com/embed/dQw4w9WgXcQ", html);
        Assert.EndsWith("</figure><p><br></p>", html);
        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
    }

    [Fact]
    public async Task An_unsupported_video_link_shows_a_message_and_keeps_the_dialog_open()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert video");

        await Type(cut.Find("input[placeholder='YouTube, Vimeo or direct video file URL']"), "https://example.com/not-a-video");
        await InsertButton(cut).ClickAsync(new());

        Assert.Equal("Unsupported video URL. Use a YouTube or Vimeo link, or a direct video file URL.", cut.Find(".cloudeditor-message._error").TextContent);
        Assert.Single(cut.FindAll(".cloudeditor-dialog"));
        Assert.Empty(Js.ControllerCalls("insertHtml"));
    }

    [Fact]
    public async Task The_video_error_goes_away_when_the_dialog_is_reopened()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Insert video");
        await Type(cut.Find("input[placeholder='YouTube, Vimeo or direct video file URL']"), "nope");
        await InsertButton(cut).ClickAsync(new());
        await cut.Find(".cloudeditor-dialogfoot .cloudeditor-action._secondary").ClickAsync(new());

        await Select(cut, "Insert video");

        Assert.Empty(cut.FindAll(".cloudeditor-message._error"));
    }

    // ----- Read-only ----------------------------------------------------------------------

    [Fact]
    public async Task A_read_only_editor_cannot_open_dialogs()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.ReadOnly, true));

        await Button(cut, "Insert image").ClickAsync(new());
        await Button(cut, "Insert video").ClickAsync(new());
        await cut.InvokeAsync(() => cut.Instance.HandleLinkShortcutAsync());

        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
    }

    private sealed class FakeBrowserFile(string name, string contentType, byte[] content, long? reportedSize) : IBrowserFile
    {
        public string Name => name;

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => reportedSize ?? content.Length;

        public string ContentType => contentType;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (Size > maxAllowedSize)
                throw new IOException($"Supplied file with size {Size} bytes exceeds the maximum of {maxAllowedSize} bytes.");

            return new MemoryStream(content);
        }
    }
}
