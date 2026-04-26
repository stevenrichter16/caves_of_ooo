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

### Phase 0 — Recon (1-2 hours)

**Deliverable:** `Docs/COMBAT-BRANCH-MAP.md`

- Read `CombatSystem.cs` line-by-line, end-to-end
- Read `CombatSystemTests.cs` end-to-end
- Build a branch map: every `if`/`switch`/early-return in the production code, marked with whether existing tests cover it
- Build a touchpoint map: every external system the production code calls, marked with whether existing tests exercise that call
- List the high-complexity zones (multi-effect re-entrancy, death event ordering, equipment drop chain) for prioritization

**Output:** a doc that identifies the ~10-20 specific branches and ~5-8 cross-system boundaries with the highest "no test exercises this" gap.

**Stop condition:** Doc is written. No code changes yet. (1-2 hours; capped.)

---

### Phase 1 — Spec-first audit (½ day, ~25-30 tests)

**Discipline:** identical to Save/Load Phase 1. Predictions made BEFORE re-reading. Each test labeled with PRED + CONFIDENCE.

**Categories I'll write tests for:**

1. **Damage math** — clamping, NaN/Infinity handling, negative damage, int.MaxValue, exactly-0
2. **HP boundaries** — HP at 0 (== vs <), HP at int.MinValue, HP set to negative externally
3. **Killer attribution** — null killer (env damage), self-kill (killer == target), removed-killer (killer not in zone)
4. **Death cascade ordering** — equipment drop before/after corpse spawn? Died event fired before/after RemoveEntity?
5. **Status effect interaction** — DoT that kills mid-tick, status applied during HandleDeath, status that gates AllowAction during ApplyDamage
6. **Faction/rep changes** — killer with no faction, killer with no Player tag, target with no faction
7. **MessageLog formatting** — empty/null display names, killer == self message text
8. **AsciiFx triggers** — splatter on null zone, projectile during melee, FX during HandleDeath
9. **Body/anatomy** — hit on missing limb, dismemberment of last attached part, attack with no weapon

**Bug-find prediction:** 1-3. Some Phase-1-style "test was naively wrong" but most likely 1-2 real bugs around damage math edges and event ordering.

**Stop condition:** All tests classified (test-wrong / code-wrong / spec-gap). Real bugs fixed in same commit with regression test.

---

### Phase 2 — Cross-system interaction matrix (½ day, ~30-40 tests)

**This is the highest-EV phase.** Every prior CombatSystem bug — and every bug in adjacent systems — has been at an integration boundary. The Save/Load aura-restore bug, the frozen-bug saga, the item-dup, the auto-kill — all integration class.

**Plan:** for each of the 15 touchpoints, write 2-3 tests that probe the boundary. Examples:

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

### Phase 3 — Adversarial cold-eye (½ day, ~12-15 tests)

**Discipline:** identical to Save/Load Phase 3. Predictions BEFORE re-reading code. Probe edges I haven't pinned in P0/P1/P2.

**Specific probes I'd write:**

1. NaN / Infinity damage — prediction: silently corrupts HP
2. `int.MaxValue` damage — overflow into negative HP?
3. `int.MinValue` damage — wraps to positive heal?
4. Negative damage (i.e., heal-via-damage) — does ApplyDamage clamp at 0 or pass-through to HP?
5. `ApplyDamage(target, 0, ...)` — no-op, or fires events anyway?
6. Self-kill: `ApplyDamage(target, 999, target, ...)` — killer == target, what does death message say?
7. Null `target` — silent no-op, or NPE?
8. Re-entrancy: Died event handler that calls `ApplyDamage(killer, 5, ...)` — double-death possible?
9. Re-entrancy: Died event handler that adds a status effect that kills the killer
10. Two simultaneous lethal hits in the same frame (e.g., walking onto two co-located runes)
11. Equipment drop with `EquippedItems[slot]` pointing to entity not in zone
12. HandleDeath called on entity already removed from zone (cell null)
13. PerformMeleeAttack with attacker == defender (self-attack)
14. PerformMeleeAttack with `rng == null`

**Bug-find prediction:** 1-2. The P0-P2 phases will have closed off most surfaces; P3 is the genuine adversarial probe for what the prior phases didn't think of.

**Stop condition:** Same as P1.

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

---

## Total estimate

| | |
|---|---|
| Total time | 2-3 days |
| Total new tests | 80-120 |
| Production bugs expected | 4-8 |
| Production fixes (per §3.9 cadence) | 1 commit per bug, regression test pinned |
| Outcome target | CombatSystem joins M1-M6 as post-audit baseline |

---

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| **Refactor temptation.** Combat is old; it's tempting to "improve" it during audit. | Discipline: audits do not refactor. If a refactor is warranted, file it as a separate Tier-3 item post-audit. |
| **"Bug" vs "design choice".** Some behaviors that look wrong may be intentional (e.g., damage clamping at 0 vs allowing negative HP for special effects). | Each fix commit articulates *why* the behavior is wrong, not just *what* is wrong. Borderline cases get flagged for design review rather than fixed. |
| **Cross-system tests are slow to write.** A Combat × StatusEffects test needs to set up both systems realistically. | Use existing test patterns: `MakeMinimalState` from Save tests, `ScenarioTestHarness` for higher-level flows. Don't reinvent setup. |
| **PlayMode bugs.** Some integration bugs only surface in real Play Mode (the IAuraProvider bug was an EditMode-invisible Unity-runtime concern). | Phase 2 includes diag-style probes (per pause-menu precedent) where suspicion warrants. Reserve the right to add Debug.Log diagnostics for manual playtest if a category resists EditMode capture. |
| **Phase 4 property-tests have a learning curve.** | Use simple `for` loops with seeded RNG rather than introducing a dedicated property-test framework. The shape is "1000 random inputs, assert invariant" — vanilla C# handles it. |

---

## Deliverables checklist

- [ ] `Docs/COMBAT-BRANCH-MAP.md` (Phase 0)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemSpecTests.cs` (Phase 1)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemIntegrationTests.cs` (Phase 2 — split files OK if topical)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemAdversarialTests.cs` (Phase 3)
- [ ] `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemPropertyTests.cs` (Phase 4)
- [ ] `Docs/COMBAT-DIVERGENCES.md` (Phase 5, if executed)
- [ ] Audit summary commit: per-phase bug count, fix locations, final test count

Production fixes land per §3.9 cadence: one commit per bug, regression test pinned.

---

## Recommended starting move

Begin with **Phase 0** (recon). Output the branch map. Read it, decide whether the rest of the plan still feels right with the actual code in front of us, or whether the priorities should shift.

Phase 0 is short and cheap. If the branch map reveals (e.g.) that 80% of the surface is already covered, the audit budget shrinks accordingly. If it reveals (e.g.) major branches with zero coverage, Phase 2 priorities reorder around those.

This is the same gate pattern the M-series used: pre-impl recon before the substantive work.
