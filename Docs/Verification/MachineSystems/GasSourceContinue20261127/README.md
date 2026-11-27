# Marsh-gas source survives Continue

Lingering gas poison now saves its damage source separately from the
poisoned creature. Ordinary entity references preserve the source
across zones, including its identity among cached occupants. The older
implementation used a second field named `Owner`, which the save
writer excluded alongside the effect's patient field.

## Native checks

The authored creator case failed on the old runtime when the saved
tick dealt four damage with an empty source. The source-less control
passed. With the fix, both cases passed within 250 focused cases,
with zero compiler errors or skips. The creator test compares the
damage event source to the exact entity in the other loaded zone and
checks that the effect still belongs to the patient. The same run
covered ordinary gas effects, marsh gas, fire, companion care, death
attribution and save graphs. Independent source review checked the
three changed files and both native results.

## Brine Camp

Normal GameBootstrap play began at the original Brine Camp arrival
with the carried lit torch and one field-grown pad. Touching the torch
to the existing mire and stepping into the resulting cloud acquired
poison through the terrain. Ascent put the traveler on the world map
while the exact mire stayed cached. Fresh Continue restored both the
same mire as source and the traveler as the effect's patient.

From the same fresh Continue at 16 HP with eight gas turns, one
wait took two HP and left seven gas turns. Applying the original
pad took the same time, held at 16 HP and cleared the effect. It
spent one pad. Both branches kept the lit torch and became charred
as the separate burning effect expired. An earlier one-turn roll
also retained its source and expired on the next action.

The pre-fix poisoned archive also loaded in the fixed runtime and kept
its nine poison turns at two damage per turn. Its source is unavailable
because the old writer omitted it. Use the fixed build for newly written
saves containing active gas poison. The old reader cannot interpret the
added field.

Independent gameplay review checked the source reference against the
only matching mire body in the saved world and checked the matched
wait and treatment receipts. After the Editor exited Play Mode, all 876
checked normal save files and 87 prior tracked edits matched their
earlier hashes.

[New exposure](01-new-marsh-gas.png) · [Wait after Continue](02-reloaded-poison-tick.png) ·
[Apply after Continue](03-pad-after-reload.png) · [Observed state and save hashes](observations.json)
