# Sill envelope independent source review

Reviewed: 2026-10-27T04:31:41.574359+00:00. Reviewer: portable_equipment_final_review.

## Result and bounds

One significant disclosure defect was found and corrected; no additional
significant finding remained in this bounded source review. The later root-owned
native results and actual captures were independently inspected as recorded
below. Packaged delivery and whole-Sill/world fullness remain unaccepted.
This reviewer did not operate Unity or execute the native suites.

Scope: exact Sill graph/POI; new VineWall cardinal silhouettes, ordinary
VillageDoor axes/open-closed states and Grass materials; immutable StoneWall,
AlchemyStill and StoneFloor pixel reuse. No generation, save schema, collision,
service behavior or new animation is introduced by this adapter.

## Finding, RED evidence and saved correction

**P2 — fixed: hidden/invalid competing owners changed the visible floor.**
`TryResolveGround` originally applied `CompetingGround` after only a spatial
membership check. A hidden current WellGroundMarker or SpikeTrap could therefore
remove native grass beneath itself, revealing its presence even though the
foreground selector refused that owner. Foreign Render/Physics ownership or
carried/equipped stale entries could also suppress material.

The reviewer supplied two parameterized tests. Root applied and executed them
before the correction. This reviewer read the retained raw file
`sill-envelope-hidden-ground-native-red.xml`:52 total,50 passed,2 failed.
Both failures are the expected hidden-owner ground substitution, with native
`sill-envelope` replaced by `Live16x24 World Glyph Fallback`; neither is a setup
or compilation failure.

The saved correction at `FlatPixelSillEnvelopeCatalog.cs:94` applies competing
owner vetoes only after `VisiblePhysicalOwner`. That helper at lines216–222
requires an owned visible RenderPart, owned PhysicsPart, no carried/equipped
custody and a positive owned stack when present. Spatial currentness still
comes from `Placed`. Malformed actual Grass/StoneFloor candidates retain the
existing conservative ambiguity rejection. Existing actual tile-state hazards
and cultivated-ground precedence remain intact.

The tests use the same visible Grass and competing owner through hidden,
visible, foreign Physics parent, restored Physics, carried, equipped, foreign
Render parent, restored Render, and removal states. Hidden/invalid cases must
retain the same original native ground tile; valid visible cases must veto it.
The hidden case additionally pins unchanged ground color. Both a settlement
marker and real TriggerOnStep trap family are covered.

## Additional source checks

- **Scope:** exact address plus live nonnull WorldLocationContext, current cached
  graph, expected authored Spread biome and Sill/Villagers/Village POI. This does
  not expand the existing six-scene `IsAdmitted` rule or adopt another settlement.
- **Owner truth:** foreground recognition uses exact identities, owned visible
  parts, non-custody physical state, single-cell geometry and semantic guards.
  Walls require actual intact structure/solidity; custom VisualID/VisualVariant
  and contradictory roles refuse the new art.
- **Doors:** frames follow owned DoorPart.QuarterTurns and lock-derived IsClosed.
  Malformed axes and separate repair/Morrowfast/archive owners refuse ordinary
  door art. Queries do not alter glyphs, solidity, permissions or turn costs.
- **Ground:** actual Grass/StoneFloor owners supply material below covering
  actors/items. The adapter does not infer floors from buildings or concepts.
  Duplicate floor ambiguity and specific actual ground/hazard precedence remain
  conservative. Native foreground is cleared only for the selected actual floor.
- **Disclosure and memory:** the new kit is visible-only. Vine connections require
  visible/explored, selected eligible cardinal neighbors. Hidden or covered
  neighbors do not supply a connection. Existing remembered/fog policy remains.
  Portrait selection is downstream of existing document authority and further
  restricted to the current selected world subject; it does not grant custody.
- **Tile selection:** separate Sill output storage avoids clobbering a successful
  portable equipment tile. Sill and the opening adapters do not overlap admission.
- **Dirty behavior:** the added HedgeContinuity test exercises actual vine
  destruction through the global dirty radius and checks adjacent end caps,
  traversable gap and unchanged full-pass count. A subsequent quiet step checks
  that a fresh change is not repeatedly resolved. The final retained native run
  includes the32-case HedgeContinuity fixture.
- **Resources:** missing/malformed injected groups do not reload Resources.
  Groups are independently available. Sprites/tiles are cached and disposed;
  borrowed textures and selector are neither destroyed nor mutated.
- **Cost:** CellOccupants uses a struct enumerator; warm lookups use existing
  context/map/part structures and cached sprites. No obvious warmed-query heap
  allocation was introduced. Repeated scope/owner scans add bounded CPU work;
  only root's actual measurements can establish performance acceptability.

## Later native and actual-evidence check

After root finished the run, this reviewer read the retained XML:
`sill-envelope-final-native-green.xml` is136/136 passed (Sill52,
HedgeContinuity32, GlobalPresentation52), including both new hidden-ground
counters. Earlier `sill-envelope-native-regression.xml` is224/224 passed
(Boundary60, CellarFloor42, ExaminePortrait42, GlobalPresentation52,
WorkingCourt28). That broader run preceded the narrow disclosure fix. These
suites overlap; they are not360 unique tests.

I visually inspected actual captures02,06,07,09,10,11,14,18 and20. Both door axes
read distinctly, the east open/closed passage changes visibly, the north reader
uses its closed sprite, and the actual stone interior differs from grass. I
also inspected12/16/17: they are pause/cellar/WorldMap respectively, despite
ambiguous filenames, and must not count as Sill route acceptance. The rewritten
`Docs/GRAPHICS-SILL-ENVELOPE.md` records the exact capture/action bounds.

The matched-zoom reload receipt restores seed65 tick1070 at Sill67,18 with the
same338 selected-cell and ground-count maps as the earlier riverbank receipt.
PNG20 visually shows the same bank composition. Live Screen fields differ,
although both exports are1280×800, so this is state/count correspondence rather
than pixel-identical proof. Root retained the earned checkpoint and restoration
receipt; the latter records preserved normal/foreign saves, preferences and
current builds, stopped Play and no cleanup deletion.

The inspected profile is a wrapped131,072-sample tail of a183,368-frame,
350.979-second Editor window with idle/tool time. ZoneRenderer mean0.1068ms,
p990.2323ms; draw-call mean35.706, maximum39. This is neither a controlled
speed comparison nor standalone/Deck performance evidence.

**Fullness remains open:** exposed grass lanes/bank look repetitive and
underfurnished; furniture/actors/services remain glyphs and the bank contains120
selected WaterPuddle fallback cells. Dark fog and remembered glyph areas are
not bare-terrain evidence. Packaging, verified main push, remaining Sill/world
coverage and physical Deck acceptance remain root-owned/outstanding.

## Reviewed source hashes

- `Assets/Scripts/Presentation/Rendering/FlatPixelSillEnvelopeCatalog.cs` SHA256 `845d703b8cbabfe792f71761387ffe330ef4b7e1acbf16222fcd74ea98da7d46`
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` SHA256 `46398686fd9a2946891ae7db66276e85d44992b30993b85a6b2ec60e1d6bb7db`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSillEnvelopeTests.cs` SHA256 `e9d72db569d5796aeb1745cd074a23bc3c632d07bf0cd3c8217190c02dbdb776`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelHedgeContinuityTests.cs` SHA256 `84cbe94e464beea25d50b6b2512af11cb3eac085033aa9f2a350ada24d0638d9`

Reviewer operations: read source, retained XML/JSON and actual PNGs; authored
the requested scratch RED patch, this review receipt and the parent-requested
Sill integration document. No Unity, Git or runtime/art edits.
