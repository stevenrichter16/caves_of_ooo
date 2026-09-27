#!/usr/bin/env python3
"""Record only the reviewed native static importer outputs after complete success.
This checks filenames, completion receipt and stable bytes, not Unity geometry.
"""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
import re

SOURCE = 'ArtSource/SpreadNativeStyle3D/sources.json'
SOURCE_SHA = '449309b8275175530a35d1ee3affe6daef0f5aad0eb7fb57122c6753e9abf0c9'
FOLDER = 'Assets/Resources/SpreadNativeStyle3D'
EVIDENCE = 'Docs/Verification/DensityCompletion/SpreadBiome/Art/NativeStyle'
REPORT = EVIDENCE + '/native-import.json'
OUTPUT = EVIDENCE + '/native-import-owned-files.json'
COUNT = 868


def sha(data):
    return hashlib.sha256(data).hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def stable_bytes(path):
    require(not path.is_symlink() and path.is_file(), 'Expected regular owned file: ' + str(path))
    before = path.stat()
    value = path.read_bytes()
    after = path.stat()
    require((before.st_ino, before.st_size, before.st_mtime_ns) ==
            (after.st_ino, after.st_size, after.st_mtime_ns), 'File changed during capture: ' + str(path))
    require(len(value) > 0, 'Empty generated file: ' + str(path))
    return value


def expected_paths(source_bytes):
    require(sha(source_bytes) == SOURCE_SHA, 'Reviewed source checksum differs')
    source = json.loads(source_bytes)
    ids = [row['id'] for row in source['models']]
    require(len(ids) == COUNT and len(set(ids)) == COUNT, 'Expected exact868 unique source IDs')
    require(all(isinstance(x, str) and re.fullmatch(r'[a-z0-9]+(?:-[a-z0-9]+)*', x) for x in ids), 'Unsafe source ID')
    result = {FOLDER + '/' + model + suffix for model in ids
              for suffix in ('.asset', '.asset.meta', '.prefab', '.prefab.meta')}
    result.update((FOLDER + '.meta', FOLDER + '/Library.asset', FOLDER + '/Library.asset.meta'))
    require(len(result) == 3475, 'Exact3475 output set changed')
    return result


def capture(repo):
    source_bytes = stable_bytes(repo / SOURCE)
    expected = expected_paths(source_bytes)
    report_bytes = stable_bytes(repo / REPORT)
    report = json.loads(report_bytes)
    require(report.get('success') is True and report.get('modelCount') == COUNT and
            report.get('sourceSha256') == SOURCE_SHA and report.get('geometryChanged') is False,
            'Complete868 native success receipt is required')
    reused, written = report.get('reusedModels'), report.get('writtenModels')
    require(type(reused) is int and type(written) is int and reused >= 0 and written >= 0 and reused + written == COUNT,
            'Exact complete reused+written counters required')
    root = repo / FOLDER
    require(root.is_dir() and not root.is_symlink(), 'Owned resource root missing or linked')
    entries = list(root.rglob('*'))
    require(all(not p.is_symlink() and p.is_file() for p in entries), 'Unexpected directory or linked resource')
    actual = {p.relative_to(repo).as_posix() for p in entries}
    actual.add(FOLDER + '.meta')
    missing, extra = sorted(expected - actual), sorted(actual - expected)
    require(not missing and not extra, 'Output set mismatch: ' + json.dumps({'missing': missing, 'extra': extra}))
    files = [{'path': rel, 'sha256': sha(stable_bytes(repo / rel))} for rel in sorted(expected)]
    # Receipt/source changes or an output addition/removal during hashing invalidate the capture.
    require(stable_bytes(repo / SOURCE) == source_bytes and stable_bytes(repo / REPORT) == report_bytes,
            'Source or native report changed during capture')
    require({p.relative_to(repo).as_posix() for p in root.rglob('*')} | {FOLDER + '.meta'} == expected,
            'Output set changed during capture')
    result = {'status': 'complete-native-import-output-capture',
              'capturedUtc': datetime.now(timezone.utc).isoformat(),
              'sourcePath': SOURCE, 'sourceSha256': SOURCE_SHA, 'sourceModelCount': COUNT,
              'nativeImportReport': REPORT, 'nativeImportReportSha256': sha(report_bytes),
              'nativeImport': report, 'exactOutputCount': len(files),
              'scope': 'Only exact reviewed868 model mesh/prefab files and metas, final library pair and scoped folder meta. Stable byte capture does not establish native tests, geometry or visual acceptance.',
              'files': files}
    prior_path = repo / EVIDENCE / 'InterruptedImport/partial-output.json'
    if prior_path.is_file():
        prior = json.loads(stable_bytes(prior_path))
        now = {row['path']: row['sha256'] for row in files}
        rows = prior.get('files', [])
        require(all(row['path'] in expected for row in rows), 'Interrupted evidence names unrelated files')
        changed = [row['path'] for row in rows if now[row['path']] != row['sha256']]
        result['interruptedOutputComparison'] = {'capturedFiles': len(rows), 'unchangedFiles': len(rows) - len(changed), 'changedFiles': changed}
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo', type=Path, default=Path('/Users/steven/caves-of-ooo'))
    parser.add_argument('--check-only', action='store_true')
    args = parser.parse_args()
    repo = args.repo.resolve()
    result = capture(repo)
    if not args.check_only:
        destination = repo / OUTPUT
        require(not destination.exists(), 'Preserve existing manifest; choose explicit review before replacement')
        destination.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'fileCount': result['exactOutputCount'], 'reused': result['nativeImport']['reusedModels'],
                      'written': result['nativeImport']['writtenModels'], 'output': None if args.check_only else str(repo / OUTPUT)}, indent=2))


if __name__ == '__main__':
    main()
