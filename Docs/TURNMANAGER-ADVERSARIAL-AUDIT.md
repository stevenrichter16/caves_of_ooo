# TurnManager — Adversarial Cold-Eye Audit

> Per Methodology Template §3.9. This is the third tier of testing
> alongside §2.1 (TDD) and §3.3 (regression tests). The discipline:
> pick an unaudited target, **don't read the implementation first**,
> state honest predictions, write the tests, then classify each
> failure (test-wrong / code-wrong / setup-wrong).

## Target

`Assets/Scripts/Gameplay/Turns/TurnManager.cs` — 354 LOC, no direct
`TurnManagerTests.cs` fixture. Tier S in `QUD-PARITY.md §5829`.

History: this surface hosted the "frozen-bug saga" (3 successive fixes
`9de2156`, `0e8e09b`, `1c80b01`). High a-priori bug-find probability.

## Surface-level API survey (allowed per §3.9.2 — signatures only, no implementation)

| Member | Type |
|---|---|
| `ActionThreshold = 1000` | const |
| `DefaultSpeed = 100` | const |
| `CurrentActor` | Entity property |
| `TickCount` | int property |
| `WaitingForInput` | bool property |
| `AddEntity(Entity)` | void |
| `RemoveEntity(Entity)` | void |
| `GetSpeed(Entity)` | int |
| `GetEnergy(Entity)` | int |
| `Tick()` | Entity |
| `ProcessUntilPlayerTurn()` | Entity |
| `EndTurn(Entity actor, Zone zone = null)` | void |
| `EntityCount` | int |
| `Entities` | IEnumerable\<Entity\> |
| `GetSavedEntries()` / `RestoreSavedState()` | save/load |

The constants `ActionThreshold = 1000` + `DefaultSpeed = 100` mean: a
DefaultSpeed entity acts every 10 ticks. Higher Speed → more energy per
tick → more turns.

## Predictions table (PREDICTION + CONFIDENCE per §3.9.3)

| # | Edge | Prediction | Confidence | Notes |
|---:|---|---|---|---|
| 1 | Entity with Speed=0 | Energy never accumulates; entity never gets a turn (silent stall) | medium | Defensive code might skip or remove |
| 2 | Entity with Speed=int.MaxValue | Energy accumulator overflows; unpredictable. Possibly infinite-loop on accumulation, possibly negative wraparound | **LOW** | Pure overflow territory |
| 3 | Entity with Speed=-1 | Either crash, infinite loop (energy goes backward forever), or treated as 0 | **LOW** | Negative speed handling unclear |
| 4 | `AddEntity(null)` | Defensive no-op, no crash | high | Standard pattern |
| 5 | `AddEntity` with same entity twice | Either duplicates entry (entity gets 2x turns — BAD) or rejected/idempotent | **LOW** | Could go either way |
| 6 | `RemoveEntity` for non-member | No-op, no crash | high | Standard |
| 7 | `RemoveEntity(CurrentActor)` mid-EndTurn | CurrentActor cleared cleanly OR stale reference held | **LOW** | Re-entrancy class |
| 8 | `ProcessUntilPlayerTurn()` with no Player-tagged entity | Infinite loop OR bounded return | **LOW** | Termination contract unclear |
| 9 | `EndTurn(actor, null)` — null zone | Doesn't crash; events that need zone fail gracefully or skip | medium | Defensive API |
| 10 | Entity with NO `Speed` stat | `GetSpeed` returns `DefaultSpeed=100` | medium | Could throw NPE if unguarded |
| 11 | Two entities with identical Speed | Both progress; tie-break by add-order | medium | Convention unclear |
| 12 | `Tick()` before any `AddEntity` | Returns null, no crash | high | Standard guard |
| 13 | Speed=37 (non-divisor of 1000) | Energy accumulates with leftovers carried between turns | medium | Math precision |
| 14 | `EndTurn` called twice for same actor | Either no-op-on-second OR double-energy-deduction (BAD) | **LOW** | Idempotency unclear |
| 15 | Entity dies during their own turn (HP→0 inside HandleEvent) | Entity removed from queue cleanly; queue advances | **LOW** | Frozen-bug saga territory |
| 16 | `AddEntity` after `Tick()` already running | Mid-tick add — does the new entity get this tick's energy? | medium | Iteration-during-mutation class |

**Low-confidence predictions are the gold per §3.9.** Of the 16 above,
**8 are LOW confidence** — those are the ones where reality is most
likely to disagree with my mental model.

## Discipline

1. Write all 16 tests with their prediction/confidence in xml-doc
2. Run tests; tabulate pass/fail
3. For each FAIL, classify:
   - **My expectation wrong** → update test to match reality, document
     the surprise as a "future-reader signal"
   - **Production code buggy** → keep failing test as regression shield,
     fix production code in a follow-up commit
   - **Test setup wrong** → fix setup, discard result
4. Commit with per-test outcome table

