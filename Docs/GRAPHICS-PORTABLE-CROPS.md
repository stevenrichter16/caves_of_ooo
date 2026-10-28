# Ordinary planted gladroot and emberwheat

**Parked by direct user override,2026-10-28:** Environmental tiles/walls/floors/ground/ground-cover polish now takes priority across all biomes. The tested reuse remains preserved, but actual acceptance covers only dry planted marks. No further crop work. See [parked checkpoint](Verification/Live16x24/portable-crops-parked-checkpoint.md), including the failed post-combat archive/no170 correction.

Status,2026-10-28: actual baseline, native RED8 and GREEN240/240 complete; minimal two-file integration remains frozen for ordinary after/growth/restart. Root owns Unity execution and publication. This extends two existing approved crop bodies outside the opening; it does not add crop mechanics, a third standing stage, new art or general biome acceptance.

## Source corrections before implementation

| Question or earlier shorthand | Verified source and correction |
| --- | --- |
| Is the missing third frame ripe art? | No. `FlatPixelOpeningArtCatalog.TryResolvePlot` maps CandyCarrotCrop and EmberwheatCrop to stages0/1, with `ripe=-1` an unused sentinel. `CropPart` and `CropSystem.TryMature` automatically replace a completed legacy crop with finite loose yield; these plants never stand at stage2. |
| Does each ordinary turn advance one authored unit? | No. Public `CropTime.Reconcile` uses10 elapsed world ticks per growth/moisture unit and stamps a new/legacy owner before advancement. `CropSystem.OnTickEnd` is a retained explicit one-unit fixture driver. Tests use the real stamped reconciliation API. Authored20/35 stage lengths are not a promise of20/35 player inputs. |
| Is dry, blocked or composted growth a different species/state? | No. Moisture and progress may change; compost changes stage length. Missing Factory holds stage1 at the release boundary and retries on the next moist interval. Render selection must be read-only, preserve this body and not require original timing/count/seed-field values. |
| Is all art missing? | No. The exact shared planted mark is `opening-states` frame6; gladroot and emberwheat sprouts are `north-plot` frames0/1. Existing OpeningArt caches both atlas groups. Modern nonlocal Greybox admission omits these planted owners. Legacy local world/subject routes also need authoritative refusal so malformed exact identities cannot borrow the older broad plot mapping. |
| Are all crops or reserve rights included? | No. Marlroot/Pitchpod have real standing ripe stages and remain regional. Nella, her tray/cords, prepared soil and other crops retain their existing contracts. Released CandyCarrot/Emberwheat produce art outside the opening is not automatically promised by this slice. |
| Which owners are actually known? | Earned `Fixtures/portable-seeds-planted166` and `portable-seeds-restarted-ground.json` establish CandyCarrotCrop25521 at42,10 and EmberwheatCrop25522 at41,10, OpenMire18.8.0, player41,10 HP18 turn2510. The prior image exposes the small gladroot glyph; the player covers the emberwheat cell. Root must normally move and capture both before claiming both visible baselines. |

References read: `Objects.json` exact two crop blueprints; `CropPart.cs`; `CropTime.cs`; `CropSystem.cs`; `CropYieldService.cs`; `FlatPixelOpeningArtCatalog.cs`; all four world/subject seams in `FlatPixelGreyboxCatalog.cs`; `FlatPixelNorthPlotTests`, `FlatPixelPlotProductTests`, `FlatPixelPortableSeedTests`, `ConnectedCropTimeTests`, and actual painter/reader helpers in `FlatPixelBucklerTests`. No Qud asset or mechanic is imported: the established flat16×24 readability direction is the reference.

## Minimal integration after RED

Reuse the two cached atlas groups inside OpeningArt through an exact portable-crop selector, then add the two exact identities to Greybox's legacy/modern world and subject routes. Avoid a new catalog, atlas, timer, world scan or constructor overload. An exact identity's refusal is authoritative even inside the opening. Preserve current world graph/cell/top-owner/FOV authority and authorized Examine documents. Accept only actual owned render/physics/crop/examinable parts and present optional single-cell footprint/status parts; reject reskins, foreign/duplicate required parts, actor/item/terrain/container roles, takeable/solid/custodied plants, wrong automatic-yield semantics or invalid standing stage. Do not pin mutable moisture/progress, stage duration, health/material, yield count2, or unused legacy seed-yield metadata.

