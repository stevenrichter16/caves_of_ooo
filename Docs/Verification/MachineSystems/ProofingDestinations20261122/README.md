# Proofing flask access without forced stashing

Source baselinebf8bb8dc1. ProofingPressActions validates every carried vessel,
sorts compatible destinations by their current volume, then shows only the first
three for each reagent element. A player can carry a valid empty or specially
chosen flask beyond those three and cannot select it without stashing others.
The ordinary item-action popup already scrolls; no UI redesign is required to
make the existing valid destinations accessible.

Smallest correction: remove only the enumeration cap. Preserve carried ownership,
single-flask consistency, liquid compatibility, all-or-nothing capacity, per-
element commands, fuller-first preference, exact destination identity, finite
reagent payment and transaction revalidation/rollback. The producer's volume sort
is retained; shared menu sorting also compares display text, so this receipt
does not promise a user-facing fullest-flask order. No new recipe, machine,
capacity, yield, save field, UI renderer or global material rule is introduced.

Native RED tests cover nine destinations and two-element eighteen-row choices,
late-empty selection among fuller preferred vessels, stale destination refusal,
interruption rollback and invalid vessel exclusions. Tests remain in warm clones;
actual GameBootstrap item selection and fresh ownership/payment/effects must
qualify the change separately.

The native continuation below uses the earned resin checkpoint1560: one naturally
gathered resin12315, six fixture resin2167 kept separately on4,9 ground. Normal
inventory input selects a previously hidden empty flask and presses the earned
unit, then coats/equips lacquered WardedCloak2342. No ad hoc grants or altered
bottle capacity are used.

This is access to existing common preparation, not complete-machine acceptance.
Lore and material limits remain those of the existing proofing process; no new
world claim follows. Package the accepted access correction together with recent
expiry and ascent feedback fixes as the next coherent Mac candidate.

## Native tests and actual continuation

Eight native RED cases compile with zero errors: seven expected missing-choice
failures and one passing invalid-vessel exclusion control. GREEN96/96 covers the
eight plus existing press, coat, trough, registry, workshop, worth and actual
input tests. No new full-suite claim. The production diff removes the cap
constant and loop bound only; independent source/test review found no P1/P2.

Continue earned1560, HP400/regard13. Normal inventory popup for natural resin12315
now offers eight empty flasks2235–2242 rather than the original first three.
Four other flasks2231–2234 hold12 mire each and remain excluded. Names/volumes of
the empty flasks are identical; command IDs establish exact recipient custody,
not a new visible identity label. Native choice at row9 selects2242, the last
empty flask,1560→1570: one carried resin is consumed and only2242 gains one
heat-proofing unit. All other liquid IDs/volumes/capacities remain unchanged.
Fixture resin2167x6 stays on the ground and is unavailable to this pack action.

Reopen the chosen bottle normally through inventory search/browse. Its full
49-choice popup offers transfers/pouring and garment coats. Native late row45
coats lacquered WardedCloak2342,1570→1580:2242 empties, its liquid ID clears,
and the spare gains a100-turn heat coat while retaining lacquer. It remains
unworn/Heat0/ArmorPart.AV0 until equipped. Accepting the normal replacement
preview puts2342 on the back: Heat35 and ArmorPart.AV1; removing the previous
signed garment returns Bower regard13→8. Equipment actions do not advance1580
under existing rules. Full two-page F1 reader and inventory feedback exercised.

F5/stop/fresh Continue1580 keeps exact2242 empty, all other bottles unchanged,
no carried resin, fixture2167x6 on40,12, seed2301x5, worn2342 with lacquer and
heat100/100, Heat35/ArmorPart.AV1, HP400/regard8. Archive
`/private/tmp/coo-proofing-destination-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
This joins ordinary world resin with either town lacquer or common portable
proofing, without mandatory machinery or sacrificing the stored fixture stock.
No additional hazard damage, worn-time expiry or complete-machine acceptance.

One observer request timed out and an initial inventory-row observer used the
wrong display-wrapper cast; both supplied no state evidence and changed no game
objects. Successful readbacks and native selection above qualify the result.
Main stopped/rootnull, owned input devices/callbacks/settings restored. Fresh
foreign/normal preservation comparison and final independent evidence review
are recorded with this receipt. No whole-directory restoration or FPS claim.

## Independent review

Bounded design/source critique approved enumeration-only access and corrected the
menu-order claim. Final source/test review and independent seven-image/result/
observation review found zero P1/P2 or publication blockers. The reviewer checked
all twelve vessel states, exact2242 recipient, conditional garment effects,
fresh equality,96 passing native cases and84/861 unchanged preservation hashes.
This is independent source/gameplay-evidence review, not a second operator.
