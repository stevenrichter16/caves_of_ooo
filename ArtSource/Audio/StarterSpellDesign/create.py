"""Original material-based starter spell audition drafts; never imports into Unity."""
import json,hashlib,wave
from pathlib import Path
import numpy as np
from scipy.signal import butter,sosfilt,resample_poly
from scipy.interpolate import PchipInterpolator
from scipy.io import wavfile
ROOT=Path(__file__).resolve().parent
SR=48000
SPECS=[
 ('flaming_hands','Flaming Hands',1.25,.22,['furnace_draft','flame_sheets','embers','heat_bloom']),
 ('jet_blast','Jet Blast',1.45,.27,['deep_current','water_sheet','cavitation','heavy_splash']),
 ('ground_surge','Ground Surge',1.55,.32,['earth_current','braided_arcs','charged_grit','discharge']),
 ('rime_grip','Rime Grip',1.65,.22,['cold_pressure','stressed_ice','frost_grains','grip_lock']),
 ('calm','Calm',1.8,.32,['warm_breath','hollow_resonance','soft_fibers','release']),
 ('conjure_rain','Conjure Rain (starter-book utility)',2.0,.22,['cloud_air','dense_rain','leaf_drops','settling'])]

def noise(rng,n,lo,hi):
 x=sosfilt(butter(2,[lo,hi],btype='bandpass',fs=SR,output='sos'),rng.normal(size=n))
 return x/(np.sqrt(np.mean(x*x))+1e-12)

