# G2 — Orven's working court

Status: bounded implementation and two-seed ordinary Play accepted; full graphics release remains open. Continuation from
`GRAPHICS-G2-WORKING-LANDSCAPE.md`, 2026-10-24 19:14 UTC heartbeat.

## Purpose and limits

The two-seed actual kitchen captures show an exposed collection of objects, even
after repeated hedge rooms were removed. Make this existing service read as one
inhabited working place: a real stone shelter edge, a clearly open approach and
purposeful working ground. Preserve the approved Cook, oven, cot, chair, pan,
trays and crop art; preserve the accepted landscape masks. Do not add furniture,
food, containers, actors or new service mechanics merely to fill space.

The gameplay reference is the user's Qud-like legibility and original Caves of
Ooo world. No copied assets or Qud engine-parity claim. Graphics remains priority
one and a release blocker. Completion of this court is not full opening release.

## Baseline and production loop

Use the real seed64/65 ordinary kitchen route and native1x/2x captures from the
preceding pass as the verified baseline, particularly
`Verification/Live16x24/working-landscape-65-kitchen-2x.png`. Its real service
positions, movement, pan access, harvest, save/load and return have been exercised.

1. Audit resident admission, staged-owner proofs/rollback, collision/approaches,
   terrain authority, crops and state-aware rendering. Save corrections before
   production changes. An independent visual designer proposes exact cell layout.
2. Reinterpret the actual capture with built-in imagegen, maintaining original
   objects/HUD and native shape constraints. Extract only owner-backed edges and
   ground. A generated picture is a design reference, never acceptance.
3. Write and run failing native tests for the intended layout and access, plus
   negative controls. Implement a small exact-address cold-generation change.
   Root alone uses Unity. Keep asset production separate until layout is viable.
4. Exercise/capture the playable geometry. Where an existing owner needs a new
   ground treatment, author editable native16x24 cells and import with correct
   metadata. Do not repaint approved identities or use a whole-scene raster.
5. Review real native1x/2x views, service interactions and changed state; fix
   material findings, run affected native tests and source counter-checks.
6. Update living records and release coverage, commit exact files, fetch/rebase,
   push and verify server main. Only then inspect worthwhile stale cache/build
   candidates. Preserve all source, saves and current delivered archive.

## Proposed implementation boundaries, pending audit

- Exact connected WaysideKitchen at `Overworld.12.11.0`, current non-Legacy
  Hedgerow formation. No general resident/stamp or legacy rewrite.
- Keep service offsets/bindings, private stock RNG, passive resident, finite
  goods, ordinary oven and unowned cot. Added masonry is real blocking/destructible
  StoneWall, never a visual-only wall. Leave a full standing row behind trays.
- Prefer a modest real StoneFloor work apron if source review establishes safe
  owner semantics and rollback. Never label a court as a road to reuse artwork;
  never allow a cosmetic floor to conceal harvestable or planted ground. A floor
  overlay over Grass would need explicit non-plantable authority while present,
  state/look agreement and exact rollback. If these cannot be justified, prune
  that approach explicitly instead of weakening generation receipts.
- Keep all border ports and reserved routes reachable. Preserve original owners,
  existing saved graphs and current service access; no clear-and-replace workaround.
  Cold placement may legitimately select a different valid site. No retrofit on
  load or cached return. No new per-frame world scans or gameplay listeners.

## Planned tests and honest acceptance

RED tests: real court wall form and ground when selected; actual service approach
and bed access; unchanged stock and crop; exact outside/legacy/plot countercases;
source/authority refusal rollback; save/cached repair, depletion and wall removal.
Rendering tests, if needed: real floor owner beneath actors, hazards outrank floor,
no hidden-neighbor/state disclosure, outside/forged/removed owner rejection,
full/incremental agreement and disposal of owned resources.

Native tests prove sampled invariants, not aesthetics. Actual ordinary movement,
look/action menus, a harvested bed, save/load and return prove only the recorded
route/fields. Compare at1280x800 native1x/2x and profile a bounded editor window.
No physical Deck, new package or complete six-site claim without those checks.

Readiness: existing service art green; existing stateful services green within
prior bounded proof; court cohesion yellow; new geometry/ground red pending tests;
full release and packaged/device gates open. No deadline-based acceptance cuts.

## Settled source corrections and extracted parts

The preceding proposed non-plantable overlay is rejected: `Zone.AddEntity`
clears Vegetation beneath Barren terrain, including original Grass, and load
repeats that clearing. It cannot satisfy this additive placement transaction.
Use real **non-Barren StoneFloor**, displayed as **worn stepping stones** with
plantable grassy joints. Preserve the exact original Grass owners; no replacement,
road identity, fabricated soil or general planting change. The crop bed stays
outside the stone pattern. This supersedes the provisional paragraph above.

Frozen local coordinates, relative to Cook(0,0), transformed together by the
existing permitted stamp rotation:

- Walls: x=-3..2,y=-4 plus(-3,-3),(-3,-2),(-3,-1), nine in total.
- Stones: x=-1..2,y=-3;(-1,-2);x=-1..1,y=-1;(1,0);
  x=0..3,y=1;(3,-1),(3,0), fifteen in total.
