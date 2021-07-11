export function init(component: HTMLElement) {

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