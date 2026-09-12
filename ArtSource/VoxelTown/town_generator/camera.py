"""Automatically framed orthographic RPG camera; no demo-specific transforms."""
import math


def build_camera(scene,collection,owner,town):
    import bpy
    from mathutils import Vector
    data=bpy.data.cameras.new('Voxel gameplay camera');data.type='ORTHO'
    data['coo_voxel_owner']=owner
    obj=bpy.data.objects.new('Voxel gameplay camera',data);obj['coo_voxel_owner']=owner
    collection.objects.link(obj)
    # 63 degrees above ground exposes both inhabitants and accessible interiors.
    # Fit projected semantic bounds, accounting for the render's aspect ratio.
    from .buildings import building_bounds, farm_bounds
    elevation=math.radians(63);azimuth=math.radians(-83);distance=town.config.town_size*1.7
    direction=Vector((math.cos(elevation)*math.cos(azimuth),math.cos(elevation)*math.sin(azimuth),math.sin(elevation)))
    rotation=(-direction).to_track_quat('-Z','Y')
    inverse=rotation.to_matrix().transposed()
    corners=[]
    for a,b,c,d in [building_bounds(v) for v in town.buildings]+[farm_bounds(f) for f in town.farms]:
        corners.extend(Vector((x,y,z)) for x,y in ((a,b),(a,d),(c,b),(c,d)) for z in (0,5))
    for key in ('plaza','water','main_entrance','secondary_entrance'):
        x,y=town.anchors[key];corners.extend((Vector((x-2,y-2,0)),Vector((x+2,y+2,3))))
    if not town.buildings:
        half=town.config.town_size/2
        corners=[Vector((x,y,0)) for x in (-half,half) for y in (-half,half)]
    projected=[inverse@v for v in corners]
    xmin,xmax=min(p.x for p in projected),max(p.x for p in projected)
    ymin,ymax=min(p.y for p in projected),max(p.y for p in projected)
    target=rotation@Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
    obj.location=target+direction*distance;obj.rotation_euler=rotation.to_euler()
    aspect=(scene.render.resolution_x*scene.render.pixel_aspect_x)/(scene.render.resolution_y*scene.render.pixel_aspect_y)
    # Blender ortho_scale spans the larger aperture dimension, not always width.
    data.ortho_scale=max(xmax-xmin,(ymax-ymin)*aspect)*1.12 if aspect>=1 else max((xmax-xmin)/aspect,ymax-ymin)*1.12
    data.clip_start=.1;data.clip_end=town.config.town_size*8
    if scene.camera is None or scene.camera.get('coo_voxel_owner')==owner:
        scene.camera=obj
    return obj
