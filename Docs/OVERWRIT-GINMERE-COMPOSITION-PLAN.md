# Overwrit and Ginmere — native voxel composition

Status: complete and installed. Final targeted gate 396/396. Full suite 12,570 total / 12,538 passed / 32 unchanged baseline failures / zero C# errors. All 307 new feature cases pass; the suite also adds one surrounding equipment countercontrol.

## Intent and scope

Develop two distinct, repeatable area composition systems using the game's native cells and entities. The Overwrit expresses absence; Ginmere expresses a descent through an inhabited vertical landscape. Authored rules select coherent masses, routes and activity spaces. Presentation follows those owners as they move, are harvested, dry out or are destroyed.

Overwrit covers the 22 ordinary surface addresses in its authored biome, explicitly excluding Unsaying (`Overworld.2.11.0`), all runtime places and underground zones. Its deliberately limited grammar is **Blank** and **Rim**. The interior is smooth, stoneless and mostly empty, with sparse new growth of uniform height. Rim scenery faces the blank from the inhabited side. Seed variation changes restrained ecological patches and the arrangement of rim furniture, not the region's identity. No procedural bleeds, invented prior-world ruins, inhabitants, loot farms or restored settlements. Threshold bleeds, underreading and the authored Unsaying remain separate W7 work.

Ginmere covers exactly `Overworld.2.7.0`, `.1` and `.2`: **Overgrown Lip**, **Expedition Terraces**, **Drowned Basin**. Shape rules organize the rim and native ground, readable ledges and supplies, then a joined water basin with dry banks and room for nest defenders. Other sinkholes and depth 3+ retain their existing pipelines. Actual registered stairs and their return journey remain authoritative.

## Verified corrections and constraints

| Topic | Verified source / implication |
|---|---|
| Overwrit identity | `Lore/Design/THE-OVERWRIT.md` and `Docs/FELLING-WORLD-DESIGN.md` §3.5 require near-blank ground, no stones or ruins, uniform-height growth and never-procedural bleeds. Blank/Rim are the physical surface grammar; adding five busy formations would contradict canon. |
| Placeholder ecology | `OverworldZoneManager.CreateOverwritPipeline` currently borrows DesertBuilder plus Ruins tables/stamps. Cacti, ruin loot and workshops are placeholder content, not Overwrit canon. Correct ordinary cells without making legacy utility-grimoire/schematic circulation unreachable. Unsaying is named in map comments but is not currently a POI; exclude it explicitly and audit its retained native source route. |
| Ground variants | Floor/Sand/Grass carry GlyphVariants. Any new uniform ground must derive from Terrain with verified native parameters; do not silently inherit the noise. |
| Recension | `PalimpsestEcho` is a human field scribe, not a ghost. Do not present it as an undertext inhabitant. |
| Ginmere geography | `SinkholeSites` and `SinkholeArchetypes` already map Ginmere to DrownedSima. No new enum or invented mouth is needed. |
| Vertical routing | Native mouth and connection registry own travel. Visual/source review corrected the initial void premise: `FormationReachability.ClearFor` removes only solid owners and retains ground. Removing grass alone creates a still-walkable invisible void, not falling. Ginmere uses an honest bare-stone mouth landing inside its green annulus; no new abyss/fall mechanic. Existing `PrepareZoneForAccess` already generates missing sinkhole parents before lower-first access. |
| Nest defense | `PricklebrowNestPart.CollectDefenderCells` requires 16 unreserved open LOS cells at radius 2–5. A blanket bank reservation would disable it. Verify actual activation, turn enrollment and a blocked-space countercheck. Species key is `PrickleBrowGecko`. |
| Water | MirePool has liquid, water coating, thermal/destruction and gas consequences. Native owner removal must remove its water model. SprayPool is only descriptive scenery and is not a substitute. |
| Anchors | RopeAnchor is examinable scenery; no climb action ships. Art and docs must not imply a new climbing system. |
| Existing state | Camera, 1.2x view, full reveal, spawn and saves remain as configured. Only fresh generation is changed. No migration work requested. |

## Delivery sequence and gates

