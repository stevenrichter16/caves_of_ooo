# M2a proposal: contained liquid and recoverable equipment

Status2026-11-16: design/source critique and actual fresh Cinderhold placement baseline complete. No production changes, native RED or accepted new mechanics yet. M1a–M1d and current Mac sourceb93ad80a9 remain accepted; do not repeat those branches. This proposal establishes contained-source handling, not a completed sump pump or a reason to make ordinary flask use inconvenient.

## Optional recovery choice

Add one discarded maintenance-oil tray, explicitly released by the extraction crew for salvage. It holds12 finite units of registered `oil` and one actual `CinderhookAxeHeadComponent` owner. The tray, release and contents are new authored content, not pre-existing canon. Cinderhold's existing smith still sells the same head. Existing routes, services, stock and quest outcomes remain unchanged.

A traveller with a compatible flask can draw off the oil and recover the head cleanly. A traveller without a flask can explicitly reach through the oil and recover the same head with disclosed oil exposure. Leaving it or buying equipment remains valid. Captured oil is useful through the existing finite ground-pour, slipping and ignition rules. It is not the discrete LampOil item and cannot silently refuel torches. The head has the existing1d8/−2hit tradeoff and ordinary assembly/refit uses; no new weapon recipe is needed.

This is not a locked essential route or invented water shortage. The actual12-capacity flask should collect the whole12-unit source conveniently in one action. A pump still requires a later independently useful setting. Do not enlarge the tray, exaggerate danger or add repeated bailing to create demand for a machine.

## Canon and source corrections

Root Lore/README.md,10_Bible.md as amended by11_SecondSpine.md, and MYSTERY-LEDGER.md govern. Lore/History/02_Geography.md describes Cinderhold's iron/Black-Gall extraction and practical Concord economy. The shipped `Concerns/Cinderhold.json` already discusses a sheared mine-pump rod, rotted binding and manual bailing; `PlaceSites.json` defines repair materials. Those are abstract place-condition/concern systems, not a physical drainage simulation. Tray recovery must not complete the concern, award its reward, drain the mine or change the communal spring. This is maintenance salvage, not Palimpsest witness material or a protected mystery.

The Peat Works mire was rejected as a pump qualification: collectible `LiquidPoolPart.Volume` and `TileStateSourcePart`'s independently renewed ground coating are different quantities. Depleting one cannot promise a dry route. Existing wells/cisterns also remain available; no scarcity claim is justified.

Independent source critique rejected a `LiquidPoolPart.Contained` shortcut. That Part changes its owner's appearance, coats entrants, projects/unprojects floor state, changes solid impact handling, influences source mixing/navigation/audio and is classified as a drowning medium. A flag checked in only three places would leave inconsistent world rules.

Use a separate contained-source Part with saved registered identity, finite volume/capacity, and no automatic ground projection, contact, refill or regeneration. Adapt it into the existing compatible-flask Fill path, reusing the same registered liquid and quantity arithmetic. Ground pools retain all existing semantics. This is one bounded source adapter, not a general pipe network or chemistry framework.

The initial inventory audit confused the90-unit BrinePool source with flask capacity. Retained M1a actual receipts show12-unit flasks. The pilot therefore uses12 source units to permit one ordinary fill; no90-unit vessel is assumed or introduced. This correction is required before native tests or production.

## Proposed transaction and access contract

- The actual head exists in `ContainerPart.Contents` from fresh generation. Never fabricate another head when opening, drawing liquid or reloading. One saved identity is transferred once.
- Normal Fill draws `min(receiver free capacity, source volume)`. Claim/pin source, vessel and actor; retain exact liquid identity, source/receiver capacities and pre/post quantities. Revalidate owned Parts, current cells/custody and expected quantities before outer commit. Rollback restores both scalars. Empty tray persists at0 with its remaining head. No deposits or source refilling in this pilot.
- Normal Open/Take/TakeAll/direct `TakeFromContainerCommand` must agree: ordinary clean taking requires zero liquid. Wet recovery is a separate, explicit action that uses the same custody transfer rather than bypassing it. Reject ordinary deposits even after the head has gone. Invalid/remote/stale/cancelled/capacity-failed actions spend nothing.
- After successful wet recovery commits, attempt the existing `LiquidCoveredEffect` once, honoring vetoes and stronger existing coatings. Provisional contact dose is `min(12, remaining source volume)`, distinct from collectible units and not a volume conversion. Oil's current dry-down is7 exposure per ordinary turn before heat adjustment; this is a brief exposure, not a durable punishment. Actual play may show it is a nearly free alternative; preserve that finding rather than inflating hazard.
- Preview exposure truthfully: oil can worsen heat injury while it remains. It does not automatically ignite the actor, guarantee a new Oily label over a stronger coating, or make the body itself a slippery floor. No exposure on failed transfer. No heat, elapsed work, wall-clock progress or unloaded production.

