#!/usr/bin/env python3
"""Read-only R00 comparison with exact reviewer-approved hash pins.

Without --approvals, every change is REVIEW_NEEDED. Approval JSON:
{"reviewedBy":"...","changes":[{"path":"...","change":"modified",
"beforeSha256":"...","afterSha256":"...","owner":"...","reason":"..."}]}
No wildcard/folder approval, source restoration, or automatic log exemptions.
"""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import subprocess

PROTECTED_PREFIXES = ('Assets/Scripts/Gameplay/', 'Assets/Resources/Content/', 'ProjectSettings/', 'Packages/')
PROTECTED_PATHS = {
    'Assets/Scripts/Presentation/Rendering/Village3DProjection.cs',
    'Assets/Scripts/Presentation/Rendering/NativeZone3DRenderSurface.cs',
}


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024*1024), b''):
            h.update(block)
    return h.hexdigest()


def enumerate_current(root, roots, previous):
    paths = set()
    for folder in roots:
        for path in (root/folder).rglob('*'):
            if '__pycache__' not in path.parts and (path.is_file() or path.is_symlink()):
                paths.add(path)
    for name in previous:
        path = root/name
        if path.exists() or path.is_symlink():
            paths.add(path)
    result = {}
    unstable = []
    for path in sorted(paths):
        name = str(path.relative_to(root))
        start = path.lstat()
        if path.is_symlink():
            target = str(path.readlink())
            item = {'sha256': hashlib.sha256(('symlink\0'+target).encode()).hexdigest(),
                    'bytes': start.st_size, 'kind': 'symlink', 'target': target}
        else:
            item = {'sha256': sha(path), 'bytes': start.st_size, 'kind': 'file'}
        end = path.lstat()
        if (start.st_size, start.st_mtime_ns, start.st_ino) != (end.st_size, end.st_mtime_ns, end.st_ino):
            unstable.append(name)
        result[name] = item
    return result, unstable


