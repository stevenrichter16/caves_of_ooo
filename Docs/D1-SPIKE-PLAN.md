# D1 Spike — Diag observability vertical-slice plan

**Status:** plan only — no code yet.
**Branch (proposed):** `feat/diag-spike` cut from `main` at `0b12da3`.
**Plan ref:** `Docs/AI-OBSERVABILITY.md` §11 third-pass recommended posture.
**Methodology:** CLAUDE.md major-feature workflow (full).

---

## 1. Goal — what the spike must prove (and what it must not)

### What the spike proves

A **vertical slice** through the entire diag substrate from a real
production code path → ring-buffer record → MCP tool query → JSON
response. Three round-trips:

1. **Code → buffer.** `Diag.Record(...)` from a hook in
   `StatusEffectsPart.RemoveEffectAt` lands in the in-memory ring.
2. **Buffer → query.** Direct (in-process) calls to `Diag.Snapshot(...)`
   return the records. Tests pass.
3. **Query → MCP.** A new `[McpForUnityTool] DiagQueryTool` registered
   with the running MCP server, callable from `/tmp/mcp-call.sh`,
   returns the same records as JSON.

If all three round-trips work end-to-end on the bear-trap showcase,
the architecture is real. The remaining D2-D5 work (more hooks, more
tools, persistence) is mechanical execution of the same pattern.

### What the spike does NOT prove (deliberately deferred)

| Out of scope for spike | Where it lands |
|---|---|
| Other hooks (`Entity.FireEvent`, `TurnManager`, `ApplyDamage`, etc.) | D2 |
| Other MCP tools (`diag_assert`, `diag_count`, `diag_inspect_entity`, `diag_causal_chain`, `diag_set_channels`, etc.) | D3-D5 |
| Disk-spill persistence (`DiagPersistence.cs`) | D5 |
| `using (Diag.WithCause(...))` ambient cause scope | D2 (when 2+ hooks exist) |
| Specialized tools (`diag_damage_history`, `diag_effect_lifecycle`) | post-Tier-1 |
| Solving the bear-trap bleeding deferred bug | D5 (full Tier-1 ship) |
| `Group="diagnostics"` + `manage_tools` enabling | D3 (until then, default `Group="core"`) |

The spike is the architectural proof; the rest of Tier-1 scales it.

### Success criteria (3 hard gates — all must pass)

**Gate 1 — substrate works in-process (verified by EditMode tests):**

- `Diag_AcceptsArbitraryNewCategoryWithoutCodeChanges` — record with
  a never-before-used category string; query it back. Verifies the
  P8 "single hook surface, multi-flavored records" principle.
- `Diag_AcceptsNullTurn_ForOutOfTurnEvents` — record with no
  `TurnManager.Active` set; verify `Turn == null`. Validates the
  generality fix from §11 third-pass.
- `Diag_RingBuffer_OverwritesOldestOnOverflow` — fill past 1024
  records; verify the buffer wraps and oldest records are dropped
  (with an internal counter incrementing).
- `Diag_DisabledChannel_DoesNotRecord` — set channel off; record;
  buffer is empty. Validates P9 (cost when disabled).
- `Diag_PayloadIsEagerlySerialized` — record an object whose field
  changes after the call; verify the recorded payload reflects the
  pre-call value. Validates the §3 Layer 0 eager-serialization pin.

**Gate 2 — hook fires AND tool returns the record (combined live verification):**

This gate verifies the full vertical slice in one motion: code path → buffer → tool query → JSON response.

- Walk onto bear trap in `TrapFurnitureShowcase`.
- After the first turn passes (effect removal happens), the substrate
  contains at least one `category=effect kind=OnRemove` record whose
  `TargetId` is the player's ID. **(Hook fired.)**
- The same record is returned by `/tmp/mcp-call.sh diag_query
  '{"category":"effect","kind":"OnRemove"}'` as JSON. **(Tool round-trip works.)**

If the hook fires but the tool returns an empty result, the bug is in
the tool. If the tool returns a result but the hook didn't fire, the
bug is in the hook. Either failure mode is informative.

**Gate 3 — JSON response shape is valid:**

- The response from `diag_query` is shaped `{ success, message, error, data: { meta, data, truncated, ... } }`.
- `meta` block includes `build_sha`, `turn`, `timestamp_unix_ms`,
  `session_id`, `buffer_fill_pct`, `dropped_records`.
- Filter parameters work in isolation: `category`, `kind`, `target`, `limit` each narrow the result independently (verified per test in D1.3).
- Tool registered with MCP server on Unity startup (visible in `tools/list`).

