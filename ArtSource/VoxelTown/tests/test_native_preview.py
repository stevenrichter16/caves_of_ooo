"""Fallible preview inputs are rejected before clearing any Blender content."""
import copy
import unittest
from town_generator.native_assets import asset_manifest
from town_generator.native_region import generate_region
from town_generator.native_scene import validate_preview_inputs


class NativePreviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.assets=asset_manifest();cls.chunk=generate_region()['chunks'][0]

    def test_generated_preview_inputs_validate_without_blender(self):
        self.assertTrue(validate_preview_inputs(self.chunk,self.assets))

    def test_unknown_visual_is_rejected_in_preflight(self):
        bad=copy.deepcopy(self.chunk);bad['placements'][0]['asset']='missing-model'
        with self.assertRaises(ValueError):validate_preview_inputs(bad,self.assets)

    def test_incompatible_palette_is_rejected_instead_of_silently_recolored(self):
        bad=copy.deepcopy(self.assets);bad['palette'][0]['hex']='FF00FF'
        with self.assertRaises(ValueError):validate_preview_inputs(self.chunk,bad)

    def test_bad_face_index_or_material_cannot_reach_blender_mesh_creation(self):
        for key in ('quads','quadMaterials'):
            bad=copy.deepcopy(self.assets)
            if key=='quads':bad['assets'][0][key][0][0]=999999
            else:bad['assets'][0][key][0]=999999
            with self.assertRaises(ValueError):validate_preview_inputs(self.chunk,bad)

    def test_bad_terrain_and_placement_are_rejected_before_scene_mutation(self):
        bad=copy.deepcopy(self.chunk);bad['terrain'][0]['material']='not-a-material'
        with self.assertRaises(ValueError):validate_preview_inputs(bad,self.assets)
        bad=copy.deepcopy(self.chunk);bad['placements'][0]['uniformScale']=float('nan')
        with self.assertRaises(ValueError):validate_preview_inputs(bad,self.assets)

    def test_nan_vertex_cannot_poison_scene_geometry(self):
        bad=copy.deepcopy(self.assets);bad['assets'][0]['vertices'][0][0]=float('nan')
        with self.assertRaises(ValueError):validate_preview_inputs(self.chunk,bad)


if __name__=='__main__':unittest.main()
