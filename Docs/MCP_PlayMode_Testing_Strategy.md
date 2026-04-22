# Caves of Ooo: MCP PlayMode Testing Strategy

A comprehensive guide for an LLM to use the Unity MCP `manage_input` tool to test gameplay in Caves of Ooo during Play Mode.

## Prerequisites

- MCP for Unity package with `manage_input` tool (gameplay group enabled)
- `InputHelper.cs` bridge installed — ALL keyboard input across the game (InputHandler + all UI scripts) must go through `InputHelper.GetKeyDown`/`GetKey`/`GetKeyUp` instead of raw `Input.GetKeyDown`. This is required because MCP input simulation injects via the New Input System, which the legacy `Input` class does not see.
- Unity Input System package (`com.unity.inputsystem`) installed
- Player Settings: Active Input Handling = "Both"
- `CavesOfOoo.asmdef` must reference `Unity.InputSystem`

## Critical Rules

1. **NEVER fire game events directly via `execute_code`** (e.g., `entity.FireEvent()`, `TurnManager.EndTurn()`). This bypasses the UI flow, leaves popups orphaned, and corrupts game state. Only use `execute_code` for **read-only observation** of game state.
2. **ALL gameplay actions must go through keyboard input** via `manage_input` — mimicking what a real player would do.
3. **Use `move_to` and `query_surroundings`** for navigation and observation — these are MCP-only helpers that handle pathfinding and state queries safely.
4. **Always check for announcement popups** after actions that trigger them (reading grimoires, zone transitions, level-ups). Press Enter/Space/Escape to dismiss.

## Game Overview

Caves of Ooo is a turn-based ASCII roguelike. The player moves on an 80x25 grid, explores zones connected by stairs, fights monsters, picks up items, equips gear, learns mutations from grimoires, crafts at settlement sites, and trades with NPCs.

Key constraint: **this is turn-based**. Each key press is one action. The game processes NPC turns between player inputs. There is a `MoveRepeatDelay` of 0.12s that throttles inputs.

## InputState Machine

The game has a state machine that determines which keys do what. Before pressing any key, know which state you're in:

| State | Context | Valid Keys |
|-------|---------|------------|
| `Normal` | Standard gameplay | WASD (move), G (pickup), I (inventory), L (look), F (factions), C (talk), 1-9 (abilities), Period (wait/stairs) |
| `PickupOpen` | Pickup popup showing items | a-z (select), Enter/Space (take), Tab (take all), Escape/G (close) |
| `ContainerPickerOpen` | Choosing which container | Up/Down/J/K (navigate), Enter/Space (select), Escape/G (cancel) |
| `InventoryOpen` | Inventory UI | Tab (switch tabs: Equipment -> Inventory -> Tinkering -> Abilities), J/K/Up/Down (navigate items), Enter (action menu), Escape/I (close) |
| `AnnouncementOpen` | Popup modal (grimoire read, etc.) | Enter, Space, or Escape (dismiss) |
| `AwaitingDirection` | Ability targeting | WASD/arrows/numpad/hjklyubn (direction), Escape (cancel) |
| `DialogueOpen` | NPC dialogue | Up/Down/J/K (navigate options), Enter (select), Escape (close) |
| `TradeOpen` | Trade UI | Left/Right (switch panels), Up/Down/J/K (navigate), Enter (buy/sell), Escape (close) |
| `LookMode` | Examining tiles | WASD/arrows (move cursor), Escape (exit) |
| `ThrowTargeting` | Throw targeting | WASD/arrows (aim), Enter (throw), Escape (cancel) |

## MCP Navigation Actions (use these instead of manual WASD)

### `move_to` -- pathfind to a target
```
manage_input(action="move_to", target="chest")       -- by entity name
manage_input(action="move_to", x=43, y=11)           -- by coordinates
manage_input(action="move_to", target="elder", max_steps=20)
```
Uses BFS pathfinding on the zone grid. Executes movement step by step with proper turn processing and renderer refresh. Stops if path is blocked.

### `query_surroundings` -- observe game state without screenshots
```
manage_input(action="query_surroundings", radius=8)
```
Returns: player position, stats (HP, STR, AGI, etc.), nearby creatures (with faction, hostility, HP), nearby items (with takeable flag, grimoire info), structures, and an ASCII mini-map.

### `wait_turns` -- pass time
```
manage_input(action="wait_turns", count=5)
```

## Complete Keystroke Flows

### Flow 1: Pick Up Items from Chest

The chest uses `ContainerPart` (not ground items). Player must stand ON the chest tile.

