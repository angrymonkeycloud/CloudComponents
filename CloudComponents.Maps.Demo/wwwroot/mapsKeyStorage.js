// mapsKeyStorage.js
// Provides secure SessionStorage access for Azure Maps subscription key
// SessionStorage is cleared when the browser tab/window is closed

const STORAGE_KEY = 'azure_maps_subscription_key';

export function getAzureMapsKey() {
    try {
        const key = sessionStorage.getItem(STORAGE_KEY);
        return key || null;
    } catch (e) {
        console.error('Error reading from sessionStorage:', e);
        return null;
    }
}

export function setAzureMapsKey(key) {
    try {
        if (key && typeof key === 'string') {
            sessionStorage.setItem(STORAGE_KEY, key);
        }
    } catch (e) {
        console.error('Error writing to sessionStorage:', e);
    }
}

export function clearAzureMapsKey() {
    try {
        sessionStorage.removeItem(STORAGE_KEY);
    } catch (e) {
        console.error('Error clearing sessionStorage:', e);
    }
}

export function hasAzureMapsKey() {
    try {
        return sessionStorage.getItem(STORAGE_KEY) !== null;
    } catch (e) {
        console.error('Error checking sessionStorage:', e);
        return false;
    }
}
