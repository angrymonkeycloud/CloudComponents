using AngryMonkey.CloudMVC;
using CloudMVC.Demo.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using AngryMonkey.Cloud.Components;

namespace CloudMVC.Demo.Controllers
{
    public class HomeController : CloudController
    {
        public IActionResult Index()
        {
            CloudPage("New Test")
                   .SetDescription("Test Description")
                   .SetBlazor(CloudPageBlazorRenderModes.Server);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}