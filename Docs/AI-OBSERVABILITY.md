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

**How cause IDs are threaded** (this is a real engineering question
the doc previously hand-waved):

- **Explicit** — preferred. The hook caller passes the prior
  `trace_id` to `Diag.Record(..., cause: priorTraceId)`. Works when
  the cause is in the same call frame. E.g., a `Damage` hook can
  reference the `Event/TakeDamage` trace because both fire inside
  the same `CombatSystem.ApplyDamage` call.
- **Stack-style "current trace"** — fallback. The substrate exposes
  a `using (Diag.WithCause(traceId)) { ... }` scope that sets a
  thread-local "ambient cause"; nested `Diag.Record` calls without
  an explicit `cause` parameter pick it up. Used at coarse boundaries
  like `Entity.FireEvent` where many Parts dispatch and we want their
  records to thread back to the event without each Part needing
  bookkeeping. Single-threaded gameplay → no async hazards.
- **None** — acceptable. If neither explicit nor ambient cause is
  available, `cause = null` is fine. Causal queries fall back to
  timestamp ordering + entity correlation. Not as precise but not broken.

**Anti-pattern:** do not store recent trace IDs in static fields
("last apply ID"). They go stale across save/load and across
domain reloads. Use the `using` scope or explicit parameters.

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

The substrate is **system-agnostic.** Combat is the first consumer in
Tier 1, but every field below is generic. Adding a new system (save,
quest, worldgen, dialogue, ui, inventory) needs zero substrate changes
— just a new category string.

