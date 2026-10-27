# Grovelands Fen water continuity — bounded preflight

2026-10-27. **Native GREEN 600/600 after genuine 12-case RED; implementation and tests frozen for the actual matched Fen comparison. Approved source art is unchanged.** The current Grovelands material pass has 495 native passes and ordinary four-formation/restart evidence. Those results do not accept the water change proposed here. Root owns Unity, RED/GREEN, actual input/captures, packaging and Git.

## Named defect and actual baseline

The actual [restart Fen capture](grovelands-after-15-restart-fen.png) shows bright repeated blue wave glyphs on dark strips amid the new quiet native forest ground. The braid exists and is readable as a route feature, but its rendering is visibly inconsistent with the surrounding materials. This is a substrate-continuity defect, not evidence that more scenery should be generated.

The [play summary](grovelands-play-summary.json) and [restart receipt](grovelands-restart-state.json) establish seed 65, `Overworld.0.8.0`, earned turn 4430, player 40,15, HP 38. The before/after restart preserves PaleSalt 42025 ×1, GoldCoin 49426 ×3, GroveRed 51793 ×1, seep 46948 stock 39, consumed cache with its row remaining, and consumed forage with Grass remaining. This preflight inspected the saved screenshot; it did not run another gameplay session or invent a water-crossing observation.

**Smallest agreed scope:** native ground for a sole **permanent, ownerless water coating** in current ordinary Grovelands. Exclude GroveSeep and other current visible physical liquid/source cells. Preserve their existing bodies and overlay contract. This source-owned boundary keeps the 91 existing Grovelands material cases intact and does not imply that finite spills, services or all water everywhere are covered.

## Source corrections before implementation

| Tempting assumption | Verified correction |
| --- | --- |
| Fen water must be a pool blueprint requiring a new sprite owner. | `GrovelandsFormationBuilder.cs:218–230` writes `water` directly with `ZoneTileState.Permanent` on open cells. It creates no water entity. Existing Grass/actors/items remain separate owners. |
| Water should be painted from the sine braid coordinates. | The generation plan only establishes placement (`GrovelandsCompositionPlan.cs:104–116`). Presentation must read current disclosed coating state, so removal, freeze, mixtures and saved changes remain truthful. Never recompute the braid as a backdrop. |
| A green native Grass tile is a sufficient water background. | `FlatPixelGrovelandsMaterialsCatalog.cs:87–102` deliberately declines any nonempty tile state. `FlatPixelFallbackCatalog.cs:56–95` then retains semantic fallback, including the actual selected owner and authored background. This saved view shows the resulting dark strips plus water marks. |
| The existing GroveSeep water predicate exactly matches water-overlay relinquishment. | Its `IsSoleLiveWater` ignores expired entries (`FlatPixelGrovelandsMaterialsCatalog.cs:256–265`), but `ZoneRenderer.cs:1394–1400` requires **exactly one list entry**, `water`, positive turns. New ground must use the stricter one-entry rule; live water plus an expired extra must not earn suppression. |
| The ordinary seep can join this fix automatically. | GroveSeep has a finite physical pool/service and a distinct full/empty wet body. Its current submitted tests intentionally keep the generic water wave (`FlatPixelGrovelandsMaterialsTests.cs:411–432`). Exclude it here; do not change those tests to enlarge scope. |
| A current zone ID alone authorizes the art. | Reuse `FlatPixelGrovelandsMaterialsCatalog.IsSupportedZone`, lines 49–60: exact cached graph, actual map dimensions, actual Grovelands biome, null current POI and no saved lair. Its candidate set comes from authored wilderness, not arbitrary matching IDs (`GrovelandsCompositionPlan.cs:38–48`). |
| The water atlas is a few generic blue cells. | `opening-ground` has 21 cells; **0–15 are all cardinal dry-edge masks**, not frames 4–11. Road 16–17 and soil 18–20 are out of scope. `FlatPixelOpeningGroundCatalog.cs:77–98,140–148` defines the exact bank semantics. |
| Suppressing the wave means clearing all marks on the cell. | The painter checks live veil mist, residues, oil, unrepresented coating, charge, heat, cold, then other cloud (`ZoneRenderer.cs:1310–1377`). Relinquish only the represented water branch; later hazard cues must still render. The nearby prose saying fire precedes water is not the actual branch order. |

