#!/usr/bin/env python3
"""Check staged/integrated utility assets against the phase-limited approved spec."""
import argparse, hashlib, json, pathlib, re, subprocess, sys
import numpy as np
from prepare_utility_audio import SPEC, TEXTS, selected_spec, catalog_for, credits_for, baseline_proof
from verify_world_audio import importer_errors

def verify(root,spec_path,through,baseline_world,decode=True):
 root=pathlib.Path(root);findings=[];assets=[];total=pcm=0;prior={}
 try:
  spec=selected_spec(pathlib.Path(spec_path),through);prior=baseline_proof(pathlib.Path(baseline_world))
  provenance=json.loads((root/'utility-provenance.json').read_text());catalog=json.loads((root/'utility-catalog.json').read_text());credits=(root/'UTILITY-CREDITS.txt').read_text()
 except (OSError,ValueError,KeyError) as error:return dict(passed=False,throughIteration=through,assetCount=0,findings=['missing/invalid source metadata: '+str(error)],assets=[],subjectiveListeningVerified=False,unityImportVerified=False)
 if provenance.get('schemaVersion')!=1 or provenance.get('throughIteration')!=through:findings.append('wrong provenance phase/schema')
 if provenance.get('sources')!=spec['sources'] or provenance.get('soundAliases')!=spec['soundAliases']:findings.append('source/alias provenance differs')
 if catalog!=catalog_for(spec):findings.append('catalog differs from selected source cues/gains/alias')
 if credits!=credits_for(spec):findings.append('full selected credits differ')
 rows=provenance.get('clips',[]);byname={r.get('path'):r for r in rows};expected={r['path'] for r in spec['clips']}
 if len(rows)!=len(expected) or set(byname)!=expected:findings.append('wrong selected provenance set')
 actual={p.relative_to(root).as_posix() for p in (root/'Utility').rglob('*.ogg')}
 if actual!=expected:findings.append('Utility files contain missing/future/unreferenced recordings')
 for row in spec['clips']:
  bad=[];stats={};path=root/row['path'];info=byname.get(row['path'],{})
  if not path.is_file() or path.is_symlink():bad.append('missing/aliased audio')
  else:
   total+=path.stat().st_size;digest=hashlib.sha256(path.read_bytes()).hexdigest()
   if digest!=row['approvedOutputSha256'] or info.get('outputSha256')!=digest or info.get('outputBytes')!=path.stat().st_size:bad.append('approved bytes/provenance mismatch')
   if any(info.get(k)!=v for k,v in row.items()):bad.append('original source metadata changed')
   frames=info.get('preparedFrames');goodframes=type(frames) is int and frames>0
   if not goodframes:bad.append('invalid prepared frame count')
   else:pcm+=frames*4
   meta=path.with_suffix('.ogg.meta')
   if not meta.is_file() or meta.is_symlink():bad.append('missing/aliased importer')
   else:
    text=meta.read_text();bad+=importer_errors(text,'sfx')
    if not re.search(r'^\s*normalize: 0\s*$',text,re.M):bad.append('unexpected importer normalization')
   if decode:
    try:
     probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration:stream=sample_rate,channels','-of','json',str(path)]));stream=probe['streams'][0];rate=int(stream['sample_rate']);channels=int(stream['channels'])
     raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-']);signal=np.frombuffer(raw,dtype='<f4')
     if len(signal)==0 or not np.isfinite(signal).all() or channels!=1 or rate!=44100:bad.append('empty/nonfinite/nonmono/non44100 signal')
     else:
      peak=float(np.max(np.abs(signal)));rms=float(np.sqrt(np.mean(signal.astype(np.float64)**2)));duration=len(signal)/rate
      if not .01<peak<.5 or rms<=1e-5 or not .035<duration<3.5:bad.append('signal outside approved short-cue headroom/duration')
      if goodframes and (abs(float(probe['format']['duration'])*rate-frames)>1 or abs(len(signal)-frames)>1152):bad.append('container/decoded frame duration changed')
      stats=dict(durationSeconds=duration,sampleRate=rate,channels=channels,peakDbfs=20*np.log10(max(peak,1e-12)),rmsDbfs=20*np.log10(max(rms,1e-12)),decodedFloatBytes=signal.size*4,outputSha256=digest)
    except (ValueError,KeyError,subprocess.CalledProcessError) as error:bad.append('decode error: '+str(error))
  findings.extend(row['path']+': '+e for e in bad);assets.append(dict(path=row['path'],errors=bad,**stats))
 guids=[]
 for p in [root/'Utility.meta',*[root/(name+'.meta') for name in TEXTS],*[p.with_suffix('.ogg.meta') for p in (root/'Utility').rglob('*.ogg')]]:
  if not p.is_file() or p.is_symlink():findings.append('missing/aliased metadata '+p.name);continue
  match=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(),re.M)
  if not match:findings.append('invalid GUID '+p.name)
  else:guids.append(match[1])
 if len(guids)!=len(set(guids)):findings.append('duplicate selected Unity GUID')
 if total>spec['budgets']['compressedBytes'] or pcm>spec['budgets']['shortDecodedFloatBytes']:findings.append('approved compressed/PCM budget exceeded')
 return dict(passed=not findings,throughIteration=through,assetCount=len(assets),audioBytes=total,shortDecodedFloatBytes=pcm,decodedSignalChecked=decode,priorAssetCount=len(prior),priorAssetsUnchanged=bool(prior),priorAssetSha256=prior,cc0LicenseSha256='a2010f343487d3f7618affe54f789f5487602331c0a8d03f49e9a7c547cf0499',sourceSpecSha256=hashlib.sha256(pathlib.Path(spec_path).read_bytes()).hexdigest(),subjectiveListeningVerified=False,unityImportVerified=False,findings=findings,assets=assets)

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--spec',type=pathlib.Path,default=SPEC);p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--through',type=int,required=True,choices=range(10,15));p.add_argument('--baseline-world',type=pathlib.Path,required=True);p.add_argument('--report',type=pathlib.Path,required=True);a=p.parse_args();r=verify(a.root,a.spec,a.through,a.baseline_world);a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items() if k not in ('assets','priorAssetSha256')},indent=2));sys.exit(0 if r['passed'] else 1)
