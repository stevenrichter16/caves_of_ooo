"""Independent runtime artifact check; no importer/exporter code is reused."""
import hashlib
import itertools
import json
import math
import wave
from pathlib import Path

import numpy as np
from scipy.signal import resample_poly

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "revision2"
OUT = ROOT / "runtime"
SR = 48000
SCALE = 8388608
APPROVED = "0f32e12303f04e5691b3f801b57760064d68d987d7f6cc51fa5ea6fea7289ab7"


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def read(p):
    with wave.open(str(p), "rb") as h:
        assert (h.getnchannels(), h.getsampwidth(), h.getframerate(), h.getcomptype()) == (1, 3, SR, "NONE")
        frames = h.getnframes()
        raw = h.readframes(frames)
    assert len(raw) == frames * 3
    q = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int64)
    x = q[:, 0] + 256 * q[:, 1] + 65536 * q[:, 2]
    return np.where(x >= SCALE, x - 2 * SCALE, x)


def append_at(destination, x, index):
    destination[index:index + len(x)] += x


def main():
    assert sha(ART / "manifest.json") == APPROVED
    approved = json.loads((ART / "manifest.json").read_text())
    catalog = json.loads((OUT / "catalog.json").read_text())
    assert catalog["sourceManifestSha256"] == APPROVED
    assert catalog["neutralCrossoverSamples"] == 960
    assert catalog["sampleRate"] == SR
    for name, expected in approved["audioHashes"].items():
        assert sha(ART / name) == expected, name
    by_id = {s["id"]: s for s in approved["spells"]}
    clips = {}
    for name, expected in catalog["audioHashes"].items():
        assert sha(OUT / name) == expected, name
        clips[name] = read(OUT / name)
        assert clips[name][0] == clips[name][-1] == 0
        assert np.any(clips[name])
    layer_rows, variant_rows = [], []
    maximum = 0.
    maximum_bound = 0.
    mask_checks = 0
    expected_gates = {
        ("jet_blast", "wet_slap"): "target",
        ("ground_surge", "grit_displacement"): "moved",
        ("rime_grip", "brittle_lock"): "frozen",
        ("rime_grip", "settling_fragments"): "frozen",
        ("calm", "resolved_overtone"): "pacified",
    }
    for spell in catalog["spells"]:
        original_spell = by_id[spell["id"]]
        c = round(spell["referenceContactSeconds"] * SR)
        for variant in spell["variants"]:
            original_variant = next(v for v in original_spell["variants"] if v["variant"] == variant["variant"])
            original_layers = {l["name"]: l for l in original_variant["layers"]}
            n = round(original_spell["duration"] * SR)
            expected_mix = read(ART / original_variant["mix"])
            reconstructed = np.zeros(n, np.int64)
            parts = []
            for layer in variant["layers"]:
                assert layer["gate"] == ("watered" if spell["id"] == "conjure_rain" else expected_gates.get((spell["id"], layer["name"]), "always"))
                original_file = original_layers[layer["name"]]["file"]
                assert sha(ART / original_file) == layer["sourceLayerSha256"]
                original = read(ART / original_file)
                restored = np.zeros(n, np.int64)
                pair = []
                for phase, offset in [("prefix", 0), ("suffix", c)]:
                    info = layer[phase]
                    if info is None:
                        pair.append(None)
                        continue
                    assert info["resourceBase"] + ".wav" == Path(info["file"]).name
                    x = clips[info["file"]]
                    assert len(x) == info["samples"]
                    append_at(restored, x, offset)
                    pair.append(x)
                assert np.array_equal(restored, original)
                if layer["gate"] != "always":
                    assert layer["prefix"] is None
                    assert not np.any(original[:c + 1])
                    assert np.array_equal(pair[1], original[c:])
                reconstructed += restored
                parts.append((layer["name"], layer["gate"], pair))
                layer_rows.append(dict(spell=spell["id"], variant=variant["variant"], layer=layer["name"],
                                       gate=layer["gate"], exactIntegerReconstruction=True,
                                       prefixSamples=0 if pair[0] is None else len(pair[0]),
                                       suffixSamples=0 if pair[1] is None else len(pair[1])))
            reference_error = int(np.max(np.abs(reconstructed - expected_mix)))
            assert reference_error <= 2
            up = []
            # Oversample padded clips separately, then explicitly enumerate all
            # masks. This checks the exporter's analytical mask-extrema formula.
            pad = 32
            for name, gate, pair in parts:
                up.append((name, gate, [None if x is None else resample_poly(np.pad(x / SCALE, (pad, pad)), 4, 1) for x in pair]))
            extent = (max(n + 960, SR + n - c) + 2 * pad) * 4
            fixed_prefix = np.zeros(extent)
            for name, gate, pair in up:
                if pair[0] is not None:
                    assert gate == "always"
                    append_at(fixed_prefix, pair[0], 0)
            worst = dict(peak=0.)
            optional_count = sum(gate != "always" for name, gate, pair in up)
            prefix_peak = float(np.max(np.abs(fixed_prefix)))
            suffix_peak = 0.
            suffix_mask_rows = []
            # Enumerate the separate suffix family at zero offset. The sum of
            # the two family maxima bounds their overlap at any relative delay.
            for mask in itertools.product([False, True], repeat=optional_count):
                suffix = np.zeros(extent)
                cursor = 0
                for name, gate, pair in up:
                    enabled = True
                    if gate != "always":
                        enabled = mask[cursor]
                        cursor += 1
                    if enabled and pair[1] is not None:
                        append_at(suffix, pair[1], 0)
                peak = float(np.max(np.abs(suffix)))
                suffix_peak = max(suffix_peak, peak)
                suffix_mask_rows.append(dict(mask=list(mask), suffixPeak=peak,
                                             allOffsetPeakBound=prefix_peak + peak))
            offset_bound = prefix_peak + suffix_peak
            maximum_bound = max(maximum_bound, offset_bound)
            for step in range(101):
                offset = step * 480 * 4
                fixed = fixed_prefix.copy()
                optional = []
                optional_names = []
                for name, gate, pair in up:
                    if gate == "always":
                        if pair[1] is not None: append_at(fixed, pair[1], offset)
                    else:
                        extra = np.zeros(extent)
                        if pair[1] is not None: append_at(extra, pair[1], offset)
                        optional.append(extra); optional_names.append(name)
                for mask in itertools.product([False, True], repeat=optional_count):
                    y = fixed.copy()
                    for enabled, extra in zip(mask, optional):
                        if enabled: y += extra
                    peak = float(np.max(np.abs(y)))
                    mask_checks += 1
                    if peak > worst["peak"]:
                        worst = dict(peak=peak, actualContactSeconds=step / 100,
                                     enabledGatedLayers=[name for enabled, name in zip(mask, optional_names) if enabled])
                # One direct full-PCM oversampling comparison per ordinary
                # reference timing checks padded-clip linearity independently.
                if step == round(spell["referenceContactSeconds"] * 100):
                    direct = resample_poly(np.pad(reconstructed / SCALE, (pad, pad)), 4, 1)
                    full = fixed.copy()
                    for extra in optional: full += extra
                    assert np.max(np.abs(direct - full[:len(direct)])) < 1e-12
            maximum = max(maximum, worst["peak"])
            variant_rows.append(dict(spell=spell["id"], variant=variant["variant"],
                                     referenceMixErrorPcm24Lsb=reference_error,
                                     prefixPeak=prefix_peak, allOffsetPeakBound=offset_bound,
                                     explicitSuffixSubsetBounds=suffix_mask_rows,
                                     contactShifts=101, masksPerContact=2 ** optional_count, **worst))
    gain = float(catalog["commonGain"])
    assert 0 < gain <= 1
    assert maximum * gain <= 10 ** (-3.2 / 20)
    assert maximum * float(np.float32(gain)) <= 10 ** (-3.2 / 20)
    assert maximum_bound * gain <= .95
    assert maximum_bound * float(np.float32(gain)) <= .95
    assert abs(maximum_bound - catalog["allRetimingPeakBoundBeforeGain"]) < 1e-12
    authored_audit = json.loads((OUT / "verification.json").read_text())
    assert abs(maximum - authored_audit["worstPeakBeforeGain"]) < 1e-12
    assert abs(maximum_bound - authored_audit["allRetimingPeakBoundBeforeGain"]) < 1e-12
    assert sha(ART / "manifest.json") == APPROVED
    report = dict(status="PASS", scope="Independent readback and explicit subset enumeration; no runtime_export imports",
                  manifestSha256=APPROVED, catalogSha256=sha(OUT / "catalog.json"),
                  exporterSha256=sha(ROOT / "runtime_export.py"), verifierSha256=sha(Path(__file__)),
                  sourceAudioHashCount=len(approved["audioHashes"]), runtimeClipHashCount=len(clips),
                  exactReconstructedLayers=len(layer_rows), variants=len(variant_rows),
                  explicitMaskAndContactCombinations=mask_checks, commonGain=gain,
                  worstOversampledPeakBeforeGain=maximum, worstOversampledPeakAfterGain=maximum * gain,
                  worstPeakAfterGainDbfs=20 * math.log10(maximum * gain),
                  allRetimingPeakBoundBeforeGain=maximum_bound,
                  allRetimingPeakBoundAfterGain=maximum_bound * gain,
                  allPhaseEndpointsZero=True, allOutcomePrefixesAbsent=True,
                  directReferenceOversamplingMatchesLinearClipSum=True,
                  layerRows=layer_rows, variantRows=variant_rows,
                  honestyBounds="Offline waveform/sample safety only. Exact reference preservation is before the catalog common gain and within original layer-to-mix rounding. Retimed playback differs intentionally; live DSP timing, listener perception, and outcome wiring remain Unity verification responsibilities.")
    (ROOT / "runtime-verification/03-independent.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: report[k] for k in ["status", "runtimeClipHashCount", "exactReconstructedLayers", "explicitMaskAndContactCombinations", "commonGain", "worstPeakAfterGainDbfs"]}, indent=2))


if __name__ == "__main__":
    main()
