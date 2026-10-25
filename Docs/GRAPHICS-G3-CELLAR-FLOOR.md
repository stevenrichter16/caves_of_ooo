# Cellar floor: quiet material beneath the working store

Status: native source and test draft prepared,2026-10-25. Not integrated or accepted. The
preceding Nella slice is accepted and preserved on main at`d313eac64`.

## Why this follows Nella

Independent review of the six actual opening scenes chose the cellar's floor
as the next bounded composition gap: the solid masonry has a material language,
but the floor between it is a featureless swatch. This is a visual enrichment,
not a missing-floor, pathfinding or interaction bug. The current floor already
has a darker color than outdoor grass. Keep the accepted walls, real room shapes,
actors, crops and all useful empty walking space.

Reviewed actual captures under`Docs/Verification/Live16x24/`:
`fresh-opening-new-warden.png`, `fresh-opening-cellar-entry.png`,
`fresh-opening-north-reloaded-return.png`,
`nella-composition-64-final-accepted-frontage.png`,
`fresh-forge-station-approach.png`, `fresh-forge-kitchen-continued.png`.
These differ in time, visibility and location; they are not a lighting regression
comparison or evidence of complete six-scene acceptance.

Other ranked findings: loose`LeatherArmor`6954 at16,17 in the north receipt
still uses a world glyph. The kitchen's visible`P` needs ordinary Look to identify
it; do not assume it is a Hatchet. Distinct combat hotbar icons are also missing:
`HudHotbarPresentation.IconFor` maps only six original powers, so the Warden and
Breaker use the generic diamond. Preserve these as later bounded tasks; don't
mix them into this terrain change. Nella and kitchen frontage need no speculative
reopening. East CharmFlowers already have native art.

## Design prompt acted on

Compare the six real scenes as one journey. Choose the largest tractable visual
inconsistency that can be improved without new gameplay or altered collision.
Verify the real owners and current renderer. Reuse the actual playable scene as
the greybox, reinterpret its screenshot, then extract a very small material family
that can be authored at16×24 and placed only on legitimate current terrain.

The selected exact prompt and built-in imagegen output are
[here](../ArtSource/Live16x24/concepts/cellar-floor-prompt.md).
Input was actual`fresh-opening-cellar-entry.png` at1280×800, player40,12,
seed64, cellar`Overworld.11.10.1`, tick240,40HP. No new runtime or forced layout
was used for concept work. Existing reference-library R01 contributes its quiet
negative-space principle only; no external artwork is imported.

**Concept decision:** retain sparse short broken slab seams. Reject altered HUD
text, resized view, changed actors/walls, gradients, fog/light shifts and the
regular repeating T marks. The generated screenshot is reference only and is
never a background or proof of gameplay integration.

## Source verification before implementation

| Premise | Verified source / consequence |
| --- | --- |
| Every open cellar cell is a real floor. | `GleanersCellarBuilder.BuildZone` creates exact`Floor` in its open mask and`StoneWall` elsewhere. No terrain insertion or layout edit is needed. |
| Existing cellar is simply missing an art resource. | More precisely,`GroundOf` selects`StoneFloor`, then`GroundColor` returns`(24,44,41)`. The opening-ground catalog only supports glade/Nella. Add one optional scoped background material; do not broaden the existing atlas admission. |
| Floor can be selected from the top object. | Incorrect: player, stairs, beam or loose items can cover it. Resolve actual lower floor ownership independently of the foreground. Preserve selected owner, collision and foreground art. |
| Every visible floor is safe to decorate. | Incorrect: cultivated soil, owned crops/field rows, pools/gas and tile state/coatings must keep their current priority. Unknown or ambiguous state should decline the new material. |
| Memory should repaint current floor detail. | No: the initial slice remains visible-only. Existing conservative memory rendering stays intact and cannot read hidden live crop/coating values. |
| Empty GlyphVariants identifies the original floor. | Incorrect: canonical factory Floor authors ``GlyphVariants="...,.'`"``. Requiring empty variants would reject the real owner. Preserve this authored variant contract; reject explicit reskins. |
| Two brown beds are separate terrain objects. | Incorrect: the generator makes two existing Floor owners Plantable and adds CultivatedSoilPart. Stone must remain suppressed after crop harvest while the prepared bed persists. |
| Every Floor-shaped object may share the new art. | Exact cellar and current admitted graph, current cell/placement, one ordinary terrain Floor with owned visible Render/Physics, ordinary visual identity, no inventory/equipment/invalid stack or foreign footprint. Malformed/custom/duplicate candidates retain fallback. |

