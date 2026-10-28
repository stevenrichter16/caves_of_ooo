# Greatdew native body

Status: original art and exact-owner integration are bounded Editor-verified;243native cases passed,3existing allocation controls inconclusive. Actual world/reader/snare/held movement/save state are verified. Root owns Unity, art production, ordinary play, publication and package decisions. This pass covers one existing exact plant hazard, not Sodden area polish or a new mechanic.

## Source sweep and corrections

| Reference | Verified contract / correction |
| --- | --- |
| `Assets/Resources/Content/Blueprints/Objects.json`, Greatdew | Exact `Greatdew`, glyph `(`, cyan `&C`, layer2, inherited empty background, non-solid/non-takeable Physics, Vegetation, Plant/Organic material, structural10HP, Examinable. No Creature Hitpoints, Thermal, crop or harvest part. |
| `Assets/Scripts/Gameplay/Entities/TriggerOnStepPart.cs`, GreatdewSnarePart | Repeatable step-trigger, baseline55% chance, Rooted4 and Acidic1.0. Entering an adjacent cell is not a trigger; waiting does not implement a distinct struggle mechanic. Rendering must never dispatch the trigger or advance RNG. |
| Actual root Continue of preserved `sodden-frogs-after157` | Player is40,10, HP38, not the previously inferred40,12. Exact disclosed Greatdew25501 is24,21 in OpenMire18.8. Current before-reader/capture evidence is root-owned. |
| Existing SoddenNativeArt/Population tests | Legacy3D/native-model Greatdew recipes exist. They do not supply an approved flat16×24 raster. Preserve these assets/routes unchanged. WineLeafSundew and PebbleSundew remain different identities. |
| `TrapJammingPart.IsSupported` and TrapJammingContentTests | Only four exact mechanical traps support timber jamming. Greatdew does not; do not invent a disabled Greatdew state from an added Jammed flag. |
| Existing fallback, shared frog and VineWall catalogs | Current graph, disclosure, selected owner and authorized Examine remain caller-owned. New exact-known refusal must be authoritative in all four Greybox routes. Optional missing/malformed injection cannot silently reload assets or borrow unrelated local art. |

## Runtime design

Add a small disposable `FlatPixelGreatdewCatalog`, default resource `Sprites/Live16x24/greatdew`, one complete16×24 frame at16PPU. Its injected Texture2D constructor borrows pixels; it owns only the cached Tile/Sprite. No atlas scan, coordinate variants, animation, per-frame texture copy, event dispatch or world traversal.

Eligible identity requires unique owned Render, Physics, Examinable, Material, Destructible and GreatdewSnare parts. The snare must also be the sole TriggerOnStep, repeatable with no faction filter. Preserve valid mutable tuning rather than pinning balance to55/4/1: chance0–100, nonnegative hold and finite nonnegative corrosion. The neutral body makes no guaranteed-capture or occupied-state claim. Positive structural damage, renamed text and valid tuning retain the body. Gone/depleted, custom-reskinned, carried/equipped, malformed, foreign-owned, contradictory service/item/creature/crop/harvest/liquid/thermal meanings and stale/multiple-cell footprints refuse it. No Thermal state is invented.

Wire only legacy world, legacy subject, modern world and modern subject routes in `FlatPixelGreyboxCatalog`, with lazy optional resource loading and disposal. Existing constructors remain source-compatible. Exact refusal returns the truthful existing glyph. Ordinary color cues come from the one existing Render event, not a second presentation-side dispatch; reader remains the neutral body. Actual movement, structural destruction and snare effects remain untouched.

## Test-first verification

New `FlatPixelGreatdewTests` uses reflection so absent adapter yields an assertion RED without a production compile dependency. Real factory owners and submitted ZoneRenderer tilemaps prove body/reader selection and color; injected textures prove missing/malformed and lifetime contracts. Initial cases cover admitted/ordinary/underground/synthetic current graphs, ownership/reskin/service/snare counterchecks, disclosure/coverage/current-graph/footprint refusal, exact alias rejection, real non-solid movement with deterministic existing RNG, actual Wet cue through one Render, structural damage/destruction and idle incremental repaint. Every refusal starts from a successful same-owner control where practical.

Root records actual native RED before runtime edits. After minimal integration, run the focused fixture plus related existing shared props/frogs/global presentation and Greatdew mechanic tests. No raw test count is acceptance. Root then captures the same actual owner and reader, a visible safe approach, ordinary interaction/damage or step outcome only if actually observed, and save/Continue. Do not inject health, location, RNG or effects into ordinary play. Source tests may exercise deterministic controls and are labeled separately.

## Review and limits

- 🟢 Bounded native and Editor verification completed below; package and whole-area acceptance remain open.
- ⚪ Broad mire salience, sparse landscape composition, other flora/equipment and all55+5 area-polish scopes remain open.
- No new gameplay, loot, balance, generation, collision, save migration, global grading or approved-art edits.
- Qud reference is the established restrained flat grid presentation; no external sprite copying or parity claim.
- Performance claim is bounded cached-view behavior and idle no-repaint assertions; no zero-allocation, FPS, hardware or Steam Deck claim.

