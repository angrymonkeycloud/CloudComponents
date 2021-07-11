import { dotNetHelper } from '../General/js/DotNet';

function mouseMove(component: HTMLElement, moveArgs: MouseEvent, maxMoveDistance: number) {

	let moveDistance = moveArgs.clientX - component.getBoundingClientRect().left - (component.querySelector('.amc-progressbar-middle').clientWidth / 2);

	if (moveDistance < 0)
		moveDistance = 0;

	if (moveDistance > maxMoveDistance)
		moveDistance = maxMoveDistance;

	document.title = moveDistance.toString();

	component.style.setProperty('grid-template-columns', moveDistance + 'px max-content 1fr')
}

export function mouseDown(component: HTMLElement) {

	const maxMoveDistance = component.clientWidth - component.querySelector('.amc-progressbar-middle').clientWidth;

	const listener = function (moveArgs: MouseEvent) { mouseMove(component, moveArgs, maxMoveDistance); };

	document.addEventListener('mousemove', listener);

	document.onmouseup = function () {
		document.removeEventListener('mousemove', listener);

		//dotNetHelper.invokeMethodAsync("Test");
	}
}