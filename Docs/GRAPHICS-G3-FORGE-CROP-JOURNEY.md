# Ordinary frost temper and kitchen crop journey

2026-10-25 UTC. **Bounded ordinary Unity acceptance completed.** Existing native
art remains unchanged. This pass closes real uses of the opening art rather than
adding another disconnected asset set. Full opening, regional, packaged and
physical Steam Deck release gates remain open.

## Plan and verification sweep

Continue the exported healthy earned checkpoint through north forage, an actual
brew, the east forge, then Orven's kitchen bed. Use ordinary controls and existing
GameBootstrap. Preserve the earlier failed east run as separate evidence. Record
item custody, owner identity, effects, condition and visible growth states. No
state grants, forced combat outcome, teleport or simulation clock setter.

| Premise checked | Correction or confirmed contract |
| --- | --- |
| Prior injured character can resume | That disposable save was not exported. This run explicitly continues the separate healthy earned branch, not recovery from5HP. |
| Underfoot `C .` offers plant Harvest | With Player and plant on the cell it opens a pile summary. Select the actual plant to get Harvest. The earlier `harvest-menu-defect` filename was incorrect and is now `forge-route-underfoot-summary`. |
| An equipped blade is listed in the forge picker | Unequip the existing blade first; keep it in inventory for the ordinary transaction. |
| Tempering must change the blade's blueprint/art | Real temper preserves LongSword identity. Neutral blade art remains correct; no invented blue glow is added. |
| Rain supplies40 world ticks | Current moisture budget is40 moist rounds,400 world ticks. The casting action already advances the first moist unit. Old tooltip/doc wording needs a separate correction. |
| Crop growth only occurs while present | Current crop implementation uses elapsed world time; historical docs claiming offscreen freeze are stale. This pass directly observes growth while present, not a new offscreen experiment. |

This is original CoO presentation and gameplay evidence. No claim of new Qud
source parity. Existing art concept/extraction/native production records remain
in the preceding LongSword, works-products and east-kitchen packets; their
accepted pixels were intentionally preserved.

## Actual connected route

Root launched ordinary SampleScene with seed64 and copied only the unmodified
earned checkpoint into an owned disposable save root. `C` continued it. Player
2695 has42HP, LongSword2699 equipped and a buckler. The temporary1280×800 Game
view is capture framing, not a special scene or alternate renderer.

1. North world-map travel, ordinary Footwork reading and movement reached the
   Patchbearer. The encounter visibly shows its finite healing tonic restoring
   15HP. Player defeated it without losing health. The17-frame north EOF capture
   records ordinary attack/reaction states, not a Riposte.
2. Harvested FrostLichenPatch8970, acquiring lichen8997. Returned to the glade;
   the ordinary Crafting brew selected and consumed it, producing8998, a frozen
   coating. `forge-route-coating-brewed` records that actual output.
3. Walked across the real eastern boundary and toward forge7,19. A Scrabbler
   encounter interrupted the approach; Player used the equipped sword and the
   creature later died from bleeding. The24-frame east capture is preserved.
4. At6,19, unequipped2699, opened the adjacent forge, selected coating8998 and
   blade2699, and crafted. The coating was consumed. The same blade became
   **frost-quenched long sword**, then was re-equipped normally.
5. F5/F6 retained exactLongSword2699, TemperCount1, HPPenalty2, HP8/8 and the
   real `Frozen,40,,0,2` payload. Actual native Examine shows the neutral blade,
  40%Frozen, Cutting/LongBlades and the retained Bleeding effect. This is one
   tempering transaction; component assembly, reforging and a Frozen hit are
   separate unaccepted checks.
6. Traveled normally to Orven's kitchen at12,11. Harvested the existing ripe
   Claspbean at25,15 and picked up2pulp plus a saved seed. From25,14 selected
   “plant prepared bed(25,15)” in the seed action menu.
