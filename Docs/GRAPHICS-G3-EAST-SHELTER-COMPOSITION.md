# G3 — Eastern tempering shelter composition

Status: **current seed64 before-captures and concept inspected; twelve-cell
footprint hypothesis selected; runtime/tests, current seed65 before-view and
candidate acceptance pending**, 2026-10-26. This is the next bounded scene pass
after the glade vegetation review. It creates no new art packet or gameplay
system. Root owns Unity and ordinary Play. This document update inspects root's
actual evidence and current source; it performs no runtime edit or new test.

## Named problem and evidence

The east connector is the remaining weak working-place link between the glade
and Orven's kitchen. The north now has a shared workpad, Nella has a cultivated
field/frontage, and the kitchen has a bounded court. Historical actual east
captures still show a small forge and broken masonry group against broad
uninterrupted green ground. Flower bodies carry more compositional weight than
the useful shelter. Quiet wilderness is desirable; filling the whole connector
with decoration would not solve recognition of this specific place.

- [Fresh-origin forge approach](Verification/Live16x24/fresh-forge-station-approach.png)
  and its JSON record seed64, `Overworld.12.10.0`, player `(6,19)`, tick5160,
  HP39 and42drams. The actual forge is beside the player and its broken masonry
  edge is visible. Combat and fog limit the surrounding view.
- [Six-site east return](Verification/Live16x24/sixsite-east-return.png) shows
  the same small shelter relationship during ordinary travel. Different player
  position, time and fog make it qualitative evidence, not a matched comparison.
- [Working-landscape review](Verification/Live16x24/working-landscape-actual-review.md)
  accepted reduced flower repetition across seeds64/65, but explicitly left the
  forge as a small object group in a large open area. Preserve that accepted
  flower improvement rather than repeating the mask exercise.
- [Fresh-origin acceptance](GRAPHICS-G3-FRESH-OPENING-ACCEPTANCE.md) records a
  real frozen quench of the existing sword, subsequent kitchen service and
  persistence. It proves the route is usable, not that this shelter composition
  is finished. Fresh Mac/Linux packages at `c893ae58f` likewise do not accept a
  future shelter change; the Mac smoke did not revisit this scene.

These historical captures nominated the defect. The current seed64 observations
below now support a bounded hypothesis. Current seed65 comparison remains open.
Do not infer absent owners from fog or treat a detached census as visible state.

## Current before-captures and concept, 2026-10-26 05:00–05:02 UTC

Root crossed from the actual glade east edge through ordinary movement. The
[current entry](Verification/Live16x24/east-shelter-before64-entry.png) and its
JSON record seed64, east `(0,16)`, tick1050, HP40 and50drams. The forge remains
at `(7,19)`, with finite Stoneburr `(9,20)` and broken walls `(5,17)`, `(6,17)`,
`(9,17)`. These useful objects still lack a shared ground relationship; the
large grass field and flower pen dominate the view. The source roster has one
ordinary Scrabbler4998, initially `(6,24)`, not an authored Cindercaller pair.

The [contested approach](Verification/Live16x24/east-shelter-before64-contested-approach.png)
and JSON record player `(5,19)`, tick1170, HP12 and50drams. Scrabbler4998 has
moved to `(4,19)` and retains15HP; the forge5013, Stoneburr5014 and wall5015–5017
positions remain unchanged. The screenshot records real stun/attack/bleed
consequences. **No forge interaction, successful temper, peaceful approach or
enemy defeat is accepted from this run.** At two cells west of the forge, the
player has not yet demonstrated its adjacent use point. Carrying two healing
tonics and two dried meat in the receipt is not evidence they were consumed.

