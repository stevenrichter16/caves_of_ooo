# Felling-Site component extraction and hidden-surface reconstruction

Status: prompt, planning, asset generation, extraction, viewer and offline implementation complete; final verification record below. Unity execution is outside this phase.

## 1. Result

Execute `FELLING-COMPONENT-EXTRACTION-PROMPT.md` to replace the flat scene study with an actual component package. Preserve source pixels for known artwork, synthesize plausible surfaces beneath changeable objects, and prove that components can disappear and be restored independently. The original movement study remains available.

The result will live under `ArtSource/FellingSite/Components/`, with extraction/validation tools under `ArtTools/`. This is CoO-original art preparation; no Qud-parity claim is involved.

## 2. Pre-implementation verification

| Existing fact or assumption | Verified finding | Implementation decision |
|---|---|---|
| Current preview is already layered | `preview.js` draws the reference plus one actor-removal patch, with clipped foreground samples. It has no exported environment components. | Build a separate component package and viewer; do not relabel clipping as extraction. |
| Source artwork contains hidden ground | It is a single flattened RGB image. Covered pixels do not exist. | Generate backing sources; record them as inference and inspect every removal. |
| Generated prop sheet has alpha | Both saved attempts decode as RGB and have painted checkerboards. | Never use them as extracted source assets. Preserve exact reference pixels using an explicitly authorized extraction path, or keep that capability pending. |
| Every pink structure is a tree or mineable rock | Lore describes petrified sandstone anatomy; the image alone does not establish material interactions. | Monumental cliff/root structure stays fixed; loose-stone mutability is a candidate requiring later gameplay mapping. |
| Zone is 48×32 | Existing `layout.json` preserves an 80×25 zone; art is 48×32 at 32 px/cell with 224px upper overhang. | Reuse the existing transform, source-space anchors and protected six/seventh coordinates. |
| A transparent cutout alone can be removed | A baked copy underneath would remain. | Remove every mutable mask from the base; export backing and source detail layers with explicit ownership. |

Read authority: `Lore/10_Bible.md`, `Lore/11_SecondSpine.md`, `Lore/MYSTERY-LEDGER.md`, `Lore/History/02_Geography.md`; current source/art and prior handoff. Exactly six barren positions, a subtle seventh absence, no invented relief carvings, creature or new ending mechanic.

## 3. Component and backing design

### Granularity

- Readable red/teal/pink plant and fungal clusters: individual candidate harvestable visual components, without drop/species claims.
- Loose stones in the accessible clearing/south approach: individual candidate changeable components.
- Monumental cliff/root geometry: fixed sections for layering/depth and inspection. Hiding them for review is not a gameplay action; unseen rooms behind the cliff are not fabricated.
- Water, waterfall, mist: separate source regions/effect surfaces with terrain responsibilities kept distinct from visual animation.
- Six soil positions and seventh absence: protected authored landmarks, not extractable vegetation or deletable props.
- Tiny indistinct moss, ground grains and dense distant texture: explicitly retained decoration. Inventory must disclose coverage limits rather than claim every painted speck is an entity.

### Pixels and surfaces

1. Build an approved actor-free baseline by compositing the existing small traveler-removal patch onto the exact reference. Record the patch and source hashes.
2. Author source-space regions for all candidate components. Refine object masks using local color/connectedness where useful and hand-reviewed outlines for low-contrast stone. A bounding box is a search region, not automatically a sprite silhouette.
3. Extract original source pixels into RGBA sprites. Preserve thin stems and disconnected leaf tips; alpha is verified numerically and over contrasting backgrounds.
4. Treat contact shadows and conservative context separately from freely movable sprite silhouettes. Export an optional source-detail/decal layer at the original ground position. State removal hides the object and its owned contact detail; moving a sprite must not carry a rectangle of ground with it.
5. Generate an empty-environment backing source with Imagegen at the original canvas/projection. Preserve fixed roots, river course, six positions and path. If broad generation misses an area or invents a feature, use targeted correction and keep only reviewed backing regions.
6. Replace only authored removal regions in the base. Blend backing context at region boundaries where appropriate, and include any changed contextual source pixels in an explicitly owned restoration layer so intact recomposition remains measurable.
7. Partition overlapping repair regions deterministically. Surviving neighbors must not be painted over by removal patches. Prefer a single cleaned base plus independently drawn source layers over applying opaque rectangular repairs on top of surviving objects.
8. Fixed sections may have cutout masks for inspection/depth. Regions behind fixed physical boundaries can remain transparent/diagnostic in inspection; no gameplay ground is promised there.

