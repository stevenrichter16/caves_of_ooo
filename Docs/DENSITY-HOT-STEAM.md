# Hot SteamEffect contact — C8

Status: **complete for the bounded hot-SteamEffect contact and keyboard source-inspection slice**. Actual Unity core270/270 and menu+neighbor50/50 are GREEN. Matched native run `10851284a3874055a979dcc7bdd1b391` passed30 checks across3 cases with16 captures, zero errors and exact editor/input/save restoration in31.0740seconds. Hot approach/wait caused2+2 Heat, escape stopped contact, and cool/noSteam controls stayed at40HP. Hot/cool descriptions and red2 feedback were visually inspected; right-HUD clipping limits complete text-layout claims. Broader64-author combustion normalization is **DEFERRED MEDIUM / balance-sensitive**, with its source plan saved and no content changes. This is a CoO-original extension, not whole-combustion completion.

## Executed first gate and current candidate

Actual RED authority: `Docs/Verification/DensityCompletion/HotSteam/native79-red.xml.gz` and summary JSON, job `0e84c8f1a7ab4df8973c20c9112f543f`, 2.8422202 seconds. Core30:13 RED/17 PASS; navigation20:6 RED/14 PASS; adversarial29:19 RED/10 PASS. Both independent fixture reviews were clear after exact-null reaction-registry cleanup was hardened. Tests restore the captured reaction-list identity, including null, readiness and factory, and preserve the original setup exception.

The private candidate changes four production scripts plus a new helper `.meta`: `SteamEffect`, new internal `SteamContact`, `TerrainNavigationWeight`, and `EffectDescriber`. New damage is **2 typed Heat** from an actual live SteamEffect source whose current finite Thermal temperature is **strictly above100**. Source co-occupants and its immediate physical perimeter are eligible, once per living creature per source pulse. Current physical placement is authoritative, using existing `SpatialQuery` methods; hypothetical navigation uses its prospective body cells and the same source rule. Full Heat resistance removes the new cost; harmful routes remain finite, so escape and sole passages remain usable.

Old anchor-adjacent cooling/Wet application and density decay are intentionally preserved. The new source-owned scratch list/set are lazy because effect loading bypasses constructors; they clear in `finally`. A thread-local set of currently executing source Entity references excludes same-source reentry even when callbacks replace the effect object; it is emptied for that source in `finally` and retains no source between pulses. Distinct source nesting and subsequent pulses remain valid. Current source eligibility and actual target contact are checked again before later damage callbacks. Typed `CombatSystem.ApplyDamage` retains resistance, veto, death and existing damage diagnostics. The new `SteamScaldAttempt` record carries requestedAmount/temperature/density immediately before canonical damage dispatch, without claiming a landed outcome. It may exist when a callback throws or vetoes. `SteamPulseRejected` records named invalid-source/reentry reasons. Canonical damage records and visuals retain landed feedback; hot/cool effect descriptions provide persistent examination text. No global units, content, save format, MaterialSim scheduler, ScaldingVeil, tile clouds or SteamCloud damage changes are in this slice.

### Review checkpoints

- Initial plan and full reference/API sweep precede new production (preserved below).
- Native79 RED is executed evidence; offline compilation alone is not GREEN.
- Core/setup/navigation fixtures and deeper callback/save/physical pairs received separate peer reads. Production preservation/source/readout, source-guard cleanup and final attempt-feedback peer reads are clear. Native270 is GREEN; matched paired Play is now complete below.
- New diagnostics/readout and pure AI eligibility share source semantics. Mutable callbacks, save reconstruction, reentrant pulse, independent sources, retained next pulse and wrong/stale owners are bounded by the authored pairs; this is not a general proof of all future callbacks.
- Live acceptance must inspect persistent hot/cool status wording, actual attributable HP/timing, canonical damage visuals and escape, plus raw log/restoration. The short separately spawned SteamCloud is not sufficient evidence for the warning lasting as long as the source effect.

## Review findings and final private execution

