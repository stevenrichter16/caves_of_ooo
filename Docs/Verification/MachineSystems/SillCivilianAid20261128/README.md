# Treating the wounded scribe in Sill

The ordinary Sill scribe can receive a carried medicine that helps his
current condition. Examine shows the wound. With the player beside
him, the world actions menu offers one dose when he is willing and
there is no active nearby hunt targeting him. The native tonic applies
its usual healing. A successful handover takes one turn and spends
one unit from the existing stack.

## Ordinary play

- At seed 65, tick 2220, Kesset already had a wound from a sari-snake.
  The replay began with the existing starter dagger and two doses of
  healing tonic. Two diagonal steps brought the traveler beside the
  snake. Flurry killed it. The next step put the traveler beside Kesset,
  with 13 of 20 HP. The other snake had been fighting the weaponsmith.
  The original menu offered chat and Examine at that safe position.
- In a separate replay while the snake was still pursuing Kesset, the
  native menu offered chat and Examine. Examine showed the wound.
  Both doses stayed in the pack, and opening and closing these views
  left the tick unchanged.
- In the earned calm position, the revised menu offered
  **treat with healing tonic (1)**. One action took Kesset from 13 to
  20 HP and left one dose in the same stack. The player's HP remained
  at 40. The log named Kesset and recorded seven points healed.
- A fresh GameBootstrap Continue preserved Kesset at 20 HP and the
  same one remaining dose. The world menu at full health offered chat
  and Examine. Inspecting the menu spent neither a turn nor medicine.

[Active pursuit](urgent-menu.png) · [Wound](calm-wound.png) ·
[Treatment choice](calm-offer.png) · [Healing in the log](care-complete.png) ·
[Full health after Continue](fresh-full-health.png)

## Source and native checks

The settlement binder uses the canonical Sill address and the existing,
named Villagers scribe with the ordinary local conversation. Generation
and save rebuild both attach the same existing aid part. Other feasible
aid remains on its existing providers. The nearby danger check uses the
first current combat hunt and the actual recipient, within three cells.
This keeps an unrelated fight from blocking aid.

The native tests first reproduced the missing registration, the offered
treatment during pursuit, and lost payment on invalidation. A second
set reproduced the wrong result for stacked hunts and payment that was
moved independently. The final tests cover a changing treatment payload,
an incapacitated recipient, wounded and full-health patients, a new hunt
during the action, an unrelated or abandoned hunt, independent injury to
the donor, relocation of a one- or two-unit stack, and nested self-use
of the same stack. The final inventory check verifies the exact dose
and patient before payment is committed. The focused run passed 505 cases.
The explicit save-contract regeneration helper was skipped.

Unity 6000.3.4f1 Editor play used private copies of earned saves.
The run exercised the normal world menu, native movement, attack and
Continue. Observations record the original and updated actor state,
the paid amount, the save hashes, and the focused regressions.
No performance benchmark was run for this interaction.

[Observed state and native results](observations.json)

## Mac standalone

The current universal Mac app includes this change and the cart harness
salvage from the preceding slice. The dedicated build compiled 32,304
pinned inputs with zero errors and four warnings. All six native
binaries contain arm64 and x86_64. Gameplay ran on an Apple M5.

From the earned calm checkpoint, the standalone menu showed Kesset at
13/20. One use logged seven points healed. A Quick Save and a separate
standalone launch brought him back at 20/20. The Look cursor then
identified him at his new position. Independent review checked the
visible health and the package architectures.

[Natural treatment choice](mac-care-offer.png) · [Paid healing](mac-care-complete.png) ·
[Scribe after a fresh standalone Continue](mac-care-continue.png) ·
[Build and preservation record](mac-package.json)
