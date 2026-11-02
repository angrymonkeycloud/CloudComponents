import test, { afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => world.disposeAll());

const info = (overrides = {}) => ({ id: 'm1', latitude: 33.8938, longitude: 35.5018, color: undefined, ...overrides });

// --- adding and removing -------------------------------------------------------------------------------------------------------------

test('a marker is placed at its longitude and latitude in the colour asked for', async () => {
    const { map, controller } = await world.createReadyController({});
    const before = map.markerList.length;

    controller.addMarker(info({ color: '#00ff00' }));

    const marker = map.markerList[before];
    assert.deepEqual(marker.options.position, [35.5018, 33.8938]);
    assert.equal(marker.options.color, '#00ff00');
});

test('a marker with no colour is red', async () => {
    const { map, controller } = await world.createReadyController({});

    controller.addMarker(info());

    assert.equal(map.markerList.at(-1).options.color, '#e81123');
});

test('a marker without an id, or with one already on the map, is ignored', async () => {
    const { map, controller } = await world.createReadyController({});
    const before = map.markerList.length;

    controller.addMarker(null);
    controller.addMarker(info({ id: '' }));
    controller.addMarker(info());
    controller.addMarker(info());

    assert.equal(map.markerList.length, before + 1);
});

test('removing a marker takes it off the map and frees its id for reuse', async () => {
    const { map, controller } = await world.createReadyController({});
    const before = map.markerList.length;
    controller.addMarker(info());

    controller.removeMarker('m1');
    assert.equal(map.markerList.length, before);

    controller.addMarker(info());
    assert.equal(map.markerList.length, before + 1);
});

test('removing a marker that is not there does nothing', async () => {
    const { controller } = await world.createReadyController({});

    assert.doesNotThrow(() => controller.removeMarker('nope'));
});

test('clearing the markers removes them all and closes their popups', async () => {
    const { map, controller } = await world.createReadyController({});
    const before = map.markerList.length;
    controller.addMarker(info({ id: 'a' }));
    controller.addMarker(info({ id: 'b' }));
    const [markerA] = map.markerList.slice(before);
    map.fire('click', {}, markerA);
    const popup = controller._markers.get('a').popup;
    assert.equal(popup.isOpen, true);

    controller.clearMarkers();

    assert.equal(map.markerList.length, before);
    assert.equal(popup.isOpen, false);
});

// --- popups ---------------------------------------------------------------------------------------------------------------------------

test('clicking a marker opens its popup and tells the page which marker it was', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({});
    controller.addMarker(info());
    const marker = map.markerList.at(-1);

    map.fire('click', {}, marker);

    assert.equal(controller._markers.get('m1').popup.isOpen, true);
    assert.deepEqual(dotNetRef.to('NotifyMarkerClickAsync'), [['m1']]);
});

test('opening one marker closes the one that was open', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addMarker(info({ id: 'a' }));
    const markerA = map.markerList.at(-1);
    controller.addMarker(info({ id: 'b', latitude: 34 }));
    const markerB = map.markerList.at(-1);

    map.fire('click', {}, markerA);
    map.fire('click', {}, markerB);

    assert.equal(controller._markers.get('a').popup.isOpen, false);
    assert.equal(controller._markers.get('b').popup.isOpen, true);
});

test('clicking the bare map closes an open popup', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addMarker(info());
    const marker = map.markerList.at(-1);
    map.fire('click', {}, marker);
    map.fire('click', { position: [35.5, 33.9], pixel: [1, 1] }); // the map-level click for the same gesture: leaves the popup

    assert.equal(controller._markers.get('m1').popup.isOpen, true, 'the click that opened it must not close it again');

    map.fire('click', { position: [35.6, 33.95], pixel: [200, 200] });

    assert.equal(controller._markers.get('m1').popup.isOpen, false);
});

test('a popup the person closed is forgotten, so the next click does not try to close it again', async () => {
    const { map, controller } = await world.createReadyController({});
    controller.addMarker(info());
    map.fire('click', {}, map.markerList.at(-1));
    const popup = controller._markers.get('m1').popup;

    popup.isOpen = false;
    map.fire('close', {}, popup);

    assert.equal(controller._activePopupId, null);
});

test('a host that shows its own details turns the built-in popup off and the click is still reported', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({ showMarkerPopups: false });
    controller.addMarker(info());

    map.fire('click', {}, map.markerList.at(-1));

    assert.equal(controller._markers.get('m1').popup, null);
    assert.deepEqual(dotNetRef.to('NotifyMarkerClickAsync'), [['m1']]);
});