Frame contract: stage0 uses source rect(96,0,16,24) in192×24 states; stage1 uses(0,0,16,24) gladroot or(16,0,16,24) emberwheat in128×24 north-plot.16PPU, centered pivot, Point/Clamp/no mipmaps, white native palette, no collider. Existing four-input states-only and nine-input states/plot constructors retain independent null/malformed fallback without Resources reload. Disposal continues owning generated views while borrowing atlas pixels.

Focused RED plan: two exact-role world/reader stage cases; independent optional atlas and old-constructor control; semantic/foreign-part authoritative refusal across all four routes; two real moist-growth/blocked-release/finite-yield/dirty-retirement cases; current owner/fog/cover/graph case; adjacent reserve/tray locality and borrowed-view lifetime control. Existing NorthPlot/PlotProduct/Global and ConnectedCropTime fixtures supply wider regressions rather than duplicating them. Tests may use injected valid atlas pixels for deterministic seams; actual appearance remains a separate root Play gate.

After GREEN, use ordinary input from166 to expose both current crops, read them, water with the earned grimoire, observe real advancement when safely attainable, and compare exact current owner/state/body. Verify released produce replaces rather than overlays the retired crop. Preserve a real save/restart. If combat or elapsed-time constraints prevent a full live lifecycle, report exactly which native and actual gates passed; do not override mechanics or invent a complete result.

## Approved reuse and immutable inputs

Existing source workflow: `Tools/Live16x24/build_opening_states.py`, editable `ArtSource/Live16x24/states/opening-states.blend`; `Tools/Live16x24/build_north_plot.py`, editable `ArtSource/Live16x24/north-plot/north-plot.blend` and `source-cells.json`. The north-plot README and saved concept/parts prompts document actual baseline → imagegen → native authored geometry. This slice reuses the unchanged accepted outputs; rebuilding or a new imagegen call would not resolve the routing gap.

SHA256 pins at preflight:

| File | SHA256 |
| --- | --- |
| Resources/opening-states.png | `9f4697147f14166ca6615672e12b2da0cbfa4a8ebeb70c7072151fca72f4983d` |
| Resources/opening-states.png.meta | `6cf5741351176e575a26f52805e7c5d66469387c124bf634192281dea5c15e56` |
| Resources/north-plot.png | `e14359280ce3d97fa645c3bbce0a5a316e0fd212a7025da363f66b01a42df641` |
| Resources/north-plot.png.meta | `3cfd750c49afbf8af98b72fe775693075420d075cb09980d1383932184f64d54` |
| states/opening-states.blend | `875e76fb02cc164983d1cc963732de155d91fa28f404d17485a217c6116f6f09` |
| north-plot/north-plot.blend | `2f27409d08ef41bbd401ec3f7a8343f50ecbe8f3cd4f1e1871fa0f71abbe1e9a` |
| north-plot/source-cells.json | `0fd7438c74e607879fe53f1f9b420003471ca8d467a6cb48c5edf23c8cc41b69` |
| build_opening_states.py | `3aaa9c5578f7bacc7fdcfc338644a68c7a783038db554204c89eda8e35ab43c6` |
| build_north_plot.py | `ed3144a94b95a4211d13e803b0d2667b84b7944cb2c0d1c1da4dc874cc261bb3` |

Resource paths above are under `Assets/Resources/Sprites/Live16x24`; source paths are under `ArtSource/Live16x24`; tools under `Tools/Live16x24`.

## Self-review and acceptance boundaries

🧪 Ordinary after/growth/restart and independent actual-image review are pending. ⚪ This is coverage, not an accepted fuller mire composition, all-crop art, biome polish or animation. No build/device/FPS claim follows. Existing approved art, saves,49foreign paths, current packages and Buckler work stay unchanged. Native growth assertions prove simulation/painter/reader behavior; actual screenshots prove only the visible state captured. Root will record publication and package source separately.

