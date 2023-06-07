class VideoInfo {
	Duration: number;
	Width: number;
	Height: number;
}

export function init(component: HTMLElement) {

	const video = component.querySelector('video');

	video.onloadeddata = function () {

		const evt = document.createEvent("HTMLEvents");
		evt.initEvent("load", false, true);

		video.dispatchEvent(evt);
	};
}

export function getVideoInfo(component: HTMLElement): VideoInfo {

	const video = component.querySelector('video');

	const videoInfo = new VideoInfo();

	try { videoInfo.Duration = video.duration; } catch { }
	videoInfo.Width = video.videoWidth;
	videoInfo.Height = video.videoHeight;

	return videoInfo;
}

export function setVideoPlaybackSpeed(component: HTMLElement, value: number) {

	const video = component.querySelector('video');

	video.playbackRate = value;
}

export function muteVolume(component: HTMLElement, mute: boolean) {

	const video = component.querySelector('video');

	video.muted = mute;
}

export function changeVolume(component: HTMLElement, newVolume: number) {

	const video = component.querySelector('video');

	video.volume = newVolume;
}

export function changeCurrentTime(component: HTMLElement, newCurrentTime: number) {

	const video = component.querySelector('video');

	video.currentTime = newCurrentTime;
}

export function play(component: HTMLElement) {

	const video = component.querySelector('video');

	video.play();

}

export function pause(component: HTMLElement) {

	const video = component.querySelector('video');

	video.pause();
}

export function stop(component: HTMLElement) {

	pause(component);

	const video = component.querySelector('video');
	video.currentTime = 0;

}

export function enterFullScreen(component: HTMLElement) {

	component.requestFullscreen({
		navigationUI: "hide"
	});
}

export function seeking(component: HTMLElement, value: number, total?: number): number {

	// Seek Info Position

	const seekInfo: HTMLElement = component.querySelector('.amc-videoplayer-seekinfo');
	const container: HTMLElement = component.querySelector('.amc-videoplayer-seekinfo-container');

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

export function exitFullScreen(component: HTMLElement) {

	document.exitFullscreen();
}

export function registerCustomEventHandler(component, eventName: string, payload) {

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

export function AddReserveAspectRatioListener(component: HTMLElement, width: number, height: number) {

	const listener = function () {

		const newHeight = component.clientWidth * height / width;

		component.style.setProperty('height', newHeight + 'px');
	};

	listener.call(this);

	window.addEventListener('resize', listener);

	return listener;
}

export function RemoveReserveAspectRatioListener(component: HTMLElement, listener: any) {

	window.removeEventListener('resize', listener);
}

export declare enum Events {
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

export enum ErrorTypes {
	NETWORK_ERROR = 'networkError',
	MEDIA_ERROR = 'mediaError',
	KEY_SYSTEM_ERROR = 'keySystemError',
	MUX_ERROR = 'muxError',
	OTHER_ERROR = 'otherError'
}

declare class Hls {
	loadSource(source: string): void;
	attachMedia(element: HTMLMediaElement): void;
	startLoad(): void;
	stopLoad(): void;
	destroy(): void;
	on(event: string, callback: Function): void;
	off(event: string, callback: Function): void;
	getCurrentTime(): number;
	getDuration(): number;
	isDynamic(): boolean;
	isSeekable(): boolean;
	isPaused(): boolean;
	getBufferedRanges(): { start: number; end: number }[];
	pause(): void;
	play(): void;
	recoverMediaError();
	seekTo(time: number): void;
	setMaxBufferSize(size: number): void;
	setMaxBufferLength(length: number): void;
	setMaxBufferHole(hole: number): void;
	static get Events(): typeof Events;
	static get ErrorTypes(): typeof ErrorTypes;
	get levels(): [any];
}

let hls;
let hlsHasInitialized = false;
export function initializeStreamingUrl(component: HTMLElement, url) {

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

export function disposeStreaming() {

	hlsHasInitialized = false;

	try {
		hls.destroy();
	} catch { }

	hls = null;
}