# Isolated shale cave image-reference parity test

**Status: PARKED by the user's latest direction, 2026-10-27.** The user ended
the cave detour, requested straight walls and resumed broad biome/area sprite
coverage, starting with shared Bed and Chair reuse. No reference parity or
whole-area polish acceptance was earned. Preserve all editable candidate sources,
rejected versions, tests and actual evidence reversibly; do not continue cave art
iterations automatically. Straight original silhouettes are the normal default;
layered/shaded native2D relief against flat quiet floors remains future area-polish
guidance. The iteration instructions and checkpoints below are retained history.


## Target and reproducible baseline

- Target: `ArtSource/Live16x24/shale-caves/scene-reference-01.png`, the AI
  reinterpretation (1586×992), **not** a game screenshot. The user's clipboard
  attachment has identical decoded pixels. Its encoded bytes differ.
- Actual baseline: `Docs/Verification/Live16x24/shale-caves-after-01-depth6-entry.png`.
- Earned fixture: `Docs/Verification/Live16x24/Fixtures/shale-caves-depth6-93/`.
  Ordinary Continue opens `Overworld.16.13.6`, player40,13, HP33, turn12430.
- Use ordinary GameBootstrap, flat16×24, normal1280×800 view, same stationary
  checkpoint for comparisons. A cave bear can attack if turns advance; do not
  change AI, health, entity positions, collision, visibility or the save for a
  prettier shot. Separate ordinary interaction trials retain their outcomes.
- Prior approved art, saves, packages and unrelated work remain preserved.
  New candidates are sibling sources/resources, with an opt-in isolated test.

## User-approved visual goal

The walls should read as layered stone with shaded relief, broad readable
facets, dark crevices and coherent joins; the ground should remain flatter and
quieter, with surface chips. This is shaded native2D art, not extruded geometry.
The old ban on bevels/edge shadows in WALL-REVISION-01 is historical and is
superseded for this isolated test by the user's explicit preference.

## Saved pre-implementation corrections

| Earlier premise | Verified correction | Consequence |
|---|---|---|
| The supplied scene was actual gameplay | Exact decoded match to the AI scene-reference | Label reference and actual captures distinctly |
| Cool source pixels should look cool in play | Cave color filter1.05/.88/.70, contrast−5, saturation−10; lit tilemaps also receive dungeon ambient.45 and an automatic amber torch | Separate controlled color/light comparisons before compensating source art |
| Current quiet wall texture could produce the concept relief | Current two cells use low-contrast internal single-row strokes, uniform borders and no cardinal boundary selection | Author original faceted layered sprites and bounded disclosed-edge variants |
| Exact full-image equality is a useful target | AI changes resolution, portions of geometry and visibility and smooths pixels | Compare registered material/composition regions, retaining real owners and native pixels |

References read: FlatPixelShaleCavesCatalog, ZoneRenderer.FlatPixelPaint,
BiomeColorPatcher, BiomePalette, LightSourceSpriteHook, FlatPixelStumpMaterialsCatalog,
LiveFlatPixelGreyboxTools, build_shale_caves.py and the existing shale source
manifest, critique and actual-play records. Qud reference: none; this is an
original presentation experiment and makes no Qud-parity claim.

## Historical iteration process — parked

1. Freeze and capture the actual stationary baseline. Record runtime grade,
   lights, camera and save/state identity.
2. Capture controlled color A/B: neutral grade alone; then neutral light with
   the decorative player torch removed. Preserve fixture lights and fog. These
   are temporary presentation experiments, not gameplay acceptance.
3. Author a reversible sibling native packet. First compare richer layered
   walls and understated floor chips; then complete finite disclosed-edge
   variants. Keep old source pixels and all accepted assets unchanged.
4. Write and run failing native tests before changed runtime integration.
   Implement an explicit isolated opt-in, exact current graph/zone admission,
   state/visibility-aware selection and reversible lighting. Test negative cases
   and resource failures. No global Cave palette replacement.
5. Reopen the same earned checkpoint; capture actual before/after views at
   normal zoom. Independent review names remaining material/color/join defects.
   Fix the largest visual mismatch and repeat until the acceptance table passes.
6. In separate ordinary play, verify wall damage/destruction/repaint, readers,
   departure/reentry, fog/memory and restart; profile without unsupported FPS
   claims. Record remaining limits, review exact owned files, push to main and
   verify the remote commit. Do not return to broad coverage merely because a
   first candidate compiles.

