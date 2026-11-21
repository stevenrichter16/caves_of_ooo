# Tools on the interact (C) menu of what they act on

*2026-11-21. User direction: "i shouldnt need to go into my inventory to use it. if i'm next to the cell that has a vent or something that gives off an element, i should be able to use the c button on the vent or whatever it is and select to scrape", then "do this quality of life thing for all tools and machines that can be used in or on the environment".*

## The rule

If a carried tool acts on a place or on a thing standing in one, then pressing **C** and a direction (or `.` for your own cell) on that place opens a menu that offers the tool's action, **run by the tool in your pack**. You never open the pack. The pack menu keeps every row it had.

A row is offered on a cell only when the tool can do that to that cell right now: the same checks, the same command and the same cost as the pack row, because it **is** the pack row.

## How it works

One generic mechanism, no per-tool list:

| Piece | Where | What it does |
|---|---|---|
| `InventoryAction.TargetCells`, `AtCell(x, y)`, `AtEntity(zone, thing)`, `ActsOnCell(x, y)` | `Inventory/InventoryAction.cs` | A producer tags the row it already shows in the pack with the cell(s) it acts on. `AtEntity` tags every cell a big thing occupies. |
| `WorldInteractionSystem.WorldCellActionsEvent` (`GetWorldCellActions`) | `Interaction/WorldInteractionSystem.cs` | An item that works on *whatever is in the clicked cell* (a scraper on a vent, a waterskin on a pool) answers this event with rows for that cell, instead of tagging a pack row. |
| `WorldInteractionSystem.AppendToolActions(rows, cell, actor)` | same | For every item the actor carries or holds: the pack rows (`InventorySystem.GetActions`) that act on this cell, plus the event's rows. |
| `WorldInteractionSystem.GatherCellActions(target, cell, actor)` | same | The one place the C menu is composed: the target's own rows, what is underfoot, the ignition row, then the tool rows. `InputHandler` builds the live menu only through it (a source pin enforces that), and the pile summary adds the tool rows too. |

What the dispatcher does to a row before the player sees it:

- **Run by the item.** `Via` is the carried item, so the existing declared-time path (`PerformInventoryActionCommand(row.Via ?? target, row.Command)`) runs the item's own handler. Nothing about execution changed.
- **Costs what the pack charges.** A row that declares a time keeps it. A row that declares none gets one action, which is what the pack's legacy rule charges for every command tagged here, with one exception: the plain "plant here" is free in the pack (only "plant prepared bed" costs a turn), so that row now declares `Free` and both menus agree. (If planting should cost a turn everywhere, that is a one-word change on that row.)
- **No hotkeys.** A pack hotkey could clash with the menu's own.
- **Names the item** when the label does not (`set down east [camp hearth]`); a label that already names it is left alone.
- **Collapses duplicates, and tells apart what differs.** Four identical flasks of the same mire are one choice, not four. Items of one name that hold differently (an empty flask and a full one, a skin with 3 and a skin with 1, a stack of three raw wheat and a stack of two) both stay, each saying what it holds (`fill with water ... [liquid flask] (empty)` and `(5/12 water)`, `cook this stack ... (x2)`), through the small `IToolMenuState` hook that `LiquidVesselPart` and `WaterskinPart` implement and the stack count. Of two rows that read alike, the one you can use is kept, not the one with the lower id.
- **Shared rows are one choice.** A row marked `Shared` is about the place or the whole kit, not the item that offers it ("make camp here" is offered by every kit and does the same whichever runs it). The menu keeps one, names no item on it and tells no items apart by what they hold.
- **Comes after the cell's own rows**, most prominent first. The first row of a menu is never a tool, so `C`, direction, `Enter` still examines.
- **Crowds collapse into one row that opens.** Rows that are the same kind of work share a heading (`InventoryAction.Group`, headings in `ToolGroups`: "set down a kit here", "pour out a liquid here", "lay a device here", "spread a material here", "fill a flask from here", "plant a seed here", "cook food here"). Once three or more rows share a heading the menu shows one row, `set down a kit here (6) >>`, whose hint names the tools ("Choose from: camp hearth, camp still, ..."). Choosing it opens a short list of exactly those rows under a title, ending in `<< back`, which returns to the menu it came from (a pile summary included). Rows about one particular thing (scrape *this* vent, douse *this* fire) carry no heading and stay inline, and two rows are not a crowd.
- **Nothing for an actor who cannot act**, nothing for a row meant to fire on the actor, and nothing is changed or recorded by asking.

