# World Action Menu — Implementation Plan

Qud-parity feature: when the player clicks any cell in look mode, a context
menu appears listing actions they can take on whatever is in that cell. Open
for chests, Chat for NPCs, Examine for everything, plus any entity-specific
actions declared by parts.

---

## 1. How Qud does it (reference study)

Qud's menu lives at `Qud.API.EquipmentAPI.TwiddleObject` + `ShowInventoryActionMenu`.
The flow:

### 1.1 Dispatch

```
player points at an object, presses Space / clicks "Interact"
     ↓
GameObject.Twiddle()                                         [XRL.World/GameObject.cs:7115]
     ↓
EquipmentAPI.TwiddleObject(Owner, GO, ...)                  [Qud.API/EquipmentAPI.cs:151]
     ↓
Fires 3 events on the object:
  - "GetInventoryActions"       (most parts handle this)
  - "GetInventoryActionsAlways" (always-firing variant)
  - "OwnerGetInventoryActions"  (fired on the acting player)
     ↓
Each part registered for those events calls E.AddAction(...) to push items
into a Dictionary<string, InventoryAction>
     ↓
ShowInventoryActionMenu(dictionary, ...)                    [line 30]
     ↓
Popup.PickOption(title, intro, options, hotkeys, ...)       [XRL.UI/Popup.cs:1643]
     ↓
Returns the selected InventoryAction
     ↓
InventoryAction.Process(GO, Owner)                          [line 118]
     ↓
Fires the Command event back on the object (e.g. "Chat", "Look", "Open")
     ↓
Part handles the command
```

### 1.2 InventoryAction fields ([XRL.World/InventoryAction.cs])

```
Name, Key (hotkey), Display, Command, Default (priority), Priority (sort),
FireOnActor, WorksAtDistance, WorksTelekinetically, WorksTelepathically,
AsMinEvent, FireOn, ReturnToModernUI
```

The `Comparer` sorts by priority then alphabetical; the `Default`-scored entry
is pre-selected in the menu.

### 1.3 The two default actions

**Look (Examine)** — added by `Description` part:
```csharp
// XRL.World.Parts/Description.cs:324
public override bool HandleEvent(GetInventoryActionsEvent E) {
    if (Visible()) {
        E.AddAction("Look", "look", "Look", null, 'l',
                    FireOnActor: false, 0, 0,
                    Override: false, WorksAtDistance: true);
    }
    return base.HandleEvent(E);
}
```

When "Look" is selected, Description's `HandleEvent(InventoryActionEvent E)`
fires: shows a popup with `tooltipInformation.LongDescription`, supports
"Recall Story" for story-bearing objects. Also fires `LookedAt` and
`AfterLookedAt` events for broader reactivity.

**Chat (Talk)** — added by `ConversationScript` part:
```csharp
// XRL.World.Parts/ConversationScript.cs:210
public override bool HandleEvent(GetInventoryActionsEvent E) {
    if (ParentObject != E.Actor) {
        E.AddAction("Chat", "chat", "Chat", null, 'h',
                    FireOnActor: false, 10, 0,
                    Override: false, WorksAtDistance: false,
                    WorksTelekinetically: false, WorksTelepathically: true);
    }
    return base.HandleEvent(E);
}
```

Priority `10` = high (appears near the top, is often the default-selected).
`WorksTelepathically: true` means Chat reaches telepathic targets beyond
normal range. When selected, fires `Chat` → `AttemptConversation(...)`.

### 1.4 Menu rendering

`Popup.PickOption` is Qud's generic option-picker. Signature:

```csharp
public static int PickOption(
    string Title, string Intro, string SpacingText, string Sound,
    IReadOnlyList<string> Options, IReadOnlyList<char> Hotkeys,
    IReadOnlyList<IRenderable> Icons, IReadOnlyList<QudMenuItem> Buttons,
    GameObject Context, IRenderable IntroIcon,
    Action<int> OnResult, int Spacing, int MaxWidth,
    int DefaultSelected, int IconPosition,
    bool AllowEscape, bool RespectOptionNewlines,
    bool CenterIntro, bool CenterIntroIcon,
    bool ForceNewPopup,
    Location2D PopupLocation, string PopupID)
```

