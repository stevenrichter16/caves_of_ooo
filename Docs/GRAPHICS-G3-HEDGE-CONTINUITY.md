# G3 hedge continuity

Status, 2026-10-26: **bounded native integration and ordinary gameplay accepted**. The kitchen hive/court patch is pushed as `87ebff4b1`. Connected hedge art now follows actual disclosed owners in the existing six admitted graphs. Native final GREEN is 338/338; ordinary destruction, gap traversal, save/load and border return are recorded below. Current packages still predate this slice; rebuilding them is next.

## Named scene defect

The actual [kitchen after-court capture](Verification/Live16x24/kitchen-orchard-after-court.png) shows the improved service court and native hives, but the long hedge runs still repeat the same tall rounded body and leaf highlights in every cell. They read as scalloped bars or a row of identical shrubs. Corners do not describe a woven boundary turning through the environment. This repeated outline is now more conspicuous beside the quieter court.

This is a continuity and repetition defect, not a request to rearrange the kitchen. Keep every hedge, gap, tree, service, floor and approach exactly where the simulation already places them. Refine end/corner/interior frames so the existing hedge reads as one laid hawthorn-and-bramble boundary, while real cut-throughs remain visibly open.

Current gameplay evidence: seed `200893250`, `Overworld.12.11.0`, player `(23,9)`, tick `4290`, HP `30`, 50 drams, flat presentation, captured `2026-10-26T07:24:10Z`. These facts come from the matching JSON receipt. The screenshot is the baseline for the next concept; it is not evidence of connected hedge art. The prior native source contact is [north-vegetation-contact.png](../ArtSource/Live16x24/north-vegetation/north-vegetation-contact.png), explicitly a source preview rather than gameplay.

## Verified source and correction table

| Premise | Audited source / consequence |
| --- | --- |
| Hedges are walls for visibility | `Objects.json` defines `Hedge` as a `PhysicalObject`, glyph `#`, RenderLayer 1, `Physics.Solid=true`, `Takeable=false`, and `Vegetation` tag. It has neither `Wall` nor `Terrain` tag. It blocks movement but must not become a sight wall. Existing `FlatPixelNorthVegetationTests.TreeAndHedgeArtDoesNotTurnMovementBarriersIntoPlayerSightWalls` verifies the distinction. |
| A neighboring bush can complete a hedge | No. The exact blueprint is `Hedge`; Tree, Bush, BerryBush, DryBrush, decorative greenery and walls are different owners. Use exact current selected owners, not visual likeness, material, glyph or ID equality. |
| A hedge needs new damage stages | It has owned `DestructiblePart`, HP/MaxHP 10, no configured wreckage. `DestructionSystem.Destroy` marks Gone, removes the owner and dirties its cell. Show the same live identity while intact; existing fire/damage feedback stays separate. After removal, neighbors cap and ground/gap becomes visible. No new damage thresholds, debris, growth or fire frames are needed. |
| Existing hedge source is connected | `FlatPixelOpeningArtCatalog.VegetationFrameOf` always returns north-vegetation frame 1 for an ordinary Hedge. The approved source has a rounded silhouette and bright leaf groups repeated unchanged in horizontal, vertical and corner contacts. Preserve that image and source; add a new optional connected derivative. |
| Existing masonry admission can be widened | `FlatPixelBoundaryCatalog` is deliberately restricted to exact glade `Wall` owners with three approved VisualIDs. Widening it would conflate material semantics and memory. Add a narrow hedge companion inside the already-existing six-zone `FlatPixelGreyboxCatalog.IsAdmitted` scope. Do not expand regional admission or modify the masonry adapter. |
| Hedges should use masonry memory | No. `FlatPixelFallbackCatalog.CanRemember` requires Terrain/Wall and rejects live families; ordinary Hedge does not qualify. A hidden hedge stays absent under current policy. Do not add tags or create remembered hedge silhouettes just for connectivity. |
| Rendering may inspect a hidden neighbor to finish a line | No. Follow the boundary adapter's disclosed-neighbor rule: first test neighbor cell Explored and IsVisible, then run the strict current-owner selector. A remembered, unexplored, covered, stale or foreign neighbor contributes no connection. |
| Existing static fallback rejects every invalid hedge | No. Its ordinary vegetation check is narrower: it does not establish every explicit variant/custody/destruction guard proposed here. A semantically invalid known Hedge must not bypass rejection through that older native image. Missing optional pixels for a valid Hedge should retain the old image; invalid owner/state should use its canonical fallback. |
| Adjacency needs a new per-frame world scan | `EnvironmentSpriteRenderer.PostRender` already expands dirty cells to their eight-neighborhood and re-resolves them. Use that bounded path; verify removal, cover/uncover and FOV changes through the submitted renderer. No new global watcher, per-owner GameObject or generation rewrite. |
| Current layout can be regenerated to improve the shot | `SpreadCompositionBuilder` creates hedge owners during cold generation and may capture exact provenance for later gameplay. Rendering must not regenerate, retag, move or replace them. Saved/current owners are the integration target. |