Two distinct callback defects were reproduced before repair. First, replacing the current SteamEffect during `BeforeTakeDamage` bypassed the initial effect-local guard: the three-case pure-core witness had **1 failure and 2 passing controls**. The source-reference guard then passed all three; a later exception-cleanup control also passed. Its matched 83-case comparison was 82 PASS/1 FAIL → 83 PASS.

Second, independent nested sources correctly dealt 2+2, but a before/after HP delta attributed 4 to the outer source. A four-case diagnostic witness had **1 failure and 3 passing controls**. Root approved a presentation scope divergence: **remove duplicate numeric scald prose and inferred landed effect records**. The canonical void damage API is unchanged. Seven final attempt/reentry cases failed against the previous record/prose contract before the attempted-only repair; they now include same/replaced source, next pulse, exception cleanup, distinct nested sources with an outer veto, cool-source attempt exclusion, ordinary damage and overkill.

Final matched corpus: **270 cases, before225 PASS/45 FAIL → after270 PASS/0 FAIL**, no newly failing cases. The 86 feature cases comprise core30, adversarial29, navigation20 and supplementary reentry7. All184 neighboring material, reaction, scheduling, save, Burning, description, navigation and Rites/ScaldingVeil cases pass. This standalone runner uses core shims: it does not prove native scene behavior, rendered feedback, UI interaction or final player balance. Runtime and current complete EditMode reference compilation have zero errors. Exact XML/logs, frozen first-guard source, final candidate source, diffs and manifest are under `Docs/Verification/DensityCompletion/HotSteam/PrivateCore/`; `evidence.json` enumerates and hashes them.

The source pulse lifetime is deliberately small:

```csharp
if (!SteamContact.TryBeginPulse(target)) return;
try { /* existing cooling/Wet; validated physical contact; typed Heat */ }
finally { /* clear per-effect scratch */ SteamContact.EndPulse(target); }
```

### In-phase self-review (Q1–Q4)

- **Q1 — symmetry:** source admission is shared by damage, live description and prospective navigation. Entry/exit guard cleanup is paired through normal, rejected, exception and replacement callbacks; distinct-source nesting remains independent. Existing anchor cooling/Wet and density decay remain unchanged.
- **Q2 — consistency:** contact revalidation uses existing committed `SpatialQuery` geometry, and canonical `ApplyDamage` retains resistance/veto/death. No temperature is inferred for tile clouds or SteamCloud. Save loading still restores public existing fields; lazy scratch and transient active-source ownership do not change the schema.
- **Q3 — counters:** actual source creation, cool/hot, 99/100 resistance, stale/foreign backlinks, co-occupants/perimeters, body vs anchor, callback invalidation, save reconstruction, exception/reentry and next-pulse independence are paired. The replacement and misattribution findings were actual REDs, not merely hypothetical risks. Final private270 and native270 passed; the matched three-case live gate now also passes below.
- **Q4 — documentation:** the original proposal below is historical. Final behavior uses attempted-only effect diagnostics and canonical landed feedback, not the early numeric scald prose. Exact manifests list four production scripts, a new helper meta, new supplementary fixture/meta and the existing adversarial feedback assertion update. No content/rules/material assets, global combustion units, scheduler or combat API edits are included.

🟡 Fixed before publication: replacement-instance reentry; nested damage overattribution. 🧪 Bounded acceptance: matched hot/cool/noSteam/escape passed; descriptions and hit feedback were inspected, with right-HUD clipping retained as a limitation. Native86+184 execution is GREEN. Existing saves containing hot SteamEffect intentionally gain this newly authored rule; no schema migration is needed or claimed.

## Actual native integration gate

Root published the final supplementary fixture first and executed **native86:45 RED/41 PASS**, job `d2517423afd7435f85670e32cf50b66c`,2.3800525seconds. The preceding refresh-race run discovered only the older79 cases and is retained under its explicit name; it is not claimed as86-case evidence. Root then published the exact five production paths. A scripts-only refresh initially omitted the newly added helper; an all-asset import resolved discovery and the loaded `SteamContact` type was verified before GREEN. This was an import-order issue, not a gameplay assertion failure.

