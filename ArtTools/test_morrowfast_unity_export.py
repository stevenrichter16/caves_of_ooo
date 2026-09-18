"""Independent source-to-Unity art export checks; gameplay geometry is separate."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from ArtTools.morrowfast_unity_export import build_art_definition, export

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Morrowfast'

class MorrowfastUnityArtExportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.manifest = json.loads((SOURCE / 'build/manifest.json').read_text())

    def test_uniform_topdown_transform_keeps_all_image_rows(self):
        art = build_art_definition(self.manifest)
        self.assertEqual((1536,1024,40.96,21.25),tuple(art[k] for k in ['canvasWidth','canvasHeight','pixelsPerCell','originX']))
        self.assertEqual(25,art['canvasHeight']/art['pixelsPerCell'])
        self.assertEqual(37.5,art['canvasWidth']/art['pixelsPerCell'])

    def test_every_semantic_owner_and_source_layer_retains_identity(self):
        art = build_art_definition(self.manifest)
        self.assertEqual({c['id'] for c in self.manifest['components']},{c['id'] for c in art['owners']})
        self.assertEqual({c['id'] for c in self.manifest['layers']},{c['id'] for c in art['layers']})
        exported = {c['id']:c for c in art['layers']}
        for layer in self.manifest['layers']:
            self.assertEqual(layer['bounds'],exported[layer['id']]['bounds'])
            self.assertEqual(layer['ownerId'],exported[layer['id']]['ownerId'])
            self.assertEqual(layer['z'],exported[layer['id']]['z'])

    def test_room_reveal_and_actor_anchors_are_exported_without_unity_geometry_claims(self):
        art = build_art_definition(self.manifest)
        self.assertEqual(5,len(art['rooms']))
        by_id = {o['id']:o for o in art['owners']}
        for c in self.manifest['components']:
            owner=by_id[c['id']]
            self.assertEqual(c['foot'],owner['sourceFoot'])
            self.assertEqual(int(21.25+c['foot'][0]/40.96),owner['anchorX'])
            self.assertEqual(min(24,int(c['foot'][1]/40.96)),owner['anchorY'])
        self.assertNotIn('cells',art)
        self.assertNotIn('navigation',art)

    def test_invalid_owner_and_resource_records_are_refused_before_export(self):
        mutations = [lambda m:m['layers'][0].update(path='../escape.png'),lambda m:m['layers'][0].update(ownerId='missing'),lambda m:m['layers'][0].update(bounds=[-1,0,10,10]),lambda m:m['components'].append(copy.deepcopy(m['components'][0])),lambda m:m['rooms'][0].update(roofId='missing'),lambda m:m['layers'][0].update(z=float('nan'))]
        for mutate in mutations:
            m=copy.deepcopy(self.manifest);mutate(m)
            with self.subTest(mutate=mutate),self.assertRaises(ValueError):build_art_definition(m)

    def test_export_copies_source_bytes_and_independently_reconstructs_the_baseline(self):
        with tempfile.TemporaryDirectory() as temp:
            destination=Path(temp)
            report=export(SOURCE,destination)
            self.assertEqual(0,report['intactChangedPixels'])
            self.assertEqual(len(self.manifest['layers']),report['layerCount'])
            self.assertGreaterEqual(report['assetCount'],len(self.manifest['layers'])+2)
            for record in report['files']:
                expected=hashlib.sha256((SOURCE/record['source']).read_bytes()).hexdigest()
                actual=hashlib.sha256((destination/record['destination']).read_bytes()).hexdigest()
                self.assertEqual(expected,actual,record['source'])
            self.assertTrue((destination/'art-definition.json').exists())
            self.assertFalse((destination/'definition.json').exists(),'Art exporter must not replace root-owned gameplay definition')

if __name__=='__main__':unittest.main()
