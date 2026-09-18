# Morrowfast: south-of-Felling Unity handoff

Status: architecture verified against the current repository on 2026-09-06. This is a concrete implementation contract and test plan; no Unity runtime implementation is claimed by this document. The live Felling preview was left untouched during this review.

Morrowfast is the proposed authored chunk at **Overworld.3.6.0**, directly south of the existing Felling-Site at **Overworld.3.5.0**. Its Stillcord watch fellowship is a new local institution: two people at the north entrance, a relief keeper, homes and useful work. It has no new divine inheritance, no claim to resolve Naro's ambiguity, and no authority over the Six. The northern path remains a practical entrance to the Felling rather than becoming a military garrison.

## Verified constraints and corrections

| Assumption checked | Actual current code | Consequence |
| --- | --- | --- |
| The southern cell is available | `WorldMapAuthoring.BiomeRows[6][3]` is Grovelands; `TierRows[6][3]` is 3. No `Places` entry occupies (3,6). Olderdeep is at (4,6). | Keep the Grovelands ecology and tier; add a new named place without moving existing lore locations. |
| A Village profile can produce this image | Ordinary `CreateVillagePipeline` first builds a random village, then runs connectivity, a wide east-west river, populations, containers and optional house drama. | Short-circuit the Morrowfast profile into one dedicated builder before adding any ordinary village builders. Otherwise later builders destroy authored geometry. |
| Felling's renderer can consume arbitrary scene data | `FellingSceneDefinition.Validate` requires exactly 55 layers, 39 mutable layers, seven landmarks and its fixed transform. The presenter also has hard-coded cliff projection and bounds. | Do not feed this scene through the Felling definition or relax its existing invariants. Introduce a separate small Morrowfast definition/runtime/presenter with narrow shared rendering hooks. |
| Felling's 1536×1024 transform fits a new top-down chunk | Felling has 48×32 artwork cells at 32 PPU, but the top seven rows (224 pixels) are cliff overhang; only its bottom 25 rows are playable. | A true top-down master cannot lose its top seven rows. Use the uniform transform below, or generate a different aspect ratio deliberately. |
| Existing doors have a full open/close state machine | `LockPart` implements reusable-key or keyless unlock, then makes Physics non-solid. There is no matching general roof or close-door system. | Roof cutaway and repeatable open/close need a small new native part/presenter contract. Do not advertise `LockPart` alone as open/close support. |
| Beds already provide player sleep | `BedPart` offers an idle reservation to NPC AI. Player rest is `RestSystem.TryRest`, used by the `RestAtInn` conversation action. | Guesthouse keeper offers actual native paid rest. Beds can be selected/examined and used by NPCs; player sleep must explicitly route to the existing rest service. |
| Merchants need a new trade implementation | `ConversationPart` starts native dialogue; `TraderPart` sets stock and purse; `ConversationManager` offers trade; `TradeSystem` and restock already operate on real inventory. | Reuse the existing trade UI and stock/purse behavior, with scene-specific NPC identity and dialogue. |
| Public part fields persist | `SaveSystem` serializes public fields and fully qualified Part type names; `[NonSerialized]` fields are skipped. | Persist semantic object state and stable IDs, never SpriteRenderer/Texture references or cached owner dictionaries. |

## Source and coordinate contract

Authoritative art package proposed at `ArtSource/Morrowfast/`, exported Unity data at `Assets/Resources/SceneArt/Morrowfast/`. Art identities stay independent of blueprint names. An alpha layer is visual evidence, not collision geometry.

For a **1536×1024 true top-down** master, use a uniform `pixelsPerCell = 1024 / 25 = 40.96`:

- Image top-left maps to world `(21.25,25)`; image bottom-right maps to `(58.75,0)`.
- `worldX = 21.25 + pixelX / 40.96`, `worldY = 25 - pixelY / 40.96`.
- Ground cell `(x,y) = (floor(worldX), floor(pixelY/40.96))`, rejecting y outside 0..24 and x outside 0..79.
- Native cell `(x,y)` occupies world rectangle `[x,x+1] × [24-y,25-y]` and has center `(x+.5,24.5-y)`.
- The art is 37.5 cells wide and 25 cells tall. Do not stretch it to 80×25, change global Zone dimensions, or use Felling's 224-pixel ground offset.
- Place the north/south lane around **native x=40**. A native center at x=40.5 corresponds to image x=788.48. Reserve at least x=39..41 at both y=0 and y=24.
- Preserve an authored terrain apron outside x≈21..59 and connect it to ordinary east/west exits. This lets the player approach neighbors without walking into unrendered void. The camera follows outside the fitted art, as the Felling camera does.
- Individual PNGs retain their original pixel dimensions. Import PPU 40.96, point filtering, no compression/mips, full rect, explicit pivot, no RGB changes. A noninteger PPU does not require resampling the source PNG.
- Camera framing is a 3:2 fit inside the existing HUD-excluded viewport. A true top-down scene needs no cliff fog overhang projection: use per-ground-cell FOV and roof footprint visibility.