1. Capture mixed-file baselines and run the untouched full suite in an independent APFS project copy while the user's editor stays open.
2. Add meaningful RED specifications before implementation: eligibility/countercontrols, seed determinism, semantic geometry and reachability, native content contracts and rendering boundaries.
3. Implement pure logical plans and guarded native builders. Any new blueprints use surgical string splices, validated JSON and exact shipped Part fields. Add observability on cold generation paths.
4. Author reproducible native voxel kits: coarse silhouettes, at most two palette colors per model, four variants per repeated family, one cell ownership, no colliders/scripts in art prefabs. Generate assets offline; no runtime per-cube objects or noise textures.
5. Integrate exact region recipes, catalog/library lookup and current-owner reconciliation. Verify every visible native owner across all eligible addresses and several seeds, plus dropped loot and defenders. Unknown assets must fail clearly, not disappear silently.
6. Run focused integration tests, then independent cold-eye review. Fix the rules rather than hand-arranging any preview.
7. Add a separate adversarial gate covering malformed scope, border/seed extremes, dependency failure atomicity, runtime POIs, final-pipeline route obstructions, duplicate/moving/destructible owners, nest activation, travel reciprocity and cross-area isolation. Preserve native save roundtrip only where new persistent state is added; no migration framework.
8. Render actual manager-generated native previews at the existing gameplay angle, inspect composition/readability/repetition, iterate, and capture native data receipts. Headless captures do not establish live input feel or runtime performance.
9. Final full regression compared by exact failure identity/message to the fresh baseline, metadata/GUID audit, living-doc close-out and commits containing only this feature's changes.

## Performance plan

Plans and field evaluation run once during fresh generation. Cached exact scope sets and family IDs make renderer rejection cheap. Reuse native dirty-cell/neighbor reconciliation and static mesh batches; actors and carried/dropped items remain transient. Combined mesh assets share the existing palette material. Capture generation times and renderer counts with previews; no live FPS claim without a gameplay profile (`Docs/PERF-FOUNDATION.md`).

## Readiness and deferred work

- Native API/lore sweep and source circulation audit: complete; Drying Breeze added to the existing renewing Sill Arcanist stock after an actual RED.
- Baseline: AR01 reproduced 12,262 total / 12,230 passed / 32 pre-existing failures / zero C# errors in `/tmp/coo-two-areas-validation`; original Unity remains open.
- Logical plans, native realization, art and rendering integration: complete. AR17 passes all 307 new cases plus 43 existing manager/sinkhole cases.
- Independent cold-eye, adversarial and preview refinement: complete. AR18 records 21 final native views, zero missing source meshes and zero unmodeled visible owners. All 80 models are installed in the original project; 405 source/art files match the validated copy and all 196 task metadata GUIDs are collision-free. Full regression: AR22 has zero new failures; all 32 names/messages match AR01 exactly.
- Deferred: Overwrit W7 authored bleeds/underreading/Unsaying; descent climb-cost and other recorded debt; live play feel/profile; Vein Pressure concept in `Docs/VEIN-PRESSURE-DESIGN.md`.

## Implementation log (newest at bottom)

- 2026-09-15: Selected Overwrit and Ginmere after surveying all completed composition areas. Captured working-tree status and six mixed integration files before edits. Read-only independent reviews identified the placeholder Overwrit circulation risk and the Ginmere nest reservation trap. Baseline launched in an isolated project with its own Library.

