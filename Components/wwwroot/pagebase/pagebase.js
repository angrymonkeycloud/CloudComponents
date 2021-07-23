function addMeta(name, content) {
    var keywordsMeta = document.createElement('meta');
    keywordsMeta.name = name;
    keywordsMeta.content = content;
    document.getElementsByTagName('head')[0].appendChild(keywordsMeta);
}
export function updateTitle(data) {
    document.title = data.Title;
    if (data.Description)
        addMeta('description', data.Description);
    if (data.Keywords)
        addMeta('keywords', data.Keywords);
    data.Scripts.forEach(function (script) {
        var scriptElement = document.createElement('script');
        if (script.Content)
            scriptElement.textContent = script.Content;
        if (script.Src)
            scriptElement.src = script.Src;
        document.getElementsByTagName('head')[0].appendChild(scriptElement);
    });
}
