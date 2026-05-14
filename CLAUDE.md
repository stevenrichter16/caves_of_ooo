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
   precondition was vacuous." See §3.4. **Note:** counter-checks are
   per-invariant pairs, distinct from the dedicated adversarial test
   sweep (§Adversarial test sweep) which probes bug-class taxonomy
   per-feature. Both gates apply.

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

### Run BOTH audit angles — taxonomy AND Qud-parity-first

The cold-eye review and the optional post-merge Explore-agent audit
should run from **two distinct angles**, not one. A single angle
has a fixed checklist and misses what the checklist doesn't ask
about.

**Angle A — Bug-class taxonomy** (the default):
- Null safety, atomicity, iterator-vs-mutation, race conditions
- Save/load reach, anti-exploit gates, diag dispatch
- Counter-check completeness, dead-code sweep
- Driven by the adversarial-test-sweep surface list

**Angle B — Qud-parity-first** (the missing half):
- Read the CoO impl and the matching Qud source side-by-side
- For each line of CoO code, ask: "what does Qud do here, and am I
  doing it the same way?"
- Drift findings: 🔴 CRITICAL (gameplay-observable Qud contract
  broken) / 🟡 NOTABLE (observable but acceptable) / 🔵 NIT
  (no gameplay impact) / ⚪ DOCUMENTED (deliberate, already noted)
- What events does Qud listen for that CoO might not?
- What guards does Qud have that CoO is missing?
- What helper methods / convenience wrappers exist in Qud's source
  that CoO would silently lack?

**Why both:** the F.3 Followers feature illustrated this empirically.
The first audit pass ran Angle A and shipped 5 findings (all real),
but deferred a 6th — "OnDestroyObjectEvent unapply hook" — as "CoO
lacks these events." A second pass running Angle B re-checked that
claim, found CoO DOES fire a `"Died"` event on the dying entity
(CombatSystem.cs:916), and surfaced the real latent bug: killing your
own follower leaked rep indefinitely. The taxonomy checklist didn't
include "what events does Qud listen for that CoO might not?" — that's
a Qud-parity question, not a bug-class taxonomy question.

**Process:**
1. Run Angle A first (it's the cheaper checklist — null safety,
   atomicity, iterator safety — all easy to enumerate).
2. **Then** run Angle B specifically (Explore-agent prompt that says
   "Qud-parity-first" and lists the Qud reference files alongside the
   CoO files). The two passes are complementary, not redundant.
3. Triage findings from both into one fix branch. Note explicitly in
   the merge commit which angle caught each finding so future audits
   can be honest about what each pattern covers.

**Self-directive: for any Qud-parity feature, I MUST run BOTH audit
angles before declaring the phase complete. Skipping Angle B because
"Angle A found nothing meaningful" is exactly how Qud-contract drift
slips through — the taxonomy angle isn't looking for those.**

### Hypothesis-driven deep audit — write RED tests BEFORE re-reading code

Both Angle A (taxonomy) and Angle B (Qud-parity-first) re-read existing
code looking for problems. That mode has a blind spot: **bugs hide
where the code looks right.** When the first audit pass declares "0
findings" but the feature is non-trivial, a third pass is warranted —
and the most effective methodology for it isn't more re-reading.
It's **hypothesis-driven RED-test authoring**.

**The methodology:** write the RED tests against hypothesized gaps
FIRST, then implement the minimum fix until each goes GREEN.

**The process:**

1. Step away from the code. Ask: *what does the player actually do
   that the per-phase tests don't simulate?* Generate hypotheses
   from player-flow, not from re-reading source. Examples that
   surface real bugs:
   - "What if the player applies a content-time mutation to an
     already-equipped item?" → Apply-to-already-equipped silent no-op
   - "What if a hook removes itself during dispatch?" → iterator skip
   - "What if a service is called with a negative reward?" → pin contract
   - "What if a stack has exactly count=1 at the boundary?" → pin boundary
2. Write **one RED test per hypothesis**. State the hypothesis in
   the test's docstring so future readers know what was being probed.
3. Run the tests. The ones that fail RED are confirmed bugs. The
   ones that go GREEN are **pinned-as-correct invariants** —
   regression infrastructure for free, since the test exists in
   case a future change breaks the contract.
