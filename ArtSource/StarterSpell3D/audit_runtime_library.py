"""Read the delivered native art data, including real geometry mutation controls.

This gate measures exported assets only. Unity's importer, visibility filtering,
mechanical outcome selection and rendered front faces require their own gates.
"""
from pathlib import Path
import copy, hashlib, json, math, sys
ROOT = Path(__file__).resolve().parent
PATH = ROOT / 'runtime/starter_spell_library.json'
RECEIPTS = ROOT.parents[1] / 'Docs/Verification/StarterSpell3D/Polish01/author'

def rotate(v, q):
    x,y,z,w=q; px,py,pz=v
    tx=2*(y*pz-z*py); ty=2*(z*px-x*pz); tz=2*(x*py-y*px)
    return (px+w*tx+y*tz-z*ty, py+w*ty+z*tx-x*tz, pz+w*tz+x*ty-y*tx)

def validate(data):
    rows=[]
    def need(name, passed, actual):
        rows.append(dict(name=name, passed=bool(passed), actual=actual))
    need('seven named entries', len(data['studies'])==7 and len({s['id'] for s in data['studies']})==7,len(data['studies']))
    need('uniform native sampling', data['fps']==100 and data['frameCount']==111 and data['releaseFrame']==22, [data['fps'],data['frameCount'],data['releaseFrame']])
    meshes={m['id']:m for m in data['meshes']}
    need('unique mesh IDs',len(meshes)==len(data['meshes']),len(meshes))
    for study in data['studies']:
        pieces=study['pieces']; sid=study['id']
        need(sid+' has actual pieces',bool(pieces),len(pieces))
        need(sid+' has live reduced-detail forms',any(p['reducedEssential'] for p in pieces),sum(p['reducedEssential'] for p in pieces))
        valid_tracks=True; peakstep=0; bounds={}; winding=True; plate_height=0
        for piece in pieces:
            poses=piece['poses']; mesh=meshes[piece['meshId']]; vs=mesh['vertices']; ns=mesh['normals']
            valid_tracks &= len(poses)==111 and all(len(p['position'])==3 and len(p['rotation'])==4 and len(p['scale'])==3 for p in poses)
            valid_tracks &= all(math.isfinite(v) for p in poses for key in ('position','rotation','scale') for v in p[key])
            valid_tracks &= all(min(p['scale'])>=-1e-6 and abs(sum(v*v for v in p['rotation'])-1)<2e-5 for p in poses)
            valid_tracks &= max(abs(v) for v in poses[-1]['scale'])<.002
            envelope=[max(p['scale']) for p in poses]; peak=max(envelope)
            if peak>0: peakstep=max(peakstep,max(abs(a-b)/peak for a,b in zip(envelope,envelope[1:])))
            for i in range(0,len(mesh['triangles']),3):
                ia,ib,ic=mesh['triangles'][i:i+3];a=vs[3*ia:3*ia+3];b=vs[3*ib:3*ib+3];c=vs[3*ic:3*ic+3]
                u=[b[k]-a[k] for k in range(3)];v=[c[k]-a[k] for k in range(3)];cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
                normal=ns[3*ia:3*ia+3];dot=sum(cross[k]*normal[k] for k in range(3));winding &= dot>0
            if piece['role'] in ('ConeCell','GroundCell','CropCell','ReactionCell'):
                for p in poses:
                    for i in range(0,len(vs),3):
                        rv=rotate([vs[i+k]*p['scale'][k] for k in range(3)],p['rotation']);point=[rv[k]+p['position'][k] for k in range(3)]
                        kind=piece.get('visualBounds','CellSurface'); boundkey=piece['role']+' '+kind
                        measure=math.hypot(point[0],point[2]) if kind=='AirborneDecoration' else max(abs(point[0]),abs(point[2]))
                        bounds[boundkey]=max(bounds.get(boundkey,0),measure)
                        if piece['role']=='ReactionCell':plate_height=max(plate_height,abs(point[1]))
        need(sid+' has finite complete nonnegative pose tracks and clears',valid_tracks,len(pieces))
        need(sid+' preserved eased scale reveal',peakstep<=.30,peakstep)
        need(sid+' triangle winding agrees with exported normals',winding,len(pieces))
        for role,bound in bounds.items():
            limit=2.75001 if role.endswith('AirborneDecoration') else .50001
            need(sid+' '+role+' stays inside its declared ownership bound at all111 samples',bound<=limit,bound)
        if sid=='rime_grip':
            need('Rime denied Frozen retains reduced cold-contact feedback',any(p['role']=='TargetImpact' and p['condition']=='Always' and p['reducedEssential'] for p in pieces),[p['id'] for p in pieces if p['role']=='TargetImpact' and p['condition']=='Always' and p['reducedEssential']])
            plates=[p for p in pieces if p['role']=='ReactionCell'];clamps=[p for p in pieces if 'blunt_split_clamp' in p['id'] or 'pale_blunt_rim' in p['id']]
            need('freeze-water plates stay below0.15cell and require reaction',len(plates)==3 and plate_height<=.15 and all(p['condition']=='FreezeWater' for p in plates),plate_height)
            need('Rime clamps require actual Frozen outcome',len(clamps)==6 and all(p['condition']=='FrozenApplied' for p in clamps),len(clamps))
        if sid=='calm':need('Calm target arcs require actual Pacified outcome',all(p['condition']=='PacifiedApplied' for p in pieces if p['role']=='TargetImpact'),len(pieces))
        if sid in ('ember_spit','calm'):
            prog=study['projectileProgress'];need(sid+' preserves increasing carrier progress',len(prog)==111 and all(b>=a-1e-6 for a,b in zip(prog,prog[1:])) and .97<=prog[int(round(study['contactFrame']))]<=1.01 and 1<=prog[-1]<=1.04,[prog[22],prog[-1]])
    return rows

