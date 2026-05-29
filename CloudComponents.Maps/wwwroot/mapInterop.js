// ES module — loaded via Blazor JS isolation:
// import('./_content/CloudComponents.Maps/mapInterop.js')
// The Azure Maps Web SDK (atlas) is loaded on-demand by this module.

const ATLAS_VERSION = '3';
const ATLAS_CSS_URL = `https://atlas.microsoft.com/sdk/javascript/mapcontrol/${ATLAS_VERSION}/atlas.min.css`;
const ATLAS_JS_URL = `https://atlas.microsoft.com/sdk/javascript/mapcontrol/${ATLAS_VERSION}/atlas.min.js`;

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
        this._trafficFlow = !!options.showTrafficFlow;
        this._trafficIncidents = !!options.showTrafficIncidents;
        this._addTrigger = options.addMarkerTrigger || 'double';   // 'disabled' | 'single' | 'double'

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

    // ── Lifecycle ────────────────────────────────────────────────────────

    _onReady() {
        this._addControls();
        this._applyTraffic();

        // Maps clicks → notify .NET; optionally drop a pin on single-click.
        this._map.events.add('click', (e) => {
            if (!e?.position) return;
            const [lng, lat] = e.position;

            // Azure Maps fires the map-level 'click' for the same gesture that
            // hit a marker. The marker handler sets a one-shot flag so we
            // don't dismiss the popup we just opened.
            if (this._suppressNextMapClick) {
                this._suppressNextMapClick = false;
            } else if (!this._wasOnMarker(e)) {
                // Plain background click → dismiss any open marker popup.
                this._closeActivePopup();
            }

            this._dotNetRef?.invokeMethodAsync('NotifyMapClickAsync', lat, lng);

            if (this._addTrigger === 'single' && !this._wasOnMarker(e)) {
                this._dotNetRef?.invokeMethodAsync('NotifyMapAddMarkerAsync', lat, lng);
            }
        });

        // Native dblclick → drop a pin (when configured).
        this._map.events.add('dblclick', (e) => {
            if (this._addTrigger !== 'double') return;
            if (!e?.position) return;
            if (this._wasOnMarker(e)) return;
            const [lng, lat] = e.position;
            this._dotNetRef?.invokeMethodAsync('NotifyMapAddMarkerAsync', lat, lng);
        });

        // Center-pin mode → broadcast the camera-center coordinate as the map moves.
        if (this._addTrigger === 'center') {
            const fire = () => {
                const c = this._map.getCamera().center;   // [lng, lat]
                if (c) this._dotNetRef?.invokeMethodAsync('NotifyCenterPinChangedAsync', c[1], c[0]);
            };
            this._map.events.add('move', fire);
            this._map.events.add('moveend', fire);
            fire(); // emit initial position
        }

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

    _addControls() {
        const c = this._options.controls || {};
        const tryAdd = (key, factory) => {
            const cfg = c[key];
            if (!cfg || !cfg.enabled) return;
            try {
                this._map.controls.add(factory(), { position: cfg.position || 'top-right' });
            } catch { /* control unavailable in current SDK */ }
        };

        tryAdd('zoom', () => new atlas.control.ZoomControl());
        tryAdd('compass', () => new atlas.control.CompassControl());
        tryAdd('pitch', () => new atlas.control.PitchControl());
        // Force the full style list so 'satellite' and 'satellite_road_labels'
        // always appear, regardless of the account's default style set.
        tryAdd('style', () => new atlas.control.StyleControl({
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
        }));
        tryAdd('fullscreen', () => new atlas.control.FullscreenControl());
        tryAdd('scale', () => new atlas.control.ScaleControl());
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

    // ── Public API (called from C#) ──────────────────────────────────────

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

        // Single click → close any other open popup, open this one, notify .NET
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

        // Double-click on a marker → remove it (when enabled)
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

    // ── Circle overlay API ───────────────────────────────────────────────

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
            this._map.layers.add(new atlas.layer.SymbolLayer(this._regionDataSource, null, {
                textOptions: {
                    textField: ['get', 'label'],
                    offset: [0, 0],
                    size: 13,
                    color: '#333',
                    haloColor: '#fff',
                    haloWidth: 1.5
                },
                filter: ['has', 'label']
            }));
        }

        // Build polygon from GeoJSON coordinate rings
        const shape = new atlas.Shape(new atlas.data.Polygon(info.coordinates), info.id, {
            fillColor: info.fillColor || 'rgba(0, 120, 212, 0.15)',
            strokeColor: info.strokeColor || '#0078d4',
            strokeWidth: info.strokeWidth ?? 2,
            label: info.label || ''
        });
        this._regionDataSource.add(shape);
        this._regions.push(info.id);
    }

    clearRegions() {
        if (this._regionDataSource) {
            this._regionDataSource.clear();
        }
        this._regions = [];
    }

    /// Geocode a query string using the Azure Maps Search API.
    /// Returns { latitude, longitude, north, south, east, west, geometryId } or null.
    async geocode(query) {
        if (!query) return null;
        try {
            const key = this._options.subscriptionKey;
            const url = `https://atlas.microsoft.com/search/address/json?api-version=1.0&subscription-key=${encodeURIComponent(key)}&query=${encodeURIComponent(query)}&limit=1&entityType=Municipality,CountrySubdivision,CountrySecondarySubdivision,CountryTertiarySubdivision`;
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

            // Try to extract polygon coordinates from various response formats
            const coords = this._extractPolygonCoords(data);
            return coords;
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

        // FeatureCollection → iterate features
        if (obj.type === 'FeatureCollection' && Array.isArray(obj.features)) {
            for (const f of obj.features) {
                const c = this._extractPolygonCoords(f.geometry || f);
                if (c) return c;
            }
        }

        // Feature → unwrap geometry
        if (obj.type === 'Feature' && obj.geometry) {
            return this._extractPolygonCoords(obj.geometry);
        }

        // GeometryCollection → iterate geometries
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
        try { this._map?.dispose(); } catch { /* noop */ }
        this._map = null;
        this._dotNetRef = null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

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
                   ${esc(info.detailsLabel || 'Open')} →
               </a>`
            : '';

        return `
            <div style="font-family:system-ui,-apple-system,'Segoe UI',sans-serif;min-width:220px;max-width:280px;">
                ${image}
                <div style="padding:10px 14px 12px;">
                    ${title ? `<div style="font-weight:600;margin-bottom:6px;color:#111;font-size:14px;word-break:break-word;">${esc(title)}</div>` : ''}
                    <div style="display:flex;flex-direction:column;gap:3px;">
                        ${detailRow('📍', info.city)}
                        ${detailRow('📐', info.area)}
                        ${detailRow('💰', info.price)}
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
