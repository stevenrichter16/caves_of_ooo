#!/usr/bin/env python3
"""Actual expansion payload contract; no Unity or subjective listening claim."""
import hashlib
import json
from pathlib import Path
import unittest

REPO=Path(__file__).resolve().parents[2]
WORLD=REPO/'Assets/Resources/Audio/World'

class PlaceAudioAssets(unittest.TestCase):
    def catalog(self):
        p=WORLD/'places-catalog.json'
        self.assertTrue(p.is_file(), 'Separate expansion catalog must exist')
        return json.loads(p.read_text())
    def test_place_ids_are_concrete_and_bounded(self):
        c=self.catalog()
        self.assertEqual({'night-field','dry-wind','marsh'}, {r['id'] for r in c['ambience']})
        self.assertEqual({'bird','insect','frog','dog','water-lap','fire','wood-creak','stone-tick'}, {r['id'] for r in c['details']})
        self.assertFalse(c.get('music'))
    def test_melee_variants_are_distinct_actual_assets(self):
        rows=self.catalog()['sounds']
        for cue in ['SwingBlade','SwingBlunt','SwingNatural','HitCutting','HitPiercing','HitBlunt','HitNatural']:
            selected=[r for r in rows if r['id']==cue]
            self.assertEqual(2,len(selected),cue)
            files=[REPO/'Assets/Resources'/(r['resource']+'.ogg') for r in selected]
            self.assertTrue(all(p.is_file() for p in files),cue)
            self.assertNotEqual(*[hashlib.sha256(p.read_bytes()).hexdigest() for p in files],cue)
    def test_every_row_has_real_imported_asset_and_conservative_gain(self):
        c=self.catalog()
        for group in ['ambience','details','sounds']:
            for r in c[group]:
                p=REPO/'Assets/Resources'/(r['resource']+'.ogg')
                self.assertTrue(p.is_file(),r['resource'])
                self.assertTrue(Path(str(p)+'.meta').is_file(),r['resource'])
                self.assertGreater(r['gain'],0)
                self.assertLessEqual(r['gain'],.6)
    def test_expansion_provenance_and_credits_are_complete(self):
        c=self.catalog();p=WORLD/'places-provenance.json'
        self.assertTrue(p.is_file())
        spec=json.loads((REPO/'Tools/Audio/place_audio_sources.json').read_text())
        manifest=json.loads(p.read_text());self.assertEqual(spec['sources'],manifest['sources'])
        self.assertEqual(len(spec['clips']),len(manifest['clips']))
        credits=(WORLD/'PLACES-CREDITS.txt').read_text()
        for r in spec['clips']:
            self.assertEqual('CC0-1.0',r['license']);self.assertIn(r['credit'],credits);self.assertIn(r['page'],credits)
        covered={r['resource'].removeprefix('Audio/World/')+'.ogg' for g in ['ambience','details','sounds'] for r in c[g]}
        self.assertEqual(covered,{r['path'] for r in spec['clips']})
    def test_original22_still_match_pinned_outputs(self):
        manifest=json.loads((WORLD/'provenance.json').read_text())
        self.assertEqual(22,len(manifest['clips']))
        for r in manifest['clips']:
            self.assertEqual(r['outputSha256'],hashlib.sha256((WORLD/r['path']).read_bytes()).hexdigest(),r['path'])

if __name__=='__main__':unittest.main(verbosity=2)
