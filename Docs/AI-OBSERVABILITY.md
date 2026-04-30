# AI-Observability — diagnostic tooling for Claude debugging the gameplay

> **Audience:** Claude (an AI coding agent debugging this game via the Unity
> MCP server). Not human dashboards, not a Unity Inspector overlay.
>
> Every design choice in this doc optimizes for what an AI does poorly with the
> existing tooling — not what a human does poorly. The two are different.

---

## 0. Why this is its own document

This game already has good observability for humans:

- `MessageLog` shows player-facing combat lines.
- `Debug.Log` lines surface in Unity console, filterable by `[Combat]` /
  `[Bootstrap]` / etc.
- `PerformanceMarkers` + `ProfilerRecorder` capture CPU spikes.
- `Docs/MCP_PlayMode_Testing_Strategy.md` rules govern live-state observation.

But none of those tools are designed for an AI that:

1. **Wakes up in a fresh session** with no memory of yesterday's bug.
2. **Pays per token** for every byte returned. A 50KB log dump costs more than
   a 200-byte structured answer.
3. **Cannot see UI.** Sidebar overlays, Unity Inspector windows, screenshots
   are nearly useless. JSON over an HTTP RPC is the medium.
4. **Reasons in predicates.** "Did event X happen between turn 10 and 11?" is
   a one-bit answer; reading 500 log lines to find it is wasteful.
5. **Cannot easily join across calls.** If `diag_query` returns a list of
   events and I want to drill into one, I need stable IDs to fetch related
   data — not "go look at the screen."

This doc designs a separate substrate that complements (not replaces) the
human-facing tools.

---

## 1. Self-critique of the first proposal

