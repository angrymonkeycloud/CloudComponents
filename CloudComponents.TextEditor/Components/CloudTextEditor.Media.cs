using System.Net;
using System.Text.RegularExpressions;
using AngryMonkey.CloudComponents.TextEditor.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.TextEditor.Components;

public partial class CloudTextEditor
{
    private string _linkUrl = string.Empty;
    private string _linkText = string.Empty;
    private bool _linkNewTab;

    private string _imageUrl = string.Empty;
    private string _imageAlt = string.Empty;
    private string _imageWidth = string.Empty;
    private bool _imageUploadMode;
    private bool _isUploading;

    private string _videoUrl = string.Empty;

    private string? _dialogError;

    [GeneratedRegex(@"(?:youtube\.com/(?:watch\?(?:.*&)?v=|shorts/|embed/)|youtu\.be/)([A-Za-z0-9_-]{6,15})", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubeRegex();

    [GeneratedRegex(@"vimeo\.com/(?:video/)?(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex VimeoRegex();

    // ----- Link -------------------------------------------------------------

    private async Task OpenLinkDialogAsync()
    {
        if (_commandsDisabled)
            return;

        CloudTextEditorSelectionState state = await _controller!.InvokeAsync<CloudTextEditorSelectionState>("saveSelection");

        _linkUrl = state.LinkUrl ?? string.Empty;
        _linkText = state.SelectedText;
        _linkNewTab = false;
        _dialogError = null;
        _activeDialog = EditorDialog.Link;
    }

    private async Task InsertLinkAsync()
    {
        if (_controller is null || string.IsNullOrWhiteSpace(_linkUrl))
            return;

        await _controller.InvokeVoidAsync("createLink", _linkUrl.Trim(), _linkText.Trim(), _linkNewTab);
        await CloseDialogAsync();
    }

    // ----- Image ------------------------------------------------------------

    private async Task OpenImageDialogAsync()
    {
        if (_commandsDisabled)
            return;

        await _controller!.InvokeVoidAsync("saveSelection");

        _imageUrl = string.Empty;
        _imageAlt = string.Empty;
        _imageWidth = string.Empty;
        _imageUploadMode = false;
        _isUploading = false;
        _dialogError = null;
        _activeDialog = EditorDialog.Image;
    }

    private async Task HandleImageFileAsync(InputFileChangeEventArgs args)
    {
        _dialogError = null;
        IBrowserFile file = args.File;

        if (file.Size > MaxUploadSize)
        {
            _dialogError = $"File is too large. Maximum size is {MaxUploadSize / (1024 * 1024)} MB.";
            return;
        }

        _isUploading = true;

        try
        {
            if (ImageUploadHandler is not null)
            {
                string? url = await ImageUploadHandler(new()
                {
                    FileName = file.Name,
                    ContentType = file.ContentType,
                    Size = file.Size,
                    OpenReadStream = () => file.OpenReadStream(MaxUploadSize)
                });

                if (string.IsNullOrWhiteSpace(url))
                    _dialogError = "Upload failed.";
                else
                    _imageUrl = url;
            }
            else
            {
                using MemoryStream memory = new();
                await file.OpenReadStream(MaxUploadSize).CopyToAsync(memory);

                string contentType = string.IsNullOrEmpty(file.ContentType) ? "image/png" : file.ContentType;
                _imageUrl = $"data:{contentType};base64,{Convert.ToBase64String(memory.ToArray())}";
            }
        }
        catch (Exception exception)
        {
            _dialogError = $"Upload failed: {exception.Message}";
        }
        finally
        {
            _isUploading = false;
        }
    }

    private async Task InsertImageAsync()
    {
        if (_controller is null || string.IsNullOrWhiteSpace(_imageUrl))
            return;

        string html = BuildImageHtml(_imageUrl.Trim(), _imageAlt.Trim(), _imageWidth.Trim());

        await _controller.InvokeVoidAsync("insertHtml", html);
        await CloseDialogAsync();
    }

    internal static string BuildImageHtml(string url, string alt, string width)
    {
        string encodedUrl = WebUtility.HtmlEncode(url);
        string encodedAlt = WebUtility.HtmlEncode(alt);
        string attributes = string.Empty;

        if (!string.IsNullOrEmpty(width))
        {
            attributes = int.TryParse(width, out int pixels)
                ? $" width=\"{pixels}\""
                : $" style=\"width:{WebUtility.HtmlEncode(width)}\"";
        }

        return $"<img src=\"{encodedUrl}\" alt=\"{encodedAlt}\"{attributes}>";
    }

    // ----- Video ------------------------------------------------------------

    private async Task OpenVideoDialogAsync()
    {
        if (_commandsDisabled)
            return;

        await _controller!.InvokeVoidAsync("saveSelection");

        _videoUrl = string.Empty;
        _dialogError = null;
        _activeDialog = EditorDialog.Video;
    }

    private async Task InsertVideoAsync()
    {
        if (_controller is null || string.IsNullOrWhiteSpace(_videoUrl))
            return;

        string? html = BuildVideoEmbedHtml(_videoUrl.Trim());

        if (html is null)
        {
            _dialogError = "Unsupported video URL. Use a YouTube or Vimeo link, or a direct video file URL.";
            return;
        }

        await _controller.InvokeVoidAsync("insertHtml", html + "<p><br></p>");
        await CloseDialogAsync();
    }

    /// <summary>
    /// Builds embed HTML for a video URL. Supports YouTube and Vimeo pages and
    /// direct video files. Returns <c>null</c> for unsupported URLs.
    /// </summary>
    internal static string? BuildVideoEmbedHtml(string url)
    {
        Match youTube = YouTubeRegex().Match(url);

        if (youTube.Success)
            return BuildIframeEmbed($"https://www.youtube-nocookie.com/embed/{youTube.Groups[1].Value}");

        Match vimeo = VimeoRegex().Match(url);

        if (vimeo.Success)
            return BuildIframeEmbed($"https://player.vimeo.com/video/{vimeo.Groups[1].Value}");

        string path = url.Split('?', 2)[0].Split('#', 2)[0];

        if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ogv", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
        {
            return $"<figure class=\"cloudeditor-embed\"><video controls src=\"{WebUtility.HtmlEncode(url)}\"></video></figure>";
        }

        return null;
    }

    private static string BuildIframeEmbed(string embedUrl) =>
        $"<figure class=\"cloudeditor-embed\"><iframe src=\"{WebUtility.HtmlEncode(embedUrl)}\" " +
        "allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" " +
        "allowfullscreen frameborder=\"0\" title=\"Embedded video\"></iframe></figure>";

    // ----- Shared -----------------------------------------------------------

    private Task CloseDialogAsync()
    {
        _activeDialog = EditorDialog.None;
        _dialogError = null;
        _isUploading = false;
        return Task.CompletedTask;
    }
}