```csharp
public static class Diag
{
    public struct Record
    {
        public string TraceId;          // UUID
        public string Category;         // free-form: "event"|"effect"|"damage"|"turn"|
                                        //            "save"|"quest"|"worldgen"|"ui"|...
        public string Kind;             // free-form: "EndTurn"|"OnApply"|"WriteEntity"|
                                        //            "ObjectiveCompleted"|"NodeEntered"|...
        public long   TimestampUnixMs;  // wall-clock; always populated
        public int?   Turn;             // nullable: TurnManager.TickCount when in turn,
                                        //          null for events outside the turn loop
                                        //          (worldgen, save/load, bootstrap, UI)
        public string ActorId;          // optional: Entity.ID, "system", or null
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

**Important field semantics:**

- **`Category`** is intentionally a free-form string, not an enum. Adding
  observability for a new system means choosing a category name and
  recording — no enum change, no recompile of the substrate.
- **`Turn` is nullable.** Many real bugs happen outside the turn loop:
  - Worldgen runs before any turn fires (`Turn = null`).
  - Save/load is wall-clock I/O (`Turn = null`).
  - Bootstrap and blueprint-load are pre-game (`Turn = null`).
  - UI menu interactions are between turns (`Turn = null`).
  Only events fired during `TurnManager.CurrentActor != null` carry a
  turn number. Query tools fall back to `TimestampUnixMs` ordering when
  `Turn` is null.
- **`ActorId` and `TargetId` are both optional.** System-level events
  (e.g., `category="save", kind="WriteEntity"`) may use `actor=null`
  and put the relevant identity in `payload`.
- **`PayloadJson` is eager.** The `payload` parameter passed to
  `Diag.Record(...)` is **JSON-serialized synchronously inside the
  Record call**. The substrate stores only the resulting string. This
  matters because:
  - Live entity references in the payload object don't go stale (the
    snapshot is what's stored).
  - There is a per-Record CPU cost for serialization (~1-5 µs per
    typical record). Quantified in P9 and validated during D1.
  - Calling `Diag.Record(..., payload: someEntity)` is fine — the
    serializer captures the entity's name/ID/HP/etc. at Record time.
  - **Anti-pattern:** never store the live `payload` object in a
    captured closure expecting lazy serialization. The substrate
    won't honor that.

**Storage:** circular `Record[]` of 1024 entries (~250 KB at typical
payload size). On overflow, oldest 512 spill to disk; the in-memory
window covers roughly the last 50 turns of normal play.

**Threading:** gameplay is single-threaded; no locks needed. `Snapshot`
and `Flush` are read-only over the buffer slice.

**Cost when disabled:** one bool check per Record call. ~5 ns.

### Layer 1 — Hook points

Targeted `Diag.Record(...)` insertions at well-chosen call sites. Each
hook is ~3-5 lines.

The hooks split into **three taxonomies of leverage**, listed in
descending order of "coverage you get for one hook":

- **Meta-foundational hooks** capture activity from many systems
  *without those systems knowing*. They sit at architectural choke
  points (event dispatch, turn boundaries) where every consumer
  routes. One hook → dozens of systems observed for free.
- **System-foundational hooks** capture all activity in one
  *foundational system that many features depend on*. Movement is the
  example: the system itself is one body of code, but player input,
  NPC AI, pet retrieval, scribe-flee, ambush triggers, and trap
  stepping all drive through it.
- **Per-system specific hooks** capture one system's internals.
  Effects, damage, AI goals, save/load. Combat is the first batch
  shipped in Tier 1.

You get the first two categories nearly for free in Tier 1 D2; you
opt into per-system hooks as each system's bugs become a recurring
debugging tax.

#### Meta-foundational hooks (Tier 1 — many systems observed for free)

Most Caves of Ooo systems route through `GameEvent` and the
`TurnManager` lifecycle. Hooking these two call sites captures an
enormous amount of cross-system activity without any of those systems
needing per-system hooks.

| Location | What it captures | Category | Why meta |
|---|---|---|---|
| `Entity.FireEvent`, `FireEventAndRelease` | event ID, target, which Parts handled it (true/false) | `event` | Effects, AI, movement, ignition, witnessing, calm, ambush, conversations, save lifecycle hooks — many use GameEvent. **Hook this once → coverage of dozens of systems for free.** |
| `TurnManager.EndTurn`, `BeginTakeAction`, `ProcessUntilPlayerTurn` | turn-boundary markers; CurrentActor; energy state | `turn` | The canonical time axis for any turn-driven analysis (combat, AI, effects, movement). |

#### System-foundational hooks (Tier 1 — one foundational system, many consumers)

A foundational system that's in the path of many features. Hooking it
gives **deep visibility into one system that has many consumers**, not
the same shape of leverage as meta-foundational hooks but still high.

| Location | What it captures | Category | Consumers |
|---|---|---|---|
| `MovementSystem.TryMove` | from/to, blocking entity, success/blocked-by-what | `event` | Player input, NPC AI, pet retrieval, scribe-flee, ambush triggers, trap stepping |

**Critical design point:** these hooks **do not call other hooks**. A
`Diag.Record` call must not produce a recursive cascade. Verified by
code review at hook insertion time.

#### Combat-specific hooks (Tier 1 — first deeply-observed system)

These finish the deep observability story for the combat system, which
is where this substrate's first major user demand came from (the
bear-trap bleeding bug).

| Location | What it captures | Category |
|---|---|---|
| `StatusEffectsPart.ApplyEffectInternal` | effect.OnApply, JustApplied capture, source actor, cause trace | `effect` |
| `StatusEffectsPart.HandleEndTurn` (skip path) | each effect: skipped (JustApplied) vs ticked, Duration before/after | `effect` |
| `StatusEffectsPart.RemoveEffectAt` | effect.OnRemove, LastRemovalCause, killer trace | `effect` |
| `Effect.OnTurnStart` per-effect (Burning, Acidic, Bleeding, Electrified) | damage rolled, post-resistance, target HP delta | `effect` |
| `CombatSystem.ApplyDamage` | full pipeline trace: input, resistance stages, armor, final HP delta | `damage` |
| `CombatSystem.HandleDeath` | killed entity, killer, last damage source | `event` |

#### Per-system hooks (added when each system becomes a debugging cost)

These are **examples**, not Tier 1 deliverables. Each row is the
shape of a future ship that adds a new system to the observability
surface. The substrate doesn't need to change; only `Diag.Record`
calls and (optionally) specialized query tools.

| Future system | Hook idea | Category |
|---|---|---|
| Save/load | `SaveSystem.Write*` and `Read*` per-entity, per-part | `save` |
| Quests | quest-state transitions, objective completion, reward grants | `quest` |
| Worldgen | per-builder placement decisions, RNG seed inputs, output counts | `worldgen` |
| Dialogue | dialogue-node entered, choice picked, branch taken | `dialogue` |
| AI goals | goal pushed/popped on each entity's goal stack with reason | `ai` |
| Inventory | pickup/drop, equipment binding to body parts, weight checks | `inventory` |
| Faction reputation | feeling delta with reason (witnessed kill, dialogue choice, etc.) | `faction` |
| UI flow | screen activated/dismissed, modal stack depth, focus shift | `ui` |
| Material/thermal | which reactions matched, fired vs skipped, heat propagation steps | `material` |

The pattern is the same every time. See **§11 — Extending observability
to a new system** for the full recipe with a worked example.

### Layer 2 — Query tools (custom MCP)

Registered via `[McpForUnityTool]` attribute on Editor-side static
classes. Auto-discovered at WebSocket-connect time by
`MCPForUnity.Editor.Tools.CommandRegistry`. The substrate plus the
filter helper live runtime-side (`Assets/Scripts/Shared/Utilities/
Diag.cs`, `DiagQuery.cs`); only the wrapper that adds the meta block
+ budget enforcement lives Editor-side
(`Assets/Editor/Diagnostics/DiagQueryTool.cs`).

**Calling convention** (post-D1.4 live verification — supersedes
earlier doc claim of first-class FastMCP tools):

```bash
# Direct first-class is NOT auto-registered with FastMCP today.
# Use the execute_custom_tool wrapper:
/tmp/mcp-call.sh execute_custom_tool '{"tool_name":"diag_query","parameters":{"category":"effect"}}'
```

The wrapper path adds one extra envelope but is otherwise identical.
Promotion to first-class is a D3 follow-up if/when the right
registration trigger is found in MCPForUnity.

**Response envelope** — every tool response gets wrapped by FastMCP's
`MCPResponse` schema:

```json
{
  "success": true,
  "message": null,
  "error": null,
  "data": { ...inner payload below... },
  "hint": null
}
```

The Layer 2 contract describes the **inner payload** at
`response.data.*`. Tool implementations return
`new SuccessResponse(null, data: payload)` to nest correctly — a
naked anonymous return with a `data` field would be flattened by
the Python normalizer (`custom_tool_service._normalize_response`),
discarding sibling keys like `meta` and `truncated`. Caught + fixed
during D1.4; see `Docs/D1-SPIKE-PLAN.md` §9.

#### Generic tools

| Tool | Purpose | Returns | Shipped |
|---|---|---|---|
| `diag_query` | Filter ring buffer (with `since_turn`/`until_turn` window) | `{ meta, data: [records], truncated, would_be_size_bytes? }` | ✅ D1.3, +D3.1 turn-window |
| `diag_count` | Aggregation: how many records match a filter | `{ count, total_scanned, sample_first_trace_id, sample_first_kind, tool_version }` | ✅ D2.5, +D3.1 turn-window |
| `diag_assert` | Predicate: at least one record matches? | `{ matched, count, sample_first_trace_id, sample_first_kind, tool_version }` | ✅ D3.2 |
| `diag_inspect_record` | One record + its causal neighbors (ancestors via CauseTraceId, descendants via buffer scan) | `{ record, caused_by, caused, tool_version }` | ✅ D3.3 |
| `diag_causal_chain` | (Subsumed by `diag_inspect_record`'s `caused_by` array) | — | ❌ folded into D3.3 |
| `diag_set_channels` | Toggle category recording at runtime | `{ channels: { name: bool, ... } }` | ⏳ D4 |

**Common parameters (all query tools):**
- `category`, `kind`, `actor`, `target` — filters (string equality;
  `kind` accepts arrays for OR matching)
- `since_turn`, `until_turn` — turn-window filter. Records with
  `Turn = null` (worldgen, save, bootstrap, UI) are **excluded** from
  any query that uses these parameters. Use `since_unix_ms` /
  `until_unix_ms` instead to include null-turn records.
- `since_unix_ms`, `until_unix_ms` — wall-clock time window (always
  applicable since `TimestampUnixMs` is always populated).
- `fields=["TraceId","Kind","Turn","..."]` — projection (omits everything else)
- `limit` (default 50, max 500), `cursor` — pagination

**Response-size budget enforcement** (operationalizing P2):
Every query tool checks the size of the JSON it would return BEFORE
returning it. If the would-be response exceeds **100 KB** (~25k
tokens), the tool refuses and returns this inner payload (wrapped
by the FastMCP envelope, so visible to the LLM at `response.data.*`):

```json
{
  "meta": { ... },
  "data": null,
  "truncated": true,
  "would_be_size_bytes": 248000,
  "hint": "Response exceeded 100KB budget. Use cursor + smaller limit, narrow filters (since_turn / kind), use fields= to project, or pass budget_kb=500 to override (max 1000)."
}
```

Override via optional `budget_kb` parameter on any query tool (default
100, max 1000). The substrate truncates rather than streams — partial
responses with stale data are worse than refusal.

**Example calls — combat (bear-trap bleeding deferred bug):**

(After D3, `diag_query` (with `since_turn`/`until_turn`),
`diag_count`, `diag_assert`, and `diag_inspect_record` ship.
`diag_set_channels` is D4. Hooks shipped: `effect/OnApply`,
`effect/OnRemove`, `damage/DamageDealt`, `turn/Begin`, `turn/End`.
Note the `execute_custom_tool` wrapper described above.)

```bash
# Was the bleeding effect ever removed during the apply turn?
/tmp/mcp-call.sh execute_custom_tool '{"tool_name":"diag_query","parameters":{"category":"effect","kind":"OnRemove","target":"player","limit":50}}'
# → response.data.data is an array of OnRemove records (filtered)
# → look for one with PayloadJson matching {"effect":"BleedingEffect", ...}
#   to confirm the bug pattern
```

Once D2 ships (`diag_assert`, `diag_causal_chain`, `diag_inspect_record`)
the same investigation collapses into three calls — predicate
match, walk causes backward, inspect root.

**Example calls — save/load drift (hypothetical, after `save` category ships):**

```bash
# Did the player's effects survive the save/load round-trip?
/tmp/mcp-call.sh diag_query '{"category":"save","actor":"player","fields":["Kind","PayloadJson"]}'
# → [
#     { Kind: "WriteEntity", PayloadJson: '{"effects":[{"name":"Stoneskin","duration":3}]}' },
#     { Kind: "RestoreEntity", PayloadJson: '{"effects":[{"name":"Stoneskin","duration":5}]}' }
#                                                                              ^^^ drift
#   ]
```

**Example calls — quest desync (hypothetical, after `quest` category ships):**

```bash
# Did the UI refresh fire after the objective completion?
/tmp/mcp-call.sh diag_assert '{"category":"quest","kind":"UIRefreshed","since_unix_ms":<after_completion>}'
# → { matched: false, count: 0 }   ← UI refresh missing; that's the bug
```

**Example calls — AI goal-stack thrash (hypothetical, after `ai` category ships):**

```bash
# Last 20 goal mutations on this NPC, with reasons
/tmp/mcp-call.sh diag_query '{"category":"ai","target":"pet_dog","kind":["GoalPushed","GoalPopped"],"limit":20,"fields":["Kind","PayloadJson","Turn"]}'
# → ordered list showing thrash pattern, e.g. push Fetch / pop Fetch / push Wander / pop Wander loop
```

Same query tools, same shape, completely different system. The
substrate doesn't know or care.

#### Specialized tools

These are **examples of a pattern**, not foundational. Each is a
convenience wrapper that pre-bakes a common multi-step query for one
system. Add one when the same generic-tool query becomes a recurring
debugging pattern. Future per-system specialized tools (e.g.
`diag_save_pipeline`, `diag_quest_state`, `diag_goal_stack`,
`diag_dialogue_path`) follow the same structure.

| Tool | Scope | Purpose |
|---|---|---|
| `diag_inspect_entity` | **Universal (general state)** | Live state dump of the entity itself: parts, stats (with modifier sources), effects (with all internal fields), tags, properties, plus last N records that name this entity |
| `diag_diff_entity` | **Universal** | Field-level diff between turn N and turn M for one entity |
| `diag_damage_history` | Combat (example) | Pre-aggregated damage records — input, each pipeline stage, final delta — across a window |
| `diag_effect_lifecycle` | Combat (example) | All records for one effect type or instance: apply → ticks → remove |

**`diag_inspect_entity` scope and the "relation" parameter:**

`diag_inspect_entity` returns **the entity's general state** —
runtime data structures (parts/stats/effects/tags/properties) plus
recent records that mention this entity. It does NOT and should NOT
return system-specific summaries (e.g., "active quest objectives,"
"serializer cursor position"). System-specific views are queries via
`diag_query` with a category filter, OR specialized tools per system
(e.g. `diag_save_pipeline`, `diag_quest_state`).

Including system-specific data in `diag_inspect_entity` would make
its response size unbounded and tie the universal tool to every
system's internals. **Stays general; system-specific data is a query.**

For the "last N records that name this entity" part of the response,
the `relation` parameter narrows which Records count as "naming" the
entity:

| `relation` | Includes records where |
|---|---|
| `"either"` (default) | `ActorId == entityId` OR `TargetId == entityId` |
| `"actor"` | `ActorId == entityId` only |
| `"target"` | `TargetId == entityId` only |
| `"payload"` | `PayloadJson` mentions the entity ID (string match — slow; use sparingly) |

A future Tier-2 extension API (`Diag.RegisterEntityInspector(category, ...)`)
could let systems contribute to `diag_inspect_entity`'s output. **Out
of scope for Tier 1.** Until then, system-specific entity views are
their own specialized tools or `diag_query` calls.

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
- **❌ Don't pass mutating-payload objects expecting lazy serialization.**
  `Diag.Record(...)` JSON-encodes synchronously. If you build a payload,
  modify it after the call, then expect the recorded version to reflect
  the modification — that's a bug in your hook, not the substrate.
  (See §3 Layer 0 "PayloadJson is eager.")
- **❌ Don't over-stamp.** Resist the urge to record every tick of every
  per-frame system. The ring buffer is finite; flooding it loses signal.

---

## 4. Tier 1 — first ship

Tier 1 is **the system-agnostic substrate plus the universal hooks plus
combat-as-the-first-deeply-observed-system.** Combat is the proving
ground, but the substrate, the universal hooks, and 8 of the 10 MCP
tools are general-purpose — they serve every future system at the same
quality level.

Success criteria (both must hold):

1. **The bear-trap bleeding deferred bug becomes solvable in 3 MCP calls.** This validates the combat-system depth.
2. **Adding observability for ANY new system requires only `Diag.Record` calls + optionally specialized tools.** No substrate changes, no schema changes. This validates the architecture is genuinely general — verified by walking through the §11 extension recipe end-to-end.

### Files to create

| Path | Purpose | Scope | Approx LOC |
|---|---|---|---|
| `Assets/Scripts/Diagnostics/Diag.cs` | static recording API + ring buffer | **Universal** | 200 |
| `Assets/Scripts/Diagnostics/DiagSerializer.cs` | payload serialization helpers (JSON, entity-state snapshotter) | **Universal** | 150 |
| `Assets/Scripts/Diagnostics/DiagPersistence.cs` | spill to disk + session load | **Universal** | 100 |
| `Assets/Editor/Diagnostics/DiagQueryTool.cs` | filter + projection | **Universal** | 80 |
| `Assets/Editor/Diagnostics/DiagAssertTool.cs` | predicate match | **Universal** | 50 |
| `Assets/Editor/Diagnostics/DiagCountTool.cs` | aggregation | **Universal** | 40 |
| `Assets/Editor/Diagnostics/DiagInspectEntityTool.cs` | full entity state dump | **Universal** | 100 |
| `Assets/Editor/Diagnostics/DiagCausalChainTool.cs` | forward/backward walk on `CauseTraceId` | **Universal** | 80 |
| `Assets/Editor/Diagnostics/DiagBufferStatusTool.cs` | buffer meta info | **Universal** | 30 |
| `Assets/Editor/Diagnostics/DiagChannelsTool.cs` | get/set channels (one tool, action: "get"\|"set"\|"enable"\|"disable") | **Universal** | 50 |
| `Assets/Editor/Diagnostics/DiagFlushTool.cs` | force spill | **Universal** | 25 |
| `Assets/Editor/Diagnostics/DiagListSessionsTool.cs` | list disk-spilled past sessions | **Universal** | 35 |
| `Assets/Editor/Diagnostics/DiagLoadSessionTool.cs` | load past session jsonl | **Universal** | 50 |

**Total new code:** ~970 LOC summed (450 substrate + 520 thin tool
wrappers averaging ~50 LOC each with parameter classes + XML doc
comments). **Every file is universal — none are combat-specific.**
Combat appears only in the hook insertions.

The two **example specialized tools** (`diag_damage_history`,
`diag_effect_lifecycle`) referenced in §3 are deferred to a small
follow-up commit AFTER Tier 1 lands. They demonstrate the per-system
specialized-tool pattern; they are NOT foundational. The substrate +
universal tools are sufficient to solve the bear-trap bleeding bug
without them.

### Hooks to add (Tier 1)

The first 3 hooks are **universal** (cover many systems) and the rest
are **combat-specific** (the first deeply-observed system).

| File | Hook | Scope | LOC |
|---|---|---|---|
| `Entity.FireEvent` (and `FireEventAndRelease`) | `event` record before dispatch | **Universal** | +5 |
| `TurnManager.EndTurn`, `BeginTakeAction`, `ProcessUntilPlayerTurn` | `turn` records | **Universal** | +4 |
| `MovementSystem.TryMove` | from/to + blocking entity | **Universal** | +3 |
| `StatusEffectsPart.ApplyEffectInternal` | `effect`/`OnApply` | Combat (first system) | +4 |
| `StatusEffectsPart.HandleEndTurn` | `effect`/skipped or ticked | Combat (first system) | +6 |
| `StatusEffectsPart.RemoveEffectAt` | `effect`/`OnRemove` | Combat (first system) | +3 |
| `CombatSystem.ApplyDamage` (typed overload) | `damage`/`ApplyDamage` w/ pipeline stages | Combat (first system) | +10 |

**Total hook code:** ~35 LOC across 6 files. **3 universal hooks + 4
combat hooks.** Future system ships add their own hooks in their own
files; the substrate stays untouched.

### Sub-milestones

Following CLAUDE.md major-feature workflow:

1. **D1 — Diag substrate (one commit).** `Diag.cs` + `DiagSerializer.cs`.
   No hooks yet. Tests for ring buffer overflow, channel filtering,
   serialization round-trip, **`Turn=null` records (worldgen-shape)**,
   and **arbitrary new categories** (verify `Diag.Record("save", ...)`
   works without substrate changes — the generality smoke test).
2. **D2 — Universal hooks (one commit).** `Entity.FireEvent`,
   `TurnManager` boundaries, `MovementSystem.TryMove`. Tests that
   each hook produces the expected record. **Includes ≥ 1 test on a
   non-combat code path** (e.g., menu navigation if it uses GameEvent,
   or a worldgen builder firing an event) to prove universal coverage
   isn't theoretical.
3. **D3 — Combat-specific hooks + persistence (one commit).** Effect
   lifecycle hooks + `CombatSystem.ApplyDamage` + `DiagPersistence.cs`.
   Manual trap-showcase playtest produces ≥ 50 records.
4. **D4 — Generic MCP tools (one commit).** `diag_query`, `diag_count`,
   `diag_assert`, `diag_inspect_entity`, `diag_causal_chain`,
   `diag_buffer_status`, `diag_set_channels`, `diag_flush`. End-to-end
   integration test: launch trap showcase, run `diag_assert` for
   "BurningEffect was applied", expect `matched: true`. **Plus a
   non-combat assert** (e.g., "TurnManager fired BeginTakeAction at
   least once") to validate generic tools work outside combat.
5. **D5 — Persistence MCP tools + Tier-1 finishing (one commit).**
   `diag_list_sessions`, `diag_load_session`. End-to-end: run trap
   showcase twice, list sessions, diff between them. **Solve the
   bear-trap bleeding bug** as the closing verification.

Each commits standalone, RED→GREEN per CLAUDE.md §2.1, full self-review.

### Verification (both criteria must pass)

**Combat-depth criterion (Tier-1 testable):** with Tier 1 shipped, I should be able to:

1. Run `TrapFurnitureShowcase` to the bear-trap step.
2. Issue 3 MCP calls (per the combat example in §3 above).
3. Get a definitive answer about where `BleedingEffect` is being
   removed (or confirm it isn't).

**Substrate-genericity criterion (Tier-1 testable):** D1 ships with an
EditMode test that exercises the substrate via the public API only,
with **zero combat dependencies and a never-before-used category
string**. The test:

```csharp
[Test]
public void Diag_AcceptsArbitraryNewCategoryWithoutCodeChanges()
{
    Diag.SetChannel("smoke_test_category", true);
    Diag.Record("smoke_test_category", "TestKind",
        actor: null, target: null,
        payload: new { foo = "bar", count = 7 });

    var query = new DiagQueryRequest { Category = "smoke_test_category" };
    var result = DiagQueryTool.Execute(query);

    Assert.AreEqual(1, result.Records.Count);
    Assert.AreEqual("TestKind", result.Records[0].Kind);
    Assert.IsTrue(result.Records[0].PayloadJson.Contains("foo"));
}

