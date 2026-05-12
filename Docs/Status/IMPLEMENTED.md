# Caves of Ooo — Implementation Record

A Qud-shaped roguelike in Unity 6 (6000.3.4f1). The core simulation has
zero Unity dependencies; only `Scripts/Presentation/` touches
MonoBehaviours.

**Snapshot (2026-05-12):**
- ~87,600 LOC across `Assets/Scripts/`, ~54,900 of that in `Gameplay/`
- 4,069 `[Test]`/`[TestCase]` attributes across 312 EditMode test files
- 220 blueprint entries in `Objects.json` (47 inherit `Creature`),
  35 in `Mutations.json`
- 9 conversation files / 378 dialogue nodes
- 65 scenarios in `Scripts/Scenarios/Custom/` (all currently
  developer-facing showcases, no shipped vertical slice)

This doc tracks what's actually playable / wired vs the deep parity
plans in `Docs/QUD-PARITY.md`, `Docs/COMBAT-QUD-PARITY-PORT.md`,
`Docs/SKILL-SYSTEM-PARITY.md`, etc.

**Status shorthand:** ✅ shipped & wired · ⚠️ shipped but no player
surface · ⏸️ deferred · 🟡 partial · 📜 documented but not started

---

## Engine foundation (Phases 1-9, original architecture)

These phases established the simulation substrate. All are stable
and load-bearing for everything that came later.

| Phase | System | Status | Key files |
|-------|--------|:------:|-----------|
| 1 | Entity / Part / Event / Stat | ✅ | `Gameplay/Entities/Entity.cs`, `Part.cs`, `GameEvent.cs` |
| 1 | Blueprint + Factory (reflection-based) | ✅ | `Data/Blueprint.cs`, `BlueprintLoader.cs`, `EntityFactory.cs` |
| 2 | Cell / Zone (80×25) grid | ✅ | `Gameplay/World/Map/Zone.cs`, `Cell.cs` |
| 2 | ZoneRenderer + CP437 tileset + QudColorParser | ✅ | `Presentation/Rendering/ZoneRenderer.cs` |
| 3 | Energy-based TurnManager | ✅ | `Gameplay/Turns/TurnManager.cs` |
| 3 | MovementSystem + PhysicsPart (bump-to-attack) | ✅ | `Gameplay/World/MovementSystem.cs` |
| 4 | ZoneGenerationPipeline + builders (Cave/Desert/Jungle/Ruins) | ✅ | `Gameplay/World/Generation/` |
| 4 | PopulationTable + ZoneManager caching | ✅ | `Data/Tables/`, `Gameplay/World/Map/ZoneManager.cs` |
| 5 | Melee CombatSystem (hit / pen / damage / death) | ✅ | `Gameplay/Combat/CombatSystem.cs` |
| 6 | InventoryPart + InventorySystem (pickup/drop/equip) | ✅ | `Gameplay/Inventory/` |
| 7 | MutationsPart + ActivatedAbilitiesPart | ✅ | `Gameplay/Mutations/MutationsPart.cs` |
| 8 | FactionManager + AIHelpers + BrainPart | ✅ | `Gameplay/AI/`, `Data/Factions/` |
| 9 | WorldMap (10×10) + ZoneTransitionSystem + 4 biomes | ✅ | `Gameplay/World/Map/WorldMap.cs`, `WorldGenerator.cs` |

---

## Combat system — Qud-parity port (Phases A-H, 2026-04)

10-phase deliberate port from Qud's `XRL.World.Combat`. All
phases shipped, each with verification sweep + adversarial sweep +
cold-eye review.

