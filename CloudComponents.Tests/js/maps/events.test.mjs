import test, { afterEach, mock } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld, FakeElement } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => {
    world.disposeAll();
    mock.timers.reset();
});

const BEIRUT = { lat: 33.8938, lng: 35.5018 };
const click = (map, { lat = BEIRUT.lat, lng = BEIRUT.lng, pixel = [100, 100], target } = {}) =>
    map.fire('click', { position: [lng, lat], pixel, originalEvent: target ? { target } : undefined });

const square = (south, west, north, east) => [[[west, south], [east, south], [east, north], [west, north], [west, south]]];

// --- ready ---------------------------------------------------------------------------------------------------------------------------

test('the page is told when the map has finished loading, once', async () => {
    const { map, dotNetRef } = await world.createController({});

    assert.equal(dotNetRef.to('NotifyMapReadyAsync').length, 0, 'not before the SDK says it is ready');
    map.fire('ready');

    assert.equal(dotNetRef.to('NotifyMapReadyAsync').length, 1);
});

test('only the enabled controls are added, each at its position', async () => {
    const { map } = await world.createReadyController({
        controls: {
            zoom: { enabled: true, position: 'top-right' },
            compass: { enabled: false, position: 'top-right' },
            scale: { enabled: true, position: 'bottom-left' },
            fullscreen: { enabled: true }
        }
    });

    assert.deepEqual(map.controlsList.map(entry => [entry.control.kind, entry.options.position]), [
        ['ZoomControl', 'top-right'],
        ['ScaleControl', 'bottom-left'],
        ['FullscreenControl', 'top-right']
    ]);
});

test('an unknown control key is ignored without breaking the others', async () => {
    const { map } = await world.createReadyController({ controls: { teleport: { enabled: true }, zoom: { enabled: true } } });

    assert.deepEqual(map.controlsList.map(entry => entry.control.kind), ['ZoomControl']);
});

test('the style control lists every style, including both satellite ones', async () => {
    const { map } = await world.createReadyController({ controls: { style: { enabled: true, position: 'top-left' } } });

    const control = map.controlsList[0].control;
    assert.equal(control.kind, 'StyleControl');
    assert.equal(control.options.layout, 'list');
    assert.equal(control.options.mapStyles.length, 9);
    assert.ok(control.options.mapStyles.includes('satellite'));
    assert.ok(control.options.mapStyles.includes('satellite_road_labels'));
});

test('traffic is off unless asked for', async () => {
    const off = await world.createReadyController({});
    const on = await world.createReadyController({ showTrafficFlow: true, showTrafficIncidents: true });

    assert.deepEqual(off.map.trafficCalls, [{ flow: 'none', incidents: false }]);
    assert.deepEqual(on.map.trafficCalls, [{ flow: 'relative', incidents: true }]);
});

test('a map opened on a real place shows the current-location dot there, and one opened on 0,0 does not', async () => {
    const placed = await world.createReadyController({ latitude: 33.89, longitude: 35.5 });
    const unplaced = await world.createReadyController({ latitude: 0, longitude: 0 });

    assert.equal(placed.map.markerList.length, 1);
    assert.deepEqual(placed.map.markerList[0].options.position, [35.5, 33.89]);
    assert.equal(unplaced.map.markerList.length, 0);
});

// --- clicks --------------------------------------------------------------------------------------------------------------------------

test('a click on the map is reported with its latitude and longitude', async () => {
    const { map, dotNetRef } = await world.createReadyController({});

    click(map);

    assert.deepEqual(dotNetRef.to('NotifyMapClickAsync'), [[BEIRUT.lat, BEIRUT.lng]]);
});

test('a click without a position is ignored', async () => {
    const { map, dotNetRef } = await world.createReadyController({});

    map.fire('click', {});
    map.fire('click', undefined);

    assert.equal(dotNetRef.calls.length, 1, 'only the ready notification');
});

