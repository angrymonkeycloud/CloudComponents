using System.Text.RegularExpressions;
using AngryMonkey.CloudComponents.TextEditor.Models;
using AngryMonkey.CloudComponents.TextEditor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.TextEditor.Components;

public partial class CloudTextEditor
{
    private bool _isCodeView;
    private string _codeViewText = string.Empty;
    private CloudTextEditorHtmlValidationResult? _codeValidation;
    private ElementReference _codeViewElement;
    private bool _focusCodeView;
    private bool _focusDesignView;
    private bool _showCodeIssues;

    private int CodeIssueCount => _codeValidation?.Messages.Count ?? 0;

    private CloudTextEditorHtmlValidationMessage? FirstCodeIssue =>
        _codeValidation?.Errors.FirstOrDefault() ?? _codeValidation?.Warnings.FirstOrDefault();

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
        _codeValidation = CloudTextEditorHtmlValidator.Validate(_codeViewText);
        _isCodeView = true;
        _focusCodeView = true;
    }

    private Task HandleCodeViewInputAsync()
    {
        _codeValidation = CloudTextEditorHtmlValidator.Validate(_codeViewText);

        if (CodeIssueCount == 0)
            _showCodeIssues = false;

        return Task.CompletedTask;
    }

    private void ToggleCodeIssues() => _showCodeIssues = !_showCodeIssues;

    private static string GetCodeIssueClass(CloudTextEditorHtmlValidationMessage message) =>
        message.Severity == CloudTextEditorValidationSeverity.Error ? "_error" : "_warning";

    private async Task HandleCodeViewKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.CtrlKey && args.Key == "Enter" && _codeValidation?.IsValid == true)
            await ApplyCodeViewAsync();
        else if (args.Key == "Escape")
            await CancelCodeViewAsync();
    }

    private async Task ApplyCodeViewAsync()
    {
        if (_controller is null)
            return;

        _codeValidation = CloudTextEditorHtmlValidator.Validate(_codeViewText);

        if (!_codeValidation.IsValid)
            return;

        await _controller.InvokeVoidAsync("setHtml", _codeViewText);

        string html = await _controller.InvokeAsync<string>("getHtml");

        _isCodeView = false;
        _codeValidation = null;
        _showCodeIssues = false;
        _focusDesignView = true;
        await HandleContentChangedAsync(html);
    }

    private Task CancelCodeViewAsync()
    {
        _isCodeView = false;
        _codeValidation = null;
        _showCodeIssues = false;
        _focusDesignView = true;
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
