import {WorldMapView,imagesReady,drawOpening} from './map.js';
const $=id=>document.getElementById(id);
const BUILD='standalone-web-1';
let state=null,busy=false,saving=null,dirty=0,lastSaveTick=-1,store=new CooBrowserSaveStore(),checkpoints=[],storageReady=false,enginePromise=null,selectedBuild='classic',target=null,localMenu=false,toastTimer,saveAfterAction=false;
const map=new WorldMapView($('world-canvas'));
drawOpening($('opening-art'));imagesReady.then(()=>map.draw());
const clean=value=>String(value??'').replace(/\{\{[^|{}]+\|([^{}]*)\}\}/g,'$1').replace(/[&^][kKrRgGwWbBmMcCyY]/g,'');
function text(id,value){$(id).textContent=clean(value);}
// The core's CP437 turn rule is decorative; keep the latest narration visible.
function narrativeMessages(){return state.messages.filter(m=>!/^\u00cd+$/.test(clean(m).trim()));}
function el(tag,content,cls){const node=document.createElement(tag);if(content!=null)node.textContent=clean(content);if(cls)node.className=cls;return node;}
function button(label,fn,cls=''){const b=el('button',label,cls);b.type='button';b.addEventListener('click',fn);return b;}
function toast(message){text('toast',message);$('toast').hidden=false;clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('toast').hidden=true,5000);}
function status(message,error=false){for(const id of ['save-state','landscape-save-state']){text(id,message);$(id).style.color=error?'var(--danger)':'';}}
function showLoading(title,detail){text('loading-title',title);text('loading-detail',detail);$('loading-screen').hidden=false;}
function hideLoading(){$('loading-screen').hidden=true;}
function showOverlay(title,kicker,children){text('overlay-title',title);text('overlay-kicker',kicker);$('overlay-body').replaceChildren(...children.filter(Boolean));$('overlay').hidden=false;}
function actions(items){const group=el('div',null,'action-grid');for(const [label,fn,cls] of items)group.append(button(label,fn,cls));return group;}
function row(name,sub,fn,side='›'){const r=button('',fn,'row'),body=el('div');body.append(el('strong',name));if(sub)body.append(el('small',sub));r.append(body,el('span',side,'price'));return r;}
function heading(title){return el('p',title,'subheading');}
function rows(list){const box=el('div',null,'rows');box.append(...list);return box;}
function empty(message){return el('p',message,'empty-state');}
function closeLocal(){localMenu=false;$('overlay').hidden=true;}
async function closePanel(){if(busy)return;if(localMenu){closeLocal();return;}await act('close');}
$('close-overlay').onclick=closePanel;
$('overlay').addEventListener('pointerdown',e=>{if(e.target===$('overlay')){e.preventDefault();closePanel();}});
async function runtime(){
 if(!enginePromise){
  let loaded=0;
  enginePromise=Blazor.start({loadBootResource(type,name,url,integrity){
   if(type==='dotnetjs')return url;
   return fetch(url,{cache:'default',integrity:integrity||undefined}).then(response=>{loaded++;$('load-progress').value=Math.min(95,loaded);return response;});
  }}).catch(error=>{enginePromise=null;throw error;});
 }
 return enginePromise;
}
async function invoke(method,...args){await runtime();return DotNet.invokeMethodAsync('CavesWeb',method,...args);}
function playState(next){
 const old=state;state=typeof next==='string'?JSON.parse(next):next;
 $('start-screen').hidden=true;$('creation-screen').hidden=true;$('game-screen').hidden=false;$('menu-button').hidden=false;document.body.classList.add('playing');
 map.set(state);render();if(state.error)toast(state.error);
 if(state.dead&&!old?.dead)showDeath();
}
function render(){
 if(!state)return;
 document.body.classList.toggle('targeting',!!target);
 document.body.classList.toggle('world-map',!!state.isWorldMap);
 text('landscape-target-name',target?.label||'');
 text('location',state.zoneName);text('biome-label',state.isWorldMap?'THE LANDS OF OOO':`${state.regionName||'The overworld'}${state.depth>0?' · below the surface':''}`.toUpperCase());text('position',state.isWorldMap?`${state.worldX}, ${state.worldY}`:`${state.x}, ${state.y}`);text('turn-number',`TICK ${state.tick}`);
 text('interact-label',state.isWorldMap?'Enter area':'Interact');
 text('map-instruction',state.isWorldMap?'Tap a place to read · compass to travel · Enter area to explore':'Tap to look · drag to pan · the world waits for you');
 text('character-name',state.name);text('character-build',`${state.build||'Wayfarer'} · level ${state.level||1}`);text('hp-text',`${state.hp} / ${state.maxHp}`);$('hp-bar').style.width=`${Math.max(0,Math.min(100,100*state.hp/state.maxHp))}%`;
 text('drams',state.drams);text('focus',state.focus);text('mobile-hp',`♥ ${state.hp}/${state.maxHp}`);text('mobile-focus',`Focus ${state.focus}`);text('mobile-drams',`${state.drams} drams`);text('mobile-turn',`t ${state.tick}`);
 const recent=state.isWorldMap?[`${clean(state.mapLocation?.name||state.regionName)} · ${state.worldX}, ${state.worldY} · Enter area to explore`]:narrativeMessages().slice(-3);
 $('log-lines').replaceChildren(...recent.map(m=>el('p',m)));
 $('wait-button').disabled=!!target||state.dead||state.faulted;
 $('target-banner').hidden=!target;$('map-instruction').hidden=!!target;
 if(target){text('target-label',target.label);text('confirm-target',target.kind==='throw'?'Review throw':'Confirm');$('confirm-target').disabled=!map.target;}
 if(!localMenu)renderPanel();
}
async function act(kind,extra={}){
 if(!state||busy||saving||document.hidden)return false;
 busy=true;localMenu=false;let delay=setTimeout(()=>{$('busy-overlay').hidden=false;},160);
 const before=state;
 try{
  playState(await invoke('Dispatch',JSON.stringify({kind,...extra,revision:state.revision})));
  const mutation=!['look','panel','close','select','throwPreview'].includes(kind)&&!(kind==='skill'&&['select','review','cancel'].includes(extra.command));
  if(mutation&&(!state.error||state.tick!==before.tick||JSON.stringify(state.inventory)!==JSON.stringify(before.inventory))){dirty++;status('Checkpoint pending');}
  if(!state.dead&&!state.faulted&&(state.zone!==before.zone||dirty>=6||state.tick-lastSaveTick>=50))await saveCheckpoint(true);
  return !state.error;
 }catch(error){toast(error.message||error);return false;}
 finally{clearTimeout(delay);$('busy-overlay').hidden=true;busy=false;if(saveAfterAction&&dirty){saveAfterAction=false;saveCheckpoint().catch(()=>{});}}
}
function renderPanel(){
 $('close-overlay').disabled=false;
 if(state.conversation){showConversation();return;}
 if(state.dead){showDeath();return;}
 const list=[];
 switch(state.panel){
  case 'inventory':
   list.push(el('p',`${state.weight} weight carried · ${state.drams} drams`,'details'));
   const equipped=state.inventory.filter(i=>i.equipped),carried=state.inventory.filter(i=>!i.equipped);
   list.push(button('Crafting & alchemy',()=>act('panel',{value:'crafting'}),'outline'));
   if(equipped.length)list.push(heading('Equipment'),rows(equipped.map(item=>row(item.name,'Equipped',()=>act('select',{id:item.id})))));
   list.push(heading('In your pack'),carried.length?rows(carried.map(item=>row(item.name,item.count>1?`${item.count} in the stack`:`Weight ${item.weight}`,()=>act('select',{id:item.id})))):empty('Your pack is empty.'));
   if(state.ground.length)list.push(heading('At your feet'),lootPicker(state.ground,'pickup'));
   showOverlay('Pack & equipment','WHAT YOU CARRY',list);break;
  case 'item':{
   const item=state.item;if(!item){act('panel',{value:'inventory'});return;}
   list.push(el('p',`${item.count} item${item.count===1?'':'s'} · weight ${item.weight}${item.equipped?' · equipped':''}`,'details'));
   list.push(actions([[item.equipped?'Unequip':'Equip',()=>act('item',{id:item.id,command:item.equipped?'Unequip':'Equip'})],['Drop',()=>act('item',{id:item.id,command:'Drop'})],['Throw…',()=>beginTarget({kind:'throw',id:item.id,label:`Throw ${clean(item.name)} · choose a tile`})]]));
   if(item.focus)list.push(button('Throw with focused splash…',()=>beginTarget({kind:'throw',id:item.id,focused:true,label:`Focused splash · choose a tile`}), 'outline'));
   const own=(item.actions||[]).filter(a=>!['equip','unequip','drop','throw'].includes(a.command.toLowerCase()));
   list.push(rows(own.map(a=>row(a.name,'',()=>act('item',{id:item.id,command:a.command})))));
   list.push(button('← Back to pack',()=>act('panel',{value:'inventory'}),'quiet'));
   showOverlay(item.name,'IN YOUR HANDS',list);break;}
  case 'look':{
   const look=state.look;if(!look){closeLocal();return;}
   if(state.isWorldMap&&look.mapLocation){
    const place=look.mapLocation;
    list.push(el('p',`The ${place.biome} · ${place.visited?'Visited':'Not yet visited'}`,'details'));
    if(look.underfoot)list.push(button('Enter this area · 1 turn',()=>act('travel',{value:'local'}),'gold'));
    else list.push(el('p','Use the compass to travel here, then choose Enter area. Reading this place takes no time.','details'));
    showOverlay(place.name,`${place.worldX}, ${place.worldY} · LOOKING TAKES NO TIME`,list);break;
   }
   list.push(el('p',look.summary),...look.details.map(d=>el('p',d,'details')));
   if(look.actions?.length)list.push(heading('Interactions'),rows(look.actions.map(a=>row(a.name,'',()=>worldAction(look.target.id,a)))));
   else if(look.target&&!look.reachable)list.push(el('p','Move beside it to interact.','details'));
   if(look.underfoot&&look.items.length)list.push(heading('At your feet'),lootPicker(look.items,'pickup'));
   showOverlay(look.title,`${look.x}, ${look.y} · LOOKING TAKES NO TIME`,list);break;}
  case 'nearby':
   if(state.ground.length)list.push(heading('At your feet'),lootPicker(state.ground,'pickup'));
   list.push(...state.nearby.map(n=>{const box=el('div');box.append(heading(n.name),actions(n.actions.map(a=>[a.name,()=>worldAction(n.id,a)])));return box;}));
   if(!list.length)list.push(empty('Nothing is within reach. Tap a tile to look, or move closer.'));
   showOverlay('Within reach','THE WORLD AROUND YOU',list);break;
  case 'abilities':
   list.push(el('p','Choose an ability, then a target. Selecting and aiming take no time.','details'));
   list.push(rows((state.abilities||[]).map(a=>{const b=row(a.name,`${a.category} · ${a.targeting==='SelfCentered'?'self':`range ${a.range}`}`,()=>a.targeting==='SelfCentered'?act('ability',{id:a.id,x:state.x,y:state.y}):beginTarget({kind:'ability',id:a.id,label:`${clean(a.name)} · range ${a.range}`}),a.cooldown?`${a.cooldown} turns`:'Ready');b.disabled=a.cooldown>0;return b;})));
   if(!state.abilities?.length)list.push(empty('No active abilities yet. Learn skills to discover new ways to act.'));
   list.push(button('Skills & attributes',()=>act('panel',{value:'skills'}),'outline'));
   showOverlay('Abilities','THE THINGS YOU CAN DO',list);break;
  case 'throwing':{
   const t=state.throwing;list.push(el('p',t.mode==='Measured'?'Prepare your aim for one turn, then confirm the throw on your next action.':t.mode==='Recovery'?'Step to a nearby item you threw and collect it.':'Choose a heavy item to drive into a target.','details'));
   list.push(rows(t.items.map(i=>row(i.name,t.mode==='Recovery'?'Recover · one turn':`Range ${i.range}`,()=>t.mode==='Recovery'?act('recover',{id:i.id}):beginTarget({kind:'throw',id:i.id,mode:t.mode,label:`${t.mode} throw · range ${i.range}`})))));
   if(!t.items.length)list.push(empty('No suitable items are in reach.'));
   showOverlay(t.mode==='Recovery'?'Recovering step':`${t.mode} throw`,'CHOOSE AN ITEM',list);break;}
  case 'throwPreview':
   if(!state.throwPreview){closeLocal();return;}
   list.push(el('p',state.throwPreview.description),actions([[state.throwPreview.mode==='Measured'?'Prepare aim · 1 turn':'Throw · 1 turn',()=>act('throw'),'gold'],['Cancel',()=>act('close')]]));
   showOverlay('Review your throw','THE WORLD WAITS',list);break;
  case 'crafting':{
   const c=state.crafting;list.push(el('p','Set ingredients aside to preview a combination. Nothing is consumed until you confirm crafting.','details'));
   list.push(rows(c.items.map(i=>row(i.name,i.group,()=>act('craft',{command:'mark',id:i.id}),i.marked?'✓ Selected':'+'))));
   if(!c.items.length)list.push(empty('Gather reagents and weapon components to begin.'));
   list.push(heading('Forge a weapon'),el('p',c.forge.IsComplete?`${c.forge.DisplayName} · ${c.forge.BaseDamage} damage · ${c.forge.FamilyDisplayName}`:`Needs ${c.forge.Missing||'a blade, haft, and binding'}.`));
   if(c.forge.IsComplete)list.push(button('Confirm · forge one',()=>act('craft',{command:'forge'}),'gold'));
   list.push(heading('Brew'),el('p',c.brew.IsValid?`${c.brew.DisplayName} · ${c.brew.Form} · ${c.brew.Properties}`:c.brew.Reason||'Select reagents to see a result.'));
   if(c.brew.IsValid)list.push(button('Confirm · brew one',()=>act('craft',{command:'brew'}),'gold'));
   if(c.canTemper)list.push(button('Quench selected weapon · needs forge',()=>act('craft',{command:'temper'}),'outline'));
   if(c.recipes.length)list.push(heading('Known recipes'),rows(c.recipes.map(r=>row(r.name,`${r.description||''} · bits: ${r.cost}${r.ingredient?` · ${r.ingredient}`:''}`,()=>act('craft',{command:'recipe',id:r.id}),r.affordable?'Craft':'Needs bits'))));
   showOverlay('Crafting & alchemy','MAKE SOMETHING OF WHAT YOU FIND',list);break;}
  case 'skills':showSkills();break;
  case 'journal':
   if(state.quests){
    const human=s=>clean(s).replace(/([a-z])([A-Z])/g,'$1 $2').replace(/[_-]/g,' ');
    if(state.quests.Active?.length){list.push(heading('Your undertakings'));for(const q of state.quests.Active){
     list.push(el('h3',human(q.QuestId)));
     for(const stage of q.Stages||[])if(stage.Status!==2)list.push(el('p',`${stage.Status===0?'✓':'→'} ${human(stage.StageId)}`,'details'));
     for(const o of q.CurrentObjectives||[])list.push(el('p',`${o.Done?'✓':'○'} ${o.Text}${o.HasProgress?` (${o.Current}/${o.Target})`:''}${o.Optional?' · optional':''}`));
    }}
    if(state.quests.Completed?.length)list.push(heading('Completed'),...state.quests.Completed.map(q=>el('p',`✓ ${human(q)}`)));
   }
   list.push(heading('Field notes'),...narrativeMessages().reverse().map(m=>el('div',m,'journal-entry')));
   showOverlay('Field journal','THINGS WORTH REMEMBERING',list);break;
  case 'character':
   list.push(el('h3',state.name),el('p',`${state.build} · ${state.zoneName}`,'details'));
   const grid=el('div',null,'stats-grid');for(const s of state.stats){const box=el('div',null,'stat-box');box.append(el('span',s.name),el('strong',s.value));grid.append(box);}list.push(grid,actions([['Learn skills',()=>act('panel',{value:'skills'})],['Abilities',()=>act('panel',{value:'abilities'})]]));
   showOverlay('The wayfarer','CHARACTER',list);break;
  case 'container':
   if(!state.contents){closeLocal();return;}
   list.push(state.contents.items?.length?lootPicker(state.contents.items,'container'):empty('The container is empty.'));
   showOverlay(state.contents.name,'WHAT’S INSIDE',list);break;
  case 'trade':
   if(!state.trade){closeLocal();return;}
   list.push(el('p',`You: ${state.drams} drams · ${clean(state.trade.name)}: ${state.trade.drams} drams`,'details'),heading('Buy'),rows(state.trade.stock.map(i=>row(i.name,`Stack of ${i.count}`,()=>act('buy',{id:i.id}),`${i.price} drams`))),heading('Sell'),rows(state.trade.sell.map(i=>row(i.name,'',()=>act('sell',{id:i.id}),`${i.price} drams`))));
   showOverlay(state.trade.name,'TRADE',list);break;
  case 'map':
   list.push(el('p','The compass crosses one area at a time on the world map. Travel advances time; reading places is free. Enter an area to explore it on foot.','details'));
   if(state.travel.canLeave)list.push(button('Enter this area · 1 turn',()=>act('travel',{value:'local'}),'gold'),button('← Return to the map',()=>act('close'),'outline'));
   else if(state.travel.canEnter)list.push(button('Set out on the world map · 1 turn',()=>act('travel',{value:'world'}),'gold'));
   else list.push(el('p','Return to the surface using the stairs before setting out on the world map.','details'));
   list.push(heading('Regions'),el('p','The Spread · The Sodden · The Beating · The Grovelands · The Overwrit · The Stump','details'),heading('Known places'));
   for(const place of [...(state.journey?.places||[])].sort((a,b)=>Number(b.current)-Number(a.current)||a.name.localeCompare(b.name))){
    const detail=`${place.x}, ${place.y} · ${place.biome} · ${place.current?'You are here':place.visited?'Visited':'Unvisited'}`;
    if(state.isWorldMap)list.push(row(place.name,detail,()=>act('look',{x:place.x+state.mapBounds.x,y:place.y+state.mapBounds.y}),place.glyph||'·'));
    else {const entry=el('div',null,'row place-entry'),body=el('div');body.append(el('strong',place.name),el('small',detail));entry.append(body,el('span',place.glyph||'·','price'));list.push(entry);}
   }
   showOverlay('World map','YOUR JOURNEY',list);break;
  default:$('overlay').hidden=true;break;
 }
}
function worldAction(id,a){return a.command==='Throw'?beginTarget({kind:'throw',id,label:'Throw · choose a visible tile'}):act('world',{id,command:a.command});}
function lootPicker(items,kind){
 const box=el('div'),chosen=new Set();
 for(const i of items){const label=el('label',null,'loot-choice'),input=document.createElement('input');input.type='checkbox';input.onchange=()=>{if(input.checked)chosen.add(i.id);else chosen.delete(i.id);};label.append(input,el('span',`${clean(i.name)}${i.count>1?` × ${i.count}`:''}`));box.append(label);}
 box.append(actions([['Take selected · 1 turn',()=>chosen.size?act(kind,{ids:[...chosen]}):toast('Select something to take.')],['Take all · 1 turn',()=>act(kind,{ids:items.map(i=>i.id)})]]));return box;
}
function showConversation(){const c=state.conversation;showOverlay(c.speaker,'IN CONVERSATION',[el('div',c.text,'conversation-text'),...c.choices.map(choice=>button(choice.text,()=>act('choice',{index:choice.index}),'choice'))]);$('close-overlay').disabled=!c.escape;}
function showSkills(){
 const s=state.skills;if(!s)return;const list=[];
 $('close-overlay').disabled=false;
 if(s.pending){const review=el('div',null,'review');review.append(el('h3','Learn this skill?'),el('p',`${s.cost} ${s.unit} · ${s.points} available`),el('p',s.warning,'details'),actions([['Confirm purchase',()=>act('skill',{command:'buy',id:s.pending}),'gold'],['Cancel',()=>act('skill',{command:'cancel'})]]));list.push(review);}
 if(s.details)list.push(el('div',s.details,'details'),el('p',s.requirements,'details'),el('p',s.summary,'details'));
 if(s.notice)list.push(el('p',s.notice,'warning'));
 for(const group of s.groups){const section=el('details',null,'skill-group');section.open=group.rows.some(r=>r.id===s.selected);section.append(el('summary',group.name));for(const r of group.rows){section.append(row(r.name,`${r.state} · rank ${r.rank}/${r.maxRank}`,()=>act('skill',{command:'select',id:r.id}),r.cost>0?`${r.cost} SP`:'—'));if(r.id===s.selected&&r.eligible)section.append(button('Review purchase',()=>act('skill',{command:'review',id:r.id}),'gold'));}list.push(section);}
 showOverlay('Skills & attributes','CHOOSE WHAT YOU BECOME',list);
}
async function beginTarget(choice){if(await act('close')){target=choice;map.target=null;render();map.draw();}}
function clearTarget(){target=null;map.target=null;render();map.draw();}
$('cancel-target').onclick=clearTarget;
$('confirm-target').onclick=async()=>{if(!target||!map.target||busy)return;const t=target,point=map.target;if(await act(t.kind==='throw'?'throwPreview':t.kind,{id:t.id,value:t.mode||'Ordinary',focused:!!t.focused,x:point.x,y:point.y}))clearTarget();};
let pointer=null;
$('world-canvas').addEventListener('pointerdown',e=>{if(busy)return;pointer={id:e.pointerId,x:e.clientX,y:e.clientY,lastX:e.clientX,lastY:e.clientY,moved:false};e.currentTarget.setPointerCapture(e.pointerId);});
$('world-canvas').addEventListener('pointermove',e=>{if(!pointer||pointer.id!==e.pointerId)return;const dx=e.clientX-pointer.lastX,dy=e.clientY-pointer.lastY;if(Math.hypot(e.clientX-pointer.x,e.clientY-pointer.y)>8)pointer.moved=true;if(pointer.moved)map.pan(dx,dy);pointer.lastX=e.clientX;pointer.lastY=e.clientY;});
$('world-canvas').addEventListener('pointerup',e=>{const p=pointer;pointer=null;if(!p||p.id!==e.pointerId||p.moved||busy||!state)return;const rect=e.currentTarget.getBoundingClientRect(),tile=map.tileAt(e.clientX-rect.left,e.clientY-rect.top);if(!tile)return;if(target){if(!state.cells.some(t=>t.x===tile.x&&t.y===tile.y&&t.v)){toast('Choose a visible place.');return;}map.target=tile;map.draw();render();}else act('look',tile);});
$('world-canvas').addEventListener('pointercancel',()=>pointer=null);
$('zoom-in').onclick=()=>map.zoom(.2);$('zoom-out').onclick=()=>map.zoom(-.2);$('center-map').onclick=()=>map.center();
// Keep a single click activation path for touch, mouse, keyboard and assistive tech.
// Cancel browser-only follow-up gestures, including Safari selection/callout fallbacks.
const compass=document.querySelector('.compass');
for(const type of ['dblclick','contextmenu','selectstart'])compass.addEventListener(type,event=>event.preventDefault());
document.querySelectorAll('[data-move]').forEach(b=>b.onclick=()=>{const [x,y]=b.dataset.move.split(',').map(Number);if(target){const origin=map.target||state;map.target={x:Math.max(0,Math.min(79,origin.x+x)),y:Math.max(0,Math.min(24,origin.y+y))};map.draw();render();}else act('step',{x,y});});
$('wait-button').onclick=()=>act('wait');document.querySelectorAll('[data-panel]').forEach(b=>b.onclick=()=>act('panel',{value:b.dataset.panel}));$('log-open').onclick=()=>act('panel',{value:'journal'});
$('interact-button').onclick=()=>state?.isWorldMap?act('travel',{value:'local'}):act('panel',{value:'nearby'});
function gameMenu(){if(!state||busy)return;localMenu=true;showOverlay('Your expedition','TAKE YOUR TIME',[rows([
 row('Save checkpoint','Keep this moment on this device',async()=>{try{await saveCheckpoint();closeLocal();}catch{}},'↓'),
 row('Export checkpoint','Download a backup file',exportBackup,'↗'),
 row('Character','Attributes, equipment, and abilities',()=>act('panel',{value:'character'})),
 row('Skills & attributes','Learn and improve',()=>act('panel',{value:'skills'})),
 row('World map','Regions, known places, and travel',()=>act('panel',{value:'map'}),'⌖'),
 row(state.isWorldMap?'Enter area':'Descend',state.isWorldMap?'Explore the place beneath your marker':'Stand on a downward stair',()=>{closeLocal();act('stairs',{value:'down'});},'↓'),
 row('Ascend',state.travel.canEnter?'Use stairs or set out on the world map':'Stand on an upward stair',()=>{closeLocal();act('stairs',{value:'up'});},'↑'),
 row('How to play','Touch and keyboard controls',showHelp,'?'),
 row('Begin another expedition','Your saved expedition is kept',async()=>{await saveCheckpoint();closeLocal();showCreation();},'+')
 ])]);}
