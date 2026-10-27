# Shared storage — body integration and bounded verification

**Current status, 2026-10-27 20:38 UTC:** implemented, with **606 distinct native passes** and actual before/after body and reader observations for all five genuine storage families. The immutable four sources were reused without new production pixels. Crate transfer and fresh Continue are observed; successful key unlock, destruction and mimic transitions are native-test evidence only. Source publication, fresh package verification and package profiling are next. No biome or whole area is declared polished.

The historical preimplementation records below retain the original source corrections and frozen scope. The current implementation log at the end supersedes their pending-runtime/test/after status. Shale source `a10e1330098c68039e560740cde07137dadc2ff3` and delivery `51810153080c76253561919c88184761924130b1` remain the published build baseline until this batch is pushed and packaged.

The next slice covers **five genuine storage owners: Chest, LockedChest,
StrongBox, Crate and OreCache**, plus an essential **MimicChest disguise
presentation guard**. The mimic is an actor, not a sixth storage family.
LockedChest and every supported living MimicChest use the unchanged Chest body,
both before and after awakening. Broad native chest coverage must not expose
the decoy by leaving it as a glyph. No lock/open/contents/damage/awake sprite
variants or overlays are included in this slice.
This is original Caves of Ooo presentation, inspired by the requested readable
pixel style; no copied Qud art or mechanical parity is claimed.

## 1. Outcome and bounded scope

Make existing storage recognizable and usable across ordinary caves, surface
regions and special sites. Wood cargo, hinged wooden storage, an iron strongbox
and a mineral cache should remain distinct at normal zoom. Preserve the five
owners' real access rules, drops, collision, contents and authored placement.
Do not create containers, improve loot, change locks or add an open-lid system.

The [prior shared-owner audit](Verification/Live16x24/post-limestone-shared-owner-audit.md)
establishes actual depth3/depth4 graph counts, source shapes and current missing
admission. Its proposed exclusion of MimicChest is superseded here: **the
disguise countercheck is mandatory for shared chest admission**, not deferred
actor polish or a reason to restrict ordinary boxes to selected regions.
Other creatures, corpses, merchant stock, furniture, sacks, urns and custom site
reskins remain outside this finite catalog.

At the retained depth3/4 census moments, the five storage families were hidden;
their counts are not visual acceptance, a safe route or permission to disclose
them. The new ordinary before observations below supersede that earlier census
limit for their named owners only. No grants, teleports, visibility changes or
convenient new props are needed.

### Actual before evidence — five genuine identities observed

Root's [before receipt](Verification/Live16x24/shared-storage-before-play.json)
records ordinary controls, Look/action menus and exact readers for all five
genuine owners. These are glyph baselines, not new native acceptance. Remote
Look proves disclosed subject identity, not adjacent access.

| Owner and actual site | Verified before action and earned checkpoint |
| --- | --- |
| Chest7483 at43,11, Sill10,10 | Player43,12,HP40. Reader shows Wood14/14; ordinary Open shows three grimoires, none taken. Captures01–05; fixture`shared-storage-chest99`,turn540. |
| Crate67305 at45,1, Stump Summit3,4 | Normal five Left reaches player45,2,HP29. Reader shows Wood10/10. Captures06–09; fixture`shared-storage-crate100`,turn9090. |
| StrongBox72917 at70,2, shallow0,9,1 | Normal approach reaches70,3,HP23. Reader/menu and actual unsuccessful Unlock preserve the real lock/solidity; no key granted. Captures10–14; fixture`shared-storage-strongbox101`,turn10910. |
| OreCache86894 at34,16, limestone16,13,4 | Remote Look from player44,16,HP33 selects the actual menu/reader, captures15–17. Subsequent ordinary Right inputs reach49,16; fixture`shared-storage-ore-cache102`,turn12140,HP33. No opening, adjacent access or loot transfer is claimed. |
| LockedChest39340 at61,3, Beating19,13 | Normal approach from59,9,HP38 reaches62,5,HP7 after hostile attacks and poison. Remote Look selects Wood18/18 and the combined locked state; captures18–21. Fixture`shared-storage-locked-chest-interruption103`,turn3630,HP7 preserves the interruption. No opening, successful unlock or transfer. |

