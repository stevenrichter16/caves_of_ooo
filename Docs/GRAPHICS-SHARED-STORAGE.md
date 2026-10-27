# Shared storage — source preflight and implementation plan

**Current status, 2026-10-27:** shared storage is the chosen next coverage
family after shale delivery. Shale source
`a10e1330098c68039e560740cde07137dadc2ff3` is on main, as verified by root;
fresh shale packages are currently in progress. This storage phase remains
**source-only planning**: actual storage before sites/captures, concept/parts
review, native RED, integration, after comparison and packaging are pending.
No runtime, tests, art, generation changes or storage gameplay acceptance are
introduced by this update. The original preflight was recorded against
limestone source `7f49fb70054b7b29957dacd17c999ae4b521c9fd`.

The next slice covers **five genuine storage owners: Chest, LockedChest,
StrongBox, Crate and OreCache**, plus an essential **MimicChest disguise
presentation guard**. The mimic is an actor, not a sixth storage family. Broad
native chest coverage must not expose the decoy by leaving it as a glyph.
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
them. Locate owners through ordinary play before selecting the comparison route.
No grants, teleports, visibility changes or convenient new props are needed.

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
| DormantPushed means currently asleep | [AIAmbushPart](../Assets/Scripts/Gameplay/AI/AIAmbushPart.cs:57) uses this as an initialization latch that remains true after waking. Inspect an unfinished owned DormantGoal, as the existing [creature state resolver](../Assets/Scripts/Presentation/Rendering/SpreadCreature3DLibrary.cs:89) does. `WakeRequested` can finish a goal before stack cleanup. |
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

| Body | First source candidate | Production decision still required |
| --- | --- | --- |
| Wooden Chest and dormant MimicChest | Unchanged `opening-anchors` frame6,16×24 | Same cached body and neutral tint for both; a missing shared texture must refuse both consistently. No separate dormant-mimic asset. |
| Crate | Unchanged `opening-cellar` frame1,16×24 | Verify in a cave and a surface scene; retain approved pixels. |
| StrongBox | Existing `Environment/cont_strongbox`,16×16 | Direct centered16PPU fullrect trial, not resizing. Decide the smallest truthful lock cue from actual before/concept review. |
| OreCache | Existing `Environment/cont_ore_cache`,16×16 | Direct centered16PPU fullrect trial; verify mineral-cache silhouette against surrounding floor and genuine veins. |
| LockedChest / lock cues | Chest body plus only necessary original derivative or overlay | Locked cue follows combined access authority. No arbitrary open/empty/damage frame grid. Exact frame count remains pending actual reference review. |

Square sources occupy their natural1×1 bounds in the rectangular grid; do not
stretch them to16×24. Cache centered Sprite/Tile views and preserve original
PNG/import/source bytes. Existing actor-square and reader UV paths establish
the mechanism, but not acceptance of these particular props.

**Mimic state policy:** five noncreature body contracts remain separate from a
narrow exact, live MimicChest contract with owned Brain, AIAmbush and Container.
Dormancy is the presence of an unfinished owned DormantGoal; it is not HP30,
AIState.Idle, DormantPushed or an invented Revealed flag. Its regular presentation
must not disclose the ambush early. Preserve current names, targeting, attitude,
reader text and one-event damage/status cues; this pass does not redesign their
information policy. No renderer query may push/pop goals, initialize a damage
baseline, wake the actor or fire gameplay events.

After the real wake transition, keep existing combat/intent cues truthful. A
single essential awake static derivative is permissible only if actual review
finds the shared body misleading; no optional animation work is included. Any
state art must use the actual goal state, including the finished-but-not-popped
interval. Keep the same authorized state in world and reader without querying
live hidden actors through remembered terrain.

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
call is warranted. A constant-size scan of the mimic's current goal stack is
the existing pure state pattern, not a full-world search.

**Implementation snippet:** none; no production draft is authorized before
the native RED boundary. Final field/constructor names and any lock-cue asset
contract must be agreed with the test author after before/parts review.

## 5. Finite milestones and counterchecks

1. **Ordinary before route and design.** Root captures disclosed examples of
   all five owners, real access menus/readers and a dormant mimic where ordinary
   travel permits. Reuse earned healthy cave/surface fixtures unchanged. Record
   interruptions and unavailable states honestly. Reinterpret the actual
   views with approved source references, extract only necessary lock/awake
   parts, then decide direct reuse versus new original native derivatives.
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
  unlock must not falsely clear the legacy lock cue. Check submitted incremental
  paint after both action paths, not only direct calls to a resolver.
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
  Preserve current dormant/awake save state using existing
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
| Reuse accepted body pixels where possible | No mandatory new atlas or animation grid. New lock/awake derivatives require an actual named readability need. |
| Preserve existing unusual mechanics | No new open state, StrongBox structure, OreCache harvest, trigger-on-open ambush or generation rewrite. |

🔵 Source sweep completed for the cited current contracts. The mimic's latch,
damage-baseline timing, color mismatch in the legacy scenario and passable
post-unlock boxes are explicit implementation constraints.

🟡 The real-chest/native versus disguised-mimic/glyph distinction would reveal
the decoy. The plan now requires shared presentation and mechanical
counterchecks; this is **not fixed in runtime yet** and blocks releasing broad
chest integration on its own.

🧪 Actual scenes, exact derivative count, native tests, input/redraw timing,
visual state transitions, performance and package checks remain pending.
Source review cannot prove these. Existing container density, empty chamber
composition, unseen special-site reskins and whole-area polish remain open.

Implementation log: source-only plan saved; no implementation snippet or
passing-test claim. Files changed by this task: **this document only**.


### Chosen next-phase continuation — 2026-10-27

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
