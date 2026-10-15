"""Offline review of the changed world objects, from their exact box source.
Run Blender --background --python this_file -- --output ArtSource/ExamineLedger/review
This is not a Unity capture and does not load the imagegen targets as textures.
"""
import argparse
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
args.output.mkdir(parents=True, exist_ok=True)
root = Path(__file__).resolve().parents[2]
repair = json.loads((root / 'ArtSource/RepairCultivation3D/kit.json').read_text())
yard = json.loads((root / 'ArtSource/CurationYard3D/kit.json').read_text())
models = {m['id']: m for pack in (repair, yard) for m in pack['models']}
selected = [
    ('repair-cultivation-lined-well-broken', 'CLAY LINING'),
    ('repair-cultivation-rope-well-broken', 'FRAYED ROPE'),
    ('repair-cultivation-wooden-gate-closed', 'TIMBER GATE'),
    ('repair-cultivation-knotflax-2-dry', 'KNOTFLAX'),
    ('repair-cultivation-hearthbulb-2-dry', 'HEARTHBULB'),
    ('repair-cultivation-seamleaf-2-dry', 'SEAMLEAF'),
    ('curation-yard-salt-cured-body-2', 'PRESERVED CASE II'),
    ('repair-cultivation-fire-clay', 'FIRE CLAY'),
]
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = []
for i, value in enumerate(repair['palette']):
    rgb = [int(value[j:j+2], 16) / 255 for j in (1, 3, 5)]
    linear = [v / 12.92 if v <= .04045 else ((v+.055) / 1.055)**2.4 for v in rgb]
    material = bpy.data.materials.new('Native palette ' + str(i))
    material.diffuse_color = (*linear, 1)
    material.use_nodes = True
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*linear, 1)
    material.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1
    materials.append(material)
for index, (model_id, title) in enumerate(selected):
    position = Vector(((index % 3 - 1) * 1.65, (1 - index // 3) * 1.8, 0))
    for box in models[model_id]['boxes']:
        c, size = box['center'], box['size']
        bpy.ops.mesh.primitive_cube_add(size=1, location=position + Vector((c['x'], c['z'], c['y'])))
        obj = bpy.context.object
        obj.name = model_id
        obj.scale = (size['x'], size['z'], size['y'])
        obj.data.materials.append(materials[box['color']])
    font = bpy.data.curves.new(title, 'FONT')
    font.body, font.size, font.align_x = title, .095, 'CENTER'
    text = bpy.data.objects.new(title, font)
    bpy.context.scene.collection.objects.link(text)
    text.location = position + Vector((0, -.7, .02))
    font.materials.append(materials[17])
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 8
scene.render.threads_mode, scene.render.threads = 'FIXED', 2
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.08, .12, .10, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .8
light = bpy.data.objects.new('Diffuse review key', bpy.data.lights.new('Review key', 'AREA'))
scene.collection.objects.link(light)
light.location, light.data.energy, light.data.size = (-3, -4, 8), 650, 4
camera = bpy.data.objects.new('Source review camera', bpy.data.cameras.new('Source review camera'))
scene.collection.objects.link(camera)
camera.location = (0, -8, 10)
camera.rotation_euler = (Vector((0, 0, .25)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type, camera.data.ortho_scale = 'ORTHO', 5.7
scene.camera = camera
scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1000, 1000, 100
scene.view_settings.view_transform = 'Standard'
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(args.output / 'eight-world-refinements.png')
bpy.ops.render.render(write_still=True)
