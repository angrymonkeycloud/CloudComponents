using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AngryMonkey.CloudComponents
{
    public partial class CopyButton : IDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        /// <summary>The text copied to the clipboard.</summary>
        [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;

        /// <summary>Content displayed next to the button (e.g. the text being offered for copy).</summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>Replaces the default copy/checkmark icons with custom content.</summary>
        [Parameter] public RenderFragment? ButtonContent { get; set; }

        /// <summary>Shows or hides the copy button. Set to false when only <see cref="CopyOnContentClick"/> should trigger the copy.</summary>
        [Parameter] public bool ShowButton { get; set; } = true;

        /// <summary>Allows clicking <see cref="ChildContent"/> to also trigger the copy.</summary>
        [Parameter] public bool CopyOnContentClick { get; set; } = false;

        private const int SuccessDurationMs = 1000;

        private bool _copied;
        private CancellationTokenSource? _resetCts;

        protected string CssClass => _copied ? "_success" : string.Empty;
        protected string ContentCssClass => CopyOnContentClick ? "_clickable" : string.Empty;
        protected string ButtonTitle => _copied ? "Copied!" : "Copy";

        protected Task OnContentClick() => CopyOnContentClick ? CopyAsync() : Task.CompletedTask;

        protected async Task CopyAsync()
        {
            if (string.IsNullOrEmpty(Text))
                return;

            _ = WriteToClipboardAsync(Text);

            _resetCts?.Cancel();
            _resetCts?.Dispose();
            _resetCts = new CancellationTokenSource();
            CancellationToken token = _resetCts.Token;

            _copied = true;

            try
            {
                await Task.Delay(SuccessDurationMs, token);

                _copied = false;
            }
            catch (TaskCanceledException) { }
        }

        private async Task WriteToClipboardAsync(string text)
        {
            try
            {
                await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
            }
            catch (JSException)
            {
            }
        }

        public void Dispose()
        {
            _resetCts?.Cancel();
            _resetCts?.Dispose();
        }
    }
}
