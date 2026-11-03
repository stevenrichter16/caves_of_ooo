const { test, expect } = require('@playwright/test');
const { gzipSync } = require('node:zlib');

async function launch(page) {
  // These tests qualify the shell, never Unity's renderer/WebGL support.
  await page.addInitScript(() => {
    const original = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function(type, ...args) { return type === 'webgl2' ? {} : original.call(this, type, ...args); };
  });
  await page.goto('/');
  await page.getByRole('button', { name: 'Begin expedition' }).click();
  await expect(page.locator('#launch')).toBeHidden();
}
const calls = page => page.evaluate(() => window.fixture.calls.filter(call => call.method === 'PhoneIntent').map(call => JSON.parse(call.value)));

test('visible primary controls fit the phone and have 44px touch targets', async ({ page }) => {
  await launch(page);
  const dimensions = await page.evaluate(() => ({ width: innerWidth, scroll: document.documentElement.scrollWidth,
    buttons: [...document.querySelectorAll('#controls button')].filter(button => button.getClientRects().length)
      .map(button => ({ label: button.textContent, width: button.getBoundingClientRect().width, height: button.getBoundingClientRect().height })) }));
  expect(dimensions.scroll).toBeLessThanOrEqual(dimensions.width);
  for (const button of dimensions.buttons) { expect(button.width, button.label).toBeGreaterThanOrEqual(44); expect(button.height, button.label).toBeGreaterThanOrEqual(44); }
});

test('a tap emits one step; a second tap cannot queue behind an unacknowledged action', async ({ page }) => {
  await launch(page);
  await page.getByRole('button', { name: 'North', exact: true }).tap();
  await page.getByRole('button', { name: 'North', exact: true }).tap();
  expect(await calls(page)).toEqual([{ kind: 'step', x: 0, y: -1, revision: 1 }]);
  await page.evaluate(() => window.fixture.view({ revision: 2 }));
  await page.getByRole('button', { name: 'East', exact: true }).tap();
  expect((await calls(page))[1]).toEqual({ kind: 'step', x: 1, y: 0, revision: 2 });
});

test('touching the map requests free inspection, never a movement command', async ({ page }) => {
  await launch(page);
  await page.locator('#map-touch').tap({ position: { x: 40, y: 40 } });
  const sent = await calls(page);
  expect(sent).toHaveLength(1); expect(sent[0].kind).toBe('inspect');
  expect(sent[0].screenX).toBeGreaterThan(0); expect(sent[0].screenX).toBeLessThan(1);
});

test('menu text reflows at 16px and uses the current modal revision', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => window.fixture.view({ revision: 3, mode: 'Menu', title: 'InventoryOpen', rows: [
    { text: '> A useful affliction: compound eyes reveal trails between the orchard and the distant forest.', color: '#e7c878' },
    { text: '[E] Equip  [D] Drop  [Esc] Back', color: '#c0c0c0' }
  ] }));
  await expect(page.locator('#reader')).toBeVisible(); await expect(page.locator('#controls')).toBeHidden();
  expect(await page.locator('#reader-content p').first().evaluate(el => parseFloat(getComputedStyle(el).fontSize))).toBeGreaterThanOrEqual(16);
  await page.locator('#menu-shortcuts button[data-key=E]').tap();
  expect(await calls(page)).toEqual([{ kind: 'key', value: 'E', revision: 3 }]);
});

test('loadout naming uses a text input and one explicit submission', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => window.fixture.view({ revision: 5, mode: 'Menu', title: 'InventoryOpen', naming: true, name: 'Loadout 1', rows: [] }));
  await page.getByLabel('Loadout name', { exact: true }).fill('Moth eyes');
  expect(await calls(page)).toHaveLength(0);
  await page.getByRole('button', { name: 'Save name', exact: true }).tap();
  expect(await calls(page)).toEqual([{ kind: 'text', value: 'Moth eyes', revision: 5 }]);
});

test('saved status waits for acknowledgment and failures remain visible', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'saving' } })));
  await expect(page.locator('#save-status')).toHaveText('Saving…');
  await page.evaluate(() => window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'error', message: 'Quota exceeded' } })));
  await expect(page.locator('#save-status')).toContainText('Quota exceeded');
});

test('real IndexedDB survives reload and rejects another tab revision', async ({ page }) => {
  await launch(page);
  await page.evaluate(async () => {
    const first = new CooBrowserSaveStore(); await first.hydrate();
    const info = JSON.stringify({ GameID: 'browser-test', SaveVersion: 8 });
    await first.commit('browser-test', 'Quick', new Uint8Array([31, 139, 8, 1]), info);
    const second = new CooBrowserSaveStore(); await second.hydrate();
    await first.commit('browser-test', 'Quick', new Uint8Array([31, 139, 8, 2]), info);
    try { await second.commit('browser-test', 'Quick', new Uint8Array([31, 139, 8, 3]), info); throw new Error('Conflict was accepted'); }
    catch(error) { if (!error.message.includes('Another tab')) throw error; }
  });
  await page.reload();
  const bytes = await page.evaluate(async () => {
    const store = new CooBrowserSaveStore(); const [saved] = await store.hydrate(); return Array.from(new Uint8Array(saved.payload));
  });
  expect(bytes).toEqual([31, 139, 8, 2]);
});


