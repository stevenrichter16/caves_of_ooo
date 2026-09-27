# C15: Spread portable art

## Status and scope

Source/import candidate published under the exact manifest; root-controlled native RED/import/GREEN is pending. Renderer integration remains owned by the Spread renderer workstream. The actual seed64 native census found undrawn Cudgel12, Hatchet15, LeatherBoots5, DetectiveNotebook1 and Torch1; these are concrete current-owner gaps, not inferred from catalog absence. `Docs/Verification/DensityCompletion/SpreadBiome/NativeCoverage/native64-gap-summary.json` is the native baseline.

This slice supplies exact portable geometry and current-state recipes. It does not change items, gameplay, equipment ownership, saves, loot or prices. There is no external parity dependency: native object definitions, current crafting/liquid/anatomy code and the accepted Spread palette are the source of truth.

## Verification sweep and corrections

| Earlier premise | Verified source and correction |
| --- | --- |
| Item inheritance finds all portables. | Inherited Physics.Takeable or Item-tag closure finds188 definitions,7 bases and181 concrete. Waterskin and LiquidFlask inherit PhysicalObject. Natural internal weapons are excluded. |
| Existing150 candidates cover the roster. |149 match current concrete definitions; stale WarlordCleaver is excluded.32 current concrete source designs were missing. Borrowed149 FBXs remain byte-identical. |
| GlassFlask is the ID. | Actual blueprint is LiquidFlask. |
| MemoryMarble/CrunchyLocket imply round marble/jewelry. | Actual descriptions say broken archive writing-surface chip and carved name-token; models follow those meanings. DetectiveNotebook is the witness-book. |
| A small creature-corpse subset is sufficient. | All107 current creature rows have nonzero corpse drops.102 use CreatureCorpse,5 MarlbackCorpse. Exact SourceBlueprint+SourceID and native corpse blueprint select23 anatomical families; rigFamily is not anatomy. |
| BrewedTonic already has BrewItemPart. | Its template does not. Actual brewing adds the part at runtime. Six first test fixtures had setup errors; corrected RED was rerun before the actual state branch. Bare template retains its base form. |
| Finite vertices imply finite source settings. | Independent review found NaN pitch bypassed range comparisons. A real one-case RED precedes the added Finite(pitch) guard. |

## Implementation

`SpreadPortableRecipes.TryRecipe(Entity,out string)` is read-only. It recognizes only current concrete or explicitly constructed identities, exact current part parents, Takeable state and visible Render without a foreign visual override; it refuses creatures, Natural weapons, abstract bases, retired names and malformed state. Receiving map/zone ownership, fog and actual equipped Body/Inventory ownership remain renderer requirements. No object origin is substituted for receiving Spread authority.

The source pack has396 exact models:181 base definitions and215 state models. The latter comprise22 additional corpse families (Marlback already has its own base source),21 material categories×8 literal severed-part types,8 shipped forged assemblies,3 non-tonic brew forms,2 constructed quest objects and12 registered flask-color swatches. Unknown corpse sources, native corpse/source mismatches, unrecognized severed types, unknown crafting components or liquids are explicit refusals. Source identity and body metadata are read, never rewritten. A bare CreatureCorpse source study is not the species-selection fallback.

`SpreadPortableSource.Validate` checks the entire bounded source pack before adoption: unique IDs, source paths/hash grammar, finite native XYZ/pitch, UInt16 buffer limits, actual nondegenerate indexed triangles, palette coordinates and one-cell dropped bounds. The editor checks every actual FBX byte hash and the exact reviewed source-pack digest before writing. It writes complete buffers directly into owned persistent Mesh assets, preventing the stale temporary-mesh-copy failure found earlier in glade work. It owns only `Assets/Resources/SpreadPortable3D`, its palette/material/prefabs and library;149 borrowed FBXs and the global palette/material are unchanged. Runtime lookup caches the validated index and exact mesh membership.

The palette keeps the accepted24 cells, adds6 explicit item accent colors, then12 exact liquid swatches. No global texture or palette is edited. All models use the native palette shader; fog, light, transient visibility and actual pickup/drop lifecycle remain native renderer responsibilities. Ground models have no scripts, colliders, actors, animation or gameplay authority. Held/worn attachment and item orientation belong to the renderer integration.

## Test receipts and self-review

