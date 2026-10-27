# Ordinary slate caves — source-only material preflight

**Status, 2026-10-27: source audit with one actual depth7 entry/return baseline.**
Root reached depth7 through ordinary travel in the fresh shale Mac build;
the narrow entry view still uses dark fallback glyphs. Depth8, wider routes,
census, readers, concept/parts study, native art, runtime RED/GREEN and slate
delivery remain pending. This does not accept whole-area composition. Root
owns Unity, gameplay and Git. Shared storage is the next implementation family;
slate remains queued, with the earned entry preserved for later work.
The preceding [shale material phase](GRAPHICS-SHALE-CAVES.md) has its own bounded
Editor evidence; that evidence does not accept slate or the remainder of the
underground. No existing area-polish status changes.

The candidate slice is exact `SlateFloor` and `SlateWall` in eligible current
ordinary depths7–8, carrying forward unchanged Rubble, Stalagmite, StairsDown
and StairsUp only when their existing owner/state contracts hold. A provisional
six-cell96×24 atlas (four quiet floors, two wall variants) is sufficient to
study first. Frame count and final palette remain contingent on actual views;
this document does not author a production adapter or sprite.

## Verified corrections before implementation

Every production source linked below was read for this preflight. This is an
original Caves of Ooo presentation change; there is no Qud parity claim or
planned simulation/generation change.

| Assumption to avoid | Verified source and required interpretation |
| --- | --- |
| Slate is another name for shale or any deep wall | [SolidEarthBuilder:42](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs:42) selects exact SlateWall/SlateFloor at7–8, after shale5–6 and before quartzite9–10. Use exact canonical depth scope and exact owners; do not promote generic Wall, VineWall or another stratum by glyph. |
| Copy shale's28HP/hardness2 | [SlateWall:8584](../Assets/Resources/Content/Blueprints/Objects.json:8584) overrides structural HP/MaxHP to38, hardness3 and WreckageBlueprint=Rubble. A surviving original wall can remain native at any real HP in1–38; damage must not demand full health. Gone/indestructible/custom maximum, hardness or wreckage retain truthful fallback. |
| Slate wall means a new mineral simulation material | It inherits [Wall:3280](../Assets/Resources/Content/Blueprints/Objects.json:3280): solid Physics, layer0, Stone/Mineral Material, combustibility0 and Thermal (flame threshold1200, heat capacity3). Slate changes the image/blueprint description, not MaterialID. Preserve owned unique parts and the current neutral thermal/status gates. |
| Floor and wall share shale's glyph/color contract | SlateWall is `#`, color`&b`, background`^k`, layer0. [SlateFloor:8875](../Assets/Resources/Content/Blueprints/Objects.json:8875) is comma, color`&b`, GlyphVariants=three commas followed by dot/apostrophe/backtick. It inherits passable [Terrain:3438](../Assets/Resources/Content/Blueprints/Objects.json:3438), layer0, without authored Material, Thermal, Destructible or background. Do not add those parts or mistake the comma for Bones/Rubble. |
| Existing slate images are a16×24 wall kit | The sixteen `Environment/slate_m00..15.png` files are opaque16×16 floor macro slices. [EnvironmentSpriteRenderer:527](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:527) describes their64×64 spatial field; [mapping:567](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:567) keys SlateFloor by blueprint and [loader:797](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:797) loads all16 position suffixes. These are not adjacency masks or wall frames. |
| Depth7/8 proves a natural cavern recipe | [UndergroundLayoutPlan:37](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs:37) can select ordinary roomed layouts at depth3+ for Spread/Sodden/Beating using a private25% sample and content checks; other eligible columns retain Strata. The plan is cold generation input, not saved render provenance. Current source/owner admission must work conservatively in either layout. |
| Every traversable cell has an actual floor entity | [StrataBuilder:34,99](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:34) uses seed60/passes3/noise0.42 at5–8, and floor rolls85% floor,10% Rubble,5% empty. Preserve an owner-free, disclosed ambient backdrop rather than inventing SlateFloor owners or allowing black pit-like holes. Counts are generation probabilities, not a slate census. |
| An ordinary column excludes room props or hazards | [CreateUndergroundPipeline:580](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:580) follows geometry with connectivity, stairs, optional route reservations, landmarks, hazards, population and containers. Current ordinary eligibility does not certify every owner as a slate material. |
| Existing depth-band eligibility proves historical generation | [Shale IsSupportedZone:69](../Assets/Scripts/Presentation/Rendering/FlatPixelShaleCavesCatalog.cs:69) validates the current cached graph, live map/POI and saved-column exclusions. This intentionally makes no claim about an edited/restored graph's unsaved original recipe. Preserve that distinction for slate. |
| A dark color identifies a wall or allows hidden occupancy checks | Selection must still use the actual visible owner, placement, disclosure and part semantics. Hidden, remembered, foreign, carried or equipped objects must not affect a visible empty-cell backdrop or disclose their presence. Raw occupancy count and BlocksMovement are unsuitable backdrop predicates. |
| Completing two material owners completes the area | Native materials leave actors, storage, hazards, mineral nodes, generic walls, room furnishings and services separately scoped. Whole-chunk polish remains a later explicit biome/area review; native frame and object counts are not acceptance. |

