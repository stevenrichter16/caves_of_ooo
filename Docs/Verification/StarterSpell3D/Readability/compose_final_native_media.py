#!/usr/bin/env python3
"""Compose lossless final native media; never run against an unaccepted candidate.

Requires an explicit final-acceptance JSON with status PASS, stage final,
runId and nativeReportSha256 matching the completed native report.
No Unity calls. No source capture is rewritten. All output paths are fresh.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import subprocess
from PIL import Image, ImageDraw, ImageFont

PRIMARY = [
    ('Pyromancy_EmberSpit', 'Ember Spit', 'ember-spit'),
    ('Hydromancy_JetBlast', 'Jet Blast', 'jet-blast'),
    ('Galvanism_GroundSurge', 'Ground Surge', 'ground-surge'),
    ('Cryomancy_RimeGrip', 'Rime Grip', 'rime-grip'),
    ('Spellcraft_Calm', 'Calm', 'calm'),
]
APPENDIX = [('Pyromancy_FlamingHands', 'Flaming Hands', 'flaming-hands'),
            ('Hydromancy_ConjureRain', 'Conjure Rain', 'conjure-rain')]
SIZE = (1920, 1080)
DEFAULT_CROP = (512, 96, 1216, 624)  # 704x528, wider than prior 370x255 crop.


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def final_pin(report, report_hash, pin):
    require(pin.get('status') == 'PASS' and pin.get('stage') == 'final',
            'An explicit final acceptance is required; candidates are not final media.')
    require(pin.get('runId') == report.get('runId') and pin.get('nativeReportSha256') == report_hash,
            'Final acceptance belongs to a different run or report bytes.')
    require(report.get('workloadComplete') and report.get('failures') == report.get('unexpectedErrors') == 0
            and not report.get('fatal'), 'Native workload is not a complete successful run.')
    for key in ('shutdownObserved', 'shutdownRootHeld', 'shutdownSavingUnregistered',
                'displayPreferencesRestored', 'inputSettingsRestored'):
        require(report.get(key) is True, 'Native preservation flag failed: ' + key)
    require((report.get('screenWidth'), report.get('screenHeight')) == SIZE, 'Full GameView must be native 1920x1080.')


def capture_durations(row):
    frames = row['frames']
    require(len(frames) >= 2, 'At least two actual captured samples are required.')
    times = [f['wallSeconds'] for f in frames] + [row['seconds']]
    require(all(isinstance(t, (float, int)) and math.isfinite(t) and t >= 0 for t in times), 'Invalid capture timestamps.')
    require(all(b > a for a, b in zip(times, times[1:])), 'Capture timestamps/completion must increase strictly.')
    return [b - a for a, b in zip(times, times[1:])]


def quote_concat(path):
    # FFconcat syntax, not shell quoting; subprocess always receives argv directly.
    return "'" + str(path).replace("'", "'\\''") + "'"


def rgb_digest(path):
    with Image.open(path) as image:
        image.load()
        return hashlib.md5(image.convert('RGB').tobytes()).hexdigest()


def font(size):
    return ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf', size)


def title(path, label, subtitle, size):
    image = Image.new('RGB', size, (24, 27, 25))
    draw = ImageDraw.Draw(image)
    draw.text((64, size[1] // 2 - 70), label, font=font(54), fill=(235, 235, 219))
    draw.text((64, size[1] // 2 + 18), subtitle, font=font(28), fill=(170, 182, 167))
    image.save(path)


def encode_lossless(timeline, path, size):
    concat_path = path.with_suffix('.ffconcat')
    lines = ['ffconcat version 1.0']
    for item in timeline:
        lines += ['file ' + quote_concat(item['encodedPath']), 'option framerate 1000',
                  'duration %.9f' % item['duration']]
    # A final repeated sample establishes the preceding final duration in concat.
    lines += ['file ' + quote_concat(timeline[-1]['encodedPath']), 'option framerate 1000']
    concat_path.write_text('\n'.join(lines) + '\n')
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-f', 'concat', '-safe', '0',
                    '-i', str(concat_path), '-fps_mode', 'vfr',
                    '-vf', 'setsar=1',  # Metadata only; decoded RGB is verified below.
                    '-c:v', 'libx264rgb',
                    '-preset', 'veryfast', '-crf', '0', '-bf', '0', '-threads', '2',
                    '-pix_fmt', 'rgb24', '-video_track_timescale', '1000000',
                    '-movflags', '+faststart', str(path)], check=True)
    probe = json.loads(subprocess.check_output([
        'ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_frames', '-show_streams', '-show_format',
        '-show_entries', 'frame=best_effort_timestamp_time:stream=codec_name,profile,pix_fmt,width,height,sample_aspect_ratio,time_base:format=duration',
        '-of', 'json', str(path)]))
    stream = probe['streams'][0]
    require((stream['width'], stream['height']) == size and stream.get('sample_aspect_ratio') == '1:1', 'Video changed dimensions or pixel aspect.')
    pts = [float(frame['best_effort_timestamp_time']) for frame in probe['frames']]
    require(len(pts) == len(timeline) + 1, 'Encoded sample count differs from capture timeline.')
    expected = [item['start'] for item in timeline] + [timeline[-1]['start'] + timeline[-1]['duration']]
    error = max(abs(actual - target) for actual, target in zip(pts, expected))
    require(error <= .0011, 'Encoded PTS changed native timing by more than 1.1 ms.')
    decoded = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(path), '-map', '0:v:0',
                                      '-fps_mode', 'passthrough', '-pix_fmt', 'rgb24', '-f', 'framemd5', '-'], text=True)
    hashes = [line.rsplit(',', 1)[1].strip() for line in decoded.splitlines() if line and not line.startswith('#')]
    source_hashes = [rgb_digest(item['encodedPath']) for item in timeline]
    require(hashes == source_hashes + [source_hashes[-1]], 'Decoded MP4 RGB pixels differ from their actual source frames.')
    duration = float(probe['format']['duration'])
    require(abs(duration - expected[-1]) <= .0021, 'MP4 duration differs from the native sample timeline.')
    evidence = {'path': str(path), 'sha256': sha(path), 'width': size[0], 'height': size[1],
                'pixelAspect': '1:1', 'codec': 'lossless libx264rgb', 'decodedFrameCount': len(hashes),
                'decodedRgbMatchesEveryInput': True, 'maximumPtsErrorSeconds': error,
                'expectedSeconds': expected[-1], 'encodedSeconds': duration, 'ffprobe': probe}
    path.with_suffix('.ffprobe.json').write_text(json.dumps(evidence, indent=2) + '\n')
    return evidence


def encode_compatibility(timeline, path, size):
    """Standard playback companion. VFR timing is preserved; RGB/chroma is lossy."""
    concat_path = path.with_suffix('.ffconcat')
    lines = ['ffconcat version 1.0']
    for item in timeline:
        lines += ['file ' + quote_concat(item['encodedPath']), 'option framerate 1000',
                  'duration %.9f' % item['duration']]
    lines += ['file ' + quote_concat(timeline[-1]['encodedPath']), 'option framerate 1000']
    concat_path.write_text('\n'.join(lines) + '\n')
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-f', 'concat', '-safe', '0',
                    '-i', str(concat_path), '-fps_mode', 'vfr', '-vf', 'setsar=1',
                    '-c:v', 'libx264', '-profile:v', 'high', '-preset', 'veryfast', '-crf', '18',
                    '-bf', '0', '-threads', '2', '-pix_fmt', 'yuv420p',
                    '-video_track_timescale', '1000000', '-movflags', '+faststart', str(path)], check=True)
    probe = json.loads(subprocess.check_output([
        'ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_frames', '-show_streams', '-show_format',
        '-show_entries', 'frame=best_effort_timestamp_time:stream=codec_name,profile,pix_fmt,width,height,sample_aspect_ratio,time_base:format=duration',
        '-of', 'json', str(path)]))
    stream = probe['streams'][0]
    require((stream['width'], stream['height']) == size and stream.get('sample_aspect_ratio') == '1:1', 'Compatibility copy changed dimensions or pixel aspect.')
    require(stream['codec_name'] == 'h264' and stream['pix_fmt'] == 'yuv420p', 'Compatibility copy must be ordinary H264 yuv420p.')
    pts = [float(frame['best_effort_timestamp_time']) for frame in probe['frames']]
    expected = [item['start'] for item in timeline] + [timeline[-1]['start'] + timeline[-1]['duration']]
    require(len(pts) == len(expected), 'Compatibility copy inserted or removed samples.')
    error = max(abs(actual - target) for actual, target in zip(pts, expected))
    require(error <= .0011, 'Compatibility copy changed recorded holds by more than1.1ms.')
    duration = float(probe['format']['duration'])
    require(abs(duration - expected[-1]) <= .0021, 'Compatibility duration changed.')
    decoded = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(path), '-map', '0:v:0',
                                      '-fps_mode', 'passthrough', '-pix_fmt', 'rgb24', '-f', 'framemd5', '-'], text=True)
    hashes = [line.rsplit(',', 1)[1].strip() for line in decoded.splitlines() if line and not line.startswith('#')]
    source_hashes = [rgb_digest(item['encodedPath']) for item in timeline]
    require(len(hashes) == len(source_hashes) + 1, 'Compatibility decoded frame count changed.')
    actual_inputs = source_hashes + [source_hashes[-1]]
    mismatches = sum(a != b for a, b in zip(hashes, actual_inputs))
    evidence = {'path': str(path), 'sha256': sha(path), 'width': size[0], 'height': size[1],
                'pixelAspect': '1:1', 'codec': 'standard H264 High / yuv420p / CRF18',
                'chromaCompressed': True, 'exactRgbPreservationClaimed': False,
                'decodedFrameCount': len(hashes), 'decodedRgbMatchesEveryInput': mismatches == 0,
                'decodedRgbFramesDifferingFromInputs': mismatches,
                'maximumPtsErrorSeconds': error, 'expectedSeconds': expected[-1],
                'encodedSeconds': duration, 'ffprobe': probe,
                'scope': 'Playback companion only. Same full-frame geometry, sample order and recorded walltime holds; no motion interpolation. RGB-to-YUV conversion, chroma subsampling and lossy encoding alter colors. The lossless RGB master and original PNGs remain authoritative. Compatibility is codec-level and ffmpeg-verified, not a claim about every app.'}
    path.with_suffix('.ffprobe.json').write_text(json.dumps(evidence, indent=2) + '\n')
    return evidence


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('report', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--acceptance', type=Path, required=True)
    parser.add_argument('--append-hands-rain', action='store_true')
    parser.add_argument('--compatibility-mp4', action='store_true', help='Also encode clearly labelled standard H264 yuv420p playback companion; RGB master remains authoritative.')
    parser.add_argument('--title-pause', type=float, default=0, help='Explicit non-gameplay title pause per spell; default none.')
    parser.add_argument('--inspection-crop', type=int, nargs=4, default=DEFAULT_CROP, metavar=('LEFT', 'TOP', 'RIGHT', 'BOTTOM'))
    args = parser.parse_args()
    report_path = args.report.resolve(); out = args.output.resolve(); pin_path = args.acceptance.resolve()
    require(not out.exists(), 'Use a fresh immutable media directory.')
    require(shutil.which('ffmpeg') and shutil.which('ffprobe'), 'ffmpeg and ffprobe are required.')
    report_hash = sha(report_path); pin_hash = sha(pin_path)
    report = json.loads(report_path.read_text()); pin = json.loads(pin_path.read_text())
    final_pin(report, report_hash, pin)
    require(math.isfinite(args.title_pause) and 0 <= args.title_pause <= 3, 'Title pause must be 0..3 seconds.')
    cleanup_path = report_path.with_name(report_path.name.replace('-native.json', '-cleanup.json'))
    cleanup = json.loads(cleanup_path.read_text())
    require(cleanup['runId'] == report['runId'] and cleanup['exitCode'] == 0 and cleanup['finalNativeVerified']
            and cleanup['privateRootRemoved'] and not Path(report['saveRoot']).exists(), 'Same-run isolated cleanup must succeed.')
    x0, y0, x1, y1 = args.inspection_crop
    require(0 <= x0 < x1 <= SIZE[0] and 0 <= y0 < y1 <= SIZE[1], 'Inspection crop must lie inside native pixels.')
    crop_size = (x1-x0, y1-y0); inspection_size = (crop_size[0]*2, crop_size[1]*2 + 128)
    specs = PRIMARY + (APPENDIX if args.append_hands_rain else [])
    rows = []; sources = []
    for spell, label, slug in specs:
        matches = [row for row in report['casts'] if row['mode'] == 'showcase' and row['spell'] == spell]
        require(len(matches) == 1, 'Expected one actual showcase for ' + spell)
        row = matches[0]
        require(row['pass'] and row['sawGesture'] and row['gestureRestored'] and row['resultStable'] and row['nativeEntry'] == spell,
                'Actual spell outcome or gesture did not pass: ' + spell)
        require(row['zoneId'] == 'Overworld.3.6.0' and (row['sourceX'], row['sourceY']) == (40, 8), 'Recalibrate inspection crop for moved fixture.')
        durations = capture_durations(row)
        for frame in row['frames']:
            path = Path(frame['path']).resolve()
            require(path.parent == report_path.parent and path.name.startswith('SSN-' + report['runId'] + '-'), 'Foreign captured frame.')
            require(sha(path) == frame['sha256'], 'Changed source PNG: ' + str(path))
            with Image.open(path) as image:
                image.load(); require(image.size == SIZE and image.format == 'PNG', 'Source must remain full native PNG.')
            sources.append({'path': str(path), 'sha256': frame['sha256'], 'wallSeconds': frame['wallSeconds'], 'spell': spell})
        require(any(frame['meshes'] > 0 for frame in row['frames']), 'No effect appeared in captured samples: ' + spell)
        rows.append((row, label, slug, durations))
    out.mkdir(parents=True); work = out/'composed-frames'; work.mkdir()
    full_timeline = []; inspection_timeline = []; selected = []; cursor = 0.0
    for row, label, slug, durations in rows:
        visible = [f for f in row['frames'] if f['meshes'] > 0]
        selected_frame = min(visible, key=lambda f: (abs(f['wallSeconds']-.4), -f['meshes']))
        original_copy = out/(slug+'-full-gameview.png')
        shutil.copyfile(selected_frame['path'], original_copy)
        require(sha(original_copy) == selected_frame['sha256'], 'Full GameView PNG copy changed.')
        selected.append({'spell': row['spell'], 'path': str(original_copy), 'source': selected_frame['path'],
                         'sha256': sha(original_copy), 'wallSeconds': selected_frame['wallSeconds'], 'meshes': selected_frame['meshes']})
        if args.title_pause:
            full_card = work/(slug+'-full-title.png'); small_card = work/(slug+'-inspection-title.png')
            title(full_card, label, 'TITLE PAUSE — recorded gameplay follows at 1× timing', SIZE)
            title(small_card, label, 'TITLE PAUSE — enlarged inspection follows', inspection_size)
            meta = {'kind': 'explicit_title_pause', 'spell': row['spell'], 'start': cursor, 'duration': args.title_pause}
            full_timeline.append({**meta, 'encodedPath': str(full_card)}); inspection_timeline.append({**meta, 'encodedPath': str(small_card)})
            cursor += args.title_pause
        for index, (frame, duration) in enumerate(zip(row['frames'], durations)):
            meta = {'kind': 'native_capture', 'spell': row['spell'], 'sourcePath': frame['path'],
                    'sourceSha256': frame['sha256'], 'captureWallSeconds': frame['wallSeconds'], 'start': cursor, 'duration': duration}
            full_timeline.append({**meta, 'encodedPath': frame['path']})
            with Image.open(frame['path']) as source:
                crop = source.convert('RGB').crop(args.inspection_crop)
            enlarged = crop.resize((crop.width*2, crop.height*2), Image.Resampling.NEAREST)
            require(enlarged.resize(crop.size, Image.Resampling.NEAREST).tobytes() == crop.tobytes(), 'Inspection introduced interpolated pixels.')
            image = Image.new('RGB', inspection_size, (24, 27, 25)); draw = ImageDraw.Draw(image)
            draw.text((24, 16), label + ' — separate 2× inspection', font=font(28), fill=(235, 235, 219))
            draw.text((24, 55), 'Actual capture t = %.3fs; recorded timing 1×' % frame['wallSeconds'], font=font(21), fill=(170, 182, 167))
            image.paste(enlarged, (0, 88))
            draw.text((24, inspection_size[1]-31), 'About 10 captured samples/s • no motion or color interpolation • not the full GameView', font=font(18), fill=(170, 182, 167))
            target = work/('%s-%03d-inspection.png' % (slug, index)); image.save(target)
            inspection_timeline.append({**meta, 'encodedPath': str(target), 'crop': list(args.inspection_crop), 'nearestMagnification': 2})
            cursor += duration
    full = encode_lossless(full_timeline, out/'readability-full-gameview-native-1x.mp4', SIZE)
    inspection = encode_lossless(inspection_timeline, out/'readability-separate-inspection-native-1x.mp4', inspection_size)
    videos = [full, inspection]
    if args.compatibility_mp4:
        videos.append(encode_compatibility(full_timeline, out/'readability-full-gameview-native-1x-COMPATIBILITY-chroma-compressed.mp4', SIZE))
    require(sha(report_path) == report_hash and sha(pin_path) == pin_hash, 'Report or final acceptance changed during composition.')
    for source in sources:
        require(sha(source['path']) == source['sha256'], 'Source frame changed during composition.')
    receipt = {'status': 'PASS', 'stage': 'final', 'runId': report['runId'], 'nativeReport': str(report_path),
               'nativeReportSha256': report_hash, 'acceptance': str(pin_path), 'acceptanceSha256': pin_hash,
               'cleanupReportSha256': sha(cleanup_path), 'primarySpells': [s[0] for s in PRIMARY],
               'appendHandsRain': args.append_hands_rain, 'sources': sources, 'fullGameViewPngs': selected,
               'fullTimeline': full_timeline, 'inspectionTimeline': inspection_timeline, 'videos': videos,
               'compatibilityCompanionRequested': args.compatibility_mp4,
               'titlePauseSeconds': args.title_pause*len(rows), 'nativeSampleSeconds': sum(sum(r[3]) for r in rows),
               'scope': 'Unmodified 1920x1080 GameView PNGs and lossless decoded-RGB full-frame MP4 are primary. Separate inspection crops only repeat source pixels 2x. No inpainting, grading, art replacement, camera changes, or motion interpolation. Each cast begins at its first available capture, holds to the next actual wall timestamp, and ends at recorded case completion. About10 samples/sec can miss motion between captures; playback verification is ffmpeg/ffprobe, not every hardware player.'}
    (out/'media-verification.json').write_text(json.dumps(receipt, indent=2)+'\n')
    (out/'README.md').write_text('# Final native spell preview\n\nPrimary: `readability-full-gameview-native-1x.mp4` — full1920×1080 native pixels at recorded1× timing. All decoded RGB frames match their source pixels exactly. The five named full-GameView PNGs are byte-for-byte copies of the original captures.\n\nSecondary: `readability-separate-inspection-native-1x.mp4` — a wider704×528 crop enlarged exactly2× with nearest-neighbor pixels, clearly labeled outside the scene. Consult the receipt for a nondefault crop.\n\nThe captures sample about10 frames/sec; holding each recorded image does not invent the motion between samples. Any requested title pauses are separately labeled timeline records, not gameplay duration. Lossless RGB H.264 decode is verified with ffmpeg; other player compatibility is not asserted.\n\nFinal acceptance, source hashes, selected timestamps, complete timelines, ffprobe metadata and decoded RGB hash verification are retained in `media-verification.json`.\n')
    if args.compatibility_mp4:
        with (out/'README.md').open('a') as readme:
            readme.write('\nOptional playback companion: `readability-full-gameview-native-1x-COMPATIBILITY-chroma-compressed.mp4` uses standard H264 High/yuv420p. It preserves full-frame dimensions and recorded variable frame holds without motion interpolation, but RGB-to-YUV conversion, chroma subsampling and lossy compression change colors. The lossless RGB master and original PNGs remain authoritative. Codec-level/ffmpeg verification does not guarantee every app can play it.\n')
    print(json.dumps({'status': 'PASS', 'runId': report['runId'], 'sourceFrames': len(sources),
                      'nativeSampleSeconds': receipt['nativeSampleSeconds'], 'videos': [v['path'] for v in receipt['videos']]}, indent=2))


if __name__ == '__main__':
    main()
