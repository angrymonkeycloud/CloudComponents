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

	console.log(cjs);
	console.log(1);

	await loadJs(castJsUrl);

	console.log(2);

	return new Promise((resolve, reject) => {

		if (!cjs)
			cjs = new Castjs();

		console.log(3);


		cjs.on('event', async (e) => {

			let additionalValue = null;

			let log = true;

			if (e === 'timeupdate') {
				additionalValue = cjs.time;
				log = false;
			}


			if (log)
				console.log(4);

			if (log)
				console.log(e);

			if (log)
				console.log(5);

			if (e === 'available') {
				console.log(6);

				resolve(null);
				return;
			}

			if (log)
				console.log(7);

			//if (firstCasting && e === 'playing') {

			//	if (castPosition !== null) {

			//		cjs.seek(castPosition);
			//		castPosition = null;
			//	}

			//	firstCasting = false;
			//}

			if (log)
				console.log(8);

			await DotNet.invokeMethodAsync('AngryMonkey.Cloud.Components', 'HandleCastJsEventStatic', e, additionalValue);

			if (log)
				console.log(9);

		});

		console.log(4.1);

		cjs.on('error', (e) => {
			console.log('error: ' + e);

			reject(e);
		});

		console.log(4.2);
	});
}

export async function startCasting(url: string, position: number) {

	console.log(100);

	console.log(cjs);

	let alreadyCasting = cjs.connected && cjs.src === url;

	console.log(101);

	if (!alreadyCasting) {
		firstCasting = true;

		console.log(102);

		cjs.cast(url, {
			title: 'Coverbox TV',
		});

		console.log(103);
	}

	if (position !== undefined) {
		if (firstCasting)
			castPosition = position;
		else
			cjs.seek(position);
	}

	console.log(104);

	cjs.play();

	console.log(105);

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