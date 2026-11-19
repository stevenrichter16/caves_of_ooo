# Machine V1 dossier: Survey resonator ("sounding-rod")

Status: design only. Repo `/Users/steven/caves-of-ooo`, HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. Everything below is a proposal for the owner and the implementing engineer.

Evidence tags: **[V]** I read the cited line in this session. **[R]** taken from `mw/*.md` or `design/ARCHITECTURE-SPEC.md`, not re-read. **[I]** inference or design. **[U]** unverified; a named RED/pin test settles it.

Authority: `Docs/MACHINE-ROSTER-VISION.md:77-81` (the pitch), `Docs/MACHINE-SYSTEMS-DESIGN.md:94` (matrix row), `mw/lore.md` section 4 M10 (root `Lore/` only; `Docs/Lore/` never used), `design/ARCHITECTURE-SPEC.md`.

---

## 0. Verdict

**Tier: `wave1-existing-foundations`, with a purpose gate in front of it, and a loud scope disclosure.**

1. **What V1 is.** A carried item, the *sounding-rod*. One action, driven into the stone of an ordinary deep cave floor and struck. It answers with at most three coarse leads about ground the player has **not yet seen** on that floor: one open hollow, one bore (a way up or down), one dense seam. Bearing in eight points, distance in three bands, no counts, no coordinates. It hears terrain only: no creatures, no items, no containers, no names. The strike is **loud** (`Noise.Mark`), which is the real cost: eyeless sound-hunters (`PaleStalker`) perceive the player. It consumes nothing. Tinkers sell it. It needs no installation, no station contract, no service terms, no permission, no new world content.
2. **Why wave1.** Every piece it stands on is shipped: the pure underground-column predicate (`UndergroundLayoutPlan.IsOrdinaryColumn`), the zone grid and `Cell.Explored`, the `MineralVein` tag, the stair Parts, the `Noise`/`HuntsBySoundPart` model, `TinkerStock`. It needs only spec slices S1 (saved-Part golden, for its one new Part), and benefits from S3 (declared action time, disabled rows) and S5 (reader contract); both have a stated fallback.
3. **The honest purpose statement.** The pitch (`ROSTER-VISION.md:77-81`) promises a lead to "a metallic structure to the northeast" or "a buried mechanism with a recognizable signature". **No such discoverable exists in this world.** A source search for secret/concealed/buried/hidden objects finds none (only unrelated comments) [V]. The only things the world genuinely hides are (a) geometry beyond line of sight, (b) stairs beyond line of sight, (c) the seams inside the `MineGallery` stamp, and (d) four things canon says must stay unmapped (sinkhole mouths, the Sealed Library, the Helmwood water passage, the Root). V1 serves (a)-(c) only. Its purpose is **real but narrow: underground way-finding, retreat-finding and one rare resource lead**, paid for in noise. Whether blind exploration of ordinary caves is actually costly enough to justify it is **unmeasured**; section 9 (Q0) is a pre-registered measurement. If Q0 fails, do not build V1; build the missing content layer first (section 8).
4. **What it must not be sold as.** It is not "the survey resonator" of the pitch, and it must not be counted toward "a machine in every town" (section 7.3). The pitch's discovery purpose is `wave3-needs-new-system` (a buried-feature layer that does not exist). V1 is the honest first instrument that such a layer would later give targets to.
5. **Hold.** `CLAUDE.md` says "No new runtime changes". V1 does not touch weapon-payload, equipped-input or permission policy (the named critique gates), and its save impact is additive, but implementing it still needs the owner to lift the hold.

---

## 1. What is shipped toward it (verified)

There is **no resonator code**. A search of `Assets/Scripts`, `Assets/Resources/Content` and `Docs` for resonat/sounding/plumb-bell/ground-ear/geolog/seismic finds only the magic `ResonanceSystem`, audio docs and the roster text [V].

