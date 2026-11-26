# Batching routine native playtest input

## Walking

Use the guarded walk for a short, planned route through a single loaded
zone. It sends one input frame per arrow-key press and checks every actual
step. The caller chooses the route. The helper never
generates routes, moves an entity directly, advances the clock, dismisses
a prompt or repeats a refused action.

Start an isolated GameBootstrap session through `NativeSaveIsolation` and
Continue the intended save normally. Attach the audit’s own keyboard, with
the normal input settings and devices saved for restoration. Prepend these
bindings to `guarded-walk.cs` and execute the combined body against that
Editor through Unity MCP. Example bindings for the Cinderhold calibration
are below.

```csharp
string auditOwner = "Codex.CinderholdAudit.";
string keyboardName = "CinderholdAuditKeyboard";
string expectedZone = "Overworld.6.6.0";
var moves = new[] {
    UnityEngine.InputSystem.Key.RightArrow,
    UnityEngine.InputSystem.Key.DownArrow
};
```

After scheduling, wait for `SessionState.GetBool(auditOwner + "keyJob", false)`
to clear and read `SessionState.GetString(auditOwner + "guardedWalk", "")`.
The result reports the completed prefix, positions, turns and elapsed time.
If movement stops early, look at the current state before deciding whether
to proceed. Continue from the actual final position. Leave hazardous ground,
crossings, modal choices, transactions, interruptions and new game outcomes
to deliberate native input with observations at each meaningful checkpoint.

Always finish the owned audit, restore its keyboard and settings, and verify
the expected normal save and preference scope.

## Cinderhold calibration

Two ordinary Continues started from the same checkpoint and followed the
same 24 keys. Fixed-delay input took 7.38 seconds. The guarded walk took
2.94 seconds, about 60% less time for this stretch. Both reached the same
cell and game turn. The guarded route retained all 40 HP and stopped with
zero moves in separate tests of an occupied cell and an open inventory.
Delaying observation for 0.31 seconds still produced exactly one move.
These timings cover movement only. [Calibration record](cinderhold-calibration.json)

## Finding an inventory item

Use the guarded search in an isolated GameBootstrap audit with its own
keyboard. Open the inventory’s **Find** field with `/` and start with an
empty query. Prepend these bindings to `inventory-search.cs` and execute
the combined body through Unity MCP.

```csharp
string auditOwner = "Codex.CinderholdAudit.";
string keyboardName = "CinderholdAuditKeyboard";
string searchText = "iron spike";
```

Use up to 48 lowercase letters, digits, spaces, hyphens or apostrophes.
The helper sends one key at a time and checks each accepted character.
Once `keyJob` clears, read `SessionState.GetString(auditOwner +
"inventorySearch", "")` and inspect the actual results. Confirm the
exact item before opening its actions. Press Enter to leave typing mode
and make the item selection separately. If search closes or the query
changes unexpectedly, inspect the current menu before sending further
keys. Keep purchases, crafting, combat and other consequential inputs
separate from routine navigation.

On Apple M5 both runs found the same sole iron spike at turn 3490.
Typing ten keys took 2.77 seconds with fixed waits and 0.03 seconds
with guarded input. The repeat-letter query `nozzle` completed exactly.
A closed search refused to start, and pressing Escape at the start of
a scheduled search stopped it with an empty query.
[Search calibration](inventory-search-calibration.json)
