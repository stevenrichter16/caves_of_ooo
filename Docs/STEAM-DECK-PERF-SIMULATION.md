# Steam Deck simulation and integration work

Status: implemented; native verification and source-order review correction in progress; baseline 5657211c5. CoO-original optimization, not a Qud port.

## Plan

1. Add a runtime-only relevant-part membership index to Zone. Seed each requested
   type once, update on physical entry/exit and successful AddPart/RemovePart,
   discard on save hydration. Expose read-only iteration for observers and pooled
   snapshots for callbacks that can mutate membership. Query types use assignability.
2. Material turns snapshot the union of thermal, lifespan and status owners, in
   the existing X/Y/cell-object order. Read live temperature/effect fields each
   turn; process the same active/passive lists in the same order. Reuse scratch
   containers and release them in finally. This narrows discovery without changing
   fire propagation, RNG ordering, creatures' turn ownership or multi-cell ticking.
3. Terrain-source seeding and combat-intent readers query their part membership
   instead of every floor/wall/item. Source callbacks receive a stable snapshot.
4. Preserve dirty cell hints across full visibility redraws; player motion emits
   its old and new cells. Only identified visibility-only requests avoid full native
   geometry reconciliation. Unknown and bulk requests retain full recovery.
5. Suppress legacy water painting only where a live native surface owns the cell;
   preserve unsupported/ASCII fallback and correct restoration after mode changes.

## Pre-implementation sweep

| Assumption | Verified correction / consequence |
| --- | --- |
| EntityVersion describes arbitrary state changes | It describes location/membership; public ThermalPart fields can change directly. Index part membership, not active thermal values. |
| All part writes use AddPart | Save hydration clears/adds Parts directly before RebuildEntityCellsFromCells; status and weapon code only reorder existing parts. Rebuild invalidates indexes; reorder does not change membership. |
| A list can be reused globally during event dispatch | Events can reenter or remove owners. Rent independent snapshots and finally-return them; no live-list iteration in simulation callbacks. |
| Material ordering is arbitrary | Existing traversal is X, then Y, then canonical cell.Objects; preserve it before active/passive partitioning. |
| Every full redraw changes geometry | Player movement needs global FOV but known local geometry. Emit both channels. Unknown sources stay conservative. |
| Native water replaces every 2D scene | Authored 2D scenes also exist. Use actual native ownership, never a generic authored-scene flag. |

## Tests / evidence / review

Native RED: six of seven initial work tests failed as expected. After production, all seven work tests and twenty-one adversarial cases passed (`dcd7bb69e60e4e2186ba72ec5e2c4dcf`). Two hidden-water tests failed RED before the cache suppression and passed in the 1,742-case broad run. Tests cover add/remove/duplicates/subclasses/rebuild, direct
temperature writes, late effect attachment, creature exclusion, callback mutation,
multi-cell once-only timing, exact ordering and full-redraw dirt retention.
Physical Steam Deck timings remain outside local verification.

## Cold-eye source callback correction (test-first)

The initial source-only snapshot changed old behavior: the prior full-owner snapshot looked up Parts as it reached each owner, so a callback could attach a source to a later pre-existing owner and seed it that pass. It also accidentally substituted Part attachment order for zone owner enumeration after reattachment.

Plan: cache an immutable snapshot of exact dictionary enumeration ranks, invalidate only on owner membership change/rebuild (not movement), and capture that snapshot for the pass. Sort indexed source owners by those ranks. If source membership changes during callbacks, refresh only indexed candidates after the current rank; earlier owners and newly spawned owners stay deferred exactly as before. Ordinary unchanged passes scan only source owners; whole-owner order discovery occurs once after actual membership changes when sources are present.

`SteamDeckTerrainSourceOrderingTests` adds nine preimplementation cases: later/earlier attachments, new owner deferral, Part reattachment order, removed-owner exclusion, no repeat for earlier owners, remove/readd at the captured visit position, zero-source recovery, and cached-rank reuse/invalidation. Native RED job `4546aed9331140e6bdcd01783f170b35` confirmed four expected failures (later attachment, original-owner remove/readd, missing order cache, and reattachment ordering), with the other five cases passing. The implementation now captures immutable dictionary ranks, tracks part-membership revisions and refreshes only remaining source candidates after callback mutations. Zero-source passes return before requesting owner ranks. Native GREEN is pending. Qud reference: none; preservation of the existing game loop.
