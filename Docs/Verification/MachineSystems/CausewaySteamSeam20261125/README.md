# Follow surface steam to the stone below the Causeway

Fresh seed 64 generation v29 adds one native steam vent beside the Sodden
Causeway at overworld `(17,9)`. It uses the existing coating, heat and
finite catch rules. The same down stair and exact return stair carry the
route. The lower stair retains its ordinary description and adds the local
mineral stain. The clue is available through ordinary Examine and takes no
tool or turn. The placement reserves no ground, moves no terrain and adds
no mining or catch source below.

## Ordinary play

| Tick | Result |
| --- | --- |
| 170 | Arrived on the surface at `(40,12)` from the world map with a normal Duelist pack. |
| 230 | Reached native down stair `7581` at `(34,11)` in six dry moves, matching the prior v28 route length. |
| 260 | Read the fissure and pale crust on vent `7635` at `(34,15)` from dry `(34,14)`. The lower zone was still uncached and the vent held no initialized stock. Saved from the shoulder. |
| 300–310 | Descended on the native stair. Landed on native `StairsUp` `11092` at `(34,11)` and examined its matching mineral stain from one step south. |
| 330–360 | Returned through the same stairs and saved on the shoulder. Player health stayed at 40. The cave bear was still present to the north. |

## Shared stock with supplied field tools

After saving the ordinary round trip, the private continuation was given a
canonical condenser, empty flask and scraper. A spare empty flask was
supplied after exhaustion to distinguish a spent vent from an unavailable
receiver. Equipment acquisition used this controlled setup. The separate
[ordinary Cinderhold and Sill route](../OrdinaryCondenser20261125/README.md)
covers acquiring a condenser and first flask.

At tick 370 the native Condense action put one water into flask `11154` and
left four of vent `7635`'s five catches. Four native scrapes took one scale
each. At tick 410 the same vent held zero catches. Examine still worked,
Scrape showed its spent state and Condense was absent with an empty spare
in the pack. A fresh Continue from the tick 410 save kept the exact vent,
the exhausted balance, the original water and all four scales. The same
physical round trip at ticks 450 and 460 kept the supply exhausted and
returned at full health.

## Verification scope

- The RED run had three expected missing-vent failures and one passing
  version 28 control. All 168 focused native checks passed on the feature,
  including both first-access orders, the original v28 graphs, source
  removal without regeneration, and existing tool and generation checks.
- Independent source review checked the native stairs and adjacent heat
  guard. Independent gameplay review checked reader screenshots, raw
  unassisted checkpoint identity and the paid tool branch evidence.
- Actual input verified the ordinary paired route in seed 64. The test
  resolves the exact native stair on cave-first generation. Additional
  seed layouts, ambient long-range visibility and a distinct downstream
  cave process are open. The Mac package qualification remains at the
  previously recorded build. The full native regression remains open.

[Surface fissure](01-surface-fissure.png) · [Cave stain](02-mineral-stain.png) ·
[Scraper option](03-scrape-choice.png) · [Observations and receipts](observations.json)