| Foundation | Fact | Evidence |
|---|---|---|
| Pure ordinary-column predicate | `UndergroundLayoutPlan.Select(seed, zoneID, biome, poi, factory)` "creates no entities and never consumes the zone, population, combat or loot RNG streams"; `IsOrdinaryColumn = valid && depth>0 && poi==null && !authored`; recomputing it later gives the pipeline's decision, so it works on cached zones | `UndergroundLayoutPlan.cs:5-9,14,22-47` [V] |
| Ordinary underground pipeline | solid earth + strata (or ruins rooms) + connectivity + up/down stairs + climb-outs (ordinary only) + route reservation + stamps + hazards + population + containers | `OverworldZoneManager.cs:604-640` [V] |
| A way back is guaranteed | `CaveClimbOutStairsBuilder` adds an up-stair in each horizontal third that lacks one; `UndergroundRouteReservationBuilder` reserves a BFS tree from the stairs to all four edges | `CaveClimbOutStairsBuilder.cs:6-43`, `UndergroundRouteReservationBuilder.cs:6-47` [V] |
| Fog is line of sight | `FieldOfView.Compute` marks `IsVisible`+`Explored` for LOS cells within `FovRadius`, default 999 | `FieldOfView.cs:17-90`, `ZoneRenderer.cs:47,1070` [V] |
| Terrain-only passability | `Cell.IsPassable() = !IsSolid()`; `IsSolid` tests the `Solid` tag, closed doors and closed archive barriers. `Creature` carries no `Solid` tag, so creatures never change passability | `Cell.cs:99-108,236`; `Objects.json` `Creature` tags [V] |
| Stairs | `StairsUp`/`StairsDown` blueprints inherit `Terrain`, carry `StairsUpPart`/`StairsDownPart` | `Objects.json` [V] |
| Seams | `MineGallery` stamp: 5x4 walled room, `GlowQuartzVein` x2 and a `DeepSupplyT2` chest, opening on one side, `Chance = 20`, `MinTier = 1`, in `UndergroundStamps`; veins carry the tag `MineralVein` and `Harvestable` | `LandmarkBuilder.cs:225-247`; `Objects.json:30209-30275` [V] |
| Noise model | `Noise.Mark(actor)` stamps `NoiseTick`; `Noise.Recent(actor, window)`; `HuntsBySoundPart.TryPerceive` perceives only recent noise "through darkness and around corners". `RigRelocation` already marks noise | `Noise.cs:13-27`, `HuntsBySoundPart.cs:18-22`, `RigRelocation.cs:125` [V] |
| The hunter | `PaleStalker`: eyeless, `HuntsBySound` (`Window` 1), `Brain.SightRadius` 10, "stand still and let it decide you are a stone". Joins underground tables at tier >= 3 (alone) and tier >= 4 (packs) | `Objects.json:19350-19440`; `PopulationTable.cs:1237,1244,1252` [V] |
| Depth to tier | tier = max(depth/3+1, zoneTier), zoneTier = surfaceTier + 1 + (depth-1)/3 | `PopulationTable.cs:1190-1195`, `ZoneTierPolicy.cs:24-28` [V] |
| Terrain varies by depth | sandstone <=2, limestone 3-4, shale 5-6, slate 7-8, quartzite 9-10, obsidian 11+ (walls and floors) | `SolidEarthBuilder.cs:42-50` [V] |
| Cave entrances | 50% of surface zones place a visible `StairsDown` | `CaveEntranceBuilder.cs:33` [V] |
| Found, not shown | a sinkhole renders as plain biome on the world map until its chunk is visited; the design doc says "Overgrown mouths are found, not shown" | `WorldMapZoneBuilder.cs:203-212`; `FELLING-WORLD-DESIGN.md:691` [V] |
| Vendor | `Tinker`: `Trader`, `StockTable=TinkerStock`, `Drams=90`; `TinkerStock` already lists `IronSpikeComponent` (100), `OakHaftComponent` (40), `LeatherBindingComponent` (35); placed in 70% of shared-pipeline villages | `Objects.json:13445,13536-13545`; `LootTables.json:2729-2750`; `VillagePopulationBuilder.cs:133-135` [V] |
| Item verb pattern | an item Part adds rows on `GetInventoryActions` via `InventoryActionList.AddAction(name,label,command,key,priority)`, then runs the command inside an `InventoryTransaction` on `InventoryAction` | `ContainedLiquidSourcePart.cs:142-175` [V] |
| Compass vocabulary | north = y-1; the project's direction tables spell north/northeast/east/southeast | `LiquidVesselService.cs:55`, `CordSnareActions.cs:18` [V] |
| Scenery | `SurveyStake` blueprint: non-solid, non-takeable, "numbering starts somewhere out of sight". The `SurveyLine` PlaceSite is an abstract `Structure` site with **no physical entity** | `Objects.json:36155`; `PlaceSites.json:86-112`; `placement.md:165` [V]/[R] |
| Operators | `StillleafSearcher` (RecensionScribe, **not** a trader), `CurationSorter` (**not** a trader), `SaltMaster`, `TentRightHost`, `Tinker` (trader) | `Objects.json` blueprint scan [V] |

### Corrections to the inputs

| # | Premise | Verdict |
|---|---|---|
| C1 | `lore.md` M10 treats the `SurveyStake`/`SurveyLine` site as a shipped survey foundation | **Weak.** `SurveyLine` is an abstract repair site with no entity; `SurveyStake` is scenery. Neither senses anything. They are naming precedent, not code. |
| C2 | The roster says a resonator reads "a large cavity below" | **Not available as a V1 read.** The only pure "below" read is `UndergroundLayoutPlan` roomed-vs-natural, which is `shallow` (always natural) for depth < 3 and whose purpose (what roomed floors give) is unmeasured. Deferred (section 8). |
| C3 | "Recomputing `IsOrdinaryColumn` equals the pipeline's decision" | **False for one zone.** The Gleaner's Cellar is `Overworld.11.10.1` (depth 1, `GleanersCellarBuilder.cs:15`), built by a bespoke pipeline (`OverworldZoneManager.cs:~157`), is not in `AuthoredWildernessZoneIDs` (`:771-772`), and the plan would call it ordinary. V1 eligibility must add it explicitly (R0 pin). `SoddenDistrict` and `MultiCellPilot` are [U]. |
| C4 | The architecture spec's census wants "a machine in every town" | **Does not apply to a carried instrument.** V1 needs no installation (section 7.3). |
| C5 | `lore.md` suggests Tent-Right water-finding | **No hidden-water world content exists** except the Helmwood passage, which is Ginmere-protected (`HelmwoodPassages.cs:1-12`, lore L1, matrix row Ginmere). Not offered. |

---

## 2. What can honestly be heard today

Allow-list, not deny-list: the instrument is **silent about anything not named here**.