## Which tools, and where they appear

| Tool (carried or held) | Row | Offered on | Tagged in |
|---|---|---|---|
| Scraper | scrape *reagent* from the *source* | each source beside you or under you (vent, ash bed, frost vent, ice, brine / acid / mire pool); names that exact source | `ScraperPart` (event) |
| Waterskin | fill the waterskin from the *source* | a well, spring or pool of fresh water beside you; names that exact source (`FillWaterskin\|<id>`); not for a full skin | `WaterskinPart` (event), `WaterVesselService.AddCellActions` |
| Waterskin | douse the *burning thing* | the burning thing | `DouseWorldActions` |
| Fire clay | smother flames on the *burning thing* | a burning thing that is not a creature | `EmergencyDousingActions` |
| Raw food | cook this stack beside a fire | every campfire, hearth or stove beside you that would accept it | `CookablePart`, `CookingService.StationsBeside` |
| Liquid flask | fill with *liquid* | the pool or tray to draw from | `LiquidVesselService` |
| Liquid flask | pour / pour one | each cell beside you and your own | `LiquidVesselService` |
| Hush strip, any laid device | lay *device* *direction* | an open cell beside you | `LayableDevicePart` |
| Spring trap (loaded) | lay the *kind* trap *direction*; on the Choir's ground the one refusal row ("Fire is not laid on the Choir's ground") on each open cell, naming no direction | an open cell beside you | `SpringTrapKitPart` |
| Knotflax cord | lay cord snare | an open cell beside you | `CordSnareActions` |
| Frog oil, silver sand | spread grease / scatter grit | each cell beside you and your own | `CombatUtilityActions` |
| Material carried for field use (moss, pith, brine) | kindle, film, wick and the like | a cell, or a thing that is not a creature | `MaterialFieldActions` |
| Camp kits and other set-down kits | set down | a legal cell beside you | `HeldFixtureActions` |
| The camp (two or more kits) | make camp here | your own cell, once: every kit that can be set down offers it and the menu keeps one (a shared row), so it never goes missing because one kit has nowhere to stand | `HeldFixtureActions` |
| Seed | plant | a prepared bed beside you; your own cell where it could take | `SeedPart` |
| Sounding rod | sound the ground | your own cell | `SoundingRodPart` |
| Torch in hand, out | light torch at nearby fire | every burning fire beside you (a held flame or a striker stands in no cell and tags nothing) | `TorchLightPart`, `IgnitionSources.GroundFiresBeside` |
| Held torch | touch the torch to *thing* | a thing that would catch (not a bare oil film: the "light it" row already covers it) | `EquipmentUtilityActions` |
| Fan, ground-charge screen, grip wrap, glow quartz | fan gas, ground charge, brace against the wall, crack quartz | the gas, the cell, the wall, the cell | `EquipmentUtilityActions` |