Planned files: new catalog+meta, new fixture+meta, narrow Greybox integration, this living document. Root adds native art and actual evidence separately. Preserve normal saves, approved art, prior builds and49foreign changes. No cleanup.

## Native RED and implementation checkpoint

Root observed native job `bb1f26f492f24c1aa9755d4314bfea4c`:21/21 cases failed with the intended absent-adapter or glyph assertions. Full uncapped failure identities/progress are retained in `Docs/Verification/Live16x24/greatdew-native-red.json`; the tool result payload is null, so no complete XML or per-case Results object is claimed. An earlier empty-discovery attempt is not RED evidence. Independent fixture/API review found no blocker before implementation.

The small new catalog and four narrow Greybox routes are now written after RED. One cached tile/sprite is lazily created for the exact owner; default resource and explicit missing/malformed injection remain separate. All constructors and existing family routes remain intact, with disposal and the existing one-Render-event color cue wired. No simulation, approved texture, source content or Unity setting changes were made by the runtime agent. Native GREEN, actual world/reader/interaction and final independent review remain pending.

## Completed bounded Editor slice — 2026-10-28 11:16 UTC

**Visible result:** the disclosed exact Greatdew now uses one original16×24 hooked-stem/dew-bead sprite in world and authorized examine. Approved neighboring art and all simulation/content files are unchanged. It is one closed body gap, not a completed mire kit or polished biome.

**Native:** intended21/21RED preceded implementation. Final job `00fc417ec0ad48babd8aa080bc84478f` returned246 distinct case records:243Passed,3Inconclusive,0Failed. All21new cases pass. The three existing regional allocation controls could not detect their positive control in this Editor; no allocation claim is made. The tool summary lists243, while case list/progress lists246; preserve and report both correctly. Receipt `greatdew-native-green.json` contains full cases. Earlier empty discovery is retained and never counted as RED.

**Actual gameplay:** fixture157 loads player40,10 HP38 in OpenMire18.8.0. Before/after01 and reader02 show exact Greatdew25501 at24,21, Plant10/10, non-solid. Ordinary input: close reader/Look, south,16west,9south; approaching frogs caused real combat, one movement became an attack, GinFrog died and playerHP fell38→27, ending24,19. One more south reaches24,20 with no effects. The next south enters24,21: Greatdew closes, Rooted and Acidic apply, HP22. Attempted east stays24,21/HP22. F5 writes turn2630. No gameplay state/RNG/health/position was injected. The acquired combat context is retained, not described as a clean safe route.

**Save/restart:** initial fixture extraction selected the unchanged launch marker. Header/timestamp checks caught this before reuse; corrected `Fixtures/greatdew-held161` is the actual GameID F5 output, SHA receipts in `greatdew-checkpoint161.json`. Fresh isolated Continue preserves owner/HP/effects. World crop changes408/564000pixels, not an exact match; a subsequent idle capture changes0. Player overlaps the plant, so exposed plant-world equality across restart is unverified. Independent review found no new significant visual defect; cause of the small surrounding color differences remains unproven. Do not relabel this exact matching. Captures01–08 and readonly state receipts are actual Unity output. Expiry/destruction remain native-only.

**Editable art:** original geometry84unit-pixel quads,26named groups,7colors,three fluid beads/two basal leaves; PNG16×24SHA `3dab2ae235b74ef5ea648348705ff9aa73f576c2e09a2cfb372879bfece7ec33`. Two fresh renders agree; saved blend reopens, four corruption controls reject bad source. Source guard correction preserves the original1163-entry snapshot historically while active14pins cover only required approved PNGs/concept/baselines/helpers, avoiding foreign unpublished metadata. Hashes were selected from the original snapshot, not silently rebased. Source includes concept labeled AI, part critique, builder, cells, palette, native/contact outputs, editable blend and verification receipts.

**Profile:** 144.83seconds/32389Editor frames including normal play/readers/idle/tools. Renderer marker mean0.166ms,p990.239ms,max144.2ms; main-thread mean4.46ms,max2244.8ms. Recorded counters are bounded observations, not a controlled speedup, sustainedFPS or Deck result. Native cached-view/no-idle-repaint controls pass; allocation probes are inconclusive as above.

**Preservation/publication:** both owned after/restart roots archived before Finish. Play=false, SaveRootOverride=null, scene clean. All49foreign file hashes unchanged. Six Unity-generated owned PNGmeta whitespace deltas backed up then restored exactly. Original art, normal saves and current/prior builds preserved; no storage deletion. Exact source/art/tests/evidence/living docs are the publication unit. Current packages stayfa553ca36; batch this with another coherent visible family before rebuilding.

**Self-review:** 🟢 independent source/runtime/actual-image review found no actionable blocker in this narrow body change. 🔵 no simulation, generation, collision, services or global palette changes. 🧪 actual expiry/destruction, exposed-body restart and packaged execution remain unverified. ⚪ broad quiet patches, dark mire hazard salience and all55+5polish scopes stay open. See `greatdew-independent-review.md`, `greatdew-preservation.json`, `greatdew-editor-profile.json` and `greatdew-restart-comparison.json`.
