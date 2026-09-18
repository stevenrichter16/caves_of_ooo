"""Offline contract tests: run with python3 -m unittest ArtTools.test_felling_scene."""

import json
import hashlib
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from ArtTools import felling_scene as scene


def layout_fixture():
    return {
        "schemaVersion": 1,
        "id": "felling-site",
        "reference": {"path": "reference.png", "width": 1536, "height": 1024},
        "grid": {"zoneWidth": 80, "zoneHeight": 25, "regionOrigin": [16, 0],
                 "regionSize": [48, 25], "artPixelsPerCell": 32,
                 "imageGroundOrigin": [0, 224]},
        "spawn": [40, 20],
        "entrance": [40, 24],
        "sterilePositions": [
            {"id": "strike-1", "cell": [36, 10]},
            {"id": "strike-2", "cell": [42, 10]},
            {"id": "strike-3", "cell": [33, 13]},
            {"id": "strike-4", "cell": [45, 13]},
            {"id": "strike-5", "cell": [36, 17]},
            {"id": "strike-6", "cell": [42, 17]},
        ],
        "seventh": {"cell": [40, 8]},
        "walkablePolygon": [[0, 224], [1536, 224], [1536, 1024], [0, 1024]],
        "blockers": [],
        "occluders": [],
        "effects": [],
        "assets": [{"id": "reference", "path": "reference.png", "role": "reference",
                    "status": "reference"}],
    }


class CoordinateTests(unittest.TestCase):
    def setUp(self):
        self.grid = layout_fixture()["grid"]

    def test_coordinate_roundtrip_matches_game_grid(self):
        self.assertEqual(scene.image_to_cell([768, 864], self.grid), (40, 20))
        self.assertEqual(scene.cell_to_image([40, 20], self.grid), (784.0, 880.0))
        for x in range(16, 64):
            for y in range(25):
                self.assertEqual(scene.image_to_cell(scene.cell_to_image([x, y], self.grid), self.grid), (x, y))

    def test_art_overhang_uses_floor_and_is_not_playable(self):
        self.assertEqual(scene.image_to_cell([768, 223], self.grid), (40, -1))
        self.assertFalse(scene.cell_in_region((40, -1), self.grid))
        self.assertFalse(scene.cell_in_region((15, 0), self.grid))
        self.assertFalse(scene.cell_in_region((64, 0), self.grid))

    def test_polygon_includes_boundaries(self):
        polygon = [[0, 0], [10, 0], [10, 10], [0, 10]]
        self.assertTrue(scene.point_in_polygon((0, 5), polygon))
        self.assertTrue(scene.point_in_polygon((5, 5), polygon))
        self.assertFalse(scene.point_in_polygon((-0.1, 5), polygon))


