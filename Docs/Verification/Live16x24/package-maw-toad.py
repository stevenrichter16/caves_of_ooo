#!/usr/bin/env python3
"""Package a fresh, already-built maw-toad Linux output. Never build or run it.

Required: --source-commit FULL_SHA --built-after UTC_ISO_TIMESTAMP
  --linux-output-dir /.../DATE-maw-toad-SHA9-current/CavesOfOoo-current
  --mac-app /.../DATE-maw-toad-SHA9-current/CavesOfOoo.app
  --linux-build-report /.../linux-build.json --mac-build-report /.../mac-build.json
  --receipt-dir /.../existing-evidence-directory [--dry-run]

Native evidence is byte-pinned: 20 MawToad cases and 285 related cases,
all passed. The two receipts contain 305 distinct identities, no skips,
failures or inconclusives; earlier executions are not added.
This helper does not rerun tests or infer gameplay/Deck acceptance from counts.

This helper adds support files only to the specified fresh Linux output. It
refuses every collision, never renames/deletes old outputs, and needs no prior
incomplete archive. It does not invoke Git, Unity, the built game or subprocesses.
The commit/freeze time are the root operator's assertions, not independently
reproduced source-to-binary provenance. --dry-run writes nothing.
"""
import argparse
import datetime as dt
import gzip
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import plistlib
import re
import shutil
import stat
import tarfile
import xml.etree.ElementTree as ET

REPO = Path('/Users/steven/caves-of-ooo')
EVIDENCE = REPO / 'Docs/Verification/Live16x24'
CREDITS = ('CREDITS.txt', 'PLACES-CREDITS.txt', 'ATMOSPHERIC-CREDITS.txt',
           'UTILITY-CREDITS.txt', 'REGIONAL-MUSIC-CREDITS.txt', 'DEFENSE-CREDITS.txt')
