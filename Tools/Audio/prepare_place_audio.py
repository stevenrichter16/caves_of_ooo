#!/usr/bin/env python3
"""Prepare only the pinned place/attack expansion; never modifies original22 assets."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import zipfile
import numpy as np
from prepare_world_audio import meta

REPO=Path(__file__).resolve().parents[2]
SPEC=Path(__file__).with_name('place_audio_sources.json')
RATE=44100

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def download(row,path):
    if not path.exists():
        tmp=path.with_suffix(path.suffix+'.part')
        subprocess.run(['curl','--fail','--silent','--show-error','--location','--max-time','180','--max-filesize',str(row['bytes']),'--output',str(tmp),row['url']],check=True)
        if tmp.stat().st_size!=row['bytes'] or sha(tmp)!=row['sha256']:raise ValueError('Downloaded source changed: '+path.name)
        tmp.replace(path)
    if path.stat().st_size!=row['bytes'] or sha(path)!=row['sha256']:raise ValueError('Cached source changed: '+path.name)

def extract(source,member):
    if source.suffix=='.zip':
        with zipfile.ZipFile(source) as z:return z.read(member)
    if source.suffix=='.7z':return subprocess.check_output(['bsdtar','-xOf',str(source),member])
    raise ValueError('Unsupported archive')

def apply_filters(samples,filters):
    """Conservative band shaping; does not certify absence of wildlife."""
    allowed={'highpassHz':'highpass','lowpassHz':'lowpass'}
    if not filters or set(filters)-set(allowed):raise ValueError('Unsupported filter contract')
    if any(not isinstance(v,(int,float)) or not np.isfinite(v) or not 10<=v<=15000 for v in filters.values()):raise ValueError('Invalid filter frequency')
    chain=','.join(allowed[k]+'=f='+str(v) for k,v in filters.items())
    raw=subprocess.check_output(['ffmpeg','-v','error','-f','f32le','-ar',str(RATE),'-ac',str(samples.shape[1]),'-i','pipe:0','-af',chain,'-f','f32le','pipe:1'],input=samples.astype('<f4').tobytes())
    return np.frombuffer(raw,dtype='<f4').copy().reshape(-1,samples.shape[1])

def prepare(samples,row):
    if not len(samples) or not np.isfinite(samples).all():raise ValueError('Invalid source signal')
    if 'startSeconds' in row:
        start=round(row['startSeconds']*RATE);count=round(row['durationSeconds']*RATE)
        if start<0 or count<=0 or start+count>len(samples):raise ValueError('Excerpt outside original source')
        samples=samples[start:start+count].copy()
    if row.get('filters'):samples=apply_filters(samples,row['filters'])
    if row['kind']=='ambience':
        overlap=RATE//2
        if len(samples)<overlap*3:raise ValueError('Bed too short for overlap')
        weight=np.linspace(0,1,overlap,dtype=np.float32)[:,None]
        blend=samples[-overlap:]*(1-weight)+samples[:overlap]*weight
        samples=np.concatenate((samples[overlap:-overlap],blend))
    else:
        active=np.flatnonzero(np.abs(samples[:,0])>max(float(np.max(np.abs(samples)))*.002,1e-7))
        if not len(active):raise ValueError('Silent source')
        guard=round(.008*RATE);samples=samples[max(0,active[0]-guard):min(len(samples),active[-1]+guard+1)].copy()
        fade=min(round((.05 if row['kind']=='detail' else .005)*RATE),len(samples)//4)
        samples[:fade]*=np.linspace(0,1,fade,dtype=np.float32)[:,None]
        samples[-fade:]*=np.linspace(1,0,fade,dtype=np.float32)[:,None]
        samples=np.pad(samples,((round(.01*RATE),round(.01*RATE)),(0,0)))
    ceiling=.35 if row['kind']=='sfx' else .25
    gain=ceiling/max(float(np.max(np.abs(samples))),1e-12)
    if row['kind']=='ambience':gain=min(gain,1)
    return samples*gain,gain

def main(cache,out):
    spec=json.loads(SPEC.read_text());cache.mkdir(parents=True,exist_ok=True)
    for folder in ['Places','Combat']:
        (out/folder).mkdir(parents=True,exist_ok=True);meta(out/folder)
    for name,row in spec['sources'].items():download(row,cache/name)
    results=[];catalog=dict(schemaVersion=1,music=[],ambience=[],details=[],sounds=[])
    with tempfile.TemporaryDirectory(prefix='coo-place-prep-') as d:
        work=Path(d)
        for row in spec['clips']:
            source=cache/row['source']
            if 'member' in row:
                data=extract(source,row['member'])
                if hashlib.sha256(data).hexdigest()!=row['memberSha256']:raise ValueError('Archive member changed')
                source=work/(row['id']+Path(row['member']).suffix);source.write_bytes(data)
            channels=2 if row['kind']=='ambience' else 1
            raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-ar',str(RATE),'-ac',str(channels),'-f','f32le','-'])
            original=np.frombuffer(raw,dtype='<f4').copy().reshape(-1,channels)
            samples,gain=prepare(original,row)
            rawpath=work/(row['id']+'.f32');rawpath.write_bytes(samples.astype('<f4').tobytes())
            target=out/row['path']
            subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(RATE),'-ac',str(channels),'-i',str(rawpath),'-c:a','libvorbis','-q:a','5','-fflags','+bitexact','-flags:a','+bitexact','-metadata','title='+row['title'],'-metadata','artist='+row['author'],'-metadata','copyright='+row['credit'],'-metadata','license='+row['licenseUrl'],'-metadata','comment='+row['changes'],str(target)],check=True)
            meta(target,'ambience' if row['kind']=='ambience' else 'sfx')
            results.append(dict(row,outputSha256=sha(target),outputBytes=target.stat().st_size,sourceDecodedFrames=len(original),preparedFrames=len(samples),preparationGain=gain))
            group='ambience' if row['kind']=='ambience' else ('details' if row['kind']=='detail' else 'sounds')
            catalog[group].append(dict(id=row['cue'],resource='Audio/World/'+row['path'].removesuffix('.ogg'),gain=row['gain']))
            print(row['path'],round(len(samples)/RATE,3),target.stat().st_size,flush=True)
    for alias in spec['ambienceAliases']:
        target=next(r for r in spec['clips'] if r['id']==alias['target'])
        catalog['ambience'].append(dict(id=alias['id'],resource='Audio/World/'+target['path'].removesuffix('.ogg'),gain=alias['gain']))
    credits=['PLACE AND COMBAT AUDIO EXPANSION','All additional sounds are CC0 1.0. No additional music or Qud audio is included.','', 'These are independently licensed field recordings and designed Foley. Frog recording method is unspecified by its creator; no species-authenticity claim is made. Marsh uses the open-field wind asset at a lower gain, not a river or synthetic animal bed.','']
    for row in spec['clips']:credits += [row['path'],row['credit'],row['page'],'License: '+row['licenseUrl'],'Changes: '+row['changes'],'']
    credits += ['Complete CC0 legal text and original Kenney notices are retained in Audio/World/Licenses. Original qubodup notice follows:',extract(cache/'swoshes.7z','swoosh.txt').decode('utf-8'),'Source/member hashes, exact excerpts and transformation data are in places-provenance.json. No endorsement is implied.']
    values={'PLACES-CREDITS.txt':'\n'.join(credits)+'\n','places-catalog.json':json.dumps(catalog,indent=2)+'\n','places-provenance.json':json.dumps(dict(schemaVersion=1,sourceVerificationDate='2026-10-11',tool=subprocess.check_output(['ffmpeg','-version'],text=True).splitlines()[0],sources=spec['sources'],clips=results),indent=2)+'\n'}
    for name,value in values.items():(out/name).write_text(value);meta(out/name)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--cache',type=Path,required=True);p.add_argument('--output',type=Path,default=REPO/'Assets/Resources/Audio/World');a=p.parse_args();main(a.cache,a.output)
