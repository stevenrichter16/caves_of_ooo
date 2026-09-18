"""Revision3 delivery checks. Measures signal properties, never claims listening quality."""
import json
import hashlib
from pathlib import Path
import numpy as np
from scipy.signal import welch
from verify import inspect, signal_gate

ROOT=Path(__file__).resolve().parent
SR=48000
N=52800
RELEASE=10560
CONTACT=14160

def rms(x):return float(np.sqrt(np.mean(x*x)))

def band_fraction(x,lo,hi):
    f,p=welch(x,SR,nperseg=2048)
    return float(p[(f>=lo)&(f<=hi)].sum()/max(p.sum(),1e-20))

def depth_gate(wind,fire,crush):
    assert band_fraction(wind,45,450)>.50, 'deep broadband wind required'
    assert band_fraction(fire,1200,7500)>.35, 'audible fine fire texture required'
    assert band_fraction(crush,45,450)>.25, 'deep wood and coal crushing required'
    assert band_fraction(crush,1400,7500)>.025, 'crush must contain splinters as well as depth'

def rise_gate(x):
    ratio=rms(x[10080:13920])/max(rms(x[2880:6720]),1e-12)
    assert ratio>1.65, 'wind and fire must crescendo together before contact'
    return ratio

def main():
    report={'passed':False,'revision':3,'files':{},'variants':[],
      'bounds':'Technical spectral/timing/headroom evidence only; no human listening or Unity integration claim.'}
    try:
        mixes=[]
        for v in range(1,4):
            audio={}
            for key in ['charge','flight','impact','composite','wind','fire','crush']:
                folder='layers' if key in ['wind','fire','crush'] else 'wav'
                path=ROOT/folder/f'ember_spit_{key}_{v:02d}.wav'
                x,m=inspect(path);audio[key]=x;report['files'][str(path.relative_to(ROOT))]=m
            wind,fire,crush=(audio[k] for k in ['wind','fire','crush'])
            if v==1:first=audio
            assert all(len(x)==N for x in [wind,fire,crush,audio['composite']])
            depth_gate(wind,fire,crush)
            wr,fr=rise_gate(wind),rise_gate(fire)
            balance=rms(fire[2400:33600])/rms(wind[2400:33600])
            assert .3<balance<1.8, 'both layers must contribute materially to the actual mix'
            assert np.max(np.abs(crush[:CONTACT]))<2e-7, 'crush cannot pre-empt actual contact'
            assert np.max(np.abs(crush[CONTACT:CONTACT+len(audio['impact'])]-audio['impact']))<2e-6
            event_mix=np.zeros(N)
            for key,start in [('charge',0),('flight',RELEASE),('impact',CONTACT)]:
                x=audio[key];event_mix[start:start+len(x)]+=x
            assert np.max(np.abs(event_mix-audio['composite']))<2e-6, 'actual event-stem reconstruction'
            assert np.max(np.abs(wind+fire+crush-audio['composite']))<2e-6, 'actual design-layer reconstruction'
            assert rms(audio['impact'][4800:12000])>.15*rms(audio['impact'][:4800]), 'ragged crushing must outlast one isolated hit'
            report['variants'].append({'windRise':wr,'fireRise':fr,'fireToWindRms':balance,
              'windLowFraction':band_fraction(wind,45,450),'crushLowFraction':band_fraction(crush,45,450)})
            mixes.append(audio['composite'])
        assert all(np.corrcoef(mixes[i],mixes[j])[0,1]<.9 for i,j in [(0,1),(0,2),(1,2)])
        preview,m=inspect(ROOT/'preview'/'ember_spit_layered_v3.wav')
        expected=np.concatenate([np.zeros(12000)]+[np.concatenate([x,np.zeros(31200)]) for x in mixes])
        assert len(preview)==len(expected) and np.max(np.abs(preview-expected))<2e-6
        report['preview']=m
        breakdown,m=inspect(ROOT/'preview'/'ember_spit_v3_layer_breakdown.wav')
        expected=np.concatenate([np.zeros(12000)]+[np.concatenate([first[k],np.zeros(19200)]) for k in ['wind','fire','crush','composite']])
        assert len(breakdown)==len(expected) and np.max(np.abs(breakdown-expected))<2e-6
        report['layerAudition']=m
        controls=[('wind removed',lambda:depth_gate(wind*0,fire,crush)),
          ('fire removed',lambda:depth_gate(wind,fire*0,crush)),
          ('crush removed',lambda:depth_gate(wind,fire,crush*0)),
          ('crescendo removed',lambda:rise_gate(np.resize(wind[10080:13920],len(wind)))),
          ('silence',lambda:signal_gate(wind*0)),
          ('clipping',lambda:signal_gate(np.ones(N)))]
        report['counterchecks']=[]
        for name,fn in controls:
            try:fn()
            except AssertionError:report['counterchecks'].append(name)
            else:raise AssertionError(name+' countercase passed')
        manifest=json.loads((ROOT/'manifest.json').read_text())
        assert manifest['revision']==3
        assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in manifest['files'].items())
        report['passed']=True
    except Exception as e:report['failure']=str(e)
    print(json.dumps(report,indent=2));return 0 if report['passed'] else 1

if __name__=='__main__':raise SystemExit(main())
