# ARCHITECTURE SPEC: shared machine/object contracts and generic installation placement

Status: design synthesis only. Repo `/Users/steven/caves-of-ooo` at HEAD `9316f9330`. No project file, Unity run, test, build or git write was made. All C#/JSON below is a sketch for the implementing engineer, not compiled code.

Inputs synthesized: `design/arch-capability-first.md`, `design/arch-data-first.md`, `design/arch-evidence-first.md`, `arch.md`, `placement.md`, plus (read for this synthesis) `enumerate.md`, `presentation.md` (summary), `home.md` (summary), `lore.md` sections 0-11, `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `Docs/MACHINE-ROSTER-VISION.md`. Lore authority is root `Lore/` only; `Docs/Lore/` is not used.

Evidence tags: **[V*]** I read the cited line in this synthesis. **[R]** taken from an architect/research report, not re-read. **[I]** inference or design. **[U]** unverified; the named RED test or first-cycle step settles it.

Standing gate to state up front: `CLAUDE.md` currently says "No new runtime changes" and keeps explicit work permission and the weapon-payload/equipped-input/save policies behind independent design critique before RED. This spec is design. Implementing any slice needs the owner to lift the "no new runtime changes" hold; S10 and S12 additionally carry the critique gate.

---

## 0. Verdict

One thesis, kept small:

1. **Add contracts only where two real consumers exist.** Each contract below is born with one shipped machine behind an adapter plus one new use (a placed machine, a bench, a test Part standing in for a later Part). No universal framework, no generic table loader, no provider registry until a third consumer exists.
2. **Capabilities are small interfaces on Parts; shared rules are static services; everything that varies per placement is data.** Reads (describe, readiness, access) enumerate `owner.Parts` for an interface. They do not fire unfiltered events, because `Entity.FireEvent` offers every event to every Part and `Part.WantEvent` is never called [R: both architects grepped; V* only that the design avoids relying on it].
3. **Adapters never change behaviour.** Each adapter slice copies the legacy function into the test as the oracle and runs both over one truth table. A behaviour change is its own slice with its own RED test.
4. **Saves are additive only.** No `FormatVersion` bump (8, strict equality), no Part class removed, renamed, moved or re-namespaced, no saved field removed or retyped. A golden test makes that a failing test (S1).
5. **"A machine in every town" is a census, not a claim.** `Installations.json` carries specs, canon exemptions with sources, and pending owners. A test classifies every authored place and ratchets `Pending` downward. With only the existing station kinds (forge, still) about 6 further settlements fit by lore today (8 once service terms exist for the two Concord places); the rest need new kinds or are canon-exempt. That is a scope disclosure for the owner, not an engineering risk (section 10).

What is built, in order of value:

| Phase | Slices | Delivers |
|---|---|---|
| A. Unblock placement | S1-S8 | save guard; relapse-retention fix; declarative action time and disabled rows; station contract with adapters and an access seam; one reader contract; validated installation registry and census; generic post-builder with first placements; census fan-out |
| A'. Hardening and permission | S9-S11 | all station users on one receipt; service terms (gated on critique); terms-bearing placements |
| B. Engine contracts | S12-S18 | multi-stack claims and reasoned process rows; work-kind table, `StationPart`, cold bench; quote modifiers; reconciler registry; jobs; receivers; stored work (canon-gated) |
| C. Bridge and hygiene | S19-S24 | installable state and verbs; NPC use; upgrade registry; repair bridge; classifier; in-room anchors |

Critical path to "usable machines in towns": **S1 -> S2 -> S6 -> S7** (about 6 isolated cycles, about 30 minutes of test time, plus actual play). S3, S4, S5 are parallel lanes that make every later machine readable, priced in turns, and findable; S7 does not depend on them because the first placed machine is the existing forge.

---

## 1. Verified ground truth and corrections

Items that change the design relative to the inputs.

| # | Premise (source) | Verdict | Evidence |
|---|---|---|---|
| 1 | Spurgrass slice is dirty/unaccepted (`arch.md` section 0) | **False at HEAD.** Committed as `9316f9330`; `serrated_kit` row is in `MaterialProcesses.json`; the commit message discloses the P2 that S12 fixes (a Recovery return leaves two stacks of one blueprint and the menu row silently disappears). `CLAUDE.md` still calls it "source selection only", so acceptance is the owner's. | [V*] `git show --stat 9316f9330`, grep count 1 |
| 2 | "Four 3x3 station scans" | **Three independent scans plus one delegation, with three readiness semantics.** `ForgeWorkReceipt.Resolve` (dx outer, dy inner; actor via `GetEntityCell`; functional readiness); `AlchemyStillPart.IsNearStill` (dx, dy; actor via `GetEntityPosition`; presence of `AlchemyStillPart` only, no readiness, no zone-current check); `WorldAudioWork.Forge` (y outer, x inner; presence of `ForgePart`; its own audio `Capture.Current`). `ForgePart.IsNearForge` is `Resolve(...) != null`. | [V*] `ForgeWorkReceipt.cs:33-43`, `AlchemyStillPart.cs:104-130`, `WorldAudioWork.cs:60-80`, `ForgePart.cs:300` |
| 3 | Part removal/rename may desync saves | **Confirmed, and also namespace/assembly and fields.** Saved type name is `type.AssemblyQualifiedName`; `LoadPart` returns null for an unresolvable name without consuming the body; an unknown field name decodes as `object`/`SkipValue`. | [V*] `SaveSystem.cs` `GetTypeName`/`ResolveType` (about :2393-2412), `LoadPart` (:1644-1648) |
| 4 | Which fields are saved | public instance, non-static, non-readonly, not `[NonSerialized]`, `CanSerializeType`; class is `SaveGraphSerializer` (static partial); hand-written serializers exist for `StatusEffectsPart`, `BitLockerPart`, `InventoryPart`, `ActivatedAbilitiesPart`, `Body` and `ISaveSerializable` Parts (`NarrativeStatePart`, `KnowledgePart`, `LairStackLedgerPart`, `LocalPeopleLedgerPart`, `StoryletPart`) | [V*] `SaveSystem.cs:2091-2108`, `:1650-1660`, grep |
| 5 | Both menus decide turn charge by hand-kept OR-chains | **Confirmed, with two corrections.** (a) World menu: reach gate at `InputHandler.cs:3202` precedes the chain at `:3459-3471`; a command in no chain falls to a bare `InventoryAction` event with no `InventoryTransaction` (about :3500-3510), so transactional Parts refuse. (b) Pack menu converts each world row into a private `ItemAction { Label, Command, Container }` at `InventoryUI.cs:1219` (struct `:269-274`), dropping every other field; the pack charge chain is `InventoryUI.cs:1511-1523`. So any new `InventoryAction` field must be copied there. | [V*] |
| 6 | Reader provider lists disagree | **Confirmed.** `ExaminablePart.BuildDescription` lists film, kitchen, dressing, oil strainer, contained liquid, repair, trap (`:132-141`); `ExamineDocumentBuilder.Build` lists kitchen, dressing, oil strainer, repair, trap (`:93-97`). Full document lacks the film and contained-liquid lines. Whether that omission is intended is [U] (S5 RED). | [V*] |
| 7 | Village pipeline slot for a post-builder | Highest builder priority in any village pipeline is 4600 (Sumphold furnishing, Quillhold copy hall); no builder anywhere in `World/` sits between 4600 and 4650 (4350, 4400 are Ginmere). **4650 is unique.** Morrowfast returns before the shared pipeline (`OverworldZoneManager.cs:975-979`). Every non-Morrowfast village gets a `Well` from `VillagePopulationBuilder` (priority 4000). | [V*] priority grep; [R] Well |
| 8 | Relapse regenerates a village | **Code path confirmed**: `PrepareZoneForAccess` -> `AdvanceSettlement` -> `UnloadZone` (`OverworldZoneManager.cs:1487-1489`); `CanUnloadZone` is a nine-predicate chain (`:1241-1244`). **Effect is inference**: no test couples `UnloadZone` with `AdvanceSettlement` (the two grep lists share no file) -> S2 begins with a premise test. Relapse-capable places: Sill and Wellmeet (well/oven/lantern), Tine Weir 150, Wellmeet Scales 900, Drowned Ledger SurveyLine 700, Cinderhold Pump 600. Every other village can never reach this path. | [V*] `PlaceSites.json` RelapseTurns; test greps |
| 9 | Zone part index can index capabilities | `GetReadOnlyEntitiesWithPart<T>() where T : Part` (`Zone.cs:125`) is constrained to classes; membership uses `type.IsInstanceOfType` (`Zone.cs:168-172`), so an interface overload is a ~10-line copy. Needed only when a zone-wide capability search has a consumer (S12). | [V*] |
| 10 | Placement geometry is reusable | `SpreadWildernessSituationBuilder.Geometry` is `internal sealed` (same assembly): `Place(x,y)` admits only exterior, dry, unreserved cells whose occupants are all `DoorPart.IsBareGround`; `PreservesRoutes(dest)` and `BorderReach` give the flood checks. Interiors are out of scope for the generic builder. | [V*] `SpreadWildernessSituationBuilder.cs:167-227`, `DoorPart.cs:70-77` |
| 11 | Post-builder precedent "always returns true" | **Not quite**: `SumpholdFurnishingBuilder` returns false on one receipt-drift branch while staging. A false clears the zone and retries up to 5 times, then nulls the zone (`ZoneGenerationPipeline`). The generic builder must never return false. | [V*] `SumpholdFurnishingBuilder.cs` staging loop; [R] pipeline |
| 12 | Forge/still are solid furniture | `TinkersForge` is non-solid, layer 5, with a field-free `ForgePart`; `AlchemyStill` likewise [R]. Route preservation is therefore vacuous for them and a standing cell is unnecessary; it matters for any solid blueprint (bench). | [V*] `Objects.json:5597-5640` |
| 13 | `InventoryActionList.AddAction` | returns `void` (`InventoryAction.cs:55`); changing it to return the row is source-compatible for statement callers. | [V*] |
| 14 | `WorkKind` is a closed switch | Three literal sites: `MaterialProcessRegistry.Valid` (`row.WorkKind!="ForgeCold" && !="HandCutting"`), `MaterialProcessPlan.TryCreate` and `Emit`/`ContextCurrent` [V* registry; R plan/service]. Only two kinds exist, one of them carried-tool. | [V*]/[R] |
| 15 | `ForgePart.Factory` coupling in `MaterialProcessPlan` | A bootstrap wiring detail set unconditionally in `GameBootstrap`; no placed machine is blocked by it. Left alone (section 4, C8). | [R] |
| 16 | `FieldWorkRigRules.Payload` vs `OilStrainingPart.Payload` and a marker Part | rig tolerates an extra Part (six required types once each, four forbidden); strainer pins exactly 5 Parts. So legacy marking is by blueprint name, not by Part. | [R: two architects]; consistent with [V*] strainer pin cited in `arch.md` |
| 17 | Saved type names are permanent | New Parts go in namespace `CavesOfOoo.Core` (assembly of `Part`), never in sub-namespaces; the factory registers parts by simple class name (`_partTypes[type.Name]`), so class names must be unique project-wide. | [V*] `EntityFactory.cs:77-92` |
| 18 | Entity IDs | `CreateEntity` advances the factory counter, so a placed installation shifts later IDs by one. Tests must compare blueprint and position, never IDs. | [I] |

---

## 2. Contract admission rules (used to cut the three designs down)

| Rule | Meaning in this spec |
|---|---|
| R1 two-consumer | A contract ships with one existing machine behind an adapter and one new use. Otherwise it is "not now" with a trigger. |
| R2 characterize first | Adapter and refactor slices start with a test that is green on HEAD (golden or oracle), then move the code. |
| R3 additive saves | New Parts have public default-constructible fields; old saves lack them. New fields on new Parts may be added later. No retyping or removal ever. |
| R4 data rows roll out | Placement, terms and processes are rows. Reverting a placement is deleting a row. |
| R5 every refusal is named | Each gate has a stable code, a reason string where a player can see it, a `Diag` record, and a test. |
| R6 optional by default | An installation that cannot place omits only itself, records why, and never fails the town. |
| R7 ordinary convenience is untouchable | No slice degrades repair, healing, carrying, basic reads or the shipped forge/still to create demand. Fees and terms exist only on new placements that name a free alternative. |
| R8 canon limits are data | Exempt places, forbidden factions, forbidden stored-work kinds and protected feeds are rows/lists validated at load, not remembered in code. |

---

## 3. Layout, naming, hygiene

- **Namespace:** every new Part, interface, enum and static in `CavesOfOoo.Core` (or `CavesOfOoo.Data` only where a data class already lives there). Folder grouping is free: `Assets/Scripts/Gameplay/{Stations,Installations,Terms,Work}/`. Tests: `Assets/Tests/EditMode/Gameplay/{Save,World,Stations,Installations}/` in namespace `CavesOfOoo.Tests`, files named `Machine<Thing>Tests.cs` / `Installation<Thing>Tests.cs`.
- **Part hygiene (enforced by S1 lint on any Part not in the golden):** namespace `CavesOfOoo.Core`; public parameterless constructor; all public fields `CanSerializeType`; **no field named `ZoneID`, `StationX`, `StationY`, or ending in `ZoneID`/`ReceiverID`** (positional authority is what makes the kitchen family non-relocatable; section 9 hook B1).
- **Diag:** reuse default-on categories `worldgen`, `furniture`, `craft`, `event`, `trade` [R: `Diag.cs:119-120`; V* that `Diag.Record(category, kind, actor:, target:, payload:)` and `IsChannelEnabled` exist]. No new category.
- **Determinism:** `FormationSelector.StableIndex(string key, int count)` (FNV-1a, `Formation.cs:254` [V*]) only. Never `string.GetHashCode()`, never the pipeline `rng`.
- **Perf:** station scans and readers run at menu/action/generation time only; retention runs on relapse only; no per-frame or per-turn allocation is added before S15; scratch lists, no LINQ in scans. Allocation-counter harness cases are known-inconclusive in three tests (`Docs` test receipt correction), so perf is stated as a design property plus a note, never a claim.
- **Living docs:** each slice updates `Docs/MACHINE-<X>.md` in the same commit (CLAUDE.md rule); on acceptance this spec is copied to `Docs/MACHINE-ARCHITECTURE-SPEC.md`.

---

## 4. Contracts

Each contract: problem and evidence, signatures, data, diag, adapters. Slice numbers refer to section 7.

### C1. Saved-Part contract (S1)

Problem: section 1 items 3, 4, 17. Retiring or moving anything saved corrupts later fields in the same entity.

Test-assembly helpers (no production change):

```csharp
// Assets/Tests/EditMode/Gameplay/Save/SavedPartContractReader.cs  (namespace CavesOfOoo.Tests)
internal static class SavedPartContractReader
{
    // Deterministic text. Header "# parts=<n>". One block per non-abstract Part subclass of typeof(Part).Assembly,
    // ordered by FullName ordinal:  "Part <FullName> | reflected" or "Part <FullName> | custom"
    // followed (reflected only) by "  <fieldName>:<FieldType.ToString()>" lines, ordinal order.
    // Field set = result of the private static SaveGraphSerializer.GetSerializablePublicFields(type, null) obtained by reflection.
    // custom = typeof(ISaveSerializable).IsAssignableFrom(type) || type in {StatusEffectsPart, BitLockerPart, InventoryPart, ActivatedAbilitiesPart, Body}.
    internal static string Snapshot();
    // Violations: golden Part missing; golden field missing; field type changed; reflected<->custom flipped. Additions are never violations.
    internal static IReadOnlyList<string> Compare(string golden, string current);
    // New-Part hygiene lint for every Part not present in the golden (section 3).
    internal static IReadOnlyList<string> Hygiene(Type partType);
}
```

Golden file: `Assets/Tests/EditMode/Gameplay/Save/SavedPartContract.txt`, read with `Path.Combine(Application.dataPath, "Tests/EditMode/Gameplay/Save/SavedPartContract.txt")` (existing tests already use `Application.dataPath` [V*: grep]). `[Explicit] GenerateSavedPartContract` regenerates it; the regeneration procedure is documented in the slice's doc.

### C2. Installation marker and generic retention (S2)

Problem: section 1 item 8. A stateful machine in a relapse-capable village is regenerated away, resetting finite state; this contradicts finite custody (`SYSTEMS-DESIGN-PRINCIPLES.md` line 11).

```csharp
namespace CavesOfOoo.Core
{
    public sealed class InstallationMarkerPart : Part
    {
        public override string Name => "InstallationMarker";
        public string InstallationId = "";   // spec row id; "player:<entityId>" for later player installs
        public string Origin = "Generated";  // Generated | Player | Relocated
        public int Revision = 1;             // spec Revision seen at placement
        public bool RetainZone;              // true: a cached village containing this entity is never unloaded for regeneration

