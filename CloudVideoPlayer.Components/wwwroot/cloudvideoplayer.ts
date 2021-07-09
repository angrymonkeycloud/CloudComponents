export function init(video: HTMLVideoElement) {

}

export function play(video: HTMLVideoElement) {
	video.play();
}

export function stop(video: HTMLVideoElement) {
	video.pause();
	video.currentTime = 0;
}