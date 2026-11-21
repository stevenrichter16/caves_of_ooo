# Machine, camp and world-tool program: handoff for the next agent

**Brand-new session?** Start at [NEW-SESSION-HANDOFF-2026-11-21.md](NEW-SESSION-HANDOFF-2026-11-21.md): a self-contained orientation (project, owner, who else edits, rules, commands, what to do next).

**Newer handoff:** read [Codex's November 21 handoff](CODEX-MACHINE-HANDOFF-2026-11-21.md)
first for verified corrections, native play, the still-running regression gate,
the unfixed scenario-capacity finding and the inventory redesign plan.

Written 2026-11-21 (America/Chicago) at the end of a five-day run (2026-11-16 to 2026-11-21). State when first written: `main` at `a36edd91f` (this doc and the verification scripts; code at `d4a31ec11`); the descriptions pass (section 10.1) is `e89a81af6`, pushed to `main`. Check `git log` for the real head.
Read section 0 first. Everything else is reference. This file is the entry point; the living records it links to hold the detail, and the memory note `machine-program-state.md` (in the Claude project memory) holds the same facts in diary form.

## 0. Read this first

**Codex handback update — November 21:** the user explicitly transferred repo,
Unity and Git control to Codex and authorized fixing the connected-source stock
bug, assessing the inherited full regression and actually playing new systems.
The current continuation is in
[MACHINE-SYSTEMS-EXECUTION-PLAN.md](MACHINE-SYSTEMS-EXECUTION-PLAN.md#active-handback-continuation--november-21-2026).
Read the [native camp/workshop receipt](Verification/MachineSystems/CampPlay20261121/README.md)
for the now-played subset and timing defects found/corrected; the original
"never played" inventory below describes the handoff state, not the later review.
Fullrun_h and the remaining acceptance/delivery gates are still being assessed.
The separate [inventory density redesign](INVENTORY-DENSITY-REDESIGN-PLAN.md) is a
new user-requested plan, not an implemented UI change.

**What this program is.** The owner wants the game's twelve proposed machines (forge/reclamation rig, fractionating still, restoration cradle, assay cabinet, culture vat, anchored capstan, sump pump, thermal exchanger, dynamo, survey resonator, signal relay, treatment bath: [MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md)) and a home/base layer to exist as real, useful, fun parts of the world, built from composable Parts and shared rules instead of one-off scripts ([SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md)). Since 2026-11-19 the owner has told me to invent a use for any machine that has none, to put machines wherever in the world is most engaging (not only at camp), and to keep going without asking. Since 2026-11-21 they also want tools usable from the interact menu of what they act on, grouped so the menu is not a wall, and descriptions that tell the player how to use each thing.

**State.**
- Built, tested and pushed: the machine architecture (installations, stations, readers, declared action time, multi-stack claims, near-miss rows), the home/base layer (eight set-down camp kits), about twenty invented machine uses, a proofing system with status dampening, five more world machines (Sorters' frame, hush strips, scraper, spring trap, Setters' bench), and carried-tool rows on the C menu with grouping. Sections 2 and 4.
- **Never played by a person.** Only the camp box, hearth, bench, still and the world-map note were played in the Editor (2026-11-18/19). Everything built after that is verified by EditMode tests, mutation checks and independent read-only reviews, not by a person at the keyboard. [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md) is the guide for doing that.
- **No full test run has finished on the final tree yet.** The last complete full-suite verdict is for commit `eb12e00f9` (section 6.4). A clean full run on the final head `e89a81af6` was started 2026-11-21 13:42 in clone4 (`fullrun_h`; about three hours; results `/Users/steven/coo-verification/results/fullrun_h.xml`): compare it with `python3 Tools/CloneVerification/compare_full.py /Users/steven/coo-verification/results/fullrun_h.xml` and re-run every new failure alone (section 10.2). An older run (`fullg`, clone2, started 10:25 on a tree with the tool-row work in progress) was still going; its result, if it finished, is not the gate.
- **Finished after this was first written:** the "descriptions pass" (section 10.1): every machine's and tool's Examine now says how to use it, what it needs and whether it is usable right now.

**Five rules that bind everything below.**
1. **Never touch the user's open Unity Editor to run tests** (it is the owner's/another agent's play surface; check `isPlaying` first). Verify in the warm clones: [Tools/CloneVerification/README.md](../Tools/CloneVerification/README.md). A run whose log shows `error CS` proves nothing.
2. **Commit and push verified work by default** (the owner: "commit and push it, always"). Explicit paths only, copy new `.meta` files, trailer line `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`, never force-push, never stage the foreign dirty files (section 7).
3. **TDD with counter-checks, then mutation checks, then an independent read-only review**, and record honest limits ("not played", "not run"). Section 6.1.
4. **The world rules are the product.** New behaviour attaches to Parts/capabilities and shared rules (finite stock, custody, noise, heat, status resistance), never to a one-off scenario switch. No effect may mint money or a reagent, bypass payment, or break save/reload. [SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md).
5. **Lore is protected.** `Lore/` (god-tree/tepui) is canon; `Docs/Lore/` (the Palimpsest world) is superseded: never design from it. Do not make an instrument claim to know what the lore says it cannot (the sounding rod reports hollow/solid, never "what").

**First five minutes.**
```bash
cd /Users/steven/caves-of-ooo && git status --short | grep -v '^ M' | head      # foreign files: section 7
git log --oneline -8
ls /Users/steven/coo-verification                                               # clone clone2 clone3 clone4 clone5 results baselines
pgrep -fl "Unity.*-projectPath" | cut -c1-160                                   # who is running what
Tools/CloneVerification/clone_run.sh clone3 'CavesOfOoo.Tests.WorldToolRowsTests' smoke1   # prove the loop works (2-3 min)
```

## 1. Why: motivations and goals

**The owner's central philosophy** (CLAUDE.md, "Central design philosophy", 2026-11-16): build machines, NPCs and objects from composable Parts, reusable capabilities and common events. Behaviours attach to instances/components and interact through shared world rules, including situations nobody authored one by one. With the right materials, components, machines and access, a player's common-sense expectation should have a consistent supported path. Preserve several useful approaches and clear consequences; avoid one-solution tool puzzles. Keep finite resources, custody and save integrity; be honest about unsupported states.

**What the machines are for.** [MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md) holds the owner's pitch for the twelve. Its recurring themes are design requirements: choices about what survives, inputs deciding outputs, ordinary convenience staying free, bounded/authored/local reservoirs and consequences, expedition and base relevance, visible state.

**How the direction moved** (the owner's words, newest decisions override older ones; full list in Appendix A):
- 2026-11-16/17: reclamation and regional processing (finite oil, leather, composite recovery), machine design as architecture first.
- 2026-11-17/18: a modular machine/object architecture, "a machine in every town" as a ratchet, then a home/base layer so a player can have a camp.
- 2026-11-19: "if a use for a machine does not exist, analyze the game and the purpose of the machine and invent a use for that machine that flows naturally with the rest of the game mechanics and the lore"; "you have full free reign of the game". This replaced the dossiers' advice not to build machines without a demonstrated purpose. The systems principles still bind. Then sixteen camp quality-of-life items, and "describe in game with small text what stoke does, when you highlight it".
- 2026-11-20/21: "start on another machine ... do this for 5 new machines ... for as long as you can without my intervention", "the machines don't have to be for camp, they can be wherever in the world you think makes it most engaging and interesting and fun". Then: the scraper must be usable from the C menu of the vent/pool/ice it acts on, and "do this quality of life thing for all tools and machines that can be used in or on the environment"; then "group similar actions into one row that opens up ... so it's not a long wall"; then descriptions that "tell the player exactly how to use them, if they're usable in that moment, or what materials are needed, or what environmental factors are needed".
- Cancelled or parked by the owner: the graphics program (new blueprints are glyph-only on purpose), the capstan, the culture vat, Steam Deck work.

**What "good" meant in practice** (the acceptance bar I held every slice to):
- It makes sense in the world: a lore read first (`Lore/`), a name the world would use, an operator and a place that exist.
- It uses what already exists: an existing tile reaction, ignition rule, stock, status effect or Part; the machine is a new answer to an existing question.
- It has a finite cost and no free money: consumables are finite or regenerate on a clock; free reagents are `NoTrade`; a tool never turns into a coin tap.
- It has at least two ways to be useful or an ordinary alternative, a cross-system consequence (noise, fire, status), and an honest refusal that says why (a disabled row with a reason, not a missing row).
- The player can read it: examine line, "works here" line, a row hint on highlight, a state line while carried.
- It is saved/reloaded exactly (golden saved-Part contract) and cached towns are untouched (fresh-only placement).

## 2. Timeline (what was done, in order)

Commits are on `main`. Counts: 112 commits 2026-11-16 to 2026-11-21, 60 new test files, 56 new source files, about 32,000 lines added across code, tests, content and docs.

| Date | Work | Key commits |
|---|---|---|
| 11-16/17 | Reclamation and processing, accepted before this run: finite Cinderhold oil recovery and contained-liquid handling, finite cured leather feed, composite processing through shared input claims, capstan purpose qualification. Docs: [MACHINE-CONTAINED-OIL-RECOVERY.md](MACHINE-CONTAINED-OIL-RECOVERY.md), [MACHINE-LEATHER-FEED.md](MACHINE-LEATHER-FEED.md), [MACHINE-COMPOSITE-PROCESSING.md](MACHINE-COMPOSITE-PROCESSING.md), [MACHINE-RECLAMATION-PROCESSING.md](MACHINE-RECLAMATION-PROCESSING.md). | `c43631ad6` `4dcb2d5bc` `c600701d0` `420f82367` `5b4309e42` |
| 11-18 | **Architecture S1-S7, S12, S19.** Saved-Part golden contract; installation marker, retention and registry with fresh-only placement (forges in Gantry, Tine, Slip); one station contract with oracle parity; one reader contract (quick and full Examine); declared action time and disabled rows; reasoned near-miss rows and multi-stack pooling; rig relocation stamp. Spurgrass serrated kit. Design reports (`MachineDesign/`, the architecture spec, the home/base design). **Home/base PB0a-PB3:** zone classifier, placement check, same-entity set down / take up / hand over, camp box, reader lines and derived list; family-zone lift (PB1a); first three kits (hearth, bench, still); arrival and world-map notes. | `2f711bece` `9d8299754` `a540ad7e0` `f0e99eb08` `ad4ab5670` `7cea032d9` `f9e104db5` `2d18426d1` `65530e82f` `22fc152e7` `1400a4f31` `80801b2fd` `11c10ba33` `59aa83b6e` |
| 11-19 | Owner direction change (invent uses). Editor play of the box, hearth, bench, still, map note; independent review found three real defects (fixed). **Flint striker**, **rest beside the hearth**, **settling at the still**. Sixteen camp quality-of-life items. **Cinderhold mine pump** (generic site-owned object). | `79292d587` `dec16b2a2` `9088368d5` `0ad433082` `2d110e404` `9bbaa2e7b` `8d60ad233` `b25927d92` `c4c087489` |
| 11-20 | **Sounding rod** (+ survey log), **steeping trough**, bodies for the Tine weir / Gantry guest pole / Ledger survey line / Wellmeet pan scales, **baking clay**, **Concord carriage**, **clay-wash**, **water puts the hearth out**, **fish trap**, **shade awning** (shade as a capability), **natural heat** at vents. Hand-test guide and scenarios. **Proofing system:** status dampening (a resistance blunts the status, not only the damage), element table, press/coat/lasting proofing, settling tray (free reagents from sources), trough unpick, proving post and workshop scenario. Then the five new machines: **Sorters' frame**, **hush strips** (+ generic laid devices), **scraper**, **spring trap**. | `cba6d7288` `8f6072b6f` `1f8d4979f` `295b2ae72` `2e717d9a3` `e782e76ef` `404445ff1` `ca10c511f` `4b543b89a` `0a5d7a5e9` `d3673ab06` `108a4fc9b` `5af043078` `bc95bae93` `095262d6b` |
| 11-21 | **Setters' bench** at Posy; Posy frame/bench excused on the documented glyph-only list; **tool rows on the C menu** (every carried tool that acts on a place shows its row on that place's menu); **grouping** of crowded rows ("set down a kit here (6) >>"); regression closure; this handoff; the descriptions pass (in flight). | `19fab8606` `787b35c60` `374624c8b` `d4a31ec11` |

## 3. Architecture as built: the map

A reader who knows the Entity/Part/GameEvent model (CLAUDE.md, "Architecture mirrors Qud") needs these additions.

### 3.1 Contracts and registries

| Concern | Where | What it does |
|---|---|---|
| Installations | `Gameplay/Installations/InstallationRegistry.cs`, `Content/Data/Installations.json`, [MACHINE-INSTALLATIONS.md](MACHINE-INSTALLATIONS.md) | Data rows say which place gets which machine (`Place`, `Blueprint`, optional `Site`, `Kind`: "Works" for non-forge machines such as the Posy frame). Fresh-only placement (a cached town is never retrofitted). A coverage census (`InstallationCoverageCensusTests`) is a ratchet: Pending may only fall. |
| Place-site objects | `Gameplay/Settlements/PlaceSiteObjectPart.cs`, `PlaceSiteRepairs.cs`, `Content/Data/PlaceSites.json`, `PlaceConditions.json`, `Repairs/` | A world object that is the physical body of an authored repair site (pump, weir, survey line, pan scales). Its states come from the concern; mending is the existing repair chain. |
| Stations | [MACHINE-STATIONS.md](MACHINE-STATIONS.md) | One station contract (oracle parity with the old scanners). A station is usable only while "ready" (a guard system: a kit that is carried/dropped is inert). |
| Readers | `Gameplay/Entities/StateReader.cs`, [MACHINE-READERS.md](MACHINE-READERS.md) | `IDescribeState { Scope (Quick/Full/Both), Order, DescribeState(DescribeContext) }`: Parts add lines to the examine text. `IMenuStatus` adds a status line to a menu. `HeldFixtureReader` lists what you keep and where. |
| Action rows | `Gameplay/Inventory/InventoryAction.cs`, [MACHINE-ACTION-ROWS.md](MACHINE-ACTION-ROWS.md) | A row has Name/Display/Command/Key/Priority/Hint/`Time` (`Unspecified/Free/OneAction`)/`Via`; a disabled row has an EMPTY Command and a `DisabledCode`/`DisabledReason`. Declared time makes the pack menu and the world menu charge the same. |
| Multi-stack claims | `InventoryTransaction`, [MACHINE-MULTI-STACK-CLAIMS.md](MACHINE-MULTI-STACK-CLAIMS.md) | Extra process inputs pool across stacks with full rollback. |
| Packable fixtures (kits) | `Gameplay/Installations/PackableFixtures.cs`, `HeldFixtureActions.cs`, `Content/Data/PackableFixtures.json`, [HOME-BASE-IMPLEMENTATION.md](HOME-BASE-IMPLEMENTATION.md) | A kit is an ordinary item that is inert while carried; "set down" places the SAME entity, adds `InstallablePart` + `PackableFixturePart` and a marker; "take up" / "hand over" reverse it. Placement rules in `PlacementCheck` (10 ordered rules), zone classes in `HeldPlaceZones.Classify`. Ground classes: `OpenGround`, `ShoreGround`, `BareGround`. Set-down is refused underground (column audit PB7 is not done) and in villages (PoiZone). |
| Laid devices | `Gameplay/Items/LaidDevices.cs` | `LayableDevicePart` (a carried item that can be laid on a neighbouring cell) and `LaidDevicePart` (the thing laid; "take up" returns the same item). Used by hush strips and the spring trap. |
| Tool rows on the C menu | `Gameplay/Interaction/WorldInteractionSystem.cs`, `InventoryAction.TargetCells/Group/Shared`, [WORLD-TOOL-ROWS.md](WORLD-TOOL-ROWS.md) | Section 3.3. |
| Proofing | `Gameplay/Alchemy/Proofing*.cs`, `Content/Data/Alchemy/Proofing.json`, `Gameplay/Effects/ElementalStatusResistance.cs`, [MACHINE-PROOFING.md](MACHINE-PROOFING.md) | Section 4.4. |
| Noise | `Gameplay/AI/Behaviors/Noise.cs` | `Noise.Mark(actor)` (a loud act) and `Noise.MarkStep` (footfalls, called at all four movement noise sites); sound-hunting creatures (`HuntsBySound`, the pale stalkers) react to it. Hush strips quiet steps; the scraper and the sounding rod make noise. |
| Ignition | `Gameplay/Items/IgnitionSources.cs` | One place that answers "what can light this": real fires first and free, then the flint striker, then a lit torch. `GroundFiresBeside` lists the fires a carried item can light from. |
| Source stock | `Gameplay/Alchemy/ProofingSources.cs` (`SourceStockPart`) | A finite, regenerating supply per source blob (steam vent, ash bed, frost vent, ice sheet, brine, acid, mire pools): capacity 3 to 5, +1 a day. The settling tray and the scraper draw on the SAME stock, so neither adds supply. |
| Shade | `Gameplay/Items/ShadePart.cs`, `Gameplay/World/BeatingGlareSystem.cs` | A capability read by the Beating's glare; the awning carries it; so do hats and roofs. |
| Natural heat | `Gameplay/Alchemy/NaturalHeat.cs`, `TileStateSourcePart` | A still within 2 cells of a steam vent or ash bed settles with no fuel. Read the declared Heat, never `ThermalPart.Temperature` (it decays to ambient). |
| Economy hooks | `Gameplay/Economy/Ledger.cs`, `ConcordCarriage.cs`, `ConcordCarriagePart.cs` | All dram movement outside trades goes through `Ledger`; the carriage keeps real goods entities in a part on the player. |

### 3.2 How to add a machine: the checklist

This is the sequence that worked and the pins that fail if you skip a step. Details with examples: the "Method" notes in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md).

1. **Plan on disk first**: a section in `MACHINE-INVENTED-USES.md` (gap it fills, why it flows from the game's rules and lore, finite accounting, what it refuses, what it does NOT do). Read the lore (`Lore/`) before naming anything.
2. **Blueprint** in `Content/Blueprints/Objects.json` (glyph-only art; the static description is flavour only, at most 130 characters, no number and no instruction; the how-to belongs in the Part's `IDescribeUse`, section 3.4). Loot/stock: `Content/Data/Loot/LootTables.json` or a trader's stock table, with a chance, never an unbounded source.
3. **Part(s)** in `Gameplay/Items/` (or the system's folder). Handle `GetInventoryActions` (pack rows), `InventoryAction` (execute), and for carried tools `GetWorldCellActions` (section 3.3). Every refusal path emits a diag record and a message (`Diag.Record("furniture", ...)`); every success too. Use `InventoryTransaction` (`Do(action, undo)`, `AfterCommit`) so a failure restores everything.
4. **Rows**: declare `ActionTime`; add a `Hint` (shown when the row is highlighted); a refused option is a disabled row with a reason, not an absent row. Tag world-acting rows `.AtCell(x,y)` / `.AtEntity(zone, thing)` and give crowded kinds a `.InGroup(ToolGroups.X)`.
5. **Readers**: implement `IDescribeUse` on the Part that owns the verbs (how, needs, limits from the rule's live constants, and `WhyNot` for when it offers nothing; a kit gets the set-down text from `PackableFixturePart`) and `IDescribeState` for state only; implement `IToolMenuState` for a carried tool when two copies can differ. `MachineUseGuideTests` has a census of every kit and a pin on description length: extend it.
6. **Kit?** Add a row to `PackableFixtures.json` and extend the pins listed in 6.5 (`HeldFixtureActionTests` shipped-kit list, `CampBenchTests.EveryShippedKitIsInertUnlessSetDown...`).
7. **Installation (a world object in a town)?** A row in `Installations.json` with the town's REAL ground list (read its composition builder's `GroundAt`), plus a voxel model alias or a line in `Assets/Tests/EditMode/Presentation/Rendering/PreviousPresentationTierGap.cs` (the glyph-only list). Without one, the "every visible native owner has a model" tests fail.
8. **New saved Part?** Regenerate the golden: `COO_REGENERATE_SAVED_PART_CONTRACT=1` with the `GenerateSavedPartContract` test in a clone, copy the file back, run `SavedPartContractTests`.
9. **Tests**: RED first (or mutation checks standing in for RED), counter-checks, a save/reload case, a refusal case per gate, a diag-emission case. Adversarial file when two or more taxonomy surfaces apply (CLAUDE.md).
10. **Scenario + hand-test entry**: add the item to `ProofingWorkshopShowcase` / `CampMachinesShowcase` and a numbered step in [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md).
11. **New files need `.meta`**: Unity generates them in the clone; copy them to the main tree before committing.
12. Mutation checks, an independent read-only review, the broad families (6.5), docs updated in the same commit, commit and push.

### 3.3 The interact (C) menu: how tool rows work

Goal: a carried tool that acts on a place appears in the interact menu of that place. Full record: [WORLD-TOOL-ROWS.md](WORLD-TOOL-ROWS.md).

- **Flow.** `c` + direction -> `InputHandler.OpenWorldActionMenu(x,y)` -> `OpenWorldActionMenuFor(target, cell, includeBackRow)` -> `WorldActionMenuUI.Open(...)`. Selection -> `ExecuteWorldActionSelection`: disabled check, picks, group opening, reach check (`ActionRequiresReach`, strike reach 1), then `InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(action.Via ?? target, action.Command))` and `EndTurnAndProcess` when the row declares `OneAction`.
- **Producing a row.** Either tag a pack row (`.AtCell`, `.AtEntity`: sets `InventoryAction.TargetCells`) or answer the `WorldInteractionSystem.WorldCellActionsEvent` ("GetWorldCellActions") for the clicked cell (scraper, waterskin fill: per-source commands such as `ScrapeSource|<id>`, `FillWaterskin|<id>`, so the row acts on the source the player chose).
- **Collecting.** `AppendToolActions` -> `CollectToolRows`: sets `Via` to the carried item, clears hotkeys, defaults time to `OneAction`, adds `[tool name]` unless the label has it or the row is `Shared`, adds a state suffix (`IToolMenuState` and stack count), sorts, and removes duplicates (same `Display`, or `shared|Name|Display`). Tool rows go at the bottom, never first.
- **ONE composition point.** `WorldInteractionSystem.GatherCellActions(target, cell, actor)` builds the whole live menu (target rows, underfoot, ignition, tools). The open path, the pile summary and the F1 details reader all call it; a source pin (`ScraperTests`) forbids composing by hand in `InputHandler` (a past bug: the F1 reader dropped the tool rows).
- **Grouping.** Rows with the same `Group` heading (`InventoryAction.ToolGroups`: lay a device here, set down a kit here, pour out a liquid here, spread a material here, fill a flask from here, plant a seed here, cook food here) collapse into one row `heading (N) >>` when there are `ToolGroupAt` (3) or more. `OpenToolGroupMenu` shows the members with a `<< back` row; the F1 reader remembers it was inside a list (`ToolGroupHeading`). A new kind of crowded placement work needs a heading and a test that groups rows from different tools.
- **Cost parity.** A world row costs what the pack row costs. Plain "plant here" declares `Free` (it is free in the pack); "plant prepared bed" costs a turn (an owner decision is open: section 9).
- **Deliberately pack-only:** rows that act on a creature or yourself.

### 3.4 Readers: state, and how to use it

Examine has two generated parts besides the blueprint's static flavour text (kept to one short sentence). **State** comes from `IDescribeState` providers (`StateReader.Collect`: a job in progress, what a tray holds, the camp's name). **Use** comes from `IDescribeUse` providers (`UseGuides.Collect`, `Gameplay/Entities/UseGuide.cs`): `How to use`, `Needs`, `Cost and limits` from the live constants of the rules, and a live `Right now you can: ... / you cannot: ... (reason)` line built from the rows the thing's own Parts would show this viewer, so it can never promise what the game refuses. The quick Examine shows the block ahead of the flavour; the full document has a "How to use" section. Section 10.1 records the pass that gave every machine and tool a guide.

## 4. Catalog: what exists and how it works

Each entry: what it is, how the player uses it, what it costs or needs, key code, tests, doc anchor. "Glyph-only" means no art (the graphics program is cancelled). Doc anchors are in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) unless noted.

### 4.1 The home/base layer: eight set-down camp kits

All are `Class: Carried` rows in `PackableFixtures.json`; each is inert while carried and a station only while set down (a dropped kit is not a station: `PackableFixturePart` is a readiness guard). "Set down" asks for an adjacent legal cell; "take up" and "hand over" reverse it; "move" opens a green/red picker; "make camp here" sets several kits down in one action (shared row on your own cell); "pack up camp"; "name this camp"; "as you left it" and "what can I do here" lines; the world map says where you keep things.

| Kit (kind) | Blueprint | Where sold | What it does | Notes / refusals |
|---|---|---|---|---|
| Camp box (`camp-box`) | CampBox | Quartermaster 35% | Solid container; deposits from the adjacent cell | Fire beside it is a consequence |
| Camp hearth (`camp-hearth`) | CampHearth | Provisioner 35% | Finite cooking fire: feed (to full), stoke, relight, dry gear, cook; burns fuel while it burns; lights the night only while burning; keeps fuel through take up; rest beside it (`HearthRestPart`: the fire burns ticks x rate of fuel while you sleep, refused up front with the numbers); water puts it out (`FireDouse`) | The flame must be carried from a village fire first; a lit hearth cannot be relit |
| Camp bench (`camp-bench`) | CampBench | Tinker 25% | A portable cold forge: the shipped process rows find it with no scanner edit | |
| Camp still (`camp-still`) | CampStill | Apothecary 20% | Alchemy station made portable (brewing); **settling** mire to water or oil over a lit fire or on a vent's natural heat; **baking** mire into fire clay (needs a real fire) | `LiquidSettlingPart`, `NaturalHeat` |
| Steeping trough (`camp-trough`) | CampTrough | Tinker 20% | Proofing and steeping garments, unpick (sections 4.2 and 4.4) | |
| Fish trap (`camp-fishtrap`) | CampFishTrap | Tinker 20% | Fishes a shared, finite pond; says when the next fish is due ("due around Dusk") | Needs `ShoreGround`; set-down offers no cell elsewhere (a rule, not a bug) |
| Settling tray (`camp-tray`) | CampSettlingTray | Tinker 20% | Catches a free reagent at a source (4.4) | Refuses cells beside a hazard |
| Shade awning (`camp-awning`) | CampAwning | Tinker 20% | Shade for a radius: removes the Beating's glare stacks while you stand in it | |

Tests: `CampBoxTests`, `CampHearthTests`, `CampHearthRestTests`, `CampBenchTests`, `CampKitVerbsTests`, `CampFireQolTests`, `CampFireUxTests`, `CampKitReviewTests`, `CampKitShowcaseTests`, `HeldFixture*Tests`, `HeldPlaceZoneClassTests`, `PlacementCheckTests`, `FishTrapTests`, `ShadeAwningTests`, `StillSettlingTests`, `NaturalHeatSettlingTests`, `FireDouseTests`. Doc: [HOME-BASE-IMPLEMENTATION.md](HOME-BASE-IMPLEMENTATION.md), 3a-3e, 3j, 3m, 3n, 3o, 3p.

### 4.2 Carried tools and weapons of the environment

| Tool | Use | Cost and limits | Code, tests, doc |
|---|---|---|---|
| **Flint striker** (Tinker 40%) | "Strike a spark" toward an adjacent cell where one point of heat would light a combustible coating (oil, grease); also lights a torch or hearth through their ordinary verbs when no real fire is near | Finite sparks; renewed by knapping with an iron spike; non-stacking | `FlintStrikerPart`, `IgnitionSources`; `FlintStrikerTests`, `FlintStrikerAdversarialTests`; 3 |
| **Sounding rod** (Tinker 30%) | In a deep ordinary cave, "sound the ground": names open ground, a way up or down, dense unseen stone, as a bearing and a distance; "read the survey log" reprints the last eight soundings | Loud (rings through stone); never says what is there | `SoundingRodPart`; `SoundingRodTests`; 3g |
| **Scraper** (Tinker 25%) | Beside a steam vent, ash bed, frost vent, ice sheet or a pool of brine, acid or mire: take one reagent grain at once (VentScale, RimeFlake, BrineCrust, AcidCrust) | Draws the same finite `SourceStockPart` a tray uses; every scrape is loud; the catch is unsellable; the only way to carry cold out of the deep | `ScraperPart`; `ScraperTests`; 3u |
| **Hush strips** (Tinker 25%, bundle of 4 to 8) | Lay a lane of felt on cells; steps on them make no noise, so sound-hunting pale stalkers do not turn toward you; take up returns the same item | Noise from striking is still heard | `LayableDevicePart`/`LaidDevicePart`, `Noise.MarkStep`; `HushStripTests`; 3t |
| **Spring trap** (Tinker 20%) | Load FireMoss, FrostLichen or SparkRoot, lay it on a cell; when stepped on it writes heat, cold or charge to the tile and the existing reactions do the rest (oil plus fire moss ignites; water plus frost lichen freezes; water plus spark root shocks) | Single use; refuses a fire load in Grovelands (Choir law); bare ground only marks | `SpringTrapParts`, `TriggerOnStepPart`; `SpringTrapTests`; 3v |
| **Cord snare, waterskin, flasks, torch, seeds, grease, grit...** | Existing items; now reachable from the C menu of what they act on (4.7) | | `CordSnareActions`, `WaterskinPart`, `LiquidVesselPart`, `TorchLightPart`, `SeedPart`, `MaterialFieldActions`; 3x family |

### 4.3 World machines (objects in towns)

| Machine | Where | Use | Notes |
|---|---|---|---|
| **Mine pump** | Cinderhold, beside the well | The town's heart: states (sheared rod, clay-plugged, running) driven by the concern; "mend for good" needs two iron spikes, a haft and binding (the reclamation chain's products) | `MinePump`; generic `PlaceSiteObjectPart`; 3f |
| **Fishing weir / guest pole / survey line / pan scales** | Tine / Gantry / Drowned Ledger / Wellmeet | Bodies for the existing repair sites (read the site state, mend through the repair chain) | Gantry's guest pole is the one exempt site; Wellmeet scales read "are" (`Plural`); 3i |
| **Sorters' frame** | Posy square (fresh worlds) | Set bright, cheap found things into six places; the Bower-Folk's regard rises; fills once, never pays again; a stranger sees one greyed row pointing at it | `SortingFramePart`, installation kind "Works"; `SortingFrameTests`; 3s |
| **Setters' bench** | Posy, near the frame | With regard 8 or more: "lacquer <garment> to tier N" (pitchpod resin; armor rises at once; tier cap grows with regard, top tier at 38) and "set the Bower sign on <piece>" (glow quartz; a worn signed piece raises regard by 5) | `SettingBenchPart`; `SettingBenchTests`; 3w |
| **Concord carriage** | Cinderhold and the Last Counter | Entrust a carried stack to a Concord keeper (fee 2 + 1 per 5 weight through `Ledger.TryDebit`, 3 days); collect at the other post; clause five and `CanBeTraded` refusals; six parcels at most | `ConcordCarriage`, `ConcordCarriagePart`; `ConcordCarriageTests`; 3k. Tally keeps no ledger (its composition rejects creatures), so there are two posts |
| **Forges** | Gantry, Tine, Slip (convenience rows) | Existing forge picker; "works here" lines everywhere | Last Counter row pulled to Pending (3D library has no forge family) |
| **Proving post** | Workshop scenario only | Calls elemental foes and hazards to test proofing; "mend yourself", "send away" | `ProvingGroundPart`; `ProvingGroundTests` |

### 4.4 The proofing system (the treatment bath, built as "proofing")

Design record: [MACHINE-PROOFING.md](MACHINE-PROOFING.md) (3q, 3r). The owner's direction: "property-driven reagents + status dampening; temporary coats from a bottle, permanent from a machine".

- **Status dampening** (`ElementalStatusResistance`, seam `StatusEffectsPart.ApplyEffectInternal`): a positive resistance leaves (100 - r)% of Frozen's cold (floor 0.10), Acidic's corrosion, Electrified's duration and stun, a fuel-less Burning's duration; 100 or more is immune ("You shrug off the freezing."). Vulnerabilities and forced applies are untouched. `ProofingWorthTests` pins the table quoted in the doc.
- **Element table** `Content/Data/Alchemy/Proofing.json`: heat, cold, electric (conductive), acid (corrosive): which reagent, which liquid, which source tile, what collects. Four liquids (`heat-proofing`, `cold-proofing`, `shock-proofing`, `acid-proofing`; colours must be one of the 12 `PouredLiquid3DLibrary` swatches; the liquid census pins the count).
- **Reagents** (property-driven): traders stock fire moss, ember fruit, frost lichen, glacier salt, glimmer brine, spark root; casters carry their own (Cindercaller fire moss, Stormbinder spark root, Soursprayer glimmer brine); the settling tray and scraper give four free reagents from sources.
- **Bottle path**: reagent -> "press into a flask" (`ProofingPressActions`, level = coats) -> on the flask "coat <garment>" (`ProofingCoatActions`: +35 resistance for 100 worn turns, cap 300, same element tops up, another replaces; works on worn armor).
- **Machine path**: set the trough down; "proof <garment> against <element> for good" (4 units of that proofing plus 2 fire clay: +25, `EnhancementProofed`, permanent); "unpick the <element> proofing" frees the slot; also clay-wash against fire (`EnhancementSteeped.Kind`, +35 heat resistance, 2 fire clay).
- **Readers**: rows say what your resistance would become, and when it would be immunity; `ElementalThreats` lists what the viewer has seen (elemental skills of visible creatures, tile heat/cold/charge, pool damage type) in the trough reader and row hints.
- **Free reagents are unsellable** (`NoTrade`); `StackerPart.CanStackWith` refuses NoTrade/tradable merges (anti-laundering).

### 4.5 Systems the machines rely on (rules to respect)

- A **kit** cannot be set down orthogonally beside a vent (tile hazard), so a still beside a vent works at 2 cells. SteamVent does not radiate; AshBed and FrostVent do. Cold has NO surface source: frost vents and ice sheets are underground only, which is why the scraper is how you bring cold up.
- **Placement** refuses a cell with a hazard orthogonal neighbour.
- **Equipping removes an item from `pack.Objects`**: use `Objects` plus `EquippedItems` when you mean "garments the player has".
- **Armor of one blueprint stacks** in the pack; treat a stack one unit at a time (`GarmentStacks`).

### 4.6 Machines from the twelve that are NOT built

| # | Machine | Status |
|---|---|---|
| 3 | Restoration cradle | Design: merge into the rig |
| 4 | Assay cabinet | Design; weakest |
| 5 | Culture vat | Design; needs lore read and the Quiet's Door standing |
| 6 | Anchored capstan | Parked by the owner |
| 8 | Thermal exchanger | Firing and the vent tap built; directed heat remains design |
| 9 | Dynamo | Flint striker built; charge rod needs a lore read |
| 2 | Fractionating still | Settling/baking built; ward-oil rendering needs a new process kind and a lore read |

Formal "complete-machine gates" in [MACHINE-SYSTEMS-EXECUTION-PLAN.md](MACHINE-SYSTEMS-EXECUTION-PLAN.md) were not closed by the invented-use builds; that plan is the authority on gates.

### 4.7 Tool rows: what shows on which cell menu

Scraper (sources), waterskin and flasks (fill from water, pour, pour one, douse), hush strips and spring trap (lay), cord snare (lay), camp kits (set down; make camp), seeds (plant here / plant prepared bed), sounding rod (own cell), torch (light at nearby fire; touch lit torch), cooking (cook this stack beside a fire), clay (smother), grease and grit (spread, scatter), glow quartz, gas fan, grip wrap, brace boots excluded. The list is the same as the checklist in [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md) section 2e2.

## 5. Scenarios (hand-test entry points)

Menu: **Caves Of Ooo > Scenarios > Home** (start from the menu, not by pressing Play first). Entries: Camp Kits (3 variants: indoors, wild ground, night and hurt), Camp Machines (wild ground), Camp Awning (the Beating at noon), Sounding Rod (an ordinary cave), Proofing Workshop, Concord Carriage. Code in `Assets/Scripts/Scenarios/Custom/` (`CampKitsShowcase`, `CampMachinesShowcase`, `CampAwningShowcase`, `SoundingRodShowcase`, `ProofingWorkshopShowcase`, `ConcordCarriageShowcase`). Wild starts can request a biome and depth (`ScenarioRunner.LaunchWild<T>(biome, depth)`, `FreshGameStart.WildStartZoneId`). A scenario flow needs a read-through of the real bootstrap steps after placement, not only a placement test (the bootstrap's farm-plot seeder once grassed a scenario's own cave cell: `FreshGameStart.WantsFarmPlotAt`).

## 6. Verification

### 6.1 Method (what I did for every slice)

1. Read the references and lore; write the plan section; list false premises found (the checklist caught several: a spring trap that was assumed to be a `LayableDevicePart`; a "free" reagent that minted stock because capacity 3 was below the held cap 4).
2. RED first, or **mutation checks standing in for RED**: apply one change to the clone's copy of the production code and expect a named test to fail. Survivors are either equivalent mutants (no behaviour change) or real test gaps; close real gaps with a new test and re-kill. Several slices only became correctly tested this way (three mutants survived the first test set of the camp work; the tool rows' first batch found seven test gaps).
3. Counter-checks for every positive assertion; adversarial file for state/parser/cross-actor/save surfaces.
4. **Independent read-only review** by a fresh agent with a hypothesis-driven brief ("what would a player do that the tests do not?"), told not to run Unity or edit. Confirm each claim against the code and a RED test before acting. It found real defects every time: a dropped kit that still worked as a station; a hearth light that never reached the light map; a taken-up burning hearth that stayed lit; tray stock per cell instead of per source blob; a reagent that over-promised; an F1 reader that dropped the tool rows; an untagged spring trap; "make camp" tied to the first kit; state-blind dedupe.
5. Broad regression families, then a full run when a global rule or a shared reader changed.
6. Docs updated in the same commit; commit and push.

