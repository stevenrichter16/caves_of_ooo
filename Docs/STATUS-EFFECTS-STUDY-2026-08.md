# Status-Effect System Study — 2026-08-15

> Trigger: "Jet Blast wets the tiles in front of me, but look mode says
> nothing about water on the tile." Commissioned as a full study of the
> status-effect system: uses, inconsistencies, intricacies, architecture
> quality, real gameplay usefulness, and whether to redesign.
>
> Method: 9-agent workflow — 7 parallel surface readers (substrate,
> concrete inventory, application paths, tile layer, UI surfaces,
> gameplay consumers, prior audits), then an adversarial bug verifier
> and an architecture critic who spot-checked 6 load-bearing claims
> against source before rendering a verdict. All claims below carry
> file:line citations verified this session.

---

## 0. TL;DR

- **The Jet Blast bug is CONFIRMED and is not a one-off.** Jet Blast
  writes a *tile-layer* water coating (`ZoneTileStateSystem.WriteCoating`)
  — a status universe that **no text UI queries at all**. The map paints
  a blue `~` for it (ZoneRenderer.cs:1052-1059), so the player sees
  water while look mode says "empty ground" (LookQueryService has zero
  `TileState` references). Oil slicks, written charge, tile heat/cold,
  ember residue, and smoke/steam clouds are all equally unnameable.
- **Verdict: each subsystem is individually well-built; the composition
  is incoherent.** There are FOUR status universes (entity effects /
  ObjectStatusMatrix / tile layer / ApplyHeat-thermal) with four type
  systems and **no unified query surface and no mandatory application
  surface**. The seams — not the substrates — are where every bug in
  this study lives.
- **Gameplay usefulness: real but one-directional and under-communicated.**
  Wet/Frozen/Electrified/Burning are genuinely load-bearing (the
  soak→shock grammar is wired end-to-end with real payoffs), but the UI
  never previews any of it, the AI never plays it back at you, one
  advertised ladder (Charred) is unreachable in normal play, and a third
  of the authored resonance table (6 of 9 riders) is dead data.
- **Recommendation: do NOT merge the universes.** Ship **Option 1 (a
  unified read-only cell-status façade, ~1-2 agent-days)** first — it
  fixes the Jet Blast class of bug wholesale — then **Option 2 (a
  mandatory application façade, ~4-6 agent-days)**, which converts the
  matrix from a convention into an invariant with a test that fails on
  bypass. Explicitly reject the full-unification Option 3.

---

## 1. The reported bug, pinned

**Root cause (verified independently twice):** tile state is written and
rendered but never queried by any text surface.

- Write path (works): `Hydromancy_JetBlast.WetTheGround`
  (Hydromancy_JetBlast.cs:156-164, also per-target :130-131 and on the
  miss path :85) → `ZoneTileStateSystem.WriteCoating`
  (ZoneTileStateSystem.cs:151-157) → sparse `List<Layer>` per cell
  (ZoneTileState.cs:129-140). **No entity is created anywhere in this
  chain.**
- Reader inventory (exhaustive grep): TileReactionSystem, 
  TilePropagationSystem, ZoneRenderer.PaintTileStateMark. **Zero text-UI
  callers.**
- Look mode is entity-only by construction: `BuildSnapshot`
  (LookQueryService.cs:13-81) reads `cell.GetTopVisibleObject()` /
  `GetVisibleObjects` and entity effects only. An entity-less wet cell
  reports **"You see empty ground."** while the map paints blue `~`.
- The interact menu (`'c'`) has the identical gap
  (WorldInteractionSystem.cs:45-63, 121-157).
- The asymmetry that makes it feel random: **ConjureWater spawns a
  WaterPuddle ENTITY** (look-visible); **JetBlast writes only tile
  state** (look-invisible). Same tree, opposite visibility.
- The renderer's own docstring promises an "Inspect Mode (P8)" for the
  full layer list (ZoneRenderer.cs:1025) — **never implemented** (grep:
  no InspectMode anywhere). The Scrape economy APIs
  (`CoatingTurns`/`CountLayers`/`Clear`) also have zero production
  callers.