[Test]
public void Diag_AcceptsNullTurn_ForOutOfTurnEvents()
{
    // No TurnManager active; Turn should record as null.
    Diag.Record("worldgen", "TestPlace", payload: new { x = 5, y = 8 });
    var records = Diag.Snapshot(10);
    Assert.IsNull(records.Last(r => r.Category == "worldgen").Turn);
}
```

If those tests pass, the substrate is provably general at the API
level. The architecture commits to "no substrate edits when adding a
new system."

**Forward commitment (verifiable on the next non-combat ship, NOT
during Tier 1):** when the first non-combat system gains observability
post-Tier-1, it must follow §10's recipe verbatim. If implementing it
requires substrate, schema, or Tier-1-tool changes, **that's a bug
filed against §10 (the recipe), not the system.** This is an
architectural promise the doc takes on, not a Tier-1 acceptance gate
— but it's how we'll know the generality is real, not paper.

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

- **Snapshot-diff anomaly detector** — auto-flag *any* unexpected
  state change: an entity field mutated without a corresponding cause
  record in the same window. Generic across systems (HP without
  damage, quest objective state without ObjectiveCompleted record,
  inventory weight without pickup/drop, faction relationship without
  reputation event). Useful but high false-positive rate; would need
  per-category whitelisting of "expected silent mutations."
- **What-if queries** — given a record, recompute the downstream
  effect under a hypothetical input change ("what would damage be if
  HR were 25?"). Generic in principle (rerun the pipeline trace with
  a mutated input) but each system needs an isolated re-execution
  harness. Niche.
- **Full event-sourced replay** — record every input + RNG, replay
  to produce byte-identical state. Mostly redundant once
  `diag_replay_scenario` (Tier 2) exists, since scenarios already
  provide deterministic setup.

---

## 7. Things this doc deliberately does NOT cover

- **Performance observability.** Already covered by
  `Docs/PERF-FOUNDATION.md`. Don't duplicate. (The diag substrate
  itself MUST satisfy the perf rules in that doc — that's a build
  constraint, not duplication.)
- **A player-facing diagnostic UI.** This substrate is queried via
  MCP from external sessions; we don't add a Unity Editor inspector
  window or in-game overlay for it. (Note: this is **not** a
  statement about whether the UI **system** can be observed — that's
  one of the future `category="ui"` consumers in §3 Layer 1's
  per-system table.)
- **Build / CI observability.** Different audience (humans + CI bots).
- **Release-build / shipped-game observability.** Tier 1 + Tier 2
  assume Unity Editor + the MCP server are running. Players running
  a release build (post-Steam-release scenarios in
  `Docs/CONTENT-ROADMAP.md` Tier 5) have no MCP query surface. The
  substrate's `DiagPersistence` jsonl spill could in principle run in
  release builds (it's plain file I/O), giving us **post-mortem
  forensics** on bug reports — but a live query channel from a
  shipped player to a debugger requires a separate transport (HTTP
  endpoint, log-upload, etc.) that's not designed for here. Listed
  as out-of-scope rather than impossible: revisit if release-build
  bug volume justifies it.

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

## 10. Extending observability to a new system

This is the contract for adding a new system to the diag surface.
Combat is the first user; everything below applies identically to
save/load, quest, worldgen, dialogue, AI, inventory, faction, UI,
material/thermal, settlement, crafting, or any future system.

**The substrate never changes.** Adding a new system is purely
additive: new `Diag.Record` calls, optionally new specialized tools.

### The 5-step recipe

#### Step 1 — Pick a category name

Free-form lowercase string with a few naming rules:

- **≤ 20 characters** (informal limit; query verbosity grows with name length).
- **Single lowercase word** preferred (`save`, `quest`, `dialogue`).
  Use `snake_case` for multi-word (`material_sim`, not `materialsim`
  or `material-sim`).
- **System-scoped** (the system's domain), not action-scoped.
  ✅ `quest` (covers all quest-related events).
  ❌ `objective_completed` (kind, not category).
- **Singular**, not plural. (`quest`, not `quests`.)
- **Match existing prefixes if possible.** If the system already has
  a `Debug.Log` prefix or namespace, reuse the lowercase form for
  consistency: `[Combat]` → `combat`, `[Bootstrap]` → `bootstrap`,
  `MaterialSim` namespace → `material_sim`.
- **Avoid collisions with existing categories.** Run `grep
  'Diag\.Record(' Assets/Scripts/` first to confirm yours is new.

Examples of good category names:
`event`, `effect`, `damage`, `turn`, `save`, `quest`, `worldgen`,
`dialogue`, `ui`, `inventory`, `faction`, `material`, `settlement`,
`crafting`, `ai`, `bootstrap`.

#### Step 2 — Decide on/off-by-default and on volume

- **On by default** if the system is sparse (a few records per turn at
  most). E.g., `save`, `quest`, `dialogue` — fired on player choice
  or system events.
- **Off by default** if the system is **chatty but bounded** (10s to
  100s of records per turn typical). E.g., `material` (per-tile
  thermal), `ai` (per-NPC goal-stack thrash). Flip on with
  `diag_set_channels` only when investigating.
- **Aggregate-first** if the system is **firehose-rate** (>100 events
  per turn typical, or per-frame in extreme cases). Don't record
  individual events even off-default — see the §10 Step 3
  "high-frequency escape hatch" below. Examples: per-tile FOV
  recompute, per-pixel lighting, per-frame UI input polling.

If unsure: off-default. Verify with profiling on the D1 success
criterion (≤ 5 ns when disabled, ~1-5 µs per Record call when enabled).

#### Step 3 — Add `Diag.Record` calls at the system's key points

The right places to record are the **decision points and state
transitions** — not every method call. A useful heuristic: if you'd
add a `Debug.Log` here when debugging, you'd add `Diag.Record` here.

```csharp
// Inside the system's source file, at a state transition:
Diag.Record(
    category: "save",
    kind: "WriteEntity",
    actor: null,                    // system-level event, no actor
    target: entity,                 // the entity being serialized
    payload: new {                  // anonymous object → JSON, eagerly serialized
        bytesWritten = 1234,
        partCount = entity.Parts.Count,
        effectCount = entity.GetPart<StatusEffectsPart>()?.EffectCount ?? 0
    }
);
```

The hook **must not throw or block**. Recording is fail-soft (the
substrate wraps every call in try/catch). Records are written; system
proceeds.

##### High-frequency escape hatch (Step 3a — for firehose-rate systems)

If your system fires **> 100 events per turn typical** (per-tile
thermal sim, per-frame UI polling, per-NPC AI scan in a populated
zone), recording every event will blow the 1024-entry ring buffer
in seconds and lose signal among noise. Don't do that.

Instead, build an **in-system aggregator** and only `Diag.Record`
summaries at natural boundaries:

```csharp
// In the firehose-rate system:
private static int _reactionsFired = 0;
private static int _reactionsSkipped = 0;
private static Dictionary<string, int> _byReactionId = new();

