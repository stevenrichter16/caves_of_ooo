#!/usr/bin/env python3
"""Package an already-built place/audio Linux player; never builds or commits.

All final receipts and the exact current HEAD must be supplied explicitly.
This script writes only the NEW place-audio delivery and refuses to replace
an existing archive. Offline asset checks do not establish subjective listening,
Unity resource-archive contents, or physical Steam Deck performance.
"""
import argparse
import datetime
import hashlib
import json
import pathlib
import re
import shutil
import subprocess
import tarfile
import xml.etree.ElementTree as ET


def require(ok, message):
    if not ok:
        raise RuntimeError(message)


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def stream_sha(stream):
    digest = hashlib.sha256()
    while block := stream.read(1024 * 1024):
        digest.update(block)
    return digest.hexdigest()


def read_json(path):
    return json.loads(path.read_text())


LICENSE_SHA256 = {'CC-BY-4.0.txt': '9ba9550ad48438d0836ddab3da480b3b69ffa0aac7b7878b5a0039e7ab429411', 'CC0-1.0.txt': 'a2010f343487d3f7618affe54f789f5487602331c0a8d03f49e9a7c547cf0499', 'kenney_impact-sounds.txt': 'b49aa9c56b04528b95913de13e506a0f7c5e807b9925db9bfef86af1f91120db', 'kenney_interface-sounds.txt': 'f7966c773bbed0eca6a9c75081c44a178b38eae112724dbb5fdfbd4192d118a9', 'kenney_rpg-audio.txt': '5735dfd72cb64cbbceda4ebc00c380c41ca680edb82ff153aa7c9ab97614c539'}

LICENSE_FILES = ('CC-BY-4.0.txt', 'CC0-1.0.txt', 'kenney_rpg-audio.txt',
                 'kenney_impact-sounds.txt', 'kenney_interface-sounds.txt')


def validate_tests(root):
    """Reject optimistic summaries unless all individual NUnit outcomes agree."""
    require(root.tag == 'test-run', 'Expected final native NUnit test-run XML.')
    summary = {key: int(root.get(key, '-1')) for key in ('total', 'passed', 'failed', 'skipped', 'inconclusive')}
    cases = list(root.iter('test-case'))
    require(root.get('result') == 'Passed' and summary['total'] > 0 and summary['passed'] > 0
            and summary['failed'] == 0 and summary['inconclusive'] == 0 and summary['skipped'] >= 0
            and summary['passed'] + summary['skipped'] == summary['total'] == len(cases),
            'Final NUnit receipt has failures or inconsistent/incomplete counts.')
    require(sum(case.get('result') == 'Passed' for case in cases) == summary['passed']
            and sum(case.get('result') == 'Skipped' for case in cases) == summary['skipped'],
            'Individual NUnit outcomes disagree with summary.')
    return summary


def validate_native(native):
    """Native output proves only the explicitly recorded world-audio checks."""
    require(native.get('mode') == 'world-audio' and native.get('complete') is True
            and native.get('errorsFinalized') is True and native.get('failures') == 0
            and native.get('unexpectedErrors') == 0 and not native.get('fatal') and native.get('runId'),
            'Final native world-audio audit is incomplete or contains failures/errors.')
    cases = native.get('audit', [])
    count = native.get('cases')
    require(type(count) is int and count > 0 and isinstance(cases, list) and len(cases) == count
            and all(isinstance(case, str) and case.startswith('PASS ') for case in cases),
            'Native check count or individual PASS records disagree.')
    require(native.get('canVerify') and native.get('cannotVerify'), 'Native report must retain its honesty bounds.')
    return cases


def combined_credits(world):
    """Retain both complete credit texts, including the original streamer notice."""
    return (world / 'CREDITS.txt').read_text() + '\n' + (world / 'PLACES-CREDITS.txt').read_text()


