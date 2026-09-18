"""Ember-only actual mesh/pose gate; the other six spells are immutable controls.

This checks geometry, timing and ownership, not rendered density or frame pacing.
Run on the old library before authoring, then unchanged on the new saved export.
"""
import argparse, copy, hashlib, json, math
from pathlib import Path
from audit_readability import points, islands, rotate

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[1]
BASE = REPO / 'Docs/Verification/StarterSpell3D/EmberRevision/E00-prechange'
OTHER = ('flaming_hands', 'jet_blast', 'ground_surge', 'rime_grip', 'calm', 'conjure_rain')


def normalize(data, sid):
    study = next(s for s in data['studies'] if s['id'] == sid)
    ids = {p['meshId'] for p in study['pieces']}
    meshes = []
    for mesh in data['meshes']:
        if mesh['id'] not in ids:
            continue
        item = copy.deepcopy(mesh)
        item['resolvedMaterial'] = copy.deepcopy(data['materials'][item.pop('materialIndex')])
        meshes.append(item)
    return dict(normalizationVersion=1, globals={k: data[k] for k in
                ('schemaVersion', 'fps', 'frameCount', 'releaseFrame', 'coordinateConvention')},
                study=study, meshes=sorted(meshes, key=lambda m: m['id']))


def pose_at(piece, frame):
    # Fractional contact is sampled between the actual adjacent exported tracks.
    lo = int(frame); hi = min(110, lo + 1); t = frame - lo
    a, b = piece['poses'][lo], piece['poses'][hi]
    q = [a['rotation'][k] * (1-t) + b['rotation'][k] * t for k in range(4)]
    n = sum(v*v for v in q)**.5
    return dict(position=[a['position'][k]*(1-t)+b['position'][k]*t for k in range(3)],
                scale=[a['scale'][k]*(1-t)+b['scale'][k]*t for k in range(3)],
                rotation=[v/n for v in q] if n else [0, 0, 0, 1])


def mapped_forward(study, forward, length):
    muzzle = study['authoredMuzzleForward']; authored = study['authoredDistanceCells']
    return forward if forward <= muzzle else muzzle + (forward-muzzle)/(authored-muzzle)*(length-muzzle)


def section_segments(mesh, vertices, plane):
    """Actual triangle intersections with an axial plane, in lateral/height."""
    result=[]
    for i in range(0,len(mesh['triangles']),3):
        tri=[vertices[k] for k in mesh['triangles'][i:i+3]];hits=[]
        for j,a in enumerate(tri):
            b=tri[(j+1)%3];da=a[2]-plane;db=b[2]-plane
            if abs(da)<1e-8:hits.append(a[:2])
            if da*db<0:
                t=da/(da-db);hits.append(tuple(a[k]+(b[k]-a[k])*t for k in (0,1)))
        unique=[]
        for p in hits:
            if not any(math.dist(p,q)<1e-7 for q in unique):unique.append(p)
        if len(unique)==2 and math.dist(*unique)>1e-7:result.append(tuple(unique))
    return result


def sections_overlap(first,second):
    # Surface segment crossings prove intersection; odd/even ray tests also
    # accept solid containment. These are actual section contours, not AABBs.
    def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    def within(p,segments):
        count=0
        for a,b in segments:
            if (a[1]>p[1])!=(b[1]>p[1]):
                x=a[0]+(p[1]-a[1])*(b[0]-a[0])/(b[1]-a[1])
                if x>p[0]:count+=1
        return count%2==1
    for a,b in first:
        for c,d in second:
            if cross(a,b,c)*cross(a,b,d)<-1e-12 and cross(c,d,a)*cross(c,d,b)<-1e-12:return True
    return any(within(a,second) for a,b in first) or any(within(a,first) for a,b in second)