The preserved pre-east checkpoint is
`/tmp/coo-glade-final64-before-east/8d2b4d6184484425819ffe087ed83dae`.
`glade-vegetation-final64-before-east.json` records glade `(79,16)`, tick1040,
HP40/50drams and only the glade cached. The matching `-checkpoint.json` records
four copied save files; `Quick.sav.gz` SHA256 is
`8ef117dd004c00196da5028a74a6f76a8d8881551778a28baa449a0f3fbe362a`.
It supports a genuine pre-generation replay; do not retrofit ground into the
already visited or currently contested east graph.

Root also preserved the earned checkpoint durably at
`Docs/Verification/Live16x24/Fixtures/east-shelter-pre-generation64/`: only its
`Quick.json`, `Quick.sav.gz`, hash manifest and isolation README. Use that
repository fixture for future continuation if the temporary path is gone;
copy its two save files into an empty audit-isolated directory with game ID
`8d2b4d6184484425819ffe087ed83dae` and follow the established isolation workflow.
It is an earned audit save, not a normal user save or permission to overwrite
one. The original temporary checkpoint remains intact.

`glade-vegetation-final64-restored.json` records normal SampleScene restored at
05:04:43UTC, out of Play with isolation/view borrowing off and null save root.
`glade-vegetation-final-preservation.json` records all four protected normal
save hashes and two foreign PNG hashes unchanged, about430GiB available and
an empty deletion list. This operational closeout is separate from shelter
visual or service-use acceptance.

Built-in imagegen used the contested capture. The exact saved prompt is
`ArtSource/Live16x24/concepts/east-shelter/shelter-prompt.txt`; output is
`shelter-v1.png` in the same directory. It usefully groups the forge and broken
edge with a small worn surface while leaving the plant on green ground. Reject
its dense mottled stone, softened pixels and screenshot reconstruction. It is
composition reference only, not a pixel-preserving edit, production raster or
new gameplay capture. No additional props, heat, stock or owner movement is
admitted. The selected extraction is existing RoadStone ownership on a sparse
native-cell footprint, using the current quiet Road material first.

## Verification sweep and corrections

Line numbers describe the inspected source and may move during implementation.

| Premise | Verified source / correction | Design consequence |
| --- | --- | --- |
| The exact east address always contains a forge | `SpreadExplorationPlan.cs:132–147` assigns the family for eligible version9+ worlds; `TryGetPlacement` and `CoveredNow`/`PlacementNow` at171–223 retain current manager, Spread/no-POI and protected-address gates. `SpreadExplorationWorksites.cs:28–35` additionally requires live terrain/population receipts and admissible source actors. | Preserve optional refusal and current graph authority. Never create the shelter merely because a renderer sees12,10. |
| Its historical coordinates are authored constants | `SpreadExplorationWorksites.cs:49,57,63,70–93`: forge `(0,0)`, finite Stoneburr `(2,1)`, three broken walls `(-2,-2),(-1,-2),(2,-2)` are relative specs; source anchor, fit search and two orientations choose the actual origin. | Compose in the accepted local frame after the existing functional fit. Do not hardcode seed64's7,19 or seed65's65,4. |
| Every shelter has the two authored defenders | `SpreadExplorationWorksites.cs:41–46,63,69` replaces the exact two-source budget with Cindercaller/Scrabbler only when the source count is two. A single source is retained. The15,10 emergency-water case is separately scoped. | Preserve one/two-source outcomes, identities/loadouts and role policies. No added enemies, medicine or emergency water for a picture. |
| North's ground can be enabled for forge by changing one condition | `SpreadExplorationWorksites.cs:94–113` appends optional ground after fit, but the strict `yard-ground` payload checks currently live inside `DryingOwner`/`DryingShape` at128,180,307–330 and run for enriched alembics. | If reusing this seam, make ground validation explicit for every admitted ground owner at creation, after staging and final acceptance. Merely broadening the append condition would bypass the north-only semantic guard. |
| An empty cell is enough for a work surface | `YardSubstrate` at361–367 requires exactly one literal Grass substrate, actual bare/unclaimed ground, no cultivated soil, interior, reservation, nonempty tile state or barren terrain. The original substrate is retained and receipt-pinned. | Reuse these restrictions. Do not cover crops, claimed beds, wet/hazard cells, pen structures, flowers, dropped objects or actors. |
| Added visual ground is mechanically inert | `Fits` at373–403 excludes the ground role from obstacle/service checks. Extra RoadStone still occupies a real cell and may change later crop placement or planting eligibility. | Preserve functional positions/routes; disclose any downstream placement difference. Do not describe paving as entirely cosmetic or promise identical later IDs. |
| Ground needs a new atlas | `FlatPixelGreyboxCatalog.GroundOf` at627–637 recognizes actual visible RoadStone below other owners. `FlatPixelOpeningGroundCatalog.IsSupportedZone` at55–57 supports glade/Nella, not east. | First evaluate the existing quiet Road ground. Do not silently broaden glade bank/soil art or create a new loader branch. Any genuinely necessary material treatment needs its own native owner/state extraction and tests. |
| A working forge should emit flame/smoke | `FlatPixelEastKitchenCatalog.cs:107–110` selects the neutral exact forge from its real `ForgePart`; its contract promises no heat or stock. `ForgePart.cs:35–78,143–190` supplies the carried-component/weapon/quench picker. | Keep the approved neutral forge and reader. No perpetual fire, invented tools, supplied components or animation that implies a simulated job. |

