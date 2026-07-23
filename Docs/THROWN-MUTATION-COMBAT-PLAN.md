# Thrown-Weapon & Mutation Line-Attack Combat — Depth Plan

> Living plan for fixing the gap `COMBAT-SYSTEM-AUDIT-2026-07.md` §1 flagged as
> the single highest-leverage finding: thrown weapons and mutation/spell
> line-attacks bypass the combat depth melee gets. **Plan only — nothing
> implemented yet.** Sub-milestones execute in order once this plan is
> confirmed.

**Status:** 📋 PLANNED, NOT STARTED.
**Origin:** user directive, 2026-07-22 — "fix and amend the lack of depth for
thrown weapons and mutations... create a comprehensive plan."
**Verification method:** a 6-agent read-only research pass (exact current
CoO code for both systems + the retaliation system + Qud decompile source
for both systems' real mechanics) ran before this plan was written. Three
of its findings **overturn assumptions in the original audit** — see the
corrections table immediately below. This plan is written against the
corrected understanding, not the audit's original framing.

---

## 0. Pre-implementation verification sweep — corrections table

Per this project's mandatory pre-impl verification step: every premise
below was checked against the actual source (CoO and Qud) before being
allowed into the plan.

| # | Original premise | Correction | Source |
|---|---|---|---|
| 1 | "Zero test coverage" for both ThrowItemCommand's damage branch and DirectionalProjectileMutationBase's damage branch (audit §1) | **False.** Both have direct tests: `InventorySystemTests.cs:628-672` pins thrown weapon-damage and improvised-weight-damage; `ProjectileMutationTests.cs:54-89` pins FireBolt's damage roll. Coverage is narrow (single scenario, no armor/resistance interaction, no exact-log-text assertion) — that's the real gap, not "zero." | test-observability-current research |
| 2 | "Add a to-hit-vs-DV check like melee" is the Qud-parity-correct fix for thrown weapons | **False — would be an overclaimed-parity bug in the other direction.** Qud's own thrown weapons never check DV either (`GameObject.PerformThrow`, `XRL.World/GameObject.cs:14793-15226` — grepped the full ~430-line method for `Stat.Random(1,20)` / `GetCombatDV` patterns, zero matches). Qud's actual mechanic: `RollPenetratingSuccesses("1d" + Agility, 3)` — a die *sized to the thrower's Agility score*, rolled once, producing a "hit budget" that depletes as the projectile crosses cells (`num2 -= num6` per cell of travel). Whether a throw connects is a **distance-vs-Agility** question, never a **DV** question. | qud-thrown-parity research, ✅ hand-verified reasoning below |
| 3 | Mutation/spell line-attacks should get an accuracy roll added | **False — would be a Qud-parity regression, not a fix.** Every Qud line/cone/beam mutation actually read — `BreatherBase`+`FireBreather` (cone), `FreezeBreath` (line), `FreezingRay` (line), `FlamingRay` (line) — applies **guaranteed** damage to every `Combat`-bearing occupant of every cell reached, gated only by phase-match and (for beams) whether a solid obstacle stops the beam's *travel* — never by a per-target accuracy/dodge roll. CoO's current single-target guaranteed-hit-if-traced already matches Qud's real accuracy model for the one target it hits. The genuine Qud-parity gap is **area coverage** (Qud hits everyone in the cone/line; CoO's `TraceFirstImpact` stops at the first entity) — a scope decision, not a bug. See §3. | qud-mutation-parity research |
| 4 | "`AddPersonalEnemy`" is the retaliation API (audit §1 wording) | **Wrong name.** That symbol doesn't exist anywhere in the codebase. The real API is `BrainPart.SetPersonallyHostile(Entity)`, backed by the public `PersonalEnemies` HashSet. | retaliation-current research |
| 5 | Retaliation has "exactly one call site" | **Confirmed, with a nuance.** One true production (player-input) call site: `InputHandler.ExecuteAttackOnNPC:3389-3399`, fired unconditionally *before* the attack resolves (fires even on a miss). A second hit, `EntityBuilder.cs:447`, is scenario/manual-playtest content-authoring scaffolding, not a live gameplay path. | retaliation-current research |

