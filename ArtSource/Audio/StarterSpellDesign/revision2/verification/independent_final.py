"""Independent artifact audit; never imports the authoring mixer or writes audio."""
from pathlib import Path
import argparse
import ast
import hashlib
import json
import subprocess
import wave
from functools import lru_cache

import numpy as np
from scipy.signal import butter, sosfilt, resample_poly, welch, find_peaks
from scipy.io import wavfile

ROOT = Path(__file__).resolve().parents[1]
SR = 48000
LSB = 1 / 8388608


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pcm24(path):
    with wave.open(str(path), "rb") as stream:
        header = dict(channels=stream.getnchannels(), sample_rate=stream.getframerate(),
                      sample_width=stream.getsampwidth(), frames=stream.getnframes(),
                      compression=stream.getcomptype())
        assert (header["channels"], header["sample_rate"], header["sample_width"],
                header["compression"]) == (1, SR, 3, "NONE"), path
        octets = np.frombuffer(stream.readframes(header["frames"]), np.uint8)
    words = octets.reshape(-1, 3).astype(np.int32)
    value = words[:, 0] | words[:, 1] << 8 | words[:, 2] << 16
    value = np.where(value & 0x800000, value - 0x1000000, value)
    return value.astype(np.float64) * LSB, header


@lru_cache(None)
def source(name):
    raw = subprocess.check_output([
        "ffmpeg", "-v", "error", "-i", str(ROOT / name), "-ac", "1",
        "-ar", str(SR), "-f", "f32le", "pipe:1"])
    x = np.frombuffer(raw, dtype=np.float32).astype(np.float64)
    assert len(x) and np.isfinite(x).all() and np.max(np.abs(x)) > 0, name
    return x


