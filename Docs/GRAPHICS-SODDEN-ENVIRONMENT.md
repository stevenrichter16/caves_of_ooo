# Sodden firm ground and mire edges

Status: finalv6 bounded Editor material pass verified; full-area polish open. Root owns Unity, image generation, actual Play and publication.
This is the first environment-polish slice under the user's new all-biome
priority, not a declaration that the Sodden or other areas are complete.

## Purpose and scope

Actual Open Mire and Reed Maze views expose a regular repeated grass sprinkle
and large flat olive pool rectangles. Give existing firm fen ground a quiet,
clustered sedge/straw/moss material, and existing wet pools readable shallow
edges. Preserve the exact generated ground owners, routes, collisions, stock,
coatings and gameplay. New art must keep a shared dark base at every tile edge;
variation changes detail density, not large checkerboard blocks of value.

Use `Sprites/Live16x24/sodden-ground`: 576×24, 36 native 16×24 cells, centered
16PPU. Frames0–3 are firm ground. Frames4–19 are filled mire, frames20–35
are depleted **wet film**, each indexed by N1/E2/S4/W8 connections. Both stocks
connect as one wet material. Examine uses isolated frame4 or20, not a topology
map. Root owns art; existing travel/reeds/Sill/cellar resources stay unchanged.
The seventh constructor argument is the optional new atlas after the borrowed
selector; the old six-input constructor delegates null. Missing/malformed new
art retains old accepted travel/grass art without implicit resource loading.

## Verification sweep and corrections

| Source | Verified contract / correction |
|---|---|
| `FlatPixelSoddenTravelCatalog.cs` | Existing canonical district/wilderness addresses and live graph/map/POI/lair guards remain authoritative. The current grass comes from Sill frames20–23, not a Sodden-specific atlas. |
| Same, `ForegroundFrame` | Mire is an exact non-takeable, non-solid Peat terrain owner with owned intact/neutral parts, bog-mire LiquidPool and TileStateSource/BurnOffGas. Volume0 remains a permanent wet film. Do not portray it as safe dry land. |
| Same, `IsSoleLiveMire` | A single live bog-mire coating is required. Mixed/expired/ice coatings refuse. Independent energy/gas markers remain visible and do not change pool stock. |
| Same, `TryResolveGround` | Independent tile state or a real competing ground owner outranks dry Grass. StoneFloor uses its existing four-frame resource and coordinate selection unchanged. |
| `EnvironmentSpriteRenderer.cs:1108` | Correction: ZoneRenderer's dirty set itself does not expand, but the following native PostRender expands it to the existing eight-neighborhood and re-resolves bodies/background. No global dirty-propagation change is planned. Actual submitted incremental/full equivalence will test this. |
| Neighbor authority | A visible current pool under an actor must keep the shore joined. Scan only exact current disclosed pool owners with unique owned parts and current footprint, reject duplicate/foreign/custodied/hidden pool owners. Do not require it to be the top selected actor. The center still requires selected world-owner authority. |
| Existing travel tests | Default-resource assertions for old grass/mire frames need precise post-RED migration. Explicit six-input compatibility keeps those old assertions meaningful. Board, wall, reeds, graph, hazard and mechanic tests remain intact. |

## Test-first implementation plan

Add `FlatPixelSoddenEnvironmentTests` using synthetic36-frame input plus real
factory owners/current graphs and actual EnvironmentSpriteRenderer submission.
Cover default new-resource routing, all16 full/depleted edge masks, isolated
portraits, occupied/hidden/foreign/custodied/mixed neighbors, current graph
refusal, ground priority, optional atlas compatibility, cached lifetime and
stable clustered floor variation. Exercise stationary neighbor changes through
real dirty rendering, then compare a full repaint, with no full-pass increase
until explicitly requested. Preserve real coating and stock state throughout.
Run native RED before modifying the existing travel adapter. Implementation is
confined to that adapter unless a reproduced submitted-render failure requires
more. No world-generation, mechanic, general rendering, RNG or timing changes.

