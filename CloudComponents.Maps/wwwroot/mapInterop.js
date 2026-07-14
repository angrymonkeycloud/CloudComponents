// ES module — loaded via Blazor JS isolation:
// import('./_content/CloudComponents.Maps/mapInterop.js')
// The Azure Maps Web SDK (atlas) is loaded on-demand by this module.

const ATLAS_VERSION = '3';
const ATLAS_CSS_URL = `https://atlas.microsoft.com/sdk/javascript/mapcontrol/${ATLAS_VERSION}/atlas.min.css`;
const ATLAS_JS_URL = `https://atlas.microsoft.com/sdk/javascript/mapcontrol/${ATLAS_VERSION}/atlas.min.js`;

// Country-level boundaries from Azure Maps' polygon API can have tens of
// thousands of vertices per ring (e.g. detailed coastlines). Running a
// synchronous point-in-polygon ray-cast against that many points blocks the
// single UI thread shared by the browser and Blazor WebAssembly, which can
// make the entire page appear frozen — not just the map. Rings are
// decimated to at most this many vertices before being used for
// interactive location-lock checks or region-overlay rendering.
const MAX_LOCK_RING_VERTICES = 500;

let _atlasLoader = null;

function loadAzureMapsSdk() {
    if (typeof window !== 'undefined' && window.atlas) return Promise.resolve();
    if (_atlasLoader) return _atlasLoader;

    _atlasLoader = new Promise((resolve, reject) => {
        if (!document.querySelector('link[data-cc-atlas]')) {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = ATLAS_CSS_URL;
            link.setAttribute('data-cc-atlas', '');
            document.head.appendChild(link);
        }

        let script = document.querySelector('script[data-cc-atlas]');
        if (script) {
            if (window.atlas) { resolve(); return; }
            script.addEventListener('load', () => resolve());
            script.addEventListener('error', () => reject(new Error('Failed to load Azure Maps SDK.')));
            return;
        }

        script = document.createElement('script');
        script.src = ATLAS_JS_URL;
        script.async = true;
        script.setAttribute('data-cc-atlas', '');
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load Azure Maps SDK.'));
        document.head.appendChild(script);
    });

    return _atlasLoader;
}

// ----- Entry points ---------------------------------------------------------

export async function createMap(dotNetRef, options) {
    await loadAzureMapsSdk();
    return new AzureMapController(dotNetRef, options);
}

export function getCurrentLocation() {
    // First fix from getCurrentPosition with cached/network fallback is often
    // tens of kilometers off. We force a fresh, high-accuracy fix and then
    // refine it via watchPosition for up to ~6 s, returning the best reading.
    return new Promise(resolve => {
        if (!navigator.geolocation) { resolve(null); return; }

        const opts = { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 };
        let best = null;
        let watchId = null;
        let finished = false;

        const finish = () => {
            if (finished) return;
            finished = true;
            if (watchId != null) navigator.geolocation.clearWatch(watchId);
            resolve(best ? [best.coords.latitude, best.coords.longitude] : null);
        };

        const consider = (pos) => {
            if (!best || (pos.coords.accuracy ?? Infinity) < (best.coords.accuracy ?? Infinity))
                best = pos;
            // Accept once we get to ~50 m or better.
            if (best && (best.coords.accuracy ?? Infinity) <= 50) finish();
        };

        navigator.geolocation.getCurrentPosition(consider, finish, opts);
        try { watchId = navigator.geolocation.watchPosition(consider, () => { }, opts); } catch { /* noop */ }

        // Hard cap so we never hang the UI.
        setTimeout(finish, 6000);
    });
}

/**
 * Returns 'granted' | 'denied' | 'prompt' | 'unsupported' without showing
 * the OS permission dialog. Used by the Blazor component to decide whether
 * to display the "allow location" confirmation popup.
 */
export async function queryLocationPermission() {
    if (typeof navigator === 'undefined' || !navigator.geolocation) return 'unsupported';
    if (!navigator.permissions?.query) return 'prompt';
    try {
        const status = await navigator.permissions.query({ name: 'geolocation' });
        return status.state; // 'granted' | 'denied' | 'prompt'
    } catch {
        return 'prompt';
    }
}

// ----- Maps controller -------------------------------------------------------

