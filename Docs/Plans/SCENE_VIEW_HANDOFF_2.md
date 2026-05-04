# Scene View System — Handoff #2 (M3+M4+M6 + bleed-through fix)

> **Read this first**, then `Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md`
> for full milestone context (especially the Progress Log table — every
> milestone has a row + a cold-eye review row).

## TL;DR

**Branch:** `claude/task-d-zKesU` (9 new commits since the last handoff).
**Status:** M1 ✅, M2 ✅ + scene-wired, M3 ✅, M4 ✅, M5 ⏸️ deferred, M6 ✅, M7 ⏳.
**Total new EditMode tests across M3+M4+M6:** 38 (14 + 15 + 9).
**Total SceneView suite:** **66 tests, never executed in this session** because Unity MCP was disconnected throughout. Run them as your first move.
**One known production bug already fixed in code (d62cbf5)** but not yet visually verified — see "First Tasks" below.

---

## What's been shipped on this branch (most recent → oldest)

```
d62cbf5  fix(scene-view): paint opaque background on scene-blank cells (world bleed-through)
cf5f6d1  docs(scene-view/M6): cold-eye review — Name/Command divergence + plan log
7186659  feat(scene-view/M6): LookAtScenePart + Campfire blueprint trigger wiring
29d82f1  docs(scene-view/M4): cold-eye review — fix test-count drift, log review pass
e743704  feat(scene-view/M4): radial dissolve transition (entry + exit)
4c42491  fix(scene-view/M3): cold-eye review fixes (param naming + dead guard)
1b730e6  feat(scene-view/M3): SceneRenderer animation port (flame/sparks/twinkle/crackle/wind)
d7e270d  feat(scene-view): wire SceneViewUI into SampleScene
   ...   (prior M1/M2/handoff #1)
```

### M3 — Animation port (commits `1b730e6`, `4c42491`)
- `SceneRenderer.Tick(float deltaTime)` advances animation state.
- Probabilistic flame glyphs via `FlameChar(intensity)` — 5 intensity tiers, JS-parity probability tables (`@#%&`/`%&\*`/etc.), color via `FlameColor` jittered per cell.
- Spark particles: spawn at flame core, drift up-and-out with `Vy *= 0.985f` resistance, age 14–24 frames; in-place `RemoveAt` cull.
- Star twinkle: per-star sine phase advances each Tick.
- Crackle period 3–7s (`_crackleLevel *= 0.92f` decay); wind gust period 4–9s (`*= 0.96f` decay) skews flame columns.
- Random ember glow on logs; per-cell ground flicker.
- Seeded `System.Random` (default 12345) — same seed → identical Frame.
- 14 tests in `SceneRendererAnimationTests.cs`.
- Cold-eye fix: `Tick(deltaT)` → `Tick(deltaTime)`; dead `s.Max <= 0` guard removed.

### M4 — Dissolve transition (commits `e743704`, `29d82f1`)
- `SceneRenderer.StartDissolve(bool reverse = false)`, `UpdateDissolve(float deltaTime)`, `GetMask(x,y)`, public state `IsDissolving`/`DissolveIsReverse`/`DissolveProgress`/`DISSOLVE_DURATION = 1.6f`.
- Radial iris from `(W/2, H/2 + 2)` (note the `+2` y-offset) over 1.6s; 1-cell soft-edge band; reveal radius `progress * maxR * 1.2f`.
- `DrawDissolveOverlay` runs only when `IsDissolving` (preserves M2/M3 baseline behavior otherwise).
- 15 tests in `SceneRendererDissolveTests.cs`.
- **`SceneViewUI` lifecycle** (the user-facing flow):
  1. `HandleActivated` → reconstruct renderer, `StartDissolve(false)`, `_isRendering=true`, ZoneRenderer NOT paused (world peeks through cleared cells during iris).
  2. `Update` → `Tick(dt)`, `UpdateDissolve(dt)` if dissolving, `RenderToTilemap`.
  3. After forward dissolve completes: `Update` pauses `ZoneRenderer` (scene fully covers).
  4. `HandleDeactivated` → `StartDissolve(reverse: true)`, `_exitDissolveActive=true`, ZoneRenderer unpaused. Cleanup (clear tilemap, `_isRendering=false`) deferred until reverse dissolve completes via `Update`.
- **Scope cuts from plan**: (a) `SceneViewManager` Begin/End hooks **NOT** added (manager stays a pure state machine); (b) JS pre-pattern (PreChar/PreColor) **NOT** implemented (overlay sits above world tilemap, so cleared cells just expose the world directly — no need for jungle pre-pattern).

