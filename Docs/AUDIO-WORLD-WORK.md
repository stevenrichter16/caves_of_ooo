# Iteration 3: committed work feedback

Iteration 2 passed its fresh actual-input live gate (26/26). Iteration 3 tests were imported first; native compiler RED was observed in `Docs/Verification/Atmosphere/iteration3-compile-red.json` (six missing work receipt/enum errors; assertions did not run). Production is implemented. Initial native38/38 GREEN is retained in `Docs/Verification/Atmosphere/iteration3-initial-green.xml`; two added resource/seed-only cases and this iteration's fresh full live gate remain pending. The new action IDs selected by root are WorkForge, WorkRepairWood, WorkRepairStone, WorkRepairMetal and HarvestPlant; Fiber/Leather use the existing Use handling recording. The assets owner has prepared paired recordings. Kitchen work is deliberately deferred: it needs its own pure validated job authority, new longer ordinary route and a separate live gate.

## Intended player result

Repairing a clay well, bracing a wooden gate, replacing rope, harvesting a ripe crop and forging at a real forge produce a short corresponding sound when the actual action completes. Looking at a menu, lacking supplies, aborting or loading completed work is silent. Merely standing beside a forge never starts a work loop. A single weapon assembled away from a forge remains a valid gameplay action but has no anvil sound.

## Implemented API and boundaries

- New Core readonly `WorldAudioWorkReceipt`: Actor, Source, Zone, Point Contact, WorldAudioCue Cue, bool SourceConsumed. Readonly copied values; exact source/actor references, never names/IDs alone.
- New Core `WorldAudioWork.Callback: Action<WorldAudioWorkReceipt>` is presentation-only and exception-contained, like combat audio. No new simulation GameEvent, RNG or serialized audio state.
- Internal capture token records exact actor cell, source cell, source Physics/Render ownership and visible/explored contact before callbacks can change them. It also captures the actual source part/recipe kind needed by the specific action. A capture miss suppresses sound, never rejects gameplay.
- Internal publish runs only from an existing InventoryTransaction.AfterCommit callback. Revalidate actor's exact current live zone/position, source/cell/part ownership, source visibility and expected committed result. Detached or replaced graph owners cannot replay work.
- Normal sources remain exact current ground owners. Portable repair intentionally uses the actor's cell but retains its carried exact item and RepairablePart identity. Crop removal is a narrowly typed captured cultivated-harvest result: the original crop part/owner/coordinates were current before this operation and that operation removed it; source must remain removed/unowned at publish. No arbitrary disappeared-owner fallback.
- Router adds/removes this subscription, uses the existing fixed SFX pool and never scans the world. It may accept the current player's explicit committed action while their own menu temporarily hides rendering, with captured visible contact and exact current actor graph. It must not treat this exception as permission for remote/NPC work. The coordinator explicitly approved this local-menu policy.

The production helper should not expose a public forge-result emitter that arbitrary visual hooks call. Tests exercise real actions/transactions, not manufactured successful work receipts. Capture/receipt are not gameplay permissions or proof that an entire third-party callback sequence is immutable.

## Exact production hook locations

1. `RepairablePart.Apply`: capture before material/payment callbacks; after all repair changes succeed, publish in existing `tx.AfterCommit` at line155. Map immutable recipe.Composition: Wood→WorkRepairWood, Masonry→WorkRepairStone, Metal→WorkRepairMetal, Fiber/Leather→Use. Unknown composition: fail quiet. Preserve recipe/repair semantics and all existing paid command behavior.
2. `FieldHarvestService.TryHarvest`: capture before factory/output callbacks; publish once in existing AfterCommit at line55, only after the exact row's successful harvested/stubble result. YieldCount does not multiply sounds. `ConsumeByGrazer` has no player work hook.
3. `CropYieldService.TryRelease`: capture only `actor != null` explicit standing-crop harvest before staging/removal. Publish once in AfterCommit at line104. Actor-null automatic maturation is silent. `gather` and ordinary harvest share this one hook. Use the exact selected source coordinates because successful cultivation consumes the crop.
4. `ForgeWeaponCommand.Execute`: find a sound-only exact nearest visible current forge witness before calling TryForgeBatch (bounded nine cells; deterministic coordinate/order ties). Capture its owned ForgePart/Physics/Render, undestroyed state, actor position and contact. On successful `madeCount > 0`, register one AfterCommit receipt; recheck exact same witness then. No hook in WeaponForgingService, whose service-only public callers have no zone and whose existing diagnostic precedes parent commit. No generic hook in ForgePart menu dispatch, which could duplicate command completion.
5. WorldAudioEventRouter typed subscription and five enum appends. New recordings stay catalog-owned by root/assets agent. No fixed-size cue arrays, loop pool expansion, passive source discovery, general crafting changes or world scans.