class AzureMapController {
    constructor(dotNetRef, options) {
        this._dotNetRef = dotNetRef;
        this._options = options;
        this._markers = new Map();
        this._regions = [];
        this._regionDataSource = null;
        this._currentLocationMarker = null;
        this._activePopupId = null;
        this._lockPolygons = null;
        this._lockBounds = null;
        this._lockDataSource = null;
        this._lastAllowedCenter = null;
        this._trafficFlow = !!options.showTrafficFlow;
        this._trafficIncidents = !!options.showTrafficIncidents;
        this._addTrigger = options.addMarkerTrigger || 'double';   // 'disabled' | 'single' | 'double'
        this._controlInstances = {};   // key -> { control, position } — lets setControls() diff/re-apply at runtime
        this._scrollHintEl = null;
        this._scrollHintTimer = null;

        const mapOptions = {
            center: [options.longitude, options.latitude],
            zoom: options.zoom,
            pitch: options.pitch ?? 0,
            bearing: options.bearing ?? 0,
            style: options.style || 'road',
            language: options.language || 'en-US',
            view: options.view || 'Auto',
            interactive: options.interactive !== false,
            authOptions: {
                authType: 'subscriptionKey',
                subscriptionKey: options.subscriptionKey
            }
        };
        if (options.minZoom != null) mapOptions.minZoom = options.minZoom;
        if (options.maxZoom != null) mapOptions.maxZoom = options.maxZoom;

        const inter = options.interactions || {};
        // Cached so runtime reads (scroll-zoom gate, setInteractions diffing) always
        // reflect the live state instead of re-deriving defaults each time.
        this._interactions = {
            dragPan: inter.dragPan !== false,
            dragRotate: inter.dragRotate !== false,
            scrollZoom: inter.scrollZoom !== false,
            dblClickZoom: inter.dblClickZoom !== false,
            boxZoom: inter.boxZoom !== false,
            keyboard: inter.keyboard !== false,
            touch: inter.touch !== false
        };
        if (inter.dragPan === false) mapOptions.dragPanInteraction = false;
        if (inter.dragRotate === false) mapOptions.dragRotateInteraction = false;
        if (inter.scrollZoom === false) mapOptions.scrollZoomInteraction = false;
        if (inter.dblClickZoom === false) mapOptions.dblClickZoomInteraction = false;
        if (inter.boxZoom === false) mapOptions.boxZoomInteraction = false;
        if (inter.keyboard === false) mapOptions.keyboardInteraction = false;
        if (inter.touch === false) mapOptions.touchInteraction = false;

        this._map = new atlas.Map(options.elementId, mapOptions);
        this._map.events.add('ready', () => this._onReady());
        this._map.events.add('error', (e) => this._onMapSdkError(e));
    }

    // -- Lifecycle --------------------------------------------------------

    _onReady() {
        this._addControls();
        this._applyTraffic();

        // Maps clicks ? notify .NET; optionally drop a pin on single-click.
        this._map.events.add('click', (e) => {
            if (!e?.position) return;
            const [lng, lat] = e.position;

            // Azure Maps fires the map-level 'click' for the same gesture that
            // hit a marker. The marker handler sets a one-shot flag so we
            // don't dismiss the popup we just opened.
            if (this._suppressNextMapClick) {
                this._suppressNextMapClick = false;
            } else if (!this._wasOnMarker(e)) {
                // Plain background click ? dismiss any open marker popup.
                this._closeActivePopup();
            }

            this._dotNetRef?.invokeMethodAsync('NotifyMapClickAsync', lat, lng);

            if (this._addTrigger === 'single' && !this._wasOnMarker(e)) {
                if (this._isPointAllowed(lng, lat)) {
                    this._dotNetRef?.invokeMethodAsync('NotifyMapAddMarkerAsync', lat, lng);
                } else {
                    this._dotNetRef?.invokeMethodAsync('NotifyLocationLockRejectedAsync', lat, lng);
                }
            }
        });

        // Native dblclick ? drop a pin (when configured).
        this._map.events.add('dblclick', (e) => {
            if (this._addTrigger !== 'double') return;
            if (!e?.position) return;
            if (this._wasOnMarker(e)) return;
            const [lng, lat] = e.position;
            if (this._isPointAllowed(lng, lat)) {
                this._dotNetRef?.invokeMethodAsync('NotifyMapAddMarkerAsync', lat, lng);
            } else {
                this._dotNetRef?.invokeMethodAsync('NotifyLocationLockRejectedAsync', lat, lng);
            }
        });

        // Center-pin mode ? broadcast the camera-center coordinate whenever the
        // camera finishes moving for any reason (drag, search fly-to, "pin my
        // location", setCenter/setBounds from C#, etc.), but not while a
        // zoom-driven camera animation is still in flight — zooming is always
        // anchored to the map center (pin position) regardless of cursor location.
        // These listeners are always registered (not just when the map starts in
        // center-pin mode) so that switching AddMarkerTrigger to CenterPin at
        // runtime via setAddMarkerTrigger() works without recreating the map;
        // _firePan itself no-ops unless this._addTrigger is currently 'center'.
        let _isZooming = false;
        this._lastAllowedCenter = this._map.getCamera().center; // [lng, lat]

        this._firePan = () => {
            if (this._addTrigger !== 'center') return; // only active in center-pin mode
            if (_isZooming) return;
            const c = this._map.getCamera().center;   // [lng, lat]
            if (!c) return;

            if (!this._isPointAllowed(c[0], c[1])) {
                // Only snap back when the last known-good center is itself inside the
                // locked area. If it is outside (e.g. the map loaded with the pin
                // outside the lock), do NOT snap — the user must be able to drag the
                // pin into the allowed area from its current position.
                const revertTo = this._lastAllowedCenter;
                if (revertTo && this._isPointAllowed(revertTo[0], revertTo[1])) {
                    this._map.setCamera({ center: revertTo, type: 'ease', duration: 200 });
                }
                this._dotNetRef?.invokeMethodAsync('NotifyLocationLockRejectedAsync', c[1], c[0]);
                return;
            }

            this._lastAllowedCenter = c;
            this._dotNetRef?.invokeMethodAsync('NotifyCenterPinChangedAsync', c[1], c[0]);
        };

        // Track zoom start/end so we can suppress pan events during zoom
        this._map.events.add('zoomstart', () => { _isZooming = true; });
        this._map.events.add('zoomend', () => { _isZooming = false; });

        // 'moveend' fires after ANY settled camera change — drag, search
        // fly-to (setBounds/setCenter), "pin my location", etc. — so the
        // tracked center coordinate (and .NET) always reflects where the
        // fixed pin visually ends up, not just manual drags.
        this._map.events.add('moveend', this._firePan);
        this._firePan(); // emit initial position (no-op unless already in center-pin mode)

        // Unified scroll-wheel zoom gate. Handles two things:
        //  1) Center-pin mode always zooms anchored to the map center (not the
        //     cursor) so the fixed pin never visually drifts.
        //  2) When ScrollZoomInteraction is disabled, a plain wheel/trackpad
        //     gesture is left alone (so the page can scroll normally past an
        //     embedded map) unless the user holds Ctrl/?/Shift, in which case we
        //     zoom anyway — the common "hold a modifier to zoom" pattern — and
        //     show a brief one-time hint the first time a bare scroll is ignored.
        this._setupScrollZoomGate();

        if (this._options.latitude !== 0 || this._options.longitude !== 0)
            this._setCurrentLocation(this._options.latitude, this._options.longitude);

        // Keep .NET in sync if the user switches style via the in-map StyleControl.
        this._map.events.add('styledata', () => {
            try {
                const s = this._map.getStyle()?.style;
                if (s && s !== this._options.style) {
                    this._options.style = s;
                    this._dotNetRef?.invokeMethodAsync('NotifyStyleChangedAsync', s);
                }
            } catch { /* noop */ }
        });

        this._dotNetRef?.invokeMethodAsync('NotifyMapReadyAsync');
    }

