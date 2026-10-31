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

**Merchant camps.** These were first excluded because seven authority pins used a camp POI as their sample revoking mutation. The user then asked for camps to be included (2026-10-30); see the camps section below.

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

## Related wilderness slice: conjured flowers (2026-10-30)

**Problem.** `FlowerField` drew a magenta `*` on a black box across the Spread, including the 114-flower meadow at 6.13 and Lampwell. The census counted 629 sampled cells. Its own re-asserted `petals` residue made the Spread adapter refuse the grass beneath it.

**Art.** An original sprite: `ArtSource/Live16x24/conjured-flowers` (see its README).

**Runtime.** `FlatPixelSpreadEnvironmentCatalog.IsConjuredFlowers` accepts only canonical `FlowerField`:
- magenta `*` vegetation terrain;
- its own `FlowerCharmPart` and `LifespanPart`;
- a residue source of petals only, with no coating or energies;
- a cell holding nothing but that petals residue, if anything.

Such a patch draws its sprite over the real grass. Any other live state, such as water, keeps the fallback. If the sprite is unavailable, the patch competes for ground exactly as before. That keeps the existing pin `CharmFlowersNeverSubstitutesForAuthoredConjuredFlowerField` unchanged and passing.

**Tests.** `FlatPixelConjuredFlowersTests`:
- two zones, at least 95% native with a matching reader;
- own petals versus water;
- recoloured or charmless flowers and CharmFlowers are refused.

**Results.**
- Stub RED: 3 failures.
- The first GREEN run broke the CharmFlowers pin: `FlowerField` had been exempted from competing even without its art. That exemption was narrowed.
- Final: all `FlatPixel*` classes 3,785 passed, 0 failed, 3 inconclusive.

**Evidence.** `conjured-flowers-{meadow,lampwell}-offline.png` and `conjured-flowers-meadow-zoom4x-offline.png` (offline renders).

**Polish debt.** The dense meadow repeats the same clump and token in every cell.

## Related wilderness slice: Beating stone ruins (2026-10-30)

**Problem.** The western Beating's grey `StoneWall` ruins (`#` `&w` on sand, for example 0.15–2.18) were the largest remaining Beating glyph owner: 190 sampled cells.

**Change.** `FlatPixelBeatingMaterialsCatalog` reuses the approved straight shaded stone masonry, `sill-environment` frame 8, which StoneWall already wears in Sill. It loads it as an independent optional group. The rule has the same static shape as Sill's (`#`, Wall, Solid), plus the Beating's own conditions: intact, and neutral temperature when a temperature exists. SandstoneWall keeps its sandstone brick.

**Tests.** `FlatPixelBeatingStoneWallTests`:
- three zones, at least 95% native with the masonry frame;
- still blocking, with a matching reader;
- counter-check: a frozen or recoloured wall, or a changed glyph, keeps the fallback.

**Results.**
- RED: 3 failures.
- GREEN: all `FlatPixel*` classes 3,789 passed, 0 failed, 3 inconclusive.

**Evidence.** `beating-stone-ruins-0-16-offline.png`: before (top), after (bottom), an offline render.

**Still visible in the ruin.** A camp resident (`@`) and its wilderness campfire.

## Merchant camps (2026-10-30, user decision)

`FlatPixelSiteAdmission` now admits merchant camps as well as sinkholes. The world has three camps, all placed during generation: Overworld.8.2.0 and 14.13.0 in the Spread, and 15.18.0 in the Beating.

| Camp | Native before | Native after |
|---|---|---|
| 8.2.0 (Spread) | 280 | 1,981 |
| 15.18.0 (Beating) | 10 | 1,929 |

**Seven pins revised deliberately.** Each used a camp only as its sample revoking point of interest, and each now uses `POIType.Lair`, which is still excluded from sites, so the revocation each pin protects is still exercised:
- Beating `WarmAuthorityIsRevokedByTheCurrentMapAndRestoredWithoutCachingApproval("poi")`;
- Grovelands `CurrentWorldAuthorityCanRevokeWarmAdmission("poi")` and `FenWater_CurrentMapGraphAndSavedSiteAuthorityCanRevokeAFormerClaim`;
- regional fixtures `PipeBodyAndFloorCannotEscapeCurrentBiomePoiOrSavedLairAuthority`;
- shared vine `RevokedRegionalAuthorityCannotFallThroughToTheSharedSurfaceRoute`;
- Sodden `OrdinaryAuthorityReadsItsOwnCoordinatesAndRevokesWarmApproval`;
- Stump `CurrentMapMutationRevokesWarmAdmissionAndRestoringItRestoresArt("poi")`.

**Tests.**
- New: three camp cases, at least 90% native.
- Run: `FlatPixel*` plus the Overwrit/Ginmere, Spread presentation-scope, Sodden district and biome merchant-camp classes. RED (camps admitted, pins unrevised): the 7 pins above failed. Final: 3,894 passed, 0 failed, 3 inconclusive.

**Evidence.** `special-site-camp-{8-2,15-18}-offline.png`.

**Still glyphs in camps.** The camp merchant and its campfire.

## Remaining

- The Cathedral and Ginmere inner sinkhole floors (`StoneFloor`, which the Grovelands adapter does not draw).
- Lair surfaces.
- Live capture.
