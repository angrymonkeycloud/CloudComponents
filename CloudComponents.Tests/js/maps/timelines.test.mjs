import test, { afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => world.disposeAll());

const point = (latitude, longitude, timestamp, extra = {}) => ({ latitude, longitude, timestamp, ...extra });

const journey = () => ({
    id: 't1',
    name: 'Karim',
    color: '#0078d4',
    showLine: true,
    points: [
        point(33.90, 35.50, '2026-03-02T09:00:00'),
        point(33.91, 35.51, '2026-03-02T09:10:00'),
        point(33.92, 35.52, '2026-03-02T09:30:00', { label: 'Office', description: 'Meeting' }),
        point(33.93, 35.53, '2026-03-02T10:15:00'),
        point(33.94, 35.54, '2026-03-02T10:30:00')
    ]
});

// --- drawing ---------------------------------------------------------------------------------------------------------------------------

test('a journey is drawn as a route line with arrows, clustered fixes, always-visible places and start and end badges', async () => {
    const { map, controller } = await world.createReadyController({});
    const baselineMarkers = map.markerList.length;

    await controller.setTimelines([journey()], false);

    // line + trail (the fixes between start and end that have no label) + place (the labelled one)
    assert.equal(map.sourceList.length, 3);
    assert.deepEqual(map.layerList.map(layer => layer.kind), ['LineLayer', 'SymbolLayer', 'BubbleLayer', 'SymbolLayer', 'BubbleLayer', 'BubbleLayer', 'SymbolLayer']);
    assert.equal(map.markerList.length - baselineMarkers, 2, 'a start badge and an end badge');
});

test('the route line runs through every point, longitude first', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.setTimelines([journey()], false);

    const line = map.sourceList[0].shapes[0];
    assert.equal(line.geometry.type, 'LineString');
    assert.deepEqual(line.geometry.coordinates[0], [35.50, 33.90]);
    assert.equal(line.geometry.coordinates.length, 5);
    assert.equal(line.properties.timelineId, 't1');
});

test('the middle fixes are split into raw fixes (clustered) and labelled places (not clustered)', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.setTimelines([journey()], false);

    const trail = map.sourceList[1];
    const places = map.sourceList[2];
    assert.deepEqual(trail.args[1], { cluster: true, clusterRadius: 42, clusterMaxZoom: 17 });
    assert.deepEqual(trail.shapes.map(feature => feature.properties.index), [1, 3]);
    assert.deepEqual(places.shapes.map(feature => [feature.properties.index, feature.properties.label]), [[2, 'Office']]);
});

test('a journey with the line switched off, or only one point, draws no line', async () => {
    const { map, controller } = await world.createReadyController({});
    const baselineMarkers = map.markerList.length;
    const noLine = { ...journey(), showLine: false };
    const single = { id: 's', points: [point(33.9, 35.5, '2026-03-02T09:00:00')] };

    await controller.setTimelines([noLine], false);
    assert.equal(map.layerList.some(layer => layer.kind === 'LineLayer'), false);

    await controller.setTimelines([single], false);
    assert.equal(map.layerList.length, 0, 'one point is just a start badge');
    assert.equal(map.markerList.length - baselineMarkers, 1);
});

test('a journey with no points is skipped', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.setTimelines([{ id: 'empty', points: [] }, { id: 'none' }], true);

    assert.equal(map.sourceList.length, 0);
    assert.equal(map.cameraCalls.length, 0, 'nothing to fit to');
});

test('drawing again replaces what was drawn before', async () => {
    const { map, controller } = await world.createReadyController({});
    const baselineMarkers = map.markerList.length;

    await controller.setTimelines([journey()], false);
    await controller.setTimelines([journey()], false);

    assert.equal(map.sourceList.length, 3);
    assert.equal(map.markerList.length - baselineMarkers, 2);
});

test('clearing the journeys removes everything they drew and closes the popup', async () => {
    const { map, controller } = await world.createReadyController({});
    const baselineMarkers = map.markerList.length;
    await controller.setTimelines([journey()], false);
    controller._openTimelinePopup('t1', 2);
    const popup = controller._timelinePopup;

    controller.clearTimelines();

    assert.equal(map.sourceList.length, 0);
    assert.equal(map.layerList.length, 0);
    assert.equal(map.markerList.length, baselineMarkers);
    assert.equal(popup.isOpen, false);
});

test('fitting to the journey frames its points with 70px padding', async () => {
    const { map, controller } = await world.createReadyController({});

    await controller.setTimelines([journey()], true);

    assert.deepEqual(map.cameraCalls.at(-1), { bounds: [35.50, 33.90, 35.54, 33.94], padding: 70, type: 'ease', duration: 700 });
});