### 6.2 The clones and the scripts

Verification clones live in `/Users/steven/coo-verification/` (`clone`, `clone2`, `clone3`, `clone4`, `clone5`; each ~13 GB nominal, APFS copy-on-write). Results go to `/Users/steven/coo-verification/results/` (durable); the baseline XML is `/Users/steven/coo-verification/baselines/full_s9.xml`. The durable scripts are in [`Tools/CloneVerification/`](../Tools/CloneVerification/README.md): `clone_run.sh`, `mutation_run.sh`, `each_mutant.sh` (+ `mutant_report.py`), `red_run.sh`, `at_commit.sh`, `compare_full.py`, `new_clone.sh`, shared `env.sh`. They replace the older `~/bin/coo_cli*.sh` / `coo_mutN*.sh` scripts (which write into a session scratchpad that disappears). A clone-busy guard refuses to start a second Unity on a busy clone (I did that twice by using the wrong script; the second instance only bounces off the project lock, but the rsync still rewrote sources under the first).

Rules: unique result names; never edit production code while a mutation batch runs (each run re-syncs); two concurrent Unity runs cause 180 s timeouts in time-sensitive tests, so re-run any new failure ALONE before believing it; a log with `error CS` is not a result; a frozen test count means a stale assembly.

### 6.3 Commands

