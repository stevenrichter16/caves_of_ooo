# Dry approaches from the two Sumphold work fingers

This is an ordinary movement scout for requirements 26–30. Both version
26 saves started before Sumphold was cached. The matched tick 100 backup
pair from each retained save was copied into a private play session.
The player entered through the world map, walked to an existing empty
boatyard crate, and followed the proposed path on foot.

| Seed | Arrival readback | Source and starting side | Actual walking path | Result |
| --- | --- | --- | --- | --- |
| 64 | Tick 140, `(39,11)` | West crate `10040` at `(20,19)`, beside it at `(21,19)` at tick 330 | North four, east one, north one | `(22,14)`, tick 390, 40 HP |
| 65 | Tick 160, `(39,11)` | East crate `9810` at `(65,19)`, beside it at `(64,19)` at tick 420 | North six, west one, north one | `(63,12)`, tick 500, 40 HP |

The arrival backups were written at ticks 130 and 150, one action before
the two arrival readbacks in the table. The west route used floor. The
east route included four dry duckboards
on ordinary floor. Both real wells remained at `(40,12)`. All four save
files were checked per branch before Stop. The working saves are
`/private/tmp/coo-sumphold-scout64-berthed-20261126` and
`/private/tmp/coo-sumphold-scout65-berthed-20261126`.

The native scout moved only the player. A new saved allocation should bind
an exact clean, empty crate during fresh optional furnishing admission.
Admission requires the real loaded route and a practical receiving berth.
Existing Crate has standard Open and Take. Its adjacent restocking needs
explicitly admitted public storage and separate validation. The load must
preserve physical cargo identity and include the contents in haul weight.
Native load, interruption, deposit and withdrawal, reload after depletion
and actual use of recovered timber remain implementation gates.
