#!/usr/bin/env python3
"""Offline asset contract; does not establish Unity import or subjective audio quality.
Requires Python3, numpy, ffmpeg and ffprobe. Reads shipped assets only.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import numpy as np

REPO = Path(__file__).resolve().parents[2]
SPEC = Path(__file__).with_name('world_audio_sources.json')


def signal_errors(samples, rate, channels, kind):
    errors = []
    if len(samples) == 0 or not np.isfinite(samples).all():
        return ['empty or non-finite decoded signal'], {}
    frames = samples.reshape(-1, channels)
    peak = float(np.max(np.abs(frames)))
    rms = float(np.sqrt(np.mean(np.square(frames, dtype=np.float64))))
    duration = len(frames) / rate
    seam = float(np.max(np.abs(frames[-1] - frames[0])))
    stats = dict(durationSeconds=round(duration, 6), sampleRate=rate, channels=channels,
                 peakDbfs=round(20*np.log10(max(peak, 1e-12)), 3),
                 rmsDbfs=round(20*np.log10(max(rms, 1e-12)), 3), seamJump=round(seam, 6),
                 decodedFloatBytes=frames.size*4)
    if peak >= .99: errors.append('insufficient peak headroom / clipped signal')
    if rms < 1e-5: errors.append('silent or effectively silent signal')
    if kind == 'sfx':
        if channels != 1 or not .035 <= duration <= 2: errors.append('SFX channel/duration contract')
    else:
        if channels != 2: errors.append('bed/music must retain stereo')
        if kind == 'music' and not 180 <= duration <= 900: errors.append('music duration contract')
        if kind == 'ambience' and not 30 <= duration <= 120: errors.append('ambience duration contract')
        if kind == 'ambience' and seam > .015: errors.append('ambience decoded seam discontinuity')
    if rate != 44100: errors.append('sample-rate contract')
    return errors, stats


def importer_errors(text, kind):
    errors = []
    if not re.search(r'^guid: [0-9a-f]{32}$', text, re.M): errors.append('invalid GUID')
    expected = {'loadType':2,'preloadAudioData':0,'compressionFormat':1,'loadInBackground':1} if kind != 'sfx' else {'loadType':0,'preloadAudioData':1,'compressionFormat':1,'loadInBackground':0}
    for key,value in expected.items():
        if not re.search(r'^\s*'+key+': '+str(value)+r'\s*$',text,re.M): errors.append('wrong importer '+key)
    return errors


def verify(root, report):
    spec=json.loads(SPEC.read_text()); findings=[]; assets=[]; guids=[]; total=0; sfx_pcm=0
    manifest_path=root/'provenance.json'
    manifest=json.loads(manifest_path.read_text()) if manifest_path.is_file() else {}
    if not manifest: findings.append('Missing provenance.json')
    if manifest and manifest.get('sources')!=spec['sources']:findings.append('Source provenance differs from pinned official downloads')
    shipped={c['path']:c for c in manifest.get('clips',[])}
    creditpath=root/'CREDITS.txt';credits=creditpath.read_text() if creditpath.is_file() else ''
    for licensefile in ['CC-BY-4.0.txt','CC0-1.0.txt']:
        p=root/'Licenses'/licensefile
        if not p.is_file() or p.stat().st_size<3000:findings.append('Missing complete license '+licensefile)
    for row in spec['clips']:
        path=root/row['path'];errors=[];stats={}
        if not path.is_file(): errors.append('missing asset')
        else:
            total+=path.stat().st_size
            try:
                probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=sample_rate,channels:format=duration','-of','json',str(path)]))
                data=probe['streams'][0]
                rate=int(data['sample_rate']);channels=data['channels']
                raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-acodec','pcm_f32le','-'])
                es,stats=signal_errors(np.frombuffer(raw,dtype='<f4'),rate,channels,row['kind']);errors+=es
                if row['kind']=='sfx':sfx_pcm+=stats.get('decodedFloatBytes',0)
            except Exception as e:errors.append('decode failed: '+str(e))
            meta=path.with_suffix(path.suffix+'.meta')
            if not meta.is_file():errors.append('missing importer')
            else:
                mt=meta.read_text();errors+=importer_errors(mt,row['kind']);guids+=re.findall(r'^guid: (.+)$',mt,re.M)
            actual_hash=hashlib.sha256(path.read_bytes()).hexdigest()
            info=shipped.get(row['path'],{})
            if info.get('outputSha256')!=actual_hash:errors.append('provenance/output hash mismatch')
            for key in ['author','title','page','license','licenseUrl','credit','changes','source']:
                if info.get(key)!=row[key]:errors.append('provenance mismatch '+key)
            if row['credit'] not in credits:errors.append('credit text missing')
            if row['kind']=='music' and info.get('sourceDecodedFrames')!=info.get('preparedFrames'):errors.append('music was cut')
            if stats:
                intended=info.get('preparedFrames',0)
                if abs(float(probe['format']['duration'])*rate-intended)>1:errors.append('prepared/container frame count mismatch')
                # Short Vorbis decodes can expose one block of codec padding;
                # container granule duration remains exact. Music may not be cut.
                tolerance=1152 if row['kind']=='sfx' else 1
                if abs(stats['durationSeconds']*rate-intended)>tolerance:errors.append('prepared/decoded frame count mismatch')
        assets.append(dict(path=row['path'],errors=errors,**stats))
        findings.extend(row['path']+': '+e for e in errors)
    if len(guids)!=len(set(guids)):findings.append('duplicate audio GUIDs')
    allguids=[]
    for mp in [Path(str(root)+'.meta'),*root.rglob('*.meta')]:
        if not mp.is_file():findings.append('missing root folder meta');continue
        match=re.search(r'^guid: ([0-9a-f]{32})$',mp.read_text(),re.M)
        if not match:findings.append('invalid metadata GUID '+str(mp))
        else:allguids.append(match.group(1))
    if len(allguids)!=len(set(allguids)):findings.append('duplicate world folder/text/audio GUIDs')
    if total>32*1024*1024:findings.append('compressed audio exceeds32MiB budget')
    if sfx_pcm>2*1024*1024:findings.append('SFX float PCM exceeds2MiB budget')
    result=dict(passed=not findings,assetCount=len(assets),audioBytes=total,sfxDecodedFloatBytes=sfx_pcm,
                subjectiveListeningVerified=False,unityImportVerified=False,findings=findings,assets=assets)
    report.parent.mkdir(parents=True,exist_ok=True);report.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='assets'},indent=2))
    return not findings


def counterchecks():
    # Actual valid-contract signal paired with silence, clipping, NaN and seam failures.
    x=(.1*np.sin(2*np.pi*np.arange(4410)/441)).astype(np.float32)
    assert not signal_errors(x,44100,1,'sfx')[0]
    assert signal_errors(x*0,44100,1,'sfx')[0]
    assert signal_errors(x*20,44100,1,'sfx')[0]
    y=x.copy();y[2]=np.nan;assert signal_errors(y,44100,1,'sfx')[0]
    assert signal_errors(x,22050,1,'sfx')[0]
    assert signal_errors(np.tile(x,30),44100,1,'sfx')[0]
    bed=np.tile(x,800).reshape(-1,2);bed[-1]=bed[0]
    assert not signal_errors(bed.ravel(),44100,2,'ambience')[0]
    bed[-1]=.5;assert signal_errors(bed.ravel(),44100,2,'ambience')[0]
    good='guid: '+'a'*32+'\nloadType: 2\npreloadAudioData: 0\ncompressionFormat: 1\nloadInBackground: 1\n'
    assert not importer_errors(good,'music')
    for before,after in [('loadType: 2','loadType: 0'),('preloadAudioData: 0','preloadAudioData: 1'),('compressionFormat: 1','compressionFormat: 0'),('a'*32,'a'*33)]:
        assert importer_errors(good.replace(before,after),'music')
    print('13 counterchecks passed')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=REPO/'Assets/Resources/Audio/World');p.add_argument('--report',type=Path,default=Path('/tmp/world-audio-verification.json'));p.add_argument('--counterchecks',action='store_true');a=p.parse_args()
    if a.counterchecks:counterchecks()
    else:sys.exit(0 if verify(a.root,a.report) else 1)