This is original Caves of Ooo artwork using the approved 16×24 language. No direct Caves of Qud source or asset parity is claimed.

## Smallest asset and implementation

One optional native resource, proposed `Sprites/Live16x24/hedge-continuity`: **256×24**, sixteen 16×24 frames, one material, Point/Clamp, no mipmaps, binary alpha, PPU 16, pivot `(8,12)`, cached Tile/Sprite wrappers, no tile collider. Keep the original `north-vegetation` atlas, `.blend`, source cells, manifests and models unchanged. Mask 0 and the isolated examine portrait should preserve the approved hedge's pixels, sourced from the editable native cells; connected derivatives may change silhouette and leaf rhythm only to solve the named continuity defect.

Use the existing cardinal mask convention, **N=1, E=2, S=4, W=8**, with zone north `(x,y-1)`. Include all sixteen masks so real T and cross intersections do not require an arbitrary fallback. Ordinary run/corner/end shapes are the visual priority. Do not rotate a square image into a rectangular 16×24 cell.

Art direction for the concept and extraction:

- Preserve the current olive leaf/wood palette and the quiet green background. No halo, new cast shadow, luminous flowers, bright outline or copied concept pixels.
- Connected runs should have a continuous broad leaf mass and quieter internal value groups; remove the repeated rounded center bulge and repeated vertical trunk/post impression. Ends are deliberate caps, and corners describe a real turn.
- Agree on a consistent connection cross-section and edge colors on each cardinal join. Shape variation belongs inside that boundary; do not introduce artificial walkable-looking cuts along a continuous hedge.
- All pixels remain inside the owner's cell. An absent neighbor never receives painted foliage. Existing one-cell gaps must remain plainly open in horizontal and vertical contacts.
- Preserve isolated mask 0 as the approved identity. New connected frames are derivatives, and the old resource remains the explicit fallback if the optional new atlas is unavailable.

Proposed runtime shape is a small `FlatPixelHedgeCatalog`, borrowing `FlatPixelFallbackCatalog` for current-owner selection, modeled on the existing boundary adapter without changing it. Add one optional field/helper/disposal entry in `FlatPixelGreyboxCatalog`, one live-world resolution branch, and a guarded isolated exact-subject branch. Do not add another positional texture argument to the already large opening catalog constructor.

Recognition (`BlueprintName == "Hedge"`) must be weaker than eligibility. Eligibility requires owned visible Render, canonical glyph `#`, no explicit VisualID/VisualVariant/GlyphVariants, Vegetation identity without actor/item disguise, owned nonportable solid Physics with no custody, valid single-cell footprint, positive owned stack if present, and owned Destructible with HP > 0 and `Gone == false`. Preserve current HP/MaxHP, hardness, material and thermal data; do not pin them to default numbers or require new tags. Isolated portraits are semantic images only; the existing examine document remains the authorization boundary.

The helper must expose a narrowly scoped fallback decision: valid known Hedge + missing/malformed optional texture may use the existing approved frame; invalid known Hedge may not escape to old native art. This decision must not suppress unrelated vegetation, masonry, actors, services or items. Outside the existing six admitted graphs, retain existing presentation behavior. Injected old constructors must not silently load the optional group.

## Pure selection and state rules

| Situation | Required result |
| --- | --- |
| Visible canonical Hedge selected at center | Select its native frame from at most four disclosed cardinal neighbors. Preserve world owner/collision exactly. |
| Visible canonical Hedge selected at neighbor | Contributes its one direction bit. No diagonal or distant connection. |
| Adjacent Tree/Bush/Wall or explicit custom Hedge art | No connector bit. Preserve that owner's own art. |
| Higher or last-equal-layer object covers neighbor | No connector bit: do not see through the currently selected foreground. Revealing the Hedge again restores the connection. |
| Neighbor hidden/unexplored | No selection/state lookup beyond disclosure checks; center caps conservatively. No leak through HP, thermal state or cached adjacency. |
| Center hidden/unexplored | Existing absence/fog rules, not a new remembered hedge. |
| Hedge destroyed/removed/moved | Existing destruction/movement dirty path re-resolves adjacent survivors; actual gap stays open. No historical connected mask is retained. |
| Replaced zone/cell graph or stale owner list | Reject stale geometry using exact object references/current selector. IDs alone are insufficient. |
| Valid source, atlas absent or malformed | Original approved static Hedge image, without silently loading injected resources. |
| Known Hedge with invalid live semantics | Canonical fallback; no bypass through older native vegetation. |
| Exact-subject examine | Isolated approved image, no neighbor lookup or discovery. Reader authority unchanged. |

