# Manual-session image review (ongoing)

All listed original PNGs were opened through `view_image`; matching evidence JSON was read. Native PNG dimensions were checked from IHDR. Reviewed files are current October17 captures, not historical images. No image or repository edits.

Shared source/session: `manual-64`, commit `5bb8e249d1fc69d9213f684bace7a8628e11b4ad`, seed64, Unity6000.3.4f1 macOS Editor, SampleScene, CUA keyboard/mouse, no controller. Runtime settings: native3D enabled, Full detail, world scale1.0, shadows on, Full FX, CRT off. Gameplay zoom multiplier1.2, target34 rows. Each cited JSON records exact camera framing. The menu-only `nativeRing=false`/`voxel=false` values mean the fullscreen inventory suppresses world presentation; they do not show that the game has fallen back to2D outdoors.

## SPR-01 — Spread opening with HUD

`SPR-01-spawn.png`,1280×800,2026-10-17T12:59:29.9377160Z. `Overworld.11.10.0`, Spread/Hedgerow, logical(11,10),depth0; player2700 at(40,12),Classic,HP40,tick10,Dawn. Ambient0.4 with tint(1,1,.94); nativeRing/voxel active; runtime fullReveal=false.

Caption suggestion: The Classic player stands among worked crop rows and signposts, with stone walls and pale reeds to the north/east and a luminous green seam to the west. A real lined well and working notice are near the player; the small green figure is the player, not a crop. The six distinct starter-power icons and right-hand vitals/log share the frame. This asks whether useful objects and characters can be picked out against the dense green ground at ordinary camera scale.

Observed: player has brighter green edges but is small relative to the field of similarly colored vegetation. Pale reed clusters and gray wall segments have clearer silhouettes. The green vein is visually dominant. Large black horizontal margins remain above/below the fitted world; these are part of the actual game frame. Source identities include RepairLinedWell2695 at(41,8), working notice2697 at(41,10), Warden2079 at(36,16), Villager2086 at(39,20), and several marlbacks. Their exact roles cannot be inferred from tiny shapes alone. This frame is not a close-up actor comparison.

Visibility explanation from source review: ZoneRenderer.cs:47 defaults FovRadius to999. Full-reveal is off, but default range is effectively zone-wide and walls/line-of-sight provide occlusion; do not describe this as a conventional small-radius fog circle. The operator is checking the runtime radius separately. The previously enabled reveal may also have left explored-memory cells, which these stills cannot distinguish without cell-level evidence.

## SPR-02 — A contrasting flower meadow

`SPR-02-flower-meadow.png`,1280×800,2026-10-17T13:00:58.7498730Z. `Overworld.12.10.0`,Spread/FlowerMeadow,logical(12,10),depth0; same player at(40,12),HP40,tick50,Dawn. Same graphics/camera multiplier and ambient; nativeRing/voxel active; fullReveal=false.

Caption suggestion: The player has reached a second Spread chunk with bands of pale charm-flowers, more open ground and a hedge pen below/right. The pen contains a real reedback grazer; the generated crop rows beside it are ripe emberwheat. Steam vents and copper pipes lie upper-left. This is a different generated formation in the same biome, useful for judging whether the scenery varies while retaining a common palette.

Observed: open central ground distinguishes this composition from SPR-01, although the repetitive green ground specks remain prominent. The pen layout reads clearly; the tiny inhabitant and the two crop rows need contextual labeling to distinguish their purposes. Some isolated low objects read as simple colored blocks at this scale. Do not infer their precise materials from the pixels. Five SteamVent owners at(29,7),(30,7),(31,7),(31,6),(32,6),and four CopperPipe owners at(29,3..6) support identifying the upper-left installation. FrontierPenGate7115 at(53,17),ReedbackGrazer7123 at(51,17),RipeCropRow7124/7125 at(55,17)/(55,19). The screenshot does not establish whether the vents are mechanically dangerous at this moment; there is no intensity/age evidence here.

Limits: this is an instrument-guided manual review journey, not proof of unaided discovery. Confirm route history from the operator log. Metadata includes distant creatures; source-default FOV radius999 means this is not a limited sight-radius demonstration. Full-reveal is off; actual line-of-sight and explored-memory evidence should be used for any stronger claim.

## ITEM-01 — Actual equipment, empty body slot selected

