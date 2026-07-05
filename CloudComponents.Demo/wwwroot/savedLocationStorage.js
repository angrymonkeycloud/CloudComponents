const STORAGE_KEY = 'cloudmaps_saved_location';

export function getSavedLocation() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        return raw ? JSON.parse(raw) : null;
    } catch (e) {
        return null;
    }
}

export function setSavedLocation(location) {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(location));
    } catch (e) { }
}

export function clearSavedLocation() {
    try {
        localStorage.removeItem(STORAGE_KEY);
    } catch (e) { }
}
