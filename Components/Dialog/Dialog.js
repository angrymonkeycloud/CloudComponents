
function selectDialogPreviousButton(component) {
    let selected = component.querySelector(".amc-dialog-buttons button:focus");

    if (!selected) {
        component.querySelector(".buttons .button").focus()
        return;
    }

    let previous = selected.previousElementSibling;

    if (previous) previous.focus();
}

function selectDialogNextButton(component) {
    let selected = component.querySelector(".amc-dialog-buttons button:focus");

    if (!selected) {
        Dialog.FocusDefault(component);
        return;
    }

    let next = selected.nextElementSibling;

    if (next) next.focus();
}

window.Dialog = {

    FocusDefault = (component) => {

        component.querySelector(".amc-dialog-buttons button").focus()
    },

    Open = (component) => {

        component.showModal();
        window.history.pushState(null, null, null);

        document.addEventListener('keydown', function (e) {
            switch (e.key) {
                case 'ArrowLeft':
                    selectDialogPreviousButton(component);
                    break;

                case 'ArrowRight':
                    selectDialogNextButton(component);
                    break;

                default:
                    break;
            }
        });
    },

    Close = (component) => {

        component.close();
    }
};
