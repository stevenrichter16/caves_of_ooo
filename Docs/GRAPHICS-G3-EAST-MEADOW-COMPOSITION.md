# East meadow: flower massing around the tempering shelter

Status: **bounded scene-composition acceptance in ordinary Unity Play on seeds
64 and 65; final native 244/244 passed**, 2026-10-26. Seed 64 also completed
save/load and a real border return. The current Mac/Linux packages remain at
source `8902d3731`, before this meadow change. This does not certify the complete
opening, regional artwork, Linux execution or physical Steam Deck experience.

## Named defect and intended change

The real `east-shelter-after65-upper-use-approach.png` and packaged
`packaged-glade-east-13-east-entry.jpg` show broad flower bodies filled by
regular rows of identical bright bells. Those bodies compete with the small
tempering shelter. The earlier ribbon-to-drift pass improved the larger outline;
the remaining defect is the interior fill and repeated density, not the approved
flower identity or the existence of quiet green space.

Use a **generation-only, exact-east thinning of the current flower bodies**:
retain substantial unequal drifts, introduce readable irregular interior
clearings, and break long filled runs with quieter feathered edges. Do not move
flowers into new grass merely to conserve an arbitrary decoration count. The
source audit below finds no finite harvest/resource allowance in CharmFlowers.
Subtraction is preferred over a new renderer, palette workaround or additional
asset packet. Accepted glade, hedge, workpad, service and character art remains
unchanged. New fresh matched views and concept critique precede implementation.

## Verification sweep and corrections

| Premise / source | Verified correction and consequence |
| --- | --- |
| All flowers are the same mechanic | `Objects.json` CharmFlowers at33522 inherits Terrain, then PhysicalObject. It has Render, Physics and Examinable, Terrain+Vegetation tags, no harvest, yield, crop, food, charm, lifespan, thermal, material, destructible or tile-source behavior. `FlowerField` is a separate conjured/charming timed owner. Never substitute it. |
| A flower-count pin preserves a resource budget | These ordinary flowers provide no harvested stock. Their count is a visual coverage choice; preserve meaningful drifts and disclose count changes rather than forcing displaced flowers into another carpet. Existing native tests require40–360 for seeds64/65, not a fixed content allowance. |
| Flowers are entirely behaviorless decoration | They remain actual selectable/examinable terrain with render layer0. Generic barren-ground rules remove/refuse vegetation. Preserve their blueprint/parts, actual visibility and removal semantics. No fake painted flowers. |
| A new exact-east generator is necessary | `SpreadCompositionPlan.OpeningFlowerDrifts` already gates non-Legacy, `Overworld.12.10.0`, Formation.FlowerMeadow. The actual landscape is flower avenues. `ObjectAt` first vetoes the two-cell border, approaches, lawns and water; tree/Bush islands take precedence inside `LandscapeObjectAt`. Keep those rules. |
| Existing density is a fixed 200-flower budget | `InOpeningFlowerDrift` uses two upper ellipses with a bite, an oblique lower ellipse and a small echo; `Roll(x,y,733)<70` selects actual flowers after exclusions. Counts depend on seed and topology. The native original-plan receipt recorded exact counts before production. |
| The same flower layout applies to all saves | Only cold empty-zone generation realizes this plan. `SpreadCompositionBuilder` refuses nonempty input; cached and serialized entity graphs are authoritative. Previously visited flowers must never be thinned on load, frame, turn or return. |
| The plan knows the forge coordinate | The forge anchor is selected later from actual population source positions and geometry at priority4300. Recomputing it in this plan would duplicate the pipeline. Do not add such prediction. |
| Non-solid flower changes cannot affect later placement | `PopulationBuilder` uses passability/reservations; the worksite's `Geometry.Place` permits these owners through `DoorPart.IsBareGround`. Core geometry therefore should remain unchanged. However `YardSubstrate` requires exactly one Grass owner: removing a flower can admit an extra RoadStone. Exact full-generation controls must catch that. |
| Keeping counts preserves all IDs | Terrain creation is row-major. Relocation changes interleaving; reducing counts changes later allocated IDs. No cross-new-world ID equality is promised. Within one played/saved graph, exact IDs/state must persist. |
| All generated object differences are permitted | Trees, Bush, ground, water, routes, reserved cells, structural owners, forge/forage/defender positions and stock remain fixed. Optional workpad footprint/count should stay fixed by retaining original substrate eligibility, subject to the bounded source design below. |

Independent read-only audit verified the blueprint chain, generic barren-ground
behavior and downstream population/placement seams. No native test or gameplay
claim is inferred from that audit.

