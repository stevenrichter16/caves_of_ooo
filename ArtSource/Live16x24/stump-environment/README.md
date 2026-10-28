# Stump environment source — first actual trial candidate

A scoped original revision of 21 native 16×24 cells within the existing 71-frame 1136×24 Stump atlas: floor0–3, dome interiors20–21, exposed dome frames56–70. The other 50 frames/19,200RGBApixels and their source metadata remain exact. The approved baseline atlas, source cells, palette, manifest, editable blend and original builder are preserved under baseline-approved71; the original source folder remains untouched. Native/runtime publication is separately owned by root. This builder only writes this new ArtSource folder.

## Material decisions and bounds

The actual Summit baseline has pink floor and subdued grey dome patches with weak solid/walkable hierarchy. The concept provides warm rose floor versus dark grey-brown stone, but its repeated stair tiers, altered owners/HUD and dense high-resolution mottling are rejected. No generated image pixels are traced, downsampled or otherwise copied into the atlas.

The floor base A59BAF is a restrained +7/+3/−1 RGB change from the approved 9E98B0, chosen to keep Gorge's unchanged water, wall and passable-ledge contrast in range under the unchanged Cave grade. Floor0 is exactly plain; the other three contain unequal shallow soft fossil fields, not tall plants, paths or new objects. Source palette appears cooler than the final scene because the existing grade warms it; this is not a grader change.

The dome is one connected cap. Only disclosed exposed edge frames gain the stronger inside-cell relief: a restrained north rim, narrow side transitions and a five-pixel south near face. Connected interior edges stay quiet, so the mass has no per-cell brick outlines or repeated tiers. There is no projected shadow, transparent recess, changed footprint or new blocking owner. StoneDome gains no material/destruction mechanic. DescentLedge, both pool stocks, tank, vein, ridge, wall and all other art stay untouched.

The first source draft's small pale-center floor/cap marks risked reading as loops. Those inner highlights were removed before the first actual trial; the rejected source preview, cells, builder and receipt remain in verification/rejected-first-loop. The current synthetic joined scene study uses existing exposed N1/E2/S4/W8 semantics (not Sodden wet connection bits) and the existing floor coordinate distribution. It is not gameplay, a reconstruction of the actual map, or evidence of blocker readability. Normal-zoom Summit and Gorge captures must decide floor repetition, cap/near-face contrast, unchanged ledge meaning, and scene fullness. No final polish or region acceptance is claimed.

## Source checks

source-red.json retains the genuine missing-output failure before rendering. source-verification.json checks all 27,264 native RGBA pixels against named source cells, exact preservation of the 50 non-target frames, full opacity of the 21 target frames, quiet ground borders and interior portions of connected dome edges. It also checks 1,179,100 opaque cell pixels across native/enlarged contacts and the joined study. Four corruption controls fail: changing an untouched wall pixel, making a solid dome pixel transparent, stamping quiet floor0, and changing the north dome rim. Twenty relevant baseline/concept/before/helper inputs remain unchanged; no foreign metadata or whole-worktree pins are included.

Two fresh builds produce byte-identical native atlas, contacts, joined study, source cells and palette. Both saved editable blends reopen with 25,750 exact integer unit quads and 300 named mesh groups, exact part/color coordinates and no animation. Different saved .blend hashes are retained honestly; binary .blend identity is not claimed. verification/rebuild-first and verification/rebuild-identity.json retain the first build and proof. The verifier hashes the actual opened Blender filepath.

New target-only palette keys avoid recoloring shared baseline cells. The old builder's build/verify entry points are not used: they publish Assets and enforce the old one-pixel perimeter contract. Existing model/camera/instance helpers author the full editable scene from original/captured source-cell descriptions; no raster editing is used. Pillow only decodes exports for verification. The baseline runtime path is recorded in baseline-provenance.json; the preserved baseline copy stays pinned while root may intentionally replace the runtime atlas.

## Reproduce

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_stump_environment.py -- --build
python3 Tools/Live16x24/build_stump_environment.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/stump-environment/editable.blend --threads 4 --python Tools/Live16x24/build_stump_environment.py -- --inspect-blend
```

Do not repeat --snapshot; it refuses to overwrite the original input map. Intended import remains centered 16 PPU, Point/Clamp/no mipmaps/no compression. frozen-handoff.json binds the candidate. Source-author review is self-review; independent art and actual gameplay review belong to root/another reviewer. No Unity, runtime, Git, new gameplay tests or package execution was performed by this art task.
