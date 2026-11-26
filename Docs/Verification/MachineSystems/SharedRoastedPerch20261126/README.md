# Share a roasted lake perch

November 26, 2026 UTC. Guided seed 64 play in the Apple Silicon Unity Editor,
starting from the [earned Second Bowl meal](../RoastedLakePerch20261126/README.md).
The player can eat one portion or give it to an adjacent injured companion.
Sharing uses the existing care action and the meal's 4–10 HP healing roll,
capped at the recipient's maximum. The party and inventory rules recheck the
exact selection before payment. Existing prepared meal and bleeding effects
have separate rules.

The player recovered a loose tool and wet-reached the original axe head in
Cinderhold. The smith paid 16 and 15 drams. A watering book and gladroot seed
stack brought the purse from 15 to 61 drams total. Orrit sold the original
tongueless bell for 58. Equipping it granted Recruit. The player reached
Gleaners' Glade and recruited Kesset Rillpost after the warden took damage
fighting local marauders. The fish, bell, recruit and injuries came from
ordinary gameplay. A dog and the warden each initially refused recruitment.
An abandoned earlier branch died in the clay field while raising funds.

| Native observation | Clock | Warden HP | Player HP | Roasted perch |
| --- | ---: | ---: | ---: | ---: |
| Open and read the exact Share row, then return | 3150 | 22/40 | 39/40 | 1 |
| Commit the share | 3160 | 28/40 | 39/40 | 0 |
| Fresh Continue from F5 | 3160 | 28/40 | 39/40 | 0 |

The Share reader names the recipient, quotes one item and 4–10 HP, and
shows 22/40 recipient HP. Independent review verified the screenshots,
source, all eight save hashes, preserved pack contents, and exact party
membership and fish payment. The final saves' backups match the pre-action
save. Checks against the existing baseline found zero mismatches among 861
normal save and preference files and 86 tracked foreign files.

[Share choice](share-choice.png) · [Reader](share-reader.png) · [Result](share-result.png)

The native feature RED had three expected missing-share failures and two
passing controls. The reader RED reproduced the incorrect self-Eat text.
The final connected runs passed 252 of 252 native cases, including exact
stack payment, stale selection, rollback, preparation and bleed preservation,
and the ordinary Eat preview. The live proof covers this meal and this
recipient. Packaged verification and wider playthrough comparison remain open.
