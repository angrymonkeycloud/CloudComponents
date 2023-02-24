using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AngryMonkey.Cloud.Components;

public class CloudPage
{
    public string? Title { get; set; }
    public List<string> TitleAddOns { get; set; } = new List<string>();
    public string? Keywords { get; set; }
    public string? Description { get; set; }
    public bool IndexPage { get; set; } = true;
    public bool FollowPage { get; set; } = true;
    public bool IsCrawler { get; set; } = true;
    public string BaseUrl { get; set; } = "/";
    public bool AutoAppendBlazorStyles { get; set; } = true;
    public List<CloudPageFeatures> Features { get; set; } = new();
    public CloudPageBlazorRenderModes BlazorRenderMode { get; set; } = CloudPageBlazorRenderModes.None;

    public List<CloudBundle> Bundles { get; set; } = new List<CloudBundle>();

    public event EventHandler? OnModified;

    public CloudPage AppendBundle(CloudBundle bundle)
    {
        Bundles.Add(bundle);

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage AppendBundle(string bundleSource)
    {
        Bundles.Add(new CloudBundle() { Source = bundleSource });

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetTitle(string title)
    {
        Title = title;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetKeywords(string keywords)
    {
        Keywords = keywords;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetDescription(string description)
    {
        Description = description;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetIndexPage(bool indexPage)
    {
        IndexPage = indexPage;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetFollowPage(bool followPage)
    {
        FollowPage = followPage;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetTitleAddOns(List<string> titleAddOns)
    {
        TitleAddOns = titleAddOns;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetBlazor(CloudPageBlazorRenderModes renderMode)
    {
        BlazorRenderMode = renderMode;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetIsCrawler(bool isCrawler)
    {
        IsCrawler = isCrawler;

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public CloudPage SetFeatures(params CloudPageFeatures[] features)
    {
        Features.AddRange(features);

        OnModified?.Invoke(this, new EventArgs());

        return this;
    }

    public string? RobotsResult()
    {
        List<string> content = new();

        if (!IndexPage)
            content.Add("noindex");

        if (!FollowPage)
            content.Add("nofollow");

        if (content.Any())
            return string.Join(", ", content);

        return null;
    }

    public string TitleResult(CloudWeb2 cloudWeb)
    {
        if (string.IsNullOrEmpty(Title))
            return cloudWeb.PageDefaults.Title;

        StringBuilder title = new($"{cloudWeb.TitlePrefix}{Title}{cloudWeb.TitleSuffix}");

        if (TitleAddOns.Any())
            foreach (string addText in TitleAddOns)
                if (title.Length + addText.Length + 1 <= 64)
                    title.Append($" {addText}");

        return title.ToString();
    }

    public string? KeywordsResult() => Keywords;

    public string? DescriptionResult() => Description?.Length > 160 ? $"{Description[..157]}..." : Description;

    public CloudPageBlazorRenderModes BlazorRenderModeResult() =>
        BlazorRenderMode == CloudPageBlazorRenderModes.None || BlazorRenderMode != CloudPageBlazorRenderModes.CrawlerAuto ? BlazorRenderMode :
        (IsCrawler ? CloudPageBlazorRenderModes.Server : CloudPageBlazorRenderModes.WebAssembly);
}
