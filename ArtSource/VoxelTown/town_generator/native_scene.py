"""Blender preview boundary for rectangular native candidates and neutral kit."""
import math
import uuid

from .materials import make_palette,PALETTE_HEX
from .mesh import VoxelMesh
from .scene import OWNER,clear_generated,_collection


def validate_preview_inputs(chunk,assets):
    """Pure preflight before any generated Blender data is removed.

    The preview supports the current named palette. A different palette is an
    explicit incompatible input, not permission to recolor imported face IDs.
    Unity's independent importer can support broader palette interchange.
    """
    def vector(values):
        return len(values)==3 and all(not isinstance(v,bool) and isinstance(v,(int,float)) and math.isfinite(v) for v in values)
    try:
        from .native_region import NativeConfig
        w,h=chunk['width'],chunk['height'];NativeConfig(width=w,height=h).validate()
        expected=[{'name':name,'hex':color} for name,color in PALETTE_HEX.items()]
        if assets['palette']!=expected or assets['voxelSize']!=.25 or assets['cellSize']!=1:
            raise ValueError('Preview requires the current toolkit palette and native units')
        if not isinstance(chunk['zoneId'],str) or not chunk['zoneId']:raise ValueError('Missing zone identity')
        rows=assets['assets'];recipes={row['id']:row for row in rows}
        if len(rows)!=len(recipes):raise ValueError('Duplicate asset identity')
        bounds={}
        for row in rows:
            vertices=row['vertices'];faces=row['quads'];materials=row['quadMaterials']
            if len(vertices)<3 or not all(vector(v) for v in vertices):raise ValueError('Invalid preview vertices')
            if not faces or len(faces)!=len(materials):raise ValueError('Invalid preview faces/material count')
            for face,material in zip(faces,materials):
                if len(face)!=4 or len(set(face))!=4 or any(type(i) is not int or not 0<=i<len(vertices) for i in face):
                    raise ValueError('Invalid preview face indices')
                if type(material) is not int or not 0<=material<len(expected):raise ValueError('Invalid preview material index')
            if not vector(row['nativePlacementOffset']):raise ValueError('Invalid native mesh pivot')
            bounds[row['id']]=([min(v[i] for v in vertices) for i in range(3)],
                               [max(v[i] for v in vertices) for i in range(3)])
        terrain=chunk['terrain'];cells=set()
        for t in terrain:
            x,y=t['x'],t['y']
            if type(x) is not int or type(y) is not int or not (0<=x<w and 0<=y<h) or (x,y) in cells:
                raise ValueError('Invalid preview terrain coverage')
            if t['material'] not in PALETTE_HEX or type(t['water']) is not bool:raise ValueError('Invalid preview terrain material/water')
            cells.add((x,y))
        if len(cells)!=w*h:raise ValueError('Incomplete preview terrain')
        ids=set()
        for p in chunk['placements']:
            if not isinstance(p['id'],str) or not p['id'] or p['id'] in ids:raise ValueError('Invalid preview owner identity')
            ids.add(p['id']);recipe=recipes.get(p['asset'])
            if recipe is None:raise ValueError('Unknown preview asset')
            if not isinstance(p['role'],str):raise ValueError('Missing semantic role')
            if p['category'] not in ('wall','furniture','npc','prop','vegetation','crop'):raise ValueError('Invalid preview category')
            if type(p['x']) is not int or type(p['y']) is not int:raise ValueError('Invalid preview anchor')
            if type(p['rotationQuarterTurns']) is not int or p['rotationQuarterTurns'] not in range(4):raise ValueError('Invalid preview rotation')
            if isinstance(p['uniformScale'],bool) or p['uniformScale']!=1:raise ValueError('Preview candidates use unscaled native recipes')
            body={(p['x']+dx,p['y']+dy) for dx,dy in p['cells']}
            if not body or not body<=cells:raise ValueError('Preview footprint exceeds terrain')
            offset=recipe['nativePlacementOffset'];low,high=bounds[p['asset']]
            # Check the actual geometry bounds under the same quarter-turn and
            # pivot math used below, before constructing any Blender object.
            minx,maxx=min(v[0] for v in body),max(v[0] for v in body)+1
            miny,maxy=h-max(v[1] for v in body)-1,h-min(v[1] for v in body)
            for x in (low[0],high[0]):
                for y in (low[1],high[1]):
                    rx,ry=x+offset[0],y+offset[1]
                    for _ in range(p['rotationQuarterTurns']):rx,ry=ry,-rx
                    xx,yy=p['x']+.5+rx,h-p['y']-.5+ry
                    if not (minx-1e-6<=xx<=maxx+1e-6 and miny-1e-6<=yy<=maxy+1e-6):
                        raise ValueError('Preview geometry exceeds claimed native footprint')
        return True
    except (KeyError,TypeError,IndexError,AttributeError) as error:
        raise ValueError(f'Malformed native preview: {error}') from error


