# Sill StoneWall shade — independent bounded review

2026-10-28. Read-only source and actual-image review; the reviewer did not run Unity, native tests or ordinary input. Only this report was written.

## Evidence inspected

- [Matched before](wall-shadow-before186.png) and [matched after](wall-shadow-after186.png), earned checkpoint 186, player at (39,11), ordinary 1280×800 view.
- [Before detail](wall-shadow-before-detail.png) and [after detail](wall-shadow-after-detail.png): nearest-neighbor 4× enlargement of actual crop (326,200)-(614,248), not new artwork.
- [Actual submitted shade receipt](wall-shadow-after186-shade.json): 20 current visible/explored StoneWall sources, 20 disclosed Grass receivers, approved sill-environment atlas, unlit material, sorting order 0, black alpha 0.16 and southward offset 2/16.
- Root's matched comparison reports 640 changed pixels, exactly 20 strips of 16×2 pixels, at rows 184–185, 232–233, 376–377 and 472–473, with no UI differences. The reviewer visually compared the images; this report does not claim an independently rerun pixel-count calculation.

## Visual finding

**No actionable P1/P2 observed. Accept this specific comparison as a restrained Sill StoneWall shade pilot.** The new strip touches the wall's southern edge, remains aligned with its width and joins continuously across adjacent wall cells. It does not appear as detached duplicate masonry, a floating halo, a scalloped stamp sequence or a raised floor slab. The central service approach remains visually open and unchanged; the shade does not bridge the entrance gap or spread over the courtyard.

The enlarged crop makes the grounding effect clear. At normal zoom the change is a quiet edge under the masonry, more legible across the long wall runs than the earlier single-player contact mask, but still modest. It provides enough shallow relief for this first wall-family trial; there is no visual reason in this pair to enlarge or darken it further. It does not transform the whole scene or establish broad lighting quality. Screen-north light is an artistic convention, not verified solar or torch behavior.

## Source and native bounds

Reviewed `EnvironmentSpriteRenderer.FlatShadow.cs`, its core renderer hooks and `FlatPixelSillWallShadowTests`. The helper consumes the already accepted current selected native StoneWall, requires a disclosed dry clear southern receiver, and borrows the existing opaque wall tile. It does not add a second owner-selection path. Source anchoring, the separate layer below the wall/above the floor, and full/incremental claim clearing are coherent. Pause, travel, legacy mode, option changes and disposal have cleanup paths; reserve-bed twine stays separate. No recurring per-cell allocation or world scan was apparent in source; this is not a measured allocation result.

Root retained 25 genuine native RED failures before implementation, then 225 related cases: 224 passed, 0 failed, 1 pre-existing unsupported allocation-counter probe skipped. The reviewer did not execute those runs. Native checks cover StoneFloor receivers, invalidation, real destruction and dirty/full parity; the matched actual receipt reviewed here contains **Grass receivers only**.

Ordinary route/restart checks, performance evidence, other receiver materials, different lighting, other wall families/biomes, packaged delivery and physical Steam Deck acceptance are outside this review. No whole-Sill or whole-game acceptance is implied.
