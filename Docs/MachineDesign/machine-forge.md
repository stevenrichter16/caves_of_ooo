# MACHINE V1 DOSSIER: Forge and reclamation rig

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. Lore authority is root `Lore/` only (`Docs/Lore/` unused). Designed against `design/ARCHITECTURE-SPEC.md` (S1-S24, C1-C11). Every authored string below is a DRAFT for the blind voice gate (lore R10).

Evidence tags: **[V*]** I read the cited line this session. **[R]** taken from a research report, not re-read. **[I]** inference or design. **[U]** unverified; the named RED test settles it.

---

## 0. Verdict

**Classification: `wave1-existing-foundations`** for V1 as defined below. Two named parts of the user's pitch are NOT in V1 and are wave2/wave3 (section 5).

One-paragraph thesis. This is the only one of the twelve machines that already runs in the world: a cold `TinkersForge` station plus three finite `ForgeCold` processes (`dismantle`, `reclaim`, `point`) with explicit manifests, shipped as M3a/b/c [V*]. So V1 is not new machinery. V1 is: (a) put that same forge where a verified local gap exists, through the spec's generic installation builder; (b) make the body say what it does; (c) fix three honesty problems the research exposed (section 1, rows F4, F6, F8). The reclamation half of the pitch ("a ruined workshop contains recognizable opportunities") has real purpose in exactly one place (Cinderhold, one find, one copy) and that stays true after V1. I say so plainly and specify the gated V1.1 content step that would change it (section 3.11), without folding it into V1.

Purpose verdict, plainly:
- Forge half: real purpose at Cinderhold and Sill (shipped), a verified local gap at the Last Counter, Tally and Gantry (V1 rows), plausible but thinner at Tine, Slip, Sumphold, Wellmeet, Morrowfast (owner decision), poor or forbidden at the other ten of the twenty census places (section 2).
- Reclamation half: one authored find, one copy, in the world. Both its fractions are cheap to buy wherever a Tinker stands (`TinkerStock` sells `IronSpikeComponent` at 100%) [V*]. Its purpose is therefore a choice and an away-from-town convenience, not a supply need. That was accepted by M3a as a choice, and it is not relitigated here, but it must not be sold as "the machine earns its place".
- Pitch parts with no carrier today: "knowledge improves your options" (tinkering study is disabled: `SchematicPart.DescribeAvailability` says "Tinkering is unavailable in this build" [V*]; `KnowledgePart` is per-NPC [V*]) and "better tools improve your options" (no quote-modifier channel; S14 is unshipped).

---

## 1. Verified ground truth, and corrections to prior premises

