# Cold ↔ Tile Bridge — cold spells reach the ground

> Trigger (2026-08-15, live play): "a tile with nothing on it but water
> does not have the water frozen when applying Rime Grip." Investigation
> in the transcript; this doc is the plan, its critical review, and the
> implementation log.

## 1. Diagnosis (verified from source, all lines re-read this session)

Two waters exist. ConjureWater spawns a **`WaterPuddle` entity**
(`Liquid,Water` + ThermalPart) — Rime Grip CAN target it via
`SkillLine.Collect` → `IsElementalTarget` (has MaterialPart), and Water
is Freezable, so `FrozenEffect` lands. Jet Blast writes a **tile
coating** (`ZoneTileStateSystem.WriteCoating("water")`) — no entity
exists, so `SkillLine.Collect` returns nothing and Rime Grip refuses
("closes on nothing", free refusal, `Cryomancy_RimeGrip.cs:57-62`).

The tile universe already owns BOTH halves of the freeze:
- `freeze_water` (Reactions.json): water coating + tile cold ≥1 →
  ice coating (4 turns), consumes 1 cold, `OccupantEffect: Frozen`
  (creatures only — `TileReactionSystem.cs:352-356`).
- `melt_ice`: ice + tile heat → water (6 turns).

**The missing piece is the writer.** `grep AddCold` across
Gameplay/Skills: zero. Only `TileStateSourcePart` (terrain seeding)
writes tile cold. Fire has `ApplyFireToTile(s)` (4 spells call it) and
lightning writes charge (2 spells); there is no `ApplyColdToTile` twin.
So `freeze_water` is authored, tested, shipped — and unreachable by any
spell. Same class as the `cold_plus_crystal` finding: content the code
cannot reach.

History: `ApplyFireToTile`'s docstring says it exists because "a
flamethrower could not light an oil slick — reported from play,
2026-08-09." This report is the cold-side twin of that event.

## 2. Plan

### 2.1 Substrate — `ZoneTileStateSystem.ApplyColdToTile(s)`
Exact twin of `ApplyFireToTile(s)`: `AddCold(zone,x,y,1,source,ability)`
(+ `ResolveAfterAbility` in the plural form). Every cold ability routes
through it "so a sixth one cannot quietly forget" (the fire docstring's
own rationale). Diag: `AddCold` already emits `tile/TileWritten`
(energy/cold).

### 2.2 Rime Grip — freeze the ground it passes over
Mirror Jet Blast's miss semantics ("a miss is not a nothing",
`Hydromancy_JetBlast.cs:81-87`):
- On the entity-hit path: also cold the **target's cell** (the grip
  seizes the thing AND ices the ground under it) → `freeze_water` fires
  on the cast via `ResolveAfterAbility`.
- On the no-entity path: cold every cell along the line
  (`SkillLine.CollectCells`, the same helper Jet Blast uses), resolve,
  and **consume the cast** (return true) instead of the free refusal —
  IF at least one cell in the line was actually written. A cast into
  bare dry ground stays a free refusal (nothing to freeze) — this keeps
  the "refusal = free" contract for the truly-nothing case and matches
  the player's mental model: Rime Grip at a wet tile freezes it.

  Message: "'s rime grip freezes the ground." on the ground-only path.

### 2.3 The other cold spells (same commit, one pass)
- `Cryomancy_RimeNova` (self-centered radius 2): `ApplyColdToTiles` over
  the radius — mirrors Conflagration's ThermalPart sweep being the
  entity half; the tile half was missing.
- `Cryomancy_ChillDraft` (radius 1 −100J sweep): `ApplyColdToTiles`
  over the 3×3.
- `Cryomancy_IceLance` (projectile): cold the landing cell — the
  target's cell if hit, else the last cell of the FX path.
- `Cryomancy_ColdSnap` (radius 2 hobble): cold the radius.
- `Cryomancy_Frostbind` / `GlacialWall` / `Hibernate` / `BrittleStrike`
  / `FrostRetort`: **deliberately not** — Frostbind/Hibernate are
  creature-state effects, GlacialWall spawns entities, BrittleStrike/
  FrostRetort are melee/passive. Cold-to-ground is for spells whose
  fantasy is "cold pours out into the world."

