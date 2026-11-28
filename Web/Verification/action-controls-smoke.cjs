#!/usr/bin/env node
// Real Blazor WASM, native actions and IndexedDB. Renderer observation delegates
// the original draw unchanged; no runtime/game mocks or artificial dispatches.
// Mobile browser emulation is not physical iPhone evidence.
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium, webkit, devices } = require(process.env.COO_PLAYWRIGHT_PATH || '@playwright/test');
process.env.PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS = '1';
const BASE = process.env.COO_SMOKE_URL || 'http://127.0.0.1:8098';
const OUT = path.resolve(process.env.COO_SMOKE_OUTPUT || path.join(__dirname, 'browser-results-action-controls'));
const STEP = 20_000, BOOT = 90_000, CASE = 300_000;
const report = { startedAt: new Date().toISOString(), url: BASE,
  scope: 'Actual WASM UI and IndexedDB; read-only renderer snapshots; mobile viewport emulation, not physical iPhone.', cases: [] };
let activeBrowser;
const check = (ok, message) => { if (!ok) throw new Error(message); };
const clean = value => String(value ?? '').replace(/\{\{[^|{}]+\|([^{}]*)\}\}/g, '$1').replace(/[&^][kKrRgGwWbBmMcCyY]/g, '');
const escape = value => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const named = name => new RegExp(`^${escape(clean(name))}(?:\\s|$)`);
async function receipt() { await fs.mkdir(OUT, { recursive: true }); await fs.writeFile(path.join(OUT, 'results.json'), JSON.stringify(report, null, 2)); }
async function step(record, name, work) {
  const start = Date.now();
  try { const detail = await work(); record.steps.push({ name, status: 'passed', ms: Date.now() - start, detail }); console.log(`PASS ${record.name}: ${name}`); return detail; }
  catch (error) { record.steps.push({ name, status: 'failed', ms: Date.now() - start, error: error.stack || String(error) }); throw error; }
}
async function frames(page) { await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))); }
async function settled(page) {
  await page.waitForFunction(() => document.getElementById('loading-screen').hidden && document.getElementById('busy-overlay').hidden
    && !/^Saving/.test(document.getElementById('save-state').textContent), null, { timeout: BOOT }); await frames(page);
}
async function observe(page) {
  await page.evaluate(async () => {
    const { WorldMapView } = await import('./map.js'), original = WorldMapView.prototype.draw;
    WorldMapView.prototype.draw = function (...args) {
      const result = original.apply(this, args);
      if (this.canvas.id === 'world-canvas') window.__actionControlView = {
        data: this.data, bounds: this.bounds ? { ...this.bounds } : null,
        target: this.target ? { ...this.target } : null, aim: this.aim ? { ...this.aim } : null,
      };
      return result;
    };
  });
  await page.locator('#center-map').tap(); await frames(page);
}
async function snapshot(page) { return page.evaluate(() => window.__actionControlView.data); }
function summary(s, inventory = true) {
  return { name: s.name, tick: s.tick, x: s.x, y: s.y, zone: s.zone, hp: s.hp, focus: s.focus, drams: s.drams,
    ...(inventory ? { inventory: s.inventory.map(i => [i.id, i.count, i.equipped]).sort((a,b) => a[0].localeCompare(b[0])) } : {}) };
}
function free(before, after, reason, inventory = true) {
  check(JSON.stringify(summary(before, inventory)) === JSON.stringify(summary(after, inventory)),
    `${reason}: ${JSON.stringify({ before: summary(before, inventory), after: summary(after, inventory) })}`);
}
async function healthy(page) { const s = await snapshot(page); check(!s.error && !s.faulted && !s.dead, `Native state rejected the action: ${s.error}`); return s; }
async function changed(page, previous) {
  await page.waitForFunction(revision => window.__actionControlView?.data?.revision > revision, previous.revision);
  await settled(page); return healthy(page);
}
async function dispatchTap(page, locator) { const before = await snapshot(page); await locator.tap(); return changed(page, before); }
async function title(page, name) { await page.getByRole('heading', { name, exact: true }).waitFor(); await frames(page); }
async function close(page) {
  if (await page.locator('#overlay').isVisible()) { await dispatchTap(page, page.locator('#close-overlay')); await page.locator('#overlay').waitFor({ state: 'hidden' }); }
}
async function targetFits(locator) {
  await locator.scrollIntoViewIfNeeded();
  const m = await locator.evaluate(node => { const r = node.getBoundingClientRect(), hit = document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2);
    return { x:r.x, y:r.y, width:r.width, height:r.height, vw:innerWidth, vh:innerHeight, hit:hit === node || node.contains(hit) }; });
  check(m.width >= 43.5 && m.height >= 43.5 && m.hit && m.x >= -1 && m.y >= -1 && m.x + m.width <= m.vw + 1 && m.y + m.height <= m.vh + 1,
    `A control is clipped, covered or below44px: ${JSON.stringify(m)}`); return m;
}
async function sheetFits(page) {
  const m = await page.locator('#overlay-body').evaluate(node => ({ width:node.clientWidth, scrollWidth:node.scrollWidth,
    docWidth:document.documentElement.scrollWidth, vw:innerWidth }));
  check(m.scrollWidth <= m.width + 1 && m.docWidth <= m.vw + 1, `Panel has horizontal overflow: ${JSON.stringify(m)}`);
  await targetFits(page.locator('#close-overlay')); return m;
}
async function ability(page, name) {
  await close(page); await dispatchTap(page, page.locator('#touch-controls [data-panel="abilities"]')); await title(page, 'Abilities');
  await dispatchTap(page, page.getByRole('button', { name: named(name) })); await title(page, clean(name)); return healthy(page);
}
async function slots(page) {
  await page.getByRole('button', { name: 'Assign slot…', exact: true }).tap(); await title(page, 'Assign ability slot');
  check(await page.getByRole('button', { name: /^Slot \d+\b/ }).count() === 10, 'Assignment view does not offer all10 slots.');
  await sheetFits(page);
}
function slot(page, n) { return page.getByRole('button', { name: new RegExp(`^Slot ${n}\\b`) }); }
async function assign(page, name, number) {
  await ability(page, name); await slots(page); await dispatchTap(page, slot(page, number)); await title(page, clean(name));
  const s = await healthy(page), a = s.abilities.find(a => clean(a.name) === clean(name));
  check(a?.slot === number - 1, `Ability did not move into slot${number}: ${JSON.stringify(a)}`); return s;
}
async function item(page, name) {
  await close(page); await dispatchTap(page, page.locator('#touch-controls [data-panel="inventory"]')); await title(page, 'Pack & equipment');
  await dispatchTap(page, page.getByRole('button', { name: named(name) })); await title(page, clean(name)); return healthy(page);
}
async function equipment(page, name) {
  await item(page, name); await dispatchTap(page, page.getByRole('button', { name:'Equip', exact:true }));
  await title(page, 'Choose equipment slot'); await sheetFits(page); return (await healthy(page)).equipment;
}
async function reviewSlot(page, choice) {
  await dispatchTap(page, page.getByRole('button', { name:named(choice.name) })); await title(page, 'Review equipment');
  const review = (await healthy(page)).equipReview;
  check(review?.claimed?.includes(choice.name), `Review lost the chosen body slot: ${JSON.stringify({ choice, review })}`);
  await targetFits(page.getByRole('button', { name:'Equip · no turn', exact:true })); return review;
}
async function aimAbility(page, name) {
  await ability(page, name); await dispatchTap(page, page.getByRole('button', { name:'Use ability', exact:true }));
  await page.locator('#target-banner').waitFor({ state:'visible' });
  check(/choose a direction/i.test(await page.locator('#target-label').textContent()), 'DirectionLine is presented as a point target.');
  check((await page.locator('#confirm-target').textContent()).trim() === 'Review ability', 'Aim skips the review step.');
}
async function aimDirection(page, dx, dy) {
  const before = await snapshot(page); await page.locator(`[data-move="${dx},${dy}"]`).tap(); await frames(page);
  const view = await page.evaluate(() => ({ target:window.__actionControlView.target, aim:window.__actionControlView.aim }));
  check(view.target?.x === before.x + dx && view.target?.y === before.y + dy,
    `Direction selection drifted from the actor: ${JSON.stringify({ origin:[before.x,before.y], direction:[dx,dy], view })}`);
  check(view.aim?.dx === dx && view.aim?.dy === dy, `Direction cue differs from selected heading: ${JSON.stringify(view)}`);
  free(before, await snapshot(page), 'Selecting a heading advanced the simulation'); return view;
}
async function reviewAbility(page) {
  const before = await snapshot(page); await dispatchTap(page, page.locator('#confirm-target'));
  await page.locator('#target-banner').waitFor({ state:'hidden' });
  const commit = page.getByRole('button', { name:/^(Use ability|Attack .+) · 1 turn$/ }); await commit.waitFor();
  free(before, await snapshot(page), 'Reviewing an ability spent a turn'); await sheetFits(page); await targetFits(commit);
  return (await healthy(page)).abilityPreview;
}
async function tapCell(page, x, y) {
  const point = await page.evaluate(({x,y}) => { const b=window.__actionControlView.bounds;return { x:b.ox+(x+.5)*b.tw,y:b.oy+(y+.5)*b.th }; }, {x,y});
  await page.locator('#world-canvas').tap({ position:point }); await frames(page);
}
async function run(record, engine) {
  let browser, context, page, timer;
  const attach = p => {
    p.setDefaultTimeout(STEP); p.setDefaultNavigationTimeout(30_000);
    p.on('pageerror', e => record.errors.push({kind:'page',message:e.stack||String(e)}));
    p.on('console', m => {if(m.type()==='error')record.errors.push({kind:'console',message:m.text()});});
    p.on('response', r => {if(r.url().startsWith(BASE)&&r.status()>=400)record.errors.push({kind:'http',status:r.status(),url:r.url()});});
    p.on('requestfailed', r => record.requestFailures.push({url:r.url(),error:r.failure()?.errorText}));
  };
  const shot = async suffix => {const file=`${record.name}-${suffix}.png`;await page.screenshot({path:path.join(OUT,file),timeout:10_000});record.screenshots.push(file);};
  try {
    browser=await engine.launch({headless:true,timeout:30_000});activeBrowser=browser;
    timer=setTimeout(()=>{record.timedOut=true;browser.close().catch(()=>{});},CASE);
    context=await browser.newContext({...devices['iPhone 13'],viewport:record.viewport,screen:record.viewport,isMobile:true,hasTouch:true,deviceScaleFactor:2});
    page=await context.newPage();attach(page);
    const initial=await step(record,'real Classic expedition boots with native equipment and abilities',async()=>{
      await page.goto(BASE,{waitUntil:'domcontentloaded'});await page.locator('#new-button').tap();await page.locator('#builds .build-card').first().waitFor();
      await page.locator('#player-name').fill(`Actions ${record.name}`.slice(0,32));await page.locator('#world-seed').fill('8128');await page.locator('#enter-world').tap();
      await page.locator('#game-screen').waitFor({state:'visible',timeout:BOOT});await settled(page);
      await page.waitForFunction(()=>document.getElementById('save-state').textContent.includes('Saved on this device'),null,{timeout:BOOT});await observe(page);
      record.release=await page.evaluate(async()=>(await(await fetch('sw.js',{cache:'no-store'})).text()).match(/ooo-static-([a-f0-9]+)/)?.[1]||'unknown');
      const s=await healthy(page);check(s.abilities.some(a=>clean(a.name)==='Jet Blast'),'Classic is missing Jet Blast.');
      check(s.inventory.some(i=>i.blueprint==='Dagger'&&i.equipped),'Classic dagger is not initially equipped.');await shot('start');return s;
    });
    const dagger=initial.inventory.find(i=>i.blueprint==='Dagger'),jet=initial.abilities.find(a=>clean(a.name)==='Jet Blast');
    const other=initial.abilities.find(a=>a.id!==jet.id);check(other,'No second native ability for assignment replacement.');
    await step(record,'all10 assignment slots are readable and assignment replacement is free',async()=>{
      const before=await snapshot(page);await ability(page,jet.name);await slots(page);
      for(let n=1;n<=10;n++)await targetFits(slot(page,n));await shot('ability-slots');
      await dispatchTap(page,slot(page,10));await title(page,clean(jet.name));check((await snapshot(page)).abilities.find(a=>a.id===jet.id).slot===9,'Jet Blast assignment failed.');
      await ability(page,other.name);await slots(page);check((await slot(page,10).textContent()).includes(clean(jet.name)),'Occupied slot does not name the ability it replaces.');
      await dispatchTap(page,slot(page,10));await title(page,clean(other.name));
      const replaced=await healthy(page);check(replaced.abilities.find(a=>a.id===jet.id).slot===-1&&replaced.abilities.find(a=>a.id===other.id).slot===9,'Replacing a slot did not unassign its old occupant.');
      await dispatchTap(page,page.getByRole('button',{name:'Remove assignment',exact:true}));
      check((await snapshot(page)).abilities.find(a=>a.id===other.id).slot===-1,'Remove assignment left the ability assigned.');
      await assign(page,jet.name,10);free(before,await snapshot(page),'Ability management changed the simulation');
      return {assigned:clean(jet.name),slot:10,replaced:clean(other.name)};
    });
    await step(record,'closing nested assignment and throw-style menus releases the native panel without a turn',async()=>{
      const before=await snapshot(page),closed=[];
      await ability(page,jet.name);await slots(page);await dispatchTap(page,page.locator('#close-overlay'));
      await page.locator('#overlay').waitFor({state:'hidden'});let s=await healthy(page);
      check(s.panel==='',`Closing assignment slots left native panel ${s.panel}, which blocks movement.`);closed.push({menu:'Assign ability slot',panel:s.panel});
      await item(page,dagger.name);await page.getByRole('button',{name:'Throw…',exact:true}).tap();await title(page,'Choose throw style');
      await dispatchTap(page,page.locator('#close-overlay'));await page.locator('#overlay').waitFor({state:'hidden'});s=await healthy(page);
      check(s.panel==='',`Closing throw style left native panel ${s.panel}, which blocks movement.`);closed.push({menu:'Choose throw style',panel:s.panel});
      const aim=await page.evaluate(()=>({target:window.__actionControlView.target,aim:window.__actionControlView.aim,targeting:document.body.classList.contains('targeting')}));
      check(!aim.target&&!aim.aim&&!aim.targeting,`Closing nested menus left targeting active: ${JSON.stringify(aim)}`);
      free(before,s,'Closing a nested menu changed the simulation');return {closed,aim};
    });
    const chosenHand=await step(record,'equipment chooses an explicit hand and cancellation preserves ownership',async()=>{
      const before=await snapshot(page);await item(page,dagger.name);await dispatchTap(page,page.getByRole('button',{name:'Unequip',exact:true}));
      check(!(await snapshot(page)).inventory.find(i=>i.id===dagger.id).equipped,'Dagger did not unequip.');free(before,await snapshot(page),'Unequipping advanced the simulation',false);
      const e=await equipment(page,dagger.name),choices=e.slots.filter(s=>s.valid&&/hand/i.test(s.name));
      check(choices.length>=2,`Expected two explicit hand choices: ${JSON.stringify(e.slots)}`);const chosen=choices.find(s=>!s.primary)||choices.at(-1);
      const unequipped=await snapshot(page);await reviewSlot(page,chosen);await shot('equipment-review');
      await dispatchTap(page,page.getByRole('button',{name:'Cancel',exact:true}));free(unequipped,await snapshot(page),'Cancelling equipment changed the pack or world');
      const again=await equipment(page,dagger.name);await reviewSlot(page,again.slots.find(s=>s.id===chosen.id));
      await dispatchTap(page,page.getByRole('button',{name:'Equip · no turn',exact:true}));
      const equipped=await healthy(page);check(equipped.inventory.find(i=>i.id===dagger.id)?.equipped,'Confirmed dagger is not equipped.');
      free(before,equipped,'Equipment selection or confirmation spent a turn',false);return {id:chosen.id,name:chosen.name,primary:chosen.primary};
    });
    await step(record,'direction buttons select a heading from the actor and preview cancellation is free',async()=>{
      const before=await snapshot(page);await aimAbility(page,jet.name);const directions=[];
      for(const [dx,dy] of [[1,0],[-1,0],[0,-1],[1,0]])directions.push(await aimDirection(page,dx,dy));
      await targetFits(page.locator('#confirm-target'));await targetFits(page.locator('#cancel-target'));await shot('direction-aim');
      const preview=await reviewAbility(page);check(preview?.mode==='DirectionLine'&&preview.direction.x===1&&preview.direction.y===0,'Review lost the selected east heading.');
      await shot('ability-review');await dispatchTap(page,page.getByRole('button',{name:'Cancel',exact:true}));
      free(before,await snapshot(page),'Cancelled ability changed the simulation');check(!await page.locator('#target-banner').isVisible(),'Cancelled review left aiming active.');return {directions,preview};
    });
    await step(record,'opening Pack from the keyboard clears aim and closing it cannot revive stale targeting',async()=>{
      const before=await snapshot(page);await aimAbility(page,jet.name);await aimDirection(page,1,0);
      const aiming=await snapshot(page);await page.keyboard.press('i');await changed(page,aiming);await title(page,'Pack & equipment');
      check(!await page.locator('#target-banner').isVisible(),'Pack left the targeting banner active.');
      const inPack=await page.evaluate(()=>({target:window.__actionControlView.target,aim:window.__actionControlView.aim,targeting:document.body.classList.contains('targeting')}));
      check(!inPack.target&&!inPack.aim&&!inPack.targeting,`Opening Pack retained stale targeting: ${JSON.stringify(inPack)}`);
      await close(page);const closed=await page.evaluate(()=>({target:window.__actionControlView.target,aim:window.__actionControlView.aim,targeting:document.body.classList.contains('targeting')}));
      check(!closed.target&&!closed.aim&&!closed.targeting,`Closing Pack revived stale targeting: ${JSON.stringify(closed)}`);
      check(!await page.locator('#target-banner').isVisible(),'Closing Pack restored the targeting banner.');
      free(before,await snapshot(page),'Interrupting aim with Pack changed the simulation');await shot('aim-interrupted-by-pack');return {inPack,closed};
    });
    await step(record,'body and ground throw previews remain distinct at the same visible body',async()=>{
      const before=await snapshot(page),body=before.cells.find(c=>c.v&&c.art==='opening-base:9');
      check(body,'The actual seeded glade has no visible Warden for the throw preview.');const previews=[];
      for(const style of ['body','ground']){
        await item(page,dagger.name);await page.getByRole('button',{name:'Throw…',exact:true}).tap();await title(page,'Choose throw style');
        const choice=page.getByRole('button',{name:style==='body'?'Throw at a body…':'Throw at the ground…',exact:true});await targetFits(choice);
        await dispatchTap(page,choice);await page.locator('#target-banner').waitFor({state:'visible'});
        check((await page.locator('#target-label').textContent()).includes(`${style} aim`),'Aim label lost the explicit throw style.');
        await tapCell(page,body.x,body.y);await dispatchTap(page,page.locator('#confirm-target'));await title(page,'Review your throw');
        const p=(await healthy(page)).throwPreview;check(p?.aim===style,`Throw review changed ${style} into ${p?.aim}.`);
        check(style==='body'?/^Body:/.test(clean(p.description)):/^Ground\b/.test(clean(p.description)),`Throw intent does not match its explicit choice: ${JSON.stringify(p)}`);
        free(before,await snapshot(page),'Throw preview spent a turn or removed the equipped item');await shot(`throw-${style}-review`);
        await dispatchTap(page,page.getByRole('button',{name:'Cancel',exact:true}));free(before,await snapshot(page),'Cancelling throw changed ownership or simulation');previews.push(p);
      }
      return {cell:{x:body.x,y:body.y},previews};
    });
    await step(record,'one explicit ability confirmation commits exactly one native turn',async()=>{
      const before=await snapshot(page);await aimAbility(page,jet.name);await aimDirection(page,1,0);await reviewAbility(page);
      free(before,await snapshot(page),'Preparing the committed cast advanced time');
      await dispatchTap(page,page.getByRole('button',{name:/^(Use ability|Attack .+) · 1 turn$/}));await page.locator('#overlay').waitFor({state:'hidden'});
      const after=await healthy(page);check(after.tick===before.tick+10,`Single confirmation did not cost exactly10ticks: ${JSON.stringify({before:summary(before),after:summary(after)})}`);
      check(after.x===before.x&&after.y===before.y&&after.hp===before.hp,'Controlled eastward cast moved or harmed the caster.');
      check(after.abilities.find(a=>a.id===jet.id).cooldown>0,'Actual cast did not start its native cooldown.');await shot('after-cast');return {before:summary(before),after:summary(after)};
    });
    await step(record,'cooldown abilities remain inspectable and assignable without another turn',async()=>{
      const before=await snapshot(page);await ability(page,jet.name);
      check(await page.getByRole('button',{name:'Use ability',exact:true}).isDisabled(),'Cooldown ability still offers an enabled Use ability.');
      await slots(page);await dispatchTap(page,slot(page,9));await title(page,clean(jet.name));
      check((await snapshot(page)).abilities.find(a=>a.id===jet.id).slot===8,'Cooldown prevented assignment management.');
      await assign(page,jet.name,10);free(before,await snapshot(page),'Cooldown ability management advanced the simulation');return {slot:10,cooldown:(await snapshot(page)).abilities.find(a=>a.id===jet.id).cooldown};
    });
    const saved=await step(record,'checkpoint persists the actual assignments and equipment',async()=>{
      await close(page);await page.locator('#more-button').tap();await title(page,'Your expedition');
      await page.getByRole('button',{name:/^Save checkpoint\b/}).tap();await page.locator('#overlay').waitFor({state:'hidden'});await settled(page);
      await page.waitForFunction(()=>document.getElementById('save-state').textContent.includes('Saved on this device'));await shot('saved');return snapshot(page);
    });
    await step(record,'new page Continue restores assignments equipment and exact paid-turn state',async()=>{
      await page.close();page=await context.newPage();attach(page);await page.goto(BASE,{waitUntil:'domcontentloaded'});
      await page.locator('#continue-button').waitFor({state:'visible'});await page.locator('#continue-button').tap();
      await page.locator('#game-screen').waitFor({state:'visible',timeout:BOOT});await settled(page);await observe(page);
      const restored=await healthy(page);free(saved,restored,'Restoring the checkpoint changed gameplay or equipment');
      check(restored.abilities.find(a=>a.id===jet.id)?.slot===9,'Saved assignment to slot10 was lost.');
      check(restored.abilities.find(a=>a.id===jet.id)?.cooldown===saved.abilities.find(a=>a.id===jet.id).cooldown,'Cooldown did not survive checkpoint.');
      await ability(page,jet.name);check((await page.locator('#overlay-kicker').textContent()).includes('SLOT 10'),'Restored assignment is not represented in the UI.');
      await item(page,dagger.name);check(await page.getByRole('button',{name:'Unequip',exact:true}).isVisible(),'Restored dagger is not represented as equipped.');await shot('continued');
      return {state:summary(restored),slot:10,chosenHand};
    });
    await step(record,'no runtime console HTTP or request failures',async()=>{
      check(record.errors.length===0&&record.requestFailures.length===0,JSON.stringify({errors:record.errors,requests:record.requestFailures}));return {errors:0,requestFailures:0};
    });
    check(!record.timedOut,'Case exceeded its time bound.');record.status='passed';
  }catch(error){record.status='failed';record.error=error.stack||String(error);console.error(`FAIL ${record.name}: ${error.message||error}`);if(page&&!page.isClosed())try{await shot('failure');}catch(capture){record.screenshotError=String(capture);}}
  finally{clearTimeout(timer);if(context)await context.close().catch(()=>{});if(browser)await browser.close().catch(()=>{});if(activeBrowser===browser)activeBrowser=null;}
}
async function main(){
  await receipt();const timeout=setTimeout(async()=>{report.error='Action-controls suite exceeded25minutes.';await receipt().catch(()=>{});if(activeBrowser)await activeBrowser.close().catch(()=>{});process.exit(2);},25*60_000);
  for(const [engineName,engine] of [['chromium',chromium],['webkit',webkit]])for(const [orientation,viewport] of [['portrait',{width:390,height:844}],['landscape',{width:667,height:375}]]){
    const name=`${engineName}-${orientation}`;if(process.env.COO_SMOKE_CASE&&process.env.COO_SMOKE_CASE!==name)continue;
    const record={name,engine:engineName,viewport,status:'running',startedAt:new Date().toISOString(),steps:[],errors:[],requestFailures:[],screenshots:[]};
    report.cases.push(record);await receipt();await run(record,engine);record.finishedAt=new Date().toISOString();await receipt();
  }
  clearTimeout(timeout);report.finishedAt=new Date().toISOString();report.passed=report.cases.filter(c=>c.status==='passed').length;report.failed=report.cases.length-report.passed;await receipt();
  console.log(JSON.stringify({cases:report.cases.length,passed:report.passed,failed:report.failed,receipt:path.join(OUT,'results.json')}));process.exitCode=report.failed?1:0;
}
main().catch(async error=>{report.error=error.stack||String(error);await receipt().catch(()=>{});console.error(error);process.exit(1);});