| # | Fact | Evidence |
|---|---|---|
| F1 | Blueprints carrying `Forge` are exactly `TinkersForge` and `AbandonedFieldWorkRig`. `ForgePart` declares no public instance field (only `public static EntityFactory Factory`), so it carries no positional authority and is relocatable by construction. | python scan of `Objects.json`; `ForgePart.cs:23-337` [V*] |
| F2 | `TinkersForge` is non-solid, layer 5, glyph `n` colour `&R`, tags `Furniture`, `Weaponcraft`; description "A hearth and work surface share the same worn reach..." There is no Thermal or LightSource Part; Cinderhold pins that shape. The forge is cold. | `Objects.json` TinkersForge; `CinderholdCompositionBuilder.cs:88` [V*] |
| F3 | Forge locations that exist today: Sill has two (square forge via `PlaceCraftingStations`, starting village only, plus the `TheSmithy` stamp); Cinderhold one (workshop rel (5,3)); the glade-ring `TemperingShelter` forge at `Overworld.12.10.0` in fresh worlds (fixed address for exploration version >=9, plus a seeded chance elsewhere); the Peat Works rig. The census in `ARCHITECTURE-SPEC.md` C3 lists only Sill and Cinderhold as `Present`; the wilderness shelter is not a census place. | `VillagePopulationBuilder.cs:63-66,255-260`; `LandmarkBuilder.cs:146`; `CinderholdCompositionPlan.cs:55`; `SpreadExplorationPlan.cs:139-146`; `SpreadExplorationWorksites.cs:49` [V*] |
| F4 | **Process rows live on the carried input item, not on the forge.** `MaterialProcessInputPart.HandleEvent` -> `MaterialProcessActions.Handle` adds rows on the item's `GetInventoryActions`; a plan needs a carried source, `ForgeWorkReceipt.Resolve` (ready forge in the actor's 3x3) and a visible+explored station cell; with no ready forge no row is added and nothing says why. The forge's own menu lists only the craft picker. A player who has not yet carried a find never learns what the forge can do. | `MaterialProcessInputPart.cs:23-59`; `MaterialProcessPlan.cs:42-62,76-77` [V*] |
| F5 | The shipped `ForgeCold` catalogue is three rows. Conservation holds: dismantle 1 tool (wt 5, value 46) -> 1 head (wt 3, 42) + 1 field haft (wt 1, 3); reclaim 1 tool -> 1 iron blank (wt 3, 8) + 1 salvaged timber (wt 1, 3); point 1 blank (3, 8) -> 1 iron spike (3, 8). No row mints weight or value. Registry limits: work kinds `ForgeCold`/`HandCutting` only, at most 2 outputs and 2 extra inputs, `Portable()` requires an Item-tag, takeable, non-solid, footprint-free blueprint. | `MaterialProcesses.json`; `Objects.json`; `MaterialProcessRegistry.cs:48,110-137` [V*] |
| F6 | **The reclaim fraction is dominated by purchase where a Tinker exists.** `TinkerStock` sells `IronSpikeComponent` at chance 100, `OakHaftComponent` 40, `LeatherBindingComponent` 35; a Tinker spawns in 70% of non-Morrowfast villages. Reclaiming trades a 46-value find for 11 value of stock. The point of the branch is availability away from a Tinker, not supply. | `LootTables.json` TinkerStock; `VillagePopulationBuilder.cs:132-134` [V*] |
| F7 | Outputs have real consumers outside the menu. Lasting repairs: Cinderhold pump = 2 `IronSpikeComponent` + 1 `OakHaftComponent` + 1 `LeatherBindingComponent`; Wellmeet pan scales = 2 spikes + 1 binding; Tine weir = 2 timber + 2 oak haft + hatchet; Drowned Ledger survey line = 3 oak haft + 1 binding. Metal gear repair consumes `SteelBladeComponent` (iron is explicitly not a substitute). Pump running gives Cinderhold prices x0.95, a Quartermaster stocking 2 spikes, a Morrowfast-heard remark. One find yields one spike, so it can contribute to but never complete a two-spike repair. | `PlaceSites.json:5-23,59-72,84-146`; `PlaceConditions.json` (Cinderhold.PumpRunning); `Tier1Repairs.json` [V*] |
| F8 | Turn-cost and permission: forge access is physical only. The Weaponsmith's "for a paying customer I'll let you work it" is dialogue; nothing enforces it. Forge verbs and `WeaponSalvagePart` never mention `RentalPart`, so a rented Concord loaner may be quenched at any forge today [grep: no `Rental` in `Weaponcraft/` or `Preparation/`; effect on a loaner is [U]]. | `Shopkeepers.json:8`; grep [V*] |
| F9 | Exactly three blueprints carry `MaterialProcessInput` (`ReleasedExtractionTool`, `RecoveredIronBlank`, `CuredLeatherOffcut`); only `ForgedWeapon` carries `WeaponSalvage`. `CuttingToolPart` is carried only by `Dagger`. | python scan [V*] |
| F10 | `SecondExplorationSites.Retain` already keeps Cinderhold, Tally, Wellmeet, Quillhold, both Peat Works zones, the pen and the abandoned counter zone from unloading, but only in worlds with exploration enabled and version >=15. Of the three V1 towns, none carries a relapse-capable repair site (relapse list in spec section 1 item 8: Sill, Wellmeet, Tine, Drowned Ledger, Cinderhold). | `SecondExplorationSites.cs:12-21` [V*]; spec [R] |
| F11 | The released find is placed fresh-only by the Cinderhold profile (`CinderholdReclamation.CreateFresh`, work rel (12,6)) with no world-version check; `ReleasedExtractionTool` is a takeable, so once taken it is not in the zone. | `CinderholdReclamation.cs:9-20`; settlement-cinderhold.md section 1 [V*/R] |
| F12 | Exterior ground in the three V1 towns is `Grass`/`StoneFloor` (Gantry), `Floor`/`StoneFloor`/`RoadStone` (Tally), `Floor`/`StoneFloor`/`Rubble` (Last Counter) per each builder's validity whitelist, so the spec's default `Ground` list works; a Beating-biome base of `Sand` underneath is [U], so the Last Counter row also lists `Sand`. Each town has a Well at (40,12) inside a protected plaza. | `GantryCompositionBuilder.cs:67`; `TallyCompositionBuilder.cs:64`; `LastCounterCompositionBuilder.cs:68`; settlement dossiers [V*/R] |

Corrections to the spec and research that V1 depends on:

| # | Premise | Correction |
|---|---|---|
| C-a | Spec S7 picks Gantry, Tine, Slip as first forge rows "for mechanism coverage". | Coverage is not purpose. Tine's weir needs oak haft and timber, not a forge; Slip's best fit is the rig variant. V1 ships purpose-qualified rows (Last Counter, Tally, Gantry) and covers Tine/Slip/Sumphold with test-only rows loaded through `InstallationRegistry.InitializeFromJson`. |
| C-b | Spec C2 retention protects "stateful installations". | It cannot protect a takeable find: the entity leaves the zone, so a marker on it retains nothing, and a regenerated zone re-places it. Premise to settle: an old (<v15) world where the Cinderhold pump patch relapses may re-issue the released tool (test F-C2). Irrelevant to V1 rows (a stateless forge is not a finite resource); decisive for V1.1 (section 6, G2). |
| C-c | Spec S13a derives "Works here" from the work-kind table. | S13a is after S12. A minimal, earlier version for the shipped forge only needs an internal registry enumerator and one `IDescribeState` on `ForgePart` (section 3.6). It is subsumed, not contradicted, by S13a. |
| C-d | M3a doc: "ready-forge admission". | The rig also qualifies as a station once repaired and released (`ForgeWorkReceipt.Ready` returns `!IsRig || Payload`), so `ForgeCold` already works at three kinds of body. V1 relies on that only as a cross-check. |

