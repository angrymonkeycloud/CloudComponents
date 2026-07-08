using System.Text.RegularExpressions;
using AngryMonkey.CloudComponents.Editor.Models;
using AngryMonkey.CloudComponents.Editor.Services;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.Editor.Components;

public partial class CloudEditor
{
    private bool _isCodeView;
    private string _codeViewText = string.Empty;
    private CloudEditorHtmlValidationResult? _codeValidation;

    [GeneratedRegex(@"(</(?:p|h[1-6]|ul|ol|li|blockquote|pre|figure|figcaption|table|thead|tbody|tfoot|tr|td|th|div)>)(<)", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBoundaryRegex();

    [GeneratedRegex(@"(>)(<(?:p|h[1-6]|ul|ol|li|blockquote|pre|figure|table|thead|tbody|tfoot|tr|hr|div)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex BlockOpenRegex();

    private async Task ToggleCodeViewAsync()
    {
        if (_controller is null || ReadOnly)
            return;

        if (_isCodeView)
        {
            if (_codeValidation?.IsValid == true)
                await ApplyCodeViewAsync();

            return;
        }

        _openColorPicker = null;

        string html = await _controller.InvokeAsync<string>("getHtml");

        _codeViewText = FormatHtml(html);
        _codeValidation = CloudEditorHtmlValidator.Validate(_codeViewText);
        _isCodeView = true;
    }

    private Task HandleCodeViewInputAsync()
    {
        _codeValidation = CloudEditorHtmlValidator.Validate(_codeViewText);
        return Task.CompletedTask;
    }

    private async Task ApplyCodeViewAsync()
    {
        if (_controller is null)
            return;

        _codeValidation = CloudEditorHtmlValidator.Validate(_codeViewText);

        if (!_codeValidation.IsValid)
            return;

        await _controller.InvokeVoidAsync("setHtml", _codeViewText);

        string html = await _controller.InvokeAsync<string>("getHtml");

        _isCodeView = false;
        await HandleContentChangedAsync(html);
    }

    private Task CancelCodeViewAsync()
    {
        _isCodeView = false;
        _codeValidation = null;
        return Task.CompletedTask;
    }

    /// <summary>Inserts line breaks between block-level elements for readable HTML source.</summary>
    internal static string FormatHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string result = BlockBoundaryRegex().Replace(html, "$1\n$2");
        return BlockOpenRegex().Replace(result, "$1\n$2");
    }
}
