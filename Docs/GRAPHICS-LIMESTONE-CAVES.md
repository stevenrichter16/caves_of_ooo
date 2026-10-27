# Ordinary limestone caves — finite material coverage plan

**Current phase: actual depth3 entry/return and depth4 passage/chamber baselines,
read-only owner census and source preflight. No new limestone runtime, tests,
native artwork or acceptance yet.** This phase follows
the delivered shallow-cave source `1ae80a3555e13ce77ff440a6054b732f00888dca`.
Its bounded Mac package, Linux archive integrity and restoration are verified;
the later delivery-evidence commit remains pending. First scope is
exact `LimestoneFloor` and `LimestoneWall` in conservatively eligible ordinary
depths3–4, with shared Rubble, Stalagmite and both stair identities as candidate
unchanged reuse. Shale and deeper bands, room props, hazards, creatures, loot and
special sites remain separate coverage. All-world coverage and later serious
biome/area polish remain required; this plan advances no polish status.

## Actual starting evidence and remaining capture work

The [actual baseline receipt](Verification/Live16x24/deeper-strata-before-play.json)
records the current Mac package, ordinary controls,1280×800 game view inside a
1280×856 captured window. From packaged-shallow-caves86 at depth2,42,12,HP33,
three ordinary Left commands reached the real down stair39,12; normal `>` entered
`Overworld.16.13.3` at39,12,HP33. The sidebar identifies limestone floor while the
actual scene still uses glyph fallback. F5 earned
[fixture87](Verification/Live16x24/Fixtures/deeper-strata-limestone87/Quick.json)
atturn11990. Normal `<` returned to sandstone depth2. The two actual images are
`deeper-strata-before-01-limestone-entry.jpg` and
`deeper-strata-before-02-sandstone-return.jpg` in the verification directory.
The ledger author viewed entry01 directly; control/transition facts belong to
root's recorded ordinary play, not a simulation run by this author.

The subsequent actual Editor entry is retained as
`deeper-strata-before-03-editor-entry.png`, with a read-only live census in
[deeper-strata-before-depth3-census.json](Verification/Live16x24/deeper-strata-before-depth3-census.json).
At39,12,HP33, this graph contains1250 LimestoneFloor(454currently visible),531
LimestoneWall(89visible),154Rubble(66visible),3Stalagmite(1visible),and the visible
up/down stairs at39,12/40,12. These are actual owner counts, not native-art counts
or whole-chunk visual acceptance. No Pillar/BrokenColumn is in this graph.

The same census records15VineWall,LockedChest,PressurePlate,IronKey,ChoirTendril,
GlowQuartzVein,OreCache,2FrostVent,BrinePool and2Crate; all were outside current
visibility at this observation. Two MarlbackScrabblers exist,one visible. Keep
these owners as explicit separate coverage; do not disclose their hidden
placement through presentation or label the scene landmark-free. Ordinary
columns can contain a small Ziggurat. This census does not prove which historical
geometry recipe ran or establish actual inspected landmarks.

Root subsequently traveled normally todepth4; images
`deeper-strata-before-04-depth4-entry.png`, `05-depth4-passage.png` and
`06-depth4-chamber.png` preserve that route. The
[depth4 census](Verification/Live16x24/deeper-strata-before-depth4-census.json)
was taken at40,12,HP33:1331LimestoneFloor(58visible),426LimestoneWall(26visible),
161Rubble(8visible),2Stalagmite(nonevisible),up stair40,12 anddown39,13,bothvisible.
Root's later wider-chamber position was44,16,HP33; the entry census's visibility
counts must not be applied to that later position.

Depth4 also contains13generic `Wall` owners (MineGallery material gap),15VineWall,
Chest/LockedChest/StrongBox,2GlowQuartzVein,2OreCache,IronKey,PressurePlate,
ChoirTendril,CaveBear and2TarSeep; all were outside visibility at the census.
No Pillar/BrokenColumn appears in this graph. Exact genericWall andVineWall must
retain their meanings; their presence neither admits them as limestone nor
proves a roomed/natural recipe. Hidden contents remain source/census coverage
facts, not visually inspected landmarks or permission to disclose them.