def validate(data, groups=None):
    rows=[]
    def need(name, ok, actual):
        rows.append(dict(name=name, passed=bool(ok), actual=actual))
    def run(group):
        return groups is None or group in groups
    study=next(s for s in data['studies'] if s['id']=='ember_spit')
    meshes={m['id']:m for m in data['meshes']}; mats=data['materials']
    contact=study['contactFrame']; ps=study['pieces']
    projectiles=[p for p in ps if p['role'] in ('ProjectileHead','ProjectileTrail')]
    tail=[p for p in ps if p['role']=='ProjectileTrail']
    impact=[p for p in ps if p['role']=='TargetImpact']
    def visible(piece, frame):
        return min(pose_at(piece,frame)['scale']) > .002
    def material(piece):
        return mats[meshes[piece['meshId']]['materialIndex']]
    def world(piece, frame, length=3):
        pose=pose_at(piece,frame); values=points(meshes[piece['meshId']],pose)
        if piece['role']=='ProjectileTrail':
            shift=mapped_forward(study,pose['position'][2],length)-pose['position'][2]
        elif piece['role']=='ProjectileHead':
            lo=int(frame);t=frame-lo
            progress=study['projectileProgress'][lo]*(1-t)+study['projectileProgress'][min(110,lo+1)]*t
            forward=progress*study['authoredDistanceCells'];shift=mapped_forward(study,forward,length)-forward
        else:
            shift=length if piece['anchor']=='Target' else 0
        return [(x,y,z+shift) for x,y,z in values]
    def allpoints(pieces,frame,length=3,solid=False):
        return [v for p in pieces if visible(p,frame) and (not solid or not material(p)['glow']) for v in world(p,frame,length)]
    def span(values,axis):
        return max((p[axis] for p in values),default=0)-min((p[axis] for p in values),default=0)
    if run('preserve'):
        before=json.loads((BASE/'preserved-inputs/ArtSource/StarterSpell3D/runtime/starter_spell_library.json').read_text())
        old=next(s for s in before['studies'] if s['id']=='ember_spit')
        need('Ember timing and carrier metadata unchanged',
             {k:v for k,v in study.items() if k!='pieces'}=={k:v for k,v in old.items() if k!='pieces'},
             {k:v for k,v in study.items() if k not in ('pieces','projectileProgress')})
        need('Shared palette unchanged',sorted(data['materials'],key=lambda m:m['id'])==sorted(before['materials'],key=lambda m:m['id']),len(data['materials']))
        for sid in OTHER:
            baseline=json.loads((BASE/'normalized-spell-exports'/f'{sid}.json').read_text())
            need(sid+' complete normalized export unchanged',normalize(data,sid)==baseline,sid)
            path=ROOT/'exports'/f'{sid}.fbx'; oldpath=BASE/'preserved-inputs/ArtSource/StarterSpell3D/exports'/f'{sid}.fbx'
            need(sid+' FBX bytes unchanged',path.read_bytes()==oldpath.read_bytes(),hashlib.sha256(path.read_bytes()).hexdigest())
    if run('impact'):
        extent=radius=halo_radius=0;max_motes=0
        for frame in (contact,contact+3,contact+8,contact+15,contact+25,contact+35):
            solid=allpoints(impact,frame,solid=True)
            extent=max(extent,span(solid,0),span(solid,2))
            radius=max([radius]+[math.hypot(x,z-3) for x,y,z in solid])
            halo=allpoints([p for p in impact if material(p)['glow']],frame)
            halo_radius=max([halo_radius]+[math.hypot(x,z-3) for x,y,z in halo])
            max_motes=max(max_motes,sum(len(islands(meshes[p['meshId']])) for p in impact if '_motes_' in p['id'] and visible(p,frame)))
        need('Compact warm impact replaces radial fire crown',extent<=1.1001 and radius<=.651,dict(span=extent,radius=radius))
        need('Impact glow stays local',halo_radius<=.6501,halo_radius)
        need('Impact has only a few fading embers',0<max_motes<=32,max_motes)
        need('Actual fractional contact has visible non-gather geometry',bool(allpoints(projectiles+impact,contact,solid=True)),contact)
    if run('tail'):
        defining=[p for p in tail if p['reducedEssential'] and not material(p)['glow']]
        late=allpoints(defining,contact-.5,4,solid=True)
        length=span(late,2);width=span(late,0)
        need('Long broad projectile tail on four-cell route',length>=2.6 and .8<=width<=1.3,dict(length=length,width=width))
        lo=min((v[2] for v in late),default=0); hi=max((v[2] for v in late),default=0)
        bands=[]
        for j in range(5):
            band=[v for v in late if lo+(hi-lo)*j/5<=v[2]<=lo+(hi-lo)*(j+1)/5]
            bands.append(span(band,0))
        need('Conical tail narrows toward trailing end',len(late)>0 and bands[0]<bands[-2]*.65 and bands[-2]>.65, bands)
        intervals=sorted((min(v[2] for v in world(p,contact-.5,4)),max(v[2] for v in world(p,contact-.5,4))) for p in defining if visible(p,contact-.5))
        gaps=[intervals[i+1][0]-intervals[i][1] for i in range(len(intervals)-1)]
        need('Defining tail sections form a connected longitudinal fill',len(intervals)>=12 and max(gaps,default=999)<=.035,dict(sections=len(intervals),largestGap=max(gaps,default=999)))
        seams=[];failures=[]
        for length in (4,4*math.sqrt(2)):
            for f in (27,28,29,contact,contact+4):
                ordered=sorted([p for p in defining if visible(p,f)],key=lambda p:pose_at(p,f)['position'][2])
                for a,b in zip(ordered,ordered[1:]):
                    va=world(a,f,length);vb=world(b,f,length)
                    lo=max(min(v[2] for v in va),min(v[2] for v in vb));hi=min(max(v[2] for v in va),max(v[2] for v in vb))
                    plane=(lo+hi)/2
                    sa=section_segments(meshes[a['meshId']],va,plane);sb=section_segments(meshes[b['meshId']],vb,plane)
                    ok=lo<=hi and sections_overlap(sa,sb)
                    record=dict(routeLength=length,frame=f,pair=[a['id'],b['id']],plane=plane,contourSegments=[len(sa),len(sb)],passed=ok)
                    seams.append(record)
                    if not ok:failures.append(record)
        need('Actual tail volumes join transversely and vertically on cardinal and diagonal routes',bool(seams) and not failures,dict(samples=len(seams),failures=failures))
        counts=[]
        for f in (27,28,29,contact):
            counts.append(sum(len(islands(meshes[p['meshId']])) for p in projectiles if '_motes_' in p['id'] and visible(p,f)))
        need('Dense travel embers replace impact-dominated particles',min(counts)>=256 and max(counts)<=384,counts)
        head=allpoints([p for p in projectiles if p['role']=='ProjectileHead'],contact-.5)
        need('Carrier head stays compact rather than hiding a long rigid tail',span(head,2)<=.80,span(head,2))
    if run('bounds'):
        bad=[];views=[]
        for cells in (1,2,4):
            for angle in range(0,360,45):
                diagonal=angle%90!=0;length=cells*(math.sqrt(2) if diagonal else 1)
                for frame in (12,18,21,22,23,25,27,29,contact,contact+4,contact+8,contact+14,contact+25):
                    visiblepoints=allpoints(projectiles,frame,length)
                    # Axial cap tests are measured on all posed vertices. The
                    # eight rotations below additionally inspect actual projection.
                    idx=min(110,int(frame));t=frame-idx
                    progress=study['projectileProgress'][idx]*(1-t)+study['projectileProgress'][min(110,idx+1)]*t
                    front=mapped_forward(study,progress*3,length)
                    if visiblepoints and (min(v[2] for v in visiblepoints)<.159 or max(v[2] for v in visiblepoints)>front+.34):
                        bad.append(dict(cells=cells,degrees=angle,frame=frame,minForward=min(v[2] for v in visiblepoints),maxForward=max(v[2] for v in visiblepoints),headForward=front))
                    a=math.radians(angle);projected=[(z*math.cos(a)-x*math.sin(a),z*math.sin(a)+x*math.cos(a)-y/math.tan(math.radians(56))) for x,y,z in visiblepoints]
                    if projected and frame==29:views.append(dict(cells=cells,degrees=angle,width=max(v[0] for v in projected)-min(v[0] for v in projected),height=max(v[1] for v in projected)-min(v[1] for v in projected)))
        need('No tail or head cap behind launch or far past contact on real routes',not bad,dict(failures=bad[:16],failureCount=len(bad),projectedRoutes=views))
        early=allpoints(projectiles,21)
        need('Pre-release source gather remains compact',span(early,2)<=.8 and span(early,0)<=.8,dict(length=span(early,2),width=span(early,0)))
        need('Ember uses projectile and target roles only',all(p['role'] in ('ProjectileHead','ProjectileTrail','TargetImpact','SourceGather') for p in ps),sorted({p['role'] for p in ps}))
    if run('motion'):
        start=allpoints(tail,contact);middle=allpoints(tail,contact+8);end=allpoints(tail,contact+16)
        lengths=[span(v,2) for v in (start,middle,end)]
        need('Tail contracts into contact instead of holding a static plume',lengths[0]>.8 and lengths[1]<lengths[0]*.80 and lengths[2]<lengths[0]*.35,lengths)
        movers=[p for p in tail if p['reducedEssential']]
        forward=[pose_at(p,contact+8)['position'][2]-pose_at(p,contact)['position'][2] for p in movers]
        need('Tail material flows toward contact while settling',sum(v>.20 for v in forward)>=len(movers)*.5 and bool(movers),forward)
        need('Every transient clears by original clear frame',all(max(pose_at(p,study['clearFrame']+.5)['scale'])<.002 for p in ps),study['clearFrame'])
        need('All sampled poses finite with nonnegative scale',all(math.isfinite(v) and (k!='scale' or v>=-1e-6) for p in ps for pose in p['poses'] for k in ('position','rotation','scale') for v in pose[k]),len(ps)*111)
    if run('budget'):
        tris=sum(len(meshes[p['meshId']]['triangles'])//3 for p in ps)
        need('Ember fits retained view and geometry budget',len(ps)<=61 and tris<=8692,dict(pieces=len(ps),triangles=tris,essential=sum(p['reducedEssential'] for p in ps)))
    return rows


def main():
    ap=argparse.ArgumentParser();ap.add_argument('--input',default=str(ROOT/'runtime/starter_spell_library.json'));ap.add_argument('--output',required=True);ap.add_argument('--controls',action='store_true');args=ap.parse_args()
    path=Path(args.input);data=json.loads(path.read_text());rows=validate(data);controls=[]
    if args.controls:
        def probe(name,group,mutate,expected):
            changed=copy.deepcopy(data);mutate(changed);tests=validate(changed,{group});failed=[r['name'] for r in tests if not r['passed']]
            controls.append(dict(name=name,passed=expected in failed,rejectedBy=failed))
        def ember(d):return next(s for s in d['studies'] if s['id']=='ember_spit')
        def crown(d):
            old=json.loads((BASE/'preserved-inputs/ArtSource/StarterSpell3D/runtime/starter_spell_library.json').read_text())
            oldpieces=[p for p in ember(old)['pieces'] if p['role']=='TargetImpact']
            ids={p['meshId'] for p in oldpieces};materialids={m['id']:i for i,m in enumerate(d['materials'])}
            d['meshes']=[m for m in d['meshes'] if m['id'] not in ids]
            for mesh in old['meshes']:
                if mesh['id'] in ids:
                    item=copy.deepcopy(mesh);item['materialIndex']=materialids[old['materials'][mesh['materialIndex']]['id']]
                    d['meshes'].append(item)
            ember(d)['pieces']=[p for p in ember(d)['pieces'] if p['role']!='TargetImpact']+copy.deepcopy(oldpieces)
        probe('Reinserted actual previous fire crown is rejected','impact',crown,'Compact warm impact replaces radial fire crown')
        probe('Deleted actual travel embers are rejected','tail',lambda d:ember(d).__setitem__('pieces',[p for p in ember(d)['pieces'] if not ('_motes_' in p['id'] and p['role']!='TargetImpact')]),'Dense travel embers replace impact-dominated particles')
        def gap(d):
            s=ember(d);tails=[p for p in s['pieces'] if p['role']=='ProjectileTrail' and p['reducedEssential']]
            if tails:
                removed={p['id'] for p in tails[max(0,len(tails)//2-1):len(tails)//2+2]}
                s['pieces']=[p for p in s['pieces'] if p['id'] not in removed]
        probe('Missing actual central tail span is rejected','tail',gap,'Defining tail sections form a connected longitudinal fill')
        def sidegap(d):
            s=ember(d);tails=[p for p in s['pieces'] if p['role']=='ProjectileTrail' and p['reducedEssential']]
            if tails:
                p=tails[len(tails)//2]
                for pose in p['poses']:pose['position'][0]+=.9;pose['position'][1]+=.6
        probe('Shifted actual middle volume cannot hide behind overlapping forward bounds','tail',sidegap,'Actual tail volumes join transversely and vertically on cardinal and diagonal routes')
        def frozen(d):
            s=ember(d);f=math.ceil(s['contactFrame'])
            for p in s['pieces']:
                if p['role']=='ProjectileTrail':
                    for k in range(f+1,55):p['poses'][k]=copy.deepcopy(p['poses'][f])
        probe('Frozen actual contraction is rejected','motion',frozen,'Tail contracts into contact instead of holding a static plume')
        def backwards(d):
            p=next(p for p in ember(d)['pieces'] if p['role']=='ProjectileTrail')
            p['poses'][25]['position'][2]=-2;p['poses'][25]['scale']=[1,1,1]
        probe('Actual backward cap is rejected','bounds',backwards,'No tail or head cap behind launch or far past contact on real routes')
        def premature(d):
            for p in ember(d)['pieces']:
                if p['role']=='ProjectileTrail':p['poses'][18]=copy.deepcopy(p['poses'][29])
        probe('Actual full tail before release is rejected','bounds',premature,'No tail or head cap behind launch or far past contact on real routes')
        def linger(d):
            p=ember(d)['pieces'][0]
            for f in (67,68,69):p['poses'][f]['scale']=[1,1,1]
        probe('Actual late fragment is rejected','motion',linger,'Every transient clears by original clear frame')
        def hands(d):
            p=next(s for s in d['studies'] if s['id']=='flaming_hands')['pieces'][0]
            p['poses'][30]['position'][0]+=.1
        probe('Changed Hands pose is rejected','preserve',hands,'flaming_hands complete normalized export unchanged')
        controls.append(dict(name='Unchanged complete new art accepted',passed=all(r['passed'] for r in rows)))
    report=dict(status='GREEN' if all(r['passed'] for r in rows+controls) else 'RED',sourceBlendSha256=data['sourceBlendSha256'],librarySha256=hashlib.sha256(path.read_bytes()).hexdigest(),assertions=len(rows+controls),passed=sum(r['passed'] for r in rows+controls),checks=rows,controls=controls)
    Path(args.output).write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k not in ('checks','controls')},indent=2))
    print('\n'.join(r['name'] for r in rows+controls if not r['passed']))
    return 0 if report['status']=='GREEN' else 1

if __name__=='__main__':raise SystemExit(main())
