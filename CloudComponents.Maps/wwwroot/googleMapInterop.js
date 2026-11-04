let googleLoader;

function loadGoogleMaps(apiKey, language) {
    if (globalThis.google?.maps)
        return Promise.resolve(globalThis.google.maps);

    if (googleLoader)
        return googleLoader;

    googleLoader = new Promise((resolve, reject) => {
        const callbackName = `cloudGoogleMapsReady_${Date.now()}`;
        globalThis[callbackName] = () => {
            delete globalThis[callbackName];
            resolve(globalThis.google.maps);
        };

        const script = document.createElement("script");
        const query = new URLSearchParams({
            key: apiKey,
            v: "weekly",
            libraries: "marker,places,geometry",
            callback: callbackName,
            loading: "async"
        });

        if (language)
            query.set("language", language.split("-")[0]);

        script.src = `https://maps.googleapis.com/maps/api/js?${query}`;
        script.async = true;
        script.onerror = () => reject(new Error("Google Maps JavaScript API could not be loaded."));
        document.head.appendChild(script);
    });

    return googleLoader;
}

function escapeHtml(value) {
    const div = document.createElement("div");
    div.textContent = value ?? "";
    return div.innerHTML;
}

function ensureGoogleMapStyles() {
    if (document.getElementById("cloud-components-google-map-styles"))
        return;

    const style = document.createElement("style");
    style.id = "cloud-components-google-map-styles";
    style.textContent = `
        .cloud-google-map-marker { position: relative; display: flex; flex-direction: column; align-items: center; cursor: pointer; transform: translateY(-17px); }
        .cloud-google-map-marker-pin { display: block; width: 24px; height: 24px; border: 3px solid #fff; border-radius: 50% 50% 50% 0; background: var(--marker-color); box-shadow: 0 2px 6px rgba(0,0,0,.35); transform: rotate(-45deg); }
        .cloud-google-map-marker-pin::after { content: ''; position: absolute; width: 7px; height: 7px; left: 5.5px; top: 5.5px; border-radius: 50%; background: #fff; }
        .cloud-google-map-marker-label, .cloud-google-map-marker-title { display: none; max-width: 190px; margin-bottom: 5px; padding: 3px 7px; overflow: hidden; border-radius: 6px; background: rgba(255,255,255,.96); color: #172033; box-shadow: 0 1px 5px rgba(0,0,0,.24); font: 600 12px/1.25 system-ui,sans-serif; text-overflow: ellipsis; white-space: nowrap; }
        .cloud-google-map-marker[data-show-label='true']:not(._detail) .cloud-google-map-marker-label { display: block; }
        .cloud-google-map-marker[data-show-title='true']._detail .cloud-google-map-marker-title { display: block; }
        .cloud-google-map-popup { display: grid; gap: 5px; min-width: 180px; max-width: 280px; color: #263449; font: 14px/1.4 system-ui,sans-serif; }
        .cloud-google-map-popup strong { font-size: 15px; }
        .cloud-google-map-popup-image { width: 100%; max-height: 140px; object-fit: cover; border-radius: 8px; }
        .cloud-google-map-popup-link { justify-self: start; margin-top: 4px; padding: 7px 11px; border-radius: 7px; background: #0b57d0; color: #fff; text-decoration: none; }
    `;
    document.head.appendChild(style);
}

function colorWithOpacity(color, fallbackOpacity) {
    if (!color)
        return { color: "#0078d4", opacity: fallbackOpacity };

    const match = color.match(/^rgba?\(([^)]+)\)$/i);
    if (!match)
        return { color, opacity: fallbackOpacity };

    const parts = match[1].split(",").map(part => part.trim());
    return {
        color: `rgb(${parts[0]}, ${parts[1]}, ${parts[2]})`,
        opacity: parts.length > 3 ? Number(parts[3]) : fallbackOpacity
    };
}

function toLatLng(point) {
    if (Array.isArray(point))
        return { lat: Number(point[1]), lng: Number(point[0]) };

    return {
        lat: Number(point.latitude ?? point.lat),
        lng: Number(point.longitude ?? point.lng)
    };
}