All paths above are under `Assets/Scripts/` except the test under `Assets/Tests/EditMode/Presentation/Rendering/`. Rendering classes are under `Presentation/Rendering/`; generation files are under `Gameplay/World/Generation/` (`GrovelandsFormationBuilder` is in `Builders/`). These are source anchors for this preflight, not a new Qud parity claim.

## Exact material and state mapping

| Actual input | Proposed output and boundary |
| --- | --- |
| Current visible/explored ordinary Grovelands cell; exactly one non-null coating entry with `Id == "water"`, `Turns == ZoneTileState.Permanent`, empty `ProjectionOwnerId`; no current visible physical liquid/source owner | Borrow opaque `opening-ground` water mask 0–15. This represents water substrate only, not a drink/refill service, depth, collision, stock or creature. |
| N/E/S/W neighbor is current, visible and explored with **no coating records** | Add mask bit N1/E2/S4/W8 respectively. The muted bank is inset inside the wet cell. |
| Neighbor hidden, unexplored, invalid/out of bounds, wet, mixed, frozen or carrying expired-but-retained coating | No dry-bank bit. Hidden topology must not become a shoreline. Diagonal/distant cells do not affect the mask. |
| Exactly selected ordinary neutral Grass owner under represented water | Suppress only that existing Grass glyph after proving current placement and the existing exact Grass semantics. Keep the actual owner and collision result. A reskinned, cultivated, effect-bearing, duplicated/foreign-part or otherwise repurposed Grass must retain its foreground fallback. |
| Player, actor, portable item, plant, crop, remnant or unknown selected foreground above water | Keep that exact foreground and reader identity. Water is not a subject portrait and must not replace an actor/item/crop body. |
| Current visible physical `LiquidPoolPart`, `WellPart`, `TileStateSourcePart`, `GasPoolPart` or finite-film source | No new water-ground claim in this slice, including GroveSeep. Preserve the current material/source and water overlay. Ignore hidden, foreign or custodied entities as authority: an unobservable source must not leak through a presentation decision. |
| Finite water, ice, brine, bog-mire, oil, acid, duplicate water, mixed coatings, null/expired entries, missing water | No new water-ground claim. Existing semantic ground and overlay remain. This pass neither mutates nor consumes any state. |
| Permanent water plus heat/charge/cold/residue/cloud | Water substrate may remain if the exact coating/source gates pass. Existing priority selects the independent mark; do not erase these channels or invent simultaneous marks. |
| Cultivation/crop/field with real permanent water | Actual water may supply substrate while the selected crop/field remains visible; do not suppress its glyph or advertise preparation/stock. Removing water must restore the existing cultivation fallback, not force native dry Grass. This follows the established water-before-soil precedence (`FlatPixelGreyboxCatalog.cs:620–639`; North counterpart test at `FlatPixelNorthSurfaceWaterTests.cs:146–158`). |
| Remembered or unexplored cell | No native live water, bank topology or live hazards; preserve existing terrain-only memory/unexplored handling. |

The source-owner refusal is conservative and bounded. It should use current placement/sole-owned render/physics and actual visible identity rather than scanning all hidden entity internals. It is not a global claim that all source projections are unsuitable for native water.

## Reusable art and minimal integration

Use the unchanged approved resource `Sprites/Live16x24/opening-ground`, **336×24**, with 21 cached cells and **only frames 0–15** used here. Its source is `ArtSource/Live16x24/Ground/`; the [source README](../../../ArtSource/Live16x24/Ground/README.md) records full opacity, 16 PPU, center pivot, Point/Clamp, no mipmaps/compression, white draw tint, flat teal `#0C4344`, muted banks and quiet mask-0 interior. I inspected its contact sheet: no new art or optional animation is needed for the first comparison.

Keep the existing 14-frame Grovelands atlas and 22 borrowed forest cells byte-identical. A region does not need a separate watercolor when the actual material and established palette match. If the matched gameplay comparison exposes a named seam/palette defect, record that defect before creating an art derivative.

Suggested minimal runtime shape, subject to root's post-RED implementation decision:

