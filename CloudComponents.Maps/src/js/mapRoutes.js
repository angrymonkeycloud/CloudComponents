export class MapRoutes {
    constructor(map, id) {
        this._map = map;
        this._id = `${id}-routes`;
        this._source = null;
        this._layers = [];
        this._restoring = false;
    }

    set(routes) {
        const features = [];
        for (const route of routes || []) {
            const points = route?.points;
            if (!Array.isArray(points) || points.length < 2 || !points.every(point =>
                Number.isFinite(point?.latitude) && Math.abs(point.latitude) <= 90 &&
                Number.isFinite(point?.longitude) && Math.abs(point.longitude) <= 180))
                continue;

            features.push(new atlas.data.Feature(
                new atlas.data.LineString(points.map(point => [point.longitude, point.latitude])),
                { routeId: route.id, color: route.color || '#2563eb' }));
        }

        if (features.length === 0) {
            this.clear();
            return;
        }

        if (!this._source) {
            this._source = new atlas.source.DataSource(this._id);
            this._layers = [
                new atlas.layer.LineLayer(this._source, `${this._id}-casing`, {
                    strokeColor: '#ffffff',
                    strokeWidth: 9,
                    strokeOpacity: 0.9,
                    lineCap: 'round',
                    lineJoin: 'round'
                }),
                new atlas.layer.LineLayer(this._source, `${this._id}-line`, {
                    strokeColor: ['get', 'color'],
                    strokeWidth: 5,
                    lineCap: 'round',
                    lineJoin: 'round'
                })
            ];
        }

        this._source.setShapes(features);
        this.restore();
    }

    restore() {
        if (!this._source || this._restoring)
            return;

        this._restoring = true;
        try {
            if (!this._map.sources.getById(this._source.getId()))
                this._map.sources.add(this._source);

            for (const layer of this._layers)
                if (!this._map.layers.getLayerById(layer.getId()))
                    this._map.layers.add(layer);
        } finally {
            this._restoring = false;
        }
    }

    clear() {
        const source = this._source;
        const layers = this._layers;
        this._source = null;
        this._layers = [];

        for (const layer of layers)
            if (this._map.layers.getLayerById(layer.getId()))
                this._map.layers.remove(layer);

        if (source) {
            if (this._map.sources.getById(source.getId()))
                this._map.sources.remove(source);
            source.dispose();
        }
    }
}