test('a click that lands on a marker is not reported as a click on the map', async () => {
    const { map, dotNetRef } = await world.createReadyController({});
    const markerElement = new FakeElement();
    markerElement.closest = selector => selector.includes('atlas-map-htmlMarker') ? markerElement : null;

    click(map, { target: markerElement });

    assert.equal(dotNetRef.to('NotifyMapClickAsync').length, 0);
});

test('a single click drops a marker only when the trigger is single-click', async () => {
    const single = await world.createReadyController({ addMarkerTrigger: 'single' });
    const double = await world.createReadyController({ addMarkerTrigger: 'double' });
    const disabled = await world.createReadyController({});

    click(single.map);
    click(double.map);
    click(disabled.map);

    assert.deepEqual(single.dotNetRef.to('NotifyMapAddMarkerAsync'), [[BEIRUT.lat, BEIRUT.lng]]);
    assert.equal(double.dotNetRef.to('NotifyMapAddMarkerAsync').length, 0);
    assert.equal(disabled.dotNetRef.to('NotifyMapAddMarkerAsync').length, 0);
});

test('a double click drops a marker only when the trigger is double-click', async () => {
    const double = await world.createReadyController({ addMarkerTrigger: 'double' });
    const single = await world.createReadyController({ addMarkerTrigger: 'single' });

    double.map.fire('dblclick', { position: [BEIRUT.lng, BEIRUT.lat] });
    single.map.fire('dblclick', { position: [BEIRUT.lng, BEIRUT.lat] });

    assert.deepEqual(double.dotNetRef.to('NotifyMapAddMarkerAsync'), [[BEIRUT.lat, BEIRUT.lng]]);
    assert.equal(single.dotNetRef.to('NotifyMapAddMarkerAsync').length, 0);
    assert.equal(single.dotNetRef.to('NotifyMapDoubleClickAsync').length, 1, 'the double click itself is still reported');
});

test('two quick taps close together count as a double click even where the browser never raises one', async () => {
    const { map, dotNetRef } = await world.createReadyController({});

    click(map, { pixel: [100, 100] });
    click(map, { pixel: [104, 102] });

    assert.equal(dotNetRef.to('NotifyMapDoubleClickAsync').length, 1);
    assert.equal(dotNetRef.to('NotifyMapClickAsync').length, 2, 'both taps are still clicks');
});

test('two clicks far apart are not a double click', async () => {
    const { map, dotNetRef } = await world.createReadyController({});

    click(map, { pixel: [100, 100] });
    click(map, { pixel: [300, 300] });

    assert.equal(dotNetRef.to('NotifyMapDoubleClickAsync').length, 0);
});

test('one double-click gesture is reported once even when both the native event and the tap pair fire', async () => {
    const { map, dotNetRef } = await world.createReadyController({});

    click(map, { pixel: [100, 100] });
    click(map, { pixel: [101, 100] });
    map.fire('dblclick', { position: [BEIRUT.lng, BEIRUT.lat] });

    assert.equal(dotNetRef.to('NotifyMapDoubleClickAsync').length, 1);
});

test('a double click right after a touch is not reported, because touch devices use press-and-hold', async () => {
    const { map, dotNetRef } = await world.createReadyController({});

    map.fire('touchstart', { position: [BEIRUT.lng, BEIRUT.lat], pixel: [10, 10], originalEvent: { touches: [{}] } });
    map.fire('touchend', {});
    map.fire('dblclick', { position: [BEIRUT.lng, BEIRUT.lat] });

    assert.equal(dotNetRef.to('NotifyMapDoubleClickAsync').length, 0);
});

// --- press and hold ------------------------------------------------------------------------------------------------------------------

const touchstart = (map, { pixel = [50, 50], touches = 1 } = {}) =>
    map.fire('touchstart', { position: [BEIRUT.lng, BEIRUT.lat], pixel, originalEvent: { touches: Array.from({ length: touches }, () => ({})) } });

test('holding a finger on the map for over half a second reports a long press', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map, dotNetRef } = await world.createReadyController({});

    touchstart(map);
    mock.timers.tick(549);
    assert.equal(dotNetRef.to('NotifyMapLongPressAsync').length, 0, 'not yet');
    mock.timers.tick(2);

    assert.deepEqual(dotNetRef.to('NotifyMapLongPressAsync'), [[BEIRUT.lat, BEIRUT.lng]]);
});