Actual final **native270:270 PASS/0 FAIL/0 SKIP**, job `96e7544da5a8436c953ce220f8e45863`,2.583263seconds. Authoritative receipt: `Docs/Verification/DensityCompletion/HotSteam/native270-green.{json,xml.gz}`. No additional production change followed the final private comparison. The native package uses the accepted steam-scheduling source creation sequence, then actual owner selection/Examine, paid dry-cell contact and escape, with exact source/target damage attribution and matching checkpoint controls. The final live outcome and its explicit limits are recorded below.

## First live source and harness correction

The four-file native route and launcher were published after their two missing-launcher restoration tests failed; the original restoration selection then passed28/28. Native run `a116022957504407afd0d79e2b31207c` created actual reaction-owned SteamEffect at706.2705°C, density0.6999999, with an ordinary40HP player and no player Burning/Wet. It stopped before contact on the compound dry reachable-cell assertion. The source cell had zero tile heat; the original report did not record both measurement cells, so it cannot prove which individual predicate was the first failure. Exact initial report/restoration are retained under `Environment/NativeHotSteam/<runId>`.

Source inspection found that `Cell.BlocksMovement()` includes the solid current Player. The existing `ignoring` argument excludes exactly that owner. Four standalone current-factory/core pairs pass: self blocking versus self ignored, foreign solid retained, empty cell and closed door. The one-file harness delta uses `BlocksMovement(input.PlayerEntity)` and writes both old/new blocker values, heat, complete tile state and occupant identities before checking. All other dry-cell liquid/gas/trap/burning/coating/residue predicates remain unchanged. Reference compilation has zero errors. This establishes the source premise and preserves foreign blockers; it is not a native replay result or proof that no simultaneous condition will fail.

**Q1–Q4 follow-up:** (1) both candidate and current occupied cells use the same existing self-ignore API; (2) other owners and all environmental safety conditions remain authoritative; (3) actual self/foreign/door pairs are retained, while replay remains the live gate; (4) the initial failed run is preserved without rewriting it as successful evidence. This is only a harness selection correction, with no new gameplay production change. Frozen plan, initial/fixed source, patch, four-case XML/log and compiler output are explicitly enumerated in `HotSteam/NativeHarness/evidence.json`.

## Discovered keyboard access gap — bounded follow-up plan

The second native run `499d8a0894774dc3af2dee4b83aa0063` passed the repaired dry-cell checks: the origin was clear, and the current cell contained only Grass and the Player, with zero heat; default movement blocked the Player while the existing self-ignore query did not. It then failed before a source screenshot because `PickTarget:OilSeep` was unavailable. OilSeep, the created WaterPuddle and SteamCloud are all Terrain, so the normal loot-pile predicate is false. The top cloud opens an individual menu without a route to the existing all-owner picker. This hides the underlying source's hazard description from the keyboard path.

Root approved a separate small interaction fix after actual native UI RED: expose exactly one existing “everything here” action when the current cell has multiple valid selectable owners, while retaining the current initial target and loot-pile summary. The existing picker must still re-resolve owner IDs; single owner and invalid-ID controls must not acquire pointless navigation. Look/C, actual hot/cool source Examine, expired cloud, ordinary item+floor, existing loot pile, stale removed owner and return after removal are paired. Menu navigation and examination stay free; no action grants, altered hazards or distant world actions. Private plan: `/tmp/coo-hot-steam-menu/PLAN.md`. Actual native menu10 executed7 expected failures/3 passing controls (job`f70cf2756a4a40cc8ad559c1a99ecc4a`). The minimal conditional entry and matching observed-menu driver then passed current reference compilation and independent peer review. Native menu50 and the matched live replay now pass, as recorded below. The driver records offered menu rows and selected/physical owners and uses the normal entry rather than synthesizing a command. Exact private plan/fixture/diffs/manifests/compiler outputs are archived in `HotSteam/TerrainMenu/evidence.json`.

