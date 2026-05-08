# Caves of Ooo — Project Rules for Claude

> Auto-loaded every session. Read this first. The rules below are
> non-optional unless the user explicitly relaxes them in a given turn.

The full **Methodology Template** lives at `Docs/QUD-PARITY.md` §5162-6209.
This file is the always-on summary + workflow trigger. When in doubt about
specifics, open that section.

---

## Always-on rules

1. **TDD is the default cadence.** Write the failing test (assertion OR
   compile error) BEFORE the production code. Run it. Confirm RED.
   Then implement the minimum to pass. See §2.1.

2. **Pre-impl verification sweep on any major feature.** Read every
   reference the plan cites. Confirm API shapes, signatures, line
   numbers, mechanic semantics. Log corrections in a table BEFORE
   writing code. The combat port caught **3 false premises** this way
   (Phases B, F, H) — saved an estimated 2-3 days. See §1.2.

3. **Living docs over chat-buffer plans.** New phase, milestone, or
   audit gets a section in `Docs/<thing>.md` with: status, Qud
   reference, implementation snippet, divergences table, in-phase
   self-review, implementation log, files changed. **Update the doc
   in the same commit as the code.**

4. **In-phase self-review BEFORE commit, not after.** Severity markers
   🔴/🟡/🔵/🧪/⚪. Fix 🟡+ pre-commit. Defer 🧪/⚪ with a note. See §5.

5. **Counter-checks for every positive assertion.** Every "X happens"
   test pairs with "Y identical-setup-but-flag-flipped does NOT
   happen." Without counter-checks, "the test passes" can mean "the
   precondition was vacuous." See §3.4.

6. **Honesty bounds in every PlayMode / live report.** Explicit
   "can verify (script-observable)" and "cannot verify (visual / feel)"
   sections. Don't paraphrase raw output. See §6.3.

7. **Scope-divergence transparency.** If the plan said X and you ship
   Y, the commit body has a SCOPE DIVERGENCE section explaining why,
   with file:line citations. See §6.4.

8. **Commit message template** (§2.3):
   ```
   <type>(<scope>): <tight present-tense summary>

   <2-3 sentence problem statement in user-visible terms>

   IMPLEMENTATION NOTES (risks verified before writing code)
     1. ...

   SCOPE DIVERGENCE FROM THE PLAN (if any)
     ...

   <PHASE> SELF-REVIEW (Methodology Template §5) (if applicable)
     🟡 ...
     🔵 ...

   Files:
   - NEW/MOD <path>: <one-line purpose>

   Tests: <N> -> <M> (+D). All green.

   Co-Authored-By: ...
   ```

---

## Major-feature workflow (use when starting any non-trivial work)

For features matching two or more of:
- Touches multiple systems (AI + combat + UI, etc.)
- Requires new blueprint/JSON content
- Claims parity with a reference codebase
- Has non-obvious failure mode (state-machine, timing, RNG)
- Will be wired into player-exercisable gameplay

**Do these in order. Do not skip steps.**

1. **Plan to disk** — write `Docs/<feature>.md` with goal, scope,
   content-readiness analysis (🟢/🟡/🔴/⚪), sub-milestone breakdown.
   §1.1.

2. **Verification sweep** — read every reference the plan cites
   (Qud source, existing CoO classes, blueprints). Produce a
   corrections table for any drift. Single highest-leverage step
   in the protocol. §1.2.

3. **Scope-prune with rationale** — if the sweep reveals a sub-feature
   is redundant in current code, document the cut + cite the lines
   that make it redundant. §1.3.

4. **Sub-milestones smallest-blast-radius-first** — each commits as
   one reviewable change, independently revertable, ships one complete
   testable behavior. §1.4.

5. **Per sub-milestone, RED→GREEN→adversarial→review→commit:**
   a. Phrase invariant in user-visible terms (§2.1)
   b. Write RED test against that invariant
   c. Run, confirm RED
   d. Implement minimum to pass
   e. Run, confirm GREEN
   f. Add counter-check (§3.4)
   g. Add adversarial / mutation-resistance test (§3.9)
   h. Self-review with severity markers (§5)
   i. Fix 🟡+ findings
   j. Update living doc with phase section
   k. Commit using the §2.3 template

6. **PlayMode sanity sweep** for player-visible features (§3.5):
   `manage_editor play` → preflight check → per-scenario raw output
   → `manage_editor stop` → summary table with honesty bounds.

7. **Manual playtest scenario** for visual/feel-dependent features
   (§3.6): a `Scenarios/Custom/<Name>.cs` + menu entry.

