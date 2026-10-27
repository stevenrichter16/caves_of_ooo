# MawToad — one missing native body

2026-10-27. **Bounded native, Editor and Mac package delivery passed.**
Source751d9247c is verified on main;305 distinct native passes, seven Editor
AFTER captures, four Mac captures/restart and independent Linux archive checks
are retained. Linux/Deck execution and whole-area/world polish remain open.
The later delivery/evidence commit is pending.
Root owns Unity, tests, integration and Git. The eight prior Stump species,
all approved sprites, legacy presentation and existing mechanics stay intact.
This is one cross-region creature gap, not complete Stump/Sodden area coverage.

## Goal, source sweep and corrections before implementation

Replace ordinary exact `MawToad`'s letter body/portrait with a compact crisp
16×24 sprite. Reuse the existing fauna and reader/claim pipeline. Qud-reference
parity is not claimed: the authority is this project's registered creature and
its actual saved gameplay. No gameplay content changes are planned.

| Source / checked premise | Verified correction and implementation constraint |
|---|---|
| Objects.json MawToad at34617 | One-cell solid Creature, `t`, layer10, HP22, AV2/DV−2, DefaultBite. Exact identity only; shared `t` is not authority. |
| EntityFactory.InitializeAnatomy at469 | No explicit Frog anatomy: defaults to Humanoid. Require legitimate owned Body, not a frog-anatomy name; do not change gameplay anatomy. |
| BoredGoal hostile scan before Staying | Wanders=false/Staying=true does not guarantee immobility. KillGoal can approach a hostile. Preserve AI and capture the undisturbed baseline before approach. Lore is not an immobility test. |
| CorpsePart / MawToad Corpse registration | Actual death creates a separate CreatureCorpse; FrogOil harvest2–4 at90%. Living sprite must not select corpse by SourceBlueprint. Corpse/product art remains separately scoped. |
| MultiCellPilot/layout.json large-maw-toad | Explicit PilotMawToad,2×2 footprint. Preserve existing multicell presenter; one-cell sprite must reject this owner. |
| Existing maw_toad.png | Original16×16, binary alpha but80 visible colors. Preserve it; author a separate crisp native16×24 body. |
| Existing fauna catalog | Five/eight argument injection must keep behavior, with explicit null for the new optional ninth texture. Reject malformed shapes independently. Cache one new pair and dispose owned Sprite/Tile only. |
| Existing submitted renderer | Damage and Wet are the real shared single Render-event color cue. Do not redispatch from readers/neighbors or invent tongue/swallow/attack-state mechanics. |

## Actual baseline, concept and parts

Source `3bafb1ee3`, fresh Mac package after the accepted canopy restart, normal
map travel RimForest2,3 → Grainfield2,2. Player40,12 HP29/40. Actual Look/examine
at24,0 identifies MawToad HP22/22 Hostile; world and portrait both remain `t`.
Captures and hashes: [baseline receipt](Verification/Live16x24/maw-toad-before-receipt.json).
Earned fixture `Fixtures/maw-toad-before76`, turn9140. No teleport/spawn/state
injection; no approach or combat acceptance yet. This geometry is unchanged.

Built-in imagegen used the actual screenshot, original MawToad and approved
opening cast contact. Exact prompts and outputs are under
`ArtSource/Live16x24/maw-toad/`: scene-concept-v1.png, parts-reference-v1.png,
concept-prompt.txt and parts-prompt.txt. The broad dark mouth, squat body and
tucked limbs are useful. Reject the concept's scene/UI alterations and the
parts output's glow, gradients, soft edge and uneven pixels. Neither is a
production sprite or an actual screenshot. Reconstruct original integer-cell
geometry; do not shrink the generated bitmap into the game.

Finite parts list: one ordinary MawToad idle body,16×24 cell, about14×13 occupied
silhouette with ground baseline row22; centered8,12 pivot,16PPU. Seven earthy
colors maximum, broad dark mouth, small low eyes, ochre/olive-brown body, tucked
limbs and sparse sand highlights. Distinct from the green small SummitSinger and
domed YellowfootWayfarer. No new optional walk/attack poses. Real damage/Wet tint
and existing removal/visibility are essential states; preserve existing motion
sources for later. Source builder and editable Blender/cells/manifest/contact
must agree exactly before Resources integration.