## Existing art and the proposed material direction

Read-only decoding of all sixteen slate macro images found4096 opaque RGBA
pixels, three exact colors and no transparency:

| RGB | Hex | Pixels across16 files |
| --- | --- | ---: |
| 78,82,96 | `4E5260` | 3243 |
| 86,90,104 | `565A68` | 550 |
| 68,72,86 | `444856` | 303 |

The inspected [slate_m00 importer](../Assets/Resources/Sprites/Environment/slate_m00.png.meta)
is Point,16PPU,no mipmaps,clamped and uncompressed. Preserve all16PNG/meta pairs
unchanged. Centering a square opaque floor leaves uncovered space in a16×24
cell; stretching its pixels or reinterpreting macro slices as masks is not the
proposed solution. Original native16×24 floor reconstruction can retain this
blue-gray palette while using much quieter broad planes and narrow seams.

The source descriptions distinguish dense, dark, flat-cleaving slate with a
faint blue cast from shale's broken laminae. A starting art proposition is
close-fitting broad planes on walkable floor and a distinctly solid compressed
wall mass, with restrained long-plane breaks rather than the shale kit's short
stacked flakes. This is a source-grounded direction, **not an accepted design**.
Actual Cave grading must establish floor/wall collision contrast, low-contrast
repetition and actor/stair separation before finalizing colors. Do not copy
shale pixels, add raised edges, import generated gradients, or brighten fog.

If actual captures support the finite six-frame plan, use the established
editable integer-cell Blender/native pipeline: exact16×24 square pixels,
opaque material tiles, consistent family borders, Point imports and read-only
pixel/geometry/reproducibility verification. Preserve prior accepted art and
all concept inputs. No source kit or generator is created by this preflight.

## Actual packaged entry baseline — 2026-10-27 19:42 UTC

The [baseline receipt](Verification/Live16x24/slate-caves-before-play.json)
records source `a10e1330098c68039e560740cde07137dadc2ff3`. From freshly Continued
fixture97 at depth5,40,13,HP33, normal `>` reached6; Up to40,12 and `>` reached7.
F5 preserved [fixture98](Verification/Live16x24/Fixtures/slate-caves-entry98/Quick.json),
turn12620,HP33. Normal `<` returned to shale6 without damage.

Untouched actual captures `slate-caves-before-01-packaged-entry.jpg` and
`slate-caves-before-02-shale-return.jpg` show a narrow, very dark glyph corridor
and return to the native shale view. This establishes the entry/return and a
specific readability gap, not the entire chunk, depth8, owner readers or new
slate sprite coverage. No scene, health, placement or visibility was modified.

## Candidate admission, owner and render contract

