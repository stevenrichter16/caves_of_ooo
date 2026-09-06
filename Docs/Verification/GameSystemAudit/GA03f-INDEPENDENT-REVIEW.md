# GA03f final cold-eye pass — detach-first, transient scopes and batch claims

Review date: 2026-09-06. Repository `/Users/steven/caves-of-ooo`, baseline
`39e11374`. Reviewed current Body.cs and InventoryTransaction.cs diffs, both
GameAuditEquipment test files, supporting inventory/planner/anatomy APIs, the
phase plan, and the cited local Qud implementations. No Assets edits, tests,
Unity/native execution, or unrelated protected changes by this reviewer.

## Verdict at this snapshot

The three initial callback findings, nested public DropAll defect, and normal
Unequip of a pending detached item are addressed in source after root-confirmed
REDs. Guard ownership/finally, the fresh target-membership rule, explicit and
unsupported detachment order, and the equal-item fixture agree with the bounded
contract. No outstanding must-fix or new concrete regression was found in this
final source pass. Final focused/full/native execution remains root-owned and
must complete before the feature's verification claim is closed.

## Confirmed repairs and source evidence

- Explicit Dismember accepts BeforeDismember, rechecks the parent, detaches and
  records the limb, then calls UnequipSubtree. A same-limb AfterUnequip reentry
  therefore sees ParentPart null and cannot sever it twice. BeforeDismember's
  own old generic callback behavior is not claimed transactional.
- New gear cannot occupy the doomed hand through a fresh Equip command:
  `TargetPartCompatibilityRule:30–46` checks exact attached-tree membership.
  Keeping ParentBody for later regeneration does not make that hand eligible.
  The surviving-hand positive control uses a different carried real Dagger.
- `ForceUnequip` explicitly adds the detached subtree's nodes to attached nodes
  before looking for and clearing exact `_Equipped` aliases. This preserves
  body-only/missing-cache cleanup and clears an ordinary multislot item from
  both severed and surviving hands. It still removes only matching cache entries.
- `InventoryTransaction.GuardForcedCleanup` creates a claim only if the item has
  no owner. It returns null for an already-held claim, so forced injury is not
  vetoed and cannot release another transaction's ownership. The new guard is
  committed in finally. It has no payment deltas or undo actions; this commit is
  claim release, not an assertion that a throwing observer completed cleanup.
- The claim remains held through destination placement, fall-message callback,
  AfterUnequip and enhancement OnUnequipped. A separate normal Equip on that
  exact item is refused, while independent items may still move. This matches
  the existing normal Unequip item-claim contract without changing raw storage
  APIs or claiming raw/API callbacks obey those command barriers.
- `CheckUnsupportedPartLoss` mirrors detach/register-before-cleanup. It retains
  its deliberate no-zone carried destination. The new unsupported-hand test
  asserts parent detachment and record count, rejects refilling that exact hand,
  preserves the attached arm, and checks exact carried items and Agility.

## Resolved: nested DropAll left detached aliases

In a removable subtree containing two separately command-equipped real items A
and B, A's AfterUnequip callback can invoke public DropAllEquipment. B still has
an equipped cache key. Nested ForceUnequip(B, zone) has no explicit detached
subtree argument and searches only the currently attached root. It clears B's
cache and reverses its bonus but leaves its severed-slot alias. The outer
UnequipSubtree subsequently reaches B, sees that stale alias and processes it
again. The original nested DropAll test starts from a root DropAll and does not
cover this ordering.

Root added `NestedDropDuringInjuryCleansEveryDetachedSlotOnlyOnce(bool nested, bool extrinsic)`:
explicit API fixture reparents the right Hand beneath the left Arm; two real
Duelist Cut Bucklers are command-equipped; positive Agility is 18. Expected end
state is Agility 14, exactly two events, both captured hand slots empty, and both
exact items on the ground. The no-nested row is its countercheck. This is a
meaningful body/API callback test, not a claimed ordinary shipped anatomy.

Root reported 32 dedicated cases: 30 pass, two failures, one each for regular and
extrinsic nested rows (three events instead of two); both no-nested controls
passed. The applied fix adds private transient `_equipmentCleanupSubtrees`.
UnequipSubtree registers a nonempty removal scope before callbacks and removes
the last scope in finally. All callers are synchronous and the field has no
external mutation API, so nested scopes unwind in order and retain outer roots.
Both DropAll's item union and ForceUnequip's exact slot clearing include every
active root, covering extrinsic limbs without relying on regeneration records.
Duplicate tree paths are harmless here: items are deduplicated, and clearing an
already-cleared slot is idempotent. Snapshot collection occurs before callbacks.

The field is private and absent from SaveSystem's explicit SaveBody/LoadBody
serialization; no save-format or public parameter surface was added. The list
retains only its capacity after successful or exceptional unwind, not a detached
subtree reference. This review finds no additional flaw in that scope mechanism.
Expanded focused execution remains root-owned.

### Resolved: normal Unequip of pending item B

The same two-Buckler setup could invoke `InventorySystem.UnequipItem(actor, B)`
from A's AfterUnequip rather than public Body.DropAllEquipment. Normal
`UnequipCommand.CaptureEquippedState` sees no attached-root slots for B, uses
its remaining numeric cache key as a legacy slot, then InventoryPart.Unequip
clears the cache and reverses the bonus but does not clear active detached-tree
aliases. The outer ForceUnequip still saw B's alias and could reverse/publish
again. The private Body scope alone was insufficient for normal commands.

