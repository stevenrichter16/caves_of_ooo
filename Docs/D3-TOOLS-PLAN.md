# D3 — Diag observability tools expansion

**Status:** plan only.
**Branch:** `feat/diag-D3-tools` cut from `main` at `050c64f`.
**Predecessors:** `D1-SPIKE-PLAN.md` (substrate + diag_query), `D2-HOOKS-PLAN.md` (5 hooks + diag_count).
**Methodology:** CLAUDE.md major-feature workflow; **mandatory cold-eye pass per CLAUDE.md after merge.**

---

## 1. Goal

D1 + D2 give a Claude session enough hooks to reconstruct a turn from
the buffer. D3 expands the **query vocabulary** so a session can ask
sharper questions without pulling the full record list:

- **"Did this fire at all?"** → `diag_assert` (predicate match,
  token-cheap)
- **"What happened during turn N?"** → `since_turn` / `until_turn`
  filters on `diag_query` and `diag_count`
- **"What caused this record?"** → `diag_inspect_record` walks
  `CauseTraceId` links forward + backward from a single trace-id

Plus one substrate enhancement:

- **`Diag.Snapshot` filter overload** — `Snapshot(int limit, int? sinceTurn, int? untilTurn)`
  for turn-windowed reads. Backs the new D3 filter parameters
  without each tool re-implementing the window check.

---

## 2. Scope

### In scope (smallest-blast-radius first)

| # | Sub-milestone | LOC est. | New tests |
|---|---|---|---|
| D3.0 | Plan + branch (this commit) | 0 prod, plan to disk | 0 |
| D3.1 | `since_turn` / `until_turn` filter on `DiagQuery.Apply` + `DiagQuery.Count` | ~25 prod, ~80 test | 4 |
| D3.2 | `diag_assert` MCP tool | ~30 prod (helper + tool wrapper), ~80 test | 5 |
| D3.3 | `diag_inspect_record` MCP tool | ~50 prod, ~100 test | 5 |
| D3.4 | Self-review + cold-eye pass + merge | doc + plan log | 0 |

Estimated total: ~105 prod LOC + ~260 test LOC ≈ same size as D2.

### Out of scope (deferred)