- Source-design roster:33 intended RED+4 controls →37 GREEN; borrowed hashes verified.
- Static recipe suite:180 intended RED+33 controls →213 GREEN.
- Corrected dynamic suite:156 RED+9 controls →165 GREEN, then2 additional authored appendage cases. Initial six BrewItem setup errors are preserved and not counted as useful missing-feature evidence.
- Full source preflight:19 RED+2 controls →21 GREEN. Independent NaN pitch counterexample:1 RED →repair.
- Current combined standalone suite:402/402 GREEN. It exercises actual item definitions and current state metadata, not native assets.
-215 missing state-source checks →215 GREEN against actual baked geometry.
- Native library/adoption fixture first records missing-API compiler RED; actual native import/test results are pending. Actual full-census undrawn owners provide a separate native rendering baseline.
- Whole-runtime, editor-builder and all four fixture compilation use actual Unity references; compilation is not an Editor test run.

🟡 Unknown or malformed provenance is not silently made humanoid or shown as another liquid. 🟡 Source preflight NaN was repaired test-first. 🔵 Exact standalone registry state is restored by the dynamic fixture. 🧪 Native pictures and source-family fidelity remain separate gates: the source galleries were viewed, but small severed-hand/foot detail, skeleton/scorpion distinction and very fine forged bindings need native visual inspection. These state assets are not declared visually complete from coverage counts. ⚪ No guarantee of all mod-authored unknown bodies/components is made; refusals remain visible in coverage reports.

## Native acceptance plan

1. Publish only the explicit new-source/test manifest; root runs the native fixtures before import for the missing-library RED.
2. Root invokes `CavesOfOoo.Editor.SpreadPortable3DBuilder.Run`, then native library validation, exact source-buffer comparison, known/foreign mesh controls and malformed-library cases.
3. Renderer adopts only after receiving-scope/current-owner validation and proves the five observed ground gaps are closed in actual seed64 owners. Test pickup, drop, carried/equipped transitions, hide/removal, receiving non-Spread refusal, saved graphs and style-material identity.
4. Native item pictures must be viewed at gameplay scale. Worn/held gear and corpse/dynamic presentation remain named sub-gates; the initial broad ground-item gate does not imply those are all finished.

## Files

New runtime: SpreadPortableRecipes, SpreadPortableSource, SpreadPortableModelIds, SpreadPortable3DLibrary. New editor importer: SpreadPortable3DBuilder. New fixtures: SpreadPortableRecipesTests, SpreadPortableDynamicTests, SpreadPortableSourceTests, SpreadPortableLibraryTests. New source root: ArtSource/SpreadPortable3D. Owned persistent native outputs are generated only after the root-run import.

Published candidate:272 explicitly owned files (18 Assets including fresh metas,254 art/source files), zero prior-path collisions. Actual-reference runtime, importer and all four fixture compilers report zero errors. Native library13 execution has not yet run.

Actual native import completed once:396 meshes,396 prefabs, one42-cell palette, one owned material and library;829,048 total catalog vertices/414,524 triangles (not one rendered frame),149 borrowed source files preserved. The first MCP wait timed out; root observed completion and validated the existing output without rerunning. Scene state stayed unchanged. All1,591 generated outputs/metas/folder-meta are explicitly hashed in `generated-native-manifest.json`; post-import415-case native run is pending.

Root actual native adoption gate:415/415 GREEN, including402 recipe/source cases and13 native library cases. Receipt `Integration/native-portable-import-green`; this confirms exact persistent buffers/materials and model lookup, not the yet-unwired floor-item renderer. A separate21-case live fixture was published test-only for native RED before the portable receiving-scope/render hooks.

Actual live rendering RED was21 cases:10 feature failures,11 controls passing, all intended failures reported unmodeled-native-blueprint. Root and renderer reviewed the narrow post-native refinement; six hooks plus the new helper/meta are now published. All named native refusals remain authoritative; exact actual constructed-quest markers are required. Native live GREEN is pending. Worn/held gear remains separate.

### Native pickup identity correction

The first post-hook native run passed 20/21 portable cases. The remaining test assumed the picked-up dagger would remain carried. Its actual fixture creates the bare Player blueprint, with no starter items; `PickupCommand` calls `AutoEquipCommand`, so the one-unit dagger occupies the free Hand and correctly has `Equipped == player` and `InInventory == null`. No production defect was found.

Three private actual-core controls passed: empty hands auto-equip the exact item; two occupied hands retain the new item as carried; an existing carried matching stack really consumes the incoming source instead. The native fixture now pins its empty starting inventory, exact equipped actor/body/item and positive quantity before calling the real Drop command (which unequips), then retains all same-ID hide/save/removal checks. Actual-reference compilation passed. Native rerun remains pending. Receipts: `Verification/DensityCompletion/SpreadBiome/Portables/pickup-auto-equip-test-publication.json` and paired control XML.