The same current-owner policy can cover the six admitted graphs when hedges actually occur there; it does not create any Hedge in a graph that has none. At most center plus four cardinal selections per query, no allocations after cache setup, and no change to turn timing or gameplay RNG.

## Pipeline and meaningful RED design

1. **Freeze prior work first.** Root commits/reconciles the separate hive/court patch. This document adds no tests or runtime while that acceptance is in progress.
2. **Actual input to concept.** Use the recorded kitchen after-court frame, plus a close actual L-corner/straight-run view from the same earned save. Record exact seed, cells, route and tick. Reuse this real layout; do not make a prettier detached hedge arrangement.
3. **Imagegen and extraction.** Reinterpret only the hedge outline/leaf rhythm against unchanged trees, cook, service court and hives. Save prompts, output, critique and extracted end/corner/straight/junction parts. Reject displaced owners, closed gaps, excessive density and light effects.
4. **Native source.** Produce editable exact-grid derivatives and a sixteen-frame atlas, keeping mask 0 pixel-identical to approved source. Review native and integer contact layouts: horizontal/vertical runs, four corners, all ends, T/cross, isolated cells and a real one-cell gap. Verify palette, binary alpha, native frame size, source/resource equivalence and matching joins.
5. **RED before integration.** Write a focused `FlatPixelHedgeContinuityTests` fixture. At least one test must use default `TryResolvePresentation` with real Hedge owners and assert the new imported run/corner frame, so tests cannot pass for an adapter that is never used. Keep a small grouped suite rather than duplicating every existing renderer test.
6. **Minimal production, native GREEN, independent review.** Add only the narrow catalog and routing/guard; use the existing dirty expansion. Run focused and neighboring tests once, fixing material issues. No art acceptance from test counts alone.
7. **Ordinary comparison.** Root replays actual approaches, inspect/bump/break and return/save-load through normal controls. Compare the same scene and close corner/run images. If the new silhouette still reads as stamps, refine source frames before widening scope.

Proposed focused native test groups:

| Invariant | RED signal and counter-check |
| --- | --- |
| Production path and sixteen masks | Real owner fixtures for the sixteen literal N/E/S/W combinations, expected native rects and stable Tile reuse. Remove a required neighbor or add only a diagonal; no wrong arm. Old static frame fails connected rect/resource checks. |
| Six-zone admission | One small parameterized current-graph family for all admitted addresses; an outside region and changed biome/POI/current cache remain excluded. Reuse existing scope tests rather than creating a new region policy. |
| Disclosure/top owner | Visible east connector versus hidden, unexplored, higher covering object and last-equal-layer cover. Restore disclosure/uncover and recover exact connection. Hidden center stays absent. |
| Ownership and eligibility | A compact mutation table exercises foreign/missing Render/Physics/Destructible, explicit art variants, Gone/zero HP/non-solid/custody, wrong blueprint and multi-cell owner. Positive before mutation; invalid center cannot bypass to old atlas and invalid neighbor cannot add a bit. |
| Current reference authority | Moved owner's stale old-cell entry, duplicate-ID impostor and replaced graph do not contribute adjacency. Use existing strict selector behavior, not ID dictionaries. |
| Real removal and redraw | Through real `DestructionSystem` plus submitted renderer/dirty path, remove one interior hedge and observe both surviving neighbor caps and cleared former cell. Counter: distant unrelated cells remain unchanged and a quiet follow-up frame causes no full redraw. This is necessary beyond direct adapter queries. |
| Portrait/fallback | Examine stays isolated and does not read hidden adjacency. Missing new art keeps original native hedge; invalid known owner uses glyph; old injected constructor does not import optional pixels. |
| No gameplay/source mutation | Repeated rendering preserves owner IDs/references/positions, parts, collision and FOV behavior; original source hashes unchanged. Shared atlas/cache disposed correctly while borrowed pixels/selector survive. |

