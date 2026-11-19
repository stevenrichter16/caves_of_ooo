# MACHINE V1 DOSSIER: Thermal exchanger (machine 8)

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `9316f9330` as read. No project file, Unity run, test, build or git write was made. All C#/JSON below is a sketch for the implementing engineer. Lore authority is root `Lore/` only (`Docs/Lore/` unused).

Evidence tags: **[V]** I read the cited line in this session. **[R]** taken from `mw/` research or the architecture spec without re-reading. **[I]** inference or design. **[U]** unverified; the named RED test or first-cycle step settles it.

Inputs read: `mw/design/ARCHITECTURE-SPEC.md` (all), `Docs/MACHINE-ROSTER-VISION.md`, `Docs/MACHINE-SYSTEMS-DESIGN.md`, `mw/lore.md` (sections 0-3, M8, 5-11), `mw/settlement-*.md` thermal rows (all 24 files, by grep), `mw/arch.md`/`presentation.md`/`home.md` (thermal greps), and the shipped thermal code and content listed in section 2.

---

## 0. Verdict

1. **Classification: `wave3-needs-new-system`.** The machine body is technically wave2 (station contract, reader, placement, retention: spec S2, S4, S5, S7, S13b). It is wave3 as an *honest product* because the thermal exchanger has **no unmet job in the world at HEAD**: every consumer the pitch names is either absent from the game or already served free by an ordinary path (section 3). Something new has to exist first (section 4).
2. **It is not a candidate for "a machine in every town".** Of 20 authored places (17 villages + 3 catacomb villages), my read of the settlement dossiers and faction matrix is: 3 plausible only as a weak or home-bridge fit (Sill, Gantry, Sumphold), 2 weak (Marrowstye, Cinderhold), Wellmeet plausible-but-needs-a-purpose, and the rest poor, canon-forbidden or exempt (section 5.9). A town is covered by *any* machine, so the thermal exchanger should simply stay out of the first placement wave; this costs the census nothing.
3. **Smallest honest V1 (gated).** A stone *heat-stone hearth* that adds exactly one capability the world lacks, **directed warmth**: the player chooses which side of the fixture a bounded plume of heat leaves from, previews what it will do to that ground (ice melts, water film boils to steam, oil film ignites), and pays finite fuel for it. Everything else the fixture does (feed, light, dry gear, cook) is *already shipped* and is reused through a capability adapter, not rebuilt. V1 is deliberately modest: it is a machine-shaped skeleton with a real, previewable, finite consequence, and it **does not pass the purpose test on its own** (section 3). It ships only with an owner-approved first consumer (section 4 G1) or as the home/base bridge fixture.
4. **Most important correction to the inputs.** The heat-source half of the pitch is *already a shipped blueprint*: `SpreadCookingCoals` is a placed, non-takeable, fuel-fed (max 25), heat-holding (500 start, cooks at >=150) fixture whose Feed / Relight / Dry gear / Cook verbs already work (`Objects.json:2358`, `CampfireWorkActions.cs:8-37,48-95`, `CookingService.cs:12`) [V]. A stone-bodied copy of it adds nothing. V1 must be judged on the flue and the reader, never on "a place with fuel and heat".

---

## 1. The pitch, clause by clause

`Docs/MACHINE-ROSTER-VISION.md` item 8: "Capture heat from a vent or furnace, or draw cooling from an appropriate source. Use it for drying materials, supporting a culture, cooling machinery, or preparing a sheltered workspace. Redirecting heat creates a local consequence: the workroom becomes comfortable while the exhaust area becomes hazardous. A modest fuel-powered version keeps ordinary homes viable; unusual locations offer stronger opportunities."

The design doc row asks for "explicit source/receiver coupling, work limits and bounded local temperature effects" and "useful cooling/heating and a second consequence share existing thermal rules; unsupported sources do not create energy" (`MACHINE-SYSTEMS-DESIGN.md:92`).

| Pitch clause | What would have to be true | What exists at HEAD | Verdict |
|---|---|---|---|
| capture heat from a **vent** | a vent offers a quantity to capture | `SteamVent` is a Terrain with entity Temperature 95, `TileStateSource` water coating 2 turns + Heat 1 (`Objects.json:33997-34065`) [V]. Heat is the coarse tile channel 0..2 (`ZoneTileState.cs:42`) [V], and the `steam` reaction consumes the coating and 1 heat every turn (`Reactions.json` id `steam`) [V]. The entity's own Temperature 95 is under the scald threshold 100 (`SteamContact.cs:22`) [V] and decays toward 25 (`ThermalPart` ambient decay) [V]. | There is no capturable quantity. A vent is a status source, not a reservoir. |
| capture heat from a **furnace** | a furnace exists | The `TinkersForge` is a field-free `Forge {}` with no Thermal and no Fuel; Cinderhold's builder pins it that way (`Objects.json:5597-5610`; `CinderholdCompositionBuilder.cs:88`: `!HasPart<ThermalPart>()`) [V]. `Oven` is inert scenery (`Objects.json:27134`) [V]. The only fuel-bearing heat fixtures are `Campfire` and `SpreadCookingCoals` [V]. | No furnace. The only source is the finite fire. |
| draw **cooling** | a cold source with a stock | `FrostVent` (Temperature -25, `Cold 1`, radiates) appears only in the Underground hazard table at weight 15 (`HazardTerrainBuilder.cs:143-148`, `Objects.json:33807-33880`) [V]; carried `GlacierSalt` (cold:3) and `FrostLichen` (cold:2) are reagents with one-shot field verbs (`MaterialFieldActions.cs` verbs Cool/Freeze) [V]. | Cold is wild-only or a consumable. No settled cold stock. |
| **drying materials** | a material whose dried form matters | `WetEffect` on gear is dried free at any hot finite fire, one quarter per action (`CampfireWorkActions.cs:25,32,48-65`) [V]; `Hydromancy_DryingBreeze` removes it in radius 1 (`Hydromancy_DryingBreeze.cs`) [V]. No raw hide, green peat or wet fibre blueprint exists (name search of 767 blueprints) [V]. `DriedMeat` exists with no production path and no advantage over `CookedMeat` (both 3d4; cooked adds a Toughness meal bonus; `Objects.json:12068,24976`) [V]. | No dryable good with a purpose. |
| **supporting a culture** | a culture vat | Not built; machine 5 is parked (`MACHINE-SYSTEMS-DESIGN.md:93`) [V]. | Consumer absent. |
| **cooling machinery** | a machine with heat state that matters | Forge, still, rig, strainer carry no thermal or fuel state (forge pin above; `AlchemyStill {}` at `Objects.json` AlchemyStill) [V]. | Consumer absent. Making existing machines overheat would degrade ordinary convenience (spec R7). |
| **sheltered workspace / comfort** | an exposure rule | The only exposure rule is the Beating glare: `Parched` after 10 consecutive exposed Height turns, cancelled by an interior cell *or any head covering* (`BeatingGlareSystem.cs:29,57-58,82-88`) [V]. `ColdResistance` is a combat damage stat; there is no ambient cold exposure (grep: no cold-exposure effect) [V]. | A hat already solves the only exposure there is. A cooled room would be strictly worse than a hat. |
| exhaust area **hazardous** | hazard by shared rules | Tile heat reacts with coatings: `ignite_oil_heat` (occupant fire 8), `steam`, `melt_ice`, plus ember spread (`Reactions.json`; `TilePropagationSystem`) [V/R]. Tile heat never damages an occupant by itself. | Real, bounded, previewable. This is the one clause that rests on shipped rules. |
| **modest fuel-powered** version keeps **homes viable** | homes have a need | No home exists and no home need is defined (`home.md:122,240`: "no general heating-a-room model", resident needs open) [R]; ordinary rest is free at beds and campfires. | "Viable" has no meaning yet. Home design must define the need. |
| **unusual locations** stronger | stronger opportunity at a vent | Vents exist only in wild chunks (Cave, Jungle, Sodden, Grovelands, Ruins, Underground tables, `HazardTerrainBuilder.cs:77-148`) [V]; none in a town; installing there needs the home/base install verb (spec S19). | Deferred. |

