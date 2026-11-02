// A stand-in for the browser and for the Azure Maps Web SDK (`atlas`), so the real mapInterop.js can be loaded and driven from Node.
// Nothing here knows how mapInterop.js works; it only records what the code asks the SDK and the DOM to do, and lets a test make the
// "browser" do things back (fire a map click, answer a fetch).

import { pathToFileURL } from 'node:url';
import path from 'node:path';

const here = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'));

/** The Maps project's wwwroot folder, found relative to this file. */
export const mapsRoot = path.resolve(here, '..', '..', '..', 'CloudComponents.Maps', 'wwwroot');

export class FakeElement {
    constructor(tag = 'div') {
        this.tagName = tag.toUpperCase();
        this.children = [];
        this.attributes = {};
        this.listeners = {};
        this.style = { cssText: '' };
        this.parentElement = null;
        this.textContent = '';
        this.innerHTML = '';
        this.clientWidth = 800;
        this.clientHeight = 600;
        this.removed = false;
        const classes = new Set();
        this.classes = classes;
        this.classList = {
            add: (...names) => names.forEach(name => classes.add(name)),
            remove: (...names) => names.forEach(name => classes.delete(name)),
            contains: name => classes.has(name),
            toggle: (name, force) => {
                const on = force === undefined ? !classes.has(name) : !!force;
                if (on) classes.add(name); else classes.delete(name);
                return on;
            }
        };
    }

    addEventListener(type, listener, options) {
        (this.listeners[type] ??= []).push({ listener, options });
    }

    /** Runs every listener registered for `type`, the way the browser would. */
    dispatch(type, event = {}) {
        for (const { listener } of this.listeners[type] ?? [])
            listener(event);
    }

    appendChild(child) {
        child.parentElement = this;
        this.children.push(child);
        return child;
    }

    remove() {
        this.removed = true;
        if (this.parentElement) this.parentElement.children = this.parentElement.children.filter(c => c !== this);
    }

    setAttribute(name, value) { this.attributes[name] = String(value); }
    getAttribute(name) { return this.attributes[name] ?? null; }
    closest() { return null; }
}

class Recorder {
    constructor(kind, args) {
        this.kind = kind;
        this.args = args;
        this.options = args[args.length - 1];
    }
}

/** A Shape, a DataSource, a layer and so on: remembers how it was built and nothing else. */
function recorder(kind) {
    return class extends Recorder {
        constructor(...args) { super(kind, args); }
        getId() { return this.options?.id ?? this.args[1] ?? kind; }
        dispose() { this.disposed = true; }
    };
}

class FakeDataSource extends Recorder {
    constructor(id, options) { super('DataSource', [id, options]); this.id = id ?? `ds-${Math.random().toString(36).slice(2)}`; this.shapes = []; }
    getId() { return this.id; }
    add(shape) { Array.isArray(shape) ? this.shapes.push(...shape) : this.shapes.push(shape); }
    setShapes(shapes) { this.shapes = [...shapes]; }
    getShapes() { return [...this.shapes]; }
    remove(shape) { this.shapes = this.shapes.filter(s => s !== shape); }
    clear() { this.shapes = []; }
    dispose() { this.disposed = true; }
    async getClusterExpansionZoom() { return 14; }
}

class FakeShape {
    constructor(geometry, id, properties) { this.geometry = geometry; this.id = id; this.properties = properties; }
    getId() { return this.id; }
}

class FakeHtmlMarker {
    constructor(options) { this.options = options; this.element = new FakeElement(); }
    getOptions() { return this.options; }
    getElement() { return this.element; }
}

class FakePopup {
    constructor(options) { this.options = options; this.isOpen = false; this.openCount = 0; }
    open(map) { this.isOpen = true; this.openCount++; this.openedOn = map; }
    close() { this.isOpen = false; }
    setOptions(options) { this.options = { ...this.options, ...options }; }
}

