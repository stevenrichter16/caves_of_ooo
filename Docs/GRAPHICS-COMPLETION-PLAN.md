# Graphics completion plan

**Chunk composition requirement (2026-10-26 local):** The user also requires
chunks not to feel too bare. Fill regions with coherent, biome-appropriate
terrain variation, flora, props and landmarks while preserving readable routes,
hazards and interactions. Judge actual whole gameplay views, not only sprite
sheets. See `Docs/GRAPHICS-WORLD-SPRITE-COVERAGE.md` for the acceptance gate.

Current priority, 2026-10-27 03:40 UTC: **fill missing and inconsistent sprites
across the entire game before nuanced animation polish.** This is the user's
latest direction and supersedes the earlier opening-first polish queue. Cover
all biomes and areas, including surface, underground, towns, interiors, dungeons
and special sites. The six connected opening scenes remain a valuable verified
anchor; they are not the boundary of this graphics journey. Preserve accepted
art and existing animation, but defer optional pose refinements while uncovered
regions and object families remain. See the dated continuation below.

Delivered baseline remains North source2375284dd and its Mac/Linux packages:
278 native passes,1 unsupported allocation-counter control skipped; bounded
Editor and Mac gameplay accepted. The East caster baseline was subsequently
captured with real movement/combat/casting, and source-only motion derivatives
were preserved. **East motion is deferred, not integrated or accepted.**
Normal saves, previous builds and unrelated work remain preserved. Storage
reclamation remains inspection-only.

Hourly report checkpoint:2026-10-27 03:25UTC; next04:25UTC. Reports must name
biomes/areas and sprite families addressed, verified gaps closed, remaining
missing/unaudited coverage, and concrete next coverage goals, with test/capture/
main-push status. See the [run inventory](GRAPHICS-RUN-CHANGELOG-2026-10-26.md).

Initial status, 2026-10-24, retained as historical context: **G1 delivery
implemented and accepted in bounded ordinary Unity Play; opening/region art
and packaged/device release remained incomplete.** Initial
audit was 2026-10-23 against `ce2fe36e7`; its findings below are historical.
As of 2026-10-24 UTC, normal fresh/Continue launch defaults to flat16×24,
with a reversible Graphics choice and continuous projection during travel.
See [renderer evidence](GRAPHICS-G1-RENDERER.md) and
[launch evidence](GRAPHICS-G1-LAUNCH.md). G2 connected masonry is now integrated
and accepted within its bounded ordinary keyboard route; see
[wall evidence](GRAPHICS-G2-BOUNDARIES.md). Pond/road/soil are also integrated
with bounded ordinary Play acceptance in [ground evidence](GRAPHICS-G2-GROUND.md);
wet-soil ordinary validation and full scene acceptance remain open.
That checkpoint was not complete regional artwork or a new Steam Deck build.

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

The current production priority is **complete native sprite coverage across all
biomes and areas**, with coherent playable slices delivered throughout the journey.
Do not require the opening's optional animation polish to finish before covering
unrepresented regions. Keep normal launch/travel and essential state readability
working as this coverage expands. A coherent fallback permits travel
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
   palette clusters and gameplay-essential state variants. Optional animation
   poses are deferred until broad sprite coverage is filled. Give each a real owner, footprint,
   pivot and state source. Art must never imply a resource, opening or service
   the simulation does not have.
5. **Author natively.** Produce editable exact-resolution sprites/frames.
   Imagegen is the design assistant, not the final pixel-grid exporter.
   Preserve approved bases. New ordinary sprites and essential state variants
   take priority over optional animation derivatives, which remain separate.
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
- Preserve existing animation. Until broad sprite coverage is filled, only
  prioritize animation work for a demonstrated gameplay-readability blocker.
  Later animation polish explains movement, attack contact, defence and state change.
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


## Next executable slice — six-site service and return, 2026-10-25 01:30UTC

The moving-body correction is on remote main at43b43c6a6; its82 exact owned
files were byte-verified remotely. Post-push storage inspection found4.96GiB
free and no worthwhile safe stale candidate;0bytes removed. Current archive,
editable source, user saves and all checkpoint branches remain protected.
The active graphics/storage heartbeat is every10minutes, not mandatory deletion.

Next use the accepted **frost-tempered/crop** checkpoint, not the separate
untempered tween replay. Start at kitchen25,14,42HP,tick2990 and verify actual
2699blade,2clay4716,4pulp and dry13515. One continuous service-and-return route
has more release value than another disconnected item packet or RNG hunt:

1. Water the existing13515, repair the actual pan using carried clay, then ask
   Orven **“ask Orven to vouch for the repair.”** The real CookIntroductionPart
   requires this player's recorded repair; do not synthesize knowledge.
2. Walk kitchen12.11→east12.10→glade11.10. Revisit the forge, descend/return from
   cellar11.10.1, then continue north works11.9→Nella11.8. Capture real approach,
   interactions and return state, retaining finite depletion and owner identity.
3. Inspect public/tied beds, read Nella's terms, and choose **“relay Orven's
   introduction”** if rights are Unknown. Legitimate grant avoids8drams. Inspect
   and harvest one authorized reserve product, replant if supplied, and verify
   the ground cord persists. Do not manufacture theft or grant state in code.
4. Return through the same route to kitchen. Verify crop13515's actual growth,
   repair, introduction and custody. Save, stop gameplay, Continue and compare
   the same owners. Export the real checkpoint before isolation cleanup.
5. Fix only measured blockers in that flow. The existing ripe Claspbean Examine
   reader has visibly cramped prose leading; inspect actual glyph bounds,
   pagination and footer clearance at1280×800 before a small typography fix.
   Source places body rows one cell apart with1.6× glyphs, but the screenshot
   does **not** prove duplicate text or actual vertical overdraw. Keep the
   approved ledger style. Runtime corrections still require RED→GREEN.

Accept only a continuous six-site receipt, readable readers, correct return to
world, truthful permission/state cues and preserved finite custody. Record
encountered fallback identities rather than pretending every incidental item
is complete. This checkpoint-based pass does not prove fresh-New-Game continuity,
packaged activation or physical Deck. The retained **October11 current package
still predates this graphics work**; update packaging after the connected
acceptance, and keep hardware proof separate. Ordinary Riposte, Player periodic
hurt, Frozen proc and pulp application remain named unobserved opportunities,
not reasons for unbounded random encounter hunting.


## Latest continuation — six-site journey and readable Examine, 2026-10-25 02:03UTC

[Connected service journey](GRAPHICS-G3-SIXSITE-JOURNEY.md) now has an ordinary
earned-checkpoint route through kitchen, east shelter, glade, cellar, north
works and Nella, then back. Actual2clay repairs pan13489; Orven13471's
introduction carries cause`ed272954d6e94cedbb3937df123dba5a`; Nella15712 grants
reserve rights without8drams. Pitchpod15729 yields2resin+1seed; real planting
and Rain create/water15735. On kitchen return13515 is ripe and the repair/
knowledge persist. All26 sampled captures are flat16×24 and42HP, not concepts.

[Reader leading](GRAPHICS-G3-READER-LEGIBILITY.md) preserves the font size and
approved portrait while increasing body baseline advance to1.25 and reducing
page capacity to20 within the same37-row maximum. Seven intended native RED
failures precede production;108 distinct GREEN cases pass with0fail/skip.
Independent source/image review finds no P1/P2; the actual same paragraph has
4–5 clear pixel rows between lines instead of1. Ordinary two-page sword reader
forward/back/close and return to world preserve tick7160.

The final exact character checkpoint is `/tmp/coo-sixsite-final-checkpoint/`
(character`70a87e9febf34b499e8aaf47a44601ed`). Its Quick333298bytes SHA256
`c9add40b3996a82f5d7d9d196e78ba19c7c7a29e1a25ceec33f0e7dfd701db56` is
recorded with all four files. Normal stop/C Continue matches all ten sampled
gameplay fields. Do not copy audit markers or replace prior accepted branches.
Root restored Play/settings/view/scene/null save override at02:02:13UTC;
console0errors,2existing Metal warnings. No new performance measurement.

**Next:** verify exact owned push, inspect storage, then prepare a current
packaged flat build; October11 current archive still predates these graphics.
Keep launch/package/hardware evidence separate. A bounded prepared-bed cue
should follow: original decorative “cut emberwheat row” masks cultivated soil
after reserve harvest; real seed planting works. Preserve historical row names
and add current/disclosed bed context through the shared readout with native
RED/counters. Fresh-New-Game whole-journey, incidental native art, ordinary
Riposte/Player periodic Hurt/Frozen proc and full regional coverage remain open.
Do not turn these named gaps into endless RNG hunting or disconnected art.


## Latest continuation — real packaged flat delivery, 2026-10-25 02:22UTC

[Packaged delivery](GRAPHICS-G1-PACKAGED-DELIVERY.md) closes the stale-build gap
for runtime source `9ffd7c973`. The normal production scene built for both Mac
and Linux with **0 errors / 4 warnings each**. Actual standalone Mac/Metal
Continue, crop Examine and movement use the flat grid without a forced art
preference. Four unchanged window captures are JPEG1280×856, including56pixels
of macOS chrome; the game viewport is1280×800. These are packaged gameplay,
not concepts or editor-only captures. Shutdown disposal warnings are recorded.

The new Linux archive is
`Builds/SteamDeck/2026-10-25-flat16x24-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`,
128610942bytes, SHA256
`76806cdcaf37c49883820f160fa2076963760b9d27fa5d27c4f243127c586bd6`.
All181 archived regular-file sizes/hashes/modes and paths passed streaming
verification. Current audio credits/licenses and a corrected README are included.
The October11 archive remains preserved. Linux/Vulkan execution and physical
Deck behavior/performance are **not** accepted by a Mac build or archive check.

Original770 user-save files (93965260bytes), names/hashes and both preference
domains are restored and verified. Temporary accepted checkpoints remain outside
the repository. Root returned the editor to StandaloneOSX, out of Play, no test
run/isolation, and restored reviewed build-generated serialization. No code or
approved artwork changed in this packaging pass. Native108-case reader evidence
belongs to the prior change, not a new test run. No new performance claim.

