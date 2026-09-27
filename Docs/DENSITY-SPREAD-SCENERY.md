# C15 missing static scenery

Status: the base source, recipe, persistent library, importer and56-model pack are published. Actual Unity63/63 missing-asset RED preceded import; all210 scenery source/recipe/library checks then passed natively. All49 integration checks passed after their retained RED. The later whole-biome censuses, actual player route and reviewed images are tracked in DENSITY-SPREAD-BIOME-ART.md; the final full-suite checkpoint is below. Historical sections retain their then-pending gates. Root owns Unity and publication windows. This slice owns missing static scenery and its narrow integration; peers own existing successful regional styling, actors, terrain and equipment. No gameplay/blueprint/lore changes or BitLocker access.

## Actual gaps and corrected premises

The complete native seed64 census records BerryBush156, Signpost27, HollowStump11, Beehive27, RiverShrine19 and FlowerField53 undrawn current owners. Additional static gaps include StoneFloor211, StoneWall69, Chair15, Bed15, Well3, Oven3, WatchLantern3, CampfireGroundMarker16, WellGroundMarker4, OvenGroundMarker4, LanternGroundMarker3, Shrine3, AlchemyShelf1, AlchemyStill2, TinkersForge2, OldStump1 and PressurePlate1. BearTrap/FireTrap/SpikeTrap/WeaponRack are source-closure additions beyond that single generated sample. Actor and portable gaps are explicitly peer-owned.

- FlowerField is a current dynamic FlowerCharm/Lifespan owner, not decorative permanent terrain. Its model must leave native disappearance and tile residue intact.
- StoneFloor deliberately has authored GlyphVariants `...,.'\``. Rejecting every nonempty variants string would leave this actual family unmodeled; accept only this exact authored value and canonical RenderString.
- OldStump is **not** an Objects.json blueprint. VillagePopulationBuilder constructs the exact quest marker with glyph `o`, non-solid Physics and QuestMarkerTrigger Fact=bmo_stump_reached/Value=1. Its one observed native owner needs that exact native contract; a lookalike without the fact remains refused.
- Traps currently have visible canonical render and one-shot TriggerOnStep subclasses. There is no hidden/armed state to invent. Render.Visible=false and removal relinquish the model; no permanent visible trap may survive actual consumption.
- Ground markers are native flat terrain owners, not functional wells, ovens or campfires. They get distinct low floor markings, never fake buildings/fire/water.
- Generic Oven blueprint currently has no authored OvenPart. Do not invent a cooking device or require a part missing from real generated content.
- Existing successful regional, quest, door and state-dependent recipes are preserved. No liquids enter this pack; exact native liquid colors stay peer-owned.

## Bounded implementation milestones

1. **Test-first exact source/authority.** New pure exact recipe helper maps these27 identities only after caller's unmodeled-native-blueprint result. Exact current cached Spread/committed-lair scope and current owner/Render/Physics backlinks; canonical appearance; nonportable; no creature/item/unsupported footprint; exact relevant part types; OldStump quest fact. Pair every success with foreign/malformed/hidden/removed/refused recipe controls. Verify owner graph, RNG state and tile state unchanged. Counter-check existing successful regional/door recipes stay byte-for-byte equivalent.
2. **Own persistent assets.** New SpreadScenery3DLibrary exposes Entry{Id,Mesh,Prefab,Spec}, Material, Load/Find/ContainsMesh. It borrows accepted24-color glade material/palette and supplies explicitly distinct silhouettes. Source under ArtSource/SpreadScenery3D (private until authorized); source validator preflights exact IDs, finite bounded cuboids/colors and output types before writes. Editor builder writes only Assets/Resources/SpreadScenery3D, no active scene or source asset edits; preview/temporary resources cleaned in finally. No runtime mesh/material cloning beyond existing owned surface clones.
3. **Visual families.** First6: rounded berry shrub with sparse warm berries; wooden sign board; hollow segmented stump; suspended comb-shaped hive; low river shrine stones; low scattered flowers. Remaining21: irregular flat masonry/wall blocks; legible chair/bed/well/oven/lantern; four low ground-marker shapes; ordinary shrine; shelf/still/forge/work rack; exact quest stump; four mechanically distinct trap silhouettes. Palette stays approved24. No written symbols, deity identity, cosmology or new lore. Propose2 deterministic variants per family; variants depend on current owner/address hash only and never consume simulation RNG. WatchLantern has a real saved LightSource.Enabled field. Use separate exact lit/unlit model forms from Enabled plus positive Radius/Intensity, without adding any synthetic light; the native light map remains authority. Two extra unlit variants bring the planned asset count to56.
4. **Native gates.** Existing all-world census is behavioral RED; focused actual native fixture should fail missing models before import. After import require every explicit owner receives exact adopted mesh/material in the current visible zone, preserved collision/stats/inventory/tile state, no repeated asset construction, real hide/remove/authority loss/recovery. Include ordinary Spread and real POI/committed lair, foreign controls, actual harvested BerryBush consumption and trap consumption. Root performs import, native test execution and visual screenshot review. A JSON source pack or positive resolver alone is not visual coverage.

