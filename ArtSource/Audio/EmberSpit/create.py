"""Original layered Ember synthesis. Python + NumPy/SciPy + FFmpeg; no recordings."""
import hashlib
import json
import subprocess
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt, resample_poly
from scipy.interpolate import PchipInterpolator

ROOT=Path(__file__).resolve().parent
SR=48000
N=52800
RELEASE=10560
CONTACT=14160

def noise(rng,n,low,high):
    x=sosfilt(butter(2,[low,high],btype='bandpass',fs=SR,output='sos'),rng.normal(size=n))
    return x/(np.sqrt(np.mean(x*x))+1e-12)

def envelope(t,attack,decay):
    return (1-np.exp(-t/attack))**2*np.exp(-t/decay)

def finish(x):
    x=sosfilt(butter(2,45,btype='highpass',fs=SR,output='sos'),x)
    x=sosfilt(butter(2,7400,btype='lowpass',fs=SR,output='sos'),x)
    x[:240]*=np.sin(np.linspace(0,np.pi/2,240))**2
    x[-4800:]*=np.linspace(1,0,4800)**3
    x[-480:]*=np.sin(np.linspace(np.pi/2,0,480))**2
    return x

def wood_crackle(rng,n,count,amount,last,coarse=False):
    x=np.zeros(n)
    for _ in range(count):
        start=int(rng.uniform(.005,last)*SR)
        duration=rng.uniform(.018,.065) if coarse else rng.uniform(.004,.022)
        length=min(int(duration*SR),n-start)
        t=np.arange(length)/SR
        low=rng.uniform(180,750) if coarse else rng.uniform(1200,2600)
        high=rng.uniform(1400,4000) if coarse else min(7400,low*3)
        grain=noise(rng,length,low,high)
        env=(1-np.exp(-t/(.001 if coarse else .00025)))*np.exp(-t/(.014 if coarse else .004))
        env*=np.cos(np.linspace(0,np.pi/2,length))**2
        x[start:start+length]+=amount*rng.uniform(.35,1)*grain*env
    return x

def synth(seed):
    rng=np.random.default_rng(seed)
    t=np.arange(N)/SR
    # The same physical swell drives both independent audible layers.
    swell=PchipInterpolator([0,.04,.12,.22,.29,.38,.58,.78,.94,1.1],
                            [0,.07,.22,.73,1,.83,.42,.09,0,0])(t)
    drift=np.interp(t,np.linspace(0,1.1,17),rng.uniform(.88,1.12,17))
    wind=(.29*noise(rng,N,55,220)+.12*noise(rng,N,190,780)+.065*noise(rng,N,700,3100))*swell*drift
    fire=(.095*noise(rng,N,1400,7000)+.025*noise(rng,N,700,1900)
          +wood_crackle(rng,N,165,.18,.89)
          +wood_crackle(rng,N,23,.19,.82,coarse=True))*swell
    wind,fire=finish(wind),finish(fire*1.3)

    # Crushing is a ragged continuum of fractures, not a descending drum tone.
    ti=np.arange(33600)/SR
    crush=(.34*noise(rng,len(ti),50,210)*envelope(ti,.009,.17)
           +.17*noise(rng,len(ti),180,750)*envelope(ti,.005,.19)
           +.055*noise(rng,len(ti),1800,6300)*envelope(ti,.003,.15))
    crush+=wood_crackle(rng,len(ti),42,.3,.31,coarse=True)*np.exp(-ti/.28)
    crush+=wood_crackle(rng,len(ti),72,.14,.56)*envelope(ti,.003,.22)
    impact=finish(crush)
    crush_layer=np.zeros(N);crush_layer[CONTACT:CONTACT+len(impact)]=impact

    # Split AFTER filtering/fading layers; extra export fades would make a seam.
    breath=wind+fire
    transition=np.clip((np.arange(N)-RELEASE)/1920,0,1)
    transition=transition*transition*(3-2*transition)
    charge=(breath*(1-transition))[:14400]
    flight=(breath*transition)[RELEASE:]
    return {'charge':charge,'flight':flight,'impact':impact,
      'composite':wind+fire+crush_layer,'wind':wind,'fire':fire,'crush':crush_layer}

def export(x,path):
    master=ROOT/'float-masters'/path.name
    wavfile.write(master,SR,x.astype(np.float32))
    subprocess.run(['ffmpeg','-v','error','-y','-i',str(master),'-c:a','pcm_s24le',str(path)],check=True)

def main():
    for folder in ['wav','layers','float-masters','preview']:(ROOT/folder).mkdir(exist_ok=True)
    variants=[synth(seed) for seed in [7301,7302,7303]]
    peak=max(np.max(np.abs(resample_poly(x,4,1))) for v in variants for x in v.values())
    gain=10**(-3/20)/peak
    receipt={'revision':3,'sampleRate':SR,'bits':24,'channels':1,'seeds':[7301,7302,7303],
      'sharedGain':float(gain),'releaseSeconds':.22,'exampleContactSeconds':.295,'compositeSeconds':1.1,
      'audioTail':'Longer natural decay; release/contact timing is unchanged.',
      'direction':'Deep wind and burning logs crescendo together; deep crushing wood and coals at contact.',
      'provenance':'Original procedural synthesis; no external samples or AI service.','files':{}}
    def record(path):receipt['files'][str(path.relative_to(ROOT))]=hashlib.sha256(path.read_bytes()).hexdigest()
    for i,v in enumerate(variants,1):
        for kind,x in v.items():
            x*=gain
            folder='layers' if kind in ['wind','fire','crush'] else 'wav'
            path=ROOT/folder/f'ember_spit_{kind}_{i:02d}.wav'
            export(x,path);record(path)
    reel=np.concatenate([np.zeros(12000)]+[np.concatenate([v['composite'],np.zeros(31200)]) for v in variants])
    path=ROOT/'preview'/'ember_spit_layered_v3.wav';export(reel,path);record(path)
    # A separate audition exposes the layers at their real mix gains.
    isolated=np.concatenate([np.zeros(12000)]+[np.concatenate([variants[0][kind],np.zeros(19200)]) for kind in ['wind','fire','crush','composite']])
    path=ROOT/'preview'/'ember_spit_v3_layer_breakdown.wav';export(isolated,path);record(path)
    (ROOT/'manifest.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print(json.dumps(receipt,indent=2))

if __name__=='__main__':main()