Ordinary dormant/awake mimic observations and successful key unlock/transfer
remain unobserved in this phase. The first StrongBox refusal
is not a successful unlock. Root owns the continuing actual controls and receipt
extension; the plan author did not operate Unity or independently replay these
routes.

## 2. Verified sources and corrected premises

| Assumption | Source correction and required consequence |
| --- | --- |
| Opening produces a durable open-lid state | [ContainerPart](../Assets/Scripts/Gameplay/Items/ContainerPart.cs:15) has Contents, capacity and lock access, but no `Opened` field. Open dispatches an event and reports contents; the [input handler](../Assets/Scripts/Presentation/Input/InputHandler.cs:3444) opens the loot popup only if access succeeds and contents exist. Do not derive a persistent open/empty sprite from a click or contents count. |
| A key lock alone decides access | `ContainerPart.IsLocked` combines `Container.Locked` and owned `LockPart.IsLocked`. All four combinations matter. The former can still block access after a key clears the latter. |
| Solid=false means a chest stopped being a chest | Both [menu unlock](../Assets/Scripts/Gameplay/Items/LockPart.cs:126) and [bump unlock](../Assets/Scripts/Gameplay/Combat/PhysicsPart.cs:140) can clear Physics.Solid. Keep the surviving body recognizable. The first bump performs the unlock and blocks movement; a subsequent move can enter. Preserve this behavior. |
| All containers are structurally and materially equivalent | The five authored contracts below differ. StrongBox and OreCache have no authored Material/Destructible; Crate has no authored Thermal. Do not invent these parts or demand them for native art. |
| Broken storage leaves an empty broken box | [DestructionSystem](../Assets/Scripts/Gameplay/World/DestructionSystem.cs:148) spills contents, removes the owner and creates only declared wreckage. These three breakable boxes declare none. Test the real dropped objects and owner removal. |
| OreCache is a harvestable vein | Its only extraction mechanism is Container access, capacity6. It has no Harvestable part. Keep it separate from GlowQuartzVein and avoid a false depleted-mineral state. |
| The cave layout selector grants presentation authority | [UndergroundLayoutPlan](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs:22) is a cold-generation decision: eligible depth3+ columns have a25% roomed roll, otherwise natural strata. It is not current-owner authority and must not run per rendered cell. Storage is shared by both geometries and authored landmarks. |
| A native Chest can safely coexist with a glyph-only mimic | [MimicChest](../Assets/Resources/Content/Blueprints/Objects.json:27999) deliberately displays “chest” and `=`; legacy [chest mapping](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:2125) includes it. Its dormant native body must share the real Chest's approved pixels, scale and neutral tint. Do not add a telltale outline, sleep particle, teeth, badge or different reader portrait. |
| A body-only mimic resolver needs a dormancy scan | [AIAmbushPart](../Assets/Scripts/Gameplay/AI/AIAmbushPart.cs:57) uses DormantPushed as an initialization latch, not current sleep state. An unfinished owned DormantGoal is relevant to AI state tests, but this slice uses exactly the same body while dormant, waking and awake. No render-time goal scan or new state cue is needed. Preserve live owned Brain/AIAmbush/Container and actor-life guards; do not mutate goals. |
| A mimic wakes on sight or automatically on opening | The current blueprint has SightRadius8, WakeOnDamage=true, WakeOnHostileInSight=false, SleepParticleInterval0. [DormantGoal](../Assets/Scripts/Gameplay/AI/Goals/DormantGoal.cs:63) detects HP loss after establishing a damage baseline; Container opening has no wake action. The older [MimicSurprise scenario comment](../Assets/Scripts/Scenarios/Custom/MimicSurprise.cs:22) claiming SightRadius0 is stale. Do not fix or rebalance these mechanics in this art pass. |

### Exact genuine-storage contracts

All five authored bodies are initially solid and RenderLayer1. Capacity is
inventory authority, not a visible fullness meter. Surviving structural damage
can retain the same body; HP must not be pinned to its pristine value.

