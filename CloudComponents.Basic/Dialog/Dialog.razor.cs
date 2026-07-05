using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudComponents.Basic
{
    public partial class Dialog : IClosable
    {
        private PopupComp? _popup;
        private bool _keyboardInitialized = false;

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

        public async Task Open()
        {
            if (_popup is null)
                return;

            await _popup.Open();

            if (!_keyboardInitialized)
            {
                await _js.InvokeVoidAsync("Dialog.InitKeyboard");
                _keyboardInitialized = true;
            }

            await _js.InvokeVoidAsync("Dialog.FocusDefault");
            await OnOpened.InvokeAsync();
        }

        public async Task Close()
        {
            if (_popup is null)
                return;

            await _popup.Close();
            await OnClosed.InvokeAsync();
        }
    }
}