Required source seams: `LiquidVesselService`, `LiquidVesselPart`, `ContainerPart`, `TakeFromContainerCommand`, `PutInContainerCommand`, existing inventory transactions and `LiquidCoveredEffect`. Natural-pool source purity must remain unchanged; contained oil must neither contaminate nearby ground water nor be reclassified because its supporting tile is wet. Solid tray impact behavior and unrelated floor films must remain ordinary.

## Actual baseline and placement

`Verification/MachineSystems/ContainedOilBaseline` starts from the earned TorchReadout/RelitCheckpoint2320HP40, seed704860946, Morrowfast3.6. Ordinary ascent and three world-map steps reached Cinderhold before first entry. `BeforeEntryCheckpoint`2390HP40 on WorldMap36,9 has no cached6.6 zone. This is the fresh-content acceptance branch; do not retrofit its later cached SiteCheckpoint.

Fresh entry2400HP40 at39,11, ordinary workshop walk to58,6 at2640. Look and full reader disclose sole StoneFloor11794 at58,5, interior, currently not generation-reserved. Forge13518 at55,5 and anvil13519 at60,5 remain. Weaponsmith13520 was wandering at41,15; its authored position is55,7. Source plan places the candidate relative to the workshop at`work.X+8,work.Y+3`; before implementation verify all variations and add its reservation in the authored optional profile before generation receipts/actor furnishing. No blanket reservation relaxation, late scatter or cached retrofit. The stage must omit only optional tray content if its validated dependencies are unavailable.

Existing before images are actual GameBootstrap views, not an after mockup. Approved matching tray/body art must be identified before the fixture is integrated; new art requires the normal imagegen/native-source workflow. The parked Cinderhold forge art draft stays parked.

## Next runnable gates

1. Resolve receiver acquisition through ordinary existing stock and the head's useful refit supplies; baseline inventory currently has no flask. Source audit identifies local WellKeeperStock's guaranteed12-unit LiquidFlask if the actual WellKeeper is present; VillagePopulationBuilder places that keeper with the main well. Smith has no flask, factor has no Trader, and random Villager TradeGoods is not a fallback. Morrowfast Sella Kettle at47,20 has authored flask stock. Actual NPC location, stock, price and purchase remain unverified here;15drams cannot be assumed enough. The earlier Sill22-dram/Wellmeet23-dram purchases are retained alternatives, not a local price promise. OakHaft and LeatherBinding each have basevalue8 and real smith stock; actual combined price depends on current trade conditions. Do not inject supplies. Actual tray-choice value remains provisional.
2. Native RED:12→6→0 through available-capacity limits, full12-unit draw, partial/full/incompatible receiver; actor/source/receiver callback replacement/removal/re-entry; rollback and one action; Open/TakeAll/direct-command/deposit bypasses; unchanged natural pools, ground films and solid impacts; one saved head owner; exposure veto/merge/failed-transfer controls.
3. Smallest source/content implementation and relevant regressions. Integrate existing liquid registry, inventory custody and readers; do not extend unrelated repair, crafting, quest or floor rules.
4. Fresh ordinary clean and manual branches, full/F1 previews, conservation, actual head assembly/refit, outside oil use, interruption, travel and fresh reload. Preserve the cached SiteCheckpoint no-retrofit control. Independent source/gameplay review and exact records/commit/push precede acceptance.

All12 complete machines remain open. This slice cannot claim pumping, replenishable storage, treatment, mixture separation, full provenance or machinery construction/relocation.

Independent final bounded document/source review:0 remainingP1/P2 contradictions after the contained-source architecture and12-unit correction. The design/source gate is cleared for nativeRED/implementation; mechanics and gameplay acceptance remain open. Acquisition, usefulness and all actual after-play gates above remain pending.
