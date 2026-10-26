# Graphics-focused run: complete change inventory through 2026-10-26

Scope: the overnight multi-pass graphics request, beginning with `55ac6069a`
on October 22, through `4e8b21e23` and the hotbar-name correction committed
with this report. This is a change inventory, not a claim that graphics are
finished. Repeated revisions are grouped by their final user-visible result.
The chronological commit appendix preserves the individual iterations.

Three independent read-only audits covered world art, UI/controls and actual
gameplay changes. Commit history and living verification records are the
sources. Existing mechanics demonstrated in new art are separated below
from mechanics or behavior actually changed during this run.

## Delivery and reporting status

- Ordinary fresh/Continue games now default to flat 16×24. The old presentation
  remains a reversible Graphics choice. Uncovered regions retain truthful
  flat glyphs, so flat continuity does not imply complete bespoke artwork.
- Current Mac and Linux packages now use source `3aca7c18e`, including the
  action-menu fixes and En / Garde name wrap. Ten actual Mac captures verify
  hotbar input, inventory and full restart. Linux archive checks are verified;
  Linux gameplay and physical Steam Deck acceptance remain open.
- The hotbar source's93/93 native passes and six Editor captures are retained.
  A subsequent32-capture earned resin→still→forge route passed26 independent
  saved-state checks and full restart. This exercises existing mechanics;
  it adds no new alchemy/forge rule or sprite. The shared native brew flask
  was audited and kept unchanged. See [delivery](GRAPHICS-G3-HOTBAR-DELIVERY.md)
  and [route](GRAPHICS-G3-EARNED-RESIN-TEMPER.md).
- The user requested hourly itemized work and goals. The active automation now
  requires completed/in-progress/blocked items, next-session goals, tests,
  screenshots and push status once per hour. Its intermediate ten-minute
  storage inspections stay quiet and read-only unless something material changes.

## 1. Production pipeline, references and preserved study

1. Built the separate Seed Keeper clearing study and modular kit: broken wall,
   clay oven, drying rack, emberwheat, two fence orientations, firewood, grass,
   marlroot, seed sack, sorting table, well, stool, striped seed shelter,
   tilled bed and basket. Preserved editable Blender/greybox sources, scene
   concepts, extracted parts, native sprites, prefabs and atlas. This is a
   **static study**, not evidence that those study-only props became gameplay.
2. Curated eight external references covering Qud spatial readability, earthen
   ovens, seed storage, seedheads, sundew forms, salt crust, wetland roots and
   reclaimed machinery. Recorded source/credit and translation notes; reference
   photos were not copied into game art.
3. Established the real-game greybox → actual capture → imagegen interpretation
   → parts/state mapping → editable native sprites/animation → integration →
   playtest/profile/comparison cycle. The proposed 3D-generator stage became
   native 2D production. Concepts are labeled separately from screenshots.
4. Established graphics as the release blocker, a six-zone opening scope,
   regional follow-up, owner/state coverage ledger, restoration records and
   continuing agent handoffs. These are delivery controls, not completed art.

Anchors: `55ac6069a`, `103eb4977`, `7215c448b`, `bd1315dec`, `413c84a71`;
[art direction](LIVE-16X24-ART-DIRECTION.md),
[reference library](ART-REFERENCE-LIBRARY.md),
[completion plan](GRAPHICS-COMPLETION-PLAN.md).

## 2. Live world rendering and controls

5. Unified terrain, actor feet, camera, lights, cursor, targeting and effects
   around rectangular world-cell projection. Corrected square-cell assumptions,
   stale aspect, undersampling, fractional framing and pixel-boundary alignment.
6. Integrated into SampleScene/GameBootstrap and real entities/controls, with
   an isolated playable greybox rather than a second demonstration simulation.
7. Added native catalogs, shared atlases/cached tiles, static-frame support,
   bounded resource ownership and current visible/remembered owner rules.
   Unsupported identities use their actual glyph/color, not a misleading sprite.
8. Made flat mode the normal launch/Continue default and persistent Graphics
   choice, with recovery of the previous 3D settings. Travel, load and return
   retain projection. Fullscreen UI temporarily uses square geometry and
   restores the rectangular world/camera on return.
9. Added reachable integer 1×–4× native zoom using PageUp/PageDown and the
   LT+D-pad control route. Modal paging remains separate; zoom takes no turn.
   Actual keyboard/Mac use is verified; physical controller feel is not.

Anchors: `6f62c4734`, `b5dc1b3f3`, `5eaddc1ac`, `1644aea51`, `47d678995`;
[renderer](GRAPHICS-G1-RENDERER.md), [launch](GRAPHICS-G1-LAUNCH.md),
[zoom](GRAPHICS-G3-PLAYABLE-ZOOM.md).

