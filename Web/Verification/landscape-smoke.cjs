#!/usr/bin/env node
// Actual Blazor WASM, UI inputs, and IndexedDB. No game/runtime mocks.
// Viewport rotation and the optional padding fixture do not simulate physical iOS.
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium, webkit, devices } = require(process.env.COO_PLAYWRIGHT_PATH || '@playwright/test');
process.env.PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS = '1';
const URL = process.env.COO_SMOKE_URL || 'http://127.0.0.1:8098';
const OUT = path.resolve(process.env.COO_SMOKE_OUTPUT || path.join(__dirname, 'browser-results-landscape'));
const TIMEOUT = 20_000, BOOT = 90_000, CASE = 180_000;
const cases = [
  { width: 844, height: 390, baseline: { width: 652, height: 249 } },
  { width: 667, height: 375, baseline: { width: 475, height: 234 } },
  { width: 568, height: 320, baseline: { width: 376, height: 179 } },
];
const report = { startedAt: new Date().toISOString(), url: URL,
  scope: 'Real WASM browser acceptance with emulated viewports. Rotation and CSS padding are test fixtures, not physical iPhone or OS-safe-area evidence.', cases: [] };
const check = (ok, message) => { if (!ok) throw new Error(message); };
async function receipt() { await fs.mkdir(OUT, { recursive: true }); await fs.writeFile(path.join(OUT, 'results.json'), JSON.stringify(report, null, 2)); }
async function step(record, name, work) {
  const began = Date.now();
  try { const detail = await work(); record.steps.push({ name, status: 'passed', ms: Date.now() - began, detail }); console.log(`PASS ${record.name}: ${name}${name.includes('canvas') ? ' ' + JSON.stringify(detail) : ''}`); return detail; }
  catch (error) { record.steps.push({ name, status: 'failed', ms: Date.now() - began, error: error.stack || String(error) }); throw error; }
}
async function state(page) {
  return page.evaluate(() => ({ position: document.getElementById('position').textContent.trim(),
    tick: Number(document.getElementById('turn-number').textContent.replace('TICK ', '')),
    hp: document.getElementById('hp-text').textContent.trim(), name: document.getElementById('character-name').textContent.trim() }));
}
function same(before, after, reason) { check(JSON.stringify(before) === JSON.stringify(after), `${reason}: ${JSON.stringify({ before, after })}`); }
async function buttonFits(page, selector, inset = { left: 0, right: 0, bottom: 0 }) {
  const metrics = await page.locator(selector).evaluate((node, safe) => {
    const rect = node.getBoundingClientRect(), hit = document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2);
    const points = [[rect.x + rect.width / 2, rect.y + 1], [rect.x + rect.width / 2, rect.y + rect.height - 1],
      [rect.x + 1, rect.y + rect.height / 2], [rect.x + rect.width - 1, rect.y + rect.height / 2]];
    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height, viewportWidth: innerWidth, viewportHeight: innerHeight,
      hit: hit === node || node.contains(hit), edges: points.map(([x,y]) => { const edge = document.elementFromPoint(x,y); return edge === node || node.contains(edge); }),
      visible: !!node.getClientRects().length && getComputedStyle(node).visibility !== 'hidden', safe };
  }, inset);
  check(metrics.visible && metrics.width >= 43.5 && metrics.height >= 43.5, `${selector} is hidden or below44px: ${JSON.stringify(metrics)}`);
  check(metrics.x >= inset.left - 1 && metrics.x + metrics.width <= metrics.viewportWidth - inset.right + 1
    && metrics.y >= -1 && metrics.y + metrics.height <= metrics.viewportHeight - inset.bottom + 1,
    `${selector} is clipped: ${JSON.stringify(metrics)}`);
  check(metrics.hit, `${selector} is covered at its center: ${JSON.stringify(metrics)}`);
  check(metrics.edges.every(Boolean), `${selector} has an obscured touch edge: ${JSON.stringify(metrics)}`);
  return metrics;
}
async function narration(page) {
  const latest = (await page.locator('#log-lines p').last().textContent()).trim();
  check(latest.length > 0 && !/^\u00cd+$/.test(latest), 'Latest log is an obsolete CP437 divider instead of narration.');
  return latest;
}
async function controls(page, aiming = false, inset) {
  const selectors = ['[data-move="-1,-1"]','[data-move="0,-1"]','[data-move="1,-1"]','[data-move="-1,0"]','#wait-button',
    '[data-move="1,0"]','[data-move="-1,1"]','[data-move="0,1"]','[data-move="1,1"]'];
  if (!aiming || page.viewportSize().height > 360) selectors.push('#zoom-in','#zoom-out','#center-map');
  if (!aiming) selectors.push('#log-open');
  selectors.push(...(aiming ? ['#cancel-target','#confirm-target'] : ['#touch-controls [data-panel="nearby"]',
    '#touch-controls [data-panel="inventory"]','#touch-controls [data-panel="abilities"]','#more-button']));
  for (const selector of selectors) await buttonFits(page, selector, inset);
  return selectors.length;
}
async function geometry(page, viewport, inset = { left: 0, right: 0, bottom: 0 }, compareBaseline = true) {
  const box = await page.locator('#world-canvas').boundingBox();
  check(box, 'No map canvas.');
  const width = viewport.width - inset.left - inset.right, height = viewport.height - inset.bottom;
  check(box.width >= width * .70, `Map width ${box.width} is below70% of available ${width}.`);
  check(box.height >= height * .90, `Map height ${box.height} is below90% of available ${height}.`);
  check(box.x >= inset.left - 1 && box.x + box.width <= viewport.width - inset.right + 1
    && box.y >= -1 && box.y + box.height <= viewport.height - inset.bottom + 1, 'Canvas exceeds safe viewport.');
  const gain = box.width * box.height / (viewport.baseline.width * viewport.baseline.height);
  if (compareBaseline) check(gain >= 1.35, `Canvas area gain is only ${gain.toFixed(3)}× baseline.`);
  const overflow = await page.evaluate(() => ({ width: innerWidth, height: innerHeight,
    docWidth: document.documentElement.scrollWidth, docHeight: document.documentElement.scrollHeight }));
  check(overflow.docWidth <= overflow.width + 1 && overflow.docHeight <= overflow.height + 1,
    `Gameplay document overflows: ${JSON.stringify(overflow)}`);
  return { canvas: box, availableWidth: width, availableHeight: height, baseline: viewport.baseline, areaMultiplier: gain, overflow };
}
async function hud(page) {
  const result = {};
  for (const id of ['mobile-hp','mobile-focus','landscape-save-state','location']) {
    const locator = page.locator('#' + id); check(await locator.isVisible(), `${id} is hidden.`);
    const text = (await locator.textContent()).trim(), box = await locator.boundingBox();
    check(text.length > 0, `${id} is empty.`);
    result[id] = { text, box };
  }
  check(/\d+\s*\/\s*\d+/.test(result['mobile-hp'].text), 'Rail does not show current/max HP.');
  check(/\d/.test(result['mobile-focus'].text), 'Rail does not show focus.');
  return result;
}
async function closeMenu(page) {
  if (await page.locator('#overlay').isVisible()) { await page.locator('#close-overlay').tap(); await page.locator('#overlay').waitFor({ state: 'hidden' }); }
}
async function menu(page, title, open, mustScroll = false) {
  const before = await state(page); await open();
  await page.locator('#overlay-title').filter({ hasText: title }).waitFor();
  await buttonFits(page, '#close-overlay');
  const scroll = await page.locator('#overlay-body').evaluate(body => {
    const before = body.scrollTop; body.scrollTop = body.scrollHeight;
    return { width: body.clientWidth, scrollWidth: body.scrollWidth, height: body.clientHeight,
      scrollHeight: body.scrollHeight, before, after: body.scrollTop };
  });
  check(scroll.scrollWidth <= scroll.width + 1, `${title} has horizontal content overflow.`);
  if (mustScroll) check(scroll.scrollHeight > scroll.height && scroll.after > 0, `${title} cannot scroll to its lower content.`);
  await buttonFits(page, '#close-overlay'); same(before, await state(page), `${title} reading/scrolling spent a turn`);
  await closeMenu(page); return scroll;
}
async function more(page) { await page.locator('#more-button').tap(); await page.locator('#overlay-title').filter({ hasText: 'Your expedition' }).waitFor(); }
async function aim(page) {
  await page.locator('#touch-controls [data-panel="abilities"]').tap();
  await page.locator('#overlay-title').filter({ hasText: 'Abilities' }).waitFor();
  await page.getByRole('button', { name: /^Jet Blast/ }).tap();
  await page.locator('#target-banner').waitFor({ state: 'visible' });
  await page.locator('[data-move="1,0"]').tap();
  await page.waitForFunction(() => !document.getElementById('confirm-target').disabled);
}
async function observeZoom(page) {
  // Read-only renderer instrumentation: original draw executes unchanged. This
  // observes manual zoom/rotation without inventing any simulation state.
  await page.evaluate(async () => {
    const { WorldMapView } = await import('./map.js');
    const original = WorldMapView.prototype.draw;
    WorldMapView.prototype.draw = function (...args) {
      const result = original.apply(this, args);
      if (this.canvas.id === 'world-canvas') window.__landscapeZoom = this.scale;
      return result;
    };
  });
  await page.locator('#center-map').tap();
}
async function run(record, engine, viewport, rotate) {
  let browser, context, page, timer;
  const shot = async suffix => { const file = `${record.name}-${suffix}.png`; await page.screenshot({ path: path.join(OUT, file), timeout: 10_000 }); record.screenshots.push(file); };
  try {
    browser = await engine.launch({ headless: true, timeout: 30_000 });
    timer = setTimeout(() => { record.timedOut = true; browser.close().catch(() => {}); }, CASE);
    const firstViewport = rotate ? { width: 390, height: 844 } : { width: viewport.width, height: viewport.height };
    context = await browser.newContext({ ...devices['iPhone 13'], viewport: firstViewport, screen: firstViewport,
      isMobile: true, hasTouch: true, deviceScaleFactor: 2 });
    const attach = p => {
      p.setDefaultTimeout(TIMEOUT); p.setDefaultNavigationTimeout(30_000);
      p.on('pageerror', e => record.errors.push({ kind: 'page', message: e.stack || String(e) }));
      p.on('console', m => { if (m.type() === 'error') record.errors.push({ kind: 'console', message: m.text() }); });
      p.on('response', r => { if (r.url().startsWith(URL) && r.status() >= 400) record.errors.push({ kind: 'http', status: r.status(), url: r.url() }); });
    };
    page = await context.newPage(); attach(page);
    await step(record, 'real seeded Classic expedition boots and saves', async () => {
      await page.goto(URL, { waitUntil: 'domcontentloaded' }); await page.locator('#new-button').tap();
      await page.locator('#builds .build-card').first().waitFor();
      await page.locator('#player-name').fill(`Landscape ${record.name}`.slice(0,32)); await page.locator('#world-seed').fill('8128');
      await page.locator('#enter-world').tap(); await page.locator('#game-screen').waitFor({ state: 'visible', timeout: BOOT });
      await page.locator('#loading-screen').waitFor({ state: 'hidden', timeout: BOOT });
      await page.waitForFunction(() => document.getElementById('save-state').textContent.includes('Saved on this device'), null, { timeout: BOOT });
      record.release = await page.evaluate(async () => (await (await fetch('sw.js', { cache: 'no-store' })).text()).match(/ooo-static-([a-f0-9]+)/)?.[1] || 'unknown');
      return state(page);
    });
    if (rotate) await step(record, 'portrait zoom survives landscape and portrait rotation without a turn', async () => {
      const before = await state(page); await observeZoom(page);
      const initial = await page.evaluate(() => window.__landscapeZoom);
      await page.locator('#zoom-in').tap(); const zoomed = await page.evaluate(() => window.__landscapeZoom);
      check(zoomed > initial, 'Manual zoom did not change the renderer.');
      const readings = [];
      for (const size of [{ width: viewport.width, height: viewport.height }, { width: 390, height: 844 }, { width: viewport.width, height: viewport.height }]) {
        await page.setViewportSize(size); await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const actual = await page.evaluate(() => window.__landscapeZoom); check(actual === zoomed, `Rotation reset zoom ${zoomed} to ${actual}.`);
        same(before, await state(page), 'Rotation changed gameplay'); readings.push({ viewport: size, zoom: actual });
        if (size.width < size.height) await shot('rotated-portrait');
      }
      return { initial, zoomed, readings };
    });
    await step(record, 'canvas is substantially larger and uses the available landscape height', async () => {
      const metrics = await geometry(page, viewport); await shot('map'); return metrics;
    });
    await step(record, 'health focus save status and all primary controls stay visible', async () => ({ hud: await hud(page), controls: await controls(page) }));
    await step(record, 'Pack Abilities Skills Journal and More remain free and scrollable', async () => {
      const results = {};
      results.pack = await menu(page, 'Pack & equipment', () => page.locator('#touch-controls [data-panel="inventory"]').tap(), true);
      results.abilities = await menu(page, 'Abilities', () => page.locator('#touch-controls [data-panel="abilities"]').tap(), true);
      results.skills = await menu(page, 'Skills & attributes', async () => { await more(page); await page.getByRole('button', { name: /^Skills & attributes/ }).tap(); }, true);
      results.journal = await menu(page, 'Field journal', () => page.locator('#log-open').tap());
      results.more = await menu(page, 'Your expedition', () => page.locator('#more-button').tap(), true);
      return results;
    });
    await step(record, 'aim and cancel preserve a turn with accessible confirmation controls', async () => {
      const before = await state(page); await aim(page); await controls(page, true);
      check(!await page.locator('#more-button').isVisible(), 'Aiming did not replace the action shortcuts.');
      same(before, await state(page), 'Aiming spent a turn'); await shot('aiming');
      await page.locator('#cancel-target').tap(); await page.locator('#target-banner').waitFor({ state: 'hidden' });
      same(before, await state(page), 'Cancelling aim spent a turn'); await controls(page); return before;
    });
    if (record.engine === 'webkit' && viewport.width === 844) await step(record, 'short landscape safe-area fixture preserves complete44px aim controls', async () => {
      const before = await state(page), readings = [];
      const style = await page.addStyleTag({ content: 'body.playing #app{padding-bottom:21px!important}' });
      try {
        for (const height of [320,315,301]) {
          await page.setViewportSize({ width: 844, height });
          await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
          await controls(page, false, { left: 0, right: 0, bottom: 21 });
          await aim(page); await controls(page, true, { left: 0, right: 0, bottom: 21 });
          await shot(`simulated-bottom21-height${height}-aiming`);
          readings.push({ height, target: await page.locator('#target-banner').boundingBox(), south: await page.locator('[data-move="0,1"]').boundingBox() });
          await page.locator('#cancel-target').tap(); await page.locator('#target-banner').waitFor({ state: 'hidden' });
          same(before, await state(page), 'Safe-area aiming or cancellation spent a turn');
        }
        return { fixture: 'CSS bottom padding only, not actual OS safe-area values.', readings };
      } finally {
        await style.evaluate(node => node.remove()); await page.setViewportSize({ width: viewport.width, height: viewport.height });
        await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
      }
    });
    await step(record, 'confirm casts real Jet Blast eastward and spends exactly one turn', async () => {
      const before = await state(page); await aim(page); await page.locator('#confirm-target').tap();
      await page.locator('#target-banner').waitFor({ state: 'hidden' });
      const after = await state(page); check(after.tick === before.tick + 10, `Cast turn differs: ${JSON.stringify({ before, after })}`);
      check(after.position === before.position && after.hp === before.hp, 'The controlled eastward ground cast moved or harmed the caster.');
      await hud(page); return { before, after, narration: await narration(page) };
    });
    await step(record, 'real compass movement remains available beside the larger map', async () => {
      const before = await state(page); await page.locator('[data-move="1,0"]').tap();
      await page.waitForFunction(previous => document.getElementById('position').textContent.trim() !== previous, before.position);
      const after = await state(page); check(after.tick > before.tick, 'Movement did not advance turns.'); return { before, after, narration: await narration(page) };
    });
    const saved = await step(record, 'checkpoint saves from the landscape menu', async () => {
      await more(page); await page.getByRole('button', { name: /^Save checkpoint/ }).tap();
      await page.locator('#overlay').waitFor({ state: 'hidden' });
      await page.waitForFunction(() => document.getElementById('landscape-save-state').textContent.includes('Saved'), null, { timeout: TIMEOUT });
      await shot('saved'); return state(page);
    });
    await step(record, 'fresh page restores the same character position tick and HP', async () => {
      await page.close(); page = await context.newPage(); attach(page); await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto(URL, { waitUntil: 'domcontentloaded' }); await page.locator('#continue-button').waitFor({ state: 'visible' });
      await page.locator('#continue-button').tap(); await page.locator('#game-screen').waitFor({ state: 'visible', timeout: BOOT });
      await page.locator('#loading-screen').waitFor({ state: 'hidden', timeout: BOOT });
      const restored = await state(page); same(saved, restored, 'Fresh continuation differs');
      await geometry(page, viewport); await controls(page); await hud(page); await narration(page); await shot('restored'); return restored;
    });
    if (record.engine === 'webkit' && viewport.width === 844) await step(record, 'simulated47px side and21px bottom padding keeps controls accessible', async () => {
      const before = await state(page);
      const style = await page.addStyleTag({ content: 'body.playing #app{padding-left:47px!important;padding-right:47px!important;padding-bottom:21px!important}' });
      try {
        await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const inset = { left: 47, right: 47, bottom: 21 }, metrics = await geometry(page, viewport, inset, false);
        await controls(page, false, inset); same(before, await state(page), 'Padding fixture changed gameplay'); await shot('simulated-insets');
        return { fixture: 'CSS app padding only; not actual OS safe-area environment values.', insets: inset, metrics };
      } finally { await style.evaluate(node => node.remove()); }
    });
    await step(record, 'no runtime console or HTTP errors', async () => { check(record.errors.length === 0, JSON.stringify(record.errors)); return { errors: 0 }; });
    check(!record.timedOut, 'Case exceeded its180-second bound.'); record.status = 'passed';
  } catch (error) {
    record.status = 'failed'; record.error = error.stack || String(error); console.error(`FAIL ${record.name}: ${error.message || error}`);
    if (page && !page.isClosed()) try { await shot('failure'); } catch (_) {}
  } finally { clearTimeout(timer); if (context) await context.close().catch(() => {}); if (browser) await browser.close().catch(() => {}); }
}
async function main() {
  await receipt(); const stop = setTimeout(() => { console.error('Landscape suite exceeded20minutes.'); process.exit(2); }, 20 * 60_000);
  for (const [name, engine] of [['chromium',chromium],['webkit',webkit]]) for (const viewport of cases) {
    if (process.env.COO_SMOKE_CASE && process.env.COO_SMOKE_CASE !== `${name}-${viewport.width}x${viewport.height}`) continue;
    const record = { name: `${name}-${viewport.width}x${viewport.height}`, engine: name, viewport,
      status: 'running', startedAt: new Date().toISOString(), steps: [], errors: [], screenshots: [] };
    report.cases.push(record); await receipt(); await run(record, engine, viewport, name === 'webkit' && viewport.width === 844);
    record.finishedAt = new Date().toISOString(); await receipt();
  }
  clearTimeout(stop); report.finishedAt = new Date().toISOString(); report.passed = report.cases.filter(c => c.status === 'passed').length;
  report.failed = report.cases.length - report.passed; await receipt();
  console.log(JSON.stringify({ passed: report.passed, failed: report.failed, receipt: path.join(OUT, 'results.json') })); process.exitCode = report.failed ? 1 : 0;
}
main().catch(async error => { report.error = error.stack || String(error); await receipt().catch(() => {}); console.error(error); process.exit(1); });
