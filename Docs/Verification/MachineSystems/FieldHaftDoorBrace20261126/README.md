# A rough field haft holds the Gleaners' wicket

The same door brace accepts an ordinary iron spike or one loose rough field
haft. The material is tied to the exact closed, unlocked door. It prevents
ordinary opening from either side, takes one action to install or remove,
and returns the same piece. The existing access, detour and weight rules
apply. The door action names the fitted piece when it refuses to open.

## Native checks

The compiling RED run passed the existing iron control and failed the new
haft case because the action was missing. The final 56 cases passed in the
idle clone, with zero compiler errors. They cover both materials under
the same player and AI gate, save roundtrip, late removal rollback, exact
material advice and refusal of stacked hafts or raw timber.

## GameBootstrap play

- A private Continue began at tick 1905 in Cinderhold with 40 HP, 21 drams,
  one timber and the rough haft already shaped in the earlier
  [Sumphold cargo route](../SumpholdPublicCargo20261126/README.md). The
  player approached Ostrel, selected him from the bed tile, and bought his
  one salvaged timber for seven drams. It merged into the original stack
  at tick 2035. The player crossed the world map to the Gleaners' glade.
- At tick 2215 the original wicket `2127` was repaired with the two actual
  timber units. Opening and closing it worked at ticks 2225 and 2235.
  The pack then offered to brace it with rough haft `7473`. One action
  lodged that same item at the closed doorway at tick 2245.
- Fresh Continue kept that exact brace on the wicket. Reading the disabled
  action and attempting to open were free. The attempt asked the player to
  remove the rough field haft first. Selecting the physical haft from the
  doorway returned it to the pack at tick 2255. The wicket opened at 2265,
  the player stepped into it at 2275 and passed through to `(46,5)` at 2285.
  A second fresh Continue kept the open wicket, exact loose haft and 40 HP.

All three four-file save sets were archived before the private sessions
closed. Independent review verified the receipt hashes, the exact linked
door and the original haft returned to the player with no duplicate
owner. The original and returned item records matched after normalizing
the serialized holder reference. The iron spike stayed in the pack.
The selected 861 ordinary state files and 86 foreign workspace files still
matched. The editor stopped and its owned save override and reload lock
were cleared.

This guided play used native input and an earned checkpoint. The iron and
NPC opening comparisons are native tests. The latest Mac package predates
the haft change. Full regression and the twelve complete machine gates
remain open.

[Observations](observations.json)
