# C8 remaining heat/contact audit — private proposal

Status: approved bounded hot-SteamEffect slice, private test-first preparation after the completed liquid renderer repair. Three native-reference test fixtures are authored/compiled privately; no new behavior test execution, production/Assets/shared-doc changes, or native editor access yet. Existing steam-scheduling, thermal baseline, and 165 material tests are previous accepted evidence, not evidence for this proposal.

## Verified corrections before implementation

| Premise | Actual code / consequence |
|---|---|
| Fire abilities do not heat ground | Already fixed: `Pyromancy_FlamingHands.ResolveSpell` writes tile heat through `ZoneTileStateSystem`, resolves tile reactions, then applies `FireDose.Attack` (300) to elemental owners. Do not duplicate this bridge. |
| One Flaming Hands should light TarSeep | Fresh authored Tar begins20, flame200 minus .3×100 volatility =>170, capacity2.5. One direct300 dose yields140 before decay, below170. Failure to ignite is expected from current authored numbers, not proof of missing propagation. Preserve a below/above crossing counter when testing Tar. |
| Steam scheduling is absent | Already repaired: steam-bearing noncreature props get one BeginTakeAction/EndTurn; creatures retain TurnManager ownership. Actual native hot706°C and ambient25°C sources both cooled/wet the neighboring probe and expired. Do not redo scheduling. |
| SteamCloud is the source of SteamEffect | False. `water_plus_fire` applies SteamEffect to the original wet/burning owner and separately spawns SteamCloud (Thermal120, Lifespan3, no SteamEffect). Existing tests explicitly pin this. |
| Vaporized already creates steam | No production listener for `Vaporized` was found. ThermalPart strips Wet and fires that event; its “upstream material reactions pick this up” comment is unsupported. The currently reachable water/fire reaction does not depend on this event. |
| All visible steam has meaningful temperature | False. Tile cloud is only ID+turns; reaction `steam` consumes heat/water and deliberately applies zero occupant damage. Do not infer physical temperature from a white cloud or from current render geometry. |
| SteamEffect already scalds | It always emits negative adjacent heat and Wet, never damage and never reads its owner's temperature. ScaldingVeil separately retaliates against an attacker; its deliberate retaliation contract must not become an aura. |
| Tile heat can simply be converted to Thermal temperature | `ZoneTileState` explicitly documents two intentionally distinct models: coarse0–2 reaction energy vs continuous entity temperature. A bridge needs a named transfer rule and dedup; sharing a number would be an unjustified scale change. |
| Combustibility has one current unit | Parsed501 blueprint entries:68 directly authored positive fields,64 in(0,1],4≥50. Thermal ignition only asks>0, but `TilePropagationSystem` asks≥50. Thus current Bush.6/Tree.45/Hedge.55 can thermally burn while the tile fuel predicate rejects them. This is a real cross-system unit gap, not proof all three should propagate identically: normalized Tree45 would still be below50. |
| Tile embers already heat scenery | PropagateFire writes an embers residue, and tile reactions apply effects/damage only to Creature occupants. Oil ignition has real Fire damage8; ordinary ember residue itself does not ApplyHeat to scenery or creatures. Do not silently add a second8-damage route to oil. |
| Hazard avoidance covers prospective steam scald | Current TerrainNavigationWeight accurately covers liquid contact/slips and gas costs; its comment explicitly rejects invented damage from tile energy alone. A newly harmful steam footprint would need a matching live-source cost, with immunity/escape controls. |

## Recommended first coherent slice: hot reaction-owned SteamEffect

This is a new authored gameplay extension allowed by C8, not a repair implied by the existing scheduling contract. Prefer it before global combustion migration because it has a current reachable source, a current continuous temperature, a bounded neighborhood and existing cooling/wetting.

