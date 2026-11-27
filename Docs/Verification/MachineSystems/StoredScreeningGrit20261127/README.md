# Carry the clay and leave the grit in camp

November 27, 2026, America/Chicago.

A sound carried sieve can process a single mixed packet from an adjacent,
installed, accessible camp box. If the mesh retains one coarse grit, the
player can choose to take the fine fraction and leave the coarse fraction
in that box. For dry clay spoil, this needs two pounds of pack room.
Packing both fractions still needs three. Ordinary portable screening can
also leave grit on suitable ground.

The selected box supplies one exact packet. The resulting grit uses its
vacated entry or joins an existing compatible stack. The shared receipt
claims the actual recipient and rechecks the pack, box and carried sieve.
When a late change interrupts the action, rollback retracts the exact grit
output before refunding the packet. If the original storage entry is
unavailable, the refund uses the existing ground fallback. Independently
moved or altered output can prevent a refund to conserve the batch.

## GameBootstrap play

1. Continued seed 65 at the western Morrowfast allotment at tick 14900.
   The retained hand sieve was already in the pack. The private run
   supplied one empty camp box 9286. Ordinary harvest at 14940 took
   packet 9287 from dry sorting heap 7197.
2. The allotment biome prohibits installing camp kits. Traveled one
   world cell north to Stump and walked to the camp ground at (43,13).
   Set down the box east and put the exact packet inside. For the
   capacity comparison, set the private hard pack limit to 30 lb
   against the actual 28 lb carried. The three-pound route was disabled.
   [Two-pound sieve choice](01-two-pound-sieve-choice.png)
3. Selected the new route. One action reached tick 15060 and filled the
   pack to exactly 30 lb with two fire clay in stack 11850. The same
   box held exact one-unit grit 11852 and the packet was spent. The
   player's ordinary Open menu exposed the grit without taking it.
   [Grit still in the box](02-grit-left-in-box.png)
4. Returned to the allotment and used both new fire clay measures to
   patch existing catch-well 7184 at tick 15330. The well offered Draw
   water, and the ordinary drink action succeeded. This well uses
   the existing legacy unmetered drink behavior.
   [Clay patch quote](03-clay-patches-catch-well.png)
5. After fresh Continue, the same well stayed repaired and its water
   action remained enabled. Ordinary travel back north found the same
   box with the same grit. The sieve, 28/30 lb pack and 40 HP persisted.
   [Reloaded water access](04-reloaded-water-access.png)

## Verification

Seven compiling first-run failures reproduced the missing split choice.
A later native case reproduced a packet returned to a detached
container list. The same source refund now falls back to the physical
tile when the list or free slot has changed. Final targeted Unity tests
passed all 374 cases with zero errors or skips, including current
compatible merges, label separation, exactly two pounds, full boxes, and
late lock, slot, list, quantity and custody changes.

Independent source review found the detached-list case and checked the
exact merge recipient. Independent gameplay review checked all four
screenshots, both raw checkpoints and the fresh Continue observations.
The two private four-file checkpoints and all 876 ordinary quick-save
hashes matched. All 87 pre-existing modified tracked files also matched.
The Editor left play with normal input and save access. This was guided
Flat Editor play on Apple Silicon, with the box and hard carry limit
controlled.

[Recorded observations](observations.json) · [Native and save receipts](receipt.json)