def validate_audio(repo, receipt, place_receipt):
    """Bind both approved manifests to the exact union of current audio files.

    Supplied signal receipts retain their explicit listening/import limits. This
    does not inspect Unity's packed resource archive or infer audible quality.
    """
    world = repo / 'Assets/Resources/Audio/World'
    sets = [('original', 'world_audio_sources.json', 'provenance.json', 'catalog.json', 'CREDITS.txt', receipt),
            ('expansion', 'place_audio_sources.json', 'places-provenance.json', 'places-catalog.json', 'PLACES-CREDITS.txt', place_receipt)]
    results, all_paths, all_guids = {}, set(), set()
    for label, source_name, provenance_name, catalog_name, credits_name, evidence in sets:
        spec = read_json(repo / 'Tools/Audio' / source_name)
        provenance = read_json(world / provenance_name)
        catalog = read_json(world / catalog_name)
        require(spec.get('schemaVersion') == provenance.get('schemaVersion') == catalog.get('schemaVersion') == 1,
                'Unknown audio manifest/catalog version.')
        rows = spec.get('clips', []); paths = [row['path'] for row in rows]
        count = len(paths)
        require(count > 0 and count == len(set(paths)) and not (set(paths) & all_paths), 'Duplicate/empty audio manifest.')
        require(evidence.get('passed') is True and evidence.get('assetCount') == count and evidence.get('findings') == [],
                label + ' audio receipt failed or has wrong count.')
        observed = evidence.get('assets', [])
        require(len(observed) == count and {row.get('path') for row in observed} == set(paths)
                and all(row.get('errors') == [] for row in observed), label + ' per-asset outcomes disagree.')
        require(provenance.get('sources') == spec.get('sources'), label + ' source provenance changed.')
        outputs = provenance.get('clips', [])
        require(len(outputs) == count and {row.get('path') for row in outputs} == set(paths), 'Incomplete audio provenance.')
        outputs = {row['path']: row for row in outputs}
        resources = {'Audio/World/' + str(pathlib.PurePosixPath(name).with_suffix('')) for name in paths}
        resource_rows = [row for key in ('music', 'ambience', 'sounds', 'details') for row in catalog.get(key, [])]
        require({row.get('resource') for row in resource_rows} == resources, label + ' catalog omits/references unapproved assets.')
        if label == 'expansion':
            expected = dict(schemaVersion=1, music=[], ambience=[], details=[], sounds=[])
            for row in rows:
                group = 'ambience' if row['kind'] == 'ambience' else ('details' if row['kind'] == 'detail' else 'sounds')
                expected[group].append(dict(id=row['cue'], resource='Audio/World/' + row['path'].removesuffix('.ogg'), gain=row['gain']))
            for alias in spec.get('ambienceAliases', []):
                target = next((row for row in rows if row['id'] == alias['target'] and row['kind'] == 'ambience'), None)
                require(target is not None, 'Invalid ambience alias target.')
                expected['ambience'].append(dict(id=alias['id'], resource='Audio/World/' + target['path'].removesuffix('.ogg'), gain=alias['gain']))
            require(catalog == expected, 'Expansion catalog differs from pinned cues/gains/aliases.')
        credits = (world / credits_name).read_text()
        hashes, importers, total, pcm = {}, {}, 0, 0
        for row in rows:
            name = row['path']; relative = pathlib.PurePosixPath(name)
            require(not relative.is_absolute() and '..' not in relative.parts, 'Unsafe audio source path.')
            path = world / name; info = outputs[name]
            require(path.is_file() and not path.is_symlink(), 'Missing/aliased audio: ' + name)
            hashes[name] = sha(path); total += path.stat().st_size
            require(hashes[name] == info.get('outputSha256') and path.stat().st_size == info.get('outputBytes'), 'Audio bytes/provenance mismatch: ' + name)
            for key, value in row.items():
                require(info.get(key) == value, 'Changed audio provenance: ' + name + ' ' + key)
            require(all(row.get(key) for key in ('author', 'title', 'page', 'license', 'licenseUrl', 'credit', 'changes', 'source')),
                    'Missing attribution: ' + name)
            require(row['credit'] in credits and row['page'] in credits and row['licenseUrl'] in credits,
                    'Shipped credits omit attribution/source/license: ' + name)
            require(row['license'] in ('CC0-1.0', 'CC-BY-4.0'), 'Unapproved license: ' + name)
            if row['kind'] == 'music':
                require(info.get('sourceDecodedFrames', 0) > 0 and info['sourceDecodedFrames'] == info.get('preparedFrames'), 'Music was cut: ' + name)
            short = row['kind'] in ('sfx', 'detail')
            if short: pcm += info.get('preparedFrames', 0) * 4
            meta = path.with_suffix(path.suffix + '.meta')
            require(meta.is_file() and not meta.is_symlink(), 'Missing/aliased importer: ' + name)
            text = meta.read_text(); match = re.search(r'^guid: ([0-9a-f]{32})$', text, re.M)
            require(match and match.group(1) not in all_guids, 'Invalid/duplicate audio GUID: ' + name)
            all_guids.add(match.group(1)); importers[name] = sha(meta)
            policy = {'loadType': 0, 'preloadAudioData': 1, 'loadInBackground': 0} if short else {'loadType': 2, 'preloadAudioData': 0, 'loadInBackground': 1}
            for key, value in {**policy, 'compressionFormat': 1, 'normalize': 0}.items():
                require(re.search(r'^\s*' + key + ': ' + str(value) + r'\s*$', text, re.M), 'Wrong import policy: ' + name + ' ' + key)
        budget = spec.get('budgets', {})
        require(total == evidence.get('audioBytes') and total <= budget.get('compressedBytes', 32 * 1024 * 1024), 'Audio compressed receipt/budget mismatch.')
        evidence_pcm = evidence.get('shortDecodedFloatBytes' if label == 'expansion' else 'sfxDecodedFloatBytes', 0)
        # The original receipt counts decoded Vorbis padding; provenance counts
        # container frames. Keep its original bound without demanding equality.
        require(0 < pcm <= evidence_pcm + 1152 * 4 * count and 0 < evidence_pcm <= budget.get('shortDecodedFloatBytes', 2 * 1024 * 1024), 'Short cue decoded budget failed.')
        results[label] = dict(asset_count=count, audio_bytes=total, short_decoded_float_bytes=evidence_pcm,
                              source_asset_sha256=hashes, importer_sha256=importers,
                              credits_sha256=sha(world / credits_name), catalog_sha256=sha(world / catalog_name),
                              source_manifest_sha256=sha(repo / 'Tools/Audio' / source_name), provenance_sha256=sha(world / provenance_name))
        all_paths.update(paths)
    actual_paths = {path.relative_to(world).as_posix() for path in world.rglob('*.ogg')}
    require(actual_paths == all_paths, 'World audio differs from exact original-plus-expansion union.')
    license_hashes = {}
    for name in LICENSE_FILES:
        path = world / 'Licenses' / name
        require(path.is_file() and not path.is_symlink(), 'Missing/aliased complete license: ' + name)
        license_hashes[name] = sha(path)
        require(license_hashes[name] == LICENSE_SHA256[name], 'Complete source license changed: ' + name)
    credits = combined_credits(world)
    require('Content ID' in credits and 'video description' in credits and 'music volume/mute' in credits, 'Streamer notice missing.')
    return dict(asset_count=len(all_paths), **results, license_sha256=license_hashes,
                combined_credits_sha256=hashlib.sha256(credits.encode()).hexdigest(), subjective_listening_verified=False,
                verification_bound='Exact current source audio union/importers/catalogs/licenses checked; supplied signal receipts retained. Packed Unity resources, subjective listening and physical Deck performance are not inferred.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('native_report', type=pathlib.Path)
    parser.add_argument('--repo', type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[2])
    parser.add_argument('--build-report', type=pathlib.Path, required=True)
    parser.add_argument('--editmode-report', type=pathlib.Path, required=True)
    parser.add_argument('--asset-report', type=pathlib.Path, required=True)
    parser.add_argument('--place-asset-report', type=pathlib.Path, required=True)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--folder', type=pathlib.Path, default=pathlib.Path('Builds/SteamDeck/2026-10-11-place-audio-current'))
    args = parser.parse_args()
    repo = args.repo.resolve()
    folder = (args.folder if args.folder.is_absolute() else repo / args.folder).resolve()
    require(folder.is_relative_to((repo / 'Builds/SteamDeck').resolve()) and folder.name == '2026-10-11-place-audio-current',
            'Package destination must be the NEW 2026-10-11-place-audio-current SteamDeck folder.')
    player = folder / 'CavesOfOoo-current'
    archive_root = player.name
    audit = folder / 'audit'
    build_path = args.build_report.resolve()
    tests_path = args.editmode_report.resolve()
    asset_path = args.asset_report.resolve()
    place_asset_path = args.place_asset_report.resolve()
    native_path = args.native_report.resolve()

    def git(*arguments):
        return subprocess.check_output(['git', *arguments], cwd=repo, text=True).strip()

    commit = git('rev-parse', 'HEAD')
    require(re.fullmatch(r'[0-9a-f]{40}', commit), 'Expected a full current HEAD commit.')
    require(args.source_commit == commit, '--source-commit must equal the full current HEAD hash.')
    # Keep revision metadata honest. Output receipts and unrelated art work are
    # not forced into a commit; production code and shipped instructions must be.
    source_scopes = ('Assets/Scripts', 'Assets/Editor', 'Assets/Resources', 'Assets/Scenes', 'Tools/SteamDeck', 'Tools/Audio')
    changed = git('diff', '--name-only', 'HEAD', '--', *source_scopes)
    require(not changed, 'Commit the final production source/instructions first:\n' + changed)
    untracked = git('ls-files', '--others', '--exclude-standard', '--', *source_scopes).splitlines()
    relevant_untracked = [name for name in untracked if pathlib.Path(name).suffix in ('.cs', '.asmdef', '.asmref')
                          or name.startswith(('Assets/Resources/', 'Assets/Scenes/', 'Tools/SteamDeck/', 'Tools/Audio/'))]
    require(not relevant_untracked, 'Uncommitted production source/instructions:\n' + '\n'.join(relevant_untracked))

    build = read_json(build_path)
    data = build.get('data', {})
    require(build.get('success') is True and data.get('result') == 'succeeded'
            and data.get('errors') == 0, 'Fresh build report is not successful with zero errors.')
    require(data.get('platform') == 'StandaloneLinux64', 'Build report is not Linux x86_64.')
    started = datetime.datetime.fromisoformat(data['started_at'].replace('Z', '+00:00'))
    completed = datetime.datetime.fromisoformat(data['completed_at'].replace('Z', '+00:00'))
    commit_time = int(git('show', '-s', '--format=%ct', commit))
    require(started.tzinfo is not None and completed.tzinfo is not None
            and completed >= started and started.timestamp() >= commit_time,
            'Build must be completed after the final HEAD commit; regenerate stale build evidence.')
    reported_output = pathlib.Path(data['output_path'])
    if not reported_output.is_absolute():
        reported_output = repo / reported_output
    executable = player / 'CavesOfOoo.x86_64'
    require(reported_output.resolve() == executable.resolve(), 'Build report points at another build directory.')
    require(executable.is_file() and (player / 'CavesOfOoo_Data').is_dir()
            and (player / 'UnityPlayer.so').is_file(), 'Fresh Linux player payload is incomplete.')
    with executable.open('rb') as stream:
        elf = stream.read(20)
    require(len(elf) == 20 and elf[:4] == b'\x7fELF' and elf[4:6] == b'\x02\x01'
            and int.from_bytes(elf[18:20], 'little') == 62, 'Player is not a 64-bit little-endian x86_64 ELF.')

    test_receipt = ET.parse(tests_path).getroot()
    summary = validate_tests(test_receipt)
    tests = {'mode': 'EditMode', 'id': test_receipt.get('id'), 'start_time': test_receipt.get('start-time'),
             'end_time': test_receipt.get('end-time'), 'duration_seconds': float(test_receipt.get('duration', '0'))}

    native = read_json(native_path)
    cases = validate_native(native)
    audio_validation = validate_audio(repo, read_json(asset_path), read_json(place_asset_path))
    screenshots = native.get('screenshots', [])
    require(screenshots and all(pathlib.Path(name).is_file() for name in screenshots), 'Native screenshot evidence is missing.')

    archive = folder / 'CavesOfOoo-SteamDeck-place-audio-current.tar.gz'
    require(not archive.exists() and not archive.is_symlink(),
            'Refusing to replace an existing delivery archive: ' + str(archive))
    require(not audit.is_symlink() and audit.resolve().is_relative_to(folder),
            'Audit directory must resolve inside the build folder without an alias.')
    receipt_inputs = {'build': build_path, 'final_editmode': tests_path, 'final_native_audio': native_path, 'audio_asset_contract': asset_path, 'place_audio_asset_contract': place_asset_path}
    derived_outputs = [archive.with_name(archive.name + '.partial'),
                       archive.with_suffix(archive.suffix + '.sha256'), audit / 'package-checks.json']
    derived_outputs.extend(audit / (name + '-receipt' + source.suffix)
                           for name, source in receipt_inputs.items())
    for path in derived_outputs:
        require(not path.is_symlink(), 'Refusing to write through an output alias: ' + str(path))

    # Validate destination links before any copy, chmod or metadata write.
    require(not player.is_symlink() and player.resolve().is_relative_to(folder), 'Player directory is an external alias.')
    payload_root = player.resolve()
    for path in player.rglob('*'):
        if path.is_symlink():
            require(not pathlib.Path(path.readlink()).is_absolute() and path.resolve().is_relative_to(payload_root),
                    'External or nonportable payload symlink: ' + str(path))
    for name in ('Launch-CavesOfOoo.sh', 'README-Steam-Deck.md', 'build-info.json', 'SHA256SUMS', 'AUDIO-CREDITS.txt', 'Audio-Provenance.json', 'Audio-Asset-Verification.json', 'Audio-Source-Manifest.json', 'PLACES-CREDITS.txt', 'Place-Audio-Provenance.json', 'Place-Audio-Source-Manifest.json', 'Place-Audio-Asset-Verification.json'):
        require(not (player / name).is_symlink(), 'Refusing to replace metadata through a symlink: ' + name)

    require(not (player / 'Audio-Licenses').is_symlink(), 'Refusing to replace license directory through a symlink.')
    # Validate all evidence before modifying the fresh player or creating an archive.
    audit.mkdir(parents=True, exist_ok=True)
    for name in ('Launch-CavesOfOoo.sh', 'README-Steam-Deck.md'):
        source = repo / 'Tools/SteamDeck' / name
        require(source.is_file(), 'Missing shipped instruction/launcher: ' + str(source))
        shutil.copyfile(source, player / name)
    world = repo / 'Assets/Resources/Audio/World'
    (player / 'AUDIO-CREDITS.txt').write_text(combined_credits(world))
    shutil.copyfile(world / 'PLACES-CREDITS.txt', player / 'PLACES-CREDITS.txt')
    shutil.copyfile(world / 'places-provenance.json', player / 'Place-Audio-Provenance.json')
    shutil.copyfile(repo / 'Tools/Audio/place_audio_sources.json', player / 'Place-Audio-Source-Manifest.json')
    shutil.copyfile(place_asset_path, player / 'Place-Audio-Asset-Verification.json')
    shutil.copyfile(world / 'provenance.json', player / 'Audio-Provenance.json')
    shutil.copyfile(repo / 'Tools/Audio/world_audio_sources.json', player / 'Audio-Source-Manifest.json')
    shutil.copyfile(asset_path, player / 'Audio-Asset-Verification.json')
    license_dir = player / 'Audio-Licenses'
    license_dir.mkdir(exist_ok=True)
    for name in LICENSE_FILES:
        require(not (license_dir / name).is_symlink(), 'Refusing to write a license through an alias.')
        shutil.copyfile(world / 'Licenses' / name, license_dir / name)
    for name in ('CavesOfOoo.x86_64', 'Launch-CavesOfOoo.sh'):
        (player / name).chmod(0o755)
    receipt_hashes = {}
    for name, source in receipt_inputs.items():
        destination = audit / (name + '-receipt' + source.suffix)
        if source != destination.resolve():
            shutil.copyfile(source, destination)
        receipt_hashes[name] = {'file': destination.name, 'sha256': sha(destination)}
    version = re.search(r'^m_EditorVersion:\s*(.+)$', (repo / 'ProjectSettings/ProjectVersion.txt').read_text(), re.M)
    info = {
        'commit': commit,
        'source_tree': git('rev-parse', 'HEAD^{tree}'),
        'packaged_at_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'unity_version': version.group(1) if version else None,
        'platform': 'Linux x86_64',
        'package_root': archive_root,
        'packaging_script_sha256': sha(pathlib.Path(__file__)),
        'delivery_readme_adjustment': 'None: shipped README is byte-identical to the committed Tools/SteamDeck source.',
        'build_report': build,
        'receipt_sha256': receipt_hashes,
        'tracked_configuration_changes_at_packaging': git('diff', '--name-only', 'HEAD', '--', 'Packages', 'ProjectSettings').splitlines(),
        'validation': {
            'selected_final_editmode': {**tests, **summary},
            'selected_final_audio_native': {
                'run_id': native['runId'], 'mode': native['mode'], 'cases': native['cases'],
                'passed': len(cases), 'failed': native['failures'], 'unexpected_errors': native['unexpectedErrors'],
                'complete': native['complete'], 'seconds': native.get('seconds'), 'screenshots': len(screenshots),
                'checks': cases, 'can_verify': native.get('canVerify'), 'cannot_verify': native.get('cannotVerify'),
            },
            'audio_assets': audio_validation,
            'source_correspondence': 'The coordinated implementation workflow selected these final receipts before committing unchanged production source. Receipt files do not independently embed a source hash.',
            'source_correspondence_note': 'These receipts do not contain a source revision hash. Their correspondence to this HEAD is established by the coordinated test, commit, then build workflow, not by machine-verified receipt revision identity.',
            'counting_note': 'EditMode tests and native scenario checks are distinct evidence and are not added together. Counts come directly from the final receipts; skipped tests remain explicit.',
            'physical_steam_deck_tested': False,
            'physical_controller_audio_performance_tested': False,
            'linux_player_runtime_tested': False,
            'performance_note': 'Native Unity Mac/editor tests and profiling are evidence for observed routes only. Audio output samples and source counters do not establish subjective sound quality, continuous recording quality, Linux runtime behavior, or Steam Deck frame rate, thermals, battery life, audio latency and physical controller feel. Previously rejected native-paint/contact raster experiments are not claimed as shipped speedups. No new numeric speedup is inferred by this packager.',
        },
    }
    (player / 'build-info.json').write_text(json.dumps(info, indent=2) + '\n')

    # A package must not follow an external symlink into unrelated host files.
    payload_root = player.resolve()
    for path in player.rglob('*'):
        if path.is_symlink():
            require(not pathlib.Path(path.readlink()).is_absolute() and path.resolve().is_relative_to(payload_root),
                    'External or nonportable payload symlink: ' + str(path))
    files = sorted(path for path in player.rglob('*') if path.is_file()
                   and path != player / 'SHA256SUMS' and path.name != '.DS_Store')
    checks = {path.relative_to(player).as_posix(): sha(path) for path in files}
    require(all('\n' not in name and '\r' not in name for name in checks), 'Unsupported newline in payload filename.')
    manifest = ''.join(f'{value}  {name}\n' for name, value in checks.items()).encode()
    (player / 'SHA256SUMS').write_bytes(manifest)
    partial = archive.with_name(archive.name + '.partial')
    try:
        with tarfile.open(partial, 'w:gz', compresslevel=6) as tar:
            tar.add(player, arcname=archive_root, filter=lambda item:
                    None if pathlib.PurePosixPath(item.name).name == '.DS_Store' else item)
        with tarfile.open(partial, 'r:gz') as tar:
            members = tar.getmembers()
            names = [member.name for member in members]
            require(len(names) == len(set(names)), 'Duplicate archive member names.')
            require(all(name == archive_root or name.startswith(archive_root + '/') for name in names)
                    and all('..' not in pathlib.PurePosixPath(name).parts for name in names), 'Unsafe archive path.')
            archived_files = {member.name.removeprefix(archive_root + '/') for member in members
                              if member.isfile() or member.issym() or member.islnk()}
            require(archived_files == set(checks) | {'SHA256SUMS'}, 'Archive file list differs from checksum manifest.')
            for name, expected in checks.items():
                stream = tar.extractfile(archive_root + '/' + name)
                require(stream is not None, 'Missing archive payload: ' + name)
                with stream:
                    require(stream_sha(stream) == expected, 'Archive digest mismatch: ' + name)
            with tar.extractfile(archive_root + '/SHA256SUMS') as stream:
                require(stream.read() == manifest, 'Archived checksum manifest differs.')
            for name in ('CavesOfOoo.x86_64', 'Launch-CavesOfOoo.sh'):
                require(tar.getmember(archive_root + '/' + name).mode & 0o111 == 0o111, 'Executable permission missing: ' + name)
        require(git('rev-parse', 'HEAD') == commit, 'HEAD changed during packaging.')
        require(not git('diff', '--name-only', 'HEAD', '--', *source_scopes), 'Production source changed during packaging.')
        # Same-directory hard link publishes atomically without replacing a file
        # that appeared after the initial no-overwrite check.
        archive.hardlink_to(partial)
    finally:
        if partial.exists():
            partial.unlink()
    archive_sha = sha(archive)
    archive.with_suffix(archive.suffix + '.sha256').write_text(archive_sha + '  ' + archive.name + '\n')
    result = {'archive': str(archive), 'commit': commit, 'bytes': archive.stat().st_size,
              'sha256': archive_sha, 'verified_payload_files': len(checks),
              'archive_stream_digests_match': True, 'archive_file_list_matches': True,
              'executable_permissions': True, 'editmode': summary,
              'native_run_id': native['runId'], 'native_cases': native['cases'], 'native_failures': 0,
              'audio_assets': audio_validation['asset_count'], 'audio_credits_and_licenses_included': True,
              'physical_steam_deck_tested': False}
    (audit / 'package-checks.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