## Ownership and integration

Proposed new runtime paths: Gameplay-independent Rendering/SpreadSceneryRecipes.cs, SpreadScenerySource.cs and SpreadScenery3DLibrary.cs. New editor Art/SpreadSceneryBuilder.cs. New test fixtures SpreadSceneryRecipesTests, SpreadScenerySourceTests, SpreadSceneryNativeTests, SpreadSceneryIntegrationTests and dedicated SpreadSceneryAdversarialTests, each with fresh metadata. Private initial directory /tmp/coo-spread-scenery.

The scenery slice now owns the private narrow Resolve refinement and library/catalog/style/voxel registration integration, coordinated with the renderer lead. Shared hooks remain untouched until the independent native integration RED gate and a separate publication window. No shared Objects.json changes. The recipe only replaces native failure `unmodeled-native-blueprint`, never another failure or a successful native model. If a family already succeeds in a region, its scoped style remains the renderer lead's work.

## Initial verification boundary and self-review

Q1 positive/source identity: complete native142-address census plus inherited source/constructed OldStump inspection establishes real gaps, not desired content invention. Q2 counterexamples: exact appearance/part/quest/source ownership and existing refusal preservation need new tests before implementation. Q3 independent review and dedicated malformed/transaction/asset-lifetime adversarial cases pending. Q4 documentation: no native import/run or visual completion claimed; source counts are native seed64 observations and rare additions are separately named.

## Initial private test evidence

The first86-case helper corpus was executed before production: compilation failed only because SpreadSceneryRecipes did not exist. Its first implementation then ran78/86 with8 trap-positive failures. This exposed a concrete API correction: those four TriggerOnStep subclasses do not override Part.Name, so their actual runtime names include the Part suffix rather than the JSON registration name. The exact type contracts were corrected; that86-case corpus then passed. No source model or actual native visual result is claimed.

## Private implementation and retained evidence

The final current pure corpus is **147/147 GREEN**: SpreadSceneryRecipesTests86, dedicated SpreadSceneryAdversarialTests24 and SpreadScenerySourceTests37. New library native fixture63 cases is saved and compiles against actual Unity references, but has not run in Unity. Editor importer also compiles against actual Unity references, zero errors. Evidence and exact private manifest live at `Docs/Verification/DensityCompletion/SpreadBiome/Scenery/`.

The test sequence preserved these failures before their corresponding corrections:

- Missing recipe API compile RED, then8 trap-positive failures because runtime Part.Name differs from JSON registration.
- Fourteen part-impostor failures: a fake component with an identical Name could claim the visual. Definitions now use actual typed component retrieval and exact backlinks.
- Missing source API compile RED and actual source-file absence RED. The authored pack then established56 distinct model geometries from688 finite cuboids.
- Two adversarial failures: a non-solid owner with a Solid tag could misrepresent its physics, and a subnormal positive box size could collapse on import. Both now refuse; minimum box edge is0.001 native cell units.
- Offline geometry review exposed accidental saturated-green stone caps on Well. A source palette countercheck failed before the two Well variants were corrected.
- Peer review found StoneFloor declared as entity would admit the cell's synthetic grass fallback. Exact floor/marker metadata tests failed compile-first; only StoneFloor is now ground. Native per-model tests pin that metadata. Actual contribution/fallback pairing belongs to the renderer integration gate.
- Missing persistent-library API compile RED retained before library implementation. The native fixture requires every expected persistent mesh/prefab/material, exact source cuboid corners and palette cells, complete metadata and inert art-only prefab; altered library/cache controls are included. Actual missing-assets RED/import/GREEN is pending.