| Exact owner | Authored render/capacity | Real parts and essential presentation state |
| --- | --- | --- |
| [Chest](../Assets/Resources/Content/Blueprints/Objects.json:24850) | `=` / `&w`,10 | Wood; HP14/hardness1; Thermal. Ordinary unlocked body; any supported added legacy lock must remain truthful. |
| [LockedChest](../Assets/Resources/Content/Blueprints/Objects.json:25088) | `=` / `&Y`,10 | Wood;18/2; Thermal; reusable iron-key lock initially locked. Locked versus unlocked is actual access state, not closed versus open. |
| [StrongBox](../Assets/Resources/Content/Blueprints/Objects.json:30496) | `=` / `&W`,8 | Iron strongbox by description; iron-key lock initially locked. No authored Material, Thermal or Destructible. Preserve locked/unlocked access without inventing breakability. |
| [Crate](../Assets/Resources/Content/Blueprints/Objects.json:30304) | `0` / `&y`,8 | Wood, combustibility0.7;10/default hardness0; no authored Thermal. Cargo identity remains independent of item count. |
| [OreCache](../Assets/Resources/Content/Blueprints/Objects.json:30563) | `*` / `&w`,6 | Solid Container without authored Material, Thermal, Destructible or Harvestable. Mineral storage identity, not a node to mine. |

### Real sources and geographic benefit

[ContainerPlacementService](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:116)
uses OreCache, Crate, BoneCache and StrongBox underground. Its surface Spread,
Beating/desert, Ruins/Overwrit and settlement pools reuse the proposed families;
Sodden includes Crate. Grovelands surface ecology deliberately uses other
containers, so this change must not insert manufactured boxes there. Existing
containers carried by an authored site still receive their exact identity.

The [underground pipeline](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:580)
adds landmarks and containers independently of natural/roomed geometry.
[MineGallery and Ziggurat](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs:201)
place Chest/LockedChest with their own loot tables; native storage does not
admit their surrounding masonry, vines, mineral nodes or traps.

The placement service can substitute a MimicChest on a1-in-12 roll in Ruins,
outside Village/Camp, and [lairs](../Assets/Scripts/Gameplay/World/Generation/Builders/LairPopulationBuilder.cs:133)
also place mimics. Therefore a biome-only storage rule would leave a real
disguise defect elsewhere. Generation already stocks containers, including a
currency fallback; rendering must never stock, refill or consume generation RNG.

## 3. Native art and state ownership

| Body | Frozen unchanged source | Body contract; actual after comparison pending |
| --- | --- | --- |
| Chest, LockedChest and living MimicChest | Unchanged `opening-anchors` frame6,16×24 | Same cached body and neutral base tint, including dormant, waking and awake mimic. Preserve real event/intent cues. A missing shared source must refuse consistently, without a local fallback bypass exposing the mimic. |
| Crate | Unchanged `opening-cellar` frame1,16×24 | Verify in a cave and a surface scene; retain approved pixels. |
| StrongBox | Unchanged `Environment/cont_strongbox`,16×16 | Direct centered16PPU FullRect body; same image locked and unlocked. Access remains in the actual menu/action, without a new visual lock cue. |
| OreCache | Existing `Environment/cont_ore_cache`,16×16 | Direct centered16PPU fullrect trial; verify mineral-cache silhouette against surrounding floor and genuine veins. |

The [part extraction and concept critique](../ArtSource/Live16x24/concepts/shared-storage/part-extraction.md)
and [reuse manifest](../ArtSource/Live16x24/concepts/shared-storage/reuse-manifest.json)
record four immutable PNGs, selected frame rectangles, current import metadata,
root-attributed HEAD metadata comparisons and editable-source pins. The concept
is a direction study only: altered lighting, positions, UI, chest pixels and
uneven pixel geometry are rejected. Only five inputs reached imagegen; the
OreCache texture was omitted, so its generated mineral shape is not approved.

Square sources occupy their natural1×1 bounds in the rectangular grid; do not
stretch them to16×24. Cache centered Sprite/Tile views and preserve original
PNG/import/source bytes. Existing actor-square and reader UV paths establish
the mechanism, but not acceptance of these particular props.