1. Add an **independently optional** borrowed `opening-ground` group to the existing Grovelands companion; retain the current injected constructor as an explicit no-water route for the 91 material tests. A new overload may inject the extra texture. Missing/wrong-size/disposed group stays unavailable and never silently reloads `Resources`.
2. Add a small `TryResolveWaterGround` path using existing `ReadyCell`/`IsSupportedZone`/current-owner helpers and the strict coating rules above. Keep water separate from `TryResolveGround`'s dry Grass/subject-reader semantics. Reuse the established cardinal bank algorithm; no broad rendering framework or generation change.
3. Greybox routing takes that result as a separate success variable and output tile. On **actual native water success only**, set the opaque GroundTile, white GroundColor, GroundKind Water and LocalGround. Preserve the selected Owner, foreground, native-body result, blocking and reader authority. Suppress a foreground glyph only when it is the same validated ordinary Grass being represented.
4. Do **not** expand `FlatPixelGreyboxCatalog.IsAdmitted`, broaden `FlatPixelOpeningGroundCatalog.IsSupportedZone` or call private opening routes with forged authority. Do not alter `ZoneRenderer` overlay priority. Current call sites already pass `flat.LocalGround && flat.GroundKind == Water` to the strict suppression helper (`ZoneRenderer.FlatPixelPaint.cs:33–49`, `ZoneRenderer.cs:1266–1272,1394–1400`).
5. Dispose only newly created cached sprites/tiles. Borrowed source textures and fallback selector remain owned by their existing callers. No hot-path allocations, per-cell resource loads, image reads or world regeneration.

A global fallback currently always reports GroundKind Ground (`FlatPixelFallbackCatalog.cs:92–94`), so setting the water kind only after this successful native result is essential. Conversely, marking every Grovelands wet cell LocalGround would hide its fallback wave even if an optional texture failed. The new path must avoid both errors.

## Actual layering and dirty refresh

`ZoneRenderer.FlatPixelPaint.cs:14–49` resolves the validated cell, paints the independent mark, records the selected owner's actual Render event, then writes foreground and background. `EnvironmentSpriteRenderer.cs:1152–1172` uses that same presentation to claim foreground and opaque background. The stable sorting order is background −1, main 0, environment 3, tile-state 4, actor shadow/body 5/6 (`ZoneRenderer.cs:347–362`). The new water belongs only to background −1, never to the high-priority state layer.

Public tile-state write/removal notifies its cell (`ZoneTileState.cs:93–96,183–187,248–259`); `ZoneRenderer.SetZone` binds only the shown graph (`ZoneRenderer.cs:609–619`). Environment incremental redraw expands each dirty cell to its eight neighbors and reclaims their backgrounds (`EnvironmentSpriteRenderer.cs:1105–1131`). That supports cardinal shore changes without a new scan, but a submitted-render test must prove the neighbor mask actually changes. Do not infer incremental correctness from a direct catalog call alone.

## Bounded RED plan — preserve the existing 91 cases

Add about twelve meaningful cases to the existing real renderer fixture, not another harness. Runtime must remain unchanged until root records native RED. Each test first establishes its positive precondition; expected-fallback-only checks without a positive native comparison are insufficient.

1. Actual naturally generated seed-65 TendrilFen: locate a disclosed permanent ownerless water cell through its current graph, assert native water background and unchanged exact owner/entity count/state. Source fixture construction is a native test, not a second ordinary visit.
2. All 16 cardinal masks in one bounded loop; paired hidden/unexplored/expired-or-ice neighbor must remove only its bank bit. No diagonal influence.
3. Public submitted painter: plain Grass water background is native, its foreground grass glyph is absent, generic water mark absent; remove water and require current dry Grass/background restoration with no stale native claim.
4. Cover/uncover using a real Player or loose known product: same body, ID, reader and collision above water; remove the covering owner and recover only the represented Grass suppression. Repurposed/cultivated Grass is never silently erased.
5. Same real cell with cultivation/crop/field: water does not erase the foreground or advance growth; remove water and retain the original unsupported-soil presentation. No invented harvest/readiness.
6. Bounded rejection matrix: finite water, ice, water+oil, duplicate water and an expired extra all keep existing cues. Actual injected missing/wrong-size water resource must leave the wave visible, not just return false from a helper.
7. Submitted priority with positive native water: charge then heat/cold as applicable; residue/veil-mist remains earlier; no production priority reordering. Assert exact submitted mark and unchanged serialized state. Mixed coating is a paired unrepresented-water control.
8. GroveSeep positive-stock and zero-stock controls retain existing source bodies/waves and no new substrate. Existing 91 tests continue unchanged. Do not treat a service volume as the Fen's water owner.
9. Current-graph/map/POI/saved-lair revocation after a successful water resolution refuses native output; restored authority recovers it. Reuse existing fixtures rather than duplicate the full prior authority matrix.
10. Fog center after successful paint clears native water and hazards without exposing current neighbors; visible restoration recovers the mask. Unexplored public resolution remains false.
11. Dirty neighbor add/remove through actual public TileState calls changes the adjacent native mask via the real renderer; full-pass count does not rise. Full/dirty outputs agree and state/ownership remain unchanged.
12. Resource independence and lifetime: malformed water leaves Grovelands/portable bodies usable; missing other optional art does not disable valid water. Disposal does not destroy approved textures or mutate source imports. Repeated queries do not create new state or advance turns; no unmeasured allocation/performance claim.

