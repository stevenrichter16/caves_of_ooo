"""Draft-audio delivery checks, with explicit falsification controls."""
import json,hashlib,wave
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly,welch
ROOT=Path(__file__).resolve().parent
IDS=['flaming_hands','jet_blast','ground_surge','rime_grip','calm','conjure_rain']
def read(path):
 with wave.open(str(path)) as w:assert (w.getframerate(),w.getnchannels(),w.getsampwidth())==(48000,1,3)
 sr,x=wavfile.read(path);return x.astype(np.float64)/2147483648

def signal(x):
 assert np.all(np.isfinite(x)) and np.sqrt(np.mean(x*x))>.005,'finite audible signal'
 assert np.max(np.abs(resample_poly(x,4,1)))<.72,'peak headroom'
 assert abs(np.mean(x))<.002,'DC'
 assert max(abs(x[0]),abs(x[-1]))<1e-5,'quiet boundaries'

def layers_match(mix,layers):
 assert len(layers)==4,'four physical layers'
 assert np.max(np.abs(sum(layers)-mix))<6e-7,'exact layer recombination'
 for layer in layers:assert np.sqrt(np.mean(layer*layer))/np.sqrt(np.mean(mix*mix))>.06,'meaningful layer contribution'

def contact_match(layer,impact,at):
 assert np.max(np.abs(layer[:at]))==0,'no early contact'
 assert np.max(np.abs(layer[at:]-impact))<2e-7,'contact relative stem placement'

def distinct(variants):
 assert all(np.corrcoef(variants[0],x)[0,1]<.9 for x in variants[1:]),'distinct variants'

def reject(fn):
 try:fn()
 except AssertionError:return True
 raise AssertionError('Countercheck accepted broken audio')

def main():
 m=json.loads((ROOT/'manifest.json').read_text());rows=[];counter=[]
 assert [x['id'] for x in m['spells']]==IDS
 for spelling in m['spells']:
  variants=[]
  for v in spelling['variants']:
   mix=read(ROOT/v['mix']);layers=[read(ROOT/p) for p in v['layers']];signal(mix);layers_match(mix,layers)
   impact=read(ROOT/v['contact']);at=round(spelling['contactSeconds']*48000)
   contact_match(layers[-1],impact,at)
   f,p=welch(mix,48000,nperseg=4096);low=float(p[(f>=50)&(f<=650)].sum()/p.sum())
   assert low>.16,'substantial low/mid physical body'
   variants.append(mix)
   rows.append({'spell':spelling['id'],'variant':v['variant'],'rms':float(np.sqrt(np.mean(mix*mix))),'lowMidFraction':low,'oversampledPeak':float(np.max(np.abs(resample_poly(mix,4,1))))})
  distinct(variants)
  counter.append(reject(lambda:distinct([variants[0],variants[0]])))
  counter.append(reject(lambda:contact_match(np.roll(layers[-1],480),impact,at)))
  counter.append(reject(lambda:layers_match(mix,layers[:-1])))
  counter.append(reject(lambda:layers_match(mix,[np.zeros_like(layers[0])]+layers[1:])))
 for name,sha in m['files'].items():assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest()==sha
 counter.extend([reject(lambda:signal(np.zeros(48000))),reject(lambda:signal(np.ones(48000)))])
 # The shared review reel must preserve every selected mix at original speed/gain.
 expected=np.concatenate([np.zeros(12000)]+[np.concatenate([read(ROOT/s['variants'][0]['mix']),np.zeros(28800)]) for s in m['spells']])
 assert np.max(np.abs(read(ROOT/'preview/starter_spells_design_reel.wav')-expected))<2e-7
 result={'pass':True,'designOnly':True,'variants':rows,'counterchecksPassed':len(counter),'filesHashed':len(m['files']),'honesty':'Numerical measurements do not verify subjective sound quality or game playback.'}
 (ROOT/'verification/green.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
if __name__=='__main__':main()
