"""Malformed manifests and state requests must fail closed without partial writes."""
import copy
import math
import unittest

try:
    from .felling_components_contract import (
        ManifestValidationError, validate_manifest, remove_components, restore_components,
    )
    from .test_felling_components_contract import sample_manifest
except ImportError:
    from felling_components_contract import (
        ManifestValidationError, validate_manifest, remove_components, restore_components,
    )
    from test_felling_components_contract import sample_manifest


class AdversarialParseTests(unittest.TestCase):
    def check_bad(self, edit):
        manifest = sample_manifest()
        edit(manifest)
        report = validate_manifest(manifest)
        self.assertFalse(report["ok"], report)
        self.assertTrue(report["errors"])

    def test_non_object_roots(self):
        for bad in (None, [], "{}", 1, True):
            with self.subTest(value=bad):
                self.assertFalse(validate_manifest(bad)["ok"])

    def test_unsupported_schema(self): self.check_bad(lambda m: m.update(schemaVersion=2))
    def test_boolean_schema_is_not_integer(self): self.check_bad(lambda m: m.update(schemaVersion=True))
    def test_missing_scene_id(self): self.check_bad(lambda m: m.pop("id"))
    def test_blank_scene_id(self): self.check_bad(lambda m: m.update(id=" "))
    def test_wrong_canvas(self): self.check_bad(lambda m: m["canvas"].update(width=1024))
    def test_fractional_canvas(self): self.check_bad(lambda m: m["canvas"].update(height=1024.0))
    def test_wrong_density(self): self.check_bad(lambda m: m["canvas"].update(pixelsPerCell=16))
    def test_wrong_ground_origin(self): self.check_bad(lambda m: m["canvas"].update(imageGroundOrigin=[0, 0]))
    def test_wrong_zone_origin(self): self.check_bad(lambda m: m["canvas"].update(regionOrigin=[0, 0]))
    def test_canvas_must_be_object(self): self.check_bad(lambda m: m.update(canvas=[]))
    def test_missing_reference(self): self.check_bad(lambda m: m.pop("reference"))
    def test_bad_sha256(self): self.check_bad(lambda m: m["reference"].update(sha256="not-a-hash"))
    def test_static_flag_is_boolean(self): self.check_bad(lambda m: m["base"].update(retainsStatic=1))
    def test_components_must_be_list(self): self.check_bad(lambda m: m.update(components={}))
    def test_component_must_be_object(self): self.check_bad(lambda m: m["components"].append(None))
    def test_duplicate_component_id(self): self.check_bad(lambda m: m["components"][1].update(id="rock-a"))
    def test_empty_component_id(self): self.check_bad(lambda m: m["components"][0].update(id=""))
    def test_blank_component_name(self): self.check_bad(lambda m: m["components"][0].update(name=" "))
    def test_missing_kind(self): self.check_bad(lambda m: m["components"][0].pop("kind"))
    def test_mutable_must_be_boolean(self): self.check_bad(lambda m: m["components"][0].update(mutable="true"))
    def test_contributing_must_be_boolean(self): self.check_bad(lambda m: m["components"][0].update(contributesToComposite=1))
    def test_visibility_must_be_boolean(self): self.check_bad(lambda m: m["components"][0].update(defaultVisible=0))
    def test_interaction_must_be_text_or_null(self): self.check_bad(lambda m: m["components"][0].update(interaction=[]))
    def test_nonfinite_depth(self): self.check_bad(lambda m: m["components"][0].update(depth=math.inf))
    def test_boolean_depth(self): self.check_bad(lambda m: m["components"][0].update(depth=True))
    def test_bounds_wrong_length(self): self.check_bad(lambda m: m["components"][0].update(bounds=[1, 2, 3]))
    def test_bounds_nan(self): self.check_bad(lambda m: m["components"][0].update(bounds=[math.nan, 0, 8, 8]))
    def test_bounds_negative(self): self.check_bad(lambda m: m["components"][0].update(bounds=[-1, 0, 8, 8]))
    def test_bounds_zero_extent(self): self.check_bad(lambda m: m["components"][0].update(bounds=[0, 0, 0, 8]))
    def test_bounds_fractional(self): self.check_bad(lambda m: m["components"][0].update(bounds=[0, 0, 8.5, 8]))
    def test_bounds_exceed_canvas(self): self.check_bad(lambda m: m["components"][0].update(bounds=[1532, 1020, 8, 8]))
    def test_foot_nan(self): self.check_bad(lambda m: m["components"][0].update(foot=[math.nan, 500]))
    def test_foot_outside_canvas(self): self.check_bad(lambda m: m["components"][0].update(foot=[1600, 500]))
    def test_pivot_outside_normalized_range(self): self.check_bad(lambda m: m["components"][0].update(pivot=[-0.1, 1]))
    def test_pivot_boolean(self): self.check_bad(lambda m: m["components"][0].update(pivot=[True, 1]))
    def test_footprint_outside_scene(self): self.check_bad(lambda m: m["components"][0].update(footprintCells=[[15, 0]]))
    def test_footprint_fractional_cell(self): self.check_bad(lambda m: m["components"][0].update(footprintCells=[[31.5, 8]]))
    def test_footprint_duplicate(self): self.check_bad(lambda m: m["components"][0].update(footprintCells=[[31, 8], [31, 8]]))
    def test_contact_wrong_type(self): self.check_bad(lambda m: m["components"][0].update(contact="shadow.png"))
    def test_inferred_flag_is_boolean(self): self.check_bad(lambda m: m["components"][0]["repair"].update(inferred="yes"))
    def test_notes_must_be_list_of_strings(self): self.check_bad(lambda m: m["components"][0]["extraction"].update(notes=[{}]))
    def test_absolute_asset_path(self): self.check_bad(lambda m: m["reference"].update(path="/tmp/reference.png"))
    def test_parent_traversal(self): self.check_bad(lambda m: m["components"][0]["sprite"].update(path="../escape.png"))
    def test_nested_parent_traversal(self): self.check_bad(lambda m: m["baseline"].update(path="folder/../../escape.png"))
    def test_windows_absolute_path(self): self.check_bad(lambda m: m["base"].update(path="C:\\escape.png"))
    def test_url_path(self): self.check_bad(lambda m: m["reference"].update(path="https://example.org/art.png"))
    def test_nul_path(self): self.check_bad(lambda m: m["reference"].update(path="art\x00.png"))
    def test_inference_mask_traversal(self): self.check_bad(lambda m: m["base"].update(inferenceMaskPath="../mask.png"))
    def test_repair_mask_traversal(self): self.check_bad(lambda m: m["components"][0]["repair"].update(maskPath="../mask.png"))
    def test_report_output_traversal(self): self.check_bad(lambda m: m["report"].update(path="../report.json"))
    def test_strict_assets_requires_base_directory(self):
        self.assertFalse(validate_manifest(sample_manifest(), require_assets=True)["ok"])