## Baseline and exact scope

Root's read-only native probe is staged at
`/tmp/coo-east-meadow-plan-baseline.cs`. It constructs original plans only and
writes `Verification/Live16x24/east-meadow-original-plans.json`, refusing overwrite.
It covers seeds0,1,64,65,1729,int.MinValue,int.MaxValue,200893250 and all three
current non-Legacy topologies: OffsetLanes, BrokenEnclosures, BankCrossing.
Each row records actual blueprint counts, original flower coordinates,
nonflower/ground/approach/lawn/water hash, ports and original signature.
The native export completed before runtime changes. Original flower counts are
176–233 across the 24 plans. Seed 64 counts are 195 / 207 / 185, seed 65 counts
are 201 / 201 / 189, and seed 200893250 counts are 230 / 209 / 216, in the
topology order above. The focused fixture embeds literal original coordinate
bitsets and the nonflower hashes from this receipt. Original single-cell holes
already numbered 23–51; merely asserting that holes exist would not test the
named defect. Original longest filled horizontal runs ranged from 7 to 16.

Root's actual seed-64 before captures now precede the imagegen reinterpretation
in `ArtSource/Live16x24/concepts/east-meadow-composition/`. Its prompt, concept
and extraction record accept unequal clusters and interior cuts. They reject
the concept's excessive sparsity, soft pixels, fabricated visibility and altered
grass. The initial native target retains 55–70% of the original flowers, with
substantial upper, lower and eastern bodies. No new sprite asset is needed for
this composition-only pass.

Changed scope is only the existing exact-east, non-Legacy FlowerMeadow grammar.
Legacy east, other addresses, other formations and all admitted-art/rendering
rules stay unchanged. Actual manager admission still comes from its exploration
assignment and current world/POI authority; a pure plan is not permission to
modify an arbitrary managed graph. Test all three plan topologies while retaining
full ordinary generation controls for seeds64,65 and the packaged seed200893250.

The separate native full-generation export completed before runtime edits in
`Verification/Live16x24/east-meadow-original-generated.json`. It records every
nonflower blueprint, position, worksite role, HP, finite-stock scalar, carried
and equipped item, container contents, RoadStone coordinate and reserved cell;
entity IDs are deliberately excluded between separately generated worlds. The
initial raw observer mistakenly retained numeric equipment-cache keys; these
are also allocated IDs, corrected explicitly below.
The original dispositions were all 2 (accepted), with these observed totals:

| Seed | Original flowers | Nonflower owners | RoadStone owners | Reserved cells |
| --- | ---: | ---: | ---: | ---: |
| 64 | 185 | 2126 | 11 | 736 |
| 65 | 201 | 2077 | 9 | 564 |
| 200893250 | 216 | 2150 | 12 | 663 |

Three additional native cases pin the normalized original serialized tuple hashes, floor
coordinates, disposition and reservation hashes. They use the literal original
receipt, not fresh candidate values. A fourth counter removes a real East
flower, performs cached return and a real save round trip, verifies exact owner
IDs/positions and absence of the removed owner, then refuses cold rebuilding of
the nonempty graph. The final focused candidate fixture therefore has 22 cases;
the four additions were made after the first 18-case RED and are preservation
controls, not four newly observed RED failures.

## Minimal implementation design

1. Obtain original native plan receipts and whole-zone/near-forge captures.
   Save source baseline pins before production. Reinterpret the actual frame
   through imagegen; retain only useful mass/clearing relationships. Reject new
   props, paths, lighting, hidden information, crop identity or altered UI.
2. After existing connectivity repair, capture the original exact-east flower
   presence. Derive the thinned placement as a strict subset. All original
   nonflower outputs and shared route/parcel/hash code remain unchanged.
3. Use seed-stable spatial variation at more than one scale: a few uneven
   connected clearings and smaller edge gaps. Avoid a uniform low probability
   everywhere, regular punched holes, identical mini-islands or another solid
   stamp. Retain a populated upper and lower drift and the smaller echo where
   ordinary route exclusions permit them. Final cut strength follows actual
   image review, not a decorative tile-count target.
4. Preserve workpad admission exactly without predicting its anchor. A narrow
   generation-only query may expose which cells were original flower cells;
   the existing optional east RoadStone candidate loop can retain its original
   Grass-only eligibility at those cells. Root approved this compatibility
   constraint for the cold substrate, not a new persisted state or service
   permission. Core Fits,
   functional positions, source receipts and rollback remain unchanged.
