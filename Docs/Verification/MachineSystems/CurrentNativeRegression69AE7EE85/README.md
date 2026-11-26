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

Keep later filters on the same pinned clone and compare actual names
to the discovery list. Eight groups remain. Even after those complete,
the partitioned run and the whole-process result have separate gates.
[Case counts and receipts](coverage.json)
