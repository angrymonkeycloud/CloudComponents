
function selectDialogPreviousButton() {
    let selected = document.querySelector(".amc-dialog-buttons button:focus");

    if (!selected) {
        document.querySelector(".amc-dialog-buttons button")?.focus();
        return;
    }

    let previous = selected.previousElementSibling;

    if (previous) previous.focus();
}

function selectDialogNextButton() {
    let selected = document.querySelector(".amc-dialog-buttons button:focus");

    if (!selected) {
        FocusDefault();
        return;
    }

    let next = selected.nextElementSibling;

    if (next) next.focus();
}

export function FocusDefault() {
    document.querySelector(".amc-dialog-buttons button")?.focus();
}

let keyboardHandler = null;

export function InitKeyboard() {
    if (keyboardHandler)
        return;

    keyboardHandler = function (e) {
        switch (e.key) {
            case 'ArrowLeft':
                selectDialogPreviousButton();
                break;

            case 'ArrowRight':
                selectDialogNextButton();
                break;

            default:
                break;
        }
    };

    document.addEventListener('keydown', keyboardHandler);
}
