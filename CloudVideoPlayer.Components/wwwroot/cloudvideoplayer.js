export function init(video) {
}
export function play(video) {
    video.play();
}
export function stop(video) {
    video.pause();
    video.currentTime = 0;
}