Hidden-surface generation supplies new pixels; extraction supplies original pixels. The manifest must distinguish them.

## 4. Milestones and outputs

### C0 — inventory and contract

Deliver this plan and prompt; `inventory-proposal.json`; final editable component authoring JSON; protected region definitions; stable schema/state contract. Candidate outlines are visually audited before export. Initial coverage target is all readily readable components in the central/southern scene plus major structural/effect layers, with distant/edge ambiguities enumerated.

### C1 — backing-source generation

Deliver generated full-canvas empty-environment source(s), exact prompts, dimensions/hashes, and visual inspection notes. Reject altered geography or residual objects in required removal regions. Preserve sources non-destructively.

### C2 — extraction/export pipeline

Test first: bounds, finite coordinates, alpha, unique IDs, safe relative paths, polygon validity, protected-region overlap, absent backing, deterministic ordering, state permissions and source fidelity. Implement deterministic mask/extraction/compositing/export after script authorization. Produce `build/` with base PNG, per-object RGBA sprites, optional contact detail, masks, structural/effect layers, manifest and measured reports.

### C3 — component review viewer

Deliver `index.html`, styles and JavaScript loading actual exported files. Show original baseline, reconstructed, and cleaned-base views; select by alpha hit test or object list; hide/restore, remove-all mutable, solo/exploded view, masks, fit/native size and selected sprite/backing detail. Protect static layers from gameplay-state actions. Mark hidden-ground inference and candidate interaction semantics clearly. No simulated combat or harvest rewards.

### C4 — raster and interaction acceptance

- Intact reconstruction is compared to the actor-free baseline; report changed pixel count and maximum channel difference. Fix unintentional drift.
- Each component removal changes its own support and reveals the base, without changing protected landmarks or surviving neighbors.
- All-mutable removal shows coherent ground/water/fixed structure; no duplicate painted objects or large rectangular cutout borders.
- Restore-all returns to the identical intact reconstruction; state input validation rejects unknown IDs and forbidden static removal.
- Check thin/high-contrast plants, adjacent objects, edge clipping, mixed water/shore surfaces and foreground rocks at native size.
- Independent code/schema review, adversarial tests and browser inspection. Record concrete limits separately from passing mathematical invariants.

### C5 — package/handoff

Deliver source/asset provenance, README, test logs, counts/coverage, state and Unity import notes referring to actual files. Update the earlier scene plan to point here. Keep Unity import, runtime integration and gameplay ownership unrun and explicitly remaining.

## 5. Runtime/import contract

The exported manifest will carry schema version, source canvas/hash, 32 PPU transform, stable IDs, classification, mutable flag, source bounds, sprite/contact/mask paths, normalized pivot, source foot anchor, candidate cell footprint, draw order, backing classification and readiness. Actor or object simulation remains authoritative; the browser only changes review state.

Unity can consume the separate component textures or the exported 39-prop RGBA atlas and its explicit rectangles. On removal, hide the object plus owned contact layer and invalidate its complete visual coverage. On movement, leave/rebuild ground contact at the old position and move only the object visual according to the gameplay footprint. Save/load restores stable component state through real entities, not PNG coordinates or browser local storage. Fixed sections require per-cell visibility ownership and depth integration before the game can use them correctly. Current `footprintCells` are single proposed anchor cells, not finished collision or visibility ownership.

## 6. Performance and reproducibility

Precompute masks, proposed anchors, source bounds and deterministic draw order offline. The browser caches decoded images and alpha hit-test masks, redraws on state changes, and avoids per-frame rebuilding. No performance claim about Unity follows from browser timing. Export compact cropped sprites and a convenience atlas of the 39 small props; verify every atlas crop against its source RGBA bytes. Store source and manifest hashes with every build report.

## 7. Acceptance and honesty boundaries

- [x] Working prompt and detailed plan on disk.
- [x] Reviewed component inventory and explicit coverage/decorative exclusions.
- [x] Actual alpha sprites and backing surfaces exported.
- [x] Clean base removes the assigned mutable silhouettes and contact support; native-scale semantic review recorded separately.
- [x] Intact reassembly and isolated/all-removal checks pass.
- [x] Browser review controls exercise the exported component state.
- [x] Tests, adversarial review, visual observations and provenance recorded.
- [ ] Unity runtime integration — outside this offline phase.

Record final counts, artifacts, failed/repaired checks and unresolved visual limits below. Generated inference cannot prove what physically existed behind a painted object. Exact intact reconstruction cannot by itself prove that removal looks convincing.

