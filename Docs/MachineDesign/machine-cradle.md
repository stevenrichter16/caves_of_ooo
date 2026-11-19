# MACHINE V1 DOSSIER: Restoration cradle

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. Lore authority is root `Lore/` only.

Evidence tags: **[V]** I read the cited line in this session. **[R]** taken from `arch.md`, `lore.md`, `settlement-*.md` or `design/ARCHITECTURE-SPEC.md` without re-reading. **[I]** inference or design. **[U]** unverified; a named RED test settles it.

Spec references (S1, S2, S3, S5, S7, S13, S14, S22, C-numbers) are the slices and contracts of `design/ARCHITECTURE-SPEC.md`.

---

## 0. Verdict (read this first)

1. **Classification: `wave3-needs-new-system`.** The pitched cradle needs a persistent component-slot capability plus graded faults on a physical owner. The spec has no such contract (S22 is only a read facade and an event; `arch.md` section 7.6 lists "wear or severity, tool requirement, alternative materials, relapse for objects" as missing). Everything else V1 needs is wave 1 or 2 (S1, S2, S3, S5).
2. **Purpose: the cradle has no real, unserved purpose in the shipped world today.** It is the weakest-grounded of the twelve. Three independent grounds, each verified below:
   - Everything breakable that ships is already served by a cheap repair ladder, and two items on it (portable gear repair, the Weaponsmith's paid repair) must stay untouched (section 1.2).
   - The one authored restoration-with-substitution situation with real consequences, the Cinderhold pump, has no physical action. "Running" is a condition flag set once at quest completion; a patch has no consumer in the world; no flooded level exists (section 1.3).
   - The pitch's own examples (quieter but slower cooling unit; oversized motor and power) need noise, throughput and power axes the engine does not have and canon forbids (lore L16, R5, R6; section 1.4).
3. **What the owner should know before approving any build:** the player-visible delta of the smallest cradle over today is one extra durability grade plus per-assembly mixing, both achievable data-only today by adding a "Middle" fix row to `PlaceSites.json` (section 2.9). The cradle earns its place only as the **repair half of the Cinderhold pump chain** (cradle plus sump pump, `settlement-cinderhold.md:226` tiering "A = 3, 7, 1"), or when a carried damaged find with two independent assemblies and a real parametric action exists. Neither exists yet.
4. **Recommendation:** do not schedule a cradle in wave 1 or 2. Keep this dossier's V1 as the ready-to-build specification for when the pump chain's physical action is authored, and meanwhile (a) do the zero-machine alternative if the owner wants the durability choice, (b) keep the cradle `Pending` or `Exempt` in the census for every town (section 5).
5. **Honesty rule:** V1 below is acceptable only as a *prerequisite increment* (slots, fits, readers, retention, one shared repair authority). It must not be reported as "restored apparatus performs its real action" until the pump physically moves water (machine 7).

---

## 1. Evidence base

### 1.1 What the pitch requires

`Docs/MACHINE-ROSTER-VISION.md:35-39` [V]: hold a damaged artifact or machine while replacing missing assemblies; substitutions change behaviour; restoring a find is a small design problem; ordinary repair stays convenient; the cradle earns its place through larger reconstruction jobs.

`Docs/MACHINE-SYSTEMS-DESIGN.md:87` [V]: missing support = "persistent component slots, explicit compatibility, faults and tested substitution tradeoffs"; first evidence = "restored apparatus performs its real action; alternate repair changes a legible capability, not merely item flavor". Status: "Proposed; M1b recovery/service and M1c bounded relocation accepted ... complete-machine prerequisites open."

`Docs/SYSTEMS-DESIGN-PRINCIPLES.md:15` [V]: prove through small playable increments with a meaningful ordinary alternative, a useful interaction outside the apparatus and a cross-system consequence; no progression padding.

### 1.2 What ships toward the cradle (the repair ladder)

| Layer | Evidence | Capability | Limit that matters here |
| --- | --- | --- | --- |
| Portable gear repair | `RepairablePart.cs:18-20,86-93,148` [V]; 10 blueprints with `PortableEquipment` (FilterHood, AcidworkerApron, ColdwardCloak, Dagger, LeatherArmor, LongSword, Battleaxe, Mace, Cudgel, IronHelmet; Objects.json scan [V]) | One carried material, one action, anywhere; removes `BrokenEffect` | `BrokenEffect` is a small marker: -2 hit, -1 AV at most (`BrokenEffect.cs:19-29` [V]). Not a "large reconstruction job". Must stay free and convenient. |
| Paid repair | `ArtisanRepairServicePart` `SecondExplorationServices.cs:51-68` [V] | Weaponsmith, 8 drams, excludes rentals | Ordinary convenience; untouchable (spec R7). |
| Structural single-fault repair | `RepairablePart.cs:12-34,118-165` [V]; 8 structure blueprints (SoddenDressingBench, CurationServiceGate, RepairLinedWell, RepairRopeWell, RepairWoodenGate, ConnectedBatchPan, GleanersBuckledWicket, AbandonedFieldWorkRig [V]); 10 recipes in `Tier1Repairs.json` [V] | One boolean, one recipe, one-way; `BlocksFunction` gate | `Context` requires exactly one `RepairablePart` and one `CompositionPart` (`RepairablePart.cs:73-75` [V]), so a second fault Part on one owner is refused; player-only (`:78`); hard-calls `ConnectedSpreadProgress.RecordRepair` (`:156`). |
| Place-site repair with substitution | `PlaceSites.json` 5 sites (Weir, GuestPole, Scales, SurveyLine, Pump) [V]; `PlaceSiteCatalog.cs:15-156` [V]; `SettlementManager.RepairPlaceSite` `:365-407` [V] | Lasting (all materials plus kept tool) versus Patch (cheap, relapse after N turns); state in `RepairableSiteState` | Abstract state, no physical owner; driven by an NPC conversation node (`ConcernConversation.cs:202-217` [V]); consumption is not transactional (`ConcernItems.TakeCarried` loop, `ConcernItems.cs:27-40` [V]). |
| Field work rig | `FieldWorkRigRules.cs:11-79` [V]; `Docs/MACHINE-FIELD-WORK-RIG.md` [V] | Recoverable station: repair 2 FireClay once, then every forge verb; local haul; reclaim 2 timber | Accepted, pinned fixture. Patched rig already supports all verbs, so there is nothing left to "restore more". Not to be altered. |
| Reclamation and composite processing | `MaterialProcesses.json` 6 rows [V]; `MaterialProcessPlan.cs:44-60` [V] | consume carried blueprints, produce new blueprints (M3a/b/c/spurgrass) | Station resolved only by `ForgeWorkReceipt.Resolve` (`:50`) or a carried cutting tool; the plan pins `Source.Parts.SequenceEqual(sourceParts)` (`:98`), so it cannot mutate a held instance in place. Produces the very components restoration consumes. |
| Weapon reforge | `WeaponAssemblyPart.cs:1-40` [V] | stores component **blueprint names** (stateless components); `ReforgeWeaponCommand` swaps one | The forge already is "the cradle" for weapons. A weapon cradle duplicates it. |

Prior independent inventory reached the same conclusion: "found no better second damaged-find loop ... BrokenEffect does not identify sound surviving constituents; do not convert it or Material/Composition tags into arbitrary yields" (`Docs/MACHINE-COMPOSITE-PROCESSING.md`, Historical qualification [V]); `ReleasedExtractionTool` "dismantle" and "reclaim" exist, and "No additional repair-back-to-original-weapon branch is promised" (`MACHINE-RECLAMATION-PROCESSING.md`, Player problem [V]). A third "restore" branch on that tool is redundant: dismantle returns the sound head and haft and ordinary pack crafting adds a binding.

### 1.3 The Cinderhold pump has no physical action (the decisive finding)

- Site `Pump`: broken text "rod has sheared at the collar, and the old binding hangs in black strings"; Lasting = 2 IronSpikeComponent + 1 OakHaftComponent + 1 LeatherBindingComponent; Patch = 2 FireClay, relapse 600 turns (`PlaceSites.json:114-147` [V]).
- Completion trigger for the concern's Restore approach is `IfPlaceSiteStage Cinderhold:Pump:StableRepair` plus `IfNotPlaceSiteMethod ...:Contracted` (`ConcernRegistry.cs:108-111` [V]); `IfPlaceSiteStage` is a "stage at least" predicate (`ConversationPredicates.cs:266` [V]).
- `Cinderhold.PumpRunning` is set only by the concern's `Outcome` (`Concerns/Cinderhold.json`, approach `pump` [V]); `SyncConditions` handles only the three Sill hearth sites (`SettlementManager.cs:487-500` [V]). Effect: prices x0.95 and a one-time Quartermaster top-up to 2 IronSpikeComponent (`PlaceConditions.json:518-534` [V]).
- **A patch therefore has no world consequence.** The `TemporarilyPurified` stage is consumed only by well/oven/lantern visuals, `LocalPeople.Water` text and `TradeStockBuilder` well goods (grep: `LocalPeople.cs:249`, `TradeStockBuilder.cs:105,126` [V]); none concerns the pump. [U residual: a repo-wide read for pump-specific patched consumers; grep found none.]
- No physical pump, no flooded level, no `6.6.1` underground exists (`settlement-cinderhold.md:120` [R, by absence]).
- Consequence for design: any graded-durability cradle at the pump is hollow unless the owner also decides that "running" is a live state (section 2.9).

### 1.4 Pitch axes versus canon and engine

- "Quieter but slower", "throughput", "power": no noise gameplay exists (grep of Gameplay finds only generation-noise and combat-noise hits [V partial]); no power Part (`arch.md` 2.7 [R]); canon has no motors or electricity and forbids a mana bar or charm storage (`lore.md` section 0 item 1, section 3 rows "motor", "cells", L16, R5, R6 [R]).
- The only shipped quiet/loud substitution is the scripted Morrowfast bell clapper (clay sleeve for a quiet call versus bare for a loud one; `MorrowfastQuests.cs` bell branch [V]). It is a quest-bound two-option precedent, not a general rule, and Morrowfast is a local non-canon proposal (`lore.md` L-list item 9).
- Weapon stats, armor, light and fuel are existing parametric axes, but weapons are the forge's, and light relevance to visibility is unverified [U]; none is a restoration problem today.

### 1.5 Settlement fit (from the 20-place dossiers [R])

Strong: Cinderhold only (`settlement-cinderhold.md:215`). Plausible: Sill, Gantry (a capability on the wright's bench), Tally, Marrowstye (weak), Posy (carried gear on request, never installations), Morrowfast (Orrit mends; local non-canon), Drowned Ledger (survey line must become a physical instrument first; bodies fenced). Poor or forbidden: First Tent, Salt-Vault, Quiet's Door, Olderdeep, Lampwell, Spivenor (plaque re-cutting is a sacred human act), Ginmere, Cathedral, Stillleaf (custody ledger), Sumphold, Slip, Tine, Quillhold (conservator canon "replace nothing"), Last Counter, Wellmeet (Scales already is the substitution data; "revisit if a finite damaged-find source is added"), merchant camps, Spread wayhouse. **Census recommendation: cradle is not a "machine in every town" candidate; at most 1 to 3 places ever.**

---

## 2. V1 definition (smallest honest version, conditional)

### 2.1 What V1 is, and what it is not

V1 = **the Cinderhold pump head in its mending cradle**: a physical, site-bound fixture that holds the pump's two assemblies (rod and collar binding) in a timber sling frame while the player replaces them, using the existing pump materials and the existing site state as the single repair authority. The "cradle" is the sling frame built into the head. A free-standing bench that holds a *carried* damaged apparatus is **not V1** (section 2.8): no such apparatus exists.

V1 is a prerequisite increment, not a complete machine (verdict 5). It exists to land: the slot capability, the fit table, graded faults, a shared repair authority, retention for a state-bearing fixture, and readers. It is blocked from acceptance as a machine by the missing physical action (section 1.3).

### 2.2 World content (fresh worlds only; cached Cinderhold never retrofitted)

| Item | Detail |
| --- | --- |
| `CinderholdPumpHead` blueprint | solid, non-takeable, no `HarvestablePart` (a communal fixture is never salvage; `MACHINE-RECLAMATION-PROCESSING.md:50` [R] "no communal-service salvage"), no `ContainerPart`/`InventoryPart`/`StackerPart`/`SpatialFootprint`, not haulable. Carries `ComponentSlotsPart`, `PumpHeadSitePart` (fields `SettlementId`, `SiteId`, same pattern as `WellSitePart.cs:14-17` [V]), `Examinable`, and property `SettlementSiteId = "Pump"` so `RefreshActiveZonePresentation` applies stage visuals (`SettlementManager.cs:445` [V]). |
| Placement | one spec row `cinderhold-pump-head` consumed by the generic post-builder (spec C3, S7): Place `Cinderhold`, `Retention: Zone` (state-bearing, so E07 forbids `None`), `Near: ["Well"]`, Radius up to 12. Candidate cells: the east yard (seed 704860946 example x44-49, y2-10; `settlement-cinderhold.md:171` [R]); an actual baseline is required before RED [U]. If geometry yields no cell the row is omitted with a diag and the town is unchanged. Fallback: a bespoke Cinderhold plan packet (precedent: the released-tool packet at work.X+12, work.Y+6). |
| Marker | `InstallationMarkerPart { RetainZone = true }` (spec C2) so a patched-pump relapse cannot regenerate the village over the fixture's saved fits. |
| Operators | none required (no keeper, no fee, no terms; S10 is gated and V1 must not wait for it). The Elder (concern giver) is unchanged. Optional one-line pointer from the Tinker or Weaponsmith is authored dialogue and needs the blind voice gate. |

### 2.3 Slots, fits and grades (data proposal; numbers are proposals, not tuned)

New data file `Assets/Resources/Content/Data/Mending.json` (JsonUtility-compatible arrays; validated per row like `MaterialProcessRegistry`; model: spec C3 error-code table).

Slots on `cinderhold-pump`: `rod`, `collar`. Each fit = one or more consumed blueprints plus a grade and a `Returnable` flag.

| Slot | Fit id | Materials consumed | Grade | Returned when later displaced |
| --- | --- | --- | --- | --- |
| rod | `oak-pinned` | 1 OakHaftComponent + 2 IronSpikeComponent | 3 | haft and spikes (sound hard parts) |
| rod | `willow-lashed` | 1 WillowHaftComponent + 1 IronSpikeComponent | 2 | haft and spike |
| collar | `leather-bound` | 1 LeatherBindingComponent | 3 | binding |
| collar | `cord-wrapped` | 2 KnotflaxCord | 2 | none (worn) |
| collar | `clay-sleeve` (**not offered**, listed only to show why) | 2 FireClay | 1 | none (cracked) |

Rule (legible, stated in the reader): the pump runs at the grade of its **weakest** assembly. Grade 3 = StableRepair, no relapse, **materials identical to the shipped Lasting set** (2 spikes + oak haft + leather; `PlaceSites.json:118-133` [V]). Grade 2 = patched, relapse proposed 2000 turns [I]. Grade 1 = patched, relapse 600 turns (the shipped Patch horizon). The shipped Elder route (Lasting, Patch) stays byte-identical; Patch (2 clay) remains cheaper than any grade-1 combination, so no grade-1 fit is offered at the cradle (it would be a dominated option, i.e. padding).

Validation at load (all are distinct RED cases): unknown blueprint; non-portable or creature blueprint; blueprint carrying a protected material identity (ChoirIron, Tepuibone, MemoryMarble, MuteStone, GlowQuartz, SariPyrite; lore L9, R8); site not in `PlaceSiteCatalog`; site equal to `Gantry:GuestPole` (sacred fixture, lore R7); duplicate fit ids; grade outside 1..3; count outside 1..8; banned vocabulary in any text (`charge`, `mana`, `battery`, `regulator`, `motor`, `power`; L16).

### 2.4 The new capability: `ComponentSlotsPart` (the one new Part)

```csharp
namespace CavesOfOoo.Core            // permanent saved name; never a sub-namespace (spec section 3)
{
    public sealed class ComponentSlotsPart : Part, IDescribeState      // IDescribeState from spec C6
    {
        public override string Name => "ComponentSlots";
        public string DefinitionId = "";                // key into Mending.json
        public string[] SlotIds   = new string[0];      // parallel flat arrays; additive saves (spec R3)
        public string[] FitIds    = new string[0];      // "" = empty or failed
        public int Revision = 1;
    }
}
```

Hygiene: no field named `ZoneID`, `StationX`, `StationY`, `*ReceiverID` (spec S1 lint). `CanSerializeType` accepts string arrays (`SaveSystem.cs:2141-2159` [V]); round trip is a RED case [U]. Fault state is **derived**, never stored twice: the single authority for stage and relapse stays `RepairableSiteState` inside `SettlementState`. The Part stores which fits are installed; the reader shows a fit as failed when the site stage is back to `Fouled`. No second fault Part, so `RepairablePart`'s exactly-one rule is never touched.

### 2.5 Player verbs (exact rows), inputs, outputs, cost, preview, states

Standing next to the head (Chebyshev 1), the world action menu lists, per slot, one row per offered fit (four rows in V1):

- `Fit rod: oak-pinned (1 oak haft, 2 iron spikes)`
- `Fit rod: willow-lashed (1 willow haft, 1 iron spike)`
- `Fit collar: leather-bound (1 leather binding)`
- `Fit collar: cord-wrapped (2 knotflax cord)`
- No clay-sleeve row (grade 1): the Elder's Patch already covers that outcome more cheaply (section 2.3).

A row the player cannot do is shown **disabled with its reason** (spec S3 `AddDisabled`): `short` ("You carry 1 of 2 iron spikes."), `held` (stack carries live recovery state), `stale-zone`, `already-stable`, `contracted` ("The Concord's mason did this one; there is nothing to fit."), `unsupported`. Without S3 the unavailable rows are simply omitted (documented fallback; the acceptance preference is S3).

| Aspect | V1 behaviour |
| --- | --- |
| Inputs | carried, non-equipped units of the fit's blueprints; consumption follows `RepairablePart.Apply`'s pattern (`Carried` check, `InventoryTransferSnapshot.Capture`, `TryConsumeOne`; `RepairablePart.cs:125-143` [V]); several stacks of one blueprint allowed; a live recovery or status payload refuses (`CanMergeProcessingInputs` rule, spec C8.2) |
| Output | the slot's fit id is written; site stage and relapse are committed through one shared service (section 2.6); no item is created, except displaced `Returnable` hard parts when a weaker fit is later upgraded, returned to the pack by the same transaction |
| Loss and waste | the sheared rod and the rotted binding are **spent** (disclosed in the reader and the row tooltip as "old fastening spent", the M3a wording); worn fits (cord, clay) are spent when displaced; hard parts are returned. Nothing is destroyed silently and nothing is created. |
| Cost | one ordinary scheduler action per fit; no fee, no fuel, no heat, no tool required (cold work); no elapsed job, no away simulation (`MACHINE-SYSTEMS-DESIGN.md:136`) |
| Preview | the reader (`x`, F1, and the Look text via spec S5) shows: each slot, installed fit or "sheared"/"rotted", the rule "the pump runs at the grade of its weakest assembly", the resulting grade for each candidate fit given the other slot, relapse horizon in turns, and the exact consumption and spent/returned lines. Consumption and result are stated *before* commitment. |
| Blocked | missing materials, stale zone, actor unavailable (non-Player, dead, blocked), head not current owner (replaced, duplicated), second head bound to the same site (second refuses), site already StableRepair, site Contracted |
| Failed | any callback or validation failure after payment restores the pack **and** the site (see hazard below); nothing stays half-fitted |
| Interrupted | not applicable (single action); closing the menu before choosing costs nothing |
| Saved | fits persist in `ComponentSlotsPart`; stage and relapse persist in `SettlementState`; relapse is reconciled by the existing arrival path (spec section 1 item 8) and on read the failed fit shows as failed; F5 and fresh Continue reproduce exactly |

**Hazard (carried from reading the code):** `SettlementManager` mutates directly and is not transaction-aware (`RepairPlaceSite` `:365-407` [V]). The cradle must wrap the site commit in `tx.Do(apply, undo)` with a captured copy of the `RepairableSiteState` fields (Stage, OutcomeTier, ResolvedByMethod, ResolvedAtTurn, RelapseAtTurn) and must not emit its announcement until `AfterCommit`.

### 2.6 One repair authority (no second path)

Extract the post-validation body of `RepairPlaceSite` into `SettlementManager.CommitPlaceSiteFix(settlementId, siteId, lasting, relapseTurns, text)`, behaviour-preserving. `RepairPlaceSite` calls it unchanged; the cradle calls it with the grade's relapse. The concern completes through `IfPlaceSiteStage ...:StableRepair` (`ConcernRegistry.cs:108-111` [V]) with no change, because the cradle writes the same site state. Golden test first (section 4). The Elder conversation route (`[Repair the pump.]`, `[Patch the pump for now.]`, `ConcernConversation.cs:208-214` [V]) is untouched.

### 2.7 Alternatives the player keeps (at least two, including doing nothing)

1. **Elder route, unchanged:** hand the materials over in conversation (Lasting or Patch). Still the cheapest and clearest path; the cradle does not replace it.
2. **Clay seal:** four FireClay, the level dries, Choir reputation -3, the grove spring trickles (`Concerns/Cinderhold.json`, approach `seal` [V]).
3. **Clear the north field** (Mosshulk and two CanopyStrangler); sets `LevelCleared`, no pump work.
4. **Do nothing:** "the lads bail"; refusing is a spoken, enacted no (`ConcernConversation.cs` RefuseAction [V]; lore R9).
5. **Buy the parts or reclaim them:** smith shelf and Quartermaster stock, or M3a/b/c outputs. Spikes compete with ordinary weapon assembly (IronSpike is a blade component); M3a yields one spike per find while the pump needs two (`MACHINE-RECLAMATION-PROCESSING.md` [R]), so the choice of where spikes go is real.

### 2.8 Cross-system consequences (outside its own menu)

- `PumpRunning` (set when the concern completes at StableRepair): Cinderhold prices x0.95 and a one-time Quartermaster top-up of 2 IronSpikeComponent (`PlaceConditions.json:518-534` [V]); quest reward 2200 XP and 50 drams (`Cinderhold.json` [V]).
- Finite iron competition between pump and weapons (item 5 above).
- Relapse of a patched grade returns the site to `Fouled` with the authored "while you were away" message (`PlaceSites.json:143-145` [V]); the village-regeneration hazard on relapse is closed by the retention marker (spec S2).
- No Choir penalty for mending, and the sealing penalty is preserved (`lore.md` section 2 row "Cinderhold concern" [R]).
- Zone retention for a state-bearing fixture (spec C2), visible in `diag worldgen/ZoneRetained`.
- **Owner decision (default: no change):** whether "running" becomes a live state (set while patched or stable, cleared on relapse). Default keeps the shipped semantics, so grades below Stable have no world consequence beyond state and message. Without this decision the grade-2 fit is a hollow axis; that is the honest reason V1 is a prerequisite increment.

### 2.9 Zero-machine alternative the owner should weigh first

Add one `Middle` fix to `PlaceSiteData` (materials, tool, relapse turns, text) and its conversation choice. Data plus a small `RepairPlaceSite` branch; no fixture, no Part, no placement, no census change. It delivers the same extra durability grade. The cradle adds only: a physical object, per-assembly mixing and the slot capability for later machines. If the durability choice is the only goal, take this alternative and leave the cradle `Pending`.

### 2.10 Canon-safe naming and operators

- Machine name (draft; needs an intent card and the fresh-context blind gate, lore R10): **"mending cradle"** (Concord register; `lore.md` section 8 row 3), shown on the fixture as **"pump head in its cradle"**. Surface-village variant: "mending bench". Recension "cradle" and Pale Curation "conservation case" are not used here (those factions' craft is non-restoration or invisible preservation).
- Draft reader voice (not shippable): "Rod sheared at the collar. Binding rotted through. Fit what you carry; the pump runs at the weakest of the two." Concord register: concrete nouns, no thesis sentences, no "free to take" (lore L15).
- Operator roles: none in V1. Roles that could later operate a bench: Tinker, Weaponsmith (Cinderhold), Concord Factor for released-goods paperwork, Quartermaster. Roles canon needs but code lacks (Conservator, Cleaner) are not required.

### 2.11 Canon landmines V1 must avoid (ids from `lore.md` section 7 and 1)

| Id | Landmine | V1 handling |
| --- | --- | --- |
| L4 | Choir crimes by machine (sealing, burning, digging) | mending adds no Choir penalty and removes none from sealing; no heat; no digging |
| L9, R8 | material collapse | fit validator rejects protected material identities; fits are plain iron, timber, leather, cord, clay |
| L16 | technology nouns | banned-vocabulary load check; no regulator, motor, cell, power |
| L15, L17 | Concord "free" wording; blind gate not run | every authored string is a draft until the blind gate passes |
| R1 | Urqu intent | the "grove wanted its spring back" line stays folk story in the Elder's mouth; no machine text attributes intent |
| R7 | sacred fixtures | `Gantry:GuestPole`, guest locker and any well under oath are never valid sites (validator, test) |
| R9 | closure and spoken end | V1 creates no standing commitment (no loan, no ownership), so no spoken-end verb is needed; the existing Elder refusal stays |
| L5, L6, L7, L8 | Curation, Recension, Bower, catacomb restoration crimes | cradle is not offered in those places; plaque re-cutting, text restoration, compositions and bodies are never inputs (exempt list, section 5) |

### 2.12 Pitch behaviours deferred, and why

| Deferred | Why | Trigger |
| --- | --- | --- |
| Free-standing bench holding a carried damaged apparatus (the real "cradle") | no carried apparatus with two independent assemblies and a physical action exists; prior inventory found none | owner approves one authored finite find (e.g. a Concord released-goods lot) with a real parametric action |
| Substitutions that change throughput, noise, power | no such axes; canon forbids power and motors | pump flow (machine 7) for lift/stroke; dynamo (9) only after the stored-work canon decision |
| Pre-Felling relic restoration | protected: may restore operation, never meaning or translation (`lore.md` M3 "Pre-Felling finds", R2, R3) | owner decision on a canon-safe device class |
| NPC mender / keeper, fees, ownership terms | S10 is gated by critique; no operator is needed for V1 | S10, S20 |
| Re-swapping sound assemblies for pure tuning; wear; compulsory maintenance | would be padding or a maintenance tax | a second consumer with a real capability |
| Armor/weapon restoration | the forge's reforge and ordinary repair already serve it | none; stays out |
| Other sites (Tine Weir, Wellmeet Scales, Drowned Ledger SurveyLine) | each needs its own physical fixture, faction rule (Scales: gift, never priced; SurveyLine: bodies fenced) | a second two-consumer use of `ComponentSlotsPart` |

---

## 3. Contract fit against the architecture spec

| Need | Spec status | Action |
| --- | --- | --- |
| Saved-Part golden, additive Part | S1 | hard prerequisite; `ComponentSlotsPart` and `PumpHeadSitePart` enter the golden as additions and pass the hygiene lint |
| Retention of a state-bearing fixture in a relapse-capable village | S2, E07 | hard prerequisite (`Retention: Zone`) |
| Disabled rows with reasons | S3 | soft prerequisite; fallback is omitted rows |
| Reader contract (`IDescribeState`) | S5 | soft prerequisite; fallback is `ExaminablePart` text |
| Generic placement of a solid fixture | S6, S7 | uses row data; solid blueprint needs the route and standing-cell checks the spec already defines |
| Station contract, process engine, quote modifiers | S4, S12, S13, S14 | **not used by V1** (no bench, no process row): V1 stays independent of the large shared-core slices |
| Component slots, graded faults, object-level alternatives | **missing from the spec** | propose spec contract **C12** (below) |
| Shared place-site commit service | **missing** (`RepairPlaceSite` is the only authority and is not transaction-aware) | new behaviour-preserving extraction (section 2.6); belongs with S22 |
| Install/rebind of a detachable assembly | S19 (home/base) | not V1; V2 bench would be the second consumer |

**Proposed spec addition C12 (slots and graded fits):** a data-defined slot capability (`ComponentSlotsPart` plus `Mending.json`) that stores installed fit ids by slot, derives faults from the owning authority (the place site, or later a `RepairablePart`), exposes a read-only fit preview to the reader contract, and mutates only inside a caller-provided `InventoryTransaction`. Two-consumer rule (spec R1): V1 is the first consumer; the second must exist before the contract generalises (candidates: a carried find on a V2 bench, or `WeaponAssemblyPart` behind a read adapter). Until then C12 stays internal to the cradle.

Stay out of: `StationPart`, `ModifyProcessQuote`, `WorkJobPart`, receivers, stored work.

---

## 4. Tier, sequencing and RED-first tests

**Tier: wave3-needs-new-system.** Smallest wave 3 possible: one new instance Part, one data table, one shared-service extraction; no new engine, time system or station kind.

Slices (isolated-clone cycle about 5 minutes; each ships one complete testable behaviour):

| Slice | Content | Cycles | Depends on |
| --- | --- | --- | --- |
| C0 | owner decisions (section 7); actual Cinderhold baseline of candidate cells; no code | 0 | none |
| C1 | `CommitPlaceSiteFix` extraction with golden of `RepairPlaceSite` truth table, behaviour-preserving | 1 | none |
| C2 | `ComponentSlotsPart`, `Mending.json` registry and validator, round trip, hygiene | 1-2 | S1 |
| C3 | fixture blueprint, `PumpHeadSitePart`, fit rows, transactional site commit with undo, reader lines | 2 | C1, C2, S3, S5 (soft) |
| C4 | spec row `cinderhold-pump-head`, retention marker, placement determinism, cached control | 1-2 + actual play | C3, S2, S6, S7 |
| C5 | actual-play acceptance and evidence | n/a | C4 |

**RED-first tests** (names; each positive has a counter-check):

Shared authority (C1, written green first, then the extraction):
- `RepairPlaceSite_TruthTableGolden_UnchangedByExtraction` (every refusal code, message and state for all 5 sites, lasting and patch)
- `CommitPlaceSiteFix_LastingSetsStableAndClearsRelapse`, `..._PatchSetsTemporaryAndRelapseAtTurn`, `..._AlreadyStableRefuses` (counter)

Definition and Part (C2):
- `MendingRegistry_RejectsUnknownBlueprint`, `..._CreatureOrNonPortableFit`, `..._ProtectedMaterialFit`, `..._GuestPoleSite`, `..._DuplicateFitId`, `..._GradeOutOfRange`, `..._BannedVocabulary`, `..._OneBadRowDoesNotDropOthers` (each with a valid-neighbour control)
- `ComponentSlots_RoundTripsPublicFieldsThroughSave`, `OldSaveWithoutPartLoadsAndIsInert`, `ComponentSlots_HygieneLint_NoPositionalFields`, `SavedPartContract_AdditionOnly`
- `GradeIsWeakestSlot` (property sweep over all fit pairs), `GradeThreeCostsExactlyTheShippedLastingMaterials` (parity), `NoGradeOneFitOffered`

Fixture and verbs (C3):
- `FitRowConsumesExactUnitsAcrossTwoStacksAndCommitsOnce`, `ShortStackShowsDisabledRowWithReason`, `EquippedUnitNotConsumable` (counter), `LiveRecoveryPayloadStackRefuses`
- `FitCommitsThroughSharedAuthority_ConcernCompletesAtStable` (drives the real concern trigger), `PatchedGradeSetsRelapseHorizonFromTable`
- `CallbackThrowAfterSiteCommitRestoresSiteAndPack` (hazard), `CallbackMovesHeadRefuses`, `StaleZoneRefuses`, `NonPlayerActorRefuses`, `SecondHeadBoundToSameSiteRefuses`, `ContractedSiteRefusesAndSaysSo`
- `UpgradeReturnsOnlyReturnableFits_ConservesUnits` (property sweep), `RelapseShowsFitAsFailedWithoutSecondAuthority`, `ElderRouteStillWorksWithHeadPresent` (counter), `DoubleSubmitChargesOnce`
- Reader: `ReaderListsSlotsGradesAndSpentReturnedLines_QuickAndDocument`, `ReaderIsReadOnly` (no events, no diag mutation), `NoStringContainsBannedTechNouns`
- Diag pins: `furniture/MendingFitted`, `furniture/MendingRejected { code }` for every refusal, `furniture/MendingUpgraded`

Placement and retention (C4):
- `FreshCinderholdHasExactlyOneHeadAtDeterministicCell` (seeds 64, 704860946, 1729 plus a 24-seed sweep), `OtherOwnersIdenticalWithRowRemoved`, `RngUntouched`, `FloodConnectivityAndRoutesPreserved`, `MissingBlueprintOmitsOnlyTheRow`, `CachedCinderholdNotRetrofitted`, `GameSessionRoundTripThenGetZoneAddsNothing`, `MarkedZoneSurvivesPatchRelapse` (hazard control)
- Lore pins: `FitsNeverCarryProtectedMaterialIdentity`, `MendingOnlyBoundToCinderholdPump`

Adversarial file (`MendingAdversarialTests.cs`): state atomicity and rollback, parser malformed inputs, save reach, cross-actor (NPC, other player id), stacking, anti-exploit (spike duplication by fit/upgrade cycles; sell-back profit loop; relapse then re-fit repeated), boundary nulls, diag dispatch order.

Cold-eye pass with both audit angles; hypothesis-driven pass on "player upgrades while the concern node is open", "relapse while menu open", "head and Elder route used in the same turn".

---

## 5. Census and placement posture

- Cradle rows in `Installations.json`: only `cinderhold-pump-head` (after C4). Everywhere else `Pending` with a named owner or `Exempt` per section 1.5.
- Exempt (canon): First Tent, Quiet's Door, Olderdeep (plaque and niche restoration forbidden), Lampwell and Spivenor (plaque re-cutting), Salt-Vault, Quillhold (conservators replace nothing), Stillleaf (custody ledger), Deepest Cathedral, Ginmere.
- Placement limits to state in every evidence record: exterior cell only; first visit in old worlds gets it, visited worlds never; entity ids shift by one; glyph presentation only (graphics cancelled); Linux and Deck unexecuted.

---

## 6. Acceptance (actual play, when C4 ships)

Fresh seed 704860946 and seed 64: walk to Cinderhold; find the head; `x` and F1 readers; read each disabled row's reason; acquire parts ordinarily (smith stock and M3 outputs, no injection); fit the weakest grade, observe stage and message, then upgrade the weak slot to Stable; Elder report completes the concern; observe `PumpRunning` effects (price x0.95, spike stock); F5; fresh Continue; relapse path with the shortest relapse (150-turn Weir precedent shows the harness); **cached-save control**: an older Cinderhold save has no head and the Elder route works; **decline path**: refuse the concern in conversation, nothing breaks. Evidence states: durability-only capability; "performs its real action" not yet satisfied (section 1.3); CUA-only images; no performance claim.

---

## 7. Owner decisions, risks, limits

Decisions needed (this dossier does not decide them):
1. Is the cradle wanted before the pump chain's physical action exists, given the verdict (default: no)?
2. Take the zero-machine "Middle" fix first (section 2.9)? (default: yes if only durability is wanted)
3. Should "running" become a live state, set while patched or stable and cleared on relapse (default: no change)? This alters the shipped economy for the Elder's Patch route.
4. Cradle at Cinderhold only, or also a second apparatus (a carried Concord released-goods lot) for V2?
5. Blind voice gate owner for every authored string.

Risks: shared-service extraction touches accepted settlement code (mitigation: golden first); a hollow capability axis (stated); iron-spike scarcity may make top grade rarely reachable (finite by design; M3a yields one per find); `SettlementManager` is not transaction-aware (hazard handled by undo); Cinderhold map faction (Concord) versus concern faction (Villagers) data inconsistency is unrelated but noted (`lore.md` section 2).

Limits: no code read for pump-specific patched consumers beyond grep; light relevance to visibility unverified; exact Cinderhold cell geometry unverified; `JsonUtility` quirks per spec S6 [U].

---

## 8. Verification record

Read this session [V]: `MACHINE-ROSTER-VISION.md`, `SYSTEMS-DESIGN-PRINCIPLES.md`, `MACHINE-SYSTEMS-DESIGN.md` (rows and sections 54-166), `MACHINE-FIELD-WORK-RIG.md`, `MACHINE-RECLAMATION-PROCESSING.md` (first 60 lines), `MACHINE-COMPOSITE-PROCESSING.md` (selected), `MACHINE-TORCH-READOUT.md` (selected), `ARCHITECTURE-SPEC.md` in full, `RepairablePart.cs`, `RepairRecipeRegistry.cs`, `Tier1Repairs.json`, `PlaceSiteCatalog.cs`, `SettlementManager.RepairPlaceSite` and `SyncConditions`, `PlaceSites.json`, `PlaceConditions.json:440-560`, `Concerns/Cinderhold.json`, `ConcernRegistry` triggers, `ConcernConversation` repair choices, `ConcernItems`, `FieldWorkRigRules.cs`, `ForgeWorkReceipt.cs`, `MaterialProcessPlan.cs` (first 110 lines), `WeaponAssemblyPart.cs`, `BrokenEffect.cs`, `CuttingToolPart.cs`, `ArtisanRepairServicePart`, `WellSitePart.cs` (header), `SaveSystem.CanSerializeType`, Objects.json scan of `Repairable` blueprints, `MorrowfastQuests.cs` bell branch.

From research [R]: `lore.md` (sections 0-8, 10-11 read in part), `arch.md` 2.1-2.10 and 7.6, `settlement-*.md` cradle rows, `settlement-cinderhold.md` selected lines.

Not read [U]: full `MaterialProcessService`, `ForgePart` verb handler, `SettlementManager` relapse reconcile bodies, `RepairableSiteState` fields, test-directory conventions beyond the spec, Cinderhold composition cell geometry.

No test, build, Unity run or git write was performed.
