#!/usr/bin/env python3
"""Package this already-built regional batch. Default/--dry-run writes nothing.

--apply adds new support/checksum/archive/receipt files only. No Unity, Git,
player execution, save changes, cleanup or replacement of existing outputs.
Failure preserves partial output for inspection; do not blindly rerun --apply.
"""
import argparse
import collections
import datetime as dt
import hashlib
import json
from pathlib import Path
import plistlib
import shutil
import tarfile
import types
import xml.etree.ElementTree as ET

REPO = Path('/Users/steven/caves-of-ooo')
EVIDENCE = REPO/'Docs/Verification/Live16x24'
SOURCE = 'aa92e274664c3501badbe2cd18944689ac61cfc6'
RUNTIME_SOURCE = '3705cb241f99821eca9156e7e4719b4f5576f6f7'
FOLDER = '2026-10-28-regional-batch-aa92e2746-current'
BUILT_AFTER = '2026-10-28T23:25:37.078834+00:00'
LINUX = REPO/'Builds/SteamDeck'/FOLDER/'CavesOfOoo-current'
MAC = REPO/'Builds/Verification'/FOLDER/'CavesOfOoo.app'
PREFIX = 'packaged-regional-batch-linux'
BASE = EVIDENCE/'deep-caves-package-audit.py'
BASE_SHA = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
CHECKPOINTS = (
    ('regional-fixtures-green.xml', 'dd51754349109040a0b8a6ce6177baaf35fba2029389bf10f214e323750529a1', 11, 0),
    ('beating-population-green.xml', 'd64a4247560289c61fee08d3e4bc0515387b061c4cfd415c280bd39faaa415ed', 79, 0),
    ('beating-brush-reader-green.xml', '507f855ebab55e3d2206bd3fe68219dccd39ee1936d3346053608f86f8bedf0c', 112, 0),
)


def helpers():
    source = BASE.read_bytes()
    if hashlib.sha256(source).hexdigest() != BASE_SHA:
        raise RuntimeError('Pinned archive helpers changed; review before reuse.')
    module = types.ModuleType('regional_package_helpers')
    exec(compile(source, str(BASE), 'exec'), module.__dict__)
    return module


