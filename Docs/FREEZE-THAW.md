# Freeze and thaw by magnitude

**Status:** implemented and unit-tested; full-suite before/after diff and
fight-sim re-run recorded below. Not run in the Unity editor.

## Existing tests changed (deliberately)

The full EditMode suite (14,278 tests in the Unity-free runner; the same ~308
environmental failures before and after) was diffed before/after. The first
run found 3 newly failing tests:

| Test | Cause | Resolution |
|---|---|---|
| `ConsumingRiteTests.HollowCoin_SpendsThreeStatuses_NotTwo`, `ConsumingRiteHypothesisTests.Hypothesis_ATwoSlotRite_LeavesTheThirdStatusForTheFollowUp` | My first draft absorbed a flame that met ice, so Wet + Frozen + Burning could not be stacked | **Production changed, tests untouched**: ignition thaws but still ignites (divergence 4) |
| `DensityItemExamineAdversarialTests.DurationIsNotInventedForPhysicalStateEffects` | Pinned the old text `100% iced until it thaws` | **Test updated**: the line now reads `100% iced; thaws in about 10 turns`, a time derived from the freeze magnitude. The test's real guard (the spec's `777` duration argument never appears) is unchanged |
**Qud reference:** none consulted. Classified **CoO-original** (§4.2). No
parity claim is made.

## Problem (player-visible)

A frozen creature or object stayed frozen for a time that had nothing to do
with how hard it was frozen:

- `FrozenEffect.OnTurnEnd` thawed **only while the owner's body was above
  freezing**. Every freeze is created by driving the body *below* freezing, so
  the thaw clock could not start until ambient decay (2% a turn) had warmed the
  body back across 0 degrees: about 65 turns for a flesh body, whatever the
  size of the chill. Rime Grip, Cold Snap or a Frost tonic was an effective
  permanent lock.
- Fire did nothing to ice. A heat dose, fire damage or an ignition left a
  frozen creature exactly as frozen as before.

## Rules now

`Cold` (0..1) is the **magnitude** of the freeze. It drains every turn, and
the creature or object acts again when it reaches 0.

| Source | Effect on Cold | Constant |
|---|---|---|
| Time, every turn | `-0.10` | `THAW_PER_TURN` |
| Warmth, every turn, body above freezing | `-0.0002` per degree above | `WARMTH_THAW_PER_DEGREE` (the old coefficient, now additive) |
| A heat dose (`ApplyHeat`) | `-0.004` per degree it raises temperature | `THAW_PER_DEGREE_OF_HEAT` |
| Fire/heat damage taken | `-0.06` per point | `THAW_PER_FIRE_DAMAGE` |
| A flame taking hold on the frozen target | `-0.5 x intensity`; the target still ignites | `THAW_PER_BURN_INTENSITY` |

So a freeze of 0.2 lasts 2 turns, 0.5 lasts 5, and 1.0 lasts 10, on time
alone. Cold and heat attacks of the same size do not cancel by accident:
only heat thaws, a cold dose never does.

**Magnitude of a new freeze** (`ThermalPart.TryFreeze`) now depends on how far
below freezing the dose drove the body: `0.10 + degreesBelow / 600`, capped at 1
(`ColdForDepth`). A Quench (-150 J) on flesh is about 0.21 (2-3 turns, less
than its 6-turn cooldown, so it can no longer be chained into a lock); a
540-degree plunge saturates at 1.0 (10 turns). Wet targets still deepen the
freeze x1.5 (existing rule).

Objects are covered because the thaw lives on the effect, which objects carry
too (ThermalPart freezes both). The look-mode description reads
`Frozen over - 40% iced; thaws in about 4 turns (fire thaws it faster).`

**Diag:** every non-time thaw emits `effect/Thawed {cause, amount, coldBefore,
coldAfter}` with `cause` in `heat | fire | ignition`; the removal itself emits
the existing `effect/OnRemove`. Time drain is not recorded
(one record per frozen creature per turn would drown the buffer).

## Divergences from the plan / prior behavior