## Historical visual acceptance ledger — no final parity acceptance

| Gate | Required observation | Status |
|---|---|---|
| Wall material | Clearly stacked slate facets at normal zoom, varied ledge lengths, dark seams and restrained highlights comparable to target | Missing |
| Wall continuity | Joined masses do not become isolated repeated bricks; exposed boundaries stay inside actual wall owners, no false walls | Missing |
| Floor | Cool slate-gray flat ground, visible scattered shallow chips without uniform noisy gravel carpet | Missing |
| Contrast | Wall shade range approaches reference while player, items and routes remain easy to distinguish | Missing |
| Scene | Same real layout/camera and quiet-vs-dense balance; no target geometry fabricated in hidden/passable cells | Baseline pending |
| State truth | Visibility, memory, damage, destruction and reader identity remain truthful; leaving test restores normal rendering | Pending |
| Reproduction | Candidate survives fresh launch/restart and is reversible; actual screenshots match the submitted sources | Pending |

Quantitative patch measurements support, but cannot replace, normal-zoom visual
review. Baseline audit: floor dominantRGB64,55,45 versus target approx64,69,75;
wall luminance p10/median/p90 approximately38.3/42.4/47.5 versus target19.6/39.4/62.4.
Different patches and target smoothing prevent treating those as exact-pixel
thresholds. Freeze matched patch coordinates in the iteration receipts.

## In-phase self-review and bounds

- 🟡 The target invents some continuous boundary mass. Never add walls where
  the simulation has none or infer undisclosed topology to reproduce that.
- 🟡 Applying grade/light changes globally would exceed this isolated test.
  Scope and restoration need explicit counter-checks.
- 🧪 Linux/Deck execution, sustained performance and every cave seed remain
  outside a single-scene visual acceptance. Report them honestly.
- ⚪ Broad regional coverage and shared-furniture work have resumed by explicit user direction; the cave prototype is parked, not completed.

## Implementation log

- 20:55:53UTC: preceding shared-storage package restoration completed, with
  51,426 snapshot objects and49 preexisting dirty paths preserved. Editor idle,
  clean scene; Mac player quit. Its delivery evidence is retained separately.
- 21:00UTC onward: independent source, rendering and visual audits identify
  the three material blockers above. Automation focus updated to this test.
- Last actual hourly report20:28:11UTC; next21:28:11UTC. Future reports separate
  planned experiments, observed improvements and verified parity.

Files changed in this phase: this plan; subsequent source/runtime/test/capture
paths will be recorded with their verification before publication.

### Controlled rendering comparison, 21:07UTC

Five actual Game captures00–04 retain the same40,13/HP33 checkpoint without
advancing turns. Floor dominantRGB changes64,55,45→58,61,63 with neutral grade,
then86,90,94 with ambient1/decorative torch off. Thus the candidate floor base
is63,68,74 rather than compensating for amber in the pixels. The wall's small
hatch remains inadequate under neutral lighting. A separate unknown-color trial
recovers a dark background while keeping the same visibility mask. All four
comparison/restored-control sidebar and hotbar rectangles are pixel-identical
to baseline. See `shale-parity-color-experiments.json`; these are controlled
rendering experiments, not accepted production or parity.

The temporary runtime values were restored, isolated files archived byte-for-byte,
and Play ended. Candidate sources are being authored separately. Native gate
fixture initially failed compilation because this project's NUnit lacks
`NonParallelizable`; that unsupported attribute was removed. Two zero-test
jobs count as neither RED nor GREEN. A real discovered failing run is required
before production implementation.

### Native scope gate, first verified cycle

The actual discovered19-case gate run failed19/19 on missing study type before
implementation (`shale-parity-gate-red.json`). The small default-off study gate
then passed19/19 (`shale-parity-gate-green.json`). This accepts only explicit
isolation/exact-current-zone admission, not candidate art, lighting or parity.
The sibling atlas and two independent test fixtures now await their own native
RED→GREEN cycle. No normal cave palette or approved source has been changed.

### First actual candidate and named revision, 21:21UTC

