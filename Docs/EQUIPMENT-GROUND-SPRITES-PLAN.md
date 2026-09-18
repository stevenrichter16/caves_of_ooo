# Equipment ground sprite wave — preimplementation plan

Status: GA03j IN PROGRESS,2026-09-06. Root adopted this verified proposal after GA03i1d2b5be8 closed at9599GREEN/native23PASS. Root read the complete32-case draft, exact renderer patch, current source contracts, metadata, art helpers and reference corrections. Tests are being authored first; no sprite/mapping/guard is implemented yet. Native BEFORE must precede renderer/asset changes. Preparation details below distinguish proposals from actual execution.

Goal: make the selected dropped equipment families readable, including correcting a mace currently displayed as a vial. This adds seven distinct family silhouettes for ten exact blueprints, not ten portraits or actor-worn equipment overlays. Existing glyphs, stats, ownership, equipment rules and content JSON do not require changes.

## Exact scope and readiness

| New body key | Exact blueprint(s) | Current glyph / color | Readiness |
| --- | --- | --- | --- |
| item_dagger | Dagger | / / &c | existing content; new asset and registration absent |
| item_sword | ShortSword, LongSword | / / &w, &y | same |
| item_spear | Spear | / / &W | same |
| item_boots | LeatherBoots, IronshodBoots | [ / &w, &w | same; same tint, so these material pairs remain visually identical |
| item_gloves | LeatherGloves | [ / &w | same |
| item_helmet | LeatherCap, IronHelmet | [ / &y, &y | same; same tint, so these material pairs remain visually identical |
| item_mace | Mace | ! / &w | same; actual wrong-vial identity to fix |

All ten blueprints ship their actual RenderLayer=5; tests create them through the existing EntityEquipmentContentFixture and real EntityFactory. LeatherArmor keeps item_armor. Greatsword/ForgedWeapon keep existing WeaponGround fallback. Hatchet/Battleaxe, Warhammer, Buckler/IronBuckler, Cloak/WardedCloak, rental names, named/elemental weapons and unspecified suffix matches remain outside this scope.

No new lore, simulation, save fields or creature body parts. No Qud tile-art parity claim.

## Preimplementation verification and correction table

Line anchors reflect the source read before this wave; recheck immediately before edits.

| Premise / surface | Verified reference | Correction / final contract |
| --- | --- | --- |
| Seven assets already exist | Resources directory and ResolveItemBody | Absent at preparation. Do not claim integrated art from the rejected imagegen concept. |
| Any metadata template works | weapon_ground.png.meta:1-110; item_armor meta as already reviewed in ENTITY-EQUIPMENT-PLAN | Use weapon_ground.png.meta, copying everything and changing only the GUID. It is Single/full-frame, PPU16, centered, Point, Clamp, no mipmaps, uncompressed. The torso armor Multiple/cropped template is wrong for this job. |
| Importer repairs Environment sprite mode | Assets/Editor/SpriteImportPostprocessor.cs:30-67 | Only the SpellFx branch forces Single. Environment mode must already be correct in metadata. Pixel settings are enforced globally. |
| Old style guide is current | Docs/STYLE-GUIDE.md:15-28,102-103,131 | Guide says no outlines and old Assets/Sprites path. Current user instructions explicitly require shared outline RGB(30,32,28), binary alpha, actual16px and Resources/Environment. Follow the user; record this scoped override. ArtTools/coo_outline_pass.py:28,96-117 corroborates the shipped shared ink and 4-neighbor outline helper. |
| Running the existing item generator is safe and bounded | ArtTools/coo_items.py:400-409 | Its main rewrites 30 existing item/fixture/container files. Do not run that main for this wave. Use a bounded seven-family source entry that reuses its blank/px_rows/palette helpers and outlines only new bodies, or a root-reviewed targeted invocation. Do not regenerate protected existing art. |
| Leather/iron are distinguished by current tint | actual Objects.json Render fields | Both boot variants use &w; both cap/helmet variants use &y. Seven family shapes improve category identity only. No new material distinction claimed. |
| Lantern is a real content blueprint | Objects.json census; renderer:1886-1898 | Actual blueprint is WatchLantern, RenderString!, layer1. No exact Lantern blueprint exists. Use WatchLantern in real-content tests; resulting tile name is Lantern. |
| Chest's actual glyph is [ | Objects.json Chest | Current Chest uses = at layer1. Fixture prepass is blueprint driven, so test actual content rather than a forged bracket setup. |
| A mapped item with absent art automatically gets its own glyph | renderer:1627-1629,1732,1751-1754 | False: it falls into generic family handling, including mace→vial and boots→cuirass. Add a narrow seven-new-body early null return after a missing lookup. Existing unrelated fallback behavior stays intact. |
| Item bodies need a new API/cache | renderer:287,569,751,882-889,1627,1822 | Existing dictionary, Resources.Load<Sprite>, and public ResolveItemBody are sufficient. No new public surface needed. |
| Item tint is authored color | renderer:989-1095 | Item tier leaves authoredColor=false and copies glyph foreground RGBA. Test bright/dim non-gray colors separately; no RGB→gray conversion should be introduced. |
| All item rendering enters through ChooseTile | renderer:1019-1029,1326-1350 | Fixture prepass runs earlier; chest/lantern/fixtures retain precedence. Actors and terrain remain resolved with existing policy. |
| Tile y equals zone y | renderer:948, harness tests:72-79 | Tile y=Zone.Height-1-zoneY. Positive tests check exact unflipped source entity and no mirrored overlay. |
| Invisible/fog items can use positive item mapping | renderer:994-1005,1866-1874; Cell.cs:164-173 | Visible cells choose highest visible entity; remembered fog chooses visible terrain layer≤1 only. Item art cannot reveal layer5 loot. |
| Simple tilemap teardown restores all test state | renderer has no OnDestroy, s_glyphCache:2062 | New helper explicitly restores shared glyph-cache entries and Perf long fields, destroys only its newly created nonpersistent Tile/Sprite objects, and restores equipment fixture globals. It does not destroy imported Resources assets or reset production registries. |
| NUnit NonParallelizable is available | local unity-custom/nunit.framework.dll string inspection | NonParallelizableAttribute is absent; don't add it. Existing project EditMode suite is run serially by root. New draft uses only established NUnit API plus normal Unity image/import APIs. |
| WillRenderAsSprite covers ground item tier | renderer:1321-1324 | That method checks actor predicate and entity/fixture prepass only. It is not a positive item-body probe; draft uses actual PostRender/overlay tiles. Do not expand that API in this static ground-art wave without a separate behavior requirement. |

References read: full CLAUDE.md (with truncated middle reread), ENTITY-EQUIPMENT-PLAN §8 and test/native/perf notes, ADVERSARIAL_TESTING.md:1-175, PERF-FOUNDATION.md:1-100 and352-390, relevant EnvironmentSpriteRenderer source including Init, LoadSingle, MakeTile, BuildTiles, ResolveCell, release/claim, ChooseTile, ResolveItemBody, TryEntityBasedTile, macro allocation and glyph cache; existing EnvironmentSpriteRendererHarnessTests setup and lifecycle/fog/dirty/tint tests; EnvironmentSpriteRendererTests; TerrainRenderCoverageTests fixture example; EntityEquipmentContentFixture; Cell top-visible logic; actual JSON rows; coo_items and coo_outline_pass helpers; SpriteImportPostprocessor; verified metadata; full STYLE-GUIDE.

Qud comparison: /Users/steven/qud-decompiled-project/XRL.World.Parts/Render.cs:15-57 exposes authored Tile, RenderString, ColorString, DetailColor, TileColor, RenderLayer, and visibility flags. This change uses CoO's existing family→Resources/tilemap abstraction, not a port of Qud's rendering pipeline. Classification: CoO-original content plus a CoO fallback correction. Do not claim matching Qud's material tint, sprite layout, identity or item icon rules.

## Implementation outline and protected boundaries

A proposed minimal renderer patch is /tmp/codex-equipment-ground-renderer.patch. Its source SHA and hunk inventory are /tmp/codex-equipment-ground-renderer-provenance.json. Neither was applied. It changes exactly three existing seams:

1. BuildTiles preload list adds the seven body keys. Seven runtime Tiles loaded once with existing Resources path.
2. ResolveItemBody's exact switch adds ten blueprint names to seven families. No suffix widening.
3. ChooseTile's item-body branch first returns existing lookup results, then returns null for seven new bodies when lookup is absent. All other item families continue to existing generic handling. Null cached value already returns null through the lookup branch.

Both EnvironmentSpriteRenderer.cs and its old harness are protected dirty. Root must preserve their preexisting changes, review the actual current file before applying any draft, and stage only these attributable renderer hunks. Put tests in a new dedicated file; do not modify the old protected harness. Retain source backup/hash before edits. Never stage the whole dirty renderer file.

Art: seven new snake_case PNGs and GUID-only template copies under Assets/Resources/Sprites/Environment. Actual16×16 RGBA, alpha0/255, shared ink(30,32,28), transparent background, neutral values compatible with current glyph tint, readable silhouette at native size. Dagger short blade; sword longer blade/hilt; spear long shaft/head; boots boot garment; gloves hand garment; helmet head garment; mace blunt head/shaft. Three to four fixture variants do not apply to these individual equipment families. Avoid new vivid fixed palettes that multiply badly with glyph color.

## TDD milestones and test draft

/tmp/codex-equipment-ground-sprite-adversarial.cs drafts one new class, GameAuditEquipmentGroundSpriteAdversarialTests, and local GroundSpriteAuditFixture. It has **32 NUnit cases**:7[TestCaseSource] asset cases,7 explicit missing-family cases,18 ordinary tests. It has not been compiled or run. Root must review the entire file and validate against the current branch before adoption.

Suggested staged RED flow preserves meaningful failure evidence:

1. Adopt plan and test draft only; run all32 and retain exact compile/result output. Missing files/mappings are expected preconditions, not yet proof of the fallback defect. Classify any fixture mistake honestly.
2. Add seven compliant art assets and import metadata plus renderer preload/exact mapping hunks only. Run again. The seven removed-registration tests should now pass their explicit preconditions and expose actual generic-fallback failure. This is the genuine RED evidence for the missing-resource behavioral fix. Some pure controls should already pass.
3. Apply the narrow ChooseTile missing-family guard and rerun all32, existing environment harness/resolver/coverage suites, then root's required full suite. No total claim until actual XML result.
4. Independent cold-eye taxonomy and Qud classification review; formulate6–12 further player-flow hypotheses if review reports no real bugs, classify confirmed defects/pinned-correct/visual-only. Fix material findings, update docs and preserve audit artifacts.

Coverage matrix:

| Cases | Observable invariant / counter |
| --- | --- |
| 7asset | Actual PNG full16rect, imported Resources Sprite, Single/PPU/pivot/Point/Clamp/no mips/uncompressed, source binary alpha with nonempty body and shared ink; empty/opaque-square/ink-only bad art fail |
| 7missing registration | Explicit loaded tile precondition; remove only one new family; own exact glyph/color survives, unrelated tonic still renders |
| All ten exact names | Actual EntityFactory Render layer5; real Sprite identity through tilemap; unflipped zone row and displaced glyph |
| Seven distinct masks/GUIDs | No copied silhouette among seven families; unique asset GUIDs different from template; global GUID collision sweep remains a separate root gate |
| Exact namespace / generic controls | Null/empty/whitespace/case/rental/unknown/unreviewed aliases stay unmapped; Greatsword/ForgedWeapon keep generic blade; FireTonic still resolves |
| Null cached tile | Mace remains its own glyph, never vial |
| Existing families | FireTonic red vial, LeatherArmor torso, missing grimoire retains its old honest '+' behavior |
| Fixture priority | Actual Chest and WatchLantern keep fixture art |
| Actor-over-item | Explicit higher-layer Villager hides dagger, leaving reveals it |
| Fog/unexplored/invisible | Known current art becomes absent in remembered fog; unexplored hidden versus revealed; invisible upper item does not suppress lower visible item |
| Full/incremental replacement | Stationary dagger→boots and mace→gloves replace old claims and colors |
| Item removal / stale glyph | Removed item exposes newly painted unknown '?' and its tint |
| Physical instances / tint | Two distinct Dagger IDs share cached body but have independent cell colors |
| Bright/dim | Exact non-gray RGBA survives; no authored-gray override; no material distinction assertion |
| Release/toggle | UI-style release restores glyph/color; disabled art restores glyph and reenable reclaims |
| Dirty scope | One dirty cell resolves1–9 cells, preserves distant item tile/color |

Fixture honesty: foreground colors are painted by the harness as ZoneRenderer would paint them; these tests prove EnvironmentSpriteRenderer's copy/claim behavior, not upstream lighting calculations. Layer20 actor is a deliberate fixture, not a claim about Villager's authored layer. Fog test simulates the real full clear/NotifyMainTilemapCleared before hidden repaint. No player keyboard, pickup API, save round-trip, FPS or visual recognition is proven by these tests.

## Performance

The resolver executes per visible cell. Keep preloading at Init, exact switches, a single existing dictionary lookup, no per-cell allocations/LINQ/resource loads, and current dirty-neighborhood logic. No new dirty hooks needed: static classification/art changes have no new gameplay state producer. Existing Perf counters and COO.EnvSprites.PostRender marker suffice; no new production diag event/action gate is introduced.

Root must capture75s BEFORE renderer edits and AFTER using the same native benchmark source/arena/content/seed, preserving provenance and native isolation teardown. Three25s phases: populated ground-items idle; deterministic walking/full redraw; bounded native pickup/drop/incremental redraw. Prefer actual input flows for the interaction phase and explicitly document any direct API stimulus. Capture COO.EnvSprites.PostRender, COO.ZoneRenderer.LateUpdate, input marker, engine/main-thread timing when available, GC allocated frame; retain raw frames and environment Perf counter deltas (Frames/FullPasses/IncrementalPasses/CellsResolved/ClaimsMade/TilemapWrites/TotalTicks/MaxTicks). Validate phase/work counts, no exception logs, current run stamp and removed private save root. Compare p99/max, not means alone. New art changes rendered work/geometry; don't claim statistically equivalent/no regression/build FPS or subjective smoothness from a single A/B. If a marker is unavailable, report it, not zero.

## Native and visual acceptance

A separate readable item display must use the ten actual item blueprints under normal/dim light, plus FireTonic/LeatherArmor/Chest/WatchLantern controls. Native checks should report actual loaded sprites and renderer claims, removal/pickup replacement, overlay release/UI and save-isolation teardown. Root must visually inspect scene captures at1×/normal play scale and nearest-neighbor enlarged contact sheet: silhouette distinguishability, transparent edges, outline consistency, no clipping, alignment and glyph tint. Script tile-name checks are insufficient for readability. Preserve screenshots as evidence with exact scope.

Can verify by script: asset sizes/source alpha/import settings, real content mapping/resource identity, exact glyph fallback, visibility/claim/tint lifecycle, native action outcomes and finite profile workloads. Can verify by image inspection: displayed silhouettes, visible edge/scale/alignment problems in those captures. Cannot verify: every monitor/lighting state, all world clutter combinations, player comfort, actor-worn equipment, item-specific material variants, Qud visual parity, global game bug freedom.

## Living docs, review, files and implementation log

Root should adopt this into Docs/EQUIPMENT-GROUND-SPRITES-PLAN.md (or an explicit separate phase section), and update ENTITY-EQUIPMENT-PLAN sprite status, WORK-LOG-2026-09-06 actual improvements, GAME-SYSTEM-AUDIT and MECHANICS-FLOW-SMOOTHING if they track this wave. Include current user override of stale style-guide outline/path claims; do not rewrite unrelated historical style decisions.

Expected owned files: new bounded ArtTools source or surgically extended coo_items entry (root choice), seven PNGs + metadata, three protected renderer hunks only, new32-case test source + metadata, root-owned native bench/scenario + metadata, living docs and verification report/proofs. No Objects.json change or new equipment mechanics needed.

Precommit self-review format: severity-marked findings, Q1symmetry/Q2cross-family/Q3counter-checks/Q4doc drift, taxonomy plus Qud classification, hypothesis results, GUID collision audit and complete owned diff attribution. Fix yellow/red before commit. Record precise test totals and known visual/perf limits. Any expanded scope needs explicit documentary rationale, not silently broader matching.

Implementation log (bottom-up status):
-2026-09-06: read-only preparation completed. Seven-family exact scope verified; same-tint materials, WatchLantern/Chest content corrections, missing-family fallback, stale style guide, generator blast radius and absent NonParallelizable API recorded.32-case draft and three-hunk renderer sketch prepared under/tmp only. No production art or code changed; no tests run.

## Root adoption log

- Root read and accepted exact seven-family/ten-blueprint scope and32-case test draft. Standing user outline/path rules override stale STYLE-GUIDE text. The existing editable pixel source is the appropriate final16px authoring format; the built-in dagger concept was rejected, never converted into an alleged final sprite. A bounded new ArtTools entry will generate only seven new bodies, preserving old art.
- True native BEFORE must run with current generic assets/mappings and the final native harness before production sprite changes. Separate missing-resource RED follows assets/preload/exact mapping but precedes the ChooseTile guard.

- Initial actual RED:32cases,3PASS/29FAIL,0CS (GA03j-initial-red.xml.gz/log.gz). Missing assets/mappings explain failures; missing-resource guard cases stop at explicit registration preconditions and do not yet prove that later branch. Three independent existing-family/control cases pass.