**Mimic body policy:** five noncreature body contracts remain separate from a
narrow exact, live MimicChest contract with owned Brain, AIAmbush and Container
and valid living-actor semantics. Use the Chest body unchanged while dormant,
waking and awake. Do not pin HP to30 or demand a particular current goal, AIState,
DormantPushed value or invented Revealed flag. Preserve actual names, targeting,
attitude, reader text, one-event damage/status colors and current combat/intent
cues. The body neither advertises a new service nor hides these existing cues.

Because the image is state-invariant, no goal-stack scan, awake derivative,
lock overlay or source PNG change is warranted. Catalog/portrait queries must
not push/pop goals, initialize the damage baseline, wake an actor or dispatch
extra gameplay/Render events. Real wake/death/save behavior still needs paired
native counterchecks so a static image cannot conceal a broken live contract.
World and authorized readers share the same selected-body identity; remembered
terrain cannot query live hidden actors.

## 4. Proposed implementation boundary — not code yet

Use a finite `FlatPixelSharedStorageCatalog` companion with independently
optional borrowed resources, cached views, a pure owner/state lookup and
Dispose that destroys only its own Sprite/Tile objects. Mirror current shared
owner conventions in [FlatPixelGreyboxCatalog](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:1005):
current manager/graph, placement and disclosure remain authoritative; world
and authorized reader resolve the same body. Retain approved specialized
opening/site roles before widening ordinary unskinned owners.

Reject unknown VisualID/variant, wrong glyph, foreign/duplicate required parts,
custody, unsupported multicell footprints, conflicting service meaning and
removed/destroyed bodies. Do not blanket-reject real surviving damage or the
unlocked passable state. Admit no other Creature through generic ContainerPart;
MimicChest is the explicit exception with its own ownership/live-state checks.
Unsupported custom states retain truthful fallback without new disclosure.

This companion grants no ground, ambient-backdrop, water or neighbor authority.
It does not require a particular biome, depth band, room layout or remembered
loot value. Preserve the current single Render-event receipt for supported
damage/status colors rather than dispatching another event from a catalog or
portrait. No per-cell allocation, asset slicing, world scan, RNG or generation
call is warranted. No goal-stack scan is needed for this state-invariant body.

### Frozen API proposal for the native RED fixture

| Surface | Contract |
| --- | --- |
| Companion | `FlatPixelSharedStorageCatalog` |
| Default construction | Parameterless constructor loads the four optional sources once. |
| Injection | `(Texture2D anchors, Texture2D cellar, Texture2D strongbox, Texture2D oreCache)`; explicit null stays absent, with no implicit resource reload. |
| Known-role gate | `static bool HasKnownRole(Entity owner)` for the five exact identities and MimicChest. No generic ContainerPart or substring admission. |
| Body selection | `bool TryResolve(Entity owner, out Tile tile)`; validates current owner semantics without world scans or game events. |
| Lifetime | `Dispose()` releases only cached Sprite/Tile views, never borrowed textures. |
| Shared caller field | `FlatPixelGreyboxCatalog._sharedStorageArt`; existing world/current-graph/placement/disclosure and authorized-reader gates stay authoritative. |

The four immutable resources are `Sprites/Live16x24/opening-anchors` (frame6),
`Sprites/Live16x24/opening-cellar` (frame1),
`Sprites/Environment/cont_strongbox` and `Sprites/Environment/cont_ore_cache`.
The first two retain16×24 cells; the latter two remain centered16×16 at16PPU.
No new atlas or import is required. Known-role refusal must not fall through an
older local resolver and bypass ownership, optional-resource or disguise guards;
retain approved specialized roles only where their exact existing contract holds.

**Implementation snippet:** none. These declarations freeze the test-facing
proposal; no production draft is authorized before genuine native RED.

## 5. Finite milestones and counterchecks

1. **Ordinary before route and design.** Root captures disclosed examples of
   all five owners, real access menus/readers and a dormant mimic where ordinary
   travel permits. Reuse earned healthy cave/surface fixtures unchanged. Record
   interruptions and unavailable states honestly. Reinterpret the actual
   views with the four immutable source references; critique silhouette and
   contrast in the actual settings. Root chose body-only reuse after review;
   no lock/awake derivative or invented cue is part of this finite slice.