## 8. Implementation log and completion record

- Prompt and plan created before pipeline implementation. Existing flat-preview limitation and failed-alpha premise explicitly corrected.
- Requested explicit authorization for non-Imagegen pixel-preserving extraction scripts because the image-generation tool requires that permission for another image-editing method. Planning, inventory and Imagegen source work continue independently while it is pending.
- User explicitly authorized image-processing scripts for extraction/export, then authorized any needed tools. Deterministic raster processing is now authorized; Imagegen remains the source of newly inferred hidden surfaces.

### Implemented package

| Milestone | Delivered result |
| --- | --- |
| C0 | 65 inventoried entries; 55 enabled, 10 disabled as false/ambiguous identifications or the painted traveler. Source-scale inspection corrected the initial proposal. |
| C1 | Preserved Imagegen empty-environment source and exact prompt. The generation altered some root silhouettes globally, so only locally masked backing samples are used. Fixed scene geometry stays source-derived. |
| C2 | 55 cropped RGBA layers, 39 separate contact contributions, local masks, a cleaned environment, base partition, deterministic manifest, original/baseline copies, and a 39-prop atlas with top-left/Unity-bottom-left rectangles and pivots. |
| C3 | Actual-file browser compositor with per-layer inspection/removal/restoration, masks, inferred coverage, native size, comparisons and exploded view. |
| C4 | Pixel-ownership, state, malformed-input and adversarial tests; independent asset/hash/atlas/reassembly validator; source-scale cutout/removal review and browser checks. Current machine results are linked below. |
| C5 | Package README, provenance, editable authoring, reproducible tools, measured reports, and game-specific Unity integration contract. |

The 39 candidate props comprise seven loose-stone objects and 32 plant/fungus/rosette groups. The other 16 layers are fixed structural/effect partitions. Tiny background growth and ambiguous pale formations are explicitly retained rather than assigned invented gameplay semantics.

### Corrections made during review

- Rejected two historical RGB prop sheets with painted checkerboards. Actual exported PNGs have real alpha; the earlier strict transparency gate now passes without weakening its check.
- Replaced misplaced full-frame inventory estimates with inspected source coordinates; disabled false boulders and added separately observed groups.
- Separated contact/context pixels from sprite silhouettes and assigned overlapping repair support to one deterministic owner. Source contact detail is hidden with its prop and never moves as part of the sprite.
- Excluded pale fixed stones from red-plant masks and protected them from backing expansion. A dim-branch regression then corrected overly aggressive saturation filtering.
- Traced pale/brown stems missed by cyan-only selection and expanded the northwestern red-plant contour to include its visible terminal sprig. These are visible-source inclusions, not invented rear geometry.
- Expanded cropped exports to include their proposed foot anchors instead of silently clamping pivots. Added current extractor-source hash alongside source-art and authoring hashes to detect stale builds.

### Evidence and remaining boundary

The package guide is [Components/README.md](../ArtSource/FellingSite/Components/README.md). The executable final check is `python3 ArtTools/verify_felling_components.py`; its [aggregate report](../ArtSource/FellingSite/Components/reports/verification-report.json) and raw logs are separate from the producer's [raster measurements](../ArtSource/FellingSite/Components/build/reports/verification.json). [Browser observations](../ArtSource/FellingSite/Components/reports/browser-review.md) record actual UI checks. The earlier scene runner passed strict asset validation, refreshed Prepared output, and passed its 61 tests.

This completes an offline removal/reassembly asset study. It does not complete arbitrary movement, unseen sides of partly occluded objects, animation frames, full visibility/collision ownership, gameplay persistence, or Unity import/render validation. Color-guided edges retain original composited fringe colors; moving a sprite over a contrasting ground may need additional matting. Broad fixed partitions need refined runtime occlusion ownership before actors can interleave correctly. Those are explicit integration and art-extension tasks, not hidden-surface recovery claims.

### Final verification result

`verify_felling_components.py` completed successfully: **100 Python tests + 19 Node tests**, zero skips; **153 strict asset checks**; all five live source/input hashes matched. Independent exported-file reconstruction changed **zero pixels**, all 39 props removed exactly matched the saved cleaned environment, all pivots matched source foot anchors, and all 39 atlas rectangles matched their source RGBA bytes. No watched inputs or outputs changed during the run. The ten corrected removal crops were re-inspected after the final rebuild in `Components/inspection/final-corrected-removals.png`.