Relevant current integration point:`FlatPixelGreyboxCatalog.TryResolvePresentation`
selects native background before foreground/boundary art. Gate on the resulting
`StoneFloor` kind and exact admitted cellar, then independently prove the lower
owner and absence of competing tile state. Do not require surviving stairs or
service owners; cold-generation admission is not current presentation authority. The working-court
companion demonstrates borrowed atlas and cached tile lifetime, but its surface
Grass/StoneFloor role rules must not be copied as cellar semantics.

## Parts extraction and native art contract

No new props. One small opaque floor atlas, proposed four16×24 frames:

| Frame | Owner/state | Intended mark |
| --- | --- | --- |
| 0 | Eligible ordinary Floor, visible | Quiet unmarked base; dominant frequency. |
| 1 | Same | One short broken horizontal seam with a restrained end cluster. |
| 2 | Same | Offset shorter seam and tiny worn corner, asymmetrical. |
| 3 | Same | Sparse worn stone joint; no enclosing border or regular T grid. |

Three flat dark green-gray colors; square pixels; Point/Clamp; no mipmaps or
compression; center pivot/16PPU; no tile collider. Start near existing`#182C29`
base and judge contrast under real lighting. Native variation is a pure stable
coordinate choice; do not consume simulation RNG, use owner allocation IDs,
read neighbors through fog or change with time. Prefer many blank cells over a
floor carpet. Most pixels in every frame stay base color. No cracks suggest
hazards, no debris implies items, no direction marks imply a safer route.

Preserve editable integer-pixel source, palette/frame manifest, deterministic
export tool and native-size contact. All new Assets files need fresh metadata.
Use a separate optional catalog/resource so missing or malformed art retains the
existing swatch and older injected catalog tests cannot silently load resources.

## Implementation prompt for the next pass

Read this plan and current graphics handoff. Revalidate the source contracts,
then write and run native failing tests for the exact visible cellar Floor
background. Implement the smallest optional cached companion and world adapter.
Keep all accepted sprite bytes, geometry, gameplay, projection and saves unchanged.
Author the four native material frames from the selected sparse-seam concept,
not by shrinking the generated screenshot. Import only after storage headroom
can safely accommodate compilation. Run focused RED/GREEN, negative authority
and precedence counters, then existing cellar/ground/fallback/court regressions.
Repeat the real cellar entry and service route with ordinary controls; inspect
actor/prop coverage, fog transitions, cultivated sootroot beds, departure and
return. Compare actual captures at the same1280×800 scale; adjust only the floor
marks if they are invisible, repetitive or too bright. Independently review,
record honest bounds and push exact owned changes after fetch/rebase.

## Acceptance matrix

- Positive: one ordinary floor below player, stairs, item and beam resolves the
  same stable background while preserving foreground identity/collision.
- Negative: other zones, replaced manager graph, stale/moved owner/cell,
  hidden/unexplored cell, altered Render/Physics ownership, foreign VisualID or
  variants, takeable/carried/equipped floor, invalid stack, multiple floor candidates,
  malformed footprint, missing/wrong-size atlas and disposed catalog retain fallback.
- Precedence: dry/wet cultivated soil, valid crop, field row, liquid/gas and
  nonempty tile state suppress the stone material; removal restores it. No hidden
  state inspection through memory. Matching identical setups without the
  competing state do resolve the floor.
