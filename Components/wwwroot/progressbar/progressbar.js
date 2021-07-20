var _amc_progressbar_valueRange = '.amc-progressbar-value';
var _amc_progressbar_changingRange = '.amc-progressbar-changingvalue';
function updatePosition(component, clientX, maxMoveDistance) {
    var moveDistance = clientX - component.getBoundingClientRect().left - (component.querySelector('.amc-progressbar-middle').clientWidth / 2);
    if (moveDistance < 0)
        moveDistance = 0;
    if (moveDistance > maxMoveDistance)
        moveDistance = maxMoveDistance;
    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr');
    var range = component.querySelector(_amc_progressbar_valueRange);
    var changingRange = component.querySelector(_amc_progressbar_changingRange);
    var total = Number(range.max);
    var value = moveDistance * total / maxMoveDistance;
    changingRange.value = value.toString();
    range.value = value.toString();
    var evt = document.createEvent("HTMLEvents");
    evt.initEvent("change", false, true);
    changingRange.dispatchEvent(evt);
}
export function mouseDown(component, clientX) {
    component["IsUserInput"] = true;
    component.classList.add('_moving');
    var range = component.querySelector(_amc_progressbar_valueRange);
    var oldValue = range.value;
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    updatePosition(component, clientX, maxMoveDistance);
    // Mouse Move
    var mouseMoveListener = function (moveArgs) { updatePosition(component, moveArgs.clientX, maxMoveDistance); };
    document.addEventListener('mousemove', mouseMoveListener);
    // Mouse Up
    var mouseUpListener = function () {
        component["IsUserInput"] = false;
        component.classList.remove('_moving');
        document.removeEventListener('mousemove', mouseMoveListener);
        document.removeEventListener('mouseup', mouseUpListener);
        if (range.value === oldValue)
            return;
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);
        range.dispatchEvent(evt);
    };
    document.addEventListener('mouseup', mouseUpListener);
}
export function touchDown(component, clientX) {
    component["IsUserInput"] = true;
    component.classList.add('_moving');
    var range = component.querySelector(_amc_progressbar_valueRange);
    var oldValue = range.value;
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    updatePosition(component, clientX, maxMoveDistance);
    // Touch Move
    var touchMoveListener = function (moveArgs) { updatePosition(component, moveArgs.touches[0].clientX, maxMoveDistance); };
    document.addEventListener('touchmove', touchMoveListener);
    // Touch Up
    var touchEndListener = function () {
        component["IsUserInput"] = false;
        component.classList.remove('_moving');
        document.removeEventListener('touchmove', touchMoveListener);
        document.removeEventListener('touchup', touchEndListener);
        if (range.value === oldValue)
            return;
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);
        range.dispatchEvent(evt);
    };
    document.addEventListener('touchend', touchEndListener);
}
export function repaint(component, value, total) {
    if (component["IsUserInput"] && component["IsUserInput"] === true)
        return;
    if (!component.querySelector)
        return;
    var range = component.querySelector(_amc_progressbar_valueRange);
    if (total === null || total === undefined)
        total = Number(range.max);
    if (value === null || value === undefined)
        value = Number(range.value);
    var maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;
    var moveDistance = value * maxMoveDistance / total;
    component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr');
}
export function getInfo(component) {
    var element = component.querySelector('.amc-progressbar-middle');
    return {
        SeekButton: {
            Left: element.clientLeft,
            Top: element.clientTop,
            Width: element.clientWidth,
            Height: element.clientHeight
        }
    };
}