class ValidationTests(unittest.TestCase):
    def assert_bad(self, layout, message):
        with self.assertRaisesRegex(scene.LayoutError, message):
            scene.validate_layout(layout)

    def test_valid_layout_reports_paths_and_unverified_geometry(self):
        report = scene.validate_layout(layout_fixture())
        self.assertEqual(report["structuralValidation"], "passed")
        self.assertEqual(report["collisionGeometryStatus"], "proposed; requires Unity visual and gameplay QA")
        self.assertEqual(report["walkableCellCount"], 1200)
        self.assertEqual(len(report["routes"]), 8)
        self.assertTrue(all(route["reachable"] for route in report["routes"]))

    def test_exactly_six_distinct_strikes(self):
        layout = layout_fixture()
        layout["sterilePositions"].pop()
        self.assert_bad(layout, "exactly six")
        layout = layout_fixture()
        layout["sterilePositions"][1]["cell"] = layout["sterilePositions"][0]["cell"]
        self.assert_bad(layout, "distinct")

    def test_seventh_is_an_absence_at_a_separate_cell(self):
        layout = layout_fixture()
        layout["seventh"]["cell"] = layout["sterilePositions"][0]["cell"]
        self.assert_bad(layout, "distinct")

    def test_marker_outside_art_region_is_rejected(self):
        layout = layout_fixture()
        layout["spawn"] = [15, 20]
        self.assert_bad(layout, "region")
        layout["spawn"] = [40, -1]
        self.assert_bad(layout, "zone")

    def test_region_must_fit_zone_and_reference(self):
        layout = layout_fixture()
        layout["grid"]["regionOrigin"] = [40, 0]
        self.assert_bad(layout, "zone")
        layout = layout_fixture()
        layout["grid"]["imageGroundOrigin"] = [0, 225]
        self.assert_bad(layout, "reference")

    def test_blocker_at_cell_center_prevents_occupancy(self):
        layout = layout_fixture()
        layout["blockers"] = [{"id": "root", "kind": "root", "blocksSight": True,
                               "polygon": [[760, 850], [810, 850], [810, 900], [760, 900]]}]
        self.assert_bad(layout, "spawn.*blocked")

    def test_occluder_is_visual_and_does_not_block(self):
        layout = layout_fixture()
        layout["occluders"] = [{"id": "canopy", "sortFootY": 950,
                                "polygon": [[0, 0], [1536, 0], [1536, 1024], [0, 1024]]}]
        self.assertEqual(scene.validate_layout(layout)["walkableCellCount"], 1200)

    def test_malformed_polygons_are_rejected(self):
        for polygon in ([[0, 0], [1, 1]], [[0, 0], [1, 1], [2, 2]],
                        [[0, 0], [float("nan"), 1], [1, 0]],
                        [[0, 0], [float("inf"), 1], [1, 0]],
                        [[0, 0], [10, 10], [0, 10], [10, 0]]):
            with self.subTest(polygon=polygon):
                layout = layout_fixture()
                layout["walkablePolygon"] = polygon
                self.assert_bad(layout, "polygon|Polygon")

    def test_self_intersecting_polygon_with_nonzero_area_is_rejected(self):
        layout = layout_fixture()
        layout["walkablePolygon"] = [[0, 0], [10, 10], [0, 10], [8, 0]]
        self.assert_bad(layout, "self-intersects")

    def test_explicit_polygon_closing_vertex_is_accepted(self):
        layout = layout_fixture()
        layout["walkablePolygon"].append(layout["walkablePolygon"][0])
        self.assertEqual(scene.validate_layout(layout)["walkableCellCount"], 1200)

    def test_barrier_prevents_bfs_reachability(self):
        layout = layout_fixture()
        layout["blockers"] = [{"id": "river", "kind": "water", "blocksSight": False,
                               "polygon": [[0, 832], [1536, 832], [1536, 864], [0, 864]]}]
        self.assert_bad(layout, "unreachable")

    def test_no_diagonal_corner_cutting(self):
        walkable = {(0, 0), (1, 1), (2, 1)}
        self.assertNotIn((1, 1), scene.reachable_cells((0, 0), walkable))
        walkable.update({(0, 1), (1, 0)})
        self.assertIn((2, 1), scene.reachable_cells((0, 0), walkable))

    def test_malformed_schema_fails_with_layout_error(self):
        for mutation in (lambda x: x.pop("grid"),
                         lambda x: x["grid"].update(artPixelsPerCell=0),
                         lambda x: x["spawn"].__setitem__(0, 40.2),
                         lambda x: x.update(schemaVersion=2),
                         lambda x: x.update(assets="bad")):
            layout = layout_fixture()
            mutation(layout)
            with self.assertRaises(scene.LayoutError):
                scene.validate_layout(layout)

    def test_asset_path_traversal_rejected_even_when_assets_optional(self):
        for path in ("../secret.png", "/tmp/secret.png", "a/../../secret.png", "C:\\secret.png",
                     "..\\secret.png", "https://example.com/file.png", "a\x00.png"):
            layout = layout_fixture()
            layout["assets"][0]["path"] = path
            with self.subTest(path=path):
                self.assert_bad(layout, "path")

    def test_existing_symlink_outside_base_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory) / "art"
            base.mkdir()
            (Path(directory) / "outside.png").write_bytes(b"x")
            (base / "reference.png").symlink_to(Path(directory) / "outside.png")
            with self.assertRaisesRegex(scene.LayoutError, "path"):
                scene.validate_layout(layout_fixture(), base_dir=base)

    def test_missing_or_pending_assets_fail_only_when_required(self):
        with tempfile.TemporaryDirectory() as directory:
            self.assertEqual(scene.validate_layout(layout_fixture(), base_dir=directory)["structuralValidation"], "passed")
            with self.assertRaisesRegex(scene.LayoutError, "missing"):
                scene.validate_layout(layout_fixture(), base_dir=directory, require_assets=True)
            layout = layout_fixture()
            # Non-image fixture files exercise presence/status, without making raster assets.
            (Path(directory) / "fixture.dat").write_text("fixture", encoding="utf-8")
            layout["reference"]["path"] = "fixture.dat"
            layout["assets"][0].update(path="fixture.dat", status="pending")
            self.assertEqual(scene.validate_layout(layout, base_dir=directory)["structuralValidation"], "passed")
            with self.assertRaisesRegex(scene.LayoutError, "pending"):
                scene.validate_layout(layout, base_dir=directory, require_assets=True)

    def test_duplicate_identifiers_are_rejected(self):
        layout = layout_fixture()
        layout["sterilePositions"][1]["id"] = "strike-1"
        self.assert_bad(layout, "duplicate")


