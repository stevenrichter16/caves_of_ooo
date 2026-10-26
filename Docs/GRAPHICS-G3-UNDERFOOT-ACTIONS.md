# G3 — Underfoot action consistency

Status: implementation and independent source review complete. Final native
regression passed **176/176**, zero skipped. Ordinary earned-save editor play
accepted the fixed underfoot menu, one paid harvest and the depleted-source
counter-check. The current `dce87081d` package predates this fix and still needs
refreshing and verification.

## Named defect and actual evidence

During the fresh standalone journey in seed `200893250`, standing on the
StoneburrPatch at northern `(12,18)` showed the Focus hint `C, .: menu / harvest`,
but that input opened a generic pile menu without Harvest. From adjacent
`(11,18)`, `C` then right exposed Harvest on the same source. The evidence is
`Verification/Live16x24/packaged-glade-east-harvest-underfoot-menu.jpg` and
`Verification/Live16x24/packaged-glade-east-harvest-adjacent-menu.jpg`.

This is a narrow gameplay/readout defect discovered during graphics acceptance,
not a new harvesting mechanic or a change to accepted art.

## Verified premises and bounded design

| Premise | Source verification | Consequence |
| --- | --- | --- |
| Underfoot harvesting is valid. | `HarvestablePart.Validate` accepts physical reach at distance zero or one. | Do not change reach, yield, permissions, or recipes. |
| Focus identifies the real source. | `WorldAffordanceQuery.At` skips the acting owner. | Preserve the truthful underfoot hint. |
| Menu resolution treats the actor as another pile object. | `WorldInteractionSystem.ResolveTarget` and `IsPileCell` include every queryable non-terrain occupant. | Add actor-aware query variants and use them consistently in the input/menu chain. |
| Other owners and terrain must remain accessible. | The existing pile summary and target picker retain exact owner IDs. | Exclude only the actual acting entity from its occupied cell, never all Player-tagged entities; retain genuine piles and terrain rows. |

The actor-less query methods retain their existing semantics. World action
execution, harvest transactions, object identity and stack ownership are not
reimplemented. This is CoO's own UI consistency repair; no Qud implementation
parity is claimed.

The menu's target, pile decision and picker now use the same actor context:

```csharp
WorldInteractionSystem.ResolveTarget(cell, PlayerEntity);
WorldInteractionSystem.IsPileCell(cell, PlayerEntity);
WorldInteractionSystem.BuildTargetPickerActions(cell, PlayerEntity);
```

## Verification plan

1. Run `UnderfootWorldActionTests` before runtime edits. Tests use the actual
   keyboard direction handler and world menu, with authored StoneburrPatch and
   Grass owners. Record native RED without counting a compile failure as a
   behavioral failure.
2. Add minimal actor-aware query overloads and pass the current actor from menu,
   picker, back-row and details-reader rebuilding call sites.
3. Run the dedicated fixture and neighboring pile, return-state, affordance,
   action-reach, physical-footprint and harvesting suites. The dedicated tests
   cover direct underfoot/adjacent menus, actual piles, a distinct Player-tagged
   owner, metadata, terrain access, spent harvests, exact owner execution and
   stale/distant/dead/replaced-owner refusals.
4. Repeat ordinary underfoot and adjacent interaction in the real game, capture
   the menu with Harvest, then perform the harvest and verify the real result.
   Root owns Unity and packaged play. Keyboard simulation tests do not establish
   physical Steam Deck acceptance.

## Implementation and self-review log

- Tests and plan saved before production changes. Root's native
  `Verification/Live16x24/underfoot-actions-red.xml` run recorded **18 cases,
  3 passed, 15 failed, zero skipped**. All failures were expected underfoot
  target/pile routing assertions; the adjacent controls passed.
- Added actor-aware target, pile and picker query overloads while retaining
  actor-less behavior. The input menu, picker, crafting refresh and details
  reader now supply the same current actor. Filtering compares the actual
  entity reference, not a Player tag or ID.
- Updated the two `FoundingRestTests` navigation cases that expected the former
  actor-as-pile indirection. A lone plume now offers Sleep directly; one loose
  item still retains the plume's explicit underfoot shortcut. The real sleep
  execution, clock, healing, narrative meeting and return-state assertions are
  unchanged. This is an intended navigation change, not removal of a safeguard.
- Initial regression `Verification/Live16x24/underfoot-actions-green.xml`:
  **172 cases, 166 passed, 6 failed, zero skipped**. All 18 dedicated underfoot
  cases passed. All six failures were successful-sleep clock assertions in
  `FoundingRestTests`, including four untouched direct-dispatch paths.
