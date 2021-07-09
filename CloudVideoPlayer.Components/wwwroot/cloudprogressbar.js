function mouseMove(component, moveArgs, maxMoveDistance) {
    var moveDistance = moveArgs.clientX - component.getBoundingClientRect().left - (component.querySelector('.cloudprogressbar-middle').clientWidth / 2);
    if (moveDistance < 0)
        moveDistance = 0;
    if (moveDistance > maxMoveDistance)
        moveDistance = maxMoveDistance;
    document.title = moveDistance.toString();
    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr');
}
export function mouseDown(component) {
    var maxMoveDistance = component.clientWidth - component.querySelector('.cloudprogressbar-middle').clientWidth;
    var listener = function (moveArgs) { mouseMove(component, moveArgs, maxMoveDistance); };
    document.addEventListener('mousemove', listener);
    document.onmouseup = function () {
        document.removeEventListener('mousemove', listener);
        //dotNetHelper.invokeMethodAsync("Test");
    };
}
