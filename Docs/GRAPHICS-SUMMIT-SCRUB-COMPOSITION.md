# SummitScrub shelter groups

2026-10-28 — bounded native and actual Editor composition verified; source `98ac4d64b9e01ce43375087c9b3b24beb4f6c8c8` is published and remotely verified. This changes fresh SummitScrub generation only. Whole-area polish remains open; no PNG, sprite-coverage or package advancement is claimed.

## Choice and actual baseline

Compared the accepted Stump summit material capture and Sodden OpenMire v6. Sodden already has basin-edge reeds, clustered ground and readable wet boundaries. SummitScrub's four domes and six isolated trees leave broad rock expanses and disconnected habitat dots. Use a few unequal groups of existing low scrub to make the real shelter pockets more substantial while retaining open windswept routes. Central emptiness and old glyph landmarks remain explicit later debt.

Actual baseline uses immutable `Fixtures/sill-court-return182`, seed 64, HP 40. Read-only runtime inspection confirmed only Glade11.10, WorldMap and Sill10.10 were cached; Summit 3.4 was genuinely ungenerated. Ordinary worldmap Up6/Left7/enter reached Summit 3.4 at40,12. The input tool timed out after executing the route; a read-only position check confirmed completion, so no input was repeated. `summit-scrub-before64-center.png/json` and owner census preserve this baseline. The existing seed 65 `stump-environment-return172` remains a cached-save control, not a new-generation test.

## Preserved contract

Add only existing Bush owners to empty, already protected habitat cells in SummitScrub, after `RepairOpenPockets` and `MarkHabitats`. Aim for 12–24 bushes grouped in 2–3 unequal short drifts beside existing Tree/StoneDome shelters, beyond approaches and their one-cell margin. Preserve dome/tree/tank positions, route/habitat/reservation masks and every other formation. Use coordinate hashes, no new draws from the generation RNG. No new tanks, resources, loot, encounters, services, crops, collision or harvesting mechanics.

Bush is real passable terrain with 6 HP, Plant material and existing thermal/destruction behavior, no harvest/loot part. Existing approved Stump-native Bush art supplies the pixels; no new raster art, sprite packet or imagegen reinterpretation is needed for this reuse-only placement pass. World/inventory/examine contracts remain the existing owner's. Added plant fuel is a real consequence to record, not a purely cosmetic paint claim.

Native signatures for 16 pre-change plans remain pinned in `Verification/Live16x24/summit-scrub-protected-plan-baselines.json`. The 32 new cases cover grouping, approach halo, unchanged old plans, real owners, population species/positions with scrub present versus removed, destruction, rebuild refusal and binary save persistence. Related Rim/Gorge controls also pass. Cached chunks receive no retrofit. This native population comparison uses the real terrain/habitat wrapper; it does not claim exhaustive coverage of every later generation stage.

## Source corrections and limits

- Saved `.sav.gz` payload is binary, not JSON; no direct save editing is used. The failed read-only parse changed no file.
- Stump habitat reservations already shelter valid flora/fauna. Adding scrub only inside that mask avoids widening landmark exclusions; prove exact reservation equality.
- PopulationBuilder admits passable cells and uses real nearby Tree/Tank habitat. Compare the actual wrapper with scrub present and absent; do not assume equal counts from a source glance.
- Fresh seed 64 currently has 126 dome cells, 6 Trees, 8 Tanks, 3 SummitSingers, 2 BrocchiniaSentinels and2SariSnakes. Existing camp/pipes/ash/crop owners are unrelated and stay intact.
- The published Sill shadow pilots stay unchanged and local. No Stump shadow rollout is part of this population pass.

## Native and actual result

The initial 31 cases produced 14 intended RED failures and 17 control passes. V1 then passed 212 related cases, but actual views showed two drifts concentrated at the same western dome. One additional test failed at squared centroid distance 27.777 versus 144. Increasing the existing-Bush separation from 2 to 8 cells distributed the new cover across distinct shelters. Final **213/213 passed**, with 32 new cases, 0 failures and 0 skips. Hash-ranked finite local queues consume no pipeline RNG. Candidate exhaustion can yield fewer groups outside the tested matrix; 21 bushes is the observed seed 64 result, not a promise for every seed.

Three normal-zoom center/west/east pairs show 21 Bush in 9/7/5 drifts. The independent final review found 0 actionable P1/P2 within this scope. Existing stationary owners, positions and solidity are exact; all other blueprint counts remain unchanged. Fresh reservations are 747 before and after. Ordinary 43,8→43,7→43,8 walks through the real Bush at HP 40; its reader identifies Plant material and 6/6 structure with the existing native portrait. Bush is passable fuel-bearing vegetation, not cosmetic paint or new loot.

Fresh Continue 188 (`summit-scrub-return188`, turn 1630, HP 40) preserves every Bush ID and position. Generation reservations are transient and correctly reload as 0; the cached zone is not regenerated. The saved/Continue world crop differs by 173 of 564000 pixels, localized to (463,190)–(477,223), with cause unproven. No whole-scene pixel-exact claim. Before/after camera views match, but mobile actors and elapsed travel time are not exact-state controls.

The 74.23 s Editor profile covers west-to-east traversal/captures plus idle/UI/tool time: 15,899 samples, none wrapped; renderer mean 0.325 ms, p99 0.274 ms, max 107.438 ms; max 41 batches. Reader/save/restart occur after that window. This is neither a controlled speedup nor a Deck or sustained-performance result.

Evidence: [final receipt](Verification/Live16x24/summit-scrub-final-receipt.json), [native summary](Verification/Live16x24/summit-scrub-native-summary.json), [full-generation comparison](Verification/Live16x24/summit-scrub-full-generation-comparison.json), [restart comparison](Verification/Live16x24/summit-scrub-restart-comparison.json), [profile](Verification/Live16x24/summit-scrub-editor-profile.json) and [independent review](Verification/Live16x24/summit-scrub-independent-review.md).

## Source corrections, self-review and limits

The full-generation recorder represented an absent PhysicsPart as false before and null after. Normalizing both to nonsolid resolves that recording mismatch; BiomeCropPatchRecord and all pre-existing stationary owners are unchanged. A 120 ms input repeated worldmap steps and entered Grainfield 2.2; the preserved route-correction evidence records ordinary Down 2/Right 1 recovery to fresh Summit 3.4. Subsequent travel uses short default presses. One test bridge timed out while native execution continued; retained XML establishes the spacing RED without a duplicate run.

§5 self-review: 0 outstanding actionable P1/P2 within this bounded generation change. Tests precede both initial implementation and the actual-driven spacing correction; the native, full-generation census, ordinary movement/reader and save evidence agree. Remaining broad central/lower emptiness, flat platform-like domes, repeated aligned scrub stems, glyph props/pipes, other formations and whole-area polish stay open. No new artwork, runtime rendering, shadow expansion or coverage count is claimed.

Root preservation checks retain 49 foreign paths, 1000 normal Editor save entries, settings and prior/current builds. Ten whitespace-only Unity meta rewrites were backed up and restored; zero bytes deleted. Source `98ac4d64b9e01ce43375087c9b3b24beb4f6c8c8` is pushed and verified on origin/main. Next batch-package the reviewed environment/shadow/composition source and verify ordinary launch/travel; 43a151ddb packages remain current and physical Deck execution remains unverified. Last actual hourly report 20:44:12.183742 UTC; next 21:44:12 UTC, unchanged by this checkpoint.
