# GA03e / A09 hauling lifecycle — independent cold-eye review

Reviewed 2026-09-06 against the applied implementation, including the final
`DragFollowActor` event marker. Repository: `/Users/steven/caves-of-ooo`.
This reviewer performed source inspection only: no Unity, tests, Assets edits,
native reruns, or production changes. Reported run results below belong to root.

## Verdict and resolved observations

No further state-loss, invalid-follow, or reciprocal-identity blocker was found in
the bounded lifecycle implementation. The initial callback replacement-grip defect
was real, independently agreed, reproduced RED by root, and addressed locally.
The two late reporting/contract observations below are now resolved in source.
No outstanding must-fix finding remains from this bounded review. The expanded
focused/native/full execution results remain root's verification responsibility.

1. **Resolved: saved-log tick mismatch during stale-link repair.**
   `GameSessionState.Load` calls `RebuildLoadedWorld` at
   `Assets/Scripts/Gameplay/Save/SaveSystem.cs:416`, before `LoadSlot` invokes the
   bootstrap apply callback at line 670. `Slip` appends a message at
   `Assets/Scripts/Gameplay/World/DragSystem.cs:420`. The actual bootstrap
   `MessageLog.TickProvider` captures its `_turnManager`
   (`Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs:364`, `:923`), which
   is not assigned the loaded manager until `ApplyLoadedGame` at line 848.
   Activating `TurnManager.Active` does not change that closure. Loading a saved
   stale grip at tick 17 while the discarded runtime was at 222 could therefore
   stamp the repair message 222. This did not advance the loaded clock, change
   hauling state, or break current sidebar fading (which uses message serials).
   It affected persisted log metadata. Root executed the bounded RED: 31/33
   passed, with both incorrect ticks (expected 17 or 0, observed 222) failing.
   The final SaveSystem lines 964–984 capture the exact previous provider, use
   the candidate state's tick (or zero for a missing clock) only around the
   hauling cleanup loop, and restore the exact delegate in `finally`.
   Index rebuild, entity snapshot construction, body parsing and two-phase
   load hooks are outside this temporary context. No bootstrap callback,
   save format, historical entry timestamp, movement path, or global clock
   publication rule was changed.

   Inspected all four added cases at
   `GameAuditHaulingAdversarialTests.cs:78–96`: healthy history retains its
   original tick and adds no message; broken history retains that tick while
   the newly appended repair entry uses 17; a missing saved clock stamps zero;
   and a deliberately throwing repair-message observer still restores the exact
   old delegate. The inherited fixture snapshots/restores TickProvider.
   These are meaningful control/exception cases rather than assertions of a
   replacement provider's equivalent value. Nested synchronous rebuilds also
   restore their outer provider by the ordinary `try/finally` stack, although
   this review does not claim an executed nested-clock test.

   The native post driver now issues real Period after stale F5 and asserts
   the discarded clock actually advances while the load stays absent. F6
   must recover the older tick and produce both the repair and `Game loaded.`
   entries at that tick. This probe occurs after all three measured phases,
   so it does not change the before/after 75-second performance workload.
   Source review clears the probe's nonvacuity; no native execution claimed here.

2. **Resolved: document the intentionally broadened `Released` meaning.**
   `DetachRemovedEntity` calls `Release` at DragSystem lines 343/346. The new
   `CleanupDiagnosticDistinguishesRemovalFromInvalidMovement` test explicitly
   expects removal to emit `Released`, while invalid movement emits `Slipped`.
   `Docs/DRAG-AND-HAUL.md:534`, `:572` and the appended A09 integration section
   now explicitly describe `Released` as release/removal/death and `Slipped`
   as validation/follow failure. The text rejects a voluntary-intent inference
   from kind alone, matching the actual consumers and tests. This closes the
   diagnostic-contract wording finding; it was not a refund or membership bug.

## Scope inspected and cleared

- `CLAUDE.md`, `Docs/HAULING-LIFECYCLE-PLAN.md`, relevant original
  `Docs/DRAG-AND-HAUL.md` semantics; `/tmp/codex_ga03e_impl.py` versus actual
  DragSystem/Zone/Save diffs. Protected spell presentation differences in the
  same files were excluded from attribution.
- `DragPart.HandleEvent`, `ValidateLink`, `ValidateEntity`, `DetachRemovedEntity`,
  `FollowInto`, `Slip`, `Release`, and `TryGrab`. Capture/recheck of the exact
  inverse Part avoids removing a callback-created replacement. Reciprocal
  checks preserve a named actor's different valid load. Stored `AppliedPenalty`
  is refunded once and cleared rather than recomputed from current weight.
