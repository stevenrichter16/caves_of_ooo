# GA02c — transfer conservation and honest sale refusal

Status: COMPLETE. Full7939/7939 GREEN,23:18:03–23:19:33UTC; zero C# errors.
Baseline b468e614,7866/7866 GREEN.

## Outcome and scope

A01/A04 and the transfer portion of A05: rejected ground placement preserves
items; full containers restore the exact carried/equipped identity; failed
outer commands reverse complete and partial merges. Successful transfers keep
their normal stacking behavior. Trader hard-capacity refusal preserves goods,
equipment and wallets, and the same trade screen shows the actual refusal and
can retry successfully when capacity clears. Partial-drop and equipment-split
rollback refresh the authoritative inventory's handling penalty.

InventoryTransferSnapshot seals only the membership/count/backreference changes
caused by its immediate Add/Remove. Undo preserves independent transfers of other
items. InventoryTransaction claims participating entities until commit/rollback;
separate reentrant disposition/equipment/sale operations on them refuse. A merge
also claims affected destination stacks. Direct operations sharing one outer
transaction remain allowed. Late affordability revalidation after unequip
preserves a different-item nested sale while refusing unaffordable outer payment.

These are short-lived utility objects/private fields, with no serialized Part
field or save-version change. Claims do not guard raw lists or every item action.
A43 pickup/container-take/buy adoption follows separately; A03 functional stack
identity and A41 arbitrary callback/effect rollback remain separate repairs.
ItemDispositionApplied/Rejected describe the immediate placement step, which an
outer transaction may later undo. InventoryTransferRejected explains reentry;
SaleRejected supplies a user-facing reason, and committed sales retain Sold.

## Verification evidence

Every run checked the compiler log first: zero C# errors.

| Gate | Raw archive | Result |
|---|---|---|
| Initial RED | GA02c-red.xml.gz |20:9 failures,11 controls;22:43:22UTC |
| Expanded RED | GA02c-expanded-red.xml.gz |22:10 failures,12 controls;22:44:41UTC |
| Minimum + neighbors | GA02c-green.xml.gz |338/338;22:48:12–13UTC |
| Adversarial RED | GA02c-adversarial-red.xml.gz |55:12 failures,43 controls;22:56:02–03UTC |
| Corrected + neighbors | GA02c-adversarial-green.xml.gz |371/371;23:00:18UTC |
| Native staging RED | GA02c-bench-red.xml.gz |7 missing-type failures;23:02:33UTC |
| Nested-payment RED | GA02c-payment-red.xml.gz |58:1 failure,57 controls;23:04:01–02UTC |
| Staging/payment + neighbors | GA02c-bench-green.xml.gz |385/385;23:08:09–10UTC |
| Final counterchecks | GA02c-counterchecks.xml.gz |73/73;23:16:33–34UTC |
| Full suite | GA02c-full-green.xml.gz |7939/7939;23:18:03–23:19:33UTC |

New cases:22 regression +44 dedicated adversarial +7 scenario/diagnostic =73.
The eight final sequential-transfer/destination-collision cases passed on their
first run; they pin correct behavior rather than represent eight found bugs.
Veto/exception retry and callback-entry assertions also passed. Other surfaces
include exact item order/ownership, malformed quantity, null/self sale, saved
stacks, two-handed equipment/bonuses, complete-vs-partial container capacity,
outcome records, independent callbacks and same-screen retry.

The initial RED confirmed lost/refused placement, wrong rollback identity,
merged-stack duplication, stale handling penalties, trader-capacity loss and
misleading UI refusal. Adversarial RED then caught two introduced callback
regressions (whole-list rollback undoing independent work; reentrant same-item
sale/drop), missing disposition records, malformed quantities and self-sale.
The final payment RED caught independently committed sales exhausting the same
trader's purse between the initial check and payment. Each correction followed
its failing test; no unrelated production cleanup is included.

## Native evidence and honesty bounds

Final GA02c-native.json: run4621440f68954790b6086b80724a72a7,
35/35 PASS,0 failures,10.452218166seconds, batch exit0. Matching full Unity log
is GA02c-native-unity.log.gz. Initial35/35 runf740aef9c8474b56b3bfbd63483ea53f
is retained separately; final assertions additionally inspect BodyPart equipment.

Real Dagger3 equips1 and leaves a distinct carried2. Native inventory keys put
the equipped unit into a full underfoot Sack: fresh refusal record, six contents,
original carried stack/index, body slots, equipment dictionary and Physics all
remain correct. Native world pile selection opens that Sack; looting SilverSand
frees one slot and the repeated inventory Put stores the exact equipped unit.
Its body-slot/dictionary equipment references disappear, carried2 stays intact.

Native Chat with an actual Merchant opens trade. Its authored150 hard capacity
holds two natural Starapple stacks99+51. Selling carried Dagger2 refuses with the
exact capacity message and fresh SaleRejected record, preserving goods/wallets.
Buying51 frees capacity; selling Dagger2 then delivers it, pays the actual computed
price once, emits a fresh Sold and clears the previous status. Reflection reads
UI state; normal keyboard input selects and confirms the actions.

**Can verify:** actual menu/modal routing, content identities, body/inventory
ownership, capacity refusal, loot and purchase freeing capacity, quantities,
wallet arithmetic for these fixtures, fresh diagnostic outcomes and clean audit
completion. **Cannot verify:** rendered pixels/feel, native equipped-sale API,
barren vegetation dropping or synthetic handling penalties. Those latter API
cases have EditMode coverage; all authored barren-rejected vegetation is not
normally takeable. Manual launcher cleanup is source-reviewed, not separately
GUI-exercised. The pre-recorded A31 destroyed-camera error still occurs during
Unity shutdown after successful audit completion; it is preserved in raw logs.

The native launcher uses an isolated save slot and restores its prior preference.
Its temporary coroutine restores input device/settings, diagnostic preferences
and callbacks. There is no ordinary new Update/per-turn work, new art/content,
or performance-improvement claim. No 75-second performance capture is claimed.

## Reference contract, divergences and cold-eye review

Root and independent reviewers read Qud Inventory.AddObject:256–301 and its
NoStack path, unequip:2005–2184, Stacker.HandleEvent:134–140 and trade consumers.
Qud carries NoStack through receive/unequip events and has a broader receive
framework. CoO exact-reference receipt/restore implements local ownership
integrity. Qud AddObject returns a GameObject and does not impose CoO's hard150
trader-capacity refusal; its multi-item/partial-quantity/liquid-wallet trade
framework is not ported here. No complete Qud-parity claim is made.

- 🟡 Fixed: capacity/refused placement, exact identity and complete/partial merge undo.
- 🟡 Fixed: callback resurrection/reentry, then late independent-sale affordability.
- 🟡 Fixed: missing outcome records and misleading sale failure UI.
- 🟡 Fixed verification gap: native audit initially omitted combat's BodyPart equipment.
- 🔵 Fixed: same-screen retry and explicit callback-entered assertion.
- ⚪ Kept: hard capacity, existing stacking/equipment events and bool trade facade.
- 🧪 Boundaries: A43 acquisition and A41 broader arbitrary callback/effect rollback
  are recorded follow-up work; claims are not a universal raw-mutation lock.

Independent taxonomy and Qud-contract reviews found no remaining must-fix
production regression within this wave. Root reviewed all production hunks,
undo ordering, exact source restoration before equipment undo, diagnostic
shapes and per-invariant controls. GUID audit:2326 unique,zero collisions.
Owned paths have no overlap with the1027-path protected-work snapshot.
