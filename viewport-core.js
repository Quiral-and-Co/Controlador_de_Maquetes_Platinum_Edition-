(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  if (root) root.JMRI_TC_VIEWPORT = api;
})(typeof window !== 'undefined' ? window : globalThis, function () {
  'use strict';
  const DEFAULT_BOUNDS = Object.freeze({ width: 3000, height: 1800 });
  const DEFAULT_LIMITS = Object.freeze({ minWidth: 300, maxWidth: 6000 });
  function dimensions(bounds = DEFAULT_BOUNDS) {
    const width = Number(bounds.width), height = Number(bounds.height);
    if (!Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0) throw new TypeError('Les dimensions del tauler han de ser positives i finites.');
    return { width, height };
  }
  function limits(options = {}) {
    const minWidth = Number(options.minWidth ?? DEFAULT_LIMITS.minWidth), maxWidth = Number(options.maxWidth ?? DEFAULT_LIMITS.maxWidth);
    if (!Number.isFinite(minWidth) || !Number.isFinite(maxWidth) || minWidth <= 0 || maxWidth < minWidth) throw new TypeError('Els límits de zoom no són vàlids.');
    return { minWidth, maxWidth };
  }
  function clampView(view, bounds = DEFAULT_BOUNDS, options = {}) {
    const board = dimensions(bounds), range = limits(options), rawWidth = Number(view?.width), rawHeight = Number(view?.height);
    if (!Number.isFinite(rawWidth) || !Number.isFinite(rawHeight) || rawWidth <= 0 || rawHeight <= 0) throw new TypeError('El viewBox ha de tenir amplada i alçada positives.');
    const width = Math.max(range.minWidth, Math.min(range.maxWidth, rawWidth)), height = width * rawHeight / rawWidth;
    let x = Number(view.x), y = Number(view.y); if (!Number.isFinite(x)) x = 0; if (!Number.isFinite(y)) y = 0;
    x = width >= board.width ? (board.width - width) / 2 : Math.max(0, Math.min(board.width - width, x));
    y = height >= board.height ? (board.height - height) / 2 : Math.max(0, Math.min(board.height - height, y));
    return { x, y, width, height };
  }
  function fit(bounds = DEFAULT_BOUNDS) { const board = dimensions(bounds); return { x: 0, y: 0, width: board.width, height: board.height }; }
  function zoomAt(view, factor, anchor, bounds = DEFAULT_BOUNDS, options = {}) {
    const current = clampView(view, bounds, options), scale = Number(factor), ax = Number(anchor?.x), ay = Number(anchor?.y);
    if (!Number.isFinite(scale) || scale <= 0 || !Number.isFinite(ax) || !Number.isFinite(ay)) throw new TypeError('El factor i el punt d’ancoratge del zoom han de ser vàlids.');
    const range = limits(options), width = Math.max(range.minWidth, Math.min(range.maxWidth, current.width * scale)), ratio = width / current.width;
    return clampView({ x: ax - (ax - current.x) * ratio, y: ay - (ay - current.y) * ratio, width, height: current.height * ratio }, bounds, options);
  }
  function panBy(view, dx, dy, bounds = DEFAULT_BOUNDS, options = {}) {
    const current = clampView(view, bounds, options), moveX = Number(dx), moveY = Number(dy);
    if (!Number.isFinite(moveX) || !Number.isFinite(moveY)) throw new TypeError('El desplaçament del tauler ha de ser finit.');
    return clampView({ ...current, x: current.x + moveX, y: current.y + moveY }, bounds, options);
  }
  function zoomPercent(view, bounds = DEFAULT_BOUNDS) { const board = dimensions(bounds), current = clampView(view, bounds); return board.width / current.width * 100; }
  return Object.freeze({ DEFAULT_BOUNDS, DEFAULT_LIMITS, clampView, fit, zoomAt, panBy, zoomPercent });
});