The focused fixture is now saved/frozen: eight cases in `CavesOfOoo.Tests.FlatPixelPortableCropTests`. It uses existing APIs only, so intended RED is nonlocal glyph/semantic refusal assertions, not a missing production API. The copied real painter helper restores its borrowed catalog, prior bound render zone/callbacks and performance counters; the suite also isolates work-audio publication used by crop release. A pre-RED source review corrected one test premise: replacing the managed graph blocks the renderer's portrait, while `ExamineDocumentBuilder.IsCurrentSubject` independently checks physical captured ownership. The graph counter therefore checks the actual null portrait, without falsely asserting that a physically intact old graph already lost its document authority. Native execution remains root's gate.

Independent source/API review by `/root/portable_equipment_final_review` found no concrete blocker: the stamped dry interval, sufficient finite moisture, missing-factory stage1 hold, next-unit retry/release and removed-owner rejection agree with CropTime/CropYieldService. This is source review, not execution.

### Actual baseline before native implementation

Root ordinary Continue166 and two left movements exposed both exact crops. Direct Examine `portable-crops-before-06-direct-examine.png` shows gladroot and `portable-crops-before-08-emberwheat-examine.png` shows emberwheat; prior field-note reader04/05 deliberately differs from exact object Examine and is not a portrait failure. A skills trial is excluded. An accidental ordinary En Garde activation during UI navigation is retained honestly. `portable-crops-before-current-state.json` and `Fixtures/portable-crops-exposed169` preserve player39,10 HP18 turn2540, same crop IDs25521/25522 at42,10/41,10, both stage0/progress0/moisture0. No injected gameplay or teleported baseline was used. Root finished the isolated Play session before importing RED.

Rain is the earned self-centered ability (radius3,40 moisture units), not a weather override. CropPart's real `^w` wet-soil background and existing ground/material paths must survive foreground admission; no new wet frame or crop-clock mutation belongs in rendering. Normal Rain from the current39,10 position can reach the two plants at distances2/3, subject to the ordinary ability/action validations.

Owned files for this tests-first phase: this document, new `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelPortableCropTests.cs` and fresh `.meta`. Proposed subsequent runtime files are OpeningArt and Greybox only, subject to root RED approval.

## Native RED and minimal frozen implementation

Root confirmed native job`fa9f4b453c97420cb2190fcd9f5498ab`: all8 cases failed as intended,7 missing-native assertions and1 semantic refusal assertion (expectedFalse/actualTrue), with no compile error. Canonical `Docs/Verification/Live16x24/portable-crops-native-red.json` retains uncapped failure identities; its result is null, so no XML/final-result payload is claimed. Root authorized implementation only after this result and actual169 baseline.

`FlatPixelOpeningArtCatalog` now exposes an internal exact-two-role selector over the existing cached state/plot views. Its existing public selector delegates those identities through the same guards, so local calls cannot bypass a refusal. Required parts are uniquely owned; optional status/footprint must be valid when present. The existing TryResolvePlot supplies automatic-yield/count/stage semantics unchanged. No new atlas, constructor, cache, timer, growth call or borrowed texture ownership is introduced.

`FlatPixelGreyboxCatalog` now uses that selector in its four existing legacy/modern world/subject seams. A known-crop refusal remains a glyph fallback, including in the opening. Modern world selection retains existing graph/top-owner/disclosure authority and remembered rejection. Existing ground/background logic is unchanged; crop moisture/`^w` still controls wet soil through its prior paths. Reserve crops, tray/cords, harvested produce and all other roles remain on existing policies.

The eight-case test file remains byte-for-byte frozen at`b78bb25585136fbdfb1470f549fe7fe1702efb99bce94fbf3e7994cfa760a169`. No extra post-RED fixture churn was added: the existing NorthPlot moisture/timing/compost controls and new real growth/retirement cases cover the adjacent semantics. Source self-review found no new gameplay writes, frame allocations per resolve, global subscription or resource reload. Independent final runtime review and native execution remain pending.

Agreed compact native group: `CavesOfOoo.Tests.FlatPixelPortableCropTests`, `FlatPixelNorthPlotTests`, `FlatPixelNorthPlotGroundTests`, `ConnectedCropTimeTests`, `FlatPixelGlobalPresentationTests`, `FlatPixelPortableSeedTests`, and `FlatPixelBucklerTests` (each under `CavesOfOoo.Tests`). NorthPlot covers adjacent frames and both finite yield semantics; NorthPlotGround guards ground/cord separation; Seed/Buckler guard the nearby shared routes. The earlier proposed additional OpeningStates/PlotProduct fixtures are unnecessary absent a new failure because the new early branch admits only these two exact planted identities. Count distinct cases once; do not sum earlier runs.

