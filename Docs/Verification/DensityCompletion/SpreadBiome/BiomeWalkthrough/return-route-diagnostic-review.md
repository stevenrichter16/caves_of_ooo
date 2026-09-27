# Biome walkthrough return-route observation repair

The actual native run `ae34cda227f54629b86c0946ca3b1ef6` passed the original item drop/pickup, real border travel, actual finite BerryBush harvest, F5/movement/F6 save restoration and exact gear checks. Three WildBerries were acquired. The actor remained at40HP. The whole walkthrough failed while returning to the original border: the current safety-graph route to0,12 grew as native movement traveled east/southeast, then became unavailable at79,22 (tick1280).

This differs from the previous39,0 source-selection failure. A fixed cohort seed is not evidence that the live scheduler and AI trajectory repeated exactly. Neither run demonstrates a production pathfinding or source-generation bug. The current audit forbids loose items/coatings, hazards and cells within2 of a threat, and adopts FindPath's corner rule; it does not enumerate every legal player action.

The two-file candidate adds only read-only failure observation:

- Generic biome WalkTo invokes the existing diagnostic if its safe path is null/empty.
- The diagnostic records the actual goal, its cell and all8 neighbors, alongside all8 approaches for every actual remaining Bones/BerryBush, current nearby cells and exact current threat owners.
- No source selection, BFS, native key, predicate, owner, stats, seed, inventory, time or scheduler changes are made.

A separate confirmed harness premise remains intentionally unrepaired until the diagnostic: HarvestablePart accepts Chebyshev distance1, but source selection enumerates only four cardinal approaches. Thus the audit can omit legal diagonal harvesting positions. The diagnostic now observes all8 without changing behavior, allowing the next actual run to distinguish this omission from genuine disconnection under the audit policy.

Full current-runtime compilation against native Unity references passed with zero errors; existing unrelated warnings are retained in compile.log. Actual replay and final biome completion remain pending. The previous raw report/log/images remain authoritative and unchanged.
