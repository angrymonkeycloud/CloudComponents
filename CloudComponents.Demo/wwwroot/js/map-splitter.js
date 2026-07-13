// Draggable splitter between the map column and results column on map demo pages.
// Delegated pointer events so it works for any .map-page-layout rendered by Blazor.
(function () {
    let active = null; // { layout, splitter, startX, startMapWidth }

    document.addEventListener('pointerdown', (e) => {
        const splitter = e.target.closest?.('.map-page-splitter');
        if (!splitter) return;
        const layout = splitter.closest('.map-page-layout');
        if (!layout) return;
        const mapCol = layout.firstElementChild;
        if (!mapCol) return;
        active = {
            layout,
            splitter,
            startX: e.clientX,
            startMapWidth: mapCol.getBoundingClientRect().width
        };
        splitter.classList.add('dragging');
        splitter.setPointerCapture?.(e.pointerId);
        document.body.style.userSelect = 'none';
        document.body.style.cursor = 'col-resize';
        e.preventDefault();
    });

    document.addEventListener('pointermove', (e) => {
        if (!active) return;
        const total = active.layout.getBoundingClientRect().width;
        const splitterWidth = active.splitter.getBoundingClientRect().width;
        const gap = 24; // grid gaps (2 x 12px)
        const available = total - splitterWidth - gap;
        let mapWidth = active.startMapWidth + (e.clientX - active.startX);
        const min = 280;
        mapWidth = Math.max(min, Math.min(available - min, mapWidth));
        active.layout.style.gridTemplateColumns = `${mapWidth}px ${splitterWidth}px 1fr`;
    });

    const end = () => {
        if (!active) return;
        active.splitter.classList.remove('dragging');
        document.body.style.userSelect = '';
        document.body.style.cursor = '';
        active = null;
    };
    document.addEventListener('pointerup', end);
    document.addEventListener('pointercancel', end);
})();
