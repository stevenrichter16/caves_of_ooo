# Opening ground — source and partial ordinary Play review

Status, 2026-10-24 UTC: **native source accepted; first matched ordinary
Play views reviewed without a material visual blocker**. The approach and
water-to-ice reaction, water traversal, covered old field, harvest and dry
preparation are now visible in actual captures. Actual thaw/restored water and
prepared wet/dry moisture transitions remain pending at this checkpoint. This does not complete the G2 scene or the graphics release.

## Source review

The original integer-quad atlas was inspected at native size, in the direct6×
contact and the direct4× continuity study. Independent actor review found no
material static-art blocker: shallow irregular banks have no raised masonry
faces, road wear repeats quietly, old field and prepared furrows are distinct,
and wet soil darkens the same geometry without inventing crops or water icons.
The source-layout illustration omits actors, fog and live state; it is not a
replacement for the gameplay evidence below.

Read-only verification recorded zero native/source/contact/context pixel
mismatches across21 fully opaque16×24 frames, bounded dry-edge treatments and
identical dry/wet geometry. A second Blender export reproduced identical atlas
bytes. Source and copied Resources PNG share SHA256
`b35f9d6a4b7051eb6971a00195a069f58d08a7d89e3060533745ee899a66dede`.

## Actual matched start view

Compared [the prior masonry start](../../../Docs/Verification/Live16x24/g2-normal-start.png)
with [the ground-pass start](../../../Docs/Verification/Live16x24/g2-ground-start.png)
at their actual1280×800 output size. The corresponding JSON receipts agree on
seed64, `Overworld.11.10.0`, player(40,12),40HP,tick10, Normal input and no forced
presentation override. The new receipt records92 current ground claims:
52 water,20 road and20 old-field cells. These are current visible claims, not
the total world census or a claim that every atlas frame was exercised.

1. **Banks and water:** the narrow muted inset edge distinguishes the wet
   material without introducing the bright rim, glow or raised face from the
   concept. The interior stays calm and continuous. No repeated glint grid,
   transparent tile seam or suggestion of stepping stones is visible. The bank
   contrast remains below the existing masonry; the separate basin stays legible
   at the break between the two approach strips.
2. **Roads and field:** sparse embedded road wear reads as a flat surface, not
   loose stones or new obstacles. At this native viewing scale the repeated
   marks stay quiet. The old-field treatment remains subordinate to the actual
   grain/stubble silhouettes; it does not add seeds, tools or additional yield.
   Plain green retains the scene's visual breathing room. Prepared dry/wet
   frames19/20 are absent from this receipt and are not accepted through this
   view alone.
3. **Foreground and UI hierarchy:** the player remains readable against plain
   ground, crops retain their contrast over field earth, and the unchanged walls
   remain the stronger boundary cue. The right log/vitals and bottom skill bar
   retain their spacing, text and borders, with no ground pixels spilling into
   either UI region. A read-only pixel comparison finds the player tile, right
   HUD and hotbar identical between these two captures. Some distant actors
   occupy different positions, so this is not a claim that all scene pixels or
   simulation placements match.

No source-art revision is justified by this start-view comparison. The change
is intentionally quiet; it improves material distinction without competing
with functional foreground owners. The next useful evidence is interaction
and changing ground state, not adding texture merely to increase detail.

## Pond approach and freeze follow-up

[Approach](../../../Docs/Verification/Live16x24/g2-ground-pond-approach.png)
shows the player at(38,7),40HP,tick80 after ordinary movement. The banks remain
subordinate to reeds and masonry, and the basin/road gap remains distinguishable.
The new viewpoint reveals the prepared bed's dry frame19, but it does not by
itself demonstrate a prepare/water action or moisture transition.

