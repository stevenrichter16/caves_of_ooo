# Sodden travel — original native static source

Status, 2026-10-27: **28-frame quiet-mire source verified and frozen; root
approved this revised contact for an actual gameplay trial.** Runtime import, native integration
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
reflections. `pass1/` retains the first material pass; `pass2/` retains the
second pass, including the exact accepted22-frame source, generator, verifier,
native/contact renders and handoff. Neither historical packet is an import
candidate.

The actual integrated whole-view capture
[`sodden-after-01-crossing-entry.png`](../../../Docs/Verification/Live16x24/sodden-after-01-crossing-entry.png)
then revealed a critical scene-scale defect: the crossing's433 filled mire
cells, counted in the parent gameplay audit, repeated the same two bright
ochre/glint shapes like a patterned carpet. Source-contact approval had not
established this large-field result. This author inspected that actual capture
before revising only the mire geometry.

The third pass removes both bright mound-like shapes. Filled base16 now has
two subdued reflection pixels and three dark wet pixels; film base17 has
three subdued reflection pixels and eight dark wet pixels. Six appended
variants shift sparse level marks around an otherwise unbroken dark olive
surface. No variant uses the old ochre/glint palette values. All20 accepted
boards, peat and tree frames remain byte-exact in semantic cells and native
pixels. The parent approved the new contact for a final gameplay trial;
**actual scene acceptance remains pending**, including hazard readability and
whether repeated marks are sufficiently quiet across the real crossing.

## Fixed extraction and owner mapping

One atlas: `sodden-travel.png`,448×24,28 cells of16×24. Proposed resource:
`Sprites/Live16x24/sodden-travel`. Exact frame order is stable:

| Frames | Actual owner/state | Meaning and limit |
| --- | --- | --- |
|0–15|Duckboard; N=1,E=2,S=4,W=8 mask|Narrow transverse planks. Join only actual disclosed eligible cardinal neighbors. No rails, new connections or extra walkable cells.|
|16|MirePool, positive stock, base|Nearly unmarked dark hazardous bog-mire with one tiny subdued level reflection; no clean-water promise.|
|17|MirePool, depleted stock but still-wet coating, base|Opaque hazardous wet film, tiny level reflection and a shallow dark smear; no dry/safe bed.|
|18–19|PeatBank variants|Full-width solid cut peat, irregular compressed strata/root fibers and muted moss lips. Neither logs nor rock blocks.|
|20–21|DeadTree variants|Ash-grey tapered trunk, angular bare forks and root flare. No living canopy, fruit or baked flame.|
|22–24|MirePool, positive stock, sparse variants1–3|Same dark wet surface;3–4 subdued reflection pixels and small dark marks at different locations.|
|25–27|MirePool, depleted stock, sparse film variants1–3|Same continuous hazardous wet film;4–5 subdued reflection pixels with sparse dark smears.|

The requested runtime selection is deterministic spatial variation with
weights9:1:1:1:75% base and25% distributed across the three variants. Filled
uses `[16,22,23,24]`; film uses `[17,25,26,27]`. Selection must not consume
simulation RNG, animate, change stock or infer safety. That adapter and its
tests are parent-owned; this source packet only records the contract.

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

The production PNG renders directly at448×24. `contact-native.png` renders
directly at198×318, and `contact.png` directly at6× that geometry. Neither is a
resized raster. The bottom contiguous wet-field study is a source-material
demonstration, not a screenshot or new playable map. Its upper four rows show
filled mire and lower two show wet film, each with exactly75% base and25%
variants. The unchanged joined boardwalk, peat bank and bare trunks remain
beside it against a contact-only dark green backdrop.

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
generation. `build-first.log`/`source-green-first.txt` belong to the first
material pass; `build-final.log`/`source-green-final.txt` to the second pass.
`variation-red.txt` records the new28-frame contract rejecting the old22-frame
output before production. `build-variation.log`, `build-variation-repeat.log`,
`variation-green-first.json`, `variation-green.txt`, `verification.json` and
`variation-reproduction.json` describe the quiet-mire revision:

- 28 distinct frames;448×24; binary alpha; exact native/source pixel agreement.
- All20 non-mire semantic frames and native pixels exactly match the retained
  pass2 output; all13 pass2 snapshot files remain hash-identical.
- All16 Duckboard masks have exactly the allowed footprint and matching
  opposite join profiles. Transverse joints and wood grain remain present.
- All eight mire frames are fully opaque, share exactly the same seamless
  boundary color and retain subdued wet-surface cues. Base16/17 use only5/11
  non-base pixels out of384; no frame contains the old bright ochre/glint
  colors. Peat contains no transparent walkable-looking seam.
- Bare trees retain one-cell geometry, baseline22 and transparent row23.
- Both direct contacts match their source cells exactly.
- All231 frozen pre-existing art/source/reference inputs remain unchanged.
- Sixteen deliberately invalid source variants were rejected: missing frame,
  duplicated state, invented/missing connection, mismatched join, depleted
  alpha hole, false-safe depleted flag, bright mire mark, overly dense film,
  duplicated mire variation, altered accepted board/peat/tree, cutbank hole,
  live leaf on dead wood and unknown palette key.
- A second bounded Blender render reproduces the decoded native RGBA pixels
  and exact source-cell JSON. Both runs independently pass every native and
  contact-placement pixel comparison. The1× contact file is byte-identical;
  the native and6× PNG file hashes differ while Blender embeds render-time
  and memory statistics. `variation-reproduction.json` records that failed
  encoded-byte comparison as well as the successful pixel/source checks.
  It does not claim encoded-PNG or `.blend` byte determinism, or a separate
  full-image decoded repeat comparison for the6× contact's labels/backdrop.

These checks validate source integrity, not simulation semantics or visual
quality by themselves. The earlier two renders were different material passes;
only the latest repeated render is a reproducibility check. No standalone Blender
reopen audit, Unity import/native test, ordinary gameplay, packaged run,
hardware performance or whole-scene acceptance is claimed by this author.
Root will judge repetition, fullness, material boundaries and hazard readability
at actual gameplay scale before accepting integration.

Frozen atlas SHA256:
`45df6770bc5ff407c4dd2d719918855affd5aa7931197222b7e9e65a08839a1b`.
Exact source, Blender, contact, verifier and input hashes are in
[`frozen-handoff.json`](frozen-handoff.json). No further art changes are planned
after this handoff unless actual scene review identifies a named defect.