```bash
T=/Users/steven/caves-of-ooo/Tools/CloneVerification
$T/clone_run.sh clone3 'CavesOfOoo.Tests.ScraperTests|CavesOfOoo.Tests.WorldToolRowsTests' scr1     # a subset (2-4 min)
$T/clone_run.sh clone2 'CavesOfOoo' fullrun_h                                                         # EVERYTHING (about 3 hours): run in the background
python3 $T/compare_full.py /Users/steven/coo-verification/results/fullrun_h.xml                       # vs baseline; reruns needed for [TIMEOUT]
$T/red_run.sh clone3 'CavesOfOoo.Tests.XTests' red1 Assets/Scripts/Gameplay/Items/XPart.cs          # new tests vs the HEAD version of a file
$T/each_mutant.sh clone3 spec.json 'CavesOfOoo.Tests.XTests' mut1 && cat ...                           # one clean run per mutant
$T/at_commit.sh clone4 <commit> 'CavesOfOoo.Tests.XTests' bis1                                          # did it already fail at that commit?
```
A mutation spec is JSON: `[{"id":"m1","file":"Assets/Scripts/...cs","old":"exact text","new":"changed text"}]`.

### 6.4 Test baseline and verdict history

The baseline `full_s9.xml` (2026-11-19) has 39,500 tests and 91 failures that already failed before this work; they are listed by `compare_full.py` only when they change. Groups seen in them: ClaimedSupplies take-all, Felling CachedSleepMenu (3), pause-menu row counts (2), two spell-scenario cases, Tinker receipts (2), equipment approval (7), haulables table, Grazer count, Cinderhold late profile, BoundedWholeStack distribution, `CompletedNativeTransitionSoundsFirstHighTierArrivalOnce` (4), `PlayerAndCurrentEquipmentRemainApprovedThroughWearCarryAndGroundTransitions("Torch")`, `OriginalFieldResidentsHaveActualRoleStockAndSmallPurses`, plus order-dependent cases that pass alone.