References above are under `Assets/Scripts/Gameplay/World/Generation/`, its
`Builders/` subfolder, `Assets/Scripts/Presentation/Rendering/`, and
`Assets/Scripts/Gameplay/Weaponcraft/ForgePart.cs`. The source authority is the
generation transaction, not `SpreadWorksite.Role`: that property is explicitly
presentation/debug identity. `FlatPixelGreyboxCatalog.IsAdmitted` at174–192
already admits the exact managed east graph and rejects claimed POIs; no wider
native-art admission is needed for this pass.

## Production prompts

### Brainstorm against the current capture

> Inspect the ordinary east entry and near-forge captures at native1280×800,
> together with their exact owner/visibility receipts. Identify the three largest
> obstacles to recognizing the tempering shelter as a place. Compare a quiet
> work-worn footprint, a better relationship to the existing broken edge, and
> leaving the scene unchanged. Choose the smallest change that makes the
> existing service legible without filling the wilderness. Preserve approved
> sprites, functional positions, finite supplies, threats, pen/crops and genuine
> fog. Reject proposals requiring fictional equipment, roads to undisclosed
> destinations or a new content system. State what the screenshot cannot prove.

### Design with imagegen and extract the native contract

> Reinterpret the actual before-capture using built-in imagegen. Keep the flat
>16×24 world, camera/HUD and exact existing forge, forage, wall and approach
> locations. Clarify a small working area with low-contrast ground and an
> irregular edge; retain large quiet surrounding grass. Avoid a rectangular
> platform, repeated ellipse stamps, bright quest markers, smooth texture,
> extra props and fake heat. Save the prompt/input/output, then criticize it
> against the real cells. Extract only admissible native parts and their exact
> owner/state sources. Prefer arrangement of accepted assets. Record rejected
> concept details; generated pixels never substitute for gameplay evidence.

### Implement the accepted bounded design

> Use the captured current graph and source sweep in this document. Write and
> run behavior-specific RED tests before runtime changes. Preserve the existing
> functional fit, source receipts, owner/resource budgets, hostile policies and
> saved graphs. Implement only the exact fresh east shelter's approved local
> footprint, with optional-content fallback and transaction-safe substrate and
> payload validation. Reuse existing art/rendering paths. Run GREEN and paired
> outside/invalid-owner/callback/cached-return counter-checks, independently
> review, then replay ordinary current seeds64/65 through entry, close use and
> return. Compare real views and fix the largest remaining defect. Update this
> document with actual receipts and limits; push exact reviewed work with the
> living record. Do not call concepts, hashes or native tests visual acceptance.

