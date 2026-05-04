# Scene View System — Implementation Plan

> **Status:** Draft — not yet started.
> Tracking: M1–M7 milestones.
> Related: `Docs/Mockups/scene-views/campfire.html` (working JS prototype, design spec)

---

## Goal

Introduce a **Scene View** mode: a hand-authored full-screen ASCII art moment
that temporarily replaces the tilemap when the player triggers an
atmospheric action (e.g. "Look at campfire"). Each scene is a piece of
art in the game's existing CP437 visual language — animated where it
should be (flames, sparks, stars), static where it should be (logs,
silhouettes) — that pauses gameplay, lets the player rest with the
moment, and resumes the world when dismissed.

Player-visible outcome: the player walks up to a campfire, presses
**Look**, the world dissolves from edges inward, an animated campfire
scene appears with rotating ambient text and four prompts (`Return /
Rest / Cook / Talk`). When they press **E**, the dissolve reverses and
they're back in the tilemap exactly where they were.

Pattern target: ship the mechanism + the campfire scene first; design
the system so adding subsequent scenes (vistas, relics, named-NPC
introductions, key dialogue beats) is a per-scene authoring exercise,
not engineering.

---

## Scope

### In scope
- `SceneViewManager` — static singleton mirroring the
  `ConversationManager.IsActive` pattern. Owns mode state.
- `SceneRenderer` — pure C# class that renders a scene's animation
  frame by frame to a tile grid. Direct port of the JS prototype's
  rendering logic.
- `SceneViewUI` (MonoBehaviour) — overlay tilemap, drives `SceneRenderer`
  each frame, handles input.
- `SceneViewData` — ScriptableObject (or plain class) describing a
  single scene's composition, animation parameters, prompts, text.
- `Campfire` scene as the first concrete `SceneViewData` instance.
- Dissolve transition (entry + exit, radial from center).
- `LookAtScenePart` — entity Part that contributes a "Look" action
  to the inventory action list, opens a configured scene when invoked.
- Wire to existing `Campfire` blueprint.
- Input mode-stack integration — game input suppressed while
  SceneView is active; only scene-prompts respond.
- TurnManager pause integration.
- ZoneRenderer hide integration (reuse existing `Paused` flag).

### Explicitly out of scope
- 3D rendering (option B / C from the design conversation — deferred
  forever unless the game evolves toward 3D)
- Save/load mid-scene (player can't save while a scene is open;
  scene state is ephemeral)
- Multi-scene navigation chains (each scene is single-shot)
- Camera shake / screen distortion effects beyond the prototyped
  dissolve, sky-tint, firelight pulse
- Additional scenes beyond Campfire (those become content tasks
  authored against the shipped system)
- Scene-internal mini-games (cook UI, fishing UI, etc. — those are
  separate features that can use the Scene View as a backdrop)

---

## Content-Readiness Analysis

| Deliverable | Status | Notes |
|---|---|---|
| `SceneViewManager` singleton | 🟢 | New class; mirrors `ConversationManager.IsActive` pattern verified. |
| `SceneRenderer` animation logic | 🟢 | Direct C# port of JS prototype. The JS is the spec. |
| `SceneViewUI` MonoBehaviour | 🟢 | Pattern verified — `DialogueUI.cs` uses identical Tilemap-overlay shape. |
| `SceneViewData` asset format | 🟡 | Need to decide: ScriptableObject vs JSON. Recommend ScriptableObject for now (matches Unity-native authoring). |
| Dissolve transition | 🟢 | Self-contained in `SceneRenderer`; no engine integration needed. |
| `LookAtScenePart` action | 🟢 | `ConversationPart` precedent verified — listen for `GetInventoryActions`, contribute action. |
| Campfire blueprint hookup | 🟡 | Verify Campfire blueprint exists (`Objects.json`); add `LookAtScene` part. |
| TurnManager pause | 🟡 | No explicit pause API. `WaitingForInput` flag exists but is for normal player-turn waiting. Need to add a `IsPaused` check in `ProcessUntilPlayerTurn`, or short-circuit via `SceneViewManager.IsActive`. |
| ZoneRenderer hide | 🟢 | `public bool Paused;` already exists on `ZoneRenderer`. Reuse it. |
| Input mode-stack | 🟡 | Verify `InputHandler` short-circuits when `ConversationManager.IsActive`. If yes, add `\|\| SceneViewManager.IsActive` alongside. |
| CP437 tilemap overlay | 🟢 | `DialogueUI.Tilemap` field is an existing UI tilemap that we can pattern-match. |