function toPath(ring) {
    return (ring ?? []).map(toLatLng).filter(point => Number.isFinite(point.lat) && Number.isFinite(point.lng));
}

function getViewport(result) {
    const viewport = result?.geometry?.viewport;
    if (!viewport)
        return null;

    const northEast = viewport.getNorthEast();
    const southWest = viewport.getSouthWest();
    return {
        north: northEast.lat(),
        east: northEast.lng(),
        south: southWest.lat(),
        west: southWest.lng()
    };
}

function viewportPolygon(viewport) {
    if (!viewport)
        return null;

    return [[
        [viewport.west, viewport.south],
        [viewport.east, viewport.south],
        [viewport.east, viewport.north],
        [viewport.west, viewport.north],
        [viewport.west, viewport.south]
    ]];
}

function googleMapType(style) {
    switch (style) {
        case "satellite": return "satellite";
        case "satellite_road_labels": return "hybrid";
        default: return "roadmap";
    }
}

function googleStyles(style) {
    if (style === "grayscale_light")
        return [{ stylers: [{ saturation: -100 }, { lightness: 15 }] }];
    if (style === "grayscale_dark" || style === "night" || style === "high_contrast_dark")
        return [
            { elementType: "geometry", stylers: [{ color: "#172033" }] },
            { elementType: "labels.text.fill", stylers: [{ color: "#d8e0ed" }] },
            { elementType: "labels.text.stroke", stylers: [{ color: "#172033" }] },
            { featureType: "water", elementType: "geometry", stylers: [{ color: "#0b1322" }] }
        ];
    if (style === "high_contrast_light")
        return [{ stylers: [{ saturation: -40 }, { contrast: 35 }] }];
    return null;
}

function controlPosition(position) {
    const controls = globalThis.google.maps.ControlPosition;
    switch (position) {
        case "top-left": return controls.TOP_LEFT;
        case "bottom-left": return controls.BOTTOM_LEFT;
        case "bottom-right": return controls.BOTTOM_RIGHT;
        default: return controls.TOP_RIGHT;
    }
}

function addressComponent(result, ...types) {
    for (const type of types) {
        const component = result?.address_components?.find(item => item.types.includes(type));
        if (component)
            return component;
    }
    return null;
}

function reverseResult(result) {
    const country = addressComponent(result, "country");
    const state = addressComponent(result, "administrative_area_level_1");
    const county = addressComponent(result, "administrative_area_level_2");
    const city = addressComponent(result, "locality", "postal_town", "administrative_area_level_3");
    const district = addressComponent(result, "sublocality", "sublocality_level_1", "neighborhood");
    const postalCode = addressComponent(result, "postal_code");

    return {
        countryCode: country?.short_name ?? null,
        countrySubdivisionCode: state?.short_name ?? null,
        countrySecondarySubdivision: county?.long_name ?? null,
        municipality: city?.long_name ?? null,
        municipalitySubdivision: district?.long_name ?? null,
        postalCode: postalCode?.long_name ?? null,
        localName: city?.long_name ?? district?.long_name ?? null,
        freeformAddress: result?.formatted_address ?? null
    };
}

function markerPopupHtml(marker) {
    const title = marker.title || marker.label;
    const address = [marker.district, marker.city, marker.subdivision, marker.country].filter(Boolean).join(", ");
    const meta = [marker.area, marker.price].filter(Boolean).join(" · ");
    const image = marker.imageUrl ? `<img class="cloud-google-map-popup-image" src="${escapeHtml(marker.imageUrl)}" alt="" />` : "";
    const button = marker.detailsUrl
        ? `<a class="cloud-google-map-popup-link" href="${escapeHtml(marker.detailsUrl)}">${escapeHtml(marker.detailsLabel || "View Property")}</a>`
        : "";

    return `<div class="cloud-google-map-popup">${image}${title ? `<strong>${escapeHtml(title)}</strong>` : ""}${marker.subtitle ? `<span>${escapeHtml(marker.subtitle)}</span>` : ""}${address ? `<span>${escapeHtml(address)}</span>` : ""}${meta ? `<span>${escapeHtml(meta)}</span>` : ""}${button}</div>`;
}

