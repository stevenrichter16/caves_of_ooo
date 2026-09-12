"""Actual lower-density native recipes: geometry, silhouettes and isolation."""
import hashlib
import json
import math
import random
import tempfile
import unittest
from pathlib import Path

from town_generator.native_assets import asset_manifest, export_assets, native_recipes
from town_generator.materials import MATERIAL_INDEX
from town_generator.native_coarse import _resample
from town_generator.mesh import VoxelMesh

KEYS = ('crate','crate_1','crate_2','crate_3','barrel','barrel_1','barrel_2',
        'barrel_3','bed','stool','rock','rock_1','rock_2','scrub','scrub_1',
        'scrub_2','tree','tree_1','tree_2')


def occupied_bounds(mesh):
    return tuple(min(p[a] for p in mesh.cells) for a in range(3)), tuple(max(p[a] for p in mesh.cells) for a in range(3))


class NativeCoarseTests(unittest.TestCase):
    def test_coarse_density_is_actually_lower_not_a_scale_change(self):
        fine=native_recipes();coarse=native_recipes(profile='native_coarse')
        old=sum(fine[k].cell_count for k in KEYS);new=sum(coarse[k].cell_count for k in KEYS)
        self.assertGreaterEqual(1-new/old,.4)
        self.assertLessEqual(1-new/old,.65)
        self.assertLess(sum(len(coarse[k].geometry()[1]) for k in KEYS),
                        sum(len(fine[k].geometry()[1]) for k in KEYS))

    def test_fine_export_bytes_stay_pinned(self):
        data=asset_manifest();encoded=(json.dumps(data,sort_keys=True,separators=(',',':'))+'\n').encode()
        self.assertEqual('a7f350b3f54e4402e8500104aaa0f44732b79456515ea0077ab2a426e08ec7b2',hashlib.sha256(encoded).hexdigest())
        native_recipes(profile='native_coarse')
        self.assertEqual(data,asset_manifest())

    def test_palette_ids_and_import_only_pitch_policy(self):
        fine=asset_manifest();coarse=asset_manifest(profile='native_coarse')
        self.assertEqual(fine['palette'],coarse['palette'])
        self.assertEqual({a['id'] for a in fine['assets']},{a['id'] for a in coarse['assets']})
        self.assertEqual('native_coarse',coarse['profile'])
        self.assertEqual('per-asset',coarse['voxelSizePolicy'])
        self.assertIsNone(coarse['voxelSize'])

    def test_open_container_cavities_remain_open_from_above(self):
        meshes=native_recipes(profile='native_coarse')
        for key in ('crate_1','barrel_2'):
            mesh=meshes[key];lo,hi=occupied_bounds(mesh)
            x=(lo[0]+hi[0])//2;y=(lo[1]+hi[1])//2
            self.assertIn((x,y,lo[2]),mesh.cells)
            self.assertTrue(all((x,y,z) not in mesh.cells for z in range(lo[2]+1,hi[2]+1)),key)
        closed=meshes['crate'];lo,hi=occupied_bounds(closed)
        self.assertIn(((lo[0]+hi[0])//2,(lo[1]+hi[1])//2,hi[2]),closed.cells)

    def test_bed_keeps_four_feet_fabric_and_tall_headboard(self):
        mesh=native_recipes(profile='native_coarse')['bed'];lo,hi=occupied_bounds(mesh)
        feet={(p[0],p[1]) for p in mesh.cells if p[2]==lo[2]}
        self.assertGreaterEqual(len(feet),4)
        # A filled plinth also has >=4 ground cells. Pin the actual air under
        # the mattress and the gap between its two front supports instead.
        centre_x=(lo[0]+hi[0])//2
        self.assertNotIn((centre_x,lo[1],lo[2]),mesh.cells)
        self.assertNotIn((centre_x,(lo[1]+hi[1])//2,lo[2]),mesh.cells)
        self.assertIn(MATERIAL_INDEX['cloth_red'],mesh.cells.values())
        self.assertIn(MATERIAL_INDEX['cloth_cream'],mesh.cells.values())
        top=[p for p in mesh.cells if p[2]==hi[2]]
        self.assertTrue(all(p[1]==hi[1] for p in top),'highest row remains headboard')

    def test_minimal_stool_keeps_three_separate_legs(self):
        fine=native_recipes()['stool'];coarse=native_recipes(profile='native_coarse')['stool']
        self.assertEqual(fine.cells,coarse.cells);self.assertEqual(.25,coarse.voxel_size)
        bottom={p[:2] for p in coarse.cells if p[2]==0}
        self.assertEqual(3,len(bottom))
        self.assertTrue(all(abs(a[0]-b[0])+abs(a[1]-b[1])>1 for a in bottom for b in bottom if a!=b))

    def test_geometry_variants_do_not_collapse(self):
        meshes=native_recipes(profile='native_coarse')
        for base,count in (('crate',4),('barrel',4),('tree',3),('rock',3),('scrub',3)):
            shapes=[frozenset(meshes[base+('_'+str(i) if i else '')].cells) for i in range(count)]
            self.assertEqual(count,len(set(shapes)),base)

    def test_export_is_explicit_deterministic_and_preserves_fine_file(self):
        with tempfile.TemporaryDirectory() as directory:
            fine=Path(directory)/'assets.json';coarse=Path(directory)/'assets-coarse.json'
            export_assets(fine);old=fine.read_bytes();state=random.getstate()
            data=export_assets(coarse,profile='native_coarse');first=coarse.read_bytes()
            self.assertEqual(data,json.loads(first));self.assertEqual(state,random.getstate())
            export_assets(coarse,profile='native_coarse');self.assertEqual(first,coarse.read_bytes())
            self.assertEqual(old,fine.read_bytes())

    def test_invalid_profile_rejected_before_existing_output_changes(self):
        with tempfile.TemporaryDirectory() as directory:
            target=Path(directory)/'keep.json';target.write_text('keep')
            for profile in ('coarse_typo','',None,False,2):
                with self.subTest(profile=profile):
                    with self.assertRaises(ValueError):export_assets(target,profile=profile)
                    self.assertEqual('keep',target.read_text())

    def test_unbound_recipes_are_unchanged(self):
        fine=native_recipes();coarse=native_recipes(profile='native_coarse')
        for key in set(fine)-set(KEYS):
            self.assertEqual(fine[key].cells,coarse[key].cells,key)
            self.assertEqual(fine[key].voxel_size,coarse[key].voxel_size,key)

    def test_returned_recipes_are_independent(self):
        first=native_recipes(profile='native_coarse');second=native_recipes(profile='native_coarse')
        first['crate'].cells.clear()
        self.assertGreater(second['crate'].cell_count,0)
        self.assertEqual(second['crate'].cells,native_recipes(profile='native_coarse')['crate'].cells)

    def test_resampling_is_translation_independent_for_negative_coordinates(self):
        source=native_recipes()['tree'];shifted=VoxelMesh(.25)
        shifted.cells={(x-101,y+77,z-23):material for (x,y,z),material in source.cells.items()}
        self.assertEqual(_resample(source,.375).cells,_resample(shifted,.375).cells)

    def test_material_ties_and_insertion_order_are_deterministic(self):
        a=VoxelMesh(.25);a.cells={(0,0,0):4,(1,0,0):3}
        b=VoxelMesh(.25);b.cells=dict(reversed(list(a.cells.items())))
        self.assertEqual({(0,0,0):3},_resample(a,.5).cells)
        self.assertEqual(_resample(a,.5).cells,_resample(b,.5).cells)

    def test_coarse_recipe_is_not_the_original_recipe_enlarged(self):
        from town_generator.assets import build_recipes
        fine=native_recipes();enlarged=build_recipes(.375);coarse=native_recipes('native_coarse')
        self.assertEqual(fine['tree'].cells,enlarged['tree'].cells)
        self.assertNotEqual(fine['tree'].cells,coarse['tree'].cells)

    def test_coarse_profile_is_rejected_by_fine_candidate_preview(self):
        from town_generator.native_scene import validate_preview_inputs
        from town_generator.native_region import generate_region
        region=generate_region();chunk=region['chunks'][0]
        validate_preview_inputs(chunk,asset_manifest())
        with self.assertRaises(ValueError):validate_preview_inputs(chunk,asset_manifest('native_coarse'))

    def test_trees_keep_wood_below_an_overhanging_leaf_canopy(self):
        meshes=native_recipes('native_coarse')
        for key in ('tree','tree_1','tree_2'):
            mesh=meshes[key]
            wood=[p for p,m in mesh.cells.items() if m==MATERIAL_INDEX['wood']]
            leaf=[p for p,m in mesh.cells.items() if m in (MATERIAL_INDEX['leaf'],MATERIAL_INDEX['leaf_light'])]
            self.assertTrue(wood);self.assertTrue(leaf)
            self.assertEqual(0,min(p[2] for p in wood))
            self.assertGreater(max(p[2] for p in leaf),max(p[2] for p in wood))
            self.assertGreater(max(p[0] for p in leaf)-min(p[0] for p in leaf),
                               max(p[0] for p in wood)-min(p[0] for p in wood))


def per_recipe_gate(key):
    def gate(self):
        fine=native_recipes()[key];coarse=native_recipes(profile='native_coarse')[key]
        self.assertGreater(coarse.cell_count,0)
        self.assertLessEqual(coarse.cell_count,fine.cell_count)
        if key!='stool':
            self.assertLess(coarse.cell_count,fine.cell_count,key)
            self.assertGreater(coarse.voxel_size,fine.voxel_size,key)
        rows={a['sourceAsset']:a for a in asset_manifest(profile='native_coarse')['assets']};row=rows[key]
        self.assertEqual(coarse.cell_count,sum(run[3] for run in row['runs']))
        self.assertEqual(coarse.voxel_size,row['voxelSize'])
        self.assertEqual([row['singleCellScale']]*3,row['singleCellUniformScale'])
        self.assertAlmostEqual(row['voxelSize']*row['singleCellScale'],row['singleCellEffectiveVoxelSize'])
        lo,hi=row['bounds']['minimum'],row['bounds']['maximum']
        self.assertEqual(0,lo[2]);self.assertAlmostEqual(lo[0]+hi[0],0);self.assertAlmostEqual(lo[1]+hi[1],0)
        for v in row['vertices']:
            self.assertTrue(all(math.isfinite(n) for n in v))
            self.assertTrue(all(lo[a]-1e-8<=v[a]<=hi[a]+1e-8 for a in range(3)))
        for run in row['runs']:
            self.assertGreater(run[3],0)
            for axis in range(3):
                start=run[axis]*row['voxelSize']+row['voxelOrigin'][axis]
                end=start+(run[3] if axis==0 else 1)*row['voxelSize']
                self.assertGreaterEqual(start,lo[axis]-1e-8);self.assertLessEqual(end,hi[axis]+1e-8)
        fine_vertices=fine.geometry()[0]
        fine_size=[max(p[a] for p in fine_vertices)-min(p[a] for p in fine_vertices) for a in range(3)]
        # Model the actual native import: one scale fits all three dimensions.
        # A pitch-only enlarged copy would have exactly the old effective pitch.
        native_owner_size=[s*.6 for s in fine_size]
        scale=min(native_owner_size[a]/row['bounds']['size'][a] for a in range(3))
        self.assertTrue(all(row['bounds']['size'][a]*scale<=native_owner_size[a]+1e-8 for a in range(3)))
        if key!='stool':self.assertGreater(row['voxelSize']*scale,fine.voxel_size*.6)
    return gate


for _key in KEYS:setattr(NativeCoarseTests,'test_recipe_'+_key,per_recipe_gate(_key))

if __name__=='__main__':unittest.main()