---

## Cross-Cutting Infrastructure Gaps

| Gap | Affected milestone | Fix |
|---|---|---|
| `TurnManager.ProcessUntilPlayerTurn` doesn't know about UI modes | M1 | Add a single guard at the loop top: `if (SceneViewManager.IsActive) return null;`. Already similar for `BrainPart.HandleTakeTurn`'s `InConversation` check. |
| `InputHandler` mode routing | M1 | Add `SceneViewManager.IsActive` to existing mode short-circuit list (where `ConversationManager.IsActive` already lives). |
| ZoneRenderer Paused flag is `public` field, not property | M1 | Already usable. `_zoneRenderer.Paused = true;` in scene-mode toggle. |
| No existing "look-at" / "examine" verb on entities | M5 | New `LookAtScenePart` is the verb. Adds `LookAt` action to inventory action list. |
| Save format doesn't currently snapshot UI mode | (none — out of scope) | Players can't enter scenes from a save mid-scene; `SceneViewManager.Reset()` on load is sufficient. |

---

## Pre-Implementation Verification Sweep (§1.2)

Per Methodology Template Part 1.2, verify each API claim before writing
code. Done before any commit:

| Claim | Verified |
|---|---|
| `TurnManager.WaitingForInput` is the only existing pause-like flag | ✅ TurnManager.cs:44 |
| `TurnManager.ProcessUntilPlayerTurn` is the loop that needs guarding | ✅ TurnManager.cs:127 |
| `ZoneRenderer.Paused` is a public field, settable | ✅ ZoneRenderer.cs:35 |
| `ConversationManager.IsActive` is the existing UI-mode short-circuit | ✅ DialogueUI.cs:82 |
| `ConversationPart` listens for `GetInventoryActions` and contributes actions | ✅ ConversationPart.cs:34 |
| `DialogueUI` is a MonoBehaviour with a Tilemap field — template for SceneViewUI | ✅ DialogueUI.cs |
| `Campfire` blueprint exists in Objects.json | (verify in M5) |
| InputHandler routes by mode flags (where to add SceneView short-circuit) | (verify in M1) |
| Existing tests: `ScenarioTestHarness` for live-bootstrap, `ConversationPartActionTests` for Part action wiring | ✅ |

---

## Effort-to-Impact Ordering

1. **M1 — Engine plumbing.** Adds the mode switch with a black screen.
   Zero scene content. Smallest blast radius. Proves the engine accepts
   the mode without breaking the turn loop.
2. **M2 — Static scene render.** Hardcoded campfire composition
   (logs, ground, tent, stars, no animation). Proves the rendering
   pipeline.
3. **M3 — Animation port.** Flames, sparks, twinkle, crackle, wind.
   Proves visual fidelity to the prototype.
4. **M4 — Dissolve transition.** Entry + exit. Proves the cinematic
   feel that justifies the whole feature.
5. **M5 — Scene asset format.** Refactor M2-M4's hardcoded composition
   into `SceneViewData`. Proves the system scales beyond one scene.
6. **M6 — Trigger wiring.** `LookAtScenePart` on `Campfire` blueprint;
   game-driven entry. Proves the player-visible feature.
7. **M7 — Self-review, audit, polish.**

---

## Implementation Tiers

| Tier | Contents |
|---|---|
| A (hours) | M1: mode-switch plumbing |
| B (small, 1–2 days) | M2: static render + M5: asset format |
| C (medium, 2–4 days) | M3: animation port + M4: dissolve |
| C (small, 1 day) | M6: trigger wiring |
| D (long-horizon) | additional scenes (one per ~1–2 hours of authoring once the system ships) |

**Total realistic build:** 1–2 weeks for a polished first pass shipping
the Campfire scene. Most time is M3 (animation polish) and M4 (transition
tuning).

---

## Milestone Breakdown (TDD per §1.4)

