# G3 East surface-water continuity

Status, 2026-10-26: **bounded implementation,229/229native GREEN, independent
review and ordinary before/after/save-return accepted**. New Mac/Linux packages
from2a8e71044include this change and the meadow work, with ordinary Mac replay
accepted in the [delivery report](Verification/Live16x24/packaged-meadow-water-report.md).
This extends one existing material to one admitted graph. Whole East/opening art,
Linux execution and physical Deck remain open. Preparatory sections below retain
their historical pending state; final results and delivery supersede those gates.

## Named defect and bounded design

The current [bank baseline](Verification/Live16x24/east-water-before-bank.png)
shows the four-cell vertical water strip as plain teal rectangles beside the
player, contrasting with the opening pond's authored banks. Preserve the exact
footprint and reuse the accepted opening-water interior and disclosed dry-bank
frames. This makes the existing wet strip legible as the same material without
adding water, rocks, reeds, new resources or decorative owners.

The Qud reference here is the project's established readable, flat native
16×24 visual language. No new Qud source/asset research or copied artwork is
claimed. The user-approved characters, foliage and existing atlas stay intact.

Scope is `Overworld.12.10.0` **only when the existing current-world, Spread,
no-POI admission succeeds**, and only a sole live `water` coating. Keep glade
water and current Nella soil behavior as before. North, reserve water, kitchen,
cellar, other biomes, mixed liquids, ice, unexplored/remembered state and East
roads/fields/prepared-soil artwork do not acquire this permission.

## Actual baseline and next replay

Root normally Continued the preserved earned kitchen fixture, seed `200893250`,
game `2e93a161b86a495483a3c2d81dab34e2`. Ordinary Right4/Up10 reached East `(27,24)`;
Right23/Up4 reached `(50,20)`. The matching [bank receipt](Verification/Live16x24/east-water-before-bank.json)
records tick4700, HP30/42,50drams and flat mode. F5 preserved the earned bank
checkpoint under [Fixtures/east-water-earned-bank](Verification/Live16x24/Fixtures/east-water-earned-bank/).
One further ordinary Right reached actual water `(51,20)`, tick4710 and the
Soaked feedback in [before-on-water](Verification/Live16x24/east-water-before-on-water.png)
and its [receipt](Verification/Live16x24/east-water-before-on-water.json).
No teleport, grants, new water or restored health supplied these captures.

The observational receipt lists eight pure-water cells: upper
`(11,2),(10,3),(11,3),(10,4)` and lower `(51,17–20)`. Each has one permanent
water record. It separately identifies brine near `(69–71,2–3)`; blue `~` marks
are not evidence of clean water. Receipt rows may describe state beyond the
visible screenshot and are not a camera-visibility census. The screenshot file
is1280×800; the receipt separately records the editor's larger reported screen
size. These must not be confused.

The [restoration receipt](Verification/Live16x24/east-water-before-restored.json)
records stopped Play, no audit isolation/view/save-root override and normal
SampleScene at10:00:15UTC. Root now owns the isolated native RED run. After
GREEN, reuse the earned bank checkpoint, ordinary Continue and the exact same
`(50,20)` framing. Cross Right onto `(51,20)` again, inspect actual water and
check the foreground over it, then save/load and leave/return if safe. Record
actual actions, tick/HP/resources, renderer and source; do not claim a freeze
spell was exercised in ordinary Play merely because its native test passed.

## Verification sweep and corrections

