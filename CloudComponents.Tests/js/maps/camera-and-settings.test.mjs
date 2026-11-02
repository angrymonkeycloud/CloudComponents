import test, { afterEach, mock } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => {
    world.disposeAll();
    mock.timers.reset();
});

// --- camera ---------------------------------------------------------------------------------------------------------------------------

test('centring the map takes latitude then longitude and hands the SDK longitude then latitude', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setCenter(33.89, 35.5);

    assert.deepEqual(map.cameraCalls.at(-1), { center: [35.5, 33.89] });
});

test('centring with a zoom sets the zoom too, and without one leaves it alone', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setCenter(33.89, 35.5, 14);
    controller.setCenter(34, 36);

    assert.deepEqual(map.cameraCalls.at(-2), { center: [35.5, 33.89], zoom: 14 });
    assert.equal('zoom' in map.cameraCalls.at(-1), false);
});

test('a zoom of zero is a zoom', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setCenter(0, 0, 0);

    assert.equal(map.cameraCalls.at(-1).zoom, 0);
});

test('fitting bounds hands the SDK west, south, east, north with the padding asked for', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setBounds(33, 35, 34, 36, 25);
    controller.setBounds(33, 35, 34, 36);

    assert.deepEqual(map.cameraCalls.at(-2), { bounds: [35, 33, 36, 34], padding: 25 });
    assert.equal(map.cameraCalls.at(-1).padding, 40, 'default padding');
});

test('padding given per side is kept, and no side may take more than 40% of the map', async () => {
    const { map, controller } = await world.createReadyController({});
    map.canvas.clientWidth = 1000;
    map.canvas.clientHeight = 500;

    controller.setBounds(33, 35, 34, 36, { top: 50, right: 900, bottom: 10000, left: 20 });

    assert.deepEqual(map.cameraCalls.at(-1).padding, { top: 50, right: 400, bottom: 200, left: 20 });
});

test('padding that is not a number counts as none and a negative side is raised to zero', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setBounds(33, 35, 34, 36, { top: Number.NaN, right: -5, bottom: undefined, left: 10 });

    assert.deepEqual(map.cameraCalls.at(-1).padding, { top: 0, right: 0, bottom: 0, left: 10 });
});

test('pitch and bearing are set only for what was given', async () => {
    const { map, controller } = await world.createReadyController({});
    map.cameraCalls.length = 0;

    controller.setCameraOrientation(45, null);
    controller.setCameraOrientation(null, 90);
    controller.setCameraOrientation(0, 0);
    controller.setCameraOrientation(null, null);

    assert.deepEqual(map.cameraCalls, [{ pitch: 45 }, { bearing: 90 }, { pitch: 0, bearing: 0 }]);
});

test('a camera move made by code is allowed to move the pin, and the permission lapses after five seconds', async () => {
    mock.timers.enable({ apis: ['setTimeout'] });
    const { controller } = await world.createReadyController({});

    controller.setCenter(33.89, 35.5);
    assert.equal(controller._allowZoomCenterChange, true);

    mock.timers.tick(4999);
    assert.equal(controller._allowZoomCenterChange, true);
    mock.timers.tick(2);
    assert.equal(controller._allowZoomCenterChange, false);
});

// --- style, traffic -------------------------------------------------------------------------------------------------------------------

test('setting the style sends it to the SDK every time, even when it looks unchanged', async () => {
    const { map, controller } = await world.createReadyController({ style: 'road' });

    controller.setStyle('road');
    controller.setStyle('satellite');

    assert.deepEqual(map.styleCalls, [{ style: 'road' }, { style: 'satellite' }]);
    assert.equal(controller._options.style, 'satellite');
});

test('setting no style does nothing', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setStyle('');
    controller.setStyle(null);

    assert.equal(map.styleCalls.length, 0);
});

test('traffic can be switched on and off for flow and incidents separately', async () => {
    const { map, controller } = await world.createReadyController({});
    map.trafficCalls.length = 0;

    controller.setTraffic(true, false);
    controller.setTraffic(false, true);
    controller.setTraffic(false, false);

    assert.deepEqual(map.trafficCalls, [
        { flow: 'relative', incidents: false },
        { flow: 'none', incidents: true },
        { flow: 'none', incidents: false }
    ]);
});