## Historical preimplementation plan: RED, integration and comparison

1. Record genuine failing native tests before runtime edits. Exact live body,
   world/examine identity and port outside Stump; paired shared-glyph, wrong
   reskin, foreign part/custody, hidden/stale/dead/corpse and multicell refusal.
2. Exercise real nonlethal damage and applied Wet through submitted rendering,
   checking one-event cues and neutral recovery; move/remove the actual owner
   and retain ground. Optional resource shape/failure/disposal and old overloads.
3. Add only the ninth cached body and exact selector. Import new native PNG with
   Point/Clamp/no mipmaps/uncompressed settings and required metadata. Preserve
   all original art and baseline content. Independent source/visual review.
4. Run focused GREEN and bounded related native fixtures. Re-run the earned
   baseline through GameBootstrap, actual reader/movement/state/removal where
   safely reached, whole-scene context and saved/restarted continuity. Keep
   native-tested versus visually observed states distinct.
5. Push exact reviewed source with living records; fresh package checks follow
   the existing protected build procedure. No Linux/Deck claims without play.

## Historical preimplementation review / next action

- 🟡 Before implementation: native RED and source/render verification pending.
- 🧪 Actual new body/state/reader, movement and package acceptance pending.
- ⚪ Broad sparse terrain, subtle stone blocking, glyph props/corpses and other
  missing world families remain open; no polish-ledger status change.
- Optional animation deferred. Next regional family after MawToad: Grovelands
  Shambler, Mosshulk and living merchant ChoirTendril. Plain Shambler is distinct
  from SporeShambler despite display-name similarities; do not borrow that art
  or its fungal mechanics without an exact-identity review.

Files planned: new native author/source assets, one fauna catalog extension,
finite native tests, actual verification evidence and same-commit living records.
At that preimplementation checkpoint no tests had run and no final body was integrated; the dated native and gameplay checkpoints below supersede that status.

## Native reconstruction checkpoint —2026-10-27 15:05UTC

One original native idle body is authored in
`Tools/Live16x24/build_maw_toad.py`, using named integer mesh quads in Blender.
The source directory contains `maw-toad.blend`, `source-cells.json`, manifest,
native PNG and a direct8×geometry contact on stone/green/reader backing studies.
The PNG SHA256 is `b9495996458c2a9474b580925b964f163264fbb6cc0b6ed4776cc9d5905c08a2`.
All384native pixels and73,728contact pixels match authored cells exactly;
binary alpha,7colors,141occupied pixels,22mouth pixels,14×13bounds and baseline22.
Reopened Blender source has33named editable meshes and141integer quads.

Independent visual/pixel/hash review passes for a gameplay trial. A protection
manifest count mismatch was caught and reconciled to1,639 before handoff; native
pixels stayed unchanged. Root reran the read-only pixel/hash verification.
No source raster was sampled, resized or edited by Python; Pillow only decodes
and compares finished renders. Original MawToad and eight accepted species stay
unchanged. No new animation, Unity import or runtime acceptance is claimed yet.
Actual graded stone/water, small eyes/feet, dark mouth and damage/Wet tint need
the normal-zoom game comparison after integration.


Native runner preflight: first filtered request discovered zero tests after a script-only refresh; it is not RED or GREEN evidence. Explicit new-file import/compile is required before rerunning the20-case fixture. Runtime remains unchanged.

Fixture compile preflight found missing CavesOfOoo.Data import for EntityFactory. Corrected the test namespace and strengthened both movement-ground nonnull preconditions before genuine behavioral RED. These harness mistakes are not evidence of missing production behavior.

A second fixture compile check found SpatialFootprintPart.Offsets is internal. The test now checks the public Zone.GetOccupiedCells(owner).Count after the actual footprint transaction. Both zero-discovery requests are excluded from behavioral evidence.


