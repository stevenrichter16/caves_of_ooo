# Graphics completion plan

Status: **G1 delivery implemented and accepted in bounded ordinary Unity Play;
opening/region art and packaged/device release remain incomplete.** Initial
audit was 2026-10-23 against `ce2fe36e7`; its findings below are historical.
As of 2026-10-24 UTC, normal fresh/Continue launch defaults to flat16×24,
with a reversible Graphics choice and continuous projection during travel.
See [renderer evidence](GRAPHICS-G1-RENDERER.md) and
[launch evidence](GRAPHICS-G1-LAUNCH.md). G2 connected masonry is now integrated
and accepted within its bounded ordinary keyboard route; see
[wall evidence](GRAPHICS-G2-BOUNDARIES.md). Pond/road/soil are also integrated
with bounded ordinary Play acceptance in [ground evidence](GRAPHICS-G2-GROUND.md);
wet-soil ordinary validation and full scene acceptance remain open.
This is not complete regional artwork or a new Steam Deck build.

## Project priority and decision authority

**Graphics completion is the game's #1 priority and a release blocker.** The
user explicitly identifies incomplete and inconsistent graphics as the largest
source of stress and the obstacle preventing the game's deep mechanics from
fulfilling their potential. This is not a cosmetic backlog to revisit after
more mechanics. A technically correct but visually incoherent build does not
resolve the blocker.

**There is no imposed completion date.** Do not rush to finish today, tomorrow
or this week, cut acceptance to meet an overnight target, or confuse taking
time with making progress. Spend the time needed on the highest-impact visual
problem, demonstrate the improvement in the actual game, and move forward.

**The user delegates judgment beyond individual prompts.** Current and future
agents should identify missing steps, challenge an ineffective workflow,
choose available tools, regroup asset work, improve real scene composition,
and fix integration/performance/readability defects without waiting for a
prompt for each decision. This is authority to achieve a coherent game, not
to multiply speculative systems or discard approved art without cause.

**Continue across sessions.** Breaks and tool/context limits require a precise
handoff, not abandonment. Keep the authorized continuation active and pursue
the next unfinished meaningful gate. Completing a small packet is not a reason
to stop. Prioritize work by its effect on the player's delivered experience;
defer unrelated feature growth until the visual release is dependable.

## The outcome

Make graphics a dependable part of playing Caves of Ooo: a distinctive,
consistent, readable flat 16×24 world that exposes the existing mechanics.
The player must see it by launching the actual game, keep it while travelling,
and understand characters, terrain, objects and changes without consulting an
art review. Preserve the previous presentation as an explicit reversible option.

The immediate release is the **complete opening journey**, followed by region
coverage using the same production system. A coherent fallback permits travel
while art is unfinished; it is not evidence that a region's art is finished.
Do not require every item in the whole game to have a bespoke picture before
delivering the opening. Do require every encountered object to remain truthful,
identifiable and usable.

Graphics is the active priority. Defer new gameplay/content systems unless a
specific defect blocks this visual release. Preserve the approved Seed Keeper,
cook, bird, grazer and jar. Continue autonomously with available tools. This is
original Caves of Ooo art inspired by the user's desired Qud-like readability,
not a claim of copied art or engine parity.

## What the audit found

| Finding | Current evidence | Consequence |
| --- | --- | --- |
| Normal launch does not enable the native presentation | `Assets/Scenes/Main/SampleScene.unity`; `Assets/Editor/Scenarios/LiveFlatPixelGreyboxTools.cs`; both camera bindings in `GameBootstrap.cs` | The user sees old graphics despite work existing in the repository. Normal delivery moves to the first milestone. |
| Presentation and native-art location eligibility are coupled | `ZoneRenderer.FlatPixel.cs`, `FlatPixelGreyboxCatalog.IsAdmitted` | Only glade, cellar, north works and Nella's plot stay in this mode. Separate global projection from local art eligibility before extending it. |
| Switching out is not a complete return path | `Village3DControls.Update` exits flat mode, then toggles older modes | Add an explicit reversible presentation setting usable from the controller-accessible graphics menu. |
| Static assets outnumber completed experiences | Twelve source groups contain 111 authored frames, including states and motion; approved standalone inputs are separate | Frame counts are not identity, animation, region or release coverage. Stop treating a few new icons as a major milestone. |
| Generic category silhouettes still substitute for uncovered identities | `FlatPixelGreyboxCatalog.TryResolve` and existing family classification | Replace rough placeholders on the release route. Outside it, use deliberate family art where correct and the real object's glyph/color where not. |
| The larger screen composition is unfinished | Existing Game captures and `ArtSource/Live16x24/PARTS.md` | Connected masonry, banks, paths, material contrast and framing have higher immediate value than more inventory detail. |
| Two latest product groups lack ordinary Play acceptance | `LIVE-16X24-PLOT-PRODUCTS.md`, `LIVE-16X24-WORKS-PRODUCTS.md` | Close those gates within the route. Do not silently promote native test results to gameplay acceptance. |
| Editor tests cannot establish the target device experience | Current profiles and capture records | Packaged launch, input, motion and physical Deck performance have distinct gates. |

The fresh [Nella plot baseline](Verification/Live16x24/graphics-finish-baseline-plot.png)
is a real 1280×800 Unity Game capture after ordinary travel from spawn to
`Overworld.11.8.0`, player `(20,10)`, 40 HP. It demonstrates current integration,
not a newly completed visual milestone. Its [read-only receipt](Verification/Live16x24/graphics-finish-baseline-plot.json)
records simulation state; that inventory can include off-camera entities and
must not be mistaken for a list of things visible in the screenshot.

## Research and the method chosen

This is a project-specific synthesis, not a claim that one universal art
workflow is best. The useful principles are:

- **Solve recognition before decoration.** Valve's production account works
  from silhouette and interior masses toward the final in-game character and
  uses screenshots/paintovers to solve design problems. It also treats contrast
  and material differences as gameplay information. Apply those principles to
  the small native sprites and real scene, rather than importing its 3D style.
  [Valve, *How Valve Connects Art Direction to Gameplay*](https://cdn.steamstatic.com/apps/valve/2008/GameFest08_ArtInSource.pdf).
- **Evaluate at the actual pixel scale.** Point filtering, uncompressed art,
  consistent pixel density and deliberate pivots preserve the authored result.
  Retain our tested rectangular projection; the older tutorial is not a reason
  to replace it or install a different package.
  [Unity/Mega Cat pixel-art workflow](https://unity.com/blog/games/2d-pixel-perfect-how-to-set-up-your-unity-project-for-retro-8-bits-games).
- **Make exports repeatable, not tool-dependent.** Aseprite supports batch
  sheet/data exports and animation tags. It is an optional native animation
  tool if available; the existing integer-cell Blender exporter is already
  useful and must not be discarded for a tool migration.
  [Aseprite CLI](https://www.aseprite.org/docs/cli/).
- **Judge the delivered device experience.** Steam's current Deck requirements
  include playable defaults, controller access and readable text at 1280×800;
  nine-pixel text is a minimum, with twelve recommended where possible. Treat
  this as a floor, not proof our game looks or feels good.
  [Steam Deck compatibility requirements](https://partner.steamgames.com/doc/steamhardware/compat).

### The iteration loop

Use one complete gameplay view or related visual family per loop:

1. **Observe.** Play the real route with ordinary controls. Capture the actual
   Game output and a short motion sequence, with seed, location, action, mode
   and output size. Start with the three largest visible or functional defects.
2. **Block.** Greybox only the geometry/composition that needs changing. Use
   real cells and owners. Test paths, collisions, targeting and interactions.
   Reuse accepted geometry when it is unchanged.
3. **Design against the capture.** Use built-in imagegen on that screenshot,
   the approved sprites and at most two relevant references. Specify the
   particular defects to solve. Reject invented actors, fake services, changed
   walkability, gradients and pseudo-3D lighting. Save input, exact prompt,
   output and the selected design decisions.
4. **Map parts and states.** Extract the needed silhouettes, connected edges,
   palette clusters and animation poses. Give each a real owner, footprint,
   pivot and state source. Art must never imply a resource, opening or service
   the simulation does not have.
5. **Author natively.** Produce editable exact-resolution sprites/frames.
   Imagegen is the design assistant, not the final pixel-grid exporter.
   Preserve approved bases; animation derivatives remain separate.
6. **Integrate the family.** Use shared materials/atlases where suitable,
   cached lookups and the existing state/visibility rules. Finish the related
   outcomes together: a harvest may leave a spent plant, remove the owner, or
   expose remaining soil, depending on the actual lifecycle.
7. **Exercise it.** Repeat the route, including a changed state, departure and
   return. Verify world/examine agreement and actual input. Capture the same
   composition and zoom; enlarged contact sheets are supplementary only.
8. **Critique and fix.** Compare baseline, concept and integrated game. Fix
   the largest remaining defect first. An independent reviewer checks both
   aesthetics/readability and state/ownership correctness.
9. **Accept and freeze.** Record the passing criteria and share the real
   screenshot or clip. Reopen accepted work only for a named defect or a
   dependency exposed later. Update the ledger and move to the next family.

There is no required count of iterations. If two passes do not improve the
named defect, reconsider the silhouette, scale, composition or tool instead
of adding detail. Limit work in progress to one unaccepted art family and one
supporting implementation slice. The two already-integrated product groups are
existing acceptance debt, not permission for more parallel unfinished batches;
close them during the next relevant ordinary route. When live input is unavailable, close source
or code work already in scope; do not accumulate new areas whose appearance
has never been checked in the game.

## Visual contract

- Flat top-down world, native 16×24 cells, square pixels and integer display
  magnification. No return to volumetric cubes or blurred generated textures.
- Preserve approved character identities and source bytes. Their deliberate
  color masses and negative space define the style; every sprite need not use
  an identical palette.
- Quiet dark green ground is the Spread anchor, not a universal biome paint.
  Other materials and regions retain readable distinctions.
- Read order: player and nearby danger; actionable objects; routes/boundaries;
  incidental detail. Strong contrast, saturation and motion must earn their
  place. Do not turn all interactables into glowing quest markers.
- Distinguish walls, low crossings, doors and empty passage by shape. Connect
  neighboring terrain visually without changing collision or revealing hidden
  neighbors. Water edges must agree with water/hazard cells.
- Native-size shape conveys object identity. Color alone must not be the only
  cue for hostility, ripe/spent, intact/broken, or charged/empty where relevant.
- Animation explains movement, attack contact, defence and state change.
  Avoid generic bouncing or constant environmental motion. Diagonal travel
  and action timing must be coherent; identical copied frames do not count as
  a complete directional set.
- Review 1× and 2× world magnification with the real HUD at 1280×800. Choose
  the default from visibility, tactical area and reading comfort, not from
  how large a sprite looks in an editor. Preserve a useful zoom choice.

## Delivery milestones

### G1 — A normal, reversible, consistently 2D playable candidate

**First delivery.** Starting a new game or continuing a game must enter the
flat presentation through ordinary launch. Walking to an unfinished location
must not silently turn it back into 3D.

Implementation order:

1. Separate the request for flat world projection from eligibility for the
   opening's authored skin. Preserve the current-world identity checks for
   regional art. Do not simply return `true` from `IsAdmitted`.
2. Add an identity-preserving native fallback for uncovered cells/subjects.
   Prefer an approved semantic family; otherwise use the real `RenderPart`
   glyph/color on the native grid. Keep terrain/material identity, actual water,
   hazards and light/state cues. Never use one generic humanoid for every
   unknown actor or paint every biome green.
3. Route world, carried examine, ground, actors, fog and effects through the
   same presentation choice. Older painters/models must not reappear through
   a fallback path. Unexplored cells stay hidden; remembered terrain must not
   expose live occupants, finite supplies or current hidden neighbors.
4. Add a separate persistent presentation preference and a controller-accessible
   graphics-menu choice. Make fresh installs default to native 2D; preserve an
   explicit previous-presentation choice. F11 and menu must allow a round trip
   without corrupting old detail settings or consuming a turn. Isolated tests
   must restore any preferences they borrow.
5. Wire the normal scene/bootstrap paths. Review the study/return menu labels
   so their wording agrees with their behavior. Preserve fullscreen square UI,
   rectangular world projection, camera framing and both new/loaded game paths.

**Tests before code:** normal scene activation; fresh/continue; mode round trip;
admitted opening → uncovered Spread → other biome → underground → return;
foreign world contexts; no actor/glyph ghosts; water and hazardous coatings;
fog and custody; world/examine identity; missing art; fullscreen and mouse
coordinate alignment. Reuse existing `FlatPixel*`, `Village3DControlsTests`
and save-isolation fixtures, adding behavior-specific failing cases first.

**Acceptance:** ordinary packaged launch plus travel and menu return, with
screenshots and a visible version identifier. This is a usable **candidate**,
not final art acceptance. Unfinished native fallbacks are explicitly listed.

### G2 — One reference-quality opening, judged as a scene

Finish the full starting view and one active combat/interaction view before
more disconnected item packets. Reuse the real ruin, pond, garden and routes.
Prioritize connected wall forms/end caps, pond banks, road edges, soil,
foreground/background contrast and purposeful clusters. Keep quiet ground.
Add visual detail only where actual owners support it.

Close player/nearby threat motion, contact/defence cues, harvest/repair changes,
cursor and landing/attack outlines. Review the HUD alongside the world for
competing contrast and readable text; do not begin an unrelated UI redesign.

**Acceptance:** the same start, ruin passage, pond action and combat views are
coherent at actual size and in motion, with zero rough category placeholders.
Objects remain recognizable with selection outlines removed. All routes and
actions remain usable; earlier art and approved actors remain recoverable.

### G3 — The complete six-zone opening journey

Complete the glade, its cellar, north works, Nella's plot, east connector and
Orven's kitchen as one experience. The east-side scope needs a current graph
audit before admission; do not admit an address belonging to a different POI.

Close the pending plot/works product loops; cover merchant-added crops and
products that this route can legitimately introduce, incidental residents,
creatures, equipment, drops and functional props. Cover growth/harvest,
repair/destruction, supply expenditure, brewing/cooking and revisit states
where those mechanics actually exist. Keep the old clinical/test studies
separate from real population.

Maintain [the coverage ledger](../ArtSource/Live16x24/release-coverage.json).
It initially records known coverage and explicit census gaps; it is not yet
an exhaustive asset inventory. During G1/G2, enumerate real generated owners,
service stock and reachable state transitions into that ledger. Count distinct
visual treatments and state coverage, not all blueprint strings as separate
artwork. Freeze this release inventory once audited. Add scope only for a
documented encountered gap, not a new feature idea.

**Acceptance route:** new game → glade interactions/combat → cellar loot and
return → north works harvesting/preparation/brewing → Nella's permission,
water/growth/harvest/plant loop → east/kitchen service → return → save/quit/
continue. Include changed/depleted and failed/denied actions. Every visible
owner on this audited route has approved bespoke or appropriate family art;
no rough placeholder is excused by its small size.

### G4 — A repeatable production path and whole-world coverage inventory

Do the small part of this milestone needed by G2/G3 as those families land.
Replace the growing atlas constructor chain with one bounded descriptor/
loader seam and shared import validation. Keep semantic state selection in
tested code; do not turn gameplay rules into a general art scripting language.
One static family should add source/data, not another lifecycle implementation.

Generate native PNG, metadata and contact output from editable source through
one repeatable command. Validate dimensions, pivot, palette/alpha, references,
approved-source hashes and resource disposal. Consolidate atlases only if
measured rendering cost justifies it; current borrowed texture/portrait UV
behavior makes blind SpriteAtlas substitution unsafe.

Inventory reachable world content by visual family and biome/site kit:
terrain/liquids/architecture, vegetation/crops, actors, usable props, equipment/
loose goods, effects and interface illustrations. Distinguish authored native,
approved shared family, temporary glyph fallback, and missing/incorrect art.
Distinguish integrated, visually accepted and hardware-verified evidence.

**Acceptance:** reproducible output; adding one family requires no new atlas
lifecycle branch; no unknown treatment on the audited opening; a finite
remaining-world backlog grounded in actual reachable generators, populations,
loot and services. No invented whole-world percentage before this census.

### G5 — Finish regional kits without reopening the style

Extend the accepted system through the rest of the Spread, then the Sodden
around Sumphold, then remaining reachable regions/depths ordered by observed
exposure and the importance of their mechanics. Do not equate another green
ground palette with a new biome.

For each region, audit and finish its terrain/material kit, characteristic
flora/fauna, people/structures, services/items and action states. Review a
quiet wilderness, a populated destination and a dangerous encounter together.
Use neighboring-region travel to catch abrupt material/scale changes. Common
equipment and people reuse the common library; distinctive gameplay warrants
distinctive forms.

**Acceptance per region:** the frozen regional owner/state backlog is empty;
ordinary travel and actions have comparable screenshots/clips; significant
visual and gameplay defects are fixed. Report remaining regions separately.
The first opening release does not wait for all of G5.

### G6 — Release, measure and close

After G3 and the necessary G4 tooling, deliver the opening build; repeat this
gate after each regional batch. Build from the recorded commit, use the
requested `-current` folder suffix, and verify the executable through ordinary
launch rather than relying on an editor-only flag. Preserve the older art and
an explicit restore choice.

Test keyboard, mouse and controller through launch, continue/new game,
exploration, targeting, graphics settings, inventory and popups. Capture actual
1280×800 output. Measure CPU/GPU/frame-time distribution, allocations and
rendering batches during a fixed active route and dense combat; record hardware,
build, resolution and settings. Separate asset loading/travel from steady play.
Do not hide spikes in an average or claim a speedup from unmatched captures.

Provisional target: smooth 60fps steady rendering on Deck at 1280×800, with
Valve's 30fps minimum as a compatibility floor, not the quality goal. Establish
a real packaged baseline before committing to a device budget; current editor
selector timings cannot prove it. Physical Deck performance/controller feel
remain **unverified** without an accessible device and observed evidence. Do
all desktop/package checks independently rather than stopping the art work.

**Opening finished:** normal default works; no silent style changes; all six
zones and their audited actions have accepted art; motion and targeting read
clearly; no significant defects; reviewed gameplay captures; packaged build
tested; remaining hardware/world limitations named. The original seven-milestone
opening may only be reported complete when its actual coverage/release gates
are met, not when this plan is written. The user subsequently requested
continuing work across sessions. The existing heartbeat remains active through
this release and the authorized remaining regional work.

## Review discipline and reporting

Use the existing `CLAUDE.md` RED → GREEN → counter-check → review cadence for
runtime behavior. Data/art-only work uses meaningful shared source/import
checks plus real visual review. Do not create a large duplicated test packet
for every static sprite. Run affected regressions for each slice and a broader
sweep at release boundaries; do not repeat unrelated tests as a substitute for
playing the result.

Keep one concise dashboard in the ledger: current delivery, completed and
pending journey gates, unresolved placeholders/states, normal-launch status,
three largest visual defects, latest accepted capture, next action. Historical
records remain evidence, not competing schedules. Share actual before/after
Game screenshots and clips at visible milestones; label concepts as concepts.

Independent review asks: Can the player tell what this is? Can they see what
changed? Does it promise something false? Does it match the neighboring art?
Does it remain legible in combat and fog? Does the normal build show it?
Fix significant correctness/readability failures before accepting a family.
An aesthetic preference without a named defect does not reopen accepted work.

## Initial audit review and handoff — historical, 2026-10-23

- 🟡 **Delivery failure identified:** normal launch remains old-style. G1 is
  the first implementation milestone. Do not mark it fixed by this document.
- 🟡 **Scheduling correction:** pause isolated new item/biome packets. Close
  the two existing product acceptance gaps as part of the full route while
  prioritizing presentation continuity and scene-level finish.
- 🔵 **Reuse:** rectangular projection, approved assets, original sources,
  existing gameplay graphs, source exporters and valid test evidence stay.
- 🧪 **Not proven:** current normal-build 2D, whole-world native coverage,
  completed east/kitchen, final motion, packaged/physical Deck acceptance.
- **This turn's actions:** source/record audit, primary-source research,
  independent activation/production reviews, ordinary seed-64 travel and the
  new plot baseline. No gameplay or production-rendering code changed; no new
  product acceptance or new native-test run is claimed. The isolated session
  was stopped, original SampleScene/editor zoom restored, and pending/save/
  view-isolation flags read back clear.
- **Next action:** perform G1's detailed source sweep, save its corrections,
  then write and run failing global projection/fallback tests before changing
  renderer behavior. Keep native art eligibility exact while enabling coherent
  presentation beyond it. Once G1 is playable, use the same actual opening
  captures to drive G2, not another unrelated concept direction.

This plan supersedes the old packet-by-packet scheduling notes while retaining
the [required production pipeline](LIVE-16X24-ART-DIRECTION.md) and its existing
evidence. It is a path to completion, not a report that completion occurred.


## Current continuation — 2026-10-24 UTC

Latest bounded action slice: [action readability](GRAPHICS-G2-ACTION-READABILITY.md).
The source-preservation and selective cleanup work is complete; source, saves
and the selected current build remain preserved. Unity opens SampleScene,
compiles and accepts ordinary controls. The earlier unread-dialog blocker is
superseded. The installed6000.3.4f1 package-signature warnings match a documented
certificate-verification defect; a patched6.3 editor remains a separate
environment follow-up, not a completed upgrade. See [storage audit](LOCAL-STORAGE-AUDIT.md)
and the [recovery receipt](Verification/Live16x24/recovery-cleanup-20261024.json).

Recovered ordinary combat confirmed separated complete damage values and
unobscured Player art, then exposed misleading attribution when a high status
word appeared beyond a nearer actor. The additional local placement correction
has native **61/61 focused plus211/211 prior-regression passes**, zero skipped,
after61cases produced50passes/11behavioral failures. These are the current
272cases; do not add the earlier253-case run as distinct new coverage.
Approved sprites, gameplay and animation timing remain unchanged.

The post-attribution replay supplies **41 actual1280×800 EOF frames**:
[24frames/20events](Verification/Live16x24/G2Action-20261024-134107-270/index.json)
and [17frames/6events](Verification/Live16x24/G2Action-20261024-134257-723/index.json).
Independent review found no new P2 in these bounded samples. [A12](Verification/Live16x24/G2Action-20261024-134107-270/frame-12.png)
shows nearby Shaken and separate1/8; [B07](Verification/Live16x24/G2Action-20261024-134257-723/frame-07.png)
shows a clamped lethal2, with later Idle/readout clearance in B14–B16. Genuine
combat rolls differed: the original Bleeding/Player geometry did not recur.
Native tests establish that exact correction; the replay is ordinary visual
regression evidence, not a controlled identical-event before/after or video.

G3 plot products now have partial ordinary acceptance: paid reserve rights,
actual Pitchpod harvest producing two resin units and one seed, pickup and
[carried resin Examine](Verification/Live16x24/plot-products-resin-carried-examine.png).
The file named `plot-products-resin-ground-examine.png` shows a pickup list,
so ground Examine remains pending alongside planting, clod preparation and
the remaining product families. Works-product forage/preparation/brew/coin
use and east/kitchen still need their own ordinary gates. The latest
[cleanup](Verification/Live16x24/g2-attribution-launch-cleanup.json) confirms
stopped Play, no isolation/pending art/borrowed view, and restored save root.

G1 default launch/travel is accepted in bounded editor Play. G2's first family,
[connected opening masonry](GRAPHICS-G2-BOUNDARIES.md), is now integrated:
552 targeted native tests pass, actual normal-start/ruin/destruction/examine
captures and independent native-size review accept the improvement. This is
one scene-finishing family, not completion of G2; pond/road/soil is also
integrated as recorded below. Continue remaining scene/framing and Player
defence/hurt coverage, then the incomplete plot/works loops and east/kitchen.
Continuous motion/audio feel, physical Deck/Vault, the full six-site journey
and packaged gates remain open. Keep this continuation active across sessions.


### Latest G2 ground continuation

Pond/road/soil family is integrated and bounded Play reviewed. Native71/71 plus
563/563 overlapping regressions, source reproduction, actual freeze/expiry bank
update, water/path walking and Harvest→Prepare support this packet. Wet20 is
native-only; no successful melt or completed scene claimed. See
[ground record](GRAPHICS-G2-GROUND.md) for exact failures, fixes, screenshots,
wrapped timing limits and restored isolated session. Remaining work includes
scene/framing and Player defence/hurt acceptance and G3's product-use debt, followed
by east/kitchen and the full six-site journey. Preserve this family's accepted
pixels unless a named defect appears.


### Historical recovery follow-up — 2026-10-24 04:36 UTC

The recovery follow-up at04:36 UTC found a historical30-slot layer table in saved
TagManager source and restored32 slots without changing any named index. Seven
standalone source checks passed; native startup and the post-fix combat capture
were still unverified at that checkpoint. A duplicate installer cache was removed
with an identical complete copy retained; disk space was still tight. The later
recovery and actual captures above supersede that blocker without rewriting its
failure receipts. See the action record above.
Independent [east/kitchen route audit](Verification/Live16x24/g3-east-kitchen-source-audit.md)
now identifies exact current source constraints, historical seed landmarks and
real service prerequisites for the next journey. It adds no art acceptance and
does not change the current G2→north products→east/kitchen delivery order.


## Latest continuation — ordinary plot loop and reader polish, 2026-10-24

The plot product packet now has actual keyboard ground resin/clod portraits,
acquired-seed planting, rain-fed stage0→1→2 growth, Marlroot harvest and finite
clod→FireClay preparation. F5/F6 preserves all197 recorded owners and10
inventory rows after canonical owner sorting; this is bounded observation
evidence, not proof of every serialized field or other zones.

Ordinary Play found and repaired two presentation faults: harvest left the
right-side FOCUS naming the removed crop, and ordinary carried seed Examine
only logged text despite existing native portrait art. Current source has
**184 distinct native passes** (57focused +127regression), zero skipped,
after behavioral RED. A second real route confirms immediate FOCUS update,
exact native seed portraits, free reading/return and replanting. Approved
sprite bytes remain unchanged. See
[plot reader record](LIVE-16X24-PLOT-READER-POLISH.md),
[actual seed reader](Verification/Live16x24/plot-products-reader-seed-carried.png),
[harvest focus](Verification/Live16x24/plot-products-reader-focus-after-harvest.png)
and [independent visual review](Verification/Live16x24/plot-reader-replay-visual-review.md).

Current Play/view/save isolation are restored; console0errors/2existing Metal
warnings. The previous locked-machine and cleanup blockers are resolved; no
new packaged build or physical Deck proof is implied. An optional local F5
checkpoint and its hashes are named in
[checkpoint receipt](Verification/Live16x24/plot-reader-replay-local-checkpoint.json);
it came from actual input and may be resumed only inside owned save isolation.

**Next:** finish remaining individual product uses/portraits recorded in
release-coverage, then the existing works forage/preparation/brew/coin loop.
The complete eight-product group, purchased crops, remaining opening scene/
action acceptance, east/kitchen, six-site release and hardware are still open.
Use the full capture/concept/extraction/native-art/integration loop when new
art is needed; do not redraw accepted sprites without a named defect. Keep
this continuation active. This is a verified slice, not graphics completion.

## Latest continuation — plot product route accepted, 2026-10-24 15:00 UTC

The existing native plot-product packet has now passed its **bounded ordinary
keyboard route**, documented in [plot products](LIVE-16X24-PLOT-PRODUCTS.md).
Twenty-four actual1280×800 captures show purchased sludge, finite compost,
rain-fed growth, gladroot ground/carried readers and eating, Marlroot seed
inspection/planting, clod preparation/FireClay reader, and resin brewing into
a burning coating. [Native gladroot reader](Verification/Live16x24/plot-products-uses-gladroot-carried.png)
and [brew reader](Verification/Live16x24/plot-products-uses-coating-reader.png)
are real game images, not concepts. Independent review found no significant
mismatch in the sampled native portraits. Approved art and runtime code did
not change in this acceptance pass; no new native-test count is claimed.

ActualF5/F6 preserves198 recorded owners and11 inventory rows, including
`Coating`/`Burning:1`, at18,7/tick1550/36drams/40HP. This is equality of the
recorded fields, not proof of every saved property or other zones. The
[local checkpoint receipt](Verification/Live16x24/plot-products-uses-local-checkpoint.json)
allows the next route to resume inside owned save isolation. Final cleanup
confirms stopped Play, released isolation/view and restored save root;
console0errors/2existing Metal warnings. No worthwhile further cache/build
removal was found;0bytes removed, current delivered archive and source kept.

**Next action:** resume that real checkpoint, travel south to north works,
and finish finite forage/preparation/brewing and currency discovery with
native visuals. The one observed brewed flask advances works-products to
partial ordinary coverage; it does not accept the other six outputs. Then
complete east connector/kitchen through the full playable capture→concept→
parts→native-art→integration loop. Do not keep replaying the accepted basic
plot route or redraw its sprites without a named defect. Carried MarlrootSeed
remains unobserved (exact ground/planting accepted); purchased crop families,
G2 scene/framing/Player defence, all-seed variants, packaged launch, physical
Deck and the complete six-site release remain open. Continue the hourly work.

## Latest continuation — works products accepted and save blocker fixed, 2026-10-24 16:01 UTC

The existing works product art passed its bounded ordinary route. Seven native
readers match source pixels exactly across named pre-/post-fix sessions. Real
crate destruction/pickup converted4coins into20drams; berries were harvested
and eaten; burr and lichen were harvested and brewed. A genuine continued-save
ID collision blocked preparation, so two runtime files now reserve loaded
numeric identities before load hooks allocate new objects. Native RED had6
expected failures; focused12 plus affected220 passed,232 distinct cases.
[Save correction](SAVE-ENTITY-IDENTITY-RESTORE.md) includes source counterchecks,
independent review, test-discovery/transport limitations and no-migration bounds.

The untouched earlier checkpoint was replayed through actual input, including
an intervening Patchbearer fight, healing and Calm. New sprigs7202→concentrate7204
→mending flask7205; F5/F6 preserved253 observed zone rows and12 carried rows.
Drinking after reload consumed the flask and healed26→33HP. Actual images and
receipt index: [works route](Verification/Live16x24/works-products-uses-index.json).
Existing already-colliding saves are not repaired; unrecorded/other-zone state
and every product use remain outside this bounded acceptance. No new sprite art.

Editor Play/isolation/save root/view restored; native console0errors/2existing
Metal warnings. The short unwrapped22.85second profile is an editor observation,
not Deck performance; the earlier long profile wrapped and only measures its
tail. No new packaged build or physical Deck acceptance.

**Next action:** east connector/kitchen via real playable greybox and captures,
imagegen reinterpretation, exact owner/state extraction, editable native sprites
and integration, ordinary controls and visual comparison. Reuse approved Cook.
The accepted basic plot/works product loops need no arbitrary repeat. Remaining
G2 scene/Player defence, north actor motion/self-treatment, incidental/purchased
content, full six-zone journey and packaged/device release remain open. Graphics
is still priority1; continue the heartbeat and regional work after the opening.


## Latest continuation — east/kitchen native route, 2026-10-24 17:07 UTC heartbeat

The two remaining surface addresses now use18 new native16×24 frames and the
unchanged approved Cook. Actual playable baseline captures led to two builtin
imagegen concepts, rejected/included part contracts, editable Blender/source
cells, a single native atlas and exact owner/state admission. Both world and
shared-reader portraits use the imported assets. See
[east/kitchen record](LIVE-16X24-EAST-KITCHEN.md) and
[actual kitchen revisit](Verification/Live16x24/east-kitchen-integrated-kitchen-revisited.png).

Ordinary keys repaired the clay pan using2FireClay, harvested Claspbean into
2pulp+1seed, collected it, saved/reloaded and walked east/kitchen both ways.
Forge inspection and real component picker worked; missing forge components
and Emberwheat grain were not invented. The approved Cook and repaired pan
portraits match native pixels exactly. Visual review found repaired-pan text
still claiming damage; this is fixed after11RED→11GREEN,152regression passes
and actual already-repaired Continue/Examine. Combined art/readout verification
has**495 distinct native Unity cases passing**, zero skipped. Independent
source and visual reviews have no remaining material finding in this slice.
[Corrected actual reader](Verification/Live16x24/east-kitchen-readout-fixed-reader.png).

Final Play/isolation/view/save-root/scene cleanup is recorded; console0errors,
2existing Metal memoryless-depth warnings. The bounded editor profile wrapped,
so its statistics describe only the32768-frame tail, not the full66.45seconds
or Deck performance. No new packaged build or physical device proof.

**Next action: scene-level working-landscape composition.** Existing east/kitchen
views have repeated hedge rectangles and parallel flower bands that overwhelm
the useful service clusters. Read the concrete cold-generation plan in
[visual review](Verification/Live16x24/east-kitchen-integration-visual-review.md).
Greybox exact opening neighbors with asymmetric courts, short broken hedge runs,
unequal flower drifts and truthful owner-backed ground. Preserve existing saved
graphs, approved sprites, mechanics, finite resources and safe approaches.
Use current actual captures→constrained imagegen scene study→real owner mapping,
then native finish and seed64/65 tests, ordinary six-stop travel and1×/2× review.
Do not substitute another disconnected sprite packet for this visible work.

Paid meal/authoritative working-ready trays, full forging and Claspbean lifecycle
remain open along with G2 defence/motion, incidental/purchased identities, full
six-zone release, package/device and regional completion. Resume the actual
18,7 kitchen checkpoint only inside owned isolation, using
`east-kitchen-integrated-checkpoint.json`. The complete release is not accepted;
keep the hourly work active. Graphics remains the release blocker and priority1.


## Latest continuation — working-landscape repetition reduced, 2026-10-24 18:12 UTC heartbeat

Exact fresh east/kitchen layouts now use unequal flower drifts and surviving
hedge fragments rather than repeated ribbons/rooms. Accepted sprites, shared
routes, finite service owners and loaded graphs are unchanged. Actual captures
fed two constrained imagegen concepts; no generated pixels were imported.
The small cold-generation mask change also clears the sampled kitchen front.
[Plan, scope and results](GRAPHICS-G2-WORKING-LANDSCAPE.md).

Native15 focused RED(5 expected failures)→15 GREEN plus197 affected regression
cases pass:212 distinct cases,0skip. Outside/Legacy hashes and real-owner/state
counter-checks pass. Source and nine-image independent reviews found no P1/P2
in this bounded change. Fresh ordinary keyboard routes on seeds64/65 visited
glade/east/kitchen, opened real pan menus, gathered Claspbean, saved/reloaded,
departed and revisited. Recorded137/169 owners match by ID and8/8 inventory rows
match after load; harvested beds stay empty. Seed64 also exercised the actual
forge picker after a real fight/Calm/tonic; no forging or fresh pan repair claim.

[Actual seed64 kitchen at2×](Verification/Live16x24/working-landscape-64-kitchen-2x.png)
and [seed65 at2×](Verification/Live16x24/working-landscape-65-kitchen-2x.png)
show native pixels and real harvested soil. Temporary presentation zoom restored;
physical Deck input remains untested. Both Play/save-isolation/view sessions are
restored, checkpoints preserved. MCP initialization timeout/late TestRunner
cleanup, known Metal depth messages and an Autosaved log misclassification are
recorded without claiming a clean transition console. Unwrapped82.97second editor
profile: renderer mean0.0600ms/p99 0.1252/max36.4736; main thread mean1.2831ms/
p99 2.4907/max888.4825; max41draws. Not a benchmark or Deck performance proof.

**Next: finish working-court framing using real owners and approaches.** Services
are clearer but still loosely arranged on broad grass, and long hedges show a
repeated bright mosaic at2×. Preserve the accepted sprite identities and new
mask result; improve place recognition with purposeful boundaries, not filler
props. The larger scene design, G2 motion/defence, service/crop outcomes, complete
six-site journey, package/device gates and regional kits remain open. No new
build exists from this pass. Continue the hourly work; graphics is still priority1.


## Latest continuation — working court integrated, 2026-10-24 19:14 UTC heartbeat

[Working-court plan and evidence](GRAPHICS-G2-WORKING-COURT.md) closes the local
kitchen framing slice:9real masonry owners and15 sparse stepping-stone owners
join the unchanged services on fresh exact-address/non-Legacy Hedgerow layouts.
The real capture→imagegen→part extraction→editable native art→integration loop
produced three16x24 ground cells; approved18 art PNGs are unchanged. Grass joints
remain plantable and original Grass is preserved. Barren paving was rejected
because it would erase original vegetation during placement/load.

272affected native Unity cases pass after recorded REDs and independent source
reviews. Actual ordinary routes on seeds64/65 reached both court orientations,
opened the pan menu and gathered Claspbean into2pulp+1seed. Seed64 entered the
unoccupied cot; seed65 had a real solid Farmer there. Actual stone reader artwork
and prose agree. F5/F6 preserved recorded owners byID and inventory on both;
seed65 border exit/return preserved all31 remaining court packet owners. The
first order-sensitive comparison is retained alongside its independent keyed
correction. Two-seed1x/2x visual review accepts this bounded scene improvement.

[Actual rotated court](Verification/Live16x24/working-court-64-kitchen-2x.png)
and [actual seed65 court](Verification/Live16x24/working-court-65-kitchen-2x.png).
Both editor sessions are stopped/restored and checkpoint saves preserved. The
68.59second unwrapped editor profile measures idle/zoom/look/menu/tool overhead,
not walking or Deck performance; max41draws, renderer mean0.05267ms/max45.63ms.
No new build, physical device proof or full release acceptance is claimed.

**Next:** close the visible incidental-actor native coverage gap, starting with
this actual Farmer, after auditing existing approved actor routing. Also retain
an existing multi-owner Look heading/reader discrepancy as a named UI follow-up;
do not misreport Player-covered floor Examine as passed. Then continue G2
motion/defence, paid meal/ready trays, full forge/Claspbean lifecycle, complete
six-zone journey, package/device acceptance and regional kits. Preserve accepted
court and landscape work. Graphics stays priority1 and the heartbeat continues.


Post-push continuation: `679ae9e2b6fae1b46eff8430b9e9066fc15cb331` was verified
on server main with all104owned blobs matching. The
[storage audit](Verification/Live16x24/working-court-storage-audit.md) reclaimed
**0bytes**: only the current delivered archive remains and current Unity caches
are active. Free space was2.25GiB. All source/saves/checkpoints/current archive
remain preserved; the older delivered package does not include these graphics.

The [next actor audit and plan](GRAPHICS-G2-INCIDENTAL-ACTORS.md) distinguishes
missing art from a missing connection: Farmer needs a distinct native frame;
ReedbackGrazer can use the user's unchanged approved `03-ridge.png`, currently
unconnected to live flat selectors. Magpie/Viper/Scrabbler/Cook already have
native routes. Begin with source-checked RED, actual capture-based Farmer concept
and native editable source, then narrow owner routing and ordinary move-away /
examine evidence. Do not silently alias inherited identities or reset the court.
The named Cindercaller gap and other release gates remain open. Keep scheduled
work active; this checkpoint completes one visible slice, not the overhaul.


## Latest continuation — two real actor gaps closed, 2026-10-24 20:19 UTC heartbeat

[Incidental actors](GRAPHICS-G2-INCIDENTAL-ACTORS.md) now has bounded ordinary
acceptance: a newly authored editable16×24 Farmer replaces the kitchen cot
glyph, and the user's unchanged approved Reedback Grazer is connected to the
actual eastern-route animal. Both exact subject readers show the same body.
Native RED2/2 preceded production; final311+277distinct Unity cases pass
(588total,0failed). Source/production reviews found no P1/P2. Existing23approved
art files stay unchanged; no AI/world-generation/motion policy was altered.

[Actual Farmer beside Cook](Verification/Live16x24/incidental-actors-65-farmer-2x.png)
and [actual Grazer after opening its pen](Verification/Live16x24/incidental-actors-64-gate-open.png).
Seed65 ordinary Continue, movement and F5/F6 retain the Farmer/cot and8inventory
rows. Fresh seed64 ordinary travel, Examine and real Open-door interaction make
Grazer5036 move51,17→50,18; F5/F6 matches300owner/6inventory rows at tick1000.
These are two static body identities, not new animations or a whole-bestiary claim.
Farmer move-away/underlying Bed repaint has a native test, not ordinary replay.

Both editor sessions are stopped/restored with null save override. Native console
has0errors and2known Metal warnings. The206.37second editor profile wrapped its
131072sample ring; retained renderer mean0.05170ms/p99 0.10325ms, max43draws.
This includes idle/UI/load and is not a complete-window/device benchmark. No new
package was made; the retained delivered SteamDeck archive still predates this art.

**Next: execute [ordinary paid kitchen service](GRAPHICS-G3-KITCHEN-SERVICE.md).**
The source-checked route uses real glade grain and cellar clay, repairs the pan,
commissions with carried pulp/drams, waits real turns, collects and reads a meal.
Current neutral trays suffice for the first acceptance; derive any new working/
ready cue from actual captures and authoritative job/custody, not arbitrary stock.
Retain Cindercaller, aggregate Look heading, intermittent court overlay, G2 defence/
motion, complete crop/forge loops, six-site journey and packaged/device gates as
open. Graphics remains priority1; the hourly continuation remains active.

Post-push/storage: `3e67ab983a543a4fd1851a89e07df863bd603975` was verified on origin/main with all84owned
blobs matching. The [storage audit](Verification/Live16x24/incidental-actors-storage-audit.md)
removed nothing (0bytes recovered); useful caches and the current archive are
preserved. Free space was2.19GiB. Continue the source-checked kitchen
service route next; full release and updated package remain open.


## Latest continuation — kitchen service visibly integrated, 2026-10-24 22:10 UTC

[Kitchen service](GRAPHICS-G3-KITCHEN-SERVICE.md) now has ordinary input evidence
from glade grain/cellar clay through repair, paid batch, pickup, reload and meal
consumption. The subsequent capture→imagegen→editable16×24→real-integration pass
adds a covered paid pan and the actual commissioned parcel on its board. Approved
base art is unchanged. The new cues survive Working/Ready reloads and disappear
on real collection; [actual ready-board Examine](Verification/Live16x24/kitchen-service-states-board-reader.png)
and [Working world](Verification/Live16x24/kitchen-service-states-working-2x.png)
are gameplay captures, not concepts. Eleven final captures, exact submitted-tile
receipts and independent visual review accept this bounded service sequence.

Native evidence:481 distinct passing cases,0failures,5explicit unavailable-GC
measurement skips. A known-allocation calibration prevented a false zero-GC
claim; a source-reviewed matcher removes the identified repeated Split path.
Separate quiet-frame tests pass.85.12s Editor profile is unwrapped but includes
save/load/tool overhead; max renderer29.699ms/MainThread959.029ms are disclosed,
not converted into physical Deck FPS or improvement claims.

Play is stopped; owned save/view/seed/preferences restored. Final console has
0errors and2existing Metal depth warnings. Current delivered October11 archive
is preserved and still predates these graphics. **No new package was built.**

Next: remaining visible Player identity/action feedback and scene defects;
then incomplete crop/forge states and the literal complete six-zone journey,
followed by packaged/device gates. Global fallback is not dedicated all-world
art. G2/G3 and the overall graphics release remain open. Continue this priority;
do not stop the heartbeat because this one service slice passed.


Post-push/storage: `3303d6240ab830bb79d701c3d7ca7f9683bf9566` was verified on
origin/main with all133owned blobs matching. The independent
[storage audit](Verification/Live16x24/kitchen-service-storage-audit.md) confirms
the current archive's unchanged hash, preserved saves/source and unrelated
local notice variants. No worthwhile unused cache/build candidate remained;
**0bytes deleted**, about1.62GiB free. Unity play is stopped, though the Editor
and import workers remain open; preserve their useful active caches. Continue
Player identity/action feedback next. No new Deck package or whole-release
acceptance is implied, and the graphics heartbeat remains active.


## Latest continuation — Cindercaller native body, 2026-10-24 22:43 UTC

[Exact Cindercaller integration](GRAPHICS-G2-CINDERCALLER.md) replaces a real
red-g fallback in the east wilderness with an editable16×24 native body and
matching exact-subject portrait. The actual baseline→imagegen→native Blender
cells→integration loop is recorded; all65 protected art hashes are unchanged.
The source sweep corrected the earlier Player-identity premise: Cindercaller is
an NPC, and the accepted Player body/motion stay unchanged.

[Actual Examine](Verification/Live16x24/cindercaller-native-reader.png) and
[actual moved/reloaded world](Verification/Live16x24/cindercaller-native-reloaded.png)
verify visible integration. Ordinary keyboard movement advances tick5250→5310,
moves the real caster78,7→78,5, and keeps its new body. F5/F6 matches all6 recorded
actor/18inventory rows. Native behavioral RED2 precedes final334distinct passes,
0fail/skip. Independent code/source/gameplay reviews find no significant issue.
This is one static identity, not a cast animation or whole-bestiary completion.

Owned Play/save/view/preference state is restored. The250.934s Editor profile
wrapped its131072-sample rings; retained max45draws and renderer mean0.05664ms
are not physical Deck or full-window performance. Only the two existing Metal
memoryless-depth diagnostics remain; MCP labels them Error while Editor shows
warnings. No new script/exception diagnostic was found. No new package was made.

**Next:** separately fix and test the direct Riposte counterstrike's missing
Attack visual/facing event using the saved
[action-source audit](Verification/Live16x24/riposte-action-source-audit.md).
Preserve accepted Player art and existing parry/guard outcomes. Then complete
crop/forge lifecycles, the literal six-zone journey and packaged/device gates.
G2/G3/full release remain open; the graphics continuation stays active.


Post-push/storage: `c76654faa17c0162bb6390007951ff37b14a657f` was verified on
origin/main with all59owned blobs matching. The independent
[post-push audit](Verification/Live16x24/cindercaller-storage-audit.md) confirms
source/saves/current archive and both unrelated local Notice variants preserved.
About1.71GiB is free; no worthwhile confirmed stale cache/build candidate remains,
so **0bytes were deleted**. The active Editor's useful caches are retained. The
current delivered archive still predates this art. Continue the next bounded
action-feedback slice; no whole-release or packaged acceptance is implied.


## Latest continuation — Riposte and travel status feedback, 2026-10-25 UTC

## Delivered source and native verification

- Riposte now explicitly opts into the existing facing/Attack event before its
  actual single-strike resolution. Local exception containment protects combat;
  reserved/spent reentry gates and ordinary multihand/default-direct callers
  remain unchanged. No art replacement. Native `riposte-feedback-final.xml`
  passed206/206 at23:28:24–40UTC, including20 focused cases; no skips/failures.
- Player owner-turn status context now uses its canonical physical/current
  managed zone rather than stale Brain.CurrentZone. NPC context policy remains
  untouched. `status-zone-red.xml`:11 total,7 intended failures/4 passing controls.
  Focused GREEN11/11; `status-zone-regression.xml` passed274/274 at00:04:08–18UTC,
  no skips/failures. Source review found no remaining P1/P2.
- Exact XML full-name comparison:206 and274 are separate selected sweeps with
  **37 overlapping FlatPixelActorMotionTests**, hence **443 distinct cases**.
  Status focused11 is already included in274. Do not report480/491 unique tests
  or imply this was the entire project suite.

## Ordinary evidence and limits

The Warden earned/used a skill point through ordinary play and learned Riposte.
The19-frame north capture `G2Action-20261024-234223-659` establishes actual Player
Hurt pixels at01/07/11, Guarded/En garde words at01, ordinary Attack at15 and
recovery to Idle. Independent source+PNG review was completed. Guarded is not a
full parry. No captured attack has reactive counter provenance; **ordinary
Riposte remains unobserved**, despite its native event/renderer tests passing.

That capture exposed old-zone periodic damage while fighting in11.9.0. The
status fix above is natively proven; a separate ordinary post-fix poison/Hurt/
numeric replay is **still pending**. Do not reuse pre-fix pictures as acceptance.

The forge route harvested FrostLichen and root observed brewed output8995, but
the24-frame east encounter `G2Action-20261024-235149-310` interrupted the approach.
Root observed the blade-bearing hand severed, both tonics consumed and eventual
world-map retreat at5HP. The limited captured window ends earlier at1HP; later
events/session observations are separate evidence. No tempering, forged output,
on-hit freezing, full parry or Riposte is accepted. Preserve the failed route.

Cleanup `riposte-feedback-cleanup.json` records stopped SampleScene, isolation/
artPending/viewBorrowed false and null save-root at23:56:08Z. This is that
checkpoint, not proof of every subsequent editor state. No current build was
replaced; packaged/device/performance gates remain unchanged.

## Next bounded work

Parent-selected next art slice is **LongSword**, after its exact identity/source
audit; it is not already authored or accepted by this packet. Keep approved
Player art unchanged and use the established concept→native→integration→actual
reader/world pipeline. Before resuming the injured character, inspect actual
save location, HP/effects, missing limbs, equipped/carried weapon and brewed8995
custody. Do not assume the severed weapon is carried or world-map time is safe.
Use reachable ordinary recovery/services; no grants, forced counters or erased
failed approach. A separately labeled fresh game is possible if recovery is
not safely attainable. Complete forge/crop loops, six-site continuity, ordinary
counter acceptance and packaged/device validation remain open.


## Latest continuation — LongSword integrated, 2026-10-25 00:25UTC

See `Docs/GRAPHICS-G3-LONG-SWORD.md` and `Docs/Verification/Live16x24/long-sword-play-summary.json`. ExactLongSword now has native16×24 world/Examine art from actual baseline→imagegen concept→editable Blender cells→runtime import. Initial2behavioralRED failed as intended;37focused and391distinct affected native regressions passed, no skips. Ordinary pickup/equip, matching ground/equipped readers, F5/F6 and cellar descent/ascent accepted for the same2699owner. Old Player/items/3D and user saves preserved. This does not complete forge/crop/combat/six-zone/package/Deck gates.

Correction to earlier instructions: the injured5HP east-route disposable save was not exported before NativeSaveIsolation cleanup and cannot be resumed. Preserve its failed-route images/events. The sword pass separately replayed the unmodified earned healthy branch and exported its final checkpoint to `/tmp/coo-long-sword-verified-checkpoint` before cleanup. It is42HP, level2, RP1, glade28,19 with2699equipped and no brewed8995; confirm live state on continuation. No forged result or recovered severed weapon is implied.

Root stopped Play and restored ordinary SampleScene/settings/save root at00:25:02UTC. Follow the connected ordinary material→forge route next; verify remaining combat/status feedback when naturally encountered. Do not author disconnected packets to avoid outstanding gameplay gates. Existing current build unchanged. Graphics/storage heartbeat remainsACTIVE every10minutes, checking safe candidates only after successful remote preservation; no useful cache purge. Bounded inspection removed0bytes and preserved source, saves and current archive.


## Latest continuation — ordinary forge and crop loops, 2026-10-25 01:05UTC

[Actual connected route](GRAPHICS-G3-FORGE-CROP-JOURNEY.md) now accepts north
FrostLichen harvest→frozen coating brew→east forge quench of the same2699 blade,
ordinary re-equip, matching native Examine and F5/F6. TemperCount1, Frozen40,
HP8/8 persist; no actual Frozen hit or component-forged weapon is claimed.

The same ordinary run reached Orven and completed Claspbean harvest→seed
planting→Rain→same13511 seed/sprout/ripe stages→harvest/pickup/reload→second seed
replant/reload. Native stages and reader are actually visible. Final kitchen
25,14,42HP,tick2990 has4pulp, cultivated12611 and dry new13515stage0. Its exported
checkpoint is `/tmp/coo-forge-kitchen-final-checkpoint`; do not copy its audit
marker. Failed prior east-route evidence remains separate.

Independent source/PNG review accepts these bounded loops. Existing accepted
art is unchanged; no runtime changes or new native-test result in this packet.
Actual screenshots and raw receipts are indexed in `forge-kitchen-play-summary`.
Play/settings/save root restored at01:03:46UTC;0errors,2existing Metal warnings.
No updated package or Deck acceptance. Full G2/G3/six-site/regional release is open.

**Next named defect:** east EOF frame18's lethalNPC1 briefly overlays the departing
Player tween, although logical damage ownership is correct. Fix presentation
occupancy with RED→GREEN and an ordinary replay before further disconnected art.
Actual Riposte, post-travel Player periodic hurt, Frozen proc and pulp application
remain unobserved. Rain tooltip/historical growth-doc wording is a separate
followup. Continue the active10minute graphics/storage schedule; push exact
owned work before safe stale-cache inspection, preserving useful active caches.


## Latest continuation — moving-body readout guard, 2026-10-25 01:25UTC

[Named readout correction](GRAPHICS-G2-TWEEN-READOUT.md) now supplements logical
cell occupancy with the actual disclosed native Walk body. This prevents a
neighbor's receipt painting across a departing actor without changing damage,
recipient, motion, fog or accepted pixels. Seven intended RED failures preceded
production. Wider native run:285passed,0failed,1 calibrated allocation-counter
skip across286 cases, including the focused61; counts are not additive.
Independent geometry/authority/lifecycle review found no actionable P1/P2.

Ordinary replay used the unmodified prepared checkpoint, normal SampleScene,
Continue and real controls.24 EOF frames show En garde, melee, Hurt/death and
Walk A/B with42HP maintained. The Scrabbler died on the first15-point strike,
so the former departing-player/periodic-lethal overlap did not recur. Native
geometry proves the correction; exact live reproduction, ordinary Riposte and
post-travel Player periodic Hurt remain unobserved. Evidence and hashes are in
`tween-readout-play-summary.json`. Profiling's ring wrapped; retained tail
includes idle/capture/tool overhead and is not a performance or Deck gate.

The distinct untempered replay branch was exported before cleanup to
`/tmp/coo-tween-replay-checkpoint`. The accepted frost-tempered/crop checkpoint
remains `/tmp/coo-forge-kitchen-final-checkpoint`, kitchen25,14,42HP,tick2990;
confirm its real state on resume and copy only the actual character folder.
Root stopped Play and restored SampleScene/view/preferences/null save override
at01:24:42UTC. Direct console0errors,2existing Metal warnings. Current build
and save backups remain intact. Repeated random counter hunting must not delay
the six-site continuity/scene gate; record ordinary feedback if it occurs
naturally. No full G2/G3, packaged, device or regional release is claimed.