If the final generated master uses another resolution, derive one uniform PPU from `height/25` and freeze its transform in the manifest. Do not silently scale width and height independently. Every component's interaction anchor and footprint are authored against this transform after looking at the final image.

## Manifest consumed by Unity

Proposed `definition.json` schemaVersion 1, id `morrowfast`, revision 1:

- `canvasWidth`, `canvasHeight`, `pixelsPerCell` (float), `originX` (float), `groundOffset:0`, `baseResource`.
- `layers[]`: unique `id`, `name`, `kind`, `resource`, optional `contactResource`, integer source `bounds:[x,y,w,h]`, `footPixelY`, stable compositing `depth`, `ownerId`, `buildingId`, `visibilityRole` (`ground`, `object`, `roof`, `interior`, `door-open`, `door-closed`).
- `owners[]`: unique `id`, native `blueprint`, `anchorX/Y`, optional actual `footprint[]`, `examine`, approved `interactionKind`, optional `buildingId` and `stock/conversation` metadata. Roof and open/closed visual states can reference one semantic owner; the PNGs remain separately exported and selectable.
- `cells[]`: complete 80×25 independent `solid/opaque/water/groundKind` specification, including the apron. Permanent structural collision is ground geometry; movable props and doors own their dynamic collision.
- `buildings[]`: unique id, interior cell set, wall cells, roof layer IDs, door owner IDs. Roof hiding is a visual consequence of player presence or explicit inspect mode, not deletion of a building.
- `arrivals[]`: named north/south/east/west anchors and source `Overworld` neighbors; no custom teleport is needed for ordinary cardinal transitions.
- `provenance.json`: source hash, each PNG hash, exact source/inferred classification, generator/edit prompts, extraction mask, underlay source, compositing order and QA results.

Validate duplicate IDs, path traversal, canvas bounds, nonfinite transforms, bad anchors, incomplete geometry, unsupported interaction kinds, unknown blueprint IDs, missing roof references and absent contact/underlay files before mutating a Zone. The exporter must fail on uncovered source pixels or a component without either a semantic owner or an explicitly documented permanent surface owner.

## Scene inventory and actual interactions

| Location | Semantic objects | Native behavior and polish |
| --- | --- | --- |
| North gatehouse | Two gate watch NPCs, relief keeper, hinged barrier, warning board, pegs, rope coils, bench, small lamp | Guards use native Brain/AIGuard posts and actual combat. Unique dialogue explains local practice, gives a free peaceful entry route and responds to return visits. Barrier uses real blocking/open state. Lamp can use native light/fuel. Board is individually readable/examinable. |
| West guesthouse | Keeper, roof, entrance, mats/beds, chest, table, stools, kettle, rain jars | Enter through a real door; roof cuts away to an inferred furnished interior. Keeper offers the existing `RestAtInn` action with an explicit local cost and site name. Chest opens real inventory; free items can be taken. No invented promise that BedPart itself grants player rest. |
| Southwest rope/tool shop | Rope seller, counter, crate, tool rack, repair stock, scale, sign, removable loose goods | Conversation/Trader/native inventory transaction. Visible display props have their own examine/owner state; purchasable stock belongs to merchant inventory so a display cannot create a second copy. Loose floor stock uses ordinary Take and associated ownership policy if one is authored. |
| East mushroom cookshop | Cook, service counter, mushroom basket/growth, hearth, jars, stools, shed door | Native trader food stock, edible Mushroom items, HarvestablePart growths that disappear once harvested, CampfirePart hearth/rest/cooking where supported. Scenery behind harvested baskets/growth is reconstructed ground/interior, never transparent void. |
| Northeast witness cottage | Witness, roof/door, desk, bookshelves/chest, pinned bark slips, two mismatched chairs | Unique dialogue preserves disputed testimony. Paper/sign objects have specific text; container holds real objects. Nothing grants a canonical answer about the seventh. |
| Central rain cistern | Stone rim, water owner, bucket, drain, marker stones | Native `Well`/WellPart offers **draw water** (`DrawWaterAtWell`) and clears Parched. This is a drink action, not an inventory-liquid fill/depletion mechanic. Split rim and water so later water-state art never removes the masonry. |
| West creek and shared yards | Creek cells, bridge boards, stones, fungi, shrubs, washline, lantern, log pile, small fauna | Deep creek remains solid with bridges explicitly passable; native water behavior where appropriate. Each substantial loose prop and growth has an owner. Bridge structure is fixed/examinable unless a destruction state and reconstructed water underlay are actually shipped. Native fauna have independent bodies and cannot be baked into ground. |

