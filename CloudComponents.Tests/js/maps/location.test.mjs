import test, { afterEach, mock } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => {
    world.disposeAll();
    mock.timers.reset();
});

const useNavigator = (value) => Object.defineProperty(globalThis, 'navigator', { value, configurable: true, writable: true });
const position = (latitude, longitude, accuracy) => ({ coords: { latitude, longitude, accuracy } });

// --- permission ------------------------------------------------------------------------------------------------------------------------

test('a browser without geolocation reports it as unsupported', async () => {
    useNavigator({});

    assert.equal(await world.mapInterop.queryLocationPermission(), 'unsupported');
});

test('a browser that cannot be asked about permission is treated as one that would prompt', async () => {
    useNavigator({ geolocation: {} });

    assert.equal(await world.mapInterop.queryLocationPermission(), 'prompt');
});

test('the permission state is passed through as the browser gave it', async () => {
    for (const state of ['granted', 'denied', 'prompt']) {
        useNavigator({ geolocation: {}, permissions: { query: async ({ name }) => { assert.equal(name, 'geolocation'); return { state }; } } });

        assert.equal(await world.mapInterop.queryLocationPermission(), state);
    }
});

test('a permission query that throws is treated as a prompt', async () => {
    useNavigator({ geolocation: {}, permissions: { query: async () => { throw new Error('nope'); } } });

    assert.equal(await world.mapInterop.queryLocationPermission(), 'prompt');
});

// --- current location --------------------------------------------------------------------------------------------------------------

test('without geolocation there is no location', async () => {
    useNavigator({});

    assert.equal(await world.mapInterop.getCurrentLocation(), null);
});

test('a fix accurate to 50 metres is taken at once and the watch is stopped', async () => {
    let cleared = null;
    useNavigator({
        geolocation: {
            // Browsers answer asynchronously, after the watch has been registered.
            getCurrentPosition: (success) => queueMicrotask(() => success(position(33.89, 35.5, 20))),
            watchPosition: () => 7,
            clearWatch: id => { cleared = id; }
        }
    });

    assert.deepEqual(await world.mapInterop.getCurrentLocation(), [33.89, 35.5]);
    assert.equal(cleared, 7);
});

test('a rough first fix is refined by the watch, and the most accurate one wins when time runs out', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    let watcher;
    useNavigator({
        geolocation: {
            getCurrentPosition: (success) => success(position(33.0, 35.0, 5000)),
            watchPosition: (success) => { watcher = success; return 1; },
            clearWatch: () => { }
        }
    });

    const result = world.mapInterop.getCurrentLocation();
    watcher(position(33.5, 35.5, 300));
    watcher(position(33.9, 35.9, 900));   // worse than the best so far: ignored
    mock.timers.tick(6000);

    assert.deepEqual(await result, [33.5, 35.5]);
});

test('with no fix at all it gives up after six seconds with null', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    useNavigator({ geolocation: { getCurrentPosition: () => { }, watchPosition: () => 1, clearWatch: () => { } } });

    const result = world.mapInterop.getCurrentLocation();
    let done = false;
    result.then(() => { done = true; });
    mock.timers.tick(5999);
    await Promise.resolve();
    assert.equal(done, false);
    mock.timers.tick(2);

    assert.equal(await result, null);
});

test('a refused location request ends the wait at once', async () => {
    useNavigator({ geolocation: { getCurrentPosition: (success, failure) => failure({ code: 1 }), watchPosition: () => 1, clearWatch: () => { } } });

    assert.equal(await world.mapInterop.getCurrentLocation(), null);
});

test('a browser whose watch throws still gives the first fix', async () => {
    useNavigator({
        geolocation: {
            getCurrentPosition: (success) => success(position(33.89, 35.5, 10)),
            watchPosition: () => { throw new Error('unsupported'); },
            clearWatch: () => { }
        }
    });

    assert.deepEqual(await world.mapInterop.getCurrentLocation(), [33.89, 35.5]);
});

test('the controller can also ask the browser once, with a ten-second timeout and a minute of caching', async () => {
    let seenOptions;
    useNavigator({ geolocation: { getCurrentPosition: (success, _failure, options) => { seenOptions = options; success(position(33.89, 35.5, 10)); } } });
    const { controller } = await world.createController({});

    assert.deepEqual(await controller.getBrowserLocation(), { latitude: 33.89, longitude: 35.5 });
    assert.deepEqual(seenOptions, { timeout: 10000, maximumAge: 60000 });
});

test('the controller gets null when the browser refuses or has no geolocation', async () => {
    useNavigator({ geolocation: { getCurrentPosition: (_success, failure) => failure({ code: 1 }) } });
    const refused = await world.createController({});
    assert.equal(await refused.controller.getBrowserLocation(), null);

    useNavigator({});
    const none = await world.createController({});
    assert.equal(await none.controller.getBrowserLocation(), null);
});
