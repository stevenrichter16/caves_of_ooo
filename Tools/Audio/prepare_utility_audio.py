#!/usr/bin/env python3
"""Stage approved utility audio through10–14; never writes the baseline Assets."""
import argparse, copy, hashlib, json, pathlib, re, shutil, subprocess, tempfile
import numpy as np
from prepare_world_audio import meta
from prepare_place_audio import download, extract, prepare, RATE
SPEC=pathlib.Path(__file__).with_name('utility_audio_sources.json')
CC0_SHA='a2010f343487d3f7618affe54f789f5487602331c0a8d03f49e9a7c547cf0499'
TEXTS=('utility-catalog.json','utility-provenance.json','UTILITY-CREDITS.txt')
def require(ok,message):
 if not ok:raise ValueError(message)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def selected_spec(path,through):
 require(type(through) is int and 10<=through<=14,'--through must be integer10..14')
 full=json.loads(path.read_text());require(full.get('schemaVersion')==1,'Unknown utility spec')
 result=copy.deepcopy(full);rows=full.get('clips',[]);ids=set();paths=set()
 for r in rows:
  p=pathlib.PurePosixPath(r['path']);require(type(r.get('iteration')) is int and 10<=r['iteration']<=14,'Invalid source iteration')
  require(p.parts==('Utility',p.name) and p.suffix=='.ogg' and r['kind']=='sfx' and re.fullmatch('[a-z0-9_]+',r['id']),'Unsafe source path/kind/id')
  require(r['id'] not in ids and r['path'] not in paths,'Duplicate source identity');ids.add(r['id']);paths.add(r['path'])
  require(r['source'] in full['sources'] and pathlib.PurePosixPath(r['source']).name==r['source'],'Invalid archive name')
  require(r['license']=='CC0-1.0' and r['licenseUrl']=='https://creativecommons.org/publicdomain/zero/1.0/','Unapproved utility license')
  require(re.fullmatch('[0-9a-f]{64}',r.get('approvedOutputSha256','')) and re.fullmatch('[0-9a-f]{64}',r.get('memberSha256','')),'Missing approved output/member hashes')
  require(all(r.get(k) for k in ('cue','author','title','page','credit','changes','member')) and type(r['gain']) in (int,float) and 0<r['gain']<=1,'Incomplete source attribution/binding')
 result['clips']=[copy.deepcopy(r) for r in rows if r['iteration']<=through];require(result['clips'],'Empty selected palette')
 result['sources']={name:copy.deepcopy(full['sources'][name]) for name in dict.fromkeys(r['source'] for r in result['clips'])}
 result['soundAliases']=[copy.deepcopy(a) for a in full.get('soundAliases',[]) if a['iteration']<=through]
 result['throughIteration']=through
 for a in result['soundAliases']:require(sum(r['id']==a['target'] for r in result['clips'])==1 and a['meaning'],'Missing alias target/honesty')
 return result

def catalog_for(spec):
 cat=dict(schemaVersion=1,music=[],ambience=[],details=[],sounds=[])
 for r in spec['clips']:cat['sounds'].append(dict(id=r['cue'],resource='Audio/World/'+r['path'][:-4],gain=r['gain']))
 for a in spec['soundAliases']:
  r=next(r for r in spec['clips'] if r['id']==a['target']);cat['sounds'].append(dict(id=a['id'],resource='Audio/World/'+r['path'][:-4],gain=a['gain']))
 return cat

def baseline_proof(world):
 proof={}
 for name in ('provenance.json','places-provenance.json','atmospheric-provenance.json'):
  manifest=json.loads((world/name).read_text())
  for row in manifest['clips']:
   p=world/row['path'];require(p.is_file() and not p.is_symlink() and sha(p)==row['outputSha256'],'Prior audio changed: '+row['path']);require(row['path'] not in proof,'Duplicate prior source');proof[row['path']]=sha(p)
 require(len(proof)==73,'Expected original approved73 prior recordings')
 license=world/'Licenses/CC0-1.0.txt';require(license.is_file() and sha(license)==CC0_SHA,'Complete original CC0 license changed/missing')
 return proof