**Minimal fix (verified viable, single point):** both look mode and the
sidebar FOCUS panel route through `LookQueryService.BuildSnapshot`, and
`zone` is already a parameter. One added details line from
`zone.TileState.Get(x, y)` ("On the ground: water (5 turns), embers…")
covers both surfaces. Two traps the fix must handle:

1. **Permanent coatings**: river/pool projections use
   `Turns = int.MaxValue` (ZoneTileState.cs:47; Zone.cs:468) — render
   without a turns suffix or it prints "water (2147483647 turns)".
2. **Fog gating**: the renderer refuses to reveal tile state through fog
   (ZoneRenderer.cs:1031-1037); the text line must apply the same gate.
3. Coatings **coexist** (water + oil on one tile is by-design,
   ZoneTileState.cs:149-155) — join ALL layers, not the first.

Bonus finds from the verification pass: the unreachable dead string
"You notice lingering traces here." (LookQueryService.cs:192-194) is the
natural home for this line; non-ember residues are invisible even on the
MAP (renderer checks the literal id `"embers"` only — first new residue
id ships pre-broken); ice coating renders as the same blue `~` as water;
clouds are lowest glyph priority so they're often invisible even on the
map.

---

## 2. The system map — four universes and a half

| # | Universe | Identity scheme | Gate | Decay clock |
|---|---|---|---|---|
| A | **Entity effects** (`Effect` + `StatusEffectsPart`) | CLR type (`HasEffect<T>`) | `CanBeAppliedTo` + BeforeApply event veto — **no material/creature gate at all** | TurnManager Begin/EndTurn per entity |
| B | **ObjectStatusMatrix** (scenery gate) | Type → material-tag predicate | Fail-closed table; creatures pass through | n/a (advisory gate, not a store) |
| C | **ApplyHeat / ThermalPart** | continuous temperature (floats) | vetoes live in the event chain (TryIgnite wet-suppression, Combustibility) | material sim tick |
| D | **Tile layer** (`ZoneTileStateSystem`) | **strings** ("water", "oil", "embers", "smoke") + 0-2 int energy channels | none (WriteCoating is unconditional) | player-turn only (InputHandler.cs:918) |
| E½ | Two parallel **reaction engines**: MaterialReactionResolver (entity-side JSON) vs TileReactionSystem (tile-side JSON) | disjoint schemas, disjoint condition vocabularies | — | — |

The A↔D bridge is a closed 4-case switch (`TileReactionSystem.MakeEffect`,
:388-398). Wet tile → wet stander, burning entity → tile oil, cloud → gas:
all missing, each requiring a bespoke Type↔string mapping nobody owns.

### The substrate itself (universe A) is genuinely good

Full detail in the reader notes; highlights: StatusEffectsPart
force-inserts at `Parts[0]` so effect vetoes run first; forced apply
bypasses `CanBeAppliedTo` but is still event-vetoable; `OnTurnStart`
damage ticks land BEFORE the AllowAction scan (poison ticks while
stunned); duration cleanup is a separate pass; a removal-cause taxonomy
(`duration_expired` / external / save_succeeded / owner_died) feeds diag;
`JustApplied` prevents same-turn double-ticks; `BeforeMove` is explicit
defense-in-depth. This is a solid, Qud-shaped substrate with real
observability.

Substrate quirks worth knowing:
- **OnStack polarity reads backwards**: `true` = absorb incoming,
  `false` = ADD A DUPLICATE instance. `RecruitedEffect.OnStack => false`
  (RecruitedEffect.cs:138) therefore stacks two RecruitedEffects on
  re-recruit — either a live dupe bug or a trap for the next reader.
- Absorbed stacks skip the entire downstream chain — no diag, no
  event, no aura refresh. Stack-refreshes are invisible to observability.
- **F18 (audit)**: lazy `EnsureStatusEffectsPart` front-insert during a
  live `FireEvent` index-walk shifts the iterator — the confirmed
  double-ApplyHeat landmine (Entity.cs:255-262 + StatusEffectsPart.cs:25-30).

---

## 3. Current uses — the payoff map

