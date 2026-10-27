# World sprite coverage: read-only code audit, 2026-10-27

## Decision

Prioritize approved portable equipment continuity first: exactly **LongSword, LeatherArmor, Hatchet, Cudgel** keep the same native identity in ordinary world, carried/equipped inventory and authorized examine outside the opening. This requires no new art or animation. It closes a real source-proven travel inconsistency while regional static kits are prepared. Do not globally admit regional terrain/services to accomplish it.

The unfinished East animation fixture and its metadata were moved, byte-for-byte, to `Tools/Live16x24/DeferredTests/FlatPixelCindercallerMotionTests.cs[.meta]`; no runtime/atlas was implemented and no native RED was run. SHA256: test `df8c545f699e667deb0cba25a12e714d2ef17681eedc04707d43fbe94e0cf009`, meta `a8c5fea1ef7945a99313199b3e9148c64c35db2d605c5aea251daaab1c6ea15f`.

## What is actually covered

- `FlatPixelGreyboxCatalog.IsAdmitted` (~184) admits six graphs only: glade11.10.0, cellar11.10.1, North11.9.0, Nella11.8.0, East12.10.0, kitchen12.11.0, with existing Spread/current-world/POI gates. Continuous flat projection is broader than native artwork coverage.
- `TryResolvePresentation` (~767) and `TryGetPresentationSubjectSprite` (~853) gate native regional actors, equipment, crops, loose products and props on that local admission. Only the four semantic identities in `FlatPixelActorMotionCatalog.IsPortableActor` (Player, PetDog, MarlbackScrabbler, MarlbackGleaner) may use opening native art elsewhere. The approved Patchbearer adapter remains local. Thus a carried LongSword or Cudgel can change to a glyph solely because the viewer traveled.
- `FlatPixelFallbackCatalog` deliberately preserves actual CP437 glyph/color in16x24 cells, including conservative remembered terrain. Its generated atlas is256x408. This is working interim fallback, not missing-texture corruption and not native identity completion.
- `EnvironmentSpriteRenderer.ResolveCell` (~1152–1180) returns from the flat path before old environment sprite logic. Older art files therefore cannot be counted as current flat coverage.
- Read-only file inventory: **36 Live16x24 PNGs**, **35 distinct literal resource paths across20 FlatPixel catalog .cs files**. Old `Sprites/Environment` has479 PNGs:474 are16x16, two64x16, two128x128 and one16x24. `Sprites/Actors` has27 sheet/image files. These are resource counts, NOT owner/state/biome coverage percentages.
- Existing `/tmp/coo-sixsite-census-observer.cs` observes actual paint receipts, selected owner, native/fallback sprite path, underlay/hazard/decoration and active body. It avoids asking resolvers to repaint. It is Editor-only, finite/on-screen and conservative; covered/hidden owners, all seeds, all lifecycle states and unvisited regions are not proven by its totals. Reuse after fresh capture rather than creating a new coverage framework.

## Concrete gaps by area/family

| Area | Source-backed current gap | Next evidence needed |
|---|---|---|
| Any surface/underground outside six graphs | Approved carried/loose equipment and most products revert to CP437; regional material ground, walls, plants, NPCs and props lack a matching native route | Actual border travel carrying/drop/equip/examine known items, then finite visible-owner census in one representative region |
| Remaining Spread / authored settlements and work sites | Same familiar identity may lose native art on departure; Wickrush crop/seed/candle and many equipment/manual families have no native mapping | Capture a real non-opening Spread site; audit exact inventory and functional prop states before choosing next packet |
| Sodden | No exact FlatPixel crop mapping for Sumpsieve,Drowsebell,Chillcress,Slipsedge,Peatlantern; biome actors/materials/services remain outside local art admission | One playable wetland route, dry/wet ground and dangerous liquid distinction, harvest→product→use |
| Grovelands | Choirwick,Knitmoss,Sourmantle,Murmurpod,Sealbark have no exact native crop mapping; jungle/forest identities are legacy-only/fallback in flat mode | One actual forest site; prioritize traversable vs blocking vegetation and distinct actor silhouettes |
| Stump | Raingourd,Prismreed,ScarletSundew,Cloudwick,Gripfrond have no exact native crop mapping; older16x16 canopy/tepui assets are source references, not accepted flat sprites | Actual vertical-route/landing site, cardinal boundaries and hazards |
| Beating | Sunbladder,Shalebean,Shadefan,Cinderpea,Spurgrass have no exact native crop mapping; desert/heat identity is outside opening kit | Real desert route, cactus/sand/stone/heat roles and one distinct encounter |
| Overwrit | Absentmint,Margincress,Binderroot,Greybladder,Hollowchime have no exact native crop mapping; library/record/ruin materials and services need site-specific inspection | Actual special-site scene and interaction/removal/state requirements |
| Underground / sinkholes / authored depths | Only exact GleanersCellar floor/fixtures/Sootroot have scoped native coverage; Lampvein,Knucklecap,Veilpuff,Brinebutton lack exact mapping; other wall/material/actor families fall back | First non-opening cave/material band with real stairs, mining/blocking, harvest and return |
| All inventories / progression | `DitchkeepersFootwork` and `WateringGrimoire` have real Grimoire mechanics but no FlatPixel catalog mapping; many weapons/armor/reagents remain glyphs | These two encountered manuals are a small next ORIGINAL static-art packet after portability; no broad book aliasing |
| Creature outcomes | Exact native aftermath is Magpie/Viper `CreatureCorpse` source metadata plus VenomGland; MarlbackCorpse separately native. Other corpse source identities are not covered | Tie each new actor packet to its actual corpse/loot identity, not a generic body promise |

