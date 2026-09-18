import unittest
import numpy as np
from ArtTools import morrowfast_components as m


class ComponentContracts(unittest.TestCase):
    def entry(self, name='cup'):
        return dict(id=name,label=name,kind='container',bounds=[2,2,6,6],
                    candidatePolygon=[[2,2],[7,2],[7,7],[2,7]],foot=[5,7],
                    actions=['Examine'],solid=True)

    def test_source_pixels_and_hollow_silhouette(self):
        e=self.entry();e['excludePolygons']=[[[4,4],[5,4],[5,5],[4,5]]]
        mask=m.entry_mask(e,(10,10))
        self.assertFalse(mask[4,4]);self.assertTrue(mask[3,3])
        source=np.arange(300,dtype=np.uint8).reshape(10,10,3)
        out=m.cutout(source,mask)
        np.testing.assert_array_equal(out[mask,:3],source[mask])
        self.assertTrue(np.all(out[~mask]==0))

    def test_disconnected_parts_are_included(self):
        e=self.entry();e['bounds']=[0,0,10,10]
        e['includePolygons']=[[[8,8],[9,8],[9,9],[8,9]]]
        self.assertTrue(m.entry_mask(e,(10,10))[9,9])

    def test_invalid_geometry_and_duplicates_rejected(self):
        for field,value in [('foot',[float('nan'),2]),('bounds',[-1,2,4,4]),
                            ('candidatePolygon',[[2,2],[7,7],[2,7],[7,2]])]:
            e=self.entry();e[field]=value
            with self.subTest(field=field),self.assertRaises(ValueError):m.validate_entries([e],(10,10))
        with self.assertRaises(ValueError):m.validate_entries([self.entry(),self.entry()],(10,10))

    def test_region_repair_preserves_every_known_outside_pixel(self):
        source=np.full((20,20,3),70,np.uint8); backing=np.full_like(source,25)
        a=np.zeros((20,20),bool);a[4:7,4:7]=True
        b=np.zeros_like(a);b[4:7,9:12]=True
        base,layers=m.repair_regions(source,backing,{'a':a,'b':b},radius=2)
        union=layers['a']['support']|layers['b']['support']
        np.testing.assert_array_equal(base[~union],source[~union])
        self.assertFalse(np.any(layers['a']['support']&layers['b']['support']))
        np.testing.assert_array_equal(m.reassemble(base,layers),source)
        self.assertTrue(np.any(m.reassemble(base,layers,hidden={'a'})[a]!=source[a]))

    def test_overlap_must_be_resolved_explicitly(self):
        a=np.ones((8,8),bool)
        with self.assertRaises(ValueError):m.repair_regions(np.zeros((8,8,3),np.uint8),np.zeros((8,8,3),np.uint8),{'a':a,'b':a})

    def test_unknown_hidden_owner_is_rejected(self):
        a=np.zeros((8,8),bool);a[3:5,3:5]=True
        base,layers=m.repair_regions(np.zeros((8,8,3),np.uint8),np.zeros((8,8,3),np.uint8),{'a':a})
        with self.assertRaises(ValueError):m.reassemble(base,layers,hidden={'missing'})

    def test_cast_shadow_is_not_transferred_to_inferred_cistern_ground(self):
        a=np.zeros((40,40),bool);a[16:24,16:24]=True
        source=np.full((40,40,3),35,np.uint8)
        backing=np.full_like(source,80)
        base,layers=m.repair_regions(source,backing,{'cistern':a},
            surface_overrides={'cistern':{'radius':8,'colorMatch':False}})
        np.testing.assert_array_equal(base[a],backing[a])
        self.assertEqual(layers['cistern']['offset'],[0,0,0])
        self.assertTrue(layers['cistern']['support'][16,9])
        np.testing.assert_array_equal(m.reassemble(base,layers),source)

    def test_barrier_avoids_repairing_adjacent_wall(self):
        a=np.zeros((12,12),bool);a[4:7,4:7]=True
        protected=np.zeros_like(a);protected[:,8:]=True
        src=np.full((12,12,3),100,np.uint8)
        base,layers=m.repair_regions(src,np.zeros_like(src),{'a':a},protected=protected)
        self.assertFalse(np.any(layers['a']['support']&protected))
        np.testing.assert_array_equal(base[protected],src[protected])

    def test_uniform_world_mapping_covers_full_topdown_canvas(self):
        self.assertEqual(m.to_world(768,1024),(40.0,0.0))
        self.assertEqual(m.to_world(768,0),(40.0,25.0))
        self.assertEqual(m.to_world(0,1024),(21.25,0.0))


if __name__=='__main__':unittest.main()
