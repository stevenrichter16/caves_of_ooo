# Visual execution matrix

Status: source inventory initialized; actual acceptance has not advanced. This is the
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

All current status axes start `not-audited`: coverage, polish, population, shadow and
delivery. This means the new execution audit has not been performed, **not** that
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
- Every status axis remains `not-audited`; the 17 prior review objects are unchanged.
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

Surface10.15 now records a bounded dry-field-row observation at seed65, native196/196, actual reader/harvest/freshContinue and independent review. Coverage remains `fallback` for the area because other visible glyphs remain; polish is `in-progress`. Population, shadows and delivery are not advanced. All other current audit axes remain not-audited; historical evidence is preserved.
