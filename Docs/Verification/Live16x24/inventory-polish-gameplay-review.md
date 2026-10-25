# Inventory polish — independent gameplay evidence review

2026-10-25 UTC. Read-only review of the five original `inventory-polish-{initial,meal-reader,before-brew,brew-complete,return}.png` captures and their paired JSON readouts. All five PNGs are1280×800. No Unity control, source changes, git actions or independent input replay by this reviewer. Root supplied the actual ordinary-input capture provenance.

**Verdict: bounded acceptance of initial real-item selection, corrected meal guidance, one actual brew acknowledgment and status retirement. No significant new visual/data inconsistency found.** This does not accept migration of already-saved meal categories, a newly packaged build or physical Steam Deck behavior.

## Visible result and receipt agreement

- **Initial Inventory:** a real frost-quenched long sword is highlighted with a gold row stripe; its native sword portrait, exact title and useful damage/penetration/family facts appear below. No category header is selected. The readout records Inventory panel1, cursor1 and empty status, with equipped sword2703 and Buckler2704. The source owner/selection tests independently cover identity and guard semantics; the screenshot alone is not an exhaustive ownership audit.
- **Meal reader:** the parcel portrait and full shared-reader text are readable. “Self-use remains available” is compatible with the authoritative “wrapped field meal cannot be thrown.” The earlier promise of throwing is absent. Healing3d4, removal of one ordinary bleed, companion restrictions and no prepared-meal bonus remain visible. Pagination1/2 and return controls fit. Inventory, equipment, tick5830,42HP and40drams remain unchanged while reading.
- **Before brewing:** Brew mode shows one selected stoneburr seed, four unselected pitchpod resin, and a stoneskin tonic preview. The away-from-station single-result limit and Brew One action are readable. The readout carries StoneburrSeed6970/count1 and resin9191/count4, with no completion status.
- **After brewing:** the stoneburr row disappears, the remaining resin is not silently marked, and the next preview correctly asks for reagents. Separate green footer feedback says **“Brewed 1 item.”** without replacing the control legend. The readout records that exact status and success=true. Independent before/after comparison finds exactly one removed owner—StoneburrSeed6970/count1—and one added owner—BrewedTonic13724, stoneskin tonic/count1. Every surviving inventory row is unchanged. No existing mixed coating or meal was consumed; resin remains4. All recorded fields other than capture timestamp, inventory and inventoryView are equal, including HP/tick/drams and sword temper/durability.
- **Return to Inventory:** the filtered wrapped field meal is highlighted, with its matching native parcel portrait and useful inline facts. Status is empty and success=false; all gameplay payload fields equal the post-brew readout. This verifies retirement on leaving Crafting without undoing or repeating the brew.

The captured meal13723 was created before the blueprint category correction and still appears under **MISCELLANEOUS**. These images do **not** demonstrate Food grouping for that existing saved instance. Fresh-factory Food grouping is covered by native tests; no save migration is claimed or required by this slice.

Some tool previews initially appeared to omit unchanged header glyphs. Direct pixel comparison confirmed identical bright-pixel masks for the before/after title, tabs and station hint. That preview artifact is not reported as a game rendering defect.

## Native test evidence

Independently parsed the saved XML leaf cases and full names:

- `inventory-polish-green.xml`:233 unique cases,233Passed,0Failed,0Skipped,08:16:40–08:17:05UTC.
- `inventory-polish-forge-green.xml`:12 unique ForgeCommandsTests cases,12Passed,0Failed,0Skipped,08:31:43–08:31:44UTC.
- Exact full-name intersection is empty: **245 distinct native passing cases** across the two runs. These are not245 repeated claims from overlapping fixtures. The older104-case focused run overlaps final coverage and is not added.

Tests cover selection/reader authority, command feedback and receipts, paid partials/stack merging, held-key release, meal guidance/category and forge/quench/reforge behavior. This review verifies the stored result structure; root owns execution and source-to-run provenance.

## Acceptance boundaries

Actual editor UI and one single brew are observed here. Partial batches, merged output, sludge/mishaps and forge/quench failure are native-test evidence rather than additional events shown in these five captures. No meal consumption/healing, combat proc, animation/performance, whole-scene art, physical Deck input, packaged-player or entire-save correctness is inferred. The earlier disk-full interruption does not count as successful visual acceptance; these later captures supply the bounded replay evidence. Final isolation cleanup and restoration are root-owned and separate from this visual review.
