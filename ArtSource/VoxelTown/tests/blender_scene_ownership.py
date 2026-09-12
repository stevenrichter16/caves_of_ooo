"""Run with Blender, not CPython. Exercise real manual/borrowed datablock survival."""
import bpy,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from town_generator.scene import clear_generated,OWNER
scene=bpy.data.scenes.new('OwnershipProbe');coll=bpy.data.collections.new('GENERATED_BUILDINGS');scene.collection.children.link(coll);coll['coo_voxel_owner']=OWNER
manualmesh=bpy.data.meshes.new('ManualMesh');manualmesh.from_pydata([(0,0,0),(1,0,0),(0,1,0)],[],[(0,1,2)])
manual=bpy.data.objects.new('ManualChild',manualmesh);coll.objects.link(manual);manual.location=(2,3,4)
ownedmesh=bpy.data.meshes.new('OwnedSharedMesh');ownedmesh['coo_voxel_owner']=OWNER;ownedmesh.from_pydata([(0,0,0),(1,0,0),(0,1,0)],[],[(0,1,2)])
owned=bpy.data.objects.new('OwnedObject',ownedmesh);owned['coo_voxel_owner']=OWNER;coll.objects.link(owned)
borrower=bpy.data.objects.new('ManualBorrower',ownedmesh);scene.collection.objects.link(borrower)
foreign=bpy.data.collections.new('GENERATED_TERRAIN');scene.collection.children.link(foreign);foreign_obj=bpy.data.objects.new('ForeignObject',None);foreign.objects.link(foreign_obj)
foreign['coo_voxel_owner']='different-generator'
clear_generated(scene)
assert manual.name in bpy.data.objects and tuple(manual.location)==(2,3,4)
assert manualmesh.name in bpy.data.meshes and borrower.data is ownedmesh
assert foreign.name in bpy.data.collections and foreign_obj.name in bpy.data.objects
assert 'OwnedObject' not in bpy.data.objects
assert manual.name in scene.objects
clear_generated(scene)
assert manual.name in scene.objects and borrower.data is ownedmesh
print('OWNERSHIP PASS: manual child, borrowed owned mesh, foreign owner, name collision and repeated cleanup')
