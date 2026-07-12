using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.TextEditor.Components;

public partial class CloudTextEditor
{
    private static readonly string[] ColorPalette =
    [
        "#000000", "#434343", "#666666", "#999999", "#cccccc", "#ffffff",
        "#e81123", "#ff8c00", "#ffd700", "#107c10", "#0078d4", "#5c2d91",
        "#f7cad2", "#ffe0b3", "#fff3b3", "#c8e6c9", "#cce4f7", "#e0d3ef"
    ];

    private string? _openColorPicker;

    private bool _commandsDisabled => ReadOnly || _isCodeView || _controller is null;

    private IEnumerable<(string Value, string Label)> BlockFormatChoices
    {
        get
        {
            yield return ("p", "Paragraph");

            int firstLevel = Math.Clamp(FirstHeadingLevel, 1, 6);
            int count = Math.Clamp(HeadingCount, 1, 6 - firstLevel + 1);

            for (int index = 0; index < count; index++)
                yield return ($"h{firstLevel + index}", $"Heading {index + 1}");
        }
    }

    private string CurrentBlockValue
    {
        get
        {
            string blockTag = _selectionState.BlockTag;
            return BlockFormatChoices.Any(choice => choice.Value == blockTag) ? blockTag : "p";
        }
    }

    private async Task ExecAsync(string command)
    {
        if (_commandsDisabled)
            return;

        _openColorPicker = null;
        await _controller!.InvokeVoidAsync("exec", command);
    }

    private Task UndoAsync() => ExecAsync("undo");

    private Task RedoAsync() => ExecAsync("redo");

    private Task ClearFormattingAsync() => ExecAsync("removeFormat");

    private async Task HandleBlockFormatChangedAsync(ChangeEventArgs args)
    {
        if (_commandsDisabled)
            return;

        string? tag = args.Value?.ToString();

        if (!string.IsNullOrEmpty(tag))
            await _controller!.InvokeVoidAsync("formatBlock", tag);
    }

    private async Task ToggleBlockAsync(string tag)
    {
        if (_commandsDisabled)
            return;

        _openColorPicker = null;
        await _controller!.InvokeVoidAsync("toggleBlock", tag);
    }

    private async Task ToggleInlineCodeAsync()
    {
        if (_commandsDisabled)
            return;

        await _controller!.InvokeVoidAsync("toggleInlineCode");
    }

    private async Task UnlinkAsync()
    {
        if (_commandsDisabled)
            return;

        await _controller!.InvokeVoidAsync("unlink");
    }

    private async Task ToggleColorPickerAsync(string picker)
    {
        if (_commandsDisabled)
            return;

        _openColorPicker = _openColorPicker == picker ? null : picker;

        if (_openColorPicker is not null)
            await _controller!.InvokeVoidAsync("saveSelection");
    }

    private async Task ApplyColorAsync(string command, string color)
    {
        if (_commandsDisabled)
            return;

        _openColorPicker = null;
        await _controller!.InvokeVoidAsync("execColor", command, color);
    }

    private Task ClearColorAsync(string command) =>
        ApplyColorAsync(command, command == "foreColor" ? "inherit" : "transparent");

    private Task ToggleFullscreenAsync()
    {
        _isFullscreen = !_isFullscreen;
        return Task.CompletedTask;
    }
}