## Bounded design decision — twelve-cell maximum hypothesis

Preferred hypothesis: a small irregular work-worn footprint joins the existing
forge to its broken working edge, with an open use apron. It should read as
local wear, distinct from the north's longer shelf aisle and the kitchen court.
Retain the forge and Stoneburr in their accepted positions. Keep the Stoneburr
in an unpaved margin and its two existing approaches accessible. Do not lengthen
walls or alter cover in the initial candidate. Reject the candidate if it adds
an obvious stamp or changes the scene less than a simpler alternative.

Select **at most twelve** RoadStone cells in the accepted forge's local frame.
This is a specific hypothesis for RED tests and native comparison, not an
accepted layout. It spans five columns and four rows, with an uneven west
shoulder, a smaller southern tail and an open east/forage margin. Do not copy
the north's20-cell shelf-aisle footprint or expand the bounds to fill rejected
cells. Each coordinate is merely eligible; the existing source/occupancy/ground
checks still decide whether its real substrate can receive a new owner.

| Local row relative to forge `(0,0)` | Candidate RoadStone local x coordinates | Seed64 world cells for comparison only |
| --- | --- | --- |
| y=-2 | 0,1 | `(7,17)`, `(8,17)` |
| y=-1 | -2,-1,0,1 | `(5,18)`, `(6,18)`, `(7,18)`, `(8,18)` |
| y=0 | -3,-2,-1 | `(4,19)`, `(5,19)`, `(6,19)` |
| y=1 | -2,-1,0 | `(5,20)`, `(6,20)`, `(7,20)` |

Native plan sketch, with x=-3..3 and y=-2..2; `r` is a candidate, `W` an existing
wall, `F` the forge, `S` Stoneburr and `.` unchanged ground:

```text
.WWrrW.
.rrrr..
rrrF...
.rrr.S.
.......
```

The exact forage cell `(2,1)` and its entire one-cell local margin
`x=1..3,y=0..2` receive **no** proposed ground. The three wall cells and forge
cell are also excluded. The forge's north/west/south use neighbors may be worn
ground while its east use neighbor remains green. No change to solid/sight
blocking or the two-approach contract is intended. Use the existing `At`
transform after the functional origin/orientation is accepted, never these
seed64 absolute coordinates as production placement.

On an unobstructed one-source fixture all twelve candidates should be possible.
In a two-source packet the melee position `(-1,1)` is already reserved by the
existing functional specs and must be skipped, leaving at most eleven; the
ranged `(2,-1)` position is outside the footprint. Preserve other exclusions
even if fewer than ten cells remain. A zero/partial pad must never relocate or
invalidate an otherwise valid original shelter merely to meet an art count.
If the resulting partial footprint reads badly in real Play, revise the bounded
design or omit its optional ground; do not bulldoze actual occupants/terrain.

The current player and enemy happen to occupy two candidate coordinates in the
contested screenshot. That is reference for eventual walking over the pad, not
authorization to place beneath live owners now. Cold generation uses the
pre-east checkpoint and existing bare-source checks before ordinary movement.

| Proposed part | Real owner/state | Protection |
| --- | --- | --- |
| Working ground hypothesis | Existing RoadStone over retained ordinary Grass; the twelve local offsets above | Optional fresh-only addition after functional fit; visible/current ownership and normal fog; no stocked contents or new service. Skip actual/spec occupants and unsuitable substrates. |
| Broken working edge | Existing three StoneWall owners | Existing coordinates, collision and sight blocking unchanged. |
| Forge | Existing TinkersForge / ForgePart | Same neutral native world/reader art, commands and stock-free behavior. |
| Binding margin | Existing finite StoneburrPatch / HarvestablePart | Same owner specification, yield and removal; never paved or duplicated. |
| Nearby landscape | Existing CharmFlowers, pen/gate/grazer, fields/crops, pipes/vents, containers and incidental actors | No relocation, replacement, new grant or changed material/hazard semantics in this pass. |

