import json, hashlib, shutil, struct, datetime, html
from pathlib import Path

R = Path(__file__).resolve().parent.parent
COMMIT = '5bb8e249d1fc69d9213f684bace7a8628e11b4ad'
def read(p): return json.loads((R/p).read_text())
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p, d): (R/p).write_text(json.dumps(d, indent=2, ensure_ascii=False)+'\n')
def known(v): return 'unknown' if v is None else v
def as_list(v): return v if isinstance(v,list) else [v] if v else []
def location_prose(loc):
    w=loc.get('logicalWorld');p=loc.get('playerTile')
    world=f"world ({w.get('x')}, {w.get('y')}), depth {w.get('depth','unknown')}" if isinstance(w,dict) else f"world {w}"
    player=f"player ({p.get('x')}, {p.get('y')})" if isinstance(p,dict) else f"player {p}"
    if isinstance(p,dict) and p.get('coordinateSystem'): player+=' in '+p['coordinateSystem']+' coordinates'
    if loc.get('lookTile'): player+='; Look cursor ('+str(loc['lookTile']['x'])+', '+str(loc['lookTile']['y'])+') in raw map coordinates'
    return str(loc.get('biome'))+'; '+str(loc.get('zone'))+'; formation '+str(loc.get('formation'))+'; '+world+'; '+player+'.'
def viewing_prose(e):
    v=e['viewingConditions']
    if e['session'] in ['manual-64','journal-64']:
        g=v['graphics']; c=v['camera']
        return (f"Native 3D setting {'on' if g['Enabled'] else 'off'}, {'low' if g['LowDetail'] else 'full'} detail, world resolution scale {g['WorldResolutionScale']}, shadows {'on' if g['ShadowsEnabled'] else 'off'}, effects {g['fx']}, CRT {'on' if g['crt'] else 'off'}. Gameplay zoom setting {c['GameplayZoomMultiplier']}; full-screen readers may use a separate UI camera. World time {v['dayBand']} at tick {v['tick']}; zone ambient {v['zoneAmbient']}. {v['visibility']}. Exact camera rectangles and local-light limits are in the manifest.")
    if e['id'].startswith('HUD-'):
        return 'Native 720p, ReferenceGlade scene zoom baseline0.82, Dawn HUD. No full-reveal override. Exact runtime zoom, quality values and local lighting were not emitted by this route; they remain unknown.'
    if e['id'].startswith('SKL-'):
        return 'Native 800p full-screen skills UI. Per-frame world lighting, camera transforms and quality values are unknown and not needed to infer the visible panel text. No world visibility conclusion is drawn from the panel.'
    g=v['graphics']
    return f"Scene gameplay zoom baseline {g.get('sceneGameplayZoomBaseline','unknown')}, not a numeric runtime camera measurement. Time {v.get('dayBand','unknown')}; tick {v.get('tick','unknown')}. {g.get('fullRevealBasis','Visibility unknown')} Exact per-frame scale, quality, ambient and camera transform were not recorded."

entries=[]
sessions={}
base_session={
    'sourceCommit':COMMIT,'unity':'6000.3.4f1','platform':'macOS / Metal / OSXEditor',
    'surface':'Unity Editor Game view; no standalone or physical Steam Deck',
    'worldSeed':64,'saveIsolation':'NativeSaveIsolation; fresh private disposable root',
    'sourceEvidence':['evidence/source-baseline.json','evidence/assembly-identity.json'],
    'newProductionHarness':False,
}
for sid in ['manual-64','journal-64']:
    sessions[sid]={**base_session,'id':sid,'scene':'Assets/Scenes/Main/SampleScene.unity',
      'input':'CUA keyboard and mouse; no connected gamepad',
      'build':('New Game > Classic chosen through UI; original dagger ID2704 equipped' if sid=='manual-64' else 'Original audit starter kit with six powers; build chooser suppressed, dagger ID2700 in inventory, no equipped weapon at journal capture'),
      'alterations':['Native audit seed64 selected before Play','Temporary native Game view resolutions1280x800 and1280x720','Runtime-only RevealEntire3DZone=false after baseline menus; serialized scene untouched'],
      'noGameplayGrants':'No stats, items, currency, invulnerability, actor teleports, AI suppression, clock freeze or direct gameplay event calls',
      'visibility':'Source FovRadius999 preserved; ordinary line of sight after runtime full-reveal override. Baseline menu exceptions listed per image.',
      'normalGameplayZoomMultiplier':1.2,
      'graphicsBaseline':{'native3D':True,'lowDetail':False,'worldResolutionScale':1,'shadows':True,'spellEffects':'Full','animationSpeed':1,'flashIntensity':0.15,'shakeIntensity':0,'CRT':False},
      'route':('Normal world-map travel:11,10→12,10→17,5→16,6→7,5→6,6→5,5→3,10→1,14; no teleportation. Destinations selected with source knowledge, not an unaided discovery test.' if sid=='manual-64' else 'Normal world-map travel11,10→Gantry7,8, then local movement, actual Padok quest acceptance and scribe report recording.'),
      'cleanup':'evidence/restoration.json'}

