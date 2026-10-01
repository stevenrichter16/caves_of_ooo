# Biome botanical art implementation review

2026-10-01. Root coordinates native Unity, imports and Play; this agent has not invoked Unity.

## Completed

- Independent280-form source/library/importer for35 new species: three growth stages×dry/wet, seed, useful harvest. Original35 botanical structures; 3,845 boxes, maximum30 per form, one24-swatch material.
- Exact catalogue-driven seed/crop/yield dispatch in native wilderness and village presenters. Growth/soil state follows current CropPart; neither origin biome nor a decoration controls selection. Existing cultivated ground overlay is reused.
- Narrow current-manager ordinary cave scope after native0/3 RED. String-only scope unchanged; detached same-ID zones denied. Current depth-specific wall/floor owners borrow shipped Wall/Floor geometry without creating or mutating entities.
- Seven Blender PNG sheets and editable scenes reviewed (Spread, Sodden, Beating, Grovelands, Overwrit, Stump, Cave). First pass revealed repeated bright furrow bars overwhelming plants; refined to restrained local soil crumbs, leaving real cultivated furrows to the saved terrain owner. Stems, pods, loops, caps, fans and rosettes now differ visibly.
- Previous37 source preserved byte-for-byte, SHA256 `14664f5ed987b81e361a2eea25734bcfdaba2c251da0a24af84b41c601ca843b`.

## Test evidence

- `source-red.log`: new source missing before production;3 errors. `source-green.log`:3/3 pass after production, checking exact280 identity, five per ecology, bounds, distinct35 mature/harvest/seed geometries, all stage changes and wet/dry geometry/paint pairing, plus old37 hash.
- `tests-red-compile.log`: compile-compatible native test setup before production, exit0.
- Root exported `../cave-red-full-result.json`:0/3 meaningful native failures before cave fix (current cave refused, native SandstoneWall unmodeled, cave presenter unready).
- `CavesOfOoo-compile.log`, `Assembly-CSharp-Editor-compile.log`, `EditModeTests-compile.log`: native-reference compiler exit0 after production. Compilation establishes API validity only, not rendered gameplay.
- Native art tests:34 `BiomeCropArtTests` +3 `BiomeCropCaveArtTests` all GREEN in root `../first-native-result.json` (overall214cases,207pass;7separate placement failures under repair). All280 forms imported successfully. Previous30 crop/repair cases remain regression targets.

## Cold-eye Q1–Q4

Q1 symmetry: crop admission, catalogue spec, prefab lookup, material registration, voxel-mesh identity and village views all consume the same new finite library. Bind/release clears the village reference; actual stage/soil changes replace views, unchanged IDs reuse roots, disappearance removes them. Ordinary-cave bind-state authority is rechecked with surface/profile authority so a receiving-map change cannot retain a stale camera.

Q2 consistency: one gameplay catalogue provides exact blueprint IDs/model stems/native crop stages. Import validates source metadata against this catalogue. Both renderers read current owners and leave native collision/actions/saves untouched. Wet/dry geometry intentionally identical; soil palette alone changes. All35 loose harvest models retain their actual blueprint owners.

Q3 counters:11 current-owner/appearance/state negative cases, stale saved reference vs replacement, removal/pickup/drop, town-owner preservation and unchanged-view reuse. Cave tests deny unmanaged address/clone and require actual depth floor/wall owners. No claim that raw source previews prove gameplay. Parent owns further gameplay/adversarial integration.

Q4 docs: model count280 equals35×8; no fourth harvested plant state, no old37 replacement, no new animation or emitted light from art. Readme names exact importer/source path. Cave support is receiving-manager authority, not a blanket string-ID renderer expansion.

No significant art-code findings remain in the bounded read-through. Native37/37 confirms the new art and cave integration. Real keyboard/Play route and existing renderer regressions remain parent acceptance gates. Lower-severity limits: sprout structure is a reduced unopened version of its own species; seed differences are subtle at distance; generic underground stone shares existing Wall/Floor geometry rather than receiving a separate geological material kit. Loose harvests have dedicated forms; newly equippable harvests retain existing equipment attachment/fallback behavior. Do not claim35 new equipment rigs or35 bespoke harvest animations.

