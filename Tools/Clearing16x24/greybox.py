"""Editable clearing composition. Run in Blender background; emits a source scene and greybox render."""
import bpy, math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Clearing16x24';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,c):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');e.inputs['Color'].default_value=(*c,1)
 o=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(e.outputs[0],o.inputs['Surface']);return m
colors=[mat('ground',(.003,.029,.017)),mat('structure',(.18,.21,.19)),mat('work surface',(.33,.36,.32)),mat('field',(.09,.13,.085)),mat('actor',(.64,.67,.60)),mat('path',(.035,.056,.036))]
def box(name,x,y,w,h,c,z=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y,z));o=bpy.context.object;o.name=name;o.scale=(w,h,.06);o.data.materials.append(colors[c]);return o
box('flat ground',0,0,20,12.5,0,-1)
# Coordinates and footprints remain reusable by the Unity authoring pass.
box('open central path',0,-.4,16,1.7,5,-.5)
box('north approach',0,1.8,2.4,5,5,-.5)
box('south approach',0,-3.4,2.4,4.5,5,-.5)
box('seed shelter roof',-5,3.4,4.5,2.0,1)
for x in [-7,-3]:box('shelter post',x,2.0,.25,1.2,2)
box('seed sorting bench',-5,1.8,2.8,.5,2)
box('seed keeper',-5,.9,.65,1.5,4,.1)
box('kitchen oven',5,3.2,1.6,1.8,1)
box('drying rack',7,3.2,1.5,1.8,2)
box('cook',4,1.2,.65,1.5,4,.1)
box('shared table',6,1.3,1.7,.65,2)
box('well',0,1.4,1.2,1.2,1)
for x in [-7,-6,-5,-4]:
 for y in [-2.1,-3.6]:box('cultivated bed',x,y,.78,1.08,3)
box('basket',-3,-3.5,.65,.75,2)
box('grass verge',-7,-4.8,4,.3,3)
for x in [3,4,5,6,7]:box('grazer fence',x,-4.5,.9,.35,1)
for y in [-3.6,-2.3]:box('grazer fence side',7.3,y,.2,1,1)
box('grazer',5,-2.8,1,.9,4)
box('tatterjay',2,-1.5,.6,.8,4)
box('beetle jar',-2,1.2,.4,.7,4)
for x,y in [(-8,4.7),(-8,-1),(8,-1),(8,4.7),(3,4.8)]:box('quiet verge',x,y,.75,.75,3)
cam=bpy.data.cameras.new('Flat camera');o=bpy.data.objects.new('Flat camera',cam);bpy.context.collection.objects.link(o);o.location=(0,0,30);cam.type='ORTHO';cam.ortho_scale=20
s=bpy.context.scene;s.camera=o;s.render.engine='CYCLES';s.cycles.samples=1;s.render.resolution_x=1280;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.filepath=str(OUT/'greybox.png');s.render.film_transparent=False;s.view_settings.view_transform='Standard';s.view_settings.look='None';s.render.filter_size=.01
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'clearing-greybox.blend'));bpy.ops.render.render(write_still=True)