| Run | Head | Result |
|---|---|---|
| `fullrun_b` (2026-11-20 08:56) | `cc155a254` | 39,746 of 39,854 passed, 91 failures = the baseline exactly (0 new, 0 fixed) |
| `fullrun_c` (11-20 12:52) | `e782e76ef` | 92 failures: 3 new (a fish-trap pin fixed in `5796a0c2f`; two timeouts passing alone); 2 baseline failures passed |
| `fullrun_d` (11-20 17:16) | `f7a2d6e7c` | 96 failures: 5 new, all explained (the same fish-trap pin; four 180 s timeouts under contention), all pass alone |
| `fullrun_e` | `a7f5b5cc6` | abandoned after 4 h 22 min, no result |
| cumulative family run `fam10` (11-21) | `19fab8606` + | 22,062 tests: 54 baseline failures; 2 "new": Posy frame/bench had no 3D model (fixed by the glyph-only list, `787b35c60`) and `TrinketEquipmentTests.InventoryRowsComparison...` (not reproducible, passes alone); one REAL regression found late and fixed (`376c5f067`) |
| tool-row wide runs (11-21) | working tree | 4,397 and 4,410 tests: 0 new failures against the baseline (6 baseline failures in those areas); the relevant suites unmutated 426/426 |
| `fullg` (clone2, 11-21 10:25) | working tree during the tool-row work | still running at 3 h 16 min when this was written; it predates the final head, so it is not the gate |
| **`fullrun_h`** (clone4, 11-21 13:42) | `e89a81af6` (final head of this run) | **running when this was written**: the gate. Compare with `compare_full.py`; the verdict is recorded here by whoever reads it first |

