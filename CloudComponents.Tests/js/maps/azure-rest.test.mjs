// What the controller asks of the Azure Maps web services, and what it makes of the answers.
//
// These are also the contract a Google implementation has to keep: the SHAPES asserted on the results (latitude/longitude,
// north/south/east/west, geometryId, names, ...) are what the Blazor component maps into GeocodeResult, MapSearchResult,
// ReverseGeocodeResult and MunicipalitySearchPage. The URLs asserted here are Azure's and are expected to change with the provider.

import test, { afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { installWorld, mockFetch } from '../support/harness.mjs';

const world = await installWorld();
afterEach(() => world.disposeAll());

const KEY = 'k e/y&1';

const town = (overrides = {}) => ({
    position: { lat: 33.98, lon: 35.62 },
    viewport: { topLeftPoint: { lat: 34.0, lon: 35.6 }, btmRightPoint: { lat: 33.96, lon: 35.64 } },
    entityType: 'Municipality',
    dataSources: { geometry: { id: 'geo-1' } },
    address: { municipality: 'Jounieh', countrySubdivision: 'Mount Lebanon', country: 'Lebanon', freeformAddress: 'Jounieh, Mount Lebanon', countryCode: 'LB' },
    ...overrides
});

async function controllerWith(routes) {
    const fetch = mockFetch(routes);
    const { controller } = await world.createController({ subscriptionKey: KEY });
    return { controller, fetch };
}

// --- geocode --------------------------------------------------------------------------------------------------------------------------

test('geocoding asks the fuzzy search for one result with the key and query encoded', async () => {
    const { controller, fetch } = await controllerWith([['search/fuzzy', { results: [town()] }]]);

    await controller.geocode('Jounieh, Lebanon');

    const url = new URL(fetch.requests[0]);
    assert.equal(url.origin + url.pathname, 'https://atlas.microsoft.com/search/fuzzy/json');
    assert.deepEqual(Object.fromEntries(url.searchParams), { 'api-version': '1.0', 'subscription-key': KEY, query: 'Jounieh, Lebanon', limit: '1' });
    assert.ok(!fetch.requests[0].includes(KEY), 'the key must be percent-encoded in the URL');
});

test('geocoding returns the point, the viewport, the geometry id and the names of the first result', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [town()] }]]);

    const place = await controller.geocode('Jounieh');

    assert.deepEqual(place, {
        latitude: 33.98,
        longitude: 35.62,
        north: 34.0,
        south: 33.96,
        east: 35.64,
        west: 35.6,
        geometryId: 'geo-1',
        entityType: 'Municipality',
        names: ['Jounieh', 'Mount Lebanon', 'Lebanon']
    });
});

test('a result with no viewport is a box of zero size around its point', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [town({ viewport: undefined })] }]]);

    const place = await controller.geocode('Jounieh');

    assert.deepEqual([place.north, place.south, place.east, place.west], [33.98, 33.98, 35.62, 35.62]);
});

test('the bounding box is used when there is no viewport', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [town({ viewport: undefined, boundingBox: { topLeftPoint: { lat: 40, lon: 1 }, btmRightPoint: { lat: 39, lon: 2 } } })] }]]);

    const place = await controller.geocode('x');

    assert.deepEqual([place.north, place.south, place.east, place.west], [40, 39, 2, 1]);
});

test('a place with no geometry has a null geometry id', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [town({ dataSources: undefined })] }]]);

    assert.equal((await controller.geocode('x')).geometryId, null);
});

test('a point of interest contributes its name', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [town({ address: {}, poi: { name: 'Casino du Liban' } })] }]]);

    assert.deepEqual((await controller.geocode('x')).names, ['Casino du Liban']);
});

test('asking for one kind of place restricts the search to the geography index and that entity type', async () => {
    const { controller, fetch } = await controllerWith([['search/fuzzy', { results: [town()] }]]);

    await controller.geocode('Lebanon', 'Country', 'LB');

    const params = fetch.params('search/fuzzy');
    assert.equal(params.idxSet, 'Geo');
    assert.equal(params.entityType, 'Country');
    assert.equal(params.countrySet, 'LB');
});

