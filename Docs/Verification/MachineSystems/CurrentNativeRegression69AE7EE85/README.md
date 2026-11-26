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

Keep later filters on the same pinned clone and compare actual names
to the discovery list. F–H is running next on the pinned discovery
snapshot. Seven groups remain. The partitioned suite and the whole
process have separate gates.
[Case counts and receipts](coverage.json)
