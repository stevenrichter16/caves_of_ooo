# A recovered camp still works on natural steam

One fixed iron case appears in newly generated Peat Works at `(63,6)`.
It holds a single ordinary 18-pound camp still. The catch repair
accepts an iron spike or a knotflax cord. Repair opens access to the
case. Taking the still still needs reach, strength and pack room.
The neighboring locker keeps its eight original units and two regional
garments. The existing public still and commercial kit remain available.

## Ordinary GameBootstrap play

I continued the seed-65 Duelist and used the world map to enter Peat
Works for the first time. The old locker at `(61,6)` contained two
knotflax cords. The field case showed a disabled Open action and named
both repair materials. Repair with an empty bill left the turn at 2920.
After taking the locker’s cord stack `15127`, I spent one cord on case
`15137`. The other cord stayed in the pack. Opening the repaired case
revealed still `15138`. I took it and left the case empty. I also picked
up a mallet head from the old locker while navigating its loot menu.

The same flask `9873` took 12 bog mire from natural pool `13481` in
Peat Works. The source dropped from 60 to 48. In the neighboring Sodden
zone `Overworld.16.8.0`, the recovered still set down at `(61,14)` on
dry ground within range of vent `17799` at `(59,14)`. The action menu
offered water and oil and explained that clay needed a hotter fire.
One action turned all 12 mire into 3 oil in the same flask. The other
flask stayed at 12 brine. The stowed torch kept its exact fuel.

A fresh Continue at tick 3700 kept the installed still, 3 oil, 48
mire and empty repaired case. I then took up the same still through
the world menu and saved it packed at tick 3710. Sari-snakes interrupted
the journey and the player used one carried healing tonic. The spike
route passed native tests. Ordinary play used cord and settled oil.

[Jammed opening and repair choice](01-fault-menu.png) ·
[The still inside the repaired case](02-recovered-kit.png) ·
[Steam offers oil and water](03-vent-still-choices.png) ·
[The settled oil](04-vent-settled-oil.png)

## Verification

The first native run on the previous runtime failed all four cases that
expected the new fixture. The existing saved-layout control passed.
The final filter passed 223 cases. It includes both payments, exact
ownership, locality, 17 pounds of free capacity, ordinary lock behavior,
conservation at existing heat sources, and binary Continue.
An independent source review found the initial shed reservation issue
and checked the final transfer guard. An independent gameplay review
reconciled the two checkpoint bodies, the backup with the still in its
jammed case, and the native menus. The main Editor left Play Mode and
all 876 normal Quick files and 87 pre-existing tracked edits matched.

[Native play states and checkpoint hashes](observations.json)