Suggested neighboring fixtures: `FlatPixelBoundaryCatalogTests`, `FlatPixelNorthVegetationTests`, `FlatPixelFallbackCatalogTests`, `FlatPixelGreyboxCatalogTests`, `FlatPixelGlobalPresentationTests` and the existing environment incremental-render tests. Confirm exact fixture selection from current test source when implementation starts; no new native receipt exists yet.

## Ordinary route and acceptance bounds

Start from the earned kitchen save rather than generating a different world. Normal Continue returns to `(23,9)` for the saved baseline. First capture the same court/long-run overview. Approach the closest visible long hedge south of the court, stopping adjacent to the actual owner reported by Focus; then take the open lane to the visible northwestern L-corner. Root must record exact hedge coordinates from current owner receipts before claiming a deterministic route—the screenshot alone is insufficient path/collision evidence. A short detour around the same end proves the gap is traversable and bumping a surviving Hedge proves it still blocks. Use ordinary examine/break for one reachable hedge, capture the fresh gap, save and return to check that the destroyed owner remains absent and neighboring art reselects correctly.

Accept only this bounded improvement: continuous hedge runs, clear ends/corners and genuine gaps at the actual camera scale, with unchanged geometry, native tree/cook/hive art, selection and fog. This is not all regional vegetation, a whole-opening completion, a new package or physical Deck acceptance.

## Self-review and continuation

- 🟡 Hidden hedge memory is explicitly rejected; masonry policy cannot be copied wholesale.
- 🟡 Old native vegetation fallback must not undermine stricter live-state/explicit-art rejection. This needs a paired integration test, not only adapter tests.
- 🔵 The named change is connected outline and leaf rhythm. Keep approved isolated identity and all existing source assets recoverable.
- 🧪 Final native GREEN is338/338 with ordinary Play accepted below. Linux execution, physical Deck and complete regional coverage remain unverified.
- ⚪ Broader vegetation animation, new growth/harvest verbs, world population edits and orchard rearrangement remain outside scope.

Delivered files: this plan; new hedge tests/catalog and metadata; exact `FlatPixelGreyboxCatalog` routing; optional native atlas and metadata; editable source, builder, imagegen/extraction records; native/ordinary evidence and living records. Root owns Unity, final acceptance and commit.

Implementation log: 2026-10-26 — reviewed project pipeline, current kitchen after-court image/receipt, approved hedge source/contact, blueprint, static vegetation mapping, connected masonry policy, strict current-owner fallback, destruction and incremental redraw paths. Saved this bounded plan while the prior patch remains frozen.

2026-10-26 — after the prior patch reached main, saved the 30-case RED fixture. Six cases exercise the shipped default world/reader path in the existing admitted graphs; a grouped loop covers all sixteen masks; grouped owner/disclosure and old-art bypass counters cover this adapter's contract; two submitted-renderer tests require real destruction/FOV changes to repaint neighboring connections without a full redraw. Reflection keeps the missing adapter an assertion failure. No production code preceded native RED.

### Production pass — actual baseline, concept and RED

After `87ebff4b1` was remotely verified, root normally Continued the original
earned kitchen checkpoint in isolated Play. `hedge-before-court` captures
23,9/tick4290. Left16 then Up5 reaches7,4/tick4500 for
`hedge-before-corner`; its actual current-owner receipt confirms the visible
corner6,3, vertical6,4–6, horizontal7–16,3, and southern18–33,15 run.
No owners were moved and no discovery or resources were granted. Normal
editor/save/view settings were restored before native tests.

Built-in imagegen reinterpreted that actual corner screenshot; prompt, concept
and extraction are in `ArtSource/Live16x24/concepts/hedge-continuity/`. Continuous
silhouette and turn are accepted as guidance; noisy fine leaves, soft shading,
changed output scale/UI and all generated pixels are rejected for runtime use.
Native first pass joins but looks tubular; the second source pass adds sparse
angular leaf clusters and edge variation while preserving exact join signatures.
The original isolated hedge remains pixel-identical.

Native RED at07:50:48UTC:30/30 expected failures,0skips. Six production-path
cases still resolve rect16 rather than rect160; remaining24 cannot find the
unimplemented adapter. Receipt: `hedge-continuity-red-summary.json` and XML.
Independent plan review found no blocking source mismatch; requested two useful
counter-checks: nonlethal actual damage retains connected identity, and semantic
rejection still applies if the optional texture is unavailable.


### First native integration review

