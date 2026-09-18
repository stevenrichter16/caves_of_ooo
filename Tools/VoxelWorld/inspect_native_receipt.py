#!/usr/bin/env python3
"""Read-only independent recomputation of archived native voxel evidence."""
from __future__ import annotations

import argparse
import csv
import gzip
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Docs/Verification/VoxelWorld'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_profile(path):
    profile = json.loads(path.read_text())
    raw = Path(profile['rawFrames'])
    assert sha(raw) == profile['rawSha256'], str(raw)
    with gzip.open(raw, 'rt') as stream:
        rows = list(csv.DictReader(stream))
    assert len(rows) == profile['frames']
    assert [int(r['sample']) for r in rows] == list(range(len(rows)))
    phases = []
    for phase_id, phase in enumerate(profile['phases']):
        selected = [r for r in rows if int(r['phase_id']) == phase_id]
        assert len(selected) == phase['frames']
        assert int(selected[0]['sample']) == phase['firstFrame']
        assert all(r['zone'] == phase['zoneId'] and r['kind'] == phase['kind'] for r in selected)
        assert all(int(b['unity_frame']) == int(a['unity_frame']) + 1 for a, b in zip(selected, selected[1:]))
        assert all(float(b['seconds']) > float(a['seconds']) for a, b in zip(selected, selected[1:]))
        for metric in [m for m in profile['metrics'] if m['phase'] == phase_id]:
            column = 'engine_frame_ms' if metric['name'] == 'Engine frame duration' else metric['name']
            values = sorted(float(r[column]) for r in selected if r[column] != 'NA')
            assert len(values) == metric['count'] and len(selected) - len(values) == metric['missing']
            if not values:
                assert not metric['available']
                continue
            expected = {'mean': sum(values) / len(values), 'max': values[-1],
                        'p95': values[math.ceil(len(values) * .95) - 1],
                        'p99': values[math.ceil(len(values) * .99) - 1]}
            for key, value in expected.items():
                assert math.isclose(value, metric[key], abs_tol=1e-7, rel_tol=1e-9), (phase_id, metric['name'], key)
        frames = [float(r['engine_frame_ms']) for r in selected]
        witnesses = {}
        for column in ['engine_frame_ms', 'Main Thread', 'GC Allocated In Frame', 'COO.ZoneRenderer.LateUpdate']:
            witness = max(selected, key=lambda r: float(r[column]))
            witnesses[column] = {key: witness[key] for key in ['sample', 'unity_frame', 'seconds', 'engine_frame_ms',
                                                              'Main Thread', 'GC Allocated In Frame', 'COO.ZoneRenderer.LateUpdate']}
        metrics = {m['name']: {k: m[k] for k in ['unit', 'mean', 'max', 'p95', 'p99']}
                   for m in profile['metrics'] if m['phase'] == phase_id and m['available']}
        phases.append({'kind': phase['kind'], 'frames': len(selected), 'seconds': phase['seconds'],
                       'engineFramesOver16_667ms': sum(v > 16.667 for v in frames),
                       'engineFramesOver33_333ms': sum(v > 33.333 for v in frames),
                       'engineFramesOver100ms': sum(v > 100 for v in frames),
                       'gcCollections': phase['endGc0'] - phase['startGc0'],
                       'metrics': metrics, 'maxWitnesses': witnesses})
    return profile, {'path': str(path), 'sha256': sha(path), 'rawPath': str(raw), 'rawSha256': sha(raw),
                     'allRawMetricsRecomputed': True, 'frames': len(rows), 'phases': phases}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('run', help='Existing named run directory under Docs/Verification/VoxelWorld')
    args = parser.parse_args(argv)
    run = (BASE / args.run).resolve()
    if run.parent != BASE.resolve():
        parser.error('Run must be one direct child directory.')
    wrapper = json.loads((run / 'receipt.json').read_text())
    artifacts = run / 'artifacts'
    native_path = artifacts / ('VWN-' + wrapper['runId'] + '-native.json')
    native = json.loads(native_path.read_text())
    cleanup = json.loads((artifacts / ('VWN-' + wrapper['runId'] + '-cleanup.json')).read_text())
    assert wrapper['pass'] and wrapper['sourceFrozen'] and not wrapper['compileErrors']
    assert json.loads((run / 'sources-before.json').read_text()) == json.loads((run / 'sources-after.json').read_text())
    assert native['runId'] == wrapper['runId'] == cleanup['runId']
    assert native['failures'] == native['unexpectedErrors'] == cleanup['exitCode'] == 0
    assert len(native['checks']) == len({c['name'] for c in native['checks']}) and all(c['pass'] for c in native['checks'])
    assert {r['zoneId'] for r in native['region']} == {'Overworld.2.6.0', 'Overworld.3.6.0', 'Overworld.3.7.0', 'Overworld.4.7.0'}
    assert all(r['swapped'] > 0 and r['missing'] == 0 and r['active'] and not r['fullReveal'] for r in native['region'])
    assert len({r['playerId'] for r in native['region']}) == 1
    for edge in native['borders']:
        assert edge['pass'] and edge['tickAfter'] > edge['tickBefore']
        if edge['key'] in ('A', 'D'):
            assert edge['fromY'] == edge['toY'] and {edge['fromX'], edge['toX']} == {0, 79}
        else:
            assert edge['fromX'] == edge['toX'] and {edge['fromY'], edge['toY']} == {0, 24}
    for row in wrapper['artifacts']:
        assert sha(Path(row['archive'])) == row['sha256'] == sha(Path(row['path']))
    for suffix in ['owners', 'tiles']:
        assert (artifacts / f"VWN-{wrapper['runId']}-saved-{suffix}.json").read_bytes() == (artifacts / f"VWN-{wrapper['runId']}-loaded-{suffix}.json").read_bytes()
    assert len(native['screenshots']) == 7
    for file in native['screenshots']:
        header = Path(file).read_bytes()[:24]
        assert header[:8] == b'\x89PNG\r\n\x1a\n' and (int.from_bytes(header[16:20], 'big'), int.from_bytes(header[20:24], 'big')) == (1920, 1080)
    current, inspected = read_profile(artifacts / f"VWN-{wrapper['runId']}-profile-full.json")
    baselines = []
    for file in sorted((ROOT / 'Docs/Verification/MultiCellPilot').glob('MCN-*-profile-full.json')):
        old, evidence = read_profile(file)
        matched = all(old[k] == current[k] for k in ['worldSeed', 'screenWidth', 'screenHeight', 'gpu', 'cpu', 'graphicsApi', 'targetFrameRate', 'vSyncCount'])
        matched &= all(all(a[k] == b[k] for k in ['rtWidth', 'rtHeight', 'cameraHalfHeight', 'cameraAspect', 'kind'])
                       for a, b in zip(old['phases'], current['phases']))
        evidence['sameRecordedCameraAndHardwareSettings'] = matched
        baselines.append(evidence)
    result = {'runId': wrapper['runId'], 'nativeSha256': sha(native_path), 'integrityPass': True,
              'uniqueChecks': len(native['checks']), 'region': native['region'], 'borders': native['borders'],
              'profile': inspected, 'historicalBaselines': baselines,
              'limits': ['Integrity pass is not a frame-time budget pass.',
                         'All four chunks were captured; continuous profile covers only the south pilot.',
                         'Latest-completed profiler counters may lag the Unity frame; maxima do not identify a causal subsystem.',
                         'Historical runs are descriptive references, not a controlled A/B: current run caches four zones, older pilot runs cache two, and code/Editor background work can differ.',
                         'bindingSeconds=0 means no binding phase was sampled, not measured zero-cost binding.',
                         'Spell gate observes native effect meshes and bone motion during an actual cast; it does not compare against an idle-bone control or prove every casting clip.']}
    output = run / 'independent-inspection.json'
    output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'output': str(output), 'integrityPass': True, 'frames': inspected['frames'], 'checks': len(native['checks']),
                      'baselines': len(baselines)}, indent=2))


if __name__ == '__main__':
    main()