5. Realize retained flowers as the same actual CharmFlowers entities. No new
   resource, renderer, atlas, frame, per-cell scene object or runtime observer.
   Count reduction and different newly allocated IDs are disclosed; existing
   functional contents/positions and same-game save state are preserved.

## Focused verification table

| Invariant | Native test / counter-check |
| --- | --- |
| Interior composition improves without erasure | Original sampled plans fail a bounded spatial hierarchy criterion: several multi-cell clearings inside the existing drift footprint and limited long fully filled runs. Pair with nonvacuous retained upper/lower bodies and substantial flower coverage. Criteria describe output shape, not the new hash formula. |
| Only intended flowers are removed | Literal pre-change native coordinates prove every retained flower was originally a flower; original nonflower/ground/approach/lawn/water hash and ports match. No new flower in an empty cell, no removed Tree/Bush. |
| Cold scope is exact | Pin legacy/outside signatures from the native baseline and existing working-landscape controls; test another formation at the same address. |
| Full generated service remains the same | Before/after seed64/65/200893250 compare nonflower functional blueprint/role/position records, stock, forge/forage/defender counts, actual RoadStone positions and reserved cells. IDs may differ between cold worlds. Do not relax disposition if a source was originally refused. |
| Actual semantics are retained | Created CharmFlowers remains persistent non-solid Terrain+Vegetation with no FlowerCharm/Lifespan/Harvestable/Crop parts; original vegetation behavior remains in existing suites. |
| Determinism and shared routes survive | Same seed repeats; caller RNG history does not alter builder output; no new caller-RNG consumption. All four border ports and existing service approaches stay reachable. |
| Existing graphs are not rewritten | Nonempty builder refuses; cached return and real save/load retain a removed flower and the exact remaining owner graph. Do not test regeneration as a substitute for persistence. |
| Optional-workpad counter | Originally flower-occupied candidate stays ineligible for new floor after thinning; original plain Grass still admits the same floor. Existing hazards, claims, cultivated beds and malformed owner counters continue passing. |

Keep the new fixture grouped and focused. Reuse existing working-landscape,
exploration geometry/pipeline, EastTemperingGround and save fixtures for their
established contracts; do not add a generic terrain safety framework.

The first native run of `EastMeadowCompositionTests` is **18 total, 8 passed,
10 expected failures, zero skips**. Receipt: `Verification/Live16x24/east-meadow-red-summary.json`
and its XML. The eight literal baseline controls passed. The eight shape cases
failed at unchanged 100% flower coverage; the original-substrate query was
absent, and the controlled worksite produced 12 floors instead of the required
10. Its two original flower cells and ten plain-Grass controls make the pad
assertion nonvacuous. Before production, the connected-clearing criterion was
tightened to require a removed group to reach a formerly interior flower with
at least three original cardinal flower neighbors. The same old density
assertion still failed first in original RED. The deeper criterion subsequently
passed final GREEN; it is not a separately observed RED claim.

The first candidate native regression run completed **243/244 passed**, with
one fixture error after the now-correct pad assertion. The test incorrectly
expected the one-use population source receipt to remain `IsCurrent` after
successful worksite placement. `SpreadGenerationReceipt.TryConsume` explicitly
consumes it even when the single source Viper is retained. Original RED stopped
at the earlier 12-versus-10 floor mismatch and therefore masked this later test
mistake. Production shape, original full-generation pins and the other focused
21 cases passed. The corrected counter now proves `IsCurrent` before placement,
false after consumption, and the identical original Viper still present at
(60,5), alongside the existing final-state proof. No production behavior was
changed for this fixture correction. The final native rerun below closes it.

A repeated native full-generation observer then exposed a second verification
defect: six equipment rows differed only in numeric cache-key prefixes, from
body-part IDs 1–10 to 487–496. `InventoryPart.EquipToBodyPart` keys its cache by
`BodyPart.ID`, whose allocator is process-global. Fresh-domain native tests
could therefore pass while the original hash incorrectly depended on prior
body allocation. Both raw receipts are preserved. The derived
`Verification/Live16x24/east-meadow-normalized-generated-comparison.json` changes
only numeric equipment prefixes to `body=`, retains values and duplicate
multiplicity, preserves nonnumeric legacy slot labels, and leaves every other
field untouched. Original and candidate normalized data, floors, reservations
and dispositions match for all three seeds. Observed flowers changed from
185→120, 201→130 and 216→139 respectively.

The test pins were updated from the normalized **original** receipt. Each full
generation case now repeats in the same domain so allocated body IDs advance;
it must match the same original normalized data both times. This proves the
equipped-item multiset, not exact anatomical slot assignment between separate
cold worlds. The original observer did not record semantic body paths, so no
such prior evidence is invented. Anatomy/equipment runtime code is unchanged;
the separate same-game save counter continues to require exact entity IDs.
This correction changes verification only and passed final native validation.

