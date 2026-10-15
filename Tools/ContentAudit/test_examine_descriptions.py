"""Counter-checks for the offline prose audit, independent of Unity."""
import copy
import json
import unittest

import examine_descriptions as audit


def blueprint(name, params, parent=None):
    result = {"Name": name, "Parts": [{"Name": "Examinable", "Params": [
        {"Key": key, "Value": value} for key, value in params]}]}
    if parent:
        result["Inherits"] = parent
    return result


class ExamineDescriptionAuditTests(unittest.TestCase):
    def test_parent_prose_is_inherited_but_child_alias_overrides(self):
        parent = blueprint("Base", [("Text", "parent text")])
        inherited = blueprint("Inherited", [], "Base")
        child = blueprint("Child", [("Description", "child prose")], "Base")
        baked = audit.resolve({x["Name"]: x for x in (parent, inherited, child)})
        self.assertEqual("parent text", audit.text_of(baked["Inherited"]))
        self.assertEqual("child prose", audit.text_of(baked["Child"]))
        self.assertEqual("parent text", audit.text_of(baked["Base"]))

    def test_explicit_empty_last_alias_clears_earlier_prose(self):
        parts = {"Examinable": {"Text": "earlier prose", "Description": ""}}
        self.assertEqual("", audit.text_of(parts))
        self.assertEqual("earlier prose", audit.text_of({"Examinable": {"Text": "earlier prose"}}))

    def test_cycle_is_rejected_and_unrelated_tree_resolves(self):
        self.assertEqual("", audit.text_of(audit.resolve({"A": blueprint("A", [])})["A"]))
        with self.assertRaisesRegex(ValueError, "Inheritance cycle"):
            audit.resolve({"A": blueprint("A", [], "B"), "B": blueprint("B", [], "A")})

    def test_prose_only_receipt_accepts_new_part_but_rejects_material_change(self):
        before = {"A": {"Name": "A", "Parts": [{"Name": "Material", "Params": [
            {"Key": "MaterialID", "Value": "Wood"}]}]}}
        after = copy.deepcopy(before)
        after["A"]["Parts"].extend(blueprint("A", [("Description", "New prose.")])["Parts"])
        self.assertTrue(audit.compare(before, after, {"A"})["allowed_fields_only"])
        after["A"]["Parts"][0]["Params"][0]["Value"] = "Steel"
        with self.assertRaisesRegex(AssertionError, "Non-description field"):
            audit.compare(before, after, {"A"})

    def test_receipt_rejects_an_unplanned_blueprint_edit(self):
        before = {"A": blueprint("A", []), "B": blueprint("B", [])}
        after = copy.deepcopy(before)
        after["A"] = blueprint("A", [("Description", "New prose.")])
        self.assertEqual(1, audit.compare(before, after, {"A"})["unchanged_count"])
        after["B"] = blueprint("B", [("Description", "Unexpected prose.")])
        with self.assertRaisesRegex(AssertionError, "differs from manifest"):
            audit.compare(before, after, {"A"})

    def test_actual_authored_cohort_has_exact_leaf_copy(self):
        data = audit.objects(audit.DEFAULT_OBJECTS)
        baked = audit.resolve(data)
        manifest = json.loads(audit.DEFAULT_MANIFEST.read_text())
        self.assertGreaterEqual(len(manifest), 50)
        for name, target in manifest.items():
            with self.subTest(blueprint=name):
                self.assertEqual(target["text"], audit.text_of(baked[name]))
                self.assertTrue(target["text"].isascii())
                self.assertLessEqual(len(target["text"]), 240)
                self.assertFalse(any(x.get("Inherits") == name for x in data.values()))

    def test_abstract_templates_remain_blank_and_existing_prose_survives(self):
        baked = audit.resolve(audit.objects(audit.DEFAULT_OBJECTS))
        for name in ("PhysicalObject", "Terrain", "Item", "Creature", "MeleeWeapon", "TonicItem"):
            with self.subTest(blueprint=name):
                self.assertFalse(audit.text_of(baked[name]))
        for name in ("Signpost", "Chest", "BreacherCleaver", "BerryBush"):
            with self.subTest(blueprint=name):
                self.assertTrue(audit.text_of(baked[name]))


if __name__ == "__main__":
    unittest.main()
