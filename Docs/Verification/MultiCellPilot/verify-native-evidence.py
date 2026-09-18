#!/usr/bin/env python3
"""Independently verify one completed native ridge run from retained artifacts."""
import argparse, csv, gzip, hashlib, json, math, struct
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('native', type=Path)
parser.add_argument('--output', type=Path)
args = parser.parse_args()
native_path = args.native.resolve()
native = json.loads(native_path.read_text())
run = native['runId']
stem = 'MCN-' + run
root = native_path.parent
findings = []
checks = []
def check(name, value, detail=None):
    checks.append({'name': name, 'pass': bool(value), **({'detail': detail} if detail else {})})
    if not value:
        findings.append(name + (': ' + str(detail) if detail else ''))

def close(a, b):
    return math.isclose(float(a), float(b), rel_tol=1e-6, abs_tol=1e-6)

check('native_path_owned_by_run', native_path.name == stem + '-native.json')
check('native_complete_and_clean', native['workloadComplete'] and native['failures'] == 0 and native['unexpectedErrors'] == 0)
for key in ['shutdownObserved', 'shutdownRootHeld', 'shutdownSavingUnregistered', 'displayPreferencesRestored', 'inputSettingsRestored']:
    check(key, native[key])
check('native_checks_all_pass', all(c['pass'] for c in native['checks']))
cleanup = json.loads((root / (stem + '-cleanup.json')).read_text())
check('cleanup_same_run_clean', cleanup['runId'] == run and cleanup['exitCode'] == 0 and cleanup['totalUnexpectedErrors'] == 0)
for key in ['privateRootRemoved', 'seedSettingRestored', 'scenesRestored', 'gameViewRestored', 'finalNativeVerified', 'inheritedSaveRootRestored', 'lastGamePreferenceRestored']:
    check(key, cleanup[key])
check('private_root_absent', not Path(cleanup['privateRoot']).exists())
profile = native['profile']
standalone = json.loads((root / (stem + '-profile-full.json')).read_text())
check('profile_embedded_matches_standalone', profile == standalone)
raw = Path(profile['rawFrames']).resolve()
check('raw_file_owned_by_run', raw == root / (stem + '-profile-full-frames.csv.gz'))
check('raw_file_sha256', hashlib.sha256(raw.read_bytes()).hexdigest() == profile['rawSha256'])
with gzip.open(raw, 'rt', newline='') as f:
    rows = list(csv.DictReader(f))
