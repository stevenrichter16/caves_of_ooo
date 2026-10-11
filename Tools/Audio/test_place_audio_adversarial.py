#!/usr/bin/env python3
"""Counterchecks of actual payload validation and preparation edge cases."""
import copy
import json
from pathlib import Path
import shutil
import tempfile
import unittest
import numpy as np
from prepare_place_audio import prepare, RATE, download
from verify_place_audio import verify, signal_errors
from test_place_audio import WORLD

class PlaceAudioCounterchecks(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='coo-place-counter-')
        self.root=Path(self.temp.name)/'World'
        shutil.copytree(WORLD,self.root)
        self.assertTrue(verify(self.root,decode=False)['passed'])
    def tearDown(self):self.temp.cleanup()
    def rejected(self,needle):
        result=verify(self.root,decode=False)
        self.assertFalse(result['passed']);self.assertTrue(any(needle in s for s in result['findings']),result['findings'])
    def editjson(self,name,change):
        p=self.root/name;r=json.loads(p.read_text());change(r);p.write_text(json.dumps(r))
    def test_missing_asset_rejected(self):
        (self.root/'Places/frog_01.ogg').unlink();self.rejected('missing asset')
    def test_corrupt_actual_bytes_rejected(self):
        (self.root/'Combat/hit_cut_01.ogg').write_bytes(b'not audio');self.rejected('output hash')
    def test_unknown_catalog_row_rejected(self):
        self.editjson('places-catalog.json',lambda r:r['details'].append(dict(id='phantom',resource='missing',gain=.1)));self.rejected('catalog')
    def test_wrong_catalog_gain_rejected(self):
        self.editjson('places-catalog.json',lambda r:r['details'][0].update(gain=2));self.rejected('catalog')
    def test_changed_original_source_rejected(self):
        self.editjson('places-provenance.json',lambda r:r['sources']['birds.ogg'].update(sha256='0'*64));self.rejected('source provenance')
    def test_changed_excerpt_provenance_rejected(self):
        self.editjson('places-provenance.json',lambda r:r['clips'][1].update(startSeconds=0));self.rejected('provenance startSeconds')
    def test_streaming_bed_preload_rejected(self):
        p=self.root/'Places/dry_wind.ogg.meta';p.write_text(p.read_text().replace('preloadAudioData: 0','preloadAudioData: 1'));self.rejected('preloadAudioData')
    def test_duplicate_guid_rejected(self):
        shutil.copyfile(self.root/'Places/frog_01.ogg.meta',self.root/'Places/frog_02.ogg.meta');self.rejected('duplicate GUID')
    def test_missing_credit_rejected(self):
        (self.root/'PLACES-CREDITS.txt').write_text('');self.rejected('missing credit/source')
    def test_missing_license_rejected(self):
        (self.root/'Licenses/CC0-1.0.txt').unlink();self.rejected('missing complete CC0')
    def test_unreferenced_clip_rejected(self):
        shutil.copyfile(self.root/'Places/frog_01.ogg',self.root/'Places/unused.ogg');self.rejected('unreferenced')
    def test_cached_source_hash_change_rejected(self):
        p=Path(self.temp.name)/'source.ogg';p.write_bytes(b'wrong')
        with self.assertRaisesRegex(ValueError,'Cached source changed'):download(dict(bytes=5,sha256='0'*64,url='https://example.invalid'),p)

class PlaceAudioSignals(unittest.TestCase):
    def test_wind_filter_retains_air_band_and_reduces_rumble_and_chirps(self):
        import prepare_place_audio as pipeline
        self.assertTrue(hasattr(pipeline, 'apply_filters'), 'Declared wind filter must process actual samples')
        t=np.arange(RATE*2)/RATE
        for hz,low,high in [(300,.7,1.1),(4000,0,.04),(15,0,.15)]:
            x=(.1*np.sin(2*np.pi*hz*t)).astype(np.float32).reshape(-1,1)
            y=pipeline.apply_filters(x,dict(highpassHz=45,lowpassHz=650))
            ratio=float(np.sqrt(np.mean(y[RATE:]**2))/np.sqrt(np.mean(x[RATE:]**2)))
            self.assertGreaterEqual(ratio,low);self.assertLess(ratio,high)

    def test_short_positive_and_bad_signals(self):
        x=(.1*np.sin(np.arange(RATE)*.02)).astype(np.float32)
        self.assertEqual([],signal_errors(x,RATE,1,'detail')[0])
        for bad,rate,channels,needle in [(x*0,RATE,1,'silent'),(x*10,RATE,1,'peak'),(x,22050,1,'sample'),(np.tile(x,5),RATE,1,'duration'),(x,RATE,2,'channels')]:
            with self.subTest(needle=needle):self.assertTrue(any(needle in s for s in signal_errors(bad,rate,channels,'detail')[0]))
        x[1]=np.nan;self.assertTrue(signal_errors(x,RATE,1,'detail')[0])
    def test_bed_seam_counter(self):
        x=np.zeros((RATE*12,2),np.float32);x[1:-1]=.1
        self.assertEqual([],signal_errors(x.ravel(),RATE,2,'ambience')[0]);x[-1]=.2
        self.assertIn('loop seam',signal_errors(x.ravel(),RATE,2,'ambience')[0])
    def test_preparation_rejects_invalid_inputs_and_honors_excerpt(self):
        x=np.full((RATE*3,1),.1,np.float32)
        row=dict(kind='detail',startSeconds=1,durationSeconds=1)
        y,_=prepare(x,row);self.assertEqual(RATE+2*round(.01*RATE),len(y));self.assertAlmostEqual(0,float(y[0,0]))
        for bad in [np.zeros_like(x),np.full_like(x,np.nan)]:
            with self.assertRaises(ValueError):prepare(bad,row)
        with self.assertRaisesRegex(ValueError,'outside'):prepare(x,dict(row,startSeconds=3))

if __name__=='__main__':unittest.main(verbosity=2)