for fn in ['manual-entries.json','journal-entries.json']:
    for e in read('evidence/'+fn)['entries']:
        e=dict(e); raw=read(e['rawMetadata']); image=R/e['image']; sid=raw['session']
        if e['id'].startswith('BASE-'):
            dest=R/'screenshots/staged'/image.name
            if image.exists(): shutil.move(str(image),str(dest))
            e['image']=str(dest.relative_to(R)); image=dest
        e['session']=sid
        e['classification']='full-reveal baseline menu supplement' if e['id'].startswith('BASE-') else 'ordinary input journey'
        e['set']='supplement' if e['id'].startswith('BASE-') or e['dimensions'][1]==720 else 'primary'
        e['captureTimestampUtc']=raw['timestampUtc'];e['timestampBasis']='Read in same end-of-frame callback as original capture'
        e['location']={'zone':raw['zone'],'biome':raw['biome'],'namedPlace':raw.get('poi') or 'unknown / no POI recorded','formation':e.get('specializedFormation',raw.get('formation')),'logicalWorld':raw['logical'],'playerTile':{k:raw['player'][k] for k in ['x','y']}}
        if e['id']=='MAP-01-drowned-ledger':
            e['location'].update(logicalWorld={'x':17,'y':5,'depth':0},playerTile={'x':47,'y':8,'coordinateSystem':'raw world-map tile'},lookTile={'x':46,'y':8,'logicalX':16,'logicalY':5},enterSiteUses='Player logical17,5, not look cursor')
        e['viewingConditions']={'output':raw['width'].__str__()+'x'+str(raw['height']),'graphics':raw['graphics'],'camera':raw['camera'],'activeCameras':raw['cameras'],'renderer':raw['renderer'],'dayBand':raw['dayBand'],'tick':raw['tick'],'zoneAmbient':raw['ambient'],'ambientTint':raw['ambientTint'],'localLightSources':'unknown; visible emitting objects only if identified in caption','uiScale':'No separate UI-scale override; per-frame cameras/pixelRects recorded','visibility':'Full-reveal scene background' if e['id'].startswith('BASE-') else 'Full reveal off; FOV radius999 retained; UI can hide native world presenter'}
        e['player']=raw['player'];e['input']=raw['input'];e['inputState']=raw['state']
        e['alterations']=sessions[sid]['alterations'] if not e['id'].startswith('BASE-') else ['Isolated seeded boot marker','Temporary native Game view size','Inherited serialized full-reveal background; not ordinary discovery evidence']
        e['nextAction']='Waiting for ordinary game input; see sequence and related images. No action fired for metadata collection.'
        e['reviewEvidence']='evidence/base-review.md' if e['id'].startswith('BASE-') else ('evidence/journal-review.md' if fn.startswith('journal') else 'evidence/manual-first-review.md')
        e['visuallyReviewed']=True
        if e['id']=='OVE-01-pilgrimage-rim':
            e['visibleFindings']=['The ordinary viewport is sparse, with subtle ground marks and little landmark contrast. This does not establish missing content: Overwrit emptiness is deliberate.']
        entries.append(e)

native=read('evidence/native-routes-entries.json')
contexts=read('evidence/native-caption-context.json') if (R/'evidence/native-caption-context.json').exists() else {}
for old in native:
    e=dict(old);route=e['nativeRoute'];sid='native-'+route['runId']
    sessions.setdefault(sid,{**base_session,'id':sid,'scene':e['scene'],'entryPoint':route['entryPoint'],'runId':route['runId'],'report':route['report'],'alterations':route['alterations'],'input':route['inputMethod'],'routeComplete':route['complete'],'failures':route['failures'],'unexpectedErrors':route['unexpectedErrors'],'settings':'Not measured separately per capture; no persisted-setting changes in route. Do not substitute manual-session camera or per-frame settings.','cleanup':'Existing route isolation teardown plus evidence/restoration.json'})
    e.update(session=sid,image=e['file'],set='primary' if e['classification']=='controlled' else 'supplement',rawMetadata='evidence/'+e['id']+'.json',captureTimestampUtc=e.get('timestampUtc') or e.get('fileWrittenUtc'),question='What does this real gameplay state make visible?',plainCaption=e['caption'])
    e['location']={'zone':e['zone'],'biome':e['biome'],'formation':known(e.get('formation')),'logicalWorld':e['logical'],'playerTile':{'x':e['player']['x'],'y':e['player']['y']},'namedPlace':'See caption; not independently emitted by route'}
    e['viewingConditions']={'output':str(e['resolution']['width'])+'x'+str(e['resolution']['height']),'graphics':e['graphics'],'dayBand':known(e.get('dayBand')),'tick':known(e.get('tick')),'uiScale':'unknown per-frame; native PNG dimensions verified','visibility':e['graphics']['fullRevealBasis']}
    e['dimensions']=[e['resolution']['width'],e['resolution']['height']];e['input']=route['inputMethod'];e['alterations']=[route['alterations']]
    e['limits']=['Exact per-frame quality, numeric camera/light and render-texture measurements were not emitted; unknowns are retained.','Desktop synthetic-input route, not physical Steam Deck testing.']
    e['relatedIds']=[];e['immediateSequence']=[e['caption']];e['contents']=[e['caption']];e['nextAction']='Capture moment in existing input route; subsequent sequence recorded in raw report.'
    if isinstance(contexts,list): ctx=next((c for c in contexts if c.get('id')==e['id']),{})
    else: ctx=contexts.get(e['id'],{})
    e.update(ctx)
    entries.append(e)