2. **Tests first.** Run the finite native fixture to genuine RED before any
   production runtime draft. Preserve raw failure evidence and distinguish
   compile/fixture corrections. New art has its own source RED, reproducible
   editable geometry, binary-alpha/dimensions/state checks and corruption
   counters. Direct immutable reuse needs input/view verification, not new art.
3. **Minimum integration.** Implement only the companion and shared world/
   reader seams. Preserve all approved source art, normal save data and existing
   actions. Confirm focused GREEN, counterchecks and relevant existing suites;
   obtain an independent source review before the actual after route.
4. **Actual comparison and persistence.** Repeat ordinary world/menu/reader,
   unlock, transfer, destruction and mimic encounter where observed. Verify
   incremental redraw, return travel and fresh Continue with earned state.
   Inspect the entire chamber and a surface view at normal zoom, not only the
   icon. Retain missing states and composition debt rather than manufacturing
   a convenient test scene. Measure only existing instrumentation as needed.
5. **Deliver the bounded result.** Update this log and coverage without promoting
   whole areas to polished. Commit exact owned files with the required template,
   fetch/rebase/push, then refresh source-bound Mac/Linux packages. Preserve
   prior builds, normal saves/preferences and unrelated changes. Actual Mac
   play, archive integrity and physical Deck execution are separate gates.

The finite RED suite should pair these invariants with contrary cases:

- Five factory positives in both cave and surface current graphs; disclosed
  world and authorized reader agree. Hidden/current-graph replacement, covered
  selection, custody, malformed owner and foreign/custom variant do not gain
  native authority. A retained reader retires after real movement/removal.
- Real menu and bump unlocking with the correct reusable key, wrong/no key and
  combined legacy/key locks. Verify access and both Physics outcomes; a key-only
  unlock must not falsely clear legacy access authority. The unchanged body
  stays native for both physical outcomes; menus/actions retain actual access.
  Check submitted identity after real actions without inventing sprite-state
  RED expectations or an unnecessary lock-cue dirtiness fix.
- Real Open/Take/TakeAll/Put preserve custody and currency rules while the body
  gains no invented opened/full/empty state. Reuse existing
  [lock-authority tests](../Assets/Tests/EditMode/Gameplay/World/DensityContainerLockAuthorityTests.cs)
  and [bump-unlock mechanics](../Assets/Tests/EditMode/Gameplay/Items/LockKeyBumpUnlockTests.cs),
  not a parallel inventory simulation. Contents are not inspected to predict loot.
- Nonlethal structural damage keeps the same surviving body. Actual destruction
  of Chest/LockedChest/Crate spills content and removes the body without phantom
  wreckage. Actual StrongBox/OreCache positives need no nonexistent structure.
- Dormant mimic and real Chest use identical native body/neutral tint and reader
  crop. Rendering/neighbor/portrait calls do not advance goals, wake the actor,
  emit sleep cues or consume an extra Render event. Missing shared chest art
  cannot make only the real chest native. Real supported status cues still work.
- Establish the mimic's damage baseline through its real turn path, then apply
  nonlethal damage and let the normal ambush path wake it. Cover finished goal
  before cleanup and awake after cleanup; nearby hostiles alone do not wake it.
  Require the same unchanged body across those genuine transitions while real
  damage/intent cues remain intact. Preserve current dormant/awake save state using existing
  [ambush save tests](../Assets/Tests/EditMode/Gameplay/AI/FirstHourAmbushSaveTests.cs:68).
  Do not use a manually set wake flag as the only positive proof. Real death
  removes the live body and retains the existing corpse/drop policy.
- Missing/malformed optional resources refuse independently; square geometry,
  shared reader UV, one-time allocation and disposal preserve borrowed textures
  and peer catalogs. No native family gains underlying ground/hazard ownership.

## 6. Review, divergences and continuation