// --- interactions and controls -------------------------------------------------------------------------------------------------------

test('interactions are pushed to the SDK as one set, each on unless switched off', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setInteractions({ dragPan: false, touch: false });

    assert.deepEqual(map.interactionCalls.at(-1), {
        dragPanInteraction: false,
        dragRotateInteraction: true,
        scrollZoomInteraction: true,
        dblClickZoomInteraction: true,
        boxZoomInteraction: true,
        keyboardInteraction: true,
        touchInteraction: false
    });
});

test('in centre-pin mode scroll zoom is always handled by the library whatever was asked', async () => {
    const { map, controller } = await world.createReadyController({ addMarkerTrigger: 'center' });

    controller.setInteractions({ scrollZoom: true });

    assert.equal(map.interactionCalls.at(-1).scrollZoomInteraction, false);
});

test('changing interactions with nothing given does nothing', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setInteractions(null);

    assert.equal(map.interactionCalls.length, 0);
});

test('controls are diffed: only what changed is added, moved or removed', async () => {
    const { map, controller } = await world.createReadyController({
        controls: { zoom: { enabled: true, position: 'top-right' }, scale: { enabled: true, position: 'bottom-left' } }
    });
    const zoomInstance = controller._controlInstances.zoom.control;

    controller.setControls({
        zoom: { enabled: true, position: 'top-right' },            // unchanged: kept as is
        scale: { enabled: true, position: 'bottom-right' },        // moved: re-added there
        compass: { enabled: true, position: 'top-left' }           // new
    });

    assert.equal(controller._controlInstances.zoom.control, zoomInstance, 'an unchanged control is not rebuilt');
    assert.equal(controller._controlInstances.scale.position, 'bottom-right');
    assert.equal(controller._controlInstances.compass.position, 'top-left');
    assert.equal(map.controlsList.length, 3);
    assert.equal(map.controlsList.filter(entry => entry.control.kind === 'ScaleControl').length, 1, 'moving removes the old one');
});

test('a control switched off is removed', async () => {
    const { map, controller } = await world.createReadyController({ controls: { zoom: { enabled: true, position: 'top-right' } } });

    controller.setControls({ zoom: { enabled: false } });

    assert.equal(map.controlsList.length, 0);
    assert.equal(controller._controlInstances.zoom, undefined);
});

test('a control missing its configuration counts as off', async () => {
    const { map, controller } = await world.createReadyController({ controls: { zoom: { enabled: true } } });

    controller.setControls({ zoom: null });

    assert.equal(map.controlsList.length, 0);
});

test('changing controls with nothing given does nothing', async () => {
    const { map, controller } = await world.createReadyController({ controls: { zoom: { enabled: true } } });

    controller.setControls(null);

    assert.equal(map.controlsList.length, 1);
});

// --- disposal -------------------------------------------------------------------------------------------------------------------------

test('disposing removes everything the controller added and releases the map', async () => {
    const { map, controller } = await world.createReadyController({ controls: { zoom: { enabled: true } }, latitude: 33, longitude: 35 });
    controller.addMarker({ id: 'a', latitude: 33, longitude: 35 });
    controller.addRegion({ id: 'r', coordinates: [[[0, 0], [1, 0], [1, 1], [0, 0]]] });
    controller.setLocationLock([[[[0, 0], [1, 0], [1, 1], [0, 0]]]], { showBoundary: true });

    controller.dispose();

    assert.equal(map.disposed, true);
    assert.equal(map.controlsList.length, 0);
    assert.equal(controller._markers.size, 0);
    assert.equal(controller._map, null);
    assert.equal(controller._dotNetRef, null);
});

test('after disposal a late SDK event cannot reach the page', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({});
    dotNetRef.clear();

    controller.dispose();
    map.fire('click', { position: [35.5, 33.9], pixel: [1, 1] });

    assert.equal(dotNetRef.calls.length, 0);
});
