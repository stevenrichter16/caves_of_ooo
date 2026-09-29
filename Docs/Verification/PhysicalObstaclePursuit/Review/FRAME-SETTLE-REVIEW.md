# Controlled-arena visual precondition — bounded source diagnosis

Run `0599104303d5454d8aa7ef48644ef2f6` completed with zero paid inputs and failed the combined HaulVisual current-owner/visible/submitted condition. The retained report proves the original-player transfer and native registration check passed. It does not separately record the four failed-condition operands, so it does not prove which one was false. The individually viewed `99-failed-current-state.png` shows the staged hedge boundary, beam, player, ordinary pursuer and Haul hint; this later failure frame does not prove the earlier model assertion was true.

## Source-supported ordering

- `QuestFreeSpreadStateNativePlayer.cs:59–60`: Capture00 completes at EndOfFrame, then the manual RunSafely nested-enumerator pump immediately enters PhysicalPursuit.
- `QuestFreeSpreadStateNativePlayer.PhysicalPursuit.cs:55–66`: the setup transition executes, Settled runs, then HaulVisual is queried.
- `QuestFreeSpreadStateNativePlayer.cs:266`: Settled waits only for Normal/no blocking FX, followed by one `yield null`. That does not guarantee a LateUpdate after a transition initiated at EndOfFrame.
- `InputHandler.cs:914–939`: native transition attaches the current zone and registers NPCs, then calls ZoneRenderer.SetZone. No simulation turn is granted by setup.
- `ZoneRenderer.cs:595–621`: SetZone binds presentation before fresh FOV and sets `_fullDirty=true`.
- `ZoneRenderer.cs:934–939,1010–1029`: normal LateUpdate consumes `_fullDirty`; RenderZone computes FOV and lighting before RefreshSpawnRingPresentation.
- `SpawnRing3DPresenter.cs:509–516`: submitted-owner proof requires authored presentation and known/visible current view state. Before the scheduled FOV/refresh, that proof may correctly refuse.
- `QuestFreeSpreadStateNativePlayer.cs:269,274`: both ordinary Capture and failure capture wait until EndOfFrame. Therefore the later failure picture can contain the correct scene although the earlier precondition ran before its first complete presentation pass.

The current-world authority is not missing: ZoneManager.SetActiveZone(Zone) installs the exact arena and calls OverworldZoneManager.OnZoneAttached, which attaches WorldLocationContext/AreaCompositionScope. SpreadPresentationScope checks that exact cached source and current Spread biome. No source evidence justifies changing those guards or renderer production.

## Narrow recommendation

After existing Settled, record scalar diagnostics, then `yield return Capture("00-controlled-arena-ready")`, then record the same scalar diagnostics before existing transfer/HaulVisual checks. The read-only capture provides a normal render boundary and a useful setup frame; it adds no paid input or explicit FOV/dirty/Refresh call. Preserve every existing current-owner, visual, physics, NPC, clock and energy assertion.

Diagnostics should contain frame number, current player tick/energy, exact current entity/cell membership, cell visibility/exploration, Render.Visible, presenter/current-zone identity, IsReady, PresentationVisible, IsRenderedEntity and Failure string. Never serialize raw Unity structs or objects. The expected result is unchanged tick/energy and false-to-true visibility/submission where scheduling was the cause. If the same predicate remains false after that frame, stop and inspect its exact diagnostic rather than broadening the observer or forcing rendering.

Root subsequently adopted the bounded one-frame Capture and pre-render scalar diagnostic while preserving the actual preimage/diff under integration/frame-settle. Its next baseline attempt is running. This review does not claim that follow-up result or a proven repaired pursuit. No shared files, gameplay, renderer, Unity or Git changes were made. Root confirmed clean teardown separately. Raw failed run remains a visibility-timing observer failure until the bounded follow-up confirms its cause; it supplies no pursuit result.