- Preserve Cook, oven(-2,0), cot(2,0), chair(2,-1),
  pan/escrow/pickup(0/1/2,-2), crop(-2,2). Replace the two old
  cold wall specs only in this exact admitted stamp.
- All owner positions remain unique and must fit bare dry source cells.
  Connectivity must count actual solid owners, not passable stones/cot/trays.
  Check the whole rear aisle, ten named court access cells, every service,
  occupiable cot and existing critical routes. Reject harmful factory content
  before publication, especially Barren floors which would erase source Grass.

Source and visual audits are saved alongside verification. Imagegen v1 uses the
actual seed65 kitchen2x capture, with its exact prompt and output in
`ArtSource/Live16x24/concepts/working-court-v1-*`/`working-court-v1.png`.
Selected: coherent incomplete back edge and quiet stone circulation around the
warm services. Rejected: soft shading, altered HUD text and any failure to keep a
full rear standing row. The concept is not a game screenshot or runtime asset.

Part mapping: nine existing StoneWall owners use accepted masonry art; fifteen
StoneFloor owners use three new coordinate-varied native16x24 sparse-stone
frames; original Grass remains real substrate; all eight functional owners use
unchanged approved sprites. Author those three frames as editable Blender pixel
quads and source cell maps, never crop pixels from the generated concept.

Rendering is exact current kitchen graph/owner scoped. A new optional atlas may
supply background below actors/items, but real crop/soil/road/liquid/hazard state
wins. Removed/foreign/carried/mis-marked owners, unseen cells and malformed assets
must refuse. Foreground selection remains authoritative, and examine uses the
same owner-backed art. Default construction may load once; injected catalogs
remain deterministic. Tiles/sprites are cached, borrowed texture is never mutated
or destroyed, no frame listener or world scan is introduced. Existing dirty hooks
carry ordinary add/remove changes; profile the real route after integration.

## Implementation and review log

- Native RED39 cases:4passed,35failed for missing court/material and associated
  publication guards. First renderer-only run22/22passed. Expanded49-case run
  passed46 with3 expected review failures (closed-door, sealed-barrier and
  tile-source floor payloads). These are saved XML results, not MCP job promises.
- Real cold-generation stamp is additive, exact-address/non-Legacy/Hedgerow only;
  old branches and saved graphs remain unchanged. Floor descriptions identify
  plantable grass joints. All solids and passable destinations are separately
  accounted for by the bounded geometry proof.
- Native material is a48x24 three-frame atlas;61–66% of each cell is green joints.
  Source/PNG/contact comparisons have zero mismatched pixels;18existing art PNGs
  remain identical. Geometry source, cell map, generator, palette, metadata and
  preservation hashes are saved under `ArtSource/Live16x24/working-court/`.
- Independent rendering review found no significant issue; its two missing
  resource-lifetime/size counters are added. Additional tagged-solid, Player-tag
  and zero-stack negative tests first failed then passed after narrow guards.
- Independent generation review found aP2: Physics-only passability did not
  reject a closed Door/SealedLibraryBarrier or TileStateSource part on a floor.
  Native counters reproduced all three. Walkable court payloads now reject
  those before any AddEntity side effect; late Barren mutation rollback passes.
  Child source re-review confirms the correction; final native run below is green.
- Bridge preflight retained an orphaned test job while native TestRunnerApi was
  idle. Root checked actual runner state and cleared only that stale bridge job.
  No tests were treated as passed on request acceptance; saved XML is authority.

### In-phase self-review

- 🟡 Fixed before acceptance: Barren-overlay premise; publication passability
  mismatch; tagged-solid/Player/empty-stack rendering counters.
- 🔵 Existing sprites and approved landscape masks frozen. New atlas texture is
  borrowed, cached tiles/sprites disposed, no new listener or per-frame scan.
- 🧪 Ordinary route, visual coherence, actual rendered layers/profile and
  save/revisit captures remain pending below. No packaged/device claim.

### Native verification gate

`working-court-final-green.xml`: **272/272 passed,0skipped**, real Unity EditMode,
2026-10-24 19:39:11–19:40:48UTC. Eleven fixtures comprise49new court cases,
26existing east/kitchen presentation cases and197existing generation/service
regressions. Source/native results do not establish visual or packaged acceptance.
Root waits for native TestRunnerApi cleanup after XML before Play; an early
launch attempt is guarded against an active runner and changes no game state.


## Actual ordinary-input acceptance — 2026-10-24 19:42–20:05 UTC

Root ran two fresh ordinary games through GameBootstrap in owned native save
isolation, without forcing presentation, changing health/AI, moving entities,
adding inventory or invoking gameplay actions through code. CUA supplied the
keys. Capture scripts only observed owners/fields and requested real Unity
ScreenCapture images. Both launch receipts show the new presentation preference
absent and no forced-presentation flag; the real game still selected flat mode.
Temporary camera zoom1.2→0.416666657 supplied native1x/2x comparison and was
restored. These are real1280x800 game views, not crops or concepts.

