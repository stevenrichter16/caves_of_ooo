# Light, shadow and shading: small Sill pilot

2026-10-28 — design only; no shadow implementation or visual acceptance. Start after the current Sill river/memory continuity pass is verified. The user's direction is to begin small, inspect actual play and iterate. Preserve the approved flat16x24 presentation, quieter floors, straight layered walls and existing gameplay. This roadmap does not expand the current implementation scope.

## Source audit and reuse limits

| Existing source | Consequence for the pilot |
| --- | --- |
| `AnimatedEntityRenderer.cs:80,365–394` loads `Sprites/Actors/actor_shadow` and creates a pooled shadow SpriteRenderer below the body (sorting5 versus6). | Reuse that actor view/lifetime if its native projection passes inspection. Do not add a separate actor discovery scan or object per frame. The legacy color has alpha.55 and localY.10; neither is an approved native setting. |
| `actor_shadow.png` is16x8 with a10x4 opaque mask; its meta is Point/16PPU with a10x4 sprite slice. | Source bytes can stay unchanged. Verify the actual loaded Sprite, pivot, bounds and shader in Unity before accepting reuse; file metadata alone does not establish correct native placement. |
| `AnimatedEntityRenderer.cs:557,593` and `AnimatedEntityRenderer.FlatPixel.cs:265` explicitly disable flat shadows. | A shadow does not currently exist in native FlatPixel play. All three lifecycle paths must obey one new bounded admission rule; changing one enable flag is insufficient. |
| `AnimatedEntityRenderer.FlatPixel.cs:ObserveFlatCell/ValidFlatOwner/ValidFlatView` validates disclosed current actor views; `CellAnchor` and `PixelSnap` use world-tilemap projection. | Extend accepted view presentation only. Do not invent motion views for static villagers, glyph residents or props. Existing player/dog/marlback coverage does not establish shadows for all Sill keepers. |
| `LightMap.cs:61–95,117–160,173–207,261` combines zone ambient/tint and local/equipped sources with line-of-sight falloff. | There is existing ambient/source lighting, but no demonstrated sun-direction field or solar cast-shadow simulation. Native actor bodies use an unlit material/white tint; general lighting integration needs a later audit. |
| `EnvironmentSpriteRenderer.cs:654–662,1167–1172` paints real reserve-bed twine in `FlatGroundDecorationTilemap`; `FlatPixelGreyboxCatalog.cs:559–588` calls `ObservePaintedReserveBed`. | Do not repurpose GroundDecoration as a generic shadow channel or trigger reserve-bed observations for shade. Reuse the actor's own shadow renderer for phase1. |

## Phase1 — one contact-shadow pilot

Begin with the already accepted native **player view in the current Sill graph**, observed around the court. Other current native actor views may follow only after this first comparison; no new actor artwork or motion admission. Draw a small dark neutral contact mask on disclosed ordinary dry grass/stone floor, below the actor body. Start around12–20% effective opacity and a pixel-snapped1–2native-pixel offset; keep the whole mask inside the relevant cell footprint. This is a grounding cue, not collision, elevation, a targeting footprint or a time-dependent sun shadow.

Use the existing borrowed resource and pooled renderer only if its loaded dimensions, pivot, material and16x24 projection satisfy those bounds. Preserve the PNG/meta and legacy settings. Missing/invalid resource or failed admission leaves the previous presentation intact. Use one bounded presentation option/guard to disable the pilot and restore old behavior; reset native shadow settings on pool reuse/mode exit so they cannot alter legacy actors.

Require the same current, selected, fully disclosed living owner as the native body. No shadow in unexplored/remembered cells, for hidden/replaced/removed/carried owners, readers/UI, water/hazard substrates or dying views in this first trial. During interpolation, retain it only when its entire rendered footprint lies within disclosed allowed ground; otherwise hide it until the position is valid. Do not read hidden neighbors to decide where to draw shade. No new colliders, TileState, source events, geometry, terrain ownership or save data.

## Phase2 — bounded wall/prop shade, only after phase1 review

Choose one actual court wall or counter family and one short consistent offset. Respect existing baked highlights/shading; avoid doubling dark edges, raised floor illusions or a grid of identical blotches. Shade must stay subordinate to wall silhouettes, doors, paths, wet surfaces, hazards and actors. Define a separate rendering contract before implementation; the reserve-twine channel is not available by assumption. Disclosed current owners and disclosed receiver pixels alone may contribute. A fixed daytime direction is an explicit artistic convention, not a claim of solar simulation. Do not expand across biomes until the single family reads correctly in normal play.

## Phase3 — appropriate lighting, later and evidence-led

Audit how native art should respond to existing ambient, lamps/torches and interiors without flattening authored palettes or obscuring status flashes. Begin with one source or one interior/exterior transition. Directional day phases, multiple cast lights, long shadows, normal maps and a broad shader/lighting rewrite remain uncommitted options. Preserve fog and gameplay light/occlusion authority; decorative shade must not manufacture a light source or visibility rule.

## Verification, iteration and credit bounds

- Before production, write focused native RED for actual shadow submission, one accepted view, invalid-resource fallback, projection/footprint, ordering, and visible-to-hidden/memory/UI/mode/pool cleanup. Extend relevant `FlatPixelActorMotionTests` movement, quiet invalidation, death and pause controls; keep `ConnectedReserveBedArtTests` and existing source-light controls unchanged. Pin unchanged simulation, body/frame identity and no extra Render events. Counter-check an otherwise identical ineligible owner/cell.
- Capture matched actual normal-zoom court views at the same zoom/time/position, plus ordinary movement, doorway/wall approach, fog edge, reader/inventory/pause, river refusal, travel/return and fresh Continue. Record natural state differences; do not claim exact pixels when actors or time differ. Compare actor grounding, floor marks, silhouette, hazard contrast and black/halo artifacts. Reject or reduce the effect if it makes bodies look elevated or muddies the scene.
- Profile a bounded actual walk and idle/UI segment with existing diagnostics: renderer time/maxima, batches and allocations, with window/wrap/tool-time limitations. Reuse pooled views/materials and current invalidation; no per-frame allocations, full-zone shadow scans or resource reloads. No speedup, package or physical Deck claim without corresponding evidence.
- Conserve the user's remaining credits: one small approved-art reuse, focused meaningful tests, one matched comparison, then a specific correction if needed. Reuse valid evidence and batch related controls. No imagegen run, new asset pipeline, generic shadow framework or exhaustive matrix for this pilot. Keep later phases planned until phase1 has an observed reason to expand.

Current status: source audit and roadmap only. Root owns the roadmap links and hourly automation prompt; future reports should distinguish design, implementation, native verification and actual visual acceptance.
