"""Read-only counter-checks for the authored Cudgel import contract."""
import copy
import unittest
from pathlib import Path
from metadata_contract import assert_authored_subset, parse_metadata

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]


def paths(value, prefix=()):
    if isinstance(value, dict):
        for key, child in value.items():
            yield prefix+(key,), child
            yield from paths(child, prefix+(key,))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield prefix+(index,), child
            yield from paths(child, prefix+(index,))


def parent_of(value, path):
    for key in path[:-1]:
        value = value[key]
    return value


class AuthoredMetadataContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.authored_text = (HERE/'native.png.meta').read_text()
        cls.authored = parse_metadata(cls.authored_text)
        cls.imported = parse_metadata((ROOT/'Assets/Resources/Sprites/Live16x24/cudgel.png.meta').read_text())

    def test_real_unity_expanded_metadata_preserves_every_authored_setting(self):
        assert_authored_subset(self.authored, self.imported)

    def test_whitespace_and_mapping_order_are_not_import_changes(self):
        spaced = '\n' + '\n'.join(line+'  ' for line in self.authored_text.splitlines())+'\n'
        assert_authored_subset(self.authored, parse_metadata(spaced))
        reordered = dict(reversed(list(self.authored.items())))
        assert_authored_subset(self.authored, reordered)

    def test_additional_default_mapping_fields_are_allowed(self):
        candidate = copy.deepcopy(self.authored)
        candidate['TextureImporter']['mipmaps']['fadeOut'] = 0
        candidate['TextureImporter']['platformSettings'][0]['ignorePlatformSupport'] = 0
        candidate['TextureImporter']['spriteSheet']['spriteCustomMetadata'] = {'entries': []}
        assert_authored_subset(self.authored, candidate)

    def test_each_authored_scalar_must_keep_its_value(self):
        for path, original in paths(self.authored):
            if isinstance(original, (dict, list)):
                continue
            with self.subTest(path=path):
                candidate = copy.deepcopy(self.imported)
                parent_of(candidate, path)[path[-1]] = (
                    original+1 if isinstance(original, (int, float)) else
                    original+'-changed' if isinstance(original, str) else 'changed-null')
                with self.assertRaises(AssertionError):
                    assert_authored_subset(self.authored, candidate)

    def test_each_authored_mapping_key_must_exist_in_its_original_parent(self):
        for path, _ in paths(self.authored):
            if not isinstance(path[-1], str):
                continue
            with self.subTest(path=path):
                candidate = copy.deepcopy(self.imported)
                del parent_of(candidate, path)[path[-1]]
                with self.assertRaises(AssertionError):
                    assert_authored_subset(self.authored, candidate)

    def test_explicit_empty_geometry_and_binding_containers_remain_empty(self):
        for path, original in paths(self.authored):
            if not isinstance(original, (dict, list)) or original:
                continue
            with self.subTest(path=path):
                candidate = copy.deepcopy(self.imported)
                parent_of(candidate, path)[path[-1]] = {'unexpected': 1} if isinstance(original, dict) else [1]
                with self.assertRaises(AssertionError):
                    assert_authored_subset(self.authored, candidate)

    def test_additional_platform_entries_are_not_treated_as_scalar_defaults(self):
        candidate = copy.deepcopy(self.imported)
        candidate['TextureImporter']['platformSettings'].append({'buildTarget': 'Standalone', 'overridden': 1})
        with self.assertRaises(AssertionError):
            assert_authored_subset(self.authored, candidate)

    def test_boolean_cannot_impersonate_numeric_import_setting(self):
        candidate = copy.deepcopy(self.imported)
        candidate['TextureImporter']['mipmaps']['enableMipMap'] = False
        with self.assertRaises(AssertionError):
            assert_authored_subset(self.authored, candidate)


if __name__ == '__main__':
    unittest.main(verbosity=2)
