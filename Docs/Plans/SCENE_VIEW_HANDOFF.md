# Scene View System — Handoff for Local Claude Code Agent

> **Purpose:** Hand off the Scene View System implementation from the
> cloud Claude session that drafted the plan + shipped M1 and M2, to a
> fresh local Claude Code session that has Unity MCP available and can
> actually execute the EditMode tests.
>
> Read this file plus
> `Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md` to pick up
> where the cloud session left off.

---

## TL;DR

**Branch:** `claude/task-d-zKesU` (pushed to `origin`)
**Status:** M1 ✅ shipped, M2 ✅ shipped. M3–M7 pending.
**Total new EditMode tests:** 29 (15 for SceneViewManager, 14 for SceneRenderer)
**Tests not yet executed in Unity** — that's your first task.
**Visual spec:** `Docs/Mockups/scene-views/campfire.html` (animated JS prototype — the authoritative reference)

---

## Why This Handoff Exists

The cloud sandbox where M1 and M2 were written has no Unity, no `dotnet`,
no `mcp__unity__*` tools. So the cloud agent could write code and verify
it via static-trace analysis but **could not actually execute the tests**.
You can. That's the whole reason we're handing off here.

---

## What Was Shipped

### M1 — SceneViewManager static state machine

| File | Lines | Purpose |
|---|---|---|
| `Assets/Scripts/Presentation/SceneViews/SceneViewManager.cs` | 74 | Static state machine: `IsActive`, `ActiveSceneID`, `Activate(id)`, `Deactivate()`, `OnActivated`, `OnDeactivated`, `Reset()` |
| `Assets/Tests/EditMode/Presentation/SceneViews/SceneViewManagerTests.cs` | 175 | 15 tests covering state transitions, null/empty guards, events, Reset semantics |

**Pattern:** mirrors `ConversationManager.IsActive` (`ConversationManager.cs:17`).
Pure data, no rendering.

**Scope cut from original plan:** TurnManager modifications were verified
unnecessary — turn flow is naturally gated by input (no movement input →
`ProcessUntilPlayerTurn` not called). Same gating pattern that already
works for conversations. Documented in plan progress log.

### M2 — SceneRenderer + SceneViewUI + InputHandler integration

| File | Lines | Purpose |
|---|---|---|
| `Assets/Scripts/Presentation/SceneViews/SceneRenderer.cs` | 255 | Pure C# renderer with hardcoded Campfire composition (logs, ground, tent, distant trees, stars, frozen-frame flame, scene text, prompts) |
| `Assets/Scripts/Presentation/SceneViews/SceneViewUI.cs` | 130 | MonoBehaviour: subscribes to SceneViewManager events, drives SceneRenderer, writes frame buffer to Unity Tilemap, toggles `ZoneRenderer.Paused` |
| `Assets/Tests/EditMode/Presentation/SceneViews/SceneRendererStaticTests.cs` | 195 | 14 tests covering frame size, log/ground/tent/star/flame/prompt placement, color heuristics, determinism |
| `Assets/Scripts/Presentation/Input/InputHandler.cs` | +30 lines | New `InputState.SceneOpen`, dispatch case at `~line 466`, `OnEnable/OnDisable` for event subscription, `HandleSceneActivated/Deactivated/SceneOpenInput` |

**Verified before commit:**
- `InputHelper.GetKeyDown` is the project's input shim (NOT raw `Input.GetKeyDown`)
- `CP437TilesetGenerator.GetTile(char)` returns `Tile`
- `DialogueUI.cs:720-727` is the canonical Tilemap-write pattern (mirrored exactly)
- Y-axis flip done in `RenderToTilemap` via `(Height - 1 - y)`

---

## Architecture Quick-Reference

```
SceneViewManager (static, namespace CavesOfOoo.Rendering)
├── IsActive : bool                  ← derived from ActiveSceneID
├── ActiveSceneID : string
├── Activate(string sceneID)         ← null/empty silently ignored
├── Deactivate()                      ← no-op when not active
├── OnActivated : Action<string>     ← subscribers: SceneViewUI, InputHandler
├── OnDeactivated : Action
└── Reset()                           ← test isolation + load-game safety net

SceneViewUI (MonoBehaviour, namespace CavesOfOoo.Rendering)
├── Tilemap (Inspector)               ← target for frame-buffer writes
├── ZoneRenderer (Inspector / auto-find)
├── CanvasWidth = 80, CanvasHeight = 28
├── CanvasOrigin (Vector2Int)
└── Owns a SceneRenderer instance

SceneRenderer (pure C#, namespace CavesOfOoo.Rendering)
├── Frame : SceneCell[]               ← Width × Height grid
├── RenderCampfire()                  ← M2: deterministic, no animation
└── GetCell(x, y)                     ← bounds-protected accessor

InputHandler (modified)
├── InputState.SceneOpen              ← new enum value
├── OnEnable/OnDisable                ← subscribe/unsubscribe SceneViewManager events
├── HandleSceneActivated/Deactivated  ← flip _inputState
└── HandleSceneOpenInput              ← [E] / [Esc] dismiss
```