4. Fix the RED bugs with the minimum change to flip each to GREEN.
   No speculative cleanup, no refactoring drift.
5. Commit with explicit per-test classification: "N confirmed bugs
   (RED→GREEN), M pinned-as-correct (already passes), K cross-system
   integration." The classification is honest about what each test
   does.

**Why this beats re-reading:**

- Re-reading suffers from confirmation bias — code that *looked*
  correct on first read tends to look correct on second read.
- Re-reading audits are structured around the code's organization
  (file by file, class by class). Bugs that span multiple files
  (cross-system) or live in the gap between two "correct" units
  (Apply attaches a Part / OnEquipped fires on equip — but Apply on
  an already-equipped item never re-fires OnEquipped) hide from
  per-file re-reads.
- RED-test authoring forces concrete scenario thinking. "What if
  the player does X then Y?" maps directly to a test setup. The
  re-read frame "is this code correct?" doesn't.
- Even when hypotheses turn out to be wrong, the pinned-correct
  test is permanent regression infrastructure — strictly more
  valuable than a re-read pass that finds nothing.

**Empirical evidence (Item Enhancements E.5.1):**

The E.4.3 audit ran BOTH angles and shipped 5 findings (1 latent
🟡 fixed, 1 🟡 hypothesis falsified, 3 minor cleanups). It
declared "0 real gameplay bugs." Honest at the time of writing.

Then the user prompted "do another run of code review." Instead of
re-reading, I generated 8 player-flow hypotheses and wrote 12 RED
tests. Result: **3 confirmed bugs (2 gameplay-visible silent
failures + 1 theoretical-but-real iterator bug), 4 pinned-as-
correct invariants, 1 cross-system integration check.** The
gameplay-visible silent failure was apply-Lacquered-to-currently-
worn-armor → AV bonus didn't land until re-equip cycle. That bug
existed for the entire shipped feature surface; both prior cold-eye
passes missed it because the per-phase mental model was "Apply is
content-time, OnEquipped is equip-time" — no overlap considered.
The hypothesis "what if the player applies during the equip-time
window?" surfaced it on the first try.

**When to run this pass:**

- After every multi-commit feature where the first audit pass
  declared "0 findings" or "no real bugs" — that's exactly the
  state where this pass is most valuable
- After every cold-eye that surfaced only `🔵` / `🧪` / `⚪`
  findings (i.e. no `🟡` or `🔴`) — the audit may have run a
  re-read pattern that the code's organization can defeat
- For any feature with cross-system flows (item ↔ inventory ↔
  combat ↔ NPC) where bugs live between systems

**Honesty bound:** this methodology is bounded by the hypotheses
the author can generate. It won't find a bug that nobody thinks to
hypothesize about. But it has a much higher ceiling than re-reading
— player-flow questions are easier to brainstorm than "what's
wrong in this 200-line file." Pair it with the fuzzing/property-
based testing pattern (out of scope of this guide) for the
truly-novel bug surface.

**Self-directive: when a cold-eye pass declares "0 real bugs" on a
non-trivial feature, I MUST run the hypothesis-driven deep audit
before declaring the feature complete. Generate 6-12 player-flow
hypotheses, write RED tests for each, classify the results
honestly. Even if all 12 turn GREEN, the regression pins are
permanent infrastructure.**

---

## Adversarial test sweep (MANDATORY for any feature with non-trivial state, parser, or cross-actor flows)

> **Full playbook: `ADVERSARIAL_TESTING.md` (project root).** That doc
> is the deep how-to: bug-class taxonomy, strategies for new vs
> existing features, code patterns, case studies, anti-patterns,
> reusability guidance for other projects. THIS section is the
> always-on trigger + checklist; open the playbook when you're
> actually designing a sweep.

**This is a separate gate from the per-sub-milestone "step g"** — that
step adds 1-3 mutation tests inline with the main suite (`Tests:` at
the bottom of `<Feature>Tests.cs`). The gate documented here adds a
**dedicated `<Feature>AdversarialTests.cs` file** with 20-60 tests
targeting bug classes the happy-path + counter-checks tests can't see.