### M1 — Engine mode switch
**Invariant (user-visible):** activating SceneViewMode pauses gameplay
and hides the world. Deactivating it resumes gameplay exactly where
it was.

**TDD plan:**
1. RED: write `SceneViewManagerTests` —
   - `Activate_SetsIsActive`
   - `Deactivate_ClearsIsActive`
   - `WhileActive_TurnManager_DoesNotProcessTurns` (set up TurnManager,
     activate SceneView, assert ProcessUntilPlayerTurn returns null)
   - `WhileActive_ZoneRenderer_Paused` (mock ZoneRenderer flag, verify)
2. GREEN: implement `SceneViewManager.Activate(scene)` /
   `Deactivate()`, set `ZoneRenderer.Paused = true/false`, add the
   guard in `TurnManager.ProcessUntilPlayerTurn`.
3. Counter-check (§3.4): `WhileInactive_TurnManager_ProcessesNormally`.

**Files touched:**
- New: `Assets/Scripts/Presentation/SceneViews/SceneViewManager.cs`
- Modified: `Assets/Scripts/Gameplay/Turns/TurnManager.cs` (one-line guard)
- Modified: `Assets/Scripts/Presentation/Input/InputHandler.cs` (one-line short-circuit)
- New: `Assets/Tests/EditMode/Presentation/SceneViews/SceneViewManagerTests.cs`

---

### M2 — Static scene render
**Invariant:** when SceneView is active, the configured scene's static
composition (logs, ground, tent, sky base) renders to the overlay
tilemap with correct colors and characters.

**TDD plan:**
1. RED: snapshot test — feed `SceneRenderer` a fixed `SceneViewData`,
   tick once, assert specific cells have specific glyphs and colors.
2. GREEN: implement `SceneRenderer.RenderFrame(grid, t)`. Hardcode the
   Campfire composition for now — refactor in M5.

**Files touched:**
- New: `Assets/Scripts/Presentation/SceneViews/SceneRenderer.cs`
- New: `Assets/Scripts/Presentation/SceneViews/SceneViewUI.cs` (MonoBehaviour)
- New: `Assets/Tests/EditMode/Presentation/SceneViews/SceneRendererStaticTests.cs`

---

### M3 — Animation port
**Invariant:** flame glyphs, spark particles, star twinkle, crackle
events, and wind gusts behave as the JS prototype does — verifiable
with deterministic seeded RNG.

**TDD plan:**
1. RED: deterministic-frame test —
   - `Frame_0_Seeded_HasExpectedFlameGlyphs`
   - `Spark_LifecycleEnds_AfterMaxAge`
   - `Crackle_RaisesIntensity_Briefly`
   - `WindGust_SkewsFlamesLaterally`
2. GREEN: port flame draw, spark particle list, star twinkle, crackle
   timer, wind-gust timer from the JS. Use `System.Random` seeded
   identically to the JS test for reproducibility.

**Files touched:**
- Modified: `SceneRenderer.cs` (animation logic)
- New: `Assets/Tests/EditMode/Presentation/SceneViews/SceneRendererAnimationTests.cs`

---

### M4 — Dissolve transition
**Invariant:** entering SceneView dissolves the prior tilemap radially
from center over ~1.6 seconds; exiting reverses. Mid-dissolve, partial
mask values produce a soft-edge blend.

**TDD plan:**
1. RED:
   - `Dissolve_Frame0_FullyMasked`
   - `Dissolve_FrameMid_HasSoftEdge`
   - `Dissolve_FrameEnd_FullyRevealed`
   - `Reverse_RestoresMaskMonotonically`
2. GREEN: implement mask `Float[]` + `UpdateDissolve(elapsed)`.
   Composition pass mixes scene with pre-pattern based on per-cell mask.

**Files touched:**
- Modified: `SceneRenderer.cs`
- Modified: `SceneViewManager.cs` (BeginEnter / BeginExit hooks)
- New: `Assets/Tests/EditMode/Presentation/SceneViews/SceneRendererDissolveTests.cs`

---

### M5 — Scene asset format
**Invariant:** Campfire scene is fully described by data (no hardcoded
composition in `SceneRenderer`). Same render output as M3.

