# Machine V1 dossier: Sump pump (machine 7)

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. Lore authority is root `Lore/` only (`Docs/Lore/` unused). All C#/JSON below is a sketch for the implementing engineer, not compiled code.

Evidence tags: **[V]** I read the cited line in this session. **[R]** taken from `mw/*.md` research, not re-read. **[I]** inference or design. **[U]** unverified; the named RED test or first-cycle probe settles it.

Designed against `mw/design/ARCHITECTURE-SPEC.md` (slice numbers S1-S24 and contracts C1-C11 refer to it). Standing gate: `CLAUDE.md` says "No new runtime changes" and keeps explicit work permission and the weapon-payload/equipped-input/save policies behind independent critique. V1 below needs no work-permission, weapon-payload or equipped-input policy and adds only additive saved fields, but implementing any slice still needs the owner to lift the hold.

---

## 0. Verdict

1. **The pump has no real purpose in the shipped world.** Nothing is submerged, no space has a water level, wading is legal everywhere, no natural pool can be drained to dry footing under the shipped rules, the one authored pump problem (Cinderhold's "Drowned Level") is a pure settlement-state record with no physical pump, no flooded space and no liquid in its zone, and there is no bulk consumer of liquid (the still takes no water; crops take 1 unit per bed by hand). This is consistent with the project's own docs, which call the pump "unqualified" and say a future pump needs "a reviewed finite sump/resource objective and explicit depletion consequences" (section 2).
2. **The smallest honest V1 is a pair, not a machine.** One authored, optional, fresh-only *opportunity* (a finite bog-water sump holding a small cache, closed against bare reaching by a level threshold) plus one hand pump that is the flask-free, exposure-free way to lower it, with a lasting consequence at the outlet. It ships at **one** site (the Sodden Peat Works, `Overworld.17.7.0`), through the same plan-flag path the field work rig and the oil strainer already use. It is **not** a town machine and adds **zero** towns to the "machine in every town" census (section 4.13).
3. **Classification: `wave2-needs-shared-contracts`**, conditional on one content decision (D1). It needs spec S3 (declared action time, disabled rows) and S5 (reader), plus one shared contract the spec lacks: a liquid-holder / ground-pour contract extracted from `LiquidVesselService` (section 5). It needs no new system: no fluid simulation, no elapsed job, no fuel or power, no fee, no terms. If the owner declines D1, the honest status is "no purpose; park behind a bulk-liquid consumer (fractionating still or vat)", and the pump stays in the spec's "Not now" list.
4. **V1 qualifies the pump at one site; it does not close the pump gate.** The design doc's gate is "a real passage or resource changes access; transferred liquid appears at a valid destination with no regeneration exploit" (`Docs/MACHINE-SYSTEMS-DESIGN.md:91`). V1 meets the letter with a finite cache and a conserved, non-seeping source, and is deliberately weak on the spirit (a passage). The Cinderhold embodiment, supply-a-still, fill-a-cistern, seepage and powered drives are later versions (section 9).
5. **Purpose test (section 4.15).** The earlier oil-tray record warned that a cheap alternative can make a machine pointless ("Actual play may show it is a nearly free alternative; preserve that finding", `Docs/MACHINE-CONTAINED-OIL-RECOVERY.md:30`). Counting actions shows the pump beating the flask path (4 versus 7) once a level threshold blocks bare reaching, so the risk in V1 is not dominance by an alternative; it is **small stakes** (the whole reward is two iron spikes that the Cinderhold smith sells). V1 therefore ships with an acceptance test that can fail: if actual play shows nobody finds the cache worth the trip, V1 is recorded as not purposeful and parked rather than inflated.

---

## 1. The pitch and the project's own bar

User pitch (`Docs/MACHINE-ROSTER-VISION.md:59-63`): "Drain a flooded room, fill a cistern, supply a still, or lower the water around submerged machinery. The liquid needs a destination, and unsuitable fluids can require special fittings. Pumping out an abandoned basement could expose a useful room while making the courtyard outside wetter. ... bounded reservoirs and authored spaces without requiring a full fluid simulation."

Project gate (`Docs/MACHINE-SYSTEMS-DESIGN.md:91`): needs "finite source/receiver transfer, level/state thresholds, compatible fittings and outlet consequences". Rules that bind V1: no thirst tax or degradation of ordinary wells (`:111`), no well-scarcity premise (`:152`), no compulsory upkeep or free refill (`:136`), "A label is not a resource supply" (`:20`), a pump needs "a finite optional sump/resource objective and its depletion/destination consequences" (`:146`), and "do not enlarge the tray ... to create demand for a machine" (`Docs/MACHINE-CONTAINED-OIL-RECOVERY.md:11`).

---

## 2. Verified foundations, and what they do not give

| # | Fact | Evidence |
|---|---|---|
| F1 | A liquid owner is one of: ground pool (`LiquidPoolPart`: `LiquidId`, `Volume`, no capacity), finite container source (`ContainedLiquidSourcePart`: `LiquidId`, `Volume`, `Capacity`), carried vessel (`LiquidVesselPart`: `Capacity`, `Volume`, `LiquidId`), or a well (`WellPart.Stock`, -1 endless). Four shapes, no shared interface. | [V] `LiquidPoolPart.cs:41-45`; `ContainedLiquidSourcePart.cs:25-27`; `LiquidVesselPart.cs` fields; `WellPart.cs` `Stock` |
| F2 | Flask <-> pool/contained-source/ground transfers exist, one action each, transactional, with exact-quantity quotes, rollback and post-commit diag. Pool draw, contained draw, ground pour are three separate code paths inside one static class; the destination rules (`ValidDestination`) and pour (`Pour`) are private. | [V] `LiquidVesselService.cs:93-124` (Fill), `:126-155` (FillContained), `:188-259` (Pour), `:299-315` (ValidDestination) |
| F3 | A poured pool is a `PouredLiquidPool` entity. While it lives, `Zone.ProjectPool` writes a **permanent** coating on its cell; it is retired only when its volume is drawn to 0 by a vessel, and only for that blueprint. Nothing evaporates it. So a poured liquid is a lasting patch, undone by labour (bail it back with a flask). | [V] `Zone.cs:839-852` (permanent projection), `LiquidVesselService.cs:161-186` (retire only `PouredLiquidPool`), `Objects.json:44602`; `LiquidCoveredEffect.cs:225` (Evaporativity affects creature coating dry-down only) |
| F4 | A natural pool at volume 0 stays: `LiquidPoolPart` reports "PoolEmpty" without removing owner or coating; the retire helper is blueprint-gated. `MirePool` also carries `TileStateSource` (renewing coating) and `BurnOffGas`. **No natural pool can be drained to dry footing**, including Sumphold's `WaterPuddle` cuts, which `settlement-sumphold.md:146` calls "the one drainable-to-dry-footing pool in the region": that claim is false in effect (the owner and its permanent coating persist at 0). | [V] `LiquidPoolPart.cs:123-128`; `LiquidVesselService.cs:181`; `Objects.json:35548-35615` (MirePool), `:23654` (WaterPuddle); `MACHINE-SYSTEMS-DESIGN.md:146` |
| F5 | **Depth is not modelled.** Wading contact applies a coating dose `min(Volume, Str+Toughness)`; reaching through a contained source applies `min(12, Volume)`. The exposure is capped at 12 regardless of volume, so a bigger sump is never harder to reach through. | [V] `LiquidPoolPart.cs:130-141`; `ContainedLiquidSourcePart.cs:112-113,155` |
| F6 | The shipped "submerged resource" gate is hard-wired: `ContainerPart.AllowsCleanAccess` asks `ContainedLiquidSourcePart.AllowsCleanAccess` (`ValidPayload() && Volume == 0`). A container under a liquid refuses clean Open/Take until drained; the shipped alternatives are a flask draw (12 per action) or "recover through the liquid" (one action, brief exposure, liquid stays). The refusal wording is hard-coded "Drain the tray first". | [V] `ContainerPart.cs:30,117,131`; `ContainedLiquidSourcePart.cs:30,146-156`; `TakeFromContainerCommand.cs:57-61,96-98` |
| F7 | `ContainedLiquidSourcePart` is generic over any registered liquid (`ValidPayload` checks the registry, not "oil"), refuses `StackCount != 1` items for take, and its context needs the actor within 1 of the source. | [V] `ContainedLiquidSourcePart.cs:31-47,67-76` |
| F8 | Bog-mire: viscous (`Fluidity 6`), `PerTurnDamage 1 Acid`, `Agility -2`, `FireDampen 50`, non-conductive. Water: `Fluidity 30`, `Conductivity 100`. Crop irrigation by pour accepts **water only**, spends 1 unit per bed, sets moisture to 40 ticks. | [V] `LiquidDefinitions/bog-mire.json`, `water.json`; `CropPourIrrigation.cs:17`; `CropWateringService.cs:12` |
| F9 | The world menu's turn charge is a hand-kept OR-chain; the oil-tray recovery had to add itself to it. A command in no chain falls to a bare `InventoryAction` with no transaction. | [V] `InputHandler.cs:3459-3471` (`:3465` is `ContainedLiquidSourcePart.IsRecovery`) |
| F10 | The Sodden Peat Works (`17.7`) already hosts two fresh-only optional fixtures through plan flags (`includeFieldWorkRig`, `includeOilStrainer`) with a closed `Valid`/`Payload` switch; an unknown solid blueprint **rejects the whole district** (`Valid` computes `solid` from a name list and returns false on `p.Solid != solid`). All three Sodden zone ids are retained forever (`SoddenDistrict.Retain`), so relapse regeneration cannot reset them. | [V] `SoddenDistrictBuilder.cs:48,157-162,181-182`; `SoddenDistrict.cs:10`; `OverworldZoneManager.cs:1241-1244`; `SoddenDistrictPlan.cs:70-95` |
| F11 | Works geometry: two fixed sheds (`Room(43,3,67,10)` north, `Room(11,14,31,20)` south, door `20,14`, salvage `27,16`); mire cuts are walled from the yard by solid `PeatBank` rows (`y=4`, `y=17`), so no dry cell touches a mire cut. Room interiors do not vary with the seed (`variation` moves cuts and wall gaps only). | [V] `SoddenDistrictPlan.cs:70-95` |
| F12 | Cinderhold's "Drowned Level": concern `cinderhold.pump` with three approaches (mend pump / clear field / clay seal), a `Pump` structure site (lasting: 2 `IronSpikeComponent` + 1 `OakHaftComponent` + 1 `LeatherBindingComponent`; patch: 2 `FireClay`, relapse 600 turns), conditions `PumpRunning` (prices x0.95, Quartermaster topped up with 2 spikes once), `LevelCleared`, `Sealed` (`RotChoir -3`). No physical pump, no flooded level, no liquid in the zone. | [V] `PlaceSites.json:114-147`; `Concerns/Cinderhold.json:4-84`; `PlaceConditions.json:518-548`; [R] `settlement-cinderhold.md:120` |
| F13 | The only shipped cistern holds 3 draws and is deliberately scarce (Beating exposure is "the region's point"); the Spread's trough is endless; `WellPart` has no add-water path. | [V] `Waysites.cs:136-143` |
| F14 | `liquid` is a default-on diag category. | [V] `Diag.cs:119-120` |
| F15 | The oil tray's own label says it "is separate from the mine pump and communal water supply". | [V] `Objects.json:18` |

### Corrections to earlier premises

| Premise | Verdict |
|---|---|
| `settlement-sumphold.md:146`: Sumphold's cuts are "the one drainable-to-dry-footing pool in the region" | **False in effect** (F4). Draining them yields collectible units but not dry footing. |
| `lore.md` section 6 lists "7 pump (well-line)" for Sill and "(weir/lake level)" for Tine | Matrix suggestions only [I]. Verified: Sill's well is an endless `WellPart`; Tine's weir is data with no entity and its lake is a 120-unit native pool with no seepage (F4, `placement.md:165`). Neither has a purpose for a pump. |
| `settlement-sumphold.md:162`: Sumphold pump "plausible" | Downgrade: nothing is submerged and wading is legal; no town unit has an authored access relation. |
| A "bog returns it" outlet is a free, hazard-free destination | Geometry does not allow it at the Works (F11). Kept only as a [U] V1.1 option. |
| Reaching through a contained liquid gets harder with depth | False (F5). V1 must add a level threshold or the pump is dominated at every size. |

---

## 3. Purpose audit (honest)

| Pitch use | What exists today | What is missing | Verdict |
|---|---|---|---|
| Drain a flooded room, reach what is inside | `ContainedLiquidSourcePart` + `ContainerPart` gate (F6) is a one-container version with no depth | An authored flooded opportunity; a level threshold (F5); a bulk way to lower it | **Buildable** (V1 supplies both) |
| Fill a cistern | Waysite cistern, 3 draws, scarcity is the point (F13); no add path on `WellPart` | A refillable cistern with a *finite* upstream source; any water source is an endless well or a Tent-Right-protected pool | **No purpose.** Pumping an endless well into a cistern mints water; Tent-Right forbids pricing or metering water (L3). |
| Supply a still | `AlchemyStill` takes reagents, not water [V] (`Docs/MACHINE-SYSTEMS-DESIGN.md:60-65`; no water use in `Gameplay/Alchemy/*`) | A fractionating still or vat that consumes bulk liquid | **No purpose until M2/M5 exist.** |
| Lower water around submerged machinery | The field work rig and oil strainer stand on dry floor | A flooded machine; a generic `Submerged` gate (F6 is hard-wired) | **Later** (needs the gate generalised) |
| Redirect a local supply / irrigate | Crop pour takes 1 unit per bed (F8) | A field at a scale where hand watering is a burden | **No purpose**; ordinary path is cheap and must stay so |
| Cinderhold "Drowned Level" | Concern, site, conditions, consumables, Choir asymmetry (F12) | Physical pump bound to the site; a flooded space; seepage (needs elapsed work) | **Strongest long-term home; wave3 (V2)** |
| Natural pools, lakes, wells, bogs | Everywhere | Dry-footing semantics (F4), protection tags (L1-L8) | **Refused by design in V1** (closed source set) |

**What must exist first (any one):** (A) an authored finite flooded objective with a level-gated reward (V1 supplies the smallest); (B) a bulk-liquid consumer machine (still/vat/bath); (C) a Cinderhold sump with seepage and elapsed-work policy (S15/S16). V1 is option A only.

---

## 4. V1 definition

### 4.1 Names and fiction (drafts; each needs an intent card and the blind voice gate, L17/R10)

- Canon vocabulary available: "mine pump" (shipped), "bilge", "bog-sump", "water-lift" (`lore.md:162`); Sodden/Villagers craft voice, not Concord and not Curation. Works precedent: "Four sound timber lengths remain in this abandoned drying frame. Harvest them once." (`Objects.json:2137` area) and "Left for whoever can use it" (rig plate, `lore.md:48`).
- **Proposed canon name:** the pump is "the crew's bog-pump" (display "hand pump"); the cache is "the sunken tool-store". Avoid "regulator", "valve", "motor", "power" (L16).
- Draft reader lines (not shippable text): pump, "An iron hand pump on a timber frame, the leather cup perished at the edges but whole. A tag on the handle: Left for whoever can work it."; store, "A timber tool-store that went under when the crew's pump stopped. Black water stands over the lid." The engine-neutral lines (fitting, quantities, limits) are generated and carry no faction voice.

### 4.2 Fixtures and parts

Everything is plan-owned, fresh-only, at `Overworld.17.7.0`.

| Entity | Parts (new in bold) | Saved fields |
|---|---|---|
| `WorksBogPump` (solid, non-takeable furniture; glyph presentation only, graphics cancelled) | Physics, Render, Examinable, **`LiquidPumpPart`** | `StrokeUnits=12`, `Reach=2`, `Fitting="bilge"` (token). No zone id, no coordinates, no owner id (S1 hygiene lint). |
| `SunkenToolStore` (solid, non-takeable; same shape as the oil tray) | Physics, Render, Examinable, Container (unlocked, authored contents), `ContainedLiquidSourcePart` (existing) | `LiquidId="bog-mire"`, `Volume=36`, `Capacity=36`, **`ReachDepth=12`** (new additive field, default 12) |
| Contents | exactly two `IronSpikeComponent`, each its own `StackCount==1` entity (F7) | none new |

Numbers and why: stroke 12 = one flask load, the unit every liquid slice already uses (`MACHINE-LIQUID-HANDLING.md` calibrates flasks to 12), so the pump matches a flask fill and removes the separate pour action. Sump 36 = three strokes: large enough that, with `ReachDepth 12`, a bare reach is refused until at least two strokes are done, small enough that nothing is a chore. Spikes: exactly the iron half of a Cinderhold lasting pump repair (F12), already sold by the Cinderhold smith, already a Works-adjacent M3a output, so the cache is an *alternative source*, not a new gate and not a new economy item; two, finite.

### 4.3 Rules

1. **Level threshold (new, additive on a shipped Part).** `ContainedLiquidSourcePart.ReachDepth` (public int, default 12). "Recover through the liquid" is offered and permitted only while `Volume <= ReachDepth`; the direct command and `CanTake` enforce the same bound (no bypass). Clean Open/Take still needs `Volume == 0` (F6). The existing 12-unit tray is unchanged (its volume never exceeds 12). Flask draws are unchanged at any volume. The refusal text in `TakeFromContainerCommand` (`:61,98`) becomes owner-neutral ("Drain it first, or recover through the liquid once it is shallow enough."); no test pins the current string [V: grep of `Assets/Tests` found none].
2. **Source set is closed (V1 safety property).** The pump draws only from a `ContainedLiquidSourcePart` owner within `Reach` of the pump. It never draws from a `LiquidPoolPart`, `WellPart`, mire, lake or spring. This sidesteps every natural-water landmine (Ledger peat, Tent-Right wells, Choir seeps, Ginmere, Quiet's Door) and the dry-footing hole (F4) by construction, and means no regeneration exploit is possible. Honest limit shown in the reader (section 4.8).
3. **Fitting.** `Fitting="bilge"` accepts `water` and `bog-mire` only (code table; unknown fitting fails closed). Anything else, including the shipped oil tray and every lore-loaded liquid (L19), is a visible refusal, not an omission. "Special fittings" for other liquids are a later version.
4. **Stroke.** One stroke moves `min(StrokeUnits, sump.Volume)` from the sump to one **outlet**. It is atomic: it either commits fully or changes nothing.
5. **Outlet.** One of the 8 neighbours of the pump cell that passes the existing destination rules (passable, unmixed with the same liquid or empty, not a Door/Stairs, not a Well, not a Container, no creature-only blockers per the existing rule). The outlet receives a `PouredLiquidPool` of the same liquid through the same code that `Pour` uses (F2/F3), so contact, permanent projection and crop-irrigation rules are the shipped ones.
6. **No seepage, no refill, no upkeep.** The sump never regains volume; the pump has no wear, fuel or timer.

### 4.4 Player verbs

| Verb | Where | Effect |
|---|---|---|
| Look / `x` / F1 reader | pump, store | State, quantities, fitting, limits, outlet warnings (4.8) |
| **Work the pump** (new) | world menu on the pump, actor adjacent | "pump 12 bog mire to the north: sump 36->24, outlet 0->12 (lasting puddle)", one row per valid outlet; disabled rows with reasons |
| Draw with a flask (existing) | world menu on the store | `FillContained`, 12 per action |
| Recover through the liquid (existing, now level-gated) | world menu on the store | One action, brief exposure, liquid stays; only at `Volume <= 12` |
| Open / Take (existing) | the store | Only at `Volume == 0` |
| Bail a puddle back with a flask (existing) | the outlet puddle | The undo path; retires the puddle at 0 |
| Leave it | n/a | Nothing is lost; the smith sells the same spikes |

### 4.5 Inputs, outputs, units

- Input: bog-mire from the sump, pool units (same unit as every liquid slice; not drams, not carried mass). 36 units finite.
- Output: the same units at the chosen outlet as a `PouredLiquidPool` (ground), and, once the sump is shallow or empty, the two spikes via existing Take/recover verbs.
- Conservation ledger, per stroke: `sump.Volume -= n; outletPool.Volume += n`, with `n = min(12, sump.Volume)`; an outlet that already holds same-liquid pool volume merges (existing rule). Total units are conserved across any scripted sequence including flask bails (property test).

### 4.6 Loss and waste

- **No unit is lost or minted.** The "waste destination" is the outlet puddle, named in the preview. There is no evaporation (F3) and no seepage.
- The puddle is a **lasting** hazard patch until bailed: `bog-mire` stings 1 acid per turn and costs 2 Agility on contact, permanent projection, no ignition (F3, F8). This is the "courtyard outside wetter" consequence, using only shipped rules.
- Undo cost is labour and a flask (draw 12 per action); retired at 0.

### 4.7 Cost

One ordinary action per stroke (turn charge through the world-menu route; declared `ActionTime.OneAction` once S3 exists, otherwise a one-line addition to the legacy chain at `InputHandler.cs:3459-3471` with its own pin test). No fuel, no fee, no wear, no maintenance, no elapsed or unloaded work. Time cost is real: working the handle three times is three turns of exposure to whatever lives in the works.

### 4.8 Preview and readers

- Row (exact quantities, same style as M1a): `pump 12 bog mire to the north (sump 36->24; outlet 0->12, lasting puddle that stings and slows)`; when the outlet already holds the same liquid: `... outlet 12->24`.
- Pump reader (generated): `Hand pump. Fitted for: water, bog mire. Stroke 12, reach 2. Intake in reach: sunken tool-store, bog mire 36/36. Outlets: 5 open. Works only a sump set apart for it; open water and wells are too wide.` (last clause is a draft [voice gate]).
- Store reader (existing `ContainedLiquidSourcePart.Describe`, extended): level line, "Too deep to reach through: draw it down to 12 or less." Conditional on `ReachDepth`.
- Both readers should come from S5's collector; until S5, add the two lines the way M2a did (`ExaminablePart.cs:138`), with the same golden-text pins.
- Disabled rows (S3 `AddDisabled`, stable codes): `no-intake` (no sump in reach), `dry` (volume 0), `unfitted` ("The pump is not fitted for oil." with the liquid named), `no-outlet` (no valid neighbour), `deep` is not a pump row (it belongs to the store).

### 4.9 Blocked, failed, interrupted, saved states

| State | Behaviour | Diag (`liquid` category, default-on) |
|---|---|---|
| No sump in reach | Disabled row `no-intake` | `PumpRejected {reason}` on selection |
| Sump dry | Disabled `dry` | same |
| Fitting mismatch (oil, protected liquid, unknown fitting) | Disabled `unfitted`, liquid named | same |
| No valid outlet | Disabled `no-outlet`; individual invalid outlets are simply not listed | same |
| Stale quote (volume, liquid, owner, cell changed between menu and commit) | Refuse, spend nothing, nothing changes | `PumpRejected {contents-or-zone-changed}` |
| Actor dead, action-blocked, not adjacent, not current | Refuse | `PumpRejected {actor-*}` |
| Callback or exception mid-commit | Both sides roll back through the transaction | none (rollback is silent, as in M1a) |
| Combat interrupts between strokes | Nothing is pending; no job exists; state is exact | n/a |
| Save at any point | All state is existing public fields (sump volume, puddle pools) plus pump config; reload continues at the same level | n/a |
| Old save with no pump or no `ReachDepth` field | Loads; the field defaults to 12; no behaviour change | n/a |
| Cached 17.7 zone | Never retrofitted (stated limit) | none |

Success emits `liquid/PumpStroke {pump, source, outlet, liquid, units, remaining}` after commit.

### 4.10 Alternatives the player keeps

Action counts assume the full 36 units and both spikes; "take" is one ordinary Open/TakeAll on a drained store, a wet recovery is one action per item (`ContainedLiquidSourcePart.cs:154-155`).

| Approach | Actions | Needs | Exposure | Where the liquid goes | Notes |
|---|---|---|---|---|---|
| **Pump** | 3 strokes + 1 take = 4 | nothing carried | none | outlet puddle(s), player-chosen | The only approach that needs no flask; you choose where the patch lands |
| **Flask bail** (existing) | 3 draws + 3 pours + 1 take = 7 | a flask (22 drams at Sill, `MACHINE-LIQUID-HANDLING.md`) | none | wherever you pour (same patch rules), or keep it in the flask | Always available to a flask owner; each pour needs a destination too |
| **Flask shortcut** (existing verbs, level-gated) | 2 draws + 2 pours + 2 recoveries = 6 | a flask | ~12 units of mire once per recovery (1 acid per turn, -2 Agility, brief) | the last 12 stay in the store | Reach-through becomes legal at volume <= 12 |
| **Pump + reach-through** | 2 strokes + 2 recoveries = 4 | nothing carried | as above | last 12 stay | Never better than the full pump (equal actions, adds exposure); it exists so the reach-through verb stays meaningful |
| **Leave it** | 0 | n/a | n/a | n/a | The smith sells the same spikes; nothing else is gated |

Honest note: the pump beats both flask paths on actions and exposure and removes the flask requirement, so its cost is the expedition (the Works) and the outlet decision, not a worse alternative. The weakness is small stakes (4.15), not dominance. The pump is not a lock: a flask owner, a buyer of spikes, and a player who walks past all remain fully served.

### 4.11 Consequences outside the pump's menu

1. **Outlet hazard.** Dumped mire is a permanent patch for the player and every NPC or creature (contact rules, `LiquidPoolPart.cs:76-143`). Dumping on a route cell is a real, reversible-by-labour mistake the preview warns about.
2. **Container gate.** Lowering the sump re-enables the store's reach-through and then clean Open/Take (F6): the pump changes what another owner allows.
3. **Placement and base rules.** Pooled cells are not bare ground for the generic installer (`DoorPart.IsBareGround` rejects `LiquidPoolPart` occupants, `DoorPart.cs:74`), so a dumped patch blocks later installation on that cell (relevant to the home/base system).
4. **Cinderhold link.** The two spikes are the iron half of the Pump site's lasting repair (F12); recovering them at the Works and using them at Cinderhold crosses three systems (liquid, container gate, repair site/conditions). It is an alternative source, never a requirement.
5. **Flask economy.** Mire drawn into a flask by the bail alternative is a carried hazard liquid; pouring it follows M1a rules.

### 4.12 Operators and NPC roles

- V1 needs **no operator**: the Works is abandoned and the fixtures are relinquished by their text, like the rig. NPCs cannot work the pump in V1 (player-only; S20 later).
- Existing roles that explain or sell around it: **PeatCutter** keeper at the dressing shelter (Sella; the route text already names "the old cutting works" in a Sumphold TollRolls work slip, `SoddenDistrict.cs:56-64`), **Tinker/smith** (sells spikes, haft, binding), **Elder** (Cinderhold concern commissioner; V2). Missing roles: none required.
- Optional discoverability: one clause appended to the existing work slip text (fresh-only, the `DescribeTownRoute` precedent, `placement.md:273`), voice-gated; draft: "A pump handle still stands over the cutters' sunk store." [draft]

### 4.13 Placement, retention, census

- **Placement:** authored plan flag, the third Works fixture group: `SoddenDistrictPlan.Create(id, seed, includeFieldWorkRig, includeOilStrainer, includeSump)`; builder passes `ContentAvailable(factory)`; both new blueprints are added to the closed `solid` list and to `Payload` validators in `SoddenDistrictBuilder.Valid` (F10) with a RED proving an unlisted solid rejects the district. Candidate region: south shed interior (`x 12-16, y 16-18`) [U] or a yard cell pair off the boards; the P0 probe fixes exact cells with constraints: pump has at least 3 valid outlet neighbours, at least one of them route-adjacent and at least two off-route (so the dump choice is real), the pair is within reach 2, neither blocks the door route `(20,14)->(20,15)` or the salvage at `(27,16)`, valid for all five `variation` values and seeds 64, 224401440, 704860946, 1729.
- **Why not the generic installer (S7):** S7 is exterior-only, one blueprint per row, and anchored to the village `Well` (spec C3, section 1 item 10). The pump+store is an interior-capable *pair* in a non-village zone.
- **Retention:** Sodden zones are retained forever (F10); no `InstallationMarkerPart` is needed. Once S2 exists, give the store a marker with `RetainZone=true` for uniformity (state-bearing, E07-consistent) and the pump `RetainZone=false`.
- **Old worlds:** a Works zone not yet generated in a world with `Exploration.Version >= 14` gains the pair on first visit; a cached Works zone never does. State this limit in the evidence.
- **Census role (spec C3 `Coverage`):** the pump adds **zero** towns. Proposed rows:

| Place | Class | Basis |
|---|---|---|
| Cinderhold | `Pending` (owner: pump V2, wave3) | only authored pump problem (F12); needs physical flooded space + seepage + terms |
| Sumphold | `Pending`, no purpose | wading legal, cuts not submerged (F4) |
| Sill, Tine, Gantry, Slip, Posy, Quillhold, Marrowstye, Tally, Last Counter | no pump purpose | `Pending`/other machines own these rows |
| Wellmeet, the First Tent | `Exempt` for pump (kind-scoped) | water is given never sold; poisoning a well is the second-worst crime; no machine at the First Tent (`lore.md` L3, `:257`, `:258`) |
| Drowned Ledger | `Exempt` for pump (kind-scoped) | dewatering peat is decay: a Pale Curation and Recension crime (L5, L6; `lore.md:158,261`) |
| Salt-Vault | `Exempt` for pump | no pump that risks the dry chambers (`lore.md:259`) |
| Ginmere | `Exempt` | frogs must not become load-bearing (Mystery #8; `lore.md:161,270`) |
| Quiet's Door, Olderdeep, Lampwell, Spivenor, Deepest Cathedral, Stillleaf | `Exempt` | catacomb/Choir/Rooted constraints (`lore.md` section 6, L4, L8) |
| Morrowfast | `Pending` (scene-runtime hook) | non-canon local proposal |

The spec's `Exemptions` are place-level, so a pump exemption at Wellmeet would also block the forge there. **Spec gap:** add an optional `Kinds` array to `Exemptions` (empty = all kinds). Until then the pump is simply never a registry row.

### 4.14 Home/base hooks

No positional or zone-id field on any new Part (S1 lint). Intake and outlet are resolved at use time by adjacency, so the pump is relocatable by construction. V1 pump is non-takeable; a portable "kit" version, player installation, and `IStationCapability` kind `pump` plus `StationReceipt.Begin` (so the S10 access seam can govern a communal pump at Cinderhold) are deferred and are the only slices that touch S4/S10/S19.

### 4.15 Purpose test (acceptance gate that can fail)

Actual play in a fresh world must show, with F12 off and no spawned supplies: (a) the pair is found by ordinary exploration and readers; (b) a player **without a flask** can open the store only with the pump, and that this is a decision rather than a lock (leaving, buying spikes, or buying a flask are all viable); (c) a player **with a flask** sees a visible, different trade-off (7 actions for a full bail, 6 with the shallow shortcut and a brief sting, versus 4 for the pump with none); (d) at least one real outlet decision with a visible lasting consequence and a successful bail-back undo; (e) a player judges, in the recorded play notes, whether the two-spike cache was worth the expedition. **Recorded failure criterion:** if nobody finds the cache worth the trip, or the outlet consequence never changes a decision, record that finding, do not inflate hazard, volume or reward to rescue it, and park the pump until a bulk consumer exists.

---

## 5. What V1 needs that the spec lacks (and what it takes from the spec)

Takes from the spec: **S3** (`ActionTime.OneAction`, `AddDisabled` with stable codes; without it the pump adds itself to the hand chain, F9), **S5** (reader contract; without it two hand edits as M2a did), S1 (saved-Part golden and hygiene lint for `LiquidPumpPart` and the additive `ReachDepth`), diag vocabulary (`liquid` category, default-on).

Spec lacks:

1. **A liquid-holder / ground-pour contract.** F1/F2: four holder shapes, three private transfer paths. V1 needs holder-to-ground transfer without a vessel. Proposal: extract `LiquidGroundPour` (destination rules + pour commit) from `LiquidVesselService.Pour` behind a parity oracle, and add read/write adapters for `ContainedLiquidSourcePart`. This is the liquid sibling of S17's receivers (`IProcessReceiver` Pack/Tray/Ground/Vessel); the pump is S17's natural first liquid consumer, but S17 as written covers process outputs, not fixture-to-fixture transfer. Keep the contract private to liquids until a second consumer.
2. **Pair placement and kind-scoped exemptions** in `Installations.json` (4.13).
3. **A generalised submerged gate.** `ContainerPart.AllowsCleanAccess` is a hard-wired call into one Part (a principle-P3 violation, `arch.md` section 6). V1 reuses it as is, adds one additive field, and records the trigger for an `IContainerAccessGuard` interface: the second flooded fixture.
4. **Interior authored placement outside the village pipeline** is not in S7/S24; V1 follows the existing plan-flag precedent instead (F10).
5. **Liquid-aware protection tags** (`lore.md` section 10 `ProtectedSubject`/`LivingTended`/`SacredFixture`/`InertScene`). V1 avoids the need with a closed source set and an allow-list fitting; any portable or player-placed pump needs them first.

No elapsed-work, job, fuel, terms or NPC contract is needed by V1 (S4, S10, S15-S17, S20 are not dependencies).

---

## 6. Slices and RED-first tests

One cycle = one isolated-clone EditMode run (about 5 minutes). Every slice: RED first (a compile error counts), counters per positive assertion, diag pins, cold-eye pass with both audit angles, hypothesis-driven pass if the first finds nothing.

| Slice | Content | Cycles | Depends |
|---|---|---|---|
| P0 | Premise probes (read-only, green on HEAD, pin the facts F3-F6, F9, F10; settle [U]) | 1 | none |
| P1 | `LiquidGroundPour` extraction with parity oracle; `ContainedLiquidSourcePart` read/write adapter | 1-2 | none |
| P2 | `ReachDepth` threshold + owner-neutral refusal text | 1 | P0 |
| P3 | `LiquidPumpPart` + `LiquidPumpService` (one stroke, rows, disabled reasons, diag, readers) | 2 | P1, P2, S3, S5 (or hand-edits with pins) |
| P4 | Content: plan flag, two blueprints, validators, fresh-only + cached controls | 2 | P3 |
| P5 | Actual play + purpose test (section 11) | play | P4 |

### P0 (pins; names are the contract)

`PumpPremise_PouredBogMireProjectsPermanentCoatingUntilDrawnOut`; `PumpPremise_NaturalMirePoolKeepsOwnerAndCoatingAtZero`; `PumpPremise_ReachThroughExposureIsCappedAtTwelveRegardlessOfVolume`; `PumpPremise_ContainedSourceHoldsBogMireAndFlaskFillsTwelve`; `PumpPremise_UnregisteredCommandFallsToBareInventoryActionWithoutTransaction`; `PumpPremise_UnknownSolidBlueprintRejectsWholeSoddenDistrict`; `PumpPremise_WorksInteriorCellsAreSeedIndependent` (five variations); `PumpPremise_PouredPoolIsNotBareGroundForDoorPlacement`.

### P1

`GroundPourParity_TruthTableMatchesLegacyPourForEveryDestinationState` (oracle copy of legacy rules over at least 30 layouts: passable/impassable, same/other/no liquid, coating of another liquid, existing pool, door, stairs, well, container, creature); `ExistingVesselPourSuitesPassUnchanged`; `ContainedSourceAdapter_RemoveAddRoundTripsExactVolumeAndRollsBack`; `ForwardersContainNoDuplicatedDestinationRules` (source pin); counters: same layout with one clause flipped.

### P2

`ReachDepthDefaultTwelveLeavesOilTrayBehaviourIdentical` (counter); `RecoverThroughLiquidRefusedWhileVolumeAboveReachDepth` (RED); `RecoverThroughLiquidOfferedAtOrBelowReachDepth`; `DirectRecoveryCommandCannotBypassReachDepth` (anti-exploit via `CanTake`); `FlaskDrawStillWorksAboveReachDepth` (counter); `CleanTakeStillRequiresZeroVolume`; `OldSaveWithoutReachDepthLoadsWithDefaultTwelve`; `ReachDepthRoundTripsPublicField`; `RefusalTextNamesNoTrayForNonTrayOwners`; the 56 shipped M2a cases pass untouched.

### P3

Mechanics: `PumpStroke_MovesExactlyStrokeUnitsFromSumpToOutlet`; `PumpStroke_LastStrokeMovesRemainder`; `PumpStroke_ConservesUnitsAcrossScriptedSequence` (property sweep over random volumes, strokes, flask bails, merges); `PumpStroke_CreatesPouredPoolWithPermanentProjectionAndContact`; `PumpStroke_MergesIntoExistingSameLiquidPoolAtOutlet`; `PumpStroke_RefusesMixedDestination`; `PumpStroke_RefusesWellContainerDoorStairsOutlets` (L3 counter set); `PumpStroke_RefusesNaturalPoolAsSource` (anti-exploit; mire and a well each); `PumpStroke_RefusesWellAsSource`.
Fittings: `Fitting_BilgeAcceptsWaterAndBogMire` (water exercises the general path with a synthetic sump); `Fitting_OilTrayIsRefusedWithNamedLiquid`; `Fitting_LoreLoadedLiquidsRefused` (the L19 set: `memory-bath`, `held-breath-lacquer`, `felling-counter-resin`, `bower-resin-amber`, `veined-pulse-mycelium`, `tepuibone-slurry`, `choir-wort`, `choir-mirror-mucilage`); `Fitting_UnknownTokenFailsClosed`.
Gates: `PumpStroke_RefusesOutOfReachIntake`; `PumpStroke_RefusesActorDeadBlockedOrNotAdjacent`; `PumpStroke_RefusesNonPlayerActor` (V1 limit, counter: player works); `PumpStroke_StaleQuoteRefusesWhenSumpChangedBetweenMenuAndCommit` (a flask draw in between); `PumpStroke_CallbackThatMovesOrReplacesSumpRollsBackBothSides`; `PumpStroke_ThrowingPoolFactoryLeavesNoPartialState`; `PumpStroke_ChargesExactlyOneTurn` (world menu, real input route); `PumpDisabledRowsCarryStableCodesAndSpendNoTurn` (`no-intake`, `dry`, `unfitted`, `no-outlet`); `PumpDisabledRowKeepsMenuOpen` (S3); `TwoPumpsTwoSumpsResolveIndependently`; `PumpResolvesIntakeByAdjacencyNotStoredCoordinates`.
Persistence and readers: `PumpPartConfigRoundTripsAndPassesHygieneLint`; `SaveMidDrainThenReloadContinuesAtSameLevel`; `PumpReaderLinesAppearInQuickAndDocumentReaders`; `StoreReaderStatesDepthRule`; diag pins `PumpStroke`, `PumpRejected` per code.
Adversarial file `LiquidPumpAdversarialTests` (taxonomy surfaces: atomicity/rollback, parser, cross-actor, anti-exploit, boundary, diag dispatch, multi-instance, save reflection): null actor/pump/zone/sump; negative or zero volume; `Volume > Capacity` corruption; duplicate `ContainedLiquidSourcePart` on one owner; command with malformed or truncated encoding; outlet cell mutated after menu; two pumps one sump; pump inside the sump's cell neighbourhood; liquid changed to another registered liquid between menu and commit.

### P4

`WorksFreshZoneContainsExactlyOnePumpAndOneSunkenStoreAtPlanCells` (five variations; seeds 64, 224401440, 704860946, 1729 plus a 24-seed sweep); `WorksOtherOwnersIdenticalWithPairRemoved` (rig, strainer, locker, salvage, notice, crops, cuts unchanged; compare blueprint+position sets, never ids); `WorksRoutesDoorAndSalvageReachableWithPair`; `WorksPumpHasThreeOutletsOneRouteAdjacentTwoOffRoute`; `WorksSunkenStoreHoldsExactlyTwoSingleIronSpikes`; `WorksSumpIsBogMireThirtySixOfThirtySix`; `WorksPairOmittedAsAWholeWhenContentUnavailableAndRestOfDistrictSurvives`; `WorksPlanValidAcceptsPairAndRejectsTamperedPayload` (store with oil, wrong volume, locked, pump with extra Part); `WorksCachedZoneNotRetrofitted`; `WorksFixtureSurvivesRelapseUnloadAttempt` (`SoddenDistrict.Retain`); `GameSessionRoundTripThenGetZoneAddsNothing`; existing `SoddenDistrict*`/rig/strainer suites pass with intentional, documented pin edits only.

---

## 7. Canon landmines it must avoid (ids from `lore.md` section 7, rules from section 1)

| Id | Constraint on V1 |
|---|---|
| L3, R7 | Never at Wellmeet or the First Tent; never pumps or pours into a `WellPart` cell; no pricing, metering or selling of water; no fee exists |
| L4 | Never in or near Choir groves, seeps, tendril fens, composting grounds ("Do not dig"); sealing keeps its Cinderhold penalty |
| L5, L6 | Never at the Drowned Ledger; the V1 sump holds tools and no body, specimen or peat feedstock; no Bog-Taken use (`lore.md:260`) |
| L1, R2 | Reader must not say where Cinderhold's "spring the grove wants back" comes from, or explain any protected subject; V1 names no spring |
| L8 | No plaque, niche, Slime-Pool or Sima-Steward adjacency; catacomb zones are E12-rejected anyway |
| L9 | Bog-mire stays bog-mire; no conversion to other materials |
| L16 | Plain craft nouns only (cup, rod, handle, collar, spout, binding); no "regulator", "valve", "motor", "power", "charge" |
| L17, R10 | Every authored string is a draft until it passes the blind voice gate; engine-neutral generated lines carry no faction voice |
| L19 | Lore-loaded liquids (the eight listed in P3) are refused by the allow-list fitting; the pump moves, never produces |
| `lore.md:54` | Cinderhold's pump path has no Choir penalty and the seal path does: V2 must keep that asymmetry and must not become a fifth approach or a second repair authority (`settlement-cinderhold.md:238`) |
| `MACHINE-SYSTEMS-DESIGN.md:45` | A communal fixture is not abandoned because nobody stands beside it: the Works fixtures carry an explicit relinquishing line, as the rig does |

---

## 8. Cross-version note: Cinderhold V2 (design only)

Cinderhold is the pump's authored home but is **wave3**: a physical pump head bound to site `Pump` through a binder Part (the `WellSitePart` `SettlementId`+`SiteId` pattern, reading `SettlementManager` stage live, no second authority), a new authored flooded space and a decision about what the lower level opens, a **seeping** sump when the clay patch relapses (600 turns, needs the elapsed-work policy S15/S16), communal-use terms (S10, Concord idiom), and retention in old worlds (S2). Open owner question carried from `settlement-cinderhold.md:257-260`: whether the physical pump is mandatory for the existing quest (no, additive only) and what the Choir consequence of pumping versus capturing is.

---

## 9. Deferred pitch behaviours, with reasons

| Pitch item | Deferred because |
|---|---|
| Drain a *room* (area level, wet/dry cells) | No depth or room-level model (F5); needs an authored space and a level rule beyond one container |
| Fill a cistern | No refillable cistern and no honest upstream source (F13); Tent-Right forbids |
| Supply a still | The still consumes no liquid; wait for the fractionating still/vat |
| Lower water around submerged machinery | Needs the container gate generalised (spec gap 3) and a flooded machine |
| Special fittings (oil, brine, acid, viscous slurry) | V1 ships one fitting and a visible refusal; each new fitting needs its own canon check |
| Pump natural pools, lakes, bogs, wells | No dry-footing semantics (F4), no protection tags (L1-L8) |
| Continuous or timed pumping, seepage | Needs S15/S16 and the elapsed-time policy; V1 is one atomic stroke per action |
| Fuel, hand-crank variants, noise, power | No canon power source (`lore.md` section 11 item 1); no noise system |
| Corrosion ("Will this fluid corrode my pump?") | No corrosion model; belongs with the assay cabinet |
| Portable pump, player installation, relocation | S19 and the home/base design; V1 hygiene keeps it possible |
| NPC-operated pump, fees, access terms | S20, S10 (Cinderhold V2) |
| Outlet into an existing natural pool ("the bog takes it back") | Works geometry does not allow it (F11); a V1.1 option if a bank-face placement is designed |

---

## 10. Risks, unknowns, owner decisions

Risks (ranked): (1) **no real purpose** unless D1 is accepted, and a weak one even then (4.10, 4.15); (2) touching shipped M2a code (`ReachDepth`, refusal text, an internal context accessor) mitigated by its 56 cases as controls; (3) the closed `Valid`/`Payload` switch can reject the whole Works district; (4) P1 extraction in a shared liquid path (parity oracle first); (5) outlet hazard can trap a careless player's own route (reversible by labour; preview warns); (6) voice/lore gates on every authored string; (7) the standing "no new runtime changes" hold.

[U] settled by named tests: exact free cells in the south shed or yard (P0 probe); whether tile-state projection survives zero-volume natural pools in play (P0 pin; only the shipped docs say so); whether any existing Sodden test pins the exact Works owner set (P4 pin edits); entity-id shift (compare blueprint+position, never ids); `SoddenSella.json` wording for an optional mention [R].

Owner decisions:

- **D1.** Accept a new, optional, finite cache (two iron spikes) as the purpose of V1? If no, park the pump (not "ship a weaker version").
- **D2.** Accept an additive `ReachDepth` field on the shipped `ContainedLiquidSourcePart` (default 12)? Alternative: a separate `SubmergedGatePart`, costing a second hard-wire in `ContainerPart`.
- **D3.** Works first (recommended: precedent, retention, regional voice) or Cinderhold first (stronger story, wave3).
- **D4.** Lift the "no new runtime changes" hold.
- **D5.** Who owns the blind voice gate for every string here.
- **D6.** Whether the pump is ever NPC-operated or priced (default: no, in every version before Cinderhold V2).

Recommended sequencing: do not start before spec Phase A (S1-S8) and S3/S5; the pump is off the critical path to "machines in every town"; schedule it after the first new machine kind (bench, S13b) and before any consumer that needs bulk liquid.

---

## 11. Actual-play acceptance script (fresh world plus cached control)

Fresh seeds 704860946 and 224401440, F12 off, ordinary route Sumphold -> 15.7 -> (dry bow) 16.7 -> 17.7: read the work slip; find the pair; `x` and F1 on both; try reach-through (refused, depth stated); work the pump one stroke to a chosen outlet, observe the lasting patch and step through it (acid, Agility); bail it back with a flask (undo); pump to 12; reach through or pump to 0 and take both spikes; also run the flask-only branch and the leave-it branch; carry the spikes to Cinderhold and use them at the Pump repair (cross-system); travel away and back; F5; fresh Continue; state, puddles and spikes exact. **Cached-save control:** an older save whose 17.7 is already generated shows no pair. Evidence states: glyph presentation only, Linux/Deck unexecuted, CUA-only images, no packaged performance claim, hybrid-versus-pump finding recorded either way.

## 12. Verification record

Read this session [V]: `LiquidVesselService.cs` (whole), `LiquidPoolPart.cs`, `ContainedLiquidSourcePart.cs`, `LiquidVesselPart.cs`, `FiniteLiquidFilmPart.cs`, `WellPart.cs`, `WellDrinkingService.cs`, `CropPourIrrigation.cs`, `CropWateringService.cs` (head), `ContainerPart.cs` (`:30,95-140`), `TakeFromContainerCommand.cs` (`:40-75`), `Zone.cs:825-895`, `TileStateSourcePart.cs` (head), `SoddenDistrictPlan.cs:1-120`, `SoddenDistrictBuilder.cs:30-80,150-200`, `SoddenDistrict.cs:1-70`, `OverworldZoneManager.cs:1236-1250`, `InputHandler.cs:3195-3212,3455-3475`, `Waysites.cs:130-235`, `Diag.cs:115-130`, `PlaceSites.json:100-170`, `Concerns/Cinderhold.json:1-80`, `PlaceConditions.json:505-560`, `LiquidDefinitions/{bog-mire,water,brine,oil,acid}.json`, `Objects.json` (tray, WaterPuddle, MirePool, PouredLiquidPool, Works salvage/locker), `MACHINE-ROSTER-VISION.md`, `MACHINE-SYSTEMS-DESIGN.md` (pump rows and notes), `MACHINE-LIQUID-HANDLING.md`, `MACHINE-CONTAINED-OIL-RECOVERY.md`, `MACHINE-FIELD-WORK-RIG.md` (head), `SYSTEMS-DESIGN-PRINCIPLES.md`, `ARCHITECTURE-SPEC.md` (whole), `lore.md` (sections 0-11), `arch.md` sections 6-10, `settlement-{sumphold,tine,cinderhold}.md` (pump/water lines).

Not read [U]: `MaterialProcessPlan`/service internals; `SoddenSella.json` text; the Sodden district tests' exact owner pins; tile-state behaviour of a zero-volume natural pool in play; hotkey routing of disabled rows (spec [U]). No test, build, Unity run or git write was performed.