function makeMarkerContent(marker, options) {
    const root = document.createElement("div");
    root.className = "cloud-google-map-marker";
    root.dataset.markerId = marker.id;

    const label = document.createElement("span");
    label.className = "cloud-google-map-marker-label";
    label.textContent = marker.label || "";

    const title = document.createElement("span");
    title.className = "cloud-google-map-marker-title";
    title.textContent = marker.title || marker.label || "";

    const pin = document.createElement("span");
    pin.className = "cloud-google-map-marker-pin";
    pin.style.setProperty("--marker-color", marker.color || "#e81123");

    root.append(label, title, pin);
    root.dataset.showLabel = options.showMarkerLabelsBeforeTitleZoom ? "true" : "false";
    root.dataset.showTitle = options.showMarkerTitlesOnZoom ? "true" : "false";
    return root;
}

function updateMarkerLabels(controller) {
    const detailed = (controller.map.getZoom() ?? 0) >= controller.options.markerTitleMinZoom;
    for (const entry of controller.markers.values()) {
        const content = entry.content;
        if (!content)
            continue;
        content.classList.toggle("_detail", detailed);
    }
}

function pointInRing(latitude, longitude, ring) {
    let inside = false;
    for (let index = 0, previous = ring.length - 1; index < ring.length; previous = index++) {
        const currentPoint = ring[index];
        const previousPoint = ring[previous];
        const currentLongitude = Number(currentPoint[0]);
        const currentLatitude = Number(currentPoint[1]);
        const previousLongitude = Number(previousPoint[0]);
        const previousLatitude = Number(previousPoint[1]);
        const intersects = ((currentLatitude > latitude) !== (previousLatitude > latitude))
            && (longitude < (previousLongitude - currentLongitude) * (latitude - currentLatitude)
                / ((previousLatitude - currentLatitude) || Number.EPSILON) + currentLongitude);
        if (intersects)
            inside = !inside;
    }
    return inside;
}

function pointInPolygons(latitude, longitude, polygons) {
    return (polygons ?? []).some(polygon => {
        const outer = polygon?.[0];
        if (!outer || !pointInRing(latitude, longitude, outer))
            return false;
        return !polygon.slice(1).some(hole => pointInRing(latitude, longitude, hole));
    });
}

function createController(dotNet, map, options) {
    return {
        dotNet,
        map,
        options,
        markers: new Map(),
        regions: new Map(),
        routeLines: [],
        timelineItems: [],
        locationLockPolygons: [],
        locationLockOverlays: [],
        listeners: [],
        infoWindow: new google.maps.InfoWindow(),
        trafficLayer: null,
        currentLocation: null,
        disposed: false,
        longPressTimer: null
    };
}

function addListener(controller, target, eventName, callback) {
    const listener = target.addListener(eventName, callback);
    controller.listeners.push(listener);
    return listener;
}