export class FakeMap {
    constructor(elementId, options) {
        this.elementId = elementId;
        this.options = options;
        this.disposed = false;
        this.listeners = [];
        this.camera = {
            center: options.center,
            zoom: options.zoom,
            pitch: options.pitch ?? 0,
            bearing: options.bearing ?? 0
        };
        this.cameraCalls = [];
        this.styleCalls = [];
        this.trafficCalls = [];
        this.interactionCalls = [];
        this.currentStyle = options.style;
        this.controlsList = [];
        this.markerList = [];
        this.sourceList = [];
        this.layerList = [];
        this.canvas = new FakeElement('canvas');
        this.container = new FakeElement('div');
        this.container.appendChild(this.canvas);

        this.imageSprite = {
            created: [],
            hasImage: id => this.imageSprite.created.includes(id),
            createFromTemplate: async id => { this.imageSprite.created.push(id); }
        };
        this.events = {
            add: (type, targetOrHandler, handler) => {
                const hasTarget = typeof targetOrHandler !== 'function';
                this.listeners.push({ type, target: hasTarget ? targetOrHandler : null, handler: hasTarget ? handler : targetOrHandler });
            },
            remove: () => { }
        };
        this.controls = {
            add: (control, options) => this.controlsList.push({ control, options }),
            remove: control => { this.controlsList = this.controlsList.filter(entry => entry.control !== control); }
        };
        this.markers = {
            add: marker => this.markerList.push(marker),
            remove: marker => { this.markerList = this.markerList.filter(m => m !== marker); }
        };
        this.sources = {
            add: source => this.sourceList.push(source),
            remove: source => { this.sourceList = this.sourceList.filter(s => s !== source); },
            getById: id => this.sourceList.find(s => s.getId() === id) ?? null
        };
        this.layers = {
            add: layer => Array.isArray(layer) ? this.layerList.push(...layer) : this.layerList.push(layer),
            remove: layer => { this.layerList = this.layerList.filter(l => l !== layer); },
            getLayerById: id => this.layerList.find(l => l.getId() === id) ?? null
        };
    }

    /** Fires the map-level (or, with `target`, the shape/marker-level) event, the way the SDK does. */
    fire(type, event = {}, target = null) {
        for (const l of [...this.listeners])
            if (l.type === type && l.target === target)
                l.handler(event);
    }

    getCamera() { return { ...this.camera }; }

    setCamera(options) {
        this.cameraCalls.push(options);
        if (options.center) this.camera.center = options.center;
        if (options.zoom != null) this.camera.zoom = options.zoom;
        if (options.pitch != null) this.camera.pitch = options.pitch;
        if (options.bearing != null) this.camera.bearing = options.bearing;
        if (options.bounds) {
            const [west, south, east, north] = options.bounds;
            this.camera.center = [(west + east) / 2, (south + north) / 2];
        }
    }

    setStyle(options) { this.styleCalls.push(options); this.currentStyle = options.style; }
    getStyle() { return { style: this.currentStyle }; }
    setTraffic(options) { this.trafficCalls.push(options); }
    setUserInteraction(options) { this.interactionCalls.push(options); }
    getCanvas() { return this.canvas; }
    getCanvasContainer() { return this.container; }
    getMapContainer() { return this.container; }
    dispose() { this.disposed = true; }
}

/** The .NET side: records every call the JavaScript makes back into C#. */
export function createDotNetRef() {
    const calls = [];
    return {
        calls,
        invokeMethodAsync(method, ...args) { calls.push({ method, args }); return Promise.resolve(); },
        /** The arguments of every call to `method`, in order. */
        to(method) { return calls.filter(call => call.method === method).map(call => call.args); },
        clear() { calls.length = 0; }
    };
}

/** Replaces `fetch` with a script. `routes` is a list of [matcher, response]; a response is data, a status, or a function. */
export function mockFetch(routes = []) {
    const requests = [];

    globalThis.fetch = async (url) => {
        const text = String(url);
        requests.push(text);

        for (const [matcher, response] of routes) {
            const matches = typeof matcher === 'string' ? text.includes(matcher) : matcher instanceof RegExp ? matcher.test(text) : matcher(text);
            if (!matches) continue;

            const answer = typeof response === 'function' ? await response(text) : response;
            if (answer instanceof Error) throw answer;
            if (typeof answer === 'number') return { ok: answer >= 200 && answer < 300, status: answer, json: async () => ({}) };
            return { ok: true, status: 200, json: async () => answer };
        }

        return { ok: false, status: 404, json: async () => ({}) };
    };

    return {
        requests,
        /** Every query-string value of the first request that contains `fragment`, decoded. */
        params(fragment) {
            const request = requests.find(r => r.includes(fragment));
            if (!request) throw new Error(`No request contained "${fragment}". Requests: ${requests.join(' | ')}`);
            return Object.fromEntries(new URL(request).searchParams.entries());
        }
    };
}