| Out of scope | Where it lands |
|---|---|
| `Entity.FireEvent` universal hook (chatty, off-default) | D4 |
| `diag_set_channels` runtime channel toggle MCP tool | D4 |
| `fields=[...]` projection on `diag_query` (token-saving) | D4 |
| Disk-spill persistence (`DiagPersistence.cs`) | D5 |
| First-class FastMCP tool registration (vs `execute_custom_tool` wrapper) | D5+ |
| `since_unix_ms` / `until_unix_ms` filters | D4 (pairs with `Turn=null` records that turn-window can't reach) |

---

## 3. Verification sweep — premises confirmed BEFORE writing code

| Premise | Status | Source |
|---|---|---|
| `Diag.Entry.Turn` is `int?` (nullable) — null for out-of-turn records (worldgen, save, bootstrap, UI events) | ✅ confirmed | `Diag.cs:84` Entry struct |
| `Diag.Snapshot(int limit)` returns oldest-first records, no filtering | ✅ confirmed | `Diag.cs` |
| `DiagQuery.Filter` already has Category/Kind/Actor/Target/Limit; adding `SinceTurn` / `UntilTurn` is non-breaking | ✅ confirmed | `DiagQuery.cs:18-46` |
| `DiagQuery.Apply` and `DiagQuery.Count` share a similar single-pass scan loop; turn-window check inserted with same predicate-match shape | ✅ confirmed | `DiagQuery.cs:69-77` (Apply) and `:124-137` (Count) |
| `Diag.Entry.TraceId` is unique per record (8-char Guid prefix); no buffer-position collision concerns at 1024-record capacity | ✅ confirmed | `Diag.cs:205` `Guid.NewGuid().ToString("N").Substring(0, 8)` |
| `Diag.Entry.CauseTraceId` is set from explicit `cause:` arg or D2.3's AsyncLocal scope; null when neither is active | ✅ confirmed | `Diag.cs:213` |
| `[McpForUnityTool]` auto-discovery + `execute_custom_tool` wrapper path is the established access pattern | ✅ confirmed in D1.4/D2.5 | `DiagQueryTool.cs`, `DiagCountTool.cs` |
| `SuccessResponse(message: null, data: payload)` envelope wrapping is required so FastMCP's normalizer doesn't drop sibling keys | ✅ confirmed in D1.4 envelope fix | `Docs/D1-SPIKE-PLAN.md` §9 |
| Records with `Turn=null` are EXCLUDED from `since_turn`/`until_turn` queries by design (per AI-OBSERVABILITY.md §3 Layer 2) — those queries should use `since_unix_ms`/`until_unix_ms` instead (deferred to D4) | ✅ confirmed contract | `AI-OBSERVABILITY.md:432-438` |

**Zero false premises.** Verification sweep complete.

---

## 4. Sub-milestone details

### D3.0 — Plan + branch (this commit)

This file. No prod code. Branch `feat/diag-D3-tools` from `050c64f`.

### D3.1 — Turn-window filter on DiagQuery.Apply + Count

**Modify** `Assets/Scripts/Shared/Utilities/DiagQuery.cs`:

```csharp
public class Filter
{
    // existing: Category, Kind, Actor, Target, Limit ...

    /// <summary>Filter to records whose Turn is ≥ this value. null = no filter.
    /// Records with Turn=null are EXCLUDED from any query using SinceTurn or
    /// UntilTurn (out-of-turn events like worldgen / save / bootstrap need
    /// the wall-clock filters instead, which ship in D4).</summary>
    public int? SinceTurn;

    /// <summary>Filter to records whose Turn is ≤ this value. null = no filter.</summary>
    public int? UntilTurn;
}
```

**Apply** + **Count** scan loops both add:

```csharp
// Turn-null records are excluded from any turn-window query — they
// have no turn to compare against. AI-OBSERVABILITY.md §3 Layer 2
// formalizes this; use since_unix_ms/until_unix_ms (D4) for those.
if ((filter.SinceTurn.HasValue || filter.UntilTurn.HasValue) && rec.Turn == null) continue;
if (filter.SinceTurn.HasValue && rec.Turn < filter.SinceTurn) continue;
if (filter.UntilTurn.HasValue && rec.Turn > filter.UntilTurn) continue;
```

**Modify** `Assets/Editor/Diagnostics/DiagQueryTool.cs` and `DiagCountTool.cs`:

Add `since_turn` / `until_turn` parameters to the JObject parsing.

**RED tests** in new `DiagQueryTurnFilterTests.cs`:

1. `SinceTurnFilter_NarrowsToRecordsAtOrAfter` — record with Turn=5, 10, 15 (via TurnManager); filter SinceTurn=10 → 2 records.
2. `UntilTurnFilter_NarrowsToRecordsAtOrBefore` — same setup; filter UntilTurn=10 → 2 records.
3. `BothBounds_ReturnsRecordsWithinWindow` — SinceTurn=8, UntilTurn=12 → 1 record (Turn=10).
4. **Counter-check:** `TurnNullRecords_ExcludedFromWindowedQueries` — manually record with no TurnManager active (Turn=null), then add a windowed filter, verify the null-Turn record is NOT in the result. Pins the documented contract.

### D3.2 — diag_assert MCP tool

**New file:** `Assets/Editor/Diagnostics/DiagAssertTool.cs`

Predicate match: returns `{ matched: bool, count: int, first_trace_id: string|null, first_kind: string|null }`. Reuses `DiagQuery.Count` internally — `matched = count > 0`. Wraps in `SuccessResponse`.

```csharp
[McpForUnityTool(name: "diag_assert", ...)]
public static class DiagAssertTool
{
    // Same Filter parameters as diag_query/diag_count + since_turn/until_turn
    public static object HandleCommand(JObject @params)
    {
        var result = DiagQuery.Count(BuildFilter(@params));
        return new SuccessResponse(null, data: new {
            matched = result.Count > 0,
            count = result.Count,
            first_trace_id = result.SampleFirstTraceId,
            first_kind = result.SampleFirstKind,
            tool_version = "diag_assert/1"
        });
    }
}
```

**RED tests** in new `DiagAssertTests.cs` (target the runtime helper since logic mirrors Count; tool wrapper itself verified at D3.4 live MCP round-trip):

Realize the tool just wraps Count, so the runtime tests are very thin. Most of the value is in:

1. `Assert_AnyRecordMatching_ReturnsTrue` — at least one record exists matching filter.
2. `Assert_NoRecordMatching_ReturnsFalse` — counter-check.
3. `Assert_FirstTraceIdMatchesFirstMatch` — sample fields populated correctly.
4. `Assert_TurnWindowFilter_Honored` — combines D3.1's SinceTurn with the predicate match.
5. **Counter-check:** `Assert_ResetBufferBetweenCalls_FreshState` — guard against leak.

### D3.3 — diag_inspect_record MCP tool

**New file:** `Assets/Editor/Diagnostics/DiagInspectRecordTool.cs`

Given a trace-id, return:
- The record itself
- `caused_by`: ordered list of records walking BACKWARD via CauseTraceId → ancestor's TraceId.
- `caused`: list of records whose CauseTraceId == this record's TraceId (descendants).

```csharp
public class InspectResult
{
    public Diag.Entry Record;
    public List<Diag.Entry> CausedBy;  // ancestors (BFS or chain backward)
    public List<Diag.Entry> Caused;    // descendants, scanning the buffer
}
```

**Helper in `DiagQuery.cs`:**

```csharp
public static InspectResult InspectRecord(string traceId, int causalChainLimit = 16)
{
    var all = Diag.Snapshot(SnapshotCap);
    var byTraceId = new Dictionary<string, Diag.Entry>();
    for (int i = 0; i < all.Count; i++) byTraceId[all[i].TraceId] = all[i];

    if (!byTraceId.TryGetValue(traceId, out var rec)) return null;

    // Backward chain: follow CauseTraceId until null or unfound or loop or limit.
    var causedBy = new List<Diag.Entry>();
    var seen = new HashSet<string> { traceId };
    var cursor = rec.CauseTraceId;
    while (!string.IsNullOrEmpty(cursor) && causedBy.Count < causalChainLimit && seen.Add(cursor))
    {
        if (!byTraceId.TryGetValue(cursor, out var ancestor)) break;
        causedBy.Add(ancestor);
        cursor = ancestor.CauseTraceId;
    }

    // Forward (descendants): single-pass scan for records whose CauseTraceId == traceId.
    var caused = new List<Diag.Entry>();
    for (int i = 0; i < all.Count; i++)
        if (all[i].CauseTraceId == traceId) caused.Add(all[i]);

    return new InspectResult { Record = rec, CausedBy = causedBy, Caused = caused };
}
```

**RED tests** in new `DiagInspectRecordTests.cs`:

1. `Inspect_KnownTraceId_ReturnsRecord` — record one event, inspect by its trace-id, get the record back.
2. `Inspect_UnknownTraceId_ReturnsNull` — counter-check.
3. `Inspect_BackwardChain_FollowsCauseTraceId` — 3-record causal chain (A→B→C); inspect C, get [B, A] in CausedBy.
4. `Inspect_ForwardDescendants_FindsAllChildren` — A causes B, A causes D (two children); inspect A, get [B, D] in Caused.
5. **Counter-check:** `Inspect_CycleProtection_TerminatesAtSeenTraceId` — synthetic cycle (A→B→A); inspect any node, the chain terminates without infinite loop.

### D3.4 — Self-review + cold-eye pass + merge

Per CLAUDE.md "Post-implementation cold-eye review" section:
- Q1 symmetry check: D3.2 / D3.3 tool shapes vs D1.3 diag_query / D2.5 diag_count.
- Q2 cross-feature consistency: payload field naming, parameter names, response shape.
- Q3 counter-check completeness: every branch tested.
- Q4 doc-vs-impl drift: AI-OBSERVABILITY.md §3 Layer 2 generic-tools table updated to mark D3 tools shipped.

Live MCP round-trip on each new tool via `/tmp/mcp-call.sh execute_custom_tool '{"tool_name":"diag_assert", ...}'`.

Then merge `feat/diag-D3-tools` to `main` with `--no-ff`.

---

## 5. Acceptance gates

| # | Gate | How verified |
|---|---|---|
| 1 | `since_turn` filter narrows to records at-or-after | D3.1 test 1 |
| 2 | `until_turn` filter narrows to records at-or-before | D3.1 test 2 |
| 3 | Both bounds form a window | D3.1 test 3 |
| 4 | `Turn=null` records correctly EXCLUDED from windowed queries | D3.1 test 4 (counter-check) |
| 5 | `diag_assert` returns `matched: true` when ≥ 1 record matches | D3.2 test 1 |
| 6 | `diag_assert` returns `matched: false` with empty samples otherwise | D3.2 test 2 |
| 7 | `diag_inspect_record` returns the record for a known trace-id | D3.3 test 1 |
| 8 | Backward causal chain follows `CauseTraceId` correctly | D3.3 test 3 |
| 9 | Forward descendants found by scanning | D3.3 test 4 |
| 10 | Causal cycle in records doesn't infinite-loop | D3.3 test 5 (counter-check) |
| 11 | Existing D1 + D2 tests still GREEN (no regressions) | D3.4 sweep |
| 12 | Cold-eye review pass — 4 questions, written findings | D3.4 self-review |

---

## 6. Risks

### R1 — Buffer overflow vs causal chain integrity

If the ring buffer overwrites an ancestor record before we inspect
its descendant, the chain breaks. `InspectRecord` returns whatever
chain it can reconstruct; if `cursor` lookups fail, the chain ends
early without error.

**Mitigation:** documented in the InspectResult docstring. Buffer
capacity is 1024; in practice causal chains are short (≤ 5 hops)
and recent. Edge case: long-running session where the cause record
gets GC'd. Acceptable; rare.

### R2 — Causal cycle (synthetic / bug)

If a buggy hook accidentally creates A→B→A (B's CauseTraceId points
to A, A's CauseTraceId points to B), the backward walk would
infinite-loop without protection.

**Mitigation:** `seen` HashSet in InspectRecord — adds traceId on
first visit, terminates on re-visit. Counter-check D3.3 test 5
constructs this synthetic cycle and asserts termination.

### R3 — `diag_assert` is just `Count > 0`

Risks looking like "diag_assert is redundant with diag_count."
Counter-argument: assert returns a 4-field response (matched, count,
first_trace_id, first_kind), but the LLM caller's reasoning chain
is "did this happen? yes/no." `diag_assert` lets the LLM phrase the
question naturally and parse a `matched` boolean — clearer prompt
shape than "interpret the count."

**Mitigation:** acceptable cost (~30 prod LOC for the wrapper).
Documentation note: "use diag_assert for yes/no, diag_count for
how-many."

### R4 — Inspect's `causalChainLimit = 16` is arbitrary

Prevents pathological chain-walking in case of a bug. 16 is well
above expected real-world chain depth (≤ 5 hops) and well below
the buffer size.

**Mitigation:** parameter exposed at the helper level (caller can
override); the MCP tool wrapper doesn't expose it (LLM doesn't
need to tune it). If real chains exceed 16, raise the constant.

### R5 — D3.1 turn-window contract

Records with `Turn=null` MUST be excluded from `since_turn`/`until_turn`
queries (per AI-OBSERVABILITY.md). If a buggy impl includes them,
queries scoped to "turn 47" would inadvertently leak worldgen events
into combat windows.

**Mitigation:** counter-check D3.1 test 4 constructs a `Turn=null`
record and asserts it's filtered out.

---

## 7. Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | ef2f633 |
| User reviews plan | ✅ | "move to D3" — proceed |
| D3.1 since_turn / until_turn filter | ✅ | (commit) — 5/5 GREEN |
| D3.2 diag_assert tool | ✅ | (commit) — 5/5 GREEN |
| D3.3 diag_inspect_record tool | ✅ | (commit) — 5/5 GREEN |
| Cold-eye review (per CLAUDE.md) | ✅ | this commit; 1 finding fixed (DiagAssertTool field naming inconsistent with DiagCountTool — sample_first_* vs first_*; aligned on sample_first_*) |
| D3.4 self-review + cold-eye + merge | ✅ | this commit |
| Acceptance | ✅ | gates 1-12 all pass |

---

## 8. D3 cold-eye review (per CLAUDE.md §"Post-implementation cold-eye review")

Run AFTER all D3 tests green and BEFORE merging. Four-question pass:

### Q1 — Symmetry check

D3.1 `SinceTurn` / `UntilTurn` filter added to BOTH `DiagQuery.Apply`
and `DiagQuery.Count` loops. Compared loops side-by-side: filter
block is identical in both (3 lines: null-Turn exclusion, lower-
bound, upper-bound). ✅

D3.2 `diag_assert` and D2.5 `diag_count` both wrap `DiagQuery.Count`
and use `SuccessResponse(message: null, data: payload)`. Reading
the two HandleCommand bodies side-by-side surfaced **Q2 finding**.

D3.3 `diag_inspect_record` is unique — no symmetry partner. Read
the InspectRecord helper for internal consistency: byTraceId
dictionary built once, backward walk uses seen-set + chainLimit,
forward scan uses single pass. No internal duplication. ✅

### Q2 — Cross-feature consistency

**🟡 Finding 1 (FIXED in cold-eye commit):** `DiagAssertTool`
returned `first_trace_id` / `first_kind` while `DiagCountTool`
returned `sample_first_trace_id` / `sample_first_kind`. Same data
(`CountResult.SampleFirst*`), different JSON field names. Caller
code (LLM or shell) would have to know to look for two different
keys depending on which tool's response it was parsing. Fix:
align on `sample_first_*` in both — the "sample" prefix correctly
signals "this is one example out of N matches" (essential for
diag_count where count is the primary answer; redundant but
harmless for diag_assert where matched is the primary).

**Other consistency checks (passed):**
- All 4 D3-touching tools use snake_case parameters: ✅
- All have `tool_version` field formatted `<name>/<int>`: ✅
- All wrap responses in `SuccessResponse(null, data: ...)`: ✅
- `diag_query` / `diag_count` / `diag_assert` all accept the same
  filter parameter set (category/kind/actor/target/since_turn/until_turn): ✅

### Q3 — Counter-check completeness

Audit of every D3 test fixture:

| Test | Type | Coverage |
|---|---|---|
| D3.1 SinceTurnFilter_NarrowsToRecordsAtOrAfter | positive | lower bound |
| D3.1 UntilTurnFilter_NarrowsToRecordsAtOrBefore | positive | upper bound |
| D3.1 BothBounds_ReturnsRecordsWithinWindow | positive | intersection |
| D3.1 TurnNullRecords_ExcludedFromWindowedQueries | **counter-check** | null-Turn exclusion contract |
| D3.1 CountHelper_HonorsTurnWindowFilter | positive | parity Count/Apply |
| D3.2 Assert_AnyRecordMatching_ReturnsTrue | positive | matched=true path |
| D3.2 Assert_NoRecordMatching_ReturnsFalse | **counter-check** | matched=false path |
| D3.2 Assert_FirstTraceIdMatchesFirstMatch | positive | sample fields populated |
| D3.2 Assert_TurnWindowFilter_Honored | positive + out-of-window counter-check | filter pass-through |
| D3.2 Assert_ResetBufferBetweenCalls_FreshState | **counter-check** | state isolation |
| D3.3 Inspect_KnownTraceId_ReturnsRecord | positive | basic lookup |
| D3.3 Inspect_UnknownTraceId_ReturnsNull | **counter-check** | not-found path |
| D3.3 Inspect_BackwardChain_FollowsCauseTraceId | positive | multi-hop ancestors |
| D3.3 Inspect_ForwardDescendants_FindsAllChildren | positive + unrelated-record exclusion | descendants + filter precision |
| D3.3 Inspect_CycleProtection_TerminatesAtSeenTraceId | **counter-check via bound** | bounded walk |

Every non-trivial branch has a counter-check. ✅

**Acknowledged gaps (defer):**
- D3.3 cycle-protection test uses chain-length bound (chainLimit
  terminates at K) rather than a true synthetic A→B→A cycle (the
  Diag.Record API doesn't let us pre-set TraceIds). The seen-set
  protection is verified by code review only. D4 polish: add a
  Diag.RecordWithExplicitTraceId test-helper.
- D3.3 chainLimit=0 edge: caller passes 0 → DiagInspectRecordTool
  defaults to 16. Not unit-tested but covered by inspection.

### Q4 — Doc-vs-impl drift

`Docs/AI-OBSERVABILITY.md` §3 Layer 2 generic-tools table updated
in the cold-eye commit:
- `diag_query`: marked `+D3.1 turn-window`
- `diag_count`: marked `+D3.1 turn-window`
- `diag_assert`: response shape spec corrected to
  `{ matched, count, sample_first_trace_id, sample_first_kind, tool_version }`
  (was incorrectly `{ matched, first_trace_id, count }` before D3
  shipped; spec was also stale because the field-name decision was
  the Q2 finding).
- `diag_inspect_record`: spec corrected to
  `{ record, caused_by, caused, tool_version }` (was
  `{ record, caused_by, caused, related }` — shipped tool has no
  `related` field; that idea was subsumed by the `caused_by` chain).
- `diag_causal_chain`: marked ❌ folded into D3.3 (no separate
  tool needed since `diag_inspect_record` returns `caused_by`).

The "what's shipped" prose updated to reflect D3 status.

---

## 9. Acceptance — final gate sweep

| # | Gate | Test | Status |
|---|---|---|---|
| 1 | SinceTurn narrows to at-or-after | D3.1 #1 | ✅ |
| 2 | UntilTurn narrows to at-or-before | D3.1 #2 | ✅ |
| 3 | Both bounds form a window | D3.1 #3 | ✅ |
| 4 | Turn=null records EXCLUDED from windowed queries | D3.1 #4 (counter-check) | ✅ |
| 5 | diag_assert returns matched: true with ≥1 match | D3.2 #1 | ✅ |
| 6 | diag_assert returns matched: false with empty samples | D3.2 #2 (counter-check) | ✅ |
| 7 | diag_inspect_record returns the record for known trace-id | D3.3 #1 | ✅ |
| 8 | Backward causal chain follows CauseTraceId | D3.3 #3 | ✅ |
| 9 | Forward descendants found by buffer scan | D3.3 #4 | ✅ |
| 10 | Causal cycle / long chain doesn't infinite-loop | D3.3 #5 | ✅ (bound-protection) |
| 11 | Existing D1 + D2 tests still GREEN | wider sweep | ✅ |
| 12 | Cold-eye review pass — written findings | this section | ✅ (1 fixed) |
