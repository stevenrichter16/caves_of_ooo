# One board from Sumphold's public crate

November 27, 2026, America/Chicago.

Beside a released public crate, a player can take one board from an eligible
stack of salvaged timber. The menu shows its weight and the quantity left.
The crate keeps the original stack and its other contents. Open and Haul still
work. This extends the existing stock and field recipe.

The choice binds the exact crate, stack, positions and cargo load. A change
in access, weight, contents, or the selected board can refuse it. One action
debits one board and places it in the pack or merges it into compatible stock.
The transaction rechecks the actual recipient and preserves valid labels.
If a later change interrupts the transfer, the crate refunds only a board
the destination can retract.

## Actual GameBootstrap play

1. Continued the retained seed 65 Duelist from Sill at 980. Ordinary travel
   reached Sumphold at 1100. Thirty-three steps reached the eastern crate
   at 1430. Crate 13010 weighed 74 lb and contained original stack 13011
   with two one-pound boards.
2. For a controlled capacity comparison, set the private pack's hard limit
   to 22 lb while it held 21 lb. The ordinary Open and Take path refused both
   boards as too heavy without advancing time or changing custody. The
   separate one-board action was still available. [One-board offer](01-one-board-offer.png)
3. That action took one turn to 1440. The same crate weighed 73 lb, held the
   original stack with one board, and read `1/24 lb of goods`. The pack was
   exactly at 22 lb. [Crate after withdrawal](02-original-board-left.png)
4. The player moved around a sari-snake and shaped the exact carried board
   into rough field haft 13034 with the ordinary inventory action. Fresh
   Continue at 1500 kept the 22 lb pack limit and weight, the same haft,
   the original board still in the crate, and 33 HP. Normal wildlife dealt
   seven damage across the withdrawal and retreat. This run granted no
   equipment, stock, or game time.
   [Shape haft](03-shape-haft.png) · [Reloaded haft](04-reloaded-haft.png)

## Verification

Seven first-run failures reproduced the missing choice. The final suite
passed 344 native cases with zero compiler errors or skips. It covers
unchanged whole withdrawal and hauling, a compatible merge, different
labels, no spare capacity, grip, lock, moved and replaced source, and
two staged changes at the point of commitment. The changed-recipient
case keeps two total boards even when a raw late change prevents the
usual destination reversal.

Independent source and gameplay reviews checked the records and exact
contents in both raw saves. All eight checkpoint hashes, all 876 normal
quick-save hashes, and all 87 pre-existing modified tracked files matched.
The Editor returned to Edit Mode with its ordinary keyboard and save root.
This is one Flat Editor route on Apple Silicon with a controlled pack limit.

[Recorded observations](observations.json) · [Native and save receipts](receipt.json)