**Final native result: 244/244 passed, zero failures or skips**, from
09:25:18 to 09:27:24 UTC on 2026-10-26. Receipts are
`Verification/Live16x24/east-meadow-green-final.xml` and
`Verification/Live16x24/east-meadow-green-final-summary.json`. The eleven
fixtures include the focused 22 cases, EastTemperingGround, working landscape,
composition/adversarial, exploration geometry/pipeline/worksites and source/
receipt/finalization controls. Independent source and normalization reviews
found no remaining material defect in this bounded change.

## Performance, ordinary acceptance and preservation

All computation occurs once during cold plan construction/generation. Bound
scans by 80×25 and reuse coordinate hashes without consuming caller RNG.
No new Update/LateUpdate hooks, runtime scans, atlases or draw layers. These
source properties are not a measured performance improvement.

Actual 1280×800 captures are in `Verification/Live16x24`; the journal records
ordinary controls and earned-checkpoint provenance. No teleport, supply grant,
visibility override or fabricated gameplay outcome was used.

| Ordinary acceptance | Actual observations |
| --- | --- |
| Seed 64 fresh East entry and matched approach | `east-meadow-after64-entry`: (0,16), tick 1050, 40 HP, 50 drams. Seven steps east and two south reached `after64-approach`: (7,18), tick 1140, 40 HP, 50 drams. This matches the before approach's position/tick/HP for the bounded flower comparison. |
| Seed 64 real forge reader | C, Down, X opened `after64-forge-reader` at the same approach. This verifies access and examination; no crafting is claimed in this pass. |
| Seed 64 save/load | Escape twice closed the reader and then opened pause. F5/F6 still saved/loaded; Tab closed pause. `after64-loaded` is at the same position/tick/HP/drams. All 246 captured owner records and every captured field match the approach when keyed by ID; array order alone differs. This is the observational owner selection, not a claim that all world state was recorded. |
| Seed 64 real border return | Left seven, Up two, Left reached glade (79,16), tick 1240. Ordinary Marlback hatchet hits dealt 14+11 damage, leaving 15 HP. Right reentered East (0,16), tick 1250, 15 HP, 50 drams (`after64-west-return`, `after64-cached-return`). All 120 flower IDs/coordinates match approach, loaded and returned states. Other live world/combat state is not claimed identical. |
| Seed 65 second arrangement | Continue, Right entered fresh East (0,16), tick 450, 40 HP, 50 drams. An Up 13 / Right 15 UI batch timed out; actual observed position was (14,5), so its requested endpoint was not assumed. Up two, Right 15 reached (29,3); two further Right 18 batches reached (47,3), then (65,3). `after65-approach` and C/Down/X `after65-forge-reader` record tick 1230, 40 HP, 50 drams. |

The matched seed-64 approach visibly interrupts the repeated pink rows while
retaining substantial meadow groups and the same service artwork. The current
seed-65 approach demonstrates the second arrangement and readable working pad.
The older `east-shelter-after65-upper-use-approach.png` is a **qualitative**
reference only: route, tick, HP and intervening art revisions differ. There is
no exact matched before-65 claim and no new seed-65 save/border-return claim.

The named repetition defect is accepted within this scope. The meadow still
contains repeated native flower marks by design; this is not complete scene or
opening acceptance. Repeated brine application/expiry messages were observed in
the seed-65 log; that existing feedback behavior is unchanged and outside this
slice. No new sprite, atlas, draw layer or per-frame computation was added, and
no measured FPS improvement is claimed. Current packages predate the slice.

## Self-review and resolved findings — 2026-10-26

- 🔵 Exact source audit supports thinning rather than count-conserving reflow;
  no finite flower harvest allowance exists to protect.
- 🔵 Resolved: optional workpad substrate depends on absence of flowers. One
  optional-floor admission condition retains original exclusions; exact floor
  positions, functional owners and stock pass all three full-generation pins.
- 🔵 Resolved: the consumed-receipt expectation and allocated equipment-key
  baseline mistakes were verification defects, corrected without runtime
  changes and explicitly retained in the test history above.
- 🧪 Native 244/244 and the bounded ordinary routes are accepted. Packages
  still precede this slice; full opening/region art and physical Deck remain open.
- ⚪ This is original CoO composition with Qud-like readability, not a claim of
  engine/art or mechanics parity. Do not reopen accepted sprites without a
  separately demonstrated defect.

