# Caves of Ooo: MCP PlayMode Testing Strategy

A comprehensive guide for an LLM to use the Unity MCP `manage_input` tool to test gameplay in Caves of Ooo during Play Mode.

## Prerequisites

- MCP for Unity package with `manage_input` tool (gameplay group enabled)
- `InputHelper.cs` bridge installed (replaces `Input.GetKeyDown` with dual-system checks)
- Unity Input System package (`com.unity.inputsystem`) installed
- Player Settings: Active Input Handling = "Both"

## Game Overview

Caves of Ooo is a turn-based ASCII roguelike. The player moves on an 80x25 grid, explores zones connected by stairs, fights monsters, picks up items, equips gear, learns mutations from grimoires, crafts at settlement sites, and trades with NPCs. Input is processed one key at a time per turn.

Key constraint: **this is turn-based**. Each key press is one action. The game processes NPC turns between player inputs. There is a `MoveRepeatDelay` of 0.12s that throttles inputs, so delays between key presses must be >= 0.15s.

## Input Reference

### Movement (8-directional, uses one turn each)
| Direction | Keys |
|-----------|------|
| North | W, UpArrow, Numpad8, K |
| South | S, DownArrow, Numpad2, J |
| East | D, RightArrow, Numpad6, L |
| West | A, LeftArrow, Numpad4, H |
| NE | Numpad9 |
| NW | Numpad7 |
| SE | Numpad3 |
| SW | Numpad1 |
| Wait | Numpad5, Period |

### Actions
| Action | Key | Notes |
|--------|-----|-------|
| Pick up item | G or Comma | Opens PickupUI if multiple items on tile |
| Open inventory | I | Full inventory management |
| Look mode | L | Examine tiles with cursor |
| Faction standings | F | Shows reputation with all factions |
| Talk to NPC | C + direction | Initiates dialogue with adjacent NPC |
| Use ability | 1-9 | Activates hotbar slot, may await direction |
| Interact (stairs, etc.) | Period (on stairs) | Descend/ascend |

### Pickup UI (when open)
| Key | Action |
|-----|--------|
| a-z | Select item by letter |
| Enter/Space | Pick up selected |
| Tab | Take all |
| Escape/G | Close |

### Inventory UI (when open)
| Key | Action |
|-----|--------|
| Arrows/J/K | Navigate items |
| Enter | Show item actions |
| Escape/I | Close |
| e | Equip selected |
| d | Drop selected |
| r | Read (grimoire) |
| a | Apply/drink (tonic) |
| t | Throw |

### Debug Keys (development only)
| Key | Action |
|-----|--------|
| F6 | Grant random mutation |
| F7 | Dump body parts to console |
| F8 | Dismember random limb |
| F9 | Debug craft recipe |
| P | Cycle well repair stage |

---

## MCP Tool Usage Pattern

### Basic key press (one game turn)
```
manage_input(action="key_down", key="W")
-- wait 0.15s --
manage_input(action="key_up", key="W")
```

### Using sequences for multi-step actions
```
manage_input(action="send_sequence", sequence=[
  {"action": "key_down", "params": {"key": "W"}, "delay": 0},
  {"action": "key_up", "params": {"key": "W"}, "delay": 0.15},
  {"action": "key_down", "params": {"key": "W"}, "delay": 0.2},
  {"action": "key_up", "params": {"key": "W"}, "delay": 0.15}
])
```

### Observing game state
- `manage_scene(action="screenshot")` -- capture game view
- `read_console(action="get")` -- read game log messages
- `find_gameobjects(search_term="Player")` -- find player entity
- `execute_code` -- query game state directly (HP, position, inventory)

### Querying game state via execute_code
```csharp
// Get player position and HP
var handler = Object.FindAnyObjectByType<CavesOfOoo.Rendering.InputHandler>();
var player = handler.PlayerEntity;
var zone = handler.CurrentZone;
var cell = zone.GetEntityCell(player);
var hp = player.GetStatValue("Hitpoints");
var maxHp = player.GetStatMax("Hitpoints");
return $"Pos: ({cell.X}, {cell.Y}), HP: {hp}/{maxHp}";
```

---

## Test Scenarios

### 1. Movement and Navigation

**Goal:** Verify the player can move in all 8 directions and is blocked by walls/solid entities.

**Setup:** Enter Play Mode, wait for bootstrap.

**Steps:**
1. Screenshot to see initial position
2. Query player position via `execute_code`
3. Move north (W key), query position again -- Y should decrease by 1
4. Move south (S key) -- Y should increase by 1
5. Move east (D key) -- X should increase by 1
6. Move west (A key) -- X should decrease by 1
7. Move toward a wall -- position should NOT change
8. Screenshot after each move to verify visual update

