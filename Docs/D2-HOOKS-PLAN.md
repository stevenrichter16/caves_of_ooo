# D2 — Diag observability hooks expansion

**Status:** plan only.
**Branch:** `feat/diag-D2-hooks` cut from `main` at `d83c4b7`.
**Predecessor:** `Docs/D1-SPIKE-PLAN.md` — substrate + 1 hook + 1 MCP tool.
**Methodology:** CLAUDE.md major-feature workflow.

---

## 1. Goal

Expand the `Diag` substrate from 1 hook (D1.2 — `effect/OnRemove`) to a
useful **vocabulary** of hooks that lets a Claude session reconstruct
a turn's worth of game state from the records alone:

- **What effects were applied?**          → `effect/OnApply`
- **What damage landed?**                  → `damage/DamageDealt`
- **Where did each turn start and end?**   → `turn/Begin`, `turn/End`
- **What caused what?**                    → `Diag.WithCause(traceId)`
  ambient scope so records inside an attack/spell/trap auto-link
  back to a single root trace ID.

Plus one Layer-2 enhancement that pays off with the new hook density:

- **`diag_count`** MCP tool — aggregation: how many records match a
  filter? Critical for "did this fire 0/1/many times?" queries
  without pulling the full record list.

---

## 2. Scope

### In scope (smallest-blast-radius first)

| # | Sub-milestone | LOC est. | New tests |
|---|---|---|---|
| D2.0 | Plan + branch (this commit) | 0 prod, plan to disk | 0 |
| D2.1 | `effect/OnApply` hook in `StatusEffectsPart.ApplyEffectInternal` | ~15 prod, ~80 test | 5 |
| D2.2 | `damage/DamageDealt` hook in `CombatSystem.ApplyDamage` | ~15 prod, ~80 test | 5 |
| D2.3 | `Diag.WithCause(traceId)` ambient scope (replaces D1.1 stub) | ~30 prod, ~90 test | 6 |
| D2.4 | `turn/Begin` + `turn/End` hooks in `TurnManager` | ~20 prod, ~90 test | 6 |
| D2.5 | `diag_count` MCP tool | ~30 prod, ~70 test | 5 |
| D2.6 | Self-review + merge | doc + plan log | 0 |

Estimated total: ~110 prod LOC + ~410 test LOC ≈ **half the size of D1**.

### Out of scope (deferred)