class ExportTests(unittest.TestCase):
    def test_cli_exports_only_json_and_returns_nonzero_on_invalid_input(self):
        script = Path(__file__).with_name("felling_scene.py")
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "layout.json"
            source.write_text(json.dumps(layout_fixture()), encoding="utf-8")
            destination = Path(directory) / "export"
            result = subprocess.run([sys.executable, str(script), "export", str(source), "--out", str(destination)],
                                    capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual({p.name for p in destination.iterdir()},
                             {"occupancy.json", "geometry-report.json", "unity-layout.json"})
            occupancy = json.loads((destination / "occupancy.json").read_text())
            self.assertEqual(len(occupancy["rows"]), 25)
            self.assertTrue(all(len(row) == 48 for row in occupancy["rows"]))
            unity = json.loads((destination / "unity-layout.json").read_text())
            self.assertEqual(unity["spawn"]["cell"], [40, 20])
            self.assertEqual(unity["spawn"]["imageCenter"], [784.0, 880.0])
            source.write_text('{"schemaVersion": 999}', encoding="utf-8")
            failure = subprocess.run([sys.executable, str(script), "validate", str(source)], capture_output=True, text=True)
            self.assertNotEqual(failure.returncode, 0)
            self.assertIn("ERROR:", failure.stderr)

    def test_export_preserves_source_fields_and_records_provenance(self):
        layout = layout_fixture()
        layout["camera"] = {"worldCenter": [40, 16], "orthographicSize": 16}
        layout["policies"] = {"visibility": "requires runtime QA"}
        layout["sterilePositions"][0]["sourceCenter"] = [670, 553]
        layout["sterilePositions"][0]["radiusPixels"] = 48
        layout["seventh"].update(id="seventh-absence", sourceCenter=[768, 503])
        layout["customHandoffField"] = "preserve optional authored information"
        with tempfile.TemporaryDirectory() as directory:
            scene.export_layout(layout, directory)
            exported = json.loads((Path(directory) / "unity-layout.json").read_text())
            for key in ("reference", "camera", "policies", "customHandoffField"):
                self.assertEqual(exported[key], layout[key])
            self.assertEqual(exported["sterilePositions"][0]["sourceCenter"], [670, 553])
            self.assertEqual(exported["sterilePositions"][0]["radiusPixels"], 48)
            self.assertEqual(exported["seventh"]["id"], "seventh-absence")
            self.assertEqual(exported["seventh"]["sourceCenter"], [768, 503])
            canonical = json.dumps(layout, sort_keys=True, separators=(",", ":"), allow_nan=False).encode("utf-8")
            self.assertEqual(exported["provenance"]["sourceLayoutCanonicalSha256"], hashlib.sha256(canonical).hexdigest())


class AssetMetadataTests(unittest.TestCase):
    """Inspect existing art read-only; no fixture images are drawn or resaved."""

    base = Path(__file__).resolve().parents[1] / "ArtSource" / "FellingSite"

    def test_existing_png_is_fully_decoded_and_hashed(self):
        report = scene.validate_layout(layout_fixture(), base_dir=self.base)
        item = report["assets"][0]
        self.assertTrue(item["decoded"])
        self.assertEqual(item["sha256"], hashlib.sha256((self.base / "reference.png").read_bytes()).hexdigest())

    def test_truncated_png_header_does_not_pass_decode_validation(self):
        with tempfile.TemporaryDirectory() as directory:
            source_bytes = (self.base / "reference.png").read_bytes()
            (Path(directory) / "reference.png").write_bytes(source_bytes[:len(source_bytes) // 2])
            with self.assertRaisesRegex(scene.LayoutError, "decode|PNG"):
                scene.validate_layout(layout_fixture(), base_dir=directory)

    def test_background_dimensions_must_match_reference(self):
        for role in ("background", "clean-plate"):
            layout = layout_fixture()
            layout["assets"].append({"id": "background", "path": "player-sheet.png", "role": role, "status": "generated"})
            with self.subTest(role=role), self.assertRaisesRegex(scene.LayoutError, "dimensions.*reference"):
                scene.validate_layout(layout, base_dir=self.base)

    def test_actor_sheet_frame_geometry_matches_image(self):
        layout = layout_fixture()
        actor = {"id": "player", "path": "player-sheet.png", "role": "actor", "status": "reference",
                 "frameWidth": 16, "frameHeight": 24, "columns": 4, "rows": 16}
        layout["assets"].append(actor)
        self.assertEqual(scene.validate_layout(layout, base_dir=self.base)["structuralValidation"], "passed")
        actor["columns"] = 5
        with self.assertRaisesRegex(scene.LayoutError, "frame geometry"):
            scene.validate_layout(layout, base_dir=self.base)
        actor.pop("columns")
        with self.assertRaisesRegex(scene.LayoutError, "frame geometry"):
            scene.validate_layout(layout, base_dir=self.base)

    def test_transparency_requirement_reads_actual_alpha_pixels(self):
        layout = layout_fixture()
        transparent = {"id": "cutout", "path": "player-sheet.png", "role": "prop-study", "status": "generated",
                       "requireTransparency": True}
        layout["assets"].append(transparent)
        metadata = scene.validate_layout(layout, base_dir=self.base)["assets"][-1]
        self.assertEqual(metadata["alphaExtrema"], [0, 255])
        self.assertGreater(metadata["transparentPixelCount"], 0)
        self.assertGreater(metadata["opaquePixelCount"], 0)
        transparent["path"] = "reference.png"
        with self.assertRaisesRegex(scene.LayoutError, "transparent.*opaque"):
            scene.validate_layout(layout, base_dir=self.base)

    def test_occluder_owner_must_reference_existing_blocker(self):
        layout = layout_fixture()
        layout["blockers"] = [{"id": "root-foot", "kind": "root", "blocksSight": False,
                               "polygon": [[0, 224], [32, 224], [32, 256], [0, 256]]}]
        layout["occluders"] = [{"id": "root-crown", "polygon": [[0, 0], [32, 0], [32, 256], [0, 256]],
                                "sortFootY": 256, "ownerId": "root-foot"}]
        self.assertEqual(scene.validate_layout(layout)["structuralValidation"], "passed")
        layout["occluders"][0]["ownerId"] = "missing-foot"
        with self.assertRaisesRegex(scene.LayoutError, "ownerId.*blocker"):
            scene.validate_layout(layout)


if __name__ == "__main__":
    unittest.main()
