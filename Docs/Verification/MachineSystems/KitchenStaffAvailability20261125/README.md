# Paid kitchen work waits for the cook

A cook who cannot act declines new commissions. When that happens during
a paid job, the pan keeps the same ingredients and shows that work is
waiting. Once the cook recovers, the original job resumes. A locked pickup
board likewise holds the work until the board reopens.

The commission checks the current cook, pan, pack and visible pickup board
again before charging. The tray keeps each exact portion separate, so a
refused order can return those portions while preserving an independent
injury, a replacement component or an ordinary death spill. Both the
existing stacked-grain saves and the new three-entry escrow load through
the same saved fields.

## Native and independent verification

The compiling RED run passed 13 cases and reproduced four failures. It
caught an unavailable cook taking a new order, a stunned cook finishing
at the deadline, and two late-start failures involving real movement,
injury or death custody.

The final connected run passed 160 of 165 cases, with zero failures,
inconclusives or compiler errors. Five existing allocation probes skipped
because this Unity runtime cannot measure the required managed allocations.
The run also covered a late board lock, a duplicate pack listing, a replaced
physics component, and save/reload of three separate ingredient objects.
An independent source reviewer checked the scoped refund and settlement
again after the follow-up cases. An independent archive review matched the
original paid job and paused save.

## Actual GameBootstrap interruption

The trial continued the earned seed 2 job from
[the local sorting comparison](../KitchenOrdinaryContrast20261125/README.md).
The test supplied only the temporary stun on the existing cook at tick 1680.
Ordinary player input advanced time and opened Examine and the pickup board.

| Tick | Observed result |
| --- | --- |
| 1580 | The player had already paid Orven two drams. Job 1 held the two grain and one pulp and was due at 1690. |
| 1690 | Ordinary waiting reached the due tick. Examine showed the cook was unavailable and the two exact escrow stacks remained. The board was empty. F5 and fresh Continue retained the same paid job and the cook’s remaining four-turn stun. |
| 1700–1730 | Native waits left the order paused through 1720. The cook recovered by 1730 and the board held the exact single meal. Player and cook purses stayed at one and 37 drams throughout. |
| 1740–1760 | The native pickup board transferred exact meal `11515` to the player at 1740. Eat consumed it at 1750. At 1760 the tray and board were empty and the ordinal and purses were unchanged. |

The native start and late-refund controls ran in EditMode. This controlled
GameBootstrap branch exercised the saved paid job. The full Mac regression
and a new Mac package remain open.

[Waiting with the ingredients held](01-waiting.png) · [Recovering the
finished meal](02-recovered.png) · [Exact evidence](receipt.json)
