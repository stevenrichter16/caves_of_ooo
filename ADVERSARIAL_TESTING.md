# Adversarial Testing — Methodology, Strategy, and Implementation

> A reusable playbook for designing and executing adversarial test suites.
> Forged in the crucible of *Caves of Ooo*'s skill, rental, and on-hit
> systems; abstracted to be applicable to other games + non-game codebases.

---

## What this document is

A living methodology guide that answers four questions:

1. **What is adversarial testing**, and how does it differ from unit
   tests, counter-checks, integration tests, and fuzzing?
2. **When and why to do it** — what bug classes does it catch that
   the other gates miss?
3. **How to do it** — the bug-class taxonomy, the file pattern, the
   process for triaging findings.
4. **How approaches differ** between writing adversarial tests for
   *new* features (design-review-aligned) vs *existing* features
   (reverse-engineered, regression-pinning).

The goal is that a future engineer (on this project or any other)
can read this document and know how to set up an adversarial test
suite for any feature they ship.

---

## Quick definitions

| Test type | What it asks | When it fails |
|---|---|---|
| **Unit test** | "Does `f(x) == y`?" | The specified behavior diverges. |
| **Counter-check** | "If I flip the precondition, does the test STILL fire RED?" | The original test was vacuous. |
| **Integration test** | "Do components A + B compose correctly?" | Cross-component invariants break. |
| **E2E test** | "Does the full production pipeline produce the expected output?" | Any layer between input and output breaks. |
| **Smoke test** | "Does the system boot without crashing?" | Catastrophic startup failure. |
| **Property-based test** | "Does invariant P hold for ALL inputs in domain D?" | A counterexample is found. |
| **Fuzzing** | "Does the system survive random/malformed input X?" | Crash, corruption, or invariant violation. |
| **Adversarial test** | "What bug class could exist that no per-invariant test would catch?" | A targeted-but-rare bug surfaces, OR a future change inadvertently violates a previously-implicit contract. |

Adversarial sits at the intersection of:
- **Targeted** (not random — the author imagines specific bug classes)
- **Multi-surface** (probes interactions, edge boundaries, and post-conditions)
- **Mutation-resistant** (designed to fail when a future change subtly
  violates the invariant, even if the happy path keeps working)

---

## Why adversarial testing exists

### The four-gate model

A robust feature ships through four gates:

1. **TDD cycle** — RED → GREEN → counter-check (per-invariant; pinned in code-as-spec)
2. **Cold-eye review** — symmetry, cross-feature consistency, doc-vs-impl drift (per-feature, post-tests)
3. **Adversarial sweep** — bug-class taxonomy applied per-feature (this gate)
4. **Live verification** — PlayMode integration, diag stream observation (post-merge)

Each gate catches a distinct bug class:

- **TDD** catches "I built the wrong thing" (spec divergence).
- **Cold-eye** catches "I built the right thing, but it's inconsistent
  with neighbors" (symmetry breaks, naming drift, payload schema
  mismatches).
- **Adversarial** catches "I built the right thing in a consistent
  shape, but it has a *latent* bug nobody asked about" (mid-execution
  state changes, save/load reflection paths, parser malformed inputs).
- **Live verification** catches "the substrate is fine but the
  integration is broken" (UI doesn't render, scene doesn't load,
  abilities don't fire on real keypresses).

### The empirical case

**On-Hit Effects, Caves of Ooo** — `OnHitEffectFactory.cs` source
comment explicitly credits adversarial tests with surfacing the
**Bleeding-`Magnitude`-vs-`DurationTurns` bug**:

