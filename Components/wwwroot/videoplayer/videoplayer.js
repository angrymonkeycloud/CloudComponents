function sleep(ms) {
    return new Promise(function (resolve) { return setTimeout(resolve, ms); });
}
var VideoInfo = /** @class */ (function () {
    function VideoInfo() {
    }
    return VideoInfo;
}());
export function init(component) {
    var video = component.querySelector('video');
    video.onloadeddata = function () {
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("load", false, true);
        video.dispatchEvent(evt);
    };
}
export function getVideoInfo(component) {
    var video = component.querySelector('video');
    var videoInfo = new VideoInfo();
    videoInfo.Duration = video.duration.toString();
    return videoInfo;
}
export function changeCurrentTime(component, newCurrentTime) {
    var video = component.querySelector('video');
    video.currentTime = newCurrentTime;
}
export function play(component) {
    component.classList.add('_playing');
    var video = component.querySelector('video');
    video.play();
}
export function pause(component) {
    component.classList.remove('_playing');
    var video = component.querySelector('video');
    video.pause();
}
export function stop(component) {
    pause(component);
    var video = component.querySelector('video');
    video.currentTime = 0;
}
export function enterFullScreen(component) {
    component.classList.add('_fullscreen');
    component.requestFullscreen();
}
export function exitFullScreen(component) {
    component.classList.remove('_fullscreen');
    document.exitFullscreen();
}