def finish(x):
 x=sosfilt(butter(2,[42,7600],btype='bandpass',fs=SR,output='sos'),x)
 fade=min(len(x)//5,7200)
 x[:480]*=np.sin(np.linspace(0,np.pi/2,480))**2
 x[-fade:]*=np.cos(np.linspace(0,np.pi/2,fade))**2
 return x

def grains(rng,n,count,lo,hi,seconds,last,kind='noise'):
 x=np.zeros(n)
 for _ in range(count):
  at=int(rng.uniform(.008,last)*SR);length=min(int(rng.uniform(*seconds)*SR),n-at)
  if length<10:continue
  t=np.arange(length)/SR;shape=np.sin(np.linspace(0,np.pi,length))**2*np.exp(-t/max(seconds[0],.01))
  if kind=='drop':
   freq=rng.uniform(lo,hi);wavelet=np.sin(2*np.pi*(freq*t+.35*freq*t*t/seconds[1]))*.65+noise(rng,length,180,4200)*.35
  else:wavelet=noise(rng,length,lo,hi)
  x[at:at+length]+=rng.uniform(.45,1)*shape*wavelet
 return x/(np.sqrt(np.mean(x*x))+1e-12)

def modes(rng,t,frequencies,decay):
 x=np.zeros(len(t))
 for f in frequencies:
  f*=rng.uniform(.98,1.02)
  x+=np.sin(2*np.pi*f*t+rng.uniform(0,2*np.pi))*np.exp(-t/decay)/np.sqrt(len(frequencies))
 return x

def synth(spec,seed):
 sid,label,dur,contact,names=spec;rng=np.random.default_rng(seed);n=round(dur*SR);t=np.arange(n)/SR
 swell=PchipInterpolator([0,.04,.14,contact,max(contact+.15,.45),dur*.68,dur], [0,.09,.45,1,.8,.23,0])(t)
 drift=np.interp(t,np.linspace(0,dur,25),rng.uniform(.8,1.2,25));swell*=drift
 at=round(contact*SR);ti=np.arange(n-at)/SR
 impact_env=(1-np.exp(-ti/.009))*np.exp(-ti/.23)
 if sid=='flaming_hands':
  body=.25*noise(rng,n,55,360)*swell
  texture=(.14*noise(rng,n,240,2100)+.08*noise(rng,n,1900,6200))*swell
  detail=.105*grains(rng,n,210,1200,6200,(.006,.035),dur*.83)*swell
  hit=(.23*noise(rng,len(ti),65,550)+.15*noise(rng,len(ti),420,3500))*impact_env
 elif sid=='jet_blast':
  body=(.25*noise(rng,n,55,310)+.05*noise(rng,n,260,950))*swell
  texture=(.15*noise(rng,n,420,3500)+.05*noise(rng,n,2300,6400))*swell
  detail=.105*grains(rng,n,160,260,950,(.018,.072),dur*.85,'drop')*swell
  hit=(.27*noise(rng,len(ti),60,380)+.19*noise(rng,len(ti),550,4800))*impact_env
  hit+=.11*grains(rng,len(ti),90,300,1350,(.014,.07),len(ti)/SR*.8,'drop')*np.exp(-ti/.38)
 elif sid=='ground_surge':
  # Rough granular current with multiple overlapping arc rates, not a pure mains hum.
  rough=.65+.23*np.sin(2*np.pi*73*t)+.12*np.sin(2*np.pi*119*t)
  body=.26*noise(rng,n,55,430)*swell*rough
  texture=(.105*noise(rng,n,600,3700)*rough+.09*grains(rng,n,180,900,5800,(.008,.04),dur*.78))*swell
  detail=.10*grains(rng,n,120,190,1900,(.02,.065),dur*.88)*swell
  hit=(.22*noise(rng,len(ti),65,600)+.13*noise(rng,len(ti),700,4900))*impact_env
  hit+=.075*grains(rng,len(ti),95,950,5500,(.009,.045),len(ti)/SR*.75)*np.exp(-ti/.3)
 elif sid=='rime_grip':
  body=(.21*noise(rng,n,60,330)+.045*noise(rng,n,400,1500))*swell
  stress=modes(rng,t,[133,207,349,557,913],1.3)
  texture=(.075*stress+.075*noise(rng,n,270,1900))*swell
  detail=.095*grains(rng,n,165,1500,6500,(.009,.055),dur*.82)*swell
  hit=(.17*noise(rng,len(ti),70,490)+.095*modes(rng,ti,[177,291,473,773],.35))*impact_env
  hit+=.1*grains(rng,len(ti),70,850,5100,(.02,.08),len(ti)/SR*.7)*np.exp(-ti/.32)
 elif sid=='calm':
  body=(.20*noise(rng,n,65,360)+.035*noise(rng,n,520,1700))*swell
  texture=(.075*modes(rng,t,[147,223,301,449],2.4)+.055*noise(rng,n,160,800))*swell
  detail=.073*noise(rng,n,850,3700)*swell
  gentle=(1-np.exp(-ti/.08))*np.exp(-ti/.44)
  hit=(.10*modes(rng,ti,[147,294,441],1.2)+.12*noise(rng,len(ti),85,490)+.035*noise(rng,len(ti),650,2600))*gentle
 else:
  # Rain is released at contact, while only canopy air gathers beforehand.
  body=.21*noise(rng,n,70,420)*swell
  wet=np.maximum(0,t-contact);rain_env=(1-np.exp(-wet/.065))*np.exp(-wet/.54)
  texture=(.12*noise(rng,n,650,4300)+.045*noise(rng,n,2400,6900))*rain_env
  detail=.11*grains(rng,n,330,380,1800,(.012,.055),dur*.9,'drop')*rain_env
  hit=(.13*noise(rng,len(ti),90,510)+.09*grains(rng,len(ti),130,400,2300,(.014,.065),len(ti)/SR*.85,'drop'))*(1-np.exp(-ti/.035))*np.exp(-ti/.46)
 layers=[finish(body),finish(texture),finish(detail)]
 hit=finish(hit);last=np.zeros(n);last[at:]=hit;layers.append(last)
 return layers,hit

def export(x,path):
 path.parent.mkdir(parents=True,exist_ok=True)
 master=ROOT/'float-masters'/path.relative_to(ROOT);master.parent.mkdir(parents=True,exist_ok=True)
 wavfile.write(master,SR,x.astype(np.float32))
 integer=np.rint(np.clip(x,-1,1-1/8388608)*8388608).astype(np.int32)
 raw=np.stack((integer&255,(integer>>8)&255,(integer>>16)&255),axis=1).astype(np.uint8)
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(3);w.setframerate(SR);w.writeframes(raw.tobytes())

def main():
 manifest={'revision':1,'status':'audition drafts; not integrated','sampleRate':SR,'bits':24,'channels':1,'provenance':'Original deterministic procedural synthesis; no recordings or external samples.','spells':[],'files':{}}
 mixes=[]
 for index,spec in enumerate(SPECS):
  sid,label,dur,contact,names=spec
  variants=[synth(spec,8100+index*100+v) for v in range(3)]
  peak=max(float(np.max(np.abs(resample_poly(sum(layers),4,1)))) for layers,hit in variants)
  rms=np.mean([np.sqrt(np.mean(sum(layers)**2)) for layers,hit in variants])
  target=.079 if sid=='calm' else .075 if sid=='conjure_rain' else .105
  gain=min(target/rms,10**(-3.2/20)/peak)
  row={'id':sid,'label':label,'durationSeconds':dur,'releaseSeconds':.22,'contactSeconds':contact,'sharedGain':float(gain),'layerNames':names,'variants':[]}
  for v,(layers,hit) in enumerate(variants,1):
   layers=[x*gain for x in layers];hit=hit*gain;mix=sum(layers)
   record={'variant':v,'seed':8100+index*100+v-1,'layers':[]}
   for name,x in zip(names,layers):
    p=ROOT/'layers'/f'{sid}_{v:02}_{name}.wav';export(x,p);record['layers'].append(str(p.relative_to(ROOT)))
   p=ROOT/'contact'/f'{sid}_{v:02}_contact.wav';export(hit,p);record['contact']=str(p.relative_to(ROOT))
   p=ROOT/'mix'/f'{sid}_{v:02}.wav';export(mix,p);record['mix']=str(p.relative_to(ROOT));row['variants'].append(record)
   if v==1:
    mixes.append(mix);export(mix,ROOT/'preview'/f'{sid}_design.wav')
  manifest['spells'].append(row)
 reel=np.concatenate([np.zeros(12000)]+[np.concatenate([x,np.zeros(28800)]) for x in mixes])
 export(reel,ROOT/'preview/starter_spells_design_reel.wav')
 for path in ROOT.rglob('*.wav'):manifest['files'][str(path.relative_to(ROOT))]=hashlib.sha256(path.read_bytes()).hexdigest()
 (ROOT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 print('Created six designs,18 variants,72 layers,18 event-relative finishes,7 previews and float masters.')
if __name__=='__main__':main()
