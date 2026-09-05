# W6.5 — the Felling-Site

Status: complete. Final suite **7547/7547 GREEN**, zero C# errors,
2026-09-05 19:18:46–19:19:45 UTC (+77 tests).
Baseline W6.4 commit `7e8f31c7`,7470 tests. Earlier spell work remains protected.

## Implemented behavior

The authored Tier5 POI at(3,5) selects a dedicated surface pipeline. Open
pink-grey stone surrounds a softly creased circle, six bare positions and one
empty seventh. The ordinary map-descent arrival at(40,12), all seven positions
and every perimeter cell are reachable. No random cave, monster, loot, village,
quest or ending interaction overlays the scene. POI behavior uses its type,
not its display name; underground zones keep their normal routing.

After each player's normal end-of-action cleanup, the seventh checks the
player's actual occupied cell. Waiting works. Standing at the point applies
normal nonstacking Confused(2): -2DV and -2Agility. It respects existing effects
and vetoes, adds no damage/world fact, and leaves a brief aftereffect that
expires normally when the player leaves. Adjacent cells, NPC turns and the six
other positions have no exposure. Urqu has no intent or speaking occupant.
The new post-cleanup application clears its own JustApplied flag so it does
not accidentally acquire a third action of duration.

The six saved Barren ground entities reject rooted vegetation through the
shared placement gate, before a moved plant leaves its source cell. Thirty-one
explicit blueprint classifications cover crops, flowers, rooted plants, fungi,
living substrate terrain and standing dead vegetation. Food, seeds, furniture,
mobile creatures and carried materials remain allowed. Installing a barren
marker clears only vegetation; loading repairs the same invariant independent
of object order. Older flora saves gain only the new Vegetation classification
from current blueprint data. Seed consumption, plot conversion and builder
success reporting now depend on actual placement success.

Old worlds receive the site POI only when its cell has no saved POI. Existing
cached ground remains intact, including player modifications; it is not
regenerated into a circle. Cached WorldMap terrain refreshes only its derived
glyph/color/name after all entity bodies resolve. IDs, occupants, custom
properties and other render fields survive. Loading does not create an
uncached WorldMap zone. Nine original16×16 sprites provide four variants each
for bare positions and stone creases, plus the empty seventh.

## Verification evidence

| Gate | Result / raw artifact |
| --- | --- |
| Authoring TDD |14 expected missing-content failures among16 cases →20/20 with saved-map controls (`W65-authoring-red.xml.gz`, `W65-authoring-green.xml.gz`) |
| Save TDD |1/4 RED on pre-site map rehydration;3 preservation controls passed (`W65-save-red.xml.gz`) |
| Runtime TDD |10/19 RED for missing exposure/barrenness/seed protection →39/39 focusedGREEN (`W65-rules-red.xml.gz`, `W65-rules-green.xml.gz`) |
| Dedicated adversarial |36 final cases, allGREEN; 31-flora census within a case. First32/33 run had one incorrect fixture biome expectation, corrected using actual map authoring (`W65-adversarial-first.xml.gz`) |
| Deterministic scenario |Intentionally missing-class compileRED →38/38 including final36 adversarial +2 scenario checks (`W65-bench-compile-red.txt`, `W65-adversarial-bench-green.xml.gz`) |
| Native Play |Run `657ea017c8ae4f87a77638a8ad210d89`:9/9 rowsPASS, zero failures, zero C# errors, launcher exit0 (`W65-live-profile.json`) |
| Art |9/9 precise dimensions/palette/alpha/importer contracts; two distinct4-variant families, zero projectGUID collisions (`W65-art-audit.json`) |
| Full suite |First7544/7547: facade violation +2 stale wilderness-only assumptions (`W65-full-integration-red.xml.gz`); next7546/7547: only the user-recorded fungal self-cloud flake (`W65-full-known-fungal-flake.xml.gz`). Final **7547/7547 GREEN**, zero C# errors (`W65-final-full-green.xml.gz`). |

Raw XML and frameCSV are gzip-compressed without rewriting their contents.
Compiler errors are always checked before trusting XML. Fixture syntax/internal
access mistakes were corrected before accepting assertion results; no stale
results were counted. Native8 matrix rows stimulate the real systems directly;
the ninth audits actual keyboard waiting through production input.

## Cold-eye and hypothesis review