**Next:** the actual harvested reserve-bed ambiguity remains the smallest
measured presentation correction. Add neutral “prepared planting bed” context
through `CellStatusReadout` only for a current disclosed canonical cultivated
soil owner, suppress it for an actual current disclosed crop, and retain hazard
wording. Do not create TileState, reveal hidden owners, query/reconcile claims,
rename the historical row or promise planting permission. Follow native RED→GREEN
with stale/foreign/hidden/live-crop/hazard/read-only counters and an ordinary
harvest/reader capture. Then continue opening composition and uncovered identities
using the required actual-greybox/capture/imagegen/extraction/native-art loop.
Fresh-character whole journey, all owner/state kits, exact ordinary Riposte and
periodic Player-Hurt receipts, Frozen proc and regional coverage remain open.


## Latest continuation — prepared beds readable after harvest, 2026-10-25 02:55UTC

[Prepared-bed readout](GRAPHICS-G3-PREPARED-BED-READOUT.md) now exposes neutral
“prepared planting bed” through the shared current-ground service. Exact current
cell/zone and the same visible owned physical soil are required; a current
visible crop suppresses it. Original row names/prose, planting rights and growth
remain unchanged. Existing live hazard text/order remains intact; stale/inactive
queries are deliberately suppressed and tested.

Native RED25 had23 expected cue failures before production. GREEN72 plus the
expanded28-case fixture gives **75 distinct passes,0fail/skip**,28new cases.
Independent review found no significant defect and supplied three additional
hazard-authority counters. The real earned seed64 replay harvested7199, collected
2resin+seed7202, compared the exact empty soil5524/row5525 at tick1570, then planted
7203 through inventory. Actual C-menu/Examine show the new cue; the replanted
crop loses it and reports dry growth. F6 restores the empty checkpoint and actual
Look/FOCUS shows the cue. Nine accepted1280×800 captures plus one rejected Pause
attempt are retained; this is gameplay evidence, not concepts. Cleanup at02:54UTC
restores noPlay/noisolation/null save override; console0errors/2existing warnings.

The exported empty checkpoint remains at `/tmp/coo-prepared-bed-empty-checkpoint/`
(character33db4c719b3b4a5baba1d70a23632cd1). Current Mac/Linux packages use9ffd7c973
and **predate this follow-up**; do not claim the cue has already been packaged.
No approved art changed and no new performance or physical Deck result is claimed.

**Next:** fresh-New-Game whole-scene acceptance. Use normal chooser and owned
isolation with no copied checkpoint or forced art. A meaningful finite route is
glade→cellar→north works→Nella→glade→east forge→kitchen, then save/stop/Continue;
pay Nella's actual8drams rather than replaying the already-proven introduction
journey. Inspect real current positions, threats and finite materials. Do not
force progress through danger or pretend a historical coordinate is current.
Prior actual images suggest two composition checks: repeated bright north hedge
bands and Nella's working beds competing with decorative brown fragments.
Reassess fresh captures before changing accepted art; use the required imagegen/
part extraction/native production loop for any warranted art change. Ordinary
Riposte/periodic Player-Hurt/Frozen proc remain opportunistic gaps, not RNG hunts.


## Latest continuation — northern field composition and earned use, 2026-10-25 03:37UTC

[Fresh-origin journey](GRAPHICS-G3-FRESH-OPENING-ACCEPTANCE.md) now reaches glade,
cellar and northern works through ordinary inputs. The fresh north frame named a
specific defect: repeated bright hedge rectangles overwhelmed its field. Following
the actual capture→imagegen→part extraction loop, the
[north composition pass](GRAPHICS-G3-NORTH-COMPOSITION.md) reuses accepted native
Hedge/Grass art and subtracts selected boundaries only in fresh nonLegacy exact
north Fallow plans. Original pocket repair runs first; no RNG draw, per-frame
work, other planned terrain cell, hidden knowledge or saved graph is changed.

Native RED22 had5expected composition failures; GREEN138/138,0fail/skip.
A60-plan comparison verifies15exact-scope Hedge→null-only changes and45unchanged
outside/Legacy cases. Independent source and actual screenshot reviews found no
significant defect. Seed64 matched entry is less repetitive with retained cover;
the existing placement system legitimately moves the works to22,17. Real combat
forced a retreat and consumed two carried tonics before an ordinary Lunge kill.
Normal harvest of both finite sources, reagent selection and brewing produced
frozen-and-stoneskin coating6974. F5→glade autosave→F6 latest-load→ordinary return
preserved32HP, exact inventory and spent sources. See north-composition-play-summary
and actual fresh-opening-north PNGs. This is bounded acceptance, not full-scene
or six-site completion. No new native sprite or benchmark is claimed.

Owned cleanup restored normal scene/preferences/view/save root at03:37UTC,
console0errors/2existing warnings. Current Mac/Linux packages predate this change.
Continue the earned checkpoint at `/tmp/coo-fresh-opening-after-north/`
(character20cb263ebed24a8380168954e23981ca; tick2260/23,17north). It contains the
real coating, one remaining Stoneburr/FrostLichen each,2FireClay,32HP,50drams;
no healing tonics remain. Next: Nella's legitimate8dram access, crop/watering,
east forge and kitchen, with a seed65 ordinary visual counter-pass. Reassess
Nella's actual working-row hierarchy before changing art. Remaining feedback,
regional kits and physical Deck gates remain open. Preserve all prior checkpoints.


## Latest continuation — wide popup backing, 2026-10-25 04:31UTC

[Wide popup correction](GRAPHICS-G1-WIDE-POPUP-BACKING.md) fixes actual vertical
background gaps observed in inventory Examine at wide window sizes. Only the
shared background quads scale with their existing grid; glyphs, portraits and
input geometry retain their owners. Native RED5:2pass/3expectedfail; GREEN93/93,
0fail/skip. Actual ordinary keyboard before/after and resize-to1280×800 captures
show solid backing; Escape returns the same inventory selection. No new build
or physical Deck/performance acceptance. User's latest existing save was backed
up exactly after they had already stopped their normal Play session.

The user now explicitly adds inventory redesign/polish to graphics completion.
[Inventory plan](GRAPHICS-G3-INVENTORY-REDESIGN.md) and
`ArtSource/Live16x24/concepts/inventory-ledger/` capture the actual baseline,
imagegen exploration, extracted layout and rejected fabricated controls/art.
Preferred next slice: retained camera-bound full-screen ledger over existing
inventory state/commands, actual anatomy, native owner portraits, current facts,
all five modes and truthful device hints. Prove popup layering/pointer geometry
first with native RED/GREEN and actual GPU captures. Nella's ground plan/concept
are preserved, pending implementation after this user-prioritized screen.


## Latest continuation — retained inventory ledger, 2026-10-25 06:10UTC

The user's requested [inventory redesign](GRAPHICS-G3-INVENTORY-REDESIGN.md)
is implemented over the actual five existing modes and transactions. It follows
the approved baseline→imagegen concept→extracted layout→native retained UI
pipeline, reuses accepted owner portraits and preserves real anatomy/ownership.
New hierarchy separates equipment/status, carried items with quantity/weight,
selected facts and explicit actions. Native controller Commands expose otherwise
keyboard-only choices; F1/full details and F2/status return to the same owner.
A real mouse failure discovered in Play is corrected at the InputSystem boundary,
including stationary pointer on Open. Bound combat skills now use the existing
authoritative ability descriptions rather than an empty grimoire-only fallback.
The shared bitmap text normalizer also converts authored bullet separators to
plain single-width hyphens; final shared UI validation passed49/49,0skipped.

Native broad regression passed480/480; final ability/mouse/transaction affected
run passed233/233,0skipped. These overlap; do not sum them as unique coverage.
Actual isolated Play covers all five modes, item facts/Examine/throw cancel,
mouse equipment removal/re-equip, real finite brewing, grimoire selection/details
without binding, full status, En Garde/Lunge/Rain/empty-slot facts and actual
1280×800→1920×1080 resize. See `inventory-ledger-live-review.json` and actual PNGs.
Idle sample35.9seconds has zero additional inventory Render calls, with valid
21draw/batch p99 counters. This is bounded Editor evidence, not a speedup,
physical Deck acceptance or a packaged release. Current packages predate this.

Next world slice remains Nella's working soil, using the preserved plan/concept
and existing frames18–20. The current normal-save/scene state is protected and
root owns testing; the plan's older pending-permission paragraph is obsolete.
The earned after-North checkpoint is untouched. Do not replace its finite
progress with artificial grants. Continue Nella's8dram access/harvest/replant/
Rain, glade/eastforge/kitchen, seed65 counter and remaining regional coverage.

Final inventory acceptance: actual corrected Rain and return-to-world captures
complete the scoped Editor pass; cleanup06:16:08UTC restores all borrowed state.
Normal save/backups match. Native49/49 shared UI final check also corrected an
old13-discipline test pin to the shipped14 including Throwing; browser runtime
unchanged. Current packages and physical Deck acceptance remain open. Resume
Nella world graphics from the protected after-North checkpoint next.


## Latest continuation — Nella soil and earned crop loop, 2026-10-25 07:01 UTC

[Nella working soil](GRAPHICS-G3-NELLA-GROUND.md) now reuses accepted native
old-field/dry/wet frames18–20 in exact admitted11,8. Both rendering routes use
current owned crops; a shared-part ownership counter was RED before correction.
Native GREEN272/272, zero skipped. Independent source and actual-image reviews
accept the bounded material change, leaving the larger repeated field layout
and loosely composed service area open.

Actual seed64 earned-route Play paid8drams, harvested/picked up2resin+1seed,
replanted the seed at18,6, cast Rain and waited normally to observe wet20→dry19.
F5→south border→F6 latest save→north return retains crop9193, paid rights,42drams,
32HP and the two resin. Actual screenshots/receipts are `nella-ground-*` in
Verification/Live16x24. Earlier null painted readouts were helper-coordinate
errors, preserved and corrected in later receipts. No approved sprite pixels
or gameplay system changed. Profiler tail is explicitly bounded/wrapped, not a
speedup or physical Deck claim. Current builds predate inventory and this pass.