**Q1–Q4 menu review:** source and ordinary item/terrain stacks share the existing picker; loot summaries and individual descriptions remain distinct; current owner-ID resolution, reach and payment are unchanged; single/invalid-ID, stale removal, no-loop and real hot/cool controls are explicit. The observed warning-access defect is a separate production interaction fix, not an exception hidden in the scenario. Actual native replay now reaches the warning using those offered keyboard entries; no action is synthesized.

## Final live acceptance and checkpoint review

Native menu10 first failed7 cases while3 controls passed. After the conditional existing-picker entry and observed driver navigation, **all50 menu and neighboring tests passed**, job `589a114d5ee447fc87e64ce258944bb4`,1.0469011seconds. Actual native run `10851284a3874055a979dcc7bdd1b391` then completed **30/30 checks,3/3 matched cases,16 captures**,31.0740446seconds,0errors. Its complete raw Unity log range and exact pre/post scene/start-scene/input/background/seed/save restoration are retained alongside the report.

| Current source | Approach | Adjacent wait | Escape | Escaped wait |
|---|---:|---:|---:|---:|
| Actual fire/water-created hot SteamEffect |40→38 |38→36 |36→36 |36→36 |
| Same creation, explicitly cooled source control |40→40 |40→40 |40→40 |40→40 |
| Same creation, explicitly removed SteamEffect control |40→40 |40→40 |40→40 |40→40 |

All12 paid actions cost exactly1000; actual hit windows matched the exact source/player identities, requested-attempt records and canonical typed2 Heat records. Examination was free and preserved live effect density. The positive source's temperature/density/HP were never injected; source prop and book were explicit factory fixtures, travel was a labelled shortcut, and negative source edits were staged controls. These observations do not claim natural supply acquisition, all-save/all-seed balance, generic cloud scalding or a new thermal bridge.

Root and render_completion viewed the hot source examination, hot adjacent-wait and cool examination images. The hot/cool descriptions are visibly distinct, and the hot wait shows red2 feedback plus36HP. The right sidebar clips some text at the screenshot edge: readable warning availability and damage feedback are accepted, full description layout quality is not claimed.

**Final Q1–Q4:** Q1 — shared contact eligibility, exact source reentry exclusion and cleanup are symmetric; cool/noSteam/escape controls do not take extra damage. Q2 — canonical damage/physical occupancy/save lifecycle and normal menu reach/payment remain authoritative. Q3 — native86 RED45/control41→native270 GREEN; menu10 RED7/control3→native50 GREEN; two original native failures remain preserved before matched live acceptance. Q4 — attempted-only effect diagnostics replace unreliable inferred landed prose; original numeric damage scope divergence is explicit, source staging is labelled, and exact owned paths accompany the checkpoint. No new sweep is needed for this completed bounded slice.

**Deferred MEDIUM / balance-sensitive:** the separate64 fresh Combustibility author normalization, Acidic's unchanged absolute.05 behavior, inherited factory closure and old-save policy remain a source-grounded future proposal under `HotSteam/DeferredCombustion/`. No content tokens, bridge, runtime scale heuristic, migration or new saved fields were changed. Right-HUD text fit is a separate presentation limitation, not a reason to loop this completed mechanic harness.

## Approved plan and pre-implementation sweep (historical plan)


Historical status at plan approval: approved bounded hot-SteamEffect slice, private test-first preparation after the completed liquid renderer repair. Three native-reference test fixtures are authored/compiled privately; no new behavior test execution, production/Assets/shared-doc changes, or native editor access yet. Existing steam-scheduling, thermal baseline, and 165 material tests are previous accepted evidence, not evidence for this proposal.

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

## Historical pre-test review status

Read-only source audit completed; no new behavior RED or native execution claimed. Existing Steam scheduling owner confirms their accepted work promised only cooling/wetting/lifetime and requires a separate design/test-first gate for heat damage. Root approved this slice, including physical co-occupancy/perimeter, typed resistance, feedback and shared navigation semantics. Liquid ambient rendering is now complete77/77 native GREEN as a separate checkpoint.


## Authorized first-slice refinement before tests

