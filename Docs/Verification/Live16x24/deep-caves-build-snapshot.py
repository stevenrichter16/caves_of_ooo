#!/usr/bin/env python3
"""Read project/player state and create a new scratch protection snapshot.

Writes only the new --output directory; never moves, restores or deletes source,
saves, preferences or builds. Run after root has verified normal idle Editor
state and finished save isolation. Existing saves are copied and hash-verified;
old builds are hashed, not copied. No Unity calls or automatic restoration.
"""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import os
import plistlib
import re
import shutil
import subprocess
import sys

REPO = Path('/Users/steven/caves-of-ooo')
EVIDENCE = REPO / 'Docs/Verification/Live16x24'
DOMAINS = ['unity.DefaultCompany.caves-of-ooo', 'com.DefaultCompany.2D-URP']
SAVES = {
    'normal-editor': Path.home() / 'Library/Application Support/DefaultCompany/caves-of-ooo/Saves',
    'normal-standalone': Path.home() / 'Library/Application Support/com.DefaultCompany.2D-URP/Saves',
}


def git(*args):
    return subprocess.check_output(['git', '-C', str(REPO), *args])


def sha(path):
    before = path.stat()
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    after = path.stat()
    assert (before.st_size, before.st_mtime_ns, before.st_ino) == (after.st_size, after.st_mtime_ns, after.st_ino), f'Changed during read: {path}'
    return h.hexdigest()


def state(path):
    if path.is_symlink():
        return {'type': 'symlink', 'target': os.readlink(path)}
    if not path.exists():
        return {'type': 'absent'}
    if path.is_dir():
        return {'type': 'directory', 'mode': oct(path.stat().st_mode & 0o777)}
    assert path.is_file(), f'Unsupported filesystem object: {path}'
    return {'type': 'file', 'bytes': path.stat().st_size,
            'mode': oct(path.stat().st_mode & 0o777), 'sha256': sha(path)}


def tree(path):
    root = state(path)
    if root['type'] == 'absent':
        return {'root': root, 'entries': {}}
    assert root['type'] == 'directory', f'Expected literal directory, not a link: {path}'
    entries = {}
    for parent, dirs, files in os.walk(path, followlinks=False):
        dirs.sort(); files.sort()
        for name in dirs + files:
            child = Path(parent) / name
            entries[child.relative_to(path).as_posix()] = state(child)
    return {'root': root, 'entries': dict(sorted(entries.items()))}


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as f:
        f.write(data)


def json_bytes(value):
    return (json.dumps(value, indent=2) + '\n').encode()


def package_receipt(path):
    report = json.loads(path.read_text())
    payload = report.get('result', report)
    return payload.get('data', payload)