If any of the 3 gates fails, the spike has revealed an architecture
problem that must be addressed before D2-D5 proceeds.

---

## 2. Verification sweep — premises confirmed BEFORE writing code

### Critical finding: namespace conflict avoided

**The `CavesOfOoo.Diagnostics` namespace already exists.** Three
existing files live there:

| File | Purpose |
|---|---|
| `Assets/Scripts/Shared/Utilities/PerformanceDiagnostics.cs` | Frame-level perf counter snapshots |
| `Assets/Scripts/Shared/Utilities/PerformanceMarkers.cs` | `Unity.Profiling.ProfilerRecorder` markers |
| `Assets/Scripts/Shared/Utilities/AIDebug.cs` | Runtime AI inspector toggles |

10+ files already do `using CavesOfOoo.Diagnostics;` (TurnManager,
CombatSystem, BrainPart, several test fixtures, etc.).

**Implication for the plan:**

- ❌ **Do NOT create `Assets/Scripts/Diagnostics/Diag.cs`** (the
  AI-OBSERVABILITY.md §4 file table). That contradicts the existing
  pattern.
- ✅ **Create `Assets/Scripts/Shared/Utilities/Diag.cs`** in namespace
  `CavesOfOoo.Diagnostics`. Sibling to `PerformanceDiagnostics.cs`.

This is a doc/reality drift in `Docs/AI-OBSERVABILITY.md` §4. After
the spike, the doc's file paths will need a correction commit.

### Other premises (all confirmed)

| Premise | Status | Source |
|---|---|---|
| `[McpForUnityTool]` attribute auto-discovers via `CommandRegistry.AutoDiscoverCommands` (scans `AppDomain.CurrentDomain.GetAssemblies()`) | ✅ confirmed Step 0 | `MCPForUnity/Editor/Tools/CommandRegistry.cs:60+` |
| Boilerplate template: `static class XxxTool` with `public static object HandleCommand(JObject @params)` | ✅ confirmed | `unity-mcp/CustomTools/RoslynRuntimeCompilation/ManageRuntimeCompilation.cs` |
| `MCPForUnity.Editor.Helpers.ErrorResponse` exists for consistent error returns | ✅ confirmed | `unity-mcp/MCPForUnity/Editor/Helpers/Response.cs:35` |
| Newtonsoft.Json is available for JObject runtime parsing | ✅ confirmed | already used by all existing custom tools |
| `Entity.ID` is a public string field | ✅ confirmed | `Entity.cs:14` |
| `Entity.BlueprintName` and `Entity.GetDisplayName()` are accessible | ✅ confirmed | `Entity.cs:15, 391` |
| `StatusEffectsPart.RemoveEffectAt` is a 6-line private method at line 477 | ✅ confirmed | `StatusEffectsPart.cs:477-484` |
| `Effect.LastRemovalCause` carries the cause string set by remove paths | ✅ confirmed | `Effect.cs:68` |
| `EditModeTests.asmdef` references `CavesOfOoo` (via GUID) → can use the new `Diag` class | ✅ confirmed | manifest references `GUID:ab5401a0...` |
| Existing test fixture pattern uses `[SetUp]` with a `Reset*` static call | ✅ confirmed | `PerformanceDiagnosticsTests.cs:5-9` |
| `TurnManager.Active` is a static property set in the constructor | ✅ confirmed | shipped in `fix/effect-tick-by-current-actor` (commit `91b8008`) |
| No existing `[McpForUnityTool]` class lives under `Assets/` (this spike pioneers the pattern in the caves-of-ooo project) | ✅ confirmed | grep returns no matches |

**Zero false premises detected.** The spike is unblocked.

---

## 3. Risk register — every concern I can identify, with mitigation

Pre-emptively flagging risks so they don't surface mid-build as
"unknown unknowns." Each risk has a mitigation already designed in.

### R1 — Anonymous-object payload could recurse into Entity, blowing up the JSON

**Concern:** `Diag.Record(target: livePlayerEntity, payload: new { effect = bleedingEffect })`. Newtonsoft serializes anonymous objects member-wise. If `BleedingEffect.Owner` references back to the player, we serialize the entire entity graph including all Parts, Stats, Effects. Cyclic. Slow. Possibly OOM on a complex scene.

**Mitigation:**
- The `Diag.Record` API takes `Entity actor` and `Entity target` as
  separate parameters; the substrate itself extracts only
  `{ id, blueprintName, displayName }` for those (small fixed shape).