New earned checkpoint `/tmp/coo-fresh-opening-after-nella/` contains character
20cb263ebed24a8380168954e23981ca at tick3510,18,7 Nella, with the real coating and
finite route supplies intact. Normal save hashes and owned Editor restoration
are verified. No stale cache target justified deletion; zero bytes removed.
Next: resolve confirmed held-action input fallthrough if source audit reproduces
it, continue fresh-origin east forge/kitchen, then next scene-composition cycle
and seed65 visual counter. Soil acceptance is not complete six-zone/world art.


## Latest continuation — inventory action release, 2026-10-25 07:26 UTC

The [held inventory shortcut fix](GRAPHICS-G3-INVENTORY-ACTION-RELEASE.md)
closes the actual planting→unwanted-movement defect found during Nella Play.
Native RED reproduced it; GREEN124/124 (4new,120controls),0skip. Ordinary heldB
300ms plants once at18,6 while Player stays18,7; freshB90ms moves17,8 normally.
Exact earned seed9200 becomes crop9201, HP32/42drams/four resin intact. Actual
PNG/JSON evidence is `inventory-action-release-*`; independent reviews pass.

F5 checkpoint `/tmp/coo-fresh-opening-after-inventory-release/` preserves fresh
origin at tick3800,17,8Nella. Scene/prefs/view/save-root restored07:26:22UTC,
normal save hashes intact; no package or physicalDeck acceptance. Continue
the existing fresh-origin eastforge/kitchen route, using a pure-frost brew from
the remaining lichen rather than the mixed coating that can protect enemies.
Gather actual raw grain/pulp and preserve clay for pan repair. Next art cycle:
compose Nella's existing work area beside a field approach, retaining current
owners/rights/finite supplies and testing two cold seeds before acceptance.


## Latest continuation — fresh-origin forge/kitchen, 2026-10-25 08:02 UTC

The fresh New Game character now completed the six-site service route: finite
cellar clay/north reagent/Nella rights and crops/glade grain/east forge/kitchen.
Actual inputs tempered same sword2703 with pure Frozen9202, repaired pan13701,
paid2drams+grain/pulp, waited and collected one meal. F5→stop→ordinary Continue
retained exact captured identities,42HP/40drams at5830. Actual captures and
bounded acceptance are in [fresh route](GRAPHICS-G3-FRESH-OPENING-ACCEPTANCE.md).
Whole-scene art, seed65, latest packaging and physical Deck remain open.

Inventory follow-up fixes observed during the route are tracked in
[polish](GRAPHICS-G3-INVENTORY-POLISH.md): header focus, craft acknowledgment,
and truthful meal guidance/category. Then Nella scene composition is next,
using real greybox→capture→imagegen→native assets→ordinary play. Earned kitchen
checkpoint is `/tmp/coo-fresh-opening-after-kitchen/`; user save hashes intact.
No stale cache target justified deletion;0bytes removed.


## Latest continuation — inventory polish, storage recovery

Inventory selection, truthful craft-completion counts and meal guidance/category
are implemented and independently reviewed; native Unity233/233passed,0skip.
[Polish record](GRAPHICS-G3-INVENTORY-POLISH.md) preserves RED/GREEN and limits.
Final ordinary menu recapture was interrupted by ENOSPC. Root stopped Play and
terminated only the owned Editor after its cleanup snippet timed out, recovering
write capacity. Next: preserve these exact files on main, remove only the two
audited stale platform PlayerDataCache directories while Unity is closed, verify
normal save/isolation restoration, replay the menu and correct ForgeCommandsTests
namespace, then resume Nella whole-scene composition. No cleanup yet in this slice.


## Latest continuation — inventory replay accepted, 2026-10-25 08:43 UTC

[Inventory polish](GRAPHICS-G3-INVENTORY-POLISH.md) now has actual1280×800 menu
captures, exact earned stoneburr→tonic deltas, readable green completion and
correct meal guidance. Independent visual/readout review accepts this bounded
menu replay;245distinct native cases pass across233+12disjoint runs. This is
not latest-package, physical Deck or complete-opening art acceptance.

Earned checkpoint: `/tmp/coo-fresh-opening-after-inventory-polish/`. Normal save
and backup hashes and pre-audit LastGameID are restored; Play/isolation cleared.
A low-impact Editor custom-resolution cleanup index error remains separately
queued for native RED/GREEN. Continue with the documented Nella composition
cycle in [Nella plan](GRAPHICS-G3-NELLA-COMPOSITION.md), using a cold pre-Nella
checkpoint, actual capture→imagegen→deterministic real-owner layout→two-seed
ordinary Play comparison. Preserve approved sprite pixels and gameplay budgets.

Storage remains constrained. The first verified closed-Editor platform-cache
cleanup recovered9,453,568bytes despite261,492,736reportedallocatedbytes.
Push this evidence first, then revalidate/delete only the stale-clone manifest's
191rawlogs/fourgeneratedcachedirectories. Preserve every compressed evidence
counterpart, source, dependencies, saves, active main caches and current builds.


Storage continuation: after verified remote preservation at696a9ff35, exact
stale-clone cleanup completed:191redundant rawlogs/fourunusedgeneratedcaches.
Observed583,999,488bytes regained (about557MiB), not the2.418GiB allocation
total. Source, compressed evidence, saves, dependencies, active main caches
and current builds remain intact. See inventory-polish-stale-clone-cleanup.json.


## Latest continuation — restored graphics workflow, 2026-10-25 09:04 UTC

Inventory polish is accepted in bounded ordinary Play with actual screenshots
and245distinct native passes. Audit-view cleanup is also fixed: short full-token
labels and correct removal index pass22native cases and a direct create/remove
smoke. The single proven orphan from this root audit was removed; user presets
were not swept. Normal save files/backups and the exact pre-audit preference
remain unchanged/restored. Unity is open in clean SampleScene, out of Play,
without isolation or a presentation override.

The next scene cycle is [Nella composition](GRAPHICS-G3-NELLA-COMPOSITION.md).
The first imagegen concept is reference only; reject its changed UI, gradients
and exaggerated growth. A cold review found Nella already near the proposed
frontage, so retain a substantial primary field and clear service bay rather
than uniformly thinning an already sparse scene. No new layout code is applied
yet. Next: source-grounded RED for the bounded descriptor/mask, native integration,
then ordinary cold64/65 before/after gameplay checks. Preserve the current
approved sprite art, actual crop states, finite budgets and public approaches.

Current packages still predate this inventory/Nella material work; no new build
or physical Steam Deck acceptance is claimed. Storage has limited headroom
after exact guarded stale-clone reclamation; avoid large builds/downloads and
retain active caches. Full opening composition and regional coverage stay open.


## Latest continuation — Nella field/frontage accepted, 2026-10-25 10:21 UTC

[Nella composition](GRAPHICS-G3-NELLA-COMPOSITION.md) now has a coherent primary
field around its complete existing service group and shorter substantial remote
remnants. The first candidate passed tests but looked too empty and was rejected.
The revised candidate passes158 native Unity cases, with real cold seed64 and
fresh New Game seed65 screenshots independently reviewed. Seed64 ordinary paid
rights→gather→replant→Rain→glean→save/load/travel return preserves sampled
identities/state; seed65 verifies approach and payment only. Approved sprite
pixels and finite yield budgets are unchanged. Full opening/region art, latest
packages and physical Deck remain open.

Unity is out of Play in clean SampleScene, with audit view/prefs/isolation restored
and normal save/backup hashes unchanged. Earned checkpoints are
`/tmp/coo-fresh-opening-after-nella-composition/` and
`/tmp/coo-fresh65-after-nella-composition/`; the original pre-Nella64 checkpoint
is intact. Current packages still predate inventory/Nella changes. Next: review
the six whole scenes together, choose the most visible inconsistency and continue
the playable capture/concept/native integration cycle. Do not reopen this accepted
bounded layout solely to manufacture another iteration.

Storage follow-through at10:28UTC: verified all118 owned files and commit
`d313eac64` on origin/main before deleting four closed, regenerable database files
from the inactive verification clone's Library: ArtifactDB, ArtifactDB-lock,
SourceAssetDB and SourceAssetDB-lock. Observed available-space gain was
158,928,896bytes (151.6MiB), not the216,727,552byte allocation sum. The exact
[receipt](Verification/Live16x24/nella-composition-storage-cleanup.json) records
both values. Source, editable assets, saves/backups, dependencies, active main
caches and current builds remain intact. A bounded independent follow-up found
no safe target above500MiB; disk remains constrained at roughly340MiB. Do not
start a large build or erase useful active caches to force one. Smaller inactive
clone compiled/search caches remain candidates for the next guarded inspection.


## Next scene cycle — cellar floor, 2026-10-25 10:41 UTC

Six actual scene captures were reviewed together. The next bounded task is
[the cellar floor](GRAPHICS-G3-CELLAR-FLOOR.md): a quiet sparse stone material
beneath the existing walls/actors, preserving prepared sootroot beds, hazards
and fog. The real entry capture drove a built-in imagegen concept; its resized
HUD, altered lettering/actors and lighting are rejected. Only sparse broken
seams inform native authoring. Exact owner/state, RED/counter, submitted-Tilemap
and ordinary-return gates are recorded. No floor runtime/art import or new
gameplay acceptance is claimed yet. Existing Nella and inventory remain frozen.
Storage is still constrained; use the guarded stale-cache process, preserve
current packages and avoid heavy builds. Whole-opening/Deck/regional gates stay open.