First GREEN attempt:337/338 passed,0skips. The remaining test failed during
fixture setup: adding a two-cell eastward footprint to the center collided with
the already placed east hedge, correctly refused by Entity.AddPart and
Zone.RefreshFootprint. The fixture now extends both footprints south into free
cells; this still exercises real multi-cell ownership. Runtime is unchanged.
A final native run follows. Source pass2 independently reviewed and verified:
zero source/native/isolated/join/contact pixel mismatches,42 protected files
unchanged, exact RGBA/source-cell rebuild. Long runs still repeat interior leaf
motifs; extra variation is deferred unless actual scene review warrants it.


### Final native and ordinary acceptance — 08:17 UTC

Final native GREEN is **338/338, zero skips**, including31 hedge cases and eight
neighboring fixtures. The final receipt and XML are
`Verification/Live16x24/hedge-continuity-final-green-*`. Added review counters
exercise actual nonlethal damage and semantic rejection with a missing optional
atlas. The earlier337/338 result is retained transparently; its only correction
was the overlapping footprint fixture, not production behavior.

The narrow adapter borrows its selector/texture, caches16 tiles and sprites,
uses at most five selected-owner queries, and routes through the existing
neighbor dirty expansion. No generation, geometry, collision, FOV, turn timing,
owner identity or gameplay mutation changed. Exact-subject examination retains
the approved isolated image. Missing pixels keep valid old art; invalid owners
cannot escape to the older native fallback.

Root replayed the original earned kitchen fixture through ordinary Continue,
keyboard movement, context examination, bump attacks, F5/F6 and chunk borders.
Each named PNG has a matching read-only current-owner JSON receipt:

| Actual input and result | Capture |
| --- | --- |
| Continue at23,9/tick4290, then Left16/Up5 to7,4/tick4500; same court and L-corner baselines | `hedge-after-court`, `hedge-after-corner` |
| C/Left/X: actual Hedge reader and preserved isolated image; Structure10/10 | `hedge-examine` |
| Walk around southern end to5,5/tick4570; surviving hedge still blocks | `hedge-around-end` |
| Right bump: Hedge12057 at6,5 takes9 damage, remains1HP and mask5, player stays5,5/tick4580 | `hedge-first-strike` |
| Second Right bump removes12057; neighbor11976 at6,4 becomesmask1; lower12143 at6,6 becomesmask0; visible gap | `hedge-broken-gap` |
| Right2 traverses gap; F5/Right/F6 returns7,5/tick4610 with removal and caps retained | `hedge-gap-reloaded` |
| Left8 exits west into11.11.0; Right8 returns through the same gap to7,5/tick4770 | `hedge-western-exit`, `hedge-returned-gap` |

All files are under `Verification/Live16x24/`; `hedge-play-journal.json` records
the actual actions. After receipts include submitted renderer texture/rects,
using the checked-in read-only `hedge-observer.cs.txt`; before captures predate
that extra submitted-tile field. The source fixture was not modified. NPC
positions can differ between replays; this is not exact whole-world equivalence.
The western neighbor still uses truthful glyph fallback outside the native kit.

Independent source/runtime review found no blocking issue. Independent review
of the actual1280×800 before/after, reader, gap and reload images accepts the
continuous leafy silhouette, clear turn and real open gap. Approved isolated
hedges remain larger by design; long runs still repeat interior motifs. Source
verification found zero source/native/isolated/join/contact pixel mismatches,
42 protected source files unchanged and identical editable-cell/RGBA rebuild.

At the same court23,9/tick4290, the Editor sample changed38→39 draw calls/batches,
with64 setPass calls,8022 triangles and16064 vertices unchanged: one shared hedge
atlas batch. Allocation totals follow different Editor test histories and are
not attributable. This is no device frame-time or memory benchmark.

Can verify from native/source/runtime receipts: masks, ownership/disclosure,
actual damage/removal, dirty repaint, persistent gap, unchanged normal saves,
restored editor state and the sampled batch change. Visual acceptance comes
from actual captured play, not tests or concepts. Cannot verify here: physical
Deck feel/performance, Linux execution, complete six-service lifecycle or all
opening/regional art.

Normal776 save files and two unrelated PNGs are byte-identical. Root stopped
its Play, restored SampleScene, preferences/view, null save-root and no isolation.
28 incidental importer whitespace changes were backed up and restored exactly.
About428GiB free; storage checks remained read-only and no deletion occurred.

Next: freeze and remotely preserve this source, rebuild dated Mac/Linux current
packages, then replay ordinary Mac Continue/inventory/examine/save-return. Keep
previous outputs and report Linux/device limits. Continue scene-level journey
acceptance and a separate Nella threat audit after current delivery is verified.