    _wasOnMarker(e) {
        // Azure Maps fires shape/marker events separately, but a map-level
        // 'click' will also fire. Detect overlap with any HtmlMarker DOM node.
        const target = e?.originalEvent?.target;
        if (!target || !(target instanceof Element)) return false;
        return !!target.closest('.azure-maps-html-marker, .atlas-map-htmlMarker');
    }

    _onMapSdkError(e) {
        const msg = (e && (e.message || e.error?.message)) || 'Azure Maps error';

        // Imagery tiles return 403 on Azure Maps SKUs without satellite. Auto-fall
        // back to 'road' so the user still gets a working map.
        const lower = String(msg).toLowerCase();
        if ((lower.includes('imagery') || lower.includes('403')) && this._options.style !== 'road') {
            try { this._map.setStyle({ style: 'road' }); } catch { /* noop */ }
        }

        this._dotNetRef?.invokeMethodAsync('NotifyMapErrorAsync', msg);
    }

    // -- Scroll-wheel zoom gate -------------------------------------------
    //
    // Handles two behaviors with a single wheel listener registered at the DOM
    // capture phase (BEFORE Azure Maps' own listener runs — see note on the
    // element hierarchy below):
    //   1) Center-pin mode always zooms anchored to the map center (not the
    //      cursor) so the fixed pin never visually drifts.
    //   2) When ScrollZoomInteraction is off, a bare wheel/trackpad gesture is
    //      left alone so the page can scroll past an embedded map — unless the
    //      user holds Ctrl/?/Shift, the common "modifier + scroll to zoom"
    //      pattern, in which case we hand the gesture to the SDK's native
    //      (cursor-anchored) zoom for one gesture. A brief hint is shown the
    //      first time a bare scroll is ignored.
    _setupScrollZoomGate() {
        const mapCanvas = this._map.getCanvas();
        let revertTimer = null;

        mapCanvas.addEventListener('wheel', (e) => {
            const centerPin = this._addTrigger === 'center';

            if (centerPin) {
                // Always self-managed so the fixed pin never drifts, regardless
                // of the configured ScrollZoomInteraction value.
                e.stopImmediatePropagation();
                e.preventDefault();
                this._zoomAroundCenter(e);
                return;
            }

            if (this._interactions.scrollZoom) return; // native SDK zoom handles it

            const modifierHeld = e.ctrlKey || e.metaKey || e.shiftKey;
            if (!modifierHeld) {
                this._showScrollHint();
                return; // let the page scroll normally
            }

            // Temporarily hand this gesture to the SDK's own scroll-zoom handler
            // (cursor-anchored, matches native behavior) instead of reimplementing
            // zoom math, then restore the configured (disabled) state shortly
            // after the gesture appears to have ended.
            this._hideScrollHint();
            try { this._map.setUserInteraction({ scrollZoomInteraction: true }); } catch { /* noop */ }
            clearTimeout(revertTimer);
            revertTimer = setTimeout(() => {
                try { this._map.setUserInteraction({ scrollZoomInteraction: !!this._interactions.scrollZoom }); } catch { /* noop */ }
            }, 400);
        }, { capture: true, passive: false });
    }

    _zoomAroundCenter(e) {
        const cam = this._map.getCamera();
        if (!cam?.center) return;

        // Normalise delta across browsers/devices
        const rawDelta = e.deltaY ?? e.wheelDelta ?? 0;
        const direction = rawDelta > 0 ? -1 : 1;          // scroll down = zoom out
        const step = e.ctrlKey ? 0.25 : 1;                // pinch-to-zoom is finer
        const newZoom = Math.max(0, Math.min(24, (cam.zoom ?? 10) + direction * step));

        this._map.setCamera({
            zoom: newZoom,
            center: cam.center,   // keep the pin center fixed
            type: 'ease',
            duration: 150
        });
    }

