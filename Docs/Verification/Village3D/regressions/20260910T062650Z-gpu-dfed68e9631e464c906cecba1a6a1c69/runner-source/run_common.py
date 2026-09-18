"""Reviewed-source launch wrappers. Import/default invocation never launches Unity."""
from __future__ import annotations
import argparse
import datetime as dt
import fcntl
import gzip
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import sys
import time
import uuid

ROOT = Path('/Users/steven/caves-of-ooo')
UNITY = Path('/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity')
MCP = Path('/Users/steven/unity-mcp/Server')
FIXED = ('V3D-after-native.json', 'V3D-after-cleanup.json', 'V3D-after-frames.csv')
ENTRY = {'gpu': 'CavesOfOoo.Editor.Village3DShaderGpuProbe.RunFromCommandLine',
         'native': 'CavesOfOoo.Editor.Village3DNativeAuditBatch.RunAfter'}
ENTRY_SOURCE = {'gpu': 'Assets/Editor/Art/Village3DShaderGpuProbe.cs',
                'native': 'Assets/Editor/Scenarios/Village3DNativeAuditBatch.cs'}


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def write_json(path, obj):
    # Each receipt is created once; no prior attempt can be overwritten.
    with Path(path).open('x') as f:
        json.dump(obj, f, indent=2, sort_keys=True)
        f.write('\n')


def process_table():
    rows = []
    result = subprocess.run(['/bin/ps', '-axo', 'pid=,comm='], check=True, capture_output=True, text=True)
    for line in result.stdout.splitlines():
        fields = line.strip().split(None, 1)
        if len(fields) == 2:
            rows.append((int(fields[0]), fields[1].strip()))
    return rows


def refuse_unity():
    editors = [(pid, name) for pid, name in process_table() if Path(name).name == 'Unity']
    if editors:
        raise RuntimeError('Concurrent Unity refused; never terminate an existing editor: ' + repr(editors))


def mcp_pids():
    # Match executable tokens, not filenames containing the word Unity or this runner.
    text = subprocess.run(['/bin/ps', '-axo', 'pid=,args='], check=True, capture_output=True, text=True).stdout
    result = []
    for line in text.splitlines():
        fields = line.strip().split(None, 1)
        if len(fields) == 2 and re.search(r'(?:^|\s|/)mcp-for-unity(?:\s|$)', fields[1]):
            pid = int(fields[0])
            if pid != os.getpid():
                result.append(pid)
    return result


