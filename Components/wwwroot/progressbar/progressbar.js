var hiddenInputRange = 'input[type="range"]';
export function init(component) {
    var range = component.querySelector(hiddenInputRange);
    var total = Number(range.max);
    var value = Number(range.value);
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    var moveDistance = value * maxMoveDistance / total;
    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr');
}
function mouseMove(component, moveArgs, maxMoveDistance) {
    var moveDistance = moveArgs.clientX - component.getBoundingClientRect().left - (component.querySelector('.amc-progressbar-middle').clientWidth / 2);
    if (moveDistance < 0)
        moveDistance = 0;
    if (moveDistance > maxMoveDistance)
        moveDistance = maxMoveDistance;
    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr');
    var range = component.querySelector(hiddenInputRange);
    var total = Number(range.max);
    var value = moveDistance * total / maxMoveDistance;
    range.value = value.toString();
}
export function mouseDown(component) {
    var range = component.querySelector(hiddenInputRange);
    var oldValue = range.value;
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    var listener = function (moveArgs) { mouseMove(component, moveArgs, maxMoveDistance); };
    document.addEventListener('mousemove', listener);
    document.onmouseup = function () {
        document.removeEventListener('mousemove', listener);
        if (range.value === oldValue)
            return;
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);
        range.dispatchEvent(evt);
    };
}