| Decision | Scope consequence |
| --- | --- |
| Supersede the prior audit's mimic exclusion | Preserve the disguise as a required companion guard. Still five genuine storage families; no generic creature or container admission. |
| Body-only reuse of four immutable sources | No new atlas, lock/open/empty/damage overlay or awake derivative. LockedChest and living MimicChest share Chest; access and combat remain existing system cues. |
| Supersede the earlier goal-scan proposal | The selected body is identical dormant/waking/awake, so render-time goal inspection has no purpose. Keep true live-owner guards and mechanical transition tests. |
| Preserve existing unusual mechanics | No new open state, StrongBox structure, OreCache harvest, trigger-on-open ambush or generation rewrite. |

🔵 Source sweep completed for the cited current contracts. The mimic's latch,
damage-baseline timing, color mismatch in the legacy scenario and passable
post-unlock boxes are explicit implementation constraints.

🟡 The real-chest/native versus disguised-mimic/glyph distinction would reveal
the decoy. The plan now requires shared presentation and mechanical
counterchecks; this is **not fixed in runtime yet** and blocks releasing broad
chest integration on its own.

🧪 Five actual genuine before identities and the immutable-source comparison
are recorded above. Ordinary mimic observation, native tests, after input/render
checks, performance and storage package checks remain pending. The new derivative count is zero.
Source review cannot prove these. Existing container density, empty chamber
composition, unseen special-site reskins and whole-area polish remain open.

Implementation log: source preflight updated with root's actual before evidence,
body-only pruning and the frozen API proposal before native RED. No production
implementation or passing-test claim. Source review also adds only
`ArtSource/Live16x24/concepts/shared-storage/reuse-manifest.json` and
`part-extraction.md`; approved art and metadata remain unchanged.


### Historical chosen next-phase continuation — 2026-10-27

After the current shale delivery, prioritize these five storage identities and
the mandatory mimic disguise guard before another cave-only material band.
The source-verified [placement pools and mapping](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:49)
contain at least one proposed family in **five of the six authored surface
biomes**: Spread, Sodden, Beating, Overwrit and Stump. They also occur in the
underground and village/camp pools, with additional registered Cave, Desert and
Ruins mappings. This is family presence in real placement sources, not five
completed regions or a promise that every zone contains every box. Grovelands
surface ecology deliberately excludes manufactured boxes; preserve that rule.

The retained depth3/4 censuses contain nine real objects across all five
identities: Chest1, LockedChest2, StrongBox1, Crate2 and OreCache3. All were
hidden at those census moments. **Actual disclosed before sites remain to be
selected and captured through ordinary play.** Start with a preserved earned
cave checkpoint and a surface crate/storage route, recording real menus,
authorized readers and access states; a reachable dormant-mimic comparison
must preserve the existing information policy. Counts do not establish visual
acceptance, safe approach routes, or permission to reveal hidden owners.

Keep approved Chest/Crate art and the existing square StrongBox/OreCache
sources immutable unless an actual named defect requires a scoped derivative.
World/reader identity, combined lock authority, genuine destruction/loot and
mimic disguise remain the essential implementation gates above. No new open,
empty, harvest or merchant service may be implied by the image. Full biome/
area composition and later serious polish remain separate and unaccepted.

The [slate preflight](GRAPHICS-SLATE-CAVES.md) stays source-only in the future
material queue; its depth7/8 baseline and acceptance are still pending. Shared
storage broadens useful coverage while surface, underground and special-area
gaps remain explicit. This continuation changes only this phase document and
does not advance any shared ledger, polish status or hourly-report clock.


### Current body-only continuation — 2026-10-27

Shale sourcea10e1330098 and delivery51810153080 are verified; shared storage is
now the active body-reuse phase. Root's normal routes saved99/100/101; remote
OreCache reader15–17/fixture102 and LockedChest reader18–21/fixture103 complete
the five genuine before identities. The latter route lost HP38 to7 through
existing combat and is retained as interrupted, without an access claim.
Ordinary mimic observation remains pending. Four unchanged resources provide all planned bodies;
no lock/open/contents/damage/awake sprite states are authored or promised.

