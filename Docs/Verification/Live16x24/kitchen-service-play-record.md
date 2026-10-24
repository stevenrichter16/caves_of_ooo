# Kitchen service ordinary acceptance — seed65

2026-10-24, root-owned Unity6000.3.4f1 Editor Play, default flat presentation,
1280×800 actual ScreenCapture PNGs. This is the pre-state-art acceptance, not a
claim that the later working/ready sprite cues were already integrated.

## Route and controls

Continued the owned `c757c38ed9c4452583230acac209cb85` checkpoint through ordinary
Continue, with player2696,40HP and50drams. Save-isolation token
`4513e352400145e896495ea74e0221e6` protected user saves; no grants, teleportation,
state setters, forced clocks or enemy removal were used. Public observational
helpers recorded fields and queued real Unity captures only.

The first approach north to the eastern grain pen harvested there but died to a
Scrabbler while returning. `failed-approach` records that failure. F6 at the death
screen did not load; the displayed L command did. The resumed autosave contained
no grain from that attempt. The successful second route went around the danger
via east46,12, west into the glade. From47,12, `C`,Left,`h` harvested46,12; from49,14,
`C`,Right,`h` harvested50,14. The two grain units stacked as Emberwheat7233.

Ordinary movement reached glade stairs28,19, with a small detour around the real
wall at31,14. `Shift+Period` descended. The covered cellar route ran through40,19,
46,19,46,22,50,22,50,19,66,19,66,18. `C`,Up,`o` opened the original crate66,17;
Enter took its two FireClay units9241. The other contents stayed. Reversing the
route and `Shift+Comma` at40,12 returned to the glade. The moving guard was not
removed or altered. The second route remained40HP.

Returned east alongy12, south from east36,12 into kitchen36,0. Actual walls and a
friendly Magpie at51,1 required a rear-aisle detour through54,0→54,3→51,3. The
Farmer on the cot stayed present. `C`,Down,`r` patched pan7208 with two actual
clay, then F5 saved. Reopening with `C`,Down,`b` commissioned the displayed
2grain+1claspbean+2drams recipe. The Working reader showed110ticks remaining.
After closing it, eleven short period waits advanced110ticks to5230 Ready.

F5 saved Ready. `C`,Down,`x` read the actual ready pan. Escape,Right2,`C`,Down,`o`
opened pickup7210 from53,3; its one row was wrapped field meal9249. Enter took it,
F5/F6 saved and reloaded. Inventory/Tab, cursor movement and F1 displayed its
native portrait and actual description. An initial cursor count reached the
claspbean seed; one further Down selected the meal before the saved capture.

Escape from the reader, Enter opened the selected meal's action menu. Down and
Enter chose the displayed Eat row. Use closed the inventory automatically; an
extra Escape opened Pause, which was closed without loading. `C`,Down,`o`
reported the pickup board empty. This last inspection minted nothing.

## Observable state / custody

See `kitchen-service-play-summary.json`, its reproducible Python validator, the
six `job-*.json` receipts and read-only `kitchen-service-observer.cs.txt`.

| Moment | Tick | Pan/job | Player/Cook drams | Actual contents |
| --- | ---: | --- | --- | --- |
| Before repair |5100|broken,Idle0|50/100|2clay9241;2grain7233;2pulp7227 carried |
| Before commission |5110|repaired,Idle0|50/100|clay gone;grain/pulp retained |
| Working |5120|Working1,start5110,due5230|48/102|grain7233×2 plus split pulp `e838ef8998d946b49b385a2150228b99`×1 in escrow |
| Ready |5230|Ready1|48/102|escrow empty;one FieldMeal9249 in pickup,station/customer/ordinal7208/2696/1 |
| Collected + reloaded |5260|repaired,Idle1|48/102|same9249×1 carried by2696;both trays empty |
| Eaten + reopened empty board |5270|repaired,Idle1|48/102|9249 absent,8other carried rows retained;both trays empty |

The original seven non-pulp carried identities and remaining pulp stay intact.
The Cook's arrival/restocked purse was100, not the blueprint's initial35; payment
is compared against the actual pre-start value. Start itself spent10ticks;
the eleven subsequent waits completed the120tick batch. Input references and
IDs were unique, physically held by escrow; output custody/provenance matched.

## Visual finding and scope

The real service works and its native meal portrait is connected. The pre-fix
Working pan still looks like an open empty bowl; the pickup board stays neutral
when its actual output is ready. Thus derive two physical state cues from the
actual Working capture and approved east/kitchen contact. Keep idle assets and
other scene owners stable. Existing neutral art is not evidence of state.

Can verify: actual keyboard route, default flat presentation, exact repair/input/
fee/output transitions, reader appearance, one-item pickup, saved item identity,
consumption, empty-board counter and restored editor isolation. Cannot verify:
healing amount or bleed removal (player was fullHP/uninjured), hardware controls,
packaged activation, GPU performance, every interruption/crop/forge outcome or
whole opening/regional completion. The current meal prose says throwing remains
available while its current handling readout says it cannot be thrown; retain
this named non-graphics readout follow-up rather than claiming that path passed.

No runtime code was changed for these acceptance checks; no new automated
runtime test count is claimed here. New sprite state behavior has its separate
RED→GREEN/native/ordinary replay gate. Stop+cleanup receipt confirms Play=false,
isolation=false,saveRoot=null and restored SampleScene/view/preferences.

Owned checkpoint copies are `/tmp/coo-kitchen-service-65-{repaired,working,ready,collected}-checkpoint`.
They contain ordinary saved graphs, not authored test state. Preserve for the
new-art replay; these local checkpoints are not user saves or release evidence
by themselves. All paired PNG/JSON files use the prefix`kitchen-service-65-`.
