"""Defense cue auditions: original synthesis and documented CC0 recordings.

Rebuild: python3 ArtSource/CombatDefenseAudio/Auditions/create.py
Requires NumPy, SciPy and FFmpeg. Writes only beside this script, never Assets.
"""
import hashlib
import json
import subprocess
import zipfile
from pathlib import Path

import numpy as np
from scipy.signal import butter, resample_poly, sosfilt

ROOT = Path(__file__).resolve().parent
SR = 48000


def band(rng, n, low, high):
    x = sosfilt(butter(2, [low, high], btype="bandpass", fs=SR, output="sos"), rng.normal(size=n))
    return x / max(1e-12, np.sqrt(np.mean(x * x)))


def env(t, attack, decay):
    return (1 - np.exp(-t / attack)) * np.exp(-t / decay)


def plate(rng, t, frequencies, decay, grit=0.012):
    """Damped, inharmonic modes; intentionally not a pitched musical chord."""
    x = np.zeros(len(t))
    for i, freq in enumerate(frequencies):
        f = freq * rng.uniform(.985, 1.015)
        tau = decay * rng.uniform(.8, 1.2) / (1 + i * .2)
        phase = 2 * np.pi * f * (t + grit * .028 * (1 - np.exp(-t / .028)))
        x += (1 / (1 + i * .27)) * np.sin(phase + rng.uniform(-.2, .2)) * env(t, .0004, tau)
    return x / np.sqrt(len(frequencies))


def add_at(target, sound, seconds, gain=1):
    at = int(seconds * SR)
    n = min(len(sound), len(target) - at)
    if n > 0:
        target[at:at+n] += sound[:n] * gain


def parry(index, duration):
    rng = np.random.default_rng(260113 + index)
    t = np.arange(round(SR * duration)) / SR
    x = np.zeros(len(t))
    configs = {
        1: ([1230, 1819, 2673, 3586, 5110, 7043], .19, .027, .65),
        2: ([674, 1018, 1593, 2261, 3147, 4823, 6180], .24, .019, .8),
        3: ([1798, 2617, 3629, 4853, 6671], .105, .019, .58),
        4: ([893, 1406, 2197, 3023, 4559, 6029], .155, .034, .67),
    }
    modes, decay, contact, amount = configs[index]
    # The short, low "ka" contact is followed by the edge's bright "chink".
    x += .32 * band(rng, len(t), 170, 2100) * env(t, .0007, .012)
    x += .22 * plate(rng, t, [391, 691, 1162], .025)
    chink = amount * plate(rng, t, modes, decay)
    chink += .14 * band(rng, len(t), 2800, 10500) * env(t, .0003, .008)
    add_at(x, chink, contact)
    # An irregular rebound adds physical chatter rather than a second loud hit.
    add_at(x, .18 * plate(rng, t, [f * 1.023 for f in modes], decay * .45), contact + .041)
    if index == 2:
        x += .15 * band(rng, len(t), 350, 2800) * env(t, .0005, .036)
    if index == 4:
        rasp = band(rng, len(t), 1900, 6800) * env(t, .002, .045)
        rasp *= .6 + .4 * np.sin(2 * np.pi * 113 * t) ** 2
        add_at(x, rasp, .016, .13)
    return x


def guard(index, duration):
    rng = np.random.default_rng(260213 + index)
    t = np.arange(round(SR * duration)) / SR
    # Pitch/body sits above deep sub-bass so the cue also has a Deck-speaker band.
    body = band(rng, len(t), 105, 520) * env(t, .002, .055)
    grain = band(rng, len(t), 370, 1900) * env(t, .001, .025)
    if index == 1:
        x = .5 * body + .14 * grain
        x += .16 * plate(rng, t, [188, 307, 547, 883], .045)
        x += .035 * band(rng, len(t), 700, 2400) * env(t, .005, .06)
    elif index == 2:
        x = .36 * body + .18 * grain
        cloth = band(rng, len(t), 500, 3200) * env(t, .003, .045)
        cloth *= .65 + .35 * np.sin(2 * np.pi * 71 * t) ** 2
        x += .09 * cloth
        add_at(x, .06 * grain, .032)
    elif index == 3:
        x = .38 * body + .16 * grain
        x += .32 * plate(rng, t, [263, 619, 1123, 1701], .036, grit=.004)
        add_at(x, .11 * band(rng, len(t), 180, 1800) * env(t, .001, .018), .021)
    else:
        x = .36 * body + .12 * grain
        x += .28 * plate(rng, t, [517, 827, 1361, 2053, 3197], .062)
        add_at(x, .055 * plate(rng, t, [873, 1693, 2879], .023), .028)
    return sosfilt(butter(2, 3400 if index == 4 else 2600, fs=SR, output="sos"), x)


