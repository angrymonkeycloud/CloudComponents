function sleep(ms) {
	return new Promise(resolve => setTimeout(resolve, ms));
}

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