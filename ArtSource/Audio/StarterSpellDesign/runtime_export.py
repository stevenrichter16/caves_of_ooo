"""Lossless contact-retiming export of the approved revision2 PCM24 layers.

Offline only. This never imports the authoring mixer, writes Assets, or edits
approved audio. Prefix + contact-relative suffix reconstruct each original
layer exactly at its reference contact. A catalog-level common gain protects
against the worst retimed overlap without changing relative layer levels.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import wave
from pathlib import Path

import numpy as np
from scipy.signal import resample_poly

ROOT = Path(__file__).resolve().parent
APPROVED_HASH = "0f32e12303f04e5691b3f801b57760064d68d987d7f6cc51fa5ea6fea7289ab7"
SR = 48000
CROSSOVER = 960
PCM_SCALE = 8388608
TARGET_PEAK = 10 ** (-3.2 / 20)
SPELLS = {
    "flaming_hands": ("Pyromancy_FlamingHands", {
        "palms_catch": "always", "combustion_body": "always", "dry_afterburn": "always"}),
    "jet_blast": ("Hydromancy_JetBlast", {
        "liquid_gather": "always", "pressurized_water": "always", "wet_slap": "target", "bubbles_and_runoff": "always"}),
    "ground_surge": ("Galvanism_GroundSurge", {
        "charging_current": "always", "advancing_arcs": "always", "ground_fracture": "always", "grit_displacement": "moved"}),
    "rime_grip": ("Cryomancy_RimeGrip", {
        "inward_ice_stress": "always", "brittle_lock": "frozen", "settling_fragments": "frozen"}),
    "calm": ("Spellcraft_Calm", {
        "warm_bowl_body": "always", "resolved_overtone": "pacified", "human_exhale": "always"}),
    "conjure_rain": ("Hydromancy_ConjureRain", {
        "leaf_rain": "watered", "close_leaf_patter": "watered", "individual_drips": "watered"}),
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def layer_gate(spell: str, layer: str) -> str:
    try:
        return SPELLS[spell][1][layer]
    except KeyError as error:
        raise ValueError(f"Unknown spell/layer: {spell}/{layer}") from error


def read_pcm24(path: Path) -> np.ndarray:
    with wave.open(str(path), "rb") as handle:
        if (handle.getnchannels(), handle.getframerate(), handle.getsampwidth(), handle.getcomptype()) != (1, SR, 3, "NONE"):
            raise ValueError(f"Expected mono48k PCM24: {path}")
        raw = handle.readframes(handle.getnframes())
    b = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int32)
    x = b[:, 0] | b[:, 1] << 8 | b[:, 2] << 16
    return np.where(x & 0x800000, x - 0x1000000, x).astype(np.int32)


def validate_pcm(x: np.ndarray) -> None:
    if x.ndim != 1 or not np.issubdtype(x.dtype, np.integer) or not len(x):
        raise ValueError("Expected nonempty mono integer PCM")
    if np.min(x) < -PCM_SCALE or np.max(x) >= PCM_SCALE:
        raise ValueError("Sample outside signed PCM24 range")


def write_pcm24(path: Path, x: np.ndarray) -> None:
    validate_pcm(x)
    words = x.astype(np.int32)
    b = np.column_stack((words & 255, words >> 8 & 255, words >> 16 & 255)).astype(np.uint8)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(1); handle.setframerate(SR); handle.setsampwidth(3)
        handle.writeframes(b.tobytes())


def split_pcm(x: np.ndarray, contact: int, crossover: int, *, gated: bool) -> tuple[np.ndarray | None, np.ndarray | None]:
    """Return cast-prefix and contact-suffix; null means a wholly silent phase.

    Rounding happens once: suffix is the integer residual, not a separately
    rounded gain multiplication. The neutral crossover runs after contact so
    the suffix never requires a negative event-relative scheduling offset.
    """
    validate_pcm(x)
    if not isinstance(contact, int) or contact < 0 or contact >= len(x):
        raise ValueError("Contact outside source")
    if not isinstance(crossover, int) or crossover < 2:
        raise ValueError("Crossover requires at least two samples")
    if x[0] != 0 or x[-1] != 0:
        raise ValueError("Approved source must have quiet boundaries")
    if gated and np.any(x[:contact + 1]):
        raise ValueError("Outcome layer would emit precontact content or begin with a click")
    if not np.any(x[:contact + 1]):
        suffix = x[contact:].copy()
        return None, suffix if np.any(suffix) else None
    span = min(crossover, len(x) - contact)
    if span < 2:
        raise ValueError("Insufficient source for neutral crossover")
    prefix = x[:contact + span].copy()
    ramp = .5 * (1 + np.cos(np.linspace(0, np.pi, span)))
    prefix[contact:] = np.rint(x[contact:contact + span].astype(np.float64) * ramp).astype(np.int32)
    suffix = x[contact:].copy()
    suffix[:span] -= prefix[contact:]
    return (prefix if np.any(prefix) else None, suffix if np.any(suffix) else None)


def subset_peak(fixed: np.ndarray, optional: list[np.ndarray]) -> tuple[float, int, int]:
    """Exact maximum across all optional whole-layer masks, without 2^N buffers.

    At any sample, the largest possible value includes every positive optional
    contribution; the smallest includes every negative one. A mask attaining
    the largest absolute sample is a valid fixed layer subset. This gives the
    same global maximum as enumerating every subset over the entire waveform.
    """
    upper, lower = fixed.copy(), fixed.copy()
    for x in optional:
        upper += np.maximum(x, 0); lower += np.minimum(x, 0)
    hi, lo = int(np.argmax(upper)), int(np.argmin(lower))
    if upper[hi] >= -lower[lo]:
        return float(upper[hi]), hi, sum(1 << i for i, x in enumerate(optional) if x[hi] > 0)
    return float(-lower[lo]), lo, sum(1 << i for i, x in enumerate(optional) if x[lo] < 0)


def common_safe_gain(peak: float) -> float:
    if not math.isfinite(peak) or peak <= 0:
        raise ValueError("Peak must be positive and finite")
    # Round toward safety; Unity stores volume as float32.
    return min(1., math.floor(TARGET_PEAK / peak * 1e7) / 1e7)


def all_offset_bound(prefix: np.ndarray, suffix: np.ndarray, optional: list[np.ndarray]) -> dict:
    """Triangle-inequality bound independent of relative phase-family offset.

    A fixed mask attaining the suffix maximum exists by subset_peak. Prefixes
    and suffixes can never sum beyond their separate absolute maxima, even if
    an arbitrary hitch changes their relative placement beyond the shift grid.
    """
    prefix_peak = float(np.max(np.abs(prefix)))
    suffix_peak, _, mask = subset_peak(suffix, optional)
    return dict(prefixPeak=prefix_peak, selectedSuffixPeak=suffix_peak,
                suffixMask=mask, peakUpperBound=prefix_peak + suffix_peak)


def runtime_safe_gain(grid_peak: float, offset_bound: float) -> float:
    if not math.isfinite(offset_bound) or offset_bound <= 0:
        raise ValueError("All-offset bound must be positive and finite")
    return min(common_safe_gain(grid_peak), math.floor(.95 / offset_bound * 1e7) / 1e7)


def load_approved_manifest(path: Path) -> dict:
    if sha(path) != APPROVED_HASH:
        raise ValueError("Approved revision2 manifest hash mismatch")
    manifest = json.loads(path.read_text())
    root = path.parent.resolve()
    for relative, expected in manifest["audioHashes"].items():
        candidate = (root / relative).resolve()
        if not candidate.is_relative_to(root) or sha(candidate) != expected:
            raise ValueError(f"Approved audio hash mismatch: {relative}")
    return manifest


def shifted_peak_audit(parts: list[dict], contact: int, original_count: int) -> dict:
    """Four-times oversampled peak over 101 shifts and all gated subsets.

    Padding each clip before resampling retains filter pre/post ringing. Since
    every shift is an integer number of source samples, linearity makes adding
    these shifted oversampled clips identical to oversampling the summed PCM.
    """
    pad = 32
    length = (max(original_count + CROSSOVER, SR + original_count - contact) + 2 * pad) * 4
    prepared = []
    for part in parts:
        row = {"name": part["name"], "gate": part["gate"]}
        for phase in ["prefix", "suffix"]:
            clip = part[phase]
            row[phase] = None if clip is None else resample_poly(np.pad(clip.astype(np.float64) / PCM_SCALE, (pad, pad)), 4, 1)
        prepared.append(row)
    worst = {"peak": 0.}
    peak_by_contact = []
    optional_names = [p["name"] for p in prepared if p["gate"] != "always"]
    prefix_group, suffix_group = np.zeros(length), np.zeros(length)
    optional_suffixes = []
    for part in prepared:
        if part["prefix"] is not None:
            if part["gate"] != "always":
                raise ValueError("Outcome prefixes invalidate shared-prefix bound")
            prefix_group[:len(part["prefix"])] += part["prefix"]
        x = np.zeros(length)
        if part["suffix"] is not None:
            x[:len(part["suffix"])] += part["suffix"]
        if part["gate"] == "always":
            suffix_group += x
        else:
            optional_suffixes.append(x)
    offset_bound = all_offset_bound(prefix_group, suffix_group, optional_suffixes)
    for step in range(101):
        offset = step * 480 * 4
        fixed = np.zeros(length)
        optional = []
        for part in prepared:
            y = np.zeros(length)
            if part["prefix"] is not None:
                y[:len(part["prefix"])] += part["prefix"]
            if part["suffix"] is not None:
                y[offset:offset + len(part["suffix"])] += part["suffix"]
            if part["gate"] == "always":
                fixed += y
            else:
                optional.append(y)
        peak, sample, mask = subset_peak(fixed, optional)
        peak_by_contact.append(peak)
        if peak > worst["peak"]:
            worst = dict(peak=peak, actualContactSeconds=step / 100,
                         peakAtSeconds=sample / (SR * 4) - pad / SR,
                         enabledOptionalLayers=[name for j, name in enumerate(optional_names) if mask & (1 << j)])
    return dict(**worst, contactShiftsChecked=101, optionalSubsetCount=2 ** len(optional_names),
                peakByContact=peak_by_contact, oversampleFactor=4,
                allRetimingBound=offset_bound,
                subsetSemantics="Every gated layer can independently be absent; conservative when multiple layers share one outcome")


def export_catalog(manifest_path: Path, output: Path) -> dict:
    approved_root = manifest_path.parent.resolve()
    output = output.resolve()
    if output.exists():
        raise ValueError("Output exists; preserve prior runtime exports")
    if "Assets" in output.parts or output == approved_root or output.is_relative_to(approved_root) or approved_root.is_relative_to(output):
        raise ValueError("Runtime staging must remain outside Assets and approved revision2")
    manifest = load_approved_manifest(manifest_path)
    catalog = dict(schemaVersion=1, sampleRate=SR, sourceManifestSha256=APPROVED_HASH,
                   neutralCrossoverSamples=CROSSOVER, neutralCrossoverSeconds=CROSSOVER / SR,
                   commonGain=1., spells=[])
    pending = []
    reports = []
    for spell in manifest["spells"]:
        sid = spell["id"]
        contact = round(spell["referenceContactSeconds"] * SR)
        row = dict(spellId=SPELLS[sid][0], id=sid, referenceContactSeconds=spell["referenceContactSeconds"], variants=[])
        for variant in spell["variants"]:
            layers, parts = [], []
            original_mix = read_pcm24(approved_root / variant["mix"])
            reconstructed_mix = np.zeros(len(original_mix), np.int64)
            for layer in variant["layers"]:
                gate = layer_gate(sid, layer["name"])
                original = read_pcm24(approved_root / layer["file"])
                before, after = split_pcm(original, contact, CROSSOVER, gated=gate != "always")
                restored = np.zeros(len(original), np.int64)
                if before is not None: restored[:len(before)] += before
                if after is not None: restored[contact:contact + len(after)] += after
                if not np.array_equal(restored, original):
                    raise ValueError("PCM partition altered approved layer")
                reconstructed_mix += restored
                info = dict(name=layer["name"], gate=gate, sourceLayer=layer["file"],
                            sourceLayerSha256=sha(approved_root / layer["file"]), originalSamples=len(original),
                            prefix=None, suffix=None)
                for phase, label, x in [("prefix", "pre", before), ("suffix", "post", after)]:
                    if x is None: continue
                    base = f"{sid}_{variant['variant']:02}_{layer['name']}_{label}"
                    relative = f"wav/{base}.wav"
                    info[phase] = dict(resourceBase=base, file=relative, samples=len(x))
                    pending.append((relative, x))
                parts.append(dict(name=layer["name"], gate=gate, prefix=before, suffix=after))
                layers.append(info)
            error = int(np.max(np.abs(reconstructed_mix - original_mix)))
            if error > 3:
                raise ValueError("Reconstructed mix exceeds approved independent-layer quantization tolerance")
            audit = shifted_peak_audit(parts, contact, len(original_mix))
            reports.append(dict(spellId=sid, variant=variant["variant"], referenceMixMaxErrorPcm24Lsb=error, **audit))
            row["variants"].append(dict(variant=variant["variant"], layers=layers))
        catalog["spells"].append(row)
    peak = max(r["peak"] for r in reports)
    offset_bound = max(r["allRetimingBound"]["peakUpperBound"] for r in reports)
    catalog["commonGain"] = runtime_safe_gain(peak, offset_bound)
    catalog["gainPolicy"] = "Apply this fixed common gain equally to every prefix and suffix; source PCM partitions themselves are not normalized"
    catalog["worstRetimedPeakBeforeGain"] = peak
    catalog["worstRetimedPeakAfterGain"] = peak * catalog["commonGain"]
    catalog["allRetimingPeakBoundBeforeGain"] = offset_bound
    catalog["allRetimingPeakBoundAfterGain"] = offset_bound * catalog["commonGain"]
    catalog["allRetimingBoundPolicy"] = "For every variant and gated subset: maxabs(sum prefixes) + maxabs(sum selected suffixes), four-times oversampled; applies to arbitrary relative offset"
    output.mkdir(parents=True)
    hashes = {}
    for relative, samples in pending:
        path = output / relative
        write_pcm24(path, samples)
        if not np.array_equal(read_pcm24(path), samples):
            raise ValueError("PCM write/read changed samples")
        hashes[relative] = sha(path)
    catalog["audioHashes"] = hashes
    (output / "catalog.json").write_text(json.dumps(catalog, indent=2) + "\n")
    receipt = dict(status="PASS", exporterSha256=sha(Path(__file__)), sourceManifestSha256=APPROVED_HASH,
                   approvedHashesVerified=len(manifest["audioHashes"]), runtimeClipCount=len(pending),
                   commonGain=catalog["commonGain"], worstPeakBeforeGain=peak,
                   worstPeakAfterGain=peak * catalog["commonGain"], exactLayerReconstruction=True,
                   allRetimingPeakBoundBeforeGain=offset_bound,
                   allRetimingPeakBoundAfterGain=offset_bound * catalog["commonGain"],
                   zeroFirstAndLastSampleForEveryPhase=all(x[0] == x[-1] == 0 for _, x in pending),
                   variants=reports)
    (output / "verification.json").write_text(json.dumps(receipt, indent=2) + "\n")
    if sha(manifest_path) != APPROVED_HASH:
        raise ValueError("Approved manifest changed during export")
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "runtime")
    args = parser.parse_args()
    result = export_catalog(ROOT / "revision2/manifest.json", args.output)
    print(json.dumps({key: result[key] for key in ["status", "runtimeClipCount", "commonGain", "worstPeakBeforeGain", "worstPeakAfterGain"]}, indent=2))


if __name__ == "__main__":
    main()