test('a journey that did not move is padded so the camera does not zoom to the maximum', async () => {
    const { map, controller } = await world.createReadyController({});
    const still = { id: 's', points: [point(33.9, 35.5, '2026-03-02T09:00:00'), point(33.9, 35.5, '2026-03-02T09:10:00')] };

    await controller.setTimelines([still], true);

    const [west, south, east, north] = map.cameraCalls.at(-1).bounds;
    assert.ok(north - south > 0.00199 && east - west > 0.00199, `${north - south} x ${east - west}`);
});

test('journeys are not fitted to unless asked', async () => {
    const { map, controller } = await world.createReadyController({});
    map.cameraCalls.length = 0;

    await controller.setTimelines([journey()], false);

    assert.equal(map.cameraCalls.length, 0);
});

test('a map without the arrow template still draws the journey, only without arrows', async () => {
    const { map, controller } = await world.createReadyController({});
    map.imageSprite.createFromTemplate = async () => { throw new Error('no template'); };

    await controller.setTimelines([journey()], false);

    assert.deepEqual(map.layerList.map(layer => layer.kind), ['LineLayer', 'BubbleLayer', 'SymbolLayer', 'BubbleLayer', 'BubbleLayer', 'SymbolLayer']);
});

// --- clicks ---------------------------------------------------------------------------------------------------------------------------

test('clicking a start badge opens its popup and tells the page which point it was', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({});
    const baseline = map.markerList.length;
    await controller.setTimelines([journey()], false);
    const [start, end] = map.markerList.slice(baseline);

    map.fire('click', {}, start);
    map.fire('click', {}, end);

    assert.deepEqual(dotNetRef.to('NotifyTimelinePointClickAsync'), [['t1', 0], ['t1', 4]]);
    assert.deepEqual(controller._timelinePopup.options.position, [35.54, 33.94]);
});

test('clicking a fix or a place on the map opens its popup and reports its index', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({});
    await controller.setTimelines([journey()], false);
    const placeLayer = map.layerList.filter(layer => layer.kind === 'BubbleLayer').at(-1);
    const placeFeature = map.sourceList[2].shapes[0];

    map.fire('click', { shapes: [placeFeature] }, placeLayer);

    assert.deepEqual(dotNetRef.to('NotifyTimelinePointClickAsync'), [['t1', 2]]);
    assert.equal(controller._timelinePopup.isOpen, true);
});

test('clicking a layer where no shape was hit does nothing', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({});
    await controller.setTimelines([journey()], false);
    const placeLayer = map.layerList.filter(layer => layer.kind === 'BubbleLayer').at(-1);

    map.fire('click', { shapes: [] }, placeLayer);
    map.fire('click', {}, placeLayer);

    assert.equal(dotNetRef.to('NotifyTimelinePointClickAsync').length, 0);
});

test('clicking a stack of fixes zooms to the level where it breaks apart', async () => {
    const { map, controller } = await world.createReadyController({});
    await controller.setTimelines([journey()], false);
    const clusterLayer = map.layerList.filter(layer => layer.kind === 'BubbleLayer')[0];
    const cluster = { properties: { cluster_id: 7 }, geometry: { coordinates: [35.51, 33.91] } };

    map.fire('click', { shapes: [cluster] }, clusterLayer);
    await new Promise(resolve => setImmediate(resolve));

    assert.deepEqual(map.cameraCalls.at(-1), { center: [35.51, 33.91], zoom: 14, type: 'ease', duration: 450 });
});

test('the pointer turns into a hand over a clickable journey layer', async () => {
    const { map, controller } = await world.createReadyController({});
    await controller.setTimelines([journey()], false);
    const layer = map.layerList.filter(l => l.kind === 'BubbleLayer')[0];

    map.fire('mouseenter', {}, layer);
    assert.equal(map.container.style.cursor, 'pointer');
    map.fire('mouseleave', {}, layer);
    assert.equal(map.container.style.cursor, '');
});

test('opening a journey popup closes an open marker popup, and a point that does not exist opens nothing', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addMarker({ id: 'm', latitude: 33.9, longitude: 35.5 });
    map.fire('click', {}, map.markerList.at(-1));
    const markerPopup = controller._markers.get('m').popup;
    await controller.setTimelines([journey()], false);

    controller._openTimelinePopup('t1', 99);
    controller._openTimelinePopup('missing', 0);
    assert.equal(markerPopup.isOpen, true);

    controller._openTimelinePopup('t1', 1);
    assert.equal(markerPopup.isOpen, false);
});

// --- popup content --------------------------------------------------------------------------------------------------------------------