def edge_fade(x, attack, release):
    y = x.copy()
    a, b = min(len(y) // 2, round(attack * SR)), min(len(y) // 2, round(release * SR))
    if a:
        y[:a] *= np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    if b:
        y[-b:] *= np.sin(np.linspace(np.pi / 2, 0, b)) ** 2
    return y


def rebuild_layer(layer, count):
    result = np.zeros(count)
    gain_errors = []
    for event in layer["events"]:
        original = source(event["source"])
        begin = round(event["sourceStartSeconds"] * SR)
        end = min(len(original), round((event["sourceStartSeconds"] + event["sourceLengthSeconds"]) * SR))
        assert 0 <= begin < end <= len(original)
        x = original[begin:end].copy()
        if event["reverse"]:
            x = x[::-1]
        if event["rate"] != 1:
            x = resample_poly(x, 1000, round(event["rate"] * 1000))
        x = sosfilt(butter(2, event["bandHz"], btype="bandpass", fs=SR, output="sos"), x)
        measured_gain = event["referenceRms"] / max(float(np.sqrt(np.mean(x * x))), 1e-12)
        gain_errors.append(abs(measured_gain - event["sourceNormalizationGain"]))
        x *= event["sourceNormalizationGain"]
        ceiling = event["softPeakCeiling"]
        if ceiling:
            x = ceiling * np.tanh(x / ceiling)
        x = edge_fade(x, event["attackSeconds"], event["releaseSeconds"])
        if event["curve"] is not None:
            curve = np.asarray(event["curve"])
            x *= np.interp(np.arange(len(x)) / SR, curve[:, 0], curve[:, 1])
        offset = round(event["placementSeconds"] * SR)
        remaining = min(len(x), count - offset)
        assert remaining > 0
        result[offset:offset + remaining] += x[:remaining]
    result = edge_fade(sosfilt(butter(2, 38, btype="highpass", fs=SR, output="sos"), result), .005, .08)
    return result * layer["postLayerMixGain"], max(gain_errors)


def db(x):
    return float(20 * np.log10(max(float(x), 1e-12)))


def metrics(y):
    frequencies, powers = welch(y, SR, nperseg=min(4096, len(y)))
    total = np.sum(powers)
    bands = [(0, 200), (200, 800), (800, 2500), (2500, 8000), (8000, 24001)]
    blocks = np.asarray([np.sqrt(np.mean(y[i:i + 240] ** 2)) for i in range(0, len(y), 240)])
    attacks, _ = find_peaks(blocks, height=.25 * max(blocks), distance=5)
    return dict(
        rms_dbfs=db(np.sqrt(np.mean(y * y))), sample_peak_dbfs=db(np.max(np.abs(y))),
        oversampled4_peak_dbfs=db(np.max(np.abs(resample_poly(y, 4, 1)))),
        dc=float(np.mean(y)), first_sample=float(y[0]), last_sample=float(y[-1]),
        first5ms_rms_dbfs=db(np.sqrt(np.mean(y[:240] ** 2))),
        last5ms_rms_dbfs=db(np.sqrt(np.mean(y[-240:] ** 2))),
        spectral_centroid_hz=float(np.sum(frequencies * powers) / total),
        strongest_spectral_bin_hz=float(frequencies[np.argmax(powers)]),
        spectral_band_energy_fractions={f"{a}-{b}Hz": float(np.sum(powers[(frequencies >= a) & (frequencies < b)]) / total) for a, b in bands},
        energy_attack_count_25percent_peak_with25ms_separation=int(len(attacks)))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest-sha", required=True)
    parser.add_argument("--output", default="independent-final.json")
    args = parser.parse_args()
    manifest_path = ROOT / "manifest.json"
    assert sha(manifest_path) == args.manifest_sha, "Frozen manifest changed before audit"
    manifest = json.loads(manifest_path.read_text())
    ledgers = sorted((ROOT / "sources").rglob("source-ledger.json"))
    source_index = {}
    for ledger in ledgers:
        data = json.loads(ledger.read_text())
        for item in data if isinstance(data, list) else data["sources"]:
            path = Path(item["local_path"])
            assert sha(path) == item["sha256"], path
            source_index[str(path.relative_to(ROOT))] = item

    audio = {}
    headers = {}
    master_checks = []
    for name, expected in manifest["audioHashes"].items():
        path = ROOT / name
        assert sha(path) == expected, path
        if not name.startswith("float-masters/"):
            audio[name], headers[name] = pcm24(path)
            assert np.isfinite(audio[name]).all()
    for name in audio:
        rate, samples = wavfile.read(ROOT / "float-masters" / name)
        assert rate == SR and samples.ndim == 1 and samples.dtype == np.float32
        assert np.isfinite(samples).all()
        error = float(np.max(np.abs(samples.astype(np.float64) - audio[name])))
        assert error <= LSB
        master_checks.append(dict(file=name, float32_master_to_pcm24_max_error=error))
    mixes, layers, operations, preview_errors = [], [], [], []
    heroes = []
    used_sources = {}
    for spell in manifest["spells"]:
        count = round(spell["duration"] * SR)
        ids = set()
        for variant in spell["variants"]:
            mix = audio[variant["mix"]]
            assert len(mix) == count
            decoded_layers = []
            for layer in variant["layers"]:
                y = audio[layer["file"]]
                assert len(y) == count
                rebuilt, gain_error = rebuild_layer(layer, count)
                reconstruction_error = float(np.max(np.abs(y - rebuilt)))
                assert reconstruction_error <= .50001 * LSB, (layer["file"], reconstruction_error)
                assert gain_error < 1e-8
                lm = metrics(y)
                assert lm["oversampled4_peak_dbfs"] <= -3.19
                assert abs(lm["dc"]) < 1e-5
                assert lm["first_sample"] == lm["last_sample"] == 0
                assert max(lm["first5ms_rms_dbfs"], lm["last5ms_rms_dbfs"]) < -60
                layers.append(dict(file=layer["file"], source_operation_reconstruction_max_error=reconstruction_error,
                                   source_gain_recalculation_max_error=gain_error, **lm))
                decoded_layers.append(y)
                for event in layer["events"]:
                    item = source_index[event["source"]]
                    assert item["sha256"] == event["sourceSha256"]
                    assert item["license"].startswith("CC0")
                    ids.add(event["source"])
                    used_sources[event["source"]] = event["sourceSha256"]
                    operations.append(dict(spell=spell["id"], variant=variant["variant"], layer=layer["name"],
                                           source=event["source"], source_sha256=event["sourceSha256"]))
            error = float(np.max(np.abs(np.sum(decoded_layers, axis=0) - mix)))
            assert error <= (len(decoded_layers) + 1) * .50001 * LSB
            mm = metrics(mix)
            assert mm["oversampled4_peak_dbfs"] <= -3.19
            assert abs(mm["dc"]) < 1e-5
            assert mm["first_sample"] == mm["last_sample"] == 0
            assert max(mm["first5ms_rms_dbfs"], mm["last5ms_rms_dbfs"]) < -60
            mixes.append(dict(spell=spell["id"], variant=variant["variant"], file=variant["mix"],
                              frames=count, layer_sum_max_error=error, **mm))
            if variant["variant"] == 1:
                assert np.array_equal(audio[spell["preview"]], mix)
                expected_breakdown = np.concatenate([np.zeros(12000)] + [np.concatenate([y, np.zeros(16800)]) for y in decoded_layers] + [mix])
                actual = audio[spell["breakdown"]]
                assert np.array_equal(actual, expected_breakdown), spell["breakdown"]
                preview_errors.append(dict(spell=spell["id"], preview_exact=True, breakdown_exact=True,
                                           breakdown_frames=len(actual), isolated_layer_gain="Identical PCM samples to delivered layer; no solo normalization"))
                heroes.append(mix)
        spell["_source_set"] = sorted(ids)

    expected_reel = np.concatenate([np.zeros(12000)] + [np.concatenate([y, np.zeros(24000)]) for y in heroes])
    assert np.array_equal(audio[manifest["reel"]], expected_reel)
    assert len(mixes) == 18 and len(layers) == 60
    overlap = []
    for i, a in enumerate(manifest["spells"]):
        for j in range(i + 1, len(manifest["spells"])):
            b = manifest["spells"][j]
            common = sorted(set(a["_source_set"]) & set(b["_source_set"]))
            pa, pb = heroes[i], heroes[j]
            length = max(len(pa), len(pb))
            pa, pb = np.pad(pa, (0, length - len(pa))), np.pad(pb, (0, length - len(pb)))
            overlap.append(dict(first=a["id"], second=b["id"], common_recorded_sources=common,
                                aligned_normalized_waveform_correlation=float(np.dot(pa, pb) / np.sqrt(np.dot(pa, pa) * np.dot(pb, pb)))))
    mixer_source = (ROOT / "mix.py").read_text()
    tree = ast.parse(mixer_source)
    call_names = sorted({ast.unparse(n.func) for n in ast.walk(tree) if isinstance(n, ast.Call)})
    suspect = [name for name in call_names if "random" in name or any(t in name for t in ["randn", "uniform", "normal", "sawtooth", "chirp"])]
    assert not suspect
    drop_source_name = "sources/water_rain/angeloyazar_water_drops.ogg"
    drops = source(drop_source_name)
    source_rms = float(np.sqrt(np.mean(drops * drops)))
    drop_activity = []
    for start, classification in [(1.58, "rejected_old_crop"), (3.10, "rejected_old_crop"),
                                  (2.06, "corrected_crop"), (3.66, "corrected_crop")]:
        crop = drops[round(start * SR):round((start + .19) * SR)]
        activity = float(np.sqrt(np.mean(crop * crop)) / source_rms)
        drop_activity.append(dict(source_start_seconds=start, classification=classification,
                                  relative_rms_activity=activity))
        assert (activity < .1) if classification == "rejected_old_crop" else (activity > 2)
    rain = next(s for s in manifest["spells"] if s["id"] == "conjure_rain")
    for variant in rain["variants"]:
        selected = next(l for l in variant["layers"] if l["name"] == "individual_drips")
        assert [e["sourceStartSeconds"] for e in selected["events"]] == [.45, 2.06, 3.66, 5.12, .45]
    assert sha(manifest_path) == args.manifest_sha, "Frozen manifest changed during audit"
    report = dict(
        status="PASS", audit="Independent source-operation and delivered-PCM verification; no importing mix.py",
        manifest_sha256=args.manifest_sha, mixer_sha256=sha(ROOT / "mix.py"), verifier_sha256=sha(Path(__file__)),
        source_ledger_sha256={str(p.relative_to(ROOT)): sha(p) for p in ledgers},
        used_source_sha256=used_sources,
        counts=dict(final_mixes=len(mixes), layers=len(layers), source_operations=len(operations), unique_used_sources=len(used_sources),
                    all_manifest_audio_hashes_verified=len(manifest["audioHashes"]), pcm24_outputs=len(audio)),
        format="All non-float-master output WAVs: mono, 48000Hz, PCM24",
        reconstruction_tolerance="Source operations to each PCM layer <= 0.5 PCM24 LSB; sum of N independently quantized layers <= (N+1)*0.5 LSB",
        review_reel=dict(file=manifest["reel"], frames=len(expected_reel), exact_pcm_sequence=True),
        mix_metrics=mixes, layer_metrics=layers, previews=preview_errors, source_operations=operations,
        float_master_checks=master_checks,
        identity_comparisons=overlap, mixer_calls=call_names, synthesized_source_call_findings=suspect,
        no_recording_shared_between_different_spells=all(not row["common_recorded_sources"] for row in overlap),
        corrected_independent_finding=dict(
            severity="NOTABLE, fixed before final audit", status="RESOLVED",
            original_manifest_sha256="7779ea9a6a1ba601181bc05121a32113d70758ba72b8c27bca086aaecc662851",
            description="Two Rain drip crops selected near-background and were boosted roughly266x/283x instead of selecting actual recorded drops. Each occurred in all three variants.",
            correction="Original source starts1.58/3.10 replaced by2.06/3.66; timeline placement and intended layer gain remain authored independently.",
            independent_remeasurement=drop_activity,
            root_regression_receipt_sha256={name: sha(ROOT / "verification" / name) for name in ["drip-crops-red.json", "drip-crops-green.json"]}),
        semantic_review=dict(
            flaming_hands="Match flare and burning-fire recordings match catching flame, combustion body and dry afterburn.",
            jet_blast="Distinct liquid bubbles, wave and splash recordings match gathered water, coherent mass and contact/runoff; ocean recording remains an explicitly documented pressure-water proxy.",
            ground_surge="Real Tesla recordings retain electrical identity, with stone/gravel physical-ground Foley; no collapse result is asserted.",
            rime_grip="Wooden strain is an explicitly named pressure proxy; distinct ice-labelled fractures close and settle. Recorded provenance does not claim raw ice formation.",
            calm="Bowl body, related bowl resonance and real exhale fit nonviolent release. No bamboo or generic wind is present in final mix operations.",
            conjure_rain="Actual foliage rain dominates; isolated water-drop records provide small wet contacts. The two weak source windows identified in review have been corrected to active drops."),
        honesty_bounds="Signal identity metrics and disjoint source sets demonstrate technical differences, not perceptual recognizability, enjoyment or user approval. Runtime synchronization and conditional status cues are not implemented or assessed by this asset audit.")
    (ROOT / "verification" / args.output).write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: report[k] for k in ["status", "manifest_sha256", "counts"]}, indent=2))


if __name__ == "__main__":
    main()