After GREEN, run affected Sodden travel/global/material tests and ordinary
healthy157 routes at normal zoom. Review Open Mire and Reed Maze, floor/pool
transitions, disclosure, stocked/film appearance where legitimately observable,
reader identity and return/restart. Compare exact source-art pins. Native tests
prove selection and geometry, not actual aesthetic improvement, GPU quality,
physical Deck performance or all-Sodden coverage. Root records those gates.

## Current evidence

Root captured ordinary healthy157 Open Mire at40,10
and Reed Maze at40,12 without changing simulated owners; actual image paths and
art critique will be appended by root. Other biomes, walls and structures remain
in the environment-polish queue. No package or release-quality claim.

## Native RED and minimal implementation

Root ran job `99d8fde54a214340801eb4597faa82a9`: **12 completed,11 intended
failures,1 passing old-six-input compatibility control**. The complete uncapped
progress is retained in `Docs/Verification/Live16x24/sodden-environment-native-red.json`;
its result field is null, so this is not an XML-result claim. A prior job
initialization failure is retained separately and is not counted as RED.

Only `FlatPixelSoddenTravelCatalog.cs` changes production. Default construction
loads the independent36-frame atlas; the old constructor explicitly passes null.
New Grass uses quiet seven-by-five-cell detail-density patches hashed from
world seed and world-relative cell coordinates. StoneFloor's prior per-cell
hash is unchanged. Full/film pool bodies use four disclosed physical-neighbor
bits, allowing a valid pool beneath an actor. Readers use isolated frames4/20.
Optional new art failure keeps accepted old material art; owned views are
cached/disposed and injected textures/selectors remain borrowed.

The new12-case fixture stayed byte-identical after RED. Existing
`FlatPixelSoddenTravelTests` default Grass/Mire expectations now name the new
material resource/ranges, with isolated mire portraits. Its old sparse-variation
ratio test explicitly injects the old six-input packet and retains its old
16/17/22–27 frame assertions. Existing old-group null/malformed controls remain
old-group controls. Refusal checks forbid both source atlases. No tests, art or
runtime were broadened to another biome or mechanic.

Frozen hashes before GREEN:

- Runtime: `55a70b008e58850364a747319ed5a731c4df3aa79158e3e77dd2176b8c35efa9`.
- New fixture: `632b7e92ecb024d778b82033cf137a4122eeefd0697d016d756c4394c6b9169b`.
- Migrated travel fixture: `503c35b4a136533aa25610b656b3e9c8778e2b4fb19be93a3efe577d02ba8450`.

Self-review: no known P1/P2 source defect. Existing current graph/map/POI/lair,
owned parts, custody, coatings and active color-refusal checks remain in use.
Only four bounded neighboring cells are scanned for a presented pool; there is
no whole-world scan, RNG/state write, new per-frame texture or GameObject. The
existing submitted neighborhood expansion handles local changes. Synthetic
frame tests prove address/geometry and state truth, not pixel aesthetics; native
GREEN and ordinary whole-scene visual review are still pending. No allocation,
frame-rate, physical Deck or release-quality result is asserted.

Recommended affected native classes: `FlatPixelSoddenEnvironmentTests`,
`FlatPixelSoddenTravelTests`, `FlatPixelGlobalPresentationTests`,
`FlatPixelGrovelandsMaterialsTests`, `FlatPixelSillEnvelopeTests` (all under
`CavesOfOoo.Tests`). The last two guard unchanged shared-resource neighbors.

### First regression and two fixture corrections

Root's first combined native run `e5f7872bc7c244d49461902f23f18520`
completed352 cases:350 passed,2 failed. The retained receipt is
`sodden-environment-native-green-first.json`. Both failures exposed test
premises; production remains exactly the frozen hash above.