- **Seed65:** walked spawn→east→kitchen. Cook51,6; open right side54,5,
  rear aisle51,3 and pan menu all reached normally. An unoccupied floor at54,6
  has matching name, portrait and grass-joint prose (`65-stones-reader`). The
  earlier `65-stone-menu`/`65-stone-examine` attempt selected a Player-covered
  cell: menu heading described stones while the reader selected You. That pair
  is retained as an existing subject-selection follow-up, not a passed stone
  reader. No new UI behavior was added in this pass.
- Farmer7139 occupied Bed7197 at53,6 and blocked a move; live readback showed
  Farmer Physics.Solid=true and Bed=false. Do not claim sleep/cot occupancy on
  this seed. The real older Farmer glyph remains visible; native actor coverage
  is the next graphics priority, not an excuse to hide its true occupancy.
- Reached Claspbean49,8 from50,8, chose Gather into pack: actual crop removed,
  bed retained,2ClaspbeanPulp+1ClaspbeanSeed carried. F5/F6 preserved all191
  recorded owner rows byID, all8 inventory rows, tick/drams/HP/player/flat mode.
  The first order-sensitive array comparison was false only because order
  changed; the independent keyed review records exact equality. Actual border
  exit to12.10 and return preserved all31 remaining resident packet owners,
  including9walls/15stones. Roaming/visibility changes elsewhere are recorded.
- **Seed64:** ordinary spawn/east/kitchen route retained real damage (23HP)
  from an east encounter. Walking south at30,13 struck the genuine upper wall;
  left/open approach reached26,17 beside Cook27,17. Walked onto unoccupied
  Bed27,19, then around the lower opening to rear aisle30,17 and pan29,17.
  The crop at25,15 was reached from25,16 and gathered normally. F5/F6 preserved
  every recorded owner byID and inventory row. This pass does not claim a
  second-seed cached return, sleep, pan repair, full cooking or forging.

Independent actual-image review accepts the bounded improvement in both
orientations. The earlier tiny angular mark was absent in later captures;
all15 actual submitted floor cells had the new ground atlas and no redundant
world floor glyph. Its exact transient cause is unproven. No speculative art
change was made. Existing18PNG art hashes remain unchanged.

### What this can and cannot verify

**Script-observable:**272native cases, source/cell/atlas equality, actual owner
positions and recorded fields, real inventory yield, keyed save/load equality,
seed65 return, live submitted material tiles, restored isolation/view/scene.
**Actual visual checks:** quiet sparse stone material, coherent masonry edge,
open approach, both orientations, truthful reader and crop disappearance.
**Not verified:** arbitrary seeds, all actor/ground state combinations, physical
Deck controls/feel/performance, new packaged launch, entire six-site release,
full paid meal/tray/forge/Claspbean lifecycle or comprehensive motion/defence.

The unwrapped68.586second editor profile covers idle/camera/look/menu/tool time,
**not walking**: renderer mean0.05267ms/p990.10575/max45.6266; main thread
mean1.25021ms/p992.18292/max1003.9395; max41draws/batches. It is an observation,
not an optimization benchmark or Deck proof. Two known Metal memoryless-depth
warnings and MCP's Autosaved misclassification were observed; no new runtime
error was established. Stale MCP test-job cleanup was only performed after
native runner inactivity. Final Play/isolation/view/save-root flags are clean.
Both checkpoint save trees remain in `/tmp/coo-working-court-64-checkpoint` and
`/tmp/coo-working-court-65-checkpoint`; user saves and backups remain preserved.

### Final self-review and continuation

- 🟡 Fixed before commit: Barren source-erasure premise; malformed passability
  payloads; invalid tagged/empty floor ownership; source-art size/disposal gaps.
- 🔵 Approved art, service stock/bindings, old branches, saved graphs and gameplay
  authorities preserved. Full native suite was not rerun:272affected cases pass.
- 🧪 Existing multi-owner look heading/reader mismatch and old Farmer glyph are
  documented from actual play. Neither is passed off as a new court feature.
- ⚪ No new building assets, all-biome claims, package or release completion.

**Next:** close the actual incidental-actor native coverage gap (start with the
Farmer seen at this court; audit exact art routing before authoring). Reuse these
playable captures and approved palettes. Continue G2 defence/motion and complete
remaining service/crop states, six-zone journey, package/device gates and then
regional kits. Keep the heartbeat active; graphics remains the #1 release blocker.

### Changed file groups

Generation: `SpreadExplorationResidents.cs`; new21case generation fixture/meta.
Rendering: `FlatPixelGreyboxCatalog.cs`; new `FlatPixelWorkingCourtCatalog.cs`
and28case rendering fixture/metas. Art: three-cell resource/meta, editable
`ArtSource/Live16x24/working-court/`, generator and saved concept/prompt.
Living records: this plan, LIVE brief, completion plan and release ledger.
Evidence: exact `Docs/Verification/Live16x24/working-court-*` reports/captures/XML.
