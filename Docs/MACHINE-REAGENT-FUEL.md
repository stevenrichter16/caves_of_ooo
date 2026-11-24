# Optional reagent fuel for finite fires

November24,2026. Implemented; native and actual integration checks passed.
Independent final source/recorded-gameplay review0remainingP1/P2. Baseline runtime088,
documentation headf8b7d1b31. Current Mac088 predates this feature.

The player can gather pitchpod resin and choose brewing, proofing, lacquer or
fire fuel. This adds a reason to carry regional stock while preserving timber
for repairs. Timber feed/full/stoke, ordinary fire services and ignition remain.
No new harvesting rate, calendar gate, upkeep or automatic fuel selection.

`combustible` is currently an alchemy property. This increment proposes an
explicit physical interpretation at a finite CampfirePart fire: one dry, stable
reagent gives5fuel per potency (supported1-3), up to capacity. The fire's own
menu names the exact item, one-unit cost, actual gain and discarded surplus.
Feeding adds no heat and does not light it. No volatile fuel, even when malformed
property text would otherwise hide volatility. Heat alone is not fuel permission.
LampOil3 and PitchpodResin1 qualify; BlastcapSpore volatility does not.

Admission stays local, without broadening stacking/processing policies. Exact
built-in simple Parts/ownership, unique IDs/Parts and inert bookkeeping only;
reject enhanced, wet/frozen, equipped, aliased or unknown live payloads. Parse
the complete property string strictly instead of relying on dropped parse errors.
Quote source identity/count/properties, actor origin and fire state. Stale input,
capacity, fuel, reach or Parts refuse without substitution.

Join the outer inventory transaction. Immediate transfer cleanup precedes a
one-unit custody-aware refund; undo only the actual clamped fuel gain. Preserve
independent injury, additions, transfers, drops and fire changes. Revalidate at
commit. Unknown detached partial identities receive a limited-refund diagnostic,
never a guessed recapture. Existing timber behavior is outside this change.

Use the existing fire-owned pack/C-menu event path, not a global reagent adapter.
A minimal Part receipt-ownership contract can keep another command-specific
exception out of the shared executor; prove it with existing hearth/torch
kindling plus the new fuel command, without migrating unrelated legacy actions.
No new saved Part/field, producer, receiver container or simulation framework.

Bounded independent design/source critique approved the optional loop with
strict complete parsing, local payload admission and newer delta receipts.
Gantry shade was considered first and rejected: actual BiomeAt7,8=Spread,
so the current Beating-only glare rule gives it no immediate practical value.
No Gantry runtime/blueprint edits were made.

Actual before: continued supplied proofing930 with no new grants, walked9steps
to40,10/1020 then north to40,9/1030. Hearth2147at40,8 has4.5/30fuel and156.674423C;
normal C menu only offers timber feeding while resin2167x8 is carried. Native
F5/four files archived before Finish at /private/tmp/coo-reagent-fuel-before-earned-20261124.
This fixture is not a natural acquisition/discovery claim.

Implemented the generic stable-property rule on the fire's existing menu path,
with a shared Part receipt hook for this command and existing torch/hearth
kindling. No new command switch for fuel in the executor. Local admission accepts
only the complete combustible/heat/viscous vocabulary at tiers1-3, built-in simple
Parts and inert tracking; unknown payloads and extra item stats/properties are not consumed.
Representational zero/rounded-up gains refuse before spending. Late identity or
payload changes preserve unknown state and diagnose limited refunds.

Valid RED34:18fail/16controls/0CS. Review exposed singleton custody, float gain
and late-ID defects; compiling RED2+4 reproduced them before correction. Expanded
GREEN250total/249pass/1Explicit generator utilityskip/0CS includes45new cases;
three focused mutants killed. Final copy check follows the same source rules.
The first test-only SpatialZone compilation failure is excluded from RED.

Actual supplied1030->1100: three ordinary waits cooled hearth to148.930908C/
4fuel. Two C-menu resin feeds gave4->9->14fuel, resin8->6, still cold/dark.
Normal flame relight1090 ->13.75fuel/441.5C/lit. Existing still BakeClay at1100
spent8mire/10process fuel ->one clay (25->26); normal burn left3.5fuel. All26timber
and4moss kept; HP364/50drams unchanged. Fullfresh1100 selected snapshot exact;
all readers free, both four-file archives verified before Finish. Five screenshots.
See [receipt](Verification/MachineSystems/ReagentFuel20261124/README.md).

Reused ordinary resin acquisition/lacquer and clay-repair consumer evidence;
no replay or claim that this fixture obtained materials naturally. No autonomous
processing/full-machine acceptance. All twelve gates and failed full suite open.

## Performance and scope

Only explicit menu/action calls enumerate carried inputs and allocate quotes.
No new per-frame/per-turn listener, cache or saved timer. Existing fuel/thermal
turns and light invalidation remain authoritative. Report actual renderer/hardware
limits honestly; no performance improvement is inferred from these small changes.
