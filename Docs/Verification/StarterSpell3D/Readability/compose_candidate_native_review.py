#!/usr/bin/env python3
"""Explicit candidate review montage; this never grants or fabricates final acceptance.

Full native PNGs remain unchanged. Only separately identified title cards are added.
The final composer and its final-acceptance requirement are not altered or invoked.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
from PIL import Image

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('native_media_helpers', HERE/'compose_final_native_media.py')
helpers = importlib.util.module_from_spec(spec); spec.loader.exec_module(helpers)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('report', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--direction-review', action='store_true', help='Jet and Surge north/south/northeast instead of five cardinal showcases.')
    args = parser.parse_args()
    report_path = args.report.resolve(); out = args.output.resolve()
    helpers.require(not out.exists(), 'Use a new immutable candidate-review directory.')
    report_hash = helpers.sha(report_path); report = json.loads(report_path.read_text())
    helpers.require(report['workloadComplete'] and report['failures'] == report['unexpectedErrors'] == 0 and not report['fatal'], 'Actual native execution must pass before motion review.')
    helpers.require((report['screenWidth'], report['screenHeight']) == helpers.SIZE, 'Unexpected native dimensions.')
    cleanup_path = report_path.with_name(report_path.name.replace('-native.json', '-cleanup.json'))
    cleanup = json.loads(cleanup_path.read_text())
    helpers.require(cleanup['runId'] == report['runId'] and cleanup['exitCode'] == 0 and cleanup['finalNativeVerified'] and cleanup['privateRootRemoved'], 'Native cleanup must pass.')
    if args.direction_review:
        selection = [(spell, label, slug, mode) for spell, label, slug in helpers.PRIMARY[1:3]
                     for mode in ('direction-north', 'direction-south', 'direction-northeast')]
    else:
        selection = [(spell, label, slug, 'showcase') for spell, label, slug in helpers.PRIMARY]
    rows = []
    for spell, label, slug, mode in selection:
        matches = [row for row in report['casts'] if row['spell'] == spell and row['mode'] == mode]
        helpers.require(len(matches) == 1, 'Expected one actual captured case: '+spell+' '+mode)
        row = matches[0]
        helpers.require(row['pass'] and row['sawGesture'] and row['gestureRestored'] and row['resultStable'] and row['nativeEntry'] == spell, 'Actual outcome/gesture failed.')
        helpers.require(any(frame['meshes'] > 0 for frame in row['frames']), 'No captured visual evidence.')
        durations = helpers.capture_durations(row)
        for frame in row['frames']:
            path = Path(frame['path']).resolve()
            helpers.require(path.parent == report_path.parent and path.name.startswith('SSN-'+report['runId']+'-'), 'Foreign source frame.')
            helpers.require(helpers.sha(path) == frame['sha256'], 'Changed source frame.')
            with Image.open(path) as image:
                image.load(); helpers.require(image.format == 'PNG' and image.size == helpers.SIZE, 'Wrong capture format/dimensions.')
        rows.append((row, label, slug, durations))
    out.mkdir(parents=True); cards = out/'separate-review-title-cards'; cards.mkdir()
    timeline = []; cases = []; cursor = 0.0
    for row, label, slug, durations in rows:
        mode = row['mode']; card = cards/(slug+'-'+mode+'.png')
        helpers.title(card, 'REVIEW / CANDIDATE — '+label, mode+' | actual native captures follow at recorded 1x timing', helpers.SIZE)
        timeline.append({'kind':'explicit_review_title_pause', 'spell':row['spell'], 'mode':mode,
                         'start':cursor,'duration':.8,'encodedPath':str(card)})
        cursor += .8
        case_start = cursor
        for frame, duration in zip(row['frames'], durations):
            timeline.append({'kind':'unaltered_native_capture','spell':row['spell'],'mode':mode,'sourcePath':frame['path'],
                             'sourceSha256':frame['sha256'],'captureWallSeconds':frame['wallSeconds'],
                             'start':cursor,'duration':duration,'encodedPath':frame['path']})
            cursor += duration
        cases.append({'spell':row['spell'],'mode':mode,'videoStart':case_start,'nativeSampleSeconds':sum(durations),
                      'firstCaptureWallSeconds':row['frames'][0]['wallSeconds'],'completionWallSeconds':row['seconds'],
                      'captureTimes':[f['wallSeconds'] for f in row['frames']],
                      'maximumRecordedHoldSeconds':max(durations),'samples':len(durations)})
    video = helpers.encode_lossless(timeline, out/'REVIEW-candidate-native-1x.mp4', helpers.SIZE)
    helpers.require(helpers.sha(report_path) == report_hash, 'Native report changed during encoding.')
    for item in timeline:
        if item['kind'] == 'unaltered_native_capture':
            helpers.require(helpers.sha(item['sourcePath']) == item['sourceSha256'], 'Source PNG changed during encoding.')
    receipt = {'status':'PASS','stage':'candidate-motion-review','finalAcceptance':False,'runId':report['runId'],
               'nativeReport':str(report_path),'nativeReportSha256':report_hash,
               'cleanupReportSha256':helpers.sha(cleanup_path),'composerSha256':helpers.sha(__file__),
               'encodingHelperSha256':helpers.sha(HERE/'compose_final_native_media.py'),
               'video':video,'cases':cases,'timeline':timeline,'explicitReviewTitlePauseSeconds':.8*len(rows),
               'nativeSampleSeconds':sum(x['nativeSampleSeconds'] for x in cases),
               'scope':'Candidate motion review only, not final art acceptance. Full 1920x1080 native RGB pixels are lossless and decoded-verified. Recorded wall timestamps, including stalls, determine every hold. The only additions are separate0.8second REVIEW/CANDIDATE title pauses. Starts at each first available sample and ends at case completion; no missing motion, retouching, interpolation, camera or gameplay change. Roughly10captured samples/second cannot show motion between samples.'}
    (out/'review-media-verification.json').write_text(json.dumps(receipt,indent=2)+'\n')
    (out/'README.md').write_text('# REVIEW / candidate native motion\n\nThis is an inspection artifact, not final art acceptance. The full1920×1080 video preserves every captured source pixel and recorded wall-time interval, including stalls. Separate0.8second review title cards identify each case. Captures sample roughly10frames/sec; no in-between motion is invented. See review-media-verification.json for exact source hashes, captured timestamps, complete timeline and decoded-RGB/ffprobe checks. The final composer remains unchanged and still requires an explicit final acceptance pin.\n')
    print(json.dumps({'stage':receipt['stage'],'finalAcceptance':False,'video':video['path'],'nativeSampleSeconds':receipt['nativeSampleSeconds'],
                      'sourceFrames':sum(x['samples'] for x in cases),'encodedSeconds':video['encodedSeconds'],
                      'decodedRgbMatchesEveryInput':video['decodedRgbMatchesEveryInput'],'maximumPtsErrorSeconds':video['maximumPtsErrorSeconds']},indent=2))

if __name__ == '__main__': main()