The above API and scope corrections are saved before test implementation.
Native tests must preserve real combined lock authority, successful/refused
access, passable unlocked boxes, contents custody, structural removal and live
mimic AI/damage/intent behavior. They must not manufacture sprite cues or demand
new redraw plumbing for a state-invariant image. Actual after, performance,
source publication, fresh storage packages and serious all-area polish remain
pending. No shared ledger, hourly clock or approved native art changed here.


### Immutable-source extraction checkpoint — 2026-10-27

The concept and four original images were inspected directly. The accepted
idea is distinct wood cargo, reinforced wood storage, compact iron storage and
mineral-cache identity. No generated pixels, positions, illumination or UI enter
production. Zero-based anchor frame6 is x96, the seventh atlas slot; the
generated “sixth frame” label is not production authority.

The read-only manifest verifies binary alpha and current Point/16PPU/no-mipmap/
uncompressed/Clamp imports for all four sources. Every stored source-cell pixel
of anchors (4,608) and cellar (3,456) agrees with its unchanged runtime atlas.
Existing Blender files and authoring scripts are hashed, not rebuilt; no fresh
legacy-script reproducibility claim is made. All26 protected current inputs
match at the end of this documentation task. Root supplied exact HEAD518 meta
hashes: anchors/cellar differ only in trailing whitespace, while StrongBox and
OreCache match raw HEAD bytes. Preserve all current metadata rather than
normalizing unrelated work.

This is four unchanged body frames for five genuine storage identities plus
one explicit living mimic disguise exception. It establishes source reuse
readiness, not native behavior, actual after comparison or area polish.


## Implemented slice and self-review — 2026-10-27 20:38 UTC

`FlatPixelSharedStorageCatalog` owns four cached Sprite/Tile pairs and borrows
unchanged textures. The five genuine storage identities plus live MimicChest
enter all four old/current world and authorized-subject seams in
`FlatPixelGreyboxCatalog`. Known-storage refusal cannot fall through to the
older permissive opening mapping. Explicit injected null resources remain
null; borrowed anchor/cellar textures are retained for existing callers.

Representative saved implementation:

```csharp
chest = Build(openingAnchors, 192, 24, 96, "chest");
crate = Build(openingCellar, 144, 24, 16, "crate");
strongBox = Build(strongBoxTexture, 16, 16, 0, "strongbox");
oreCache = Build(oreCacheTexture, 16, 16, 0, "ore cache");
```

The square sources use their original full bounds at16PPU, centered in the
16x24 cell, rather than stretching or borrowing the old cropped StrongBox
subsprite. Guards require owned visible Render/Physics/Container/Examinable,
correct identity and body contract, actual placement/disclosure at the caller,
and no custody/custom service/footprint conflict. Surviving damaged or unlocked
passable boxes retain their body. A live owned mimic shares the exact Chest
body while dormant, waking and awake; lookup never inspects goals or modifies
AI. This provides body/color parity, not a redesign of existing hostile outline,
health or Flesh reader disclosure.

The existing painter preserves real changed Render color cues once; the base
native image stays white. No production render event, lock, content, damage,
movement, placement or turn behavior was added. Construction caches four
resources; render lookup does bounded part checks and no Resources.Load,
Sprite.Create or goal scan. Source cost review is not a physical Deck profile.

### Saved RED, corrections and GREEN

- 43 native RED failures before runtime integration are retained in
`shared-storage-native-red.json`, with source snapshot and receipt. The first
compile attempt used inaccessible internal SpatialZone; its saved correction
uses public zone cell membership. A zero-discovery XML attempt is explicitly
invalid evidence.
- Independent review found the old injected anchor constructor regression:
39 existing cases produced five failures. Retaining borrowed anchor/cellar
resources fixed the regression; all39 now pass. The regression RED is preserved.
- The test helper prematurely disposed a borrowed catalog across render/save
checks. Its pre-correction source is retained; detaching borrowed lifetime did
not change the43 behavioral assertions.
- Final136 focused plus470 related cases pass with zero overlap/failure/skip:
**606 distinct native cases**, including body/reader/disclosure, authority,
combined locks, correct/wrong key, real Open/Take/Put/TakeAll, custody, structural
spill/removal, real mimic damage/wake/save/death, Render cue and disposal checks.
- Raw completed TestJob.Result JSON and actual job metadata are authoritative.
The stale zero-case XML callback is not substituted for either GREEN run.