### 6.5 Lessons and pitfalls (each cost me time)

Verification and tooling
- `read_console` misses compile errors: grep the Editor/batch log for `error CS`.
- Subset runs miss cross-system regressions. After a change to a global rule (status application, stacking, noise, a shared reader), run the Equipment/Inventory/Item families too, not only the proofing ones (a WetCharge test broke that way). After adding any blueprint that appears in a generated settlement, run the rendering and coverage families and a full run.
- A new kit must be run against `CampBenchTests`, `HeldFixtureActionTests` (it pins the shipped kit list), and every test class that enumerates `PackableFixtureRegistry.All`.
- A new blueprint in a generated Spread town needs a native model or a line in `PreviousPresentationTierGap.cs`. A new installation row needs the town's real ground list (`no-cell` with probed=48 means the ground list or radius is wrong). Wellmeet's chunk is fully 3D-modelled: an unmodelled blueprint fails a test there.
- Tests that mix enhancement suites need `EnhancementFactory.ForceReinitialize()`. `Entity.SpatialZone` is not visible to tests. `GatherActions` needs the trough ADJACENT to the actor and the cell visible; `Place("Grass")` cells are not visible by default (set `IsVisible`/`Explored`).
- Disabled rows have an EMPTY `Command`: find them by `DisabledCode`.
- Real movement goes through `TryMoveDetailedCore` (not `TryMove`): test both.
- Mutation pitfalls: `false && A || B` can be a no-op; removing an `out var` breaks the build; a mutant with compile errors is INVALID, not killed; mutations in one spec must have disjoint expected tests; never combine mutants in one run when you need a verdict per mutant.
- Do not read `ThermalPart.Temperature` of a vent (it decays); read the declared heat.
- Several three-hour full runs on one machine make time-sensitive tests time out: re-run alone.

