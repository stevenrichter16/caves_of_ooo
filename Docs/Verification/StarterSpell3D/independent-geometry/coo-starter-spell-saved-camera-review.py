import bpy, json, math, hashlib
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
source=Path('/Users/steven/caves-of-ooo/ArtSource/StarterSpell3D/starter_spells.blend')
bpy.ops.wm.open_mainfile(filepath=str(source))
rows=[]
for scene in bpy.data.scenes:
 if not any(o.name.endswith('__ActorRoot') for o in scene.objects):continue
 bpy.context.window.scene=scene;bpy.context.view_layer.update()
 cam=scene.camera;assert cam is not None
 ray=cam.matrix_world.to_quaternion()@Vector((0,0,-1))
 pitch=math.degrees(math.asin(-ray.z))
 coords=[world_to_camera_view(scene,cam,Vector(p)) for p in [(0,0,0),(1,0,0),(0,1,0),(0,0,1)]]
 px=(coords[1].x-coords[0].x)*scene.render.resolution_x
 py=(coords[2].y-coords[0].y)*scene.render.resolution_y
 ph=(coords[3].y-coords[0].y)*scene.render.resolution_y
 checks=dict(pitch56=abs(pitch-56)<1e-4,horizontalFit=cam.data.sensor_fit=='HORIZONTAL',compensatedAspect=abs(scene.render.pixel_aspect_x-1/math.sin(math.radians(56)))<1e-6 and scene.render.pixel_aspect_y==1,unitGroundSpansEqual=abs(px-py)<1e-3,heightParallax=abs(ph/py-1/math.tan(math.radians(56)))<2e-6)
 rows.append(dict(scene=scene.name,camera=cam.name,pitchDegrees=pitch,orthographic=cam.data.type,orthographicScale=cam.data.ortho_scale,actualPixelAspect=[scene.render.pixel_aspect_x,scene.render.pixel_aspect_y],sourceResolution=[scene.render.resolution_x,scene.render.resolution_y],unitEastPixels=px,unitNorthPixels=py,heightParallaxInGroundUnits=ph/py,checks=checks))
out=dict(source=str(source),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),method='Actual saved Blender scene camera matrices and world_to_camera_view, no render.',scenes=rows,passed=sum(sum(row['checks'].values()) for row in rows),failed=sum(sum(not v for v in row['checks'].values()) for row in rows))
Path('/tmp/coo-starter-spell-final-camera-review.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(out,indent=2))
assert len(rows)==7
assert out['failed']==0
