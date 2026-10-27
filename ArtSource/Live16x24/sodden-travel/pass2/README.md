# Sodden travel — original native static source

Status, 2026-10-27: **source verified and frozen; root approved the revised
contact for an actual gameplay trial.** Runtime import, native integration
tests, ordinary gameplay and scene-fullness acceptance belong to the parent
task. Source/contact approval is not completion of the Sodden biome.

The source author wrote only this packet and
[`build_sodden_travel.py`](../../../Tools/Live16x24/build_sodden_travel.py).
No `Assets`, Unity, Git, gameplay, generation or save changes were made by this
author. The packet adds static identities and essential material states, with
no animation. Existing accepted images remain unchanged.

## Actual scene, concept and correction

The root's actual input captures are retained under
[`Docs/Verification/Live16x24`](../../../Docs/Verification/Live16x24):

- `sodden-before-01-crossing-entry.png`
- `sodden-before-02-raised-turn.png`
- `sodden-before-03-east-board.png`
- `sodden-before-04-shelter-entry.png`

The [concept](../concepts/sodden-travel/concept.png) and
[exact imagegen prompt](../concepts/sodden-travel/prompt.txt) belong to the parent
task and are unchanged. The concept uses the east-board capture as its layout,
the shelter as adjacent-site context and accepted Sill art as a style anchor.
All were viewed before native authoring. Fog is not a land edge and does not
authorize decorative walls or banks.

Accepted concept elements: continuous dark olive/ochre mire, a narrow weathered
wood route, dense cut-peat strata and bare ash-grey dead wood. Rejected: boards
that resemble stone blocks, peat that resembles mossy fence posts, busy grass,
invented route/shore geometry and live tree canopies. This packet is original
integer-cell geometry; no concept or screenshot pixels were traced, sampled,
cropped, resized or edited into production sprites.

The first native contact passed its source contract but exposed material
problems. Both the author and independent reviewer found paving-like board
panels, cactus-like upright forks and peat caps that pinched at repeated joins.
Depleted mire needed a clearer wet-film cue. One bounded revision changed the
boards to subdued warmer wood with long grain/small splits, splayed and tapered
the bare forks, made peat joins continuous and strengthened level wet-film
reflections. Both material passes remain saved; `pass1/` is historical and must
not be imported. Root accepted the revised contact for a gameplay trial.

## Fixed extraction and owner mapping

One atlas: `sodden-travel.png`,352×24,22 cells of16×24. Proposed resource:
`Sprites/Live16x24/sodden-travel`. Exact frame order is stable:

| Frames | Actual owner/state | Meaning and limit |
| --- | --- | --- |
|0–15|Duckboard; N=1,E=2,S=4,W=8 mask|Narrow transverse planks. Join only actual disclosed eligible cardinal neighbors. No rails, new connections or extra walkable cells.|
|16|MirePool, positive stock|Continuous dark hazardous bog-mire with quiet oxidized slicks; no clean-water promise.|
|17|MirePool, depleted stock but still-wet coating|Opaque hazardous wet film with sparse level reflections; no dry/safe bed.|
|18–19|PeatBank variants|Full-width solid cut peat, irregular compressed strata/root fibers and muted moss lips. Neither logs nor rock blocks.|
|20–21|DeadTree variants|Ash-grey tapered trunk, angular bare forks and root flare. No living canopy, fruit or baked flame.|

Frame rectangles are `[index*16,0,16,24]`. Tiles use centered pivot(0.5,0.5),
16PPU, Point filtering, Clamp, no mipmaps or compression. Import metadata is
owned by the parent; this packet contains no new Unity `.meta`.

Source stock and current liquid coating are separate facts. The current
`TileStateSourcePart` can retain/renew bog-mire after pool Volume0. Therefore
both authored pool frames are fully opaque wet surfaces. Frame17 must not
authorize a dry-ground material, erase a hazard, or suppress unrelated charge,
heat, cold, mixtures or other state cues. Runtime must select the correct
owner/state and retain truthful fallback where neutral art is insufficient.

The source creates no Reeds, Grass, StoneFloor or StoneWall replacement.
The parent may borrow their existing approved pixels under its independently
tested regional predicates. The first intended consumers are the real Sodden
district crossing/shelter; this source does not grant global art admission.

## Editable production and reproduction

The generator imports existing `camera_for`, material/label helpers and
`model`/`instance`/`plane` functions. It never calls another generator's main
routine. Each visible native cell becomes a named unit quad grouped by material
and part. `source-cells.json` retains the exact palette and semantic cells;
`sodden-travel.blend` retains editable collections and all three render scenes.

The production PNG renders directly at352×24. `contact-native.png` renders
directly at198×210, and `contact.png` directly at6× that geometry. Neither is a
resized raster. The bottom join study is a source-material demonstration, not
a screenshot or new playable map. It shows board turns, both wet surfaces,
continuous peat and bare trunks against a contact-only dark green backdrop.

From the repository root, reproduce only when explicitly revisiting this
source; the handed-off atlas is currently frozen:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_sodden_travel.py
python3 ArtSource/Live16x24/sodden-travel/verify.py
```

The generator writes only this packet and disables Blender backup versions.
The verifier decodes images for comparison and never authors/edits pixels.
Do not execute the older imported generators' Assets-writing entry points.

## Evidence and limits

`source-red.txt` records a genuine missing-output failure before the first
generation. `build-first.log` and `source-green-first.txt` belong to the first
material pass. `build-final.log`, `source-green-final.txt` and
`verification.json` describe the final revision:

- 22 distinct frames;352×24; binary alpha; exact native/source pixel agreement.
- All16 Duckboard masks have exactly the allowed footprint and matching
  opposite join profiles. Transverse joints and wood grain remain present.
- Both mire states are fully opaque, retain wet-surface cues and differ in
  more than35 native cells. Peat contains no transparent walkable-looking seam.
- Bare trees retain one-cell geometry, baseline22 and transparent row23.
- Both direct contacts match their source cells exactly.
- All231 frozen pre-existing art/source/reference inputs remain unchanged.
- Ten deliberately invalid source variants were rejected: missing frame,
  duplicated state, invented/missing connection, mismatched join, depleted
  alpha hole, false-safe depleted flag, cutbank hole, live leaf on dead wood
  and unknown palette key.

These checks validate source integrity, not simulation semantics or visual
quality by themselves. The two source renders were different material passes;
they are **not** a repeated-build determinism test. No standalone Blender
reopen audit, Unity import/native test, ordinary gameplay, packaged run,
hardware performance or whole-scene acceptance is claimed by this author.
Root will judge repetition, fullness, material boundaries and hazard readability
at actual gameplay scale before accepting integration.

Frozen atlas SHA256:
`5e21a1716e458aac0715d6b32b800dd61eb20a46216d0615a95431036ab6346a`
(2712bytes). Exact source, Blender, contact, verifier and input hashes are in
[`frozen-handoff.json`](frozen-handoff.json). No further art changes are planned
after this handoff unless actual scene review identifies a named defect.