test('double-clicking a marker removes it when removal is allowed', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({ allowMarkerRemoval: true });
    const before = map.markerList.length;
    controller.addMarker(info());
    const marker = map.markerList.at(-1);
    let stopped = false;

    marker.element.dispatch('dblclick', { stopPropagation: () => { stopped = true; } });

    assert.equal(map.markerList.length, before);
    assert.equal(stopped, true, 'the map must not also treat it as a double click');
    assert.deepEqual(dotNetRef.to('NotifyMarkerRemovedAsync'), [['m1']]);
});

test('double-clicking a marker does nothing when removal is not allowed', async () => {
    const { map, controller, dotNetRef } = await world.createReadyController({});
    controller.addMarker(info());
    const marker = map.markerList.at(-1);

    marker.element.dispatch('dblclick', { stopPropagation: () => { } });

    assert.equal(marker.element.listeners.dblclick, undefined);
    assert.equal(dotNetRef.to('NotifyMarkerRemovedAsync').length, 0);
});

// --- popup content --------------------------------------------------------------------------------------------------------------------

test('the popup shows the title, the details and the coordinates to five places', async () => {
    const { controller } = await world.createReadyController({});

    const html = controller._buildPopupContent(info({ title: 'Sea view', city: 'Jounieh', area: '120 sqm', price: '$150,000', imageUrl: 'https://img.test/a.jpg' }));

    assert.match(html, /Sea view/);
    assert.match(html, /Jounieh/);
    assert.match(html, /120 sqm/);
    assert.match(html, /\$150,000/);
    assert.match(html, /33\.89380, 35\.50180/);
    assert.match(html, /<img src="https:\/\/img\.test\/a\.jpg"/);
});

test('the popup falls back to the label for its title and leaves out what it was not given', async () => {
    const { controller } = await world.createReadyController({});

    const html = controller._buildPopupContent(info({ label: 'Pin A' }));

    assert.match(html, /Pin A/);
    assert.doesNotMatch(html, /<img/);
    assert.doesNotMatch(html, /<a href/);
});

test('the popup offers a link with the label it was given, or "Open"', async () => {
    const { controller } = await world.createReadyController({});

    const named = controller._buildPopupContent(info({ title: 'T', detailsUrl: '/properties/1', detailsLabel: 'View Property' }));
    const unnamed = controller._buildPopupContent(info({ title: 'T', detailsUrl: '/properties/1' }));

    assert.match(named, /<a href="\/properties\/1"/);
    assert.match(named, /View Property/);
    assert.match(unnamed, /Open/);
});

