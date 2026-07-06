using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudComponents.Basic;

public partial class VideoPlayer : ComponentBase
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;
}
