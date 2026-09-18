import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import numpy as np
from PIL import Image
from ArtTools.morrowfast_actor_sheets import export, ACTORS, FRAME_SIZE

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Morrowfast'

class MorrowfastActorSheetTests(unittest.TestCase):
    def test_derived_actors_have_only_original_body_alpha_and_one_static_pose(self):
        with tempfile.TemporaryDirectory() as tmp:
            destination = Path(tmp)
            report = export(SOURCE, destination)
            self.assertEqual(2, len(report['actors']))
            for spec, record in zip(ACTORS, report['actors']):
                source = np.array(Image.open(SOURCE / spec['source']).convert('RGBA'))
                sheet = np.array(Image.open(destination / record['file']).convert('RGBA'))
                width, height = FRAME_SIZE
                self.assertEqual((height * 16, width * 4, 4), sheet.shape)
                first = sheet[:height, :width]
                x, y = record['bodyOffset']
                expected = np.zeros((height, width), dtype=np.uint8)
                expected[y:y+source.shape[0], x:x+source.shape[1]] = source[:, :, 3]
                np.testing.assert_array_equal(first[:, :, 3], expected)
                self.assertEqual(0, sheet[:, width:, 3].sum(), 'Unused animation columns stay transparent')
                for row in range(16): np.testing.assert_array_equal(first, sheet[row*height:(row+1)*height, :width])
                self.assertGreater((first[y:y+source.shape[0],x:x+source.shape[1],:3] != source[:,:,:3]).sum(), 0, 'The two hats receive documented distinct palette shifts')
                np.testing.assert_array_equal(first[y+18:y+source.shape[0],x:x+source.shape[1]], source[18:], 'Faces, hands and lower clothing keep source pixels')
                self.assertEqual(0, np.count_nonzero((first[:,:,3] != 0) & (first[:,:,3] != 255)))
                self.assertEqual(hashlib.sha256((SOURCE / spec['source']).read_bytes()).hexdigest(), record['sourceSha256'])
                self.assertNotIn('contact', spec['source'])
                self.assertEqual('static source pose repeated for four facings and four states', record['motion'])

    def test_export_preserves_the_source_manifest_and_source_body_files(self):
        paths = [SOURCE/'build/manifest.json'] + [SOURCE/a['source'] for a in ACTORS]
        before = {str(p):p.read_bytes() for p in paths}
        with tempfile.TemporaryDirectory() as tmp: export(SOURCE, Path(tmp))
        self.assertEqual(before, {str(p):p.read_bytes() for p in paths})

if __name__ == '__main__': unittest.main()