test('lifting the finger early, moving it, or adding a second finger cancels the long press', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map, dotNetRef } = await world.createReadyController({});

    touchstart(map);
    map.fire('touchend', {});
    mock.timers.tick(600);

    touchstart(map, { pixel: [50, 50] });
    map.fire('touchmove', { pixel: [70, 50], originalEvent: { touches: [{}] } });
    mock.timers.tick(600);

    touchstart(map);
    map.fire('touchmove', { pixel: [50, 50], originalEvent: { touches: [{}, {}] } });
    mock.timers.tick(600);

    touchstart(map, { touches: 2 });
    mock.timers.tick(600);

    assert.equal(dotNetRef.to('NotifyMapLongPressAsync').length, 0);
});

test('a small wobble of the finger does not cancel the long press', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map, dotNetRef } = await world.createReadyController({});

    touchstart(map, { pixel: [50, 50] });
    map.fire('touchmove', { pixel: [54, 53], originalEvent: { touches: [{}] } });
    mock.timers.tick(600);

    assert.equal(dotNetRef.to('NotifyMapLongPressAsync').length, 1);
});

test('the click that follows a long press is swallowed', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map, dotNetRef } = await world.createReadyController({});

    touchstart(map);
    mock.timers.tick(600);
    click(map);

    assert.equal(dotNetRef.to('NotifyMapClickAsync').length, 0);
});

test('a long press on a marker is not a long press on the map', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map, dotNetRef } = await world.createReadyController({});
    const markerElement = new FakeElement();
    markerElement.closest = () => markerElement;

    map.fire('touchstart', { position: [BEIRUT.lng, BEIRUT.lat], pixel: [1, 1], originalEvent: { touches: [{}], target: markerElement } });
    mock.timers.tick(600);

    assert.equal(dotNetRef.to('NotifyMapLongPressAsync').length, 0);
});

// --- location lock --------------------------------------------------------------------------------------------------------------------

test('a marker dropped outside the locked area is refused and reported as refused', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'single' });
    controller.setLocationLock([square(33, 35, 34, 36)], {});

    click(map, { lat: 40, lng: 10 });
    click(map, { lat: 33.5, lng: 35.5, pixel: [400, 400] });

    assert.deepEqual(dotNetRef.to('NotifyLocationLockRejectedAsync'), [[40, 10]]);
    assert.deepEqual(dotNetRef.to('NotifyMapAddMarkerAsync'), [[33.5, 35.5]]);
});

test('a double click outside the locked area is refused too', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'double' });
    controller.setLocationLock([square(33, 35, 34, 36)], {});

    map.fire('dblclick', { position: [10, 40] });

    assert.equal(dotNetRef.to('NotifyMapAddMarkerAsync').length, 0);
    assert.deepEqual(dotNetRef.to('NotifyLocationLockRejectedAsync'), [[40, 10]]);
});

// --- centre pin ------------------------------------------------------------------------------------------------------------------------

test('a map that starts in centre-pin mode reports where the pin is as soon as it is ready', async () => {
    const { dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'center', latitude: 33.89, longitude: 35.5 });

    assert.deepEqual(dotNetRef.to('NotifyCenterPinChangedAsync'), [[33.89, 35.5]]);
});

test('the pin moves only when the person drags the map, not when the camera is moved by code or by zooming', async () => {
    const { map, dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'center', latitude: 33.89, longitude: 35.5 });
    dotNetRef.clear();

    map.setCamera({ center: [36, 34] });
    map.fire('moveend');
    assert.equal(dotNetRef.to('NotifyCenterPinChangedAsync').length, 0, 'a programmatic move is not a new selection');

    map.fire('dragstart');
    map.setCamera({ center: [35.7, 33.95] });
    map.fire('moveend');

    assert.deepEqual(dotNetRef.to('NotifyCenterPinChangedAsync'), [[33.95, 35.7]]);
});

