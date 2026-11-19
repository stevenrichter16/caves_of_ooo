# CRITIQUE (lens: feasibility and false-premise sweep) of ARCHITECTURE-SPEC.md, PLACEMENT-PLAN.md and the machine dossiers

Status: independent read-only critique. No project file, Unity run, test, build or git write was made. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9` plus a dirty working tree. Evidence tags: **[V]** I read the cited line in this pass. **[R]** taken from a report. **[I]** inference. Lore authority: root `Lore/` only.

## 0. Verdict

**Accept with changes.** The spec's engineering contracts (C1-C6) are largely grounded: of ~45 code claims I re-read, ~38 hold exactly (section 3). The defects are concentrated in three places:

1. **The Wave 1 placement model cannot reach the sites the placement plan names** (P1-1). The plan's acceptance gate ("placed cell lies in the dossier window across all plan variants") is unpassable with `Near` + `Radius` + `MinRadius` rows. What actually ships is "a free cell in the ring `MinRadius` around the Well at (40,12)", for every town.
2. **Two authoritative inputs disagree on what S7 writes into shipped data**, and the working tree already follows the older one (P2-1).
3. **Several slice premises are partly false** (S9 scope, S12 shape, S3 golden and file list, the S2 critical-path dependency) (P2-2 to P2-6).

No save-corrupting premise was found in the contracts. One test-assembly list is incomplete (`BrainPart`), and the working tree has already corrected it.

Working tree note [V]: `Assets/Scripts/Gameplay/Installations/{InstallationMarkerPart,InstallationPostBuilder,InstallationRegistry}.cs`, `Installations.json`, `SavedPartContract*`, five `Installation*Tests.cs` files and one added `CanUnloadZone` conjunct plus one `CreateVillagePipeline` line in `OverworldZoneManager.cs` (diff at 1162-1165, 1247-1248) are untracked or modified. They implement S1, S2, S6 and the S7 builder. Findings below that touch those files are advice to the implementer, not accusations: the files follow the spec faithfully.

## 1. Corrections table (premise -> verdict -> fix)

| # | Where | Claim | Verdict | Evidence | Fix |
|---|---|---|---|---|---|
| C1 | Spec C1 (custom list) | custom = `ISaveSerializable` or {StatusEffects, BitLocker, Inventory, ActivatedAbilities, Body} | **Incomplete.** `BrainPart` also has hand-written save/load | `SaveSystem.cs:1634,1661,1927,1973` [V] | add `BrainPart` (the working-tree reader already did: `SavedPartContractReader.cs` `CustomSerialized`) |
| C2 | Spec section 1 item 2, C5 | mineral preparation and brewing "use only a presence check" | **Half false.** Infusion uses `ForgePart.IsNearForge` = `ForgeWorkReceipt.Resolve != null`, i.e. strict readiness, just not claimed through the transaction. Only the still is presence-only. | `MineralPreparationPart.cs:28`, `ForgePart.cs:300`, `AlchemyStillPart.cs:104-131` [V] | reword; S9's real gap for infusion is "readiness proven but station not retained through callbacks" |
| C3 | Spec S9 and adapter table | `WeaponSalvagePart` and the forge verb commands move "from presence/Near to Begin" | **False for salvage; partly false for verbs.** Salvage already calls `ForgeWorkReceipt.Begin`; forge verbs call `Begin` when `Zone != null` (batch or selected station only for `ForgeWeapon`) | `WeaponSalvagePart.cs:26-27`; `ForgeWeaponCommand.cs:102`; `ReforgeWeaponCommand.cs:77`; `TemperWeaponCommand.cs:66` [V] | shrink S9 to `MineralPreparationPart`, `BrewReagentsCommand`, `PreparationRecipePart` (still recipes), plus the unselected single-forge `ForgeWeapon` path; run each "RED on HEAD" claim as a probe first, because several may already be GREEN |
| C4 | Plan 4.1 (all three rows) | `Near` + `Radius` + `MinRadius` reproduce "the supply forecourt", "the goods-store apron", "the exchange forecourt pad" | **False.** See P1-1 | `VillageBuilder.cs:165-177`; `InstallationPostBuilder.cs` candidate sort; settlement dossiers | P1-1 |
| C5 | Spec C3 failure table | "pipeline retry ... places again identically" | **False as stated.** Retry re-seeds `rng = new Random(rng.Next())` (pipeline `Generate`) and every rng-driven prop/NPC differs, so `Fits` outcomes and the chosen cell can differ. Identical-cell holds for relapse regeneration only (same world seed) | `ZoneGenerationPipeline.cs:36,48,63,69` [V] | S7 test `RetryReplacesOnceAtTheSameCell` should assert "exactly one, valid cell", not "same cell"; keep the same-cell assertion for the relapse test only |
| C6 | Plan section 0 item 5 / rows | "every Beating row needs `Sand` in `Ground`" | **False for the Last Counter.** Its composition lays `Floor`/`StoneFloor`/`Rubble` only. `Sand` is Wellmeet/First Tent (`ConnectivityBuilder.FloorBlueprint`) | `LastCounterCompositionBuilder.cs:68,81`; `OverworldZoneManager.cs` pipeline `FloorBlueprint=(wellmeet\|\|firstTent)?"Sand":"Floor"` [V] | per-row grounds from each builder's `Valid` whitelist; drop `Sand` from the Last Counter |
| C7 | Spec C3 / S7 dependency chain | critical path `S1 -> S2 -> S6 -> S7` | **S7's three shipped rows do not need S2's retention.** Gantry, Tally and the Last Counter have no relapse-capable site (`RelapseTurns` exists only for Tine 29, Wellmeet 79, Drowned Ledger 109, Cinderhold 143). S7 needs only the marker Part (replay guard). | `PlaceSites.json:29,79,109,143` [V]; `SettlementSiteDefinitions.IsTrackedVillage` (Sill, Wellmeet only) [V] | split S2 into S2a (marker Part, S7 prerequisite) and S2b (retention conjunct; prerequisite of the first state-bearing or player row) |
| C8 | Spec S7 vs Plan 4.1/4.2 | S7 ships data rows Gantry, **Tine, Slip** | **Conflict.** Plan ships Gantry, **Tally, the Last Counter**; Tine and Slip are test-only, Slip declined. Working tree `Installations.json` has the spec's three rows. | `Installations.json` (3 rows) [V]; Plan 4.2 table | P2-1 |
| C9 | Spec S12 | pooling "for the source and extra inputs" | **Only extra inputs matter.** All six shipped rows have source `InputCount 1`; `serrated_kit` source is `KnotflaxCord`, the **spines are the extra input** (`AdditionalInputCounts [2]`) | `MaterialProcesses.json` rows [V]; `MaterialProcessPlan.cs:107-120` [V] | narrow S12 to extra-input pooling; drop source pooling |
| C10 | Spec S3 file list | rows consumed by two menus | **At least five consumers.** Also the world-action details reader (copies a row without the new fields), the controller menu, and underfoot rows | `InputHandler.cs:4695-4699`; `InputHandler.Controller.cs:179,195-196`; `WorldInteractionSystem.cs:301,328` [V] | add to S3's file list and RED set |
| C11 | Spec S3 `SelectAction` guard | hotkey routes through `SelectAction` | **True, with a side effect.** `SelectedActivationKey = key` is assigned before `SelectAction(i)`; a disabled-row return leaves a stale key that a later real selection reads (`_worldActionKeyToRelease`) | `WorldActionMenuUI.cs:262-269,275-281`; `InputHandler.cs:3098` [V] | clear `SelectedActivationKey` on the disabled path; add a test |
| C12 | Plan 8 / spec S1 | golden is a repo file | **Process hazard.** The golden captures every compiled Part in the working tree; generated from a dirty tree it freezes WIP Parts and fails on a clean isolated clone | `SavedPartContract.txt` is 385 Parts [V] | S1 doc: regenerate only in the isolated clean clone at HEAD |
| C13 | Plan W2-B | `Fee` at the Last Counter "with the Envoy as present operator" | **Geometrically infeasible as drafted.** The Envoy stands inside the supply post at `(post.X+6, post.Y+4)`; forecourt rows are `Y+9..Y+13`; `OperatorWithin` defaults to 3 | `settlement-last-counter.md:77`; spec C7 `OperatorWithin = 3` | raise the bound and require line of sight, or place the station at the post (needs in-room anchors, S24) |
| C14 | Spec C3 Radius cap | `Radius` 1..12 | **Cannot reach Tally's preferred apron** (x50-58 is distance 10-18 from the Well at (40,12)) | `settlement-tally.md:128,152` [R] | P1-1 |
| C15 | Plan section 2 `MinRadius` | "a row that must keep Chebyshev 3 clear sets `MinRadius: 3`" | **Ambiguous.** Tally's dossier admits distance >= 3 (`settlement-tally.md:132`); "keep 3 clear" would mean `MinRadius 4` | -- | define `MinRadius` as inclusive minimum distance in the spec; one boundary test |
| C16 | Spec S7 acceptance | "use one `ForgeCold` process at it" | Needs a `ReleasedExtractionTool` or `RecoveredIronBlank`; the only authored find is Cinderhold's. In a fresh world at Gantry this is not reachable by play | `machine-forge.md` F4, F9 [R] | acceptance uses a fixture-seeded input, and says so |
| C17 | Canon cite | `Lore/Factions/07_TentRight.md:119` | off by one: the quote is line 118 | `07_TentRight.md:118` [V] | fix both docs; also record that "exempt" is an inference from a descriptive sentence, not a prohibition |
| C18 | Plan Wave 1 needs S5 | spec critical path excludes S5 | inconsistency | Plan section 1 vs spec section 0 | state S5 is a Wave 1 prerequisite for the reader line, not for placement |
| C19 | Spec "never retrofit cached zones" | no precedent mentioned | Precedent exists for authored scenes (`FellingSceneRuntime.UpgradeCachedZone`, `MorrowfastSceneRuntime.UpgradeCachedZone`) | `OverworldZoneManager.cs` PrepareZoneForAccess (about 1478-1485) [V] | not a defect; offer as an owner option for the stateless forge only |

## 2. Findings

### P1-1. Wave 1 sites are unreachable with the specified anchor model; the acceptance gate cannot pass, and what would ship is the Well ring in every town

**Verified mechanics.**
- `Near: ["Well"]` is a constant. `PlaceMainWell` puts the Well at `VillageBuilder.GetVillageSquareCenter()` = `(34+6, 9+3)` = **(40,12)** in every shared-pipeline village (`VillageBuilder.cs:165-177`; `VillagePopulationBuilder.cs:552-577`) [V].
- Candidates are sorted nearest-first by Chebyshev distance, then hash (`InstallationPostBuilder.cs` candidate sort; spec C3 step 5) [V]. With `MinRadius` m the forge lands in ring m (or the next ring with a fitting cell) of the first `Near` anchor. `MaxProbe 48` covers rings m..m+1 or so.
- A non-solid forge is safe by construction (`Fits` skips route and standing checks), so the dossiers' "verified windows" (computed for solid footprints) are not what makes it safe.

**What the plan asks for versus what the model can do.**

| Town | Plan target | Where the dossiers put it | What `Near/Radius/MinRadius` produces |
|---|---|---|---|
| Gantry | exchange forecourt pad, rows out2-out3, door.x +-2..9 | Exchange NW or SW at x9-26 (formations 0, 1) or NE x53+ (formation 2): distance 14-30 from the Well; wayboard (34,10) is distance 8+ from x<=26 | forge in the north hub yard, e.g. formation 0 row 8, x36-43 are open ground (`settlement-gantry.md` formation-0 map) [R/V] |
| Tally | "court in front of the goods-store door", NE apron x50-58,y9-11 | distance 10-18 from the Well; only the alternative "N-E court x43-48,y3-9" is inside Radius 9 | ring 3-4 cells around the Well, not either court |
| The Last Counter | supply-post forecourt, footprints `(2..5,12)` from the post origin | anchor is the Envoy **inside** the post at `(post.X+6,post.Y+4)`; nearest exterior cells are 4-6 away (rows `Y+8..Y+10` hugging the south wall); verified footprints are 8 away | a wall-hugging cell outside the post, not the verified forecourt row |

The plan's own fallback ("tune `Near` or wait for S24") does not work: `Near` names owners, not plan regions, and S24 is a 2-cycle slice scheduled after S7/S8. The plan's section 7 "same machine, different place" is also not achieved by placement: all three forges would sit at the same relative position (ring m of the Well), differing only in prose.

**Why P1.** Wave 1 is the only wave that ships player-visible machines. Its stated acceptance cannot pass; shipping the approximation bakes a different site than the one the dossier purpose assumed into every save that visits the town (the plan itself says wrong rows cannot be repaired by data).

**Fix (choose one before RED).**
- **Option A (zero new code; recommended for non-solid forges).** Declare the hub-ring semantic honest. Rewrite Wave 1 purposes and hooks to "beside the square", replace the window test by invariants (exterior, unreserved, not `RoadStone`, no door/stairs/Well within 1, within N of the Well, not in the documents/guest ring of Tally), define `MinRadius` inclusively (C15), and drop the claim that placement differs per town.
- **Option B (about +1 cycle, only for composed towns).** Pull a minimal plan hook into S7: `CreateVillagePipeline` already holds `gantryBase`, `tallyBase`, `last`, `post`, `camp` (`OverworldZoneManager.cs` 1000-1100 region [V]). Add to the spec row an optional `Region` token (for example `front-apron:ExchangeFloor`, `forecourt:SupplyPost`) resolved by a small `IPlacementRegions` the composition builders implement (opt-in; the Sumphold furnishing builder already reads its plan via `terrain.Plan`, `SumpholdFurnishingBuilder.cs:20-31` [V]). The window tests then become real.
- Either way, move the "S24 deferral" note out of Wave 1's acceptance text.

### P2-1. Spec S7 and Placement Plan disagree on the first shipped rows; the working tree has the spec's, and visited towns keep them forever

Spec S7 ships `gantry-forecourt-forge`, `tine-forecourt-forge`, `slip-forecourt-forge` (Retention None). Plan 4.2 ships Gantry, Tally, the Last Counter, keeps Tine and Slip **test-only**, and declines Slip in favour of a Wave 3 lamp-works. `Installations.json` in the working tree has the spec's three rows [V]. The plan is the later and more evidenced document; the stickiness argument (first visit bakes the layout) is its risk 1.
**Fix.** The owner picks the shipped rows (plan decision 2) before `Installations.json` is committed. Until then ship zero rows; keep Tine and Slip rows only inside tests via `InitializeFromJson` (the test files already do exactly that). Add a census assertion that the shipped file's non-test rows equal the owner-approved list.

### P2-2. S9 is mis-scoped (C2, C3)

Three of its named users already go through `ForgeWorkReceipt.Begin`. Two of its "RED on HEAD" tests (`ForgeVerbsRefuseWhenStationSwapped`, a salvage swap) may already be GREEN. **Fix:** make S9's first step a probe table (one row per station user: scan used, `Begin` used or not, callback re-check used or not); only the rows showing a gap get a RED test. Expect 1 cycle, not 1-2.

### P2-3. S12 is larger and differently shaped than sized

Verified call sites a pooled extra input touches: `MaterialProcessIngredient.Resolve` (`MaterialProcessPlan.cs:107-120`), the plan's `Current()` (`:96-104`, `ExtraInputs.All(i=>i.Before(Actor))`), `MaterialProcessService.TryExecute` input claims, per-input `PreparationActions.Begin`, `Guard`, the consume loop (`TryConsumeMaterialProcessUnit` per unit), `Final()` (`plan.ExtraInputs.Any(i=>!i.After(actor))`), `InventoryTransferSnapshot` coverage and the quote/preview text (`MaterialProcessService.cs:66-137`) [V]. That is a shared transaction core, not "2-3 cycles".
**Fix.** Split: **S12a** pooled *resolution* and a read-only plan (row appears, reasons, no consumption change; RED = the Recovery two-stack case), **S12b** pooled consumption, claims, `Final()` and rollback at each take. Narrow to extra inputs (C9). Keep the independent critique gate.

### P2-4. S3 mis-specifies its parity golden and under-lists consumers (C10, C11)

The two charge chains are **predicate families** (`WorldResourceActions.IsCommand`, `PreparationActions.IsCommand` with prefixes `InfuseMineral|`, `PrepareRecipe|`, `RefuelTorch|`, `CompostCrop|`, `TransplantCrop|`; `RegionalRequest:`; `CompanionManagementActions`), not command literals (`InputHandler.cs:3440-3510`; `PreparationActions.cs:12-16`) [V]. "Enumerate every command literal" cannot prove parity. **Fix:** golden = for each family, a representative command per branch, asserted through both menus; plus the C10 and C11 items. Size 2-3 cycles.

### P2-5. Wave 2 `Fee` at the Last Counter is infeasible as drafted (C13)

See C13. **Fix:** decide before S10: operator bound plus line of sight, or an in-room station. Do not put a Fee row in S11 until the geometry check passes in a probe.

### P2-6. The critical path is over-constrained (C7)

S7's rows do not need retention; S2b can run in parallel and ship with the first state-bearing or player row. This shortens the time to the first player-visible machine by 1-2 cycles and makes S7's first cycle depend only on the marker.

### P2-7. Determinism claims need narrowing (C5)

`RetryReplacesOnceAtTheSameCell` and the spec's "places again identically" are false under a pipeline retry; `RegenerationAfterRelapseReplacesAtTheSameCell` holds only if relapse regeneration uses the same seed and unchanged settlement state. Home design adds a second `changed` source (well-keeper renewal, `SettlementManager.cs:102-112`) [V]; add it to S2's RED list as home's critique R1 already asks.

## 3. Verified claims (so the owner knows what is solid)

[V] unless noted.
- Three station scans plus one delegation: `ForgeWorkReceipt.cs:33-43`, `AlchemyStillPart.cs:104-131`, `WorldAudioWork.cs:60-80`, `ForgePart.cs:300`. `Ready` clauses verbatim as spec C5 lists them (`ForgeWorkReceipt.cs:17-28`).
- Saved type name is `AssemblyQualifiedName`; `LoadPart` returns null for an unresolvable type; unknown fields decode as `object`/`SkipValue` without consuming bytes (misalignment): `SaveSystem.cs:1644-1648, 2085-2090, 2393-2412, 2215-2218`. `FormatVersion = 8`, strict equality (`:48,167`).
- Field rules (public, instance, non-readonly, not `[NonSerialized]`, `CanSerializeType`; `ParentEntity` excluded) and class `SaveGraphSerializer` (`:971,2091-2108`).
- World menu reach gate precedes the chain; chain and fallthrough as described (`InputHandler.cs:3202-3210, 3459-3471, 3520-3524`); pack-menu `ItemAction` (`InventoryUI.cs:269-274`), copy at `:1219`, charge chain `:1511-1523`. `InventoryActionList.AddAction` returns `void` (`InventoryAction.cs:55`).
- Reader provider lists differ exactly as spec 1 item 6 says (`ExaminablePart.cs:132-141`, `ExamineDocumentBuilder.cs:93-97`); the document builder has no film or contained-liquid reference.
- Highest builder priority in any village pipeline is 4600 (`SumpholdFurnishingBuilder.cs:13`, `QuillholdCopyHallBuilder.cs:14`); 4650 is free; Morrowfast returns before the shared pipeline (`OverworldZoneManager.cs:975-979`); `CreateVillagePipeline` tail at `:1163-1165`; `CanUnloadZone` chain `:1242-1248`; relapse path `PrepareZoneForAccess -> AdvanceSettlement -> UnloadZone` (`:1487-1491`), and `UnloadZone` is called nowhere else in production (`grep`).
- `Zone.GetReadOnlyEntitiesWithPart<T>() where T : Part` and `IsInstanceOfType` membership (`Zone.cs:125-130, 168-172`); `GenReservedCells` is not serialized (`:397`).
- `Geometry` is `internal sealed`; `Place` admits exterior bare reserved-free cells only and requires `x,y` in `[2, W-3]`/`[2, H-3]` (`SpreadWildernessSituationBuilder.cs:167-227`); `DoorPart.IsBareGround` excludes Furniture, Solid, Wall, containers, liquids (`DoorPart.cs:70-77`).
- `StableIndex` is FNV-1a (`Formation.cs:254`); `Diag` default-on categories include `worldgen`, `furniture`, `craft`, `event`, `trade` (`Diag.cs:119-120`).
- `TinkersForge` is non-solid, layer 5, glyph `n`/`&R`, tags Furniture and Weaponcraft, only `Forge` Part (`Objects.json:5597-5640`). Factory registers parts by simple class name (`EntityFactory.cs:77-92`); no name collisions for the new type names at HEAD.
- Priority of `Sumphold` post-builder shape (`Current()`, receipt staging, false only on one drift branch) as spec 1 item 11 says (`SumpholdFurnishingBuilder.cs:22-33, 53-65`).
- `PlaceSites.json` relapse list (Tine 150, Wellmeet 900, Ledger 700, Cinderhold 600) and tracked villages (Sill, Wellmeet) match the spec's relapse-capable list.
- Tinker 70% and `TinkerStock` (`VillagePopulationBuilder.cs:132-134`; `LootTables.json:2729`); infusion needs `IsNearForge` (`MineralPreparationPart.cs:28`); Last Counter sign says "or bring the kit to a forge" (`LastCounterCompositionBuilder.cs:177`).
- The kitchen fee precedent (`KitchenBatchPart.cs:352` `DeferCurrencyTransfer`), `CuttingToolPart.UpgradeLoadedDefinition` (`SaveSystem.cs:263`) and the crop/kitchen reconcile call (`InputHandler.cs:1204`) exist.
- Commit `9316f9330` contains `serrated_kit` (`MaterialProcesses.json` row 6).

## 4. Slice sizing and order

| Slice | Spec size | My estimate | Note |
|---|---|---|---|
| S1 | 1 | 1 (already drafted) | add `BrainPart`; generate in the clean clone |
| S2 | 1-2 | 1 (a) + 1 (b) | split (C7); add well-keeper renewal row |
| S3 | 2 | 2-3 | predicate-family golden; five consumers; stale activation key |
| S4 | 2-3 | 3 | eight files; oracles contain it; rig disabled row last |
| S5 | 2 | 2 | golden churn; voice gate for the marker line |
| S6 | 1 | 1-2 | `JsonUtility` quirks [U]; pipeline-priority test for 16 villages |
| S7 | 2 + play | 2-3 | +1 if Option B (P1-1); sweep sizes may need a cap (8 seeds default, 24 in an explicit run) to keep the 5-minute cadence |
| S8 | 1 | 1 | data only after owner picks |
| S9 | 1-2 | 1 | probe table first (P2-2) |
| S12 | 2-3 | 3-4 | split a/b (P2-3) |
| S13a/b | 2 + 2 | 2 + 2 | ok |
| S14 | 2 | 2 | ok |
| S15 | 1 | 1 | call sites partly verified (`InputHandler.cs:1204`; `GameBootstrap` lines not read) |

Dependency soundness: S9 -> S4 and S12 -> S3,S4 are sound; S7 -> S2 should be S7 -> S2a; S13b -> S7 is sound; the plan's "Wave 1 needs S5" belongs in the spec's dependency list.

## 5. Wave 1 shippability

- Mechanically shippable on shipped foundations: yes, for a non-solid, stateless, Open `TinkersForge`. The builder shape is proven by Sumphold/Quillhold; state is not required; forge verbs, mineral infusion and weapon salvage resolve any ready forge in the actor's 3x3.
- Not shippable as described: the three named sites (P1-1), the spec/plan row conflict (P2-1), the Fee follow-on (P2-5).
- The three W1 towns have no authored `ForgeCold` input, so process rows never appear there unless the player carries Cinderhold's single find (`machine-forge.md` F4); the value there is the weapon-forge verbs and infusion, which is what the plan's purpose text should say.
- Wave 1 delivers machines in 3 towns (plus Wellmeet if approved) of 17; "every town" is a ratchet on Wave 2+ gates, as the plan already states. This is a scope disclosure, not a feasibility defect.

## 6. P3 nits

- C1, C6, C12, C15-C19 above.
- Spec C1 hygiene lint (no `*ZoneID` fields) is overbroad for any future Part that must name a zone; home adopted it knowingly (N-13). Scope the lint to Parts attached to installable machines.
- `ZoneGenerationPipeline.AddBuilder` uses an unstable `List.Sort` (`:36`), so tied priorities (4100) have undefined order; 4650 uniqueness is therefore load-bearing; keep the S6 test.
- Authored strings in S5/S7 (marker line "Set here to be used.") need the blind gate before merge.
- `InstallationRegistry.LoadDefaults()` call site unspecified; confirm lazy-load on first `For()` in play and tests.
- Disabled-row reader text for rows that become disabled between menu open and the details reader (`InputHandler.cs:4698`).

## 7. Verification record

Read: both spec documents in full; dossier verdicts for all twelve machines (classification lines) and `machine-forge.md`, `machine-still.md` headers; `settlement-gantry/tally/last-counter` plan sections; `ForgeWorkReceipt`, `ForgePart` (header, rows, events), `AlchemyStillPart.IsNearStill`, `WorldAudioWork.Forge`, `MineralPreparationPart`, `WeaponSalvagePart`, `PreparationRecipePart`, `PreparationActions`, `MaterialProcessPlan`, `MaterialProcessService.TryExecute`, `MaterialProcesses.json`, `SaveSystem` part save/load/field rules/type names, `InputHandler` world-menu gate and chain, `InventoryUI` row copy and charge chain, `WorldActionMenuUI` selection, `WorldInteractionSystem.GatherActions`, `Zone` part index, `ZoneManager.UnloadZone`, `ZoneGenerationPipeline`, `OverworldZoneManager` village pipeline and unload/relapse, `SettlementManager.AdvanceSettlement`, `SettlementSiteDefinitions`, `VillagePopulationBuilder` (Well, Tinker, crafting stations), `VillageBuilder` square bounds, `SpreadWildernessSituationBuilder.Geometry`, `SumpholdFurnishingBuilder`, `DoorPart.IsBareGround`, `Formation.StableIndex`, `Diag` categories, `EntityFactory` part registration, `WorldMapAuthoring.Places`, `PlaceSites.json`, `Objects.json` TinkersForge, `LastCounterCompositionBuilder` ground whitelist and sign text, the working-tree installation files, `Lore/Factions/07_TentRight.md` and `catacomb_village_design.md` citations.

Not read (named so nothing is implied): `GameBootstrap` reconcile call sites; `JsonUtility` field-initializer behaviour; the exact `SettlementManager` calls to reach relapse in a test; village-count assumptions in `SpreadExplorationWorksites`/`SpreadWorksiteManifest` tests (only grepped; no exact forge counts found, `ChunkActivityCensusBenchTests` is a bench without asserts); Tally and Gantry plan sources beyond the dossiers' extracts.