### Native behavioral RED —15:10UTC

The compiled20-case fixture failed20/20 before production/resource edits, on the missing dedicated resource or missing ninth constructor. Raw XML and exact test source retained as maw-toad-red.xml and maw-toad-red-tests.cs.txt. Now implementing only the ninth optional cached native cell and exact MawToad selector, preserving existing5/8 overloads and shared guards/cues.


First behavioral GREEN attempt:18/20passed; two custody tests dereferenced a correctly absent world tile. Corrected only the tests to require null owner/tile for carried/equipped cases and direct catalog refusal for every unsupported body. Runtime remained unchanged; originalREDsource and initialGREENXML retained.


### Native GREEN / frozen runtime —15:14UTC

Final focused20/20 and related285/285 native cases passed, no skips/failures:305distinct identities across two disjoint XMLs. Related set includes the old44fauna and19Summit cases unchanged. Original20RED and first18pass/2fixturefailure outputs are retained separately, not added to the final count. Imported16×24texture is Point/Clamp/RGBA32, one mip, byte-identical source. Independent runtime/import and latest fixture reviews report no unresolved blocker. No live-after acceptance until actual play below.


### Historical pre-package MawToad native and actual Editor checkpoint —2026-10-27

MawToad now has one original editable native16x24 body with305 distinct native passes and bounded actual Editor world/reader, chase, combat damage cue, F5 and fresh Continue evidence. All nine identified Stump fauna bodies are integrated/observed, not whole-biome or area-polish acceptance. Maw source push and fresh packages are pending; current delivered package remains canopy source3bafb1ee3.

One original seven-color16×24 body is reconstructed as editable integer-cell
Blender geometry, with exact native/contact/source verification and1,639 prior
art inputs preserved. Final focused20 and disjoint related285 cases pass
(305 distinct; no skips/failures). Retained genuine20RED and initial18/20
fixture-correction results are historical, not additional final passes.

Ordinary fixture76 Continue in Grainfield2,2 began at40,12 HP29/40. Reader
Maw62804 at24,0 had22/22HP. Walking around the stone ridge led to an actual
hostile approach at31,1 beside player32,1. Normal combat hit the Maw for7,
leaving15HP, and its bite dealt6 to the player, leaving23HP; capture05 visibly
records the red damage cue. Three rightward moves ended at player35,1 with
Maw34,1, and F5 retained fixture77turn9530. Fresh isolated Continue restores
those cells/health plus second Maw62805 at73,24 HP22. Seven actual captures
and [the play receipt](Verification/Live16x24/maw-toad-after-receipt.json)
record the bounded result. Saved06/restarted07 world rectangle[0,0,940,680]
is exactly639,200 decoded RGB pixels equal; sidebar/hotbar are outside the claim.
Both isolated roots were archived before Finish. Wet, death/corpse and multicell
counterchecks are native-tested only; no ordinary observation of those states
is claimed here.

Finish the reviewed MawToad source push and fresh Mac/Linux delivery, then Grovelands Shambler, Mosshulk and living merchant ChoirTendril. Continue remaining native terrain/flora/actors/props/services/crops and essential states across every biome, settlement, underground and special area. Separate serious area polish remains required after broad coverage; optional nuanced animation is deferred.
Current delivered package stays canopy source3bafb1ee3; source push and new
Maw packages remain pending. No Linux/Deck execution or sustained performance
claim. Existing55 inventory polish scopes and5 Stump area-polish records are
unchanged. Last actual hourly report15:27UTC; next16:27UTC. This is a saved
continuation, not another hourly report. The saved source/runtime/import review is [independent review](Verification/Live16x24/maw-toad-independent-review.md).

Historical pre-package self-review: no unresolved significant source/runtime/test issue after
the independent review. Actual neutral body/reader, chase, combat red cue and
saved health/position continuity pass within this route. Native-only Wet/death/
multicell checks are not visual acceptance. New source/package delivery is the
remaining immediate gate; broad missing families and serious area polish stay
open. No optional animation, AI, combat, population or collision rule changed.