Process
- Confirm every review claim against the code and a RED test before acting; reviewers are sometimes wrong and sometimes right in ways the author missed.
- The commit message of `d4a31ec11` says "40 new tests"; the real number is 21 (plus tightened assertions). It is pushed; do not rewrite history to fix it.
- When a slice is "done", ask what the player would do that the tests do not: that question found every real defect.

## 7. Repo state, git rules, what not to touch

- Branch `main`; push to `origin/main` after every verified slice. Trailer on every commit: `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Never force-push; never `git add -A` or `git add .`; stage explicit paths and the `.meta` files of new files.
- **Foreign changes to preserve (about 84 tracked files plus untracked ones):** many `*.png.meta`/`*.json.meta` importer rewrites under `Assets/Resources/`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/*`, stray `Assets/InitTestScene*.unity`, `_to_delete/`, `Docs/RELEASE-CANDIDATE-*.md`, `ProjectSettings/Packages/com.unity.probuilder/`, `Assets/Scripts/Editor.meta`, images under `Docs/Verification/`, `ArtSource/**` logs and `.blend1` files. They belong to other work (graphics, the owner's editor). Do not stage, revert, reformat or delete them; inspect with `git status --short | grep -v '^ M'`.
- `CLAUDE.md` is long and historical. Its many "Current graphics ..." paragraphs are superseded by the owner's 2026-11-16 cancellation of graphics. The "Current machine continuation" paragraph at its top (2026-11-17) is superseded by this handoff (its "no new runtime changes" no longer holds). The paragraphs that still bind: the central design philosophy, the TDD/review/adversarial/performance rules, the Unity MCP workflow notes.
- **Automation.** CLAUDE.md mentions an ACTIVE hourly task `caves-of-ooo-machine-foundations` coordinating Unity/Git ownership. At the time of writing the Claude scheduled-tasks list shows only two disabled tasks (brainstorm, lore analysis), so that task is either another agent's automation or gone. Before running Unity or committing, check `pgrep -fl Unity` and `git log -3` for activity from another agent.
- Unity version 6000.3.4f1 at `/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app`. The MCP server (`~/bin/coo_mcp_keeper.py`, `~/bin/mcp_r.sh`) dies whenever any Unity quits; `clone_run.sh` restarts it. Stop the keeper with `pkill -f coo_mcp_keeper.py` at session end.

## 8. Honest limits (read before trusting any of this)

- Nothing after the box, hearth, bench, still and map note has been played by a person or by the Editor. Hint text and menus are verified through menu state, not a rendered frame.
- Wording is placeholder pending the owner's voice review; numbers (fees, turns, degrees per unit, sound radius, regeneration rates) are proposals.
- Posy's frame and bench exist only in freshly generated worlds (cached towns keep their old content, by design).
- Art: all new blueprints are glyph-only. Linux and Steam Deck execution were never run.
- The carriage has two posts (Cinderhold, Last Counter); the Tally keeper is blocked by a composition rule (a chip was offered, `task_6bfa5ca4`).
- The survey rod's, spring trap's and scraper's numbers have no balance playtest.
- Not claimed: any formal V0-V7 / whole-area / gate closure from the older plans.

## 9. Open decisions for the owner

1. Whether plain "plant here" should stay free in both menus while "plant prepared bed" costs a turn.
2. The grouping rule: threshold 3 and the heading set (`ToolGroups`) are tunable; say if the grouped menu still feels long or gets in the way.
3. The Gantry, Tine and Slip convenience forges (data rows; deletable before release); the Last Counter forge needs a 3D family.
4. Voice review of all placeholder strings; glyph-only art for the kits and tools.
5. Rest-anywhere (a portable bed) is withheld as a balance lever; the camp hearth already allows rest beside a lit fire.
6. A portable seedbed (reverses a written deferral; needs a fertility and economy call).
7. A second hearth fuel from DryBrush bundles (on the owner-blocked list).
8. Hand-over objects for the five to seven dialogue-only Supply approaches (data: `Concerns/*.json` `Supply`; `ConcernConversation.HandSupply`); skip quiet.dim ward oil and the Gantry bowl (lore).

## 10. Next steps

### 10.1 The descriptions pass (built 2026-11-21, commit `e89a81af6`; the owner's request, kept for reference)

Owner: "instead of a long description for these tools and machines that doesn't tell the player exactly how they work or how to use them, the descriptions should do exactly that - tell the player how to use them, if they're usable in that moment, or what materials are needed, or what environmental factors are needed, etc to use them. go through all the machines and tools and do this."

**What was built** (full record: [MACHINE-READERS.md](MACHINE-READERS.md) "Update 2026-11-21" and [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) 3x). A second reader contract beside the state readers: `IDescribeUse` + `UseGuide` + one collector `UseGuides.Collect` (`Gameplay/Entities/UseGuide.cs`). The Part that owns a thing's verbs fills three lists (how, needs, limits) from the live constants of the rule that enforces each number, and may name why it offers nothing right now. The collector composes `How to use / Needs / Cost and limits / Right now`, and builds the live "Right now you can: ... / you cannot: ... (reason)" line from the very `GetInventoryActions` rows the thing's own Parts would show this viewer, so it cannot promise what the game refuses and it repeats the greyed row's own reason. Both Examine readers show the block ahead of the flavor (quick: lines; full document: a "How to use" section). The static blueprint descriptions of the machines and tools are cut to what the thing is (at most 130 characters, no number, no instruction word: pinned by a test); `DescribeState` of the providers that used to mix instructions into state keeps only state.

**Covered:** the eight camp kits (generic set-down/take-up guide on `PackableFixturePart`, ordered first while carried and last while standing, with the ground rules, the countries the placement allows and the caps from `PlacementCheck`/`HeldPlaceZones`; the box adds its capacity), the hearth and resting by it, the still (brew, settle, bake), every forge and the camp bench, the trough, the tray, the fish trap, the awning, the flint striker, the sounding rod, the scraper, hush strips and the laid strip, the spring trap, the Sorters' frame, the Setters' bench, the proving post, every repair-site object (pump, weir, survey line, pan scales), the waterskin and the flask. A carried tool lying on the ground says "pick it up first".
**Not covered, on purpose:** the torch (its live flame and refuel lines already say how: `ItemExamineService`), crafting materials and seeds (the "Field uses" section), the Concord's carriage (a conversation), the field work rig (its accepted M1b description is a tested contract that already states its yield, loss and hauling rules; it keeps its own "work at the rig" rows), reagents (the press verb is on the reagent; add a guide to `ReagentPart` when wanted), and old authored furniture.
**Add a guide to a new machine:** implement `IDescribeUse` on the Part that owns the verbs (checklist item 5 in section 3.2); a kit gets the shared set-down text for free from its `PackableFixturePart`. A thing whose rows are added outside its own Parts' `GetInventoryActions` handlers needs `IDescribeUseRows`.
**Verification:** `UseGuideTests` (the collector, 23 cases) and `MachineUseGuideTests` (31 cases: each machine in the states a player meets it in, every shipped kit carried and standing, a handed-over fixture, a far kit, a tool on the ground, a stunned actor, the forge, still, skin and hearth rest stating their own rows, the descriptions pinned short); eight older tests that pinned the mixed text were updated to read the right source. Runs on the final code: the guide and machine families 1,080 tests (the one failure was a test of mine placing a tool out of reach; fixed, and the focused rerun is 340 of 340); the examine, inventory, item and crafting families 3,668 tests with 8 failures that are all in the 2026-11-19 baseline (0 new). Mutation check: 41 mutants of the collector, both readers' integration and the guides, all killed. **An independent read-only review found real defects** (inert picker headers listed as actions, distant or ungated rows saying "you can", a merchant's stock previewed as if the merchant were the viewer, a handed-over fixture described as takeable, wrong forge and striker wording, a missing ground rule): all fixed with tests, listed in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) 3x. **Every wording is in [MACHINE-USE-TEXTS.md](MACHINE-USE-TEXTS.md) for the owner's voice pass. Not played in the Editor.**

