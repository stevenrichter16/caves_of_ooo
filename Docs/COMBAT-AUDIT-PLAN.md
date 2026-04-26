# CombatSystem Deep Sweep — Audit Plan

**Date drafted:** 2026-04-26
**Branch (planned):** `audit/combat-deep-sweep` (off `main` at `5f4f869`)
**Companion to:** `Docs/QUD-PARITY.md §3.9`, `Docs/roadmap.md` Tier-1 #3

---

## Why CombatSystem, why now

Three reasons CombatSystem is the highest-EV next audit target:

1. **Empirical bug-find rate of 12.5%.** The single cross-cut adversarial probe against `ApplyDamage` (`65df19c`) was 16 tests, found 2 production bugs (no-HP autokill, already-dead duplicate `HandleDeath` causing item-dup). Same surface, similar probe shape, expected return: 1 bug per ~8 tests. No other audited surface in this project has exceeded 0%.

2. **Cross-cutting blast radius.** Every combat-adjacent system has now produced a bug class:
   - **Status effects** — frozen-bug saga (3 fixes), IAuraProvider visuals (1 fix)
   - **Save/load** — constructor-bypass, consume-contract
   - **Combat** — auto-kill, item-dup
   - All four bug classes had a CombatSystem touchpoint. The system is the connective tissue; the connective tissue has the highest density of inter-system invariants.

3. **Pre-M-style methodology.** CombatSystem.cs (677 LOC, 7 public methods) was written before the pre-impl plan / TDD-first / post-impl audit cadence crystallized. It hasn't received the M-style treatment that produced 0% bug-find on M1-M6.

---

## Scope (what's in, what's out)

**In scope** — the seven public methods of `CombatSystem.cs` and their immediate cross-system effects (the 15 touchpoints listed below).

**Out of scope** — damage types and resistance systems. These are M9 future work (`Docs/QUD-PARITY.md`). Several bug classes that exist in typed-damage systems (resistance bypass, type confusion, type-specific armor) cannot exist in CoO's current single-int damage model and should not be probed.

**Out of scope** — refactoring. If a refactor is warranted, it gets filed as a separate item post-audit (see Risks).

## What's there now

| File | LOC | Tests | Public API |
|---|---:|---:|---|
| `CombatSystem.cs` | 677 | — | 7 public methods |
| `CombatSystemTests.cs` | 632 | 37 | — |

**Public entry points** (the 7 surfaces this audit must cover):

| Method | Purpose | Known status |
|---|---|---|
| `PerformMeleeAttack(attacker, defender, zone, rng)` | High-level orchestrator | Some coverage |
| `ApplyDamage(target, amount, source, zone)` | The damage entry point | 2 bugs caught + fixed; **likely more lurking** |
| `HandleDeath(target, killer, zone)` | Death cascade (drop, splatter, Died event, remove) | Implicated in M6 break-loop fix; likely more |
| `GetDV(entity)`, `GetAV(entity)`, `GetPartAV(entity, part)` | Defense calculations | Light coverage |
| `RollPenetrations(pv, av, rng)` | Armor penetration math | Probabilistic; underpinned for property tests |
| `SelectHitLocation(body, rng)` | Hit location targeting | Body-tree dependent |

**Cross-system touchpoints** (each is a candidate for integration test):

- Body / BodyPart (limb removal, dismemberment ordering, anatomy lookup)
- Inventory (drop-on-death ordering vs corpse spawn vs equipment unequip)
- StatusEffectsPart (BeforeApply/AfterApply during combat, OnApplyKills, OnTickKills)
- Faction / FactionManager (rep changes on kill, hostility checks before strike)
- Save/Load (HandleDeath fired after RemoveEntity? before? cell resolvable?)
- AsciiFxBus (death splatter on null zone, projectile FX during attack)
- MessageLog (null/empty display names, control characters in names)
- Mutations (MutationsPart entry that triggers on kill; mutation that causes self-damage)
- BrainPart (Target reference when target dies; PersonalEnemies update)
- CorpsePart (M5 — fired by Died event chain; ordering relative to drops)
- TriggerOnStepPart (M6 — Died → corpse → trigger collision potential)
- TurnManager (entity in turn queue when killed; CurrentActor death)
- Zone (RemoveEntity ordering; cell resolvable during HandleDeath)
- WitnessedEffect (M2 — death broadcast to nearby Passive NPCs)
- DeathSplatterFx (FX emit before zone.RemoveEntity)
- LevelingSystem (XP awarded to Player killers)

