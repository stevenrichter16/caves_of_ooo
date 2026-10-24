# Restore factory identities after loading a saved world

## Status — 2026-10-24

Native focused GREEN confirmed:12/12 passed,0skipped at16:36:32 UTC, preserved
in `Verification/Live16x24/works-products-uses-save-identity-green.xml`.
Affected native regression passed220/220,0skipped at16:37:36–45 UTC.
The two GREEN result sets are disjoint:232 distinct cases. Ordinary replay
from the untouched earlier checkpoint also passed; see the acceptance below.
The first discovery run found zero tests before the
new fixture imported; it is not RED evidence. Fresh native RED at
16:34:44–45 UTC has12 cases,6passed/6failed/0skipped, preserved in
`Verification/Live16x24/works-products-uses-save-identity-red.xml`.
The six failures are loaded numeric range, nested-before-hook range,
inactive-zone range, two exhaustion cases and the actual missing recipe row.
This is a CoO save correctness fix found during the works-product ordinary
graphics/use gate, not a Qud implementation-parity claim.

The real continued game carried CandyCarrot ID2710 and subsequently created
MendleafSprig ID2710. Strict carried-item authority correctly hid the sprig's
preparation action. The intended two-sprig recipe and station were valid.
Evidence: `Verification/Live16x24/works-products-uses-duplicate-identities.json`
and the `concentrate-not-offered` screenshot. Preserve that failed checkpoint;
replay the untouched earlier save after fixing allocation, not by rewriting
the duplicate objects or weakening custody.

## Verified source corrections

| Premise | Source correction |
| --- | --- |
| Missing still or fewer than two sprigs hides the menu row | `PreparationRecipePart.HandleEvent` only gates discovery on exact carried ownership and authored recipe. Station/count are execution checks. |
| Harvest merge corrupts the resident back-reference | Normal `InventoryPart.AddObjectCore` sets the first unit's custody and merges later units. Actual receipt instead has two distinct carried references sharing one ID. |
| A loaded factory continues the saved counter | `EntityFactory` starts its private integer at1. `SaveReader` creates plain placeholders and restores IDs, with no allocator reservation. |
| Reserving during each body decode is safe | QuickLoad can reuse the live factory. A later parser/footer failure must not change its counter. Commit only after validation. |
| Only inventory needs scanning | The existing decoded token graph includes inactive zones, containers, equipment and other referenced owners. Reserve that graph before any load hook can create entities. |

## Bounded implementation plan

1. Add focused behavioral tests using actual token/session loading, a new
   factory, load-hook allocation and actual harvest/action discovery. Include
   unchanged higher live counters, rejected-load isolation, opaque saved IDs,
   nested/inactive owners and integer exhaustion. Root owns native execution.
2. Keep the factory's existing integer sequence for ordinary creation. Add
   an internal positive numeric reservation operation that never rewinds.
   Saturate at `int.MaxValue`; use the existing GUID-N identity convention
   when saturated rather than wrapping or returning a possibly occupied ID.
3. Reserve decoded identities once at the shared `RunLoadHooks` entry:
   standalone token graphs have passed their final check; sessions have also
   completed the binary footer and saved-plan metadata validation. Source
   inspection confirms those preceding plan restorers do not allocate factory
   entities. Reserving here prevents a later invalid plan from changing the
   borrowed factory. No save-format change, source ID rewrite,
   per-frame work, new global registry or weakened action permissions.
4. Focused GREEN, affected save/preparation regression, independent source
   review, then the real earlier checkpoint and ordinary concentration menu.

## Limits and self-review

- 🟡 Existing already-colliding saved identities are intentionally not repaired.
  Ambiguous references cannot safely be reassigned by this allocator fix.
- 🔵 Reservation visits the already decoded graph once at load; it does not
  scan the world on each creation. Opaque saved IDs remain byte-for-byte.
- 🧪 This prevents allocations from colliding with the loaded numeric range.
  Uncoordinated factories mixed manually into one world remain outside this
  supported load path. The ordinary acceptance below remains bounded to its
  named current-zone and carried-field observations.

The correction retains the integer field/type for existing factory callers,
uses invariant positive integer parsing and formatting, and saturates to the
already supported GUID-N convention. `LocalPeople` accepts both factory
numbers and GUID identities. Independent fixture source review found no
compile/setup blocker. Settled production review by
`/root/projection_cold_review/look_focus_draft_review` found no material
finding: validated commit ordering, whole token graph before hooks, no earlier
factory creation in the plan restorers, unchanged saved IDs, monotonic
reservation and overflow safety were checked. The review was read-only;
root executed the actual native tests. Author self-review reached the same
bounded conclusion. No ordinary gameplay acceptance is implied by12 GREEN.

Owned production targets: `EntityFactory.cs` and `SaveSystem.cs` only.
New focused fixture: `EntityFactorySaveIdentityTests.cs`.

## Actual ordinary replay and final review

From the untouched earlier plot checkpoint, normal Continue and walking reached
the works. A Patchbearer blocked the intended path: bump attacks reduced HP to18,
a carried healing tonic and enemy return hits left26, then the real Calm spell
pacified it. No supplies, damage, world state or presentation were injected.
Harvest created two sprigs under new ID7202; the saved carrot retained ID2710.
The real preparation menu appeared, consumed both sprigs into concentrate7204,
and brewing consumed that into healing-only BrewedTonic7205, Healing2d4.

F5/F6 preserved253 recorded current-zone owner rows as an unordered multiset,
12 carried rows with unique IDs, player62,18, HP26,36drams and tick2800.
These are selected observed fields, not every saved property or all zones.
Drinking after reload consumed the tonic and raised HP26→33. Final ordinary
F5 checkpoint is tick2850 at62,18. Existing historical collisions elsewhere
are not migrated or declared repaired. Both original and failed checkpoints
are retained locally outside the user's save folder.

Evidence: `works-products-uses-test-summary.json`, `fixed-save-comparison.json`,
`fixed-mending-used.png/json`, and `fixed-checkpoint.json` under
`Docs/Verification/Live16x24/` (all latter names use the `works-products-uses-`
prefix). Source cold review and independent seven-portrait comparison found
no material defect. Strict ownership checks remain unchanged. The temporary
zero-discovery run and regression connector initialization timeout are not
success evidence; the fresh native XMLs and ordinary replay are.

This closes this allocator defect and the basic works-product route. It does
not complete all saves, every product application, all graphics, a packaged
release or physical Steam Deck testing. No accepted sprite changed.
