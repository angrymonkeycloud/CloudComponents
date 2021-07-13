import { dotNetHelper } from '../General/js/DotNet';

const hiddenInputRange = 'input[type="range"]';

function updatePosition(component: HTMLElement, clientX: number, maxMoveDistance: number) {

	let moveDistance = clientX - component.getBoundingClientRect().left - (component.querySelector('.amc-progressbar-middle').clientWidth / 2);

	if (moveDistance < 0)
		moveDistance = 0;

	if (moveDistance > maxMoveDistance)
		moveDistance = maxMoveDistance;

	component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr')

	const range: HTMLInputElement = component.querySelector(hiddenInputRange);

	const total = Number(range.max);

	const value = moveDistance * total / maxMoveDistance;

	range.value = value.toString();
}

export function mouseDown(component: HTMLElement, clientX: number) {

	component["IsUserInput"] = true;

	const range: HTMLInputElement = component.querySelector(hiddenInputRange);

	const oldValue = range.value;

	const maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;

	const listener = function (moveArgs: MouseEvent) { updatePosition(component, moveArgs.clientX, maxMoveDistance); };

	updatePosition(component, clientX, maxMoveDistance);

	document.addEventListener('mousemove', listener);

	document.onmouseup = function () {

		component["IsUserInput"] = false;

		document.removeEventListener('mousemove', listener);

		if (range.value === oldValue)
			return;

		const evt = document.createEvent("HTMLEvents");
		evt.initEvent("change", false, true);

		range.dispatchEvent(evt);
	}
}

export function repaint(component: HTMLElement, value?: number, total?: number) {

	if (component["IsUserInput"] && component["IsUserInput"] === true)
		return;

	if (!component.querySelector)
		return;

	const range: HTMLInputElement = component.querySelector(hiddenInputRange);

	if (total === null || total === undefined)
		total = Number(range.max);

	if (value === null || value === undefined)
		value = Number(range.value);

	const maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;

	const moveDistance = value * maxMoveDistance / total;

	component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr')
}