public static void EvaluateReactions(...)
{
    foreach (var reaction in candidates)
    {
        bool fired = TryFire(reaction);
        if (fired) {
            _reactionsFired++;
            _byReactionId[reaction.Id] = _byReactionId.GetValueOrDefault(reaction.Id) + 1;
        } else {
            _reactionsSkipped++;
        }
        // NOTE: NOT calling Diag.Record per reaction.
    }
}

// Called once per turn from the natural turn boundary
// (e.g., MaterialSimSystem.TickEnd):
public static void RecordTurnSummary()
{
    if (_reactionsFired == 0 && _reactionsSkipped == 0) return;
    Diag.Record("material", "TurnSummary",
        payload: new {
            fired = _reactionsFired,
            skipped = _reactionsSkipped,
            top_reactions = _byReactionId
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        });
    _reactionsFired = 0;
    _reactionsSkipped = 0;
    _byReactionId.Clear();
}
```

When deeper detail IS needed, gate per-event recording behind a
**second, finer-grained channel**:

```csharp
if (Diag.IsChannelEnabled("material_verbose"))
{
    Diag.Record("material_verbose", "ReactionMatched",
        target: target,
        payload: new { reactionId, sourceState, ... });
}
```

The investigator opts into the verbose channel only for the bug
they're chasing. Default state: only summaries flow.

**Why this matters:** without an aggregation tier, "off-by-default"
isn't enough — turning the channel on swamps the buffer; turning it
off loses the system entirely. The aggregator gives you "always-on
summaries + opt-in verbose" instead of binary on/off.

#### Step 4 — (Optional) Add specialized tools when a query becomes recurring

If you find yourself running the same multi-step generic-tool query
repeatedly for one system, that's a signal to wrap it. The
specialized-tool template:

```csharp
[McpForUnityTool("diag_save_pipeline",
    Description = "Show the full save/load pipeline trace for one save operation.",
    Group = "diagnostics")]
