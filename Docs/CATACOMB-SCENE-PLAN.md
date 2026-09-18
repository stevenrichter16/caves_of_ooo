# Catacomb village — layered scene implementation

Status: implemented and verified on 2026-09-05. User selected parity with the **layered browser Felling scene**. The independent final run passed 140 tests and all eight export/provenance gates. No Unity execution or Assets changes are part of this task. Package entry point: `ArtSource/CatacombVillage/index.html`; handoff: `ArtSource/CatacombVillage/README.md`.

## Goal and source

Build a new playable art study from `Docs/StyleExploration/Felling-2026-09-05/05-catacomb-tileset.png`. Preserve the actual sprite artwork, compose an inhabited catacomb chamber, and deliver movement, independently removable components, complete backing surfaces, reproducible exports and measured verification.

The source is 1536×1024 RGBA. Inspection corrected the initial visual assumption that its gradient was an opaque background: the sheet has usable alpha, generally below 255 even in solid shapes. Extract conservatively bounded subjects using the source alpha; normalize selected pixel opacity and retain original RGB. Titles, separators and neighboring objects are excluded. Atlas-source pixels and deliberately resized placed sprites are separately identified.

## Working production prompt

Act as environment artist and scene implementation engineer. Use the specified catacomb sprite sheet as the actual asset source. Compose a new coherent chamber with a central fungal hearth, perimeter niche dwellings, lantern-beetle tending, luminous pools, food storage and a welcoming threshold. Follow `Lore/10_Bible.md` and `catacomb_village_design.md` material culture. Do not transplant the Felling's six/seventh landmarks, invent a god encounter or imply new canonical gameplay rules. Generate only an empty supporting cavern/floor if needed; never redraw source props and label them extracted sprites. Place every changeable component over already complete supporting art. Export transparent sprites, placement metadata, support masks and a clean all-props-removed image. Build an integrated browser with walking/pathfinding, actual collision changes on prop removal, alpha selection, hide/restore, solo, masks, inferred ground, exploded view and native-size comparison. Reject invalid state changes and restoration onto the walker. Test and inspect actual exports until they meet the Felling browser's functional baseline. Keep Unity and unrelated work untouched.

## Verification sweep

| Question | Observed fact | Decision |
| --- | --- | --- |
| Which Felling? | The repository also contains a shipped Unity W6.5 scene with unrelated 16 px sprites. User explicitly chose the prior layered browser package. | Match offline movement and component mechanics; no Unity parity claim. |
| Are sprites opaque rectangles? | Sheet has native RGBA contours, including internal openings. | Use its alpha rather than infer silhouettes from the displayed background. |
| Must hidden pixels be recovered from a flattened scene? | This scene is assembled from separate sprites. Its floor can exist before props are placed. | Build the complete substrate first; removal simply exposes it. No unnecessary inpainting. |
| Does art define gameplay? | Catacomb culture supports hearths, niches and lanterns; source icons do not specify harvest/destruction rewards. | Removal is a review mechanic; static architecture remains fixed for navigation. |
| Which coordinates? | The Felling browser used a special image-to-zone transform; this is a new standalone chamber. | Explicit 1536×1024 canvas and 48×32 study grid at 32 px/cell. It is not a Unity 80×25 zone. |

## Composition

The central fungal hearth is the brightest focus. Niche modules form the northern dwellings, with stairs and doorways at the sides. A hatchery and pool occupy the east, food and sleeping arrangements the west. A clear southern approach forks around the hearth to the north wall. Residents are small source-derived sprites; dense background and large prop art never silently become the player collision rectangle.

## Milestones

1. **Extract and review source assets.** Save the original and SHA; author precise source bounds; produce real RGBA, a catalog/contact sheet, masks and extraction tests. Preserve source-resolution exports separately from placed resized versions.
2. **Create the supporting environment.** Imagegen supplies an empty matching cavern substrate if needed. Save exact prompt and output; use no generated people, furniture or decorative symbols. Background remains complete beneath components.
3. **Compose and export.** Author stable instances, scale, foot anchors, depth, terrain walkability and component blocker cells. Build baseline, all-mutable-removed underlay, local masks, individual placed sprites and atlas with measured provenance. Known source art is not generatively redrawn.
4. **Build the browser.** Integrated Walk/Inspect modes, keyboard and no-corner-cutting path movement; removal updates collision; restoration into the walker fails atomically. Provide selection/list, hide/restore, remove-all, solo, overlays, exploded, fit/native size and baseline/underlay views.
5. **Verify and iterate.** Test-first extraction/composition/state invariants, adversarial malformed data and state tests, independent asset/reassembly/provenance checks. Inspect native cutouts, whole-scene composition, individual and multiple removals, movement routes and responsive browser controls. Fix material findings before delivery.
6. **Handoff.** README, manifests, source/generation prompts, test logs, visual review and parity matrix. State unsupported Unity behavior explicitly.

