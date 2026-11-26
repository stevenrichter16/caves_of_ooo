# Peat Works field head recovery

An ordinary leather-bound forged weapon can yield its recorded head in one
pack action with a sound cutting tool carried or equipped separately. The
choice previews the lost haft, binding and improvements. A working forge
still yields both the head and haft. The cutting capability belongs to the
tool. The new head has the recorded type's ordinary workmanship and receives
none of the old assembly's enhancements.

## Actual GameBootstrap play

The seed 64 branch continued from [the earned Peat Works save](../FieldRigCompatibleClay20261125/README.md)
at tick 620, with 40 HP and two purchased measures of prepared lining clay.
The recorded mallet was `9354`, the separately carried dagger was `2698`,
and the adjacent field work rig `7420` was still broken. The player opened the
ordinary pack action list, read the choice and cancelled at the same tick.

| Tick | Result |
| --- | --- |
| 630 | One field cut spent the mallet and returned exactly one peat-packing mallet head `9355`. The same dagger, both clay measures and the broken rig remained. F5 and fresh Continue preserved the result. |
| 680 | The player moved to the surviving locker, took its two existing knotflax cords and closed it. |
| 1010 | The player walked to the collapsed drying frame and gathered its four timber lengths through the usual Harvest action. |
| 1020–1030 | One timber made a rough field haft `9360` and one cord made a plain binding `9361`. |
| 1030 | Away from a usable forge, normal pack crafting used the recovered head and the two new parts to assemble and equip the field-hafted cord-bound peat-packing mallet `9362`. |
| 1040 | One move with the mallet equipped, F5, and fresh Continue kept that exact equipped assembly at 40 HP, with one cord, three timber, both clay measures and the same dagger. |

The earlier working forge branch used both clay measures to repair the rig
and recovered one head and one haft. Independent comparison found the two
branches' canonical head bodies byte-identical. The field branch took a
longer walked route to gather replacement rough stock and kept the clay
available for another repair. Both outcomes are from the same earned save.
The navigation in this verification was guided.

- [Field and forge choices in the pack](01-field-choice.png)
- [Recovered head after Continue](02-recovered-head.png)
- [Rebuilt field mallet equipped](03-rebuilt-mallet.png)

## Verification

Compiling RED reproduced the absent field option. The final connected
EditMode run passed 197 cases with no compiler errors. Cases include exact
head payload and old station behavior, stale/replaced cutter refusal, output
merge rollback, and a failed cut returning the same independently injured
weapon. An independent review checked source, saved bodies, the archive
hashes and screenshots. Evidence includes the two fresh Continues in the
Editor. The broader inherited regression and the complete reclamation forge
acceptance gate remain open.

[Observations](observations.json) · [Checkpoint hashes](checkpoints.json)