Already on the right menu before this change: lighting from a fire, a torch or a striker (`FireStarting`, `IgnitionSources`), and every machine, fixture, container, door, lock, trap, bed, crop and station that offers its own rows using what you carry (the proving post, the trough, the sorting frame, the setting bench, the campfire, the well's "draw water").

### Left in the pack on purpose

- **Care of a creature**: drench, smother a burning creature, share a meal, treat a companion, wick a coat off a creature, cool or warm a creature. The user's scope is the environment; those are about someone.
- **Rows about you**: drink, eat, apply, plant your feet (`BraceBoots`), coat a garment, press a reagent into a flask, transfer between flasks.

## Why this shape

A loaded camp (six kits, two flasks, a strip, a cord) would put about fifteen placement rows on every open cell. With grouping that cell shows four tool rows: the kits and the pours as one row each, and the strip and the cord as themselves (`CampKitVerbsTests.AFullyLoadedPackOnOpenGroundGivesAShortMenu_...`). The count is on the row, the list is one key away, and what you can do about one thing stays one step. Grouping runs after duplicates collapse, so four identical flasks never make a group of four. The F1 details reader inside an opened list rebuilds that list and puts the highlight back (`WorldActionReaderContext.ToolGroup`). Opening or leaving a list is menu navigation, exempt from the reach check like the other navigation rows (the rows inside are checked when chosen).

## How to give a new tool this

1. Tag the pack row where it is added: `actions.AddAction(...).AtCell(x, y)` for a cell, `.AtEntity(zone, thing)` for a thing. Nothing else: the command, checks and cost are the row's.
2. If the tool works on whatever is in the clicked cell (so one pack row cannot name it), handle `WorldInteractionSystem.WorldCellActionsEvent` and add rows for `e.GetParameter<Cell>("Cell")`. Do not set `Via` or the hotkey; the dispatcher does.
3. Declare a time on the row if it is not one action.
4. If the row is one of many of the same kind (placing, pouring, spreading), put it under a shared heading: `.InGroup(ToolGroups.SetDown)`. Add a heading to `ToolGroups` only for a genuinely new kind of work.

## Tests

`WorldToolRowsTests` (the dispatcher, with a probe tool: tag gating on both axes, `Via`, time, hotkeys, naming, duplicates, shared rows, held state, equipped items, the event path, big things, a row meant to fire on the actor, dead actor, order, pile summary, no side effects); `WorldToolFamilyRowsTests` (flask and tray fill, pour, douse, smother, cooking, waterskin fill from the chosen source and its refusals, stunned, planting and its counters, plant cost); `WorldToolRowsInputTests` (the real input handler: the menu opened on a cell offers the row, choosing it runs the tool and pays exactly one action only when it commits, a refusal pays nothing, the details reader's row rebuild, row matching and highlight restore keep the tool rows and tell two flasks' rows apart); (including grouped rows through the real handler: open the list, run a member and pay one action, back, the details reader inside a list, a list opened from a pile summary, a list whose rows all vanished); the group rules in `WorldToolRowsTests` (two rows are not a crowd, three are, no heading never groups, headings group separately, duplicates collapse first, the group's priority, a disabled member, command round trip); `CampKitVerbsTests` (a full camp on open ground is a short menu); and one test beside each family's own suite: `HushStripTests`, `SpringTrapTests`, `CombatSupplySnareTests`, `CombatInventoryUtilityTests`, `CampKitVerbsTests`, `SoundingRodTests`, `ItemUtilityEquipmentTests`, `MaterialFieldTests`, `ScraperTests` (and a source pin that the input handler composes menus only through `GatherCellActions`), `CampFireQolTests`, `ProofingWorkshopTests` (the whole workshop loadout on a plain cell stays at six tool rows or fewer). Each positive has a counter: out of reach, no tool carried, not a source, a creature, a spent or full thing, a stale row.

## Honest limits

- **Not played in the Editor.** The menu composition, the rows, their commands and their effects are verified headless through the real `GatherCellActions`, the real `OpenWorldActionMenuFor` / `ExecuteWorldActionSelection` of the input handler (driven by reflection, as `TrapJammingInputTests` does) and the real command path. The live popup (scrolling, highlight, what a long menu feels like) is not, and neither is `OpenWorldActionDetailsReader` itself (it needs the announcement UI); its row rebuild and the menu restore are.
- **A second independent pass** over those fixes found no P1 or P2. Its smaller notes are addressed (stack sizes, usable-over-disabled, the reader's row match as a testable function, a test fixture that leaked a static) except one: a flask's capacity and a camp box's contents are not shown when two of the same name differ only in that, so those two would still collapse to one row until the next menu open.
- **An independent review found and I fixed:** the F1 details reader rebuilt the menu by hand and dropped the tool rows (and the older "light it" row), the spring trap's rows were never tagged, "make camp here" depended on the first kit, rows that differed only in what an item held were collapsed, and the torch and cooking rows tagged only the first fire. Each now has a test.
- **Grouping is headless-verified only.** The grouped row, the opened list, the back row (including from a pile summary), the F1 reader inside a list and the reach exemption are driven through the real input handler by reflection; how the popup looks and feels with a title and a count is not.
- **Reach is the pack's reach.** A row exists only where the pack row would: beside you or under you for most; a look cursor on a far cell shows nothing.
- **`WorldAffordanceQuery`** (the small "C, W: menu / harvest" cue) is unchanged and does not yet mention tool rows.
