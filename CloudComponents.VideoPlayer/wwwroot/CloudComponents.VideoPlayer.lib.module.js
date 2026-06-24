// Blazor JS Initializer for CloudComponents.VideoPlayer
// Blazor WASM automatically discovers and imports this file (beforeStart) before
// any component renders, ensuring the window.amcVideoPlayer* globals are available.

export async function beforeStart(options, extensions) {
    await loadScript('_content/AngryMonkey.CloudComponents.VideoPlayer/hls.js');
    await loadScript('_content/AngryMonkey.CloudComponents.VideoPlayer/videoPlayer.js');
    await loadScript('_content/AngryMonkey.CloudComponents.VideoPlayer/videoPlayerCast.js');
    await loadScript('_content/AngryMonkey.CloudComponents.VideoPlayer/progressbar.js');
}

function loadScript(src) {
    return new Promise((resolve, reject) => {
        // Avoid double-loading if already present
        if (document.querySelector(`script[src="${src}"]`)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}