1. The new submitted-neighbor test incorrectly expected the last removed pool
   to leave a bog-mire coating. `Zone.RemoveEntity` calls `UnprojectPool`
   (`Zone.cs:472,813`), and the last matching pool calls
   `ZoneTileState.RemoveUnprojectedCoating` (`ZoneTileState.cs:212`). Corrected
   the final assertion to absence after removal, and added the positive
   presence assertion while the depleted owner is still placed. This preserves
   the distinction between depletion and removal without changing mechanics.
2. The existing hidden-cultivation test intentionally compares Sodden16.7 and
   Sill10.10. Its blanket new-resource expectation was too broad. Its two
   explicit case inputs now specify Sodden fen versus unchanged Sill grass;
   the same disclosure/owner checks continue unchanged.

Final corrected fixture hashes awaiting rerun:

- New fixture: `1583437b0c607afaf1b296594ddab67adfb0da10f08ee9c7db6dedadc22bf91c`.
- Travel fixture: `e719f8106e563aec42fb18349eff1503dd89a0a34aa74651db80013fede277b0`.

Independent source review by the existing portable-equipment reviewer found
no concrete fixture/API blocker or runtime P1/P2. This review checked owned
parts/disclosure, under-actor neighbors, wet-film joins, isolated portraits,
old-constructor fallback, cached-view lifetime and submitted repaint. It did
not execute tests or establish actual visual quality. Native rerun and actual
whole-scene review remain pending.

### Whole-view refinement: sparse gaps within detailed ground patches

The corrected five-class run succeeded with352 completed and no recorded
failures (job `057608d1f9ec42ad96dbfb1c5b5c82ee`, receipt
`sodden-environment-native-green.json`). The tool returned no result object;
do not infer full per-case rows or skip counts from that progress receipt.

Actual `sodden-environment-after-09-v3-matched.png` and the v3 source pattern
still show a broad flat olive field, weak shore material and repeated identical
detail runs. Root's fourth art pass addresses material/edge contrast. A separate
minimal coordinate refinement breaks stamp runs: all quiet coarse patches stay
quiet; approximately one eighth of cells inside nonquiet patches select quiet
frame0 using a stable world-seed/global-cell hash. Coarse material grouping,
existing stone selection, owner/state guards and cache lifetime stay unchanged.
This cannot by itself establish a polished scene.

The existing firm-ground test gained a single meaningful assertion: sampled
full world-aligned7×5 nonquiet patches must not all remain identical35-cell
rectangles. Existing coherence (>0.65 adjacent matching), three material
variants, repeat-query caching, unchanged state/version and StoneFloor controls
remain. Root ran that changed test before production: job
`e600394255e24ef4b041deea5c9f2051`, one completed/one intended assertion failure,
expected >0 varied detail patches but got0. Receipt:
`sodden-environment-pattern-red.json`; no compilation failure counted.

After that RED, only `FenGroundVariantAt` changed. Final frozen runtime SHA:
`5e1229f62b09cdd26898c8d9c7377cb0d745f6866f82949c8257eca53533ddc3`.
Fixture SHA: `0223239ddf2b1dc2d1a8782b2ce2e3ec4d51cf9f62d47a73b2ec5757e86cf9a0`.
Travel fixture remains `e719f8106e563aec42fb18349eff1503dd89a0a34aa74651db80013fede277b0`.
Root's final focused12 plus travel regression, v4 actual review and persistence
are pending. No further mechanic or renderer changes were made.

## Final focused native evidence (v4)

Job `ff01de971b4d44099430e24ac0ed0f51` returns146 per-case rows:143Passed,3Inconclusive allocation controls,0Failed. All12 new environment cases pass. The allocation counter did not detect its positive control; no zero-allocation proof is claimed. Earlier corrected352-completed/no-failure progress evidence covers the wider global/Grovelands/Sill regression set before the final isolated grass variation. These are successive runs, not a combined final352-pass claim.

