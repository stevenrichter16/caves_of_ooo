# Steam Deck HUD performance fixes

Status: implementation complete; native GREEN confirmed for all 30 new cases
and nine existing companion cases. Root owns integrated before/after profiling.
Qud reference: none. This is a presentation-work optimization, with existing CoO
HUD contents and lifecycle behavior as its compatibility contract.

## Goal and scope

Stop native tilemap work on unchanged hotbar frames, reuse bounded hotbar
metadata/snapshots using exact visible-state checks, and remove full inventory
screen/action construction from sidebar vitals. Own HotbarRenderer.cs,
HotbarStateBuilder.cs, SidebarStateBuilder.cs and their tests only. Root agent
owns Unity refresh, RED/GREEN runs and live verification. No commits or staging.

Content readiness: 🟢 no new art, blueprints, save schema or gameplay content.
🟡 lifecycle and mutable public ability fields require invalidation coverage.

## Verification sweep and corrections

| Premise | Verified source and correction |
| --- | --- |
| Hotbar rebuilds only on change | HotbarRenderer.Render clears both maps unconditionally. Explicit Clear is called by ZoneRenderer at reset/pause/disabled paths and must invalidate cached presentation. |
| Ability events can invalidate a cache | ActivatedAbility fields and ActivatedAbilitiesPart.SlotAssignments are public and mutable; validate exact source/output values on every build rather than depending on change events. |
| Snapshot can retain input slot collection | HotbarSnapshot accepts any IReadOnlyList, including mutable arrays/lists. Renderer must retain a defensive slot copy, not trust reference equality. Builder-generated snapshots must not reuse a mutable scratch list. |
| Sidebar only needs basic stat reads | InventoryScreenData.Build exits early without InventoryPart; HP formatting uses Max-or-Value, MP is scalar, AV/DV use CombatSystem. Preserve those behaviors. |
| Weight can safely be cached by inventory count | InventoryPart.GetCarriedWeight reflects mutable weight/stack/liquid/equipment state. Continue authoritative weight and defense queries; remove item display, action gathering, category sorting and paperdoll work. |
| Existing log/status cache is in this fix | Sidebar already has log scratch lifetime and status hashing contracts. Do not expand this bounded change into whole-sidebar caching without complete revision sources. |

References read: CLAUDE.md, ADVERSARIAL_TESTING.md, PERF-FOUNDATION.md,
STEAM-DECK-PERFORMANCE-AUDIT.md finding 1, the three owned implementations and
existing tests, HotbarSnapshot, ActivatedAbility/ActivatedAbilitiesPart,
GrimoireTooltipData, InventoryScreenData, InventoryPart, CombatSystem,
InventorySystem, PerformanceDiagnostics and ZoneRenderer lifecycle call sites.

## Milestones and performance approach

1. Author existing-API work-count tests: equal hotbar render has zero clears and
   preserves tilemap contents; changed presentation repaints; explicit clear and
   camera disable/re-enable recover. Sidebar performs no GetInventoryActions
   events while direct InventoryScreenData.Build still does.
2. Root records RED. Implement hotbar exact comparisons with a defensive copy,
   background lifetime gate and granular slot/text repaint; bounded per-slot
   hotbar metadata plus immutable cached snapshots. Implement direct sidebar
   vital queries, preserving null/missing inventory and computed defenses.
3. Root records GREEN. Run cold-eye checks and dedicated adversarial cases for
   mutable snapshots, direct ability mutations, sparse/remapped slots, player
   replacement, cooldown changes, lifecycle recovery and direct vital changes.
4. Root performs integrated verification. Report work-count evidence separately
   from CPU/allocation/GPU timing. No Steam Deck frame-time claims without capture.

Performance foundations: validate-on-use caches; no LINQ/reflection in hot path;
no shared mutable snapshot collection; fixed ten-slot metadata cache; no
unbounded dictionaries keyed by entity or arbitrary display strings.

## Scope divergences

| Audit recommendation | Bounded implementation rationale |
| --- | --- |
| Invalidate/cache entire sidebar snapshot | Deferred whole-snapshot caching: logs, thoughts, focus, effects, stats and nested inventory fields do not expose one complete revision. Direct authoritative vitals eliminate the largest avoidable inventory UI workload without stale-state risk. |

## Implementation log

- Plan and source/API verification recorded before production edits.
- Native focused RED: root reports 5/7 failed in expected redundant render,
  snapshot reuse and inventory action work assertions; lifecycle Clear and
  direct-vitals correctness passed. Receipt:
  Docs/Verification/SteamDeckPerformance/2026-10-10/hud-red.json.
- Seven focused tests and 23 dedicated adversarial cases authored using existing
  APIs before production changes. All 30 passed native execution.
- Root completed the pre-change profile and explicitly released production edits.
- Hotbar render retains a defensive slot copy and compares all 13 slot fields plus
  three text fields. Unchanged content returns before any tilemap operation.
  Changes clear only the header row, summary row or affected ordinary slots via
  fixed blank buffers; arbitrary sparse/reordered slots use an ordered replay.
