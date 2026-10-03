# Fieldwork cold-eye review: watering and mutable art

Review scope: final uncommitted fieldwork milestone against `Docs/EXPLORATION-DEPTH-NEXT-DESIGN.md`, including a final read-only delivery pass after root reported native door/HUD 158/158, content/art 106/106 and scenes 4/4 GREEN. This is CoO-original work; no Qud parity claim. No Unity calls were made by this reviewer. The final pass changed only this scratch report.

## Result

No significant unresolved issue remains in the reviewed production or its design contract. The pallet cover wording and the subsequently observed damaged-door HUD/menu promise are both resolved. Watering still matches the exact-owner, one-unit, 40-moisture, elapsed-time and rollback contract. The focused command/art pair has been published by root into Assets; native receipts remain root-owned. The ordinary native journey is still being completed by root and is not declared complete by this review.

### Resolved content mismatch: pallet cover wording

- File: `Assets/Resources/Content/Blueprints/Objects.json:50578`.
- Original wording: “haul the whole solid load as cover.” Final inspected wording: “haul the whole solid load as a movable obstacle.”
- Design §4 and the current in-phase self-review explicitly say the low pallet is a movable obstacle and is not advertised as cover. Physics.Solid blocks movement, whereas cell sight blocking uses the Solid tag/closed-door path. The pallet deliberately has no Solid tag.
- Root applied the finite wording correction. The original intended collision/LOS distinction remains intact; no additional sight-blocking mechanic was introduced. Closed by final direct source inspection at the same JSON entry.

## Q1 — Symmetry

- Compared the new watering path to `CropPart.Water`, `CropTime.Reconcile`, existing water-vessel payment and outer `PerformInventoryActionCommand`. The new service settles old world time before its payment snapshot (`CropWateringService.cs:50–59`), revalidates the same crop/cell/vessel after possible legacy yield callbacks, and joins the enclosing transaction. Undo restores only newly paid water/moisture/background (`:73–80`); earned stage progress, stamp and wet fraction survive. This is intentionally different from undoing an entire old-time reconciliation.
- Commit/rollback presentation is symmetric: success marks the real crop cell dirty and emits interaction only after commit (`:88–99`); rollback restores the old background and marks that same current cell (`:79–80`). A carried vessel is not incorrectly used as the gesture target.
- The new gate uses the existing mutable door pattern. `RepairablePart.cs:143` marks `Repair.Completed`; `DoorPart.cs:59` marks `Door.State`; `ConnectedSpread3DLibrary.cs:136–143` resolves buckled/closed/open from actual repair/door state. Its recipe is unbatched and carries the actual quarter turns (`:185–189`). `SpawnRing3DPresenter.cs:218–229` replaces changed model/prefab views and updates orientation. `SameGeometry` includes model, position, batching and quarter turns (`:251–252`). A repaired closed owner therefore does not remain bound to the buckled geometry after the ordinary refresh.
- The existing generic SpreadFieldGate refinement admits its exact blueprint only; it does not override the new wicket's own routing. Model lookup includes the connected extension library, so mutable view creation can resolve all four new imported resources.

## Q2 — Cross-feature consistency

