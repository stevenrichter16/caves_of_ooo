# Grovelands composition system

Status: complete and integrated, 2026-09-12.

## Intent and art direction

Grow places, not uniform scatter. Preserve the four shipped formations and the
Choir's grown material culture: open cathedral floors, water-veins, concentrated
fruiting shelves, and ordered beds of the half-reclaimed. Preserve two-color
voxel objects. Reuse actual modeled native blueprints so harvesting, damage,
occupancy and interaction remain authoritative. No new decorative-only objects.

## Verified architecture and corrections

| Existing behavior | Design consequence |
|---|---|
| JungleBuilder scatters trees before formation placement | Replace only wilderness Grovelands terrain with composition-aware growth. |
| Formation selection is stable by zone identity | Preserve selection; world seed varies layout and local character. |
| Formation runs at 2500, connectivity at 3000 | Reserve spaces before growth; retain existing reachability repair and final connectivity. |
| Grove has guaranteed seep/sign; field has 2–4 caches | Preserve signatures and loot budget, vary their arrangement. |
| Voxel renderer supports four chunks; ring supports eight | Extend supported wilderness Grovelands through a shared predicate, retaining native fallback for unmodeled entities. |
| A biome can contain authored POIs and mountain summit groves | Exclude authored sites and retain the existing summit builder. |

## Implementation sequence

1. Pure deterministic plan: focal clearing, secondary openings, shared dry edge
   approaches, regional moisture/growth fields, bounded beds and shelf spaces.
2. Native terrain realization: clustered canopy and understory, quiet floors,
   then existing formation mechanics consume the planned focal spaces.
3. Wider voxel presentation for wilderness Grovelands, no generated-world replay
   in rendering. Four formation seed sheets and live spawn inspection.
4. Seed, boundary, signature, reachability, native-ownership and regeneration
   tests; review, fix, regression sweep and living evidence.

## Verification and performance

Run meaningful RED tests before implementation. Check same-seed repeatability,
different-seed variation, shared neighbor portals, open reserved approaches,
signature exclusivity and unchanged loot ceiling. Exercise actual native
generation, not just plan masks. Inspect multiple layouts rather than manually
editing a showcase. Keep generation bounded to chunk-sized arrays; no new
Update loop, new renderer cache, or per-frame regeneration. Emit a composition
diagnostic with formation, seed, focal point and growth counts. No performance
or balance claims from static screenshots. This is CoO-original design, not a
Qud parity port. Deferred: new bespoke model families and new ambient audio;
this phase uses the already shipped voxel kit and native gameplay affordances.

## Review and iteration log

- G01: RED at compile time because the composition types and formation seam did
  not exist. G02: 38 targeted checks passed after implementation.
- G03: two expected renderer-boundary failures identified the remaining four-chunk
  restriction. G04: 110/113; prior renderer scope pins exposed that sinkhole mouths
  were not in WorldMapAuthoring.Places. Added SinkholeSites exclusion instead of
  weakening those existing pins.
- G05: rendered all four formations at three seeds. Visual finding: reserved
  openings swallowed too much canopy. Added explicit complementary copse masses,
  mirrored with the bed arrangement; kept regional field sampling independent.
- Independent source review found three material issues: fen realization still
  used legacy waterways, late solids intruded on reserved approaches, and the
  shared pipeline reached POIs. G06 reproduced six failures. Fixed water/placement
  guards and made composition an explicit wilderness-only pipeline choice.
- G07: 118/119. The remaining assertion found the intentional GroveSeep destination
  at the center of the dry approach mask. Excluded its single terminal cell and
  explicitly asserted that the seep remains real water. Surrounding approach cells
  still must remain dry and open; no blanket water exemption.
- G08: twelve actual voxel renders show stronger vegetation masses and distinct
  formation spaces. All twelve report zero missing voxel mesh mappings. Field
  examples carry 55–67 row entities and 2–3 caches, within the original 2–4 budget.
  These are terrain/formation previews, not full population or gameplay captures.
- Added native snapshot tests comparing real blueprint/position and water state
  despite different caller RNG histories. This bounds determinism to the composed
  terrain and formation; other legacy pipeline stages retain their existing RNG.

## Authoring controls and boundaries