function attachMapEvents(controller) {
    let lastClick = 0;
    addListener(controller, controller.map, "click", event => {
        if (!event.latLng)
            return;
        const latitude = event.latLng.lat();
        const longitude = event.latLng.lng();
        controller.dotNet.invokeMethodAsync("NotifyMapClickAsync", latitude, longitude);
        if (controller.options.addMarkerTrigger === "single")
            tryAddMarker(controller, latitude, longitude);
        lastClick = Date.now();
    });

    addListener(controller, controller.map, "dblclick", event => {
        if (!event.latLng)
            return;
        const latitude = event.latLng.lat();
        const longitude = event.latLng.lng();
        controller.dotNet.invokeMethodAsync("NotifyMapDoubleClickAsync", latitude, longitude);
        if (controller.options.addMarkerTrigger === "double")
            tryAddMarker(controller, latitude, longitude);
    });

    addListener(controller, controller.map, "idle", () => {
        updateMarkerLabels(controller);
        if (controller.options.addMarkerTrigger !== "center")
            return;
        const center = controller.map.getCenter();
        if (center)
            controller.dotNet.invokeMethodAsync("NotifyCenterPinChangedAsync", center.lat(), center.lng());
    });

    const element = controller.map.getDiv();
    const beginLongPress = event => {
        if (!event.touches || event.touches.length !== 1)
            return;
        const touch = event.touches[0];
        controller.longPressTimer = setTimeout(() => {
            const rect = element.getBoundingClientRect();
            const projection = controller.map.getProjection();
            if (!projection)
                return;
            const bounds = controller.map.getBounds();
            if (!bounds)
                return;
            const northEast = projection.fromLatLngToPoint(bounds.getNorthEast());
            const southWest = projection.fromLatLngToPoint(bounds.getSouthWest());
            const scale = 2 ** (controller.map.getZoom() ?? 0);
            const worldPoint = new google.maps.Point(
                southWest.x + ((touch.clientX - rect.left) / rect.width) * (northEast.x - southWest.x),
                northEast.y + ((touch.clientY - rect.top) / rect.height) * (southWest.y - northEast.y));
            const latLng = projection.fromPointToLatLng(worldPoint, true);
            controller.dotNet.invokeMethodAsync("NotifyMapLongPressAsync", latLng.lat(), latLng.lng());
        }, 550);
    };
    const cancelLongPress = () => {
        clearTimeout(controller.longPressTimer);
        controller.longPressTimer = null;
    };
    element.addEventListener("touchstart", beginLongPress, { passive: true });
    element.addEventListener("touchend", cancelLongPress, { passive: true });
    element.addEventListener("touchmove", cancelLongPress, { passive: true });
    controller.touchCleanup = () => {
        element.removeEventListener("touchstart", beginLongPress);
        element.removeEventListener("touchend", cancelLongPress);
        element.removeEventListener("touchmove", cancelLongPress);
    };
}

async function tryAddMarker(controller, latitude, longitude) {
    if (!pointInPolygons(latitude, longitude, controller.locationLockPolygons)
        && controller.locationLockPolygons.length > 0) {
        await controller.dotNet.invokeMethodAsync("NotifyLocationLockRejectedAsync", latitude, longitude);
        return;
    }
    await controller.dotNet.invokeMethodAsync("NotifyMapAddMarkerAsync", latitude, longitude);
}

async function createAdvancedMarker(controller, marker) {
    const position = { lat: Number(marker.latitude), lng: Number(marker.longitude) };
    const content = makeMarkerContent(marker, controller.options);
    let mapMarker;

    if (google.maps.marker?.AdvancedMarkerElement) {
        mapMarker = new google.maps.marker.AdvancedMarkerElement({
            map: controller.map,
            position,
            title: marker.title || marker.label || "",
            content,
            gmpClickable: true
        });
    } else {
        mapMarker = new google.maps.Marker({
            map: controller.map,
            position,
            title: marker.title || marker.label || ""
        });
    }

    const clickHandler = async () => {
        if (controller.options.showMarkerPopups) {
            controller.infoWindow.setContent(markerPopupHtml(marker));
            controller.infoWindow.open({ map: controller.map, anchor: mapMarker });
        }
        await controller.dotNet.invokeMethodAsync("NotifyMarkerClickAsync", marker.id);
    };

    let clickCleanup;
    if (google.maps.marker?.AdvancedMarkerElement && mapMarker instanceof google.maps.marker.AdvancedMarkerElement) {
        mapMarker.addEventListener("gmp-click", clickHandler);
        clickCleanup = () => mapMarker.removeEventListener("gmp-click", clickHandler);
    } else {
        const clickListener = mapMarker.addListener("click", clickHandler);
        clickCleanup = () => clickListener.remove();
    }

    let doubleClickHandler = null;
    if (controller.options.allowMarkerRemoval && content) {
        doubleClickHandler = async event => {
            event.stopPropagation();
            removeMarker(controller, marker.id);
            await controller.dotNet.invokeMethodAsync("NotifyMarkerRemovedAsync", marker.id);
        };
        content.addEventListener("dblclick", doubleClickHandler);
    }

    controller.markers.set(marker.id, { marker: mapMarker, content, clickCleanup, doubleClickHandler, data: marker });
    updateMarkerLabels(controller);
}