hud=read('evidence/hud720-entries.json');sessions[hud['session']['id']]={**base_session,**hud['session']}
for old in hud['entries']:
    e=dict(old);s=hud['session']
    e.update(set='supplement',captureTimestampUtc=e['timestampUtc'],location={'zone':s['zone'],'biome':s['biome'],'formation':s['formation'],'namedPlace':'Reference Glade','logicalWorld':{'x':11,'y':10,'depth':0},'playerTile':{'x':40,'y':12}},viewingConditions={'output':'1280x720','graphics':s['graphicsSettings'],'camera':s['camera'],'visibility':s['visibility'],'timeAndLighting':s['timeAndLighting'],'uiScale':'Native720p; reported camera pixel rectangles in session'},input=s['input'],alterations=s['alterations'],visuallyReviewed=True,reviewEvidence='evidence/hud720-review.md',nextAction='Waiting in the shown normal input state; route next step in report.')
    entries.append(e)

skills=read('evidence/skills800-entries.json');sid='skills-'+skills['runId']
sessions[sid]={**base_session,'id':sid,**{k:v for k,v in skills.items() if k!='entries'},'build':'Original starter plus staged10SP; purchases and Vault assignment through real UI','input':skills['sourceAudit']['input'],'settings':'Full-screen UI; inherited ReferenceGlade zoom0.82 and FOV999/revealfalse. Per-frame graphics and local light unknown.'}
for old in skills['entries']:
    e=dict(old);src=R/e['image'];dst=R/'screenshots/staged'/(e['id']+'.png');shutil.copy2(src,dst)
    e.update(image=str(dst.relative_to(R)),sourceOriginal=str(src.relative_to(R)),session=sid,set='supplement',captureTimestampUtc=datetime.datetime.fromtimestamp(src.stat().st_mtime,datetime.timezone.utc).isoformat(),timestampBasis='Original PNG modification time; no per-frame UTC callback value',location={'zone':'Overworld.11.10.0','biome':'Spread','namedPlace':'Reference Glade','formation':'authored reference site','logicalWorld':{'x':11,'y':10,'depth':0},'playerTile':'unknown; full-screen skills view'},viewingConditions={'output':'1280x800','graphics':'unknown per-frame; route does not change persisted graphics preferences','camera':'Full-screen skills renderer; world scene zoom baseline0.82 is not this UI scale','uiScale':'native1280x800; exact internal UI transform unknown','timeAndLighting':'not applicable to full-screen panel; underlying world not emitted','visibility':'No reveal override; full-screen menu'},input=skills['sourceAudit']['input'],alterations=[skills['sourceAudit']['stagedState']],visuallyReviewed=True,reviewEvidence='evidence/skills800-review.md',nextAction='Navigate the real skills menu; selected detail page and warnings remain authoritative.')
    entries.append(e)

for e in entries:
    if e['id']=='SKL-03-pyromancy-final-page':
        e['alterations']=['10 SP staged at route start; at this frame only Acrobatics and Vault have been bought through UI, with8 SP remaining and Vault assigned to slot ten. Later Long Blades/Deflect/Riposte/En Garde purchases belong to later route frames.']
    p=R/e['image'];w,h=struct.unpack('>II',p.read_bytes()[16:24]);assert [w,h]==e['dimensions'],e['id']
    e['sha256']=digest(p);e['sourceCommit']=COMMIT
    e.setdefault('overrides',e.get('alterations',[]))
    e.setdefault('limits',[]);e.setdefault('relatedIds',[])
    for field in ['contents','immediateSequence','alterations','limits']: e[field]=as_list(e.get(field))