test('a country filter alone does not switch to the geography index', async () => {
    const { controller, fetch } = await controllerWith([['search/fuzzy', { results: [town()] }]]);

    await controller.geocode('Hamra', null, 'LB,SY');

    const params = fetch.params('search/fuzzy');
    assert.equal('idxSet' in params, false);
    assert.equal(params.countrySet, 'LB,SY');
});

test('geocoding nothing asks nothing', async () => {
    const { controller, fetch } = await controllerWith([]);

    assert.equal(await controller.geocode(''), null);
    assert.equal(await controller.geocode(undefined), null);
    assert.equal(fetch.requests.length, 0);
});

test('geocoding gives null for no results, a result with no position, a refusal or a network failure', async () => {
    for (const answer of [{ results: [] }, { results: [{ address: {} }] }, {}, 401, 500, new Error('offline')]) {
        const { controller } = await controllerWith([['search/fuzzy', answer]]);

        assert.equal(await controller.geocode('x'), null, JSON.stringify(answer));
    }
});

// --- search suggestions --------------------------------------------------------------------------------------------------------------

test('a search asks for as many suggestions as wanted, between one and twenty, five by default', async () => {
    const { controller, fetch } = await controllerWith([['search/fuzzy', { results: [] }]]);

    await controller.search('a', 8);
    await controller.search('a', 0);
    await controller.search('a', 99);
    await controller.search('a');
    await controller.search('a', -3);

    assert.deepEqual(fetch.requests.map(url => new URL(url).searchParams.get('limit')), ['8', '5', '20', '5', '1']);
});

test('suggestions carry an address line, a description, the point, the viewport and the geometry id', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [town()] }]]);

    const [suggestion] = await controller.search('Jou');

    assert.deepEqual(suggestion, {
        address: 'Jounieh, Mount Lebanon',
        description: 'Jounieh, Mount Lebanon, Lebanon',
        latitude: 33.98,
        longitude: 35.62,
        north: 34.0,
        south: 33.96,
        east: 35.64,
        west: 35.6,
        geometryId: 'geo-1'
    });
});

test('a suggestion with no address text falls back to the place name and then to its coordinates', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [
        town({ address: { country: 'Lebanon' }, poi: { name: 'Harbour' } }),
        town({ address: {}, position: { lat: 33.123456, lon: 35.654321 } })
    ] }]]);

    const suggestions = await controller.search('x');

    assert.equal(suggestions[0].address, 'Harbour');
    assert.equal(suggestions[1].address, '33.12346, 35.65432');
    assert.equal(suggestions[1].description, null);
});

test('results without a position are left out of the suggestions', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [{ address: {} }, town()] }]]);

    assert.equal((await controller.search('x')).length, 1);
});

test('searching for nothing, or failing, gives no suggestions', async () => {
    for (const answer of [{}, 500, new Error('offline')]) {
        const { controller } = await controllerWith([['search/fuzzy', answer]]);

        assert.deepEqual(await controller.search('x'), [], JSON.stringify(answer));
    }

    const { controller, fetch } = await controllerWith([]);
    assert.deepEqual(await controller.search(''), []);
    assert.equal(fetch.requests.length, 0);
});

// --- reverse geocoding ---------------------------------------------------------------------------------------------------------------

test('reverse geocoding sends the point as latitude,longitude and asks for English', async () => {
    const { controller, fetch } = await controllerWith([['reverse', { addresses: [{ address: {} }] }]]);

    await controller.reverseGeocode(33.8938, 35.5018);

    const url = new URL(fetch.requests[0]);
    assert.equal(url.pathname, '/search/address/reverse/json');
    assert.equal(url.searchParams.get('query'), '33.8938,35.5018');
    assert.equal(url.searchParams.get('language'), 'en-US');
});

