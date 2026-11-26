# A rough field haft can brace Sella’s dressing bench

November 26, 2026 UTC. Sella’s covered dressing bench accepts two salvaged
timber, or one timber with one sound loose rough field haft. The repair
uses plain timber first. A valid haft stack can supply one unit when
one timber is missing. The shared bill presents both choices in Examine
and the selected repair reader. Sella tells the player about the spare
haft when asked about supplies. Existing saved benches show the current
bill and keep their route guidance.

## Actual play

The guided seed 64 journey used the [earned shelter save](../SoddenTreatmentChoices20261125/README.md)
at tick 2000 with four Peat Works timber, two pads, two cords, 50 drams
and 40 HP. The player used the existing inventory, trade and world menus.

| Clock | What happened |
| ---: | --- |
| 2120 | Sumphold’s Brinna bought exactly two of the four timbers for two drams. The original stack kept two units, and Brinna’s stock gained a physical two-unit stack. |
| 2170 | Back at the shelter, one timber became rough field haft `12517` through ordinary pack preparation. The player carried the one remaining timber. |
| 2260 | The bench’s repair reader and Sella’s supplies dialogue both explained the mixed bill. Reading and closing the choice cost no time or materials. |
| 2270 | One normal repair spent the timber and haft. Bench `5333` was working with the same keeper, and fresh Continue preserved the repair. |
| 2280 | The existing service produced one dressing for one pad, one cord and two drams. Sella held both drams, and the player kept one pad, one cord and the resulting dressing on fresh Continue. |

The earlier ordinary two-timber repair uses the same service for the
same fee. The mixed bill gives a spare prepared piece a second use.
Shaping a piece for this job still costs one timber and an action.

- [Two timber sold in Sumphold](01-timber-sale.png)
- [Mixed bill in the bench reader](02-mixed-repair.png)
- [Paid dressing after the repair](03-paid-dressing.png)

## Verification

The compiling RED reproduced two missing mixed paths. The initial connected
run passed all 206 cases. The final run passed 280 cases, including an
added control for the stale saved sentence. Five broader equipment-art
checks failed with the same names and results as the November historical
full run. The repair controls cover the priority of two timber,
exact consumption from a stack, refusal of two hafts alone and rejection
of altered, assembled and equipped inputs. A late interruption restores
the payment and preserves a separate actor injury. The repair and
preparation each survived a fresh Editor Continue. Independent review
checked the source and exact archive state. [Checkpoint hashes](checkpoints.json)

The live branch qualifies actual preparation of one dressing. The
earlier trial contains the treatment application comparison. Packaged
gameplay and the inherited full regression remain open.
