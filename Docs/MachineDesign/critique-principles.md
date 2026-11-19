# Independent critique: principles, modularity, save and performance risk

Lens: does `ARCHITECTURE-SPEC.md` (and `PLACEMENT-PLAN.md`, `machine-*.md` where they lean on it) really follow `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, and what can corrupt saves, break determinism, allocate per frame, leak units, duplicate or mint, mis-handle NPC use or FOV, or block the later home/base system.

Method: read-only. I read the spec, the placement plan, the principles, the roadmap and the three architect inputs only where needed, then **verified every claim I rely on against code** (paths below are repo-relative to `/Users/steven/caves-of-ooo`). The working tree already contains an uncommitted implementation of S1, S2, S6 and S7 (`Assets/Scripts/Gameplay/Installations/*`, `Assets/Tests/EditMode/Gameplay/Save/SavedPartContract*`, `Assets/Resources/Content/Data/Installations.json`, one conjunct in `OverworldZoneManager.CanUnloadZone`); I used it as evidence of what the spec text produces, but the critique target is the spec. Nothing was run, built or written to the project.

Evidence tags: **[V]** I read the cited line this session. **[I]** inference. **[U]** unverified; the named test settles it.

---

## 0. Verdict: accept with changes

The spec is unusually honest and its core is sound: additive-only saves, a golden for saved Parts, a single retention conjunct, one deterministic post-builder at an unused priority, adapters with oracles, veto-before-claim for access, no `FormatVersion` bump. The shape follows the principles better than most designs I have seen in this repo. I found **one P1** (production data that the other authoritative document rejects, and which is already in the working tree) and **eleven P2** gaps where the spec's own headline guarantees ("a golden test makes that a failing test", "retention follows instances", "a modifier contributes before payment", "reverting a placement is deleting a row") are weaker than stated. Each has a concrete, small fix. None needs a redesign.

Scorecard against the principles:

| Principle | Conformance | Where it slips |
|---|---|---|
| Parts, capabilities, shared rules; no per-pairing edits | Mostly | `StationReadiness` keeps four Part-specific clauses in central code (P2-5); "exactly one provider" forbids composing two station-kind Parts on one entity (P2-5) |
| Definitions vs instances | Partly | `ServiceTermsPart` copies policy (Access, Fee, OperatorBlueprint) by value into saved instances (P2-6); marker `RetainZone` is a static copy (P2-2) |
| Veto before commit, finite accounting | Yes for terms/stations | Quote modifiers add an untyped "declared cost" with no charge path (P2-8); `ActionTime.Free` is an unverified promise (P2-7) |
| Adapters for incompatible state | Yes | S9 deliberately removes today's presence-only leniency (named, acceptable) |
| Honest limits | Yes | First Tent / Quiet's Door exemption `Source` lines are inference presented as canon text (P3-8) |
| No universal framework; add abstractions when behavior requires | Mostly | R1 admits a "test Part standing in" as the second consumer, which lets any contract through (P3-1) |
| Save integrity | Strong intent | The S1 guard has verified holes (P2-1); retention semantics around relapse and installed state are under-specified (P2-2, P2-3) |

---

## 1. What I verified and found sound (so the implementer does not re-litigate it)

1. **`FormatVersion` strict equality and the stream-framing hazard are real.** `Assets/Scripts/Gameplay/Save/SaveSystem.cs:48,167`; saved type name is `AssemblyQualifiedName` (`:2393`); `LoadPart` returns null for an unresolved type without consuming the body (`:1644-1648`); the entity loop `continue`s (`:1037-1039`) and then fails at the `Entity.End` check (`:1048`). So a removed/renamed/moved Part makes the save **unloadable and loud**, not silently corrupt. [V] The spec's S1 premise is right; its wording "corrupts later fields" is slightly too strong (it fails at `Entity.End`).
2. **A removed saved field desynchronises the stream.** `ReadPublicFields` reads an unknown name with `typeof(object)` (`:2076-2089`) and `ReadFieldValue(object)` returns `SkipValue` consuming **zero bytes** (`:2235-2236`). So deleting or renaming a field misaligns every later field. [V] The spec's census design (types **and** fields) is therefore required, not optional.
3. **Plan writers derive from the same field list as the golden reads.** `GetSerializableFieldWriters` calls `GetSerializablePublicFields` (`SaveFieldWriters.cs:38,44`). [V] The golden's field set is the real wire set.
4. **Priority 4650 is unique.** Highest village builder priority is 4600 (`SumpholdFurnishingBuilder.cs:13`, `QuillholdCopyHallBuilder.cs:14`), pipeline sorts by priority (`ZoneGenerationPipeline.cs:36`). [V]
5. **`CanUnloadZone` is relapse-time only.** Sole production caller chain: `UnloadZone` (`ZoneManager.cs:125-127`) from `OverworldZoneManager.cs:1489-1491` plus three dev scenarios. The marker's full-zone entity scan therefore costs nothing per frame or per turn. [V]
6. **Post-pipeline acceptance does not re-validate content.** `SpreadExplorationPlan.ValidateInstalled` is identity-only (`SpreadExplorationPlan.cs:232-236`), so an extra forge cannot trip the save-time validator. [V] (The `FinalValidator` hook and `SecondExplorationSites.Install` run after the pipeline; neither scans for foreign entities in what I read. A RED per exploration-destination village is still cheap insurance, see P3-4.)
7. **A new class name cannot collide:** none of `StationPart, InstallablePart, WorkJobPart, WorkStorePart, ServiceTermsPart, ConsignmentPart, LiquidSettlingPart, ComponentSlotsPart, WoundStrikerPart, WindingBenchPart, CapstanPart` exists today. Factory registration is by `type.Name` (`EntityFactory.cs:83`). [V]
8. **The forge is not haulable.** `DragRules.IsRooted` (`DragRules.cs:106-112`): no `HandlingPart` and not `Takeable` means rooted, and `TinkersForge` has neither (`Blueprints/Objects.json:5597-5640`). So the "Retention None forge is hauled away and a second one regenerates" duplication path does not exist for forges. It would exist for any future haulable bench (P2-2). [V]
9. **Reach is default-deny** (`InputHandler.cs:3113-3122`), so a newly declared command is reach-gated without being listed. [V]
10. **Money path conformance.** `DeferCurrencyTransfer` is inside `InventoryTransaction` (`:50-54`), an allowed purse writer in the EC2 source pin (`MoneySupplyTests.cs:94-115`); the spec's fee design does not trip the pin. [V]

---

## 2. P1

### P1-1. The production data file already ships rows that the placement plan rejects, and visited towns can never be corrected

Evidence.
- Spec S7 selects Gantry, Tine and Slip "for mechanism coverage" and says owner sign-off is required before RED (`ARCHITECTURE-SPEC.md:584-592`).
- `PLACEMENT-PLAN.md:98-106` (table) and `:14`: decline Slip (lamp-works is the honest fit), approve Tine only "for regression value or keep test-only", and the plan's whole premise is that "a visited town never gains or loses a machine" so wrong rows are permanent (`:14`, risk 1 at `:227`). The spec says the same (C3 old-worlds rule, `:275`).
- Working tree `Assets/Resources/Content/Data/Installations.json` (untracked; the file was still being edited while I reviewed, last modified 19:08 on 2026-11-18) now carries six production rows: `gantry-forecourt-forge`, `tine-forecourt-forge`, `slip-forecourt-forge`, then the plan-qualified `tally-exchange-forge`, `last-counter-supply-forecourt-forge` and `wellmeet-work-yard-forge`. The first three have one shared generic `Purpose`/`Alternative` sentence and a `Basis` ending "mechanism coverage" (lines 26, 46, 66); the last three cite a verified local gap (lines 87, 109, 129). So the plan-qualified rows are already in; the two the plan declines or demotes (Slip, Tine) and the generic Gantry basis are what remain.

Why P1. This is the one place where a mistake cannot be undone by data: first visit bakes the layout into every save that visits (spec `:275`; `Authority` silently no-ops on cached zones, C3 step 1). A forge placed in Tine and Slip to prove a mechanism is a permanent unexplained machine in real saves, which is precisely the "progression padding / machine without purpose" the principles and the user's goals forbid, and it makes the shipped census claim ("every town") look better than it is. It is P1 rather than P2 only because the data file is the thing that gets committed and shipped; the fix costs minutes now and is impossible later.

Fix.
1. **Mechanism coverage belongs in test fixtures, not the production file.** The S7 control matrix already loads rows through `InstallationRegistry.InitializeFromJson`; keep the Tine and Slip rows (and the Sumphold ordering probe) as in-test JSON with a scoped override and automatic restore (see P2-10). Gantry stays only if play proves the gap (the plan says so, `PLACEMENT-PLAN.md:91`); until then give it a purpose that names a local problem or move it to the fixture too.
2. The shipped `Installations.json` contains **only rows on an explicit owner-approved allowlist** committed in a test (`ShippedInstallationIdsAreOwnerApproved`: an ordinal-sorted constant list; adding a row forces a deliberate diff and a decision log entry). Reconcile S7/S8 text to PLACEMENT-PLAN Wave 1 (Last Counter, Tally, Wellmeet; Gantry after play).
3. Add validator rule **E15**: a shipped row's `Basis`/`Purpose` must name a local gap and may not contain the tokens `mechanism` or `coverage`; `Purpose` must differ per place (three working-tree rows share one `Purpose` and one `Alternative` sentence, which is itself a smell).
4. S7 acceptance step "walk to Gantry, Tine and Slip" (`:591`) becomes "walk to the towns that have approved rows".

---

## 3. P2

### P2-1. The S1 saved-Part golden has verified holes, so the spec's headline guarantee is overstated

Spec thesis item 4 and R3 (`:23`, `:69`): "A golden test makes that a failing test." Verified holes (each a way to corrupt every later save while the golden stays green):

1. **Nested serialisable classes.** `CanSerializeType` accepts any class with a parameterless constructor (`SaveSystem.cs:2156-2157`) and `IList`/array elements recurse into their own public fields. The snapshot records only `FieldType.ToString()` (spec `:103`; working tree `SavedPartContractReader.cs` `Snapshot`). Removing or retyping a field **inside** a payload class (`EscrowEntry`, a consignment record, any `List<X>` element type) is invisible. This is exactly the shape of S16 `WorkJobPart` (escrow lists), relay `ConsignmentPart.Goods`, S18 `WorkStorePart`, and the home `PackableFixturePart`.
2. **Enums serialise as `Convert.ToInt32`** (`SaveSystem.cs:2180`, read `:2245`). Reordering or inserting a member in the middle renumbers saved values silently. The golden records the enum type name only.
3. **`OnBeforeSave`/`OnAfterSave` hooks write extra bytes.** `SavePart` calls them around the field block (`:1622,1641`). Two shipped Parts override them: `Assets/Scripts/Gameplay/World/Spatial/SpatialFootprintPart.cs` and `Assets/Scripts/Gameplay/AI/AIAmbushPart.cs`. A Part with such hooks has a hidden wire format that the "reflected" classification does not flag.
4. **Spec text omits `BrainPart` from the custom list** (spec `:107`), but `SavePart`/`LoadPart` special-case it (`:1634-1635`, `:1661-1662`). The working-tree reader already adds it (`CustomSerialized` array). The spec text should be corrected so the next implementer does not regress it.
5. Out of scope but worth one sentence in the doc: effects, goals and entity `Properties` are reflected too (the spec says effects/goals are out of scope; that is acceptable only while no machine slice adds an Effect).

Fix (small, same slice, still one cycle):
- Snapshot **recurses**: for every field whose type is a serialisable class, array/list/hashset element class or `Nullable<T>`, emit the nested type's fields indented (cycle guard by type); emit each enum's `name=value` table; flag `hooks` when `OnBeforeSave`/`OnAfterSave` is overridden (`GetMethod(...).DeclaringType != typeof(Part)`).
- Add `Compare` self-tests `Compare_NestedFieldRemoved_IsViolation`, `Compare_EnumMemberRenumbered_IsViolation`, `Compare_HookAdded_IsViolation` with controls.
- Correct spec `:107` to list `BrainPart` and `ISaveSerializable`.

### P2-2. Marker retention is a static, unconditional copy; it can retain forever on stale or moved markers, and it is the wrong shape for later state

Evidence: `InstallationMarkerPart.Retains` is `RetainZone || Origin != "Generated" || legacy blueprint` (spec `:135`; `InstallationMarkerPart.cs:30-38`). Nothing consults whether the entity is still an installed fixture.
- A Player-origin marker on a portable fixture that is picked up, thrown or dropped in any village makes that village retained **permanently** (a dropped item in a cell is a zone entity). The home design already found this (`mw/home-design/HOME-BASE-DESIGN.md:757` item 13, "R2 closes it"); the spec text does not.
- `RetainZone` is decided once from the spec row (`Retention`) and validated against a **blueprint** denylist (P2-4). State-bearing Parts attached later (S16 `WorkJobPart` escrow, S18 stored work, S19 installs, `Packet` plans that `AddPart` post hoc as `SecondExplorationSites.Install` does) can create finite custody on an entity whose marker says `RetainZone=false`.

Fix:
- Make retention **derived**, not declared: `RetentionReason(entity)` = `marker present AND (Origin == Generated ? RetainZone : IsInstalled(entity))` OR any Part on the entity implementing a tiny `IRetainsZone { bool Retains { get; } }` (implemented by `WorkJobPart` while a job or escrow exists, `ContainedLiquidSourcePart` while `Volume > 0`, and so on; zero new saved fields). `IsInstalled` is `InstallablePart.State == Installed` once S19 lands; until then `Origin != Generated` is limited to `Relocated` (the rig).
- Keep the blueprint-name list only as the documented legacy shim, with a ratchet test that the list may never grow.
- RED: `LooseOrCarriedPlayerMarkerDoesNotRetain`, `StateAddedAfterPlacementRetains`, `RetentionReleasesWhenStateEmpties`.

### P2-3. Retention interacts with relapse-dependent generation and is unspecified for the starting village (the likeliest home)

Evidence: generation reads the main well's stage. `TradeStockBuilder.cs:105-110` injects Silver Sand into the Merchant while the well is Fouled/TemporarilyPurified, and `GetTradeGoodsForZone` switches the shelf by stage (`:118-130`). `IsTrackedVillage` is true only for the starting village and Wellmeet (`SettlementSiteDefinitions.cs:20-27`), i.e. Sill, the town a player is most likely to settle in. Today a relapse regenerates the village, which is also how the stock is re-synchronised to the Fouled state. A retained Sill (any Player/Relocated marker, or a state-bearing generated machine) no longer regenerates, so the well can relapse while the merchant's shelf and Silver Sand stay as generated. The spec discloses "no longer re-rolls build-time stock" (`:144`) but not this coupling, and the starting village is the highest-probability home location.

Fix: decide and test the policy before S19, not after.
- Option A (smallest): when a retained, relapse-capable village relapses, run a **narrow refresh** of the relapse-dependent shelf (a `RefreshStageDependentStock` call limited to ensuring Silver Sand and the stage goods table), no regeneration.
- Option B: refuse `Retention: Zone` rows in tracked villages (Sill, Wellmeet) and document it; players' bases there use the refresh from A.
- RED either way: `RetainedRelapsedVillageStillOffersAWayToRepairTheWell` (generate Sill, buy and consume the only Silver Sand, relapse, assert a supply path exists), plus `RetentionDoesNotChangeStageDependentStockForUntrackedVillages` (control).
- Also correct spec `:49`: `SecondExplorationSites.Retain` already retains Quillhold, Cinderhold, Tally and Wellmeet when exploration is enabled at version >= 15 (`SecondExplorationSites.cs:15-21`), so the "regenerates" hazard for those towns is limited to pre-v15 worlds; the live hazard is Sill, Tine and the Drowned Ledger.

### P2-4. "State-bearing" is a hard-coded type denylist; custody integrity depends on someone remembering to extend it

Evidence: spec E07 lists five Part types (`:223`); the working tree's `InstallationRegistry.StateBearing` is the same five (`InstallationRegistry.cs:191-192`). A blueprint with `DestructiblePart` (HP, `Gone`), `LockPart` (locked state), `CampfirePart`/`LightSourcePart`/`ThermalPart` (fuel and burn), `CropPart`, `WellPart`, `HarvestablePart` is not caught, so a `Retention: None` placement can silently take finite state and lose it to relapse regeneration. (Today the forge has none of these, so the failure is latent.)

Fix: invert the check. Admission is an **allowlist of stateless Part types** (Render, Physics, Examinable, ForgePart, the station Parts, `InstallationMarkerPart`, ...). Any other Part type forces `Retention: Zone` or rejects the row. Add a census test over **all** `Objects.json` blueprints that classifies every Part type used as stateless or stateful so a new Part cannot enter placement unclassified. Unknown types default to stateful (fail closed).

### P2-5. `StationReadiness` hard-codes four Parts' physical rules, and "exactly one provider" forbids composition

Evidence: spec `:335-337` keeps verbatim the clauses `RepairablePart.BlocksFunction`, `DraggedPart`, `HarvestablePart.Harvested`, `DestructiblePart Gone or HP<=0` inside the central evaluator (from `ForgeWorkReceipt.cs:24-26`), and requires "exactly one Part providing `kind` and parented to owner" (from `:23`, `owner.Parts.Count(p => p is ForgePart) != 1`).
- Adding a new physical state (frozen, flooded, jammed, unpowered) means editing `StationReadiness`: the opposite of "routine additions should not require editing every interacting system" (principles line 5). The spec already created `IStationReadinessGuard` for the rig; the four legacy clauses should be its first four implementations.
- A machine cannot carry two Parts that both provide `cold-work` (a bench with a forge attached, a player-built composite). That is a parity artifact of the legacy `ForgePart` count, promoted into the general contract.

Fix:
- `RepairablePart`, `DraggedPart`, `HarvestablePart`, `DestructiblePart` each implement `IStationReadinessGuard` (three lines each, same codes `faulted/hauled/harvested/destroyed`). `StationReadiness.Evaluate` keeps only **structural identity** clauses (zone current, cell round-trip, parent/zone match). The truth-table oracle in S4 proves parity unchanged.
- Replace "exactly one provider" with "at least one provider of `kind` whose own guard is satisfied; two providers of one kind are `two-providers` only if their `Reach` or guards disagree". Keep the legacy "one `ForgePart`" count as a `ForgePart`-local guard so shipped behaviour is identical.

### P2-6. Terms are copied into saved instances; Fee rules duplicate the existing `Tender` model

Evidence: `ServiceTermsPart` holds `Access`, `Fee`, `OperatorBlueprint`, `OperatorWithin` by value (spec `:395-401`). The principles say definitions describe reusable contracts and instances hold condition, quantities, ownership (`SYSTEMS-DESIGN-PRINCIPLES.md:9`). Access mode, fee and operator role are policy (definition); only `OperatorID` and later `OwnerID` are instance state. Consequences: a mispriced fee is permanent in every visited town (no migration path; `Revision` on the marker is unused by any reader), and every instance of a terms row carries duplicated strings in saves.
Separately, `Docs/ECONOMY-SYSTEM.md` EC6 already decides who accepts drams: `Tender.For(trader)` (`Assets/Scripts/Gameplay/Economy/Tender.cs:24-43`). The spec re-encodes this as "Tent-Right Fee rejected at load, Gift cannot carry Fee" (`:405`, S10a), a second copy of a faction/biome rule that will drift from `IsDeepWasteland` and catacomb barter rules.

Fix:
- `ServiceTermsPart` stores `TermsId` (definition key, resolved from a `ServiceTerms.json` table by the same per-row-dropping loader pattern) plus `OperatorID`/`OwnerID` only. `Revision` mismatch then has a defined meaning (re-read the definition; instance state untouched).
- Fee admissibility = `Tender.For(operator) == Drams` evaluated at use time (and at load for the place's faction as a cheap early check), not a parallel hard-coded faction list.
- RED: `TermsRevisionChangeReachesCachedInstancesWithoutRewritingState`, `FeeRefusedWhenTenderIsBarter`.

### P2-7. `ActionTime.Free` is an unverified promise, and two turn authorities coexist indefinitely

Evidence: S3 lets a row declare `Free` or `OneAction` and bypasses the legacy chains (spec `:297-302`). Under the legacy chain, only listed commands spend a turn (`InputHandler.cs:3459-3471`); under the new rule any Part can mark a state-changing command free. Nothing ties "free" to "mutated nothing".

Fix: after a `Free` command commits, assert on the transaction that it recorded no currency delta, no item transfer and no claim on world entities (a `HasCommittedEffects` accessor on `InventoryTransaction`); if violated, spend the turn **and** emit `event/FreeActionMutated` (a RED pin plus test-Part case). Keep the "chains not deleted" decision but add the guard test `NoCommandAppearsInBothAChainAndADeclaration` so the two authorities cannot double charge.

### P2-8. The quote modifier channel can add a cost with no typed charge path (unit leak and silent bypass)

Evidence: spec `:423`: handlers may "add a declared cost", "raise an output count within a row-declared `Modifiable` bound"; `ProcessQuote` has `Costs` with no unit or kind (`:423`). Principles line 11: "No effect may silently bypass payment." A cost in an unknown unit (items, drams, durability, ticks) has no defined commit-time payer, and two modifiers composed in Part order can give different results (additive then multiplicative).

Fix:
- `Costs` is a closed union: `Item{blueprint,units}`, `Drams{amount}` (via `DeferCurrencyTransfer` to a declared payee, never a new sink), `Durability{item,amount}`, `Ticks{n}`. The row declares the allowed kinds and ceilings (`Modifiable`); a handler may not introduce a kind or blueprint the row did not declare (the spec says blueprint, add kind).
- Modifiers are **additive deltas collected, summed and clamped** by the engine, never in-place assignments, so order is irrelevant (match the spec's own `VetoOrderIndependentOfPartOrder` standard, applied to modifiers).
- RED: `ModifierOrderDoesNotChangeQuote`, `UndeclaredCostKindRefused`, `EveryDeclaredCostKindIsChargedAtCommit` (property sweep), `PreviewEqualsCharge`.

### P2-9. Time units are inconsistent across the spec, the dossiers and the code

Evidence (all [V]): `TurnManager.TickCount` is the "turn" counter handed to settlements (`GameBootstrap.cs:417,1018` -> `OverworldZoneManager.SetTurnProvider` -> `SettlementManager.SetCurrentTurnProvider`, `OverworldZoneManager.cs:1202-1205`); at Speed 100 a player action costs 10 ticks (`TurnManager.cs:9-19`, `ActionThreshold 1000`). So `PurifyRelapseTurns = 120` (`SettlementRepairDefinitions.cs:5`) and `RelapseTurns: 150` are **ticks**, `CropTime.WorldTicksPerUnit = 10` is ticks, the kitchen's "120 ticks" is ticks, while the spec's `ActionTime.OneAction`, the bath dossier's `WearTurns` (per wearer `EndTurn`, i.e. per action, which also varies with Speed) and "turns" in the placement text are actions. A bare field named `Turns` in a new schema will be read either way.

Fix: name the unit in every field (`...Ticks` vs `...Actions`), add a naming lint to the S6/S14/S16 row validators (reject a bare `Turn(s)` field in new data), and state once in `MACHINE-INSTALLATIONS.md` that all durations in rows are `WorldClock` ticks and that per-action effects (G1 hook) declare `Actions` explicitly. Do not convert silently.

### P2-10. The static mutable registry will pollute unrelated tests and generation

Evidence: `InstallationRegistry` holds static `Specs/Exemptions/...` and `loaded` (working tree `:140`); `InitializeFromJson` sets `loaded = true`, so test-injected rows replace the defaults for the rest of the domain and `CreateVillagePipeline` consults it for every village (`OverworldZoneManager.cs:1165`). Isolated-clone EditMode runs are ~5 minutes, so an order-dependent failure is expensive to diagnose. The spec offers only `ResetForTests()`.

Fix: provide `using (InstallationRegistry.UseForTest(json)) { ... }` that snapshots and restores the static state, plus a shared fixture base (or `[SetUpFixture]`) asserting defaults are loaded before each fixture, and a test `RegistryDefaultsRestoredAfterEveryFixture`. Same pattern for the future second table.

### P2-11. Placement receipt has no rollback after the add, and rng-untouched is tested too narrowly

Evidence: spec step 7 (`:258`) rolls back on a mismatch but says nothing about an exception after `zone.AddEntity` (reading tile state, running the intact check, `Diag.Record`). The working tree wraps `PlaceOne` in a catch-all that logs `Omitted: exception` (`InstallationPostBuilder.cs:43`; line numbers drift because the working tree is being edited concurrently) while the staged entity can remain in the zone (added at `:110`, checks at `:111-113`). That produces a forge in the world while the diag says "omitted". Separately, the `RngUntouched` test only compares the passed `Random`; `factory.CreateEntity` could read a static generation stream (`TraderPart.Rng` is set for the whole pipeline run, `ZoneManager.cs:90-94`; `LoadoutPart.Rng` is static) for a future blueprint with loadouts or stacks, perturbing later traders in that zone.

Fix: stage inside `try { ... } catch { rollback } ` with `finally` guaranteeing `RemoveEntity` unless the commit flag is set; add `ExceptionAfterAddRollsBackAndOmits`. Extend `RngUntouched` with a counting `TraderPart.Rng` and `LoadoutPart.Rng` fixture proving admitted blueprints draw zero values (and make E14 reject `LoadoutPart`).

---

## 4. P3 (nits and hygiene)

1. **R1 is too weak.** "plus one new use (a placed machine, a bench, a test Part standing in for a later Part)" (`:67`) lets any contract ship with a test double. S14 (`:635`, "Ships only with a test modifier"), S21 (one entry), S23 and S24 satisfy R1 on paper with zero production consumers. Tighten R1 to "two production consumers, or one consumer plus an owner-approved dated trigger"; re-sequence S14 behind the first real modifier (the dossiers show none qualifies yet).
2. **Entity-ID shift** is real (`EntityFactory.cs:189-191`, sequential ints) and `Create` burns an ID even on rollback. IDs seed per-entity RNG and visuals (`TraderRestockSystem.cs:97`, `SettlementSiteVisuals.cs:113,142,203`). So "reverting a placement is deleting a row" (R4) does not restore world equivalence for later-generated zones in fresh worlds. State this in the doc; no code fix needed. Existing `string.GetHashCode` uses (`ZoneManager.cs:91`, `TraderRestockSystem.cs:97`) are a separate pre-existing determinism risk given `Formation.StableIndex`'s own comment that string hashes are randomised on modern .NET; the spec correctly avoids them.
3. **"marker index first"** (spec `:136`) is not what the code does (single `foreach` over all zone entities, `InstallationMarkerPart.cs:46-51`). Harmless (relapse only), but use `Zone.GetReadOnlyEntitiesWithPart<InstallationMarkerPart>()` (`Zone.cs:~115`) plus the legacy-name scan, or fix the text.
4. **Cheap insurance:** add `InstalledForgeDoesNotFailExplorationFinalizers` for the four `SecondExplorationSites` destinations (Quillhold, Cinderhold, Tally, Wellmeet) so a later `FinalValidator` that counts owners cannot silently null a town (`ZoneManager.cs:99`, `CommitGeneratedZone` false returns null).
5. **Anchor/reserve semantics:** C3 step 8 (`:259`) adds the standing cell to `GenReservedCells` only for solid kinds; keep the [U] test but also assert that adding a reservation after the receipt is not read by the save-time identity check. Low risk.
6. **Operator presence has no sight or state rule** (spec `:405`, `OperatorWithin 3`): a sleeping, paralysed or fleeing operator behind a wall counts as present and collects. Add `awake`, `not in conversation`, same-room (line-of-sight or no wall between) clauses and `OperatorClosedReason` text; bind by `OperatorBlueprint` **and** rebind when the bound ID is dead and a living resident of that blueprint stands within range (LocalPeople renames residents but IDs are stable; replacements get new IDs).
7. **NPC use needs a finite-input rule.** C11 lets NPCs run the same command (`:440`). Outputs worth more than inputs, crafted by an NPC from restocked inventory and sold to the player, become a wealth faucet that EC2's pin does not see. Either exclude value-adding processes from NPC use until a conservation check exists, or charge the NPC's own finite stock and forbid free restock of its inputs. [I]
8. **Exemption `Source` strings are partly inference.** `Lore/Factions/07_TentRight.md:119` says "a monument to a choice" (descriptive); the exemption's content "no machine installed" is a design inference, and the placement plan itself says the settlement report offered a bench option and was declined. Label exemptions `canon` (quote) or `design-inference` in the row (`Kind`), so a later owner can tell which are rules. The Quiet's Door source (`catacomb_village_design.md:409-416`, "Refuses all surface contact") is canonical per `Lore/10_Bible.md:73` but says nothing about machines; same labelling.
9. **Hierarchy of ids.** "Stacks ordered by ID ordinal" (spec `:422`): IDs are decimal strings, so ordinal order puts `"10"` before `"9"`. Deterministic but surprising; specify inventory order or numeric order.
10. **Descriptions in shipped data.** `Purpose/Alternative/Basis` (up to 400 chars each, `:189,229`) ship in `Resources` and are never read at runtime. Acceptable as living documentation; mention that they are not localised or voice-gated (L17) and must not be surfaced in readers.
11. **Per-call allocations in the contracts.** `IStationCapability.Kinds` returning `IReadOnlyList<string>` must be a cached static array, never `new[]` per call (it is called per object per scanned cell). `InstallationRegistry.For` allocates a list per call (generation-time only; fine, but cache it).
12. **S9 is a named behaviour change.** `AlchemyStillPart.IsNearStill` accepts any entity with the Part, destroyed or otherwise (`AlchemyStillPart.cs:104-130`); moving brewing to the strict receipt removes that leniency. It is the right call; list it under R7 as an accepted, announced change with a one-line release note, not "no behaviour change".
13. **Downgrade is unsupported by construction** (additive Parts, strict version equality): a save containing `InstallationMarkerPart` opened in an older 8-format build fails loudly at `Entity.End` (see section 1 item 1). Say so in the doc because the project routinely has several build generations installed (CLAUDE.md notes an older app launched by display name).
14. **Census ratchet constant.** `Pending` as a committed integer in a test is a merge-conflict magnet; assert against a sorted list of place ids so the diff is reviewable.

---

## 5. Premise corrections (stale or false statements in the inputs)

| Statement | Verdict | Evidence |
|---|---|---|
| Spec C1 "custom" list = StatusEffects, BitLocker, Inventory, ActivatedAbilities, Body, `ISaveSerializable` | Incomplete: `BrainPart` is also custom (and nested classes/enums/hooks are uncovered) | `SaveSystem.cs:1634-1635,1661-1662` |
| "Relapse regenerates Cinderhold and Wellmeet" | Only in worlds with exploration version < 15 or disabled; v15+ already retains them | `SecondExplorationSites.cs:15-21` |
| "marker index first, then a name scan" | Not implemented; single entity scan | `InstallationMarkerPart.cs:46-51` |
| "LoadPart ... desync later fields" | Fails loudly at `Entity.End`, not silently | `SaveSystem.cs:1037-1048` |
| Placement plan: Retention None "re-places it at the same cell" after relapse | True only while earlier builders draw an identical rng stream; holds for untracked towns (Tine). For tracked Sill/Wellmeet the stage-dependent shelf (`TradeStockBuilder.cs:93-130`, an extra draw for Improved) can diverge; the spec's RED uses Tine so it would not catch this [U] | `TradeStockBuilder.cs` |
| Spec/placement: forge is "stateless, field-free" | True today; the guarantee is only as good as P2-4 | `Blueprints/Objects.json:5597-5640` |
| First Tent exemption "no machine installed" cited to `07_TentRight.md:119` | Source is descriptive; exemption is a design decision | see P3-8 |

---

## 6. Contracts that would constrain the home/base system (summary)

Already compatible (verified): marker `Origin`/`Revision`, single retention conjunct, `Owner` fails closed, no positional authority fields in new Parts (hygiene lint), no version bump, retention keyed on instances not zone ids (`home-design/HOME-BASE-DESIGN.md:101,205-251` adopts these).

Needs the changes above before S19 / home work:
1. **P2-2** derived, installed-state-aware retention (the home design itself lists the gap as its R2).
2. **P2-3** relapse semantics for retained tracked villages (a base in Sill is the common case).
3. **P2-5** composition of providers on one entity (a player-assembled bench with a forge head).
4. **P2-6** terms by reference so a base's `Owner` access is a definition + `OwnerID`, not a copied policy block.
5. **P2-9** one stated time unit, because base upkeep, installs and jobs will all need durations.
6. `ActionTime` has only `Free`/`OneAction` (`:283`); installs and take-down need multi-action costs. Additive later (the enum is never saved); no action now, but do not use `sbyte` ordinals in any saved or hashed place.

---

## 7. Concrete amendments by slice

| Slice | Amendment |
|---|---|
| S1 | Recursive nested-type and enum snapshot, `hooks` flag, correct custom list; self-tests for each (P2-1) |
| S2 | Derived retention (`IRetainsZone`, installed-state aware), legacy list ratchet, relapse refresh policy + RED for the Silver Sand supply (P2-2, P2-3) |
| S3 | `Free` verification via transaction-effects accessor; no-command-in-both guard (P2-7) |
| S4 | Move the four physical clauses into `IStationReadinessGuard` implementations; relax "exactly one provider"; cached `Kinds` arrays (P2-5, P3-11) |
| S6 | E15 purpose lint; stateless-allowlist admission over all blueprints; naming lint for time units; `UseForTest` scope (P1-1, P2-4, P2-9, P2-10) |
| S7 | Production rows limited to owner-approved allowlist; exception-safe receipt with finally rollback; static-RNG probes (P1-1, P2-11) |
| S10 | `TermsId` + `Tender` check; richer operator presence; rebind rule (P2-6, P3-6) |
| S12 | stack order specified (P3-9); otherwise unchanged |
| S14 | Typed cost union, additive deltas, defer behind first real modifier (P2-8, P3-1) |

---

## 8. Verification record

Read this session [V]: `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`; `design/ARCHITECTURE-SPEC.md` (all); `design/PLACEMENT-PLAN.md` (all); headers and hazard greps of the twelve `machine-*.md` (relay `ConsignmentPart` section read); `home.md` summary and `home-design/HOME-BASE-DESIGN.md` greps; `SaveSystem.cs` (`SavePart`, `LoadPart`, entity load loop, `WritePublicFields`, `ReadPublicFields`, `GetSerializablePublicFields`, `CanSerializeType`, `ReadFieldValue`/`WriteFieldValue` heads, type naming); `SaveFieldWriters.cs`; `OverworldZoneManager.cs` (retention chain, relapse path, turn provider); `ZoneManager.cs` (`GetZone` head, `UnloadZone`); `SettlementManager.cs` (`AdvanceSettlement`, `RefreshActiveZonePresentation`); `SettlementSiteDefinitions.cs`; `TradeStockBuilder.cs`; `SecondExplorationSites.cs`; `SpreadExplorationPlan.cs` (`FinalizeGenerated`, `ValidateInstalled`); `ForgeWorkReceipt.cs`; `AlchemyStillPart.IsNearStill`; `DragRules.cs`; `InventoryTransaction.cs`; `Ledger.cs`, `MoneySupplyTests.cs` pin; `Tender.cs` (existence/API heads); `TurnManager.cs` (speed/threshold); `CropTime.cs` header; `EntityFactory.cs` (registration, id counter); `Zone.cs` (part index); `InputHandler.cs` (reach gate, cost chain); `TinkersForge` blueprint; `TraderRestockSystem.cs:97`; `Formation.StableIndex`; the working-tree `InstallationMarkerPart`, `InstallationPostBuilder`, `InstallationRegistry`, `SavedPartContractReader`, `Installations.json`; `Lore/Factions/07_TentRight.md:119`; `catacomb_village_design.md:405-418`; `Lore/10_Bible.md:73`.

Not verified [U], each with the test that settles it: whether `LoadoutPart.Rng`/`TraderPart.Rng` are read by `CreateEntity` for any currently admissible blueprint (P2-11 probe); whether `FinalValidator`s or `SecondExplorationSites.Install` ever scan for foreign entities (P3-4 test); whether a post-commit step compares `GenReservedCells` (spec's own [U]); exact `SettlementManager` calls to set a structure site into relapse (spec's S2 step 0); `JsonUtility` field-initializer behaviour; hotkey routing for disabled rows (spec's own [U]); whether `HasCommittedEffects` can be derived from existing `InventoryTransaction` state or needs a small additive field (P2-7).

No Unity, test, build or git write was performed. The only file written is this report.
