# A heavy opening with recoverable timber

November 25 UTC, 2026. Local candidate for the second generated opportunity
contrast. Independent final source and gameplay review, publication and Mac
delivery remain open.

Fresh Spread version 20 uses a stable per-world and per-site choice for an
admitted HeavyFrame location. Original hoist and reclaimable pallet outcomes
share the same optional geometry, outside walking route and existing cache.
Version 19 keeps its hoist policy. The pallet uses the existing 90-pound load
and its finite two-board dismantle. Generation omits the optional graph when
the actual material output is unavailable or the pallet contract is malformed.

## Editor play

The completed branches used independent fresh seed 1 games with the standard
starting equipment, 18 Strength and 40 HP. Native movement and map travel
reached 9.12.0 from Gleaners’ and walked to the generated pallet at 68,6.

| Route | Observed outcome |
| --- | --- |
| Dismantle and repair | Examine was free at tick 400. Dismantle at 410 removed pallet `7039` and gave two salvaged timber as stack `7043`. The player crossed the opening by 430. Normal return travel carried both boards to garden wicket `2130`, which consumed them and became usable at 610. |
| Haul intact | The player pulled pallet `7038` south two cells to 68,8, let go at tick 432, walked around the relocated pallet and crossed the opening at 492. The same owner remained whole, the cache stock retained its identities and counts, and the player received no timber. |

Fresh Continue matched the intact route at 492 and repaired route at 610. The
repaired save also kept the spent source absent, its gap open and the cache
contents unchanged. The seed 14 approach encountered a scrabbler and the
player died at tick 290 before reaching the pallet. That interrupted run is
preserved separately and contributes no successful haul evidence. Ordinary
fighting at Gleaners’ during the seed 1 return killed one villager before the
wicket was repaired. The player was at full health at the repaired checkpoint.

The first readout described the pallet as currently blocking the opening even
after it moved. The final text describes the pallet and its uses. The gameplay
archives keep the text already saved on their existing instances.

- [Final pallet reader](01-pallet-reader.png)
- [Moving the intact pallet](02-hauling.png)
- [The repaired garden wicket](03-wicket.png)

## Unity test cases and local archives

Two compiling tests reproduced the missing pallet in current seeds 1 and 14.
The first targeted pass was 184 of 184. The final expanded run passed 237 of
237, with zero compiler errors, skipped or inconclusive cases. Coverage
includes both passage methods, save state, the old saved version, a fresh
hoist seed, refusal with missing or malformed material, existing shipment
behavior and the original Gleaners’ pallet.

The full Unity runs are under `/Users/steven/coo-verification/results` with
names `satellite-timber-red-20261125`, `satellite-timber-green-20261125` and
`satellite-timber-final-20261125`. Three F5 checkpoints preserve original save
and backup pairs at ticks 430, 492 and 610. Their roots are
`/private/tmp/coo-satellite-timber-dismantled-20261125`,
`/private/tmp/coo-satellite-timber-hauled-20261125` and
`/private/tmp/coo-satellite-timber-repaired-20261125`. The separate seed 14
archive is `/private/tmp/coo-satellite-timber-seed14-interruption-20261125`.
The Editor on the development Mac supplied these observations. Performance
and packaged behavior remain unmeasured.

## Shared action label

The Focus hint originally called the pallet action “harvest” while the world
menu called it “dismantle for timber (2).” The read-only cue now carries the
authored label from the selected finite source. The same label reaches
adjacent, focused and controller hints. A changed label invalidates a captured
cue. Generic harvest keeps its ordinary wording.

Both compiling RED cases reproduced the mismatch. The expanded run passed all
112 cases. On fresh Continue from the intact-route archive, walking next to
the moved pallet produced the updated Focus hint at tick 512. Opening and
cancelling the menu left the clock at 512 and the pallet intact. The controller
format was exercised by tests. The Editor screenshots show keyboard input.

- [Focus beside the moved pallet](04-hint.png)
- [The matching world menu](05-menu.png)

## Independent output admission follow-up

Independent review found that a present timber template could be advertised
even if the harvest command would refuse it. Generation, ordinary harvest
and its Focus hint now share admission for takeable, noncreature output with
one authored unit. The nonportable, creature and multi-unit fixtures each
reproduced the defect before correction. The earlier verified routes used
the unchanged canonical one-unit timber template. The broader seasonal
regression includes these countercases and the existing affordance tests.