- `Zone.RemoveEntity` invokes cleanup after successful removal and index/tag
  updates. Failed, repeated, and wrong-zone removals do not detach. Ordinary
  `Zone.MoveEntity` remains distinct, retaining hauling through valid movement.
- `Gone` detects committed prop destruction even before `Destroyed` callbacks.
  Structural HP zero with a vetoed destruction remains valid. Creature death
  uses base HP rather than modified display HP; missing HP is not treated as
  death. These match the inspected producers and counterchecks.
- Full-session finalization runs after the footer and both entity-load hook
  phases; all cached-zone indexes are rebuilt before any link validation.
  The reader's completed-body list reaches wholly unplaced referenced entities,
  including nonplayer pairs. This is complete for graphs emitted by the shipped
  writer. Standalone token-graph defaults remain unchanged. Manual rebuild
  snapshots placed entities plus the player; its narrower scope is documented.
- Cleanup uses a stable entity snapshot. Zone enumeration used to locate an
  entity finishes before message callbacks can run; this avoids retaining the
  cached-zone enumerator across cleanup callbacks. No all-zone scan was added
  to ordinary movement. The event marker uses the pooled event object store;
  no new per-move collection is constructed (initial dictionary growth is not
  a guarantee of zero allocation).
- `GameAuditHaulingLifecycleTests` and `GameAuditHaulingAdversarialTests` cover
  actual HaulBarrel content, direct removal, voluntary/forced/diagonal following,
  destruction-time observations, veto controls, horizontal/vertical transfer,
  stored/clamped refunds, wrong-zone and stale reciprocal ownership, saved
  null/cross-zone/unplaced/Gone/dead links, repeated rebuild, exact replacement
  Parts, and distinct nested movement. Existing decode-isolation checks remain
  in root's final focused set. Fixture restores message/static hooks and its
  inherited save-session state; no additional fixture leak was found here.

## Tested callback hypothesis and repair

`Entity.FireEvent` reanchors its index when a handler removes itself. An invalid
old `DragPart` could emit a slip message, whose callback grabbed another load;
the dispatcher then reached the newly appended `DragPart` with the same old
`AfterMove`. Checking only the old Part identity did not stop that second
invocation. Root reported 53/54 passing with the replacement case RED: the new
load moved from (4,6) to (5,5) even though it was grabbed after arrival at (4,5).

The final marker at DragSystem lines 62–63 is set before validation. A replacement
Part for that actor skips the already-used event. Genuine nested `ForceMoveTo`
creates a different pooled event, so it may follow normally; its explicit
countercheck asserts actor (3,5), new load (4,5), valid grip and Speed 70.
`GameEvent.Release` clears the object dictionary, so later real movement is not
suppressed by pooled marker residue. No generic event-dispatch or MovementSystem
rewrite is needed for this cleanup-induced case.

An earlier non-drag listener establishing a grip during an `AfterMove` before
any drag handler has run is broader original event-order behavior. This marker
does not capture the grip at movement start and must not be described as fixing
arbitrary callback ordering. Production `MessageLog.OnMessage` currently logs
to Unity; the mutation callbacks in these tests are deliberate adversarial seams.

## Evidence and honesty bounds

Root's reported initial lifecycle RED: 26 cases, 8 pass / 18 fail, zero compiler
errors. Reported final focused result after the marker and nested countercheck:
163/163 pass, including 55 new hauling cases and save-decode neighbors. This
review does not independently certify those executions or a future full/native
result.

The valid native baseline reported 41 passing observations over 84.34 seconds,
75.2867 measured seconds, 76,774 frames, 137 discrete and 178 held steps
(89 repeats), zero step failures, complete marker samples, and both actual
stale-save and ordinary-removal resurrection. Baseline pass means the known bug
was reproduced, not repaired. Earlier launch/capacity failures remain archived;
they are not performance evidence.

Native HaulBarrel is the actual yellow `0` glyph content and currently has no
matching barrel sprite. No new art/content, animation feel, pixel coverage,
zero-GC result, or performance improvement is established by this review.
Post-fix native and full-suite verification remain root's responsibility.

The temporary native generator was corrected to the root-selected editor GUID,
all three GUIDs were statically verified distinct 32-character lowercase hex,
and validation now precedes every generated write. Its sample capacity is
200,000. Only the `/tmp` generator was updated; it was not reapplied to Assets.