`ITEM-01-equipment.png`,1280×800,2026-10-17T13:01:23.9141250Z. InventoryOpen above the same flower meadow,player(40,12),tick50; no turn change. Equipment tab is highlighted. Actual equipped Dagger entity2704 occupies the left hand; body is selected and empty. The right list totals six items/stacks including the equipped dagger, two dried meat,two healing tonic,a watering grimoire,four gladroot seeds and two emberwheat seeds. Top totals show weight12/270 and currency50.

Caption suggestion: This is the Classic character's real equipment screen before changing its loadout. The left-hand dagger appears as a small cyan slash inside the body diagram, with1d4/1PV below; the empty right hand has1d2. The same dagger's name,weight and price appear in the list. This tests whether the old tile-based equipment view explains ownership and material as clearly as the3D world.

Observed: tab labels,item names,stats and category separators are legible. The thin green-gray outlines and empty-slot labels are subdued but readable against the black background. The rightmost equipped marker on the dagger row visibly ends at `[E` near the image edge; its expected closing bracket is not visible. Treat as apparent marker clipping pending source/alternate-resolution check. At this sample's small inventory size,most of the panel is empty; it does not test scrolling or large inventories. A cyan slash in the doll is not proof of a visible3D equipped model.

## ITEM-02 — Inventory tab with the same dagger

`ITEM-02-inventory.png`,1280×800,2026-10-17T13:02:35.9278890Z. Same player/item identities,inventory and tick50. Inventory tab is highlighted; no new items or equipment changes occurred between ITEM-01 and ITEM-02.

Caption suggestion: The same six-item loadout is now shown with Inventory selected. The footer changes to Find,Drop,Actions and neighboring-tab controls,while the dagger remains highlighted in cyan on the list. Compare ITEM-01 to assess whether the tab change gives enough visual feedback while retaining shared information.

Observed: all footer actions fit and are clearer than the small popups' terrain-backed hints in BASE images. Top totals and names are visible. The apparent right-edge `[E` truncation persists. The inventory and equipment tabs differ mainly in highlighted tab,selected body outline and footer; their overall layout is otherwise the same. No Examine panel is shown yet, so this is a list/doll view rather than a complete item-description or world/inventory/equipped comparison.

## Review ledger

Opened and checked: SPR-01-spawn;SPR-02-flower-meadow;ITEM-01-equipment;ITEM-02-inventory. All1280×800,nonblack,distinct frames. BASE images were separately reviewed in `base-review.md`.

## ITEM-03 — Dagger examination at800p

`ITEM-03-dagger-examine.png`,1280×800,2026-10-17T13:06:17.7279950Z. AnnouncementOpen, same flower meadow and player(40,12),tick50. Equipped Dagger2704 remains the same object as ITEM-01/02.

Caption suggestion: Examining the player's equipped dagger opens the dark field-ledger reader over the inventory. A light-colored3D blade portrait appears upper-right. The text says the equipment is sound; it describes1d4 damage per penetration,+1 penetration bonus,a Strength cap3,Piercing attributes,10% Confused for2 turns on damaging hits,and a hand slot. Material is Steel,composition Metal,weight4lb. These are current displayed mechanics,not inferred from the portrait.

Observed: the larger art makes the blade and handle recognizable in a way the equipment doll's cyan slash does not. All text and footer fit. Section headings and material labels distinguish current condition,mechanics and composition. Dense mechanic lines are tightly spaced,especially the wrapped status sentence; this is readable but less relaxed than the short descriptive paragraph. The slash in the reader's1/1 page display renders correctly; this uses a different text path from the suspected journal punctuation issue. Inventory remains visible around the opaque panel and its actions footer overlaps the lower surrounding area without hiding the reader's controls. Enter closes and Esc backs out,explicitly without advancing a turn.

## ITEM-04 — Matched dagger examination at720p

`ITEM-04-dagger-examine-720.png`,1280×720,2026-10-17T13:06:32.1996430Z. Same exact item,character,tick50 and text as ITEM-03; output height is the explicit override.

Caption suggestion: This is the same dagger reader at the shorter handheld check size. The complete single page,portrait,material table and close/back controls remain visible. Compare ITEM-03 directly for density and character shapes rather than treating this as a different item.

Observed: no reader content is clipped or missing at720p. The type is smaller vertically and mechanic lines remain tight,but the selected yellow item name,green section headings and white body stay distinguishable. This is a keyboard/mouse capture; it does not test Deck buttons or touchscreen comfort. No additional page is available for this short description.