## 3. Opening glade and animation

10. Added native player and Warden bodies; distinct Villager, PetDog,
    MarlbackScrabbler and MarlbackGleaner silhouettes; dark ruin, low walls,
    rubble/columns, tall/short reeds, grass-bush, ripe wheat and spent stubble.
11. Added functional landmarks: cracked/sealed repair well, filled/empty basin,
    cellar steps, haulable beam, chest, barrel, sign, mushroom ring, quartz vein
    and lit masonry.
12. Added state art: buckled/closed/open wicket, cold/hot-coal/burning fire,
    seed/sprout/ripe Drawgourd, unlit/lit torch and loose waterskin. Repaint now
    follows actual stationary torch/fuel/wet/frozen/burning changes.
13. Added sixteen action-pose derivatives across player, dog, Scrabbler and
    Gleaner: two walking poses, melee and hurt for each. Pooled integration
    follows actual actions, fog, removal and zone transitions. This is not
    complete directional animation for every actor.
14. Added 48 connected masonry frames: sixteen cardinal masks each for low,
    dark and lit walls. Actual disclosed topology controls joins/end caps.
15. Added sixteen water-bank masks, two road variants, old-field earth and
    dry/wet prepared furrows. Harvested rows expose prepared beds correctly;
    hazards, mixed liquids and unsupported states keep truthful fallback.
16. Reworked glade grass/reeds into unequal drifts with holes and quiet field/
    ruin approaches, preserving fixed discoveries, routes and dressing budgets.
    Rejected intermediate overly regular or solid clusters after actual play.

Anchors: `510f7cea8`, `724d4e079`, `e8bfb946f`, `c3df1e330`, `4f50de3ab`,
`4f4fb0167`, `e6b6609f1`, `ee3c90abe`, `37bbc54f4`, `db3369cbf`;
[motion](LIVE-16X24-MOTION.md), [boundaries](GRAPHICS-G2-BOUNDARIES.md),
[ground](GRAPHICS-G2-GROUND.md), [composition](GRAPHICS-G3-GLADE-COMPOSITION.md).

## 4. Item, equipment and remains artwork

17. Opening goods: Emberwheat, ToastedEmberwheat, DrawgourdShell, DrawgourdSeed,
    Mushroom, RoastedMushroom, GlowQuartz, HealingTonic, DriedMeat, FireClay,
    SalvagedTimber and MarlbackCorpse.
18. Plot products: CandyCarrot/Gladroot, CandyCarrotSeed, EmberwheatSeed,
    MarlrootClod, MarlrootSeed, PitchpodResin, PitchpodSeed and InertSludge.
19. Northern goods: StoneburrSeed, FrostLichen, MendleafSprig, WildBerries,
    GoldCoin, BrewedTonic and ConcentratedMendleaf.
20. Equipment: LongSword, Hatchet and LeatherArmor, without fictitious
    enchantments baked into the pixels.
21. Aftermath: Magpie corpse, Viper corpse and VenomGland. Low fallen poses
    distinguish remains; selected pile-owner priority is retained.
22. Ground/carried/equipped/Examine art shares the exact subject and state;
    pickup/drop, custody, removal and save/load do not substitute another item.

Anchors: `4f50de3ab`, `4f4fb0167`, `0fdcec658`, `e9a4739c7`, `f05427044`,
`8758d67dc`, `f5e7c3d79`; [loose items](LIVE-16X24-LOOSE-ITEMS.md),
[plot products](LIVE-16X24-PLOT-PRODUCTS.md),
[works products](LIVE-16X24-WORKS-PRODUCTS.md),
[aftermath](GRAPHICS-G3-OPENING-AFTERMATH.md).

## 5. Connected cellar

23. Added native upstairs, crate, timber pallet, StoneWall, Sootroot seed/
    sprout/ripe, SootrootPulp and SootrootSeed in the real cellar.
24. Added four quiet native stone-floor variants while retaining actual soil,
    coating and object authority. Actual loot, pallet dismantle, crop harvest,
    depleted states, stairs, growth, save/restart and revisit were exercised.

Anchors: `e332cf040`, `8312e1d7b`, `4b61c630b`;
[cellar](LIVE-16X24-CELLAR.md), [floor](GRAPHICS-G3-CELLAR-FLOOR.md),
[growth evidence](GRAPHICS-G3-CELLAR-GROWTH.md).

## 6. North works and Nella's reserve

25. Admitted the actual North works and Nella graphs to native presentation.
    Added Tree, Hedge, BerryBush and DryBrush; reused approved Bush/Reeds.
26. Added AlchemyStill, AlchemyShelf, StoneburrPatch, FrostLichenPatch,
    MendleafPlant, Magpie, Viper and Patchbearer with/without its healing slot.
