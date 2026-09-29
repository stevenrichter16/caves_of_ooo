# Controlled arena cache identity — final bounded setup correction

The actual fixed run `a0b3ba924cac492aa637cd3a48084105` reached contact after one pull and11 waits through12 legal unforced moves around the hedge end at(37,7). Root reports all12 gameplay checks passed, but one unexpected normal autosave error prevents describing it as a clean Play acceptance. This review addresses setup authority only, not pathfinding or gameplay.

## Exact cause

The observer stages `new Zone(originalZone.ZoneID)`. `ZoneManager.SetActiveZone(Zone)` at68–72 replaces the cached value for that ID. But the generated source at Overworld.11.10.0 is already held by `SpreadExplorationPlan.installed`. Its `ValidateInstalled` at179–182 requires the exact cached original; `BindForSave` at268–274 validates every retained graph. Normal InputHandler.HandleZoneTransition calls QuickSave at969–970 and correctly rejects the replacement. Keeping the old object in an observer local is insufficient: it must remain at its actual cached address. Neither save errors nor installed records should be cleared, suppressed or bypassed.

## Smallest safe setup

1. Before creating any arena entities, snapshot existing cache key/reference pairs and `Exploration.RetainedGraphCount`.
2. Select the first canonical-ordered existing Exploration entry for which Family=None, DispositionFor(id)==0, no cached zone exists, and TryGetPlacement(manager,id,out current) succeeds with the exact entry. These queries only read existing metadata. They exclude current protected, rare, wayhouse and regional placements without generating a source. Refuse if no candidate exists.
3. Create the same controlled `Zone(selectedId)`, copy the same starting ambient scalar values and stage exactly the same real factory Grass/hedges/beam/pursuer at unchanged coordinates. Do not call GetZone, a pipeline, TryMarkPlacementCommitted, FinalizeGenerated, or mutate any manifest/installed/disposition state. Mark it explicitly as a test-authored graph at an unused address, not an ordinarily generated opportunity.
4. Use the same single original-player transfer and actual HandleZoneTransition. It installs one new cache entry, attaches normal current world/render authority, registers NPCs normally and performs normal autosave. Keep every previous cached reference including the original11.10 graph; require old cache count+1, exact new arena and unchanged retained count/disposition0. The player naturally leaves the old graph, so do not require its old entity membership to remain unchanged.
5. Keep the end-of-frame capture, visibility/approved-mesh assertions, real grab/pull/release/waits, target/interference admission, source identity checks and ordinary cleanup. Do not reset clock/energy or force a refresh/goal/path. Normal isolated teardown discards the temporary world.

`OnZoneAttached` only attaches derived contexts; it does not install a generated exploration graph. `BindForSave` checks already-installed graphs and writes their dispositions; an additional cached disposition0 controlled graph does not violate that contract. No saved migration or production save relaxation is justified.

Ordinary WorldTravellers may still run at a Family=None address. Preserve the actual hook and additions ledger. If a two-actor controlled setup is desired, source-only selection may additionally require the existing deterministic `WorldTravellers.EntrySample(seed,id)%8 !=0`, explicitly disclosed as metadata admission rather than suppressing an actor or overriding RNG. Otherwise retain existing honest interference refusal. Existing player signature protects stats/gear/purse without falsely requiring normal arrival-memory properties to remain absent.

## Acceptance recommendation

Retain the original paired observer results and their exact input hashes: unchanged FindPath stalls through30 waits; fixed FindPath traverses the same arena. Both used the same observer/layout, and the autosave fault occurs at setup, so the movement differential remains useful with that limitation stated. Run one clean candidate after this observer-only cache correction, with the same coordinates/geometry/real source definitions and unchanged assertions. This demonstrates clean setup, autosave and execution; it is not a newly identical-address baseline pair. The native43 baseline12-failure evidence and735 affected GREEN remain independent mechanics gates. Another full baseline Play is unnecessary unless root requires identical corrected setup for the final claim or new interference changes the actual mechanics.

Read-only recommendation only. No shared source, Unity, Git or gameplay changes made. Root owns the final correction and execution; the clean follow-up is not claimed complete here.