check('raw_exact_frame_count', len(rows) == profile['frames'])
check('raw_sample_sequence', [int(row['sample']) for row in rows] == list(range(len(rows))))
check('raw_unity_frames_strictly_advance', all(int(a['unity_frame']) < int(b['unity_frame']) for a, b in zip(rows, rows[1:])))
check('raw_time_strictly_advances', all(float(a['seconds']) < float(b['seconds']) for a, b in zip(rows, rows[1:])))
phase_stats = []
for i, phase in enumerate(profile['phases']):
    part = [row for row in rows if int(row['phase_id']) == i]
    check(f'phase_{i}_exact_frame_count', len(part) == phase['frames'])
    check(f'phase_{i}_first_frame', part and int(part[0]['sample']) == phase['firstFrame'])
    check(f'phase_{i}_identity', all(row['zone'] == phase['zoneId'] and row['kind'] == phase['kind'] for row in part))
    check(f'phase_{i}_raw_time_in_phase', all(float(row['seconds']) >= phase['startSeconds'] - 1e-3 and float(row['seconds']) <= phase['startSeconds'] + phase['seconds'] + 1e-3 for row in part))
    if phase['kind'].endswith('_idle'):
        check(f'phase_{i}_raw_idle_control', all((int(row['tick']), int(row['player_x']), int(row['player_y'])) == (phase['startTick'], phase['startX'], phase['startY']) for row in part))
    else:
        issued = [s for s in profile['steps'] if s['phase'] == i and s['inputIssued']]
        successful = [s for s in issued if s['pass']]
        check(f'phase_{i}_real_input_count', len(issued) == phase['inputAttempts'])
        check(f'phase_{i}_real_movement_count', len(successful) == phase['successfulSteps'] and len(successful) >= 2)
        check(f'phase_{i}_actual_cardinal_movement', all(abs(s['afterX'] - s['beforeX']) + abs(s['afterY'] - s['beforeY']) == 1 and (s['afterX'], s['afterY']) == (s['targetX'], s['targetY']) and s['afterTick'] > s['beforeTick'] and s['afterHp'] > 0 and s['inputState'] == 'Normal' for s in successful))
        check(f'phase_{i}_raw_position_and_tick_advance', len({(row['player_x'], row['player_y']) for row in part}) >= 2 and int(part[-1]['tick']) > int(part[0]['tick']))
    verified_metrics = 0
    for metric in [m for m in profile['metrics'] if m['phase'] == i]:
        column = 'engine_frame_ms' if metric['name'] == 'Engine frame duration' else metric['name']
        values = sorted(float(row[column]) for row in part if row[column] != 'NA' and math.isfinite(float(row[column])))
        check(f"phase_{i}_{metric['name']}_counts", len(values) == metric['count'] and len(part)-len(values) == metric['missing'] and sum(v > 0 for v in values) == metric['positive'])
        if metric['available']:
            computed = {'mean': math.fsum(values) / len(values), 'max': values[-1], 'p95': values[math.ceil(len(values)*.95)-1], 'p99': values[math.ceil(len(values)*.99)-1]}
            check(f"phase_{i}_{metric['name']}_summaries", all(close(value, metric[key]) for key, value in computed.items()), {'computed': computed, 'reported': {k: metric[k] for k in computed}} if not all(close(value, metric[key]) for key, value in computed.items()) else None)
            verified_metrics += 1
    phase_stats.append({'phase': phase['kind'], 'seconds': phase['seconds'], 'frames': len(part), 'verifiedAvailableMetrics': verified_metrics, 'successfulSteps': phase['successfulSteps']})
check('all_raw_phase_ids_known', all(0 <= int(row['phase_id']) < len(profile['phases']) for row in rows))
for label in ['south-entry', 'destructible-ridge', 'restored-save']:
    image_path = root / (stem + '-' + label + '.png')
    header = image_path.read_bytes()[:24]
    check(label + '_actual_1080_png', header[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', header[16:24]) == (1920,1080) and str(image_path) in native['screenshots'])
for suffix in ['owners', 'tiles']:
    before = (root / (stem + '-saved-' + suffix + '.json')).read_bytes()
    after = (root / (stem + '-loaded-' + suffix + '.json')).read_bytes()
    check(suffix + '_ledger_byte_identity', before == after)
owners = json.loads((root / (stem + '-loaded-owners.json')).read_text())['owners']
check('loaded_owners_unique_identity', len(owners) == len({row['id'] for row in owners}) == len({row['owner'] for row in owners}))
check('named_destroyed_ridge_remains_absent', all(row['owner'] != 'PilotRidgeNE-3-6' for row in owners))
check('named_surviving_ridge_and_pipe_present', {'PilotRidgeNE-6-2', 'movable-copper-pipe'} <= {row['owner'] for row in owners})
for owner in owners:
    cells = [tuple(map(int, value.split(','))) for value in owner['cells'].split(';')]
    check('body_' + owner['owner'], len(cells) == len(set(cells)) and all(0 <= owner['x']+x < 80 and 0 <= owner['y']+y < 25 for x,y in cells))
result = {'runId': run, 'nativeReport': str(native_path), 'passed': not findings, 'checks': len(checks), 'failures': findings, 'profilePhases': phase_stats, 'ownerCount': len(owners), 'rawSha256': profile['rawSha256'], 'bounds': 'Independent parsing of retained native PNG headers, gzip CSV rows, recorded phase/metric summaries and exact save/load ledgers. Does not assess image quality, prove absence of unobserved defects or establish a frame-rate budget.', 'details': checks}
if args.output:
    args.output.write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({key: value for key,value in result.items() if key != 'details'}, indent=2))
raise SystemExit(0 if not findings else 1)
