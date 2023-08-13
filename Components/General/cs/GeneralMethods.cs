using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public class GeneralMethods
    {
        internal static Task<IJSObjectReference> GetIJSObjectReference(IJSRuntime jsRuntime, string path)
        {
#if !DEBUG
			path = path.Replace(".js", ".min.js");
#endif

            string importPath = $"./_content/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/{path}?v={Guid.NewGuid()}";

            return jsRuntime.InvokeAsync<IJSObjectReference>("import", importPath).AsTask();
        }

        public static string ToUrlFriendly(string text)
        {
            List<char> nonFriendly = text.Where(c => !char.IsLetterOrDigit(c)).ToList();

            foreach (char c in nonFriendly)
                text = text.Replace(c, '-');

            while (text.Contains("--"))
                text = text.Replace("--", "-");

            return text.Trim('-').ToLower();
        }
    }
}