---

## Self-audit checklist (run BEFORE every commit)

For each diff hunk, ask:

- [ ] Does this commit introduce production behavior? Is there a
  test diff in the same commit covering it? (§2.1)
- [ ] Did the test fail RED before this implementation? If you can't
  recall, you compressed steps — note it in the self-review.
- [ ] For every positive assertion, is there a counter-check that
  would FAIL if the wiring were wrong? (§3.4)
- [ ] Are there magic numbers that should be named constants?
- [ ] Are there docstring claims unbacked by tests? (these become
  🧪 findings)
- [ ] Does any commit message claim "Qud parity" when the work is
  actually CoO-original? Re-classify per §4.2.
- [ ] If the diff exposes new public API (method, field, event), is
  the contract documented inline?
- [ ] Did the verification sweep reveal a false premise? Did you
  document the correction in the commit body?

Findings format (one per finding):

```
🟡 Finding N — <one-line title>
**File:** <path>:<line-range>
<1 paragraph: what's wrong, what's observable>
**Why it matters:** <concrete consequence>
**Proposed fix:** <1-3 sentences, sketch only>
```

Fix every 🟡 and 🔴 pre-commit. Defer 🧪/⚪ with a note in the doc.

---

## Post-implementation cold-eye review (MANDATORY after every multi-commit feature)

**Tests passing is necessary but not sufficient.** Green tests prove
the code does what the test asserts; they don't prove the code is
internally consistent, symmetric with neighbors, or matches the
docs. Always run the four-question pass below AFTER tests are green
and BEFORE you call the feature done. Empirically this caught four
real issues in D2 (hook-position asymmetry, redundant payload field,
test-coverage gap, doc-vs-impl drift) AFTER all 29 tests were green
and AFTER the merge had landed on main.

### Q1 — Symmetry check

When you add a feature that mirrors an existing one (OnApply mirrors
OnRemove, "Begin" mirrors "End", forward mirrors backward, etc.):
**open both source files side-by-side and read them line-by-line.**
Position of the new code relative to surrounding calls must match
the existing one. Caught D2.1: OnApply was firing before
`effect.Applied`, but D1.2's OnRemove fires after `effect.Remove` —
asymmetric chronological position relative to the user-visible
message-log entry.

The check: *if I swap a record's category from "OnApply" to "OnRemove"
in my head, would the surrounding code make equally good sense?*
If no, one side is misplaced.

### Q2 — Cross-feature consistency

When you add multiple similar features in one ship (D2 added 5
hooks + 1 tool), audit **every public-facing shape** — payload
schemas, return types, parameter names, channel names — for naming
convention. Make a mental table:

```
Hook            | category | kind          | actor | target | payload fields
OnRemove        | effect   | OnRemove      | -     | yes    | effect, duration, cause
OnApply         | effect   | OnApply       | yes   | yes    | effect, duration, justApplied, forced
DamageDealt     | damage   | DamageDealt   | yes   | yes    | amount, hpAfter, lethal, attributes
turn/Begin      | turn     | Begin         | yes   | -      | blueprintName, hp        ← had `entityId` redundant w/ ActorId
turn/End        | turn     | End           | yes   | -      | blueprintName, hp        ← same
```

Each column should follow a consistent rule. If one row deviates,
flag it. Caught D2.4: turn payload duplicated `entityId = actor.ID`
in the payload while the other hooks correctly put IDs only in the
top-level field.

### Q3 — Counter-check completeness

For every payload field with a non-trivial branch (boolean flags,
enum-ish values, success/failure paths, optional vs default args),
verify **each branch is asserted by a test**, including a counter-
check per §3.4. Caught D2.1: the `forced` field had two paths
(`ApplyEffect` → false, `ForceApplyEffect` → true), only the false
path was implicitly tested. A buggy impl that hard-coded
`forced = false` for both paths would have passed all 5 D2.1 tests.

The check: list every non-trivial field in the payload and trace
which test exercises which value. Gaps in the table become tests.

### Q4 — Doc-vs-impl drift

Open the design doc (`Docs/AI-OBSERVABILITY.md` for diag work,
`Docs/<feature>.md` generally) and read each spec'd
parameter / return shape / response field against the **actual
shipped code**. Caught D2.5: AI-OBSERVABILITY.md's generic-tools
table claimed `diag_count` returns `{ count: int }`, but the shipped
tool returns
`{ count, total_scanned, sample_first_trace_id, sample_first_kind, tool_version }`.

