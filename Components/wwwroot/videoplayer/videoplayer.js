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
    videoInfo.Duration = video.duration;
    videoInfo.Width = video.videoWidth;
    videoInfo.Height = video.videoHeight;
    return videoInfo;
}
export function setVideoPlaybackSpeed(component, value) {
    var video = component.querySelector('video');
    video.playbackRate = value;
}
export function muteVolume(component, mute) {
    var video = component.querySelector('video');
    video.muted = mute;
}
export function changeVolume(component, newVolume) {
    var video = component.querySelector('video');
    video.volume = newVolume;
}
export function changeCurrentTime(component, newCurrentTime) {
    var video = component.querySelector('video');
    video.currentTime = newCurrentTime;
}
export function play(component) {
    var video = component.querySelector('video');
    video.play();
}
export function pause(component) {
    var video = component.querySelector('video');
    video.pause();
}
export function stop(component) {
    pause(component);
    var video = component.querySelector('video');
    video.currentTime = 0;
}
export function enterFullScreen(component) {
    component.requestFullscreen({
        navigationUI: "hide"
    });
}
export function seeking(component, value, total) {
    // Seek Info Position
    var seekInfo = component.querySelector('.amc-videoplayer-seekinfo');
    var container = component.querySelector('.amc-videoplayer-seekinfo-container');
    var seekInfoWidth = seekInfo.clientWidth;
    var seekInfoInnetWidth = Number(window.getComputedStyle(seekInfo, null).width.replace('px', ''));
    var containerWidth = container.clientWidth;
    if (!total) {
        var video = component.querySelector('video');
        total = video.duration;
        var progressBar = component.querySelector('.amc-videoplayer-progress');
        value = value - component.getBoundingClientRect().left;
        value = total * value / progressBar.clientWidth;
    }
    var position = (value * seekInfoWidth / total) - (containerWidth / 2);
    if (position < 0)
        position = 0;
    else if (position + containerWidth > seekInfoInnetWidth)
        position = seekInfoInnetWidth - containerWidth;
    container.style.setProperty('margin-left', position + 'px');
    return value;
}
export function exitFullScreen(component) {
    document.exitFullscreen();
}
export function registerCustomEventHandler(component, eventName, payload) {
    var videoElement = component.querySelector('video');
    if (!(videoElement && eventName))
        return false;
    if (!videoElement.hasOwnProperty('customEvent')) {
        videoElement['customEvent'] = function (eventName, payload) {
            this['value'] = getJSON(this, eventName, payload);
            var event;
            if (typeof (Event) === 'function')
                event = new Event('change');
            else {
                event = document.createEvent('Event');
                event.initEvent('change', true, true);
            }
            this.dispatchEvent(event);
        };
    }
    videoElement.addEventListener(eventName, function () { videoElement.customEvent(eventName, payload); });
    // Craft a bespoke json string to serve as a payload for the event
    function getJSON(videoElement, eventName, payload) {
        if (payload && payload.length > 0) {
            // this syntax copies just the properties we request from the source element
            // IE 11 compatible
            var data = {};
            for (var obj in payload) {
                var item = payload[obj];
                if (videoElement[item])
                    data[item] = videoElement[item];
            }
            // this stringify overload eliminates undefined/null/empty values
            return JSON.stringify({ name: eventName, state: data }, function (k, v) { return (v === undefined || v == null || v.length === 0) ? undefined : v; });
        }
        else {
            return JSON.stringify({ name: eventName });
        }
    }
}
export function AddReserveAspectRatioListener(component, width, height) {
    var listener = function () {
        var newHeight = component.clientWidth * height / width;
        component.style.setProperty('height', newHeight + 'px');
    };
    listener.call(this);
    window.addEventListener('resize', listener);
    return listener;
}
export function RemoveReserveAspectRatioListener(component, listener) {
    window.removeEventListener('resize', listener);
}
