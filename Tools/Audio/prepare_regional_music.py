#!/usr/bin/env python3
"""Prepare complete licensed compositions for the staged music expansion.

No Unity invocation. Output is explicit; no default writes into Assets. Existing
world/place/action assets are outside ownership. Python3,numpy,ffmpeg,curl required.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess
import tempfile
import uuid
import numpy as np

RATE=44100
SPEC=Path(__file__).with_name('regional_music_sources.json')
STAGES=(16,17,19)

def sha(path):
 digest=hashlib.sha256()
 with path.open('rb') as stream:
  for block in iter(lambda:stream.read(1024*1024),b''):digest.update(block)
 return digest.hexdigest()

def validate_spec(spec):
 if spec.get('schemaVersion')!=1:raise ValueError('Unsupported source manifest')
 sources=spec.get('sources',{});ids=set();paths=set()
 for name,row in sources.items():
  if Path(name).name!=name or not row.get('url','').startswith('https://www.scottbuckley.com.au/library/wp-content/uploads/'):
   raise ValueError('Source must name an exact official download')
  if not re.fullmatch('[0-9a-f]{64}',row.get('sha256','')) or not isinstance(row.get('bytes'),int) or not 0<row['bytes']<50000000:
   raise ValueError('Source size/hash contract')
  if not isinstance(row.get('decodedFrames'),int) or not 180*RATE<=row['decodedFrames']<=900*RATE:raise ValueError('Pinned complete composition length missing')
 for row in spec.get('clips',[]):
  if not re.fullmatch(r'Music/[A-Za-z0-9]+\.ogg',row.get('path','')):raise ValueError('Invalid music output path')
  if row.get('id') in ids or row['path'] in paths:raise ValueError('Duplicate music identity')
  ids.add(row.get('id'));paths.add(row['path'])
  if row.get('source') not in sources or row.get('iteration') not in STAGES:raise ValueError('Unregistered source/stage')
  if row.get('kind')!='music' or row.get('author')!='Scott Buckley' or row.get('license')!='CC-BY-4.0':raise ValueError('Unreviewed music attribution/license')
  for key in ['id','context','title','mix','page','credit','changes','licenseUrl']:
   if not isinstance(row.get(key),str) or not row[key].strip():raise ValueError('Missing provenance '+key)
  if not row['page'].startswith('https://www.scottbuckley.com.au/library/') or row['licenseUrl']!='https://creativecommons.org/licenses/by/4.0/':raise ValueError('Wrong primary source/license')
  if row['credit']!="'"+row['title']+"' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au":raise ValueError('Exact author attribution missing')
  if not isinstance(row.get('runtimeGain'),(int,float)) or not math.isfinite(row['runtimeGain']) or not 0<row['runtimeGain']<=1:raise ValueError('Invalid gain')
 if not ids:raise ValueError('No selected music')

def selected_rows(spec,through):
 validate_spec(spec)
 if through not in STAGES:raise ValueError('Unsupported stage')
 return [r for r in spec['clips'] if r['iteration']<=through]

def verify_source(path,row):
 if not path.is_file() or path.stat().st_size!=row['bytes'] or sha(path)!=row['sha256']:
  raise ValueError('Official source bytes changed: '+str(path))

def download_source(path,row):
 if not path.exists():
  temp=path.with_suffix(path.suffix+'.part')
  subprocess.run(['curl','--fail','--silent','--show-error','--location','--max-time','120','--max-filesize','50000000','--output',str(temp),row['url']],check=True)
  verify_source(temp,row);temp.replace(path)
 verify_source(path,row)

def music_gain(peak):
 if not math.isfinite(peak) or peak<=0:raise ValueError('Invalid/silent peak')
 return min(1,.75/peak)

def raw_stats(path):
 size=path.stat().st_size
 if size==0 or size%8:raise ValueError('Empty/incomplete stereo float frames')
 data=np.memmap(path,dtype='<f4',mode='r');peak=0.;squares=0.
 # Bound working RAM to ten seconds, even for ten-minute full compositions.
 for start in range(0,len(data),RATE*2*10):
  chunk=data[start:start+RATE*2*10]
  if not np.isfinite(chunk).all():raise ValueError('Nonfinite music signal')
  peak=max(peak,float(np.max(np.abs(chunk))));squares+=float(np.sum(np.square(chunk,dtype=np.float64)))
 rms=math.sqrt(squares/len(data));frames=len(data)//2
 del data
 if rms<1e-5:raise ValueError('Effectively silent music')
 return dict(frames=frames,peak=peak,rms=rms)

def write_meta(path,audio=False):
 meta=Path(str(path)+'.meta')
 if meta.exists():return
 text='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
 if path.is_dir():text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
 elif audio:
  text+='AudioImporter:\n  externalObjects: {}\n  serializedVersion: 8\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 2\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 0.65\n    conversionMode: 0\n    preloadAudioData: 0\n  platformSettingOverrides: {}\n  forceToMono: 0\n  normalize: 0\n  loadInBackground: 1\n  ambisonic: 0\n  3D: 1\n'
 else:text+='TextScriptImporter:\n  externalObjects: {}\n'
 text+='  userData: \n  assetBundleName: \n  assetBundleVariant: \n';meta.write_text(text)

def write_documents(spec,rows,results,out,through,version):
 catalog={'schemaVersion':1,'music':[dict(id=r['context'],resource='Audio/World/'+str(Path(r['path']).with_suffix('')),gain=r['runtimeGain']) for r in rows],'ambience':[],'sounds':[],'details':[]}
 sources={r['source']:spec['sources'][r['source']] for r in rows}
 proof=dict(schemaVersion=1,sourceVerificationDate=spec['verificationDate'],throughIteration=through,tool=version,sources=sources,clips=results)
 credits=['REGIONAL MUSIC CREDITS','', 'Complete independently licensed compositions by Scott Buckley; no Caves of Qud recordings included.','']
 for row in rows:credits += [row['credit'],'Supplied mix: '+row['mix'],row['page'],'License: '+row['licenseUrl'],'Changes: '+row['changes'],'']
 credits+=['STREAMING / VIDEO NOTE','These compositions use YouTube Content ID. Include each complete attribution line for music heard in your video description; in-game credit alone does not guarantee that a video avoids claims. The game provides independent music volume/mute.','The composer requests no separate resale/reupload to music-streaming platforms and no fingerprint enrollment of remixes. This use synchronizes music with gameplay.',spec['licensePolicyPage'],'','The complete CC-BY4.0 license is shipped at Audio/World/Licenses/CC-BY-4.0.txt. Source/output hashes and preparation changes are in regional-music-provenance.json. No artist endorsement is implied.']
 for name,text in [('regional-music-catalog.json',json.dumps(catalog,indent=2)+'\n'),('regional-music-provenance.json',json.dumps(proof,indent=2)+'\n'),('REGIONAL-MUSIC-CREDITS.txt','\n'.join(credits)+'\n')]:
  target=out/name;target.write_text(text);write_meta(target)

def main(cache,out,through,spec_path=SPEC):
 spec=json.loads(spec_path.read_text());rows=selected_rows(spec,through)
 cache.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True);write_meta(out)
 (out/'Music').mkdir(exist_ok=True);write_meta(out/'Music')
 version=subprocess.check_output(['ffmpeg','-version'],text=True).splitlines()[0];results=[]
 with tempfile.TemporaryDirectory(prefix='coo-regional-music-') as folder:
  work=Path(folder)
  for row in rows:
   source=cache/row['source'];download_source(source,spec['sources'][row['source']])
   raw=work/'source.f32';decoded=work/'output.f32'
   subprocess.run(['ffmpeg','-v','error','-y','-i',str(source),'-ar',str(RATE),'-ac','2','-f','f32le','-acodec','pcm_f32le',str(raw)],check=True)
   before=raw_stats(raw)
   if before['frames']!=spec['sources'][row['source']]['decodedFrames']:raise ValueError('Source decode no longer matches reviewed complete composition')
   gain=music_gain(before['peak']);target=out/row['path'];temp=work/(row['id']+'.ogg')
   subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(RATE),'-ac','2','-i',str(raw),'-af','volume='+format(gain,'.12g'),'-c:a','libvorbis','-q:a','4','-fflags','+bitexact','-flags:a','+bitexact','-metadata','title='+row['title']+' ('+row['mix']+')','-metadata','artist='+row['author'],'-metadata','copyright='+row['credit'],'-metadata','license='+row['licenseUrl'],'-metadata','comment='+row['changes'],str(temp)],check=True)
   subprocess.run(['ffmpeg','-v','error','-y','-i',str(temp),'-f','f32le','-acodec','pcm_f32le',str(decoded)],check=True)
   after=raw_stats(decoded)
   if after['frames']!=before['frames']:raise ValueError('Complete composition frame count changed')
   if after['peak']>=.99 or not 180<=after['frames']/RATE<=900:raise ValueError('Music signal/duration outside contract')
   if temp.stat().st_size>8*1024*1024:raise ValueError('Individual compressed track exceeds8MiB')
   temp.replace(target);write_meta(target,audio=True)
   results.append(dict(row,outputSha256=sha(target),outputBytes=target.stat().st_size,sourceDecodedFrames=before['frames'],preparedFrames=after['frames'],preparationGain=gain,sampleRate=RATE,channels=2,decodedPeak=after['peak'],decodedRms=after['rms']))
   print(row['path'],round(after['frames']/RATE,3),target.stat().st_size,flush=True)
 if sum(r['outputBytes'] for r in results)>32*1024*1024:raise ValueError('Expansion compressed budget exceeds32MiB')
 write_documents(spec,rows,results,out,through,version)

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--cache',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--through-iteration',type=int,choices=STAGES,required=True);args=parser.parse_args();main(args.cache,args.output,args.through_iteration)