**TDD plan:**
1. RED: load `Campfire` SceneViewData asset, render frame, assert
   identical to the M3 hardcoded output.
2. GREEN: define `SceneViewData` ScriptableObject with composition
   layers (logs, tent, trees, ground, sky, flame zone), animation
   params (spark spawn rate, crackle period, wind period), text
   rotation list, prompt list. Refactor `SceneRenderer` to read from
   the asset.

**Files touched:**
- New: `Assets/Scripts/Presentation/SceneViews/SceneViewData.cs`
- New: `Assets/Resources/Content/SceneViews/Campfire.asset`
- Modified: `SceneRenderer.cs` (read from data not hardcoded)
- New: `Assets/Tests/EditMode/Presentation/SceneViews/SceneViewDataLoadTests.cs`

---

### M6 — Campfire entity wiring
**Invariant:** player adjacent to a Campfire entity, presses **L**
(or selects "Look at fire" from action menu), enters the campfire
scene. Press **E** to return.

**TDD plan:**
1. RED: scenario test — spawn Campfire next to player, fire
   `GetInventoryActions` event, assert "Look" action is contributed.
   Execute the action, assert `SceneViewManager.IsActive` is true and
   the active scene is `Campfire`.
2. GREEN: implement `LookAtScenePart` (`SceneID` field). Listens for
   `GetInventoryActions`, adds `LookAt` action. Action handler calls
   `SceneViewManager.Activate(SceneViewData)`.
3. Add `LookAtScene` part to Campfire blueprint with
   `SceneID="Campfire"`.

**Files touched:**
- New: `Assets/Scripts/Gameplay/Entities/Parts/LookAtScenePart.cs`
- Modified: `Assets/Resources/Content/Blueprints/Objects.json`
  (Campfire blueprint adds `LookAtScene` part)
- New: `Assets/Tests/EditMode/Presentation/SceneViews/LookAtScenePartTests.cs`

---

### M7 — Self-review, polish, audit
- §5 self-review pass on all M1–M6 commits
- PlayMode sanity sweep: enter scene, animation runs, exit returns to
  game state preserved
- Update this plan doc with M5 post-review findings table
- Verify performance: `SceneRenderer` should run at 30+ fps on a
  ~80×28 grid (matches existing tilemap render budget)
- Write 1 manual playtest scenario: `Scenarios/Custom/CampfireScene.cs`

---

## Authoring Format — `SceneViewData`

```csharp
[CreateAssetMenu(fileName="Scene", menuName="CavesOfOoo/SceneView")]
public class SceneViewData : ScriptableObject
{
    public string SceneID;          // unique ID, used by LookAtScenePart
    public int Width = 80;
    public int Height = 28;
    public Color BackgroundColor;

    // Static composition layers (rendered in order, layered)
    public List<StaticArtLayer> StaticLayers;

    // Animated zones
    public FlameZoneConfig FlameZone;       // null if no flame
    public StarFieldConfig Stars;            // null if no stars
    public SparkSourceConfig SparkSource;   // null if no sparks

    // Periodic events
    public CrackleConfig Crackle;
    public WindGustConfig WindGust;

    // Text + prompts
    public List<string> AmbientTextLines;
    public float AmbientTextRotateSeconds = 10f;
    public List<ScenePrompt> Prompts;        // [E] RETURN, [R] REST, etc.

    // Transition
    public DissolveConfig EntryDissolve;
    public string PrePatternMode;            // "Tilemap", "JungleNoise", "Black"
}
```

Each scene authored as one `.asset` file. Authoring effort per scene:
~1–2 hours after the system ships.

---

## Verification Checklist

- [ ] `SceneViewManager.IsActive` reflects state correctly
- [ ] `TurnManager` does not advance turns while a scene is active
- [ ] `ZoneRenderer.Paused` is true while scene is active
- [ ] Movement input is suppressed while scene is active
- [ ] `[E]` exits the scene
- [ ] Campfire blueprint contributes a "Look" action
- [ ] Selecting "Look" enters Campfire scene
- [ ] Dissolve transitions play in both directions (entry + exit)
- [ ] Flame, spark, star, crackle, wind animations run at 30+ fps
- [ ] Ambient text rotates at configured interval
- [ ] After exit, player position / zone state / turn count are
      identical to before entry