---

## 2. Purpose audit: is there a real reason, place by place

Twenty census places (spec C3: 17 authored villages + 3 catacomb villages). Ratings come from the settlement dossiers, reconciled with the code facts above. "Poor" means no forge purpose; it is not a canon ban, and the town should be claimed by another machine or left as it is.

| Class | Places | Why |
|---|---|---|
| **Present (shipped)** | Sill (2 forges), Cinderhold (forge + anvil + Weaponsmith + the one find) | F3 |
| **V1 rows (purpose-qualified, Open terms)** | **the Last Counter**: its chest sign says "bring the kit to a forge" and the zone has none; the chest's finite kit gives a real re-forge choice (leather binding vs guard lashing; the displaced binding comes back [R: execution plan]). **Tally**: its traders sell all six weapon components and rent loaners yet it has zero forge. **Gantry**: a crossroads with no workshop at all; demand inferred from traffic [R] | settlement-last-counter.md 4.3 row 1; settlement-tally.md 4 row 1; settlement-gantry.md |
| **Owner decision (plausible, weaker)** | Tine (best of the surface villages; no local finds), Slip (rig variant; FireClay and Tinker stock local), Sumphold (its furnishing builder forbids `ForgePart`, so needs its own builder), Wellmeet (strong only as a given-not-sold portable rig; `Gift` terms), Morrowfast (strong, but scene-authored, local non-canon, needs a manifest and a permission rule) | dossiers |
| **Poor (do not place a forge)** | Posy, Marrowstye, Quillhold, the Salt-Vault, the Drowned Ledger | no metal, salvage or local forge problem; some dossiers name a better-fitting machine (Marrowstye/Salt-Vault: treatment or steeping bench) |
| **Exempt by canon (fire kinds)** | the First Tent, the Quiet's Door, Olderdeep, Lampwell, Spivenor (also Deepest Cathedral, forbidden outright, not a census village) | spec C3 `Exemptions`; lore L4, L8; `08_MaterialCulture.md:77` |

So the forge covers 5 of 20 census places in V1 and at most 10 with owner decisions. The other ten must be claimed by other machines or exempted; this dossier does not pretend a forge makes them "have a machine".

**What must exist before the pitch is honoured** (not in V1): a second authored find class with a Find placement kind (section 6, G2); an independent economic and canon critique per find; a knowledge carrier; a quote-modifier channel for "better tools"; heat and fuel for any melting claim; a payload policy before ordinary broken equipment can be a find.

---

## 3. V1 definition

### 3.1 Scope

V1 = three increments that can each be reverted by deleting rows or one line:
- **V1a Findable forge.** Three installation rows (Last Counter, Tally, Gantry) for the existing `TinkersForge`, placed by the spec's `InstallationPostBuilder` (priority 4650), `Retention: None`, Open access, optional (a town that cannot fit one is simply unchanged).
- **V1b Legible forge.** The forge reader gains a "Works here" line derived from the registry; the installation marker adds the spec's neutral line.
- **V1c Honesty fixes (test-only, no gameplay change).** Characterization tests pin: conservation of every `ForgeCold` row; the silent no-forge menu; the rented-loaner behaviour; the Cinderhold re-issue premise. Each can decide a follow-up, none changes play.

Not in V1: any new find, any new blueprint, any process row, heat, fuel, jobs, terms or fees, native art, a Find placement kind.

### 3.2 Rows (data, spec C3 schema)

