(function(root, factory) {
  'use strict';
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.JMRI_TC_SELECTION_MOVE = api;
})(typeof window !== 'undefined' ? window : globalThis, function() {
  'use strict';

  function finite(value, label) {
    const number = Number(value);
    if (!Number.isFinite(number)) throw new TypeError(label + ' ha de ser un nombre finit.');
    return number;
  }
  function snap(value, grid) { return Math.round(value / grid) * grid; }
  function clamp(value, min, max) { return Math.max(min, Math.min(max, value)); }

  function createDrag(pieces, anchorId, pointer, board, grid) {
    grid = grid === undefined ? 10 : finite(grid, 'La graella');
    if (!Array.isArray(pieces) || pieces.length === 0) throw new TypeError('Cal seleccionar almenys una peça.');
    if (!(grid > 0)) throw new RangeError('La graella ha de ser positiva.');
    const width = finite(board && board.width, 'L’amplada del tauler');
    const height = finite(board && board.height, 'L’alçada del tauler');
    if (!(width > 0 && height > 0)) throw new RangeError('Les dimensions del tauler han de ser positives.');
    const pointerStart = {
      x: finite(pointer && pointer.x, 'La coordenada X del cursor'),
      y: finite(pointer && pointer.y, 'La coordenada Y del cursor')
    };
    const seen = new Set();
    const originals = pieces.map(piece => {
      if (!piece || piece.id === undefined || piece.id === null) throw new TypeError('Cada peça ha de tenir un identificador.');
      const id = String(piece.id);
      if (seen.has(id)) throw new Error('La selecció conté identificadors duplicats: ' + id);
      seen.add(id);
      return { id, x: finite(piece.x, 'La coordenada X de ' + id), y: finite(piece.y, 'La coordenada Y de ' + id) };
    });
    const anchor = originals.find(piece => piece.id === String(anchorId));
    if (!anchor) throw new Error('La peça d’ancoratge no forma part de la selecció.');
    const bounds = originals.reduce((box, piece) => ({
      minX: Math.min(box.minX, piece.x), minY: Math.min(box.minY, piece.y),
      maxX: Math.max(box.maxX, piece.x), maxY: Math.max(box.maxY, piece.y)
    }), { minX: Infinity, minY: Infinity, maxX: -Infinity, maxY: -Infinity });
    if (bounds.minX < 0 || bounds.minY < 0 || bounds.maxX > width || bounds.maxY > height) {
      throw new RangeError('La selecció conté peces fora de la superfície del tauler.');
    }
    return { anchorId: anchor.id, pointerStart, originals, bounds, board: { width, height }, grid };
  }

  function move(drag, pointer) {
    if (!drag || !Array.isArray(drag.originals) || !drag.originals.length) throw new TypeError('Estat de moviment invàlid.');
    const x = finite(pointer && pointer.x, 'La coordenada X del cursor');
    const y = finite(pointer && pointer.y, 'La coordenada Y del cursor');
    const rawDx = snap(x - drag.pointerStart.x, drag.grid);
    const rawDy = snap(y - drag.pointerStart.y, drag.grid);
    const epsilon = 1e-9;
    const minDx = Math.ceil((-drag.bounds.minX - epsilon) / drag.grid) * drag.grid;
    const maxDx = Math.floor((drag.board.width - drag.bounds.maxX + epsilon) / drag.grid) * drag.grid;
    const minDy = Math.ceil((-drag.bounds.minY - epsilon) / drag.grid) * drag.grid;
    const maxDy = Math.floor((drag.board.height - drag.bounds.maxY + epsilon) / drag.grid) * drag.grid;
    if (minDx > maxDx || minDy > maxDy) throw new RangeError('La selecció no cap dins de la superfície del tauler.');
    const dx = clamp(rawDx, minDx, maxDx);
    const dy = clamp(rawDy, minDy, maxDy);
    return { dx, dy, positions: drag.originals.map(piece => ({ id: piece.id, x: piece.x + dx, y: piece.y + dy })) };
  }
  function rotate(pieces, angleDeg, board) {
    if (!Array.isArray(pieces) || pieces.length === 0) throw new TypeError('Cal seleccionar almenys una peça.');
    const angle = finite(angleDeg, 'L’angle de rotació');
    const width = finite(board && board.width, 'L’amplada del tauler');
    const height = finite(board && board.height, 'L’alçada del tauler');
    if (!(width > 0 && height > 0)) throw new RangeError('Les dimensions del tauler han de ser positives.');
    const seen = new Set();
    const originals = pieces.map(piece => {
      if (!piece || piece.id === undefined || piece.id === null) throw new TypeError('Cada peça ha de tenir un identificador.');
      const id = String(piece.id);
      if (seen.has(id)) throw new Error('La selecció conté identificadors duplicats: ' + id);
      seen.add(id);
      return {
        id,
        x: finite(piece.x, 'La coordenada X de ' + id),
        y: finite(piece.y, 'La coordenada Y de ' + id),
        rotation: finite(piece.rotation === undefined ? 0 : piece.rotation, 'La rotació de ' + id)
      };
    });
    const bounds = originals.reduce((box, piece) => ({
      minX: Math.min(box.minX, piece.x), minY: Math.min(box.minY, piece.y),
      maxX: Math.max(box.maxX, piece.x), maxY: Math.max(box.maxY, piece.y)
    }), { minX: Infinity, minY: Infinity, maxX: -Infinity, maxY: -Infinity });
    const center = { x: (bounds.minX + bounds.maxX) / 2, y: (bounds.minY + bounds.maxY) / 2 };
    const radians = angle * Math.PI / 180, c = Math.cos(radians), s = Math.sin(radians);
    const epsilon = 1e-7;
    const positions = originals.map(piece => {
      let x = center.x + (piece.x - center.x) * c - (piece.y - center.y) * s;
      let y = center.y + (piece.x - center.x) * s + (piece.y - center.y) * c;
      if (x < -epsilon || y < -epsilon || x > width + epsilon || y > height + epsilon) {
        throw new RangeError('La rotació deixaria una peça fora de la superfície del tauler.');
      }
      if (Math.abs(x) < epsilon) x = 0;
      if (Math.abs(y) < epsilon) y = 0;
      if (Math.abs(x - width) < epsilon) x = width;
      if (Math.abs(y - height) < epsilon) y = height;
      return { id: piece.id, x, y, rotation: ((piece.rotation + angle) % 360 + 360) % 360 };
    });
    return { angle, center, positions };
  }
  function moveXYZ(pieces, delta, board) {
    if (!Array.isArray(pieces) || !pieces.length) throw new TypeError('Cal seleccionar almenys una peça.');
    const width = finite(board && board.width, 'L’amplada del tauler');
    const height = finite(board && board.height, 'L’alçada del tauler');
    if (!(width > 0 && height > 0)) throw new RangeError('Les dimensions del tauler han de ser positives.');
    const dx = finite(delta && delta.x, 'El desplaçament X');
    const dy = finite(delta && delta.y, 'El desplaçament Y');
    const dz = finite(delta && delta.z, 'El desplaçament Z');
    const seen = new Set();
    const originals = pieces.map(piece => {
      if (!piece || piece.id === undefined || piece.id === null) throw new TypeError('Cada peça ha de tenir un identificador.');
      const id = String(piece.id);
      if (seen.has(id)) throw new Error('La selecció conté identificadors duplicats: ' + id);
      seen.add(id);
      return { id: piece.id, x: finite(piece.x, 'La coordenada X de ' + id), y: finite(piece.y, 'La coordenada Y de ' + id), z: finite(piece.zMm === undefined ? 0 : piece.zMm, 'La coordenada Z de ' + id) };
    });
    const positions = originals.map(piece => ({ id: piece.id, x: piece.x + dx, y: piece.y + dy, zMm: piece.z + dz }));
    for (const p of positions) {
      if (p.x < 0 || p.x > width || p.y < 0 || p.y > height) throw new RangeError('El moviment XYZ deixaria una peça fora de la superfície del tauler.');
      if (p.zMm < -10000 || p.zMm > 10000) throw new RangeError('La cota Z ha de quedar entre -10000 i 10000 mm.');
    }
    return { delta: { x: dx, y: dy, z: dz }, positions };
  }

  function createParallel(pieces, distanceMm, options) {
    if (!Array.isArray(pieces) || !pieces.length) throw new TypeError('Cal seleccionar almenys una via per generar-ne una paral·lela.');
    const distance = finite(distanceMm, 'La distància entre eixos');
    if (!(distance > 0) || distance > 1000) throw new RangeError('La distància entre eixos ha de ser superior a 0 i no superar 1000 mm.');
    options = options || {};
    const side = options.side === undefined ? 'left' : options.side;
    if (side !== 'left' && side !== 'right') throw new RangeError('El costat ha de ser left o right.');
    const sign = side === 'left' ? 1 : -1;
    const seen = new Set();
    const tracks = pieces.map((piece, index) => {
      if (!piece || piece.id === undefined || piece.id === null) throw new TypeError('Cada via ha de tenir identificador.');
      const sourceId = String(piece.id);
      if (seen.has(sourceId)) throw new Error('La selecció conté identificadors duplicats: ' + sourceId);
      seen.add(sourceId);
      const x = finite(piece.x, 'La coordenada X de ' + sourceId);
      const y = finite(piece.y, 'La coordenada Y de ' + sourceId);
      const rotation = finite(piece.rotation === undefined ? 0 : piece.rotation, 'La rotació de ' + sourceId);
      const radians = rotation * Math.PI / 180;
      const copy = JSON.parse(JSON.stringify(piece));
      copy.id = typeof options.idFactory === 'function' ? String(options.idFactory(piece, index)) : sourceId + '-P' + (index + 1);
      if (!copy.id || seen.has(copy.id)) throw new Error('L’identificador de la via paral·lela és buit o duplicat.');
      copy.name = (piece.name || sourceId) + ' · paral·lela ' + distance + ' mm';
      copy.x = x + Math.sin(radians) * distance * sign;
      copy.y = y - Math.cos(radians) * distance * sign;
      if (copy.x < 0 || copy.x > finite(options.board && options.board.width || 3000, 'L’amplada del tauler') ||
          copy.y < 0 || copy.y > finite(options.board && options.board.height || 1800, 'L’alçada del tauler')) {
        throw new RangeError('La via paral·lela quedaria fora de la superfície del tauler.');
      }
      if (copy.type === 'curve' && copy.geometry) {
        const radius = finite(copy.geometry.radiusMm || 360, 'El radi de la corba');
        const adjusted = radius + sign * distance;
        if (adjusted < 20) throw new RangeError('El radi de la corba paral·lela seria inferior a 20 mm.');
        copy.geometry.radiusMm = adjusted;
      }
      copy.parallelOf = sourceId;
      copy.parallelDistanceMm = distance;
      copy.parallelSide = side;
      return copy;
    });
    return { distanceMm: distance, side, tracks };
  }

  return Object.freeze({ createDrag, move, moveXYZ, createParallel, rotate });
});