class AdversarialStateTests(unittest.TestCase):
    def test_unknown_requested_ids(self):
        for fn in (remove_components, restore_components):
            with self.assertRaises(ManifestValidationError): fn(sample_manifest(), [], ["missing"])

    def test_unknown_already_removed_ids(self):
        with self.assertRaises(ManifestValidationError): remove_components(sample_manifest(), ["missing"], [])

    def test_nonmutable_removal_and_restore_rejected(self):
        manifest = sample_manifest()
        manifest["components"][0]["mutable"] = False
        for fn in (remove_components, restore_components):
            with self.assertRaises(ManifestValidationError): fn(manifest, [], ["rock-a"])

    def test_illegal_existing_removed_state_is_rejected(self):
        manifest = sample_manifest()
        manifest["components"][0]["contributesToComposite"] = False
        with self.assertRaises(ManifestValidationError): remove_components(manifest, ["rock-a"], [])

    def test_string_is_not_an_id_collection(self):
        with self.assertRaises(ManifestValidationError): remove_components(sample_manifest(), [], "rock-a")

    def test_nonstring_ids_and_none_are_rejected(self):
        for request in ([None], [1], None, {"rock-a": True}):
            with self.subTest(request=request):
                with self.assertRaises(ManifestValidationError): remove_components(sample_manifest(), [], request)

    def test_malformed_manifest_cannot_update_state(self):
        manifest = sample_manifest()
        manifest["components"].append(copy.deepcopy(manifest["components"][0]))
        with self.assertRaises(ManifestValidationError): remove_components(manifest, [], ["rock-a"])


if __name__ == "__main__":
    unittest.main()
