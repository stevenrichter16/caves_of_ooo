"""Actual geometry/pose/material readability gate at the retained gameplay scale.

This is an art-data gate, not a claim about native GPU pixels or frame time.
Run before and after generation; controls damage real asset arrays, not receipts.
"""
from pathlib import Path
import argparse, copy, hashlib, json, math
ROOT=Path(__file__).resolve().parent
PPU=33.38
COT=1/math.tan(math.radians(56))
FAMILIES=('ember_spit','flaming_hands','jet_blast','ground_surge','rime_grip','calm')

def rotate(v,q):
 x,y,z,w=q;px,py,pz=v;tx=2*(y*pz-z*py);ty=2*(z*px-x*pz);tz=2*(x*py-y*px)
 return (px+w*tx+y*tz-z*ty,py+w*ty+z*tx-x*tz,pz+w*tz+x*ty-y*tx)
def points(mesh,pose):
 vs=mesh['vertices'];out=[]
 for i in range(0,len(vs),3):
  p=rotate([vs[i+k]*pose['scale'][k] for k in range(3)],pose['rotation']);out.append(tuple(p[k]+pose['position'][k] for k in range(3)))
 return out

def islands(mesh):
 """Weld repeated triangle corners, then count independent actual mesh islands."""
 verts=mesh['vertices'];keys=[tuple(round(v,5) for v in verts[i:i+3]) for i in range(0,len(verts),3)]
 parents={k:k for k in keys}
 def find(k):
  while parents[k]!=k:parents[k]=parents[parents[k]];k=parents[k]
  return k
 for i in range(0,len(mesh['triangles']),3):
  a,b,c=[keys[n] for n in mesh['triangles'][i:i+3]];ra=find(a)
  for k in (b,c):parents[find(k)]=ra
 groups={}
 for key in keys:groups.setdefault(find(key),set()).add(key)
 return [tuple(sum(p[k] for p in group)/len(group) for k in range(3)) for group in groups.values()]

