# Crust from natural pools with an equipped blade

The world action uses the selected sound blade on an adjacent, visible
natural pool. It spends one turn and one catch from the same finite
deposit used by a dedicated scraper or settling tray. It makes noise and
leaves the liquid in place. The dedicated scraper still works on hard
scale and rime. The new row binds the exact blade and the selected pool's
zone and cell. Looking at the action creates no stock.

## Ordinary gameplay

Both replays began with the same earned seed 65 save after resting at
Brine Camp, at tick 6590 with 42 HP and dagger 2703 equipped. The previous
runtime offered no skim row in the mire's normal interaction menu.
The new runtime was played through the ordinary keyboard controls and
GameBootstrap.

| Action | Observed result |
| --- | --- |
| Interact south from the camp bed at 14,11 | Skim took acid crust 23319 from natural mire 11339. The shared deposit on 11214 fell from four catches to three. The pool retained 60 measures and the player stayed at 42 HP. |
| Walk to dry ground at 33,19 and interact southwest | The same dagger took brine crust 23321 from natural pool 12544. The shared deposit on 12543 fell from five catches to four. The pool retained 74 measures. |
| Pick both crusts in Brew | The preview showed corrosion and electrification. Brew spent both exact crusts and produced acidic & electrified tonic 23322 with both effects. The player stayed at 42 HP. |

Both gathers used one turn apiece. Fresh Continue restored the exact
tonic at tick 6860, its Acidic 1 and Electrified 1 effects, both pool
volumes and the remaining catches. The new playtest qualifies
acquisition and brewing of the combined tonic. Its application and
throwing remain open for a separate gameplay trial.

## Verification

The baseline ran the final fixture and failed the seven new cases with
36 scraper controls passing. The updated focused run passed all 291.
It covers the three natural liquid types, carried and equipped tools,
shared stock with a scraper and real tray, and refusal of stale,
thermal, poured, empty and distant targets. The interrupted action keeps
the source and reagent quantities consistent. Independent review checked
the source, the matched start, visible menu and both exact finite debits.
The playtest used the Unity Editor on Apple M5. The 880 pinned normal
save files, the 87 pre-existing modified workspace paths and both
preference files matched their recorded bytes after cleanup.

[Mire interaction](01-mire-menu.png) · [Brine interaction](02-brine-menu.png) · [Brew preview](03-both-reagents-preview.png) · [Observed states](observations.json)
