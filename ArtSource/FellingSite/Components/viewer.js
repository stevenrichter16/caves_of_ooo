/* Offline component review. No gameplay simulation, source writes, or asset generation. */
(function () {
  'use strict';

  const views = ['reference', 'baseline', 'reconstructed', 'base'];
  const fail = message => { throw new Error(message); };
  const pair = value => Array.isArray(value) && value.length === 2 && value.every(Number.isFinite);
  const relativePath = value => typeof value === 'string' && value.length > 0 && !/^(?:[a-z][a-z\d+.-]*:|\/)/i.test(value);
  const ordered = components => [...components].sort((a, b) => a.depth - b.depth || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0));

  function resolveAssetUrl(path, manifestUrl) {
    if (!relativePath(path)) fail(`Asset path must be relative to the manifest URL: ${String(path)}`);
    return new URL(path, manifestUrl).href;
  }

  function validateManifest(data) {
    if (!data || data.schemaVersion !== 1 || typeof data.id !== 'string') fail('Unsupported or incomplete component manifest. Expected schemaVersion 1.');
    const canvas = data.canvas;
    if (!canvas || !Number.isInteger(canvas.width) || canvas.width <= 0 || !Number.isInteger(canvas.height) || canvas.height <= 0 || !Number.isFinite(canvas.pixelsPerCell) || canvas.pixelsPerCell <= 0 || !pair(canvas.imageGroundOrigin) || !pair(canvas.regionOrigin)) fail('Manifest canvas dimensions or coordinate mapping are invalid.');
    const asset = (value, name) => {
      if (!value || !relativePath(value.path)) fail(`${name}.path must be a relative asset path.`);
      if (value.sha256 !== undefined && !/^[a-f\d]{64}$/i.test(value.sha256)) fail(`${name}.sha256 must contain 64 hexadecimal characters.`);
    };
    for (const key of ['reference', 'baseline', 'base', 'report']) asset(data[key], key);
    if (!relativePath(data.base.inferenceMaskPath) || typeof data.base.retainsStatic !== 'boolean') fail('Base inference mask or retainsStatic flag is invalid.');
    if (!Array.isArray(data.components)) fail('Manifest components must be an array.');
    const ids = new Set();
    for (const component of data.components) {
      if (!component || typeof component.id !== 'string' || !component.id || typeof component.name !== 'string') fail('Each component needs an id and name.');
      if (ids.has(component.id)) fail(`Duplicate component id: ${component.id}`);
      ids.add(component.id);
      for (const key of ['mutable', 'defaultVisible', 'contributesToComposite']) if (typeof component[key] !== 'boolean') fail(`${component.id}.${key} must be boolean.`);
      if (!Number.isFinite(component.depth)) fail(`${component.id}.depth must be finite.`);
      const bounds = component.bounds;
      if (!Array.isArray(bounds) || bounds.length !== 4 || !bounds.every(Number.isInteger) || bounds[0] < 0 || bounds[1] < 0 || bounds[2] <= 0 || bounds[3] <= 0 || bounds[0] + bounds[2] > canvas.width || bounds[1] + bounds[3] > canvas.height) fail(`${component.id}.bounds must be a positive rectangle inside the canvas.`);
      if (!pair(component.foot) || !pair(component.pivot) || !Array.isArray(component.footprintCells) || !component.footprintCells.every(pair)) fail(`${component.id} has invalid placement data.`);
      asset(component.sprite, `${component.id}.sprite`);
      if (component.contact !== null && component.contact !== undefined) asset(component.contact, `${component.id}.contact`);
      if (!component.repair || !relativePath(component.repair.maskPath) || typeof component.repair.inferred !== 'boolean') fail(`${component.id} has invalid repair metadata.`);
      if (!component.extraction || typeof component.extraction.method !== 'string' || typeof component.extraction.quality !== 'string' || !Array.isArray(component.extraction.notes) || !component.extraction.notes.every(note => typeof note === 'string')) fail(`${component.id} has invalid extraction metadata.`);
    }
    return data;
  }

  function createReviewState(manifest) {
    return {view: 'reconstructed', selectedId: null, soloId: null, hidden: new Set(manifest.components.filter(item => !item.defaultVisible).map(item => item.id)), masks: false, inferred: false, exploded: false, actualSize: false};
  }

  function reduceReviewState(manifest, previous, action) {
    const next = {...previous, hidden: new Set(previous.hidden)};
    const find = () => manifest.components.find(item => item.id === action.id) || fail(`Unknown component: ${action.id}`);
    switch (action.type) {
      case 'SELECT': next.selectedId = find().id; if (next.soloId) next.soloId = next.selectedId; break;
      case 'SET_VIEW':
        if (!views.includes(action.view)) fail(`Unknown source view: ${action.view}`);
        next.view = action.view; next.soloId = null; next.exploded = false; break;
      case 'TOGGLE_VISIBILITY': {
        const component = find();
        if (!component.contributesToComposite) fail(`${component.name} is retained in the base; this inspection cutout cannot be hidden independently.`);
        if (next.hidden.has(component.id)) next.hidden.delete(component.id); else next.hidden.add(component.id);
        next.view = 'reconstructed'; next.soloId = null; break;
      }
      case 'REMOVE_MUTABLE':
        for (const item of manifest.components) if (item.mutable && item.contributesToComposite) next.hidden.add(item.id);
        next.view = 'reconstructed'; next.soloId = null; break;
      case 'RESTORE_ALL':
        next.hidden = new Set(manifest.components.filter(item => !item.defaultVisible).map(item => item.id));
        next.view = 'reconstructed'; next.soloId = null; next.exploded = false; break;
      case 'SOLO': {
        const component = find();
        next.soloId = previous.soloId === component.id ? null : component.id;
        next.selectedId = component.id; next.view = 'reconstructed'; next.exploded = false; break;
      }
      case 'SET_OPTION':
        if (!['masks', 'inferred', 'exploded', 'actualSize'].includes(action.key) || typeof action.value !== 'boolean') fail('Invalid review option.');
        next[action.key] = action.value;
        if (action.key === 'exploded' && action.value) { next.view = 'reconstructed'; next.soloId = null; next.actualSize = false; }
        break;
      default: fail(`Unknown review action: ${action.type}`);
    }
    return next;
  }

  function getDrawOrder(manifest, state) {
    return ordered(manifest.components.filter(item => item.contributesToComposite && !state.hidden.has(item.id)));
  }

  function hitTestComponents(manifest, state, point, alphaById) {
    const candidates = state.soloId ? manifest.components.filter(item => item.id === state.soloId) : state.view === 'reference' || state.view === 'baseline' ? ordered(manifest.components) : getDrawOrder(manifest, state);
    return [...candidates].reverse().filter(item => {
      const [left, top, width, height] = item.bounds;
      const x = Math.floor(point[0] - left), y = Math.floor(point[1] - top);
      const alpha = alphaById.get(item.id);
      return x >= 0 && y >= 0 && x < width && y < height && alpha && alpha[y * width + x] > 0;
    });
  }

  function clientToScene(point, rect, dimensions) {
    return [(point[0] - rect.left) * dimensions[0] / rect.width, (point[1] - rect.top) * dimensions[1] / rect.height];
  }

  function compareSpriteToBaseline(sprite, baseline, bounds, canvasWidth) {
    const [left, top, width, height] = bounds;
    const result = {opaquePixels: 0, matchingPixels: 0, differentPixels: 0, partialAlphaPixels: 0};
    for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
      const at = (y * width + x) * 4;
      if (sprite[at + 3] === 0) continue;
      if (sprite[at + 3] !== 255) { result.partialAlphaPixels++; continue; }
      result.opaquePixels++;
      const source = ((top + y) * canvasWidth + left + x) * 4;
      if (sprite[at] === baseline[source] && sprite[at + 1] === baseline[source + 1] && sprite[at + 2] === baseline[source + 2]) result.matchingPixels++;
      else result.differentPixels++;
    }
    return result;
  }

  function intersectInferenceWithAlpha(globalMask, localAlpha, bounds, canvasWidth) {
    const [left, top, width, height] = bounds;
    const result = new Uint8Array(width * height);
    for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) result[y * width + x] = Math.min(localAlpha[y * width + x], globalMask[(top + y) * canvasWidth + left + x]);
    return result;
  }

  if (typeof module !== 'undefined' && module.exports) module.exports = {validateManifest, resolveAssetUrl, createReviewState, reduceReviewState, getDrawOrder, hitTestComponents, clientToScene, compareSpriteToBaseline, intersectInferenceWithAlpha};
  if (typeof document === 'undefined') return;

  const el = id => document.getElementById(id);
  const canvas = el('scene'), context = canvas.getContext('2d'), frame = el('scene-frame');
  const manifestUrl = new URL('./build/manifest.json', document.baseURI).href;
  const runtime = {manifest: null, state: null, images: new Map(), sprites: new Map(), contacts: new Map(), repairs: new Map(), alphas: new Map(), fidelity: new Map(), masks: new Map(), inferredSprites: new Map(), errors: [], report: null, hoverId: null, overlaps: [], loading: false, generation: 0, queuedDraw: false, baselinePixels: null, originalPixels: null, inference: null, baseInference: null, assembly: null, explosion: null};
  const announce = message => { el('announcement').textContent = message; };
  const selected = () => runtime.manifest?.components.find(item => item.id === runtime.state?.selectedId) || null;
  const assetName = item => `${item.name} (${item.id})`;
  const number = value => value.toLocaleString('en-US');

  function showError(message) {
    el('error').textContent = message;
    el('error').hidden = !message;
  }

  function offscreen(width, height) {
    const result = document.createElement('canvas'); result.width = width; result.height = height;
    return result;
  }

  function pixelsOf(image) {
    const surface = offscreen(image.naturalWidth, image.naturalHeight);
    const ctx = surface.getContext('2d', {willReadFrequently: true});
    ctx.drawImage(image, 0, 0);
    return ctx.getImageData(0, 0, surface.width, surface.height).data;
  }

  function alphaOf(pixels) {
    const alpha = new Uint8Array(pixels.length / 4);
    for (let i = 0; i < alpha.length; i++) alpha[i] = pixels[i * 4 + 3];
    return alpha;
  }

  async function imageAsset(path, dimensions) {
    const url = new URL(resolveAssetUrl(path, manifestUrl));
    url.searchParams.set('review', String(runtime.generation));
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => {
        if (dimensions && (image.naturalWidth !== dimensions[0] || image.naturalHeight !== dimensions[1])) reject(new Error(`${path}: expected ${dimensions.join('×')}, got ${image.naturalWidth}×${image.naturalHeight}.`));
        else resolve(image);
      };
      image.onerror = () => reject(new Error(`Could not load ${path}`));
      image.src = url.href;
    });
  }

  function recordError(label, error, required = false) {
    runtime.errors.push({label, message: error.message || String(error), required});
  }

  async function fetchJson(url) {
    const response = await fetch(url, {cache: 'no-store'});
    if (!response.ok) fail(`HTTP ${response.status}: ${new URL(url).pathname}`);
    return response.json();
  }

  async function loadBuild() {
    if (runtime.loading) return;
    runtime.loading = true; runtime.generation = Date.now();
    runtime.manifest = null; runtime.state = null; runtime.hoverId = null; runtime.overlaps = [];
    runtime.images.clear(); runtime.sprites.clear(); runtime.contacts.clear(); runtime.repairs.clear(); runtime.alphas.clear(); runtime.fidelity.clear(); runtime.masks.clear(); runtime.inferredSprites.clear();
    runtime.errors = []; runtime.inference = null; runtime.baseInference = null; runtime.assembly = null; runtime.report = null;
    runtime.baselinePixels = null; runtime.originalPixels = null;
    el('component-list').replaceChildren(); el('component-count').textContent = '0';
    el('selection-details').replaceChildren(); el('extraction-notes').replaceChildren();
    el('selection-name').textContent = 'Choose a component'; el('selection-state').textContent = 'Reading the current build…';
    el('comparisons').hidden = true; el('overlap-picker').hidden = true; el('retained-note').hidden = true; el('compare-link').hidden = true;
    el('fidelity-status').textContent = 'Source fidelity unverified while assets load.';
    el('loading').hidden = false; el('loading').textContent = 'Reading component build…';
    el('refresh').disabled = true;
    showError('');
    try {
      if (location.protocol === 'file:') fail('Use a local HTTP server. From the repository root, run python3 -m http.server 8765, then open /ArtSource/FellingSite/Components/.');
      runtime.manifest = validateManifest(await fetchJson(manifestUrl));
      runtime.state = createReviewState(runtime.manifest);
      const data = runtime.manifest;
      const size = [data.canvas.width, data.canvas.height];
      const baseResults = await Promise.allSettled(['reference', 'baseline', 'base'].map(key => imageAsset(data[key].path, size)));
      baseResults.forEach((result, index) => {
        const key = ['reference', 'baseline', 'base'][index];
        if (result.status === 'fulfilled') runtime.images.set(key, result.value);
        else recordError(key, result.reason, true);
      });
      if (runtime.images.has('baseline')) runtime.baselinePixels = pixelsOf(runtime.images.get('baseline'));
      if (runtime.images.has('reference')) runtime.originalPixels = pixelsOf(runtime.images.get('reference'));
      const otherResults = await Promise.allSettled([imageAsset(data.base.inferenceMaskPath, size), fetchJson(resolveAssetUrl(data.report.path, manifestUrl))]);
      if (otherResults[0].status === 'fulfilled') {
        runtime.inference = grayscaleMask(otherResults[0].value, [193, 145, 224], 112);
        if (runtime.images.has('base')) {
          const baseAlpha = alphaOf(pixelsOf(runtime.images.get('base')));
          const coverage = intersectInferenceWithAlpha(runtime.inference.alpha, baseAlpha, [0, 0, ...size], size[0]);
          runtime.baseInference = coloredMask(coverage, size[0], size[1], [193, 145, 224], 112);
        }
      }
      else recordError('inference coverage', otherResults[0].reason);
      if (otherResults[1].status === 'fulfilled') runtime.report = otherResults[1].value;
      else recordError('build report', otherResults[1].reason);
      let completed = 0;
      await Promise.all(data.components.map(async component => {
        const dimensions = component.bounds.slice(2);
        const results = await Promise.allSettled([
          imageAsset(component.sprite.path, dimensions),
          component.contact ? imageAsset(component.contact.path, dimensions) : Promise.resolve(null),
          imageAsset(component.repair.maskPath, dimensions)
        ]);
        if (results[0].status === 'fulfilled') {
          const image = results[0].value;
          runtime.sprites.set(component.id, image);
          const pixels = pixelsOf(image);
          runtime.alphas.set(component.id, alphaOf(pixels));
          runtime.fidelity.set(component.id, {
            baseline: runtime.baselinePixels ? compareSpriteToBaseline(pixels, runtime.baselinePixels, component.bounds, data.canvas.width) : null,
            reference: runtime.originalPixels ? compareSpriteToBaseline(pixels, runtime.originalPixels, component.bounds, data.canvas.width) : null
          });
        } else recordError(`${component.id} sprite`, results[0].reason, component.contributesToComposite);
        if (results[1].status === 'fulfilled') { if (results[1].value) runtime.contacts.set(component.id, results[1].value); }
        else recordError(`${component.id} contact`, results[1].reason, component.contributesToComposite);
        if (results[2].status === 'fulfilled') runtime.repairs.set(component.id, grayscaleMask(results[2].value, [202, 155, 232], 112));
        else recordError(`${component.id} repair coverage`, results[2].reason);
        completed++;
        el('loading').textContent = `Reading component assets… ${completed} / ${data.components.length}`;
      }));
      if (!canReconstruct()) {
        runtime.state.view = runtime.images.has('reference') ? 'reference' : runtime.images.has('baseline') ? 'baseline' : 'base';
        showError('Reconstruction is unavailable because required assets are missing. The available source comparison is shown instead. Expand Build evidence & asset loading for exact failures.');
      } else if (runtime.errors.length) showError(`${runtime.errors.length} optional asset or evidence item(s) could not be read. Reconstruction is available; missing coverage and inspection assets remain unverified. See Build evidence & asset loading.`);
      verifyAssembly();
      updateEvidence();
      updateUI();
      announce('Component review loaded. Select artwork or use the component list.');
    } catch (error) {
      showError(`Component build unavailable: ${error.message}\nThe viewer needs build/manifest.json and its declared images. Use Refresh build after export finishes.`);
      el('inventory-status').textContent = 'Build unavailable · no reconstruction claimed.';
      el('view-status').textContent = 'No component assets loaded.';
      el('assembly-status').textContent = 'Assembly comparison unavailable.';
      runtime.manifest = null; runtime.state = null;
      for (const button of document.querySelectorAll('[data-view], #remove-mutable, #restore-all, #hide-selected, #solo-selected, #locate-selected')) button.disabled = true;
      context.clearRect(0, 0, canvas.width, canvas.height);
    } finally {
      runtime.loading = false; el('loading').hidden = true; el('refresh').disabled = false;
      scheduleDraw();
    }
  }

  function canReconstruct() {
    if (!runtime.manifest || !runtime.images.has('base')) return false;
    return runtime.manifest.components.filter(item => item.contributesToComposite).every(item => runtime.sprites.has(item.id) && (!item.contact || runtime.contacts.has(item.id)));
  }

  function verifyAssembly() {
    if (!canReconstruct() || !runtime.baselinePixels) return;
    const {width, height} = runtime.manifest.canvas;
    const surface = offscreen(width, height), ctx = surface.getContext('2d', {willReadFrequently: true});
    ctx.imageSmoothingEnabled = false; ctx.drawImage(runtime.images.get('base'), 0, 0);
    for (const item of getDrawOrder(runtime.manifest, createReviewState(runtime.manifest))) drawComponent(ctx, item);
    const pixels = ctx.getImageData(0, 0, width, height).data;
    let differentPixels = 0, maximumChannelDifference = 0;
    for (let i = 0; i < pixels.length; i += 4) {
      let differs = false;
      for (let channel = 0; channel < 4; channel++) {
        const difference = Math.abs(pixels[i + channel] - runtime.baselinePixels[i + channel]);
        if (difference) differs = true;
        maximumChannelDifference = Math.max(maximumChannelDifference, difference);
      }
      if (differs) differentPixels++;
    }
    runtime.assembly = {differentPixels, maximumChannelDifference, pixels: width * height};
  }

  function grayscaleMask(image, color, opacity) {
    const pixels = pixelsOf(image), alpha = new Uint8Array(pixels.length / 4);
    for (let i = 0; i < alpha.length; i++) alpha[i] = pixels[i * 4];
    return {alpha, image: coloredMask(alpha, image.naturalWidth, image.naturalHeight, color, opacity), outline: coloredMask(alpha, image.naturalWidth, image.naturalHeight, color, 0, true)};
  }

  function coloredMask(alpha, width, height, color, opacity, outline = false) {
    const surface = offscreen(width, height), ctx = surface.getContext('2d');
    const result = ctx.createImageData(width, height);
    for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
      const index = y * width + x;
      if (!alpha[index]) continue;
      const edge = x < 2 || y < 2 || x >= width - 2 || y >= height - 2 || !alpha[index - 2] || !alpha[index + 2] || !alpha[index - width * 2] || !alpha[index + width * 2];
      const value = outline ? edge ? 255 : 0 : opacity * alpha[index] / 255;
      result.data.set([color[0], color[1], color[2], value], index * 4);
    }
    ctx.putImageData(result, 0, 0);
    return surface;
  }

  function spriteMask(item, outline = false) {
    const key = `${item.id}:${outline}`;
    if (!runtime.masks.has(key)) {
      const alpha = runtime.alphas.get(item.id);
      if (!alpha) return null;
      const hue = [...item.id].reduce((sum, char) => sum + char.charCodeAt(0), 0);
      const colors = [[197, 219, 139], [118, 194, 198], [222, 159, 132], [188, 161, 218], [210, 197, 127]];
      runtime.masks.set(key, coloredMask(alpha, item.bounds[2], item.bounds[3], outline ? [235, 243, 193] : colors[hue % colors.length], outline ? 0 : 90, outline));
    }
    return runtime.masks.get(key);
  }

  function inferredSpriteMask(item) {
    if (!runtime.inference || !runtime.alphas.has(item.id) || !(item.extraction.inferredPixelCount > 0)) return null;
    if (!runtime.inferredSprites.has(item.id)) {
      const coverage = intersectInferenceWithAlpha(runtime.inference.alpha, runtime.alphas.get(item.id), item.bounds, runtime.manifest.canvas.width);
      runtime.inferredSprites.set(item.id, coloredMask(coverage, item.bounds[2], item.bounds[3], [193, 145, 224], 112));
    }
    return runtime.inferredSprites.get(item.id);
  }

  function dispatch(action, message) {
    if (!runtime.manifest) return;
    try {
      runtime.state = reduceReviewState(runtime.manifest, runtime.state, action);
      if (runtime.state.view === 'reconstructed' && !runtime.state.soloId && !canReconstruct()) {
        runtime.state.view = runtime.images.has('reference') ? 'reference' : 'baseline';
        announce('Reconstruction is unavailable; source comparison remains visible.');
      } else if (message) announce(message);
      updateUI(); scheduleDraw();
    } catch (error) { announce(error.message); showError(error.message); }
  }

  function updateEvidence() {
    el('asset-errors').replaceChildren();
    for (const item of runtime.errors) {
      const li = document.createElement('li'); li.textContent = `${item.required ? 'Required' : 'Optional'} · ${item.label}: ${item.message}`; el('asset-errors').append(li);
    }
    el('report-status').replaceChildren();
    if (runtime.report) {
      const intact = runtime.report.intactReassembly, removal = runtime.report.allMutableRemoved;
      const summary = intact && Number.isFinite(intact.changedPixels) && Number.isFinite(intact.maxChannelDifference)
        ? `Export report: intact assembly ${intact.passed === true ? 'passed' : 'did not pass'} · ${number(intact.changedPixels)} changed pixels · maximum channel difference ${intact.maxChannelDifference}/255.`
        : 'Build report loaded; expected intact-reassembly measurements are unavailable.';
      el('report-status').append(document.createTextNode(summary));
      if (removal && Number.isFinite(removal.protectedChangedPixels) && Number.isFinite(removal.inferredPixelCount)) el('report-status').append(document.createTextNode(` All-mutable-removed export: ${number(removal.protectedChangedPixels)} protected pixels changed; ${number(removal.inferredPixelCount)} inferred pixels.`));
      if (removal?.path && relativePath(removal.path)) {
        const link = document.createElement('a'); link.href = resolveAssetUrl(removal.path, manifestUrl); link.textContent = ' Open removal export'; link.target = '_blank'; link.rel = 'noopener'; el('report-status').append(link);
      }
    } else el('report-status').textContent = 'Build report unavailable. No report-derived provenance claim is made.';
    el('report-content').textContent = runtime.report ? JSON.stringify(runtime.report, null, 2) : '';
    const result = runtime.assembly;
    el('assembly-status').textContent = !result ? 'Assembly comparison unavailable: baseline or required component assets are missing.' : result.differentPixels === 0 ? `Default assembly: exact RGBA match to loaded baseline · ${number(result.pixels)} pixels checked in this browser.` : `Default assembly differs from baseline at ${number(result.differentPixels)} / ${number(result.pixels)} pixels · maximum channel difference ${result.maximumChannelDifference}/255. See build report.`;
  }

  function updateUI() {
    const data = runtime.manifest, state = runtime.state;
    if (!data || !state) return;
    for (const button of document.querySelectorAll('[data-view]')) {
      button.setAttribute('aria-pressed', String(button.dataset.view === state.view));
      button.disabled = button.dataset.view === 'reconstructed' ? !canReconstruct() : !runtime.images.has(button.dataset.view);
    }
    for (const [id, key] of [['show-masks', 'masks'], ['show-inference', 'inferred'], ['show-exploded', 'exploded']]) el(id).checked = state[key];
    el('show-exploded').disabled = !canReconstruct();
    el('show-inference').disabled = !runtime.inference;
    el('actual-size').setAttribute('aria-pressed', String(state.actualSize));
    el('actual-size').disabled = state.exploded;
    el('actual-size').title = state.exploded ? 'Exploded review has an illustrative fit transform. Return to normal view for native pixels.' : 'Toggle fitted composition and native pixels in a scrollable frame.';
    el('remove-mutable').disabled = !canReconstruct(); el('restore-all').disabled = !canReconstruct();
    const contributing = data.components.filter(item => item.contributesToComposite);
    const hidden = contributing.filter(item => state.hidden.has(item.id)).length;
    const mutable = contributing.filter(item => item.mutable).length;
    el('inventory-status').textContent = `${data.components.length} components · ${contributing.length} assembly layers · ${mutable} mutable · ${hidden} hidden`;
    renderList(); renderSelection(); updateViewStatus(); sizeViewport(); renderCrops();
  }

  function renderList() {
    const focusedId = document.activeElement?.getAttribute('data-component-id');
    const previousScroll = el('component-list').scrollTop;
    const query = el('component-search').value.trim().toLowerCase();
    const items = ordered(runtime.manifest.components).reverse().filter(item => `${item.name} ${item.id} ${item.kind}`.toLowerCase().includes(query));
    el('component-count').textContent = query ? `${items.length} / ${runtime.manifest.components.length}` : String(items.length);
    const fragment = document.createDocumentFragment();
    for (const item of items) {
      const li = document.createElement('li'), button = document.createElement('button'), label = document.createElement('span'), detail = document.createElement('small'), status = document.createElement('span');
      button.type = 'button'; button.setAttribute('aria-pressed', String(item.id === runtime.state.selectedId));
      button.setAttribute('data-component-id', item.id);
      label.append(document.createTextNode(item.name));
      detail.textContent = `${item.kind} · ${item.contributesToComposite ? item.mutable ? 'mutable' : 'static review layer' : 'retained study'}`; label.append(detail);
      const missing = !runtime.sprites.has(item.id);
      status.textContent = missing ? 'missing' : !item.contributesToComposite ? 'study' : runtime.state.hidden.has(item.id) ? 'hidden' : 'visible';
      status.className = 'layer-state';
      if (missing) button.classList.add('missing-layer');
      else if (runtime.state.hidden.has(item.id)) button.classList.add('hidden-layer');
      button.append(label, status); button.addEventListener('click', () => selectComponent(item.id));
      li.append(button); fragment.append(li);
    }
    if (!items.length) { const li = document.createElement('li'); li.textContent = 'No matching components.'; fragment.append(li); }
    el('component-list').replaceChildren(fragment);
    el('component-list').scrollTop = previousScroll;
    if (focusedId) [...el('component-list').querySelectorAll('button')].find(button => button.getAttribute('data-component-id') === focusedId)?.focus({preventScroll: true});
  }

  function renderSelection() {
    const item = selected();
    el('selection-details').replaceChildren(); el('extraction-notes').replaceChildren();
    el('hide-selected').disabled = !item || !item.contributesToComposite || !canReconstruct();
    el('solo-selected').disabled = !item || !runtime.sprites.has(item.id);
    el('locate-selected').disabled = !item;
    el('retained-note').hidden = !item || item.contributesToComposite;
    el('compare-link').hidden = !item;
    if (!item) {
      el('selection-name').textContent = 'Choose a component'; el('selection-state').textContent = 'Click the artwork or use the component list.';
      el('fidelity-status').textContent = 'Source fidelity unverified until a component is selected.';
      el('overlap-picker').hidden = true; return;
    }
    el('selection-name').textContent = item.name;
    const hidden = runtime.state.hidden.has(item.id);
    el('selection-state').textContent = !runtime.sprites.has(item.id) ? 'Sprite asset unavailable.' : !item.contributesToComposite ? 'Retained in base; inspection cutout.' : `${hidden ? 'Hidden' : 'Visible'} · ${item.mutable ? 'mutable authoring flag' : 'static layer; hiding is inspection only'}`;
    el('hide-selected').textContent = hidden ? 'Restore' : 'Hide';
    el('hide-selected').title = item.contributesToComposite ? item.mutable ? 'Change this layer’s review visibility.' : 'Hide this static layer for inspection only.' : 'This cutout is retained in the base and cannot be hidden independently.';
    el('solo-selected').setAttribute('aria-pressed', String(runtime.state.soloId === item.id));
    const details = {Id: item.id, Bounds: item.bounds.join(', '), Depth: String(item.depth), Foot: item.foot.join(', '), Method: item.extraction.method, Quality: item.extraction.quality, Removal: item.repair.inferred ? `Inferred ${item.repair.surface} backing` : 'No inferred backing declared for its removal'};
    if (typeof item.extraction.provenance === 'string') details.Provenance = item.extraction.provenance;
    if (Number.isFinite(item.extraction.inferredPixelCount)) details['Inferred px'] = `${number(item.extraction.inferredPixelCount)} (export metadata)`;
    details.Contact = item.contact ? 'Separate contribution, drawn before sprite' : 'None';
    const removal = runtime.report?.removals?.find?.(entry => entry.id === item.id);
    if (removal) {
      if (Number.isFinite(removal.subjectPixels)) details['Subject px'] = number(removal.subjectPixels);
      if (Number.isFinite(removal.repairPixels)) details['Repair px'] = number(removal.repairPixels);
      if (Number.isFinite(removal.outsideSupportChangedPixels)) details['Off-support'] = `${number(removal.outsideSupportChangedPixels)} pixels changed`;
      if (Number.isFinite(removal.protectedChangedPixels)) details['Protected'] = `${number(removal.protectedChangedPixels)} pixels changed`;
    }
    for (const [key, value] of Object.entries(details)) {
      const dt = document.createElement('dt'), dd = document.createElement('dd'); dt.textContent = key; dd.textContent = value; el('selection-details').append(dt, dd);
    }
    const fidelity = runtime.fidelity.get(item.id);
    const result = fidelity?.baseline;
    el('fidelity-status').textContent = !result ? 'Sprite fidelity unverified: baseline or sprite unavailable.' : `${result.opaquePixels === 0 ? 'Sprite has no opaque pixels available for an exact RGB comparison.' : result.differentPixels ? `${number(result.differentPixels)} of ${number(result.opaquePixels)} opaque sprite pixels differ from baseline RGB.` : `${number(result.opaquePixels)} opaque sprite pixels exactly match baseline RGB.`}${result.partialAlphaPixels ? ` ${number(result.partialAlphaPixels)} partial-alpha pixels excluded.` : ''}${fidelity.reference?.opaquePixels ? ` Original RGB: ${number(fidelity.reference.matchingPixels)}/${number(fidelity.reference.opaquePixels)} match.` : ''}`;
    for (const note of item.extraction.notes) { const li = document.createElement('li'); li.textContent = note; el('extraction-notes').append(li); }
    el('overlap-picker').hidden = runtime.overlaps.length < 2;
    el('overlap-list').replaceChildren();
    for (const overlapping of runtime.overlaps) {
      const button = document.createElement('button'); button.type = 'button'; button.textContent = overlapping.name; button.setAttribute('aria-pressed', String(overlapping.id === item.id));
      button.addEventListener('click', () => selectComponent(overlapping.id)); el('overlap-list').append(button);
    }
  }

  function updateViewStatus() {
    const state = runtime.state;
    if (state.soloId) el('view-status').textContent = `Solo inspection · ${selected()?.name || state.soloId} · checkerboard is transparency. Hidden-layer choices are preserved.`;
    else if (state.exploded) el('view-status').textContent = 'Exploded review · offsets and scale are illustrative; original placements are unchanged.';
    else if (state.view === 'reference') el('view-status').textContent = 'Untouched original reference · painted traveler is part of the image. Review visibility changes do not alter it.';
    else if (state.view === 'baseline') el('view-status').textContent = 'Baseline target · original with the declared actor-removal patch. This is the assembly comparison target.';
    else if (state.view === 'base') el('view-status').textContent = `Actual base underlay · ${runtime.manifest.base.retainsStatic ? 'retained static scenery remains.' : 'independent layers are removed; checkerboard is an unfilled diagnostic void.'}`;
    else el('view-status').textContent = 'Reconstructed from exported base + visible layers · contact contribution precedes each sprite. Click artwork to inspect.';
    el('overlay-legend').hidden = !state.masks && !state.inferred;
    el('overlay-legend').textContent = `${state.masks ? 'Colored coverage: actual sprite alpha. ' : ''}${state.inferred ? state.view === 'reference' || state.view === 'baseline' ? 'Purple outlines mark inferred backing beneath this source comparison.' : 'Purple fill: exposed inferred backing, including structural sprites. Outline: selected removal coverage.' : ''}`;
  }

  function selectComponent(id) {
    dispatch({type: 'SELECT', id}, `Selected ${runtime.manifest.components.find(item => item.id === id)?.name || id}.`);
  }

  function sizeViewport() {
    const item = runtime.state?.soloId ? selected() : null;
    const dimensions = item ? item.bounds.slice(2) : runtime.manifest ? [runtime.manifest.canvas.width, runtime.manifest.canvas.height] : [1536, 1024];
    if (canvas.width !== dimensions[0] || canvas.height !== dimensions[1]) { canvas.width = dimensions[0]; canvas.height = dimensions[1]; }
    const availableWidth = Math.max(220, el('scene-column').clientWidth - 2);
    const availableHeight = Math.max(240, window.innerHeight - el('workspace').getBoundingClientRect().top - 78);
    const actual = Boolean(runtime.state?.actualSize && !runtime.state.exploded);
    frame.classList.toggle('actual-size', actual); frame.tabIndex = actual ? 0 : -1;
    if (actual) {
      frame.style.width = `${availableWidth + 2}px`; frame.style.height = `${availableHeight}px`;
      canvas.style.width = `${dimensions[0]}px`; canvas.style.height = `${dimensions[1]}px`;
    } else {
      const scale = Math.min(availableWidth / dimensions[0], availableHeight / dimensions[1], 1);
      const width = Math.floor(dimensions[0] * scale), height = Math.floor(dimensions[1] * scale);
      frame.style.width = `${width + 2}px`; frame.style.height = `${height + 2}px`;
      canvas.style.width = `${width}px`; canvas.style.height = `${height}px`;
      frame.scrollLeft = 0; frame.scrollTop = 0;
    }
    el('inspector').style.maxHeight = `${availableHeight + 34}px`;
  }

  function checker(ctx, width, height) {
    ctx.fillStyle = '#141b11'; ctx.fillRect(0, 0, width, height); ctx.fillStyle = '#252d20';
    for (let y = 0; y < height; y += 16) for (let x = 0; x < width; x += 16) if ((x / 16 + y / 16) % 2 === 0) ctx.fillRect(x, y, 16, 16);
  }

  function drawComponent(ctx, item, offset = [0, 0]) {
    const x = item.bounds[0] + offset[0], y = item.bounds[1] + offset[1];
    if (runtime.contacts.has(item.id)) ctx.drawImage(runtime.contacts.get(item.id), x, y);
    if (runtime.sprites.has(item.id)) ctx.drawImage(runtime.sprites.get(item.id), x, y);
  }

  function drawMain() {
    runtime.queuedDraw = false;
    if (!runtime.manifest || !runtime.state || runtime.loading) return;
    const state = runtime.state, data = runtime.manifest;
    context.imageSmoothingEnabled = false; context.clearRect(0, 0, canvas.width, canvas.height);
    runtime.explosion = null;
    if (state.soloId) {
      const item = selected(); checker(context, canvas.width, canvas.height);
      if (item && runtime.sprites.has(item.id)) {
        if (runtime.contacts.has(item.id)) context.drawImage(runtime.contacts.get(item.id), 0, 0);
        context.drawImage(runtime.sprites.get(item.id), 0, 0);
        if (state.inferred) { const inferred = inferredSpriteMask(item); if (inferred) context.drawImage(inferred, 0, 0); }
        if (state.masks) { const mask = spriteMask(item); if (mask) context.drawImage(mask, 0, 0); }
        if (state.inferred && runtime.repairs.has(item.id)) context.drawImage(runtime.repairs.get(item.id).outline, 0, 0);
      }
      return;
    }
    if (state.exploded) { drawExploded(); return; }
    checker(context, canvas.width, canvas.height);
    const image = runtime.images.get(state.view === 'reconstructed' ? 'base' : state.view);
    if (image) context.drawImage(image, 0, 0);
    if (state.inferred && runtime.inference) {
      if (state.view === 'reference' || state.view === 'baseline') context.drawImage(runtime.inference.outline, 0, 0);
      else if (runtime.baseInference) context.drawImage(runtime.baseInference, 0, 0);
    }
    if (state.view === 'reconstructed') for (const item of getDrawOrder(data, state)) {
      drawComponent(context, item);
      if (state.inferred) { const inferred = inferredSpriteMask(item); if (inferred) context.drawImage(inferred, item.bounds[0], item.bounds[1]); }
    }
    const overlays = state.view === 'reconstructed' ? getDrawOrder(data, state) : ordered(data.components);
    if (state.masks) for (const item of overlays) {
      const mask = spriteMask(item); if (mask) context.drawImage(mask, item.bounds[0], item.bounds[1]);
    }
    if (state.inferred && selected() && runtime.repairs.has(selected().id)) context.drawImage(runtime.repairs.get(selected().id).outline, selected().bounds[0], selected().bounds[1]);
    const highlightIds = state.view === 'reference' || state.view === 'baseline' ? state.masks ? [runtime.hoverId, state.selectedId] : [] : [runtime.hoverId, state.selectedId];
    for (const id of new Set(highlightIds.filter(Boolean))) {
      const item = data.components.find(candidate => candidate.id === id);
      if (!item) continue;
      const mask = spriteMask(item, true);
      if (mask) context.drawImage(mask, item.bounds[0], item.bounds[1]);
      if (state.hidden.has(id) && item.contributesToComposite && state.view === 'reconstructed') {
        context.save(); context.setLineDash([5, 6]); context.strokeStyle = '#d6e4b4aa'; context.lineWidth = 2; context.strokeRect(...item.bounds); context.restore();
      }
    }
  }

  function drawExploded() {
    const items = getDrawOrder(runtime.manifest, runtime.state);
    const extraX = items.length * 11, extraY = items.length * 7;
    const scale = Math.min(canvas.width / (canvas.width + extraX + 30), canvas.height / (canvas.height + extraY + 30));
    const tx = 15, ty = extraY * scale + 15;
    const shifted = items.map((item, index) => ({...item, bounds: [item.bounds[0] + (index + 1) * 11, item.bounds[1] - (index + 1) * 7, item.bounds[2], item.bounds[3]], original: item}));
    runtime.explosion = {scale, tx, ty, components: shifted};
    context.save(); context.translate(tx, ty); context.scale(scale, scale);
    context.globalAlpha = .3; context.drawImage(runtime.images.get('base'), 0, 0);
    if (runtime.state.inferred && runtime.baseInference) context.drawImage(runtime.baseInference, 0, 0);
    context.globalAlpha = 1;
    for (const item of shifted) {
      drawComponent(context, item);
      if (runtime.state.inferred) {
        const inferred = inferredSpriteMask(item.original);
        if (inferred) context.drawImage(inferred, item.bounds[0], item.bounds[1]);
        if (item.id === runtime.state.selectedId && runtime.repairs.has(item.id)) context.drawImage(runtime.repairs.get(item.id).outline, item.bounds[0], item.bounds[1]);
      }
      if (runtime.state.masks || item.id === runtime.state.selectedId || item.id === runtime.hoverId) {
        const mask = spriteMask(item.original, item.id === runtime.state.selectedId || item.id === runtime.hoverId);
        if (mask) context.drawImage(mask, item.bounds[0], item.bounds[1]);
      }
      if (item.id === runtime.state.selectedId) {
        context.strokeStyle = '#d9e4b9a6'; context.lineWidth = 2; context.setLineDash([5, 6]);
        context.beginPath(); context.moveTo(item.original.bounds[0], item.original.bounds[1]); context.lineTo(item.bounds[0], item.bounds[1]); context.stroke(); context.setLineDash([]);
      }
    }
    context.restore();
  }

  function scheduleDraw() {
    if (!runtime.queuedDraw) { runtime.queuedDraw = true; requestAnimationFrame(drawMain); }
  }

  function renderCrops() {
    const item = selected(); el('comparisons').hidden = !item;
    if (!item) return;
    el('comparison-title').textContent = `${item.name} · aligned crops`;
    el('crop-description').textContent = `All four crops share source rectangle [${item.bounds.join(', ')}]. Checkerboard is transparency. Base underlay excludes structural sprites; some inferred backing is carried by those lower layers. Hide the selection to inspect its assembled backing. ${runtime.state.inferred ? 'Purple marks exposed inferred pixels.' : 'Enable Inferred regions to inspect coverage.'}`;
    for (const key of ['reference', 'baseline', 'sprite', 'base']) {
      const crop = el(`crop-${key}`), ctx = crop.getContext('2d');
      crop.width = item.bounds[2]; crop.height = item.bounds[3]; ctx.imageSmoothingEnabled = false;
      const image = key === 'sprite' ? runtime.sprites.get(item.id) : runtime.images.get(key);
      if (image) {
        if (key === 'sprite') ctx.drawImage(image, 0, 0);
        else ctx.drawImage(image, ...item.bounds, 0, 0, item.bounds[2], item.bounds[3]);
        if (key === 'base' && runtime.state.inferred && runtime.baseInference) ctx.drawImage(runtime.baseInference, ...item.bounds, 0, 0, item.bounds[2], item.bounds[3]);
        if (key === 'sprite' && runtime.state.inferred) { const inferred = inferredSpriteMask(item); if (inferred) ctx.drawImage(inferred, 0, 0); }
        if (key === 'sprite' && runtime.state.masks) { const mask = spriteMask(item); if (mask) ctx.drawImage(mask, 0, 0); }
      } else {
        ctx.fillStyle = '#211c16'; ctx.fillRect(0, 0, crop.width, crop.height); ctx.fillStyle = '#e6bd9d'; ctx.font = '12px sans-serif'; ctx.fillText('Asset unavailable', 8, 20);
      }
      crop.setAttribute('aria-label', `${item.name}: ${key} crop, ${crop.width} by ${crop.height} pixels${image ? '' : ', asset unavailable'}`);
    }
  }

  function pointerInfo(event) {
    let point = clientToScene([event.clientX, event.clientY], canvas.getBoundingClientRect(), [canvas.width, canvas.height]);
    if (runtime.state.soloId && selected()) point = [point[0] + selected().bounds[0], point[1] + selected().bounds[1]];
    if (runtime.explosion) {
      point = [(point[0] - runtime.explosion.tx) / runtime.explosion.scale, (point[1] - runtime.explosion.ty) / runtime.explosion.scale];
      return {point, hits: hitTestComponents({...runtime.manifest, components: runtime.explosion.components}, runtime.state, point, runtime.alphas)};
    }
    return {point, hits: hitTestComponents(runtime.manifest, runtime.state, point, runtime.alphas)};
  }

  function locateSelection() {
    const item = selected(); if (!item) return;
    if (runtime.state.exploded) dispatch({type: 'SET_OPTION', key: 'exploded', value: false});
    if (runtime.state.actualSize) {
      const point = runtime.state.soloId ? [item.bounds[2] / 2, item.bounds[3] / 2] : [item.bounds[0] + item.bounds[2] / 2, item.bounds[1] + item.bounds[3] / 2];
      frame.scrollLeft = point[0] - frame.clientWidth / 2; frame.scrollTop = point[1] - frame.clientHeight / 2;
    }
    canvas.focus({preventScroll: true}); scheduleDraw(); announce(`Located ${item.name} at its authored placement.`);
  }

  canvas.addEventListener('pointermove', event => {
    if (!runtime.state || runtime.loading) return;
    const {point, hits} = pointerInfo(event);
    runtime.hoverId = hits[0]?.id || null;
    el('pointer-status').textContent = runtime.state.exploded ? 'Illustrative offsets' : `${Math.floor(point[0])}, ${Math.floor(point[1])} px`;
    canvas.style.cursor = hits.length ? 'pointer' : 'default'; scheduleDraw();
  });
  canvas.addEventListener('pointerleave', () => { runtime.hoverId = null; el('pointer-status').textContent = '—'; scheduleDraw(); });
  canvas.addEventListener('pointerdown', event => {
    if (!runtime.state || runtime.loading || event.button !== 0) return;
    canvas.focus({preventScroll: true});
    const {hits} = pointerInfo(event); runtime.overlaps = hits;
    if (hits.length) selectComponent(hits[0].id);
    else if (runtime.state.soloId) announce('Transparent part of the solo sprite. The inspected component remains selected.');
    else { runtime.state.selectedId = null; updateUI(); scheduleDraw(); announce('No component at this pixel. Use the list to inspect hidden layers.'); }
  });
  for (const button of document.querySelectorAll('[data-view]')) button.addEventListener('click', () => dispatch({type: 'SET_VIEW', view: button.dataset.view}, `Showing ${button.textContent.toLowerCase()} comparison.`));
  for (const [id, key] of [['show-masks', 'masks'], ['show-inference', 'inferred'], ['show-exploded', 'exploded']]) el(id).addEventListener('change', () => dispatch({type: 'SET_OPTION', key, value: el(id).checked}));
  el('actual-size').addEventListener('click', () => {
    if (!runtime.state) return;
    dispatch({type: 'SET_OPTION', key: 'actualSize', value: !runtime.state.actualSize}, runtime.state.actualSize ? 'Fit view: the complete composition is visible.' : 'Actual size: native pixels in a scrollable frame.');
    if (selected()) locateSelection();
  });
  el('component-search').addEventListener('input', () => { if (runtime.manifest) renderList(); });
  el('remove-mutable').addEventListener('click', () => dispatch({type: 'REMOVE_MUTABLE'}, 'All mutable assembly layers hidden. Static layers retain their review visibility.'));
  el('restore-all').addEventListener('click', () => dispatch({type: 'RESTORE_ALL'}, 'Authored visibility restored. Solo and exploded inspection ended.'));
  el('hide-selected').addEventListener('click', () => { const item = selected(); if (item) dispatch({type: 'TOGGLE_VISIBILITY', id: item.id}, `${item.name} ${runtime.state.hidden.has(item.id) ? 'restored' : 'hidden'} in the review composition.`); });
  el('solo-selected').addEventListener('click', () => { const item = selected(); if (item) dispatch({type: 'SOLO', id: item.id}, runtime.state.soloId === item.id ? 'Solo ended; previous visibility restored.' : `Solo inspection: ${item.name}.`); });
  el('locate-selected').addEventListener('click', locateSelection);
  el('native-crops').addEventListener('change', () => el('comparisons').classList.toggle('native-crops', el('native-crops').checked));
  el('refresh').addEventListener('click', loadBuild);
  canvas.addEventListener('keydown', event => {
    if (!runtime.state || event.ctrlKey || event.metaKey || event.altKey) return;
    const key = event.key.toLowerCase(), item = selected();
    if (key === 'h' && item && !el('hide-selected').disabled) { event.preventDefault(); el('hide-selected').click(); }
    else if (key === 's' && item && !el('solo-selected').disabled) { event.preventDefault(); el('solo-selected').click(); }
    else if (key === 'l' && item) { event.preventDefault(); locateSelection(); }
    else if (key === 'escape' && runtime.state.soloId) { event.preventDefault(); dispatch({type: 'SOLO', id: runtime.state.soloId}, 'Solo inspection ended.'); }
  });
  window.addEventListener('resize', () => { sizeViewport(); scheduleDraw(); });
  if (!context) { showError('This browser did not provide a 2D Canvas context.'); el('loading').hidden = true; }
  else { sizeViewport(); loadBuild(); }
})();