**What this means for scope:** the plan below gives thrown weapons a
**new, Qud-faithful accuracy mechanic** (distance/Agility-based, explicitly
*not* a DV check) and gives mutations **no accuracy mechanic at all** — the
fix for mutations is entirely about the message-log lie, diag, and
retaliation, none of which touch hit/miss.

---

## 1. Thrown weapons

### 1.1 Current state (verified)

`ThrowItemCommand.cs:112-298` → `LineTargeting.TraceFirstImpactToTarget`
(pure geometry, no probability) → on a creature hit, `GetThrownDamage()`
(`ThrowItemCommand.cs:392-400`, a private helper — dice-rolls the thrown
item's `MeleeWeaponPart.BaseDamage` if present, else an improvised-weapon
weight formula) → `CombatSystem.ApplyDamage(hitTarget, damage, actor, zone)`
— the **raw int overload** (`CombatSystem.cs:930`), which wraps the amount
in `new Damage(amount)` with **zero attributes** and forwards to the
primary typed overload (`CombatSystem.cs:715`).

Confirmed consequences of the zero-attribute wrap:
- `ApplyResistances` never fires for thrown damage — a thrown Fire-tagged
  dagger deals fully unresisted flat damage regardless of the target's
  HeatResistance, unlike an otherwise-identical melee weapon
  (`CombatSystem.cs:320-329` explicitly copies `weapon.Attributes` onto
  the `Damage` object for melee; nothing analogous exists in
  `GetThrownDamage`).
- None of `OnHitClassEffects` / `OnHitWeaponEffects` / `OnHitGasEmit` /
  `ItemEnhancementDispatch` fire — a thrown Serrated dagger doesn't bleed;
  a thrown Lifesteal weapon doesn't heal its thrower.
- The message log shows the raw dice roll, not the post-resistance HP
  delta (same bug class as the melee bug already fixed via the
  `hpBefore`/`hpAfter` pattern at `CombatSystem.cs:378-399`).
- No hit/miss mechanic exists at all — a successful geometric trace to a
  creature is a guaranteed hit for guaranteed damage.
- `ThrowItemCommand.cs:140` uses an **unseeded** `new Random()` — every
  other combat entry point (melee via `InputHandler._combatRng:76`, a
  persistent seedable field reused across calls) threads an injected
  `Random`. This blocks deterministic testing of anything probability-based
  added to this path.

### 1.2 Design decisions

