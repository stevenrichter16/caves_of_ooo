#!/usr/bin/env python3
"""Prepare only approved third-palette assets; original53 remain untouched."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import numpy as np
from prepare_world_audio import meta
from prepare_place_audio import download, extract, prepare, RATE
REPO=Path(__file__).resolve().parents[2]
SPEC=Path(__file__).with_name('atmospheric_audio_sources.json')

def main(cache,out):
 spec=json.loads(SPEC.read_text());cache.mkdir(parents=True,exist_ok=True)
 (out/'Atmospheric').mkdir(parents=True,exist_ok=True);meta(out/'Atmospheric')
 for name,row in spec['sources'].items():download(row,cache/name)
 results=[];catalog=dict(schemaVersion=1,music=[],ambience=[],details=[],sounds=[])
 with tempfile.TemporaryDirectory(prefix='coo-atmospheric-prep-') as d:
  work=Path(d)
  for row in spec['clips']:
   source=cache/row['source']
   if 'member' in row:
    data=extract(source,row['member'])
    if hashlib.sha256(data).hexdigest()!=row['memberSha256']:raise ValueError('Archive member changed')
    source=work/(row['id']+Path(row['member']).suffix);source.write_bytes(data)
   raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-ar',str(RATE),'-ac','1','-f','f32le','-'])
   original=np.frombuffer(raw,dtype='<f4').copy().reshape(-1,1);samples,gain=prepare(original,row)
   rawpath=work/(row['id']+'.f32');rawpath.write_bytes(samples.astype('<f4').tobytes());target=out/row['path']
   subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(RATE),'-ac','1','-i',str(rawpath),'-c:a','libvorbis','-q:a','5','-fflags','+bitexact','-flags:a','+bitexact','-metadata','title='+row['title'],'-metadata','artist='+row['author'],'-metadata','copyright='+row['credit'],'-metadata','license='+row['licenseUrl'],'-metadata','comment='+row['changes'],str(target)],check=True)
   meta(target,'sfx')
   results.append(dict(row,outputSha256=hashlib.sha256(target.read_bytes()).hexdigest(),outputBytes=target.stat().st_size,sourceDecodedFrames=len(original),preparedFrames=len(samples),preparationGain=gain))
   group='details' if row['kind']=='detail' else 'sounds'
   catalog[group].append(dict(id=row['cue'],resource='Audio/World/'+row['path'].removesuffix('.ogg'),gain=row['gain']))
   print(row['path'],round(len(samples)/RATE,3),target.stat().st_size,flush=True)
 credits=['ATMOSPHERIC DETAIL AND WORK AUDIO CREDITS','All additional clips are CC0 1.0. No additional music or Qud recordings are included.','Leaf recordings are designed Foley for reeds/harvest, not a claim of real reed field recordings. Stone and room details are sparse designed textures. Forge and material repair sounds are for actual successful actions, not unseen workers.','']
 for row in spec['clips']:credits += [row['path'],row['credit'],row['page'],'License: '+row['licenseUrl'],'Changes: '+row['changes'],'']
 credits += ['The complete CC0 legal text is retained in Audio/World/Licenses/CC0-1.0.txt. Source/member hashes and exact changes are in atmospheric-provenance.json. No artist endorsement is implied.']
 values={'ATMOSPHERIC-CREDITS.txt':'\n'.join(credits)+'\n','atmospheric-catalog.json':json.dumps(catalog,indent=2)+'\n','atmospheric-provenance.json':json.dumps(dict(schemaVersion=1,sourceVerificationDate='2026-10-11',tool=subprocess.check_output(['ffmpeg','-version'],text=True).splitlines()[0],sources=spec['sources'],clips=results),indent=2)+'\n'}
 for name,value in values.items():(out/name).write_text(value);meta(out/name)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--cache',type=Path,required=True);p.add_argument('--output',type=Path,default=REPO/'Assets/Resources/Audio/World');a=p.parse_args();main(a.cache,a.output)