7. Read the carried Watering Grimoire, learned Conjure Rain through its normal
   action, and cast from25,14. Ordinary wait turns grew the same replacement
   crop13511. The actual native world forms and ripe Examine are captured.
8. Harvested again, picked up the output and F5/F6: crop13511 was gone, cultivated
   Grass12611 remained, pulp13509 increased2→4, and seed13514×1 was carried.
   Replanted that exact seed normally and F5/F6 again: new crop13515 is stage0,
   dry, with4pulp retained and no ClaspbeanSeed left in inventory.

| Actual capture | Tick | Crop | Stage | Growth units | Moisture |
| --- | ---: | --- | ---: | ---: | ---: |
| `kitchen-crop-watered-seed` |2550|13511|0|1|39|
| `kitchen-crop-grown-sprout` |2740|13511|1|0|20|
| `kitchen-crop-grown-ripe` |2940|13511|2|0|0|
| `kitchen-crop-harvest-reloaded` |2980|none|—|—|—|
| `kitchen-crop-replanted-reloaded` |2990|13515|0|0|0|

[Actual blade reader](Verification/Live16x24/forge-route-tempered-reader.png),
[ripe crop in world](Verification/Live16x24/kitchen-crop-grown-ripe.png),
[ripe reader](Verification/Live16x24/kitchen-crop-ripe-reader.png), and
[final replanted world](Verification/Live16x24/kitchen-crop-replanted-reloaded.png)
are unmodified Unity ScreenCapture outputs.

## Review, evidence and limits

`forge-kitchen-play-summary.json` indexes exact files/hashes and asserts sampled
state invariants. Independent `projection_cold_review` inspected PNGs and raw
parts/state receipts. Final bounded verdict: no significant new inconsistency
in temper custody/condition/native reader or crop growth/harvest/reload.

- 🔵 No production or test code changed in this acceptance packet. No RED/GREEN
  run is claimed. Earlier391 native LongSword tests are historical, not rerun.
- 🟡 Separate discovered presentation defect: east frame18's NPC lethal1 is
  logically attributed correctly but briefly overlaps Player's departing tween
  body. It is assigned its own focused RED/fix pass; this packet does not accept
  that frame as clean combat readability or change damage ownership.
- 🧪 No actual Frozen proc, Claspbean pulp application, full parry/Riposte or
  post-travel Player status damage was observed. All recorded attacks have
  Origin0/CounterDepth0 and Player remains42HP.
- 🧪 Selective EOF samples establish individual frames. They are not continuous
  video, normal frame pacing, physical controls or a Deck performance result.
- 🧪 Rain tooltip and historical farming-doc time terminology remain a named
  followup. No claim that those words changed here.

**Can verify:** actual ordinary keyboard route, screenshot identity/state,
selected persisted fields, finite material consumption, same blade and cultivated
bed, replacement crop growth, renewed seed consumption. **Cannot infer:** all
seeds/regions, every action/outcome, all six sites in one release run, packaged
activation, physical controller feel, current Deck performance or complete art.

## Cleanup and next action

Before stopping, root exported/hash-checked the finalF5 checkpoint to
`/tmp/coo-forge-kitchen-final-checkpoint` (receipt in this packet). It is kitchen
25,14,42HP,tick2990 with frost-quenched2699 equipped and dry13515 planted. Earlier
prepared/tempered checkpoints are also retained. Resume only the real character
folder; do not copy the zero-byte audit marker as a playable save.

At01:03:46UTC, Play was stopped, isolation/view/seed/preferences restored, normal
SampleScene open, and save-root override null. Direct console counts immediately
before stop:0errors,2existing warnings,124logs. No new package or current-build
replacement. Preserve all source, saves, backups and useful active caches.

Next: fix the measured tween/readout overlap with native RED→GREEN and an actual
ordinary replay; then remaining combat/status and complete six-zone continuity.
Keep graphics/storage heartbeat active. Push exact owned evidence first, verify
origin/main, then inspect safe stale cache candidates; deleting nothing is valid.
