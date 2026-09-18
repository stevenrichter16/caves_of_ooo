"""Bind the current Rain contact correction without rewriting earlier attempt receipts."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent;RECEIPTS=ROOT.parents[1]/'Docs/Verification/StarterSpell3D/Polish01/author'
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path,obj):path.write_text(json.dumps(obj,indent=2)+'\n')
comparison=json.loads((RECEIPTS/'31-rain-scene-comparison.json').read_text());blend=digest(ROOT/'starter_spells.blend');assert blend==comparison['afterBlendSha256']
previous=json.loads((RECEIPTS/'16-media-provenance.json').read_text());manifest=json.loads((ROOT/'manifest.json').read_text());native=json.loads((ROOT/'runtime/starter_spell_library.json').read_text());assert native['sourceBlendSha256']==blend
assert 'SELECTED_ANIMATIONS_RENDERED ["conjure_rain"]' in (RECEIPTS/'30-rain-motion-render.log').read_text()
oldrows={s['id']:s for s in previous['studies']};proof={s['id']:s for s in comparison['studies']};rows=[]
for spec in manifest['studies']:
 sid=spec['id'];frames=sorted((ROOT/'frames'/sid).glob('*.png'));assert len(frames)==37;changed=sid=='conjure_rain';new=[dict(path=str(p.relative_to(ROOT)),sha256=digest(p)) for p in frames]
 if not changed:assert proof[sid]['all37SceneSamplesEqual'] and new==oldrows[sid]['frames']
 rows.append(dict(id=sid,finalGeometryBlendSha256=blend,renderedFromBlendSha256=blend if changed else previous['finalBlendSha256'],retainedViaCompleteSceneEquivalence=not changed,equivalenceReceipt='31-rain-scene-comparison.json',frames=new,hero=dict(path='renders/'+sid+'.png',sha256=digest(ROOT/'renders'/(sid+'.png')),renderedFromBlendSha256=blend)))
for revision,inventory,field in [('v1','baseline-inventory.json','files'),('p1_before_rain_contact','inventory.json','files')]:
 folder=ROOT/'revisions'/revision
 for row in json.loads((folder/inventory).read_text())[field]:assert digest(folder/row['path'])==row['sha256']
source=ROOT.parents[1]/manifest['source'];assert digest(source)==manifest['sourceSha256']
provenance=dict(version='Polish01 Rain contact correction',finalBlendSha256=blend,nativeLibrarySha256=digest(ROOT/'runtime/starter_spell_library.json'),villageSourceSha256=digest(source),statement='Rain37frames rendered fresh; six full scene sequences retained only after all evaluated mesh vertices/materials/cameras/lights match at every37sample. All seven hero stills refreshed. Every earlier attempt receipt and durable baseline remains unchanged.',studies=rows)
write(ROOT/'media-provenance.json',provenance);write(RECEIPTS/'38-rain-media-provenance.json',provenance)
files=[ROOT/'starter_spells.blend',ROOT/'manifest.json',ROOT/'asset-audit.json',ROOT/'README.md',ROOT/'media-provenance.json']+sorted(ROOT.glob('*.py'))+sorted((ROOT/'exports').glob('*.fbx'))+sorted((ROOT/'renders').glob('*'))+sorted((ROOT/'runtime').glob('*'))
report=dict(status='ART_READY_FOR_NATIVE_ACCEPTANCE',version=provenance['version'],blendSha256=blend,renderedSourceFrames=259,newlyRenderedFrames=37,equivalentRetainedFrames=222,animatedStudioMeshCount=145,nativePieceCount=sum(len(s['pieces']) for s in native['studies']),nativeTriangleCount=sum(len(m['triangles'])//3 for m in native['meshes']),files=[dict(path=str(p.relative_to(ROOT.parents[1])),bytes=p.stat().st_size,sha256=digest(p)) for p in files if p.is_file()])
write(RECEIPTS/'39-rain-delivery-inventory.json',report);print(json.dumps(dict(status=report['status'],blendSha256=blend,artifacts=len(report['files']),newFrames=37,equivalentRetainedFrames=222),indent=2))