Storage follow-up10:42UTC: after verifying `801f96d04` and all122 owned files
on origin/main, removed only the inactive clone's generated ScriptAssemblies
(164DLL/PDB files) and Search (11indexes/database files), with no open handles.
Observed additional available-space gain41,766,912bytes (39.8MiB), versus
81,870,848allocatedbytes. Combined observed gain for this Nella heartbeat is
200,695,808bytes (191.4MiB). Current free space remains about308MiB because
other activity consumes space between checks. No large safe remaining project
cache/build target was found. Main active caches, source, editable assets,
saves/backups, dependencies and current packages remain protected. Exact receipt:
[small-cache cleanup](Verification/Live16x24/nella-composition-small-cache-cleanup.json).


Cellar continuation10:54UTC: four editable native floor candidates and a34-case
uncompiled test draft are preserved outside Assets. No new floor runtime or Play
acceptance. Independent source review passed; actual lighting/repetition awaits
integration. The clean owned Unity Editor is normally closed. Exact older
player-build intermediates (477.7MiB allocation) are audited separately from
useful editor/import/JIT caches; preserve/push this owned work before any guarded
reclamation. See[the cellar plan](GRAPHICS-G3-CELLAR-FLOOR.md).


## Continuation — cellar floor accepted,2026-10-25 11:36UTC

The quiet native cellar floor is now implemented and boundedly accepted. See
`Docs/GRAPHICS-G3-CELLAR-FLOOR.md` and independent
`Docs/Verification/Live16x24/cellar-floor-gameplay-review.md`. Four editable16×24
frames resolve only actual visible ordinary Floor owners; prepared beds, crops,
coatings, pools, gas and memory retain their current priority. Foreground,
geometry and simulation are unchanged. Independent review's malformed duplicate
Floor finding was reproduced RED and fixed. Native Unity sweep290/290 passes,
0skips. Actual fresh seed64 entry→combat→spent seed harvest→pallet dismantle→
Examine→F5/F6→glade→cellar return preserves recorded floor/soil state; seed65
opposite lower passage is separately captured.All gameplay captures are1280×800. No package/Deck or complete-opening claim.

29.076seconds active stationary world produced no additional environment redraw
or resource growth. This is bounded counter evidence, not FPS. Normal saves,
backups and current Linux archive rehashed unchanged; ordinary editor preferences
and isolation restored. Before integration, source/evidence was pushed to main at
`f516e6d99`, then the closed Editor's exact stale player-only intermediates were
removed:341.2MiB actual recovery. Active caches/current builds were preserved;
about425MiB remains after testing, so no new package build or routine Library wipe.

Next: Warden En Garde/Lunge/Follow Through share generic diamond hotbar icons in
actual gameplay. Audit exact icon path; use the actual frame→imagegen→native
editable icon→real integration/test/play loop. Do not reopen accepted Nella,
kitchen or cellar art without a named defect. Whole opening and regional art
remain the priority; ongoing heartbeat continues.


## Continuation — Warden skill identity,2026-10-25 11:59UTC

[Warden hotbar](GRAPHICS-G3-WARDEN-HOTBAR.md) replaces three identical generic
diamonds with native guard/thrust/blade-and-step pictograms. Actual-cellar capture
→imagegen→editable pixel rows→native RED→implementation→105passing native tests
→ordinary New Game keyboard/cast/cooldown/aim/cancel/F5 and independent visual
review are complete. World art/layout and combat rules are unchanged. Actual
captures:`Docs/Verification/Live16x24/warden-hotbar-ready.png` and
`warden-hotbar-follow-through-aim.png`. Normal save/backup/current archive hashes
unchanged; audit prefs/view restored and Unity stopped cleanly. Packages still
predate inventory/Nella/cellar/Warden work; physical Deck and full opening open.

A separate click check found the hotbar still reading legacy mouse input while
inventory already uses the native device. Clicks failed; do not claim mouse
acceptance or rescale icon geometry from editor Screen readback. Next narrowly
fix/prove that input boundary with native RED and real clicks, then return to the
whole-scene opening gaps. No accepted art needs reopening. Storage about303MiB at
11:59UTC; preserve useful caches/current packages, no heavy build or speculative
cleanup. Heartbeat remains active for ongoing world/regional completion.


## Continuation — hotbar mouse corrected,2026-10-25 12:15UTC

[Hotbar pointer correction](GRAPHICS-G3-HOTBAR-POINTER.md) closes the actual click
failure found during Warden icon acceptance. One authoritative native pointer
pair with matching legacy fallback now feeds unchanged painted-card hit testing.
Native RED11/10fail→GREEN107/107, plus actual OS clicks for Lunge/Follow Through
AIM, En Garde cast and cooldown refusal independently reviewed. Same approved
icons/layout/world art. Editor stopped cleanly; normal save/backup/current archive
hashes unchanged. Earned mouse checkpoint:`/tmp/coo-hotbar-pointer-earned`.

Next: return to world completeness with ordinary owner discovery of the known
north incidental LeatherArmor6954 at16,17 and unidentified kitchen P glyph.
Use real current Look/Examine and capture before choosing the native item art
slice; do not infer identity from a glyph. Full opening/regions and current
package/physical Deck gates remain open. Two obsolete Unity Hub3.20.1installer
caches are audited for guarded deletion only after this work is verified on main;
current Hub/editor/dependencies/caches and packages remain protected.


## Preservation and storage closeout — 2026-10-25 12:21 UTC

Pointer code, native tests, actual mouse screenshots and living records were
verified on origin/main at `34fe9cc8b0758f3083e9abf5190712e430b8e41b`
(270 owned files byte-compared). Only then, after fresh version/hash/inode/type
and no-open-handle checks plus independent review, removed exactly the two
obsolete Unity Hub 3.20.1 installer ZIPs listed in
`Docs/Verification/Live16x24/hotbar-pointer-stale-hub-installer-cleanup.json`.
Observed filesystem availability increased by **381.2 MiB** to 567.5 MiB;
this is the measured delta, not an inference from file sizes. Earlier this
heartbeat's stale player-cache removal recovered 341.2 MiB separately.

All source/editable assets, saves/backups, current builds/archives, installed
Hub/editor, newer pending Hub update state and useful active caches remain.
Unity is out of Play with normal preferences restored. New packages remain
pending because current disk headroom is still inadequate for a safe build.
Continue the documented opening incidental-item identity pass next; this is
not opening or regional completion, and the graphics heartbeat remains active.


## Continuation — native opening equipment accepted, 2026-10-25 13:03 UTC

[Hatchet and leather armor](GRAPHICS-G3-INCIDENTAL-EQUIPMENT.md) now retain native
16×24 identity in actual ground, exact Examine, carried and equipped views.
Two earned seed64 branches verify pickup/equip/unequip/drop and F5/F6 continuity
without grants or teleport. Actual inventory captures:
`Docs/Verification/Live16x24/incidental-items-hatchet-inventory.png` and
`incidental-items-armor-equipped.png`. Native RED29→GREEN143, independent review's
raw-footprint RED2→final147/147; no skips. Editable rows, extraction/concept,
resource metadata and independent reviews accompany the implementation. Neutral
art does not assert condition/stats; no mechanics, geometry or loot changes.

61.592 seconds stationary active Play caused no environment redraw or equipment
resource growth; this is not FPS or Deck benchmarking. Normal save/backups and
current archive hashes are unchanged. Cleanup exposed a changed LastGameID
following native-runner activity; restored the verified original pointer with
Unity stopped, preserved the extra save and all evidence. Future native runs
must keep save isolation through delayed Play resumption. Corrected restoration
receipt: `incidental-items-final-restored-corrected.json`.

Next is whole-scene seed65 north approach and Nella lifecycle using the earned
Warden cellar checkpoint, not another isolated item packet. Full opening,
regional art, new package and physical Deck gates remain open. Read-only storage
inspection found no worthwhile stale data; no deletion. Current packages still
predate recent art/UI/input changes. Preserve them and useful active caches.


## Continuation — second-seed evidence and storage recovery, 2026-10-25 13:45 UTC

[Second-seed composition](GRAPHICS-G3-SEED65-COMPOSITION.md) records ordinary
cellar→glade→north→Nella travel, one mendleaf harvest, actual contested retreat,
Nella payment, mature harvest versus a young-crop counter-check, replanting and
initial watering/growth. Captures and exact owner receipts are real1280×800
Unity gameplay. Full north still use, crop maturity and save-return remain open.

ENOSPC interrupted the post-F6 capture. Evidence was verified on main via a
non-forced Git data API checkpoint (`3cec8c906`) before removing exactly the
superseded Oct11 fullscreen-skills archive; observed recovery122.16MiB. No source,
normal save/backup, current build/archive or active cache was removed. Original
save pointers, scene/view/preferences and all protected hashes are restored.
The isolated replay root was automatically discarded on emergency Play stop;
its original earned cellar checkpoint survives, so unfinished continuity must
be replayed rather than inferred. Current storage is still too low for a build
or long replay. No further clearly unnecessary large project cache was found.

Independent visual review confirms green world seams in the Examine backing.
Next narrow fix: world popup backing coverage with native RED/GREEN, then actual
Examine and action-menu captures after restoring sufficient disk headroom.
Next scene target remains the weakly grouped northern worksite and broad field
fragments; capture the unobscured yard before selecting a bounded layout change.
Do not replace approved sprites or add unrelated asset packets. Full opening,
regional graphics, updated package and physical Deck gates remain pending.


## Continuation — popup sampling fix, 2026-10-26

[Popup raster coverage](GRAPHICS-G3-POPUP-RASTER-COVERAGE.md) now records actual
GPU RED, rejected overlap/shared-grid candidates, 49 passing native tests and
ordinary earned north Play captures. UI SolidBlock has an opaque texture gutter;
geometry and approved art remain unchanged. Mendleaf actions/Examine and
inventory sword details have clean backgrounds. Editor isolation is restored.
Storage blocker is cleared (about432GiB available after separately authorized
OpenClaw error-log/XCTest cleanup); all other cleanup remains inspection-only.
Next: second-seed return/growth and north-yard composition. Updated packages,
physical Deck, full opening and regional graphics acceptance remain open.


