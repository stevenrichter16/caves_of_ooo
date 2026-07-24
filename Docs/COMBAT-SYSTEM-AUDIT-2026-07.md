# Combat System Audit — 2026-07-22

> Full-system audit of `CombatSystem.cs` + its on-hit dispatch chain, the
> status-effect lifecycle, all 4 weapon skill trees, performance, Qud
> parity, test coverage, and diag/observability — run as an 8-dimension
> parallel workflow with two independent adversarial skeptics verifying
> every candidate finding before it counts as real. This is a **findings
> report**, not an implementation log — nothing here has been fixed yet
> unless a follow-up commit says so.

**Status:** 🔍 AUDIT COMPLETE, ZERO FIXES APPLIED — this doc is the
punch list. 39 candidate findings survived to a verdict: **35 CONFIRMED**,
2 PLAUSIBLE (split skeptic vote, judgment call), 2 REFUTED (kept in §8 for
transparency). A ninth pass (a completeness critic, not a dimension) then
identified **7 gaps no dimension touched** — §1 below — which is arguably
the single most important output of this audit.

**Method:** 1 scoping agent → 8 parallel dimension agents (each read the
actual source, cited file:line, returned ≤10 prioritized findings) → every
finding checked by 2 independent skeptic agents instructed to actively try
to refute it, not rubber-stamp it → 1 completeness critic. 88 agents
total, 1456 tool calls, ~7.4M tokens. Three of the highest-stakes findings
below were additionally hand-verified by direct file read before this doc
was written (marked ✅ hand-verified).

---

## §1. The one meta-finding: melee is solid, everything else barely exists

The 8 dimensions all scoped themselves to `CombatSystem.PerformSingleAttack`
and its immediate melee call graph — reasonably, since that's "the combat
system" in the obvious sense. The completeness critic then read the
*rest* of what deals damage in this game and found it in dramatically
worse shape than melee:

- **Thrown weapons have zero accuracy check and skip the entire on-hit
  pipeline.** `ThrowItemCommand.cs:186-190` rolls damage and calls the raw
  `CombatSystem.ApplyDamage(Entity,int,Entity,Zone)` overload directly on
  whatever `LineTargeting.TraceFirstImpactToTarget`'s pure geometry
  returns — no to-hit roll, no DV/dodge, no crit, and none of
  OnHitClassEffects/OnHitWeaponEffects/ItemEnhancementDispatch/
  SkillEventDispatcher fire. Every thrown weapon in the game is affected.
