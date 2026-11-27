# Camp pickup preserves shared stock on refusal

November 26, 2026, America/Chicago.

A fish trap or settling tray reconciles the time it spent installed
before moving into the pack. The pickup transaction now owns that
catch and its exact source debit. A refused pickup, interrupted move
or failed combined pack-up restores the fixture, its catch clock and
every affected stock record. An untouched source stays untouched.
A scraper, tray or condenser draws from the same source budget. A
second actor can still work a different source during the first action.

## Native regression

Five countercases failed on the original code with the intended catch
and clock mutations. Two further cases reproduced late interruptions
after provisional custody moved to the pack. One of those also found
an independent trap could draw from the same pond. On the fixed
runtime, all 775 cases passed across 32 suites, with zero compiler
errors, failures, skips or inconclusive results. The separate scraper
countercase passed. It attempted both the same stock and a distinct
stock during tray pickup, then checked that the independent scrape
kept its debit and reagent after the pickup rolled back.

The pinned wider regression contains 42,389 rows, with 42,226 passes,
146 failures, nine inconclusive results and eight skips. Of the 146
failures, 75 match historical case names. The other 71 resolve to 27
cases covered by later production fixes, 25 missing scratch inputs
and 19 obsolete fixture assumptions. Focused receipts cover those
follow-ups. In this run, 9 of 14 historical controls passed. Three of
the five reproduced failures expect a completed throw to undo its
landed item and bait state after a late callback. The current command
seals on launch, so those controls need separate prelaunch rejection
and committed-partial assertions. The other two use a Groundwire
Screen without the required throwable handling data, so their
AfterUnequip hook never runs. The later
[test-only follow-up](../CurrentNativeRegression69AE7EE85/README.md#historical-throw-and-equipment-follow-up)
checks the correct prelaunch and postlaunch behavior.
The pinned receipts now reconcile all I–R case names. A whole-process
regression on the final runtime remains open.
[Pinned wider comparison](../CurrentNativeRegression69AE7EE85/README.md)
and [exact test receipts](receipts.json).

## Actual GameBootstrap play

- **Shoreline fish trap.** Started from the retained ordinary Stump
  save, traveled to Morrowfast and bought the existing mended trap
  for 35 drams. Returned to the cascade and set the trap at 45,12.
  Ordinary wait and travel produced a visible one-fish projection.
  At tick 8450, an isolated comparison set only the carry cap to
  the pack's current 38 weight. Before the fix, the menu refused
  pickup but banked a fish and created a 6-of-7 pond ledger. After the
  fix, the same refusal left the trap installed with raw held 0,
  progress 0 and no pond ledger. The turn stayed at 8450. Restoring
  the cap to 150 and choosing the same row moved exact trap 71547
  into the pack with one fish and debited pond 58374 once. A nearby
  sky-sari hit the player on the successful turn, from 38 to 20 HP.
  A fresh Continue at 8460 retained the exact trap and 6-of-7 ledger.
  The [inventory reader](01-trap-carried.png) showed the held fish.
  This branch stopped with the catch in the packed trap. Eating and
  cooking have [separate ordinary-play evidence](../RoastedLakePerch20261126/README.md).

- **Hearth and still.** Loaded the retained supplied camp at 2470.
  The [hearth reader](02-hearth-before-still.png) showed 7.75 fuel and
  about 31 turns. The still's live row named input flask 2235 and
  receiver 2236. It spent four brine into two clean water, discarded
  concentrate and used five fuel plus one turn of burn. The
  [resulting reader](03-hearth-after-still.png) showed 2.5 fuel and
  about 10 turns. The still immediately disabled the remaining mire
  work for short fuel. A fresh Continue at 2480 matched both exact
  vessels, fuel 2.5 and 343 HP. This check used already-installed
  fixtures and did not test new kit acquisition.

- **Cinderhold forge.** Loaded the older ordinary Cinderhold save at
  4350. Processed one carried knotflax cord and one leather binding
  into a guard lashing, moved one tile, and refitted the same
  Cinderhook axe 13682 at the fixed forge. The old plain binding came
  back as item 13695. Equipped the result and saved. A fresh Continue
  at 4370 retained the exact weapon, recovered binding, hit bonus
  0 and penetration bonus −1. The [actual inventory reader](04-cinderhold-axe-reloaded.png)
  showed both combat values on separate lines. The test did not
  exercise a combat hit.

Independent source review found no blocking issue in the shared-stock
paths. Independent gameplay review read the raw save data, checked
all 20 checkpoint hashes and four screenshots, and matched the
reported reload outcomes. It also repeated the save and worktree
protection check.

All three paths used the Flat 16×24 editor on Apple Silicon. This is
bounded Editor gameplay evidence for the affected fixture and two
machine uses. Whole-machine acceptance, packaged gameplay and hardware
performance are separate.

[Play observations](observations-live.json) and
[save and worktree protection](protection-verification.json).
All 876 normal quick-save files across the Editor and player roots
and 87 previously modified tracked files matched their before hashes.

## Current Mac package

The universal package at
`/Users/steven/coo-verification/builds/mac-fbc34ce26-20261126/CavesOfOoo.app`
was built from runtime `fbc34ce263cbbea9fb70dda3c654462a35d8c06b`.
All 32,252 committed build inputs matched before compilation. Unity
completed the build in 23.45 seconds with zero errors and five warnings.
All 186 app files and 186 previous app files matched their hashes.
Each of the six Mach-O binaries includes Apple Silicon and Intel.

The package completed GameBootstrap on Apple M5 with 2,673 entities.
All 876 normal quick saves matched again. The nine preferences changed
by startup were logically restored, and the test player was stopped.
Packaged gameplay and operation on physical Intel hardware remain open.
[Apple Silicon startup receipt](mac-startup-verification.json).