| Premise / audited authority | Correction and consequence |
| --- | --- |
| A water-looking glyph or saved water coordinate is sufficient | `FlatPixelOpeningGroundCatalog.TryResolve` reads current `ZoneTileState`; it does not reconstruct a pond from generation coordinates or a `~` glyph. Keep that authority. A finite `LiquidPoolPart` and an unowned coating are different gameplay facts; rendering must not create a reservoir or spend one. |
| Every active water entry is clean water | The renderer's `IsFlatPixelWaterRepresented` rule requires exactly one coating record, ID `water`, Turns>0. A second retained expired record, duplicate water, oil, brine or ice must retain fallback. Do not replace this with `HasCoating("water")`. |
| Neighboring non-water means dry bank | `DisclosedDry` first proves a current explored/visible cell and strict selector, then requires no coating records. Hidden, unexplored, ice/oil or retained expired entries do not establish dry ground. Frames use dry-edge bits N1/E2/S4/W8, unlike hedge connected-owner bits. |
| Opening ground needs another atlas | The existing `Sprites/Live16x24/opening-ground` is336×24:16water masks,2road frames,stubble,dry/wet prepared soil. Reuse frames0–15 at16×24, centered pivot, Point/Clamp/no mipmaps. Preserve existing native pixels/editable sources. |
| Setting `opening=true` for East is safe | It would also admit roads and opening-specific soil rules. Widen only exact supported-zone routing and the water eligibility predicate, then reject East before the non-water field/prepared loop. Nella and the original opening retain their separate behavior. |
| A test-forced catalog proves shipping routing | `FlatPixelGreyboxCatalog.TryResolvePresentation` also checks `IsSupportedZone` before constructing the optional companion. Test the real default constructor and imported resource with an actual detached manager/current East graph. The old injected constructor stays resource-free. |
| An ID proves a current graph | `FlatPixelFallbackCatalog.IsCurrentGraph` and `SpreadPresentationScope` require the manager's cached graph reference. `IsAdmitted` additionally rejects a different biome or an occupied POI address. Keep all three authorities. |
| A moving actor should replace water ground | The strict selector chooses the foreground owner independently of the current lower coating. Preserve actor/item reference, collision and position while drawing water below it. |
| New neighbor invalidation is necessary | `ZoneTileStateSystem.BindRenderHook` publishes real coating changes; `EnvironmentSpriteRenderer.PostRender` already expands dirty cells to their eight neighbors. Reuse it; verify submitted ground changes without a new full pass or manual test dirtiness. |
| TileState.Tick freezes water | Tick only ages written layers/energies. The existing authored `freeze_water`/`melt_ice` reactions are in `Content/Data/TileReactions/Reactions.json`. Test real reaction resolution separately from expiry and preserve the ice mark. |
| A water bank replaces all hazard overlays | `ZoneRenderer.PaintTileStateMark` suppresses only represented sole water. Charge, residue, clouds, heat/cold and mixed/ice marks retain existing priority. No overlay production rewrite is needed. |
| All existing tests remain semantically current | `FlatPixelOpeningGroundCatalogTests.OtherGraphsDoNotBorrowTheOpeningGroundFamily` currently lists exact East as an excluded water address. After proving RED, update that obsolete case while retaining a genuine unsupported address and the new East non-water counters. Do not weaken unrelated coverage. |

Source authorities: `FlatPixelOpeningGroundCatalog`, `FlatPixelGreyboxCatalog`,
`FlatPixelFallbackCatalog`, `SpreadPresentationScope`, `ZoneTileState`,
`ZoneTileStateSystem`, `TileReactionSystem`, `ZoneRenderer`,
`EnvironmentSpriteRenderer`; existing opening-ground and ground-mark fixtures.
`WaterPuddle` in Objects.json has finite LiquidPool volume120 and no explicit
TileStateSource in its blueprint. Never assume that merely placing that owner
establishes the runtime coating under test.

## Pipeline prompts and extraction

**Brainstorm prompt:** Review the actual East bank capture and exact live water
records. Identify the smallest reuse of accepted material that makes this wet
strip readable beside the existing flowers, trees and service space. Preserve
positions, collision, resources, hazards and the quiet background. Reject a
new decorative packet or regional expansion.

**Design prompt:** Compare the current strip with the accepted opening-ground
atlas. Reinterpret only its material in imagegen, then separate usable guidance
from artifacts. Bind every accepted pixel/state rule to actual visible coating
and neighbor disclosure. Specify counters for hidden, frozen, mixed and dry
states and an exact ordinary before/after replay.

The built-in imagegen [prompt](../ArtSource/Live16x24/concepts/east-water-continuity/prompt.txt),
[concept](../ArtSource/Live16x24/concepts/east-water-continuity/concept.png) and
[extraction](../ArtSource/Live16x24/concepts/east-water-continuity/extraction.md)
use the real baseline plus existing atlas. Accept quiet teal interiors and
restrained broken bank pixels inside the existing cells. Reject resizing,
softened pixels/ground, altered UI/foliage, saturated banks and uniform edging
on every side. No generated pixels enter production.

| Extracted part | Native/source mapping | Admission and state |
| --- | --- | --- |
| Water interior +16bank masks | Existing `Assets/Resources/Sprites/Live16x24/opening-ground.png` frames0–15; editable `ArtSource/Live16x24/Ground/source-cells.json`, `opening-ground.blend`, `build_ground.py` | Exact current admitted East; sole positive water record; disclosed coating-free cardinal edges only. |
| Actor/item over water | Existing foreground owner and ordinary environment Tilemap | No owner replacement, extra sprite object, placement or resource change. |
| Frozen/mixed/unknown/hidden water | Existing fallback and hazard painter | Refuse new bank material and preserve current disclosure/priority. |

