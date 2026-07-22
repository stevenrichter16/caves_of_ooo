# Gather / Loot System — plan

> **Status: EXECUTED (M1-M4).** Two placement mechanisms shipped:
> creature drops (loot on death, via CorpsePart) and gather nodes
> (renewable forage/mineral points, via GatherNodePart), both
> resolving from a shared data-driven LootTable. A first content
> slice (6 blueprints, 4 loot tables, 2 PopulationTable wirings, 1
> creature-drop wiring) proves the system end-to-end.
>
> **Honesty bound:** this session has no Unity editor. All C# was
> reviewed line-by-line and all JSON is parse-validated (see the
> commands in each milestone's commit), but the EditMode suite has
> **not been run**. Run it first thing next Unity session — 30
> new/changed tests across `LootTableRegistryTests`,
> `GatherNodePartTests`, `WorldIngredientsContentTests`, and the
> `CorpsePartTests` additions.
>
> **Goal:** answer "how much of the world's craftable/alchemical
> items can the player actually find?" with "a real system, not
> hardcoded lists" (user direction, option B). Two placement
> mechanisms: creature drops (loot on death) and gather nodes
> (renewable forage/mineral points), both resolving from a shared
> data-driven loot table.

---

## Pre-implementation verification sweep (findings table)

Read before writing code, per CLAUDE.md §1.2. Corrections found:

| Assumption going in | What's actually true | Impact |
|---|---|---|
| "There's probably some container/drop system already" | `ContainerPart` exists (full model: Contents, Open/Loot action) but nothing ever pre-fills it; `WoodenBarrel` looks lootable but has no ContainerPart at all | Reuse ContainerPart as the drop *destination*, don't reinvent it |
| "Death probably doesn't have a clean hook" | `CombatSystem.HandleDeath` already fires a `"Died"` event with `Target/Killer/Zone` params, and **`CorpsePart` (M5.1, fully shipped + tested)** already listens to it and spawns a corpse entity at the death cell | Extend CorpsePart additively (one new optional field), don't build a parallel death-hook |
| "Terrain-object interaction probably needs new UI plumbing" | `WorldInteractionSystem` (`ResolveTarget` + `GetInventoryActions`/`InventoryAction` event pair) already drives world-object actions from Parts, and is already wired to input/UI (`WorldActionMenuUI`, `InputHandler`) | A new `GatherNodePart` needs zero UI work — declare an action, handle the command, done (same shape as `GrimoirePart`/`SchematicPart`) |
| "Custom part state will need hand-written save code" | `SaveSystem.WritePublicFields`/`ReadPublicFields` (reflection path) already supports `int/string/bool/Entity/IList<T>` — confirmed `ContainerPart` has **zero** special-case save code and already round-trips via the generic path | New parts with only simple public fields need no save code at all |
| "There must be a loot-table concept already, just not applied" | Confirmed **zero** existing loot-table system. `PopulationTable`/`PopulationEntry` is the only weighted-table concept, and it's a *zone population* tool (spawns free entities in open cells at zone-gen), not an *item-drop* tool | New, standalone `LootTable`/`LootTableRegistry`, JSON-driven, mirroring `TinkerRecipeRegistry`'s init/reset test-hook convention |
| "The Sarisariñama bestiary creatures (Sari-Snake, Wardline, Gin Frog...) exist as spawnable blueprints" | They do not — only generic creatures (Snapjaw, Scorpion, CaveBear, GiantSpider, etc.) are spawnable today | **Scope cut, logged now:** M4's creature-drop wiring uses existing creatures only; new bestiary creatures are a separate content ship (needs Brain/AI/stat balancing) |
| "Diag category=loot" | `Diag.DefaultOnCategories` is a fixed on-by-default list (`event, effect, damage, turn, furniture, trade, quest, skill, enhancement, mineral-trade, worldmap, liquid, gas, gasbench, questbench, alchemy, craft`) — no "loot" | Use `category="craft"` (closest fit: this system exists to feed crafting/alchemy ingredients into the player's hands) rather than proliferate a new category |

---

## Scope-prune

Cut from this ship, with rationale:

- **New Sarisariñama creature blueprints** (Sari-Snake, Wardline, Gin Frog, Sima-Steward, eyeless predator). Spawning a new creature needs Brain/AI wiring and combat-stat balancing that's out of scope for "can the player pick things up." Their drops from `WORLD-INGREDIENTS.md` are deferred; this ship wires drops onto *existing* creatures only.
- **The full 40-item WORLD-INGREDIENTS.md catalogue.** Authoring a first reviewable slice (~8-10 items across forage + creature-drop + mineral) proves the system end-to-end; the rest is mechanical repetition of the same pattern, better done once the system is confirmed working (next Unity session).
- **PopulationTable refactor** to unify with the new LootTable shape. They're structurally similar (weighted entries, Roll()) but unifying now means touching an already-stable, tested, multi-caller system for no immediate gain. Noted as a future cleanup, not done here.
- **Standing/reputation gates on harvesting** (drosera rings, hearth-plume cuttings — the "harvesting ethics" hooks from `WORLD-INGREDIENTS.md` §IX). Real content work for a future ship; this ship's gather nodes are harvestable without a reputation check.

## Sub-milestones (smallest blast radius first)

1. **M1 — LootTable data model + registry.** New files only, zero
   existing-code changes. `LootEntry`/`LootTable` (weighted
   entries, `Roll(rng)`), `LootTableRegistry` (JSON-driven,
   `EnsureInitialized`/`InitializeFromJson`/`ResetForTests`/
   `TryGetTable`, mirroring `TinkerRecipeRegistry`).
2. **M2 — Creature drops.** One new optional field on `CorpsePart`
   (`LootTableID`); on successful corpse spawn, if set, roll the
   table and populate a runtime-attached `ContainerPart` on the
   corpse. Purely additive — every existing `CorpsePart` test must
   still pass unmodified (the new path only activates when the new
   field is non-empty).
3. **M3 — Gather nodes.** New `GatherNodePart` (forage/mineral
   points): declares a "Harvest" action via the existing
   `GetInventoryActions`/`InventoryAction` pipeline, yields items
   from a `LootTable` straight into the harvester's inventory,
   tracks remaining uses / regrow-at-turn via `TurnManager.Active`.
   New part, new blueprints — zero risk to existing systems.
4. **M4 — Content seeding.** A first slice of blueprints (reagents +
   gather-node wrappers + loot tables) from `WORLD-INGREDIENTS.md`,
   wired additively into `PopulationTable`'s existing biome tables
   and onto one existing creature's `CorpsePart`.

Each milestone: tests authored first (RED intent documented — this
environment has no Unity editor, so tests are authored, not run;
flagged honestly, same discipline as the tinkering ship), then
implementation, then self-review.

---

## Post-implementation summary

### What shipped, per milestone

- **M1** — `LootEntry`/`LootTable`/`LootTableRegistry`
  (`Assets/Scripts/Gameplay/Loot/`). Independent-per-entry percent
  rolls (not PopulationEntry's competing-weight model) so a table
  can yield 0, 1, or several drops at once. 9 tests
  (`LootTableRegistryTests.cs`): parse, case-insensitivity, weight-0
  vs weight-100 counter-check pair, count-range bounds, multi-entry
  independence, malformed-entry skip, null-rng safety, reset hygiene.
- **M2** — `CorpsePart.LootTableID` (one new optional field) +
  `RollLootOntoCorpse` (attaches/reuses a `ContainerPart` on the
  spawned corpse). 4 new tests appended to the existing, untouched
  `CorpsePartTests.cs` — including the counter-check
  (`CorpsePart_WithoutLootTableID_NoContainerAdded`) proving the
  M5.1 default path is unchanged. All 13 pre-existing CorpsePart
  tests are unmodified.
- **M3** — `GatherNodePart` (`Assets/Scripts/Gameplay/Loot/`):
  Harvest action via the existing GetInventoryActions/InventoryAction
  pipeline (zero new UI); MaxUses/RegrowTurns depletion model; a
  `TestCurrentTurn` override (not TurnManager's real tick machinery —
  see the finding below) for deterministic regrow tests. 16 tests
  (`GatherNodePartTests.cs`): basic harvest, no-factory/no-inventory
  graceful paths, single-use removal (+ the "survives until last use"
  counter-check for MaxUses>1), regrow-gating in both directions,
  display-name swap, unknown-table and dry-table edge cases (+ the
  "dry roll still consumes the use" counter-check).
- **M4** — first content slice: `PaleReed` (new reagent),
  `SnapjawFangPoint` (new weapon component), `GlowQuartz` enriched
  with a `Reagent` part (existing item, Commerce value preserved),
  three gather-node blueprints (`PaleReedNode`, `FireMossPatch`,
  `GlowQuartzSeam`), one creature-drop wiring (`Snapjaw` →
  `loot_snapjaw_drops`), 4 loot tables (`LootTables.json`), and
  additive `PopulationTable.CaveTier1()`/`CaveTier2()` wiring. 10
  tests (`WorldIngredientsContentTests.cs`) load the **real**
  `Objects.json` + `LootTables.json` — including a cross-reference
  check that every loot-table entry's blueprint name actually
  resolves, and pins on the two `PopulationTable` wirings so a future
  edit can't silently drop them.

### Findings during implementation (verification-sweep catches)

- 🟡 **`TurnManager` has no test-reset/advance-tick API.** My first
  draft of the regrow tests invented `ResetForTests()`/
  `SetActiveForTests()`/`AdvanceTickForTests()` — none exist.
  Corrected before shipping: `GatherNodePart` gets its own
  `TestCurrentTurn` override (mirrors `CorpsePart.TestRng`), which
  also decouples gather-node regrow timing from TurnManager's
  `EndTurn`/`CurrentActor`/`JustApplied` machinery (tuned for status
  effects) — a better fit, not just a workaround.
- 🔵 **Zone resolution for a non-creature Part.** Neither `Entity`
  nor `Part` carries a zone back-reference; the established pattern
  (`ChairPart`, `AIRetrieverPart`) resolves it from the *acting*
  entity's `BrainPart.CurrentZone`. `GatherNodePart.Deplete` follows
  suit for single-use removal.
- 🔵 **`ContainerPart` has zero special-case save code** — confirmed
  before relying on it: `List<Entity>` round-trips via
  `SaveSystem`'s generic `IList` + `Entity`-reference reflection
  path. Runtime-attaching one to a corpse needed no new save work.

### Next-Unity-session checklist

1. `refresh_unity` → `read_console types=[error]` must be empty.
2. Run EditMode: expect all 3 new fixtures green plus the 4 new
   `CorpsePartTests` methods; `WorldIngredientsContentTests` is the
   one most likely to catch a JSON typo this review missed.
3. Playmode sanity: kill a Snapjaw, open the corpse, confirm a fang
   point; find/spawn a `FireMossPatch`, Harvest it, confirm
   depletion + (after simulated turns) regrowth.
4. `diag_query category=craft kind=NodeHarvested` /
   `kind=CorpseLootRolled` for the observability loop.

### Continuation roadmap

1. Author the remaining ~30 items from `Docs/Design/WORLD-INGREDIENTS.md`
   once M1-M4 are confirmed working — the pattern is now proven.
2. New Sarisariñama bestiary creatures (Sari-Snake, Wardline, Gin
   Frog…) as their own content ship (Brain/AI + stat balancing),
   then their drops slot into this same LootTable system with zero
   further engineering.
3. The harvesting-ethics hooks from `WORLD-INGREDIENTS.md` §IX
   (drosera rings, hearth-plume standing-gates) — reputation checks
   layered onto `GatherNodePart.CanApply`-equivalent, deferred here.
4. The `PopulationTable`/`LootTable` unification noted as a
   scope-prune — worth revisiting once both have more callers, not
   before.