| Phase | Surface | Status |
|-------|---------|:------:|
| A | Damage typed class + attribute list (replace int → `Damage` w/ tag list) | ✅ |
| C | RollPenetrations Qud-faithful (1d10-2 exploding + per-set decay + MaxBonus cap) | ✅ |
| D | OnHit class effects + weapon-attached effects | ✅ |
| E | Resistances (Cold/Heat/Acid/Electric) + IgnoreResist bypass | ✅ |
| F | BeforeTakeDamage hook + critical-hit pipeline | ✅ |
| G | Anatomy / Body integration (severable limbs, natural weapons, dismember events) | ✅ |
| H | Off-hand / multi-weapon penalty + methodology-debt closure | ✅ |

**Known gap:** ranged combat (Qud's `Missile.cs`) is **not** ported.
Engine is melee-only. See `Docs/COMBAT-QUD-PARITY-PORT.md` for full
phase docs.

Reference docs: `Docs/COMBAT-QUD-PARITY-PORT.md`,
`Docs/COMBAT-PARITY-PORT-REVIEW.md`, `Docs/COMBAT-BRANCH-MAP.md`.

---

## Goal-stack AI — Qud-parity port

| Phase | Surface | Status |
|-------|---------|:------:|
| 0 | GoalHandler base + BrainPart._goals (LIFO) + age tracking | ✅ |
| 1 | A* pathfinding (`FindPath.cs`, pool-based, 2000-node cap) | ✅ |
| 2 | StartingCell / Staying / WhenBoredReturnToOnce | ✅ |
| 3 | AIBoredEvent + AIBehaviorPart base | ✅ (2 subclasses shipped) |
| 4 | IdleQueryEvent + SittingEffect + ChairPart/BedPart | ✅ |
| 5 | PushGoal / PushChildGoal / Pop / FailToParent / HasGoal | ✅ |
| 5 | InsertGoalAfter and 12 other insertion overloads | ⏸️ (no production callers) |
| 7 | AIShopper / AIPilgrim / AIShoreLounging | ⚠️ infra, no content |
| 8 | Party / Follower system | ✅ (F.1-F.3 shipped) |
| 12 | Calendar / world time | 📜 |
| 13 | Zone lifecycle + NPC zone transitions | 🟡 (followers only) |
| 14 | AI combat intelligence (target select / threat assessment) | 📜 |

**Concrete goals shipped (24):** Bored, Command, Delegate,
DisposeOfCorpse, Dormant, Flee, FleeLocation, FollowLeader, GoFetch,
Guard, Kill, LayRune, MoveTo, MoveToExterior, MoveToInterior, NoFight,
Pet, Retreat, Step, Wait, WanderDuration, Wander, WanderRandomly.

**AI behavior parts shipped:** AIGuardPart (warden), AIWellVisitorPart
(farmer), AIAmbushPart, AILayRunePart, AIRetrieverPart (magpie). M1-M6
milestones in `Docs/QUD-PARITY.md` document the buildout.

Reference: `Docs/QUD-PARITY.md` §1-§14.

---

## Followers (F.1-F.3, 2026-05)

Multi-phase ship with audit-pass + Qud-parity-second-pass cadence.

- F.1 — Leader/Follower scaffolding + FollowLeaderGoal + faction hostility guard
- F.2 — Recruitment via `Persuasion_Recruit` activated ability + `RecruitedEffect`
- F.2.7 — Cross-zone follower transit
- F.3 — Slot system (`GetCompanionLimitEvent`) + `GrantsRepAsFollowerPart`
- F.3 audit pass 2 — added `Died` hook for follower cleanup (Qud-parity finding)

Reference: `Docs/FOLLOWERS.md`.

---

## Skills system (WSP1-8, ~50 concrete skill classes)

**Trees shipped (11):** Acrobatics, Axe, ShortBlades, LongBlades,
Cudgel, Pyromancy, Cryomancy, Galvanism, Corrosion, Spellcraft,
Persuasion.

**Powers per tree:**

| Tree | Concrete skill powers |
|------|----------------------|
| Acrobatics | Dodge, EvasiveRoll, Tumble, Vault |
| Axe | Expertise, Cleave, Berserk, Decapitate, Dismember, HookAndDrag, RendArmor, Whirlwind |
| Cudgel | Expertise, Backswing, Bludgeon, ChargingStrike, Conk, Disarm, GroundPound, Hammer, ShatteringBlows, Slam |
| LongBlades | Expertise, Lacerate, Lunge (stances ⏸) |
| ShortBlades | Expertise, Bloodletter, Disengage, Flurry, Hobble, Jab, Puncture, Rejoinder, Shank |
| Pyromancy | Charsplit, Cinder, HeartFlame |
| Cryomancy | BrittleStrike, FrostRetort, Frostbind, Hibernate |
| Galvanism | GroundStrike, Overload, ShockRetort |
| Corrosion | AcidRetort, Etch |
| Spellcraft | ArcaneSurge, Empower, LeyTap |
| Persuasion | Recruit, Dismiss |

**Wired:** Each skill is a `BaseSkillPart` with virtual overrides
(`OnAttackerAfterAttack`, `OnGetToHitModifier`, etc.). Player has a
skill screen (X key) and a hotbar; abilities route through
`SkillsPart.HandleEvent`. Diag-instrumented (`CommandRouted` /
`SkillRejected` / `CommandRejected`).

**Deferred:** Survival, Tinkering-as-skill-tree, Cooking, Discipline,
ranged-weapon trees, LongBlades stance batch (6 powers).

Reference: `Docs/SKILL-SYSTEM-PARITY.md`, `Docs/SKILL-TREE-QUD-PARITY.md`,
`Docs/AUTHORING-SKILLS.md`.

---

## Effects system (26 concrete)

Acidic, Berserk, Bleeding, Broken, Burning, Charred, Confused,
Electrified, Frozen, HearthAura, Hibernating, Hobbled, Hooked,
Paralyzed, Poisoned, Recruited, Rooted, ShatterArmor, Sitting,
Smoldering, Steam, Stoneskin, Stunned, Wet, Witnessed.

- Flat durations only — no Qud-style parameterized variants
  (`BurningIntensity(N)`)
- TYPE_NEGATIVE / TYPE_GENERAL flagging honest (WSP6.16 backfill)
- Save/load round-trip pinned (SL.6 audit)
- Effect chaining (Wet → Frozen, Fire → Smoke) ⏸

Reference: `Docs/qud-effects-analysis.md`,
`Docs/EFFECT-TICK-ON-APPLY-TURN.md`.

---

## Mutations (30+ concrete classes)

**Activated:** FlamingHands, FireBolt, IceShard, IceLance, FrostNova,
RimeNova, AcidSpray, PoisonSpit, ArcBolt, ChainLightning, Thunderclap,
PrismaticBeam, EmberVein, KindleFlame, ChillDraft, DryingBreeze,
ConjureWater, Conflagration, Hearthwarm, Quench, Calm, WardGleam.

**Passive:** Telepathy, Regeneration, UnstableGenome,
IrritableGenome, Esper, Chimera, ExtraArmPrototype.

**Infrastructure:** `MutationRegistry`, `MutationDefinition`,
`MutationCategoryDefinition`, `IRankedMutation`, ranked scaling,
mutation-granted equipment tracker, modifier tracker, source-type
(natural/granted/temporary).

Reference: `Docs/Plans/COO_MUTATION_SYSTEM_IMPLEMENTATION_PLAN.md`.

---

## Items / equipment / handling

- **Slots & grip:** Body / Hand / Head + `GripType` (OneHand /
  TwoHand) + `HandlingPart` / `HandlingService`
- **Categories:** Melee Weapons, Armor, Food, Tonics, Books, etc.
- **Stacker:** runtime item stacking
- **Containers:** `ContainerPart` (containers in zone + picker UI)
- **Locks/Keys:** `LockPart` + `KeyPart` (LockedDoorShowcase scenario)
- **Tonics:** healing, status (poison/fire/acid/lightning/frost/water/bleed/charred),
  Cure (Antidote/BurnSalve/Panacea)
- **Books:** examinable lore items (multiple in world-gen)
- **Item enhancements (E.1, 2026-05):** `IItemEnhancement` substrate,
  `EnhancementFactory` registry, slot-cap veto, `IMeleeEnhancement`
  filter, Apply/Remove lifecycle. **No concrete enhancements
  shipped yet** — substrate only.
- **OnHitEffects:** weapon-attached effect spec + factory
  (Flaming Sword, Ice Sword, Acidic Dagger, Cryolance, Thunder Hammer,
  Emberspear all wired)

Reference: `Docs/ITEM-ENHANCEMENTS.md`, `Docs/ON-HIT-EFFECTS.md`.

---

## Anatomy / Body

`Body` + `BodyPart` + `BodyPartCategory` + `BodyPartType` +
`Laterality` (left/right) + `NaturalWeaponFactory` +
`SeveredLimbFactory` + `SeveredLimbPart`. Dismemberment via
`Axe_Decapitate` / `Axe_Dismember` skills. Humanoid / quadruped /
insectoid templates.

---

## Materials / thermal sim

`MaterialPart`, `ThermalPart`, `FuelPart`, `LifespanPart`,
`MaterialSimSystem`, `MaterialReactionResolver`. Reaction blueprints
(JSON-loadable): `fire_plus_raw_meat` (cooking), `fire_plus_raw_starapple`,
`fire_plus_fungal`, `cold_plus_metal`, `lightning_plus_conductor`.

Reference: `Docs/qud-effects-analysis.md`, `Docs/Design/Mechanics/`.

---

## Tinkering

`BitLockerPart` (bit currency), `TinkerItemPart`,
`TinkerRecipe`/`TinkerRecipeRegistry`, `TinkerModificationRegistry`,
`TinkeringService`, mods directory. Player-facing UI ⚠ (debug-only
access for now).

Reference: `Docs/COO_TINKERING_IMPLEMENTATION_PLAN.md`,
`Docs/qud-tinkering-analysis.md`.

---

## Economy / trade / rentals

- `CommercePart` (item value), `TradeSystem` + `TradeUI` (NPC buy/sell)
- `RentalSystem` + `RentalPart` — rent weapons, return-veto, cooldown,
  cross-village blueprint matching. Two adversarial sweeps, **0 bugs
  found**, deep contract pinning.

Reference: `Docs/WEAPON-RENTAL-SYSTEM.md`, `Docs/SHOPPING-PARITY.md`.

---

## Conversations / NPCs

`ConversationManager`, `ConversationPart`, `ConversationActions`,
`ConversationPredicates`. 9 JSON files / 378 nodes total:

| File | Nodes |
|------|------:|
| FriendlyNPCs | 81 |
| Factions | 71 |
| RotChoir | 68 |
| HouseThresker | 47 |
| Palimpsest | 43 |
| Villagers | 38 |
| Wardens | 19 |
| Marceline_Quest | 6 |
| Quartermaster | 5 |

**NPC blueprints with conversation hooks:** Elder, Villager, Tinker,
Merchant, Warden, Farmer, Innkeeper, Scribe (+ faction-specific).
World-state predicates exist but are not yet driving branching in
shipped conversations.

---

## Settlements (village repair gameplay)

`SettlementManager` + `SettlementRuntime` + 3 site types:
`WellSitePart`, `OvenSitePart`, `LanternSitePart`. State machine:
Fouled → Purified → TemporarilyPurified, with repair stages, method
IDs, outcome tiers, problem types. `CampfirePart`, `SanctuaryPart`
support sites. Settlement visuals + definitions JSON-driven.

⚠ No quest hook — repair is a passive site mechanic; no NPC
dispatches the player on the task and no rep reward.

---

## House Drama / Narrative State

- `HouseDramaRuntime` + `HouseDramaPart` + `HouseDramaLoader` +
  `HouseDramaData` — persistent NPC relationships, grudges
- `NarrativeStatePart` + `KnowledgePart` + `FactBag` +
  `QualityRegistry` + `INarrativeReactor` — fact-tracking substrate
- 2 dramas shipped: HouseThresker (47 nodes), HouseVex
- Witnessed effect surfaces narrative state (scribes record events)

⚠ Loaded at bootstrap; no scenario player can complete to feel it.

Reference: `Docs/HOUSE-DRAMA-SYSTEM.md`,
`Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md`.

---

## Storylets / Quests

`StoryletPart` + `StoryletRegistry` + `StoryletData` + `QuestState`.
1 storylet declared: `IronKeyQuest.json`. Quest log UI state-builder
shipped (`QuestLogStateBuilder`).

⚠ No quest-giver NPC dispatches the IronKeyQuest in any scenario yet.

Reference: `Docs/QUEST-SYSTEM.md`,
`Docs/Plans/STORYLET_QUEST_LAYER_IMPLEMENTATION_PLAN.md`.

---

## Save / Load (SL.1-SL.10, 144 tests, audit closed)

`SaveSystem.cs` (1876 LOC). Full round-trip serialization via
reflection + `ISaveSerializable`. Auto-discovers `Part` subclasses.
Goal-stack restoration with custom fields. Static-factory re-wiring on
cold load. 60 contracts pinned in SL.10 audit closure. 0 bugs found in
adversarial sweep.

**Player UI:** `BootMenuController` (Continue / New Game on launch),
`PauseMenuController` (Save / Load via Tab), `DeathScreenController`
(Continue from autosave / Start over). F5/F6 hotkeys prepared
(`InputHandler.cs:80`) but ⚠ not wired.

Reference: `Docs/SAVE-LOAD-AUDIT.md`,
`Docs/SAVESYSTEM-DEEP-DIVE-AUDIT.md`.

---

## Trap / furniture / interaction

`PressurePlatePart`, `TripWirePart`, `TriggerOnStepPart`,
`LightSourceFlickerPart`, `ExaminablePart`, `TrapFurniturePart`.
Showcases for each in `Scenarios/Custom/`.

Reference: `Docs/TRAP-FURNITURE.md`.

---

## Presentation / UI

Renderers (compute snapshot, gate on fingerprint):

- `ZoneRenderer` (CP437 + per-cell dirty hooks)
- `SidebarRenderer` (HP/MP/LV/XP/AV/DV/WT/DR + 30-line message log +
  thoughts mode 't')
- `HotbarRenderer` (skill / ability bindings)
- `LookOverlayRenderer` ('L' key — examine + tile probe)
- `WorldCursorRenderer`, `AnimatedEnvironmentRenderer`,
  `EnvironmentSpriteRenderer`, `GlyphGhostRenderer`, `AsciiFxRenderer`
- `BiomePalette` / `BiomeColorPatcher` (per-biome color tweaks)

Modal UI:

- InventoryUI (I), SkillsScreenUI (X), AbilityManagerUI (M),
  DialogueUI, TradeUI, ContainerPickerUI, PickupUI,
  GrimoirePickerUI, WorldActionMenuUI (right-click), FactionUI

Input:

- `InputHandler` (WASD/arrows/numpad/vi keys, bump-attack)
- `SaveLoadInputController`, look-mode controller

Effects layer:

- `CrtToggleController`, `HitStopController`,
  `SpriteEnvToggleController`, `LightSourceSpriteHook`

---

## Performance / observability

- `Diag.cs` substrate + `DiagQuery` / `DiagCount` / `DiagAssert` /
  `DiagInspectRecord` MCP tools
- Every gate emits a category-appropriate diag record (skill, effect,
  damage, turn, furniture, trade, quest, ai)
- `ZoneRenderHooks.MarkCellDirty` for visible state changes
- Snapshot-fingerprint gating on renderers
- 70,000× perf bug surfaced + fixed (2026-04-28) via
  `ProfilerRecorder` over 60-90s windows

Reference: `Docs/PERF-FOUNDATION.md`, `Docs/AI-OBSERVABILITY.md`,
`Docs/D1-SPIKE-PLAN.md`, `Docs/D2-HOOKS-PLAN.md`,
`Docs/D3-TOOLS-PLAN.md`.

---

## Scenarios (65 — all developer-facing)

`Scripts/Scenarios/Custom/`. Categories:

- **Showcases:** ElementalSwords, FlamingSword, Cryolance,
  ThunderHammer, EmberSpear, AcidicDagger, CombatHooks, CombatParity,
  SkillTree, StatusEffectGlow, OnHitEffects, ThrowableTonics,
  TrapFurniture, TripWire, LockedDoor, MerchantShop, MagpieFetchesGold,
  PetDogFetchesBone, RuneCultistAmbush, Recruit, AnimatedEnvironment,
  TonicTestBench, RentalTestBench, AcidicDagger
- **AI demos:** ScribeSeeksShelter, ScribeWitnessesSnapjawKill,
  IgnoredScribe, WoundedScribeFleesToShrine, PacifiedWarden,
  CorneredWarden, CalmThenWitness, CalmTestSetup, WitnessLineOfSightWall,
  WitnessRadiusBoundary, WitnessStacksOnSecondDeath, MimicSurprise,
  SleepingTroll, VillageChildrenPetting, ElementalCreatureZoo,
  FiveSnapjawAmbush, SnapjawBurial, SnapjawRingAmbush, StoutSnapjaw,
  InspectAIGoals
- **Test benches:** EmptyStartingZone, QuestShowcase

**No shipped vertical-slice scenario** — none of the 65 form a
20-30 minute experience arc with goal → friction → resolution.

---

## Known gaps (engine vs game)

The honest tension flagged in `Docs/roadmap.md` is still live:

- **No vertical slice / win condition / progression curve** — `IMPLEMENTED`
  features compose into a sandbox, not a game.
- **Ranged combat absent** — melee-only.
- **NPCs cannot cross zones** (except followers via F.2.7).
- **Status effects render as sidebar text only** — no glyph indicators.
- **Tinkering accessible only via debug** — no in-game crafting UI.
- **House Drama / Narrative State runtime exists but no scenario
  surfaces it.**
- **IronKeyQuest storylet declared but no NPC dispatches it.**
- **F5/F6 quicksave prepared in code, not wired.**
- **No experience / leveling / character creation** — players spawn
  pre-built; can't spend skill points on entry.
- **`IMPLEMENTED.md` itself was 1+ year stale before this rewrite** —
  ground truth for engine state.

---

## Reference docs

| Path | Topic |
|------|-------|
| `Docs/QUD-PARITY.md` | Goal-stack parity tracker + Methodology Template (§5162+) |
| `Docs/COMBAT-QUD-PARITY-PORT.md` | 10-phase combat port |
| `Docs/SKILL-SYSTEM-PARITY.md` | Skill infrastructure parity |
| `Docs/SKILL-TREE-QUD-PARITY.md` | Skill content per tree |
| `Docs/FOLLOWERS.md` | F.1-F.3 follower system |
| `Docs/ITEM-ENHANCEMENTS.md` | E.1 weapon enchantment substrate |
| `Docs/SAVE-LOAD-AUDIT.md` | SL.1-SL.10 audit closure |
| `Docs/roadmap.md` | Strategic "what next" |
| `Docs/CONTENT-ROADMAP.md` | Content-shaped roadmap |
| `Docs/Status/IMPLEMENTED.md` | This file |
| `ADVERSARIAL_TESTING.md` (root) | Adversarial sweep playbook |