| Class | Source in the zone | Why it is safe |
|---|---|---|
| Hollow | connected unexplored cells with `IsPassable()` | terrain only; a closed door reads solid |
| Bore | unexplored cell holding `StairsUpPart` or `StairsDownPart` | the instrument cannot tell up from down; that ambiguity is the leftover exploration, and "the nearest way off this floor" is also a retreat lead |
| Seam | unexplored cell holding an entity with tag `MineralVein` | the class never names the mineral (lore R8/L9: Glow-Quartz is not a battery; Choir-Iron is a faction sign) |

Things the world hides that V1 must **not** reveal (each is a refusal path, section 3.6):

- Sinkhole mouths on the world map and Stillleaf's sealed library: canon "never marked" (`FELLING-WORLD-DESIGN.md:658-662`, `02_Recension.md:177,278` per lore.md). V1 never reads another chunk and never reads the world map.
- Sinkhole floors, the Root column, lair stacks, villages: all have `poi != null`, so `IsOrdinaryColumn` is false by construction (`UndergroundLayoutPlan.cs:14`).
- The Helmwood water passage (`WaterPassagePart`) and the Gin-Frog "door detector": Ginmere is a sinkhole column; also not in the allow-list.
- Creatures and anything alive (`ROSTER-VISION`, lore R1/L1: no live hidden actors).
- Containers and their contents; reliquaries; plaques; bone; bodies; inscriptions (lore L5/L6/L8; none is on the allow-list).

---

## 3. V1 definition

### 3.1 Form and findability

- **Item** `SoundingRod`: Takeable, carried in the pack (held or stowed, not wielded). New Part `SoundingRodPart` (namespace `CavesOfOoo.Core`). Not a station, not installed, no `InstallationMarkerPart`.
- **Where a player finds it.** One data row in `TinkerStock` (`LootTables.json:2729`). Tinkers exist in ~70% of the 16 shared-pipeline villages (`VillagePopulationBuilder.cs:133-135`), so it is findable in roughly 11 of 16 towns in a fresh world, never in a town without a Tinker, never in catacomb, Choir or First-Tent pipelines [I: the First Tent shares the village pipeline and could roll a Tinker; see section 7.2].
- **Price.** Set in RED against the `Commerce` value of `IronSpikeComponent` plus `OakHaftComponent` (the plausible parts of an ash-and-iron rod); tunable [U].
- **Old worlds.** A Tinker already generated and cached has no rod; an unvisited town gets it on first visit; a rod bought later works in any zone, cached or not (it reads the live grid). Stated limit, not a defect.

### 3.2 Player verbs

| Verb | Where | Cost | Result |
|---|---|---|---|
| **Sound the ground** (command `SoundGround`) | pack menu on a carried rod (V1: pack only; a dropped rod offers only pickup, which removes any origin or "installed" ambiguity) | **one action** (`ActionTime.OneAction` via S3; fallback: add the command to both OR-chains, `InputHandler.cs:~3459-3471` and `InventoryUI.cs:~1511-1523`, with a charging-parity test) | up to three lead lines and one limit line in the message log; same text stored on the rod |
| **Read the rod** (`x` / F1) | anywhere | free | what it is, what it hears, **live preview of ground and reach at the current position**, the last reading with its age |
| Sell, drop, carry | ordinary item | ordinary | nothing special |

Player-only. Followers and NPCs cannot sound (S20 later).

### 3.3 Sensing model (all deterministic, no RNG)

Inputs: the live `Zone`, the player's cell as origin, `reach` from the ground table, the unexplored mask.

**Ground table** (code table with a pin test; extract to data on a second consumer, per spec R1). Keyed by the **floor blueprint of the cell under the player**:

| Floor | Reach (paces, Chebyshev) | Rationale [I] |
|---|---|---|
| `SandstoneFloor` (depth <=2) | 12 | soft stone drinks the pulse |
| `LimestoneFloor` (3-4) | 18 | |
| `ShaleFloor` (5-6) | 14 | layered, scatters |
| `SlateFloor` (7-8) | 20 | |
| `QuartziteFloor` (9-10) | 26 | dense, carries |
| `ObsidianFloor` (11+) | 30 | |
| any other floor token | **refuse** (`ground-uncoupled`) | loose or made-up ground |

These numbers are proposals, tunable, not canon. The terrain dependence the pitch asks for is real and verified (the depth table is shipped), not invented. A guard test requires that every floor token `SolidEarthBuilder.GetMaterialsForDepth` can return has a row, so a future depth band cannot silently refuse.

**Reading.** Consider only cells with `!Explored`, Chebyshev distance from origin in `[4, reach]`, inside the zone (it never crosses the zone edge). Bearing = one of eight points from the origin to the lead's **nearest cell**. Distance band from that same cell: **close** (<= 8 paces), **middling** (9-16), **far** (> 16, only possible when reach > 16).

| Class | Rule | Report |
|---|---|---|
| Hollow | 8-connected components of unexplored passable cells; choose the largest by cell count; size word: below 30 cells is **not reported**; 30-119 "a chamber"; >= 120 "a hall" | at most one |
| Bore | nearest unexplored `StairsUpPart`/`StairsDownPart` cell | at most one |
| Seam | nearest unexplored `MineralVein`-tagged cell | at most one |

Thresholds (4 paces, 30, 120) are proposals [I]. Output is at most **3 lead lines + 1 limit line**, ordered bore, seam, hollow. No coordinates, no counts, no percentages, no mineral names. Triangulating from two strikes is supported and intended: each reading stays coarse, the player's own movement narrows it.