The two are complementary, not redundant:
- **Step g (per-invariant):** "if I flip the precondition, does the
  test still fail?" — pairs with each positive assertion.
- **This gate (per-feature):** "what bug classes could exist that no
  per-invariant test would catch?" — driven by a taxonomy of surfaces,
  not by the spec.

Empirically:
- Skill-system audit: **80 adversarial tests** found 0 bugs but caught
  the **pre-WSP8.2 SkillRejected diag gap** (asymmetry between newer
  and older actives) during the symmetry sub-pass.
- Rental system: **43 adversarial tests** across two waves found 0
  bugs in main, but the suite's existence proves invariants like
  *RentalPart fields round-trip via reflection* + *CanBeTraded veto
  vs post-return clearance* + *cross-village blueprint matching* —
  contracts that were undocumented before the test pinned them.
- On-hit effects: **21 adversarial tests** — the prior-shipment
  comment in `OnHitEffectFactory.cs` explicitly credits adversarial
  tests with surfacing the Bleeding-`Magnitude`-vs-`DurationTurns`
  bug. **A real bug, found by this exact pattern.**

### When to do this gate

Run a dedicated adversarial sweep after a feature ships if **two or
more** apply:

- Feature touches **state atomicity** (ink + inventory transfer,
  rent + cooldown, multi-step transactions where partial failure
  needs rollback).
- Feature has a **parser** (string → spec, JSON-driven, comma/
  semicolon-delimited, blueprint-driven content).
- Feature has **cross-actor flows** (renter / lessor, attacker /
  defender, source / target, two-party state mutations).
- Feature has **stacking semantics** (status effects, counters,
  wallets, charge buffers — anywhere "X already has Y" matters).
- Feature has **save/load reach** (Parts that survive serialization,
  whether explicitly handled or via reflection fall-through).
- Feature has **anti-exploit gates** (sell-veto, can't-double-rent,
  faction-rep guards — anything where a clever player path could
  break the design intent).
- Feature has **probabilistic / RNG-gated behavior** (chance %,
  effect rolls — boundaries at 0% / 100% / negative).
- Feature has **diag emission contracts** (when does CommandRouted
  fire? When SkillRejected? Both? Neither? Order matters.).

### Bug-class taxonomy — surfaces to probe

Use this as a checklist when designing the file. Not every category
applies to every feature, but reading the list forces you to ask "is
this surface present?"

| Surface | Probe with |
|---|---|
| **State atomicity** | Force a partial-failure path (overweight inventory, missing currency); assert NO partial mutation |
| **Rollback paths** | Same setup but rollback target — verify the system returns to pre-attempt state |
| **Save/load reflection** | Round-trip the entity through `SaveGraphSerializer.SaveEntityBody`/`LoadEntityBody`; assert public fields preserved |
| **Mid-execution death** | Snapshot stability: target dies on swing/tick 1, verify the loop's remaining iterations behave correctly |
| **Cross-actor flows** | Two-party (renter ≠ lessor), three-party (drop-then-pickup-by-other), cross-instance (same blueprint, different ID) |
| **Parser malformed inputs** | Null, empty, whitespace-only, only-delimiters, missing required field, non-numeric where int expected, gibberish, mixed-valid-with-malformed |
| **Stacking semantics** | Re-apply already-present effect; verify NON-stacking returns true from `OnStack`; STACKING extends/accumulates correctly |
| **Boundary inputs** | Null actor / target / argument across every public API; assert no crash |
| **Conversation/dialogue actions** | `int.TryParse` failure paths, null listener, missing argument, non-existent blueprint name |
| **Anti-exploit invariants** | Try the exploit (re-rent, sell-rental, return-to-wrong-actor); assert the gate holds |
| **Probability boundaries** | chance=0 → never fires; chance=100 → always fires; chance<0 → filtered |
| **Self-referential gates** | actor == target, ArcaneSurge-style skip-self via Guid (NOT command-name), "item already in inventory" |
| **Cross-system aggregation** | Multiple modifier sources (skills × mutations) sum correctly via the dispatcher |
| **Effect-name normalization** | Case-insensitive (`BURNING` ≡ `burning`), aliases (`fire` → BurningEffect), whitespace trim |
| **Diag dispatch invariants** | Internal rejection emits BOTH `CommandRouted` AND `SkillRejected`; cooldown emits ONLY `CommandRejected`; non-matching events early-out |
| **Cooldown / re-fire** | Overwrite vs accumulate; second-cast-during-buff-window |
| **Duplicate add/remove** | AddSkill twice → false; RemoveSkill on unowned → false (idempotent) |
| **Multi-instance** | Two physical entities with same blueprint name; verify cross-instance equivalence |