### Actual before/after results and boundaries

See `shared-storage-before-play.json`, `shared-storage-after-play.json`, and
`shared-storage-independent-review.md` for exact images, hashes and review.

| Site | Actual result |
| --- | --- |
| Stump Summit, Crate67305 | Native wood cargo, matching Wood10/10 reader; real Open and EmberFruit67306 transfer; earned104 and fresh Continue retain player custody and remaining67307/67308. Capture05 has PauseUI;05/06 pixel equality is rejected, while functional restoration is verified. |
| Sandstone depth1, StrongBox72917 | Full iron body and matching reader; actual Unlock refusal preserves real lock and solidity. No key granted. |
| Old earned Sill, Chest7483 | Native wood storage and Wood14/14 reader; actual Open retains all three real grimoires, none taken. |
| Limestone depth4, OreCache86894 | Compact mineral body and matching reader through ordinary remote Look; no adjacent access or transfer claim. |
| Beating, LockedChest39340 | Native Chest body and Wood18/18 reader at preserved combat interruption; HP7/poison/confusion unchanged through inspection. No new turn, unlock or safe route claim. |

All five full isolated roots were archived and verified before Finish; all
stopped with play=false, root=null and clean scene. No normal saves were used
for testing. The five earned before checkpoints remain unchanged; new104 is
explicitly the crate's real post-transfer save. Existing regional Continue works; the additional ordinary sandstone
strongbox→surface stairs→same cave→same strongbox trip preserves HP23 and
original owner72917, earned105 turn11290. See the travel/profile supplement. An ordinary mimic observation remains open.

### Whole-scene review and remaining work

The images retain mechanically truthful layout and density. They also expose
unfinished presentation: Stump's broad sparse center and subtle blocking domes;
the old Sill graph's generic grass/glyph water; the limestone cache's subdued
normal-zoom contrast; Beating's remaining glyph actors. These are separate
coverage/polish debt, not reasons to fabricate props, improve loot or promote a
whole-area status. All55 area entries and five category audits remain without
bounded polish acceptance. No nuanced animation work was added.

Source, independent native and actual image reviews are complete; publication
and source-bound fresh Mac/Linux package checks follow.
Physical Deck play remains untested. Storage cleanup is inspection-only.


### Travel/profile supplement — 20:43 UTC

Actual strongbox101 began at70,3,HP23, traveled by normal movement to the known
stairs56,7, ascended to surface0.9.0 and descended back, then returned to the
same strongbox70,2. F5earned105 at70,3,turn11290,HP23. Captures17/18 and full
root archive are in the after receipt. Surface0.9.0's broad sparse scraped
field is a new named polish debt, not polished-area acceptance.

The117.230491-second Editor sample records45,131 frames without capacity wrap;
raw CSV samples and true maxima are retained in `shared-storage-profile.json`.
Renderer mean0.132917ms,p950.193ms,max93.922250ms; Input mean0.033549ms,
p950.028625ms,max441.775917ms; Main Thread mean2.592895ms,p955.465125ms,
max1798.074834ms; GC mean50,320.066bytes,p9518,509bytes,max440,784,590bytes.
Movement, stair loading, idle and MCP/CUA instrumentation are mixed. There is
no matched before, isolated adapter cost, demonstrated speedup, sustained
standalone benchmark or Deck performance claim. Recorders were stopped/disposed
before Finish. Independent body/reader review passed with cave contrast debt.

🔵 Five genuine world/reader identities, native counterchecks and scoped
resource lifetime are verified. 🔵 The old injected-resource regression was
fixed before commit. 🧪 Ordinary mimic observation and successful-key actual
play remain open; native checks cover their contracts. ⚪ Whole-area polish,
remaining regional families and physical Deck remain outside this acceptance.

Files: new shared catalog/meta and43-case native suite/meta; modified greybox
catalog; this phase plan and five living records; immutable reuse/concept
record; before/after images, seven earned save pairs, native evidence/source
corrections, profile samples and independent review. No approved source pixels
or gameplay mechanics changed. Fresh package gates remain pending.