Current evidence is therefore entry/return plus depth4 passage/chamber views
and read-only owner census. No whole-chunk composition, historical roomed-status
audit, shale depth5 or new native limestone presentation is established. Review
these actual views and closer wall/floor/rubble/stair readers and blocking
before concept selection; root retains control of the ongoing normal-play
baseline.
Use short observed movement batches and existing supplies; record danger or
interruption without changing health, AI, inventory, visibility or generation.

## Preimplementation verification and corrections

The cited production files were reopened for this plan. No Qud parity is
claimed: this is CoO-original presentation of existing CoO semantics, with
unchanged mechanics. The table is a saved source correction before new code.

| Potential false premise | Verified correction and source |
| --- | --- |
| All underground stone can share sandstone admission and art | [SolidEarthBuilder.cs:42](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs:42) maps sandstone1–2, limestone3–4, shale5–6, then slate/quartzite/obsidian. This first kit must not widen the old depth1/2 policy or recolor sandstone owners by depth alone. |
| Limestone wall uses the sandstone HP30/hardness2 contract | [Objects.json:8466](../Assets/Resources/Content/Blueprints/Objects.json:8466) gives `LimestoneWall` structural HP35/max35,hardness3,Rubble wreckage; render`#`,color`&w`,background`^K`,layer0. Its [Wall parent:3280](../Assets/Resources/Content/Blueprints/Objects.json:3280) supplies solid physics, Stone/Mineral material and Thermal. Surviving1..35 remains rock; no creature Hitpoints stat is required. |
| LimestoneFloor needs invented Material/Thermal parts | [Objects.json:8799](../Assets/Resources/Content/Blueprints/Objects.json:8799) inherits Terrain, render`.`,variants`...,.'` plus backtick,color`&w`,layer0,passable; it has no authored Material/Thermal/Destructible. Do not mutate or reject the real factory shape to fit a copied wall predicate. |
| A saved zone ID proves its original generation recipe | [WorldLocationContext.cs:5](../Assets/Scripts/Gameplay/World/WorldLocationContext.cs:5) is derived nonserialized authority. The [current shallow guard:72](../Assets/Scripts/Presentation/Rendering/FlatPixelShallowCavesCatalog.cs:72) rechecks attached manager, exact cached object,current map/POI and surface-keyed saved-lair claim. This is current eligibility, not historical provenance. |
| Checking present POI alone excludes old special graphs | [LairStacks.cs:203](../Assets/Scripts/Gameplay/World/Generation/LairStacks.cs:203) provides read-only `HasSavedColumn`; use the canonical surfaceZ0 key. `Inspect`/`TryPlan` initialize or consult mutable generation state and must not run per rendered cell. Preserve registered sinkhole, Root, pilot and authored-column exclusions. |
| All limestone scenes are natural caves | [UndergroundLayoutPlan.cs:37](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs:37) permits a stable25% roomed choice atdepth3+ only for ordinary Spread/Sodden/Beating columns with required content. Current16,13 must be audited; a single viewport does not prove its layout. The previous0,9 Overwrit column cannot establish roomed coverage. |
| Ordinary current-column eligibility means no landmark owners | The actual depth3 census contains VineWall/locked chest/pressure plate/key/Choir and resource/hazard owners. Preserve these literal meanings; no generic limestone substitution or hidden-content disclosure. Presence of a Ziggurat-like family does not itself revoke ordinary-column authority or prove roomed geometry. |
| Rooms are included for free with a wall/floor kit | [RuinsBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/RuinsBuilder.cs:99) adds actual Pillar/BrokenColumn at roughly5% of room-floor positions. They are distinct owners, not limestone-wall variants. Existing opening BrokenColumn admission is not widened by this plan. |
| Existing limestone macro art is a native16×24 wall/mask set | [EnvironmentSpriteRenderer.cs:527](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:527),[565](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:565),[797](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:797) describe a64×64 ground field split into16 position-indexed16×16 slices. They are floor-field references, not connectivity masks or wall art. Centering them as a ground leaves vertical gaps; stretching violates square native pixels. |
| Black gaps must be fixed by adding floor entities | The [current backdrop seam:122](../Assets/Scripts/Presentation/Rendering/FlatPixelShallowCavesCatalog.cs:122) is an owner-free cached background tint separate from `LocalGround`. A limestone canvas must use its own approved base color and the same disclosure/state refusal contract; no fabricated floor, reader or collision owner. |
| Depth3 is only a palette change | [OverworldZoneManager.cs:580](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:580) also adds real landmarks,hazards,population and containers. [PopulationTable.cs:1102](../Assets/Scripts/Data/Tables/PopulationTable.cs:1102) uses depth/3+1:depths3–5 are tier2. Source actor/loot possibilities are not observations of fixture87. |