test('reverse geocoding returns the address parts, with null for what Azure did not say', async () => {
    const full = await controllerWith([['reverse', { addresses: [{ address: {
        countryCode: 'LB', countrySubdivisionCode: 'JL', countrySecondarySubdivision: 'Baabda', municipality: 'Beirut',
        municipalitySubdivision: 'Hamra', postalCode: '1103', localName: 'Beirut', freeformAddress: 'Hamra, Beirut'
    } }] }]]);
    assert.deepEqual(await full.controller.reverseGeocode(33.89, 35.5), {
        countryCode: 'LB', countrySubdivisionCode: 'JL', countrySecondarySubdivision: 'Baabda', municipality: 'Beirut',
        municipalitySubdivision: 'Hamra', postalCode: '1103', localName: 'Beirut', freeformAddress: 'Hamra, Beirut'
    });

    // fetch is global: the second script replaces the first, so the first result is read before it is made.
    const sparse = await controllerWith([['reverse', { addresses: [{ address: { countryCode: 'LB' } }] }]]);
    assert.deepEqual(await sparse.controller.reverseGeocode(33.89, 35.5), {
        countryCode: 'LB', countrySubdivisionCode: null, countrySecondarySubdivision: null, municipality: null,
        municipalitySubdivision: null, postalCode: null, localName: null, freeformAddress: null
    });
});

test('reverse geocoding a point on the equator or the prime meridian is still a point', async () => {
    const { controller, fetch } = await controllerWith([['reverse', { addresses: [{ address: { countryCode: 'GH' } }] }]]);

    const result = await controller.reverseGeocode(0, 0);

    assert.equal(result.countryCode, 'GH');
    assert.equal(fetch.requests.length, 1);
});

test('reverse geocoding with no point, no address or a failure gives null', async () => {
    const empty = await controllerWith([]);
    assert.equal(await empty.controller.reverseGeocode(null, 35), null);
    assert.equal(await empty.controller.reverseGeocode(33, undefined), null);
    assert.equal(empty.fetch.requests.length, 0);

    for (const answer of [{ addresses: [] }, {}, 403, new Error('offline')]) {
        const { controller } = await controllerWith([['reverse', answer]]);

        assert.equal(await controller.reverseGeocode(33, 35), null, JSON.stringify(answer));
    }
});

// --- municipalities -----------------------------------------------------------------------------------------------------------------

const municipality = (name, extra = {}) => ({ address: { countryCode: 'LB', municipality: name, localName: name, freeformAddress: `${name}, Lebanon`, ...extra } });

test('a municipality search is limited to municipalities of one country in the geography index, paged', async () => {
    const { controller, fetch } = await controllerWith([['search/fuzzy', { summary: { totalResults: 0 }, results: [] }]]);

    await controller.searchMunicipalities('Jou', 'LB', 25, 50, true);

    const params = fetch.params('search/fuzzy');
    assert.equal(params.idxSet, 'Geo');
    assert.equal(params.entityType, 'Municipality');
    assert.equal(params.countrySet, 'LB');
    assert.equal(params.limit, '25');
    assert.equal(params.ofs, '50');
    assert.equal(params.language, 'en-US');
    assert.equal(params.typeahead, 'true');
});

test('typeahead is only asked for when wanted, and the page size is held between 1 and 100, defaulting to 100', async () => {
    const { controller, fetch } = await controllerWith([['search/fuzzy', { results: [] }]]);

    await controller.searchMunicipalities('Jou', 'LB');
    await controller.searchMunicipalities('Jou', 'LB', 500, -4);
    await controller.searchMunicipalities('Jou', 'LB', 0);

    const urls = fetch.requests.map(url => new URL(url).searchParams);
    assert.equal(urls[0].has('typeahead'), false);
    assert.deepEqual(urls.map(p => p.get('limit')), ['100', '100', '100']);
    assert.deepEqual(urls.map(p => p.get('ofs')), ['0', '0', '0']);
});

test('municipalities come back with the fields the import reads, and the total Azure reported', async () => {
    const { controller } = await controllerWith([['search/fuzzy', {
        summary: { totalResults: 312 },
        results: [municipality('Jounieh', { countrySubdivisionCode: 'JL', countrySecondarySubdivision: 'Keserwan', municipalitySubdivision: 'Ghazir' })]
    }]]);

    const page = await controller.searchMunicipalities('Jou', 'LB', 10, 0);

    assert.equal(page.totalResults, 312);
    assert.deepEqual(page.results, [{
        countryCode: 'LB', countrySubdivisionCode: 'JL', countrySecondarySubdivision: 'Keserwan', localName: 'Jounieh',
        municipality: 'Jounieh', municipalitySubdivision: 'Ghazir', freeformAddress: 'Jounieh, Lebanon'
    }]);
});

