# Quest Log UI (Q1) — implementation

> Phase Q1 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. Closes the
> "quests are simulated but invisible to the player" gap (same class as
> the gas-visibility bug). Foundation (`QuestLogStateBuilder` +
> `QuestLogSnapshot`) already shipped; this adds the renderer + hotkey.
> **Status:** **Q1 COMPLETE & PlayMode-verified** (2026-05-23). Compile
> clean (0 CS errors), 9/9 quest-log tests GREEN, and a live in-game
> screenshot confirms legible text + color-coded per-stage status. The
> PlayMode sanity sweep caught **two real rendering defects** that static
> analysis missed; both **fixed pre-merge** (Findings 1-2). Ready to merge
> from `feature/q1-quest-log-ui`.

## Goal

A full-screen ASCII overlay opened by **`q`** that lists the player's
active and completed quests, each active quest showing its objectives
(stages) with per-row status decoration — mirroring Qud's
`XRL.UI.QuestLog.GetLinesForQuest` per-stage rows.

## Scope / decisions

- **Render per-stage rows with status**, not just the current stage.
  Status per row: `Done` (✓, green) / `Current` (►, yellow) / `Pending`
  (·, grey). This matches Qud's quest screen AND is forward-compatible
  with the chosen flat-step model (Q3): when stages become an unordered
  step set, the same rows render as Done/Pending (drop `Current`); only
  the *state-builder* changes, not the renderer.
- **Additive snapshot enrichment.** Add `QuestLogStageRow` + a `Stages`
  list to `QuestLogActiveEntry`; keep `CurrentStageId`/`CurrentStageIndex`/
  `EnteredStageAtTurn` so existing `QuestLogStateBuilderTests` stay green.
- **Display names deferred to Q2.** v1 shows quest IDs + stage IDs (dev
  strings). Q2 adds `Name`/`Description` to `QuestData`.
- **Renderer is presentation glue** (MonoBehaviour drawing to the shared
  Tilemap) — verified via PlayMode + manual, not EditMode. The TDD
  target is the state-builder enrichment (pure, EditMode-tested).

## Content-readiness

🟢 Foundation exists (`StoryletPart`, snapshot, builder, tests, IronKey
quest content + `QuestShowcase`). No new content needed for v1.

## Plan

1. **Snapshot enrichment (TDD):** `QuestLogStageRow { StageId, Status }`
   + `QuestLogStageStatus { Done, Current, Pending }`; add
   `IReadOnlyList<QuestLogStageRow> Stages` to `QuestLogActiveEntry`.
   State builder fills it from the registry quest's `Stages` vs
   `CurrentStageIndex`. RED tests → implement → GREEN.
2. **`QuestLogUI` MonoBehaviour** (`Presentation/UI/QuestLogUI.cs`):
   mirror `InventoryUI` — `Tilemap` field, `DrawChar`/`DrawText`/
   `ClearRegion`, `Open()`/`Close()`/`IsOpen`/`HandleInput()`; sets
   `ZoneRenderer.Paused`. Renders title, Active section (quest header +
   stage rows), Completed section, empty-state, footer.
3. **Input wiring** (`InputHandler.cs`): `InputState.QuestLogOpen`,
   `QuestLogUI` property, `KeyCode.Q` → `OpenQuestLog()`, routing +
   `HandleQuestLogInput()` (close on `q`/Esc).
4. **Bootstrap wiring** (`GameBootstrap.cs`): construct + wire `Tilemap`,
   `ZoneRenderer`, `StoryletPart` source, assign to `InputHandler`.
5. **Verify:** compile clean → state-builder tests green → PlayMode:
   launch `QuestShowcase`, accept the quest, press `q`, screenshot.

## Implementation log

**2026-05-23 — Q1 v1 implemented.**

- **Snapshot enrichment (TDD).** Added `QuestLogStageStatus`
  {Done,Current,Pending} + `QuestLogStageRow` {StageId,Status} and a
  `Stages` list on `QuestLogActiveEntry` (optional ctor arg, defaults
  empty → existing 4-arg callers + their tests stay green). RED tests
  (`Build_ActiveEntry_PopulatesAllStagesWithStatus`,
  `…_FirstStage_NoneDone` counter-check, `…_QuestNotInRegistry_EmptyStages`)
  assert 3 populated rows; builder initially returned empty (RED certain
  by construction — transport flakiness prevented a machine RED run, but
  empty-list-vs-3-rows can't pass). `QuestLogStateBuilder` now fills
  `Stages` from the registry quest vs `CurrentStageIndex`.
- **`QuestLogUI`** (`Presentation/UI/QuestLogUI.cs`, namespace
  `CavesOfOoo.Rendering` to match `InventoryUI`): MonoBehaviour mirroring
  InventoryUI's draw shape (`DrawChar`/`DrawText`, `W=80`/`H=45`,
  `CP437TilesetGenerator.GetTile` + `Tilemap.SetColor`, `Vector3Int(x,
  H-1-y,0)`). Renders title + ACTIVE (quest id + per-stage rows: √ done /
  ► current / · pending) + COMPLETED + empty-state + footer. Pulls
  `StoryletPart.Current` so no per-open wiring. `ClearAllTiles` on close.