Root authorized the hot-SteamEffect slice after liquid color repair. At this pre-test checkpoint production remained unwritten. Physical hot-contact includes co-occupants on the source's own physical cells plus the immediate8-neighbor perimeter, excluding the source identity: otherwise stepping onto a non-solid steaming OilSeep would evade the hazard. Existing cooling/wetting is preserved; any physical footprint correction to that older path must have separate explicit counterchecks. No inference of harmfulness for tile-cloud or SteamCloud visuals.

Add live hot/cool SteamEffect wording to EffectDescriber and actual landed scald feedback through the usual message/damage path. The separately spawned SteamCloud lasts3 turns while owner steam can remain longer; its image alone cannot prove ongoing warning/readability. Native acceptance must inspect persistent status readout/feedback and escape, not assume the earlier short-lived puff suffices. Avoid introducing a new aura/art system as an unmeasured prerequisite; if the actual native warning proves unreadable, record it as a failed gate and repair narrowly.


## Performance, callback ownership and diagnostics plan

This is a per-source turn feature, not a per-frame visual or whole-zone scan. Follow `Docs/PERF-FOUNDATION.md` scratch-list pattern: bounded physical source cells plus their nine-cell neighborhoods, deduplicated targets in source-owned scratch storage; pure AI queries use existing struct physical-cell enumeration and no mutable cache or allocations. Preflight eligibility and current occupancy again before each dispatched damage so earlier callbacks may remove/move/cool the source or targets without delivering stale later hits. A per-source active-pulse guard prevents recursive callbacks from multiplying the same source; independent sources and later scheduled pulses remain independent. Private transient scratch/guard fields are not added to save schema.

Use the existing typed `CombatSystem.ApplyDamage` pipeline, which already emits damage attempts, resistance changes, veto/zero outcomes and actual landed damage with source/target identity. Add a bounded `effect/SteamScald` outcome at the new attempted-contact seam (actual landed amount, source temperature/density and outcome), and one `effect/SteamPulseRejected` on invalid source invocation with a named eligibility reason; do not emit fabricated damage success for immune/vetoed contacts or pollute pure AI queries. Feedback describes actual landed scald only. No new diagnostic channel or broad per-cell logs.

Initial private fixtures: HotSteamContactTests (core actual reaction/temperature/resistance/readout), HotSteamNavigationTests (same live source cost, full physical body, pure queries, detour/escape/immune and visual-only controls), HotSteamContactAdversarialTests (existing save-field roundtrip, multi-cell dedup, independent sources/self, callback invalidation, canonical veto/typed attributes, nonfinite density, actual target liveness/type, current committed footprint, same-source recursive pulse and target without Thermal, ScaldingVeil separation). Exact native RED must precede any hot-steam production. Existing scheduling34, liquid/gas/path/thermal and EffectDescriber neighbors remain regression gates; actual native paired route remains mandatory before completion.


### Final source/API recheck before native RED

`SpatialQuery.Distance` and `DistanceToCell` already measure minimum Chebyshev distance over committed physical bodies. Reuse those exact methods for contact revalidation and prospective-cell source distance; do not invent a second anchor/body distance implementation. `Zone.GetOccupiedCells(entity)` uses committed offsets, while its anchor-parameter overload intentionally uses candidate offsets for prospective navigation. The stale raw-footprint pair distinguishes them.

`SaveGraphSerializer.LoadEffect` bypasses constructors with `FormatterServices.GetUninitializedObject` before restoring public fields. Any new private scratch containers must therefore be lazy-initialized; do not rely on a field initializer running after load. The initially proposed effect-local active-pulse guard was later replaced by a transient source-reference guard after the reproduced replacement callback defect. Existing named public Density/Duration and Thermal temperature fields remain the save contract.

Re-read the accepted `DensitySteamNativePlayer.SteamProbe`: it already proves actual OilSeep spell/reaction can stop burning within eight ordinary waits while retaining Steam density>.2 and hot706°C, with no remaining Wet/tileHeat. The proposed native scald route can reuse that reached state before walking into the dry adjacent cell; no added direct extinguish or temperature-setting is needed for the positive route. Only the ambient/noSteam paired controls use explicitly recorded single-field diagnostic changes.