## Fixtures and native evidence

`WorldAudioWorkTests.cs` and `WorldAudioWorkAdversarialTests.cs` are in `Assets/Tests/EditMode/Presentation/Rendering`, each with a fresh32hex meta. They use the existing FiftyWorldFixture to create real authored repair/crop/forge content, existing InventoryCommandExecutor/PerformInventoryActionCommand paths and actual transaction staging. Synthetic distinguishable audio clips prove accepted routing without assuming EditMode playback hardware.

Coverage:

- Three authored physical repairs plus Metal/Wood/Leather portable repair, exact supply consumption and selected cue/contact.
- Stage→Commit vs Stage→Rollback; no sound before commit or on a second commit.
- Finite row harvest with 1 vs3 yield units emits once, spent repeat silent.
- Actual cultivated Harvest/Gather emits once after source consumption, produces genuine yields; automatic maturity is silent.
- Actual one-unit/batch forge emits once at exact forge; legitimate pack assembly is silent.
- Late AfterInventoryAction failure rolls back repair/row/crop without sound.
- Hidden/unexplored/source-hidden contact, moved actor, wrong loaded graph, mute and disposed router refuse routing; restored valid supply is a positive counter.
- Destroyed/portable/hidden/removed-after-staging forge does not produce anvil feedback even when the existing one-unit gameplay action remains legal.
- Player action-menu hidden-world completion is intentionally distinct from passive source discovery.
- Grazer consumption does not impersonate hand harvest; an audio observer exception cannot refund a paid repair.
- The exact cultivated crop reinserted after staging or a newly hidden captured cell cannot borrow the consumed-owner exception; committed yields remain intact.

Independent source review found no significant gameplay premise error and requested two consumed-owner adversaries. The initial38 cases (16 core,22 adversarial) passed native Unity. The final proposed40 cases add one real-resource test that verifies all five cues have distinct paired short mono imported recordings, plus the actual seed-only harvest path (three seeds, zero produce, one consumed-source sound). These two await the final native run.

Native compile follow-up corrected two fixture access premises: `FieldHarvestPart.ConsumeByGrazer` and `CropYieldService` are internal across the test assembly. The tests now drive public `ReedbackGrazer` configuration plus `TakeTurn`, and `CropSystem.OnTickEnd` at the actual wet maturation boundary. No production visibility or gameplay permission was changed. The first attempted run returned stale assembly/zero tests and is not GREEN.

Native RED was compile RED for the missing receipt/enum API, not38 executed assertion failures. Independent production review found no material regression: all hooks remain after commit, automatic growth stays silent, and source identity/removal branches do not change payment or gameplay permission. `git diff --check` is clean. No claim of subjective listening or live success is made before the coordinator runs the gate.

## Actual live gate immediately after this iteration

The final implementation retains the entire existing real generated field route in `ReferenceGladeNativeRepairCultivation.cs`: acquire its actual clay/timber/cord, perform all three actual repairs, harvest its original ripe crop, plant the returned seed, cast actual rain and spend ordinary waits through growth, harvest again, then use F5/F6. The old route's initial disclosed travel shortcut reaches the unchanged generated field; it does not grant supplies or fix crop stages. Any retained shortcut must be disclosed in the new report and approved by root's route contract. No additional placement, inventory grant, instant repair or direct hook invocation.