### 2.4 Content
`ice.json` already exists (step 2). `freeze_water`/`melt_ice` exist.
No content change.

### 2.5 Tests (RED first)
1. `ApplyColdToTile` writes cold=1 + emits `tile/TileWritten`.
2. Rime Grip at an empty wet tile → coating becomes `ice`, cast
   consumed (cooldown > 0), message logged. **The reported repro.**
3. Rime Grip at empty DRY ground → still a free refusal (no cooldown).
   Counter-check for the "consume only if written" rule.
4. Rime Grip hitting a creature standing on water → creature Frozen
   (existing) AND the tile under it is ice (new).
5. Rime Nova over a wet radius → every wet cell in radius is ice.
6. Melt round-trip: Rime Grip freezes → Flaming Hands (ApplyFireToTile)
   on the ice → water again. Proves the pair, end to end, through the
   spells.
7. Occupant: a creature standing on Jet Blast water when Rime Grip hits
   the ground gets `Frozen` via the reaction's OccupantEffect (0.6) —
   pinning that ground-freeze is a real CC path, not just a glyph.
8. Look-mode readout: after the freeze, `GroundLine` says "ice (4
   turns)" — one line, proves the whole chain is player-visible.

### 2.6 Live check
Play mode: Jet Blast east, Rime Grip east → pale `~` + look "On the
ground: ice (4 turns)"; then Flaming Hands the ice → water again.

## 3. Critical review of the plan (before implementation)

Attacked five ways, each against source. Three changed the plan.

**R1 — Does the freeze fire on an EMPTY tile (no occupant)?** The
plan's whole premise. VERIFIED YES: `TileReactionSystem.Apply`
(:302-333) consumes inputs and writes the output coating
unconditionally; only `ApplyToOccupants` is creature-gated. An empty
wet tile becomes ice with no one standing there. Plan holds.

**R2 — Priority + oscillation.** `melt_ice` (45) sorts AFTER
`freeze_water` (40); a tile carrying both cold and heat freezes first,
then the melt matches the fresh ice and reverses it in the same
action. `_firedThisAction` prevents infinite oscillation (each
reaction fires once per tile per action), so the worst case is one
freeze→melt round trip that lands on water — not a hang. Acceptable
and pre-existing; the bridge does not change it. Test 6 (melt round
trip) is kept but must run the melt in a SECOND action, which is what
a real Flaming Hands cast after a Rime Grip cast is anyway.

**R3 — Power-creep, the real finding.** `freeze_water`'s
`OccupantEffect: Frozen` applies `FrozenEffect(0.6)` — a HARD
action-lock — to every creature standing on a wet tile that freezes.
The plan's §2.3 wired ColdSnap (a radius-2 *hobble*, deliberately not
a lock — its own docstring: "No damage — buys turns") to cold the
ground. Over Jet Blast water that would silently upgrade ColdSnap from
"hobbles everything in 2" to "action-locks everything in 2 that is
standing in water" — the exact "froze a whole line, ends fights
outright" failure Rime Grip's docstring warns about. **CUT ColdSnap
from the ground pass.** RimeNova already applies Frozen(0.6) directly
to every creature in radius, so its ground cold adds only the ice
coating (creatures were locked anyway) — keep. ChillDraft is
utility-radius-1 with no creature CC today; ground cold there would
add CC via the reaction — **cut it too**, same reasoning: the bridge
must not hand CC to spells that were designed without it. Final
ground-cold set: **Rime Grip (single target/line), Rime Nova (already
CC), Ice Lance (single landing cell)**. Each already locks or hits its
target; the ground freeze adds board state, not new lockdown radius.

**R4 — Renderer + readout freshness.** `WriteCoating` calls
`Changed()` → renderer dirty hook (ZoneTileState.cs:156). The look
readout reads live state. Both refresh. Holds.