The mandatory limit line states the reach in this stone and that silence is not proof of absence ("heard to N paces in this stone").

**Fog rule.** Only unexplored cells are reported, so the rod never re-describes what the player has seen and never becomes a minimap of the known floor.

### 3.4 Inputs, outputs, loss, cost

| Item | Value |
|---|---|
| Material inputs | **none** consumed. The finite things are: one action turn, one noisy tick, carry weight, the purchase price |
| Outputs | up to 3 lead lines (units: 8-point bearing, 3 bands in paces, 2 size words) + 1 limit line; stored in `SoundingRodPart` fields; **no world object is created or changed** |
| Loss / waste | no material waste. The only loss is informational: the rod keeps **one** reading; a new strike overwrites it. Messages scroll. Stated plainly in the reader |
| Cost | 1 action; `Noise.Mark(actor)`; price at purchase; no fuel, fee, wear or terms in V1 |
| Conservation | a sounding changes no entity, position, HP, tile state or inventory; the only writes are the actor's `NoiseTick` property and the rod's record fields (RED test snapshots the zone) |

Why no consumable: wear or a spent tip would be a new maintenance cycle on an optional tool and a new demand for iron created to justify the machine, which `MACHINE-SYSTEMS-DESIGN.md:19,136` forbids. A later optional wear/repair using the existing `RepairablePart` bridge (S22) and `IronSpikeComponent` (a real consumer for reclamation output, already stocked by Tinkers) is listed as V1.1, not V1.

### 3.5 Preview (what the reader and menu show before committing)

Shown, live: the ground under the player and its reach in paces ("limestone: carries about 18 paces") or the refusal reason; "reads only what you have not seen"; "hears stone, not breath" [draft]; "striking is loud; hunters that listen will notice"; "one action". **Not shown:** any part of the result. A test pins that the preview text for two different zones with the same floor is identical (no oracle).

### 3.6 States

| State | Code | Behaviour | Player text (drafts; voice gate pending) |
|---|---|---|---|
| surface or any non-underground zone | `surface` | refused; no turn, no noise | "Open ground gives the rod nothing to hear." |
| underground but not an ordinary column: sinkhole floor, Root, lair stack, village, authored zone, Gleaner's Cellar `Overworld.11.10.1` | `not-ordinary` | refused; no turn, no noise | **identical text** to `ground-uncoupled` so the refusal does not tell the player a floor is special |
| floor token not in the table, liquid underfoot, standing on stairs/door | `ground-uncoupled` | refused | "The rod gets no clean answer from this ground." |
| hostile creature in view | `hostile-near` | refused | "Not with that watching." |
| hauling or being dragged | `hauling` | refused | |
| rod broken or destroyed | `broken` | refused | |
| not the player | `not-player` | refused | |
| context stale (zone changed between menu and commit) | `stale` | refused | |
| success, leads found | | 1 action, `Noise.Mark`, record written, message | |
| success, nothing unseen in reach | `nothing-heard` | **still costs the action and the noise** (that is the price of not knowing); record overwritten with the empty reading | "Nothing new answers inside N paces." |
| interrupted | n/a | one-action command; no partial state. The reading is computed before any write; if it throws, nothing is written (`read-failed`, diag only) | |
| saved | | `LastReading`, `LastTick`, `LastDepth`, `LastReach` round-trip by public field; an old save without a rod is untouched | |

Every refusal records a diag event (`event/GroundSoundingRefused { code }`) and spends nothing.

### 3.7 Persistence

`SoundingRodPart` public fields: `string LastReading`, `int LastTick`, `int LastDepth`, `int LastReach`. No field named `ZoneID`, no station coordinates (spec C1 hygiene, so a later planted post is not blocked). The reader labels the record "as struck at turn N", because bearings are relative to where the player stood and the floor may since have changed. To anchor the bearings the record also stores "struck K paces <direction> of the nearest stair" computed from the zone at strike time (text only). Whether the HUD shows coordinates is [U]; the stair anchor avoids needing it.

---

## 4. Alternatives the player keeps (genuinely different)

All verified to exist; none is degraded to create demand.

1. **Walk it with light.** A torch (fuel-bearing, readable per M1d0; `Warden` stock `Torch` chance 100, `LootTables.json:~2765`) shows line-of-sight geometry; costs fuel; draws **sight** hunters. The rod trades that for **sound** hunters. Different risk, different information (light shows detail near you; the rod hears coarse shape through rock and around corners).
2. **Follow the guaranteed routes.** Every ordinary floor reserves a path from the stairs to all four edges and always has at least one up-stair per third (`UndergroundRouteReservationBuilder`, `CaveClimbOutStairsBuilder`). Walking edge-ward never traps a player.
3. **Be quiet.** The Wardens' counsel is the shipped counterplay: stand still and let the hunter decide you are a stone. A player who judges stalkers likely simply does not strike.
4. **Leave it alone / never descend.** No content is gated by the rod or by depth. A rod is optional; ordinary exploration, surface content, repair, healing and carrying are untouched.
5. **Triangulate or sound once.** One strike at arrival for a bearing, or two strikes from different spots for a crossing. The player chooses how much noise to spend.

Genuinely different consequences, not menu labels: different information, different exposure (sight vs sound), different spend (fuel vs a noisy tick), different route logic.

---

## 5. Consequences outside its own menu

