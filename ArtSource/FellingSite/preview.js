/* Offline review only. This deliberately does not replace Unity collision or FOV. */
(function () {
  'use strict';

  const cellKey = (x, y) => `${x},${y}`;
  const sameCell = (a, b) => a[0] === b[0] && a[1] === b[1];

  function pointInPolygon(point, polygon) {
    if (!Array.isArray(polygon) || polygon.length < 3) return false;
    const [x, y] = point;
    let inside = false;
    for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
      const [ax, ay] = polygon[j];
      const [bx, by] = polygon[i];
      const cross = (x - ax) * (by - ay) - (y - ay) * (bx - ax);
      if (Math.abs(cross) < 1e-7 && x >= Math.min(ax, bx) && x <= Math.max(ax, bx) && y >= Math.min(ay, by) && y <= Math.max(ay, by)) return true;
      if ((ay > y) !== (by > y) && x < (bx - ax) * (y - ay) / (by - ay) + ax) inside = !inside;
    }
    return inside;
  }

  function normalizeLayout(layout) {
    const grid = layout.grid || {};
    // Names are explicit so a missing mapping produces a visible error instead of a guessed map.
    const zoneSize = grid.zoneSize || grid.zoneDimensions || [grid.zoneWidth, grid.zoneHeight];
    const regionSize = grid.regionSize || grid.artRegionSize;
    const regionOrigin = grid.regionOrigin || grid.artRegionOrigin;
    const pixelsPerCell = grid.artPixelsPerCell || grid.pixelsPerCell;
    const imageGroundOrigin = grid.imageGroundOrigin;
    for (const [name, value] of Object.entries({zoneSize, regionSize, regionOrigin, imageGroundOrigin})) {
      if (!Array.isArray(value) || value.length !== 2 || !value.every(Number.isFinite)) throw new Error(`layout.json has no valid grid.${name} pair.`);
    }
    if (!Number.isFinite(pixelsPerCell) || pixelsPerCell <= 0) throw new Error('layout.json has no positive grid.pixelsPerCell.');
    if (!Array.isArray(layout.walkablePolygon) || layout.walkablePolygon.length < 3) throw new Error('layout.json needs a walkablePolygon in image pixels.');
    if (!Array.isArray(layout.spawn) || layout.spawn.length !== 2) throw new Error('layout.json needs a spawn cell.');
    return {...layout, grid: {...grid, zoneSize, regionSize, regionOrigin, pixelsPerCell, imageGroundOrigin}};
  }

  function buildGeometry(rawLayout) {
    const layout = normalizeLayout(rawLayout);
    const {zoneSize, regionSize, regionOrigin, pixelsPerCell: ppc, imageGroundOrigin} = layout.grid;
    const cellToPixel = cell => [imageGroundOrigin[0] + (cell[0] - regionOrigin[0] + 0.5) * ppc, imageGroundOrigin[1] + (cell[1] - regionOrigin[1] + 0.5) * ppc];
    const pixelToCell = point => [Math.floor((point[0] - imageGroundOrigin[0]) / ppc) + regionOrigin[0], Math.floor((point[1] - imageGroundOrigin[1]) / ppc) + regionOrigin[1]];
    const cells = new Map();
    for (let y = regionOrigin[1]; y < regionOrigin[1] + regionSize[1]; y++) {
      for (let x = regionOrigin[0]; x < regionOrigin[0] + regionSize[0]; x++) {
        if (x < 0 || y < 0 || x >= zoneSize[0] || y >= zoneSize[1]) continue;
        const pixel = cellToPixel([x, y]);
        const blockers = (layout.blockers || []).filter(blocker => pointInPolygon(pixel, blocker.polygon));
        cells.set(cellKey(x, y), {cell: [x, y], pixel, walkable: pointInPolygon(pixel, layout.walkablePolygon) && blockers.length === 0, blocksSight: blockers.some(blocker => blocker.blocksSight), blockers});
      }
    }
    const isWalkable = cell => Boolean(cells.get(cellKey(...cell))?.walkable);
    return {layout, cells, cellToPixel, pixelToCell, isWalkable};
  }

  function neighbors(geometry, cell) {
    const out = [];
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
      if (!dx && !dy) continue;
      const next = [cell[0] + dx, cell[1] + dy];
      if (!geometry.isWalkable(next)) continue;
      if (dx && dy && (!geometry.isWalkable([cell[0] + dx, cell[1]]) || !geometry.isWalkable([cell[0], cell[1] + dy]))) continue;
      out.push({cell: next, cost: dx && dy ? Math.SQRT2 : 1});
    }
    return out;
  }

  function findPath(geometry, start, goal) {
    if (!geometry.isWalkable(start) || !geometry.isWalkable(goal)) return null;
    if (sameCell(start, goal)) return [];
    const heuristic = cell => {
      const dx = Math.abs(cell[0] - goal[0]);
      const dy = Math.abs(cell[1] - goal[1]);
      return Math.max(dx, dy) + (Math.SQRT2 - 1) * Math.min(dx, dy);
    };
    const first = {cell: start, cost: 0, score: heuristic(start), parent: null};
    const open = [first];
    const best = new Map([[cellKey(...start), first]]);
    while (open.length) {
      open.sort((a, b) => a.score - b.score || a.cost - b.cost);
      const current = open.shift();
      if (best.get(cellKey(...current.cell)) !== current) continue;
      if (sameCell(current.cell, goal)) {
        const path = [];
        for (let item = current; item.parent; item = item.parent) path.push(item.cell);
        return path.reverse();
      }
      for (const neighbor of neighbors(geometry, current.cell)) {
        const key = cellKey(...neighbor.cell);
        const cost = current.cost + neighbor.cost;
        if (best.has(key) && best.get(key).cost <= cost) continue;
        const item = {cell: neighbor.cell, cost, score: cost + heuristic(neighbor.cell), parent: current};
        best.set(key, item);
        open.push(item);
      }
    }
    return null;
  }

  function hasLineOfSight(geometry, from, to) {
    let [x, y] = from;
    const dx = Math.abs(to[0] - x), dy = Math.abs(to[1] - y);
    const sx = x < to[0] ? 1 : -1, sy = y < to[1] ? 1 : -1;
    let error = dx - dy;
    while (true) {
      if (x === to[0] && y === to[1]) return true;
      if (!(x === from[0] && y === from[1]) && geometry.cells.get(cellKey(x, y))?.blocksSight) return false;
      const twiceError = 2 * error;
      if (twiceError > -dy) { error -= dy; x += sx; }
      if (twiceError < dx) { error += dx; y += sy; }
    }
  }

  function computeVisible(geometry, origin, radius = 12) {
    const visible = new Set();
    for (const [key, {cell}] of geometry.cells) {
      if (Math.hypot(cell[0] - origin[0], cell[1] - origin[1]) <= radius && hasLineOfSight(geometry, origin, cell)) visible.add(key);
    }
    return visible;
  }

  function createMovementInput() {
    return {held: new Set(), pending: []};
  }

  function heldMovementVector(held) {
    return [Number(held.has('right')) - Number(held.has('left')), Number(held.has('down')) - Number(held.has('up'))];
  }

  function pressMovementInput(input, direction) {
    if (!['up', 'down', 'left', 'right'].includes(direction) || input.held.has(direction)) return;
    const previousHeld = new Set(input.held);
    input.held.add(direction);
    const intent = {vector: heldMovementVector(input.held), sources: new Set(input.held)};
    const last = input.pending[input.pending.length - 1];
    // Simultaneous directional keys form one diagonal intent; separate taps remain separate steps.
    if (last && [...previousHeld].some(key => last.sources.has(key))) input.pending[input.pending.length - 1] = intent;
    else input.pending.push(intent);
  }

  function releaseMovementInput(input, direction) {
    input.held.delete(direction);
  }

  function takeMovementIntent(input) {
    // A key-up cannot erase a tap that happened between animation frames.
    if (input.pending.length) return input.pending.shift().vector;
    const vector = heldMovementVector(input.held);
    return vector[0] || vector[1] ? vector : null;
  }

  function clearMovementInput(input) {
    input.held.clear();
    input.pending.length = 0;
  }

  // Allow geometry checks with Node, without a browser or Unity and without a dependency bundle.
  if (typeof module !== 'undefined' && module.exports) module.exports = {pointInPolygon, normalizeLayout, buildGeometry, neighbors, findPath, computeVisible, createMovementInput, pressMovementInput, releaseMovementInput, takeMovementIntent, clearMovementInput};
  if (typeof document === 'undefined') return;

  const el = id => document.getElementById(id);
  const canvas = el('scene');
  const sceneFrame = el('scene-frame');
  const ctx = canvas.getContext('2d');
  const controls = {grid: el('show-grid'), collision: el('show-collision'), landmarks: el('show-landmarks'), occlusion: el('show-occlusion'), fov: el('show-fov'), effects: el('show-effects'), reduceMotion: el('reduce-motion')};
  controls.reduceMotion.checked = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const state = {geometry: null, reference: null, clean: null, cleanPatch: null, actor: null, view: 'clean', cell: null, tween: null, path: [], input: createMovementInput(), pointer: null, invalidTarget: null, dirty: true, lastFrame: 0, visible: null, loadVersion: 0};
  const announce = message => { el('announcement').textContent = message; };

  function polygonPath(polygon) {
    ctx.beginPath();
    polygon.forEach(([x, y], i) => i ? ctx.lineTo(x, y) : ctx.moveTo(x, y));
    ctx.closePath();
  }

  async function loadImage(path) {
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => resolve(image);
      image.onerror = () => reject(new Error(`Could not load ${path}`));
      const url = new URL(path, document.baseURI);
      url.searchParams.set('review', String(Date.now()));
      image.src = url.href;
    });
  }

  async function loadScene() {
    const version = ++state.loadVersion;
    el('loading').hidden = false;
    el('error').hidden = true;
    clearMovementInput(state.input);
    state.geometry = null;
    state.path = [];
    state.tween = null;
    try {
      if (location.protocol === 'file:') throw new Error('Open this viewer through a local HTTP server. From the repository root, run: python3 -m http.server 8765\nThen open http://localhost:8765/ArtSource/FellingSite/preview.html');
      const response = await fetch(`layout.json?review=${Date.now()}`, {cache: 'no-store'});
      if (!response.ok) throw new Error(`layout.json could not be read (HTTP ${response.status}).`);
      const geometry = buildGeometry(await response.json());
      const assets = geometry.layout.assets || [];
      const referenceAsset = assets.find(asset => asset.id === 'reference');
      const cleanAsset = assets.find(asset => asset.id === 'clean-plate' && ['ready', 'generated'].includes(asset.status));
      const actorAsset = assets.find(asset => asset.role === 'actor' && asset.path);
      const [referenceResult, cleanResult, actorResult] = await Promise.allSettled([
        loadImage(referenceAsset?.path || 'reference.png'),
        cleanAsset?.path ? loadImage(cleanAsset.path) : Promise.reject(new Error('Clean plate is not marked ready in layout.json.')),
        loadImage(actorAsset?.path || 'player-sheet.png')
      ]);
      if (version !== state.loadVersion) return;
      state.reference = referenceResult.status === 'fulfilled' ? referenceResult.value : null;
      state.clean = cleanResult.status === 'fulfilled' ? cleanResult.value : null;
      state.actor = actorResult.status === 'fulfilled' ? actorResult.value : null;
      if (!state.reference && !state.clean) throw new Error(`No artwork could be loaded. ${referenceResult.reason?.message || ''} ${cleanResult.reason?.message || ''}`);
      for (const [name, image] of [['Reference', state.reference], ['Clean plate', state.clean]]) {
        if (image && (image.naturalWidth !== 1536 || image.naturalHeight !== 1024)) throw new Error(`${name} is ${image.naturalWidth} × ${image.naturalHeight}; this scene study requires 1536 × 1024 artwork.`);
      }
      state.cleanPatch = cleanAsset?.patchRect || null;
      if (state.cleanPatch) {
        const rect = state.cleanPatch;
        if (!Array.isArray(rect) || rect.length !== 4 || !rect.every(Number.isFinite) || rect[0] < 0 || rect[1] < 0 || rect[2] <= 0 || rect[3] <= 0 || rect[0] + rect[2] > 1536 || rect[1] + rect[3] > 1024) throw new Error('The clean-plate patchRect must be [x, y, width, height] inside the 1536 × 1024 artwork.');
        if (state.clean && !state.reference) throw new Error('The actor removal patch needs reference.png as its base, but the reference could not be loaded.');
      }
      if (!geometry.isWalkable(geometry.layout.spawn)) throw new Error(`Spawn ${geometry.layout.spawn.join(', ')} is blocked in layout.json.`);
      state.geometry = geometry;
      state.view = state.clean ? 'clean' : 'reference';
      state.cell = geometry.layout.spawn.slice();
      state.pointer = null;
      state.visible = null;
      state.invalidTarget = null;
      state.cleanFailure = cleanResult.status === 'rejected' ? cleanResult.reason.message : '';
      el('clean-view').disabled = !state.clean;
      el('reference-view').disabled = !state.reference;
      const {zoneSize, regionSize, regionOrigin, pixelsPerCell, imageGroundOrigin} = geometry.layout.grid;
      el('grid-mapping').textContent = `${zoneSize.join('×')} zone · ${regionSize.join('×')} art · ${pixelsPerCell}px/cell`;
      el('grid-mapping').title = `Art origin: zone cell ${regionOrigin.join(', ')}. Ground begins at image pixel ${imageGroundOrigin.join(', ')}. Cells sample their centers.`;
      updateAssetStatus();
      updatePlayerReadout();
      announce('Scene ready. Click to walk, or focus the scene and use W, A, S, D or arrow keys.');
      state.dirty = true;
    } catch (error) {
      if (version !== state.loadVersion) return;
      el('error').textContent = error.message;
      el('error').hidden = false;
      el('asset-status').textContent = 'Scene unavailable. See the loading error above.';
      el('clean-view').disabled = true;
      el('reference-view').disabled = true;
      ctx.clearRect(0, 0, canvas.width, canvas.height);
    } finally {
      if (version === state.loadVersion) el('loading').hidden = true;
    }
  }

  function updateAssetStatus() {
    const clean = state.view === 'clean';
    el('clean-view').setAttribute('aria-pressed', String(clean));
    el('reference-view').setAttribute('aria-pressed', String(!clean));
    const sprite = state.actor ? 'Existing player sprite.' : 'Player sprite unavailable; traveler shown as a marker.';
    el('asset-status').textContent = clean
      ? state.cleanPatch ? `Reference + actor removal patch · seam review pending. ${sprite}` : `Generated clean plate · draft artwork. ${sprite}`
      : state.clean ? 'Original reference · traveler is painted; movable proxy hidden.' : 'Reference fallback · clean plate unavailable. Traveler is painted; movable proxy hidden.';
    el('asset-status').title = !state.clean ? state.cleanFailure : '';
  }

  function updatePlayerReadout() {
    el('player-cell').textContent = state.cell ? `${state.cell[0]}, ${state.cell[1]}` : '—';
    state.visible = null;
  }

  function resetTraveler() {
    if (!state.geometry) return;
    state.cell = state.geometry.layout.spawn.slice();
    state.tween = null;
    state.path = [];
    clearMovementInput(state.input);
    state.invalidTarget = null;
    updatePlayerReadout();
    revealTraveler(true);
    announce('Traveler reset to the arrival position.');
    state.dirty = true;
  }

  function revealTraveler(center = false) {
    if (!state.geometry || !sceneFrame.classList.contains('actual-size')) return;
    const [x, y] = state.geometry.cellToPixel(state.cell);
    const width = sceneFrame.clientWidth, height = sceneFrame.clientHeight;
    if (center) {
      sceneFrame.scrollLeft = x - width / 2;
      sceneFrame.scrollTop = y - height / 2;
      return;
    }
    // Keep keyboard movement visible while letting a reviewer independently scroll through the art.
    if (document.activeElement !== canvas) return;
    const padding = 24;
    if (x < sceneFrame.scrollLeft + padding) sceneFrame.scrollLeft = x - padding;
    else if (x > sceneFrame.scrollLeft + width - padding) sceneFrame.scrollLeft = x - width + padding;
    if (y - 48 < sceneFrame.scrollTop + padding) sceneFrame.scrollTop = y - 48 - padding;
    else if (y > sceneFrame.scrollTop + height - padding) sceneFrame.scrollTop = y - height + padding;
  }

  function actorPixel(now) {
    if (!state.tween) return state.geometry.cellToPixel(state.cell);
    const from = state.geometry.cellToPixel(state.tween.from);
    const to = state.geometry.cellToPixel(state.tween.to);
    const t = Math.min(1, (now - state.tween.start) / state.tween.duration);
    return [from[0] + (to[0] - from[0]) * t, from[1] + (to[1] - from[1]) * t];
  }

  function moveActor(now) {
    if (state.tween && now - state.tween.start >= state.tween.duration) {
      state.cell = state.tween.to;
      state.tween = null;
      updatePlayerReadout();
      revealTraveler();
      state.dirty = true;
    }
    if (state.tween) return;
    let next = null;
    const intent = takeMovementIntent(state.input);
    if (intent) {
      const [dx, dy] = intent;
      const desired = [state.cell[0] + dx, state.cell[1] + dy];
      if (neighbors(state.geometry, state.cell).some(item => sameCell(item.cell, desired))) next = desired;
    } else if (state.path.length) next = state.path.shift();
    if (next) {
      const diagonal = next[0] !== state.cell[0] && next[1] !== state.cell[1];
      state.tween = {from: state.cell.slice(), to: next, start: now, duration: 150 * (diagonal ? Math.SQRT2 : 1)};
      state.dirty = true;
    }
  }

  function drawActor(pixel) {
    const [x, y] = pixel;
    ctx.fillStyle = '#080c0999';
    ctx.beginPath(); ctx.ellipse(x, y - 2, 12, 5, 0, 0, Math.PI * 2); ctx.fill();
    ctx.strokeStyle = '#d2dfa68c'; ctx.lineWidth = 1.5;
    ctx.beginPath(); ctx.ellipse(x, y - 1, 14, 5, 0, 0, Math.PI * 2); ctx.stroke();
    if (state.actor) ctx.drawImage(state.actor, 0, 0, 16, 24, Math.round(x - 16), Math.round(y - 48), 32, 48);
    else {
      ctx.fillStyle = '#d5ddb1';
      ctx.beginPath(); ctx.moveTo(x, y - 27); ctx.lineTo(x + 7, y - 16); ctx.lineTo(x, y - 4); ctx.lineTo(x - 7, y - 16); ctx.closePath(); ctx.fill();
      ctx.strokeStyle = '#20291c'; ctx.lineWidth = 2; ctx.stroke();
    }
  }

  function drawPlate() {
    if (state.view === 'clean' && state.clean) {
      if (state.cleanPatch) {
        // Canvas composition keeps every pixel outside the actor removal rectangle from the original.
        ctx.drawImage(state.reference, 0, 0);
        const [x, y, width, height] = state.cleanPatch;
        ctx.drawImage(state.clean, x, y, width, height, x, y, width, height);
      } else ctx.drawImage(state.clean, 0, 0);
    } else ctx.drawImage(state.reference, 0, 0);
  }

  function drawOcclusion(actorPosition) {
    if (!controls.occlusion.checked) return;
    for (const occluder of state.geometry.layout.occluders || []) {
      if (!Array.isArray(occluder.polygon) || actorPosition[1] >= occluder.sortFootY) continue;
      ctx.save(); polygonPath(occluder.polygon); ctx.clip(); drawPlate(); ctx.restore();
    }
  }

  function drawEffects(now) {
    if (!controls.effects.checked || controls.reduceMotion.checked) return;
    for (const [index, effect] of (state.geometry.layout.effects || []).entries()) {
      const polygon = effect.polygon;
      if (!Array.isArray(polygon) || polygon.length < 3) continue;
      const xs = polygon.map(point => point[0]), ys = polygon.map(point => point[1]);
      const minX = Math.min(...xs), maxX = Math.max(...xs), minY = Math.min(...ys), maxY = Math.max(...ys);
      const width = maxX - minX, height = maxY - minY;
      ctx.save(); polygonPath(polygon); ctx.clip();
      const phase = now / 1000;
      const kind = String(effect.kind || effect.id).toLowerCase();
      if (kind.includes('mist')) {
        for (let i = 0; i < 4; i++) {
          const x = minX + width * (.2 + .6 * ((i / 4 + phase * .025) % 1));
          const y = minY + height * (.35 + .22 * Math.sin(phase * .35 + i));
          const radius = Math.max(10, Math.min(width, height) * .4);
          const mist = ctx.createRadialGradient(x, y, 0, x, y, radius);
          mist.addColorStop(0, '#cbd5c40b'); mist.addColorStop(1, '#cbd5c400');
          ctx.fillStyle = mist; ctx.fillRect(minX, minY, width, height);
        }
      } else if (kind.includes('waterfall') || kind.includes('fall')) {
        ctx.strokeStyle = '#bed5ca4d'; ctx.lineWidth = 1;
        for (let i = 0; i < 12; i++) {
          const x = minX + width * (((i * .61803398875) + index * .1) % 1);
          const y = minY + ((phase * 55 + i * 29) % (height + 24)) - 12;
          ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x, y + 6 + i % 7); ctx.stroke();
        }
      } else if (kind.includes('water') || kind.includes('stream') || kind.includes('ripple')) {
        ctx.strokeStyle = '#adc3b629'; ctx.lineWidth = 1;
        for (let i = 0; i < 9; i++) {
          const t = (phase * .19 + i * .271) % 1;
          const x = minX + width * ((i * .61803398875 + .2) % 1);
          const y = minY + height * ((i * .38196601125 + .1) % 1);
          ctx.globalAlpha = Math.sin(t * Math.PI);
          ctx.beginPath(); ctx.ellipse(x, y, 3 + t * 13, 1 + t * 4, 0, 0, Math.PI * 2); ctx.stroke();
        }
      } else if (kind.includes('glow') || kind.includes('spore') || kind.includes('mote')) {
        for (let i = 0; i < 6; i++) {
          const x = minX + width * ((i * .61803398875 + .2) % 1);
          const y = minY + height * ((i * .38196601125 + phase * .01) % 1);
          ctx.fillStyle = `rgba(152,212,185,${.1 + .15 * (.5 + .5 * Math.sin(phase + i))})`;
          ctx.fillRect(x, y, 2, 2);
        }
      }
      ctx.restore();
    }
  }

  function drawGrid() {
    if (!controls.grid.checked) return;
    const {regionSize, imageGroundOrigin, pixelsPerCell: size} = state.geometry.layout.grid;
    ctx.save(); ctx.strokeStyle = '#dce5c941'; ctx.lineWidth = 1;
    ctx.beginPath();
    for (let x = 0; x <= regionSize[0]; x++) { const px = imageGroundOrigin[0] + x * size; ctx.moveTo(px, imageGroundOrigin[1]); ctx.lineTo(px, imageGroundOrigin[1] + regionSize[1] * size); }
    for (let y = 0; y <= regionSize[1]; y++) { const py = imageGroundOrigin[1] + y * size; ctx.moveTo(imageGroundOrigin[0], py); ctx.lineTo(imageGroundOrigin[0] + regionSize[0] * size, py); }
    ctx.stroke();
    ctx.font = '10px ui-monospace, monospace'; ctx.fillStyle = '#e6eddbb3';
    for (const {cell, pixel} of state.geometry.cells.values()) if (cell[0] % 4 === 0 && cell[1] % 4 === 0) ctx.fillText(cell.join(','), pixel[0] - size / 2 + 2, pixel[1] - size / 2 + 11);
    ctx.restore();
  }

  function drawCollision() {
    if (!controls.collision.checked) return;
    const layout = state.geometry.layout;
    ctx.save();
    polygonPath(layout.walkablePolygon); ctx.fillStyle = '#b4d78910'; ctx.fill(); ctx.strokeStyle = '#c4e195bb'; ctx.lineWidth = 2; ctx.stroke();
    for (const blocker of layout.blockers || []) {
      polygonPath(blocker.polygon); ctx.fillStyle = blocker.kind === 'water' ? '#7eadd236' : '#d27c6936'; ctx.fill(); ctx.strokeStyle = blocker.kind === 'water' ? '#8bb9d7b3' : '#d99987b3'; ctx.stroke();
    }
    for (const {pixel, walkable} of state.geometry.cells.values()) {
      ctx.fillStyle = walkable ? '#c8dd9b85' : '#dd9a8573';
      ctx.fillRect(pixel[0] - 1, pixel[1] - 1, 2, 2);
    }
    for (const occluder of layout.occluders || []) {
      polygonPath(occluder.polygon); ctx.strokeStyle = '#d4b8e3bb'; ctx.setLineDash([5, 5]); ctx.stroke();
    }
    ctx.setLineDash([]);
    drawLabel('Cell centers: green = walkable · red = blocked · purple = foreground mask', 18, 1002, 'left');
    ctx.restore();
  }

  function drawLabel(text, x, y, align = 'center') {
    ctx.save(); ctx.font = '12px ui-monospace, monospace';
    const width = ctx.measureText(text).width + 14;
    const left = align === 'left' ? x : x - width / 2;
    ctx.fillStyle = '#131a12e8'; ctx.fillRect(left, y - 15, width, 23);
    ctx.fillStyle = '#e1e6d2'; ctx.textAlign = 'left'; ctx.fillText(text, left + 7, y);
    ctx.restore();
  }

  function drawLandmarks() {
    if (!controls.landmarks.checked) return;
    const layout = state.geometry.layout;
    ctx.save(); ctx.strokeStyle = '#ddd8aeae'; ctx.lineWidth = 2;
    for (const [index, position] of (layout.sterilePositions || []).entries()) {
      const [x, y] = state.geometry.cellToPixel(position.cell);
      ctx.beginPath(); ctx.arc(x, y, 28, 0, Math.PI * 2); ctx.stroke();
      drawLabel(`${index + 1} · ${position.label || 'sterile position'}`, x, y + 46);
    }
    if (layout.seventh?.cell) {
      const [x, y] = state.geometry.cellToPixel(layout.seventh.cell);
      ctx.strokeStyle = '#d7ceb69c'; ctx.setLineDash([3, 6]);
      ctx.beginPath(); ctx.arc(x, y, 17, 0, Math.PI * 2); ctx.stroke(); ctx.setLineDash([]);
      drawLabel('Seventh · absence (annotation only)', x, y - 28);
    }
    if (layout.entrance) {
      const [x, y] = state.geometry.cellToPixel(layout.entrance);
      drawLabel('South approach', x, Math.min(y - 18, 1006));
    }
    ctx.restore();
  }

  function drawRoute(now) {
    const points = [actorPixel(now), ...state.path.map(state.geometry.cellToPixel)];
    if (state.tween) points.splice(1, 0, state.geometry.cellToPixel(state.tween.to));
    if (points.length > 1) {
      ctx.save(); ctx.strokeStyle = '#dde4bd8c'; ctx.setLineDash([3, 8]); ctx.lineWidth = 2;
      ctx.beginPath(); points.forEach(([x, y], i) => i ? ctx.lineTo(x, y) : ctx.moveTo(x, y)); ctx.stroke();
      const last = points[points.length - 1]; ctx.setLineDash([]); ctx.beginPath(); ctx.arc(last[0], last[1], 7, 0, Math.PI * 2); ctx.stroke(); ctx.restore();
    }
    if (state.invalidTarget && now < state.invalidTarget.until) {
      const [x, y] = state.geometry.cellToPixel(state.invalidTarget.cell);
      ctx.save(); ctx.strokeStyle = '#efb39a'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(x - 6, y - 6); ctx.lineTo(x + 6, y + 6); ctx.moveTo(x + 6, y - 6); ctx.lineTo(x - 6, y + 6); ctx.stroke(); ctx.restore();
    }
  }

  function drawFov() {
    if (!controls.fov.checked) return;
    if (!state.visible) state.visible = computeVisible(state.geometry, state.cell);
    ctx.save(); ctx.fillStyle = '#070b09c9';
    const {pixelsPerCell: size, imageGroundOrigin} = state.geometry.layout.grid;
    ctx.fillRect(0, 0, canvas.width, imageGroundOrigin[1]);
    for (const [key, {pixel}] of state.geometry.cells) if (!state.visible.has(key)) ctx.fillRect(pixel[0] - size / 2, pixel[1] - size / 2, size, size);
    ctx.restore();
  }

  function draw(now) {
    ctx.imageSmoothingEnabled = false;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawPlate();
    if (state.view === 'clean' && state.clean) {
      drawEffects(now);
      drawRoute(now);
      const pixel = actorPixel(now);
      drawActor(pixel);
      drawOcclusion(pixel);
    }
    drawFov();
    drawGrid();
    drawCollision();
    drawLandmarks();
    if (state.pointer && (controls.grid.checked || controls.collision.checked)) {
      const {pixelsPerCell: size} = state.geometry.layout.grid;
      const pixel = state.geometry.cellToPixel(state.pointer);
      ctx.strokeStyle = state.geometry.isWalkable(state.pointer) ? '#e5ebcc' : '#e5aa91'; ctx.lineWidth = 2;
      ctx.strokeRect(pixel[0] - size / 2 + 1, pixel[1] - size / 2 + 1, size - 2, size - 2);
    }
  }

  function frame(now) {
    if (state.geometry) {
      moveActor(now);
      if (state.invalidTarget && now >= state.invalidTarget.until) { state.invalidTarget = null; state.dirty = true; }
      const ambient = state.view === 'clean' && controls.effects.checked && !controls.reduceMotion.checked && (state.geometry.layout.effects || []).length;
      const targetAnimating = state.invalidTarget && now <= state.invalidTarget.until + 50;
      if (state.dirty || state.tween || ambient || targetAnimating) {
        if (state.dirty || now - state.lastFrame >= 1000 / 30) {
          draw(now); state.lastFrame = now; state.dirty = false;
        }
      }
    }
    requestAnimationFrame(frame);
  }

  function pointerCell(event) {
    const bounds = canvas.getBoundingClientRect();
    return state.geometry.pixelToCell([(event.clientX - bounds.left) * canvas.width / bounds.width, (event.clientY - bounds.top) * canvas.height / bounds.height]);
  }

  canvas.addEventListener('pointermove', event => {
    if (!state.geometry) return;
    state.pointer = pointerCell(event);
    const cell = state.geometry.cells.get(cellKey(...state.pointer));
    el('pointer-cell').textContent = `${state.pointer.join(', ')} · ${cell ? cell.walkable ? 'walkable' : 'blocked' : 'outside grid'}`;
    canvas.style.cursor = cell?.walkable ? 'crosshair' : 'default';
    state.dirty = true;
  });
  canvas.addEventListener('pointerleave', () => { state.pointer = null; el('pointer-cell').textContent = '—'; state.dirty = true; });
  canvas.addEventListener('pointerdown', event => {
    if (!state.geometry || event.button !== 0) return;
    canvas.focus({preventScroll: true});
    clearMovementInput(state.input);
    const target = pointerCell(event);
    const start = state.tween ? state.tween.to : state.cell;
    const route = findPath(state.geometry, start, target);
    if (route === null) {
      state.invalidTarget = {cell: target, until: performance.now() + 1100};
      announce(`Cell ${target.join(', ')} is blocked or unreachable in this study.`);
    } else {
      state.path = route;
      state.invalidTarget = null;
      announce(`Route to cell ${target.join(', ')}: ${route.length} steps.`);
    }
    state.dirty = true;
  });

  const keyDirection = {w: 'up', ArrowUp: 'up', s: 'down', ArrowDown: 'down', a: 'left', ArrowLeft: 'left', d: 'right', ArrowRight: 'right'};
  window.addEventListener('keydown', event => {
    if (!state.geometry || event.ctrlKey || event.metaKey || event.altKey) return;
    if (event.target !== canvas && event.target !== document.body) return;
    const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
    if (keyDirection[key]) {
      event.preventDefault(); pressMovementInput(state.input, keyDirection[key]); state.path = []; state.dirty = true;
    } else if (key === 'Escape') {
      state.path = []; clearMovementInput(state.input); state.dirty = true; announce('Route cancelled.');
    } else if (key === 'r') resetTraveler();
  });
  window.addEventListener('keyup', event => {
    const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
    if (keyDirection[key]) releaseMovementInput(state.input, keyDirection[key]);
  });
  window.addEventListener('blur', () => clearMovementInput(state.input));
  canvas.addEventListener('blur', () => clearMovementInput(state.input));
  document.addEventListener('visibilitychange', () => { if (document.hidden) clearMovementInput(state.input); });
  for (const control of Object.values(controls)) control.addEventListener('change', () => {
    el('fov-notice').hidden = !controls.fov.checked;
    state.dirty = true;
  });
  el('clean-view').addEventListener('click', () => { state.view = 'clean'; updateAssetStatus(); state.dirty = true; });
  el('reference-view').addEventListener('click', () => { state.view = 'reference'; updateAssetStatus(); state.dirty = true; });
  el('reset-position').addEventListener('click', resetTraveler);
  el('actual-size').addEventListener('click', () => {
    const actualSize = sceneFrame.classList.toggle('actual-size');
    el('actual-size').setAttribute('aria-pressed', String(actualSize));
    sceneFrame.tabIndex = actualSize ? 0 : -1;
    if (actualSize) revealTraveler(true);
    else { sceneFrame.scrollLeft = 0; sceneFrame.scrollTop = 0; }
    announce(actualSize ? 'Actual-size view: 1536 by 1024 native pixels. Scroll the scene to inspect details. Toggle Actual size again to fit the scene.' : 'Fit view: the complete scene is visible.');
    state.dirty = true;
  });
  el('reload-assets').addEventListener('click', loadScene);
  if (!ctx) {
    el('error').textContent = 'This browser did not provide a 2D Canvas context.';
    el('error').hidden = false;
    el('loading').hidden = true;
  } else {
    loadScene();
    requestAnimationFrame(frame);
  }
})();