        // Pinned legacy fixtures that cannot take a Part (exact-Part validators, e.g. OilStrainingPart.Payload == 5 Parts).
        internal static readonly string[] LegacyRetainedBlueprints = { "PeatOilStrainer", "AbandonedFieldWorkRig", "CinderholdOilRecoveryTray" };
        internal static bool Retains(Entity e);   // marker.RetainZone || Origin != Generated || blueprint in the legacy list
        // The one new CanUnloadZone term. Cached zone only; marker index first, then a name scan of the zone (relapse-time only, cheap).
        internal static bool Retain(ZoneManager manager, string zoneID);
    }
}
```

`OverworldZoneManager.CanUnloadZone` gains exactly one conjunct: `&& !InstallationMarkerPart.Retain(this, zoneID)` (same shape as `CurationIntakePart.Retain` [V*, `:1241-1244`]). Diag `worldgen/ZoneRetained { zone, reason: "marker" | "legacy-blueprint", count }` each time it fires.

Policy (decided here): **retention is per installation kind, not blanket.** A stateless forge in Tine regenerates identically after relapse (deterministic builder) and the town keeps refreshing its build-time stock rolls; a state-bearing kind (any `RepairablePart`, container/inventory, `ContainedLiquidSourcePart`, kitchen/work-job/work-store Part) sets `RetainZone=true`; a player-installed or relocated machine always retains. The legacy blueprint list protects shipped finite fixtures (strainer, rig wherever it was hauled, Cinderhold tray) without editing their pinned validators or saves. Consequence disclosed: a retained village no longer re-rolls build-time stock after relapse; well, oven and lantern visuals still update because `WellSitePart` reads live site stage [R: `WellSitePart.cs:34-57`].

No `State`/`OwnerId` fields now. They are additive later (R3), so reserving them buys nothing.

### C3. Installation registry, census and generic placement (S6, S7, S8)

**Data file** `Assets/Resources/Content/Data/Installations.json` (JsonUtility-compatible: arrays of small `[Serializable]` classes; unknown keys are silently dropped, so forbidden fields are *declared* and rejected):

```json
{
  "Version": 1,
  "ForbiddenFactions": ["RotChoir", "CatacombFolk"],
  "Installations": [
    {
      "Id": "gantry-forecourt-forge",
      "Place": "Gantry",
      "Blueprint": "TinkersForge",
      "Retention": "None",
      "Near": ["Well"],
      "Radius": 9,
      "MaxProbe": 48,
      "Ground": ["Grass", "StoneFloor", "Floor"],
      "Revision": 1,
      "Purpose": "A working forge for the crossroads' wagon trade, reachable from the square.",
      "Alternative": "Carried kits and the Cinderhold forge remain; ordinary repair is unchanged.",
      "Basis": "faction row only (surface villagers own forges); mw/lore.md section 5; owner sign-off required"
    }
  ],
  "Exemptions": [
    { "Place": "the First Tent",    "Reason": "monument to a choice; no machine installed", "Source": "Lore/Factions/07_TentRight.md:119" },
    { "Place": "the Quiet's Door",  "Reason": "refuses contact; installed machines by outsiders forbidden", "Source": "mw/lore.md section 6 citing catacomb_village_design.md:409-416" },
    { "Place": "Overworld.4.6.2",   "Reason": "founding village: no open flame, no machine at the plume or wall; fire kinds exempt", "Source": "mw/lore.md section 6" },
    { "Place": "Overworld.12.3.2",  "Reason": "catacomb hub: open-flame work is an obscenity; fire kinds exempt (cold-work kinds need an attach point and a separate owner decision)", "Source": "mw/lore.md sections 4 (M1), 6" },
    { "Place": "Overworld.16.4.2",  "Reason": "Spore-Wedded: no thermal or powered kinds; fire kinds exempt (cold-work kinds need an attach point and a separate owner decision)", "Source": "mw/lore.md section 6" }
  ],
  "Pending": [
    { "Place": "Morrowfast", "Owner": "scene-runtime hook", "Note": "scene-authored zone, returns before the shared pipeline" }
  ]
}
```

C# row/definition types (model: `MaterialProcessRow`/`MaterialProcessRegistry` [V*]; no generic `DefinitionTable` until a second table exists):

```csharp
[Serializable] internal sealed class InstallationRow {
    public string Id, Place, Blueprint, Retention, Purpose, Alternative, Basis, Absolute, Rng, Terms;
    public string[] Near, Ground;
    public int Radius, MaxProbe, Revision;
    public bool Required;                 // declared only so the validator can reject it
}
[Serializable] internal sealed class InstallationExemption { public string Place, Reason, Source; }
[Serializable] internal sealed class InstallationPending   { public string Place, Owner, Note; }
[Serializable] internal sealed class InstallationFile { public int Version; public string[] ForbiddenFactions;
    public InstallationRow[] Installations; public InstallationExemption[] Exemptions; public InstallationPending[] Pending; }

public sealed class InstallationSpec { /* immutable view: Id, Place, Blueprint, Retain (Retention=="Zone"), Near, Radius, MaxProbe, Ground, Revision, Purpose, Alternative, Basis */ }

