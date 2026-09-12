"""Warm sun, readable ambient fill and a bounded set of practical lights."""
import math


def build_lighting(scene,collection,owner,props):
    import bpy
    if scene.world is None or str(scene.world.get('coo_voxel_owner','')).split(':')[0]==owner:
        old=scene.world
        world=bpy.data.worlds.new('Voxel atmosphere');world['coo_voxel_owner']=owner
        world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.43,.53,.58,1)
        world.node_tree.nodes['Background'].inputs[1].default_value=.65
        scene.world=world
        if old and old.users==0:bpy.data.worlds.remove(old)
    def light(name,kind,position,color,power):
        data=bpy.data.lights.new(name,kind);data.color=color;data.energy=power;data['coo_voxel_owner']=owner
        obj=bpy.data.objects.new(name,data);obj.location=position;obj['coo_voxel_owner']=owner;collection.objects.link(obj)
        return obj
    sun=light('Warm afternoon sun','SUN',(0,0,30),(1,.85,.64),3.0)
    sun.rotation_euler=tuple(math.radians(a) for a in (28,-22,-28));sun.data.angle=math.radians(8)
    candidates=[p for p in props if p['asset'] in ('lantern','torch','firepit','furnace')]
    for p in candidates[:16]:
        x,y,z=p['position'];lamp=light(p['id']+'_light','POINT',(x,y,z+1.65),(1,.36,.06),45 if p['asset']=='lantern' else 80)
        lamp.data.shadow_soft_size=.35
    scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
    scene.render.resolution_x=1800;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
    scene.view_settings.view_transform='AgX';scene.view_settings.exposure=.35
