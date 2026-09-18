"""Audit real imported FBX geometry, UV variation and optional source/publication parity.

Run with Blender --background --python roundtrip.py -- <export-root>.
--compare <other-root> checks semantic mesh/UV equality, not FBX timestamps.
--output <report-dir> permits read-only audits of the installed Unity assets.
"""
import argparse
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

import bpy

sys.path.insert(0,str(Path(__file__).resolve().parent))
from contract import validate_variant_fingerprints


def audit(root):
 rows=json.loads((root/'catalog.json').read_text())['models'];reports=[]
 for row in rows:
  bpy.ops.wm.read_factory_settings(use_empty=True)
  path=root/row['path']
  if not path.exists():path=root/'Models'/Path(row['path']).name
  bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
  bpy.context.view_layer.update()
  objects=sorted((ob for ob in bpy.context.scene.objects if ob.type=='MESH'),key=lambda ob:ob.name)
  errors=[];tri=0;vertices=[];visual=hashlib.sha256()
  for ob in objects:
   mesh=ob.data
   if not mesh.uv_layers:errors.append('missing-uv')
   if not mesh.materials:errors.append('missing-material')
   mesh.calc_loop_triangles();tri+=len(mesh.loop_triangles)
   # Normalize to the exported model frame. Names, paths, metadata, creation
   # timestamps and arbitrary object ordering are not proof of visual variation.
   visual.update(struct.pack('<II',len(mesh.vertices),len(mesh.polygons)))
   for vertex in mesh.vertices:
    p=ob.matrix_world@vertex.co;vertices.append(p)
    visual.update(struct.pack('<3f',*p))
   for polygon in mesh.polygons:
    visual.update(struct.pack('<I',len(polygon.vertices)))
    for index in polygon.vertices:visual.update(struct.pack('<I',index))
   if mesh.uv_layers:
    for uv in mesh.uv_layers[0].data:visual.update(struct.pack('<2f',*uv.uv))
   for triangle in mesh.loop_triangles:
    a,b,c=[mesh.vertices[i].co for i in triangle.vertices]
    if (b-a).cross(c-a).length<1e-10:errors.append('degenerate');break
  if not vertices:errors.append('empty')
  if any(not math.isfinite(c) for p in vertices for c in p):errors.append('nonfinite')
  if any(abs(p.x)>.5001 or abs(p.y)>.5001 or p.z<-.0001 for p in vertices):errors.append('outside-cell')
  if tri!=row['triangles']:errors.append('triangle-drift')
  reports.append({'id':row['id'],'family':row['family'],'variant':row['variant'],
                  'triangles':tri,'visualFingerprint':visual.hexdigest(),'failures':sorted(set(errors))})
 return reports,validate_variant_fingerprints(reports)


def main():
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('root',type=Path)
 parser.add_argument('--compare',type=Path)
 parser.add_argument('--output',type=Path)
 args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
 root=args.root.resolve();output=(args.output or root).resolve();output.mkdir(parents=True,exist_ok=True)
 reports,errors=audit(root)
 (output/'roundtrip.json').write_text(json.dumps(reports,indent=2)+'\n')
 summary={'root':str(root),'models':len(reports),'variantErrors':errors,
          'geometryErrors':[row for row in reports if row['failures']]}
 if args.compare:
  peer,peer_errors=audit(args.compare.resolve());lookup={row['id']:row for row in peer}
  mismatches=[]
  for row in reports:
   other=lookup.pop(row['id'],None)
   if other is None or row['visualFingerprint']!=other['visualFingerprint'] or row['triangles']!=other['triangles']:
    mismatches.append(row['id'])
  mismatches.extend(lookup)
  summary['comparison']={'root':str(args.compare.resolve()),'models':len(peer),'mismatches':sorted(mismatches),
                         'variantErrors':peer_errors,'geometryErrors':[row for row in peer if row['failures']]}
 summary['pass']=not errors and not summary['geometryErrors']
 if args.compare:
  summary['pass']&=not summary['comparison']['mismatches'] and not summary['comparison']['variantErrors'] and not summary['comparison']['geometryErrors']
 (output/'roundtrip-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
 assert summary['pass'],summary
 print('ROUNDTRIP PASS',len(reports),'models; four real variants per family')


if __name__=='__main__':main()