test('text from a listing cannot inject markup into the popup', async () => {
    const { controller } = await world.createReadyController({});
    const attack = '<img src=x onerror=alert(1)>"\'&';

    const html = controller._buildPopupContent(info({ title: attack, city: attack, area: attack, price: attack, imageUrl: `x" onerror="alert(1)`, detailsUrl: `javascript:alert(1)" onclick="x`, detailsLabel: attack }));

    assert.doesNotMatch(html, /<img src=x onerror/);
    assert.doesNotMatch(html, /onerror="alert\(1\)"/, 'a quote in a URL must not end the attribute');
    assert.doesNotMatch(html, /onclick="x/);
    assert.match(html, /&lt;img src=x onerror=alert\(1\)&gt;&quot;&#39;&amp;/);
});

test('escaping covers every character that can end text or an attribute', async () => {
    const { controller } = await world.createReadyController({});

    assert.equal(controller._escapeHtml(`&<>"'`), '&amp;&lt;&gt;&quot;&#39;');
    assert.equal(controller._escapeHtml(5), '5');
    assert.equal(controller._escapeHtml('plain'), 'plain');
});

// --- titled markers -------------------------------------------------------------------------------------------------------------------

test('markers are plain pins unless titles are switched on and the marker has one', async () => {
    const { map, controller } = await world.createReadyController({});
    const { map: titledMap, controller: titled } = await world.createReadyController({ showMarkerTitlesOnZoom: true });

    controller.addMarker(info({ title: 'Sea view' }));
    titled.addMarker(info({ title: 'Sea view' }));
    titled.addMarker(info({ id: 'untitled' }));

    assert.equal(map.markerList.at(-1).options.htmlContent, undefined);
    const withTitle = titledMap.markerList.at(-2).options;
    assert.equal(withTitle.anchor, 'bottom');
    assert.match(withTitle.htmlContent, /cloud-map-marker-title/);
    assert.equal(titledMap.markerList.at(-1).options.htmlContent, undefined, 'no title, no label');
});

test('a titled marker shows the price and size on their own line, or the city when it has neither', async () => {
    const { controller } = await world.createReadyController({ showMarkerTitlesOnZoom: true });

    const priced = controller._buildTitledMarkerContent(info({ title: 'Sea view', price: '$150,000', area: '120 sqm', city: 'Jounieh' }));
    const placed = controller._buildTitledMarkerContent(info({ title: 'Sea view', city: 'Jounieh' }));
    const bare = controller._buildTitledMarkerContent(info({ title: 'Sea view' }));

    assert.match(priced, /<span class="cloud-map-marker-details">\$150,000 · 120 sqm<\/span>/);
    assert.doesNotMatch(priced, /Jounieh/);
    assert.match(placed, /<span class="cloud-map-marker-details">Jounieh<\/span>/);
    assert.doesNotMatch(bare, /cloud-map-marker-details/);
});

test('a titled marker only accepts a hex colour, so a colour cannot carry CSS', async () => {
    const { controller } = await world.createReadyController({ showMarkerTitlesOnZoom: true });

    const good = controller._buildTitledMarkerContent(info({ title: 'A', color: '#12ab34' }));
    const bad = controller._buildTitledMarkerContent(info({ title: 'A', color: 'red;background:url(x)' }));

    assert.match(good, /--cloud-map-marker-color:#12ab34/);
    assert.match(bad, /--cloud-map-marker-color:#e81123/);
    assert.doesNotMatch(bad, /url\(x\)/);
});

test('a titled marker escapes its title', async () => {
    const { controller } = await world.createReadyController({ showMarkerTitlesOnZoom: true });

    const html = controller._buildTitledMarkerContent(info({ title: '<script>x</script>' }));

    assert.doesNotMatch(html, /<script>/);
    assert.match(html, /&lt;script&gt;/);
});

test('titles appear once the map is zoomed in far enough and disappear when it is zoomed out', async () => {
    const { map, controller, options } = await world.createReadyController({ showMarkerTitlesOnZoom: true, markerTitleMinZoom: 14, zoom: 10 });
    const container = world.elementsById.get(options.elementId);
    assert.equal(container.classList.contains('_marker-titles-visible'), false);

    map.camera.zoom = 14;
    map.fire('zoomend');
    assert.equal(container.classList.contains('_marker-titles-visible'), true);

    map.camera.zoom = 13.9;
    map.fire('zoomend');
    assert.equal(container.classList.contains('_marker-titles-visible'), false);
    assert.ok(controller);
});

test('the title zoom threshold is 15 when none is given', async () => {
    const { map, options } = await world.createReadyController({ showMarkerTitlesOnZoom: true, zoom: 10 });
    const container = world.elementsById.get(options.elementId);

    map.camera.zoom = 14.9;
    map.fire('zoomend');
    assert.equal(container.classList.contains('_marker-titles-visible'), false);
    map.camera.zoom = 15;
    map.fire('zoomend');
    assert.equal(container.classList.contains('_marker-titles-visible'), true);
});

test('titles are never managed when the feature is off', async () => {
    const { map, options } = await world.createReadyController({ zoom: 10 });
    const container = world.elementsById.get(options.elementId);

    map.camera.zoom = 20;
    map.fire('zoomend');

    assert.equal(container.classList.contains('_marker-titles-visible'), false);
});

// --- the current-location dot -----------------------------------------------------------------------------------------------------

test('showing the current location replaces the dot, centres on it at street zoom and sets the intentional-move flag', async () => {
    const { map, controller } = await world.createReadyController({ latitude: 33.89, longitude: 35.5 });
    const dots = () => map.markerList.filter(marker => marker.options.anchor === 'center');
    assert.equal(dots().length, 1);

    controller.showCurrentLocation(34, 36);

    assert.equal(dots().length, 1, 'the old dot is replaced, not added to');
    assert.deepEqual(dots()[0].options.position, [36, 34]);
    assert.deepEqual(map.cameraCalls.at(-1), { center: [36, 34], zoom: 15 });
    assert.equal(controller._allowZoomCenterChange, true);
});

// A link comes from the host app, not from a visitor, but a library that renders it should not turn a script URL into a clickable link.
test('a javascript: details link is not rendered as a link', { todo: 'HARDENING: _buildPopupContent only HTML-escapes detailsUrl; a javascript: scheme still becomes a live href' }, async () => {
    const { controller } = await world.createReadyController({});

    const html = controller._buildPopupContent(info({ title: 'T', detailsUrl: 'javascript:alert(1)' }));

    assert.doesNotMatch(html, /href="javascript:/i);
});