test('switching to centre-pin mode later reports the pin straight away and stops the SDK from scroll-zooming off-centre', async () => {
    const { controller, map, dotNetRef } = await world.createReadyController({ latitude: 33.89, longitude: 35.5, interactions: { scrollZoom: true } });
    dotNetRef.clear();

    controller.setAddMarkerTrigger('center');

    assert.deepEqual(dotNetRef.to('NotifyCenterPinChangedAsync'), [[33.89, 35.5]]);
    assert.deepEqual(map.interactionCalls.at(-1), { scrollZoomInteraction: false });
});

test('switching away from centre-pin mode gives scroll-zoom back and stops reporting the pin', async () => {
    const { controller, map, dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'center', interactions: { scrollZoom: true }, latitude: 33.89, longitude: 35.5 });
    dotNetRef.clear();

    controller.setAddMarkerTrigger('single');
    map.fire('dragstart');
    map.fire('moveend');

    assert.deepEqual(map.interactionCalls.at(-1), { scrollZoomInteraction: true });
    assert.equal(dotNetRef.to('NotifyCenterPinChangedAsync').length, 0);
});

test('a pin dragged out of the locked area snaps back to the last allowed place and the refusal is reported', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'center', latitude: 33.5, longitude: 35.5 });
    controller.setLocationLock([square(33, 35, 34, 36)], {});
    dotNetRef.clear();

    map.fire('dragstart');
    map.setCamera({ center: [10, 40] });
    map.cameraCalls.length = 0;
    map.fire('moveend');

    assert.deepEqual(dotNetRef.to('NotifyLocationLockRejectedAsync'), [[40, 10]]);
    assert.equal(dotNetRef.to('NotifyCenterPinChangedAsync').length, 0);
    assert.deepEqual(map.cameraCalls[0], { center: [35.5, 33.5], type: 'ease', duration: 200 });
});

test('a pin that starts outside the lock is left where it is so the person can drag it in', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({ addMarkerTrigger: 'center', latitude: 10, longitude: 10 });
    controller.setLocationLock([square(33, 35, 34, 36)], {});
    map.cameraCalls.length = 0;
    dotNetRef.clear();

    map.fire('dragstart');
    map.fire('moveend');

    assert.equal(map.cameraCalls.length, 0, 'no snap back to a place that is itself outside the lock');
    assert.equal(dotNetRef.to('NotifyLocationLockRejectedAsync').length, 1);
});

// --- style and errors -------------------------------------------------------------------------------------------------------------

test('a style change made inside the map is reported to the page', async () => {
    const { map, dotNetRef } = await world.createReadyController({ style: 'road' });

    map.currentStyle = 'satellite';
    map.fire('styledata');

    assert.deepEqual(dotNetRef.to('NotifyStyleChangedAsync'), [['satellite']]);
});

test('a style event that changes nothing is not reported', async () => {
    const { map, dotNetRef } = await world.createReadyController({ style: 'road' });

    map.fire('styledata');

    assert.equal(dotNetRef.to('NotifyStyleChangedAsync').length, 0);
});

test('an SDK error is reported with its message', async () => {
    const { map, dotNetRef } = await world.createReadyController({ style: 'road' });

    map.fire('error', { message: 'Network down' });
    map.fire('error', { error: { message: 'Nested message' } });
    map.fire('error', {});

    assert.deepEqual(dotNetRef.to('NotifyMapErrorAsync'), [['Network down'], ['Nested message'], ['Azure Maps error']]);
});

test('imagery refused for the pricing tier drops the map back to the road style', async () => {
    const { map, dotNetRef } = await world.createReadyController({ style: 'satellite' });

    map.fire('error', { message: 'Failed to load imagery tile: 403' });

    assert.deepEqual(map.styleCalls, [{ style: 'road' }]);
    assert.equal(dotNetRef.to('NotifyMapErrorAsync').length, 1);
});

test('a road map is not restyled by a 403 because there is nothing to fall back to', async () => {
    const { map } = await world.createReadyController({ style: 'road' });

    map.fire('error', { message: 'HTTP 403' });

    assert.equal(map.styleCalls.length, 0);
});

