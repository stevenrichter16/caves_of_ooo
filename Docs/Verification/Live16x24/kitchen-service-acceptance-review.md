# Independent kitchen-service acceptance review

Reviewed 2026-10-24. Read-only review of the six `kitchen-service-65-job-*.json` receipts, their saved observer source, root’s `kitchen-service-play-record.md`, and actual paired Unity captures. No Unity actions, runtime mutations, tests, cache changes, staging or commits were performed by this reviewer. This document is the only review output written into the repository.

## Result

The saved evidence supports one completed ordinary keyboard kitchen-service cycle in flat mode: acquire real ingredients and clay, repair, pay, wait, collect, save/reload the collected item, examine it, eat it and reopen an empty pickup board. No significant contradiction was found in that cycle’s recorded quantities, identities, custody or provenance. This is **pre-state-art acceptance**; it does not accept future Working/Ready art or the complete graphics release.

## Independently checked transitions

All six observations report seed 65, `Overworld.12.11.0`, flat presentation, the current cached-zone graph and one current pan. Pan 7208, Cook 7190, escrow 7209 and pickup 7210 retain canonical visible/explored cells, exactly one object reference/ID at their cells, owned Physics and their expected identities. The pan retains its owned repair part. Each carried/container row has owned Physics/Stacker, its expected custodian, no equipped owner, no cached-zone index and exactly one reference/ID across the three recorded inventories.

| Observation | Tick | Independently checked outcome |
| --- | ---: | --- |
| Before repair | 5100 | Broken pan, Idle ordinal 0; carried FireClay 9241 ×2, Emberwheat 7233 ×2 and ClaspbeanPulp 7227 ×2; player/Cook purses 50/100. |
| Before start | 5110 | Pan repaired; FireClay 9241 is absent. Every other complete inventory row is unchanged from the preceding observation. Both trays empty; purses unchanged. |
| Working | 5120 | Job ordinal 1 **started at 5110**, due 5230, total 120 ticks and 110 remaining. Player/Cook purses 48/102: exactly two drams transferred. Emberwheat 7233 ×2 moved intact to escrow; one pulp was split into escrow ID `e838ef8998d946b49b385a2150228b99`, leaving original 7227 ×1 carried. Job input rows equal the two actual escrow rows in all recorded fields, with matching expected IDs/counts; commissioner resolves to player 2696. Pickup empty. |
| Ready | 5230 | Ready ordinal 1 exactly at due time; inputs cleared and escrow empty. Exactly one FieldMeal 9249 ×1 appears in pickup 7210, and the job output row equals that actual pickup row. Provenance is station 7208 / customer 2696 / ordinal 1, with owned FieldMeal part. Working inventory and purses remain unchanged. |
| Collected and reloaded | 5260 | Same FieldMeal 9249 ×1 carried by player 2696, with identical recorded item fields except the expected custody/reference changes from pickup to player. Provenance remains 7208/2696/1. Pan remains repaired; job is Idle ordinal 1 with cleared commissioner/input/output fields; both trays empty. Other inventory rows and purses unchanged. |
| Eaten and reopened | 5270 | FieldMeal 9249 absent from player and both trays. All eight remaining full inventory rows exactly match Ready’s inventory; purses unchanged. Pan remains repaired, job Idle ordinal 1 and trays empty. |

The repair inventory comparison excluded only the consumed two-clay row. The commission comparison removed only the two-grain row and decremented only original pulp 7227 by one. These were full-row comparisons, not blueprint totals. The subsequent collection/reload changed only meal custody/reference fields; consumption removed that single row. Thus the observed sequence contains no extra fee, duplicate output, unrelated inventory loss or repeat commission ordinal.

## Actual screen checks

PNG headers independently confirm **all 15** `kitchen-service-65-*.png` captures are 1280×800. Visually inspected `repair-action`, `working-reader`, `ready-reader`, `pickup-row`, `collected-reloaded`, `meal-reader`, `eaten-empty`, `failed-approach` and `reloaded-after-death`.

- The repair action asks for two fire clay. The Working reader displays 110 ticks remaining and the recipe 2 emberwheat + 1 claspbean pulp + 2 drams / 120 ticks. The Ready reader changes to one wrapped meal ready in the pickup tray.
- The pickup menu contains one wrapped field meal. The later world capture logs its collection and “Game loaded.” The carried meal reader shows its native parcel portrait. The final capture logs eating the meal and opening an empty pickup board.
- World captures retain the real court, characters, crops, UI and limited visibility. Current pre-state-art bowl/board pictures are neutral; they do **not** visually prove Working/Ready state without the actual reader and receipts. This matches the named next art task.
- The failed approach visibly has 0 HP and a Scrabbler death. The resumed capture has 40 HP. This failed attempt is kept distinct from the successful later cycle and is not counted as a successful route.

## Limits and retained follow-up

Root’s recorded keyboard procedure and visible load log support a real ordinary save/reload after collection. These six observer receipts do not independently prove byte-for-byte full-save equivalence or a Working/Ready-state reload: there is no paired before/after full job receipt at the same persistence moment. No claim is made about internal `SpatialZone`; the observer explicitly records public cached-zone indexes only. Nor do absent rows establish destruction of every unreachable runtime reference. Idle’s negative raw `remainingTicks` is simply zero due tick minus current tick, not an active overdue job.

Eating occurred at full 40/40 HP without bleeding, so this review accepts consumption but cannot measure healing or bleed removal. It does not accept companion sharing, throwing, interrupted jobs, every seed/route, mouse/controller or physical Deck behavior, packaged activation, performance, animation or regional coverage. PNG dimensions alone cannot establish control feel or cadence.

The carried meal reader visibly says “Self-use and existing throwing remain available” while its handling readout says the meal cannot be thrown. Root already records this non-graphics prose follow-up; `ItemTacticalUseDescription.cs` appends that generic statement to the FieldMeal hint. Do not claim throwing passed or treat the contradictory text as accepted. This does not invalidate the separately demonstrated repair/payment/cooking/collection/consumption cycle.
