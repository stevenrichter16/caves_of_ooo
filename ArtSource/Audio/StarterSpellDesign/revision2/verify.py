"""Verify material lineage and delivery; never equate numeric distance with listening approval."""
import json,hashlib,wave
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly
ROOT=Path(__file__).resolve().parent
IDS=['flaming_hands','jet_blast','ground_surge','rime_grip','calm','conjure_rain']
def read(p):
 with wave.open(str(p)) as w: assert (w.getframerate(),w.getnchannels(),w.getsampwidth())==(48000,1,3)
 _,x=wavfile.read(p);return x.astype(np.float64)/2147483648

def signal(x):
 assert len(x)>4800 and np.all(np.isfinite(x)),'finite nonempty delivery'
 assert np.sqrt(np.mean(x*x))>.004,'non-silent mix'
 assert np.max(np.abs(resample_poly(x,4,1)))<.7081,'headroom'
 assert abs(np.mean(x))<.001,'DC'
 assert max(abs(x[0]),abs(x[-1]))<1e-5,'quiet boundaries'

def reconstruct(mix,layers):
 assert layers and np.max(np.abs(sum(layers)-mix))<1e-6,'layer sum'

def reject(fn):
 try:fn()
 except AssertionError:return True
 raise AssertionError('negative control accepted broken signal')

def main():
 m=json.loads((ROOT/'manifest.json').read_text());assert [s['id'] for s in m['spells']]==IDS
 rows=[];controls=[];dominant=[]
 for spell in m['spells']:
  mixes=[]
  for v in spell['variants']:
   mix=read(ROOT/v['mix']);layers=[read(ROOT/l['file']) for l in v['layers']]
   signal(mix);reconstruct(mix,layers);mixes.append(mix)
   for l,x in zip(v['layers'],layers):
    assert l['events'],'every layer has recorded-source lineage'
    assert np.sqrt(np.mean(x*x))/np.sqrt(np.mean(mix*mix))>.025,'layer contributes'
    for event in l['events']:
     p=ROOT/event['source'];assert p.exists()
     assert hashlib.sha256(p.read_bytes()).hexdigest()==event['sourceSha256']
   rows.append({'id':spell['id'],'variant':v['variant'],'seconds':len(mix)/48000,'layers':len(layers),'rms':float(np.sqrt(np.mean(mix*mix))),'oversampledPeak':float(np.max(np.abs(resample_poly(mix,4,1))))})
   controls.append(reject(lambda:reconstruct(mix,layers[1:])))
  assert all(np.corrcoef(mixes[0],x)[0,1]<.99 for x in mixes[1:]),'variants are not duplicate files'
  dominant.append(spell['dominantSource'])
  # Each isolated layer is presented at its real mix gain, followed by its exact composite.
  expected=np.concatenate([np.zeros(12000)]+[np.concatenate([read(ROOT/l['file']),np.zeros(16800)]) for l in spell['variants'][0]['layers']]+[read(ROOT/spell['variants'][0]['mix'])])
  assert np.max(np.abs(read(ROOT/spell['breakdown'])-expected))<2e-7
 assert len(set(dominant))==len(IDS),'distinct dominant source per spell'
 expected=np.concatenate([np.zeros(12000)]+[np.concatenate([read(ROOT/s['variants'][0]['mix']),np.zeros(24000)]) for s in m['spells']])
 assert np.max(np.abs(read(ROOT/m['reel'])-expected))<2e-7
 for p,sha in m['audioHashes'].items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha
 controls+=[reject(lambda:signal(np.zeros(48000))),reject(lambda:signal(np.ones(48000)))]
 result={'pass':True,'variants':rows,'negativeControls':len(controls),'audioHashes':len(m['audioHashes']),'bounds':'Sources/layers/delivery verified. Distinct materials and gestures do not prove subjective auditory success; listener review required.'}
 (ROOT/'verification/green.json').write_text(json.dumps(result,indent=2)+'\n');print('PASS:',len(rows),'mixes,',len(controls),'negative controls,',len(m['audioHashes']),'audio hashes')
if __name__=='__main__':main()
