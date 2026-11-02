#!/usr/bin/env python3
"""Package the already-built 2026-11-01 graphics checkpoint. Default/--dry-run writes nothing.

--apply adds new support/checksum/archive/receipt files only. No Unity, Git,
player execution, save changes, cleanup or replacement of existing outputs.
Requires --linux-job with the actual successful receipt job ID.
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
EVIDENCE = REPO/'Docs/Verification/GraphicsExecution/BatchDe600'
SOURCE = 'de60021715b31469a7f0c1070b2096476faeec6f'
RUNTIME_SOURCE = SOURCE
FOLDER = '2026-11-02-graphics-de6002171-current'
BUILT_AFTER = '2026-11-02T00:15:44.644633Z'
LINUX = REPO/'Builds/SteamDeck'/(FOLDER+'-retry')/'CavesOfOoo-current'
MAC = REPO/'Builds/Verification'/FOLDER/'CavesOfOoo.app'
PREFIX = 'linux'
BASE = REPO/'Docs/Verification/Live16x24/deep-caves-package-audit.py'
BASE_SHA = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
ADAPTED_FROM = 'Docs/Verification/Live16x24/regional-batch-package-audit.py'
ADAPTED_FROM_SHA = '8e3fa353485b285ab4611f0d6b2307bc61ece8919d3f0ad5e51a17c2bc5f63e4'
OVERLAY = EVIDENCE/'build-input-audit.json'
OVERLAY_SHA = '84ff8dab9706e5e0b935713029e155563f3fab56590364efc7b48541d52a883b'
CHECKPOINTS = (('Docs/Verification/GraphicsExecution/FieldComposition/final-green.xml', '8d3db7ac22f5d0ca2f72720229d7b8a29b9e70220a1b3f1838242aa099202fdf', 25, 0), ('Docs/Verification/GraphicsExecution/Ginmere/final-regression-green.xml', 'd044cefced6ff5c9a85d75eab07885c432a874e2ec710a7a397736a5eaceabd6', 160, 0), ('Docs/Verification/GraphicsExecution/GinmereDescent/green.xml', 'b38f3b151df3babc18043626ea5c9643311dc1028ede31a0609dee3e1aeb1508', 137, 0))


def helpers():
    source = BASE.read_bytes()
    if hashlib.sha256(source).hexdigest() != BASE_SHA:
        raise RuntimeError('Pinned archive helpers changed; review before reuse.')
    module = types.ModuleType('current_graphics_package_helpers')
    exec(compile(source, str(BASE), 'exec'), module.__dict__)
    return module


def native_checkpoints(a, boundary):
    rows = []
    for name, pin, passed, skipped in CHECKPOINTS:
        path = a.canonical(REPO/name)
        a.require(a.digest(path) == pin, 'Native checkpoint bytes changed: '+name)
        root = ET.parse(path).getroot()
        cases = root.findall('.//test-case')
        counts = collections.Counter(c.get('result') for c in cases)
        expected = {'Passed': passed, **({'Skipped': skipped} if skipped else {})}
        a.require(counts == expected and len(cases) == passed+skipped
                  and len({c.get('fullname') for c in cases}) == len(cases)
                  and all(c.get('fullname') for c in cases), 'Native identities/results differ: '+name)
        a.require(all(int(root.get(k, '-1')) == v for k, v in
                      {'total': passed+skipped, 'passed': passed, 'failed': 0, 'skipped': skipped, 'inconclusive': 0}.items()),
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
            'limits': 'Separate retained bounded field-composition/Ginmere-surface/descent checkpoints; '
                      'no additive all-source test total. This helper runs no tests.'}



def overlay_evidence(a, boundary):
    """Check the reviewed neutral overlay and restoration without invoking Git."""
    path = a.canonical(OVERLAY)
    a.require(a.digest(path) == OVERLAY_SHA, 'Build-input audit changed; review before reuse.')
    audit = json.loads(path.read_text())
    a.require(audit['authoritativeSourceCommit'] == SOURCE, 'Wrong overlay source revision.')
    restored = audit['controlRestoration']
    a.require(a.timestamp(restored['verifiedUtc']) <= boundary,
              'Owned renderer restoration must precede build boundary.')
    a.require(restored['checks'] == [] and audit['temporaryOwnedControls'] == [],
              'This batch has no temporary renderer controls.')
    a.require(audit['revalidatedInheritedOverlay']['all_current_tracked_runtime_matches_HEAD'] is True,
              'Missing root runtime-source audit.')
    neutral = audit['trackedMetadata'] + audit['untrackedBuildRootFiles']
    a.require(len(audit['trackedMetadata']) == 73 and len(audit['untrackedBuildRootFiles']) == 6,
              'Reviewed neutral overlay inventory differs.')
    for row in neutral:
        a.require(a.digest(a.canonical(REPO/row['path'])) == row['sha256'],
                  'Audited neutral build input changed: '+row['path'])
    settings = audit['sceneBuildSettings']
    a.require(settings['matchesAuthoritativeCommit'] is True
              and a.digest(a.canonical(REPO/settings['path'])) == settings['sha256'],
              'Enabled build scene settings changed.')
    a.require(audit['enabledBuildScenes'] == [{'enabled': True,
              'path': 'Assets/Scenes/Main/SampleScene.unity',
              'guid': '8c9cfa26abfee488c85f1582747f6a02'}], 'Unexpected build scene inventory.')
    return {'path': str(path.relative_to(REPO)), 'sha256': OVERLAY_SHA,
            'packaged_copy': 'build-input-audit.json', 'counts': audit['counts'],
            'restoration': restored, 'root_runtime_source_audit_retained': True,
            'known_neutral_build_inputs_rechecked': len(neutral),
            'enabled_scene_settings_rechecked': True,
            'clean_worktree_claim': False,
            'limits': 'Reviewed neutral import/editor overlay is disclosed, not silently staged. '
                      'The 339 excluded ignored files and two Docs PNGs remain a point-in-time inventory; '
                      'their bytes need not remain fixed while Unity runs. This helper does not scan for '
                      'new inputs, audit the import cache, invoke Git, or prove source-to-binary reproducibility.'}

def main():
    p = argparse.ArgumentParser(description=__doc__)
    mode = p.add_mutually_exclusive_group()
    mode.add_argument('--dry-run', action='store_true')
    mode.add_argument('--apply', action='store_true')
    p.add_argument('--linux-job', required=True, help='Exact actual Linux job ID in linux-build.json.')
    args = p.parse_args()
    a = helpers()
    boundary, now = a.timestamp(BUILT_AFTER), dt.datetime.now(dt.timezone.utc)
    a.require(args.linux_job.startswith('build-') and len(args.linux_job) > 6, 'Expected actual Linux build job.')
    overlay = overlay_evidence(a, boundary)
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
        ('StandaloneOSX', mac, 'mac', 'build-e10b01445c'),
        ('StandaloneLinux64', raw, 'linux', args.linux_job),
    ):
        path = EVIDENCE/(label+'-build.json')
        reports[label] = a.build_receipt(path, platform, target, SOURCE, boundary, now)
        a.require(reports[label]['summary']['job_id'] == job, 'Wrong current graphics build job: '+label)
    native = native_checkpoints(a, boundary)
    original_files, original_dirs = a.tree_manifest(out)
    audio = REPO/'Assets/Resources/Audio/World'
    copies = [(raw, executable),
              (REPO/'Tools/SteamDeck/Launch-CavesOfOoo.sh', out/'Launch-CavesOfOoo.sh'),
              (REPO/'Tools/SteamDeck/README-Steam-Deck.md', out/'README-Steam-Deck.md'),
              (Path(reports['linux']['path']), out/'build-report.json'),
              (Path(reports['mac']['path']), out/'mac-build-report.json'),
              (OVERLAY, out/'build-input-audit.json'),
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
            'planned_new_files': len(outputs), 'builds': reports, 'native_checkpoints': native,
            'build_input_overlay': overlay,
            'package_helper': {'path': str(Path(__file__).resolve().relative_to(REPO)),
                               'sha256': a.digest(Path(__file__).resolve()),
                               'adapted_from': ADAPTED_FROM, 'adapted_from_sha256': ADAPTED_FROM_SHA}}
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
                runtime_source_commit=RUNTIME_SOURCE, package_flavor='current-graphics-field-composition-grove-sandstone',
                scope='Published graphics through de6002171: Spread field margins and Ginmere/Cathedral sandstone surfaces plus Ginmere first-descent floor/walls. Bounded slice evidence only, not whole-area acceptance.',
                source_provenance='Source/runtime revision and build boundary are root assertions. Exact output/job/time receipts and neutral input/settings bytes are checked; current runtime-byte equality is a retained root assertion; no source-to-binary reproducibility claim.',
                preservation_sequence='Full snapshot /private/tmp/coo-graphics-build-protection-de6002171-v2 completed before either build:55324tracked objects,75foreign paths, normal saves/preferences and previousa72 packages. Restart-generated batching restored before final snapshot. Final restoration is a separate receipt.',
                canonical_executable_copy=copy_info, helper_sha256=BASE_SHA,
                default_presentation='Flat16x24 unless a saved explicit choice exists',
                controller_guidance='Existing built-in controls documented in unchanged README-Steam-Deck.md; no new controller mapping or physical verification.',
                mac_package_smoke='Separate ordinary runtime evidence required; not performed by this helper.',
                linux_execution=False, physical_steam_deck=False, performance_benchmark=False,
                performance_limits='Separate Editor manual profiles exist, including matched Felling observations; this helper neither validates those measurements nor treats them as packaged/Linux/Deck performance acceptance.',
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