Use the current shallow/limestone/shale pattern, with the depth band changed
only after verified slate tests. Prepare canonical7/8 addresses once; exclude
Sinkhole mouths, RootSite, MultiCellPilot and AuthoredWilderness surface
columns. On every query, require `WorldLocationContext.For(zone)` and exact
reference equality with its current CachedZones entry, correctly sized map and
POI arrays, current null POI and no saved lair claim for the surface ID.
[WorldLocationContext:5](../Assets/Scripts/Gameplay/World/WorldLocationContext.cs:5)
is derived/nonserialized; [LairStacks.HasSavedColumn:203](../Assets/Scripts/Gameplay/World/Generation/LairStacks.cs:203)
is read-only. Do not call generating/get-or-create/ledger-adopting APIs from a
render query, infer authority from a scene name, or globally admit depth7/8.
Do not require the surface biome to be Cave: the underground pipeline is used
beneath eligible surface columns.

| Candidate owner | Actual semantics | Proposed source/reuse |
| --- | --- | --- |
| SlateFloor | Exact passable Terrain owner, comma variants,`&b`,layer0; no material/structure/thermal/service meaning | New floor0–3, deterministic cached variation; actual floor-owned LocalGround only |
| SlateWall | Exact solid Wall,`#`/`&b`/`^k`,Stone/Mineral,Thermal, structural38/hardness3/Rubble | New wall4–5; surviving damage remains the same essential body, real destruction removes it |
| [Rubble:3489](../Assets/Resources/Content/Blueprints/Objects.json:3489) | Passable Terrain,`,`/`&y`,layer0; no material/structure/thermal | Unchanged16×16 `Environment/rubble`, centered16PPU; ambient backing does not create a floor identity |
| [Stalagmite:3523](../Assets/Resources/Content/Blueprints/Objects.json:3523) | Solid PhysicalObject,`^`/`&y`,layer1; no authored material/structure/thermal | Unchanged16×16 `Environment/stalagmite`, centered16PPU |
| [StairsDown:8989](../Assets/Resources/Content/Blueprints/Objects.json:8989) | Passable Terrain,`>`/`&W`,layer0, owned StairsDown part/tag | Unchanged opening-anchors frame4 |
| [StairsUp:9032](../Assets/Resources/Content/Blueprints/Objects.json:9032) | Passable Terrain,`<`/`&W`,layer0, owned StairsUp part/tag | Unchanged opening-cellar frame0 |

Retain selected-visible-owner identity, exact current placement/one-cell
footprint, owned unique essential parts and the existing reskin/custody/status/
foreign-service refusals. Do not erase a live hazard coating or heat/charge
mark to make the material image visible. [DestructionSystem:167](../Assets/Scripts/Gameplay/World/DestructionSystem.cs:167)
removes the actual wall, creates its declared real Rubble and marks the cell
dirty; use this existing action path rather than a presentation-only wreck.

An independent ambient cell backing can reuse the selector's background Tile
and a final art-approved unmarked floor color. It must leave Owner null for
empty cells, retain actual fallback GroundKind, and set AmbientBackdrop
separately from floor-owned LocalGround. Actual nonempty TileState, competing
terrain/cultivation/source/hazard meaning, authored backgrounds and missing or
rejected actual floor art retain fallback. Hidden/custodied/foreign occupants
must not change the choice. The existing [backdrop/reader path:124](../Assets/Scripts/Presentation/Rendering/FlatPixelShaleCavesCatalog.cs:124)
and [Greybox ground hook:945](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:945)
are the bounded template; no new per-cell scene scan is needed.

Keep ground, selected foreground and exact authorized-subject reader paths
separate. A floor under an actor must not suppress that actor; a portrait must
match its actual selected visible owner. Existing paint paths submit native
LocalGround/AmbientBackdrop without per-cell light tint while retaining the
current postprocess grade ([environment:1155](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:1155),
[main paint:44](../Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixelPaint.cs:44)).
Do not claim a new lighting behavior or change that policy in this art slice.

## Finite implementation and validation gates