**Load-bearing** (many real consumers, resistance-routed):
- **Wet — the grammar's noun** (~12 consumers): shock charge ×2, freeze
  ×1.5, fire suppression >0.35 moisture, Thunderclap ×2, Overload
  chain-through, spendable by ALL five rite elements, ScaldingVeil
  hard-requires it, RenderedSteam pair-detonates it.
- **Frozen**: hard action-lock (strongest single payoff in the game),
  BrittleStrike +50%, Cryomancy root +25%, three element spends,
  brittle-shatter, trade gate.
- **Electrified**: DoT + contact stun + conductor chaining + GroundStrike
  +50% + Galvanism root + the only interpreted rite riders (StormAnvil's
  Stun/Arc/Shatter).
- **Burning**: DoT + spread + Pyromancy root + Pyroclasm (the game's one
  consume-status-for-damage verb) + rite spends + trade gate.

**Medium**: Acidic (root + Etch + own DoT, but cross-element inert — H4).
**Thin**: Charred (two payoff skills, no reliable applier — H1).

**The grammar being taught**: soak→shock, soak→freeze, soak→fireproof
(counter-grammar), soak+burn→steam, freeze→shatter, burn→stoke/detonate,
anything→rite currency (quadratic multiplier `1 + ΣMult + 0.25n²` makes
stacking super-linear on purpose), anything-negative→Shank penetration.

**The seven holes (all verified):**
- **H1 — Charred is unreachable in normal play.** Its only appliers are
  fuel-exhaustion (requires FuelPart — held by exactly 5 blueprints, all
  scenery/items, zero creatures) and one throwable tonic. Two whole
  skills (Cinder, Charsplit) pay off a state whose advertised loop
  ("Burning expires → Charred → Charsplit") cannot happen on creatures.
  Charred is also anti-fire intrinsically (Combustibility ×0.3), cutting
  against its own fire-payoff fantasy, and absent from Resonance.json.
- **H2 — Six of nine authored riders are dead data.** Only StormAnvil
  interprets Stun/Arc/Shatter; Flare, Steam, Thaw, Encase, Corrode,
  Spread appear in no C# switch anywhere.
- **H3 — No resonance preview UI.** `Preview`'s docstring promises "the
  targeting UI (SM12) previews the payoff" — zero Presentation callers.
  Players do the multiplier math blind.
- **H4 — Acid is cross-element inert** (the unimplemented "Spread" rider
  is exactly the missing acid+wet interaction).
- **H5 — The AI never plays the grammar.** No file in Gameplay/AI reads
  any elemental status or casts any skill/rite. Enemies neither exploit
  a soaked player nor flee while burning. Status pressure on the player
  comes only from environment (traps, gas, tiles, liquids, on-hit
  weapons).
- **H6 — Wet is not TYPE_NEGATIVE** — rites spend it but Shank doesn't
  count it and EvasiveRoll won't clear it. Defensible, but asymmetric.
- **H7 — Electric DoT kill credit is null-sourced** (acknowledged 🔵 in
  ElectrifiedEffect.cs:99-105) — combo kills don't attribute.

**Honest usefulness verdict:** the system is REAL for the
player-as-attacker — the combo loop is wired end-to-end with genuine
magnitudes (+25% roots, +50% strikes, ×2 Thunderclap, ×3+ two-mark
rites, hard CC). Its actual-play ceiling is capped by: nothing previews
payoffs (H3), the enemy side never participates (H5), one ladder is
unreachable (H1), and a third of the resonance table silently does
nothing (H2). Today it functions as a solid one-directional combo system
that the game under-communicates and never turns back on the player.
(Bound: no PlayMode pacing/feel verification in this study.)

---

## 4. Inconsistencies, ranked by cost to future feature work

1. **No single "apply status to arbitrary target" entry point.** Path A
   (`Entity.ApplyEffect`) is completely ungated; the matrix is optional.
   Four incompatible idioms coexist: Thunderclap's dual-path (correct),
   Oilmark's direct-apply (violation), the Hitpoints-stat gate family
   (a category error — scenery HP lives on DestructiblePart, so
   `GetStatValue("Hitpoints") > 0` silently excludes scenery, sometimes
   one line below a comment acknowledging scenery exists —
   Galvanism_GroundSurge.cs:113-131), and the bolts'
   `Hitpoints > 0 || objectStillStanding` third idiom. Every new skill
   is written by pattern-matching whichever file the agent read last.