Proposed behavior for review: a live SteamEffect owner above a named100°C scald threshold emits one small typed Heat hit per susceptible living creature per pulse among its actual physical co-occupants and immediate neighboring physical cells. Ambient/no-Thermal/nonfinite/below-threshold/expired steam remains cooling/wetting-only. Proposed starting hit is2 before HeatResistance, independent of burn intensity; value/threshold remain tuning choices until native matched controls. Preserve the existing .1×density Wet and -20×density cooling semantics. Do not make SteamCloud or temperature-less tile clouds harmful in this slice, and do not redirect ScaldingVeil.

Use one internal live-source eligibility/dose function, shared by scald and prospective navigation cost, so AI does not avoid harmless cool steam. No new saved field required: current SteamEffect.Density and current owner ThermalPart suffice. Old saved steam would gain the newly authored live rule; document that explicit behavior change rather than promising old saves are inert.

Neighborhood rule must be explicit before coding. Suggested physical footprint semantics: gather the source's occupied cells, enumerate immediate8-way perimeter cells, deduplicate target owners through Occupants, and recheck current zone membership/overlap before every callback. Do not use anchor-only Objects for the new damage; that would miss a large creature touching by its body. Damage is once per source pulse, regardless source/target footprint; two distinct hot sources remain two distinct exposures. Existing cooling/wetting should move to the same deduplicated physical contacts only if paired existing one-cell behavior and new footprint tests justify that separate correction. No per-zone all-entity scan, global dedup state or recursive heat writes.

Minimum expected file scope after actual RED: `SteamEffect.cs`; a small pure/shared `SteamContact` helper only if it makes scald/navigation share the actual rule; `TerrainNavigationWeight.cs` if same slice adds truthful avoidance; dedicated core/adversarial tests and one native scenario/launcher. Avoid Objects.json, registry edits, ThermalPart, BurningEffect, TileReaction JSON, SaveSystem, render/material/assets in this slice. A dynamic steam source may sit adjacent to a candidate path cell, so navigation query must inspect the bounded neighborhood/current source occupancy, not just occupants of the destination itself.

## Test-first pairs

1. Actual water_plus_fire reaction owner with retained Steam and hot Thermal hurts one living neighboring factory Player on one material pulse; exactly matched ambient source does not. Preserve equal cooling/Wet/lifetime and source ownership, recording actual landed Heat damage.
2. Hot/noSteam, Steam/noThermal, density0/expired, source detached, actor detached/dead/deathhandled, distant cell, and source self never take a new scald hit. No-thermal creatures can receive typed damage if living; source temperature is the gate.
3. HeatResistance0/50/100 use the canonical CombatSystem path and expected post-resistance amount; negative resistance remains ordinary vulnerability. No explicit bypass of LiquidCoveredEffect's existing reaction to Heat damage.
4. One-cell and multicell source/target contacts deliver exactly one pulse. Same physical perimeter but remote anchor must still count; two touching cells must not double damage. Callback removal, death and relocation must stop stale deliveries; no second death/drop.
5. One ordinary player action triggers exactly one noncreature steam source pulse regardless NPC count; Creature steam is still advanced only by TurnManager. Separate material ticks are deliberately separate pulses.
6. Fresh state and save/load preserve current Temperature/Density and the new rule; no format bump. Empty/unknown effect/source cannot acquire scald semantics by display name or SteamCloud blueprint alone.
7. If avoidance is included: equal alternative route avoids live hot source, cool/noSteam/immune control pays no scald penalty; actual harmful source on all routes never becomes an impassable wall; escaping the hazard stays possible. Existing gas/liquid/slip costs unchanged.

## Native paired counter-scene (after core RED/GREEN)

Reuse existing isolated-save/real-keyboard thermal baseline infrastructure; keep actor normal40HP/resistance, real factory OilSeep source and real native spell dispatch. Stage source/supplies explicitly and label the scenario as a controlled mechanic demonstration, not natural loot/balance acquisition.