**Final protection and actual hourly report —15:27UTC:** Root confirms Unity
Edit is idle (not playing/compiling/pending/active), isolated rootnull and scene
clean. Normal saves and44 foreign paths are preserved; game preference keys
are restored and standalone preferences match fully. Editor engine/test metadata
keys `PT_Run`, `unity.player_session_count`, `unity.player_sessionid` and
`unity_connect.session_id` changed as expected; full Editor preferences are
not claimed identical. Both isolated earned roots were archived before Finish.

The actual15:27UTC itemized report completed canopy deliverya6add4a20 and Maw
native/Editor verification, marked Maw push/packages in progress, and set next
goals to Grovelands Shambler/Mosshulk/living ChoirTendril and remaining regional
families. Sparse chunks, subtle stone blockers and glyph props remain polish
debt. Optional animation remains deferred. Next hourly report16:27UTC.


### MawToad packaged delivery and next actual baseline —2026-10-27

Maw source `751d9247cf05f55e0baf8f8f8cb02ded0eb931ae` is pushed and verified
on origin/main. Both fresh builds succeeded with0errors/4warnings each
(Mac57.83s, Linux45.73s). The retained final native evidence remains20focused
plus285disjoint related passes:305distinct, no failure/skip/inconclusive.
Neither packaging nor this update reran or adds to those native counts.

Actual source-labelled Mac process6617 continued fixture77 at player35,1HP23
and showed the authorized Maw reader at34,1,15/22HP. One normal Right step
and F5 retained fixture `packaged-maw-toad78`,turn9540,player36,1HP23 with
Maw35,1. After full quit and confirmed old-process absence, fresh process6949
Continued that earned state. Four actual1280×856JPEGs and the independent
review verify body/reader and bounded movement/save/restart. Saved03/restarted04
world rectangle[0,96,940,696] is exactly564,000 decoded RGB pixels equal.
The displayed7-damage hit/6-damage bite log is inherited Editor history:
**no new packaged combat, damage flash, Wet or death is claimed.**

The independently checked Linux archive contains189files/13directories/202members,
128,791,022bytes, SHA256
`517541b827fce5397700ae39b940a55ed26b0ab85072d9563a70c11f84242ac1`.
Linux and physical Deck were not executed. Restoration verifies50,883tracked
files including44foreign paths, normal saves, both preference domains and prior
builds/archive; five build-generated files were backed up and restored, and the
test session was archived. No cleanup was performed. This package restoration
is distinct from the earlier Editor-run metadata exceptions recorded above.
See [Mac play](Verification/Live16x24/packaged-maw-toad-mac-play.json),
[package review](Verification/Live16x24/packaged-maw-toad-review.md),
[archive receipt](Verification/Live16x24/packaged-maw-toad-linux-archive.json)
and [restoration](Verification/Live16x24/packaged-maw-toad-restoration.json).
The later delivery/evidence commit is still pending root's push.

Next Grovelands baseline is now actual: Grove4,7 Shambler18,6HP16 (`z`,Flesh,
Hostile), and Fen0,8 Mosshulk13,2HP55 (`M`,Hostile) plus ChoirTendril19,17HP80
(`F`,Neutral). Five new BEFORE JPEGs and earned Grove79turn9700/Fen80turn9820
fixtures retain playerHP23. These are baseline observations, not new native
actor acceptance. Follow [the Grovelands actor plan](GRAPHICS-GROVELANDS-ACTORS.md):
one original Shambler and two unchanged resource reuses, preserving committed
intent and living talk/trade. Root owns that phase's next implementation.

All nine identified Stump living bodies now have native coverage; whole regions,
other fauna/outcomes/props/crops and world coverage remain incomplete. Sparse
chunks and subtle blocking cues remain explicit debt. All55inventory polish
scopes and5Stump area-polish records stay unchanged; serious area-by-area polish
remains required. Optional nuanced animation stays deferred. Actual hourly clock
remains15:27UTC, next16:27UTC; this delivery update is not another report.
