# Throwable Consumables — Tonics Shatter on Impact

**Status:** shipped
**Branch:** `feat/throwable-consumables`
**Plan ref:** `Docs/CONTENT-ROADMAP.md` Tier 2 → Throwable consumables

## Goal

Make every tonic with a `HasThrowablePayload()` shatter on impact when
thrown, applying its effect in a 3×3 area around the landing cell.
Closes the "drink for self / throw for battlefield control" symmetry
that the elemental tonics (just shipped) opened up.

## User-visible invariant

"When a player throws a tonic, the bottle shatters wherever it lands.
Every creature within radius 1 of the impact cell receives the
tonic's effect. The bottle does not remain on the ground."

## Scope

In:
- `ThrowItemCommand.cs` — extend the three impact paths (direct hit /
  miss / wall hit) so all three shatter tonics, with AOE.
- `TonicPart.ApplyTo` calls per-creature in radius 1.
- New tests covering the three impact paths + AOE + counter-check
  (non-tonic items still ground-land).
- New showcase scenario: `ThrowableTonicsShowcase`.

Out:
- Other throwable categories (food, runes, weapons) — out of scope.
- Friendly-fire policy — for now AOE hits everyone in radius including
  the thrower if standing close. Realistic; tightens player risk.
- Visual shatter VFX — uses existing `ThrownObject` projectile theme.
  AOE shimmer / burst overlay deferred unless playtest demands it.

## Verification sweep (complete — no false premises)

| Premise | Status | Source |
|---|---|---|
| `ThrowItemCommand.TryApplyThrownTonic` exists for direct-hit case | ✅ | `ThrowItemCommand.cs:330-345` |
| `TonicPart.HasThrowablePayload` returns true for healing / stat / CureTonic / StatusTonic | ✅ | `TonicPart.cs:73-79` |
| `TonicPart.ApplyTo(target, user, zone, rng, consumeItem, showUseMessage)` is the canonical single-target apply | ✅ | `TonicPart.cs:81-121` |
| `LineTargeting.TraceFirstImpactToTarget` returns `ImpactCell`, `LastTraversableCell`, `HitEntity`, `BlockedBySolid` | ✅ | `ThrowItemCommand.cs:142-149` |
| Miss path leaves item on ground via `zone.AddEntity` | ✅ confirmed gap | `ThrowItemCommand.cs:194-199` |
| Tonic blueprints use `Drink: true` not `Throw` action — no UI change needed | ✅ | `Objects.json:480 etc` |
| `Zone.GetCell` returns null on out-of-bounds; safe for AOE iteration | ✅ | `Zone.cs:49-54` |
| Direct-hit path consumes item via `consumedOnImpact = true` (skips `zone.AddEntity`) | ✅ | `ThrowItemCommand.cs:158-164,194` |

**No corrections required.** Implementation is one helper + three call-site swaps.

## Design

### AOE pattern: radius-1 (3×3) around impact

```
. . .       . . .       . . .
. T .  vs.  . X T   or  . T X
. . .       . . .       . . .
```

Where T = thrower's intended target, X = actual impact cell. Either
way, every creature in the 3×3 around X gets the tonic effect.

Radius 1 chosen because:
- Single-cell-only is functionally identical to current direct-hit
  case (boring).
- Radius 2 (5×5) makes throwing a tonic too dominant for one action.
- Radius 1 preserves the per-attack target-pick value while adding
  battlefield control on miss.

### Three impact paths

| Trace result | Landing cell | AOE center | Item consumed? |
|---|---|---|---|
| `HitEntity != null` (direct hit) | `trace.ImpactCell` | hit cell | ✅ yes |
| `BlockedBySolid` (wall hit) | `trace.LastTraversableCell` | last traversable | ✅ yes |
| else (miss) | `trace.ImpactCell` ?? `actorCell` | landing cell | ✅ yes |

All three paths set `consumedOnImpact = true` after applying AOE, so
the item never re-enters the zone via `zone.AddEntity`.

### New helper: `ApplyTonicAoe`

```csharp
private static void ApplyTonicAoe(Entity actor, Entity item, Cell center, Zone zone, Random rng)
{
    var tonic = item.GetPart<TonicPart>();
    if (tonic == null) return;
    int hitCount = 0;
    for (int dx = -1; dx <= 1; dx++) {
        for (int dy = -1; dy <= 1; dy++) {
            var cell = zone.GetCell(center.X + dx, center.Y + dy);
            if (cell == null) continue;
            // snapshot occupants (apply may dispatch events that mutate the cell)
            for (int i = 0; i < cell.Objects.Count; i++) {
                var occupant = cell.Objects[i];
                if (occupant == null) continue;
                if (!occupant.HasTag("Creature")) continue;
                tonic.ApplyTo(occupant, actor, zone, rng, consumeItem: false, showUseMessage: false);
                hitCount++;
            }
        }
    }
    if (hitCount == 0)
        MessageLog.Add($"{item.GetDisplayName()} shatters with no effect.");
    else
        MessageLog.Add($"{item.GetDisplayName()} shatters, splashing {hitCount} target{(hitCount > 1 ? "s" : "")}.");
}
```