- Both supported physical supply representations use the same exact-vessel command and one-unit cost: Waterskin.Charges and pure-water LiquidVessel.Volume. Skin capacity/quantity, liquid capacity/identity, inventory membership/backlinks, duplicate IDs, source part identity, singleton count and equipped aliases are checked. The last liquid unit empties its liquid ID; rollback restores it.
- Actor/target requirements mirror the intended repair/cultivation authority: living actionable player, exact active zone graph, current planted owner and range at most one, usable prepared bed. There is no biome whitelist that accidentally excludes underground crops.
- Actual cellar Sootroot composition is compatible: `GleanersCellarBuilder.cs:136–145` creates standing Sootroot and adds Plantable + CultivatedSoil to the existing non-solid Floor. `CultivatedSoilPart.IsCultivated` admits it. Bare old legacy crops deliberately remain rain-only, while prepared legacy owners pass the same service after legacy stamp initialization. These are documented compatibility choices.
- Physical pool presence refuses hand watering by the existing prepared-soil predicate. Watering adds no pool/coating and leaves generic pouring unchanged. No additional pour/drain plumbing is proposed; existing flooded-bed counter and native water behavior are the relevant bounded checks.
- Prepared native terrain keeps its original visual identity: `RepairCultivationRecipes.Current(..., retainNativeTerrain:true)` admits the glade Grass, and `SpawnRing3DGroundPatches.cs:183,213–214` fingerprints and overlays the prepared bed. Crop wet/dry art reads the actual saved MoistureTicks through `BiomeCropRecipes.cs:31–39`; there is no separate visual moisture authority.

## Q3 — Counter-checks and anti-duplication

- Existing new watering fixtures contain paired success/refusal for the actual input dispatcher, cancellation, before-veto and after-failure; both vessel types; ripe/no-benefit/empty/wrong-liquid; malformed IDs; moved/removed/foreign/equipped/stack/duplicate sources; action blocking/death; bare/prepared beds; future/fraction/version clock corruption; stale legacy maturity; nested second watering; and functioning/broken well refill.
- The service claims actor, crop and vessel before its debit. Nested watering through the actual command pipeline cannot spend a second source while the outer actor claim is live. No production AfterInventoryAction handler was found that independently rewrites crop moisture during the outer transaction, so a synthetic callback overwriting unrelated moisture is not reported as a normal-player defect.
- Clock overflow inspection: `CropTime.cs:75–80` widens current-minus-last and wet ticks to long before arithmetic, caps whole units by available moisture, and resets fraction when the supply is exhausted. `CropSystem.ConsumeWetUnits` widens and saturates stage progress; advancement loops over at most the existing growth transitions. Existing `ConnectedCropTimeAdversarialTests.LargeElapsedTimeIsBoundedByMoistureAndStages` already probes int-max elapsed state. Watering validates malformed stamps before reconciliation and snapshots afterwards; no new overflow path was found.
- The first integrated run's diagnostic count failures were test isolation defects: separate EntityFactory instances reuse numeric IDs. The published correction scopes receipts by previously seen TraceId rather than ID alone. Final direct inspection confirms the exact correction remains in both assertions. Root's subsequent native receipts supersede the initial failing run; the main design ledger preserves that history without counting failed cases as passes.
- Supplemental check now published at `Assets/Tests/EditMode/Presentation/Rendering/MundaneCropWateringArtTests.cs` (byte-identical to the scratch proposal): two cases use the actual generated dry Sootroot/prepared Floor, select a real carried Waterskin through the crop menu command, execute PerformInventoryActionCommand, and pair committed wet model with outer-failure dry model. It also checks exact one-unit debit/restoration, dirty reason, unchanged crop/soil ownership, no instant growth, imported dry/wet mesh relationship and explicit presenter refresh. No additional production change was required for this composition check.

## Q4 — Design versus implementation

- Shared irrigation matches the recorded one-unit/40-unit top-up, no immediate growth, wet-fraction preservation, pure menu reads, exact source selection, rejection and legacy-owner behavior.
- New content admission is explicitly Enabled + version12; version11 remains accepted by restore (`SpreadExplorationPlan.cs:340`). New actions on existing eligible crops are distinct from retrofitting saved graph content. Glade/cellar fieldwork generation predicates both retain that distinction (`OverworldZoneManager.cs:155,190`).
- The Drawgourd placement reserves its cell before gravel dressing; new wicket replaces the planned wall specification before graph construction. The old hanging-open repair gate remains distinct. Normal destruction, native open/close occupancy and permanent source removal remain owned by the existing systems.
- The source budget matches four forms added to the connected library, with existing material/palette and other model entries retained. The source contact sheet has visibly distinct buckled, closed, open and pallet forms. That is an artifact observation, not a gameplay camera/readability conclusion.
- Final inspected text and model/action behavior match the design. The shared wicket blueprint gives generic damage/repair/bypass information; site-specific garden/cellar directions belong only on the authored glade owner. The pallet text now correctly advertises a movable obstacle. No remaining significant doc-versus-code mismatch was identified; the design status intentionally remains in progress while root finishes the ordinary native journey.

