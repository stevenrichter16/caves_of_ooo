# GroveSeep wet floor

**Status (2026-10-29):** bounded Editor-verified. The eligible GroveSeep in ordinary Grovelands (seed 64 Overworld.4.7.0 at 50,11) now sits on the approved Fen water floor instead of a black cell. Its sole live water is represented, so the `~` glyph is relinquished exactly as on a seep-less Fen cell. The seep's approved wet body (grovelands-materials frames 2/3) and reader portrait are unchanged. No art, mechanics, placement or population change.

## Decision

Three backings were compared (`grove-seep-06-backing-options-offline.png`):

| Option | Read | Decision |
|---|---|---|
| Black (before) | Interrupts the forest floor | Defect |
| Forest floor | Looks best, but a permanently wet cell would read dry, and the `~` glyph would remain on top | Rejected (handoff: no dry floor under the seep) |
| Fen water floor with banks | Coherent wet cell, glyph relinquished | Shipped |

## Runtime contract (`FlatPixelGrovelandsMaterialsCatalog.TryResolveWaterGround`)

One eligible GroveSeep may sit on the Fen water floor. Eligibility is its existing `ForegroundFrame`: approved body, usable well, sole live unmixed water. Every other pool, well, source, gas or film still refuses. Two seeps on one cell refuse, and a mixed or foreign seep keeps its fallback and its mark. The dry-ground query still refuses the wet cell.

## Tests (`FlatPixelGrovelandsMaterialsTests`)

| Run | Result |
|---|---|
| RED, verification copy (command line) | 5 intended failures; 2 unrelated bridge-log SetUp failures while its server was down |
| GREEN, verification copy | 111/111 |
| All `FlatPixel*` + camera + stair classes, verification copy | 3,966 passed, 0 failed, 3 existing allocation probes inconclusive |

**Deliberately revised pins:**
- `FenWater_PhysicalSeepSourcesKeepTheirOriginalWetBodyAndWave` became `...KeepTheirWetBodyOnTheFenFloor`.
- `SubmittedSeepKeepsItsActualWetOrHeatOverlayAndNoDryGrass` now expects the Fen water floor (never dry grass) and keeps the heat mark.

**New tests:**
- an isolated seep gets mask-15 banked water and never dry grass;
- an oil-mixed seep and a foreign seep keep their fallback and mark.

## Actual play (Editor, isolated saves, real keys)

Immutable `Fixtures/regional-fixtures194` was continued with a real C key: Overworld.4.7.0, player at 53,17, turn 4,130, HP 38.

| Capture | Shows |
|---|---|
| `grove-seep-01-continued.png`, `-02-readable-crop6x.png` | Seep on banked Fen water in the forest floor; no black, no `~` |
| `grove-seep-03-reader.png` | Look → examine: unchanged native portrait and water text |
| `grove-seep-04-saved.png`, `-05-fresh-continue.png` | F5 and fresh Continue: the world crop `(0,40,940,640)` matches exactly (0/564,000 pixels) |

Earned checkpoint: `Fixtures/grovelands-seep-floor200`.

## Limits and next

- At readable zoom the isolated seep reads as a small rectangular basin, a full-cell water tile around the ring. A future art pass could add a native damp-ground backing that blends into the forest floor.
- No packaged build includes this change.