The35-species `BiomeCrops.json` registry has exact native crop-name references for only Drawgourd,Claspbean,Pitchpod,Marlroot and Sootroot. This is a static exact-name count (5 of35), not a gameplay acceptance percentage. CandyCarrot/Emberwheat exist separately; inherited/generated definitions and all states require runtime checking. Topology/content audit by seed65_route_audit independently found400 canonical surface cells across six biome tables,17 settlements,6 sinkhole mouths and generic depth/material bands; consult its source anchors rather than treating712 raw blueprint definitions as a sprite denominator.

## Minimal first implementation and RED boundary

Production touchpoint should stay `FlatPixelGreyboxCatalog`:
1. Allow `IncidentalEquipmentArtFor`/its existing exact semantic resolver outside local admission in world and subject routes. That catalog only recognizes LeatherArmor/Hatchet/Cudgel and already validates owned visible Render/Physics, Item, single-cell, equipment role, positive stack and no explicit VisualID/variant/glyph reskin.
2. Permit **exact LongSword** through the existing opening art resolver outside local admission, retaining its current semantic eligibility. Do not turn all `Item` into global native art; do not let the broad opening resolver admit Warden, walls, wells, crops or service actors elsewhere.
3. Keep `IsAdmitted`, `LocalGround`, optional texture behavior, four portable actor identities, current-graph/canonical selection and `ExaminePortraitRenderer.IsCurrentPresentationSubject` unchanged. Ordinary document authority remains separate from semantic sprite selection.

Reuse existing helpers, no new framework:
- `FlatPixelGlobalPresentationTests.Fixture`: actual CameraCleanup/Awake graph, `Show`, `Frame`→LateUpdate, `Foreground`, `Document`, `Portraits`, exact current manager; existing outside Spread/Sodden/underground and local controls.
- `FlatPixelIncidentalEquipmentTests.Reader`, real pickup/equip/drop and transfer/same-ID stale-document cases (~266,310), plus owned-role/footprint/resource counters.
- `FlatPixelLongSwordTests` and `FlatPixelLongSwordAdversarialTests`: neutral modified blade, exact artwork, missing resource, aliases/reskins and current custody.
- Existing counter assertions intentionally expect regional fallback at `FlatPixelIncidentalEquipmentTests.FogMovedEntriesReplacedGraphsAndOtherRegionsCannotGainNativeDisclosure` and `FlatPixelLongSwordAdversarialTests.FogAndDifferentRegionDoNotGainNewSwordDisclosure`. Update only the outside-region native expectation/name after recording new RED; preserve their fog/moved/stale-graph controls. HealingTonic outside fallback in GlobalPresentation remains unchanged by this four-identity slice.

Required positives: each of four exact factory items uses the same existing16x24 sprite in canonical local, non-opening Spread, Sodden and underground world; matching carried/equipped/examine uses viewer's current graph. Required paired counters: unknown blade/armor, explicit reskin or foreign functional part, hidden/remembered owner, stale/equal-ID graph, transferred/same-ID forged custody, missing atlas, no regional ground/service admission. Assert asset identity/rect and canonical owner, not merely nonnull.

Gameplay acceptance: carry the real current equipment across one ordinary unadmitted border; world drop/pickup/examine and inventory at1x/2x; save/load and return; compare unchanged terrain/known service fallback. Then capture representative new biome site and author its static kit through the required capture→imagegen→parts→native-source→integration loop. No optional East cast/motion implementation during this coverage phase.

## Limits / review

Read-only source/resource audit, not a fresh all-world Unity census. No Unity/test/build/Git operation or production change was performed. Only the two explicitly authorized deferred test moves changed repository files. Existing approved art and runtime were preserved. No claim that a regional kit, Linux execution, physical Deck, performance target or all-world graphical completion is accepted.
