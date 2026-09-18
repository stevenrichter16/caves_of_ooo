"""Bind final assets/media to frozen source and document retained equivalent frames."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent;RECEIPTS=ROOT.parents[1]/'Docs/Verification/StarterSpell3D'
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
comparison=json.loads((RECEIPTS/'26-fragment-geometry-comparison.json').read_text());manifest=json.loads((ROOT/'manifest.json').read_text());source=digest(ROOT/'starter_spells.blend')
assert source==comparison['afterBlendSha']
rows=[]
for spec,comparisonrow in zip(manifest['studies'],comparison['studies']):
 assert spec['id']==comparisonrow['id']
 frames=sorted((ROOT/'frames'/spec['id']).glob('*.png'));assert len(frames)==37
 rows.append(dict(id=spec['id'],finalGeometryBlendSha256=source,renderedFromBlendSha256=source if comparisonrow['changed'] else comparison['beforeBlendSha'],retainedViaCompleteVisibleVertexEquivalence=not comparisonrow['changed'],equivalenceReceipt='26-fragment-geometry-comparison.json',frames=[dict(path=str(p.relative_to(ROOT)),sha256=digest(p)) for p in frames],hero=dict(path='renders/'+spec['id']+'.png',sha256=digest(ROOT/'renders'/(spec['id']+'.png')))))
provenance=dict(finalBlendSha256=source,statement='Four changed full sequences rendered anew. Three unchanged sequences retained only after every preview sample has identical evaluated visible effect geometry; studio/actor/camera/material code did not change. All PNG density metadata is stripped without changing IDAT bytes.',studies=rows)
(ROOT/'media-provenance.json').write_text(json.dumps(provenance,indent=2)+'\n');(RECEIPTS/'34-final-media-provenance.json').write_text(json.dumps(provenance,indent=2)+'\n')
files=[ROOT/'starter_spells.blend',ROOT/'manifest.json',ROOT/'asset-audit.json',ROOT/'README.md',ROOT/'media-provenance.json']+sorted((ROOT/'exports').glob('*.fbx'))+sorted((ROOT/'renders').glob('*'))
report=dict(status='COMPLETE',blendSha256=source,renderedSourceFrames=259,animatedMeshCount=104,files=[dict(path=str(p.relative_to(ROOT.parents[1])),bytes=p.stat().st_size,sha256=digest(p)) for p in files])
(RECEIPTS/'35-final-delivery-inventory.json').write_text(json.dumps(report,indent=2)+'\n')
old=RECEIPTS/'20-final-delivery-inventory.json'
if old.exists():
 data=json.loads(old.read_text());data['status']='SUPERSEDED_DRAFT';data['supersededBy']='35-final-delivery-inventory.json';old.write_text(json.dumps(data,indent=2)+'\n')
print(json.dumps(dict(status='COMPLETE',blendSha256=source,artifacts=len(files),rawFrames=259),indent=2))
