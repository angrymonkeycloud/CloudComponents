
class VideoInfo {
	Duration: string;
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
	videoInfo.Duration = video.duration.toString();
	return videoInfo;
}

export function changeCurrentTime(component: HTMLElement, newCurrentTime: number) {

	const video = component.querySelector('video');

	video.currentTime = newCurrentTime;
}

export function play(component: HTMLElement) {

	component.classList.add('_playing');

	const video = component.querySelector('video');

	video.play();

}

export function pause(component: HTMLElement) {

	component.classList.remove('_playing');

	const video = component.querySelector('video');

	video.pause();
}

export function stop(component: HTMLElement) {

	pause(component);

	const video = component.querySelector('video');
	video.currentTime = 0;
}

export function enterFullScreen(component: HTMLElement) {

	component.classList.add('_fullscreen');

	component.requestFullscreen();
}

export function exitFullScreen(component: HTMLElement) {

	component.classList.remove('_fullscreen');
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