After40expected failures/one existing legacy pass, the combined new scope/art/
preview plus preserved shale suite passed96/96. Actual candidate05 uses the
new source v2; all2,000 cells' owners/visibility, player40,13/HP33 and camera
match baseline exactly. World HUD and hotbar remain pixel-identical. Floor
means are close to the target; wall shade spread improved9.2→47.23 but the
walls remain too blue/green and too finely patterned. The floor's three small
clusters read as a repeated tiny-triangle carpet. This iteration is **not parity**.

Revision v3 is bounded to fewer/broader gray wall planes and two larger flat
angular floor chips percell, preserving floorbase63,68,74. Reference-like
continuous wall bands cannot be drawn over undisclosed floor: the actual
cell census proves those concept bands sometimes invent solid boundaries.
This is a mechanical exclusion, not accepted material parity.

Independent code review found a pause lifecycle issue: Repaint can be called
while fullscreen UI owns presentation, and the study grade remains active.
Actual inventory08/09 retained the new Canvas UI (no observed world overwrite),
but every pixel changed grade on toggle. A new pause admission-loss test will
fail before the fix; after fixing, actual inventory/toggle needs stable pixels.
The generic borrowed-tilemap overwrite concern remains a source-level risk.

### Pause correction RED, 21:26UTC

The new fullscreen-pause lifecycle case failed on the uncorrected preview.
Tick now suspends presentation overrides while Paused; Repaint independently
refuses Paused to protect menu-owned surfaces. Actual after-fix menu comparison
remains pending. Controlled void07 gives27,21,21, close to reference28,22,19,
using(.114,.085,.070,.75); retained native pixel/grade quantization prevents
claiming exact background equality. Candidatev3 source is imported next.

## Actual hourly report — 2026-10-27 21:28:31UTC

Completed: broad coverage held by user; original cave/controlled grade-light
captures; isolated candidate integration and actual05;96nativepasses. Floor
color is close, wall/floor shape parity is still open. In progress: revisedv3
broader grayer wall planes and two larger floor chips; pause/restore correction.
Next: actualv3 comparison, independent review and named remaining revisions,
then ordinary interaction/travel/restart and exactownedmainpublication.
Shared-storage source d9f757841 is verified onmain; caveexperiment remainslocal.
This report does not accept an area or restart broad coverage.
Next hourly report due22:28:31UTC.

### V3 result and bounded shape revision, 21:38UTC

Actual10/14 retain the stationary fixture. V3 wall means now closely match the
reference, but independent review still fails parity on repeated roof-shaped
floor chips and aligned horizontal wall planes. Palette tuning is held. The
next finite contract is72frames:8 distinct floor layouts and16 disclosed-edge
masks times4 distinct wall plane layouts,1152×24. Shape schedules and quiet
intervals must differ, not merely translate. A separate16×16 gray Rubble
derivative addresses the actual51visible olive rubble owners; original art and
semantic identity remain unchanged outside the isolated study. New contract
and derivative tests must fail before runtime adaptation/publication.

The pause correction passed147unique native cases. Actual inventory11–13 are
byte- and pixel-identical across active/disabled/rearmed study; Escape restores
the candidate world14. This closes the observed UI-grade defect. V3 isolated
root was preserved byte-for-byte before stopping for the next test cycle.

### Additional pre-implementation API corrections

- The catalog preserves6/7argument construction; the new optional Rubble
  injection is an8argument overload with the borrowed selector before the
  derivative texture. Old callers explicitly supply no derivative. The default
  resource is `Sprites/Live16x24/shale-parity-rubble`.
- `Entity` exposes`SetTag`, not`AddTag`; the newly authored refusal test was
  corrected before native RED. A partial-write compile error is not a valid
  gameplay regression result.
- BiomeColorPatcher's namespace is`CavesOfOoo.Presentation.Rendering`; two
  diagnostic snippets used incorrect namespaces and made no state changes.
- Read-only disk inspection at21:42UTC found383.68GiB free and all49foreign
  dirty-path hashes unchanged. No storage reclamation was performed.

### V4 runtime contract RED,21:41UTC

Native EditMode discovered44candidate-art cases:41failed and3baseline/fallback
controls passed. Failures identify the old576×24resource, rejected1152×24
injection, missing Rubble resource and missing8argument overload. Raw evidence:
`shale-parity-v4-art-red.json`. The minimum adapter now accepts72frames, uses
8floor/4wall variants, and selects the optional16×16 derivative only after
existing exact-owner/visibility/custody guards and complete study admission.
Six/seven-argument callers preserve their original Rubble and explicit-null
behavior. Source publication and GREEN remain pending.