### `TryApplyThrownTonic` becomes `ApplyTonicShatter` (renamed for clarity)

Old (single-target): only fires on direct hit, applies to one creature.
New (AOE): always fires for tonics, applies to every Creature-tagged
entity in the 3×3 around impact. Returns true if tonic was thrown
(consumed).

## Sub-milestones

### M1 — AOE helper + always-shatter on direct hit
- Add `ApplyTonicAoe` helper.
- Direct-hit path: replace single-target `TryApplyThrownTonic` call
  with `ApplyTonicAoe` around `trace.ImpactCell`.
- RED tests: AOE hits adjacent creatures; counter-check non-adjacent
  creature isn't hit.

### M2 — Miss-path shatter
- Miss path detects tonic, calls `ApplyTonicAoe` at landing cell, sets
  `consumedOnImpact = true`.
- RED tests: missed throw shatters; item not on ground; creatures in
  cell get effect.

### M3 — Wall-hit shatter
- Wall-hit path detects tonic, calls `ApplyTonicAoe` at
  `LastTraversableCell`.
- RED tests: thrown-into-wall shatters at last traversable; item not
  on ground.

### M4 — Counter-checks + scenario
- Counter-check: non-tonic items (rocks, weapons) still ground-land
  unchanged on miss.
- Counter-check: stack of tonics — only one consumed per throw (stack
  count decrements correctly).
- New scenario: `ThrowableTonicsShowcase` — player + 4 weak NPCs in a
  2×2 cluster, player has 5 elemental tonics. Throw at center, all 4
  get hit. Manual playtest visual.
- Scenario smoke test added.

## Test plan (≥12 tests)

1. **Tonic direct hit AOE — adjacent creature gets effect.** RED.
2. **Tonic direct hit — far creature does NOT get effect.** Counter-check.
3. **Tonic miss to empty cell — shatters; item not in zone after.**
4. **Tonic miss to empty cell — creatures in radius 1 get effect.**
5. **Tonic miss — empty 3×3 logs "shatters with no effect".**
6. **Tonic wall hit — shatters at last-traversable; item not in zone.**
7. **Tonic wall hit — creatures at last-traversable + adjacent get effect.**
8. **Non-tonic thrown into wall — still lands as ground item.** Counter-check.
9. **Non-tonic missed — still lands as ground item.** Counter-check.
10. **Friendly creature in AOE — receives effect.** (Documents friendly-fire policy.)
11. **Multiple creatures in AOE — all get effect.**
12. **Stack of tonics: throw one — stack decrements; remaining tonics not consumed.** Counter-check (stack-aware).

## Performance section

Per `CLAUDE.md` Performance triggers:
- ❌ No render hook (status effects plumb sidebar / cell dirty already)
- ❌ No hot-path allocations (one-shot throw action)
- ❌ No new cache
- ❌ No new MonoBehaviour with Update
- ❌ No per-frame / per-turn event listener

The 3×3 AOE iteration is O(9 cells × C objects/cell × 1 ApplyTo call),
bounded and cheap. Does not require a Performance section.

## Pre-flagged self-review

- 🟡 **AOE radius is hardcoded** (1). If we need radius-2 tonics in the
  future, lift to a `TonicPart.ThrowRadius` blueprint param. Not needed
  for the current 6 tonics.
- 🔵 **AOE shimmer FX** (small ring-wave around impact cell) would
  improve readability. Defer until visual playtest shows confusion.
- 🔵 **Friendly-fire policy** — current design hits the thrower if in
  AOE. Mitigates degenerate "stand on top of enemy and throw at self"
  trick but adds learning cliff. Watch playtest.
- ⚪ **Net change is small** — `TryApplyThrownTonic` removed, three
  paths in `Execute()` updated, one helper added. ~30 LOC.

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| RED tests | ✅ | 8 expected failures (5 NRE on missing AOE + 3 explicit on missing shatter); 4 baseline tests passed (counter-checks for HealingTonic/non-tonic still GREEN) |
| GREEN impl | ✅ | `ApplyTonicAoe` helper + 3 impact paths shatter; `TryApplyThrownTonic` removed |
| Counter-check fixes | ✅ | (1) AOE test layout: snapjaws repositioned off the trace path; (2) stack-test: discovered `Item` blueprint inherits StackerPart so `AddPart(new StackerPart)` was creating a duplicate — fixed by mutating existing one |
| Showcase scenario | ✅ | `ThrowableTonicsShowcase` — 5 snapjaws + 5×elemental tonics; menu entry priority 111 |
| Smoke test | ✅ | added to `ScenarioCustomSmokeTests` |
| Self-review | ✅ | All 🟡 / 🔴 clear; 🔵 hardcoded radius noted, deferred |
| Test results | ✅ | 12/12 ThrowableTonic + 31/31 scenario smokes + 378/378 broader regression |
| Merged to main | ✅ | this commit |