def compare(previous, current, approvals):
    pins = {}
    errors = []
    for pin in approvals.get('changes', []):
        path = pin.get('path')
        if not isinstance(path, str) or not path or Path(path).is_absolute() or '..' in Path(path).parts or path in pins:
            errors.append('Invalid or duplicate exact approval path: '+str(path))
            continue
        pins[path] = pin
    if pins and not approvals.get('reviewedBy'):
        errors.append('Explicit reviewer identity is required for approvals.')
    changes = []
    used = set()
    for name in sorted(set(previous) | set(current)):
        before = previous.get(name)
        after = current.get(name)
        before_hash = before.get('sha256') if before else None
        after_hash = after.get('sha256') if after else None
        if before_hash == after_hash:
            continue
        kind = 'added' if before is None else 'removed' if after is None else 'modified'
        protected = (name in PROTECTED_PATHS
                     or name.endswith('.meta') and name[:-5] in PROTECTED_PATHS
                     or name.startswith(PROTECTED_PREFIXES)
                     or before is not None and name.startswith('ArtSource/StarterSpell3D/revisions/'))
        pin = pins.get(name)
        exact = (pin is not None and pin.get('change') == kind
                 and pin.get('beforeSha256') == before_hash and pin.get('afterSha256') == after_hash)
        complete = exact and bool(pin.get('owner')) and bool(pin.get('reason')) and bool(approvals.get('reviewedBy'))
        approved = bool(complete and not protected and not errors)
        if approved:
            used.add(name)
        changes.append({'path': name, 'change': kind, 'beforeSha256': before_hash, 'afterSha256': after_hash,
                        'beforeBytes': before.get('bytes') if before else None,
                        'afterBytes': after.get('bytes') if after else None,
                        'protected': protected, 'approved': approved,
                        'reviewState': 'PROTECTED_CHANGE' if protected else 'APPROVED_EXACT_HASH' if approved else 'REVIEW_NEEDED',
                        'owner': pin.get('owner') if complete else None, 'reason': pin.get('reason') if complete else None})
    return changes, sorted(set(pins)-used), errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--root', type=Path, default=Path('/Users/steven/caves-of-ooo'))
    parser.add_argument('--baseline', type=Path, default=Path(__file__).resolve().parent/'R00-prechange')
    parser.add_argument('--approvals', type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    baseline = args.baseline.resolve()
    out = args.output.resolve()
    if out.exists():
        raise ValueError('Use a fresh immutable verification directory.')
    snap = json.loads((baseline/'snapshot.json').read_text())
    manifest_path = baseline/'source-hashes-before.json'
    if sha(manifest_path) != snap['manifestSha256']:
        raise ValueError('R00 manifest no longer matches its original snapshot receipt.')
    previous = json.loads(manifest_path.read_text())
    approvals = json.loads(args.approvals.read_text()) if args.approvals else {}
    current, unstable = enumerate_current(root, snap['roots'], previous)
    changes, unused, errors = compare(previous, current, approvals)
    git_before = (baseline/'git-status-before.txt').read_text().splitlines()
    git_now = subprocess.check_output(['git', 'status', '--short'], cwd=root, text=True).splitlines()
    report = {
        'status': 'PASS' if not unstable and not errors and not unused and all(c['approved'] for c in changes) else 'REVIEW_NEEDED',
        'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'baselineManifestSha256': sha(manifest_path), 'baselineHead': snap['head'],
        'currentHead': subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),
        'baselineFiles': len(previous), 'currentFiles': len(current),
        'unchangedFiles': len(set(previous)&set(current))-sum(c['change']=='modified' for c in changes),
        'changedFiles': len(changes), 'approvedChanges': sum(c['approved'] for c in changes),
        'unreviewedChanges': sum(not c['approved'] for c in changes),
        'protectedChanges': sum(c['protected'] for c in changes),
        'unstableDuringHash': unstable, 'approvalErrors': errors,
        'unusedOrMismatchedApprovals': unused, 'changes': changes,
        'approvalsSha256': sha(args.approvals) if args.approvals else None,
        'newGitStatusEntriesForSeparateReview': sorted(set(git_now)-set(git_before)),
        'scope': 'All R00 Assets/ProjectSettings/Packages/ArtSource/StarterSpell3D files plus exact extra R00 references. Every added, removed or modified file in this scope requires an exact reviewed before/after hash pin. Gameplay/content/camera/settings/package changes remain protected even with a pin. Source bytes are never restored or edited.',
        'limits': 'The supplementary git-status delta is not a hash proof of previously modified documents outside the R00 snapshot scope. PASS is explicitly limited to recorded R00 scope; unknown scoped changes are never automatically approved, including generated logs and metadata.',
    }
    out.mkdir(parents=True)
    (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    (out/'source-hashes-current.json').write_text(json.dumps(current,indent=2)+'\n')
    (out/'git-status-current.txt').write_text('\n'.join(git_now)+'\n')
    template = {'reviewedBy': '', 'changes': [
        {k:c[k] for k in ('path','change','beforeSha256','afterSha256')} | {'owner':'','reason':''}
        for c in changes]}
    (out/'review-required-template.json').write_text(json.dumps(template,indent=2)+'\n')
    lines = ['# R00 source preservation', '',
             f"**{report['status']}**: {len(changes)} changes, {report['approvedChanges']} exact reviewed pins, {report['unreviewedChanges']} unreviewed, {report['protectedChanges']} protected.",
             '', report['scope'], '', report['limits'], '',
             '| Path | Change | Review state |', '| --- | --- | --- |']
    lines += ['| '+c['path']+' | '+c['change']+' | '+c['reviewState']+' |' for c in changes]
    (out/'report.md').write_text('\n'.join(lines)+'\n')
    print(json.dumps({k:report[k] for k in ('status','baselineFiles','currentFiles','changedFiles',
                                         'approvedChanges','unreviewedChanges','protectedChanges')},indent=2))
    return 0 if report['status']=='PASS' else 2


if __name__ == '__main__':
    raise SystemExit(main())