public static class DiagSavePipelineTool
{
    public class Parameters
    {
        [ToolParameter("Save filename or ID")]
        public string save_id { get; set; }
    }

    public static object HandleCommand(JObject @params)
    {
        string saveId = @params["save_id"]?.ToString();
        // ... pre-baked query against `category="save"` records, formatted
        // for the common save/load debugging flow ...
        return new { meta = ..., pipeline = ... };
    }
}
```

Specialized tools are **conveniences**, not new architecture. Skip
this step until the lack hurts.

### Worked example — adding `category="save"` (hypothetical, illustrative)

This is the full sequence to add observability for the save/load
system. **Touches zero substrate files.**

> **Hedge:** the actual `SaveSystem` method names below are sketched
> from a quick read; a real implementation should `grep` `Assets/Scripts/Gameplay/Save/SaveSystem.cs`
> to find the correct entry points. The recipe is method-name-agnostic;
> what matters is hooking at the natural state-transition boundaries.

#### File 1: hook insertion (purely additive)

```csharp
// Assets/Scripts/Gameplay/Save/SaveSystem.cs
using CavesOfOoo.Diagnostics;

public bool WriteEntityToSave(Entity entity, SaveWriter writer)
{
    int bytesBefore = writer.Position;
    bool ok = WriteEntityCore(entity, writer);
    Diag.Record("save", ok ? "WriteEntity" : "WriteEntityFailed",
        target: entity,
        payload: new {
            bytes = writer.Position - bytesBefore,
            partCount = entity.Parts.Count,
            effectCount = entity.GetPart<StatusEffectsPart>()?.EffectCount ?? 0,
            success = ok
        });
    return ok;
}