Doc-vs-impl drift accumulates silently and turns living docs into
useful-fiction. The fix is fast (5 minutes per ship); the cost of
not doing it compounds.

### Process

1. Tests green ✓
2. Merge to main ✓ (or about to)
3. **STOP. Run the cold-eye pass.** Read all the diffs together, not
   one-by-one.
4. Found something? File a `fix/<thing>-cold-eye-review` branch
   with a single self-contained fix-commit. Don't bury it in a
   future feature commit. Push. Merge. Now done.
5. Found nothing? Note "cold-eye review pass complete, 0 findings"
   in the merge commit body so a future reader knows it actually
   ran.

**Self-directive: I MUST run this cold-eye pass after every
multi-commit feature, even when tests are green and the merge
feels clean. Tests-green-feels-clean is exactly the state where
latent inconsistencies hide.**

---

## Unity MCP workflow

**Standard post-edit cycle:**
1. `mcp__unity__refresh_unity` (compile request)
2. `mcp__unity__read_console types=[error]` — must be empty
3. `mcp__unity__run_tests mode=EditMode` — must pass
4. If timeout: `manage_editor stop`, retry
5. If new test count looks stale: `refresh_unity mode=force`, retry
6. Commit

**Common pitfalls (§7.2):**
- **Stale-assembly trap**: tests pass with the OLD code. Re-run after
  `refresh_unity`. The combat-port self-review hit this twice.
- **Stale-assembly trap (Play-mode variant)**: if `editor.is_focused: false`,
  Unity defers domain reloads even when `editor_state` reports
  `ready_for_tools: true`. Symptom: a Play-mode reflection probe shows
  a `CavesOfOoo.dll` build time older than the on-disk DLL, and new
  types/methods are missing. **Fix:** ask the user to focus the editor
  window or restart it; `refresh_unity {mode: force}` alone is not enough.
  Met during the Phase F/G/H methodology-debt closure (2026-04-26).
- **Active-instance routing can drop**: if multiple Unity Editor instances
  are connected to the same MCP server, `set_active_instance` may not
  persist for the whole session — a later call can route to a *different*
  project's editor. Symptom: tests "vanish" or `read_console` returns 0
  errors when there should be many, because the request hit a different
  Unity. **Fix:** re-confirm `editor_state.unity.instance_id` before
  relying on a result. Re-pin via `set_active_instance` if it drifted.
  Met during M2 of the storylet/quest layer plan (2026-04-26).
- **Test job init timeout** during recompile is normal — retry once
  after the editor settles (`sleep 3` if needed).
- `int.TryParse(arg, out duration)` writes 0 on failure AND returns
  false. Always pair with explicit sentinel guard.
- `Zone.GetReadOnlyEntities()` returns a live `Dictionary.KeyCollection`.
  Do NOT iterate while calling `ApplyEffect`/`AddEntity`/`RemoveEntity`.
  Use `zone.GetAllEntities()` instead (see Zone.cs:141).
- `execute_code` is **read-only state observation**. Firing gameplay
  events through it corrupts state — see `Docs/MCP_PlayMode_Testing_Strategy.md`.
- Entering Play mode resets the scene. **Warn the user before doing it.**

**Tool selection (§7.1):**

| Tool | Use for | Don't use for |
|---|---|---|
| `refresh_unity` | Compile after script edits | Stopping play mode |
| `read_console` | Errors after compile | Profiling |
| `run_tests` | Full EditMode/PlayMode suite | Single-test filter (often unreliable) |
| `manage_editor play/stop` | Enter/exit Play mode | Dismissing UI |
| `execute_code` | Read-only state observation | Firing events |
| `manage_input` | Keyboard playtest flows | Logic without a real key binding |

---

## When to delegate to a subagent (Task tool)

Use Task when the work is genuinely bounded and self-contained.
Don't use it for the actual feature work — delegating implementation
loses too much context.

| Task type | Subagent |
|---|---|
| Open-ended codebase questions | Explore (any thoroughness level) |
| Find symbol/file | Explore quick |
| Multi-file refactor with clear targets | general-purpose |
| Plan an implementation strategy | Plan |
| Code-review a diff for methodology compliance | general-purpose w/ explicit "review against §5 severity scale" prompt |

**Delegating self-review:** if you've been deep in implementation for
a while, spawn a general-purpose agent with the diff and the
`Methodology Template §5` checklist. Cross-check 3-4 of its claims
against source files before accepting the report (§5.4).

---

## Parity / reference-code work