### 10.2 The final regression gate

The descriptions pass is committed and pushed (`e89a81af6`) and the gate run was started: `Tools/CloneVerification/clone_run.sh clone4 'CavesOfOoo' fullrun_h` (2026-11-21 13:42, about three hours; avoid other Unity runs on this machine while it goes, they cause 180-second timeouts). When `/Users/steven/coo-verification/results/fullrun_h.xml` exists, run `python3 Tools/CloneVerification/compare_full.py /Users/steven/coo-verification/results/fullrun_h.xml`, re-run every NEW failure ALONE with its exact class filter (a timeout that passes alone is contention, not a regression), fix any that fail alone, and write the verdict into the table in section 6.4. Replace the baseline copy in `baselines/` only if the owner agrees the failures it lists are still the accepted set. If the run died (the XML is missing or short), start it again; do not trust a partial file.

### 10.3 A hand-play session (the single most valuable verification still missing)

Follow [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md) (sections 1 to 3) in the Editor from the Home menu; the guide says what each step should show. Log defects with the exact row text, what the small text promised, and what happened. Diag records exist for every machine (`furniture/HearthDoused`, `trade/CarriageSent`, `skill/Sounded`, `furniture/ScraperRefused`, ...), so "it did nothing" is answerable with `diag_query`.

