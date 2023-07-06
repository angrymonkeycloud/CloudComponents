declare class Castjs {
	available: boolean;
	connected: boolean;
	device: string;
	src: string;
	title: string;
	description: string;
	poster: string;
	subtitles: object[];
	volumeLevel: number;
	muted: boolean;
	paused: boolean;
	time: number;
	timePretty: string;
	duration: number;
	durationPretty: string;
	progress: number;
	state: string;

	constructor(options?: { receiver?: string, joinpolicy?: string });

	on(eventType: string, callback: (e) => void);

	cast(source: string, metadata?: object): void;

	volume(level: number): void;

	play(): void;

	pause(): void;

	mute(): void;

	unmute(): void;

	subtitle(index: number): void;

	seek(time: number, relative?: boolean): void;

	disconnect(): void;
}

declare class DotNet {
	static invokeMethodAsync(assemblyName: string, methodIdentifier: string, ...args: any[]);
}

let cjs;
let firstCasting = true;
let castPosition: number = null;
const castJsUrl = 'https://cdnjs.cloudflare.com/ajax/libs/castjs/5.2.0/cast.min.js';

async function loadJs(sourceUrl) {
	return new Promise((resolve, reject) => {

		var scripts = document.getElementsByTagName('script');

		for (var i = 0; i < scripts.length; i++)
			if (scripts[i].src == sourceUrl) {
				resolve(null);
				return;
			}

		var tag = document.createElement('script');
		tag.src = sourceUrl;
		tag.type = "text/javascript";

		tag.onload = function () {
			resolve(null);
		}

		tag.onerror = function () {
			console.error("Failed to load script: " + sourceUrl);
			reject("Failed to load script");
		}

		document.body.appendChild(tag);
	});
}

export async function createCastJsInstance(): Promise<any> {
	return new (window as any).Castjs();
}

export async function init() {

	console.log('init: start');

	console.log(cjs);

	await loadJs(castJsUrl);

	console.log('init: js loaded');

	if (!cjs)
		cjs = new Castjs();

	return new Promise((resolve, reject) => {

		console.log('init: before event');

		cjs.on('event', async (e) => {

			let additionalValue = null;

			let log = true;

			if (e === 'timeupdate') {
				additionalValue = cjs.time;
				log = false;
			}

			if (e === 'connect') {
				additionalValue = cjs.device;
			}

			if (log) {
				console.log('event: started');
				console.log('----------------');
				console.log(e);
				console.log('----------------');
			}

			if (e === 'available') {
				console.log('event: available');

				resolve(null);
				return;
			}

			//if (firstCasting && e === 'playing') {

			//	if (castPosition !== null) {

			//		cjs.seek(castPosition);
			//		castPosition = null;
			//	}

			//	firstCasting = false;
			//}

			await DotNet.invokeMethodAsync('AngryMonkey.Cloud.Components', 'HandleCastJsEventStatic', e, additionalValue);

			if (log)
				console.log('event: ended');
		});

		console.log('event: before error');

		cjs.on('error', (e) => {
			console.log('error: ' + e);

			reject(e);
		});

		if (cjs.available)
			resolve(null);

		console.log('init: end');
	});
}

export async function startCasting(url: string, title: string, position: number) {

	console.log('casting: started');
	
	console.log(cjs);

	let alreadyCasting = cjs.connected && cjs.src === url;

	console.log('casting: already casting = ' + alreadyCasting);

	if (!alreadyCasting) {
		firstCasting = true;

		console.log('casting: cast url');

		cjs.cast(url, {
			title: title,
		});

		console.log('casting: cast url end');
	}

	if (position !== undefined) {
		if (firstCasting)
			castPosition = position;
		else
			cjs.seek(position);
	}

	console.log('casting: before play');
	console.log('casting: Available = ' + cjs.available);
	console.log('casting: Connected = ' + cjs.connected);
	console.log(cjs);
	console.log('_____________________________________');

	cjs.play();

	console.log('casting: after play');
	console.log('casting: Available = ' + cjs.available);
	console.log('casting: Connected = ' + cjs.connected);
	console.log(cjs);
	console.log('_____________________________________');

	console.log('casting: end');
}

export function stopCasting() {
	if (cjs.available)
		cjs.disconnect();

	//cjs = null;

	//var scripts = document.getElementsByTagName('script');
	//for (var i = 0; i < scripts.length; i++)
	//	if (scripts[i].src === castJsUrl) {
	//		scripts[i].parentNode.removeChild(scripts[i]);
	//		return;
	//	}
}

export function pauseCasting() {
	if (cjs.available)
		cjs.pause();
}
export function playCasting() {
	if (cjs.available)
		cjs.play();
}