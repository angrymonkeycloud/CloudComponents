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
    component.requestFullscreen();
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