Source SHA: `486cd629f7e3a0179eec0bba35737712860be93785ac6f489546991ec054379a`. Original code-generated models use exactly the accepted glade24 palette, borrowed through the existing ReferenceGladeVoxelLibrary material. The preview includes28 state families (27 identities plus unlit WatchLantern), one displayed variant each; both variants are present in source and pure coverage. It is an offline silhouette review, not an in-game screenshot. Well masonry now stays stone-colored; lit/unlit lanterns differ in the small window panel without inventing light. No rune, deity identity, cosmology or new authored text was added.

## Performance and asset ownership

The recipe has a fixed dictionary of27 definitions and precomputed model-ID strings. A successful lookup creates no meshes, materials, arrays or model strings and consumes no simulation RNG. The library builds its index and mesh-membership set at validation/bind time; repeated Find/ContainsMesh calls are lookups. No MonoBehaviour or turn listener was added.

The importer completes exact source/hash/palette/geometry and all destination-type checks before writing. It borrows the approved glade material rather than editing or copying it; outputs are56 meshes,56 inert prefabs and one library under the new owned resource folder. Temporary prefab roots live in a preview scene and are destroyed in finally; the preview scene is also closed in finally. Output preflight is not a transactional rollback promise for later filesystem/import failures. A later native import gate must check borrowed assets and active-scene state remain unchanged.

## Pre-import cold-eye review and limits

Q1 symmetry: hidden/removed/depleted sources remain native authority. WatchLantern lit/unlit, harvested/intact, actual/spoof parts and required backlinks have paired pure cases; current-zone, preserved native refusals and successful regional recipes are caller integration obligations.

Q2 cross-feature consistency: public Entry{Id,Mesh,Prefab,Spec}, Material, Load/Find/ContainsMesh matches the other additive libraries. No new material family is introduced. Exact StoneFloor ground classification is the one intentional metadata distinction from other models and ground markers.

Q3 adversarial:24 dedicated recipe cases plus malformed-source cases cover spoofed parts, foreign/missing component ownership, unsupported footprints, physical contract mismatch, invalid variants/lights, duplicate/missing IDs, nonfinite/degenerate/out-of-bounds source boxes, wrong palette and incomplete catalogs. Native output/cache adversarial cases are authored but unexecuted. Renderer peer reviewed typed-state guards, source/material ownership and importer boundaries; its concrete floor classification finding was repaired before import.

Q4 honesty: standalone proves the pure source and recipe contracts, native-reference compilation proves API availability, and the offline image proves the authored silhouettes can be rendered in that preview. **None yet proves actual Unity-imported visibility, fog, batching, current-world ownership, floor fallback suppression, live harvest/trap removal or whole-biome visual completion.** Those are still required native integration and screenshot gates. This work is original CoO presentation and makes no Qud-art parity claim.

## Base publication checkpoint

The exact reviewed20-file base package is now published with absent-target and SHA-256 preconditions verified for every file; `base-publication.json` records the receipt. No renderer integration hooks or generated native assets were included. Root owns the63-case native missing-assets run and importer window; they remain pending here.

## Native base gate and integration work

Actual Unity EditMode established **63/63 missing-library/asset failures** before import (`Integration/native-scenery-equipment70-red.xml.gz`). The importer then created the exact56 meshes and56 prefabs plus library; root recorded the227-file resource inventory in `Scenery/native-import-owned-files.json` and confirmed the source and active scene were unchanged. All **210 scenery tests passed natively** afterward:86 recipes,24 dedicated adversarial,37 source and63 imported-library cases (`Integration/native-scenery-gear-terrain424-first.xml.gz`). That combined424-case run had one unrelated equipment Walk failure; it was not a scenery failure and is not claimed green here.

This closes native asset/source/type verification, not live world integration. The private `SpreadSceneryIntegrationTests` fixture currently has49 cases and compiles against the actual current runtime. It requires exact persistent mesh/palette/batch evidence for the27 families, untouched generated wilderness/village owners and a committed Spread lair; native harvest/trap consumption, saved lantern state, floor/marker fallback pairing, custom/foreign/stale refusal and unchanged prior native success/refusal. The independent source-level API compile failures are preserved, and the actual49-case native integration RED gate will precede shared renderer hooks. Current actual model coverage and native screenshot acceptance remain pending.