- **Mutation/spell line-attacks have the same 100%-accuracy problem.**
  `DirectionalProjectileMutationBase.cs` uses the same unconditional
  `LineTargeting.TraceFirstImpact` — first entity in the path is always
  hit, no DV roll. (It does correctly route through
  `MutationDamageHelpers.ApplySpellDamage`, so resistance at least
  applies — thrown weapons don't even get that.)
- **Both of those paths print the wrong damage number in the message
  log.** `CombatSystem`'s melee path deliberately computes
  `actualDamage = hpBefore - hpAfter` (post-resistance) for its log line.
  `ThrowItemCommand.cs:189` and `DirectionalProjectileMutationBase.cs:94-96`
  both print the pre-resistance raw roll instead — so the combat log
  overstates damage taken by any target with nonzero AV/resistance,
  for every thrown weapon and every line-attack mutation in the game.
- **DV/Dodge is consequently irrelevant to two entire combat modalities.**
  Direct consequence of the above two: any Dodge/Evasion investment
  protects against melee only.
- **AI target *acquisition* has no threat model.**
  `AIHelpers.FindNearestHostile` is a pure nearest-Chebyshev-distance scan
  gated only by `FactionManager.IsHostile` — no aggro weighting, no
  focus-fire coordination, no "protect the weakest ally."
- **Followers never assist their leader in combat.** Confirmed by grep:
  `new KillGoal(...)` is only ever pushed from `BoredGoal.cs` and
  `GuardGoal.cs`, neither of which reads party-leader-under-attack state.
  A recruited companion standing next to its fighting leader will not
  engage the leader's attacker — "assist my leader" is simply
  unimplemented.
- **Retaliation/aggro-on-hit has exactly one call site in the entire
  game.** `SetPersonallyHostile`/`AddPersonalEnemy` is called from
  precisely one place: `InputHandler.ExecuteAttackOnNPC` (the player's
  melee-confirm UI flow). Thrown damage, mutation damage, AoE gas/tonic
  damage, and all AI-vs-AI damage never make the victim hostile to its
  attacker. A neutral creature killed by a thrown axe has no mechanism to
  ever become aggressive in response.
- **Mid-combat save/load round-tripping was never examined** by any of
  the 8 dimensions despite `SaveSystem.cs` demonstrably serializing
  combat-adjacent state (the `PersonalEnemies` HashSet, goal stacks).
  Not confirmed broken — confirmed *unchecked*, which for an RPG with
  persistent death (this project's own stated identity) is itself a gap.

  **Status: STALE — substantively closed by two more-recent, dedicated
  audits.** `Docs/SAVE-LOAD-AUDIT.md` (effects/Parts, all tiers) and
  `Docs/TURNMANAGER-ADVERSARIAL-AUDIT.md` (TurnManager specifically,
  dated 2026-07-23) each already cover this ground and found/fixed real
  bugs along the way (2 TurnManager scheduling bugs, 1 HibernatingEffect
  save/load bug). A 2026-07-23 scoping pass cross-checked every item a
  fresh audit would have checked — TurnManager energy/tick state,
  `StatusEffectsPart` active effects, sentinel `Tags` (`DEATH_HANDLED_TAG`
  etc.), `ShatterArmorEffect.StackCount`, `PersonalEnemies`, `GasPoolPart`
  density — and found all of them already citably tested. The **only**
  genuinely unpinned field was `TurnManager.WaitingForInput` (mechanically
  wired correctly, just never asserted) — closed via
  `Spec_TurnManager_WaitingForInput_True_Preserved` /
  `_False_Preserved` in `SaveSystemSpecTests.cs`. No production bug found;
  this finding is now fully addressed. Do not re-commission a fresh
  save/load audit on this basis — see `SAVESYSTEM-DEEP-DIVE-AUDIT.md`'s
  own note on diminishing returns from repeated adversarial passes on
  this surface.

**Bottom line: if "improve the combat system" means highest leverage per
hour, the thrown-weapon/mutation-line-attack accuracy gap and the
single-call-site retaliation gap are both bigger than anything in §2-§7.**
They're also both scoped narrowly enough (2-4 files each) to be
tractable follow-up features rather than a re-audit.

---

## §2. Confirmed bugs — melee pipeline & effects

Severity legend (matches this project's own convention):
🔴 = high-impact, gameplay-observable · 🟡 = medium · 🔵 = low/nit.

### 🔴 `ShatterArmorEffect` has zero effect on ~all real combat (independently found twice)
**File:** `CombatSystem.cs:263, 1259-1276` vs `602-639` — ✅ hand-verified.
`PerformSingleAttack` picks AV via
`hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender)` (line
263). `SelectHitLocation` returns a non-null part for any creature with a
normal `AnatomyFactory` body — i.e. virtually everything. `GetPartAV`
(1259-1276) sums equipped + natural armor only; `GetAV` (602-639) is the
*only* path that subtracts `ShatterArmorEffect.AV_REDUCTION * StackCount`
(628-637), and it's only reached when the defender has no Body at all.
Result: `Cudgel_ShatteringBlows` procs, the UI shows the effect active,
and the very next attack's penetration roll is computed against the
*unshattered* AV. `ShatterArmorEffectTests.cs` only unit-tests `GetAV()`
directly, never a full `PerformSingleAttack`, so this has zero regression
coverage. Independently flagged by both the damage-pipeline dimension and
the Qud-parity dimension.
**Fix:** move the shatter subtraction into a shared helper both `GetAV`
and `GetPartAV` call. Add a `PerformSingleAttack`-level test with a
Body-having defender + active `ShatterArmorEffect`.

### 🔴 Dismemberment can re-fire `HandleDeath` on an already-dead target
**File:** `CombatSystem.cs:426-484`, `1290-1321`; `Body.cs:268-271`.
The `hpAfter > 0` survivor gate at line 426 is captured once and never
re-checked. Inside the on-hit block, `ItemEnhancementDispatch` can
independently kill the defender (e.g. `EnhancementPaleSalt`'s bonus
damage vs. Undead calls `ApplyDamage` synchronously). Control then
proceeds unconditionally to `CheckCombatDismemberment` (line 483-484),
which has no is-still-alive guard; if its chance roll hits a Mortal body
part, `Body.Dismember` (Body.cs:268-271) unconditionally re-invokes
`CombatSystem.HandleDeath` on a target that's already dead —
`HandleDeath` itself has none of `ApplyDamage`'s re-entrancy guard.
**Failure:** a Pale-Salt weapon kills an Undead enemy mid-chain via its
bonus-damage proc; the dismemberment roll then fires a *second* full
death pipeline — duplicate kill message, duplicate death FX, and (for
any entity carrying `AddFactWhenSlain`, which is a non-idempotent delta)
a quest kill-counter incrementing by 2 for one kill.
**Fix:** re-check the defender's live HP immediately before
`CheckCombatDismemberment` and the skill dispatch calls; harden
`HandleDeath` with the same idempotency guard `ApplyDamage` already has.

### 🔴 `CharredEffect.OnRemove`'s restore is permanently broken after any save/load
**File:** `Effects/Concrete/CharredEffect.cs:14-15, 34-42`.
`_originalCombustibility`/`_hasStoredOriginal` are **private** fields;
`SaveSystem.WritePublicFields` only walks public instance fields, so
neither survives a save/load. `OnRemove` checks `_hasStoredOriginal`
before restoring — after any reload it's `false`, so the restore
silently no-ops and `MaterialPart.Combustibility` (public, persisted at
its already-reduced value) stays permanently charred. The existing test
`EffectRoundTripPrivateStateTests.CharredEffect_PrivateState_IntentionallyNotPersisted`
asserts this exact behavior and its own comment claims "OnApply
re-captures on load" — but nothing calls `OnApply` again post-load; this
is a real bug wearing an "intentional" ⚪ label.
**Fix:** make the two fields public (mirrors `HibernatingEffect`'s
existing fix for the same class of problem), or add an `OnAfterLoad`
that re-derives the original from the blueprint default.

### 🔴 `HibernatingEffect` / `FungalInfectionEffect` permanently corrupt `BaseValue` if the buffed stat has any Bonus/Penalty
**File:** `HibernatingEffect.cs:72-73, 79-82, 93-101`; same pattern in
`FungalInfectionEffect.cs:101, 111-112, 244-250`.
`OnApply` captures `GetStatValue(...)` — the fully composited
`BaseValue + Bonus - Penalty + Boost` — but `OnRemove` writes that
composite value back into `BaseValue` alone, permanently baking in
whatever Bonus/Penalty existed at capture time. `CoatedInPlasmaEffect.cs`
(same folder) does this correctly — captures/restores `BaseValue`
directly — proving the fix pattern already exists in-tree. Dormant today
(nothing currently puts Bonus/Penalty on HeatResistance/ColdResistance/
Toughness), but every existing test fixture for this class constructs
the stat with Bonus/Penalty implicitly 0, so the 5 passing
`HibernatingEffectTests` never exercise the defect.
**Fix:** capture/restore `stat.BaseValue` directly, not
`GetStatValue`/`.Value`. Add a test with a nonzero Bonus present at
apply-time.

### 🟡 `BurningEffect.OnStack` drops the re-igniter's identity — misattributes kill credit
**File:** `BurningEffect.cs:170-178`.
`OnStack` merges `Intensity` from the incoming re-ignition but never
touches `IgnitionSource`; `IgnitionSource` is what per-turn fire damage
(and any resulting kill) gets attributed to. Attacker A ignites a
target; Attacker B re-ignites it two turns later with a bigger hit — all
subsequent tick damage, and a possible burn-tick kill, stays credited to
A even though B contributed more and may have landed the actual kill.
**Fix:** update `IgnitionSource` on stack (either "most recent igniter"
or "larger contribution wins" — pick one and pin it with a test).

**Status: FIXED.** Chose "most recent igniter wins" (simpler than
tracking cumulative per-source contribution, matches the intuitive
convention that whoever is currently burning you is credited).
`OnStack` now reassigns `IgnitionSource = burn.IgnitionSource`
unconditionally, right after the existing Intensity-merge line — the
Intensity formula itself is untouched. Tests in
`Assets/Tests/EditMode/Gameplay/Effects/BurningEffectTests.cs` (new
file, 4 tests): re-ignition by a different source reassigns
IgnitionSource; counter-check that same-source re-ignition needs no
special casing; counter-check that the Intensity-merge formula is
unaffected; counter-check that stacking still absorbs into one
instance rather than duplicating.

### 🟡 `ConfusedEffect`'s non-stacking guard is bypassable via `ForceApplyEffect`
**File:** `ConfusedEffect.cs:22-29`; `StatusEffectsPart.cs:52-72`.
Unlike every other non-stacking effect in the codebase (`Hibernating`,
`Recruited`, `Broken` — all correctly override `OnStack`), `Confused`
implements its no-duplicate rule via `CanApply`, which
`ApplyEffectInternal` only consults on the *non-forced* path. Its
`OnStack` is the `Effect` base default (returns false = "don't absorb"),
so `ForceApplyEffect` on an already-confused target adds a second,
independent instance — doubling the DV/Agility penalty. Unreachable
today (nothing calls `ForceApplyEffect` with `Confused`), but silent the
moment any content does.
**Fix:** move the guard into `OnStack` (mirroring the sibling effects),
so it applies regardless of forced/unforced.

**Status: FIXED.** Added `public override bool OnStack(Effect incoming)
=> true;` to `ConfusedEffect.cs`, mirroring `HibernatingEffect`'s
identical pattern exactly — unconditionally absorbs a stacked/forced
re-apply instead of adding a duplicate instance. The pre-existing
`CanApply` gate on the non-forced path is untouched. Tests in
`Assets/Tests/EditMode/Gameplay/Effects/StatusEffectTests.cs`:
`Confused_ForceApply_DoesNotDuplicateInstance` (RED before fix — a
forced re-apply doubled the DV/Agility penalty) and
`Confused_NormalDoubleApply_StillBlockedAndStillLogsRejectionPath`
(counter-check — the normal non-forced path is still rejected via
`CanBeAppliedTo` before ever reaching the stacking loop, not silently
rerouted through the new `OnStack` override).

### 🔵 `PaperSkinEffect`'s combat-log math double-counts its own increase, and it has zero test coverage anywhere
**File:** `PaperSkin.cs:25-34`.
Line 29 is the one real mutation (`damage.Amount += Increase`). Lines
30-31 both log `damage.Amount` *after* that mutation while labeling it
"Original damage" and computing a "from X to Y" line that reads the
already-incremented value on both sides — the log visually implies the
increase was applied twice when mechanically it wasn't. Grep across all
of `Assets/Tests/` finds zero references to `PaperSkinEffect` despite it
being live content via `OnHitEffectFactory`.
**Fix:** capture `original = damage.Amount` before the mutation; log
`original` and `damage.Amount`, not two post-mutation reads. Add a test
file — there currently is none.

### 🔵 `FrozenEffect`/`AcidicEffect` mislabel natural-recovery as `duration_expired`
**File:** `FrozenEffect.cs:60-73`; `AcidicEffect.cs:71-76`.
Both effects run at `DURATION_INDEFINITE` for their entire life and end
only when their physical value (Cold/Corrosion) decays to zero — but
neither sets `LastRemovalCause` before forcing `Duration = 0`, so both
inherit the generic default. `diag_query kind=OnRemove` can't distinguish
"thawed out" from "a fixed timer ran out" for either effect, even though
`StunnedEffect`/`BleedingEffect` already set a distinct cause
(`CAUSE_SAVE_SUCCEEDED`) for their own non-timer recovery path — this is
the same bug class as the `owner_died` mislabeling fixed earlier today
(commit `54ce874`), recurring in two more places.
**Fix:** add a `CAUSE_NATURAL_RECOVERY` (or reuse an existing distinct
constant) and set it before the early `Duration = 0` in both effects.

### 🔵 `HandleDeath`'s kill-XP award has no self-kill guard
**File:** `CombatSystem.cs:1046-1054`; `LevelingSystem.cs:24-38`.
`if (killer != null && killer.HasTag("Player")) AwardKillXP(...)` never
checks `killer != target`. Low-impact today (player entities presumably
default `XPValue` to 0), but nothing prevents a future self-damage
mechanic (spell backfire, self-set trap) from awarding the player XP for
killing themselves if any pathway ever gives the player entity a nonzero
`XPValue`.
**Fix:** add `killer != target &&` to the gate.

### 🟡 (plausible — judgment call) `ApplyDamage` decrements `Hitpoints.BaseValue` unclamped
**File:** `CombatSystem.cs:384, 843-847`; `Stat.cs`.
The inline comment claims the decrement is "(clamped to ≥ 0)"; the code
does `hpStat.BaseValue -= amount` — `BaseValue` is a bare public int with
no clamp anywhere (only the computed `.Value` getter clamps). A 40-damage
overkill on a 10-max-HP creature leaves `BaseValue == -30`; all the
existing `<= 0` death checks read via `.Value`/`GetStatValue` so they
still work correctly today. Three call sites elsewhere in the codebase
already read `.BaseValue` directly (`LifestealPart.cs:37` among them).
**Split vote:** one skeptic confirmed as described; the other agreed on
the mechanism but pushed back that the practical risk is speculative
without a concrete heal-an-ally mechanic that reads `BaseValue` today.
**Read as:** a real sharp edge worth a defensive one-line fix
(`Math.Max(hpStat.Min, hpStat.BaseValue - amount)`), not an active bug.

**Status: FIXED.** `CombatSystem.cs`'s `ApplyDamage` now does
`hpStat.BaseValue = Math.Max(hpStat.Min, hpStat.BaseValue - amount)`,
and the same for the `hpAlias` ("HP") stat a few lines below when one
exists and isn't the same `Stat` object. RED-first coverage in
`Assets/Tests/EditMode/Gameplay/Combat/ApplyDamageHpFloorClampTests.cs`:
massive-overkill floor pin (both `Hitpoints` and the `HP` alias) +
counter-check that a normal, non-overkill hit still decrements by the
exact amount.

---

## §3. On-hit dispatch chain

### 🔴 Every on-hit dispatcher requires successful penetration + target survival; Qud's equivalent doesn't
**File:** `CombatSystem.cs:314-318, 360-364, 426-485` vs. Qud's
`Combat.cs:1175-1186` (`WeaponHit` fires before `Penetrations` is even
finalized).
`PerformSingleAttack` returns early on `penetrations == 0` and again on
`damage.Amount <= 0`, and the entire on-hit block (class effects, weapon
specs, gas emission, item enhancements, skill dispatch) is *additionally*
gated on `hpAfter > 0`. In Qud, weapon-mod Parts like `BleedingOnHit` and
the `IMeleeModification` family (`ModFlaming`, `ModElectrified`, ...) fire
on every connecting hit, independent of whether armor blocks all
penetration and independent of whether the hit is lethal.
`GAS-SYSTEM-PLAN.md:901-914` already tracks the survival-gate half of
this as a deferred divergence — but only for `OnHitGasEmit`; the identical
gate on class effects, weapon effects, and (critically) item enhancements
is undocumented.
**Failure:** a Serrated dagger's bleed proc, or an EmberSpear's Burning
spec, never rolls against a heavily-armored target that fully blocks
penetration — and never rolls on the killing blow against *anything*,
regardless of armor, since the target's HP is already 0 by the time the
gate is checked.
**Fix:** split the dispatch — weapon-mod-style effects (class/weapon/gas/
enhancement) should run right after the hit-roll succeeds, before the
penetration/damage early-returns; keep skill-level dispatch and
dismemberment gated on survival, matching Qud's actual split.

### 🔴 `Cudgel_Slam` / `Cudgel_GroundPound` bypass the entire on-hit pipeline
**File:** `Cudgel_Slam.cs:160`; `Cudgel_GroundPound.cs:118`.
Every other active ability in every weapon class (Conk, ChargingStrike,
HookAndDrag, Whirlwind, Backstab, Flurry, Shank, Lunge — 8 confirmed
call sites) delivers damage via `CombatSystem.PerformSingleAttack`. Slam
and GroundPound instead call the raw `ApplyDamage(Entity,int,Entity,Zone)`
overload, which wraps the amount in a zero-attribute `Damage` and skips
the to-hit roll, crit roll, `OnHitClassEffects`, `OnHitWeaponEffects`,
`OnHitGasEmit`, `ItemEnhancementDispatch`, and the skill
`AttackerAfterAttack`/`WeaponMadeCriticalHit` dispatch entirely.
**Failure:** a character with `Cudgel_Bludgeon` (50% stun-on-hit) wielding
a Lifesteal mace casts Slam or GroundPound — full weapon-scaled damage
lands on every target, but the stun never rolls, Lifesteal never heals,
no enhancement or gas effect fires. Nothing in either skill's own
doc-comments mentions this trade-off (contrast `Axe_Whirlwind.cs:161-164`,
which explicitly documents that its hits *do* roll normal on-hit hooks).
Reads as unintentional drift, not a documented design choice.
**Fix:** route through `PerformSingleAttack` per target (accepting a
to-hit roll), or explicitly thread the on-hit pipeline through the raw
path and document the guaranteed-hit/no-procs trade-off the way Whirlwind
does. Either way, pin the decision with a test.

### 🟡 `ChargingStrike`/`Backstab` bonus damage has the same bypass, just quieter
**File:** `Cudgel_ChargingStrike.cs:132`; `ShortBlades_Backstab.cs:160`.
Both correctly route their *base* swing through `PerformSingleAttack`
(procs fire normally), but then apply their bonus damage (+50% momentum,
flank bonus) via the same raw `ApplyDamage` — no second proc roll on the
bonus portion. Backstab's own comment (140-144) acknowledges this as a
deliberate simplification; ChargingStrike's does not.
**Fix:** fold the bonus into the primary attack via a temporary
damage-multiplier hook, or document the trade-off in ChargingStrike to
match Backstab's existing comment.

**Status: FIXED** (real fix, not the documentation fallback — both call
sites already had a validated `MeleeWeaponPart weapon` and a
`System.Random rng` in scope, threaded through the preceding
`PerformSingleAttack` call a few lines earlier). Both skills' bonus-
damage call sites now use `SkillCombatHelpers.DealGuaranteedHitDamage`
(the same helper built for the `Cudgel_Slam`/`Cudgel_GroundPound` fix,
SM8/B2) instead of the raw `ApplyDamage(int)` overload — the bonus
damage now gets its own on-hit dispatch roll, same as the base swing.
Updated both skills' doc-comments (ChargingStrike's class summary;
Backstab's `_isFlanked` field comment and its bonus-damage call-site
comment, which previously described the old raw-`ApplyDamage`
approach) so the two skills' documentation is consistent. Tests: new
`ChargingStrike_BonusDamage_DispatchesItemEnhancementTwice` /
`Backstab_FlankedBonusDamage_DispatchesItemEnhancementTwice`, using a
new `CountingEnhancementProbe` test double (a boolean "fired" probe
can't distinguish one dispatch from two, which is exactly the
distinction this fix needs a test to make) — both assert
`FireCount == 2` (base swing + bonus damage each dispatch once).

### 🟡 `OnHitGasEmit` can't enforce "no damage → no on-hit effect," unlike its 3 siblings
**File:** `OnHitGasEmit.cs:32-47`.
`OnHitClassEffects.Apply` and `OnHitWeaponEffects.Apply` both take
`actualDamage` and explicitly guard `actualDamage <= 0`, with inline
comments naming the rationale ("vetoed/fully-resisted hits don't trigger
on-hit effects"). `OnHitGasEmit.Apply`'s signature has no damage
parameter at all, and the `CombatSystem.cs:444` call site doesn't pass
one — so a fully-resisted 0-damage hit still spawns its on-hit gas
cloud. Currently unreachable in shipped content (no blueprint sets
`EmitGasOnHitRaw` yet), so latent rather than active.
**Fix:** add `Damage damage, int actualDamage` to `OnHitGasEmit.Apply`
and gate it identically to its siblings — both are already in scope at
the call site.

**Status: FIXED.** `OnHitGasEmit.Apply` now takes `Damage damage, int
actualDamage` (matching `OnHitWeaponEffects.Apply`'s parameter order)
and gates on `actualDamage <= 0` as its first check, mirroring the
siblings' wording/placement exactly. All 3 real call sites now pass
both args: `CombatSystem.cs`'s melee dispatch, `SkillCombatHelpers
.DealGuaranteedHitDamage`, and `ThrowItemCommand.cs`'s thrown-weapon
on-hit dispatch (the last of these had `actualDamage` in scope but
previously wasn't threading it into `OnHitGasEmit.Apply` at all).
Tests in `OnHitGasEmitTests.cs` §PART V: counter-check that a landed,
non-resisted hit still spawns gas as before, plus new coverage that
`actualDamage <= 0` (fully-resisted or negative) spawns none and
emits no diag record.

### 🔵 `ItemEnhancementDispatch`'s docstring names the wrong predecessor
**File:** `ItemEnhancementDispatch.cs:38-40`.
Claims it fires "right after `OnHitWeaponEffects.Apply`"; the actual
order is `OnHitWeaponEffects` (437) → `OnHitGasEmit` (444) →
`ItemEnhancementDispatch` (453) — gas sits between the two.
Doc-vs-impl drift, no runtime effect. **Fix:** update the comment.

---

## §4. Weapon skills

### 🔵 `Cudgel_Disarm`'s planning doc claims a mechanic that was never shipped
**File:** `Cudgel_Disarm.cs:45-123` vs. `Docs/SKILL-ACTIVES-BRAINSTORM.md:126-135`.
The brainstorm doc marks Disarm "✅ Shipped" with "target spends 1T
re-equipping." The shipped `OnCommand` unequips and drops the weapon —
no turn-cost or re-equip-delay of any kind is applied to the target.
**Fix:** either implement the 1-turn cost or amend the doc to record it
as a scoped-out clause, the way other entries in the same doc already
record deliberate cuts.

*(The weapon-skills dimension's other high-severity findings —
`Cudgel_Slam`/`Cudgel_GroundPound` bypassing the pipeline — are filed
under §3 since they're fundamentally on-hit-dispatch bugs.)*

---

## §5. Performance

### 🔴 `Body.GetParts()` allocates a fresh List + full recursive tree walk on every call — twice per attack, unconditionally
**File:** `CombatSystem.cs:510` (`GatherMeleeWeapons`), `1228`
(`SelectHitLocation`); `Body.cs:73-77` → `BodyPart.cs:356-367`.
Both call sites call `body.GetParts()` with no result-list argument;
`BodyPart.GetParts(result: null)` always allocates
`new List<BodyPart>()` and walks the whole tree. `GetParts` already
supports a `result` parameter to avoid this — grep confirms **no caller
anywhere in the codebase** ever passes one. The prior perf investigation's
"Fix #6" (`GatherMeleeWeapons` scratch list) wrapped the outer
`List<WeaponSlot>` but never touched the `body.GetParts()` call supplying
its source data — the bigger allocation survived that fix untouched.
**Failure:** in the populated-combat scenario the original investigation
profiled (4-5 NPCs), one full swing-cycle triggers 3+ recursive tree-walk
allocations; with 10 attacking NPCs per turn, 20-30 allocations/turn —
exactly the recurring-`GC.Alloc` pattern the project's own perf doc says
isolated tests miss.
**Fix:** add a static scratch `List<BodyPart>` (mirroring
`_gatherWeaponsScratch`) and call `body.GetParts(scratchList)` after
`.Clear()` in both call sites. Direct application of this project's own
documented Pattern 1, zero behavior change.

### 🟡 `GetDV`/`GetAV` allocate a closure + delegate on every single attack
**File:** `CombatSystem.cs:570-596, 602-639`.
Both mutate a captured local (`bestDV`/`totalAV`) inside a lambda passed
to `body.ForeachEquippedObject` — the capture forces a display-class +
delegate allocation that can't be cached, since the captured state
differs per call. `GetDV` runs unconditionally on every melee swing
against a Body-having defender (essentially all of them).
**Fix:** replace with a plain non-capturing for-loop over
`body.GetEquippedParts()` (or add a struct-visitor overload to
`ForeachEquippedObject`).

### 🟡 5 `gas`-category diag call sites are missing the `IsChannelEnabled` guard every other category uses
**File:** `PoisonedByGasEffect.cs:63-64, 71-72`; `OnHitGasEmit.cs:71-82`;
`FungalInfectionEffect.cs:208-217, 227-232`.
Every other audited call site (`CombatSystem.cs`, `StatusEffectsPart.cs`,
every `EnhancementXxx.cs`) correctly wraps `Diag.Record` in
`if (Diag.IsChannelEnabled(...))` — `Diag.cs`'s own docstring explains why
(the anonymous-object payload allocation can't be elided otherwise).
These 5 `gas`-category sites don't, and `gas` is on by default, so this
fires in normal play, not just theoretically.
**Fix:** wrap each in the guard. Mechanical, ~5 call sites.

### 🔵 `WeaponSlot` is a class, so the "scratch list" perf fix still allocates one object per weapon per attack
**File:** `CombatSystem.cs:525, 541, 1327-1332`.
The Tier-B scratch-list fix reused the outer `List<WeaponSlot>`, but each
`new WeaponSlot { ... }` element is still a fresh heap allocation — the
fix's own doc comment addresses the container, not the elements.
**Fix:** convert `WeaponSlot` to a struct (one in-place mutation site
needs to become read-modify-write on the indexer).

*(One performance-dimension candidate — an unconditional `Guid`/
`Diag.WithCause` allocation at the top of every attack — was **refuted**:
see §8.)*

---

## §6. Qud-parity drift

*(§3's "on-hit dispatch requires survival" finding is also, at its root,
a Qud-parity finding — Qud's `WeaponHit` timing genuinely differs. Filed
under §3 since its primary impact is dispatch correctness.)*

### 🟡 Off-hand attacks are a different mechanic from Qud's, with an overclaiming comment
**File:** `CombatSystem.cs:23-45, 169-170` vs. Qud's `Combat.cs:741-791`,
`Stats.cs:55-58` (`RuleSettings.BASE_SECONDARY_ATTACK_CHANCE = 15`).
CoO's off-hand always swings, at a flat -2 to-hit, with a comment
claiming this "mirrors Qud's secondary weapon hit penalty." Qud's actual
mechanic is a **~15% chance the off-hand attacks at all** (rolled per
turn, can exceed 100% with dual-wield investment) — if it does attack,
no separate penalty applies. This is a real balance divergence, not just
a comment nit: a naked (unskilled) dual-wielder in CoO swings both
weapons every single turn at near-full accuracy, dramatically stronger
than the Qud baseline the comment claims to mirror.
**Fix:** either drop the "mirrors Qud" framing and own it as a CoO
simplification, or port the percent-chance-to-swing model.

### 🟡 No per-weapon post-roll veto/mutate hook (Qud's `AttackerHit`/`WeaponHit` chain has no CoO equivalent)
**File:** `CombatSystem.cs:76-80` vs. Qud's `Combat.cs:1143-1212`.
CoO's only pre-damage vetoable hook (`BeforeMeleeAttack`) fires once per
whole multi-weapon turn, before weapons are even gathered. Qud fires a
per-weapon event chain *after* the penetration roll but *before* damage,
letting Parts veto the hit or mutate the already-rolled penetration
count. No CoO event does this today. Latent — no current content (shield
block, parry, ward) needs it yet, but it's the first thing that will be
missing the moment one does.
**Fix:** when a shield/parry/ward mechanic is planned, add the per-weapon
hook then, mirroring Qud's timing rather than extending the single
whole-turn gate.

---

## §7. Observability (8 confirmed — every silent gate in the core pipeline)

Per this project's own rule ("every gate that can reject emits a
diag record"), the 8 findings below are all the same shape: a real
success/failure branch in the combat/effect pipeline that leaves no
trace for `diag_query`. Grouped as a table since the fix is uniform.

| Gate | File:line | Why it matters |
|---|---|---|
| `PerformMeleeAttack`'s `BeforeMeleeAttack` veto | `CombatSystem.cs:76-80` | The *first* gate in the whole pipeline — a veto here means the entire downstream diag chain for that attack never exists, not even a stub |
| `BeforeTakeDamage` veto / full-resist absorb | `CombatSystem.cs:786-821` | No single `kind` answers "was this attack fully resisted" — must manually correlate two other records by CauseTraceId |
| `ApplyDamage`'s 2 guard clauses (already-dead, zero-amount) | `CombatSystem.cs:715-738` | Guards a *previously real, adversarial-test-caught* double-death bug, but the guard firing today leaves zero trace |
| `HandleDeath`'s whole kill lifecycle | `CombatSystem.cs:1046-1127` | XP award / loot drop / Died event / witness broadcast all silent — only signal is `DamageDealt.lethal=true` |
| `CheckCombatDismemberment` (threshold / chance / veto) | `CombatSystem.cs:1290-1321` | The single largest coverage gap `COMBAT-BRANCH-MAP.md` already flagged, and Phase H built the veto hook — but its outcomes are invisible |
| `OnHitClassEffects`'s 3 chance rolls (Stun/Bleed/Confuse) | `OnHitClassEffects.cs:90-112` | Zero `Diag` usage in the whole 114-line file — "my Cudgel never stuns" has no diagnostic path |
| `OnHitWeaponEffects`'s chance roll + unknown-name drop | `OnHitWeaponEffects.cs:37-49` | A typo'd `EffectName` in a weapon blueprint fails **silently forever** — not a compile error, not a warning, not a diag record |
| `StatusEffectsPart`'s `OnStack` gate + 2 pre-apply reject gates | `StatusEffectsPart.cs:57-72` | 23 concrete effects override `OnStack` with materially different behavior (extend/ignore/refresh) — none of it is queryable |

**Fix (uniform):** add `Diag.Record(category: "damage"|"effect", kind: "<Gate>Rejected"|"<Gate>Fired", ...)` at each site, gated by
`IsChannelEnabled`, matching the style already used correctly elsewhere
in the same files. All 8 are mechanical, additive, and independently
shippable.

---

## §8. Test-coverage gaps — 2 of these are regressions in *today's* stun-save work

### 🔴 `StunnedEffect.OnStack`'s "keep the harder save" merge treats `SaveTarget=0` as the numerically weakest DC, not as its own "no save" sentinel
**File:** `StunnedEffect.cs:79-81` — ✅ hand-verified.
`if (stun.SaveTarget > SaveTarget) SaveTarget = stun.SaveTarget;`.
`SaveTarget=0` is documented as "save disabled, deterministic countdown"
— a distinct mode, not "DC 0." Nearly every stun call site uses the
default (`saveTarget=0`): `Cudgel_Slam`, `Cudgel_Conk`,
`Cudgel_GroundPound`, `GasStunPart`, `ChainLightningMutation`,
`IceShardMutation`, `FrostNovaMutation`, bear traps. Meanwhile
`OnHitClassEffects.TryApplyStunned` constructs a `saveTarget=16` stun for
its passive 15%-on-Bludgeoning-hit proc — and Cudgel weapons carry the
Bludgeoning attribute, so a Cudgel weapon's own ordinary swings roll this
proc alongside its skills.
**Failure:** `Cudgel_Slam` lands its intentionally-guaranteed,
non-escapable stun (`saveTarget=0`). A later swing with the same weapon
class procs the passive Bludgeoning stun (`saveTarget=16`) on the same
target. `OnStack` sees `16 > 0` and flips `SaveTarget` from 0 to 16 —
the guaranteed skill stun becomes saveable, purely as a side effect of
an unrelated passive proc, with no change to `Cudgel_Slam` itself.
**Fix:** treat 0 as a sentinel, not a magnitude — merge should be
`if (SaveTarget == 0 || stun.SaveTarget == 0) SaveTarget = 0; else SaveTarget = Math.Max(SaveTarget, stun.SaveTarget);`
(or the opposite policy if "saveable always wins" is the intended
design — but that should be an explicit, tested decision either way).

### 🔴 `OnHitEffectFactory`'s `"stunned"` case never wires `Magnitude → SaveTarget` — the exact bug class already fixed for `Bleeding` in the same file
**File:** `OnHitEffectFactory.cs:68-71` — ✅ hand-verified.
`case "stunned": return new StunnedEffect(duration: ...)` — only
`Duration` is forwarded. The adjacent `"bleeding"` case (73-87) *already*
had this exact bug (Magnitude silently dropped) and was fixed with an
explicit comment explaining the fix and a regression test — the same
pattern was never reapplied three lines down for Stunned, despite the
spec format's own canonical docstring example being
`"Burning,30,,5,1.0;Stunned,5,,1,0"`.
**Failure:** a future weapon blueprint authoring `"Stunned,20,,2,16"`
(intending a DC-16 saveable stun, mirroring how Bleeding specs use
Magnitude) gets a fully deterministic, unsaveable stun instead — silently.
**Fix:** mirror the Bleeding case:
`new StunnedEffect(duration: ..., saveTarget: spec.Magnitude > 0f ? (int)spec.Magnitude : 0, rng: rng)`.
Add a test analogous to the existing Bleeding-magnitude regression test.

### 🟡 `OnHitClassEffectsTests` never asserts the Bludgeoning stun actually carries its `SaveTarget`
**File:** `Tests/.../OnHitClassEffectsTests.cs:32-49`.
Both existing tests read only `.Duration` off the resulting effect. A
future edit that reverts `TryApplyStunned` to
`new StunnedEffect(BLUDGEONING_STUN_DURATION)` (dropping the save,
e.g. via a refactor or a copy-paste from `GasStunPart`'s save-less call)
would pass every existing test silently.
**Fix:** add an assertion reading `.SaveTarget` and comparing to
`BLUDGEONING_STUN_SAVE_TARGET`.

### 🔵 `StunnedEffect` has no analog of `BleedingEffect`'s "save target eases over time" regression test
**File:** `StunnedEffect.cs:49-51` vs. `StatusEffectTests.cs:788`
(`Bleeding_SaveTargetDecreasesOverTime`).
The existing `StunnedEffectSaveTests` never reads `.SaveTarget` after a
failed turn, so the `if (SaveTarget > 1) SaveTarget--;` easing line could
be broken (wrong comparison, off-by-one, deleted) with no test noticing.
**Fix:** add the mirrored test.

*(Test-density context: 439 Combat tests, 228 Effects tests, ~470 Skills
tests — high overall, and the stun-save feature and removal-cause fix
both landed with dedicated test files. These 4 gaps are specifically
about the newest, least-adversarially-hardened surface.)*

---

## §9. Refuted (kept for transparency, no action needed)

- ~~`BeforeTakeDamage`'s comment claims it "mirrors Qud's
  `BeforeApplyDamageEvent`" but the ordering relative to resistance is
  backwards~~ — both skeptics independently confirmed the *code* order is
  as claimed, but reading Qud's `Physics.cs:3418` directly showed the
  comment's characterization of Qud's own ordering was itself correct;
  the claimed mismatch didn't hold up.
- ~~`PerformSingleAttack` allocates a `Guid` + opens a `Diag.WithCause`
  scope unconditionally, unlike the gated `Diag.Record` calls further
  down~~ — confirmed true as a factual observation, but refuted on
  materiality: `Diag.WithCause` is a lightweight `AsyncLocal` scope, not
  a payload allocation, and the project's own gating convention is
  specifically about avoiding the *anonymous-object* allocation
  (per `Diag.cs`'s docstring) — this call site was never meant to be
  gated the same way.

---

## Appendix: methodology detail

- **Dimensions:** damage-pipeline, on-hit dispatch, status-effect
  lifecycle, weapon-skill consistency, performance, Qud parity, test
  coverage, observability/diag.
- **Scope pass** mapped: `CombatSystem.cs` (1334 lines, grown from 677
  since the 2026-04-26 audit — `COMBAT-BRANCH-MAP.md`'s line numbers are
  now stale), `Damage.cs`, `LineTargeting.cs`/`SpellTargeting.cs`,
  `OnHitClassEffects.cs`/`OnHitWeaponEffects.cs`/`OnHitGasEmit.cs`,
  `Effect.cs`/`StatusEffectsPart.cs` + 30 concrete effects, the flat
  75-file `Skills/` directory (11 skills each for ShortBlades/Cudgel, 9
  for Axe, plus Pyromancy/Cryomancy/Spellcraft/Galvanism/Acrobatics/
  Corrosion/Persuasion/LongBlades), and ~1137 existing EditMode tests
  across Combat/Effects/Skills.
- **Existing docs cross-checked:** `COMBAT-AUDIT-PLAN.md`,
  `COMBAT-BRANCH-MAP.md`, `COMBAT-TEST-BACKLOG.md`,
  `COMBAT-QUD-PARITY-PORT.md` (Phases A-H, merged, 2087→2181 tests,
  zero regressions), `PERF-FOUNDATION.md`, `PERF-COMBAT-INVESTIGATION.md`.
  The 2026-04-26 audit's own baseline (~29% branch coverage, 12.5%
  bug-find rate on `ApplyDamage`) is the historical comparison point —
  this pass's 35/39 (90%) confirm rate on 8 targeted dimensions plus a
  completeness pass reflects the narrower, more targeted framing versus
  a blind branch sweep.
- **Verdict counts:** 35 CONFIRMED, 2 PLAUSIBLE (split vote), 2 REFUTED.
- **Scale:** 88 agents, 1456 tool calls, ~7.43M tokens, all general-purpose
  agents at high reasoning effort, two independent skeptics per finding.