27. Broke paired hedge bands into unequal remnants; placed the actual still/
    shelf/forage work area on a bounded stone yard; admitted live North water
    banks using the existing atlas.
28. Added native RiverShrine world/reader art. Its neutral frame does not imply
    a new charged/depleted shrine mechanic.
29. Integrated the unchanged approved Seed Keeper 3 at Nella's real site.
    Added carrot/wheat sprouts, marlroot and Pitchpod sprout/ripe frames,
    neutral reserve tray and reserve ground cords.
30. Added actual prepared dry/wet soil; re-composed Nella's service and existing
    rows into a coherent main frontage with unequal remote remnants.
31. Added sixteen copper-pipe connections, neutral peat and live water-bank
    admission. Visible current owners control connections, not imagined flowing
    irrigation. Actors can interrupt the visible topology.

Anchors: `67629188e`, `d936500db`, `6d0702962`, `1aab4c528`, `00a188cac`,
`d025d95c5`, `d313eac64`, `c893ae58f`, `e49047200`, `a458e3bee`, `0a64468e5`;
[north vegetation](LIVE-16X24-NORTH-VEGETATION.md),
[works](LIVE-16X24-NORTH-WORKS.md), [plot](LIVE-16X24-NORTH-PLOT.md),
[Nella corner](GRAPHICS-G3-NELLA-CORNER.md).

## 7. East meadow and Orven's kitchen

32. Added native East/kitchen admission and eighteen worksite/plant/product
    frames: forge, oven, cot, chair, cracked/sealed batch pan, input tray,
    pickup board, Claspbean planted/growing/ripe, pulp, seed, FieldMeal,
    CharmFlowers, pipe, neutral steam vent and millstone. Reused approved cook.
33. Added Farmer and Cindercaller bodies and integrated the unchanged approved
    ridge/ReedbackGrazer into live owners and readers.
34. Replaced repeated flower avenues and hedge rectangles with unequal forms
    and clearer work approaches; further refined East flowers into drifts.
35. Added the fifteen-floor kitchen court, then refined its three floor sprites
    into broader, quieter stones. Added a bounded optional forge workpad.
36. Added real paid-job Working pan and Ready pickup art, driven by escrow and
    commissioned output. Repaired pans no longer keep stale split-pan guidance.
37. Added neutral Orchard Beehive art; harvesting still removes its real owner.
38. Added sixteen connected hedge frames, preserving the approved isolated
    frame and current visibility/destruction updates.
39. Added East live water banks and reused native art for solid walls, pen
    gate open/closed states and peat. Actual passage, collision, save/reload and
    return were verified; no claim that the penned animal was released.

Anchors: `8d02f27fc`, `9728a7c65`, `679ae9e2b`, `3e67ab983`, `3303d6240`,
`c76654faa`, `dce87081d`, `87ebff4b1`, `8902d3731`, `5c7c7e4d6`, `2a8e71044`,
`5a3c74359`; [East/kitchen](LIVE-16X24-EAST-KITCHEN.md),
[orchard](GRAPHICS-G3-KITCHEN-ORCHARD.md),
[hedges](GRAPHICS-G3-HEDGE-CONTINUITY.md),
[East review](GRAPHICS-G3-EAST-KITCHEN-REVIEW.md).

## 8. Inventory redesign and polish

40. Rebuilt the existing five modes—Equipment, Inventory, Tinkering, Abilities
    and Crafting—as one dark-green equipment ledger: anatomy, categorized pack,
    quantity/weight, persistent selected-item facts/portrait, tabs and controls.
41. Corrected reader layering, F1/Examine return, visible errors, cached selected
    descriptions, grimoire candidate facts and truthful replacement-slot prompts,
    including unusual anatomy and long overflow.
42. Repaired actual mouse position/button/wheel coherence and parked-pointer
    focus theft. Keyboard/controller navigation and painted hit regions agree.
43. Controller X opens Commands; F2/Commands expose character status. Bound
    combat abilities use authoritative details, with correct empty/candidate
    states and readable separators.
44. Added activation-key release handling so planting through an inventory
    letter does not also step the character when the inventory closes.
45. Tabs enter on a real item; rebuilds preserve the exact selected item and
    handle removed items or empty searches deliberately.
46. Brew/Forge completion shows actual output, partial results and failures
    without rerunning transactions. Corrected FieldMeal Food categorization
    and its unsupported throwing promise; no saved-item migration was added.
47. Replaced WORN with DMG/DEF: weapon dice, armor/shield AV and signed DV.
    Equipped [E] moved to the far right; quantity/weight capacity was retained.
