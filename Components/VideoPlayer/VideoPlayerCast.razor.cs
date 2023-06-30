using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace AngryMonkey.Cloud.Components;

public partial class VideoPlayerCast
{
    private ElementReference ComponentElement { get; set; }

    private Task<IJSObjectReference> _module;
    private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "videoplayer/videoplayercast.js");

    static VideoPlayerCast PlayerCast;
    [Parameter] public required VideoPlayer Player { get; set; }
    [Parameter] public required VideoPlayerMetadata Metadata { get; set; }
    private string CastJsUrl = "https://cdnjs.cloudflare.com/ajax/libs/castjs/5.2.0/cast.min.js";

    public VideoPlayerCast()
    {
        PlayerCast = this;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            //if (!Metadata.CastingInitialized)
            //{
                var module = await Module;

                await module.InvokeVoidAsync("init");
                Metadata.CastingInitialized = true;
            //}

            //if (Metadata.CastStatus == VideoPlayerMetadata.CastStatuses.Initializing)
            //{
                Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Connecting;
                await StartCast();
            //}
        }
    }

    internal async Task StartCast()
    {
        var module = await Module;

        try
        {
            await module.InvokeVoidAsync("startCasting", Metadata.VideoUrl, Metadata.IsStream ? Metadata.CurrentTime : null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            Thread.Sleep(1000);
            await StartCast();
        }
    }
    private async Task StopCast()
    {
        var module = await Module;

        await module.InvokeVoidAsync("stopCasting");

        Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.NotCasting;
        Metadata.CastingInitialized = false;
    }

    private async Task PlayCast()
    {
        var module = await Module;

        await module.InvokeVoidAsync("playCasting");
    }

    private async Task PauseCast()
    {
        var module = await Module;

        await module.InvokeVoidAsync("pauseCasting");
    }

    public async Task HandleCastJsEvent(string eventData, object? value)
    {

        switch (eventData.ToLower())
        {
            case "playing":
                Metadata.Status = VideoPlayerMetadata.VideoStatus.Playing;
                Metadata.IsVideoPlaying = true;
                break;

            case "pause":
                Metadata.Status = VideoPlayerMetadata.VideoStatus.Paused;
                Metadata.IsVideoPlaying = false;
                break;

            case "buffering":
                Metadata.Status = VideoPlayerMetadata.VideoStatus.Buffering;
                break;

            case "connect":
                Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Casting;
                break;

            case "timeupdate":
                Metadata.CurrentTime = double.Parse(value!.ToString());
                break;
        }

        Player.Repaint();
        StateHasChanged();

        if (eventData == "timeupdate")
            return;

        Console.WriteLine(eventData);
        Console.WriteLine("------------");
    }

    [JSInvokable]
    public static async Task HandleCastJsEventStatic(string eventData, object? value)
    {
        await PlayerCast.HandleCastJsEvent(eventData, value);
    }
}