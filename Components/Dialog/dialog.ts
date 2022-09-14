
function focusButton(component: HTMLElement, selector: string) {

	console.log('------------');
	console.log(1);

	console.log(selector);

	try {
		const elemnt = component.querySelector(selector) as HTMLButtonElement;

		console.log(elemnt);

		elemnt.focus();
	} catch (e) {
		console.log(e);
	}
	console.log(2);
}

function selectPreviousButton(component: HTMLElement): void {
	let selected = component.querySelector(".amc-dialog-buttons button:focus") as HTMLButtonElement;

	if (!selected) {
		focusButton(component, ".buttons .button");
		return;
	}

	let previous = selected.previousElementSibling as HTMLButtonElement;

	if (previous) previous.focus();
}

function selectNextButton(component: HTMLElement): void {
	let selected = component.querySelector(".amc-dialog-buttons button:focus") as HTMLButtonElement;

	if (!selected) {
		focusButton(component, ".amc-dialog-buttons button");
		return;
	}

	let next = selected.nextElementSibling as HTMLButtonElement;

	if (next) next.focus();
}

function preventScrolling(): void {

	const width = document.body.offsetWidth;
	document.querySelector('html').classList.add('_noscroll');
	const scrollWidth = document.body.offsetWidth - width;

	if (scrollWidth > 0) {
		document.body.style.marginRight = scrollWidth + 'px';

		const header: HTMLElement = document.querySelector('body > header');

		if (header !== null)
			header.style.right = scrollWidth + 'px';
	}
}

function allowScrolling(): void {

	document.querySelector('html').classList.remove('_noscroll');
	document.body.style.marginRight = null;

	const header: HTMLElement = document.querySelector('body > header');

	if (header !== null)
		header.style.right = null;
}

export function DialogOpened(component: HTMLElement) {

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

export function DialogClosed(component: HTMLElement) {

	allowScrolling();
}