ids={e['id'] for e in entries};assert len(ids)==len(entries)
for e in entries:
    assert e['session'] in sessions
    assert all(x in ids for x in e['relatedIds']), (e['id'],e['relatedIds'])
    assert e['visuallyReviewed']
manifest={'schema':'caves-of-ooo-graphics-review-v1','createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source':{'commit':COMMIT,'branch':'main','remoteMainAtStartAndEnd':COMMIT,'trackedTreeCleanBeforeAndAfter':True,'worktreeQualification':'Many pre-existing untracked reports/screenshots; whole worktree was not clean. None were removed. Fresh native route reports also written by existing workflows.','assemblyEvidence':'evidence/assembly-identity.json','noProductionChanges':True,'noCommitOrPush':True},'counts':{'primary':sum(e['set']=='primary' for e in entries),'supplement':sum(e['set']=='supplement' for e in entries),'allOriginals':len(entries)},'unknownConvention':'unknown or null means not measured; source baseline is not a per-frame measurement. Raw snapshots may include visible-in-zone entities outside the camera viewport.','sessions':sessions,'images':entries,'restoration':'evidence/restoration.json','coverage':'README.md','findings':'findings.md','excludedEvidence':'Raw native route folders and excluded-* retain non-selected outputs for provenance. They are not additional reviewed screenshot entries.'}
write('manifest.json',manifest)

groups=[('Regions and settlements',('SPR','SOD','GRO','SET','STU','OVE','BEA')),('Items, equipment and people',('ITEM','NPC')),('Underground, work and state changes',('CAV','LAI','WRK')),('Combat and targeting',('CMB','THR','HUD')),('Dialogue, journal and navigation',('UI','JRN','MAP')),('Skills, trade and baseline menus',('SKL','TRD','BASE'))]
lines=['# Screenshot captions','','All images are fresh lossless game-frame originals. Click an image ID to open its full-resolution PNG. Conditions and limits belong to each image; compare within the same session before comparing across sessions. The [manifest](manifest.json) supplies hashes and raw evidence.','']
for title,prefixes in groups:
    lines+=['## '+title,'']
    for e in entries:
        if e['id'].split('-')[0] not in prefixes: continue
        loc=e['location'];v=e['viewingConditions']
        lines+=['### '+e['id'],'',f"[Open original]({e['image']}) · {e['dimensions'][0]}×{e['dimensions'][1]} · **{e['set']} / {e['classification']}**",'',e['plainCaption'],'',f"**Review question:** {e['question']}",'', '**What is present:** '+'; '.join(e.get('contents',[]))+'.','', '**Sequence:** '+' → '.join(e.get('immediateSequence',[]))+'.','', '**Location:** '+location_prose(loc),'',f"**Conditions:** session `{e['session']}`; {e.get('captureTimestampUtc','unknown')}; macOS Unity Editor, main `{COMMIT[:8]}`. Input: "+(e['input'].get('method','unknown') if isinstance(e['input'],dict) else e['input'])+'.','', '**Viewing details:** '+viewing_prose(e),'', '**Capture alterations:** '+'; '.join(e.get('alterations',[]))+'.','', '**Limits:** '+' '.join(e.get('limits',[])),'', '**Next:** '+e.get('nextAction','unknown'),'']
        if e.get('relatedIds'): lines+=['**Compare:** '+', '.join(f'[{x}](#{x.lower()})' for x in e['relatedIds']), '']
        lines+=['**Evidence:** ['+e['rawMetadata']+']('+e['rawMetadata']+'). SHA-256: `'+e['sha256']+'`.','']
(R/'captions.md').write_text('\n'.join(lines))

cards=[]
for e in entries:
    cards.append('<article><h2>'+html.escape(e['id'])+'</h2><p>'+html.escape(e['set']+' · '+e['classification'])+'</p><a href="'+e['image']+'"><img loading="lazy" src="'+e['image']+'" alt="'+html.escape(e['plainCaption'],quote=True)+'"></a><p>'+html.escape(e['plainCaption'])+'</p><p>'+html.escape(e['question'])+'</p></article>')
(R/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Caves of Ooo graphics review</title><style>body{background:#111b1d;color:#e5eeea;font:16px system-ui;margin:24px}a{color:#9de3dc}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(400px,1fr));gap:24px}article{padding:16px;background:#1a292c;border:1px solid #42605b}img{width:100%;height:auto}h2{font-size:18px}</style><h1>Caves of Ooo · fresh graphics review</h1><p>Navigation only. Click each image for its original native-resolution PNG; reduced browser previews are not evidence for text readability.</p><p><a href="README.md">README</a> · <a href="captions.md">Full captions</a> · <a href="manifest.json">Manifest</a> · <a href="findings.md">Findings</a></p><main>'+''.join(cards)+'</main>')
print(json.dumps(manifest['counts']))
