"""Read-only reproduction of captured GPU evidence; requires Pillow and numpy.
Writes only this run's independent-evidence.json. It never starts Unity or edits Assets.
"""
from pathlib import Path
import hashlib
import json
import re
import numpy as np
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parent
captures = root / "captures"
report_path = captures / "diagnostic.json"
report = json.loads(report_path.read_text())
launch = json.loads((root / "launch-result.json").read_text())
restore = json.loads((captures / "scene-restore.json").read_text())
checks = []
def check(name, condition):
    checks.append({"name": name, "passed": bool(condition)})

check("actual source scene", report["sourceScene"] == "Assets/Scenes/Main/SampleScene.unity")
check("captured without scoped logs", report["status"] == "CAPTURED" and not report["unexpectedLogs"] and not report["error"])
check("scene and dirty flags preserved", report["activeScenePreserved"] and report["sceneDirtyFlagsPreserved"])
check("original editor scene setup restored", restore["sceneSetupRestored"] is True)
check("launch succeeded", launch["unityExit"] == 0 and launch["compileErrors"] == 0 and launch["shaderErrors"] == 0)
check("launcher reports protected files unchanged", launch["changedProtectedFiles"] == [])
log = (root / "unity.log").read_text()
check("no C# compiler or shader failures in raw log", not re.search(r"error CS\d+|Shader error", log))
conditions = ("native_default_probe", "baseline", "shadow_off", "unlit_palette", "unlit_white", "lit_white_no_shadow", "ambient_only_source_editor_SH", "ambient_only_neutral_SH", "neutral_SH_palette_no_shadow", "neutral_SH_palette_shadow")
expected = {f"{condition}_{height}" for condition in conditions for height in (216, 768)}
check("exact 20 condition/resolution frames", len(report["frames"]) == 20 and {f["name"] for f in report["frames"]} == expected)
frames = {}
for f in report["frames"]:
    # Resolve by file name for relocated repository copies, without trusting absolute paths.
    path = captures / Path(f["png"]).name
    pixels = np.asarray(Image.open(path).convert("RGB"))
    height, width = pixels.shape[:2]
    check(f["name"] + " PNG hash", hashlib.sha256(path.read_bytes()).hexdigest() == f["pngSha256"])
    check(f["name"] + " dimensions/sRGB", width == f["width"] and height == f["height"] and height in (216, 768) and width == height * 4 // 3 and f["targetSRgb"])
    check(f["name"] + " decoded pixel statistics", int((pixels.max(axis=2) < 12).sum()) == f["darkPixels"] and abs(float(pixels.mean()) / 255 - f["meanLuma"]) < 1e-8)
    frames[f["name"]] = pixels

metrics = {}
for height in (216, 768):
    a = {name: frames[f"{name}_{height}"] for name in conditions}
    white = (a["unlit_white"].min(axis=2) >= 250)
    mask = np.asarray(Image.fromarray(white.astype("uint8") * 255).filter(ImageFilter.MinFilter(3))) > 0
    check(f"{height} positive subject-only geometry mask", int(mask.sum()) > 1000 and int(mask.sum()) < mask.size // 3)
    check(f"{height} original renderer probes match explicit source SH", np.array_equal(a["native_default_probe"], a["baseline"]))
    result = {"erodedSubjectPixels": int(mask.sum()), "frames": {}, "pairs": {}}
    for name, pixels in a.items():
        rgb = pixels[mask].astype(float)
        luma = rgb.mean(axis=1)
        result["frames"][name] = {"meanRGB255": float(luma.mean()), "p01RGB255": float(np.percentile(luma, 1)), "p05RGB255": float(np.percentile(luma, 5)), "darkMeanRGBBelow64": int((luma < 64).sum()), "darkMaxRGBBelow12": int((rgb.max(axis=1) < 12).sum())}
    for before, after in (("baseline", "shadow_off"), ("shadow_off", "neutral_SH_palette_no_shadow"), ("neutral_SH_palette_shadow", "neutral_SH_palette_no_shadow"), ("ambient_only_source_editor_SH", "ambient_only_neutral_SH")):
        diff = a[after][mask].astype(int) - a[before][mask].astype(int)
        result["pairs"][before + " -> " + after] = {"meanSignedRGB255": float(diff.mean()), "meanAbsoluteRGB255": float(abs(diff).mean()), "pixelsChangedMoreThan8": int((abs(diff).max(axis=1) > 8).sum())}
    metrics[str(height)] = result

previous = root.parent / "visual-diagnostic-a121220db7e44176a16cbca2bc73fe6c" / "captures"
comparison = None
if previous.exists():
    same = [name for name in sorted(expected) if hashlib.sha256((previous / (name + ".png")).read_bytes()).digest() == hashlib.sha256((captures / (name + ".png")).read_bytes()).digest()]
    comparison = {"GPU01FrameHashesIdentical": len(same), "compared": len(expected)}
receipt = {"status": "PASS" if all(c["passed"] for c in checks) else "FAIL", "scope": "Independent verification of captured GPU02 files and subject-only paired-light observations; not native gameplay or artistic acceptance.", "runId": report["runId"], "diagnosticSha256": hashlib.sha256(report_path.read_bytes()).hexdigest(), "checks": checks, "checkCount": len(checks), "failed": [c["name"] for c in checks if not c["passed"]], "sourceScene": report["sourceScene"], "sceneRestore": restore, "GPU01Comparison": comparison, "subjectMask": "Same unlit-white frame: all RGB >=250; eroded one pixel with 3x3 minimum filter. Floor stays below threshold. Metrics are stored sRGB byte values, not linear radiometric luminance.", "metrics": metrics, "bounds": ["Only two imported subjects at camera66 degrees and two resolutions; no full chunk coverage.", "Preview scene uses captured SampleScene SH. Native-default-probe preserves renderer bindings inside that preview, not a live game frame.", "White diagnostic FogTexture omits intended native gameplay illumination; typical unlit-source-free pilot visible cells use about RGB(0.392,0.372,0.368).", "World camera direct capture omits final2D compositing, movement, and UI.", "f8 native screenshots used camera81 degrees, so their visual difference from this66-degree probe is not a controlled regression comparison.", "Protected file preservation and scene restoration rely on launcher/capture receipts; this verifier independently checks their values and frame hashes, not the historical Unity process.", "Raw log includes a nonfatal startup licensing token message outside the scoped rendering probe; no compiler/shader errors or scoped probe errors were reported.", "Neutral SH is a positive control, not an approved lighting setting. No production lighting change is justified by this evidence alone."]}
(root / "independent-evidence.json").write_text(json.dumps(receipt, indent=2) + "\n")
print(json.dumps({"status": receipt["status"], "checks": len(checks), "failed": receipt["failed"], "GPU01Comparison": comparison}, indent=2))
raise SystemExit(0 if receipt["status"] == "PASS" else 1)