## Evidence limits

Can verify here: source ordering/ownership contracts, actual API wiring, saved fields, model admission/refresh logic, final content text, static source preview, and the directly inspected first native wicket screenshot. Root reports native door/HUD 158/158, content/art 106/106 and scenes 4/4 GREEN. Those counts are reported from the coordinating agent's native receipts, not from a Unity run performed by this reviewer. The published supplemental art fixture is present and unchanged from the reviewed proposal.

Cannot claim here: completed ordinary native journey, all later in-game screenshots, unaided discovery, menu feel through a full expedition, long-term water economy, campaign variety or repeated-expedition appeal. Root is completing the journey and its screenshot acceptance. Human playtesting is still required for balance and appeal.

## Resolved native screenshot follow-up: damaged-door prompt

Root's first native fieldwork screenshot (`c6f535d56d3041239e5244521ff6bec3/fieldwork-02-closed-wicket-and-bypass.png`) visibly advertised “C, W: menu / open door” for the still-buckled wicket. This is a confirmed presentation defect, found after the source-only review above.

`WorldAffordanceQuery.Verb` duplicated ordinary door prerequisites but omitted `RepairablePart.BlocksFunction` (`WorldAffordanceQuery.cs:123–126`). Actual `DoorPart.CanOperate` already used that guard (`DoorPart.cs:34`). In addition, `DoorPart.GetInventoryActions` unconditionally offered open/close (`DoorPart.cs:21–22`), so the menu and HUD both promised a blocked operation. The earlier hanging-open RepairWoodenGate had the analogous misleading close action.

Finite correction applied: use the existing pure repair-block guard in the HUD door predicate and only gate the door menu row by that same prerequisite. RepairObject/Examine remain provided by their own parts; ordinary doors without a repair fault are unchanged. No new repair HUD verb, inventory scan, callback or InputHandler change is needed.

Published tests before production: `Assets/Tests/EditMode/Gameplay/Interaction/RepairDoorAffordanceTests.cs` (10 cases), damaged/repaired wicket and old hanging-open gate for both HUD and actual menu, plus ordinary VillageDoor and SpreadFieldGate counters and a pure-query callback trap. Production patch was prepared outside Assets at `/tmp/repair-door-affordance-production.patch` and published only after root's native RED authorization.

Root subsequently reported native RED with exactly four expected failures (two damaged HUD and two damaged menu cases), with all six controls GREEN. Following that receipt, the two production guards were published unchanged: five added/two removed lines across DoorPart and WorldAffordanceQuery. Scoped whitespace validation passed. Final source self-review confirmed both call the same side-effect-free repair prerequisite used by operation; no new event dispatch, repair/material check, authority change, save field or InputHandler branch was introduced. Root now reports the final native door/HUD selection GREEN at 158/158. The final read-only pass reconfirmed both guards and a clean scoped diff check. This finding is closed. No additional production edits were made in the final review; the ordinary native journey remains root-owned work.

## Final acceptance closure

Root completed native run4045caba70154a09a61ccafcd4b35ed5:19/19, complete=true, no failures or unexpected errors. The actual northern foraging trip replaced the optional Sill report attempt; away-growth, second harvest/next planting and exact F5/F6 replacement graph all passed. Screenshots were inspected and original clean SampleScene restored. Earlier pending journey statements above describe review chronology; this gate is now closed. Human discovery/balance/performance limitations remain.