1. **Actual before first.** Root should obtain ordinary6→7→8 entries/returns
   from a preserved earned checkpoint, capture normal-zoom world views and real
   floor/wall/stair readers, and retain current owner/state censuses. Inspect
   wider disclosed routes and any actual room/landmark interfaces. Use short
   ordinary movement batches; [PopulationTable:1102](../Assets/Scripts/Data/Tables/PopulationTable.cs:1102)
   places both7/8 at tier3 and can roll enemies. No safety, enemy absence,
   specific placement or successful route is established by this source audit.
2. **Concept and parts.** Apply the required actual-capture→imagegen→parts→native
   pipeline in [art direction](LIVE-16X24-ART-DIRECTION.md#required-production-pipeline-and-agent-handoff).
   Reject altered geometry, visibility, HUD, resampled pixels and misleading
   raised forms. Name each part's real owner and essential state. Do not create
   disconnected concepts or new props merely to raise density.
3. **Genuine native RED before production.** Test exact7/8 positives against
   neighboring6/9 refusals; current graph/map/POI/lair/special exclusions;
   six actual factory owners and slate-specific38/3/glyph/variant contracts;
   malformed/duplicate/foreign essential parts, reskin/custody/disclosure;
   real floor versus empty/Rubble backing and visible competing meanings;
   same-layer actor/ground/reader identity; water/heat/charge/cultivation
   preservation; independent missing-resource groups/disposal; actual submitted
   full/dirty paint and structural destruction→Rubble. Pair every refusal with
   a valid native precondition. Reuse finite existing renderer fixtures.
4. **Minimal integration after RED.** One companion and bounded Greybox hooks
   following the current band adapters; lazy cached Sprite/Tile views, no
   creation, resource loads, parsing or Render event dispatch in the hot query.
   Optional borrowed textures fail independently; disposing owned views never
   destroys source textures. No generation, damage, loot, provenance, health,
   global admission or existing artwork changes. Broader helper extraction is
   outside this slice unless a concrete defect requires it.
5. **Actual compare and revise.** Inspect wall/floor contrast under the real
   grade, Rubble/ownerless continuity, stairs, actors, hazards, memory boundaries
   and repetition. Exercise actual wall collision/damage/destruction, Rubble
   traversal/readers,6↔7↔8 travel and fresh-process/isolated Continue as available.
   Record real damage, interruption and rejected inputs honestly. Profile the
   actual route with native allocation controls where supported; retain tool,
   idle and combat overhead rather than claiming FPS or Deck results.
6. **Review and release.** Independently review source, runtime, native and
   actual evidence, update living records in the same source commit, push and
   verify main, then fresh packages and bounded Mac restart/Linux archive
   verification as applicable. Linux execution and physical Deck acceptance
   require their own evidence. Keep previous builds, sources and normal saves.

## Scope pruning, review and continuation

- **Included candidate:** two slate material owners plus four immutable shared
  travel/terrain owners, and a separate truthful ambient backing.
- **Excluded:** quartzite9–10, obsidian11+, special/lair/authored column coverage,
  generic/Vine walls, storage, mineral nodes, hazard owners, actors, room props,
  new topology, decorative generation and nuanced animation.
- **Independent remaining queue:** the [shared-owner audit](Verification/Live16x24/post-limestone-shared-owner-audit.md)
  identifies finite cross-region storage and actor/hazard gaps. Surface biome,
  settlement, interior and special-area coverage remain priorities too; serial
  cave bands must not become a substitute for those gaps.
- **Polish remains separate:** every biome/area needs later serious normal-zoom
  composition/fullness, routes, landmarks/interiors/services, silhouette,
  palette, state, travel and visibility review. Coherent quiet spaces and
  readable density are judged from whole gameplay views, not art counts. No
  existing55 inventory or five Stump area polish state is advanced here.
- **Self-review:** no known source blocker to this bounded proposal; actual
  contrast, repeated patterns, danger, room layout and area fullness are still
  unobserved. The exact old palette and wall38/3 corrections prevent copying
  shale assumptions. Native RED, performance and gameplay results remain absent.
- **Implementation log/files changed:** this source-only preflight creates only
  `Docs/GRAPHICS-SLATE-CAVES.md`. No art, runtime, tests, shared ledger, Unity or
  Git action is performed by its author. Next action is root's actual baseline.
