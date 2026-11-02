import test from 'node:test';
import assert from 'node:assert/strict';
import { installWorld, createDotNetRef } from '../support/harness.mjs';

const world = await installWorld();
const options = { elementId: 'sdk-map', latitude: 0, longitude: 0, zoom: 2, subscriptionKey: 'k', controls: {} };

test('a script that cannot be fetched fails the map with a message that names the SDK', async () => {
    const pending = world.mapInterop.createMap(createDotNetRef(), options);
    await new Promise(resolve => setImmediate(resolve));

    world.documentHead.children.find(child => child.tagName === 'SCRIPT').onerror();

    await assert.rejects(pending, /Failed to load Azure Maps SDK/);
});

// One bad load (a tunnel, a blocked CDN) is remembered for the life of the page, so every later attempt fails at once even after
// the network is back, until the page is reloaded.
test('after a failed load a later attempt tries again', { todo: 'BUG: loadAzureMapsSdk caches its rejected promise in _atlasLoader and never resets it' }, async () => {
    const retry = world.mapInterop.createMap(createDotNetRef(), options);
    await new Promise(resolve => setImmediate(resolve));
    const scripts = world.documentHead.children.filter(child => child.tagName === 'SCRIPT');
    assert.ok(scripts.length >= 2, 'a second attempt should add a second script');

    globalThis.atlas = world.atlas;
    scripts.at(-1).onload();
    await retry;
});