## Accepted implementation and source review

`SpreadCompositionPlan` captures a private 80×25 byte mask only after original
connectivity repair. Bit 1 records original flower cells; bit 2 records retained
flowers. During capture the readonly field is still null, so the original
grammar is observed. A smooth coarse coordinate affinity and smaller fine
variation rank flowers within each of the existing separated upper, lower and
eastern bodies; `OpeningFlowerRetention = .64` retains their substantial
presence. Stable coordinate tie-breaks make repeated cold plans deterministic.
This is an authored visual proportion, not a conserved gameplay population.

`ObjectAt` only overrides cells recorded as original flowers: removed cells
return null directly, without falling through to a newly generated Bush.
`SpreadExplorationWorksites` retains the original flower exclusion in its
existing optional east floor loop. Core placement, actors, resources, source
proofs, transaction and rollback code are unchanged.

Independent source review found no material issue in capture ordering, exact
scope, nonflower preservation, deterministic ranking, optional-floor admission
or the cold-only lifetime. The reviewer separately checked that the focused
tests use independent original receipts and that the pad counter has two real
excluded cells plus ten admitted plain-Grass controls. This is bounded source
acceptance, not visual or gameplay acceptance.

Owned implementation files:

- `Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationWorksites.cs`
- `Assets/Tests/EditMode/Gameplay/World/EastMeadowCompositionTests.cs` and fresh metadata
- This living design record and the original native verification receipts

Root owns concept/extraction records, Unity verification, actual captures and
the final release/push record.


## Root baseline, concept and native RED — 2026-10-26

Actual current1280×800 seed64 captures are `east-meadow-before64-entry`,
`-approach` and `-forge-reader` in `Verification/Live16x24`. The ordinary replay
continues the original earned pre-East checkpoint, crosses the border, walks to
7,18 and uses C/down/X. Tick1050entry and1140approach/reader,40HP/50drams.
`east-meadow-play-journal.json` records controls and provenance. JSON owner lists
are observational full censuses and include undisclosed owners; only the PNG
is the visual acceptance record. No grants, teleport or visibility override.

Built-in imagegen reinterpreted the actual approach. Prompt, concept and explicit
part/owner extraction are in `ArtSource/Live16x24/concepts/east-meadow-composition/`.
Keep its connected openings/unequal groups. Reject excess thinning, changed
resolution/visibility, smoothed sprites and invented gradients. Approved native
flowers and all other sprite pixels remain unchanged; native production here is
the editable placement source. Original reference library R01 remains the
readability context; no reference artwork is imported.

Native focused RED ran 09:04:04–09:04:06 UTC: 18 cases, 8 passed, 10 expected failures, zero skips
(`east-meadow-red.xml` and summary). Original nonflower/subset pins passed. Eight
shape cases failed at 100% original coverage; missing original-substrate query and
12 rather than 10 paving cells failed separately. An earlier request encountered
a transient domain-reload 503 and is not test evidence. The connected-group test
was tightened before production to require a formerly interior flower, preventing
edge-only erosion from masquerading as a new interior opening.

At this original baseline/RED checkpoint, 776 normal saves, 372 existing
art/source files and two foreign PNGs were hash-identical; roughly 428 GiB was
free. Storage checks were read-only and no cleanup occurred. Runtime, GREEN and
ordinary acceptance were pending at that historical point and are now recorded
above. Root owns the final preservation, journal, coverage and push records;
this paragraph does not stand in for their final verification.


## Next scene handoff: existing surface water (not yet implemented)

Read-only source/save review identifies the East pure-water cell-state patches
as the next visible inconsistency: outside glade they remain flat teal rectangles.
Existing glade native bank frames 0–15 are the candidate reusable art. Do not widen
all ground/material admission or reskin other blue glyphs and liquid-source
entities. Mixed liquids, ice, hidden neighbors and overlays retain their rules.

The earned random-seed kitchen fixture's East TileState footer contains permanent
pure-water cells (11,2), (10,3), (11,3), (10,4) and (51,17–20). These are actual cell coatings,
not proof of finite drinkable source entities. The upper patch is visible in
`packaged-glade-east-13-east-entry.jpg`. Resume the preserved
`Fixtures/packaged-glade-east-earned-kitchen` with ordinary Continue; the recorded
northern return reaches East (27,24). From there, walking toward (50,20) is a candidate
approach to the vertical patch, subject to actual obstacles/combat. Capture a
before view before any shoreline change, then follow the full concept/extraction/
native gates. This old earned graph can prove rendering continuity, not fresh
meadow generation. No water code or assets changed in the meadow pass.
