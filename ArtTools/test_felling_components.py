"""Pixel ownership and reveal invariants; fixtures are synthetic, not reference artwork."""
import unittest
import numpy as np
from ArtTools import felling_components as fc


class ComponentPixelsTests(unittest.TestCase):
    def setUp(self):
        self.source = np.full((24, 32, 3), (38, 49, 27), dtype=np.uint8)
        self.backing = self.source.copy()
        self.a = np.zeros((24, 32), bool); self.a[7:12, 8:12] = True
        self.b = np.zeros((24, 32), bool); self.b[8:13, 16:20] = True
        self.source[self.a] = (110, 33, 39)
        self.source[self.b] = (40, 115, 92)

    def test_binary_sprite_copies_known_pixels_and_clears_background(self):
        rgba = fc.rgba_cutout(self.source, self.a)
        np.testing.assert_array_equal(rgba[self.a, :3], self.source[self.a])
        self.assertTrue(np.all(rgba[self.a, 3] == 255))
        self.assertTrue(np.all(rgba[~self.a] == 0))

    def test_polygon_does_not_fill_its_bounding_rectangle(self):
        m = fc.polygon_mask((32, 24), [[3, 3], [13, 3], [3, 13]])
        self.assertTrue(m[4, 4]); self.assertFalse(m[12, 12])

    def test_self_intersecting_polygon_is_rejected(self):
        with self.assertRaises(ValueError):
            fc.polygon_mask((32,24),[[2,2],[12,12],[2,12],[12,2]])

    def test_red_segmentation_keeps_plant_and_excludes_green_ground(self):
        entry = dict(bounds=[4, 4, 12, 12], candidatePolygon=[[4,4],[15,4],[15,15],[4,15]], maskMethod='red')
        mask = fc.segment(self.source, entry)
        self.assertTrue(np.all(mask[self.a]))
        self.assertFalse(mask[4,4])

    def test_cyan_segmentation_does_not_select_red_neighbor(self):
        entry = dict(bounds=[4,4,20,14],candidatePolygon=[[4,4],[23,4],[23,17],[4,17]],maskMethod='cyan')
        mask = fc.segment(self.source, entry)
        self.assertTrue(np.all(mask[self.b])); self.assertFalse(np.any(mask[self.a]))

    def test_red_mask_excludes_pale_petrified_stone(self):
        rgb=self.source.copy();rgb[6:10,12:15]=(114,86,79)
        entry=dict(bounds=[4,4,14,14],candidatePolygon=[[4,4],[17,4],[17,17],[4,17]],maskMethod='red')
        mask=fc.segment(rgb,entry)
        self.assertTrue(mask[8,9]);self.assertFalse(mask[7,14])

    def test_red_mask_retains_dim_brown_connecting_stem(self):
        rgb=self.source.copy();rgb[12:16,9:11]=(65,51,38)
        entry=dict(bounds=[4,4,14,16],candidatePolygon=[[4,4],[17,4],[17,19],[4,19]],maskMethod='red')
        mask=fc.segment(rgb,entry)
        self.assertTrue(mask[14,9], 'visible brown stem must survive saturation filtering')

    def test_excluded_stone_is_not_claimed_as_sprite_or_repair(self):
        entry=dict(bounds=[4,4,14,14],candidatePolygon=[[4,4],[17,4],[17,17],[4,17]],maskMethod='red',excludePolygons=[[[10,4],[17,4],[17,17],[10,17]]])
        m=fc.segment(self.source,entry)
        self.assertTrue(m[8,8]);self.assertFalse(m[8,11])
        barrier=fc.polygon_mask((32,24),entry['excludePolygons'][0])
        base,layers=fc.make_mutable_layers(self.source,self.backing,{'a':m},barriers={'a':barrier})
        self.assertFalse(np.any(layers['a']['support']&barrier))

    def test_intact_layers_reassemble_exact_source(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a, 'b':self.b})
        np.testing.assert_array_equal(fc.compose(base, layers), self.source)

    def test_removal_exposes_backing_instead_of_baked_object(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a, 'b':self.b})
        removed = fc.compose(base, layers, {'a'})
        np.testing.assert_array_equal(removed[self.a], self.backing[self.a])
        np.testing.assert_array_equal(removed[self.b], self.source[self.b])

    def test_removal_cannot_change_unrelated_pixels(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a, 'b':self.b})
        removed = fc.compose(base, layers, {'a'})
        np.testing.assert_array_equal(removed[~layers['a']['support']], self.source[~layers['a']['support']])

    def test_overlapping_contact_support_has_one_owner(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a, 'b':self.b})
        self.assertFalse(np.any(layers['a']['support'] & layers['b']['support']))
        self.assertTrue(np.all(layers['a']['support'][self.a]))
        self.assertTrue(np.all(layers['b']['support'][self.b]))

    def test_all_removed_leaves_no_source_subject_pixels(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a, 'b':self.b})
        removed = fc.compose(base, layers, {'a','b'})
        np.testing.assert_array_equal(removed[self.a | self.b], self.backing[self.a | self.b])

    def test_order_of_removal_and_definition_order_do_not_change_pixels(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'b':self.b, 'a':self.a})
        other, other_layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a, 'b':self.b})
        np.testing.assert_array_equal(base, other)
        np.testing.assert_array_equal(fc.compose(base,layers,{'a','b'}), fc.compose(other,other_layers,{'b','a'}))

    def test_restoration_is_recomposition_not_mutation(self):
        base, layers = fc.make_mutable_layers(self.source, self.backing, {'a':self.a})
        original_base=base.copy()
        fc.compose(base,layers,{'a'})
        np.testing.assert_array_equal(base,original_base)
        np.testing.assert_array_equal(fc.compose(base,layers),self.source)

    def test_protected_region_rejects_subject_overlap(self):
        with self.assertRaises(ValueError):
            fc.make_mutable_layers(self.source,self.backing,{'a':self.a},self.a)

    def test_protected_region_clips_contact_expansion(self):
        p=np.zeros((24,32),bool);p[:,5:7]=True
        base,layers=fc.make_mutable_layers(self.source,self.backing,{'a':self.a},p)
        self.assertFalse(np.any(layers['a']['support'] & p))
        np.testing.assert_array_equal(base[p],self.source[p])

    def test_overlapping_object_silhouettes_fail_instead_of_stealing_pixels(self):
        with self.assertRaises(ValueError):
            fc.make_mutable_layers(self.source,self.backing,{'a':self.a,'duplicate':self.a})

    def test_empty_mask_fails_loudly(self):
        with self.assertRaises(ValueError):
            fc.make_mutable_layers(self.source,self.backing,{'empty':np.zeros((24,32),bool)})

    def test_missing_or_mismatched_backing_is_rejected(self):
        with self.assertRaises(ValueError):
            fc.make_mutable_layers(self.source,self.backing[:12],{'a':self.a})

    def test_unknown_removed_id_fails(self):
        base,layers=fc.make_mutable_layers(self.source,self.backing,{'a':self.a})
        with self.assertRaises(ValueError): fc.compose(base,layers,{'bogus'})

    def test_contact_layer_contains_no_object_alpha(self):
        base,layers=fc.make_mutable_layers(self.source,self.backing,{'a':self.a})
        self.assertTrue(np.all(layers['a']['contact'][self.a,3]==0))
        self.assertTrue(np.any(layers['a']['contact'][...,3]>0))

    def test_crop_keeps_foot_outside_visible_silhouette_without_pivot_clamping(self):
        x,y,w,h=fc.bounds_with_anchor(self.a,[18,19])
        pivot=[(18-x)/w,1-(19-y)/h]
        self.assertTrue(0<=pivot[0]<=1 and 0<=pivot[1]<=1)
        self.assertEqual(x+pivot[0]*w,18)
        self.assertEqual(y+(1-pivot[1])*h,19)
        self.assertLessEqual(x,8);self.assertLessEqual(y,7)

if __name__ == '__main__': unittest.main()