**Wiring flow when a scene opens:**

1. Caller invokes `SceneViewManager.Activate("Campfire")`
2. `OnActivated` event fires
3. `SceneViewUI.HandleActivated` sets `ZoneRenderer.Paused = true` and starts rendering
4. `InputHandler.HandleSceneActivated` saves prior state, flips `_inputState = SceneOpen`
5. While SceneOpen, only `[E]` / `[Esc]` is recognized — they call `SceneViewManager.Deactivate()`
6. `OnDeactivated` fires → SceneViewUI clears Tilemap, `Paused = false`; InputHandler restores prior state

---

## Your First Tasks (in order)

### 1. Verify M1+M2 EditMode tests pass

```
mcp__unity__refresh_unity                          # compile after pull
mcp__unity__read_console types=[error]             # must be empty
mcp__unity__run_tests mode=EditMode filter=SceneView  # 29 tests should run, all green
```

If any fail, paste the failures and we diagnose. **Don't continue to M3 until M1+M2 are green.**

### 2. Scene-wire SceneViewUI in Unity Editor

The MonoBehaviour exists but isn't placed in `SampleScene.unity` yet.

1. Open `Assets/Scenes/SampleScene.unity`
2. Create a new GameObject named `SceneViewUI`
3. Add component `SceneViewUI` (CavesOfOoo.Rendering)
4. Add a `Tilemap` component (or reference an existing UI overlay tilemap that covers full-screen)
5. Drag the Tilemap into the `Tilemap` field on SceneViewUI
6. Leave `ZoneRenderer` field empty (auto-discovered via `FindObjectOfType` in Awake)
7. Verify: `CanvasWidth = 80`, `CanvasHeight = 28`, `CanvasOrigin = (0, 0)` (or whatever fits your screen layout)
8. Save the scene

### 3. Manual smoke test

Run play mode. From a debug console (or temporary keybinding), trigger:

```csharp
CavesOfOoo.Rendering.SceneViewManager.Activate("Campfire");
```

You should see:
- ZoneRenderer hidden (Paused=true)
- Campfire scene rendered: stars, tent left, distant trees right, logs anchored center, frozen flame above, scene text, prompts at bottom
- Press `[E]` or `[Esc]` → scene clears, world returns

**M2 is static** — no animation. Flames don't flicker yet. That's M3.