2. **No unified per-cell status query** → the Jet Blast gap, phantom-`~`
   UX, unnameable embers/clouds/charge, the unimplemented P8 IOU. Every
   tile-visible feature ships invisible-to-text by default.
3. **Type↔string identity split** between entity effects and tile layers
   blocks all four missing bridges; `"ice"` coating has no
   LiquidDefinition (null → neither conductive nor flammable, renders as
   water) — this defect in miniature.
4. **Dual reaction engines** with disjoint JSON schemas; fire-can't-melt-
   ice (audit F6) is a direct casualty.
5. **F18 lazy-part iterator shift** — doubles calibrated first-ignition
   heat doses. Cheap fix, real gameplay effect.
6. **OnStack polarity + silent absorb** (above).
7. **Tile state doesn't save (audit F7/F8)** — a status universe that
   evaporates on save/load is a genre-level contract violation for a
   persistent-world RPG, and it caps how load-bearing tile state is
   *allowed* to become.

### The violator list (application-path map, production sites)

Genuine matrix bypasses where the verdict would differ:
1. **Pyromancy_Oilmark.cs:90** — oil-coats scenery with
   `LiquidCoveredEffect`, which has NO matrix row (TryApply would refuse
   it as Meaningless). Either add the row (coating scenery for later
   ignition looks like intended grammar) or this is the clearest
   reach-past in the codebase.
2. **MaterialReactionResolver.ApplyStatusEffectByName (:289-304)** —
   data-driven, unguarded: any reaction JSON row can apply any effect
   name to any material.
3. **ThermalPart.TryFreeze (:166)** — freeze-by-cooling has no Freezable
   material check; chill a stone wall and it gets Frozen even though the
   matrix says WrongMaterial. The ignite side has a Combustibility veto;
   the freeze side has no analogue. Path-B/Path-C gate drift.
4. **MaterialPart.cs:243 electric chain** — uses its own conductor
   vocabulary (`Conductivity >= 50` OR Metal OR Conductor) vs the
   matrix's (Conductor, Metal, **Water**). Verified consequence:
   Thunderclap can electrify a WaterPuddle, but chain propagation
   refuses to pass charge INTO that same puddle.
5. **GasCryoPart.cs:67** — Frozen on anything with a Hitpoints stat,
   bypassing Freezable for future statted non-creatures.

Plus five "verdict-equivalent today" bypasses (SteamEffect, ConjureWater,
LiquidCoveredEffect, Cudgel_Hammer, BurningEffect char) that silently
encode "this effect's matrix row is `always`" at the call site — if a row
is ever tightened they will not notice.

The application-path reader also produced the **eleven rules of thumb a
new contributor needs** (which path when, fire-is-special, tags-never-
numeric-fields, removal-never-gated, tile-is-Path-D…) — currently written
nowhere. Rule zero of any fix: write these down where the agent will
find them.

---

## 5. Architecture verdict

**Each subsystem is individually well-built; the composition is
incoherent.** The deliberate decisions — two energy models meeting only
at explicit reaction boundaries, a sparse tile store with a
player-turn decay clock, a fail-closed scenery matrix — are defensible
and documented in the code's own comments. The defects are all at the
seams, and they are *forcing functions*, not one-off oversights:

1. **No unified read surface** → the Jet Blast gap is the DEFAULT
   OUTCOME of adding any tile-side state, re-decided per-surface every
   time. The renderer got a bespoke bridge; look mode didn't; the next
   feature re-rolls the dice.
