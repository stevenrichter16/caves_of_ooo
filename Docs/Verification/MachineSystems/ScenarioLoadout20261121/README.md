# Complete scenario loadouts — November 21, 2026

Native play found that Camp Machines silently lost its late items at the shipped
pack's hard150 cap. The sidebar's Strength-derived capacity270 did not describe
that admission limit. Tests had raised the hard cap to400 before applying the
fixture, hiding the problem. Ordinary player carry rules are unchanged here.

This adopts the preserved shared ScenarioLoadout draft and five scenario callers
from the overlapping worker. The helper measures a fixture's requested item
weights, increases an existing hard cap to accommodate the loadout/current carry
or the soft capacity, preserves larger/uncapped limits, checks actual newly
delivered quantities and logs shortfalls in-game. Empty/nonpositive loadouts
leave the cap unchanged. It runs during scenario setup, outside frame/turn paths.

The independent critique found the empty-loadout cap change and a weak shortfall
test. That guard is corrected. Codex added cases proving a preexisting dagger
cannot mask a failed new grant and duplicate missing rows report the combined
request; both are present and pass in GREEN. Factory definitions are restored
after the unavailable-blueprint countercase.

- Preserved native RED:35 tests,31 pass/four fail, zero compile errors. Real
  shipped-cap tests lose flasks, the room test retains150, and empty input raises
  the cap incorrectly. Five scenario files were restored to baseline in the
  verification clone for this run; the working tree was preserved.
- Preserved native GREEN:159/159 camp scenario, fish trap, trough, scraper and
  machine-guide tests; zero compile errors. This overlaps other receipts.
- Root's five independent mutation runs all compile and are killed: skip room
  (four failures), ignore prior quantities (one), suppress shortfall message
  (one), reintroduce empty-loadout cap lift (one), use minimum room (two).
- Final bounded independent source review: zero remaining P1/P2. Reviewer did
  not operate Unity or execute tests. The helper's new metadata is preserved.

Actual fresh SampleScene/GameBootstrap play used seed64 and the corrected Camp
Machines scenario. The hard cap was270, the complete initial pack weighed175,
and all five flasks, six timber, striker, torch and repair parts arrived. Two
flasks held12 mire each; three were empty. Native input drove every action;
read-only inspection recorded state. No extra supplies or clock jumps were used.

- Still2158 was set at45,9, then moved through the real picker to46,10. This
  brought steam vent46,12 within two cells. The menu offered12 mire→3 water/oil,
  while refusing clay baking with an explicit insufficient-vent-heat reason.
  Settling at140→150 yielded3 water in flask2189, with no hearth/fuel present.
- Operator input earlier poured the first12 mire onto44,10 and used one frost
  lichen there. These were ordinary actions, not missing or duplicated inputs.
  The final record consequently has two lichen and no remaining mire. One move
  picker started from the fixture cell rather than the actor cell; its actual
  accepted destination was46,10, confirmed before using the still.
- Trap2159 was placed at37,10 at230. Ordinary waiting was interrupted by vipers,
  poison and confusion. The player equipped the ordinary dagger, defeated two
  vipers, drank both starting healing tonics and resumed waiting. No immunity,
  HP grants or direct combat/clock calls bypassed the encounter. Native waiting
  was bounded and guarded; health reached5 before recovery, never death.
- At1080 the trap projected one catch without changing its ledger. A corpse
  pile required choosing the exact fish-trap owner. Taking one perch cost
  1080→1090: trapHeld0, LastTick1080, Progress260; pond2198 at31,10 was3/4.
  Eating that same perch through the ordinary pack action cost1090→1100 and
  healed26→29 HP, demonstrating a useful food interaction outside the machine.
- F5 saved an earned checkpoint. A completely fresh Play/Continue retained
  tick1100, HP29, cap270, the five same-ID flasks including water3, still2158,
  trap2159 and its progress/ledger, and pond3/4. Reload did not mint a catch or
  replenish the pond. The consumed perch and healing tonics stayed consumed.

NativeSaveIsolation owned both disposable save roots. The earned save was
archived before cleanup at `/private/tmp/coo-camp-machines-complete-earned-20261121`.
Temporary keyboard/settings were removed and main Play ended with a clean scene
and null save override. Source hashes, observations and native XMLs accompany
this record. These are fixture/play checks, not ordinary acquisition, independent
game operation, complete machine acceptance or Mac delivery. Final independent
source/artifact review found zero P1/P2; all seven owned source hashes and the
bundled native XML counts match. The reviewer did not reenact gameplay or cleanup.
Awning, sounding,
spring-trap and further town/freight gameplay remain open.

Ownership overlap and its user-authorized resolution are recorded in
[the regression receipt](../Regression20261121/README.md).
