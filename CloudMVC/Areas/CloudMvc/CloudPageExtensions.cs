using AngryMonkey.Cloud.Components;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components;

public static class CloudPageExtensions
{
    public static RenderMode GetRenderMode(this CloudPage cloudPage) => cloudPage.BlazorRenderModeResult() switch
    {
        CloudPageBlazorRenderModes.Server => RenderMode.ServerPrerendered,
        CloudPageBlazorRenderModes.WebAssembly => RenderMode.WebAssemblyPrerendered,
        _ => RenderMode.Static,
    };
}
