"""Tests the actual source-to-Unity export contract, including access routes."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from PIL import Image

from ArtTools.felling_unity_export import build_definition, export, reachable

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/FellingSite'


class FellingUnityExportTests(unittest.TestCase):
    def setUp(self):
        self.manifest = json.loads((SOURCE / 'Components/build/manifest.json').read_text())
        self.layout = json.loads((SOURCE / 'layout.json').read_text())
        self.physical = json.loads((SOURCE / 'Integration/physical-layout.json').read_text())

    def definition(self):
        return build_definition(self.manifest, self.layout, self.physical)

    def test_actual_inventory_contains_every_fixed_and_removable_layer(self):
        d = self.definition()
        self.assertEqual(55, len(d['layers']))
        self.assertEqual(39, sum(c['mutable'] for c in d['layers']))
        self.assertEqual({c['id'] for c in self.manifest['components']}, {c['id'] for c in d['layers']})
        self.assertEqual(39, sum(bool(c['contactResource']) for c in d['layers']))

    def test_preserves_exact_source_bounds_foot_and_art_space(self):
        d = self.definition()
        self.assertEqual((1536, 1024, 32, 16, 224), tuple(d[k] for k in ['canvasWidth','canvasHeight','pixelsPerCell','originX','groundOffset']))
        by_id={c['id']:c for c in d['layers']}
        for c in self.manifest['components']:
            self.assertEqual(c['bounds'], by_id[c['id']]['bounds'])
            self.assertEqual(c['foot'][1], by_id[c['id']]['footPixelY'])

    def test_source_landmark_cells_and_counts_are_preserved(self):
        d = self.definition()
        self.assertEqual([(36,10),(43,10),(33,13),(46,13),(36,16),(43,16)],[(c['x'],c['y']) for c in d['landmarks'] if c['kind']=='bare'])
        self.assertEqual([(40,8)],[(c['x'],c['y']) for c in d['landmarks'] if c['kind']=='seventh'])

    def test_every_mutable_prop_has_a_reachable_adjacent_approach(self):
        d=self.definition()
        walk={(c['x'],c['y']) for c in d['cells'] if not c['solid']}
        walk-={(c['anchorX'],c['anchorY']) for c in d['layers'] if c['blocksMovement']}
        visited=reachable(walk,(40,20))
        for c in d['layers']:
            if c['mutable']:
                self.assertTrue(any(max(abs(x-c['anchorX']),abs(y-c['anchorY']))<=1 for x,y in visited),c['id'])
        for c in d['landmarks']:
            self.assertIn((c['x'],c['y']),visited)
        self.assertIn((40,24),visited)
        for edge in [lambda x,y:x==0,lambda x,y:x==79,lambda x,y:y==0,lambda x,y:y==24]:
            self.assertTrue(any(edge(x,y) for x,y in visited))

    def test_reachable_does_not_cut_blocked_diagonal_corners(self):
        self.assertEqual({(0,0)},reachable({(0,0),(1,1)},(0,0)))
        self.assertIn((1,1),reachable({(0,0),(1,0),(0,1),(1,1)},(0,0)))

    def test_invalid_and_duplicate_component_records_fail(self):
        for mutate in [lambda m:m['components'].append(copy.deepcopy(m['components'][0])),lambda m:m['components'][0].update(bounds=[-1,0,20,20]),lambda m:m.update(schemaVersion=999)]:
            with self.subTest(mutate=mutate):
                m=copy.deepcopy(self.manifest);mutate(m)
                with self.assertRaises(ValueError):build_definition(m,self.layout,self.physical)

    def test_source_paths_cannot_escape_export_root(self):
        for path in ['../outside.png','/tmp/outside.png','sprites/../../outside.png']:
            m=copy.deepcopy(self.manifest);m['components'][0]['sprite']['path']=path
            with self.subTest(path=path),self.assertRaises(ValueError):build_definition(m,self.layout,self.physical)

    def test_collisions_are_complete_unique_cells_and_trunk_is_immutable(self):
        d=self.definition();cells={(c['x'],c['y']):c for c in d['cells']}
        self.assertEqual(2000,len(d['cells']));self.assertEqual(2000,len(cells))
        self.assertTrue(cells[40,0]['solid']);self.assertTrue(cells[40,0]['opaque'])
        self.assertFalse(cells[40,20]['solid']);self.assertFalse(cells[40,24]['solid'])
        self.assertFalse(next(c for c in d['layers'] if c['id']=='stump-main')['mutable'])

    def test_actual_export_copies_contributions_without_repainting(self):
        with tempfile.TemporaryDirectory() as temp:
            target=Path(temp)/'FellingSite';report=export(SOURCE,target)
            self.assertEqual('passed',report['status'])
            self.assertEqual(0,report['intactChangedPixels'])
            self.assertEqual(55,report['componentCount'])
            for record in report['files']:
                self.assertEqual(record['sourceSha256'],hashlib.sha256((target/record['destination']).read_bytes()).hexdigest())
            with Image.open(target/'Art/base.png') as backing:
                self.assertEqual((1536,1024),backing.size)


if __name__=='__main__':unittest.main()