Renders centered, with:
- Title bar (empty for twiddle)
- Intro text (object DisplayName if player is confused)
- Icon (object's RenderForUI rendering)
- Scrollable option list, each formatted `{hotkey}) {display}`
- Default selection highlighted
- Hotkey or up/down+Enter to choose; Escape cancels (returns `-1`)

Hotkeys are chosen automatically (see `ApplyHotkey`): prefer a letter that
appears in the display text, lowercase first, uppercase fallback for collisions.

---

## 2. What we already have

| Piece | Location | Notes |
|-------|----------|-------|
| `InventoryAction` | `Assets/Scripts/Gameplay/Inventory/InventoryAction.cs:11` | Mirrors Qud's shape minus Telekinesis/Telepathy niceties. Ready to use. |
| `InventoryActionList` | same file, line 51 | `AddAction(name, display, command, key, priority, fireOnActor)` + `Sort()`. Ready to use. |
| `GetInventoryActions` event | fired on inventory items today | `ContainerPart`, etc. already respond. Needs to also fire on ground entities. |
| `InventoryAction` event | dispatched by commands | `ContainerPart.HandleEvent` for `OpenContainer`, etc. Existing infra we reuse. |
| Look mode | `InputHandler.HandleLookModeInput` | Entry via `L`, cursor via WASD/arrows, currently opens throw popup on Enter/click. |
| `ConversationPart` | handles dialogue | Already on NPCs that can talk. Doesn't currently expose a "Chat" action. |
| `ContainerPart` | chests, corpses | Already declares "Open"/"Unlock" actions on `GetInventoryActions`. |
| Entity `GetDisplayName()` | `Entity.cs:391` | Returns render's DisplayName + stack count. Enough for "pile of items, including X, Y, Z". |
| `MessageLog.Add` | game-wide logging | For simple Examine text until we add a proper description popup. |

### Gaps (what's missing)

1. **No "twiddle/interact" dispatcher** — there's no `EquipmentAPI.TwiddleObject` analog. The event flow exists (`GetInventoryActions` + `InventoryAction`) but only inventory items trigger it.
2. **No entity-level `Description`** — we have `DisplayName` but no long-description field. Examine would currently have to synthesize a short line from what we know.
3. **No menu UI** — we have `ItemActionPopup` embedded inside `InventoryUI`, but it's coupled to the inventory screen. No `Popup.PickOption`-equivalent generic menu.
4. **No "Examine" default on anything** — no part yet adds it.
5. **No "Chat" default on NPCs** — `ConversationPart` works but doesn't declare a Chat action.

---

## 3. Design

Mirror Qud's pattern carefully but keep our surface smaller:

### 3.1 The dispatcher

New: `WorldInteractionSystem.ShowActionMenu(Entity actor, Entity target, ...)`

- Fires `GetInventoryActions` on `target` with an `InventoryActionList` parameter
- If the list is empty → falls back to a synthetic "Examine" action
- Sorts the list via `InventoryActionList.Sort()`
- Opens the `WorldActionMenuUI` with the list
- On selection → fires `InventoryAction` event with the chosen `Command`
- Escape cancels cleanly

### 3.2 Default actions

**Examine (universal)** — a new `ExaminablePart` (or synthesize if missing) that:

```csharp
public override bool HandleEvent(GameEvent e) {
    if (e.ID == "GetInventoryActions") {
        var actions = e.GetParameter<InventoryActionList>("Actions");
        actions?.AddAction("Examine", "examine", "Examine", 'x',
                           priority: 0);
    } else if (e.ID == "InventoryAction" &&
               e.GetStringParameter("Command") == "Examine") {
        var actor = e.GetParameter<Entity>("Actor");
        MessageLog.Add(GenerateExamineText());
        e.Handled = true;
        return false;
    }
    return true;
}
```

Where `GenerateExamineText()` uses:
- `entity.GetPart<DescriptionPart>()?.Text` if we add a Description part
- Else `"You see {DisplayName}."`

**Synthesized Examine for cells without any interactable entity** — if a cell
has only terrain, the dispatcher short-circuits:
- Single non-terrain entity → `"You see {name}."` + its description
- Multiple non-terrain entities → `"A pile of items, including: a, b, c."`
- Only terrain (floor/wall) → `"You see the {terrain's DisplayName}."`

**Chat** — add to `ConversationPart`:

```csharp
public override bool HandleEvent(GameEvent e) {
    if (e.ID == "GetInventoryActions") {
        var actions = e.GetParameter<InventoryActionList>("Actions");
        actions?.AddAction("Chat", "talk", "Chat", 'c', priority: 10);
    } else if (e.ID == "InventoryAction" &&
               e.GetStringParameter("Command") == "Chat") {
        var actor = e.GetParameter<Entity>("Actor");
        ConversationManager.StartConversation(ParentEntity, actor);
        e.Handled = true;
        return false;
    }
    return true;
}
```

NPCs without a real `ConversationID` (current fallback is the "hi" default
per user request) — `ConversationManager.StartConversation` already falls
back; if it doesn't, we add a default-greeting path that emits "hi" as a
one-off `MessageLog` line.

### 3.3 Targeting: which entity gets queried when player clicks a cell?

A cell can contain multiple entities: a NPC standing on grass with a dropped
dagger underneath. We need a rule.

**Pick rule** (matches Qud's intuition — "thing you're most likely to want to interact with"):

1. Iterate `cell.Objects` in descending `RenderLayer` order (top visible first)
2. If top visible has a `GetInventoryActions` responder → that's the target
3. Else, fall back to the synthesized cell-level "Examine" (pile summary)

This means clicking the cell with a NPC-on-grass queries the NPC (Chat, Examine);
clicking a cell with just a dagger queries the dagger (Examine, Pick Up);
clicking a cell with only grass gets the synthesized "You see grass." line.

### 3.4 The menu UI — `WorldActionMenuUI`

New `MonoBehaviour`. Pattern mirrors `PickupUI` / `ContainerPickerUI` (already
in our codebase) since those are the closest existing popup analogs.

**Properties:**
- `Tilemap Tilemap` — foreground tilemap (assigned by GameBootstrap, same as InventoryUI)
- Optional `Camera PopupCamera`
- Private state: `Entity Target`, `List<InventoryAction> Actions`, `int CursorIndex`, `bool IsOpen`

**Public API:**
- `void Open(Entity actor, Entity target, List<InventoryAction> actions)` — sets state, renders
- `void Close()` — hides, clears state
- `bool IsOpen { get; }`
- `InventoryAction PollSelection()` — returns the action picked this frame (null if none yet)

**Rendering** (tilemap-based, matches existing style):
- Centered box: width clamped to max of (DisplayName length, longest action, 20), height = actions + 4
- Title row: the target's DisplayName in bright yellow
- Separator row
- Action rows: `> a) open` / `  b) examine` etc. Cursor marked with `>`.
- Hotkey in a distinct color (white for available letters)
- Border drawn with standard box glyphs (`┌`, `│`, `└`, etc.)

**Input handling** (read once per frame from `InputHandler`):
- `Up`/`Down` or `W`/`S` → move cursor
- `Enter` → select cursor action
- Hotkey letter matching any action's `Key` → select that action
- `Escape` → cancel

### 3.5 InputHandler wiring

1. New `InputState.WorldActionMenu`.
2. In `HandleLookModeInput`, `Enter`/left-click on a cell:
   - Resolve target entity via the pick rule
   - Gather actions via `GetInventoryActions` event
   - If no actions and no entity → synthesize cell-level Examine text; stay in look mode (don't open menu for trivial case) OR open a 1-item menu with just Examine — pick one. Plan recommends: always open the menu so behavior is uniform; single "Examine" menu for empty cells is still a valid interaction.
   - Call `WorldActionMenuUI.Open(player, target, actions)`
   - Transition `InputState → WorldActionMenu`
3. New `HandleWorldActionMenuInput()`:
   - Poll the menu for a selection each frame
   - On selection: fire `InventoryAction` event with selected `Command` on target, close menu, return to `InputState.Normal`
   - On cancel: close menu, return to `InputState.LookMode` (let player re-aim without restarting look)

### 3.6 The "pile" summary for Examine

When the resolved target is synthesized (cell has multiple non-terrain objects):

```csharp
private string ExamineCellSummary(Cell cell) {
    var visible = new List<Entity>();
    foreach (var e in cell.Objects)
        if (!e.HasTag("Wall") && !e.HasTag("Floor") && !e.HasTag("Terrain"))
            visible.Add(e);

    if (visible.Count == 0)
        return $"You see the {GetTerrainName(cell)}.";

    if (visible.Count == 1)
        return $"You see {visible[0].GetDisplayName()}.";

    var names = string.Join(", ", visible.Select(e => e.GetDisplayName()));
    return $"A pile of items, including: {names}.";
}
```

---

## 4. Implementation sequence (concrete commits)

### 4a — Generic Examine action + `GetInventoryActions` on ground entities

**Files:**
- `Assets/Scripts/Gameplay/Entities/Entity.cs` — optional: add a `GetDescription()` helper that returns either a DescriptionPart's Text (if we add one) or a default like "You see {name}.".
- New: `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs` — adds Examine action, handles the command by logging the description. Attach to ALL entities via a base blueprint (e.g., put on `PhysicalObject`).
- Blueprint: `Assets/Resources/Content/Blueprints/Objects.json` — add `Examinable` part to `PhysicalObject` so it cascades to every item, creature, chest, etc.

**Tests:**
- Part handles GetInventoryActions → adds Examine
- Examine command fires → MessageLog contains "You see ..."
- Every blueprint inheriting PhysicalObject exposes Examine (sample: Chest, Snapjaw, Warden, HealingTonic)

**Scope:** ~80 lines + 1 blueprint change. 30 min.

### 4b — Chat action on `ConversationPart`

**Files:**
- `Assets/Scripts/Gameplay/Conversations/ConversationPart.cs` — add `HandleEvent(GetInventoryActions)` + `HandleEvent(InventoryAction)` paths per design above.
- Default-greeting fallback in `ConversationManager.StartConversation`: if the NPC has a `ConversationPart` but no valid ConversationID → log `"{Name} says: 'Hi.'"` and return true (interaction "succeeded" from a menu perspective).

**Tests:**
- NPC with ConversationPart → has Chat action
- Chat command on NPC with ConversationID → starts dialogue
- Chat command on NPC WITHOUT ConversationID → logs "hi" greeting, no crash
- Bonus: Snapjaw (no ConversationPart) → no Chat action

**Scope:** ~60 lines. 30 min.

### 4c — `WorldInteractionSystem` dispatcher

**Files:**
- New: `Assets/Scripts/Gameplay/Core/WorldInteractionSystem.cs` — `ResolveTarget(cell)` + `GatherActions(target)` + `ExamineCellSummary(cell)` helpers.
- Unit-testable without UI: returns the list + target.

**Tests:**
- ResolveTarget picks top render-layer entity with GetInventoryActions responder
- ResolveTarget with only terrain → returns null (synthesized Examine path)
- GatherActions fires event, returns sorted list
- ExamineCellSummary: empty cell (terrain only), single-item cell, pile
- Integration: Cell with Chest → returns Chest + [Open, Examine]

**Scope:** ~150 lines + tests. 1 hour.

### 4d — `WorldActionMenuUI` MonoBehaviour + InputHandler integration

**Files:**
- New: `Assets/Scripts/Presentation/UI/WorldActionMenuUI.cs` — pattern mirroring `PickupUI`. Render + input + state.
- `Assets/Scripts/Presentation/Input/InputHandler.cs` — add `InputState.WorldActionMenu`, wire look-mode Enter/click to open the menu, add `HandleWorldActionMenuInput()`.
- `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` — `AddComponent<WorldActionMenuUI>()` + wire Tilemap reference (same pattern as PickupUI/ContainerPickerUI).

**Tests:**
- Manual: click a chest in look mode → menu appears with Open + Examine
- Manual: click an NPC → menu appears with Chat + Examine
- Manual: click empty floor → menu appears with just Examine (or bypass menu, show message inline — design decision flagged for the commit to settle)
- Manual: Escape → menu closes, returns to look mode
- Manual: Hotkey letter → selects that action

**Scope:** ~400 lines. 2 hours.

### 4e — Docs + README update

**Files:**
- `Assets/Scripts/Scenarios/README.md` — brief note about the new interaction flow (players can use 'c'+direction OR look-mode+click)
- Update `CLAUDE.md` memory with the new architecture pointer

**Scope:** 15 min.

---

## 5. Things explicitly out of scope for v1

- **Distant actions** — Qud's `WorksAtDistance` / `WorksTelekinetically` / `WorksTelepathically`. Our click-target is always visible in the same zone; distance modifiers can come later if needed.
- **Per-action icons** in the menu. Start text-only.
- **Sorting preference configuration** — use the existing `InventoryActionList.Sort()`.
- **Multi-action loops** (Qud's outer `while(true)` that keeps the menu open so you can do multiple actions in a row). Our v1 closes after one selection.
- **Mouse-over preview / tooltip** on menu items. Keyboard + click-to-select only.
- **Action-menu hotkey bindings in Keymap settings.** Hard-coded for now.

---

## 6. Risks

| Risk | Mitigation |
|------|------------|
| Adding `ExaminablePart` to `PhysicalObject` cascades to literally every blueprint including walls. | Check `HasTag("Wall"/"Floor"/"Terrain")` in the part itself and skip declaring Examine for pure terrain. Separate synthesized cell-Examine handles that case. |
| Menu UI pattern duplication with ItemActionPopup (InventoryUI). | Accept for v1; the two live in different contexts (inventory screen vs look-mode overlay). Refactor to share only if pain emerges. |
| `Popup.PickOption`-equivalent UI is non-trivial to build from scratch. | Copy the style of `ContainerPickerUI` (already renders a list with hotkeys in the same codebase). Use it as a template. |
| Event coupling: lots of parts start hooking `GetInventoryActions`. | This is fine — that's the point of the pattern. Keep action-adding logic minimal per part; if it grows, factor into shared helper. |
| Synthesized Examine for terrain needs terrain-description strings we don't have. | Fall back to `DisplayName` ("floor", "stone wall") from the terrain entity's RenderPart. |

---

## 7. Acceptance criteria

- [ ] Look mode + Enter/click on a Chest → menu with "Open" and "Examine"
- [ ] Selecting "Open" opens the chest, same as the 'c'+direction path
- [ ] Look mode + Enter/click on a Snapjaw → menu with "Examine" only (no Chat, no ConversationPart)
- [ ] Look mode + Enter/click on a Scribe (has ConversationPart) → menu with "Chat" and "Examine"
- [ ] Selecting "Chat" on an NPC without a real ConversationID → NPC says "Hi." in message log
- [ ] Look mode + Enter/click on empty floor → message log: `"You see the floor."` (or single-option menu with Examine)
- [ ] Look mode + Enter/click on cell with 3 dropped items → message log: `"A pile of items, including: dagger, leather helmet, torch."`
- [ ] Escape during menu → returns to look mode, no action consumed
- [ ] Hotkey selection works
- [ ] Existing tests all still pass (no regressions)
- [ ] New tests cover each piece: ExaminablePart, Chat action, ResolveTarget rule, cell summary

---

## 8. Out-of-order reading: the current Piece A path

The 'c'+direction path (commit `018c3e8`) ALSO works independent of this plan —
it short-circuits to the container's existing InventoryAction handler. Once the
action menu lands, 'c'+direction can be updated to go through the same dispatcher
for consistency (single "what happens when you interact with X" codepath), but
it's not required.

---

## Cross-refs

- Qud: `Qud.API.EquipmentAPI.TwiddleObject` + `ShowInventoryActionMenu`
- Qud: `XRL.World.InventoryAction` + `InventoryActionEvent`
- Qud: `XRL.UI.Look.cs` Space/click → Twiddle dispatch
- Qud: `Description.cs` (adds "Look") + `ConversationScript.cs` (adds "Chat")
- Ours: `InventoryAction` + `InventoryActionList` (ready to reuse)
- Ours: `ContainerPart` (already declares Open/Unlock via GetInventoryActions)
- Ours: `ItemActionPopup` embedded in `InventoryUI.cs:2761` (reference for popup style)
- Ours: `ContainerPickerUI` / `PickupUI` (reference for standalone popup MonoBehaviour)
