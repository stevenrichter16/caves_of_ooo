#!/usr/bin/env python3
"""Download verified licensed sources and prepare only the agreed compact world set.
Python3 + numpy + ffmpeg/ffprobe + curl. Never invokes Unity. Existing catalog/spells
are outside this script's ownership. Source archive/member hashes are pinned.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import uuid
import zipfile
import numpy as np

REPO=Path(__file__).resolve().parents[2]
SPEC=Path(__file__).with_name('world_audio_sources.json')
RATE=44100


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def download(url,path,expected=None):
    if not path.exists():
        temp=path.with_suffix(path.suffix+'.part')
        subprocess.run(['curl','--fail','--silent','--show-error','--location','--max-time','120','--max-filesize','50000000','--output',str(temp),url],check=True)
        temp.replace(path)
    if expected and sha(path)!=expected:raise ValueError('Source hash changed; re-review author source: '+str(path))


def meta(path,kind=None):
    p=Path(str(path)+'.meta')
    if p.exists():return  # Preserve Unity identity on reruns.
    guid=uuid.uuid4().hex
    text='fileFormatVersion: 2\nguid: '+guid+'\n'
    if path.is_dir():text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
    elif kind:
        streaming=kind!='sfx'
        text+='AudioImporter:\n  externalObjects: {}\n  serializedVersion: 8\n  defaultSettings:\n    serializedVersion: 2\n'
        text+=f'    loadType: {2 if streaming else 0}\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 0.65\n    conversionMode: 0\n    preloadAudioData: {0 if streaming else 1}\n'
        text+=f'  platformSettingOverrides: {{}}\n  forceToMono: 0\n  normalize: 0\n  loadInBackground: {1 if streaming else 0}\n  ambisonic: 0\n  3D: 1\n'
    else:text+='TextScriptImporter:\n  externalObjects: {}\n'
    text+='  userData: \n  assetBundleName: \n  assetBundleVariant: \n';p.write_text(text)


def main(cache,out):
    spec=json.loads(SPEC.read_text());cache.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True)
    meta(out)
    for folder in ['Music','Ambience','Sfx','Licenses']:(out/folder).mkdir(exist_ok=True);meta(out/folder)
    for name,row in spec['sources'].items():download(row['url'],cache/name,row['sha256'])
    version=subprocess.check_output(['ffmpeg','-version'],text=True).splitlines()[0]
    results=[]
    with tempfile.TemporaryDirectory(prefix='coo-audio-work-') as td:
        work=Path(td)
        for row in spec['clips']:
            source=cache/row['source']
            if 'member' in row:
                with zipfile.ZipFile(source) as z:data=z.read(row['member'])
                if hashlib.sha256(data).hexdigest()!=row['memberSha256']:raise ValueError('Archive member changed')
                source=work/(row['id']+'.source.ogg');source.write_bytes(data)
            channels=1 if row['kind']=='sfx' else 2
            raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-ar',str(RATE),'-ac',str(channels),'-f','f32le','-acodec','pcm_f32le','-'])
            samples=np.frombuffer(raw,dtype='<f4').copy().reshape(-1,channels)
            original_frames=len(samples)
            if row['kind']=='ambience':
                if row['id']=='water':samples=samples[30*RATE:120*RATE]
                # Rotate through the overlap so the loop boundary is the original
                # adjacent pair x[f-1],x[f], not the unrelated original endpoints.
                overlap=int(.75*RATE)
                weight=np.linspace(0,1,overlap,dtype=np.float32)[:,None]
                blend=samples[-overlap:]*(1-weight)+samples[:overlap]*weight
                samples=np.concatenate((samples[overlap:-overlap],blend))
            if row['kind']=='sfx':
                active=np.flatnonzero(np.abs(samples[:,0])>max(float(np.max(np.abs(samples)))*.002,1e-7))
                if len(active):
                    guard=int(.008*RATE)
                    samples=samples[max(0,active[0]-guard):min(len(samples),active[-1]+guard+1)]
                fade=min(int(.005*RATE),len(samples)//4)
                samples[:fade]*=np.linspace(0,1,fade,dtype=np.float32)[:,None]
                samples[-fade:]*=np.linspace(1,0,fade,dtype=np.float32)[:,None]
                samples=np.pad(samples,((int(.01*RATE),int(.01*RATE)),(0,0)))
            peak=float(np.max(np.abs(samples)))
            ceiling=.35 if row['kind']=='sfx' else (.5 if row['kind']=='ambience' else .75)
            gain=ceiling/max(peak,1e-12)
            if row['kind']=='music':gain=min(gain,1)  # Keep the composer's dynamics/relative level.
            samples*=gain
            prepared=work/(row['id']+'.f32');prepared.write_bytes(samples.astype('<f4').tobytes())
            target=out/row['path']
            subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(RATE),'-ac',str(channels),'-i',str(prepared),'-c:a','libvorbis','-q:a','4' if row['kind']=='music' else '5','-fflags','+bitexact','-flags:a','+bitexact','-metadata','title='+row['title'],'-metadata','artist='+row['author'],'-metadata','copyright='+row['credit'],'-metadata','license='+row['licenseUrl'],'-metadata','comment='+row['changes'],str(target)],check=True)
            meta(target,row['kind'])
            results.append(dict(row,outputSha256=sha(target),outputBytes=target.stat().st_size,sourceDecodedFrames=original_frames,preparedFrames=len(samples),preparationGain=gain))
            print(row['path'],round(len(samples)/RATE,3),target.stat().st_size,flush=True)
    licenses=out/'Licenses'
    for code,url in [('CC-BY-4.0','https://creativecommons.org/licenses/by/4.0/legalcode.txt'),('CC0-1.0','https://creativecommons.org/publicdomain/zero/1.0/legalcode.txt')]:
        download(url,licenses/(code+'.txt'));meta(licenses/(code+'.txt'))
    for name in ['kenney_rpg-audio.zip','kenney_impact-sounds.zip','kenney_interface-sounds.zip']:
        with zipfile.ZipFile(cache/name) as z:
            p=licenses/(name.removesuffix('.zip')+'.txt');p.write_bytes(z.read('License.txt'));meta(p)
    credits=['WORLD AUDIO CREDITS','', 'Music composed by Scott Buckley. These are independently licensed works; no Caves of Qud music is included.','']
    for row in spec['clips'][:3]:credits += [row['credit'],row['page'],'License: '+row['licenseUrl'],'Changes: '+row['changes'],'']
    credits+=['STREAMING / VIDEO NOTE','Scott Buckley uses YouTube Content ID. To avoid or resolve claims, include the three complete music credit lines above in your video description. In-game credits alone do not guarantee that uploaded videos avoid claims. The game provides independent music volume/mute.','The composer requests no separate resale/reupload to music-streaming platforms and no fingerprint enrollment of remixes. This use synchronizes music with gameplay.','https://www.scottbuckley.com.au/library/using-this-music/','', 'AMBIENCE AND FOLEY (CC0)','']
    for row in spec['clips'][3:]:credits += [row['title'],row['credit'],'Creator: '+row['author'],row['page'],'License: '+row['licenseUrl'],'Changes: '+row['changes'],'']
    credits+=['Full CC-BY4.0 and CC0 license texts and original Kenney notices are included in Licenses. Detailed download/member hashes and preparation changes are in provenance.json.','No artist endorsement is implied.']
    (out/'CREDITS.txt').write_text('\n'.join(credits)+'\n');meta(out/'CREDITS.txt')
    (out/'provenance.json').write_text(json.dumps(dict(schemaVersion=1,sourceVerificationDate='2026-10-10',tool=version,sources=spec['sources'],clips=results),indent=2)+'\n');meta(out/'provenance.json')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--cache',type=Path,required=True);p.add_argument('--output',type=Path,default=REPO/'Assets/Resources/Audio/World');a=p.parse_args();main(a.cache,a.output)