- Root temporarily restored the two runtime files and the rest fixture to
  HEAD, then ran `Verification/Live16x24/underfoot-founding-baseline.xml`:
  **16 cases, 10 passed, the same 6 failed, zero skipped**. Candidate files were
  restored before further edits. `WorldResourceActions.ActorCurrent` and its use
  by `RestSystem` require exact Physics ownership since `b3189afc7b` (October 6),
  but the manually built fixture player had no PhysicsPart and last changed in
  `6b3f89392` (September 26). Added the real actor prerequisite to that fixture
  and two missing/foreign Physics refusal counters. No rest authority was
  relaxed and no rest production code changed.
- Added two post-GREEN query counters: the actor-less overloads keep their
  original stack semantics, and a foreign actor with the same ID cannot hide
  the real occupant. The dedicated fixture now has 20 cases.
- Independent source review found no material issue; it requested the explicit
  actor-less comparison now included above. Review covers code semantics, not
  live gameplay or a newly built package.
- Final native `Verification/Live16x24/underfoot-actions-final-green.xml`:
  **176 cases, 176 passed, zero failed, zero skipped**, including all 20 dedicated
  underfoot cases and all 18 rest cases. This re-ran the same ten-fixture group
  with four additional counters; do not add prior totals as disjoint coverage.
  The groups were `UnderfootWorldActionTests`, `InteractKeyPileAndTakeTests`,
  `InteractKeyReturnStateTests`, `WorldInteractionSystemTests`,
  `WorldActionReachTests`, `WorldAffordanceQueryTests`, `WorldAffordanceUiTests`,
  `MultiCellTargetingAdversarialTests`, `FoundingRestTests` and
  `DensityHarvestNativeUiTests`.
- 🔵 Self-review: no action execution or authority path changed; existing
  transaction checks still reject removed, distant, dead or replaced sources.
  Source and native gates passed. The earlier standalone captures show the
  defect before this patch; they are not proof of the fixed package.
- Can verify by tests: routing, actor/target identity, menu rows, source state,
  yield and turn payment. Cannot establish by those tests alone: final visual
  readability, controller feel or the behavior of a refreshed package.
- Files initially owned: `UnderfootWorldActionTests.cs` and metadata, this plan.
  Runtime scope after RED: `WorldInteractionSystem.cs` and minimal
  `InputHandler.cs` query adapters and the described `FoundingRestTests`
  navigation expectations. No harvest parts, inventory transactions,
  blueprints, sprites, saves or graphics settings are changed.

## Ordinary earned-save acceptance

Root loaded the real standalone-earned seed `200893250` checkpoint through
normal `C` Continue in an isolated editor session. This preserved the earned
inventory, equipment and six cached zones. Root then walked from the kitchen
through the normal northern border into the eastern shelter and onto its
StoneburrPatch. No grants or teleports were used. The fixed check concerns the
same source type and interaction rule as the original northern failure, at a
different already-existing source; it is not a before/after of the same owner.

All following evidence is under `Verification/Live16x24/`, with both a PNG
capture and matching JSON receipt for each stem:

| Evidence stem | Actual context | Observed result |
| --- | --- | --- |
| `underfoot-earned-loaded` | Kitchen `Overworld.12.11.0`, `(23,9)`, tick 4290 | Normal Continue resumed the earned game. |
| `underfoot-earned-east-return` | East `Overworld.12.10.0`, `(27,24)`, tick 4430 | Ordinary border travel reached the eastern zone; source `11587` still occupied `(23,17)`. |
| `underfoot-earned-harvest-menu` | East `(23,17)`, tick 4540 | Standing on source `11587`, `C` then `.` directly opened its menu with **Harvest**, Examine and everything here. |
| `underfoot-earned-harvested` | Same cell, tick 4550 | Pressing `h` harvested **one StoneburrSeed**, owner `13790`; source `11587` was removed. The log records the yield and carried weight increased from 47 to 48. |
| `underfoot-earned-depleted-menu` | Same cell, tick 4550 | Reopening `C` then `.` showed the grass's Examine-only menu, with no repeat Harvest. Inventory stayed unchanged and reopening cost no time. |

The JSON receipts keep HP **30** and drams **50** throughout this segment.
All pre-harvest inventory entries and equipment remain intact after the one
new seed is added. Every receipt reports `flat: true`; all five actual
1280×800 captures were independently viewed. The menu is readable and names
the actionable source before harvesting, then the remaining grass afterward.

**Can verify:** actual keyboard menu routing in the editor, real border travel,
loaded earned state, source/product identity, one ten-tick action, unchanged HP
and drams, no repeat action after depletion, and the visible flat-game menu.
**Cannot verify from this evidence:** the fix in the existing Mac/Linux package,
physical Steam Deck controls, or any wider six-zone graphics release. The
current package source is `dce87081d` and does **not** contain this change.
