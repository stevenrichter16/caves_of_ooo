"""P1: exported color cap without any geometry or construction-state change."""
import hashlib
import copy
import json
import random
import tempfile
import unittest
from pathlib import Path

from town_generator.native_assets import asset_manifest,export_assets,native_recipes
from town_generator.materials import MATERIAL_INDEX,PALETTE_NAMES
from town_generator.native_palette import cap_export_materials

KEYS=('awning','banner','barrel','barrel_1','barrel_2','barrel_3','bed','bridge',
      'crate','crate_1','crate_2','crate_3','crop','crop_1','crop_2','door','fence',
      'firepit','floor','furnace','gate','hay','irrigation','lantern','livestock',
      'livestock_1','livestock_2','market_stall','npc','npc_1','npc_2','palm',
      'palm_1','palm_2','pot','reeds','reeds_1','reeds_2','rock','rock_1','rock_2',
      'ruin','scrub','scrub_1','scrub_2','shelf','sign','stairs','stool','table',
      'torch','tree','tree_1','tree_2','trough','wall','well','window','workbench')


def digest(value):
    return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()


def construction_digest():
    return digest({k:[(list(p),v) for p,v in sorted(m.cells.items())]
                   for k,m in native_recipes('native_coarse').items()})


def used(row):
    return set(row['quadMaterials'])|set(row['triangleMaterials'])|{run[4] for run in row['runs']}


class NativePaletteTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows={a['sourceAsset']:a for a in asset_manifest('native_coarse')['assets']}
        cls.raw=native_recipes('native_coarse')

    def test_geometry_topology_runs_bounds_and_fitting_are_bit_exact(self):
        rows=[]
        for a in asset_manifest('native_coarse')['assets']:
            row={k:v for k,v in a.items() if k not in ('quadMaterials','triangleMaterials','runs')}
            row['runs']=[r[:4] for r in a['runs']];rows.append(row)
        self.assertEqual('997396e225abb8c3e02be18adf46abeba76db0ec84307b7515938a4b3194904f',digest(rows))

    def test_construction_materials_stay_identical_after_repeated_exports(self):
        before=construction_digest()
        self.assertEqual('3887aabbd20439f77189517646f6851b9391faf65b82f943d1f453e127f9d403',before)
        asset_manifest('native_coarse');asset_manifest('native_coarse')
        self.assertEqual(before,construction_digest())

    def test_fine_profile_and_palette_are_byte_identical(self):
        fine=asset_manifest();encoded=(json.dumps(fine,sort_keys=True,separators=(',',':'))+'\n').encode()
        self.assertEqual('a7f350b3f54e4402e8500104aaa0f44732b79456515ea0077ab2a426e08ec7b2',hashlib.sha256(encoded).hexdigest())
        self.assertEqual(fine['palette'],asset_manifest('native_coarse')['palette'])
        self.assertGreater(len(set(next(a for a in fine['assets'] if a['sourceAsset']=='bed')['quadMaterials'])),2)

    def test_tree_and_bed_keep_semantic_contrast(self):
        for key in ('tree','tree_1','tree_2','palm','palm_1','palm_2'):
            self.assertEqual({MATERIAL_INDEX['wood'],MATERIAL_INDEX['leaf']},used(self.rows[key]),key)
        self.assertEqual({MATERIAL_INDEX['wood'],MATERIAL_INDEX['cloth_red']},used(self.rows['bed']))

    def test_metal_only_on_containers_that_already_had_metal(self):
        metal=MATERIAL_INDEX['metal']
        for key in ('crate','crate_1','crate_2','crate_3','barrel','barrel_1','barrel_2','barrel_3'):
            source=set(self.raw[key].cells.values());colors=used(self.rows[key])
            self.assertEqual(metal in source,metal in colors,key)
            self.assertTrue(colors<=source,key+' invents a material')

    def test_fire_water_and_npc_accents_survive(self):
        for key in ('firepit','furnace','torch','lantern'):
            self.assertIn(MATERIAL_INDEX['ember'],used(self.rows[key]))
        for key in ('well','irrigation','trough'):
            self.assertIn(MATERIAL_INDEX['water'],used(self.rows[key]))
        for key,skin in (('npc','skin'),('npc_1','skin_dark'),('npc_2','skin')):
            self.assertIn(MATERIAL_INDEX[skin],used(self.rows[key]))
            self.assertTrue(any(PALETTE_NAMES[m].startswith('cloth_') for m in used(self.rows[key])))
        self.assertIn(MATERIAL_INDEX['dry_leaf'],used(self.rows['scrub_2']))

    def test_repeated_export_is_deterministic_and_does_not_consume_rng(self):
        state=random.getstate();first=asset_manifest('native_coarse')
        self.assertEqual(state,random.getstate());self.assertEqual(first,asset_manifest('native_coarse'))
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'assets-coarse.json';export_assets(path,'native_coarse');old=path.read_bytes()
            export_assets(path,'native_coarse');self.assertEqual(old,path.read_bytes())

    def test_new_high_color_family_fails_before_mutation_but_two_colors_pass(self):
        row={'sourceAsset':'future_asset','quadMaterials':[3,4,5],
             'triangleMaterials':[3,3,4,4,5,5],
             'runs':[[0,0,0,1,3],[1,0,0,1,4],[2,0,0,1,5]]}
        before=copy.deepcopy(row)
        with self.assertRaises(ValueError):cap_export_materials(row)
        self.assertEqual(before,row)
        row['quadMaterials']=[3,4];row['triangleMaterials']=[3,3,4,4];row['runs'].pop()
        before=copy.deepcopy(row);self.assertEqual(before,cap_export_materials(row))

    def test_missing_semantic_colors_fail_before_mutation(self):
        row={'sourceAsset':'bed','quadMaterials':[8,9,10],
             'triangleMaterials':[8,8,9,9,10,10],
             'runs':[[0,0,0,1,8],[1,0,0,1,9],[2,0,0,1,10]]}
        before=copy.deepcopy(row)
        with self.assertRaises(ValueError):cap_export_materials(row)
        self.assertEqual(before,row)


def recipe_gate(key):
    def gate(self):
        row=self.rows[key];source=self.raw[key];colors=used(row)
        self.assertGreater(len(colors),0);self.assertLessEqual(len(colors),2,key)
        self.assertTrue(colors<=set(source.cells.values()),key+' adds a new color')
        self.assertEqual([m for m in row['quadMaterials'] for _ in range(2)],row['triangleMaterials'])
        # One material map must account for both original source cells and faces.
        mapping={}
        for run in row['runs']:
            x,y,z,length,m=run
            for xx in range(x,x+length):
                old=source.cells[(xx,y,z)]
                self.assertEqual(mapping.setdefault(old,m),m)
        original_faces=source.geometry()[2]
        self.assertEqual([mapping[m] for m in original_faces],row['quadMaterials'])
        if len(set(source.cells.values()))<=2:
            self.assertEqual({m:m for m in set(source.cells.values())},mapping)
            self.assertEqual(original_faces,row['quadMaterials'])
    return gate


for _key in KEYS:setattr(NativePaletteTests,'test_recipe_'+_key,recipe_gate(_key))

if __name__=='__main__':unittest.main()
