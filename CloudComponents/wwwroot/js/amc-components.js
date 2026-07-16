
function selectDialogPreviousButton() {
    let selected = document.querySelector(".amc-dialog-buttons button:focus");

    if (!selected) {
        document.querySelector(".amc-dialog-buttons button")?.focus();
        return;
    }

    let previous = selected.previousElementSibling;

    if (previous) previous.focus();
}

function selectDialogNextButton() {
    let selected = document.querySelector(".amc-dialog-buttons button:focus");

    if (!selected) {
        Dialog.FocusDefault();
        return;
    }

    let next = selected.nextElementSibling;

    if (next) next.focus();
}

window.Dialog = {

    FocusDefault: () => {
        document.querySelector(".amc-dialog-buttons button")?.focus();
    },

    InitKeyboard: () => {
        document.addEventListener('keydown', function (e) {
            switch (e.key) {
                case 'ArrowLeft':
                    selectDialogPreviousButton();
                    break;

                case 'ArrowRight':
                    selectDialogNextButton();
                    break;

                default:
                    break;
            }
        });
    }
};

const _amc_progressbar_valueRange = '.amc-progressbar-value';
const _amc_progressbar_changingRange = '.amc-progressbar-changingvalue';

amcProgressBarUpdatePosition = (component, clientX, maxMoveDistance) => {

    let moveDistance = clientX - component.getBoundingClientRect().left - (component.querySelector('.amc-progressbar-middle').clientWidth / 2);

    if (moveDistance < 0)
        moveDistance = 0;

    if (moveDistance > maxMoveDistance)
        moveDistance = maxMoveDistance;

    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr')

    const range = component.querySelector(_amc_progressbar_valueRange);
    const changingRange = component.querySelector(_amc_progressbar_changingRange);

    const total = Number(range.max);

    const value = moveDistance * total / maxMoveDistance;

    changingRange.value = value.toString();
    range.value = value.toString();

    const evt = document.createEvent("HTMLEvents");

    evt.initEvent("change", false, true);
    changingRange.dispatchEvent(evt);
}

window.amcProgressBarMouseDown = (component, clientX) => {

    component["IsUserInput"] = true;
    component.classList.add('_moving');

    const range = component.querySelector(_amc_progressbar_valueRange);

    const oldValue = range.value;

    const maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;

    amcProgressBarUpdatePosition(component, clientX, maxMoveDistance);

    // Mouse Move

    const mouseMoveListener = function (moveArgs) { amcProgressBarUpdatePosition(component, moveArgs.clientX, maxMoveDistance); };

    document.addEventListener('mousemove', mouseMoveListener);

    // Mouse Up

    const mouseUpListener = function () {
        component["IsUserInput"] = false;

        component.classList.remove('_moving');

        document.removeEventListener('mousemove', mouseMoveListener);
        document.removeEventListener('mouseup', mouseUpListener);

        if (range.value === oldValue)
            return;

        const evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);

        range.dispatchEvent(evt);
    }

    document.addEventListener('mouseup', mouseUpListener);
}

window.amcProgressBarTouchDown = (component, clientX) => {

    component["IsUserInput"] = true;
    component.classList.add('_moving');

    const range = component.querySelector(_amc_progressbar_valueRange);

    const oldValue = range.value;

    const maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;

    amcProgressBarUpdatePosition(component, clientX, maxMoveDistance);

    // Touch Move

    const touchMoveListener = function (moveArgs) { amcProgressBarUpdatePosition(component, moveArgs.touches[0].clientX, maxMoveDistance); };

    document.addEventListener('touchmove', touchMoveListener);

    // Touch Up

    const touchEndListener = function () {
        component["IsUserInput"] = false;

        component.classList.remove('_moving');

        document.removeEventListener('touchmove', touchMoveListener);
        document.removeEventListener('touchup', touchEndListener);

        if (range.value === oldValue)
            return;

        const evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);

        range.dispatchEvent(evt);
    }

    document.addEventListener('touchend', touchEndListener);
}