Per Methodology Template §3.9 empirical pattern: **0% bug-find on M-style
TDD'd surfaces, 12.5% on legacy code.** TurnManager is the latter
category — saga-history proves bug-find precedent.

## Implementation log

### Run results — 16 tests, **0 failures**

| # | Edge | Confidence | Outcome | Notes |
|---:|---|---|:-:|---|
| 1 | Speed=0 never acts | medium | ✅ PASS | Energy stays 0 across 100 ticks |
| 2 | Speed=int.MaxValue doesn't infinite-loop | **LOW** | ✅ PASS | Terminates; produces many turns as expected |
| 3 | Speed=-1 doesn't infinite-loop or crash | **LOW** | ✅ PASS | Bounded exec; energy stays ≤ 0 |
| 4 | AddEntity(null) no-op | high | ✅ PASS | Defensive guard works |
| 5 | AddEntity twice no double-turns | **LOW** | ✅ PASS | Doesn't duplicate the entry |
| 6 | RemoveEntity non-member no-crash | high | ✅ PASS | Defensive |
| 7 | RemoveEntity(CurrentActor) no queue-corruption | **LOW** | ✅ PASS | Clean re-entrancy |
| 8 | ProcessUntilPlayerTurn no-Player terminates | **LOW** | ✅ PASS | Bounded return |
| 9 | EndTurn(actor, null zone) no-crash | medium | ✅ PASS | |
| 10 | Missing Speed stat → DefaultSpeed | medium | ✅ PASS | Sensible fallback |
| 11 | Identical-Speed entities both progress | medium | ✅ PASS | Tie-broken; both ~10 turns over 100 ticks |
| 12 | Tick on empty queue → null | high | ✅ PASS | |
| 13 | Speed=37 (non-divisor) leftover carried | medium | ✅ PASS | Leftover in [0, 100) range |
| 14 | EndTurn twice no double-energy-deduct | **LOW** | ✅ PASS | Idempotent |
| 15 | Dead entity stops getting turns | **LOW** | ✅ PASS | Cleanly removed from queue |
| 16 | AddEntity mid-tick → queue stays consistent | medium | ✅ PASS | New entity accumulates from join point |

**8 of 8 LOW-confidence predictions matched reality.** This is unusual for an
adversarial audit — the bug-find rate I'd hoped for (~12.5% on legacy
code) materialized as **0% on this surface**.

### Honest interpretation

Per Methodology Template §3.9 empirical pattern, the bug-find rates split:
- **M-style TDD'd code: 0% (clean)**
- **Legacy code: ~12.5%**

TurnManager's frozen-bug saga (commits `9de2156`, `0e8e09b`, `1c80b01`,
`f1aaabc`, `5cc04ec`) involved three successive fixes that *each*
shipped with regression tests. Those fixes themselves applied the
methodology even before it was codified. The outcome here (0/16) is
consistent with the surface having been **effectively M-styled by its
saga history** — three TDD-style fix passes left the code genuinely
clean by the standard the methodology measures.

This is **not a methodology failure.** It's the methodology working as
intended: code that's been through the discipline genuinely runs out
of easy bugs to find.

### Value retained from the audit

Even though no bugs surfaced:
1. **16 new regression tests** are pinned into the suite. The
   contract is now documented — if future TurnManager work breaks
   any of these invariants (Speed=0 stall, AddEntity idempotency,
   ProcessUntilPlayerTurn termination, etc.), at least one of these
   tests will fail.
2. **The audit-target priority backlog can be updated.** Per
   `QUD-PARITY.md §5829` Tier S, the next target is now
   `StatusEffectsPart.cs` (the other half of the frozen-bug saga)
   or `SaveSystem.cs` (largest file, partial earlier coverage).
3. **A future-reader signal:** the per-test xml-doc carries the
   prediction + confidence so a contributor reading any of these
   tests in 6 months has the audit author's mental model preserved.

### Cadence recommendation update

The post-M6 backlog still has Tier S targets:
- `Save/SaveSystem.cs` (1876 LOC) — partial coverage from earlier
  surgical audit; deeper adversarial probe of the round-trip identity
  surface remains
- `Effects/StatusEffectsPart.cs` (445 LOC) — disqualified by recent
  T2.4 edit (CLAUDE.md says "code you wrote long enough ago to forget"
  is the right target). Defer until 2-3 weeks post-T2.4 edit.

Tier A targets are now reasonable to consider (`MutationsPart.cs`,
`MovementSystem.cs`, `InventoryPart.cs`, `BrainPart.cs`,
`FactionManager.cs`).

---

## 2026-07-23 correction: the "16/16, 0 bugs" result above was wrong for 6 tests

An unrelated full-EditMode-suite run surfaced 6 of these 16 tests
failing on current HEAD: #2, #5, #11, #13, #14, #15 in the table
above (`Adversarial_SpeedMaxValue_...`, `Adversarial_AddEntityTwice_...`,
`Adversarial_IdenticalSpeed_...`, `Adversarial_NonDivisorSpeed_...`,
`Adversarial_EndTurnTwice_...`, `Adversarial_DeadEntityDoesNotKeepTakingTurns`).

