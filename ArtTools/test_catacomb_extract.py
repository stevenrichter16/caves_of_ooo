"""Pixel-fidelity and malformed-input checks for the illustrated-sheet extractor."""
import unittest
import numpy as np
from PIL import Image
try:
    from ArtTools.catacomb_extract import extract_sprite, validate_authoring
except ModuleNotFoundError:
    from catacomb_extract import extract_sprite, validate_authoring


class ExtractionTests(unittest.TestCase):
    def setUp(self):
        self.a=np.zeros((12,12,4),dtype=np.uint8)
        self.a[:,:,:3]=[77,88,99]
        self.a[3:9,4:8,3]=250
        self.image=Image.fromarray(self.a)
        self.entry={'id':'fixture','kind':'prop','bounds':[1,1,10,10],'foot':[6,9]}

    def test_visible_rgb_is_exact_source_and_alpha_is_binary(self):
        sprite,meta=extract_sprite(self.image,self.entry)
        got=np.asarray(sprite)
        self.assertEqual(set(np.unique(got[:,:,3])),{0,255})
        self.assertTrue(np.all(got[:,:,:3][got[:,:,3]>0]==[77,88,99]))
        self.assertEqual(np.count_nonzero(got[:,:,3]),24)

    def test_low_alpha_sheet_haze_does_not_become_sprite(self):
        self.a[2:10,2:10,3]=12;self.a[3:9,4:8,3]=250
        sprite,_=extract_sprite(Image.fromarray(self.a),self.entry)
        self.assertEqual(np.count_nonzero(np.asarray(sprite)[:,:,3]),24)

    def test_semitransparent_cutline_has_documented_threshold(self):
        self.a[3,4,3]=127;self.a[3,5,3]=128
        sprite,_=extract_sprite(Image.fromarray(self.a),self.entry)
        self.assertEqual(np.count_nonzero(np.asarray(sprite)[:,:,3]),23)

    def test_internal_hole_remains_transparent(self):
        self.a[5,5,3]=0
        sprite,meta=extract_sprite(Image.fromarray(self.a),self.entry)
        x,y,_,_=meta['sourceBounds']
        self.assertEqual(np.asarray(sprite)[5-y,5-x,3],0)

    def test_crop_stays_native_scale_and_foot_is_not_clamped(self):
        sprite,meta=extract_sprite(self.image,self.entry)
        x,y,w,h=meta['sourceBounds']
        self.assertEqual(sprite.size,(w,h))
        self.assertEqual(meta['foot'],[6-x,9-y])
        self.assertEqual(meta['pivot'],[(6-x)/w,1-(9-y)/h])
        self.assertGreaterEqual(meta['foot'][1],0)
        self.assertLessEqual(meta['foot'][1],h)

    def test_neighbor_outside_roi_is_not_included(self):
        self.a[0,:,3]=250
        sprite,_=extract_sprite(Image.fromarray(self.a),self.entry)
        self.assertEqual(np.count_nonzero(np.asarray(sprite)[:,:,3]),24)

    def test_optional_exclusion_removes_island_without_rgb_changes(self):
        e=dict(self.entry,excludePolygons=[[[4,3],[5,3],[5,4],[4,4]]])
        sprite,_=extract_sprite(self.image,e)
        self.assertEqual(np.count_nonzero(np.asarray(sprite)[:,:,3]),20)

    def test_rejects_empty_alpha(self):
        with self.assertRaises(ValueError):extract_sprite(Image.new('RGBA',(12,12)),self.entry)

    def test_rejects_rgb_source_with_no_authoritative_alpha(self):
        with self.assertRaises(ValueError):extract_sprite(self.image.convert('RGB'),self.entry)

    def test_rejects_invalid_bounds(self):
        for bounds in [[-1,0,2,2],[0,0,99,2],[0,0,0,2],[0.5,0,2,2],[True,0,2,2]]:
            with self.subTest(bounds=bounds),self.assertRaises(ValueError):
                extract_sprite(self.image,dict(self.entry,bounds=bounds))

    def test_rejects_duplicate_and_path_ids(self):
        for entries in [[self.entry,self.entry],[dict(self.entry,id='../escape')]]:
            with self.assertRaises(ValueError):validate_authoring({'assets':entries},[12,12])

    def test_rejects_nonfinite_foot(self):
        with self.assertRaises(ValueError):extract_sprite(self.image,dict(self.entry,foot=[float('nan'),2]))

    def test_rejects_crossed_and_degenerate_exclusion_polygons(self):
        for poly in [[[2,2],[8,8],[2,8],[8,2]],[[2,2],[5,5],[8,8]]]:
            with self.subTest(poly=poly),self.assertRaises(ValueError):
                extract_sprite(self.image,dict(self.entry,excludePolygons=[poly]))

    def test_rejects_mismatched_declared_canvas(self):
        with self.assertRaises(ValueError):
            validate_authoring({'canvas':[99,99],'assets':[self.entry]},[12,12])

    def test_shipped_assets_have_unique_source_pixel_ownership(self):
        from pathlib import Path
        import json
        root=Path(__file__).resolve().parents[1]
        authoring=json.loads((root/'ArtSource/CatacombVillage/source-authoring.json').read_text())
        source=Image.open(root/'Docs/StyleExploration/Felling-2026-09-05/05-catacomb-tileset.png')
        ownership=np.zeros((source.height,source.width),dtype=bool)
        for entry in authoring['assets']:
            sprite,meta=extract_sprite(source,entry)
            x,y,w,h=meta['sourceBounds'];mask=np.asarray(sprite)[:,:,3]>0
            region=ownership[y:y+h,x:x+w]
            self.assertFalse(np.any(region & mask),entry['id']+' includes another asset')
            region |= mask

    def test_source_buffer_not_mutated(self):
        before=np.asarray(self.image).copy();extract_sprite(self.image,self.entry)
        np.testing.assert_array_equal(before,np.asarray(self.image))

if __name__=='__main__':unittest.main()
