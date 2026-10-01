# Tier 1 repair runtime handoff

Status: runtime frozen for root's native tests. No commits/push. Only assigned files edited; owned-files.json and frozen-postimages.json are exact.

## Implemented contracts

- CompositionPart.MaterialsRaw is comma-separated exact case-sensitive structural categories, independent of MaterialPart reactions.
- RepairRecipeRegistry lazily loads Content/Data/Repairs/Tier1Repairs. Immutable recipe records; three initial IDs; quantity 1..64; invalid or duplicate catalogues install no partial definitions. Validate(factory) catches missing/nonportable/creature supplies.
- RepairablePart has RecipeId, Repaired, RepairCommand="RepairObject", Describe(), TryRepair(actor,zone), static BlocksFunction(owner). One current ground repair owner, one composition Part, live local player, valid recipe/category, surviving structure. No combat HP healing, item BrokenEffect removal, quest-state mutation, tools, skill/quality, deterioration or general object resurrection.
- Payment selects exact genuinely carried units across positive stacks, rejects equipment aliases and foreign/stale links. Claims actor/target/materials; scoped InventoryTransferSnapshot plus saved repair flag share outer PerformInventoryActionCommand receipt. After-action exceptions refund repair units and preserve independent later inventory changes. Success prose/diag/dirty hook waits until commit.
- WellPart.IsUsable gates both direct draw/menu and WaterVesselService source selection. Absent Repairable retains ordinary well behavior. DoorPart.CanOperate gates jammed gates; no lock/key authority changes, closed-state/collision remains Door-owned.
- Diagnostics: furniture/ObjectRepaired after commit, furniture/RepairRejected per native repair gate, furniture/WaterDrawRejected for attempted damaged well draw.
- Normal public-field serialization carries recipe/composition/repaired flags without a format bump; native entity graph round-trip covered. Root owns full zone-retention integration.

## Evidence

- initial-red.log: compiler RED CS0246 missing RepairablePart after core tests were written, before production definitions. No assertion-red claim for that first pass.
- core-green.xml: 12/12 core cases.
- adversarial-first.xml: 60/60 (12 core + 48 taxonomy cases), no new issues in that first sweep.
- hypothesis-red.xml: 68 cases,64 pass / 4 fail. Four additional malformed-state holes confirmed: retained stale Part facade dispatches replacement fault; duplicate repair Parts accepted; duplicate composition Parts accepted; equipped dictionary alias could become carried payment.
- final-green.xml: 68/68 after minimal guards. Eight hypothesis cases:4 confirmed bug fixes, 4 already-correct pins.
- affected-regression.xml: 207/207 (68 new + 139 existing) after fixes. Old classes: DensityGeneratedDoorTests, DensityGeneratedDoorAdversarialTests, SettlementManagerTests, WellSitePartTests, FinitePoolDrawDirtyTests, DensityEverydayAdversarialTests, DensityEverydayIndependentReviewTests. PartRoundTripHelper included unchanged as test dependency.
- Failed first selection for wider sweep was test-harness omission of PartRoundTripHelper; added existing helper to isolated scratch selection, no repo runner edits.
- All reference evidence uses isolated Tools/EditModeRunner with COO_REPO root. It proves game-rule/transaction/save behavior; cannot establish native input, render, world placement, exact Unity seed, scheduler or feel. Native runtime/content/input/art acceptance remains root-owned.
- git diff --check on owned paths passed.

## Cold-eye Q1-Q4 / scope

Q1 symmetry: both direct well water and waterskin collection consult the same availability predicate. Door behavior enters existing CanOperate shared by AI/direct use, not a separate UI-only flag. Ordinary fixtures retain old behavior.
Q2 consistency: one immutable recipe schema, one command/transaction path across all three faults, same exact composition/payment ownership rules, committed success diagnostic and rejection reasons. Public saved fields match blueprint param contract.
Q3 counters: composition mismatch vs material-reaction lookalike; insufficient/wrong/zero/negative/forged/equipped/foreign supply; actor dead/stunned/nonplayer/distant; ground-target integrity; malformed catalogues; already-repaired idempotency; native save both states; HP never healed; distinct same-blueprint target unchanged; after-action rollback/no success receipt; nested replay denied. Dedicated 56-case adversarial file includes 8 hypotheses.
Q4 docs vs source: follows Tier1 three recipes2 FireClay / 2 SalvagedTimber / 1 KnotflaxCord, no later tiers. No blanket legacy replacement. Honest limit: manual composed test owners prove runtime; actual content and geography are pending root acceptance.