## Actual integration RED and premise corrections

Actual native run `d905291cee9942ff8d71a2b898c61f4d` executed49 integration cases, all failing (`Integration/native-gear167-green-fauna-scenery81-red.xml.gz`). **47 failures were the intended absent renderer-hook evidence; two exposed fixture assumptions.** FlowerField already has a successful native recipe in the ordinary Spread fixture, despite its earlier census visibility gap. The repaired test now explicitly preserves that native success; this missing-only integration does not claim FlowerField coverage. Its two authored scenery variants remain available but unselected. The separate successful-native style work owns that family. The first map-ordered Spread village contained no eligible native-missing owners; the repaired source test scans the bounded actual map, records each inspected village ID/count, and requires a real generated village with at least one eligible owner. It never manufactures a source to satisfy this check.

The private candidate adds one `SpreadSceneryWorldRecipes` wrapper and six narrow renderer changes: recipe composition inside the existing outer common-terrain refinement; exact catalog/prefab lookup; explicit persistent mesh/material style contracts; one presenter constructor argument; and56 borrowed-mesh registrations at actual Spread bind. Prior successful native recipes and named refusals return unchanged before scenery scope/state checks. The wrapper requires the exact cached receiving zone and current owner anchor, then delegates canonical appearance/typed parts to the pure recipe. Its deterministic owner/address hash consumes no simulation RNG. The existing static glade material batch remains the rendering path; no new materials, colliders, turn callbacks or gameplay mutations are introduced.

Full current-runtime and repaired native-test reference compilation both pass with zero errors. Shared integration production is still held for root review/publication and actual GREEN. The corrected49-case fixture remains49 cases, not a smaller corpus. Root's native base210 pass is unaffected; actual generated scenery screenshots and complete biome coverage remain pending.

## Integration publication checkpoint

Root authorized the exact9-entry integration publication after the independent renderer review found no concrete authority, material or mesh-lifetime blocker. Every current preimage and candidate postimage hash matched; `Scenery/integration-publication.json` records all files. Existing common-terrain and equipment hooks were preserved. The source pack had already passed actual import and210 native checks. No native integration GREEN or screenshot completion is claimed yet.

## Native integration GREEN

Actual run `1b5d6c2d56e64e228c657ff052dea3e4` passed **49/49 scenery integration cases**, plus15 common-terrain,12 style-evidence and20 restoration neighbors. Raw `Integration/native-scenery96-green-shadow5-fixture-failure.xml.gz` preserves the full101-case run; the five unrelated shadow cases had fixture-premise failures, so no whole-run green claim is made. `Scenery/integration-native-green-summary.json` records exact source selection output from the real generated village test.

This establishes actual imported current-owner drawing via exact persistent mesh/material and committed batch contributions, unchanged prior native success/refusals, generated ordinary wilderness/village and committed-lair scope, native lantern save/state, harvest/trap disappearance, strict stale/custom/foreign refusal, and StoneFloor-only fallback suppression. Independent renderer review cleared the narrow authority, borrowed-material and mesh-lifetime changes before publication. Q1/Q2 integration pairs and Q3 source/library/adversarial checks are now green in Unity. Q4 remains bounded: these assertions do not judge silhouette/lighting/occlusion quality or establish every Spread chunk/content family is finished. The offline gallery remains the only imagery inspected for this source pack so far; root's whole-biome census and native screenshots are separate gates. FlowerField continues to preserve its successful native recipe and is not newly claimed by this missing-only slice.


## Final native integration checkpoint

The complete unfiltered Unity EditMode run `fd718936b9304ca3b0441e84e92ad5d4` executed **19709 cases: 19709 passed, 0 failed and 0 skipped**, in 979.5685913 seconds. The authoritative receipt is `Docs/Verification/DensityCompletion/Integration/native-spread-full-second.json`. The earlier complete-sweep failures and test-first correction receipts remain preserved.

This full-suite result adds regression evidence to the scoped native source/owner/state controls, actual player routes and reviewed images above. It does not turn finite source censuses into every possible seed, isolated pose galleries into every animation transition, or the512-per-kind transient budget into unlimited output. The separate legacy2D liquid-shimmer defect and later content acceptance remain explicit followups.