```json
{ "Id": "last-counter-square-forge", "Place": "the Last Counter", "Blueprint": "TinkersForge", "Retention": "None",
  "Near": ["Well"], "Radius": 9, "MaxProbe": 48, "Ground": ["Floor", "StoneFloor", "Sand"], "Revision": 1,
  "Purpose": "The post's own sign sends travellers with a kit to a forge; this makes that branch usable beside the finite consignment chest.",
  "Alternative": "Assemble one weapon from the pack as the sign says, process away from any station, or carry the kit to the Cinderhold or Sill forge. Nothing requires the forge.",
  "Basis": "settlement-last-counter.md 4.3 row 1 (strong); lore.md section 5 Concord O; owner sign-off for Open terms" },
{ "Id": "tally-exchange-forge",  "Place": "Tally",  "Blueprint": "TinkersForge", "Retention": "None", "Near": ["Well"], "Radius": 9, "MaxProbe": 48, "Ground": ["Floor","StoneFloor","Grass"], "Revision": 1,
  "Purpose": "The exchange sells every weapon component and has no forge; assembled-and-re-forged weapons can be made where the parts are bought.",
  "Alternative": "Pack assembly needs no forge; the Sill and Cinderhold forges remain; buying a finished weapon remains.", "Basis": "settlement-tally.md 4 row 1 (strong); owner sign-off for Open terms" },
{ "Id": "gantry-forecourt-forge", "Place": "Gantry", "Blueprint": "TinkersForge", "Retention": "None", "Near": ["Well"], "Radius": 9, "MaxProbe": 48, "Ground": ["Grass","StoneFloor","Floor"], "Revision": 1,
  "Purpose": "A crossroads with no workshop; finds and worn gear pass through it.",
  "Alternative": "Carry intact to Cinderhold, sell to the Merchant, or assemble in the pack.", "Basis": "settlement-gantry.md forge row (strong, demand inferred and to be proven in play); owner sign-off" }
```

Notes: `Retention: None` is correct because `TinkersForge` carries no state-bearing Part (E07 passes). None of the three towns is relapse-capable (F10), so regeneration cannot even occur; Tally is also a retained destination in v>=15 worlds. Placement is exterior and deterministic; a town that cannot fit emits `worldgen/InstallationOmitted` and stays as it is (spec failure table). The forge is non-solid, so route and standing-cell checks are vacuous (spec item 12).

### 3.3 Player verbs (exact)

| Verb | Where it appears | Command id | Turn cost | Notes |
|---|---|---|---|---|
| Look / Examine the forge | `x` reader and F1 selected-action reader | none | free | V1b adds "Works here" lines |
| Craft (one button; sectioned picker with pick-marks: Heads/blades, Hafts, Bindings, Quenches, Weapon) | forge world-action menu | `CraftKit` -> `ForgeWeapon`, `ReforgeWeapon`, `QuenchWeapon` | one action per committed command (legacy OR-chain in `InputHandler`) | `ForgePart.cs:34-83,154-211` [V*]; unchanged |
| Forge a batch | non-menu callers | `ForgeWeaponBatch` | one action | unchanged |
| Salvage a forged weapon | menu of a carried `ForgedWeapon` | `SalvageForgedWeapon` | one action | returns recorded head and haft; binding and improvements lost; `WeaponSalvagePart.cs:13,32-37` [V*] |
| Infuse an item with a mineral | menu of a carried mineral | mineral prep command | one action | needs a ready forge; unchanged |
| Dismantle tool / Reclaim iron and timber / Dress iron point | menu of the carried find or blank, with a ready forge within one step | `MaterialProcess|<id>|<station>|<token>` | one ordinary action (`ActionCost => 1`); cancel and inspect free | shipped; V1 only adds second locations |

Hand-cutting rows (`cut_binding`, `guard_lashing`, `serrated_kit`) share the registry but need no forge; they are not forge verbs and "Works here" does not list them.

### 3.4 Finite inputs, outputs, units, loss

| Process | In (units) | Out (units) | Loss and destination | Weight / base value |
|---|---|---|---|---|
| `dismantle` | 1 `ReleasedExtractionTool` | 1 `CinderhookAxeHeadComponent` + 1 `FieldHaftComponent` | "old fastening spent" (destroyed, no item) | 5/46 -> 4/45 |
| `reclaim` | the same 1 tool | 1 `RecoveredIronBlank` + 1 `SalvagedTimber` | "shaped workmanship and old fastening lost" (destroyed) | 5/46 -> 4/11 |
| `point` | 1 `RecoveredIronBlank` | 1 `IronSpikeComponent` | none | 3/8 -> 3/8 |

All outputs enter the pack through the shared transfer snapshot; there is no waste item and no ground spill (S17 receivers are deferred). Unit = one authored item; the blank is "a work blank, not an ore grade or a liquid measure" (blueprint text). `ReleasedExtractionTool` has Stacker MaxStack 20 but exactly one exists in a world.

### 3.5 Cost

One ordinary scheduler action per committed craft or process. No fuel, no heat, no fee, no elapsed time, no maintenance. Cancelling, reading and leaving unstarted work are free. Placement costs nothing at runtime (generation time only).

### 3.6 Preview and readers

- Process rows (shipped): "`<ActionText>: <inputs> -> <outputs>; <LossText> (1 action, pack)`" built from the registry, plus the shared bounded selected-action preview (reserve 6 choices, 73 columns x 4 lines) [V*: `MaterialProcessInputPart.cs:43-46`; M3a doc].
- **New (V1b), forge body reader**, via S5's `IDescribeState` implemented on `ForgePart` and an internal `MaterialProcessRegistry.ForWorkKind("ForgeCold")` enumerator (about 6 lines; not a loader):
  `Works here: dismantle tool; reclaim iron and timber; dress iron point.` (each phrase is a shipped `ActionText`; the only new string is the fixed prefix, voice-gate pending). Updates automatically if rows change; never lists `HandCutting` rows.