- Background is painted once until Clear/camera-unavailable lifecycle invalidation.
  First use still clears preexisting supplied maps. Public Clear retains its
  unconditional native clear semantics. Root integrates Render(null, camera) in
  ZoneRenderer's unavailable-camera branch to avoid repeated lifecycle clears.
- HotbarStateBuilder validates every slot each call, retains only ten slots of
  derived name data, and clones/wraps scratch data only when output changes.
  Equal output reuses the prior snapshot; direct public mutations remain visible.
- Sidebar queries only displayed stats, authoritative AV/DV, weight/capacity and
  currency. It preserves case-insensitive legacy HP/LV labels, scalar MP,
  HP max fallback and missing-inventory behavior.
- Static diff whitespace check passed. No Unity tools run by the HUD agent.
- Independent rendering-agent cold-eye review covered all three HUD production
  files, InventoryScreenData parity, mutable slot retention and disabled-camera
  clear/recovery; no actionable regressions found. Remaining focus/log/status
  construction and authoritative weight work are documented limits.

## In-phase self-review

- 🟡 Resolved during pre-implementation review: storing a caller's slot list would
  miss in-place mutation. Renderer copies slot values; builder exposes read-only
  copies that cannot be mutated through an array/list downcast.
- 🟡 Resolved during lifecycle review: leaving ZoneRenderer's disabled-camera
  branch on explicit Clear would still issue two clears every disabled frame.
  Root has the exact transition-aware Render(null, camera) integration patch.
- 🔵 Stat.Value clamps to Max. Adversarial Max=0/-1 expected results were corrected
  to 0/0 and -1/-1 before implementation, rather than misclassifying old behavior.
- Cold-eye review: cache equality covers all HotbarSlotSnapshot fields; null,
  enable/disable, Clear/repaint and sparse/remapped slot paths are symmetric.
  No hash-only acceptance, mutable retained builder lists or per-entity caches.
- Native GREEN: 30/30 new cases and 9/9 existing companion cases passed in root
  mixed run job 3431f8b33958478484d61a9a1e07f26f. Receipt:
  Docs/Verification/SteamDeckPerformance/2026-10-10/mixed-green-1.json.
  Other mixed-run failures belonged to separate tracks, not the HUD cases.
- Root's broad 1,742-case native run, job e2b7486c6ca147138ec56075d2a974eb,
  also passed all HUD and settings cases. Its five failures belonged to other
  tracks; this records the bounded HUD acceptance, not all-suite GREEN.
- 🧪 No measured Deck hardware performance claim. No new live screenshot or
  visual-readability claim from this agent.
- ⚪ No save format or gameplay rule changes; no Qud parity claim.
- ⚪ Existing sidebar log-scratch and status-hash contracts remain unchanged.
  Weight still performs its authoritative scan and allocation. Whole-sidebar
  zero-allocation/cache behavior is deliberately not claimed.

Adversarial coverage: 23 cases probe mutable public state, source metadata, name
boundaries, cross-player cache replacement, pending/unbound mapping, retained
snapshot stability, nonselected cooldowns, mutable caller arrays, granular paint,
null camera recovery, missing inventory and HP bounds. No new serialized state,
parser syntax, probability or cross-actor gameplay transaction is introduced;
those taxonomy surfaces do not apply. All 23 adversarial cases passed. Zero failures establish only these bounded
contracts, not absence of all possible interactions.

## Files changed

- Docs/STEAM-DECK-PERF-HUD.md — plan, verification and results.
- Assets/Scripts/Presentation/Rendering/HotbarRenderer.cs — exact content gate,
  defensive cache, granular painting and lifecycle-aware invalidation.
- Assets/Scripts/Presentation/Rendering/HotbarStateBuilder.cs — bounded metadata
  reuse and immutable snapshots validated against live visible ability values.
- Assets/Scripts/Presentation/Rendering/SidebarStateBuilder.cs — direct displayed
  vitals without building inventory item/action/equipment/paperdoll data.
- Assets/Tests/EditMode/Presentation/Rendering/SteamDeckHudPerformanceTests.cs
  (+ fresh 32-hex meta) — seven existing-API work/correctness contracts.
- Assets/Tests/EditMode/Presentation/Rendering/SteamDeckHudAdversarialTests.cs
  (+ fresh 32-hex meta) — 23 cache, mutation and lifecycle boundary cases.

Can verify (script-observable): native RED assertions above; production control
flow, cache inputs, defensive ownership and clean static diff. Native GREEN verifies
tile work suppression, action-event exclusion and immediate updates in the fixtures.

Cannot verify (visual/feel): Steam Deck CPU/GPU latency, controller feel, HUD
readability during resize or pause/resume, or player-perceived smoothness. Root's
native Mac editor capture is development evidence, not Deck hardware evidence.