def finish(x):
    x = sosfilt(butter(2, 65, btype="highpass", fs=SR, output="sos"), x)
    x = sosfilt(butter(2, 10500, btype="lowpass", fs=SR, output="sos"), x)
    x = np.tanh(x * .8) / .8
    attack, release = 48, min(1440, len(x) // 5)
    x[:attack] *= np.linspace(0, 1, attack)
    x[-release:] *= np.linspace(1, 0, release) ** 2
    x = np.concatenate([np.zeros(384), x, np.zeros(480)])
    peak = np.max(np.abs(resample_poly(x, 4, 1)))
    return x * (10 ** (-6 / 20) / peak)


def recording(filename, duration, member=None):
    path = ROOT / "sources" / filename
    if member:
        with zipfile.ZipFile(path) as archive:
            raw = archive.read(member)
    else:
        raw = path.read_bytes()
    decoded = subprocess.run(["ffmpeg", "-v", "error", "-i", "pipe:0", "-ar", str(SR), "-ac", "1",
                              "-f", "f32le", "pipe:1"], input=raw, capture_output=True, check=True).stdout
    x = np.frombuffer(decoded, dtype="<f4").astype(np.float64)
    x /= max(1e-12, np.max(np.abs(x)))
    start = max(0, int(np.flatnonzero(np.abs(x) > .015)[0]) - 48)
    n = round(SR * duration)
    result = np.zeros(n)
    take = x[start:start+n]
    result[:len(take)] = take
    return result


def write_pair(name, samples):
    wav = ROOT / f"{name}.wav"
    mp3 = ROOT / f"{name}.mp3"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "1",
                    "-i", "pipe:0", "-c:a", "pcm_s24le", str(wav)], input=samples.astype("<f4").tobytes(), check=True)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-c:a", "libmp3lame", "-b:a", "160k", str(mp3)], check=True)
    return wav, mp3


def metrics(samples):
    active = samples[np.abs(samples) > np.max(np.abs(samples)) * .01]
    spectrum = np.abs(np.fft.rfft(samples)) ** 2
    hz = np.fft.rfftfreq(len(samples), 1 / SR)
    return {
        "durationSeconds": round(len(samples) / SR, 4),
        "samplePeakDbFS": round(20 * np.log10(np.max(np.abs(samples)) + 1e-15), 3),
        "truePeak4xDbFS": round(20 * np.log10(np.max(np.abs(resample_poly(samples, 4, 1))) + 1e-15), 3),
        "activeRmsDbFS": round(20 * np.log10(np.sqrt(np.mean(active * active)) + 1e-15), 3),
        "energyCentroidHz": round(float(np.sum(hz * spectrum) / np.sum(spectrum)), 1),
        "energyAbove3kHzPercent": round(100 * float(np.sum(spectrum[hz > 3000]) / np.sum(spectrum)), 3),
        "dcOffset": round(float(np.mean(samples)), 8),
    }