public bool RestoreEntityFromSave(Entity entity, SaveReader reader)
{
    bool ok = RestoreEntityCore(entity, reader);
    Diag.Record("save", ok ? "RestoreEntity" : "RestoreEntityFailed",
        target: entity,
        payload: new {
            partCount = entity.Parts.Count,
            effectCount = entity.GetPart<StatusEffectsPart>()?.EffectCount ?? 0,
            success = ok
        });
    return ok;
}
```

That's the entire production change. Two `Diag.Record` calls. No
substrate edits, no new MCP tool files, no schema changes.

#### Sample query (from any future Claude session)

A user reports "after save/load, my Stoneskin's Duration is wrong."

```bash
# Enable the save channel for this session
/tmp/mcp-call.sh diag_set_channels '{"enable":["save"]}'

# Reproduce: save and reload
# (player action — diag captures automatically)

# Inspect both halves of the round-trip
/tmp/mcp-call.sh diag_query '{"category":"save","actor":"player","fields":["Kind","PayloadJson"]}'
# → [
#     { Kind: "WriteEntity", PayloadJson: '{"effectCount":1, ...}' },
#     { Kind: "RestoreEntity", PayloadJson: '{"effectCount":1, ...}' }
#   ]

# Use diag_inspect_entity to get pre-save and post-load state
/tmp/mcp-call.sh diag_inspect_entity '{"entity":"player","at_trace_id":"<write_id>"}'
/tmp/mcp-call.sh diag_inspect_entity '{"entity":"player","at_trace_id":"<restore_id>"}'
# → diff the two; pinpoint the field that drifted
```

No new tool was needed — `diag_query` and `diag_inspect_entity` are
universal. The whole "save observability" investment was 8 lines of
production code.

#### Optional Step 4 — when does it pay to specialize?

If save bugs become a **monthly recurring debugging cost**, build
`diag_save_pipeline` as a convenience that pre-bakes the
WriteEntity → RestoreEntity diff query. Until then, the generic tools
do the job.

### Generalization audit checklist

Before claiming a system is "observable," verify:

- [ ] Records use a unique `category` string consistently (not
  scattered as `"save"` in some places and `"saveSystem"` elsewhere).
- [ ] System-level events (no actor or target) leave those fields
  null and put the relevant identity in `payload`.
- [ ] Records that fire outside the turn loop (worldgen, save,
  bootstrap, UI menu) leave `Turn = null` so they don't fake a
  zero turn that pollutes time-window queries.
- [ ] Records carry `CauseTraceId` when they're a downstream effect
  of another record (e.g., `category="save", kind="RestoreEntity"`
  could carry the trace ID of the corresponding `WriteEntity` if
  the save format includes it).
- [ ] At least one positive query and one counter-check query are
  documented in the system's commit message.
- [ ] The on/off-by-default decision is explicit and justified.

---

## 11. Critique log — what's resolved, and what this third pass still doesn't catch

This section is the rolling honesty ledger. Each pass through the doc
adds a row showing what got fixed and what new issues surfaced.

### Resolution status of the §11 second-pass items (all 11)

After the third pass through the doc, every flagged issue is either
addressed inline or has a deliberate "deferred with rationale"
disposition.

| # | Issue (from second pass) | Status | Where addressed |
|---|---|---|---|
| 🟡 1 | Generality criterion not Tier-1 testable | ✅ Reframed | §4 — split into Combat-depth criterion (Tier-1 testable) + Substrate-genericity criterion (Tier-1 testable via D1 substrate-only smoke test) + Forward commitment (verifiable on next non-combat ship, NOT a Tier-1 gate) |
| 🟡 2 | Hook taxonomy fuzzy ("Universal" overloaded) | ✅ Refined | §3 Layer 1 — split into Meta-foundational (`Entity.FireEvent`, `TurnManager`) vs System-foundational (`MovementSystem.TryMove`) vs Per-system specific |
| 🟡 3 | Category naming convention not pinned | ✅ Pinned | §10 Step 1 — ≤ 20 chars, lowercase, snake_case multi-word, system-scoped not action-scoped, singular, match Debug.Log prefix when one exists, grep for collisions |
| 🟡 4 | High-frequency-system escape hatch missing | ✅ Added | §10 Step 3a — three-tier volume bucketing (sparse/chatty/firehose), worked example with in-system aggregator that records summaries at turn boundaries + opt-in `_verbose` channel for deep dives |
| 🟡 5 | Specialized tools in Tier-1 LOC budget (already removed in second pass) | ✅ N/A | Already addressed in second pass; struck from the list |
| 🟡 6 | `diag_inspect_entity` may need extension API | ✅ Clarified scope | §3 Layer 2 — explicitly limited to general entity state (parts/stats/effects/tags). System-specific data via `diag_query` with category filter, OR specialized tools. Tier-2 `RegisterEntityInspector` API documented but deferred. |
| 🟡 7 | Payload-object lifetime / serialization timing | ✅ Pinned eager | §3 Layer 0 — `PayloadJson` is JSON-serialized synchronously inside `Diag.Record`. Cost ~1-5 µs/call. Plus anti-pattern entry: don't expect lazy serialization |
| 🟡 8 | `inspect_entity` "targeting" semantics ambiguous | ✅ Defined | §3 Layer 2 — new `relation` parameter: `"either"` (default), `"actor"`, `"target"`, `"payload"`. Default is `ActorId == entityId OR TargetId == entityId` |
| 🔵 9 | §6/§7 read combat-flavored | ✅ Generalized | §6 anomaly detector now system-agnostic (any field change without cause record). §7 distinguishes "we won't observe X" vs "we won't add UI for X." |
| 🔵 10 | No cross-process / release-build story | ✅ Documented as deferred | §7 — explicit acknowledgment that release-build observability is out of scope. `DiagPersistence` jsonl spill could enable post-mortem forensics; live query channel for shipped players needs separate transport. Revisit if release-build bug volume justifies it. |
| 🔵 11 | No response-size budget enforcement | ✅ Specified | §3 Layer 2 — every query tool checks would-be JSON size; refuses >100KB responses with `{ truncated: true, hint: ... }`. Override via `budget_kb` param (default 100, max 1000) |

**Net: 11/11 second-pass issues addressed.** Doc grew 1186 → ~1500
lines.

### What this third pass introduced (new issues to track)

Honest gaps the third pass introduced or didn't catch. These are the
seed for a hypothetical fourth pass; **none block Step 1 (the spike).**

**🟡 12. The `using (Diag.WithCause(traceId))` scope (P4) is specified but not implemented.**

P4 names three cause-ID threading mechanisms (explicit param, ambient
`using` scope, null fallback). The `using` mechanism would need a
`[ThreadStatic]` field plus an `IDisposable` returning struct. That's
cheap (~30 LOC), but it's an unflagged Tier-1 deliverable hiding
inside §3 Layer 0's API. Add to the §4 file table (probably folded
into `Diag.cs`) before D1.

**🟡 13. The "next-system gate" forward commitment (§4) is policy, not enforcement.**

§4 commits: "if the next non-combat system requires substrate or tool
changes, that's a recipe bug." Good intent, but if I'm in a future
session pressed for time, I might rationalize substrate edits as
"small fixes." Counter-measure: when a non-combat system PR proposes
substrate edits, the PR description must explicitly cite which §10
recipe step is wrong and propose the recipe revision.

This is a process rule, not a code rule — flag for the next CLAUDE.md
revision rather than this doc.

**🟡 14. The high-frequency aggregator pattern (§10 Step 3a) has no test scaffolding.**

The §10 Step 3a worked example shows a static aggregator with
`Dictionary<string, int>` accumulators. But it doesn't address:
- How to TEST the aggregator (mock turn boundary, accumulate, verify summary).
- What happens on domain reload (statics get reset; partial-turn accumulation lost).
- How to handle multi-zone scenes (per-zone aggregators? global?).

Acceptable for now (firehose-rate observability isn't on the Tier 1
critical path), but future-Claude implementing the second
firehose-rate consumer should expand §10 Step 3a with a concrete
testable shape.

**🟡 15. Anti-pattern list (§3 Layer 5) doesn't have an example for "categorize too narrowly."**

The category naming convention in §10 Step 1 says "system-scoped not
action-scoped." But the anti-pattern list doesn't reinforce. A
category like `objective_completed` (action) instead of `quest`
(system) splinters queries — every kind needs its own diag_query
call. Worth one sentence in §3 Layer 5.

**🔵 16. The `relation` parameter on `diag_inspect_entity` (fix for #8) doesn't say what `"payload"` actually does for nested matches.**

I wrote: `"payload" — PayloadJson mentions the entity ID (string match — slow; use sparingly)`. But: does `"player"` substring-match against `"player_corpse"`? Probably not what the user wants. Either a regex-bounded match (`\bplayer\b`?) or accept the imprecision and document it.

Cosmetic; refine when first user hits it.

**🔵 17. Persistence format (`~/Library/Logs/CavesOfOoo/diag-{branch}-{timestamp}.jsonl`) still not specified.**

§4 D5 references it but no schema. Per-line JSON object? Newline-delimited records? File header with metadata? Rotation policy when size exceeds N MB? This becomes a real D5 design question; flagging now so I don't pretend it's pinned.

**🔵 18. The doc is now 1500 lines.**

P2 says "default response budget ≤ 5 KB." The doc itself, when served
to a future Claude session as instructions, is ~50 KB just for the
markdown. Token cost on each session is real. Worth a structural
look post-spike: can §1 (self-critique of the FIRST proposal),
§9 (Step 0 findings), and possibly the long §11 critique log be
moved to a sibling `AI-OBSERVABILITY-HISTORY.md` so future-Claude
can read the contract without the archaeology?

### What this third pass deliberately did NOT do

- I did **not** verify the example aggregator code (§10 Step 3a) compiles. It's pseudocode-grade.
- I did **not** specify the persistence file format (#17 above).
- I did **not** implement the `using` cause scope (#12 above).
- I did **not** profile the eager-payload-serialization cost on a typical record (~1-5 µs is an estimate, validated during D1).

### Recommended posture for Step 1 (the spike)

The doc is now defensible as the contract for D1-D5. **No third-pass
items block the spike.** Items 12-18 are post-spike refinements.

If I'm building Step 1 next, the order is unchanged from the previous
recommendation:

1. Read `MCPForUnity/CustomTools/RoslynRuntimeCompilation/ManageRuntimeCompilation.cs` once more for the `[McpForUnityTool]` boilerplate I'll be copying.
2. Build `Diag.cs` substrate (no hooks). Include the `using (Diag.WithCause(...))` scope from item #12.
3. Add the `Diag_AcceptsArbitraryNewCategoryWithoutCodeChanges` and `Diag_AcceptsNullTurn_ForOutOfTurnEvents` tests from §4. Confirm both pass.
4. Add ONE hook (`StatusEffectsPart.RemoveEffectAt`).
5. Build `DiagQueryTool.cs` with budget enforcement (item #11/#fix).
6. Verify: walk into bear trap, run `diag_query category=effect kind=OnRemove`, see the captured record.

If that round-trip works, the architecture is real.

---

## 12. Document maintenance

When the substrate ships:

- Each hook insertion adds one row to a "live hooks" table in this doc.
- When a deferred bug is solved using diag tools, link the `Docs/KNOWN-ISSUES/`
  doc to the diag-session jsonl that captured the trace.
- New MCP tools added in Tier 2/3 update the tool table in §3.
- New systems gaining observability (per §10 recipe) add a row to a
  "live categories" table that lists category, on/off default, hook
  files, and any specialized tools.

This doc is the contract for what I expect from the substrate. Code drifting
from it is a bug; doc drifting from code is a different bug. Both fixed in
the same commit as the drift.
