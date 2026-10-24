#!/usr/bin/env python3
"""Check this project's saved layer contract without Unity or YAML dependencies.

This deliberately validates the small authored TagManager shape, not Unity's
native parser. Passing it cannot establish successful editor startup.
"""
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
PREFIX = (
    "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!78 &1\n"
    "TagManager:\n  serializedVersion: 3\n  tags: []\n  layers:\n"
)
SUFFIX = (
    "  m_SortingLayers:\n  - name: Default\n    uniqueID: 0\n"
    "    locked: 0\n  m_RenderingLayers:\n  - Default\n"
)
NAMES = {
    0: "Default", 1: "TransparentFX", 2: "Ignore Raycast",
    4: "Water", 5: "UI", 8: "World", 9: "Sidebar",
    10: "PopupOverlay", 11: "Hotbar",
}


def authored_document(slots=None):
    if slots is None:
        slots = [NAMES.get(index, "") for index in range(32)]
    return PREFIX + "".join("  - " + name + "\n" for name in slots) + SUFFIX


def validate_saved_contract(source):
    """Raise ValueError on changed slot shape, indices or retained metadata."""
    if not source.startswith(PREFIX) or not source.endswith(SUFFIX):
        raise ValueError("TagManager header/sorting/rendering metadata changed")
    rows = source[len(PREFIX):-len(SUFFIX)].splitlines()
    if len(rows) != 32:
        raise ValueError(f"Expected 32 layer slots, found {len(rows)}")
    if any(not row.startswith("  - ") for row in rows):
        raise ValueError("Expected canonical authored sequence delimiter '- '")
    names = {index: row[4:] for index, row in enumerate(rows) if row[4:]}
    if names != NAMES:
        raise ValueError(f"Named layer indices changed: {names}")


class TagManagerContractTests(unittest.TestCase):
    def test_saved_project_contract(self):
        validate_saved_contract((ROOT / "ProjectSettings/TagManager.asset").read_text())

    def test_named_gameplay_indices_match_runtime_constants(self):
        source = (ROOT / "Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs").read_text()
        for name, index in (("Default", 0), ("World", 8), ("Sidebar", 9),
                            ("PopupOverlay", 10), ("Hotbar", 11)):
            match = re.search(r"public const int " + name + r"Layer\s*=\s*(\d+);", source)
            self.assertIsNotNone(match, name)
            self.assertEqual(index, int(match.group(1)), name)
            self.assertEqual(name, NAMES[index])

    def test_independent_valid_control(self):
        validate_saved_contract(authored_document())

    def test_rejects_both_historical_short_tables(self):
        slots = [NAMES.get(index, "") for index in range(32)]
        validate_saved_contract(authored_document(slots))
        for count in (30, 31):
            with self.subTest(count=count), self.assertRaisesRegex(ValueError, "32 layer slots"):
                validate_saved_contract(authored_document(slots[:count]))

    def test_rejects_name_shift_even_with_32_slots(self):
        slots = [NAMES.get(index, "") for index in range(32)]
        validate_saved_contract(authored_document(slots))
        slots[8], slots[12] = slots[12], slots[8]
        with self.assertRaisesRegex(ValueError, "indices changed"):
            validate_saved_contract(authored_document(slots))

    def test_rejects_changed_sorting_metadata(self):
        control = authored_document()
        validate_saved_contract(control)
        with self.assertRaisesRegex(ValueError, "metadata changed"):
            validate_saved_contract(control.replace("uniqueID: 0", "uniqueID: 1"))

    def test_preserves_canonical_empty_slot_format(self):
        control = authored_document()
        validate_saved_contract(control)
        # This is an authored-format rule, not a claim that bare '-' is invalid YAML.
        with self.assertRaisesRegex(ValueError, "sequence delimiter"):
            validate_saved_contract(control.replace("  - \n", "  -\n", 1))


if __name__ == "__main__":
    unittest.main(verbosity=2)
