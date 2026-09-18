import unittest
import numpy as np
from PIL import Image
from ArtTools import catacomb_scene as cs


class CatacombCompositionTests(unittest.TestCase):
    def setUp(self):
        self.base=Image.new('RGBA',(24,20),(40,48,36,255))
        self.a=Image.new('RGBA',(6,6));self.a.putpixel((2,3),(130,80,50,255))
        self.b=Image.new('RGBA',(6,6));self.b.putpixel((2,3),(30,160,150,255))
        self.layers=[dict(id='a',bounds=[4,4,6,6],depth=1,mutable=True,image=self.a),dict(id='b',bounds=[4,4,6,6],depth=2,mutable=True,image=self.b)]

    def test_front_layer_owns_overlap(self):
        self.assertEqual(cs.compose(self.base,self.layers).getpixel((6,7)),(30,160,150,255))

    def test_removing_front_reveals_real_surviving_back_object(self):
        self.assertEqual(cs.compose(self.base,self.layers,{'b'}).getpixel((6,7)),(130,80,50,255))

    def test_removing_back_keeps_front_unchanged(self):
        self.assertEqual(cs.compose(self.base,self.layers,{'a'}).getpixel((6,7)),(30,160,150,255))

    def test_removing_all_exposes_complete_substrate(self):
        self.assertEqual(cs.compose(self.base,self.layers,{'a','b'}).tobytes(),self.base.tobytes())

    def test_removal_does_not_mutate_original_or_neighbor_pixels(self):
        before=self.base.tobytes();a=np.array(cs.compose(self.base,self.layers));b=np.array(cs.compose(self.base,self.layers,{'b'}))
        self.assertEqual(self.base.tobytes(),before);self.assertEqual(np.any(a!=b,axis=2).sum(),1)

    def test_unknown_id_rejected(self):
        with self.assertRaises(ValueError):cs.compose(self.base,self.layers,{'missing'})

    def test_fixed_layer_removal_rejected(self):
        self.layers[0]['mutable']=False
        with self.assertRaises(ValueError):cs.compose(self.base,self.layers,{'a'})

    def test_duplicate_instance_rejected(self):
        with self.assertRaises(ValueError):cs.compose(self.base,self.layers+self.layers[:1])

    def test_depth_and_id_sort_is_independent_of_definition_order(self):
        self.assertEqual(cs.compose(self.base,self.layers).tobytes(),cs.compose(self.base,list(reversed(self.layers))).tobytes())

    def test_source_resize_uses_only_original_palette_and_binary_alpha(self):
        im=cs.resize_sprite(self.a,.5);self.assertEqual(im.size,(3,3))
        self.assertLessEqual({tuple(p) for p in np.asarray(im).reshape(-1,4)},
                             {tuple(p) for p in np.asarray(self.a).reshape(-1,4)})

    def test_invalid_scale_rejected(self):
        for scale in (0,-1,float('nan'),float('inf')):
            with self.assertRaises(ValueError):cs.resize_sprite(self.a,scale)

    def test_off_canvas_placement_rejected(self):
        self.layers[0]['bounds']=[22,4,6,6]
        with self.assertRaises(ValueError):cs.compose(self.base,self.layers)

    def test_incorrect_sprite_dimensions_rejected(self):
        self.layers[0]['bounds']=[4,4,5,6]
        with self.assertRaises(ValueError):cs.compose(self.base,self.layers)

    def test_route_reaches_destination_around_blockers(self):
        cells={(x,y) for x in range(6) for y in range(5)}-{(3,y) for y in range(4)}
        self.assertIn((5,0),cs.reachable(cells,(0,0)))

    def test_sealed_route_and_diagonal_corner_cut_are_not_reachable(self):
        cells={(x,y) for x in range(6) for y in range(5)}-{(3,y) for y in range(5)}
        self.assertNotIn((5,0),cs.reachable(cells,(0,0)))
        self.assertEqual(cs.reachable({(0,0),(1,1)},(0,0)),{(0,0)})

    def test_blocker_footprint_changes_only_for_removed_mutable(self):
        c=[dict(id='a',mutable=True,blocksMovement=True,footprintCells=[[1,1]]),dict(id='b',mutable=False,blocksMovement=True,footprintCells=[[2,1]])]
        self.assertEqual(cs.blocked_cells(c),{(1,1),(2,1)})
        self.assertEqual(cs.blocked_cells(c,{'a'}),{(2,1)})

    def test_blocker_static_removal_cannot_bypass_navigation(self):
        c=[dict(id='a',mutable=False,blocksMovement=True,footprintCells=[[2,1]])]
        with self.assertRaises(ValueError):cs.blocked_cells(c,{'a'})


if __name__=='__main__':unittest.main()
