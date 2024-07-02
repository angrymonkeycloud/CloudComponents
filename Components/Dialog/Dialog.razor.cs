using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public partial class Dialog : IClosable
    {
        private ElementReference ComponentElement { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public required string Title { get; set; }
        [Parameter] public required List<DialogButton> Buttons { get; set; }
        [Parameter] public bool IsOpened { get; set; }

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
            IsOpened = true;

            StateHasChanged();

            await _js.InvokeVoidAsync("Dialog.Open", ComponentElement);
            await _js.InvokeVoidAsync("Dialog.FocusDefault", ComponentElement);

            _navigation.LocationChanged += HandleLocationChanged;

            await OnOpened.InvokeAsync();
        }

        private async void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            if (!IsOpened)
                return;

            await Close();
        }

        public async Task Close()
        {
            IsOpened = false;

            StateHasChanged();

            await _js.InvokeVoidAsync("Dialog.Close", ComponentElement);
            await OnClosed.InvokeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsOpened)
                return;

            await Close();

            _navigation.LocationChanged -= HandleLocationChanged;
        }
    }
}
