"""Neutral engine export gates: actual geometry, ownership size and units."""
import copy
import json
import math
import random
import tempfile
import unittest
from pathlib import Path

from town_generator.assets import build_recipes
from town_generator.native_assets import asset_manifest, export_assets, native_recipes


class NativeAssetTests(unittest.TestCase):
    def test_every_recipe_survives_as_real_geometry_and_voxels(self):
        source=build_recipes(.25);data=asset_manifest()
        self.assertEqual(set(source),{a['sourceAsset'] for a in data['assets']})
        for a in data['assets']:
            self.assertGreater(len(a['vertices']),3)
            self.assertEqual(len(a['quads']),len(a['quadMaterials']))
            self.assertEqual(len(a['triangles']),len(a['triangleMaterials']))
            self.assertEqual(len(a['triangles']),len(a['quads'])*2)
            self.assertEqual(sum(r[3] for r in a['runs']),source[a['sourceAsset']].cell_count)

    def test_axes_scale_and_palette_are_explicit(self):
        data=asset_manifest()
        self.assertEqual(.25,data['voxelSize'])
        self.assertEqual(1,data['cellSize'])
        self.assertEqual('Blender: X east, Y north, Z up',data['axes'])
        self.assertEqual('centred horizontal bounds; floor at Z=0',data['pivot'])
        self.assertTrue(all(len(row['hex'])==6 for row in data['palette']))
        for a in data['assets']:
            lo,hi=a['bounds']['minimum'],a['bounds']['maximum']
            self.assertAlmostEqual(lo[0]+hi[0],0)
            self.assertAlmostEqual(lo[1]+hi[1],0)
            self.assertEqual(0,lo[2])
            for v in a['vertices']:
                self.assertTrue(all(math.isfinite(n) for n in v))

    def test_uniform_fit_is_honest_about_large_objects(self):
        assets={a['sourceAsset']:a for a in asset_manifest()['assets']}
        self.assertTrue(assets['crate']['fitsOneCellAtNativeScale'])
        self.assertFalse(assets['well']['fitsOneCellAtNativeScale'])
        self.assertFalse(assets['bed']['fitsOneCellAtNativeScale'])
        self.assertEqual(1,assets['crate']['singleCellScale'])
        self.assertAlmostEqual(1/3,assets['well']['singleCellScale'])
        self.assertEqual(.5,assets['bed']['singleCellScale'])
        for a in assets.values():
            scale=a['singleCellScale'];size=a['bounds']['size']
            self.assertTrue(0<scale<=1)
            self.assertLessEqual(max(size[:2])*scale,1+1e-8)
            self.assertEqual([scale]*3,a['singleCellUniformScale'])
            self.assertGreaterEqual(a['nativeFootprintBounds']['width'],1)
            self.assertGreaterEqual(a['nativeFootprintBounds']['depth'],1)

    def test_export_is_json_roundtrippable_and_preserves_other_files(self):
        with tempfile.TemporaryDirectory() as folder:
            target=Path(folder)/'assets.json';manual=Path(folder)/'manual.txt';manual.write_text('keep')
            export_assets(target)
            data=json.loads(target.read_text());self.assertEqual(asset_manifest(),data)
            before=target.read_bytes();export_assets(target)
            self.assertEqual(before,target.read_bytes());self.assertEqual('keep',manual.read_text())

    def test_export_does_not_consume_global_rng_or_share_mutable_results(self):
        state=random.getstate();first=asset_manifest();second=asset_manifest()
        self.assertEqual(state,random.getstate());self.assertEqual(first,second)
        first['assets'][0]['vertices'].clear()
        self.assertNotEqual(first,second);self.assertEqual(second,asset_manifest())

    def test_mesh_material_indices_and_faces_are_valid(self):
        data=asset_manifest()
        for a in data['assets']:
            count=len(a['vertices'])
            for face in a['quads']+a['triangles']:
                self.assertTrue(all(0<=i<count for i in face))
                self.assertEqual(len(face),len(set(face)))
            self.assertTrue(all(0<=m<len(data['palette']) for m in a['quadMaterials']))

    def test_recipe_conversion_preserves_source_and_surface_dimensions(self):
        source=build_recipes(.25);before=copy.deepcopy(source['stool'].cells)
        recipes=native_recipes()
        self.assertEqual(before,source['stool'].cells)
        self.assertEqual(set(source),set(recipes))
        for name,mesh in recipes.items():
            self.assertEqual(source[name].cell_count,mesh.cell_count)

    def test_crates_and_barrels_have_four_geometry_variants_in_one_cell(self):
        recipes=native_recipes();rows={a['sourceAsset']:a for a in asset_manifest()['assets']}
        for family in ('crate','barrel'):
            keys=[family]+[family+'_'+str(i) for i in range(1,4)]
            shapes=[]
            for key in keys:
                self.assertIn(key,recipes)
                shapes.append(frozenset(recipes[key].cells))
                self.assertTrue(rows[key]['fitsOneCellAtNativeScale'])
                self.assertEqual(1,rows[key]['singleCellScale'])
            self.assertEqual(4,len(set(shapes)),family+' needs changed geometry, not only color')


if __name__=='__main__':unittest.main()