1. Start from an isolated normal checkpoint. Native FlamingHands ignites actual OilSeep. Retreat one cell so ConjureWater's actual endpoint2 reaches the source; native inventory-read the bounded supplied grimoire as the existing accepted route does.
2. Wait at safe distance until Burning ends while a live SteamEffect remains; record exact sourceTemperature/density and no concurrent oil/fire floor exposure. Use an adjacent dry legal cell: source actualLiquidPool stays on its own cell. Native approach and one paid wait produce attributable Heat scald, cooling/Wet and readable feedback, then native escape stops further hits. Safety stop above10HP, no HP writes/heal injection.
3. Restore exact same checkpoint for matched coolSteam and hotNoSteam controls. Repeat native source creation first. Only then explicitly stage the diagnostic control (set sourceTemperature to ambient OR remove only SteamEffect), as the previous Steam22 test already labels. Same paid approach/wait/escape produces no scald. These state controls are honest staged counter-scenes, not new obtainable player powers.
4. A core immune pair covers HeatResistance100; do not silently buff the native actor. If native immunity is desired later, use an actually acquired heat-protection source and prove it separately.
5. Compare HP/time/action counters, source/actor IDs, temperature/density, effects, damage diagnostics, exact source graph and raw Unity log byte range. Preserve settings/scenes/save root, bounded timeouts and interruption recovery. Capture live hot/cool/escape frames. Numeric checks can establish attribution/timing/restore; only image inspection establishes readable steam/contact feedback; this does not establish global fire balance.

## Remaining C8 work, kept separate

- Combustibility normalization requires an explicit save/content unit policy. Do not use an unreviewed `<=1 ? ×100 : unchanged` heuristic: a legitimate low percentage and old fractional value are ambiguous. Inventory every direct author and material mutation (Acidic subtracts an absolute amount; Charred multiplies/restores). Pair old/new saves before choosing migration vs authored-version metadata.
- Object↔tile fire bridge needs one named finite rule on the appropriate seam. Already-existing ability bridge and oil damage must not be double-applied. A test-first candidate scene is a real burning Bush next to spilled oil vs nonburning/cool source, and embers adjacent to actual fuel vs inert/no fuel, with exact player action count and bounded range. It is not yet specified enough to implement safely.
- Vaporized→steam hook is genuinely unconsumed, but filling it could spawn excessive visuals or multiply the working water/fire reaction. Treat it as a separate source/lifetime/once-per-crossing feature with explicit cold/noWet/veto/duplicate-reaction controls.
- No new hazard source, universal tile-cloud scald, or global combustion tuning is included by calling the small hot-SteamEffect slice done.

## Review status

Read-only source audit completed; no new behavior RED or native execution claimed. Existing Steam scheduling owner confirms their accepted work promised only cooling/wetting/lifetime and requires a separate design/test-first gate for heat damage. Root approved this slice, including physical co-occupancy/perimeter, typed resistance, feedback and shared navigation semantics. Liquid ambient rendering is now complete77/77 native GREEN as a separate checkpoint.


## Authorized first-slice refinement before tests

Root authorized the hot-SteamEffect slice after liquid color repair. Production remains unwritten. Physical hot-contact includes co-occupants on the source's own physical cells plus the immediate8-neighbor perimeter, excluding the source identity: otherwise stepping onto a non-solid steaming OilSeep would evade the hazard. Existing cooling/wetting is preserved; any physical footprint correction to that older path must have separate explicit counterchecks. No inference of harmfulness for tile-cloud or SteamCloud visuals.

Add live hot/cool SteamEffect wording to EffectDescriber and actual landed scald feedback through the usual message/damage path. The separately spawned SteamCloud lasts3 turns while owner steam can remain longer; its image alone cannot prove ongoing warning/readability. Native acceptance must inspect persistent status readout/feedback and escape, not assume the earlier short-lived puff suffices. Avoid introducing a new aura/art system as an unmeasured prerequisite; if the actual native warning proves unreadable, record it as a failed gate and repair narrowly.


## Performance, callback ownership and diagnostics plan

