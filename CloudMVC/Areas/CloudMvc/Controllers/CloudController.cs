using AngryMonkey.Cloud.Components;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.CloudMVC
{
    public class CloudController : Controller
    {
        public CloudPage CloudPage(string? title = null)
        {
            CloudPage cloudPage = new();

            if (!string.IsNullOrEmpty(title))
                cloudPage.SetTitle(title);

            cloudPage.SetIsCrawler(IsCrawler());

            cloudPage.OnModified += (object? sender, EventArgs e) => { ViewData["CloudPageStatic"] = cloudPage; };

            ViewData.Add("CloudPageStatic", cloudPage);

            return cloudPage;
        }

        private bool IsCrawler()
        {
            string userAgeny = ControllerContext.HttpContext.Request.Headers.UserAgent.ToString().Trim().ToLower();

            return CloudWeb2.CrawlersUserAgents.Any(userAgeny.Contains);
        }
    }
}