## Proposed owner and presentation boundary

The two limestone identities require their exact factory render/part meaning,
current physical placement, sole owned required parts and current disclosed
selection. Preserve custom VisualID/variant, status-color, foreign/custodied,
multicell, dead/destroyed and malformed owner fallback instead of applying
neutral pixels. A damaged wall stays its surviving structural state; normal
[DestructionSystem.cs:171](../Assets/Scripts/Gameplay/World/DestructionSystem.cs:171)
removes it and places real Rubble, then marks that cell dirty. The catalog must
never create wreckage or change damage, hardness, passability or loot.

Candidate shared families are the unchanged centered16PPU16×16 rubble/stalagmite
sources and native down/up stair cells already validated in shallow caves. They
still require exact current owner/state/reader checks and fresh actual contrast
review on the pale limestone ground. Neutral floor art belongs to one real
`LimestoneFloor`, including beneath a selected actor/prop; the selected foreground
and equal-layer insertion order remain authoritative. Only a selected actual
floor's redundant glyph may disappear after successful ground submission.

Prefer the established small companion/Greybox ground,selected-foreground,
subject and disposal seams. Resolve implementation shape only after source
preflight and genuine native RED; no new all-region admission bypass or broad
rendering framework. Keep ambient backing separate from floor identity. It may
cover only eligible disclosed neutral cells, with legitimate visible owner
checks; hidden/foreign/custodied content must not toggle it. Real floor resource
failure, authored backgrounds, mixed/wet/heat/charge/crop/source/trigger meanings
retain fallback. Preserve ordinary overlay priority rather than blanket mark
suppression. Exact current-body frame and reader must agree.

## Art pipeline and composition gate

1. Finish current shallow delivery and inspect real limestone entry,passthrough,
   wall edge,stair/counterpart and relevant owner readers. Capture actual Game
   frames and keep current world/camera/visibility state recorded.
2. Use those captures with approved cast/material art as imagegen references;
   save prompts,inputs,concepts and critique. Preserve the real geometry and
   paths. Reject high-relief3D walls, invented rooms/props, gradients, enlarged
   actors, altered fog and resampled UI. No concept is production acceptance.
3. Extract a finite part/state sheet:quiet pale dusty limestone floor,clear
   solid limestone wall,shared debris/obstacle/stairs and an owner-free base
   color only where semantically eligible. Choose frame counts from the actual
   comparisons; do not precommit a large mask/animation kit.
4. Author original editable native16×24 cells through the existing integer-cell
   source pipeline if existing art cannot fill the rectangular field safely.
   Preserve every old macro field,source,meta and working animation. Verify
   native dimensions,binary alpha,palette,geometry/pixel agreement and protected
   inputs; Point import with no resampling. Concept references precede implementation as required by the pipeline. New
   runtime production code must wait for genuine native RED; root controls
   implementation start. Native art uses its independent source validation gate.
5. Integrate through real owners after RED, then compare ordinary normal-zoom
   gameplay. Evaluate wall/floor/collision separation and continuous rubble-only
   backing, landmarks,sightlines,route contrast and expected quiet areas across
   actual visible extents. Unexplored or remembered darkness is not bare space.
   Do not add generic decoration or density without a named actual composition
   defect and a separate generation change/test plan.

## Bounded RED, GREEN and counterchecks

Proposed tests are **not authored or executed by this plan**. Use existing real
factory/manager/public renderer fixtures; avoid another harness.