This is a per-source turn feature, not a per-frame visual or whole-zone scan. Follow `Docs/PERF-FOUNDATION.md` scratch-list pattern: bounded physical source cells plus their nine-cell neighborhoods, deduplicated targets in source-owned scratch storage; pure AI queries use existing struct physical-cell enumeration and no mutable cache or allocations. Preflight eligibility and current occupancy again before each dispatched damage so earlier callbacks may remove/move/cool the source or targets without delivering stale later hits. A per-source active-pulse guard prevents recursive callbacks from multiplying the same source; independent sources and later scheduled pulses remain independent. Private transient scratch/guard fields are not added to save schema.

Use the existing typed `CombatSystem.ApplyDamage` pipeline, which already emits damage attempts, resistance changes, veto/zero outcomes and actual landed damage with source/target identity. Add a bounded `effect/SteamScald` outcome at the new attempted-contact seam (actual landed amount, source temperature/density and outcome), and one `effect/SteamPulseRejected` on invalid source invocation with a named eligibility reason; do not emit fabricated damage success for immune/vetoed contacts or pollute pure AI queries. Feedback describes actual landed scald only. No new diagnostic channel or broad per-cell logs.

Initial private fixtures: HotSteamContactTests (core actual reaction/temperature/resistance/readout), HotSteamNavigationTests (same live source cost, full physical body, pure queries, detour/escape/immune and visual-only controls), HotSteamContactAdversarialTests (existing save-field roundtrip, multi-cell dedup, independent sources/self, callback invalidation, canonical veto/typed attributes, nonfinite density, actual target liveness/type, current committed footprint, same-source recursive pulse and target without Thermal, ScaldingVeil separation). Exact native RED must precede any hot-steam production. Existing scheduling34, liquid/gas/path/thermal and EffectDescriber neighbors remain regression gates; actual native paired route remains mandatory before completion.


### Final source/API recheck before native RED

`SpatialQuery.Distance` and `DistanceToCell` already measure minimum Chebyshev distance over committed physical bodies. Reuse those exact methods for contact revalidation and prospective-cell source distance; do not invent a second anchor/body distance implementation. `Zone.GetOccupiedCells(entity)` uses committed offsets, while its anchor-parameter overload intentionally uses candidate offsets for prospective navigation. The stale raw-footprint pair distinguishes them.

`SaveGraphSerializer.LoadEffect` bypasses constructors with `FormatterServices.GetUninitializedObject` before restoring public fields. Any new private scratch containers must therefore be lazy-initialized; do not rely on a field initializer running after load. The new effect-local active-pulse guard is private/transient, so existing named public Density/Duration and Thermal temperature fields remain the save contract.

Re-read the accepted `DensitySteamNativePlayer.SteamProbe`: it already proves actual OilSeep spell/reaction can stop burning within eight ordinary waits while retaining Steam density>.2 and hot706°C, with no remaining Wet/tileHeat. The proposed native scald route can reuse that reached state before walking into the dry adjacent cell; no added direct extinguish or temperature-setting is needed for the positive route. Only the ambient/noSteam paired controls use explicitly recorded single-field diagnostic changes.


## Final private implementation checkpoint (2026-09-27)

The shared current living document is `Docs/DENSITY-HOT-STEAM.md`; sections above retain the approved pre-test plan. Actual native79 RED:38fail41pass. First effect-local guard was replaced after actual pure replacement-effect RED1/3 with a thread-local active-source set removed in finally, with no saved fields or global entity lifecycle changes. Final attempted-only effect record is `SteamScaldAttempt(requestedAmount,temperature,density)`; no duplicate numeric prose or net-HP landed claim remains. A nested-source diagnostic RED established outer overattribution4 vs actual2 before this root-approved divergence. Canonical damage API/visuals and explicit hot/cool readout remain.

Final private matched270 before225PASS45FAIL ->270GREEN,0newfail45newpass. Feature86 =core30+adversarial29+navigation20+reentry7;184neighborsGREEN. Both native-reference compiles0errors and final peer reads clear. Native GREEN and matched real-keyboard Play are pending. Exact8-path publication manifest and diffs are prepared; no shared Assets writes by this agent. See PrivateCore/evidence.json for raw hashes and exact execution bounds.