// --- scroll gate -----------------------------------------------------------------------------------------------------------------------

/** The "hold Ctrl to zoom" hint, if one is showing: the only thing besides the canvas the gate puts in the map container. */
const hints = map => map.container.children.filter(child => child !== map.canvas);

const wheel = (map, props = {}) => {
    const event = { deltaY: 100, ctrlKey: false, metaKey: false, shiftKey: false, stopped: false, prevented: false, ...props };
    event.stopImmediatePropagation = () => { event.stopped = true; };
    event.preventDefault = () => { event.prevented = true; };
    map.canvas.dispatch('wheel', event);
    return event;
};

test('a plain scroll over a map that has scroll-zoom off leaves the page scrolling and shows a hint once', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map } = await world.createReadyController({ interactions: { scrollZoom: false } });

    const first = wheel(map);
    wheel(map);

    assert.equal(first.prevented, false);
    assert.equal(map.interactionCalls.length, 0);
    assert.equal(hints(map).length, 1, 'one hint element, reused');
    assert.match(hints(map)[0].textContent, /scroll to zoom/);
});

test('the hint goes away on its own', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map } = await world.createReadyController({ interactions: { scrollZoom: false } });

    wheel(map);
    mock.timers.tick(1500);

    assert.equal(hints(map).length, 0);
});

test('holding a modifier lets one gesture zoom the map and then switches scroll-zoom off again', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map } = await world.createReadyController({ interactions: { scrollZoom: false } });

    wheel(map, { ctrlKey: true });
    assert.deepEqual(map.interactionCalls.at(-1), { scrollZoomInteraction: true });
    mock.timers.tick(401);

    assert.deepEqual(map.interactionCalls.at(-1), { scrollZoomInteraction: false });
});

test('shift and the meta key also count as the zoom modifier', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { map } = await world.createReadyController({ interactions: { scrollZoom: false } });

    wheel(map, { shiftKey: true });
    wheel(map, { metaKey: true });

    assert.deepEqual(map.interactionCalls, [{ scrollZoomInteraction: true }, { scrollZoomInteraction: true }]);
});

test('a map with scroll-zoom on is left entirely to the SDK', async () => {
    const { map } = await world.createReadyController({ interactions: { scrollZoom: true } });

    const event = wheel(map);

    assert.equal(event.stopped, false);
    assert.equal(map.interactionCalls.length, 0);
    assert.equal(hints(map).length, 0);
});

test('in centre-pin mode every scroll zooms around the centre and is kept from the SDK', async () => {
    const { map } = await world.createReadyController({ addMarkerTrigger: 'center', latitude: 33.89, longitude: 35.5, zoom: 10 });
    map.cameraCalls.length = 0;

    const zoomOut = wheel(map, { deltaY: 100 });
    const zoomIn = wheel(map, { deltaY: -100 });

    assert.equal(zoomOut.stopped && zoomOut.prevented, true);
    assert.deepEqual(map.cameraCalls[0], { zoom: 9, center: [35.5, 33.89], type: 'ease', duration: 150 });
    assert.equal(zoomIn.prevented, true);
    assert.equal(map.cameraCalls[1].zoom, 10, 'back up from 9');
});

test('a pinch gesture zooms in finer steps and the zoom never leaves 0 to 24', async () => {
    const { map } = await world.createReadyController({ addMarkerTrigger: 'center', latitude: 33.89, longitude: 35.5, zoom: 10 });
    map.cameraCalls.length = 0;

    wheel(map, { deltaY: -100, ctrlKey: true });
    assert.equal(map.cameraCalls[0].zoom, 10.25);

    map.camera.zoom = 24;
    wheel(map, { deltaY: -100 });
    assert.equal(map.cameraCalls[1].zoom, 24, 'capped at 24');

    map.camera.zoom = 0;
    wheel(map, { deltaY: 100 });
    assert.equal(map.cameraCalls[2].zoom, 0, 'floored at 0');
});
