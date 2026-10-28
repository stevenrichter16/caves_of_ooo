# Light, shadow and shading: small Sill pilot

2026-10-28 — phase 1 now has bounded native and actual Editor verification; source publication pending. Phase 2/3 remain design only. The Sill river/memory continuity pass is published. The user's direction is to begin small, inspect actual play and iterate. Preserve the approved flat16x24 presentation, quieter floors, straight layered walls and existing gameplay. The first implementation is limited to the current Sill player.

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

Current status: phase 1 is implemented and observed as recorded below. Root owns the roadmap links and hourly automation prompt; future reports must distinguish this bounded pilot from wall/prop shading and wider lighting, which remain planned.

## Pilot baseline / source correction — 2026-10-28 19:39 UTC

Actual isolated Continue185 at Sill39,11, HP40 captured in `Verification/Live16x24/light-shadow-before185.png/json`. Live resource inspection confirms rect(3,1,10,4), pivot(5,2),16PPU, Point, borrowed actor_shadow. Active native Player shadow is disabled; pooled legacy settings are alpha.55, local(0,.10), lit material. Phase1 will reuse this exact mask at black alpha.18 and local(1/16,2/16), unlit below body. Material admission will reuse the existing disclosed Sill Grass/StoneFloor policy; water, state layers and competing ground refuse. Actual bounds across departing/arriving cells must remain disclosed and dry. Root owns tests/Play; baseline session finished normally.

Native RED (19:46:15–31 UTC):79 cases,60 passed,18 new contact-shadow failures,1 existing allocation-counter test skipped because its counter cannot observe a known allocation. Every new failure is missing shadow submission/option. MCP reported initialization timeout while the native runner was active; retained actual XML is authoritative. Source edits wait until runner cleanup ends.

Import correction: the initial scripts-only refresh compiled callers before discovering the new partial source. Requested a full asset refresh to import it; no gameplay test result claimed from that compile attempt.

## Phase 1 result — 2026-10-28 19:58 UTC

The existing player renderer now adds a small black 18% contact mask in the current Sill graph, on disclosed ordinary dry Grass/StoneFloor only. It reuses the unchanged `actor_shadow` PNG/import and existing pooled renderer, with unlit material, sorting 5 below body 6, and local offset (1/16,2/16). `FlatContactShadowEnabled` is a reversible component/property option, not a new saved player preference. No new pixels, lights, colliders, source events, inventory portraits or world/save schema were introduced.

### Source corrections and verification

- Extracted the existing Sill dry-floor admission into an internal bounded local helper shared by material selection and the pilot. Its material result and refusal policy remain the same. A second bounded receiver check refuses disclosed props/items/walls except the player and ordinary floors; hidden owners do not disclose themselves through shade.
- Every actual mask-overlapped cell is checked, including the departing cell during interpolation. Quiet reconciliation, motion/state transitions, missing resource, death, pause and mode/pool reuse have explicit paths. Pool acquisition restores legacy color, position, material and scale.
- Final targeted native run: **281 passed, 0 failed, 1 existing allocation-counter skip** (282 cases, +18 new). All 18 new contact-shadow cases passed after the retained RED. The skip cannot establish zero allocations; its counter failed to observe a known allocation. XML and five tested source hashes are retained. MCP reported initialization timeouts while native tests were active; native XML and cleanup state were checked before further work.
- Independent source review found no actionable P1/P2. Direct local occupant enumeration uses struct enumerators, existing part lists/dictionary lookups and cached footprints; at most four receiver cells are checked. This is source analysis, not a measured zero-allocation claim.

### Actual comparison and route

`light-shadow-before185.png` and `light-shadow-after185.png` use the same earned checkpoint, camera and player39,11. Exactly **16 pixels** differ, confined to (468,324)-(476,328) beneath the boots; all other pixels in this pair are identical. Detail PNGs are explicitly nearest-neighbor enlarged actual crops, not new artwork. The shadow is correctly grounded and intentionally faint at normal zoom. Independent visual review found no halo or spill; it did **not** establish a noticeable scene-wide lighting improvement. Keep this as the first small pilot. A stronger-opacity comparison remains optional later, after an actual need is identified; no opacity iteration or broad acceptance is claimed here.

Ordinary input checked court walking (including a normal occupied path/detour), Grass and actual StoneFloor at40,7, inventory, WellGroundMarker40,11 refusal with the solid well still blocking40,12, river39,20 refusal, the real water reader, dry39,19 recovery, normal worldmap glade11.10 control (no shade outside Sill), Sill return and F5. Fresh isolated Continue186 restores turn1130, HP40, player39,11 and the enabled contact shadow. No teleport, reveal, gameplay hook injection or world-state staging was used. Full restart world image differs482/564000 pixels in a left-side region (x11–266), away from the player; the player crop is exact. Cause of those other local repaint differences is unproven; no whole-scene pixel-exact restart claim.

The actual Editor profile includes181.861s /35971frames, with32768 retained wrapped samples: renderer mean.150ms, p99.312ms, maximum184.731ms; Main Thread maximum5670.977ms;87 maximum batches. UI/tool/idle and full repaint work are included. This is not a controlled performance comparison, a speedup result or a physical Deck result; transient peak attribution remains unproven.

### Preservation, remaining work and reproducibility

Normal saves (1000 Editor entries),49 protected foreign paths, game preferences and current/prior builds were preserved. Ten whitespace-only Unity metadata rewrites were backed up and restored; no import setting or source-art change. Storage inspection only, zero bytes deleted. Unity is stopped, save override cleared, scene clean. Existing packages still use43a151ddb and do not include this pilot.

Reproduce with the established isolated native launcher, copying immutable `Fixtures/sill-continuity-return185` for the matched pair or `Fixtures/light-shadow-return186` for the final Continue; press ordinary C. Never use normal save roots. Native fixture `FlatPixelActorMotionTests` plus Sill envelope/memory/river, fallback and opening-ground controls supply the retained282-case result. The footprint test explicitly hides/wets the departing cell while the destination remains visible.

Next: choose **one Sill masonry wall or counter** for a short coherent directional shade pilot, with an explicit disclosed receiver/rendering-channel contract and matched normal-zoom before/after. Do not reuse the reserve-bed twine channel by assumption. Then compare the result with another biome before expanding. Other NPCs, props, walls, ambient lighting response and all other biomes remain unimplemented for this shadow layer. Environmental materials/population work stays active; no full-area polish or new population acceptance. Crops and optional animation stay deferred. Last actual hourly report19:44UTC; next20:44UTC.

Self-review: native/actual/scope/legacy symmetry checked. No outstanding actionable source-review P1/P2; visual subtlety, restart-local differences, profile peak attribution and broader lighting/area acceptance remain explicit limitations.
