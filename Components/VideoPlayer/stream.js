export function setVideoUrl(component, url) {
    var video = component.querySelector('video');

    if (!video.src.includes('blob:')) {
        // Instantiate the hls.js player
        var hls = new Hls();

        // Load the M3U8 playlist
        var m3u8Url = url;
        hls.loadSource(m3u8Url);

        // Attach the hls.js player to the video element
        hls.attachMedia(video);

        // Play the video
        video.play();
    }

}