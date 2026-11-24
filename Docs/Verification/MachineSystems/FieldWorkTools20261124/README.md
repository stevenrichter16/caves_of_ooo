# Field work tools — bounded verification, November 24, 2026

Three new capabilities: finite aimed hearth heat; a one-root, one-charge carried
rod with one/two-cell straight delivery; and one-water steam condensation using
the elected proofing source's existing catch stock. Three forge recipes connect
existing iron spikes, clay and cord to those tools. Recipes, corrections, tests,
screens and packaging are not additional capabilities.

Contract and source corrections: [MACHINE-FIELD-WORK-TOOLS.md](../../../MACHINE-FIELD-WORK-TOOLS.md).
Code is `FieldWorkToolPart`, the component manifest adapter, shared processing
permission/menu retention, explicit blueprint/manifest content and Tools category.
The new Part saves only Kind and Charge. Tool bodies have no StackerPart; no
physical merge/split or grouping permission was added. Existing Parts gain no
saved fields. Creation-safe policy covers only the audited scalar tool Part;
unknown Parts still refuse. All twelve complete-machine gates remain open.

## Native checks and exclusions

Final `field-tools-final-20261124t`: **245/245**, 46 new and199 retained, zero
compile errors. The199 retained cover striker, processing content/registry/plan/
execution, armor recovery, box work and field binding. Tests ran only in idle
verification clones; main Editor was reserved for actual play.

| Receipt | Meaning |
|---|---|
| a / b | Missing-feature RED:16 genuine absent-tool failures; five nozzle cases initially had incomplete installed-hearth fixtures and are excluded. b includes one passing old striker control. |
| c | Invalid compile attempt after moving an interface member; no acceptance. Corrected Armor's explicit implementation to the base interface. |
| d | First155/155 targeted green. |
| e | Corrected nozzle fixtures against old00846 source in clone4 only: five meaningful RED failures and one passing old control. Main source never reverted. |
| f / g | Three review test fixtures were invalid; excluded. Two precision cases also exposed Mono's floating-point intermediate behavior. |
| h | Corrected185/185 green. |
| i | All three review mutants killed in clone4 only: five meaningful failures, one control. Covers receiver freezing, exact five-fuel representation and earlier committed-observer movement. |
| j |236/236, before actual menu discovery. Superseded by finalo. |
| k / l | Actual-driven stable-menu REDs: each three failures and one control, for all tool families and forge manifests. |
| m |239/239 after stable current-quote retention. |
| n | Steady steam at25°C RED, one failure and one untouched-source control. The vent's declared heat/water stays active while its ThermalPart passively cools; no reheating chore is justified. |
| o |240/240 including the cooled steady-vent case; superseded by finalt. |
| p / q | Last-root forced-state REDs: p3fail/1control, q4fail/1control. Equipped ownership, regrown quantity, raw pack alias and a newly active effect cannot count as consumed payment. |
| r |244/244 after exact removed-unit admission and shared refund alias refusal. |
| s | Own-hearth actor access RED:1failure/3controls; an NPC is not granted access to a player-held fire by proximity. |
| t | Final245/245,46new/199retained, zero compile errors. |

Full readers use pure guide rows. Repeated executable gathers retain an original
quote only for the same actor, zone, exact source/station/claims/choice while its
original predicates remain valid and it is still offered. Consumed and unoffered
tokens are removed. Changed resources, movement, freezing, blocked paths, denial
permissions, callbacks, first-use stock rollback, shared legacy holders, injury
preservation, empty-flask singleton admission and saved charges have countercases.
Final last-root guards additionally reject forced equipment, quantity, alias or
effect changes; shared refund avoids a second same-item/ID list entry. NPCs are
not authorized to spend player-held hearth fuel. Rod and natural-vent actor
capabilities are otherwise unchanged.
Heat/charge reactions occur after committed payment, with an immediate context
recheck. Every log is losslessly gzip-compressed; XML bytes are retained.

## Actual GameBootstrap play

Supplied `FieldWorkToolsShowcase`, seed64, game
`6b560a4d763d40a3b1224805a75ad93b`, ordinary Spread wild zone10.11.0. Authored
fixture supplies three spikes, three clay, one cord, hearth/timber/striker,
two roots, three flasks, a forge/pipe/vent/ice,400HP and Strength70. No finished
tools are granted. Afterwards all movement, choices, reads, saves and pickup are
native input. No runtime item/stat/position injection was used to earn a step.

Initial startup suffered a late script reload, so that root has no gameplay
acceptance. Later trials waited for current compilation and balanced an owned
assembly-reload lock around the session; every lock was released after cleanup.
The initial unneeded N key was ordinary southeast movement, not new-game
acceptance. The actual route returned by ordinary arrows to the forge.