That's **15 touchpoints**. Even one test per is 15 integration tests; targeted to known-fragile boundaries it's 30-45.

---

## The plan (six-phase sweep, ~2-3 days)

### Phase 0 — Recon + prioritized test backlog (1-2 hours)

**Deliverables:**
1. `Docs/COMBAT-BRANCH-MAP.md` — the branch + touchpoint map
2. **`Docs/COMBAT-TEST-BACKLOG.md`** — a prioritized list of test candidates, ranked by `(coverage gap × bug-likelihood × cross-system blast radius)`. Phases 1-3 will pull from this backlog rather than writing tests "by category."

The backlog binds recon to test-writing. Without it, Phase 1 could happily write 25 tests that don't address the gaps Phase 0 found.

- Read `CombatSystem.cs` line-by-line, end-to-end
- Read `CombatSystemTests.cs` end-to-end
- Build a branch map: every `if`/`switch`/early-return, marked with existing test coverage
- Build a touchpoint map: every external system call, marked with existing test coverage
- Identify the high-complexity zones (re-entrancy, death event ordering, equipment drop chain)
- **Translate findings into a prioritized backlog**: each entry is `(branch_or_touchpoint, gap_severity 1-5, suspected_bug_class, suggested_test_shape)`. The top items become Phase 1's first tests; cross-system items become Phase 2's; edge items become Phase 3's.

**Stop condition + check-in gate:** Both docs written. Pause for user review BEFORE writing any tests. The recon may reorder priorities or shorten the audit budget.

---

### Phase 1 — Spec-first audit (½ day, ~25-30 tests)

**Discipline:** identical to Save/Load Phase 1. Predictions made BEFORE re-reading. Each test labeled with PRED + CONFIDENCE. Tests are pulled from Phase 0's backlog (canonical-contract items only — adversarial-edge items go to Phase 3).

**Categories pulled from the backlog (canonical-contract items):**

1. **Damage math** — clamping behavior, exactly-0, exactly-`Hitpoints.Max`, ordinary positive
2. **HP boundary** — HP at 0 (== vs <), HP just-above-0, HP at Max
3. **Killer attribution (canonical)** — null killer (env damage), killer with proper Faction tag, killer with Player tag (XP path)
4. **Death cascade ordering** — equipment drop, corpse spawn, Died event fire, RemoveEntity (the documented order)
5. **Status effect (canonical)** — DoT damage applied via OnTurnStart, status applied via canonical OnApply
6. **Faction/rep changes (canonical)** — kill with proper killer faction, kill with player as killer
7. **MessageLog formatting (canonical)** — populated display names, well-formed killer
8. **AsciiFx triggers (canonical)** — splatter with valid zone, projectile FX with valid path
9. **Body/anatomy (canonical)** — hit on attached limb, dismemberment of non-mortal part

> **Adversarial-edge items moved to Phase 3** (deduped from earlier draft): NaN/Infinity damage, int.MaxValue/MinValue, negative damage, self-kill, removed-killer, empty/null display names, splatter on null zone, hit on missing limb. These are the surfaces where my predictions are uncertain or genuinely cold-eye; they belong in P3.

**Bug-find prediction:** 1-3. Most likely class: documented contract drift — production behavior diverged from the contract its comments claim.

**Stop condition:** All tests classified. Real bugs fixed in same commit with regression test. **After each fix, run full EditMode suite before continuing.**

---

### Phase 2 — Cross-system interaction matrix (½ day, ~30-40 tests)

**This is the highest-EV phase.** Every prior CombatSystem-adjacent bug has been at an integration boundary. The Save/Load aura-restore bug, the frozen-bug saga, the item-dup, the auto-kill — all integration class.

