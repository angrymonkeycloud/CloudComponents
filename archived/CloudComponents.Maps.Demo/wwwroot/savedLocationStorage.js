// savedLocationStorage.js
// Persists a single user-picked map location (latitude/longitude/label) using
// browser LocalStorage, so it survives across tabs and browser restarts.
// Swap this module (and SavedLocationService) for a real backend call in a
// production app — the Blazor-facing API shape would stay the same.

const STORAGE_KEY = 'cloudmaps_saved_location';

export function getSavedLocation() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        return raw ? JSON.parse(raw) : null;
    } catch (e) {
        console.error('Error reading saved location from localStorage:', e);
        return null;
    }
}

export function setSavedLocation(location) {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(location));
    } catch (e) {
        console.error('Error writing saved location to localStorage:', e);
    }
}

export function clearSavedLocation() {
    try {
        localStorage.removeItem(STORAGE_KEY);
    } catch (e) {
        console.error('Error clearing saved location from localStorage:', e);
    }
}