- Installation line from the spec: "Set here to be used." (draft; voice gate).
- Not shown in V1: a fee or access line (terms are S10), a disabled-row reason (S12), a heat or fuel line (the forge has none).

### 3.7 Blocked, failed, interrupted, saved

| State | Behaviour | Evidence |
|---|---|---|
| No ready forge within one step, or station not visible+explored | no process row; silent (honest limit F4; S12 adds `no-station`) | `MaterialProcessPlan.cs:50-51` [V*] |
| Source or output custody changed between preview and commit; callback moves the forge; actor dead or action-blocked | refuse with nothing spent; `event/PreparationRejected { reason: "process_unavailable" }` | `MaterialProcessService.cs:68-79,123-136`; `PreparationActions.cs:38` [V*] |
| Output would not fit | refused before payment; rollback by `InventoryTransferSnapshot.Restore` | `MaterialProcessService.cs:95-122` [V*] |
| Interrupted | no job exists; nothing is pending to resume; a half-done state cannot exist | no elapsed work in V1 |
| Placement cannot fit / content missing / receipt mismatch | town unchanged, `worldgen/InstallationOmitted` | spec failure table |
| Saved | pack items and the placed forge persist with the cached zone; marker Part is additive; `FormatVersion` stays 8 | spec C1, C2 |
| Old world, town already visited | no forge appears, ever (stated limit, not a defect) | spec C3 |

### 3.8 Alternatives the player keeps

1. Leave the forge alone. Nothing in a town requires it; the sign, chest, shops, repair and trade all work without it.
2. Assemble a single weapon in the pack from carried components with no workshop (ordinary pack crafting stays) [V*: field-rig doc text].
3. Sell or carry the find intact (about 16 drams quoted at the same tinker; M3a evidence) or buy finished components at a Tinker/Weaponsmith.
4. Hand-cutting processes need no station at all.
5. Carry the find to another ready forge (Sill, Cinderhold, the glade-ring shelter, a repaired and released Peat Works rig).
6. For repairs: patch with clay, seal, or leave the structure broken; the pump has a "the lads bail" refusal [R].

### 3.9 Consequences outside the menu

- A reclaimed `IronSpikeComponent` (plus a bought haft and binding) is a lasting-repair input for the Cinderhold pump and Wellmeet scales; a running pump changes Cinderhold prices (x0.95), Quartermaster stock, concern rewards (2200 XP, 50 drams) and a remark heard in Morrowfast (F7).
- A weapon forged, re-forged or quenched at a new forge enters combat; the M3a branches were verified with an equipped Cinderhook axe and an iron-point weapon against a real scrabbler [R].
- At the Last Counter, a placed forge makes the chest sign's second branch real and lets the finite consignment (counterweight long blade, oak haft, leather binding, guard lashing) be re-forged to compare bindings, which pack assembly cannot do [R: chest contents `LastCounterCompositionBuilder.cs:86` seen in a grep line, V*; displaced-binding return from the execution plan, R].
- A placed forge changes only its own cell: non-solid, no route change, no Thermal or Light, so it cannot ignite cloth or brush walls.

### 3.10 Actual-play acceptance for V1a/V1b (fresh world plus cached control)

Fresh seeds 64 and 704860946: walk to the Last Counter, Tally and Gantry; Look and `x` the forge (reader shows "Works here"); at Tally buy components and forge/re-forge at the forge; carry a Cinderhold find (earned checkpoint) to the placed forge and run `dismantle`, F5, fresh Continue; verify position and marker persist. Cached control: an older save with Gantry already visited has no forge. Evidence states: glyph presentation only (placed forges draw as `n &R`; native art reaches only admitted zones), Open access, Linux/Deck unexecuted.

### 3.11 V1.1 (gated, not V1): a second find with three exclusive fractions

Recorded so the owner can decide, not scheduled. Three rows over one source need no engine change (`MaterialProcessInputPart.ProcessIds` lists any number; the menu loop is `foreach(id in ids.Distinct())` [V*]). Criteria for the find: authored finite manifest; explicit Concord "written off" style release; each fraction maps to a different real consumer (a weapon workmanship, a repair pair such as 2 spikes, a gear-repair `SteelBladeComponent` that the source itself declares, never converted from iron); sum of output value and weight at most the source; one copy; in a place with a forge within reach (Peat Works shed with its rig, or a V1 town); not a clone of M3a. Needs G2 first, plus independent economic and canon critique per find (M3a took 294 native checks).

---

## 4. Canon

