# GA03f / A10 — forced equipment lifecycle verification

Status: COMPLETE. Accepted injury and normal death drops reverse equipment
contributions, clear exact body/cache/Physics ownership, and dispatch the ordinary
post-unequip/enhancement lifecycle. Saved dropped gear stays unequipped. Rejected
injury, unaffected gear, unrelated penalties and legitimate mobility loss retain
their behavior. DropAll processes distinct body plus compatibility-cache items;
Body-level refused placement retains the exact carried item without merging.

## Executed gates

| Gate | Result | UTC interval | Seconds |
|---|---|---|---|
| red | 3/13 PASS; 10 FAIL | 2026-09-06 10:21:34Z – 2026-09-06 10:21:34Z | 0.3831101 |
| minimum | 134/134 PASS; 0 FAIL | 2026-09-06 10:24:21Z – 2026-09-06 10:24:21Z | 0.4842798 |
| adversarial-red | 32/35 PASS; 3 FAIL | 2026-09-06 10:30:17Z – 2026-09-06 10:30:17Z | 0.457001 |
| focused | 156/156 PASS; 0 FAIL | 2026-09-06 10:32:57Z – 2026-09-06 10:32:57Z | 0.6681726 |
| review-hypotheses | 163/164 PASS; 1 FAIL | 2026-09-06 10:44:05Z – 2026-09-06 10:44:06Z | 0.7420474 |
| detached-scope-red | 30/32 PASS; 2 FAIL | 2026-09-06 10:46:05Z – 2026-09-06 10:46:06Z | 0.4569399 |
| final-focused | 166/166 PASS; 0 FAIL | 2026-09-06 10:47:15Z – 2026-09-06 10:47:16Z | 0.8207214 |
| pending-item-red | 32/34 PASS; 2 FAIL | 2026-09-06 10:50:21Z – 2026-09-06 10:50:22Z | 0.5351344 |
| settled-focused | 456/456 PASS; 0 FAIL | 2026-09-06 10:52:25Z – 2026-09-06 10:52:27Z | 1.4085828 |
| full | 9369/9369 PASS; 0 FAIL | 2026-09-06 10:55:03Z – 2026-09-06 10:57:21Z | 137.99489 |

Every runtime row had0C# errors. First authoring compilation missed the Data
namespace for EntityFactory; its log is retained and is not runtime RED evidence.
47new tests:13initial +34dedicated. Baseline9322→9369; count is not a bug count.
Initial RED reproduced10 failures with3controls passing. The dedicated/review
cycles confirmed five callback defects: same-limb reentry, fresh gear in a doomed
slot, same-item re-equip during cleanup, nested forced cleanup of another detached
item, and ordinary transfer of another pending item. Regular and extrinsic limbs
have paired controls. Additional guard, regeneration and saved-subtree hypotheses
were already correct after the first fix and are regression pins, not new bugs.

## Implementation and review

Body settles anatomy/regeneration before publishing forced cleanup. Active
subtrees remain visible to nested cleanup through a private finally-scoped list,
including extrinsic limbs omitted from regeneration records. Complete-set item
claims prevent ordinary commands from taking participating gear mid-removal;
forced nested cleanup remains allowed and skips exact items already processed.
Existing outer claims retain their owner. Unrelated spare gear remains usable.

Generic equip contributions reverse once, exact aliases clear, a ground/carried
destination settles, handling/equipment caches refresh, and event/ForcedUnequipped
records exact actor/item/blueprint/destination. AfterUnequip precedes enhancement
cleanup as in the normal command. The obsolete ClearEquipmentFromAllParts helper
was removed; captured slots cover attached and detached references together.
No raw inventory storage lifecycle was changed and no save fields were added.

Independent review is GA03f-INDEPENDENT-REVIEW.md. Root cross-checked claim ownership,
slot membership, transient scope unwind and Qud ordering against source. The equal
carry fixture was corrected to use two actual DuelistCut Bucklers with positive
CanStackWith, so its no-merge assertion is nonvacuous. All notable findings in
this bounded wave were fixed before commit.

Qud Body2505 unequips before CutAndQueueForRegeneration2614. CoO deliberately
settles anatomy first so its new callbacks cannot refill a doomed slot or sever
it twice. This is a documented divergence; CoO enhancement/claim/destination
semantics are local integration, not full Qud parity.

## Native gameplay evidence

Final run `b400878318fc4156a9465bbb91ce3fe2`:111PASS/0FAIL/0unexpected errors, 12.4065528seconds.
Shutdown 12.4267235seconds; runtime saving unregistered, disposable
root held through teardown and removed afterwards. Raw final JSON/log retained.

Queued keys use actual I/Tab/arrows/Enter equipment menus, developer F8 with a
BeforeDismember veto then left/right/Feet injury, F5/F6 and death-screen L. Actual
DuelistCut Buckler adds Agility2, actual GlowQuartz Dagger adds radius2, and actual
IronshodBoots adds SpeedPenalty5. Saved healthy and injured graphs retain exact
item identities/slots and the correct contributions. Feet loss adds legitimate
MobilityPenalty60 while removing the boots5 and retaining unrelated7 (total67).

Walking into the actual12damage SpikeTrap with staged13HP survives and preserves
gear; staged1HP triggers real death and clears all three equipped items once.
Death-screen F6 is suppressed; L restores healthy100HP, equipped bonuses, the
trap and exact saved clock. No probe Parts are saved. Fixture mods, HP and owned
checkpoint-byte restoration are explicit setup rather than player actions.

First run43d3cb546d034a13b524277d74a22de7 passed99 observations before a combined
recovery assertion required F6's success prose from the death controller. Source
shows that only the F6 controller adds that line. The driver now checks graph/
clock recovery on both routes and the message on F6 only. First failed final JSON
records one bench failure plus its unexpected assertion error; both raw artifacts
are retained. Final recovery passed without a production death-controller change.

### Can verify

Script-observable ownership, real content/mod effects, equipment callbacks and
lighting state, accepted/vetoed injury, genuine trap death and loaded graph/clock
recovery through queued gameplay input. GUID audit2448valid/2448unique/0collisions.
The private cleanup scope is absent from saved Body fields.

### Cannot verify

Physical keyboard delivery, sprites/lighting appearance, injury feel, ordinary
random-combat dismember probability, performance speedup or zero allocation.
This adds bounded injury/death traversal, with no new frame/turn listener and no
performance claim requiring a new75second workload.

No general observer-exception atomicity/continuation guarantee: throwing observers
can leave a partially processed injury; owned item guards/scopes still unwind.
No migration of raw unapplied historical equip bonuses or corrupt foreign aliases.
Body failed-placement fallback alone does not fix later Combat.DropInventoryOnDeath
ignoring a refused AddEntity for vegetation-tagged API equipment; that remains in
the whole-game AddEntity-failure queue. Managed/implied removal, A11 mortal death,
A41 generic callback rollback and stale prebuilt plans remain separately scoped.
Loadout repair is next; current authored Loadout reach is absent. Proposed extra
Engraved/Lacquered/natural/Temporary rows are not claimed as new GA03f tests.

## Attribution

Only Body.cs and InventoryTransaction.cs change production. Two new test classes,
three scenario/launcher classes and matching metadata, living docs and GA03f raw
verification ship with them. Body/InventoryTransaction were clean at baseline.
Existing spell/art changes and the prior applied Save/ZoneRenderer patch portions
remain unstaged and untouched by this wave. No content JSON, sprite or save format
was edited. No push or merge was performed.
