# Visual execution matrix

Status: source inventory initialized; bounded field-row and Felling dry-ground
observations recorded. No whole-area polish acceptance has advanced. This is the
machine-readable worklist for [the execution plan](GRAPHICS-EXECUTION-PLAN.md).
The data lives at `ArtSource/Live16x24/visual-execution-matrix.json` and starts at
source `1f68a9ba87f3bbaf37284c0a6ea6b43892719acd`.

## Scope and denominator

| Record kind | Count |
|---|---:|
| area family | 16 |
| biome family | 10 |
| formation family | 31 |
| opening anchor | 6 |
| settlement | 17 |
| shared family | 9 |
| sinkhole depth | 18 |
| sinkhole stack | 6 |
| site instance | 32 |
| surface coordinate | 400 |
| underground band | 6 |
| **Total scope records** | **551** |

There are **400 distinct authored surface coordinates**, drawn directly from the
20×20 `WorldMapAuthoring.BiomeRows`. Their biome distribution is
Beating 107, Grovelands 43, Overwrit 23, Sodden 63, Spread 142, Stump 22.
Tier, road and river flags come from the corresponding authored tables. Formation
and actual scene role are deliberately unresolved until the current pipeline is
examined: a settlement, site or exploration family can replace an ordinary recipe.

The matrix contains ten serialized biome-family keys, including four compatibility
keys with no fresh authored surface cells. It has 31 authored surface formation
families (27 ordinary enum formations plus four Overwrit formations), 17 settlements,
six sinkhole stacks expanded to all 18 registered mouth/floor addresses, six ordinary
underground material bands, 16 other area families and six opening anchors.

Other-area instance expansion retains the finite legacy address lists, reads the
11 current `SecondExplorationSites.Destinations` entries and resolves their source
constants, and expands all 7 authored river coordinates. Second-exploration
installation remains conditional on actual version/content/placement. It is not
assumed to exist in a loaded save. Shared-family rows retain all 9 registered
cross-region groups, including the newer generated atlas batch.

These records intentionally overlap. A settlement can also be a surface coordinate,
a river overlay, a special scene and an exploration destination. `related_scope_ids`
links the concrete surface row to those review responsibilities; **551 is not a
count of separate places or accepted areas**. There are 415 distinct registered
zone IDs across the currently expanded records.

## Acceptance and inherited evidence

All current status axes were initialized `not-audited`: coverage, polish, population,
shadow and delivery. Subsequent bounded observations are recorded below. This means the new execution audit has not been performed, **not** that
previously integrated art disappeared. The17 historical concrete polish records are
copied unchanged into `prior_reviews`, including their seed, state, limits and evidence.
Applicable rows reference them by `prior_review_ids`; no old bounded slice is promoted
to new full-area acceptance. Older family coverage is linked by JSON pointer or copied
as a clearly labeled `prior_coverage_summary`.

The historical review distribution is 13 in-progress, three not-audited and one reviewed;
there were no whole-area bounded acceptances to inherit. Historical defects are also
retained separately because some have subsequently been fixed. Reconcile them against
current source and actual captures before calling them current defects.

Absent evidence fields inherit `record_defaults`: empty before/after captures,
reader/route/reload checks, regressions, profiles and independent reviews, plus null
observed revision/reviewer/camera. Every accepted record must override those defaults
with reproducible evidence appropriate to its scope. `not-applicable` needs a reason
and reviewer. Neither intentionally quiet terrain nor a family-level asset census
waives an actual area review.

## Work order and next actions

Follow V0–V7 in the execution plan. Each row includes a next action; the first useful
runtime work is ordinary Spread field/row presentation and Felling ground, followed by
selected-owner gaps and acceptance of the large art batch. Reassess family-owned sparse
Spread composition after its real ground cover renders. Trial broader masonry shadows
in one ordinary settlement before extending other areas.

For a concrete area, record seed, actual role/formation, source revision, normal zoom
and resolution, all whole-chunk views, interiors/entrances, owner/reader/state checks,
ordinary travel/reload, relevant tests/profile and independent review. Track coverage,
material polish, population and lighting independently; a successful surface sprite
cannot close the interior, state, population or packaged-run gates.

Expand settlement `interiors.records` with real footprint/owner identities, not invented
zone IDs. Expand unresolved generated families from the manager/planner at a declared
seed. Select ordinary underground natural/roomed examples and boundary travel for each
band (1–2,3–4,5–6,7–8,9–10,11+); depth 11+ and possible seeds remain open-ended.
Generated-rule changes require the plan's three-seed and adverse-placement controls.