function removeMarker(controller, id) {
    const entry = controller.markers.get(id);
    if (!entry)
        return;
    entry.clickCleanup?.();
    if (entry.doubleClickHandler && entry.content)
        entry.content.removeEventListener("dblclick", entry.doubleClickHandler);
    entry.marker.map = null;
    entry.marker.setMap?.(null);
    controller.markers.delete(id);
}

function clearMarkers(controller) {
    for (const id of [...controller.markers.keys()])
        removeMarker(controller, id);
}

function addRegion(controller, region) {
    removeRegion(controller, region.id);
    const fill = colorWithOpacity(region.fillColor, 0.15);
    const paths = (region.coordinates ?? []).map(toPath);
    const polygon = new google.maps.Polygon({
        map: controller.map,
        paths,
        fillColor: fill.color,
        fillOpacity: fill.opacity,
        strokeColor: region.strokeColor,
        strokeOpacity: 1,
        strokeWeight: region.strokeWidth,
        clickable: false
    });
    controller.regions.set(region.id, [polygon]);
}

function addMergedRegion(controller, region) {
    removeRegion(controller, region.id);
    const polygons = [];
    for (const coordinates of region.polygons ?? []) {
        polygons.push(new google.maps.Polygon({
            map: controller.map,
            paths: (coordinates ?? []).map(toPath),
            fillOpacity: 0,
            strokeColor: region.strokeColor,
            strokeOpacity: 1,
            strokeWeight: region.strokeWidth,
            clickable: false
        }));
    }
    controller.regions.set(region.id, polygons);
}

function removeRegion(controller, id) {
    for (const polygon of controller.regions.get(id) ?? [])
        polygon.setMap(null);
    controller.regions.delete(id);
}

function clearRegions(controller) {
    for (const id of [...controller.regions.keys()])
        removeRegion(controller, id);
}

async function geocode(controller, query, entityType, countrySet) {
    const request = { address: query };
    if (countrySet)
        request.componentRestrictions = { country: countrySet.split(",")[0].trim() };
    const response = await controller.geocoder.geocode(request);
    const result = response.results?.find(item => matchesEntityType(item, entityType)) ?? response.results?.[0];
    if (!result)
        return null;
    const viewport = getViewport(result);
    return {
        latitude: result.geometry.location.lat(),
        longitude: result.geometry.location.lng(),
        north: viewport?.north ?? result.geometry.location.lat(),
        south: viewport?.south ?? result.geometry.location.lat(),
        east: viewport?.east ?? result.geometry.location.lng(),
        west: viewport?.west ?? result.geometry.location.lng(),
        geometryId: result.place_id ?? null
    };
}

function matchesEntityType(result, entityType) {
    if (!entityType)
        return true;
    const normalized = entityType.toLowerCase();
    if (normalized === "country") return result.types.includes("country");
    if (normalized === "countrysubdivision") return result.types.includes("administrative_area_level_1");
    if (normalized === "municipality") return result.types.some(type => ["locality", "postal_town", "administrative_area_level_3"].includes(type));
    if (normalized === "neighbourhood" || normalized === "neighborhood") return result.types.some(type => type.startsWith("sublocality") || type === "neighborhood");
    return true;
}

async function reverseGeocode(controller, latitude, longitude) {
    const response = await controller.geocoder.geocode({ location: { lat: latitude, lng: longitude } });
    const result = response.results?.[0];
    return result ? reverseResult(result) : null;
}

async function search(controller, query, limit) {
    const response = await controller.geocoder.geocode({ address: query });
    return (response.results ?? []).slice(0, limit).map(result => {
        const viewport = getViewport(result);
        const address = reverseResult(result);
        return {
            address: result.formatted_address,
            latitude: result.geometry.location.lat(),
            longitude: result.geometry.location.lng(),
            north: viewport?.north ?? result.geometry.location.lat(),
            south: viewport?.south ?? result.geometry.location.lat(),
            east: viewport?.east ?? result.geometry.location.lng(),
            west: viewport?.west ?? result.geometry.location.lng(),
            geometryId: result.place_id ?? null,
            description: [address.municipality, address.countryCode].filter(Boolean).join(", ") || null
        };
    });
}