test('a result with no name of any kind is dropped, and without a reported total the count is what was returned', async () => {
    const { controller } = await controllerWith([['search/fuzzy', { results: [municipality('Jounieh'), { address: { countryCode: 'LB' } }] }]]);

    const page = await controller.searchMunicipalities('Jou', 'LB');

    assert.equal(page.results.length, 1);
    assert.equal(page.totalResults, 1);
});

test('a municipality search with no query or no country asks nothing and finds nothing', async () => {
    const { controller, fetch } = await controllerWith([]);
    const log = console.log;
    console.log = () => { };
    try {
        assert.deepEqual(await controller.searchMunicipalities('', 'LB'), { totalResults: 0, results: [] });
        assert.deepEqual(await controller.searchMunicipalities('Jou', ''), { totalResults: 0, results: [] });
    } finally {
        console.log = log;
    }

    assert.equal(fetch.requests.length, 0);
});

test('a refused or failed municipality search is an empty page', async () => {
    const error = console.error;
    console.error = () => { };
    try {
        for (const answer of [500, new Error('offline')]) {
            const { controller } = await controllerWith([['search/fuzzy', answer]]);

            assert.deepEqual(await controller.searchMunicipalities('Jou', 'LB'), { totalResults: 0, results: [] });
        }
    } finally {
        console.error = error;
    }
});

// --- boundary shapes ------------------------------------------------------------------------------------------------------------------

const polygon = [[[0, 0], [1, 0], [1, 1], [0, 0]]];

test('a boundary is fetched by geometry id and unwrapped from the Azure response', async () => {
    const { controller, fetch } = await controllerWith([['search/polygon/json', { additionalData: [{ geometryData: { type: 'FeatureCollection', features: [{ geometry: { type: 'Polygon', coordinates: polygon } }] } }] }]]);

    const rings = await controller.getPolygon('geo-1');

    assert.deepEqual(rings, polygon);
    assert.equal(fetch.params('search/polygon/json').geometries, 'geo-1');
});

test('with no geometry id there is no boundary and no request', async () => {
    const { controller, fetch } = await controllerWith([]);

    assert.equal(await controller.getPolygon(null), null);
    assert.equal(await controller.getPolygon(''), null);
    assert.equal(fetch.requests.length, 0);
});

test('a boundary lookup that is refused, empty or fails gives null', async () => {
    for (const answer of [404, {}, { additionalData: [] }, new Error('offline')]) {
        const { controller } = await controllerWith([['search/polygon/json', answer]]);

        assert.equal(await controller.getPolygon('geo-1'), null, JSON.stringify(answer));
    }
});

test('the current boundary service is asked for the area containing a point, newest version first', async () => {
    const { controller, fetch } = await controllerWith([['search/polygon?', { type: 'Polygon', coordinates: polygon }]]);

    const rings = await controller.getBoundaryPolygon(33.89, 35.5, 'adminDistrict');

    assert.deepEqual(rings, polygon);
    const params = fetch.params('search/polygon?');
    assert.equal(params['api-version'], '2026-01-01');
    assert.equal(params.coordinates, '35.5,33.89', 'longitude first');
    assert.equal(params.resultType, 'adminDistrict');
    assert.equal(params.resolution, 'medium');
});

test('an area type that was not given is a locality', async () => {
    const { controller, fetch } = await controllerWith([['search/polygon?', { type: 'Polygon', coordinates: polygon }]]);

    await controller.getBoundaryPolygon(33.89, 35.5);

    assert.equal(fetch.params('search/polygon?').resultType, 'locality');
});

test('when the newest boundary version is refused the older one is tried', async () => {
    const { controller, fetch } = await controllerWith([
        ['api-version=2026-01-01', 400],
        ['api-version=2025-01-01', { type: 'Polygon', coordinates: polygon }]
    ]);

    assert.deepEqual(await controller.getBoundaryPolygon(33.89, 35.5, 'locality'), polygon);
    assert.equal(fetch.requests.length, 2);
});

test('when neither boundary version answers there is no boundary', async () => {
    const { controller } = await controllerWith([['search/polygon?', 500]]);

    assert.equal(await controller.getBoundaryPolygon(33.89, 35.5), null);
});

test('a boundary lookup needs a real point', async () => {
    const { controller, fetch } = await controllerWith([]);

    assert.equal(await controller.getBoundaryPolygon(Number.NaN, 35), null);
    assert.equal(await controller.getBoundaryPolygon(33, 'x'), null);
    assert.equal(fetch.requests.length, 0);
});