### 10.4 Candidate builds (each needs a lore read first; section 9 has the owner-blocked ones)

1. Hand-over objects for dialogue-only Supply approaches.
2. Spore-salting as a fifth proofing element from PaleSalt (Pale Curation lore).
3. Ward-oil rendering at the still (needs a new process kind; lore read of ward).
4. Charge rod (the dynamo's second use; lore read of binding-work, Sumphold East Cut consumer).
5. Tally ledger keeper (blocked by the composition validator; `VillagePopulationBuilder` owns service NPCs).
6. PB7: column audit so kits can be set down underground (the scraper is the interim path for cold).
7. Directed heat plume and a vent tap (thermal exchanger).
8. Lamp stand at Quillhold/Slip (marginal; needs a Supply hand-over hook); plaque tending at Lampwell is blocked (ForbiddenFactions).
9. Culture vat (a tended colony jar) once the Quiet's Door standing and lore are read.

## 11. Index

**Docs (read in this order if you need depth):** [SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md) -> [MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md) -> [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) (every build, section 3a to 3w; considered-and-not-built, section 5) -> [HOME-BASE-IMPLEMENTATION.md](HOME-BASE-IMPLEMENTATION.md) and [HOME-BASE-DESIGN.md](HOME-BASE-DESIGN.md) -> [MACHINE-ARCHITECTURE-SPEC.md](MACHINE-ARCHITECTURE-SPEC.md) -> [MACHINE-PROOFING.md](MACHINE-PROOFING.md) -> [WORLD-TOOL-ROWS.md](WORLD-TOOL-ROWS.md) -> [MACHINE-HAND-TEST.md](MACHINE-HAND-TEST.md) -> [MACHINE-SYSTEMS-EXECUTION-PLAN.md](MACHINE-SYSTEMS-EXECUTION-PLAN.md) (dated updates) -> `MachineDesign/` (the per-machine dossiers and critiques) -> the record per contract: [INSTALLATIONS](MACHINE-INSTALLATIONS.md), [STATIONS](MACHINE-STATIONS.md), [READERS](MACHINE-READERS.md), [ACTION-ROWS](MACHINE-ACTION-ROWS.md), [MULTI-STACK-CLAIMS](MACHINE-MULTI-STACK-CLAIMS.md), [STATION-USERS](MACHINE-STATION-USERS.md).

**Code folders:** `Assets/Scripts/Gameplay/Items/` (tools, kits, readers), `Gameplay/Installations/` (registry, packable fixtures, held-fixture actions), `Gameplay/Alchemy/` (proofing, settling, sources), `Gameplay/Settlements/` (site objects, campfire work, rest), `Gameplay/Interaction/WorldInteractionSystem.cs`, `Gameplay/Effects/ElementalStatusResistance.cs`, `Gameplay/Economy/` (carriage, ledger), `Presentation/Input/InputHandler.cs`, `Presentation/UI/WorldActionMenuUI.cs`, `Scripts/Scenarios/Custom/`. **Content:** `Resources/Content/Blueprints/Objects.json`, `Data/PackableFixtures.json`, `Data/Installations.json`, `Data/Alchemy/Proofing.json`, `Data/LiquidDefinitions/*proofing*.json`, `Data/Loot/LootTables.json`.

**Tests by feature** (`Assets/Tests/EditMode/...`): Alchemy: `ProofingRegistryTests`, `ProofingPressTests`, `ProofingCoatTests`, `ProofingTroughTests`, `ProofingWorkshopTests`, `ProofingWorthTests`, `ProvingGroundTests`, `ElementalThreatsTests`; Effects: `ElementalStatusResistanceTests`; World: the `Camp*`, `HeldFixture*`, `Placement*`, `Installation*`, `FishTrapTests`, `ShadeAwningTests`, `StillSettlingTests`, `NaturalHeatSettlingTests`, `FireDouseTests`, `FlintStriker*Tests`, `SoundingRodTests`, `ScraperTests`, `HushStripTests`, `SpringTrapTests`, `SettlingTrayTests`, `SortingFrameTests`, `SettingBenchTests`, `PlaceSiteObjectTests`, `WorldToolRowsTests`, `WorldToolFamilyRowsTests`; Presentation: `WorldToolRowsInputTests`, `ActionTimeDeclarationTests`; Economy: `ConcordCarriageTests`; Save: `SavedPartContractTests`.

**Memory (Claude project memory):** `machine-program-state.md` (diary of the same facts), `commit-and-push-by-default.md`, `clone-verification-chains.md`, `headless-unity-loop.md`, `mcp-server-relaunch.md`, `main-editor-play-check.md`, `lore-corpus-authority.md`.

## Appendix A: the owner's directions, in order (quotes)

- 2026-11-16: "Build machines, NPCs and objects from composable Parts, reusable capabilities and common events ..." (CLAUDE.md central philosophy).
- 2026-11-17: "These are the machines I'd be most interested in developing." (the twelve; [MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md)).
- 2026-11-19: "if a use for a machine does not exist, analyze the game and the purpose of the machine and invent a use for that machine that flows naturally with the rest of the game mechanics and the lore"; "you have full free reign of the game"; continue "inventing machines, QoL pass after each, as long as possible, free rein except deleting the game"; sixteen camp quality-of-life items (all but "camp as a system" and "banking the fire"); "describe in game with small text what stoke does, when you highlight it".
- 2026-11-20: proofing: "property-driven reagents + status dampening; temporary coats from a bottle, permanent from a machine"; "commit and push it once the tests pass, then start on another machine. invent and create and design and implement its uses in game, and have it make sense with the world, gameplay, and lore. do this for 5 new machines. do this for as long as you can without my intervention"; "the machines don't have to be for camp, they can be wherever in the world you think makes it most engaging and interesting and fun".
- 2026-11-21: "for the scraper, i shouldnt need to go into my inventory to use it. if i'm next to the cell that has a vent or something that gives off an element, i should be able to use the c button on the vent ... and select to scrape"; "do this quality of life thing for all tools and machines that can be used in or on the environment"; "commit and push it, always"; "group similar actions into one row that opens up and gives you the options. whatever design you think is best so it's not a long wall"; the descriptions request (section 10.1); "make a thorough handoff doc for another agent on what you've done these past few days, what the motivations and goals are, and anything else that is pertinent for the next agent" (this file).

## Appendix B: user-facing behaviour changes worth knowing when you play

- The C menu on a cell now ends with rows for your carried tools that act there; crowded kinds show as one grouped row. Tool rows run the tool in your pack without opening it.
- A resistance now shortens or weakens a status as well as the damage.
- A lit camp hearth is water-sensitive (pouring a non-fuel liquid cools it).
- Walking on laid hush strips makes no noise; scraping and sounding do.
- Examining a flask says what its liquid is good for; a refused set-down says what ground the kit needs.