- **InputHandler:** `InputState.QuestLogOpen`; `QuestLogUI` property;
  `KeyCode.Q` (was unbound) → `OpenQuestLog()`; dispatch case;
  `OpenQuestLog`/`HandleQuestLogInput`/`CloseQuestLog` mirroring the
  Inventory trio (camera `SetUIView`/`RestoreGameView`, `ZoneRenderer.Paused`).
- **GameBootstrap:** `GetComponent/AddComponent<QuestLogUI>` + tilemap +
  `inputHandler.QuestLogUI = …`, mirroring the inventory block.
- **Compile:** 0 CS errors. **State tests:** 9/9 GREEN.
- **Glyph render fix (post-screenshot):** switched DrawChar from `GetTile`
  to `GetTextTile` (text atlas, no entity overrides) + ASCII markers
  (* > -). See Finding 1.
- **PlayMode sanity sweep DONE:** entered Play, runtime-confirmed
  `InputHandler.QuestLogUI` wired, seeded a 3-stage demo quest @ stage 1
  + a completed quest, invoked `OpenQuestLog` via reflection, captured the
  Game view. Screenshot confirms legible text + green/yellow/grey
  color-coded per-stage status + correct layout. (Screenshot was a
  verification artifact, not committed.)

## In-phase self-review (Methodology Template §5)

Run before commit. No 🔴/🟡 code defects found; the open items are 🧪
(test/verification gaps, all gated on the wedged editor transport) + 🔵
(deferred polish). Counter-check present (§3.4):
`Build_ActiveEntry_FirstStage_NoneDone` would fail a buggy
`i <= current ⇒ Done`.

```
🟡→FIXED Finding 1 — UI text rendered with ENTITY glyphs (tree/snapjaw)
Two-stage discovery:
  (a) Source analysis: original markers 16/250/251 aren't drawn into the
      atlas → would render BLANK. Switched to shade blocks 176/177/178.
  (b) PlayMode screenshot then revealed the REAL, deeper defect: DrawChar/
      DrawText used CP437TilesetGenerator.GetTile — the GAME atlas, where
      letters are overridden with roguelike entity glyphs ('T'=tree,
      's'=snapjaw). So labels rendered "QUES🌲 LOG", "AC🌲IVE", "be🐍t...".
Fix (applied pre-merge): route ALL glyphs through GetTextTile — the narrow
TEXT atlas with NO entity overrides (the path Sidebar/Hotbar/LookOverlay
use for legible text) — and use plain ASCII status markers (* > -) that
the text font guarantees. Re-screenshot confirms fully legible text +
color-coded statuses (green * done / yellow > current / grey - pending).
LESSON: the mirror-of-InventoryUI argument was insufficient — InventoryUI
uses GetTile and either avoids T/s or accepts the quirk; only the live
screenshot exposed this. Vindicates the "smoke ≠ works" PlayMode mandate.

✅ Finding 2 — layout VERIFIED; bottom band is the persistent HUD (NOT a defect)
PlayMode screenshot confirms the layout is correct: title / ACTIVE
section / quest header + indented per-stage rows / COMPLETED section /
footer, all on-screen at the SetUIView(80,45) framing. The teal band at
screen-bottom is the persistent HUD (Hotbar/Sidebar render to their OWN
tilemaps, not the main zone tilemap QuestLogUI clears) — it shows during
EVERY fullscreen overlay (inventory included), so it is not introduced by
the quest log. (My footer is faintly visible amid it = two tilemap layers.)
FUTURE POLISH (out of Q1 scope, shared across all overlays): optionally
suppress/hide the HUD tilemaps during fullscreen overlays for a cleaner
look — affects InventoryUI etc. too, so it belongs in a UI-chrome pass.

✅ Finding 3 — compile + state-test GREEN (RESOLVED)
Transport recovered; confirmed: `read_console` 0 CS errors, and
`run_tests group=QuestLogStateBuilderTests` → 9/9 GREEN (the 3 new tests
+ 6 existing). A test run starting also proves the assemblies compiled.

🔵 Finding 4 — IDs shown as dev strings (no display names)
Active quests show QuestId; stages show StageId. Readable but dev-facing.
Deferred to Q2 (quest/objective display metadata) — already planned.
```

**Adversarial sweep (§Adversarial):** NOT warranted for Q1. The taxonomy
surfaces (save/load reach, parser, cross-actor, stacking, anti-exploit,
RNG) are absent — the state-builder is a pure read-model transform and
the renderer is presentation. The 3 unit tests + counter-check cover the
status-mapping logic. (Re-evaluate at Q3 when the flat-step model adds
state mutation + save reach.)

