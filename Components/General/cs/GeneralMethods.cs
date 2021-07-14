using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	internal class GeneralMethods
	{
		internal static Task<IJSObjectReference> GetIJSObjectReference(IJSRuntime jsRuntime, string path)
		{
#if !DEBUG
			path = path.Replace(".js", ".min.js");
#endif

			string importPath = $"./_content/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/{path}?v={Guid.NewGuid()}";

			return jsRuntime.InvokeAsync<IJSObjectReference>("import", importPath).AsTask();
		}
	}
}