async function searchMunicipalities(controller, query, countryCode, limit, offset) {
    const response = await controller.geocoder.geocode({
        address: query,
        componentRestrictions: countryCode ? { country: countryCode } : undefined
    });
    const all = (response.results ?? [])
        .filter(result => matchesEntityType(result, "Municipality"))
        .map(reverseResult);
    return { totalResults: all.length, results: all.slice(offset, offset + limit) };
}

async function polygonByPlaceId(controller, placeId) {
    const response = await controller.geocoder.geocode({ placeId });
    return viewportPolygon(getViewport(response.results?.[0]));
}

async function resolveBoundary(controller, query, entityType, countrySet) {
    const requestedTypes = Array.isArray(entityType) ? entityType : [entityType];
    const request = { address: query };
    if (countrySet)
        request.componentRestrictions = { country: countrySet.split(",")[0].trim() };
    const response = await controller.geocoder.geocode(request);
    const result = requestedTypes
        .map(type => response.results?.find(item => matchesEntityType(item, type)))
        .find(Boolean) ?? response.results?.[0];
    return viewportPolygon(getViewport(result));
}

function fitBounds(controller, south, west, north, east, padding) {
    const bounds = new google.maps.LatLngBounds({ lat: south, lng: west }, { lat: north, lng: east });
    if (typeof padding === "number") {
        controller.map.fitBounds(bounds, padding);
        return;
    }
    controller.map.fitBounds(bounds, padding ? {
        top: padding.top,
        right: padding.right,
        bottom: padding.bottom,
        left: padding.left
    } : 40);
}

function clearLines(lines) {
    for (const line of lines)
        line.setMap?.(null);
    lines.length = 0;
}

function setRoutes(controller, routes) {
    clearLines(controller.routeLines);
    for (const route of routes ?? []) {
        const line = new google.maps.Polyline({
            map: controller.map,
            path: (route.points ?? []).map(toLatLng),
            strokeColor: route.color || "#2563eb",
            strokeOpacity: 0.95,
            strokeWeight: 5
        });
        controller.routeLines.push(line);
    }
}

function clearTimelines(controller) {
    for (const item of controller.timelineItems) {
        item.setMap?.(null);
        if ("map" in item)
            item.map = null;
    }
    controller.timelineItems.length = 0;
}

async function setTimelines(controller, timelines, fitToBounds) {
    clearTimelines(controller);
    const bounds = new google.maps.LatLngBounds();
    let hasPoint = false;

    for (const timeline of timelines ?? []) {
        const points = (timeline.points ?? []).map(toLatLng);
        if (timeline.showLine !== false && points.length > 1) {
            const line = new google.maps.Polyline({
                map: controller.map,
                path: points,
                strokeColor: timeline.color || "#0078d4",
                strokeOpacity: 0.9,
                strokeWeight: 4,
                icons: [{ icon: { path: google.maps.SymbolPath.FORWARD_CLOSED_ARROW, scale: 2 }, offset: "50%", repeat: "80px" }]
            });
            controller.timelineItems.push(line);
        }

        for (let index = 0; index < points.length; index++) {
            const point = points[index];
            const source = timeline.points[index];
            const marker = new google.maps.Marker({
                map: controller.map,
                position: point,
                title: source.label || timeline.name || "",
                label: source.label ? { text: source.label, color: timeline.color || "#0078d4" } : undefined
            });
            marker.addListener("click", async () => {
                const content = `<div class="cloud-google-map-popup"><strong>${escapeHtml(source.label || timeline.name || "Location")}</strong>${source.description ? `<span>${escapeHtml(source.description)}</span>` : ""}${source.timestamp ? `<span>${escapeHtml(new Date(source.timestamp).toLocaleString())}</span>` : ""}</div>`;
                controller.infoWindow.setContent(content);
                controller.infoWindow.open({ map: controller.map, anchor: marker });
                await controller.dotNet.invokeMethodAsync("NotifyTimelinePointClickAsync", timeline.id, index);
            });
            controller.timelineItems.push(marker);
            bounds.extend(point);
            hasPoint = true;
        }
    }

    if (fitToBounds && hasPoint)
        controller.map.fitBounds(bounds, 50);
}