48. Used retained/pool-backed UI and invalidated content snapshots rather than
    repaints every idle frame. A bounded 35.9-second Editor sample observed no
    additional inventory Render calls; this is not a hardware FPS guarantee.

Anchors: `f25554e6f`, `24491663d`, `f69e5f6a2`, `671c0410e`;
[inventory](GRAPHICS-G3-INVENTORY-REDESIGN.md),
[release fence](GRAPHICS-G3-INVENTORY-ACTION-RELEASE.md),
[polish](GRAPHICS-G3-INVENTORY-POLISH.md).

## 9. Examine, dialogue, popups and hotbar

49. Native Examine portraits show the authorized current world/carried/equipped
    subject at integer scale with correct aspect. Corrected double-brightened
    colors/alpha without repainting approved art; retained the previous 3D route.
50. Look/FOCUS refreshes after harvest, removal and visibility changes.
    Ordinary carried Examine opens the structured reader instead of log-only
    output; stale custody/provider cases refuse rather than show another item.
51. Increased reader body spacing from 1 to 1.25 and adjusted pagination to
    retain all facts and protected controls. This also affects shared announcements.
52. Added current-speaker native portraits to dialogue, including Nella and
    Warden, with safe short-node padding and clean trade/close handoff.
53. Fixed wide-window popup backing and the separate fractional-pixel green
    seams. Preserved glyph/hit geometry and translucent dialogue blending.
54. Filled and brightened pickup/dialogue footer controls, including after
    reader return, item removal, paging and close.
55. Expanded long world-action menus to show actual recipe prices and clear
    their previous footprint on shrink/close. Fixed coherent native mouse
    clicks, including wide edges and parked-pointer keyboard priority.
56. Added distinct original En Garde, Lunge and Follow Through HUD icons.
57. Fixed native mouse activation of hotbar cards, preserving aiming/cancel,
    cooldown refusal, keyboard priority and fallback.
58. Latest: wrapped En Garde into En / Garde to relieve border crowding.
    Lunge, Follow / Through and Conjure / Rain remain intact. This is not a
    skill, card, camera or font change; packaged delivery is still next.

Anchors: `4f4fb0167`, `e332cf040`, `41cd8b456`, `9ffd7c973`, `ad6f3439f`,
`0d1442551`, `183e0cc36`, `4c37bc025`, `99b86bb02`, `cc963241d`, `cb0c2497e`,
`34fe9cc8b`; [reader](GRAPHICS-G3-READER-LEGIBILITY.md),
[action menu](GRAPHICS-G3-ACTION-COST-READABILITY.md),
[hotbar names](GRAPHICS-G3-HOTBAR-NAMES.md).

## 10. Actual gameplay correctness and combat feedback changes

59. Reserved decoded entity IDs before load hooks create entities. This fixes
    continued-game collisions that blocked legitimate item preparation/brewing.
    Invalid loads do not advance the live allocator; old collisions are not
    rewritten and no save-format migration was introduced.
60. Player turn-start effects resolve the player's actual destination zone
    after travel, fixing stale-zone poison/feedback/death context. NPC policy
    and effect strength/duration are unchanged. Native verification is complete;
    the precise post-fix ordinary poison reproduction remains open.
61. Underfoot interactions exclude only the acting player from pile resolution,
    making lone usable sources directly available without weakening permissions
    or hiding genuine piles.
62. Added cold-generation approach checks for Nella and Orven. Actual first
    versions failed live review and were corrected; saved worlds stay literal.
63. In the two exact connected service sites, ordinary Viper/Scrabbler encounter
    rolls become an existing grazer one-for-one before creation. This prevents
    demonstrated missing/dead-on-arrival service cases. It is not global enemy
    removal, NPC resurrection, invulnerability or guaranteed safety on detours.
64. Issued Riposte faces the opponent and emits the existing Attack cue,
    including misses; rejected attempts do not. No chance/damage/RNG changes.
    Native tests cover it, but an actual triggered Riposte capture remains open.
65. Damage numbers remain whole and distinct, separate from status words.
    Labels follow recipient attribution and avoid other visible/interpolating
    bodies. They do not merge two hits into a misleading number; when no honest
    placement fits, the combat log remains authoritative.
66. Reduced repeated allocation in bounded reserve-permission validation while
    keeping fees, witnesses, theft consequences and finite-source rules intact.

The layout changes in sections 3, 6 and 7 also change fresh deterministic
placement/navigation. They preserve their documented route/resource constraints;
they do not rearrange existing saved worlds.