- Submitted Tilemap: actual water/oil/ice write and removal must dirty/repaint
  background without a full redraw and without changing foreground. Direct catalog
  queries alone do not satisfy this integration gate. Reuse the existing coating
  producer-driven test pattern in`FlatPixelOpeningGroundCatalogTests`.
- Resource: exact dimensions/palette/opaque pixels/import settings; original
  approved art hashes stable; no per-cell Sprite/Tile allocations; borrowed
  texture/selector survive disposal; repeat calls reuse cached tiles.
- Actual Play: ordinary entry/landing, both routes as reachable, stairs and
  terrain Look, beam/store passage, crop beds, save/return with same owners.
  At least one fresh second-seed opposite passage for composition comparison.
- Visual: floor reads as worn stone below existing masonry, actor and useful
  object silhouettes remain dominant, no regular bright grid, no new obstacle
  or misleading interaction. Actual matched screenshots are required.
- Limits: no controller comfort, physical Deck performance, current-package
  delivery or whole-opening completion claim from Editor tests/captures alone.

Regression groups:`FlatPixelCellarScopeTests`,
`FlatPixelOpeningCellarArtTests`, `FlatPixelOpeningGroundCatalogTests`,
`FlatPixelWorkingCourtTests`, `FlatPixelGlobalPresentationTests`,
`GleanersCellarBuilderTests`. Independent source review confirmed these seams
and corrected the canonical-variant and same-owner cultivated-bed premises.

## Current handoff and storage limit

Only this plan, exact concept prompt/output and critique exist. No floor runtime,
asset import, native test or new floor gameplay acceptance has happened. Unity is
out of Play in clean SampleScene with prior audit state restored. Earned checkpoints
and user saves are preserved. Roughly300–340MiB available remains a practical
build/import risk; do not wipe active Library or delete current packages. The next
step is the exact RED fixture plus editable native source once write headroom is
adequate, then the complete integration/ordinary-play cycle above.


## Continuation — native source and exact storage candidates,10:54UTC

The selected concept's sparse marks are now authored as editable integer-cell
source in`ArtSource/Live16x24/cellar-floor/source-cells.json`. Four16×24 frames
use three exact flat colors and0/10/9/7 marked pixels respectively. The new
`Tools/Live16x24/export_cellar_floor.py` reads only these cells and writes a64×24
opaque atlas, an8× source contact and manifest inside ArtSource. It does not
read/resize a generated screenshot or import anything into Unity. Two exports
were byte-identical; independent PNG CRC/dimensions/palette/alpha verification
passed. Native atlas SHA256:
`bad3d80ec59655e46dad6a0df992a8a98a1c87fe6b8623e6a3478a45b4b8da0c`.

Independent source-art review found no significant issue:97.4–100% base pixels,
short irregular seams, no enclosing border or repeated T grid. Room-level
distribution and readability under game lighting remain unverified.

`Tools/Live16x24/drafts/FlatPixelCellarFloorTests.cs.txt` contains15methods/34cases
for the proposed optional catalog and submitted Tilemap. It stays outside Assets:
**uncompiled, unrun, not RED evidence**. Its independent review corrected one
premise: Player movement requests a full FOV redraw intentionally; only coating
changes assert incremental repaint. The next runtime gate is to promote/import
that draft and record a genuine native failing run before implementation.

The owned Editor was verified idle/clean/out of Play and closed normally with
Cmd+Q. MCP refused a programmatic Exit pattern; its safety guard was retained.
A bounded audit identified477.7MiB allocated player-only intermediates from the
completed02:08Mac/02:18Linux builds, separate from active editor/import/JIT
caches. Exact3552-file candidate manifest is preserved as
`Verification/Live16x24/cellar-floor-stale-player-cache-manifest.json.gz`. It
includes11directories,28player graph metadata files plus2AOT hash files.
**No candidate deletion yet.** Push this source/evidence first, recheck all
identities/no handles/current-package hashes, then reclaim only those stale
intermediates and measure actual free-space gain. Preserve Editor graphs,
ScriptAssemblies, import/shader caches, JIT, dependencies, sources and saves.