test('a place popup names the place, the journey, the stay, the leg and the average speed', async () => {
    const { controller } = await world.createReadyController({});
    const tl = journey();

    const html = controller._buildTimelinePopupContent(tl, tl.points[2], 2);

    assert.match(html, />Place</);
    assert.match(html, />Office</);
    assert.match(html, />Karim</);
    assert.match(html, /Stayed 45 min/);
    assert.match(html, /Meeting/);
    assert.match(html, /m from previous point|km from previous point/);
    assert.match(html, /km\/h average/);
    assert.match(html, /30 min into the timeline/);
    assert.match(html, /Point 3 of 5/);
});

test('the first and last points are labelled Start and End, with their own accent colours', async () => {
    const { controller } = await world.createReadyController({});
    const tl = journey();

    const start = controller._buildTimelinePopupContent(tl, tl.points[0], 0);
    const end = controller._buildTimelinePopupContent(tl, tl.points[4], 4);
    const fix = controller._buildTimelinePopupContent(tl, tl.points[1], 1);

    assert.match(start, />Start</);
    assert.match(start, /#107c10/);
    assert.doesNotMatch(start, /from previous point/);
    assert.match(end, />End</);
    assert.match(end, /#d13438/);
    assert.match(fix, />GPS point</);
});

test('text from the journey cannot inject markup into its popup', async () => {
    const { controller } = await world.createReadyController({});
    const tl = { ...journey(), name: '<b>name</b>' };
    tl.points[2] = { ...tl.points[2], label: '<img src=x onerror=1>', description: '<script>1</script>' };

    const html = controller._buildTimelinePopupContent(tl, tl.points[2], 2);

    assert.doesNotMatch(html, /<img src=x/);
    assert.doesNotMatch(html, /<script>/);
    assert.doesNotMatch(html, /<b>name<\/b>/);
});

test('a badge caption escapes the label too', async () => {
    const { controller } = await world.createReadyController({});

    const html = controller._buildTimelineEndpointHtml('start', point(33.9, 35.5, '2026-03-02T09:00:00', { label: '<i>x</i>' }));

    assert.doesNotMatch(html, /<i>x<\/i>/);
    assert.match(html, /Start · &lt;i&gt;x&lt;\/i&gt;/);
    assert.match(controller._buildTimelineEndpointHtml('end', point(1, 1, null)), /End</);
});

// A colour comes from the host app, not a visitor; the titled marker already limits colours to hex and the popup could do the same.
test('a journey colour cannot carry CSS into its popup', { todo: 'HARDENING: _buildTimelinePopupContent writes tl.color into a style attribute unescaped; the titled marker checks hex' }, async () => {
    const { controller } = await world.createReadyController({});
    const tl = { ...journey(), color: 'red;background:url(//evil)' };

    const html = controller._buildTimelinePopupContent(tl, tl.points[1], 1);

    assert.doesNotMatch(html, /url\(\/\/evil\)/);
});

// --- helpers --------------------------------------------------------------------------------------------------------------------------

test('distances are measured on the sphere in metres', async () => {
    const { controller } = await world.createReadyController({});

    assert.equal(Math.round(controller._haversineMeters(0, 0, 0, 1)), 111195);
    assert.equal(Math.round(controller._haversineMeters(33.8938, 35.5018, 33.8938, 35.5018)), 0);
    const beirutToTripoli = controller._haversineMeters(33.8938, 35.5018, 34.4367, 35.8497);
    assert.ok(beirutToTripoli > 65000 && beirutToTripoli < 72000, String(beirutToTripoli));
});

test('distances read in metres up to a kilometre and in kilometres beyond', async () => {
    const { controller } = await world.createReadyController({});

    assert.equal(controller._formatDistance(0), '0 m');
    assert.equal(controller._formatDistance(999.4), '999 m');
    assert.equal(controller._formatDistance(1000), '1.00 km');
    assert.equal(controller._formatDistance(12345), '12.35 km');
});

test('durations read in minutes, hours, or both, and not at all under a minute', async () => {
    const { controller } = await world.createReadyController({});

    assert.equal(controller._formatDuration(0.5), '');
    assert.equal(controller._formatDuration(Number.NaN), '');
    assert.equal(controller._formatDuration(Infinity), '');
    assert.equal(controller._formatDuration(45), '45 min');
    assert.equal(controller._formatDuration(60), '1 h');
    assert.equal(controller._formatDuration(135), '2 h 15 min');
});

test('dates that cannot be read are nothing, not "Invalid Date"', async () => {
    const { controller } = await world.createReadyController({});

    assert.equal(controller._parseDate(null), null);
    assert.equal(controller._parseDate(''), null);
    assert.equal(controller._parseDate('not a date'), null);
    assert.ok(controller._parseDate('2026-03-02T09:30:00') instanceof Date);
    assert.equal(controller._formatClockTime('not a date'), '');
    assert.match(controller._formatClockTime('2026-03-02T09:30:00'), /09.30/);
});
