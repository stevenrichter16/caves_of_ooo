"""Six distinct recorded-material studies. No shared generated noise or synthetic impact."""
import hashlib,json,subprocess,wave,math
from pathlib import Path
from functools import lru_cache
import numpy as np
from scipy.signal import butter,sosfilt,resample_poly
from scipy.io import wavfile
ROOT=Path(__file__).resolve().parent
SR=48000
F='sources/fire_surge/'
W='sources/water_rain/'
I='sources/ice_calm/'
@lru_cache(None)
def decode(name):
 # Float output retains source/decoder overshoots until intentional mix gain.
 raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(ROOT/name),'-ac','1','-ar',str(SR),'-f','f32le','pipe:1'])
 return np.frombuffer(raw,np.float32).astype(np.float64)
def fade(x,attack=.004,release=.015):
 x=x.copy();a=min(len(x)//2,round(attack*SR));b=min(len(x)//2,round(release*SR))
 if a:x[:a]*=np.sin(np.linspace(0,np.pi/2,a))**2
 if b:x[-b:]*=np.sin(np.linspace(np.pi/2,0,b))**2
 return x
class Study:
 def __init__(self,sid,title,dur,contact,v):
  self.sid=sid;self.title=title;self.dur=dur;self.n=round(dur*SR);self.contact=contact;self.v=v;self.layers=[]
 def layer(self,name,role):
  l={'name':name,'role':role,'events':[],'data':np.zeros(self.n)};self.layers.append(l);return l
 def event(self,l,source,start,length,at,rate=1,level=.10,lo=60,hi=7500,reverse=False,curve=None,attack=.006,release=.03,crest=4.5):
  data=decode(source);a=round(start*SR);b=min(len(data),round((start+length)*SR));assert b>a,(source,start,length)
  x=data[a:b].copy()
  if reverse:x=x[::-1]
  if rate!=1:
   # Rational polyphase rate conversion changes pitch and duration together.
   x=resample_poly(x,1000,round(rate*1000))
  x=sosfilt(butter(2,[lo,hi],btype='bandpass',fs=SR,output='sos'),x)
  raw_rms=float(np.sqrt(np.mean(x*x)));gain=level/max(raw_rms,1e-12);x*=gain
  # Soft peak control retains recorded detail while keeping isolated clicks from
  # turning the entire body down. No oscillator/noise is added.
  ceiling=level*crest if crest else None
  if ceiling: x=ceiling*np.tanh(x/ceiling)
  x=fade(x,attack,release)
  if curve is not None:
   t=np.arange(len(x))/SR;x*=np.interp(t,[p[0] for p in curve],[p[1] for p in curve])
  offset=round(at*SR);assert offset>=0
  count=min(len(x),self.n-offset)
  if count>0:l['data'][offset:offset+count]+=x[:count]
  l['events'].append({'source':source,'sourceSha256':hashlib.sha256((ROOT/source).read_bytes()).hexdigest(),'sourceStartSeconds':start,'sourceLengthSeconds':length,'placementSeconds':at,'rate':rate,'pitchSemitones':12*math.log2(rate),'reverse':reverse,'bandHz':[lo,hi],'referenceRms':level,'softPeakCeiling':ceiling,'sourceNormalizationGain':gain,'attackSeconds':attack,'releaseSeconds':release,'curve':curve})
 def finalize(self):
  for l in self.layers:l['data']=fade(sosfilt(butter(2,38,btype='highpass',fs=SR,output='sos'),l['data']),.005,.08)
  return self

def make(sid,v):
 d=v-1;detune=[1,.965,1.045][v-1]
 if sid=='flaming_hands':
  s=Study(sid,'Flaming Hands',1.55,.22,v)
  l=s.layer('palms_catch','Two small match flares prepare the hands.')
  s.event(l,F+'ignition.flac',.32,.28,.025,rate=1.15*detune,level=.06,lo=350,hi=6800)
  s.event(l,F+'ignition.flac',.36,.45,.205,rate=.73*detune,level=.21,lo=90,hi=6000,release=.10)
  l=s.layer('combustion_body','Slowed real fire gives the expanding flame physical weight.')
  s.event(l,F+'fireplace_pagdev.wav',23.8+d*.9,1.0,.16,rate=.78*detune,level=.17,lo=55,hi=1700,curve=[(0,0),(.08,.75),(.2,1),(.48,.9),(.82,.2),(1.3,0)],attack=.035,release=.14)
  l=s.layer('dry_afterburn','Natural fireplace snaps continue as the sheet loses pressure.')
  s.event(l,F+'fire-1.wav',.48+d*.15,1.17,.29,rate=1.06*detune,level=.083,lo=1200,hi=7200,curve=[(0,.6),(.13,1),(.35,.85),(.75,.3),(1.15,0)],release=.10)
 elif sid=='jet_blast':
  s=Study(sid,'Jet Blast',1.7,.27,v)
  l=s.layer('liquid_gather','Hollow moving liquid rather than air.')
  s.event(l,W+'bmaczero_bubbles_loop1.wav',.3+d*.55,.44,.018,rate=.77*detune,level=.083,lo=90,hi=2400,curve=[(0,0),(.08,.9),(.25,1),(.55,0)],attack=.015)
  l=s.layer('pressurized_water','The actual water mass accelerates through release.')
  s.event(l,W+'transitking_wave_01.flac',.25+d*.045,1.06,.17,rate=1.38*detune,level=.16,lo=100,hi=6800,curve=[(0,0),(.055,1),(.34,1),(.73,0)],attack=.02,release=.09)
  l=s.layer('wet_slap','One broad water splash at physical contact.')
  s.event(l,W+'thimras_ocean_splash.ogg',.008,1.2,.27,rate=.95*detune,level=.18,lo=70,hi=7300,attack=.003,release=.16)
  l=s.layer('bubbles_and_runoff','Liquid cavities and small drops remain after the large slap.')
  for at,rate,level in [(.53,.8,.055),(.83,1.05,.045),(1.1,1.3,.035)]:s.event(l,W+'bmaczero_bubbles_single1.wav',0,.31,at+d*.008,rate=rate*detune,level=level,lo=150,hi=4600)
 elif sid=='ground_surge':
  s=Study(sid,'Ground Surge',1.25,.32,v)
  l=s.layer('charging_current','Rough Tesla current establishes in a short held electrical note.')
  s.event(l,F+'continuousspark.wav',.006,.18,.045,rate=.72*detune,level=.10,lo=130,hi=4700,attack=.025,release=.035)
  l=s.layer('advancing_arcs','Connected electrical snaps advance over the four-cell study path.')
  for j,at in enumerate([.22,.248,.279,.32]):s.event(l,F+'spark.wav',.095,.085,at,rate=(1.14-j*.09)*detune,level=.16+j*.016,lo=300,hi=6200,attack=.002,release=.014)
  s.event(l,F+'continuousspark.wav',.008,.18,.325,rate=.86*detune,level=.085,lo=160,hi=4200,curve=[(0,1),(.09,.7),(.21,0)])
  l=s.layer('ground_fracture','A single stony contact gives grounded weight without a thunderclap.')
  s.event(l,F+'rock_break.ogg',0,.4,.32,rate=.67*detune,level=.16,lo=55,hi=2200,attack=.003,release=.05)
  l=s.layer('grit_displacement','Short irregular stone/gravel movement trails the charge.')
  s.event(l,F+'gravel.ogg',.06,.52,.37,rate=.88*detune,level=.095,lo=220,hi=5800,release=.09)
  s.event(l,F+'stone01.ogg',.01,.23,.51+d*.012,rate=.71*detune,level=.035,lo=95,hi=1600)
 elif sid=='rime_grip':
  s=Study(sid,'Rime Grip',1.65,.22,v)
  l=s.layer('inward_ice_stress','An actual ice fracture is drawn backward into the closing hand.')
  s.event(l,I+'ice-shatters/IceShatters/LedasLuzta4.ogg',.12,.32,.025,rate=.77*detune,reverse=True,level=.12,lo=170,hi=5900,attack=.03,release=.018)
  l=s.layer('brittle_lock','Hard ice fractures close in a tight uneven pair.')
  s.event(l,I+'ice-shatters/IceShatters/LedasLuzta33.ogg',.015,.62,.22,rate=.83*detune,level=.19,lo=120,hi=6800,attack=.003,release=.08)
  s.event(l,I+'ice-shatters/IceShatters/LedasLuzta2.ogg',.022,.3,.305,rate=.64*detune,level=.11,lo=65,hi=1900,attack=.003,release=.07)
  l=s.layer('settling_fragments','A few higher brittle pieces settle after the restrained closure.')
  for at,rate,level in [(.56,1.45,.050),(.87,1.12,.034),(1.10,1.6,.023)]:s.event(l,I+'ice-shatters/IceShatters/LedasLuzta.ogg',.05,.32,at+d*.01,rate=rate*detune,level=level,lo=1050,hi=7200,attack=.004,release=.05)
 elif sid=='calm':
  s=Study(sid,'Calm',2.35,.32,v)
  l=s.layer('warm_bowl_body','A real rubbed bowl sings softly, with its hard strike excluded.')
  s.event(l,I+'rubbed-singing-bowl_ryancacophony_public-hq-preview.mp3',26.0+d*.45,2.8,.018,rate=.62*detune,level=.105,lo=85,hi=1900,curve=[(0,0),(.16,.55),(.34,.9),(.7,1),(1.45,.6),(2.32,0)],attack=.08,release=.12,crest=None)
  l=s.layer('resolved_overtone','The same material opens into a quieter related resonance at actual pacification.')
  s.event(l,I+'rubbed-singing-bowl_ryancacophony_public-hq-preview.mp3',27+d*.7,2.4,.32,rate=.93*detune,level=.057,lo=220,hi=2500,curve=[(0,0),(.18,.65),(.54,1),(1.18,.6),(2.03,0)],attack=.10,release=.16,crest=None)
  l=s.layer('human_exhale','A real gentle exhalation releases the tension without an impact.')
  s.event(l,I+'exhale_otherthings_public-hq-preview.mp3',.14,.57,.10,rate=.84*detune,level=.025,lo=200,hi=3000,attack=.065,release=.13)
 else:
  s=Study(sid,'Conjure Rain',2.25,.22,v)
  l=s.layer('leaf_rain','Real fine rain on foliage is the defining body; no bass wind.')
  s.event(l,W+'samsterbirdies_rain_on_leaves_hq.mp3',8+d*5.2,2.02,.22,rate=1,level=.10,lo=240,hi=7600,curve=[(0,0),(.11,.7),(.35,1),(1.15,.9),(1.68,.42),(2.02,0)],attack=.025,release=.12)
  l=s.layer('close_leaf_patter','A quieter low-mid foliage layer gives raindrops body without a water slam.')
  s.event(l,W+'samsterbirdies_rain_on_leaves_hq.mp3',29+d*3.1,1.85,.27,rate=.95*detune,level=.039,lo=150,hi=1550,curve=[(0,0),(.15,1),(.75,.8),(1.3,.65),(1.95,0)],release=.09)
  l=s.layer('individual_drips','Small selected recorded water contacts remain as the rainfall thins.')
  for at,start,level in [(.25,.45,.020),(.43,1.58,.028),(1.12,3.1,.026),(1.54,5.12,.018),(1.82,.45,.013)]:s.event(l,W+'angeloyazar_water_drops.ogg',start,.19,at+d*.007,rate=1.25*detune,level=level,lo=420,hi=5500,attack=.004,release=.045)
 return s.finalize()

def export(x,rel):
 path=ROOT/rel;path.parent.mkdir(parents=True,exist_ok=True)
 assert np.max(np.abs(x))<1,'No clipping during PCM export'
 master=ROOT/'float-masters'/rel;master.parent.mkdir(parents=True,exist_ok=True);wavfile.write(master,SR,x.astype(np.float32))
 val=np.rint(x*8388608).astype(np.int32);raw=np.stack([val&255,(val>>8)&255,(val>>16)&255],axis=1).astype(np.uint8)
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(3);w.setframerate(SR);w.writeframes(raw.tobytes())

def main():
 ids=['flaming_hands','jet_blast','ground_surge','rime_grip','calm','conjure_rain'];heroes=[]
 dominant=[F+'ignition.flac',W+'thimras_ocean_splash.ogg',F+'spark.wav',I+'ice-shatters/IceShatters/LedasLuzta33.ogg',I+'rubbed-singing-bowl_ryancacophony_public-hq-preview.mp3',W+'samsterbirdies_rain_on_leaves_hq.mp3']
 manifest={'revision':2,'status':'recorded-material audition studies; not imported into game','sampleRate':SR,'spells':[],'audioHashes':{},'reel':'preview/recorded_material_reel.wav'}
 for sid,dom in zip(ids,dominant):
  variants=[make(sid,v) for v in [1,2,3]]
  peak=max(float(np.max(np.abs(resample_poly(sum(l['data'] for l in s.layers),4,1)))) for s in variants)
  rms=np.mean([np.sqrt(np.mean(sum(l['data'] for l in s.layers)**2)) for s in variants])
  target=.073 if sid=='calm' else .068 if sid=='conjure_rain' else .105
  gain=min(10**(-3.2/20)/peak,target/rms)
  spell={'id':sid,'title':variants[0].title,'duration':variants[0].dur,'referenceContactSeconds':variants[0].contact,'dominantSource':dom,'sharedMixGain':gain,'variants':[]}
  for s in variants:
   row={'variant':s.v,'layers':[],'mix':f'mix/{sid}_{s.v:02}.wav'}
   for l in s.layers:
    x=l.pop('data')*gain;path=f'layers/{sid}_{s.v:02}_{l["name"]}.wav';export(x,path);l['file']=path;l['postLayerMixGain']=gain;row['layers'].append(l);l['_samples']=x
   mix=sum(l['_samples'] for l in row['layers']);export(mix,row['mix'])
   if s.v==1:
    heroes.append(mix);spell['preview']=f'preview/{sid}.wav';export(mix,spell['preview'])
    breakdown=np.concatenate([np.zeros(12000)]+[np.concatenate([l['_samples'],np.zeros(16800)]) for l in row['layers']]+[mix])
    spell['breakdown']=f'preview/{sid}_layers_then_mix.wav';export(breakdown,spell['breakdown'])
   for l in row['layers']:l.pop('_samples')
   spell['variants'].append(row)
  manifest['spells'].append(spell)
 reel=np.concatenate([np.zeros(12000)]+[np.concatenate([x,np.zeros(24000)]) for x in heroes]);export(reel,manifest['reel'])
 for folder in ['layers','mix','preview','float-masters']:
  for p in (ROOT/folder).rglob('*.wav'):manifest['audioHashes'][str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 (ROOT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print('Six source-based material studies mixed;18 variants; layer auditions at actual gain.')
if __name__=='__main__':main()
