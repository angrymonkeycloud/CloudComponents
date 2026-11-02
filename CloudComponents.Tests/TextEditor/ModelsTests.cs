using System.Text.Json;
using AngryMonkey.CloudComponents.TextEditor.Models;

namespace CloudComponents.Tests.TextEditor;

public class ModelsTests
{
    [Fact]
    public void Every_toolbar_feature_is_on_by_default()
    {
        CloudTextEditorToolbarOptions options = new();
        System.Reflection.PropertyInfo[] flags = [.. typeof(CloudTextEditorToolbarOptions).GetProperties().Where(property => property.PropertyType == typeof(bool))];

        Assert.Equal(19, flags.Length);
        Assert.All(flags, flag => Assert.True((bool)flag.GetValue(options)!, flag.Name + " should default to true"));
    }

    [Fact]
    public void Toolbar_features_can_be_switched_off_individually()
    {
        CloudTextEditorToolbarOptions options = new() { Video = false, Fullscreen = false };

        Assert.False(options.Video);
        Assert.False(options.Fullscreen);
        Assert.True(options.Image);
        Assert.True(options.CodeView);
    }

    [Fact]
    public void A_new_selection_state_describes_plain_left_aligned_paragraph_text()
    {
        CloudTextEditorSelectionState state = new();

        Assert.False(state.Bold || state.Italic || state.Underline || state.Strikethrough || state.Subscript || state.Superscript || state.InlineCode);
        Assert.False(state.UnorderedList || state.OrderedList || state.Link);
        Assert.Equal("p", state.BlockTag);
        Assert.Equal("left", state.Alignment);
        Assert.Null(state.LinkUrl);
        Assert.Equal(string.Empty, state.SelectedText);
    }

    [Fact]
    public void The_selection_state_is_read_from_the_camel_cased_object_the_script_sends()
    {
        const string json = """
            {"bold":true,"italic":true,"underline":true,"strikethrough":true,"subscript":true,"superscript":true,"inlineCode":true,
             "blockTag":"h2","alignment":"justify","unorderedList":true,"orderedList":true,"link":true,"linkUrl":"https://example.com","selectedText":"chosen"}
            """;

        CloudTextEditorSelectionState? state = JsonSerializer.Deserialize<CloudTextEditorSelectionState>(json);

        Assert.NotNull(state);
        Assert.True(state.Bold && state.Italic && state.Underline && state.Strikethrough && state.Subscript && state.Superscript && state.InlineCode);
        Assert.True(state.UnorderedList && state.OrderedList && state.Link);
        Assert.Equal("h2", state.BlockTag);
        Assert.Equal("justify", state.Alignment);
        Assert.Equal("https://example.com", state.LinkUrl);
        Assert.Equal("chosen", state.SelectedText);
    }

    [Fact]
    public void A_selection_state_with_a_null_link_url_is_accepted()
    {
        CloudTextEditorSelectionState? state = JsonSerializer.Deserialize<CloudTextEditorSelectionState>("""{"link":false,"linkUrl":null}""");

        Assert.NotNull(state);
        Assert.Null(state.LinkUrl);
    }

    [Fact]
    public void The_selection_state_serialises_with_the_names_the_script_uses()
    {
        string json = JsonSerializer.Serialize(new CloudTextEditorSelectionState { Bold = true, BlockTag = "pre" });

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.GetProperty("bold").GetBoolean());
        Assert.Equal("pre", document.RootElement.GetProperty("blockTag").GetString());
        Assert.True(document.RootElement.TryGetProperty("inlineCode", out _));
        Assert.True(document.RootElement.TryGetProperty("selectedText", out _));
    }

    [Fact]
    public async Task A_file_upload_exposes_its_details_and_opens_its_content_on_demand()
    {
        int opened = 0;
        CloudTextEditorFileUpload upload = new()
        {
            FileName = "photo.png",
            ContentType = "image/png",
            Size = 3,
            OpenReadStream = () => { opened++; return new MemoryStream([1, 2, 3]); }
        };

        Assert.Equal(0, opened);

        await using Stream stream = upload.OpenReadStream();
        using MemoryStream copy = new();
        await stream.CopyToAsync(copy);

        Assert.Equal(1, opened);
        Assert.Equal([1, 2, 3], copy.ToArray());
        Assert.Equal("photo.png", upload.FileName);
        Assert.Equal("image/png", upload.ContentType);
        Assert.Equal(3, upload.Size);
    }
}