## ITEM-05 — The same dagger dropped on the ground

`ITEM-05-dagger-ground.png`,1280×800,2026-10-17T13:07:17.9089840Z. Flower meadow;player2700 at(42,12),tick70,Dawn,HP40. No equipped items. Actual Dagger2704 is on tile(40,12),two cells west of the player,confirmed by metadata. Log records unequipping and dropping the dagger. Camera follows the moved player at the same zoom.

Caption suggestion: After the ordinary unequip/drop actions,the player walked two cells east so the same steel dagger could be judged on the ground. Its tiny,low-profile shape lies to the left of the green player. The examine portrait and inventory slash in ITEM-01/03 refer to this exact entity2704;no replacement item or raised display prop was created.

Observed: the dropped dagger is difficult to pick out from the dark green ground specks at this scale. Its identification relies on its known tile and the log,whereas the examine portrait is immediately blade-like. Preserve this original as evidence of the actual low profile; do not present an enlarged derived crop as the ordinary view. The log carries the entire prior description and drop notices,leaving little blank space. The world has not failed to render; it is a visibility/contrast question for a small object. This frame does not demonstrate an equipped3D representation; a separate re-equipped owner capture is still needed.

Additional reviewed files: ITEM-03-dagger-examine1280×800;ITEM-04-dagger-examine-7201280×720;ITEM-05-dagger-ground1280×800. Each original opened and metadata checked;all nonblack,distinct.

### Source-supported inventory marker explanation

The missing final bracket in ITEM-01/02 is supported by the row layout: `InventoryUI.cs:1840` appends ` [E]`; the row reserves a49-column string (`RIGHT_W=49`,line33) but draws it starting at`RIGHT_START+1=32` (2499/2503),while `DrawText` stops at`x+i<80` (4248). A normal equipped row has33 padded name columns+7 weight columns+5 value columns+4 equipped-marker columns=49. Its last character lands atcolumn80 and is discarded. This is a source-supported one-character row overflow,not an output PNG crop. No fix made.

## ITEM-06 — Dagger recovered and equipped on its original owner

`ITEM-06-dagger-owner.png`,1280×800,2026-10-17T13:08:43.9202430Z. Flower meadow,player2700 back at(40,12),tick100,Dawn,HP40. Dagger2704 is again equipped in slot331;log records actual pickup and equip. Related to ITEM-01..05,with no substitute weapon.

Caption suggestion: The player has returned to the dropped dagger and re-equipped it. This is the ordinary camera view of its actual owner. At this distance the actor is a narrow green silhouette; the pale blade is not separately easy to identify,even though the log and same-entity equipment record confirm it is equipped. Compare its large clear portrait in ITEM-03 with this real scale.

Limits: the pixels cannot by themselves establish whether an equipment attachment exists and is tiny/occluded or whether it has no representation. Use the presenter's read-only `TryGetEquipmentView`/fallback metadata before making that distinction. The image alone demonstrates weak recognition,not a proven missing-model defect.

## MAP-01 — Player at Drowned Ledger, Look cursor on its western neighbor

`MAP-01-drowned-ledger.png`,1280×800,2026-10-17T13:09:33.4114650Z. Current zoneWorldMap,LookMode,player raw map tile(47,8),logicalworld(17,5),DrownedLedger destination,Height,tick310;FovRadius999,fullRevealoff. Glyph/tile world map,not native3D.

Caption suggestion: The map is presented as a compact colored glyph grid within the game viewport. The green player stands on the Drowned Ledger destination,while a pale square Look cursor is one tile west at raw(46,8). The Focus sidebar consequently says “the Sodden,”not the Drowned Ledger. This is useful evidence of the difference between the player's position and an inspection selection. Descending would use the player's occupied cell(47,8),logical(17,5),not the Look cursor.

Observed: no label in the map grid spells out Drowned Ledger; location recognition relies on focus/context and visited symbols. The map occupies a relatively small portion of the available world region,with large gray/black margins. Similar muted green and teal symbols make the selected white square the strongest location cue. No map legend is visible. This is a real map visualization rather than a failed3D draw.

## SOD-01 — Actual descent into the Drowned Ledger