**Verification:**
```csharp
var zone = handler.CurrentZone;
var cell = zone.GetCell(targetX, targetY);
return $"Target cell solid: {cell.IsSolid()}, wall: {cell.IsWall()}";
```

**What to check:**
- Position changes correctly for each direction
- Walls (IsSolid=true) block movement
- Entity positions update in zone cell grid
- FOV updates after each move (explored cells expand)
- MoveRepeatDelay prevents double-moves from rapid input

### 2. Combat Encounter

**Goal:** Walk into a hostile creature and verify combat resolves.

**Setup:** Need to locate a hostile creature first.

**Steps:**
1. Query nearby entities to find a hostile:
```csharp
var zone = handler.CurrentZone;
var playerCell = zone.GetEntityCell(handler.PlayerEntity);
var entities = new System.Collections.Generic.List<string>();
for (int dx = -5; dx <= 5; dx++)
  for (int dy = -5; dy <= 5; dy++) {
    var c = zone.GetCell(playerCell.X + dx, playerCell.Y + dy);
    if (c != null)
      foreach (var e in c.Objects)
        if (e.HasTag("Creature") && e != handler.PlayerEntity)
          entities.Add($"{e.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName} at ({c.X},{c.Y})");
  }
return string.Join("\n", entities);
```
2. Navigate toward the hostile creature
3. Move into their tile to initiate melee attack
4. Check console for combat messages ("You hit the X", "The X attacks you")
5. Query HP after combat to verify damage
6. Screenshot to see combat result

**Verification:**
- Hit/miss messages appear in console
- Damage reduces HP
- If creature dies, it's removed from zone
- XP awarded on kill (check Experience stat)
- Equipment drops from dead creatures

### 3. Item Pickup

**Goal:** Pick up items from the ground.

**Steps:**
1. Find items on nearby ground tiles:
```csharp
var zone = handler.CurrentZone;
var items = new System.Collections.Generic.List<string>();
zone.ForEachCell((c, x, y) => {
  foreach (var e in c.Objects)
    if (e.HasTag("Item") && e.GetPart<CavesOfOoo.Core.PhysicsPart>()?.Takeable == true)
      items.Add($"{e.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName} at ({x},{y})");
});
return string.Join("\n", items.Take(10));
```
2. Navigate to a tile with items
3. Press G to open PickupUI (or comma if single item)
4. Screenshot to see pickup popup
5. Press 'a' (first item) then Enter to pick up
6. Press Escape to close
7. Verify item in inventory:
```csharp
var inv = handler.PlayerEntity.GetPart<CavesOfOoo.Core.Inventory.InventoryPart>();
return string.Join("\n", inv.Objects.Select(o =>
  o.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName ?? o.BlueprintName));
```

**What to check:**
- PickupUI opens with correct item list
- Item transfers from ground to inventory
- Weight tracking updates
- Auto-equip triggers if slot is free
- Stack merging for stackable items (same blueprint)

### 4. Inventory Management

**Goal:** Open inventory, examine items, equip/unequip gear.

**Steps:**
1. Press I to open inventory
2. Screenshot to see inventory UI
3. Navigate with arrow keys to select an item
4. Press Enter to see action menu
5. Press 'e' to equip (if weapon/armor)
6. Press Escape to close menus
7. Verify equipment via:
```csharp
var inv = handler.PlayerEntity.GetPart<CavesOfOoo.Core.Inventory.InventoryPart>();
var equipped = inv.EquippedItems;
return string.Join("\n", equipped.Select(kvp =>
  $"Slot: {kvp.Key} = {kvp.Value.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName}"));
```

**What to check:**
- Inventory UI displays all carried items
- Equipment slots are correct (hand, body, etc.)
- Equipping a weapon changes combat stats
- Two-handed weapons occupy both hand slots
- Unequipping moves item back to carried list

### 5. Grimoire Reading (Mutation Learning)

**Goal:** Read a grimoire to learn a spell/mutation.

**Steps:**
1. Find a grimoire item (either pick up or grant via debug):
```csharp
// Check if player has any grimoires
var inv = handler.PlayerEntity.GetPart<CavesOfOoo.Core.Inventory.InventoryPart>();
var grimoires = inv.Objects.Where(o => o.GetPart<CavesOfOoo.Gameplay.Items.GrimoirePart>() != null).ToList();
return $"Grimoires in inventory: {grimoires.Count}\n" +
  string.Join("\n", grimoires.Select(g => g.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName));
```
2. If no grimoire, use F6 to grant a random mutation directly (debug shortcut)
3. Otherwise: open inventory (I), navigate to grimoire, press 'r' to read
4. Check mutations:
```csharp
var muts = handler.PlayerEntity.GetPart<CavesOfOoo.Gameplay.Mutations.MutationsPart>();
if (muts == null) return "No MutationsPart";
return string.Join("\n", muts.MutationList.Select(m => $"{m.DisplayName} (Level {m.Level})"));
```

