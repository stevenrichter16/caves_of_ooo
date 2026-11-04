#!/usr/bin/env node
// Real browser/WASM acceptance. Requires the final published wwwroot to be
// served; no engine, DOM, storage, or response mocks are installed.
// This emulates iPhone viewports, not physical iPhone hardware/performance.
const fs = require('node:fs/promises');
const path = require('node:path');
const { gunzipSync } = require('node:zlib');
const { chromium, webkit, devices } = require(process.env.COO_PLAYWRIGHT_PATH || '@playwright/test');

process.env.PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS = '1';
const BASE_URL = process.env.COO_SMOKE_URL || 'http://127.0.0.1:8098';
const OUTPUT = path.resolve(process.env.COO_SMOKE_OUTPUT || path.join(__dirname, 'browser-results'));
const CASE_TIMEOUT = 180_000;
const START_TIMEOUT = 90_000;
const STEP_TIMEOUT = 20_000;
const suite = { baseUrl: BASE_URL, startedAt: new Date().toISOString(), scope: 'Real Blazor WASM; mobile viewport emulation, not physical iPhone.', cases: [] };
let activeBrowser = null;

function check(condition, message) { if (!condition) throw new Error(message); }
async function receipt() {
  await fs.mkdir(OUTPUT, { recursive: true });
  await fs.writeFile(path.join(OUTPUT, 'results.json'), JSON.stringify(suite, null, 2));
}
async function step(record, name, work) {
  const start = Date.now();
  try {
    const detail = await work();
    record.steps.push({ name, status: 'passed', durationMs: Date.now() - start, ...(detail === undefined ? {} : { detail }) });
    console.log(`PASS ${record.name}: ${name}`);
    return detail;
  } catch (error) {
    record.steps.push({ name, status: 'failed', durationMs: Date.now() - start, error: error.stack || String(error) });
    throw error;
  }
}
async function stateFromUi(page) {
  return page.evaluate(() => ({
    position: document.getElementById('position').textContent.trim(),
    tick: document.getElementById('turn-number').textContent.trim(),
    hp: document.getElementById('hp-text').textContent.trim(),
    name: document.getElementById('character-name').textContent.trim(),
  }));
}
async function overflow(page, screen) {
  const metrics = await page.evaluate(() => {
    const d = document.documentElement, b = document.body, modal = document.getElementById('overlay');
    const result = { viewport: innerWidth, document: d.scrollWidth, body: b.scrollWidth };
    if (!modal.hidden) {
      const sheet = modal.querySelector('.sheet').getBoundingClientRect(), content = document.getElementById('overlay-body');
      result.sheet = { left: sheet.left, right: sheet.right, top: sheet.top, bottom: sheet.bottom, viewportHeight: innerHeight };
      result.content = { width: content.clientWidth, scrollWidth: content.scrollWidth };
    }
    return result;
  });
  check(metrics.document <= metrics.viewport + 1 && metrics.body <= metrics.viewport + 1, `${screen} has horizontal page overflow: ${JSON.stringify(metrics)}`);
  if (metrics.sheet) {
    check(metrics.sheet.left >= -1 && metrics.sheet.right <= metrics.viewport + 1, `${screen} sheet exceeds viewport width.`);
    check(metrics.sheet.top >= -1 && metrics.sheet.bottom <= metrics.sheet.viewportHeight + 1, `${screen} sheet exceeds viewport height.`);
    check(metrics.content.scrollWidth <= metrics.content.width + 1, `${screen} content has horizontal overflow.`);
  }
  return metrics;
}
async function closeMenu(page) {
  if (await page.locator('#overlay').isVisible()) {
    await page.locator('#close-overlay').tap();
    await page.locator('#overlay').waitFor({ state: 'hidden', timeout: STEP_TIMEOUT });
  }
}
async function openExpeditionMenu(page) {
  await page.locator('#more-button').tap();
  await page.locator('#overlay-title').filter({ hasText: 'Your expedition' }).waitFor({ timeout: STEP_TIMEOUT });
}
function attachDiagnostics(page, record) {
  page.on('pageerror', error => record.pageErrors.push(error.stack || String(error)));
  page.on('console', message => {
    if (message.type() === 'error') record.consoleErrors.push(message.text());
  });
  page.on('requestfailed', request => record.requestFailures.push({ url: request.url(), error: request.failure()?.errorText }));
  page.on('response', response => {
    if (response.url().startsWith(BASE_URL) && response.status() >= 400)
      record.httpErrors.push({ status: response.status(), url: response.url() });
  });
}

