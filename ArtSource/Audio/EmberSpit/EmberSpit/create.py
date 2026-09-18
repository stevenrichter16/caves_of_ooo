"""Original deterministic Ember sound synthesis. Run with Python + NumPy/SciPy + FFmpeg."""
import hashlib
import json
import subprocess
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt, resample_poly

ROOT=Path(__file__).resolve().parent
SR=48000

def noise(rng,n,low,high):
    x=sosfilt(butter(2,[low,high],btype='bandpass',fs=SR,output='sos'),rng.normal(size=n))
    return x/(np.sqrt(np.mean(x*x))+1e-12)

def envelope(t,attack,decay):
    return (1-np.exp(-t/attack))**2*np.exp(-t/decay)

def finish(x):
    x=sosfilt(butter(2,75,btype='highpass',fs=SR,output='sos'),x)
    x=sosfilt(butter(2,7000,btype='lowpass',fs=SR,output='sos'),x)
    ramp=np.sin(np.linspace(0,np.pi/2,480))**2
    x[:480]*=ramp; x[-2400:]*=np.linspace(1,0,2400)**3
    x[-480:]*=ramp[::-1]
    return x

def grains(rng,n,count,amount,last):
    x=np.zeros(n)
    for _ in range(count):
        start=int(rng.uniform(.006,last)*SR)
        length=min(int(rng.uniform(.004,.012)*SR),n-start)
        t=np.arange(length)/SR
        grain=noise(rng,length,1600,5800)*np.sin(np.linspace(0,np.pi,length))**2
        x[start:start+length]+=amount*rng.uniform(.4,1)*grain*np.exp(-t/.004)
    return x

def synth(seed):
    rng=np.random.default_rng(seed)
    pitch=rng.uniform(.94,1.06)
    t=np.arange(int(.22*SR))/SR
    # A breath gathers into a charcoal flick. No long sustained pitched note.
    charge=.055*noise(rng,len(t),350,2100)*np.sin(np.pi*t/.22)**2
    phase=2*np.pi*np.cumsum((210-90*t/.22)*pitch)/SR
    charge+=.042*np.sin(phase)*envelope(t,.008,.052)
    charge+=grains(rng,len(t),5,.065,.16)

    t=np.arange(int(.4*SR))/SR
    # Continuous turbulent air: moving colour, warm body, fine dense crackle.
    dark=noise(rng,len(t),240,1300)
    bright=noise(rng,len(t),1200,4900)
    colour=np.exp(-t/.105)
    turbulence=1+.16*np.sin(2*np.pi*37*t)+.1*np.sin(2*np.pi*63*t+.7)
    flight=(.22*dark+.24*colour*bright)*envelope(t,.003,.082)*turbulence
    phase=2*np.pi*np.cumsum((500*np.exp(-t/.06)+135)*pitch)/SR
    flight+=.075*np.sin(phase)*envelope(t,.002,.035)
    flight+=grains(rng,len(t),38,.045,.19)*np.exp(-t/.095)

    t=np.arange(int(.36*SR))/SR
    # A small papery hit and charcoal sizzle, deliberately below the flight.
    impact=.065*noise(rng,len(t),700,3300)*envelope(t,.0018,.015)
    impact+=.027*noise(rng,len(t),2100,5700)*envelope(t,.003,.048)
    impact+=.023*np.sin(2*np.pi*(260*t-130*t*t))*envelope(t,.001,.018)
    impact+=grains(rng,len(t),9,.027,.13)*np.exp(-t/.085)
    return {k:finish(v) for k,v in [('charge',charge),('flight',flight),('impact',impact*3)]}

def composite(stems):
    x=np.zeros(38400)
    for kind,start in [('charge',0),('flight',10560),('impact',14160)]:
        y=stems[kind];x[start:start+len(y)]+=y
    return x

def main():
    for folder in ['wav','float-masters','preview']: (ROOT/folder).mkdir(exist_ok=True)
    variants=[synth(seed) for seed in [7301,7302,7303]]
    for v in variants:v['composite']=composite(v)
    peak=max(np.max(np.abs(resample_poly(x,4,1))) for v in variants for x in v.values())
    gain=10**(-4/20)/peak
    receipt={'sampleRate':SR,'bits':24,'channels':1,'seeds':[7301,7302,7303],
        'sharedGain':float(gain),'releaseSeconds':.22,'exampleContactSeconds':.295,
        'provenance':'Original procedural synthesis; no external samples or AI service.', 'files':{}}
    for i,v in enumerate(variants,1):
        for kind,x in v.items():
            x*=gain
            name=f'ember_spit_{kind}_{i:02d}.wav'
            src=ROOT/'float-masters'/name;dst=ROOT/'wav'/name
            wavfile.write(src,SR,x.astype(np.float32))
            subprocess.run(['ffmpeg','-v','error','-y','-i',str(src),'-c:a','pcm_s24le',str(dst)],check=True)
            receipt['files'][str(dst.relative_to(ROOT))]=hashlib.sha256(dst.read_bytes()).hexdigest()
    # Three normal-speed casts, separated by quiet space; no gain change.
    reel=np.concatenate([np.zeros(int(.25*SR))]+[np.concatenate([v['composite'],np.zeros(int(.55*SR))]) for v in variants])
    src=ROOT/'preview'/'ember_spit_three_variations_float.wav'
    dst=ROOT/'preview'/'ember_spit_three_variations.wav'
    wavfile.write(src,SR,reel.astype(np.float32))
    subprocess.run(['ffmpeg','-v','error','-y','-i',str(src),'-c:a','pcm_s24le',str(dst)],check=True)
    receipt['files'][str(dst.relative_to(ROOT))]=hashlib.sha256(dst.read_bytes()).hexdigest()
    (ROOT/'manifest.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print(json.dumps(receipt,indent=2))

if __name__=='__main__':main()
