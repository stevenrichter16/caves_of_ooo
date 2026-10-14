"""Decode every audition and check format, finite samples, headroom and identity."""
import json
import subprocess
from pathlib import Path

import numpy as np
from scipy.signal import resample_poly

ROOT = Path(__file__).resolve().parent
manifest = json.loads((ROOT / "manifest.json").read_text())
files = []
for clip in manifest["clips"] + manifest["reels"]:
    for extension in ("wav", "mp3"):
        path = ROOT / clip[extension]
        stream = json.loads(subprocess.check_output([
            "ffprobe", "-v", "error", "-show_streams", "-of", "json", str(path)]))["streams"][0]
        decoded = subprocess.check_output([
            "ffmpeg", "-v", "error", "-i", str(path), "-f", "f32le", "-ac", "1", "-ar", "48000", "pipe:1"])
        samples = np.frombuffer(decoded, dtype="<f4")
        peak = float(np.max(np.abs(resample_poly(samples, 4, 1))))
        assert np.isfinite(samples).all() and 0 < peak < .7, path
        assert int(stream["sample_rate"]) == 48000 and stream["channels"] == 1, path
        assert abs(float(np.mean(samples))) < .001, path
        files.append({"file": path.name, "bytes": path.stat().st_size,
                      "decodedSeconds": round(len(samples)/48000, 4),
                      "truePeak4xDbFS": round(20*np.log10(peak), 3), "finite": True,
                      "sampleRate": 48000, "channels": 1})
assert len({clip["sha256"][clip["wav"]] for clip in manifest["clips"]}) == 8
report = {"passed": True, "revision": manifest["revision"], "verifiedFiles": len(files),
          "allEightClipsDistinct": True, "files": files,
          "bound": "Technical decoding, finite samples, headroom, DC and format checks. Subjective listening and device testing are not claimed."}
(ROOT / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"passed": True, "verifiedFiles": len(files), "revision": manifest["revision"]}))