**Names (drafts, intent card plus blind gate required).** Keep the shipped display name "tinker's forge" (surface/Tinker voice) and the shipped "anvil". The reclamation function has no new in-world name in V1; its rows are already the shipped phrases and the item says "Written off and released for salvage; no outstanding claim". Candidates for later Concord-voiced surfaces: "released-goods bench", "write-off bench"; "salvage bench" is register-safe anywhere; catacomb "cold-dressing stone" and Tent-Right "lent rig (given)" are not V1 (those places are exempt or deferred). Do not use "reclamation rig" in player text at Concord places (lore.md M1), and no technology nouns (regulator, motor, alloy; L16).

**Operator roles.** None required: V1 forges are Open communal fixtures, like the Sill square forge. Where present, the natural keepers are Tinker (70% of villages), Weaponsmith (Cinderhold; Sill smithy), Armorer, ConcordFactor (release paperwork, Cinderhold), Quartermaster (rentals). No new role. A bound operator is S10's `OperatorBlueprint` and is out of V1.

**Landmines to avoid (lore.md ids).**
- L4 / R6: no forge on Choir ground; Deepest Cathedral forbidden; no burning (the forge has no heat, which is also why it needs no Choir exemption beyond the placement list).
- L8 and the lore.md M1 feedstock list: no bodies, preserved specimens, `PreFellingBody`, plaques, Memory-Marble, Tepuibone walls, ancestral bone, Bower installations, Choir-grown structures, guest-cloth pole or well. Structurally excluded because the registry names exact portable source blueprints and `Portable()` rejects creatures, terrain, solid or footprint blueprints [V*].
- L9 / R8: iron stays iron; no iron to steel; Choir-Iron never enters the registry (iron blank "is iron, not steel").
- L13: Choir-Iron in Spore-Wedded smithing is unresolved; Spivenor is exempt and no Choir-Iron process exists.
- L15: do not copy "free to take" (tray) into Concord-owned text; the placed forge uses "Set here to be used."
- L17: no shipped machine string has a recorded blind-gate pass; V1 adds two draft strings, both gated.
- L18: forge permission remains dialogue-only; V1 does not pretend otherwise (section 8).
- R7 / R9: no Tent-Right placement in V1 (no water pricing, no oath automation); no standing arrangement, fee or contract, so no "spoken end" is owed.
- L14: `Docs/Lore/` unused.

---

## 5. Deferred pitch behaviours

| Pitch behaviour | Why not V1 | Wave and prerequisite |
|---|---|---|
| "Intact regulator, several plates, or a small amount of rare alloy; recovering one sacrifices the others" (3-way) | needs a second find; existing find is a 2-way. Canon: no "regulator"/"alloy"; use real materials (lore.md section 3) | V1.1 content on spec G2; no engine change |
| "Knowledge improves your options" | no carrier: tinkering study disabled, `KnowledgePart` per-NPC | wave3; owner picks the knowledge store |
| "Better tools improve your options" | needs a quote channel that can raise a bounded output; `HandCutting` is a carried-tool precedent only | wave2: S14 plus a tool-capability row |
| Heat, fuel, melting, alloying, ore | forge is cold by pinned shape; M3a explicitly excludes them | wave3: new thermal and fuel rules |
| Elapsed processing, jobs, receivers, waste items | no job system; spec S15-S17 | wave2 |
| Terms, fees, operator-run forge ("the smith works it for you") | access seam is inert; S10 gated by independent critique | wave2: S9-S11, S20 |
| Ordinary broken equipment as finds (repair vs dismantle) | needs payload policy for equipment, equipped-input and save policy; CLAUDE.md names these as critique-gated | wave3 |
| Pre-Felling ruin finds | parts must stay un-explained (lore.md section 3, R2/R3) | owner and lore review |
| Portable or home forge | `InstallablePart` (S19); the rig already shows haul, refit and relocate | wave2 with home/base |

---

## 6. What the machine needs that the spec lacks

| # | Gap | V1 impact | Proposal |
|---|---|---|---|
| G1 | No early "Works here" source: S13a is after S12. | V1b | internal `MaterialProcessRegistry.ForWorkKind(string)` plus `ForgePart : IDescribeState`; S13a later replaces the source with the work-kind table. |
| G2 | **No Find placement kind and no retention for a finite takeable.** E14 rejects Takeable; the builder rejects entities with container-like Parts; C2's marker cannot protect an item that leaves the zone. | V1.1 only | `Kind: "Find"` rows (takeable authored item, one per spec, bare ground, off routes) and zone retention keyed by the spec row (retain a place whose rows include a finite Find), not by a surviving entity. |
| G3 | No "do not put a second station of the same kind within reach" admission; `ForgeWorkReceipt.Resolve` returns the first ready forge in scan order, so two forges within one cell are ambiguous. | V1a | admission clause: no `IStationCapability` of the same kind within Chebyshev 2 of the chosen cell. |
| G4 | No per-row minimum distance from the anchor. Tally's `ConcernWorld` lands documents on the first open cell in rings from the centre; keep forge at Chebyshev >=3 from (40,12). | V1a, Tally | optional `MinDistance` int, or rely on 4650 running after document placement [U]; test F-A9. |
| G5 | Rented loaners at a Concord forge: no spec statement. | V1a at Tally/Last Counter | premise test F-C3, then pin or refuse (owner). |
| G6 | S7's mechanism-town list conflicts with purpose. | V1a | ship purpose rows, test mechanism with test-only rows (C-a). |

