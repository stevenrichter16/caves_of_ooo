"""Dedicated actual-bpy ownership taxonomy gate; run with Blender."""
import sys,unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from town_generator.scene import clear_generated,OWNER

class OwnershipTests(unittest.TestCase):
    def setUp(self):
        self.scene=bpy.data.scenes.new('AuditScene')
        self.c=bpy.data.collections.new('GENERATED_PROPS')
        self.c['coo_voxel_owner']=OWNER;self.scene.collection.children.link(self.c)
    def mesh(self,owned=True):
        mesh=bpy.data.meshes.new('AuditMesh')
        if owned:mesh['coo_voxel_owner']=OWNER
        mesh.from_pydata([(0,0,0),(1,0,0),(0,1,0)],[],[(0,1,2)])
        return mesh
    def obj(self,owned=True,mesh=None,collection=None):
        o=bpy.data.objects.new('AuditObject',mesh)
        if owned:o['coo_voxel_owner']=OWNER
        (collection or self.c).objects.link(o);return o
    def test_owned_object_removed(self):
        o=self.obj();name=o.name;clear_generated(self.scene);self.assertNotIn(name,bpy.data.objects)
    def test_unowned_object_preserved(self):
        o=self.obj(False);clear_generated(self.scene);self.assertIn(o.name,self.scene.objects)
    def test_unowned_transform_preserved(self):
        o=self.obj(False);o.location=(4,8,3);clear_generated(self.scene);self.assertEqual(tuple(o.location),(4,8,3))
    def test_borrowed_mesh_survives(self):
        mesh=self.mesh();self.obj(True,mesh);manual=self.obj(False,mesh);clear_generated(self.scene);self.assertIs(manual.data,mesh)
    def test_unused_owned_mesh_removed(self):
        mesh=self.mesh();name=mesh.name;self.obj(True,mesh);clear_generated(self.scene);self.assertNotIn(name,bpy.data.meshes)
    def test_unused_manual_mesh_preserved(self):
        mesh=self.mesh(False);name=mesh.name;self.obj(True,mesh);clear_generated(self.scene);self.assertIn(name,bpy.data.meshes)
    def test_nested_manual_collection_survives(self):
        child=bpy.data.collections.new('ManualNest');self.c.children.link(child);o=self.obj(False,collection=child)
        clear_generated(self.scene);self.assertIn(o.name,self.scene.objects)
    def test_nested_owned_object_removed(self):
        child=bpy.data.collections.new('OwnedNest');child['coo_voxel_owner']=OWNER;self.c.children.link(child)
        o=self.obj(True,collection=child);name=o.name;clear_generated(self.scene);self.assertNotIn(name,bpy.data.objects)
    def test_foreign_owner_preserved(self):
        self.c['coo_voxel_owner']='somebody-else';o=self.obj(False);clear_generated(self.scene);self.assertIn(o.name,self.scene.objects)
    def test_name_does_not_grant_ownership(self):
        del self.c['coo_voxel_owner'];o=self.obj(False);clear_generated(self.scene);self.assertIn(o.name,self.scene.objects)
    def test_repeat_cleanup_idempotent(self):
        o=self.obj(False);clear_generated(self.scene);clear_generated(self.scene);self.assertIn(o.name,self.scene.objects)
    def test_foreign_scene_shared_object_survives(self):
        other=bpy.data.scenes.new('OtherAudit');o=self.obj();other.collection.objects.link(o)
        clear_generated(self.scene);self.assertIn(o.name,other.objects)
    def test_shared_collection_foreign_scene_survives(self):
        other=bpy.data.scenes.new('OtherAudit');other.collection.children.link(self.c);o=self.obj()
        clear_generated(self.scene);self.assertIn(o.name,other.objects)
    def test_owned_object_in_unowned_collection_untouched(self):
        c=bpy.data.collections.new('ManualCollection');self.scene.collection.children.link(c);o=self.obj(True,collection=c)
        clear_generated(self.scene);self.assertIn(o.name,self.scene.objects)
    def test_manual_camera_preserved(self):
        data=bpy.data.cameras.new('ManualCamera');o=self.obj(False,data);self.scene.camera=o
        clear_generated(self.scene);self.assertIs(self.scene.camera,o)
    def test_manual_world_preserved(self):
        w=bpy.data.worlds.new('ManualWorld');self.scene.world=w;clear_generated(self.scene);self.assertIs(self.scene.world,w)
    def test_unlinked_own_templates_removed(self):
        self.scene['coo_voxel_scene']='probe';c=bpy.data.collections.new('Template');c['coo_voxel_owner']=OWNER;c['coo_voxel_scene']='probe';c.use_fake_user=True
        o=self.obj(True,collection=c);name=o.name;clear_generated(self.scene);self.assertNotIn(name,bpy.data.objects)
    def test_unlinked_foreign_templates_preserved(self):
        self.scene['coo_voxel_scene']='probe';c=bpy.data.collections.new('Template');c['coo_voxel_owner']=OWNER;c['coo_voxel_scene']='different';c.use_fake_user=True
        o=self.obj(True,collection=c);name=o.name;clear_generated(self.scene);self.assertIn(name,bpy.data.objects)
    def test_manual_child_visibility_flags_preserved(self):
        o=self.obj(False);o.hide_render=True;clear_generated(self.scene);self.assertTrue(o.hide_render)
    def test_empty_scene_cleanup(self):
        s=bpy.data.scenes.new('EmptyAudit');clear_generated(s);self.assertEqual(len(s.objects),0)

suite=unittest.defaultTestLoader.loadTestsFromTestCase(OwnershipTests)
r=unittest.TextTestRunner(verbosity=2).run(suite)
if not r.wasSuccessful():raise RuntimeError('Regeneration adversarial gate failed')
