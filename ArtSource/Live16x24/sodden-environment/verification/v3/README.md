# Sodden environment native source — v3 trial

Original editable 16×24 materials: a 576×24 atlas of 36 opaque cells. Frames 0–3 are quiet firm fen ground; 4+mask are filled MirePool; 20+mask are depleted, still-wet MirePool film. Connected cardinal bits are N=1, E=2, S=4, W=8. A missing bit places a restrained shoreline inside that existing cell. Connected edges share exact wet-base pixels, including full/depleted joins. Ground frames share the same base and boundary color. Centered pivot, 16 PPU, Point, Clamp, no mipmaps or compression are the intended import settings; runtime publication is performed separately by root.

The concept and actual before images guided material separation and low groundcover. No generated pixels were cropped, traced or rescaled into the asset. Original named pixel parts are emitted as integer unit quads with flat emission materials in editable.blend. The small builder reuses the established camera/model/instance helpers; Pillow only reads rendered exports for checks. No approved art, Assets resource, runtime, Unity scene or gameplay state was changed by this source-art task.

## Parts and critique

Firm ground has unequal compact off-center moss/fray clusters with restrained granular edge pixels, two sparse flattened straw fragments, and a quieter but still textured variant. These are low surface marks, not new plants or objects. Filled pools use lifted desaturated green-gray wet peat with very low-contrast broken horizontal film. Connected interiors retain this subtle level texture without a bright dash grid. Depleted pools keep the same wet base, with a subdued broad dark veil and limited olive film; they never become safe grass or cracked dry soil. The shore is narrow and broken rather than a continuous bright outline.

Root rejected v1's evenly spaced oval ground stamps and repeated pairs of pool ticks. The exact first candidate, builder and receipts remain under verification/v1. V2 removed interior ticks and replaced centered ground ovals with flatter asymmetric trails; root approved an actual trial. Actual sodden-environment-after-01 then revealed nearly black hole-like pools and parallel diagonal hatching where the runtime repeats a variant across coarse patches. V2 is retained under verification/v2, with its separate first-rebuild files still under verification/rebuild-first.

V3 addresses those named actual defects only: the wet base rises from 1C241B to 303B37, horizontal film is five RGB levels above it, and the firm ground's common diagonal strokes become compact unequal moss/fray clusters. The ground base and all ground borders remain 3D492E; masks, frame allocation, opacity and topology are unchanged. Actual v3 acceptance is pending. Remaining actual-view gates include coarse-patch repetition, depleted-film tiling, current biome grading, hazard salience, low groundcover richness, actor/reed contrast and whole-scene sparseness. No density/count check proves polish or fullness.

contact-native.png/contact.png show all cells; scene-pattern-native.png/scene-pattern.png are synthetic joined material studies generated from geometry. They are not gameplay captures, actual topology, or altered before images. The mixed-stock study deliberately shows full/depleted joins. There are no fog, HUD, geometry, collision or gameplay changes in these studies.

## Verified source bounds

The genuine pre-generation missing-output failure is retained as source-red.json. Current source-verification.json proves all 13,824 native RGBA pixels match source-cells.json, all 36 frames are opaque, 64 connected edges have the common wet base, and 1,127,424 cell pixels in native/enlarged contacts and pattern match their source cells. Four independent pixel corruptions were rejected (wrong ground RGB, transparent pool, invented connected shore, wrong depleted film RGB). Ground base coverage is at least 280/384 pixels per cell.

Both fresh v3 builds reopened from saved Blender files and matched all 13,824 unit quads, 320 named mesh groups, integer coordinates, exact parts and emission palette. Native atlas, contacts, synthetic pattern, source cells and palette are byte-identical across the two builds; verification/rebuild-identity.json records this. Saved .blend hashes differ, so binary .blend reproducibility is explicitly not claimed. Both v3 geometry proofs are retained, with the first under verification/v3-rebuild-first. Sixteen relevant approved PNG/concept/before/helper pins remain unchanged; the guard excludes .meta files and unrelated foreign work. Root's broader worktree-preservation audit is separate.

## Reproduce

Run from the repository root with the existing Python Pillow dependency and Blender:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_sodden_environment.py -- --build
python3 Tools/Live16x24/build_sodden_environment.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/sodden-environment/editable.blend --threads 4 --python Tools/Live16x24/build_sodden_environment.py -- --inspect-blend
```

Do not repeat --snapshot: it intentionally refuses to replace the existing protected-input map. frozen-handoff.json binds the current packet. This source author's checks are self-review; root and a separate reviewer own independent source/actual judgments. Native runtime tests, actual gameplay, persistence and package acceptance remain separate evidence.