def main():
    if sys.flags.optimize:
        raise RuntimeError('Run without -O; preservation assertions are required.')
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source-commit', required=True)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--confirm-editor-idle', action='store_true',
                   help='Operator confirms stopped Play, finished isolation and ordinary preference/save state.')
    p.add_argument('--previous-linux-receipt', type=Path,
                   default=EVIDENCE / 'packaged-playable-zoom-linux-archive.json')
    p.add_argument('--previous-mac-receipt', type=Path,
                   default=EVIDENCE / 'packaged-playable-zoom-mac-build.json')
    args = p.parse_args()
    assert args.confirm_editor_idle, 'First verify stopped Play, completed native isolation and normal preferences with the root Unity owner.'
    assert re.fullmatch(r'[0-9a-f]{40}', args.source_commit)
    assert git('rev-parse', 'HEAD').decode().strip() == args.source_commit
    for name in ('CavesOfOoo', 'caves-of-ooo'):
        running = subprocess.run(['pgrep', '-x', name], capture_output=True)
        assert running.returncode == 1, 'A standalone player may still be writing saves; close it first.'
    output = args.output.absolute()
    assert output.is_absolute() and not output.exists() and not output.is_symlink(), 'Use a fresh snapshot directory.'
    assert not output.resolve().is_relative_to(REPO.resolve()), 'Protection snapshot must be outside the project.'
    assert output.parent.is_dir()

    # Preflight preferences from CFPreferences, not just potentially stale plist
    # bytes. Exports are read-only and their complete values stay in scratch.
    exports = {}
    for domain in DOMAINS:
        result = subprocess.run(['defaults', 'export', domain, '-'], capture_output=True)
        if result.returncode:
            error = result.stderr.decode(errors='replace')
            assert 'does not exist' in error.lower() or 'not found' in error.lower(), error
            exports[domain] = None
        else:
            plistlib.loads(result.stdout)
            exports[domain] = result.stdout

    previous_linux = json.loads(args.previous_linux_receipt.read_text())
    archive = REPO / previous_linux['archive']
    assert archive.is_file() and not archive.is_symlink()
    archive_state = state(archive)
    assert archive_state['sha256'] == previous_linux['archive_sha256'], 'Previous transfer archive no longer matches its accepted receipt.'
    linux_tree = archive.parent / 'CavesOfOoo-current'
    previous_mac = package_receipt(args.previous_mac_receipt)
    assert previous_mac.get('result') == 'succeeded' and previous_mac.get('platform') == 'StandaloneOSX'
    mac_tree = Path(previous_mac['output_path'])
    if not mac_tree.is_absolute(): mac_tree = REPO / mac_tree
    assert linux_tree.is_dir() and mac_tree.is_dir()
    status = git('status', '--porcelain=v1', '--untracked-files=no', '-z')
    paths = sorted(set(n.decode() for n in git('ls-files', '-z').split(b'\0') if n))
    changed = sorted(set(n.decode() for n in git('diff', '--name-only', '-z', 'HEAD').split(b'\0') if n))

    output.mkdir(exist_ok=False)
    write(output / 'git-status.porcelain-z', status)
    write(output / 'git-index.stage-z', git('ls-files', '--stage', '-z'))
    write(output / 'tracked-before.diff', git('diff', '--binary', 'HEAD'))
    tracked = {name: state(REPO / name) for name in paths}
    # Normally root's frozen source is clean. Preserve actual preexisting dirty
    # bytes as well so later review never assumes git-show is their backup.
    for name in changed:
        original = REPO / name
        if original.is_file() and not original.is_symlink():
            write(output / 'tracked-dirty-backups' / name, original.read_bytes())
            assert sha(output / 'tracked-dirty-backups' / name) == tracked[name]['sha256']

    preferences = {}
    for domain, data in exports.items():
        physical = Path.home() / 'Library/Preferences' / (domain + '.plist')
        row = {'logical_domain_present': data is not None, 'physical_plist': state(physical)}
        if data is not None:
            parsed = plistlib.loads(data)
            write(output / 'preferences' / (domain + '.plist'), data)
            row.update({'logical_keys': len(parsed), 'canonical_sha256': hashlib.sha256(plistlib.dumps(parsed, sort_keys=True)).hexdigest()})
        if physical.is_file() and not physical.is_symlink():
            write(output / 'preferences-physical' / (domain + '.plist'), physical.read_bytes())
        preferences[domain] = row

    saves = {}
    for label, source in SAVES.items():
        before = tree(source)
        row = {'path': str(source), 'before': before, 'copied': False}
        if before['root']['type'] == 'directory':
            dest = output / (label + '-Saves')
            shutil.copytree(source, dest, symlinks=True)
            assert tree(dest) == before and tree(source) == before, f'Save tree changed during snapshot: {source}'
            row.update({'copied': True, 'backup_path': str(dest), 'copy_verified': True})
        saves[label] = row

    previous = {
        'linux_archive': {'path': str(archive), 'state': archive_state,
                          'receipt_path': str(args.previous_linux_receipt), 'receipt_sha256': sha(args.previous_linux_receipt)},
        'linux_build': {'path': str(linux_tree), 'tree': tree(linux_tree)},
        'mac_build': {'path': str(mac_tree), 'tree': tree(mac_tree),
                      'receipt_path': str(args.previous_mac_receipt), 'receipt_sha256': sha(args.previous_mac_receipt)},
    }
    assert git('rev-parse', 'HEAD').decode().strip() == args.source_commit, 'HEAD changed during snapshot.'
    assert git('status', '--porcelain=v1', '--untracked-files=no', '-z') == status, 'Working-tree status changed during snapshot.'
    result = {
        'at': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'source': args.source_commit,
        'tracked_files': tracked, 'tracked_hashes': {n: s['sha256'] for n, s in tracked.items() if s['type'] == 'file'},
        'dirty_tracked_paths': changed, 'preferences': preferences, 'normal_saves': saves,
        'previous_current': previous, 'free_bytes': shutil.disk_usage(REPO).free,
        'source_or_player_mutations': False, 'builds_copied': False, 'storage_deletion_performed': False,
        'limits': 'Read-only input snapshot plus verified save copies; no Editor state query, atomic filesystem freeze, build invocation, restore, package execution or device acceptance. Root must retain a separate native Editor/isolation receipt.'
    }
    write(output / 'before.json', json_bytes(result))
    print(json.dumps({'snapshot': str(output), 'source': args.source_commit, 'tracked_files': len(tracked),
                      'dirty_tracked_files_preserved': len(changed), 'save_roots': {k: v['copied'] for k, v in saves.items()},
                      'previous_archive_sha256': archive_state['sha256'], 'source_or_player_mutations': False}))


if __name__ == '__main__':
    main()
