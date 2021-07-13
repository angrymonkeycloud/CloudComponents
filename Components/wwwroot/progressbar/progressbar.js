var hiddenInputRange = 'input[type="range"]';
function updatePosition(component, clientX, maxMoveDistance) {
    var moveDistance = clientX - component.getBoundingClientRect().left - (component.querySelector('.amc-progressbar-middle').clientWidth / 2);
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
export function mouseDown(component, clientX) {
    component["IsUserInput"] = true;
    var range = component.querySelector(hiddenInputRange);
    var oldValue = range.value;
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    var listener = function (moveArgs) { updatePosition(component, moveArgs.clientX, maxMoveDistance); };
    updatePosition(component, clientX, maxMoveDistance);
    document.addEventListener('mousemove', listener);
    document.onmouseup = function () {
        component["IsUserInput"] = false;
        document.removeEventListener('mousemove', listener);
        if (range.value === oldValue)
            return;
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);
        range.dispatchEvent(evt);
    };
}
export function repaint(component, value, total) {
    if (component["IsUserInput"] && component["IsUserInput"] === true)
        return;
    if (!component.querySelector)
        return;
    var range = component.querySelector(hiddenInputRange);
    if (total === null || total === undefined)
        total = Number(range.max);
    if (value === null || value === undefined)
        value = Number(range.value);
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    var moveDistance = value * maxMoveDistance / total;
    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr');
}
