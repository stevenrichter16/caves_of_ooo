import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from town_generator.config import Config
from town_generator.layout import generate_town
from town_generator.scene import realize
s=bpy.data.scenes.new('ManualScene')
w=bpy.data.worlds.new('HandPaintedWorld');w.color=(.12,.34,.56);s.world=w
cam=bpy.data.objects.new('ManualCamera',bpy.data.cameras.new('ManualLens'));s.collection.objects.link(cam);s.camera=cam
realize(generate_town(Config(building_count=0,population=0,vegetation_amount=0,ruin_amount=0)),s)
assert s.world is w,'Manual world assignment was replaced'
assert s.camera is cam,'Manual camera assignment was replaced'
print('MANUAL SCENE STATE PASS')
