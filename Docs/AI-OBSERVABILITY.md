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

**Storage:** circular `Record[]` of 1024 entries (~250 KB at typical
payload size). On overflow, oldest 512 spill to disk; the in-memory
window covers roughly the last 50 turns of normal play.

**Threading:** gameplay is single-threaded; no locks needed. `Snapshot`
and `Flush` are read-only over the buffer slice.

**Cost when disabled:** one bool check per Record call. ~5 ns.

### Layer 1 — Hook points

Targeted `Diag.Record(...)` insertions at well-chosen call sites. Each
hook is ~3-5 lines.

The hooks split into three categories. **Universal hooks** capture
broad activity for free; **per-system hooks** are added when each
system gets observed; **combat hooks** are the first batch (Tier 1
ships these). The substrate gives you universal coverage immediately,
and you opt into deeper per-system hooks as their bugs become a
recurring tax.

#### Universal hooks (Tier 1 — broad coverage of MANY systems for free)

Hooking these few call sites captures an enormous amount of activity
across all subsystems, because most Caves of Ooo systems route
through `GameEvent` and the turn loop.

| Location | What it captures | Category | Why universal |
|---|---|---|---|
| `Entity.FireEvent`, `FireEventAndRelease` | event ID, target, which Parts handled it (true/false) | `event` | Effects, AI, movement, ignition, witnessing, calm, ambush, conversations, save lifecycle hooks — many use GameEvent. **Hook this once → coverage of dozens of systems for free.** |
| `TurnManager.EndTurn`, `BeginTakeAction`, `ProcessUntilPlayerTurn` | turn-boundary markers; CurrentActor; energy state | `turn` | The canonical time axis for any turn-driven analysis (combat, AI, effects, movement). |
| `MovementSystem.TryMove` | from/to, blocking entity, success/blocked-by-what | `event` | Used by player input, NPC AI, pet retrieval, scribe-flee, ambush triggers, trap stepping. |

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

**Example calls — combat (bear-trap bleeding deferred bug):**

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
| `diag_inspect_entity` | **Universal** | Full state dump: parts, stats (with modifier sources), effects (with all internal fields), tags, properties, last N events targeting this entity |
| `diag_diff_entity` | **Universal** | Field-level diff between turn N and turn M for one entity |
| `diag_damage_history` | Combat (example) | Pre-aggregated damage records — input, each pipeline stage, final delta — across a window |
| `diag_effect_lifecycle` | Combat (example) | All records for one effect type or instance: apply → ticks → remove |

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

**Combat-depth criterion:** with Tier 1 shipped, I should be able to:

