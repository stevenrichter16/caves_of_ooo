#!/usr/bin/env python3
"""Bounded shared-furniture build/smoke restoration including observed window preferences.

Adapted from /tmp/coo-shared-storage-window-restore.py. The fresh Mac launch introduced
exactly UnitySelectMonitor and Screenmanager Window Position X/Y alongside the
existing LastGameID key; only these named preference additions are permitted. Root supplies a fresh snapshot,
reviewed current hashes, source SHA and stopped-Editor assertion. --apply is
explicit; this helper does not build, run Unity, clean caches or delete saves.
"""
from pathlib import Path
import argparse, datetime, hashlib, importlib.util, json, plistlib, re, shutil, subprocess, sys

ROOT = Path('/Users/steven/caves-of-ooo')
SETTINGS = {'Assets/Settings/UniversalRP.asset', 'Assets/UniversalRenderPipelineGlobalSettings.asset',
            'ProjectSettings/ProjectSettings.asset', 'ProjectSettings/UnityConnectSettings.asset'}
OWNED_METADATA = {'Assets/Scripts/Presentation/Rendering/FlatPixelSharedFurnitureCatalog.cs.meta',
                  'Assets/Tests/EditMode/FlatPixelSharedFurnitureTests.cs.meta'}
EDITOR_DOMAIN = 'unity.DefaultCompany.caves-of-ooo'
PLAYER_DOMAIN = 'com.DefaultCompany.2D-URP'

def logical(domain):
    return plistlib.loads(subprocess.check_output(['defaults', 'export', domain, '-']))

def logical_hash(value):
    return hashlib.sha256(plistlib.dumps(value, sort_keys=True)).hexdigest()