If smoke test passes → continue to M3. If not, paste what you see (or don't see) and we diagnose.

---

## What's Pending (M3–M7)

Per `Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md`:

### M3 — Animation port
Port the JS prototype's animation logic to C#:
- Probabilistic flame glyph draw (per-cell glyph weighted by intensity)
- Spark particles (tracked list, drift up-and-out, fade by age)
- Star twinkle (per-star sine phase)
- Crackles (random intensity boost + spark burst)
- Wind gusts (lateral skew)

**TDD:** deterministic seeded RNG → expected glyph at known cells.

**Source spec:** `Docs/Mockups/scene-views/campfire.html` lines for `flameChar()`, `spawnSpark()`, the per-frame `frame()` function, and crackle/wind timers. The JS is the spec — port it line-for-line.

### M4 — Dissolve transition
Radial dissolve from center, ~1.6s. Mockup file shows the algorithm.

### M5 — `SceneViewData` ScriptableObject
Refactor hardcoded composition in `SceneRenderer` into a data asset. Each scene becomes its own `.asset` file. Unblocks adding scenes without engineering.

### M6 — Trigger wiring
`LookAtScenePart` on Campfire blueprint. Player presses Look → fires `GetInventoryActions` event → action contributed → executing action calls `SceneViewManager.Activate`.

### M7 — Self-review, audit, manual playtest scenario

---

## Known Issues / Open Questions

| Severity | Issue | Where | Fix |
|---|---|---|---|
| 🔵 | `SceneViewUI.HandleActivated` hardcodes scene-id check for `"Campfire"` | `SceneViewUI.cs:55` | M5: scene-id resolved via SceneViewData registry |
| 🔵 | `Update()` re-renders every frame even though composition is static in M2 | `SceneViewUI.cs:80` | Harmless. M3 needs per-frame rendering anyway |
| 🔵 | Tests static-traced only, never executed | (your job) | Run them in Unity Test Runner |

---

## Files Touched This Session (Scene View work only)

```
NEW Assets/Scripts/Presentation/SceneViews/SceneViewManager.cs
NEW Assets/Scripts/Presentation/SceneViews/SceneRenderer.cs
NEW Assets/Scripts/Presentation/SceneViews/SceneViewUI.cs
NEW Assets/Tests/EditMode/Presentation/SceneViews/SceneViewManagerTests.cs
NEW Assets/Tests/EditMode/Presentation/SceneViews/SceneRendererStaticTests.cs
MOD Assets/Scripts/Presentation/Input/InputHandler.cs (+30 lines)
NEW Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md (430 lines)
NEW Docs/Mockups/scene-views/index.html
NEW Docs/Mockups/scene-views/campfire.html (the visual spec)
```

---

## Commit Trail (newest first, scene-view only)

```
3a62bf2 feat(scene-view/M2): SceneRenderer + SceneViewUI + InputHandler wiring
56abc03 feat(scene-view/M1): SceneViewManager static state machine
f32ebfa docs: add Scene View System implementation plan
c15fa02 mockup: ASCII campfire scene view (option A — hand-authored scene)
```

Each milestone independently revertable per Methodology Template §1.4.

---

## Other Significant Work On This Branch

The cloud session also produced (already committed, may be relevant
context for future work but not required to continue Scene Views):

- **Narrative State Layer M1–M5** — see `Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md` and its handoff doc. Status: ✅ shipped, awaiting Unity test verification just like Scene Views.
- **Lore + design docs:**
  - `ROTCHOIR_VOICES.md` — five named Choir NPCs (Mogu, Grib, Nam, Sien, Sopp)
  - `IDEAS.md` — Blocs, Sects, Memory Consumption, Blackmail V1+V2 with full anti-cheese frameworks
  - `Docs/Design/INK_SYSTEM.md` — Ink as Class A pillar (Palimpsest-aligned)
  - `Docs/Design/BLACKMAIL_SYSTEM.md` — full design treatment
- **Eight animated zone demos** at `Docs/Mockups/shifting-zones/`:
  Mycelial Deep, Temporal Substrate, Inkbleed, Hollow Cathedral,
  Bright-Water Coast, Saccharine Concord, First Root Chamber,
  Half-Standing Spire, plus a live jungle chunk that ports the actual
  `JungleBuilder.cs` algorithm with time-of-day sun raycast.

---

## Resuming the Local Session

```bash
cd ~/caves_of_ooo
git fetch origin
git checkout claude/task-d-zKesU
git pull
claude
```

**First prompt to the local session:**

> Read `Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md` and
> `Docs/Plans/SCENE_VIEW_HANDOFF.md`. Run the 29 EditMode tests under
> `CavesOfOoo.Tests.EditMode.Presentation.SceneViews` namespace using
> `mcp__unity__run_tests`. If green, scene-wire `SceneViewUI` in
> `SampleScene.unity` per the handoff's "Your First Tasks" section.
> Don't proceed to M3 until M1+M2 are verified working.

If tests pass, second prompt:

> Continue with M3 — Animation port from
> `Docs/Mockups/scene-views/campfire.html` (the JS is the spec).
> TDD: write deterministic seeded-RNG tests first, port the
> probabilistic flame draw, spark particles, star twinkle, crackle,
> and wind gust logic. Update plan progress log when M3 ships.

---

## Key Design Decisions (so the local session doesn't relitigate them)

1. **`SceneViewManager` is a static class, not an instance singleton.**
   Mirrors `ConversationManager.IsActive` exactly. Pure data, no rendering.

2. **`ActiveSceneID` is a `string` for now (M1–M4).** Will become a
   `SceneViewData` reference in M5. The string form deliberately
   decouples M1–M4 from M5's asset format.

3. **`Reset()` clears event subscribers.** Necessary for test isolation
   given the static class. Production callers: future save-load hook.

4. **TurnManager untouched.** Scope cut from plan; verified during pre-impl
   sweep that input gating handles turn flow naturally.

5. **InputHandler subscribes to `SceneViewManager.OnActivated/OnDeactivated`
   directly in `OnEnable/OnDisable`.** Decoupled — no caller needs to
   tell InputHandler about scene state changes.

6. **`Y`-axis flipped in `RenderToTilemap`** because the scene grid grows
   down (top-left is row 0) but Unity tilemaps grow up. `(Height - 1 - y)`.

7. **JS prototype is the authoritative visual spec.** `Docs/Mockups/scene-views/campfire.html` —
   anything ambiguous in the C# port, the JS wins.

8. **Hand-authored scenes, not procedural.** Each scene is its own piece
   of art. Cap at ~10–15 total. The Caves-of-Qud "scene as poem" approach.
