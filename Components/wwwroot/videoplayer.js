export function init(component) {
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
