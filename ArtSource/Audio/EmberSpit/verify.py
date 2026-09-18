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


if __name__=='__main__':
    from verify_layers import main
    raise SystemExit(main())