window.amcProgressBarRepaint = (component, value, total) => {

    if (component["IsUserInput"] && component["IsUserInput"] === true)
        return;

    if (!component.querySelector)
        return;

    const range = component.querySelector(_amc_progressbar_valueRange);

    if (total === null || total === undefined)
        total = Number(range.max);

    if (value === null || value === undefined)
        value = Number(range.value);

    const maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;

    const moveDistance = value * maxMoveDistance / total;

    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr')
}

window.amcProgressBarGetInfo = (component) => {

    const element = component.querySelector('.amc-progressbar-middle');

    return {
        Left: element.getBoundingClientRect().left,
        Top: element.getBoundingClientRect().right,
        Width: element.clientWidth,
        Height: element.clientHeight
    };
}
class VideoInfo {
    constructor() {
        this.Duration = 0;
        this.Width = 0;
        this.Height = 0;
    }
}

window.amcVideoPlayerInit = (component) => {

    const video = component.querySelector('video');

    const event = new Event('load', { bubbles: true, cancelable: true });
    video.dispatchEvent(event);
}

window.amcVideoPlayerGetVideoInfo = (component) => {

    const video = component.querySelector('video');

    const videoInfo = new VideoInfo();

    try { videoInfo.Duration = video.duration; } catch { }
    videoInfo.Width = video.videoWidth;
    videoInfo.Height = video.videoHeight;

    return videoInfo;
}

window.amcVideoPlayerRemovePoster = (component) => {

    const video = component.querySelector('video');
    video.setAttribute("poster", "");
}

window.amcVideoPlayerSetVideoPlaybackSpeed = (component, value) => {

    const video = component.querySelector('video');

    video.playbackRate = value;
}

window.amcVideoPlayerMuteVolume = (component, mute) => {

    const video = component.querySelector('video');

    video.muted = mute;
}

window.amcVideoPlayerChangeVolume = (component, newVolume) => {

    const video = component.querySelector('video');

    video.volume = newVolume;
}

window.amcVideoPlayerChangeCurrentTime = (component, newCurrentTime) => {

    const video = component.querySelector('video');

    video.currentTime = newCurrentTime;
}

window.amcVideoPlayerPlay = (component) => {

    const video = component.querySelector('video');

    video.play();

}

window.amcVideoPlayerPause = (component) => {

    const video = component.querySelector('video');

    video.pause();
}

window.amcVideoPlayerStop = (component) => {

    pause(component);

    const video = component.querySelector('video');
    video.currentTime = 0;

}

window.amcVideoPlayerEnterFullScreen = (component) => {

    component.requestFullscreen({
        navigationUI: "hide"
    });
}

window.amcVideoPlayerSeeking = (component, value, total) => {

    // Seek Info Position

    const seekInfo = component.querySelector('.amc-videoplayer-seekinfo');
    const container = component.querySelector('.amc-videoplayer-seekinfo-container');

    const seekInfoWidth = seekInfo.clientWidth;
    const seekInfoInnetWidth = Number(window.getComputedStyle(seekInfo, null).width.replace('px', ''));
    const containerWidth = container.clientWidth;

    if (!total) {

        const video = component.querySelector('video');
        total = video.duration;

        const progressBar = component.querySelector('.amc-videoplayer-progress');

        value = value - component.getBoundingClientRect().left;
        value = total * value / progressBar.clientWidth;
    }

    let position = (value * seekInfoWidth / total) - (containerWidth / 2);

    if (position < 0)
        position = 0;
    else if (position + containerWidth > seekInfoInnetWidth)
        position = seekInfoInnetWidth - containerWidth;

    container.style.setProperty('margin-left', position + 'px');

    return value;
}

window.amcVideoPlayerExitFullScreen = (component) => {

    document.exitFullscreen();
}