**D1 — Give thrown weapons a real Damage object.** `GetThrownDamage`'s
result gets wrapped in `new Damage(amount)` with `AddAttribute("Thrown")`
(parallel to melee's `"Melee"` tag) and `AddAttributes(weapon.Attributes)`
when the thrown item has a `MeleeWeaponPart` with populated `Attributes`.
Route through the **typed** `ApplyDamage(Entity, Damage, Entity, Zone)`
overload instead of the raw-int one. This alone fixes the resistance
bypass, independent of the accuracy question below.

**D2 — Route thrown hits through the on-hit dispatch chain**, gated
identically to melee (`actualDamage > 0`, mirroring
`CombatSystem.cs:426`): `OnHitClassEffects.Apply`, `OnHitWeaponEffects.Apply`,
`OnHitGasEmit.Apply` (for a thrown item with a gas spec), and
`ItemEnhancementDispatch.DispatchOnHit`. Thrown weapons carry real
`MeleeWeaponPart`-backed identity today (`GetThrownDamage` already reads
it) — there's no mechanical reason a thrown Serrated dagger shouldn't
bleed the same as a melee-swung one. **Not in scope:** `SkillEventDispatcher`'s
`AttackerAfterAttack`/`WeaponMadeCriticalHit` — those are skill-proc hooks
tied to the *attacking creature's* active-ability rotation, not weapon
identity; wiring them to a thrown item opens a design question (do skills
fire off a thrown weapon the same as a wielded one?) this plan defers.

**D3 — LOCKED, 2026-07-22 (Option A, no trajectory wobble).** Give thrown
weapons a real accuracy mechanic — a **direct, near-literal port of
Qud's actual Layer-1 check**, not melee's DV roll. This section
supersedes an earlier draft of this plan that mischaracterized Qud's
mechanic as "a hit budget that depletes with distance traveled" — that
was an over-read of a research paraphrase. Direct verification against
`XRL.Rules/Stat.cs:1090-1159` and `XRL.World/GameObject.cs:14869-15092`
found Qud's real mechanic has three genuinely separate layers, and only
the first is in scope here:

1. **The accuracy-eligibility roll (IN SCOPE):**
   `RollPenetratingSuccesses("1d" + Agility, 3)` — computed **once, at
   the start of the throw**, not per cell of travel. With dice-string
   `"1d" + Agility`, that's one die whose **size** equals the thrower's
   Agility score; success if the roll is ≥ 3. For any Agility ≥ 3 this
   collapses to a clean closed form (the "explode on max" clause only
   ever engages for Agility < 3, i.e. never in practice):
   `P(hit) = (Agility − 2) / Agility` — 50% at Agility 4, 75% at 8,
   87.5% at 16, 90% at 20, asymptotically approaching but never
   reaching 100% as Agility grows.
2. **Trajectory wobble (`AimVariance`) — EXPLICITLY OUT OF SCOPE.** A
   separate mechanic that can make the projectile's flight path drift
   from the intended cell entirely (`GameObject.cs:14876-14891`,
   feeding `MissileWeapon.CalculateBulletTrajectory`). CoO's
   `LineTargeting` is a clean, deterministic Bresenham trace with no
   concept of "might veer into the wrong cell" — porting this would be
   a distinct, much larger feature (a badly-aimed throw hitting a wall
   or a bystander instead), not part of this fix. **User confirmed:
   no trajectory wobble.**
3. **Damage severity — ⚠️ CORRECTED 2026-07-22, was wrong when first
   written.** The original draft of this bullet asserted this layer was
   "already effectively ported" and needed "no new code," on the theory
   that Qud's `RollDamagePenetrations` is the same algorithm
   `CombatSystem.RollPenetrations` already implements. That part is true
   (same algorithm), but the draft conflated "the algorithm already
   exists in the codebase" with "`ThrowItemCommand.GetThrownDamage`
   already calls it" — verified FALSE by direct code read during SM5's
   post-implementation adversarial-sweep prep:
   `GetThrownDamage` (`ThrowItemCommand.cs:475` as of SM5) was, and
   until SM5c below remained, a flat
   `DiceRoller.Roll(weapon.BaseDamage, rng) + strengthBonus` with **zero**
   call to `RollPenetrations`/`GetAV`/`GetPartAV` anywhere in the thrown
   path — meaning thrown-weapon damage severity bypassed the target's
   armor (AV) entirely, unlike melee and unlike Qud's real mechanic.
   Surfaced to the user 2026-07-22; user chose **"wire in
   RollPenetrations+AV"** over "leave as documented divergence" — see
   **SM5c** below, a sub-milestone this plan's original §1.3 list never
   scheduled (an oversight this correction also fixes).

**Implementation shape:**
- `bool hit = DiceRoller.Roll("1d" + agilityScore, rng) >= 3;` — one
  roll, reusing `DiceRoller`'s existing arbitrary-die-size parsing. No
  new dice-math needed.
- **Explicitly does not read the defender's DV or AV for the hit/miss
  gate** — this is a deliberate, permanent divergence from melee, not
  an oversight; document it inline (per this project's §4.3 divergence-table
  convention) so a future contributor doesn't "symmetry-fix" it into a
  DV check by analogy with melee.
- Range is already a separate, pre-existing gate
  (`HandlingService.GetThrowRange`/`CanThrow`) in both games — the
  accuracy roll does not additionally scale with distance, matching
  Qud's real (verified) behavior exactly.
- Damage severity routes through `GetAV`/`GetPartAV` + `RollPenetrations`
  exactly like melee (see **SM5c** below) — this matches Qud's own reuse
  of the same math for both systems, confirmed by direct comparison of
  the two function bodies. `bonus`/`maxBonus` mirror melee's inputs
  (`StatUtils.GetModifier(actor, weapon.Stat)` + weapon `PenBonus`/
  `MaxStrengthBonus`) **excluding** `skillPenBonus`
  (`SkillEventDispatcher.GetSkillPenetrationModifier`) and crit/AutoPen —
  both are deliberately deferred together with D2's other skill-hook
  exclusions, for the same reason: "do skills fire off a thrown weapon
  the same as a wielded one?" is an open design question this plan
  doesn't answer, so no skill hook gets partial, inconsistent wiring
  while that question is unresolved.

**D4 — Miss behavior:** fold an accuracy-miss into the existing
"no `HitEntity`" landing path (`ThrowItemCommand.cs:224-261`) rather than
inventing new behavior — a missed throw continues to the traced
impact/traversable cell and lands there, identical to today's
`BlockedBySolid`/empty-cell case.

**D5 — Fix the message log** to report the actual post-resistance HP
delta (`hpBefore`/`hpAfter`), mirroring the melee fix at
`CombatSystem.cs:378-399` exactly — same pattern, same wording
conventions.

**D6 — Seed the RNG.** Add an optional `System.Random rng = null`
constructor parameter to `ThrowItemCommand` (defaults to a fresh
`Random()` if not supplied, so any other callers keep compiling); update
the confirmed call site `InputHandler.cs:2073` to pass `_combatRng` — the
same persistent field melee already reuses. (`InventorySystem.cs:64`'s
convenience wrapper call stays on the default for now — no combat RNG is
threaded through that path today, and it isn't part of this fix's blast
radius; flagged as a loose end, not silently ignored.)

**D7 — Diag.** New category-`"damage"` kinds (reusing the existing
default-on category, matching melee's shape so `diag_query category=damage`
stays one unified query surface): `kind="ThrowHitRoll"` (payload: weapon,
agilityScore, roll, target=3, landed) and `kind="ThrowPenetration"`
(payload: mirrors melee's `Penetration` kind). `DamageDealt` already fires
generically from `ApplyDamage` — no new kind needed there.

### 1.3 Sub-milestones (smallest blast radius first)

1. **SM1 — Damage typing fix (D1).** Wrap in a real `Damage` object,
   route through the typed `ApplyDamage` overload. Zero accuracy-model
   change. New test: throw a Fire-attributed weapon at a
   Heat-Resistant target, assert resistance actually reduces the HP
   delta (impossible to assert today — this is a genuine RED test).
   Existing `InventorySystemTests` fixtures (no resistance on
   `TargetDummy`) stay green untouched.
2. **SM2 — Message-log fix (D5).** Isolated, no dependency on SM1's
   Damage-object change (works off the `hpBefore`/`hpAfter` delta
   regardless of attribute tagging).
3. **SM3 — On-hit dispatch (D2).** Depends on SM1 (needs the typed
   `Damage` object and `actualDamage` value dispatch reads). New tests:
   a thrown Serrated dagger bleeds; a thrown weapon with a class-tagged
   damage attribute (Bludgeoning/Cutting/Piercing) rolls
   `OnHitClassEffects` the same as melee.
4. **SM4 — RNG seeding (D6).** Small, mechanical, unblocks SM5's
   deterministic tests. Do this *before* SM5, not after.
5. **SM5 — Accuracy mechanic (D3 + D4).** Depends on SM4 (needs seeded
   RNG for deterministic hit/miss tests). One roll:
   `DiceRoller.Roll("1d" + agilityScore, rng) >= 3`. Tests: Agility 3
   (P=1/3, boundary case — 1d3 rolling exactly 3 is the only success),
   Agility 4 (P=50%, exact-coin-flip determinism via a seeded rng),
   Agility 20 (P=90%), Agility 1-2 (degenerate low end — confirm no
   crash/infinite-loop via `DiceRoller`'s own handling of a die smaller
   than the target), miss-lands-at-traced-cell behavior (D4), and a
   counter-check that damage severity (RollPenetrations/GetAV) is
   completely unaffected by the accuracy roll's outcome distribution
   (i.e. the two rolls are independent).

   **⚠️ As shipped, SM5 only implemented the accuracy roll (D3 Layer 1) +
   miss-landing (D4); the RollPenetrations/GetAV counter-check above was
   never written because the damage-severity wiring it depends on
   (D3 Layer 3) didn't exist yet — see SM5c below, added post-hoc to
   close this gap.**
6. **✅ SHIPPED 2026-07-22 — SM5c — Damage severity via `RollPenetrations` +
   AV (D3 Layer 3, corrected).** Added 2026-07-22 after the adversarial-sweep prep read
   surfaced that `GetThrownDamage` never actually called
   `RollPenetrations`/`GetAV`/`GetPartAV` — see the corrected Layer-3
   bullet above. `GetThrownDamage` gains a `target` parameter; computes
   `bonus = strMod + weapon.PenBonus`, `maxBonus = effectiveMaxStrBonus +
   weapon.PenBonus` (mirroring melee's non-crit, non-skill inputs
   exactly — see the "Implementation shape" note above for the
   skill-bonus/crit exclusion rationale), selects a hit location via
   `SelectHitLocation`/`GetPartAV` when the target has a `Body` (else
   falls back to `GetAV`), rolls `penetrations = RollPenetrations(av,
   bonus, maxBonus, rng)`, and — if `penetrations == 0` — returns 0 (a
   NEW failure mode, "hits but fails to penetrate," distinct from an
   accuracy miss, needing its own message so it isn't a silent no-op).
   Otherwise sums one `DiceRoller.Roll(weapon.BaseDamage, rng)` per
   penetration (or `perPenetration = Max(1, Ceil(weight/2))` × penetrations
   for the improvised-item fallback, which has no dice expression). Also
   closes **D7's `ThrowPenetration` diag kind** (natural to add alongside
   this exact code) **and `ThrowHitRoll`** (the accuracy-roll diag,
   independently addable to SM5's existing `RollThrowAccuracy`, folded in
   here since both close the same D7 decision). Tests: high-AV target
   takes less damage than a 0-AV target at an identical seed (monotonic —
   guaranteed by `RollPenetrations`' math given identical underlying die
   draws, not a statistical fluke); fails-to-penetrate produces a distinct
   message, not silence; PenBonus/Strength genuinely raise the penetration
   count (counter-check with a weak vs. strong attacker at the same AV);
   improvised (no-`MeleeWeaponPart`) throws still route through the same
   mechanism; `ThrowPenetration`/`ThrowHitRoll` diag payload shape pins.
   **Shipped:** 6 new tests + 5 pre-existing `InventorySystemTests` pins
   re-computed (their exact HP values were derived from the old flat
   dice+strength formula; the new penetration-based formula is still
   fully deterministic per-seed, just a different number — same seed,
   re-verified new value, not a loosened assertion). 284/284
   `InventorySystemTests` + 43/43 tonic/adversarial thrown-command tests
   (unaffected — tonics/grenades and empty-cell/wall throws never reach
   this code path) + 86/86 combat/mutation/retaliation suites all GREEN.
7. **Adversarial sweep** (mandatory gate — see §4) after SM5c.

---

## 2. Mutation / spell line-attacks (`DirectionalProjectileMutationBase`)

### 2.1 Current state (verified)

9 concrete subclasses (`QuenchMutation`, `KindleMutation`, `CalmMutation`,
`PoisonSpitMutation`, `AcidSprayMutation`, `IceLanceMutation`,
`ArcBoltMutation`, `IceShardMutation`, `FireBoltMutation`) — all
single-target directed bolts that only override the 7 abstract properties
(`DamageDice`, `ElementAttribute`, etc.) plus `ApplyOnHitEffect`; **none**
override `Cast()` itself, so a base-class fix reaches all 9 uniformly.
(True multi-target beams/AOE — `PrismaticBeamMutation`, `FrostNovaMutation`,
`ChainLightningMutation`, `ThunderclapMutation` — extend `BaseMutation`
directly, not this class, and are **out of scope** for this plan.)

`Cast()` (`DirectionalProjectileMutationBase.cs:69-124`): rolls
`DiceRoller.Roll(DamageDice, rng)` (line 91) → **logs the raw roll to
MessageLog immediately** (lines 94-96) → *then* calls
`MutationDamageHelpers.ApplySpellDamage(target, damage, ElementAttribute,
ParentEntity, zone)` (line 106-107), which internally does the correct
`hpBefore`/`ApplyDamage`/`hpAfter` delta computation and **returns the true
post-resistance damage** (`MutationDamageHelpers.cs:79-82) — **but the
caller discards that return value.** This is the exact bug class already
fixed for melee (`CombatSystem.cs:366-377`'s own comment documents the
original melee bug in these words: *"the HP bar told the truth, the log
lied"*) — unfixed here.

`LineTargeting.TraceFirstImpact` stops at the first entity/solid obstacle
— single-target by construction, consistent across all 9 subclasses.

No accuracy roll exists anywhere in this path — confirmed as the
**correct** Qud-parity stance per §0 correction #3, not a gap.

### 2.2 Design decisions

**D8 — Do NOT add an accuracy/dodge roll.** This is the load-bearing
correction from this plan's verification sweep. Adding one would make
CoO's mutations *less* Qud-faithful, not more. Document this explicitly
in the class's own doc-comment so a future contributor doesn't
"symmetry-fix" it against melee by analogy (CLAUDE.md's own Q1 symmetry
check would otherwise flag "mutations don't roll to-hit like melee does"
as a false-positive divergence — pre-empt that here).

**D9 — Fix the message log**, mirroring melee's exact pattern: capture
`ApplySpellDamage`'s return value (the true post-resistance delta) and
use it in the log line instead of the raw pre-resistance `damage` local.
This also automatically folds in any skill-based spell-damage modifier
`ApplySpellDamage` applies internally (per the research, both resistance
*and* skill bonus are already reflected in that one return value — one
fix closes both discrepancies).

**D10 — Diag.** New `category="damage"` kind, `kind="MutationDamage"`
(payload: mutation class name via `ImpactVerb`/`GetType().Name`, dice
rolled, element attribute, resolved post-resistance amount, target). Not
`HitRoll`/`Penetration` — those imply an accuracy gate that doesn't exist
here; a distinct kind name keeps `diag_query kind=HitRoll` honestly
scoped to attacks that actually roll to-hit.

**D11 — `CalmMutation` is a documented no-op for this fix.** Its
`DamageDice="0"` means `DiceRoller`'s invalid-pattern fallthrough returns
0, so it never reaches the `damage > 0` branch at all (`CalmMutation.cs`'s
own docstring already says this). SM6/SM7 below touch code Calm's Cast()
also runs through, but produce zero observable change for it — worth one
pinning test so a future refactor doesn't accidentally wake this branch
up for Calm without intending to.

**D12 (deferred, NOT in this plan's committed scope) — area-of-effect
widening.** The one genuine Qud-parity gap that *is* real for mutations:
Qud's line/cone mutations hit **every** occupant of **every** cell in
the affected area; CoO's `TraceFirstImpact` hits only the first. Fully
porting this means (a) a new `LineTargeting` method that returns every
occupied cell along a path rather than stopping at the first, (b)
rebalancing all 9 mutations' damage dice for multi-target application
(a `2d4` Fire Bolt hitting 4 lined-up enemies is a very different power
level than hitting 1), and (c) deciding whether `ApplyOnHitEffect`
(Burning/Poisoned/Frozen/etc.) should also apply to every hit target.
This is a scope expansion + balance decision, not a bug fix — flagged in
§5 as an explicit go/no-go question, **excluded from the sub-milestones
below** unless confirmed.

### 2.3 Sub-milestones

1. **SM6 — Message-log fix (D9).** Isolated, safe, mirrors melee's
   already-shipped fix. New test: cast a damage mutation at a
   resistant target, assert the LOGGED number matches the actual HP
   delta (no such test exists today for any of the 9 subclasses —
   `Wsp7MagicSkillsTests.cs`'s existing resistance test calls
   `ApplySpellDamage` directly, never through `Cast()`/`MessageLog`, so
   it can't and doesn't catch this).
2. **SM7 — Diag (D10).** Independent of SM6, can land in either order.
3. **SM11 — `CalmMutation` pin (D11).** One test, confirms the fix
   touches Calm's code path with zero observable effect.

*(No accuracy-mechanic milestone — see D8.)*

---

## 3. Shared: retaliation-on-hit (both systems currently lack it)

### 3.1 Current state (verified)

`BrainPart.SetPersonallyHostile(Entity)` / `PersonalEnemies` (HashSet,
save/load round-tripped via `SaveSystem.cs:1438-1490`) has **exactly one**
live production call site: `InputHandler.ExecuteAttackOnNPC:3389-3399`,
firing unconditionally before the player's melee swing even resolves.
Neither thrown-weapon damage nor mutation damage ever calls it — a
neutral creature killed by a thrown axe or a fire bolt has no mechanism
to ever become hostile in response.

`CombatSystem.ApplyDamage` is confirmed the genuine single choke point
all attacker-inflicted damage funnels through (melee, thrown, mutation,
traps, and any DoT effect tick that carries a real source). The
`source`/`attacker` parameter is **not** reliably populated, though:
`BleedingEffect`/`PoisonedEffect`/`AcidicEffect`/`ElectrifiedEffect`/
`FungalInfectionEffect` and environmental/reflected `LiquidCoveredEffect`
damage all explicitly pass `source: null` (an acknowledged, documented
contract on `MutationDamageHelpers.ApplySpellDamage`'s own docstring: "may
be null for environmental spell damage"). Melee, thrown weapons, and
mutation casts all reliably pass a real attacker entity.

### 3.2 Design decision

**D13 — Add a generic retaliation hook inside `CombatSystem.ApplyDamage`
itself**, immediately alongside where `DamageDealt` already fires (i.e.
gated on damage actually landing — post-veto, post-resistance):

```
if (source != null && source != target && source.HasTag("Player"))
{
    target.GetPart<BrainPart>()?.SetPersonallyHostile(source);
}
```

Rationale for each guard:
- **`source != target`** — mirrors `FactionManager.GetFeeling`'s own
  existing `source == target → 100` (maximally-friendly) precedent;
  today's only self-damage paths (`Pyromancy_HeartFlame`,
  `Spellcraft_LeyTap`) bypass `ApplyDamage` entirely so this guard is
  currently a no-op safety net, not a live behavior change — but it's
  free and matches established convention.
- **`source.HasTag("Player")`, V1-scoped** — mirrors today's actual
  behavior (only player action currently triggers retaliation at all).
  NPC-on-NPC incidental damage (mutation AoE catching a bystander, chain
  lightning bouncing between hostiles) is deliberately **not** covered
  yet — see §5 open question. This is a conservative, behavior-preserving
  superset of today, not a redesign of faction logic.
- **Placed at damage-landed time, not attack-attempt time** — this is a
  **semantic change from melee's existing behavior**: today's
  `InputHandler` call fires even on a miss; this hook only fires when
  damage actually connects. The two are **complementary, not
  conflicting** (`SetPersonallyHostile`/`HashSet.Add` is idempotent) —
  `InputHandler`'s existing pre-attack call is left untouched, so
  "swinging at someone provokes them even on a whiff" keeps working
  exactly as today, while the new landed-damage hook is what actually
  closes the gap for thrown/mutation/DoT-with-a-real-source damage.

This single insertion point fixes retaliation for **both** systems in
this plan simultaneously (both already funnel through `ApplyDamage`) plus,
as a side effect, for any `BurningEffect`/`PoisonedByGasEffect` tick that
carries a real `IgnitionSource`/`Owner` — those already pass a real
source today and would light up for free.

### 3.3 Sub-milestone

**SM5b** (after both SM1/SM3 for thrown and SM6 for mutations have
landed, so there's damage flowing through both paths to exercise): add
the hook, with tests: player kills a neutral NPC via thrown weapon → NPC
becomes hostile before it dies (order matters — check pre-death, not
post-corpse); player mutation-bolts a neutral NPC that survives → it's
hostile on its next turn; **counter-check**: an NPC damaging another NPC
via mutation splash does NOT create a new `PersonalEnemies` entry (V1
Player-only guard); **counter-check**: self-damage does not add
self-hostility (even though no current path reaches this, per CLAUDE.md
§3.4 pair every positive assertion with its flip).

---

## 4. Adversarial sweep applicability (CLAUDE.md gate check)

This feature touches ≥2 taxonomy surfaces, so the dedicated adversarial
sweep gate applies after SM5c (thrown accuracy + damage severity) and
SM5b (retaliation):

- **RNG-gated behavior** — the new thrown accuracy roll: boundary
  Agility values (1-2 degenerate, 3 exact-third, 4 exact-coin-flip),
  and the asymptotic-not-100%-even-at-huge-Agility property.
- **Cross-actor flows** — retaliation is inherently source/target;
  self-damage, null-source, and NPC-source guards all need adversarial
  coverage, not just the happy path.
- **Diag emission contracts** — new kinds (`ThrowHitRoll`,
  `ThrowPenetration`, `MutationDamage`) need "fires exactly once per
  landed hit, never on a miss/veto" pins, matching the existing melee
  diag contract tests' shape.

## 5. Open decisions

1. **✅ RESOLVED, 2026-07-22 — Thrown accuracy model (D3):** Option A,
   near-literal port of Qud's Layer-1 roll (`1d[Agility] >= 3`), **no**
   trajectory wobble (Layer 2 explicitly excluded). See §1.2 D3 for the
   corrected, verified mechanic and worked probabilities.
2. **Proceeding with the stated recommendation (no objection raised) —
   Retaliation scope (D13):** V1 is Player-sourced-damage-only.
   NPC-vs-NPC retaliation (a mutation AoE or chain-lightning bounce
   making two NPCs hostile to each other) is explicitly deferred, not
   silently decided either way. Confirm V1 scope is correct.
3. **Proceeding with the stated recommendation (no objection raised) —
   Mutation area-of-effect widening (D12):** stays **excluded** from
   this plan's committed sub-milestones — it's a balance-affecting scope
   expansion (hit-everyone-in-the-line vs. hit-the-first-target), not a
   bug fix. Would need its own damage-rebalancing sub-milestone and its
   own plan section if wanted later.
4. **Proceeding with the stated recommendation (no objection raised) —
   Thrown gas-spec dispatch:** in scope, per D2 (cheap, same gate as the
   other three dispatchers).

---

## Appendix: exact signatures a fix will call into

(Recorded here so implementation doesn't have to re-derive them.)

```csharp
// CombatSystem.cs
public static void ApplyDamage(Entity target, Damage damage, Entity source, Zone zone)   // line 715, primary
public static void ApplyDamage(Entity target, int amount, Entity source, Zone zone)        // line 930, thrown's current call
public static int GetDV(Entity entity)                                                     // line 570
public static int GetAV(Entity entity)                                                     // line 602
public static int GetPartAV(Entity entity, BodyPart hitPart)                                // line 1259
public static int RollPenetrations(int targetInclusive, int bonus, int maxBonus, Random rng) // line 674

// Melee's to-hit formula to mirror the SHAPE of (not the DV target) for thrown:
int hitRoll = DiceRoller.Roll(20, rng);
int totalHit = hitRoll + agilityMod + hitBonus + skillHitBonus;   // CombatSystem.cs:180-188

// MutationDamageHelpers.cs
public static int ApplySpellDamage(Entity target, int baseDamage, string elementAttribute,
    Entity attacker, Zone zone)   // line 56-83; returns Math.Max(0, hpBefore - hpAfter)

// BrainPart.cs
public void SetPersonallyHostile(Entity target)        // line 63-76
public bool IsPersonallyHostileTo(Entity target)        // line 78-81

// Diag.DefaultOnCategories (Diag.cs:119-120) — verbatim, 17 entries:
// event, effect, damage, turn, furniture, trade, quest, skill, enhancement,
// mineral-trade, worldmap, liquid, gas, gasbench, questbench, alchemy, craft
```