| # | Was | Is | Why |
|---|---|---|---|
| 1 | Thaw only above freezing | Thaw always, faster when warm | Thaw time must follow magnitude |
| 2 | Every freeze = Cold 1.0 | Cold by depth of the dose | "According to magnitude of freeze" needs a magnitude |
| 3 | Fire inert against ice | Heat, fire damage and ignition thaw | Requested |
| 4 | (first draft) a flame that meets remaining ice is *absorbed* and does not ignite | Ignition thaws and **still ignites** | The full-suite diff found 2 consuming-rite tests (Hollow Coin, Sundering Word) that prime Wet + Frozen + Burning on one target. That trio is the resonance mechanic; absorption would have made Frozen + Burning mutually exclusive in that order and killed it. Cold already defeats fire on *apply*; the reverse thins the ice instead |

## Balance consequence (read this)

Rime Grip + dagger was the legacy Classic kit's best fight (it froze everything
and stabbed it). With the lock gone, the same scripted policy on the same
seeds drops from **80% to 37% wins** against two Scrabblers and a Gleaner
(`Docs/Design/StartingBuildsProbe/fight-sim-designtime-builds-on-freeze-code.txt`
vs the pre-change `fight-sim-final.txt`). That is the requested change working,
not a regression to fix. (None of the four new builds is built around a freeze
lock; the design excluded Quench for exactly that reason.) The Bombardier's frost tonic still helps, but as a 4-10 turn
window rather than a permanent one: its S3/S5 win rates are 8 points under the
design-time numbers (85/67 vs 93/75; sim noise is about +/-6).

## In-phase self-review

- 🟡 *Fixed pre-commit.* `Thaw` at 0 from a non-time cause must remove the
  effect immediately; a time tick leaves removal to the EndTurn sweep (which
  already handles `Duration == 0`). The time path is `ThawTime_IsMagnitudeOverRate`; the immediate path is `HeatDose_ThawsByTheTemperatureItRaises` and `IgnitingAFrozenTarget_WithEnoughFlame_ThawsItAndCatchesFire`.
- 🔵 `TurnsToThaw` ignores warmth and fire, so the description's number is an
  upper bound ("about"). Acceptable: the text says fire thaws it faster.
- 🧪 Not covered: gas-cloud heat sources and Burning *damage* ticks on a frozen
  target beyond the single-hit path (Frozen extinguishes Burning on apply, so a
  frozen target cannot be burning).
- ⚪ Constants are tuning values with no external reference.

## Tests

`Assets/Tests/EditMode/Gameplay/Effects/FrozenThawTests.cs` (30 tests):
thaw time = magnitude / rate for 0.2/0.5/1.0; a below-freezing body still thaws
(the old lock); warmth speeds it; wet x1.5; `ColdForDepth` monotonic, floored,
capped; shallow vs deep dip; a Quench-sized dose thaws before its cooldown and a
deep chill outlasts it; a heat dose thaws and a cold dose does not (counter);
heat capacity matters; ignition on a frozen target melts ice by intensity and still
ignites, thaws it completely when strong enough, and Wet + Frozen + Burning still
stack (the resonance priming trio); unfrozen ignition unchanged (counter); fire/heat damage thaws, Slashing/Cold/Bludgeoning do not
(counter); objects thaw and are thawed by heat; diag `Thawed` cause for each
route and none for time; describer text.

## Files

- MOD `Assets/Scripts/Gameplay/Effects/Concrete/FrozenEffect.cs` - thaw model.
- MOD `Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs` - frozen target absorbs/thaws on ignition.
- MOD `Assets/Scripts/Gameplay/Materials/ThermalPart.cs` - heat dose thaws; freeze magnitude by depth.
- MOD `Assets/Scripts/Gameplay/Effects/EffectDescriber.cs` - look-mode text.
- NEW `Assets/Tests/EditMode/Gameplay/Effects/FrozenThawTests.cs` (30 tests).
- MOD `Assets/Tests/EditMode/Gameplay/Items/DensityItemExamineAdversarialTests.cs` - one assertion (see above).