def main():
    specifications = [
        ("parry_01_clean_kachink", "Clean steel kachink", "Dry contact followed by a crisp, narrow steel ring.", .64),
        ("parry_02_broad_clash", "Broad blade clash", "Heavier initial contact and a lower, broader metallic ring.", .78),
        ("parry_03_dry_edge", "Recorded dry edge", "Actual machete/spatula contact with a bright modal layer and softened high frequencies; shortest ringing tail.", .44),
        ("parry_04_rasped_catch", "Recorded ringing catch", "A second actual metal contact blended with a textured scrape and restrained steel ring.", .60),
        ("guard_01_padded_catch", "Padded catch", "Compact low thud with a faint, immediately damped metallic core.", .40),
        ("guard_02_leather_wrap", "Wrapped guard", "A recorded cardboard impact filtered into a muffled catch, with a small friction texture.", .46),
        ("guard_03_wood_brace", "Wooden brace", "A dull wooden knock with body and a brief secondary rattle.", .48),
        ("guard_04_damped_steel", "Damped steel", "Recorded soft contact under a restrained low metal knock; the ring is caught and quickly dies.", .56),
    ]
    manifest = {
        "revision": 3, "sampleRate": SR, "channels": 1, "wavBits": 24,
        "status": "Auditions only; not imported into Unity or bound to gameplay.",
        "origin": "Four wholly original deterministic procedural cues and four mixes with documented CC0 recordings. No Qud assets, AI audio service or external compositions.",
        "license": "Original synthesis layers plus CC0-1.0 recordings where individually declared below. Source credits retained voluntarily; see SOURCES.md. No exclusive-rights claim for procedural output.",
        "tooling": "Python, NumPy, SciPy modal resonators and filtered noise; FFmpeg WAV/MP3 encoding.",
        "synthesisSeeds": {"parry": [260114,260115,260116,260117], "guard": [260214,260215,260216,260217]},
        "auditionLevel": "Each cue has a -6 dBFS 4x-estimated true peak; no limiter normalization in playback. Guards are intentionally darker, not pitched versions of parries.",
        "listeningBound": "Signal/decoding checks are automated. User audition and physical Steam Deck listening remain unverified.",
        "clips": [], "reels": [],
    }
    downloads = json.loads((ROOT / "sources" / "downloads.json").read_text())
    for source in downloads:
        assert hashlib.sha256((ROOT / "sources" / source["file"]).read_bytes()).hexdigest() == source["sha256"], source["file"]
    manifest["downloads"] = downloads
    groups = {"parry": [], "guard": []}
    for name, title, intent, duration in specifications:
        kind, number = name.split("_")[:2]
        x = (parry if kind == "parry" else guard)(int(number), duration)
        source = "Original procedural synthesis only."
        if kind == "parry" and int(number) in (3, 4):
            take = 2 if int(number) == 3 else 1
            cutoff = 5500 if int(number) == 3 else 3800
            raw = recording(f"machete_vs_spatula_{take}.wav", duration)
            raw = sosfilt(butter(2, cutoff, fs=SR, output="sos"), raw)
            x = .55 * raw + .4 * x
            source = f"CYBERDEViL, Machete vs. Spatula, take {take}, CC0-1.0; cropped, mono48kHz, {cutoff}Hz lowpass, blended with original modal synthesis."
        elif kind == "guard" and int(number) in (2, 4):
            take = 3 if int(number) == 2 else 6
            raw = recording("impact_-_starninjas.zip", duration, f"impact.{take}.ogg")
            raw = sosfilt(butter(2, 2300, fs=SR, output="sos"), raw)
            x = .7 * raw + .25 * x
            source = f"StarNinjas, 10 Impact/Shield Blocks, impact.{take}.ogg, CC0-1.0; silence trimmed, mono48kHz, 2300Hz lowpass, original synthesis blend."
        x = finish(x)
        wav, mp3 = write_pair(name, x)
        row = {"id": name, "title": title, "intent": intent, "source": source, "wav": wav.name, "mp3": mp3.name, **metrics(x)}
        row["sha256"] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (wav, mp3)}
        manifest["clips"].append(row)
        groups[kind].append((name, x))
    for kind, rows in groups.items():
        pieces = [np.zeros(round(.3 * SR))]
        positions = []
        at = .3
        for i, (name, x) in enumerate(rows):
            positions.append({"option": i+1, "id": name, "startsAtSeconds": round(at, 4)})
            pieces.append(x); at += len(x) / SR
            if i < len(rows)-1:
                pieces.append(np.zeros(SR)); at += 1
        pieces.append(np.zeros(round(.3 * SR)))
        reel = np.concatenate(pieces)
        wav, mp3 = write_pair(f"{kind}_options_01_to_04", reel)
        manifest["reels"].append({"kind": kind, "wav": wav.name, "mp3": mp3.name,
                                  "gapSeconds": 1, "order": positions, **metrics(reel)})
    (ROOT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps({"clips": len(manifest["clips"]), "reels": len(manifest["reels"]),
                      "peakDbFS": max(c["truePeak4xDbFS"] for c in manifest["clips"])}, indent=2))


if __name__ == "__main__":
    main()