### Pattern: a dedicated adversarial file

```
Assets/Tests/EditMode/.../<Feature>AdversarialTests.cs
```

Structure:
- Class `public class <Feature>AdversarialTests`
- Setup that resets relevant globals (`Diag.ResetAll()`,
  `MessageLog.Clear()`, `SkillRegistry.ResetForTests()`)
- Fixture helpers mirroring the per-feature test file
- Sections grouped by bug class, separated by `// ════════════════`
  comment banners
- Each test method prefixed `Adversarial_*` for easy
  group-name filtering
- Comments explain WHAT bug class is being probed and WHY a buggy
  impl would fail the test ("if a future change suppresses
  CommandRouted on internal bail, queries lose the dispatch trace")

Cumulative coverage measured per-system. The skill-system suite is
the canonical example: 394 baseline + 109 E2E + 37 + 10 deep
adversarial = 540+ tests across the surface.

### Process

1. Per-feature tests green ✓
2. Cold-eye review pass complete ✓
3. **STOP. Audit the bug-class taxonomy above.** Which surfaces does
   this feature touch? Two or more → run the gate.
4. Create `<Feature>AdversarialTests.cs`. Write 20-60 tests across
   the applicable categories. Group by category. Comment intent.
5. Run. Investigate every failure — failure means the test caught
   a real bug, NOT that the test is wrong.
6. Found a bug? File a `fix/<feature>-adversarial-finding` branch.
   Single self-contained fix-commit. Update the source comment to
   credit the adversarial test ("Latent bug surfaced by adversarial
   tests — see <FileName>"). Push. Merge.
7. Found nothing? Note "adversarial sweep complete, N tests, 0 bugs
   found" in the merge commit body. Document the surfaces probed
   so a future reader knows what's covered (and what isn't).

**Honesty bound: 0 bugs found in an adversarial sweep does NOT
prove the system is bug-free. Adversarial tests are bounded by the
bug classes the author imagines. To find truly-novel bug classes
you'd need fuzzing or property-based testing — out of scope of this
gate. The gate's value is (a) catching the rare bug it does find +
(b) creating regression targets so future changes break visibly.**

**Self-directive: I MUST run this gate for any feature where two or
more taxonomy surfaces apply, even when the per-feature tests are
green and the cold-eye pass found nothing. The OnHit Bleeding bug
proves the pattern catches real bugs the other gates miss; the
absence-of-bugs in skill + rental sweeps creates regression
infrastructure for future changes.**

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
| `ADVERSARIAL_TESTING.md` (root) | Adversarial testing methodology playbook — bug-class taxonomy, new-vs-existing strategies, code patterns, case studies. Read before designing any adversarial sweep. |
| `Docs/MCP_PlayMode_Testing_Strategy.md` | Live-bootstrap testing rules (Rule 1: never fire events via `execute_code`) |
| `Docs/PERF-FOUNDATION.md` | Optimization patterns, anti-patterns, audit findings (read before adding any feature touching per-frame paths) |
| `Docs/PERF-COMBAT-INVESTIGATION.md` | Original combat-perf audit (2026-04) — hypotheses + which were red herrings |
| `qud_decompiled_project/` | In-repo Qud subset (visual effects only) |
| `/Users/steven/qud-decompiled-project/` | Full Qud decompile (core game logic, 5368 files) |

---

## Project specifics (auto-memory)

- **🎮 Genre: RPG, NOT roguelike.** Character persists across save/load.
  Death is recoverable. Pacts/Brands/Marks carry full permanent narrative
  weight. World state persists. See `Docs/PROJECT-IDENTITY.md` for the
  authoritative statement. Anywhere a design appears to assume roguelike
  conventions (permadeath, run-based reset, meta-progression layer), the
  assumption is **wrong** — re-read with RPG framing.
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
