"""Authored state silhouettes, native palette, and cell envelopes."""
import json
import unittest
from pathlib import Path
ROOT = Path(__file__).parent
FORMS = ['pan-cracked','pan-empty','pan-covered','pan-ready','pantry-empty','pantry-full','pickup-empty','pickup-full','reserve-tray','field-meal','ink-desk','footwork-manual','heavy-frame','reserve-bed','wicket-buckled','wicket-closed','wicket-open','timber-pallet']
class ConnectedSourceTests(unittest.TestCase):
    def setUp(self):
        self.pack = json.loads((ROOT/'kit.json').read_text()) if (ROOT/'kit.json').exists() else {'models': [], 'palette': []}
        self.models = {m['id'].removeprefix('connected-spread-'):m['boxes'] for m in self.pack['models']}
    def test_all_intended_original_forms_exist(self):
        self.assertEqual(set(FORMS), set(self.models))
    def test_geometry_is_bounded_and_uses_existing_palette(self):
        old = json.loads((ROOT.parent/'CurationYard3D'/'kit.json').read_text())['palette']
        self.assertEqual(old,self.pack['palette'])
        for name,boxes in self.models.items():
            self.assertTrue(3<=len(boxes)<=100, name)
            self.assertEqual(len(boxes),len({b['name'] for b in boxes}))
            self.assertGreaterEqual(len({b['color'] for b in boxes}),2)
            for b in boxes:
                self.assertIn(b['color'],range(24))
                for axis in 'xyz': self.assertGreater(b['size'][axis],0)
                for axis in 'xz': self.assertLessEqual(abs(b['center'][axis])+b['size'][axis]/2,.50001)
                self.assertGreaterEqual(b['center']['y']-b['size']['y']/2,-.00001)
                self.assertLessEqual(b['center']['y']+b['size']['y']/2,1.65)
    def test_work_and_ready_states_change_physical_shape(self):
        self.assertIn('pan-covered',self.models)
        for a,b in [('pan-cracked','pan-empty'),('pan-empty','pan-covered'),('pan-covered','pan-ready'),('pantry-empty','pantry-full'),('pickup-empty','pickup-full')]:
            self.assertNotEqual([(p['center'],p['size']) for p in self.models[a]],[(p['center'],p['size']) for p in self.models[b]])
        self.assertFalse(any('parcel' in p['name'] for p in self.models['pickup-empty']))
        self.assertTrue(any('parcel' in p['name'] for p in self.models['pickup-full']))
        self.assertTrue(any('cover' in p['name'] for p in self.models['pan-covered']))
    def test_permission_marker_and_mundane_tools_are_readable_geometry(self):
        self.assertIn('reserve-tray',self.models)
        self.assertTrue(any('tie' in p['name'] for p in self.models['reserve-tray']))
        for tool in ['grinding-bowl','pestle','ink-bottle','label']:
            self.assertTrue(any(tool==p['name'] for p in self.models['ink-desk']),tool)
        self.assertGreaterEqual(len([p for p in self.models['footwork-manual'] if 'footstep' in p['name']]),3)
    def test_claimed_soil_keeps_stakes_cord_and_furrows_without_fake_crop(self):
        self.assertIn('reserve-bed',self.models)
        names=[p['name'] for p in self.models['reserve-bed']]
        self.assertGreaterEqual(len([n for n in names if 'stake' in n]),4)
        self.assertGreaterEqual(len([n for n in names if 'cord' in n]),4)
        self.assertTrue(any('furrow' in n for n in names))
        self.assertFalse(any('crop' in n or 'grain' in n for n in names))
    def test_fieldwork_states_show_broken_hinges_open_aperture_and_reclaimable_boards(self):
        for name in ['wicket-buckled','wicket-closed','wicket-open','timber-pallet']:
            self.assertIn(name,self.models)
        self.assertNotEqual(self.models['wicket-buckled'],self.models['wicket-closed'])
        self.assertNotEqual(self.models['wicket-closed'],self.models['wicket-open'])
        self.assertTrue(any('plank' in b['name'] for b in self.models['timber-pallet']))
if __name__=='__main__': unittest.main()