public static class InstallationRegistry
{
    public static void LoadDefaults();
    public static void ResetForTests();
    // Per-row dropping: one bad optional row never cancels the others (unlike MaterialProcessRegistry, which clears everything).
    public static IReadOnlyList<string> InitializeFromJson(string json);   // returns error codes "E04:gantry-x"
    public static IReadOnlyList<InstallationSpec> For(string place);       // data order
    public static IReadOnlyList<string> Validate(EntityFactory factory, WorldMap map);  // blueprint-level admission; creates nothing
    public static CoverageReport Coverage(WorldMap map);                   // Installed | Present | Exempt | Pending | Unclassified
}
```

Load-time validation (each is a distinct error; the RED set needs at least 24):

| Code | Rejects |
|---|---|
| E01 | malformed file, or row count outside 0..64 |
| E02 | duplicate `Id` |
| E03 | `Id`/`Blueprint`/`Near[i]`/`Ground[i]` not a token (same rule as `MaterialProcessRegistry.Token`) |
| E04 | `Place` is neither a `WorldMapAuthoring.Places[].Name` nor one of the three catacomb village zone ids |
| E05 | `Place` both has a spec row and an `Exemptions` entry |
| E06 | place faction in `ForbiddenFactions`, or place listed in `Exemptions` |
| E07 | `Retention` not `None`/`Zone`; or `None` on a blueprint carrying a state-bearing Part (`RepairablePart`, `ContainerPart`, `InventoryPart`, `ContainedLiquidSourcePart`, `KitchenBatchPart`, later `WorkJobPart`/`WorkStorePart`) (admission) |
| E08 | `Radius` outside 1..12; `MaxProbe` outside 1..128; `Revision` < 1; `Near` longer than 4 |
| E09 | empty `Purpose`, `Alternative` or `Basis`; any of them over 400 chars |
| E10 | `Required: true` (R6), non-empty `Absolute`, non-empty `Rng` |
| E11 | non-empty `Terms` before S10 ships |
| E12 | spec row whose `Place` is a catacomb zone (no attach point yet) |
| E13 | `Exemptions` entry without `Reason` or `Source`; `Pending` entry without `Owner` |
| E14 | blueprint missing, a Creature/Terrain/Takeable blueprint, or carrying `SpatialFootprint` (admission) |

`Near` always has `"Well"` appended as the last fallback, so a spec cannot fail for lack of an anchor in the 16 shared-pipeline villages.

**Coverage census.** `Coverage(map)` classifies the 17 `WorldMapAuthoring.Places` names plus `Overworld.4.6.2`, `Overworld.12.3.2`, `Overworld.16.4.2` as exactly one of: `Installed(ids)` (at least one spec), `Present` (legacy stations exist: `Sill` and `Cinderhold`, pinned by a test that generates both and finds `TinkersForge`), `Exempt(reason, source)`, `Pending(owner)`. A test fails on `Unclassified`, on a place in two classes, and on `Pending` count above a committed constant that may only decrease. Final acceptance for "every town" is `Pending == 0`.

**Builder.**

```csharp
public sealed class InstallationPostBuilder : IZoneBuilder        // IZoneBuilder: Name, Priority, BuildZone(Zone, EntityFactory, Random) [V*]
{
    public string Name => "Installations";
    public int Priority => 4650;                                   // unique in every village pipeline
    public InstallationPostBuilder(OverworldZoneManager manager, PointOfInterest poi, string zoneID);
    public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng);   // never reads or advances rng; never returns false
}
```

Registered by one line at the tail of `CreateVillagePipeline`, after the Quillhold line [V*, `:1163-1165`]: `if (InstallationRegistry.For(poi.Name).Count > 0) pipeline.AddBuilder(new InstallationPostBuilder(this, poi, zoneID));`. Morrowfast is excluded by its early return. Sinkhole villages use other pipelines and stay Exempt/Pending until a spec needs an attach point.

Algorithm per spec, in `Id` data order, all deterministic:

1. **Authority** (silent no-op, return true, no diag: a replay is not an omission): `zone.ZoneID == zoneID`; manager non-null; `ReferenceEquals(factory, manager.Factory)`; `!manager.CachedZones.ContainsKey(zoneID)`; `!ReferenceEquals(manager.ActiveZone, zone)`; `manager.WorldMap` POI at the zone is a village named `spec.Place` (the Quillhold/Sumphold `Current()` shape, minus terrain tokens).
2. **Replay guard:** a marker with this `InstallationId` already in the zone: skip (`Omitted: already-present`).
3. **Admission** (blueprint-level, no entity created): blueprint exists, non-creature, non-terrain, non-takeable, no `SpatialFootprint`, `Retention` consistent (E07/E14). Failure: `Omitted: content`.
4. **Anchor:** first resolvable owner among `Near` (blueprint match in the zone, lowest `(y, x)`); none: `Omitted: no-anchor`.
5. **Candidates:** cells within Chebyshev `Radius` of the anchor, ordered by `(distance, StableIndex(zoneID + ":install:" + spec.Id + ":" + x + "," + y, 1<<30), y, x)`; probe at most `MaxProbe`. A cell passes only if: `geometry.Place(x, y)` (exterior bare ground; `new SpreadWildernessSituationBuilder.Geometry(zone, empty set)`); the cell holds exactly one object and its blueprint is in `Ground` (default `Grass, StoneFloor, Floor`; never `RoadStone`, the public route); no `DoorPart`, `StairsUpPart`, `StairsDownPart` or `Well` within Chebyshev 1; and, **only if the blueprint is solid**, `geometry.PreservesRoutes(new[]{(x, y)})` plus at least one of the 8 neighbours reachable from `BorderReach` (a standing cell). Non-solid blueprints (forge, still) skip the route and standing checks (V12).
6. **Create exactly once** inside try/catch (no plan-time create-and-discard probe). Reject (`Rejected: fresh`) if the entity has no ID or carries `InventoryPart`, `ContainerPart`, `TraderPart` or `StackerPart`. A throwing factory or creation callback: `Omitted: create-failed`.
7. **Stage and commit as a receipt** (Sumphold/Quillhold pattern [V*]): capture `originals`, `SpreadGenerationReceipt.CaptureFinalState(zone, originals)`, `zone.TileState.ToSaveString()`, a copy of `GenReservedCells` and `zone.EntityCount`; add the marker (`RetainZone = spec.Retain`, `Origin = "Generated"`, `Revision`), set `Properties["SettlementId"] = zoneID`, `zone.AddEntity(entity, x, y)`; then require originals intact, tile state and reservations unchanged, `EntityCount == before + 1`, entity cell as chosen. Any mismatch: `zone.RemoveEntity(entity)`, `Omitted: displaced-original`.
8. After success, add the standing cell (solid kinds only) to `zone.GenReservedCells` so later generation steps avoid it [U: confirm no post-commit step compares reservations; `LaterBuildersAvoidStandingCell` decides; drop the step if it conflicts].
9. `Diag worldgen/InstallationPlaced { id, place, zone, x, y, blueprint, anchor, probed }`.

Failure table (R5/R6):

| Condition | Outcome | Diag (`worldgen`) | Player sees |
|---|---|---|---|
| row invalid at load | row dropped, others load | `InstallationRejected { id, code }` | nothing |
| authority fails (cached/active/wrong zone/factory) | silent no-op | none | old towns never gain a machine |
| content missing or malformed | omit this row, return true | `InstallationOmitted { reason: "content" }` | town unchanged |
| no anchor / no fitting cell | omit | `Omitted { reason: "no-anchor" \| "no-cell", probed }` | town unchanged |
| create throws | discard, omit | `Omitted { reason: "create-failed" }` | town unchanged |
| fresh check fails | omit | `InstallationRejected { reason: "fresh" }` | town unchanged |
| receipt mismatch | rollback, omit | `Omitted { reason: "displaced-original" }` | town unchanged |
| pipeline retry (another builder returned false) | `ClearZone` removed us; builder keeps no state; places again identically | `Placed` again | same |

Old worlds: **no exploration-version gate and no `SpreadExplorationPlan.CurrentVersion` change** (bumping it without adding 17 to the restore list makes every v17 world throw on load [R: `SpreadExplorationPlan.cs:39,347`]). An unvisited town in an old world receives its installation on first visit (the Cinderhold packet precedent [R]); a town already generated and cached never does. That limit is stated in every placement slice's evidence.

### C4. Declarative action time and disabled rows (S3)

Problem: section 1 item 5. A new command must be added to two OR-chains or it silently refuses; unavailable rows vanish, so a player cannot tell "no forge here" from "the forge is fouled".

```csharp
public enum ActionTime : sbyte { Unspecified = -1, Free = 0, OneAction = 1 }   // rounds arrive with a second consumer

public class InventoryAction {                                  // existing fields unchanged
    public ActionTime Time = ActionTime.Unspecified;            // Unspecified: the legacy chains decide; declared: they are bypassed
    public string DisabledCode = "";                            // stable token, e.g. "faulted"
    public string DisabledReason = "";                          // one sentence; non-empty => row is disabled
    public bool IsDisabled => !string.IsNullOrEmpty(DisabledReason);
}
public class InventoryActionList {
    public InventoryAction AddAction(string name, string display, string command, char key = '\0', int priority = 0,
                                     bool fireOnActor = false, ActionTime time = ActionTime.Unspecified);   // now returns the row
    public InventoryAction AddDisabled(string name, string display, string code, string reason, char key = '\0', int priority = 0);
}
```

Behaviour:

- **Disabled row** (both menus): listed and dimmed; selecting it (Enter, click or hotkey) logs `DisabledReason` to `MessageLog`, records `event/ActionDisabled { code, command, name }`, spends no turn, opens no transaction and **keeps the menu open**. World menu: in `WorldActionMenuUI.SelectAction(int)` (`:277`), a disabled row returns before `_selectionMade = true`; hotkey selection must route through the same method [U: read `MenuShortcutMap` use]. Pack menu: the first lines of `ExecuteItemAction` for the row. `Disabled` wins over `Time`.
- **Declared time, world menu:** one clause placed **after** the reach gate (`InputHandler.cs:3202-3210`) and before the legacy chain: `ExecuteCommand(PerformInventoryActionCommand(target, command))`; on success `OneAction` calls `EndTurnAndProcess()` and `RequestZoneRedraw(...)`, `Free` only redraws; then `_inputState = _worldActionMenuReturnState`.
- **Declared time, pack menu:** `ItemAction` gains `Time`, `DisabledCode`, `DisabledReason`, copied at `InventoryUI.cs:1219`; in the charge test after `TryPerformActionViaCommand`, `spend = action.Time != Unspecified ? action.Time == OneAction : <legacy chain>`.
- The legacy chains are **not deleted** in this contract. Producers migrate by declaring `Time`; a parity table proves each migrated command charges what its chain charged. Deletion is a later cleanup after the parity suite has stayed green for a milestone.
- `WorldAffordanceQuery` "matches the sealed menu provider without firing `GetInventoryActions`" [V*]; it enumerates no rows, so it is unaffected.

### C5. Station contract (S4, hardened in S9)

Problem: section 1 items 2, 12. Three scans with three readiness meanings; the rig is special-cased inside `ForgeWorkReceipt.Ready`; mineral preparation and brewing use only a presence check, a weaker guarantee than `ForgeWorkReceipt.Begin` gives the same forge [R].

```csharp
public enum ReadinessMode { Strict = 0, PresenceOnly = 1 }
// Strict: bit-for-bit ForgeWorkReceipt.Ready/Near/Resolve semantics (actor via GetEntityCell, Current(zone) required).
// PresenceOnly: bit-for-bit AlchemyStillPart.IsNearStill semantics (actor via GetEntityPosition, no Current(zone), no readiness,
//               provider presence only). Kept until the S9 oracle proves the difference is unreachable in shipped play.

public interface IStationCapability                  // implemented by a Part
{
    IReadOnlyList<string> Kinds { get; }             // kebab tokens: ForgePart => {"forge","cold-work"}; AlchemyStillPart => {"still"}
    int Reach { get; }                               // Chebyshev cells; 1 for every shipped station
    ReadinessMode Readiness { get; }
}
public interface IStationReadinessGuard             // implemented by any Part that owns a physical veto (the rig payload; later fuel, loose state)
{
    bool TryBlock(Entity station, Zone zone, out string code);   // pure: no mutation, no events
}
public interface IStationAccessGuard                // the permission seam; implemented by ServiceTermsPart in S10
{
    bool TryDeny(Entity actor, Entity station, Zone zone, string kind, string command, out string code, out string reason);  // pure
}

public readonly struct StationCandidate { public readonly Entity Station; public readonly string BlockCode; public bool Ready => BlockCode == null; }