Independent source reviews checked taxonomy/atomicity and canon consistency.
The first full suite subsequently found a 🟡 integration miss: production
exposure bypassed Entity.ApplyEffect. It now uses that shared facade, keeping
material/status dispatch and presentation capture consistent. The strict
facade source pin was not weakened. Two older Stump tests now distinguish
authored POIs from wilderness while continuing to reject random lairs/camps.
No confirmed 🟡+ defect remained after these fixes. This is CoO-original content, so there is no
new Qud-port claim requiring an invented counterpart. A hypothesis-driven gate
then pinned saved exposure/expiry, nonzero prior stat penalties, effect veto,
dead/removed/foreign marker, duplicates, NPC/adjacent controls, movement source
and index/version preservation, all31 vegetation classes, seed refusal,
loaded object order, pre-tag legacy flora, cached ground preservation, renamed
routes, unknown POIs, sinkhole discovery, real lateral arrivals and descent.
These are regression pins, not a claim that every possible bug was found.

The only initial adversarial assertion failure was test data: Olderdeep(4,6)
is in Grovelands, not Stump. Its hidden-map control now uses the actual underlying
biome and still requires the discovered name only after visitation. No production
behavior was changed to satisfy a wrong geography assumption.

## Performance and honesty bounds

Native capture:75.001255583seconds,74,360frames,7,410world ticks. The exposure
marker was active in741frames. Full-frame p99 is0 because action-bearing frames
are under1% of the interval; the table below reports positive-sample p99 as well
as preserving all original samples in `W65-live-frames.csv.gz`.

| CPU marker | Active-frame mean | Active-frame p99 | Maximum |
| --- | ---: | ---: | ---: |
| Felling exposure |0.085080ms|0.220875ms|0.381000ms|
| Turn processing |0.007006ms|0.015916ms|0.068500ms|
| Input Update |0.020948ms|0.047500ms|4.740042ms|
| ZoneRenderer LateUpdate |0.699776ms|2.372584ms|10.300292ms|

GC is whole-editor/frame data, not allocation attribution to this feature:
active-frame mean27,673.9bytes, p99364,884bytes, max18,926,380bytes. The ordinary
cell check allocates nothing by inspection; effect creation and messages do.
The arithmetic and positive-sample counts are in `W65-active-metrics.json`.
No exposure-only GC claim or zero-allocation claim is made. The earlier
pre-facade capture is retained under W65-pre-facade-*; these table values
come from the final corrected implementation.

**Can verify (script-observable):** authored counts, reachability, exact
positions, actual status/stat/expiry behavior, saved memberships, content
mapping, native input-driven waiting, raw CPU/frame values and test outcomes.

**Cannot verify (visual/feel):** atmosphere, readability at game scale, visual
suitability of the stone circle, the sensation of wrong air or the landmark's
fit in the wider mountain. The inspected pixel contact sheet is art evidence;
headless Play and CPU capture do not establish a human look-pass. The previously
recorded renderer destroyed-camera teardown warning remains separate debt;
this native audit/exit was captured before editor teardown.

## Deliberate scope interpretations

| Canon / plan | Shipped interpretation |
| --- | --- |
| Six bare positions + one highest-bleed point |Exact tile geometry is authored staging; nothing assigns the positions to named Firsts. |
| Wrong air / standing effect |Existing two-action stat confusion, with brief ordinary aftereffect; no canonical numeric duration is claimed. |
| Nothing grows at the six |Explicit rooted/standing-dead vegetation taxonomy; dropped food/furniture are not living roots and remain allowed. |
| Existing worlds |Map repair adds the landmark, while previously cached ground is preserved rather than forcibly regenerated. |
| Place precedes plot |No Naro-history adjudication, intentional Urqu, god NPC, quest or ending action. |

## Files changed

New: FellingSiteBuilder, SeventhPositionPart, BarrenGroundRules; five FellingSite
test classes; FellingSiteBench/Player + menu/batch launcher; ArtTools pixel
source and nine PNG/meta pairs. Modified: PointOfInterest, WorldGenerator,
OverworldZoneManager, WorldMap, WorldMapZoneBuilder, Zone, TurnManager,
SeedPart, FarmPlotSeeder, BuilderSpawn, Objects.json, SaveSystem and
EnvironmentSpriteRenderer; existing StumpFormation/WorldMapAuthoring tests now
recognize authored POIs. Phase log, parity classification and this report
ship with the code. Only incremental changes enter the two shared files.

Protected-work check: all403 baseline files still exist. Only our incremental
SaveSystem/EnvironmentSpriteRenderer changes and the editor's runtime MCP log
differ. The two staged shared files retain the exact preexisting edit sequence
in the worktree; every other protected file is byte-identical. Additional
concurrent ArtSource/ArtTools component-extraction outputs remain unstaged.
