"""Blender boundary: safe ownership, linked voxel assets, semantic scene assembly."""
import math
import uuid
from .materials import make_palette, PALETTE_NAMES
from .mesh import VoxelMesh
from .terrain import ground_cells, ground_height, in_water, water_distance, ground_material
from .spatial import Clearance

OWNER = 'caves-of-ooo.voxel-town.v1'
COLLECTIONS = ('TERRAIN','BUILDINGS','PROPS','VEGETATION','WATER','NPCS','LIGHTING')


def _owned(data):
    return str(data.get('coo_voxel_owner','')).split(':')[0] == OWNER


def clear_generated(scene):
    """Remove this scene's generated content, preserving manual and borrowed data.

    A matching name alone never grants ownership. Manual children of owned
    collections are relinked into the scene before their container is removed.
    Shared objects/collections visible in another scene remain intact there.
    """
    import bpy
    uid = scene.get('coo_voxel_scene')
    local = set(scene.collection.children_recursive)
    targets = {c for c in local if _owned(c)}
    if uid:
        targets.update(c for c in bpy.data.collections if _owned(c) and c.get('coo_voxel_scene') == uid)
    foreign = set()
    foreign_objects = set()
    for other in bpy.data.scenes:
        if other != scene:
            foreign.update(other.collection.children_recursive)
            foreign_objects.update(other.objects)
    # Collection instances are borrowers too, including unlinked/manual source
    # libraries and instances in other scenes. Never invalidate their source.
    for instance in bpy.data.objects:
        if not _owned(instance) and instance.instance_collection is not None:
            source=instance.instance_collection
            borrowed={source,*source.children_recursive}
            foreign.update(borrowed)
            for c in borrowed:foreign_objects.update(c.objects)
    objects = {o for c in targets for o in c.objects}
    doomed={o for o in objects if _owned(o) and o not in foreign_objects}
    for view_layer in scene.view_layers:view_layer.update()
    # A manual child may live in a manual collection while its parent is owned.
    # Detach it before removal and retain the evaluated world-space transform.
    for obj in list(bpy.data.objects):
        if obj not in doomed and obj.parent in doomed:
            transform=obj.matrix_world.copy()
            obj.parent=None;obj.matrix_world=transform
    for obj in objects:
        if not _owned(obj):
            if obj.name not in scene.collection.objects:
                scene.collection.objects.link(obj)
        elif obj not in foreign_objects:
            bpy.data.objects.remove(obj, do_unlink=True)
        else:
            for c in list(obj.users_collection):
                if c in targets and c not in foreign:
                    c.objects.unlink(obj)
    # Preserve nested manual collections before unlinking generated parents.
    for c in targets:
        for child in list(c.children):
            if child not in targets and child.name not in scene.collection.children:
                scene.collection.children.link(child)
    for c in targets:
        if c in foreign:
            for parent in [scene.collection] + list(local - foreign):
                if c.name in parent.children:
                    parent.children.unlink(c)
        else:
            bpy.data.collections.remove(c)
    for group in (bpy.data.meshes,bpy.data.cameras,bpy.data.lights,bpy.data.materials,bpy.data.worlds):
        for data in list(group):
            if _owned(data) and data.users == 0:
                group.remove(data)


def _collection(scene, name, link=True):
    import bpy
    c = bpy.data.collections.new(name)
    c['coo_voxel_owner'] = OWNER
    c['coo_voxel_scene'] = scene['coo_voxel_scene']
    if link: scene.collection.children.link(c)
    else: c.use_fake_user = True
    return c


def voxel_runs(mesh):
    """Lossless x-axis runs: [x,y,z,length,material_index], all in voxel units."""
    runs=[]
    for x,y,z in sorted(mesh.cells,key=lambda p:(p[2],p[1],p[0])):
        material=mesh.cells[x,y,z]
        if runs and runs[-1][1:3]==[y,z] and runs[-1][0]+runs[-1][3]==x and runs[-1][4]==material:
            runs[-1][3]+=1
        else:runs.append([x,y,z,1,material])
    return runs