- 2026-09-15 AR00/AR01: first isolated run exposed six missing ArtSource/Docs fixtures in the validation copy. After cloning those inputs, AR01 reproduced exactly 12,262 total / 12,230 pass / 32 pre-existing failures / zero CS. No production changes were included in that baseline.
- 2026-09-15 AR02/AR03: actual missing-type RED captured for the Overwrit plan, Ginmere plan and both voxel libraries. Production proceeded afterward. Circulation audit found ArcanistStock already carries five of six utility books and all three schematics; the remaining Drying Breeze entry is held for an actual stock-test RED before its surgical data fix. Ordinary Blank omits random cave entrances; the Rim retains that native entry route.
- 2026-09-15 AR04/AR05: native preflight and source-circulation tests exposed malformed Ginmere parts, absent renewing Drying Breeze stock, entrances outside the physical Overwrit rim, and missing sprite fallbacks. Fixed detached-instance contract validation, the one stock entry, rim placement filtering and thirteen real fallback sprites.
- 2026-09-15 AR06: direct-lower-first Ginmere travel already passed through the existing parent-first guard; no redundant manager fix. Five actor/item tests exposed body variants rerolling with movement; new-area portable bodies now retain owner-based variants.
- 2026-09-15 AR08/AR09: first native previews and art tests exposed thin detached terraces, programmed frog rows, inconsistent growth-tip height, first-action water coating absent until renewal, and several native cave owners without recipes. Native water is seeded only after staged placement commits. Art uses four variants per family, two swatches maximum and combined one-cell meshes.
- 2026-09-15 AR10/AR11: 27 actual Overwrit malformed-content failures drove exact staged-owner validation. Two runtime-POI failures drove zone-local map authority, reattached on cached/new/restored graphs without saved fields or regeneration. Six observed Ginmere geometry/mouth failures authorize the second composition pass.
- 2026-09-15 AR12/AR13: expanded from 64 to 80 models with frost vents, ice sheets, stalagmites and bone caches. Added exact native Snapjaw-role aliases. All-address native-owner coverage and scope/movement/destruction rendering checks pass; sole remaining art RED is the visibly sandy Overwrit ground, authorizing a neutral gray-green source swatch.


## Installed behavior and review outcome

**Overwrit:** 22 ordinary surface chunks use the lore’s Blank/Rim grammar.
Four native identities provide quiet ground, uniformly young growth, rare plain
waymarkers and inward-facing pilgrimage benches. Growth and furniture retain
native destruction and fire/material behavior. Cave entrances stay on actual
rim cells. Unsaying and runtime places remain outside this composition.

**Ginmere:** the existing named sima at `(2,7)` has three connected levels.
Its overgrown lip surrounds a walkable native stone landing; expedition shelves
grow from thick cliff masses; a joined mire basin has dry frog banks and room
for the nest’s sixteen native defenders. Real stairs, supplies, water sources,
container contents, hostile brains and destruction remain authoritative.

The new art consists of 16 Overwrit and 64 Ginmere models: four variants per
family, at most two palette swatches, combined meshes and one-cell ownership.
Thirteen 16×16 Overwrit fallback sprites retain binary alpha and the shared
outline. Ground deliberately has one visible appearance. No geometry-only
colliders, per-voxel GameObjects, new save fields or regeneration of existing
saved chunks is introduced.

### Fixes established by observed failures

- Native factory fail-soft behavior could commit invisible or malformed owners.
  Both composition builders now validate detached native instances before mutation.
- Random entrances in a rim-address chunk could appear in its blank interior.
  A scoped cell filter now keeps them on the physical rim, away from approaches.
- Removing placeholder ruin scatter exposed missing Drying Breeze circulation.
  The existing renewing Sill Arcanist stock now includes the native book.
- Fresh MirePool water lacked its coating until a later tick. Native leases now
  seed after the full staged placement commits; removal still allows drying.
- Surface-address checks overrode runtime towns. Zone-local map authority now
  protects generated, cached and restored native places and releases stale views.
- Movable bodies changed variant with cells, then again between Ginmere levels.
  Their native owner identity now determines the new-family body variant.
- Several cave owners lacked art recipes despite zero missing-mesh counters.
  Full visible-owner enumeration drove frost/ice/stalagmite/cache models and
  exact native Snapjaw-role aliases.
- Growth tips varied in height; the floor source was too yellow; terraces were
  detached thin strips; frogs lined up. Generator and art-source rules were
  corrected after their actual RED assertions and static-image review.

### Self-review and adversarial gate

🟡 All observed material findings above are corrected and pass AR17. Independent
review checked zone-local lifetime/restore symmetry, runtime POI changes, native
membership/removal, body movement, material content and all 21 final static views.
No remaining material finding was identified within that scope.

The dedicated native adversarial suites plus the 20-case rendering player-flow
sweep cover failure atomicity, extreme seeds, exact scope, actual lower-first
travel, reciprocal stairs, sixteen scheduled nest defenders, blocked-space retry,
water destruction/lifetime and unchanged-owner movement across depth. AR16’s five
cross-depth cases were confirmed bugs; its other fifteen new rendering cases are
honest regression pins. Direct-lower-first travel was already correct, so no
redundant parent-generation change was made.

