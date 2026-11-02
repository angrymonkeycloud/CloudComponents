import test, { afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => world.disposeAll());

test('a map is built from the options with the documented defaults', async () => {
    const { map } = await world.createController({ latitude: 33.89, longitude: 35.5, zoom: 10, subscriptionKey: 'abc' });

    assert.deepEqual(map.options.center, [35.5, 33.89], 'the SDK takes [longitude, latitude]');
    assert.equal(map.options.zoom, 10);
    assert.equal(map.options.pitch, 0);
    assert.equal(map.options.bearing, 0);
    assert.equal(map.options.style, 'road');
    assert.equal(map.options.language, 'en-US');
    assert.equal(map.options.view, 'Auto');
    assert.equal(map.options.interactive, true);
    assert.deepEqual(map.options.authOptions, { authType: 'subscriptionKey', subscriptionKey: 'abc' });
});

test('the map is built on the element the component rendered', async () => {
    const { map } = await world.createController({ elementId: 'azmap-123' });

    assert.equal(map.elementId, 'azmap-123');
});

test('the style, language, view, pitch and bearing asked for are passed to the SDK', async () => {
    const { map } = await world.createController({ style: 'satellite_road_labels', language: 'ar', view: 'LB', pitch: 45, bearing: 90 });

    assert.equal(map.options.style, 'satellite_road_labels');
    assert.equal(map.options.language, 'ar');
    assert.equal(map.options.view, 'LB');
    assert.equal(map.options.pitch, 45);
    assert.equal(map.options.bearing, 90);
});

test('a zoom limit reaches the SDK only when one was given', async () => {
    const free = await world.createController({});
    const limited = await world.createController({ minZoom: 3, maxZoom: 18 });

    assert.equal('minZoom' in free.map.options, false);
    assert.equal('maxZoom' in free.map.options, false);
    assert.equal(limited.map.options.minZoom, 3);
    assert.equal(limited.map.options.maxZoom, 18);
});

test('a zero minimum zoom is a limit and is not dropped', async () => {
    const { map } = await world.createController({ minZoom: 0, maxZoom: 0 });

    assert.equal(map.options.minZoom, 0);
    assert.equal(map.options.maxZoom, 0);
});

test('an interaction is switched off in the SDK only when it was switched off', async () => {
    const defaults = await world.createController({});
    const restricted = await world.createController({
        interactions: { dragPan: false, dragRotate: false, scrollZoom: false, dblClickZoom: false, boxZoom: false, keyboard: false, touch: false }
    });
    const flags = ['dragPanInteraction', 'dragRotateInteraction', 'scrollZoomInteraction', 'dblClickZoomInteraction', 'boxZoomInteraction', 'keyboardInteraction', 'touchInteraction'];

    for (const flag of flags) {
        assert.equal(flag in defaults.map.options, false, `${flag} is left to the SDK by default`);
        assert.equal(restricted.map.options[flag], false, `${flag} is off when asked`);
    }
});

test('one interaction can be switched off without touching the others', async () => {
    const { map } = await world.createController({ interactions: { touch: false } });

    assert.equal(map.options.touchInteraction, false);
    assert.equal('dragPanInteraction' in map.options, false);
});

test('a map that is not interactive says so to the SDK', async () => {
    const { map } = await world.createController({ interactive: false });

    assert.equal(map.options.interactive, false);
});

test('the controller exposes every operation the component calls', async () => {
    const { controller } = await world.createController({});
    const operations = [
        'addMarker', 'removeMarker', 'clearMarkers', 'addRegion', 'addMergedRegion', 'removeRegion', 'clearRegions', 'setRoutes', 'clearRoutes',
        'setTimelines', 'clearTimelines', 'setLocationLock', 'clearLocationLock', 'isPointAllowed', 'geocode', 'search', 'searchMunicipalities',
        'reverseGeocode', 'getPolygon', 'getBoundaryPolygon', 'resolveBoundary', 'getBrowserLocation', 'setAddMarkerTrigger', 'setCenter',
        'setStyle', 'setTraffic', 'setInteractions', 'setCameraOrientation', 'setBounds', 'showCurrentLocation', 'setControls', 'dispose'
    ];

    for (const operation of operations)
        assert.equal(typeof controller[operation], 'function', `${operation} is part of the contract with the Blazor component`);
});

test('the module exports the entry points the Blazor side imports', () => {
    for (const name of ['createMap', 'getCurrentLocation', 'queryLocationPermission'])
        assert.equal(typeof world.mapInterop[name], 'function', name);
});