`SOD-01-drowned-ledger-arrival.png`,1280×800,2026-10-17T13:10:15.8475420Z. Reopened and reviewed after the operator excluded the premature map frame. Current zone`Overworld.17.5.0`,Sodden/ReedMaze,POI“the Drowned Ledger”,Palimpsest,tier2,ExcavationCamp profile. Player2700 at(39,11),tick320,Height,HP40. NativeRing/voxel active,fullRevealfalse,FovRadius999. Ambient0.4,tint(.82,.86,.8);same zoom1.2 and full-quality settings.

Caption suggestion: This is the first actual ground view after using the ordinary world-map descent onto the Drowned Ledger. The player arrives near a square well,with plank walkways,survey stakes,raised peat/sandstone edges,tents and scattered reeds. Black areas are current unseen/occluded parts of the site,not an image crop. A Curation sorter is among the visible inhabitants. The review question is whether these working surfaces and objects identify an excavation/recording camp before its name is read.

Observed: the planks provide a clear warm-colored route against the dark green floor. Raised walls and tent boundaries establish the space,but the large flat green surfaces and low objects are hard to assign specific working roles without inspection. The well is a readable square shape beside the player;fine tools/records are not obvious at arrival scale. Regional mood is darker and wetter than the Spread flower field,but retained green tones and tiny actor scale mean the color family alone does not make this a recognizable named settlement. Caption should not promise an exposed recording area until the closer working-area frame exists.

Evidence lists48Duckboards,28PeatBanks,24SandstoneWalls,22TentWalls,3SurveyStakes,twoPreFellingBodies and CurationSorter9676,“Halvra Sorre,Curation sorter”,at(21,11). Those counts are visible-entity metadata,not all necessarily inside the camera's crop. Player movement/arrival is natural input;this is a review-guided destination,not proof of unaided discovery. The opening log still contains previous dagger interaction text rather than a prominent destination banner.

An earlier premature map frame was archived by the operator as`evidence/excluded-early-map-*` and is excluded from the final screenshot set. It was inspected only to detect the capture race;it does not count as Sodden coverage.

Additional opened originals: ITEM-06,MAP-01,finalSOD-01;all1280×800/nonblack. RuntimeFov999 is now confirmed in MAP-01/finalSOD-01metadata.

## SOD-02 — Excavation work ground between tent and wet pits

`SOD-02-drowned-ledger-work.png`,1280×800,2026-10-17T13:10:59.2891480Z. DrownedLedger`Overworld.17.5.0`,player(22,11),tick490,Height,HP40. Same native3D/full quality/ambient. Full-revealoff,radius999.

Caption suggestion: The player has walked west from arrival toward the camp's working ground. An open gap leads into the reading tent to the north. Plank crossings,peat banks,survey stakes and a preserved body border the wet excavation spaces below. Curation sorter Halvra Sorre is now at(25,15). The frame asks whether the relationship between workers,remains and excavation is understandable before reading labels.

Observed: warm planks and straight borders read as human construction,but the body beside the small western plank and the thin stakes are much subtler than the large empty pits. At this distance the pale body can be mistaken for another small ground object. The tent interior is mostly occluded from the outside,which is appropriate to the player's current line-of-sight;do not compare it to SOD-03 as an object disappearance. This is the same site at a later tick after actual movement,not a fixed-camera screenshot comparison. No tool handling/worker animation can be judged from the still. Sources: preserved body9667 at(14,15);survey stakes9669(5,16),9670(33,17),9671(58,13);campfire9680(28,20);well9679(40,12).

## SOD-03 — Inside the reading tent

`SOD-03-reading-tent.png`,1280×800,2026-10-17T13:12:02.5598800Z. Same DrownedLedger zone,player(17,7),tick580,Height,HP40. Native3D/full quality remains;radius999/full-revealoff. Actual line-of-sight now exposes the tent interior and leaves outside regions dim/hidden.

Caption suggestion: Inside the tent,the player stands beside a reading table and a preserved body. Three crates sit against the left wall,a chair stands to the right,and Recension scribe Calven Vell is by the northern side. This is the more intimate working-area counterpart to SOD-01 arrival: records,stored material,staff and a body share an inhabited room rather than being arranged into a review gallery.