## Source verification and limitations

Initialization checked:

- All four authored tables have 20 rows of 20 cells.
- All17 ledger settlement names/addresses match current `WorldMapAuthoring.Places`.
- All 400 coordinate IDs are unique and in bounds; all scope IDs are unique.
- All 31 formation, 18 sinkhole-depth and six material-band rows are present.
- Eleven distinct second-exploration destinations resolve to source constants.
- Every status axis initially remained `not-audited`; the 17 prior review objects
  remain unchanged after current bounded observations.
- JSON parses and its counts agree with its records. Source SHA256 values are retained
  for drift detection; they are not runtime test evidence.

No Unity run, actual new capture, visual-quality approval, profile, save test or packaged
execution was performed to create this inventory. Source-grounded enumeration is V0
work only. Generated Spread families, regional requests, lairs and merchant camps still
require concrete address expansion. Historical metadata is not guaranteed to describe
all later content; discoveries must add rows rather than silently disappear from scope.

## Maintenance

Use stable record IDs. Update only the owned rows with each accepted slice, preserving
prior evidence and stating its source revision. Recompute `counts` after additions;
keep legacy references and overlap links. Do not set family-wide acceptance merely
because one child passed. Update current summaries in the same reviewed commit as
implementation and evidence. Keep hardware/packaged limitations separate from native
or Editor results.

Minimal validation (from repository root):

```python
import collections, json
from pathlib import Path
p = Path("ArtSource/Live16x24/visual-execution-matrix.json")
d = json.loads(p.read_text())
r = d["records"]
assert len(r) == d["counts"]["records"]
assert len({x["id"] for x in r}) == len(r)
assert dict(collections.Counter(x["kind"] for x in r)) == d["counts"]["by_kind"]
assert sum(x["kind"] == "surface_coordinate" for x in r) == 400
for x in r:
    for axis, state in x["status_axes"].items():
        assert state in d["status_contract"][axis], (x["id"], axis, state)
```

## First current observation — 2026-11-01

Surface10.15 now records a bounded dry-field-row observation at seed65, native196/196, actual reader/harvest/freshContinue and independent review. Coverage remains `fallback` for the area because other visible glyphs remain; polish is `in-progress`. Population, shadows and delivery are not advanced. At that checkpoint all other current audit axes remained not-audited; historical evidence is preserved. The subsequent Felling observation is separately recorded below.

## Felling dry-ground observation — 2026-11-01

The surface3.5, Felling family and its concrete site row now reference the same
bounded observation; these are three overlapping scopes, not three independently
accepted areas. At actual seed704860946, **1638 authored dry-ground cells** reuse
approved Stump ground frames0–3. Coverage stays `fallback` and polish `in-progress`:
355 solid/blocking-water cells and7 ritual landmark cells remain excluded. Population,
shadow and delivery axes do not advance.

[Before](Verification/GraphicsExecution/FellingBaseline/flat-before.png),
[after](Verification/GraphicsExecution/FellingBaseline/flat-after.png),
[ground Examine](Verification/GraphicsExecution/FellingBaseline/ground-examine.png),
[walk](Verification/GraphicsExecution/FellingBaseline/ground-walk.png) and
[fresh Continue](Verification/GraphicsExecution/FellingBaseline/fresh-continue.png)
record the actual flat Editor scope. The baseline was turn290 at40,12HP40; ordinary
walking reached40,6 then40,13. F5 saved turn420 at40,13HP40, and fresh Continue retained
that position/HP. No pixel-identical whole-scene restart is claimed.

[Native regression](Verification/GraphicsExecution/FellingBaseline/regression-green.xml)
passed163/163 with no failures, skips or inconclusives. Root reports independent
source/visual review with no new P1/P2 finding in the dry-ground slice. Source hashes
are recorded; the final commit is pending root publication. See
[the slice record](GRAPHICS-FELLING-GROUND.md).

Source corrections:1645 open dry source positions include7 terrain owners with ritual
meaning, so the safe ground denominator is1638. The Felling address must be reserved
before generic Stump-site admission; a changed POI must not bypass its dedicated
authority checks. The old preview route/legacy-presenter report contains failed
assertions and remains failed evidence. A domain-reload profile/Continue attempt had
empty active-zone counters and cannot establish performance or reload acceptance;
valid later gameplay evidence is kept separate.

