#!/usr/bin/env python3
"""Verify staged/full regional music artifacts. No Unity or listening inference.

Long waveforms decode to a temporary file and are scanned in ten-second chunks;
no whole-composition float buffer or source download is required for verification.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import tempfile
from prepare_regional_music import selected_rows, raw_stats, sha, RATE, SPEC

REPO=Path(__file__).resolve().parents[2]

def result(assets,findings,license_hash=None):
 return dict(passed=not findings and all(not row['errors'] for row in assets),assetCount=len(assets),
  audioBytes=sum(row['bytes'] for row in assets),findings=findings,assets=assets,
  licenseSha256=license_hash,subjectiveListeningVerified=False,unityImportVerified=False)

def verify(folder,spec_path=SPEC,license_file=None):
 folder=Path(folder);assets=[];findings=[]
 try:
  spec=json.loads(Path(spec_path).read_text());proof=json.loads((folder/'regional-music-provenance.json').read_text())
  if not isinstance(proof,dict) or proof.get('schemaVersion')!=1:raise ValueError('Unknown output provenance schema')
  selected=selected_rows(spec,proof['throughIteration'])
  catalog=json.loads((folder/'regional-music-catalog.json').read_text());credits=(folder/'REGIONAL-MUSIC-CREDITS.txt').read_text()
  rows=proof['clips']
  if not isinstance(rows,list) or any(not isinstance(row,dict) for row in rows):raise ValueError('Invalid output clip rows')
 except (OSError,ValueError,KeyError,TypeError) as error:
  return result([],['Missing/invalid music metadata: '+str(error)])
 actual={row.get('path'):row for row in rows}
 if len(rows)!=len(actual) or set(actual)!={row['path'] for row in selected}:findings.append('Provenance rows differ from exact selected manifest')
 expected=dict(schemaVersion=1,music=[dict(id=row['context'],resource='Audio/World/'+str(Path(row['path']).with_suffix('')),gain=row['runtimeGain']) for row in selected],ambience=[],sounds=[],details=[])
 if catalog!=expected:findings.append('Catalog differs from exact staged contexts/order/gains/groups')
 if proof.get('sources')!={row['source']:spec['sources'][row['source']] for row in selected}:findings.append('Pinned official source provenance differs')
 license_path=Path(license_file) if license_file is not None else folder/'Licenses/CC-BY-4.0.txt';license_hash=None
 try:
  text=license_path.read_text()
  if len(text)<3000 or 'Attribution 4.0 International' not in text:findings.append('Complete CC-BY-4.0 license missing or invalid')
  else:license_hash=sha(license_path)
 except OSError:findings.append('Complete CC-BY-4.0 license missing')
 guids=set()
 with tempfile.TemporaryDirectory(prefix='coo-music-verify-') as tmp:
  for row in selected:
   path=folder/row['path'];record=actual.get(row['path'],{});errors=[];stats={};size=path.stat().st_size if path.is_file() else 0;digest=None;rate=None;channels=None
   if any(record.get(key)!=value for key,value in row.items()):errors.append('Output attribution/context differs from independent source spec')
   source_frames=spec['sources'][row['source']]['decodedFrames']
   if type(record.get('sourceDecodedFrames')) is not int or record['sourceDecodedFrames']!=source_frames or type(record.get('preparedFrames')) is not int or record['preparedFrames']!=source_frames:
    errors.append('Full composition source/prepared frame receipt changed')
   if not path.is_file():errors.append('Missing waveform')
   else:
    digest=sha(path)
    if digest!=record.get('outputSha256') or type(record.get('outputBytes')) is not int or size!=record['outputBytes']:errors.append('Waveform bytes/hash differ from preparation receipt')
    try:
     probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=sample_rate,channels','-of','json',str(path)]))
     streams=probe['streams']
     if len(streams)!=1:raise ValueError('Expected exactly one audio stream')
     rate=int(streams[0]['sample_rate']);channels=streams[0]['channels']
     if rate!=RATE or channels!=2:errors.append('Actual waveform is not stereo44100Hz')
     else:
      raw=Path(tmp)/'decoded.f32';subprocess.run(['ffmpeg','-v','error','-y','-i',str(path),'-f','f32le','-acodec','pcm_f32le',str(raw)],check=True)
      stats=raw_stats(raw)
      if stats['frames']!=source_frames:errors.append('Decoded full composition frame count changed')
      if stats['peak']>=.99 or stats['rms']<.00001:errors.append('Clipping/silent signal outside contract')
    except (OSError,ValueError,KeyError,TypeError,subprocess.CalledProcessError) as error:errors.append('Decode/probe failure: '+str(error))
    if size>8*1024*1024:errors.append('Compressed track exceeds8MiB')
   meta=Path(str(path)+'.meta');text=meta.read_text() if meta.is_file() else ''
   match=re.search(r'^guid: ([0-9a-f]{32})$',text,re.M)
   if not match:errors.append('Missing/invalid import GUID')
   elif match.group(1) in guids:errors.append('Duplicate music import GUID')
   else:guids.add(match.group(1))
   for required in ['loadType: 2','preloadAudioData: 0','loadInBackground: 1','forceToMono: 0','normalize: 0']:
    if required not in text:errors.append('Missing streamed stereo import contract: '+required)
   if row['credit'] not in credits or row['licenseUrl'] not in credits or row['page'] not in credits:errors.append('Required original source credit absent')
   assets.append(dict(path=row['path'],errors=errors,bytes=size,sha256=digest,decodedFrames=stats.get('frames'),decodedPeak=stats.get('peak'),decodedRms=stats.get('rms'),sampleRate=rate,channels=channels,
    source=row['source'],completeCompositionVerified=not errors,unityImportVerified=False))
   findings.extend(row['path']+': '+error for error in errors)
 if sum(row['bytes'] for row in assets)>32*1024*1024:findings.append('Expansion exceeds32MiB')
 return result(assets,findings,license_hash)

if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('--root',type=Path,default=REPO/'Assets/Resources/Audio/World')
 parser.add_argument('--spec',type=Path,default=SPEC)
 parser.add_argument('--license-file',type=Path,help='Use an existing complete license when validating a staging directory')
 parser.add_argument('--report',type=Path,required=True)
 args=parser.parse_args();receipt=verify(args.root,args.spec,args.license_file);args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(receipt,indent=2)+'\n')
 print(json.dumps({key:value for key,value in receipt.items() if key!='assets'},indent=2));raise SystemExit(0 if receipt['passed'] else 1)