Net: of eleven clauses, exactly one (directed hazard) rests on shipped rules, one (fuel-powered fixture) is already shipped, and nine have no consumer or are already served free.

---

## 2. Verified foundations to reuse (and their limits)

| Foundation | File:line [V unless noted] | Reuse in V1 | Limit |
|---|---|---|---|
| Finite cooking fire | `CampfirePart.cs` fields `FiniteCooking`, `AllowRest`; `CampfireWorkActions.cs:8-37` (menu), `:48-95` (act) | Feed (`+10` per `SalvagedTimber`, cap `MaxFuel`, `:80`), Relight (sets 450 from a carried flame, only below 450 with fuel, `:36,88`), Dry gear (needs >=150 and fuel>0, `:22,48-65`) | Type-keyed on `CampfirePart` (`Source()`), so only a campfire entity qualifies. |
| Cooking | `CookingService.cs:12` (threshold 150), `FindStation` scans `HasPart<CampfirePart>` | Cook at any qualifying source | Same type key. |
| Thermal/fuel parts | `ThermalPart.cs` (`ApplyHeat`, ambient decay, ignition crossing, `GroveLaw.OnIgnite` at `:164`), `FuelPart.cs` (`ConsumeFuel`) | State on the fixture | Thermal state does not advance while the zone is inactive (`QUEST-FREE-COOKING-WORKSPOT.md:38,89`: "no elapsed-away cooling") [V]. |
| Tile energy and reactions | `ZoneTileState.cs:42` (`MaxEnergy = 2`), `TileStateSourcePart.cs` (`Heat`, `Cold`, `RadiatesEnergy`, `Seed`), `ZoneTileStateSystem.cs:32-47,210` (`OnPlayerTurnEnd`, `AddHeat`), `Reactions.json` | A bounded plume writes heat through a `TileStateSourcePart` on a short-lived entity; consequences follow the shipped reactions | Heat is a coarse status, not joules; no occupant effect except through a reaction. |
| Short-lived entities | `LifespanPart.cs` (counts `TurnsRemaining` on `EndTurn`, removes host) driven by `MaterialSimSystem.TickMaterialEntities` (`MaterialSimSystem.cs:26-60`) | The plume is exactly this pattern (steam cloud, ash drift) | Counts only active-zone turns; no away credit (honest limit, matches policy). |
| Entity-to-neighbour heat primitive | `MaterialSimSystem.EmitHeatToAdjacent` (`MaterialSimSystem.cs:122-155`) | Not used in V1; noted so the next reader does not claim "no heat exchange exists" (`arch.md:127` is slightly wrong: the primitive exists, but only burning/steam effects call it) | No machine-controlled transfer exists. |
| Hazard scatter | `HazardTerrainBuilder.cs:77-148` | Source of wild vents (deferred use) | Wild only. |
| Village oven repair site | `SettlementSiteDefinitions.cs:20-60` (`HeatStone`, `HeatFailure`), `OvenSitePart.cs`, `PlaceConditions.json` (`RebuiltByHand.VillageOven`: price 0.98, Farmer stock 3 `ToastedEmberwheat`) | The only shipped *town* heat consequence; candidate retrofit target | Tracked only in the starting village and Wellmeet; the `Oven` blueprint is inert elsewhere. |
| Spec contracts | `ARCHITECTURE-SPEC.md` C2 marker/retention, C4 declared `ActionTime` and disabled rows, C5 station kinds, C6 reader, C3 placement | Body, reader, placement, retention | See section 7 for gaps. |

---

## 3. Purpose test (principle 1)

"Every proposed machine needs a specific resource, problem, alternative solution, cost, and observable outcome. Do not degrade ordinary movement, crafting or recovery to manufacture demand." (`MACHINE-SYSTEMS-DESIGN.md:21`)