function setLocationLock(controller, polygons, options) {
    clearLocationLock(controller);
    controller.locationLockPolygons = polygons ?? [];
    const bounds = new google.maps.LatLngBounds();
    let hasPoint = false;

    if (options?.showBoundary) {
        for (const polygonCoordinates of controller.locationLockPolygons) {
            const fill = colorWithOpacity(options.fillColor, 0.08);
            const paths = (polygonCoordinates ?? []).map(toPath);
            const polygon = new google.maps.Polygon({
                map: controller.map,
                paths,
                fillColor: fill.color,
                fillOpacity: fill.opacity,
                strokeColor: options.strokeColor || "#0078d4",
                strokeWeight: 2,
                clickable: false
            });
            controller.locationLockOverlays.push(polygon);
            for (const path of paths) {
                for (const point of path) {
                    bounds.extend(point);
                    hasPoint = true;
                }
            }
        }
    }

    if (options?.zoomToBoundary && hasPoint)
        controller.map.fitBounds(bounds, 40);
}

function clearLocationLock(controller) {
    for (const overlay of controller.locationLockOverlays)
        overlay.setMap(null);
    controller.locationLockOverlays.length = 0;
    controller.locationLockPolygons = [];
}

function setControls(controller, controls) {
    controller.options.controls = controls;
    controller.map.setOptions({
        zoomControl: controls.zoom?.enabled ?? false,
        zoomControlOptions: { position: controlPosition(controls.zoom?.position) },
        fullscreenControl: controls.fullscreen?.enabled ?? false,
        fullscreenControlOptions: { position: controlPosition(controls.fullscreen?.position) },
        scaleControl: controls.scale?.enabled ?? false,
        rotateControl: controls.compass?.enabled ?? false,
        rotateControlOptions: { position: controlPosition(controls.compass?.position) },
        streetViewControl: false,
        mapTypeControl: controls.style?.enabled ?? false,
        mapTypeControlOptions: { position: controlPosition(controls.style?.position) }
    });
}

function setInteractions(controller, interactions) {
    controller.options.interactions = interactions;
    controller.map.setOptions({
        draggable: interactions.dragPan !== false,
        scrollwheel: interactions.scrollZoom === true,
        disableDoubleClickZoom: interactions.dblClickZoom === false,
        keyboardShortcuts: interactions.keyboard !== false,
        gestureHandling: interactions.touch === false ? "none" : (interactions.scrollZoom ? "greedy" : "cooperative")
    });
}

