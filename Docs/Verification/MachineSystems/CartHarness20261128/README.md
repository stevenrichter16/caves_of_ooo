# Salvage from the overturned cart

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple silicon.

One freshly generated Fallen Cart south of Sill has a loose harness
strip. Its bright mark and Examine text show the available material.
A sound carried or equipped blade with the ordinary flexible-cutting
capability trims one cured leather offcut in one action. The available strap gives the existing cart sprite a warm tint.
Native inspection recorded the same tile with neutral color after
trimming. The cart, letters and contents stay in place when the player trims the strap.
The existing pack recipe makes one leather binding from the offcut.
Bought leather and finished bindings remain alternatives.

Fire destroys the available leather through the cart's actual burning
effect. Dousing saves the surviving cart without restoring the strap.
The normal fire can still cook bought raw food. The harness's finite
state uses the ordinary save and the original cart. This passes the
opportunity to a newly generated copy of the one site. Existing cached
carts retain their original content.

## Ordinary play

The seed-65 traveler continued the earned Bought save at tick 2200 with
the original dagger, two healing tonics, bought meat and lit torch, and
a flask of water. Atlas travel and field movement reached the exact
cart at (62,2). A viper and poison took the traveler to 8 HP. Two tonic
applications brought the traveler to 27 HP at 2470. Both outcomes start
from that exact cold cart save. No tools, health or materials were supplied.

| Choice | Observed result |
| --- | --- |
| Trim first | The original worn dagger took one cured offcut at 2480. The same cart had 12 structural HP. Its cache, sack and note contents and identities matched their pre-cut state. After atlas travel back to Sill, one pack action at 2530 used the offcut and made one leather binding. Continue kept the binding, worn dagger, trimmed cart and goods. The traveler retained 7 HP from the encounter. |
| Use the flame | The original torch lit the cart at 2480. Its Cut state stayed false and its harness became charred. The C menu offered normal cooking and dousing. Normal Douse at 2490 spent one measure from the original flask (12 → 11) and stopped the flame at 10 cart HP. Examine reported a burned-through harness and there was no Cut row. Continue retained the burned harness, cold cart and used water. The raw meat was kept in this branch. |

## Verification

All 10 installed-behavior cases failed with the cart installation
hook removed. A separate native case failed before the authored cart accepted
the harness tint. The final focused run passed 252 cases and skipped
the explicit saved-part contract regeneration helper. Cases cover old saves, tool custody, rain, lost reach and
visibility, capacity, restores, stale quotes, late ignition and
rollback of a single stock unit. Independent source and gameplay
reviews checked the two outcomes.

Both branches used private saves. All 880 ordinary Quick files and
87 unrelated tracked paths matched the starting hashes. The Editor
playtest covers this one generated site. The current Mac package is
the prior dry-brine milestone.

- [Loose harness marker](01-loose-harness.png)
- [Cut action](02-cut-option.png)
- [Trimmed cart](03-trimmed-harness.png)
- [Existing leather-binding recipe](04-binding-recipe.png)
- [Reader after dousing](05-doused-harness.png)
- [Exact observations and save hashes](observations.json)