test('a very detailed boundary is thinned so a point-in-area check cannot freeze the page', async () => {
    const big = Array.from({ length: 5000 }, (_, i) => [Math.cos(i / 5000 * 2 * Math.PI), Math.sin(i / 5000 * 2 * Math.PI)]);
    big.push(big[0]);
    const { controller } = await controllerWith([['search/polygon/json', { type: 'Polygon', coordinates: [big] }]]);

    const [thinned] = await controller.getPolygon('geo-1');

    assert.ok(thinned.length <= 502, `kept ${thinned.length} points`);
    assert.deepEqual(thinned[0], thinned.at(-1), 'the ring is still closed');
});

test('a boundary that is already small is left exactly as it is', async () => {
    const { controller } = await controllerWith([['search/polygon/json', { type: 'Polygon', coordinates: polygon }]]);

    assert.deepEqual(await controller.getPolygon('geo-1'), polygon);
});

// --- reading a polygon out of a response ----------------------------------------------------------------------------------------------

test('every GeoJSON wrapper Azure has used is unwrapped to the same rings', async () => {
    const { controller } = await controllerWith([]);
    const wrappers = {
        polygon: { type: 'Polygon', coordinates: polygon },
        feature: { type: 'Feature', geometry: { type: 'Polygon', coordinates: polygon } },
        collection: { type: 'FeatureCollection', features: [{ geometry: { type: 'Polygon', coordinates: polygon } }] },
        geometries: { type: 'GeometryCollection', geometries: [{ type: 'Polygon', coordinates: polygon }] },
        azure: { additionalData: [{ geometryData: { type: 'Polygon', coordinates: polygon } }] }
    };

    for (const [name, wrapper] of Object.entries(wrappers))
        assert.deepEqual(controller._extractPolygonCoords(wrapper), polygon, name);
});

test('of several polygons the one with the longest outer ring is taken', async () => {
    const { controller } = await controllerWith([]);
    const small = [[[0, 0], [1, 0], [1, 1], [0, 0]]];
    const large = [[[0, 0], [5, 0], [5, 5], [3, 6], [0, 5], [0, 0]]];

    assert.deepEqual(controller._extractPolygonCoords({ type: 'MultiPolygon', coordinates: [small, large, small] }), large);
});

test('a response with no polygon gives nothing', async () => {
    const { controller } = await controllerWith([]);

    for (const value of [null, undefined, 'x', 5, {}, { type: 'Point', coordinates: [1, 2] }, { type: 'FeatureCollection', features: [] }])
        assert.equal(controller._extractPolygonCoords(value), null, JSON.stringify(value));
});

// --- resolving a name to an outline -----------------------------------------------------------------------------------------------

const fuzzyThenPolygon = (places, polygonAnswer = { type: 'Polygon', coordinates: polygon }) => [
    ['search/fuzzy', url => places(new URL(url).searchParams)],
    ['search/polygon/json', polygonAnswer],
    ['search/polygon?', polygonAnswer]
];

test('a town is outlined from the geography index at the level asked for when its name matches', async () => {
    const { controller, fetch } = await controllerWith(fuzzyThenPolygon(() => ({ results: [town()] })));

    const rings = await controller.resolveBoundary('Jounieh, Lebanon', ['Municipality'], 'LB');

    assert.deepEqual(rings, polygon);
    const first = fetch.params('search/fuzzy');
    assert.equal(first.entityType, 'Municipality');
    assert.equal(first.countrySet, 'LB');
});

test('a match for some other place is not outlined: asking for Hamra must not draw all of Beirut', async () => {
    const beirut = town({ address: { municipality: 'Beirut', country: 'Lebanon' }, dataSources: { geometry: { id: 'beirut' } } });
    const { controller, fetch } = await controllerWith(fuzzyThenPolygon(() => ({ results: [beirut] })));

    await controller.resolveBoundary('Hamra, Beirut', ['Neighbourhood'], 'LB');

    assert.equal(fetch.requests.filter(url => url.includes('polygon/json')).length, 0, 'the geometry of the mismatched place is not used');
});