Keep parameterization bounded and overlap intentional. Existing opening/North water tests can supply regression controls rather than being copied wholesale. Additional cases should respond to a concrete failure or source finding.

## Remaining production and acceptance gates

The actual baseline above is already captured. Root should save a focused reinterpretation/critique of that view against the unchanged approved water contact, retaining the exact braid, fog and routes; reject wider invented rivers, reflections, new plants and embellished banks. No concept may count as gameplay evidence.

After RED→minimum implementation→GREEN, replay the same earned Fen fixture and compare the actual whole view at player 40,15. Use ordinary movement to cross a visible real water segment and return; inspect a water cell and a covering actor/item where naturally available. Verify dry bank continuity, preserved approaches, native grass recovery, cursor/reader access, save/restart and source-state persistence. Retain raw captures and receipts; do not infer immunity, drinkability, collision or hazard acceptance from static water pixels. Inspect the Grove seep as an explicit unchanged counter-view if practical.

This bounded visible repair precedes Overwrit/Stump. It does **not** turn Grovelands into a completed biome or displace the full-world coverage queue: Fen/other actors, props, hazards, crops, special sites; Beating/Sodden remaining owners and water; Overwrit, Stump, other registered biomes, underground, settlements and interiors remain explicit work. No layout/density change is proposed, and whole-chunk fullness remains an actual gameplay gate.

## Preflight self-review

- 🔵 CoO-original material-continuity plan using existing exact state and rendering ownership; no Qud parity claim.
- 🔵 Actual saved Fen view and approved water contact were inspected; source mappings and overlay ordering were checked directly.
- 🔵 The native implementation and test gates are verified below; the 91 original material cases remain unchanged.
- 🧪 Actual after comparison, focused concept/reuse critique, package parity and sustained performance remain pending in this record.
- 🔵 The initial preflight changed this document only. Following root’s source-freeze release, the 12 test cases below were added; no Unity, Git, art generation or runtime changes were performed by this agent.

## Test-only handoff before native RED

After root committed the material source as `a2800c25f` and explicitly released
the freeze, 12 `FenWater_*` methods were appended to
`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGrovelandsMaterialsTests.cs`.
The earlier 91 case definitions and submitted `Paint` fixture are byte-for-byte
unchanged. The expected fixture count is now 103. Most new assertions use the
public Greybox and actual submitted painter; optional resource injection alone
uses reflection for the proposed six-argument constructor so missing production
can fail an assertion without stopping compilation.

Before native RED, source self-review corrected the new actor/item test’s
fixture lifetime: one `Paint` wraps the whole loop because
`EnvironmentSpriteRenderer.OnDestroy` disposes its catalog. It no longer tries
to reuse a world catalog after destroying that fixture. This was a saved
pre-RED test correction, not a production defect or native test result.

The physical source veto remains required even when `ProjectionOwnerId` is
empty: ordinary `Zone.ProjectPool` itself writes unowned permanent water.
Malformed null coating records are not submitted through the unchanged painter
in this bounded test set; existing renderer/serializer null behavior is not
claimed safe. Root must retain actual native RED evidence before authorizing
implementation, then run the same cases GREEN.