**Touchpoint prioritization (NOT all 15 are equal):**

| Tier | Touchpoints | Why |
|---|---|---|
| **A — high prior bug history** | StatusEffectsPart, Inventory, Body, Save/Load, CorpsePart | All have had bugs at the Combat boundary in the last 6 months. **Probe deeply** — 4-5 tests each |
| **B — medium complexity, no recent bugs but rich interactions** | Faction, BrainPart, TurnManager, Zone, TriggerOnStepPart (M6) | Possible bugs but no recent evidence. **Probe canonically** — 2-3 tests each |
| **C — low complexity or simple boundary** | MessageLog, AsciiFxBus (death splatter), LevelingSystem, WitnessedEffect, Mutations, DeathSplatterFx | Simple data-flow boundaries. **Smoke-test** — 1 test each, only if backlog calls for it |

Total target: 30-40 tests. Tier A absorbs ~60% of the budget; Tier B ~30%; Tier C ~10%.

**Plan:** for each touchpoint, pull tests from Phase 0's backlog. The list below shows the SHAPE of each touchpoint's tests; the actual tests are determined by what the backlog reports as ungeared.

| Boundary | Sample tests |
|---|---|
| Combat × Body | Hit a dismembered (`HasTag("Severed")`) limb; combat with body that has no limbs left; PerformMeleeAttack on entity with `Body == null` |
| Combat × Inventory | HandleDeath where inventory contains a referenced equipped item; killing entity mid-pickup; equipment drop with `EquippedItems` containing instance not in `Objects` |
| Combat × StatusEffects | DoT that kills target; status effect's OnRemove triggers during HandleDeath; status applied by killer that names killer in OnApply |
| Combat × Faction | Kill an entity whose faction was just changed; kill with `killer.HasTag("Faction") == false` |
| Combat × Save | Save mid-attack (after BeforeAttack, before damage); HandleDeath called on entity already in save graph |
| Combat × AsciiFx | DeathSplatter with `zone == null`; splatter when entity is at zone boundary |
| Combat × MessageLog | `target.GetDisplayName()` returns empty string; returns string with `\n`; returns null |
| Combat × Mutations | Mutation that fires `Died` listener and triggers ApplyDamage on the killer |
| Combat × BrainPart | Killer's `Target` is the entity that just died; PersonalEnemies cleanup |
| Combat × CorpsePart | M5 — corpse spawned at cell where killer is standing; corpse spawn after `Died` listener already removed entity |
| Combat × TriggerOnStepPart | M6 — die on a rune; rune fires AsciiFx that requires zone (zone might be partially-removed mid-death) |
| Combat × TurnManager | Killing the CurrentActor mid-turn; killing entity that's queued for next turn |
| Combat × Zone | HandleDeath when entity's cell is null; RemoveEntity-then-AddEntity within HandleDeath |
| Combat × WitnessedEffect | Death broadcast at zone boundary (witnesses on other side of map); broadcast with no Passive NPCs |
| Combat × LevelingSystem | XP for player kill of zero-level entity; XP for self-kill |

**Bug-find prediction:** **2-4 real bugs.** This is where the integration class lives.

**Stop condition:** All 30-40 tests classified. Real bugs fixed.

---

### Phase 3 — Adversarial cold-eye (½ day, ~12-18 tests)

**Discipline:** identical to Save/Load Phase 3. Predictions BEFORE re-reading. Probe genuinely-uncertain edges that P0-P2 didn't cover.

**Probes (consolidated — includes the adversarial-edges moved out of P1):**

*Numerical extremes:*
1. NaN / Infinity damage (PRED: stat is int — does it round-trip a float-cast? probably crashes or silently corrupts)
2. `int.MaxValue` damage (PRED: overflow into negative HP)
3. `int.MinValue` damage (PRED: wraps to positive heal)
4. Negative damage (PRED: clamps at 0 OR is a stealth heal)
5. `ApplyDamage(target, 0, ...)` (PRED: no-op, but does it fire BeforeDamage/AfterDamage anyway?)

