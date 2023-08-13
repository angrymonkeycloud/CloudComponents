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

    static VideoPlayerCast PlayerCast;
    [Parameter] public required VideoPlayer Player { get; set; }
    [Parameter] public required VideoPlayerMetadata Metadata { get; set; }
    private string CastJsUrl = "https://cdnjs.cloudflare.com/ajax/libs/castjs/5.2.0/cast.min.js";

    private string? CastDeviceName;

    public VideoPlayerCast()
    {
        PlayerCast = this;
    }

    private string InfoStatus
    {
        get
        {
            if (!string.IsNullOrEmpty(CastDeviceName))
                return CastDeviceName;

            return "Casting";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            //if (!Metadata.CastingInitialized)
            //{
            await JS.InvokeVoidAsync("amcVideoPlayerCastInit");
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
        try
        {
            await JS.InvokeVoidAsync("amcVideoPlayerStartCasting", Metadata.VideoUrl, $"{Metadata.Title} | Coverbox TV", Metadata.IsLive ? Metadata.CurrentTime : null);
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
        await JS.InvokeVoidAsync("amcVideoPlayerStopCasting");

        Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.NotCasting;
        Metadata.CastingInitialized = false;

        await Player.PlayVideo();
    }

    private async Task PlayCast()
    {
        await JS.InvokeVoidAsync("amcVideoPlayerPlayCasting");
    }

    private async Task PauseCast()
    {
        await JS.InvokeVoidAsync("amcVideoPlayerPauseCasting");
    }

    public async Task HandleCastJsEvent(string eventData, object? value)
    {

        switch (eventData.ToLower())
        {
            case "playing":
                Metadata.PlayingState = PlayingStates.Playing;
                break;

            case "pause":
                Metadata.PlayingState = PlayingStates.Paused;
                break;

            case "buffering":
                Metadata.PlayingState = PlayingStates.Buffering;
                break;

            case "connect":
                Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Casting;
                CastDeviceName = value?.ToString();
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