Previously loaded or saved yards remain literal. No renderer reconciliation,
retroactive migration or repeated visit should create ground, resources or
owners. Global flat projection and reversible older presentation stay intact.

## Execution and acceptance gates

1. **Current before-capture.** Seed64 entry and contested-near views are recorded
   above; current seed65 and actual adjacent use remain pending. Root uses normal SampleScene/GameBootstrap and
   ordinary movement within save isolation. Retain a genuine pre-east checkpoint
   if a cold-generation comparison is needed. Record source, seed, managed
   address/POI, entry/player coordinates, zoom, time, fog, HP and inventory. Read
   exact visible forge/forage/wall roles and positions; separate off-camera census
   from photographed owners. Capture entry and use-distance world views plus
   exact-subject Examine. No teleport, grants or FOV reveal for acceptance.
2. **Design/extraction.** Use those captures for the prompts above. Record one
   preferred design, its real-cell footprint, budget and rejected details. Reuse
   existing editable sprite sources; a layout-only change needs no new raster.
   If new art is indispensable, name the defect and complete exact16×24 editable
   source/import/world-reader coverage before admission.
3. **Native RED.** New tests must fail for the missing intended footprint, while
   pre-existing invariants remain meaningful. Preserve pre-change captures and
   native baseline signatures. Do not count zero-case/stale-assembly runs as
   behavioral RED. Add a handwritten Unity `.meta` for any new `Assets/` source.
4. **Minimal implementation/GREEN.** Append optional terrain only after the
   original fit succeeds. Reject malformed payloads and revoked authority;
   missing optional art/content or no suitable ground keeps the existing service
   usable without a partial composition. Preserve rollback ownership when a
   callback independently changes/transfers an owner. No new per-frame work.
5. **Independent review and ordinary replay.** Test the current admitted64/65
   outcomes, including both observed orientations/source-count variants where
   they actually occur. Fresh generation receives the candidate; old saved east
   graphs do not. Compare current before/after entry and close views, exercise
   real actions, leave toward kitchen/glade, return, F5/F6 and normal Continue.
   Record which autosave actually loads. Preserve original user save hashes and
   restore borrowed preferences/editor state.
6. **Accept/freeze.** At1× and2× with real HUD, the shelter should read as a
   purposeful small working place without selection outlines. Forge, forage,
   cover and two approaches remain distinct; there is no new geometric stamp,
   material ambiguity or false service cue. Share actual gameplay screenshots.
   Keep the result bounded to sampled scenes/states and move toward the full
   opening review rather than another isolated item packet.

### Required positive/counter pairs

- Exact fresh east TemperingShelter receives only the chosen footprint;
  wrong family/address, north/store sites and excluded/legacy/current-invalid
  contexts retain their prior behavior. Use current source authority rather
  than assuming every generated seed commits the optional shelter.
- A clean one-source substrate fixture yields the twelve declared cells and
  preserves the original source actor. Its two-source counterpart skips the
  melee cell, retains the exact existing replacement pair and role policies,
  and adds at most eleven ground owners. Test both current forge orientations;
  compare the transformed footprint to a literal test contract, not to the
  production offset array. All original forge/forage/wall specs and all five
  Stoneburr cell/cardinal positions remain unchanged and receive no added ground;
  additionally assert the full declared one-cell forage margin receives none.
- Same functional positions, wall/forage/actor budget and caller random stream
  with and without optional ground content. Missing RoadStone must not move the
  forge or turn a valid original site into a refusal. Paving may affect later
  planting; separately compare and report that exposure.
- Bare literal Grass can receive ground; cultivated/claimed/reserved/interior,
  wet/coated/barren, mixed-owner and foreign/stale-substrate cells cannot. Empty
  occupancy alone must not authorize it. Preserve original Grass identity.
