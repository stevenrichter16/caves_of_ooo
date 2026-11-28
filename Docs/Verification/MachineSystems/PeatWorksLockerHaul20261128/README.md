# Peat Works shows the locker’s current haul weight

Examine now gives the current weight of the locker and everything it
holds as one number. It uses the same weight as hauling. The line makes
it possible to weigh up taking individual supplies and moving the load.

## Ordinary play

The seed 65 traveler returned from the earned recovered-still save
at tick 3020. Two ordinary steps reached the surviving cutters’ locker
13286. It held seven entries at that point. Its 40 lb shell and 36 lb
of supplies weighed 76 lb. The old Examine explained that removing
supplies would lighten it, but displayed no weight.

The ordinary loot menu transferred the exact 12 lb acidworker apron
15134. The other six entries kept their identities, amounts and locker
custody. Examine now showed 64 lb. Taking the apron was optional.
The traveler’s 128 lb haul limit covered both loads.

Taking hold put the traveler at Speed 75. One southward step pulled
the same locker from (61,6) to (61,7), with its remaining contents.
Letting go restored Speed 100. The readout followed the locker and
still showed 64 lb after the move. The 76 lb load was inspected.
The recorded hauling step used the 64 lb load.

The player equipped the exact apron through the pack. It gave +50 acid
resistance and reduced worn Speed to 95. Fresh Continue kept the moved locker, its six
contents, the worn apron and the new readout. Health stayed at 41.
The trial measured the worn resistance and ended before an acid hit.

[Loaded locker](loaded.png) ·
[After taking the apron](lighter.png) ·
[Locker after hauling](moved.png)

## Native checks

The shared Examine provider applies to placed containers specifically
configured for hauling. It uses actual handling weight, including
stacked and nested cargo. Unavailable cargo weights are omitted.
The original place description, cargo and hauling rules stay in use.

The first run on the old source passed four controls and failed the
two cases expecting a weight line. The final run passed 246 cases
and skipped one explicit save-contract generator. Checks cover exact custody,
withdrawing a stack, moving a load, invalid weight and stale placement.
The playtest used an isolated Unity Editor save on Apple M5.

[Observed weights, contents, save pins and test totals](observations.json)

## Mac package

The updated universal Mac app includes the frost vent, Ginmere rope and
locker changes. It compiled 32,306 committed inputs with four warnings
and zero errors. Ordinary Continue on Apple M5 reached the moved locker
and opened its 64 lb Examine readout. Eleven retained apps and the
normal save and preference files matched their pinned contents.

[Mac locker Examine](mac-locker-reader.png) ·
[Mac package receipt](mac-package.json)
