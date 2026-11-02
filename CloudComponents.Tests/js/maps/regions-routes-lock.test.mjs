import test, { afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => world.disposeAll());

const square = (south, west, north, east) => [[west, south], [east, south], [east, north], [west, north], [west, south]];
const ring = (south, west, north, east) => [square(south, west, north, east)];

// --- regions --------------------------------------------------------------------------------------------------------------------------

test('a region is drawn as a filled outlined polygon with the default colours', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.addRegion({ id: 'r1', coordinates: ring(33, 35, 34, 36) });

    const [source] = map.sourceList;
    const shape = source.getShapes()[0];
    assert.equal(shape.getId(), 'r1');
    assert.equal(shape.geometry.type, 'Polygon');
    assert.deepEqual(shape.geometry.coordinates, ring(33, 35, 34, 36));
    assert.deepEqual(shape.properties, { fillColor: 'rgba(0, 120, 212, 0.15)', strokeColor: '#0078d4', strokeWidth: 2 });
});

test('a region keeps the colours, width and label it was given', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.addRegion({ id: 'r1', coordinates: ring(33, 35, 34, 36), fillColor: '#ff000040', strokeColor: '#ff0000', strokeWidth: 0, label: 'Zone A' });

    assert.deepEqual(map.sourceList[0].getShapes()[0].properties, { fillColor: '#ff000040', strokeColor: '#ff0000', strokeWidth: 0, label: 'Zone A' });
});

test('the region layers are made once however many regions are added', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.addRegion({ id: 'a', coordinates: ring(33, 35, 34, 36) });
    controller.addRegion({ id: 'b', coordinates: ring(34, 35, 35, 36) });

    assert.equal(map.sourceList.length, 1);
    assert.deepEqual(map.layerList.map(layer => layer.kind), ['PolygonLayer', 'LineLayer']);
    assert.equal(map.sourceList[0].getShapes().length, 2);
});

test('removing a region takes only that one away', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addRegion({ id: 'a', coordinates: ring(33, 35, 34, 36) });
    controller.addRegion({ id: 'b', coordinates: ring(34, 35, 35, 36) });

    controller.removeRegion('a');

    assert.deepEqual(map.sourceList[0].getShapes().map(shape => shape.getId()), ['b']);
    assert.deepEqual(controller._regions, ['b']);
});

test('removing a region that is not there, or with no id, changes nothing', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addRegion({ id: 'a', coordinates: ring(33, 35, 34, 36) });

    controller.removeRegion('zzz');
    controller.removeRegion('');
    controller.removeRegion(undefined);

    assert.equal(map.sourceList[0].getShapes().length, 1);
});

test('removing a region before any exists is harmless', async () => {
    const { controller } = await world.createReadyController({});

    assert.doesNotThrow(() => controller.removeRegion('a'));
});

test('clearing the regions leaves the map with none', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addRegion({ id: 'a', coordinates: ring(33, 35, 34, 36) });

    controller.clearRegions();

    assert.equal(map.sourceList[0].getShapes().length, 0);
    assert.deepEqual(controller._regions, []);
});

test('adding nothing as a region is ignored', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.addRegion(null);

    assert.equal(map.sourceList.length, 0);
});

test('several boundaries can be drawn as one merged outline with no fill', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.addMergedRegion({
        id: 'merged',
        polygons: [ring(33, 35, 34, 36), ring(33.5, 35.5, 34.5, 36.5)],
        strokeColor: '#00aa00',
        label: 'Both'
    });

    const shape = map.sourceList[0].getShapes()[0];
    assert.equal(shape.getId(), 'merged');
    assert.equal(shape.properties.fillColor, 'rgba(0, 0, 0, 0)');
    assert.equal(shape.properties.strokeColor, '#00aa00');
    assert.equal(shape.properties.strokeWidth, 2);
    assert.equal(shape.properties.label, 'Both');
    assert.ok(['Polygon', 'MultiPolygon'].includes(shape.geometry.type));
    assert.equal(map.sourceList[0].getShapes().length, 1, 'one outline, not one per boundary');
});

test('two overlapping boundaries merge into one polygon whose outline spans both', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.addMergedRegion({ id: 'merged', polygons: [ring(0, 0, 2, 2), ring(1, 1, 3, 3)] });

    const geometry = map.sourceList[0].getShapes()[0].geometry;
    assert.equal(geometry.type, 'Polygon');
    const points = geometry.coordinates[0];
    assert.ok(points.length > 5, 'the union has more corners than one square');
    assert.equal(Math.min(...points.map(p => p[0])), 0);
    assert.equal(Math.max(...points.map(p => p[0])), 3);
});

test('a merged region with no boundaries draws nothing', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.addMergedRegion({ id: 'merged', polygons: [] });
    await controller.addMergedRegion(null);

    assert.equal(map.sourceList.length, 0);
});

