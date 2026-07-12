/**
 * CloudDataGrid — browser-side helpers loaded lazily via import().
 * Exposed as an ES module so the Razor class library can call
 * JSRuntime.InvokeVoidAsync after importing the module.
 */

export function downloadTextFile(fileName, content, contentType) {
    const blob = new Blob([content], { type: contentType || 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
}
