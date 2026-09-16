# Cinderhold voxel kit

Status: complete and installed, 40 combined-mesh voxel variants. WB11 passes the actual asset and native-owner coverage tests. WB15 rebuild preserves all262 combined asset/metadata files byte for byte. WB10’s six native captures have zero missing meshes or unmodeled visible owners.

CoO-original art with at most two palette swatches and240 vertices per model. [Native contracts and complete verification](CINDERHOLD-SUMPHOLD-COMPOSITION-PLAN.md).

## Verified contracts before production

| Source | Native fact | Art constraint |
|---|---|---|
| WorldMapAuthoring / Geography | Cinderhold is currently Grovelands tier3, road present, river absent; historical prose calls it tier1/2. | Preserve current map balance and scoped forest-edge identity; do not silently recolor every Grovelands chunk. |
| PruningPost stamp / ConcordFactor conversation | Factor, notice-board, CampGoodsT1 chest and campfire are the native profile. The quest accepts/posts/reports a writ, not a tree-kill counter. | Pale-coated administrative actor, broad posted notices, separate existing goods/fire owners. No fake pruning tool or invented text/date. |
| Objects.json: ConcordFactor | Creature with Conversation and GivesRep, no Trader. | Distinct factor model remains separate from the new local Weaponsmith shop. |
| TinkersForge / ForgePart | Walkable furniture with real forge/re-forge/quench verbs and native adjacency/item gates. No LightSource or Thermal Part. | Open coal bowl and rear chimney, no Unity light, damage aura or art collider. Coal color is presentation only. |
| SmithAnvil | Solid 120-weight Handling fixture; minLift18, not carryable/throwable; no ForgePart. | Separate low horn-and-waist anvil, not a duplicate crafting station. |
| Weaponsmith | Native Villager-derived `@` actor, Shop_Weaponsmith and WeaponsmithStock, boots/gloves. | Broad working forearms and apron, actual stock/equipment remain native. |
| MarketStall / VillagePopulationBuilder | MarketStall is a solid native fixture; CrunchyLocket is a dynamically created carved name-token with CompleteObjectiveOnTaken. | Stall has an open serving gap. Token has real pendant/cord geometry but no direct blueprint alias; root validates the native quest Part before selecting it. |
| WB08 native camera | Borrowed Wellmeet RoadStone produces a dominant pale road across the forest town. | Quiet mid-value slot64 road, constant full-cell top; change the art rule rather than moving the demo roads. |
| Wall blueprints | SandstoneWall is masonry. No WoodenWall/PlankFloor blueprint is shipped. | Low quiet stone masses; do not depict fireproof stone as wooden shed walls. |

## Exact kit API

`CinderholdVoxelKitLibrary.ResourcePath = "CinderholdVoxel3D/Library"`.
`Load`, `Find`, `Validate`, `Family` and `ModelId` follow the existing strict
native-kit contract. `Family` matches exact blueprint names; unknowns return null.
`ModelId` accepts only the ordered families below and variants0–3, otherwise throws.

| Ordered family | Exact alias | Palette / silhouette |
|---|---|---|
| ground | Grass, Floor | Single quiet earth slot75, constant full-cell top; buried variation only. |
| wall | SandstoneWall | Two full-cell cutaway strata12/56, height1.10; no individual decorative caps. |
| factor | ConcordFactor | Pale coat19 / dark12, folded papers and composed face. |
| notice | CinderholdNoticeBoard | Planed support8 / posted sheets19; two legs and a broad board. |
| forge | TinkersForge | Dark12 bowl/chimney and restrained coal49; open bowl below the rear chimney. |
| anvil | SmithAnvil | Dark12 foot/waist and iron13 top/horn; distinct low silhouette. |
| smith | Weaponsmith | Apron8 / warm32 forearms and face; broad shoulders. |
| stall | MarketStall | Wood8 / muted canopy35, low counter and genuinely open serving gap. |
| token | None: guarded dynamic CrunchyLocket only | Carved wood9 pendant / cord19, low portable silhouette and open loop. |
| path | RoadStone | Single mid-value packed-stone64, constant full-cell top; buried variation only. |

IDs are `cinderhold-{family}-{0…3}`, 40 entries. Ground and path have metadata kind
`ground`. Interiors, common village services, furniture, residents,
markers and containers reuse appropriate existing libraries through parent
integration. All added native actors require their actual canonical glyph and
stable body choice; the art library itself does not infer role from display names.