Anchors: `ee0f180cf`, `cda7087dd`, `8970db280`, `adad4d439`, `c698697d7`,
`4c37bc025`, `20d5c54a0`, `2a3c23a53`, `43b43c6a6`, `1aab4c528`;
[load identities](SAVE-ENTITY-IDENTITY-RESTORE.md),
[underfoot actions](GRAPHICS-G3-UNDERFOOT-ACTIONS.md),
[Nella population](GRAPHICS-G3-NELLA-POPULATION.md),
[Riposte](GRAPHICS-G2-RIPOSTE-FEEDBACK.md),
[travel effects](GRAPHICS-G2-STATUS-ZONE-FEEDBACK.md).

## 11. Verification, delivery and recovery work

67. Added selected native RED/GREEN, owner/state/fog/custody/countercase and GPU
    tests with actual-game screenshot routes. Suites overlap: their counts must
    not be summed into a fabricated total or whole-project green claim.
68. Exercised all six connected sites with ordinary controls: movement/combat,
    menus, finite loot, harvest, prepare, water, growth, repair, rights, trade,
    brewing, tempering, commissioned cooking, save/quickload/full restart and
    return. Each record limits what its particular save/seed proved.
69. Latest packaged route has 33 actual captures and 31 saved-state checks for
    the earned kitchen→East→glade→North→Nella return and Pitchpod growth,
    harvest, replant, full restart and border persistence. The current forge
    was inspected, not used; its earned-resin leg is still pending.
70. Produced successive source-labelled Mac/Linux packages and archive audits,
    preserving previous builds. Mac runs have actual evidence. No physical
    Deck or Linux gameplay claim is made from an archive check.
71. Added safe isolated-save launch/capture/restoration and corrected temporary
    GameView cleanup so user presets survive. Recovered concrete editor/platform/
    reload/modal/space failures; did not label restarts as gameplay fixes.
72. Preserved approved pixels, editable sources, prompts, old presentation,
    normal saves/preferences, unrelated work and exact source/build provenance.
    Earlier authorized cleanup is maintenance, not a gameplay improvement.
    Current storage reclamation remains inspection-only.

## Existing work not newly created by this run

The approved Seed Keeper, Downloads cook, Tatterjay and ridge grazer imports
date to October 18; the supplied beetle jar to October 21. They anchored this
run, which integrated relevant owners without claiming to have invented them.
The earlier throwing/Hurler and Measured Throw work, composable combat system,
parry/guard sounds, initial reusable floating text, original Skills screen,
and original examine-box redesign predate this overnight scope.

Crop growth/yields, reserve rights/fees, alchemy recipes, forging/quenching,
corpse butchery, kitchen repair and its 120-tick paid job, trade, the shrine's
Stoneskin donation and combat skills also existed. This run visualized,
exposed, repaired specific boundaries and exercised them. It did not invent
those deep mechanics or rebalance their values.

## Still unfinished

- Complete visual acceptance of the opening and every object/state in its six
  sites; a native-looking viewport is not a complete census.
- Remaining regional art kits and incidental equipment/manual/loot identities.
- Full-cast/directional animation, continuous combat feel, ordinary captured
  issued Riposte and the named post-travel status reproduction.
- A redesigned trade screen; the current inventory redesign does not cover it.
- New packages containing the latest En Garde wrap, then the current earned
  North-still/East-temper continuation and remaining service/item-use gates.
- Linux execution, physical Steam Deck controls and sustained release profiling.
  Shared atlases, retained UI and bounded Editor samples are not a measured
  whole-game speedup or a hardware performance guarantee.

## Current hourly session report and goals

Report checkpoint: 2026-10-26 21:53 UTC. Worked on: En Garde spacing repair;
native RED and 93/93 GREEN; six ordinary Unity captures at 1×/2×, real stance
activation, Lunge aim and inventory round trip; independent source/visual
review; complete save/preference/current-build and foreign-edit preservation;
hourly automation update; this comprehensive historical audit. Source/evidence
and this report are being committed together; remote verification follows.

Next session: freeze the reviewed source, refresh and verify current Mac/Linux
packages, replay ordinary hotbar use, then continue the unchanged earned
Pitchpod-resin save toward North brewing/East tempering. Prioritize the next
demonstrated opening defect over unrelated new asset packets. Share actual
captures and report completed/in-progress/blocked items and next goals hourly.

## Chronological commit appendix

The following history starts with the first overnight clearing commit and ends
at the pre-report HEAD. The report/hotbar commit follows it. Subjects are history,
not independent proof of acceptance; the current scope and limits above govern.

