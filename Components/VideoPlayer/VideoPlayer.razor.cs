using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.Cloud.Components;

public partial class VideoPlayer : ComponentBase
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;
}