window.amcVideoPlayerRegisterCustomEventHandler = (component, eventName, payload) => {

    const videoElement = component.querySelector('video');

    if (!(videoElement && eventName))
        return false

    if (!videoElement.hasOwnProperty('customEvent')) {
        videoElement['customEvent'] = function (eventName, payload) {

            this['value'] = getJSON(this, eventName, payload)

            var event
            if (typeof (Event) === 'function')
                event = new Event('change')
            else {
                event = document.createEvent('Event')
                event.initEvent('change', true, true)
            }

            this.dispatchEvent(event)
        }
    }

    videoElement.addEventListener(eventName, function () { videoElement.customEvent(eventName, payload) });

    // Craft a bespoke json string to serve as a payload for the event
    function getJSON(videoElement, eventName, payload) {

        if (payload && payload.length > 0) {
            // this syntax copies just the properties we request from the source element
            // IE 11 compatible
            let data = {};
            for (const obj in payload) {
                const item = payload[obj];

                if (videoElement[item])
                    data[item] = videoElement[item]
            }

            // this stringify overload eliminates undefined/null/empty values
            return JSON.stringify(
                { name: eventName, state: data }
                , function (k, v) { return (v === undefined || v == null || v.length === 0) ? undefined : v }
            )
        } else {
            return JSON.stringify(
                { name: eventName }
            )
        }
    }
}

window.amcVideoPlayerAddReserveAspectRatioListener = (component, width, height) => {

    const listener = function () {

        const newHeight = component.clientWidth * height / width;

        component.style.setProperty('height', newHeight + 'px');
    };

    listener.call(this);

    window.addEventListener('resize', listener);

    return listener;
}

window.amcVideoPlayerRemoveReserveAspectRatioListener = (component, listener) => {

    window.removeEventListener('resize', listener);
}

const Events = {
    MEDIA_ATTACHING = "hlsMediaAttaching",
    MEDIA_ATTACHED = "hlsMediaAttached",
    MEDIA_DETACHING = "hlsMediaDetaching",
    MEDIA_DETACHED = "hlsMediaDetached",
    BUFFER_RESET = "hlsBufferReset",
    BUFFER_CODECS = "hlsBufferCodecs",
    BUFFER_CREATED = "hlsBufferCreated",
    BUFFER_APPENDING = "hlsBufferAppending",
    BUFFER_APPENDED = "hlsBufferAppended",
    BUFFER_EOS = "hlsBufferEos",
    BUFFER_FLUSHING = "hlsBufferFlushing",
    BUFFER_FLUSHED = "hlsBufferFlushed",
    MANIFEST_LOADING = "hlsManifestLoading",
    MANIFEST_LOADED = "hlsManifestLoaded",
    MANIFEST_PARSED = "hlsManifestParsed",
    LEVEL_SWITCHING = "hlsLevelSwitching",
    LEVEL_SWITCHED = "hlsLevelSwitched",
    LEVEL_LOADING = "hlsLevelLoading",
    LEVEL_LOADED = "hlsLevelLoaded",
    LEVEL_UPDATED = "hlsLevelUpdated",
    LEVEL_PTS_UPDATED = "hlsLevelPtsUpdated",
    LEVELS_UPDATED = "hlsLevelsUpdated",
    AUDIO_TRACKS_UPDATED = "hlsAudioTracksUpdated",
    AUDIO_TRACK_SWITCHING = "hlsAudioTrackSwitching",
    AUDIO_TRACK_SWITCHED = "hlsAudioTrackSwitched",
    AUDIO_TRACK_LOADING = "hlsAudioTrackLoading",
    AUDIO_TRACK_LOADED = "hlsAudioTrackLoaded",
    SUBTITLE_TRACKS_UPDATED = "hlsSubtitleTracksUpdated",
    SUBTITLE_TRACKS_CLEARED = "hlsSubtitleTracksCleared",
    SUBTITLE_TRACK_SWITCH = "hlsSubtitleTrackSwitch",
    SUBTITLE_TRACK_LOADING = "hlsSubtitleTrackLoading",
    SUBTITLE_TRACK_LOADED = "hlsSubtitleTrackLoaded",
    SUBTITLE_FRAG_PROCESSED = "hlsSubtitleFragProcessed",
    CUES_PARSED = "hlsCuesParsed",
    NON_NATIVE_TEXT_TRACKS_FOUND = "hlsNonNativeTextTracksFound",
    INIT_PTS_FOUND = "hlsInitPtsFound",
    FRAG_LOADING = "hlsFragLoading",
    FRAG_LOAD_EMERGENCY_ABORTED = "hlsFragLoadEmergencyAborted",
    FRAG_LOADED = "hlsFragLoaded",
    FRAG_DECRYPTED = "hlsFragDecrypted",
    FRAG_PARSING_INIT_SEGMENT = "hlsFragParsingInitSegment",
    FRAG_PARSING_USERDATA = "hlsFragParsingUserdata",
    FRAG_PARSING_METADATA = "hlsFragParsingMetadata",
    FRAG_PARSED = "hlsFragParsed",
    FRAG_BUFFERED = "hlsFragBuffered",
    FRAG_CHANGED = "hlsFragChanged",
    FPS_DROP = "hlsFpsDrop",
    FPS_DROP_LEVEL_CAPPING = "hlsFpsDropLevelCapping",
    ERROR = "hlsError",
    DESTROYING = "hlsDestroying",
    KEY_LOADING = "hlsKeyLoading",
    KEY_LOADED = "hlsKeyLoaded",
    LIVE_BACK_BUFFER_REACHED = "hlsLiveBackBufferReached",
    BACK_BUFFER_REACHED = "hlsBackBufferReached"
}

