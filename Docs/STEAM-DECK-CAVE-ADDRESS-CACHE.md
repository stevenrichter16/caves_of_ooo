# Native rendering: cache immutable cave address parsing

Status: native regression GREEN 29/29; matched timing pending.
Qud reference: none. No gameplay or rendering eligibility change is intended.

## Verified cost and implementation plan

`SpawnRing3DPresenter.PresentationVisible` checks `GladeAuthorityMatches`, which
calls `BiomeCropRecipes.IsOrdinaryCave` even for surface zones. That method calls
`BiomeCropPlan.IsOrdinaryCave(manager, string)`. Before rejecting depth zero,
`Canonical` splits the address and builds its canonical string every time. Native
tile suppression and per-owner visibility call these predicates repeatedly. This
is independent of the previously rejected extra recipe-resolution experiment.

The root's current Mac movement baseline has a full-redraw median of 38.508 ms
and p95 47.816 ms (run `e6c5d38cbb5b4a6f92b29bdb753ebf3b`). This marker does not
attribute milliseconds to address parsing; the candidate still needs an actual
paired measurement, in addition to a work-count proof.

1. In `BiomeCropRecipes`, retain a `ConditionalWeakTable<Zone, Address>` like the
   existing `SpreadPresentationScope` address cache. Store only the last exact
   `ZoneID`, canonical validity, coordinates, depth and canonical surface string.
   Reparse when the mutable ZoneID changes; weak keys do not retain old worlds.
2. Extract the live ordinary-column predicate in `BiomeCropPlan` into an internal
   canonical-address entry point, reused by its existing string entry point and
   the presentation cache. Never cache manager, POI, saved lair ownership, map or
   the final eligibility answer. Check exact current graph membership each time.
3. Add a read-only cumulative parse counter for deterministic work tests. Warm
   surface and cave queries must perform zero further parses; a changed address
   performs one new parse. The counter measures actual parser entry, not calls to
   the predicate. Root runs missing-counter/API tests RED before production.
4. Counter-check raw address changes, canonical aliases, protected columns, POI
   changes, late lair claims, map disappearance, receiving-manager changes and
   same-address cached graph replacement. Keep standalone clones rejected.
5. Run existing cave art/crop placement fixtures, then matched root performance
   route. Retain only if correct and measured work improves without a timing
   regression. No new per-frame cache, gameplay state or save data is added.

## Pre-implementation corrections

| Initial premise | Verified correction |
| --- | --- |
| Depth-zero test already avoids parsing | It follows `Canonical`, so surface visibility calls still allocate split/canonical strings. |
| Current zone ID alone grants cave styling | `WorldLocationContext` and exact manager cache membership are required and must stay live. |
| A removed lair map icon restores ordinary cave status | `LairStacks.HasSavedColumn` preserves real saved ownership after POI deletion; never cache its answer. |
| An address cache can reuse world authority | Only immutable data derived from the current string is cacheable. Different managers and mutable POIs must change the answer immediately. |
| A strong dictionary is harmless for these tiny strings | The game can visit many zones; use weak zone ownership instead of retaining entire zone graphs. |

## Evidence, self-review and bounds

Root-owned native job `906c01be24a84e60b53d5f5607f4d9d3` confirmed the nine
work-test failures before production (missing parse diagnostic). All twenty
authority/adversarial counter-checks already passed. The combined job contained
102 cases: 83 passed, 19 failed; the other ten failures belong to the separate
root-owned fog milestone. Receipt:
`Verification/SteamDeckPerformance/2026-10-10/slate3/fog-cave-red-gas-green.xml`.

Implementation shares the strict canonical parser between public crop planning
and the weak presentation cache. A warmed query reads current receiving-manager
membership, map, POI and saved lair ownership with cached coordinates; only the
string parse and canonical/surface string creation are skipped. Null/empty
addresses or absent manager caches fail closed before dictionary access.

Self-review: no zone/manager reference is retained by a strong static map;
address changes replace all parsed fields, including invalid results; manager
reattachment does not reuse authority; both planning and presentation route into
the same live column predicate. The twenty authority cases are regression pins,
not twenty claimed bugs.

Root-owned combined job `6657f5e2ec39437daea5599ac8ebf4b5` passed all **29 new
cache cases**, including zero additional parses over 4,096 warmed surface queries
and 4,096 warmed cave queries. The full combined job contained 198 cases and one
failure in the independent audio milestone; no all-job GREEN claim is made here.
Broad final regression passes1359/1359 including all29 cache cases. Paired movement timing is inconclusive because NPC behavior changes the workload; see STEAM-DECK-PERFORMANCE-SLATE3.md.

Cold-eye review compared old/new authority order and shared canonical parsing.
The old exact receiving-graph, current POI and durable lair checks remain; surface
queries now reuse a known depth-zero address rather than repeatedly splitting it.
The value stores no manager/world/zone reference and contains no cached eligibility
boolean. Invalid-to-valid address parsing uses the same canonical helper as crop
placement. Adjacent `ReferenceGladePlan`, `SpreadPresentationScope` and
`SoddenPresentationScope` already avoid repeated parsing; no broader authority
cache is needed for this finding. No additional actionable issue found.

Work counters and Mac timing will be reported separately. No physical Steam Deck
CPU/GPU claim follows from source inspection or a successful Unity test.

Files planned: `BiomeCropRecipes.cs`, `BiomeCropPlan.cs`, focused/adversarial tests
with handwritten Unity metadata, this living document.