## Reproduction and performance

Run `CavesOfOoo.Editor.CinderholdVoxelKitBuilder.Run()` in the isolated editor.
It creates or updates mesh and prefab assets under `Assets/Resources/CinderholdVoxel3D`
in place, preserving GUIDs on rebuild, then validates the generated library.
It writes no scene. Each asset is a single already-coarse combined mesh, one
identity-transform prefab and the existing shared ring material. No new textures,
colliders, MonoBehaviours, Unity lights, animation clips or sockets.

Every mesh uses at most two palette swatches and ten boxes / 240 vertices, with
horizontal bounds inside one native cell. Runtime presentation borrows the
combined mesh directly. The offline generator is the source of all variants;
there are no manually arranged demonstration assets or frame/turn loops here.

## Verification and self-review

- WB01's actual three-seed native census preceded the exact alias budget.
- WB04 captured missing CinderholdVoxelKitLibrary and SumpholdVoxelKitLibrary
  compile errors before either production type existed. Root disregarded stale
  test results after that compile RED.
- Tests now pin all40 entries, four distinct shapes, quiet ground, exact aliases and
  negative spelling/foreign-owner controls, material/prefab/metadata consistency,
  one-cell bounds, strict input validation and malformed-copy rejection.
- The extra-Light corruption control prevents an apparently harmless visual
  addition from inventing native forge illumination. Ray tests probe real forge
  bowl openings, anvil waist/horn and notice support gaps, with occupied controls.
  Additional tests pin full-cell wall continuity, palette and working forearms.
- Current source-only audit across both kits:64 meshes, maximum240 vertices, two swatches,
  single-cell horizontal bounds, six same-kind copied source metas with unique
  GUIDs and no collisions. This is not generated-asset or visual acceptance.
- WB07 passed the original kit contracts. Independent WB08 Cinderhold-64 camera
  review confirmed the pale route overwhelmed the composition; WB09 captured
  missing stall/token/path and40-versus28 count failures before their production.
  Ray tests pin a real open stall and token loop with occupied controls, and the
  path test distinguishes its muted palette from the bright borrowed road.
- WB10 rebuilt all 64 pair models with zero C# errors and exit 0. All six native
  preview rows report zero missing meshes and zero unmodeled owners. Independent
  inspection covered Cinderhold seeds 64 and 1729: the road remains legible without
  cream glare, walls expose working interiors, and the visible right-side stall
  in 1729 reads as a roofed counter. No serious art/material defect was found.
- 🧪 Faces, tools and individual merchandise remain small at full-chunk framing.
  The inspected census rows did not include CrunchyLocket, so the token is covered
  by actual asset tests rather than a claimed native screenshot observation.
  Static previews cannot establish input feel, animation quality or sustained FPS.
- ⚪ No forging, extraction, shop, heat or destruction policy is invented by art.
  New local access to the existing smith/forge belongs to the native area work.

## Implementation log

- 2026-09-15: Read CLAUDE.md, pair plan, current map, blueprints, profile stamps,
  forge source and actual census. Corrected forge/anvil, factor/trader and stone/
  timber assumptions before production; chose seven four-variant families.
- 2026-09-15: Wrote strict kit and anatomy tests, root captured WB04 missing-type
  RED, then authored the library and offline generator. Source audit passes;
  root owns isolated export, shared renderer and final verification.
- 2026-09-15: WB07 original art tests passed. Inspected WB08 Cinderhold-64;
  authored road/late-owner expansion contracts, root captured WB09 actual RED,
  then appended stall/token/path. Source audit64 total across both kits passes;
  existing 28 Cinderhold geometry methods remain unchanged.
- 2026-09-15: WB10 exported the expanded kits and rendered six native rows with
  zero missing/unmodeled owners. Independently inspected both areas at seeds 64
  and 1729. The targeted material correction is accepted within static-view
  limits; parent owns the final test receipt and generated-byte comparison.

Files: CinderholdVoxelKitLibrary.cs, CinderholdVoxelKitBuilder.cs,
CinderholdVoxelKitTests.cs, their three metas, this document and generated kit
assets exported by the parent task. No shared integration source is owned here.
