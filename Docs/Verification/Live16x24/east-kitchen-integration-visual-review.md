# East / kitchen integration visual review

2026-10-24. Independently inspected the seven supplied actual Unity PNGs, all confirmed 1280×800, against `east-kitchen-baseline-kitchen-layout.png`, the native contact, and the approved Cook PNG. Read-only; no Unity calls or source changes.

## Finding: repaired pan still tells the player it is split

**P2 — contradictory state wording, not an atlas defect.**

In `Docs/Verification/Live16x24/east-kitchen-integrated-pan-repaired-reader.png` the portrait visibly changes to the pale repair seam and Current State correctly says that fresh clay closes the split and the pan can hold a paid meal. However:

- The examine title and right-side focus still call it **“split clay batch pan.”**
- The generic description beneath Current State still commands **“Repair the split with two fire clay, then commission Orven.”**

The same stale name appears in `east-kitchen-integrated-pan-repaired.png` beside the successful-repair log. The reader leaves a reasonable player uncertain whether the paid repair actually fixed the object. Use a state-neutral base name/description or render those two text surfaces from the real integrity state. Keep the already-correct repair-seam sprite and Current State sentence. No new art is needed.

## Bounded visual acceptance

- `east-kitchen-integrated-continued.png` visibly replaces the baseline glyph-heavy kitchen with the actual native objects in the same arrangement. Its quiet solid ground makes the stations and characters easier to separate. No smoothing, displaced giant sprites, extra shadow footprints or invented adjacent furniture is visible.
- The actual Cook reads as the approved rust/cream cap-and-apron figure in world view and `east-kitchen-integrated-cook-reader.png`; the enlarged portrait retains the same flat clustered style. Screenshot review supports visual correspondence, not a new byte-identity assertion.
- Cot, chair and oven are distinguishable in the installed grouping. The cot's light woven bars are small but its height and two end rails separate it from the open-backed chair. The oven has a round pale body and dark mouth, unlike the rectangular furniture. The earlier potential rack/grille confusion is not a blocker in this arrangement.
- `east-kitchen-integrated-pan-reader.png` versus `east-kitchen-integrated-pan-repaired-reader.png` clearly shows dark crack versus pale seal in the portrait. The corresponding world cue is subtle at native scale but visible; it is supported by the real repair log. No source-pixel change is warranted solely to enlarge it.
- The three adjacent pan/tray/board owners remain distinct: rounded ochre pan, divided input tray, plain handled pickup board. Neither neutral tray depicts food or an active job.
- Ripe claspbean appears below the kitchen in `east-kitchen-integrated-continued.png`. `east-kitchen-integrated-crop-actions.png` presents ordinary harvest choices. `east-kitchen-integrated-harvest-ground.png` shows the mature plant removed, a small loose seed on the selected tile, two pulp entries plus seed in Contents, and the harvest log. This supports ordinary harvest→ground feedback; it does not show all three overlapping goods simultaneously, and should not be described that way.
- The 16×24 objects remain appropriately small relative to the whole map and consistent with the other native assets. This is actual native-cell presentation rather than the larger concept-board scale.

## Wider scene follow-ups, not reasons to rewrite this kit

- Long repeated hedge segments dominate the composition more than the service objects. The uniform motif follows existing owner placement. Future connected edge/corner variants could reduce repetition without changing geography or repainting approved characters.
- Sparse isolated glyph fallbacks remain in the wider region, and much of the ground is intentionally empty. This slice therefore demonstrates a coherent kitchen cluster, not complete regional asset coverage.
- The supplied integrated set is kitchen-focused. It does not show the actual integrated forge approach or enough CharmFlowers to accept their repeated field appearance. Those checks remain open; no claim about flower density or forge approach contrast follows from these images.

## Limits

No physical Steam Deck performance/control conclusion; no frame-time/renderer-count conclusion; no ordinary stage0→stage1 growth sequence; no complete paid kitchen working→ready→collected sequence; no claims about other seeds, fog memory, or every sprite state. Those require separate evidence. Subject to correcting the contradictory pan wording, the supplied captures support bounded kitchen native-art and real repair/harvest visual acceptance.

## Added ordinary captures reviewed

The following four actual 1280×800 captures supersede the earlier missing-forge/flower observation. No Unity actions or repository edits were performed for this review.

