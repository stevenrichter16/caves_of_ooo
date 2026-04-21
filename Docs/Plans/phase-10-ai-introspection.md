# Phase 10: AI Debug / Introspection Tooling — Implementation Plan

## Context and scope

Qud's debugging workflow for "what is this NPC thinking?" rests on three cheap primitives:

1. **`Brain.Think(string)`** — a single-slot `LastThought` assignment (no ring buffer), optionally echoed to the player message queue when the NPC has a `ThinkOutLoud` int property set.
2. **`GoalHandler.GetDescription()` / `GetDetails()`** — `GetDetails()` returns `null` by default; `GetDescription()` defaults to `GetType().Name + (": " + details if non-null)`. Only three goals in the entire decompiled source actually override these (`Kill`, `Step`, `Command`), suggesting the feature is low-touch: overrides added on the fly when debugging a specific behavior.
3. **A debug-internals inspector** — Qud uses an existing general-purpose `GetDebugInternalsEvent` listener on `Brain` that renders the goal stack top-down, collapsing consecutive duplicate descriptions with `x2` / `x3`, alongside `LastThought`, `PartyLeader`, kill-radii, etc.

Caves of Ooo already has a **`LookSnapshot` + `LookOverlayRenderer`** pipeline (`Assets/Scripts/Gameplay/Look/`) that surfaces HP, relation label, and flags for the cell the look cursor is on. The inspector fits naturally as an extra panel that fills in only when the cursor is on a `Creature`-tagged entity *and* a debug toggle is on.

The scope is: mirror Qud's three primitives with minimal ceremony, wire them into the existing look-mode pipeline, and stop. This is plumbing, not a new system.

### Honest scope divergence

The three deliverables are **not equal**:
- **`Brain.Think`**: ~20 lines of code + tiny test. One commit, ~2 hours.
- **`GetDescription` / `GetDetails` base + overrides**: base is 10 lines; the 17 overrides are individually trivial but add up. One commit for the base API + defaults; one commit for "bulk overrides across all goals." ~3-4 hours total.
- **Goal-stack inspector UI**: this is the real work. Requires extending `LookSnapshot` (or piggybacking on sidebar focus), adding a toggle, threading it through `SidebarStateBuilder` or `LookQueryService`, and rendering on the sidebar or a new overlay tilemap. ~4-6 hours even if we reuse the sidebar focus slot. This is **~60 % of total effort**.

Plan accordingly: Think lands first and cheap; the overrides are mechanical; budget the bulk of review attention on the inspector.

## Verification sweep (corrections to initial assumptions)

Applying QUD-PARITY §Methodology Part 1.2 — here's what the brief got wrong vs. what the live code says:

1. **Qud does NOT use a ring buffer for Think.** The brief lists "N = 10 ring buffer" as one option. Qud's `Brain.cs:222` has a single `public string LastThought` and `Brain.Think(string Hrm)` simply does `LastThought = Hrm;`. The inspector in `GetDebugInternalsEvent` (line 2249) prints `LastThought.IsNullOrEmpty() ? "none" : LastThought`. **Recommend: match Qud exactly — single `LastThought`, no ring buffer.** Ring buffers are a v2 feature if ever needed.

2. **`GetDescription` is not abstract, and it is called.** Qud's `GoalHandler.GetDescription()` builds `GetType().Name + (": " + GetDetails() if non-null)`. The brief's option "make it abstract to force implementations" should be explicitly rejected: Qud's own pattern is *extend as needed*, and only three of ~40 Qud goal classes override it. We should match.

3. **`GetDetails()` returns a plain string in Qud, not a list or struct.** Qud's pattern from `Step.cs:43-58` is "append to a static `StringBuilder`, return `.ToString()`." No tabular key/value. **Recommend: single-line `string` with in-string separators (`" | "`) — keep it simple.** Don't prematurely invent `GoalDetail { Key, Value }`.

4. **Goal-stack inspector already exists in Qud as part of `GetDebugInternalsEvent`.** It's not a dedicated UI — it's a debug event handler that also emits `PartyLeader`, `MinKillRadius`, etc. We don't need to match that full event; we just need the goal-stack formatting (including `x2` run-length collapsing, which Qud does).