Observed: the warm brown crate group and the enclosed rectangular plan are recognizable. The reading table's little raised page/slat form distinguishes it from the chair. The pale body and scribe remain small,and their relationship is clearer with the supplied caption than on first glance. Character roles cannot confidently be named from silhouettes alone. Black/very dim exterior patches contrast with the newly visible room;this is not full reveal. Identity: ReadingTable9672 at(16,5),PreFellingBody9666 at(16,6),RecensionScribe9673 at(21,4),Chair7804 at(23,7),crates7526/7528/7615 at(10,4)/(11,4)/(10,5). These are actual generated owners;no rearrangement is stated in the operator's manual route.

Additional opened originals:SOD-02/SOD-03,both1280×800,nonblack and distinct.

## UI-01 — Recension scribe's distinction between bodies and readings

`UI-01-recension-dialogue.png`,1280×800,2026-10-17T13:13:20.3721610Z. DrownedLedger readingtent,player(20,7),tick610,Dusk,HP40;DialogueOpen;keyboard/mouse,no controller.

Caption suggestion: Calven Vell,Recension scribe,says that a reading is entered as the expedition's assertion,never the body's. The selected response is “Noted,as claimed,”with Farewell and Attack below. The physical tent,reading table and body remain visible behind the compact translucent conversation panel,connecting the lore with the working space shown in SOD-03.

Observed: full speaker name and role fit across the yellow title;neutral attitude is readable. The left-hand faction label truncates to“Recensio”without an ellipsis in its narrow portrait column. The speaker is represented by a cyan glyph,not the3D body portrait used in the newer Examine reader. Speech and choices fit in the popup;cyan Talk/red Attack give quick distinctions. The dark gray keyboard-control footer is again outside the opaque/readable panel and blends with world detail. This short branch is not a long/paged-dialogue stress test. Text correctness beyond the captured branch is unassessed.

## UI-02 — Matched Recension conversation at720p

`UI-02-recension-dialogue-720.png`,1280×720,2026-10-17T13:13:31.9888240Z. Same NPC,branch,player position,tick610 and inputmethod as UI-01. Outputheight is the only intended comparative override;the world camera/hotbar adapt to the shorter frame.

Caption suggestion: The same conversation remains fully displayed at720p. Name,speech and three choices fit,while the persistent right log and six-power hotbar continue to occupy their normal areas. This checks whether a real conversation remains readable without abandoning the world context.

Observed: no dialogue speech/choice clipping. The left faction label remains truncated and the footer remains weak in contrast. Shorter screen height compresses world/HUD elements but preserves the next action. This is a desktop keyboard capture at a Deck-relevant resolution,not Deckhardware testing. Paired with UI-01,not a second sampled NPC.

Additional opened originals:UI-01/02,1280×800/1280×720 respectively,nonblack,matching text and tick.

## SOD-04 — Open mire and readable dry routes

`SOD-04-open-mire.png`, 1280×800, 2026-10-17T13:14:34.6776670Z. `Overworld.16.6.0`, Sodden / OpenMire, no POI. Player (40,12), tick 670, Dusk, HP 40. Source-default FOV radius 999, runtime full reveal off, full-quality settings unchanged.

Caption suggestion: Broad blue water patches divide a dark green mire, leaving crossing dry ground around the player. Reeds line the margins and several low vents sit near the bottom of the frame. This is a wilderness counterpart to the Drowned Ledger's engineered plank routes.

Observed: water contrasts clearly with the dry paths; the crossing structure is understandable at the normal camera. Tiny plants and ground specks give texture without competing as strongly as the pools. The player's green silhouette remains small. The record includes 614 MirePool cells, 50 Reeds, four SteamVents, three Reedfrogs and one Bandfrog, but creature records can be outside the camera crop and are not proof that a reviewer can identify them in this image. This still does not demonstrate pool depth, damage, traversability or vent timing. No mechanical effect should be inferred merely from blue color.

## GRO-01 — Fruiting wall wilderness

`GRO-01-fruiting-wall.png`, 1280×800, 2026-10-17T13:15:09.7028620Z. `Overworld.7.5.0`, Grovelands / FruitingWall, no POI. Player (40,12), tick 890, Dusk, HP 40. Full-quality settings and nativeRing/voxel flags remain true.

Caption suggestion: Round-canopy trees, pale vertical vine/fungal forms and low purple-red growth border an open central route. The cyan-green player stands in a visibly stepped square pool of light. A Shambler is in the lower-left area; small woven baskets offer signs of usable objects among the growth.

