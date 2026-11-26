# One sealed field satchel on the Concord carriage

A carried field satchel can travel as one parcel from Cinderhold to the
Last Counter or the reverse. The existing fee counts the full load and
the receiving pack needs room for that weight. The confirmation lists
the contents, fee and delivery time. Ordinary sale still requires empty
storage. The clerk applies existing freight restrictions to each
enclosed item. The seal keeps the same bag and goods together.

## Actual GameBootstrap play

The branch began with the paid Sill satchel at tick 160 and 31 drams.
The original storage fixture supplied comparison equipment. The ordinary
starting meat and tonics were available in that pack. Raw receipts pin
the same contents and named owners at each step.

| Tick | Result |
| --- | --- |
| 160 | The player unpacked two dried meat `2713` from the other satchel and stowed the meat and both healing tonics `2712` in purchased satchel `7341`. The load was 7 lb and the pack stayed 38 lb. |
| 420 | Native travel reached Povra at Cinderhold after wildlife damage reduced HP to 30. The quote listed both stacks, 7 lb, four drams and three days. Back kept the purse and cargo. Sending left 27 drams, a 31 lb pack and one exact parcel due at tick 4020. Fresh Continue matched. |
| 740 | The Last Counter showed the parcel as in transit and gave no collection choice. Waiting beside the counter led to a fatal sky-sari attack at 760. That branch was stopped. |
| 4020 | A separate Continue from the earned arrival at 670 kept the same in-transit bag. After ascending, 334 ordinary waits on the world map took the clock from 680 to 4020 at 30 HP. |
| 4090 | The player descended, walked to Darret and collected the one parcel. The same bag and both stacks returned once. The pack returned to 38 lb and the purse stayed 27. Fresh Continue preserved the bag, and the collection choice was gone. |
| 4110 | After stepping back to the world map, the player opened the bag inline and drank one delivered tonic. HP rose from 30 to 40. One tonic and both meat units remained in the exact bag. Fresh Continue matched the spent dose and the empty carriage record. |

This is a guided inter-post route. The two loose stacks would cost six
drams under the same rates, as covered in testing. The paid satchel was
already owned. Traveling with the same supplies in the pack remains a
practical free alternative. The service uses the established carriage
ledger and clock.

## Verification

The compiling RED reproduced the missing packed offer and passed the
ordinary flask control. The first implementation run reached collection
and exposed an incorrect envoy blueprint in the test fixture. The
corrected final run passed 141 cases with no compiler errors. Coverage
includes ordinary freight, whole-weight refusal, repeated collection,
stale same-weight replacement, an enclosed restriction added during
another item's callback and the ordinary loaded-sale veto. Independent
review checked source and the recorded gameplay.

- [Departure quote](01-sealed-bargain.png)
- [Arrival at the Last Counter](02-ready-to-collect.png)
- [Contents returned in the inventory](03-returned-contents.png)
- [Observations and exact archive hashes](observations.json)