test('a spelling variant of the place asked for still counts as the place', async () => {
    const baabda = town({ address: { municipality: 'Baabda' }, dataSources: { geometry: { id: 'baabda' } } });
    const { controller } = await controllerWith(fuzzyThenPolygon(() => ({ results: [baabda] })));

    assert.deepEqual(await controller.resolveBoundary('Babda', ['Municipality'], 'LB'), polygon);
});

test('levels are tried in order and the first one with a shape wins', async () => {
    const asked = [];
    const { controller } = await controllerWith(fuzzyThenPolygon(params => {
        asked.push(params.get('entityType'));
        return { results: [town({ address: { municipality: 'Hamra', neighbourhood: 'Hamra' } })] };
    }));

    await controller.resolveBoundary('Hamra', ['Municipality', 'Neighbourhood'], 'LB');

    assert.deepEqual(asked, ['Municipality']);
});

test('with no level asked for, the best match is outlined from its own geometry', async () => {
    const { controller, fetch } = await controllerWith(fuzzyThenPolygon(() => ({ results: [town()] })));

    const rings = await controller.resolveBoundary('Jounieh');

    assert.deepEqual(rings, polygon);
    assert.equal(fetch.requests.filter(url => url.includes('polygon/json')).length, 1);
});

test('when no shape is held for the match the point is outlined at the level asked for through the current boundary service', async () => {
    const { controller, fetch } = await controllerWith([
        ['search/fuzzy', { results: [town()] }],
        ['search/polygon/json', {}],
        ['search/polygon?', { type: 'Polygon', coordinates: polygon }]
    ]);

    const rings = await controller.resolveBoundary('Jounieh', ['CountrySubdivision'], 'LB');

    assert.deepEqual(rings, polygon);
    assert.equal(fetch.params('search/polygon?').resultType, 'adminDistrict', 'CountrySubdivision is an adminDistrict');
});

test('an unknown place has no boundary', async () => {
    const { controller } = await controllerWith(fuzzyThenPolygon(() => ({ results: [] })));

    assert.equal(await controller.resolveBoundary('Atlantis', ['Municipality'], 'LB'), null);
});

test('each Azure entity type is outlined with the matching boundary type', async () => {
    const { controller } = await controllerWith([]);
    const expected = {
        Country: 'countryRegion', CountrySubdivision: 'adminDistrict', CountrySecondarySubdivision: 'adminDistrict2',
        CountryTertiarySubdivision: 'adminDistrict2', Neighbourhood: 'neighborhood', PostalCodeArea: 'postalCode',
        Municipality: 'locality', MunicipalitySubdivision: 'locality', Anything: 'locality', undefined: 'locality'
    };

    for (const [entityType, boundaryType] of Object.entries(expected))
        assert.equal(controller._boundaryTypeFor(entityType === 'undefined' ? undefined : entityType), boundaryType, entityType);
});

// --- place-name matching -----------------------------------------------------------------------------------------------------------

test('names are compared without accents, case, spaces or punctuation', async () => {
    const { controller } = await controllerWith([]);

    assert.equal(controller._normalizePlaceName('  Zahlé-Maallaqa! '), 'zahlemaallaqa');
    assert.equal(controller._normalizePlaceName('Beyrouth'), 'beyrouth');
    assert.equal(controller._normalizePlaceName(null), '');
});

test('the edit distance counts insertions, deletions and substitutions', async () => {
    const { controller } = await controllerWith([]);

    assert.equal(controller._editDistance('babda', 'baabda'), 1);
    assert.equal(controller._editDistance('kitten', 'sitting'), 3);
    assert.equal(controller._editDistance('', 'abc'), 3);
    assert.equal(controller._editDistance('same', 'same'), 0);
});

test('a name matches when it contains, is contained in, or is a few letters from the wanted one', async () => {
    const { controller } = await controllerWith([]);
    const matches = (names, wanted) => controller._placeNameMatches({ names }, controller._normalizePlaceName(wanted));

    assert.equal(matches(['Mount Lebanon Governorate'], 'Lebanon'), true);
    assert.equal(matches(['Beirut'], 'Greater Beirut'), true);
    assert.equal(matches(['Baabda'], 'Babda'), true);
    assert.equal(matches(['Tripoli'], 'Beirut'), false);
    assert.equal(matches([], 'Beirut'), false);
    assert.equal(matches(['Anything'], ''), true, 'no name wanted accepts any match');
});