- `east-kitchen-integrated-east-return.png`: the forge, copper elbows, low vent grilles, millstone and pale-pink CharmFlowers are visible in the actual east zone. The flower sprite remains subordinate to the player/HUD and is consistent with the accepted flat palette. There is no oversized flame or invented forge table. The scene still exposes long, mechanically regular flower bands; that is a placement/composition issue, not a reason to redraw the flower frame.
- `east-kitchen-integrated-forge-reader.png`: world and portrait show the same squat, neutral masonry hearth with a dark mouth. The selected owner is the real tinker’s forge; its description names the actual forging/reforging use. The source’s decision not to depict active fire is appropriate.
- `east-kitchen-integrated-forge-actions.png`: ordinary interaction opens the real component/weapon/quench menu and shows the actual carried choices. This establishes access and UI correspondence; it is not evidence of a successful forged output.
- `east-kitchen-integrated-kitchen-revisited.png`: the approved Cook and distinct oven/cot/chair remain legible on return. The harvested crop has not visually resurrected: the small prepared-ground patch remains empty. The neutral trays do not promise a meal/job state. The image still shows a small service corner visually outweighed by repeated large rectangular hedge runs.

**Updated disposition:** no additional significant sprite/integration defect in these four images. Bounded forge, flower repetition, kitchen repair/harvest and return appearance can be accepted after the already-reported pan wording correction is replayed. The parent is fixing that P2; this review does not claim the text fix has yet been visually verified. Full paid cooking, crop growth, other seeds, motion, packaged output and physical Deck evidence remain outside these images.

## One recommended next pass: compose the opening as a connected working landscape

Do this as one scene-level G2/G3 pass, with the east forge and Orven’s kitchen as the first concrete before/after, and the six-stop journey as its acceptance route. Do not start another unrelated item atlas. The aim is that routes and inhabited working areas read before hundreds of repeated boundary motifs, while the glade, cellar, works, plot, forge and kitchen retain different purposes.

### Why this is the next meaningful change

`GRAPHICS-COMPLETION-PLAN.md` puts whole-scene framing, material contrast, purposeful clusters and the complete six-site journey before regional asset expansion. `LIVE-16X24-ART-DIRECTION.md` explicitly calls for broken rhythms and concentration of human-made detail where people work. The coverage ledger lists G2 scene-level finish immediately after east/kitchen.

The visible repetition has an identifiable source: `SpreadCompositionPlan.CreateExplorationParcels` gives BrokenEnclosures five axis-aligned rectangles; Hedgerow `ObjectAt` places the same solid Hedge along their boundaries except the broad gates/approaches. The flower-avenues branch uses three nearly parallel sine bands. `SpreadExplorationResidents` then fits the whole kitchen’s small relative stamp into the nearest safe gap around its seed-derived anchor. This produces the observed tiny service corner inside large empty geometric rooms. Another oven, brighter Cook or finer pan would not fix that hierarchy.

### Concrete work and bounds

1. **Recompose the actual cold-generated service footprints and their approaches.** Give Orven one clearly entered, asymmetric working court rather than another complete rectangular field room: one stronger shelter/hedge side, a short returning edge, a broad open working side, and the existing oven/pan/input/pickup/cot/chair arranged around usable space. Give the forge a small broken masonry work edge and a clear entry from its real dry approach. Use the existing relative role positions/seed-derived anchor and safe-placement proofs; do not paste seed-64 coordinates into other seeds. Keep the functional owners and finite stock unchanged. If a proposed footprint cannot satisfy existing access/withdrawal guarantees, retain the valid old layout rather than deleting obstacles after generation.
2. **Break the landscape’s dominant repeated silhouettes in the same pass.** For the exact admitted opening surface neighbors, replace a few long complete parcel rectangles with offset/L-shaped surviving hedge runs and broad deliberate gaps. Change the east flower placement from three ruler-like rows to two or three unequal drifts interrupted by the working approach, retaining quiet empty ground and the same flower identity. These are real generated Hedge/CharmFlowers placements, not renderer-painted openings. Keep relevant cover, passage/haul, border and encounter constraints; do not randomly puncture boundaries used by existing gameplay. Do not globally replace Spread’s geography.
3. **Give real activity a restrained ground/material footprint.** Reuse existing native road/soil/masonry vocabulary where actual RoadStone, CultivatedSoil, wall and coating owners support it. If the new cold-built court needs a short packed approach, create actual RoadStone owners in validated bare, noncrop cells linking it to the route. The blueprint describes a packed road; it should lead somewhere, not be sprinkled under furniture as generic decoration. Keep cultivated beds and water/hazard states exact. `IsApproach` is only reserved traversability: `GroundAt` deliberately returns Grass outside OldRoad, so do not color every approach as a road in the renderer. The current native ground catalog is glade-scoped; any six-site admission must be explicit and preserve current-world/POI/fog checks, not silently widen from the foreground-kit change.
4. **Finish connected boundary appearance only as needed by that composition.** The current cell-by-cell hedge motif remains a stronger repeated visual rhythm than the small props. If the captured redesign still needs it, add restrained connected hedge run/end/corner derivatives selected from actual visible neighbors, keeping clear passable gaps. This supports the composed scene; it is not an isolated new sheet deliverable. Do not add glow, filler clutter or fake resources to make the courtyard feel occupied.

