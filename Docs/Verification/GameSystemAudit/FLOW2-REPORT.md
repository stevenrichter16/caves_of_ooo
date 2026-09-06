# FLOW2 — visible refusals and contextual retry

Status: COMPLETE — NATIVE AND REGRESSION VERIFIED. Baseline9e90d421,8542GREEN.
Accepted smoothing step3; existing CoO interaction contracts, no Qud parity claim.

## Behavior and scope

Failed inventory actions now keep the exact popup, item and cursor. Equip-slot,
manual body selection, unequip and displacement confirmation follow the same outcome
rule. Failed replacement keeps both the confirmation and its originating popup.
Command helpers preserve their bool signatures and capture the returned reason;
shared detail row44 displays it without replacing the row43 action legend.

Forge, brew and tinker refusals use that surface. Successful forging followed by a
failed independent quench explicitly reports the partial outcome; the weapon and
paid components remain committed. Enter still starts another forge, not a quench-only
retry. Partial successful batches do not acquire a false full-failure message.
Crafting marks and Clear picks now share the same action-status lifetime.

Status persists across redraw/rebuild and movement within a popup or crafting list.
Changing item, recipe, equipment slot, mode or popup clears it; successful attempts,
explicit Clear picks and inventory open/close clear it. Throw/examine retain their
existing handoffs. Successful tinkering can legitimately restore a red normal detail
about missing bits for the next craft; cleared failure does not mean a blank footer.

Runtime changes stay in the two InventoryUI files. No command/payment/target rules,
content/art or saved fields change. Existing trade/pickup status remains in place.
Removed duplicated unconditional popup-close blocks in favor of one outcome path;
this is no claim to remove unrelated dead mechanics. A47 and broader audit stay open.

## Source corrections and TDD

Root and independent readers checked UI action dispatch and all completion paths,
PutInContainer, Sack content/discovery, the real transfer native route, Forge/Brew,
Tinker bit costs/yields and shared status rendering. Source corrections are recorded
before implementation in MECHANICS-FLOW-SMOOTHING-2026-09-05.md.

- Full Sack means six distinct entries; a fully mergeable stack can still be put in.
- The inventory menu finds same-cell containers; the native repair uses actual loot.
- Paid Sharp uses ModSharp tag/penetration/ModificationCount, not random Sharp's Part.
- Seed label Plant dispatches PlantSeed; Part overrides HandleEvent, not FireEvent.
- BodyPart namespace is Core.Anatomy; DrawText omits spaces after clearing tiles.
- Final-unit consumption removes an Entity while its detached StackCount can remain1.
- Composite quench failure cannot be described as whole-forge failure/refund.
- Historical GA02c driver expected refusal closure; its reusable assertion/Escape route
  is updated, while new reruns write FLOW2-transfer-native.json to preserve old evidence.

First compile6CS0246loglines and symmetry compile3CS0506loglines are retained; no stale
XML was used. Corrected24RED and34symmetryRED preceded runtime implementation. The
missing-status assertions also fail positive controls; these are not34distinctbugs.
Minimum143GREEN. Dedicated66run:62pass,3confirmed feedback gaps plus one detached-count
fixture error. Fixes followed that RED. Expanded181GREEN; staging80RED passed72 with
only8missing-native-scenario failures. Final Clear-picks39RED passed38 and reproduced
one stale-message defect before its fix. Focused241GREEN,04:22:54–04:23:01UTC,
7.2221512seconds,zeroC#errors. New tests34regression,39dedicatedadversarial,8staging.

## Cold-eye and adversarial review

Independent taxonomy/reference review plus root pass attempted ten breaks:

1. Repeated refusals mutate items/selection: exact carried/container arrays and units.
2. Equip/manual/unequip lose context: blocked/control hooks with exact successful retry.
3. Replacement discards confirmation/origin: both originating UI paths and occupied part.
4. New refusal leaves old reason: locked/full reversal and safe null/empty reason fallback.
5. Navigation erases useful feedback: popup/craft retention versus recipe/mode exit.
6. Clear picks leaves stale failure: actual C action RED then action-entry clear.
7. Failed quench claims full failure: one-shot actual drop callback, paid kit and retained
   weapon, versus normally consumed quench/tempered weapon. Callback assertions run outside
   the callback so the command executor cannot swallow NUnit failures.