Root confirmed this RED in both regular and extrinsic rows: 34 dedicated cases,
32 pass / two fail. The test now explicitly requires the pending item's ordinary
transfer to refuse, followed by two total cleanup events, Agility 14, cleared
captured slots and exact ground items. The no-nested and nested-public-drop rows
remain controls in the same fixture.

The applied `Body.ForceUnequipAll` at lines 859–875 guards the complete distinct
captured set before invoking the first ForceUnequip. Both DropAllEquipment and
UnequipSubtree use this helper. All newly owned claims are released in reverse
order in finally, including partial acquisition/observer failure paths. Existing
outer claims return null and remain with their owner; per-item ForceUnequip
borrows the batch claim. New guards have no payment deltas or undo actions, so
their final Commit only releases ownership and cannot publish a wallet callback.

Pending B is a participant in the forced removal, while a different spare item
on a surviving hand is outside that set and remains available to independent
commands. Nested public forced cleanup is still allowed and sees active roots;
the outer pass skips items whose exact slots/cache were already cleared. The
whole-batch barrier also prevents reusing a just-processed item from a later
item's callback before the removal operation ends. No raw storage API or planner
change is needed. This source pass finds no new concrete regression in the
batch mechanism; root's expanded focused/native runs are still required.

## Resolved test-validity correction

`ForcedCarryFallbackKeepsDistinctIdenticalItemEvenAtCapacity` originally compared
a Duelist Cut Buckler with a plain Buckler, which already refused merging.
The corrected fixture applies Duelist Cut to both and asserts CanStackWith
positively before injury. Full/nonfull rows retain exact count/reference checks.
That closes the nonvacuity finding and supports the equal-payload no-merge claim.

## Added pins inspected

- Existing outer item claim: forced cleanup succeeds, same item Equip remains
  refused until the outer transaction commits, then normal Equip reapplies the
  actual GlowQuartz radius/flag. Finally rolls back an unfinished outer claim.
- Owned claim after success/throw: the observer deliberately throws only in its
  flagged row; after clearing it, normal same-item Equip succeeds. The test
  explicitly limits its claim to guard lifetime and ordinary bonus state.
- Normal regeneration: the same hand reference returns empty, the exact carried
  item remains a singleton, and later normal Equip adds its bonus once.
- Save after injury: every saved detached subtree contains no equipped alias;
  regeneration does not restore a ground item's old equipment link.
- Prior fixtures cover multislot/one event, root-level handwear survival,
  HardenedShell and unrelated Speed penalties, missing inventory, no-zone
  LightMap invalidation with unchanged zone membership, exact diagnostics,
  real combat death and explicit NoDrop suppression, and normal/veto controls.

Root's new pins do not automatically establish every proposed enhancement or
natural-equipment row from the design matrix. Current dedicated files exercise
GlowQuartz and real equipment modifications; avoid claiming newly executed
Engraved/Lacquered/Temporary/natural-default cases unless corresponding tests or
clearly identified existing neighbors are included in the final evidence.

## Qud comparison and intentional divergence

Read directly from the local reference:

- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Body.cs:2505` calls
  `Part.UnequipPartAndChildren` before `CutAndQueueForRegeneration` at line 2614.
- `/Users/steven/qud-decompiled-project/XRL.World.Anatomy/BodyPart.cs:3545–3609`
  sends forced unequip/drop operations, excludes natural real equipment from
  loot, catches/logs operation exceptions, and snapshots child traversal.

CoO deliberately detaches/registers first so its newly published cleanup
callbacks cannot fill a doomed slot. The current Body method summary was fixed
to state that divergence explicitly; retain it in phase/commit documentation.
CoO's exact inventory fallback, enhancement dispatcher, claim guard, separate
`_DefaultBehavior` channel, and exception policy are local contracts, not an
exact Qud port. The Qud reference supports using a forced lifecycle; it does not
establish identical callback order, recovery policy, or natural-item taxonomy.

## Boundaries retained

No generic callback-exception rollback/continue guarantee. A thrown observer can
leave a partially processed injury; only owned guard release is guaranteed by
finally. No new global migration of raw unapplied equip bonuses or corrupt
cross-actor equipment aliases. Managed/implied-part removal and arbitrary stale
prebuilt Equip plans remain separate recorded seams. The downstream death
carried-drop AddEntity(false) issue is already explicitly bounded in the phase
plan; Body's fallback alone does not repair it.

The work adds bounded injury/death traversal and allocations, not a frame
listener. No zero-GC, speedup, sprite, lighting appearance or injury-feel claim.
Root owns focused/full/native execution and attribution; this report verifies
source and test reasoning only.

The native result currently reported by root is intermediate: the first run
passed 99 observations before a driver assumption failed. Death-screen load does
not publish F6's `Game loaded.` prose; the driver was split to check graph/clock
recovery on both routes and that message only on F6. The failed raw evidence is
retained, and a rerun is pending. This is not a complete native pass and was not
independently executed by this reviewer.

## Root execution after this source review

Final focused456GREEN; native111PASS/0FAIL/0unexpected with successful teardown;
full9369GREEN/0CS. The intermediate evidence above is retained as review history.
See GA03f-REPORT.md for raw artifacts and exact times.