### Actual v4 comparison and bounded rejection,21:45–21:49UTC

The new integration passed165native cases,0failed/skipped (`native-green-03`).
Actual15 keeps all2,000cell owners/disclosure and player40,13/HP33/camera exact.
The gray Rubble integrates materially, but its rounded paired caps and the
walls' closed rectangular planes still differ from the reference. Wall shade
spread regressed49.44→34.86 despite near-target means. Floor silhouettes vary
better but remain faint. Actual15 was shared inline; parity remains rejected.
V5 is restricted to source shapes/facet contrast at the same72+1contract, fixed
floorbase and unchanged runtime behavior. Preserve v4; no further frame growth.

Actual17/18 show the real Rubble and ShaleWall readers with the same native
subject and terrain/structure meaning. Ordinary stair ascent19 disables study
and restores patcher/ambient.45/automatic torch; reentry20 restores the scoped
study with ambient1/torch off. HP remains33. The earned checkpoint is retained
in `Fixtures/shale-parity-v4-travel` (turn12440). A54.55second instrumented Editor
profile retained19,041samples: renderer marker p95.209ms and MainThread p95
3.677ms, but instrumented spikes reach1.91s. This is no matched performance
improvement, sustained FPS, packaged or Deck claim. Recorders were disposed.
All isolated files were preserved before stopping. New depth6 wall destruction
and final-art restart remain pending; old depth5 destruction is not reused as
acceptance of the new isolated scene.

### User-directed faceted silhouette revision,21:55UTC

The user explicitly requests additional facets and non-straight wall boundaries,
with floor pattern continuing beneath recesses inside the wall cell. This
supersedes the experiment's opaque rectangular-wall source restriction. Preserve
logical collision/owners and undisclosed topology; recesses stay inside each
actual wall cell and only on admitted disclosed ShaleFloor-facing edges.
Keep connected opaque cores, bounded stepped contours, no interior pinholes or
transparent seams between joined walls. The existing native floor pattern can
serve as presentation backing, without creating a floor owner or changing wall
identity, reader semantics or hazard representation. Source contour/corner
checks and native underlay/counter-case RED must precede integration.

V5's first source wall was rejected before publication because disconnected
fracture islands replaced horizontal layers. Retain it as a source study; the
next candidate combines connected horizontal strata with the user's new
faceted silhouette direction. Broad regional work remains held.

Independent lifecycle audit also found disabling a suspended preview left its
request flag enabled; a17th preview case now precedes the one-line correction.
Explicit Restore was safe already; component-disable cleanup must also revoke
the flag even when no active lighting snapshot remains.

### Faceted edge contract and integration plan

Keep72terrain frames plus1Rubblematerial. Floors and mask0 walls are opaque.
For disclosed exposed sides only, binary-alpha cuts run1–3native pixels inward
with slow stepped profiles; side unions form corners. The corex3..12/y3..20
remains opaque, with no internal pinholes, detached fragments or cuts at a
joined edge except corners contributed by a perpendicular exposed side.

Use `TryResolveWallRecessBackdrop` separately from the existing ambient-body
method. The Greybox adapter consumes it after successful native wall selection
and without claiming LocalGround or a new floor entity. It supplies the cached
flat floor-pattern tile and white tint as AmbientBackdrop; wall Owner/Kind,
BlocksMovement, foreground and reader identity remain unchanged. Neutral
current TileState and no competing visible substrate/background/source are
required. An exposed wall whose backing is unsafe retains the original opaque
wall. Zero valid exposure uses the candidate's opaque mask0. Missing resources
or old constructors retain prior presentation. Share a private safety predicate
to avoid foreground↔backdrop recursion. Existing dirty-neighbor repaint handles
changes to disclosed adjacent floor; verify both body and backing retire.

The suspended-disable regression failed natively before correction (1/1).
OnDisable now always revokes the study request; GREEN remains pending.

### Recess backing RED and preview GREEN,22:01UTC

MCP reported a job-initialization timeout, but Unity's independently saved XML
confirms77discovered cases actually executed:44prior art and17preview cases
passed;16new recess cases failed on the missing policy. Raw XML and an exact
hash/summary are retained in `shale-parity-recess-red-preview-green.*`; the
transport timeout itself is not counted as RED. The runner finished cleanup
before edits. The suspended-disable correction is now natively GREEN.