| Candidate job | Ordinary path that already exists (stays free) | Honest residual value of an exchanger | Result |
|---|---|---|---|
| Dry wet gear | Hot finite fire: one quarter per action, no fuel spent (`CampfireWorkActions.cs:48-65`); Drying Breeze rite; wading is itself a free fire shield (`WetEffect` text) | Fewer actions per stack | **Fails**: convenience over convenience. Not a reason to exist. |
| Cook | Campfire/coals/hearth, whole stack in one action (`CookingService.cs`) | None | **Fails.** |
| Comfort / shelter (Beating) | Any head covering; interior cells (`BeatingGlareSystem.cs:57-58`); meal `ToastedEmberwheat` +20 HeatResistance 100 turns; `ColdwardCloak` +50 ColdResistance (`Objects.json:6758,1547`) | None | **Fails.** |
| Dried meat / cold chain | Cooking already outclasses drying; no spoilage system (`EnhancementPaleSalt.cs:29-31` states food spoilage "doesn't exist in CoO yet") | None | **Fails.** A cold-room has no consumer. |
| Directed ground heat | Kindle verbs and spells already ignite oil and thaw ice at will (`MaterialFieldActions.cs`, `ZoneTileStateSystem.ApplyFireToTile`) | A *standing, fuel-priced, aimable* source that persists a few turns and can be set before an encounter | **Passes weakly**: a real, previewable consequence; no unmet need. |
| Fuel for fires | `SalvagedTimber` is lootable, stocked and harvestable (`Objects.json` yields; `LootTables.json:39,3261`) but is also tier-1 repair material (`Tier1Repairs.json:6,26,56,76`) | Peat or turf as a regional alternative fuel would relieve that competition | **Real but small, and needs new content** (see C1). |

### Candidate first consumers (the only things that would make V1 matter)