    _showScrollHint() {
        if (this._scrollHintEl) {
            clearTimeout(this._scrollHintTimer);
            this._scrollHintTimer = setTimeout(() => this._hideScrollHint(), 1400);
            return;
        }

        try {
            const el = document.createElement('div');
            el.textContent = 'Hold Ctrl (?) or Shift + scroll to zoom the map';
            el.style.cssText =
                'position:absolute;top:12px;left:50%;transform:translateX(-50%);z-index:8;' +
                'padding:8px 14px;background:rgba(32,32,32,0.92);color:#fff;' +
                'font-family:system-ui,-apple-system,"Segoe UI",sans-serif;font-size:12.5px;' +
                'font-weight:500;border-radius:8px;box-shadow:0 4px 14px rgba(0,0,0,0.25);' +
                'pointer-events:none;white-space:nowrap;';

            const host = (this._map.getCanvasContainer?.()) || this._map.getCanvas().parentElement;
            host.appendChild(el);
            this._scrollHintEl = el;
        } catch { /* noop */ }

        this._scrollHintTimer = setTimeout(() => this._hideScrollHint(), 1400);
    }

    _hideScrollHint() {
        clearTimeout(this._scrollHintTimer);
        this._scrollHintTimer = null;
        if (this._scrollHintEl) {
            try { this._scrollHintEl.remove(); } catch { /* noop */ }
            this._scrollHintEl = null;
        }
    }

    _addControls() {
        const c = this._options.controls || {};
        Object.keys(c).forEach(key => this._addControl(key, c[key]));
    }

    /// Maps a control key to its Azure Maps SDK factory. Returns null for unknown keys.
    _controlFactory(key) {
        switch (key) {
            case 'zoom': return () => new atlas.control.ZoomControl();
            case 'compass': return () => new atlas.control.CompassControl();
            case 'pitch': return () => new atlas.control.PitchControl();
            case 'style':
                // Force the full style list so 'satellite' and 'satellite_road_labels'
                // always appear, regardless of the account's default style set.
                return () => new atlas.control.StyleControl({
                    mapStyles: [
                        'road',
                        'grayscale_light',
                        'grayscale_dark',
                        'night',
                        'road_shaded_relief',
                        'satellite',
                        'satellite_road_labels',
                        'high_contrast_dark',
                        'high_contrast_light'
                    ],
                    layout: 'list'
                });
            case 'fullscreen': return () => new atlas.control.FullscreenControl();
            case 'scale': return () => new atlas.control.ScaleControl();
            default: return null;
        }
    }

    _addControl(key, cfg) {
        if (!cfg || !cfg.enabled) return;
        const factory = this._controlFactory(key);
        if (!factory) return;
        try {
            const control = factory();
            const position = cfg.position || 'top-right';
            this._map.controls.add(control, { position });
            this._controlInstances[key] = { control, position };
        } catch { /* control unavailable in current SDK */ }
    }

    _removeControl(key) {
        const entry = this._controlInstances[key];
        if (!entry) return;
        try { this._map.controls.remove(entry.control); } catch { /* noop */ }
        delete this._controlInstances[key];
    }

    /// Runtime sync for map controls (zoom/compass/pitch/style/fullscreen/scale),
    /// called from C# (AzureMap.razor.cs OnParametersSetAsync) whenever a
    /// MapControlOption changes after the map was already created. Diffs against
    /// the currently-applied instances so untouched controls are left alone.
    setControls(controls) {
        if (!controls) return;
        Object.keys(controls).forEach(key => {
            const cfg = controls[key];
            const existing = this._controlInstances[key];
            const shouldShow = !!cfg?.enabled;
            const position = cfg?.position || 'top-right';

            if (!shouldShow) {
                if (existing) this._removeControl(key);
                return;
            }

            if (existing && existing.position === position) return; // already correct
            if (existing) this._removeControl(key); // position changed -> re-add there
            this._addControl(key, cfg);
        });
    }

    _applyTraffic() {
        try {
            this._map.setTraffic({
                flow: this._trafficFlow ? 'relative' : 'none',
                incidents: !!this._trafficIncidents
            });
        } catch { /* noop */ }
    }

    _setCurrentLocation(lat, lng) {
        if (this._currentLocationMarker)
            this._map.markers.remove(this._currentLocationMarker);

        this._currentLocationMarker = new atlas.HtmlMarker({
            anchor: 'center',
            position: [lng, lat],
            htmlContent:
                '<div style="width:16px;height:16px;border-radius:50%;background:#0078d4;' +
                'border:3px solid #fff;box-shadow:0 0 0 4px rgba(0,120,212,.25),0 0 8px rgba(0,120,212,.6);"></div>'
        });
        this._map.markers.add(this._currentLocationMarker);
    }

    // -- Public API (called from C#) --------------------------------------

    addMarker(info) {
        if (!info || !info.id || this._markers.has(info.id)) return;

        const marker = new atlas.HtmlMarker({
            position: [info.longitude, info.latitude],
            color: info.color || '#e81123'
        });
        this._map.markers.add(marker);

        const popup = new atlas.Popup({
            content: this._buildPopupContent(info),
            position: [info.longitude, info.latitude],
            pixelOffset: [0, -32],
            closeButton: true
        });

        const entry = { marker, popup };
        this._markers.set(info.id, entry);

        // Single click ? close any other open popup, open this one, notify .NET
        this._map.events.add('click', marker, () => {
            // Prevent the map-level 'click' that fires for the same gesture
            // from closing the popup we are about to open.
            this._suppressNextMapClick = true;
            this._closeActivePopup(info.id);
            try { entry.popup.open(this._map); this._activePopupId = info.id; } catch { /* noop */ }
            this._dotNetRef?.invokeMethodAsync('NotifyMarkerClickAsync', info.id);
        });

        // Keep our active-id in sync if the user closes the popup manually.
        this._map.events.add('close', entry.popup, () => {
            if (this._activePopupId === info.id) this._activePopupId = null;
        });

        // Double-click on a marker ? remove it (when enabled)
        // HtmlMarker doesn't expose 'dblclick' directly; bind via its DOM element.
        const el = marker.getOptions().htmlContent ? null : marker.getElement?.();
        const domNode = el || marker.getElement?.();
        if (domNode && this._options.allowMarkerRemoval) {
            domNode.addEventListener('dblclick', (ev) => {
                ev.stopPropagation();
                this._removeMarkerInternal(info.id);
                this._dotNetRef?.invokeMethodAsync('NotifyMarkerRemovedAsync', info.id);
            });
        }
    }

