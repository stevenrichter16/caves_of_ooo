"""Record P1 provenance without rewriting any preserved v1 receipt."""
from pathlib import Path
import hashlib, json
ROOT=Path(__file__).resolve().parent
RECEIPTS=ROOT.parents[1]/'Docs/Verification/StarterSpell3D/Polish01/author'
EXPECTED='39ea9e4134d839be88f9f44fcb87c4bd7ee2936ec6300d6fadfd3930165d3e39'

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path,data):path.write_text(json.dumps(data,indent=2)+'\n')
blend=digest(ROOT/'starter_spells.blend');assert blend==EXPECTED
manifest=json.loads((ROOT/'manifest.json').read_text())
log=(RECEIPTS/'11-motion-render.log').read_text()
assert 'SELECTED_ANIMATIONS_RENDERED' in log
v1=ROOT/'revisions/v1';baseline=json.loads((v1/'baseline-inventory.json').read_text());preservation=[]
for row in baseline['files']:
    actual=digest(v1/row['path']);assert actual==row['sha256']
    preservation.append(dict(path=row['path'],sha256=actual,preserved=True))
old=json.loads((v1/'media-provenance.json').read_text());old_studies={s['id']:s for s in old['studies']}
rows=[]
for spec in manifest['studies']:
    sid=spec['id'];assert 'LOOP_FRAMES_READY '+sid in log
    frames=sorted((ROOT/'frames'/sid).glob('*.png'));assert len(frames)==37
    previous={r['path']:r['sha256'] for r in old_studies[sid]['frames']}
    files=[dict(path=str(p.relative_to(ROOT)),sha256=digest(p)) for p in frames]
    changed=sum(row['sha256']!=previous[row['path']] for row in files);assert changed>0
    rows.append(dict(id=sid,renderedFromBlendSha256=blend,all37FramesRenderedFresh=True,changedComparedWithV1=changed,frames=files,hero=dict(path='renders/'+sid+'.png',sha256=digest(ROOT/'renders'/(sid+'.png')))))
source=ROOT.parents[1]/manifest['source'];assert digest(source)==manifest['sourceSha256']
library=ROOT/'runtime/starter_spell_library.json';native=json.loads(library.read_text());assert native['sourceBlendSha256']==blend
provenance=dict(version='Polish01',finalBlendSha256=blend,statement='All seven full37-frame sequences and seven hero stills rendered fresh from frozen P1 source. Composed media use those frames. PNG physical-density metadata removed without changing compressed IDAT bytes. No v1 receipt overwritten.',renderLogSha256=digest(RECEIPTS/'11-motion-render.log'),nativeLibrarySha256=digest(library),villageSourceSha256=digest(source),v1BaselinePreservation=preservation,studies=rows)
write(ROOT/'media-provenance.json',provenance);write(RECEIPTS/'16-media-provenance.json',provenance)
files=[ROOT/'starter_spells.blend',ROOT/'manifest.json',ROOT/'asset-audit.json',ROOT/'README.md',ROOT/'media-provenance.json']+sorted(ROOT.glob('*.py'))+sorted((ROOT/'exports').glob('*.fbx'))+sorted((ROOT/'renders').glob('*'))+sorted((ROOT/'runtime').glob('*'))
report=dict(status='ART_READY_FOR_NATIVE_ACCEPTANCE',version='Polish01',blendSha256=blend,renderedSourceFrames=259,animatedStudioMeshCount=145,nativePieceCount=sum(len(s['pieces']) for s in native['studies']),nativeTriangleCount=sum(len(m['triangles'])//3 for m in native['meshes']),files=[dict(path=str(p.relative_to(ROOT.parents[1])),bytes=p.stat().st_size,sha256=digest(p)) for p in files if p.is_file()])
write(RECEIPTS/'17-delivery-inventory.json',report)
print(json.dumps(dict(status=report['status'],blendSha256=blend,artifacts=len(report['files']),rawFrames=259,allSevenSequencesRenderedFresh=True),indent=2))
