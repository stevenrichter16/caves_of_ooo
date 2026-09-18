import unittest
from ArtTools import morrowfast_native_layout as n

class NativeLayoutTests(unittest.TestCase):
    def test_uniform_mapping_keeps_all_twenty_five_rows(self):
        self.assertEqual(n.to_cell(768,0),(40,0))
        self.assertEqual(n.to_cell(768,1023),(40,24))

    def test_authored_source_compiles_complete_native_zone(self):
        d=n.create_layout()
        self.assertEqual(len(d['owners']),71)
        self.assertEqual(len(d['buildings']),5)
        self.assertEqual({(c['x'],c['y']) for c in d['cells']},{(x,y) for x in range(80) for y in range(25)})
        self.assertFalse(any(c['solid'] for c in d['cells'] if c['x'] in (39,40,41) and c['y'] in (0,24)))

    def test_all_open_rooms_and_owner_approaches_are_connected(self):
        d=n.create_layout(); seen=n.flood(d,open_doors=True)
        self.assertIn((40,0),seen)
        for o in d['owners']:
            self.assertTrue(n.approaches(o)&seen,o['id'])
        for b in d['buildings']:
            self.assertTrue({(p['x'],p['y']) for p in b['interior']}&seen,b['id'])

    def test_closed_house_walls_do_not_leak(self):
        d=n.create_layout();seen=n.flood(d,open_doors=False)
        for b in d['buildings']:
            self.assertFalse({(p['x'],p['y']) for p in b['interior']}&seen,b['id'])

if __name__=='__main__':unittest.main()
