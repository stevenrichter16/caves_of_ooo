# F8 native entry, purchase and cached return

Run `3dc45efa833d4801a089292e24a6fd97` completed on the first native attempt: **8/8 checks, zero failures, 11 paid inputs, 7.0418848 seconds**, original player ending at40/40HP. This receipt was independently checked against the complete report and all five captured frames. No further observer change or replay is needed.

The fixed source was seed3 / `Overworld.11.7.0`, chosen before Play from the retained bounded source census. The destination was initially uncached. Ordinary New Game, map ascent, three north map steps and descent produced the real traveller `traveller:3:Overworld.11.7.0`. Fresh `TravellerEntryRoll: placed` and `RoadsideExchange: committed-in-place` records occurred at tick80. The observer did not create or transfer the merchant or modify its stock, source roll, position or purse.

Current native dialogue identified the route between Tine and Sill and offered Trade. The native purchase transferred the same one-unit Torch `6848` for9drams: player50→41, merchant500→509, with a fresh actual `Bought` record and exact combined item ownership/quantity checks. Four ordinary ground steps reached the speaker. Real map exit and descent returned to the same cached merchant and graph at tick150; restock stamp remained80, disposition remained2, the Torch remained with the player and the merchant's remaining six stock owners were unchanged. The source was not duplicated or reapplied.

## Viewed evidence

- `00-ordinary-start.png`: visible voxel Spread and ordinary40HP/50drams start.
- `01-actual-native-roadside-entry.png`: current player and merchant rendered beside the generated packed-road junction.
- `02-real-route-dialogue.png`: readable route sentence and native Trade choice.
- `03-native-actual-stock-purchase.png`: actual stock list, selected Torch, weight3, value3, buy price9. This frame is before confirmation; the subsequent report receipt and return frame establish the completed purchase.
- `04-returned-original-owner-and-purchase.png`: returned roadside scene,40HP,41drams and visible “You buy torch for9drams” log. Source identity and exact stock conservation come from the assertions/report, not inferred from matching silhouettes.

No new blocking rendering/readability issue was found in these frames. The existing dense sidebar log and its clipping are outside this bounded route's visual acceptance. Root separately confirmed clean Main-scene restoration; this reviewer did not operate Unity or independently inspect live editor state.

## Gates and bounds

The six new launcher/seed/isolation cases first failed for their missing APIs while eight existing controls passed. After publication the exact native selection passed14/14; see `E3/Integration/native-exchange-observer-{red,green}.json`. The preceding full E3 suite passed21,450/21,450 before these observer additions; the14 follow-up cases are reported separately.

This proves one real, useful roadside transaction and return at a preselected legitimate source. It does not measure uninformed player discovery, encounter frequency, balance, human awareness, use of the purchased Torch, saving/reloading this particular run, or stock after ordinary restocking. Frozen seeds1/64/1729 had no qualifying source winners; seed3 is transparently a controlled positive from the prior finite census. No source reroll, debug grant, teleport or substitute merchant was used. The cached return was70ticks after stock creation, below the normal greater-than300tick restock rule; it is not a claim of permanent finite commerce.

Q1: actual current entry and native UI are exercised. Q2: source roll/cap/stock and existing modes are preserved. Q3: exact owner/item/unit/purse and no-reapplication assertions are nonvacuous. Q4: all named native gates passed with viewed frames; the limitations above remain explicit.
