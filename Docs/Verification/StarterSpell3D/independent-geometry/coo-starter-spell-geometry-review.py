import bpy, json, math, sys, hashlib, re
from pathlib import Path
from mathutils import Vector

args=sys.argv[sys.argv.index('--')+1:]
source=Path(args[0]);output=Path(args[1])
bpy.ops.wm.open_mainfile(filepath=str(source))
rows=[]
def record(name, passed, **evidence):
 rows.append(dict(name=name,passed=bool(passed),**evidence))
def scene_for(sid):
 found=[s for s in bpy.data.scenes if any(o.name==sid+'__ActorRoot' for o in s.objects)]
 assert len(found)==1,(sid,len(found))
 scene=found[0];bpy.context.window.scene=scene
 return scene
def at(scene, name='Contact', offset=0):
 marker=scene.timeline_markers.get(name);assert marker is not None,name
 frame=marker.frame+offset;scene.frame_set(math.floor(frame),subframe=frame%1)
 bpy.context.view_layer.update()
 return frame
def points(ob):
 graph=bpy.context.evaluated_depsgraph_get();ev=ob.evaluated_get(graph)
 mesh=ev.to_mesh()
 try:return [ev.matrix_world@v.co for v in mesh.vertices]
 finally:ev.to_mesh_clear()
def centre(ps):return Vector(tuple((min(p[k] for p in ps)+max(p[k] for p in ps))*.5 for k in range(3)))
def occupied(ps):
 return sorted({(round(p.x),round(p.y)) for p in ps})
def clip(poly,axis,bound,keep_more):
 if not poly:return []
 out=[]
 prev=poly[-1];insideprev=prev[axis]>=bound if keep_more else prev[axis]<=bound
 for cur in poly:
  inside=cur[axis]>=bound if keep_more else cur[axis]<=bound
  if inside!=insideprev:
   t=(bound-prev[axis])/(cur[axis]-prev[axis]);out.append((prev[0]+t*(cur[0]-prev[0]),prev[1]+t*(cur[1]-prev[1])))
  if inside:out.append(cur)
  prev,insideprev=cur,inside
 return out
def area(poly):return abs(sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly))))*.5 if poly else 0
def footprint_areas(objects,origin,cells):
 sums={tuple(c):0.0 for c in cells}
 graph=bpy.context.evaluated_depsgraph_get()
 for ob in objects:
  ev=ob.evaluated_get(graph);mesh=ev.to_mesh()
  try:
   mesh.calc_loop_triangles();pts=[ev.matrix_world@v.co-origin for v in mesh.vertices]
   for tri in mesh.loop_triangles:
    poly=[(pts[i].x,pts[i].y) for i in tri.vertices]
    for cell in sums:
     clipped=poly
     for axis in (0,1):
      clipped=clip(clipped,axis,cell[axis]-.5,True);clipped=clip(clipped,axis,cell[axis]+.5,False)
     sums[cell]+=area(clipped)
  finally:ev.to_mesh_clear()
 return {str(k):v for k,v in sums.items()}

# Exact integer crop anchors are measured from actual soil meshes, not manifest claims.
s=scene_for('conjure_rain');frame=at(s,offset=12);src=s.objects['conjure_rain__ActorRoot'].matrix_world.translation.copy()
crops=[o for o in s.objects if o.name.startswith('Crop__soil_') and o.type=='MESH']
offsets=[centre(points(o))-src for o in crops]
record('Rain has actual crop geometry',len(crops)>0,count=len(crops))
record('Rain crop centres are source-relative integer cells within Chebyshev three',bool(crops) and all(abs(p.x-round(p.x))<1e-4 and abs(p.y-round(p.y))<1e-4 and max(abs(p.x),abs(p.y))<=3.0001 for p in offsets),frame=frame,source=list(src),cropOffsets=[list(p) for p in offsets])

# Independent group geometry must sit inside cells one through four, away from source.
s=scene_for('ground_surge');frame=at(s,offset=2);src=s.objects['ground_surge__ActorRoot'].matrix_world.translation.copy()
groups={}
for ob in s.objects:
 match=re.match(r'Surge__cell_(\d+)_angular_stitch_',ob.name)
 if match and ob.type=='MESH':groups.setdefault(int(match.group(1)),[]).append(ob)