def credits_for(spec):
 lines=['UTILITY ACTION AUDIO CREDITS',f'Approved cumulative action palette through iteration{spec["throughIteration"]}. All additional recordings are CC0 1.0; no new music or Qud recording is included.','']
 for a in spec['soundAliases']:lines.extend([a['id']+': '+a['meaning'],'This is a quieter design alias, not an additional original recording.',''])
 for r in spec['clips']:lines.extend([r['path'],r['credit'],r['page'],'License: '+r['licenseUrl'],'Changes: '+r['changes'],''])
 lines.extend(['Complete CC0 legal text remains in Audio/World/Licenses/CC0-1.0.txt.','Full original archive/member hashes, exact transformations and output hashes are in utility-provenance.json.','No artist endorsement or subjective listening claim.'])
 return '\n'.join(lines)+'\n'

def prepare_assets(spec_path,cache,out,through,baseline_world,previous=None):
 spec=selected_spec(pathlib.Path(spec_path),through);cache=pathlib.Path(cache);out=pathlib.Path(out);baseline_world=pathlib.Path(baseline_world).resolve();previous=pathlib.Path(previous) if previous else None
 require(not out.is_symlink() and not out.resolve().is_relative_to(baseline_world) and not baseline_world.is_relative_to(out.resolve()),'Output must be a separate staging root outside baseline Assets')
 before=baseline_proof(baseline_world);cache.mkdir(parents=True,exist_ok=True)
 for name,source in spec['sources'].items():download(source,cache/name)
 allowed={r['path'] for r in spec['clips']}
 require(not (out/'Utility').is_symlink(),'Output Utility alias')
 if (out/'Utility').exists():require({p.relative_to(out).as_posix() for p in (out/'Utility').rglob('*.ogg')}<=allowed,'Output already contains a later/unapproved utility clip; use a fresh stage')
 for rel in [*allowed,*[p+'.meta' for p in allowed],'Utility.meta',*TEXTS,*[p+'.meta' for p in TEXTS],'copy-map.json']:
  require(not (out/rel).is_symlink(),'Output file alias: '+rel)
 prior={}
 if previous:
  old=json.loads((previous/'utility-provenance.json').read_text());prior={r['path']:r for r in old['clips']}
  for row in spec['clips']:
   if row['path'] not in prior:continue
   info=prior[row['path']];p=previous/row['path'];require(all(info.get(k)==v for k,v in row.items()) and sha(p)==row['approvedOutputSha256'] and sha(p)==info['outputSha256'],'Earlier utility data changed; will not silently regenerate')
   require(p.with_suffix('.ogg.meta').is_file() and not p.is_symlink() and not p.with_suffix('.ogg.meta').is_symlink(),'Earlier importer missing/aliased')
 (out/'Utility').mkdir(parents=True,exist_ok=True)
 if previous:
  for rel in ['Utility.meta',*[p+'.meta' for p in TEXTS]]:
   p=previous/rel;require(p.is_file() and not p.is_symlink(),'Previous shared utility meta missing/aliased')
   if (out/rel).exists():require((out/rel).read_bytes()==p.read_bytes(),'Existing stage has another Unity identity')
   else:shutil.copyfile(p,out/rel)
 meta(out/'Utility');results=[]
 with tempfile.TemporaryDirectory(prefix='coo-utility-prep-') as td:
  work=pathlib.Path(td)
  for row in spec['clips']:
   target=out/row['path']
   if row['path'] in prior:
    source=previous/row['path']
    if source.resolve()!=target.resolve():
     shutil.copyfile(source,target);shutil.copyfile(source.with_suffix('.ogg.meta'),target.with_suffix('.ogg.meta'))
    results.append(prior[row['path']]);continue
   content=extract(cache/row['source'],row['member']);require(hashlib.sha256(content).hexdigest()==row['memberSha256'],'Pinned archive member changed')
   rawsource=work/(row['id']+'.wav');rawsource.write_bytes(content)
   raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(rawsource),'-ar',str(RATE),'-ac','1','-f','f32le','-'])
   original=np.frombuffer(raw,dtype='<f4').copy().reshape(-1,1);signal,gain=prepare(original,row);require(0<len(signal)/RATE<3.5,'Utility phrase exceeds existing bound')
   rawfile=work/(row['id']+'.f32');rawfile.write_bytes(signal.astype('<f4').tobytes())
   subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(RATE),'-ac','1','-i',str(rawfile),'-c:a','libvorbis','-q:a','5','-fflags','+bitexact','-flags:a','+bitexact','-metadata','title='+row['title'],'-metadata','artist='+row['author'],'-metadata','copyright='+row['credit'],'-metadata','license='+row['licenseUrl'],'-metadata','comment='+row['changes'],str(target)],check=True)
   require(sha(target)==row['approvedOutputSha256'],'Encoder output differs from already approved recording bytes; do not replace an approved recording without review')
   meta(target,'sfx');decoded=np.frombuffer(subprocess.check_output(['ffmpeg','-v','error','-i',str(target),'-f','f32le','-']),dtype='<f4');require(np.isfinite(decoded).all() and .01<float(np.max(np.abs(decoded)))<.5,'Decoded signal outside existing bounds')
   results.append(dict(row,outputSha256=sha(target),outputBytes=target.stat().st_size,sourceDecodedFrames=len(original),preparedFrames=len(signal),preparationGain=gain,decodedPeak=float(np.max(np.abs(decoded))),decodedRms=float(np.sqrt(np.mean(decoded.astype('float64')**2)))))
 provenance=dict(schemaVersion=1,sourceVerificationDate=spec['sourceVerificationDate'],throughIteration=through,tool=subprocess.check_output(['ffmpeg','-version'],text=True).splitlines()[0],sources=spec['sources'],clips=results,soundAliases=spec['soundAliases'])
 for name,value in {'utility-catalog.json':json.dumps(catalog_for(spec),indent=2)+'\n','utility-provenance.json':json.dumps(provenance,indent=2)+'\n','UTILITY-CREDITS.txt':credits_for(spec)}.items():(out/name).write_text(value);meta(out/name)
 files=['Utility.meta',*TEXTS,*[p+'.meta' for p in TEXTS]]
 for row in spec['clips']:files.extend([row['path'],row['path']+'.meta'])
 mapping=dict(schemaVersion=1,throughIteration=through,resources=[dict(source=rel,destination='Assets/Resources/Audio/World/'+rel,sha256=sha(out/rel),policy='preserve-existing-identical' if rel.endswith('.meta') or rel.endswith('.ogg') else 'replace-utility-metadata-only') for rel in files],neverCopy=['World.meta','Music.meta','Licenses','earlier original73 palettes'])
 (out/'copy-map.json').write_text(json.dumps(mapping,indent=2)+'\n');require(before==baseline_proof(baseline_world),'Prior73 changed while preparing')
 return dict(throughIteration=through,assetCount=len(results),audioBytes=sum(r['outputBytes'] for r in results),priorAssetCount=len(before),priorAssetsUnchanged=True,unityImportVerified=False,subjectiveListeningVerified=False)

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--spec',type=pathlib.Path,default=SPEC);p.add_argument('--cache',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--through',type=int,required=True,choices=range(10,15));p.add_argument('--baseline-world',type=pathlib.Path,required=True);p.add_argument('--previous',type=pathlib.Path);a=p.parse_args();print(json.dumps(prepare_assets(a.spec,a.cache,a.output,a.through,a.baseline_world,a.previous),indent=2))