def realize(town, scene=None):
    """Realize a semantic Town without mutating its source model.

    Return (scene, manifest, stats). The manifest retains voxel occupancy and
    entity/asset IDs independently of greedy render meshes. It is an interchange
    foundation, not a claim of Unity runtime destruction or navigation support.
    """
    import bpy
    from .assets import build_assets, build_recipes
    from .architecture import build_structure
    from .props import plan_props
    from .vegetation import plan_vegetation
    from .population import plan_population
    from .camera import build_camera
    from .lighting import build_lighting
    town.config.validate()
    # All fallible pure planning completes before the previous generation is
    # touched. Invalid content is a rejection, not an erase-and-partial-rebuild.
    props=plan_props(town);npcs=plan_population(town);vegetation=plan_vegetation(town)
    structures=[(b,build_structure(b,town.config.voxel_size)) for b in town.buildings]
    recipes=build_recipes(town.config.voxel_size)
    if scene is None:
        scene=next((s for s in bpy.data.scenes if s.get('coo_voxel_owner')==OWNER),None)
        if scene is None:scene=bpy.data.scenes.new('Voxel Settlement')
    scene['coo_voxel_owner']=OWNER
    if not scene.get('coo_voxel_scene'):scene['coo_voxel_scene']=uuid.uuid4().hex
    clear_generated(scene)
    collections={k:_collection(scene,'GENERATED_'+k) for k in COLLECTIONS}
    templates=_collection(scene,'GENERATED_ASSET_LIBRARY',False)
    # Fresh palette avoids rewriting a material a manual object has borrowed.
    palette=make_palette(OWNER+':'+uuid.uuid4().hex)
    prototypes=build_assets(templates,palette,town.config.voxel_size,OWNER)
    manifest={'schema':1,'units':'metres','axes':'Blender: X east, Y north, Z up',
              'rotation_units':'radians','voxel_size':town.config.voxel_size,'palette':list(PALETTE_NAMES),
              'assets':{},'terrain':[],'structures':[],'instances':[]}
    for key,recipe in recipes.items():
        manifest['assets'][key]={'runs':voxel_runs(recipe),'cells':recipe.cell_count}
    q=town.config.voxel_size;clearance=Clearance(town);batches={}
    water=VoxelMesh(q)
    # Fine samples near routes eliminate metre-wide stair steps while broad,
    # coherent terrain patches retain cheap generation over large maps.
    for cell in ground_cells(town):
        x,y,z=cell['center'];sx,sy,sz=cell['size']
        key=(math.floor(x/16),math.floor(y/16))
        batch=batches.setdefault(key,VoxelMesh(q))
        subdivisions=4 if clearance.near_path(x,y,1.5) or any(abs(water_distance(x,y,w))<2 for w in town.water_bodies) else 1
        for ix in range(subdivisions):
            for iy in range(subdivisions):
                xx=x-sx/2+(ix+.5)*sx/subdivisions;yy=y-sy/2+(iy+.5)*sy/subdivisions
                wet=in_water(xx,yy,town)
                zz=-.75 if wet else (ground_height(xx,yy,town)-.25 if subdivisions>1 else z)
                mat=ground_material(xx,yy,town)
                if not wet and clearance.near_path(xx,yy,0):mat='worn_ground'
                batch.box((xx,yy,zz),(sx/subdivisions,sy/subdivisions,sz),mat)
                if wet:water.box((xx,yy,-.375),(sx/subdivisions,sy/subdivisions,.25),'water')
    for key,batch in batches.items():
        name=f'terrain_{key[0]}_{key[1]}'
        batch.build(name,collections['TERRAIN'],palette,OWNER)
        manifest['terrain'].append({'id':name,'runs':voxel_runs(batch)})
    if water.cell_count:
        water.build('oasis',collections['WATER'],palette,OWNER)
        manifest['terrain'].append({'id':'oasis','runs':voxel_runs(water),'role':'water'})
    for b,mesh in structures:
        obj=mesh.build(b.id+'_'+b.role,collections['BUILDINGS'],palette,OWNER)
        obj.location=(*b.position,0);obj.rotation_euler.z=b.rotation
        obj['semantic_id']=b.id;obj['role']=b.role
        manifest['structures'].append({'id':b.id,'role':b.role,'position':list(obj.location),
            'rotation':b.rotation,'runs':voxel_runs(mesh)})
    for group,rows in [('PROPS',props),('VEGETATION',vegetation),('NPCS',npcs)]:
        for row in rows:
            key=row['asset']
            obj=bpy.data.objects.new(row['id'],prototypes[key].data)
            obj['coo_voxel_owner']=OWNER
            obj['semantic_id']=row['id'];obj['asset_key']=key;obj['role']=row.get('role','')
            obj.location=row['position'];obj.rotation_euler.z=row.get('rotation',0)
            obj.scale=row.get('scale',(1,1,1));collections[group].objects.link(obj)
            manifest['instances'].append(dict(row,collection=group))
    build_lighting(scene,collections['LIGHTING'],OWNER,props)
    build_camera(scene,collections['LIGHTING'],OWNER,town)
    stats={'buildings':len(town.buildings),'paths':len(town.paths),'farms':len(town.farms),
        'props':len(props),'vegetation':len(vegetation),'npcs':len(npcs),
        'residents':sum(n.get('role')!='livestock' for n in npcs),'livestock':sum(n.get('role')=='livestock' for n in npcs),
        'visible_objects':len(scene.objects),'unique_meshes':len({o.data for o in scene.objects if o.type=='MESH'}),
        'visible_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in scene.objects if o.type=='MESH'),
        'unique_asset_voxels':sum(r.cell_count for r in recipes.values()),
        'terrain_voxels':sum(m.cell_count for m in batches.values()),
        'asset_prototypes':len(prototypes)}
    return scene,manifest,stats