5. **The brief lists "L" as the Look key at ~line 322.** Actual line: 357 (`InputHelper.GetKeyDown(KeyCode.L)` → `EnterLookMode()`). Look-mode input handling is at `HandleLookModeInput` starting at line 1295. Minor detail, but confirms the plan hooks into the right code path.

6. **There's no `DebugSettings` ScriptableObject yet.** The brief speculates one might exist; I confirmed via `Glob` that nothing matches `DebugSettings*.cs`. The `#if DEBUG` / `UNITY_EDITOR` pattern is barely used in this project (`Grep` found it in only 2 files outside test code). We should **not** introduce a ScriptableObject; a static bool in a `Diagnostics` type is consistent with `PerformanceDiagnostics`.

7. **`LookSnapshot.DetailLines` caps at 2 lines.** `LookOverlayRenderer.cs:48` loops `i < 2`. If the inspector reuses the look overlay, we hit a limit. Better option: put the goal-stack panel on the *sidebar* (`SidebarSnapshot` has unconstrained log entries) rather than the tight look overlay.

8. **There IS a scenario for PetGoal: `VillageChildrenPetting.cs`.** The brief calls it out — confirmed it exists and spawns a `VillageChild` that will emit PetGoal on a probability gate. Usable as the v1 integration test exemplar. Note the scenario description warns PetGoal fires only ~every 20 turns, so a scenario test will need `ctx.AdvanceTurns(50)` or similar.

9. **`MessageLog` already has a `TickProvider` hook and tests `Clear()` it between runs.** If we do route Think through MessageLog-when-flag-set, the tick stamping is free. But see recommendation below — don't put Think through MessageLog.

10. **`ThinkWhileInZone` doesn't appear in Qud's `Brain.cs`.** `Grep` for `ThinkWhileInZone` in `/qud_decompiled_project` returns nothing. The brief speculates it might exist — it doesn't. Skip.

## Design decisions (resolving the brief's questions)

1. **Where does `Think(string)` go?** → Single-slot `LastThought` on `BrainPart`, mirroring Qud exactly. An optional `ThinkOutLoud` boolean on `BrainPart` (default false) routes a copy to `UnityEngine.Debug.Log` with tag `[Think:<entityName>]` when set. *No ring buffer, no MessageLog integration.* The inspector reads `LastThought` directly.

2. **No-op in release?** → `Think` is NOT conditional-compiled. It's a single field assignment (no allocation unless `ThinkOutLoud` is on) and we want to be able to flip `ThinkOutLoud` via a scenario or debug setting at runtime, which requires the method to exist in release. Qud does the same: `Think` exists unconditionally. The *Debug.Log echo* is gated by the runtime `ThinkOutLoud` flag, which is off by default.

   Hot path cost: `Think` is called inside `GoalHandler.TakeAction`, which runs in the `AiTakeTurn` profiler marker. Cost is one string-ref write per call; no allocation unless `ThinkOutLoud` composes the `$"[Think:{name}] {msg}"` format string. Acceptable. The expensive formatting sits behind a `if (ThinkOutLoud)` gate.

3. **`GetDescription` default?** → Non-abstract, defaults to Qud's pattern: `GetType().Name + (":" + GetDetails() if details non-null)`. Makes every goal inspectable without implementing anything. Explicit overrides land incrementally.

4. **`GetDetails` format?** → `string` returning `null` by default, overridden with a one-line summary. Multi-field goals join with `" | "`. Do NOT introduce `List<string>` or a `GoalDetail` struct. Private fields on goals (`_walkAttempts`, `_phase`) get read by `GetDetails()` directly — they're on the same class, so no visibility changes needed.

5. **Inspector trigger?** → **Already-working path:** hovering in look mode on a `Creature`-tagged entity. We don't add a new key or menu; we enrich `LookSnapshot` with an optional `GoalStackLines` field, and show it in the sidebar focus panel when the debug toggle is on. No new binding; no action-menu entry; no production-player exposure.

