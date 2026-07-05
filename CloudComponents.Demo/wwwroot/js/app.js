/**
 * Download helpers for CloudComponents Demo.
 * Exported as an ES module; import via IJSRuntime.InvokeAsync("import", "./js/download.js").
 */

export function downloadText(filename, content) {
    const blob = new Blob([content], { type: "text/plain;charset=utf-8" });
    triggerDownload(blob, filename);
}

export function downloadSvgFromElement(elementId, filename, color, size) {
    const host = document.getElementById(elementId);
    if (!host) return;
    const svgEl = host.querySelector("svg");
    if (!svgEl) return;

    const clone = svgEl.cloneNode(true);

    const styleTag = document.createElementNS("http://www.w3.org/2000/svg", "style");
    styleTag.textContent = `
.amc-svg-stroke { fill: none; stroke: ${color}; stroke-width: 0.5; stroke-miterlimit: 10; }
.amc-svg-stroke._corner-rounded { stroke-linecap: round; }
.amc-svg-fill { fill: ${color}; }
`;
    clone.insertBefore(styleTag, clone.firstChild);
    clone.setAttribute("width", size);
    clone.setAttribute("height", size);
    clone.setAttribute("xmlns", "http://www.w3.org/2000/svg");

    const serialized = new XMLSerializer().serializeToString(clone);
    const blob = new Blob([serialized], { type: "image/svg+xml;charset=utf-8" });
    triggerDownload(blob, filename);
}

function triggerDownload(blob, filename) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

export function toggleTheme() {
    const html = document.documentElement;
    const current = html.getAttribute("data-theme");
    html.setAttribute("data-theme", current === "light" ? "dark" : "light");
    return html.getAttribute("data-theme");
}

export function getTheme() {
    return document.documentElement.getAttribute("data-theme") || "dark";
}