> Earlier this used DurationTurns, which has different semantics
> (Bleeding has no fixed Duration — it's indefinite, save-curable).
> Future content authoring a "Bleeding,30,1d3,0,18" spec expects 18
> to be the save-target DC, not "duration". **Latent bug surfaced
> by adversarial tests; no in-game blueprint hits this path today.**

That bug would never have been caught by:
- Unit tests (no test exercised the Bleeding factory path)
- Counter-checks (no spec mismatched the field semantics)
- Cold-eye review (the asymmetry was internal to one switch case)
- Integration tests (no in-game blueprint triggered it yet)

It surfaced because someone wrote an adversarial test that
deliberately fed an unusual spec format to the factory. **One test
prevented a content-time crash a future contributor would have
shipped.**

### The pattern that proves the value

Looking at the *Caves of Ooo* skill / rental / on-hit audits:

| System | Adversarial tests | Real bugs found | Contracts pinned |
|---|---|---|---|
| Skill system | 80 | 1 (SkillRejected gap on pre-WSP8.2 actives) | 24 |
| Rental + Ink | 43 | 0 in main; 3 NUnit syntax errors blocking compile | 12 |
| On-Hit Effects | 21 | 1 (Bleeding-Magnitude bug — fixed pre-this-session) | 8 |

44 contracts pinned + 2 real bugs caught. Even the "0 bugs found"
sweeps weren't wasted: every test is **regression infrastructure**
for the next change to the system.

---

## The bug-class taxonomy — surfaces to probe

This is the canonical checklist. Not every category applies to
every feature, but reading the list forces you to ask "is this
surface present?"

### State surfaces

| # | Surface | What can go wrong |
|---|---|---|
| 1 | **State atomicity** | Multi-step transaction partially completes; one half mutates, the other doesn't, leaving orphaned state. |
| 2 | **Rollback paths** | The "happy path" works but the failure path leaves zombie state. |
| 3 | **Save/load reflection** | Parts without explicit save handlers fall to reflection-based serialization. Object references, generic collections, or non-public fields break silently. |
| 4 | **Mid-execution state changes** | Multi-strike or multi-target loops snapshot state at activation; if the snapshot is unstable (re-evaluated mid-loop), targets that die during the loop break the iteration. |
| 5 | **Cross-actor flows** | Two-party (renter ≠ lessor), three-party (drop-then-pickup-by-other), cross-instance (same blueprint, different IDs). |
| 6 | **Stacking semantics** | Re-applying an effect to a target that already has it — non-stacking (no-op), additive (compound magnitude), or duration-extending (compound time)? Each implementation choice has a different bug shape. |
| 7 | **Multi-instance / blueprint-name matching** | Multiple physical entities sharing a blueprint name. The system might match by ID (one specific entity) or by blueprint (any instance) — both have different correct/incorrect failure modes. |

### Input surfaces

| # | Surface | What can go wrong |
|---|---|---|
| 8 | **Boundary inputs** | Null, empty, zero, negative, max-int across every public API. |
| 9 | **Parser malformed inputs** | Null, empty, whitespace-only, only-delimiters, missing required field, non-numeric where int expected, gibberish, mixed-valid-with-malformed. |
| 10 | **Probability boundaries** | `chance=0` should never fire, `chance=100` should always fire, `chance<0` should be filtered. |
| 11 | **Effect-name normalization** | Case-insensitive (`BURNING` ≡ `burning`), aliases (`fire` → BurningEffect), whitespace trim. |
| 12 | **Conversation/dialogue actions** | `int.TryParse` failure paths, null listener, missing argument, non-existent blueprint name. |

### Logic surfaces

| # | Surface | What can go wrong |
|---|---|---|
| 13 | **Anti-exploit invariants** | Re-rent rejection, sell-vetoed-rentals, return-to-wrong-actor — all the "but what if the player tries..." gates. |
| 14 | **Self-referential gates** | actor == target, ArcaneSurge-style skip-self via Guid match (NOT command-name match), "item already in inventory." |
| 15 | **Cross-system aggregation** | Multiple modifier sources (skills × mutations × items) — does the dispatcher sum them, take max, or short-circuit? |
| 16 | **Cooldown / re-fire** | If the cooldown gate is bypassed and a buff-firing skill is re-fired during its own buff window, do charges overwrite (correct) or accumulate (bug)? |
| 17 | **Duplicate add/remove** | AddSkill twice → false; RemoveSkill on unowned → false (idempotent). |

### Observability surfaces

| # | Surface | What can go wrong |
|---|---|---|
| 18 | **Diag dispatch invariants** | Internal rejection emits BOTH `CommandRouted` AND `SkillRejected`; cooldown emits ONLY `CommandRejected`; non-matching events early-out. The contract is rich and easy to break with a "simplification." |

---

## Strategies — how to design adversarial tests

### Universal strategy: the surface audit

For any feature, do this audit:

1. **List the public API.** Every method, event, JSON-driven config,
   conversation action, dialogue verb.
2. **For each entry in #1, walk the bug-class taxonomy.** Which of
   the 18 surfaces does this surface touch?
3. **For each surface that applies, write 1-3 tests.** Group them
   under a section banner in the test file.
4. **Add cross-surface tests.** Some bug classes only manifest at
   intersections (e.g., save/load DURING a mid-execution state).

If 4+ surfaces apply → you've found a feature that warrants the
gate. Write the suite.

### Strategy A — for **new features** (write while building)

Adversarial tests for new features are written **alongside** the
implementation. The author has full mental model; bugs are
*prevention-focused* rather than discovery-focused.

**Workflow:**
1. As you implement each path (`if X then Y else Z`), ask:
   *"What's the adversarial input that breaks this branch?"* Write
   the test before moving on.
2. As you add cross-system hooks (event subscriptions, modifier
   aggregations, save serialization), write *one* adversarial test
   per hook: "what invariant am I claiming holds across this hook?"
3. As you compose the feature, write *interaction* tests:
   "feature × neighbor-feature in this combination produces this
   composed behavior."
4. **Bias toward "would this fail if I shipped a regression?"** —
   the goal is regression infrastructure, not coverage padding.

**Distinct advantages:**
- You can shape the implementation to be adversarial-test-friendly
  (expose internals, parameterize for testability).
- You catch the bug class as design intent solidifies, before the
  spec ossifies.
- The test names and intent comments document the design rationale.

**Distinct risks:**
- You only test bug classes you imagine. Document the *unprobed*
  surfaces explicitly so a future audit knows what was skipped.
- Tests written too early can pin half-baked design choices that
  later need to change — separate "behavior I'm sure about" from
  "behavior I'm testing the current shape of" via comment.

### Strategy B — for **existing features** (write after the fact)

Adversarial tests for existing features are *reverse-engineered*.
The design intent is implicit in the code; you have to infer it.

**Workflow:**
1. **Read the source.** Map every branch, every public method,
   every event subscription. Don't trust the docstring — trust the
   `if`s.
2. **Infer the design intent.** What contract is this implementation
   trying to uphold? Write it down in comments.
3. **Survey the bug-class taxonomy** against the inferred contract.
   *Pay extra attention to surfaces #3, #4, #6, #15, #16* —
   these are the surfaces that accumulate the most assumptions
   over time, and the most likely to have latent bugs in code
   that "just works."
4. **Read existing tests carefully.** Distinguish tests that pin
   the *intent* from tests that pin the *current implementation*
   — the former are real specs, the latter might be regressions
   waiting to be reclassified.
5. **Write the suite.** Treat each test as a "this is what I
   inferred the contract to be" claim. Be ready to discover that
   your inference was wrong (and that's the bug).

**Distinct advantages:**
- The system is battle-tested by real usage; bugs that exist are
  the *latent* ones that haven't bitten yet — exactly what
  adversarial gates catch best.
- Save/load, cross-version compatibility, dependent-system
  invariants all have years of accumulated assumptions to probe.

**Distinct risks:**
- **You may pin existing bugs as "intended behavior."** Counter
  this by reviewing each test's expected outcome against the
  feature's design doc (if one exists) or by asking the original
  author.
- **The surface area is bigger.** A new feature has a small,
  bounded surface; an existing one has integrated with everything.
  Budget more tests (40-60+ vs 20-30).
- **Reflection-based serialization is the highest-bug-yield
  surface for existing code.** When `Part` types lack explicit
  save handlers, the catch-all reflection path silently breaks
  for object-reference fields, generic collections, and
  non-default-constructible types. **Always probe save/load on
  existing features.**

### Strategy C — when you're not sure if a feature qualifies

A feature warrants a dedicated adversarial sweep when 2+ of these apply:

- Touches state atomicity (multi-step transactions)
- Has a parser (string → spec, JSON-driven)
- Has cross-actor flows
- Has stacking semantics
- Has save/load reach
- Has anti-exploit gates
- Has probabilistic / RNG-gated behavior
- Has diag emission contracts

If only 1 surface applies, the per-sub-milestone counter-checks are
probably enough. If 4+ apply, plan for 40-60 tests.

---

## Implementation pattern

### File structure

```
Assets/Tests/EditMode/.../<Feature>AdversarialTests.cs
```

For larger systems, use a versioned naming pattern:
- `<Feature>AdversarialTests.cs` — first wave
- `<Feature>DeepAdversarialTests.cs` — second wave (after first wave found 0 bugs but new surfaces emerged)

### Class structure

```csharp
namespace <Project>.Tests
{
    /// <summary>
    /// Adversarial tests for the <Feature> system.
    /// Targets bug classes the per-method happy-path tests miss:
    /// <list type="bullet">
    ///   <item>State atomicity (Ink + inventory transfer atomic)</item>
    ///   <item>Anti-exploit invariants (re-rent rejection)</item>
    ///   <item>Refund precision (floor math edge cases)</item>
    ///   ...
    /// </list>
    /// </summary>
    public class <Feature>AdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset relevant globals: Diag.ResetAll(),
            // MessageLog.Clear(), <Registry>.ResetForTests()
        }

        // ── Fixture helpers (mirror the per-feature test file) ──────────

        private static Entity CreateActor(...) { ... }

        // ════════════════════════════════════════════════════════════════
        // A. <Bug Class Category>
        //   <One-paragraph explanation of the surface being probed>
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_<Specific Behavior>_<Expected Result>()
        {
            // Setup that triggers the bug class
            ...

            // Action
            ...

            // Assertion with intent comment
            Assert.IsFalse(result,
                "<What the assertion proves>. <What a buggy impl would do>.");
        }
    }
}
```

### Naming conventions

- **File:** `<Feature>AdversarialTests.cs`
- **Class:** `<Feature>AdversarialTests` (matches file name)
- **Method:** `Adversarial_<Specific>_<EdgeCase>_<ExpectedBehavior>`
  - Examples:
    - `Adversarial_TryRent_InsufficientInk_NoStateMutated`
    - `Adversarial_Whirlwind_TargetDiesOnFirstSwing_OtherTargetsStillHit`
    - `Adversarial_Parse_MalformedFollowedByValid_KeepsValid`
- **Section banners:** `// ════════════` boxes separating bug-class categories

### Code-level patterns proven useful

#### Pattern 1: Snapshot stability

Tests that a multi-target operation iterates a stable snapshot,
even when the snapshot's contents change mid-loop.

```csharp
[Test]
public void Adversarial_Whirlwind_TargetDiesOnFirstSwing_OtherTargetsStillHit()
{
    var atk = MakeBodied("atk");
    Equip(atk, MakeWeapon("axe", "2d8+5", "Cutting Axe"));
    var fragile = MakeBodied("fragile", hp: 1);   // dies on swing 1
    var sturdy = MakeBodied("sturdy", hp: 200);   // must still be hit

    skill.OnCommand(ctx);

    Assert.LessOrEqual(fragile.GetStatValue("Hitpoints"), 0);
    Assert.Less(sturdy.GetStatValue("Hitpoints"), sturdyHpBefore,
        "Sturdy must STILL be struck — Whirlwind's snapshot must "
        + "be stable across mid-loop deaths.");
}
```

#### Pattern 2: Save/load round-trip

Helper method that reflects an entity through the production save
pipeline. Catches the implicit-reflection-serializer bug class.

```csharp
private static Entity RoundTripEntity(Entity src)
{
    using var stream = new MemoryStream();
    var writer = new SaveWriter(stream);
    SaveGraphSerializer.SaveEntityBody(src, writer);
    stream.Position = 0;
    var reader = new SaveReader(stream, factory: null);
    var loaded = new Entity();
    SaveGraphSerializer.LoadEntityBody(loaded, reader);
    return loaded;
}

[Test]
public void Adversarial_SaveLoad_RentalPartFields_RoundTrip()
{
    var item = CreateRentalWeapon();
    item.AddPart(new RentalPart { InkPaid = 17, LessorBlueprintName = "X" });
    var loaded = RoundTripEntity(item);
    var loadedRental = loaded.GetPart<RentalPart>();
    Assert.AreEqual(17, loadedRental.InkPaid);
    Assert.AreEqual("X", loadedRental.LessorBlueprintName);
}
```

#### Pattern 3: Force-trial probability

Run the same operation across many seeds to verify probabilistic
outcomes are correctly bounded.

```csharp
[Test]
public void Adversarial_ZeroDamage_NoClassHooksFire()
{
    var defender = MakeCreature("def");
    var damage = new Damage(10);
    damage.AddAttribute("Bludgeoning");

    // 100 trials at 100% chance — verify NONE fire because
    // actualDamage is 0.
    for (int seed = 0; seed < 100; seed++)
    {
        OnHitClassEffects.Apply(damage, actualDamage: 0, defender,
            attacker: null, zone: null, rng: new Random(seed));
    }
    Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
        "actualDamage=0 must veto all class hooks. If this fails, "
        + "the contract is broken.");
}
```

#### Pattern 4: Multi-attribute coverage

Tests that combinations of attributes/flags/conditions all fire
their respective hooks independently.

```csharp
[Test]
public void Adversarial_MultiAttributeWeapon_FiresAllMatchingClassHooks()
{
    var damage = new Damage(10);
    damage.AddAttribute("Cutting");
    damage.AddAttribute("Piercing");

    int bleedCount = 0, confusedCount = 0;
    for (int seed = 0; seed < 100; seed++)
    {
        var d = MakeCreature("d" + seed);
        OnHitClassEffects.Apply(damage, actualDamage: 5, d, null, null, new Random(seed));
        var sep = d.GetPart<StatusEffectsPart>();
        if (sep.HasEffect<BleedingEffect>()) bleedCount++;
        if (sep.HasEffect<ConfusedEffect>()) confusedCount++;
    }
    Assert.Greater(bleedCount, 5, "Cutting class hook fires...");
    Assert.Greater(confusedCount, 0, "Piercing class hook fires...");
}
```

#### Pattern 5: Diag dual-emission verification

Tests that the diag stream contains the right *combination* of
records — not just one record but the full set.

```csharp
[Test]
public void Adversarial_Diag_SkillInternalRejection_EmitsBOTH_CommandRoutedAND_SkillRejected()
{
    Diag.ResetAll();
    var cmd = GameEvent.New("CommandSlam");
    cmd.SetParameter("Zone", zone);
    cmd.SetParameter("RNG", new Random(0));
    atk.FireEvent(cmd);

    var routed = DiagQuery.Apply(new DiagQuery.Filter
        { Category = "skill", Kind = "CommandRouted" }).Records;
    var rejected = DiagQuery.Apply(new DiagQuery.Filter
        { Category = "skill", Kind = "SkillRejected" }).Records;

    Assert.AreEqual(1, routed.Count,
        "CommandRouted MUST fire even when the skill internally bails.");
    Assert.AreEqual(1, rejected.Count,
        "SkillRejected MUST fire from the skill's internal gate.");
}
```

---

## Process

### Pre-condition: tests-green + cold-eye-pass-complete

Don't run an adversarial sweep until:
1. Per-feature tests are GREEN (TDD cycle complete)
2. Cold-eye review pass is complete (Q1-Q4 from the symmetry/
   consistency/counter-check/doc-vs-impl checklist)

The adversarial sweep finds bugs the *previous* gates miss — running
it before those gates mixes their findings.

### Step-by-step

1. **Audit the bug-class taxonomy.** Walk the 18 surfaces above.
   Mark which apply to your feature. 2+ → run the gate. 4+ →
   plan for 40-60 tests.
2. **Map the surfaces to test categories.** Group by surface.
   Each section banner in the test file = one bug-class category.
3. **Write the test file.** Use the file/class/method naming
   conventions above. Comment intent ("if a future change suppresses
   X, this test fails with reason Y").
4. **Run the suite.** Investigate every failure individually.
5. **For each failure:** is this a real bug, or is your test wrong?
   - **Real bug:** file `fix/<feature>-adversarial-finding-N`,
     single self-contained fix-commit. Update the source comment
     to credit the adversarial test (so future readers know how it
     was caught).
   - **Test wrong:** revise the test. Ensure the revision still
     probes the bug class — don't just weaken the assertion.
6. **Document the result in the merge commit:**
   - "Adversarial sweep complete: N tests, M bugs found and fixed."
   - "Adversarial sweep complete: N tests, 0 bugs found. Surfaces
     probed: <list>. Surfaces deferred: <list>."

### Triage rubric for findings

When an adversarial test fails, classify the failure:

| Classification | Meaning | Action |
|---|---|---|
| 🔴 **Hard bug** | Crash, data corruption, save/load break | Fix immediately, blocking. |
| 🟡 **Latent bug** | Works today but a future change is likely to break it | Fix now, document the brittleness. |
| 🔵 **Design ambiguity** | Test exposes that the spec doesn't fully define the behavior | Decide the intended behavior with the team, then update either the test or the impl + a doc. |
| 🟢 **Test wrong** | Test's expected outcome doesn't match the (correct) intended behavior | Revise the test; ensure it still probes the bug class. |
| ⚪ **Test redundant** | Test is a duplicate of a counter-check | Remove or consolidate. |

---

## Case studies (Caves of Ooo)

### Case 1: Skill system — 80 adversarial tests, 1 bug found

**Surfaces probed:** mid-execution state changes, effect stacking,
cross-skill aggregation, self-referential gates, diag invariants,
boundary inputs, state purity.

**Real bug surfaced:** the audit found that 5 pre-WSP8.2 actives
(Slam, Conk, Berserk, HookAndDrag, Shank) didn't emit the
`SkillRejected` diag record on rejection paths — asymmetric with
the 19 newer actives that did emit. Caught by a direct symmetry
probe across all 24 actives.

**Fix shipped:** added `EmitSkillRejectedDiag` calls at every
rejection path in the 5 actives, plus 10 new E2E tests verifying
the diag flow.

**Contracts pinned:** 24 — including snapshot stability for
Whirlwind, multi-Cudgel-passive composition, dispatch dual-emission
invariants, RootedEffect's AllowMovement-only semantic.

### Case 2: Rental system — 43 adversarial tests, 0 bugs in main

**Surfaces probed:** state atomicity, anti-exploit, refund
precision, equip-then-return, multi-rental isolation, save/load
reflection, conversation actions, cross-actor flows, multi-village.

**Real bug surfaced:** none in the rental system itself, but the
audit incidentally found 3 NUnit overload-resolution errors in
`RentalSystemTests.cs` (`Does.Contain` ambiguity with strings)
that were preventing the full test suite from compiling. Fixed
those during the audit.

**Contracts pinned:** 12 — including `RentalPart.InkPaid` +
`LessorBlueprintName` round-trip via reflection (no explicit save
handler exists; the test proves the catch-all reflection path
works), `CanBeTraded` veto via `HandleEvent`, cross-village
blueprint matching by design.

### Case 3: On-Hit Effects — 21 adversarial tests, 1 historical bug already fixed

**Surfaces probed:** class-hook + per-weapon stacking, multi-attribute
weapons, vetoed-hit gate, probability boundaries, parser malformed
inputs, factory case+alias resolution, stacking semantics, null safety.

**Real bug surfaced (historical):** The Bleeding factory case had
been using `DurationTurns` as the save-target DC, but the spec
contract intended `Magnitude` to be the DC. A future content
author writing `"Bleeding,30,1d3,0,18"` would have shipped silently
broken behavior — saved by an adversarial test that fed that exact
shape to the factory. **No in-game blueprint hits this path today**
— the bug was caught before any user content depended on it.

**Lesson:** adversarial tests are *especially* valuable when the
feature is content-extensible. The bug class isn't "does the shipped
code work" — it's "does the shipped code SAFELY ACCEPT the shapes
content authors will write tomorrow?"

---

## Anti-patterns to avoid

### 🚫 Coverage padding

Writing 50 tests that all probe the same bug class. The taxonomy is
your guide — distribute tests across categories.

### 🚫 Pinning current implementation as if it's intent

For existing features, every test should reflect *intended
behavior*, not *what the code happens to do*. Verify with the
design doc or the original author. Misuse: "the code does X, so
my test asserts X" — even if X is a latent bug.

### 🚫 Tests that pass because the precondition is vacuous

If your test's setup never triggers the path you're testing, it
passes for the wrong reason. Counter-checks (CLAUDE.md §3.4) prevent
this for per-invariant tests; adversarial tests need the same
discipline. Example: "test that flanking gives bonus damage" — make
sure the setup actually creates the flank position, don't just hope
it does.

### 🚫 Coupling adversarial tests to fixture details

If your adversarial test breaks every time the fixture changes a
trivial detail (rename a field, change a default value), it's
brittle. Use the same fixture helpers as the per-feature tests so
fixture-level changes don't cascade.

### 🚫 Ignoring "0 bugs found" as a result

Documenting "0 bugs found, surfaces probed: <list>" is more
valuable than just "tests pass" because it tells future readers:
- What contracts are pinned
- What surfaces are NOT pinned (deferred)
- Where to add tests if a new bug class is suspected

### 🚫 Treating adversarial tests as replacement for fuzzing

Adversarial tests are bounded by the author's bug-class imagination.
Fuzzing finds bugs the author didn't imagine. Both have value;
adversarial is *cheaper and faster*, fuzzing is *more thorough*.
Use both at different cadences.

---

## Honesty bounds

Document these in every adversarial-sweep summary:

> **Bug-class limit.** Adversarial tests are bounded by the surfaces
> the author imagines. 0 bugs found does NOT prove zero bugs — it
> proves zero bugs in the *probed* surfaces. Truly novel bug classes
> require fuzzing or property-based testing.
>
> **Verification limit.** PlayMode integration via `execute_code` is
> read-only state observation; it cannot fully replicate user input
> (keypress capture, frame timing, render integration). The
> substrate may be verified while the user-experience layer remains
> untested.
>
> **Time limit.** Adversarial sweeps target catching bugs at
> ship-time. Bugs that emerge from feature interactions added LATER
> require either (a) re-running the sweep on the new combined
> surface or (b) adding integration tests at the new boundary.

---

## Reusability for other projects

Most of this document generalizes. Concretely:

### What generalizes

- **The four-gate model** (TDD, cold-eye, adversarial, live)
- **The bug-class taxonomy** (atomicity, parsers, save/load, cross-actor,
  stacking, etc.) — these surfaces exist in any non-trivial system
- **The file/class/method naming conventions**
- **The 5 code patterns** (snapshot stability, save/load round-trip,
  force-trial probability, multi-attribute coverage, dual-emission
  verification)
- **The triage rubric** (🔴/🟡/🔵/🟢/⚪)
- **The honesty bounds**
- **The anti-patterns**

### What's Caves of Ooo / Unity-specific

- The **Diag system** for dispatch invariants. Translate to your
  project's equivalent (event log, structured logging, tracing).
- **`SaveGraphSerializer`** for save/load round-trip. Translate to
  your serialization framework (JSON, BSON, Protobuf, custom).
- **`Entity`/`Part`** ECS-style architecture. Translate to your
  component model.
- **Unity test runner conventions** (`[Test]`, `Assert.That`,
  `[SetUp]`). Translate to your test framework.
- **Specific bug-class examples** (`SkillRejected` diag, `RentalPart`
  reflection, `Whirlwind` snapshot). Use as templates for your
  domain's equivalents.

### Adoption checklist for a new project

1. Adopt the 4-gate model in your contributor docs.
2. Translate the 18-surface taxonomy to your project's idioms.
3. Establish a `<Feature>AdversarialTests.<ext>` file convention.
4. Adopt the `Adversarial_*` test naming prefix.
5. Establish the triage rubric.
6. Run an initial adversarial sweep on your most-recently-shipped
   non-trivial feature. Document the result.
7. Make the sweep a CI gate for any PR adding 2+ taxonomy surfaces.

---

## Living document

This document is meant to evolve. When you discover a new bug class
that the existing 18 surfaces don't capture, **add it here**. When
you find a code pattern that produces unusually high bug-yield per
test, **document it as a Pattern N**. When an adversarial sweep
catches a particularly interesting bug, **add it as a case study**.

The value of this methodology compounds across every adversarial
sweep that uses + extends it.

---

## Appendix: minimum-viable adversarial sweep

If you're short on time and need to run *some* adversarial coverage
on a feature, the absolute minimum is:

1. **One test per top-priority surface** that the feature touches.
   For most features that's 4-8 tests — enough to catch the
   highest-yield bug classes (state atomicity, save/load,
   cross-actor flows).
2. **Include a save/load round-trip test** if the feature has any
   serializable state. This single test has the highest bug-yield
   per minute of authoring.
3. **Include a parser malformed-input test** if the feature has any
   string-parsed configuration. Use null + empty + gibberish.
4. **Document what you skipped.** "Adversarial sweep deferred:
   stacking semantics, anti-exploit gates, RNG boundaries —
   schedule for follow-up."

The minimum-viable sweep catches roughly 40-60% of the bugs a full
sweep would catch, in roughly 15% of the time.

---

*End of document. Maintained alongside `CLAUDE.md` §"Adversarial
test sweep." Last revised after the WSP8.4 audits of the Caves of
Ooo skill, rental, and on-hit effects systems.*