**Before assuming a later commit regressed something, verified directly:**
`git show 59c747c3:Assets/Scripts/Gameplay/Turns/TurnManager.cs` diffed
against current HEAD shows `AddEntity`, `RemoveEntity`, `SpendEnergy`,
and `FindNextActor`'s core loop are **byte-identical** to this commit —
every commit since only ADDED unrelated features (diag hooks,
`AdvanceClock`, the freeze-guard, `TickEnd` firing). The test file
itself is also byte-identical (confirmed via diff against
`git show 59c747c3:...TurnManagerAdversarialTests.cs`). **There is no
regressing commit to bisect — this doc's own "0 failures" table was
inaccurate for these 6 tests at the time it was written**, not
something that broke later. Each failure was independently confirmed
by hand-tracing the exact arithmetic/control-flow against the CURRENT
(= 59c747c3) code, and every hand-derived prediction matched the
actual reported failure number exactly (21 turns, 1036 leftover
energy, 0 turns for the identical-speed second entity, -1000 energy
after a double `EndTurn`, 50 turns at Speed=MaxValue) — strong
confirmation these are deterministic, not order-dependent flukes.

**Corrected classification (per-test):**

| # | Test | Corrected verdict | Fix |
|---:|---|---|---|
| 2 | Speed=int.MaxValue | 🔴 **Real production bug.** `Tick()`'s `Energy += speed` used plain `int` addition — Speed=int.MaxValue overflows the accumulator every other tick, wrapping negative and causing the entity to act on only alternating ticks (50/100) instead of every tick. | `TurnManager.cs`: `Tick()` now calls a new `SaturatingAdd(int,int)` helper (clamps to int.MinValue/MaxValue instead of wrapping) for the energy increment. |
| 5 | AddEntity twice | 🟡 **Test-harness flaw, no production bug.** `AddEntity`'s duplicate-prevention (reference-equality via `FindEntry`) was already correct. The test's own loop called raw `Tick()` in isolation without ever calling the paired `EndTurn()` — `Tick()` only reports who's ready, it does not spend energy (that's `EndTurn`'s job, called separately by `ProcessUntilPlayerTurn` in real gameplay). Without spending, the actor's energy never resets, so it's returned by every tick once it first qualifies (21 of 30) — nothing to do with duplication. | Test fixed: loop now calls `tm.EndTurn(who)` whenever `who != null`, mirroring real usage. |
| 11 | Identical Speed | 🟡 **Test-harness flaw**, same root cause as #5. `FindNextActor`'s tie-break (equal energy + equal speed keeps whichever was already `best`) means the first-inserted entity wins every tie forever once energy is never spent — the second entity never gets picked at all. | Test fixed: same `EndTurn` call added; once energy resets after each turn, the tie legitimately alternates between the two entities. |
| 13 | Speed=37 (non-divisor) | 🟡 **Test-harness flaw**, same root cause. `GetEnergy(a)` reported the raw un-spent accumulation (37×28=1036); the assertion "leftover in [0,100)" implicitly assumed spending happened automatically, but it never did. | Test fixed: `EndTurn` added; leftover after one real spend is 36, matching the test's own doc-comment prediction. |
| 14 | EndTurn called twice | 🔴 **Real production bug.** `SpendEnergy` had zero idempotency guard — every call unconditionally deducted `ActionThreshold`, so a second (redundant/erroneous) `EndTurn` call for the same actor drove Energy to -1000. | `TurnManager.cs`: `SpendEnergy` now only deducts when `entry.Energy >= ActionThreshold` — matches the exact gate `FindNextActor` used to select the actor in the first place, so normal single-call flow is unaffected and a second call becomes a true no-op. |
| 15 | Dead entity stops acting | 🟡 **Test-harness flaw** (two parts): same missing-`EndTurn` root cause PLUS a wrong assumption that TurnManager has some built-in HP-based auto-removal (per the test's own comment, "the TurnManager either removes them automatically OR a sweep does" — neither exists; `IsRegistered`'s doc-comment says removal is the CALLER's job, done by `CombatSystem.HandleDeath` calling `RemoveEntity`). | Test fixed: `EndTurn` added, plus an explicit `tm.RemoveEntity(dying)` call at the simulated death, mirroring what `CombatSystem.HandleDeath` actually does in production. |

Also fixed while in this file: `AddEntity(null)` (test #4 in the
table above, originally rated "high confidence, PASS") had **zero**
null-guard in production — confirmed genuinely broken (not test
drift), fixed with a one-line guard.

**Lesson for future adversarial-audit docs:** the discipline's own
step 2 ("Run tests; tabulate pass/fail") is only as trustworthy as
the actual run behind it. If a future audit's "0 bugs" result looks
suspicious relative to the LOW-confidence predictions it's paired
with (3 of these 6 were explicitly flagged LOW-confidence by the
original author — exactly the ones most likely to surprise), that's
a signal to re-run and hand-verify rather than trust the table at
face value.
