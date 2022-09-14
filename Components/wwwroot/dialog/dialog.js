function focusButton(component, selector) {
    console.log('------------');
    console.log(1);
    console.log(selector);
    try {
        var elemnt = component.querySelector(selector);
        console.log(elemnt);
        elemnt.focus();
    }
    catch (e) {
        console.log(e);
    }
    console.log(2);
}
function selectPreviousButton(component) {
    var selected = component.querySelector(".amc-dialog-buttons button:focus");
    if (!selected) {
        focusButton(component, ".buttons .button");
        return;
    }
    var previous = selected.previousElementSibling;
    if (previous)
        previous.focus();
}
function selectNextButton(component) {
    var selected = component.querySelector(".amc-dialog-buttons button:focus");
    if (!selected) {
        focusButton(component, ".amc-dialog-buttons button");
        return;
    }
    var next = selected.nextElementSibling;
    if (next)
        next.focus();
}
function preventScrolling() {
    var width = document.body.offsetWidth;
    document.querySelector('html').classList.add('_noscroll');
    var scrollWidth = document.body.offsetWidth - width;
    if (scrollWidth > 0) {
        document.body.style.marginRight = scrollWidth + 'px';
        var header = document.querySelector('body > header');
        if (header !== null)
            header.style.right = scrollWidth + 'px';
    }
}
function allowScrolling() {
    document.querySelector('html').classList.remove('_noscroll');
    document.body.style.marginRight = null;
    var header = document.querySelector('body > header');
    if (header !== null)
        header.style.right = null;
}
export function DialogOpened(component) {
    console.log('opening');
    console.log(component);
    focusButton(component, ".amc-dialog-buttons button");
    preventScrolling();
    document.addEventListener('keydown', function (e) {
        console.log(1);
        switch (e.keyCode) {
            //case 13: // Enter
            //	CoreMessage.click();
            //	break;
            case 27: // Escape
                break;
            case 37: // Left Arrow
                selectPreviousButton(component);
                break;
            case 39: // Right Arrow
                selectNextButton(component);
                break;
            default:
                break;
        }
    });
    console.log('opened');
}
export function DialogClosed(component) {
    allowScrolling();
}