- For the `payload` parameter, the substrate uses a Newtonsoft
  `JsonSerializerSettings` with `ReferenceLoopHandling = Ignore`
  and a `MaxDepth = 4`. Cycles are silently dropped; deep graphs
  are truncated.
- Anti-pattern documented in `AI-OBSERVABILITY.md` §3 Layer 5:
  "Don't store entity object references in records." Hooks should
  pass extracted scalars in the payload object, not live entity
  references.
- Verified by test `Diag_PayloadWithEntityReference_DoesNotRecurse`.

### R2 — Ring-buffer concurrency between gameplay-thread Record and tool-thread Snapshot

**Concern:** `Diag.Record` runs on the Unity main thread (gameplay).
The MCP tool dispatcher could run on the bridge's WebSocket thread.
Reading the buffer while it's being written may produce torn reads
or skipped records.

**Mitigation:**
- Investigate during D1.1: read `MCPForUnity/Editor/Services/Transport/`
  to confirm whether tool execution is marshaled back to the main
  thread (likely yes — most Unity APIs require main thread).
- If main-thread dispatch is confirmed: no concurrency, no locks.
  Note in the code comment.
- If NOT confirmed: add a single `lock` around the ring-buffer
  write+read, OR (better) use `Volatile.Read`/`Volatile.Write` on
  the next-write index for lock-free reads (snapshot may be slightly
  stale but never torn).
- This decision is made in D1.1 and documented; do not over-engineer
  before knowing.

### R3 — Domain reload between sessions clears the static `Diag` ring buffer mid-investigation

**Concern:** Modifying any `.cs` file triggers Unity domain reload,
which resets all static state. If I'm investigating a bug and edit a
file, the buffer is wiped before my next query.

**Mitigation:**
- Documented as a known limitation; spill to disk (`DiagPersistence`)
  in D5 will fix this.