**What to check:**
- Grimoire read message appears
- Mutation added to MutationsPart
- Ability registered in ActivatedAbilitiesPart
- Hotbar slot populated
- Duplicate read shows "already known" message

### 6. Ability/Spell Casting

**Goal:** Use a mutation ability from the hotbar.

**Steps:**
1. Verify player has abilities:
```csharp
var abilities = handler.PlayerEntity.GetPart<CavesOfOoo.Gameplay.Mutations.ActivatedAbilitiesPart>();
if (abilities == null) return "No abilities";
return string.Join("\n", abilities.AbilityList.Select((a, i) =>
  $"Slot {i+1}: {a.DisplayName} CD={a.CooldownRemaining}"));
```
2. Press the ability's number key (1-9)
3. If directional: press a direction key (W/A/S/D) to target
4. If immediate: ability fires immediately
5. Screenshot to see effect
6. Check console for ability messages
7. Verify cooldown applied

**What to check:**
- Ability fires with visual/text feedback
- Cooldown prevents re-use
- Mana/MP cost deducted (if applicable)
- Effect applied to target (damage, status effect)
- Hotbar UI updates cooldown display

### 7. Zone Transition (Stairs)

**Goal:** Find and use stairs to move between floors.

**Steps:**
1. Find stairs:
```csharp
var zone = handler.CurrentZone;
var stairs = new System.Collections.Generic.List<string>();
zone.ForEachCell((c, x, y) => {
  foreach (var e in c.Objects)
    if (e.HasTag("StairsDown") || e.HasTag("StairsUp"))
      stairs.Add($"{e.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName} at ({x},{y}) - {(e.HasTag("StairsDown") ? "DOWN" : "UP")}");
});
return string.Join("\n", stairs);
```
2. Navigate to stairs tile
3. Press Period (.) to use stairs
4. Screenshot new zone
5. Verify zone changed:
```csharp
return $"Current zone: {handler.CurrentZone?.ZoneID}";
```

**What to check:**
- New zone generates correctly
- Player position in new zone is at arrival stairs
- FOV recalculates for new zone
- Zone ID reflects new depth
- Return via opposite stairs works

### 8. Faction Interaction and Trading

**Goal:** Find a friendly NPC and initiate trade.

**Steps:**
1. Find NPCs:
```csharp
var zone = handler.CurrentZone;
var npcs = new System.Collections.Generic.List<string>();
zone.ForEachCell((c, x, y) => {
  foreach (var e in c.Objects) {
    if (e.HasTag("Creature") && e != handler.PlayerEntity) {
      string faction = e.GetTag("Faction") ?? "None";
      bool hostile = CavesOfOoo.Gameplay.AI.FactionManager.Instance?.IsHostile(e, handler.PlayerEntity) ?? false;
      npcs.Add($"{e.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName} ({faction}, hostile={hostile}) at ({x},{y})");
    }
  }
});
return string.Join("\n", npcs);
```
2. Navigate adjacent to a friendly NPC
3. Press C then direction toward NPC to talk
4. If trader: navigate dialogue options to trade
5. Screenshot trade UI
6. Test buy/sell with arrow keys and Enter

**What to check:**
- Hostile creatures attack on contact; friendly don't
- Faction determines hostility (Snapjaws = hostile, Villagers = friendly)
- Dialogue UI opens for non-hostile NPCs
- Trade prices reflect Ego stat and faction reputation

### 9. Settlement Interaction

**Goal:** Find and interact with settlement structures (campfire, well, oven).

**Steps:**
1. Find settlement sites:
```csharp
var zone = handler.CurrentZone;
var sites = new System.Collections.Generic.List<string>();
zone.ForEachCell((c, x, y) => {
  foreach (var e in c.Objects) {
    if (e.GetPart<CavesOfOoo.Gameplay.Settlements.CampfirePart>() != null)
      sites.Add($"Campfire at ({x},{y})");
    if (e.GetPart<CavesOfOoo.Gameplay.Settlements.WellSitePart>() != null)
      sites.Add($"Well at ({x},{y})");
    if (e.GetPart<CavesOfOoo.Gameplay.Settlements.OvenSitePart>() != null)
      sites.Add($"Oven at ({x},{y})");
  }
});
return string.Join("\n", sites);
```
2. Navigate adjacent to a campfire
3. Check console for proximity message ("The campfire crackles warmly")
4. Navigate to well, test debug cycle (P key)

### 10. Death and Recovery

**Goal:** Verify death handling when HP reaches 0.

**Steps:**
1. Check current HP
2. Find a hostile creature
3. Engage in combat repeatedly until HP is low
4. Or use execute_code to set HP low:
```csharp
var hp = handler.PlayerEntity.Statistics["Hitpoints"];
hp.Penalty = hp.BaseValue - 1; // Set to 1 HP
return $"HP set to {hp.Value}/{hp.Max}";
```
5. Take one more hit
6. Check if death screen/game over triggers
7. Check console for death messages