**Implementation prompt:** First run the new fixture against unchanged production
and retain native RED. Make the smallest `FlatPixelOpeningGroundCatalog`
admission change; retain the `opening` predicate for its old non-water behavior
and explicitly stop exact East before dry-field/soil selection. Reuse the
existing optional atlas, caching, lifecycle, default route and dirty-neighbor
expansion. Run focused GREEN and neighboring regressions, independently review,
then ordinary replay from the earned bank save and compare actual screenshots.
Fix material defects before committing exact owned files and living evidence.

Design pseudocode (the minimal candidate is now saved; acceptance remains pending):

```text
supported = existing opening/Nella scope OR exact admitted East
waterAllowed = existing opening OR exact admitted East
if disclosed current cell has a sole live water coating:
    return existing bank frame from disclosed dry cardinal neighbors
if exact admitted East:
    refuse native non-water ground
continue the existing opening/Nella non-water rules
```

## RED, GREEN and counter-checks

`CavesOfOoo.Tests.EastSurfaceWaterPresentationTests` contains36expanded native
cases, authored before production. Three real default-world cases use seeds64,
65 and200893250 and assert the imported native resource, frame13, white tint,
retained foreground and loss of water art after actual coating removal. The
remaining focused groups cover:

- all16mask combinations with stable cache and no state/collision mutation;
- visible/hidden/unexplored neighbors and center; ice/oil/expired-record counters;
- mixed/duplicate/expired-extra records and sole ice fallback;
- East RoadStone, CropRow, RipeCropRow and prepared Grass exclusion;
- unchanged north/Nella-water/kitchen/cellar/other-site scope and original glade;
- exact current graph, biome, POI and unbound-graph authority;
- actor/item coverage and finite-pool preservation without inventing stock;
- missing optional art, old injected-constructor fallback and warm allocation;
- actual Tick expiry/re-add below unchanged foreground through bound hooks,
  no additional full pass, and a quiet next frame;
- actual authored freeze/melt, ice's pale `~`, wet-neighbor semantics and
  bank restoration through the same submitted rendering path;
- actual charge/embers/steam overlays and expiry above native bank ground.

The fixture uses the existing renderer/Tilemap harness with the **default**
resource catalog. Its supplied explored/visible cells test presentation and
invalidation, not ordinary player-driven FOV or travel. It snapshots/restores borrowed hooks, saved globals, messages,
performance counters and reaction definitions. No tested coating mutation is
followed by a manual MarkDirty. Native tests prove those script-observable
contracts, not camera appearance, ordinary input, performance on a Deck or a
completed scene. Root owns native execution and records actual receipts/results.

Recommended regression fixtures: `FlatPixelOpeningGroundCatalogTests`,
`FlatPixelGroundStateReadabilityTests`, `FlatPixelGreyboxCatalogTests`,
`FlatPixelGlobalPresentationTests`, `FlatPixelFallbackCatalogTests`, plus the
existing incremental renderer fixtures if that code changes. Do not add a new
rendering framework or run unrelated full-project tests merely to inflate counts.

## Review, acceptance and handoff

- 🟡 Keep exact East admission separate from opening soil/road eligibility.
- 🟡 Preserve coating-list and current/disclosed-neighbor authority; no hidden
  shore inference, mixed-liquid disguise or invented finite source.
- 🔵 Reuse accepted art unchanged. This is a material-continuity correction,
  not a claim that the channel layout, whole East scene or every water material
  is finished.
- 🧪 Native execution, independent code review, actual same-view after capture,
  movement/Soaked feedback and save/return remain pending at this plan revision.
- ⚪ Broader regional banks, brine/mire art, new ripples/animation and gameplay
  hazard changes are explicitly deferred.

Acceptance requires readable banks at the actual camera scale, correct open
water joins, the same real footprint and owner/resource state, unchanged hazard
cues and ordinary play evidence. Share actual before/after screenshots inline.
Current packages predate this change; Linux execution and physical Deck remain
separate gates. No storage deletion or cleanup is part of this slice.

Files authored in this preparatory pass: this plan,
`Assets/Tests/EditMode/Presentation/Rendering/EastSurfaceWaterPresentationTests.cs`
and its metadata. No production, Unity, package or git action was performed by
the plan/test author. Root owns integration, native runs, live acceptance,
continuation/coverage updates and main publication.