| Out of scope | Where it lands |
|---|---|
| `Entity.FireEvent` universal hook (chatty, off-default) | D3 |
| `since_turn` / `until_turn` filter on `diag_query` (needs causal reasoning + timestamp matching) | D3 |
| `diag_assert` (predicate match) | D3 |
| `diag_inspect_record` (causal walk + related) | D3 |
| `Diag.WithCause(...)` reading from `Entity.FireEvent` automatically | D3 (waits on D3's universal hook) |
| `diag_set_channels` runtime channel toggle MCP tool | D3 |
| Disk-spill persistence (`DiagPersistence.cs`) | D5 |

---

## 3. Verification sweep — premises confirmed BEFORE writing code

| Premise | Status | Source |
|---|---|---|
| `StatusEffectsPart.ApplyEffectInternal` adds to `_effects` at line 110, fires `Applied()` + `SendApplied()` at lines 111-112 | ✅ confirmed | `StatusEffectsPart.cs:52-114` |
| Stacking branch (line 70) returns BEFORE the apply path — D2.1 hook at the apply path NOT in stack branch (hook for stacking is D3+) | ✅ confirmed | `StatusEffectsPart.cs:69-72` |
| `effect.JustApplied` is set at line 105 before `Apply()` returns; payload should include `JustApplied` so causal traces can distinguish trap-step vs cross-actor application | ✅ confirmed | `StatusEffectsPart.cs:104-105` |
| `CombatSystem.ApplyDamage` mutates `hpStat.BaseValue` at line 622 (and HP alias at 626 if present); `DamageDealt` event fires at line 631 only when `source != null` | ✅ confirmed | `CombatSystem.cs:516-637` |
| Hook insertion: between line 626 (HP applied) and line 631 (DamageDealt event), so the record's payload reflects post-damage state. Source-less damage (e.g., trap, environment) ALSO records — even though no DamageDealt event fires for it. | ✅ confirmed | `CombatSystem.cs:619-637` |
| `TurnManager.CurrentActor` is set at line 232 right before `BeginTakeAction` event fires (line 242) and cleared at line 293 after `EndTurn` event fires (line 281) | ✅ confirmed | `TurnManager.cs:232-294` |
| `TurnManager.Active` is the static property used by D1 substrate `TryGetCurrentTurn()` | ✅ confirmed | shipped in `91b8008` |
| `Diag.WithCause(traceId)` exists as a stub at `Diag.cs:286` returning a single boxed `IDisposable` no-op. Replacing it with `AsyncLocal<string>` impl is non-breaking — no caller depends on the return value. | ✅ confirmed | `Diag.cs:286-323`, no `using (Diag.WithCause(...))` callers in repo yet |
| `MCPForUnity.Editor.Tools.CommandRegistry` auto-discovers `[McpForUnityTool]` classes — same pattern as D1.3 `DiagQueryTool` | ✅ confirmed in D1.3 | `Assets/Editor/Diagnostics/DiagQueryTool.cs` |
| `DiagQuery.Filter` already supports the same fields `diag_count` needs; D2.5 tool wraps `DiagQuery.Apply()` and returns `{count, sample_first_trace_id}` instead of the records array | ✅ confirmed | `DiagQuery.cs:18-46` |
| `Diag.IsChannelEnabled("damage")` returns true by default per D1.1 default-on list (`event`, `effect`, `damage`, `turn`) — no SetChannel needed for new hooks | ✅ confirmed | `Diag.cs` (default channel set) |

**Zero false premises.** Verification sweep complete.

---

## 4. Sub-milestone details

### D2.0 — Plan + branch (this commit)

This file. No prod code. Branch `feat/diag-D2-hooks` from `d83c4b7`.

### D2.1 — `effect/OnApply` hook

**Insertion:** `StatusEffectsPart.cs` line ~112 between `_effects.Add(effect)`
and `effect.Applied(ParentEntity)`. Mirrors D1.2's OnRemove pattern.

```csharp
if (Diag.IsChannelEnabled("effect"))
{
    Diag.Record(
        category: "effect",
        kind: "OnApply",
        target: ParentEntity,
        actor: source,
        payload: new
        {
            effect = effect.GetType().Name,
            duration = effect.Duration,
            justApplied = effect.JustApplied,
            forced = forced
        });
}
```

**RED tests** in new `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagOnApplyHookTests.cs`:

1. `ApplyEffect_ProducesOnApplyRecord` — apply Stunned; assert one
   `effect/OnApply` record exists with the right TargetId.
2. `OnApplyRecord_PayloadIncludesEffectName` — assert payload contains
   `"StunnedEffect"`.
3. `OnApplyRecord_PayloadIncludesJustApplied` — apply during own turn
   → JustApplied=true; apply across actors → JustApplied=false.
4. `OnApplyRecord_ActorIsSourceWhenProvided` — apply with `source: attacker`
   → ActorId == attacker.ID.
5. **Counter-check:** `ApplyEffect_StackingDoesNotProduceOnApplyRecord` —
   apply Bleeding twice; the second one merges via `OnStack` (returns
   true at line 70), no second OnApply record is emitted.

### D2.2 — `damage/DamageDealt` hook

**Insertion:** `CombatSystem.cs` line ~627 between HP apply and the
existing `DamageDealt` event. Records ALL damage including source-less
(traps, environment) — broader than the existing event which only
fires when `source != null`.

```csharp
if (Diag.IsChannelEnabled("damage"))
{
    Diag.Record(
        category: "damage",
        kind: "DamageDealt",
        actor: source,
        target: target,
        payload: new
        {
            amount = amount,
            hpAfter = hpStat.BaseValue,
            lethal = hpStat.BaseValue <= 0,
            attributes = damage.AttributesString  // post-resistance routing
        });
}
```

**RED tests** in new `DiagDamageHookTests.cs`:

1. `ApplyDamage_ProducesDamageDealtRecord` — basic 5 damage; assert
   one record with `amount=5`.
2. `DamageRecord_PayloadIncludesHpAfter` — verify `hpAfter` matches
   actual post-damage HP.
3. `DamageRecord_LethalFlagTrueOnKill` — damage that drops HP ≤ 0
   → `lethal=true`.
4. `DamageRecord_FiresEvenWhenSourceIsNull` — environmental damage
   (trap-style) still records. Counter-check that the existing
   `DamageDealt` event doesn't fire here, but the diag record DOES —
   broader observability than the existing event.
5. **Counter-check:** `ZeroDamage_DoesNotRecord` — damage that resolves
   to 0 actual (full resistance, vetoed) returns early at line 620;
   no record fires.

### D2.3 — `Diag.WithCause(traceId)` ambient scope

**Replaces** the D1.1 no-op stub with `AsyncLocal<string>` impl.

```csharp
private static readonly AsyncLocal<string> _currentCause = new();

public static IDisposable WithCause(string traceId)
{
    string previous = _currentCause.Value;
    _currentCause.Value = traceId;
    return new CauseScope(previous);
}

public static string CurrentCause => _currentCause.Value;

private sealed class CauseScope : IDisposable
{
    private readonly string _previous;
    public CauseScope(string previous) => _previous = previous;
    public void Dispose() => _currentCause.Value = _previous;
}
```

`Diag.Record(...)` becomes: `cause = explicit ?? _currentCause.Value`.

**RED tests** in new `DiagWithCauseTests.cs`:

1. `Record_OutsideScope_HasNullCause` — control: no scope active.
2. `Record_InsideScope_PicksUpScopeCause` — `using (Diag.WithCause("abc"))
   { Record(...); }` → record's CauseTraceId == "abc".
3. `ExplicitCauseParam_OverridesScopeCause` — pass `cause:"xyz"` while
   scope active with `"abc"` → "xyz" wins.
4. `NestedScope_InnerWins` — `using ("a") using ("b") { Record(); }`
   → cause = "b". After inner disposed: cause = "a". After outer
   disposed: cause = null.
5. `ScopeIsThreadIsolated` — concurrent threads with different scopes
   don't interfere (AsyncLocal contract). Use `Task.Run` x2.
6. **Counter-check:** `ScopeDisposed_RestoresToNull_NotPreviousString` —
   set scope, dispose; explicit assertion that the static state is
   restored to null (or to the outer scope), not leaked.

### D2.4 — `turn/Begin` + `turn/End` hooks

**Insertion sites:** `TurnManager.cs`:
- After `CurrentActor = actor` (line 232), BEFORE `BeginTakeAction`
  fires — emits `turn/Begin`.
- In `EndTurn` after `actor.FireEventAndRelease(endTurn)` (line 281),
  BEFORE `CurrentActor = null` (line 293) — emits `turn/End`.

```csharp
// turn/Begin
if (Diag.IsChannelEnabled("turn"))
{
    Diag.Record("turn", "Begin", actor: actor,
        payload: new
        {
            entityId = actor?.ID,
            blueprintName = actor?.BlueprintName,
            hp = actor?.GetStatValue("Hitpoints", -1)
        });
}

// turn/End
if (Diag.IsChannelEnabled("turn"))
{
    Diag.Record("turn", "End", actor: actor,
        payload: new
        {
            entityId = actor?.ID,
            blueprintName = actor?.BlueprintName,
            hp = actor?.GetStatValue("Hitpoints", -1)
        });
}
```

**RED tests** in new `DiagTurnHookTests.cs`:

1. `BeginTakeAction_ProducesTurnBeginRecord`
2. `EndTurn_ProducesTurnEndRecord`
3. `TurnPair_BeginBeforeEnd_OrderedInBuffer`
4. `TurnRecord_ActorIdMatchesActor`
5. `TurnRecord_PayloadCarriesHpAtTurnBoundary`
6. **Counter-check:** `BlockedTurn_StillProducesBeginAndEnd` —
   stunned NPC's BeginTakeAction returns false; the EndTurn cleanup
   path (line 244) still produces an End record.

### D2.5 — `diag_count` MCP tool

Wraps `DiagQuery.Apply(filter)`, returns `{count, sample_first_trace_id}`
instead of the records array. Token-cheap aggregation.

```csharp
[McpForUnityTool(name: "diag_count",
    Description = "Count how many records match a filter. Cheap aggregation.")]
public static class DiagCountTool
{
    public static object HandleCommand(JObject @params)
    {
        // Build DiagQuery.Filter, call DiagQuery.Apply with high limit,
        // return { count: result.Records.Count, total_scanned, sample_first_trace_id }
    }
}
```

**RED tests** in new `DiagCountTests.cs` (runtime side):

1. `Count_NoFilter_ReturnsTotalRecordCount`
2. `Count_CategoryFilter_NarrowsCount`
3. `Count_ZeroMatches_ReturnsZero`
4. `Count_WithSampleTraceId_FirstMatchTraceId`
5. **Counter-check:** `Count_RespectsHighInternalLimit` — 600 records
   in buffer → count returns 600 (not 500 from default DiagQuery limit).
   `diag_count` must use a 5000 cap internally (or call a count-only
   path on DiagQuery).

### D2.6 — Self-review + merge

Aggregate findings table. Live MCP verification of `diag_count` and
the new categorization (cross-validate via `diag_query category=damage`
that D2.2 records are reachable). Update `Docs/AI-OBSERVABILITY.md`
"Generic tools" table with `diag_count` row.

---

## 5. Acceptance gates

| # | Gate | How verified |
|---|---|---|
| 1 | `effect/OnApply` records fire on every fresh apply | D2.1 tests 1-5 |
| 2 | OnApply payload distinguishes JustApplied vs not | D2.1 test 3 |
| 3 | `damage/DamageDealt` records fire including source-less | D2.2 test 4 |
| 4 | Damage records carry post-damage HP | D2.2 test 2 |
| 5 | `Diag.WithCause` honors AsyncLocal contract (thread-isolated, restore on dispose) | D2.3 test 5, 6 |
| 6 | Explicit cause param overrides scope | D2.3 test 3 |
| 7 | `turn/Begin` and `turn/End` records fire even on blocked turns | D2.4 test 6 |
| 8 | `diag_count` returns accurate counts via MCP | D2.5 tests 1-5 + live verification |
| 9 | Existing 178/178 EditMode regression sweep still GREEN | per-sub-milestone test runs |

---

## 6. Risks

### R1 — Hook fires inside event handlers, breaking re-entrancy

D2.1 OnApply fires INSIDE `ApplyEffectInternal`. If an `EffectApplied`
listener re-enters `ApplyEffect` (e.g., a part triggers a counter-buff),
the second apply's diag record could land before the first one if
ordering matters.

**Mitigation:** Diag records are oldest-first by write-order, so
inner records DO appear after outer in the buffer — no ordering
issue. Test D2.1 #5 (counter-check on stacking) covers re-entrancy.

### R2 — `AsyncLocal` allocation overhead

Each `WithCause(traceId)` allocates a `CauseScope` IDisposable.
Per-call cost is small but in tight loops (per-effect-tick scopes)
it adds up.

**Mitigation:** Don't put scopes inside per-effect-tick paths. Use
them at the level of "an attack" or "a trap firing" — once per
high-level action, not per low-level tick. Document this in the
Diag.WithCause docstring.

### R3 — Damage hook emits records for healing

`CombatSystem.ApplyDamage` is sometimes called with negative amounts
(?) — would record "damage" with negative `amount`.

**Mitigation:** Verify in D2.2: line 619 clamps at 0, so amount is
always ≥ 0. Healing routes through `Heal` not `ApplyDamage`. No
risk.

### R4 — Turn hooks double-fire if BeginTakeAction is blocked

If `actor.FireEventAndRelease(beginTakeAction)` returns false (line 242)
and the EndTurn cleanup path fires (line 244), we'd get one Begin
and one End record per blocked turn. Is that confusing?

**Mitigation:** No — that's the CORRECT shape. A blocked turn is
still a turn; it spent the actor's energy. Begin + End mark its
boundaries even if no action happened in the middle. Test D2.4 #6
explicitly covers this case.

### R5 — `diag_count` returns stale counts due to async hook firing

Diag records are written synchronously in the same call that triggers
the hook. So the count is "what's in the buffer at query time" —
exactly the right semantics. No staleness risk.

---

## 7. Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ⏳ | this commit |
| User reviews plan | ⏳ | (or proceed by default) |
| D2.1 OnApply hook | ⏳ | RED → GREEN |
| D2.2 DamageDealt hook | ⏳ | RED → GREEN |
| D2.3 WithCause AsyncLocal impl | ⏳ | RED → GREEN |
| D2.4 turn/Begin + turn/End hooks | ⏳ | RED → GREEN |
| D2.5 diag_count MCP tool | ⏳ | RED → GREEN |
| D2.6 self-review + merge | ⏳ | per CLAUDE.md §2.3 |
| Acceptance | ⏳ | gates 1-9 all pass |
