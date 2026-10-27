# Overwrit before evidence — independent review

Reviewed 2026-10-27. **The supplied baseline records are internally consistent and suitable as before evidence. They do not establish native-art, runtime, gameplay-after or whole-chunk acceptance.** No evidence-integrity blocker was found. The native extraction caveats below must remain visible during production.

This reviewer read the census, play summary, source blueprints/composition code, three fixture headers and retained reference records. The actual images 06, 07 and 08 and concept v2 were opened at their original sizes. No Unity, game, Git, image-generation or build action was performed; this review document is the only file written.

## Integrity and measured bounds

All **23 recorded artifact rows covering 21 unique files** match their byte lengths and SHA-256 values: census, eight actual PNGs, six fixture files, reference photograph, the repeated reference-input entries, both exact prompts, both concepts and the inherited Grovelands style capture. Repeated rows are references to the same bytes, not additional evidence. All eight actual PNGs are **1280×800**. Both concepts are **1586×992**, distinct generated references rather than native sheets or gameplay screenshots. Concept v1's bytes were verified; this bounded visual review focused on v2.

The census lists **eight distinct actual zone records**, each with 2,000 OverwritGround owners, one Player, HP 38, `actualCurrentGraph=true` and `savedLair=false`. Its enumerated non-ground rows exactly reproduce its per-blueprint counts, with every coordinate inside 80×25. The reader did not rerun the runtime census. The `keys[].id` field contains blueprint names, not persistent entity IDs; this is an owner-family/placement record, not an independent identity-persistence proof.

| Recorded zone | Growth count | Other recorded owners besides ground/player |
| --- | ---: | --- |
| Overworld.0.9.0 | 31 | StairsDown 1, HollowchimeCrop 2, patch record 1 |
| Overworld.0.10.0 | 8 | None |
| Overworld.3.10.0 | 24 | None |
| Overworld.4.11.0 | 28 | None |
| Overworld.4.12.0 | 30 | PilgrimBench 1, MargincressCrop 2, patch record 1 |
| Overworld.3.13.0 | 23 | None |
| Overworld.3.14.0 | 32 | PilgrimBench 1, BinderrootCrop 2, patch record 1 |
| Overworld.0.13.0 | 27 | None |

Thus the receipt's two benches and zero waymarkers agree with its census. Only the bench at **4,12 / 76,18** has an approached world view and full reader here. The second bench at **3,14 / 75,5** is census evidence only. The five pilgrimage candidates are not five furniture observations, and the eight visited zones are not eight accepted native scenes. The ordinary source contains 22 candidate addresses; these records do not cover all of them.

Each fixture's Quick.json equals the play summary's complete copied header. The corresponding gzip was read to EOF, validating CRC/size/footer, and its unchanged binary prefix independently confirms COO0, save/session format 8, game version 1.0, seed 65, game `50e670c30b6d48feb82756df64bd1cbc` and the matching active zone. This was decode-only inspection, not a game load. Turns and HP below come from Quick.json; the entire entity graph was not decoded for this review.

| Fixture | Active zone | Header turn | Header HP |
| --- | --- | ---: | --- |
| overwrit-blank-baseline65 | Overworld.0.10.0 | 4660 | 38/40 |
| overwrit-bench-baseline65 | Overworld.4.12.0 | 5260 | 38/40 |
| overwrit-dry-baseline65 | Overworld.0.13.0 | 5460 | 38/40 |

## Actual visual findings

- [06 bench approach](overwrit-before-06-bench-approach.png) shows player **75,18**, HP **38/40**, a small horizontal bench glyph immediately to the right, sparse growth concentrated at the right margin, and a separate magenta crop owner near the upper margin. Repeated yellow ground glyphs dominate the otherwise quiet field. This is the concrete material/readability problem to address; it is not evidence that new scenery needs to be generated.
- [07 bench reader](overwrit-before-07-bench-reader.png) identifies an **inward-facing bench**, **Object**, **Wood**, **Structure 12/12**, and describes a plain wooden bench facing empty ground. The actual reader supports the summary. No resting, repair, travel or other bench service was exercised or established.
- [08 DryRim](overwrit-before-08-dry-rim.png) shows player **40,12**, HP **38/40**, the same dominant ground grid and a small visible growth cluster toward the lower edge. The centered viewport is not a full 80-column panorama. Offscreen rim objects are not missing merely because the center capture omits them. Conversely, intentional quiet does not itself establish a finished composition.