    removeMarker(id) { this._removeMarkerInternal(id); }

    _removeMarkerInternal(id) {
        const entry = this._markers.get(id);
        if (!entry) return;
        try { entry.popup.close(); } catch { /* noop */ }
        if (this._activePopupId === id) this._activePopupId = null;
        this._map.markers.remove(entry.marker);
        this._markers.delete(id);
    }

    _closeActivePopup(exceptId) {
        if (!this._activePopupId || this._activePopupId === exceptId) return;
        const prev = this._markers.get(this._activePopupId);
        if (prev) { try { prev.popup.close(); } catch { /* noop */ } }
        this._activePopupId = null;
    }

    clearMarkers() {
        this._markers.forEach(entry => {
            try { entry.popup.close(); } catch { /* noop */ }
            this._map.markers.remove(entry.marker);
        });
        this._markers.clear();
        this._activePopupId = null;
    }

    // -- Circle overlay API -----------------------------------------------

    addRegion(info) {
        if (!info) return;
        if (!this._regionDataSource) {
            this._regionDataSource = new atlas.source.DataSource();
            this._map.sources.add(this._regionDataSource);
            this._map.layers.add(new atlas.layer.PolygonLayer(this._regionDataSource, null, {
                fillColor: ['get', 'fillColor'],
                fillOpacity: 1
            }));
            this._map.layers.add(new atlas.layer.LineLayer(this._regionDataSource, null, {
                strokeColor: ['get', 'strokeColor'],
                strokeWidth: ['get', 'strokeWidth']
            }));
        }

        const props = {
            fillColor: info.fillColor || 'rgba(0, 120, 212, 0.15)',
            strokeColor: info.strokeColor || '#0078d4',
            strokeWidth: info.strokeWidth ?? 2
        };
        if (info.label) props.label = info.label;
        const shape = new atlas.Shape(new atlas.data.Polygon(info.coordinates), info.id, props);
        this._regionDataSource.add(shape);
        this._regions.push(info.id);
    }

    removeRegion(regionId) {
        if (!this._regionDataSource || !regionId) return;
        const shapes = this._regionDataSource.getShapes();
        for (const shape of shapes) {
            if (shape.getId() === regionId) {
                this._regionDataSource.remove(shape);
                break;
            }
        }
        this._regions = this._regions.filter(id => id !== regionId);
    }

    clearRegions() {
        if (this._regionDataSource) {
            this._regionDataSource.clear();
        }
        this._regions = [];
    }

    // -- Location lock ----------------------------------------------------

    /// Set the polygons that constrain interactive marker/center-pin selection.
    /// `polygons` is an array of GeoJSON Polygon coordinate rings (double[][][]).
    /// When `showBoundary` is true, also renders them as a region overlay.
    setLocationLock(polygons, options) {
        this._lockPolygons = Array.isArray(polygons) && polygons.length > 0 ? polygons : null;

        // Cache the combined bounding box once so `_isPointAllowed` can reject
        // most out-of-area points in O(1) instead of always paying for a full
        // ray-cast over every ring (see MAX_LOCK_RING_VERTICES comment above).
        this._lockBounds = this._lockPolygons ? this._computeBounds(this._lockPolygons) : null;

        this._clearLockBoundary();

        if (this._lockPolygons && options?.showBoundary) {
            if (!this._lockDataSource) {
                this._lockDataSource = new atlas.source.DataSource();
                this._map.sources.add(this._lockDataSource);
                this._map.layers.add(new atlas.layer.LineLayer(this._lockDataSource, null, {
                    strokeColor: options.strokeColor || '#107c10',
                    strokeWidth: 2,
                    strokeDashArray: [2, 2]
                }));
                this._map.layers.add(new atlas.layer.PolygonLayer(this._lockDataSource, null, {
                    fillColor: options.fillColor || 'rgba(16, 124, 16, 0.10)'
                }));
            }
            this._lockPolygons.forEach((rings, i) => {
                this._lockDataSource.add(new atlas.Shape(new atlas.data.Polygon(rings), `lock-${i}`));
            });
        }

        if (this._lockPolygons && options?.zoomToBoundary && this._lockBounds) {
            const b = this._lockBounds;
            this.setBounds(b.south, b.west, b.north, b.east, 40);
        }
    }

    clearLocationLock() {
        this._lockPolygons = null;
        this._lockBounds = null;
        this._clearLockBoundary();
    }

    _clearLockBoundary() {
        if (this._lockDataSource) this._lockDataSource.clear();
    }

    _computeBounds(polygons) {
        let north = -90, south = 90, east = -180, west = 180;
        let found = false;
        for (const rings of polygons) {
            for (const ring of rings) {
                for (const [lng, lat] of ring) {
                    found = true;
                    if (lat > north) north = lat;
                    if (lat < south) south = lat;
                    if (lng > east) east = lng;
                    if (lng < west) west = lng;
                }
            }
        }
        return found ? { north, south, east, west } : null;
    }