---

## 7. Slices and RED-first tests

Dependencies: V1a needs spec S1, S2, S6, S7; V1b needs S5; V1c needs nothing (test-only). V1a can be rows-and-tests only once S7 exists. S2's retention conjunct is not exercised by V1 rows (all `None`), but the marker Part is.

| Slice | Content | Depends on | Cycles |
|---|---|---|---|
| F0 | Owner decisions: the three rows and Open terms; voice gate for two draft strings; settle premises | none | 0 |
| F-C (V1c) | Test-only characterization (below) | none | 1 |
| F-A (V1a) | Three rows in `Installations.json` plus tests | S1, S2, S6, S7 | 1 + actual play |
| F-B (V1b) | `ForgePart : IDescribeState` plus registry enumerator plus reader tests | S5 | 1-2 |
| F-D | Fresh-world and cached-control actual play | F-A, F-B | play |
| V1.1 | Find kind, retention by row, one second find (gated) | G2, critique | later |

Test names, with expected first state (RED = fails before code; CHAR = green on HEAD, characterization).

**F-C (test-only):**
- F-C1 `EveryForgeColdRow_NeverMintsWeightOrValue` (CHAR; sum of output weight and base value <= source for all three rows; the lint that every future row must pass).
- F-C2 `ReleasedToolNotReissuedAfterRelapseRegeneration_Premise` (premise [U]; old-world Cinderhold; may be RED, then an owner decision).
- F-C3 `QuenchRentedLoaner_Control` (CHAR; records what happens today: F8).
- F-C4 `ProcessMenu_WithoutReadyForge_ShowsNoRow_Characterization` (CHAR; pins F4 so S12 flips it deliberately).
- F-C5 `ForgeColdWorksAtAnyReadyForge_SecondZone` (CHAR; test forge placed in a Gantry-shaped zone runs `dismantle`, `reclaim`, `point` with one action each).
- F-C6 `Registry_Validate_RejectsCreatureTerrainSolidFootprintSources` (CHAR).