- Exact current depth3/4 positives paired with2/5,noncanonical,detached/stale
  graph,current POI,legacy saved lair,registered special-column refusals. Preserve
  current map validation without initializing generation or lair state.
- Real limestone floor below body;exact surviving wall35 and damaged1;wrong
  hardness/maxHP/material/thermal/custom identity refusal. Actual genericWall
  andVineWall from the same ordinary column must not be relabeled limestone. Actual destruction
  to Rubble retains unrelated tile state and repaint/reader truth.
- Shared stair/debris/obstacle positives paired with hidden,remembered,covered,
  foreign,custodied,multicell and malformed-part counters. Same-layer ties and
  actual owner selection are tested rather than forcibly chosen.
- Neutral owner-free ambient versus real floor identity; missing floor artwork
  cannot be concealed by the ambient fallback. Hidden/foreign blockers leave
  eligible background unchanged; disclosed hazards/background/cultivation veto.
- Independent missing/wrong-size resource groups and borrowed-texture lifetime;
  submitted full/dirty painting,actual ground color/overlay marks and same-owner
  portrait parity. No extra Render event or whole-zone/camera reset.
- Actual normal stairs depth2↔3 and later3↔4, wall damage/wreckage if safely
  reachable, save and fresh Continue. Tests do not stand in for these actions.

## Performance, review and release gates

Follow [PERF-FOUNDATION](PERF-FOUNDATION.md). Cache Sprite/Tile views once per
optional resource group, dispose only owned views, and keep explicitnull inputs
missing. Precompute finite canonical address facts; recheck mutable current
manager/map/lair/owner authority without per-cell Split,LINQ,new collections,
whole-zone census,event dispatch or generation. Continue existing per-cell dirty
hooks and submitted paint paths. Existing native tint semantics bypass cell
lighting; actual Cave grading and normalzoom contrast therefore require capture,
not assumptions from ungraded contacts.

Profile a meaningful actual Editor route after integration, retaining max values
and instrumentation/idle/combat limits; do not label it package/Deck FPS or an
optimization without a matched baseline. Counterchecks and retained raw native
RED/GREEN are followed by independent runtime/art/actual-view review. Fix
significant findings before source commit. Update this phase and shared living
records in the same commit, push exact owned changes through root, then verify
fresh Mac gameplay and Linux archive integrity separately. No Linux or physical
Deck execution is implied by an archive. Preserve normal saves,preferences,
previous builds and unrelated edits; no storage deletion is authorized.

## Implementation log and in-phase self-review

- 2026-10-27: saved plan after actual packaged depth3 entry/return, then
  normal Editor depth3/4 baselines and read-only owner censuses, with source
  verification. Only this new Markdown file was written for the phase. No
  limestone runtime/test/art draft, new native test result or acceptance.
- 🧪 Pending: wider depth3 composition, reviewed depth4 passage/chamber and
  natural/roomed evidence,
  closer owner/state readers and visible landmark review,concept/parts/native art,RED→GREEN,integration,
  profile,actual comparison/persistence and delivery.
- ⚪ Deliberate scope: limestone3–4 first. Shale5–6 and all deeper strata,room
  props,actors,hazards,containers,specialsites and full-area polish remain open.
- ⚪ CoO-original graphics, no Qud-parity claim. Source-only candidate coverage
  and material counts never establish a polished biome or area.

Files changed for this preimplementation checkpoint:
`Docs/GRAPHICS-LIMESTONE-CAVES.md` only. Root owns further actual capture and all
Unity/Git actions. Current last actual hourly remains17:27:22UTC,next18:27:22UTC.

### Additional actual baseline checkpoint — 18:14 UTC

Root normally traversed depth4 to44,16,HP33 and F5 saved fixture88 atturn12090.
Capture07 retains that checkpoint;08 is the real limestone wall reader at44,15:
Stone,solid,structure35/35. No wall was attacked. The full isolated root was
archived, then Finish verified Playfalse,rootnull,scene clean. The exact control
receipt is `Verification/Live16x24/deeper-strata-before-editor-play.json`. These
views extend the baseline only; no native limestone coverage is accepted.
