export function setVideoUrl(component, url) {
    return new Promise(function (resolve, reject) {
        var video = document.createElement('video');
        var hls = new Hls();
        hls.attachMedia(video);
        hls.on(Hls.Events.FRAG_PARSING_METADATA, function (event, data) {
            var metadata = data.samples[0].data;
            var url = URL.createObjectURL(new Blob([metadata]));

            return url;
        });

        hls.loadSource(url);
        hls.startLoad();
    });


    //// Get the video element
    //var video = component.querySelector('video');

    //if (!video.src.includes('blob:')) {
    //    // Instantiate the hls.js player
    //    var hls = new Hls();

    //    // Load the M3U8 playlist
    //    var m3u8Url = url;
    //    hls.loadSource(m3u8Url);

    //    // Attach the hls.js player to the video element
    //    hls.attachMedia(video);

    //    // Play the video
    //    video.play();
    //}
}