# Gantry composition

Status: complete and installed for fresh native generation. CQ13 passes all 433 targeted checks. CQ18 full suite: 14,476 passed, 32 unchanged baseline failures, zero C# errors and no new failures. CQ15 reproduces all 380 asset/metadata files byte for byte; CQ19 confirms installation and GUID uniqueness. CoO-original composition and art; no Qud-parity or live-FPS claim.

Gantry is the tier-one Spread road crossing at `Overworld.7.8.0`, authored profile `CrossroadsExchange`. Three formations—CrossingExchange, CaravanForecourt and RegistryCrossing—reassign exchange, record office, rest and household spaces around useful crossings. Dimensions follow activity. The native central well remains at (40,12). Caravan shelter is native Cloth; permanent partitions are ordinary destructible Wood. Ground, rooms and live owners are generated before art.

## Verified lore and scope

Geography describes low-strength Concord exchange, Pale Curation records and Tent-Right caravan rest (Lore/History/02_Geography.md:58). Timber and practical furnishings follow MaterialCulture. This is a mundane records office: FilerClerk belongs to Marrowstye and could wrongly complete its courier quest. A separate ordinary registrar carries no quest actions, trade or reputation grant. The native host still offers actual UnderTheCloth by conversation, never by proximity. Copying remains the ordinary Scribe service, not the registrar desk.

New physical owners: GantryTimberWall, GantryRegistryDesk, GantryExchangeCounter (Container8) and GantryWayboard; all destructible and Wood. The registrar is an ordinary mortal Creature with existing self-preservation and loadout. No new fishing, caravan hire, debt or audience mechanics. No old-save migration.

## Implementation

Base1000 stages every required blueprint before publishing. Profile3860 places four native owners after the cave roll, one time per realized graph. Arrival3870 reserves actual stairs and neighbors. The cave filter verifies the current complete3×3 dry exterior footprint, not just original plan cells. New graph/profile guards are finite, exact and owned by the map that created or restored the graph.

`TryGetServiceCell` supplies native Merchant/Quartermaster in ExchangeFloor, Scribe in RegistryOffice and Innkeeper in CaravanRest. Village population claims valid positions before decor/other residents, revalidates before publishing and falls back on current ordinary placement if invalid. This retains native stock/repair/rental/service wiring and avoids duplicate NPCs.

## Verification and review

Actual phase RED: CQ01-red, compile failure for missing Gantry/Quillhold builder types before implementation; no stale XML interpreted. Tests include finite-address counterchecks, three role arrangements, all-cell connectivity, late population, missing/malformed owner atomicity, live cave footprints, graph reuse and service anchor fallback. Focused/full results pending.

⚪ Native pre-existing beds/chairs/poles retain their existing mechanics; their absence of Destructible is not silently changed by presentation. New authored furniture is destructible. 🧪 Render captures verify composition, not interactive feel.

## Art and performance

Seven families, four variants each; continuous muted ground/path, broad timber walls, two low working surfaces, pale registrar and coarse wayboard. At most two colors/model, <=240vertices, one cell footprint, one mesh, no collider/light/MonoBehaviour. Variant identity for the registrar ignores position. Generation allocations happen only on fresh build; render only maps current owner references and borrows immutable resources.

## Implementation log

- 2026-09-16: Lore/API sweep, RED then base/profile/arrival and actual new content authored; shared integration and review in progress.


## Final integration and verification

The pending gates in earlier chronological entries are closed. CQ14 exposed one additional literal equipment-list failure: GantryRegistrar was missing from the expected kit table. The retained equipment ownership/loot exclusions now include its intentional gloves and boots; CQ17 verifies the complete equipment fixtures before the final full run. CQ13 passes all 433 phase checks, including 219 cases in dedicated adversarial fixtures. CQ18 adds 434 passing cases to the verified TC20 baseline: all 32 existing failures have identical test names and failure messages. One former Tine negative control was renamed and both Tine/Gantry plain-village controls now explicitly select an ordinary profile; their negative assertions remain intact.

[Final twelve-view gallery](Verification/VoxelWorld/CQ12-final-preview/index.html) covers all three actual formations in each of the four towns. All requested seeds are nonzero and match each native manager's effective seed. Every capture has zero missing meshes and zero unmodeled visible owners. CQ05/CQ08 seed-zero formation labels are superseded evidence. Independent source, player-flow and final visual reviews are complete.

The phase adds 92 combined-mesh models, with four variants per family and no more than two palette colors per model. CQ15 rebuild reproduces all 380 asset and metadata files byte for byte. CQ19 checks the installed resources, GUID uniqueness and exact synchronization of 2,099 source/content/assembly/meta inputs between the working project and the isolated validation project. [Close-out evidence](Verification/VoxelWorld/CQ19-closeout/README.md) records the case comparison, asset hashes and exact shared-source delta.

Fresh native generation receives the new rules at Gantry (7,8), Tine (13,7), Quillhold (14,9) and Tally (10,14), surface depth 0. Existing graph instances remain intact. Native trade, containers, copying, lodging, rental frontages, water contact, destruction and entity lifetimes remain authoritative. The current spawn, 1.2x camera and full reveal are unchanged. The original Unity editor was not restarted. Static captures establish scene composition and coverage; they do not measure live input feel, animation or sustained frame rate.

New files and originally clean shared files are committed directly. Exact changes in already-mixed shared files are installed in the working tree and preserved in the close-out implementation patch, without committing unrelated work. This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.
