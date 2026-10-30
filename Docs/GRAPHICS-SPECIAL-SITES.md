# Special-site surfaces

**Status (2026-10-30):** sinkhole surfaces implemented and regression-tested. Evidence is offline renders of the real seed-64 zones drawn by the real presentation catalog; there are no live captures yet.

## Problem

A sampled world census (`Docs/Verification/Live16x24/world-surface-census-2026-10-30.txt`, every other zone, liquids loaded) found the surface graphs of special sites almost entirely glyph. Every biome adapter admits only point-of-interest-free wilderness.

An earlier census run without `LiquidRegistry` reported about 5,900 Sodden mire cells as glyph. That was an artifact: the legacy `water` source resolves to `bog-mire` only through the registry, and with liquids loaded only 91 remain.

## Change

`FlatPixelSiteAdmission.IsOrdinarySite(zone, biome)` admits the current cached surface graph of a **sinkhole** in the given biome. It excludes:
- authored Places;
- lairs and saved lair columns;
- unique sites such as the Root and the Felling-Site;
- the caves below.

Each biome adapter (Spread, Sodden, Beating, Grovelands, Stump) gains an internal `AdmittingSites` switch. The greybox builds a separate, identically ruled instance for its biome's sites. Every ground, plant, wall and fixture therefore uses exactly the rules and approved art the wilderness uses. Public wilderness admission is unchanged.

One supporting fix: Sodden's fen-ground variant read the zone's map coordinates from the wilderness table. It now uses `WorldMap.FromZoneID`, which gives identical values and also works for sites.

**Merchant camps are deliberately excluded.** Seven existing authority pins use a camp POI as the map mutation that must revoke wilderness art. Admitting camps would mean revising those pins, which needs its own decision.

| Sinkhole | Biome | Native before | Native after |
|---|---|---|---|
| Stillleaf | Stump | 10 | 1,930 |
| Ginmere | Grovelands | 5 | 1,755 |
| the Deepest Cathedral | Grovelands | 3 | 1,756 |
| Lampwell | Spread | 306 | 1,858 |
| Spivenor | Sodden | 445 | 1,615 |

## Tests

`FlatPixelSpecialSiteTests`:
- five sinkholes at least 75% native;
- a site belongs only to its own biome, and wilderness admission is unchanged;
- settlement, lair, wilderness and the Root are never sites;
- the cave below a site is not the site.

**Results.**
- RED (stub admission): 5 failures.
- First GREEN run, with camps admitted: 8 failures. Seven were authority pins using a camp POI, so camps were dropped; the eighth was the Sodden coordinate lookup, now fixed.
- Final: all `FlatPixel*` classes 3,775 passed, 0 failed, 3 inconclusive.

## Evidence

`special-site-{stillleaf,ginmere,cathedral,lampwell,spivenor}-offline.png`: before (top), after (bottom).

## Slice 2: the sinkhole lip (2026-10-30)

**Problem.** The solid `SinkholeLip` ring of about 60 cells per mouth drew as grey `#` glyphs.

**Art.** A new original rim block (`ArtSource/Live16x24/sinkhole-lip`): a moss cap, a strata rock face and an undercut base, in two frames.

**Runtime.** `FlatPixelSinkholeLipCatalog` admits only `FlatPixelSiteAdmission` sinkholes in any biome. It accepts only the canonical lip:
- `#` `&y`, layer 2;
- solid terrain;
- no visual override, damage, state or foreign meaning.

The lip stays solid. The greybox wires it as a foreground and reader.

**Tests.**
- Five lip cases: at least 95% native, still blocking, reader matches.
- One counter-check: a lip placed in wilderness, and a recoloured lip, keep their glyph.
- **RED (stub catalog):** 5 failures, 0 of 60 in each mouth.
- **GREEN:** all `FlatPixel*` classes 3,781 passed, 0 failed, 3 inconclusive.

**Evidence.** `special-site-lip-{lampwell,cathedral,stillleaf}-offline.png`: the previous slice (top), with the lip (bottom).

## Remaining

- The Cathedral and Ginmere inner sinkhole floors (`StoneFloor`, which the Grovelands adapter does not draw).
- Merchant camps and lair surfaces.
- Live capture.
