#!/usr/bin/env node
// Real Blazor WASM, native travel, UI input and IndexedDB; no simulation fixtures.
// Renderer observation is read-only. Mobile emulation is not physical iPhone evidence.
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium, webkit, devices } = require(process.env.COO_PLAYWRIGHT_PATH || '@playwright/test');
process.env.PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS = '1';
const BASE = process.env.COO_SMOKE_URL || 'http://127.0.0.1:8098';
const OUT = path.resolve(process.env.COO_SMOKE_OUTPUT || path.join(__dirname, 'browser-results-world-travel'));
const STEP = 20_000, BOOT = 90_000, CASE = 300_000;
const report = { startedAt: new Date().toISOString(), url: BASE,
  scope: 'Real WASM and IndexedDB; emulated phone viewports; read-only renderer observations; no game fixtures.',
  nativeReturnPolicy: 'The native actor remembers its last surface zone and position, not a position per zone. An immediate same-zone return restores its exact position; ascending from Sill replaces that memory, so later glade descent uses the center.', cases: [] };
let activeBrowser;
const check = (condition, message) => { if (!condition) throw new Error(message); };
async function receipt() { await fs.mkdir(OUT, { recursive: true }); await fs.writeFile(path.join(OUT, 'results.json'), JSON.stringify(report, null, 2)); }
async function step(record, name, work) {
  const start = Date.now();
  try { const detail = await work(); record.steps.push({ name, status: 'passed', ms: Date.now() - start, detail }); console.log(`PASS ${record.name}: ${name}`); return detail; }
  catch (error) { record.steps.push({ name, status: 'failed', ms: Date.now() - start, error: error.stack || String(error) }); throw error; }
}
async function state(page) {
  return page.evaluate(() => ({
    mode: document.getElementById('world-canvas').dataset.mode,
    location: document.getElementById('location').textContent.trim(),
    position: document.getElementById('position').textContent.trim(),
    tick: Number(document.getElementById('turn-number').textContent.replace('TICK ', '')),
    hp: document.getElementById('hp-text').textContent.trim(),
    name: document.getElementById('character-name').textContent.trim(),
    zone: window.__worldTravelView?.data?.zone,
    worldX: window.__worldTravelView?.data?.worldX,
    worldY: window.__worldTravelView?.data?.worldY,
  }));
}
function unchanged(before, after, reason) {
  check(JSON.stringify(before) === JSON.stringify(after), `${reason}: ${JSON.stringify({ before, after })}`);
}
async function frames(page) { await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))); }
async function settled(page) {
  await page.waitForFunction(() => document.getElementById('loading-screen').hidden
    && document.getElementById('busy-overlay').hidden
    && !/^Saving/.test(document.getElementById('save-state').textContent), null, { timeout: BOOT });
  await frames(page);
}
async function observeRenderer(page) {
  await page.evaluate(async () => {
    if (window.__worldTravelWatching) return;
    const { WorldMapView } = await import('./map.js');
    const draw = WorldMapView.prototype.draw;
    WorldMapView.prototype.draw = function (...args) {
      const result = draw.apply(this, args);
      if (this.canvas.id === 'world-canvas') window.__worldTravelView = {
        data: this.data, bounds: this.bounds ? { ...this.bounds } : null, scale: this.scale,
      };
      return result;
    };
    window.__worldTravelWatching = true;
  });
  await page.locator('#center-map').tap(); await frames(page);
}
async function more(page) {
  await settled(page); await page.locator('#more-button').tap();
  await page.getByRole('heading', { name: 'Your expedition', exact: true }).waitFor();
}
async function worldPanel(page) {
  await more(page); await page.getByRole('button', { name: /^World map\b/ }).tap();
  await page.getByRole('heading', { name: 'World map', exact: true }).waitFor(); await settled(page);
}
async function closePanel(page) {
  await page.locator('#close-overlay').tap(); await page.locator('#overlay').waitFor({ state: 'hidden' }); await settled(page);
}
async function setOut(page) {
  const before = await state(page); await worldPanel(page);
  unchanged(before, await state(page), 'Opening the travel panel advanced the world');
  await page.getByRole('button', { name: 'Set out on the world map · 1 turn', exact: true }).tap();
  await page.waitForFunction(() => document.getElementById('world-canvas').dataset.mode === 'world');
  await page.locator('#overlay').waitFor({ state: 'hidden' }); await settled(page);
  const after = await state(page);
  check(after.tick === before.tick + 10, `World-map entry must cost one ordinary turn: ${JSON.stringify({ before, after })}`);
  check(after.location === 'World map', `World map is mislabeled: ${after.location}`);
  return { before, after };
}
async function enterArea(page, expectedName) {
  const before = await state(page); const button = page.locator('#interact-button');
  check(/Enter area/i.test(await button.textContent()), 'World-map interaction shortcut does not offer Enter area.');
  await button.tap(); await page.waitForFunction(() => document.getElementById('world-canvas').dataset.mode === 'local');
  await page.locator('#overlay').waitFor({ state: 'hidden' }); await settled(page);
  const after = await state(page);
  check(after.location === expectedName, `Expected ${expectedName}, entered ${after.location}.`);
  check(after.tick === before.tick + 10, `Area entry must cost one ordinary turn: ${JSON.stringify({ before, after })}`);
  return { before, after };
}
async function worldStep(page, dx, expectedX) {
  const before = await state(page); await page.locator(`[data-move="${dx},0"]`).tap();
  await page.waitForFunction(position => document.getElementById('position').textContent.trim() === position, `${expectedX}, 10`);
  await settled(page); const after = await state(page);
  check(after.mode === 'world' && after.worldX === expectedX && after.worldY === 10,
    `Compass did not move exactly one world cell: ${JSON.stringify({ before, after })}`);
  check(after.tick === before.tick + 20, `One world step must cost20ticks: ${JSON.stringify({ before, after })}`);
  return { before, after };
}
async function controlFits(page, selector) {
  const result = await page.locator(selector).evaluate(node => {
    const r = node.getBoundingClientRect(), point = document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2);
    return { x: r.x, y: r.y, width: r.width, height: r.height, vw: innerWidth, vh: innerHeight,
      visible: !!node.getClientRects().length && getComputedStyle(node).visibility !== 'hidden', hit: point === node || node.contains(point) };
  });
  check(result.visible && result.width >= 43.5 && result.height >= 43.5 && result.hit,
    `${selector} is not an accessible44px target: ${JSON.stringify(result)}`);
  check(result.x >= -1 && result.y >= -1 && result.x + result.width <= result.vw + 1 && result.y + result.height <= result.vh + 1,
    `${selector} is outside the viewport: ${JSON.stringify(result)}`);
  return result;
}
async function fittedMap(page) {
  const result = await page.evaluate(() => {
    const v = window.__worldTravelView, canvas = document.getElementById('world-canvas'), rect = canvas.getBoundingClientRect();
    const data = v?.data, b = v?.bounds;
    const cells = (data?.cells || []).filter(c => c.x >= 30 && c.x < 50 && c.y >= 3 && c.y < 23);
    const blockedCells = b ? cells.flatMap(cell => {
      const x = rect.left + b.ox + (cell.x + .5) * b.tw, y = rect.top + b.oy + (cell.y + .5) * b.th;
      const hit = document.elementFromPoint(x, y);
      return hit === canvas ? [] : [{ x: cell.x, y: cell.y, screenX: x, screenY: y,
        blocker: hit ? `${hit.tagName.toLowerCase()}#${hit.id}.${String(hit.className).replace(/\s+/g, '.')}` : null }];
    }) : [];
    return { mode: canvas.dataset.mode, isWorldMap: data?.isWorldMap, mapBounds: data?.mapBounds, scale: v?.scale,
      position: { x: data?.x, y: data?.y, worldX: data?.worldX, worldY: data?.worldY },
      cellCount: cells.length, uniqueCells: new Set(cells.map(c => `${c.x},${c.y}`)).size,
      hitTestedCells: b ? cells.length : 0, blockedCellCount: blockedCells.length, blockedCells: blockedCells.slice(0, 20),
      namedCells: cells.filter(c => typeof c.name === 'string' && c.name.length && c.biome).length,
      landmarkCount: cells.filter(c => c.landmark && c.mapGlyph && c.mapColor).length,
      sill: cells.find(c => c.x === 40 && c.y === 13), playerCell: cells.find(c => c.x === data?.x && c.y === data?.y),
      bounds: b, canvas: { width: rect.width, height: rect.height },
      extent: b ? { left: b.ox + 30 * b.tw, top: b.oy + 3 * b.th, right: b.ox + 50 * b.tw, bottom: b.oy + 23 * b.th } : null };
  });
  check(result.mode === 'world' && result.isWorldMap, 'The renderer is not using its world-map mode.');
  check(result.scale === 1, `Default world-map zoom did not stay at1: ${result.scale}`);
  check(JSON.stringify(result.mapBounds) === JSON.stringify({ x: 30, y: 3, width: 20, height: 20 }), `Native map bounds differ: ${JSON.stringify(result.mapBounds)}`);
  check(result.uniqueCells === 400 && result.cellCount === 400 && result.namedCells === 400, `Not all400 native world cells are projected: ${JSON.stringify(result)}`);
  check(result.sill?.landmark && /Sill/i.test(result.sill.name) && result.sill.mapGlyph && result.landmarkCount > 0,
    `Known Sill marker is missing: ${JSON.stringify(result.sill)}`);
  check(result.playerCell?.mapGlyph && result.playerCell?.mapColor, 'Player marker lost the underlying native terrain marker.');
  const e = result.extent;
  check(e && e.left >= -1 && e.top >= -1 && e.right <= result.canvas.width + 1 && e.bottom <= result.canvas.height + 1,
    `All20×20 cells do not fit the canvas: ${JSON.stringify(result)}`);
  check(result.bounds.tw > 0 && result.bounds.th > 0, 'World-map tile size is invalid.');
  check(result.hitTestedCells === 400 && result.blockedCellCount === 0,
    `World-map cell centers cannot all receive taps: ${JSON.stringify({ count: result.hitTestedCells, blocked: result.blockedCellCount, cells: result.blockedCells })}`);
  for (const selector of ['[data-move="-1,-1"]','[data-move="0,-1"]','[data-move="1,-1"]','[data-move="-1,0"]','#wait-button',
    '[data-move="1,0"]','[data-move="-1,1"]','[data-move="0,1"]','[data-move="1,1"]','#interact-button',
    '#touch-controls [data-panel="inventory"]','#touch-controls [data-panel="abilities"]','#more-button']) await controlFits(page, selector);
  return result;
}
async function inspectRemote(page) {
  const before = await state(page);
  const point = await page.evaluate(() => {
    const b = window.__worldTravelView.bounds;
    return { x: b.ox + (43 + .5) * b.tw, y: b.oy + (17 + .5) * b.th }; // Logical13,14, away from player and HUD.
  });
  await page.locator('#world-canvas').tap({ position: point }); await page.locator('#overlay').waitFor({ state: 'visible' });
  await settled(page); unchanged(before, await state(page), 'A remote map tap moved the player or spent a turn');
  const text = await page.locator('#overlay-body').textContent(); check(text.trim().length > 0, 'Map inspection is empty.');
  await closePanel(page); unchanged(before, await state(page), 'Closing map inspection spent a turn');
  return { inspectedLogicalCell: [13,14], point, text };
}
async function run(record, engine) {
  let browser, context, page, timer;
  const attach = p => {
    p.setDefaultTimeout(STEP); p.setDefaultNavigationTimeout(30_000);
    p.on('pageerror', e => record.errors.push({ kind: 'page', message: e.stack || String(e) }));
    p.on('console', m => { if (m.type() === 'error') record.errors.push({ kind: 'console', message: m.text() }); });
    p.on('response', r => { if (r.url().startsWith(BASE) && r.status() >= 400) record.errors.push({ kind: 'http', status: r.status(), url: r.url() }); });
    p.on('requestfailed', r => record.requestFailures.push({ url: r.url(), error: r.failure()?.errorText }));
  };
  const shot = async suffix => { const file = `${record.name}-${suffix}.png`; await page.screenshot({ path: path.join(OUT, file), timeout: 10_000 }); record.screenshots.push(file); };
  try {
    browser = await engine.launch({ headless: true, timeout: 30_000 }); activeBrowser = browser;
    timer = setTimeout(() => { record.timedOut = true; browser.close().catch(() => {}); }, CASE);
    context = await browser.newContext({ ...devices['iPhone 13'], viewport: record.viewport, screen: record.viewport,
      isMobile: true, hasTouch: true, deviceScaleFactor: 2 });
    page = await context.newPage(); attach(page);
    const initial = await step(record, 'real seeded Classic glade boots and saves', async () => {
      await page.goto(BASE, { waitUntil: 'domcontentloaded' }); await page.locator('#new-button').tap();
      await page.locator('#builds .build-card').first().waitFor(); await page.locator('#player-name').fill(`Travel ${record.name}`.slice(0,32));
      await page.locator('#world-seed').fill('8128'); await page.locator('#enter-world').tap();
      await page.locator('#game-screen').waitFor({ state: 'visible', timeout: BOOT }); await settled(page);
      await page.waitForFunction(() => document.getElementById('save-state').textContent.includes('Saved on this device'), null, { timeout: BOOT });
      await observeRenderer(page);
      record.release = await page.evaluate(async () => (await (await fetch('sw.js', { cache: 'no-store' })).text()).match(/ooo-static-([a-f0-9]+)/)?.[1] || 'unknown');
      const s = await state(page); check(s.mode === 'local' && s.location === 'Gleaners’ Glade', `Unexpected starting place: ${JSON.stringify(s)}`);
      await shot('glade'); return s;
    });
    const localZoom = await step(record, 'manual local zoom and World map reader are free', async () => {
      const before = await state(page), prior = await page.evaluate(() => window.__worldTravelView.scale);
      await page.locator('#zoom-in').tap(); await frames(page); const scale = await page.evaluate(() => window.__worldTravelView.scale);
      check(scale > prior, 'Manual zoom did not change local scale.'); await worldPanel(page);
      unchanged(before, await state(page), 'Zoom or travel reader spent a turn'); await shot('travel-panel'); await closePanel(page);
      return scale;
    });
    await step(record, 'moved glade position restores exactly on an immediate native roundtrip', async () => {
      const before = await state(page), [x,y] = before.position.split(',').map(Number);
      await page.locator('[data-move="1,0"]').tap();
      await page.waitForFunction(position => document.getElementById('position').textContent.trim() === position, `${x + 1}, ${y}`);
      await settled(page); const moved = await state(page);
      check(moved.tick === before.tick + 10, 'The controlled glade step did not cost one ordinary turn.');
      const ascent = await setOut(page), descent = await enterArea(page, 'Gleaners’ Glade');
      check(descent.after.position === moved.position, `Immediate descent lost the noncentral position: ${JSON.stringify({ moved, descent })}`);
      check(await page.evaluate(() => window.__worldTravelView.scale) === localZoom, 'Immediate map roundtrip reset manual local zoom.');
      return { moved, ascent, descent, policy: report.nativeReturnPolicy };
    });
    await step(record, 'world-map entry costs exactly one turn', async () => {
      const result = await setOut(page); check(result.after.position === '11, 10', `UI leaked embedded map offsets: ${JSON.stringify(result)}`); return result;
    });
    await step(record, 'all400 cells fit and receive taps with native markers and44px controls', async () => { const detail = await fittedMap(page); await shot('world-map'); return detail; });
    if (record.name === 'webkit-landscape') await step(record, 'short landscape keeps all400 world cells tappable at default zoom', async () => {
      const before = await state(page);
      await page.setViewportSize({ width: 568, height: 320 }); await settled(page);
      const compact = await fittedMap(page); unchanged(before, await state(page), 'Short landscape resize spent a turn');
      await shot('world-map-short-landscape');
      await page.locator('#touch-controls [data-panel="abilities"]').tap();
      await page.getByRole('heading', { name: 'Abilities', exact: true }).waitFor();
      await page.getByRole('button', { name: /^Jet Blast/ }).tap();
      await page.getByRole('heading', { name: 'Jet Blast', exact: true }).waitFor();
      await page.getByRole('button', { name: 'Use ability', exact: true }).tap();
      await page.locator('#target-banner').waitFor({ state: 'visible' }); await frames(page);
      unchanged(before, await state(page), 'Opening world-map aim spent a turn');
      await page.locator('#cancel-target').tap(); await page.locator('#target-banner').waitFor({ state: 'hidden' }); await settled(page);
      unchanged(before, await state(page), 'Cancelling world-map aim spent a turn');
      await shot('world-map-short-landscape-after-aim-cancel');
      const afterAimCancel = await fittedMap(page);
      await page.setViewportSize(record.viewport); await settled(page);
      const restored = await fittedMap(page); unchanged(before, await state(page), 'Restoring landscape size spent a turn');
      return { viewport: { width: 568, height: 320 }, compact, afterAimCancel, restored };
    });
    await step(record, 'remote world inspection is free and cannot teleport', () => inspectRemote(page));
    await step(record, 'west compass moves one world cell for20ticks', () => worldStep(page, -1, 10));
    await step(record, 'Enter area descends into actual Sill', async () => { const result = await enterArea(page, 'Sill'); await shot('sill'); return result; });
    await step(record, 'return to map and east to glade preserve native travel', async () => {
      const ascent = await setOut(page); check(ascent.after.position === '10, 10', 'Sill ascent returned to the wrong world cell.');
      const east = await worldStep(page, 1, 11), descent = await enterArea(page, 'Gleaners’ Glade');
      check(descent.after.position === initial.position, `Glade descent after the Sill ascent must use its native center: ${JSON.stringify({ initial, returned: descent.after })}`);
      check(await page.evaluate(() => window.__worldTravelView.scale) === localZoom, 'World-map travel reset manual local zoom.');
      await shot('returned-glade'); return { ascent, east, descent, localZoom, policy: report.nativeReturnPolicy };
    });
    const saved = await step(record, 'checkpoint saves while physically on world map', async () => {
      await setOut(page); await worldStep(page, -1, 10); await more(page);
      await page.getByRole('button', { name: /^Save checkpoint\b/ }).tap(); await page.locator('#overlay').waitFor({ state: 'hidden' }); await settled(page);
      await page.waitForFunction(() => document.getElementById('save-state').textContent.includes('Saved on this device'));
      const s = await state(page); check(s.mode === 'world' && s.position === '10, 10', 'Saved position is not Sill on the world map.');
      await shot('saved-world-map'); return s;
    });
    await step(record, 'new page Continue restores world position and supports descent', async () => {
      await page.close(); page = await context.newPage(); attach(page); await page.goto(BASE, { waitUntil: 'domcontentloaded' });
      await page.locator('#continue-button').waitFor({ state: 'visible' }); await page.locator('#continue-button').tap();
      await page.locator('#game-screen').waitFor({ state: 'visible', timeout: BOOT }); await settled(page); await observeRenderer(page);
      const restored = await state(page); unchanged(saved, restored, 'Checkpoint continuation changed world-map state');
      await fittedMap(page); await shot('continued-world-map'); const descent = await enterArea(page, 'Sill'); await shot('continued-sill');
      return { restored, descent };
    });
    await step(record, 'no browser runtime console or HTTP failures', async () => { check(record.errors.length === 0, JSON.stringify(record.errors)); return { errors: 0, requestFailures: record.requestFailures }; });
    check(!record.timedOut, 'World-travel case exceeded its time bound.'); record.status = 'passed';
  } catch (error) {
    record.status = 'failed'; record.error = error.stack || String(error); console.error(`FAIL ${record.name}: ${error.message || error}`);
    if (page && !page.isClosed()) try { await shot('failure'); } catch (captureError) { record.screenshotError = String(captureError); }
  } finally {
    clearTimeout(timer); if (context) await context.close().catch(() => {}); if (browser) await browser.close().catch(() => {});
    if (activeBrowser === browser) activeBrowser = null;
  }
}
async function main() {
  await receipt(); const stop = setTimeout(async () => {
    report.error = 'World-travel suite exceeded25minutes.'; await receipt().catch(() => {});
    if (activeBrowser) await activeBrowser.close().catch(() => {}); process.exit(2);
  }, 25 * 60_000);
  for (const [engineName, engine] of [['chromium',chromium],['webkit',webkit]])
    for (const [orientation, viewport] of [['portrait',{ width:390, height:844 }],['landscape',{ width:844, height:390 }]]) {
      const name = `${engineName}-${orientation}`;
      if (process.env.COO_SMOKE_CASE && process.env.COO_SMOKE_CASE !== name) continue;
      const record = { name, engine:engineName, viewport, status:'running', startedAt:new Date().toISOString(), steps:[], errors:[], requestFailures:[], screenshots:[] };
      report.cases.push(record); await receipt(); await run(record, engine); record.finishedAt = new Date().toISOString(); await receipt();
    }
  clearTimeout(stop); report.finishedAt = new Date().toISOString(); report.passed = report.cases.filter(c => c.status === 'passed').length;
  report.failed = report.cases.length - report.passed; await receipt();
  console.log(JSON.stringify({ cases:report.cases.length, passed:report.passed, failed:report.failed, receipt:path.join(OUT,'results.json') }));
  process.exitCode = report.failed ? 1 : 0;
}
main().catch(async error => { report.error = error.stack || String(error); await receipt().catch(() => {}); console.error(error); process.exit(1); });