The local quirks should appear in both art and truthful actions: a rain gauge marked by knots; a clerk who writes only departures; a cook who swaps two bowls before serving; a relief watch who repairs travelers' cords. They add local character without introducing unimplemented schedules or claiming invented supernatural mechanics.

Known existing blueprint building blocks include `Villager`, `Innkeeper`, `Tinker`, `Merchant`, `Provisioner`, `Warden`, `Chair`, `Bed`, `Chest`, `Crate`, `Bookshelf`, `LockedDoor`, `Well`, `Campfire`, `WaterPuddle`, `MushroomRing`, `Mushroom`, `Torch`, `Tepuibone`. `Warden` already includes Trader and a loadout; do not blindly inherit its whole content into an ordinary Stillcord watch keeper. Prefer narrow new NPC blueprints with verified neutral faction, conversational identity, own simple loadout and AIGuard where intended. The shipped set does not currently establish a `Rope` blueprint: author one honestly if it is supposed to be a real trade item.

## Minimal implementation slices and files

1. **Data and simulation before presentation.** New `Gameplay/World/MorrowfastSceneDefinition.cs`, `MorrowfastSceneStatePart.cs`, `MorrowfastSceneRuntime.cs`; new `Generation/Builders/MorrowfastBuilder.cs`. Definition validates complete content. Runtime stages all terrain/owners before any mutation; stable `morrowfast-owner:{id}` identities support lookup after save/load. State lives on ordinary ground (as Felling's does), so no invisible state entity leaks into the object picker.
2. **World route.** Add `Place(3,6,"Morrowfast","Stillcord",profile:"Morrowfast")` and a dedicated profile branch returning only its authored pipeline before normal `VillageBuilder`. Add local neutral Stillcord faction content. Normal world generation reserves all Places before opportunistic POIs. Verify old saved-map rehydration separately: current `RehydrateAuthoredProfiles` only updates profiles on existing Village POIs; a newly added Place needs deliberate idempotent insertion on old maps. Cache migration must preserve unknown placed items, NPC deaths, doors, inventories and removed props.
3. **Native object behaviors.** Reuse ordinary Chat, Trade, Harvest, Take/Drop, OpenContainer, Examinable, lights and rest. Add only `MorrowfastDoorPart`/gate policy for repeatable open/close. A successful action changes authoritative owner collision/opacity and saves state; close refuses an occupied door footprint. Never remove collision merely because a roof is hidden. Actions validate actual live owner, actual zone, reach and actor health; success alone consumes the normal action energy.
4. **Faction and dialogue.** Add scene NPC blueprints and a separate `Resources/Content/Conversations/Morrowfast.json` (the loader already uses LoadAll). Build the fellowship as a small named neutral faction, with guard dialogue providing a free peaceful entrance. Use existing conversation predicates and listener properties for a repeat-visit acknowledgment. If access needs a persistent credential, name and test it explicitly. Do not charge a mandatory fee or make the only exit depend on a living guard; release from the Felling side remains possible.
5. **Source-aligned presentation.** New `Presentation/Rendering/MorrowfastScenePresenter.cs` owns base, component/contact sprites and roof/interior states. It reads owners/cell fog and never changes simulation. A small Morrowfast shader is needed: the existing Felling shader hard-codes fog UV as `(world.x-16)/48, world.y/32`, with no transform uniforms. Use the same unlit alpha/fog approach with explicit scene origin/size uniforms; do not edit Felling shader coordinates globally. New importer filters only `SceneArt/Morrowfast/Art`. It uses the uniform transform above.
6. **Narrow rendering integration.** `ZoneRenderer` currently knows one Felling presenter and exposes `FellingPresenter`. Add a Morrowfast sibling and aggregate `ClaimsCell`, authored-entity suppression and alpha-picking at existing seams. Preserve ordinary native actor/item priority and return the clicked semantic owner even for overlapping roofs/props. `CameraFollow` gets the same active-scene framing choice; departure, sprite toggle, Look targeting and outside-apron travel restore ordinary behavior. Avoid a broad refactor of all rendering during this scene addition.
7. **Actual play audit and intact preview.** Add `Scenarios/Custom/MorrowfastScenePlayAudit.cs` plus its editor menu. Reuse existing NativeSaveIsolation and native Input System pulse timing. Travel normally from the Felling south edge and back; do not teleport, reveal fog or call gameplay actions via read-only MCP execute_code. Keep an intact preview distinct from the destructive interaction audit.

Core proposed public API: `MorrowfastSceneDefinition.Load()/Parse()/Validate()`, `MorrowfastSceneRuntime.IsActive(zone)/Install(zone,factory,preserveExisting)/FindOwner(zone,id)/GetState(zone)`, and presenter `Bind/Unbind/Refresh/ClaimsCell/IsAuthoredEntity/TryPickImage`. Do not generalize the Felling public definition to accept a second scene ID; that would erase valuable existing source-specific test guarantees.

## RED → GREEN and native verification contract

Before production behavior, execute failing tests against these invariants. Pair each successful path with a matching refusal/other-zone path; record raw output, not only counts.

- World (3,6) is a named Grovelands tier-3 settlement for multiple seeds; no neighboring authored site changes; ordinary Felling south-edge crossing reaches its north arrival at the same x and returns correctly.
- All 2000 cells are declared exactly once. Open gates permit north/south travel. Every building entrance, every native interaction anchor and every intended edge has a real cardinal approach. Use `Cell.BlocksMovement(actor)`, not only `IsPassable`, and include initial NPC bodies. Guards cannot occupy the only lane or trap the player by idling.
- Every exported component hash/bounds/alpha is valid. Compositing all original source layers returns the master exactly. Inferred roofs/interiors/backing are evaluated separately and never described as known source pixels.
- Each building exterior, open doorway, roof cutaway, interior furniture, closed door and empty room has a matching visual/collision state. Try closing on the player, NPC, item, dead/removed owner, wrong zone and out of reach. Failed actions consume no energy and change no state.
- Harvest and pickup remove the world owner and its sprite exactly once; backing is opaque and plausible. Drop at another cell moves the item view and keeps the same identity. Merchant inventory is not duplicated by display props.
- NPC dialogue opens, actual trade transfers item/currency both ways, hostile refusal uses existing rules, resting charges only on success, and guards still use normal AI/combat. Repeated dialogue cannot farm a credential or stock.
- Save/reload with an open door, hidden roof, harvested mushrooms, moved item, depleted shop, dead guard and a player inside a house preserves semantic state and permits escape. Roof visibility recomputes from loaded location; no cache object serializes. Reentry never respawns consumed stock/owners or dead residents.
- Roof alpha picking reaches roof/building outside and interior furniture inside; click-ground beside an object does not select the neighboring image rectangle. Native actor ownership wins at occupied cells. True top-down FOV never uses the Felling's cliff-overhang reveal and never reveals an unvisited room through its roof.
- Camera retains artwork aspect with wide/narrow screens and existing HUD; no pale gutter seams, water duplicate overlay, dark multiplication on actors or lost navigation outside the art footprint. Sprite-off and non-Morrowfast zones stay unchanged.
- Native isolated Play run: Felling → south edge → gate conversation/open → three shops/guest service → enter all five buildings → use cistern → harvest/take/drop/container/trade → save/load → return north → reenter and verify no reset. Capture intact, roof-cutaway, open gate, harvested backing and post-load screenshots. Report profiler context and failures candidly.

Verification honesty: source reconstruction, native ownership/collision/state, route connectivity and command results are script-observable. Natural placement, stylistic consistency, intelligible roofs and convincing inferred surfaces require reviewing actual rendered screenshots at gameplay zoom. A passing raster test alone is not proof of visual polish or playability.

## Readiness and review

- Ready: existing normal zone transitions, native dialogue/trade/harvest/take/container/rest/guard behavior, Felling's proven source-owner pattern and native audit isolation.
- Awaiting final art: exact masks, component count, door/room polygons, ground reconstruction, sprite feet, creek/bridge boundaries and source camera composition.
- New behavior required: repeatable doors, roof cutaway and scene-specific gate policy; each needs tests before production.
- Explicit limit: this proposal does not add a new biome, global military faction, divine cosmology, real-time daily schedules or procedural house interior generator. It implements authored local interactions that the player can actually exercise.

Self-review: the initial assumption that 32 PPU and Felling's transformer could be reused was rejected because seven playable image rows would be lost. The assumption that LockPart supplies closeable doors and BedPart supplies player rest was also rejected by source inspection. No implementation has been changed as part of this architecture pass.