## Native route candidate, frozen for root execution

`Assets/Scripts/Scenarios/Custom/ReferenceGladeNativeBiomeCrops.cs` reuses the existing isolated keyboard harness. It has 15 botanical checks plus the ordinary-player check: generated Marlroot harvest/pickup; real clod-to-FireClay processing; returned-seed planting; the starting grimoire's actual rain; paid wet growth to sprout and standing ripe; second harvest; F5/F6 graph replacement/depletion; actual connected Cave Lampvein harvest and portable lighting. Exactly two disclosed travel shortcuts; no crop/stage/moisture/material/equipment/stat grants. Cave selection observes an actual cached incoming StairsDown edge with physical endpoint stairs and a usable path from arrival to the patch approach. Lamp handling includes both carried and autoequipped identities, and uses the real equipment UI to stow the lamp before the stowed-versus-dropped light comparison.

All three native-reference assemblies compiled at exit0 after this partial and root launcher seams. That checks API compatibility; root must execute the Play route before making gameplay claims. The source pack remains unchanged during route preparation.

First live route `686ef8095d2c487ba99bed010d0e4429` stopped before any travel because its 12-cell hostile exclusion rejected both existing Marlroot sources. This was a demonstration selection limit, not absent content. Narrow route correction considers all actual candidates/four adjacent approaches, uses the established normal travel clearance2, and selects the greatest measured bed/approach threat separation. It records distances and leaves danger/NPC scheduling unchanged; actual growth still stops below11HP. Cave approach also uses clearance2 while retaining physical incoming stairs/path checks. Native-reference compile remains exit0 for all three assemblies. Root permits at most two more native attempts; hostile pressure must be reported instead of changing the seed/world/player.

Second route `91e607abb7b24e379a59c7653eca3f38` passed eight checks, including native harvest/pickup, Prepare to FireClay, returned-seed planting, grimoire/rain. It stopped at the compound growth-safety guard before sprout; the rain screenshot shows40HP but does not establish which later predicate failed. No blame assigned to gameplay without evidence. The route now records the exact failed predicates and current crop/actor/threat state and captures a failure screenshot before throwing. No source choice or production behavior changed. Reference compilation passes all three assemblies. A final native attempt remains parent-controlled.

Third route `970f23d7fdec4de69d79c930d0d15e0d` again reached eight passes; the observer itself then dereferenced a missing player cell before saving guard details. That observer bug is fixed with a nullable location and actual last12 message-log entries. Missing spatial membership is consistent with committed death (CombatSystem removes deceased owners), but diagnosis awaits recorded HP/death/log evidence. No production fix was inferred.

A separately labelled cave-only mode now reuses the same actual source selection and keyboard cave route. Launcher calls ConfigureBotanicalCaveOnly before Initialize(biomeCrops:true); completion requires4 cave checks plus the initial ordinary-player check, exactly1 disclosed travel and at least3 screenshots. It makes no surface-growth claim and permits no grants. All three assemblies compile after the factorization. Root owns final native execution and launcher/report seam.

Final surface diagnostic `72856a61409344b797a54033b6a81700` establishes **actual hostile interruption**, not crop failure: same zone/plant/actor alltrue; input Normal; HP3, deathHandledfalse; six paid waits, one rain cast; growth8/moisture32. An adjacent MarlbackScrabbler targeted the player; actual log records9+21+7 damage and stun. The intact crop had advanced normally. Root visually reviewed the failure screenshot. The route correctly stopped below11HP; no seed/world/player change and no further surface retry. Eight earlier acceptance checks passed. Standing ripe/repeat-harvest/save-reload in this exposed Play route remain unverified; native all35 lifecycle tests are separate evidence, not substituted Play claims. Root is executing the independently scoped cave-only route.

Cave-only native receipt `1c383cb0539e4c089cf2df1361826b9e` passes **5/5**, zero errors, complete=true, exactly one labelled travel. Root visually reviewed the real cave Examine and dropped Lampvein-fan screenshots. This validates the current native cave/crop rendering, real harvesting/pickup, and actual portable light interaction. It does not turn the separately interrupted surface route into a complete growth/save demonstration. Source and assets remained frozen throughout final execution.
