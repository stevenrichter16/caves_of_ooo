"""Normal contracts and synthetic PNG fixtures; no game/source artwork is changed."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

try:
    from .felling_components_contract import (
        ManifestValidationError, validate_manifest, remove_components, restore_components,
    )
except ImportError:
    from felling_components_contract import (
        ManifestValidationError, validate_manifest, remove_components, restore_components,
    )


def sample_manifest():
    digest = "a" * 64
    component = {
        "id": "rock-a", "name": "South rock", "kind": "rock",
        "mutable": True, "interaction": "remove", "contributesToComposite": True,
        "depth": 4, "bounds": [500, 500, 8, 8], "foot": [504, 508],
        "pivot": [0.5, 1], "footprintCells": [[31, 8]],
        "sprite": {"path": "rock-a.png", "sha256": digest}, "contact": None,
        "repair": {"maskPath": "rock-a-mask.png", "surface": "stone", "inferred": True},
        "extraction": {"method": "source-mask", "quality": "reviewed", "notes": []},
        "defaultVisible": True,
    }
    other = copy.deepcopy(component)
    other.update(id="plant-b", name="Fern", kind="plant")
    other["sprite"]["path"] = "plant-b.png"
    other["repair"]["maskPath"] = "plant-b-mask.png"
    # Equal depth is legal; consumers sort ties by stable ID.
    return {
        "schemaVersion": 1, "id": "felling-site",
        "canvas": {"width": 1536, "height": 1024, "pixelsPerCell": 32,
                   "imageGroundOrigin": [0, 224], "regionOrigin": [16, 0]},
        "reference": {"path": "reference.png", "sha256": digest},
        "baseline": {"path": "baseline.png", "sha256": digest},
        "base": {"path": "base.png", "sha256": digest,
                 "inferenceMaskPath": "inference-mask.png", "retainsStatic": True},
        "report": {"path": "report.json"}, "components": [component, other],
    }


class NormalContractTests(unittest.TestCase):
    def test_valid_dictionary_and_equal_depths(self):
        report = validate_manifest(sample_manifest())
        self.assertTrue(report["ok"], report)
        self.assertTrue(report["valid"])
        self.assertEqual(2, report["componentCount"])

    def test_report_is_json_serializable(self):
        json.dumps(validate_manifest(sample_manifest()), allow_nan=False)

    def test_validation_does_not_change_input(self):
        manifest = sample_manifest()
        before = copy.deepcopy(manifest)
        validate_manifest(manifest)
        self.assertEqual(before, manifest)

    def test_remove_restore_and_idempotency(self):
        manifest = sample_manifest()
        self.assertEqual(frozenset({"rock-a"}), remove_components(manifest, [], ["rock-a"]))
        self.assertEqual(frozenset({"rock-a"}), remove_components(manifest, ["rock-a"], ["rock-a", "rock-a"]))
        self.assertEqual(frozenset(), restore_components(manifest, ["rock-a"], ["rock-a"]))
        self.assertEqual(frozenset(), restore_components(manifest, [], ["rock-a"]))

    def test_removal_order_and_manifest_order_do_not_matter(self):
        manifest = sample_manifest()
        ab = remove_components(manifest, remove_components(manifest, [], ["rock-a"]), ["plant-b"])
        ba = remove_components(manifest, remove_components(manifest, [], ["plant-b"]), ["rock-a"])
        manifest["components"].reverse()
        self.assertEqual(ab, ba)
        self.assertEqual(ab, remove_components(manifest, [], ["rock-a", "plant-b"]))

    def test_state_failure_is_atomic_and_inputs_are_unchanged(self):
        manifest = sample_manifest()
        state = {"rock-a"}
        requested = ["plant-b", "missing"]
        with self.assertRaises(ManifestValidationError):
            remove_components(manifest, state, requested)
        self.assertEqual({"rock-a"}, state)
        self.assertEqual(["plant-b", "missing"], requested)

    def test_mutable_noncontributing_record_is_structurally_legal(self):
        manifest = sample_manifest()
        manifest["components"][0]["contributesToComposite"] = False
        self.assertTrue(validate_manifest(manifest)["ok"])
        with self.assertRaises(ManifestValidationError):
            remove_components(manifest, [], ["rock-a"])


class AssetContractTests(unittest.TestCase):
    def setUp(self):
        from PIL import Image
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.manifest = sample_manifest()
        for field in ("reference", "baseline", "base"):
            path = self.root / self.manifest[field]["path"]
            Image.new("RGB", (1536, 1024), (20, 30, 40)).save(path)
            self.manifest[field]["sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
        Image.new("L", (1536, 1024), 0).save(self.root / "inference-mask.png")
        for component in self.manifest["components"]:
            image = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
            image.putpixel((4, 4), (100, 90, 80, 255))
            path = self.root / component["sprite"]["path"]
            image.save(path)
            component["sprite"]["sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
            Image.new("L", (8, 8), 255).save(self.root / component["repair"]["maskPath"])

    def tearDown(self):
        self.temp.cleanup()

    def validate(self):
        return validate_manifest(self.manifest, self.root, require_assets=True)

    def test_real_dimensions_hash_alpha_and_grayscale_masks(self):
        self.assertTrue(self.validate()["ok"], self.validate())
        # report.path is the future report destination, not an input asset.
        self.assertFalse((self.root / "report.json").exists())

    def test_hash_mismatch_fails(self):
        self.manifest["components"][0]["sprite"]["sha256"] = "b" * 64
        self.assertFalse(self.validate()["ok"])

    def test_missing_asset_fails(self):
        (self.root / "rock-a.png").unlink()
        self.assertFalse(self.validate()["ok"])

    def test_corrupt_png_fails(self):
        path = self.root / "rock-a.png"
        path.write_bytes(b"not a PNG")
        self.manifest["components"][0]["sprite"]["sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
        self.assertFalse(self.validate()["ok"])

    def test_wrong_crop_dimensions_fail(self):
        self.manifest["components"][0]["bounds"][2] = 7
        self.assertFalse(self.validate()["ok"])

    def test_fully_opaque_rectangle_is_not_a_cutout(self):
        self.replace_sprite("RGBA", (100, 100, 100, 255))
        self.assertFalse(self.validate()["ok"])

    def test_fully_transparent_empty_cutout_fails(self):
        self.replace_sprite("RGBA", (0, 0, 0, 0))
        self.assertFalse(self.validate()["ok"])

    def test_rgb_sprite_without_alpha_fails(self):
        self.replace_sprite("RGB", (100, 100, 100))
        self.assertFalse(self.validate()["ok"])

    def test_empty_repair_mask_fails(self):
        from PIL import Image
        Image.new("L", (8, 8), 0).save(self.root / "rock-a-mask.png")
        self.assertFalse(self.validate()["ok"])

    def test_contact_obeys_crop_and_alpha_contract(self):
        component = self.manifest["components"][0]
        component["contact"] = copy.deepcopy(component["sprite"])
        self.assertTrue(self.validate()["ok"], self.validate())
        component["contact"]["sha256"] = "b" * 64
        self.assertFalse(self.validate()["ok"])

    def test_symlink_escape_fails_even_with_matching_asset_bytes(self):
        with tempfile.TemporaryDirectory() as outside:
            path = Path(outside) / "rock.png"
            path.write_bytes((self.root / "rock-a.png").read_bytes())
            (self.root / "rock-a.png").unlink()
            (self.root / "rock-a.png").symlink_to(path)
            self.assertFalse(self.validate()["ok"])

    def replace_sprite(self, mode, color):
        from PIL import Image
        path = self.root / "rock-a.png"
        Image.new(mode, (8, 8), color).save(path)
        self.manifest["components"][0]["sprite"]["sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    unittest.main()