## Continuation — eastern forge workpad accepted, 2026-10-26 05:52 UTC

[East shelter composition](GRAPHICS-G3-EAST-SHELTER-COMPOSITION.md) now groups
the existing forge and broken wall with an optional quiet native work surface.
No new sprite packet, stock, enemies or gameplay service was added. Twelve
candidate cells are filtered through actual ownership/ground; ordinary seed64
admits11, seed65 admits9. Flowers and the forage margin remain intact. Shared
north/east floor validation now protects malformed/late-mutated payloads and
Owned Grass. Native RED38:31fail/7pass →178-case seven-fixture GREEN →42-case
focused review rerun,182distinct cases total, zero skips.

Actual1280×800 ordinary editor screenshots show seed64 adjacent forge use,
menu/reader and F5/F6 plus glade exit/east return with all11floor/substrate IDs
retained. Seed65's first southern approach died; an ordinary upper-lane replay
reached the forge at65,3,31HP/50drams, used its menu/reader and preserved all9
floor/substrate IDs through F5/F6. No supplies were granted, and no crafting
success or seed65 border return is claimed. Before/after views differ in
runtime IDs, fog and combat; independent visual review accepts the bounded
composition improvement, not exact simulation equivalence or full-scene finish.

Root restored normal editor/save/view state and verified all four protected
normal save/backup hashes and two unrelated PNG hashes.26 importer-only meta
rewrites and an incidental generated census were backed up and restored.
About429GiB free; no storage deletion. The dated Mac/Linux packages are still
sourcec893ae58f, preceding glade/east changes; physical Deck remains untested.

Next: a coherent current six-zone1280×800 journey review with inventory,
interaction/state and earned supplies, then current-package parity and actual
Mac journey replay. Choose the largest remaining scene-level defect from those
views. Do not keep producing disconnected packets. Full opening/regional art,
complete forge lifecycle and device acceptance remain open; continue this work.


## Continuation — current packaged journey and underfoot fix, 2026-10-26

[Current packaged journey](Verification/Live16x24/packaged-glade-east-journey-report.md)
records fresh Mac seed200893250 visiting all six opening graphs with ordinary
keyboard controls and no grants. North harvesting/brewing → East tempering a
looted sword succeeded; kitchen dialogue and quit/Continue retained the earned
game. Current dated Mac/Linux outputs now include glade/east composition at
source dce87081d. Linux/device execution remains unverified. A viper killed Nella
before reserve service, so this is a six-zone visit, **not six-service acceptance**.

Actual play exposed underfoot Harvest being hidden by treating the player as
another pile occupant. [Underfoot consistency](GRAPHICS-G3-UNDERFOOT-ACTIONS.md)
now has RED15/18fail →176/176 native GREEN, baseline-confirmed fixture drift
repair, independent review and actual earned editor Harvest/depletion captures.
The frozen dce packages predate this small fix; do not describe them as containing
it. Existing sprites, harvest authority and yields are unchanged.

Next scene: kitchen court and adjoining orchard, using actual packaged18/22
captures as playable baselines. Independent review prioritizes Beehive glyph6
fallback and small indistinct service silhouettes/entrance. Preserve accepted
art; use the imagegen/extraction/native/editable/integration/playtest loop and
avoid unrelated packets. Nella service survival needs a separately scoped audit.
Normal saves (776 files), preferences, editor state and foreign work are protected;
no storage deletion. Whole-opening art, regional kits and physical Deck gates
remain open. Continue across sessions from this bounded next scene.


## Continuation — kitchen/orchard identity accepted, 2026-10-26 07:35 UTC

[Kitchen/orchard](GRAPHICS-G3-KITCHEN-ORCHARD.md) completes the actual capture →
imagegen scene/parts → editable native pixels → integration → ordinary Play loop.
Wild hives now have one neutral native 16×24 image in the six admitted graphs
and matching examination art. Three quiet court textures replace the small
pebble marks without moving the 15 floor owners or services. Original art and
3D sources remain recoverable. RED: 41/42 failed as expected; GREEN: 260/260
passed, with independent source, runtime and ordinary-scale visual review.

Actual earned seed 200893250 replay shows matching court views, hive examination,
harvest yielding two Honeycomb and removing the hive, F5/F6 and a western exit/
return without respawn. The two previously earned FireClay repaired pan 13771;
save/load retained that repair. The meal batch remains Idle. NPC positions differ
between hive replays, so they are not identical whole-world comparisons. The
sampled court view used 38 draw calls before and after; these are Editor samples,
not physical Deck timings.

All 776 normal save files, 65 protected art/source files and two foreign PNGs
are unchanged; normal editor/view/save settings are restored. The 26 incidental
importer whitespace rewrites were backed up and restored. About 429 GiB is free;
no storage cleanup occurred. Dated Mac/Linux packages still use source dce87081d
and predate both the underfoot fix and this artwork.

Next: improve repeated scalloped hedge silhouettes using disclosed-neighbor
end/corner/interior art, preserving exact owners, collision and approved props.
The earned fixture `Verification/Live16x24/Fixtures/kitchen-orchard-earned-repaired`
retains the actual harvest and pan repair for subsequent comparisons. Separately
reproduce Nella's initial threat exposure in seed 200893250 before changing
placement; mortality is intentional. Full opening/regional art and package/device
acceptance remain open. Continue from this bounded next scene refinement.


## Continuation — connected hedge accepted, 2026-10-26 08:17 UTC

[Hedge continuity](GRAPHICS-G3-HEDGE-CONTINUITY.md) completes actual baseline →
imagegen reinterpretation/extraction → two native source passes → narrow runtime
integration → native338/338 → independent review → ordinary Play. Existing hedge
owners now form continuous ends/corners/runs through disclosed cardinal neighbors.
The isolated approved image and every original source remain intact. Native RED
was30/30 expected failures; the first337/338 GREEN exposed only an overlapping
footprint test setup, corrected before final338/338 with zero skips.

Actual1280×800 earned kitchen play shows matching court/corner views, examination,
nonlethal hit, destruction, walking through the real gap, F5/F6 and a western
chunk exit/return retaining the gap. No grants or geometry edits. One added shared
atlas batch was observed (38→39draw calls); this is not a Deck timing benchmark.
All776 normal saves and two unrelated PNGs are unchanged;42 prior art/source files
are preserved. Normal scene/view/save settings restored;28 importer whitespace
rewrites backed up/restored. About428GiB free, no cleanup/deletion.

Next: fresh dated Mac/Linux current packages from this pushed source, including
underfoot actions, hive/court and connected hedges, then actual Mac ordinary
Continue/inventory/reader/movement/save-return. Existing dce87081d packages predate
those changes. Preserve previous outputs. Full six-service/whole-opening art,
regional kits, Linux execution and physical Deck remain open. Nella's service
loss is still a separate scoped audit; do not declare mortality itself a bug.


## Continuation — delivered kitchen/hedge packages, 2026-10-26 08:34 UTC

[Current delivery](Verification/Live16x24/packaged-kitchen-hedge-report.md) freezes
source8902d3731 in new dated Mac/Linux `-current` outputs, including underfoot
Harvest, hive/court and connected hedges. Both builds0errors/4warnings; archive
128645260bytes,181files independently hash/size/mode verified. Mac ordinary
Continue/inventoryDMG-DEF/rightmost[E]/item+hedge+hive readers/underfoot harvest
and quit/Continue are captured. Harvest yieldedone Honeycomb and remained absent
after return. These12 actual1280×800-viewport screenshots are not concepts.

Normal776savefiles and preference mappings preserved; editor idle inSampleScene,
StandaloneOSX with no isolation/saveoverride. All24765 tracked source hashes and
two unrelated PNGs unchanged after backing up/restoring32 build-generated files.
About427.7GiB free; no cleanup/deletion. Known shutdown thread/ComputeBuffer
notices persist. Linuxexecution,physicalDeck,fullsix-service journey and whole
opening/regional art remain open. Current archive path/checksum are inG1/latest.

Next selected scene defect: east meadow's repeated dense flower ellipses overpower
the small forge. Capture current matched entry/upperapproach views, reinterpret
withimagegen, then refine eligible dressing into unequal interiors/holes and a
quiet destination margin while preserving budgets,workpad,owners and routes.
After that, audit plain teal water rectangles outsideglade for shared bank
presentation. Nella active/fallow hierarchy needs a current matched view before
changing acceptedwork. Continue this scene-level cycle; no unrelatedassetpacket.

## Continuation — East meadow composition accepted, 2026-10-26

The [East meadow pass](GRAPHICS-G3-EAST-MEADOW-COMPOSITION.md) is accepted for its
bounded density/readability defect. Actual before captures, imagegen critique
and extraction led to generation-only thinning of existing flower bodies.
Approved sprites, routes, service owners, stocks and the optional forge pad
remain unchanged; no new atlas or rendering subsystem was added. Final native
regression passed **244/244**, zero failures or skips, at 09:25:18–09:27:24 UTC.
Full-generation controls preserve all compared nonflower fields, pad positions
and reservations for seeds 64, 65 and 200893250; flowers change from 185/201/216
to 120/130/139. Allocated equipment-cache keys are normalized to an equipped-item
multiset with duplicate counts preserved; this does not certify anatomical
slot assignment between independently generated worlds.

Ordinary seed 64 Play reached East (7,18), tick 1140, HP 40 and 50 drams, and
opened the forge reader through C/Down/X. F5/F6 preserved all 246 captured owner
records exactly when compared by ID. An ordinary westward exit and return
preserved all 120 flower IDs and positions; two Marlback hatchet hits reduced
HP to 15 before re-entry at (0,16), tick 1250. Seed 65 independently entered the
fresh East and reached (65,3), tick 1230, HP 40 and 50 drams, then opened the
same reader. A timed-out movement batch was corrected from the observed actual
position. Its older screenshot is a qualitative comparison only; no matched
before/after combat state, new seed 65 save-return or crafting is claimed.
Actual captures and route details are linked in the pass record.