## Data contract

`ArtSource/CatacombVillage/build/scene.json` contains schemaVersion/id/title; canvas dimensions; base/baseline/underlay/inference-mask paths; navigation dimensions/walkable cells/spawn; a source-derived player sprite; components with stable IDs, asset origin, bounds, foot/pivot, depth, sprite/contact/mask paths, mutable flag, default visibility, movement-blocking flag and explicit footprint cells. Paths are relative to this JSON. Immutable base plus state-driven contributions is the only composition model. No progressive painting or permanent mutation of the baseline occurs.

Source extraction catalog stays under `source-assets/`; placed scene sprites under `build/`. Any resize uses nearest-neighbor sampling and is recorded. Original-pixel extraction claims apply before resizing; composition exactness compares the authored scene baseline to its reconstructed exported layers.

## Performance and observability

Following `Docs/PERF-FOUNDATION.md`, decode and cache assets once, redraw when state changes, and rebuild occupancy only on movement-blocking state changes. Precompute hit masks and cell lists. Pathfinding operates on the bounded 48×32 grid; there is no full-scene raster reconstruction on each animation frame. Review actions report accepted/rejected outcomes visibly. Browser measurements are not Unity performance evidence.

## Acceptance and honesty bounds

- [x] Source sprites have real alpha and exclude sheet labels/neighbors.
- [x] New scene visibly uses the supplied sprites in a coherent layout.
- [x] Intact exported reassembly equals the authored baseline exactly.
- [x] Removing all mutable props equals the clean supporting composition; no baked duplicates.
- [x] Spawn and authored destinations are reachable; removal changes only declared blocker cells; restoration cannot trap the walker.
- [x] Individual and multiple removals, restore, selection, solo, overlays and native view work in the actual browser. See the live review record for observations and limits.
- [x] Tests, adversarial review, live browser checks and provenance recorded.
- Scope exclusion: Unity import/gameplay integration.

This is original CoO art preparation, not Qud-parity work. Generated ground is authored inference; source sprites preserve visible imagery, not complete unseen sides or animation. Review removal does not grant in-game destruction or harvesting permissions.

## Implementation log

- Inspected source art and current Felling implementations; resolved the two-version ambiguity with the user before dependent runtime work.
- Reused the user's existing authorization for deterministic image scripts; no repeated tool approval needed.
- Delegated source extraction and browser implementation while composing the environment and building independent verification.
- Extracted 51 native assets with 628,959 uniquely owned source pixels and zero ownership overlap. Composed 81 instances from 46 distinct assets: 46 mutable props and 35 fixed layers.
- Generated one complete empty cavern substrate; retained the exact prompt/output and its SHA. Source props were extracted, not regenerated. Built 81 placed sprites/masks, a complete atlas, independent baseline/reassembly and 46 removal comparisons.
- Initial route assertions caught two blocked destination cells. Corrected footprint alignment and authored destinations. All 652 initially open cells and all six authored destinations are now reachable.
- Visual review found three fully occluded mutable details and scattered entrance pavers. Moved the details to visible edges and connected the pavers through the dolmen. Follow-up reviewed the final baseline and corrected removal strips; every mutable removal now changes visible pixels.
- Corrected the source bottom-left/player top-left pivot conversion after an actual-artifact regression failed. Added source-derived player placement checks.
- Browser adversarial review fixed underlay hit-picking of hidden props, encoded path traversal, invalid player scales and inconsistent ID depth ties. Live input review fixed movement after focusing checkboxes. Added smooth 110 ms position interpolation between 115 ms committed steps and a desktop fit height that shows the entrance traveler.
- Final live review found and corrected an undefined time variable in Exploded view introduced by the interpolation change. Added tests that execute the actual exploded and normal render functions, then reloaded and visually verified the corrected sprite-card inventory.
- Independent final run at 20:39:44–20:39:46 UTC: 86 Python tests (16 extraction, 17 composition, 53 adversarial) and 54 Node tests; zero skips; eight gates passed. Validated 167 PNGs, 82 placed derivatives including the player, all 81 atlas entries, current provenance and zero reassembly pixel differences. No watched input/output changed during the run.
- Final evidence: `ArtSource/CatacombVillage/reports/verification.json`, raw logs alongside it, `inspection/composition-review.md`, `reports/browser-review.md` and `reports/implementation-record.md`.