record('Ground Surge has four distinct stitch groups',set(groups)=={1,2,3,4},groups=sorted(groups))
for n,objects in sorted(groups.items()):
 ps=[p-src for ob in objects for p in points(ob)];c=centre(ps)
 bounds=[[min(p[k] for p in ps),max(p[k] for p in ps)] for k in range(3)]
 record('Ground Surge group '+str(n)+' occupies its actual forward cell',abs(c.x-n)<.1 and abs(c.y)<.1 and bounds[0][0]>=n-.5-1e-4 and bounds[0][1]<=n+.5+1e-4 and bounds[1][0]>=-.5 and bounds[1][1]<=.5,frame=frame,relativeCentre=list(c),relativeBounds=bounds,objects=[o.name for o in objects])

# A broad stream footprint should visibly enter all four target cells, using actual faces.
# Require 0.01 sq-cell summed projected mesh area, allowing decorative shape variation.
s=scene_for('jet_blast');frame=at(s,offset=3);src=s.objects['jet_blast__ActorRoot'].matrix_world.translation.copy()
folds=[o for o in s.objects if o.type=='MESH' and o.name.startswith('Jet__') and ('fold' in o.name or 'river_strip' in o.name)]
cells=[(1,0),(2,0),(2,-1),(2,1)];areas=footprint_areas(folds,src,cells)
record('Jet Blast has elevated broad flow meshes',bool(folds),objects=[o.name for o in folds])
for cell in cells:record('Jet Blast covers resolved cell '+str(cell),areas[str(cell)]>=.01,frame=frame,summedProjectedMeshArea=areas[str(cell)],minimumReadableArea=.01)
water=[o for o in s.objects if o.type=='MESH' and (o.name.startswith('Jet__') or o.name.startswith('jet_blast__')) and any(m and m.name.startswith('SpellFolk_') and any(word in m.name for word in ('river','water_light','ivory')) for m in o.data.materials)]
flat=[]
for ob in water:
 ps=points(ob)
 if ps and max(p.z for p in ps)<=.06 and max(p.x for p in ps)-min(p.x for p in ps)>.3 and max(p.y for p in ps)-min(p.y for p in ps)>.3:flat.append(ob.name)
record('Jet Blast contains no broad ground puddle',not flat,groundMeshes=flat)

# Calm hero is mid-flight; inspect the actual contact marker and evaluated open-loop body.
s=scene_for('calm');frame=at(s);src=s.objects['calm__ActorRoot'].matrix_world.translation.copy()
loop=s.objects.get('Calm__travel_open_loop');assert loop is not None
meshes=[o for o in loop.children_recursive if o.type=='MESH'];ps=[p-src for ob in meshes for p in points(ob)];c=centre(ps)
record('Calm actual contact reaches the fourth forward cell',bool(ps) and abs(c.x-4)<.45 and abs(c.y)<.45,frame=frame,relativeMeshCentre=list(c),loopWorldLocation=list(loop.matrix_world.translation),source=list(src),objects=[o.name for o in meshes])
contact=list(c)
s.frame_set(25);bpy.context.view_layer.update();mid=centre([p-src for ob in meshes for p in points(ob)])
record('Calm earlier hero pose is a distinct mid-flight control',mid.x<3.5 and mid.x<contact[0]-.2,heroFrame=25,heroRelativeMeshCentre=list(mid),contactRelativeMeshCentre=contact)

result=dict(source=str(source),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),blender=bpy.app.version_string,method='Read-only evaluated mesh world vertices and projected triangle clipping at stored phase markers; independent assertions, no render and no manifest truth booleans.',checks=rows,passed=sum(r['passed'] for r in rows),failed=sum(not r['passed'] for r in rows))
output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(result,indent=2)+'\n')
print('COO_GEOMETRY_AUDIT',str(output),result['passed'],'passed',result['failed'],'failed')
for row in rows:
 print('PASS' if row['passed'] else 'FAIL',row['name'],json.dumps({k:v for k,v in row.items() if k not in ('name','passed')}))
sys.exit(0 if not result['failed'] else 1)
