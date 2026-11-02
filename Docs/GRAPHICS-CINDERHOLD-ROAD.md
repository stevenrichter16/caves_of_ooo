# Cinderhold packed-road material correction

The settlement's broad road network previously looked like green ground. Exact Cinderhold road owners now reuse approved packed-earth frames 32–33 from `beating-materials`, making crossings and building approaches distinct from grass. This is material polish of an already native family, not new ASCII coverage.

## Contract and tests

Admission requires the current canonical Cinderhold graph at Overworld.6.6.0, Village/Cinderhold/PruningPost identity and no saved lair. Live ground, memory and exact terrain readers share the same selection. Existing wall contact shadows require the actual road resource. Marrowstye, Gantry and Tally behavior remains covered by controls. No geometry, collision, gameplay, save schema, RNG or shadow strength changed; legacy remains reversible.

Eight new road cases failed genuinely before implementation; all 98 selected road/settlement-ground/Cinderhold-shadow cases passed afterward. Because the accepted road and sandstone wall now intentionally share an atlas, the older missing-road fixture removes only the road views while preserving valid wall views. This keeps the counter-test meaningful. Source review found no actionable P1/P2.

## Actual gameplay

GameBootstrap at 1280×800, normal1×: matched fixture31,9 HP40 Dusk changes135,988world pixels and zero right-UI pixels. Actual adjacent30,9 Look and Examine identify Packed road and display its native portrait. Up into the sandstone wall remains blocked at31,9. Walked to33,11, ascended toWorldMap36,9 and descended toCinderhold33,11. F5 saved turn1880; a fresh isolated Continue restored33,11 HP40 Dusk with0/639,200world pixels changed. Log UI differences are expected. No F12 or injected movement/visibility.

The431.153-second after-only Editor profile includes tools, travel and Continue: rendererp95 .084292ms/max88.466334ms; Mainmax891.256583ms. This is an observation, not matched performance attribution or R11 acceptance.

## Bounds and next action

Independent visual review accepts improved packed-route separation with readable entrances, actors, campfire and existing shadows. Stepped widths, isolated grass cutouts, repeating flecks, sparse open spaces and green regional cast remain. Four existing buildings and an occupied office make additional random clutter inappropriate. No full-town/interior, population or new shadow acceptance is claimed.

Evidence, targeted receipts and AfterCheckpoint are in `Docs/Verification/GraphicsExecution/CinderholdRoad`. Reproduction: isolated native Play; copy its Quick pair using InternalGameID9ba2c3d179134acc9f898addede41f94, Continue, compare33,11. Do not replace normal saves. Current61cc packages exclude this slice pending the next batch. Next fill the107-coordinate Beating WorldMap ASCII region with approved sand; then review the documented sparse Beating RuinField19.12 margin composition. R11 and physical Linux/Deck execution remain open. No storage deletion.