def restart_mcp(server, run_id):
    refuse_unity()
    stopped = mcp_pids()
    for pid in stopped:
        try:
            os.kill(pid, signal.SIGTERM)
        except ProcessLookupError:
            pass
    time.sleep(2)  # Required shutdown settling interval; no Unity is running.
    remaining = mcp_pids()
    if remaining:
        raise RuntimeError('MCP did not stop in the required 2s interval; refusing launch: ' + repr(remaining))
    logs = Path('/tmp/codex-village-regression-mcp')
    logs.mkdir(exist_ok=True)
    live_log = logs / (run_id + '.log')
    uv = shutil.which('uv')
    if not uv:
        raise RuntimeError('uv is unavailable; refusing to launch without the requested MCP restart.')
    with live_log.open('xb') as log:
        process = subprocess.Popen([uv, 'run', 'mcp-for-unity', '--transport', 'http'], cwd=server,
                                   stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
    time.sleep(5)  # Required startup interval before the Unity launch.
    if process.poll() is not None:
        raise RuntimeError(f'MCP exited during 5s startup ({process.returncode}); inspect {live_log}')
    refuse_unity()
    return {'stopped_pids': stopped, 'new_pid': process.pid, 'live_log': str(live_log),
            'transport': 'http', 'shutdown_wait_seconds': 2, 'startup_wait_seconds': 5,
            'scope': 'Service process remains available afterwards; archived log is a final snapshot.'}


def sources(root):
    # Actual code, authored content, imported village/ring art and pipeline inputs.
    # Library/Temp/UserSettings and verification outputs are deliberately excluded.
    families = ('Assets/Scripts', 'Assets/Editor', 'Assets/Tests', 'Assets/Shaders', 'Assets/Art3D',
                'Assets/Resources', 'Assets/Settings', 'Assets/Scenes/Main', 'ProjectSettings', 'Packages')
    result = {}
    for family in families:
        folder = root / family
        if not folder.exists():
            continue
        for path in sorted(folder.rglob('*')):
            if path.is_file():
                result[str(path.relative_to(root))] = {'sha256': sha(path), 'bytes': path.stat().st_size}
    return result


class FixedOutputs:
    """Reserve fixed native filenames; keep previous files intact and restore exactly."""
    def __init__(self, live, run):
        self.live, self.run = live, run
        self.previous = run / 'preserved-fixed'
        self.artifacts = run / 'artifacts'
        self.rows = []
        self.reserved = False

    def reserve(self):
        self.previous.mkdir()
        self.artifacts.mkdir(exist_ok=True)
        if self.live.stat().st_dev != self.previous.stat().st_dev:
            raise RuntimeError('Fixed-output reservation requires the same filesystem for atomic renames.')
        for name in FIXED:
            path = self.live / name
            if path.is_symlink() or (path.exists() and not path.is_file()):
                raise RuntimeError('Refusing unexpected fixed output type: ' + str(path))
            self.rows.append({'name': name, 'existed': path.exists(), 'sha256': sha(path) if path.exists() else None})
        write_json(self.run / 'fixed-before.json', self.rows)
        self.reserved = True
        for row in self.rows:
            if row['existed']:
                # Destination was just created and must never replace another backup.
                destination = self.previous / row['name']
                if destination.exists():
                    raise RuntimeError('Backup collision: ' + str(destination))
                (self.live / row['name']).rename(destination)

    def collect_and_restore(self):
        if not self.reserved:
            return {'restored': True, 'scope': 'No fixed filenames reserved.'}
        receipt = {'restored': True, 'files': [], 'recovery': str(self.previous)}
        for row in self.rows:
            name = row['name']; live = self.live / name; saved = self.previous / name; produced = self.artifacts / name
            item = dict(row)
            try:
                # A partial reservation can leave an untouched original in place.
                if row['existed'] and not saved.exists():
                    item['restored'] = live.is_file() and sha(live) == row['sha256']
                    item['scope'] = 'Original was never moved.'
                else:
                    if live.exists() or live.is_symlink():
                        if live.is_symlink() or not live.is_file() or produced.exists():
                            raise RuntimeError('Refusing ambiguous new output; recover from ' + str(saved))
                        live.rename(produced)
                        item['new_archive'] = str(produced)
                        item['new_sha256'] = sha(produced)
                    if row['existed']:
                        if sha(saved) != row['sha256'] or live.exists():
                            raise RuntimeError('Previous output changed or restore destination occupied: ' + str(saved))
                        # Keep a durable backup; retain original timestamp/mode on restoration.
                        shutil.copy2(saved, live)
                        item['restored'] = sha(live) == row['sha256']
                    else:
                        item['restored'] = not live.exists()
            except Exception as error:
                item['restored'] = False
                item['error'] = repr(error)
                # Continue restoring the other names; never discard the durable backups.
            receipt['restored'] &= item['restored']
            receipt['files'].append(item)
        return receipt


def archive_native_screens(live, run, old_names):
    destination = run / 'artifacts' / 'screens'
    destination.mkdir(exist_ok=True)
    mapping = []
    for path in sorted(live.glob('V3D-after-*.png')):
        if path.name in old_names:
            continue
        if not re.fullmatch(r'V3D-after-[0-9a-f]{32}-.+\.png', path.name) or path.is_symlink():
            raise RuntimeError('Unexpected new screenshot path: ' + str(path))
        target = destination / path.name
        if target.exists():
            raise RuntimeError('Screenshot archive collision: ' + str(target))
        shutil.copy2(path, target)
        digest = sha(path)
        if sha(target) != digest:
            raise RuntimeError('Screenshot copy mismatch: ' + str(path))
        mapping.append({'original': str(path), 'archive': str(target), 'sha256': digest})
    return mapping


def log_diagnostics(path):
    text = path.read_text(errors='replace') if path.exists() else ''
    patterns = {'csharp': r'\berror CS\d+\b', 'shader': r'\bShader error\b',
                'exceptions': r'\b[A-Za-z_][\w.]*Exception(?::|\s*\()',
                'logged_errors': r'UnityEngine\.Debug:LogError\b|\bAssertion failed\b',
                'fatal': r'\b(?:SIGSEGV|Fatal error|Fatal Error|Crash!!!|Aborting batchmode due to failure)\b'}
    result = {'log_exists': path.is_file(), 'categories': {key: [] for key in patterns}}
    for number, line in enumerate(text.splitlines(), 1):
        for category, pattern in patterns.items():
            if re.search(pattern, line):
                result['categories'][category].append({'line': number, 'text': line})
    return result


def main(kind):
    parser = argparse.ArgumentParser(description='Review-safe village regression runner. Default prints a plan only.')
    parser.add_argument('--execute', action='store_true', help='Actually restart MCP and launch one owned Unity process.')
    parser.add_argument('--project', type=Path, default=ROOT)
    parser.add_argument('--unity', type=Path, default=UNITY)
    parser.add_argument('--mcp-server', type=Path, default=MCP)
    parser.add_argument('--archive-root', type=Path)
    parser.add_argument('--archive-stale-lock', action='store_true', help='After repeated no-Unity checks, archive an unheld leftover Temp/UnityLockfile.')
    parser.add_argument('--timeout', type=int, default=1200 if kind == 'native' else 600)
    args = parser.parse_args()
    root = args.project.resolve()
    archive = (args.archive_root or root / 'Docs/Verification/Village3D/regressions').resolve()
    run_id = uuid.uuid4().hex
    stamp = dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    run = archive / (stamp + '-' + kind + '-' + run_id)
    command = [str(args.unity), '-projectPath', str(root), '-logFile', str(run / 'unity.log'), '-executeMethod', ENTRY[kind]]
    if kind == 'gpu':
        command[1:1] = ['-batchmode', '-quit']
        command += ['-village3dGpuReport', str(run / 'artifacts/shader-gpu.json')]
    plan = {'kind': kind, 'review_only': not args.execute, 'runner_run_id': run_id, 'archive': str(run), 'command': command,
            'fixed_native_outputs': list(FIXED) if kind == 'native' else [], 'mcp_waits': [2, 5],
            'concurrency': 'Refuse any existing Unity process; never pkill Unity. Runner lock protects these two wrappers only.'}
    if not args.execute:
        print(json.dumps(plan, indent=2)); return 0
    if args.timeout < 60:
        parser.error('timeout must allow at least 60s')
    if not args.unity.is_file() or not (args.mcp_server / 'pyproject.toml').is_file():
        raise RuntimeError('Verified Unity executable/MCP project is missing.')
    source = root / ENTRY_SOURCE[kind]
    if not source.is_file() or ENTRY[kind].split('.')[-1] not in source.read_text():
        raise RuntimeError('Expected Editor entrypoint source is absent; re-review the contract.')
    lock_path = Path('/tmp/codex-village-regression-' + hashlib.sha256(str(root).encode()).hexdigest()[:16] + '.lock')
    with lock_path.open('a+') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise RuntimeError('Another village regression wrapper owns this project.')
        refuse_unity()
        stale = root / 'Temp/UnityLockfile'
        if stale.exists() and not args.archive_stale_lock:
            raise RuntimeError('Leftover UnityLockfile: no automatic deletion. Re-run with --archive-stale-lock after review; process/holder checks still apply.')
        run.mkdir(parents=True, exist_ok=False)
        (run / 'artifacts').mkdir()
        write_json(run / 'launch-plan.json', plan)
        runner_source = run / 'runner-source'
        runner_source.mkdir()
        for runner_path in sorted(Path(__file__).parent.iterdir()):
            if runner_path.is_file() and runner_path.suffix in ('.py', '.md'):
                shutil.copy2(runner_path, runner_source / runner_path.name)
        before = sources(root); write_json(run / 'source-hashes-before.json', before)
        live = root / 'Docs/Verification/Village3D'
        live.mkdir(parents=True, exist_ok=True)
        fixed = FixedOutputs(live, run)
        old_screens = {p.name for p in live.glob('V3D-after-*.png')}
        prior_run_ids = set()
        for path in live.rglob('*native.json'):
            try:
                prior_run_ids.add(json.loads(path.read_text()).get('runId'))
            except (ValueError, OSError):
                pass
        process = None; mcp = None; failure = None; returncode = None; forced = False; restored = None; screen_map = []
        started = utc(); start_time = time.monotonic()
        try:
            mcp = restart_mcp(args.mcp_server, run_id)
            write_json(run / 'mcp-start.json', mcp)
            if stale.exists():
                refuse_unity()
                lsof = shutil.which('lsof')
                if not lsof:
                    raise RuntimeError('lsof is required before archiving a leftover project lock.')
                holders = subprocess.run([lsof, '-t', str(stale)], capture_output=True, text=True)
                if holders.returncode not in (0, 1) or holders.stdout.strip():
                    raise RuntimeError('Project lock still has a holder or lsof failed; refusing launch.')
                if stale.is_symlink() or not stale.is_file():
                    raise RuntimeError('Unexpected project lock type.')
                shutil.copy2(stale, run / 'stale-UnityLockfile')
                if sha(stale) != sha(run / 'stale-UnityLockfile'):
                    raise RuntimeError('Stale lock backup mismatch.')
                refuse_unity(); stale.unlink()
            if kind == 'native':
                fixed.reserve()
            refuse_unity()
            with (run / 'unity-stdout.log').open('xb') as stdout:
                process = subprocess.Popen(command, cwd=root, stdout=stdout, stderr=subprocess.STDOUT)
                write_json(run / 'owned-unity-process.json', {'pid': process.pid, 'started_utc': utc(), 'command': command})
                try:
                    returncode = process.wait(timeout=args.timeout)
                except subprocess.TimeoutExpired:
                    raise RuntimeError('Owned Unity exceeded runner deadline; graceful termination will be attempted.')
        except (Exception, KeyboardInterrupt) as error:
            failure = repr(error)
        finally:
            if process is not None and process.poll() is None:
                try:
                    process.terminate()  # Only the PID this wrapper started.
                    try:
                        process.wait(timeout=30)
                    except subprocess.TimeoutExpired:
                        forced = True; process.kill(); process.wait(timeout=15)
                    returncode = process.returncode
                except Exception as error:
                    failure = (failure or '') + '\nOwned process stop: ' + repr(error)
            # Never restore names while an editor could still be writing them.
            try:
                refuse_unity()
            except Exception as error:
                failure = (failure or '') + '\nArchive deferred; recover preserved-fixed only after all Unity exits: ' + repr(error)
            else:
                if kind == 'native':
                    try:
                        screen_map = archive_native_screens(live, run, old_screens)
                        write_json(run / 'screenshot-map.json', screen_map)
                    except Exception as error:
                        failure = (failure or '') + '\nScreenshot archive: ' + repr(error)
                    finally:
                        # A screenshot failure must not prevent old fixed files from restoration.
                        try:
                            restored = fixed.collect_and_restore()
                            write_json(run / 'fixed-restoration.json', restored)
                        except Exception as error:
                            failure = (failure or '') + '\nFixed restore: ' + repr(error)
            # Also preserve startup failures, for which restart_mcp did not return a receipt.
            mcp_log = Path('/tmp/codex-village-regression-mcp') / (run_id + '.log')
            if mcp_log.exists():
                try:
                    shutil.copy2(mcp_log, run / 'mcp-server-snapshot.log')
                except Exception as error:
                    failure = (failure or '') + '\nMCP log snapshot: ' + repr(error)
        diagnostics = log_diagnostics(run / 'unity.log')
        stdout_diagnostics = log_diagnostics(run / 'unity-stdout.log')
        for category, rows in stdout_diagnostics['categories'].items():
            diagnostics['categories'][category].extend(dict(row, source='unity-stdout.log') for row in rows)
        write_json(run / 'log-diagnostics.json', diagnostics)
        cs = diagnostics['categories']['csharp']
        print('C# compiler errors:', len(cs), flush=True)  # Before every case/result summary.
        for row in cs:
            print(f"{row.get('source', 'unity.log')} line {row['line']}: {row['text']}", flush=True)
        after = sources(root); write_json(run / 'source-hashes-after.json', after)
        changes = sorted(path for path in set(before) | set(after) if before.get(path) != after.get(path))
        from validate_results import validate_gpu, validate_native
        validation = []
        try:
            validation = validate_gpu(run) if kind == 'gpu' else validate_native(run, live, prior_run_ids, screen_map)
        except Exception as error:
            validation.append({'check': 'result_validation_exception', 'pass': False, 'detail': repr(error)})
        passed = not failure and returncode == 0 and not forced and not changes and diagnostics['log_exists'] \
                 and all(not rows for rows in diagnostics['categories'].values()) and validation and all(r['pass'] for r in validation) \
                 and (kind != 'native' or bool(restored and restored['restored']))
        for name in ('unity.log', 'unity-stdout.log'):
            path = run / name
            if path.exists():
                with path.open('rb') as src, (run / (name + '.gz')).open('xb') as dst:
                    with gzip.GzipFile(filename='', mode='wb', fileobj=dst, mtime=0) as out:
                        shutil.copyfileobj(src, out)
        result = {'runner_run_id': run_id, 'kind': kind, 'status': 'PASS' if passed else 'FAIL', 'started_utc': started,
                  'finished_utc': utc(), 'wall_seconds': time.monotonic() - start_time, 'unity_exit_code': returncode,
                  'forced_owned_process_kill': forced, 'runner_failure': failure, 'source_inputs_unchanged': not changes,
                  'changed_source_inputs': changes, 'csharp_error_lines': len(cs), 'validation': validation,
                  'archive': str(run), 'bounds': 'Existing GPU12 or native35 regression only. Source/hash/full-log and receipt checks are wrapper-added; no new art, gameplay or performance claim.'}
        write_json(run / 'runner-result.json', result)
        manifest = {str(p.relative_to(run)): {'sha256': sha(p), 'bytes': p.stat().st_size}
                    for p in sorted(run.rglob('*')) if p.is_file()}
        write_json(run / 'archive-manifest.json', manifest)
        print('Regression', result['status'], 'Unity exit', returncode, 'Archive', run, flush=True)
        for check in validation:
            if not check['pass']:
                print('FAILED:', check['check'], check.get('detail', ''), flush=True)
        if failure:
            print(failure, flush=True)
        return 0 if passed else 1


if __name__ == '__main__':
    raise SystemExit('Use run_gpu.py or run_native.py; both default to plan-only.')