6. **Inspector UI layout?** → **Render into the sidebar's focus panel** (the area below vitals where the current look target is summarized). Sidebar is already variable-width and has no 2-line detail cap. Focus block content when debug toggle is on and the target is a Creature with a BrainPart:
   - Line 1: `[X,Y] Name` (already present — `LookSnapshot.Header`)
   - Line 2: `HP xx/yy | relation` (already present — `BuildPrimaryDetail`)
   - **NEW:** `Goals:` header
   - **NEW:** goal lines bottom-to-top (oldest at bottom, topmost = innermost, matching Qud's convention) with run-length collapsing (`BoredGoal`, `MoveToGoal: (44,11) x2`)
   - **NEW:** `Thought: <LastThought or "none">`

   No corner HUD, no new overlay tilemap, no centered popup. Reuse what's there.

7. **Release/editor gating?** → A `static bool AIInspectorEnabled` on a new `CavesOfOoo.Diagnostics.AIDebug` class (sibling of `PerformanceDiagnostics`). Default `false` (so production build looks unchanged). Flip it via:
   - A new scenario `InspectAIGoals` that sets it true in `Apply()`.
   - `UNITY_EDITOR`-only menu item `Tools/Caves of Ooo/AI/Toggle Goal Inspector` (one-line menu, no new settings asset).
   - A console toggle later if wanted.

   When off, `LookQueryService` does not populate `GoalStackLines`, so there's literally zero rendering cost.

   *If a player ever sees it:* the Think strings will look like dev jargon ("No hostiles in sight radius"). That's fine for v1 — it's editor-gated.

8. **`ThinkWhileInZone` or variants?** → Does not exist in Qud. Skip.

9. **Testing:** new `ScenarioVerifier` extensions for goal-stack + last-thought verification; scenario test using `VillageChildrenPetting`; counter-check test that the inspector is empty for a `Creature`-less cell.

10. **Scope cuts explicitly marked:**
    - **v1 (this plan):** `Brain.LastThought` + `Think()` + `ThinkOutLoud`. `GetDescription`/`GetDetails` base + overrides on the 6 most-visible goals (Kill, MoveTo, GoFetch, Flee, Retreat, Command). Sidebar focus enhancement gated by `AIDebug.AIInspectorEnabled`.
    - **Deferred (v2):** overrides on the remaining 11 goals. Think ring buffer. History playback. `GetDebugInternalsEvent` parity (party leader, kill radii, allegiances). Filter/search on Think log. Click-to-pin inspector.

## Approach (per deliverable)

### Deliverable 1 — `Brain.Think(string)`

In `Assets/Scripts/Gameplay/AI/BrainPart.cs`, add:

```csharp
// --- Debug Introspection (Phase 10) ---

/// <summary>Most recent thought string set by a goal handler. Null until first Think().</summary>
public string LastThought;

/// <summary>When true, every Think() call is echoed to UnityEngine.Debug.Log.</summary>
public bool ThinkOutLoud;

/// <summary>
/// Record a debug thought for this NPC's current tick.
/// Mirrors Qud's Brain.Think: single-slot LastThought + optional console echo.
/// Safe to call on every goal tick — no allocation unless ThinkOutLoud is on.
/// </summary>
public void Think(string thought)
{
    LastThought = thought;
    if (ThinkOutLoud && thought != null)
    {
        string name = ParentEntity?.GetDisplayName() ?? "?";
        UnityEngine.Debug.Log($"[Think:{name}] {thought}");
    }
}
```

In `GoalHandler.cs` add a shim so goals can call `Think("...")` directly:

```csharp
// --- Debug ---

/// <summary>Record a thought on the parent brain. Mirrors Qud's GoalHandler.Think.</summary>
protected void Think(string thought) => ParentBrain?.Think(thought);
```

Nothing else changes. Goals can then sprinkle `Think("...")` calls wherever they want. The minimal seed pass (Deliverable 2's commit) adds 1-2 Think calls in each of the ~6 goals we override anyway.

### Deliverable 2 — `GetDescription()` and `GetDetails()`

In `GoalHandler.cs` add two virtuals to the base (staying close to Qud):

```csharp
// --- Inspector-friendly descriptions ---

/// <summary>
/// One-liner for the goal-stack inspector. Default format:
///   "TypeName"          (GetDetails() == null)
///   "TypeName: details" (GetDetails() != null)
/// Override via GetDetails() for state-specific strings; override this only when
/// the default "Type: Details" shape is wrong.
/// </summary>
public virtual string GetDescription()
{
    string details = GetDetails();
    string typeName = GetType().Name;
    return string.IsNullOrEmpty(details) ? typeName : $"{typeName}: {details}";
}

/// <summary>
/// State summary for this goal (target coords, phase, counter values).
/// Default: null. Override to surface interesting runtime state to the inspector.
/// </summary>
public virtual string GetDetails() => null;
```

Then the first wave of overrides (six most-visible in a test run):

- `KillGoal.GetDetails()` → `Target == null ? null : $"target={Target.GetDisplayName()}"`
- `MoveToGoal.GetDetails()` → `$"to=({TargetX},{TargetY}) age={Age}/{MaxTurns}"` (Age cap is informative)
- `GoFetchGoal.GetDetails()` → `$"phase={_phase} attempts={_walkAttempts}/{MaxWalkAttempts} item={Item?.GetDisplayName()}"`
- `FleeGoal.GetDetails()` → `$"from={FleeFrom?.GetDisplayName()} age={Age}/{MaxTurns}"`
- `RetreatGoal.GetDetails()` → `$"phase={_phase} waypoint=({WaypointX},{WaypointY}) age={Age}/{MaxTurns}"`
- `CommandGoal.GetDetails()` → `Command`

For `BoredGoal`, `WaitGoal`, `WanderRandomlyGoal`, `WanderGoal`, `StepGoal`, `NoFightGoal`, `DormantGoal`, `PetGoal`, `GuardGoal`, `FleeLocationGoal`, `WanderDurationGoal`, `DelegateGoal` — defer to v2. The default `GetDescription()` ("BoredGoal", "WaitGoal") is perfectly readable.

Also inject 1-2 `Think()` calls into the six goals above during the same commit, matching Qud's style:

- `KillGoal.TakeAction()`: first `Think($"I'm going to kill {Target.GetDisplayName()}.");` on adjacency, `Think("can't reach, pathing");` on the non-adjacent branch.
- `FleeGoal.TakeAction()`: `Think($"fleeing from {FleeFrom?.GetDisplayName()}");`.
- `MoveToGoal`: `Think("path blocked, replanning");` on the blocked-step branch.
- `GoFetchGoal.WalkToItem`: `Think($"walking to {Item?.GetDisplayName()}");` and `Think("giving up on fetch — max attempts");` in the bail branch.
- `RetreatGoal.Recover`: `Think("recovering at safe spot");`.
- `CommandGoal`: `Think($"executing command {Command}");`.

Each of these is a one-liner and doesn't change any behavior — tests should stay green.

### Deliverable 3 — Goal-stack inspector UI

**New type** `Assets/Scripts/Shared/Utilities/AIDebug.cs`:

```csharp
namespace CavesOfOoo.Diagnostics
{
    /// <summary>
    /// Runtime toggles for AI debug/introspection. All default off so production
    /// renders unchanged. Toggled by scenarios, editor menu items, or console.
    /// </summary>
    public static class AIDebug
    {
        /// <summary>When true, LookQueryService emits a goal-stack summary in LookSnapshot.</summary>
        public static bool AIInspectorEnabled;
    }
}
```

**Extend `LookSnapshot`** with an optional field (nullable — null means "inspector off or not applicable"):

```csharp
public IReadOnlyList<string> GoalStackLines { get; }    // NEW, nullable
public string LastThought { get; }                       // NEW, nullable
```

Update the constructor to accept them (or add an overload for backwards compatibility — existing LookQueryService builds will simply pass null).

**Extend `LookQueryService.BuildSnapshot`** to populate these when:
- `AIDebug.AIInspectorEnabled == true`, AND
- `primary != null && primary.HasTag("Creature")`, AND
- `primary.GetPart<BrainPart>() != null`.

New private helper in `LookQueryService`:

```csharp
private static void BuildBrainInspection(
    Entity creature,
    out List<string> goalLines,
    out string lastThought)
{
    goalLines = null;
    lastThought = null;
    if (creature == null) return;
    var brain = creature.GetPart<BrainPart>();
    if (brain == null) return;

    lastThought = string.IsNullOrEmpty(brain.LastThought) ? "none" : brain.LastThought;

    int count = brain.GoalCount;
    if (count == 0) return;

    goalLines = new List<string>(count);
    // Walk top-down so the first line is the currently-executing goal.
    // Collapse consecutive identical descriptions with "xN" (matches Qud).
    int i = count - 1;
    while (i >= 0)
    {
        var goal = brain.PeekGoalAt(i);   // NEW accessor — see below
        string desc = goal.GetDescription();
        int run = 1;
        while (i - run >= 0 && brain.PeekGoalAt(i - run).GetDescription() == desc)
            run++;
        goalLines.Add(run > 1 ? $"{desc} x{run}" : desc);
        i -= run;
    }
}
```

Add to `BrainPart`:

```csharp
/// <summary>Peek at a specific stack index without removing. Used by the inspector.</summary>
public GoalHandler PeekGoalAt(int index)
{
    return (index >= 0 && index < _goals.Count) ? _goals[index] : null;
}
```

**Render in the sidebar.** `SidebarStateBuilder.cs` already consumes `LookSnapshot` via `currentLookSnapshot`. Extend its output:
- Add `GoalStackLines` and `LastThought` to `SidebarSnapshot` (which is a DTO — trace its constructor and add fields).
- In `SidebarRenderer` (or wherever the focus panel draws), append the goal-stack section below the existing detail lines when populated.

The sidebar already has no 2-line cap on the focus panel — it draws line-by-line — so the only rendering work is "inject lines into the existing flow."

**Editor menu toggle** — `Assets/Editor/AIDebugMenu.cs` (new, inside a new `Editor/` folder that isn't compiled into the runtime build):

```csharp
#if UNITY_EDITOR
using UnityEditor;
using CavesOfOoo.Diagnostics;

public static class AIDebugMenu
{
    [MenuItem("Tools/Caves of Ooo/AI/Toggle Goal Inspector")]
    public static void ToggleInspector()
    {
        AIDebug.AIInspectorEnabled = !AIDebug.AIInspectorEnabled;
        EditorUtility.DisplayDialog(
            "AI Goal Inspector",
            AIDebug.AIInspectorEnabled ? "Enabled." : "Disabled.",
            "OK");
    }
}
#endif
```

**CP437 glyph check** — we render ASCII letters, digits, `:`, `|`, parens, and `x` for run-length. All are standard CP437; no new glyphs needed. The existing `CP437TilesetGenerator.GetTextTile(char)` used by `LookOverlayRenderer` covers these. Confirmed by inspecting `LookOverlayRenderer.DrawLine`.

**New scenario** `Assets/Scripts/Scenarios/Custom/InspectAIGoals.cs` — reuses `VillageChildrenPetting`-ish layout but toggles the inspector on:

```csharp
[Scenario(
    name: "Inspect AI Goals (Phase 10)",
    category: "Debug",
    description: "Spawns a Snapjaw, Warden, and VillageChild. Press L to hover any of them and see their goal stack + last thought on the sidebar.")]
public class InspectAIGoals : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        CavesOfOoo.Diagnostics.AIDebug.AIInspectorEnabled = true;

        var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
        for (int dx = 1; dx <= 6; dx++)
            ctx.World.ClearCell(p.x + dx, p.y);

        ctx.Spawn("Snapjaw").AtPlayerOffset(6, 0);
        ctx.Spawn("Warden").AtPlayerOffset(3, -2);
        ctx.Spawn("VillageChild").AtPlayerOffset(3, 2);

        ctx.Log("AI inspector enabled. Press 'L' and hover any creature to see their goal stack + last thought in the sidebar.");
    }
}
```

## Plan corrections from verification sweep

- Drop the ring-buffer option outright (Qud is single-slot; mirror that).
- Drop the "make `GetDescription` abstract" option (Qud is non-abstract with sensible default).
- Drop `DebugSettings` ScriptableObject (over-engineered; static bool is consistent with project conventions).
- Drop reusing `LookOverlayRenderer` (hardcoded 2-line cap); use the sidebar focus panel instead.
- Drop `ThinkWhileInZone` (not in Qud).
- Correct Look key line: 357, not 322.

## Files: new and modified

### New files
- `Assets/Scripts/Shared/Utilities/AIDebug.cs` — static `AIInspectorEnabled` toggle, sibling of `PerformanceDiagnostics`.
- `Assets/Editor/AIDebugMenu.cs` — editor-only `Tools/Caves of Ooo/AI/Toggle Goal Inspector` menu.
- `Assets/Scripts/Scenarios/Custom/InspectAIGoals.cs` — opt-in scenario that turns the inspector on and spawns a few NPCs.
- `Assets/Tests/EditMode/Gameplay/AI/AIDebugTests.cs` — fixture for Think, GetDescription defaults, GoFetch GetDetails, goal-stack rendering, counter-checks.
- `Assets/Tests/EditMode/Gameplay/AI/LookQueryServiceAIInspectorTests.cs` — scenario-based fixture asserting the inspector populates `GoalStackLines` for a Creature and stays null for a non-Creature.

### Modified files
- `Assets/Scripts/Gameplay/AI/BrainPart.cs` — add `LastThought`, `ThinkOutLoud`, `Think(string)`, `PeekGoalAt(int)`.
- `Assets/Scripts/Gameplay/AI/Goals/GoalHandler.cs` — add `GetDescription()`, `GetDetails()`, `Think(string)` helper.
- `Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs`, `MoveToGoal.cs`, `GoFetchGoal.cs`, `FleeGoal.cs`, `RetreatGoal.cs`, `CommandGoal.cs` — add `GetDetails()` + 1-2 `Think()` calls.
- `Assets/Scripts/Gameplay/Look/LookSnapshot.cs` — add `GoalStackLines`, `LastThought` optional fields + constructor overload.
- `Assets/Scripts/Gameplay/Look/LookQueryService.cs` — branch on `AIDebug.AIInspectorEnabled` + `Creature` tag to populate the new fields.
- `Assets/Scripts/Presentation/Rendering/SidebarSnapshot.cs` — add `GoalStackLines`, `LastThought` fields (pass-through from LookSnapshot).
- `Assets/Scripts/Presentation/Rendering/SidebarStateBuilder.cs` — forward the new fields.
- `Assets/Scripts/Presentation/Rendering/SidebarRenderer.cs` (or wherever focus panel draws) — emit the goal-stack section and `Thought:` line when populated.
- `Assets/Tests/EditMode/TestSupport/EntityVerifier.cs` — add `LastThoughtContains(string)` and `TopGoalDescriptionContains(string)` asserts (optional — only if 3+ tests need them).

## Implementation sequence (5 commits)

### Commit 1 — Think primitive (small, foundation)
- Add `LastThought`, `ThinkOutLoud`, `Think(string)` to `BrainPart`.
- Add protected `Think(string)` helper on `GoalHandler`.
- Tests: 3 unit tests — (a) `Think` stores LastThought; (b) Think with null is safe; (c) ThinkOutLoud=true emits a Debug.Log in the expected format (use `LogAssert`).
- Everything compiles; full EditMode suite green (1608 → 1611).

### Commit 2 — GetDescription / GetDetails base API
- Add the two virtuals to `GoalHandler`.
- Do NOT override in subclasses yet.
- Tests: (a) default `GetDescription()` on any concrete goal returns the type name; (b) a custom goal with `GetDetails` overridden returns `"Type: details"`; (c) regression — all existing goal-stack tests still pass.
- Pure additive; 1608 → 1611.

### Commit 3 — Overrides + Think seeding on 6 goals
- Add `GetDetails()` + 1-2 `Think()` calls to `KillGoal`, `MoveToGoal`, `GoFetchGoal`, `FleeGoal`, `RetreatGoal`, `CommandGoal`.
- Tests: for each, one unit test that walks the goal through a state change and asserts `GetDetails()` reflects it. Example: construct a `GoFetchGoal`, advance, assert `GetDetails().Contains("phase=Pickup")`.
- Counter-check (QUD-PARITY Part 3.4): a test that `GoFetchGoal._walkAttempts` reads `0/2` at push time (i.e., we exposed state accurately, didn't break encapsulation).
- 1611 → ~1623 tests.

### Commit 4 — AIDebug toggle + LookSnapshot/LookQueryService wiring
- Add `AIDebug` class.
- Add `GoalStackLines`, `LastThought` to `LookSnapshot` (constructor overload for backwards compat).
- Add `PeekGoalAt` to `BrainPart`.
- Extend `LookQueryService.BuildSnapshot` with `BuildBrainInspection` populated only when `AIInspectorEnabled` and target is a Creature with a BrainPart. Include Qud's run-length `xN` collapsing.
- Tests:
  - `LookQueryServiceAIInspectorTests`: with toggle off → `GoalStackLines == null`; with toggle on and hovering a Snapjaw → `GoalStackLines` non-null, contains `"KillGoal"` or `"BoredGoal"`; with toggle on and hovering an Item (non-Creature) → still null. (Counter-check.)
  - Goal-stack formatting: push `[BoredGoal, MoveToGoal, MoveToGoal]` (synthetic), assert rendering is `["MoveToGoal: ... x2", "BoredGoal"]`.
- 1623 → ~1630.

### Commit 5 — Sidebar rendering + scenario + editor menu
- Extend `SidebarSnapshot`, `SidebarStateBuilder`, `SidebarRenderer` to render the goal-stack block + `Thought:` line.
- Add `InspectAIGoals` scenario.
- Add `Assets/Editor/AIDebugMenu.cs`.
- Visual verification in the editor: launch `InspectAIGoals`, hover each creature, confirm the sidebar shows the stack.
- Scenario test: run `VillageChildrenPetting`, toggle inspector on, advance 50 turns, look at the child, assert sidebar snapshot contains either `BoredGoal`, `WanderRandomlyGoal`, or `PetGoal` in the goal lines (accept any — PetGoal is probabilistic).
- 1630 → ~1633.

All five commits individually green-tests, one conceptual thing each.

## Testing plan

### New tests by commit

**Commit 1 — `AIDebugTests` (3 tests):**
- `Think_StoresLastThought`: `brain.Think("hi"); Assert.AreEqual("hi", brain.LastThought);`
- `Think_WithNull_NoThrow_LastThoughtIsNull`.
- `Think_WhenThinkOutLoud_EmitsDebugLog`: use `LogAssert.Expect(LogType.Log, new Regex("^\\[Think:"))`.

**Commit 2 — extension of `AIDebugTests` (3 tests):**
- `GetDescription_DefaultForKillGoal_IsTypeName`.
- `GetDescription_WithDetails_FormatsAsTypeColonDetails`: use a test-only goal that overrides GetDetails.
- `GetDetails_DefaultReturnsNull`.

**Commit 3 — `GoalDetailTests` (~9 tests):**
- For each of the six overridden goals, one test that triggers a phase/state change via scenario (e.g. `ctx.Spawn("Snapjaw")` + 1 turn → assert `FindGoal<KillGoal>().GetDetails().Contains("target=")`).
- Counter-check: `GoFetchGoal_GetDetails_DoesNotExposeNonPublicFields` — spot-check via reflection that no public getter was added for `_walkAttempts`.

**Commit 4 — `LookQueryServiceAIInspectorTests` (5 tests):**
- `Inspector_Off_GoalStackLinesNull`.
- `Inspector_On_CreatureTarget_GoalStackLinesPopulated`.
- `Inspector_On_NonCreatureTarget_GoalStackLinesNull` (counter-check — look at a chest).
- `Inspector_On_EmptyCell_GoalStackLinesNull`.
- `Inspector_RunLengthCollapsing_Works` (synthetic brain with duplicate-description goals).

**Commit 5 — `AIInspectorSidebarIntegrationTest` (2 tests):**
- `Scenario_InspectAIGoals_SidebarSnapshotContainsGoalLinesForWarden`.
- `Scenario_VillageChildrenPetting_WithInspector_EventuallyShowsPetGoalOrBored` (run 50 turns, accept any of `PetGoal`/`BoredGoal`/`WanderRandomlyGoal`).

### Test infrastructure

- All fixtures use `ScenarioTestHarness` + `ctx.AdvanceTurns(n)` + `ctx.Verify()` where possible (QUD-PARITY says use existing).
- Add two helpers to `EntityVerifier` only if 3+ tests need them:
  - `LastThoughtContains(string substring)`
  - `TopGoalDescriptionContains(string substring)`
- Tests that toggle `AIDebug.AIInspectorEnabled` MUST reset it in `[TearDown]` to avoid cross-fixture state bleed.

## Verification steps (proving each deliverable works in-game)

1. **`Brain.Think`** — Launch `InspectAIGoals`. Set `AIDebug.AIInspectorEnabled = true` (scenario does this). In Unity console: verify no spam unless `ThinkOutLoud` is manually flipped. Hover Snapjaw with `L` → sidebar shows `Thought: I'm going to kill Player.` (or similar).
2. **`GetDescription` / `GetDetails`** — In the same scenario, hover each creature and verify the sidebar stack shows:
   - Snapjaw: `KillGoal: target=Player` (top), `BoredGoal` (bottom).
   - Warden: `GuardGoal` / `BoredGoal`. (GuardGoal defaults to type name since we didn't override — that's correct.)
   - VillageChild: `WanderRandomlyGoal` / `BoredGoal` during idle; occasionally `PetGoal`.
3. **Inspector UI** — Toggle the editor menu `Tools/Caves of Ooo/AI/Toggle Goal Inspector` off, hover a creature → sidebar shows only HP + relation (baseline). Toggle back on → goal stack reappears. Confirms gating works bidirectionally.

## Scope cuts / deferred

**v1 (in):** Brain.Think, LastThought, ThinkOutLoud, GetDescription/GetDetails base + 6 overrides, sidebar inspector gated by AIDebug toggle, InspectAIGoals scenario, editor menu.

**v2 (deferred):**
- GetDetails overrides for the remaining 11 goals (BoredGoal, WaitGoal, Wander*, StepGoal, NoFightGoal, DormantGoal, PetGoal, GuardGoal, FleeLocationGoal, WanderDurationGoal, DelegateGoal) — defaults are fine.
- Ring-buffer thought history.
- Qud-style `GetDebugInternalsEvent` parity (PartyLeader, kill radii, allegiances).
- Click-to-pin inspector (follow entity even as cursor moves).
- Replay / history / filtering of thoughts.
- Console command to toggle `AIDebug.AIInspectorEnabled` at runtime.

## Risks

1. **Think-per-tick perf** — `Think` is a single field write + null check. Zero allocation unless `ThinkOutLoud` is on. With 50 NPCs ticking 3 goals/tick and each calling Think once, that's 150 writes/tick. Negligible. *If* a goal were to `Think($"hp is {hp}")` every tick with `ThinkOutLoud` off, the interpolation still allocates a string. **Mitigation:** in Commit 3, only add `Think` calls at *branch points* (once per phase change), not inside loops. Watch `COO.Turns.AI.TakeTurn` marker in a Profiler snapshot before and after Commit 3 — should be within noise.

2. **Sidebar overlay bloat** — The inspector adds up to `(goal-count + 2)` lines to the sidebar focus panel. For a 20-deep stack on a misbehaving NPC, that pushes the message log down. **Mitigation:** cap the rendered stack to 8 lines with `… (N more)` overflow. Add this in Commit 5.

3. **CP437 glyph coverage** — we render `| x N : ( ) =` — all ASCII. No new glyphs needed. Confirmed by `Grep` of `CP437TilesetGenerator` usage in existing overlays.

4. **Test flakiness from AIDebug toggle leaking across fixtures** — a test that sets `AIDebug.AIInspectorEnabled = true` and crashes before teardown will leak state. **Mitigation:** put the toggle reset in `[TearDown]`, and have `ScenarioTestHarness.CreateContext()` reset it defensively. Add an assertion in the harness that catches the leak.

5. **`LookSnapshot` is immutable (sealed class with get-only properties)** — extending it means either a new constructor overload or mutation. Choose the constructor overload for backwards compat, but this risks call sites being missed. **Mitigation:** `Grep` for `new LookSnapshot(` and audit all call sites in Commit 4.

6. **SidebarSnapshot is similarly a DTO** — same risk as LookSnapshot. Same mitigation.

7. **Live-code shapes drift** — the verification sweep above already caught 10+ deltas between the brief's assumptions and the actual code. Any commit in this plan should re-run a small `Grep` check before editing to confirm signatures haven't changed since this plan was written. (QUD-PARITY Part 1.2 discipline.)

### Critical Files for Implementation

- `Assets/Scripts/Gameplay/AI/BrainPart.cs`
- `Assets/Scripts/Gameplay/AI/Goals/GoalHandler.cs`
- `Assets/Scripts/Gameplay/Look/LookQueryService.cs`
- `Assets/Scripts/Presentation/Rendering/SidebarStateBuilder.cs`
- `Assets/Scripts/Gameplay/Look/LookSnapshot.cs`
