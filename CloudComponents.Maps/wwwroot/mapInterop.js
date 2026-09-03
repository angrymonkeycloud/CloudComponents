// ES module � loaded via Blazor JS isolation:
// import('./_content/CloudComponents.Maps/mapInterop.js')
// The Azure Maps Web SDK (atlas) is loaded on-demand by this module.

const ATLAS_VERSION = '3';
const ATLAS_CSS_URL = `https://atlas.microsoft.com/sdk/javascript/mapcontrol/${ATLAS_VERSION}/atlas.min.css`;
const ATLAS_JS_URL = `https://atlas.microsoft.com/sdk/javascript/mapcontrol/${ATLAS_VERSION}/atlas.min.js`;

// Country-level boundaries from Azure Maps' polygon API can have tens of
// thousands of vertices per ring (e.g. detailed coastlines). Running a
// synchronous point-in-polygon ray-cast against that many points blocks the
// single UI thread shared by the browser and Blazor WebAssembly, which can
// make the entire page appear frozen � not just the map. Rings are
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
        this._timelineGroups = [];
        this._timelineMeta = new Map();
        this._timelinePopup = null;
        this._lockPolygons = null;
        this._lockBounds = null;
        this._lockDataSource = null;
        this._lastAllowedCenter = null;
        this._allowZoomCenterChange = false;
        this._intentionalCenterChangeTimer = null;
        this._centerPinMoveRequested = true;
        this._trafficFlow = !!options.showTrafficFlow;
        this._trafficIncidents = !!options.showTrafficIncidents;
        this._addTrigger = options.addMarkerTrigger || 'double';   // 'disabled' | 'single' | 'double'
        this._controlInstances = {};   // key -> { control, position } � lets setControls() diff/re-apply at runtime
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
                // Plain background click ? dismiss any open marker/timeline popup.
                this._closeActivePopup();
                this._closeTimelinePopup();
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
        // zoom-driven camera animation is still in flight � zooming is always
        // anchored to the map center (pin position) regardless of cursor location.
        // These listeners are always registered (not just when the map starts in
        // center-pin mode) so that switching AddMarkerTrigger to CenterPin at
        // runtime via setAddMarkerTrigger() works without recreating the map;
        // _firePan itself no-ops unless this._addTrigger is currently 'center'.
        let _isZooming = false;
        let _isZoomSettling = false;
        let _zoomSettleTimer = null;
        let _zoomAnchorCenter = null;
        const initialCenter = this._map.getCamera().center;
        this._lastAllowedCenter = initialCenter ? [initialCenter[0], initialCenter[1]] : null;

        this._firePan = () => {
            if (this._addTrigger !== 'center') return; // only active in center-pin mode
            if (!this._centerPinMoveRequested && !this._allowZoomCenterChange) return;
            if ((_isZooming || _isZoomSettling) && !this._allowZoomCenterChange) return;
            const c = this._map.getCamera().center;   // [lng, lat]
            if (!c) return;

            if (!this._isPointAllowed(c[0], c[1])) {
                // Only snap back when the last known-good center is itself inside the
                // locked area. If it is outside (e.g. the map loaded with the pin
                // outside the lock), do NOT snap � the user must be able to drag the
                // pin into the allowed area from its current position.
                const revertTo = this._lastAllowedCenter;
                if (revertTo && this._isPointAllowed(revertTo[0], revertTo[1])) {
                    this._map.setCamera({ center: revertTo, type: 'ease', duration: 200 });
                }
                this._dotNetRef?.invokeMethodAsync('NotifyLocationLockRejectedAsync', c[1], c[0]);
                this._centerPinMoveRequested = false;
                this._finishIntentionalCenterChange();
                return;
            }

            this._lastAllowedCenter = [c[0], c[1]];
            this._dotNetRef?.invokeMethodAsync('NotifyCenterPinChangedAsync', c[1], c[0]);
            this._centerPinMoveRequested = false;
            this._finishIntentionalCenterChange();
        };

        // A selected coordinate may change only after a real map pan. Zooming,
        // including an attempted zoom beyond the minimum or maximum, can still
        // raise moveend and must never be interpreted as a new pin location.
        this._map.events.add('dragstart', () => {
            if (this._addTrigger === 'center')
                this._centerPinMoveRequested = true;
        });

        // Preserve the exact selected coordinate through user zooms. Azure Maps
        // can slightly shift the camera center during zoom projection/rounding,
        // so restore the last reported pin coordinate when zooming finishes.
        // Programmatic search/restore/location moves opt out because they are
        // intentionally changing both the center and (sometimes) the zoom.
        this._map.events.add('zoomstart', () => {
            clearTimeout(_zoomSettleTimer);
            _isZoomSettling = false;
            _isZooming = true;
            _zoomAnchorCenter = this._addTrigger === 'center' && !this._allowZoomCenterChange
                && this._lastAllowedCenter
                ? [this._lastAllowedCenter[0], this._lastAllowedCenter[1]]
                : null;
        });
        this._map.events.add('zoomend', () => {
            _isZooming = false;
            _isZoomSettling = true;

            if (_zoomAnchorCenter && !this._allowZoomCenterChange) {
                this._map.setCamera({
                    center: [_zoomAnchorCenter[0], _zoomAnchorCenter[1]],
                    type: 'jump'
                });
            }
            _zoomAnchorCenter = null;

            clearTimeout(_zoomSettleTimer);
            _zoomSettleTimer = setTimeout(() => {
                _isZoomSettling = false;
            }, 50);
        });

        // 'moveend' fires after ANY settled camera change � drag, search
        // fly-to (setBounds/setCenter), "pin my location", etc. � so the
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
        //     zoom anyway � the common "hold a modifier to zoom" pattern � and
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
    // capture phase (BEFORE Azure Maps' own listener runs � see note on the
    // element hierarchy below):
    //   1) Center-pin mode always zooms anchored to the map center (not the
    //      cursor) so the fixed pin never visually drifts.
    //   2) When ScrollZoomInteraction is off, a bare wheel/trackpad gesture is
    //      left alone so the page can scroll past an embedded map � unless the
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
            this._closeTimelinePopup();
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

    // -- Tracking/history timelines ---------------------------------------
    //
    // Each timeline gets its own layer group so color, clustering and popups
    // never bleed between timelines. A group is composed of:
    //   * a route line plus direction arrows spaced along it, so the direction
    //     of travel is readable without playing anything back;
    //   * the raw GPS fixes in a CLUSTERED source — zoomed out they stack into
    //     numbered bubbles, zooming in (or clicking a cluster) breaks them
    //     apart until every individual fix is visible;
    //   * labeled "places", in their own unclustered source so a meaningful
    //     stop is never swallowed by a cluster. Their name labels are
    //     collision-managed by the SDK, so crowded areas stay readable;
    //   * start/end badges as HTML markers so both ends of the journey are
    //     unmistakable at any zoom.
    // Every one of those is clickable and opens a details popup.

    /// Replace all rendered timelines. `timelines` is an array of
    /// { id, name, color, showLine, points: [{ latitude, longitude, timestamp, label, description }] }.
    async setTimelines(timelines, fitToBounds) {
        this._disposeTimelineGroups();
        this._closeTimelinePopup();
        this._timelineMeta = new Map();

        let north = -90, south = 90, east = -180, west = 180, hasPoints = false;

        for (const tl of (timelines || [])) {
            const pts = tl.points || [];
            if (pts.length === 0) continue;

            this._timelineMeta.set(tl.id, tl);
            const color = tl.color || '#0078d4';
            const group = { id: tl.id, sources: [], layers: [], markers: [] };

            for (const p of pts) {
                hasPoints = true;
                if (p.latitude > north) north = p.latitude;
                if (p.latitude < south) south = p.latitude;
                if (p.longitude > east) east = p.longitude;
                if (p.longitude < west) west = p.longitude;
            }

            // ---- Route line + direction arrows ------------------------------
            if (tl.showLine !== false && pts.length > 1) {
                const lineSource = new atlas.source.DataSource();
                this._map.sources.add(lineSource);
                group.sources.push(lineSource);
                lineSource.add(new atlas.data.Feature(
                    new atlas.data.LineString(pts.map(p => [p.longitude, p.latitude])),
                    { timelineId: tl.id }));

                const lineLayer = new atlas.layer.LineLayer(lineSource, null, {
                    strokeColor: color,
                    strokeWidth: ['interpolate', ['linear'], ['zoom'], 5, 2.5, 15, 5],
                    strokeOpacity: 0.75,
                    lineCap: 'round',
                    lineJoin: 'round'
                });
                this._map.layers.add(lineLayer);
                group.layers.push(lineLayer);

                const arrowLayer = await this._createDirectionArrowLayer(lineSource, color);
                if (arrowLayer) {
                    this._map.layers.add(arrowLayer);
                    group.layers.push(arrowLayer);
                }
            }

            // ---- Split the points: endpoints / labeled places / raw fixes ----
            const trailFeatures = [];
            const placeFeatures = [];
            pts.forEach((p, i) => {
                if (i === 0 || i === pts.length - 1) return; // endpoints are HTML markers
                const feature = new atlas.data.Feature(
                    new atlas.data.Point([p.longitude, p.latitude]),
                    { timelineId: tl.id, index: i, label: p.label || '' });
                (p.label ? placeFeatures : trailFeatures).push(feature);
            });

            // ---- Raw GPS fixes: clustered ------------------------------------
            if (trailFeatures.length > 0) {
                const trailSource = new atlas.source.DataSource(null, {
                    cluster: true,
                    clusterRadius: 42,
                    // Past this zoom every fix is drawn individually — the
                    // "fully zoomed in = full detail" end of the scale.
                    clusterMaxZoom: 17
                });
                this._map.sources.add(trailSource);
                group.sources.push(trailSource);
                trailSource.add(trailFeatures);

                const clusterLayer = new atlas.layer.BubbleLayer(trailSource, null, {
                    filter: ['has', 'point_count'],
                    radius: ['step', ['get', 'point_count'], 11, 10, 14, 50, 18, 200, 22],
                    color: color,
                    opacity: 0.9,
                    strokeColor: '#ffffff',
                    strokeWidth: 2
                });
                const clusterCountLayer = new atlas.layer.SymbolLayer(trailSource, null, {
                    filter: ['has', 'point_count'],
                    iconOptions: { image: 'none' },
                    textOptions: {
                        textField: ['get', 'point_count_abbreviated'],
                        color: '#ffffff',
                        size: 11,
                        offset: [0, 0.1],
                        allowOverlap: true,
                        ignorePlacement: true
                    }
                });
                const trailLayer = new atlas.layer.BubbleLayer(trailSource, null, {
                    filter: ['!', ['has', 'point_count']],
                    radius: ['interpolate', ['linear'], ['zoom'], 8, 3, 16, 5.5],
                    color: color,
                    opacity: 0.95,
                    strokeColor: '#ffffff',
                    strokeWidth: 1.5
                });

                this._map.layers.add([clusterLayer, clusterCountLayer, trailLayer]);
                group.layers.push(clusterLayer, clusterCountLayer, trailLayer);

                this._bindTimelineClusterExpand(clusterLayer, trailSource);
                this._bindTimelinePointClick(trailLayer);
            }

            // ---- Labeled places: always visible ------------------------------
            if (placeFeatures.length > 0) {
                const placeSource = new atlas.source.DataSource();
                this._map.sources.add(placeSource);
                group.sources.push(placeSource);
                placeSource.add(placeFeatures);

                const placeLayer = new atlas.layer.BubbleLayer(placeSource, null, {
                    radius: ['interpolate', ['linear'], ['zoom'], 8, 6.5, 16, 9],
                    color: color,
                    strokeColor: '#ffffff',
                    strokeWidth: 3
                });
                const placeLabelLayer = new atlas.layer.SymbolLayer(placeSource, null, {
                    iconOptions: { image: 'none' },
                    textOptions: {
                        textField: ['get', 'label'],
                        offset: [0, -1.4],
                        size: 12,
                        color: '#1b1b1b',
                        haloColor: '#ffffff',
                        haloWidth: 2
                    }
                });

                this._map.layers.add([placeLayer, placeLabelLayer]);
                group.layers.push(placeLayer, placeLabelLayer);
                this._bindTimelinePointClick(placeLayer);
            }

            // ---- Start / end badges ------------------------------------------
            group.markers.push(this._addTimelineEndpointMarker(tl, 0, 'start'));
            if (pts.length > 1)
                group.markers.push(this._addTimelineEndpointMarker(tl, pts.length - 1, 'end'));

            this._timelineGroups.push(group);
        }

        if (fitToBounds && hasPoints) {
            // Pad degenerate bounds (single point / stationary trace) so the
            // camera doesn't zoom to max.
            if (north - south < 0.002) { north += 0.001; south -= 0.001; }
            if (east - west < 0.002) { east += 0.001; west -= 0.001; }
            this._beginIntentionalCenterChange();
            this._map.setCamera({
                bounds: [west, south, east, north],
                padding: 70,
                type: 'ease',
                duration: 700
            });
        }
    }

    clearTimelines() {
        this._disposeTimelineGroups();
        this._timelineMeta = new Map();
        this._closeTimelinePopup();
    }

    _disposeTimelineGroups() {
        for (const group of (this._timelineGroups || [])) {
            for (const marker of group.markers) {
                try { this._map.markers.remove(marker); } catch { /* noop */ }
            }
            for (const layer of group.layers) {
                try { this._map.layers.remove(layer); } catch { /* noop */ }
            }
            for (const source of group.sources) {
                try { this._map.sources.remove(source); } catch { /* noop */ }
            }
        }
        this._timelineGroups = [];
    }

    /// Arrow glyphs repeated along the route so the direction of travel is
    /// obvious at a glance. Uses the SDK's scalable 'triangle-arrow-up' image
    /// template rotated 90° — with `placement: 'line'` the icon's own "up"
    /// axis maps onto the line's heading. Returns null when the template isn't
    /// available so the route still renders without arrows.
    async _createDirectionArrowLayer(lineSource, color) {
        const iconId = `cc-tl-arrow-${color.replace(/[^a-z0-9]/gi, '')}`;
        try {
            if (!this._map.imageSprite.hasImage(iconId))
                await this._map.imageSprite.createFromTemplate(iconId, 'triangle-arrow-up', color, '#ffffff');

            return new atlas.layer.SymbolLayer(lineSource, null, {
                iconOptions: {
                    image: iconId,
                    allowOverlap: true,
                    ignorePlacement: true,
                    anchor: 'center',
                    rotation: 90,
                    size: 0.75
                },
                lineSpacing: 90,
                placement: 'line'
            });
        } catch {
            return null;
        }
    }

    /// Start/end are HTML markers rather than layer features: there are only two
    /// per timeline and they must stay legible and unclustered at every zoom.
    _addTimelineEndpointMarker(tl, index, kind) {
        const p = tl.points[index];
        const marker = new atlas.HtmlMarker({
            position: [p.longitude, p.latitude],
            anchor: 'top',
            pixelOffset: [0, -15],
            htmlContent: this._buildTimelineEndpointHtml(kind, p)
        });
        this._map.markers.add(marker);

        this._map.events.add('click', marker, () => {
            this._suppressNextMapClick = true;
            this._openTimelinePopup(tl.id, index);
            this._dotNetRef?.invokeMethodAsync('NotifyTimelinePointClickAsync', tl.id, index);
        });

        return marker;
    }

    _buildTimelineEndpointHtml(kind, p) {
        const isStart = kind === 'start';
        const bg = isStart ? '#107c10' : '#d13438';
        const glyph = isStart ? '&#9654;' : '&#9632;';           // ▶ / ■
        const caption = (isStart ? 'Start' : 'End')
            + (p.label ? ` · ${this._escapeHtml(p.label)}` : '')
            + (this._formatClockTime(p.timestamp) ? ` · ${this._formatClockTime(p.timestamp)}` : '');

        return `<div style="display:flex;flex-direction:column;align-items:center;cursor:pointer;">
                    <div style="width:28px;height:28px;border-radius:50%;background:${bg};
                                border:3px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.35);
                                display:flex;align-items:center;justify-content:center;
                                color:#fff;font-size:11px;line-height:1;">${glyph}</div>
                    <div style="margin-top:4px;padding:2px 8px;border-radius:10px;background:${bg};color:#fff;
                                font:600 10.5px/1.5 system-ui,-apple-system,'Segoe UI',sans-serif;
                                white-space:nowrap;box-shadow:0 1px 3px rgba(0,0,0,.3);">${caption}</div>
                </div>`;
    }

    /// Clicking a stack of points zooms to exactly the level where that cluster
    /// breaks apart — the "tap to see what's inside" gesture users expect.
    _bindTimelineClusterExpand(layer, source) {
        this._map.events.add('click', layer, (e) => {
            const feature = e?.shapes?.[0];
            if (!feature) return;
            const props = typeof feature.getProperties === 'function' ? feature.getProperties() : feature.properties;
            const coords = typeof feature.getCoordinates === 'function'
                ? feature.getCoordinates()
                : feature.geometry?.coordinates;
            if (!props || props.cluster_id == null || !coords) return;

            this._suppressNextMapClick = true;
            source.getClusterExpansionZoom(props.cluster_id)
                .then(zoom => {
                    this._beginIntentionalCenterChange();
                    this._map.setCamera({ center: coords, zoom, type: 'ease', duration: 450 });
                })
                .catch(() => { /* noop */ });
        });
        this._bindTimelinePointerCursor(layer);
    }

    _bindTimelinePointClick(layer) {
        this._map.events.add('click', layer, (e) => {
            const feature = e?.shapes?.[0];
            if (!feature) return;
            const props = typeof feature.getProperties === 'function' ? feature.getProperties() : feature.properties;
            if (!props || props.timelineId == null) return;

            this._suppressNextMapClick = true;
            this._openTimelinePopup(props.timelineId, props.index);
            this._dotNetRef?.invokeMethodAsync('NotifyTimelinePointClickAsync', props.timelineId, props.index);
        });
        this._bindTimelinePointerCursor(layer);
    }

    _bindTimelinePointerCursor(layer) {
        this._map.events.add('mouseenter', layer, () => {
            this._map.getCanvasContainer().style.cursor = 'pointer';
        });
        this._map.events.add('mouseleave', layer, () => {
            this._map.getCanvasContainer().style.cursor = '';
        });
    }

    _haversineMeters(lat1, lng1, lat2, lng2) {
        const R = 6371000;
        const toRad = d => d * Math.PI / 180;
        const dLat = toRad(lat2 - lat1);
        const dLng = toRad(lng2 - lng1);
        const a = Math.sin(dLat / 2) ** 2 +
            Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLng / 2) ** 2;
        return R * 2 * Math.asin(Math.sqrt(a));
    }

    _openTimelinePopup(timelineId, index) {
        const tl = this._timelineMeta?.get(timelineId);
        const p = tl?.points?.[index];
        if (!p) return;

        if (!this._timelinePopup)
            this._timelinePopup = new atlas.Popup({ pixelOffset: [0, -16], closeButton: true });

        this._closeActivePopup(); // marker popups and timeline popups are mutually exclusive
        this._timelinePopup.setOptions({
            position: [p.longitude, p.latitude],
            content: this._buildTimelinePopupContent(tl, p, index)
        });
        this._timelinePopup.open(this._map);
    }

    _closeTimelinePopup() {
        if (this._timelinePopup) { try { this._timelinePopup.close(); } catch { /* noop */ } }
    }

    _parseDate(value) {
        if (!value) return null;
        const d = new Date(value);
        return isNaN(d) ? null : d;
    }

    _formatClockTime(value) {
        const d = this._parseDate(value);
        return d ? d.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }) : '';
    }

    _formatDuration(minutes) {
        if (!isFinite(minutes) || minutes < 1) return '';
        const h = Math.floor(minutes / 60);
        const m = Math.round(minutes % 60);
        if (h === 0) return `${m} min`;
        return m === 0 ? `${h} h` : `${h} h ${m} min`;
    }

    _formatDistance(meters) {
        return meters >= 1000 ? `${(meters / 1000).toFixed(2)} km` : `${Math.round(meters)} m`;
    }

    _buildTimelinePopupContent(tl, p, index) {
        const esc = (s) => (s == null ? '' : this._escapeHtml(s));
        const pts = tl.points;
        const isStart = index === 0;
        const isEnd = index === pts.length - 1;
        const accent = isStart ? '#107c10' : isEnd ? '#d13438' : (tl.color || '#0078d4');
        const kindText = isStart ? 'Start' : isEnd ? 'End' : (p.label ? 'Place' : 'GPS point');

        const when = this._parseDate(p.timestamp);
        const dateText = when ? when.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' }) : '';
        const timeText = when ? when.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }) : '';

        // How long the journey paused here — the gap to the next recorded point.
        let stayText = '';
        if (p.label && index < pts.length - 1 && when) {
            const next = this._parseDate(pts[index + 1].timestamp);
            if (next) stayText = this._formatDuration((next - when) / 60000);
        }

        // Leg into this point: distance covered and average speed.
        let legText = '', speedText = '';
        if (index > 0) {
            const prev = pts[index - 1];
            const meters = this._haversineMeters(prev.latitude, prev.longitude, p.latitude, p.longitude);
            legText = `${this._formatDistance(meters)} from previous point`;
            const prevWhen = this._parseDate(prev.timestamp);
            if (when && prevWhen) {
                const seconds = (when - prevWhen) / 1000;
                if (seconds > 0) speedText = `${(meters / seconds * 3.6).toFixed(1)} km/h average`;
            }
        }

        let elapsedText = '';
        if (index > 0 && when) {
            const startWhen = this._parseDate(pts[0].timestamp);
            const elapsed = startWhen ? this._formatDuration((when - startWhen) / 60000) : '';
            if (elapsed) elapsedText = `${elapsed} into the timeline`;
        }

        const row = (icon, text) => text
            ? `<div style="display:flex;align-items:flex-start;gap:7px;font-size:12px;color:#555;line-height:1.45;">
                   <span style="opacity:.5;flex:none;">${icon}</span><span>${esc(text)}</span>
               </div>`
            : '';

        const title = p.label || tl.name || kindText;

        return `
            <div style="font-family:system-ui,-apple-system,'Segoe UI',sans-serif;min-width:225px;max-width:280px;">
                <div style="height:4px;background:${accent};border-radius:6px 6px 0 0;"></div>
                <div style="padding:10px 14px 12px;">
                    <div style="display:flex;align-items:center;gap:8px;">
                        <span style="flex:none;padding:2px 7px;border-radius:5px;background:${accent};color:#fff;
                                     font-size:10px;font-weight:700;letter-spacing:.03em;text-transform:uppercase;">${kindText}</span>
                        ${dateText ? `<span style="font-size:11px;color:#888;">${esc(dateText)}</span>` : ''}
                    </div>
                    <div style="font-weight:600;color:#111;font-size:14.5px;margin-top:7px;word-break:break-word;">${esc(title)}</div>
                    ${p.label && tl.name ? `<div style="font-size:11.5px;color:#888;margin-top:1px;">${esc(tl.name)}</div>` : ''}
                    <div style="display:flex;flex-direction:column;gap:4px;margin-top:9px;">
                        ${row('&#128337;', timeText)}
                        ${row('&#9203;', stayText ? `Stayed ${stayText}` : '')}
                        ${row('&#128172;', p.description)}
                        ${row('&#128207;', legText)}
                        ${row('&#128663;', speedText)}
                        ${row('&#9201;', elapsedText)}
                    </div>
                    <div style="margin-top:9px;padding-top:8px;border-top:1px solid #eee;
                                font-variant-numeric:tabular-nums;font-size:11px;color:#999;">
                        Point ${index + 1} of ${pts.length} &middot; ${p.latitude.toFixed(5)}, ${p.longitude.toFixed(5)}
                    </div>
                </div>
            </div>`;
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
    /// caller can fit the camera to it, and a geometry Id for polygon lookups.
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

    /// Search only Azure Maps geography municipality records inside one country.
    /// This is intentionally paged: bulk geography imports can continue with `offset`
    /// until every result reported by Azure has been collected.
    async searchMunicipalities(query, countryCode, limit, offset, useTypeahead) {
        if (!query || !countryCode) {
            console.log("Azure Maps: missing query or countryCode", {
                query,
                countryCode
            });

            return {
                totalResults: 0,
                results: []
            };
        }

        try {
            const key = this._options.subscriptionKey;

            const pageSize = Math.max(
                1,
                Math.min(100, limit || 100)
            );

            const pageOffset = Math.max(0, offset || 0);

            const typeahead = useTypeahead
                ? "&typeahead=true"
                : "";

            const url =
                `https://atlas.microsoft.com/search/fuzzy/json` +
                `?api-version=1.0` +
                `&subscription-key=${encodeURIComponent(key)}` +
                `&query=${encodeURIComponent(query)}` +
                `&idxSet=Geo` +
                `&entityType=Municipality` +
                `&countrySet=${encodeURIComponent(countryCode)}` +
                `&limit=${pageSize}` +
                `&ofs=${pageOffset}` +
                `&language=en-US` +
                typeahead;

            const resp = await fetch(url);

            if (!resp.ok) {
                return {
                    totalResults: 0,
                    results: []
                };
            }

            const data = await resp.json();

            const results = (data?.results || [])
                .map(result => {
                    const address = result?.address || {};

                    return {
                        countryCode:
                            address.countryCode ?? null,

                        countrySubdivisionCode:
                            address.countrySubdivisionCode ?? null,

                        countrySecondarySubdivision:
                            address.countrySecondarySubdivision ?? null,

                        localName:
                            address.localName ?? null,

                        municipality:
                            address.municipality ?? null,

                        municipalitySubdivision:
                            address.municipalitySubdivision ?? null,

                        freeformAddress:
                            address.freeformAddress ?? null
                    };
                })
                .filter(result =>
                    result.localName ||
                    result.municipality ||
                    result.municipalitySubdivision
                );

            return {
                totalResults:
                    data?.summary?.totalResults ??
                    results.length,

                results
            };
        }
        catch (error) {
            console.error(
                "Azure Maps municipality search exception:",
                error
            );

            return {
                totalResults: 0,
                results: []
            };
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
                postalCode: addr.postalCode ?? null,
                localName: addr.localName ?? null,
                freeformAddress: addr.freeformAddress ?? null
            };
        } catch {
            return null;
        }
    }

    /// Fetch the actual boundary polygon for a geometry Id from Azure Maps Search Polygon API.
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
            // then decimate large rings � see MAX_LOCK_RING_VERTICES above.
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

        // MultiPolygon � pick the largest sub-polygon
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
    /// changes after the map was already created � e.g. a demo page letting the user
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
        if (this._addTrigger === 'center') {
            this._centerPinMoveRequested = true;
            this._firePan?.();
        }
    }

    setCenter(lat, lng, zoom) {
        this._beginIntentionalCenterChange();
        const cam = { center: [lng, lat] };
        if (zoom != null) cam.zoom = zoom;
        this._map.setCamera(cam);
    }

    setStyle(style) {
        if (!style) return;
        try {
            // Push to the SDK unconditionally � Azure Maps is idempotent here
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
            this._beginIntentionalCenterChange();
            this._map.setCamera({
                bounds: [west, south, east, north],
                padding: paddingPx ?? 40
            });
        } catch { /* noop */ }
    }

    showCurrentLocation(lat, lng) {
        this._setCurrentLocation(lat, lng);
        this._beginIntentionalCenterChange();
        this._map.setCamera({ center: [lng, lat], zoom: 15 });
    }

    _beginIntentionalCenterChange() {
        this._allowZoomCenterChange = true;
        clearTimeout(this._intentionalCenterChangeTimer);
        this._intentionalCenterChangeTimer = setTimeout(() => {
            this._allowZoomCenterChange = false;
        }, 5000);
    }

    _finishIntentionalCenterChange() {
        this._allowZoomCenterChange = false;
        clearTimeout(this._intentionalCenterChangeTimer);
        this._intentionalCenterChangeTimer = null;
    }

    dispose() {
        clearTimeout(this._intentionalCenterChangeTimer);
        this.clearMarkers();
        this.clearRegions();
        this.clearTimelines();
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
