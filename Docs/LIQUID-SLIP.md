# Liquid Slip — the `Slippery` flag wakes up

> Trigger (2026-08-15): after the cold-tile bridge shipped, the user
> asked what a frozen tile does to an NPC. Answer: nothing after the
> freeze-moment Frozen snap — ice is a 4-turn glyph. `LiquidDefinition.
> Slippery` has been "cosmetic till LQ.8" since the liquid plan
> (`Docs/LIQUID-COATING-SYSTEM-PLAN.md` §13.2: "zero gameplay
> consumers"). This is the LQ.8 slip half. Sticky stays deferred.

## 1. Goal

Stepping onto a slippery surface — ice, oil, gel, sundew dew, whether
it is a **tile coating** (Jet Blast water frozen, Oilmark slick) or a
**pool entity** (oil pool from a thrown flask) — risks a slip: Qud's
**slide-over** (you step on, and with some chance you are flung one
random cell). Player and NPC alike. Data-flagged: a liquid is
slippery because its JSON says so, and *how* slippery is a flat
percent in the same JSON. No new status effect; a slip is a one-shot
event.

**Decided by the user (2026-08-15):** Qud's slide, **without** the
Agility save for now — a flat `x%` chance per liquid. The save is a
documented follow-up (§7, §8 R1), and the data shape is chosen so
adding it later changes the roll, not the content.

Non-goals (this pass): the Agility save / `Stable` surefooting,
Sticky/Stuck (honey, sap), AI path-cost awareness, a Prone effect,
per-body-part footing.

## 2. Verification sweep — every claim below re-read this session

### 2.1 Qud reference (`/Users/steven/qud-decompiled-project`)

| # | Fact | Cite |
|---|---|---|
| Q1 | The slip is resolved in `BaseLiquid.ObjectEnteredCell` — on **entering** the cell, not on leaving or on a failed step. | `XRL.Liquids/BaseLiquid.cs:402-423` |
| Q2 | Gate: liquid is `SlipperyWhenFrozen && frozen` OR `SlipperyWhenWet && !wading && !Slimewalking && !frozen`; mover must have a `Body`; open volume only. | `:404-408` |
| Q3 | Roll: `MakeSave("Agility", difficulty)`; **failure ⇒ message + particle + `Move(randomDirection, Forced: true)`** — the move already succeeded; the slip is an extra, un-vetoable displacement of one cell in a random direction. Not "lose the turn", not prone. | `:408-421` |
| Q4 | Difficulty = `SlipperySaveTargetBase(5) + Amount.DiminishingReturns(0.3) − IntProperty("Stable")`, capped 24. | `:452-456` |
| Q5 | Save chance = `(21 − difficulty + StatMod) × 5%`, clamped to [5%, 95%] (natural 1 / natural 20). | `XRL.Rules/Stat.cs SaveChance` |
| Q6 | Water is `SlipperyWhenFrozen` only ("=subject.T= =verb:slip= on the ice!"); oil, gel, slime are slippery wet AND frozen; sticky is the sibling branch (`Stuck` effect). | `LiquidWater.cs:32-37`, `LiquidOil.cs:32-38`, `LiquidGel.cs:29-35`, `LiquidSlime.cs:32-38` |
| Q7 | Smart AI weights slippery cells in pathing (`GetNavigationWeight` → `max(difficulty/3, 2)`). | `BaseLiquid.cs:517-527` |
| Q8 | The forced slide re-enters `ObjectEnteredCell` in the new cell — chains are possible in Qud, bounded only by the saves. | `:421` + Q1 |

### 2.2 CoO facts

| # | Fact | Cite |
|---|---|---|
| C1 | `LiquidDefinition.Slippery` (bool) exists, loaded from JSON; **zero readers** in production code. `oil`, `gel`, `pebble-sundew-dew` are `Slippery:true`; `water:false`; `ice:false` (authored display-only by me in the status study step 2 — must flip to true). | `LiquidDefinition.cs:69`; grep; the four JSON files |
| C2 | Two liquid universes: tile coatings (`ZoneTileState.TileState.Coatings` — `List<Layer{Id,Turns}>`, no amount) and pool entities (`LiquidPoolPart{LiquidId, Volume}`). Both are keyed by the same `LiquidRegistry` id. **⚠ Corrected in implementation (§9.2): at ground level they are already ONE — `Zone.ProjectPool` mirrors every pool entity into the tile layer as a permanent coating.** | `ZoneTileState.cs:55-72`; `LiquidPoolPart.cs:41`; `Zone.cs:463` |
| C3 | Movement pipeline: `TryMoveEx` / `TryMoveTo` / `ForceMoveTo` → `zone.MoveEntity` → `AfterMove` (on mover) → `FireCellEnteredEvents` (`EntityEnteredCell` on the *other* occupants). `ForceMoveTo` skips `BeforeMove` (knockbacks must not be vetoed by stun) and only bounds-checks; callers guarantee legality. | `MovementSystem.cs:50-105, 111-154, 182-209` |
| C4 | **Re-entrancy hazard:** `FireCellEnteredEvents` uses a shared static scratch list and its comment says no listener may trigger a nested move ("switch to ArrayPool or gate re-entrance"). A slip that slides the mover **must not run inside an `EntityEnteredCell` handler.** | `MovementSystem.cs:22-29` |
| C5 | Player: `TryMoveEx` → moved ⇒ `EndTurnAndProcess()`. A blocked move spends no turn unless action-blocked. So a "stay put" slip would need new plumbing (a third outcome) to consume the turn; a **post-move** slip needs none — the turn is already spent. | `InputHandler.cs:689-768` |
| C6 | NPCs move via `MovementSystem.TryMove` (StepGoal, AIHelpers) — same pipeline, so a post-move slip covers them for free. | `StepGoal.cs:23`; `AIHelpers.cs:330-395` |
| C7 | Legal-destination rules for a forced displacement already exist: `SkillCombatHelpers.DragAlong` — in-bounds, `!cell.IsSolid()`, no other Creature; loot never blocks. | `SkillCombatHelpers.cs:302-309` |
| C8 | No `MakeSave` exists in CoO. The precedent for a data-driven percent roll with a diag trail is `GasFungalSporesPart` (`TestRng ?? _defaultRng`, `roll < chance`, payload carries chance+roll). `StatUtils.GetModifier` exists (Qud's floor((score−16)/2)) for the day the save is added. | `GasFungalSporesPart.cs:74-95`; `StatUtils.cs:16` |
| C9 | `Entity.GetIntProperty("Stable")` is available (Qud's surefooting knob) — nothing sets it today; **not used in this pass.** No flying/hover tag exists in content. | `Entity.cs:197` |
| C10 | AI has no per-cell path-cost seam (`AIHelpers` is LOS/greedy). Q7 has nowhere to land. | grep |
| C11 | Diag categories: `liquid` (32 records) and `tile` (6) exist. `TileState.Get(x,y)` is one dictionary lookup, no allocation. | `Diag.cs:120`; `ZoneTileState.cs:116-121` |
| C12 | `Cell.ParentZone` exists, `Cell.IsSolid()` exists. | `Cell.cs:18, 91` |

**False-premise found and corrected:** my earlier chat description
("X% chance the move is spent and you stay put") is **not** Qud's
mechanic. Qud's slip is a post-entry forced random slide (Q3). The
plan follows Qud's *shape* (slide) with a flat chance in place of the
save (user decision); §8 records both.

## 3. Design

### 3.1 The mechanic (Qud's slide, flat-chance)

On every **completed** move (voluntary or forced) into cell C:

1. Collect slippery liquids present in C: tile coatings whose
   `LiquidRegistry.Get(id).Slippery`, plus any `LiquidPoolPart` in
   `C.Objects` whose liquid is Slippery (and `Volume > 0`). First
   match wins — one roll per move, never two (oil coating + oil pool
   in one cell rolls once).
2. Mover must be a Creature. (Q2's `Body` gate; items and props never
   slip. Player included.)
3. Roll: `slipped = rng.Next(100) < liquid.SlipChance`. That's it. No
   stat, no property, no save. `SlipChance` is authored per liquid
   (§3.2); 0 ⇒ never (Slippery:true with SlipChance 0 is a legal
   "cosmetic-slippery" row), 100 ⇒ always.
4. On a slip: message `"<Name> slips on the <liquid>!"`, pick a
   random direction among the 8, and if that cell is legal (C7 rules:
   in bounds, not solid, no other creature) `ForceMoveTo` there.
   Illegal ⇒ the slip still happened (message + diag) but no
   displacement — Qud's `Move(Forced)` into a wall fails the same way.
5. The forced slide runs the same post-move check on its landing cell
   (Q8), **capped at `MAX_SLIDE_CHAIN = 3` per originating move** —
   an ice sheet can carry you but not forever; the cap makes tests
   bounded and guards the C4 hazard from a pathological RNG (or a
   `SlipChance: 100` liquid on a long sheet).

### 3.2 Data

`LiquidDefinition`: keep `Slippery` (bool) as the gate; add
`SlipChance` (int percent, **default 50** when absent — so a liquid
that says `Slippery: true` and nothing else is meaningfully
slippery, not silently inert). Content, not a magic constant in code.

| Liquid | Slippery | SlipChance | Note |
|---|---|---|---|
| ice | **true** (flip from false) | 50 | the reason this exists |
| oil | true | 40 | Oilmark slicks are common; a hair gentler |
| gel | true | 50 | Qud: slippery wet and frozen |
| pebble-sundew-dew | true | 30 | forage-flavour hazard, mild |
| water | false | — | never (Qud: water slips only when frozen — and frozen water is `ice` here) |

Numbers are a first tuning; the diag record makes them auditable and
the fix is a JSON edit. **Forward-compat with the deferred save:** when
the Agility save arrives, `SlipChance` becomes the *base* the save
modifies (e.g. `chance = SlipChance − 5×AgilityMod`, clamped) — the
content rows stay valid, only the roll changes.

### 3.3 Where the code lives

`Assets/Scripts/Gameplay/Turns/LiquidSlipSystem.cs` (static):
- `public static bool ResolveAfterMove(Entity mover, Zone zone, Cell landed, int chainDepth = 0)` — the whole of §3.1; returns true if a slip occurred.
- `public static LiquidDefinition FindSlipperyLiquid(Zone zone, Cell cell)` — reads the tile layer's coatings. *(Plan said "then pool entities"; dropped in implementation — pools are already projected into the tile layer, §9.2.)*
- `public const int MAX_SLIDE_CHAIN = 3;`
- `public static System.Random TestRng` — the fungal-spores pattern; default a static `System.Random`.
- `LiquidDefinition.SlipChance` (int, default 50).

`MovementSystem`: after `FireCellEnteredEvents(...)` in **all three**
move methods, call `LiquidSlipSystem.ResolveAfterMove(entity, zone,
targetCell, chainDepth)`. `ForceMoveTo` gains an optional
`slipChain` int parameter (default 0) so the slide can carry depth;
external callers (knockbacks) pass nothing and get depth 0 — a shove
onto ice can slip you once more, which is correct and delicious.

Ordering: after `FireCellEnteredEvents` returns (C4 hazard). Runs
outside any handler; the scratch list is idle when the slide's own
`ForceMoveTo` recurses.

### 3.4 Messages & readout

- Slip: `"You slip on the ice!"` / `"The snapjaw slips on the oil!"`
  (via `GetDisplayName()`, matching every other MessageLog line).
- Look mode / `c` menu ground line: append `" (slippery)"` after a
  slippery coating's name — `CellStatusReadout.DescribeLayer` reads
  the registry already; one flag. Small, and it teaches the rule.

### 3.5 Performance (required — touches the per-move path)

| Risk | Mitigation |
|---|---|
| Per move: tile lookup + coating scan | `TileState.Get` = one dictionary lookup, no alloc; coatings list is ≤2 typically. Early-out when `state == null` (the overwhelming case: unwritten tiles). |
| Per move: pool-entity scan of `cell.Objects` | Already iterated by `FireCellEnteredEvents`; a second linear pass over a handful of objects, `GetPart<LiquidPoolPart>` per object. Sparse. |
| Registry lookup per coating | `LiquidRegistry.Get` = dictionary. |
| Random direction pick | `rng.Next(8)` + a static readonly `(dx,dy)[8]` table — no allocation. |
| Diag | Gated `Diag.IsChannelEnabled("liquid")`; anonymous payload only inside the gate. |
| Recursion | Depth-capped at 3; each level is one `ForceMoveTo`. |

No new MonoBehaviour, no per-frame work, no cache.

### 3.6 Observability

`liquid` category:
- `kind=SlipRolled` — actor, payload `{ liquidId, chance, roll, slipped, chainDepth }` *(plan had a `source` field; dropped with the pool scan, §9.2)* — every roll, pass or fail (the "gate" rule). No roll ⇒ no record, so "did the game even consider a slip here?" is one query.
- `kind=Slipped` — actor, payload `{ liquidId, fromX, fromY, toX, toY, displaced(bool), chainDepth }` — the outcome, including "slipped but nowhere to go".

## 4. Sub-milestones (smallest blast radius first)

**S1 — data.** `LiquidDefinition.SlipChance` (default 50);
`ice.json` `Slippery:true`; the four JSON rows set per §3.2.
Tests: registry round-trips the field; default 50 when absent;
`water.Slippery == false`; `ice.Slippery == true`.

**S2 — resolver + wiring.** `LiquidSlipSystem` + the three
`MovementSystem` call sites + `ForceMoveTo(slipChain)`.
Tests: §5.

**S3 — readout + doc.** `" (slippery)"` on the ground line; this doc's
§9 log; CLAUDE.md-mandated cold-eye (Q1-Q4).

**S4 — live check.** Play mode: Jet Blast → Rime Grip → walk onto the
ice with `TestRng` seeded to slip → "You slip on the ice!" + displaced
one cell; `diag_query category=liquid kind=Slipped`. Then the honest
"cannot verify" line: feel of the displacement (visual).

## 5. Tests (RED first) — `Assets/Tests/EditMode/Gameplay/Turns/LiquidSlipTests.cs`

Fixture: zone, creature (tagged Creature), registry loaded from the
real JSON, `LiquidSlipSystem.TestRng = new Random(seed)`. Because the
roll is a bare `rng.Next(100) < chance`, tests force outcomes by
**authoring the chance** (a test-only registry row or `SlipChance` 0 /
100 on a scratch definition) rather than by hunting seeds — seeds are
used only where the *direction* matters, and the seed's direction is
asserted in the test so a future RNG change fails loudly.

| # | Test | Pins |
|---|---|---|
| 1 | `SlipChance_DefaultsTo50_WhenAbsentFromJson` + `IceIsSlippery_WaterIsNot` | data |
| 2 | `SteppingOntoIce_AtChance100_SlidesOneRandomCell` | the mechanic; landed ≠ ice cell; landed is one of the 8 neighbours; message logged |
| 3 | `SteppingOntoIce_AtChance0_StaysPut` | counter-check (Slippery:true, chance 0 — cosmetic-slippery is legal) |
| 4 | `SteppingOntoWater_NeverSlips_EvenAtChance100` | `Slippery:false` gate beats the number; identical setup, flag flipped |
| 5 | `SteppingOntoOilPoolEntity_CanSlip` | pool universe, same resolver |
| 6 | `SteppingOntoDryGround_NoRollAtAll` | no `SlipRolled` diag — the early-out is real |
| 7 | `AnItemMovedOntoIce_DoesNotSlip` | Creature gate |
| 8 | `SlideIntoAWall_SlipsButDoesNotMove` | message + `Slipped{displaced=false}`, position unchanged |
| 9 | `SlideOntoMoreIce_ChainsButStopsAtThree` | chance 100, ice sheet; final displacement ≤ 3 cells from the first landing; exactly 3 `Slipped` records |
| 10 | `SlideNeverLandsOnAnotherCreature` | occupancy rule (all 8 neighbours but one occupied → lands on the free one; all occupied → displaced=false) |
| 11 | `KnockbackOntoIce_RollsOnce` | `ForceMoveTo` path from `TryPush` also resolves; chainDepth 0 |
| 12 | `SlipRolled_ChanceInPayload_MatchesTheJson` | the diag carries the authored number — auditability |
| 13 | `NpcStepGoal_OntoIce_Slips` | AI path (StepGoal) — NPCs are not exempt |
| 14 | `PlayerTurn_IsSpentExactlyOnce_OnASlip` | the C5 reason for post-move: no double EndTurn |
| 15 | `SlipRolled_Diag_OnEveryRoll` + `Slipped_Diag_OnFailure` | observability |
| 16 | `GroundLine_SaysSlippery` | readout |

Adversarial gate (CLAUDE.md taxonomy — probabilistic boundaries,
cross-actor, save/load reach), `LiquidSlipAdversarialTests.cs`, ~12:
`SlipChance` 0 / 100 / negative / >100 in JSON (clamp on load, never
throw); unknown liquid id in a coating (registry null → no roll);
mover removed from zone mid-chain (dies to a step trap on landing →
chain stops, no NRE); pool with `Volume=0` → no roll (mirrors
`LiquidPoolPart`'s PoolEmpty refusal); coating + pool both present →
one roll; `FreezeUnderStandingCreature_DoesNotRollSlip` (no move, no
roll — R7); a slip whose random direction is the cell it came from
(legal — you slide back; pinned as allowed, not a bug); zone-edge cell
never slides out of bounds; TestRng reset between tests (pollution
guard).

## 6. Content readiness

🟢 All four liquid JSONs exist. 🟢 `ice` coating is produced by
`freeze_water`. 🟢 Oil coatings are produced by Oilmark; oil pools by
thrown flasks/tonics. 🟢 No new blueprints. 🟡 `SlipChance` is a new
optional field — absent ⇒ default 50, so every other liquid file is
untouched (and none of them is `Slippery:true`, so none gains
behaviour by accident — verified: only oil/gel/pebble-sundew-dew carry
the flag, C1).

## 7. Divergences from Qud (deliberate)

| Qud | CoO | Why |
|---|---|---|
| **Agility save** (`MakeSave`, chance `(21−diff+mod)×5%`, difficulty `5 + amount(diminishing) − Stable`) | **Flat per-liquid `SlipChance` %** — no stat, no property | **User decision, 2026-08-15: keep it simple.** Follow-up path is fixed now so it's a roll change, not a content change: `chance = SlipChance − 5×AgilityMod`, clamped [5,95], and `− 5×Stable`; `StatUtils.GetModifier` + `GetIntProperty("Stable")` already exist. |
| Difficulty scales with liquid *amount* (drams, diminishing) | One number per liquid | Tile coatings have no amount; pool Volume is not comparable to drams. Content number > fake math. |
| `SlipperyWhenWet` vs `SlipperyWhenFrozen` on one liquid | One `Slippery` bool per liquid; **ice is its own liquid id** | CoO's freeze produces an `ice` coating rather than flagging the water frozen — the flag lives on ice.json. Same outcome. |
| Wading depth exempts | No depth model | Documented in the liquid plan already. |
| `Slimewalking` exemption | None yet | Lands with the save follow-up as the `Stable` property. |
| Smart-AI navigation weight | None | No path-cost seam (C10). NPCs are as blind to ice as they are to oil today — kiting a hunter across your own ice is a *feature* until AI grows a cost map. Logged as follow-up. |
| Unbounded slide chain | Cap 3 | C4 safety + testability. |
| Particle text | AsciiFx burst optional | Not in v1; the message carries it. |

## 8. Critical review of the plan (before implementation)

**R1 — Post-move slide vs "lose the move"; save vs flat chance.** My
chat framing was stay-put; Qud is slide. Slide wins on three counts:
(a) it is the reference; (b) it needs zero InputHandler/turn plumbing
(C5) and works for NPCs and knockbacks identically (C6, C3); (c) it is
*more* interesting — an ice patch between you and the archer is a
hazard, not just a tax. **User chose slide, and chose a flat chance
over the Agility save.** Reviewed for what that costs: nothing
structural. The save is a strictly-local change to the one `Next(100)
< chance` line, and §3.2 fixes the forward-compat rule so the JSON
rows survive it. What it costs in *feel*: every creature slips alike —
a nimble rogue and a lumbering brute are equally clumsy on ice until
the save lands. Acceptable for a first cut; the diag record will show
whether ice reads as fun or as a tax before any stat math is argued
about.

**R1b — Default-50 for an absent `SlipChance`.** Considered default 0
("safe": a bare `Slippery:true` does nothing). Rejected: that's the
fail-silent shape the status study kept finding — a flag that says
slippery and isn't. Default 50 means the flag *means* something the
moment it's set, and every shipped `Slippery:true` row sets an
explicit number anyway.

**R2 — The C4 re-entrancy hazard is real and the design dodges it
structurally**, not by luck: the resolver is called by `MovementSystem`
after `FireCellEnteredEvents` returns, never from a handler. The
`LiquidPoolPart` `EntityEnteredCell` handler stays as it is (coat
only). Test 5 proves the pool universe slips via the *movement* seam,
not via the pool's handler. If someone later moves the slip into the
handler, test 9's chain would corrupt the scratch list — pin a
comment on `_enteredCellScratch` naming this file.

**R3 — Double roll when both universes are present** (oil coating on
the tile AND an oil pool entity in the same cell — possible after a
thrown flask on an Oilmark slick). `FindSlipperyLiquid` returns one
liquid; one roll. Adversarial test covers it.

**R4 — Frozen-on-freeze + slip-on-step interplay.** A creature that
was standing on water when it froze gets Frozen(0.6) and does not
move, so it never rolls a slip until it thaws and steps. A creature
that steps onto already-frozen ice rolls a slip but never gets Frozen.
Two different consequences, both readable. No conflict.

**R5 — Player-experience risk: slipping *off* the ice into a hostile.**
The slide can land you adjacent to (never onto) an enemy — Qud allows
this. Acceptable; it's a hazard. What it must NOT do is slide you
through a wall or onto a creature (C7 rules, tests 8/10) or off the
zone edge (in-bounds check; no zone transition from a slip).

**R6 — Determinism.** `TestRng` static injection mirrors
`GasFungalSporesPart`; the default RNG is a static `System.Random`.
Save/load: nothing to persist — a slip is instantaneous.

**R7 — Where's the counter for "the reaction froze the tile under a
*standing* creature — does it slip?"** No: no move occurred. Pinned
implicitly by test 6's "no move, no roll" shape; add an explicit
adversarial: `FreezeUnderStandingCreature_DoesNotRollSlip`.

**R8 — The numbers.** 50% on ice is Qud-harsh (Qud's ice at average
Agility lands near there too). It's tunable data with a diag trail;
ship it, watch the `SlipRolled` records in play, adjust JSON. If it
feels bad the fix is a number, not code.

**R9 — Chance-100 chains and the cap.** With the save gone, a
`SlipChance: 100` liquid (legal content) on a long sheet slides every
time; only `MAX_SLIDE_CHAIN` stops it. The cap is therefore load-
bearing, not defensive — test 9 pins it at exactly three `Slipped`
records, and the adversarial sweep tries chance 100 on a 10-cell
sheet.

**R10 — Randomness ownership.** One static RNG in `LiquidSlipSystem`
(fungal-spores pattern), not `ctx.Rng` — movement has no context
object and knockbacks arrive from many callers. `TestRng` overrides
it; the fixture resets it in TearDown (pollution guard in the
adversarial file).

Net revisions from review: added test 14 (turn spent once), the
adversarial "freeze under standing creature" case, the pool
`Volume=0` rule, R9's chance-100 sheet, R10's RNG reset.

## 9. Implementation log — shipped 2026-08-15

**Status:** ✅ shipped, one commit. Tests 6672 → 6702 (+30: 17 in
`LiquidSlipTests`, 13 in `LiquidSlipAdversarialTests`). All green.
Live-verified in Play mode.

### 9.1 What shipped

| File | Change |
|---|---|
| `LiquidDefinition.cs` | `SlipChance` (int, default 50) beside `Slippery`. |
| `ice.json` / `oil.json` / `gel.json` / `pebble-sundew-dew.json` | `Slippery:true` (ice flipped) + `SlipChance` 50/40/50/30. |
| `LiquidSlipSystem.cs` | NEW. `ResolveAfterMove`, `FindSlipperyLiquid`, `MAX_SLIDE_CHAIN=3`, `TestRng`. |
| `MovementSystem.cs` | `ResolveAfterMove` after `FireCellEnteredEvents` in `TryMoveEx` / `TryMoveTo` / `ForceMoveTo`; `ForceMoveTo(…, int slipChain = 0)`; scratch-list comment names the contract. |
| `CellStatusReadout.cs` | `DescribeLayer` → "ice (4 turns, slippery)". |
| `ColdTileBridgeTests.cs` | one exact-string assertion updated to the new readout. |

### 9.2 Found in implementation

**False premise in the sweep (C2): the "two liquid universes" are
already one at ground level.** `Zone.ProjectPool` (Zone.cs:463,
Palimpsest P2b) mirrors every `LiquidPoolPart` entity into
`TileState` as a permanent coating on `AddEntity`, and unprojects on
removal. The first cut scanned coatings *then* pool entities and
labelled the diag `source: tile|pool`; the pool test came back
`source:"tile"` because the projection had already been found. The
pool scan was dead code and the label was misleading — both removed.
`FindSlipperyLiquid` reads the tile layer only; puddle and spill are
one question, one roll. (Consequence to know: a pool with `Volume=0`
still projects and therefore still slips — that is P2b's behaviour,
not this feature's; the plan's "no volume, no slip" adversarial was
dropped as moot.)

**Mover removed mid-chain (adversarial design, before the run):**
`ResolveAfterMove` now requires `zone.GetEntityCell(mover) == landed`.
A cell-entry handler that kills or teleports the mover (a step trap)
would otherwise let the slide `MoveEntity` a body that is no longer in
the zone — re-adding it. Pinned by
`Adversarial_MoverDiesOnLanding_ChainStopsCleanly`.

**Chain arithmetic:** three slides ⇒ three rolls (the landing of the
third slide is refused a roll), not four. Test 9's assertion was
corrected; the doc's "cap 3" was right, my count of rolls was not.

### 9.3 Live check (Play mode, `execute_code` calling the exact
`MovementSystem.TryMoveEx` the InputHandler calls; `manage_input` is
not present in this MCP build)

```
[registry] ice.Slippery=True chance=50
[ground]   On the ground: ice (4 turns, slippery)
[before]   player@(39, 11)   ice at (40, 11)
[moved]    True   [after] player@(41, 11)
[msg]      you slips on the ice!
[diag]     liquid/Slipped {"liquidId":"ice","fromX":40,"fromY":11,"toX":41,"toY":11,"displaced":true,"chainDepth":0}
```
Can verify: the whole pipeline in the real runtime with shipped
content. Cannot verify: the InputHandler keypress → `EndTurnAndProcess`
glue (unchanged code; a real keypress cannot be injected here) and the
visual feel of the displacement.

### 9.4 Cold-eye (Q1-Q4)

- Q1: all three move methods call the resolver at the same position
  (after cell-entry dispatch). ✓
- Q2: two external `ForceMoveTo` callers (push/pull) untouched, default
  chain 0. ✓
- Q3: `slipped` true/false, `displaced` true/false, `chainDepth` 0 and
  >0 each asserted. ✓
- Q4: two doc lines described the removed pool scan / `source` field —
  corrected in place with pointers here. ✓

### 9.5 Self-review markers

- 🟡 mover-removed-mid-chain guard — fixed pre-commit (9.2).
- 🔵 "you slips on the ice!" — house grammar (every MessageLog line
  concatenates `GetDisplayName()`; "you's rime grip" already ships).
  A verb-agreement helper is a game-wide fix, not this feature's.
- ⚪ Agility save / `Stable` — deferred by decision (§7 row 1).
- ⚪ AI path-cost awareness of slippery cells — no seam (C10).
- ⚪ `Volume=0` pools slip via projection — P2b behaviour, noted.

## 10. Decisions taken

- **Slide, not stay-put** — Qud's shape (§2.1 Q3), user-confirmed.
- **Flat `SlipChance` %, no Agility save** — user decision; §7 row 1
  fixes how the save slots in later without touching content.
- Ready to implement S1→S4 on "go".
