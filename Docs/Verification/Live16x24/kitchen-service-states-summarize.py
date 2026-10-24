from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3]; dest=root/'Docs/Verification/Live16x24'
def read(name): return json.loads((dest/name).read_text())
phases=['idle','working','working-reloaded','ready','ready-reloaded','collected','collected-reloaded']
jobs={p:read('kitchen-service-states-'+p+'-job.json') for p in phases}
w,r,c=jobs['working'],jobs['ready'],jobs['collected']
keys=['tick','player','pan','cook','job','escrow','pickup','inventory']
checks={
 'real_paid_escrow':w['player']['drams']==48 and w['cook']['drams']==102 and [(i['blueprint'],i['count'],i['inInventoryId']) for i in w['escrow']['rows']]==[('Emberwheat',2,'7209'),('ClaspbeanPulp',1,'7209')],
 'working_reload_equal_recorded_fields':all(w[k]==jobs['working-reloaded'][k] for k in keys),
 'due_120_ticks':r['tick']==w['job']['dueTick']==5230 and w['job']['dueMinusStart']==120,
 'unique_ready_output':r['pickup']['rowCount']==1 and r['job']['outputId']=='9249' and r['pickup']['rows'][0]['inInventoryId']=='7210',
 'ready_output_survives_reload_and_two_steps':r['pickup']==jobs['ready-reloaded']['pickup'] and all(r['job'][k]==jobs['ready-reloaded']['job'][k] for k in r['job'] if k!='remainingTicks'),
 'collected_actual_same_meal':c['job']['state']=='Idle' and c['pickup']['rowCount']==0 and any(i['id']=='9249' and i['inInventoryId']=='2696' for i in c['inventory']),
 'collected_reload_equal_recorded_fields':all(c[k]==jobs['collected-reloaded'][k] for k in keys),
 'flat_current_graph_all':all(j['flat'] and j['currentGraphMatches'] for j in jobs.values())}
expected={'idle':('east-kitchen',80,'east-kitchen',112),'working':('kitchen-service',0,'east-kitchen',112),'working-reloaded':('kitchen-service',0,'east-kitchen',112),'ready':('east-kitchen',80,'kitchen-service',16),'ready-reloaded':('east-kitchen',80,'kitchen-service',16),'collected':('east-kitchen',80,'east-kitchen',112)}
for phase,e in expected.items():
 rows={x['blueprint']:x for x in read('kitchen-service-states-'+phase+'-tiles.json')['rows']};a,b=rows['ConnectedBatchPan'],rows['ConnectedKitchenPickup']
 checks['submitted_'+phase]=(a['texture'],a['rect'][0],b['texture'],b['rect'][0])==e and all(x['rect'][2:]==[16,24] and x['ppu']==16 for x in rows.values())
assert all(checks.values()),checks
from PIL import Image
captures=[]
for p in sorted(dest.glob('kitchen-service-states-*.png')):
 size=Image.open(p).size;assert size==(1280,800)
 captures.append({'file':p.name,'width':size[0],'height':size[1],'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
latest={}
for name in ['kitchen-service-regression.xml','kitchen-service-quiet-final.xml']:
 for t in ET.parse(dest/name).getroot().iter('test-case'):latest[t.get('fullname')]=t.get('result')
summary={'kind':'ordinary_keyboard_service_cue_acceptance','seed':65,'zone':'Overworld.12.11.0','checks':checks,'captures':captures,'nativeDistinct':{'passed':list(latest.values()).count('Passed'),'failed':list(latest.values()).count('Failed'),'skipped':list(latest.values()).count('Skipped')},'bounds':['Real keyboard resume of actually played repaired checkpoint; no gameplay grants, time setters or forced jobs.','Working and collected reload compare named recorded fields; Ready replay has two subsequent movement turns.','Working-2x temporarily changes view only; reload returns saved default1.2.','Portrait/window and tilemap evidence establish sampled rendering, not every state, packaged run or physical Steam Deck.','Five GC allocation tests explicitly skipped; separate quiet-frame tests pass.']}
(dest/'kitchen-service-states-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'checks':len(checks),'captures':len(captures),'nativeDistinct':summary['nativeDistinct']}))