These frames support ground/cluster/bench before-state comparison. No native Overwrit output, natural waymarker, destruction/burning loop, crop operation, fresh restart of an Overwrit fixture or package-after behavior is demonstrated here.

## Concept v2 and native-part caveats

The [v2 concept](../../../ArtSource/Live16x24/overwrit-materials/concepts/overwrit-v2.png) offers a useful quiet pale plane and restrained dark shoots. Its reduced surface noise addresses the named repeated-grid problem. The existing reference record correctly rejects its gradients, enlarged bench, repainted player/HUD and imperfect placements. These are production extraction requirements, not permission to copy or resample generated pixels.

1. **Preserve the equal-tip identity.** Objects.json describes OverwritNewGrowth as a few pale shoots whose tips all reach the same height. V2 has visibly uneven stems and bright bead-like tips that can read as flowers or seedheads. Use short, sparse shoots with a common native top line; vary lower leaves/spacing instead of height. Do not invent a flower, crop stage, harvest product or species. The NPS photograph is a silhouette reference, not a biome/species specification. This is the material correction not fully captured by the current brief's broad acceptance of v1/v2 shoot silhouettes.
2. **Keep the bench one cell and materially plain.** Its occupied gameplay footprint is a solid wooden object, not a multi-cell platform. V2 remains larger than a literal native cell and does not establish the inward-facing orientation. The source provides `OverwritCompositionPlan.InwardQuarterTurns`; at the eastern rim the inward direction is west. Confirm how the chosen native silhouette communicates that existing orientation, or keep an honest neutral projection rather than inventing new facing state. No service symbol, cushion, occupant or animation is supported.
3. **Keep the floor smooth and opaque.** Its description says smooth ground with no stones breaking the surface; it has no MaterialPart requirement. Avoid the concept's smooth lighting gradient, gravel, raised chips, strong seams or repeated decorative marks. Quiet whole-field color and sparse low-contrast scrape variation should carry the identity. Final scene-scale repetition remains an actual gameplay gate.
4. **Keep the marker provisional.** OverwritWaymarker is a short blank Stone object, solid/destructible with Rubble wreckage; it has no readable sign or travel service. No marker appeared in the five recorded pilgrimage candidates, and none appears in the concept. A source-derived native fixture can validate its routing, but cannot be labelled a natural observation. Do not add a marker to a before/after scene or fabricate a label/arrow to close that evidence gap.
5. **Preserve other current state.** Existing crop beds add CultivatedSoilPart/Plantable to real ground; ordinary ground art must not erase that use. NewGrowth and the bench retain their actual Plant/Wood destruction and thermal behavior. Damaged/burning/removal states need truthful native handling or fallback, without optional animation as a prerequisite. The separate magenta crop, existing player, portable art, fog, approach lanes and owner positions must remain authoritative.

## Runtime and review disposition

At this review, **3,217 existing tracked source files**—all .cs files under Assets/Scripts and Assets/Tests plus Objects.json—match the preserved `ab26e270cb376253c971a41c3449ecbbf4171444` build snapshot byte-for-byte. No Overwrit runtime/test change is evidenced by that comparison. It does not inspect a live heap or certify arbitrary untracked files, and it does not claim the art is integrated.

The baseline and reference bytes pass this bounded review. Before final native authoring, resolve the equal-tip shoot interpretation and preserve the already recorded bench/gradient/placement rejections. Then the normal sequence remains: original editable native source, exact owner/state routing with RED/GREEN, matched ordinary views including bench and regional edge, reader/movement/state checks and fresh restart/package gates. Native fullness, all 22 ordinary candidates, the unobserved marker and all biome coverage remain open.

## Root resolution recorded

The root-added [concept critique and parts map](../../../ArtSource/Live16x24/overwrit-materials/concepts/critique-and-parts.md) now explicitly requires every shoot variant to share a clipped top line, vary only its lower leaves/stems, and reject the concept's flower/seed-bead shapes. That records the requested design correction and agrees with the native preflight's common-height requirement. This closes the written extraction-specification issue only: no native pixels, runtime implementation or gameplay acceptance is established by this note. Review frozen after this documentation check.