When porting from `qud_decompiled_project/` (in-repo subset, 521
files, mostly visual) or `/Users/steven/qud-decompiled-project/`
(full 5368-file decompile, includes core game logic):

1. **Survey before claiming** — read each candidate file, compare
   signatures + mechanical behavior. (§4.1)
2. **Classify honestly** — Match / Extension / Divergent / CoO-original.
   Overclaimed parity is a bug. (§4.2)
3. **Document divergences** — per-phase divergence table in the
   living doc, NOT just the class docstring. (§4.3)

**False-premise detection patterns** (caught 3× in the combat port):
- "Qud has X" — verify by reading the actual file. Memory is unreliable.
- "Qud doesn't have X" — `grep -rn` to confirm. Easy to miss.
- "These helpers are missing" — check both instance and static surface.

---

## Reference docs (in this repo)

| Path | What it covers |
|---|---|
| `Docs/QUD-PARITY.md` §5162-6209 | Full Methodology Template (the canonical rules) |
| `Docs/COMBAT-QUD-PARITY-PORT.md` | 10-phase combat-system port — every phase fully documented per template |
| `Docs/COMBAT-PARITY-PORT-REVIEW.md` | Self-review findings doc + PlayMode sweep results |
| `Docs/COMBAT-AUDIT-PLAN.md` | Audit cadence pattern (Phases 0-4½ structure) |
| `Docs/COMBAT-BRANCH-MAP.md` | Branch coverage map (per-method ✅/⚠️/❌) |
| `Docs/COMBAT-TEST-BACKLOG.md` | Prioritized test entries with format `[#] (TARGET, SEVERITY, BUG_CLASS, PHASE)` |
| `Docs/MCP_PlayMode_Testing_Strategy.md` | Live-bootstrap testing rules (Rule 1: never fire events via `execute_code`) |
| `Docs/PERF-FOUNDATION.md` | Optimization patterns, anti-patterns, audit findings (read before adding any feature touching per-frame paths) |
| `Docs/PERF-COMBAT-INVESTIGATION.md` | Original combat-perf audit (2026-04) — hypotheses + which were red herrings |
| `qud_decompiled_project/` | In-repo Qud subset (visual effects only) |
| `/Users/steven/qud-decompiled-project/` | Full Qud decompile (core game logic, 5368 files) |

---

## Project specifics (auto-memory)

- **Game scene:** `Assets/Scenes/SampleScene.unity` (NOT `Assets/game.unity`)
- **Unity 6** (6000.3.4f1) at `/Users/steven/caves-of-ooo/`
- **Architecture mirrors Qud:** Entity (bag of Parts) + GameEvent (string-keyed)
- **CP437 tilemap** at runtime (80×25), Sprite-Lit-Default + Global Light 2D
- **EditMode tests:** 2181+ NUnit tests, ~30s full suite

---

## Observability — every gate emits a diag record

Before writing the production code for a new feature, decide which
diag records it will emit. Don't punt this — the substrate
(`Diag.cs`) and the MCP tools (`diag_query` / `diag_count` /
`diag_assert` / `diag_inspect_record`) exist precisely so future
"why didn't X happen?" debugging starts with a query, not with code
grep + `execute_code` poking. If the records aren't there, the
debug session degrades to detective work — which is what the
WSP3-7 skill-system buildout did because the dispatch path was never
instrumented.

**The rule:** every action that can succeed OR fail emits a
category-appropriate diag record on each branch. Specifically:

1. **Every gate that can reject emits a record.** Cooldown checks,
   weapon-class checks, line-of-sight checks, target-type checks,
   resource-cost checks, etc. — each gets a `kind=Rejected` (or
   similar) record with a `reason` field naming which gate fired.
2. **Every successful action emits a record.** `kind=Routed` /
   `kind=Casted` / `kind=Activated` etc. The record is the proof
   the system worked end-to-end.
3. **Use the standard categories.** `event`, `effect`, `damage`,
   `turn`, `furniture`, `trade`, `quest`, `skill`, `ai` — listed in
   `Diag.DefaultOnCategories`. Add new ones only when none fit.
4. **Payload includes everything a debugger would want.** For a
   skill: command name, skill class, display name, cooldown
   remaining, reason for rejection. For damage: amount, attributes,
   resistance reduction. For movement: from/to cells, blocked
   reason if applicable. The payload is read by `diag_query`'s
   PayloadJson — make it grep-friendly.

**The verification workflow:**

