# Recovered liquid oil feeds portable torch light

November23,2026 (America/Chicago). Direct finite oil-to-torch refilling is
implemented and verified through native checks and actual GameBootstrap input.
Final independent source/gameplay-evidence/artifact review found0P1/P2; bounded
Editor acceptance is supported. This
connects existing regional processing to another equipment use; it adds no
saleable LampOil output, new fuel economy, decay or compulsory darkness.

## Behavior and native evidence

A valid carried oil flask offers the exact unlit packed singleton torch, with
one-oil cost and current→capped fuel. One oil adds up to25fuel, like existing
LampOil. Excess above capacity is lost; the torch stays out. Bought LampOil,
ordinary torches and existing ignition sources retain their behavior. Only
ordinary registered liquid oil qualifies, not arbitrary combustible liquids.
Existing light/fuel/thermal Parts own lighting and burning. No saved fields added.

Quotes retain exact values; display rounds to five significant digits. Refill
precedes the many transfer/pour rows and explicitly costs OneAction. Shared
inventory claims and final state checks protect exact source, torch and Parts.
Rollback owns quantity/fuel deltas; it preserves independent injuries, custody,
replaced Parts/liquids and bounded independent scalar changes. Arbitrary
unstructured consumption of provisional credit is not fully reversible.

| Native run | Result |
| --- | --- |
| red01 |22total/11pass/11fail/0CS; positive cases lacked the new row, negative controls refused. |
| green01 |257total/256pass/0fail/1Explicit saved-contract generation utilityskip/0CS;13.8158968s excluding imports. |
| readability_red01 |1total/1fail/0CS; actual48.8000031float noise reproduced. The initial test's transfer-priority assertion had no transfer recipient; final fixture adds a real empty recipient and asserts transfers exist. |
| green02, final |54/54/0CS:23oil cases and31retained torch readout cases. Overlaps green01; do not sum runs. |

The257core run contains22new oil,27DensityTorchAdversarial,24DensityTorchLight,
45FiftySecondPreparationAdversarial,26FiftySecondPreparation,54OilStraining,
31TorchReadout and28SavedPartContract (27pass/1skip). No full-suite acceptance.
Countercases cover unsupported sources/torches, pure/stale menus, partial cap,
canonical empty flask, before/after callbacks, reentry, injuries, drops, changed
liquids, independent deltas and graph reload. Raw XML/logs: [Native](Native).

## Actual input, provenance and readability

Game d3c56928ecec4cd0a4532b3402867974, strong ProofingWorkshopShowcase seed64.
Started from the earned ColdOilPlay220 archive at36,10, HP400/purse50. Its flask
2231 already held3oil actually settled from12mire at70→80 in that earlier play;
see [provenance](../ColdOilPlay20261121/README.md). Reuse of earned evidence is
explicit: no new ordinary acquisition, commerce or balance proof. No item,
health, fuel, time, money or position was injected in this increment.

Ordinary auto-equip split one full torch from stack2298 into exact singleton
9be07b23c7a24a079adb7aeb10f66c7b. Four paid moves reached39,9 at260 and used
50→48.8000031fuel. Actual extinguish270 left that fuel. A UI-close assumption
sent one ordinary down move280 and an awaiting-direction prompt; observed menus
corrected it, without resetting state. Unequip was free. The flask's real menu
offered the refill, but buried it beneath transfers and showed float noise.
Cancellation preserved280/source3/fuel48.8000031. F5/four-file archive preceded
isolation finish and the small display/priority correction.

Fresh Continue280 retained that before-payment state. The revised actual menu
now shows the refill immediately below Examine with48.8→50/50. Paid Enter
280→290 spent oil3→2 and filled only that torch, still Out. This actual capped
gain is about1.2fuel; full25gain is native-tested, not hand-played. The selected
F1 flame/fuel page shows50/50 and both fuel alternatives. Its whole five-page
document was observed structurally, not all pages traversed.

Normal equip/move reached39,9 at300; actual light action310 lit that torch
through existing ignition, and the normal burn left49.7fuel. Normal world-map
departure320, waits to900 and return910 retained2oil and the torch's real burn
progress31.7000465. Some rapid .07s waits were dropped; slower observed waits
reached the actual tick. No clock jump or ordinary-discovery claim.

Four left moves reached35,9 at950, held torch30.50005/lit. Native extinguish
950→960 at the same cell reduced the actual renderer's light values:32,9
0.9428572→0.6 and30,9 0.63809526→0.466666669. Ambient remains0.4 and the
fixture's player light and camp lights remain; screenshots are subtle. This
proves a practical light contribution, not sole illumination, a new night
ambient policy, matched pixel equality or a graphics improvement.

The return/relight/walk was interrupted by a viper. Retained log records bite3,
three2-damage poison ticks and an ordinary fist8kill. Later relight used the
finite flint striker per the log, rather than a claimed free hearth relight.
Remaining strikes and final poison status were not separately compared.
The final earned result is1040 at37,9, HP391/purse50, oil2, exact torch held/lit
with29.3000526fuel, hearth16. Planned35,9 was not reached in this last sequence.
F5/four-file archive preceded Stop. Completely fresh Continue1040 restored
the entire measured snapshot exactly; this is selected state, not a whole-save
or entire-inventory equality claim. Another F5/archive preceded final finish.

Actual screenshots, raw snapshots and retained log: [Actual](Actual).
Earned save hash receipts: [Earned](Earned); payloads stay outside the repo.

## Tooling, review and preservation

Before play, main initially compiled a stale source graph before importing the
new asset; native clone compilation was clean. Explicit import resolved it.
Compilation had completed just before an unnecessary but safe clean idle main
restart; no Play began on the stale assembly. Later known-asset imports worked.
An observer guessed a nonexistent CookingFirePart, then used actual CampHearth
identity. A post-play log observer guessed a public field and was corrected to
its reflected private field. Failed observers posted no gameplay inputs.

Independent design/source critique and bounded implementation review found
0materialP1/P2. Final artifact/gameplay-evidence review verified27artifact pins,7source/test
pins, counts, cancellation and fresh measured state with0P1/P2. Reviewers do
not act as second gameplay operators.

Final input settings/devices restored, main stopped/rootnull/clean, profiler off,
original7built-in views/index0. Fresh selected SHA comparison:84historical
foreign files and861selected normal files unchanged; the separate foreign
handoff and PackageManagerSettings match the retained Mac receipt. Unity
operational state/TestResults are excluded, not byte-exact full preferences.
All18apps and prior earned roots retained. CurrentMac7ed predates rain/oil;
no package, FPS, hardware, Intel or Deck acceptance. Original failed regression
and all12complete-machine gates stay open. See [receipt](receipt.json) and
[preservation](Actual/preservation.json).

After bounded publication, broaden to another material/tool/project interaction
with medium-depth nuance. Do not expand this into a universal fuel system.
