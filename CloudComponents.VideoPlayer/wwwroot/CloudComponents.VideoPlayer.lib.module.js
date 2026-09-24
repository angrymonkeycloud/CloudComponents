// Load the global video-player helpers before either a standalone WebAssembly
// app or a Blazor Web App renders a player component.
const assets = [
    { src: '_content/AngryMonkey.CloudComponents.VideoPlayer/hls.js', ready: () => typeof window.Hls !== 'undefined' },
    { src: '_content/AngryMonkey.CloudComponents.VideoPlayer/videoPlayer.js', ready: () => typeof window.amcVideoPlayerRegisterCustomEventHandler === 'function' },
    { src: '_content/AngryMonkey.CloudComponents.VideoPlayer/videoPlayerCast.js', ready: () => typeof window.amcVideoPlayerCastInit === 'function' },
    { src: '_content/AngryMonkey.CloudComponents.VideoPlayer/progressbar.js', ready: () => typeof window.amcProgressBarRepaint === 'function' }
];

export function beforeStart() {
    return ensureAssets();
}

export function beforeWebStart() {
    return ensureAssets();
}

function ensureAssets() {
    window.__amcVideoPlayerAssetsPromise ??= assets.reduce(
        (previous, asset) => previous.then(() => loadScript(asset)),
        Promise.resolve())
        .catch(error => {
            // Video is optional UI. Report the asset failure, but never prevent
            // the host Blazor application from starting; the component itself
            // will render its error state when initialization is unavailable.
            console.error('CloudComponents.VideoPlayer assets could not be initialized.', error);
        });

    return window.__amcVideoPlayerAssetsPromise;
}

function loadScript(asset) {
    if (asset.ready())
        return Promise.resolve();

    const absoluteSource = new URL(asset.src, document.baseURI).href;
    let script = Array.from(document.scripts)
        .find(item => new URL(item.src, document.baseURI).href === absoluteSource);

    return new Promise((resolve, reject) => {
        const timeout = window.setTimeout(
            () => reject(new Error(`Timed out loading ${asset.src}.`)),
            15000);

        const complete = () => {
            window.clearTimeout(timeout);
            if (asset.ready())
                resolve();
            else
                reject(new Error(`${asset.src} loaded without registering its expected API.`));
        };

        const fail = () => {
            window.clearTimeout(timeout);
            reject(new Error(`Could not load ${asset.src}.`));
        };

        if (script) {
            script.addEventListener('load', complete, { once: true });
            script.addEventListener('error', fail, { once: true });
            window.setTimeout(() => { if (asset.ready()) complete(); }, 0);
            return;
        }

        script = document.createElement('script');
        script.src = asset.src;
        script.addEventListener('load', complete, { once: true });
        script.addEventListener('error', fail, { once: true });
        document.head.appendChild(script);
    });
}