### M6 — Trigger wiring (commits `7186659`, `cf5f6d1`)
- New `Assets/Scripts/Gameplay/Entities/LookAtScenePart.cs` (note: flat folder, NOT `Entities/Parts/` per the plan's outdated path).
- Mirrors `ConversationPart`'s two-stage `HandleEvent`: declares `Look` action ('l', priority 10) on `GetInventoryActions`; handles `InventoryAction` with command `"LookAtScene"` by calling `SceneViewManager.Activate(SceneID)`.
- Campfire blueprint in `Assets/Resources/Content/Blueprints/Objects.json` (line 3606 area) gains `{ "Name": "LookAtScene", "Params": [{ "Key": "SceneID", "Value": "Campfire" }] }`.
- 9 tests in `LookAtScenePartTests.cs` — action declaration + execution + counter-checks (empty/null SceneID, wrong command) + 2 blueprint-integration end-to-end tests.
- Cold-eye 🔵 resolved: explanatory comment about Name="Look" / Command="LookAtScene" divergence (vs `ConversationPart`'s Name==Command coincidence) — intentional disambiguation.
- **M5 (`SceneViewData` ScriptableObject) is deferred** — pure scaffolding without payoff for the single shipping scene. M6 uses string `Activate(SceneID)` form throughout.

### Bleed-through fix (commit `d62cbf5`)
- **User reported during playtest**: campfire scene appeared correctly but the world map was visible underneath.
- Root cause: `SceneViewUI.RenderToTilemap` set `Tilemap.SetTile(pos, null)` for both `' '` (scene-blank) and `'\0'` (renderer default) → all the naturally-blank cells in the campfire composition (sky between stars, ground outside firelight pool, areas around tent/logs) leaked the world tilemap below.
- Fix: `'\0'` is now the sentinel for "intentionally transparent for dissolve" (`DrawDissolveOverlay` writes `'\0'` for cleared cells). `' '` is "scene-blank, occlude the world" — paints CP437 `'Û'` ('█' solid block) with `Color.black`. CP437 generator already produces this tile (`CP437TilesetGenerator.cs:354` notes it's "Solid block for background fills").
- Dissolve transition still gets correct world peek-through (mid-mask probabilistic clears + full-mask cells write `'\0'` → transparent).

---

## Why this handoff exists / known channel issue

The previous session ran without working Unity MCP RPC for most of M3, all of M4, and all of M6. Unity Editor.log showed `[CodexMcpKickstart] HTTP bridge start attempted. Attempt=4/5 Started=False`, the kickstart caps at 5 retries. By end of session, **the Unity-MCP server was actually healthy** (Python proxy `mcp-for-unity --transport http --http-url http://127.0.0.1:8080` at PID 55291, Unity at PID 49560 connected to it via project-scoped token), but the prior Claude Code instance's MCP client channel was stuck disconnected and could not be revived without a Claude Code restart.

**Therefore: 66 SceneView tests have never been executed in Unity.** RED → GREEN was reasoned through static traces of the mask math and direct code review of ConversationPart precedent. Every test references real, verified API surfaces. The risk is real but the bar to red-flag a regression is low — running the suite is your first task.

---

## Your First Tasks (in order, pace MCP calls)

### 1. Confirm MCP is healthy from a fresh session

```
mcp__unity__refresh_unity (compile=request, wait_for_ready=true)
mcp__unity__read_console (types=[error])     # must be empty
```

If MCP responds, proceed. If not, check:
- Unity processes alive: `ps -ax | grep -i Unity | head -5`
- Python proxy alive: `ps -ax | grep mcp-for-unity` (expect a `--http-url http://127.0.0.1:8080` server)
- Direct probe: `curl -m 3 http://localhost:8080/mcp` (200 + JSON-RPC error about needing `text/event-stream` = healthy)

If the proxy is healthy but `mcp__unity__*` says "not connected," it's a Claude Code client-side issue — the user needs to restart Claude Code.

### 2. Run the 66-test SceneViews suite

```
mcp__unity__run_tests
  mode=EditMode
  assembly_names=["EditModeTests"]
  test_names=["CavesOfOoo.Tests.EditMode.Presentation.SceneViews"]
  include_failed_tests=true
```

Expected: 66/66 green. Breakdown:
- 15 SceneViewManager (M1)
- 13 SceneRendererStaticTests (M2)
- 14 SceneRendererAnimationTests (M3)
- 15 SceneRendererDissolveTests (M4)
- 9 LookAtScenePartTests (M6)

If anything red, paste the failure summary first; do NOT start fixing until you understand why.

**Do NOT run the full 2181-test EditMode suite.** Per `memory/feedback_unity_mcp_pacing.md`, the full suite plus rapid MCP calls dropped the plugin session in the previous run. Scope to the SceneViews namespace; collateral risk is minimal (SceneRenderer/SceneViewUI/LookAtScenePart have no callers outside the SceneViews namespace + InputHandler dispatch).

### 3. Manually verify the bleed-through fix in Play mode

Per CLAUDE.md "Entering Play mode resets the scene. Warn the user before doing it" — confirm with the user before clicking Play.

```
mcp__unity__manage_editor action=play
```

Then in-game:
- Walk player adjacent to a Campfire (zone seed dependent — may need to spawn one via debug tools or use a starting-area Campfire)
- Press 'l' or open the inventory action menu and pick "look at fire"
- **Verify**: forward dissolve plays (~1.6s), animated campfire scene appears, **the world map should be FULLY hidden once the iris is fully open** (this is the bleed-through bug fix — d62cbf5)
- Press [E] → reverse dissolve plays, world returns
- `mcp__unity__manage_editor action=stop`

If world is still visible behind the active scene, the fix didn't take — paste a screenshot and we diagnose.

### 4. (Only if 1–3 are clean) Commit the auto-generated `.meta` files

When Unity recovers, it'll auto-generate `.meta` files for `LookAtScenePart.cs` and `LookAtScenePartTests.cs` (they were missing because Unity's asset import was stalled when we created those files). Single-purpose follow-up commit:

```bash
git -C /Users/steven/house_feature/caves_of_ooo add \
  Assets/Scripts/Gameplay/Entities/LookAtScenePart.cs.meta \
  Assets/Tests/EditMode/Presentation/SceneViews/LookAtScenePartTests.cs.meta
git -C /Users/steven/house_feature/caves_of_ooo commit -m "chore(scene-view/M6): add Unity-generated .meta files"
```

---

## What's deferred to M7 (the audit milestone)

Logged in `Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md` Progress Log under cold-eye rows. Don't fix any of these until M3/M4/M6 are PlayMode-verified working.

🧪 deferred test gaps:
- M3: `WindGust_SkewsFlamesLaterally` (needs internal-state setter; brittle without one)
- M3: `FlameChar` tier→glyph mapping not directly tested (only via seed determinism)
- M3: `FlameColor` height-tier→color mapping only spot-checked for warmth
- M3: `Crackle_LevelStartsAtZero` counter-check missing
- M4: Soft-edge probabilistic clear-vs-darken render branch not directly asserted
- M4: SceneViewUI MonoBehaviour lifecycle (forward → ACTIVE → reverse → IDLE) not unit-tested (PlayMode territory)
- M6: `e.Handled = true` set in code but not asserted by any test

⚪ accepted minor:
- M4: HandleDeactivated doesn't immediate-render — relies on next Update tick (~17ms delay, imperceptible)
- M3/M4: property-pattern divergence (M3 uses `=> _field`, M4 uses `{ get; private set; }`); both valid

---

## Open design considerations for tomorrow / post-launch

1. **Camera framing for the Scene View.** Right now SceneViewUI's overlay tilemap is parented under ZoneGrid sharing the existing camera. The 80×28 canvas may not perfectly fit the camera's viewport; if the player sees scene tiles cropped or with margins, ZoneGrid scale or a dedicated camera + culling mask are options. Confirm visually in Play mode before adding scope.
2. **Day/night state.** Decided in plan (open question #4): scenes are atmospheric snapshots, NOT derived from world time-of-day. Keep that.
3. **Companion NPCs in scene.** Plan §"Open Design Questions" #5: deferred until first companion ships.
4. **Scene-internal mini-games.** Out of scope (cooking UI, fishing UI, etc. would build on top).
5. **M5 SceneViewData ScriptableObject.** Skipped intentionally — only valuable when adding a 2nd scene.

---

## Repo facts (verify before relying on them — they may have shifted)

- **Unity project root**: `/Users/steven/house_feature/caves_of_ooo` (NOT the worktree under `.claude/worktrees/`).
- **Branch**: `claude/task-d-zKesU` (the parent's branch — Unity is connected to this clone, not the worktree).
- **Game scene**: `Assets/Scenes/Main/SampleScene.unity`. `SceneViewUI` GameObject is parented under `ZoneGrid` (instance ID may vary; lookup `find_gameobjects search_method=by_component search_term=SceneViewUI`).
- **Unity 6 (6000.3.4f1)**.
- **Test assembly**: `EditModeTests.asmdef`. Group filter `CavesOfOoo.Tests.EditMode.Presentation.SceneViews` works via `test_names` parameter (NOT `group_names` — empirically `group_names` returned 0 matches).

---

## If something goes off the rails

- **Stop, surface the blocker, get user confirmation before destructive action.** Don't `git reset --hard`, don't amend commits, don't force-push.
- **Run the full cold-eye pass** (CLAUDE.md §"Post-implementation cold-eye review") if you ship any new milestone work — Q1 symmetry, Q2 cross-feature consistency, Q3 counter-check completeness, Q4 doc-vs-impl drift. The previous session caught real findings via this in M3 + M4 + M6.
- **Memory files at** `/Users/steven/.claude/projects/-Users-steven-house-feature-caves-of-ooo/memory/` — currently has one feedback note about pacing Unity MCP calls. Read `MEMORY.md` first.
- **CLAUDE.md is non-optional.** TDD (RED before GREEN), pre-impl verification sweep on any major feature, counter-checks on every positive assertion, in-phase self-review with severity markers, scope-divergence transparency in commits.

---

## Suggested first prompt for the new Claude Code session

> Read `Docs/Plans/SCENE_VIEW_HANDOFF_2.md` and the `Progress Log` table
> at the bottom of `Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md`.
> Do tasks 1, 2, and 3 from the handoff in order — confirm MCP healthy,
> run the 66 SceneViews tests, then ask me before entering Play mode for
> the visual bleed-through verification. Do NOT proceed to M7 until
> 1+2+3 are clean.