1. **Noise to hunters (verified).** Success and `nothing-heard` call `Noise.Mark(actor)` exactly once. `HuntsBySoundPart` creatures perceive a recently-noisy actor within the brain's sight radius (PaleStalker: 10) "through darkness and around corners" (`HuntsBySoundPart.cs:10-22`). Pale stalkers join underground tables from tier 3, in packs from tier 4 (`PopulationTable.cs:1237-1252`). So the rod is most useful exactly where it is most dangerous. Controls in tests: unmarked, beyond 10 paces, and a sight-based creature (no effect beyond ordinary).
2. **Route, light and fuel.** The bore and seam leads change where the player walks, which changes encounters, torch burn and whether a retreat exists. Measured in Q0 and actual play (turns and fuel to reach the way down, blind vs with the rod, same seed and floor).
3. **Economy.** A Tinker (purse 90 drams) is paid through the existing ledger (`Docs/ECONOMY-SYSTEM.md` EC2); no dram sink or source is added.
4. **A seam lead is a real finite resource.** It points at a `MineGallery` (vein and a supply chest). The rod does not mention the chest.

---

## 6. Canon

### 6.1 Name and voice

- In-world item: **sounding-rod** (Recension Searcher idiom, `lore.md:192`; Surface/Tinker column offers "plumb-bell", `lore.md:318`). Verb: "sound the ground". Draft reader line (Recension register, `lore.md:193`): "It answers hollow or solid. It does not answer which hollow. Enter that beside the reading." Every string above is a **draft** that needs an intent card and the fresh-context blind gate (lore R10, L17); none is shippable on this document's authority.
- Avoid in player text: resonator, pulse, signal, return, calibration (L16, technology register); "listening post" (Listening Tradition, catacomb; `catacomb_village_design.md:208`); anything that mimics villager flash-sonar (`:508`).

### 6.2 Operators