Performance: no new per-frame/per-turn listener, MonoBehaviour, or cache. Shared BlocksFunction loops owner Parts with no allocation; composition splitting and payment list/snapshot allocations occur only when an action or explicit description asks. Catalogue cached lazily. No need for a new frame profiler for this action-only slice; root art may have separate requirements.

No remaining yellow/red finding in this bounded runtime slice. This is not proof of bug-freedom. Root integration must still provide exact transactional menu turn handling, Examine/guidance, useful visible fault models, original supplies/source placement, genuine current spawn access and changed-zone persistence.

## Native menu regression follow-up

Added RepairCultivationInputTests (8 cases) at root request. Root owns native RED by temporarily restoring the original world-menu dispatch branch, then GREEN against the new branch. The fixture uses the actual menu and native command selection with a clean cached zone, exact real repair/crop blueprints, real supplies and prepared ground. Covers each command's success, execution-time refusal, before-action veto, after-action rollback; exact one paid action or zero time; materials/crop/output rollback and preserved bed. No Unity invocation by this agent.

Read-only placement review raised: give the repaired gate a meaningful fence/aperture context; prove access from actual Morrowfast boundary arrival rather than any east-edge component; pair rollback removal of Plantable with NotifyEntityTagRemoved. These concerns were sent to root; no root-owned source edited.

## Final site regression follow-up

Added RepairCultivationSiteTests, eight cases, before root native regression. Covers wrong-zone refusal; missing repair/crop dependencies followed by clean retry; preserving every original owner ID and position; no repeat install/refill; actual generated seeds 64 and 1729 reachable from east-center arrival; exact prepared soil survives harvest and explicit unload; full native saved world retains repaired well, depleted clay bank, harvested crop's physical cord/seed and prepared bed through save/load/unload. No production edits or Unity calls for this follow-up. Root owns native result recording; these additions are not included in the historical 207-case reference count.


## Final integration cold-eye pass (2026-09-30)

Reviewed RepairCultivationSite, MorrowfastStartingGarden, zone-generation/retention integration, exact content/loot/initial stocks, material/soil guidance, UI dispatch and repair/crop runtime together. No further significant runtime findings. Root reports its native walkthrough 21/21; this review does not independently claim pixels, exact native seeds or native execution.

- Q1 symmetry: repair payment and state use one rollback receipt; repaired well drawing and vessel filling share availability. Crop placement/release and rollback preserve the exact crop/bed. Site cultivation tag additions have matching removal notifications on rollback; existing terrain stays literal.
- Q2 cross-feature consistency: all three explicit fault recipes match their compositions and paid materials, both supply channels exist, initial Morrowfast stock matches restock tables, and the seedkeeper stock validator knows the added seeds. The original settlement repair quests remain separate.
- Q3 counters: source-preservation, missing dependencies, repeated install, actual east-center arrival, depletion/soil persistence and native-save cases are in RepairCultivationSiteTests; successful/refused/before-veto/after-exception real-menu paths are paired in RepairCultivationInputTests. Core/adversarial repair cases remain, with three new before/after authored Examine cases. No assertion of arbitrary-seed guaranteed placement.
- Q4 doc/implementation: bounded retention covers the finite authored site and prepared beds, including harvested-empty plots. New plants require prepared beds and active-area watering/growth, remain standing at maturity, and yield ground produce plus one returned seed. The gate restores ordinary close/open operation, not a region unlock. Existing cached content receives no migration. These match the stated limits. Root owns final status/evidence updates to the living doc.

### Fixed review finding: stale broken description after repair

Objects.json originally kept permanent damage prose on all three repaired structures and a jammed display name on the gate; ExaminablePart appended it after the live repaired line. Added three real-blueprint command/Examine regressions in MaterialRepairTests before changing the content. `examine-red.xml`: 3 tested, 0 passed, 3 assertion failures. Surgical content-only changes replace static damage prose with construction descriptions and rename the gate to allotment gate. `examine-green.xml`: 3/3 pass. `examine-json-diff.json` proves only RepairLinedWell, RepairRopeWell and RepairWoodenGate changed semantically. `git diff --check` passed. This is reference-runner evidence pending native final sweep.

Changed paths for this follow-up only: Assets/Tests/EditMode/Gameplay/Repairs/MaterialRepairTests.cs and Assets/Resources/Content/Blueprints/Objects.json. Root owns the latter overall; only four values on its three new blueprints were touched. Selected 28 existing affected native fixtures are in affected-native-fixtures.json. All Assets edits frozen again.
