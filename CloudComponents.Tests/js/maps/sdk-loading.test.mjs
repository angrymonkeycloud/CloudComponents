// The Azure Maps SDK is not bundled: the first map injects its stylesheet and script and waits for them.
// This file never sets the global `atlas` up front, which is what makes the loader do its work.
import test from 'node:test';
import assert from 'node:assert/strict';
import { installWorld, createDotNetRef } from '../support/harness.mjs';

const world = await installWorld();
const options = { elementId: 'sdk-map', latitude: 0, longitude: 0, zoom: 2, subscriptionKey: 'k', controls: {} };
const find = (tag) => world.documentHead.children.find(child => child.tagName === tag);

test('the first map adds the versioned stylesheet and script and waits for the script to load', async () => {
    let settled = false;
    const pending = world.mapInterop.createMap(createDotNetRef(), options).then(controller => { settled = true; return controller; });
    await new Promise(resolve => setImmediate(resolve));

    const link = find('LINK');
    const script = find('SCRIPT');
    assert.equal(link.href, 'https://atlas.microsoft.com/sdk/javascript/mapcontrol/3/atlas.min.css');
    assert.equal(link.rel, 'stylesheet');
    assert.equal(script.src, 'https://atlas.microsoft.com/sdk/javascript/mapcontrol/3/atlas.min.js');
    assert.equal(script.async, true);
    assert.equal(script.getAttribute('data-cc-atlas'), '');
    assert.equal(link.getAttribute('data-cc-atlas'), '');
    assert.equal(settled, false, 'no map until the SDK is there');

    globalThis.atlas = world.atlas;
    script.onload();
    const controller = await pending;

    assert.equal(typeof controller.addMarker, 'function');
    assert.equal(world.mapsCreated.length, 1);
});

test('later maps use the SDK that is already there and add nothing to the page', async () => {
    const childrenBefore = world.documentHead.children.length;

    await world.mapInterop.createMap(createDotNetRef(), { ...options, elementId: 'sdk-map-2' });

    assert.equal(world.documentHead.children.length, childrenBefore);
    assert.equal(world.mapsCreated.length, 2);
});
