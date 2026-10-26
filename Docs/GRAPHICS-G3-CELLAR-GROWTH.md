# Cellar watered-growth visual acceptance

Status: bounded current-package crop lifecycle accepted, 2026-10-26.
Whole-opening and device acceptance remain open.

Graphics completion is the primary release blocker. This pass closes an
explicit ordinary-play gap using existing native art. It does not assert a
new defect or replace accepted sprites before inspecting their actual states.

## Plan and source sweep

Use current e49047200 Mac package, normal SampleScene/GameBootstrap, default
flat presentation and unchanged earned seed64 cellar fixture. Root controls
ordinary input; original saves and both preference domains are protected.
Verify player65,8,HP30/40 and the dry sootroot at65,10 after Continue.
The fixture's last savedtick is590. Read carried WateringGrimoire if needed.
Rain is self-centered,radius3,40moist units top-up,5turn cooldown. Sootroot
requires28moist units/stage,10worldticks/unit; one cast cannot complete both
stages. Existing native cellarframes3/4/5 and products6/7 are already authored.

1. Capture actual dry baseline/reader and carried book.
2. Cast via ordinary hotbar, inspect wet seed and young state; top up before
   moisture expires. Save, quit and Continue while young.
3. Observe ripe state, harvest, pickup, product portraits and depleted bed.
4. Replant if an earned seed permits; inspect actual readiness/moisture cues.
5. Leave and return via actual stairs, save/restart as useful. Independent
   visual review; update bounded coverage and push exact owned records.

If an actual visual defect appears, name it, use actual capture for imagegen
reinterpretation, extract owner/state requirements, then native authoring and
RED/GREEN/counterchecks before integration. Otherwise preserve accepted art
and document the observed lifecycle. No unrelated newmechanics or broad reskin.
Qud influence is low-resolution readability only; no copied art/code claim.

## Honesty bounds

Actual Mac keyboard actions/captures support visible states and usability.
Savedmetadata supplies turn/health. No debugger grants, teleportation or forced
crop progress. Existing screenshots/concepts/tests do not replace this play.
No physicalDeck/Linuxexecution, performance, allcrop or wholeopening claim.
Storage is inspection-only; no reclamation.

## Actual ordinary package result

The unchanged earned seed64 checkpoint was replayed in the e49047200 Mac
package through normal keyboard controls at a 1280×800 game viewport. The 26
JPEGs are untouched native-window captures (1280×856 including window chrome).
No crop progress, inventory, position or world state was injected.

| Capture | Actual state/action |
| --- | --- |
| 01–02 | Continue at65,8; wait, approach65,9 and examine dry seed65,10. Reader says growth is paused. |
| 03–05 | Open carried watering grimoire; choose displayed Read action with Return. Close the notice and cast hotbar4. Wet seed reader says growth continues. Savedtick620, HP30. |
| 06–07 | 27 ordinary waits produce the native forked sprout. Savedtick890, same HP30. Reader agrees: sprout/moist. |
| 08–10 | Top up Rain, save at tick900; normal quit, relaunch same package, Continue. World and reader retain moist sprout. |
| 11–13 | 27 further waits; save tick1170. Fuller rooted ripe sprite, ripe reader and actual harvest action agree. |
| 14–18 | Normal hand harvest gives30XP; crop disappears. Step onto65,10 and Pick Up shows2pulp+1seed. Take all; inventory displays native pulp portrait, quantity2 and extinguishing/cooling use text. |
| 19–21 | Carried earned seed offers Plant Here at65,10. Plant; step north; reader shows new dry seed. Cast Rain and save. |
| 22–26 | Down3/Left25 to stairs40,12; ascend to glade28,19; descend; Right25/Up3 to bed. Save tick1800/HP30. Replanted crop is a sprout; reader now says dry/paused, consistent with one finite40-unit watering over the58-action trip. |

The three seeds already beneath the player at65,9 belong to the older spent
bed. They are not included in this new65,10 harvest. The ripe action menu also
offers seed-only harvesting; this pass chose normal `h) harvest`. The inventory
Read row is `a)` in this context; use the visible selected action, not a guessed
source shortcut. Hotbar4 casts immediately; an extra Return after the first cast
only reported cooldown4. Those extra UI observations are retained rather than
hidden. The exact timing assertion uses saved metadata plus ordinary action
counts; screenshots alone are not a clock or identity dump.

[Young after restart](Verification/Live16x24/packaged-cellar-growth-10-young-restarted-reader.jpg),
[ripe crop](Verification/Live16x24/packaged-cellar-growth-13-ripe-reader.jpg),
[harvested pulp in inventory](Verification/Live16x24/packaged-cellar-growth-18-pulp-selected.jpg),
[revisited dry sprout](Verification/Live16x24/packaged-cellar-growth-26-revisit-dry-sprout.jpg).

## Review, preservation and next work

Independent visual review of01–07 and08–26 found no material sprite/reader
mismatch. The existing seed/sprout/ripe/product art remains accepted. The blue
watering-grimoire glyph is a known separate fallback, not evidence that crop
art needs redrawing. No new asset, imagegen concept or runtime change was
needed; no RED/GREEN result is claimed for this acceptance-only pass.

Both preferences restored logically; isolated test Saves moved intact to the
recorded temporary protection directory. All776normal save files,374approved
art/source files and2unrelated screenshots are unchanged. Thirty importer
metadata files differed only in whitespace; backed up and restored exactly.
No source/ProjectSettings diff and no storage cleanup. Logs retain preexisting
shutdown thread-finalization and ComputeBuffer disposal notices; no runtime
exception was observed. This is not a leak/performance or physical Deck verdict.
See `packaged-cellar-growth-restored.json` and `packaged-cellar-growth-manifest.json`.

This closes the previously pending **ordinary watered sootroot lifecycle** in
the current Mac package. It does not accept every crop, biome or platform.
Next is fresh current-generated six-zone whole-scene acceptance: compare 1×/2×
arrival, service/action and departure framing before changing composition.
Keep existing native sprites stable; fix only a demonstrated scene problem.
Old saved North layouts predate newer workpad composition and must not be used
to diagnose current generation. Combat motion/defence feedback and the finite
opening fallback census remain part of that release, with regional kits next.
