#!/usr/bin/env python3
"""Package a fresh, already-built deep-caves Linux output. Never build or run it.

Required: --source-commit FULL_SHA --built-after UTC_ISO_TIMESTAMP
  --linux-output-dir /.../DATE-deep-caves-SHA9-current/CavesOfOoo-current
  --mac-app /.../DATE-deep-caves-SHA9-current/CavesOfOoo.app
  --linux-build-report /.../linux-build.json --mac-build-report /.../mac-build.json
  --receipt-dir /.../existing-evidence-directory [--dry-run]

Native evidence is byte-pinned at sequential source checkpoints: 419 stone,
19 SteamVent focused and 189 SteamVent related occurrences. There are 102
repeated identities: 627 occurrences, 525 distinct Passed identities, no failures,
skips or inconclusives. This is NOT one combined final-source 525-case run.
The helper does not rerun tests or infer gameplay/Deck acceptance from counts.

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
# Complete Results were retained before Play; actual later job envelopes
# honestly have null results or a summary with null case rows. Both are pinned.
# These constants replace a synthetic unified native-summary receipt: no such
# combined execution occurred. Current source bytes are bound separately.
BOUND_SOURCE_COMMIT = '7e2ed7a30dbd363331480321933b36fc67c58659'
NATIVE_RECEIPTS = (('shared-stone-props-final-green.json',
  419,
  'd15f9cade4bdffe9ebfcf7610f63a1b8f56078d706d937ac91207a390130002e',
  'shared-stone-props-final-job.json',
  '8e62fb9ab380316e95482b98eec4df29c022f086c4ce1791a224cc0077cee676',
  '7a7951072dde42ee88a3816f40de5b1b',
  'null'),
 ('shared-steam-vent-native-green.json',
  19,
  'eae1097e38f0cb46159b7fa92ad5b9f7ffeec382e283eb862bdd40ddd8b8e835',
  'shared-steam-vent-native-job.json',
  '37e70a699402e7d7c501fd956f3dfd1566f83e27ca0c0ada6247171ab02f0a8e',
  '1c6f8b1094d7482e854fa0e2dc98fc56',
  'null'),
 ('shared-steam-vent-related-green.json',
  189,
  '00ea601ec8088948d3f6a7aaf364f2ba4ab8d8b9d8ed7b0ebc43429ea4ad715e',
  'shared-steam-vent-related-job.json',
  'a2c949d45c909a74a3074534333aae8081bd61703618349139f92de77186b4f3',
  '6d16cd2688d14ac5a66f5cc5e43a03ed',
  'summary-null-cases'))
EXPECTED_CLASSES = ({'FlatPixelFallbackCatalogTests': 40,
  'FlatPixelGreyboxCatalogTests': 33,
  'FlatPixelLimestoneCavesTests': 36,
  'FlatPixelObsidianCavesTests': 41,
  'FlatPixelQuartziteCavesTests': 36,
  'FlatPixelShaleCavesTests': 36,
  'FlatPixelShallowCavesTests': 56,
  'FlatPixelSharedFurnitureTests': 33,
  'FlatPixelSharedStonePropsTests': 29,
  'FlatPixelSharedStorageTests': 43,
  'FlatPixelSlateCavesTests': 36},
 {'FlatPixelSharedSteamVentTests': 19},
 {'FlatPixelEastKitchenTests': 26,
  'FlatPixelEastSteamAdversarialTests': 20,
  'FlatPixelEastSteamTests': 36,
  'FlatPixelFallbackCatalogTests': 40,
  'FlatPixelGreyboxCatalogTests': 33,
  'FlatPixelSharedStonePropsTests': 29,
  'TileStateSourcePartTests': 5})
NATIVE_BINDINGS = {'ArtSource/Live16x24/obsidian-caves/obsidian-caves.png': 'b1d09491679b24cfac5aac4f39f63cd2d47d1dacba04ead64149541205f09d81',
 'ArtSource/Live16x24/quartzite-caves/quartzite-caves.png': '47b8a6984d02da33b000628e61daee9cd21a22fd5d4160293267a902204340eb',
 'Assets/Resources/Sprites/Environment/pillar.png': 'ae67290b62c6534b11f933397d5e6ce44bb53a84602d38e5d4a52749622d23a1',
 'Assets/Resources/Sprites/Environment/steam_vent.png': 'c381fbc440337013e30cf47c68c76944ab624038b1232819bd923fcb1628821d',
 'Assets/Resources/Sprites/Live16x24/east-kitchen.png': '594079f4dbfe9d17f0b58f8fcb6adc647d63dae9cfd385ce32fd8b6e31413c56',
 'Assets/Resources/Sprites/Live16x24/obsidian-caves.png': 'b1d09491679b24cfac5aac4f39f63cd2d47d1dacba04ead64149541205f09d81',
 'Assets/Resources/Sprites/Live16x24/opening-base.png': '71ce34419f0e5b1a9a494dd4abd2b29efa8c5866dab97e2aaa57b1e2fb42df2e',
 'Assets/Resources/Sprites/Live16x24/quartzite-caves.png': '47b8a6984d02da33b000628e61daee9cd21a22fd5d4160293267a902204340eb',
 'Assets/Resources/Sprites/Live16x24/slate-caves.png': 'e3fe86854fb139721f42058585bd5d000f266fde58f2b426451dff6ad1dfb1ff',
 'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': '76c7cce4367f3d85fd3e874b44b597a768f420be63c280fc329ab38590abd98c',
 'Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs': '87c156c5867d21f9d856788732ea03a715329adf36c2e6778e7ea96bacc72a07',
 'Assets/Scripts/Presentation/Rendering/FlatPixelObsidianCavesCatalog.cs': 'fe9946014bfed03771b46bb383f7572d025fdd79fbe6654846d054bb694193c6',
 'Assets/Scripts/Presentation/Rendering/FlatPixelQuartziteCavesCatalog.cs': '734d1cec031c0f00ad6aeeadf300ee6270cd4b616a88bb26e83b7542d5478bb8',
 'Assets/Scripts/Presentation/Rendering/FlatPixelShaleCavesCatalog.cs': 'f7311b85c44ce550a10e62ba1ea5cc83d968d8eac37916af7a9ea32c40dabca6',
 'Assets/Scripts/Presentation/Rendering/FlatPixelShallowCavesCatalog.cs': 'f06eabc4b6c263a05660bf19cbacbc6eb59a6c4b09f3d85f4aa628a5466ca88d',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedSteamVentCatalog.cs': 'd8086aefc4c8de74a603e519a38d69ba08dcbeb3983a69989d094524cef3274a',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedStonePropsCatalog.cs': 'b2098dd328edec732fe1b663a302714e3ba4288009afff7cdb8b4e446f382908',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSlateCavesCatalog.cs': 'e59940798d370e58c3d9f3051c3864cd5cf0d788e32189558e2753be944b0969',
 'Assets/Tests/EditMode/FlatPixelObsidianCavesTests.cs': 'f7424f35f70846cd9c5455c7d99c8dafed329a31406930dc7f67d31616e42c71',
 'Assets/Tests/EditMode/FlatPixelQuartziteCavesTests.cs': '9600bab89a89571cc766f6fab2b3fa6520706e620f0fcde4ab2c7b8fe78413fd',
 'Assets/Tests/EditMode/FlatPixelSharedSteamVentTests.cs': '799fd830370a37aaf9be2e09c2c49923164012a5ac3e4406383762e4d641a676',
 'Assets/Tests/EditMode/FlatPixelSharedStonePropsTests.cs': '0330b193061852229d09f78b824805e4e25849abdfcfd97907bb994668408ede',
 'Docs/Verification/Live16x24/shared-steam-vent-native-green.json': 'eae1097e38f0cb46159b7fa92ad5b9f7ffeec382e283eb862bdd40ddd8b8e835',
 'Docs/Verification/Live16x24/shared-steam-vent-native-job.json': '37e70a699402e7d7c501fd956f3dfd1566f83e27ca0c0ada6247171ab02f0a8e',
 'Docs/Verification/Live16x24/shared-steam-vent-related-green.json': '00ea601ec8088948d3f6a7aaf364f2ba4ab8d8b9d8ed7b0ebc43429ea4ad715e',
 'Docs/Verification/Live16x24/shared-steam-vent-related-job.json': 'a2c949d45c909a74a3074534333aae8081bd61703618349139f92de77186b4f3',
 'Docs/Verification/Live16x24/shared-stone-props-final-green.json': 'd15f9cade4bdffe9ebfcf7610f63a1b8f56078d706d937ac91207a390130002e',
 'Docs/Verification/Live16x24/shared-stone-props-final-job.json': '8e62fb9ab380316e95482b98eec4df29c022f086c4ce1791a224cc0077cee676'}

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


def native_result(pin, boundary):
    name, expected_total, expected_sha, job_name, job_sha, job_id, detail_shape = pin
    path = canonical(EVIDENCE / name)
    require(path.parent == EVIDENCE and re.fullmatch(r'[0-9a-f]{64}', expected_sha),
            'Native result must be an exact durable byte-pinned receipt.')
    actual_sha = digest(path)
    require(actual_sha == expected_sha, 'Native result differs from reviewed bytes: ' + str(path))
    raw = json.loads(path.read_text())
    require(isinstance(raw, dict) and raw.get('mode') == 'EditMode', 'Expected raw native EditMode Result.')
    summary, cases = raw.get('summary'), raw.get('results')
    require(isinstance(summary, dict) and isinstance(cases, list)
            and len(cases) == expected_total and all(isinstance(case, dict) for case in cases),
            'Native summary/case details are missing.')
    names = {case.get('fullName') for case in cases}
    require(len(names) == expected_total and all(isinstance(name, str) and name for name in names)
            and all(case.get('state') == 'Passed' for case in cases),
            'Missing/duplicate identities or a non-Passed case.')
    require(summary.get('total') == summary.get('passed') == expected_total
            and summary.get('failed') == summary.get('skipped') == 0
            and summary.get('resultState') == 'Passed' and summary.get('inconclusive', 0) == 0,
            'Native result must be entirely Passed; no inconclusive exceptions.')
    job_path = canonical(EVIDENCE / job_name)
    require(job_path.parent == EVIDENCE and digest(job_path) == job_sha,
            'Actual native job receipt differs from its reviewed byte pin.')
    response = json.loads(job_path.read_text())
    require(response.get('success') is True and response.get('error') is None
            and isinstance(response.get('data'), dict), 'Expected successful actual tool response envelope.')
    job = response['data']
    require(job.get('job_id') == job_id and job.get('status') == 'succeeded'
            and job.get('mode') == 'EditMode' and job.get('error') is None,
            'Native job was not successfully completed.')
    progress = job.get('progress', {})
    require(progress.get('total') == progress.get('completed') == expected_total
            and progress.get('failures_so_far') == [] and progress.get('failures_capped') is False,
            'Native completed-job progress differs from the complete raw Result.')
    # Retain actual post-Play metadata shapes; full case evidence is the
    # separately pinned exported Result, never fabricated job case detail.
    detail = job.get('result')
    if detail_shape == 'null':
        require(detail is None, 'Native job shape differs from the reviewed null-result response.')
    else:
        require(detail_shape == 'summary-null-cases' and isinstance(detail, dict)
                and detail.get('mode') == 'EditMode' and detail.get('summary') == summary
                and 'results' in detail and detail['results'] is None,
                'Native metadata differs from the reviewed summary/null-cases response.')
    start, finish, updated = (job.get(key) for key in ('started_unix_ms', 'finished_unix_ms', 'last_update_unix_ms'))
    require(all(type(value) is int for value in (start, finish, updated))
            and 0 < start <= finish <= updated and updated / 1000 <= boundary.timestamp(),
            'Actual native times must precede the asserted source/build boundary.')
    return ({'path': str(path), 'sha256': actual_sha, 'mode': 'EditMode', 'job_id': job_id,
             'summary': summary, 'actual_case_rows': expected_total,
             'actual_job_metadata': job, 'job_receipt': {'path': str(job_path), 'sha256': job_sha},
             'started_unix_ms': start, 'finished_unix_ms': finish,
             'scope': 'Earlier completed native execution; full Result exported before Play and paired with actual retained job metadata. This helper does not run tests.'}, names)


def native_evidence(boundary):
    # Current frozen bytes do not relabel earlier execution as a run of this
    # final source. Both provenance boundaries are explicit in the output.
    for name, expected_sha in NATIVE_BINDINGS.items():
        require(digest(canonical(REPO / name)) == expected_sha,
                'Current frozen source/evidence/resource differs: ' + name)
    receipts, groups = [], []
    for pin, expected_overlap in zip(NATIVE_RECEIPTS, (0, 0, 102)):
        receipt, names = native_result(pin, boundary)
        prior = set.union(*groups) if groups else set()
        overlap = len(names & prior)
        require(overlap == expected_overlap, 'Unexpected overlap across retained checkpoints.')
        receipt['overlap_with_preceding_receipts'] = overlap
        receipts.append(receipt); groups.append(names)
    require(not (groups[1] & groups[2]), 'Steam focused/related executions must be disjoint.')
    for names, counts in zip(groups, EXPECTED_CLASSES):
        require(sum(counts.values()) == len(names) and all(
            sum(name.startswith('CavesOfOoo.Tests.' + cls + '.') for name in names) == count
            for cls, count in counts.items()), 'Native fixture scope differs from reviewed case identities.')
    require(sum(map(len, groups)) == 627 and len(set.union(*groups)) == 525,
            'Expected 627 retained occurrences and 525 distinct Passed identities.')
    return {'receipts': receipts,
            'current_source_binding': {'source_commit': BOUND_SOURCE_COMMIT,
                'pins': NATIVE_BINDINGS, 'current_source_evidence_resource_bytes_match': True,
                'limits': 'Selected current runtime/test/resource bytes and retained evidence are pinned in this reviewed helper. '
                'The 419-case stone checkpoint predates Steam integration; these are sequential checkpoints, not a combined final-source run. '
                'Root asserts the source revision/freeze and build provenance; binary reproducibility is not independently proved.'},
            'execution_occurrences': 627, 'passed_execution_occurrences': 627,
            'failed_execution_occurrences': 0, 'skipped_execution_occurrences': 0,
            'inconclusive_execution_occurrences': 0,
            'repeated_identity_occurrences': 102,
            'distinct_test_identities_across_listed_receipts': 525,
            'distinct_passed_test_identities': 525, 'distinct_skipped_test_identities': 0,
            'distinct_inconclusive_test_identities': 0, 'all_cases_passed': True,
            'combined_final_source_execution': False,
            'stone_checkpoint_passed': 419, 'steam_focused_passed': 19, 'steam_related_passed': 189,
            'skipped_identities': [], 'skipped_reason': None,
            'inconclusive_identities': [], 'inconclusive_reason': None,
            'limits': '419 stone plus 19 focused/189 related Steam occurrences, with 102 repeated identities, give 525 distinct retained Passed identities. '
            'Earlier Quartzite/Obsidian receipts are subsets and are not added. No failed/skipped/inconclusive rows. '
            'These are successive source checkpoints, not one 525-case final-source execution. '
            'Actual job metadata and separately retained complete raw Results supply counts/times without fabrication. '
            'This helper runs no tests and certifies neither whole-area graphics, gameplay, Linux/Deck execution nor performance.'}


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
    parser.add_argument('--receipt-prefix', default='packaged-deep-caves-linux')
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    require(re.fullmatch(r'[0-9a-f]{40}', args.source_commit), 'Pass the full frozen source SHA.')
    require(args.source_commit == BOUND_SOURCE_COMMIT, 'Source differs from this reviewed batch binding.')
    require(re.fullmatch(r'[a-z0-9][a-z0-9-]*', args.receipt_prefix), 'Unsafe receipt prefix.')
    boundary, now = timestamp(args.built_after), dt.datetime.now(dt.timezone.utc)
    out = canonical(args.linux_output_dir, directory=True)
    mac = canonical(args.mac_app, directory=True)
    evidence_dir = canonical(args.receipt_dir, directory=True)
    require(out.name == 'CavesOfOoo-current' and mac.name == 'CavesOfOoo.app', 'Unexpected product output name.')
    for folder in (out.parent, mac.parent):
        require(re.fullmatch(r'\d{4}-\d{2}-\d{2}-deep-caves-' + args.source_commit[:9] + r'-current', folder.name),
                'Both fresh output folders must identify this deep-caves source and end in -current.')
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
        'package_flavor': 'deep-caves-materials-shared-stone-steam', 'built_after_utc': boundary.isoformat(),
        'slice_description': 'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.',
        'source_provenance': 'Full revision and source-freeze time asserted by the root build operator. Both successful receipts identify the requested fresh outputs and start after that boundary. This helper does not query Git or independently prove source-to-binary reproducibility.',
        'platform': 'Linux x86_64', 'mac_build_receipt': mac_receipt, 'linux_build_receipt': linux_receipt,
        'default_presentation': 'Flat16x24 when no saved explicit choice exists', 'prior_presentation_retained': True,
        'readme_sha256': digest(out/'README-Steam-Deck.md'),
        'verification': {'native_test_evidence': native, 'build_receipts': 'Mac and Linux succeeded with zero reported errors; warnings retained in each summary.',
            'editor_gameplay': 'Not verified by this packaging helper.', 'mac_package_smoke': 'Not verified by this packaging helper; separate ordinary standalone evidence required.',
            'linux_execution': False, 'physical_steam_deck': False, 'performance_benchmark': 'Not performed or certified.'},
        'coverage_limits': ['Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.',
            'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.',
            f'Three complete retained native Results contain {native["distinct_passed_test_identities"]} distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.',
            'Build/archive integrity does not certify gameplay, physical controls, full journeys, audio quality, sustained FPS or device performance. Linux is not executed; physical Steam Deck is untested. Worldwide static coverage and later area polish remain open.'],
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