# Only these two retained executions count; earlier/material receipts are not added.
FAUNA_NATIVE = (
    'maw-toad-green.xml', 20,
    'f29585a9ccf4c4b3e93199c8b6188f82f22436a52fb96a83c0684a218afd0eab',
)
RELATED_NATIVE = (
    'maw-toad-related-green.xml', 285,
    '78a3041ab42446404db04c80aedaeaf7db7f19cc8c9520281f199ca73a3fa44d',
)


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as fp:
        for chunk in iter(lambda: fp.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def json_text(value):
    return json.dumps(value, indent=2) + '\n'


def write_new(path, content):
    with path.open('x', encoding='utf-8') as fp:
        fp.write(content)


def canonical(path, directory=False):
    require(path.is_absolute() and path.resolve(strict=True) == path,
            'Use an existing absolute path without symlinks or traversal: ' + str(path))
    require(path.is_dir() if directory else path.is_file(), 'Unexpected path type: ' + str(path))
    return path


def timestamp(value):
    result = dt.datetime.fromisoformat(value.replace('Z', '+00:00'))
    require(result.tzinfo is not None, 'Timestamp needs an explicit timezone: ' + value)
    return result.astimezone(dt.timezone.utc)


def build_receipt(path, platform, expected_output, source, boundary, now):
    canonical(path)
    data = json.loads(path.read_text())
    # Accept the two observed MCP envelopes without treating the literal
    # summary result string "succeeded" as a nested JSON object.
    payload = data.get('result') if isinstance(data.get('result'), dict) else data
    require(payload.get('success', True) is True, 'Build receipt reports failure: ' + str(path))
    summary = payload.get('data', payload)
    require(isinstance(summary, dict), 'Malformed build receipt: ' + str(path))
    require(summary.get('result') == 'succeeded' and summary.get('errors') == 0,
            'Successful zero-error build required: ' + str(path))
    require(summary.get('platform') == platform, 'Wrong build platform: ' + str(path))
    require(str(summary.get('job_id', '')).startswith('build-'), 'Missing concrete build job ID.')
    started, completed = timestamp(summary['started_at']), timestamp(summary['completed_at'])
    require(boundary <= started <= completed <= now + dt.timedelta(minutes=2),
            'Receipt must be fresh after --built-after and have ordered non-future timestamps.')
    reported = Path(summary['output_path'])
    if not reported.is_absolute():
        reported = REPO / reported
    require(reported.resolve(strict=True) == expected_output, 'Receipt does not match requested output: ' + str(path))
    for envelope in (data, payload, summary):
        for key in ('runtime_source_commit', 'source_commit'):
            if key in envelope:
                require(envelope[key] == source, 'Receipt explicitly names a different source commit.')
    return {'path': str(path), 'sha256': digest(path), 'summary': summary}


def native_job(path, expected_total, expected_sha, scope, boundary):
    canonical(path)
    require(path.parent == EVIDENCE, 'Native evidence must be a durable receipt in ' + str(EVIDENCE))
    require(re.fullmatch(r'[0-9a-f]{64}', expected_sha), 'Native evidence needs its exact reviewed SHA256.')
    actual_sha = digest(path)
    require(actual_sha == expected_sha, 'Native receipt differs from reviewed byte-pinned evidence: ' + str(path))
    raw = json.loads(path.read_text())
    # The saved MCP receipt has one structured result envelope. Do not parse
    # text copies or mistake the summary's result string for another envelope.
    structured = raw.get('structuredContent', raw)
    require(isinstance(structured, dict), 'Malformed structured native receipt.')
    payload = structured.get('result', structured)
    require(isinstance(payload, dict) and payload.get('success') is True,
            'Native MCP result must report success: ' + str(path))
    job = payload.get('data')
    require(isinstance(job, dict) and job.get('mode') == 'EditMode'
            and job.get('status') == 'succeeded' and job.get('error') is None,
            'Require completed successful native EditMode job: ' + str(path))
    require(isinstance(job.get('job_id'), str) and job['job_id'], 'Native job ID is missing.')
    start, finish = job.get('started_unix_ms'), job.get('finished_unix_ms')
    require(type(start) is int and type(finish) is int and 0 < start <= finish
            and finish / 1000 <= boundary.timestamp(),
            'Native job must finish before the asserted source/build freeze boundary.')
    progress = job.get('progress')
    require(isinstance(progress, dict) and progress.get('completed') == expected_total
            and progress.get('total') == expected_total
            and progress.get('failures_so_far') == [] and progress.get('failures_capped') is False,
            'Native progress is incomplete, failed, capped or mismatched.')
    result = job.get('result')
    require(isinstance(result, dict) and result.get('mode') == 'EditMode',
            'A non-null complete native result is required; status alone is insufficient.')
    summary, cases = result.get('summary'), result.get('results')
    require(isinstance(summary, dict) and isinstance(cases, list), 'Native summary/cases missing.')
    require(summary.get('total') == summary.get('passed') == expected_total
            and summary.get('failed') == 0 and summary.get('skipped') == 0
            and summary.get('resultState') == 'Passed', 'Native summary is not all-passed.')
    if 'inconclusive' in summary:
        require(summary['inconclusive'] == 0, 'Inconclusive native results are not accepted.')
    require(len(cases) == expected_total and all(isinstance(case, dict) for case in cases),
            'Missing native case details.')
    names = {case.get('fullName') for case in cases}
    require(len(names) == expected_total and all(isinstance(name, str) and name for name in names)
            and all(case.get('state') == 'Passed' for case in cases),
            'Native cases include missing/duplicate identities or non-Passed results.')
    return ({'path': str(path), 'sha256': actual_sha, 'mode': 'EditMode',
             'job_id': job['job_id'], 'summary': summary,
             'started_unix_ms': start, 'finished_unix_ms': finish,
             'inconclusive_identities': [], 'inconclusive_reason': None,
             'scope': scope + ' Already recorded; this helper does not run tests.'}, names)


def native_xml(path, expected_total, expected_sha, boundary):
    canonical(path)
    actual_sha = digest(path)
    require(actual_sha == expected_sha, 'Native XML differs from exact reviewed bytes: ' + str(path))
    run = ET.parse(path).getroot()
    cases = run.findall('.//test-case')
    names = {case.get('fullname') for case in cases}
    require(run.tag == 'test-run' and run.get('result') == 'Passed',
            'Native XML overall result must be Passed.')
    require(int(run.get('total', '-1')) == int(run.get('testcasecount', '-1'))
            == len(cases) == len(names) == expected_total
            and int(run.get('passed', '-1')) == expected_total,
            'Native XML count or distinct identity mismatch.')
    require(int(run.get('failed', '-1')) == int(run.get('inconclusive', '-1'))
            == int(run.get('skipped', '-1')) == 0
            and all(isinstance(name, str) and name for name in names)
            and all(case.get('result') == 'Passed' for case in cases),
            'Native XML has non-Passed cases, missing identities or inconsistent counts.')
    require(run.find(".//property[@name='platform'][@value='EditMode']") is not None,
            'Native XML must identify EditMode execution.')
    started, finished = timestamp(run.get('start-time', '')), timestamp(run.get('end-time', ''))
    require(started <= finished <= boundary, 'Native XML must finish before the source/build freeze.')
    related = path.name == RELATED_NATIVE[0]
    return ({'path': str(path), 'sha256': actual_sha, 'mode': 'EditMode',
             'summary': dict(run.attrib), 'inconclusive_identities': [], 'inconclusive_reason': None,
             'skipped_identities': [], 'skipped_reason': None,
             'scope': ('285 related identities, all passed. ' if related
                       else '20 MawToad identities, all passed. ')
             + 'Already recorded; this helper does not run tests or add earlier executions.'}, names)


def native_evidence(boundary):
    receipts, groups = [], []
    for name, total, sha in (FAUNA_NATIVE, RELATED_NATIVE):
        receipt, names = native_xml(EVIDENCE / name, total, sha, boundary)
        require(not any(names & prior for prior in groups), 'The reviewed fauna/related receipts must not overlap.')
        receipt['overlap_with_preceding_receipts'] = 0
        receipts.append(receipt); groups.append(names)
    require(all(name.startswith('CavesOfOoo.Tests.FlatPixelMawToadTests.') for name in groups[0]),
            'The 20 fauna receipt must contain exactly the MawToad fixture.')
    names = groups[0] | groups[1]
    require(len(names) == 305, 'Expected 305 distinct all-passed identities.')
    return {'receipts': receipts, 'execution_occurrences': 305,
            'passed_execution_occurrences': 305, 'failed_execution_occurrences': 0,
            'skipped_execution_occurrences': 0, 'inconclusive_execution_occurrences': 0,
            'distinct_test_identities_across_listed_receipts': 305,
            'distinct_passed_test_identities': 305, 'distinct_skipped_test_identities': 0,
            'distinct_inconclusive_test_identities': 0, 'all_cases_passed': True,
            'skipped_identities': [], 'skipped_reason': None,
            'limits': 'Two byte-pinned nonoverlapping executions contain 305 distinct passed identities: '
            '20 MawToad cases and 285 related cases. No skipped, failed, inconclusive '
            'or missing result is allowed, and no earlier aggregate is added. '
            'This helper runs no tests and certifies neither complete Stump fauna/areas nor '
            'whole-chunk fullness, actual movement, gameplay, Linux execution or physical Deck behavior.'}


def tree_manifest(root):
    files, directories = [], []
    for path in [root] + sorted(root.rglob('*')):
        mode = path.lstat().st_mode
        require(stat.S_ISDIR(mode) or stat.S_ISREG(mode), 'Link or special file refused: ' + str(path))
        require(not mode & 0o7000, 'Special permission bits refused: ' + str(path))
        relative = path.relative_to(root).as_posix()
        require('\n' not in relative and '\r' not in relative and '\\' not in relative,
                'Ambiguous archive/checksum path refused: ' + str(path))
        row = {'path': relative, 'mode': oct(stat.S_IMODE(mode))}
        if stat.S_ISDIR(mode):
            directories.append(row)
        else:
            row.update(bytes=path.stat().st_size, sha256=digest(path))
            files.append(row)
    return files, directories


def verify_archive(archive, top, files, directories):
    expected = {top + '/' + row['path']: row for row in files}
    expected_dirs = {top if row['path'] == '.' else top + '/' + row['path']: row for row in directories}
    seen, seen_files, seen_dirs = set(), set(), set()
    with tarfile.open(archive, 'r|gz') as tf:
        for member in tf:
            name = member.name.rstrip('/')
            parts = PurePosixPath(name).parts
            require(parts and parts[0] == top and '..' not in parts and not name.startswith('/')
                    and '\\' not in name and name not in seen, 'Unsafe or duplicate archive entry: ' + name)
            seen.add(name)
            if member.isdir():
                require(name in expected_dirs and member.mode == int(expected_dirs[name]['mode'], 8),
                        'Unexpected directory or mode: ' + name)
                seen_dirs.add(name)
                continue
            require(member.isfile() and name in expected, 'Unexpected link/special/file entry: ' + name)
            row, h = expected[name], hashlib.sha256()
            with tf.extractfile(member) as fp:
                for chunk in iter(lambda: fp.read(1024 * 1024), b''):
                    h.update(chunk)
            require(member.size == row['bytes'] and member.mode == int(row['mode'], 8)
                    and h.hexdigest() == row['sha256'], 'Archive byte/size/mode mismatch: ' + name)
            seen_files.add(name)
    require(seen_files == set(expected) and seen_dirs == set(expected_dirs), 'Archive entries differ from the complete tree manifest.')
    # tarfile's streaming gzip adapter can stop at the tar terminator without
    # checking the outer gzip CRC/ISIZE/footer. Read that wrapper to EOF too;
    # a damaged/truncated archive must not receive a successful receipt.
    with gzip.open(archive, 'rb') as wrapper:
        for _ in iter(lambda: wrapper.read(1024 * 1024), b''):
            pass
    return len(seen)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--built-after', required=True, help='UTC source-freeze boundary asserted by root; both builds must start afterward.')
    parser.add_argument('--linux-output-dir', type=Path, required=True)
    parser.add_argument('--mac-app', type=Path, required=True)
    parser.add_argument('--linux-build-report', type=Path, required=True)
    parser.add_argument('--mac-build-report', type=Path, required=True)
    parser.add_argument('--receipt-dir', type=Path, required=True)
    parser.add_argument('--receipt-prefix', default='packaged-maw-toad-linux')
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    require(re.fullmatch(r'[0-9a-f]{40}', args.source_commit), 'Pass the full frozen source SHA.')
    require(re.fullmatch(r'[a-z0-9][a-z0-9-]*', args.receipt_prefix), 'Unsafe receipt prefix.')
    boundary, now = timestamp(args.built_after), dt.datetime.now(dt.timezone.utc)
    out = canonical(args.linux_output_dir, directory=True)
    mac = canonical(args.mac_app, directory=True)
    evidence_dir = canonical(args.receipt_dir, directory=True)
    require(out.name == 'CavesOfOoo-current' and mac.name == 'CavesOfOoo.app', 'Unexpected product output name.')
    for folder in (out.parent, mac.parent):
        require(re.fullmatch(r'\d{4}-\d{2}-\d{2}-maw-toad-' + args.source_commit[:9] + r'-current', folder.name),
                'Both fresh output folders must identify this maw-toad source and end in -current.')
    require(out.parent != mac.parent, 'Mac and Linux output roots must be separate.')
    executable = canonical(out / 'CavesOfOoo.x86_64')
    with executable.open('rb') as fp:
        elf = fp.read(20)
    require(elf[:4] == b'\x7fELF' and elf[4:6] == b'\x02\x01' and elf[18:20] == b'\x3e\x00',
            'Expected little-endian Linux ELF64 x86-64 executable.')
    require((out/'CavesOfOoo_Data/Managed/CavesOfOoo.dll').is_file(), 'Missing Linux game assembly.')
    player = canonical(out/'UnityPlayer.so')
    require(os.access(player, os.X_OK), 'UnityPlayer.so must already be executable.')
    plist = plistlib.loads(canonical(mac/'Contents/Info.plist').read_bytes())
    mac_name = plist.get('CFBundleExecutable')
    require(isinstance(mac_name, str) and mac_name and Path(mac_name).name == mac_name, 'Invalid Mac executable identity.')
    mac_exe = canonical(mac/'Contents/MacOS'/mac_name)
    require(os.access(mac_exe, os.X_OK), 'Mac executable is not executable.')
    with mac_exe.open('rb') as fp:
        require(fp.read(4) in (b'\xcf\xfa\xed\xfe', b'\xfe\xed\xfa\xcf', b'\xca\xfe\xba\xbe', b'\xca\xfe\xba\xbf'),
                'Expected Mach-O or universal Mac executable.')
    require((mac/'Contents/Resources/Data/Managed/CavesOfOoo.dll').is_file(), 'Missing Mac game assembly.')
    linux_receipt = build_receipt(args.linux_build_report, 'StandaloneLinux64', executable, args.source_commit, boundary, now)
    mac_receipt = build_receipt(args.mac_build_report, 'StandaloneOSX', mac, args.source_commit, boundary, now)
    require(linux_receipt['summary']['job_id'] != mac_receipt['summary']['job_id'], 'Build job IDs must differ.')
    original_files, original_dirs = tree_manifest(out)
    native = native_evidence(boundary)

    audio = REPO/'Assets/Resources/Audio/World'
    copies = [(REPO/'Tools/SteamDeck/Launch-CavesOfOoo.sh', out/'Launch-CavesOfOoo.sh'),
              (REPO/'Tools/SteamDeck/README-Steam-Deck.md', out/'README-Steam-Deck.md'),
              (args.linux_build_report, out/'build-report.json'),
              (args.mac_build_report, out/'mac-build-report.json'),
              (REPO/'Assets/Audio/StarterSpells-SOURCES.md', out/'STARTER-SPELLS-SOURCES.md')]
    copies += [(audio/name, out/name) for name in CREDITS]
    licenses = sorted((audio/'Licenses').glob('*.txt'))
    require(licenses, 'Missing audio license texts.')
    copies += [(path, out/'Audio-Licenses'/path.name) for path in licenses]
    copies += [(path, out/('audio-'+path.name)) for path in sorted(audio.glob('*provenance.json'))]
    source_hashes = {str(canonical(src)): digest(src) for src, _ in copies}
    archive = out.parent/'CavesOfOoo-SteamDeck-flat16x24-current.tar.gz'
    receipt_path = evidence_dir/(args.receipt_prefix+'-archive.json')
    info_path = evidence_dir/(args.receipt_prefix+'-build-info.json')
    outputs = [dst for _, dst in copies] + [out/'AUDIO-CREDITS.txt', out/'build-info.json', out/'SHA256SUMS',
               archive, Path(str(archive)+'.sha256'), receipt_path, info_path]
    require(len(outputs) == len(set(outputs)), 'Output paths collide.')
    require(not (out/'Audio-Licenses').exists() and not (out/'Audio-Licenses').is_symlink(), 'Fresh output already contains Audio-Licenses.')
    for path in outputs:
        require(not path.exists() and not path.is_symlink(), 'Refusing to overwrite: ' + str(path))
    for destination in (receipt_path, info_path):
        require(out not in destination.parents, 'External receipts must be outside the packaged output.')
    if args.dry_run:
        print(json_text({'dry_run': True, 'no_files_written': True, 'source_commit': args.source_commit,
              'built_after_utc': boundary.isoformat(), 'linux_output': str(out), 'mac_app': str(mac),
              'archive': str(archive), 'receipt': str(receipt_path), 'planned_new_files': len(outputs),
              'native_evidence': native, 'linux_execution': False, 'physical_steam_deck': False}))
        return

    # The only mutations below are new support/archive/receipt files within
    # explicitly supplied output locations and executable mode on this fresh
    # Linux output. Failure preserves partial output for inspection; no cleanup.
    (out/'Audio-Licenses').mkdir()
    for src, dst in copies:
        require(digest(src) == source_hashes[str(src)], 'Support input changed during packaging: ' + str(src))
        with src.open('rb') as source, dst.open('xb') as target:
            shutil.copyfileobj(source, target, length=1024*1024)
        shutil.copystat(src, dst, follow_symlinks=False)
        require(digest(dst) == source_hashes[str(src)], 'Support copy mismatch: ' + str(dst))
    executable.chmod(0o755)
    (out/'Launch-CavesOfOoo.sh').chmod(0o755)
    write_new(out/'AUDIO-CREDITS.txt', '\n\n'.join('=== '+name+' ===\n\n'+(out/name).read_text() for name in CREDITS)+'\n')
    info = {
        'created_at_utc': dt.datetime.now(dt.timezone.utc).isoformat(), 'runtime_source_commit': args.source_commit,
        'package_flavor': 'maw-toad-static-native-body', 'built_after_utc': boundary.isoformat(),
        'slice_description': 'The exact living one-cell MawToad uses one original native 16x24 body and authorized reader portrait, centered at 16 PPU. The previous eight species retain their unchanged borrowed artwork and five/eight-argument injection behavior; the optional ninth texture fails independently. Current graph, disclosure, selected owner, owned parts, one-cell footprint and live-state guards remain required. The body can travel outside Stump without a placement-habitat gate. The existing single Render-event damage/status cue is reused without extra reader or neighbor events. Existing anatomy, AI, bite, population, collision, multicell pilot and corpse mechanics remain unchanged. No optional animation or new corpse art was added.',
        'source_provenance': 'Full revision and source-freeze time asserted by the root build operator. Both successful receipts identify the requested fresh outputs and start after that boundary. This helper does not query Git or independently prove source-to-binary reproducibility.',
        'platform': 'Linux x86_64', 'mac_build_receipt': mac_receipt, 'linux_build_receipt': linux_receipt,
        'default_presentation': 'Flat16x24 when no saved explicit choice exists', 'prior_presentation_retained': True,
        'readme_sha256': digest(out/'README-Steam-Deck.md'),
        'verification': {'native_test_evidence': native, 'build_receipts': 'Mac and Linux succeeded with zero reported errors; warnings retained in each summary.',
            'editor_gameplay': 'Not verified by this packaging helper.', 'mac_package_smoke': 'Not verified by this packaging helper; separate ordinary standalone evidence required.',
            'linux_execution': False, 'physical_steam_deck': False, 'performance_benchmark': 'Not performed or certified.'},
        'coverage_limits': ['This bounded one-body slice extends the shared fauna catalog from eight to nine exact species. It is not complete Stump or Sodden fauna, all ordinary areas, full-chunk or worldwide graphics acceptance. Other actors, corpses, props, crops and special sites remain coverage work.',
            'A correct native body and integer-scale portrait do not establish whole-scene fullness, travel readability, other creatures or area polish. Existing anatomy and gameplay are unchanged. Fog and undisclosed cells are not treated as bare ground. No decorative fullness is invented.',
            'The two native receipts contain 305 distinct passed identities: 20 MawToad and 285 related, with zero skips, failures or inconclusives. Earlier execution totals are not added. These checks do not establish allocation or device performance. Actual controls, hurt/status visibility, travel, save/reload and Mac smoke require separate root evidence.',
            'Build/archive integrity does not certify gameplay, physical controls, full journeys, audio quality, sustained FPS or device performance. Linux is not executed; physical Steam Deck is untested. Remaining worldwide static coverage stays queued; nuanced animation remains deferred.'],
        'build_generated_settings': 'Root reviews/restores only build-generated settings separately. This helper does not edit project settings or saves.',
    }
    write_new(out/'build-info.json', json_text(info))
    files, directories = tree_manifest(out)
    write_new(out/'SHA256SUMS', ''.join(row['sha256']+'  '+row['path']+'\n' for row in files))
    files, directories = tree_manifest(out)
    by_name = {row['path']: row for row in files}
    require((out/'SHA256SUMS').read_text() == ''.join(row['sha256']+'  '+row['path']+'\n' for row in files if row['path'] != 'SHA256SUMS'),
            'Checksum manifest does not cover every other file exactly.')
    for before in original_files:
        after = by_name[before['path']]
        require((before['bytes'], before['sha256']) == (after['bytes'], after['sha256']), 'Original build bytes changed: ' + before['path'])
        require(after['mode'] == ('0o755' if before['path'] == 'CavesOfOoo.x86_64' else before['mode']), 'Unexpected build permission change.')
    require(by_name['Launch-CavesOfOoo.sh']['mode'] == '0o755', 'Launcher must be executable.')
    with archive.open('xb') as destination:
        with tarfile.open(fileobj=destination, mode='w:gz', compresslevel=6, dereference=True) as tf:
            tf.add(out, arcname=out.name)
    members = verify_archive(archive, out.name, files, directories)
    require(tree_manifest(out) == (files, directories), 'Build output changed during archive creation.')
    sha = digest(archive)
    write_new(Path(str(archive)+'.sha256'), sha+'  '+archive.name+'\n')
    receipt = {'at_utc': dt.datetime.now(dt.timezone.utc).isoformat(), 'runtime_source_commit': args.source_commit,
        'archive': str(archive), 'archive_bytes': archive.stat().st_size, 'archive_sha256': sha,
        'regular_files': len(files), 'archive_members': members, 'directories': len(directories),
        'unpacked_file_bytes': sum(row['bytes'] for row in files), 'every_archived_file_hash_size_mode_verified': True,
        'every_archived_directory_and_mode_verified': True, 'unsafe_paths_or_links': False,
        'gzip_crc_size_and_footer_verified_to_eof': True,
        'internal_sha256sums_covers_every_other_file': True, 'original_build_bytes_preserved': True,
        'prior_packages_modified': False, 'linux_executable_format': 'ELF64 little-endian x86-64 (header inspected; not executed)',
        'linux_execution': False, 'physical_steam_deck': False,
        'licenses': {'credit_sections': list(CREDITS), 'license_texts': [path.name for path in licenses], 'starter_spell_sources': True},
        'build_info': info, 'files': files, 'directory_manifest': directories}
    write_new(receipt_path, json_text(receipt))
    write_new(info_path, json_text(info))
    print(json_text({key: value for key, value in receipt.items() if key not in ('files', 'directory_manifest', 'build_info')}))


if __name__ == '__main__':
    main()