Current Mac/Linux packages remain at `8902d3731` and therefore precede this
meadow change. No new per-frame work was added, but no FPS improvement was
measured. Linux execution, physical Deck, the complete six-service journey,
whole-opening acceptance and remaining regional art are still open. Repeated
brine application/expiry log messages were observed and left unchanged outside
this graphics slice.

Next: capture the actual East pure-water patches, then test a narrow reuse of
the existing disclosed-neighbor bank art. Preserve mixed liquids, ice, hidden
neighbors, hazards and authoritative owners; do not broaden ground admission
to every surface. The water handoff in the pass record is a plan, not implemented
or accepted work. Preserve current Nella art pending a matched active/fallow
scene review and its separate service-loss audit.


## Continuation — 2026-10-26: East surface-water material continuity

Bounded ordinary Unity Play accepted in [East water record](GRAPHICS-G3-EAST-WATER.md).
The exact admitted East graph now reuses opening-water bank frames only for a
sole live water coating; dry ground, other regions, mixed liquids, ice and
hidden-neighbor disclosure remain unchanged. Existing artwork/source bytes are
preserved. Built-in imagegen reinterpretation and part/state extraction used
an actual earned-bank screenshot, with no concept pixels in production.

Native RED36expectedfailures preceded the minimal ground-catalog change;
GREEN229/229passed,0skips across six fixtures. Independent review found no
material blocker. Actual1280×800 before/after shows the same earned(50,20)bank
and(51,20)wet crossing.371observed owner tuples match by exactID; inventory,
coatings,HP/drams/tick match before/after and F5/F6. RealDown5to kitchen(51,0),
Up5return retains bank art and water footprint. Freeze/melt remains native
rather than ordinary spell-input coverage.86.13s mixed editor profile includes
crossing/save-load/idle time and is not a Deck or controlled performance claim.

OwnedPlay ended; normalSampleScene/prefs/view/save root restored.776normal save
files,374preexisting art/source files and2foreignscreenshots remain unchanged.
No storage deletion. About427.5GiBfree. Exact owned source/evidence/doc changes
are the main-publication unit; current packages8902d3731still predate meadow
and water. Next refresh dated Mac/Linux packages, preserve all existing outputs,
and check ordinary Mac Continue/new-game/crossing/save-return. Then resume the
complete six-zone review and separate Nella service-loss audit; wholeopening,
remaining regional art,Linuxexecution and physicalDeck acceptance remain open.


## Continuation — combat aftermath identity, 2026-10-26 11:27 UTC

[Aftermath pass](GRAPHICS-G3-OPENING-AFTERMATH.md) adds original native16×24
Magpie/Viper remains and the existing VenomGland, following actual capture,
imagegen reinterpretation, part extraction and editable flat Blender source.
Living/approved art is unchanged. Native236/236passed,0skips, including51new
exact-identity/custody/state/adversarial cases. Independent reviews clean.

Actual isolated seed65 controls picked up the earned magpie corpse9225, displayed
it in inventory and full Examine, dropped it at51,13, stepped aside and F5/F6
retained its native body and metadata. HP35/tick2650 includes ordinary enemy
attack/bleeding, not fabricated safety. Original pile red% is RawMeat atlayer5
covering corpse atlayer3; priority is preserved. Viper outcomes remain native
test coverage, not ordinary encounter acceptance. Actual04–07captures linked
in the pass. Wholeopening/regional and physicalDeck acceptance remain open.

OwnedPlay restored;776normal saves,374approvedart/source files,two foreignPNGs
unchanged.29whitespace-only generatedmetas backed up/restored.No storagecleanup.
Current2a8e71044Mac/Linux packages predate aftermath. Next: current North sole-
water baseline and bounded shared bank reuse; preserve dry court/field, mixed
liquids, ice, hidden neighbors and every other graph. No broad loot reskin.


## Continuation — North water material continuity, 2026-10-26 11:49 UTC

[North water pass](GRAPHICS-G3-NORTH-WATER.md) reuses the approved native bank
frames for sole live water on exact admitted North. Actual capture, imagegen
reinterpretation and part/state extraction led to a minimal scope change;
no new artwork, entities or resource changes. Dry North terrain and mixed,
frozen, hidden or foreign graph states retain their existing presentation.

Native22expectedRED →299/299GREEN, zero skipped. Actual seed65 Continue at
(58,16), crossing the PeatBog, F5/F6, garden exit and North return are captured.
Both matched and reload comparisons retain205owners plus player/inventory/
coating state exactly. Bog6905 retains70water; its source lease4 is distinct
from the observed permanent surface coating. Independent review found no
material issue. A bounded26.4s editor profile is retained without a device
performance claim. Normal scene/preferences/save root restored;776normal
saves,374approvedart/source and two foreign screenshots unchanged. No cleanup.

Current2a8e71044Mac/Linux packages precede aftermath and North water. Next
refresh dated packages, preserve all earlier outputs, then replay actual
Mac Continue/corpse inventory/North crossing and save-return. Whole opening,
cellar watered growth, secondary crops, regional kits, Linux execution and
physical Deck remain open. Do not replace those gates with asset counts.


## Continuation — current aftermath/North packages, 2026-10-26 12:02 UTC

[Delivery](Verification/Live16x24/packaged-aftermath-north-report.md) freezes
runtime source `e49047200f28708d4bfd77fb7007d2c7843ba2ab` in new dated Mac and
Linux `2026-10-26-aftermath-north-e49047200-current` folders. Both builds
succeeded with0errors/4warnings; all181Linux archive files independently match
hashes/sizes/modes. ArchiveSHA256
`089a9cc4d06b1d3c20bd2c9052bd606fbeae8d13b97ef25c632edb181ae0e400`.
Earlier builds/archive preserved. Source and delivery are now aligned.

Fourteen actual Mac captures verify two earned paths: NorthContinue/crossing/
inventory/F5quitContinue/gardenexit/return, and magpiecorpse pickup/inventory/
Examine/drop/F5quitContinue. Native body persists at51,13; player52,13HP35
includes an ordinary enemystrike. Currentinventory DMG/DEF and far-right[E]
are visible. Existing shutdownthread/ComputeBuffer notices remain recorded.
No physicalDeck/Linuxexecution/performance/fullopening claim.

Normal editorOSX/scene/preferences/save root restored;47,731tracked prebuild
hashes,776normal saves,374approved art/source and two foreign screenshots
verified.33generated files backed up/restored; both test Saves runs archived.
No storagecleanup. Next: ordinary cellar watered-sootroot growth/revisit and
owner-state review, then remaining secondary crops/regional kits. Preserve
accepted artwork absent a named defect. Heartbeat remains active.

The next earned checkpoint is now retained at
`Docs/Verification/Live16x24/Fixtures/cellar-earned-dry-sootroot64`: unchanged
seed64cellar tick590/HP30, expected player65,8 and drycrop4729at65,10.
Read carried WateringGrimoire if needed; existing Rain radius3 and40moist
units require topping up to reach560wet worldticks for stage0→ripe2.
Capture actual young stage1 and save/reload before completion. Existing
cellarframes3/4/5 andproducts6/7 already cover these states; this is a pending
ordinary-play gate, not evidence of missing artwork or new acceptance.


## Continuation — current packaged cellar lifecycle, 2026-10-26 12:26 UTC

[Cellar growth](GRAPHICS-G3-CELLAR-GROWTH.md) closes the ordinary watered
sootroot gap in the e49047200 Mac package using the unchanged earned64fixture.
Twenty-six actual captures show dry seed→watered→young, save/quit/Continue young,
ripe, normal harvest2pulp+1seed, pickup with native inventory art, replant, water,
real surface stairs/return and dry sprout after finite water expired. Savedticks
620/890/900/1170/1800 andHP30 retained. No grants, forced growth, runtime/art
changes or new native test claim. Independent visual review found no mismatch.

Both preference maps restored, isolated Saves retained outside normal location;
776normal saves,374approvedart/source and2foreignPNGs unchanged.30whitespace-only
importer metas backed up/restored. Existing shutdown notices retained. No cleanup.

Next: fresh current-generated opening scene framing at1x/2x through6zones,
ordinary combat feedback and finite native/fallback census. Current packages
remain e49047200. Older saved Northgeometry is not a current generation defect.
Secondary crops, remaining regions, Linux execution and physical Deck remain
open. Keep approved art stable unless the fresh scene reveals a named problem.


## Continuation — 2026-10-26 playable native zoom

[Plan, RED/GREEN and actual captures](GRAPHICS-G3-PLAYABLE-ZOOM.md).
The fresh scene review found controller zoom could never reach2×atDeck layout.
Fixed real input access without changing approved art/default framing.
23native cases pass;3broader log pointer preconditions also fail on unchanged
code and are disclosed. Actual1280×800Game views verify2×, notice targeting,
inventoryreturn and ordinarycamera tracking. Package newsource next, then use
its ordinary zoom for six-zone scene review. Do not use oldsavedlayouts to
judge currentgeneration; newseed224401440fixture is preserved.


## Continuation — 2026-10-26 delivered playable zoom

Current packages freeze47d678995 and now contain the zoom fix. Actual Mac
captures verify1×/2×/4×, inventory return, ordinary stairs into the cellar and
back, save/relaunch/Continue. Source23native cases pass; disclosed3 broader
log-pointer precondition failures also occur on unchanged code. Independent
visual/archive review passed. No new artwork was warranted for this camera
repair. All normal gameplay state and approved assets remain preserved; storage
inspection only. Read the package record and continue the fresh seed224401440
six-zone scene review using the now-reachable ordinary zoom. The surface/cellar
path is accepted, but not complete regional or device coverage.

## Continuation — 2026-10-26 current-scene fixes

Read GRAPHICS-G3-CURRENT-SCENE-REVIEW.md and GRAPHICS-G3-NELLA-CORNER.md.
Current seed224401440 now has accepted neutral shrine art, reachable initial
Nella service placement, exact-speaker native dialogue portraits, and connected
copper/peat/water art in Nella's real corner. The latest corner has139native
passes and actual1×/2×, examine, wet-entry, save/reload and zone-return evidence.
These are source/editor acceptances; packages47d678995 still predate them.
Next finish fresh East/Orven whole views and remaining bounded six-zone review,
then consolidate delivery. Do not call individual accepted corners a complete
opening release or replace the accepted keeper/crops without a named defect.