Observed: cyan player against olive floor is easier to isolate than the tiny green player against detailed Spread grass. The treatment changes substantially from the Spread and Sodden images: broad, largely flat ground color; simpler silhouettes; stronger square lighting bands; fewer fine surface details. Metadata that native rendering is enabled is not sufficient to prove that every object has a native 3D replacement. The appearance difference is real and merits diagnosis outside this evidence-only assignment. Same Dusk band as SOD-04 helps the comparison, although biome tint differs. Source records include 68 VineWalls, 49 Trees, 45 Bushes, 39 FruitingBodies, Shambler 14889 at (22,18), and baskets at (42,8)/(20,13).

## SET-01 — Cinderhold road junction

`SET-01-cinderhold.png`, 1280×800, 2026-10-17T13:15:36.1275950Z. `Overworld.6.6.0`, Grovelands / TendrilFen, POI Cinderhold, SaccharineConcord, tier 3, PruningPost profile. Player (39,11), tick 950, Dark, HP 40.

Caption suggestion: Broad stone roads meet among enclosed rooms. Two orange campfires and a square well establish inhabited infrastructure around the player, while walls occlude rooms at the edges. This settlement uses a cross-road plan distinct from the Drowned Ledger's wet plank camp.

Observed: routes and large room boundaries read clearly, but small props do little at this camera scale to explain that this is a pruning post. The stepped illumination around the player is conspicuous. The road and ground are flat gray/olive compared with the detailed Spread/Sodden treatment; nativeRing/voxel remain true in metadata. The scene is at Dark rather than the earlier camp's Height, so lighting/color differences cannot all be attributed to art style. Merchant Darret Pennel (20,18) and the scribe (61,20) are in source records, largely outside the central view. Records also show a notice board, watch lantern, chest and well; do not promise all of those are readable in the image.

## STU-01 — Stump pools, ledges and stone islands

`STU-01-cascade-gorge.png`, 1280×800, 2026-10-17T13:16:04.8150860Z. `Overworld.5.5.0`, Stump; generic formation metadata says `None`, no POI. Player (40,12), tick 1010, Dark, HP 40. The operator separately read the specialized formation as `CascadeGorge`; retain both that value and the generic JSON field rather than treating `None` as absence of generated form.

Caption suggestion: Beige-gray stone islands and crenellated walls enclose broad teal spray pools. Ledges create a narrow passage toward the lower-right. The player stands in the central water area, with small green creatures to the right. The Focus panel reads “pink stone, spray pool, You” and water on the ground.

Observed: water versus land is easy to read, and the enclosed pools/ledges give this frame a different layout from the preceding regions. The teal player is less distinct against teal water than against Grovelands olive ground. The terrain is comparatively flat and simply shaded; the native-renderer flags alone do not establish model coverage. Records include 986 TepuiStone cells, 275 SprayPool, 97 TepuiWall and 14 DescentLedge, CascadeFather 19936 at (48,13) and GlasspaneFrog 19938 at (54,19). Exact creature silhouettes should be cross-checked before labeling them in a crop. The still does not prove danger, movement cost or water interaction.

## OVE-01 — Sparse Overwrit ground

`OVE-01-pilgrimage-rim.png`, 1280×800, 2026-10-17T13:16:31.2998160Z. `Overworld.3.10.0`, Overwrit; generic formation `None`, no POI. Player (40,12), tick 1170, Dark, HP 40. Full-quality/nativeRing/voxel enabled, runtime full reveal off, FOV radius 999.

Caption suggestion: The player stands on an almost uninterrupted olive-green expanse of scraped ground. A few tiny marks and a stepped square light gradient are the only visible detail in the central world view. This preserves the current visual sparsity rather than framing around it.

Observed: this is nonblack and is not a failed PNG, but nearly all of the viewport lacks recognizable landmarks, vegetation or terrain variation. The metadata lists 2,000 OverwritGround, 31 OverwritNewGrowth and one StairsDown; it does not imply that all new-growth cells or stairs lie in this camera crop. The visual result is useful negative evidence about regional specificity and environmental readability. The operator separately read the specialized formation as `PilgrimageRim`; the plain generic formation field does not supply this value. The sparse pixels alone do not communicate that name. The HUD remains readable and the six initial spell icons are unchanged.

## BEA-01 — Salt-pan camp at Dawn

`BEA-01-salt-pan.png`, 1280×800, 2026-10-17T13:16:57.9084120Z. `Overworld.1.14.0`, Beating / SaltPan, no named POI. Player (40,12), tick 1310, Dawn, HP 40. Source-default camera and full-quality settings, nativeRing/voxel true.