---

## Automated Regression Test Sequences

### Quick Smoke Test (30 seconds)
```
1. Enter Play Mode
2. Wait for bootstrap (check console for "Bootstrap complete")
3. get_status -- verify input system detected
4. Screenshot -- verify game renders
5. Move W, W, W -- verify 3 moves north
6. Screenshot -- verify position changed
7. Query HP -- verify player alive
8. Stop Play Mode
```

### Full Movement Test
```
1. Query initial position
2. Move N, verify Y-1
3. Move S, verify Y+1
4. Move E, verify X+1
5. Move W, verify X-1
6. Move toward known wall, verify position unchanged
7. Wait (Period), verify turn passed but position same
```

### Combat Loop Test
```
1. Query nearby hostiles
2. Navigate to adjacent tile of nearest hostile
3. Record attacker HP and defender HP
4. Move into hostile (attack)
5. Read console for hit/miss/damage
6. Query HP changes
7. Repeat until one dies
8. If player dies: note death handling
9. If enemy dies: check XP gain, item drops
```

### Full Inventory Cycle
```
1. Find ground item
2. Navigate to item
3. G to pick up
4. I to open inventory
5. Navigate to item, Enter for actions
6. Equip or use
7. Check stats changed
8. Drop item
9. Verify item on ground
```

---

## State Observation Cheat Sheet

### Player Stats
```csharp
var p = handler.PlayerEntity;
var sb = new System.Text.StringBuilder();
foreach (var kvp in p.Statistics)
  sb.AppendLine($"{kvp.Key}: {kvp.Value.Value}/{kvp.Value.Max}");
return sb.ToString();
```

### Nearby Entity Map
```csharp
var zone = handler.CurrentZone;
var pc = zone.GetEntityCell(handler.PlayerEntity);
var sb = new System.Text.StringBuilder();
for (int dy = -3; dy <= 3; dy++) {
  for (int dx = -3; dx <= 3; dx++) {
    var c = zone.GetCell(pc.X + dx, pc.Y + dy);
    if (c == null) { sb.Append('#'); continue; }
    if (dx == 0 && dy == 0) { sb.Append('@'); continue; }
    var top = c.Objects.LastOrDefault();
    sb.Append(top?.GetPart<CavesOfOoo.Core.RenderPart>()?.RenderString?[0] ?? '.');
  }
  sb.AppendLine();
}
return sb.ToString();
```

### Current Input State
```csharp
var tm = handler.TurnManager;
return $"WaitingForInput: {tm.WaitingForInput}\nCurrentActor: {tm.CurrentActor?.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName ?? "null"}";
```

### Inventory Summary
```csharp
var inv = handler.PlayerEntity.GetPart<CavesOfOoo.Core.Inventory.InventoryPart>();
var carried = inv.Objects.Select(o => o.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName).ToList();
var equipped = inv.EquippedItems.Select(kvp => $"[{kvp.Key}] {kvp.Value.GetPart<CavesOfOoo.Core.RenderPart>()?.DisplayName}").ToList();
return $"Carried ({carried.Count}): {string.Join(", ", carried)}\nEquipped ({equipped.Count}): {string.Join(", ", equipped)}";
```

---

## Timing Considerations

- **Turn-based**: Each key press = one game turn. NPCs act between player inputs.
- **MoveRepeatDelay**: 0.12s minimum between accepted inputs. Use >= 0.15s delay.
- **Sequence step delays**: Use 0.2-0.3s between key_down and key_up for reliable detection.
- **Bootstrap time**: Game needs ~1-2 seconds to initialize on Play Mode entry. Wait for console log "Bootstrap complete" or check `TurnManager.WaitingForInput == true`.
- **Screenshot timing**: In Play Mode, screenshots are async (captured at end of frame). Allow 0.5s after requesting before reading the file.
- **Domain reload**: Entering/exiting Play Mode triggers domain reload. MCP connection persists but needs ~1-2s to stabilize.

## Known Limitations

1. **QueueStateEvent persistence**: Each key_down queues a one-frame state. The InputHelper bridge detects transitions via manual state tracking. Very rapid key presses (< 0.1s apart) may be missed.
2. **Input state for held keys**: For held-key movement (e.g., holding W to walk north), send key_down, wait desired duration, then key_up. The game's `Input.GetKey` check via InputHelper will see the key as held.
3. **Modal UIs consume input**: When InventoryUI, PickupUI, DialogueUI, or TradeUI is open, movement keys don't move the player -- they navigate the UI instead. Always close modals before attempting movement.
4. **NPC turns between inputs**: After each player action, all NPCs with enough energy take their turns. This can include combat, movement, and ability usage that changes the game state before the next player input.