## Native RED and first implementation

Root’s native EditMode job `bfdbcd1375d84d76981ce7961af55fa7` completed
103/103 with failed status at **09:47:20–09:47:37 UTC**, before production. The uncapped raw receipt
[`grovelands-water-native-red.json`](grovelands-water-native-red.json) records
all 12 `FenWater_*` failures, each expecting native ground but receiving false.
No RED XML was retained; the UI lost its result after the job. The raw job
receipt is the evidence, and no XML result is invented.

After root authorized implementation, the companion added one independently
optional 21-cell borrowed group and a strict permanent ownerless water path.
The existing five-argument injected constructor still omits water explicitly.
The new overload is `(atlas14, sill24, vegetation4, opening10, water21, selector)`.
Only water cells 0–15 are selected; source textures and imports are unchanged.
A current visible physical source veto preserves the GroveSeep body/wave
contract, and cardinal masks use current disclosed coating-free neighbors.

Greybox now accepts a separate successful water result without overwriting
other foreground output. Only an exact selected ordinary Grass owner may lose
its glyph; actors/items/crops, collision and reader identity remain independent.
`LocalGround` and `GroundKind.Water` are set only after an actual optional native
water tile succeeds, so existing strict painter relinquishment is sufficient.
No overlay branch/order, generation, state, source palette or gameplay changed.

Implementation files are
`Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs`
and `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`; the test
file and this living record accompany them. The prior 91 tests and 12 new cases
are unchanged after RED. Native GREEN is verified below; root is performing
the actual matched-view comparison separately.

## First combined native GREEN and implementation self-review

The first post-implementation combined run passed **600/600**, with zero failed,
skipped or inconclusive cases, at **09:51:05–09:51:50 UTC**. Its complete native
XML is [grovelands-water-native-green.xml](grovelands-water-native-green.xml).
Direct XML inspection confirms **12 new Fen water cases, the unchanged 91
Grovelands material cases, and 497 related cases**. There was no post-implementation
trial failure, test weakening or production change to manufacture this result.
The full XML was retained after root focused/opened Test Runner before the job;
the earlier RED evidence remains the raw job receipt, not an invented RED XML.

The public submitted-painter cases verify the borrowed opaque background,
Grass-only foreground relinquishment, independent hazard priority, missing-resource
wave retention, fog clears and incremental neighbor banks. The natural Fen case
uses actual seed-65 generation, but it remains a native fixture: actual gameplay
acceptance is pending root’s matched earned-save replay. No new source pixels,
animation, generation, entity state or overlay-priority changes are included.

- 🔵 Existing water 0–15 cells were reused exactly; the 14 Grovelands material
  cells and existing forest groups remain unchanged.
- 🔵 Current map/POI/lair, disclosed current owner and permanent unowned water
  gates are retained. Visible physical source cells, including empty GroveSeep,
  preserve their prior body/wave behavior.
- 🔵 Optional resource failure cannot mark a water substrate represented; a
  successful water tile is required before the existing painter relinquishes
  only its water glyph. No global opening admission or mark-order expansion.
- 🔵 Native counterchecks cover actor/item/reader identity, cultivation restoration,
  mixtures/finite/ice/projected coatings, resource lifetime and real dirty repaint.
- 🧪 No new zero-allocation, performance, packaged run, Linux execution, physical
  Deck, all-water, whole-biome or whole-chunk-fullness claim follows from these tests.
- 🔵 Production and tests are now frozen. Further edits require a named actual
  defect or a concrete independent-review finding.

Exact files changed in this bounded water phase:

- `Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs`: optional borrowed water group, strict substrate/source checks, disclosed cardinal masks and owned sprite/tile lifetime.
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`: separate successful water-ground routing and exact selected-Grass suppression; existing foreground outputs retained.
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGrovelandsMaterialsTests.cs`: 12 appended cases plus bounded helpers; previous 91 definitions and the submitted painter fixture preserved.
- `Docs/Verification/Live16x24/grovelands-water-next-preflight.md`: source corrections, bounded plan, RED/GREEN receipts and acceptance limits.

Root owns the separately saved native job/XML evidence and the upcoming actual
captures, living coverage records, package refresh and main push. This document
does not certify those pending actions on their behalf.
