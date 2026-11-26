# Current native regression in shorter runs

Unity discovered 42,389 current EditMode case names for runtime
`69ae7ee85`. The nine planned filters cover those names exactly once
and preserve the earlier complete-run results. The immutable discovery
list is at `/private/tmp/coo-native-discovery-69ae7ee85/cases.json`.
Use the pinned scratch clone at
`/Users/steven/coo-verification/clone-native-69ae7ee85`.

The first filter produced exactly its 527 expected names. Two
clay-baking assertions failed again when run alone. The fixture added
a creation-callback part to normal clay, so the stack policy correctly
kept the outputs separate. The rollback fixture's callback then failed
before adding its two independent units. Removing that probe from the
merge tests restored the intended ordinary input. The other tests
still exercise rejection and re-entry through the creation callback.

All 527 cases passed after the fixture correction, with no compiler
errors, omissions or skips. The receipt pins the one changed test
file. The previous untouched 525-pass result and isolated repeat
remain available. Production behavior and the Mac runtime did not
change in this follow-up.

The second filter ran all 9,373 expected B–E cases, with 9,342 passing
and 31 failing. Twenty-two failures match the historical baseline at
the same case names. Eleven previously failing names now pass. All
nine additional failures ran again in an isolated filter. Three passed
after the scratch clone received the pinned Codex sources, connectivity
sample and loot census input. These were missing files in the clone.

Six failed again. Four now exercise the intended kitchen clock guard,
current armor labels, deterministic reader cleanup and the alternative
timber repair bill. The kitchen test checks exact stock and payment
rollback, followed by a successful retry. Two related assertions found
missing level and experience data on four laborer blueprints. Newly
created Gleaners and Wayside porters, the supplier and kitchen hand now
use level 2 and 9 XP, the values of comparable villagers. Previously
saved NPCs retain their serialized stats. A follow-up native run on the
corrected sources passed all 243 selected cases, including all six
reproduced failures. The repair-bill test has a new case name because
its expected text is part of its parameter list.

The F–H filter matched all 12,889 expected names. It produced 12,863
passes, 11 failures, 9 inconclusive results and 6 skips. Nine failures
match historical names. Both additional failures reproduced in isolation.
The opening census now checks the actual glyphs of the five public freight
owners instead of requiring an opening atlas tile for them. The restock
fixture explicitly uses the legacy one-entry table, and the opening dealer
fixture checks the two separately supplied offcuts in the weaponsmith's
shelf. Both original failure cases and the adjacent controls passed in a
398-case follow-up. These test corrections did not change production art
or merchant behavior.

I–R produced 7,625 rows with 7,587 passes and 38 failures. Strict name
comparison found 7,624 distinct names. Two passed reagent fuel cases with
different runner IDs shared a rounded float display name. Discovery had
given the second a generated suffix. The test now names the two inputs
explicitly, preserving their literals and assertions. The original
report remains ambiguous by name.

Eleven failure names are historical. Twenty-five provisioner cases
found one shared defect: the ordinary empty satchel refused the merchant
fingerprint, preventing a new provisioner profile from publishing.
Two other cases needed fixture updates for the existing finite sponsor
budget and the scribe's actual saved stock clock. The corrected sources
passed a 246-case follow-up and the Sill purchase was exercised in play.
The entire I–R band was not rerun after the corrections.
[Provisioner playtest](../ProvisionerSatchel20261126/README.md)

The S filter, excluding Spread, matched all 3,850 expected names, with
3,809 passes, 39 failures and two skips. Fifteen failure names match
the historical result. Twenty additional cases could not load the
tracked Rim Forest input missing from the scratch clone. Three
allotment render checks reported owners with ordinary glyphs and no
matching Previous-mode model. A custody check counted the Stillleaf
inventory-icon index as a blueprint supply reference. Isolated reruns
of those discrepancies remain open.

Spread A–G matched all 1,385 names, with 1,383 passes and two failures.
Two historical geometry cases now passed. The seed-keeper purse still
differs from its historical fixture. The additional failure could not
load the tracked creature appearance requirements in the scratch clone.
That exact file and the Rim Forest input were restored from the original
commit for a separate rerun.

Three filters have yet to run. Keep the I–R name discrepancy and the
separate whole-process verdict open.
[Case counts and receipts](coverage.json)