Implementation log, 2026-10-26: source audit, actual root baseline/receipt and
imagegen critique reviewed;36focused cases saved before runtime. Root's native
[RED summary](Verification/Live16x24/east-water-red-summary.json) and
[XML](Verification/Live16x24/east-water-red.xml) record10:04:22–10:04:25UTC,
0passed/36failed/0skipped. Failures establish missing East bank admission or the
default placeholder resource, rather than compilation or fixture setup errors.
After that RED, root saved the bounded production candidate and changed the
existing unsupported-address case from12.10to12.12. Independent fixture review
found no material setup/contract/restoration issue; its finite-pool preservation
claim is specifically an ordinary WaterPuddle, not a finite film. Native GREEN,
code review and ordinary after evidence are still pending. Root now owns this
document for final results and continuation.


## Integrated result and final review — 2026-10-26

After the actual36-failure RED, root changed only the optional ground catalog:
exact admitted East can borrow its existing16water frames; East exits before
all dry field/road/prepared logic. No native pixels, assets, generation, liquids,
collision, renderer invalidation or save format changed. The obsolete East
water-negative case now uses unsupported12.12; dedicated new counters retain
East dry-family refusal.

Native [GREEN XML](Verification/Live16x24/east-water-green-03.xml) and
[summary](Verification/Live16x24/east-water-green-03-summary.json) record
**229/229 passed,0failed,0skipped**,10:06:47–10:07:02UTC:36new,71opening-ground,
20ground-state,33greybox,29global and40fallback cases. Earlier GREEN requests01
and02 were refused by a stale MCP tests-running flag: no native tests ran in
those attempts. The original RED job later reported bridge initialization
timeout despite its complete native XML; both receipts are retained. No test
assertion was weakened to resolve that transport issue.

Ordinary default-presentation Continue from the earned bank checkpoint restores
(50,20),tick4700,30/42HP,50drams. The actual
[after-bank](Verification/Live16x24/east-water-after-bank.png) and
[after-on-water](Verification/Live16x24/east-water-after-on-water.png) show the
new bank/ripple material, real foreground and unchanged water/Soaked feedback.
All371recorded owner tuples match by exact ID, as do inventory/coatings and the
player scalar state between matched before/after views. Enumeration order
changes across loading; no entity-field differences were discarded.
F5/F6 at(51,20),tick4710 retains those same records and the native water. Down5
crosses the real southern border into kitchen(51,0); Up5 returns to
East(51,20),tick4810,30HP,50drams with the bank intact. Time and moving NPC state
can advance during travel. The water footprint/layers remain exact. See the
[play journal](Verification/Live16x24/east-water-play-journal.md) and
[state comparisons](Verification/Live16x24/east-water-state-comparisons.json).

The86.13s unwrapped editor profile includes one water step, save/load and
substantial idle/tool time:18,551samples; ZoneRenderer mean0.126ms,p99 0.282ms;
main-thread mean4.64ms,p99 21.13ms with a1.028s maximum during this mixed
interval;41.01mean/42max draw calls. This is neither a controlled performance
comparison nor a Deck benchmark. The focused warmed selector check allocates
zero managed bytes; idle/native dirty tests avoid unnecessary full redraw.

- 🟡 Fixed the observed material discontinuity through default production
  routing, with strict disclosure, live-water and exact-site authority.
- 🟡 Prevented dry East material scope expansion with an explicit early return
  and positive/negative native cases.
- 🔵 Independent source/fixture/evidence review found no material blocker.
- 🔵 Accepted artwork/editable sources remain unchanged; no new allocation
  family, shader, atlas or per-tile GameObject was introduced.
- 🧪 Freeze/melt and overlay adversarial checks are native, not ordinary spell
  input acceptance. Linuxexecution and physicalDeck remain unverified.
- ⚪ Wholeopening/regional artwork and current packaged parity remain open.

Root stopped owned Play and restored SampleScene, save root, preferences and
view.29Unity importer trailing-whitespace rewrites were individually verified,
backed up and restored; no content/dependency/source was deleted.776normal save
files,374preexisting art/source files and two foreign screenshots match their
baseline. Storage remains inspection-only, about427.5GiB free.

Next: freeze this reviewed source on main, refresh dated Mac/Linux current
packages to include both meadow massing and water continuity, then ordinary Mac
Continue/new-game and the earned crossing/save-return. Preserve every existing
package and current archive. After delivery parity, review the complete six-zone
route, with Nella's service-loss audit still separate from art approval.