`GrovelandsCompositionPlan` owns the biome rules: clearings, bed extents/stages,
mirrored group placement, dry approaches, local canopy groups, and regional
moisture/growth. `GrovelandsCompositionBuilder` translates those masks into the
existing Grass, Tree, Bush and VineWall native entities. The formation builder
uses planned spaces while retaining its signature mechanics and reachability
repair. The plan is generation-only; renderers never reconstruct destroyed growth.

World seed controls local composition and character; zone identity retains the
shipped 3:2:2:1 formation weighting. Character changes canopy intensity; moisture
changes understory density. Neighbor dry portal positions agree by hashing the
shared boundary. Regional influences sample adjacent world coordinates. Local
fen channels intentionally terminate inside the chunk; this does not claim a
world-spanning hydrology simulation. Existing final connectivity may open further
routes. No new creature AI or ambient sound was introduced.

Voxel rendering extends to authored wilderness Grovelands; town, sinkhole and
special doll-site identities retain their previous art routing. A finite static
zone-address set avoids parsing/allocation during entity recipe resolution.
Source art, palette, camera and full-reveal configuration are untouched. Existing
unsupported native entities keep the presenter's fallback instead of disappearing.

- G09 full suite: 11,863 tests, 11,829 passed, 34 failed. Exact-name comparison
  with P16 shows the same 32 baseline failures plus two new regressions: exposed
  MendleafPlant missing its voxel mapping, and a projected raised-body pick just
  outside the zone. Added a narrow MendleafPlant→green Bush art-family fallback
  (original entity/Harvestable unchanged), and reject out-of-bounds ground picks
  before raycasting. This preserves the existing invalid-pick contract; raised
  hits inside the zone still use their real geometry. Both failures were existing
  regression tests, not newly weakened assertions.
- The independent re-review cleared the three composition issues. Strengthened
  native repeatability preconditions to require both builder calls to succeed.

- G11 final targeted gate: 357/357 passed, zero compiler errors, including the
  newly exposed mapping/picking regressions and all composition adversarial cases.

## Reproducing the visual review

With the editor closed and the MCP server running, invoke the installed Unity
binary with `-batchmode -projectPath <repo> -executeMethod
CavesOfOoo.Editor.GrovelandsCompositionPreviewBatch.Run -quit -logFile <log>` and
set `GROVELANDS_PREVIEW_OUT` to a new output directory. The batch creates twelve
actual voxel renders (four formations, three seeds), logical plan signatures and
an object-count receipt. It does not save scenes/assets or persist preferences.
Camera pitch is the existing gameplay surface; these previews omit the HUD,
native population and live lightmap. Inspect the ordinary game separately.

Visual artifact: `Verification/VoxelWorld/G08-composition-preview/contact-sheet.png`.
The final terminal-seep and picking/mendleaf fixes do not change these selected
preview geometries; native gameplay verification is recorded separately.

## Final verification and handoff

G12: 11,863 total / 11,831 passed / 32 failed, zero compiler errors. Exact failure
names match the recorded P16 baseline; zero new failures. G11 targeted: 357/357.
New composition fixtures contain 36 cases, including seeded loops, reproduced
review failures, full-mask approach checks, POI exclusions and native repeatability.

Normal Unity bootstrap + N verified Overworld.2.6.0 at (40,12), walkable, world seed
966875690. The native chunk contains 68 Tree entities, 83 CompostRows and 2 caches;
voxel presentation reports 31 mesh replacements, zero missing, no failure. Full
reveal and 3D mode are true. Captured and visually inspected the real gameplay
camera: clearings separate the growth masses and the player reads against open
floor. Left Unity playing the fresh game. Live evidence and image:
`Verification/VoxelWorld/G10-composition-final/live-verification.json` and
`live-spawn.png`.

Can verify: native layout/ownership, seed determinism within the composition
stages, tested reachability and approach reservations, rendering state, twelve
preview images and actual fresh-game startup. Cannot infer: extended play balance,
player preference, or hitch-free runtime from these checks. Later gameplay effects
and legacy landmark/population stages can change the generated ground; this is
not a permanent ban on blocking or wetting paths. Existing saved/generated chunks
are not rebuilt or migrated.

Preservation boundary: new planner, terrain builder, tests and preview tool are
included directly. Six modified existing native files remain installed in the
mixed working tree; the exact delta, source hashes and reverse-apply verification
are recorded under G10 rather than absorbing unrelated foundation changes.
No palette, model assets, camera, spawn address or visibility options changed.
