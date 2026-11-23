# Corrected a9 full regression assessment — November22/23,2026

The raw native full run is **failed**, not a current whole-game acceptance:
40,813 total,40,692 passed,104 failed,9 inconclusive,8 skipped; zero compiler
errors. Native case interval00:50:24–03:52:16UTC. All3,887 pinned source/test/config
files remain exact in clone2. This includes preserved foreign changes; it is not
a clean committed-tree export. New well/clay/cooling/counting-family discovery
was checked (85 names matched the recorded family filter). Later UI tests/code
were not part of this pinned a9 run.

The user explicitly authorized concurrent UI work before this run finished.
UI RED overlapped its tail; the original run-start noConcurrent flag is an
initial plan, not proof of the whole duration. clone2 was never resynced or
edited during/after the run. Source/test pin verification is retained.

## Comparison and independent reruns

Compared names AND whitespace-normalized failure messages to full_s9(39,500,
91 failures) and inherited fullrun_h(40,376,92 failures). Thirteen failure names
are absent from both. One old fullrun_h W7 case now passes. Of91 common failures,
full_s9 has one changed terrain message adding HushStripLaid, already present
in fullrun_h. The six changed fullrun_h messages differ only in clone paths
(five CurationAnnex source-directory failures and one Voxel constructor failure).
No graphics work was resumed to hide those existing gaps.

All13 new-name cases were rerun in separate fresh native processes on clone4,
using the original clone2 snapshot copied read-only through the canonical sync.
Every process discovers exactly1 case with zero compiler errors. All3,887 pins
match before and after. Ten water cases fail alone, one previously known well
case fails alone, and the fuel/journal cases pass alone. See isolated/results.json
and retained raw XML/logs. Root held this owned orchestration between runs while
capturing UI, then resumed it; no shared in-process fixture state was reused.

## Corrections and remaining limitation

Ten DensityEverydayAdversarial water cases construct an actor and pool without
PhysicsPart. Current shared admission refuses them before their callback,
receipt/reentry/rollback assertions. Correct the fixtures with owned actor
physics and non-solid ground-pool physics; remove the now-duplicate rest setup
physics. Keep all resource/rollback assertions and production admission intact.
These cases now exercise the intended behaviors.

PouredFuelCombustion's callback-order test steps onto oil with40% slip chance,
then assumes it remains at the ignition cell. It passes alone, so the original
failure's exact slip is not proven by XML. Its fixture now scopes/restores an
explicit no-slip RNG and asserts actual occupancy before ignition. Real entry,
contact, depletion, damage-callback and reentry checks remain. This controls an
unrelated random movement effect rather than weakening combustion behavior.

The journal wrapping test passes alone. In the full run the wrapped MORROWFAST
assertion passed but a hardcoded[Tab] footer assertion met the legitimate
controller footer. No input-device leak is proven. The fixture now explicitly
scopes/restores devices and tests BOTH keyboard[Tab] and controller[Y] navigation
alongside the long objective. No production journal behavior changed.

The corrected three classes, UI/examine/comparison integration and20 new UI
cases pass in one bounded native run: **471/471,zeroCS**. The exact final result
is [GREENf](../InventorySections20261122/item_sections_regression_green_f_20261122.xml).
This is not a second full-suite run; no inferred new full-run failure count is
reported.

The remaining WellDrawRemainsProtectedFromUnrelatedGroundSpill failure is already
reproduced alone on committed ef5 in
[the retained baseline receipt](../WellFlasks20261122/README.md), identical message.
Its success expectation conflicts with existing source-safety refusal beside
acid. This older policy/test conflict remains **open**; neither production safety
nor the assertion was silently weakened. It is new to these two full-run failure
name lists, not proven new to a9. The other91 baseline failure names remain open.

Independent bounded source/evidence review and raw comparisons are retained.
No runtime water/combustion/journal balance or save behavior changed in these
fixture corrections. Native current UI qualification is separate; all12 complete
machine/full-delivery gates remain open. Old accepted targeted branches/builds
were not repeated as new qualification.