- For the spike: surfaced in `meta.dropped_records` and
  `meta.session_id` (UUID created on Diag's static constructor).
  Tools warn if the session ID changed between calls.
- Spike acceptance criterion explicitly says "no editing during
  the trap-showcase verification run."

### R4 — `[McpForUnityTool]` discovery race on first run

**Concern:** When I add the new `DiagQueryTool.cs`, the auto-discovery
runs on next domain reload. But the WebSocket bridge to the Python
server might not auto-reregister tools after reload — it only sends
on connect. If the MCP server was already running, my new tool
might not be visible until I restart the server.

**Mitigation:**
- Investigate during D1.4: read `WebSocketTransportClient.cs:577`
  (`ReregisterToolsAsync`) — figure out whether this auto-fires on
  reload or needs manual trigger.
- If it doesn't auto-fire: invoke `manage_editor stop` + restart, or
  call `ReregisterToolsAsync` explicitly via `execute_code`. Document
  in the spike's "before testing the tool" runbook.

### R5 — `Diag.Record` cost on hot paths regresses combat performance

**Concern:** `StatusEffectsPart.RemoveEffectAt` runs every time an
effect ends. With my hook, every removal incurs JSON serialization
cost (~1-5 µs per `AI-OBSERVABILITY.md` §3 Layer 0). Multiplied
over a populated combat scene, this could be a measurable regression.

**Mitigation:**
- The first thing `Diag.Record` does is check `IsChannelEnabled`.
  Cost when disabled: one bool dictionary lookup, ~5 ns. The hook
  is essentially free until someone explicitly turns the channel
  on.
- The `effect` channel is **on by default**. So in normal play,
  every effect removal pays the ~1-5 µs serialization cost.
- D1.5 includes a `ProfilerRecorder` benchmark: 100-effect-removal
  loop, before/after, must show < 5% regression. If it regresses
  more, the hook is gated behind `effect` being explicitly enabled.
- Per CLAUDE.md "profile before optimizing" — measure, don't speculate.

### R6 — JObject parameter parsing fails silently on missing fields

**Concern:** `@params["category"]?.ToString()` returns null if the
caller forgot the parameter. If we don't validate, the tool returns
all records unfiltered, which then trips the budget enforcement and
returns `truncated`. Confusing error path.

**Mitigation:**
- Each parameter explicitly null-checked at the top of
  `HandleCommand` with a meaningful `ErrorResponse` if required
  fields are missing.
- For optional filters: null means "don't filter on this field" —
  documented inline.
- Test: `DiagQueryTool_MissingRequiredParam_ReturnsErrorResponse`.

### R7 — Test fixture leakage between tests

**Concern:** `Diag` ring buffer is static. Tests that record records
will see leftover state from prior tests in the same fixture.

**Mitigation:**
- Pattern from `PerformanceDiagnosticsTests`: `[SetUp]` calls
  `Diag.ResetAll()` (a new public method I'll add for tests only,
  with `[Conditional("UNITY_INCLUDE_TESTS")]` or just a public
  reset method).
- Documented in the test file's class-level comment.

### R8 — Disposable struct for `Diag.WithCause(...)` deferred to D2 but referenced in §3

**Concern:** `AI-OBSERVABILITY.md` §3 Layer 0 specifies the public
API including `Diag.WithCause(traceId)`. The spike defers this to
D2 when 2+ hooks exist. If I leave it as a missing method, the
substrate doesn't match the doc contract.

**Mitigation:**
- Implement the bare scaffolding in D1.1: a public method that
  returns `IDisposable`. The body is empty (no cause threading
  yet). Substrate matches contract; behavior is no-op until D2.
- Documented inline: "no-op until D2 wires it up."

### R9 — `Diag.Snapshot` could throw mid-iteration; tool would crash without handling

**Concern:** The ring buffer is iterated by `Snapshot(...)` to filter
records. If the substrate has a bug (e.g., index out of range during
the wrap, NullReference on a malformed Record), the exception bubbles
up to `DiagQueryTool.HandleCommand`, which propagates to the MCP
dispatcher, which returns a generic error to the caller. The tool
becomes useless for diagnosing the substrate.

**Mitigation:**
- `DiagQueryTool.HandleCommand` wraps the entire body in `try/catch`.
  On exception, returns `ErrorResponse($"Diag tool failed: {ex.Message}")`
  with stack trace logged via `Debug.LogException` (visible to
  `read_console`).
- The tool itself should not be the place where Diag bugs get
  discovered: `DiagTests.DiagTests_Snapshot_DoesNotThrowOnEmpty`
  and similar substrate tests catch substrate issues in D1.1.
- Documented as a §3 anti-pattern: "MCP tool wrappers wrap their
  implementation in try/catch; substrate bugs become tool errors,
  not silent crashes."

### R10 — The bear-trap bleeding bug is still present and might mask spike verification

**Concern:** Gate 2 verification walks onto the bear trap and looks
for an `OnRemove BleedingEffect` record. But the bug parked in
`Docs/KNOWN-ISSUES/BEAR-TRAP-BLEEDING-EVAPORATES.md` *is* the user's
report that bleeding is being silently removed. So we should see
the record — and that's the bug we want to debug eventually. Spike
verification should still pass: the record DOES land, so the
substrate works.

**Mitigation:**
- This is actually a feature, not a risk: the spike's "I see an
  unexpected `OnRemove` record" IS the diagnostic we'll use during
  D5 to solve the deferred bug.
- Spike acceptance: ANY `OnRemove` record (Stunned removal, Burning
  removal, Bleeding removal, even a no-effect non-trap turn) proves
  the hook fires. Doesn't have to be Bleeding specifically.

---

## 4. Files to create / modify (exact deltas)

| Path | Op | LOC | Purpose |
|---|---|---|---|
| `Assets/Scripts/Shared/Utilities/Diag.cs` | **NEW** | ~220 | Substrate: `Record` struct + ring buffer + `Record(...)` API + `Snapshot(int)` + `IsChannelEnabled` + `SetChannel` + `ResetAll` (test helper) + `WithCause` no-op stub |
| `Assets/Scripts/Shared/Utilities/Diag.cs.meta` | NEW (auto) | n/a | Unity meta |
| `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs` | MOD | +3 | Hook in `RemoveEffectAt`: `Diag.Record("effect", "OnRemove", target: ParentEntity, payload: new { effect = effect.GetType().Name, duration = effect.Duration, cause = effect.LastRemovalCause })` |
| `Assets/Editor/Diagnostics/DiagQueryTool.cs` | **NEW** | ~150 | `[McpForUnityTool("diag_query")]` static class + nested `Parameters` class + `HandleCommand(JObject)` with category/kind/target/limit filtering + budget enforcement + meta block |
| `Assets/Editor/Diagnostics/DiagQueryTool.cs.meta` | NEW (auto) | n/a | Unity meta |
| `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagTests.cs` | **NEW** | ~180 | 5 substrate tests + 2 counter-checks (Gate 1) + 1 hook integration test (Gate 2 in-process slice) |
| `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagTests.cs.meta` | NEW (auto) | n/a | Unity meta |
| `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagPerfTest.cs` | **NEW** | ~50 | 3 perf benchmarks (D1.5) — disabled-channel cost, enabled-channel cost, RemoveEffect regression |
| `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagPerfTest.cs.meta` | NEW (auto) | n/a | Unity meta |

**Total:** ~603 LOC new + ~3 LOC modified across 1 production file.

This is **smaller than the §4 D1 file table** because:

1. The spike only needs ONE tool, not ten. Tier 1 D3-D5 add the rest.
2. The substrate has the API surface but defers persistence to D5 (no `DiagPersistence.cs`).
3. The hook insertion is a single line; the supporting test file is real but bounded.

---

## 5. Sub-milestones (smallest blast radius first)

Each sub-milestone is **its own commit**. RED→GREEN→counter-check→
adversarial→review→commit per CLAUDE.md §2.1.

### D1.1 — Substrate API surface (one commit, ~220 LOC)

**File:** `Assets/Scripts/Shared/Utilities/Diag.cs` (new, no hooks).

**RED tests (write first, observe RED):**
1. `Diag_AcceptsArbitraryNewCategoryWithoutCodeChanges` — record with
   `category="smoke_test_category"`, query, expect 1 record.
2. `Diag_AcceptsNullTurn_ForOutOfTurnEvents` — record with no
   `TurnManager.Active`, expect `Turn == null`.
3. `Diag_RingBuffer_OverwritesOldestOnOverflow` — fill 1100 records,
   buffer holds last 1024, dropped count = 76.
4. `Diag_DisabledChannel_DoesNotRecord` — `SetChannel("foo", false)`,
   record, snapshot empty.
5. `Diag_PayloadIsEagerlySerialized` — record with payload object,
   mutate object after Record, snapshot reflects pre-mutation state.

Plus 2 counter-checks (CLAUDE.md §3.4):
6. `Diag_EnabledChannel_DoesRecord` — counterpart to #4 (verify
   #4's precondition wasn't vacuous).
7. `Diag_PayloadWithEntityReference_DoesNotRecurse` — record an
   Entity in payload; verify only `{id, blueprintName, displayName}`
   is captured, not the whole entity graph (validates R1 mitigation).

**Implementation:** `Diag.cs` with all-purpose `Record(...)` method,
ring buffer storage, channel-state dict, `Snapshot(limit)`, `ResetAll()`.

**GREEN expected for tests 1-7.**

**Self-review checklist (per CLAUDE.md §5):**
- 🟡 Concurrency mitigation per R2: investigate transport
  threading, document the choice in code comment.
- 🟡 Confirm `JsonSerializerSettings` per R1 work as expected
  (test #7 covers this).
- 🟡 LOC count matches estimate (~220).

### D1.2 — Hook in `StatusEffectsPart.RemoveEffectAt` (one commit, ~3 LOC)

**File:** `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs`
(modify, +3 lines).

**RED test (write first):**
8. `RemoveEffect_ProducesDiagOnRemoveRecord` — apply Stunned to
   entity, remove it, snapshot Diag, expect record with
   `category="effect" kind="OnRemove" target=entity.ID
   payload.effect="StunnedEffect"`.

**Implementation:** add the `Diag.Record(...)` call to
`RemoveEffectAt(int index)` between `effect.Remove(ParentEntity)`
and `effect.Owner = null`. (Reading `LastRemovalCause` requires
the effect still has its state intact.)

**GREEN expected for test 8.**

**Counter-check (already covered by D1.1 test #4):** the hook is
gated by `Diag.IsChannelEnabled("effect")`, so disabling the
channel produces no record.

**Self-review:**
- 🟡 Hook position: BEFORE `Owner = null` so the record can read
  effect state without null-deref risk.
- 🟡 Cost when disabled (one dict lookup) — confirmed acceptable
  via D1.5 benchmark.

### D1.3 — `DiagQueryTool` MCP tool (one commit, ~150 LOC)

**File:** `Assets/Editor/Diagnostics/DiagQueryTool.cs` (new).

**RED tests (in-process):**
9. `DiagQueryTool_NoFilters_ReturnsAllRecords` — record N records,
   call `HandleCommand(new JObject())`, get back all N (or up to
   default limit 50).
10. `DiagQueryTool_CategoryFilter_NarrowsResults` — record records in
    multiple categories, query for one, get only that subset.
11. `DiagQueryTool_KindFilter_NarrowsResults`.
12. `DiagQueryTool_TargetFilter_NarrowsResults`.
13. `DiagQueryTool_LimitParam_RespectedAndReturnsCursor` — request
    limit=5 of 10 records, get 5 + cursor.
14. `DiagQueryTool_BudgetExceeded_ReturnsTruncatedTrue` — record many
    large-payload entries, verify `truncated=true` response when
    budget is exceeded (overrideable via `budget_kb` parameter).
15. `DiagQueryTool_MetaBlock_PopulatedCorrectly` — verify `meta`
    contains `build_sha`, `turn`, `timestamp_unix_ms`, `session_id`,
    `buffer_fill_pct`, `dropped_records`.

**Counter-check (CLAUDE.md §3.4):**
16. `DiagQueryTool_MissingCategory_ReturnsAllCategories` — verify
    that omitting a filter doesn't accidentally hard-code "effect"
    or some other category (would mean filter logic is broken).

**Implementation:** `[McpForUnityTool("diag_query", ...)]` class with
nested `Parameters`, `HandleCommand(JObject @params)` that:
1. Reads filter params (all optional)
2. Pulls `Diag.Snapshot(5000)` (oversize for filtering)
3. Applies filters in-memory
4. Applies projection (if `fields=[...]` given — defer to post-spike;
   spike returns full records)
5. Applies budget check
6. Wraps in `{ meta, data }` and returns

**Self-review:**
- 🟡 Group="core" for the spike (skip `manage_tools` step). Move
  to "diagnostics" in D3.
- 🟡 `fields` projection deferred to post-spike — note in the tool's
  parameter schema with `Required=false, DefaultValue=null`.
- 🟡 Budget enforcement uses `JsonConvert.SerializeObject(response).Length`
  which is potentially expensive; it's the only way to know
  pre-flight size. Document the cost.

### D1.4 — Live MCP round-trip verification (one commit, ~10 LOC test/script changes)

**File:** docs only (this plan's verification section) + a small
helper script.

**Manual verification steps documented in the plan; executed live:**
1. `refresh_unity` (force) to compile new code.
2. Inspect `read_console` for compile errors.
3. Confirm the new tool appears in the MCP server's tools list:
   `tools/list` should contain `diag_query`. **(Investigates R4 — if
   not visible, document the manual `ReregisterToolsAsync` trigger.)**
4. Call `/tmp/mcp-call.sh diag_query '{"category":"effect"}'`.
5. Expect `{success: true, data: {meta: ..., data: [], truncated: false}}`.
   Empty data array is fine (no effects fired yet in current session).

**Hook verification on real gameplay:**
6. `manage_editor` play.
7. Launch `TrapFurnitureShowcase` scenario.
8. Walk player onto bear trap.
9. Wait one turn (effect ticks fire).
10. `manage_editor` stop.
11. Call `/tmp/mcp-call.sh diag_query '{"category":"effect","kind":"OnRemove"}'`.
12. **Expected:** at least 1 record with target = player. Could be
    Stunned (Duration→0 cleanup), Bleeding (save passed cleanup), or
    something else. ANY record proves the spike works.

If any step fails, that's the architectural problem to address.

### D1.5 — Performance benchmark (one commit, ~50 LOC)

**File:** `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagPerfTest.cs` (new).

Per CLAUDE.md "profile before optimizing":

**Benchmark:**
17. `Diag_DisabledChannelOverhead_LessThan10NsPerCall` — 100k calls
    with channel off, measure ProfilerRecorder, divide. Expect
    < 10 ns/call.
18. `Diag_EnabledChannelOverhead_LessThan10MicrosecondsPerCall` —
    100k calls with payload, channel on. Expect < 10 µs/call.
19. `RemoveEffect_With_Diag_Hook_NoMoreThan5PercentRegression` — apply
    + remove an effect 1000 times with hook on, vs hook disabled.
    Expect < 5% delta.

**If benchmarks fail:** gate the hook behind `Diag.IsChannelEnabled`
without recording, OR move the channel default to off (and document
in `AI-OBSERVABILITY.md` §10 step 2 that `effect` is off-default).

### D1.6 — Self-review + commit per sub-milestone

Each of D1.1-D1.5 is its own commit per CLAUDE.md §2.3 template:

```
diag(spike): D1.X — <one-line summary>

<2-3 sentence what the commit ships>

IMPLEMENTATION NOTES (risks verified before writing code)
  R1: [how it was mitigated]
  R2: [how it was mitigated]
  ...

D1 SELF-REVIEW (Methodology Template §5)
  🟡 ...
  🔵 ...

Files:
- NEW/MOD <path>: <one-line purpose>

Tests: <N> -> <M> (+D). All green.

Co-Authored-By: ...
```

After D1.5 lands, **D1 spike is complete.** The next decision is whether
to proceed with D2-D5 immediately or pause and review.

---

## 6. Performance section (per CLAUDE.md §"Performance — non-negotiables")

Triggers analysis: **does the diag hook affect a per-frame or per-turn path?**

- ❓ **Plumbs `ZoneRenderHooks`?** No.
- ✅ **Allocates collections inside per-frame / per-turn methods?** Yes,
  potentially: `Diag.Record(payload: new { ... })` allocates the
  anonymous object and calls `JsonConvert.SerializeObject`.
- ❓ **Adds a new cache?** No (the ring buffer is bounded but not
  a cache).
- ❓ **Adds a new MonoBehaviour with `Update`/`LateUpdate`?** No.
- ❓ **Adds a new event listener that fires per-frame / per-turn?** Yes:
  `RemoveEffectAt` is per-effect-removal, ~5-50× per turn in combat.

Two of the triggers fire ⇒ this section is required.

### Profile before, profile after

D1.5 does both:
- Benchmark BEFORE: 1000 effect-apply-remove cycles with `Diag.cs`
  shipped but `effect` channel disabled. Establishes baseline.
- Benchmark AFTER: same loop, channel enabled. Compare. Acceptance:
  < 5% wall-clock regression.

### Patterns followed

- **Cache miss is cheap:** N/A (no cache).
- **No allocations in hot paths:** the `Diag.Record` payload param
  IS an allocation. Mitigated by gating recording on
  `IsChannelEnabled` (early return before the param is accessed,
  if possible — depends on whether C# evaluates `new { ... }`
  before the method body executes).

  **Important:** because C# evaluates arguments before the method
  call, the anonymous object is allocated regardless of channel
  state. Mitigation: **callers pass payloads as `Func<object>`
  for hot-path hooks**, OR **use the channel check at the call site
  guard** (`if (Diag.IsChannelEnabled("effect")) Diag.Record(...)`)
  — this is the cheaper option.

  D1.2 uses the call-site guard pattern explicitly. Documented in
  `Diag.cs` API docs and §3 Layer 5 anti-patterns.

- **Per-cell dirty hooks:** N/A.
- **Renderers gate on snapshot fingerprint:** N/A.
- **`Application.runInBackground = true`:** unrelated.

### `Application.runInBackground` check

Diag does not regress this; rate of recording is still bounded by
turn rate.

---

## 7. Pre-flagged self-review findings (severity-marked, per CLAUDE.md §5)

Things I expect to flag at commit time. Listing now so they're not
"surprises."

- **🟡 1. Tool group=core for spike (anti-pattern from §3 P9).**
  The spike defaults `DiagQueryTool` to `Group="core"` so I don't
  have to call `manage_tools` before testing. This violates P9 (off-
  by-default for chatty categories) only if `diag_query` itself is
  chatty — it's not (one call returns ≤ N records). But it does
  put the tool in the always-on tool list, eating one slot. Move
  to `Group="diagnostics"` in D3 when more diag tools exist.

- **🟡 2. Single-tool spike doesn't validate `manage_tools` enablement
  flow.** Per AI-OBSERVABILITY.md §9 Step 0 finding #7, the
  preflight is `manage_tools enable group=diagnostics`. Spike skips
  this; D3 picks it up.

- **🟡 3. No `using (Diag.WithCause(...))` ambient cause threading
  — stub only.** §3 Layer 0 specifies the API; spike implements as
  no-op. D2 wires it up when Entity.FireEvent hook needs to thread
  causes from event-dispatch into per-Part records.

- **🟡 4. JSON projection (`fields=[...]`) deferred to post-spike.**
  Less important than core query/filter; spike returns full records.
  D3 adds projection.

- **🔵 5. Buffer size hardcoded at 1024.** Doc says "circular Record[]
  of 1024." Configurable knob is overkill for spike. If buffer fills
  too quickly in real use, increase as a one-line config.

- **🔵 6. Domain-reload state loss not mitigated by spike.** R3
  documented; D5 persistence fixes it. Investigators must finish
  their session without editing C# files mid-investigation.

- **🔵 7. Spike does not cover Tier-1 §4 file-table count promise
  (~970 LOC across 13 files).** Spike is ~553 LOC across 4 files.
  The remaining LOC ships in D2-D5. Doc claim is for the FULL Tier 1
  ship, not the spike specifically.

- **🔵 8. `fields` projection in `Parameters` schema declared but not
  read in `HandleCommand`.** Acceptable: the parameter is documented
  for forward-compat; D3 implements the read.

- **⚪ 9. `meta.build_sha` requires reading git HEAD at runtime.**
  Use `Application.version` or a build-time stamp, OR shell out to
  `git rev-parse HEAD` once on `Diag` static init. Latter is fine
  for editor-only builds. Document tradeoff.

---

## 8. Open questions for user

These are NOT blocked by the plan; they're checkpoints for user
preference / approval:

1. **D1.1 through D1.5 commit cadence:** five commits in a row, OR
   batch into 2-3 larger commits? Recommendation: five (each is its
   own RED→GREEN cycle and small enough to revert cleanly).

2. **Benchmark gate (D1.5):** if benchmarks show > 5% regression on
   `RemoveEffect`, do I (a) gate the hook with explicit call-site
   `IsChannelEnabled` check, OR (b) move `effect` channel default
   to off, OR (c) ship as-is and accept the regression as a known
   cost? Recommendation: (a). Cheapest, preserves on-default UX.

3. **Verification gate (D1.4):** the bear-trap walk verification is
   manual — there's no automated EditMode test that drives the trap
   showcase end-to-end. Acceptable, OR worth adding a PlayMode test?
   Recommendation: manual for spike, automated PlayMode test in
   D5 finishing.

4. **Branch strategy:** one branch `feat/diag-spike` for all 5
   commits, merged to main as one merge commit? OR five separate
   branches each merged separately? Recommendation: one branch,
   five commits, one merge.

If the user has no objections, default recommendations apply.

---

## 9. Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | e580cf8 |
| User reviews plan | ✅ | "go" — defaults applied |
| D1.1 substrate | ✅ | a5f02c9 — 7/7 GREEN |
| D1.2 hook | ✅ | 685b1f2 — 8/8 GREEN, zero regressions in 93-test sweep |
| D1.3 query tool | ✅ | 588f96c — 16/16 GREEN, 175/175 wider sweep |
| D1.4 live MCP round-trip | ✅ | filters + budget + meta block all verified live; SuccessResponse envelope fix landed inline |
| D1.5 perf benchmark | ⏳ | profile before / after |
| D1.6 self-review + commits | ⏳ | per CLAUDE.md §2.3 |
| Spike acceptance | ⏳ | gates 1, 2, 3 all pass |

### D1.4 live verification log

Performed against the running editor on `feat/diag-spike` after `588f96c`:

| Scenario | Expected | Observed | Result |
|---|---|---|---|
| `tools/list` includes `diag_query` as first-class | ✅ | ❌ — only via `execute_custom_tool` wrapper | 🟡 Finding 4 |
| `execute_custom_tool` with `tool_name="diag_query"` | response | ✅ | ✅ |
| Inner response shape `{meta, data, truncated}` | meta block present | meta initially **dropped** by Python normalizer (it pulled my inner `data` field straight up to envelope's `data`, discarding sibling keys) | 🔴 → fixed |
| Fix: wrap return in `SuccessResponse(null, data: payload)` | meta + data + truncated all reachable as `envelope.data.{meta,data,truncated}` | ✅ | ✅ |
| 5 filter scenarios (no-filter, category=event, category=custom_cat, kind, limit=2) | each narrows correctly with sensible `meta.{returned_count, total_scanned}` | ✅ all 5 | ✅ |
| Budget exceeded (50 records × 2KB payload, default 100KB budget) | `truncated=true`, `would_be_size_bytes`, hint, `data=null` | ✅ — 107KB payload, truncated, hint says "exceeded 100KB budget" with override instructions | ✅ |
| Budget override (`budget_kb=500`) | 50 records returned, `truncated=false` | ✅ | ✅ |

**🔴 → fixed inline:** the FastMCP Python service's `_normalize_response` (`Server/src/services/custom_tool_service.py:267-278`) extracts `response["data"]` directly when present, dropping sibling keys. Before fix: the LLM saw only the records array, no meta block, no truncated flag. After fix (wrap in `SuccessResponse`): the whole `{meta, data, truncated}` payload nests inside the envelope's `data`, preserving every field.

**🟡 Finding 4** — `diag_query` is not auto-promoted to a first-class FastMCP tool despite `[McpForUnityTool]` registration; access is via `execute_custom_tool {tool_name: "diag_query"}`. The DiagQueryTool docstring claimed first-class status; that was overoptimistic for this build of MCPForUnity. Track for D3+ post-spike (the bash command in CLAUDE.md & this doc need updating; for now the wrapper path works fine).