Plan adjustment requested by the coordinator: keep the full old route and its20 gameplay checks instead of shortening to the first crop. Eight added checks cover five actual work actions, fixed16-source pool, no work replay on graph replacement and restored SFX preference. Including ordinary boot, expected total is29. The audit correlates exact typed receipt source/actor/cue/contact with the imported Action voice and AcceptedSoundCount. A bounded active-observation window samples that source during playback because the existing key helper may wait longer than the short clip. It records per-source peak/count, not continuous listener audio or a listening claim. Screenshots remain ordinary visual evidence. Only in-memory SFX volume is initialized; saved preferences are untouched. Iterator finally and the main Cleanup hook both restore and unsubscribe, including abort.

Required live observations: WorkRepairStone at the lined well, WorkRepairWood at the gate, Use at the rope well, HarvestPlant at both original and grown crop; no work replay after load and no source growth beyond16. Mute and already-spent-work refusal are native unit counterchecks, not claimed as extra actions in this live route. The existing field has no guaranteed forge. Either use the existing authored specialist tempering-shelter route with real carried kit for a second bounded live subroute, or state honestly that forging has native command coverage but no new live forge proof. Do not silently count crop/repair Play as a forge live check.

Save this prompt before release:

> Iteration 3: add one-shot committed repair, explicit field/cultivated harvest and actual visible-forge action sounds with the five approved paired work cues; Fiber/Leather use existing handling. Work must follow real transaction completion, preserve gameplay/payment and reject rollback, stale/hidden sources, automatic crop maturity and idle forge presence. Execute native RED before production, counterchecks and focused GREEN, then run a fresh actual-input generated-field route with three material repairs and original ripe-crop harvest immediately after this iteration. Record actual accepted imported audio and signal with honest forge/route limits. Do not begin iteration 4 until this live gate completes or reports a real blocker.

## Files and scope review

- New `Assets/Scripts/Gameplay/Effects/WorldAudioWork.cs` and meta: copied committed-work receipts and exact current source checks.
- `WorldAudioHooks.cs`: five appended cue identities; no changed numeric values for existing cues.
- `RepairablePart.cs`, `FieldHarvestService.cs`, `CropYieldService.cs`, `ForgeWeaponCommand.cs`: sound capture before callbacks; publication only in existing completion transactions.
- `WorldAudioEventRouter.cs`: additive/disposable typed work listener; generic semantic hooks cannot forge new work cues.
- Two new work test files/metas:38 native GREEN initially,40 final authored cases after real catalog and seed-only additions.
- `ReferenceGladeNativeRepairCultivation.cs`: existing full route plus measured work playback. `ReferenceGladeNativePlayer.cs`: only the approved cleanup call.

Self-review: no gameplay timings, materials, crop maturity, RNG or save schema changed. Forge discovery is a bounded3×3 scan per actual craft, never an idle loop. The work callback does not fabricate NPC trades, kitchen shifts or forge hammering when no action occurred. Scope intentionally omits passive kitchen work, automatic maturity Foley, quarry/corpse harvesting and portable-repair live proof. The five new pairs are catalogued prepared recordings; their binding is tested without implying literal recordings of the fictional action.

Independent installed-scenario review: no material timing, cleanup or count issue found. The reviewer confirmed that the separate monitor sees short imported work voices during existing key/menu waits, the exact receipt/accepted-count correlation avoids attributing unrelated PCM to work, all20 prior gameplay checks remain, and cleanup covers setup failure plus abort. Forge/portable live coverage and subjective sound quality remain explicitly outside this route.

## Coordinator final verification

Native affected regression1766/1766 passed, including final40 work cases. Failed full cultivation run f201d1698f424750aa25c9d3d22e21ff and failed work approach e3af2a66304d412982659442eea6fd43 are retained. Both encountered actual rotling danger; no production farming/AI changes were made. The distinct work route permits up to two original starting tonics through the existing ordinary inventory helper, and finishes before planting/growth.

Fresh work-only run f960c8915203426084b29babd8723a55 completed21/21 with zero errors in23.16s. Three material repairs, original crop harvest and real checkpoint passed with exact imported audio observations and sampled per-source signal. Both starting tonic units were consumed and recorded. Native growth, forge and portable-equipment playback are not claimed from this route. No source change since the broad test other than the explicitly scoped audit route/tonic support.