| ID | Candidate | Canon and world fit | New things it needs | Risks |
|---|---|---|---|---|
| C1 | **Turf for fuel**: cut peat, dry it on a rack by the stone, burn it as a second fuel | "Cut, stack, dry, burn" is Sumphold's authored pitch (`Sumphold.json` per `settlement-sumphold.md:163`); peat is canon fuel; peat-cutters are canon (`Lore/History/02_Geography.md:69`) | `PeatBank` harvest (it yields nothing today and its 37 faces are pinned), items `CutTurf`/`DriedTurf`, feed rows as data (spec S18: feeds as data with identical constants), conversion rows with `ToUnits <= FromUnits` and declared loss (S17) | Bog-Taken adjacency (canon refusal 4.3.1: nothing may treat bodies as feedstock); regional recovery bonus requires two banks left unburnt (`RegionalSituations.cs:49` [R]); fire near boards and peat (flame temps 300-320) |
| C2 | **Heat input for the fractionating still** (machine 2) | Strongest functional fit: the still needs "fuel/work" (`MACHINE-SYSTEMS-DESIGN.md:89`) | The still V1 itself | Zero standalone value; do not build the exchanger before the still defines its own heat need. Prefer to fold the exchanger into the still's design until a second consumer arrives (spec R1). |
| C3 | **Warmth for a culture vat** (machine 5) | Catacomb idiom | Parked machine | Choir/patch landmines (L2) |
| C4 | **Home hearth** (the pitch's "ordinary homes") | Bridge to base design | A defined home need | No such need exists; inventing one is manufacturing demand |
| C5 | **Hot work at the forge** (recovered iron refined hot) | Natural: `ForgeCold` exists *because* hot work does not (`MaterialProcesses.json` kind `ForgeCold`) | Hot-process rows, ore/refining qualification (open in `MACHINE-SYSTEMS-DESIGN.md:56`) | Pinned forge shape; belongs to machine 1 |

**Recommendation:** do not schedule the exchanger as a standalone machine. If an owner wants thermal content soonest, qualify **C2** (inside the still) or **C1** (regional fuel) *first*; ship this dossier's V1 only as the second consumer of the heat-source capability. If the owner wants a thermal object for towns anyway, see the retrofit option in section 5.9.

---

## 4. What must exist first (gates)

| Gate | Needed | Owner | Why |
|---|---|---|---|
| G1 | One owner-approved **first consumer** from section 3 (C1 or C2 recommended) with its own qualification evidence | owner | Without it V1 manufactures demand (principle 1) |
| G2 | Lift the "no new runtime changes" hold in `CLAUDE.md`; S10/S12 critique gates unaffected by V1 | owner | Standing hold |
| G3 | Spec slices S1 (save golden), S2 (marker/retention), S3 (declared time, disabled rows), S4 (station contract), S5 (reader), S7 (placement) | engineering | The body needs them; V1 adds no saved Part until S1 exists |
| G4 | Behaviour-preserving **heat-source adapter** over the campfire (slice T1 below) | engineering | Otherwise the stone cannot be a cooking/drying source without copying `CampfirePart` |
| G5 | Canon voice pass (L17/R10) for every authored string; no "exchanger" in speech | voice owner | `lore.md` M8 |
| G6 | Home-need definition (only if V1 is to serve the home bridge) | home design | "Viable" is undefined |

Stored-work canon (`lore.md` section 11 item 1; spec S18) is **not** a gate for V1: V1 banks nothing. It becomes a gate for any banked-heat version.

---

## 5. V1 definition (gated by G1-G5)

### 5.1 Name and voice

- Engine id (draft): `HeatStoneHearth`. Do not reuse `HeatStone` (already `RepairableSiteType.HeatStone`).
- In-world vocabulary (drafts for the blind voice gate, from `lore.md` section 8): "heat-stone", "drying rack", "flue", "dry-room". Concord at a post may say "the hearth is by the crate"; surface villagers say "heat-stone". Never "exchanger", "thermal", "power" or "cell" in speech or UI (L16).
- Pale Curation and Recension vocabulary ("dry-room", "mineral chamber") is reserved for a possible later cold/dry variant; V1 uses the surface villager idiom only.

### 5.2 The fixture

One immobile stone stub. Parts (new Parts in namespace `CavesOfOoo.Core` only, spec C1 hygiene):

| Part | Notes |
|---|---|
| `RenderPart`, `PhysicsPart` (solid, non-takeable), `MaterialPart` (stone, Combustibility 0), `ExaminablePart` | The fixture cannot burn or ignite neighbours itself. |
| `CampfirePart { FiniteCooking = true, AllowRest = false }`, `ThermalPart`, `FuelPart { MaxFuel = 25, FuelMass = 0 }` | Mirrors `SpreadCookingCoals` constants (Thermal 500/25/300/.8/.02, fuel 25) [V `Objects.json:2358` region] so every shipped finite-cooking rule applies unchanged. **FuelMass starts 0**: placement mints no fuel. Thermal starts at ambient (cold) so nothing is pre-heated. |
| `HearthFluePart` (new) | Fields: `int FlueSide` (0..3 cardinal, default 0), `int FlueTurns = 20`, `int FlueFuelCost = 4` (design numbers, see 5.5). **No coordinates, no zone ids** (spec C1 hygiene). The outlet cell is resolved at use time from `FlueSide`. |
| `InstallationMarkerPart` (`RetainZone = true`) | `FuelPart` is state-bearing, so spec E07 requires `Retention: Zone`; the entity enters the S2 retention predicate (needed in relapse-capable villages). |
| `IStationCapability` on `HearthFluePart` with `Kinds = {"heat-hearth"}`, `Reach = 1`, `Readiness = Strict` | A new kind token. Do not reuse `cold-work` (that token means fire-free *metal* work, `ForgeCold`) and do not name a kind `cold` (collides with the cold-side idea). |
| `IDescribeState` on `HearthFluePart` | Reader lines: lit/unlit and fuel `n/25`, flue side, what the open flue would do (section 5.4). |

Plume entity (new blueprint `HearthPlume`, runtime-spawned, never placed by the registry): `RenderPart` (glyph/colour only), `PhysicsPart` non-solid non-takeable, `LifespanPart { TurnsRemaining = FlueTurns }`, `TileStateSourcePart { Heat = 1, RadiatesEnergy = true }`, no `ThermalPart`. Exactly the shipped pattern of steam clouds and ash drifts (`LifespanPart.cs` header) [V].

Facing: `HearthFluePart` records the chosen side as an integer, so a moved fixture (home bridge, S19) never carries a stale coordinate (spec section 9 hook 4).

### 5.3 Player verbs

All rows come from `GetInventoryActions`. With spec S3 landed each declares its `ActionTime`; without S3 they would silently fall through the two OR-chains (spec section 1 item 5), so **S3 is a hard prerequisite**.

| Verb | Source | Inputs | Output / effect | Cost and time | Preview shown on the row and in the reader |
|---|---|---|---|---|---|
| Feed | reused (`CampfireWorkActions`, `+10`, cap `MaxFuel`) | 1 carried `SalvagedTimber` | `FuelMass += 10` up to 25 | one action; 1 timber | "feed coals (1 salvaged timber, up to 10 fuel)" (existing text) |
| Light | reused (`:36,88`) | a carried flame source | Temperature set to 450 if below and fuel > 0 | one action; no item spent | "relight coals from flame" (existing text); disabled with reason when no fuel or no flame |
| Dry gear | reused (`DryGear|` family, `:32,48-65`) | a carried wet item | quarter moisture removed, no fuel | one action | existing text |
| Cook | reused (`CookingService`) | a carried raw stack | cooked stack | one action | existing text |
| **Turn the flue** | new | none | `FlueSide` cycles N/E/S/W, skipping a blocked side | **free** (`ActionTime.Free`) | "turn the flue to the east: outlet is bare grass" or the blocked reason |
| **Open the flue** | new | `FlueFuelCost` fuel (4) and a lit hearth (Temperature >= 150) | spawns one `HearthPlume` on the outlet cell for `FlueTurns` active turns; the plume heats the outlet and its four orthogonal neighbours (heat 1, the existing radiate rule) | **one action** (`ActionTime.OneAction`); 4 of the hearth's fuel; no item spent | "open the flue east: uses 4 of 25 fuel, warms the bay 20 turns. Here: oil film will ignite; water film will boil to steam; sheet ice will melt." (one clause per coating present; "nothing here would change" when none) |

The new rows are the only new gameplay. Everything else is the shipped finite fire reached through the capability adapter (T1).

### 5.4 Preview, outputs, loss and waste

- **Outputs are world state, not items.** V1 creates no goods, no liquid and no currency. This keeps it clear of the economy (EC2 single ledger), of Reagent/Liquid minting (L19) and of any unit-conversion table.
- **Loss destination:** fuel burned is consumed (no refund on expiry); the plume's warmth dissipates with the plume ("the warmth leaves through the flue"); no residue, soot or ash in V1.
- **Honest limit shown in the reader:** "Heat here is a coarse status for a few turns, not a measure of energy" (`ZoneTileState.cs` header [V]); the 4-fuel/20-turn price is a game-rule price, not physics.
- **Consequence preview** is computed by a pure read of the outlet five cells: `TileState.CoatingTurns(x,y,id)`, `TileState.Heat/Cold`, and whether each cell is dry bare ground, using the same reaction table the world uses (`Reactions.json`). It lists the first applicable reaction per cell; it never fires an event. (Spec gap G-S5: no shared "preview a world consequence" contract exists; V1 builds it inside the Part like the strainer does.)
- **Not previewed, stated in the reader:** ignition of *entities* (cloth, brush) is not caused by tile heat; only by oil film or embers spreading (`TilePropagationSystem`). The reader says so rather than promising either safety or hazard.

### 5.5 Numbers (design values, not derived)

| Value | Proposal | Basis |
|---|---|---|
| Max fuel | 25 | `SpreadCookingCoals` [V] |
| Feed | +10 per timber | `CampfireWorkActions.cs:80` [V] |
| Light | set to 450 | `:88` [V] |
| Hot threshold | 150 | `CookingService.cs:12` [V] |
| Flue price | 4 fuel | **design number**; balance pass required |
| Flue duration | 20 active turns | **design number**; balance pass required |
| Plume heat | 1 (radiates to four neighbours) | `TileStateSourcePart` radiate rule, `MaxEnergy 2` [V] |

### 5.6 Blocked, failed, interrupted and saved states

| State | Behaviour | Code / diag |
|---|---|---|
| Not lit (Temperature < 150) | Open-flue row **disabled** with reason "The stone is cold. Light it first." Dry/Cook rows keep their shipped rules. | spec S3 `AddDisabled`; code `cold`; diag `event/ActionDisabled` |
| Fuel short (< 4) | Disabled: "There is not enough fuel to hold the flue open." | `fuel-short` |
| Outlet blocked (wall, solid, door, stairs, deep water, creature) | `Turn the flue` skips it; if all four blocked, row disabled "No side is open." | `flue-blocked` |
| Actor not adjacent / stale zone / station swapped mid-commit | Refused through `StationReceipt.Begin` re-proof; nothing spent | `StationRefused` diag (S4) |
| Interrupted (callback, rollback, death) | The whole open-flue step is one `InventoryTransaction`: fuel deduction and plume spawn each register an undo (`tx.Do(apply, undo)` pattern, `CampfireWorkActions.cs:76-80`); on failure the fuel is restored and the plume removed | precedent `CampfireWorkActions.TryAct` |
| Saved while plume exists | Plume saves as an ordinary entity with its `LifespanPart.TurnsRemaining`; hearth saves fuel and temperature. **Away time is not credited**; a plume in an unloaded zone is frozen, not expired (same policy as finite cooking heat) | spec C9 policy |
| Hearth in a relapse-capable village | `RetainZone` keeps the zone (spec S2) so fuel and heat are not regenerated away | S2 predicate |
| Old saves / cached villages | No hearth appears (placement is fresh-only, spec C3); the new Parts are absent in old saves and load as nothing (spec R3) | S1 golden |
| Second plume while one exists | Refused: "The flue is already open." One plume per hearth; no stacking, no extension, no refresh exploit | `plume-active` |

### 5.7 Alternatives the player keeps (at least two genuinely different)

1. **Leave it alone.** Nothing in the game requires the hearth; repair, healing, carrying and descriptions are unchanged. No quest, route or door depends on it.
2. **The ordinary campfire and coals** keep Dry gear, Cook, Feed, Relight and (campfire) Rest exactly as today; the hearth is not better at any of them.
3. **Magic and carried reagents for the same ground effects:** `Pyromancy` and `Kindle*` ignite oil; `Cryomancy`/`GlacierSalt` freeze; `EmberFruit` warms a Frozen target; `Hydromancy_DryingBreeze` dries creatures (`MaterialFieldActions.cs`, skills) [V].
4. **Gear and food for environments:** head covering for glare, `ToastedEmberwheat` (+20 HeatResistance), `ColdwardCloak` (+50 ColdResistance).
5. **Different ground:** take another route, wade, or use interiors; the flue is one way to change an outlet cell, never the only way.

### 5.8 Consequence outside its own menu (cross-system, verified)

1. **Tile reactions** at the outlet follow the shipped table: oil + heat -> `ignite_oil_heat` (occupant fire damage 8, embers, smoke); water + heat -> `steam` cloud; ice + heat -> `melt_ice`; heat and cold annihilate each other on the same tile (`ZoneTileState.cs:316`) [V/R]. A player can deliberately prepare an oiled doorway, or accidentally boil a puddle they meant to cross.
2. **Fire law.** In Grovelands surface and the level-2 floor, player-sourced ignition costs -40 Rot Choir reputation (`ThermalPart.cs:164` -> `GroveLaw.OnIgnite`; `settlement-deepest-cathedral.md:117` [R]). A plume that ignites oil there could dodge the charge if its source is not the player. **Decision:** V1 is never placed in Choir ground (census exemption) and the open-flue verb records the actor as the cause for diag; the dodge is pinned by a RED test, not assumed away ([U], T3 test `FlueIgnitionOnGroveSurfaceIsAttributedToTheActor`).
3. **Finite timber economy.** `SalvagedTimber` is also tier-1 repair material (`Tier1Repairs.json:6,26,56,76`) and the reclamation output (`MaterialProcesses.json` `reclaim`); every timber fed to the hearth is not available for repairs. This is a real, honest competition, not an artificial tax.
4. **Settlement state.** In relapse-capable villages (Sill, Wellmeet) a fuelled hearth forces zone retention (spec S2). Consequence disclosed: a retained village no longer re-rolls build-time stock after relapse.
5. **Encounters.** An aimed plume can pre-set a hazard for a fight (oil + ember) or clear ice from a path: relocating a supported hazard source alters an encounter by the same rules everywhere (`MACHINE-SYSTEMS-DESIGN.md` open-world interactions). No scripted trigger.

### 5.9 Placement, operators and census fit

**Where it can honestly go (all need G1):**

| Place | Fit | Evidence | V1 note |
|---|---|---|---|
| Sill (starting village) | plausible | tracked oven site, Farmer/Provisioner economy, existing forge and still (`settlement-sill.md:150`) | Better as a **retrofit** (below), not a second fixture |
| Gantry | plausible only as home appliance | little public demand; cloth walls flame 260 (`settlement-gantry.md:182,201`) | Keep >= 3 cells from `TentWall`; likely better held for home bridge |
| Sumphold | plausible | drying racks and peat in the authored pitch, but no dryable item and a real fire hazard near boards and peat (`settlement-sumphold.md:163`) | Natural home of C1 |
| Wellmeet | plausible, needs purpose | Tent-Right tolerates; heat exhaust beside cloth is the hazard (`settlement-wellmeet.md:97`) | Never priced; gift idiom only (L3) |
| Marrowstye, Cinderhold | weak | sacred climate / no consumer (`settlement-marrowstye.md:270`, `settlement-cinderhold.md:220`) | Do not place; Cinderhold forge shape is pinned thermal-free |
| Poor: First Tent, Tally, Last Counter, Slip, Posy, Quillhold, Salt-Vault, Drowned Ledger, Spread Wayhouse, Stillleaf, merchant camps, Morrowfast | poor | each dossier row cites an existing free path or hazard | No thermal row |
| Forbidden or exempt for fire/thermal: Olderdeep, Lampwell, Spivenor, Quiet's Door, Deepest Cathedral, Ginmere | canon | patch/plume/Choir law (`lore.md` L2, L4; dossiers) | Exempt with source in `Installations.json` |

**Smallest honest town-level thermal change is not this machine.** The shipped village oven repair already pays out price and stock only (`PlaceConditions.json` `RebuiltByHand.VillageOven`). Making the *repaired* oven of Sill/Wellmeet a working heat source (a readiness guard on the site stage, the existing finite-fire verbs through the capability adapter) is a **retrofit of existing components**, gives a shipped quest a physical payoff, creates no new machine kind and no new consumer, and avoids the fresh-only placement limit for Sill (a tracked, stage-linked fixture). The Morrowfast dossier reaches the same conclusion (`settlement-morrowfast.md:128`: "a retrofit of existing components, not a machine"). [I] Recommended to the owner as the town answer; not part of V1.

**Operator roles (all shipped, none requires a new role)** [R `lore.md` section 11 item 6]: Tinker (fits and explains the flue), Provisioner (drying), Innkeeper (comfort talk), Farmer (Sill oven bread), CurationSorter (dry-room talk at a Curation place). V1 ships `Open` (no `ServiceTermsPart`); at a Concord place a `Fee` idiom and at Tent-Right a `Gift` idiom wait for spec S10/S11 and must pass the voice gate. The shipped forge and still stay Open.

**Census:** thermal rows are **not** added in the first wave; no census class changes. `Pending` for thermal is not needed because every town is satisfied by another machine kind (spec section 10 item 3).

### 5.10 Canon landmines this must avoid (`lore.md` ids)

| Id | Constraint in V1 |
|---|---|
| L1 | Never a source, tap or reader for the **tenth fire** (`Overworld.2.19.0`) or any authored scene: the verb refuses in zones in `AuthoredWildernessZoneIDs` (`OverworldZoneManager.cs:771`), and install specs exempt them |
| L2 / R6 | No heat from Urqu, Sari-Pyrite, the Founding Plume or any `HearthPatch`; fixture refuses within 5 cells of a patch (Lampwell dossier R2) and is exempt in Olderdeep, Lampwell, Spivenor, Quiet's Door |
| L3 / R7 | Never priced or metered in a Tent-Right place; water is never a feed; the oath and guest-cloth are untouched; at Wellmeet any terms are `Gift` only |
| L4 | No fire fixture in Choir ground; plume ignition attributed to the actor (5.8.2) |
| L5 | Never place in or alter a preservation chamber; a cooled room at the Salt-Vault or Marrowstye intake would be "allowing decay" tampering |
| L9, L10 | No Glow-Quartz, no "charge", no stored charms: V1 stores nothing (no banked heat) |
| L11 | No gauge of Urqu bleed or closure state; the reader reports only fuel, lit state and outlet consequences |
| L15, L16, L17 | No "free" in Concord strings; no technology nouns; every authored string needs the blind voice gate (no shipped machine string has a recorded pass) |
| L19 | Output is never a liquid or reagent, so nothing can approach the deep-binding liquids |

### 5.11 Deferred pitch behaviours and why

| Deferred | Why | Trigger |
|---|---|---|
| Capture heat from a natural **vent** | No capturable quantity (section 1); vents are wild only; needs the player install verb (spec S19) and a vent anchor model | After S19 and a decision on how a status source is metered |
| **Cold** source / cold-room | `FrostVent` is underground only; no spoilage or cold-chain consumer; Pale-Salt "keeps the inventory fresh" has no mechanic behind it (`EnhancementPaleSalt.cs:29`) | A spoilage or curing system, owner-approved |
| **Banked heat** (store heat in the stone) | Needs the stored-work vocabulary (canon decision, spec C10/S18) and a consumer; thermal state is not credited while away, so a bank would only hold, not run | S18 plus G1 |
| Drying **materials** with dried forms | No dryable good exists; needs C1 or a new regional raw feed | G1 |
| Supporting a **culture** | Machine 5 parked | Machine 5 V1 |
| **Cooling machinery** | No machine has heat state; adding one to the shipped forge/still would degrade ordinary convenience | A new machine that needs it |
| **Comfort** / home viability | No exposure or home-need rule | Home design defines the need |
| Entity-ignition from the plume (cloth/brush) | Would require an `ApplyHeat` emitter; tile heat does not do this; changes hazard balance | Owner request, own RED |
| Terms (Fee/Gift/Withheld) | Spec S10 gated on critique | S10/S11 |
| NPC use | Spec S20 | S20 |

### 5.12 Tier

`wave3-needs-new-system` for the reasons in section 0. Body-only pieces map to spec waves as: marker/retention S2 (wave1-able), declared time/disabled rows S3 and station contract S4 and reader S5 (wave2 shared contracts), placement S7 (wave2), heat-source adapter T1 and plume T3 (new, but small and behaviour-preserving), first consumer G1 (new system).

---

## 6. Slices and RED-first tests

Cadence: one isolated-clone EditMode cycle is about 5 minutes. Each slice is RED-first (a compile error counts), carries a counter-check per positive assertion, an adversarial file, diag pins and a cold-eye pass (CLAUDE.md). Test names are prefixed `MachineThermal`. All slices wait on G2.

### T0. Decision slice (no code)
Owner picks G1, confirms the retrofit option and the exemption list. Output: a one-page decision record.

### T1. Heat-source capability adapter (behaviour-preserving; 2 cycles; depends on S4, S5)
**Goal:** the shipped finite fire's verbs accept any entity that exposes the capability, not only a `CampfirePart`. No saved field added or changed.
**Sketch:** `IHeatSource { bool FiniteCooking; bool AllowRest; }` implemented by `CampfirePart`; `CampfireWorkActions.Source/Fuel/Hot`, `CookingService.FindStation` and the temperature notification read the capability.
**RED first:**
- `HeatSourceAdapter_CampfireTruthTable_MatchesLegacyOracle` (copies of `Source`, `Fuel`, `Hot`, `FindStation` as oracles; at least 30 layouts: lit/cold, fuel 0/partial/full, takeable, creature, replaced Part, other zone)
- `HeatSourceAdapter_FeedRelightDryCook_ForCampfire_UnchangedBytes`
- `NonCampfireHeatSource_CanCook` (RED today: a test Part exposing the capability; counter: an entity named like a campfire with no capability is not a source)
- `NonCampfireHeatSource_WithoutFuelPart_Refuses`
- `AllowRestFalse_NeverOffersRest_ForAnyHeatSource`
- `FiniteCookingFalse_LegacyAuthoredStations_KeepTheirEstablishedPolicy`
- Existing `QuestFreeCooking*`, `CookingService*`, `CampfireWorkActions*` suites pass untouched.
**Adversarial:** two heat sources adjacent (resolution order identical to legacy); source removed or replaced between preview and commit (callback test); NaN or infinite temperature or fuel; capability on a creature; capability on a takeable item.
**Risk:** medium (shared cooking code). **Honesty:** `CampfireWorkActions` is `internal static`; the adapter must not widen its visibility beyond the test assembly.

### T2. Heat-stone fixture, station kind, reader, S7 row (2 cycles + play; depends on S2, S3, S4, S5, S6, S7, T1)
**RED first:**
- `HeatStoneHearth_BlueprintPassesInstallationAdmission` (non-creature, non-terrain, non-takeable, no `SpatialFootprint`)
- `HeatStoneHearth_StartsUnfuelledAndCold` (counter: a copy of `SpreadCookingCoals` starts hot and fuelled; the hearth must not)
- `HeatStoneHearth_FuelBearing_RequiresRetentionZone` (spec E07 positive and counter)
- `HeatStoneHearth_ThermalAndFuelConstantsMatchCoals` (pins 25/150/450/+10)
- `HeatStoneHearth_ReaderShowsLitFuelAndFlueSide_InBothReaders` (quick and document)
- `HeatStoneHearth_StationKindIsHeatHearthNotColdWork` (counter: a `cold-work` process does not resolve at the hearth; the hearth does not satisfy `ForgeCold`)
- `HeatStoneHearth_Hygiene_NoPositionalFieldsOrZoneIds` (spec S1 lint)
- `HeatStoneHearth_RoundTripsPublicFields_AndOldSaveLoadsWithout` (S1 golden addition)
- placement matrix from S7 (determinism, rng untouched, route preservation, flood connectivity, cached zone unchanged) run against **one** test row only; **no production row is added** until G1.
**Acceptance:** native green; actual play on a test seed: place, Look, `x`, feed, light, dry, cook, F5, fresh Continue; cached control shows none.

### T3. Flue and plume (2 cycles; depends on T2, S3)
**RED first:**
- `TurnFlue_IsFreeAndCyclesThroughOpenSides`; `TurnFlue_SkipsBlockedSides`; `TurnFlue_AllBlocked_DisabledWithReason`
- `OpenFlue_SpendsExactlyFourFuelOnce_AndSpawnsExactlyOnePlume`
- `OpenFlue_ChargesOneTurn_NotMore` (S3 `OneAction`; cost-equivalence golden for neighbours)
- `OpenFlue_Unlit_DisabledNamingCause` (counter: lit passes); `OpenFlue_FuelShort_DisabledNamingCause`
- `OpenFlue_SecondPlume_Refused_NoStackingNoRefresh`
- `Plume_WritesHeatToOutletAndFourNeighbours_Only` (counter: diagonals and farther cells untouched)
- `Plume_ExpiresAfterTwentyActiveTurns_AndNotOnAwayTime` (leave zone, return: remaining turns unchanged)
- `Plume_OilFilmAtOutlet_Ignites_ByShippedReaction_NotBySpecialCode` (capability proof: the plume contains no reaction logic)
- `Plume_WaterFilm_BoilsToSteam`; `Plume_SheetIce_Melts` (counter: dry bare ground changes nothing)
- `Preview_ListsFirstReactionPerCell_AndDoesNotFireEvents` (read-only; event and diag counts unchanged)
- `OpenFlue_RollbackRestoresFuelAndRemovesPlume` (callback throws mid-payment; identical to `FeedCookingFire` rollback pattern)
- `OpenFlue_RefusesInAuthoredScenes_AndOnHearthPatchAdjacency` (L1/L2)
- `FlueIgnitionOnGroveSurfaceIsAttributedToTheActor` (L4; [U] decides whether the shipped fire-law gate applies)
- `Plume_SaveRoundTrip_KeepsTurnsRemaining`
- diag pins: `furniture/FlueOpened { zone, side, fuelBefore, fuelAfter }`, `furniture/StationRefused` with codes `cold`, `fuel-short`, `flue-blocked`, `plume-active`.
**Adversarial taxonomy:** state atomicity, rollback, save/load reach, stacking, boundary inputs (null actor, fuel exactly 4, fuel 3.999), cross-actor (two actors, one hearth), anti-exploit (reopen to refresh, feed-open-feed loop to generate heat from nothing: assert conserved fuel, no free heat), diag dispatch.
**Acceptance:** actual play on a fresh seed with an oil slick and a puddle at two outlets (two different outcomes), F5, fresh Continue; plume present after reload with the saved remaining turns.

### T4. First consumer (owner-selected; size depends on G1)
For C1: `PeatBank` harvest slice, `CutTurf`/`DriedTurf`, feed rows as data, a conversion with declared loss, bog-body adjacency refusal. For C2: folded into the still's own V1. Not specified further here because the choice is open.

### T5. Placement rows and census (1 cycle; data)
Only after T4: add rows for the chosen places; run the S7/S8 control matrix; keep exemptions for L1-L5 places.

Total new test cases for T1-T3 as listed: about 45 named, before adversarial files.

---

## 7. Spec gaps (where this machine needs something the architecture spec lacks)

| Gap | Detail | Proposal |
|---|---|---|
| G-S1 | The spec's adapter table (section 6) has **no row for `CampfirePart`, `CampfireWorkActions` or `CookingService`**, yet they are the shipped heat-source/consumer seam and are type-keyed. | Add a T1-style adapter row; capability, not type |
| G-S2 | No shared notion of a **bounded environmental effect source** that a verb spawns and that expires. The plume reuses `LifespanPart` + `TileStateSourcePart`, which is enough, but nothing documents the pattern as a contract. | Document as a recipe; no new contract until a second consumer (pump outlet, dynamo spark) |
| G-S3 | `TileStateSourcePart` cannot be gated by another Part (no "only while lit"). | Avoided by spawning the plume entity; an additive gating field is possible later (default empty keeps old behaviour) |
| G-S4 | C6 (`IDescribeState`) describes **state**, not a **preview of a world consequence** at chosen cells. | Build the outlet preview inside `HearthFluePart.Describe` and the row label (strainer precedent); extract only on a second consumer |
| G-S5 | Station kind vocabulary: `cold-work` means fire-free metalwork. A thermal cold source would collide by name. | Reserve kind tokens `heat-hearth` and `cold-store` (the latter unused until a consumer exists) |
| G-S6 | No contract for **who attributes** an effect source's consequences (fire law, reputation). | A RED test pins attribution per slice; no new contract |
| G-S7 | Spec S18 stored-work vocabulary is unresolved (owner decision 6); V1 avoids it by storing nothing. | Keep V1 non-banking until the owner decides |
| G-S8 | Spec C3 E14 forbids Terrain blueprints for placed installations; the plume is a runtime-spawned non-installation, so it is outside the registry, but the validator test must not scan it. | Exclude spawned-effect blueprints from `Validate` by a name list pinned by a test |

---

## 8. Owner decisions

1. Approve a first consumer (G1) or defer the exchanger until machine 2 or a regional fuel qualifies.
2. Accept the retrofit of the repaired village oven as the town-level thermal answer (section 5.9).
3. Confirm the exemption list and the no-fire-in-Choir-ground rule for this kind.
4. Confirm the design numbers (4 fuel, 20 turns) are placeholders for a balance pass.
5. Decide the first slot for the blind voice gate for every string in this dossier.

---

## 9. Verification record

**Read this session [V]:** `ARCHITECTURE-SPEC.md` (all); `MACHINE-ROSTER-VISION.md`; `MACHINE-SYSTEMS-DESIGN.md` (all); `lore.md` sections 0-3, M8, 5-11; `ThermalPart.cs` (1-200, plus the fire-law call at `:164`); `FuelPart.cs`; `MaterialSimSystem.cs` (1-60, 100-155); `HearthAuraEffect.cs`; `TileStateSourcePart.cs`; `ZoneTileState.cs` (1-140 and grep); `ZoneTileStateSystem.cs` (all); `Reactions.json` (all rows); `COLD-TILE-BRIDGE.md` (head); `CampfirePart.cs`; `CampfireWorkActions.cs` (all); `CookingService.cs` (1-120); `MaterialFieldActions.cs` (150-300); `Hydromancy_DryingBreeze.cs`; `WetEffect.cs`; `SteamEffect.cs`; `SteamContact.cs`; `BeatingGlareSystem.cs`; `ParchedEffect.cs`; `LifespanPart.cs`; `OvenSitePart.cs` (head); `SettlementSiteDefinitions.cs` (30-110); `SettlementManager.cs` (relapse block); `PlaceConsequences.cs` (head); `PlaceConditions.json` (oven rows); `HazardTerrainBuilder.cs` (60-160); `CinderholdCompositionBuilder.cs:84-94`; `MaterialProcesses.json`; `EnhancementPaleSalt.cs` (22-36); `QUEST-FREE-COOKING-WORKSPOT.md` (grep); blueprints `SteamVent`, `FrostVent`, `AshBed`, `Oven`, `TinkersForge`, `AlchemyStill`, `SpreadCookingCoals`, `ColdwardCloak`, `RawMeat`, `DriedMeat`, `CookedMeat`, `ToastedEmberwheat`, `Emberwheat`, `GlacierSalt`, `FrostLichen`, `EmberFruit`, `CuredLeatherOffcut`, `PaleSalt`, `SaltCuredBody`; name search of all 767 blueprints; `Lore/History/02_Geography.md:69` by grep.

**Taken from research [R] (not re-read):** settlement dossier ratings (read by grep, per-row), `Sumphold.json` pitch text, `RegionalSituations.cs:49`, `GroveLaw` details beyond the `ThermalPart.cs:164` call, `TilePropagationSystem` ember spread, `VillagePopulationBuilder` oven placement, `home.md` open questions.

**Unverified [U], settled by the named RED test:** whether the hearth's *Light* (relight sets Temperature to 450 directly, `CampfireWorkActions.cs:88`) passes through `TryIgnite` and the fire law; that every non-creature `LifespanPart` owner receives `EndTurn` each player turn when zone-active (the shipped steam cloud proves it for that blueprint, `MaterialSimSystem.cs:26-60`); whether tests pin the `Oven` blueprint shape (affects the retrofit option); the exact `IgnitionSources.Find` contract; `JsonUtility` row behaviour for the S6 registry.

**Corrections to earlier reports found while reading:**
1. `arch.md:127` says there is no heat exchange between two owners. `MaterialSimSystem.EmitHeatToAdjacent` exists (`MaterialSimSystem.cs:122-155`) but is only reached from burning/steam effects; no machine-controlled transfer exists. The conclusion stands.
2. The pitch and `lore.md` M8 call vents "fair game" for a source. Natural `SteamVent` entities are below the scald threshold (95 versus 100) and carry only the coarse tile status, so capture has no quantity to work with (section 1).
3. The architecture spec's adapter table omits the campfire and cooking seam although it is the shipped heat-source and drying path (G-S1).
4. `SpreadCookingCoals` already supplies the "fuel-powered heat fixture" half of the pitch; any dossier or slice that treats a fuelled hearth as new content is duplicating it.

No test, build, Unity run or git write was performed.
