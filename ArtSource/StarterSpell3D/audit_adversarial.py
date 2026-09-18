"""Real in-memory asset mutations. Never saves the damaged Blender data."""
import bpy, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;FILE=ROOT/'starter_spells.blend';before=hashlib.sha256(FILE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(FILE));manifest=json.loads((ROOT/'manifest.json').read_text());checks=[]
def need(name,value,actual):checks.append(dict(name=name,passed=bool(value),actual=actual))
def scene_for(sid):
 spec=next(s for s in manifest['studies'] if s['id']==sid);scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene;return spec,scene
def pose_delta(scene,rig):
 rows=[]
 for frame in [0,12,22,45]:
  scene.frame_set(frame);rows.append([float(v) for bone in rig.pose.bones for v in bone.rotation_euler])
 return max(abs(a-b) for row in rows for a,b in zip(row,rows[0]))
def object_delta(scene,ob):
 rows=[]
 for frame in [0,12,22,35,70,110]:
  scene.frame_set(frame);bpy.context.view_layer.update();rows.append([float(v) for row in ob.matrix_world for v in row])
 return max(abs(a-b) for row in rows for a,b in zip(row,rows[0]))
spec,scene=scene_for('ember_spit');rig=bpy.data.objects[spec['id']+'__CasterRig'];before_delta=pose_delta(scene,rig)
need('Baseline caster gesture really moves',before_delta>.1,before_delta)
rig.animation_data_clear()
for b in rig.pose.bones:b.rotation_euler=(0,0,0)
after_delta=pose_delta(scene,rig);need('Removing the actual caster action is detected',after_delta==0,after_delta)

spec,scene=scene_for('flaming_hands');fx=next(o for o in scene.objects if o.type=='MESH' and o.get('transientEffect'));before_delta=object_delta(scene,fx)
need('Baseline standalone effect transform really moves',before_delta>.2,before_delta)
fx.animation_data_clear();fx.scale=(.5,.5,.5);after_delta=object_delta(scene,fx)
need('Removing the actual mesh action is detected',after_delta==0,after_delta)

spec,scene=scene_for('jet_blast');fx=next(o for o in scene.objects if o.type=='MESH' and o.get('transientEffect'));scene.frame_set(110);bpy.context.view_layer.update();scale=max(abs(v) for v in fx.matrix_world.to_scale())
need('Baseline effect is completely cleared',scale<.002,scale)
fx.scale=(.2,.2,.2);fx.keyframe_insert('scale',frame=110);scene.frame_set(110);bpy.context.view_layer.update();scale=max(abs(v) for v in fx.matrix_world.to_scale())
need('A real late lingering effect is detected',scale>.002,scale)

spec,scene=scene_for('ground_surge');root=bpy.data.objects[spec['actorRoot']];before_delta=object_delta(scene,root)
need('Baseline actor root is fixed',before_delta==0,before_delta)
root.keyframe_insert('location',frame=0);root.location.x+=.5;root.keyframe_insert('location',frame=22);after_delta=object_delta(scene,root)
need('Actual injected caster-root translation is detected',after_delta>.4,after_delta)

spec,scene=scene_for('rime_grip');name=spec['actorRoot'];root=bpy.data.objects[name]
need('Baseline required named caster root exists',scene.objects.get(name) is root,name)
root.name='MUTATED_missing_required_actor_root'
need('Renaming actual required hierarchy root is detected',scene.objects.get(name) is None,list(o.name for o in scene.objects if o.get('nativeCellRoot')))

spec,scene=scene_for('calm');actual=[o for o in scene.objects if o.type=='MESH' and o.get('transientEffect')];expected=len(spec['animatedMeshes'])
need('Baseline transient inventory matches manifest',len(actual)==expected,len(actual))
bpy.data.objects.remove(actual[0],do_unlink=True);remaining=sum(o.type=='MESH' and bool(o.get('transientEffect')) for o in scene.objects)
need('Deleting an actual named effect mesh is detected',remaining!=expected,remaining)

spec,scene=scene_for('conjure_rain');fx=next(o for o in scene.objects if o.type=='MESH' and o.get('transientEffect'));mat=fx.data.materials[0].copy();fx.data.materials[0]=mat;node=mat.node_tree.nodes.get('Principled BSDF')
alpha=node.inputs['Alpha'].default_value;need('Baseline effect material is opaque',alpha==1,alpha)
node.inputs['Alpha'].default_value=.5;alpha=node.inputs['Alpha'].default_value
need('Introducing actual shader transparency is detected',alpha!=1,alpha)

after=hashlib.sha256(FILE.read_bytes()).hexdigest();need('In-memory mutations never alter delivered blend',before==after,dict(before=before,after=after))
report=dict(status='GREEN' if all(c['passed'] for c in checks) else 'RED',assertions=len(checks),passed=sum(c['passed'] for c in checks),checks=checks)
(ROOT/'adversarial-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if report['status']!='GREEN':raise SystemExit(1)