The original menu (delivered in the chat where the user asked "are there
observability systems we can set up") had three tiers. Reviewing it now:

### What was right

- Identifying **custom MCP tools** as the delivery channel. Per-session tool
  registration is the only way future-Claude inherits this without rebuilding
  it from memory.
- Tiering by **build size** so the first ship is shippable in hours.
- Calling out **`GameplayTrace` ring buffer + `EffectLifecycleLog`** as the
  obvious starting point — the bear-trap bleeding deferred bug would collapse
  to a single JSON call.

### What was wrong or under-specified

| Critique | Why it matters | Fix |
|---|---|---|
| Framed deliverables like human dashboards ("surface via sidebar overlay") | I cannot see UI. Every byte must be JSON-over-HTTP. | Strike all UI deliverables. Custom MCP tools only. |
| Under-weighted token cost | Returning 500 events × 200B/event = 100KB. Not free. | Default response budget ≤ 5KB. Pagination + aggregation + field selection are first-class. |
| No causal IDs | I can't drill from a summary into a detail without identifiers. | Every observable carries a `trace_id`. Cross-tool joins by ID. |
| No persistence across sessions | My context dies when the session ends. | Spill ring buffer to `~/Library/Logs/CavesOfOoo/diag-*.jsonl`. List + load via tool. |
| No predicate / aggregation tools | The most common AI question is "did X happen?" — yes/no. | First-class `diag_assert`, `diag_count`, `diag_causal_chain` tools. |
| "UI inspector overlay" listed as Tier 3 | Wrong audience. Wasted slot. | Drop entirely. |
| No replay primitive | The bear-trap bleeding bug needs a deterministic re-run, not just current-state reads. | Tier 2: `diag_replay_scenario(seed, input)` that captures the full diag stream and returns it. |
| No mention of self-observability | When my own tools fail, how do I know? | Every response carries a `meta` block: build SHA, turn, dropped count. |
| Conflated "log channel" with "diag layer" | Channels are debug-print toggles. Diag is structured + queryable. Different tools. | Separate them. Keep both. |

### What was missing entirely

1. **Field selection.** Many queries only need 1-2 fields per record (e.g.,
   "Duration of every BleedingEffect tick" — not the whole effect object).
   First-class `fields=[...]` parameter on every query tool.
2. **Diff queries.** "What changed on the player between turn N and N+1?" is
   the most common debugging question. Cheaper to compute server-side than to
   diff two full snapshots client-side.
3. **Test-failure attachment.** When an EditMode test fails, the diag stream
   from that test run should be inspectable via tool. Currently a test failure
   gives me the assertion message — and that's it.
4. **Causal chains.** Given a "BurningEffect removed at turn N" record, I want
   to walk backwards: what events ran during turn N's EndTurn? Which Part
   handled them? Which one called `RemoveEffect`? — all in one call.
5. **What-if queries.** Less critical, but: "if the dice roll were 3 instead
   of 2 here, what would the post-resistance damage be?" Useful for tuning
   investigations.

---

## 2. Design principles

These are the load-bearing rules. Everything below derives from them.

### P1 — JSON-first, not log-first

Every observable lives as structured data with named fields. Human-readable
text is at most a sibling field (`message: "you takes 1 fire damage."`),
never the primary representation.

### P2 — Token-cheap by default

- Default response budget: **≤ 5 KB** (~1k tokens).
- All list-returning tools support `limit` (default 50), `cursor`, and
  `fields=[...]`.
- Aggregation tools (`diag_count`, `diag_assert`) return a number or a
  bool, not a list.
- Verbose dumps require explicit opt-in (`verbose: true` parameter).

### P3 — Predicates over dumps

- "Did X happen?" → yes/no + matching `trace_ids` for drill-down. Not "here's
  every event, you figure it out."
- "How many of Y in window Z?" → integer.
- "What's the causal chain producing state S?" → ordered list of N records.

### P4 — Causal IDs everywhere

- Every recorded observable has `trace_id` (UUID).
- Damage records reference the source event's `trace_id`.
- Effect lifecycle records reference the apply event's `trace_id`.
- Cross-tool joins work: `diag_inspect_record(trace_id)` returns the original
  record + everything it caused.

### P5 — Read-only by contract

- No diag tool mutates gameplay state. Period.
- The replay tool runs against a forked Zone snapshot; original session
  is undisturbed.
- This is the same rule as `Docs/MCP_PlayMode_Testing_Strategy.md` Rule 1
  (never fire events via `execute_code`). Same rationale: state corruption
  is silent and devastating.

### P6 — Persistent across sessions

- Ring buffer is in-memory but spills to
  `~/Library/Logs/CavesOfOoo/diag-{branch}-{timestamp}.jsonl` on:
  - Buffer fill (oldest 50% spills).
  - Editor exit / scenario teardown.
  - Explicit `diag_flush` tool call.
- A `diag_list_sessions` tool returns timestamped past sessions. Each can be
  loaded by `diag_load_session(path)` for forensic queries.

### P7 — Self-observable

Every tool response includes a `meta` block:

```json
{
  "meta": {
    "build_sha": "2c63ffc",
    "turn": 17,
    "timestamp_unix_ms": 1777590000123,
    "session_id": "5a8b...",
    "buffer_fill_pct": 64,
    "dropped_records": 0,
    "tool_version": "diag/1"
  },
  "data": ...
}
```

So I can always tell: was the buffer overflowing? Is this fresh? Which build?

### P8 — Single hook surface, multi-flavored records

Don't fragment the substrate. Everything goes through `Diag.Record(...)`.
The `category` field distinguishes flavors:

```
category="event"   → kind="EndTurn" target="player" payload=...
category="effect"  → kind="OnApply" effect="BleedingEffect" target="player" payload=...
category="damage"  → kind="ApplyDamage" target="glowmaw" stages=[...]
category="turn"    → kind="BeginTakeAction" actor="player"
```

One ring buffer, one query surface, multiple semantic layers above it.

### P9 — Off-by-default for chatty categories, on for the rest

- `event`, `effect`, `damage`, `turn` — ON by default.
- `material` (per-tick reaction evaluations), `ai` (per-NPC goal-stack
  thrash) — OFF by default. Flip via `diag_set_channels`.
- This keeps the ring buffer useful in non-investigation runs without losing
  the ability to investigate.

---

## 3. Architecture

### Layer 0 — Recording substrate

**`Assets/Scripts/Diagnostics/Diag.cs`** — single static class.

```csharp
public static class Diag
{
    public struct Record
    {
        public string TraceId;          // UUID
        public string Category;         // "event" | "effect" | "damage" | "turn" | "material" | "ai"
        public string Kind;             // "EndTurn" | "OnApply" | "ApplyDamage" | ...
        public long   TimestampUnixMs;
        public int    Turn;             // TurnManager.TickCount snapshot
        public string ActorId;          // Entity.ID or "system"
        public string TargetId;         // optional
        public string CauseTraceId;     // optional, links to causing record
        public string PayloadJson;      // category-specific JSON blob
    }

    public static void Record(string category, string kind,
        Entity actor = null, Entity target = null,
        object payload = null, string cause = null);

    public static IReadOnlyList<Record> Snapshot(int limit);
    public static void Flush();         // spill to disk
    public static bool IsChannelEnabled(string category);
    public static void SetChannel(string category, bool enabled);
}
```

**Storage:** circular `Record[]` of 1024 entries (~250 KB at typical payload
size). On overflow, oldest 512 spill to disk; the in-memory window covers
roughly the last 50 turns of normal play.

**Threading:** gameplay is single-threaded; no locks needed. `Snapshot` and
`Flush` are read-only over the buffer slice.

**Cost when disabled:** one bool check per Record call. ~5 ns.

### Layer 1 — Hook points

Targeted insertions, ~3-5 lines each. Goal: capture the events we've
actually needed to debug, not the universe.

| Location | Hook | Category |
|---|---|---|
| `Entity.FireEvent`, `FireEventAndRelease` | event start + which Parts handled it (true/false) | `event` |
| `StatusEffectsPart.ApplyEffectInternal` | effect.OnApply, JustApplied capture, source actor, cause trace | `effect` |
| `StatusEffectsPart.HandleEndTurn` (skip path) | each effect: skipped (JustApplied) vs ticked, Duration before/after | `effect` |
| `StatusEffectsPart.RemoveEffectAt` | effect.OnRemove, LastRemovalCause, killer trace | `effect` |
| `Effect.OnTurnStart` (per-effect) | damage rolled, post-resistance, target HP delta | `effect` |
| `CombatSystem.ApplyDamage` | full pipeline trace: input, resistance stages, final HP delta | `damage` |
| `CombatSystem.HandleDeath` | killed entity, killer, last damage source | `event` |
| `TurnManager.EndTurn`, `BeginTakeAction`, `ProcessUntilPlayerTurn` | turn-boundary markers | `turn` |
| `MovementSystem.TryMove` | from/to, blocking entity if any | `event` |
| `MaterialReactionResolver.EvaluateReactions` | which reactions matched, fired vs skipped | `material` (off-default) |
| `BrainPart`/AIGoal pushes/pops | goal stack changes with reason | `ai` (off-default) |

**Critical design point:** the hooks **do not call other hooks**. A `Diag.Record` call must not produce a recursive cascade. Verified by code review at hook insertion time.

### Layer 2 — Query tools (custom MCP)

Registered via `MCPForUnity/CustomTools/`, callable from `/tmp/mcp-call.sh`
(or from a future Claude session's tool registration).

#### Generic tools

| Tool | Purpose | Returns |
|---|---|---|
| `diag_query` | Filter ring buffer | List of records with optional `fields` projection |
| `diag_count` | Aggregation: how many records match a filter | `{ count: int }` |
| `diag_assert` | Predicate: at least one record matches? | `{ matched: bool, first_trace_id: string \| null, count: int }` |
| `diag_causal_chain` | Walk `CauseTraceId` links forward and backward from a record | Ordered list |
| `diag_inspect_record` | One record + its immediate causes & effects | `{ record, caused_by, caused, related }` |

**Common parameters (all query tools):**
- `category`, `kind`, `actor`, `target` — filters
- `since_turn`, `until_turn`, `since_unix_ms`, `until_unix_ms` — time windows
- `fields=["TraceId","Kind","Turn","..."]` — projection (omits everything else)
- `limit` (default 50, max 500), `cursor`

**Example calls** (the bear-trap bleeding deferred bug):

```bash
# Was the bleeding effect ever removed during the apply turn?
/tmp/mcp-call.sh diag_assert '{"category":"effect","kind":"OnRemove","target":"player","payload_match":{"effect":"BleedingEffect"},"since_turn":10,"until_turn":10}'
# → { matched: true, first_trace_id: "abc...", count: 1 }   ← bug confirmed

# What caused the removal?
/tmp/mcp-call.sh diag_causal_chain '{"trace_id":"abc...","direction":"backward","limit":10}'
# → ordered list ending at the cause

# Inspect that root cause
/tmp/mcp-call.sh diag_inspect_record '{"trace_id":"<root>"}'
# → which Part called RemoveEffect, on what frame, with what message
```

Three calls. Done. Replaces the entire 5-step diagnostic plan in
`Docs/KNOWN-ISSUES/BEAR-TRAP-BLEEDING-EVAPORATES.md`.

#### Specialized tools

| Tool | Purpose |
|---|---|
| `diag_inspect_entity` | Full state dump: parts, stats (with modifier sources), effects (with all internal fields), tags, properties, last N events targeting this entity |
| `diag_diff_entity` | Field-level diff between turn N and turn M for one entity |
| `diag_damage_history` | Pre-aggregated damage records — input, each pipeline stage, final delta — across a window |
| `diag_effect_lifecycle` | All records for one effect type or instance: apply → ticks → remove |

#### Channel control

| Tool | Purpose |
|---|---|
| `diag_set_channels` | Enable/disable per-category recording |
| `diag_get_channels` | Current channel state |
| `diag_buffer_status` | Fill %, oldest record turn, dropped count |
| `diag_flush` | Force spill to disk now |

#### Persistence

| Tool | Purpose |
|---|---|
| `diag_list_sessions` | List `~/Library/Logs/CavesOfOoo/diag-*.jsonl` files with summary metadata |
| `diag_load_session` | Open a past session's jsonl into the in-memory buffer (read-only mode) |
| `diag_session_summary` | High-level stats for current session: events by category, turns elapsed, top entities by event count |

### Layer 3 — Test-integration

EditMode tests already use `MessageLog` for assertions. Extend with diag
capture:

- `using (Diag.TestScope("MyTestName")) { ... }` block in a test wraps a
  scoped subset of records, written to disk on test failure.
- A custom MCP tool `diag_test_failure_context(test_name)` fetches the most
  recent failing-test diag dump.

Doesn't require touching every test. Opt-in. Used when the assertion message
alone is insufficient.

### Layer 4 — Replay primitive (Tier 2)

```bash
/tmp/mcp-call.sh diag_replay_scenario '{
  "scenario": "TrapFurnitureShowcase",
  "rng_seed": 42,
  "input_sequence": ["e","e","e","e","e","wait","wait"],
  "capture_categories": ["event","effect","damage","turn"]
}'
```

Returns: deterministic diag dump for the entire run, plus final entity
states, plus any uncaught exceptions.

**Implementation cost:** medium. Requires:
- RNG threading audit (everything must use the seed-able `System.Random`
  per CLAUDE.md "Determinism audit" cross-cutting note)
- Input event injection path that bypasses real keyboard
- Scenario harness already exists at
  `Assets/Tests/EditMode/TestSupport/ScenarioTestHarness.cs`

**Why it's Tier 2 not Tier 1:** ~1-2 days of work. The deterministic-input
piece needs design (especially with `_combatRng` references scattered).
Can ship the lower tiers first and let those harden before this.

### Layer 5 — Anti-patterns to avoid

- **❌ Don't add a Unity Editor inspector window.** I can't see it.
- **❌ Don't surface diag through `MessageLog`.** Pollutes the player log,
  degrades human UX, doubles the hook cost.
- **❌ Don't return paragraphs of free-form text.** Every field is named and
  typed.
- **❌ Don't make tools that mutate state.** Read-only by contract.
- **❌ Don't fragment the substrate.** One `Diag.Record`, one ring buffer,
  many query views.
- **❌ Don't auto-enable verbose categories.** AI category alone could fill
  the buffer in 100 turns; off-default.
- **❌ Don't make recording fail-loud.** A diag record that throws must NOT
  break gameplay. Wrap every hook in `try { } catch { /* swallow + warn */ }`.
- **❌ Don't store entity object references in records.** Persist only IDs
  and serialized fields. Entity references go stale across save/load.
- **❌ Don't over-stamp.** Resist the urge to record every tick of every
  per-frame system. The ring buffer is finite; flooding it loses signal.

---

## 4. Tier 1 — first ship

Goal: enable solving the bear-trap bleeding deferred bug in one session.

### Files to create

| Path | Purpose | Approx LOC |
|---|---|---|
| `Assets/Scripts/Diagnostics/Diag.cs` | static recording API + ring buffer | 200 |
| `Assets/Scripts/Diagnostics/DiagSerializer.cs` | payload serialization helpers (JSON, entity-state snapshotter) | 150 |
| `Assets/Scripts/Diagnostics/DiagPersistence.cs` | spill to disk + session load | 100 |
| `MCPForUnity/CustomTools/diag_query.py` | MCP tool: filter + project | 80 |
| `MCPForUnity/CustomTools/diag_assert.py` | MCP tool: predicate match | 50 |
| `MCPForUnity/CustomTools/diag_count.py` | MCP tool: aggregation | 40 |
| `MCPForUnity/CustomTools/diag_inspect_entity.py` | MCP tool: full entity state | 100 |
| `MCPForUnity/CustomTools/diag_causal_chain.py` | MCP tool: forward/backward walk | 80 |
| `MCPForUnity/CustomTools/diag_buffer_status.py` | MCP tool: meta info | 30 |
| `MCPForUnity/CustomTools/diag_set_channels.py` | MCP tool: channel toggle | 30 |

**Total new code:** ~860 LOC, mostly mechanical.

### Hooks to add

| File | Hook | LOC |
|---|---|---|
| `Entity.FireEvent` (and `FireEventAndRelease`) | `event` record before dispatch | +5 |
| `StatusEffectsPart.ApplyEffectInternal` | `effect`/`OnApply` record | +4 |
| `StatusEffectsPart.HandleEndTurn` | per-effect `effect`/skipped or `effect`/ticked | +6 |
| `StatusEffectsPart.RemoveEffectAt` | `effect`/`OnRemove` record | +3 |
| `CombatSystem.ApplyDamage` (typed overload) | `damage`/`ApplyDamage` record with stages | +10 |
| `TurnManager.EndTurn`, `BeginTakeAction` | `turn` records | +4 |

**Total hook code:** ~32 LOC across 6 files.

### Sub-milestones

Following CLAUDE.md major-feature workflow:

1. **D1 — Diag substrate (one commit).** `Diag.cs` + `DiagSerializer.cs`.
   No hooks yet. Tests for ring buffer overflow, channel filtering,
   serialization round-trip.
2. **D2 — Hooks + persistence (one commit).** All 6 hook insertions +
   `DiagPersistence.cs`. Tests verify each hook produces the expected
   record. Manual playtest of trap showcase produces ≥ 50 records.
3. **D3 — Generic MCP tools (one commit).** `diag_query`, `diag_count`,
   `diag_assert`, `diag_buffer_status`, `diag_set_channels`. End-to-end
   integration test: launch trap showcase, run `diag_assert` for
   "BurningEffect was applied", expect `matched: true`.
4. **D4 — Specialized MCP tools (one commit).** `diag_inspect_entity`,
   `diag_causal_chain`. Verify the bear-trap bleeding bug can be
   diagnosed with three tool calls.
5. **D5 — Persistence MCP tools (one commit).** `diag_list_sessions`,
   `diag_load_session`. End-to-end: run trap showcase twice, list
   sessions, diff between them.

Each commits standalone, RED→GREEN per CLAUDE.md §2.1, full self-review.

### Verification

The success criterion for Tier 1 is **deferred bug resolution.** When this
ships, I should be able to:

1. Run `TrapFurnitureShowcase` to the bear-trap step.
2. Issue 3 MCP calls (per the example in §2 above).
3. Get a definitive answer about where `BleedingEffect` is being removed (or
   confirm it isn't).

If that workflow doesn't work, Tier 1 isn't done.

---

## 5. Tier 2 — replay + advanced (parked)

After Tier 1 hardens. Adds:

- `diag_replay_scenario` — deterministic re-run from scenario + seed + input
- `diag_diff_entity` — field-level state diff
- `diag_damage_history` — pre-aggregated damage records
- `diag_effect_lifecycle` — full lifecycle for one effect instance
- Test-integration scope (`using (Diag.TestScope(...))`)

Estimated 1-2 days. RNG-determinism audit is the main work; the hooks and
tools are mechanical.

---

## 6. Tier 3 — beyond (parked, possibly never)

- **Snapshot-diff anomaly detector** — auto-flag "HP changed without
  DamageDealt event." Useful but high false-positive rate.
- **What-if queries** — "what would damage be if HR were 25?" Requires
  isolated ApplyResistances test harness. Niche.
- **Full event-sourced replay** — record every input + RNG, replay to
  produce byte-identical state. Mostly redundant once `diag_replay_scenario`
  exists, since scenarios already provide deterministic setup.

---

## 7. Things this doc deliberately does NOT cover

- **Performance observability.** Already covered by
  `Docs/PERF-FOUNDATION.md`. Don't duplicate.
- **Save/load observability.** Save tests already pin shape; not in scope.
- **Build / CI observability.** Different audience (humans + CI bots).
- **Player-facing UI.** This is for me, not them.

---

## 8. Open questions — answered (Step 0 investigation)

| Question | Status | Answer |
|---|---|---|
| Does the MCP custom-tools framework support the parameter shapes I want (filters, projection)? | ✅ resolved | YES. C# `HandleCommand(JObject @params)` accepts arbitrary nested JSON at runtime. Schema declared via optional nested `Parameters` class with `[ToolParameter]` attributes — supports `string`, `integer`, `number`, `boolean`, `array`, `object` types (`ToolDiscoveryService.GetParameterType`). Nested-dict filters like `payload_match: {effect: "BleedingEffect"}` work. |
| Is `Entity.FireEvent` hot-path-critical? Adding a record per call could matter. | ⏳ deferred to D1 verification | Profile during D1 spike (per CLAUDE.md "profile before optimizing"). Recording cost when category-disabled is one bool check + early return — should be ~5ns. Concern is when the category is ENABLED on a per-frame path. |
| Does `Stat` have a setter hook for capturing modifier-source changes? | ⏳ deferred to D3 | Out of scope for first ship; Tier 1 captures effects/damage/turn boundaries which already cover ~90% of the bugs we've actually hit. |
| Where exactly does `_combatRng` live, and is its determinism contract satisfied? | ⏳ Tier 2 only | Not relevant until `diag_replay_scenario` is on the table. Tier 1 doesn't need determinism. |

---

## 9. Step 0 findings — concrete revisions to §3 architecture

Step 0 was a no-code investigation of the `unity-mcp/MCPForUnity/` package
to verify the architecture in §3 was actually buildable. Several
assumptions were wrong; revisions below.

### Revision 1 — No Python files

**Original (§3 / §4):** ship Tier 1 as `MCPForUnity/CustomTools/diag_*.py`
files, one per tool.

**Reality:** custom tools live entirely in **Unity-side C#**. The Python
server has a generic `/register-tools` HTTP endpoint plus an
`execute_custom_tool` dispatcher, both implemented in
`Server/src/services/custom_tool_service.py`. Unity registers tools at
WebSocket-connect time via `WebSocketTransportClient.SendRegisterToolsAsync`
(Transports/WebSocketTransportClient.cs:525-575), and Python forwards
calls back to Unity via `send_with_unity_instance`.

**Implication:** zero Python work. All ~860 LOC of Tier 1 code is C# in
the Caves of Ooo project's own Unity scripts.

### Revision 2 — `[McpForUnityTool]` attribute auto-discovery

**Original:** unspecified registration mechanism.

**Reality:** declared via `[McpForUnityTool("name", Description=...)]` on a
class. `CommandRegistry.AutoDiscoverCommands` (Editor/Tools/CommandRegistry.cs:60+)
scans `AppDomain.CurrentDomain.GetAssemblies()` for the attribute on Unity
domain reload. Discovered tools auto-register with the Python bridge.

Existing example: `unity-mcp/CustomTools/RoslynRuntimeCompilation/ManageRuntimeCompilation.cs`
— ~700 LOC standalone tool, sits outside the unity-mcp package, gets
auto-discovered.

**Implication:** drop a `[McpForUnityTool]` C# class anywhere in the
Unity project's compiled assemblies and it auto-registers. No central
manifest file to update.

### Revision 3 — Tool schema is via a nested `Parameters` class

**Original:** unspecified.

**Reality:** `ToolDiscoveryService.ExtractParameters` (line 153) looks for a
**nested `Parameters` class** with `[ToolParameter]`-decorated properties.
Type mapping in `GetParameterType` (line 189):

```csharp
typeof(string)              → "string"
typeof(int)/typeof(long)    → "integer"
typeof(float)/typeof(double)→ "number"
typeof(bool)                → "boolean"
IsArray | IEnumerable       → "array"
otherwise                   → "object"   ← what we want for nested filter dicts
```

Pattern:

```csharp
[McpForUnityTool("diag_query",
    Description = "Query the diag ring buffer with filters and projection.",
    Group = "diagnostics")]
public static class DiagQueryTool
{
    public class Parameters
    {
        [ToolParameter("Category filter: event, effect, damage, turn, material, ai")]
        public string category { get; set; }

        [ToolParameter("Kind filter (e.g. 'OnApply', 'EndTurn')")]
        public string kind { get; set; }

        [ToolParameter("Target entity ID or name")]
        public string target { get; set; }

        [ToolParameter("Nested payload-field filter, e.g. {\"effect\":\"BleedingEffect\"}")]
        public Dictionary<string, object> payload_match { get; set; }

        [ToolParameter("Field projection: only return these fields per record")]
        public string[] fields { get; set; }

        [ToolParameter("Max records (default 50, max 500)", Required = false, DefaultValue = "50")]
        public int? limit { get; set; }
    }

    public static object HandleCommand(JObject @params)
    {
        // Read nested JSON via JObject — works regardless of schema strictness
        string category = @params["category"]?.ToString();
        var payloadMatch = @params["payload_match"] as JObject;
        string effectFilter = payloadMatch?["effect"]?.ToString();
        // ...
        return new { meta = new {...}, data = filtered };
    }
}
```

The `Parameters` class is documentation for the LLM (gives type hints in
the schema). The actual runtime parsing happens in `HandleCommand`'s
`JObject` traversal. Nested JSON is not constrained by the schema — it
flows through `parameters: dict` on the Python side and reaches Unity as
`JObject @params`.

**Implication:** my `payload_match: {effect: "BleedingEffect"}` design from
§3 is buildable as-is. No flattening required.

### Revision 4 — File layout

**Original (§4):** unspecified split between runtime substrate and
editor-side tool wrappers.

**Reality:**

| Layer | Location | Why |
|---|---|---|
| L0 substrate (`Diag.cs`, ring buffer, record API) | `Assets/Scripts/Diagnostics/` | Runtime-callable from gameplay code (StatusEffectsPart, CombatSystem hooks). Same assembly as `CavesOfOoo.asmdef`. |
| L1 hooks | inline in their target source files | Each hook is `Diag.Record(...)`; needs runtime visibility |
| L2 MCP tool wrappers (`DiagQueryTool.cs` etc.) | `Assets/Editor/Diagnostics/` | `[McpForUnityTool]` lives in `MCPForUnity.Editor.Tools` — editor-only namespace. Tools must be in an editor-side assembly. They call into the runtime `Diag` API for data. |

Split is clean: **substrate is runtime, query surface is editor.** This
matches the existing pattern (e.g., `Assets/Editor/Scenarios/ScenarioMenuItems.cs`
calls into runtime `ScenarioRunner`).

### Revision 5 — Group everything under `Group = "diagnostics"`

**Reality:** `[McpForUnityTool]` has a `Group` attribute (defaults to
`"core"`). Tools in non-core groups are hidden by default and enabled
per-session via `manage_tools` meta-tool. The MCP server's instructions
already reference this dynamic visibility model.

**Implication:** stamp every diag tool with `Group = "diagnostics"`. They
won't crowd the default tool list; we enable them when investigating.

### Revision 6 — Calling pattern from `/tmp/mcp-call.sh`

**Reality:** auto-registered tools become first-class MCP tools (line
360 of `custom_tool_service.py`: `self._mcp.tool(name=definition.name, ...)(wrapped)`).
They appear in `tools/list` and can be called directly — no
`execute_custom_tool` wrapper needed.

```bash
# After the substrate ships:
/tmp/mcp-call.sh diag_query '{"category":"effect","kind":"OnRemove","target":"player","payload_match":{"effect":"BleedingEffect"},"limit":5}'
```

Direct call. Same shape as `read_console`, `run_tests`, etc.

### Revision 7 — `manage_tools` enables our group

Before the first session that wants diag tools, one preflight call:

```bash
/tmp/mcp-call.sh manage_tools '{"action":"enable","groups":["diagnostics"]}'
```

(Confirmed: `manage_tools` is in the existing 43-tool list per Step 0 ToolSearch.)

### Revisions to Tier 1 D1-D5 sub-milestones

The D1-D5 ordering in §4 still holds. Concrete file deltas:

| Original §4 path | Revised path |
|---|---|
| `Assets/Scripts/Diagnostics/Diag.cs` | unchanged ✅ |
| `Assets/Scripts/Diagnostics/DiagSerializer.cs` | unchanged ✅ |
| `Assets/Scripts/Diagnostics/DiagPersistence.cs` | unchanged ✅ |
| `MCPForUnity/CustomTools/diag_query.py` (and all `*.py` siblings) | DELETED — wrong target; instead → |
| (new) | `Assets/Editor/Diagnostics/DiagQueryTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagAssertTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagCountTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagInspectEntityTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagCausalChainTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagBufferStatusTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagSetChannelsTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagFlushTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagListSessionsTool.cs` |
| (new) | `Assets/Editor/Diagnostics/DiagLoadSessionTool.cs` |

**Net LOC estimate revised down:** ~860 → ~700, because the C# tool
classes are thinner than equivalent Python wrappers (no marshalling
boilerplate, no separate registration code).

### Step 0 verdict

**The architecture in §3 is buildable as-is, with the file-layout
revisions above.** No design changes needed. All assumptions about
parameter shapes, nested filters, projection, and direct MCP calling
are confirmed by reading the actual unity-mcp source.

The first-ship spike (Step 1 in the chat-side recommendation) is unblocked
and can target:

- `Assets/Scripts/Diagnostics/Diag.cs` (substrate, no hooks yet)
- ONE hook in `StatusEffectsPart.RemoveEffectAt` (highest leverage for
  the bear-trap deferred bug)
- ONE tool: `Assets/Editor/Diagnostics/DiagQueryTool.cs`
- One verification: walk onto bear trap, run `/tmp/mcp-call.sh diag_query
  '{"category":"effect","kind":"OnRemove","limit":10}'`, see the captured
  remove record

If that round-trips successfully, all of Tier 1 is mechanical execution
of the same pattern.

---

## 10. Document maintenance

When the substrate ships:

- Each hook insertion adds one row to a "live hooks" table in this doc.
- When a deferred bug is solved using diag tools, link the `Docs/KNOWN-ISSUES/`
  doc to the diag-session jsonl that captured the trace.
- New MCP tools added in Tier 2/3 update the tool table in §3.

This doc is the contract for what I expect from the substrate. Code drifting
from it is a bug; doc drifting from code is a different bug. Both fixed in
the same commit as the drift.
