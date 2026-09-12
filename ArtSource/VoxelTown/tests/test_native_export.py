"""Export failures preserve prior files; data-only results stay deterministic."""
import builtins
import contextlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from town_generator.native_assets import export_assets
from town_generator.native_generate import main,export_region


class NativeExportTests(unittest.TestCase):
    def test_missing_blender_rejects_render_before_touching_prior_exports(self):
        original=builtins.__import__
        def importing(name,*args,**kwargs):
            if name=='bpy':raise ImportError('Blender deliberately unavailable')
            return original(name,*args,**kwargs)
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);(root/'assets.json').write_text('prior-assets');(root/'region.json').write_text('prior-region')
            with patch('builtins.__import__',side_effect=importing):
                with self.assertRaises(ImportError):main(['--output',folder,'--render'])
            self.assertEqual('prior-assets',(root/'assets.json').read_text())
            self.assertEqual('prior-region',(root/'region.json').read_text())

    def test_bad_region_cannot_replace_existing_file(self):
        with tempfile.TemporaryDirectory() as folder:
            target=Path(folder)/'region.json';target.write_text('keep')
            with self.assertRaises(ValueError):export_region(target,{'schemaVersion':1})
            self.assertEqual('keep',target.read_text())
            self.assertEqual(['region.json'],[p.name for p in Path(folder).iterdir()])

    def test_invalid_envelope_rejects_before_any_export(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):main(['--output',folder,'--width','39'])
            self.assertFalse(list(Path(folder).iterdir()))

    def test_data_export_is_repeatable_and_preserves_unrelated_files(self):
        with tempfile.TemporaryDirectory() as folder,contextlib.redirect_stdout(io.StringIO()):
            root=Path(folder);(root/'manual.txt').write_text('keep')
            main(['--output',folder]);before={p.name:p.read_bytes() for p in root.iterdir()}
            main(['--output',folder]);self.assertEqual(before,{p.name:p.read_bytes() for p in root.iterdir()})
            self.assertEqual(4,len(json.loads((root/'region.json').read_text())['chunks']))

    def test_asset_export_replaces_symlink_without_rewriting_its_target(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);manual=root/'manual.json';manual.write_text('keep')
            target=root/'assets.json';target.symlink_to(manual);export_assets(target)
            self.assertFalse(target.is_symlink());self.assertEqual('keep',manual.read_text())

    def test_nonfile_asset_destination_is_rejected_without_temporary_debris(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);target=root/'assets.json';target.mkdir()
            with self.assertRaises(OSError):export_assets(target)
            self.assertTrue(target.is_dir());self.assertEqual(['assets.json'],[p.name for p in root.iterdir()])


if __name__=='__main__':unittest.main()
