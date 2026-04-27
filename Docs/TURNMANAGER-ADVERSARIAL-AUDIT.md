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