// --- routes ---------------------------------------------------------------------------------------------------------------------------

const route = (id, points, color) => ({ id, points: points.map(([latitude, longitude]) => ({ latitude, longitude })), color });

test('a route is drawn as a casing line under a coloured line', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setRoutes([route('r1', [[33.8, 35.5], [33.9, 35.6]], '#ff0000')]);

    assert.deepEqual(map.layerList.map(layer => layer.kind), ['LineLayer', 'LineLayer']);
    const feature = map.sourceList[0].shapes[0];
    assert.deepEqual(feature.geometry.coordinates, [[35.5, 33.8], [35.6, 33.9]], 'longitude first');
    assert.deepEqual(feature.properties, { routeId: 'r1', color: '#ff0000' });
});

test('a route with no colour is blue', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setRoutes([route('r1', [[33.8, 35.5], [33.9, 35.6]])]);

    assert.equal(map.sourceList[0].shapes[0].properties.color, '#2563eb');
});

test('a route needs two points and valid coordinates or it is skipped', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setRoutes([
        route('one-point', [[33.8, 35.5]]),
        route('bad-latitude', [[91, 35.5], [33.9, 35.6]]),
        route('bad-longitude', [[33.8, 181], [33.9, 35.6]]),
        route('not-a-number', [[Number.NaN, 35.5], [33.9, 35.6]]),
        route('good', [[33.8, 35.5], [33.9, 35.6]]),
        { id: 'no-points' }
    ]);

    assert.deepEqual(map.sourceList[0].shapes.map(feature => feature.properties.routeId), ['good']);
});

test('setting routes again replaces the old ones without stacking layers', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setRoutes([route('a', [[33.8, 35.5], [33.9, 35.6]])]);
    controller.setRoutes([route('b', [[34.0, 35.5], [34.1, 35.6]])]);

    assert.equal(map.layerList.length, 2);
    assert.deepEqual(map.sourceList[0].shapes.map(feature => feature.properties.routeId), ['b']);
});

test('setting no usable routes clears what was drawn', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.setRoutes([route('a', [[33.8, 35.5], [33.9, 35.6]])]);

    controller.setRoutes([route('bad', [[33.8, 35.5]])]);

    assert.equal(map.layerList.length, 0);
    assert.equal(map.sourceList.length, 0);
});

test('clearing the routes removes the layers and the source', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.setRoutes([route('a', [[33.8, 35.5], [33.9, 35.6]])]);

    controller.clearRoutes();

    assert.equal(map.layerList.length, 0);
    assert.equal(map.sourceList.length, 0);
});

test('clearing routes that were never set is harmless', async () => {
    const { controller } = await world.createReadyController({});

    assert.doesNotThrow(() => controller.clearRoutes());
});

test('routes are put back when a style change wipes the map layers', async () => {
    const { map, controller } = await world.createReadyController({ style: 'road' });
    controller.setRoutes([route('a', [[33.8, 35.5], [33.9, 35.6]])]);
    map.layerList.length = 0;
    map.sourceList.length = 0;

    map.fire('styledata');

    assert.equal(map.layerList.length, 2);
    assert.equal(map.sourceList.length, 1);
});

// --- location lock --------------------------------------------------------------------------------------------------------------------

test('with no lock every point is allowed', async () => {
    const { controller } = await world.createReadyController({});

    assert.equal(controller.isPointAllowed(0, 0), true);
    assert.equal(controller.isPointAllowed(89, 179), true);
});

test('a lock allows points inside its polygon and refuses points outside it', async () => {
    const { controller } = await world.createReadyController({});
    controller.setLocationLock([ring(33, 35, 34, 36)], {});

    assert.equal(controller.isPointAllowed(33.5, 35.5), true);
    assert.equal(controller.isPointAllowed(32.9, 35.5), false);
    assert.equal(controller.isPointAllowed(33.5, 36.1), false);
    assert.equal(controller.isPointAllowed(34.1, 35.5), false);
    assert.equal(controller.isPointAllowed(33.5, 34.9), false);
});

test('the public check takes latitude then longitude, the private one longitude then latitude', async () => {
    const { controller } = await world.createReadyController({});
    controller.setLocationLock([ring(33, 35, 34, 36)], {});

    assert.equal(controller.isPointAllowed(33.5, 35.5), true);
    assert.equal(controller._isPointAllowed(35.5, 33.5), true);
    assert.equal(controller.isPointAllowed(35.5, 33.5), false, 'swapped arguments are outside');
});

test('a point may be allowed by any one of several areas', async () => {
    const { controller } = await world.createReadyController({});
    controller.setLocationLock([ring(33, 35, 34, 36), ring(10, 10, 11, 11)], {});

    assert.equal(controller.isPointAllowed(33.5, 35.5), true);
    assert.equal(controller.isPointAllowed(10.5, 10.5), true);
    assert.equal(controller.isPointAllowed(20, 20), false, 'inside the combined bounding box but in neither area');
});

