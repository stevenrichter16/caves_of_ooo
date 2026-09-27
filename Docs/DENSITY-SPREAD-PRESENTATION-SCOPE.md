# C15: map-owned Spread presentation scope

Status: published after exact source preimage checks; independently reviewed by root and standalone_verify. Native execution and renderer integration pending. This is CoO world authority, not Qud parity. It enables the renderer to ask whether a real current graph belongs to the Spread style; it changes no layout, population, actor, item, loot or save data.

## Sweep corrections

| Initial assumption | Verified source | Consequence |
| --- | --- | --- |
| Add manager/save attachment hooks. | WorldLocationContext already binds actual generated/accessed/restored graphs through OnZoneAttached. | Reuse it; no duplicate hooks or singleton. |
| A cached lair address proves generated ownership. | CachedZones can be replaced with a new object at the same ID. | Bind derived exact Zone/Manager/Ledger references only after successful commit or complete restore. |
| Inspecting a ledger is harmless. | LairStacks.Inspect reaches the lazy ledger creation/adoption path. | New IsCommittedFloor uses only TryGetValue and never creates state. |
| Spread has higher-tier surface content. | All142 actual authored Spread map coordinates are tier1, including Slip. | No false higher-tier reachability claim. Imported creatures/items still require renderer coverage. |
| Source rigs need Hurt/Die. | Shipped catalog clips are Idle/Walk/Interact/Attack/Hit, with separate native spell casting. | Preserve actual rig contract; death cleanup is a runtime acceptance check. |

## Contract and bounded implementation

`SpreadPresentationScope.IsActive(zone)` requires the exact graph currently cached by its actual WorldLocationContext manager, canonical in-bounds Overworld address, valid current map arrays and current Spread biome. Every surface POI is allowed. Non-reference detached coordinate-only graphs are rejected; the existing exact ReferenceGladePlan geometry contract remains separate and unchanged.

Underground scope additionally requires current Lair POI, recorded Spread biome, nonlegacy record and generated bit within the saved final depth. The exact cached graph must be bound to that manager and current ledger by successful CommitGenerated or Restore. Ordinary caves, ungenerated depths, legacy single-floor records, foreign cached replacements and incomplete loaded graphs fail. Removed stairs, dead boss, depleted caches and ordinary current tier changes do not regenerate content or revoke an otherwise committed floor. No new save fields exist.

The per-zone parsed-address cache is derived presentation state only. Querying never generates a zone, changes RNG, stocks rewards, repairs routes, creates/adopts a ledger or mutates the map. Current map biome/POI replacement is observed each query.

## Evidence and counter-checks

Initial meaningful32-case run:19 missing-feature failures and13 negative controls. Implemented32/32, then expanded39/39. Existing lair/legendary/reward/save integration plus new suite:263/263 GREEN. Removing only the successful-commit binding produced2/2 expected failures; removing only the restore binding produced1 expected lair failure while the surface-load control passed. Actual Unity-reference full runtime and test compilation both report zero errors. One early test API typo was corrected before meaningful RED and is not counted as production failure evidence.

The39 cases cover every surface POI kind, map and biome changes, malformed IDs/arrays, cache identity, lazy-state absence, actual lower-first stacks, legacy controls, fake graphs, depleted/removed owners, full save reconstruction and failed incomplete saved graph. A full20×20 address control independently observes the actual142 Spread surface cells.

These tests prove eligibility and exact ownership, not rendering, native performance, actor model coverage or image quality. Renderer integration and root-owned native gates remain separate.

## Self-review

- Closed: a depth/type-only check would allow borrowed same-address graphs; exact derived commit/restore references prevent it.
- Closed: lazy ledger lookup would mutate ordinary rendering queries; read-only TryGetValue controls prevent it.
- Closed: failed restore must not bind partial floor authority; all saved floor membership is validated before binding.
- Pending native: whole-biome renderer/profile switching, actual save input and visual family closure. No image acceptance claimed here.

## Files

- New Gameplay/World/Generation/SpreadPresentationScope.cs and meta.
- Narrow LairStacks.cs derived exact-floor binding/query; no persisted schema change.
- New SpreadPresentationScopeTests.cs and meta.
- Verification receipts, source manifest and actor/portable source requirements are retained with the C15 evidence. Source requirements enumerate107 concrete creature definitions and188 Takeable/Item-tag definitions; neither number proves universal ordinary reachability or complete native model coverage.