```
1. move_to(target="chest")       -- pathfind adjacent to chest
2. move_to(x=CHEST_X, y=CHEST_Y) -- step onto chest tile (chest is not Solid)
3. key_press("G")                -- triggers TryPickupItem() -> auto-takes all from single container
4. (Items transferred to inventory, turn consumed)
5. Check: query_surroundings to verify items in inventory
```

If multiple containers on one tile, a ContainerPickerUI opens:
```
4. Up/Down to select container
5. Enter to confirm
```

### Flow 2: Pick Up Ground Items

Items on the ground (not in containers) use PickupUI.

```
1. move_to the item's tile
2. key_press("G")
   - Single item: auto-pickup, no UI
   - Multiple items: PickupUI opens
3. If PickupUI open:
   - a-z letter keys to select specific item
   - Tab to take all
   - Escape to close without taking
```

### Flow 3: Read a Grimoire (Learn Mutation/Knowledge)

Must be done from the Inventory UI:

```
1. key_press("I")               -- open inventory (starts on Equipment tab)
2. key_press("Tab")             -- switch to Inventory tab (list of carried items)
3. key_press("J") x N           -- navigate down to the grimoire
4. key_press("Enter")           -- open item action popup
5. key_press("R")               -- select "Read" action (hotkey 'r')
6. key_press("Escape")          -- close inventory
7. ANNOUNCEMENT POPUP appears on next frame
8. key_press("Enter")           -- dismiss announcement popup
9. (Repeat step 8 for each pending announcement)
```

**Critical: Announcement popups queue up.** If you read multiple grimoires, each one queues an announcement. They appear one at a time after closing the inventory. Press Enter for EACH one. Use `execute_code` (read-only) to check:
```csharp
var ui = Object.FindAnyObjectByType<CavesOfOoo.Rendering.AnnouncementUI>();
return $"Popup open: {ui?.IsOpen}";
```

### Flow 4: Cast a Spell (Directional Ability)

Spells auto-bind to hotbar slots when learned. Starting mutations fill slots 0-6 (keys 1-7), grimoire spells fill slots 7-9 (keys 8-0).

```
1. key_press("8")               -- activate Kindle (slot 7 = key 8)
   - Game enters AwaitingDirection state
   - Log shows: "Kindle - choose a direction."
2. key_press("W")               -- aim north
   - Projectile fires, damage resolves, cooldown starts
   - Turn ends, NPCs act
```

For self-centered abilities (no direction needed), pressing the number key fires immediately.

Key-to-slot mapping:
| Key | Slot Index | Default Mutation |
|-----|-----------|-----------------|
| 1 | 0 | Flaming Hands |
| 2 | 1 | Fire Bolt |
| 3 | 2 | Ice Shard |
| 4 | 3 | Poison Spit |
| 5 | 4 | Prismatic Beam |
| 6 | 5 | Frost Nova |
| 7 | 6 | Chain Lightning |
| 8 | 7 | (first grimoire spell) |
| 9 | 8 | (second grimoire spell) |
| 0 | 9 | (third grimoire spell) |

### Flow 5: Equip a Weapon/Armor

```
1. key_press("I")               -- open inventory
2. key_press("Tab")             -- switch to Inventory tab
3. key_press("J") x N           -- navigate to weapon/armor
4. key_press("Enter")           -- open action popup
5. key_press("E")               -- equip (hotkey 'e')
6. key_press("Escape") or "I"   -- close inventory
```

### Flow 6: Talk to NPC

```
1. move_to(target="elder")      -- move adjacent to NPC
2. key_press("C")               -- enter talk mode
3. key_press("D")               -- direction toward NPC (e.g., east)
   - DialogueUI opens
4. key_press("J"/"K")           -- navigate dialogue options
5. key_press("Enter")           -- select option
6. key_press("Escape")          -- close dialogue
```

### Flow 7: Use Stairs

```
1. move_to(target="stairs leading down")
2. move_to(x=STAIR_X, y=STAIR_Y)  -- step onto stair tile
3. key_press("Period")          -- descend/ascend
4. (Zone transition, new zone generated)
```

### Flow 8: Throw an Item

```
1. key_press("I")               -- open inventory
2. Navigate to throwable item
3. key_press("Enter")           -- action popup
4. key_press("T")               -- throw (hotkey 't')
   - Enters ThrowTargeting state
5. WASD to aim direction
6. Enter to confirm throw
```

### Flow 9: Drink/Apply a Tonic

```
1. key_press("I")               -- open inventory
2. Navigate to tonic
3. key_press("Enter")           -- action popup
4. key_press("A")               -- apply/drink (hotkey 'a')
5. key_press("Escape")          -- close inventory
```

