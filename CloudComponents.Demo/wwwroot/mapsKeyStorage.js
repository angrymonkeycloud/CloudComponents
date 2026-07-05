const STORAGE_KEY = 'azure_maps_subscription_key';

export function getAzureMapsKey() {
    try {
        return sessionStorage.getItem(STORAGE_KEY) || null;
    } catch (e) {
        return null;
    }
}

export function setAzureMapsKey(key) {
    try {
        if (key && typeof key === 'string') sessionStorage.setItem(STORAGE_KEY, key);
    } catch (e) { }
}

export function clearAzureMapsKey() {
    try {
        sessionStorage.removeItem(STORAGE_KEY);
    } catch (e) { }
}

export function hasAzureMapsKey() {
    try {
        return sessionStorage.getItem(STORAGE_KEY) !== null;
    } catch (e) {
        return false;
    }
}
