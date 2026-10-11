#!/usr/bin/env python3
"""Validate expansion bytes/catalog/importers; does not establish audible quality."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import numpy as np
from verify_world_audio import importer_errors

REPO=Path(__file__).resolve().parents[2]
SPEC=Path(__file__).with_name('atmospheric_audio_sources.json')

def signal_errors(samples,rate,channels,kind):
    errors=[]
    if not len(samples) or channels<1 or len(samples)%channels or not np.isfinite(samples).all():return ['empty/nonfinite/malformed signal'],{}
    x=samples.reshape(-1,channels);peak=float(np.max(np.abs(x)));rms=float(np.sqrt(np.mean(x.astype(np.float64)**2)));duration=len(x)/rate;seam=float(np.max(np.abs(x[-1]-x[0])))
    if rate!=44100:errors.append('sample rate')
    if channels!=(2 if kind=='ambience' else 1):errors.append('channels')
    if not (10<=duration<=90 if kind=='ambience' else .035<=duration<=(3.5 if kind=='detail' else 4)):errors.append('duration')
    if peak>=.6:errors.append('peak headroom')
    if rms<1e-5:errors.append('silent')
    if kind=='ambience' and seam>.015:errors.append('loop seam')
    return errors,dict(durationSeconds=duration,sampleRate=rate,channels=channels,peakDbfs=20*np.log10(max(peak,1e-12)),rmsDbfs=20*np.log10(max(rms,1e-12)),seamJump=seam,decodedFloatBytes=x.size*4)

def verify(root,decode=True):
    spec=json.loads(SPEC.read_text());errors=[];assets=[];total=pcm=0
    try:manifest=json.loads((root/'atmospheric-provenance.json').read_text());catalog=json.loads((root/'atmospheric-catalog.json').read_text());credits=(root/'ATMOSPHERIC-CREDITS.txt').read_text()
    except (OSError,ValueError) as e:return dict(passed=False,findings=['missing/invalid expansion metadata: '+str(e)])
    if manifest.get('sources')!=spec['sources']:errors.append('source provenance differs')
    expectedcat=dict(schemaVersion=1,music=[],ambience=[],details=[],sounds=[])
    rows=manifest.get('clips',[]);shipped={r['path']:r for r in rows}
    if len(rows)!=len(spec['clips']) or len(shipped)!=len(rows):errors.append('provenance clip count')
    for row in spec['clips']:
        group='ambience' if row['kind']=='ambience' else ('details' if row['kind']=='detail' else 'sounds')
        expectedcat[group].append(dict(id=row['cue'],resource='Audio/World/'+row['path'].removesuffix('.ogg'),gain=row['gain']))
        path=root/row['path'];bad=[];stats={};info=shipped.get(row['path'],{})
        if not path.is_file():bad.append('missing asset')
        else:
            total+=path.stat().st_size
            if info.get('outputSha256')!=hashlib.sha256(path.read_bytes()).hexdigest():bad.append('output hash')
            if info.get('outputBytes')!=path.stat().st_size:bad.append('output bytes')
            for k,v in row.items():
                if info.get(k)!=v:bad.append('provenance '+k)
            mp=Path(str(path)+'.meta')
            if not mp.is_file():bad.append('missing importer')
            else:bad+=importer_errors(mp.read_text(),'ambience' if row['kind']=='ambience' else 'sfx')
            if row['kind']!='ambience':pcm+=int(info.get('preparedFrames',0))*4
            if decode:
                try:
                    probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration:stream=sample_rate,channels','-of','json',str(path)]));stream=probe['streams'][0];rate=int(stream['sample_rate']);channels=stream['channels']
                    raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-'])
                    issues,stats=signal_errors(np.frombuffer(raw,dtype='<f4'),rate,channels,row['kind']);bad+=issues
                    expected=info.get('preparedFrames',0)
                    if abs(float(probe['format']['duration'])*rate-expected)>1:bad.append('container duration')
                    if abs(stats.get('durationSeconds',0)*rate-expected)>(1152 if row['kind']!='ambience' else 1):bad.append('decoded duration')
                except (ValueError,KeyError,subprocess.CalledProcessError) as e:bad.append('decode: '+str(e))
        if row['credit'] not in credits or row['page'] not in credits:bad.append('missing credit/source')
        if row['license']!='CC0-1.0' or row['licenseUrl']!='https://creativecommons.org/publicdomain/zero/1.0/':bad.append('unexpected license')
        errors += [row['path']+': '+b for b in bad];assets.append(dict(path=row['path'],errors=bad,**stats))
    for alias in spec['ambienceAliases']:
        row=next(r for r in spec['clips'] if r['id']==alias['target'])
        expectedcat['ambience'].append(dict(id=alias['id'],resource='Audio/World/'+row['path'].removesuffix('.ogg'),gain=alias['gain']))
    if catalog!=expectedcat:errors.append('catalog does not match pinned manifest')
    expectedfiles={r['path'] for r in spec['clips']}
    actualfiles={str(p.relative_to(root)) for folder in ['Atmospheric'] for p in (root/folder).glob('*.ogg')}
    if expectedfiles!=actualfiles:errors.append('missing or unreferenced expansion audio')
    allguid=[]
    for p in root.rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(),re.M)
        if match:allguid.append(match.group(1))
        else:errors.append('bad guid '+str(p.relative_to(root)))
    if len(allguid)!=len(set(allguid)):errors.append('duplicate GUID')
    for p in [root/'Atmospheric',root/'atmospheric-catalog.json',root/'atmospheric-provenance.json',root/'ATMOSPHERIC-CREDITS.txt']:
        if not Path(str(p)+'.meta').is_file():errors.append('missing meta '+p.name)
    licensepath=root/'Licenses/CC0-1.0.txt'
    if not licensepath.is_file() or licensepath.stat().st_size<3000:errors.append('missing complete CC0 license')
    if total>spec['budgets']['compressedBytes']:errors.append('compressed budget')
    if pcm>spec['budgets']['shortDecodedFloatBytes']:errors.append('preloaded PCM budget')
    return dict(passed=not errors,assetCount=len(assets),audioBytes=total,shortDecodedFloatBytes=pcm,subjectiveListeningVerified=False,unityImportVerified=False,findings=errors,assets=assets)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=REPO/'Assets/Resources/Audio/World');p.add_argument('--report',type=Path,default=Path('/tmp/atmospheric-audio-verification.json'));a=p.parse_args();r=verify(a.root);a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items() if k!='assets'},indent=2));sys.exit(0 if r['passed'] else 1)