test('colon menu footers expose existing crafting and loadout actions at full width', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => window.fixture.view({ revision: 8, mode: 'Menu', title: 'InventoryOpen', rows: [
    {text: 'Space: pick  S: save  E: unequip  X: clear  F/B: mode  F1: details', color: '#e5e4cf'}
  ] }));
  for (const key of ['Space','S','E','X','F','B','F1','I']) await expect(page.locator('#menu-shortcuts button[data-key="' + key + '"]')).toBeVisible();
  const widths = await page.evaluate(() => ({reader: document.querySelector('#reader').getBoundingClientRect().width, area: document.querySelector('#play-area').getBoundingClientRect().width}));
  expect(widths.reader).toBe(widths.area);
});

test('target selection exposes details and a dedicated confirmation without a hidden wait action', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => window.fixture.view({revision: 4, mode: 'Targeting', title: 'ThrowTargeting', rows: [{text:'Throw: stone',color:'#fff'}]}));
  await expect(page.locator('#wait')).toBeDisabled();
  await expect(page.locator('#target-reader')).toContainText('Throw: stone');
  await page.getByRole('button', {name:'Confirm target',exact:true}).tap();
  expect(await calls(page)).toEqual([{kind:'key',value:'Return',revision:4}]);
});

test('a pointer held across a new UI revision cannot activate its old control', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => {
    const button = document.querySelector('[data-step="0,-1"]');
    button.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId: 1 }));
    window.fixture.view({ revision: 2 });
    button.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 }));
  });
  expect(await calls(page)).toHaveLength(0);
  await page.getByRole('button', { name: 'North', exact: true }).tap();
  expect(await calls(page)).toEqual([{kind:'step',x:0,y:-1,revision:2}]);
});

test('field notes take input ownership and release it through the Unity bridge', async ({ page }) => {
  await launch(page);
  await page.getByRole('button', {name:'Open recent messages'}).tap();
  await expect(page.locator('#journal')).toBeVisible();
  await page.locator('#journal-close').tap();
  expect(await page.evaluate(() => window.fixture.calls.filter(call => call.method === 'Overlay').map(call => call.value))).toEqual(['journal','closed']);
  expect(await calls(page)).toHaveLength(0);
});

test('real browser gzip validation and IndexedDB backup import round trip', async ({ page }) => {
  await launch(page);
  const raw = Buffer.alloc(12); raw.writeUInt32LE(0x304f4f43); raw.writeInt32LE(8, 4); raw.writeInt32LE(123, 8);
  const encoded = gzipSync(raw).toString('base64');
  const exported = await page.evaluate(async encoded => {
    const store = new CooBrowserSaveStore(); await store.hydrate();
    const bundle = {format:'caves-of-ooo-backup',version:1,buildId:'shell-test-build',checkpoints:[{
      gameId:'backup-browser',slot:'Quick',payloadBase64:encoded,metadata:JSON.stringify({GameID:'backup-browser',SaveVersion:8})
    }]};
    await store.importBackup(bundle, 'shell-test-build');
    return store.exportBackup('shell-test-build');
  }, encoded);
  expect(exported.checkpoints[0].payloadBase64).toBe(encoded);
});

test('a new confirmation modal resets the reader to its reviewable content', async ({ page }) => {
  await launch(page);
  await page.evaluate(() => {
    window.fixture.view({revision:2,mode:'Menu',title:'InventoryOpen',modal:'inventory:base:1',
      rows:Array.from({length:50},(_,i)=>({text:'Pack item '+i,color:'#e5e4cf'}))});
    document.querySelector('#reader-content').scrollTop=500;
    window.fixture.view({revision:3,modal:'inventory:replace',rows:[{text:'REPLACE EQUIPMENT? Will unequip: sword',color:'#e7c878'},{text:'[Enter] Confirm  [Esc] Back',color:'#e7c878'}]});
  });
  await expect(page.locator('#reader-content')).toContainText('Will unequip: sword');
  expect(await page.locator('#reader-content').evaluate(el=>el.scrollTop)).toBe(0);
  await expect(page.locator('#reader-content')).not.toContainText('Pack item');
  await expect(page.locator('#menu-shortcuts')).toBeHidden();
  expect(await page.locator('#reader-content').evaluate(el=>el.clientHeight)).toBeGreaterThanOrEqual(140);
});