Remaining: rock/water/ritual material identity, sparse-center composition, shadows,
sustained performance and its controlled comparison, current package and physical
Deck execution. A valid profile is ongoing with root; this matrix does not predict
its result or mark R11 passed. No full-area or family-wide acceptance is implied.

Root follow-up: clean71s Felling and81s field Editor observations are now linked.
Controlled before/after comparison, spike attribution and packaged performance
remain open. Felling's1,638 count is an integration test over source coordinates;
actual gameplay observation covers the recorded viewport/reader/walk/reload, not
every cell or the entire authored scene.

## Current package observation — 2026-11-01

The current Mac checkpoint through0a2cc9bae has actual cold-launch, field travel/save/freshContinue and Felling dry-ground save/freshContinue evidence in `GRAPHICS-CURRENT-PACKAGES.md`. Overlapping rows retain bounded evidence and `editor-verified` as a conservative area-delivery status; a bounded Mac slice does not certify entire-area delivery. Linux is built/archive-verified only. The Felling performance comparison is recorded, with an unexplained transient allocation/stall burst and packaged profile still open. Existing coverage/polish/population/shadow statuses are unchanged.

Stillleaf depth1 material pilot (2026-11-01): canonical2.4.1 sandstone observed in
actual play,128/128native checks, real underfoot reader and saved/freshContinue.
Coverage stays fallback and polish in-progress due ledge/other glyphs; population
and shadows unchanged. R11 and package delivery for this slice remain open.
See GRAPHICS-STILLLEAF-DESCENT.md and its actual evidence.

## 2026-11-01 Marrowstye shadow observation

Surface12.12 and settlement/marrowstye now record bounded Editor shadow observation
and fallback coverage.116distinct native passes; actual1056changed shade pixels,
occupancy/reader verification and exact639200pixel saved/fresh comparison. Population
and whole-area acceptance do not advance. Oven47,21 remains a named ASCII defect;
earnedWorkshopBaseline3960 is the next material slice. R11 and package lag remain
explicit in GRAPHICS-MARROWSTYE-SHADOW.md.

## 2026-11-01 work-fixture continuity

Marrowstye and shared furniture rows separately record actual ordinary Oven observation
and integrated/native-tested MillStone.147nativepasses and exact4040reload support
bounded body identity; no whole-family, population or performance acceptance.

## 2026-11-01 ordinary surface stone walls

Stump1.2 now records bounded canonical Wall world/Examine/reload,211distinctnative
passes and exact2170reload. Spread admission is native-tested only; family rows
retain their unaccepted status. One-frame repetition, remembered glyph transition,
other owners, population, broader shadows and R11remain open. Batch package next.
See GRAPHICS-SURFACE-STONE-WALLS.md.

## Current delivery through a72a22983

BatchA72 verifies bounded actualMac wall/oven/Marrowshade/Stillleaf materials,
movement/readers/stairs/save/freshContinue; three exact worldreloadpairs. Linux
retryarchiveverified,notexecuted. Full-area axes remain unaccepted. Current source
now matches packages; R11/physicalhardware and separatepopulation/polish remainopen.

## Bounded composition update — 2026-11-01

Spread10.15 seed65 now records the15-owner cold field-margin pilot separately under population=reviewed. Three actual viewpoints,2×readability, interactions and exact reload passed; full-area polish, encounter balance and R11 remain open. Current packagesa72a22983 exclude the addition. See `GRAPHICS-SPREAD-FIELD-COMPOSITION.md`. Next actual Ginmere floor baseline; no universal coverage/polish promotion.

2026-11-01: Ginmere/Cathedral surfaceSandstoneFloor bounded observations added to six overlapping records (surface/depth0/stack).177floor owners per actual seed704860946site;239distinct native passes. Ginmere matched/reload exact, Cathedral after-only. No whole-area polish/population/shadow acceptance. See [surface floor record](GRAPHICS-GROVE-SINKHOLE-FLOORS.md).

## Ginmere first descent — 2026-11-02

`sinkhole/ginmere/depth/1` records observed bounded SandstoneFloor/Wall reuse,137native passes and actual readers/movement/collision/stairs/reload. The overlapping stack gets a bounded note; depth2 remains untouched. Coverage stays fallback for remaining owners, polish in-progress, population/shadows not advanced. R11 and20750worldpixel restart differences remain open. See [slice](GRAPHICS-GINMERE-DESCENT.md).