Frozen runtime SHA256: OpeningArt`63f84ab0a795502460c8f3486c60d9b67346f2d3e0663a23fac12f671fc1d83d`; Greybox`362eee1535b53d84198e59d2881bb8651c466bd4e8e88719033f862107cc2e12`. These source hashes are not a publication or package claim. Root may append actual acceptance and publication later.

## Final native GREEN and frozen source review

Root's job`b5ae86a2476544d888c500a0802236bc` completed at2026-10-28T13:28:54.971UTC (started13:28:21.348UTC). Canonical `Docs/Verification/Live16x24/portable-crops-native-green.json` contains240 distinct full-name result rows, all Passed, zero failed/skipped, and reported suite duration29.028523seconds. The actual final group included OpeningStates in addition to the agreed compact seven classes:

| Fixture | Passed |
| --- | ---: |
| FlatPixelPortableCropTests | 8 |
| FlatPixelNorthPlotTests | 54 |
| FlatPixelNorthPlotGroundTests | 50 |
| ConnectedCropTimeTests | 13 |
| FlatPixelGlobalPresentationTests | 52 |
| FlatPixelPortableSeedTests | 5 |
| FlatPixelBucklerTests | 8 |
| FlatPixelOpeningStatesTests | 50 |
| Total distinct cases | 240 |

This count includes the new8; earlier RED/regression totals are not added. Final file hashes remain exactly those above, including the unchanged test SHA. Independent runtime review by `/root/portable_equipment_final_review` verified those hashes and found no P1/P2: cached-frame/part/state guards, four authoritative refusal routes and explicit modern memory exclusion are coherent. It found no soil, cue, timer, constructor or resource-lifetime change. Its source review is distinct from root's native execution and forthcoming actual Play results.

## Wet-soil and normal Rain source trace

`CropPart.Water` settles prior elapsed growth first, then sets moisture to the greater of its current amount and the supplied40. It writes `RenderPart.BackgroundColor = "^w"` and marks the real cell dirty. `OnDriedOut` clears that background when moisture expires. The new body selector never calls either method or reconciles the clock.

Outside the opening, `FlatPixelFallbackCatalog.TryResolve` independently collects disclosed owner backgrounds, including a real lower crop under another foreground, parses/darkens `^w`, and retains the highest applicable background layer. `FlatPixelSoddenTravelCatalog.TryResolveGround` refuses a disclosed owned CropPart through `CompetingGround`, so its opaque grass cannot cover this wet soil. Greybox begins with the fallback ground/color, and the new crop branch changes only foreground. `ZoneRenderer.FlatPixelPaint` paints that ground separately (with existing lighting outside authored local ground). Inside the opening, existing `GroundOf` distinguishes wet/dry crop substrate, including under a covering actor, and the existing ground catalog retains its prior policy. This is a source trace, not a claim that an after screenshot has already been inspected.

`Hydromancy_ConjureRain` is an earned self-centered skill: Chebyshev radius3, cooldown5, max/top-up40 authored moisture units. It is not a weather setter, water-pool creator or targeted crop command. Normal `InputHandler` number-key activation (current earned slot4), clicked hotbar or Enter on the selected slot immediately resolves SelfCentered after usability/source checks; there is no aiming cursor. Even a successful cast with no crops spends its action and reports that no crops were found. From baseline39,10, both crops at41/42,10 are within range.

Forty authored units correspond to400 elapsed world ticks, not40 arbitrary player inputs. With uninterrupted moisture, fresh gladroot needs20+20 units and fresh emberwheat35+35; emberwheat therefore requires another useful top-up to complete. Recasting too soon does not add40 onto existing moisture. Runtime source/blueprint values, not the tooltip shorthand “40 ticks,” determine this acceptance route. Actual commands, elapsed ticks, water/state outcomes and any combat interruptions belong in the forthcoming root Play receipt; none are inferred here.