def native_checkpoints(a, boundary):
    rows = []
    for name, pin, passed, skipped in CHECKPOINTS:
        path = a.canonical(EVIDENCE/name)
        a.require(a.digest(path) == pin, 'Native checkpoint bytes changed: '+name)
        root = ET.parse(path).getroot()
        cases = root.findall('.//test-case')
        counts = collections.Counter(c.get('result') for c in cases)
        expected = {'Passed': passed, **({'Skipped': skipped} if skipped else {})}
        a.require(counts == expected and len(cases) == passed+skipped
                  and len({c.get('fullname') for c in cases}) == len(cases)
                  and all(c.get('fullname') for c in cases), 'Native identities/results differ: '+name)
        a.require(all(int(root.get(k, '-1')) == v for k, v in
                      {'total': passed+skipped, 'passed': passed, 'failed': 0, 'skipped': skipped}.items()),
                  'Native XML summary differs: '+name)
        start, end = a.timestamp(root.get('start-time')), a.timestamp(root.get('end-time'))
        a.require(start <= end <= boundary, 'Native checkpoint must precede build boundary.')
        rows.append({'path': str(path.relative_to(REPO)), 'sha256': pin,
                     'passed': passed, 'failed': 0, 'skipped': skipped,
                     'started_at_utc': start.isoformat(), 'completed_at_utc': end.isoformat(),
                     'skips': [{'identity': c.get('fullname'), 'reason': c.findtext('reason/message')}
                               for c in cases if c.get('result') == 'Skipped']})
    return {'checkpoints': rows, 'combined_final_source_execution': False,
            'counts_added_together': False,
            'limits': 'Separate retained regional-fixture, ruin-composition and brush-reader checkpoints; '
                      'no additive all-source test total. This helper runs no tests.'}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    mode = p.add_mutually_exclusive_group()
    mode.add_argument('--dry-run', action='store_true')
    mode.add_argument('--apply', action='store_true')
    args = p.parse_args()
    a = helpers()
    boundary, now = a.timestamp(BUILT_AFTER), dt.datetime.now(dt.timezone.utc)
    out, mac = a.canonical(LINUX, True), a.canonical(MAC, True)
    a.canonical(EVIDENCE, True)
    raw = a.canonical(out/'CavesOfOoo')
    executable = out/'CavesOfOoo.x86_64'
    with raw.open('rb') as f:
        elf = f.read(20)
    a.require(elf[:4] == b'\x7fELF' and elf[4:6] == b'\x02\x01' and elf[18:20] == b'\x3e\x00',
              'Raw output is not Linux ELF64 x86-64.')
    a.canonical(out/'CavesOfOoo_Data/Managed/CavesOfOoo.dll')
    player = a.canonical(out/'UnityPlayer.so')
    a.require(player.stat().st_mode & 0o111, 'UnityPlayer.so must already be executable.')
    plist = plistlib.loads(a.canonical(mac/'Contents/Info.plist').read_bytes())
    name = plist.get('CFBundleExecutable')
    a.require(isinstance(name, str) and name and Path(name).name == name, 'Invalid Mac executable identity.')
    mac_exe = a.canonical(mac/'Contents/MacOS'/name)
    a.require(mac_exe.stat().st_mode & 0o111, 'Mac executable is not executable.')
    with mac_exe.open('rb') as f:
        a.require(f.read(4) in (b'\xcf\xfa\xed\xfe', b'\xfe\xed\xfa\xcf', b'\xca\xfe\xba\xbe', b'\xca\xfe\xba\xbf'),
                  'Mac output is not Mach-O/universal.')
    a.canonical(mac/'Contents/Resources/Data/Managed/CavesOfOoo.dll')
    reports = {}
    for platform, target, label, job in (
        ('StandaloneOSX', mac, 'mac', 'build-aa27cba792'),
        ('StandaloneLinux64', raw, 'linux', 'build-f9a19dbd21'),
    ):
        path = EVIDENCE/('packaged-regional-batch-'+label+'-build.json')
        reports[label] = a.build_receipt(path, platform, target, SOURCE, boundary, now)
        a.require(reports[label]['summary']['job_id'] == job, 'Wrong batch build job: '+label)
    native = native_checkpoints(a, boundary)
    original_files, original_dirs = a.tree_manifest(out)
    audio = REPO/'Assets/Resources/Audio/World'
    copies = [(raw, executable),
              (REPO/'Tools/SteamDeck/Launch-CavesOfOoo.sh', out/'Launch-CavesOfOoo.sh'),
              (REPO/'Tools/SteamDeck/README-Steam-Deck.md', out/'README-Steam-Deck.md'),
              (Path(reports['linux']['path']), out/'build-report.json'),
              (Path(reports['mac']['path']), out/'mac-build-report.json'),
              (REPO/'Assets/Audio/StarterSpells-SOURCES.md', out/'STARTER-SPELLS-SOURCES.md')]
    copies += [(audio/name, out/name) for name in a.CREDITS]
    licenses = sorted((audio/'Licenses').glob('*.txt'))
    a.require(licenses, 'Audio licenses missing.')
    copies += [(path, out/'Audio-Licenses'/path.name) for path in licenses]
    copies += [(path, out/('audio-'+path.name)) for path in sorted(audio.glob('*provenance.json'))]
    pins = {str(src): a.digest(a.canonical(src)) for src, _ in copies}
    archive = out.parent/'CavesOfOoo-SteamDeck-flat16x24-current.tar.gz'
    receipt = EVIDENCE/(PREFIX+'-archive.json')
    info_receipt = EVIDENCE/(PREFIX+'-build-info.json')
    copy_receipt = EVIDENCE/(PREFIX+'-executable-copy.json')
    outputs = [dst for _, dst in copies] + [out/'AUDIO-CREDITS.txt', out/'build-info.json', out/'SHA256SUMS',
               archive, Path(str(archive)+'.sha256'), receipt, info_receipt, copy_receipt]
    a.require(len(outputs) == len(set(outputs)), 'Output paths collide.')
    for path in outputs+[out/'Audio-Licenses']:
        a.require(not path.exists() and not path.is_symlink(), 'Refusing to overwrite: '+str(path))
    plan = {'source_commit': SOURCE, 'runtime_source_commit': RUNTIME_SOURCE, 'built_after_utc': BUILT_AFTER,
            'linux_output': str(out), 'mac_app': str(mac), 'archive': str(archive),
            'planned_new_files': len(outputs), 'builds': reports, 'native_checkpoints': native}
    if not args.apply:
        print(a.json_text(dict(plan, dry_run=True, no_files_written=True)))
        return

    (out/'Audio-Licenses').mkdir()
    for src, dst in copies:
        a.require(a.digest(src) == pins[str(src)], 'Copy input changed: '+str(src))
        with src.open('rb') as source, dst.open('xb') as target:
            shutil.copyfileobj(source, target, length=1024*1024)
        shutil.copystat(src, dst, follow_symlinks=False)
        a.require(a.digest(dst) == pins[str(src)], 'Copied bytes differ: '+str(dst))
    executable.chmod(0o755)
    (out/'Launch-CavesOfOoo.sh').chmod(0o755)
    copy_info = {'source_commit': SOURCE, 'at_utc': dt.datetime.now(dt.timezone.utc).isoformat(),
                 'raw_build_executable': str(raw.relative_to(REPO)),
                 'canonical_archive_executable': str(executable.relative_to(REPO)),
                 'action': 'exclusive copy; raw build executable preserved',
                 'bytes': raw.stat().st_size, 'both_sha256': a.digest(raw), 'mode': '0o755'}
    a.require(a.digest(executable) == copy_info['both_sha256'], 'Canonical executable differs.')
    a.write_new(out/'AUDIO-CREDITS.txt', '\n\n'.join('=== '+n+' ===\n\n'+(out/n).read_text() for n in a.CREDITS)+'\n')
    info = dict(plan, created_at_utc=dt.datetime.now(dt.timezone.utc).isoformat(),
                runtime_source_commit=RUNTIME_SOURCE, package_flavor='reviewed-regional-continuity-beating-shadows-ruin-shelters',
                scope='Reviewed ordinary Spread terrain and fixtures, regional fixture continuity, Beating masonry shading and fresh ruin shelter composition; approved existing art and gameplay retained.',
                source_provenance='Frozen build HEAD includes docs after the separately recorded runtime revision; root asserts provenance, exact output/job/time receipts checked, not source-to-binary reproducibility.',
                canonical_executable_copy=copy_info, helper_sha256=BASE_SHA,
                default_presentation='Flat16x24 unless a saved explicit choice exists',
                controller_guidance='Existing built-in controls documented in unchanged README-Steam-Deck.md; no new controller mapping or physical verification.',
                mac_package_smoke='Separate ordinary runtime evidence required; not performed by this helper.',
                linux_execution=False, physical_steam_deck=False, performance_benchmark=False,
                whole_area_polish_accepted=False,
                limits='Archive integrity does not certify gameplay, all-source tests, whole-area polish, Linux/Deck execution or performance.')
    a.write_new(out/'build-info.json', a.json_text(info))
    files, directories = a.tree_manifest(out)
    a.write_new(out/'SHA256SUMS', ''.join(row['sha256']+'  '+row['path']+'\n' for row in files))
    files, directories = a.tree_manifest(out)
    a.require((out/'SHA256SUMS').read_text() == ''.join(row['sha256']+'  '+row['path']+'\n' for row in files if row['path'] != 'SHA256SUMS'),
              'Checksums do not cover every other packaged file exactly.')
    by_name = {row['path']: row for row in files}
    a.require(all(by_name.get(row['path']) == row for row in original_files), 'Original Linux build bytes/modes changed.')
    dir_names = {row['path']: row for row in directories}
    a.require(all(dir_names.get(row['path']) == row for row in original_dirs), 'Original Linux directory modes changed.')
    a.require(by_name['CavesOfOoo.x86_64']['mode'] == by_name['Launch-CavesOfOoo.sh']['mode'] == '0o755',
              'Canonical executable and launcher must be 0755.')
    with archive.open('xb') as destination:
        with tarfile.open(fileobj=destination, mode='w:gz', compresslevel=6, dereference=True) as tf:
            tf.add(out, arcname=out.name)
    members = a.verify_archive(archive, out.name, files, directories)
    a.require(a.tree_manifest(out) == (files, directories), 'Output changed during archive creation.')
    sha = a.digest(archive)
    a.write_new(Path(str(archive)+'.sha256'), sha+'  '+archive.name+'\n')
    result = {'at_utc': dt.datetime.now(dt.timezone.utc).isoformat(), 'runtime_source_commit': RUNTIME_SOURCE,
              'archive': str(archive.relative_to(REPO)), 'archive_bytes': archive.stat().st_size, 'archive_sha256': sha,
              'regular_files': len(files), 'directories': len(directories), 'archive_members': members,
              'every_archived_file_hash_size_mode_verified': True, 'every_archived_directory_and_mode_verified': True,
              'gzip_crc_size_and_footer_verified_to_eof': True, 'internal_sha256sums_covers_every_other_file': True,
              'original_build_bytes_and_modes_preserved': True, 'prior_packages_modified': False,
              'linux_execution': False, 'physical_steam_deck': False, 'build_info': info,
              'files': files, 'directory_manifest': directories}
    a.write_new(copy_receipt, a.json_text(copy_info))
    a.write_new(info_receipt, a.json_text(info))
    a.write_new(receipt, a.json_text(result))
    print(a.json_text({k: v for k, v in result.items() if k not in ('build_info', 'files', 'directory_manifest')}))


if __name__ == '__main__':
    main()