| Commit | Date | Change |
| --- | --- | --- |
| `55ac6069a` | 2026-10-22T21:49:19-05:00 | feat(art): build a native 16x24 seed keeper clearing |
| `103eb4977` | 2026-10-22T21:59:04-05:00 | docs(art): curate visual references for the overnight pipeline |
| `7215c448b` | 2026-10-22T22:05:29-05:00 | docs(art): require complete live presentation for the opening |
| `bd1315dec` | 2026-10-22T22:16:54-05:00 | docs(art): require the playable greybox to pixel-art pipeline |
| `6f62c4734` | 2026-10-22T22:32:10-05:00 | fix(rendering): project actors camera and lights through world cells |
| `b5dc1b3f3` | 2026-10-22T23:36:03-05:00 | feat(rendering): activate an isolated playable 16x24 opening greybox |
| `510f7cea8` | 2026-10-22T23:54:12-05:00 | feat(rendering): integrate the first native 16x24 opening sprites |
| `724d4e079` | 2026-10-23T00:07:40-05:00 | feat(rendering): give the opening its distinct native pixel cast |
| `e8bfb946f` | 2026-10-23T00:29:52-05:00 | feat(art): render native opening anchors from live state |
| `c3df1e330` | 2026-10-23T01:03:28-05:00 | feat(art): show live native object states in the opening |
| `4f50de3ab` | 2026-10-23T01:14:36-05:00 | feat(art): author native opening motion and loose item sources |
| `4f4fb0167` | 2026-10-23T02:28:39-05:00 | feat(art): animate native opening and match item portraits |
| `e332cf040` | 2026-10-23T03:46:12-05:00 | feat(art): extend native pixel presentation into the cellar |
| `f136ba4ee` | 2026-10-23T03:48:22-05:00 | docs(art): require milestone screenshots in chat |
| `67629188e` | 2026-10-23T04:31:13-05:00 | feat(art): establish the playable north pixel route |
| `d936500db` | 2026-10-23T05:22:15-05:00 | feat(art): replace north-route vegetation markers with native pixels |
| `6d0702962` | 2026-10-23T06:36:41-05:00 | feat(art): give north works and creatures native state-aware sprites |
| `1aab4c528` | 2026-10-23T07:42:35-05:00 | feat(art): integrate Nella's native crop and reserve plot sprites |
| `4cf77221b` | 2026-10-23T09:45:00-05:00 | docs(art): verify the live north plot through harvest and revisit |
| `0fdcec658` | 2026-10-23T12:06:45-05:00 | feat(art): give north plot goods native item silhouettes |
| `e9a4739c7` | 2026-10-23T17:41:32-05:00 | feat(art): distinguish loose north works goods in native pixels |
| `ce2fe36e7` | 2026-10-23T18:05:48-05:00 | docs(art): record recovered works selector measurement |
| `413c84a71` | 2026-10-23T19:38:30-05:00 | docs(art): prioritize a complete normally launched 2D presentation |
| `5eaddc1ac` | 2026-10-23T20:03:20-05:00 | feat(graphics): preserve world identity in native flat fallback cells |
| `1644aea51` | 2026-10-23T21:09:50-05:00 | feat(graphics): make native flat presentation the reversible gameplay default |
| `e6b6609f1` | 2026-10-23T21:53:14-05:00 | feat(graphics): connect opening masonry to real wall topology |
| `ee3c90abe` | 2026-10-23T22:39:10-05:00 | feat(graphics): integrate state-aware opening ground art |
| `20d5c54a0` | 2026-10-23T23:30:48-05:00 | fix(rendering): separate damage and status readouts |
| `dd1d11798` | 2026-10-23T23:48:44-05:00 | fix(project): restore complete saved layer table |
| `4fc5a666e` | 2026-10-24T08:00:04-05:00 | docs(recovery): preserve remaining local source before cleanup |
| `2a3c23a53` | 2026-10-24T08:51:52-05:00 | fix(graphics): keep combat labels with their recipients after editor recovery |
| `41cd8b456` | 2026-10-24T09:58:29-05:00 | fix(presentation): show current harvest focus and carried item portraits |
| `7e6c259f3` | 2026-10-24T10:44:30-05:00 | docs(graphics): accept the ordinary plot product route |
| `ee0f180cf` | 2026-10-24T12:03:56-05:00 | fix(save): reserve loaded identities before creating new items |
| `e0ccb15df` | 2026-10-24T12:06:23-05:00 | docs(graphics): record verified source preservation and cache audit |
| `8d02f27fc` | 2026-10-24T13:09:00-05:00 | feat(graphics): integrate native east worksite and kitchen art |
| `62adccc54` | 2026-10-24T13:10:42-05:00 | docs(graphics): record east kitchen push and storage protection |
| `9728a7c65` | 2026-10-24T14:06:26-05:00 | feat(graphics): break repeated east meadow and kitchen layouts |
| `34ca726f4` | 2026-10-24T14:08:18-05:00 | docs(graphics): record landscape preservation and storage audit |
| `679ae9e2b` | 2026-10-24T15:08:23-05:00 | feat(graphics): give Orven's kitchen a playable stone court |
| `47236211c` | 2026-10-24T15:11:24-05:00 | docs(graphics): preserve court delivery and next actor plan |
| `3e67ab983` | 2026-10-24T15:50:21-05:00 | feat(graphics): show native Farmer and approved Grazer in live opening |
| `2ed459cd1` | 2026-10-24T15:51:14-05:00 | docs(graphics): record verified actor delivery and storage decision |
| `3303d6240` | 2026-10-24T17:15:35-05:00 | feat(graphics): show paid kitchen progress in native pixel art |
| `5d7e897fa` | 2026-10-24T17:19:04-05:00 | docs(graphics): record verified kitchen delivery and storage audit |
| `c76654faa` | 2026-10-24T17:46:06-05:00 | feat(graphics): give the Cindercaller a native 16x24 body |
| `fa0bb4184` | 2026-10-24T17:48:06-05:00 | docs(graphics): record Cindercaller delivery and storage audit |
| `cda7087dd` | 2026-10-24T19:10:50-05:00 | fix(graphics): restore Riposte and travelled status feedback |
| `f05427044` | 2026-10-24T19:30:09-05:00 | feat(graphics): give the opening long sword native world and Examine art |
| `a5cd732c8` | 2026-10-24T20:08:11-05:00 | docs(graphics): verify ordinary frost temper and Claspbean renewal |
| `43b43c6a6` | 2026-10-24T20:26:38-05:00 | fix(graphics): keep combat readouts clear of moving native actors |
| `2ed9f5459` | 2026-10-24T20:29:32-05:00 | docs(graphics): record preservation and connected service acceptance route |
| `9ffd7c973` | 2026-10-24T21:05:05-05:00 | fix(graphics): space examine text and verify six-site continuity |
| `f551652bb` | 2026-10-24T21:26:01-05:00 | build(graphics): deliver verified flat16x24 current packages |
| `37bbc54f4` | 2026-10-24T21:56:27-05:00 | fix(graphics): reveal prepared soil beneath harvested rows |
| `00a188cac` | 2026-10-24T22:41:48-05:00 | fix(graphics): break repeated hedge bands around northern works |
| `bee27d796` | 2026-10-24T23:01:26-05:00 | docs(graphics): record verified preservation and storage limit |
| `0d1442551` | 2026-10-24T23:33:31-05:00 | fix(graphics): keep wide popup backgrounds opaque |
| `f25554e6f` | 2026-10-25T01:18:06-05:00 | feat(ui): redesign the inventory as a readable equipment ledger |
| `d025d95c5` | 2026-10-25T02:05:05-05:00 | feat(rendering): distinguish Nella's prepared and watered soil |
| `24491663d` | 2026-10-25T02:28:37-05:00 | fix(input): wait for inventory action shortcuts to be released |
| `f69e5f6a2` | 2026-10-25T03:28:18-05:00 | fix(inventory): retain item focus and report completed crafts |
| `696a9ff35` | 2026-10-25T03:46:13-05:00 | docs(graphics): accept the inventory polish in ordinary play |
| `ac00cd398` | 2026-10-25T04:05:29-05:00 | fix(editor): restore owned graphics audit resolutions precisely |
| `a7bb8b5f3` | 2026-10-25T04:05:46-05:00 | docs(graphics): preserve safe cleanup and the next scene design |
| `d313eac64` | 2026-10-25T05:27:58-05:00 | feat(graphics): give Nella a coherent working field frontage |
| `801f96d04` | 2026-10-25T05:41:44-05:00 | docs(graphics): preserve Nella cleanup and next cellar material gate |
| `d4361e8fe` | 2026-10-25T05:43:36-05:00 | docs(storage): record verified inactive clone cache recovery |
| `f516e6d99` | 2026-10-25T05:55:13-05:00 | chore(graphics): preserve native cellar floor source and test draft |
| `8312e1d7b` | 2026-10-25T06:39:05-05:00 | feat(graphics): give the playable cellar a quiet native stone floor |
| `cb0c2497e` | 2026-10-25T07:03:00-05:00 | feat(graphics): distinguish the Warden's three hotbar abilities |
| `34fe9cc8b` | 2026-10-25T07:19:59-05:00 | fix(input): use the native pointer for hotbar clicks |
| `ec65d74ee` | 2026-10-25T07:21:41-05:00 | docs(graphics): record verified preservation and stale installer cleanup |
| `8758d67dc` | 2026-10-25T08:10:14-05:00 | feat(graphics): give opening hatchets and leather armor native identity |
| `3cec8c906` | 2026-10-25T08:38:23-05:00 | docs(graphics): preserve second-seed playtest before storage recovery |
| `445c0813e` | 2026-10-25T08:50:01-05:00 | docs(graphics): record second-seed recovery and popup raster plan |
| `183e0cc36` | 2026-10-25T21:31:22-05:00 | fix(ui): prevent world seams through popup backing |
| `671c0410e` | 2026-10-25T21:53:22-05:00 | feat(ui): show combat values in inventory item rows |
| `c893ae58f` | 2026-10-25T22:48:06-05:00 | feat(graphics): ground the northern still yard in its working layout |
| `35c140c67` | 2026-10-25T23:10:53-05:00 | docs(graphics): verify fresh flat-world packages and inventory |
| `db3369cbf` | 2026-10-26T00:10:30-05:00 | feat(graphics): compose opening vegetation into uneven wild groups |
| `dce87081d` | 2026-10-26T00:57:13-05:00 | feat(graphics): connect the eastern forge with a quiet workpad |
| `8970db280` | 2026-10-26T01:58:26-05:00 | fix(interaction): expose actions on objects under the player |
| `87ebff4b1` | 2026-10-26T02:39:22-05:00 | feat(graphics): give orchard hives a native identity |
| `8902d3731` | 2026-10-26T03:23:54-05:00 | feat(graphics): connect live hedges across their disclosed neighbors |
| `2f5749769` | 2026-10-26T03:40:26-05:00 | docs(graphics): verify current kitchen and hedge packages |
| `5c7c7e4d6` | 2026-10-26T04:49:23-05:00 | feat(graphics): break up repeated eastern flower masses |
| `2a8e71044` | 2026-10-26T05:15:26-05:00 | fix(graphics): carry water banks into the eastern meadow |
| `4dcfdee80` | 2026-10-26T05:36:09-05:00 | docs(graphics): verify meadow and water in current packages |
| `c06a653ae` | 2026-10-26T05:47:09-05:00 | docs(graphics): verify Nella crop loop in current Mac build |
| `f5e7c3d79` | 2026-10-26T06:29:13-05:00 | feat(graphics): retain native identity for opening animal remains |
| `e49047200` | 2026-10-26T06:52:10-05:00 | feat(graphics): carry native water banks into the North works |
| `106ce80e5` | 2026-10-26T07:08:02-05:00 | docs(graphics): verify current aftermath and North packages |
| `4b61c630b` | 2026-10-26T07:30:44-05:00 | docs(graphics): verify packaged cellar growth and revisit |
| `47d678995` | 2026-10-26T08:04:30-05:00 | fix(graphics): make native pixel zoom reachable through ordinary controls |
| `39f68b6b3` | 2026-10-26T08:17:15-05:00 | docs(graphics): verify playable zoom in current desktop packages |
| `a458e3bee` | 2026-10-26T09:04:21-05:00 | feat(graphics): give the river shrine a native world and examine identity |
| `adad4d439` | 2026-10-26T09:59:05-05:00 | fix(world): give Nella a reachable initial approach |
| `ad6f3439f` | 2026-10-26T10:22:34-05:00 | fix(ui): show the current speaker’s native art in dialogue |
| `0a64468e5` | 2026-10-26T10:56:54-05:00 | feat(graphics): render Nella’s connected copper and wet peat |
| `c698697d7` | 2026-10-26T12:09:33-05:00 | fix(world): keep the connected kitchen staffed on first arrival |
| `5a3c74359` | 2026-10-26T12:33:53-05:00 | feat(graphics): give East walls, gate and peat their native art |
| `66ea682cf` | 2026-10-26T12:54:34-05:00 | docs(graphics): verify consolidated current packages and gameplay captures |
| `4c37bc025` | 2026-10-26T13:53:44-05:00 | fix(opening): preserve Nella access and readable pickup controls |
| `99b86bb02` | 2026-10-26T14:11:50-05:00 | fix(ui): back and brighten dialogue controls |
| `dab99535b` | 2026-10-26T14:28:00-05:00 | docs(graphics): verify corrected Nella packages in real play |
| `cc963241d` | 2026-10-26T15:33:07-05:00 | fix(ui): expose action costs and use coherent mouse input |
| `daa388643` | 2026-10-26T15:34:51-05:00 | docs(graphics): record settled editor and metadata restoration |
| `d9959fe60` | 2026-10-26T16:02:28-05:00 | docs(graphics): verify action-menu fixes in current player packages |
| `4e8b21e23` | 2026-10-26T16:28:46-05:00 | docs(graphics): verify the earned Pitchpod cycle in current package |


73. **Connected copper consistency (2026-10-26):** North and East exact pipe owners
    now share Nella's accepted connected native art in world/Examine, with no peat
    scope expansion.105/105 targeted native tests; actual ordinary six-site review
    and actor-cover checks recorded in GRAPHICS-SIX-SCENE-POLISH.md.