### What stays stable

- Approved Seed Keeper, Cook, tatterjay, grazer and beetle-jar source bytes; accepted Player, masonry/ground, plot/works products and all 18 east/kitchen source frames unless a named defect is demonstrated. New hedge connection derivatives may coexist with the accepted base; do not repaint the whole style.
- Existing current/save entity graphs, harvested/depleted/repaired states, inventories, service contracts and actual runtime custody. No live rearrangement or refill for prettier screenshots. Cold-generation geometry changes require a fresh isolated world for the comparison; a continued old world must remain valid.
- The accepted glade’s pond/garden/ruin function, cellar stairs and underground collision, known border ports, safety/withdrawal rules and carried-state art. Use their existing visual language to judge the route, rather than unnecessarily rebuilding the glade or cellar to match a new courtyard.
- Resting, crop growth/watering, crafting, repair, combat and resource mechanics. This pass exposes them spatially; it does not add a new service or content system.

### Evidence required to accept this pass

Capture baseline and revised ordinary views at the same 1280×800 framing, first with seed64 and then a different already-supported layout seed such as65. Use actual screenshot + approved sprites for one tightly constrained imagegen scene paintover, reject invented props/services/walkability, then map the selected geometry to real owners before native finish. Test the generation changes RED→GREEN with border connectivity, exact graph authority, ordinary safe approaches, two-sided service access and unchanged finite custody; counter-check any passage/haul/cover scenario touching changed hedges.

Walk from glade through the actual six stops, use at least one existing interaction in each relevant court, depart/return and save/Continue. Review the whole view with selection removed: the service cluster and its approach should be recognizable before the enclosing hedge motif, empty ground should remain purposeful, and no route/working state should be promised only by paint. Check 1×/2× world magnification with the HUD. Report unchanged older saved geometry honestly. This would be a visible scene-composition improvement; it still would not independently complete motion, packaged launch or physical Deck gates.

## Pan wording P2 closed — final bounded visual check

Inspected `Docs/Verification/Live16x24/east-kitchen-readout-fixed-reader.png` against `east-kitchen-integrated-pan-repaired-reader.png` (both actual 1280×800). The examine title, FOCUS name and Contents now consistently say **clay batch pan**. Current State still correctly states that fresh clay closes the split and the pan can hold a paid expedition meal. The static description now describes the pan, free public oven and commissioning terms without instructing the player to repair an already-repaired split. The previously contradictory log description is also absent from the current readout.

The repaired sprite remains readable and unchanged: a direct read-only pixel comparison of the portrait region `(680,135)–(745,180)` in the earlier image and `(680,150)–(745,195)` in the corrected image is exactly equal. The 15-pixel shift is the popup’s new vertical layout, not an art change. The pale repair seam and neutral shallow-pan silhouette remain intact. Text fits within the reader; its footer and close/back controls are visible.

**Disposition: the reported P2 is resolved in the supplied actual replay. No remaining P1/P2 found in this bounded east/kitchen native-art and repaired-pan readout review. Freeze the accepted kit.** This closes the earlier conditional acceptance; the full cooking loop, other seed/layout states, complete six-site scene composition, packaged/device and motion gates remain separately open as already recorded.
