using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public partial class Tabs
    {
        [Parameter] public List<TabItem> TabsList { get; set; }

        protected override async void OnInitialized()
        {
            if (TabsList.Any() && !TabsList.Any(key => key.IsActive))
                await TabsList.First().InvokeOnActivated();

            base.OnInitialized();
        }

        protected async Task OnTabClicked(TabItem item)
        {
            TabItem activeItem = TabsList.FirstOrDefault(key => key.IsActive);

            if (activeItem != null)
                activeItem.IsActive = false;

            await item.InvokeOnActivated();
        }
    }
}