- **Tinker**: vendor only. The rod is an item sale, not a machine service, so the Tent-Right barter rule (`Tender`) applies to the sale like any good, and no dram-priced machine service exists at Wellmeet.
- **Explainers, not operators, and not V1:** `StillleafSearcher` (the natural voice for the honest-limits line; shipped, not a trader), `CurationSorter` (Drowned Ledger stakes; **bodies excluded**), `SaltMaster`/`TentRightHost` (water-finder's rod idiom; no water content exists). A conversation line from any of them is a voice-gated later addition.
- **Warden**: sells the torch; the complementary light.

### 6.3 Landmines it must avoid (ids from `lore.md` section 7 and rules section 1)

| Id | How V1 avoids it |
|---|---|
| L1, R2 | reads no sinkhole mouth, Sealed Library, Root, tenth fire, woven doll, Gin Frogs, layers; refuses every `poi != null` column and the authored list |
| L2, R1, R6 | uses no Urqu, Sari-Pyrite or plume source; attributes no intent; never reports the *sari* sound |
| L3, R7 | not a Tent-Right machine service; no price on water, no guest-right automation |
| L4 | no burning, digging or sealing; the strike is read-only |
| L5 | reads no body, specimen or bog subject (not on the allow-list) |
| L6, R3, R4 | reads no inscription or under-text; no under-text detection; not underreading |
| L8 | no plaque, niche, bone or catacomb map; every catacomb column is `poi != null` |
| L9, R8 | the seam class never names quartz, iron or salt; never "battery" |
| L10, R5 | stores no charge; no mana-like bar |
| L11 | reads no Urqu gradient and forecasts nothing; gives no bleed map |
| L16 | no technology nouns in player text |
| L17 | all strings pass the blind gate before they ship |
| Forbidden-vocabulary test | no output or reader line contains: quartz, iron, salt, bone, plaque, niche, body, root, urqu, sari, charge, battery, mana, glow |

---

## 7. Fit with the architecture spec

### 7.1 Contracts used and where the spec lacks something

| Need | Spec contract | Status |
|---|---|---|
| New Part is saved safely | S1 golden + hygiene lint | **Required** (trivial; the Part enters as an addition) |
| Declared one-action cost, disabled row with a reason | S3 `ActionTime`, `AddDisabled` | **Preferred.** Fallback: two literal chain additions and a parity test |
| Reader line (live preview, last reading) | S5 `IDescribeState` | **Preferred.** Fallback: one line in each of `ExaminablePart.BuildDescription` and `ExamineDocumentBuilder.Build` |
| Placement, marker, retention | S2, S6, S7 | **Not needed** (item, not a placed installation) |
| Station query/receipt, service terms, process engine, jobs | S4, S9-S18 | **Not needed** |

Where the spec lacks what this machine needs (stated, not hidden):

1. **No instrument-limits contract.** `lore.md` section 10 proposes `ProtectedSubject`/`InertScene`/`InstrumentLimits`; the spec has none. V1 implements eligibility as a pure resonator-local function (`SoundingEligibility`: surface test, `IsOrdinaryColumn` recompute, explicit exclusion of the Gleaner's Cellar and the authored list) plus allow-list sensing. Extract to a shared contract only when the assay cabinet or relay is the second consumer (spec R1).
2. **`ActionTime` has no rounds.** V1 is one action by design. A longer listen (longer reach in exchange for staying still) would be the second consumer of the C4 rounds trigger.
3. **No notes or leads store.** Nothing persists "a lead on this floor" beyond the rod's last reading; `SpreadDiscoveryReports` is a hard-coded family list (`presentation.md:148`). A real journal of leads is a prerequisite for the pitch's "lead worth investigating" at map scale.
4. **No carried-tool contract.** `IStationCapability` is for stations; carried tools use bespoke receipts (`MaterialCuttingWorkReceipt`). V1 spends nothing, so it needs none; wear (V1.1) would.

### 7.2 Census and every-town program

- Per `lore.md` section 6 the resonator fits Wellmeet (water-finding, no content), the Drowned Ledger (stakes; bodies landmine), Stillleaf (lead only; location excluded), and Concord posts (route surveying). It is forbidden or inert at the Quiet's Door, Olderdeep, Lampwell, Spivenor, Ginmere, the Cathedral, the First Tent.
- **Recommendation: classify the resonator as vended, not installed, and do not count the Tinker's stock row toward "a machine in every town".** A row in a stock table is not a place to use a machine; counting it would be census padding. In the spec's coverage classes it contributes nothing; it should appear in the census doc as `Not an installation (carried instrument)`.
- The First Tent: the shared pipeline can roll a Tinker there; the lore is "no machine installed" (`07_TentRight.md:119`). A rod in a Tinker's pack is an item, not an installation, but the owner should confirm [U].

### 7.3 Home/base hooks (nothing here blocks them)

- The reader is `GroundSounding.Read(Zone, originX, originY, reach, ...)`, origin and reach are **parameters**, so a later planted rod (spec S19 `InstallablePart`, `InstallationMarkerPart{Origin="Player"}`) calls the same reader with its own cell. No zone id or coordinate is stored on the Part.
- Eligibility is a pure function of the zone, so a player-planted "post" can never be placed on a sinkhole, Root, catacomb or authored column: the base design inherits the protection for free.
- The "second base: underground listening post" is a **later** installed form; name it "sounding post" or similar, not "listening post".
- A planted rod would be a *kind* of tool/station for S4's contracts (`IStationCapability` kind `survey`), added only then.

---

## 8. Deferred pitch behaviours (and why)

| Pitch behaviour | Deferred to | Why / what must exist first |
|---|---|---|
| "a metallic structure to the northeast", "a buried mechanism with a recognizable signature" | V2+, **`wave3-needs-new-system`** | No buried or concealed finite feature exists. Needs a content layer: authored buried sites (e.g. reclaimable machinery for the forge program) with coarse signatures, per-feature protection flags, and generation hooks that respect the cached-zone rule. Also needs metal identity without the material-collapse landmine (L9) |
| Survey of other chunks / map-scale leads | V2+ | Sinkhole mouths are "found, not shown"; the Sealed Library is "never marked"; catacomb maps are guarded knowledge (`catacomb_village_design.md:327`). Needs canon decisions and a leads store (7.1 gap 3) before any cross-chunk read |
| "a large cavity **below**" | V1.1, after measurement | Pure and cheap (`UndergroundLayoutPlan.IsRoomed`), but roomed-vs-natural only exists for depth >= 3 and what roomed floors give is unmeasured. Q0 can measure it |
| Calibration and samples | V2+ | Needs sample provenance (assay prerequisites); none ships |
| Surface terrain (peat, sand, grass) coupling | V2+ | No surface purpose: surface is line-of-sight open |
| Multi-round listening, noise vs reach | V1.x | Needs `ActionTime` rounds (7.1 gap 2) |
| Rod wear and re-tipping with `IronSpikeComponent` | V1.1 (optional) | Adds a maintenance cycle; only worth it if play shows demand for a sink; uses `RepairablePart` + S22 |
| Up vs down discrimination for bores | V1.x, after play | Ambiguity is also a retreat feature; decide from play |
| Planted/installed post, second base | with S19 + home design | `InstallablePart`, install verbs, spoken-end verb for any standing arrangement (lore R9) |
| NPC sounding (Searchers) | S20 | NPCs act only near the player; no offscreen survey |
| Relay sharing of readings | with the relay | none exists |

---

## 9. Classification, sequencing, tests

### 9.1 Classification

**`wave1-existing-foundations`** for V1, conditional on Q0. Classification is about *what it needs*, not *how much it delivers*. The machine's pitch-level purpose is `wave3`; V1 is its honest floor.

### 9.2 Q0: purpose gate (test-only, 1 cycle, no production change)

Pre-registered rule (tunable, but fixed before measuring): **proceed to R1 only if, over at least 40 seeded ordinary underground floors at depths 1-6, the nearest unexplored way off the floor is farther than 25 walking steps from the arrival stair after the first field-of-view update in at least half of the floors, or the median fraction of passable cells unexplored at arrival is at least 60%.** If it fails, the way-finding purpose is too thin; do not build; build section 8's content layer first.

Q0 and premise-pin tests (green or recording on HEAD):

- `ResonatorPurpose_BlindRouteLengthFromArrivalStairToNearestUnseenBoreIsMeasured`
- `ResonatorPurpose_UnexploredFractionAfterArrivalFovIsMeasured`
- `ResonatorPurpose_BoreCountPerFloorIsMeasured`
- `ResonatorPurpose_SeamPrevalenceInOrdinaryColumnsIsMeasured` (realized `MineGallery` frequency; settles the 20% [U])
- `ResonatorPurpose_PaleStalkerPresenceByDepthAndSurfaceTierIsMeasured`
- `SoundingPremise_CellIsPassableIgnoresCreatures` and counter `..._ClosedDoorIsNotPassable`
- `SoundingPremise_OrdinaryColumnPredicateFalseForSinkholeFloorRootLairStackVillageAndGleanersCellar` and counter `..._TrueForPlainSpreadSandstoneColumn`; also decides `SoddenDistrict` and `MultiCellPilot` [U]
- `SoundingPremise_PaleStalkerPerceivesNoiseMarkedActorWithinSightRadiusOnly` with controls: unmarked, 11 paces, marked two ticks ago
- `SoundingPremise_EveryGetMaterialsForDepthFloorTokenIsKnown` (feeds the table guard)

### 9.3 Slices (each is one reviewable change, about 5 minutes per cycle)

| Slice | Delivers | Cycles | Depends |
|---|---|---|---|
| R0 / Q0 | purpose measurement and premise pins; doc paragraph with the numbers | 1 | none |
| R1 | `GroundSounding.Read` (pure), ground table, `SoundingEligibility`, forbidden-vocabulary and no-leak guards | 1-2 | R0 |
| R2 | `SoundingRodPart`, blueprint `SoundingRod` in `Objects.json`, command, one-action cost, `Noise.Mark`, record, refusal codes, diag, save round trip, reader line | 2 | R1, S1; S3 and S5 preferred |
| R3 | `TinkerStock` row, price, barter/dram acceptance, stock golden | 1 | R2 |
| Play | fresh-world and cached-save actual play, section 9.5 | n/a | R3 |

Total about 5-6 isolated EditMode cycles (about 30 minutes of test time) plus actual play. Independent of S2, S6, S7 (placement lane).

### 9.4 RED-first test names

**R1: `GroundSoundingTests` and `GroundSoundingAdversarialTests`**

- Reads: `Read_ReportsLargestUnexploredOpenRegionAsHollowWithEightPointBearing`, `Read_BandsFollowPaceThresholds`, `Read_PocketBelowThirtyCellsIsNotReported`, `Read_ReportsUnexploredStairAsBoreWithoutSayingUpOrDown`, `Read_ReportsUnexploredMineralVeinAsSeamWithoutNamingTheMineral`, `Read_AtMostOneLeadPerClassAndThreeLeads`, `Read_ReachFollowsTheFloorUnderfoot` (all six rows), `Read_IsDeterministicForSameZoneAndOrigin` (two calls equal; an external `new Random(123)` unaffected), `Read_StopsAtTheZoneEdge`.
- Counter-checks (identical setup, one clause flipped): `Read_IgnoresExploredCells`, `Read_IgnoresCellsCloserThanFourPaces`, `Read_IgnoresCellsBeyondReach`, `Read_IgnoresCreaturesItemsAndContainers` (a creature or chest inside a hollow, or a creature blocking a passage, changes nothing), `Read_ClosedDoorRoomReadsAsSolid`, `Read_DoesNotListEntitiesNotOnTheAllowList` (plaque, bone, body, inscription blueprints in the unexplored mask are silent), `Read_ChangesNoWorldState` (entity count, positions, tile state, HP, inventory snapshot).
- Output guards: `Read_NeverEmitsCoordinatesCountsOrPercentages` (regex over every line), `Read_NeverContainsForbiddenVocabulary` (the list in 6.3), `Read_LimitLineAlwaysNamesReach`.
- Eligibility: `Eligibility_SurfaceRefusedWithSurfaceCode`, `Eligibility_SinkholeFloorRootLairVillageAuthoredAndGleanersCellarAreNotOrdinary`, `Eligibility_OrdinaryColumnAcceptedAtDepthsOneToTwelve`, `Eligibility_UnknownFloorTokenRefusedUncoupled`, `Eligibility_EveryDepthBandFloorHasAReachRow`, `Eligibility_RefusalTextDoesNotDistinguishProtectedFromLooseGround`.
- Adversarial: null zone, null origin, origin out of bounds, origin on stairs, origin in liquid, all cells explored, no passable cells, zone full of veins (line cap), malformed reach table row (zero/negative) rejected, roomed and natural layouts both read, cached zone read.

**R2: `SoundingRodTests` and `SoundingRodAdversarialTests`**

- Verb and cost: `SoundCommandAppearsForACarriedRodInThePackMenu`, `SoundCommandIsAbsentForADroppedRod` (counter), `SoundChargesExactlyOneActionInTheWorldAndPackPaths`, `RefusalSpendsNoTurnAndMarksNoNoise` (every code), `DisabledRowShowsTheReasonWithoutExecuting` (S3) , `DoubleSubmitChargesOnce`.
- Noise: `SoundMarksNoiseOnce` (`Noise.Recent(actor, 1)`), `SoundingMakesAPaleStalkerWithinTenPacesPerceiveTheActor`, counters: `NoSoundingNoPerception`, `PaleStalkerElevenPacesAwayDoesNotPerceive`, `SightedCreatureBehaviourUnchangedBeyondOrdinary`.
- Record: `SuccessWritesRecordAndMessage`, `NothingHeardStillSpendsActionAndNoise`, `RecordRoundTripsSave` (public fields), `OldSaveWithoutRodLoads`, `RecordLabelsAgeAndStairAnchor`, `NewStrikeOverwritesRecord`.
- Refusals: `HostileInViewRefuses`, `HaulingRefuses`, `BrokenRodRefuses`, `NonPlayerActorCannotSound`, `StaleZoneRefuses`, `ReadFailureWritesNothing`.
- No oracle: `PreviewTextIsIdenticalForTwoZonesWithTheSameFloor`.
- Reader: `ReaderShowsGroundAndReachBeforeStriking`, `ReaderShowsLastReadingAndAge` (quick and document, S5).
- Diag pins: `GroundSoundedDiagPinned`, `GroundSoundingRefusedDiagPinned` (category `event`, default-on).
- Saved-Part contract (S1): `SoundingRodPart_IsInGoldenAsAddition`, `SoundingRodPart_PassesHygiene` (namespace, constructor, no `*ZoneID` field).

**R3: `SoundingRodStockTests`**

- `TinkerStockContainsSoundingRodRow`, `ExistingTinkerStockRowsUnchanged` (golden), `CachedTownStockUntouched`, `BarterTraderTakesTheRod`, `RodWeightAndValueAreWithinCarryLimits`, `NoNewCurrencyPath` (dram conservation across a buy and a sell).

### 9.5 Actual-play acceptance (CUA-only evidence, Linux/Deck unexecuted, as usual)

Fresh world (seed 64 and 704860946): buy the rod from a Tinker; reach an ordinary cave (50% cave entrances) at depth 1-2; read the rod's live preview; sound at the arrival stair; walk toward the reported bore and seam bearings and record whether they hold; sound again from a second spot and triangulate; sound on the surface (refusal), in a village (refusal), in a sinkhole floor (identical refusal text); on a tier >= 3 floor with a stalker placed by a documented fixture, show the noise consequence and the "stand still" alternative; compare turns and torch fuel to the way down, blind vs with the rod, on the same floor from a checkpoint copy. F5, fresh Continue: the record and rod persist. **Cached-save control:** an older save with a visited cave and no rod loads untouched; the rod works in it. State the limits: no packaged performance claim; glyph presentation only (graphics cancelled).

---

## 10. Owner decisions, risks, [U] list

Owner decisions:

1. Lift "no new runtime changes" for this slice (and confirm no critique gate applies).
2. Accept the narrow purpose, or require Q0 to pass first (recommended).
3. Confirm that the resonator is **not** counted toward the every-town census.
4. Confirm the Tinker as vendor and the First Tent question.
5. Approve information policy: reveal unexplored terrain only, quantized, three classes.
6. Voice owner for every string (blind gate).

Risks: (1) the purpose may be too thin (Q0); (2) a hollow lead may add little in winding caves (cut it if Q0 says so; the smaller V1 is bore + seam + limit line); (3) a seam lead is rare, since it depends on `MineGallery` (20% per roll, tier >= 1) [U realized frequency]; (4) the recomputed ordinary predicate can disagree with a bespoke pipeline (C3), pinned; (5) the noise cost matters only where hunters exist; below tier 3 it is nearly free, which is honest.

[U]: `SoddenDistrict`/`MultiCellPilot` underground ordinariness; HUD coordinates; which hostile-in-view helper to reuse (F5's gate); realized seam and stalker frequencies; glyph/colour uniqueness among item blueprints; rod price and weight; whether `StockTable` rows are rolled once at generation (assumed, matches the cached-town limit); First Tent Tinker.

---

## 11. Verification record

Read in this session: `Docs/MACHINE-ROSTER-VISION.md`, `Docs/MACHINE-SYSTEMS-DESIGN.md`, `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `design/ARCHITECTURE-SPEC.md`, `lore.md` sections 0-7 and 8-11 for M10; `UndergroundLayoutPlan.cs`; `OverworldZoneManager.cs` (routing, `CreateUndergroundPipeline`, `AuthoredWildernessZoneIDs`); `CaveEntranceBuilder.cs`; `StairsDownBuilder.cs`; `CaveClimbOutStairsBuilder.cs`; `UndergroundRouteReservationBuilder.cs`; `SinkholeMouthBuilder.cs`; `WorldMapZoneBuilder.cs`; `Cell.cs` (passability); `FieldOfView.cs`; `ZoneRenderer.cs` FOV radius; `Noise.cs`; `HuntsBySoundPart.cs`; `DormantGoal.cs`; `RigRelocation.cs` noise call; `PopulationTable.cs` (underground tiers, PaleStalker); `ZoneTierPolicy.cs`; `SolidEarthBuilder.cs` depth table; `LandmarkBuilder.cs` (`MineGallery`, `Underground`); `TradeStockBuilder.cs`; `LootTables.json` (`TinkerStock`, `WardenStock`); `Objects.json` (Tinker, PaleStalker, Creature, stairs, walls, `SurveyStake`, vein tags, operator roster); `MaterialProcesses.json`; `PlaceSites.json` (`SurveyLine`); `Concerns/DrownedLedger.json`; `VillagePopulationBuilder.cs` (Tinker 70%); `HelmwoodPassages.cs`; `ContainedLiquidSourcePart.cs` (verb pattern); `CuttingToolPart.cs` (tool precedent); `Diag.cs` default categories; `GleanersCellarBuilder.cs`.

Not read ([U], settled by named tests): `SoddenDistrict.Eligible` underground behaviour; the F5 hostile gate; HUD coordinate display; light-vs-dark rendering of LOS-visible cells; item price tables; `StampCatalog` full underground list beyond the first stamps; the realized seam frequency.

No test, build, Unity run or git write was performed.
