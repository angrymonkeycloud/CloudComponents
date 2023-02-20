using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public class TabItem
    {
        public TabItem(string content)
        {
            Content= content;
        }

        public TabItem(Type content)
        {
            ContentType = content;
        }

        public TabItem(Type content, IDictionary<string, object> parameters)
        {
            ContentType = content;
            Content = parameters;
        }

        public Type ContentType { get; set; }
        public object Content { get; set; }
        public bool IsActive { get; set; } = false;
        public string CssClass
        {
            get
            {
                List<string> classes = new() { "amc-tab" };

                if (IsActive)
                    classes.Add("_active");

                return string.Join(" ", classes);
            }
        }

        public async Task InvokeOnActivated()
        {
            IsActive = true;
            await OnActivated.InvokeAsync();
        }

        public EventCallback OnActivated;
    }
}