    /// True when no lock is set, or the [lng, lat] point falls within any locked polygon.
    _isPointAllowed(lng, lat) {
        if (!this._lockPolygons) return true;

        // Fast path: reject points clearly outside the combined bounding box of
        // all locked areas before paying for a ray-cast. Most "outside the
        // locked area" checks (e.g. selecting a search result far from the
        // locked country) are rejected here without touching polygon vertices,
        // which is what previously made searching inside a locked map freeze
        // the page.
        const b = this._lockBounds;
        if (b && (lat > b.north || lat < b.south || lng > b.east || lng < b.west))
            return false;

        return this._lockPolygons.some(rings => this._pointInRings(lng, lat, rings));
    }

    /// Public wrapper so C# can validate a coordinate (e.g. a resolved GPS fix)
    /// against the current location lock without duplicating polygon math.
    isPointAllowed(latitude, longitude) {
        return this._isPointAllowed(longitude, latitude);
    }

    // Ray-casting point-in-polygon, honoring holes (rings after the first are treated as
    // exclusions per GeoJSON convention).
    _pointInRings(lng, lat, rings) {
        if (!Array.isArray(rings) || rings.length === 0) return false;
        const inOuter = this._pointInRing(lng, lat, rings[0]);
        if (!inOuter) return false;
        for (let i = 1; i < rings.length; i++) {
            if (this._pointInRing(lng, lat, rings[i])) return false; // inside a hole
        }
        return true;
    }