/**
 * Installs the fake browser and SDK as globals and loads the real mapInterop.js.
 * Returns the module and helpers to build controllers. Call once per test file.
 */
export async function installWorld() {
    const mapsCreated = [];
    const documentHead = new FakeElement('head');

    const atlas = {
        Map: class extends FakeMap { constructor(id, options) { super(id, options); mapsCreated.push(this); } },
        HtmlMarker: FakeHtmlMarker,
        Popup: FakePopup,
        Shape: FakeShape,
        source: { DataSource: FakeDataSource },
        layer: { LineLayer: recorder('LineLayer'), PolygonLayer: recorder('PolygonLayer'), SymbolLayer: recorder('SymbolLayer'), BubbleLayer: recorder('BubbleLayer') },
        control: Object.fromEntries(['ZoomControl', 'CompassControl', 'PitchControl', 'StyleControl', 'FullscreenControl', 'ScaleControl'].map(name => [name, recorder(name)])),
        data: {
            Polygon: class { constructor(coordinates) { this.type = 'Polygon'; this.coordinates = coordinates; } },
            MultiPolygon: class { constructor(coordinates) { this.type = 'MultiPolygon'; this.coordinates = coordinates; } },
            LineString: class { constructor(coordinates) { this.type = 'LineString'; this.coordinates = coordinates; } },
            Point: class { constructor(coordinates) { this.type = 'Point'; this.coordinates = coordinates; } },
            Feature: class { constructor(geometry, properties) { this.type = 'Feature'; this.geometry = geometry; this.properties = properties; } }
        }
    };

    const elementsById = new Map();
    const createdElements = [];

    globalThis.window = globalThis;
    globalThis.Element = FakeElement;
    globalThis.document = {
        head: documentHead,
        getElementById: id => {
            if (!elementsById.has(id)) elementsById.set(id, new FakeElement('div'));
            return elementsById.get(id);
        },
        querySelector: () => null,
        createElement: tag => {
            const element = new FakeElement(tag);
            createdElements.push(element);
            return element;
        }
    };
    Object.defineProperty(globalThis, 'navigator', { value: { geolocation: undefined, permissions: undefined }, configurable: true, writable: true });

    const mapInterop = await import(pathToFileURL(path.join(mapsRoot, 'mapInterop.js')).href);
    const routes = await import(pathToFileURL(path.join(mapsRoot, 'mapRoutes.js')).href);

    let counter = 0;

    /** Builds a controller through the module's own entry point, with the SDK already "loaded". */
    async function createController(options = {}) {
        globalThis.atlas = atlas;
        const dotNetRef = createDotNetRef();
        const mapOptions = {
            elementId: `map-${++counter}`,
            latitude: 0,
            longitude: 0,
            zoom: 12,
            subscriptionKey: 'test-key',
            controls: {},
            ...options
        };
        const controller = await mapInterop.createMap(dotNetRef, mapOptions);
        const map = mapsCreated[mapsCreated.length - 1];
        return { controller, map, dotNetRef, options: mapOptions };
    }

    /** A controller whose map has finished loading, so controls, traffic and the click handlers are in place. */
    async function createReadyController(options = {}) {
        const created = await createController(options);
        created.map.fire('ready');
        return created;
    }

    const created = [];
    const createControllerTracked = createController;

    /** Disposes every controller this world built, so a timer one left running cannot hold the process open. */
    function disposeAll() {
        for (const controller of created.splice(0))
            try { controller.dispose(); } catch { /* already disposed */ }
    }

    const track = async (promise) => { const result = await promise; created.push(result.controller); return result; };

    return {
        atlas, mapInterop, routes, createdElements, documentHead, elementsById, mapsCreated, disposeAll,
        createController: options => track(createControllerTracked(options)),
        createReadyController: options => track(createReadyController(options))
    };
}
