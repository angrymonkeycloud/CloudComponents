interface PageBaseData {
	Title: string
	Description?: string,
	Keywords?: string,
	Scripts?: PageBaseScript[]
}

interface PageBaseScript {
	Src: string,
	Content: string
}

function addMeta(name: string, content: string) {

	const keywordsMeta = document.createElement('meta');
	keywordsMeta.name = name;
	keywordsMeta.content = content;
	document.getElementsByTagName('head')[0].appendChild(keywordsMeta);
}

export function updateTitle(data: PageBaseData) {
	document.title = data.Title;

	if (data.Description)
		addMeta('description', data.Description);

	if (data.Keywords)
		addMeta('keywords', data.Keywords);

	data.Scripts.forEach(function (script) {
		const scriptElement = document.createElement('script');

		if (script.Content)
			scriptElement.textContent = script.Content;

		if (script.Src)
			scriptElement.src = script.Src;

		document.getElementsByTagName('head')[0].appendChild(scriptElement);
	});
}