    _pointInRing(lng, lat, ring) {
        let inside = false;
        for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
            const xi = ring[i][0], yi = ring[i][1];
            const xj = ring[j][0], yj = ring[j][1];
            const intersects = ((yi > lat) !== (yj > lat)) &&
                (lng < (xj - xi) * (lat - yi) / (yj - yi) + xi);
            if (intersects) inside = !inside;
        }
        return inside;
    }

    /// Geocode a query string using the Azure Maps Search API.
    /// When `entityType` is provided (e.g. "Country", "CountrySubdivision"), the search is
    /// restricted to the Geo index and that entity type only, so a query like "Lebanon" always
    /// resolves to the country itself instead of a same-named city/POI elsewhere in the world.
    /// `countrySet` further narrows results to one or more ISO 3166-1 alpha-2 country codes.
    /// Returns { latitude, longitude, north, south, east, west, geometryId } or null.
    async geocode(query, entityType, countrySet) {
        if (!query) return null;
        try {
            const key = this._options.subscriptionKey;
            // Use fuzzy search so free-text addresses, streets, and landmarks all resolve.
            let url = `https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&subscription-key=${encodeURIComponent(key)}&query=${encodeURIComponent(query)}&limit=1`;
            if (entityType) {
                // entityType is only honored by the API when idxSet includes Geo.
                url += `&idxSet=Geo&entityType=${encodeURIComponent(entityType)}`;
            }
            if (countrySet) url += `&countrySet=${encodeURIComponent(countrySet)}`;
            const resp = await fetch(url);
            if (!resp.ok) return null;
            const data = await resp.json();
            const r = data?.results?.[0];
            if (!r?.position) return null;
            const vp = r.viewport || r.boundingBox;
            const geoId = r.dataSources?.geometry?.id ?? null;
            return {
                latitude: r.position.lat,
                longitude: r.position.lon,
                north: vp?.topLeftPoint?.lat ?? r.position.lat,
                south: vp?.btmRightPoint?.lat ?? r.position.lat,
                east: vp?.btmRightPoint?.lon ?? r.position.lon,
                west: vp?.topLeftPoint?.lon ?? r.position.lon,
                geometryId: geoId
            };
        } catch {
            return null;
        }
    }

    /// Search for places using the Azure Maps fuzzy search API and return up to `limit`
    /// suggestions for a search-box dropdown. Each result includes a viewport so the
    /// caller can fit the camera to it, and a geometry ID for polygon lookups.
    async search(query, limit) {
        if (!query) return [];
        try {
            const key = this._options.subscriptionKey;
            const max = Math.max(1, Math.min(20, limit || 5));
            const url = `https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&subscription-key=${encodeURIComponent(key)}&query=${encodeURIComponent(query)}&limit=${max}`;
            const resp = await fetch(url);
            if (!resp.ok) return [];
            const data = await resp.json();
            const results = data?.results || [];
            return results.filter(r => r?.position).map(r => {
                const vp = r.viewport || r.boundingBox;
                const addr = r.address || {};
                const description = [addr.municipality, addr.countrySubdivision, addr.country]
                    .filter(Boolean)
                    .join(', ');
                return {
                    address: addr.freeformAddress || r.poi?.name || `${r.position.lat.toFixed(5)}, ${r.position.lon.toFixed(5)}`,
                    description: description || null,
                    latitude: r.position.lat,
                    longitude: r.position.lon,
                    north: vp?.topLeftPoint?.lat ?? r.position.lat,
                    south: vp?.btmRightPoint?.lat ?? r.position.lat,
                    east: vp?.btmRightPoint?.lon ?? r.position.lon,
                    west: vp?.topLeftPoint?.lon ?? r.position.lon,
                    geometryId: r.dataSources?.geometry?.id ?? null
                };
            });
        } catch {
            return [];
        }
    }

    /// Get the browser's current geolocation without showing any custom prompt.
    /// Returns { latitude, longitude } or null if unavailable / denied.
    async getBrowserLocation() {
        return new Promise((resolve) => {
            if (!navigator?.geolocation) { resolve(null); return; }
            navigator.geolocation.getCurrentPosition(
                (pos) => resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude }),
                () => resolve(null),
                { timeout: 10000, maximumAge: 60000 }
            );
        });
    }

    /// Reverse geocode a lat/lon pair using the Azure Maps Reverse Search API.
    /// Returns { countryCode, countrySubdivisionCode, countrySecondarySubdivision, municipality } or null.
    async reverseGeocode(latitude, longitude) {
        if (latitude == null || longitude == null) return null;
        try {
            const key = this._options.subscriptionKey;
            const url = `https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&subscription-key=${encodeURIComponent(key)}&query=${encodeURIComponent(latitude)},${encodeURIComponent(longitude)}&language=en-US`;
            const resp = await fetch(url);
            if (!resp.ok) return null;
            const data = await resp.json();
            const addr = data?.addresses?.[0]?.address;
            if (!addr) return null;
            return {
                countryCode: addr.countryCode ?? null,
                countrySubdivisionCode: addr.countrySubdivisionCode ?? null,
                countrySecondarySubdivision: addr.countrySecondarySubdivision ?? null,
                municipality: addr.municipality ?? null,
                municipalitySubdivision: addr.municipalitySubdivision ?? null,
                postalCode: addr.postalCode ?? null
            };
        } catch {
            return null;
        }
    }

    /// Fetch the actual boundary polygon for a geometry ID from Azure Maps Search Polygon API.
    /// Returns an array of coordinate rings (GeoJSON Polygon coordinates) or null.
    async getPolygon(geometryId) {
        if (!geometryId) return null;
        try {
            const key = this._options.subscriptionKey;
            const url = `https://atlas.microsoft.com/search/polygon/json?api-version=1.0&subscription-key=${encodeURIComponent(key)}&geometries=${encodeURIComponent(geometryId)}`;
            const resp = await fetch(url);
            if (!resp.ok) return null;
            const data = await resp.json();

            // Try to extract polygon coordinates from various response formats,
            // then decimate large rings — see MAX_LOCK_RING_VERTICES above.
            const coords = this._extractPolygonCoords(data);
            return coords ? this._simplifyRings(coords) : null;
        } catch {
            return null;
        }
    }

    /// Recursively extract polygon coordinates from any GeoJSON-like structure.
    _extractPolygonCoords(obj) {
        if (!obj || typeof obj !== 'object') return null;

        // Direct Polygon
        if (obj.type === 'Polygon' && obj.coordinates) return obj.coordinates;

        // MultiPolygon — pick the largest sub-polygon
        if (obj.type === 'MultiPolygon' && obj.coordinates) {
            let best = obj.coordinates[0];
            for (const poly of obj.coordinates) {
                if (poly[0]?.length > best[0]?.length) best = poly;
            }
            return best;
        }

        // FeatureCollection ? iterate features
        if (obj.type === 'FeatureCollection' && Array.isArray(obj.features)) {
            for (const f of obj.features) {
                const c = this._extractPolygonCoords(f.geometry || f);
                if (c) return c;
            }
        }

        // Feature ? unwrap geometry
        if (obj.type === 'Feature' && obj.geometry) {
            return this._extractPolygonCoords(obj.geometry);
        }

        // GeometryCollection ? iterate geometries
        if (obj.type === 'GeometryCollection' && Array.isArray(obj.geometries)) {
            for (const g of obj.geometries) {
                const c = this._extractPolygonCoords(g);
                if (c) return c;
            }
        }

        // Azure Maps additionalData wrapper
        if (Array.isArray(obj.additionalData)) {
            for (const ad of obj.additionalData) {
                const c = this._extractPolygonCoords(ad.geometryData || ad);
                if (c) return c;
            }
        }

        return null;
    }

    /// Uniformly decimates each ring to at most MAX_LOCK_RING_VERTICES points so
    /// downstream point-in-polygon checks (location lock) and rendering (region
    /// overlays) stay fast even for very detailed country/coastline boundaries.
    _simplifyRings(rings) {
        if (!Array.isArray(rings)) return rings;
        return rings.map(ring => this._simplifyRing(ring));
    }

    // Keeps every Nth point (N = ceil(length / max)) and re-closes the ring
    // (first point re-appended) per GeoJSON convention.
    _simplifyRing(ring) {
        if (!Array.isArray(ring) || ring.length <= MAX_LOCK_RING_VERTICES) return ring;

        const step = Math.ceil(ring.length / MAX_LOCK_RING_VERTICES);
        const simplified = [];
        for (let i = 0; i < ring.length; i += step) {
            simplified.push(ring[i]);
        }

        const first = ring[0];
        const last = simplified[simplified.length - 1];
        if (!last || last[0] !== first[0] || last[1] !== first[1]) {
            simplified.push(first);
        }

        return simplified;
    }

    /// Runtime sync for AddMarkerTrigger ('disabled' | 'single' | 'double' | 'center'),
    /// called from C# (AzureMap.razor.cs OnParametersSetAsync) whenever the parameter
    /// changes after the map was already created — e.g. a demo page letting the user
    /// switch between click-to-place and always-visible center-pin selection.
    setAddMarkerTrigger(trigger) {
        this._addTrigger = trigger || 'disabled';

        // Keep the scroll-zoom gate and native SDK scroll-zoom interaction aligned
        // with center-pin mode's "always anchor zoom to the map center" behavior.
        try {
            this._map.setUserInteraction({
                scrollZoomInteraction: this._addTrigger === 'center' ? false : this._interactions.scrollZoom
            });
        } catch { /* noop */ }

        // Re-emit immediately so switching into center-pin mode at runtime notifies
        // .NET of the current pin position without waiting for the next camera move.
        if (this._addTrigger === 'center') this._firePan?.();
    }

    setCenter(lat, lng, zoom) {
        const cam = { center: [lng, lat] };
        if (zoom != null) cam.zoom = zoom;
        this._map.setCamera(cam);
    }

    setStyle(style) {
        if (!style) return;
        try {
            // Push to the SDK unconditionally — Azure Maps is idempotent here
            // and this guarantees the map matches the requested value even
            // when the user changed it via the in-map StyleControl.
            this._map.setStyle({ style });
            this._options.style = style;
        } catch { /* noop */ }
    }

    setTraffic(flow, incidents) {
        this._trafficFlow = !!flow;
        this._trafficIncidents = !!incidents;
        this._applyTraffic();
    }

    /// Runtime sync for interaction flags (dragPan/dragRotate/scrollZoom/dblClickZoom/
    /// boxZoom/keyboard/touch), called from C# whenever an interaction parameter
    /// changes after the map was already created. Updates the cached snapshot used
    /// by the scroll-zoom gate and pushes the rest straight to the SDK.
    setInteractions(options) {
        if (!options) return;
        this._interactions = {
            dragPan: options.dragPan !== false,
            dragRotate: options.dragRotate !== false,
            scrollZoom: options.scrollZoom !== false,
            dblClickZoom: options.dblClickZoom !== false,
            boxZoom: options.boxZoom !== false,
            keyboard: options.keyboard !== false,
            touch: options.touch !== false
        };

        try {
            this._map.setUserInteraction({
                dragPanInteraction: this._interactions.dragPan,
                dragRotateInteraction: this._interactions.dragRotate,
                // Center-pin mode always self-manages scroll (see _setupScrollZoomGate);
                // pushing 'false' here avoids the SDK fighting our own wheel handler.
                scrollZoomInteraction: this._addTrigger === 'center' ? false : this._interactions.scrollZoom,
                dblClickZoomInteraction: this._interactions.dblClickZoom,
                boxZoomInteraction: this._interactions.boxZoom,
                keyboardInteraction: this._interactions.keyboard,
                touchInteraction: this._interactions.touch
            });
        } catch { /* setUserInteraction unavailable in current SDK */ }
    }

    setCameraOrientation(pitch, bearing) {
        const cam = {};
        if (pitch != null) cam.pitch = pitch;
        if (bearing != null) cam.bearing = bearing;
        if (Object.keys(cam).length) this._map.setCamera(cam);
    }

    setBounds(south, west, north, east, paddingPx) {
        try {
            this._map.setCamera({
                bounds: [west, south, east, north],
                padding: paddingPx ?? 40
            });
        } catch { /* noop */ }
    }

    showCurrentLocation(lat, lng) {
        this._setCurrentLocation(lat, lng);
        this._map.setCamera({ center: [lng, lat], zoom: 15 });
    }

    dispose() {
        this.clearMarkers();
        this.clearRegions();
        this.clearLocationLock();
        this._hideScrollHint();
        Object.keys(this._controlInstances).forEach(key => this._removeControl(key));
        try { this._map?.dispose(); } catch { /* noop */ }
        this._map = null;
        this._dotNetRef = null;
    }

    // -- Helpers ----------------------------------------------------------

    _buildPopupContent(info) {
        const esc = (s) => (s == null ? '' : this._escapeHtml(s));
        const title = info.title || info.label;

        const image = info.imageUrl
            ? `<div style="width:100%;height:120px;overflow:hidden;border-radius:8px 8px 0 0;">
                   <img src="${esc(info.imageUrl)}" alt="${esc(title)}"
                        style="width:100%;height:100%;object-fit:cover;" />
               </div>`
            : '';

        const detailRow = (icon, text) => text
            ? `<div style="display:flex;align-items:center;gap:6px;font-size:12px;color:#555;">
                   <span style="opacity:.55;">${icon}</span>
                   <span>${esc(text)}</span>
               </div>`
            : '';

        const action = info.detailsUrl
            ? `<a href="${esc(info.detailsUrl)}"
                  style="display:inline-block;margin-top:8px;padding:6px 14px;border-radius:6px;
                         background:#0078d4;color:#fff;text-decoration:none;font-weight:500;font-size:12px;">
                   ${esc(info.detailsLabel || 'Open')} ?
               </a>`
            : '';

        return `
            <div style="font-family:system-ui,-apple-system,'Segoe UI',sans-serif;min-width:220px;max-width:280px;">
                ${image}
                <div style="padding:10px 14px 12px;">
                    ${title ? `<div style="font-weight:600;margin-bottom:6px;color:#111;font-size:14px;word-break:break-word;">${esc(title)}</div>` : ''}
                    <div style="display:flex;flex-direction:column;gap:3px;">
                        ${detailRow('??', info.city)}
                        ${detailRow('??', info.area)}
                        ${detailRow('??', info.price)}
                    </div>
                    <div style="margin-top:6px;font-variant-numeric:tabular-nums;font-size:11px;color:#888;">
                        ${info.latitude.toFixed(5)}, ${info.longitude.toFixed(5)}
                    </div>
                    ${action}
                </div>
            </div>`;
    }

    _escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }
}