## Continuation — 2026-10-26 consolidated current-scene delivery

Runtime5a3c74359 now has new dated Mac/Linux-current packages. Both build
receipts succeed with0errors/4warnings; the181file Linux archive independently
passes hash/size/mode verification. Fourteen actual Mac captures show ordinary
Continue, gate close/save/quit/Continue, matching reader, reopen/cross, zoom,
inventory and fresh Warden movement. [Complete record](Verification/Live16x24/packaged-current-scene-report.md).

All48289tracked prebuild objects,776normal save files,374approved source/art
files and both foreign PNGs are preserved. Complete logical standalone prefs
restored, generated test saves archived, old current builds/archive unchanged.
Storage inspection only. Integrated cold-eye source and independent package/
visual reviews found no significant issue. Next use this consolidated build
for fresh six-site services/scene acceptance; opening completion and physical
Deck/performance remain open. Keep the graphics heartbeat active.


## Continuation — 2026-10-26 current journey repairs

Read GRAPHICS-G3-CURRENT-PACKAGE-JOURNEY.md, GRAPHICS-G3-NELLA-POPULATION.md and
GRAPHICS-G3-PICKUP-FOOTER.md. Eighteen actual Mac-package captures extend fresh
Warden seed242967504 through cellar combat/harvest/replant/water and North
combat/finite frozen-coating brewing. Nella's entire service was missing; this
is a failed journey gate, not full-opening acceptance. Keep the failing fixture.

Corrected source now passes139native tests after reproduced RED. Exact cold
Nella encounter slots use the existing bounded grazer policy; other zones/saves
and published owners remain literal. Ordinary unchanged-cellar replay reaches
Nella, opens native examine/chat portraits, pays8drams and reloads the same
owner/rights. Six pickup screenshots verify backed readable hints, reader return,
shrink, close and actual material recovery. Source corrections are not in the
current5a3c74359packages yet. Approved art/source and776normal saves preserved;
owned tests stopped/restored. No storage cleanup.

Next: fix the separately observed dialogue footer in conversation13, freeze a
corrected delivery, then resume actual East/Orven and six-site revisits. The
corrected earned Nella fixture is current-journey-corrected-nella242967504.
Whole opening, regions, physical Deck/Linux execution and performance remain
open; keep the graphics heartbeat active. Avoid substituting asset counts for
playable acceptance or overwriting the old failed package evidence.


## Continuation — 2026-10-26 dialogue footer accepted

Dialogue now backs the complete owned panel and renders controls in readable
light gray. Native RED8/11failures → fill-only counter2/11failures → final
100/100focused/neighboring GREEN after a verified assembly reload. Four actual
Nella conversation/topic/return/closed screenshots pass independent review;
no state changes or lingering tiles. Normal saves/art/foreign work preserved,
Editor idle and isolation restored. See GRAPHICS-G3-DIALOGUE-FOOTER.md.

Next: freeze and push these fixes, build new dated Mac/Linux-current packages
per GRAPHICS-G3-NELLA-FOOTERS-DELIVERY.md, verify real packaged chat/save/Continue
and restore all state. Then continue East/Orven/revisits. Existing5a3c74359
packages predate Nella cold-population and both popup fixes; full opening and
physical Deck acceptance remain open. Storage checks are inspection-only.


## Continuation — 2026-10-26 corrected Nella packages delivered

Runtime99b86bb026dd19967f2fb7f1e6997b9a36df41f4 is pushed and now built into
new Mac/Linux `2026-10-26-nella-footers-99b86bb02-current` folders.17actual Mac
captures verify Nella chat/topic/close, equipment damage/defense/[E], native
portrait, real two-stack pickup/reader/shrink/close/recovery and save/quit/Continue.
Archive181files independently verified; SHA125eeb9cb067d8d5a92fcbb2c215011302739763cfad190db2aef5b5c22adabe.
See Verification/Live16x24/packaged-nella-footers-report.md for rejected initial
snapshot, corrected process guard, source test gates and limits. All48,445tracked
objects restored to baseline; normal saves/prefs, approvedart and old5a3builds
preserved. Editor idle OSX, isolation/AutoRefresh restored. No storage cleanup.

Next: continue ordinary East/Orven/revisits from earned fixture
Verification/Live16x24/Fixtures/current-journey-packaged-nella242967504
(player63,9,HP31,tick2290,42drams,seed242967504). This checkpoint is genuinely
earned but its already-generated Nella is not a second fresh-population test.
Full opening/region, Linux execution, physical Deck and frame-time gates remain
open. Keep heartbeat active and approved art stable; fix named actual gaps.


## Continuation — 2026-10-26 connected six-scene consistency pass

Fresh seed64 ordinary input reviewed glade, cellar, North, Nella, East and
kitchen at1x/2x across two isolated sessions.40 actual captures and runtime
receipts are indexed in [six-scene polish](GRAPHICS-SIX-SCENE-POLISH.md).
CopperPipe had three conflicting appearances: North glyph, East repeated
elbow, Nella joined native line. Exact North/East owners now reuse accepted
Nella cardinal frames in world and Examine; peat scope stays unchanged.
Native RED:10fail/37pass; GREEN:105/105 across the corner/East suites. Actual
walking covers/uncovering and Examine verified both affected sites.

Normal saves, complete logical preferences,34foreign tracked edits and previous
packages preserved. Before-fix disposable checkpoint was lost to automatic
Play-stop cleanup; retained captures are a fresh-seed comparison, not identical
tick acceptance. Post-fix checkpoints were archived before stopping. No storage
cleanup. Package delivery/restart acceptance follows this source checkpoint.

Next named visual defect: Kitchen BrinePool11528 at36,13 and11527 at36,14 still
use blue glyphs. Actual volumes90 each, effective coatingbrine, no current
heat/cold/charge, freeze−8°C. Author distinct native salt-water art through the
capture/concept/source/integration loop; do not label it freshwater or assume
empty stock removes an independently renewed source coating. Preserve approved
layout. Actor animation, incidental glyphs and full opening/regions/device/
performance coverage remain open. Current report:2026-10-26 23:27UTC.


### Pipe package delivery completed — 2026-10-26 23:35 UTC

Source6b1032459 is on main. New Mac package passed ordinary Continue, pipe
Examine, player cover/uncover, full quit/restart and North→glade→North travel;
eight original screenshots and earned return checkpoint retained. Linux retry
built with0errors/0warnings and its180-file archive passed independent integrity
review; first attempt with2import-worker errors is preserved separately and
rejected for delivery. Native105/105 tests remain the code gate. No Linux
execution, physical Deck or performance acceptance claimed.

Guarded restoration compared48,827 tracked objects;34foreign edits, normal
saves/full logical preferences and previous packages preserved. Test saves
archived, Editor idle/player closed; no storage cleanup. Current package paths,
SHA and actual evidence are in [six-scene polish](GRAPHICS-SIX-SCENE-POLISH.md).
Next: distinct kitchen brine identity from captured real owners and state,
then actor-motion/incidental-glyph consistency; whole opening remains open.


## Kitchen brine consistency — 2026-10-27 00:10 UTC

The exact Kitchen12.11 brine pools now have an original editable native16×24
identity, shared with their reader portraits. Actual keyboard Continue/Look/
Examine/cover/uncover/F5 captures establish this bounded Editor result;192/192
native tests cover the new state gates and neighboring water/pipes/fixtures.
Source RED preceded implementation. Freeze/melt/renewal uses existing semantics;
only a represented neutral brine layer loses its duplicate wave. See
[Kitchen brine](GRAPHICS-KITCHEN-BRINE.md) for pipeline, corrections, actual
screenshots, profile spike/limits and preservation receipts.

Normal saves/gameplay settings,34foreign edits and current packages preserved;
no storage cleanup. Current delivery still source6b1032459 pending new package
verification. Next: push current source, fresh Mac/Linux builds, normal kitchen
Continue/restart capture, then six-site actor-motion/incidental identity review.
Whole opening and remaining regions remain unfinished.


## Kitchen package milestone and next scene defect — 2026-10-27 00:25 UTC

Completed: native kitchen brine identity and shared reader,192/192 native tests,
actual Editor and seven fresh Mac captures including save/full restart. Source
447e82c4f verified on origin/main before builds; exact delivery receipts and
screenshots are the follow-up push. Fresh Mac/Linux builds0errors4warnings each;
all188 archived Linux files independently checked. Current package paths, hashes,
preservation and footprint correction are in [Kitchen brine](GRAPHICS-KITCHEN-BRINE.md).
Normal saves/preferences,34 foreign edits and prior packages preserved; no cleanup.

In progress: six-scene consistency/whole-opening acceptance. Next hourly goals:
return through ordinary input to East Cudgel9357 at29,6; capture the exact pile
and reader; use that baseline for imagegen/native wooden-club art, then verify
world/carried/reader, pickup/drop and save-return. Preserve accepted art. Do not
call static motion captures an animation defect. Physical Deck/Linux execution
and sustained performance remain unverified. Use explicit compress_lz4 for the
next fresh delivery. Last user-report checkpoint:2026-10-27 00:25UTC.


## East cudgel integrated — 2026-10-27 00:54 UTC

Cudgel9357 from the real East loot pile now has original native16x24 wood art in
world, inventory and Examine. Actual baseline travel, exact pile selection,
pickup/drop, unchanged-checkpoint comparison and auto-equip/sword-restore pass.
Source RED42failed/6passed → GREEN48/48 plus129neighbor regressions. Editable
Blender/cells, built-in imagegen prompt/concept and imported source checks retained.
Independent source/runtime/visual review found no significant defect. Captures
cudgel01–10 are actual Unity Editor gameplay; normal saves/preferences,34foreign
tracked edits and previous packages preserved. No storage cleanup.