*Reference graph:*
6. `target == null` (PRED: silent no-op)
7. Self-kill: `ApplyDamage(target, 999, target, ...)` — message text? XP path? (PRED: messages weird, no XP)
8. Removed-killer: killer was in zone at attack-start, removed before damage settles (PRED: silent no-op or message-text NPE)
9. PerformMeleeAttack with `attacker == defender` (PRED: self-damage path)
10. PerformMeleeAttack with `rng == null` (PRED: NPE somewhere in roll path)

*Re-entrancy:*
11. Died event handler that calls `ApplyDamage(killer, 5, ...)` — does the killer survive? (PRED: depends on whether `killer.Hitpoints > 0` is checked at top of ApplyDamage post-65df19c)
12. Died event handler that adds a status effect to the killer that kills them (PRED: status's OnApply fires HandleDeath which fires another Died handler — infinite chain or bounded?)
13. Two simultaneous lethal hits same frame (e.g., walking onto two co-located runes — already pinned by M6's `Contains` break, regression)
14. HandleDeath on entity whose `zone.GetEntityCell()` returns null (PRED: defensive guards somewhere; specifically check)

*Boundary cases:*
15. Equipment drop where `EquippedItems[slot]` points to instance not in `Inventory.Objects` (PRED: drop-once OR drop-twice OR not-at-all)
16. Hit on missing limb (PRED: defensive somewhere)
17. Empty/null `target.GetDisplayName()` in MessageLog formatting (PRED: prints empty / null / "(unknown)")
18. DeathSplatterFx with `zone == null` (PRED: silent no-op or NPE)

**Bug-find prediction:** 1-2. Likely class: re-entrancy or numerical-extreme that the P1-P2 canonical tests didn't reach.

**Stop condition:** All probes classified. Real bugs fixed.

---

### Phase 4 — Property-based / fuzz (½ day, ~5-8 properties)

**Why this layer:** spec/cross/adversarial are example-based — they test specific scenarios. Property tests assert *invariants over all possible inputs*. They catch a different class of bug — usually clamping/ordering/idempotence violations the example tests miss.

**Properties to assert:**

1. **HP-in-range invariant** — after ANY sequence of `ApplyDamage` calls, target's HP is in `[Min, Max]`
2. **No-resurrection invariant** — once HandleDeath has fired, target's HP cannot increase via ApplyDamage
3. **HandleDeath idempotence** — `HandleDeath` called N times produces same end state as called once (already pinned for N=2; extend to N=many)
4. **Zero-damage no-op** — `ApplyDamage(target, 0, _, _)` does not fire BeforeDamage/AfterDamage events (or it does — pin whichever is true)
5. **Damage commutativity** — for non-killing damage values, `ApplyDamage(t, A); ApplyDamage(t, B)` produces same final HP as `ApplyDamage(t, B); ApplyDamage(t, A)` (assuming no triggered effects)
6. **Penetration distribution** — `RollPenetrations(pv, av, rng)` over 10000 rolls produces a defensible distribution (no negative results, mean ≥ 0, etc.)
7. **Hit-location uniformity** — `SelectHitLocation` over 10000 rolls hits every alive part proportionally to its TargetWeight

**Bug-find prediction:** 0-2. Mostly catches what the other phases missed.

**Stop condition:** Each property either holds for 1000+ random configurations, or the failure case is captured as a regression test.

---

### Phase 4½ — Lightweight mutation testing (1-2 hours, optional but recommended)

**Why:** mutation testing is the single most powerful technique for detecting *coverage gaps in existing tests*. It works by altering the production code (flip `<` to `<=`, swap variable A for B, change a constant) and re-running the test suite. **Tests that still pass after the mutation are inadequate** — they did not exercise that branch with assertions sharp enough to catch the mutation.

**Manual variant (no tooling required):** make ~5-10 surgical edits to `CombatSystem.cs`, run the suite, see what passes. The edits to try:

1. Flip the no-HP check at the top of `ApplyDamage` (the post-65df19c fix). If suite passes, the fix isn't tested. (Sanity check.)
2. Flip `if (defender.GetStatValue("Hitpoints", 0) <= 0)` to `< 0`. Off-by-one coverage.
3. Change the death-cascade order (move `Died` event firing to before equipment drop). Order-of-operations coverage.
4. Make `BroadcastDeathWitnessed` a no-op. Witness-effect coverage.
5. Always return `false` from `RollPenetrations`. Penetration probability coverage.
6. Always return the same `BodyPart` from `SelectHitLocation`. Hit-location uniformity coverage.
7. Make `DropInventoryOnDeath` drop nothing. Drop-coverage.
8. Always return `0` from `GetDV`. Defense-zero coverage.

For each mutation: revert before the next. If the suite passes after a mutation, **add a test** that catches it. The test then becomes part of the project's permanent regression shield.

**Bug-find prediction:** 0-1 actual production bug, but **2-5 coverage gaps closed**. The *output* of this phase is mostly tests, not fixes.

**Stop condition:** ~5-10 mutations attempted, coverage gaps closed with new tests.

---

### Phase 5 — Differential vs Qud reference (optional, ½ day)

**The codebase has the Qud decompiled source at `qud_decompiled_project/` and was patterned on it. Combat is one of the closer parity surfaces.** Diverging combat behaviors are either intentional design choices or accidental differences from the reference.

**Plan:**

- Locate Qud's `XRL.World.Capabilities.Combat` (or equivalent) damage entry point
- Compare to `ApplyDamage`. Note divergences in:
  - Damage clamping rules
  - HandleDeath ordering (drop → corpse → Died event → remove)
  - Event names + parameters
  - Killer attribution for status-effect deaths (DoT, environmental)
  - Hit-location selection probabilities
- Each divergence: classify as `intentional` (CoO simplification) or `accidental` (parity drift)

**Bug-find prediction:** 0-2. Higher value as documentation than as bug-finding.

**Stop condition:** A short divergences table is written, classified, and either accepted or fixed.

---

## Stop conditions for the whole sweep

- **Phase 0 mandatory** before any test-writing.
- **Phases 1-3 are the core.** P4 and P5 are optional layers if budget permits.
- **Stop the sweep early** if Phase 1 + Phase 2 collectively yield 0 bugs (extremely unlikely given the empirical baseline; would suggest the existing 37 tests already covered everything).
- **Continue past Phase 3** if running average bug-find > 5%.

## User check-in gates

Three explicit pause points where I stop and surface findings before continuing — to avoid 2-3 days of unsupervised work and to let priorities reorder mid-sweep based on what's actually being found.

| Gate | When | What I report | What you decide |
|---|---|---|---|
| **G1** | After Phase 0 | Branch map + prioritized backlog. Surprises in coverage. Touchpoints I want to upgrade/downgrade vs the original tier list | Whether the audit budget should grow, shrink, or refocus. Whether to continue. |
| **G2** | After Phase 2 | Bugs found so far + classification. Whether Phase 3 still feels needed (if bug-find rate is high, P3 is mandatory; if zero, P3 still scheduled but expectations lowered) | Whether to push into P3, P4, or stop. Whether any specific bugs warrant deeper investigation before continuing. |
| **G3** | After all executed phases | Full audit summary: bugs found, fixes landed, tests written, branches+touchpoints now baseline-covered, items deferred | Whether the audit closes here or extends with a P5/P6/follow-up branch. Whether CombatSystem joins the post-audit baseline. |

Between gates I work continuously; I do not pause every commit. The gates are *findings-based* checkpoints, not procedural ones.

---

## Total estimate

| | |
|---|---|
| Total time | 2-3 days (mandatory phases); +½ day each for P4½ and P5 if executed |
| Total new tests | 80-130 (up from earlier draft due to P4½ coverage-gap closures) |
| Production bugs expected | 4-8 (treat as a budget, not a target — see Risks) |
| Coverage gaps closed (independent of bugs) | 2-5 (mostly from P4½ mutation testing) |
| Production fixes (per §3.9 cadence) | 1 commit per bug, regression test pinned, full suite re-run before next test |
| Outcome target | CombatSystem joins M1-M6 as post-audit baseline |

---

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| **Refactor temptation.** Combat is old; it's tempting to "improve" it during audit. | Discipline: audits do not refactor. If a refactor is warranted, file it as a separate Tier-3 item post-audit. |
| **"Bug" vs "design choice" ambiguity.** Some surprising behaviors may be intentional (e.g., damage clamping at 0 vs allowing negative HP for special effects). | **Decision framework**: if the surprising behavior is *documented in code comments or referenced docs*, it's design — write a test that pins it as a contract. If the behavior is *undocumented and surprising*, it's likely a bug — fix it. If both unclear, mark the test `[Ignore]` with a note and surface to user for design review rather than fix. |
| **Cross-system tests are slow to write.** A Combat × StatusEffects test needs both systems set up realistically. | Use existing test patterns: `MakeMinimalState` from Save tests, `ScenarioTestHarness` for higher-level flows. Don't reinvent setup. |
| **PlayMode-only bugs.** Some integration bugs only surface in real Play Mode (the IAuraProvider bug was an EditMode-invisible Unity-runtime concern). | **Tactic per pause-menu precedent**: when a Phase 2 category resists EditMode capture (the assertion can't reach the bug class because it's behind a Unity-runtime boundary like Camera/Tilemap/SceneManager), add `[CombatDiag/...]`-style Debug.Log statements at the suspicious chokepoints, push to a side branch, ask user to manually playtest one scenario, then remove the diagnostics in a follow-up commit once the bug is identified. |
| **Phase 4 property-tests have a learning curve.** | Use simple `for` loops with seeded RNG. The shape is "1000 random inputs, assert invariant" — vanilla C# handles it. |
| **Motivated reasoning from bug predictions.** Predicting "4-8 bugs" creates pressure to find that many. Tempting to upgrade marginal findings to bugs to validate the prediction. | **Discipline**: at the end of Phase 3, count the bugs honestly. If the actual rate is below 5%, the prior probes were thorough; do not fish for additional findings to hit the prediction. The prediction is a budget, not a target. |
| **Regression cascade from fixes.** CombatSystem is cross-cutting; a fix can break a system that was passing. | **Per-fix discipline**: after each production fix, run the FULL EditMode suite before adding the next test. A 2071-test full run takes ~22s — cheap enough to gate every fix on it. If the suite fails, revert the fix and re-classify the bug (was it actually a contract drift, or was it load-bearing behavior?). |

---

## Deliverables checklist

- [ ] `Docs/COMBAT-BRANCH-MAP.md` (Phase 0 — recon + touchpoint map)
- [ ] `Docs/COMBAT-TEST-BACKLOG.md` (Phase 0 — prioritized backlog that drives P1-P3)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemSpecTests.cs` (Phase 1)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemIntegrationTests.cs` (Phase 2 — split files OK if topical, e.g. `CombatStatusEffectsIntegrationTests.cs`, `CombatInventoryIntegrationTests.cs`)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemAdversarialTests.cs` (Phase 3)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemPropertyTests.cs` (Phase 4 — optional)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemMutationCoverageTests.cs` (Phase 4½ — coverage tests added because mutations passed)
- [ ] `Docs/COMBAT-DIVERGENCES.md` (Phase 5 — optional)
- [ ] Audit summary commit: per-phase bug count, fix locations, final test count

Production fixes land per §3.9 cadence: one commit per bug, regression test pinned, **full EditMode suite re-run before the next test is added**.

---

## Recommended starting move

Begin with **Phase 0** (recon). Output the branch map. Read it, decide whether the rest of the plan still feels right with the actual code in front of us, or whether the priorities should shift.

Phase 0 is short and cheap. If the branch map reveals (e.g.) that 80% of the surface is already covered, the audit budget shrinks accordingly. If it reveals (e.g.) major branches with zero coverage, Phase 2 priorities reorder around those.

This is the same gate pattern the M-series used: pre-impl recon before the substantive work.