def validate(data):
 checks=[];meshes={m['id']:m for m in data['meshes']};mats=data['materials']
 def need(name,ok,actual):checks.append(dict(name=name,passed=bool(ok),actual=actual))
 need('All seven study entries remain',len(data['studies'])==7,len(data['studies']))
 validmats=all(isinstance(m.get('glow'),bool) and isinstance(m.get('emission'),(int,float)) and math.isfinite(m['emission']) and 0<=m['emission']<=1 for m in mats)
 need('Explicit finite material emission and glow roles',validmats,len(mats))
 alphaok=True;glowcount=0;solidcount=0
 for m in data['meshes']:
  mat=mats[m['materialIndex']];cols=m.get('vertexColors',[])
  if cols:
   alphaok &= len(cols)==len(m['vertices'])//3*4 and all(math.isfinite(v) and 0<=v<=1 for v in cols)
   alphaok &= len(cols)%4==0 and all(abs(cols[i+k]-1)<1e-6 for i in range(0,len(cols),4) for k in range(3))
  if mat.get('glow'):
   glowcount+=1;alphaok &= bool(cols[3::4]) and min(cols[3::4])==0 and max(cols[3::4])>=.35
  else:solidcount+=1;alphaok &= not cols or all(abs(v-1)<1e-6 for v in cols[3::4])
 need('Actual soft halos have finite white-RGB alpha gradients',alphaok and glowcount>=6,dict(glowMeshes=glowcount,solidMeshes=solidcount))
 need('Local glow has enough static strength for actual game scale',all(m.get('emission',0)>=.5 for m in mats if m.get('glow')) and glowcount>=6,[m.get('emission') for m in mats if m.get('glow')])
 for s in data['studies']:
  sid=s['id'];ps=s['pieces'];contact=int(math.ceil(s['contactFrame']));peak=contact+8;primary=[];motes=[];cohorts=[];boundsok=True;maxradius=0
  for p in ps:
   me=meshes[p['meshId']];mat=mats[me['materialIndex']]
   if p.get('visualBounds') not in ('CellSurface','AirborneDecoration'):boundsok=False
   if p['role'] in ('ConeCell','GroundCell','CropCell','ReactionCell'):
    for pose in p['poses']:
     for x,y,z in points(me,pose):
      if p.get('visualBounds','CellSurface')=='CellSurface':boundsok &= abs(x)<=.50001 and abs(z)<=.50001
      else:maxradius=max(maxradius,math.hypot(x,z));boundsok &= math.hypot(x,z)<=2.75001
   if '_motes_' in p['id']:
    centers=islands(me);cohorts.append(len(centers))
    if max(p['poses'][peak]['scale'])>.55:
     pose=p['poses'][peak]
     for c in centers:
      q=rotate([c[k]*pose['scale'][k] for k in range(3)],pose['rotation']);motes.append(tuple(q[k]+pose['position'][k] for k in range(3)))
   if 'RD_' in p['id'] and '_motes_' not in p['id'] and not mat.get('glow') and mat.get('emission',0)>=.55:
    primary.append(p)
  need(sid+' has explicit bounded surface/decorative ownership',boundsok,dict(maxAirborneCellRadius=maxradius))
  need(sid+' retains finite draw count',0<len(ps)<=144,len(ps))
  if sid not in FAMILIES:continue
  if sid=='ember_spit':
   # User superseded the shared contact crown with a flowing projectile.
   # Preserve other-family gates; test Ember by its actual role and phase.
   from audit_ember_identity import validate as ember_identity
   checks.extend(ember_identity(data,{'impact','tail','bounds','motion','budget'}))
   continue
  low=300
  need(sid+' has dense actual multi-island mote cohorts',len(motes)>=low and len(motes)<=448 and cohorts and all(8<=n<=16 for n in cohorts),dict(posedMotes=len(motes),visibilityBoundary='Geometry is active; occlusion and actual visible pixels require native/GPU review',cohortIslandCounts=cohorts))
  need(sid+' has bright connected defining surfaces',len(primary)>=2,len(primary))
  # Relative extents are calculated within owning semantic anchors. Cell studies
  # restore their explicit offsets, while target/projectile groups stay local.
  projected=[];world=[];contactsizes=[];holds=[]
  for p in primary:
   me=meshes[p['meshId']];pose=p['poses'][peak]
   offset=(p.get('lateralCell',0),0,p.get('forwardCell',0)) if p['anchor']=='Cell' else (0,0,s['authoredDistanceCells']) if p['anchor']=='Target' else (0,0,0)
   for x,y,z in points(me,pose):
    world.append((z+offset[2],x+offset[0],y));projected.append(((z+offset[2])*PPU,(x+offset[0]-y*COT)*PPU))
   scales=[max(v['scale']) for v in p['poses']];m=max(scales)
   contactsizes.append(scales[contact]/m if m else 0)
   holds.append(sum(v>=m*.94 for v in scales[contact:contact+25]))
  extent=max((max(v[k] for v in projected)-min(v[k] for v in projected) for k in (0,1)),default=0) if projected else 0
  minimum={'ember_spit':120,'flaming_hands':118,'jet_blast':140,'ground_surge':130,'rime_grip':118,'calm':125}[sid]
  need(sid+' defining envelope reads at retained gameplay scale',extent>=minimum,dict(projectedSpanPixels=round(extent,2),requiredPixels=minimum,pixelsPerCell=PPU))
  spans={};minor_spans={}
  for degrees in range(0,360,45):
   a=math.radians(degrees);coords=[((x*math.cos(a)-y*math.sin(a))*PPU,(x*math.sin(a)+y*math.cos(a)-z*COT)*PPU) for x,y,z in world]
   axes=[max(v[k] for v in coords)-min(v[k] for v in coords) for k in (0,1)] if coords else [0,0]
   spans[degrees]=max(axes);minor_spans[degrees]=min(axes)
  need(sid+' broad silhouette survives all eight cast directions',min(spans.values(),default=0)>=75,dict(projectedSpanPixels=spans,requiredMinimum=75))
  need(sid+' does not collapse to an edge-on sliver',min(minor_spans.values(),default=0)>=(18 if sid=='ground_surge' else 40),dict(projectedMinorAxisPixels=minor_spans,requiredMinimum=18 if sid=='ground_surge' else 40))
  need(sid+' primary forms readable at exact contact and held',contactsizes and min(contactsizes)>=.40 and max(holds,default=0)>=15,dict(minContactScaleFraction=min(contactsizes,default=0),longestStrongHoldFrames=max(holds,default=0)))
  need(sid+' clears new forms by original clear frame',all(max(p['poses'][min(110,int(math.ceil(s['clearFrame'])))]['scale'])<.002 for p in ps if 'RD_' in p['id']),s['clearFrame'])
  if sid!='ground_surge':
   deltas=[]
   for p in primary:
    if p['role']=='ProjectileHead':continue
    a=p['poses'][contact+8];b=p['poses'][contact+21];deltas.append(sum((a['position'][k]-b['position'][k])**2 for k in range(3))**.5+sum((a['rotation'][k]-b['rotation'][k])**2 for k in range(4))**.5)
   need(sid+' coherent primary moves during its strong hold',max(deltas,default=0)>.035,dict(maxTranslationPlusQuaternionDelta=max(deltas,default=0),threshold=.035))
 return checks

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--output',required=True);ap.add_argument('--input',default=str(ROOT/'runtime/starter_spell_library.json'));ap.add_argument('--controls',action='store_true');args=ap.parse_args()
 path=Path(args.input);data=json.loads(path.read_text());rows=validate(data);controls=[]
 if args.controls:
  def probe(name,mutate):
   d=copy.deepcopy(data);mutate(d);fails=[r['name'] for r in validate(d) if not r['passed']];controls.append(dict(name=name,passed=bool(fails),rejectedBy=fails))
  probe('Deleted actual particle cohorts rejected',lambda d:d['studies'][0].__setitem__('pieces',[p for p in d['studies'][0]['pieces'] if '_motes_' not in p['id']]))
  probe('Removed material self-light rejected',lambda d:[m.__setitem__('emission',0) for m in d['materials']])
  probe('Flattened actual glow alpha rejected',lambda d:[m.__setitem__('vertexColors',[1.0]*len(m.get('vertexColors',[]))) for m in d['meshes']])
  probe('Malformed actual RGBA length rejected',lambda d:next(m for m in d['meshes'] if m.get('vertexColors')).__setitem__('vertexColors',[1,1,1]))
  def spill(d):
   p=next(p for s in d['studies'] for p in s['pieces'] if p['role']=='ConeCell' and p.get('visualBounds')=='AirborneDecoration');p['poses'][30]['position']=[4,0,0]
  probe('Airborne radius spill rejected',spill)
  probe('Wrong ground-surface classification rejected',lambda d:next(p for s in d['studies'] for p in s['pieces'] if p['role']=='ConeCell' and p.get('visualBounds')=='AirborneDecoration').__setitem__('visualBounds','CellSurface'))
  def late(d):
   s=d['studies'][0]
   for p in s['pieces']:
    if 'RD_' in p['id']:
     for f in (math.floor(s['contactFrame']),math.ceil(s['contactFrame'])):p['poses'][f]['scale']=[0,0,0]
  probe('Invisible exact contact rejected',late)
  def flatten(d):
   ids={p['meshId'] for p in d['studies'][0]['pieces'] if 'RD_' in p['id']}
   for mesh in d['meshes']:
    if mesh['id'] in ids:
     for i in range(0,len(mesh['vertices']),3):mesh['vertices'][i]*=.01
   for piece in d['studies'][0]['pieces']:
    if piece['meshId'] in ids:
     for pose in piece['poses']:pose['position'][0]*=.01
  probe('Actual flattened Ember tail geometry rejected',flatten)
  def freeze_hold(d):
   study=d['studies'][0];frame=int(math.ceil(study['contactFrame']))+8
   for piece in study['pieces']:
    if 'RD_' not in piece['id'] or piece['role']=='ProjectileHead':continue
    fixed=piece['poses'][frame]
    for pose in piece['poses']:
     pose['position']=list(fixed['position']);pose['rotation']=list(fixed['rotation'])
  probe('Actual frozen primary follow-through rejected',freeze_hold)
  probe('Lingering new forms rejected',lambda d:next(p for p in d['studies'][0]['pieces'] if 'RD_' in p['id'])['poses'][int(math.ceil(d['studies'][0]['clearFrame']))].__setitem__('scale',[1,1,1]))
  controls.append(dict(name='Unchanged exported art accepted',passed=all(r['passed'] for r in rows)))
 report=dict(status='GREEN' if all(r['passed'] for r in rows+controls) else 'RED',librarySha256=hashlib.sha256(path.read_bytes()).hexdigest(),sourceBlendSha256=data['sourceBlendSha256'],assertions=len(rows+controls),passed=sum(r['passed'] for r in rows+controls),checks=rows,controls=controls)
 Path(args.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('checks','controls')},indent=2));print('\n'.join(r['name'] for r in rows+controls if not r['passed']))
 return 0 if report['status']=='GREEN' else 1
if __name__=='__main__':raise SystemExit(main())
