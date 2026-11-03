#!/usr/bin/env python3
"""Export an exact committed Unity project into a new, disposable Web build copy.

Never edits the desktop checkout. A sparse checkout is supported: Git supplies
tracked blobs, downloading missing objects through its configured remote.
"""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess

EXCLUDE = {
    'Assets/Editor/CodexMcpKickstart.cs',
    'Assets/Editor/Diagnostics/DiagQueryTool.cs',
    'Assets/Editor/Diagnostics/DiagCountTool.cs',
    'Assets/Editor/Diagnostics/DiagAssertTool.cs',
    'Assets/Editor/Diagnostics/DiagInspectRecordTool.cs',
}
ROOTS = ('Assets/', 'Packages/', 'ProjectSettings/')

def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args])

def prepare(repo, ref, output, dry_run=False):
    repo, output = repo.resolve(), output.resolve()
    if output == repo or repo in output.parents:
        raise ValueError("Build output must be outside the source checkout.")
    source = git(repo, 'rev-parse', '--verify', ref + '^{commit}').decode().strip()
    if output.exists():
        raise ValueError('Output must be a new directory; existing projects are never replaced.')
    records = []
    for record in git(repo, 'ls-tree', '-rz', source).split(b'\0'):
        if not record:
            continue
        header, raw_path = record.split(b'\t', 1)
        mode, kind, oid = header.decode().split()
        path = raw_path.decode('utf-8')
        if not path.startswith(ROOTS):
            continue
        if kind != 'blob' or mode not in ('100644', '100755') or '..' in PurePosixPath(path).parts:
            raise ValueError('Unsupported project entry: ' + path)
        records.append((oid, path, mode))
    paths = {path for _, path, _ in records}
    required = {'Assets/Scenes/SampleScene.unity', 'Assets/Editor/WebPhoneBuild.cs',
                'Assets/WebGLTemplates/CavesPhone/index.html', 'Packages/manifest.json'}
    required.update('Assets/WebGLTemplates/CavesPhone/' + name for name in
                    ('phone.js', 'phone.css', 'web-save-store.js', 'service-worker.js', 'manifest.webmanifest', 'icon.svg'))
    if required - paths:
        raise ValueError('Commit does not contain the complete Web port: ' + ', '.join(sorted(required - paths)))
    report = {'source': source, 'files': len(records), 'excludedEditorTools': sorted(EXCLUDE),
              'editor': git(repo, 'show', source + ':ProjectSettings/ProjectVersion.txt').decode().strip()}
    if dry_run:
        return report
    output.mkdir(parents=True)
    # cat-file requests only the selected paths. git archive on a partial clone
    # can prefetch unrelated historical art/verification blobs before filtering.
    process = subprocess.Popen(['git', '-C', str(repo), 'cat-file', '--batch'],
                               stdin=subprocess.PIPE, stdout=subprocess.PIPE)
    try:
        for oid, path, mode in records:
            if path.removesuffix('.meta') in EXCLUDE:
                continue
            process.stdin.write((oid + '\n').encode()); process.stdin.flush()
            header = process.stdout.readline().decode().split()
            if len(header) != 3 or header[1] != 'blob':
                raise RuntimeError('Missing Git blob: ' + path)
            remaining = int(header[2])
            destination = output / path
            destination.parent.mkdir(parents=True, exist_ok=True)
            with destination.open('wb') as file:
                while remaining:
                    block = process.stdout.read(min(remaining, 1024 * 1024))
                    if not block:
                        raise RuntimeError('Incomplete Git blob: ' + path)
                    file.write(block); remaining -= len(block)
            if process.stdout.read(1) != b'\n':
                raise RuntimeError('Invalid Git framing for ' + path)
            if mode == '100755':
                destination.chmod(0o755)
        process.stdin.close()
        if process.wait() != 0:
            raise RuntimeError('Git export failed.')
    finally:
        if process.poll() is None:
            process.terminate(); process.wait()
        process.stdin.close(); process.stdout.close()
    manifest_path = output / 'Packages/manifest.json'
    manifest = json.loads(manifest_path.read_text())
    manifest['dependencies'].pop('com.coplaydev.unity-mcp', None)
    manifest['dependencies']['com.unity.nuget.newtonsoft-json'] = '3.2.2'
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    lock_path = output / 'Packages/packages-lock.json'
    if lock_path.exists():
        lock = json.loads(lock_path.read_text())
        lock['dependencies'].pop('com.coplaydev.unity-mcp', None)
        if 'com.unity.nuget.newtonsoft-json' in lock['dependencies']:
            lock['dependencies']['com.unity.nuget.newtonsoft-json'].update(
                version='3.2.2', depth=0, source='registry', dependencies={}, url='https://packages.unity.com')
        lock_path.write_text(json.dumps(lock, indent=2) + '\n')
    # Broad preservation is a compatibility gate, to be narrowed after IL2CPP
    # reflection round trips and real phone download/memory measurements.
    preserve = output / 'Assets/WebPortPreservation'
    preserve.mkdir(exist_ok=True)
    (preserve / 'link.xml').write_text('<linker><assembly fullname="CavesOfOoo" preserve="all" /></linker>\n')
    report['manifestSha256'] = hashlib.sha256(manifest_path.read_bytes()).hexdigest()
    (output / '.coo-web-project.json').write_text(json.dumps(report, indent=2) + '\n')
    return report

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--ref', default='HEAD')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    print(json.dumps(prepare(args.repo.resolve(), args.ref, args.output.resolve(), args.dry_run), indent=2))