1. Run `TrapFurnitureShowcase` to the bear-trap step.
2. Issue 3 MCP calls (per the combat example in §3 above).
3. Get a definitive answer about where `BleedingEffect` is being
   removed (or confirm it isn't).

**Generality criterion:** I should be able to walk through §11's
extension recipe and produce a working `category="save"` observability
addition (in mock form, ~30 min) **without modifying any Tier 1 file**.
If §11 instructions don't actually work end-to-end, the generality is
illusory and the Tier 1 ship needs revision.

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

## 10. Extending observability to a new system

This is the contract for adding a new system to the diag surface.
Combat is the first user; everything below applies identically to
save/load, quest, worldgen, dialogue, AI, inventory, faction, UI,
material/thermal, settlement, crafting, or any future system.

**The substrate never changes.** Adding a new system is purely
additive: new `Diag.Record` calls, optionally new specialized tools.

### The 4-step recipe

#### Step 1 — Pick a category name

Free-form lowercase string. Convention: short, system-scoped, singular.

Examples: `save`, `quest`, `worldgen`, `dialogue`, `ui`, `inventory`,
`faction`, `material`, `settlement`, `crafting`, `ai`.

If the system already has a `Debug.Log` prefix (e.g., `[Combat]`,
`[Bootstrap]`), reuse the lowercase form for consistency.

#### Step 2 — Decide on/off-by-default

- **On by default** if the system is sparse (a few records per turn at
  most). E.g., `save`, `quest`, `dialogue` — fired on player choice
  or system events.
- **Off by default** if the system is chatty (potentially hundreds of
  records per turn). E.g., `material` (per-tile thermal), `ai`
  (per-NPC goal-stack thrash). Flip on with `diag_set_channels` only
  when investigating.

If unsure: off-default and verify with profiling on the D1 success
criterion (≤ 5 ns when disabled).

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
    payload: new {                  // anonymous object → JSON
        bytesWritten = 1234,
        partCount = entity.Parts.Count,
        effectCount = entity.GetPart<StatusEffectsPart>()?.EffectCount ?? 0
    }
);
```

The hook **must not throw or block**. Recording is fail-soft (the
substrate wraps every call in try/catch). Records are written; system
proceeds.

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

## 11. Second-pass critique — what the generality revisions still don't fix

This section tracks honest remaining issues after the §3/§4/§10
generality pass. Listed for transparency rather than addressed
inline because (a) most are minor; (b) some are forward-looking
("verify after next ship") rather than fixable now; (c) burying
them risks them being forgotten.

### Issues addressed inline during this pass

- ✅ LOC math reconciled (700 → 970).
- ✅ `get_channels`/`set_channels` collapsed into one `DiagChannelsTool` with action parameter.
- ✅ Save/load worked example hedged ("the actual SaveSystem method names will differ").
- ✅ `Turn`-nullable query semantics specified (excluded from `since_turn`/`until_turn`; included via `since_unix_ms`).
- ✅ Cause-ID threading mechanism specified (explicit / `using` scope / null fallback).

### Issues flagged but NOT addressed (rank-ordered by build risk)

**🟡 1. The "generality criterion" in §4 verification is forward-looking, not a Tier-1 gate.**

I wrote: "I should be able to walk through §10's recipe and produce a
working `category="save"` observability addition (in mock form, ~30 min)
without modifying any Tier 1 file."

This isn't testable inside Tier 1 — there's no second consumer yet.
It's an architectural claim ("the substrate will generalize") that
becomes verifiable only when the second non-combat ship lands. The
honest reframe: **The first non-combat system shipped after Tier 1
must follow §10's recipe verbatim. If implementing it requires
substrate, schema, or tool changes, that's a bug filed against the
recipe, NOT against the system.** That's a future commitment, not
a Tier-1 acceptance gate.

**🟡 2. Hook taxonomy is fuzzy: "Universal" mixes meta-hooks and high-leverage system hooks.**

In §3 Layer 1, I labeled three hooks "Universal":
- `Entity.FireEvent` — TRUE universal (captures all GameEvent dispatch from any system; meta-level).
- `TurnManager.EndTurn`/`BeginTakeAction` — TRUE universal (turn boundaries are system-agnostic).
- `MovementSystem.TryMove` — NOT meta-universal; it's a single system that happens to be USED by many features.

The sub-categories are different: Entity.FireEvent gives "many systems for free"; MovementSystem.TryMove gives "deep visibility into one system that has many consumers." Both are valuable but they're different leverage shapes. Current grouping is acceptable, but a future revision could split into "Tier-A meta hooks" vs "Tier-B high-leverage system hooks."

**🟡 3. Category naming convention not pinned.**

§10 says categories are "free-form lowercase string." But long names like `combat_status_effects_lifecycle` would make queries verbose; short and clear (`effect`) is better. Convention to add to §10 Step 1: ≤ 16 chars, single-word lowercase preferred, snake_case if multi-word, match existing `Debug.Log` prefix when one exists.

**🟡 4. High-frequency-system escape hatch missing from §10.**

Some systems fire 100+ events per turn even when "off-default" makes sense (e.g., per-tile thermal propagation, per-frame UI input polling). Just turning the channel on overflows the buffer; turning it off loses signal entirely. Recipe needs an escape clause: "if the system fires > 100 events per turn typical, build an in-system aggregator first (counts + summary statistics) and only `Diag.Record` summaries, not individual events."

**🟡 5. Specialized tool examples (`diag_damage_history`, `diag_effect_lifecycle`) demoted to "examples" but their LOC is still in the Tier-1 budget.**

Wait — re-reading: I removed them from the Tier-1 file table entirely and noted they're a small follow-up commit. ✅ This is actually addressed correctly. Striking.

**🟡 6. `diag_inspect_entity` is listed as Universal but its implementation may need system-specific extension points.**

`diag_inspect_entity` returns "parts, stats (with modifier sources), effects, tags, properties, last N events." That's combat-friendly. For richer system-specific entity views (e.g., a save-debugging session might want "serializer cursor position," a quest-debugging session might want "active quest objectives"), the tool needs a way for systems to register custom inspectors.

Concrete fix would be: `Diag.RegisterEntityInspector("save", entity => ...)` API. Tier 2 work; not Tier 1.

**🟡 7. `Diag.Record` payload-object lifetime.**

I wrote: `payload: new { effects = ... }` — anonymous object, JSON-encoded. But when does the encoding happen? If lazy (at query time), the anonymous object captures live entity references that go stale. If eager (at Record time), there's a per-call serialization cost on the hot path.

**Right answer is eager.** Document this in §3 Layer 0: "`payload` is JSON-serialized SYNCHRONOUSLY inside `Diag.Record`; references go stale safely because the JSON snapshot is what's stored." Worth pinning.

**🟡 8. `DiagInspectEntityTool` "last N events targeting this entity" — query is undefined.**

What does "targeting" mean? `Record.TargetId == entityId`? `Record.ActorId == entityId OR TargetId == entityId`? Just events that named this entity in any field? Spec ambiguous. Pick one and document: probably `ActorId == entityId OR TargetId == entityId`, with a `relation` parameter to narrow.

**🔵 9. §6 (Tier 3) and §7 (out-of-scope) read combat-flavored.**

§6 example: "auto-flag X HP changed without DamageDealt event." With a general substrate, that's "any anomaly: entity field changed without a corresponding cause record." §7 lists "Player-facing UI" as out of scope, but UI is a system that COULD be observed (per the §3 "per-system" table). The "out of scope" really means "we won't add a player-facing UI for the diag system itself" — different meaning. Wording could be tighter.

**🔵 10. No story for cross-process observability.**

If the player runs a release build (no Unity Editor MCP), the substrate has no query surface. For Tier 1 / Tier 2 this is fine — debugging happens in Editor. But long-term (post-Steam-release scenarios in `Docs/CONTENT-ROADMAP.md` Tier 5), would players running a release-mode game have any way to capture diag for support? Out of scope now; flagging.

**🔵 11. No quota / rate-limit on per-tool response size.**

P2 says "≤ 5 KB default response budget" but there's no enforcement mechanism in the schema. A `diag_query` with no `limit` parameter could return 500 records × 2KB = 1MB. The default `limit=50` constrains it, but a careless caller can override. Worth adding: tools refuse to return responses > 100 KB, returning `{ truncated: true, hint: "use cursor or smaller limit" }` instead.

### What this critique pass did NOT do

- I did **not** verify any specific method names in the Caves of Ooo `SaveSystem` source. The §10 example is illustrative only.
- I did **not** profile recording cost. P9 claims 5 ns when disabled; that's an estimate, validated during D1.
- I did **not** test the §10 recipe end-to-end on a real second system. That's the §11 "second-pass critique 🟡 #1" — verifiable only when next non-combat ship lands.
- I did **not** define the persistence format (jsonl schema, file rotation policy, max disk usage). Surfaced as a D5 design question.

### Recommended posture for Step 1 (the spike)

These remaining issues are **acceptable risks for the spike.** None block Tier 1 D1+D2 (substrate + universal hooks + one combat hook). They become real questions during D3-D5 and the first post-Tier-1 ship.

Document priorities for the next revision pass (post-spike):

1. Pin payload-lifetime semantics (🟡 #7) before any third party reads §3.
2. Define `diag_inspect_entity` "targeting" (🟡 #8) before D4 implementation.
3. Add high-frequency aggregation escape hatch to §10 (🟡 #4) before second non-combat ship.
4. Rest can wait.

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
