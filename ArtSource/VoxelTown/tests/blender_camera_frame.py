import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
from town_generator.config import Config,ARCHETYPES
from town_generator.layout import generate_town
from town_generator.buildings import building_bounds,farm_bounds
from town_generator.camera import build_camera
for seed in (0,41,73):
 for ratio in ((1600,1400),(1920,1080),(1080,1920)):
  town=generate_town(Config(seed=seed,archetypes=ARCHETYPES));s=bpy.data.scenes.new('FrameAudit');s.render.resolution_x,s.render.resolution_y=ratio
  cam=build_camera(s,s.collection,'test-owner',town)
  s.view_layers[0].update()
  for a,b,c,d in [building_bounds(v) for v in town.buildings]+[farm_bounds(f) for f in town.farms]:
   for x,y in ((a,b),(a,d),(c,b),(c,d)):
    for z in (0,4):
     point=world_to_camera_view(s,cam,Vector((x,y,z)))
     assert .025<=point.x<=.975 and .025<=point.y<=.975,(seed,ratio,tuple(point))
print('CAMERA FRAME PASS:3 seeds x3 aspect ratios x all building/farm corners')