2. **The matrix is a door with no wall** → gates only volunteers; four
   application idioms; capability-by-tag (matrix) and capability-by-
   simulation (ThermalPart) answer the same question from different
   data (audit F13's five shock authorities).
3. **The Hitpoints gate is a category error the type system invites** —
   there is no `CanReceiveStatus(entity, effectType)` to call, so each
   skill invents the cheapest predicate available.
4. **String universe vs type universe can't see each other** → every
   bridge is a bespoke switch built only when a playtest complains (the
   ApplyFireToTile comment literally says "Reported from play,
   2026-08-09").

---

## 6. Redesign options and recommendation

### Option 1 — Unify the QUERY surface only ("CellStatusReadout")
One read-only service `Describe(Zone, x, y)` merging entity effects (via
EffectDescriber), tile coatings/residues/energy/clouds (via a new
string→display registry that folds LiquidDefinitions in and gives
`"ice"`/`"embers"`/clouds names), and health. LookQueryService, the
interact-menu title, and the promised P8 inspect mode all consume it.
**Scope: ~1-2 agent-days. Risk: near zero** (pure additive reads).
Fixes inconsistency #2 wholesale. Strategic risk: makes the four-universe
split more livable.

### Option 2 — Query façade + mandatory APPLICATION façade
Option 1 **plus** a single `StatusApplication.TryApply(target, effect,
source, zone)` internalizing: creature pass-through, matrix lookup, and
a DestructiblePart-aware aliveness check (killing the Hitpoints idiom).
Enforced not by doctrine but by an EditMode reflection test that FAILS
on any production `Entity.ApplyEffect` call site outside the façade
(with a self-buff whitelist). The application-path map is the migration
checklist (~40 sites). Prerequisite commit: fix F18. Riders: matrix rows
for LiquidCoveredEffect + the F14 disagreements; diag on every façade
refusal. **Scope: ~4-6 agent-days. Risk: moderate** — some behavior
changes are *corrections* (scenery legally receiving Electrified from
GroundSurge is a balance change to playtest, not just a refactor).

### Option 3 — One status substrate for entities AND tiles
Merge tile state into the effect model. **Rejected.** It pays a
multi-week, 6600-test-churn price to demolish walls that aren't the
problem, and destroys deliberate, load-bearing design (coarse 0-2 tile
energy, sparse cost model, player-turn decay — the code argues for these
convincingly). It also solves bridge-*capability* but not
bridge-*policy* (you still must decide whether a burning creature
ignites oil).

### Recommendation

**Option 1 first, then Option 2. Reject Option 3.** The decisive
argument for this codebase specifically: **every future skill is written
by an AI agent pattern-matching precedent.** A single correct entry
point plus a test that fails on bypass converts "the agent copied the
wrong idiom" from a silent bug into a RED test — worth more than any
amount of doctrine in CLAUDE.md. Option 1's display registry is also the
exact artifact Option 2's bridges and any future tile feature need
anyway.

Defer with a written decision record: the dual reaction engines (unify
only when a content milestone actually needs a cross-engine rule) and
tile save/load F7 (real, genre-relevant, independently shippable).

Sequenced estimate at agent pace: Option 1 ≈ 1-2 days; F18 + façade +
migration ≈ 4-6 days; two user checkpoints (post-query-façade playtest;
post-migration balance pass on scenery now legally receiving statuses).

---

## 7. Quick-fix shortlist (independent of the redesign)

Each is small, self-contained, and worth doing regardless of which
option ships:

1. **The Jet Blast fix** — tile-state detail line in `BuildSnapshot`
   (§1; handles Permanent, fog, multi-layer). Also fixes oil, charge,
   embers, clouds in one stroke. This IS the seed of Option 1.
2. **F18** — eager StatusEffectsPart in EntityFactory or
   snapshot-iterate in FireEvent.
3. **RecruitedEffect.OnStack** — decide dupe-block vs stack; current
   `false` adds duplicates.
4. **MaterialPart.cs:243 conductor vocabulary** — ask the matrix, like
   Overload already does.
5. **ThermalPart.TryFreeze** — add the Freezable check (mirror the
   ignite side's Combustibility veto).
6. **Renderer residue check** — generalize the literal `"embers"` so the
   first new residue id doesn't ship pre-broken.

## 8. Honesty bounds

Code-observable claims only, all file:line-cited and spot-checked (6
load-bearing claims re-verified against source by the critic; the bug
diagnosis re-derived independently by a second agent). NOT verified:
live-play pacing/feel of the combo loop, whether NPCs survive long
enough for two-mark setups, FX readability. The usefulness verdict (§3)
is a code-truth ceiling, not a playtest.