## Inventory UI Tab Navigation

The inventory has 4 tabs, cycled with Tab key:

| Tab | Contents | Hints |
|-----|----------|-------|
| Equipment | Body slot display (Head, Body, Arms, Hands, Feet) + item list | `[Enter]equip [e]unequip [>]inventory [Esc]close` |
| Inventory | Carried items list with categories (Tonics, Books, Weapons) | `[Enter]actions [d]drop [>]tinkering [Esc]close` |
| Tinkering | Crafting recipes | `[Enter]craft [>]abilities [Esc]close` |
| Abilities | Hotbar slot assignments (10 slots) | `[Enter]bind [>]equipment [Esc]close` |

## State Observation (read-only execute_code)

Use these to understand game state WITHOUT taking screenshots:

### Current InputState
```csharp
// Check what state the game is in via reflection
var handler = Object.FindAnyObjectByType<CavesOfOoo.Rendering.InputHandler>();
var stateField = handler.GetType().GetField("_inputState", 
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
return $"InputState: {stateField?.GetValue(handler)}";
```

### Check for Announcement Popup
```csharp
var ui = Object.FindAnyObjectByType<CavesOfOoo.Rendering.AnnouncementUI>();
return $"Announcement open: {ui?.IsOpen}";
```

### Player Stats
```csharp
var p = handler.PlayerEntity;
var sb = new System.Text.StringBuilder();
foreach (var kvp in p.Statistics)
  sb.AppendLine($"{kvp.Key}: {kvp.Value.Value}/{kvp.Value.Max}");
return sb.ToString();
```

### Inventory Contents
```csharp
var inv = handler.PlayerEntity.GetPart<CavesOfOoo.Core.InventoryPart>();
foreach (var item in inv.Objects) {
  var rend = item.GetPart<CavesOfOoo.Core.RenderPart>();
  sb.AppendLine(rend?.DisplayName ?? item.BlueprintName);
}
```

### Ability Slots and Cooldowns
```csharp
var abilities = handler.PlayerEntity.GetPart<CavesOfOoo.Core.ActivatedAbilitiesPart>();
for (int i = 0; i < abilities.AbilityList.Count; i++) {
  var a = abilities.AbilityList[i];
  sb.AppendLine($"Slot {i}: {a.DisplayName} CD={a.CooldownRemaining} Usable={a.IsUsable}");
}
```

## Timing Considerations

- **key_press duration**: Default 0.1s hold. The MCPInputBridge re-queues state every frame during this hold. With `MoveRepeatDelay` of 0.12s, a 0.1s hold produces exactly 1 movement action (safe for turn-based games).
- **Sequence delays**: Use 0.2-0.3s between steps in `send_sequence`. This ensures the game processes each action and NPCs take their turns before the next input.
- **Bootstrap time**: Game needs ~2-3 seconds to initialize. Wait for console log containing "Step 9/9" or use `execute_code` to check `handler.TurnManager?.WaitingForInput == true`.
- **After zone transitions**: Allow 1-2 seconds for new zone generation before querying.

## Known Limitations

1. **MCPInputBridge re-queuing**: The bridge holds key state by re-queuing `QueueStateEvent` every frame in `Update()`. For turn-based games, `key_press` with default 0.1s duration produces 1 action. Longer holds may produce multiple actions due to `GetKey` continuous movement.
2. **Popup queue**: Reading multiple grimoires queues multiple announcement popups. Each must be dismissed individually with Enter. Always check `AnnouncementUI.IsOpen` after actions that might trigger announcements.
3. **Auto-bind limit**: Only 10 hotbar slots (keys 1-9 and 0). If all slots are full, new abilities exist but can't be cast via number keys until rebound through the Abilities tab.
4. **Container pickup**: Pressing G on a chest auto-takes ALL items (single container path). There is no "take one item" option from containers — use inventory to drop unwanted items after.

## Spawn Area Reference

The starting zone (Overworld.10.10.0) contains:
- **Player start**: (39, 11)
- **Chest with 10 grimoires**: (43, 11) -- contains 3 knowledge + 7 spell grimoires
- **Compass stones** (Solid, block movement): (41,11), (43,9), (43,13), (45,11)
- **Weapons/armor cache**: around (40-41, 16-17) -- daggers, swords, chain mail, helmets
- **Settlement structures**: campfire, fouled well, cracked oven, lanterns
- **NPCs (Villagers faction, friendly)**: elder, villager, merchant, scribe, warden, farmer, tinker, well-keeper
- **Stairs down**: (50, 15) -- leads to depth 1 with hostile creatures