The minimum underlay policy and Greybox hook now follow the saved design. A
private neutral-cell/visible-co-owner check is shared, with no recursion. Unsafe
exposed walls fall back to the prior opaque body; the wall's semantics never
become a floor. GREEN, corrected alpha source and actual capture remain pending.

### Test-premise correction after integration,22:04UTC

First integrated run182cases passed166 and failed16 on an over-specific new
test assumption: it demanded GreyboxKind.Wall. In this non-opening region,
TryResolvePresentation uses fallback.Kind (Unknown), while the selected entity
remains a real tagged solid ShaleWall with its correct native reader. Classify
itself does return Wall but is not the kind source on this path. Preserve the
existing classification rather than change unrelated gameplay to satisfy a
test. Correct the test to compare against the same scene with the study off,
retaining explicit entity/solid/reader/backdrop checks; rerun allaffected cases.
Raw failure receipt is retained. This does not relax collision or identity.

## Latest user-directed parking and verified bounds — 22:19UTC

The final retained native receipt is `shale-parity-native-green-04.json`:
**182 distinct passed, zero failed/skipped**, SHA256
`180378048a69cd50fe620d86e714de997e0951d11881a639927f2d323e7cfacb`.
Actual`shale-parity-21-faceted-walls-v5b.png` demonstrates facets and floor-pattern
recesses; repeated wall/floor motifs still differ from the AI reference. These
results do not accept parity. No further source iteration is queued.

`shale-parity-faceted-toggle.json` records actual inventory22, disable while
paused23 (pixel-identical), ordinary Escape to disabled original presentation24,
and explicit re-enable25 (pixel-identical to21). No gameplay turns were
intentionally advanced in this v5b session. The suspended-disable test-premise
correction and prior raw failures remain recorded above.

`shale-parity-parked-restoration.json` records root's explicit disable and observed
Unity stopped, study=false, save-root=null, scene clean at22:19:47UTC. The isolated
v5b directory was already absent: **no new archive was created or claimed**.
Immutable depth6-93, earned v4-travel fixtures and previous archives remain
preserved. Normal-state preservation claims retain their separate receipt bounds;
no cleanup occurred. The prototype stays default-off and opt-in only. Final
candidate ordinary gameplay/restart/packages and full-area parity were not
accepted before parking; they do not block the resumed broader queue.

The current delivered package is shared-storage source`d9f757841`, with eight
actual Mac views, real Crate transfer, save/full-process Continue and564,000
identical world pixels; Linux archive integrity passed independent review.
Delivery evidence publication is pending root. This is separate evidence, not a
package of the cave experiment. See`packaged-shared-storage-actual-play.json`,
`packaged-shared-storage-restoration.json` and its independent review.

Next: `GRAPHICS-SHARED-FURNITURE.md`, two unchanged Bed/Chair cells and ordinary
healthy room65 baseline. Continue every biome/area and later serious separate
polish; defer optional nuanced animation. All55+5 polish states remain unchanged.
Last actually sent hourly report21:28:31UTC; next22:28:31UTC. Parking records no new
hourly report. Root owns runtime restoration, automation and exact publication.

### Preservation publication review

The parked source/evidence packet passed independent review with no new P1/P2
blocker; current normal defaults stay straight and the study requires explicit
isolated opt-in. Source verification passed and all289 frozen handoff hashes
plus49 unrelated dirty-path pins still match. The eight actual resumed Sodden
baseline captures and preserved107/108 fixtures record future furniture work,
not new native coverage.

The staged whitespace check reports36 trailing-space lines solely in six
byte-preserved Unity-generated importer metadata snapshots (including historical
candidate copies). Those exact metadata bytes are retained for source evidence;
no runtime/source-code whitespace issue is present. The structural diff check
with blank-at-EOL excluded passes. No unrelated whitespace was normalized.

Scope divergence: the user ended the full-parity objective. Preserve the
reversible prototype and its unaccepted differences, disable it, and resume
broad coverage. No new cave package or completed parity claim accompanies this
publication. Earlier shared-storage Mac/Linux evidence belongs to sourced9f,
not to the parked cave prototype.