See [Cudgel record](GRAPHICS-EAST-CUDGEL.md). Next freeze/push this source, fresh
compressed Mac/Linux packages and actual ordinary Continue/restart from earned
Fixtures/cudgel-after64 (3500,East26,6,28HP,club27,6). Existing current447e82c4f
packages predate this item. Whole six-scene opening/regional/physical Deck gates
remain open. Next named scene candidate is the visible East steam-vent glyph;
audit exact owner/state before authoring. Last hourly report remains00:25UTC.


## Packaged cudgel delivery — 2026-10-27 01:04 UTC

Native cudgel art is verified in the actual Mac build from source `0545aa9a2`:
world at 1x/2x, Examine, pickup, inventory, save and full quit/restart/Continue.
The matching compressed Linux archive passed independent checksum, size, mode
and contents checks. Native tests: 48 targeted plus 129 regression cases passed.
See [full delivery record](GRAPHICS-EAST-CUDGEL.md) for builds, seven original
screenshots, earned turn-3510 fixture and restoration evidence. Linux execution,
physical Deck, sustained performance and whole-opening acceptance remain open.

Next: [East steam tile cue](GRAPHICS-EAST-STEAM-PLAN.md). Exact saved-state review
confirms the pale marks are the generic tile-cloud apostrophe, not missing vent
body art. Preserve the approved grates and existing gameplay. Normal saves,
preferences, 34 unrelated tracked changes and previous builds are preserved;
no storage cleanup was performed. Last hourly report remains 00:25 UTC.

## Hourly checkpoint — 2026-10-27 01:25 UTC

Reported: cudgel delivery complete and pushed through c709e7a63; 177 native
cases and actual Mac restart passed. Steam baseline and concept are captured;
original editable source approved for in-game trial. Native steam RED: 36 cases,
35 failed/1 passed, including two actual-paint failures on the old apostrophe.
Initial class-name exact filter selected zero tests and is not counted as evidence;
correct group_names run produced the retained east-steam-red.xml.

Coming-session goals: finish minimal tile-state routing, pass focused and neighbor
regressions, compare actual1x/2x gameplay and renewal/save-restart, inspect render
cost, review and push exact changes. Full six-scene opening and regional coverage
remain unfinished. Storage inspection-only. Last hourly report:01:25UTC.

### East steam continuation — 2026-10-27 01:42 UTC

Scoped native steam tile-cloud cue now passes211native tests and actual Editor
before/after, exact-reader, movement/wait and F5 checks. Five real clouds;
approved grate/tonic preserved. Source and editable art in
`ArtSource/Live16x24/east-steam`; full acceptance/limits in
`Docs/GRAPHICS-EAST-STEAM-PLAN.md`. Refresh packages after source push.
Next scene-level pass is North Patchbearer encounter motion/readability, not
whole-opening acceptance. Last hourly report01:25UTC. Storage inspection-only.


### North encounter motion — 2026-10-27 02:50 UTC

[North motion record](GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md) now verifies
ordinary full/empty walk, attack, hurt and recovery, real finite tonic use,
exact-reader consistency, death, glade return and North revisit/F5. Eight native
editable derivatives borrow approved idles.278native cases pass,1allocation
probe skips after its counter-control fails. Independent code/art/capture review
found no material issue. Five sampled sequences60frames and seven original Game
stills retained; no continuous-video or whole-scene claim. Profile is a bounded
Editor stationary/reader sample, not hardware performance acceptance.

Next freeze/push source and package normal MacContinue/restart. Then capture
East Cindercaller body motion/cast attribution beside animated Scrabbler; existing
Ember Spit FX/audio is functional. All six scenes have bounded evidence; whole
opening, remaining actors/services/regional kits, Linux execution and physical
Deck remain open. Normal saves/gameplay preferences and34foreign tracked edits
preserved. Storage inspection only. Last hourly report02:25UTC,next03:25UTC.


### North encounter delivered — 2026-10-27 03:01 UTC

Source`2375284dd94fc5614f9d676457fe4a921dd931ac` is verified on origin/main. Fresh compressed Mac/Linux
builds each succeeded with0errors/4warnings. Mac ordinaryContinue600 from the
earned close-approach checkpoint shows the full Patchbearer. Real combat triggered
healing16HP and consumed its tonic at640; exact Examine shows the empty cradle
and separate Antidote. Continued combat, retreat, F5 at660, full process quit,
relaunch andContinue preserve player6,19/22HP and Patchbearer6,22/6HP/empty.
Eight original1280x856 native-window screenshots include1280x800 game output.
These Mac stills validate state and presentation, not every animation interval;
the separate Editor60sampled frames provide motion evidence. No debug gameplay
changes or picture editing. Native278passed/1allocation-counter skip.

Current Mac: `Builds/Verification/2026-10-27-north-motion-2375284dd-current/CavesOfOoo.app`.
Current Linux archive: `Builds/SteamDeck/2026-10-27-north-motion-2375284dd-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.
Archive181files,128684199bytes,SHA256`c33ba3283ba1717f74c5738fa1bb1391d307c5a3d25f456f12b0c9f3006a60b4`. Launcher,credits,
licenses and checksum list included; byte/size/mode/path integrity checked.
No Linux execution or physical Deck claim. Known premature-thread-finalization
and ComputeBuffer shutdown notices remain. Normal save trees, gameplay
preferences, entire standalone preference map,34foreign tracked changes and
previous delivered Mac/archive match protection. Owned test saves archived,
reviewed generated settings restored. Storage inspection only;~410GiBfree.

Evidence:`Verification/Live16x24/packaged-north-motion-mac-play-summary.json`,
`packaged-north-motion-independent-review.json`,`packaged-north-motion-linux-archive.json`
and`packaged-north-motion-restoration.json`. Whole six-scene opening, regional
kits, Guarded/Parried timing and sustained hardware performance remain open.
Next is the East Cindercaller ordinary movement/cast baseline, as planned in
`Docs/GRAPHICS-EAST-CASTER-MOTION-PLAN.md`. Preserve existing spell FX/audio and
approved neutral body; carried FireMoss is not spell fuel. No new caster graphics
are claimed yet. Keep continuation active. Last hourly report02:25UTC,next03:25UTC.

## Current continuation and hourly report — 2026-10-27 05:25 UTC

The user priority is missing static sprites across every biome/area and coherent,
non-bare chunks. Nuanced new motion remains deferred. Portable equipment and
Sill24-frame terrain/woven walls/doors are delivered through cc130817a on main;
actual Mac Continue/full restart and matching Linux archive were verified.

Current slice: [Sodden travel](GRAPHICS-SODDEN-TRAVEL.md). Actual before-route,
imagegen and native source/integration done.99-case original RED96fail/3pass
then99GREEN; independent hidden-cultivation counter failed in both Sodden and
Sill, then the narrow saved guard correction passed295 selected native cases.
A post-run scene-restoration timing error occurred when root started Play;
retain that limitation and verify runner completion before later transitions.

Actual crossing entry, north/east raised route, Duckboard native Examine and
ordinary Break→removal of58,3 are captured. First real view revealed bright
repeated mire carpet and black backing beneath vegetation/boards. These are
named defects, not accepted polish. Source is revising only mire to weighted
static variants; runtime will preserve real dry Grass/StoneFloor beneath exact
neutral dry overlay owners. No generated backdrop or invented prop placement.

Coming-hour goals: finish those visual corrections, final crossing/shelter/works
whole-view/play/save/profile checks, review and exactpush, then ordinary Sodden
formations. Source audit finds six coherent formations/57static addresses;
DrownedCopse uses literal plain water and must not become mire. Actual density
and all-biome acceptance remain open. Storage remains inspection-only.
Last hourly report05:25UTC;next06:25UTC.

## Current continuation — 2026-10-27 05:48 UTC

Static sprite coverage across all biomes/areas and non-bare chunk composition
remain the priority. New subtle animation remains deferred. Sill and portable
equipment are packaged and on main through cc130817a. The new Sodden travel kit
covers the actual shelter, crossing and peat works only: 28 editable frames plus
unchanged borrowed materials, 304 native passes, 3 inconclusive allocation probes,
13 final actual Game captures and ordinary F5/Play-restart Continue at works39,16.
Bright repeated mire and black dry-overlay backing were corrected through actual
comparison. Fullness is still incomplete: regular grass and remaining real
service/plant/actor glyphs are listed in `GRAPHICS-SODDEN-TRAVEL.md`.

Next extend the same material kit to six ordinary Sodden formations with actual
current map/POI/saved-lair authority, before adding unrelated art packets. Preserve
Copse plain water. Follow the saved ordinary plan, real baseline/after captures,
RED→GREEN and independent review. Then cover missing Sodden services and Beating,
continuing all-biome, settlement, underground and special-site inventory. Current
packages are Sill; Sodden package refresh is pending. No Linux/Deck claim. Normal
saves and unrelated work stay protected; storage is inspection-only.
Last hourly report05:25UTC;next06:25UTC.

## Current continuation — 2026-10-27 06:13 UTC

District source d48f623f5 is on main. Ordinary Sodden extension now covers all
six natural formation samples with actual matched before/after screenshots.
22new cases went RED→GREEN; combined186=183passed,0failed,3 existing allocation
probes inconclusive. Real PeatBank reader, east-route combat/coating, walked
15,5→16,5→15,5 boundary and F53120/freshContinueHP4 verified. No gameplay
state repaired for screenshots. Fullness remains incomplete: plain-water blue
waves, missing actual owners, grass stipple and wide-board repetition.

See `GRAPHICS-SODDEN-TRAVEL.md` and
`Verification/Live16x24/sodden-ordinary-play-summary.json`. Exact source push
and fresh Mac/Linux packaging are next; current protected packages are still
Sill246bda61e. Future static services plan is preserved; then Beating and all
remaining areas. No nuanced animation or storage cleanup. Root owns Unity;
isolation restored and roots archived. Last hourly report05:25UTC,next06:25UTC.