Caption suggestion: Sparse pale salt formations rise from broad yellow-olive sand. An open camp and two grid-like tent structures sit north of the player; a well and small figures provide signs of human occupation. A dark teal brine patch lies just south of the player's light pool.

Observed: the sparse desert/camp layout differs from green regions, but the dominant flat yellow-green floor and simple glyph-like props share the less detailed treatment seen in Grovelands/Stump/Overwrit. Hard-edged square lighting remains noticeable even at Dawn. Metadata identifies tent-right host Sennel Clothward (40,9), a wayfaring merchant (44,8), a scorpion (41,1), two ambush bandits farther west, a trap and chest. Several are outside or obscured in this camera crop. Do not claim the bear trap is visible or that a fight has occurred. The right log still contains prior autosaves and unsuccessful pickup messages rather than current camp events; those are authentic session history.

Additional originals opened: SOD-04, GRO-01, SET-01, STU-01, OVE-01 and BEA-01, all 1280×800, nonblack and visually distinct. These are ordinary-input regional route captures; site selection was guided by review needs. No art/rendering fixes were made for the images.


## Source audit of visible readability issues (read-only)

The evidence task made no production fixes. These source paths explain observed pixels or identify a still-pending verification:

- **Equipped marker clipping — observed ITEM-01/02.** `Assets/Scripts/Presentation/UI/InventoryUI.cs:33` reserves a 49-column right panel. At line 1840 the marker is ` [E]`; lines 1844–1853 build a 49-character equipped row from a 33-character name field, seven weight characters, five value characters and four marker characters. The row starts at column 32 (`RIGHT_START + 1`, lines 2499/2503), while line 4248 clips at column 80. The final bracket is discarded. This is not screenshot cropping.
- **Faction truncation — observed UI-01/02.** `Assets/Scripts/Presentation/UI/DialogueUI.cs:31` sets portrait width 10, giving eight content columns after borders. Lines 595–622 take `Substring(0, maxFacLen)` without an ellipsis. `Recension` has nine characters, so the visible label becomes `Recensio`. The nearby comment claiming it fits is incorrect. This is the faction label, not the speaker's full role/title.
- **Terrain-backed dark footers — observed BASE-02/03/04 and UI-01/02.** `StartingBuildMenuUI.cs:100` fills through `BORDER_H`, and lines 127–129 draw DarkGray instructions on the next row. `PauseMenuUI.cs:176–182` explicitly puts the hint outside the border. `DialogueUI.cs:640–646` draws the actions string in DarkGray after the bordered content. The result is weak contrast over detailed terrain; no input failure was observed from this.
- **Both device guides at startup — observed BASE-01.** `BootMenuController.cs:54–55` unconditionally logs keyboard and controller New Game/Continue instructions. `NativeGamepadInput.cs:67` defines connection using a live added Gamepad. The manual-session metadata reports no gamepad, so both strings do not prove hardware connection.
- **Quest current marker and progress slash — source defect candidate, actual journal capture still pending at this stage.** `QuestLogUI.cs:70` uses `>` for current, line 212 uses `/` for progress, and line 269 calls `CP437TilesetGenerator.GetTextTile`. That atlas is cleared transparent and allocates all 256 tiles (generator lines 758–780), but only paints the characters in `GetFontData` (lines 793–801 and 833–930). The dictionary omits `>` and `/`, so their tiles are transparent despite existing in the cache; the question-mark fallback is not taken. Do not claim the marker was visibly verified until a real quest with a current stage/progress is captured. The dagger reader's visible slash is a different UI/font path and does not clear this concern.
- **Regional renderer flags are not a model inventory.** The simplified appearance of GRO-01, SET-01, STU-01, OVE-01 and BEA-01 is visible despite nativeRing/voxel enabled metadata. Do not call those flags proof of coverage or diagnose old 2D fallback from the stills alone. Further read-only presenter/model coverage evidence belongs in the final audit if available.

Corrected specialized formations: STU-01 = `CascadeGorge`, OVE-01 = `PilgrimageRim`, explicitly read by the operator; their generic metadata field remains `None`. The manual JSON fragment contains all 24 ordinary-folder PNGs, including clearly classified baseline-menu/full-reveal supplements, and references raw evidence instead of duplicating entity lists.
