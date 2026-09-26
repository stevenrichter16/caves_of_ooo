"""C13 original creature bindings. Run with python3 -m unittest (no Blender)."""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
RING = ROOT / 'ArtSource/SpawnRing3D'
EXPECTED = {
    'MarlbackScrabbler': 'ring-snapjaw',
    'MarlbackGleaner': 'ring-marlback-gleaner',
    'MarlbackTunnelguard': 'ring-marlback-tunnelguard',
    'MarlbackWallkeeper': 'ring-marlback-wallkeeper',
    'MarlbackBreacher': 'ring-snapjaw-warlord',
    'GroveLanternMoth': 'ring-grove-lantern-moth',
}
LEGACY = {'Snapjaw', 'SnapjawScavenger', 'SnapjawHunter', 'SnapjawChieftain', 'SnapjawWarlord', 'GlowMoth'}

class OriginalCreatureArtTests(unittest.TestCase):
    def test_all_six_current_creatures_have_distinct_source_bindings(self):
        catalog = json.loads((RING / 'catalog-contract.json').read_text())
        bindings = {b['blueprint']: b for b in catalog['blueprints']}
        for blueprint, model in EXPECTED.items():
            with self.subTest(blueprint=blueprint):
                self.assertIn(blueprint, bindings)
                self.assertEqual([model], bindings[blueprint]['models'])
        self.assertEqual(6, len(set(EXPECTED.values())))

    def test_live_catalog_does_not_bind_retired_spawnable_blueprints(self):
        for path in [RING/'catalog-contract.json', RING/'catalog.json', ROOT/'Assets/Art3D/SpawnRing/Definitions/catalog.json']:
            with self.subTest(path=str(path)):
                data=json.loads(path.read_text())
                self.assertFalse(LEGACY & {b['blueprint'] for b in data['blueprints']})

    def test_marlback_recipe_is_original_anatomy_not_old_canine_head(self):
        source=(RING/'build_ring.py').read_text()
        self.assertIn('def marlback(',source)
        self.assertNotIn('Long_snapjaw_muzzle',source)
        self.assertNotIn('Scaled_canine_head',source)
        for feature in ['Marlback_blunt_face','Marlback_shale_plate','Marlback_digging_rake']:
            self.assertIn(feature,source)

    def test_moth_recipe_is_winged_without_emissive_gameplay_claim(self):
        source=(RING/'build_ring.py').read_text()
        self.assertIn('def lantern_moth(',source)
        self.assertIn('Lantern_moth_wing_band',source)

    def test_live_builder_maps_immutable_captured_legacy_identities(self):
        source=(RING/'build_ring.py').read_text()
        self.assertIn('CAPTURED_BLUEPRINT_ALIASES',source)
        for name in LEGACY:
            self.assertIn(repr(name),source)

    def test_sprite_generator_has_original_low_body_and_wing_recipes(self):
        source=(ROOT/'ArtTools/coo_vertical_slice.py').read_text()
        self.assertIn('def marlback_frame(',source)
        self.assertIn('def lantern_moth_frame(',source)
        self.assertNotIn('def snapjaw_frame(',source)

    def test_native_importers_offer_bounded_subset_publication(self):
        ring=(ROOT/'Assets/Editor/Art/SpawnRing3DAssetBuilder.cs').read_text()
        voxel=(ROOT/'Assets/Editor/Art/VoxelWorldMeshBuilder.cs').read_text()
        self.assertIn('string[] modelIds=null',ring)
        self.assertIn('string[] sourceAssetPaths=null',voxel)

    def test_scoped_body_model_files_exist_in_adopted_source(self):
        for model in EXPECTED.values():
            with self.subTest(model=model):
                self.assertTrue((RING/'models'/(model+'.fbx')).is_file())

if __name__=='__main__': unittest.main()
