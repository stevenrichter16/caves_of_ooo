# Environmental polish: start with the Sodden fen floor

**Status,2026-10-28: first scoped Sodden material pass bounded Editor-verified; nextStump.** The user's direct override prioritizes environmental tiles, walls, floors, ground and ground cover **across all biomes/areas**. Stop extending cultivation/lifecycle work; preserve the tested crop integration with bounded dry-appearance acceptance. Root owns its closing records and Unity/Git. The Sodden pass now has original native tiles, guarded integration and native/actual evidence; see [bounded record](GRAPHICS-SODDEN-ENVIRONMENT.md). All full-area acceptance remains open.

## Choice grounded in actual scenes

Reviewed PNGs are under `Docs/Verification/Live16x24/`:

| Actual whole-view evidence | Visible weakness and real source |
| --- | --- |
| `sodden-frogs-after-01-open-mire.png`, `sodden-ordinary-after-04-open-mire.png`, current `portable-crops-after-01-dry-world.png` | Nearly uniform sprig repetition on firm ground; large flat olive pools with weak shoreline/material distinction. Visible in Dark and Dawn. Exact Grass/MirePool/Reeds: `FlatPixelSoddenTravelCatalog` and `SoddenCompositionPlan`. |
| `stump-canopy-after-07-summit.png` | Nearly uniform pink floor dominates sparse objects; muted raised patches need clearer blocking/material cues. Exact TepuiStone and Stump families: `FlatPixelStumpMaterialsCatalog`. |
| `grovelands-actors-after-01-grove.png` | Woodland center reads as settlement lawn despite its enclosing vegetation. Exact Grass borrows Sill frames20–23 in `FlatPixelGrovelandsMaterialsCatalog`. |
| `shared-village-roles-after-01-world.png` | Disclosed ground repeats the sprinkle motif; masonry/approaches need stronger composition. Dark glyph regions are memory, not demonstrated missing native ground. `FlatPixelSillEnvelopeCatalog`. |

**Sodden firm-ground/mire-edge pass is the first bounded result**, then Stump stone/blockers and Grovelands forest floor. OpenMire has a conspicuous whole-screen defect and existing real material boundaries. This is a higher-impact next action than another isolated object, without a blind navigation search.

## One concrete next pass

Normal Continue from `Fixtures/sodden-frogs-after157`: OpenMire18.8.0, turn2350, HP38/40 per its `Quick.json`. Capture current position/state before comparing; do not continue the injured cultivation branch. Keep actual pools, routes, reeds, actors and collision unchanged initially.

Give actual Sodden **Grass** a distinct low fen-floor material: connected irregular patches of short matted sedge and occasional flattened straw, quiet open centers, no repeated sprig in every cell. Use a separate editable native16×24 resource, leaving the approved shared Sill atlas untouched. Vary broad patches and individual cells deterministically; keep detail subordinate to actors, loot and hazards. Do not imply new harvestable plants, obstacles, mud mechanics or safe roads.

Clarify actual **MirePool** surfaces with a readable dark wet body/olive film and restrained irregular edges inside their real cells. Preserve filled/depleted states: depleted mire remains wet film and can retain/renew its independent source; zero volume is not safe dry ground. Use the existing separate shoreline Reeds, without arbitrary props or hidden hazardous co-occupancy.

Verified source: `SoddenCompositionPlan` builds the OpenMire basin mask, separate dry-side reeds and Grass substrate. `FlatPixelSoddenTravelCatalog.TryResolveGround` currently borrows Sill frames20–23; `GroundVariantAt` gives variant0 twelve of sixteen outcomes. Preserve owner/state/graph/visibility and coating-priority guards, cached tiles and batching; no per-frame noise, new GameObjects or generation changes are required for this first pass.

Next visual action: actual screenshot → imagegen concept → editable native parts → in-game comparison. Brief: “Preserve exact camera, flat16×24 grid, HUD, owners, routes and pool geometry. Reimagine only disclosed fen floor and real mire: irregular low matted-sedge patches, quiet space, readable wet-film edges, reduced repetition. No invented objects, paths, lighting system, elevation, crop work or safe bridges.” Reject generated placement changes and blur.

## Acceptance and wider continuation

**Coverage:** exact materials/states, depleted/foreign/hidden owners, unknown neighbors, coatings, fog/memory, missing art, dirty repaint and save/return remain truthful. Write failing tests before runtime changes. Preserve shared atlas bytes. Native passes establish integration, not polish.

**Polish:** compare matched actual whole views, walk firm ground and an existing mire edge, inspect/revisit, then check a second established formation such as ReedMaze or PeatCuts. Require visibly reduced repetition, clearer firm/wet boundaries and coherent regional identity without clutter or obscured entities. Record remaining weakness; no full-biome, FPS or Deck claim.

`ArtSource/Live16x24/world-sprite-coverage.json` (`world_polish_phase`, per-biome `polish_review`) and `Docs/GRAPHICS-WORLD-SPRITE-COVERAGE.md` retain all55+5scopes open. Their older coverage-first ordering is superseded, not completed. Continue environment-first through Stump, Grovelands, settlements/interiors, Beating/Overwrit/Spread, underground and special-site walls/floors, each with an actual baseline and material-source check. Preserve accepted actors/items and animations; unknown areas remain not_audited. These bounded viewport comparisons do not certify any full80×25area.

## Next biome already scoped

[Stump floor and raised-stone plan](GRAPHICS-STUMP-ENVIRONMENT.md) compares actual SummitScrub views and identifies the flat pink floor/subtle dome blocker hierarchy. Healthy earned68 and closer Summit74 are recorded. Reuse the existing71-frame editable atlas; begin with current actual baseline and imagegen for the named floor/dome revision. Preserve passable DescentLedge and all non-target frames. Follow with Grovelands forest floor, then the remaining settlement/interior/underground/special-site material ledger; do not return to crop-state or inventory packets.