**R5 — Rime Grip's "consume only if written" rule.** The plan said:
on no-entity, cold the line, consume the cast if any cell was written,
else free refusal. Problem: `AddCold` on a DRY cell still writes cold
=1 to the store — "written" is not "froze anything". A dry-ground
cast would consume the cast for a fizzle. Sharpen: consume iff at
least one cell in the line held a **water coating** before the cast
(the thing the grip can grip). Dry line → free refusal, unchanged
message. This is testable and matches the player model exactly:
"Rime Grip freezes water; it does not freeze bare rock."

**R6 — Cold on the target's cell when a creature IS hit.** Kept: the
grip seizes the creature AND ices the ground under it. If that ground
was water, `freeze_water` also applies Frozen(0.6) via OccupantEffect
— on a creature Rime Grip already gave Frozen(0.5). FrozenEffect
stacks by refreshing/raising Cold (`OnStack`, verified in step 1
studies), so this is a small deepening on wet ground, coherent with
"a wet target freezes deeper" (Rime Grip's own SM6 docstring). Not a
new lockdown. Holds.

Net revisions: §2.3 shrinks to RimeGrip + RimeNova + IceLance; §2.2's
miss rule keys on pre-existing water, not on "any write"; test 6 runs
melt as a separate action. Tests 3 and 5 re-target accordingly.

> **Reader note:** §2 is the plan AS WRITTEN; §3 revised it (R3 cut
> ChillDraft + ColdSnap; R5 sharpened the miss rule to "pre-existing
> water"). §4 records what shipped, including one further divergence
> found in implementation. Read §2 → §3 → §4 in that order.

## 4. Implementation log — shipped 2026-08-15

**Status:** ✅ shipped, one commit. Tests 6662 → 6672 (+10). All green.
Live-verified in Play mode on the reported case (empty wet tile).

### 4.1 What shipped (files)

| File | Change |
|---|---|
| `ZoneTileStateSystem.cs` | NEW `ApplyColdToTile` / `ApplyColdToTiles` — exact twins of `ApplyFireToTile(s)` (singular writes cold=1, callers resolve; plural writes + one `ResolveAfterAbility`). |
| `Cryomancy_RimeGrip.cs` | No-entity branch: `CollectCells` → filter `HasCoating(water)` → if any: `ApplyColdToTiles` + "freezes the ground" + return true; dry line unchanged (free refusal). Both hit branches (seize AND shatter) call `IceTheGround(groundPos)`; `groundPos` captured BEFORE damage. |
| `Cryomancy_RimeNova.cs` | Radius sweep collects `groundCells`; `ApplyColdToTiles` over them after the ThermalPart pass. |
| `Cryomancy_IceLance.cs` | `ApplyOnHitEffect` also colds the target's cell + resolves. |
| `ColdTileBridgeTests.cs` | NEW, 10 tests (below). |

### 4.2 Tests (RED first, each; the plan's 8 + 2 found in implementation)

| # | Test | Plan § | Pins |
|---|---|---|---|
| 1 | `ApplyColdToTile_WritesColdAndEmitsDiag` | 2.5.1 | substrate + `tile/TileWritten` |
| 2 | `RimeGrip_AtAnEmptyWetTile_FreezesTheWater` | 2.5.2 | **the reported repro**: water→ice, cast consumed, message |
| 3 | `RimeGrip_AtDryGround_IsStillAFreeRefusal` | 2.5.3 (R5) | counter-check: dry line = free, no cold residue |
| 4 | `RimeGrip_HittingACreatureOnWater_FreezesBoth` | 2.5.4 | creature Frozen AND ground ice |
| 5 | `RimeGrip_ShatteringAPropOnWater_StillFreezesTheGround` | **new** | see 4.3 — the live find |
| 6 | `RimeNova_FreezesEveryWetCellInRadius` | 2.5.5 | edge + diagonal ice; radius-3 water stays water (counter) |
| 7 | `FreezeThenMelt_RoundTripsThroughTheSpells` | 2.5.6 (R2) | Rime Grip → ice → Flaming Hands (second action) → water |
| 8 | `RimeGrip_GroundFreeze_FreezesTheOccupantViaTheReaction` | 2.5.7 | ground freeze is real CC (OccupantEffect Frozen) |
| 9 | `AfterTheFreeze_LookModeSaysIce` | 2.5.8 | `GroundLine` → "ice (4 turns)" |
| 10 | `IceLance_FreezesTheLandingCell` | **new** | Ice Lance hit ices the target's cell |

### 4.3 Found in implementation — the shatter branch (live probe)

First live probe (Play mode, cast east at a chest standing on Jet Blast
water): `[before] water (6 turns) → [cast handled] True → [after] water
(6 turns) → "rime grip shatters chest!"`. The first cut iced the ground
only after the seize path; the shatter branch `return`ed early, AND a
shattered target may already be gone from the zone, so a post-damage
`GetEntityPosition(target)` could return (-1,-1). Fix: capture
`groundPos` before `RouteDamage`; `IceTheGround` on both branches.
Test 5 pins it. This is the same bug class as the plan's own R6 concern
(the ground pass must not depend on the target surviving).

Second live probe on the ACTUAL reported case (a fully empty line,
picked with the spell's own `SkillLine.Collect` predicate):
```
[before] On the ground: water (6 turns)
[handled] True
[after]  On the ground: ice (4 turns)
[msg]    you's rime grip freezes the ground.
[interact] You see the grass. (ice underfoot)
```
(An intermediate probe "east" re-hit the chest five cells away — Rime
Grip is a range-5 nearest-entity line, so that is the designed rule,
not a defect; noted so a future reader does not misread it.)

### 4.4 Scope divergence from the plan (found by the cold-eye pass)

**Ice Lance miss path — NOT shipped.** §2.3 said "the target's cell if
hit, else the last cell of the FX path." `ProjectileSpellSkillBase`
exposes only `ApplyOnHitEffect` (called only while the target still
stands, `:126-127`); there is no landing hook, and the fire twin
`Pyromancy_Kindle` does not fire the ground on hit OR miss. Adding a
landing hook is a base-class change across six projectile spells —
outside this bug's reviewed scope. Shipped: hit-only (already one step
past Kindle). Follow-up option, not a defect: `OnLanded(Point impact)`
on the base, then Ice Lance colds the impact cell on a miss and Kindle
gets the symmetric fire pass.

Consequence to know: Ice Lance that KILLS its target does not ice the
ground (same gate as its −300J chill). Rime Grip's shatter branch does
(4.3). Deliberate asymmetry: the grip is a touch on the ground under a
thing; the lance is a bolt into a body.

### 4.5 Cold-eye pass (Q1-Q4)

- **Q1 symmetry:** `ApplyColdToTile` singular does not resolve, its
  callers do — exactly as `ApplyFireToTile` / `Pyromancy_FlamingHands.cs:59-61`.
  Plural resolves once. ✓
- **Q2 consistency:** signatures, default args, diag category (`tile` /
  `TileWritten` energy/cold) identical to heat. ✓
- **Q3 counter-checks:** dry-line refusal (test 3), out-of-radius water
  stays water (test 6), Ice Lance's dry-cell case implied by the
  matrix (cold on dry ground = cold=1, decays next `Tick`, no ice). ✓
- **Q4 doc-vs-impl:** one divergence, §4.4. §2.2/§2.3 left as written
  with the reader note above rather than rewritten. ✓

### 4.6 Not done / deferred (tracked, not asked)

- Projectile landing hook (4.4).
- ChillDraft / ColdSnap stay entity-only (R3 — a design decision, not
  a gap: the reaction's OccupantEffect Frozen would hand hard CC to
  spells authored without it).
- Tile cold has no reader beyond the reaction engine — a chilled dry
  tile is invisible to the player until water lands on it. Same as
  heat today; the status-effects study §6 "tile energy readout" option
  covers both.
