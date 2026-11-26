# Clean recovery with the Cinderhold hand pump

The released maintenance-oil tray uses the existing hand pump's cardinal
reach and finite stroke limit. A stroke moves the quoted oil into an
ordinary compatible outlet and leaves the tray and its contents in place.
A bare outlet must be clear and dry. An existing compatible pool can
receive the oil. Carrying a compatible flask and reaching through the
oil remain available.

## Actual GameBootstrap play

- The seed 64 version 25 checkpoint began at tick 1560, with 27 HP and
  six drams. The player had forged exact hand pump 9209 in ordinary
  play and reached the released tray without a flask.
- The C menu on tray 9064 offered three distinct clear outlets. The
  player read the east outlet's quote and returned to the menu. The
  tray still held 12 oil and the outlet was empty at tick 1560.
- One stroke moved all 12 oil to new ground pool 9210 at `59,6`.
  At tick 1570, tray 9064 was still installed and held original fitting
  9065. The empty tray offered its ordinary Open action.
- The player opened it and took fitting 9065 through the usual pickup
  list. At tick 1580 the player had the same pump and axe head, with
  no oil coating. The tray was empty. Fresh Continue returned the
  same exact identities, quantities, position and health.
- One ordinary step into the outlet at tick 1590 applied oil coating
  to the player. The pool still contained 12 units. Health stayed at
  27. This demonstrates ordinary ground contact with the moved oil.

## Verification

The former source passed 40 tests and failed the three intended new
cases. The final connected Unity run passed 247 cases with no
failures, skips or compiler errors. It checks finite compatible stock,
locked or competing sources, stale quotes, rollback and other pump
uses. An independent review matched 12 archived save files and three
captures. This is bounded portable pumping and salvage access. Installed
drainage, pressure, NPC use and packaged qualification stay open.

[Player quote](01-quote.png) · [Clean recovery](02-clean-recovery.png) ·
[Ground contact](03-moved-oil.png) · [Native observations](observations.json)