def main():
    if sys.flags.optimize: raise RuntimeError('Run without -O; preservation assertions are required.')
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source-commit', required=True)
    p.add_argument('--snapshot', type=Path, required=True)
    p.add_argument('--reviewed', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--receipt', type=Path, required=True)
    p.add_argument('--confirm-editor-idle', action='store_true')
    p.add_argument('--apply', action='store_true')
    a = p.parse_args()
    assert a.confirm_editor_idle, 'Root must first verify stopped Play, finished isolation and idle native runner.'
    assert re.fullmatch(r'[0-9a-f]{40}', a.source_commit)
    for path in (a.snapshot, a.reviewed):
        assert path.is_absolute() and path.resolve(strict=True) == path
    assert a.snapshot.is_dir() and a.reviewed.is_file()
    for path in (a.output, a.receipt):
        assert path.is_absolute() and not path.exists() and not path.is_symlink()
        assert path.parent.resolve(strict=True) == path.parent
    assert not a.output.is_relative_to(ROOT) and not a.output.is_relative_to(a.snapshot)
    assert a.receipt.parent == ROOT/'Docs/Verification/Live16x24'
    assert a.receipt.name == 'packaged-shared-furniture-restoration.json'
    before = json.loads((a.snapshot/'before.json').read_text())
    reviewed = json.loads(a.reviewed.read_text())
    assert before['source'] == reviewed['source_commit'] == a.source_commit
    assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip() == a.source_commit
    for name in ('CavesOfOoo', 'caves-of-ooo'):
        assert subprocess.run(['pgrep', '-x', name], stdout=subprocess.DEVNULL).returncode == 1
    spec = importlib.util.spec_from_file_location('snapshot', '/tmp/coo-sill-build-snapshot.py')
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
    expected = reviewed['generated_hashes']
    assert isinstance(expected, dict) and set(expected) <= SETTINGS | OWNED_METADATA
    assert all(isinstance(h, str) and re.fullmatch(r'[0-9a-f]{64}', h) for h in expected.values())
    current = {name: m.state(ROOT/name) for name in before['tracked_files']}
    changed = {name for name, state in current.items() if state != before['tracked_files'][name]}
    assert changed == set(expected), ('Unexpected or unreviewed tracked change', sorted(changed ^ set(expected)))
    originals = {}
    for name, generated_hash in expected.items():
        assert name not in before['dirty_tracked_paths'], 'Do not restore preexisting dirty work.'
        state = current[name]; baseline = before['tracked_files'][name]
        assert state['type'] == baseline['type'] == 'file' and state['mode'] == baseline['mode']
        assert state['sha256'] == generated_hash
        original = subprocess.check_output(['git', 'show', a.source_commit+':'+name], cwd=ROOT)
        assert hashlib.sha256(original).hexdigest() == before['tracked_hashes'][name]
        if name in OWNED_METADATA:
            # Only trailing horizontal whitespace may differ. GUIDs, tokens,
            # line endings, importer defaults and meaningful content must agree.
            normalize = lambda data: re.sub(rb'[ \t]+(?=\r?$)', b'', data, flags=re.M)
            assert normalize(original) == normalize((ROOT/name).read_bytes())
        originals[name] = original
    assert subprocess.check_output(['git', 'ls-files', '--stage', '-z'], cwd=ROOT) == (a.snapshot/'git-index.stage-z').read_bytes()
    editor_before = plistlib.loads((a.snapshot/'preferences'/(EDITOR_DOMAIN+'.plist')).read_bytes())
    player_before = plistlib.loads((a.snapshot/'preferences'/(PLAYER_DOMAIN+'.plist')).read_bytes())
    assert logical(EDITOR_DOMAIN) == editor_before
    editor_saves = before['normal_saves']['normal-editor']; player_saves = before['normal_saves']['normal-standalone']
    assert m.tree(Path(editor_saves['path'])) == editor_saves['before']
    assert player_saves['before']['root']['type'] == 'absent', 'This narrow helper only archives a newly introduced standalone test save tree.'
    save = Path(player_saves['path'])
    assert m.tree(save) == reviewed['standalone_save_tree']
    assert reviewed['standalone_save_tree']['root']['type'] == 'directory'
    player_current = logical(PLAYER_DOMAIN)
    assert logical_hash(player_current) == reviewed['standalone_preferences_sha256']
    extras = set(player_current) - set(player_before)
    owned_extras = reviewed['owned_introduced_preference_keys']
    assert isinstance(owned_extras, list) and len(owned_extras) == len(set(owned_extras))
    assert extras == set(owned_extras)
    assert all(isinstance(key, str) and re.fullmatch(r'(?:CavesOfOoo\.LastGameID|LastGameID(?:_h[0-9]+)?|UnitySelectMonitor|Screenmanager Window Position X|Screenmanager Window Position Y)', key) for key in extras)
    for item in before['previous_current'].values():
        assert (m.tree(Path(item['path'])) if 'tree' in item else m.state(Path(item['path']))) == item.get('tree', item.get('state'))
    result = {'source_commit': a.source_commit, 'reviewed_manifest_sha256': m.sha(a.reviewed),
              'tracked_files': len(before['tracked_files']), 'dirty_tracked_files_preserved': len(before['dirty_tracked_paths']),
              'build_serialization_to_restore': sorted(expected), 'prior_builds_preserved': True,
              'editor_preferences_and_saves_unchanged': True, 'normal_standalone_saves_originally_absent': True,
              'test_save_archive': str(a.output/'standalone-session-Saves'),
              'preference_extra_key_count': len(extras), 'cache_deletion': False}
    if not a.apply:
        print(json.dumps(dict(result, dry_run=True, no_files_written=True), indent=2)); return
    a.output.mkdir()
    for name, original in originals.items():
        backup = a.output/'generated-backups'/name; backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes((ROOT/name).read_bytes()); assert m.sha(backup) == expected[name]
        (ROOT/name).write_bytes(original)
    save.rename(a.output/'standalone-session-Saves')
    assert m.tree(a.output/'standalone-session-Saves') == reviewed['standalone_save_tree']
    (a.output/'preferences-before.plist').write_bytes(plistlib.dumps(player_current, sort_keys=True))
    subprocess.run(['defaults', 'import', PLAYER_DOMAIN, str(a.snapshot/'preferences'/(PLAYER_DOMAIN+'.plist'))], check=True)
    merged = logical(PLAYER_DOMAIN)
    assert set(merged) - set(player_before) <= extras
    for key in sorted(set(merged) - set(player_before)):
        subprocess.run(['defaults', 'delete', PLAYER_DOMAIN, key], check=True)
    assert logical(PLAYER_DOMAIN) == player_before and logical(EDITOR_DOMAIN) == editor_before
    assert all(m.state(ROOT/name) == state for name, state in before['tracked_files'].items())
    assert m.tree(Path(editor_saves['path'])) == editor_saves['before'] and not save.exists()
    for item in before['previous_current'].values():
        assert (m.tree(Path(item['path'])) if 'tree' in item else m.state(Path(item['path']))) == item.get('tree', item.get('state'))
    result.update(at_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), applied=True,
                  all_tracked_snapshot_objects_equal=True, normal_saves_and_preferences_restored=True,
                  free_bytes=shutil.disk_usage(ROOT).free,
                  limits='Root supplies Editor-idle assertion; no Unity/build/player invocation or cache cleanup. No unrelated file restored.')
    with a.receipt.open('x') as f: f.write(json.dumps(result, indent=2)+'\n')
    print(json.dumps(result, indent=2))

if __name__ == '__main__': main()