$('more-button').onclick=gameMenu;$('menu-button').onclick=gameMenu;$('save-button').onclick=()=>saveCheckpoint().catch(()=>{});
function showHelp(){localMenu=true;showOverlay('One turn at a time','A FIELD GUIDE',[el('p','The world waits while you think. Use the compass to move in eight directions. Bump into a hostile creature to strike; the center dot waits one turn.'),el('p','Tap a tile to inspect it for free. Drag the map to look farther away. Interact lists the things within reach: conversations, supplies, doors, plants, and the work of a place.'),el('p','Pack holds your belongings and equipment. Abilities opens targeting: choose a tile, then confirm. Looking, reading, and choosing never rush you.'),heading('Keyboard'),el('p','Arrow keys or H J K L move. Y U B N move diagonally. Period waits. I opens your pack; A abilities; E interactions; Shift+K skills; Shift+J journal; M opens the world map guide. Esc closes a menu; F5 saves. Use < and > on stairs; < from the surface enters the world map, and > enters the area beneath your marker.'),heading('World travel'),el('p','Open More → World map to set out from the surface. Move with the compass, tap landmarks to read, then choose Enter area. Returning immediately to the area you left restores your previous position.'),heading('Along the way'),el('p','Read the notice near the glade’s center. The well needs fire clay. There is a cellar to the west, and beyond the glade lies Sill.'),heading('On your iPhone'),el('p','Open this page in Safari. Use Share → Add to Home Screen for a full-screen expedition. Once the game has finished downloading, it can reopen offline while its cached files remain on your device.'),heading('Your progress'),el('p','Checkpoints save locally after several actions, on travel, and when you press Save. Wait for “Saved on this device” before closing. Export a backup before clearing browser data. Different browsers keep separate expeditions.')]);}
$('help-start').onclick=showHelp;
function showDeath(){localMenu=true;target=null;showOverlay('The expedition ends','EVERY ROAD LEAVES A STORY',[el('p',`${clean(state.name)} fell in ${state.zoneName}. Your last living checkpoint is kept.`),actions([['Continue checkpoint',()=>continueGame(state.gameId),'gold'],['Another expedition',()=>{closeLocal();showCreation();}]]),...narrativeMessages().slice(-6).map(m=>el('div',m,'journal-entry'))]);}
async function compress(raw){return new Uint8Array(await new Response(new Blob([raw]).stream().pipeThrough(new CompressionStream('gzip'))).arrayBuffer());}
async function decompress(bytes){
 const stream=new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip')).getReader();const chunks=[];let total=0;
 while(true){const {done,value}=await stream.read();if(done)break;total+=value.byteLength;if(total>128*1024*1024){await stream.cancel();throw new Error('This checkpoint is too large.');}chunks.push(value);}
 const out=new Uint8Array(total);let offset=0;for(const chunk of chunks){out.set(chunk,offset);offset+=chunk.byteLength;}return out;
}
function metadata(s){return JSON.stringify({GameID:s.gameId,SaveVersion:8,GameVersion:BUILD,PlayerName:s.name,Zone:s.zone,Tick:s.tick,SavedAt:new Date().toISOString(),Build:s.build});}
async function saveCheckpoint(internal=false){
 if(!state||state.dead||state.faulted||!storageReady||(busy&&!internal))return;
 if(saving)return saving;
 const snapshot=state;status('Saving…');
 saving=(async()=>{
  try{
   const raw=await invoke('Checkpoint'),payload=await compress(raw);
   await store.commit(snapshot.gameId,'Quick',payload,metadata(snapshot));
   dirty=0;lastSaveTick=snapshot.tick;status('Saved on this device');
   const at=checkpoints.findIndex(c=>c.gameId===snapshot.gameId&&c.slot==='Quick');const saved={gameId:snapshot.gameId,slot:'Quick',payload,metadata:metadata(snapshot)};
   if(at<0)checkpoints.push(saved);else checkpoints[at]=saved;
   try{localStorage.setItem('ooo.last-game',snapshot.gameId);}catch{}
  }catch(error){status('Save failed — export a backup',true);toast(error.message||error);throw error;}
  finally{saving=null;}
 })();return saving;
}
function latestCheckpoint(id){
 const records=checkpoints.filter(c=>c.slot==='Quick'&&(!id||c.gameId===id));
 if(!id){let preferred;try{preferred=localStorage.getItem('ooo.last-game');}catch{}const remembered=records.find(c=>c.gameId===preferred);if(remembered)return remembered;}
 return records.sort((a,b)=>String(JSON.parse(b.metadata).SavedAt||'').localeCompare(String(JSON.parse(a.metadata).SavedAt||'')))[0];
}
async function continueGame(id){
 if(busy)return;busy=true;let candidate=null,published=false;
 try{
  if(saving)await saving;
  candidate=new CooBrowserSaveStore();const found=await candidate.hydrate();
  const matches=found.filter(c=>c.slot==='Quick'&&(!id||c.gameId===id));
  let preferred;try{preferred=localStorage.getItem('ooo.last-game');}catch{}
  const saved=(!id&&matches.find(c=>c.gameId===preferred))||matches.sort((a,b)=>String(JSON.parse(b.metadata).SavedAt||'').localeCompare(String(JSON.parse(a.metadata).SavedAt||'')))[0];
  if(!saved)throw new Error('No saved checkpoint was found.');
  showLoading('Returning to Ooo…','Reading your last checkpoint.');await runtime();
  const raw=await decompress(saved.payload);const restored=await invoke('Restore',raw);
  const previous=store;store=candidate;checkpoints=found;storageReady=true;published=true;
  store.onUnavailable=message=>{storageReady=false;status('Storage unavailable — reload',true);toast(message);};
  try{(await previous.open()).close();}catch{}
  localMenu=false;playState(restored);lastSaveTick=state.tick;dirty=0;render();
  status(saved.recovered?'Recovered previous checkpoint':'Checkpoint restored');
 }catch(error){toast(error.message||error);status('Could not restore checkpoint',true);}
 finally{if(candidate&&!published)try{(await candidate.open()).close();}catch{}busy=false;hideLoading();}
}
let builds=[];
async function showCreation(){
 if(busy)return;
 $('start-screen').hidden=true;$('game-screen').hidden=true;$('creation-screen').hidden=false;document.body.classList.remove('playing');$('menu-button').hidden=true;localMenu=false;target=null;$('overlay').hidden=true;
 $('world-seed').value=String(1+crypto.getRandomValues(new Uint32Array(1))[0]%2147483646);
 if(!builds.length){
  try{builds=await(await fetch('builds.json')).json();builds=builds.Builds||builds;}
  catch{showLoading('Preparing your expedition…','Loading the available paths.');try{builds=JSON.parse(await invoke('Catalog'));}finally{hideLoading();}}
 }
 renderBuilds();window.scrollTo(0,0);
}
const symbols={duelist:'†',breaker:'♜',stormcaller:'ϟ',bombardier:'✺',warden:'◇',hurler:'⌁',classic:'✧'};
function renderBuilds(){
 $('builds').replaceChildren(...builds.map(build=>{
  const card=button('',()=>{selectedBuild=build.Id;renderBuilds();},'build-card');card.setAttribute('aria-pressed',String(build.Id===selectedBuild));
  card.append(el('span',symbols[build.Id]||'✦','build-glyph'),el('h3',build.Name),el('p',build.Tagline),el('small',build.Weakness));
  if(build.Id===selectedBuild)card.append(el('span','●','selected-dot'));return card;
 }));
}
$('new-button').onclick=showCreation;$('continue-button').onclick=()=>continueGame();
$('creation-back').onclick=()=>{$('creation-screen').hidden=true;if(state){playState(state);}else $('start-screen').hidden=false;};
$('creation-form').addEventListener('submit',async event=>{
 event.preventDefault();if(busy||!storageReady)return;busy=true;$('enter-world').disabled=true;
 try{
  if(saving)await saving;
  const seed=Number($('world-seed').value);if(!Number.isInteger(seed)||seed<1||seed>2147483647)throw new Error('Use a world seed between 1 and 2147483647.');
  showLoading('Opening the world…','The first visit downloads the game. Your expedition runs here, in your browser.');
  await runtime();text('loading-detail','Growing the glade and meeting its inhabitants…');
  localMenu=false;playState(await invoke('NewGame',selectedBuild,seed,$('player-name').value));dirty=1;lastSaveTick=-1;
  await saveCheckpoint(true);map.center();
 }catch(error){text('creation-status',error.message||error);toast(error.message||error);}
 finally{busy=false;$('enter-world').disabled=false;hideLoading();}
});
function download(bundle,name){const url=URL.createObjectURL(new Blob([JSON.stringify(bundle)],{type:'application/json'}));const link=el('a');link.href=url;link.download=name;link.click();setTimeout(()=>URL.revokeObjectURL(url),60000);}
function base64(bytes){let binary='';for(let i=0;i<bytes.length;i+=32768)binary+=String.fromCharCode(...bytes.subarray(i,i+32768));return btoa(binary);}
async function exportBackup(){
 if(busy||saving)return;busy=true;
 try{
  if(state&&!state.dead&&!state.faulted){
   const payload=await compress(await invoke('Checkpoint'));
   download({format:'caves-of-ooo-backup',version:1,buildId:BUILD,checkpoints:[{gameId:state.gameId,slot:'Quick',payloadBase64:base64(payload),metadata:metadata(state)}]},'caves-of-ooo-checkpoint.json');
  }else download(await store.exportBackup(BUILD),'caves-of-ooo-checkpoint.json');
  toast('Checkpoint exported. Keep it somewhere safe.');
 }catch(error){toast(error.message||error);}finally{busy=false;}
}
$('restore-file').onclick=()=>{if(!busy)$('import-file').click();};
$('import-file').onchange=async event=>{
 const file=event.target.files?.[0];if(!file||busy)return;busy=true;
 try{
  if(state)throw new Error('Reload this page before importing a different expedition.');
  if(file.size>128*1024*1024)throw new Error('This backup is too large.');
  const bundle=JSON.parse(await file.text());
  if(bundle.format!=='caves-of-ooo-backup'||bundle.buildId!==BUILD||!Array.isArray(bundle.checkpoints)||bundle.checkpoints.length<1||bundle.checkpoints.length>100)throw new Error('Choose a checkpoint exported from this browser edition.');
  showLoading('Reading your checkpoint…','Checking the expedition before importing it.');await runtime();
  // Validate the full simulation graph before replacing any durable record.
  for(const cp of bundle.checkpoints){const bytes=Uint8Array.from(atob(cp.payloadBase64),c=>c.charCodeAt(0));const candidate=JSON.parse(await invoke('Restore',await decompress(bytes)));if(candidate.gameId!==cp.gameId)throw new Error('Checkpoint identity does not match its contents.');}
  await store.importBackup(bundle,BUILD);checkpoints=await store.hydrate();$('continue-button').hidden=false;
  text('startup-status','Checkpoint imported. Continue when you’re ready.');toast('Checkpoint imported.');
 }catch(error){toast(error.message||error);text('startup-status',error.message||error);}
 finally{busy=false;event.target.value='';hideLoading();}
};
async function openStorage(){
 try{checkpoints=await store.hydrate();storageReady=true;const saved=latestCheckpoint();$('continue-button').hidden=!saved;
  if(saved){const info=JSON.parse(saved.metadata);$('continue-button').firstChild.textContent=`Continue ${clean(info.PlayerName||'expedition')} `;text('startup-status',`Checkpoint found · ${clean(info.Build||'wayfarer')} · tick ${info.Tick||0}`);}else text('startup-status','Your expedition is saved in this browser. Export a checkpoint to carry it elsewhere.');
  store.onUnavailable=message=>{storageReady=false;status('Storage unavailable — reload',true);toast(message);};
 }catch(error){text('startup-status','Saved expeditions could not be read. '+(error.message||error));$('new-button').disabled=true;status('Storage unavailable',true);const recover=button('Export recovery data',async()=>{try{download(await store.exportRecovery(BUILD),'caves-of-ooo-recovery.json');}catch(e){toast(e.message);}});$('startup-status').after(recover);}
}
openStorage();
const moves={ArrowUp:[0,-1],ArrowDown:[0,1],ArrowLeft:[-1,0],ArrowRight:[1,0],h:[-1,0],j:[0,1],k:[0,-1],l:[1,0],y:[-1,-1],u:[1,-1],b:[-1,1],n:[1,1],'1':[-1,1],'2':[0,1],'3':[1,1],'4':[-1,0],'6':[1,0],'7':[-1,-1],'8':[0,-1],'9':[1,-1]};
document.addEventListener('keydown',event=>{
 if(/^(INPUT|TEXTAREA|SELECT)$/.test(event.target.tagName)||event.ctrlKey||event.metaKey||event.altKey||event.repeat)return;
 if(event.key==='Escape'){event.preventDefault();if(target)clearTarget();else closePanel();return;}
 if(!state||$('game-screen').hidden)return;
 if(event.key==='F5'){event.preventDefault();saveCheckpoint().catch(()=>{});return;}
 if(!$('overlay').hidden)return;
 if(moves[event.key]){event.preventDefault();const [x,y]=moves[event.key];if(!target)act('step',{x,y});return;}
 if(event.key==='.'||event.key==='5'){event.preventDefault();if(!target)act('wait');return;}
 const panels={i:'inventory',a:'abilities',e:'nearby',K:'skills',J:'journal',m:'map',c:'character'};
 if(panels[event.key]){event.preventDefault();act('panel',{value:panels[event.key]});return;}
 if(event.key==='>'||event.key==='<'){event.preventDefault();act('stairs',{value:event.key==='<'?'up':'down'});}
});
document.addEventListener('visibilitychange',()=>{pointer=null;if(document.hidden){if(busy)saveAfterAction=true;else if(dirty)saveCheckpoint().catch(()=>{});}});
window.addEventListener('pagehide',()=>{if(busy)saveAfterAction=true;else if(dirty)saveCheckpoint().catch(()=>{});});
window.addEventListener('resize',()=>{if(pointer){const id=pointer.id;pointer=null;if($('world-canvas').hasPointerCapture(id))$('world-canvas').releasePointerCapture(id);}map.draw();});
// Cache only this static release; a waiting update never replaces a running game.
if('serviceWorker'in navigator)navigator.serviceWorker.register('sw.js').catch(()=>{});
