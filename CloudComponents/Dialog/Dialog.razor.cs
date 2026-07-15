using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AngryMonkey.CloudComponents
{
    public partial class Dialog : IClosable, IAsyncDisposable
    {
        private PopupComp? _popup;
        private bool _keyboardInitialized = false;
        private IJSObjectReference? _jsModule;

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public required string Title { get; set; }
        [Parameter] public required List<DialogButton> Buttons { get; set; }

        [Parameter] public EventCallback OnOpened { get; set; }
        [Parameter] public EventCallback OnClosed { get; set; }

        protected async Task ButtonClicked(DialogButton button)
        {
            button.OnReply?.Invoke();

            if (button.AutoClose)
                await Close();
        }

        private async Task<IJSObjectReference> GetJsModuleAsync()
            => _jsModule ??= await GeneralMethods.GetIJSObjectReference(_js, "js/dialog.js");

        public async Task Open()
        {
            if (_popup is null)
                return;

            await _popup.Open();

            IJSObjectReference module = await GetJsModuleAsync();

            if (!_keyboardInitialized)
            {
                await module.InvokeVoidAsync("InitKeyboard");
                _keyboardInitialized = true;
            }

            await module.InvokeVoidAsync("FocusDefault");
            await OnOpened.InvokeAsync();
        }

        public async Task Close()
        {
            if (_popup is null)
                return;

            await _popup.Close();
            await OnClosed.InvokeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_jsModule is not null)
            {
                try
                {
                    await _jsModule.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                }
            }
        }
    }
}