export async function createMap(dotNet, options) {
    await loadGoogleMaps(options.apiKey, options.language);
    ensureGoogleMapStyles();
    await Promise.all([
        google.maps.importLibrary?.("maps") ?? Promise.resolve(),
        google.maps.importLibrary?.("marker") ?? Promise.resolve(),
        google.maps.importLibrary?.("geocoding") ?? Promise.resolve()
    ]);

    const element = document.getElementById(options.elementId);
    if (!element)
        throw new Error(`Map element '${options.elementId}' was not found.`);

    const map = new google.maps.Map(element, {
        center: { lat: options.latitude, lng: options.longitude },
        zoom: options.zoom,
        mapId: options.mapId || undefined,
        mapTypeId: googleMapType(options.style),
        styles: options.mapId ? undefined : googleStyles(options.style),
        heading: options.bearing,
        tilt: options.pitch,
        minZoom: options.minZoom,
        maxZoom: options.maxZoom,
        clickableIcons: false,
        disableDefaultUI: true
    });

    const controller = createController(dotNet, map, options);
    controller.geocoder = new google.maps.Geocoder();
    setControls(controller, options.controls);
    setInteractions(controller, options.interactions);
    attachMapEvents(controller);
    if (options.showTrafficFlow || options.showTrafficIncidents)
        controller.trafficLayer = new google.maps.TrafficLayer({ map });

    google.maps.event.addListenerOnce(map, "idle", () => dotNet.invokeMethodAsync("NotifyMapReadyAsync"));

    return {
        addMarker: marker => createAdvancedMarker(controller, marker),
        removeMarker: id => removeMarker(controller, id),
        clearMarkers: () => clearMarkers(controller),
        addRegion: region => addRegion(controller, region),
        addMergedRegion: region => addMergedRegion(controller, region),
        removeRegion: id => removeRegion(controller, id),
        clearRegions: () => clearRegions(controller),
        geocode: (query, entityType, countrySet) => geocode(controller, query, entityType, countrySet),
        reverseGeocode: (latitude, longitude) => reverseGeocode(controller, latitude, longitude),
        search: (query, limit) => search(controller, query, limit),
        searchMunicipalities: (query, countryCode, limit, offset) => searchMunicipalities(controller, query, countryCode, limit, offset),
        getBrowserLocation: () => new Promise(resolve => {
            if (!navigator.geolocation) {
                resolve(null);
                return;
            }
            navigator.geolocation.getCurrentPosition(
                position => resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude }),
                () => resolve(null),
                { enableHighAccuracy: true, timeout: 12000, maximumAge: 30000 });
        }),
        getPolygon: geometryId => polygonByPlaceId(controller, geometryId),
        resolveBoundary: (query, entityType, countrySet) => resolveBoundary(controller, query, entityType, countrySet),
        setCenter: (latitude, longitude, zoom) => {
            map.setCenter({ lat: latitude, lng: longitude });
            if (zoom != null) map.setZoom(zoom);
        },
        setBounds: (south, west, north, east, padding) => fitBounds(controller, south, west, north, east, padding),
        setStyle: async style => {
            options.style = style;
            map.setMapTypeId(googleMapType(style));
            if (!options.mapId) map.setOptions({ styles: googleStyles(style) });
            await dotNet.invokeMethodAsync("NotifyStyleChangedAsync", style);
        },
        setTraffic: (flow, incidents) => {
            if (flow || incidents) {
                controller.trafficLayer ??= new google.maps.TrafficLayer();
                controller.trafficLayer.setMap(map);
            } else {
                controller.trafficLayer?.setMap(null);
            }
        },
        setCameraOrientation: (pitch, bearing) => map.setOptions({ tilt: pitch ?? map.getTilt(), heading: bearing ?? map.getHeading() }),
        setControls: controls => setControls(controller, controls),
        setInteractions: interactions => setInteractions(controller, interactions),
        setAddMarkerTrigger: trigger => { options.addMarkerTrigger = trigger; },
        showCurrentLocation: (latitude, longitude) => {
            controller.currentLocation?.setMap(null);
            controller.currentLocation = new google.maps.Marker({
                map,
                position: { lat: latitude, lng: longitude },
                title: "Current location",
                icon: {
                    path: google.maps.SymbolPath.CIRCLE,
                    scale: 8,
                    fillColor: "#1a73e8",
                    fillOpacity: 1,
                    strokeColor: "#ffffff",
                    strokeWeight: 3
                }
            });
            map.setCenter({ lat: latitude, lng: longitude });
            if ((map.getZoom() ?? 0) < 15) map.setZoom(15);
        },
        setLocationLock: (polygons, lockOptions) => setLocationLock(controller, polygons, lockOptions),
        clearLocationLock: () => clearLocationLock(controller),
        isPointAllowed: (latitude, longitude) => pointInPolygons(latitude, longitude, controller.locationLockPolygons),
        setRoutes: routes => setRoutes(controller, routes),
        clearRoutes: () => setRoutes(controller, []),
        setTimelines: (timelines, fitToBoundsValue) => setTimelines(controller, timelines, fitToBoundsValue),
        clearTimelines: () => clearTimelines(controller),
        dispose: () => {
            if (controller.disposed) return;
            controller.disposed = true;
            clearMarkers(controller);
            clearRegions(controller);
            clearLines(controller.routeLines);
            clearTimelines(controller);
            clearLocationLock(controller);
            controller.currentLocation?.setMap(null);
            controller.trafficLayer?.setMap(null);
            controller.infoWindow.close();
            for (const listener of controller.listeners) listener.remove();
            controller.listeners.length = 0;
            controller.touchCleanup?.();
            clearTimeout(controller.longPressTimer);
            element.replaceChildren();
        }
    };
}