**F-A (placement):**
- F-A1 `ForgeRows_PlaceExactlyOneTinkersForge_AtDeterministicCell` (RED; seeds 64, 704860946, 1729 plus 24-seed sweep, per town).
- F-A2 `PlacedForge_IsReadyStation_ResolveAndNearAgree` (RED; adjacent actor resolves it; shape pin: non-solid, no LightSource or Thermal).
- F-A3 `NoSecondForgeWithinChebyshev2OfPlacedForge` (RED; G3; counter: Sill's two legacy forges unchanged).
- F-A4 `OtherOwnersIdenticalWithRowRemoved_ExceptForgeAndMarker` (RED; compare blueprint and position sets, never IDs).
- F-A5 `RngUntouched_RandomPopulationIdentical` (RED).
- F-A6 `ForgeKindRowsExistOnlyInQualifiedPlaces` (RED; allowlist constant that changes only with sign-off; exempt and poor places have zero rows).
- F-A7 `ForgeRowRetentionIsNoneAndBlueprintIsStateless` (RED; E07) and `ForgeRowAlternativeAndPurposeNonEmpty` (RED; E09).
- F-A8 `VisitedTownGainsNoForge_CachedControl` (RED).
- F-A9 `DocumentsAndGuestsLandOffForgeCell_Tally` (RED; G4).
- F-A10 `CompositionTestsStillPass_WithRowsOn` (existing Gantry, Tally, Last Counter, Cinderhold suites, exact counts of Envoy, Sign, Campfire, Chest etc. unchanged).
- F-A11 `PlacedForge_SaveRoundTripKeepsPositionAndMarker` (RED).
- F-A12 `PlacedForge_ProcessesCarriedFind_OneActionEach_OtherCellFails` (RED; counters: two cells away, other zone, rig fouled).
- F-A13 diag pins `worldgen/InstallationPlaced`, `InstallationOmitted` (RED; spec).

**F-B (reader):**
- `ForgeReader_WorksHere_ListsForgeColdActionTexts` (RED).
- `ForgeReader_WorksHere_ExcludesHandCuttingRows` (RED; counter).
- `ForgeReader_WorksHere_FollowsCatalogue_NotHardcoded` (RED; inject a test row, reset after).
- `ForgeReader_QuickAndFullDocumentBothShowIt` (RED; parity with S5).
- `ForgeReader_OtherBytesUnchanged` (CHAR golden).
- `ForgeReader_IsReadOnly` (RED; no events, no diag).
- `ForgeReader_AuthoredTextWhitelist` (RED; only fixed prefix plus registry `ActionText`).

Adversarial gate (spec taxonomy): state atomicity and rollback (placement receipt mismatch; process callback moves forge), cross-zone (two towns, one spec), parser/loader (spec E01-E14), probability/RNG (no rng consumed), anti-exploit (a find or container blueprint named as a forge row; a rented loaner), diag dispatch, save reach.

---

## 8. Risks, owner decisions, honest limits

Owner decisions:
1. Lift "no new runtime changes" (CLAUDE.md) and confirm V1 scope; confirm the three rows and Open terms at Concord towns (the Weaponsmith's "paying customer" line stays unenforced; terms come with S10/S11).
2. Whether Tine, Slip, Sumphold, Wellmeet, Morrowfast get forge or rig rows later.
3. Whether the second find (V1.1) is wanted, and which place hosts it.
4. Rented loaners at a Concord forge: pin as allowed or refuse (F8, G5).
5. The knowledge carrier for the pitch's "knowledge improves your options".

Risks, ranked: (1) placement fit in composed towns with reserved plazas and lanes (omitted with a diag; census and test fail loudly); (2) Open Concord forges look like ignoring Concord idiom (L18); (3) V1a is purpose-thin at Gantry (rating "strong, demand inferred"); (4) rented-loaner exposure grows with Concord forges; (5) glyph-only presentation.

Honest limits to state in every evidence record: forge is cold and has no heat or fuel; placed forges draw as the red `n` glyph (four stations share `n`; colour distinguishes); discovery is by Look, `x` and F1; processing in new towns needs a find carried from Cinderhold (only one exists); the reclaim fraction is a choice, not a supply; old worlds' visited towns never gain a forge; entity IDs shift by one at placement; Linux and Deck unexecuted.

---

## 9. Home/base bridge

`ForgePart` is field-free and holds no zone or cell, so the same blueprint plus an `InstallationMarkerPart { Origin = "Player", RetainZone = true }` is a player-built forge with no new Part (spec section 9 hooks B1, B3). `ServiceTermsPart.Access = "Owner"` is where a home forge's permission lives. The rig is the only existing "installed vs loose" machine (haul, refit, relocate across one border; M1b/M1c) and is the model for `InstallablePart` (S19). Nothing in V1 stores a station coordinate, so nothing blocks later player placement. `ForgePart.Factory` is a static bootstrap wiring, set unconditionally; it does not block placement or relocation.

---

## 10. Verification record

Read this session [V*]: `ARCHITECTURE-SPEC.md` (all 738 lines); `Docs/MACHINE-ROSTER-VISION.md`, `MACHINE-SYSTEMS-DESIGN.md`, `SYSTEMS-DESIGN-PRINCIPLES.md`, `MACHINE-RECLAMATION-PROCESSING.md`, `MACHINE-FIELD-WORK-RIG.md` (full); `MACHINE-SPURGRASS-KIT.md` and the spurgrass source selection (head); `ForgePart.cs`, `ForgeWorkReceipt.cs`, `WeaponSalvagePart.cs`, `MaterialProcessRegistry.cs`, `MaterialProcessPlan.cs`, `MaterialProcessService.cs`, `MaterialProcessInputPart.cs`, `CinderholdReclamation.cs`, `CuttingToolPart.cs`, `SchematicPart.cs`, `KnowledgePart.cs`; `MaterialProcesses.json`; `PlaceSites.json`, `PlaceConditions.json`, `Tier1Repairs.json`, `LootTables.json` (forge-relevant tables), `Shopkeepers.json:1-30`; python scans of `Objects.json` (Forge, MaterialProcessInput, WeaponSalvage, CuttingTool, Repairable, Harvestable owners; component stats; the thirteen blueprints named in section 1); `VillagePopulationBuilder.cs:55-70,125-140,250-262`; `LandmarkBuilder.cs:140-150`; `SpreadExplorationPlan.cs:125-160`; `SpreadExplorationWorksites.cs:1-120`; `SecondExplorationSites.cs:1-40`; `AreaCompositionScope.cs`; builder whitelists in `LastCounter`, `Tally`, `Gantry` composition builders; `DoorPart.IsBareGround`; `PreparationActions.cs` diag lines; research reports `lore.md` (sections 0-12), `presentation.md` (findings, sections 6, 8), `enumerate.md` (sections 0-3), and the forge rows of all 25 `settlement-*.md` dossiers.

Not read, each settled by the named test: exterior ground at Tally and Last Counter under reserved plazas (F-A placement RED); whether `ConcernWorld` documents are placed before or after priority 4650 (G4); whether forge verbs affect a rented loaner (F-C `QuenchRentedLoaner_Control`); whether the Cinderhold release re-issues after relapse in old worlds (F-C premise); how Tally/Last Counter late proofs treat an added owner (F-A `CompositionTestsStillPass_WithRowsOn`); `TemperingShelter` shelter behaviour beyond the placement spec.

No test, build, Unity run or git write was performed.
