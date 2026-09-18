from pathlib import Path
exec((Path(__file__).resolve().parent/'codex-village3d-rebuild-audit.py').read_text().split('\nbefore=json.loads')[0])
geometry=[]
for root in [original,rebuilt]:
 read_model(root/'models/axis_probe.fbx',False)
 rows=[]
 for ob in sorted(bpy.context.scene.objects,key=lambda o:o.name):
  if ob.type!='MESH':continue
  rows.append({'name':ob.name,'vertices':[[quant(f) for f in ob.matrix_world@v.co] for v in ob.data.vertices],'faces':[list(p.vertices) for p in ob.data.polygons]})
 geometry.append(rows)
assert geometry[0]==geometry[1],'Diagnostic probe geometry changed'
p=out/'rebuild-verification.json';report=json.loads(p.read_text());report['diagnosticProbeGeometryEqual']=True;report['diagnosticProbeMeshNames']=[x['name'] for x in geometry[0]]
report['limits'].append('The separate diagnostic axis probe is excluded from runtime model hashes: final-v2 preserved the previously accepted fixture byte-for-byte. Its regenerated geometry, marker names and topology also match; source palette UVs evolved during art polish.')
p.write_text(json.dumps(report,indent=2)+'\n');print('AXIS_REBUILD_GEOMETRY_PASS',[x['name'] for x in geometry[0]])
