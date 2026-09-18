"""Technical delivery gate. Audible taste is not inferred from waveform checks."""
import json
import wave
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly

ROOT = Path(__file__).resolve().parent

def inspect(path):
    with wave.open(str(path),'rb') as wav:
        assert wav.getsampwidth()==3 and wav.getnchannels()==1, 'actual PCM24 mono header required'
    sr, data = wavfile.read(path)
    assert sr == 48000 and data.ndim == 1 and data.dtype == np.int32, '48kHz mono24-bit WAV required'
    assert np.all((data & 255) == 0), 'PCM24 in int32 container required'
    x = data.astype(float) / 2147483648
    peak=signal_gate(x)
    return x, dict(samples=len(x), seconds=len(x)/sr, peakDbFS=float(20*np.log10(peak)), rmsDbFS=float(20*np.log10(np.sqrt(np.mean(x*x)))))

def signal_gate(x):
    assert np.isfinite(x).all() and np.max(np.abs(x)) > .015, 'non-silent finite audio required'
    peak = np.max(np.abs(resample_poly(x, 4, 1)))
    assert peak < 10 ** (-1 / 20), 'oversampled peak headroom required'
    assert abs(x.mean()) < .001, 'DC offset'
    assert max(abs(x[0]), abs(x[-1])) < .0001, 'boundary click'
    assert np.sqrt(np.mean(x[-480:] ** 2)) < .002, 'quiet final10ms'
    return peak

def impact_gate(flight, impact):
    assert np.sqrt(np.mean(impact[:4800] ** 2)) < .65 * np.sqrt(np.mean(flight[:4800] ** 2)), 'impact must remain quieter than flight'

def main():
    report = {'passed':False, 'files':{}, 'bounds':'Signal checks only; no listening or Unity integration claim.'}
    try:
        composites=[]
        for variant in range(1,4):
            signals={}
            for kind in ['charge','flight','impact','composite']:
                name=f'ember_spit_{kind}_{variant:02d}.wav'
                x, metrics=inspect(ROOT/'wav'/name)
                signals[kind]=x; report['files'][name]=metrics
            impact_gate(signals['flight'],signals['impact'])
            assert len(signals['composite'])==38400
            composed=np.zeros(38400)
            for kind,start in [('charge',0),('flight',10560),('impact',14160)]:
                x=signals[kind]; composed[start:start+len(x)]+=x
            assert np.max(np.abs(composed-signals['composite'])) < 2e-6, 'composite must retain event timing and stem gains'
            composites.append(signals['composite'])
        assert all(np.corrcoef(composites[i],composites[j])[0,1]<.9 for i,j in [(0,1),(0,2),(1,2)]), 'real variant diversity'
        reel, metrics=inspect(ROOT/'preview'/'ember_spit_three_variations.wav')
        expected=np.concatenate([np.zeros(12000)]+[np.concatenate([x,np.zeros(26400)]) for x in composites])
        assert len(reel)==len(expected) and np.max(np.abs(reel-expected))<2e-6, 'preview must preserve actual speed and gain'
        report['preview']=metrics
        try: impact_gate(signals['flight'],signals['flight']*2)
        except AssertionError: pass
        else: raise AssertionError('loud impact mutation survived')
        for name, mutation, reason in [('silence',np.zeros(38400),'non-silent'),('clipping',np.ones(38400)*.99,'headroom')]:
            try:signal_gate(mutation)
            except AssertionError as e:assert reason in str(e), f'{name} failed for wrong reason'
            else:raise AssertionError(f'{name} mutation survived')
        report['mutationControls']=['silent delivery rejected','clipped delivery rejected','oversized impact rejected']
        report['passed']=True
    except Exception as e:
        report['failure']=str(e)
    print(json.dumps(report,indent=2))
    return 0 if report['passed'] else 1

if __name__=='__main__': raise SystemExit(main())
