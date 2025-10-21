using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
 public partial class VideoPlayer
 {
 private async Task CallReserveAspectRatio()
 {
 while (Metadata.CurrentVideoInfo == null)
 await Task.Delay(1000);

 if (Metadata.ReserveAspectRatio)
 _resizeListener = await JS.InvokeAsync<object>("amcVideoPlayerAddReserveAspectRatioListener", ComponentElement, Metadata.CurrentVideoInfo.Width, Metadata.CurrentVideoInfo.Height);
 else await JS.InvokeVoidAsync("amcVideoPlayerRemoveReserveAspectRatioListener", ComponentElement, _resizeListener);
 }
 }
}