⚪ CoO-original composition and visual design; no Qud source-parity claim.
⚪ Mouth art follows the existing walkable stone/stair abstraction. No falling,
rope climbing, authored Overwrit bleeds, resettlement or underreading is added.
🧪 Static native captures verify composition and model coverage. Live input,
HUD/lighting behavior and sustained frame rate were not verified. Recorded
native generation timings are not an FPS claim. The original Unity editor,
current spawn, camera view and full-reveal preference were not changed.

### Preview and reproduction

[Open the final seed-switching native gallery](Verification/VoxelWorld/AR18-final-native-preview/index.html).
Use fresh native zones from `OverworldZoneManager.GetZone`: Ginmere is
`Overworld.2.7.0` through `.2`; Overwrit’s finite addresses are listed in
`OverwritCompositionPlan`. Existing saved graphs remain untouched.
The reusable authoring commands are `OverwritVoxelKitBuilder.Run` and
`GinmereVoxelKitBuilder.Run`. `AreaCompositionPreviewBatch.Run` captures real
manager-created zones; its `AREA_PREVIEW_OUT` directory is required.

### Working-tree and commit boundary

The initial working tree already contained substantial unrelated work, including
untracked shared voxel infrastructure. Five task-touched tracked files match
HEAD at the captured baseline and can be committed directly. Ten mixed shared
files remain installed in the working tree; their earlier contents are not
absorbed into this feature commit. `AR20-closeout/implementation.patch` records
all fifteen exact feature deltas, and `integration.json` identifies the ten
mixed paths plus before/after SHA256 values. The patch reverse-checks against
the current files. It is a matching-baseline reproduction artifact, not a claim
that a fresh HEAD checkout contains the pre-existing untracked infrastructure.
All task-owned new source, art, tests, docs and scoped evidence ship normally.

The first temporary validation copy lost older dependencies during AR14. That
invalid run is recorded as environmental failure, never accepted as gameplay
results. Validation resumed from an independent persistent copy at
`/Users/steven/.cache/caves-of-ooo-validation/two-areas/project`. All thirteen
initial snapshots were recovered and verified against their recorded hashes.

### Final implementation log

- AR15: persistent native build succeeded with zero compiler errors; refined
  cliffs, anchored terraces, stone mouth and irregular frog banks rendered.
- AR16: 307 feature cases, 302 pass and five confirmed cross-depth variant REDs.
  All earlier native and art corrections passed.
- AR17: after the identity-only portable-body fix, 350/350 targeted cases pass,
  including all 307 new cases and 43 existing manager/sinkhole cases; zero CS.
- AR18: 21 final native images across seven compositions and three seeds;
  zero missing meshes and zero unmodeled visible native owners. Independent
  visual review and root inspection found no material remaining composition issue.
- Original-project installation: 403 source/art files byte-match validation;
  196 task metadata GUIDs are unique across all original Assets.
- AR19 full regression: 12,569 total / 12,535 pass / 34 fail / zero CS. All
  307 new cases pass. Thirty-two failures match baseline names/messages exactly;
  two older scope cases still incorrectly classify Ginmere’s surface as non-voxel.
- AR21: both scope fixtures corrected after that observed failure. The existing
  two-handed-weapon test now asserts Ginmere’s voxel equipment and adds Olderdeep
  `(4,6)` as the unchanged non-voxel control; the excluded-depth control moves
  from Ginmere surface to depth 3. All original ownership/geometry assertions
  remain. Independent review confirms no assertions were weakened. Expanded
  targeted run passes 396/396. Two additional mixed-file snapshots were captured
  before these fixture changes, bringing the exact-delta manifest to fifteen.
- AR22 full regression: 12,570 total / 12,538 passed / 32 failed / zero CS.
  All 32 failures match AR01 baseline names and messages exactly; zero new
  failures. The suite grows by 308: 307 new feature cases plus the additional
  unchanged-region equipment control. Only a test failure-message label was
  corrected afterward: `(4,6)` is Olderdeep, not Stillleaf. No assertions,
  inputs, production behavior or art changed after the final full run.