[Frozen pond](../../../Docs/Verification/Live16x24/g2-ground-pond-frozen.png)
shows the Rime Grip log and cooldown at the same player position, tick90.
The saved state changes five cells(33–37,7) from water to ice: water0, ice3,
with five native water claims retired. A clear white existing ice mark replaces
the teal ground there. Surviving wet ground stays continuous; the adjacent ice
does not acquire an invented dry-bank rim. The bright zig-zag ice cue is existing
hazard presentation, not an authored native ice surface or proof that every
hazard's final art is complete. It is readable and is not a blocker for this
bounded water-ground packet.

The former misleading thaw filename has been corrected to
[g2-ground-thaw-attempt.png](../../../Docs/Verification/Live16x24/g2-ground-thaw-attempt.png).
This is a **failed thaw attempt**, not accepted evidence of thaw: its receipt
remains tick90 with the same five ice3/water0 cells. It must not be used to claim
melting or water restoration.

## Ordinary water traversal and field follow-up

These are separate recorded journey segments; their tick values are not a
single continuous chronology.

[Water walk](../../../Docs/Verification/Live16x24/g2-ground-water-walk.png),
player(36,8),40HP,tick120, shows the player clearly over the still-wet foreground
cell and the right-side ground description reads water. The background stays
teal beneath the actor. The earlier ice strip has expired and now exposes dry
ground, not restored pond water; no ice remains in the receipt. Its surviving
wet neighbor(32,7) changes from mask0 to mask2, giving the new east dry edge.
The new notch matches the actual changed state rather than restoring the
original source ellipse. No stale white ice mark or bank into the neighboring
cell is visible. **Ice expiration is not a successful thaw/melt action.**

[Covered field](../../../Docs/Verification/Live16x24/g2-ground-field-covered.png),
player(46,12),40HP,tick90, retains frame18 old-field soil under the player and
actual unharvested RipeCropRow. No plain-green hole appears where the actor
stands, and the gold figure stays legible above the darker field.

The [Harvest menu](../../../Docs/Verification/Live16x24/g2-ground-field-harvest-menu.png)
at adjacent player(45,12) offers the real ripe-row action. The
[harvested result](../../../Docs/Verification/Live16x24/g2-ground-field-harvested.png)
at tick110 logs one grain packed, sets the target row's `harvested=true` and
preserves frame18. Standing ripe grain becomes stubble; the soil does not
pretend the bed was prepared or that fresh yield returned.

The [Prepare menu](../../../Docs/Verification/Live16x24/g2-ground-field-prepare-menu.png)
then offers the real cut-stubble preparation action. The
[prepared result](../../../Docs/Verification/Live16x24/g2-ground-field-prepared.png)
at tick120 logs preparation with the grain already spent. Target(46,12) remains
harvested, now has `cultivated=true`, and changes to frame19 while neighboring
old-field cells keep18. The small broken furrows remain subordinate to the
stubble rather than looking like a new crop or raised boundary. Menus and world
return stay legible; these captures do not establish every fullscreen/mode path.

No material ground-art defect was found in these new captures. Frame20 wet
prepared soil remains **source/native-test-only**: no ordinary wetting/drying
capture is claimed here.

## Remaining acceptance boundaries

**Can verify here:** native source geometry and rendered pixels; current start
view material contrast, continuity and UI containment; the saved start-state
metadata and visible claim distribution; the later approach and actual visible
water-to-ice transition with retained hazard cue; subsequent water traversal,
ice expiration/new dry edge, covered old field, Harvest and dry Prepare results. The parent performed the ordinary
launch and saved the capture; this review used only files and image inspection.

**Cannot verify from these captures:** a loose item preserving every material
underneath, finite basin use, successful thaw/steam state changes,
hidden-neighbor nondisclosure under changing fog, wet/dry furrow transitions,
reserve cords and every hazard combination, save/load or mode roundtrips,
frame cost, physical Steam Deck feel,
or a finished scene/release. Native tests and later ordinary Play receipts must
support those claims separately. No Unity, runtime or art files were changed
by this review; only this source review document was added.