async function exercise(record, engine, viewport) {
  let browser, context, page;
  const screenshots = async suffix => {
    const filename = `${record.name}-${suffix}.png`;
    await page.screenshot({ path: path.join(OUTPUT, filename), fullPage: !await page.locator('#game-screen').isVisible(), timeout: 10_000 });
    record.screenshots.push(filename);
  };
  try {
    browser = await engine.launch({ headless: true, timeout: 30_000 });
    activeBrowser = browser;
    context = await browser.newContext({
      ...devices['iPhone 13'], viewport, screen: viewport,
      isMobile: true, hasTouch: true, deviceScaleFactor: 2,
      acceptDownloads: true,
    });
    page = await context.newPage();
    page.setDefaultTimeout(STEP_TIMEOUT); page.setDefaultNavigationTimeout(30_000);
    attachDiagnostics(page, record);

    await step(record, 'start page loads without horizontal overflow', async () => {
      await page.goto(BASE_URL, { waitUntil: 'domcontentloaded' });
      await page.locator('#new-button').waitFor({ state: 'visible' });
      await page.waitForFunction(() => !document.getElementById('startup-status').textContent.includes('Opening your local'), null, { timeout: STEP_TIMEOUT });
      check(await page.locator('#new-button').isEnabled(), 'Storage startup disabled new expeditions.');
      const metrics = await overflow(page, 'Start page'); await screenshots('start'); return metrics;
    });

    const character = `Smoke ${record.name}`;
    await step(record, 'create a real seeded expedition through the phone UI', async () => {
      await page.locator('#new-button').tap();
      await page.locator('#builds .build-card').first().waitFor();
      await overflow(page, 'Build selection');
      await page.locator('#player-name').fill(character);
      await page.locator('#world-seed').fill('8128');
      await page.locator('#enter-world').tap();
      await page.locator('#game-screen').waitFor({ state: 'visible', timeout: START_TIMEOUT });
      await page.locator('#loading-screen').waitFor({ state: 'hidden', timeout: START_TIMEOUT });
      await page.waitForFunction(() => document.getElementById('save-state').textContent.includes('Saved on this device'), null, { timeout: START_TIMEOUT });
      const initial = await stateFromUi(page);
      check(initial.name === character, `Unexpected loaded character: ${initial.name}`);
      check(/^\d+, \d+$/.test(initial.position), `Missing real player position: ${initial.position}`);
      check(/^TICK \d+$/.test(initial.tick), `Missing real scheduler tick: ${initial.tick}`);
      check((await page.locator('#world-canvas').evaluate(canvas => canvas.width > 0 && canvas.height > 0)), 'Map canvas did not initialize.');
      await screenshots('game'); return initial;
    });

    await step(record, 'game viewport and compass remain usable', async () => {
      const metrics = await overflow(page, 'Game');
      for (const locator of [page.locator('#wait-button'), page.locator('[data-move="1,0"]'), page.locator('#more-button')]) {
        const box = await locator.boundingBox();
        check(box && box.width >= 43.5 && box.height >= 43.5, 'Primary touch control is smaller than 44 CSS pixels.');
        check(box.x >= -1 && box.x + box.width <= viewport.width + 1 && box.y >= -1 && box.y + box.height <= viewport.height + 1,
          `Primary control is outside the viewport: ${JSON.stringify(box)}`);
      }
      return metrics;
    });

    await step(record, 'map inspection is free', async () => {
      const before = await stateFromUi(page);
      await page.locator('#center-map').tap();
      const canvas = page.locator('#world-canvas'), box = await canvas.boundingBox();
      check(box && box.width > 0 && box.height > 0, 'Map has no visible area.');
      await canvas.tap({ position: { x: box.width / 2, y: box.height / 2 } });
      await page.locator('#overlay').waitFor({ state: 'visible' });
      check((await page.locator('#overlay-kicker').textContent()).includes('LOOKING TAKES NO TIME'), 'Map tap did not open the real inspection view.');
      const after = await stateFromUi(page);
      check(before.tick === after.tick && before.position === after.position && before.hp === after.hp, 'Inspecting the map spent a turn or changed the player.');
      await overflow(page, 'Inspection'); await screenshots('inspection'); await closeMenu(page);
    });

    await step(record, 'pack inspection is free and fits the phone', async () => {
      const before = await stateFromUi(page);
      await page.locator('#touch-controls [data-panel="inventory"]').tap();
      await page.locator('#overlay-title').filter({ hasText: 'Pack & equipment' }).waitFor();
      check(await page.locator('#overlay-body .row').count() > 0, 'Starting inventory is empty.');
      await page.locator('#overlay-body .row').first().tap();
      await page.locator('#overlay-kicker').filter({ hasText: 'IN YOUR HANDS' }).waitFor();
      check((await stateFromUi(page)).tick === before.tick, 'Reading the pack spent a turn.');
      await overflow(page, 'Item inspection'); await closeMenu(page);
    });

    await step(record, 'compass movement advances the real game', async () => {
      const before = await stateFromUi(page);
      let after = before;
      for (const direction of ['1,0', '0,1', '-1,0', '0,-1', '1,1', '-1,1', '1,-1', '-1,-1']) {
        await closeMenu(page);
        await page.locator(`[data-move="${direction}"]`).tap();
        try {
          await page.waitForFunction(previous => document.getElementById('position').textContent.trim() !== previous,
            before.position, { timeout: 2_000 });
        } catch (_) { /* A wall or an opened door is a valid blocked step; try another direction. */ }
        after = await stateFromUi(page);
        if (after.position !== before.position) break;
      }
      check(after.position !== before.position, `No adjacent compass step moved the player from ${before.position}.`);
      check(Number(after.tick.replace('TICK ', '')) > Number(before.tick.replace('TICK ', '')), 'Movement did not advance the scheduler.');
      await page.waitForFunction(() => !document.getElementById('save-state').textContent.startsWith('Saving'), null, { timeout: STEP_TIMEOUT });
      return { before, after };
    });

    const saved = await step(record, 'explicit checkpoint reaches browser storage', async () => {
      await openExpeditionMenu(page);
      await page.getByRole('button', { name: /^Save checkpoint/ }).tap();
      await page.locator('#overlay').waitFor({ state: 'hidden' });
      await page.waitForFunction(() => document.getElementById('save-state').textContent.includes('Saved on this device'), null, { timeout: STEP_TIMEOUT });
      const saved = await stateFromUi(page); await screenshots('saved'); return saved;
    });

    await step(record, 'fresh page restores exact position, tick, HP, and character', async () => {
      await page.close();
      page = await context.newPage(); page.setDefaultTimeout(STEP_TIMEOUT); page.setDefaultNavigationTimeout(30_000);
      attachDiagnostics(page, record);
      await page.goto(BASE_URL, { waitUntil: 'domcontentloaded' });
      await page.locator('#continue-button').waitFor({ state: 'visible', timeout: STEP_TIMEOUT });
      await page.locator('#continue-button').tap();
      await page.locator('#game-screen').waitFor({ state: 'visible', timeout: START_TIMEOUT });
      await page.locator('#loading-screen').waitFor({ state: 'hidden', timeout: START_TIMEOUT });
      const restored = await stateFromUi(page);
      check(JSON.stringify(restored) === JSON.stringify(saved), `Restored state differs: ${JSON.stringify({ saved, restored })}`);
      await overflow(page, 'Restored game'); await screenshots('restored'); return restored;
    });

    await step(record, 'export downloads the actual compressed checkpoint', async () => {
      await openExpeditionMenu(page);
      const pending = page.waitForEvent('download', { timeout: STEP_TIMEOUT });
      await page.getByRole('button', { name: /^Export checkpoint/ }).tap();
      const download = await pending;
      const target = path.join(OUTPUT, `${record.name}-checkpoint.json`);
      await download.saveAs(target);
      check(!await download.failure(), 'Browser failed to download the backup.');
      const backup = JSON.parse(await fs.readFile(target, 'utf8'));
      check(backup.format === 'caves-of-ooo-backup' && backup.version === 1 && backup.checkpoints.length === 1, 'Unexpected backup envelope.');
      const checkpoint = backup.checkpoints[0], info = JSON.parse(checkpoint.metadata);
      const raw = gunzipSync(Buffer.from(checkpoint.payloadBase64, 'base64'), { maxOutputLength: 128 * 1024 * 1024 });
      check(raw.length > 100 && raw.readUInt32LE(0) === 0x304f4f43 && raw.readInt32LE(4) === 8, 'Export does not contain the real save graph/header.');
      check(info.PlayerName === saved.name && `TICK ${info.Tick}` === saved.tick, 'Export metadata differs from the saved expedition.');
      record.backup = path.basename(target);
      await closeMenu(page);
      return { compressedBytes: Buffer.from(checkpoint.payloadBase64, 'base64').length, rawBytes: raw.length, buildId: backup.buildId };
    });

    await step(record, 'offline fresh page continues the acknowledged checkpoint', async () => {
      await page.waitForFunction(() => 'serviceWorker' in navigator && !!navigator.serviceWorker.controller,
        null, { timeout: STEP_TIMEOUT });
      await page.evaluate(() => navigator.serviceWorker.ready.then(registration => !!registration.active));
      await context.setOffline(true);
      try {
        await page.reload({ waitUntil: 'domcontentloaded' });
        await page.locator('#continue-button').waitFor({ state: 'visible', timeout: STEP_TIMEOUT });
        await page.locator('#continue-button').tap();
        await page.locator('#game-screen').waitFor({ state: 'visible', timeout: START_TIMEOUT });
        await page.locator('#loading-screen').waitFor({ state: 'hidden', timeout: START_TIMEOUT });
        const restored = await stateFromUi(page);
        check(JSON.stringify(restored) === JSON.stringify(saved), `Offline restore differs: ${JSON.stringify({ saved, restored })}`);
        await overflow(page, 'Offline game'); await screenshots('offline');
        return restored;
      } finally { await context.setOffline(false); }
    });

    // One clean-profile import per browser engine bounds runtime while proving
    // the exported file restores through the real import and graph-validation UI.
    if (viewport.width < viewport.height) await step(record, 'backup imports into a clean browser profile', async () => {
      const importContext = await browser.newContext({ ...devices['iPhone 13'], viewport, screen: viewport,
        isMobile: true, hasTouch: true, deviceScaleFactor: 2, acceptDownloads: true });
      try {
        const importedPage = await importContext.newPage(); importedPage.setDefaultTimeout(STEP_TIMEOUT);
        importedPage.setDefaultNavigationTimeout(30_000); attachDiagnostics(importedPage, record);
        await importedPage.goto(BASE_URL, { waitUntil: 'domcontentloaded' });
        await importedPage.waitForFunction(() => !document.getElementById('startup-status').textContent.includes('Opening your local'),
          null, { timeout: STEP_TIMEOUT });
        check(!await importedPage.locator('#continue-button').isVisible(), 'The import profile already contains a checkpoint.');
        await importedPage.locator('#import-file').setInputFiles(path.join(OUTPUT, record.backup));
        await importedPage.waitForFunction(() => document.getElementById('startup-status').textContent.includes('Checkpoint imported'),
          null, { timeout: START_TIMEOUT });
        await importedPage.locator('#continue-button').tap();
        await importedPage.locator('#game-screen').waitFor({ state: 'visible', timeout: START_TIMEOUT });
        await importedPage.locator('#loading-screen').waitFor({ state: 'hidden', timeout: START_TIMEOUT });
        const restored = await stateFromUi(importedPage);
        check(JSON.stringify(restored) === JSON.stringify(saved), `Imported restore differs: ${JSON.stringify({ saved, restored })}`);
        await overflow(importedPage, 'Imported game');
        const filename = `${record.name}-imported.png`;
        await importedPage.screenshot({ path: path.join(OUTPUT, filename), timeout: 10_000 }); record.screenshots.push(filename);
        return restored;
      } finally { await importContext.close(); }
    });

    await step(record, 'browser reports no runtime or HTTP errors', async () => {
      check(record.pageErrors.length === 0, `Runtime errors: ${record.pageErrors.join('\n')}`);
      check(record.httpErrors.length === 0, `HTTP errors: ${JSON.stringify(record.httpErrors)}`);
      check(record.consoleErrors.length === 0, `Console errors: ${record.consoleErrors.join('\n')}`);
      return { pageErrors: 0, consoleErrors: 0, httpErrors: 0, requestFailures: record.requestFailures };
    });
    record.status = 'passed';
  } catch (error) {
    record.status = 'failed'; record.error = error.stack || String(error);
    if (page && !page.isClosed()) {
      try { await screenshots('failure'); } catch (captureError) { record.screenshotError = String(captureError); }
    }
    console.error(`FAIL ${record.name}: ${error.message || error}`);
  } finally {
    if (context) await context.close().catch(() => {});
    if (browser) await browser.close().catch(() => {});
    if (activeBrowser === browser) activeBrowser = null;
  }
}