public static class StationReadiness
{
    // Strict structural + physical clauses VERBATIM from ForgeWorkReceipt.Ready (zone current; cell round-trip; owner.SpatialZone == zone;
    // exactly one Part providing `kind` and parented to owner; RepairablePart.BlocksFunction; DraggedPart; HarvestablePart.Harvested;
    // DestructiblePart Gone or HP<=0), then every IStationReadinessGuard on owner.Parts. Codes:
    // "stale-zone","cell-mismatch","no-provider","two-providers","faulted","hauled","harvested","destroyed","rig-payload", plus guard codes.
    public static bool Evaluate(Entity owner, Zone zone, string kind, out string code);
}
public static class StationQuery
{
    public static Entity Resolve(Entity actor, Zone zone, string kind);              // first Ready provider; scan order dx -1..1 outer, dy -1..1 inner, cell.Objects order (legacy)
    public static bool Near(Entity actor, Entity owner, Zone zone, string kind);      // Ready and SpatialQuery.Distance <= provider Reach
    public static void Collect(Entity actor, Zone zone, string kind, List<StationCandidate> into);  // scratch list; includes unready with BlockCode; no LINQ
}
public static class StationAccess { public static bool Allowed(Entity actor, Entity station, Zone zone, string kind, string command, out string code, out string reason); }
public sealed class StationReceipt                  // = ForgeWorkReceipt.Begin generalised
{
    public Entity Station { get; }
    public static StationReceipt Begin(Entity actor, Zone zone, InventoryTransaction tx, string action, string kind, Entity selected = null);
    // 1) StationAccess.Allowed (before any claim) 2) Near 3) tx.TryClaim(actor), tx.TryClaim(station)
    // 4) capture ID, BlueprintName, Parts sequence, both cells; tx.BeforeCommit(re-prove identity, cells, Near, actor alive, action not blocked)
    // Refusal returns null and records furniture/StationRefused { code, kind, command }.
}
public sealed class StationPart : Part, IStationCapability   // S13: new benches only; legacy machines implement the interface on their own Part
```

Adapters (behaviour-preserving, S4): `ForgePart : IStationCapability, IStationReadinessGuard` (the rig clause `!IsRig || Payload` moves here from `ForgeWorkReceipt.Ready`); `AlchemyStillPart : IStationCapability` with `PresenceOnly`; `ForgeWorkReceipt.Ready/Near/Resolve/Begin` and `ForgePart.IsNearForge` and `AlchemyStillPart.IsNearStill` become one-line forwards. **`WorldAudioWork.Forge` is not migrated**: it answers a different question (an audible workbench in contact, with its own capture rules) and is documented as the third, intentionally independent scan. The rig keeps its other references until its own slice (S19 area). Forge verb commands keep calling `ForgeWorkReceipt.Near` unchanged in S4.

Disabled-row adapter (S4): `ForgePart.HandleEvent("GetInventoryActions")` today returns no rows when `IsRig && !Near`. New behaviour: when the rig is adjacent but `StationReadiness.Evaluate` fails, emit `AddDisabled("Craft", "Work the rig (...)", code, reason)` with reasons "The rig is fouled and needs repair first." (`faulted`), "The rig is being hauled." (`hauled`), etc.; when the actor is simply not adjacent, rows stay omitted exactly as today. Reason strings are engine-neutral; any faction-voiced text passes the blind voice gate (lore R10).

Permission seam: `StationAccess.Allowed` loops `IStationAccessGuard` Parts on the station. With no guard it allows, so every shipped station keeps today's open behaviour. The seam is tested in S4 with a test-only guard Part; enforcement semantics arrive in S10 after the independent critique, and only reach a user once S9 has moved every station user onto `StationReceipt.Begin`.

### C6. Reader contract (S5; "Works here" in S13)

Problem: section 1 item 6. Every new machine edits two builders by hand.

```csharp
public enum DescribeMode { Quick, Document }
public interface IDescribeState { void Describe(DescribeContext context); }       // implemented by Parts
public sealed class DescribeContext {
    public Entity Owner, Viewer; public Zone Zone; public bool InWorld; public DescribeMode Mode;
    public void State(int order, string line);    // "Current state"
    public void Uses(int order, string line);     // "Use & mechanics" (Document only)
    public void Limits(int order, string line);   // what this does not do
    public void Access(int order, string line);   // terms (S10)
}
public static class StateReader { public static DescribeResult Collect(Entity owner, Entity viewer, Zone zone, bool inWorld, DescribeMode mode); }
```

Lines are ordered by `(section, order, insertion)` and de-duplicated by exact text; a throwing describer is isolated and skipped with `event/DescribeFailed`. Legacy order constants preserve today's text order: film 100, kitchen 110, dressing 120, oil strainer 130, contained liquid 140, repair 150, trap 160; new contributors use 200+. Both `ExaminablePart.BuildDescription` and `ExamineDocumentBuilder.Build` call `Collect` for the Part-contributed lines only; their other sections (intent, threat, effects, ground, flavor, directions, facts) are untouched. Legacy Parts implement `IDescribeState` as a three-line method calling their existing `Describe()`.

Expected, stated up front: the document gains exactly the film and contained-liquid lines; all other bytes are identical. If S5's RED shows the omission was deliberate, the document keeps omitting them via `Mode`.

`InstallationMarkerPart : IDescribeState` adds one neutral line ("Set here to be used.") [draft; voice gate]. The derived "Works here: <ActionText of each process this station provides>" line is generated from `MaterialProcessRegistry` in S13 (it needs the work-kind table), closing `presentation.md` finding 6 without a new execution path.

### C7. Service terms (S10; gated)

Problem: `ForgeWorkReceipt.Ready` checks physical state only while the Weaponsmith promises "for a paying customer I'll let you work it" (dialogue only) [R: `Shopkeepers.json:8`]; three staffed paid services re-implement operator presence and a fee. Canon supplies three idioms (Concord contract/release, Tent-Right gift, Pale Curation withheld) [R lore.md section 10].

```csharp
public sealed class ServiceTermsPart : Part, IStationAccessGuard, IDescribeState   // attached to placed instances; absent = Open
{
    public override string Name => "ServiceTerms";
    public string Access = "Open";        // Open | Fee | Gift | Withheld | Owner (Owner reserved: unregistered => denied, fail closed)
    public int Fee;                       // drams per committed use; only Access == "Fee"
    public string OperatorBlueprint = ""; // a living, friendly, same-zone person with this blueprint within OperatorWithin must be present
    public int OperatorWithin = 3;
    public string OperatorID = "";        // bound by ID after population; never by display name (LocalPeople renames residents)
    public string Reason = "";            // Withheld refusal and reader line (voice-gated)
    public string OwnerID = "";           // reserved for home/base
}
```

Rules to decide under critique, with this spec's default proposal: `Fee` is paid to the present operator with `tx.DeferCurrencyTransfer(actor, operator, Fee)` (precedent `KitchenBatchPart.cs:352` [R]); there is **no new dram sink**; a dead, absent or hostile operator closes the station, never frees it; terms only refuse use (no theft law, no reputation penalty; none exists and Tent-Right canon rejects property crime); `Gift` cannot carry a fee (load rejection) and is the only idiom valid in Tent-Right places (`Fee` rejected there; water is never priced); `Withheld` refuses with its authored reason before any input is touched; `Owner` is unregistered until the home design registers it. Shipped forges and stills stay Open. A non-Open spec must name a free `Alternative` (already mandatory in the schema).

Questions the independent critique must answer before RED: who is paid and may a sink ever exist; operator-presence rule (distance, sight, friendliness, absent-keeper UX); refusal copy and the blind voice gate; interaction with salvage rights ("no communal-service salvage" in `MACHINE-RECLAMATION-PROCESSING.md:50` [R]); the spoken-end verb for any standing arrangement (lore R9); how a Fee shows in the quote (S14).

### C8. Process engine contracts (S12, S13, S14)

1. **Work-kind table (S13).** Replace the three literal switches with one static code table: `"ForgeCold" -> (station kind "cold-work", participant role "Station")`, `"HandCutting" -> (carried cutting tool, role "Tool")`. Adding a third kind is one table row plus, if the receipt differs, one class. No `WorkKinds.json`, no provider registry: two rows do not justify a loader. Trigger for the registry: the first process row whose kind is neither of these. The engine resolves the `ForgeCold` station through `StationQuery.Resolve(actor, zone, "cold-work")` / `StationReceipt.Begin(..., "cold-work")`, so a `StationPart` bench with `cold-work` works with no fire. `ForgePart.Factory` coupling stays (section 1 item 15).
2. **Multi-stack ingredient claims (S12; gated on critique).** `IngredientClaim` replaces `MaterialProcessIngredient` for the source and extra inputs:

```csharp
internal sealed class IngredientClaim {
    internal readonly (Entity Owner, int Units, int OriginalCount, MaterialProcessItemState State)[] Takes;  // ordered
    internal static IngredientClaim Resolve(Entity actor, Entity anchor, string blueprint, int units, EntityFactory factory, out string reason);
    internal bool Before(Entity actor);   // every take still carried, count and state unchanged
    internal bool After(Entity actor);    // every take reduced by exactly its units; emptied stacks fully detached
}
```
   **Parity rule:** wherever the legacy resolver succeeds (one carried stack with enough units; several only if all plain), the result is that exact choice. Pooling engages only where the legacy returns null for lack of one sufficient stack, and only across stacks of one blueprint that `MaterialProcessPayloadPolicies.CanMergeProcessingInputs` admits pairwise (the very predicate that already decides ordinary merges, treating inert `ThrownRecoveryPart`/`StatusEffectsPart` as non-payload and refusing live ones [R: `:61-76`]). Order: anchor first, then siblings by ID ordinal. No state is laundered or erased; incompatible stacks give reason `stacks-incompatible`. Rollback is the existing `InventoryTransferSnapshot.Capture(pack, ...)`. Reason codes feed disabled rows: `short`, `held`, `stacks-incompatible`, `no-station`, `no-tool`, `terms`, `content`. Alternative rejected: making `AddRetrievedObject` merge (core inventory change, wrong blast radius, covers only one cause of split stacks).
3. **Quote and modifier channel (S14).** `ProcessQuote` (effective input/output counts seeded from the definition, `Costs`, `Applied`, `Revision`); `ModifyProcessQuote` event fired on actor, station/tool and source **before the preview is built** and again at commit; handlers may lower an input count (min 1), raise an output count within a row-declared `Modifiable` bound, add a declared cost, or refuse; they may never introduce a blueprint the row did not declare. `Quote.Current()` includes `Revision`, so a modifier that appears or changes between preview and payment refuses the commit. `BeforeMaterialProcess` stays the late veto; `MaterialProcessCompleted` gains `Outputs`. This implements the principle line the audit found unimplemented (`SYSTEMS-DESIGN-PRINCIPLES.md` line 11). No production modifier ships with S14; the first qualified consumer brings its own.
4. **Row schema v2** (arrays of inputs/outputs up to 4/4, `WorkCost`, `OnFull`) ships only with the first qualified recipe that exceeds today's 2 extra inputs / 2 outputs / 64 units; the v1 loader normalises into v2 so the six shipped rows stay byte-identical. Not scheduled (limit change is a product decision).

### C9. Elapsed work and jobs (S15, S16)

Policy (verified in docs and kitchen code [R]): one world clock, lazy bounded reconciliation on arrival, after a paid turn and on load; no away simulation; time never creates another job; a full receiver blocks, it never destroys or spills; interruption returns what remains and keeps the fee.

- **S15 reconciler registry (hazard):** `WorldTime.Register(id, Action<Zone,int>, order)` and `WorldTime.ReconcileZone(zone, tick)` replace the four paired `CropTime`/`KitchenBatchPart` call sites (`InputHandler.cs:1204-1205,1318-1319`, `GameBootstrap.cs:728-729,1033-1034` [R]) preserving order Crop then Kitchen; a throwing reconciler is isolated with `furniture/ReconcileFailed`. `CropSystemPart.cs:31`'s crop-only fallback stays.
- **S16 `WorkJobPart`** lifts the kitchen's generic fields (`State, StartTick, DueTick, JobOrdinal, BlockedReason`, escrow lists, receiver, operator ids) with the kitchen's semantics, reconciled through the registry; per-turn cost is the existing part index returning an empty list in the common case. `KitchenBatchPart` registers as a reconciler and gains a read-only view; it stays zone-pinned and classified non-relocatable (stated limit). Ships with the first timed machine, not before.

### C10. Receivers, waste, unit conversion (S17) and stored work (S18)

Both wait for their first consumer (two-consumer rule). Contract sketches live in section 7. Canon-forbidden vocabulary is rejected by validation: any kind, item or text naming `charge`, `mana`, `battery`, `quartz`, `pyrite`, `urqu`, `light`, `glow`; no feed whose item carries Glow-Quartz, Sari-Pyrite, hearth-patch or Founding-Plume identity; conversions only as explicit rows with `ToUnits <= FromUnits` and no cycle. The candidate vocabularies from the three designs (heat/tension/head; hand/fire/wound; heat/wound/head/banked) agree only on "stored mechanical or thermal work, never charge"; the owner decides (`lore.md` section 11 item 1). Overflow on feeding is refused, never clamped or lost.

### C11. Bridge contracts for home/base (S19, S20)

- `InstallablePart { Mode: Fixture | Portable, State: Installed | Loose }`, `IRebindable { CanInstallAt, Rebind }`, events `BeforeInstall`/`Installed`/`Uninstalled`/`StationRebound`. A player install adds an `InstallationMarkerPart { Origin = "Player", RetainZone = true }`, so a base zone is retained by the C2 predicate with no special case. `Loose` vetoes readiness through an `IStationReadinessGuard`. The kitchen family stays non-installable (stated). `RigRelocation` stamps `Origin = "Relocated"` after a committed crossing and calls a no-op `Rebind`.
- NPC use: `StationPerception.Perceives(actor, station, zone)` keeps the exact `Cell.IsVisible && Cell.Explored` rule for the player and uses same-zone adjacency for others; `AIStationKeeperPart : AIBehaviorPart` on `AIBoredEvent` and `UseStationGoal : GoalHandler` copy `AIUndertakerPart`/`DisposeOfCorpseGoal`; the NPC runs the same `PerformInventoryActionCommand` as the player. NPCs only act while the player is present (leaving a zone removes its creatures from the scheduler [R]); no offscreen production.

---

## 5. Event and diag catalogue

New `GameEvent`s are deliberately few. Seams are interfaces, so Part order never matters.

| Name | Kind | Where | Payload |
|---|---|---|---|
| `IStationCapability`, `IStationReadinessGuard`, `IStationAccessGuard` | interfaces | S4 | see C5 |
| `IDescribeState` | interface | S5 | see C6 |
| `ModifyProcessQuote` | event | S14 | `Actor, Source, Station, Tool, Process, Quote` |
| `MaterialProcessCompleted` | event (exists) | S14 adds | `+ Outputs, Quote` |
| `BeforeInstall`, `Installed`, `Uninstalled`, `StationRebound` | events | S19 | `Actor, Station, Zone, Cell, Origin` |
| `StructureRepaired` | event | S22 | `Actor, Owner, RecipeId, Zone` (fired after commit; the hard call to `ConnectedSpreadProgress.RecordRepair` stays) |

Diag records (default-on categories; every gate emits one, per CLAUDE.md observability):

| Record | Category | Slice | Payload |
|---|---|---|---|
| `ZoneRetained` | worldgen | S2 | `zone, reason, count` |
| `ActionDisabled` | event | S3 | `code, command, name` |
| `StationRefused` | furniture | S4 | `code, kind, command, station` |
| `DescribeFailed` | event | S5 | `part, mode` |
| `InstallationPlaced / Omitted / Rejected` | worldgen | S6, S7 | see C3 |
| `StationAccessDenied` | furniture | S10 | `policy, code, kind` |
| `StationFee` | trade | S10 | `station, operator, fee` |
| `ProcessRefused` | craft | S12 | `process, reason` |
| `ReconcileFailed`, `WorkJobStateChanged` | furniture | S15, S16 | `id` / `job, old, new` |

---

## 6. Adapter table: every shipped machine

Rule: the adapter slice contains the legacy function copied into the test as the oracle; the machine's existing suites pass **untouched**; no saved public field is added, removed or retyped on any existing Part.

| Shipped machine | Slice | Implements / receives | What stays exactly as is | Oracle / pin | Save impact |
|---|---|---|---|---|---|
| `ForgePart`, `ForgeWorkReceipt` | S4 | `IStationCapability` (forge, cold-work; Strict), `IStationReadinessGuard` (rig payload); `Ready/Near/Resolve/Begin` forward to `StationQuery`/`StationReceipt` | forge verbs, rows, `StationId` in quote commands; `ForgeWeapon/Reforge/TemperWeaponCommand` keep calling `Near` | truth table of every `Ready` clause + two-adjacent-forge order | none |
| `AlchemyStillPart` | S4 | `IStationCapability` (still; PresenceOnly); `IsNearStill` forwards | brewing rules, batching, leniency (a hauled or destroyed still still counts) | oracle copy of `IsNearStill` | none |
| `WorldAudioWork.Forge` | none | not migrated (different question) | row-major scan, audio capture rules | existing audio suites | none |
| Field work rig, `FieldWorkRigRules` | S4 (readiness, disabled row), S19 (rest) | the readiness clause moves into `ForgePart`'s guard; other 19 references stay | stats, yields, recipe, relinquish text, refit | `MachineFieldWorkRigTests`; hauled rig keeps its zone via legacy list (S2) | none |
| `RigRelocation` | S19 | stamps `Origin="Relocated"`, calls no-op `Rebind` | two hauling rounds, plain-cell rule, one-border limit | existing 193-case receipts + new retention pin | none |
| `OilStrainingPart` | S3 (row declares `OneAction`), S5 (reader) | `IDescribeState` delegating to `Describe()`; blueprint-name retention only | exact 5-Part payload, 2-to-1 transaction, player-only gate, no marker Part | golden reader text | none |
| `ContainedLiquidSourcePart`, tray | S2, S5 | legacy retention; `IDescribeState` (full document gains the contained-oil line) | custody table, recovery flow, fresh-only profile entry | golden text | none |
| Reclamation (`CinderholdReclamation`, released tool, offcuts) | none | unchanged; census lists Cinderhold as `Present` | plan-owned profile entries stay Path A | M3a/b suites | none |
| Material-process rows (6) | S13 (work-kind table), S12 (claims) | table lookup; claims reuse `CanMergeProcessingInputs` | every row id, count, text, permission Part | v1 definitions equal field by field; event parameter table (`Station` vs `Tool`) | none |
| `KitchenBatchPart` | S15, S16 | reconciler registration; read-only job view | zone pin, fee 2, 120 ticks, escrow, art query | reconcile-order golden | none |
| Repairs (`RepairablePart`) | S3, S5, S22 | declared `OneAction`; `IDescribeState`; later `StructureRepaired` | one-way semantics, recipes, `RecordRepair` call | `BlocksFunction` oracle | none |
| `MineralPreparationPart`, `BrewReagentsCommand`, `WeaponSalvagePart` | S4 forward, S9 harden | presence checks forward in S4 (unchanged semantics); S9 moves them to `StationReceipt.Begin` | their results for every shipped scenario | S9 callback-moves-station RED | none |
| Staffed services (`SoddenPreparationPart`, `BotanicalInkDeskPart`, `LocalGatheringClaimPart`) | none | reference idioms for `ServiceTermsPart` only | everything | existing suites | none |
| Torch/campfire fuel | S18 | feeds read from data with identical constants (10, 25) | burn rules | feed-equals-legacy-constant pin | none |
| `CuttingToolPart` | S13, S21 | work-kind row; first `PartUpgrade` entry | `DefinitionSeen` marker, split/merge guards | upgrade idempotence | none |

---

## 7. Ordered slice plan

Cadence: one cycle is one isolated-clone EditMode run (about 5 minutes including compile and reload). Every slice is RED-first (a compile error counts as RED), carries a counter-check per positive assertion, an adversarial file when two or more taxonomy surfaces apply, a cold-eye pass with both audit angles, and diag pins. Every generation slice also needs actual play in a fresh world plus a cached-save control, with the project's usual limits (CUA-only evidence, no packaged performance claim, Linux/Deck unexecuted).

Dependency lanes (arrows mean "needs"): S2->S1; S6->S1; S7->S2,S6; S8->S7; S3 independent; S4 independent (S3 for the disabled-row adapter); S5 independent; S9->S4; S10->S9,S4,S5 and the critique; S11->S8,S10; S12->S3,S4 and critique; S13->S4,S12(for reasons); S14->S12; S15 independent; S16->S15; S17->S16 (first consumer); S18->canon decision; S19->S2,S4; S20->S4,S10; S21->S1; S22->S4; S23->S7; S24->S8.

| Slice | One-line value | Cycles |
|---|---|---|
| S1 | removing/renaming a saved Part or field is a failing test | 1 |
| S2 | stateful installations and legacy finite fixtures survive relapse | 1-2 |
| S3 | declared turn cost; disabled rows with reasons | 2 |
| S4 | one station query/receipt; permission seam; rig disabled row | 2-3 |
| S5 | both readers agree; new Parts appear in both with no builder edit | 2 |
| S6 | validated installation specs and the every-town census | 1 |
| S7 | usable forge in the first towns, deterministic, retained, optional | 2 + play |
| S8 | remaining eligible rows and exemptions by data | 1 |
| S9 | every station user on one receipt (hazard removal) | 1-2 |
| S10 | service terms with Open/Gift/Withheld then Fee (gated) | 2 + 2 |
| S11 | terms-bearing placements in Concord/Curation/Tent-Right places | 1 |
| S12 | vanished process rows return; blocked rows say why (gated) | 2-3 |
| S13 | work-kind table, "Works here", `StationPart` cold bench | 2 + 2 |
| S14 | quote modifiers act before preview and payment | 2 |
| S15 | one elapsed-time reconcile call | 1 |
| S16 | generic timed job, kitchen as read-only view | 2 |
| S17 | receivers/waste/conversions (with first consumer) | 2 |
| S18 | stored work (canon-gated) | 1-2 |
| S19 | installable state and verbs (home/base) | 2 |
| S20 | NPC perception and keeper | 2 |
| S21 | `PartUpgrade` registry | 1 |
| S22 | repair event and site-binding facade | 1 |
| S23 | machine classifier and glyph ratchets | 1-2 |
| S24 | in-room anchors and voice-gated mention | 2 |

### S1. Saved-Part contract golden (test-only; 1 cycle)

- **Goal.** Turn "never remove, rename, move or retype a saved Part or field" into a failing test before any Part is added.
- **Files.** NEW `Assets/Tests/EditMode/Gameplay/Save/SavedPartContractReader.cs`, `SavedPartContractTests.cs`, `SavedPartContract.txt`. No production file.
- **RED first.** `SavedPartContract_GoldenFileExists` (fails before generation); `SavedPartContract_EveryGoldenPartAndFieldStillExistsUnchanged`; `SavedPartContract_HeaderCountMatchesBlocks` (guards truncation); `SavedPartContract_CustomSerializedPartsListedWithoutFields`; checker self-tests proving the test is not vacuous: `Compare_MissingPart_IsViolation`, `Compare_MissingField_IsViolation`, `Compare_RetypedField_IsViolation`, `Compare_MovedNamespace_IsViolation`, `Compare_ReflectedToCustomFlip_IsViolation`, `Compare_AddedPart_IsNotViolation`; `Hygiene_WrongNamespace_Violation`, `Hygiene_NoParameterlessCtor_Violation`, `Hygiene_PositionalAuthorityField_Violation` (`ZoneID`, `StationX`, `StationY`, `*ReceiverID`), `Hygiene_ConformingPart_NoViolation`, using synthetic nested types.
- **Counter-checks.** Each violation test has an identical-setup control that passes.
- **Acceptance.** Native green on HEAD; `SavedPartContract.txt` and its `.meta` committed; a doc paragraph states the regenerate procedure (`[Explicit] GenerateSavedPartContract`) and that regenerating to hide a removal requires owner approval.
- **Depends on.** none. **Risk.** low (reflection over private `GetSerializablePublicFields`; if the method signature differs the first cycle finds out). **Honesty.** custom-serialized Parts are covered by name only; their wire format is covered by existing save tests. Effects and goals are saved by the same reflection but are out of scope (this program adds Parts).

### S2. Installation marker and generic retention (1-2 cycles; hazard removal)

- **Goal.** A cached village that contains a retaining marker or a pinned legacy fixture is not unloaded on relapse.
- **Files.** NEW `Gameplay/Installations/InstallationMarkerPart.cs`; MOD `OverworldZoneManager.CanUnloadZone` (+ one conjunct); NEW `Assets/Tests/EditMode/Gameplay/World/InstallationRetentionTests.cs`, `InstallationRetentionAdversarialTests.cs`.
- **Step 0 (premise, before any code).** `RelapseRegeneratesUnmarkedVillage_Control`: `OverworldZoneManager.CreateDetached(scope.Factory, 64, true)`; `GetZone` Tine (13.7); put a structure site in `TemporarilyPurified` with `RelapseAtTurn` in the past through the same `SettlementManager` calls `SettlementManagerTests`/`PlaceSiteTests` use [U: exact calls]; a second `GetZone` while not active must return a **different instance**. If it returns the same instance, the hazard is narrower than claimed: record that, keep S2 for the legacy blueprints and player installs only.
- **RED first.** `MarkedVillageKeepsZoneInstanceAcrossRelapse`; `MarkedEntityStateAndIdentitySurviveRelapse`; `RetainZoneFalseMarkerDoesNotRetain` (counter); `PlayerOrRelocatedOriginAlwaysRetains`; `LegacyBlueprintsRetainTheirZone` for `PeatOilStrainer`, `AbandonedFieldWorkRig`, `CinderholdOilRecoveryTray`; `HauledRigInsideRelapsingVillageIsRetained` (the V7 path: a rig placed in Tine); `UnlistedBlueprintDoesNotRetain`; `ZoneWithoutAnyMarkedEntityStillRegenerates` (Sill control); `RetentionSurvivesSessionRoundTrip` (`GameSessionState` capture/save/load then relapse); `MarkerPartRoundTripsPublicFields`; `SaveWithoutMarkerLoadsAndIsNotRetained`; `ZoneRetainedDiagFiresWithReason`; existing `SecondExploration*Retention*`, `SoddenDistrictIntegration`, `CurationIntake` retention suites unchanged and green.
- **Adversarial.** marker on an entity not in any zone; two markers, one retaining; retaining entity removed from the zone, retention ends; null manager and unknown zone id safe; marker added after load; `Retain` never throws; unrelated predicates unchanged.
- **Acceptance.** Native green; actual play (fresh world): a marked fixture in Tine survives a Weir relapse with the player away 150+ turns, identity and state intact; an old save with no markers is untouched (control); F5; fresh Continue.
- **Depends on.** S1 (the new Part enters the golden as an addition and passes the hygiene lint). **Risk.** low-medium (a deliberate, narrow behaviour change). **Doc.** `Docs/MACHINE-INSTALLATIONS.md` (new, living).

### S3. Declarative action time and disabled rows (2 cycles)

- **Goal.** A row can declare its turn cost and a reason it is unavailable; the legacy chains keep working for undeclared rows.
- **Files.** MOD `Gameplay/Inventory/InventoryAction.cs` (C4); MOD `Presentation/UI/WorldActionMenuUI.cs` (dim, `SelectAction` guard); MOD `Presentation/Input/InputHandler.cs` (one clause after the reach gate); MOD `Presentation/UI/InventoryUI.cs` (`ItemAction` fields, copy at `:1219`, disabled guard, charge test); NEW `Assets/Tests/EditMode/Presentation/Input/ActionTimeDeclarationTests.cs`, `ActionTimeAdversarialTests.cs` using the existing reflection fixtures (`OpenWorldActionMenuFor`, `ExecuteWorldActionSelection`, `_actions`).
- **RED first.** `CostEquivalenceGolden_UnspecifiedCommandsChargeWhatTheirChainChargedToday` (green first; enumerates every command literal in both chains and the `PreparationActions.IsCommand` families, for both menus); `DeclaredOneActionRowRunsInTransactionAndChargesOneTurn` (a test Part declares a command in neither chain: refuses today); `DeclaredFreeRowChargesNothingEvenWhenALegacyChainWouldCharge`; `DeclaredRowStillObeysTheReachGate`; `DisabledRowIsListedDimAndSelectionLogsReasonWithoutExecuting`; `DisabledRowSpendsNoTurnAndOpensNoTransaction`; `DisabledRowKeepsMenuOpen`; `DisabledHotkeyBehavesLikeEnter`; `PackMenuCopiesDeclaredFieldsToItemAction`; `DisabledWinsOverDeclaredTime`; `ActionDisabledDiagPinned`.
- **Adversarial.** null or empty command with a declared time; `FireOnActor` with disabled; duplicate names; hotkey collision between disabled and enabled rows; whitespace-only reason is not disabled; row mutated after list sort.
- **Acceptance.** Native green; no shipped row changes (mechanism only; the rig adapter is S4). Actual play: a menu opened on any ordinary object looks and behaves exactly as before; F5; fresh Continue.
- **Depends on.** none. **Risk.** low-medium (two UI hunks; chains untouched). **Limit.** disabled rows in the process menu need S12's reasons.

### S4. Station contract, adapters, permission seam (2-3 cycles)

- **Goal.** One query, one readiness evaluation, one receipt; shipped forge, still and rig go through it with no behaviour change; the access seam exists and is inert.
- **Files.** NEW `Gameplay/Stations/{StationContracts,StationReadiness,StationQuery,StationReceipt,StationAccess}.cs`; MOD `ForgePart.cs` (interfaces, rig clause, disabled rig row), `AlchemyStillPart.cs` (`IsNearStill` body), `ForgeWorkReceipt.cs` (forwarders); NEW tests `StationQueryParityTests.cs`, `StationReceiptTests.cs`, `StationAdversarialTests.cs`.
- **RED first.** Oracle copies of `ForgeWorkReceipt.Ready/Near/Resolve/Begin` and `AlchemyStillPart.IsNearStill` in the test file; `StationQueryParity_TruthTable` over at least 40 layouts (actor placed / unplaced / other zone; station absent or in each of the 9 cells; ready, faulted, hauled, harvested, destroyed by `Gone` and by `HP<=0`, two providers, provider parent mismatch, rig good and bad payload, zone not current); `TwoAdjacentForgesResolveInLegacyScanOrder`; `StillPresenceOnlyKeepsLegacyLeniency`; `BareNamedBlueprintWithoutProviderPartIsNotAStation`; `ReadinessReasonCodesAreStable`; `ReadinessGuardPartCanVeto` (test Part); `AccessGuardDeniesBeforeAnyClaim` and `NoAccessGuardMeansAllowed`; `BeginRetainsExactStationThroughCommit` (a callback that swaps or moves the station refuses and rolls back, identical to the oracle); `EveryRefusedBeginEmitsStationRefusedDiag`; `ForwardersContainNoScanLoop` (source pin); `RigAdjacentButFouledShowsDisabledRowNamingTheCause`, `RigNotAdjacentStillOmitsRows` (counter), `HauledRigDisabledReason`.
- **Counter-checks.** every positive truth-table row has the same layout with one clause flipped; the same entity name without the Part is not a station.
- **Acceptance.** All Forge, Alchemy, rig, salvage, brew and mineral suites pass **unchanged**; actual play: forge verbs, one `ForgeCold` process and a brew still work; the fouled rig shows a disabled row with its reason; fresh and cached controls; F5; fresh Continue.
- **Depends on.** S1 (hygiene for `StationPart` later); S3 for the disabled-row mechanism (the contract can land first with the rig row last). **Risk.** medium (eight files; oracles contain it). **Not done here.** `WorldAudioWork.Forge`; `MineralPreparationPart` and `BrewReagentsCommand` keep their presence check (S9).

### S5. Reader contract (2 cycles)

- **Goal.** Both readers call one collector; a new Part appears in both without editing either builder.
- **Files.** NEW `Gameplay/Entities/{IDescribeState,DescribeContext,StateReader}.cs`; MOD `ExaminablePart.cs` (`:132-141`), `ExamineDocumentBuilder.cs` (`:93-97`); `IDescribeState` on `KitchenBatchPart`, `SoddenPreparationPart`, `OilStrainingPart`, `ContainedLiquidSourcePart`, `FiniteLiquidFilmPart`, `RepairablePart`, `TrapJammingPart`, `InstallationMarkerPart`; NEW tests `StateReaderGoldenTests.cs`, `StateReaderAdversarialTests.cs`.
- **RED first.** `ReaderParityGolden_QuickTextUnchangedForSevenProviders` and `..._DocumentTextUnchangedForFourProviders` (golden strings captured from HEAD, green before the move); `FullDocumentGainsFilmAndContainedLines` (RED; decides the [U] in section 1 item 6); `FullDocumentAllOtherLinesByteIdentical`; `NewPartContributesToBothReadersWithoutEditingEitherBuilder` (test-assembly Part; the capability proof); `PartThatAddsNothingChangesNothing`; `LinesOrderedBySectionThenOrderThenInsertion`; `DuplicateTextDeduplicated`; `DescribeIsReadOnly` (no events fired, no state or diag mutation); `ThrowingDescriberIsIsolated`; `InstallationMarkerLineAppearsInBothReaders`.
- **Adversarial.** null owner, viewer or zone; huge line counts respect the existing text budget; mode flags.
- **Acceptance.** Native green; actual play: `x` on the Cinderhold tray shows the live contained-oil line (or the RED proves it was deliberate); `x` on a placed forge shows the installation line (after S7); F5.
- **Depends on.** S2 (marker line only; the collector does not). **Risk.** low-medium (golden churn).

### S6. Installation registry, validator, coverage census (1 cycle; no generation change)

- **Goal.** Specs are validated data and "every town" is a measurable ratchet before anything is placed.
- **Files.** NEW `Gameplay/Installations/{InstallationSpec,InstallationRegistry,CoverageReport}.cs`; NEW `Resources/Content/Data/Installations.json` (zero installations; the exemptions and `Pending` rows from C3, plus `Pending` entries for Gantry, Tine, Slip, Sumphold, Posy, Quillhold, Marrowstye, Tally, Wellmeet, the Salt-Vault, the Drowned Ledger, the Last Counter, each with an Owner and Note); NEW tests `InstallationRegistryTests.cs`, `InstallationCoverageCensusTests.cs`, `VillagePipelinePriorityTests.cs`.
- **RED first.** One test per error code E01-E14 (at least 24 cases including boundary values), each with a valid-neighbour control; `OneInvalidRowDoesNotDropOthers`; `NearAlwaysEndsWithWell`; `CoverageClassifiesAll20PlacesExactlyOnce`; `UnclassifiedPlaceFailsTheCensus`; `PendingCountMayOnlyDecrease` (committed constant); `PresentPlacesActuallyHaveAForge` (generate Sill and Cinderhold, seed 64, find `TinkersForge`); `ExemptionRequiresSourceAndReason`; `RegistryLoadsFromResourcesAndDefaultsAreInert`; `EveryVillagePipelineHasUniquePriorities` (all 16 shared-pipeline places, documented exception: `ContainerBuilder`/`TradeStockBuilder` tie at 4100 per `placement.md` 1.2) and `Priority4650IsFree`.
- **Acceptance.** Native green; zero runtime behaviour; census prints the 20 classifications into the doc.
- **Depends on.** S1. **Risk.** low-medium (`JsonUtility` quirks: field initializers and absent nested objects are [U]; every optional field is therefore a nullable string or array, never a nested object).

### S7. Generic post-builder and the first placements (2 cycles + actual play)

- **Goal.** A usable existing forge in the first towns: deterministic, optional, fresh-only, retained when state-bearing.
- **Files.** NEW `Gameplay/Installations/{InstallationPostBuilder,InstallationAdmission}.cs`; MOD `OverworldZoneManager.CreateVillagePipeline` (+1 line); MOD `Installations.json` (+3 rows: `gantry-forecourt-forge`, `tine-forecourt-forge`, `slip-forecourt-forge`, all `Retention: None`, moved out of `Pending`); NEW tests `InstallationPlacementTests.cs`, `InstallationDeterminismTests.cs`, `InstallationAdversarialTests.cs`.
- **Why these three.** Three structurally different pipelines: Gantry (composed, preferred service cells), Tine (composed, wet neighbours, fastest relapse), Slip (plain `VillageBuilder`). The towns are chosen for mechanism coverage; the forge rows rest on the faction row only, so **owner sign-off is required before RED**; the mechanism proof is unchanged if the data names other surface-village towns.
- **RED first.** `FreshTownPlacesExactlyOneForgeAtTheDeterministicCell` (seeds 64, 704860946, 1729, plus a 24-seed sweep, per town); `PlacementIsSeedSensitiveButStablePerSeed`; `OtherOwnersIdenticalWithRowRemoved` (compare blueprint + position sets, never IDs; difference is exactly +1 forge, +1 marker); `RngUntouched` (`new Random(123)` before and after equals a fresh one); `FloodConnectivityAndStairsPreserved` (`CinderholdCompositionTests.AssertConnected` pattern [V*]); `ForgeNotOnRoadStoneAndNotNextToDoorStairsOrWell`; `SolidTestBlueprintKeepsAStandingCellAndRoutes` (a test blueprint with `Solid=true`; makes the solid path non-vacuous); `MissingBlueprintOmitsOnlyTheRow` / `MalformedBlueprintOmitsOnlyTheRow` / `CreationCallbackInjectionOmitsOnlyTheRow` / `NoFittingCellOmitsWithDiag`; each leaves the base town identical and `BuildZone` true; `CachedZoneNotRetrofitted` (second `BuildZone` on a populated zone changes nothing); `GameSessionRoundTripThenGetZoneAddsNothing`; `RetryReplacesOnceAtTheSameCell`; `ReplayGuardSkipsExistingMarker`; `RegenerationAfterRelapseReplacesAtTheSameCell` (Tine, Retention None); `StateBearingTestBlueprintWithRetentionZoneIsRetained`; `StateBearingBlueprintWithRetentionNoneIsRejectedAtLoad`; `SecondExplorationPacketsUntouched` (Quillhold, Cinderhold, Tally, Wellmeet pipelines with rows on and off); `StandingCellReservationDoesNotChangeLaterBuilders` ([U] decider); diag pins for Placed/Omitted/Rejected; no existing exact-count settlement test regresses (re-run `ChunkActivityCensus`, `SpreadExplorationWorksites`, `SpreadWorksiteManifest` suites, whose village counts are [U]).
- **Adversarial.** taxonomy: state atomicity and rollback (receipt mismatch), parser inputs (covered by S6), cross-zone (two towns same spec), save reach, anti-exploit (spec naming a creature or container blueprint), probability/RNG boundaries (rng untouched), diag dispatch.
- **Acceptance.** Native green. Actual play, fresh seeds 64 and 704860946: walk to Gantry, Tine and Slip; find the forge on the square ring; Look, `x`; use a forge verb and one `ForgeCold` process at it; leave, return; F5; fresh Continue; the machine and its position persist; **cached-save control**: an older save with Gantry already visited shows none. Evidence states: glyph presentation only (the forge draws as a glyph in non-admitted zones, as the Cinderhold forge does today), terms unenforced as at Sill and Cinderhold, Linux/Deck unexecuted.
- **Depends on.** S2, S6 (S1 transitively). **Risk.** medium-high (generation). **Limits recorded.** exterior only; plan-reserved in-room placement is S24; entity-ID shift by one; first visit in old worlds gets machines, visited towns never.

### S8. Census fan-out by data (1 cycle)

- **Goal.** Every authored place is `Installed`, `Present`, `Exempt` with a source, or `Pending` with an owner; no silent gaps.
- **Files.** `Installations.json` rows only; `InstallationCoverageCensusTests` baseline constant lowered; docs.
- **Rows proposed (illustrative; each needs owner and lore sign-off and a blind-gate pass for any authored string):** forge at Sumphold; still at Posy (pigment) and Quillhold (ink works). Places that need terms (Tally, the Last Counter, Wellmeet, Marrowstye, the Salt-Vault) stay `Pending` until S11; places with no fitting existing kind (the Drowned Ledger and the catacomb cold-work questions) stay `Pending` with a named new-kind owner.
- **RED first.** parametrized placement tests per new row using the S7 control matrix (Quillhold with `SecondExplorationSites` and `QuillholdCopyHallBuilder` both on); `EveryPlaceHasRowOrExemptionOrPending`; `PendingCountDropsTo<N>`; `ForbiddenFactionsNeverReceiveInstallation`.
- **Acceptance.** Native; actual spot check in two of the new towns (one per faction family). **Depends on.** S7. **Risk.** low technically; the gate is lore sign-off per row.

### S9. Station hardening (1-2 cycles; hazard removal, named behaviour change)

- **Goal.** Every station user goes through `StationReceipt.Begin`, so retention and access apply uniformly.
- **Files.** MOD `MineralPreparationPart.cs:28`, `BrewReagentsCommand.cs:128`, `PreparationRecipePart.cs:50-53`, `WeaponSalvagePart.cs`, forge verb commands (`ForgeWeaponCommand`, `ReforgeWeaponCommand`, `TemperWeaponCommand`) from presence/`Near` to `Begin`; tests `StationHardeningTests.cs`.
- **RED first.** `MineralPreparationRefusesWhenCallbackMovesTheForge`, `BrewRefusesWhenCallbackMovesTheStill`, `BatchBrewRefusesWhenStillSwapped`, each RED on HEAD; `ForgeVerbsRefuseWhenStationSwapped`; `NoBehaviourChangeForStationHeldStill` (counter); `AccessGuardNowCoversEveryStationUser` (test guard denies every path); the PresenceOnly oracle: if no shipped scenario differs, `PresenceOnly` is retired (a second named change, only with the oracle evidence).
- **Acceptance.** Native; existing suites green; actual play: brew at the still, mineral prep at the forge, forge a weapon. **Depends on.** S4. **Risk.** medium.

### S10. Service terms (gated; S10a 2 cycles, S10b 2 cycles)

- **Gate.** Independent design critique first (section 4 C7 lists the questions), then RED.
- **S10a.** `ServiceTermsPart` with `Open`, `Gift`, `Withheld`, `Owner` (fail closed), operator presence; reader line through `IDescribeState` (`Access`); census/validator accepts `Terms` blocks; `Gift`+`Fee` and any Tent-Right place with `Fee` rejected at load. RED: `NoTermsPartMeansOpenForEveryShippedStation`; `WithheldVetoesBeforeAnyInputTouched`; `WithheldReasonShownAsDisabledRow`; `GiftRejectsFeeAtLoad`; `TentRightFeeRejectedAtLoad`; `OwnerAccessDeniesAsUnregistered`; `OperatorAbsentDeadOrHostileClosesTheStation`; `OperatorBoundByIdNotDisplayName`; `VetoOrderIndependentOfPartOrder`.
- **S10b.** `Fee`: affordability refused before commitment with nothing spent; paid exactly once by `DeferCurrencyTransfer` to the present operator; rollback refunds; total drams conserved over a scripted sequence. RED: `FeeRefusedWhenPurseShortSpendsNothing`, `FeePaidOnceToOperator`, `FeeRollsBackWithTransaction`, `DoubleSubmitChargesOnce`, `DramsConservedAcrossScriptedSequence`, `StationFeeDiagPinned`; adversarial: overflow, negative or zero fee, operator without purse.
- **Acceptance.** Native; actual play at a `Fee` bench (preview shows the fee text once S14 lands, otherwise the row label; pay, use, F5) and a `Withheld` read at a Curation place; a protected survey kills the operator and the station closes. **Depends on.** S4, S5, S9. **Risk.** high (economy, voice, lore).

### S11. Terms-bearing placements (1 cycle, data)

Concord places (Tally, the Last Counter) with `Fee`; Tent-Right (Wellmeet) with `Gift`; Pale Curation (Marrowstye) with `Withheld` where an instrument already exists. Each names a free `Alternative`, passes the blind voice gate, and keeps shipped Cinderhold/Sill forges Open. **Depends on.** S8, S10.

### S12. Multi-stack claims and reasoned process rows (gated; 2-3 cycles)

- **Gate.** Independent critique of the sibling-admission rule and pooled-consumption order before RED (CLAUDE.md names payload and equipped-input policy).
- **Goal.** The `serrated_kit` row stops vanishing when two spines are two stacks; any blocked process row says why.
- **Files.** `MaterialProcessPlan.cs` (`IngredientClaim`, `TryCreate(..., out reason)`), `MaterialProcessService.cs` (per-take consume/final), `MaterialProcessInputPart.cs` (disabled rows), `Zone.GetReadOnlyEntitiesWithCapability<T>() where T : class` (first consumer: the `no-station` row), tests `MachineMultiStackClaimTests.cs` + adversarial.
- **RED first.** `TwoStacksOfOneSpinePoolAndRowAppears` (RED today; carry two single-unit `SpurgrassSpine` stacks, one holding an inert `ThrownRecoveryPart`, plus `KnotflaxCord`); commit consumes one spine from each stack and the cord, output one `SerratedEdgeComponent`; counters: a sibling with a live recovery claim gives disabled `held`; one spine total gives `short`; one stack of two behaves exactly as today (parity); throwing callback restores every stack and the cord; mutating a sibling between preview and commit refuses; stack order is by ID ordinal; equipped stacks excluded; no cross-blueprint mixing; `PooledConsumptionConservesUnits` property sweep over random splits of 2..6 units into 1..4 stacks; `ClaimRollbackRestoresEveryStackAtEachTake`; `NoStateLaundering`; `NoStationRowShownOnlyWhenAStationOfThatKindIsInTheExploredZone`; capability index follows add/remove/move/save hydration; M3a/b/c and spurgrass suites untouched.
- **Acceptance.** Native; actual play: throw one of two spines, recover it, the row is offered; F5; fresh Continue. **Depends on.** S3, S4. **Risk.** high (shared transaction core, finite accounting).

### S13. Work-kind table, "Works here", `StationPart`, cold bench (S13a 2 cycles, S13b 2 cycles)

- **S13a.** The code work-kind table (C8.1); `ForgeCold` through `StationQuery(..., "cold-work")`; the generated "Works here" reader line. RED: `ProcessUsesStationKindColdWork`; `ShippedForgeStillQualifies` (parity with S4); `WorksHereLineGeneratedFromRegistryAndCannotDrift`; `UnknownWorkKindRowRejectedAtLoad`; a source pin that the three literals no longer appear outside the table; all six `Machine*Process*` suites untouched.
- **S13b.** `StationPart` (C5) and one fire-free bench blueprint (name and description are authored text: draft only, blind gate pending; unique glyph/colour pair among machine blueprints, recorded as the glyph fallback). RED: `ColdProcessRunsAtBenchWithNoForgePart`; `BenchWithBenchKindOnlyIsNotColdWork`; `FouledBenchYieldsNoPlan`; `TwoBenchesResolveDeterministically`; `BenchPlacesThroughS7WithRetentionNone`; hygiene lint passes. This is the first new machine kind through the contracts (the second consumer of C5 and C8). **Depends on.** S4, S7; S12 for reasons. **Risk.** medium.

### S14. Quote modifier channel (2 cycles; content-free)

`ProcessQuote`, `ModifyProcessQuote`, `Outputs` on `MaterialProcessCompleted` (C8.3). RED: a test modifier lowers a cost before the preview text and payment equals preview; a modifier that changes or vanishes between preview and commit refuses with nothing spent; out-of-bounds and undeclared blueprints are rejected; no handler means every shipped row is bit-identical; event parameters match the pinned table. **Depends on.** S12. Ships only with a test modifier; the first qualified production modifier brings its own slice.

### S15-S24 (summaries; each RED-first, with counters and diag pins)

- **S15 `WorldTime` registry (hazard, 1 cycle).** RED: `ReconcileOrderGolden` (Crop then Kitchen, arrival, paid turn, load); `ReconcilerExceptionIsolated`; `LoadDoesNotDoubleReconcile`; `CropSystemFallbackStillFires`. Kitchen and crop suites untouched.
- **S16 `WorkJobPart` (2 cycles, with the first timed machine).** RED: kitchen semantics replayed (start, interrupt keeps fee, ready, receiver full blocks, owner invalidated, unloaded zone completes one job, reload cannot repeat output, time never creates another job); `NoJobsNoScan`.
- **S17 Receivers/waste/conversions (2 cycles, with the first consumer).** `IProcessReceiver` (`Pack` extracted verbatim, `Tray`, `Ground`, `Vessel`), `Waste: Discard | Retain` shown in the preview, explicit `UnitConversions` rows. RED: `OutputBlockedKeepsInputs`, `DiscardRequiresDeclaredLossText`, `ConversionConservesWithLossDeclared`, `FilmCounterNotConvertible`, `ReceiversResolvedByRoleNotStoredPositions`.
- **S18 Stored work (1-2 cycles; blocked on the canon decision and a first consumer).** Feeds as data with identical torch/campfire constants (25, 10); `WorkStorePart`; lint tests `ForbiddenKindRejected`, `ProtectedFeedRejected`, `ConversionCycleRejected`, `OverflowRefusedNotClamped`, `NoDrainWhileAway`.
- **S19 Installable state (2 cycles, with the home/base design).** `InstallablePart`, `IRebindable`, events, `RigRelocation` stamps. RED: `RelocatedRigZoneSurvivesRelapse` (pin), `OldSaveWithoutPartIsNotInstallable`, `InstallMarkerRoundTrip`, `UninstallReleasesRetention`, `RebindFailureRollsBackInstall`, `LooseVetoesReadiness`.
- **S20 NPC perception and keeper (2 cycles).** `StationPerception`, `AIStationKeeperPart`, `UseStationGoal`. RED: `NonPlayerProcessWorksOutsidePlayerView` (RED today), `PlayerBehaviourUnchanged`, goal walks then uses, refused by terms fails to parent, keeper returns to post, no stock changes while the player is away.
- **S21 `PartUpgrade` registry (1 cycle).** Replaces `SaveSystem.cs:263`'s hard-coded `CuttingToolPart.UpgradeLoadedDefinition` call; first entry is that upgrade with its `CuttingToolDefinitionSeen` fence; RED: `CuttingToolUpgradeGolden`, `UpgradeNeverAddsContent`, `UpgradeRunsOnceAndFences`.
- **S22 Repair bridge (1 cycle).** `StructureRepaired` after commit, `Faults` read facade, additive `RepairablePart.SiteBinding`; the quest hook stays a direct call. RED: `BlocksFunctionGolden` (five consumers), `RepairFiresStructureRepairedAfterCommitOnly`, `RolledBackRepairFiresNothing`.
- **S23 Classifier (1-2 cycles).** `MachineMeaning.Has(entity)` ORs capability interfaces, marker and the legacy Part types the 36-file chain lists today; new generation code uses it; ratchets: file count of copied chains may not grow, glyph+colour uniqueness among machine blueprints. Art is out of scope.
- **S24 In-room anchors and mention (2 cycles).** Profile-phase builder (priority 3862) for plan-reserved cells and standing frontage through an opt-in `ISettlementPlanView` on the composed builders whose rows need it; voice-gated `Mention`. RED: `ProfilePhaseReservesCellsBeforePopulation`, `RoomAnchorRejectedWithoutPlanView`, all 11 `Signature()` identical, no route redrawn.

---

## 8. Resolved conflicts

| Topic | Cap-first | Data-first | Evidence-first | **Chosen** | Why |
|---|---|---|---|---|---|
| Builder priority | 4700 | 4650 | 4650 | **4650** | highest village priority is 4600; 4650 is unique |
| Placement anchor | `NearService`/perimeter/room | owner list + `Well` fallback | square center + offsets | **owner list, `Well` appended** | `GetVillageSquareCenter` exists only for ordinary villages; the Well exists in all 16 |
| Plan-reserved path | opt-in at 3862 | designed, deferred | S13 | **deferred to S24** | no spec needs in-room frontage yet |
| Retention scope | all marked | retain-always | per kind, stateless excluded | **per kind** (`RetainZone`, lint on state-bearing Parts; player origin always) | smallest behaviour change; avoids freezing stock re-rolls for a stateless forge |
| Legacy retention | blueprint-name list | zone-id list | manifest | **blueprint-name list** | catches a rig hauled into any village; no zone ids |
| Marker fields | `Retain`, presentation key | `State`, `OwnerId`, `HomeZoneId` reserved | `Retains`, `PlaceId` | **4 fields now**; others additive later | additive saves make reserving pointless |
| Station identity | `IStationCapability` + `StationPart` | `Stations.json` + `IStationAdapter` registry | `IStationProvider` | **interface on Parts + `StationPart`; no JSON table** | two rows per kind do not justify a table; legacy saves unaffected |
| Readiness | event answered by owning Parts | central + adapter hook | central + guard | **central clauses + `IStationReadinessGuard` interface** | no unfiltered dispatch; rig clause moves to its Part |
| Readers | event | interface | event | **interface** | defined order, one pass, no dispatch cost |
| Process work kinds | `WorkKinds.json` + provider | `WorkKinds.json` + `IWorkReceipt` | provider registry | **code table; registry when a third kind exists** | two rows; loader and registry are speculative |
| Action time | int `Turns` | int `Cost` | enum `ActionTime` | **enum** | no consumer for N >= 2 |
| Disabled rows | zone-explored visibility rule | opt-in only | blocked rows in S2b | **mechanism S3; near-but-unready in S4; zone-wide rule in S12** | visibility rule needs the capability index |
| Table loader | per-registry | `DefinitionTable<,>` | per-registry | **per-registry (model: `MaterialProcessRegistry`)** | one new table only; extract on the second |
| Per-row vs all-or-nothing load | per-row | all-or-nothing atomic | per-row | **per-row drop** | optional placement must not cancel the world |
| Version gate | none | `MinExplorationVersion` 15 | none | **none** | 2 of 3; Cinderhold precedent; no exploration-version change |
| Optional/Required | optional | optional | optional | **optional; `Required` rejected** | a false retries 5 times then nulls the town |
| Fee payee | person (`DeferCurrencyTransfer`) | person | ledger sink | **person; sink rejected pending critique** | kitchen precedent conserves money; new sink is an economy decision |
| Permission timing | after census, S9 | C7 mid | S6 after station | **seam in S4, hardening S9, semantics S10 after critique** | cannot enforce until every user shares one receipt |
| Mineral/brew strengthening | separate S16 | inside adapter slice | inside adapter slice | **separate S9** | adapters change no behaviour |
| Audio scan | RowMajor parameter | not migrated implicitly | forwarded | **not migrated** | different question; fewer parameters |
| First machine placed | fire-free bench first (S6) | existing forge first | existing forge first | **existing forge first (S7), bench S13b** | placement is unblocked by existing content; the bench is the second consumer of the station contract |
| `ForgePart.Factory` coupling | replace now | replace in C4 | n/a | **later** | not a blocker; no consumer |
| Capability index | S2 | n/a | n/a | **S12 (first consumer)** | two-consumer rule |
| Part census content | types + fields | types + fields | type names only | **types + fields + custom flag** | a removed field misaligns the stream even when the type survives |
| Namespaces | `CavesOfOoo.Core.*` folders | `CavesOfOoo.Core` | `CavesOfOoo.Core.Stations` | **`CavesOfOoo.Core` only** | saved name is assembly-qualified and permanent |
| Stored work vocabulary | Heat/Tension/Head | hand/fire/wound | heat/wound/head/banked | **deferred; forbid list only** | canon decision belongs to the owner |
| Spurgrass status | committed | dirty | committed | **committed (`9316f9330`)** | verified |

---

## 9. Home/base hooks (contracts the later system relies on)

1. `InstallationMarkerPart` is on every placement from the first one; player installs use `Origin = "Player"`, `RetainZone = true`; the single C2 predicate retains a base zone. No new registry.
2. `StationReceipt.Begin` + `IStationAccessGuard` are the only access choke point once S9 lands; `ServiceTermsPart.Access = "Owner"` is registered by the home design and **fails closed while unregistered**; the spoken-end verb (lore R9) is the home design's job and the terms reader reserves an `Access` line for it.
3. `IStationReadinessGuard` lets an `InstallablePart` in `Loose` state veto readiness without editing any station.
4. New Parts never store zone ids or station/receiver coordinates (S1 lint); receivers and operators are resolved at use time by role/group/ID, so machines stay movable and cached zones survive regeneration.
5. `InstallablePart` + `IRebindable` + install events (S19); the kitchen family is declared non-relocatable.
6. Placement checks (`Geometry` fit, route preservation, standing cell, receipt rollback) live in one reusable place (`InstallationPostBuilder` internals extracted to `InstallationPlacement` when the install verb needs them) so a player build uses the same rules as authored placement.
7. Reconcile-on-arrival jobs (S15, S16) need no new time system for a base; no away simulation; no output repeated on reload.
8. Station identity is by entity ID and kind tokens, never display name; `SettlementId` property is a convention, not enforcement.
9. `FormatVersion` stays 8; `SavedPartContract` fails any base-system refactor that renames or retypes a saved Part.
10. Shared diag vocabulary (`worldgen/Installation*`, `furniture/Station*`) lets base acceptance use `diag_assert` instead of log scraping.
11. Canon shapes the base idiom: Tent-Right hospitality is an enacted act (a base never auto-grants guest-right; water is never priced), catacomb niches and the patch are not claimable, Concord homes are portable and contractual, Curation/Recension homes are filed and need a spoken end.

---

## 10. Owner decisions, risks, not now, verification

### Owner decisions (this spec does not decide them)

1. Lift "no new runtime changes" and acknowledge the S10/S12 critique gates.
2. First placement rows (which towns, which kind). The forge rows for Gantry, Tine, Slip and Sumphold rest on the faction row alone (`lore.md` section 5 surface villagers O); the settlement dossiers list other fits for those towns and no forge. Posy and Quillhold stills do have settlement-level fits.
3. "Every town" scope: with forge and still only, about 6 further settlements fit today (8 once terms exist for Tally and the Last Counter); the rest need new kinds (the widest lore fit is the assay bench, qualification-gated) or are canon-exempt (the First Tent, the Quiet's Door, the three catacomb villages for fire kinds, Morrowfast pending its own hook). Confirm the exemption list.
4. First-visit semantics in old worlds (accepted default) versus a per-spec `Since` gate.
5. Retention per kind (accepted default) versus retain-always.
6. Stored-work canon (lore Q1) before S18.
7. Fee payee and the permission idioms (S10 critique); whether the Cinderhold Weaponsmith's "paying customer" promise is ever enforced (default: shipped forges stay Open).
8. Disabled-row visibility rule at S12 (zone-explored) and the blind voice gate owner for every authored string.

### Risks (ranked)

1. **Shared-core slices** (S3 UI, S4 stations, S12 claims, S14 quote) touch code every machine slice uses. Mitigation: oracles, goldens, one core change per cycle, adversarial files first.
2. **Standing hold** in `CLAUDE.md` ("no new runtime changes", payload/equipped-input/save critique gates).
3. **Retention is a deliberate behaviour change** for towns holding a retaining marker or legacy fixture; limited by the per-kind rule; S2 begins with a premise test that may narrow it.
4. **Placement omissions** in towns with little free exterior floor: omitted with a diag; the census fails the build; S24 is the guaranteed-frontage answer.
5. **Presentation debt**: placed machines are glyphs outside admitted zones and four stations share glyph `n` (colour differs); readers (`x`, F1, S5) and Look are the discovery channel; no art is in scope (graphics cancelled).
6. **Lore/voice debt**: no shipped machine string has a recorded blind-gate pass (L17); every authored string in S7+ needs one.
7. **Entity-ID shift** and `JsonUtility` quirks (S6) are first-cycle [U] items; neither is a design risk.
8. **Isolated-clone cadence**: S4, S12, S13 are the largest; each is split along the lines above.
9. **Old worlds**: visited towns never gain a machine (stated limit, not a defect).

### Not now (with trigger)

New machine kinds (still fractionation, cradle, vat, pump, exchanger, dynamo, resonator, relay, bath, capstan, cultures): each needs purpose qualification (principle 1) and canon re-grounding; trigger: owner approves a kind. Native art bodies and the `PropIcons` route (graphics cancelled). A `DefinitionTable<,>` loader, `Stations.json`, `WorkKinds.json`, provider registry (trigger: second table or third work kind). Row schema v2 beyond 2 inputs/2 outputs. Interior/room placement and plan-role anchors (S24). Catacomb-village and Morrowfast attach points (trigger: a spec targets them). `ForgePart.Factory` decoupling. Deleting the two turn chains, `ForgeWorkReceipt`, `FieldWorkRigRules` or the 36-file chains (only after parity soaks). Converting the kitchen, Sodden desk, ink desk or Nella claim onto generic Parts. Any charge/mana/battery/Glow-Quartz/Urqu power. NPC production away from the player. A property/theft law. Retrofitting cached zones, a `FormatVersion` bump or a `SpreadExplorationPlan.CurrentVersion` bump. Reading, restoring or explaining any protected subject (lore R1-R11; exemptions are the enforcement point).

### Verification record

Read this session ([V*]): `InventoryAction.cs`; `ForgeWorkReceipt.cs`; `AlchemyStillPart.IsNearStill`; `WorldAudioWork.Forge`; `ForgePart` header and action handler; `RepairablePart.BlocksFunction`; `InputHandler` world menu input, selection, reach gate and chain; `InventoryUI` `ItemAction`, row copy and charge chain; `WorldInteractionSystem.GatherActions`; `WorldAffordanceQuery` haul verb; `WorldActionMenuUI` selection; `OverworldZoneManager` village pipeline head and tail, `CanUnloadZone`, `PrepareZoneForAccess`; `ZoneManager.UnloadZone`; `Zone` part index; `SumpholdFurnishingBuilder` staging; `SpreadWildernessSituationBuilder.Geometry`; `DoorPart.IsBareGround`; `Formation.StableIndex`; `IZoneBuilder`; `EntityFactory` part registration; `SaveSystem` part load, type naming, field rules, load hook; `MaterialProcessRegistry`; `ExaminablePart` and `ExamineDocumentBuilder` provider lists; `WorldMapAuthoring.Places`; `PointOfInterest`; `TinkersForge` blueprint; `PlaceSites.json` relapse values; builder priority grep; commit `9316f9330`; test-directory conventions.

Not read ([U], each named by the test that settles it): menu hotkey routing and disabled-row rendering colours; exact `SettlementManager` calls to put a structure site into relapse; `MaterialProcessPlan`/`Service` bodies beyond the architects' citations; `SpreadGenerationReceipt` signatures beyond `CaptureFinalState`; whether any post-commit step compares `GenReservedCells`; `JsonUtility` field-initializer behaviour; village counts in `ChunkActivityCensus`/`SpreadExplorationWorksites` tests; the lore settlement dossiers (`settlement-*.md`) beyond `lore.md` and `enumerate.md`.

No test, build, Unity run or git write was performed.