| Tick | Observed result |
|---|---|
|50–70|All three native forge choices consume exactly the supplied3spikes/3clay/1cord and create nozzle2160, rod2161 and condenser2162.|
|100–130|Initial crowded camp placement refuses. Ordinary northward walking permits own hearth2148 at41,9; native stoke spends two timber and one strike; actor40,10.|
|130|F1/repeated gathering exposes token replacement. Two heat attempts refuse with fuel19.5 and ice unchanged. Saved all four files before cleanup.|
|130fresh→140|After the fix, both free reader pages preserve the selected token. Nozzle heat to41,11 succeeds: fuel19.5→14.25 includes5paid plus0.25ordinary burn; ice→water coating, all three physical flasks remain empty.|
|150–170fresh|One carried root is consumed into rod charge1. Ordinary walking to40,12; full fresh170 preserves charge1/root1 and selected state. Both reader pages remain free.|
|180|Native two-cell straight discharge to bare copper pipe42,12 spends charge1→0. This case proves delivery/reach, not target injury; the pipe has no creature occupant.|
|220→240|C-menu vent43,14 and tool menu each catch one water into exact empty flasks2154/2155. Source2159 capacity3, available3→2→1, LastTick220. Empty2156 stays empty; HP400.|
|300→310fresh|Normal return to own hearth. Grouped equal-state flasks display representative2154; one selected water pours northeast through ordinary cooling. Heat314.523926→269.533447 includes40cooling plus normal ambient decay; fuel10.25→10 is ordinary burn. One water remains in2155. Fresh310 preserves selected values.|
|310→330|Native menu cancellation remains free, then normal right/left walking and F5. Separate clean CoreFinal330 preserved.|
|340–450|Separate continuation loads the last supplied root. Ordinary westward walking/waiting meets natural Viper2115, HP8. Pouring the remaining vent water beneath it makes a one-unit physical pool2725; viper wet0.99/conductive. Its bite costs the player2HP, retained at398.|
|460fresh|Rod discharge into that water shocks/stuns the viper; shared turn rules kill it. Charge0, all flasks0, player398. Fresh460 retains corpse2727, waterpool2725volume1 and ground root2726; dead viper does not return.|
|470–490fresh|Native move onto the cell, G pickup menu selects root2726 while leaving the corpse, then ordinary load consumes the root into charge1. Fresh490: one stored discharge, no carried or ground root, corpse and one-water pool retained, vent stock1/3 Last220, HP398 and Wet0.98.|
|Final guard490fresh|A bounded compatibility continuation repeats only the affected normal ground pickup/load after the final payment/access guards. Native success and full fresh490 selected equality; all four files archived separately as FinalPaymentGuard490.|

The retained loot diagnostic records Viper2115's `DeathBeastT1`, one item, at
turn454. Source death processing creates loot before the corpse; the observed
root fits that existing path. The rod creates no root/refund. Individual weighted
RNG provenance is not claimed beyond this record and source-backed inference.

`Actual/fresh-comparisons.json` compares six fresh pairs. All selected values
match with UI input state excluded and fixture enumeration sorted by stableID;
only the first130 raw fixture enumeration order differs. This is not whole-world,
normal-save-restoration or compressed-byte equality. `checkpoint-integrity.json`
checks seven archived F5 checkpoints and all28 files before their original sessions
were cleaned up. Every prior root, save and build remains preserved.

## Screens and review limits

Five native game PNGs total. Accepted:02forge choices,04paid thaw,05stored rod
reader,06finite condenser reader. **03-nozzle-thaws-ice.png is the failed130
attempt despite its filename and is excluded.** The timed-out initial01 capture
produced no file. Root visually inspected04–06 without modifying them; no graphics
or icon-polish acceptance. No combat screenshot was added; that continuation is
recorded native state/actions/messages/loot diagnostic/fresh-save evidence.

Independent bounded current-source and recorded-play review: zero remaining
P1/P2. An old60°C predicate finding was explicitly withdrawn after rechecking
the current source and REDn/GREENo. Reviewer did not operate Unity or the player.
Final natural-root pickup/reload review separately verified all28 checkpoint
files, all six normalized fresh comparisons and the retained loot diagnostic.
Final payment-guard/native245 and current-source normal pickup/load/fresh490
were reviewed separately. Package review remains a separate delivery check.

This validates three bounded tool connections and an outside-machine/cross-system
outcome. Ordinary tool discovery, economic/combat balance, complete machines,
unloaded-zone/hardware performance and packaged gameplay remain open. The wider
failed suite40813total/40692pass/104fail/9inconclusive/8skip is not cleared by these
targeted checks. No FPS, Intel or controller claim. Graphics cancelled;
capstan/cultures parked; Deck deferred. Current Mac9a predates this batch until
the separate delivery receipt supersedes it.