- Harmless non-takeable/non-solid RoadStone can join the packet; fake Solid,
  crop/harvest, liquid/gas, trap, repair, container, stairs, shared parts or
  changed ownership cannot. Recheck after later callbacks, after placement and
  at final acceptance, including tile-state mutation and late authority loss.
- Real four-port connectivity and two functional approaches remain; neither a
  footprint nor its validation creates a safe route by deleting existing danger.
  One original source actor remains one; two-source replacement remains bounded.
- Current visible lower ground persists under player/item movement; hidden or
  remembered cells do not reveal live owners/states. Existing renderer behavior
  may suffice, but exercise it on this actual layer stack before acceptance.
- Save/load and cached return preserve exact sampled floor/service IDs and
  finite depletion; absent/destroyed/harvested owners never regenerate. Ordinary
  state changes retain their existing source semantics.

Reuse the relevant existing fixtures: `SpreadExplorationWorksitesTests`,
`SpreadWorksiteManifestTests`, `SpreadWorkingLandscapeTests`,
`FlatPixelEastKitchenTests`, and the north ground transaction tests. Inspect
their current names/results before selecting the sweep. Existing tests provide
controls, not automatic acceptance of a new footprint. Record distinct counts,
skips and exact scopes rather than adding overlapping totals.

### Ordinary supplied forge operation

Prefer the already demonstrated north-forage → brewed pure-frost coating → east
quench route. Carry a legitimately acquired eligible melee weapon and coating;
use the forge's normal sectioned picker and `>> Craft <<`. Unequip the weapon
through normal inventory if required for selection, then re-equip afterward.
Record the exact weapon/coating IDs, consumption, durability and temper payload,
plus reader agreement and save-return. Avoid the mixed Frozen/Stoneskin brew
when the intended example is a freezing weapon; it carries both effects.

If no valid supplies remain, retain the empty/insufficient-selection refusal
and obtain supplies through actual play or defer the operation. Never grant a
head/haft/binding kit for this picture. A successful quench does not demonstrate
full forging, re-forging, the on-hit effect, healing, or every product lifecycle.
Record combat interruptions honestly; do not change defenders for an easy shot.

## Review, scope and handoff

Qud reference: the user's desired readable exploration/material hierarchy,
not copied sprites or a claim of matching decompiled behavior. This is original
CoO scene composition. No external reference image has been selected and no
external image is needed to justify this bounded reuse. Built-in imagegen's
current shelter concept is now recorded above; its pixels are not imported.

- 🟢 Existing scene, neutral native forge, functional service and an earned
  supplied quench route exist. No new mechanics are required.
- 🟢 Current seed64 entry and contested near view support the named grouping
  defect; concept/extraction selects a twelve-cell maximum existing-ground
  hypothesis. The current one-source enemy budget remains protected.
- 🧪 Current seed65 before-view, native RED/GREEN, implementation, independent
  runtime/visual review and ordinary candidate acceptance remain pending.
- 🧪 Combat blocked service use in the current seed64 run. The supplied forge
  operation above remains a required future gate, not completed evidence.
- ⚪ Ground has real placement/planting consequences; preserve/disclose these
  rather than label it entirely cosmetic. Existing approved sprites stay frozen.
- ⚪ Linux execution, physical Deck controls/performance and a complete packaged
  six-site journey remain separate gates. No new build or performance claim.

Implementation log: planning/source and current capture/concept review only.
Files changed by this planning update: this document only. Root separately owns
the captures, concept and checkpoint. No new runtime or passing test is claimed.
Next action: preserve/push finished glade work, obtain the current seed65
before-view, then write/run RED for this bounded footprint and its paired
source/ground guards. Replay actual seed64 from the preserved pre-east checkpoint
after implementation; protect ordinary supplies and record combat honestly.
Update this record in the same commit as any eventual implementation, using
CLAUDE.md's review and commit requirements.