8. Partial batches become false all-or-nothing failure: full/partial forge and brew,
   actual output unit counts, checked preconsumption and supported callback controls.
9. Retention breaks handoffs: announcement and exact pending throw item remain intact.
10. Feedback overwrites layout or persists after success: actual tiles, clipping, repeated
    redraw and retry restoration. Capacity/bit/target controls also pin no extra payment.

All discovered production findings fixed before close-out. Script context checks are
bounded reference/integer comparisons during existing renders, not a measured claim
of zero total allocations. Native driver/source review found no remaining route,
measurement, completion or save-isolation blockers.

## Native and honesty bounds

New disposable arena combines actual full Sack, equipped singleton plus carried Dagger2,
Forge/Still, real components/reagents and craft_dagger with C only. Native keyboard first
refuses Put and observes the identical popup/action/cursor and existing status tiles.
It then closes both menus, opens the actual Sack loot, takes SilverSand, reopens Put and
transfers once. Tinker refuses missing B, natively disassembles one dagger to obtain B,
then retries BC crafting and merges back to two units. Forge incomplete-pick refusal
and FireMoss invalid-brew refusal each get actual selection repair and retry.

Can verify native keyboard dispatch, existing rendered status glyph/color/legend,
selection continuity, exact item/bit payments and output units. Same-instance Sack
retry after capacity changes, equipment vetoes, stale selections, null reasons and
callback partial outcomes are EditMode evidence. Native loot repair necessarily reopens
the popup. Reflection only observes; it does not select items or force a render.

Cannot verify subjective buttery feel, broad visual quality or OS mouse delivery from
these headless runs. FLOW1's desktop legacy-mouse limitation remains explicit. Known
A31camera teardown, if logged, stays in raw evidence rather than being omitted.

## Measurement and completion

The immediately preceding UI baseline is FLOW1 run775c6f2fbb674d21a6700a924b584b5c:
75.000108seconds,366913frames,70/70navigation,69/69toggles. Final FLOW2 uses the same
75second25idle/25navigation/25toggle recorder and bounded500000sample capacity. Metrics
use nanoseconds for script markers and bytes for whole-frame GC. Prior-frame sampling
makes phase boundaries approximate; editor/harness setup and actual input remain in
whole-frame GC. Capture order/setup differ, so no performance speedup claim.

Final combined native, historical-driver compatibility,75second raw/comparison, full
suite and GUID/ownership results follow before commit.

Combined native51/51PASS,run3906820dbf0e446ea646329a926696ab,90.842086042s,
exit0,zeroC#errors,no logged native exceptions. Measured75.165754333seconds,
363171frames,70/70navigation and69/69toggle changes. Input max2.451042ms,
p99.002541ms; inventory render max1.068166ms; whole-frame GCmax4,348,176bytes.
The prior baseline input max2.391458ms,p99.002292ms and render max1.481416ms
do not support a speedup claim. Raw compressed CSV and phase statistics retained
in FLOW2-after-frames.csv.gz and FLOW2-perf-comparison.json.

Historical transfer compatibility rerun35/35PASS,c688c7e8fa8c44bd8865f2e81da46865,
10.5798875seconds,exit0,zeroC#errors,no logged exceptions. Original GA02c archived
outputs remain intact. Full suite and final ownership/GUID results follow.

Final full suite **8623/8623 GREEN**,0compiler errors,0failed/skipped/inconclusive;
2026-09-06 04:28:14–04:30:03UTC,108.4742895seconds. Added81tests
(34regression,39dedicated adversarial,8scenario staging). Raw FLOW2-full.xml.gz.
Final asset audit:2380unique GUIDs,0collisions. All37owned paths exist and have
zero overlap with the1027-path protected baseline manifest. No art, blueprint or
save schema changes. Independent and root cold-eye: no remaining production
🟡/🔴 findings; desktop mouse/visual feel remain explicitly unverified.
