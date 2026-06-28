
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
        Dialog.FocusDefault();
        return;
    }

    let next = selected.nextElementSibling;

    if (next) next.focus();
}

window.Dialog = {

    FocusDefault: () => {
        document.querySelector(".amc-dialog-buttons button")?.focus();
    },

    InitKeyboard: () => {
        document.addEventListener('keydown', function (e) {
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
        });
    }
};
