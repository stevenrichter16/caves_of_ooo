# FLOW5 — direct Separate one

Status: COMPLETE. Baseline8766tests, HEADe4acbd8c; final8834/8834GREEN.
Accepted smoothing step6; CoO-original usability feature. Source sweep and plan are
in Docs/MECHANICS-FLOW-SMOOTHING-2026-09-05.md.

## Behavior

A carried stack now offers **Separate one**. It leaves the remainder in place and
opens the new singleton's item menu, focusing its exact row regardless of ID sorting.
The singleton can be upgraded directly with existing Sharp rules. No equip/unequip
detour, station requirement, new recipe, world-turn cost or weight increase.

The command validates actual carried membership, Physics ownership and both equipment
representations. It claims the source before clone initialization and revalidates
before mutation. It prepares the clone before decrementing, retains the source's
crafting mark and removes the clone's copied mark. Snapshot/undo enrollment precede
quantity/owner/list writes. The no-merge append is private to this command; normal
later acquisitions still merge compatible items. Final carried mass must match,
including already-overweight packs. Existing arbitrary clone-payload/deep-copy
contracts and general event rollback are not expanded.

Success returns the exact new Entity and focuses it through the existing popup seam.
Refusal keeps the old popup, item, cursor and visible reason for retry. Commands are
per-attempt; callers read the recipient only with a successful corresponding result.
Validation is a query and does not erase a previously committed result; rollback of
that execution clears its recipient. This contract corrected an initial doc overclaim.

## Raw gates

MCP restarted/settled before every Unity run; compiler errors checked before fresh XML.

| Archive | Total/pass/fail | UTC / duration | Meaning |
|---|---|---|---|
|FLOW5-red.xml.gz|29/2/27|05:43:28–05:43:29;1.6268074s|Command/menu absent; singleton/zero-row controls already pass |
|FLOW5-minimum.xml.gz|95/95/0|05:45:58–05:46:03;4.7816438s|Initial implementation plus feedback/identity neighbors |
|FLOW5-adversarial.xml.gz|81/81/0|05:48:51–05:48:54;3.2006368s|30dedicated cases plus transfer neighbors |
|FLOW5-staging-red.xml.gz|6/0/6|05:51:22;.2421046s|Scenario absent before authoring |

All listed runs0compilererrors. Dedicated matrix now33cases after query-preservation
and clone-weight success/refusal controls. Initial tests29; staging6; total new68.

## Review and attempted-break gate

Independent taxonomy/CoO-contract source review found0production must-fix. Ten
hypotheses probe clone failure/revalidation, same-transaction nesting, late rollback,
independent completed work, participant protection during publication, repeated
separation, mark ownership, saved payload/remerge, sort-dependent focus and timing.

Preparation callbacks that remove/empty the source retain their own changes after
refusal. Same transaction source2: inner separates and outer refuses; committing
retains inner work, rolling back restores source. Source3: both separate and reverse
rollback restores exact order/count. Independent Torch drops survive a failed split;
nested independent attempts on source/new singleton refuse while claims are active.

Additional counterchecks retain detached recipient references and assert owner fields
clear after rollback. A clone-only weight increase exercises the new mass-refusal
branch with source/list/handling restored. Paid Sharp applies to the new singleton
only, consumes BC once, and ordinary later insertion distinguishes modified payloads.

## Native scenario and honesty bounds

Scenario uses existing Dagger/StoneFloor art, actual Sharp recipe and BC bits. It
starts with Dagger3 and empty equipment. The driver uses queued keyboard input for
inventory→Separate one→Mods→Sharp→exact new singleton. Reflection observes only.
It checks exact new popup/row, original2/new1, IDs, ownership, unchanged mass/time,
source ineligibility, actual +1PenBonus/ModificationCount1 and one BC payment.

Can verify: those script-observable actions, quantities, payloads, menu references,
turn/energy values and test-covered refusal/rollback/save invariants.
Cannot verify: rendered pixels, subjective smoothness, desktop mouse ingestion, all possible third-party
Part payloads or general callback atomicity. No performance-speedup claim. This adds
an on-demand popup action and sparse inventory command, not a per-frame rendering
loop change; no new hot-path performance comparison is claimed.

Launcher uses a fresh disposable GUID marker slot, preserves prior preference,
never loads that marker, unregisters synthetic runtime and deletes only its own slot.
Existing A31destroyed-camera shutdown debt may occur and must be reported from raw
log separately from gameplay checks. Final native/full/GUID/ownership gates recorded below.

## Files

New SeparateOneCommand, two InventoryUI branches, regression/UI/adversarial/staging
tests, GameAuditSeparateOneBench/Player/Batch, this report, smoothing/audit/daily docs
and raw evidence. No blueprint JSON, art, saved fields or new persisted Part.

## Final focused/native evidence

Expanded134/134GREEN,05:53:59–05:54:06UTC,6.9690878s,0compilererrors.
Native2a08cb6be3d34dfaaf00d6858758bbb6: **16/16PASS**,5.014162292s,
exit0,0compilererrors. FLOW5-native.json/log.gz retained. Exact separation/focus,
sole valid Sharp target, one BC payment, unchanged remainder/mass/time all passed.

Independent native/staging review found one 🟡 test cleanup issue: ScenarioTestHarness
creates a TurnManager but does not restore Active. The new staging fixture now
captures/restores it; full suite includes that correction. Runtime/native behavior
unchanged. No further native source blocker identified.

2401uniqueGUIDs,0collisions. Raw native log contains2known A31destroyed-camera
exceptions during shutdown after the successful report, from the existing protected
ZoneRenderer/WorldFxCoordinator teardown. Exit0 is not a clean-shutdown claim.

## Close-out

**8834/8834GREEN**,05:56:35–05:58:42UTC,126.1659211s,0compilererrors,
0failed/skipped/inconclusive.68newcases:29regression,33dedicated adversarial,
6staging. Native16PASS;2401uniqueGUIDs/0collisions. Final source/native review
complete; staging Active-state cleanup fixed before full run. No remaining FLOW5
must-fix. Known A31shutdown errors and visual/mouse/performance bounds remain explicit.

Self-review: 🟡 missing direct split/focus flow implemented after RED; staging
static-state leak fixed. 🔵 per-attempt result contract documented and query pin added;
no general no-merge insertion API introduced. ⚪ ordinary future merges remain
intentional; arbitrary payload cloning/A31are separate. 🧪 no subjective feel claim.

Ownership gate:29owned whole paths,0overlap against1027protected paths.
No protected spell/art file staged; no InventoryPart change.
