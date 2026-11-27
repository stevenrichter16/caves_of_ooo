# Mire readiness at Sill's public still

November 27, 2026. Native GameBootstrap, seed 65, Flat16x24 Editor play.
The shared heat checks now drive both the still's Examine text and its
interaction rows. Steady natural heat can settle mire. Baking one fire
clay requires eight mire from the selected flask, a lit fire that can
spare ten fuel and room for the output. A fire with five to nine fuel
can settle a flask and shows why it cannot bake. The heat named in a
preview is the currently eligible source. The action checks live
conditions again.

## Ordinary material path

The player Continued the retained [finite pool refill](../FiniteFlaskPoolReceipt20261127/README.md)
checkpoint at tick 2570, with empty flask 9873, brine flask 10242 and
the already purchased torch. Ordinary travel entered Peat Works, and
the player filled flask 9873 from natural pool 13481 at (34,8). Its
count fell from 60 to 48 while the flask rose from zero to twelve mire.
A sari-snake fight raised the player to level 2 and later left 23 HP.
The Sill log also records a scribe killed by a sari-snake on entry.
After returning to Sill, the player set the exact owned torch on dry
ground at (38,12), beside the public still 9990. The cold menu and
Examine both required a lit fire before this drop. Both branches
began at the saved worksite at tick 2990 with the same two flasks,
23 HP and about 42 loose torch fuel.

| Branch | Result | Fuel after work |
| --- | --- | --- |
| Settle to water | Twelve mire became three water in flask 9873. | 36.7 after five process fuel and about 0.3 normal flame burn. |
| Bake and settle the remainder | Eight mire made fire clay 15169. Four mire remained in flask 9873, then settled to one oil. | 31.7 after baking and 26.4 after settling. Each action also spent about 0.3 normal flame burn. |
| Use the remainder away from the still action | The player extinguished and retrieved the same torch, then used the one oil from the same flask. | The preview quoted 26.4 to 50/50. The dark torch reached 50 and the flask emptied. |

Fresh Continue from `Water3000` preserved the water and the exact loose
torch's fuel and location. Fresh Continue from `ClayOil3040` preserved
the one fire clay, empty flask and exact refilled torch in the pack.
The separate twelve brine remained in its own flask in both branches.

## Verification and scope

The five original new cases reproduced the readiness bug on the old
source. A sixth case covers hidden fire privacy. The final focused run
passed 174 Unity EditMode cases, including stationary and camp setup,
short and exhausted fires, natural heat, atomic clay output, brine
distillation and oil reuse. The visible heat preview identifies a
currently eligible source and displays its balance only when visible.
Independent reviews checked the shared predicates and hidden-source
behavior, the exact material and torch identities, and all twelve
checkpoint file hashes against the native play record.

This live route used the public Sill still. A placed camp still by
a vent was exercised by the native tests. There is no fresh ordinary
vent or camp-still route in this receipt. The paid alternatives were
compared from one seed and one worksite. The guide's final wording
clarifies that settling uses the whole selected flask. There is no
recoverable residue from that action. The four measures left after
baking are still raw mire. The editor briefly lost its session during
script reload before the scored run. A later play slowdown recovered
without changing the test save. After both branches, 87 unrelated
tracked files and 876 normal Quick save files matched their prior
hashes.

- [Playtest state, menu rows and save pins](observations.json)
- [Universal Mac startup receipt](mac-startup-verification.json)
- [Draw at the natural pool](captures/01-natural-mire.png)
- [Cold still Examine](captures/02-cold-still.png)
- [Paid still Examine](captures/03-paid-still-examine.png)
- [One oil refills the same torch](captures/04-refuel-same-torch.png)