test('a hole in an area is not allowed', async () => {
    const { controller } = await world.createReadyController({});
    const withHole = [square(0, 0, 10, 10), square(4, 4, 6, 6)];
    controller.setLocationLock([withHole], {});

    assert.equal(controller.isPointAllowed(2, 2), true);
    assert.equal(controller.isPointAllowed(5, 5), false, 'inside the hole');
});

test('a concave area refuses the notch', async () => {
    const { controller } = await world.createReadyController({});
    const lShape = [[[0, 0], [10, 0], [10, 4], [4, 4], [4, 10], [0, 10], [0, 0]]];
    controller.setLocationLock([lShape], {});

    assert.equal(controller.isPointAllowed(2, 2), true);
    assert.equal(controller.isPointAllowed(8, 2), true);
    assert.equal(controller.isPointAllowed(2, 8), true);
    assert.equal(controller.isPointAllowed(8, 8), false, 'the cut-out corner');
});

test('a lock with no polygons is no lock at all', async () => {
    const { controller } = await world.createReadyController({});

    controller.setLocationLock([], {});
    assert.equal(controller.isPointAllowed(50, 50), true);

    controller.setLocationLock(null, {});
    assert.equal(controller.isPointAllowed(50, 50), true);
});

test('an area with no outer ring allows nothing, so a lock built from a failed boundary lookup shuts everything out', async () => {
    const { controller } = await world.createReadyController({});

    controller.setLocationLock([[]], {});

    assert.equal(controller.isPointAllowed(50, 50), false);
});

test('clearing the lock allows everything again', async () => {
    const { controller } = await world.createReadyController({});
    controller.setLocationLock([ring(33, 35, 34, 36)], {});

    controller.clearLocationLock();

    assert.equal(controller.isPointAllowed(0, 0), true);
});

test('the boundary is drawn as a dashed outline and a fill only when asked', async () => {
    const hidden = await world.createReadyController({});
    const shown = await world.createReadyController({});

    hidden.controller.setLocationLock([ring(33, 35, 34, 36)], { showBoundary: false });
    shown.controller.setLocationLock([ring(33, 35, 34, 36)], { showBoundary: true, strokeColor: '#00ff00', fillColor: '#00ff0020' });

    assert.equal(hidden.map.sourceList.length, 0);
    assert.deepEqual(shown.map.layerList.map(layer => layer.kind), ['LineLayer', 'PolygonLayer']);
    assert.equal(shown.map.layerList[0].args[2].strokeColor, '#00ff00');
    assert.deepEqual(shown.map.layerList[0].args[2].strokeDashArray, [2, 2]);
    assert.equal(shown.map.layerList[1].args[2].fillColor, '#00ff0020');
    assert.equal(shown.map.sourceList[0].getShapes().length, 1);
});

test('replacing a lock redraws its boundary instead of piling shapes up', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.setLocationLock([ring(33, 35, 34, 36)], { showBoundary: true });
    controller.setLocationLock([ring(10, 10, 11, 11), ring(20, 20, 21, 21)], { showBoundary: true });

    assert.equal(map.sourceList.length, 1);
    assert.equal(map.sourceList[0].getShapes().length, 2);
});

test('zooming to the boundary fits the camera to the combined box with 40px padding', async () => {
    const { map, controller } = await world.createReadyController({});
    map.cameraCalls.length = 0;

    controller.setLocationLock([ring(33, 35, 34, 36), ring(30, 34, 31, 37)], { zoomToBoundary: true });

    assert.deepEqual(map.cameraCalls[0], { bounds: [34, 30, 37, 34], padding: 40 });
});

test('a lock that is not asked to zoom leaves the camera alone', async () => {
    const { map, controller } = await world.createReadyController({});
    map.cameraCalls.length = 0;

    controller.setLocationLock([ring(33, 35, 34, 36)], { zoomToBoundary: false });

    assert.equal(map.cameraCalls.length, 0);
});

test('a point outside the combined bounding box is refused without looking at a single vertex', async () => {
    const { controller } = await world.createReadyController({});
    // A ring that would throw if the ray cast ever reached it proves the bounding box answered first.
    const big = [[[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]]];
    controller.setLocationLock([big], {});
    const original = controller._pointInRings;
    let castCount = 0;
    controller._pointInRings = (...args) => { castCount++; return original.apply(controller, args); };

    assert.equal(controller.isPointAllowed(50, 50), false);
    assert.equal(castCount, 0);
    assert.equal(controller.isPointAllowed(5, 5), true);
    assert.equal(castCount, 1);
});