const ErrorTypes = {
    NETWORK_ERROR = 'networkError',
    MEDIA_ERROR = 'mediaError',
    KEY_SYSTEM_ERROR = 'keySystemError',
    MUX_ERROR = 'muxError',
    OTHER_ERROR = 'otherError'
}

//declare class Hls {
//	loadSource(source);
//	attachMedia(element);
//	startLoad();
//	stopLoad();
//	destroy();
//	on(event, callback);
//	off(event, callback);
//	getCurrentTime();
//	getDuration();
//	isDynamic();
//	isSeekable();
//	isPaused();
//	getBufferedRanges();
//	pause();
//	play();
//	recoverMediaError();
//	seekTo(time);
//	setMaxBufferSize(size);
//	setMaxBufferLength(length);
//	setMaxBufferHole(hole);
//	static IsSupported();
//	static get Events();
//	static get ErrorTypes();
//	get levels();
//}

let hls;
let hlsHasInitialized = false;

window.amcVideoPlayerIsStreamingPlayableNatively = (component) => {

    const video = component.querySelector('video');

    if (video.canPlayType('application/vnd.apple.mpegurl'))
        return true;

    return false;
}

window.amcVideoPlayerInitializeStreamingUrl = (component, url) => {

    return new Promise((resolve, reject) => {

        if (hlsHasInitialized)
            return;

        hls = new Hls();
        hlsHasInitialized = true;

        const video = component.querySelector('video');

        hls.on(Hls.Events.MANIFEST_PARSED, function (event, data) {

            if (data.levels.length === 0) {

                disposeStreaming();
                reject('Error: ' + data.type);
            }

            hls.on(Hls.Events.MEDIA_ATTACHED, function (event, data) {
                resolve(data.media.currentSrc);
            });

            hls.attachMedia(video);
        });

        hls.on(Hls.Events.ERROR, function (event, data) {

            disposeStreaming();
            reject('Error: ' + data.type);
        });

        hls.loadSource(url);
    });
}

window.amcVideoPlayerDisposeStreaming = () => {

    hlsHasInitialized = false;

    try {
        hls.destroy();
    } catch { }

    hls = null;
}
//declare class Castjs {
//	available: boolean;
//	connected: boolean;
//	device: string;
//	src: string;
//	title: string;
//	description: string;
//	poster: string;
//	subtitles: object[];
//	volumeLevel: number;
//	muted: boolean;
//	paused: boolean;
//	time: number;
//	timePretty: string;
//	duration: number;
//	durationPretty: string;
//	progress: number;
//	state: string;

//	constructor(options?: { receiver?: string, joinpolicy?: string });

//	on(eventType: string, callback: (e) => void);

//	cast(source: string, metadata?: object): void;

//	volume(level: number): void;

//	play(): void;

//	pause(): void;

//	mute(): void;

//	unmute(): void;

//	subtitle(index: number): void;

//	seek(time: number, relative?: boolean): void;