async function main() {
  await fs.mkdir(OUTPUT, { recursive: true });
  const kill = setTimeout(async () => {
    suite.error = 'The 13-minute suite limit was exceeded.';
    await receipt().catch(() => {});
    if (activeBrowser) await activeBrowser.close().catch(() => {});
    process.exit(2);
  }, 13 * 60_000);
  for (const [engineName, engine] of [['chromium', chromium], ['webkit', webkit]]) {
    for (const [orientation, viewport] of [['portrait', { width: 390, height: 844 }], ['landscape', { width: 844, height: 390 }]]) {
      const record = { name: `${engineName}-${orientation}`, engine: engineName, viewport, status: 'running',
        startedAt: new Date().toISOString(), steps: [], screenshots: [], pageErrors: [], consoleErrors: [], httpErrors: [], requestFailures: [] };
      suite.cases.push(record); await receipt();
      let timeout;
      await Promise.race([
        exercise(record, engine, viewport),
        new Promise(resolve => { timeout = setTimeout(async () => {
          record.status = 'failed'; record.error = `Case exceeded ${CASE_TIMEOUT / 1000} seconds.`;
          if (activeBrowser) await activeBrowser.close().catch(() => {});
          resolve();
        }, CASE_TIMEOUT); }),
      ]);
      clearTimeout(timeout); record.finishedAt = new Date().toISOString(); await receipt();
    }
  }
  clearTimeout(kill); suite.finishedAt = new Date().toISOString();
  suite.passed = suite.cases.filter(item => item.status === 'passed').length;
  suite.failed = suite.cases.length - suite.passed; await receipt();
  console.log(JSON.stringify({ total: suite.cases.length, passed: suite.passed, failed: suite.failed, results: path.join(OUTPUT, 'results.json') }));
  process.exitCode = suite.failed ? 1 : 0;
}
main().catch(async error => { suite.error = error.stack || String(error); await receipt().catch(() => {}); console.error(error); process.exit(1); });