When a user reports a bug ("X doesn't work"), step 1 is:
```
diag_assert category=<area> kind=<expected_success_kind>
```
- `matched=false` → the success path never fires → investigate the
  upstream pipeline (input dispatch, condition gates, etc.)
- `matched=true` → the success path fires → investigate downstream
  (effect resolution, target state, side-effect propagation)

**The pattern that surfaced this rule (skill-system bug, 2026-05):**
`SkillsPart.TryRouteSkillCommand` emitted `Added`/`Removed` records
but had ZERO instrumentation on the activation pipeline. When the
user reported "Slam doesn't hit," the right first query was
`diag_assert category=skill kind=CommandRouted` → which would have
returned `matched=false` and immediately pointed at the dispatch
gap. Instead the diag stream was unhelpful (only Add/Remove
records), so the debug session burned tool calls grepping +
execute-coding. The fix added `CommandRouted` and `CommandRejected`
emissions; future bugs in the activation pipeline now surface as
expected.

**Tests pin the emissions.** Like other contract-level invariants,
diag emissions need test coverage so a future contributor can't
silently drop them. Pattern: in the unit test for the action,
`Diag.ResetAll()` then exercise the action then `DiagQuery.Apply`
to assert the expected record fired. See
`SkillsPartTests.HandleEvent_SuccessfulRoute_EmitsCommandRoutedDiag`
for the canonical example.

---

## Performance — non-negotiables for new features

Read `Docs/PERF-FOUNDATION.md` before adding any feature that touches
the per-frame or per-turn paths. The strategies + audit history live
there; the rules below are the always-on subset.

1. **Profile before optimizing, profile after writing.** Use
   `Unity.Profiling.ProfilerRecorder` over real gameplay (60-90s
   window) — not isolated 5-call tests. Sort by **max**, not avg,
   to catch spikes. The 70,000× perf bug we shipped a fix for in
   2026-04-28 was invisible to isolated tests; only `[Perf] spike`
   logs caught it.

2. **Cache misses must be cheap.** Any `Dictionary<,>` lookup
   followed by "compute and insert" must be cheap to miss — or
   gated behind explicit invalidation, never per-call. Pre-populate
   the cache for the full input domain or just return a fallback
   on miss.

3. **No allocations in hot paths.** `LateUpdate`, `Update`, turn
   loops, per-cell render, per-NPC AI scan — none of these may
   `new List<>` / `new Dictionary<>` / use LINQ. Use the scratch-list
   pattern in `Docs/PERF-FOUNDATION.md §Pattern 1`.

4. **Per-cell dirty hooks for visible changes.** If gameplay code
   changes a cell's visible state (entity moved, color flash,
   status applied), call `ZoneRenderHooks.MarkCellDirty(x, y, source)`.
   Do not call the full-zone `MarkDirty` unless FOV / lightmap
   actually needs recompute (player moved, light source moved).

5. **Renderers gate work behind content fingerprint.** Sidebar,
   hotbar, and any new UI panel must skip work when the snapshot
   it would render is identical to the last frame's. See
   `SidebarRenderer.ComputeSnapshotFingerprint` for the pattern.

6. **`Application.runInBackground = true` for development.** This
   is set in `ProjectSettings.asset` (`runInBackground: 1`). Without
   it, the editor throttles to 10fps when the window loses focus —
   indistinguishable from "the game is laggy" in a profiler. If
   you ever see `cpu_frame_time = 100ms` with `cpu_main_thread = 0ms`,
   this setting reverted.

**Performance section required in feature plans.** When two or more
of these apply, the feature's `Docs/<feature>.md` plan needs a
**Performance** section citing which patterns it'll use:
- Plumbs `ZoneRenderHooks` from gameplay
- Allocates collections inside per-frame / per-turn methods
- Adds a new cache (`Dictionary<,>` keyed by gameplay state)
- Adds a new MonoBehaviour with `Update` / `LateUpdate`
- Adds a new event listener that fires per-frame / per-turn

---

## Final reminders

- **Don't barrel into work after a false-premise correction.** Stop,
  surface the correction, get user confirmation, then proceed.
- **The audit cadence (gap-coverage → adversarial cold-eye) is what
  catches latent bugs.** Empirical: 0% on M-style code (already
  TDD'd), 12.5% on legacy code. See §3.9.
- **GC pressure is acceptable in turn-based combat.** Don't reach
  for event-pool optimizations unless a profiler shows a problem.
- **You will be tempted to skip steps when the work feels small.**
  The combat port's three false premises were "small" features.
  Run the sweep anyway.