Final runtime SHA256 `5e1229f62b09cdd26898c8d9c7377cb0d745f6866f82949c8257eca53533ddc3`; new fixture `0223239ddf2b1dc2d1a8782b2ce2e3ec4d51cf9f62d47a73b2ec5757e86cf9a0`. Independent review finds no P1/P2 runtime issue. Correction: the added test proves at least one complete detailed7x5 patch has variation; it does not guarantee every patch is nonuniform. Native source/test receipt is `Verification/Live16x24/sodden-environment-native-final.json`.

### Second whole-view refinement: mixed detail shapes, not quiet holes alone

Actual `sodden-environment-after-14-v5-matched.png` still exposes regular dabs
and detail lattices. Sparse holes did not sufficiently vary the remaining
identical detail frames. Root's v6 makes quiet frame0 entirely base material;
other source-frame changes remain root/art-author evidence. This prevents a
recognizable quiet-frame dab from repeating across every quiet patch.

Before runtime, the existing firm-ground test gained an actual returned-frame
assertion requiring >1 **nonzero** detail variant within a full7×5 detail
window, plus a35–80% quiet-share bound. Existing >0.65 neighbor coherence,
stable cache, state/version and StoneFloor controls remain. Root's native job
`ebc3d071a38a44c88cc060b94104ee45` confirmed exactly one intended failure:
expected >0 mixed-detail patches, actual0. Receipt:
`sodden-environment-mixing-red.json`, uncapped progress. No production preceded
this RED.

After RED, the existing fine hash now selects quiet0 for approximately one
third of cells within nonquiet coarse patches; the rest select independently
among details1–3. There is no dominant repeated detail stamp inside a patch.
Coarse quiet patches stay quiet, the same seed/global coordinates remain the
only inputs, and no mechanic, resource layout, owner guard or stone hash changes.

Source-only distribution check explains the density choice: the fixed fixture
region already contains440/640 coarse quiet cells (68.75%), and the whole18.8
grid1230/2000 (61.5%). Making half the rest quiet would imply about84%/81% quiet
respectively. The agreed one-third choice predicts about79%/74%; these are
source calculations, not native results or proof of visual quality. The native
coherence/quiet assertions and actual v6 screenshot are the acceptance gates.

Final runtime SHA `df26299afe0f1142fe195e3e089563d585911d2e14edb464c1bbff954de3d0a0`;
new fixture SHA `fba3f40d9b81c672ff642fe7f0fdbc194a95596a2386714c920234cc97d93c6a`;
travel fixture remains `e719f8106e563aec42fb18349eff1503dd89a0a34aa74651db80013fede277b0`.
Root's final native and ordinary v6 comparison remain pending. The runtime
change remains confined to the existing ground-selector return/comment.

## Final v6 test/source checkpoint

Job53b5f4864cf441adb6655ae5c4d05d36:146rows,143Passed/3Inconclusive allocation controls,0Failed. This final run follows mixingRED ebc3d071a38a44c88cc060b94104ee45, one intendedfailure. Runtime df26299afe0f1142fe195e3e089563d585911d2e14edb464c1bbff954de3d0a0 and fixture fba3f40d9b81c672ff642fe7f0fdbc194a95596a2386714c920234cc97d93c6a receive independent source review with0P1/P2. Do not sum the repeated143-pass runs.

Art v6 PNG8cdc575283d951a7f02d106c01906bcce59085b75107f3f8b7674cb323d7d938 changes only66frame0pixels fromv5; all35otherframes/13440pixels are identical. Quietframe0 is plain3D492E. Two independent exports/reopened Blender sources pass exact geometry/pixel checks;157frozenhandoff file hashes verified byroot. Earlier candidates are preserved. Final actual comparison remains a separate gate.

Final actual15/16 and freshContinue171/Examine18 are verified; independent visual review0P1/P2. See acceptance/finalreceipt for exact scope, intermediate failures, profile spikes and remaining differences. This commit publishes source and evidence; packages remain43a151ddb. Next environmental work is Stump, not morecrop/item packets.