- [ ] All new EditMode tests pass

---

## Reference: JS Prototype as Spec

The complete behavior is prototyped in
`Docs/Mockups/scene-views/campfire.html`. That file is the authoritative
visual spec. C# port preserves:

- Layer composition (sky / trees / tent / ground / logs / flame /
  sparks / text / prompts)
- Color palette (yellow→orange→red→ember flame gradient)
- Animation parameters (spark spawn rate, crackle period 4–7s, wind
  period 5–9s, text rotation 10s)
- Glyph probability tables for flame intensity tiers
- Dissolve curve (radial from center, ~1.6s, soft-edge blend)

Anything the prototype does that the spec above doesn't capture, treat
the prototype as canonical.

---

## Long-Horizon: Scaling to More Scenes

After Campfire ships, candidate scenes to author next (priority order
based on Caves of Ooo lore importance):

| Scene | Trigger | Why |
|---|---|---|
| **Vista from a high point** | Standing on a cliff/tower entity | Demonstrates scenes don't need flames; tests static composition + parallax |
| **Named NPC face** | First-time encounter with Mogu/Grib/Nam/Sien/Sopp etc. | Pays off the Rot Choir voices doc; scene gives weight to the introduction |
| **First Root contact** | Reaching the deepest cave | Cosmological climax moment; can use the First Root Chamber visual style |
| **Looking at a relic** | Examining a quest item | Tests text-heavy scenes; fewer animation moving parts |
| **Saccharine Concord arrival** | First entering candy biome | Shows scene system handles bright/cheerful aesthetics, not just somber |
| **Palimpsest archive** | Standing in a Palimpsest sanctum | Ties to BlackmailV2 / Ink design — text bleeding through layers |

**Cap**: ~10–15 total scenes ever. More than that and the moments stop
feeling special. Each scene is a deliberate authoring choice, not a
system outcome.

---

## Open Design Questions

1. **Trigger UX.** "Look at campfire" — is this a dedicated `L` key
   (always tries to look at adjacent interesting entity) or a
   per-action menu choice? Recommend: action-list contribution, surfaced
   via existing inventory-actions UI. Same path the `Chat` action uses.
2. **Scene state and re-entry.** If a player re-enters the same campfire,
   does the ambient text resume from where it was, or restart? Recommend:
   restart. Scenes are moments; revisits are new moments.
3. **Sound.** Out of scope for v1.0 — but worth flagging that the
   campfire scene cries out for a low-frequency crackle audio loop.
   Add to a future audio-pass plan.
4. **Day/night state.** Does scene rendering reflect time-of-day from
   the world (sunlight system from the jungle-chunk demo)? Recommend:
   no, scenes are atmospheric snapshots — internal lighting is part of
   the scene's authored mood, not derived from world state.
5. **Companion NPCs.** If the player has a companion, do they appear in
   the campfire scene? Recommend: yes, but as a separate sub-feature
   (`SceneCompanion` slot in `SceneViewData`). Defer until first
   companion ships.
6. **Skipping the dissolve.** Should there be a setting to skip the
   transition for players who find it slow? Yes — add an
   accessibility toggle. Defer to settings menu work.

---

## Progress Log

| Date | Milestone | Status | Notes |
|---|---|---|---|
| 2026-05-04 | Plan | ✅ Drafted | Plan document committed |
| 2026-05-04 | M1 | ✅ Shipped | SceneViewManager static class + 15 EditMode tests. **Scope cut from plan**: TurnManager + InputHandler modifications deferred. Pre-impl verification sweep showed turn flow is naturally gated by input (no movement input → ProcessUntilPlayerTurn isn't called) — same pattern as how conversations work without TurnManager changes. InputHandler changes paired with SceneViewUI in M2 where there's actually something to handle the input. Cleaner cut per §1.3. |
| TBD | M2 | ⏳ Pending | — |
| TBD | M3 | ⏳ Pending | — |
| TBD | M4 | ⏳ Pending | — |
| TBD | M5 | ⏳ Pending | — |
| TBD | M6 | ⏳ Pending | — |
| TBD | M7 | ⏳ Pending | — |