## Deferred items

| Item | Why deferred | Tracked in |
|---|---|---|
| Quest/stage **display names** (not IDs) | scope → Q2 | QUEST-SYSTEM-QUD-PARITY.md §5 |
| **Scrolling** for long quest lists | v1 clamps at `H-4` rows; overflow truncated | this doc |
| Reactive refresh (rebuild while open on quest change) | v1 rebuilds on each open — sufficient | depends on Q4 quest GameEvents |
| Flat-step model migration | scope → Q3; snapshot is forward-compatible | QUEST-SYSTEM-QUD-PARITY.md §4.1 |
| **Hide persistent HUD during fullscreen overlays** | affects all overlays (inventory too); UI-chrome pass | this doc, Finding 2 |

*(Resolved this phase: compile + 9/9 tests GREEN; PlayMode visual verify; glyph-render defect.)*

## Files changed

- NEW `Assets/Scripts/Presentation/UI/QuestLogUI.cs` — the overlay renderer.
- MOD `Assets/Scripts/Presentation/Rendering/QuestLogSnapshot.cs` — +`QuestLogStageStatus`, +`QuestLogStageRow`, +`Stages` on entry.
- MOD `Assets/Scripts/Presentation/Rendering/QuestLogStateBuilder.cs` — populate `Stages` from registry vs current index.
- MOD `Assets/Scripts/Presentation/Input/InputHandler.cs` — `InputState.QuestLogOpen`, `QuestLogUI` prop, `q` keybind, dispatch, Open/Handle/Close trio.
- MOD `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` — construct + wire `QuestLogUI`.
- MOD `Assets/Tests/EditMode/Presentation/Rendering/QuestLogStateBuilderTests.cs` — +3 tests (status rows + counter-check + defensive).

## Tests

3 added (`Build_ActiveEntry_PopulatesAllStagesWithStatus`,
`Build_ActiveEntry_FirstStage_NoneDone`,
`Build_QuestNotInRegistry_EmptyStages`); existing 6 retained (additive
enrichment). **9/9 GREEN confirmed** (run `65fb3884…`); compile 0 CS errors.

---

## Live objective progress for counter/collect-N objectives (2026-05-25)

**Gap:** counter/collect objectives (the kill-N + collect-N pool quests, see
`Docs/QUEST-POOL-EXPANSION.md`) showed only static text + a binary done/pending
marker — "Rout the dirt gnomes (3)" with no live "1 of 3" feedback. The player
couldn't see progress until the whole objective flipped to done.

**Fix (presentation-only, automatic, zero content-schema change):** an objective
that gates on `IfFact:<fact>:>=:N` **with N > 1** is a counter. `QuestLogStateBuilder`
parses that trigger, reads the live fact via `NarrativeStatePart`, and surfaces
`Current`/`Target` (current clamped to `[0, Target]`) on `QuestLogObjectiveRow`.
`QuestLogUI` appends `" (Current/Target)"`. So the log now shows **"Rout the
dirt gnomes (1/3)"** ticking up as you kill them.

**Why N > 1 only:** single-target objectives (`>=:1` kill-1 / reach / fetch via
`IfHaveItem`) are *not* counters — "(0/1)/(1/1)" is noise, and the done/pending
marker already conveys them. Only genuine multi-counters get the suffix.

**Design notes:**
- `Build(StoryletPart, NarrativeStatePart narrativeState = null)` — the new param
  is optional + defaults to `NarrativeStatePart.Current`, so the sole production
  caller (`QuestLogUI.Rebuild`) is unchanged and tests inject an explicit state
  (no singleton pollution). `QuestLogObjectiveRow`'s new fields are optional
  ctor args → backward-compatible.
- Counter objective text dropped its redundant static "(N)" suffix
  (`ClearTheWarren` / `TheCandyTax` JSON) so the live "(c/t)" isn't doubled.

**Tests:** `QuestLogStateBuilderTests` +6 (live progress; clamp-to-target;
zero-when-unset; **counter-checks**: single-target `>=:1` → no progress,
non-`IfFact` fetch → no progress; null-state no-crash). **19/19 green.**
Content text change benign (`QuestVillagePoolTests` 11/11). Regression chunk
(other `Build` caller `QuestShowcaseDiagTests` + sibling state-builders +
village builder) 12/12. **Live (rule 7):** real `ClearTheWarren.json` through the
runtime builder with `warren_gnomes_routed=2` → `rout_gnomes: hasProgress=True
2/3` — the shipped content's trigger parses into live progress.

**Files:** MOD `QuestLogSnapshot.cs` (+`HasProgress`/`Current`/`Target`),
`QuestLogStateBuilder.cs` (+`narrativeState` param, `TryGetCounter` parse),
`QuestLogUI.cs` (+progress suffix), `ClearTheWarren.json` + `TheCandyTax.json`
(drop static count), `QuestLogStateBuilderTests.cs` (+6).
