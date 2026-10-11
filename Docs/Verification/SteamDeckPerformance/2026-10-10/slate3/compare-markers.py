"""Recompute paired native marker statistics from the retained raw observations.

Usage: python3 compare-markers.py BASELINE_DIR AFTER_DIR OUTPUT_JSON [CONTEXT]
Only positive observations of a marker count as active work. Markers overlap;
their durations are not additive. This reports a Mac editor route, not Deck FPS.
"""
import csv
from collections import Counter
import gzip
import hashlib
import json
import math
from pathlib import Path
import statistics
import sys


def summary(values):
    values = sorted(values)
    assert values and all(math.isfinite(x) and x >= 0 for x in values)
    return dict(count=len(values), median=statistics.median(values),
                p95=values[int((len(values)-1)*.95)], maximum=max(values))


def read(folder):
    folder = Path(folder)
    report = json.loads((folder/'report.json').read_text())
    markers = json.loads((folder/'profile-markers.json').read_text())
    assert report['complete'] and report['failures'] == 0 and report['unexpectedErrors'] == 0
    assert markers['normalIntervalComplete'] and not markers['overflow']
    raw = folder/'profile-markers.csv.gz'
    with gzip.open(raw, 'rt') as stream:
        rows = list(csv.DictReader(stream))
    assert len(rows) == markers['sampledFrames']
    units = {m['name']: m['units'] for m in markers['markers'] if m['valid']}
    values = {}
    for key, unit in units.items():
        active = [float(row[key]) for row in rows if row[key] and float(row[key]) > 0]
        if active:
            divisor = 1000000 if unit == 'TimeNanoseconds' else 1
            values[key] = dict(unit='ms' if divisor != 1 else unit,
                              **summary([v/divisor for v in active]))
    return dict(run=report['runId'], complete=report['complete'], cases=report['cases'],
                frames=len(rows), wallMs=summary([float(row['wallMs']) for row in rows]),
                positions=dict(Counter(row['x']+','+row['y'] for row in rows)),
                corridorOnly=all(row['x'] in ('40','41') and row['y']=='12' for row in rows),
                startTick=int(rows[0]['tick']), endTick=int(rows[-1]['tick']),
                seconds=report['profileSeconds'], before=markers['before'], after=markers['after'],
                rawSha256=hashlib.sha256(raw.read_bytes()).hexdigest(), activeMarkers=values)


baseline, after = read(sys.argv[1]), read(sys.argv[2])
settings = ['screenWidth','screenHeight','targetWidth','targetHeight','msaa','vSync',
            'targetFrameRate','filter','zone','runInBackground','focused','profilerEnabled','timeScale']
differences = [key for key in settings if len({str(run[phase][key])
              for run in (baseline, after) for phase in ('before','after')}) != 1]
result = dict(baseline=baseline, after=after, settingDifferences=differences,
              bothConfinedToCorridor=baseline['corridorOnly'] and after['corridorOnly'],
              context=sys.argv[4] if len(sys.argv)>4 else 'Original baseline has no world audio; after run includes world audio.',
              scope='Positive per-marker observations; latest completed native sample may be the preceding frame. Nested markers are not additive. Editor timing includes scheduling and GC, and does not prove Steam Deck performance. Exact population/AI outcome equivalence is not asserted by matching render settings.')
Path(sys.argv[3]).write_text(json.dumps(result, indent=2)+'\n')
print(json.dumps(dict(before=baseline['run'], after=after['run'], settingDifferences=differences,
                     fullRedrawBefore=baseline['activeMarkers']['COO.ZoneRenderer.RenderZone'],
                     fullRedrawAfter=after['activeMarkers']['COO.ZoneRenderer.RenderZone']), indent=2))