//	disconnect(): void;
//}

//declare class DotNet {
//	static invokeMethodAsync(assemblyName: string, methodIdentifier: string, ...args: any[]);
//}

let cjs;
let firstCasting = true;
let castPosition = null;
const castJsUrl = 'https://cdnjs.cloudflare.com/ajax/libs/castjs/5.2.0/cast.min.js';

window.amcVideoPlayerLoadJs = async (sourceUrl) => {
    return new Promise((resolve, reject) => {

        var scripts = document.getElementsByTagName('script');

        for (var i = 0; i < scripts.length; i++)
            if (scripts[i].src == sourceUrl) {
                resolve(null);
                return;
            }

        var tag = document.createElement('script');
        tag.src = sourceUrl;
        tag.type = "text/javascript";

        tag.onload = function () {
            resolve(null);
        }

        tag.onerror = function () {
            console.error("Failed to load script: " + sourceUrl);
            reject("Failed to load script");
        }

        document.body.appendChild(tag);
    });
}

window.amcVideoPlayerCreateCastJsInstance = async () => {
    return new (window).Castjs();
}

window.amcVideoPlayerCastInit = async () => {

    console.log('init: start');

    console.log(cjs);

    await amcVideoPlayerLoadJs(castJsUrl);

    console.log('init: js loaded');

    if (!cjs)
        cjs = new Castjs();

    return new Promise((resolve, reject) => {

        console.log('init: before event');

        cjs.on('event', async (e) => {

            let additionalValue = null;

            let log = true;

            if (e === 'timeupdate') {
                additionalValue = cjs.time;
                log = false;
            }

            if (e === 'connect') {
                additionalValue = cjs.device;
            }

            if (log) {
                console.log('event: started');
                console.log('----------------');
                console.log(e);
                console.log('----------------');
            }

            if (e === 'available') {
                console.log('event: available');

                resolve(null);
                return;
            }

            //if (firstCasting && e === 'playing') {

            //	if (castPosition !== null) {

            //		cjs.seek(castPosition);
            //		castPosition = null;
            //	}

            //	firstCasting = false;
            //}

            await DotNet.invokeMethodAsync('CloudComponents.VideoPlayer', 'HandleCastJsEventStatic', e, additionalValue);

            if (log)
                console.log('event: ended');
        });

        console.log('event: before error');

        cjs.on('error', (e) => {
            console.log('error: ' + e);

            reject(e);
        });

        if (cjs.available)
            resolve(null);

        console.log('init: end');
    });
}

window.amcVideoPlayerStartCasting = async (url, title, position) => {

    console.log('casting: started');

    console.log(cjs);

    let alreadyCasting = cjs.connected && cjs.src === url;

    console.log('casting: already casting = ' + alreadyCasting);

    if (!alreadyCasting) {
        firstCasting = true;

        console.log('casting: cast url');

        cjs.cast(url, {
            title: title,
        });

        console.log('casting: cast url end');
    }

    if (position !== undefined) {
        if (firstCasting)
            castPosition = position;
        else
            cjs.seek(position);
    }

    console.log('casting: before play');
    console.log('casting: Available = ' + cjs.available);
    console.log('casting: Connected = ' + cjs.connected);
    console.log(cjs);
    console.log('_____________________________________');

    cjs.play();

    console.log('casting: after play');
    console.log('casting: Available = ' + cjs.available);
    console.log('casting: Connected = ' + cjs.connected);
    console.log(cjs);
    console.log('_____________________________________');

    console.log('casting: end');
}

window.amcVideoPlayerStopCasting = () => {
    if (cjs.available)
        cjs.disconnect();

    //cjs = null;

    //var scripts = document.getElementsByTagName('script');
    //for (var i = 0; i < scripts.length; i++)
    //	if (scripts[i].src === castJsUrl) {
    //		scripts[i].parentNode.removeChild(scripts[i]);
    //		return;
    //	}
}

window.amcVideoPlayerPauseCasting = () => {
    if (cjs.available)
        cjs.pause();
}
window.amcVideoPlayerPlayCasting = () => {
    if (cjs.available)
        cjs.play();
}