def realize_chunk(chunk,assets,scene=None):
    """Build linked meshes and bounded terrain, preserving manually owned data."""
    validate_preview_inputs(chunk,assets)
    import bpy
    from mathutils import Vector
    if scene is None:
        scene=next((s for s in bpy.data.scenes if s.get('native_voxel_zone')==chunk['zoneId'] and s.get('coo_voxel_owner')==OWNER),None)
        if scene is None:scene=bpy.data.scenes.new('Native '+chunk['zoneId'])
    scene['coo_voxel_owner']=OWNER;scene['native_voxel_zone']=chunk['zoneId']
    if not scene.get('coo_voxel_scene'):scene['coo_voxel_scene']=uuid.uuid4().hex
    clear_generated(scene)
    collections={key:_collection(scene,'GENERATED_'+key) for key in ('TERRAIN','BUILDINGS','PROPS','VEGETATION','WATER','NPCS','LIGHTING')}
    library=_collection(scene,'GENERATED_ASSET_LIBRARY',False)
    palette=make_palette(OWNER+':native:'+uuid.uuid4().hex)
    recipes={a['id']:a for a in assets['assets']};prototypes={}
    for asset_id in sorted({p['asset'] for p in chunk['placements']}):
        row=recipes[asset_id];mesh=bpy.data.meshes.new(asset_id)
        mesh['coo_voxel_owner']=OWNER;mesh.from_pydata(row['vertices'],[],row['quads']);mesh.update()
        for material in palette:mesh.materials.append(material)
        for polygon,material in zip(mesh.polygons,row['quadMaterials']):polygon.material_index=material
        obj=bpy.data.objects.new(asset_id,mesh);obj['coo_voxel_owner']=OWNER
        library.objects.link(obj);prototypes[asset_id]=obj
    batches={};water=VoxelMesh(.25);height=chunk['height']
    for cell in chunk['terrain']:
        x,y=cell['x'],height-1-cell['y'];key=(x//16,y//16)
        mesh=batches.setdefault(key,VoxelMesh(.25))
        bottom=-.75 if cell['water'] else -.25
        mesh.bounds((x,y,bottom),(x+1,y+1,bottom+.25),cell['material'])
        if cell['water']:water.bounds((x,y,-.5),(x+1,y+1,-.25),'water')
    for key,mesh in batches.items():mesh.build(f'native-ground-{key[0]}-{key[1]}',collections['TERRAIN'],palette,OWNER)
    if water.cells:water.build('native-water',collections['WATER'],palette,OWNER)
    for row in chunk['placements']:
        obj=bpy.data.objects.new(row['id'],prototypes[row['asset']].data);obj['coo_voxel_owner']=OWNER
        obj['semantic_id']=row['id'];obj['role']=row['role'];obj['native_cells']=str(row['cells'])
        angle=-row['rotationQuarterTurns']*math.pi/2;offset=recipes[row['asset']]['nativePlacementOffset']
        ox=math.cos(angle)*offset[0]-math.sin(angle)*offset[1]
        oy=math.sin(angle)*offset[0]+math.cos(angle)*offset[1]
        obj.location=(row['x']+.5+ox,height-row['y']-.5+oy,0)
        obj.rotation_euler.z=angle;obj.scale=(row['uniformScale'],)*3
        group='BUILDINGS' if row['category']=='wall' else 'VEGETATION' if row['category'] in ('vegetation','crop') else 'NPCS' if row['category']=='npc' else 'PROPS'
        collections[group].objects.link(obj)
    scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.render.resolution_x=1920;scene.render.resolution_y=800;scene.render.resolution_percentage=100
    world=bpy.data.worlds.new('Native voxel ambient');world['coo_voxel_owner']=OWNER;world.use_nodes=True
    world.node_tree.nodes['Background'].inputs['Color'].default_value=(.50,.60,.68,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
    if scene.world is None or scene.world.get('coo_voxel_owner')==OWNER:scene.world=world
    else:bpy.data.worlds.remove(world)
    sun_data=bpy.data.lights.new('Native voxel sun','SUN');sun_data['coo_voxel_owner']=OWNER
    sun_data.energy=2.;sun_data.angle=.20;sun_data.color=(1.,.85,.65)
    sun=bpy.data.objects.new('Native voxel sun',sun_data);sun['coo_voxel_owner']=OWNER
    sun.rotation_euler=(math.radians(25),math.radians(-20),math.radians(-35));collections['LIGHTING'].objects.link(sun)
    camera_data=bpy.data.cameras.new('Native gameplay camera');camera_data['coo_voxel_owner']=OWNER;camera_data.type='ORTHO'
    camera=bpy.data.objects.new('Native gameplay camera',camera_data);camera['coo_voxel_owner']=OWNER
    collections['LIGHTING'].objects.link(camera)
    direction=Vector((0,-math.cos(math.radians(63)),math.sin(math.radians(63))))
    rotation=(-direction).to_track_quat('-Z','Y');inverse=rotation.to_matrix().transposed()
    corners=[inverse@Vector((x,y,z)) for x in (0,chunk['width']) for y in (0,height) for z in (-1,6)]
    xmin,xmax=min(v.x for v in corners),max(v.x for v in corners);ymin,ymax=min(v.y for v in corners),max(v.y for v in corners)
    target=rotation@Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
    camera.location=target+direction*max(chunk['width'],height)*2;camera.rotation_euler=rotation.to_euler()
    aspect=scene.render.resolution_x/scene.render.resolution_y
    camera_data.ortho_scale=max(xmax-xmin,(ymax-ymin)*aspect)*1.08
    camera_data.clip_end=max(chunk['width'],height)*6
    if scene.camera is None or scene.camera.get('coo_voxel_owner')==OWNER:scene.camera=camera
    scene.view_settings.view_transform='AgX'
    return scene