data=json.loads(PATH.read_text()); rows=validate(data)
# These mutate real exported assets, not their summarized reports.
controls=[]
def probe(name, mutate):
    damaged=copy.deepcopy(data); mutate(damaged)
    failed=[r['name'] for r in validate(damaged) if not r['passed']]
    controls.append(dict(name=name,passed=bool(failed),rejectedBy=failed))
probe('reflected triangle rejected',lambda d:d['meshes'][0]['triangles'].__setitem__(slice(0,3),list(reversed(d['meshes'][0]['triangles'][:3]))))
probe('one-frame scale pop rejected',lambda d:d['studies'][0]['pieces'][0]['poses'][1].__setitem__('scale',[1,1,1]))
probe('negative scale rejected',lambda d:d['studies'][0]['pieces'][0]['poses'][30].__setitem__('scale',[-1,1,1]))
probe('missing study pieces rejected',lambda d:d['studies'][0].__setitem__('pieces',[]))
probe('cell spill rejected',lambda d:next(p for p in d['studies'][1]['pieces'] if p.get('visualBounds','CellSurface')=='CellSurface')['poses'][35].__setitem__('position',[1,0,0]))
probe('unconditional freeze plates rejected',lambda d:next(p for s in d['studies'] if s['id']=='rime_grip' for p in s['pieces'] if p['role']=='ReactionCell').__setitem__('condition','Always'))
probe('missing reduced neutral Rime contact rejected',lambda d:[p.__setitem__('reducedEssential',False) for s in d['studies'] if s['id']=='rime_grip' for p in s['pieces'] if p['role']=='TargetImpact' and p['condition']=='Always'])
controls.append(dict(name='unchanged delivered library accepted',passed=all(r['passed'] for r in rows)))
report=dict(status='GREEN' if all(r['passed'] for r in rows+controls) else 'RED',sourceBlendSha256=data['sourceBlendSha256'],librarySha256=hashlib.sha256(PATH.read_bytes()).hexdigest(),assertions=len(rows)+len(controls),passed=sum(r['passed'] for r in rows+controls),checks=rows,controls=controls)
(Path(sys.argv[1]) if len(sys.argv)>1 else ROOT/'runtime-data-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k not in ('checks','controls')},indent=2))
if report['status']!='GREEN':
    print(json.dumps([r for r in rows+controls if not r['passed']],indent=2));raise SystemExit(1)
